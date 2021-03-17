using System;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace WinLibrary.UI
{
    public class UIConfirmDialog : UIDialog
    {
        public Action cancelButtonClicked;

        protected Button btnCancel = new Button();

        public bool isBtnCancelEnabled
        {
            get => btnCancel.Enabled;
            set => btnCancel.Enabled = value;
        }

        public UIConfirmDialog(IGameSystem gs) : base(gs)
        {
            CancelButton = btnCancel;

            btnCancel.Click += (s, e) => cancelButtonClicked?.Invoke();
        }

        public UIConfirmDialog(IGameSystem gs, string title, string text) : this(gs)
        {
            this.setCommandWindow(title).addMessage<UIConfirmDialog>(text);

            addConfirmButtons();

            cancelButtonClicked = Close;
        }

        public UIConfirmDialog addConfirmButtons(string okButtonText = null, string cancelButtonText = null)
        {
            var tlp = new TableLayoutPanel()
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill
            }.addColumnStyle(50).addColumnStyle(50).setAutoSizeP().addTo(panel);

            btnOk.addTo(tlp);
            btnOk.Text = okButtonText ?? w.ok;
            btnOk.Anchor = AnchorStyles.Right;
            btnCancel.addTo(tlp);
            btnCancel.Text = cancelButtonText ?? w.cancel;
            btnCancel.Anchor = AnchorStyles.Left;

            return this;
        }
    }
}
