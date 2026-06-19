using System;
using System.Globalization;
using System.Windows.Forms;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui.Controls;
using QMC.Common;
using QMC.Common.Motion;
using QMC.Common.Motion.Ajin;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// MotionPage 의 SPEED 탭. 축 속도/가감속 파라미터를 로드/편집/저장한다.
    /// 저장은 <see cref="MotionAxisStore"/> JSON 파일과 매니저 in-memory 양쪽에 적용된다.
    /// </summary>
    public partial class MotionPage
    {
        private bool _speedDirty;
        private bool _speedLoading;

        private void InitializeSpeedTab()
        {
            try
            {
                speedGrid.ReadOnly = true;
                speedGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                speedGrid.MultiSelect = false;
                speedGrid.AllowUserToOrderColumns = false;
                speedGrid.AllowUserToResizeColumns = true;
                speedGrid.AllowUserToResizeRows = false;
                foreach (DataGridViewColumn col in speedGrid.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
                    col.Resizable = DataGridViewTriState.True;
                }

                speedGrid.CellDoubleClick += OnSpeedCellDoubleClick;
                speedGrid.ColumnHeaderMouseDoubleClick += OnSpeedHeaderDoubleClick;
                speedGrid.DataError += (s, e) => { e.ThrowException = false; };

                btnSpeedReload.Click += (s, e) => LoadSpeedRows();
                btnSpeedSave.Click += (s, e) => SaveSpeedRows();
                btnSpeedScale.Click += OnSpeedScaleClick;
                UpdateSpeedScaleButton();
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-SPD",
                    "MotionPage",
                    "InitializeSpeedTab failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// .mot 파일 LoadParameters 직후 호출. 보드에서 읽을 수 있는 파라미터를
        /// 각 <see cref="AjinAxis"/> 가 자신의 <see cref="AxisSetup"/> / <see cref="AxisConfig"/> 로 직접 동기화한 뒤
        /// SPEED 그리드를 다시 로드한다. (보드 I/O 는 모두 Ajin 레이어에서 처리)
        /// </summary>
        public void ApplyParametersFromBoard()
        {
            try
            {
                if (!AjinSystem.IsOpen) return;

                int updated = 0;
                foreach (MotionAxisRow row in _rows)
                {
                    AjinAxis ajin = row?.Axis as AjinAxis;
                    if (ajin == null || ajin.Config == null) continue;
                    if (ajin.ReadSetupFromBoard())
                        updated++;
                }

                SaveMotionAxisSettings();
                LoadSpeedRows();
                RefreshConfigForSelected();

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC", "PARA-LOAD-APPLY",
                    "axes=" + updated);
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-SPD",
                    "MotionPage",
                    "ApplyParametersFromBoard failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnSpeedCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_speedLoading) return;
                if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;

                DataGridViewRow row = speedGrid.Rows[e.RowIndex];
                DataGridViewCell cell = row.Cells[e.ColumnIndex];
                string title = (row.Cells["AXIS"].Value?.ToString() ?? "AXIS") +
                               " - " +
                               (speedGrid.Columns[e.ColumnIndex].HeaderText ?? string.Empty);
                string current = cell.Value?.ToString() ?? string.Empty;

                using (var dlg = new NumericKeypadDialog(title, current, string.Empty))
                {
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                    string newText = NormalizeSpeedInputText(dlg.ValueText, e.ColumnIndex);
                    if (string.Equals(newText, current, StringComparison.Ordinal)) return;

                    cell.Value = newText;
                    _speedDirty = true;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-SPD",
                    "MotionPage",
                    "OnSpeedCellDoubleClick failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// 전체 DefaultVelocity 퍼센트 스케일 버튼 클릭. NumericKeypad 로 % 값을 입력받아
        /// 안전 범위(1~100)로 보정한 뒤 모션 공통 스케일과 AppSettings 에 적용/저장한다.
        /// </summary>
        private void OnSpeedScaleClick(object sender, EventArgs e)
        {
            try
            {
                double current = MotionSpeedScale.ScalePercent;
                string currentText = current.ToString("0.###", CultureInfo.InvariantCulture);

                using (var dlg = new NumericKeypadDialog("DEFAULT SPEED SCALE %", currentText, "%"))
                {
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                    double value;
                    if (!double.TryParse(dlg.ValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                        !double.TryParse(dlg.ValueText, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                    {
                        QMC.Common.MessageDialog.Show("Invalid speed scale value. Enter " +
                            MotionSpeedScale.MinPercent + " ~ " + MotionSpeedScale.MaxPercent + " %.");
                        return;
                    }

                    ApplySpeedScalePercent(value);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-SPD",
                    "MotionPage",
                    "OnSpeedScaleClick failed: " + ex.Message);
                QMC.Common.MessageDialog.Show("Speed scale change failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>입력된 % 값을 보정/적용하고 AppSettings 에 저장한다.</summary>
        private void ApplySpeedScalePercent(double percent)
        {
            try
            {
                double clamped = MotionSpeedScale.ClampPercent(percent);
                MotionSpeedScale.ScalePercent = clamped;

                if (QMC.CDT320.AppSettingsStore.Current != null)
                {
                    QMC.CDT320.AppSettingsStore.Current.DefaultVelocityScalePercent = clamped;
                    QMC.CDT320.AppSettingsStore.Save();
                }

                UpdateSpeedScaleButton();

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC", "SPEED-SCALE",
                    "DefaultVelocity scale = " + clamped.ToString("0.###", CultureInfo.InvariantCulture) + " %");
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-SPD",
                    "MotionPage",
                    "ApplySpeedScalePercent failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>스케일 버튼 표시 문구를 현재 적용 퍼센트로 갱신한다.</summary>
        private void UpdateSpeedScaleButton()
        {
            try
            {
                if (btnSpeedScale == null) return;
                btnSpeedScale.Text = MotionSpeedScale.ScalePercent.ToString("0.###", CultureInfo.InvariantCulture) + " %";
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>현재 등록된 축 컬렉션을 SPEED 그리드에 채운다.</summary>
        private void LoadSpeedRows()
        {
            try
            {
                _speedLoading = true;
                speedGrid.Rows.Clear();

                foreach (MotionAxisRow row in _rows)
                {
                    if (row?.Axis == null || row.Axis.Config == null) continue;
                    AxisConfig c = row.Axis.Config;

                    int idx = speedGrid.Rows.Add(
                        row.Axis.Name,
                        FormatAxisValue(c.DefaultVelocity, row.Axis, "0.###"),
                        FormatAxisValue(c.Acceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.Deceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.StopDeceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.HomeFirstVelocity, row.Axis, "0.###"),
                        FormatAxisValue(c.HomeSecondVelocity, row.Axis, "0.###"),
                        FormatAxisValue(c.HomeThirdVelocity, row.Axis, "0.###"),
                        FormatAxisValue(c.HomeLastVelocity, row.Axis, "0.###"),
                        FormatAxisValue(c.HomeFirstAcceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.HomeFirstDeceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.HomeSecondAcceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.HomeSecondDeceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.JogCoarseVelocity, row.Axis, "0.###"),
                        FormatAxisValue(c.JogFineVelocity, row.Axis, "0.###"),
                        FormatAxisValue(c.JogAcceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.JogDeceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.JogStopDeceleration, row.Axis, "0.###"),
                        FormatAxisValue(c.InPositionTolerance, row.Axis, "0.######"));

                    speedGrid.Rows[idx].Tag = row.Axis;
                }

                _speedDirty = false;
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-SPD",
                    "MotionPage",
                    "LoadSpeedRows failed: " + ex.Message);
            }
            finally
            {
                _speedLoading = false;
            }
        }

        private void OnSpeedCellChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_speedLoading) return;
            if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
            _speedDirty = true;
        }

        /// <summary>컬럼 헤더 더블클릭 시 해당 파라미터를 전체 축에 일괄 적용한다.</summary>
        private void OnSpeedHeaderDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (_speedLoading) return;
                if (e.ColumnIndex <= 0) return; // AXIS 컬럼 제외
                if (speedGrid.Rows.Count == 0) return;

                string columnHeader = speedGrid.Columns[e.ColumnIndex].HeaderText ?? string.Empty;
                string title = "ALL AXES - " + columnHeader;
                string current = speedGrid.Rows[0].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;

                using (var dlg = new NumericKeypadDialog(title, current, string.Empty))
                {
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                    string newText = NormalizeSpeedInputText(dlg.ValueText, e.ColumnIndex);
                    int changed = 0;
                    foreach (DataGridViewRow row in speedGrid.Rows)
                    {
                        BaseAxis axis = row.Tag as BaseAxis;
                        if (IsDegreeAxis(axis))
                            continue;

                        row.Cells[e.ColumnIndex].Value = newText;
                        changed++;
                    }

                    _speedDirty = _speedDirty || changed > 0;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-MOTION-SPD",
                    "MotionPage",
                    "OnSpeedHeaderDoubleClick failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>그리드 편집 결과를 각 축 Config 에 반영하고 JSON 저장소에 저장한다.</summary>
        private void SaveSpeedRows()
        {
            try
            {
                int applied = 0;
                foreach (DataGridViewRow row in speedGrid.Rows)
                {
                    BaseAxis axis = row.Tag as BaseAxis;
                    if (axis == null || axis.Config == null) continue;

                    AxisConfig c = axis.Config;
                    c.DefaultVelocity = ReadDisplayCell(row, "DEFAULT_VEL", c.DefaultVelocity, axis);
                    c.Acceleration = ReadDisplayCell(row, "ACCEL", c.Acceleration, axis);
                    c.Deceleration = ReadDisplayCell(row, "DECEL", c.Deceleration, axis);
                    c.StopDeceleration = ReadDisplayCell(row, "STOP_DEC", c.StopDeceleration, axis);
                    c.HomeFirstVelocity = ReadDisplayCell(row, "HOME_VEL_1", c.HomeFirstVelocity, axis);
                    c.HomeSecondVelocity = ReadDisplayCell(row, "HOME_VEL_2", c.HomeSecondVelocity, axis);
                    c.HomeThirdVelocity = ReadDisplayCell(row, "HOME_VEL_3", c.HomeThirdVelocity, axis);
                    c.HomeLastVelocity = ReadDisplayCell(row, "HOME_VEL_4", c.HomeLastVelocity, axis);
                    c.HomeFirstAcceleration = ReadDisplayCell(row, "HOME_ACC_1", c.HomeFirstAcceleration, axis);
                    c.HomeFirstDeceleration = ReadDisplayCell(row, "HOME_DEC_1", c.HomeFirstDeceleration, axis);
                    c.HomeSecondAcceleration = ReadDisplayCell(row, "HOME_ACC_2", c.HomeSecondAcceleration, axis);
                    c.HomeSecondDeceleration = ReadDisplayCell(row, "HOME_DEC_2", c.HomeSecondDeceleration, axis);
                    c.JogCoarseVelocity = ReadDisplayCell(row, "JOG_COARSE", c.JogCoarseVelocity, axis);
                    c.JogFineVelocity = ReadDisplayCell(row, "JOG_FINE", c.JogFineVelocity, axis);
                    c.JogAcceleration = ReadDisplayCell(row, "JOG_ACC", c.JogAcceleration, axis);
                    c.JogDeceleration = ReadDisplayCell(row, "JOG_DEC", c.JogDeceleration, axis);
                    c.JogStopDeceleration = ReadDisplayCell(row, "JOG_STOP_DEC", c.JogStopDeceleration, axis);
                    c.InPositionTolerance = ReadDisplayCell(row, "INPOS_TOL", c.InPositionTolerance, axis);
                    applied++;
                }

                // 모델 변경 후 보드에도 동일 값을 기록한다 (모델 → 보드 동기화)
                int boardWritten = 0;
                foreach (DataGridViewRow row in speedGrid.Rows)
                {
                    AjinAxis ajin = row.Tag as AjinAxis;
                    if (ajin == null)
                        continue;
                    if (ajin.WriteSetupToBoard())
                        boardWritten++;
                }

                bool saved = SaveMotionAxisSettings();
                _speedDirty = false;

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC", "SPEED-SAVE",
                    "axes=" + applied + ", board=" + boardWritten + ", saved=" + saved);

                QMC.Common.MessageDialog.Show(saved
                    ? "Saved speed parameters for " + applied + " axes."
                    : "Speed parameters were applied, but some settings failed to save. Check Alarm/Event Log.");
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    "UI-MOTION-SPD",
                    "MotionPage",
                    "SaveSpeedRows failed: " + ex.Message);
                QMC.Common.MessageDialog.Show("Save failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static double ReadCell(DataGridViewRow row, string columnName, double fallback)
        {
            try
            {
                object v = row.Cells[columnName].Value;
                if (v == null) return fallback;
                string text = v.ToString();
                if (string.IsNullOrWhiteSpace(text)) return fallback;
                double d;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    return d;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out d))
                    return d;
                return fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static double ReadDisplayCell(DataGridViewRow row, string columnName, double fallback, BaseAxis axis)
        {
            try
            {
                double displayValue = ReadCell(row, columnName, AxisUnitConverter.ToDisplay(fallback, axis));
                return AxisUnitConverter.FromDisplay(displayValue, axis);
            }
            catch
            {
                return fallback;
            }
            finally
            {
            }
        }

        private string NormalizeSpeedInputText(string text, int columnIndex)
        {
            try
            {
                string valueText = text ?? string.Empty;
                double value;
                if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                    !double.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                    return valueText;

                DataGridViewColumn col = columnIndex >= 0 && columnIndex < speedGrid.Columns.Count
                    ? speedGrid.Columns[columnIndex]
                    : null;

                string format = col != null && col.Name == "INPOS_TOL" ? "0.######" : "0.###";
                return value.ToString(format, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "SPEED-NUMERIC", "Speed input normalize failed: " + ex.Message);
                return text ?? string.Empty;
            }
            finally
            {
            }
        }

        private static bool IsDegreeAxis(BaseAxis axis)
        {
            try
            {
                return AxisUnitConverter.IsDegree(AxisUnitConverter.DisplayUnitFor(axis));
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }
    }
}


