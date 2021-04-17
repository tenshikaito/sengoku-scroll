using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI
{
    public class UIDebugWindow : UIWindow
    {
        private readonly TextBox tbOutput;
        private readonly TextBox tbInput;

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

        public UIDebugWindow appendOutputText(string text)
        {
            tbOutput.AppendText(text);

            return this;
        }

        public UIDebugWindow appendOutputNewLine()
        {
            tbOutput.AppendText(Environment.NewLine);

            return this;
        }

        public void invokeAppendOutputText(string text)
            => BeginInvoke(new Action(() => appendOutputText(text)));

        public void invokeAppendOutputNewLine()
            => BeginInvoke(new Action(() => appendOutputNewLine()));

        public void clearOutputText() => tbOutput.Clear();

        public void invokeClearOutputText() => BeginInvoke(new Action(() => tbOutput.Clear()));
    }
}
