using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Controls;
using QMC.Common;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class PickerZoneSetupDialog : Form
    {
        private readonly MachineController _controller;
        private readonly Timer _refreshTimer;
        private bool _loading;

        public PickerZoneSetupDialog(MachineController controller)
        {
            try
            {
                _controller = controller;
                InitializeComponent();

                cboSide.SelectedIndex = 0;
                LoadSelectedSetup();

                _refreshTimer = new Timer();
                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += OnRefreshTimerTick;
                _refreshTimer.Start();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (_refreshTimer != null)
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Tick -= OnRefreshTimerTick;
                    _refreshTimer.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                base.OnFormClosed(e);
            }
        }

        private void OnSideChanged(object sender, EventArgs e)
        {
            LoadSelectedSetup();
        }

        private void OnReloadClick(object sender, EventArgs e)
        {
            LoadSelectedSetup();
        }

        private void OnToleranceClick(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new NumericKeypadDialog("Zone Tolerance", txtTolerance.Text, "mm"))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    double value;
                    if (!TryParseDouble(dlg.ValueText, out value) || value <= 0.0)
                    {
                        MessageDialog.Show("Zone 허용오차 값이 올바르지 않습니다.", "Picker Zone",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    txtTolerance.Text = FormatNumber(value);
                    UpdateCurrentDisplay();
                }
            }
            catch (Exception ex)
            {
                MessageDialog.Show("Zone 허용오차 수정 실패: " + ex.Message, "Picker Zone",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0)
                    return;
                if (e.ColumnIndex != colMin.Index && e.ColumnIndex != colMax.Index)
                    return;

                string currentText = Convert.ToString(gridZones.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                using (var dlg = new NumericKeypadDialog(
                    Convert.ToString(gridZones.Rows[e.RowIndex].Cells[colZone.Index].Value) + " " +
                    gridZones.Columns[e.ColumnIndex].HeaderText,
                    currentText,
                    "mm"))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    double value;
                    if (!TryParseDouble(dlg.ValueText, out value))
                    {
                        MessageDialog.Show("X 위치 값이 올바르지 않습니다.", "Picker Zone",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    gridZones.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = FormatNumber(value);
                    UpdateCurrentDisplay();
                }
            }
            catch (Exception ex)
            {
                MessageDialog.Show("Zone 값 수정 실패: " + ex.Message, "Picker Zone",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void OnGridCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            OnGridCellClick(sender, e);
        }

        private void OnTeachMinClick(object sender, EventArgs e)
        {
            TeachSelectedColumn(colMin.Index);
        }

        private void OnTeachMaxClick(object sender, EventArgs e)
        {
            TeachSelectedColumn(colMax.Index);
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            try
            {
                if (MessageDialog.Show("Picker X Zone 설정을 저장하시겠습니까?", "Picker Zone",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                    return;

                PickerZoneXSetup setup = GetSelectedSetup();
                if (setup == null)
                {
                    MessageDialog.Show("Picker setup을 찾을 수 없습니다.", "Picker Zone",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Ranges가 겹친다. 어쩔수없다.
                //string validationMessage;
                //if (!ValidateGridRanges(out validationMessage))
                //{
                //    MessageDialog.Show(validationMessage, "Picker Zone",
                //        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                ApplyGridToSetup(setup);

                Form1 host = Owner as Form1;
                if (host != null && !string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                    host.SaveMachineRecipe(host.CurrentRecipeName);

                lblStatus.Text = "저장 완료.";
                UpdateCurrentDisplay();
            }
            catch (Exception ex)
            {
                MessageDialog.Show("Picker X Zone 저장 실패: " + ex.Message, "Picker Zone",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void OnCloseClick(object sender, EventArgs e)
        {
            Close();
        }

        private void OnRefreshTimerTick(object sender, EventArgs e)
        {
            try
            {
                UpdateCurrentDisplay();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void LoadSelectedSetup()
        {
            try
            {
                _loading = true;
                PickerZoneXSetup setup = GetSelectedSetup();
                if (setup == null)
                {
                    gridZones.Rows.Clear();
                    lblStatus.Text = "Picker setup을 찾을 수 없습니다.";
                    return;
                }

                setup.Ensure();
                chkUseEncoderZone.Checked = setup.UseEncoderZone;
                txtTolerance.Text = FormatNumber(setup.ZoneTolerance);
                gridZones.Rows.Clear();
                AddZoneRow("Avoid", "AVOID", setup.Avoid);
                AddZoneRow("Input", "PICKUP", setup.Input);
                AddZoneRow("Bottom", "INSPECT_B", setup.Bottom);
                AddZoneRow("Side", "INSPECT_S", setup.Side);
                AddZoneRow("Output", "PLACE", setup.Output);
                lblStatus.Text = "불러오기 완료.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "불러오기 실패: " + ex.Message;
            }
            finally
            {
                _loading = false;
                UpdateCurrentDisplay();
            }
        }

        private void AddZoneRow(string key, string displayName, PickerZoneXRange range)
        {
            if (range == null)
                range = new PickerZoneXRange();

            int rowIndex = gridZones.Rows.Add(
                range.Enabled,
                displayName,
                FormatNumber(range.MinX),
                FormatNumber(range.MaxX),
                "-",
                "-");
            gridZones.Rows[rowIndex].Tag = key;
        }

        private void ApplyGridToSetup(PickerZoneXSetup setup)
        {
            setup.UseEncoderZone = chkUseEncoderZone.Checked;

            double tolerance;
            if (!TryParseDouble(txtTolerance.Text, out tolerance) || tolerance <= 0.0)
                tolerance = 1.0;
            setup.ZoneTolerance = tolerance;
            setup.Ensure();

            foreach (DataGridViewRow row in gridZones.Rows)
            {
                if (row == null || row.IsNewRow)
                    continue;

                PickerZoneXRange range = ResolveRange(setup, Convert.ToString(row.Tag));
                if (range == null)
                    continue;

                double min;
                double max;
                TryParseDouble(Convert.ToString(row.Cells[colMin.Index].Value), out min);
                TryParseDouble(Convert.ToString(row.Cells[colMax.Index].Value), out max);

                range.Enabled = IsRangeRowEnabled(row);
                range.MinX = min;
                range.MaxX = max;
            }
        }

        private PickerZoneXRange ResolveRange(PickerZoneXSetup setup, string key)
        {
            if (setup == null)
                return null;
            switch (key)
            {
                case "Avoid":
                    return setup.Avoid;
                case "Input":
                    return setup.Input;
                case "Bottom":
                    return setup.Bottom;
                case "Side":
                    return setup.Side;
                case "Output":
                    return setup.Output;
                default:
                    return null;
            }
        }

        private PickerZoneXSetup GetSelectedSetup()
        {
            CDT320_Machine machine = _controller != null ? _controller.Machine : null;
            if (machine == null)
                return null;

            bool isFront = cboSide == null || cboSide.SelectedIndex <= 0;
            if (isFront)
            {
                if (machine.PickerFrontUnit == null || machine.PickerFrontUnit.Setup == null)
                    return null;
                machine.PickerFrontUnit.Setup.EnsureGeometryData();
                return machine.PickerFrontUnit.Setup.ZoneX;
            }

            if (machine.PickerRearUnit == null || machine.PickerRearUnit.Setup == null)
                return null;
            machine.PickerRearUnit.Setup.EnsureGeometryData();
            return machine.PickerRearUnit.Setup.ZoneX;
        }

        private BaseAxis GetSelectedPickerX()
        {
            CDT320_Machine machine = _controller != null ? _controller.Machine : null;
            if (machine == null)
                return null;

            bool isFront = cboSide == null || cboSide.SelectedIndex <= 0;
            if (isFront)
                return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null;

            return machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerX : null;
        }

        private void TeachSelectedColumn(int columnIndex)
        {
            try
            {
                if (gridZones.CurrentRow == null)
                    return;

                BaseAxis pickerX = GetSelectedPickerX();
                if (pickerX == null)
                {
                    MessageDialog.Show("PickerX 축을 찾을 수 없습니다.", "Picker Zone",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                gridZones.CurrentRow.Cells[columnIndex].Value = FormatNumber(pickerX.ActualPosition);
                gridZones.CurrentRow.Cells[colUse.Index].Value = true;
                lblStatus.Text = Convert.ToString(gridZones.CurrentRow.Cells[colZone.Index].Value) + " 티칭 완료.";
                UpdateCurrentDisplay();
            }
            catch (Exception ex)
            {
                MessageDialog.Show("Picker Zone 티칭 실패: " + ex.Message, "Picker Zone",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void UpdateCurrentDisplay()
        {
            if (_loading)
                return;

            BaseAxis pickerX = GetSelectedPickerX();
            if (pickerX == null)
            {
                lblCurrent.Text = "Current X: -";
                return;
            }

            double actual = pickerX.ActualPosition;
            lblCurrent.Text = "Current X: " + FormatNumber(actual) + " mm";

            double tolerance;
            if (!TryParseDouble(txtTolerance.Text, out tolerance) || tolerance <= 0.0)
                tolerance = 1.0;

            List<DataGridViewRow> matchedRows = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in gridZones.Rows)
            {
                if (row == null || row.IsNewRow)
                    continue;

                row.Cells[colCurrent.Index].Value = FormatNumber(actual);
                double min;
                double max;
                TryParseDouble(Convert.ToString(row.Cells[colMin.Index].Value), out min);
                TryParseDouble(Convert.ToString(row.Cells[colMax.Index].Value), out max);

                PickerZoneXRange temp = new PickerZoneXRange
                {
                    Enabled = IsRangeRowEnabled(row),
                    MinX = min,
                    MaxX = max
                };
                bool match = temp.Enabled && temp.Contains(actual, tolerance);
                if (match)
                    matchedRows.Add(row);
                row.Cells[colMatch.Index].Value = match ? "Y" : "-";
            }

            if (matchedRows.Count > 1)
            {
                foreach (DataGridViewRow row in matchedRows)
                    row.Cells[colMatch.Index].Value = "OVERLAP";
                lblStatus.Text = "현재 X 위치가 여러 Zone에 겹칩니다. Zone 범위를 다시 설정하세요.";
            }
        }

        private bool ValidateGridRanges(out string message)
        {
            message = string.Empty;
            try
            {
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < gridZones.Rows.Count; i++)
                {
                    DataGridViewRow first = gridZones.Rows[i];
                    if (!IsRangeRowEnabled(first))
                        continue;

                    double firstMin;
                    double firstMax;
                    if (!TryGetRowRange(first, out firstMin, out firstMax))
                    {
                        message = Convert.ToString(first.Cells[colZone.Index].Value) + " Zone 범위 값이 올바르지 않습니다.";
                        return false;
                    }

                    for (int j = i + 1; j < gridZones.Rows.Count; j++)
                    {
                        DataGridViewRow second = gridZones.Rows[j];
                        if (!IsRangeRowEnabled(second))
                            continue;

                        double secondMin;
                        double secondMax;
                        if (!TryGetRowRange(second, out secondMin, out secondMax))
                        {
                            message = Convert.ToString(second.Cells[colZone.Index].Value) + " Zone 범위 값이 올바르지 않습니다.";
                            return false;
                        }

                        if (RangesOverlap(firstMin, firstMax, secondMin, secondMax))
                            builder.AppendLine(Convert.ToString(first.Cells[colZone.Index].Value) + " / " +
                                               Convert.ToString(second.Cells[colZone.Index].Value));
                    }
                }

                if (builder.Length <= 0)
                    return true;

                message = "Picker X Zone 범위가 겹쳐 저장할 수 없습니다.\r\n\r\n" +
                          builder +
                          "\r\n각 Zone의 Min/Max를 서로 겹치지 않게 다시 설정하세요.";
                return false;
            }
            catch (Exception ex)
            {
                message = "Picker X Zone 범위 검증 실패: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool IsRangeRowEnabled(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow)
                return false;
            object value = row.Cells[colUse.Index].Value;
            return value != null && Convert.ToBoolean(value);
        }

        private bool TryGetRowRange(DataGridViewRow row, out double min, out double max)
        {
            min = 0.0;
            max = 0.0;
            if (row == null)
                return false;

            if (!TryParseDouble(Convert.ToString(row.Cells[colMin.Index].Value), out min))
                return false;
            if (!TryParseDouble(Convert.ToString(row.Cells[colMax.Index].Value), out max))
                return false;

            return true;
        }

        private static bool RangesOverlap(double firstMin, double firstMax, double secondMin, double secondMax)
        {
            double aMin = Math.Min(firstMin, firstMax);
            double aMax = Math.Max(firstMin, firstMax);
            double bMin = Math.Min(secondMin, secondMax);
            double bMax = Math.Max(secondMin, secondMax);

            return aMin <= bMax && bMin <= aMax;
        }

        private static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
