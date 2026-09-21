using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Logging;

using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class IoCylinderPanelControl : UserControl
    {
        private readonly List<IoCylinderItem> _items = new List<IoCylinderItem>();
        private readonly Dictionary<IoCylinderItem, IoCylinderRow> _rows = new Dictionary<IoCylinderItem, IoCylinderRow>();
        private bool _isRefreshing;
        private bool _isCommandRunning;
        private int _columnCount = 1;

        public IoCylinderPanelControl()
        {
            try
            {
                InitializeComponent();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        /// <summary>행을 몇 개 열로 배치할지. 1=단열(기본), 2=2열. FlowLayoutPanel wrap 이용.</summary>
        public int ColumnCount
        {
            get { return _columnCount; }
            set { _columnCount = Math.Max(1, value); }
        }

        private bool _autoFitParentGroupHeight;

        /// <summary>true면 SetItems 후 부모 GroupBox 높이를 행 수에 맞춰 자동 조정하고 스크롤을 끈다.
        /// 높이가 항상 내용에 맞춰지므로 스크롤 없이 전 항목이 보인다.</summary>
        public bool AutoFitParentGroupHeight
        {
            get { return _autoFitParentGroupHeight; }
            set
            {
                _autoFitParentGroupHeight = value;
                if (value)
                    rowsHost.AutoScroll = false;   // 높이 자동맞춤이 보장되므로 스크롤바 잔상 제거
            }
        }

        private void FitParentGroupHeight()
        {
            try
            {
                if (!_autoFitParentGroupHeight)
                    return;

                GroupBox group = Parent as GroupBox;
                if (group == null || !IsHandleCreated)
                    return;

                // 배치 완료된 실제 마지막 행의 바닥(Bottom)을 실측 → 간격/wrap 오차와 무관하게 정확
                int maxBottom = 0;
                foreach (Control child in rowsHost.Controls)
                {
                    if (child.Bottom > maxBottom)
                        maxBottom = child.Bottom;
                }
                if (maxBottom <= 0)
                    return;

                int contentHeight = maxBottom + rowsHost.Padding.Bottom + 6;
                int chrome = group.Height - rowsHost.Height;   // 그룹 헤더 + 패딩 (현재 레이아웃 기준 실측)
                if (chrome < 0)
                    chrome = 24;
                group.Height = contentHeight + chrome;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "IO-PANEL", "FitParentGroupHeight failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int ComputeRowWidth()
        {
            return Math.Max(1, (rowsHost.ClientSize.Width / _columnCount) - 3);
        }

        // 호출부는 "읽는 순서(1열 항목 전부 → 2열 항목 전부)"로 항목을 준다.
        // LeftToRight wrap 컨테이너에서 열 우선(column-major)으로 보이도록 flow 순서를 재배열한다.
        private List<IoCylinderItem> OrderForDisplay()
        {
            if (_columnCount <= 1 || _items.Count == 0)
                return _items;

            int n = _items.Count;
            int rowsPerCol = (n + _columnCount - 1) / _columnCount;
            var ordered = new List<IoCylinderItem>(n);
            for (int r = 0; r < rowsPerCol; r++)
            {
                for (int c = 0; c < _columnCount; c++)
                {
                    int idx = c * rowsPerCol + r;
                    if (idx < n)
                        ordered.Add(_items[idx]);
                }
            }
            return ordered;
        }

        public void SetItems(IEnumerable<IoCylinderItem> items)
        {
            try
            {
                _items.Clear();
                _rows.Clear();
                rowsHost.Controls.Clear();

                if (items != null)
                    _items.AddRange(items);

                foreach (var item in OrderForDisplay())
                    AddRow(item);

                RefreshStates();

                // 레이아웃 확정 후 실측해야 그룹 크롬/행 높이가 정확하다
                if (_autoFitParentGroupHeight && IsHandleCreated)
                    BeginInvoke((Action)FitParentGroupHeight);
                else
                    FitParentGroupHeight();
            }
            catch (Exception ex)
            {
                string message = "I/O panel set failed: " + Name + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", message);
                QMC.Common.MessageDialog.Show(this, Lang.Format("controls.io.setFailed", Name, ex.Message), Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        public void RefreshStates()
        {
            try
            {
                _isRefreshing = true;
                foreach (var item in _items)
                    RefreshRow(item);
            }
            catch (Exception ex)
            {
                string message = "I/O panel refresh failed: " + Name + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", message);
                QMC.Common.MessageDialog.Show(this, Lang.Format("controls.io.refreshFailed", Name, ex.Message), Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void AddRow(IoCylinderItem item)
        {
            try
            {
                if (item == null)
                    return;

                var rowPanel = new Panel();
                var label = new Label();

                // 출력/실린더는 사용자가 클릭해 조작 가능 → 토글 스위치 + Hand 커서로 읽기전용 입력과 구분한다.
                bool controllable = item.ItemType == IoCylinderItemType.Output ||
                                    item.ItemType == IoCylinderItemType.Cylinder;

                rowPanel.Height = 30;
                rowPanel.Width = ComputeRowWidth();
                rowPanel.Margin = new Padding(0);
                rowPanel.BackColor = Color.FromArgb(207, 211, 216);
                rowPanel.Tag = item;

                IndicatorDot dot = null;
                ToggleSwitch toggle = null;
                Control indicator;
                if (controllable)
                {
                    toggle = new ToggleSwitch();
                    toggle.Location = new Point(2, 7);
                    toggle.Size = new Size(28, 16);
                    toggle.OnColor = Color.LimeGreen;
                    toggle.OffColor = Color.FromArgb(85, 85, 85);
                    toggle.BackColor = Color.Transparent;
                    toggle.Cursor = Cursors.Hand;
                    toggle.Tag = item;
                    indicator = toggle;
                }
                else
                {
                    dot = new IndicatorDot();
                    dot.Location = new Point(6, 7);
                    dot.Size = new Size(16, 16);
                    dot.OnColor = Color.LimeGreen;
                    dot.OffColor = Color.FromArgb(85, 85, 85);
                    dot.BackColor = Color.Transparent;
                    dot.Tag = item;
                    indicator = dot;
                }

                label.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                label.BackColor = Color.FromArgb(207, 211, 216);
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
                label.ForeColor = Color.FromArgb(20, 24, 28);
                label.Location = new Point(32, 0);
                label.Size = new Size(Math.Max(40, rowPanel.Width - 32), 30);
                label.Padding = new Padding(8, 0, 6, 0);
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.Text = item.DisplayName ?? string.Empty;
                label.Tag = item;

                if (controllable)
                {
                    rowPanel.Cursor = Cursors.Hand;
                    label.Cursor = Cursors.Hand;
                }

                rowPanel.Controls.Add(indicator);
                rowPanel.Controls.Add(label);
                rowPanel.Click += Row_Click;
                indicator.Click += Row_Click;
                label.Click += Row_Click;
                if (item.ItemType == IoCylinderItemType.Input && item.InputDoubleClickCommand != null)
                {
                    rowPanel.MouseDoubleClick += Row_MouseDoubleClick;
                    indicator.MouseDoubleClick += Row_MouseDoubleClick;
                    label.MouseDoubleClick += Row_MouseDoubleClick;
                    rowPanel.Cursor = label.Cursor = indicator.Cursor = Cursors.Hand;
                }
                rowPanel.ContextMenuStrip = BuildContextMenu(item);
                label.ContextMenuStrip = rowPanel.ContextMenuStrip;
                indicator.ContextMenuStrip = rowPanel.ContextMenuStrip;

                rowsHost.Controls.Add(rowPanel);
                _rows[item] = new IoCylinderRow(rowPanel, dot, toggle, label);
            }
            catch (Exception ex)
            {
                string message = "I/O row add failed: " + (item != null ? item.DisplayName : string.Empty) + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", message);
                QMC.Common.MessageDialog.Show(this, Lang.Format("controls.io.rowAddFailed", item != null ? item.DisplayName : string.Empty, ex.Message), Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private ContextMenuStrip BuildContextMenu(IoCylinderItem item)
        {
            try
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("Refresh", null, (s, e) => RefreshStates());

                return menu;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void RefreshRow(IoCylinderItem item)
        {
            try
            {
                if (item == null || !_rows.ContainsKey(item))
                    return;

                bool actualOn = item.StateGetter != null && item.StateGetter();
                bool forced = item.ItemType == IoCylinderItemType.Input &&
                    item.InputOverrideGetter != null && item.InputOverrideGetter();
                bool on = actualOn || forced;
                IoCylinderRow row = _rows[item];
                if (row.Dot != null)
                {
                    row.Dot.OnColor = forced ? Color.DarkOrange : Color.LimeGreen;
                    row.Dot.IsOn = on;
                }
                if (row.Toggle != null)
                    row.Toggle.IsOn = on;
                row.Label.Text = forced
                    ? item.DisplayName + " 강제 ON·실제 " + (actualOn ? "ON" : "OFF")
                    : GetDisplayText(item, on);
                row.Label.BackColor = forced ? Color.FromArgb(255, 237, 205)
                    : on ? Color.FromArgb(219, 246, 226) : Color.FromArgb(207, 211, 216);
                row.Label.ForeColor = forced ? Color.FromArgb(130, 70, 0)
                    : on ? Color.FromArgb(20, 115, 55) : Color.FromArgb(20, 24, 28);
            }
            catch (Exception ex)
            {
                string message = "I/O row refresh failed: " + (item != null ? item.DisplayName : string.Empty) + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", message);
                QMC.Common.MessageDialog.Show(this, Lang.Format("controls.io.rowRefreshFailed", item != null ? item.DisplayName : string.Empty, ex.Message), Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private string GetDisplayText(IoCylinderItem item, bool on)
        {
            try
            {
                if (item == null)
                    return string.Empty;

                if (item.ItemType == IoCylinderItemType.Output)
                    return (item.DisplayName ?? string.Empty) + " : " + (on ? (item.OnText ?? "ON") : (item.OffText ?? "OFF"));

                if (item.ItemType == IoCylinderItemType.Cylinder)
                    return (item.DisplayName ?? string.Empty) + " : " + (on ? (item.OnText ?? "FWD") : (item.OffText ?? "BWD/OFF"));

                return item.DisplayName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
            }
        }

        private async void Row_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Control control = sender as Control;
            IoCylinderItem item = control != null ? control.Tag as IoCylinderItem : null;
            if (e.Button != MouseButtons.Left || _isRefreshing || _isCommandRunning ||
                item == null || item.ItemType != IoCylinderItemType.Input || item.InputDoubleClickCommand == null)
                return;

            _isCommandRunning = true;
            IDisposable recipeOperationScope = null;
            try
            {
                if (!TryBeginRecipePanelCommand(item, out recipeOperationScope))
                    return;

                await item.InputDoubleClickCommand();
                RefreshStates();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL",
                    "입력 신호 복구 요청 중 오류가 발생했습니다. " + item.DisplayName + ": " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("controls.io.forceFlow"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isCommandRunning = false;
                if (recipeOperationScope != null)
                    recipeOperationScope.Dispose();
            }
        }

        private async void Row_Click(object sender, EventArgs e)
        {
            if (_isRefreshing || _isCommandRunning)
                return;
            Control control = sender as Control;
            var item = control != null ? control.Tag as IoCylinderItem : null;
            if (item == null || !item.CanControl)
                return;

            _isCommandRunning = true;
            IDisposable recipeOperationScope = null;
            try
            {
                if (!TryBeginRecipePanelCommand(item, out recipeOperationScope))
                    return;

                if (item.ItemType == IoCylinderItemType.Output)
                {
                    bool current = item.StateGetter != null && item.StateGetter();
                    await WriteOutputAsync(item, !current);
                }
                else if (item.ItemType == IoCylinderItemType.Cylinder)
                {
                    await ToggleCylinderAsync(item);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", "Row click failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isCommandRunning = false;
                if (recipeOperationScope != null)
                    recipeOperationScope.Dispose();
            }
        }

        private bool TryBeginRecipePanelCommand(IoCylinderItem item, out IDisposable scope)
        {
            string reason;
            if (QMC.CDT320.Interlocks.MotionGuardRuntime.TryBeginRecipeSensitiveOperation(
                    "I/O Panel: " + (item != null ? item.DisplayName : string.Empty),
                    out scope, out reason))
                return true;

            QMC.Common.MessageDialog.Show(this, reason, Lang.T("controls.io.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private async Task WriteOutputAsync(IoCylinderItem item, bool value)
        {
            try
            {
                if (item == null || item.OutputWriter == null)
                    return;

                int result = await item.OutputWriter(value);
                EventLogger.Write(EventKind.Event, "QMC", "IO-PANEL", item.DisplayName + "=" + (value ? "ON" : "OFF"));
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, Lang.Format("controls.io.outputFailed", item.DisplayName), Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

                RefreshStates();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", "Output write failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task ToggleCylinderAsync(IoCylinderItem item)
        {
            try
            {
                if (item == null)
                    return;

                bool current = item.StateGetter != null && item.StateGetter();
                string commandName = current ? "Backward" : "Forward";
                Func<Task<int>> command = current ? item.BackwardCommand : item.ForwardCommand;
                await RunCommandAsync(item, commandName, command);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", "Cylinder toggle failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task RunCommandAsync(IoCylinderItem item, string commandName, Func<Task<int>> command)
        {
            try
            {
                if (item == null || command == null)
                    return;

                int result = await command();
                EventLogger.Write(EventKind.Event, "QMC", "IO-PANEL", item.DisplayName + "=" + commandName);
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, Lang.Format("controls.io.commandFailed", item.DisplayName, commandName), Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

                RefreshStates();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", "Cylinder command failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("controls.io.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void IoCylinderPanelControl_Resize(object sender, EventArgs e)
        {
            try
            {
                foreach (Control control in rowsHost.Controls)
                {
                    control.Width = ComputeRowWidth();
                    foreach (Control child in control.Controls)
                    {
                        Label label = child as Label;
                        if (label != null)
                            label.Width = Math.Max(40, control.Width - label.Left);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", "Resize failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private sealed class IoCylinderRow
        {
            public Panel Panel { get; private set; }
            public IndicatorDot Dot { get; private set; }
            public ToggleSwitch Toggle { get; private set; }
            public Label Label { get; private set; }

            public IoCylinderRow(Panel panel, IndicatorDot dot, ToggleSwitch toggle, Label label)
            {
                try
                {
                    Panel = panel;
                    Dot = dot;
                    Toggle = toggle;
                    Label = label;
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
}
