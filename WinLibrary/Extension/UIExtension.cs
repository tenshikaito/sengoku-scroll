using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinLibrary.Extension
{
    public static class UIExtension
    {
        public static T addTo<T>(this T c, Control oc) where T : Control
        {
            oc.Controls.Add(c);

            return c;
        }

        public static T setAutoSizeF<T>(this T f) where T : Form
        {
            f.setAutoSize();
            f.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            return f;
        }

        public static T setCenter<T>(this T f, bool isForce = false) where T : Form
        {
            if (isForce)
            {
                var rect = SystemInformation.WorkingArea;
                f.Location = new Point((rect.Width - f.Width) / 2, (rect.Height - f.Height) / 2 - 20);
            }
            else
            {
                f.StartPosition = FormStartPosition.CenterScreen;
            }

            return f;
        }

        public static T init<T>(this T f, string title) where T : Form
        {
            f.Text = title;

            return f;
        }

        public static T setCommandWindow<T>(this T f, string title) where T : Form
        {
            f.Text = title;
            f.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            f.SizeGripStyle = SizeGripStyle.Hide;

            f.hideCommandButton();

            return f;
        }

        public static T setAutoSize<T>(this T c) where T : Control
        {
            c.AutoSize = true;

            return c;
        }

        public static T refreshListViewOnClick<T>(this T c, ListView lv, Action<ListView> focus) where T : Control
        {
            c.Click += new EventHandler((s, e) => focus(lv));

            return c;
        }

        public static T setAutoSizeP<T>(this T p) where T : Panel
        {
            p.setAutoSize();
            p.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            return p;
        }

        public static T hideCommandButton<T>(this T f) where T : Form
        {
            f.ControlBox = false;
            f.MinimizeBox = false;
            f.MaximizeBox = false;

            return f;
        }

        public static FlowLayoutPanel init(this FlowLayoutPanel p, FlowDirection fd = FlowDirection.TopDown)
        {
            p.Dock = DockStyle.Fill;
            p.FlowDirection = fd;

            return p;
        }

        public static Button init(this Button b, string text, Action onClick, bool isFill = true)
        {
            if (isFill) b.Dock = DockStyle.Fill;
            b.Text = text;
            b.Click += (s, e) => onClick();

            return b;
        }

        public static Button setAutoSize(this Button b)
        {
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            return b;
        }

        public static TextBox setMultiLine(this TextBox tb)
        {
            tb.Multiline = true;

            return tb;
        }

        public static Label init(this Label l, string text)
        {
            l.Text = text;

            return l;
        }

        public static Label setLeftCenter(this Label l)
        {
            l.Dock = DockStyle.Left;
            l.TextAlign = ContentAlignment.MiddleLeft;

            return l;
        }

        public static Label setMiddleCenter(this Label l)
        {
            l.TextAlign = ContentAlignment.MiddleCenter;

            return l;
        }

        public static Label setRightCenter(this Label l)
        {
            l.Dock = DockStyle.Right;
            l.TextAlign = ContentAlignment.MiddleRight;

            return l;
        }

        public static CheckBox init(this CheckBox cb, string text = null, bool isChecked = false, Action onClick = null)
        {
            cb.Dock = DockStyle.Fill;
            cb.Text = text;
            cb.Checked = isChecked;
            if (onClick != null) cb.Click += (s, e) => onClick();

            return cb;
        }

        public static CheckBox setButtonStyle(this CheckBox cb)
        {
            cb.Appearance = Appearance.Button;

            return cb;
        }

        public static TabControl init(this TabControl tc)
        {
            tc.Dock = DockStyle.Fill;

            return tc;
        }

        public static TabPage init(this TabPage tp, string title)
        {
            tp.Text = title;

            return tp;
        }

        public static ListView init(this ListView lv)
        {
            lv.Dock = DockStyle.Fill;
            lv.FullRowSelect = true;
            lv.View = View.Details;

            return lv;
        }

        public static ListView addColumn(this ListView lv, string title, int width = 120, HorizontalAlignment ha = HorizontalAlignment.Center)
        {
            lv.Columns.Add(title, width, ha);

            return lv;
        }

        public static ListView autoResizeColumns(this ListView lv, int offset = 5)
        {
            lv.BeginUpdate();

            var list = lv.Columns.Cast<ColumnHeader>().ToList();

            list.ForEach(o =>
            {
                o.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);

                var widthHeader = o.Width;

                o.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

                if (widthHeader > o.Width) o.Width = widthHeader;
            });

            var last = list.LastOrDefault();

            if (last != null)
            {
                var sumWidth = list.Sum(o => o.Width);

                if (sumWidth < lv.Width) last.Width += lv.Width - sumWidth - offset;
            }

            lv.EndUpdate();

            return lv;
        }

        public static ListView selectFirstRow(this ListView lv)
        {
            if (lv.Items.Count > 0)
            {
                lv.Focus();
                lv.Items[0].Selected = true;
            }

            return lv;
        }

        public static TableLayoutPanel addRowStyle(this TableLayoutPanel tlp, float height, SizeType st = SizeType.Percent)
        {
            tlp.RowStyles.Add(new RowStyle(st, height));

            return tlp;
        }

        public static TableLayoutPanel addColumnStyle(this TableLayoutPanel tlp, float width, SizeType st = SizeType.Percent)
        {
            tlp.ColumnStyles.Add(new ColumnStyle(st, width));

            return tlp;
        }

        public static ListViewItem addColumn(this ListViewItem lvi, string text)
        {
            lvi.SubItems.Add(text);

            return lvi;
        }

        public static ListView setData(this ListView lv, ListViewItem[] list)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            lv.Items.AddRange(list);
            lv.autoResizeColumns();
            lv.selectFirstRow();
            lv.EndUpdate();

            return lv;
        } 

        public static ComboBox initDropDownList<TKey>(
            this ComboBox cb,
            IList<KeyValuePair<TKey, string>> data = null,
            TKey? value = default,
            string valueField = "Key",
            string textField = "Value") where TKey : struct

        {
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.ValueMember = valueField;
            cb.DisplayMember = textField;
            if (data != null) cb.DataSource = data;
            if (value != null) cb.SelectedValue = value;

            return cb;
        }

        public static ComboBox setDropDownList<TKey>(this ComboBox cb, IList<KeyValuePair<TKey, string>> data)
        {
            cb.DataSource = data;

            return cb;
        }
    }

}
