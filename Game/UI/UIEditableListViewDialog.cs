using Core;

namespace Game.UI
{
    public class UIEditableListViewDialog : WinLibrary.UI.UIEditableListViewDialog
    {
        protected new GameSystem gameSystem { get; }

        protected new GameWording w => gameSystem.gameWording;

        public UIEditableListViewDialog(GameSystem gs) : base(gs)
        {
            gameSystem = gs;
        }
    }
}
