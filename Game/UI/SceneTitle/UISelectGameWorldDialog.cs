using Core;
using Game.Extension;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UISelectGameWorldDialog : UIConfirmDialog
    {
        private Label lbIntroduction = new Label();
        private PictureBox pbThumbnail = new PictureBox();
        private ListView listView = new ListView();

        private Dictionary<string, GameWorldInfo> gameWorldInfoMap = new Dictionary<string, GameWorldInfo>();

        public GameWorldInfo selectedGameWorld
            => gameWorldInfoMap.TryGetValue(listView.FocusedItem.Tag as string, out var value) ? value : null;

        public UISelectGameWorldDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_game_world).setCenter(true);

            var tlp = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                MinimumSize = new Size(640, 480)
            }.addRowStyle(40).addRowStyle(60).addColumnStyle(60).addColumnStyle(40).addTo(panel);

            (GroupBox gb, Panel p) createGroupBox(string text)
            {
                var gb = new GroupBox()
                {
                    Text = text,
                    Dock = DockStyle.Fill,
                }.addTo(tlp);

                var p = new Panel()
                {
                    Dock = DockStyle.Fill
                }.addTo(gb);

                return (gb, p);
            }

            var (gb, p) = createGroupBox(w.thumbnail);

            // TODO Bitmap
            new Label().init("thumbnail").addTo(p);

            tlp.SetColumnSpan(gb, 2);

            (gb, p) = createGroupBox(w.game_world);

            var listView = new ListView().init()
                .addColumn(w.name)
                .addColumn(w.map_size)
                .addTo(p);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            (gb, p) = createGroupBox(w.introduction);

            lbIntroduction.Width = 240;
            lbIntroduction.addTo(p);

            listView.SelectedIndexChanged += (s, e)
                => lbIntroduction.Text = selectedGameWorld?.introduction ?? string.Empty;

            addConfirmButtons();
        }

        public void setData(IEnumerable<GameWorldInfo> list)
        {
            gameWorldInfoMap = list.ToDictionary(o => o.name);

            listView.setData(list.toGameWorldInfoList());
        }
    }
}
