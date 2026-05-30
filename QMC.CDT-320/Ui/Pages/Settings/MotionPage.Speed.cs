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

                AjinFactory.AxisManager.Save(MotionAxisStore.DefaultPath);
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

                    string newText = dlg.ValueText ?? string.Empty;
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
                        c.DefaultVelocity.ToString("0.###", CultureInfo.InvariantCulture),
                        c.Acceleration.ToString("0.###", CultureInfo.InvariantCulture),
                        c.Deceleration.ToString("0.###", CultureInfo.InvariantCulture),
                        c.HomeFirstVelocity.ToString("0.###", CultureInfo.InvariantCulture),
                        c.HomeSecondVelocity.ToString("0.###", CultureInfo.InvariantCulture),
                        c.HomeThirdVelocity.ToString("0.###", CultureInfo.InvariantCulture),
                        c.HomeLastVelocity.ToString("0.###", CultureInfo.InvariantCulture),
                        c.HomeFirstAcceleration.ToString("0.###", CultureInfo.InvariantCulture),
                        c.HomeFirstDeceleration.ToString("0.###", CultureInfo.InvariantCulture),
                        c.HomeSecondAcceleration.ToString("0.###", CultureInfo.InvariantCulture),
                        c.HomeSecondDeceleration.ToString("0.###", CultureInfo.InvariantCulture),
                        c.JogCoarseVelocity.ToString("0.###", CultureInfo.InvariantCulture),
                        c.JogFineVelocity.ToString("0.###", CultureInfo.InvariantCulture),
                        c.JogAcceleration.ToString("0.###", CultureInfo.InvariantCulture),
                        c.JogDeceleration.ToString("0.###", CultureInfo.InvariantCulture),
                        c.InPositionTolerance.ToString("0.####", CultureInfo.InvariantCulture));

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

                    string newText = dlg.ValueText ?? string.Empty;
                    foreach (DataGridViewRow row in speedGrid.Rows)
                        row.Cells[e.ColumnIndex].Value = newText;

                    _speedDirty = true;
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
                    c.DefaultVelocity = ReadCell(row, "DEFAULT_VEL", c.DefaultVelocity);
                    c.Acceleration = ReadCell(row, "ACCEL", c.Acceleration);
                    c.Deceleration = ReadCell(row, "DECEL", c.Deceleration);
                    c.HomeFirstVelocity = ReadCell(row, "HOME_VEL_1", c.HomeFirstVelocity);
                    c.HomeSecondVelocity = ReadCell(row, "HOME_VEL_2", c.HomeSecondVelocity);
                    c.HomeThirdVelocity = ReadCell(row, "HOME_VEL_3", c.HomeThirdVelocity);
                    c.HomeLastVelocity = ReadCell(row, "HOME_VEL_4", c.HomeLastVelocity);
                    c.HomeFirstAcceleration = ReadCell(row, "HOME_ACC_1", c.HomeFirstAcceleration);
                    c.HomeFirstDeceleration = ReadCell(row, "HOME_DEC_1", c.HomeFirstDeceleration);
                    c.HomeSecondAcceleration = ReadCell(row, "HOME_ACC_2", c.HomeSecondAcceleration);
                    c.HomeSecondDeceleration = ReadCell(row, "HOME_DEC_2", c.HomeSecondDeceleration);
                    c.JogCoarseVelocity = ReadCell(row, "JOG_COARSE", c.JogCoarseVelocity);
                    c.JogFineVelocity = ReadCell(row, "JOG_FINE", c.JogFineVelocity);
                    c.JogAcceleration = ReadCell(row, "JOG_ACC", c.JogAcceleration);
                    c.JogDeceleration = ReadCell(row, "JOG_DEC", c.JogDeceleration);
                    c.InPositionTolerance = ReadCell(row, "INPOS_TOL", c.InPositionTolerance);
                    applied++;
                }

                QMC.CDT320.Ajin.AjinFactory.AxisManager.Save(MotionAxisStore.DefaultPath);
                _speedDirty = false;

                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC", "SPEED-SAVE",
                    "axes=" + applied);

                QMC.Common.MessageDialog.Show("Saved speed parameters for " + applied + " axes.");
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
    }
}


