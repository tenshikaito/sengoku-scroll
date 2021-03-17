using System;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace WinLibrary.UI
{
    /// <summary>
    /// 提供带有应用按钮的确认对话框
    /// </summary>
    public class UISaveableConfirmDialog : UIConfirmDialog
    {
        public Action applyButtonClicked;

        protected Button btnApply = new Button();

        public UISaveableConfirmDialog(IGameSystem gs) : base(gs)
        {
            CancelButton = null;

            btnApply.Click += (s, e) => applyButtonClicked?.Invoke();
        }

        protected UIDialog addSaveableConfirmButtons()
        {
            var p = new FlowLayoutPanel().init(FlowDirection.RightToLeft).setAutoSizeP().addTo(panel);

            btnApply.addTo(p).Text = w.apply;
            btnCancel.addTo(p).Text = w.cancel;
            btnOk.addTo(p).Text = w.ok;

            return this;
        }
    }
}
