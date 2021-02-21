using Core;
using WinLibrary;
using WinLibrary.UI;

namespace Game.UI
{
    public abstract class UICommandWindow : UICommandWindow<GameWording>
    {
        protected UICommandWindow(IGameSystem<GameWording> gs) : base(gs)
        {
        }
    }
}
