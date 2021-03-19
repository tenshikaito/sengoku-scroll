using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.Helper
{
    public static class UIHelper
    {
        public static (GroupBox gb, Panel p) createGroupBox(string text, Panel plRoot, Panel p = null)
        {
            var gb = new GroupBox()
            {
                Text = text,
                Dock = DockStyle.Fill,
            }.addTo(plRoot);

            if (p == null) p = new Panel();

            p.Dock = DockStyle.Fill;

            p.addTo(gb);

            return (gb, p);
        }

    }
}
