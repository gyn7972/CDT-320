using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class JogPositionListControl : UserControl
    {
        private readonly List<JogAxisItem> _items = new List<JogAxisItem>();
        private bool _wrapColumnsWhenMany = true;

        public bool WrapColumnsWhenMany
        {
            get
            {
                try
                {
                    return _wrapColumnsWhenMany;
                }
                catch
                {
                    return true;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _wrapColumnsWhenMany = value;
                    RebuildRows();
                    RefreshState();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-POS", "Wrap mode set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        public JogPositionListControl()
        {
            try
            {
                InitializeComponent();
                ApplyGridStyle();
                ApplyDesignTimeItems();
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

                RebuildRows();
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

        private void ApplyDesignTimeItems()
        {
            try
            {
                if (LicenseManager.UsageMode != LicenseUsageMode.Designtime || _items.Count > 0)
                    return;

                SetItems(new[]
                {
                    JogAxisItem.Single("StageY", null, AxisUnitConverter.Micrometer, 1.0, "Y+", "Y-"),
                    JogAxisItem.Single("StageT", null, AxisUnitConverter.Degree, 1.0, "T+", "T-"),
                    JogAxisItem.Single("ExpanderZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                    JogAxisItem.Single("CameraX", null, AxisUnitConverter.Micrometer, 1.0, "X+", "X-"),
                    JogAxisItem.Single("NeedleX", null, AxisUnitConverter.Micrometer, 1.0, "X+", "X-"),
                    JogAxisItem.Single("NeedleZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                    JogAxisItem.Single("EjectPinZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-"),
                    JogAxisItem.Single("CameraZ", null, AxisUnitConverter.Micrometer, 1.0, "Z+", "Z-")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-POS", "Design items failed: " + ex.Message);
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
                    JogAxisItem item2 = row.Cells[colAxis2.Index].Tag as JogAxisItem;

                    if (item != null)
                    {
                        row.Cells[colAxis.Index].Value = item.AxisName;
                        row.Cells[colPosition.Index].Value = FormatPosition(item);
                    }

                    if (item2 != null)
                    {
                        row.Cells[colAxis2.Index].Value = item2.AxisName;
                        row.Cells[colPosition2.Index].Value = FormatPosition(item2);
                    }
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
                grid.Columns[colPosition2.Index].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-POS", "Jog position style failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RebuildRows()
        {
            try
            {
                grid.Rows.Clear();

                bool wrapped = _wrapColumnsWhenMany && _items.Count > 4;
                colAxis2.Visible = wrapped;
                colPosition2.Visible = wrapped;

                if (!wrapped)
                {
                    foreach (JogAxisItem item in _items)
                    {
                        int index = grid.Rows.Add(item.AxisName, "0 " + item.DisplayUnit, string.Empty, string.Empty);
                        grid.Rows[index].Tag = item;
                    }

                    return;
                }

                int splitIndex = (_items.Count + 1) / 2;
                for (int index = 0; index < splitIndex; index++)
                {
                    JogAxisItem item = _items[index];
                    JogAxisItem item2 = index + splitIndex < _items.Count ? _items[index + splitIndex] : null;
                    int rowIndex = grid.Rows.Add(
                        item != null ? item.AxisName : string.Empty,
                        item != null ? "0 " + item.DisplayUnit : string.Empty,
                        item2 != null ? item2.AxisName : string.Empty,
                        item2 != null ? "0 " + item2.DisplayUnit : string.Empty);
                    grid.Rows[rowIndex].Tag = item;
                    grid.Rows[rowIndex].Cells[colAxis2.Index].Tag = item2;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static string FormatPosition(JogAxisItem item)
        {
            try
            {
                double value = item.GetDisplayPosition();
                return value.ToString("0.###", CultureInfo.InvariantCulture) + " " + item.DisplayUnit;
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


