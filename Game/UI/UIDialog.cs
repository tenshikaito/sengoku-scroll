using Core;
using WinLibrary;
using WinLibrary.UI;

namespace Game.UI
{
    public class UIDialog : UIDialog<GameWording>
    {
        public UIDialog(IGameSystem<GameWording> gs) : base(gs)
        {
        }

        public UIDialog(IGameSystem<GameWording> gs, string title, string text) : base(gs, title, text)
        {
        }
    }
}
