﻿using Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace WinLibrary.UI
{
    public class UIListViewDialog<TWording> : UIConfirmDialog<TWording> where TWording : IWording
    {
        protected ListView listView;
        protected TableLayoutPanel pButtons;

        public string selectedValue => listView.FocusedItem == null ? null : (string)listView.FocusedItem.Tag;

        public UIListViewDialog(IGameSystem<TWording> gs) : base(gs)
        {
            var tlp = new TableLayoutPanel()
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                MinimumSize = new Size(640, 480)
            }.addColumnStyle(80).addColumnStyle(20).addTo(panel);

            listView = new ListView().init().addTo(tlp);

            pButtons = new TableLayoutPanel()
            {
                ColumnCount = 1,
                RowCount = 1,
                Dock = DockStyle.Top
            }.setAutoSizeP().addTo(tlp);

            listView.autoResizeColumns();
        }

        protected Button addButton(string text, Action onButtonClicked)
            => new Button().init(text, onButtonClicked).addTo(pButtons);

        public void setData(List<string> list) => setData(list.Select(o => new ListViewItem()
        {
            Tag = o,
            Text = o
        }).ToArray());

        protected void setData(ListViewItem[] list)
        {
            listView.BeginUpdate();
            listView.Items.Clear();
            listView.Items.AddRange(list);
            listView.autoResizeColumns();
            listView.selectFirstRow();
            listView.EndUpdate();
        }
    }
}