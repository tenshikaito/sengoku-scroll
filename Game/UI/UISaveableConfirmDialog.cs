using Core;
using WinLibrary;
using WinLibrary.UI;

namespace Game.UI
{
    /// <summary>
    /// 提供带有应用按钮的确认对话框
    /// </summary>
    public class UISaveableConfirmDialog : UISaveableConfirmDialog<GameWording>
    {
        public UISaveableConfirmDialog(IGameSystem<GameWording> gs) : base(gs)
        {
        }
    }
}
