using Core;

namespace Game.UI
{
    public abstract class UIWindow : WinLibrary.UI.UIWindow
    {
        protected new GameSystem gameSystem { get; }

        protected new GameWording w => gameSystem.gameWording;

        protected UIWindow(GameSystem gs) : base(gs)
        {
            gameSystem = gs;
        }
    }
}
