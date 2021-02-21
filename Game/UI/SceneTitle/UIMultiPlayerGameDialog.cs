using Core.Network;
using Game.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneTitle
{
    public class UIMultiPlayerGameDialog : UIEditableListViewDialog
    {
        public Action refreshButtonClicked;

        public UIMultiPlayerGameDialog(GameSystem gs) : base(gs)
        {
            this.setCommandWindow(w.scene_title.select_game_world).setCenter(true);

            listView.addColumn(w.name)
                .addColumn(w.ip)
                .addColumn(w.game_world)
                .addColumn(w.status);

            listView.DoubleClick += (s, e) => btnOk.PerformClick();

            addButton(w.refresh, () => refreshButtonClicked?.Invoke());

            addConfirmButtons();
        }

        public void setData(List<ServerInfo> list) => listView.setData(list.Select(o => new ListViewItem()
        {
            Tag = o.code,
            Text = o.name
        }.addColumn($"{o.ip}:{o.port}").addColumn(w.symbol_none).addColumn(w.symbol_none)).ToArray());

        public void refresh(IDictionary<string, TestServerData> map)
        {
            listView.Items.Cast<ListViewItem>().ToList().ForEach(o =>
            {
                var code = (string)o.Tag;

                if (map.TryGetValue(code, out var value))
                {
                    o.SubItems[2].Text = value?.gameWorldName ?? w.symbol_none;
                    o.SubItems[3].Text = value == null ? w.disconnected : value.ping + "ms";
                }
            });
        }
    }
}
