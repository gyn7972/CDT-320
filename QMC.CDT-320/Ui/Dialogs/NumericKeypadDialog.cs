using System;
using System.Globalization;
using System.Windows.Forms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class NumericKeypadDialog : Form
    {
        // [픽업 BIN 필터 2026-08-27] 자유 텍스트 모드 — OK 시 수식 평가를 강제하지 않고 입력
        // 문자열을 그대로 반환한다(Text형 그리드 항목의 터치 입력용, 예: BIN CSV "1" / "1.3").
        private readonly bool _freeTextMode;

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
            : this(title, valueText, unit, false)
        {
        }

        public NumericKeypadDialog(string title, string valueText, string unit, bool freeTextMode)
        {
            try
            {
                InitializeComponent();
                _freeTextMode = freeTextMode;
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
                // 자유 텍스트 모드: 점은 목록 구분자로도 쓰이므로("1.3"=1,3) 중복 제한 없이 입력.
                if (_freeTextMode)
                {
                    ReplaceSelection(".");
                    return;
                }

                string current = txtValue.Text ?? string.Empty;
                int start = txtValue.SelectionStart;
                int length = txtValue.SelectionLength;
                string remaining = current.Remove(start, length);
                if (CurrentNumberTokenHasDot(remaining, start))
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

        private void OperatorButton_Click(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null)
                    return;

                string op = Convert.ToString(button.Tag, CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(op))
                    op = button.Text;

                InsertOperator(op);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Operator failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void EqualsButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_freeTextMode)
                    return;   // 자유 텍스트 모드에서는 수식 평가 없음.

                double result;
                if (!TryEvaluateExpression(txtValue.Text, out result))
                {
                    QMC.Common.MessageDialog.Show(this, "Calculation expression is invalid.", "Numeric Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtValue.Focus();
                    txtValue.SelectAll();
                    return;
                }

                SetValue(result);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NUMERIC-KEYPAD", "Equals failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                // [픽업 BIN 필터 2026-08-27] 자유 텍스트 모드: 수식 평가 없이 입력값 그대로 반환
                // (빈 값 허용 — Text 항목의 "필터 해제" 입력). 유효성은 항목 setter가 정규화한다.
                if (_freeTextMode)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                double value;
                if (!TryEvaluateExpression(txtValue.Text, out value))
                {
                    QMC.Common.MessageDialog.Show(this, "Number value is invalid.", "Numeric Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtValue.Focus();
                    txtValue.SelectAll();
                    return;
                }

                SetValue(value);
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

        private void InsertOperator(string op)
        {
            op = NormalizeOperator(op);
            if (string.IsNullOrWhiteSpace(op))
                return;

            string text = txtValue.Text ?? string.Empty;
            int start = txtValue.SelectionStart;
            int length = txtValue.SelectionLength;

            if (length > 0)
            {
                ReplaceSelection(op);
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                if (op == "-")
                    ReplaceSelection(op);
                return;
            }

            int previousIndex = start - 1;
            while (previousIndex >= 0 && char.IsWhiteSpace(text[previousIndex]))
                previousIndex--;

            if (previousIndex >= 0 && IsOperatorChar(text[previousIndex]))
            {
                if (op == "-" && text[previousIndex] != '-')
                {
                    ReplaceSelection(op);
                    return;
                }

                txtValue.Text = text.Remove(previousIndex, 1).Insert(previousIndex, op);
                txtValue.SelectionStart = previousIndex + op.Length;
                txtValue.SelectionLength = 0;
                return;
            }

            ReplaceSelection(op);
        }

        private static bool TryEvaluateExpression(string expression, out double value)
        {
            value = 0.0;
            try
            {
                var parser = new ExpressionParser(expression);
                return parser.TryParse(out value);
            }
            catch
            {
                value = 0.0;
                return false;
            }
            finally
            {
            }
        }

        private void SetValue(double value)
        {
            txtValue.Text = value.ToString("0.######", CultureInfo.InvariantCulture);
            txtValue.SelectionStart = txtValue.Text.Length;
            txtValue.SelectionLength = 0;
        }

        private static string NormalizeOperator(string op)
        {
            op = (op ?? string.Empty).Trim();
            if (op == "×") return "*";
            if (op == "÷") return "/";
            if (op == "+" || op == "-" || op == "*" || op == "/") return op;
            return string.Empty;
        }

        private static bool IsOperatorChar(char ch)
        {
            return ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '×' || ch == '÷';
        }

        private static bool CurrentNumberTokenHasDot(string text, int cursor)
        {
            int left = Math.Min(Math.Max(cursor - 1, -1), text.Length - 1);
            while (left >= 0 && !IsOperatorChar(text[left]))
                left--;

            int right = Math.Min(Math.Max(cursor, 0), text.Length);
            while (right < text.Length && !IsOperatorChar(text[right]))
                right++;

            for (int i = left + 1; i < right; i++)
            {
                if (text[i] == '.')
                    return true;
            }

            return false;
        }

        private sealed class ExpressionParser
        {
            private readonly string _text;
            private int _index;

            public ExpressionParser(string text)
            {
                _text = (text ?? string.Empty).Replace('×', '*').Replace('÷', '/');
            }

            public bool TryParse(out double value)
            {
                value = ParseExpression();
                SkipWhiteSpace();
                return _index >= _text.Length && !double.IsNaN(value) && !double.IsInfinity(value);
            }

            private double ParseExpression()
            {
                double value = ParseTerm();
                while (true)
                {
                    SkipWhiteSpace();
                    if (Match('+'))
                        value += ParseTerm();
                    else if (Match('-'))
                        value -= ParseTerm();
                    else
                        return value;
                }
            }

            private double ParseTerm()
            {
                double value = ParseFactor();
                while (true)
                {
                    SkipWhiteSpace();
                    if (Match('*'))
                        value *= ParseFactor();
                    else if (Match('/'))
                    {
                        double divisor = ParseFactor();
                        if (Math.Abs(divisor) < double.Epsilon)
                            throw new DivideByZeroException();
                        value /= divisor;
                    }
                    else
                        return value;
                }
            }

            private double ParseFactor()
            {
                SkipWhiteSpace();
                if (Match('+'))
                    return ParseFactor();
                if (Match('-'))
                    return -ParseFactor();

                return ParseNumber();
            }

            private double ParseNumber()
            {
                SkipWhiteSpace();
                int start = _index;
                bool hasDigit = false;

                while (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] == '.'))
                {
                    if (char.IsDigit(_text[_index]))
                        hasDigit = true;
                    _index++;
                }

                if (!hasDigit)
                    throw new FormatException("Number expected.");

                string token = _text.Substring(start, _index - start);
                double value;
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    throw new FormatException("Invalid number.");

                return value;
            }

            private bool Match(char ch)
            {
                if (_index >= _text.Length || _text[_index] != ch)
                    return false;

                _index++;
                return true;
            }

            private void SkipWhiteSpace()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                    _index++;
            }
        }

        private void ReplaceSelection(string text)
        {
            try
            {
                int start = txtValue.SelectionStart;
                string value = text ?? string.Empty;
                txtValue.Text = txtValue.Text.Remove(start, txtValue.SelectionLength).Insert(start, value);
                txtValue.SelectionStart = start + value.Length;
                txtValue.SelectionLength = 0;
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


