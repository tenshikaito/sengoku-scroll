using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UISelectGameScenarioDialog : UIConfirmDialog
    {
        private Label lbIntroduction = new Label();
        private PictureBox pbThumbnail = new PictureBox();
        private ListView listView = new ListView();

        private Dictionary<string, GameScenarioInfo> gameScenarioInfoMap = new Dictionary<string, GameScenarioInfo>();

        public GameScenarioInfo selectedScenarioInfo
            => gameScenarioInfoMap.TryGetValue(listView.FocusedItem.Tag as string, out var value) ? value : null;

        public UISelectGameScenarioDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_game_scenario).setCenter(true);

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
                .addTo(tlp);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            lbIntroduction.Width = 240;
            lbIntroduction.addTo(tlp);

            listView.SelectedIndexChanged += (s, e)
                => lbIntroduction.Text = selectedScenarioInfo?.introduction ?? string.Empty;

            addConfirmButtons();
        }

        public void setData(IEnumerable<GameScenarioInfo> list) => listView.setData(list.Select(o => new ListViewItem()
        {
            Tag = o.code,
            Text = o.name
        }.addColumn(o.name)).ToArray());
    }
}
