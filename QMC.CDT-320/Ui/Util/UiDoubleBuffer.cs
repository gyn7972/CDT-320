using System;
using System.Reflection;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Util
{
    internal static class UiDoubleBuffer
    {
        private static readonly PropertyInfo DoubleBufferedProperty =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Enable(Control root)
        {
            if (root == null)
                return;

            EnableOne(root);

            foreach (Control child in root.Controls)
                Enable(child);
        }

        private static void EnableOne(Control control)
        {
            try
            {
                if (control is TextBoxBase || control is ComboBox || control is NumericUpDown)
                    return;

                DoubleBufferedProperty?.SetValue(control, true, null);
            }
            catch
            {
            }
        }
    }
}
