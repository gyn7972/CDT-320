using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class IoCylinderPanelControl : UserControl
    {
        private readonly List<IoCylinderItem> _items = new List<IoCylinderItem>();
        private readonly Dictionary<IoCylinderItem, IoCylinderRow> _rows = new Dictionary<IoCylinderItem, IoCylinderRow>();
        private bool _isRefreshing;
        private bool _isCommandRunning;

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

        public void SetItems(IEnumerable<IoCylinderItem> items)
        {
            try
            {
                _items.Clear();
                _rows.Clear();
                rowsHost.Controls.Clear();

                if (items != null)
                    _items.AddRange(items);

                foreach (var item in _items)
                    AddRow(item);

                RefreshStates();
            }
            catch (Exception ex)
            {
                string message = "I/O panel set failed: " + Name + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", message);
                QMC.Common.MessageDialog.Show(this, message, "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, message, "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                var dot = new IndicatorDot();
                var label = new Label();

                rowPanel.Height = 30;
                rowPanel.Width = Math.Max(1, rowsHost.ClientSize.Width - 2);
                rowPanel.Margin = new Padding(0);
                rowPanel.BackColor = Color.FromArgb(205, 205, 205);
                rowPanel.Tag = item;

                dot.Location = new Point(6, 7);
                dot.Size = new Size(16, 16);
                dot.OnColor = Color.LimeGreen;
                dot.OffColor = Color.FromArgb(85, 85, 85);
                dot.BackColor = Color.Transparent;
                dot.Tag = item;

                label.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                label.BackColor = Color.FromArgb(205, 205, 205);
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                label.ForeColor = Color.FromArgb(20, 24, 28);
                label.Location = new Point(32, 0);
                label.Size = new Size(Math.Max(40, rowPanel.Width - 32), 30);
                label.Padding = new Padding(8, 0, 6, 0);
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.Text = item.DisplayName ?? string.Empty;
                label.Tag = item;

                rowPanel.Controls.Add(dot);
                rowPanel.Controls.Add(label);
                rowPanel.Click += Row_Click;
                dot.Click += Row_Click;
                label.Click += Row_Click;
                rowPanel.ContextMenuStrip = BuildContextMenu(item);
                label.ContextMenuStrip = rowPanel.ContextMenuStrip;
                dot.ContextMenuStrip = rowPanel.ContextMenuStrip;

                rowsHost.Controls.Add(rowPanel);
                _rows[item] = new IoCylinderRow(rowPanel, dot, label);
            }
            catch (Exception ex)
            {
                string message = "I/O row add failed: " + (item != null ? item.DisplayName : string.Empty) + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", message);
                QMC.Common.MessageDialog.Show(this, message, "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                bool on = item.StateGetter != null && item.StateGetter();
                IoCylinderRow row = _rows[item];
                row.Dot.IsOn = on;
                row.Label.Text = GetDisplayText(item, on);
                row.Label.BackColor = on ? Color.FromArgb(219, 246, 226) : Color.FromArgb(205, 205, 205);
                row.Label.ForeColor = on ? Color.FromArgb(20, 115, 55) : Color.FromArgb(20, 24, 28);
            }
            catch (Exception ex)
            {
                string message = "I/O row refresh failed: " + (item != null ? item.DisplayName : string.Empty) + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", message);
                QMC.Common.MessageDialog.Show(this, message, "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    return (item.DisplayName ?? string.Empty) + " : " + (on ? "ON" : "OFF");

                if (item.ItemType == IoCylinderItemType.Cylinder)
                    return (item.DisplayName ?? string.Empty) + " : " + (on ? "FWD" : "BWD/OFF");

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

        private async void Row_Click(object sender, EventArgs e)
        {
            try
            {
                if (_isRefreshing || _isCommandRunning)
                    return;

                Control control = sender as Control;
                var item = control != null ? control.Tag as IoCylinderItem : null;
                if (item == null || !item.CanControl)
                    return;

                _isCommandRunning = true;
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isCommandRunning = false;
            }
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
                    QMC.Common.MessageDialog.Show(this, item.DisplayName + " output failed.", "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                RefreshStates();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", "Output write failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    QMC.Common.MessageDialog.Show(this, item.DisplayName + " " + commandName + " failed.", "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                RefreshStates();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "IO-PANEL", "Cylinder command failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "I/O Panel", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    control.Width = Math.Max(1, rowsHost.ClientSize.Width - 2);
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
            public Label Label { get; private set; }

            public IoCylinderRow(Panel panel, IndicatorDot dot, Label label)
            {
                try
                {
                    Panel = panel;
                    Dot = dot;
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


