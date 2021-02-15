using Core;
using WinLibrary;
using WinLibrary.UI;

namespace Game.UI
{
    public class UIConfirmDialog : UIConfirmDialog<GameWording>
    {
        public UIConfirmDialog(IGameSystem<GameWording> gs) : base(gs)
        {
        }

        public UIConfirmDialog(IGameSystem<GameWording> gs, string title, string text) : base(gs, title, text)
        {
        }
    }
}
