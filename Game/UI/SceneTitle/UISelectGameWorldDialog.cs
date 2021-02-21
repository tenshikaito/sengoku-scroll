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
            }.addTo(panel);

            new Label().init("thumbnail").addTo(tlp);

            var gb = new GroupBox()
            {
                Dock = DockStyle.Fill
            }.addTo(tlp);

            new Label().init(string.Empty).addTo(gb);

            var listView = new ListView().init()
                .addColumn(w.name)
                .addColumn(w.map_size)
                .addTo(tlp);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            lbIntroduction.Width = 240;
            lbIntroduction.addTo(tlp);

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
