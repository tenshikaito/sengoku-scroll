using Core;
using Game.Extension;
using System.Collections.Generic;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UIEditGameWorldDialog : UIEditableListViewDialog
    {
        public UIEditGameWorldDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.edit_map).setCenter(true);

            listView.addColumn(w.name);

            btnEdit.Text = w.edit_game_world;

            listView.DoubleClick += (s, e) => btnEdit.PerformClick();

            addConfirmButton(w.close);
        }

        public void setData(IEnumerable<GameWorldInfo> list) => listView.setData(list.toGameWorldInfoList());
    }
}
