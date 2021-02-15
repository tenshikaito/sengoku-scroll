using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UISelectGameScenarioDialog : UIListViewDialog
    {
        public Action introductionButtonClicked;

        public UISelectGameScenarioDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.single_player_game).setCenter(true);

            listView
                .addColumn(w.name);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            addButton(w.introduction, () => introductionButtonClicked?.Invoke());

            addConfirmButtons();
        }

        public void setData(List<GameScenarioInfo> list) => setData(list.Select(o => new ListViewItem()
        {
            Tag = o.code,
            Text = o.name
        }.addColumn(o.name)).ToArray());
    }
}
