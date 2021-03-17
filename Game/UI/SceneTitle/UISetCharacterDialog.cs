using Core;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UISetCharacterDialog : UIConfirmDialog
    {
        private Label lbIntroduction = new Label();
        private PictureBox pbThumbnail = new PictureBox();
        private ListView listView = new ListView();

        public UISetCharacterDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_game_scenario).setCenter(true);

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

            listView
                .init()
                .addColumn(w.name)
                .addTo(p);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            (gb, p) = createGroupBox(w.introduction);

            lbIntroduction.Width = 240;
            lbIntroduction.addTo(p);

            addConfirmButtons();
        }

        public void setData(IEnumerable<GameScenarioInfo> list) => listView.setData(list.Select(o => new ListViewItem()
        {
            Tag = o.code,
            Text = o.name
        }.addColumn(o.name)).ToArray());
    }
}
