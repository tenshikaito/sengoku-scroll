using Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;
using static Game.Helper.UIHelper;

namespace Game.UI.SceneTitle
{
    public class UISelectCharacterDialog : UIListViewDialog
    {
        private PictureBox pbThumbnail = new PictureBox();
        private PictureBox pbPortrait = new PictureBox();
        private Label lbName = new Label();
        private Label lbIntroduction = new Label();

        public UISelectCharacterDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_character).setCenter(true);

            var tlp = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                MinimumSize = new Size(640, 480)
            }
            .addRowStyle(20).addRowStyle(40).addColumnStyle(40)
            .addColumnStyle(60).addColumnStyle(40)
            .addTo(panel);

            var flp = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
            };

            var (gb, p) = createGroupBox(null, flp);

            tlp.SetColumnSpan(gb, 3);

            gb.addTo(tlp);

            addRadioButton(w.character, flp, onCharacterRadioButtonClicked);
            addRadioButton(w.terrain, flp, onForceRadioButtonClicked);
            addRadioButton(w.stronghold, flp, onStrongholdRadioButtonClicked);

            // TODO Bitmap
            new Label().init("thumbnail").addTo(p);

            tlp.SetColumnSpan(gb, 2);

            (gb, p) = createGroupBox(w.game_world, tlp);

            listView
                .init()
                .addColumn(w.name)
                .addTo(p);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            (gb, p) = createGroupBox(w.introduction, tlp);

            addConfirmButtons();
        }

        private RadioButton addRadioButton(string text, Panel plRoot, EventHandler callback)
        {
            var rbForce = new RadioButton()
            {
                Text =text,
            }.addTo(plRoot);

            rbForce.Click += callback;

            return rbForce;
        }

        private void onCharacterRadioButtonClicked(object sender, EventArgs e)
        {
        }

        private void onForceRadioButtonClicked(object sender, EventArgs e)
        {
        }

        private void onStrongholdRadioButtonClicked(object sender, EventArgs e)
        {
        }

        public void setData(IEnumerable<GameScenarioInfo> list) => listView.setData(list.Select(o => new ListViewItem()
        {
            Tag = o.code,
            Text = o.name
        }.addColumn(o.name)).ToArray());
    }
}
