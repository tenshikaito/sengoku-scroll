using Core;

namespace Game.UI
{
    public class UIListViewDialog : WinLibrary.UI.UIListViewDialog
    {
        protected new GameSystem gameSystem { get; }

        protected new GameWording w => gameSystem.gameWording;

        public UIListViewDialog(GameSystem gs) : base(gs)
        {
            gameSystem = gs;
        }
    }
}
