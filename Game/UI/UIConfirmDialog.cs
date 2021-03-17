using Core;

namespace Game.UI
{
    public class UIConfirmDialog : WinLibrary.UI.UIConfirmDialog
    {
        protected new GameSystem gameSystem { get; }

        protected new GameWording w => gameSystem.gameWording;

        public UIConfirmDialog(GameSystem gs) : base(gs)
        {
            gameSystem = gs;
        }

        public UIConfirmDialog(GameSystem gs, string title, string text) : base(gs, title, text)
        {
        }
    }
}
