using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// ??? ??? (?? / ?) ?? ???.
    /// <list type="bullet">
    ///   <item><description><see cref="DefineItems"/> ? ?? ??? ? ? ??? ?,</description></item>
    ///   <item><description><see cref="SetValue"/> / <see cref="SetValues"/> ? ?? ??? ????.</description></item>
    ///   <item><description>? CONFIG / STATUS / I-O ?? ?? ? ?? ?? ??? ?? ???? ??.</description></item>
    /// </list>
    /// </summary>
    public partial class ParamGrid : UserControl
    {
        // ?????????????????????????????????????????????
        //  ?? ??
        // ?????????????????????????????????????????????

        private readonly Dictionary<string, Label> _valueByName =
            new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _editable =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// ?? ?? ??? ? ??? ???? ? ??.
        /// ??? ?? ??.
        /// </summary>
        public event Action<string> ItemClicked;

        private int _pairsPerRow = 1;
        private int _nameWidth = 130;
        private int _rowHeight = 22;
        private string _placeholder = "—";

        // ?????????????????????????????????????????????
        //  ?? ??
        // ?????????????????????????????????????????????

        /// <summary>? ?? ??? (??,?) ? ??. 1~4 ??.</summary>
        public int PairsPerRow
        {
            get { return _pairsPerRow; }
            set { _pairsPerRow = value < 1 ? 1 : value; }
        }

        /// <summary>?? ?? ??? ? (??).</summary>
        public int NameWidth
        {
            get { return _nameWidth; }
            set { _nameWidth = value < 40 ? 40 : value; }
        }

        /// <summary>? ?? ?? (??).</summary>
        public int RowHeight
        {
            get { return _rowHeight; }
            set { _rowHeight = value < 14 ? 14 : value; }
        }

        /// <summary>?? ??? ? ??? ???.</summary>
        public string Placeholder
        {
            get { return _placeholder; }
            set { _placeholder = value ?? "—"; }
        }

        /// <summary>?? ?? ??.</summary>
        public Color NameColor { get; set; } = Color.FromArgb(0x60, 0x60, 0x60);

        /// <summary>? ?? ??.</summary>
        public Color ValueColor { get; set; } = Color.Black;

        /// <summary>??/?? ?? ??.</summary>
        public Color AlertColor { get; set; } = Color.IndianRed;

        /// <summary>?? ?? ?? ? ?? ??.</summary>
        public Color EditableColor { get; set; } = Color.FromArgb(0x1F, 0x49, 0x7D);

        // ?????????????????????????????????????????????
        //  ???
        // ?????????????????????????????????????????????

        public ParamGrid()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
        }

        // ?????????????????????????????????????????????
        //  ??? ?? / ?? API
        // ?????????????????????????????????????????????

        /// <summary>
        /// ??? ?? ?? ??? ????.
        /// ?? ? ?? ??? ?? ???? ?? ????.
        /// </summary>
        public void DefineItems(IEnumerable<string> names)
        {
            try
            {
                _layout.SuspendLayout();
                _layout.Controls.Clear();
                _layout.RowStyles.Clear();
                _layout.ColumnStyles.Clear();
                _valueByName.Clear();
                _editable.Clear();

                if (names == null) return;

                var list = new List<string>();
                foreach (var n in names)
                {
                    if (!string.IsNullOrEmpty(n))
                        list.Add(n);
                }
                if (list.Count == 0) return;

                int pairs = _pairsPerRow;
                int colCount = pairs * 2;
                _layout.ColumnCount = colCount;

                for (int p = 0; p < pairs; p++)
                {
                    _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, _nameWidth));
                    _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / pairs));
                }

                int rowCount = (list.Count + pairs - 1) / pairs;
                _layout.RowCount = rowCount;

                for (int r = 0; r < rowCount; r++)
                    _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, _rowHeight));

                for (int i = 0; i < list.Count; i++)
                {
                    int row = i / pairs;
                    int pairIdx = i % pairs;

                    var nameLabel = new Label
                    {
                        Text = list[i],
                        Font = new Font(Font, FontStyle.Regular),
                        ForeColor = NameColor,
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoSize = false,
                        Dock = DockStyle.Fill,
                        Margin = Padding.Empty,
                    };

                    var valueLabel = new Label
                    {
                        Text = _placeholder,
                        Font = new Font(Font, FontStyle.Bold),
                        ForeColor = ValueColor,
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoSize = false,
                        Dock = DockStyle.Fill,
                        Margin = Padding.Empty,
                        AutoEllipsis = true,
                    };
                    string itemKey = list[i];
                    valueLabel.Tag = itemKey;
                    valueLabel.Click += OnValueLabelClick;

                    int col = pairIdx * 2;
                    _layout.Controls.Add(nameLabel, col, row);
                    _layout.Controls.Add(valueLabel, col + 1, row);

                    _valueByName[list[i]] = valueLabel;
                }
            }
            catch (Exception ex)
            {
                QMC.CDT320.Alarms.AlarmManager.Raise(
                    QMC.CDT320.Alarms.AlarmSeverity.Warning,
                    "UI-PARAMGRID",
                    Name,
                    "DefineItems failed: " + ex.Message);
            }
            finally
            {
                _layout.ResumeLayout();
            }
        }

        /// <summary>
        /// ???? ?? ????. ???? ?? ??? ??.
        /// ?? ??? ??? ????(flicker ??).
        /// </summary>
        public void SetValue(string name, string value, bool alert = false)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return;
                if (!_valueByName.TryGetValue(name, out var lbl)) return;

                string text = value ?? _placeholder;
                Color color = alert ? AlertColor : (_editable.Contains(name) ? EditableColor : ValueColor);

                if (lbl.Text != text)
                    lbl.Text = text;
                if (lbl.ForeColor != color)
                    lbl.ForeColor = color;
            }
            catch (Exception ex)
            {
                QMC.CDT320.Alarms.AlarmManager.Raise(
                    QMC.CDT320.Alarms.AlarmSeverity.Warning,
                    "UI-PARAMGRID",
                    Name,
                    "SetValue failed [" + name + "]: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>?? ??.</summary>
        public void SetValues(IDictionary<string, string> values)
        {
            try
            {
                if (values == null) return;
                _layout.SuspendLayout();
                foreach (var kv in values)
                    SetValue(kv.Key, kv.Value);
            }
            catch (Exception ex)
            {
                QMC.CDT320.Alarms.AlarmManager.Raise(
                    QMC.CDT320.Alarms.AlarmSeverity.Warning,
                    "UI-PARAMGRID",
                    Name,
                    "SetValues failed: " + ex.Message);
            }
            finally
            {
                _layout.ResumeLayout();
            }
        }

        /// <summary>?? ? ??? placeholder ? ???.</summary>
        public void ClearValues()
        {
            try
            {
                _layout.SuspendLayout();
                foreach (var lbl in _valueByName.Values)
                {
                    if (lbl.Text != _placeholder)
                        lbl.Text = _placeholder;
                    if (lbl.ForeColor != ValueColor)
                        lbl.ForeColor = ValueColor;
                }
            }
            catch (Exception ex)
            {
                QMC.CDT320.Alarms.AlarmManager.Raise(
                    QMC.CDT320.Alarms.AlarmSeverity.Warning,
                    "UI-PARAMGRID",
                    Name,
                    "ClearValues failed: " + ex.Message);
            }
            finally
            {
                _layout.ResumeLayout();
            }
        }

        /// <summary>??? ?? ?? ??.</summary>
        public IReadOnlyCollection<string> ItemNames
        {
            get { return (IReadOnlyCollection<string>)_valueByName.Keys; }
        }

        // ?????????????????????????????????????????????
        //  ?? ?? ?? ??
        // ?????????????????????????????????????????????

        /// <summary>
        /// ??? ?? ???? ????. ? ?? ?? ? <see cref="ItemClicked"/> ???? ????.
        /// </summary>
        public void SetEditable(string name, bool editable = true)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return;
                if (!_valueByName.TryGetValue(name, out var lbl)) return;

                if (editable)
                {
                    _editable.Add(name);
                    lbl.Cursor = Cursors.Hand;
                    lbl.ForeColor = EditableColor;
                }
                else
                {
                    _editable.Remove(name);
                    lbl.Cursor = Cursors.Default;
                    lbl.ForeColor = ValueColor;
                }
            }
            catch (Exception ex)
            {
                QMC.CDT320.Alarms.AlarmManager.Raise(
                    QMC.CDT320.Alarms.AlarmSeverity.Warning,
                    "UI-PARAMGRID",
                    Name,
                    "SetEditable failed [" + name + "]: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>??? ?? ?? ?? ???.</summary>
        public void ClearEditable()
        {
            try
            {
                foreach (var name in _editable)
                {
                    if (_valueByName.TryGetValue(name, out var lbl))
                    {
                        lbl.Cursor = Cursors.Default;
                        lbl.ForeColor = ValueColor;
                    }
                }
                _editable.Clear();
            }
            catch
            {
            }
        }

        private void OnValueLabelClick(object sender, EventArgs e)
        {
            try
            {
                var lbl = sender as Label;
                if (lbl == null) return;
                string name = lbl.Tag as string;
                if (string.IsNullOrEmpty(name)) return;
                if (!_editable.Contains(name)) return;
                ItemClicked?.Invoke(name);
            }
            catch (Exception ex)
            {
                QMC.CDT320.Alarms.AlarmManager.Raise(
                    QMC.CDT320.Alarms.AlarmSeverity.Warning,
                    "UI-PARAMGRID",
                    Name,
                    "OnValueLabelClick failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
