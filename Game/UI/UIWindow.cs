using Core;
using WinLibrary;
using WinLibrary.UI;

namespace Game.UI
{
    public abstract class UIWindow : UIWindow<GameWording>
    {
        protected UIWindow(IGameSystem<GameWording> gs) : base(gs)
        {
        }
    }
}
