using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    internal sealed class PickerAppliedZoneMoveDialog : Form
    {
        private static readonly Color HeaderBackColor = Color.FromArgb(225, 120, 0);
        private static readonly Color BodyBackColor = Color.FromArgb(238, 238, 238);
        private static readonly Color PanelBackColor = Color.White;
        private static readonly Color LabelBackColor = Color.FromArgb(224, 224, 224);

        private sealed class ZoneItem
        {
            public string Text { get; set; }
            public string PositionArrayName { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private readonly ComboBox cboPicker = new ComboBox();
        private readonly ComboBox cboZone = new ComboBox();

        public int PickerNo
        {
            get { return cboPicker.SelectedIndex + 1; }
        }

        public string ZoneText
        {
            get
            {
                ZoneItem item = cboZone.SelectedItem as ZoneItem;
                return item != null ? item.Text : string.Empty;
            }
        }

        public string PositionArrayName
        {
            get
            {
                ZoneItem item = cboZone.SelectedItem as ZoneItem;
                return item != null ? item.PositionArrayName : string.Empty;
            }
        }

        public PickerAppliedZoneMoveDialog(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(500, 260);
            BackColor = BodyBackColor;
            Font = new Font("Malgun Gothic", 9F, FontStyle.Regular);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = BodyBackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            Controls.Add(root);

            Label header = new Label
            {
                Text = string.IsNullOrWhiteSpace(title) ? "APPLIED ZONE MOVE" : title.ToUpperInvariant(),
                Dock = DockStyle.Fill,
                BackColor = HeaderBackColor,
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 14F, FontStyle.Bold),
                Padding = new Padding(16, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(header, 0, 0);

            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(14, 14, 14, 8),
                Padding = new Padding(10),
                BackColor = PanelBackColor,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            root.Controls.Add(content, 0, 1);

            AddComboRow(content, 0, "PICKER", cboPicker);
            AddComboRow(content, 1, "ZONE", cboZone);

            cboPicker.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPicker.FlatStyle = FlatStyle.Flat;
            cboPicker.Items.AddRange(new object[] { "PICKER #1", "PICKER #2", "PICKER #3", "PICKER #4" });
            cboPicker.SelectedIndex = 0;

            cboZone.DropDownStyle = ComboBoxStyle.DropDownList;
            cboZone.FlatStyle = FlatStyle.Flat;
            cboZone.Items.Add(new ZoneItem { Text = "BOTTOM", PositionArrayName = "DieBottomPosition" });
            cboZone.Items.Add(new ZoneItem { Text = "SIDE", PositionArrayName = "DieSidePosition" });
            cboZone.Items.Add(new ZoneItem { Text = "PLACE", PositionArrayName = "DiePlacePosition" });
            cboZone.SelectedIndex = 0;

            Label guide = new Label
            {
                Text = "Z AVOID -> Y AVOID -> X/T MOVE -> Y MOVE -> Z MOVE",
                Dock = DockStyle.Fill,
                Margin = new Padding(14, 0, 14, 0),
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.WhiteSmoke,
                ForeColor = Color.FromArgb(64, 64, 64),
                Font = new Font("Malgun Gothic", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(guide, 0, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 8, 14, 8),
                BackColor = BodyBackColor
            };
            root.Controls.Add(buttons, 0, 3);

            CalibrationDialogButton btnOk = new CalibrationDialogButton
            {
                Text = "MOVE",
                DialogResult = DialogResult.OK,
                Size = new Size(110, 34),
                Role = CalibrationDialogButtonRole.Primary
            };
            CalibrationDialogButton btnCancel = new CalibrationDialogButton
            {
                Text = "CANCEL",
                DialogResult = DialogResult.Cancel,
                Size = new Size(110, 34),
                Role = CalibrationDialogButtonRole.Normal
            };
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private static void AddComboRow(TableLayoutPanel content, int row, string labelText, ComboBox combo)
        {
            TableLayoutPanel rowLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = PanelBackColor
            };
            rowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            rowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Label label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(12, 0, 0, 0),
                BackColor = LabelBackColor,
                ForeColor = Color.Black,
                Font = new Font("Malgun Gothic", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            combo.Dock = DockStyle.Fill;
            combo.Margin = new Padding(12, 9, 12, 8);
            combo.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);

            rowLayout.Controls.Add(label, 0, 0);
            rowLayout.Controls.Add(combo, 1, 0);
            content.Controls.Add(rowLayout, 0, row);
        }
    }
}
