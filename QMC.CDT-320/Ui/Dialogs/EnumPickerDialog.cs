using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// Enum ?? ?? ??? ?? ? ??? ???? ?? ?????.
    /// ????? ? ???? ???? ???? ??? ?? ? ??.
    /// </summary>
    public partial class EnumPickerDialog : Form
    {
        public string SelectedValue { get; private set; }

        public EnumPickerDialog(string title, IEnumerable<string> options, string current)
        {
            Text = string.IsNullOrEmpty(title) ? "Select" : title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(360, 320);
            Font = new Font("맑은 고딕", 10F, FontStyle.Bold);

            var list = options != null ? new List<string>(options) : new List<string>();
            SelectedValue = current ?? string.Empty;

            var lblTitle = new Label
            {
                Text = string.IsNullOrEmpty(title) ? "Select" : title,
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(0xCC, 0x66, 0x00),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
            };
            Controls.Add(lblTitle);

            var listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                ItemHeight = 32,
                IntegralHeight = false,
            };
            foreach (var s in list) listBox.Items.Add(s);
            if (!string.IsNullOrEmpty(current))
            {
                int idx = listBox.Items.IndexOf(current);
                if (idx >= 0) listBox.SelectedIndex = idx;
            }
            listBox.DoubleClick += (s, e) => Commit(listBox);
            Controls.Add(listBox);

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
            };
            var btnOk = new Button
            {
                Text = "OK",
                Width = 100,
                Height = 40,
                DialogResult = DialogResult.None,
            };
            btnOk.Click += (s, e) => Commit(listBox);
            var btnCancel = new Button
            {
                Text = "Cancel",
                Width = 100,
                Height = 40,
                DialogResult = DialogResult.Cancel,
            };
            pnlButtons.Controls.Add(btnOk);
            pnlButtons.Controls.Add(btnCancel);
            Controls.Add(pnlButtons);

            Controls.SetChildIndex(pnlButtons, 0);
            Controls.SetChildIndex(listBox, 1);
            Controls.SetChildIndex(lblTitle, 2);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void Commit(ListBox listBox)
        {
            try
            {
                if (listBox.SelectedItem == null) return;
                SelectedValue = listBox.SelectedItem.ToString();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }
    }
}
