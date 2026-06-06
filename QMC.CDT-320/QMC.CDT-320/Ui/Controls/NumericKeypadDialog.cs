using System;
using System.Globalization;
using System.Windows.Forms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class NumericKeypadDialog : Form
    {
        public string ValueText
        {
            get
            {
                try
                {
                    return txtValue.Text;
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "ValueText failed: " + ex.Message);
                    return string.Empty;
                }
                finally
                {
                }
            }
        }

        public NumericKeypadDialog(string title, string valueText, string unit)
        {
            try
            {
                InitializeComponent();
                lblTitle.Text = string.IsNullOrWhiteSpace(title) ? "Parameter" : title;
                txtValue.Text = valueText ?? string.Empty;
                lblUnit.Text = unit ?? string.Empty;
                txtValue.SelectAll();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void DigitButton_Click(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null)
                    return;

                ReplaceSelection(button.Text);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Digit failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void DotButton_Click(object sender, EventArgs e)
        {
            try
            {
                string current = txtValue.Text ?? string.Empty;
                int start = txtValue.SelectionStart;
                int length = txtValue.SelectionLength;
                string remaining = current.Remove(start, length);
                if (remaining.Contains("."))
                    return;

                if (string.IsNullOrEmpty(remaining))
                {
                    ReplaceSelection("0.");
                    return;
                }

                if (remaining == "-" && start >= 1)
                {
                    ReplaceSelection("0.");
                    return;
                }

                ReplaceSelection(".");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Dot failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtValue.SelectionLength > 0)
                {
                    ReplaceSelection(string.Empty);
                    return;
                }

                if (txtValue.SelectionStart <= 0)
                    return;

                int index = txtValue.SelectionStart;
                txtValue.Text = txtValue.Text.Remove(index - 1, 1);
                txtValue.SelectionStart = index - 1;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Backspace failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            try
            {
                txtValue.Clear();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Clear failed: " + ex.Message);
            }
            finally
            {
            }
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
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Sign failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                double value;
                if (!double.TryParse(txtValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                    !double.TryParse(txtValue.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                {
                    QMC.Common.MessageDialog.Show(this, "Number value is invalid.", "Numeric Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtValue.Focus();
                    txtValue.SelectAll();
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "OK failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Cancel failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void TripleZeroButton_Click(object sender, EventArgs e)
        {
            try
            {
                ReplaceSelection("000");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Triple zero failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void IncrementButton_Click(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null)
                    return;

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
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Increment failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ReplaceSelection(string text)
        {
            try
            {
                int start = txtValue.SelectionStart;
                txtValue.Text = txtValue.Text.Remove(start, txtValue.SelectionLength).Insert(start, text ?? string.Empty);
                txtValue.SelectionStart = start + (text ?? string.Empty).Length;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}


