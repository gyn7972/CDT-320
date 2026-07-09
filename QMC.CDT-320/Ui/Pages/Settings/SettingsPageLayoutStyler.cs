using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    internal static class SettingsPageLayoutStyler
    {
        private static readonly Padding NoPadding = Padding.Empty;
        private static readonly Padding ActionPadding = new Padding(2);
        private static readonly Padding ActionRowPadding = new Padding(1);
        private static readonly Color PageBackColor = Color.White;
        private static readonly Color GroupBackColor = Color.White;
        private static readonly Color GroupForeColor = Color.Black;
        private static readonly Color ActionBackColor = Color.FromArgb(128, 128, 128);
        private static readonly Font ActionFont = new Font("맑은 고딕", 8F, FontStyle.Bold);

        public static void Apply(Control page)
        {
            if (page == null)
                return;

            page.BackColor = PageBackColor;
            Normalize(page, true);
        }

        public static void ApplyHeader(Label header)
        {
            if (header == null)
                return;

            header.Dock = DockStyle.Fill;
            header.Margin = NoPadding;
            header.Padding = new Padding(12, 0, 0, 0);
            header.TextAlign = ContentAlignment.MiddleLeft;
        }

        public static void ApplyRoot(TableLayoutPanel root)
        {
            if (root == null)
                return;

            root.Dock = DockStyle.Fill;
            root.Margin = NoPadding;
            root.Padding = NoPadding;
        }

        public static void ApplyGroupBox(GroupBox group)
        {
            if (group == null)
                return;

            group.Dock = DockStyle.Fill;
            group.Margin = NoPadding;
            group.Padding = new Padding(1, 9, 1, 1);
            group.BackColor = GroupBackColor;
            group.ForeColor = GroupForeColor;
            group.Font = UiTheme.SectionFont;
            group.TabStop = false;
        }

        public static void ApplyActionRow(TableLayoutPanel row)
        {
            if (row == null)
                return;

            row.Dock = DockStyle.Fill;
            row.Margin = NoPadding;
            row.Padding = ActionRowPadding;

            foreach (Control child in row.Controls)
                ApplyActionControl(child);
        }

        public static void ApplyActionControl(Control control)
        {
            if (control == null)
                return;

            control.Dock = DockStyle.Fill;
            control.Margin = ActionPadding;
            control.Font = ActionFont;

            var button = control as Button;
            if (button != null)
            {
                button.BackColor = ActionBackColor;
                button.FlatStyle = FlatStyle.Flat;
                button.ForeColor = Color.White;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.UseVisualStyleBackColor = false;
                button.MinimumSize = new Size(72, 28);
            }
            else if (IsActionControl(control))
            {
                control.BackColor = ActionBackColor;
                control.ForeColor = Color.White;
            }
        }

        private static void Normalize(Control control, bool isRoot)
        {
            if (control == null)
                return;

            bool isHeader = IsHeaderLabel(control);

            if (!isRoot && !isHeader)
                control.Margin = Padding.Empty;
            else if (isHeader)
                ApplyHeader(control as Label);

            var group = control as GroupBox;
            if (group != null)
            {
                group.BackColor = GroupBackColor;
                group.ForeColor = GroupForeColor;
                group.Padding = new Padding(1, 9, 1, 1);
                group.Font = UiTheme.SectionFont;
                group.TabStop = false;
            }

            var table = control as TableLayoutPanel;
            if (table != null)
            {
                table.Margin = Padding.Empty;
                table.Padding = NoPadding;
            }

            var flow = control as FlowLayoutPanel;
            if (flow != null)
            {
                flow.Margin = Padding.Empty;
                flow.Padding = NoPadding;
            }

            var panel = control as Panel;
            if (panel != null)
            {
                panel.Margin = Padding.Empty;
                if (!(control is TableLayoutPanel))
                    panel.Padding = NoPadding;
            }

            var grid = control as DataGridView;
            if (grid != null)
            {
                grid.Margin = Padding.Empty;
                grid.BackgroundColor = PageBackColor;
                grid.BorderStyle = BorderStyle.FixedSingle;
            }

            var button = control as Button;
            if (button != null)
                button.Margin = Padding.Empty;

            if (IsActionControl(control))
                ApplyActionControl(control);

            var input = control as TextBoxBase;
            if (input != null)
                input.Margin = Padding.Empty;

            var combo = control as ComboBox;
            if (combo != null)
                combo.Margin = Padding.Empty;

            var check = control as CheckBox;
            if (check != null)
                check.Margin = Padding.Empty;

            foreach (Control child in control.Controls)
                Normalize(child, false);
        }

        private static bool IsActionControl(Control control)
        {
            if (control == null)
                return false;

            string typeName = control.GetType().Name ?? string.Empty;
            if (typeName.IndexOf("ActionButton", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (!(control is Button))
                return false;

            string parentName = control.Parent != null ? control.Parent.Name ?? string.Empty : string.Empty;
            if (parentName.IndexOf("action", System.StringComparison.OrdinalIgnoreCase) >= 0
                || parentName.IndexOf("buttonLayout", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            string text = (control.Text ?? string.Empty).Trim();
            return string.Equals(text, "APPLY", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "CONNECT", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "DISCONNECT", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "PING", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "CLEAR LOG", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "CAMERA SCALE SETUP", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHeaderLabel(Control control)
        {
            if (!(control is Label))
                return false;

            string name = control.Name ?? string.Empty;
            return name.IndexOf("Header", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsHeaderLabel(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (IsHeaderLabel(child) || ContainsHeaderLabel(child))
                    return true;
            }

            return false;
        }
    }
}
