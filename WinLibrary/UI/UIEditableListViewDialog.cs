using Library;
using System;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace WinLibrary.UI
{
    public class UIEditableListViewDialog<TWording> : UIListViewDialog<TWording> where TWording : IWording
    {
        protected Button btnAdd;
        protected Button btnEdit;
        protected Button btnRemove;

        public Action addButtonClicked;
        public Action<string> removeButtonClicked;
        public Action<string> editButtonClicked;

        public UIEditableListViewDialog(IGameSystem<TWording> gs) : base(gs)
        {
            var p = pButtons;

            btnAdd = new Button() { Dock = DockStyle.Fill }.init(w.add, () => addButtonClicked?.Invoke()).addTo(p);
            btnEdit = new Button() { Dock = DockStyle.Fill }.init(w.edit, () =>
            {
                var o = listView.FocusedItem;
                if (o == null) return;
                editButtonClicked?.Invoke((string)o.Tag);
            }).addTo(p);
            btnRemove = new Button() { Dock = DockStyle.Fill }.init(w.remove, () =>
            {
                var o = listView.FocusedItem;
                if (o == null) return;
                removeButtonClicked?.Invoke((string)o.Tag);
            }).addTo(p);
        }
    }
}
