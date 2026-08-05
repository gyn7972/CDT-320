using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Sequencing;
using QMC.CDT320.Sequencing.Calibration;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class NeedleCalibrationDialog : Form
    {
        private const string MoveSpeedKey = "Move Speed";
        private const string MoveAccKey = "Move Acc";
        private const string MoveDecKey = "Move Dec";
        private const string MoveTimeoutKey = "Move Timeout";
        private const string TouchStageYKey = "Touch StageY Position";
        private const string TouchNeedleXKey = "Touch NeedleX Position";
        private const string NeedleCapTeachKey = "NeedleCap Teaching Position";
        private const string NeedlePinTeachKey = "NeedlePin Teaching Position";
        private const string CapNearTouchOffsetKey = "Cap Near Touch Offset";
        private const string PinReadyBelowFlushKey = "Pin Ready Below Flush";
        private const string CapSearch100MaxKey = "Cap Search 100um Max";
        private const string CapSearch10MaxKey = "Cap Search 10um Max";
        private const string CapSearch1MaxKey = "Cap Search 1um Max";
        private const string PinSearch10MaxKey = "Pin Search 10um Max";
        private const string PinSearch1MaxKey = "Pin Search 1um Max";
        private const string TouchStableKey = "Touch Stable";
        private const string TouchPollKey = "Touch Poll Interval";
        private const string MoveAvoidAfterKey = "Move Avoid After Cal";

        private bool _busy;
        private bool _loadedOnce;
        private CancellationTokenSource _runCts;
        private NeedleCalibrationResult _lastSuccessfulResult;

        public static NeedleCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "NeedleCalibrationDialog",
                owner,
                () => new NeedleCalibrationDialog());
        }

        public NeedleCalibrationDialog()
        {
            InitializeComponent();
            CalibrationDialogButtonStyle.ApplyFooterButtons(
                new[] { _btnCheck, _btnUseCurrent, _btnMoveTouch, _btnSeqStop, _btnAvoid, _btnReload, _btnClose },
                new[] { _btnStart },
                new[] { _btnSave });
            CalibrationDialogGridBehavior.Apply(_settingsGrid, _resultGrid, _teachingGrid);
            UpdateResultSaveButtonEnabled();
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            CheckReady(true);
        }

        private void btnUseCurrent_Click(object sender, EventArgs e)
        {
            UseCurrentPosition();
        }

        private async void btnMoveTouch_Click(object sender, EventArgs e)
        {
            await MoveTouchAsync().ConfigureAwait(true);
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            await RunCalibrationAsync().ConfigureAwait(true);
        }

        // 실행 중 정지 요청: 취소되면 시퀀스의 취소 경로가 NeedleZ/EjectPinZ를 정지시킨다.
        private void btnSeqStop_Click(object sender, EventArgs e)
        {
            try
            {
                CancellationTokenSource cts = _runCts;
                if (cts == null)
                {
                    _status.Text = "실행 중인 Needle Calibration 시퀀스가 없습니다.";
                    return;
                }

                cts.Cancel();
                _status.Text = "Needle Calibration 정지 요청을 보냈습니다. 축 정지 로그를 확인하세요.";
            }
            catch (Exception ex)
            {
                _status.Text = "정지 요청 실패: " + ex.Message;
            }
        }

        private async void btnAvoid_Click(object sender, EventArgs e)
        {
            await MoveZAvoidAsync().ConfigureAwait(true);
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadFromMachine();
        }

        private void btnSaveParameters_Click(object sender, EventArgs e)
        {
            SaveSettingsFromUi(true);
        }

        private void btnSaveResult_Click(object sender, EventArgs e)
        {
            SaveLastSuccessfulResult();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        // 실행 중 X/Alt+F4로 창이 닫혀 시퀀스가 화면 없이 계속 도는 것을 막는다.
        // 단 사용자가 직접 닫을 때(UserClosing)만 붙잡는다 — 앱/Windows/소유자(Form1) 종료 경로에서
        // e.Cancel을 세우면 Form1 종료가 취소되어 프로그램을 끌 수 없게 된다.
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_busy)
                {
                    if (e.CloseReason != CloseReason.UserClosing)
                    {
                        RequestRunCancelForClose("Needle Calibration 창 종료(" + e.CloseReason + ")");
                        base.OnFormClosing(e);
                        return;
                    }

                    DialogResult result = QMC.Common.MessageDialog.Show(this,
                        "Needle Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?",
                        "NEEDLE CAL",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                        RequestRunCancelForClose("Needle Calibration 창 닫기");

                    e.Cancel = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "NEEDLE-CAL-CLOSE",
                    "Needle Calibration 창 종료 확인 중 예외가 발생했습니다. error=" + ex.Message);
                if (e.Cancel)
                    return;
            }

            base.OnFormClosing(e);
        }

        private void RequestRunCancelForClose(string reason)
        {
            try
            {
                CancellationTokenSource cts = _runCts;
                if (cts != null)
                    cts.Cancel();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "NEEDLE-CAL-CLOSE-CANCEL",
                    reason + " 중 정지 요청 실패: " + ex.Message);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!_loadedOnce)
            {
                _loadedOnce = true;
                LoadFromMachine();
            }
        }
        private void LoadFromMachine()
        {
            try
            {
                NeedleCalibrationData data = ResolveNeedleData();
                InputStageUnit stage = ResolveStage();
                if (data == null || stage == null || stage.Recipe == null)
                {
                    _status.Text = "Needle Calibration 설정을 불러올 수 없습니다.";
                    return;
                }

                stage.Recipe.EnsurePositionObjects();
                NeedleZCalibrationSettings settings = data.ZCalibration;
                settings.EnsureDefaults();
                ApplyRecipeFallbacks(settings, stage);

                _settingsGrid.Rows.Clear();
                AddSetting(MoveSpeedKey, settings.Motion.MoveVelocity, "mm/s");
                AddSetting(MoveAccKey, settings.Motion.MoveAcceleration, "mm/s2");
                AddSetting(MoveDecKey, settings.Motion.MoveDeceleration, "mm/s2");
                AddSetting(MoveTimeoutKey, settings.Motion.MoveTimeoutMs, "ms");
                AddSetting(TouchStageYKey, settings.TouchStageYPosition, "mm");
                AddSetting(TouchNeedleXKey, settings.TouchNeedleXPosition, "mm");
                AddSetting(NeedleCapTeachKey, settings.NeedleCapTeachingPosition, "mm");
                AddSetting(NeedlePinTeachKey, settings.NeedlePinTeachingPosition, "mm");
                AddSetting(CapNearTouchOffsetKey, settings.NeedleCapNearTouchOffsetMm, "mm");
                AddSetting(PinReadyBelowFlushKey, settings.NeedlePinReadyBelowFlushMm, "mm");
                AddSetting(CapSearch100MaxKey, settings.CapSearch100umMaxDistanceMm, "mm");
                AddSetting(CapSearch10MaxKey, settings.CapSearch10umMaxDistanceMm, "mm");
                AddSetting(CapSearch1MaxKey, settings.CapSearch1umMaxDistanceMm, "mm");
                AddSetting(PinSearch10MaxKey, settings.PinSearch10umMaxDistanceMm, "mm");
                AddSetting(PinSearch1MaxKey, settings.PinSearch1umMaxDistanceMm, "mm");
                AddSetting(TouchStableKey, settings.TouchStableMs, "ms");
                AddSetting(TouchPollKey, settings.TouchPollIntervalMs, "ms");
                AddSetting(MoveAvoidAfterKey, settings.MoveAvoidAfterCalibration ? "True" : "False", "");

                RefreshResultGrid(data);
                RefreshTeachingGrid(stage);
                _status.Text = "설정을 불러왔습니다. 모든 숫자는 더블클릭 키패드로 입력하세요.";
            }
            catch (Exception ex)
            {
                _status.Text = "설정 로드 실패: " + ex.Message;
            }
        }

        private void RefreshResultGrid(NeedleCalibrationData data)
        {
            _resultGrid.Rows.Clear();
            if (data == null)
                return;

            AddResult("NeedleCapTouchPosition", data.NeedleCapTouchPosition, "mm");
            AddResult("NeedlePinFlushPosition", data.NeedlePinFlushPosition, "mm");
            AddResult("NeedlePinReadyPosition", data.NeedlePinReadyPosition, "mm");
            AddResult("Valid", data.NeedleZCalibrationValid ? "OK" : "-", "");
            AddResult("Updated", data.NeedleZCalibrationUpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "");
        }

        private void RefreshTeachingGrid(InputStageUnit stage)
        {
            _teachingGrid.Rows.Clear();
            if (stage == null)
                return;

            AddTeaching("StageY", stage.StageY != null ? stage.StageY.ActualPosition : 0.0, "mm");
            AddTeaching("NeedleX", stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : 0.0, "mm");
            AddTeaching("NeedleZ", stage.NeedleZ != null ? stage.NeedleZ.ActualPosition : 0.0, "mm");
            AddTeaching("EjectPinZ / NeedleCap", stage.EjectPinZ != null ? stage.EjectPinZ.ActualPosition : 0.0, "mm");
            AddTeaching("Touch Sensor", stage.WaferStageTouchSensor != null && stage.WaferStageTouchSensor.IsOn ? "ON" : "OFF", "");
        }

        private void AddTeaching(string name, double value, string unit)
        {
            AddTeaching(name, value.ToString("0.######", CultureInfo.InvariantCulture), unit);
        }

        private void AddTeaching(string name, string value, string unit)
        {
            _teachingGrid.Rows.Add(name, value ?? string.Empty, unit ?? string.Empty);
        }

        private void AddResult(string name, double value, string unit)
        {
            AddResult(name, value.ToString("0.######", CultureInfo.InvariantCulture), unit);
        }

        private void AddResult(string name, string value, string unit)
        {
            _resultGrid.Rows.Add(name, value ?? string.Empty, unit ?? string.Empty);
        }

        private void AddSetting(string name, double value, string unit)
        {
            AddSetting(name, value.ToString("0.######", CultureInfo.InvariantCulture), unit);
        }

        private void AddSetting(string name, int value, string unit)
        {
            AddSetting(name, value.ToString(CultureInfo.InvariantCulture), unit);
        }

        private void AddSetting(string name, string value, string unit)
        {
            int row = _settingsGrid.Rows.Add(name, value ?? string.Empty, unit ?? string.Empty);
            _settingsGrid.Rows[row].Tag = name;
            ApplySettingToolTip(_settingsGrid.Rows[row], GetSettingToolTip(name));
        }

        private void SettingsGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            string name = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Tag, CultureInfo.InvariantCulture);
            e.ToolTipText = GetSettingToolTip(name);
        }

        private static void ApplySettingToolTip(DataGridViewRow row, string toolTip)
        {
            if (row == null || string.IsNullOrWhiteSpace(toolTip))
                return;

            foreach (DataGridViewCell cell in row.Cells)
                cell.ToolTipText = toolTip;
        }

        private static string GetSettingToolTip(string name)
        {
            switch (name)
            {
                case MoveSpeedKey:
                    return "Needle Z Calibration에서 StageY, NeedleX, NeedleZ, EjectPinZ를 티칭/탐색 위치로 이동할 때 사용하는 전용 속도입니다.";
                case MoveAccKey:
                    return "Needle Z Calibration 전용 이동 가속도입니다. 접촉 탐색 중 충격을 줄이려면 과도하게 크게 넣지 않습니다.";
                case MoveDecKey:
                    return "Needle Z Calibration 전용 이동 감속도입니다. 접촉 감지 후 정지 안정성에 영향을 줍니다.";
                case MoveTimeoutKey:
                    return "각 이동 명령 후 인포지션 완료를 기다리는 최대 시간입니다. 초과하면 캘리브레이션을 실패 처리합니다.";
                case TouchStageYKey:
                    return "WaferStageTouchSensor가 있는 StageY 위치입니다. NeedleX가 센서 위로 이동할 때 함께 사용하는 기준 Y 위치입니다.";
                case TouchNeedleXKey:
                    return "NeedleX를 WaferStageTouchSensor 위로 맞추는 X 위치입니다. MOVE TOUCH와 Cap/Pin 접촉 탐색의 기준입니다.";
                case NeedleCapTeachKey:
                    return "NeedleCap 역할의 EjectPinZ 탐색 시작 기준 위치입니다. USE CURRENT로 현재 EjectPinZ 위치를 넣을 수 있습니다.";
                case NeedlePinTeachKey:
                    return "NeedlePinZ 탐색 시작 기준 위치입니다. NeedlePin과 NeedleCap Flush 위치를 찾기 전 NeedleZ를 이 위치로 이동합니다.";
                case CapNearTouchOffsetKey:
                    return "저장된 NeedleCapTouchPosition 근처로 이동할 때 접촉점에서 떨어져 둘 여유 거리입니다. Pin 탐색 전 Cap을 터치 근처에 배치합니다.";
                case PinReadyBelowFlushKey:
                    return "NeedlePinFlushPosition에서 아래로 내릴 거리입니다. 최종 NeedlePinReadyPosition은 Flush 위치에서 NeedlePin 접촉 탐색 방향으로 이 값만큼 더 이동한 위치입니다.";
                case CapSearch100MaxKey:
                    return "NeedleCap을 100um 단위로 내리며 터치 센서를 찾을 수 있는 최대 거리입니다. 미감지 시 즉시 실패합니다.";
                case CapSearch10MaxKey:
                    return "100um 탐색 후 BackOff한 위치에서 NeedleCap을 10um 단위로 재탐색할 최대 거리입니다.";
                case CapSearch1MaxKey:
                    return "10um 탐색 후 BackOff한 위치에서 NeedleCap을 1um 단위로 정밀 탐색할 최대 거리입니다.";
                case PinSearch10MaxKey:
                    return "NeedlePinZ와 NeedleCap Flush 위치를 찾기 위해 NeedlePin을 10um 단위로 탐색할 최대 거리입니다.";
                case PinSearch1MaxKey:
                    return "NeedlePin 10um 탐색 후 BackOff한 위치에서 1um 단위로 정밀 탐색할 최대 거리입니다.";
                case TouchStableKey:
                    return "터치 센서 ON 상태가 이 시간 동안 유지되어야 접촉으로 인정합니다.";
                case TouchPollKey:
                    return "접촉 탐색 중 터치 센서 상태를 다시 확인하는 주기입니다.";
                case MoveAvoidAfterKey:
                    return "캘리브레이션 완료 또는 실패 후 NeedleZ/EjectPinZ를 Avoid 위치로 복귀할지 선택합니다.";
                default:
                    return string.Empty;
            }
        }

        private void SettingsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            string name = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Tag, CultureInfo.InvariantCulture);
            if (name == MoveAvoidAfterKey)
            {
                bool current = ReadBool(name, true);
                _settingsGrid.Rows[e.RowIndex].Cells[1].Value = current ? "False" : "True";
                return;
            }

            string unit = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Cells[2].Value, CultureInfo.InvariantCulture);
            string currentText = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Cells[1].Value, CultureInfo.InvariantCulture);
            using (var keypad = new NumericKeypadDialog(name, currentText, unit))
            {
                if (keypad.ShowDialog(this) == DialogResult.OK)
                    _settingsGrid.Rows[e.RowIndex].Cells[1].Value = keypad.ValueText;
            }
        }

        private void UseCurrentPosition()
        {
            try
            {
                if (_busy)
                    return;

                InputStageUnit stage = ResolveStage();
                if (stage == null)
                    return;

                SetSetting(TouchStageYKey, stage.StageY != null ? stage.StageY.ActualPosition : 0.0);
                SetSetting(TouchNeedleXKey, stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : 0.0);
                SetSetting(NeedleCapTeachKey, stage.EjectPinZ != null ? stage.EjectPinZ.ActualPosition : 0.0);
                SetSetting(NeedlePinTeachKey, stage.NeedleZ != null ? stage.NeedleZ.ActualPosition : 0.0);
                RefreshTeachingGrid(stage);
                // [문구 정정 2026-07-27] SAVE 버튼이 PARAMETER SAVE / SAVE RESULT 둘로 나뉘었다.
                // 티칭값은 파라미터 쪽이므로 PARAMETER SAVE를 명시한다(SAVE RESULT 오조작 방지).
                _status.Text = "현재 StageY/NeedleX/EjectPinZ/NeedleZ 위치를 캘리브레이션 티칭값으로 넣었습니다. PARAMETER SAVE로 저장하세요.";
            }
            catch (Exception ex)
            {
                _status.Text = "현재 위치 적용 실패: " + ex.Message;
            }
        }

        private void SetSetting(string name, double value)
        {
            foreach (DataGridViewRow row in _settingsGrid.Rows)
            {
                if (Convert.ToString(row.Tag, CultureInfo.InvariantCulture) == name)
                {
                    row.Cells[1].Value = value.ToString("0.######", CultureInfo.InvariantCulture);
                    return;
                }
            }
        }

        private bool SaveSettingsFromUi(bool showMessage)
        {
            try
            {
                NeedleCalibrationData data = ResolveNeedleData();
                if (data == null)
                {
                    _status.Text = "Needle CalibrationData를 찾을 수 없습니다.";
                    return false;
                }

                NeedleZCalibrationSettings settings = data.ZCalibration;
                if (settings.Motion == null)
                    settings.Motion = new CalibrationMotionSettings();
                settings.Motion.MoveVelocity = Math.Max(0.001, ReadDouble(MoveSpeedKey));
                settings.Motion.MoveAcceleration = Math.Max(0.001, ReadDouble(MoveAccKey));
                settings.Motion.MoveDeceleration = Math.Max(0.001, ReadDouble(MoveDecKey));
                settings.Motion.MoveTimeoutMs = Math.Max(100, ReadInt(MoveTimeoutKey, CalibrationMotionSettings.DefaultMoveTimeoutMs));
                settings.TouchStageYPosition = ReadDouble(TouchStageYKey);
                settings.TouchNeedleXPosition = ReadDouble(TouchNeedleXKey);
                settings.NeedleCapTeachingPosition = ReadDouble(NeedleCapTeachKey);
                settings.NeedlePinTeachingPosition = ReadDouble(NeedlePinTeachKey);
                settings.NeedleCapNearTouchOffsetMm = Math.Max(0.0, ReadDouble(CapNearTouchOffsetKey));
                settings.NeedlePinReadyBelowFlushMm = Math.Max(0.001, ReadDouble(PinReadyBelowFlushKey));
                settings.CapSearch100umMaxDistanceMm = Math.Max(0.001, ReadDouble(CapSearch100MaxKey));
                settings.CapSearch10umMaxDistanceMm = Math.Max(0.001, ReadDouble(CapSearch10MaxKey));
                settings.CapSearch1umMaxDistanceMm = Math.Max(0.001, ReadDouble(CapSearch1MaxKey));
                settings.PinSearch10umMaxDistanceMm = Math.Max(0.001, ReadDouble(PinSearch10MaxKey));
                settings.PinSearch1umMaxDistanceMm = Math.Max(0.001, ReadDouble(PinSearch1MaxKey));
                settings.TouchStableMs = Math.Max(0, ReadInt(TouchStableKey, 10));
                settings.TouchPollIntervalMs = Math.Max(1, ReadInt(TouchPollKey, 5));
                settings.MoveAvoidAfterCalibration = ReadBool(MoveAvoidAfterKey, true);
                settings.EnsureDefaults();

                Form1 host = ResolveHost();
                if (host != null)
                    host.SaveMachineSettings();

                string saveReason;
                CalibrationData calData = ResolveCalibrationData();
                if (calData != null)
                    CalibrationDataStore.Save(calData, out saveReason);

                LoadFromMachine();
                if (showMessage)
                    _status.Text = "Needle Z Calibration 설정값을 저장했습니다.";
                return true;
            }
            catch (Exception ex)
            {
                _status.Text = "설정 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-CAL-SAVE-SETTING", _status.Text);
                return false;
            }
        }

        private void SaveLastSuccessfulResult()
        {
            if (_lastSuccessfulResult == null || !_lastSuccessfulResult.Success)
            {
                _status.Text = "저장할 Needle Z 측정 결과가 없습니다. START CAL을 정상 완료한 뒤 SAVE RESULT를 누르세요.";
                // [로그 보강 2026-07-27] 차단 사실을 이력에 남긴다(Collet BlockResultSave와 동일 기준).
                QMC.Common.Log.Write("Calibration", "SYSTEM", "NeedleCalSaveResultBlocked", _status.Text + " - Check");
                EventLogger.Write(EventKind.Warning, "CAL", "NEEDLE-CAL-SAVE-RESULT-BLOCKED", _status.Text);
                QMC.Common.MessageDialog.Show(
                    this,
                    _status.Text,
                    "NEEDLE Z CAL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                CalibrationData calibrationData = ResolveCalibrationData();
                if (calibrationData == null)
                    throw new InvalidOperationException("Needle CalibrationData를 찾을 수 없습니다.");

                calibrationData.EnsureObjects();
                NeedleCalibrationData data = calibrationData.Needle;
                if (!data.NeedleZCalibrationValid ||
                    !AreNearlyEqual(data.NeedleCapTouchPosition, _lastSuccessfulResult.NeedleCapTouchPosition) ||
                    !AreNearlyEqual(data.NeedlePinFlushPosition, _lastSuccessfulResult.NeedlePinFlushPosition) ||
                    !AreNearlyEqual(data.NeedlePinReadyPosition, _lastSuccessfulResult.NeedlePinReadyPosition))
                {
                    _lastSuccessfulResult = null;
                    _status.Text = "마지막 정상 측정 이후 Needle Z 결과가 변경되어 SAVE RESULT를 차단했습니다. 다시 측정하세요.";
                    EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-SAVE-RESULT-STALE", _status.Text);
                    QMC.Common.MessageDialog.Show(
                        this,
                        _status.Text,
                        "NEEDLE Z CAL",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                string saveReason;
                if (!CalibrationDataStore.Save(calibrationData, out saveReason))
                    throw new InvalidOperationException("CalibrationData 저장 실패: " + saveReason);

                Form1 host = ResolveHost();
                if (host != null)
                    host.SaveMachineSettings();

                RefreshResultGrid(data);
                _status.Text = "마지막 정상 측정 결과를 저장했습니다. CapTouch=" +
                               data.NeedleCapTouchPosition.ToString("F6") +
                               ", PinFlush=" + data.NeedlePinFlushPosition.ToString("F6") +
                               ", PinReady=" + data.NeedlePinReadyPosition.ToString("F6");
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-SAVE-RESULT", _status.Text);
                _lastSuccessfulResult = null;
            }
            catch (Exception ex)
            {
                _status.Text = "Needle Z 측정 결과 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-CAL-SAVE-RESULT", _status.Text);
                QMC.Common.MessageDialog.Show(
                    this,
                    _status.Text,
                    "NEEDLE Z CAL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                UpdateResultSaveButtonEnabled();
            }
        }

        private static bool AreNearlyEqual(double left, double right)
        {
            double scale = Math.Max(1.0, Math.Max(Math.Abs(left), Math.Abs(right)));
            return Math.Abs(left - right) <= (1e-9 * scale);
        }

        private bool CheckReady(bool showOk)
        {
            try
            {
                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    _status.Text = reason;
                    if (showOk)
                        QMC.Common.MessageDialog.Show(this, reason, "NEEDLE Z CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                InputStageUnit stage = ResolveStage();
                if (stage == null)
                {
                    _status.Text = "InputStageUnit을 찾을 수 없습니다.";
                    return false;
                }

                if (!IsAxisReady(stage.StageY, "StageY")) return false;
                if (!IsAxisReady(stage.NeedleBlockX, "NeedleX")) return false;
                if (!IsAxisReady(stage.NeedleZ, "NeedleZ")) return false;
                if (!IsAxisReady(stage.EjectPinZ, "EjectPinZ")) return false;
                if (stage.WaferStageTouchSensor == null)
                {
                    _status.Text = "WaferStageTouchSensor를 찾을 수 없습니다.";
                    return false;
                }

                if (showOk)
                    _status.Text = "실행 가능한 상태입니다. 터치 센서 위치와 Z 시작 위치를 확인하세요.";
                return true;
            }
            catch (Exception ex)
            {
                _status.Text = "준비 확인 실패: " + ex.Message;
                return false;
            }
        }

        private bool IsAxisReady(BaseAxis axis, string name)
        {
            if (axis == null)
            {
                _status.Text = name + " 축을 찾을 수 없습니다.";
                return false;
            }

            axis.UpdateStatus();
            if (!axis.IsServoOn || axis.IsAlarm)
            {
                _status.Text = name + " 축 상태가 준비되지 않았습니다. servo=" +
                               (axis.IsServoOn ? "ON" : "OFF") +
                               ", alarm=" + (axis.IsAlarm ? "ON" : "OFF");
                return false;
            }

            return true;
        }

        private async Task MoveTouchAsync()
        {
            await RunSequenceActionAsync(
                "MoveTouch",
                "NeedleX/StageY를 터치 센서 티칭 위치로 이동 중입니다.",
                async (sequence, token) => await sequence.MoveTouchTeachingPositionOnlyAsync(token, SequenceRunMode.Manual).ConfigureAwait(false),
                false).ConfigureAwait(true);
        }

        private async Task MoveZAvoidAsync()
        {
            await RunSequenceActionAsync(
                "MoveAvoid",
                "NeedleZ/EjectPinZ를 Avoid 위치로 이동 중입니다.",
                async (sequence, token) => await sequence.MoveAvoidOnlyAsync(token, SequenceRunMode.Manual).ConfigureAwait(false),
                false).ConfigureAwait(true);
        }

        private async Task RunCalibrationAsync()
        {
            await RunSequenceActionAsync(
                "StartCal",
                "Needle Z Calibration 실행 중입니다.",
                async (sequence, token) => await sequence.RunAsync(token, SequenceRunMode.Manual).ConfigureAwait(false),
                true).ConfigureAwait(true);
        }

        private async Task RunSequenceActionAsync(
            string actionName,
            string runningMessage,
            Func<NeedleCalibrationSequence, CancellationToken, Task<int>> action,
            bool saveAfterSuccess)
        {
            if (_busy)
                return;

            Form1 host = null;
            Action stopHandler = null;
            IDisposable actionScope = null;
            CancellationTokenSource runCts = null;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                if (saveAfterSuccess)
                {
                    _lastSuccessfulResult = null;
                    UpdateResultSaveButtonEnabled();
                }
                if (!SaveSettingsFromUi(false) || !CheckReady(false))
                    return;

                host = ResolveHost();
                runCts = BeginManualCalibrationRun(host, actionName, out actionScope, out stopHandler);
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new NeedleCalibrationSequence(context);

                _status.Text = runningMessage;
                int result = await action(sequence, runCts.Token).ConfigureAwait(true);
                LoadFromMachine();

                if (result != 0)
                {
                    _status.Text = "Needle Z Calibration 실패. Alarm/Event Log를 확인하세요. " + sequence.Result.Message;
                    QMC.Common.MessageDialog.Show(this, _status.Text, "NEEDLE Z CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (saveAfterSuccess)
                {
                    host.SaveMachineSettings();
                    _lastSuccessfulResult = new NeedleCalibrationResult
                    {
                        Success = true,
                        Message = sequence.Result.Message,
                        TouchStageYPosition = sequence.Result.TouchStageYPosition,
                        TouchNeedleXPosition = sequence.Result.TouchNeedleXPosition,
                        NeedleCapTouchPosition = sequence.Result.NeedleCapTouchPosition,
                        NeedlePinFlushPosition = sequence.Result.NeedlePinFlushPosition,
                        NeedlePinReadyPosition = sequence.Result.NeedlePinReadyPosition
                    };
                    UpdateResultSaveButtonEnabled();
                    _status.Text = "측정 완료. SAVE RESULT로 마지막 정상 측정값을 확인 저장하세요. CapTouch=" +
                                   sequence.Result.NeedleCapTouchPosition.ToString("F6") +
                                   ", PinFlush=" + sequence.Result.NeedlePinFlushPosition.ToString("F6") +
                                   ", PinReady=" + sequence.Result.NeedlePinReadyPosition.ToString("F6");
                }
                else
                {
                    _status.Text = sequence.Result.Message;
                }
            }
            catch (OperationCanceledException)
            {
                _status.Text = "Needle Z Calibration이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-STOP", _status.Text);
            }
            catch (SequenceStopException ex)
            {
                _status.Text = "Needle Z Calibration 정지: " + ex.Message;
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-STOP", _status.Text);
            }
            catch (Exception ex)
            {
                _status.Text = "Needle Z Calibration 예외: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-CAL-RUN", _status.Text);
                QMC.Common.MessageDialog.Show(this, _status.Text, "NEEDLE Z CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private CancellationTokenSource BeginManualCalibrationRun(
            Form1 host,
            string actionName,
            out IDisposable actionScope,
            out Action stopHandler)
        {
            if (host == null || host.Controller == null)
                throw new InvalidOperationException("MachineController가 준비되지 않았습니다.");

            actionScope = host.Controller.BeginManualActionScope(
                ManualMotionScopeKind.ProcessSequence,
                "NeedleCalibration:" + actionName);
            CancellationTokenSource runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
            _runCts = runCts;
            stopHandler = delegate
            {
                try
                {
                    CancellationTokenSource cts = _runCts;
                    if (cts != null && !cts.IsCancellationRequested)
                        cts.Cancel();

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "NeedleZCalStop",
                        "메인 STOP 요청으로 Needle Z Calibration 정지 요청. action=" + actionName);
                }
                catch
                {
                }
            };
            host.Controller.StopRequested += stopHandler;
            return runCts;
        }

        private void EndManualCalibrationRun(
            Form1 host,
            Action stopHandler,
            CancellationTokenSource runCts,
            IDisposable actionScope)
        {
            if (host != null && host.Controller != null && stopHandler != null)
                host.Controller.StopRequested -= stopHandler;

            if (ReferenceEquals(_runCts, runCts))
                _runCts = null;

            if (runCts != null)
                runCts.Dispose();

            if (actionScope != null)
                actionScope.Dispose();
        }

        private bool CanRunManualCalibration(out string reason)
        {
            reason = string.Empty;
            try
            {
                Form1 host = ResolveHost();
                if (host == null)
                {
                    reason = "Main 화면을 찾을 수 없어 Needle Z Calibration을 실행할 수 없습니다.";
                    return false;
                }

                if (AlarmManager.HasActive)
                {
                    reason = "현재 알람 상태입니다. 알람 해제 후 Needle Z Calibration을 실행하세요.";
                    return false;
                }

                if (host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않았습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.AutoRunning)
                {
                    reason = "자동 운전 중에는 Needle Z Calibration을 실행할 수 없습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.ManualRunning || host.Controller.IsManualBusy)
                {
                    reason = "다른 수동 동작이 실행 중입니다. 완료 후 다시 실행하세요.";
                    return false;
                }

                if (host.Controller.IsSequenceRunning)
                {
                    reason = "시퀀스가 실행 중입니다. 완료 후 Needle Z Calibration을 실행하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Needle Z Calibration 실행 조건 확인 실패: " + ex.Message;
                return false;
            }
        }

        private NeedleCalibrationData ResolveNeedleData()
        {
            CalibrationData data = ResolveCalibrationData();
            if (data == null)
                return null;

            data.EnsureObjects();
            data.Needle.EnsureObjects();
            return data.Needle;
        }

        private CalibrationData ResolveCalibrationData()
        {
            Form1 host = ResolveHost();
            if (host == null || host.Machine == null || host.Machine.VisionUnit == null || host.Machine.VisionUnit.Config == null)
                return null;

            host.Machine.VisionUnit.Config.EnsureCalibrationObjects();
            host.Machine.VisionUnit.Config.CalibrationData.EnsureObjects();
            return host.Machine.VisionUnit.Config.CalibrationData;
        }

        private InputStageUnit ResolveStage()
        {
            Form1 host = ResolveHost();
            return host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
        }

        private Form1 ResolveHost()
        {
            Form1 host = Owner as Form1;
            if (host != null)
                return host;

            foreach (Form form in Application.OpenForms)
            {
                host = form as Form1;
                if (host != null)
                    return host;
            }

            return FindForm() as Form1;
        }

        private static void ApplyRecipeFallbacks(NeedleZCalibrationSettings settings, InputStageUnit stage)
        {
            if (settings == null || stage == null || stage.Recipe == null)
                return;

            stage.Recipe.EnsurePositionObjects();
            if (Math.Abs(settings.TouchStageYPosition) <= double.Epsilon)
                settings.TouchStageYPosition = stage.Recipe.WaferY.ProcessPosition;
            if (Math.Abs(settings.TouchNeedleXPosition) <= double.Epsilon)
                settings.TouchNeedleXPosition = stage.Recipe.NeedleX.NeedlePinCalPosition;
            if (Math.Abs(settings.NeedleCapTeachingPosition) <= double.Epsilon)
                settings.NeedleCapTeachingPosition = stage.Recipe.EjectPinZ.NeedlePinCalPosition;
            if (Math.Abs(settings.NeedlePinTeachingPosition) <= double.Epsilon)
                settings.NeedlePinTeachingPosition = stage.Recipe.NeedleZ.NeedlePinCalPosition;
        }

        private double ReadDouble(string name)
        {
            string text = ReadString(name, "0");
            double value;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                throw new InvalidOperationException(name + " 값이 숫자가 아닙니다. value=" + text);
            return value;
        }

        private int ReadInt(string name, int fallback)
        {
            string text = ReadString(name, fallback.ToString(CultureInfo.InvariantCulture));
            int value;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private bool ReadBool(string name, bool fallback)
        {
            string text = ReadString(name, fallback ? "True" : "False");
            bool value;
            return bool.TryParse(text, out value) ? value : fallback;
        }

        private string ReadString(string name, string fallback)
        {
            foreach (DataGridViewRow row in _settingsGrid.Rows)
            {
                if (Convert.ToString(row.Tag, CultureInfo.InvariantCulture) == name)
                    return Convert.ToString(row.Cells[1].Value, CultureInfo.InvariantCulture);
            }

            return fallback;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _settingsGrid.Enabled = enabled;
            _btnCheck.Enabled = enabled;
            _btnUseCurrent.Enabled = enabled;
            _btnMoveTouch.Enabled = enabled;
            _btnStart.Enabled = enabled;
            // SEQ STOP은 실행 중에만 누를 수 있어야 한다(다른 버튼과 반대).
            _btnSeqStop.Enabled = !enabled;
            _btnAvoid.Enabled = enabled;
            _btnReload.Enabled = enabled;
            _btnSaveParameters.Enabled = enabled;
            _btnSave.Enabled = enabled && _lastSuccessfulResult != null && _lastSuccessfulResult.Success;
            _btnClose.Enabled = enabled;
        }

        private void UpdateResultSaveButtonEnabled()
        {
            if (_btnSave != null)
                _btnSave.Enabled = !_busy && _lastSuccessfulResult != null && _lastSuccessfulResult.Success;
        }
    }
}
