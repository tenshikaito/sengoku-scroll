using Core;
using Game.Extension;
using System;
using System.Collections.Generic;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UISelectGameWorldDialog : UIListViewDialog
    {
        public Action introductionButtonClicked;

        public UISelectGameWorldDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.single_player_game).setCenter(true);

            listView
                .addColumn(w.name)
                .addColumn(w.map_size);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            addButton(w.introduction, () => introductionButtonClicked?.Invoke());

            addConfirmButtons();
        }

        public void setData(IEnumerable<GameWorldInfo> list) => setData(list.toGameWorldInfoList());
    }
}
