using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    // R1 — Handler(QMC.CDT_320.Ui.Controls.NumericKeypadDialog) public API 1:1 미러.
    // 의존성 치환: EventLogger.Write → Debug.WriteLine, QMC.Common.MessageDialog → MessageBox.
    public partial class NumericKeypadDialog : Form
    {
        public string ValueText
        {
            get
            {
                try { return txtValue.Text; }
                catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] ValueText failed: " + ex.Message); return string.Empty; }
            }
        }

        public NumericKeypadDialog(string title, string valueText, string unit)
        {
            InitializeComponent();
            lblTitle.Text = string.IsNullOrWhiteSpace(title) ? "Parameter" : title;
            txtValue.Text = valueText ?? string.Empty;
            lblUnit.Text = unit ?? string.Empty;
            txtValue.SelectAll();
        }

        private void DigitButton_Click(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null) return;
                ReplaceSelection(button.Text);
            }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] Digit failed: " + ex.Message); }
        }

        private void DotButton_Click(object sender, EventArgs e)
        {
            try
            {
                string current = txtValue.Text ?? string.Empty;
                int start = txtValue.SelectionStart;
                int length = txtValue.SelectionLength;
                string remaining = current.Remove(start, length);
                if (remaining.Contains(".")) return;

                if (string.IsNullOrEmpty(remaining)) { ReplaceSelection("0."); return; }
                if (remaining == "-" && start >= 1) { ReplaceSelection("0."); return; }
                ReplaceSelection(".");
            }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] Dot failed: " + ex.Message); }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtValue.SelectionLength > 0) { ReplaceSelection(string.Empty); return; }
                if (txtValue.SelectionStart <= 0) return;

                int index = txtValue.SelectionStart;
                txtValue.Text = txtValue.Text.Remove(index - 1, 1);
                txtValue.SelectionStart = index - 1;
            }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] Backspace failed: " + ex.Message); }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            try { txtValue.Clear(); }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] Clear failed: " + ex.Message); }
        }

        private void SignButton_Click(object sender, EventArgs e)
        {
            try
            {
                string text = txtValue.Text ?? string.Empty;
                if (text.StartsWith("-", StringComparison.Ordinal))
                    txtValue.Text = text.Substring(1);
                else
                    txtValue.Text = "-" + text;
                txtValue.SelectionStart = txtValue.Text.Length;
            }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] Sign failed: " + ex.Message); }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                double value;
                if (!double.TryParse(txtValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                    !double.TryParse(txtValue.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                {
                    MessageBox.Show(this, "Number value is invalid.", "Numeric Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtValue.Focus();
                    txtValue.SelectAll();
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] OK failed: " + ex.Message); }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            try { DialogResult = DialogResult.Cancel; Close(); }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] Cancel failed: " + ex.Message); }
        }

        private void TripleZeroButton_Click(object sender, EventArgs e)
        {
            try { ReplaceSelection("000"); }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] Triple zero failed: " + ex.Message); }
        }

        private void IncrementButton_Click(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null) return;

                double step;
                if (!double.TryParse(Convert.ToString(button.Tag), NumberStyles.Float, CultureInfo.InvariantCulture, out step))
                    return;

                double current = 0.0;
                string text = txtValue.Text ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text) &&
                    !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out current) &&
                    !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out current))
                {
                    current = 0.0;
                }

                txtValue.Text = (current + step).ToString("0.######", CultureInfo.InvariantCulture);
                txtValue.SelectionStart = txtValue.Text.Length;
                txtValue.SelectionLength = 0;
            }
            catch (Exception ex) { Debug.WriteLine("[NUMERIC-KEYPAD] Increment failed: " + ex.Message); }
        }

        private void ReplaceSelection(string text)
        {
            int start = txtValue.SelectionStart;
            txtValue.Text = txtValue.Text.Remove(start, txtValue.SelectionLength).Insert(start, text ?? string.Empty);
            txtValue.SelectionStart = start + (text ?? string.Empty).Length;
        }
    }
}
