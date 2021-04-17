using Core;
using Core.Code;
using Game.Extension;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;
using static Game.Helper.UIHelper;

namespace Game.UI.SceneTitle
{
    public class UISelectGameStageDialog : UIConfirmDialog
    {
        private readonly Label lbIntroduction = new Label();
        private readonly PictureBox pbThumbnail = new PictureBox();
        private readonly ListView listView = new ListView();

        private readonly RadioButton[] rbStartModeList;

        private Dictionary<string, MapInfo> gameStageInfoMap = new Dictionary<string, MapInfo>();

        public MapInfo selectedGameStage
            => listView.FocusedItem != null && gameStageInfoMap.TryGetValue(listView.FocusedItem.Tag as string, out var value)
            ? value
            : null;

        public StartMode checkedStartMode => (StartMode)rbStartModeList.Single(o => o.Checked).Tag;

        public UISelectGameStageDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_game_stage).setCenter(true);

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

            var (gb, p) = createGroupBox(w.thumbnail, tlpTop);

#warning TODO thumbnail
            new Label().init("thumbnail").addTo(p);

            var flp = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown
            };

            rbStartModeList = new[]
            {
                addStartMode(w.start_mode.scenario, StartMode.scenario, flp, true,w.start_mode.scenario_introduction ),
                addStartMode(w.start_mode.random, StartMode.random,  flp,false,w.start_mode.random_introduction),
                addStartMode(w.start_mode.creation, StartMode.creation,  flp,false,w.start_mode.creation_introduction)
            };

            (gb, p) = createGroupBox(w.start_mode, tlpTop, flp);

            (gb, p) = createGroupBox(w.game_stage, tlpBottom);

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
                => lbIntroduction.Text = selectedGameStage?.introduction ?? string.Empty;

            addConfirmButtons();
        }

        private RadioButton addStartMode(string text, StartMode sm, FlowLayoutPanel flp, bool isChecked, string introductionText)
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


        public void setData(IEnumerable<MapInfo> list)
        {
            gameStageInfoMap = list.ToDictionary(o => o.name);

            listView.setData(list.toGameStageInfoList());
        }
    }
}
