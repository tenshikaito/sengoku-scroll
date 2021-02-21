using Library;
using System;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace WinLibrary.UI
{
    public class UICommandWindow<TWording> : UIWindow<TWording> where TWording : IWording
    {
        public Action cancelButtonClicked;

        public UICommandWindow(IGameSystem<TWording> gs) : base(gs)
        {
        }

        protected void initCommandWindow(string title)
            => this.setCommandWindow(title).setAutoSizeF().setCenter();

        protected void addCommandButton(string text, Action callback)
            => new Button().init(text, callback).setAutoSize().addTo(panel);

        protected void addCancelButton(string text = null)
            => addCommandButton(text ?? w.cancel, () => cancelButtonClicked?.Invoke());
    }
}
