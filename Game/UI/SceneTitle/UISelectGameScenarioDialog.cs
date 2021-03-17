using Core;
using Core.Code;
using Game.Extension;
using System.Collections.Generic;
using System.Drawing;
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

        private RadioButton[] rbStartModeList;

        private Dictionary<string, GameScenarioInfo> gameScenarioInfoMap = new Dictionary<string, GameScenarioInfo>();

        public GameScenarioInfo selectedGameScenario
            => listView.FocusedItem != null && gameScenarioInfoMap.TryGetValue(listView.FocusedItem.Tag as string, out var value)
            ? value
            : null;

        public ScenarioMode checkedStartMode => (ScenarioMode)rbStartModeList.Single(o => o.Checked).Tag;

        public UISelectGameScenarioDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_game_scenario).setCenter(true);

            var tlpRoot = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                MinimumSize = new Size(640, 480)
            }.addRowStyle(40).addRowStyle(60).addTo(panel);

            var tlpTop = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
            }.addColumnStyle(80).addColumnStyle(20).addTo(tlpRoot);

            var tlpBottom = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
            }.addColumnStyle(60).addColumnStyle(40).addTo(tlpRoot);

            (GroupBox gb, Panel p) createGroupBox(string text, Panel plRoot, Panel p = null)
            {
                var gb = new GroupBox()
                {
                    Text = text,
                    Dock = DockStyle.Fill,
                }.addTo(plRoot);

                if (p == null) p = new Panel();

                p.Dock = DockStyle.Fill;

                p.addTo(gb);

                return (gb, p);
            }

            var (gb, p) = createGroupBox(w.thumbnail, tlpTop);

#warning TODO thumbnail
            new Label().init("thumbnail").addTo(p);

            var flp = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.TopDown
            };

            RadioButton addStartMode(string text, ScenarioMode sm, bool isChecked, bool isEnabled, string introductionText)
            {
                var rb = new RadioButton()
                {
                    Text = text,
                    Tag = sm,
                    Checked = isChecked,
                    Enabled = isEnabled,
                }.addTo(flp);

                new ToolTip().SetToolTip(rb, introductionText);

                return rb;
            }

            var gm = gs.currentGameMode;

            rbStartModeList = new[]
            {
                addStartMode(w.scenario_mode.story, ScenarioMode.story, gm == GameMode.personal, gm == GameMode.personal, w.scenario_mode.story_introduction),
                addStartMode(w.scenario_mode.open, ScenarioMode.open, gm != GameMode.personal, true, w.scenario_mode.open_introduction),
            };

            (gb, p) = createGroupBox(w.start_mode, tlpTop, flp);

            (gb, p) = createGroupBox(w.game_world, tlpBottom);

            listView
                .init()
                .addColumn(w.name)
                .addTo(p);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            (gb, p) = createGroupBox(w.introduction, tlpBottom);

            lbIntroduction.Width = 240;
            lbIntroduction.addTo(p);

            listView.SelectedIndexChanged += (s, e)
                => lbIntroduction.Text = selectedGameScenario?.introduction ?? string.Empty;

            addConfirmButtons();
        }

        public void setData(IEnumerable<GameScenarioInfo> list)
        {
            gameScenarioInfoMap = list.ToDictionary(o => o.name);

            listView.setData(list.toGameScenarioInfoList());
        }
    }
}
