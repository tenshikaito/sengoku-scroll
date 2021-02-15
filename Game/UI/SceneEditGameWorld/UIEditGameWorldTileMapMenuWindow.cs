using Core.Model;
using Game.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneEditGameWorld
{
    public partial class UIEditGameWorldTileMapMenuWindow : UIWindow
    {
        private readonly ListView lvTerrain;

        public UIEditGameWorldTileMapMenuWindow(
            GameSystem gs,
            Action pointer,
            Action brush,
            Action rectangle,
            Action fill,
            Action<int> onTerrainSelected) : base(gs)
        {
            this.setCommandWindow(w.main_tile_map).setAutoSizeF().setCenter();

            StartPosition = FormStartPosition.Manual;
            Location = new Point(gs.formMain.Location.X, gs.formMain.Location.Y + 240);

            var list = new List<CheckBox>();
            var p = new FlowLayoutPanel().init(FlowDirection.LeftToRight).setAutoSizeP().addTo(panel);

            var cbPointer = new CheckBox();

            list.Add(cbPointer.init(w.pointer, true, () =>
            {
                list.ForEach(o => o.Checked = false);
                cbPointer.Checked = true;
                pointer();
            }).setButtonStyle().setAutoSize().addTo(p));

            var cbBrush = new CheckBox();

            list.Add(cbBrush.init(w.scene_edit_game_world.brush, false, () =>
            {
                list.ForEach(o => o.Checked = false);
                cbBrush.Checked = true;
                brush();
            }).setButtonStyle().setAutoSize().addTo(p));

            var cbRectangle = new CheckBox();

            list.Add(cbRectangle.init(w.scene_edit_game_world.rectangle, false, () =>
            {
                list.ForEach(o => o.Checked = false);
                cbRectangle.Checked = true;
                rectangle();
            }).setButtonStyle().setAutoSize().addTo(p));

            var cbFill = new CheckBox();

            list.Add(cbFill.init(w.scene_edit_game_world.fill, false, () =>
            {
                list.ForEach(o => o.Checked = false);
                cbFill.Checked = true;
                fill();
            }).setButtonStyle().setAutoSize().addTo(p));

            p = new FlowLayoutPanel().init(FlowDirection.LeftToRight).setAutoSizeP().addTo(panel);

            var tc = new TabControl() { MinimumSize = new Size(360, 540) }.init().addTo(panel);

            var tp = new TabPage().init(w.terrain.text).addTo(tc);

            lvTerrain = new ListView()
            {
                Scrollable = true
            }.init().addTo(tp);

            lvTerrain.addColumn(w.name)
                .addColumn(w.terrain_image.text)
                .addColumn(w.terrain.is_surface)
                .autoResizeColumns();

            lvTerrain.SelectedIndexChanged += (s, e) =>
            {
                if (lvTerrain.FocusedItem == null) return;

                onTerrainSelected((int)lvTerrain.FocusedItem.Tag);
            };
        }

        public void setTerrain(List<Terrain> data, Dictionary<int, TerrainImage> terrainImage)
        {
            lvTerrain.Items.Clear();

            lvTerrain.BeginUpdate();

            lvTerrain.Items.AddRange(data.Select(o =>
            {
                var lvi = new ListViewItem()
                {
                    Tag = o.id,
                    Text = o.name
                };

                lvi.addColumn(terrainImage.TryGetValue(o.imageId, out var ti) ? ti.name : w.symbol_none)
                .addColumn(o.isSurface.getSymbol(w));

                return lvi;
            }).ToArray());

            lvTerrain.autoResizeColumns();

            lvTerrain.EndUpdate();
        }
    }
}
