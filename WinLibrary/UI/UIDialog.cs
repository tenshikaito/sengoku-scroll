using Library;
using System;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace WinLibrary.UI
{
    public class UIDialog<TWording> : UIWindow<TWording> where TWording : IWording
    {
        public Action okButtonClicked;

        protected Button btnOk = new Button();

        public bool isBtnOkEnabled
        {
            get => btnOk.Enabled;
            set => btnOk.Enabled = value;
        }

        public UIDialog(IGameSystem<TWording> gs) : base(gs)
        {
            AcceptButton = btnOk;
            CancelButton = btnOk;

            btnOk.Click += (s, e) => okButtonClicked?.Invoke();
        }

        public UIDialog(IGameSystem<TWording> gs, string title, string text) : this(gs)
        {
            this.setCommandWindow(title).addMessage<UIDialog<TWording>>(text);

            addConfirmButton();

            okButtonClicked = Close;
        }

        protected UIDialog<TWording> addConfirmButton(string okButtonText = null)
        {
            btnOk.addTo(panel);
            btnOk.Text = okButtonText ?? w.ok;
            btnOk.Anchor = AnchorStyles.None;

            return this;
        }
    }
}
