using Core;

namespace Game.UI
{
    public abstract class UICommandWindow : WinLibrary.UI.UICommandWindow
    {
        protected new GameSystem gameSystem { get; }

        protected new GameWording w => gameSystem.gameWording;

        protected UICommandWindow(GameSystem gs) : base(gs)
        {
            gameSystem = gs;
        }
    }
}
