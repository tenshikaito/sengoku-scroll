using Core;
using Core.Data;
using Game.Graphic;
using Game.Helper;
using Game.UI.GameComponent;
using Library;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WinLibrary;
using WinLibrary.Graphic;

namespace Game.Scene
{
    public partial class SceneGame : SceneBase
    {
        private readonly GameWorldSystem gameWorld;

        private readonly ZoomableTileMapSprites<TileMapSprites> zoomableTileMapSprites;
        private readonly TileMapSprites.MapSpritesInfo mapSpritesInfo;

        private readonly UIPlayerInfoPanel uiPlayerInfoPanel;
        private readonly UITileInfoPanel uiTileInfoPanel;

        private readonly DefaultStatus tileMapStatus;

        private Camera camera => gameWorld.camera;

        public TileMapSprites tileMap => zoomableTileMapSprites.tileMapSprites;

        public SceneGame(GameSystem gs, GameWorldSystem gw) : base(gs)
        {
            gameWorld = gw;

            tileMapStatus = new DefaultStatus(this);

            mapSpritesInfo = new TileMapSprites.MapSpritesInfo(gameWorld);

            zoomableTileMapSprites = new ZoomableTileMapSprites<TileMapSprites>();

            addChild(zoomableTileMapSprites);

            uiPlayerInfoPanel = new UIPlayerInfoPanel(gameSystem, gameWorld, new Point(formMain.Width - 540, 30));
            uiTileInfoPanel = new UITileInfoPanel(gameSystem, new Point(30, formMain.Height - 108));

            addChild(uiPlayerInfoPanel);
            addChild(uiTileInfoPanel);

            loadMap();
        }

        public override void start()
        {
            switchStatus(tileMapStatus);
        }

        public override void finish()
        {
            tileMapStatus.finish();
        }

        private void loadMap()
        {
            var msi = mapSpritesInfo;

            zoomableTileMapSprites.setTileMap(new List<TileMapSprites>()
            {
                new TileMapViewSprites(gameSystem, gameWorld, msi, false),
                new TileMapDetailSprites(gameSystem, gameWorld, msi, false)
            });
        }
        
        public void refreshTileInfo()
        {
            var gw = gameWorld;
            var gm = gw;
            var tm = gm.tileMap;

            var cursorPos = tileMap.cursorPosition;

            if (tm.isOutOfBounds(cursorPos))
            {
                uiTileInfoPanel.setText(null, null, null);

                return;
            }

            var t = tm[cursorPos];
            var mt = gm.masterData.tileMapTerrain;

            mt.TryGetValue(t.terrainSurface ?? t.terrain, out var tt); ;

            uiTileInfoPanel.setText(tt.name, null, null);
        }

        public void zoomMap(MouseEventArgs e)
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
    }

    public partial class SceneGame : SceneBase
    {
        public class Status : GameObject
        {
            protected SceneGame scene;

            public Status(SceneGame s) => scene = s;

            public virtual void setDrawMode(DrawMode dm)
            {

            }

            protected void onPointerButtonClicked() => setDrawMode(DrawMode.pointer);

            protected void onBrushButtonClicked() => setDrawMode(DrawMode.brush);

            protected void onRectangleButtonClicked() => setDrawMode(DrawMode.rectangle);

            public enum DrawMode
            {
                pointer,
                brush,
                rectangle,
            }
        }

        public class DefaultStatus : Status
        {
            private readonly PointerStatus pointerStatus;
            private readonly DrawTileStatus drawTileStatus;
            private readonly DrawTileRectangleStatus drawTileRectangleStatus;

            private Status currentStatus;

            public DefaultStatus(SceneGame s) : base(s)
            {
                pointerStatus = new PointerStatus(this);
                drawTileStatus = new DrawTileStatus(this);
                drawTileRectangleStatus = new DrawTileRectangleStatus(this);
            }

            public override void start()
            {
                setDrawMode(DrawMode.pointer);
            }

            public override void finish()
            {
            }

            public override void mouseMoved(MouseEventArgs e)
            {
                scene.refreshTileInfo();
            }

            public override void mouseWheelScrolled(MouseEventArgs e)
            {
                scene.zoomMap(e);
            }

