using QMC.CDT_320.Ui.Localization;
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
            InitializeLanguageBindings();

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
                EditPositiveDistance(txtTolerance, "Zone Tolerance", "Zone 허용오차 값이 올바르지 않습니다.");
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

        private void OnXClearanceClick(object sender, EventArgs e)
        {
            try
            {
                EditPositiveDistance(
                    txtXClearance,
                    "Front/Rear Picker Facing X",
                    "Front/Rear PickerY 동시 전진 금지 X거리 값이 올바르지 않습니다.");
            }
            catch (Exception ex)
            {
                MessageDialog.Show("Front/Rear PickerY 동시 전진 금지 X거리 수정 실패: " + ex.Message, "Picker Zone",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void OnYOutDistanceClick(object sender, EventArgs e)
        {
            try
            {
                EditPositiveDistance(txtYOutDistance, "Picker Y Out Distance", "Picker Y 돌출 판정 거리 값이 올바르지 않습니다.");
            }
            catch (Exception ex)
            {
                MessageDialog.Show("Picker Y 돌출 판정 거리 수정 실패: " + ex.Message, "Picker Zone",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void EditPositiveDistance(TextBox targetTextBox, string title, string invalidMessage)
        {
            try
            {
                using (var dlg = new NumericKeypadDialog(title, targetTextBox != null ? targetTextBox.Text : string.Empty, "mm"))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    double value;
                    if (!TryParseDouble(dlg.ValueText, out value) || value <= 0.0)
                    {
                        MessageDialog.Show(invalidMessage, "Picker Zone",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (targetTextBox != null)
                        targetTextBox.Text = FormatNumber(value);
                    UpdateCurrentDisplay();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(title + " 수정 중 예외가 발생했습니다. " + ex.Message, ex);
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

                string validationMessage;
                if (!ValidateGridRanges(out validationMessage))
                {
                    MessageDialog.Show(validationMessage, "Picker Zone",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ApplyGridToSetup(setup);
                ApplySafetyToSelectedPickerSetup();

                Form1 host = Owner as Form1;
                if (host != null && !string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                    host.SaveMachineRecipe(host.ActiveRecipeName);

                Lang.BindKey(lblStatus, "extraDialog.pickerZone.saved");
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
                    Lang.BindKey(lblStatus, "extraDialog.pickerZone.missing");
                    return;
                }

                setup.Ensure();
                chkUseEncoderZone.Checked = setup.UseEncoderZone;
                txtTolerance.Text = FormatNumber(setup.ZoneTolerance);
                LoadSelectedSafetySettings();
                gridZones.Rows.Clear();
                AddZoneRow("Avoid", "AVOID", setup.Avoid);
                AddZoneRow("Input", "PICKUP", setup.Input);
                AddZoneRow("Bottom", "INSPECT_B", setup.Bottom);
                AddZoneRow("Side", "INSPECT_S", setup.Side);
                AddZoneRow("Output", "PLACE", setup.Output);

                // 현재 기준(사용자 지시 2026-07-29): 저장된 설정에 판정 불가 구간(갭/겹침)이 있으면
                // 다이얼로그를 여는 즉시 상태줄로 알린다 — 신규 검증 도입 이전에 저장된 갭 인지용.
                string adjacencyMessage;
                if (!ValidateGridRanges(out adjacencyMessage))
                    Lang.BindKey(lblStatus, "extraDialog.pickerZone.invalidLoaded");
                else
                    Lang.BindKey(lblStatus, "extraDialog.pickerZone.loaded");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "extraDialog.pickerZone.loadFailed", ex.Message);
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

        private void LoadSelectedSafetySettings()
        {
            try
            {
                CDT320_Machine machine = _controller != null ? _controller.Machine : null;
                bool isFront = cboSide == null || cboSide.SelectedIndex <= 0;

                if (machine == null)
                {
                    txtXClearance.Text = FormatNumber(150.0);
                    txtYOutDistance.Text = FormatNumber(1.0);
                    return;
                }

                if (isFront)
                {
                    if (machine.PickerFrontUnit == null || machine.PickerFrontUnit.Setup == null)
                        return;

                    machine.PickerFrontUnit.Setup.EnsureGeometryData();
                    txtXClearance.Text = FormatNumber(machine.PickerFrontUnit.Setup.PickerYFacingXClearance);
                    txtYOutDistance.Text = FormatNumber(machine.PickerFrontUnit.Setup.PickerYOutDistance);
                    return;
                }

                if (machine.PickerRearUnit == null || machine.PickerRearUnit.Setup == null)
                    return;

                machine.PickerRearUnit.Setup.EnsureGeometryData();
                txtXClearance.Text = FormatNumber(machine.PickerRearUnit.Setup.PickerYFacingXClearance);
                txtYOutDistance.Text = FormatNumber(machine.PickerRearUnit.Setup.PickerYOutDistance);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "extraDialog.pickerZone.clearanceFailed", ex.Message);
            }
            finally
            {
            }
        }

        private void ApplySafetyToSelectedPickerSetup()
        {
            try
            {
                CDT320_Machine machine = _controller != null ? _controller.Machine : null;
                if (machine == null)
                    return;

                double xClearance;
                if (!TryParseDouble(txtXClearance.Text, out xClearance) || xClearance <= 0.0)
                    xClearance = 150.0;

                double yOutDistance;
                if (!TryParseDouble(txtYOutDistance.Text, out yOutDistance) || yOutDistance <= 0.0)
                    yOutDistance = 1.0;

                bool isFront = cboSide == null || cboSide.SelectedIndex <= 0;
                if (isFront)
                {
                    if (machine.PickerFrontUnit == null || machine.PickerFrontUnit.Setup == null)
                        return;

                    machine.PickerFrontUnit.Setup.EnsureGeometryData();
                    machine.PickerFrontUnit.Setup.PickerYFacingXClearance = xClearance;
                    machine.PickerFrontUnit.Setup.PickerYOutDistance = yOutDistance;
                    return;
                }

                if (machine.PickerRearUnit == null || machine.PickerRearUnit.Setup == null)
                    return;

                machine.PickerRearUnit.Setup.EnsureGeometryData();
                machine.PickerRearUnit.Setup.PickerYFacingXClearance = xClearance;
                machine.PickerRearUnit.Setup.PickerYOutDistance = yOutDistance;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Picker 안전거리 설정 적용 중 예외가 발생했습니다. " + ex.Message, ex);
            }
            finally
            {
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
                Lang.BindFormat(lblStatus, "extraDialog.pickerZone.taught", Convert.ToString(gridZones.CurrentRow.Cells[colZone.Index].Value));
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
                Lang.BindKey(lblCurrent, "extraDialog.pickerZone.noCurrent");
                return;
            }

            double actual = pickerX.ActualPosition;
            Lang.BindFormat(lblCurrent, "extraDialog.pickerZone.current", FormatNumber(actual));

            double tolerance;
            if (!TryParseDouble(txtTolerance.Text, out tolerance) || tolerance <= 0.0)
                tolerance = 1.0;

            List<DataGridViewRow> matchedRows = new List<DataGridViewRow>();
            List<double> matchedMins = new List<double>();
            List<double> matchedMaxs = new List<double>();
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
                {
                    matchedRows.Add(row);
                    matchedMins.Add(Math.Min(min, max));
                    matchedMaxs.Add(Math.Max(min, max));
                }
                row.Cells[colMatch.Index].Value = match ? "Y" : "-";
            }

            if (matchedRows.Count > 1)
            {
                // 현재 기준(사용자 지시 2026-07-29): 맞닿은 경계의 허용오차 밴드는 raw 범위 포함 존
                // (공유 경계점은 그 값에서 시작하는 존)으로 확정 표시한다 — 런타임 판정 타이브레이크와 동일.
                // 진짜 겹침 설정일 때만 OVERLAP 경고를 유지한다.
                int rawIndex = -1;
                int rawCount = 0;
                int startIndex = -1;
                int startCount = 0;
                for (int i = 0; i < matchedRows.Count; i++)
                {
                    if (actual < matchedMins[i] || actual > matchedMaxs[i])
                        continue;

                    rawCount++;
                    rawIndex = i;
                    if (Math.Abs(matchedMins[i] - actual) <= 0.000001)
                    {
                        startCount++;
                        startIndex = i;
                    }
                }

                int winner = rawCount == 1 ? rawIndex : (rawCount > 1 && startCount == 1 ? startIndex : -1);
                if (winner >= 0)
                {
                    for (int i = 0; i < matchedRows.Count; i++)
                        matchedRows[i].Cells[colMatch.Index].Value = i == winner ? "Y" : "-";
                }
                else
                {
                    foreach (DataGridViewRow row in matchedRows)
                        row.Cells[colMatch.Index].Value = "OVERLAP";
                    Lang.BindKey(lblStatus, "extraDialog.pickerZone.overlap");
                }
            }
        }

        // 존 범위 행 스냅샷 — 인접 계약 검증용.
        private sealed class ZoneRangeEntry
        {
            public string Name;
            public double Min;
            public double Max;
        }

        // 기존 조건: 확장(허용오차 포함) 범위가 서로 겹치지만 않으면 저장을 허용했다 — 인접 존 사이에
        //           2x허용오차 초과의 "판정 불가 갭"이 구조적으로 강제됐고, 그 갭에 떨어진 목표는
        //           Encoder Zone Unknown -> Y존 폴백 오판으로 이어졌다(실장비 2026-07-29 15:29:58,
        //           FrontPickerX 팔로잉 중간좌표 877.9 -> Side(835)~Output(900) 갭 -> Input 오판 Critical).
        // 현재 기준(사용자 지시 2026-07-29, 갭 강제 폐지): Encoder Zone 사용 시 인접 활성 존은
        //           다음 MinX == 이전 MaxX (같은 값으로 맞닿음) 여야 저장할 수 있다 — 갭/겹침 모두 거부.
        //           경계 ±허용오차 밴드의 이중매칭은 판정식 타이브레이크(raw 범위 포함 존 확정,
        //           PickerZoneInterlockRules.TryResolveEncoderXZoneByPosition)가 해소하므로
        //           맞닿은 배치에서 판정 불가 X 좌표는 존재하지 않는다.
        //           Encoder Zone 미사용 시 범위는 존 판정에 쓰이지 않으므로 기존 겹침 검사만 유지한다.
        private bool ValidateGridRanges(out string message)
        {
            message = string.Empty;
            try
            {
                double tolerance;
                if (!TryParseDouble(txtTolerance.Text, out tolerance) || tolerance <= 0.0)
                {
                    message = "Zone 허용오차 값이 올바르지 않습니다.";
                    return false;
                }

                List<ZoneRangeEntry> entries = new List<ZoneRangeEntry>();
                foreach (DataGridViewRow row in gridZones.Rows)
                {
                    if (!IsRangeRowEnabled(row))
                        continue;

                    double min;
                    double max;
                    if (!TryGetRowRange(row, out min, out max))
                    {
                        message = Convert.ToString(row.Cells[colZone.Index].Value) + " Zone 범위 값이 올바르지 않습니다.";
                        return false;
                    }

                    entries.Add(new ZoneRangeEntry
                    {
                        Name = Convert.ToString(row.Cells[colZone.Index].Value),
                        Min = Math.Min(min, max),
                        Max = Math.Max(min, max)
                    });
                }

                if (chkUseEncoderZone.Checked)
                    return ValidateEncoderZoneAdjacency(entries, out message);

                return ValidateLegacyOverlap(entries, tolerance, out message);
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

        // 인터락 기준: Encoder Zone 사용 시 모든 X 좌표가 정확히 한 Zone으로 판정되도록
        //             인접 활성 존을 같은 경계값으로 맞닿게(다음 MinX == 이전 MaxX) 강제한다
        //             (갭/겹침 모두 저장 거부 — 경계 밴드는 판정식 타이브레이크가 확정).
        private static bool ValidateEncoderZoneAdjacency(List<ZoneRangeEntry> entries, out string message)
        {
            message = string.Empty;
            if (entries == null || entries.Count < 2)
                return true;

            entries.Sort((a, b) => a.Min.CompareTo(b.Min));

            StringBuilder builder = new StringBuilder();
            const double epsilon = 0.001;
            for (int i = 1; i < entries.Count; i++)
            {
                ZoneRangeEntry prev = entries[i - 1];
                ZoneRangeEntry next = entries[i];
                double difference = next.Min - prev.Max;
                if (Math.Abs(difference) <= epsilon)
                    continue;

                builder.AppendLine(
                    prev.Name + "(~" + FormatNumber(prev.Max) + ") <-> " + next.Name + "(" + FormatNumber(next.Min) + "~): " +
                    next.Name + " MinX는 " + prev.Name + " MaxX(" + FormatNumber(prev.Max) + ")와 같아야 합니다 (현재 " +
                    (difference > 0.0
                        ? "판정 불가 갭 " + FormatNumber(difference) + "mm"
                        : "겹침 " + FormatNumber(-difference) + "mm") + ").");
            }

            if (builder.Length <= 0)
                return true;

            message = "Picker X Zone에 판정 불가 구간이 있어 저장할 수 없습니다.\r\n\r\n" +
                      builder +
                      "\r\n인접 Zone은 이전 Zone MaxX와 같은 값에서 시작(맞닿음)해야 " +
                      "모든 X 좌표가 정확히 한 Zone으로 판정됩니다.";
            return false;
        }

        // 기존 조건 유지: Encoder Zone 미사용 시 범위는 존 판정에 쓰이지 않으므로 겹침만 금지한다.
        private static bool ValidateLegacyOverlap(List<ZoneRangeEntry> entries, double tolerance, out string message)
        {
            message = string.Empty;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = i + 1; j < entries.Count; j++)
                {
                    if (RangesOverlap(entries[i].Min, entries[i].Max, entries[j].Min, entries[j].Max, tolerance))
                        builder.AppendLine(entries[i].Name + " / " + entries[j].Name);
                }
            }

            if (builder.Length <= 0)
                return true;

            message = "Picker X Zone 범위가 겹쳐 저장할 수 없습니다.\r\n\r\n" +
                      builder +
                      "\r\nZone 허용오차(" + FormatNumber(tolerance) + "mm)를 포함해 서로 겹치지 않게 다시 설정하세요.";
            return false;
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

        private static bool RangesOverlap(double firstMin, double firstMax, double secondMin, double secondMax, double tolerance)
        {
            double safeTolerance = Math.Max(0.0, tolerance);
            double aMin = Math.Min(firstMin, firstMax) - safeTolerance;
            double aMax = Math.Max(firstMin, firstMax) + safeTolerance;
            double bMin = Math.Min(secondMin, secondMax) - safeTolerance;
            double bMax = Math.Max(secondMin, secondMax) + safeTolerance;

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
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(lblTitle, "extraDialog.pickerZoneSetupDialog.lblTitle.caption");
            Lang.BindKey(lblSide, "extraDialog.pickerZoneSetupDialog.lblSide.caption");
            Lang.BindKey(chkUseEncoderZone, "extraDialog.pickerZoneSetupDialog.chkUseEncoderZone.caption");
            Lang.BindKey(lblTolerance, "extraDialog.pickerZoneSetupDialog.lblTolerance.caption");
            Lang.BindKey(lblXClearance, "extraDialog.pickerZoneSetupDialog.lblXClearance.caption");
            Lang.BindKey(lblYOutDistance, "extraDialog.pickerZoneSetupDialog.lblYOutDistance.caption");
            Lang.BindKey(colUse, "extraDialog.pickerZoneSetupDialog.colUse.caption");
            Lang.BindKey(colZone, "extraDialog.pickerZoneSetupDialog.colZone.caption");
            Lang.BindKey(colMin, "extraDialog.pickerZoneSetupDialog.colMin.caption");
            Lang.BindKey(colMax, "extraDialog.pickerZoneSetupDialog.colMax.caption");
            Lang.BindKey(colCurrent, "extraDialog.pickerZoneSetupDialog.colCurrent.caption");
            Lang.BindKey(colMatch, "extraDialog.pickerZoneSetupDialog.colMatch.caption");
            Lang.BindKey(btnReload, "extraDialog.pickerZoneSetupDialog.btnReload.caption");
            Lang.BindKey(btnTeachMin, "extraDialog.pickerZoneSetupDialog.btnTeachMin.caption");
            Lang.BindKey(btnTeachMax, "extraDialog.pickerZoneSetupDialog.btnTeachMax.caption");
            Lang.BindKey(btnSave, "extraDialog.pickerZoneSetupDialog.btnSave.caption");
            Lang.BindKey(btnClose, "extraDialog.pickerZoneSetupDialog.btnClose.caption");
            Lang.BindKey(this, "extraDialog.pickerZoneSetupDialog.this.caption");
            Load += (sender, args) => Lang.Apply(this);
        }

    }
}
