using Library;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace WinLibrary.UI
{
    public abstract class UIWindow<TWording> : Form where TWording : IWording
    {
        protected IGameSystem<TWording> gameSystem;

        protected FlowLayoutPanel panel = new FlowLayoutPanel();

        protected TWording w => gameSystem.wording;

        public UIWindow(IGameSystem<TWording> gs)
        {
            gameSystem = gs;

            Owner = gs.formGame;

            panel.init().setAutoSizeP().addTo(this);

            this.setAutoSizeF().setCenter();
        }

        protected T addMessage<T>(string text) where T : UIWindow<TWording>
        {
            new Label() { Margin = new Padding(20) }.init(text).setAutoSize().setMiddleCenter().addTo(panel);

            return (T)this;
        }
    }
}
