using Core;
using Core.Data;
using Core.Helper;
using Game.Graphic;
using Game.Helper;
using Game.UI;
using Game.UI.GameComponent;
using Game.UI.SceneEditGameWorld;
using Library;
using Library.Component;
using Library.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinLibrary;
using WinLibrary.Graphic;

namespace Game.Scene
{
    public partial class SceneEditGameWorld : SceneBase
    {
        private readonly GameWorldSystem gameWorld;

        private UIConfirmDialog uiConfirmDialog;

        private readonly UIEditGameWorldMenuWindow uiEditGameWorldMenuWindow;

        private UIEditGameWorldDatabaseWindow uiEditGameWorldDatabaseWindow;

        private DrawContent drawContent = DrawContent.terrain;
        private int drawContentId = 0;

        private readonly ZoomableTileMapSprites<TileMapSprites> zoomableTileMapSprites;
        private readonly TileMapSprites.MapSpritesInfo mapSpritesInfo;

        private readonly UITileInfoPanel uiTileInfoPanel;

        private readonly UIEditGameWorldTileMapMenuWindow uiEditGameWorldTileMapMenuWindow;

        private readonly PointerStatus pointerStatus;
        private readonly DrawTileStatus drawTileStatus;
        private readonly DrawTileRectangleStatus drawTileRectangleStatus;
        private readonly DrawTileFillStatus drawTileFillStatus;

        private Status currentStatus;

        private GameWorldSystem gameMap => gameWorld;
        private Camera camera => gameWorld.camera;

        public SceneEditGameWorld(GameSystem gs, GameWorldSystem gw) : base(gs)
        {
            gameWorld = gw;

            mapSpritesInfo = new TileMapSprites.MapSpritesInfo(gameWorld);

            zoomableTileMapSprites = new ZoomableTileMapSprites<TileMapSprites>();

            addChild(zoomableTileMapSprites);

            uiTileInfoPanel = new UITileInfoPanel(gameSystem, new Point(30, formMain.Height - 108));

            addChild(uiTileInfoPanel);

            loadMap();

            uiEditGameWorldTileMapMenuWindow = new UIEditGameWorldTileMapMenuWindow(
                gameSystem,
                onPointerButtonClicked,
                onBrushButtonClicked,
                onRectangleButtonClicked,
                onFillButtonClicked,
                onTerrainSelected);

            pointerStatus = new PointerStatus(this);
            drawTileStatus = new DrawTileStatus(this);
            drawTileRectangleStatus = new DrawTileRectangleStatus(this);
            drawTileFillStatus = new DrawTileFillStatus(this);

            uiEditGameWorldMenuWindow = new UIEditGameWorldMenuWindow(
                gs,
                onDatabaseButtonClicked,
                onSaveButtonClicked,
                onExitButtonClicked);
        }

        private enum DrawContent
        {
            terrain
        }

        private void onTerrainSelected(int id)
        {
            drawContent = DrawContent.terrain;

            drawContentId = id;
        }

        public override void start()
        {
            uiEditGameWorldMenuWindow.Show(formMain);

            uiEditGameWorldTileMapMenuWindow.Show(formMain);

            setDrawMode(DrawMode.pointer);

            uiEditGameWorldTileMapMenuWindow.setTerrain(
                gameMap.masterData.tileMapTerrain.Values.ToList(),
                gameMap.masterData.terrainImage);
        }

        public override void finish()
        {
            uiEditGameWorldTileMapMenuWindow.Hide();

            uiEditGameWorldMenuWindow.Close();
        }

        private void onDatabaseButtonClicked()
        {
            if (uiEditGameWorldDatabaseWindow != null) return;

            uiEditGameWorldDatabaseWindow = new UIEditGameWorldDatabaseWindow(gameSystem, gameWorld)
            {
                okButtonClicked = onDatabaseWindowOkButtonClicked,
                cancelButtonClicked = () => uiEditGameWorldDatabaseWindow.Close(),
                applyButtonClicked = onDatabaseWindowApplyButtonClicked
            };

            uiEditGameWorldDatabaseWindow.FormClosing += (s, e) => uiEditGameWorldDatabaseWindow = null;
            uiEditGameWorldDatabaseWindow.ShowDialog(formMain);
        }

        private void onDatabaseWindowOkButtonClicked()
        {
            saveDatabase();

            uiEditGameWorldDatabaseWindow.Close();

            refreshTileMap();
        }

        private void onDatabaseWindowApplyButtonClicked()
        {
            saveDatabase();

            new UIDialog(gameSystem, "alert", "saved").ShowDialog();

            refreshTileMap();
        }

        private void saveDatabase()
        {
            gameMap.masterData = uiEditGameWorldDatabaseWindow.gameWorldMasterData;
        }

