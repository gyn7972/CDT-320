using QMC.Common.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class JogPositionListControl : UserControl
    {
        private readonly List<JogAxisItem> _items = new List<JogAxisItem>();

        public JogPositionListControl()
        {
            try
            {
                InitializeComponent();
                ApplyGridStyle();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public void SetItems(IEnumerable<JogAxisItem> items)
        {
            try
            {
                _items.Clear();
                grid.Rows.Clear();

                if (items != null)
                    _items.AddRange(items);

                foreach (JogAxisItem item in _items)
                {
                    int index = grid.Rows.Add(item.AxisName, "0 " + item.Unit);
                    grid.Rows[index].Tag = item;
                }

                RefreshState();
            }
            catch (Exception ex)
            {
                string message = "Jog position bind failed: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "JOG-POS", message);
                QMC.Common.MessageDialog.Show(this, message, "Jog Position", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        public void RefreshState()
        {
            try
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    JogAxisItem item = row.Tag as JogAxisItem;
                    if (item == null)
                        continue;

                    row.Cells[colAxis.Index].Value = item.AxisName;
                    row.Cells[colPosition.Index].Value = FormatPosition(item);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-POS", "Jog position refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyGridStyle()
        {
            try
            {
                grid.DefaultCellStyle.BackColor = Color.FromArgb(132, 136, 140);
                grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 35, 40);
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(132, 136, 140);
                grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 35, 40);
                grid.DefaultCellStyle.Font = new Font("Malgun Gothic", 8.5F, FontStyle.Bold);
                grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                grid.Columns[colPosition.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-POS", "Jog position style failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static string FormatPosition(JogAxisItem item)
        {
            try
            {
                double scale = item.DisplayScale <= 0 ? 1.0 : item.DisplayScale;
                double value = item.GetActualPosition() * scale;
                return value.ToString("0.###", CultureInfo.InvariantCulture) + " " + item.Unit;
            }
            catch
            {
                return "0";
            }
            finally
            {
            }
        }
    }
}


