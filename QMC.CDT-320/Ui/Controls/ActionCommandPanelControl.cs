using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.CDT_320.Ui.Security;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class ActionCommandPanelControl : UserControl
    {
        private readonly Dictionary<string, ActionButton> _buttons = new Dictionary<string, ActionButton>();
        private readonly Dictionary<ActionButton, ActionCommandItem> _items = new Dictionary<ActionButton, ActionCommandItem>();
        private bool _isExecuting;

        public int CommandColumns { get; private set; }
        public int CommandRows { get; private set; }
        public Padding CommandMargin { get; set; }
        public Font CommandFont { get; set; }

        public ActionCommandPanelControl()
        {
            try
            {
                InitializeComponent();
                CommandColumns = 1;
                CommandRows = 1;
                CommandMargin = new Padding(4);
                CommandFont = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public void SetItems(IEnumerable<ActionCommandItem> items, int columns)
        {
            try
            {
                int rows = CalculateRows(items);
                SetItems(items, columns, rows);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public void SetItems(IEnumerable<ActionCommandItem> items, int columns, int rows)
        {
            try
            {
                if (columns <= 0)
                    columns = 1;
                if (rows <= 0)
                    rows = 1;

                ClearItems();
                CommandColumns = columns;
                CommandRows = rows;
                ConfigureLayout(columns, rows);

                if (items == null)
                    return;

                foreach (ActionCommandItem item in items)
                    AddItem(item);
            }
            catch (Exception ex)
            {
                string message = "Action command bind failed: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "ACTION-PANEL", "ActionCommandPanel", message);
                QMC.Common.MessageDialog.Show(this, message, "Action Command", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        public void SetCommandEnabled(string key, bool enabled)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return;
                if (!_buttons.ContainsKey(key))
                    return;

                _buttons[key].Enabled = enabled;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "ACTION-PANEL", "Set command enabled failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public ActionButton GetButton(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return null;
                return _buttons.ContainsKey(key) ? _buttons[key] : null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private int CalculateRows(IEnumerable<ActionCommandItem> items)
        {
            try
            {
                int maxRow = 0;
                if (items != null)
                {
                    foreach (ActionCommandItem item in items)
                    {
                        if (item != null && item.Row > maxRow)
                            maxRow = item.Row;
                    }
                }

                return maxRow + 1;
            }
            catch
            {
                return 1;
            }
            finally
            {
            }
        }

        private void ConfigureLayout(int columns, int rows)
        {
            try
            {
                rootLayout.SuspendLayout();
                rootLayout.Controls.Clear();
                rootLayout.ColumnStyles.Clear();
                rootLayout.RowStyles.Clear();
                rootLayout.ColumnCount = columns;
                rootLayout.RowCount = rows;

                for (int column = 0; column < columns; column++)
                    rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));

                for (int row = 0; row < rows; row++)
                    rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
            }
            catch
            {
                throw;
            }
            finally
            {
                rootLayout.ResumeLayout(false);
            }
        }

        private void AddItem(ActionCommandItem item)
        {
            try
            {
                if (item == null)
                    return;

                string key = string.IsNullOrWhiteSpace(item.Key) ? item.Text : item.Key;
                if (string.IsNullOrWhiteSpace(key))
                    key = Guid.NewGuid().ToString("N");

                ActionButton button = new ActionButton();
                button.Dock = DockStyle.Fill;
                button.Font = CommandFont;
                button.Margin = CommandMargin;
                button.Name = "btnAction" + SanitizeName(key);
                button.Text = string.IsNullOrWhiteSpace(item.Text) ? key : item.Text;
                button.Enabled = item.Enabled;
                button.Tag = key;
                button.Click += ActionButton_Click;

                int column = Math.Max(0, Math.Min(item.Column, Math.Max(0, CommandColumns - 1)));
                int row = Math.Max(0, Math.Min(item.Row, Math.Max(0, CommandRows - 1)));
                int span = Math.Max(1, Math.Min(item.ColumnSpan <= 0 ? 1 : item.ColumnSpan, CommandColumns - column));

                rootLayout.Controls.Add(button, column, row);
                if (span > 1)
                    rootLayout.SetColumnSpan(button, span);

                _buttons[key] = button;
                _items[button] = item;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void ClearItems()
        {
            try
            {
                foreach (ActionButton button in _items.Keys)
                    button.Click -= ActionButton_Click;

                _items.Clear();
                _buttons.Clear();
                rootLayout.Controls.Clear();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async void ActionButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_isExecuting)
                    return;

                ActionButton button = sender as ActionButton;
                if (button == null || !_items.ContainsKey(button))
                    return;

                await ExecuteItemAsync(button, _items[button]);
            }
            catch (Exception ex)
            {
                ShowCommandError("Action command failed", ex);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteItemAsync(ActionButton button, ActionCommandItem item)
        {
            try
            {
                if (item == null)
                    return -1;

                _isExecuting = true;
                SetAllButtonsEnabled(false);
                button.Enabled = false;

                string key = string.IsNullOrWhiteSpace(item.Key) ? item.Text : item.Key;
                EventLogger.Write(EventKind.Event, UserSession.Name, "ACTION-PANEL", "Click: " + key);

                int result = 0;
                if (item.ExecuteAsync != null)
                    result = await item.ExecuteAsync();

                if (result != 0)
                {
                    string message = key + " failed. Result=" + result;
                    EventLogger.Write(EventKind.Alarm, "UI", "ACTION-PANEL", "ActionCommandPanel", message);
                    AlarmManager.Raise(AlarmSeverity.Error, "ActionCommandFail", "ActionCommandPanel", message);
                    QMC.Common.MessageDialog.Show(this, message, "Action Command", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                return result;
            }
            catch (Exception ex)
            {
                ShowCommandError(item != null ? item.Text : "Action command", ex);
                return -1;
            }
            finally
            {
                _isExecuting = false;
                RestoreButtonEnabledStates();
            }
        }

        private void SetAllButtonsEnabled(bool enabled)
        {
            try
            {
                foreach (ActionButton button in _items.Keys)
                    button.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void RestoreButtonEnabledStates()
        {
            try
            {
                foreach (KeyValuePair<ActionButton, ActionCommandItem> pair in _items)
                    pair.Key.Enabled = pair.Value.Enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private string SanitizeName(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return "Command";

                string result = string.Empty;
                foreach (char ch in value)
                {
                    if (char.IsLetterOrDigit(ch))
                        result += ch;
                }

                return string.IsNullOrEmpty(result) ? "Command" : result;
            }
            catch
            {
                return "Command";
            }
            finally
            {
            }
        }

        private void ShowCommandError(string title, Exception ex)
        {
            try
            {
                string message = title + ": " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "ACTION-PANEL", "ActionCommandPanel", message);
                AlarmManager.Raise(AlarmSeverity.Error, "ActionCommandException", "ActionCommandPanel", message);
                QMC.Common.MessageDialog.Show(this, message, "Action Command", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}


