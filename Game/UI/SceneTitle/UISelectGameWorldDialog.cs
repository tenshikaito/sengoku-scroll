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
    public class UISelectGameWorldDialog : UIConfirmDialog
    {
        private Label lbIntroduction = new Label();
        private PictureBox pbThumbnail = new PictureBox();
        private ListView listView = new ListView();

        private RadioButton[] rbStartModeList;

        private Dictionary<string, GameWorldInfo> gameWorldInfoMap = new Dictionary<string, GameWorldInfo>();

        public GameWorldInfo selectedGameWorld
            => listView.FocusedItem != null && gameWorldInfoMap.TryGetValue(listView.FocusedItem.Tag as string, out var value)
            ? value
            : null;

        public StartMode checkedStartMode => (StartMode)rbStartModeList.Single(o => o.Checked).Tag;

        public UISelectGameWorldDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_game_world).setCenter(true);

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

            RadioButton addStartMode(string text, StartMode sm, bool isChecked, string introductionText)
            {
                var rb = new RadioButton()
                {
                    Text = text,
                    Tag = sm,
                    Checked = isChecked
                }.addTo(flp);

                new ToolTip().SetToolTip(rb, introductionText);

                return rb;
            }

            rbStartModeList = new[]
            {
                addStartMode(w.start_mode.scenario, StartMode.scenario, true,w.start_mode.scenario_introduction ),
                addStartMode(w.start_mode.random, StartMode.random, false,w.start_mode.random_introduction),
                addStartMode(w.start_mode.creation, StartMode.creation, false,w.start_mode.creation_introduction)
            };

            (gb, p) = createGroupBox(w.start_mode, tlpTop, flp);

            (gb, p) = createGroupBox(w.game_world, tlpBottom);

            listView
                .init()
                .addColumn(w.name)
                .addColumn(w.map_size)
                .addTo(p);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            (gb, p) = createGroupBox(w.introduction, tlpBottom);

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
