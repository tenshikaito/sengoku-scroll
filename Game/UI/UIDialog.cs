using WinLibrary;

namespace Game.UI
{
    public class UIDialog : WinLibrary.UI.UIDialog
    {
        public UIDialog(GameSystem gs) : base(gs)
        {
        }

        public UIDialog(GameSystem gs, string title, string text) : base(gs, title, text)
        {
        }
    }
}
