using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UIGameWorldDetailDialog : UIConfirmDialog
    {
        private readonly TextBox tbName;
        private readonly TextBox tbWidth;
        private readonly TextBox tbHeight;

        public (string name, string width, string height) value => (tbName.Text, tbWidth.Text, tbHeight.Text);

        public UIGameWorldDetailDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.add);

            var p = new TableLayoutPanel()
            {
                ColumnCount = 2
            }.setAutoSizeP().addTo(panel);

            new Label() { Text = w.name }.setRightCenter().setAutoSize().addTo(p);

            tbName = new TextBox() { Text = "test" }.addTo(p);

            new Label() { Text = w.width }.setRightCenter().setAutoSize().addTo(p);

            tbWidth = new TextBox() { Text = "100" }.addTo(p);

            new Label() { Text = w.height }.setRightCenter().setAutoSize().addTo(p);

            tbHeight = new TextBox() { Text = "100" }.addTo(p);

            addConfirmButtons();
        }
    }
}
