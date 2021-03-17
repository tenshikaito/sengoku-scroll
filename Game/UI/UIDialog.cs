using Core;
using WinLibrary;

namespace Game.UI
{
    public class UIDialog : WinLibrary.UI.UIDialog
    {
        protected new GameSystem gameSystem { get; }

        protected new GameWording w => gameSystem.gameWording;

        public UIDialog(GameSystem gs) : base(gs)
        {
            gameSystem = gs;
        }

        public UIDialog(IGameSystem gs, string title, string text) : base(gs, title, text)
        {
        }
    }
}
