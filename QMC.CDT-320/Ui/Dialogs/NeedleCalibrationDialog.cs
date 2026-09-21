using System;
using QMC.CDT_320.Ui.Localization;
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
    public sealed partial class NeedleCalibrationDialog : Form, ILocalizedView
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

        private void InitializeLocalization()
        {
            Lang.BindKey(lblHeader, "calibration.needle.lblHeader");
            Lang.BindKey(groupSettings, "calibration.needle.groupSettings");
            Lang.BindKey(colSettingItem, "calibration.needle.colSettingItem");
            Lang.BindKey(colSettingValue, "calibration.needle.colSettingValue");
            Lang.BindKey(colSettingUnit, "calibration.needle.colSettingUnit");
            Lang.BindKey(_btnSaveParameters, "calibration.needle.btnSaveParameters");
            Lang.BindKey(groupResults, "calibration.needle.groupResults");
            Lang.BindKey(colResultItem, "calibration.needle.colResultItem");
            Lang.BindKey(colResultValue, "calibration.needle.colResultValue");
            Lang.BindKey(colResultUnit, "calibration.needle.colResultUnit");
            Lang.BindKey(groupTeaching, "calibration.needle.groupTeaching");
            Lang.BindKey(colTeachingItem, "calibration.needle.colTeachingItem");
            Lang.BindKey(colTeachingActual, "calibration.needle.colTeachingActual");
            Lang.BindKey(colTeachingUnit, "calibration.needle.colTeachingUnit");
            Lang.BindKey(_status, "calibration.needle.status");
            Lang.BindKey(_btnCheck, "calibration.needle.btnCheck");
            Lang.BindKey(_btnUseCurrent, "calibration.needle.btnUseCurrent");
            Lang.BindKey(_btnMoveTouch, "calibration.needle.btnMoveTouch");
            Lang.BindKey(_btnStart, "calibration.needle.btnStart");
            Lang.BindKey(_btnSeqStop, "calibration.needle.btnSeqStop");
            Lang.BindKey(_btnAvoid, "calibration.needle.btnAvoid");
            Lang.BindKey(_btnReload, "calibration.needle.btnReload");
            Lang.BindKey(_btnSave, "calibration.needle.btnSave");
            Lang.BindKey(_btnClose, "calibration.needle.btnClose");
            Lang.BindKey(this, "calibration.needle.this");
            CalibrationDialogText.BindGrid(_settingsGrid);
            CalibrationDialogText.BindGrid(_resultGrid);
            CalibrationDialogText.BindGrid(_teachingGrid);
        }

        public void ApplyLanguage()
        {
            // 언어 변경은 표시만 무효화하며 선택/입력/설정값을 다시 불러오지 않습니다.
            foreach (DataGridViewRow row in _settingsGrid.Rows)
                ApplySettingToolTip(row, GetSettingToolTip(Convert.ToString(row.Tag, CultureInfo.InvariantCulture)));
            Invalidate(true);
        }

        public NeedleCalibrationDialog()
        {
            InitializeComponent();
            InitializeLocalization();
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
                    Lang.BindFormat(_status, "calibration.status.s145");
                    return;
                }

                cts.Cancel();
                Lang.BindFormat(_status, "calibration.status.s146");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s147", ex.Message);
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
                        Lang.T("calibration.message.m013"),
                        Lang.T("calibration.message.m014"),
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
                    Lang.BindFormat(_status, "calibration.status.s148");
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
                Lang.BindFormat(_status, "calibration.status.s149");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s049", ex.Message);
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
                    return Lang.T("calibration.tip.t073");
                case MoveAccKey:
                    return Lang.T("calibration.tip.t074");
                case MoveDecKey:
                    return Lang.T("calibration.tip.t075");
                case MoveTimeoutKey:
                    return Lang.T("calibration.tip.t076");
                case TouchStageYKey:
                    return Lang.T("calibration.tip.t077");
                case TouchNeedleXKey:
                    return Lang.T("calibration.tip.t078");
                case NeedleCapTeachKey:
                    return Lang.T("calibration.tip.t079");
                case NeedlePinTeachKey:
                    return Lang.T("calibration.tip.t080");
                case CapNearTouchOffsetKey:
                    return Lang.T("calibration.tip.t081");
                case PinReadyBelowFlushKey:
                    return Lang.T("calibration.tip.t082");
                case CapSearch100MaxKey:
                    return Lang.T("calibration.tip.t083");
                case CapSearch10MaxKey:
                    return Lang.T("calibration.tip.t084");
                case CapSearch1MaxKey:
                    return Lang.T("calibration.tip.t085");
                case PinSearch10MaxKey:
                    return Lang.T("calibration.tip.t086");
                case PinSearch1MaxKey:
                    return Lang.T("calibration.tip.t087");
                case TouchStableKey:
                    return Lang.T("calibration.tip.t088");
                case TouchPollKey:
                    return Lang.T("calibration.tip.t089");
                case MoveAvoidAfterKey:
                    return Lang.T("calibration.tip.t090");
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
                Lang.BindFormat(_status, "calibration.status.s150");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s151", ex.Message);
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
                    Lang.BindFormat(_status, "calibration.status.s152");
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
                    Lang.BindFormat(_status, "calibration.status.s153");
                return true;
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s052", ex.Message);
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-CAL-SAVE-SETTING", _status.Text);
                return false;
            }
        }

        private void SaveLastSuccessfulResult()
        {
            if (_lastSuccessfulResult == null || !_lastSuccessfulResult.Success)
            {
                Lang.BindFormat(_status, "calibration.status.s154");
                // [로그 보강 2026-07-27] 차단 사실을 이력에 남긴다(Collet BlockResultSave와 동일 기준).
                QMC.Common.Log.Write("Calibration", "SYSTEM", "NeedleCalSaveResultBlocked", _status.Text + " - Check");
                EventLogger.Write(EventKind.Warning, "CAL", "NEEDLE-CAL-SAVE-RESULT-BLOCKED", _status.Text);
                QMC.Common.MessageDialog.Show(
                    this,
                    _status.Text,
                    Lang.T("calibration.message.m015"),
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
                    Lang.BindFormat(_status, "calibration.status.s155");
                    EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-SAVE-RESULT-STALE", _status.Text);
                    QMC.Common.MessageDialog.Show(
                        this,
                        _status.Text,
                        Lang.T("calibration.message.m015"),
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
                Lang.BindFormat(_status, "calibration.status.s156", data.NeedleCapTouchPosition.ToString("F6"), data.NeedlePinFlushPosition.ToString("F6"), data.NeedlePinReadyPosition.ToString("F6"));
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-SAVE-RESULT", _status.Text);
                _lastSuccessfulResult = null;
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s157", ex.Message);
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-CAL-SAVE-RESULT", _status.Text);
                QMC.Common.MessageDialog.Show(
                    this,
                    _status.Text,
                    Lang.T("calibration.message.m015"),
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
                    CalibrationDialogText.BindStatus(_status, reason);
                    if (showOk)
                        QMC.Common.MessageDialog.Show(this, reason, Lang.T("calibration.message.m015"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                InputStageUnit stage = ResolveStage();
                if (stage == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s158");
                    return false;
                }

                if (!IsAxisReady(stage.StageY, "StageY")) return false;
                if (!IsAxisReady(stage.NeedleBlockX, "NeedleX")) return false;
                if (!IsAxisReady(stage.NeedleZ, "NeedleZ")) return false;
                if (!IsAxisReady(stage.EjectPinZ, "EjectPinZ")) return false;
                if (stage.WaferStageTouchSensor == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s159");
                    return false;
                }

                if (showOk)
                    Lang.BindFormat(_status, "calibration.status.s160");
                return true;
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s065", ex.Message);
                return false;
            }
        }

        private bool IsAxisReady(BaseAxis axis, string name)
        {
            if (axis == null)
            {
                Lang.BindFormat(_status, "calibration.status.s161", name);
                return false;
            }

            axis.UpdateStatus();
            if (!axis.IsServoOn || axis.IsAlarm)
            {
                Lang.BindFormat(_status, "calibration.status.s162", name, (axis.IsServoOn ? "ON" : "OFF"), (axis.IsAlarm ? "ON" : "OFF"));
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

                CalibrationDialogText.BindStatus(_status, runningMessage);
                int result = await action(sequence, runCts.Token).ConfigureAwait(true);
                LoadFromMachine();

                if (result != 0)
                {
                    Lang.BindFormat(_status, "calibration.status.s163", sequence.Result.Message);
                    QMC.Common.MessageDialog.Show(this, _status.Text, Lang.T("calibration.message.m015"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    Lang.BindFormat(_status, "calibration.status.s164", sequence.Result.NeedleCapTouchPosition.ToString("F6"), sequence.Result.NeedlePinFlushPosition.ToString("F6"), sequence.Result.NeedlePinReadyPosition.ToString("F6"));
                }
                else
                {
                    CalibrationDialogText.BindStatus(_status, sequence.Result.Message);
                }
            }
            catch (OperationCanceledException)
            {
                Lang.BindFormat(_status, "calibration.status.s165");
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-STOP", _status.Text);
            }
            catch (SequenceStopException ex)
            {
                Lang.BindFormat(_status, "calibration.status.s166", ex.Message);
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-STOP", _status.Text);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s167", ex.Message);
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-CAL-RUN", _status.Text);
                QMC.Common.MessageDialog.Show(this, _status.Text, Lang.T("calibration.message.m015"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
