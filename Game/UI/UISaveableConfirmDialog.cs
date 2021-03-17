using Core;

namespace Game.UI
{
    /// <summary>
    /// 提供带有应用按钮的确认对话框
    /// </summary>
    public class UISaveableConfirmDialog : WinLibrary.UI.UISaveableConfirmDialog
    {
        protected new GameSystem gameSystem { get; }

        protected new GameWording w => gameSystem.gameWording;

        public UISaveableConfirmDialog(GameSystem gs) : base(gs)
        {
            gameSystem = gs;
        }
    }
}