            public override void setDrawMode(DrawMode dm)
            {
                if (currentStatus != null) children.Remove(currentStatus);

                switch (dm)
                {
                    case DrawMode.pointer: addStatus(pointerStatus); break;
                    case DrawMode.brush: addStatus(drawTileStatus); break;
                    case DrawMode.rectangle: addStatus(drawTileRectangleStatus); break;
                }
            }

            private void addStatus(Status s) => addChild(currentStatus = s);

            public class Status : GameObject
            {
                protected DefaultStatus gameStatus;

                protected SceneGame scene => gameStatus.scene;

                protected GameSystem gameSystem => scene.gameSystem;

                protected GameWorldSystem gameWorld => scene.gameWorld;

                protected GameWorld gameWorldData => gameWorld;

                protected TileMap tileMap => gameWorldData.tileMap;

                public Status(DefaultStatus s) => gameStatus = s;
            }

            public class PointerStatus : Status
            {
                public PointerStatus(DefaultStatus s) : base(s)
                {
                }

                public override void mouseDragging(MouseEventArgs e, Point p)
                {
                    gameStatus.scene.camera.dragCamera(p);
                }
            }

            public class DrawTileStatus : Status
            {
                public DrawTileStatus(DefaultStatus s) : base(s)
                {
                }

                public override void mouseReleased(MouseEventArgs e) => draw(e);

                public override void mouseDragging(MouseEventArgs e, Point p) => draw(e);

                private static void draw(MouseEventArgs e)
                {
                    //var p = gameStatus.tileMap.getTileLocation(e);

                    //if (tileMap.isOutOfBounds(p)) return;

                    //switch (scene.drawContent)
                    //{
                    //    case DrawContent.terrain:

                    //        var tid = (byte)scene.drawContentId;

                    //        var t = scene.gameWorld.masterData.mainTileMapTerrain[tid];

                    //        tileMap.setTerrain(p, tid, t.isSurface);

                    //        if (!t.isSurface) tileMap.terrainSurface.Remove(tileMap.getIndex(p));

                    //        gameStatus.mainMapSpritesInfo.resetTileFlag(p);

                    //        break;
                    //}
                }
            }

            public class DrawTileRectangleStatus : Status
            {
                public Point? startPoint;
                public SpriteRectangle selector;

                public DrawTileRectangleStatus(DefaultStatus s) : base(s)
                {
                    selector = new SpriteRectangle()
                    {
                        color = Color.White,
                        boundSize = 1,
                    };
                }

                public override void mousePressed(MouseEventArgs e)
                {
                    if (gameWorldData.tileMap.isOutOfBounds(scene.tileMap.cursorPosition)) return;

                    startPoint = e.Location;

                    selector.position = e.Location;
                    selector.size = new Size(1, 1);
                }

                public override void mouseReleased(MouseEventArgs e)
                {
                    //if (startPoint == null) return;

                    //startPoint = null;

                    //switch (scene.drawContent)
                    //{
                    //    case DrawContent.terrain:

                    //        var tlp = selector.position;
                    //        var trp = new Point(selector.position.X + selector.size.Width, selector.position.Y);
                    //        var blp = new Point(selector.position.X, selector.position.Y + selector.size.Height);

                    //        var tl = gameStatus.tileMap.getTileLocation(tlp);
                    //        var tr = gameStatus.tileMap.getTileLocation(trp);
                    //        var bl = gameStatus.tileMap.getTileLocation(blp);

                    //        var tm = gameWorld.mainTileMap;

                    //        tm.checkBound(ref tl);
                    //        tm.checkBound(ref tr);
                    //        tm.checkBound(ref bl);

                    //        ++tr.x;
                    //        ++bl.y;

                    //        var width = tr.x - tl.x;
                    //        var height = bl.y - tl.y;
                    //        var tid = (byte)scene.drawContentId;
                    //        var t = scene.gameWorld.masterData.mainTileMapTerrain[tid];

                    //        tm.eachRectangle(tl, new TileMap.Size(height, width), o =>
                    //        {
                    //            tileMap.setTerrain(o, tid, t.isSurface);

                    //            if (!t.isSurface) tileMap.terrainSurface.Remove(tileMap.getIndex(o));

                    //            gameStatus.mainMapSpritesInfo.resetTileFlag(o);
                    //        });

                    //        break;
                    //}

                    //selector.size = Size.Empty;
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
        }
    }
}
