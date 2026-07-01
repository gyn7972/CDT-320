using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public enum CalibrationDialogButtonRole
    {
        Normal,
        Primary,
        Dark,
        Help
    }

    public sealed class CalibrationDialogButton : Button
    {
        private CalibrationDialogButtonRole _role;

        public CalibrationDialogButton()
        {
            Role = CalibrationDialogButtonRole.Normal;
        }

        public CalibrationDialogButtonRole Role
        {
            get { return _role; }
            set
            {
                _role = value;
                ApplyRoleStyle();
            }
        }

        private void ApplyRoleStyle()
        {
            switch (_role)
            {
                case CalibrationDialogButtonRole.Primary:
                    CalibrationDialogButtonStyle.ApplyFooterButtons(null, new[] { this });
                    break;
                case CalibrationDialogButtonRole.Dark:
                    CalibrationDialogButtonStyle.ApplyFooterButtons(null, null, new[] { this });
                    break;
                case CalibrationDialogButtonRole.Help:
                    CalibrationDialogButtonStyle.ApplyFooterButtons(null, null, null, new[] { this });
                    break;
                case CalibrationDialogButtonRole.Normal:
                default:
                    CalibrationDialogButtonStyle.ApplyFooterButtons(new[] { this });
                    break;
            }
        }
    }

    internal static class CalibrationDialogButtonStyle
    {
        private static readonly Color NormalBackColor = Color.White;
        private static readonly Color NormalForeColor = Color.Black;
        private static readonly Color PrimaryBackColor = Color.FromArgb(230, 126, 0);
        private static readonly Color DarkBackColor = Color.FromArgb(64, 64, 64);
        private static readonly Color BorderColor = Color.FromArgb(176, 176, 176);
        private static readonly Font ButtonFont = new Font("맑은 고딕", 9.5F, FontStyle.Bold);
        private static readonly Font HelpButtonFont = new Font("맑은 고딕", 12F, FontStyle.Bold);

        public static void ApplyFooterButtons(Button[] normalButtons, Button[] primaryButtons = null, Button[] darkButtons = null, Button[] helpButtons = null)
        {
            ApplyButtons(normalButtons, NormalBackColor, NormalForeColor, ButtonFont);
            ApplyButtons(primaryButtons, PrimaryBackColor, Color.White, ButtonFont);
            ApplyButtons(darkButtons, DarkBackColor, Color.White, ButtonFont);
            ApplyButtons(helpButtons, NormalBackColor, NormalForeColor, HelpButtonFont);
        }

        public static void ApplyCompactButtons(params Button[] buttons)
        {
            ApplyButtons(buttons, NormalBackColor, NormalForeColor, ButtonFont);
        }

        private static void ApplyButtons(Button[] buttons, Color backColor, Color foreColor, Font font)
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                button.BackColor = backColor;
                button.ForeColor = foreColor;
                button.Cursor = Cursors.Hand;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = BorderColor;
                button.Font = font;
                button.Margin = new Padding(6, 4, 6, 4);
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.UseVisualStyleBackColor = false;
            }
        }
    }
}
