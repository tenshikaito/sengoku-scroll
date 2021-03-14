using Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.Extension
{
    public static class UIExtension
    {
        public static ListViewItem[] toGameWorldInfoList(this IEnumerable<GameWorldInfo> list)
            => list.Select(o => new ListViewItem()
            {
                Tag = o.name,
                Text = o.name
            }.addColumn($"{o.width}x{o.height}")).ToArray();

        public static ListViewItem[] toGameScenarioInfoList(this IEnumerable<GameScenarioInfo> list)
            => list.Select(o => new ListViewItem()
            {
                Tag = o.code,
                Text = o.name
            }.addColumn(o.name)).ToArray();
    }
}
