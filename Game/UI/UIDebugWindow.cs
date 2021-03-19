using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI
{
    public class UIDebugWindow : UIWindow
    {
        private TextBox tbOutput;
        private TextBox tbInput;

        public UIDebugWindow(GameSystem gs, Func<string, Task> inputText) : base(gs)
        {
            var tlp = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                MinimumSize = new System.Drawing.Size(320, 240),
            }.addRowStyle(80).addRowStyle(20).addTo(panel);

            tbOutput = new TextBox()
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Multiline = true
            }.addTo(tlp);

            tbInput = new TextBox()
            {
                Dock = DockStyle.Fill,
                Multiline = true
            }.addTo(tlp);

            tbInput.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    var text = tbInput.Text;

                    tbInput.ResetText();

                    _ = inputText(text.Trim());
                }
            };
        }

        protected override void OnShown(EventArgs e)
        {
            tbInput.Focus();
        }

        public void appendOutputText(string text) => BeginInvoke(new Action(() =>
        {
            tbOutput.AppendText(text);
            tbOutput.AppendText(Environment.NewLine);
        }));

        public void clearOutputText() => BeginInvoke(new Action(() => tbOutput.Text = string.Empty));
    }
}
