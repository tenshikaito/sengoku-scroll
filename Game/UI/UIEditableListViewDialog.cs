using Core;
using WinLibrary;
using WinLibrary.UI;

namespace Game.UI
{
    public class UIEditableListViewDialog : UIEditableListViewDialog<GameWording>
    {
        public UIEditableListViewDialog(IGameSystem<GameWording> gs) : base(gs)
        {
        }
    }
}
