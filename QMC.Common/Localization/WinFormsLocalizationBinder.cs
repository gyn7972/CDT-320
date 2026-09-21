using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QMC.Common.Localization
{
    /// <summary>
    /// 명시적인 번역 키와 i18n 태그가 있는 컨트롤만 갱신합니다.
    /// 임의의 입력값은 검색하지 않으며 모든 메서드는 해당 컨트롤의 UI 스레드에서 호출해야 합니다.
    /// </summary>
    public sealed class WinFormsLocalizationBinder
    {
        private readonly LocalizationService _service;
        private readonly Action<Control, string> _setText;
        private readonly ConditionalWeakTable<object, Binding> _bindings = new ConditionalWeakTable<object, Binding>();
        private readonly ConditionalWeakTable<Control, List<ToolTipBinding>> _toolTips
            = new ConditionalWeakTable<Control, List<ToolTipBinding>>();
        private readonly ConditionalWeakTable<ComboBox, ChoiceBinding> _choices
            = new ConditionalWeakTable<ComboBox, ChoiceBinding>();
        private readonly ConditionalWeakTable<DataGridView, CellBinding> _cells
            = new ConditionalWeakTable<DataGridView, CellBinding>();

        private sealed class Binding
        {
            public string Key;
            public string Literal;
            public object[] Arguments;
            public Func<string, string> Formatter;
        }

        /// <summary>
        /// 기본 표시 속성은 Control.Text입니다. 프로젝트 전용 컨트롤은 setText로 처리하며
        /// 서비스의 언어 변경 이벤트나 폼 수명 주기에 자동으로 구독하지 않습니다.
        /// </summary>
        public WinFormsLocalizationBinder(LocalizationService service, Action<Control, string> setText = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _setText = setText ?? ((control, text) => control.Text = text);
        }

        /// <summary>Tag를 보존하면서 번역 키를 등록하고 즉시 적용합니다. 컨트롤을 강하게 보관하지 않습니다.</summary>
        public void Bind(Control control, string key)
        {
            if (control == null || control.IsDisposed) return;
            Binding binding = _bindings.GetValue(control, item => new Binding());
            binding.Key = key ?? string.Empty;
            binding.Literal = null;
            binding.Arguments = null;
            binding.Formatter = null;
            SetDisplayText(control, _service.GetString(binding.Key));
        }

        /// <summary>원문 스냅샷과 순수 표시 변환기를 연결합니다. Apply에서 모델을 다시 조회하지 않습니다.</summary>
        public void BindDisplay(Control control, string originalText, Func<string, string> formatter)
        {
            if (control == null || control.IsDisposed) return;
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            Binding binding = _bindings.GetValue(control, item => new Binding());
            binding.Key = null;
            binding.Literal = originalText;
            binding.Arguments = null;
            binding.Formatter = formatter;
            SetDisplayText(control, GetText(binding));
        }

        private sealed class ToolTipBinding
        {
            public WeakReference<ToolTip> ToolTip;
            public string Key;
            public bool Disposed;
        }

        private sealed class ChoiceBinding
        {
            public Func<string, string> Format;
        }

        private sealed class CellBinding
        {
            public Func<string, string> Format;
            public Func<DataGridViewCell, bool> CanFormat;
        }

        /// <summary>선택 항목은 원문으로 유지하고 그릴 때만 변환합니다. 기존 사용자 그리기가 없는 콤보에 연결합니다.</summary>
        public void BindChoices(ComboBox combo, Func<string, string> format)
        {
            if (combo == null || combo.IsDisposed) return;
            if (format == null) throw new ArgumentNullException(nameof(format));
            ChoiceBinding binding;
            if (!_choices.TryGetValue(combo, out binding))
            {
                binding = new ChoiceBinding();
                _choices.Add(combo, binding);
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.DrawItem += DrawChoice;
            }
            binding.Format = format;
            combo.Invalidate();
        }

        private void DrawChoice(object sender, DrawItemEventArgs e)
        {
            var combo = sender as ComboBox;
            ChoiceBinding binding;
            if (combo == null || !_choices.TryGetValue(combo, out binding)) return;
            e.DrawBackground();
            if (e.Index >= 0 && e.Index < combo.Items.Count)
            {
                string original = combo.GetItemText(combo.Items[e.Index]);
                TextRenderer.DrawText(e.Graphics, binding.Format(original), e.Font, e.Bounds,
                    combo.Enabled ? e.ForeColor : System.Drawing.SystemColors.GrayText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            e.DrawFocusRectangle();
        }

        /// <summary>문자열 셀의 표시만 변환합니다. 기본 대상은 읽기 전용 셀이며 콤보 셀은 항상 제외합니다.</summary>
        public void BindReadOnlyCells(DataGridView grid, Func<string, string> format,
            Func<DataGridViewCell, bool> canFormat = null)
        {
            if (grid == null || grid.IsDisposed) return;
            if (format == null) throw new ArgumentNullException(nameof(format));
            CellBinding binding;
            if (!_cells.TryGetValue(grid, out binding))
            {
                binding = new CellBinding();
                _cells.Add(grid, binding);
                grid.CellFormatting += FormatCell;
            }
            binding.Format = format;
            binding.CanFormat = canFormat;
            grid.Invalidate();
        }

        private void FormatCell(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = sender as DataGridView;
            CellBinding binding;
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0 || !(e.Value is string) ||
                !_cells.TryGetValue(grid, out binding)) return;
            DataGridViewCell cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell is DataGridViewComboBoxCell ||
                !(binding.CanFormat != null ? binding.CanFormat(cell) : cell.ReadOnly)) return;
            e.Value = binding.Format((string)e.Value);
            e.FormattingApplied = true;
        }

        /// <summary>표시할 인수를 복사해 보관합니다. 언어를 바꿔도 입력값이나 모델을 다시 읽지 않습니다.</summary>
        public void BindFormat(Control control, string key, params object[] arguments)
        {
            if (control == null || control.IsDisposed) return;
            Binding binding = _bindings.GetValue(control, item => new Binding());
            binding.Key = key ?? string.Empty;
            binding.Literal = null;
            binding.Arguments = arguments == null ? new object[0] : (object[])arguments.Clone();
            binding.Formatter = null;
            SetDisplayText(control, GetText(binding));
        }

        /// <summary>열의 Name, DataPropertyName, 셀 값과 선택은 유지하고 머리글만 연결합니다.</summary>
        public void Bind(DataGridViewColumn column, string key)
        {
            if (column == null) return;
            BindKey(column, key);
            column.HeaderText = _service.GetString(key ?? string.Empty);
        }

        /// <summary>ListView의 열 식별자와 행 값은 유지하고 머리글만 연결합니다.</summary>
        public void Bind(ColumnHeader column, string key)
        {
            if (column == null) return;
            BindKey(column, key);
            column.Text = _service.GetString(key ?? string.Empty);
        }

        /// <summary>메뉴의 명령, Tag, 체크 상태와 이벤트는 유지하고 표시만 연결합니다.</summary>
        public void Bind(ToolStripItem item, string key)
        {
            if (item == null || item.IsDisposed) return;
            BindKey(item, key);
            item.Text = _service.GetString(key ?? string.Empty);
        }

        /// <summary>메뉴의 동적 표시 인수를 보관하며 명령과 Tag는 유지합니다.</summary>
        public void BindFormat(ToolStripItem item, string key, params object[] arguments)
        {
            if (item == null || item.IsDisposed) return;
            Binding binding = _bindings.GetValue(item, target => new Binding());
            binding.Key = key ?? string.Empty;
            binding.Literal = null;
            binding.Arguments = arguments == null ? new object[0] : (object[])arguments.Clone();
            binding.Formatter = null;
            item.Text = GetText(binding);
        }

        private void BindKey(object target, string key)
        {
            Binding binding = _bindings.GetValue(target, item => new Binding());
            binding.Key = key ?? string.Empty;
            binding.Literal = null;
            binding.Arguments = null;
            binding.Formatter = null;
        }

        /// <summary>툴팁을 약한 참조로 연결하며 컨트롤의 Text와 Tag는 변경하지 않습니다.</summary>
        public void Bind(ToolTip toolTip, Control control, string key)
        {
            if (toolTip == null || control == null || control.IsDisposed) return;
            List<ToolTipBinding> bindings = _toolTips.GetValue(control, item => new List<ToolTipBinding>());
            foreach (ToolTipBinding existing in bindings)
            {
                ToolTip target;
                if (!existing.Disposed && existing.ToolTip.TryGetTarget(out target) && ReferenceEquals(target, toolTip))
                {
                    existing.Key = key ?? string.Empty;
                    toolTip.SetToolTip(control, _service.GetString(existing.Key));
                    return;
                }
            }
            var binding = new ToolTipBinding
            {
                ToolTip = new WeakReference<ToolTip>(toolTip),
                Key = key ?? string.Empty
            };
            toolTip.Disposed += (sender, args) => binding.Disposed = true;
            bindings.Add(binding);
            toolTip.SetToolTip(control, _service.GetString(binding.Key));
        }

        /// <summary>번역하지 않을 고정 문구를 등록합니다. 기존 키 등록을 대체하며 Tag는 보존합니다.</summary>
        public void BindLiteral(Control control, string text)
        {
            if (control == null || control.IsDisposed) return;
            Binding binding = _bindings.GetValue(control, item => new Binding());
            binding.Key = null;
            binding.Literal = text ?? string.Empty;
            binding.Arguments = null;
            binding.Formatter = null;
            SetDisplayText(control, binding.Literal);
        }

        /// <summary>
        /// 현재 컨트롤과 자식의 i18n 태그를 우선 적용하고, 없으면 등록된 키를 적용합니다.
        /// 각 컨트롤의 ILocalizedView도 호출합니다. 값, 선택 및 Tag는 변경하지 않습니다.
        /// </summary>
        public void Apply(Control root)
        {
            Apply(root, new HashSet<Control>());
        }

        private void Apply(Control root, HashSet<Control> visited)
        {
            if (root == null || root.IsDisposed || !visited.Add(root)) return;
            string key;
            Binding binding;
            if (TryGetTranslationKey(root.Tag as string, out key))
                SetDisplayText(root, _service.GetString(key));
            else if (_bindings.TryGetValue(root, out binding))
                SetDisplayText(root, GetText(binding));

            if (root.IsDisposed) return;
            if (root is ILocalizedView view) view.ApplyLanguage();
            if (root.IsDisposed) return;
            ApplyToolTips(root);

            if (root is DataGridView grid)
                foreach (DataGridViewColumn column in grid.Columns)
                    ApplyText(column, column.Tag, text => column.HeaderText = text);
            if (root is ListView list)
                foreach (ColumnHeader column in list.Columns)
                    ApplyText(column, column.Tag, text => column.Text = text);
            if (root is ToolStrip strip)
                foreach (ToolStripItem item in strip.Items) ApplyMenuItem(item);
            if (root is ComboBox choices && _choices.TryGetValue(choices, out var choiceBinding))
                choices.Invalidate();
            if (root is DataGridView cells && _cells.TryGetValue(cells, out var cellBinding))
                cells.Invalidate();
            Apply(root.ContextMenuStrip, visited);

            var children = new Control[root.Controls.Count];
            root.Controls.CopyTo(children, 0);
            foreach (Control child in children) Apply(child, visited);
        }

        private void ApplyMenuItem(ToolStripItem item)
        {
            if (item == null || item.IsDisposed) return;
            ApplyText(item, item.Tag, text => item.Text = text);
            if (item is ToolStripDropDownItem menu)
                foreach (ToolStripItem child in menu.DropDownItems) ApplyMenuItem(child);
        }

        private void ApplyToolTips(Control control)
        {
            List<ToolTipBinding> bindings;
            if (!_toolTips.TryGetValue(control, out bindings)) return;
            for (int index = bindings.Count - 1; index >= 0; index--)
            {
                ToolTipBinding binding = bindings[index];
                ToolTip toolTip;
                if (binding.Disposed || !binding.ToolTip.TryGetTarget(out toolTip))
                    bindings.RemoveAt(index);
                else
                    toolTip.SetToolTip(control, _service.GetString(binding.Key));
            }
        }

        private void ApplyText(object target, object tag, Action<string> setText)
        {
            string key;
            Binding binding;
            if (TryGetTranslationKey(tag as string, out key))
                setText(_service.GetString(key));
            else if (_bindings.TryGetValue(target, out binding))
                setText(GetText(binding));
        }

        private string GetText(Binding binding)
        {
            if (binding.Formatter != null) return binding.Formatter(binding.Literal);
            if (binding.Key == null) return binding.Literal;
            return binding.Arguments == null
                ? _service.GetString(binding.Key)
                : _service.Format(binding.Key, binding.Arguments);
        }

        private static bool TryGetTranslationKey(string tag, out string key)
        {
            key = null;
            if (string.IsNullOrEmpty(tag)) return false;
            foreach (string part in tag.Split(';'))
            {
                string value = part.Trim();
                if (!value.StartsWith("i18n:", StringComparison.Ordinal)) continue;
                key = value.Substring(5);
                return key.Length > 0;
            }
            return false;
        }

        private void SetDisplayText(Control control, string text)
        {
            if (control.IsDisposed) return;
            _setText(control, text);
            if (!control.IsDisposed) control.Invalidate();
        }
    }
}
