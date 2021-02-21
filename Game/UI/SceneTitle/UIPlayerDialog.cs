using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UIPlayerDialog : UIEditableListViewDialog
    {
        public string name => listView.FocusedItem?.Tag as string;

        public UIPlayerDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_game_world).setCenter(true);

            listView.addColumn(w.name);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            addConfirmButton(w.ok);
        }
    }
}
