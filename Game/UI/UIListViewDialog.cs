using Core;
using WinLibrary;
using WinLibrary.UI;

namespace Game.UI
{
    public class UIListViewDialog : UIListViewDialog<GameWording>
    {
        public UIListViewDialog(IGameSystem<GameWording> gs) : base(gs)
        {
        }
    }
}