        private async void onSaveButtonClicked()
        {
            try
            {
                await save();

                new UIDialog(gameSystem, "message", "data saved.").ShowDialog(formMain);
            }
            catch (Exception e)
            {
                new UIDialog(gameSystem, "error", "save error!" + e.Message).ShowDialog(formMain);
            }
        }

        private void onExitButtonClicked()
        {
            if (uiConfirmDialog != null) return;

            var dialog = uiConfirmDialog = new UIConfirmDialog(gameSystem, "confirm", "exit?");

            dialog.okButtonClicked = () =>
            {
                dialog.Close();

                uiConfirmDialog = null;

                gameSystem.sceneToTitleEditGameWorld();
            };

            dialog.cancelButtonClicked = () =>
            {
                dialog.Close();

                uiConfirmDialog = null;
            };

            dialog.ShowDialog(formMain);
        }

        private async Task save()
        {
            await GameWorldHelper.saveGameWorldData(gameWorld.name, gameMap);
        }

        public enum DrawMode
        {
            pointer,
            brush,
            rectangle,
            fill
        }

        protected void onPointerButtonClicked() => setDrawMode(DrawMode.pointer);

        protected void onBrushButtonClicked() => setDrawMode(DrawMode.brush);

        protected void onRectangleButtonClicked() => setDrawMode(DrawMode.rectangle);

        protected void onFillButtonClicked() => setDrawMode(DrawMode.fill);

        public TileMapSprites tileMap => zoomableTileMapSprites.tileMapSprites;

        private void loadMap()
        {
            var s = this;
            var msi = mapSpritesInfo;

            zoomableTileMapSprites.setTileMap(new List<TileMapSprites>()
            {
                new TileMapViewSprites(s.gameSystem, s.gameWorld, msi, true),
                new TileMapDetailSprites(s.gameSystem, s.gameWorld, msi, true)
            });
        }

        public override void mouseMoved(MouseEventArgs e)
        {
            var gw = gameWorld;
            var gwd = gw;
            var tm = gwd.tileMap;

            var cursorPos = tileMap.cursorPosition;

            if (tm.isOutOfBounds(cursorPos))
            {
                uiTileInfoPanel.setText(null, null, null);

                return;
            }

            var t = tm[cursorPos];
            var mt = gwd.masterData.tileMapTerrain;

            mt.TryGetValue(t.terrainSurface ?? t.terrain, out var tt); ;

            uiTileInfoPanel.setText(tt.name, null, null);
        }

        public override void mouseWheelScrolled(MouseEventArgs e)
        {
            var cursorPos = tileMap.cursorPosition;
            var center = camera.center;
            var sCenter = camera.translateWorldToScreen(center);
            var tileVertex = tileMap.getTileLocation(sCenter);
            bool flag;

            if (e.Delta >= 0) flag = zoomableTileMapSprites.next();
            else flag = zoomableTileMapSprites.previous();

            if (flag)
            {
                tileMap.cursorPosition = cursorPos;

                if (e.Delta >= 0) camera.center = new Point(cursorPos.x * tileMap.tileWidth, cursorPos.y * tileMap.tileHeight);
                else camera.center = new Point(tileVertex.x * tileMap.tileWidth, tileVertex.y * tileMap.tileHeight);
            }
        }

        public void setDrawMode(DrawMode dm)
        {
            if (currentStatus != null) children.Remove(currentStatus);

            switch (dm)
            {
                case DrawMode.pointer: addStatus(pointerStatus); break;
                case DrawMode.brush: addStatus(drawTileStatus); break;
                case DrawMode.rectangle: addStatus(drawTileRectangleStatus); break;
                case DrawMode.fill: addStatus(drawTileFillStatus); break;
            }
        }

        private void addStatus(Status s) => addChild(currentStatus = s);

        public void refreshTileMap() => loadMap();

        public class Status : GameObject
        {
            protected SceneEditGameWorld scene;

            protected GameSystem gameSystem => scene.gameSystem;

            protected GameWorldSystem gameWorld => scene.gameWorld;

            protected GameWorld gameMap => scene.gameMap;

            protected TileMap tileMap => gameMap.tileMap;

            public Status(SceneEditGameWorld s) => scene = s;
        }

        public class PointerStatus : Status
        {
            public PointerStatus(SceneEditGameWorld s) : base(s)
            {
            }

            public override void mouseDragging(MouseEventArgs e, Point p)
            {
                scene.camera.dragCamera(p);
            }
        }

        public class DrawTileStatus : Status
        {
            public DrawTileStatus(SceneEditGameWorld s) : base(s)
            {
            }

            public override void mouseReleased(MouseEventArgs e) => draw(e);

            public override void mouseDragging(MouseEventArgs e, Point p) => draw(e);

