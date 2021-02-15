using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UIPlayerDetailDialog : UIConfirmDialog
    {
        private readonly TextBox tbName;

        public string name => tbName.Text;

        public UIPlayerDetailDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.detail);

            var p = new TableLayoutPanel()
            {
                ColumnCount = 2
            }.setAutoSizeP().addTo(panel);

            new Label() { Text = w.name }.setRightCenter().setAutoSize().addTo(p);

            tbName = new TextBox() { Text = "player" }.addTo(p);

            addConfirmButtons();
        }
    }
}