            private void draw(MouseEventArgs e)
            {
                var p = scene.tileMap.getTileLocation(e);

                if (tileMap.isOutOfBounds(p)) return;

                switch (scene.drawContent)
                {
                    case DrawContent.terrain:

                        var tid = (byte)scene.drawContentId;

                        var t = scene.gameMap.masterData.tileMapTerrain[tid];

                        tileMap.setTerrain(p, tid, t.isSurface);

                        if (!t.isSurface) tileMap.terrainSurface.Remove(tileMap.getIndex(p));

                        scene.mapSpritesInfo.resetTileFlag(p);

                        break;
                }
            }
        }

        public class DrawTileRectangleStatus : Status
        {
            public Point? startPoint;
            public SpriteRectangle selector;

            public DrawTileRectangleStatus(SceneEditGameWorld s) : base(s)
            {
                selector = new SpriteRectangle()
                {
                    color = Color.White,
                    boundSize = 1,
                };
            }

            public override void mousePressed(MouseEventArgs e)
            {
                if (gameMap.tileMap.isOutOfBounds(scene.tileMap.cursorPosition)) return;

                startPoint = e.Location;

                selector.position = e.Location;
                selector.size = new Size(1, 1);
            }

            public override void mouseReleased(MouseEventArgs e)
            {
                if (startPoint == null) return;

                startPoint = null;

                switch (scene.drawContent)
                {
                    case DrawContent.terrain:

                        var tlp = selector.position;
                        var trp = new Point(selector.position.X + selector.size.Width, selector.position.Y);
                        var blp = new Point(selector.position.X, selector.position.Y + selector.size.Height);

                        var tl = scene.tileMap.getTileLocation(tlp);
                        var tr = scene.tileMap.getTileLocation(trp);
                        var bl = scene.tileMap.getTileLocation(blp);

                        var tm = gameMap.tileMap;

                        tm.checkBound(ref tl);
                        tm.checkBound(ref tr);
                        tm.checkBound(ref bl);

                        ++tr.x;
                        ++bl.y;

                        var width = tr.x - tl.x;
                        var height = bl.y - tl.y;
                        var tid = (byte)scene.drawContentId;
                        var t = scene.gameMap.masterData.tileMapTerrain[tid];

                        tm.eachRectangle(tl, new TileMap2D.Size(height, width), o =>
                        {
                            tileMap.setTerrain(o, tid, t.isSurface);

                            if (!t.isSurface) tileMap.terrainSurface.Remove(tileMap.getIndex(o));

                            scene.mapSpritesInfo.resetTileFlag(o);
                        });

                        break;
                }

                selector.size = Size.Empty;
            }

            public override void mouseDragging(MouseEventArgs e, Point p)
            {
                if (startPoint == null) return;

                var sp = startPoint.Value;

                selector.position = sp;
                selector.size = new Size(e.X - sp.X, e.Y - sp.Y);
            }

            public override void draw()
            {
                if (startPoint == null) return;

                gameSystem.gameGraphic.drawRectangle(selector);
            }
        }

        public class DrawTileFillStatus : Status
        {
            private readonly Stack<MapPoint> points = new Stack<MapPoint>();
            private readonly HashSet<MapPoint> foundPoints = new HashSet<MapPoint>();
            private readonly HashSet<MapPoint> markedPoints = new HashSet<MapPoint>();
            private byte selectedTerrainId;
            private byte? selectedTerrainSurfaceId;

            public DrawTileFillStatus(SceneEditGameWorld s) : base(s)
            {
            }

            public override void mouseReleased(MouseEventArgs e)
            {
                var p = scene.tileMap.getTileLocation(e);

                if (tileMap.isOutOfBounds(p)) return;

                var t = tileMap[p];

                var tdid = (byte)scene.drawContentId;
                var td = scene.gameMap.masterData.tileMapTerrain[tdid];

                selectedTerrainId = t.terrain;
                selectedTerrainSurfaceId = t.terrainSurface;

                markedPoints.Clear();
                foundPoints.Clear();
                points.Clear();

                points.Push(p);

                while (true)
                {
                    if (!points.Any()) break;

                    p = points.Peek();

                    if (tileMap.isOutOfBounds(p) || foundPoints.Contains(p))
                    {
                        points.Pop();

                        continue;
                    }

                    foundPoints.Add(p);

                    t = tileMap[p];

                    if (t.terrain == selectedTerrainId
                        && t.terrainSurface == selectedTerrainSurfaceId)
                    {
                        markedPoints.Add(p);

                        add(p.x + 1, p.y);
                        add(p.x - 1, p.y);
                        add(p.x, p.y + 1);
                        add(p.x, p.y - 1);
                    }
                    else
                    {
                        points.Pop();
                    }
                }

                var list = markedPoints.ToList();

                list.ForEach(o => tileMap.setTerrain(o, tdid, td.isSurface));

                if (!td.isSurface) list.ForEach(o => tileMap.terrainSurface.Remove(tileMap.getIndex(o)));

                list.ForEach(scene.mapSpritesInfo.resetTileFlag);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            private void add(int x, int y) => points.Push(new MapPoint(x, y));
        }
    }
}