using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Sequencing;
using QMC.CDT320.Sequencing.Calibration;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class VisionFocusCalibrationDialog : Form
    {
        private enum FocusSettingKey
        {
            Mode,
            PickerSide,
            ColletNo,
            DefaultPosition,
            MinusRange,
            PlusRange,
            Step,
            FineMinusRange,
            FinePlusRange,
            FineStep,
            RepeatCount,
            MoveVelocity,
            MoveAcceleration,
            MoveDeceleration,
            SettleDelay,
            MotionTimeout,
            VisionTimeout,
            VisionBestTimeout,
            FocusValueMode,
            ReturnDefault
        }

        private sealed class SettingRowInfo
        {
            public FocusSettingKey Key;
            public string Name;
            public string Unit;
            public string ToolTip;
            public bool Numeric;
            public bool Integer;
            public string[] Options;

            public SettingRowInfo(
                FocusSettingKey key,
                string name,
                string unit,
                string toolTip,
                bool numeric,
                bool integer,
                string[] options)
            {
                Key = key;
                Name = name;
                Unit = unit;
                ToolTip = toolTip;
                Numeric = numeric;
                Integer = integer;
                Options = options;
            }
        }

        private static readonly string[] ModeOptions =
        {
            "Bottom Collet",
            "Bottom Die",
            "Front Side 0deg",
            "Front Side 90deg",
            "Rear Side 0deg",
            "Rear Side 90deg"
        };

        private static readonly string[] SideOptions = { "Front", "Rear" };
        private static readonly string[] ColletOptions = { "1", "2", "3", "4" };
        private static readonly string[] BoolOptions = { "True", "False" };
        private static readonly string[] FocusValueModeOptions = { "Ack Only", "Wait Result (Test)" };

        private bool _loading;
        private bool _busy;
        private VisionFocusScanKind _selectedKind = VisionFocusScanKind.BottomCollet;
        private VisionFocusPickerSide _selectedPickerSide = VisionFocusPickerSide.Front;
        private int _selectedPickerNo = 1;
        private double _defaultPosition;
        private double _minusRange = 0.2;
        private double _plusRange = 0.2;
        private double _step = 0.02;
        private double _fineMinusRange = 0.05;
        private double _finePlusRange = 0.05;
        private double _fineStep = 0.01;
        private int _repeatCount = 1;
        private double _moveVelocity = 30.0;
        private double _moveAcceleration = 300.0;
        private double _moveDeceleration = 300.0;
        private int _settleDelayMs = 50;
        private int _motionTimeoutMs = 5000;
        private int _visionTimeoutMs = 5000;
        private int _visionBestTimeoutMs = 120000;
        private VisionFocusValueReceiveMode _focusValueReceiveMode = VisionFocusValueReceiveMode.AckOnly;
        private bool _returnToDefaultAfterScan = true;
        private CancellationTokenSource _runCts;

        public static VisionFocusCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "VisionFocusCalibrationDialog",
                owner,
                () => new VisionFocusCalibrationDialog());
        }

        public VisionFocusCalibrationDialog()
        {
            try
            {
                InitializeComponent();
                CalibrationDialogGridBehavior.Apply(gridSettings, gridSamples, gridSaved);
                ConfigureEditableSettingGrid();
                InitializeRuntime();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-DIALOG-INIT",
                    "Vision Focus Cal 창 초기화 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void InitializeRuntime()
        {
            try
            {
                _loading = true;
                ApplyButtonStyle();
                LoadSettingsToUi();
                RefreshSavedGrid();
                lblStatus.Text = "대기 중입니다. Focus 기준 위치를 확인한 뒤 START SCAN을 실행하세요.";
            }
            finally
            {
                _loading = false;
                RefreshSettingGrid();
            }
        }

        private void ApplyButtonStyle()
        {
            CalibrationDialogButtonStyle.ApplyFooterButtons(
                new[] { btnCheck, btnUseCurrent, btnMoveDefault, btnMoveZAvoid, btnMoveYAvoid, btnApplyBest, btnReload, btnClose },
                new[] { btnStartScan },
                new[] { btnSave });
        }

        private void ConfigureEditableSettingGrid()
        {
            gridSettings.ReadOnly = false;
            gridSettings.EditMode = DataGridViewEditMode.EditOnEnter;
            colSettingName.ReadOnly = true;
            colSettingValue.ReadOnly = false;
            colSettingUnit.ReadOnly = true;
        }

        private void gridSettings_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridSettings.IsCurrentCellDirty)
                gridSettings.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void gridSettings_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colSettingValue.Index)
                return;

            DataGridViewRow row = gridSettings.Rows[e.RowIndex];
            SettingRowInfo info = row.Tag as SettingRowInfo;
            if (info != null && info.Numeric)
                e.Cancel = true;
        }

        private void gridSettings_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || e.RowIndex < 0 || e.ColumnIndex != colSettingValue.Index)
                return;

            try
            {
                DataGridViewRow row = gridSettings.Rows[e.RowIndex];
                SettingRowInfo info = row.Tag as SettingRowInfo;
                if (info == null || info.Numeric)
                    return;

                ApplySettingValue(row);
                if (info.Key == FocusSettingKey.Mode ||
                    info.Key == FocusSettingKey.PickerSide ||
                    info.Key == FocusSettingKey.ColletNo)
                {
                    LoadSettingsToUi();
                    RefreshSavedGrid();
                }
                else
                {
                    RefreshSettingGrid();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Focus 설정 변경 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        private void gridSettings_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != colSettingValue.Index)
                return;

            DataGridViewRow row = gridSettings.Rows[e.RowIndex];
            SettingRowInfo info = row.Tag as SettingRowInfo;
            if (info == null)
                return;

            if (!info.Numeric)
            {
                lblStatus.Text = info.Name + " 항목은 숫자 키패드 수정 대상이 아닙니다.";
                return;
            }

            try
            {
                string current = Convert.ToString(row.Cells[colSettingValue.Index].Value, CultureInfo.InvariantCulture);
                using (NumericKeypadDialog dialog = new NumericKeypadDialog(info.Name, current, info.Unit))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    ApplyNumericSetting(info, dialog.ValueText);
                    RefreshSettingGrid();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "숫자 입력 처리 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        private void gridSettings_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            SettingRowInfo info = gridSettings.Rows[e.RowIndex].Tag as SettingRowInfo;
            if (info != null)
                e.ToolTipText = info.ToolTip;
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            try
            {
                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lblStatus.Text = "실행 가능한 상태입니다. Default Pos는 저장된 Focus Cal 등록값만 사용합니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "실행 조건 확인 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-CHECK", lblStatus.Text);
            }
            finally
            {
            }
        }

        private void btnUseCurrent_Click(object sender, EventArgs e)
        {
            try
            {
                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                _defaultPosition = ResolveCurrentAxisPosition(host.Machine);
                RefreshSettingGrid();
                lblStatus.Text = "현재 축 위치를 Default Pos로 적용했습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "현재 위치 적용 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-USE-CURRENT", lblStatus.Text);
            }
            finally
            {
            }
        }

        private async void btnMoveDefault_Click(object sender, EventArgs e)
        {
            await RunMoveDefaultAsync().ConfigureAwait(true);
        }

        private async void btnMoveZAvoid_Click(object sender, EventArgs e)
        {
            await RunMoveZAvoidAsync().ConfigureAwait(true);
        }

        private async void btnMoveYAvoid_Click(object sender, EventArgs e)
        {
            await RunMoveYAvoidAsync().ConfigureAwait(true);
        }

        private async void btnStartScan_Click(object sender, EventArgs e)
        {
            await RunScanAsync().ConfigureAwait(true);
        }

        private void btnApplyBest_Click(object sender, EventArgs e)
        {
            ApplyBestFocusToInspectionPosition();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadSettingsToUi();
            RefreshSavedGrid();
            lblStatus.Text = "Vision Focus Cal 설정값을 다시 불러왔습니다.";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettingsFromUi(true);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
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
                "VisionFocusCalibration:" + actionName + ":" + _selectedKind + ":" + _selectedPickerSide + ":" + _selectedPickerNo);
            CancellationTokenSource runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
            _runCts = runCts;
            stopHandler = delegate
            {
                try
                {
                    CancellationTokenSource cts = _runCts;
                    if (cts != null && !cts.IsCancellationRequested)
                        cts.Cancel();

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionFocusCalStop",
                        "메인 STOP 요청으로 Vision Focus Calibration 정지 요청. action=" + actionName +
                        ", kind=" + _selectedKind +
                        ", side=" + _selectedPickerSide +
                        ", pickerNo=" + _selectedPickerNo);
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

        private async Task RunMoveDefaultAsync()
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

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                if (!SaveSettingsFromUi(false))
                    return;

                runCts = BeginManualCalibrationRun(host, "MoveDefault", out actionScope, out stopHandler);
                VisionFocusScanRequest request = BuildRequest(false);
                var sequence = new VisionFocusScanSequence(host.Machine, request);
                int result = await sequence.MoveDefaultOnlyAsync(runCts.Token, SequenceRunMode.Manual).ConfigureAwait(true);
                lblStatus.Text = result == 0
                    ? "Default Pos 이동 완료."
                    : "Default Pos 이동 실패. Alarm/Event Log를 확인하세요.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Default Pos 이동이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Default Pos 이동 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-MOVE-DEFAULT", lblStatus.Text);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunMoveZAvoidAsync()
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

                if (!ApplyAllSettingRowsFromGrid())
                    return;

                if (!IsBottomFocusKind(_selectedKind))
                {
                    lblStatus.Text = "Z AVOID는 Bottom Collet/Bottom Die 모드에서만 사용할 수 있습니다.";
                    return;
                }

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                runCts = BeginManualCalibrationRun(host, "MoveZAvoid", out actionScope, out stopHandler);
                int result;
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("VisionFocusCalibrationDialog.MoveZAvoid"))
                {
                    result = await MoveSelectedPickerZToAvoidAsync(host.Machine).ConfigureAwait(true);
                }
                runCts.Token.ThrowIfCancellationRequested();

                lblStatus.Text = result == 0
                    ? "Picker Z Avoid 이동 완료. " + BuildSelectedPickerAxisLabel(ResolveSelectedPickerZAxis())
                    : "Picker Z Avoid 이동 실패. Alarm/Event Log를 확인하세요.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-Z-AVOID",
                    lblStatus.Text + ", result=" + result);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Picker Z Avoid 이동이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Picker Z Avoid 이동 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-Z-AVOID-EX", lblStatus.Text);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunMoveYAvoidAsync()
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

                if (!ApplyAllSettingRowsFromGrid())
                    return;

                if (!IsBottomFocusKind(_selectedKind))
                {
                    lblStatus.Text = "Picker Y Avoid는 Bottom 모드에서만 사용할 수 있습니다.";
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                runCts = BeginManualCalibrationRun(host, "MoveYAvoid", out actionScope, out stopHandler);
                int result;
                string label;
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("VisionFocusCalibrationDialog.MoveYAvoid"))
                {
                    if (!IsSelectedPickerZInAvoidPosition(host.Machine))
                    {
                        lblStatus.Text = "Picker Y Avoid 이동 전 선택 Picker Z를 먼저 Avoid 위치로 이동하세요.";
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    result = await MoveSelectedPickerYToAvoidAsync(host.Machine).ConfigureAwait(true);
                    label = BuildSelectedPickerAxisLabel(PickerAxis.PickerY);
                }
                runCts.Token.ThrowIfCancellationRequested();

                lblStatus.Text = result == 0
                    ? "Picker Y Avoid 이동 완료. " + label
                    : "Picker Y Avoid 이동 실패. Alarm/Event Log를 확인하세요.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-PICKER-Y-AVOID",
                    lblStatus.Text + ", result=" + result);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Picker Y Avoid 이동이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Picker Y Avoid 이동 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-PICKER-Y-AVOID-EX", lblStatus.Text);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunScanAsync()
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

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                if (!SaveSettingsFromUi(false))
                    return;

                gridSamples.Rows.Clear();

                runCts = BeginManualCalibrationRun(host, "StartScan", out actionScope, out stopHandler);
                VisionFocusScanRequest request = BuildRequest(true);
                var sequence = new VisionFocusScanSequence(host.Machine, request);
                int result = await sequence.RunAsync(runCts.Token, SequenceRunMode.Manual).ConfigureAwait(true);
                PopulateSamples(sequence.Result);
                RefreshSavedGrid();

                if (result != 0)
                {
                    lblStatus.Text = "Focus Scan 실패: " + sequence.Result.Message;
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                host.SaveMachineSettings();
                lblStatus.Text = "Focus Scan 완료. Best=" + sequence.Result.BestPosition.ToString("F3") +
                                 ", Score=" + sequence.Result.BestScore.ToString("F4") +
                                 ", Sample=" + sequence.Result.SampleCount;
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Focus Scan이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Focus Scan 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-SCAN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private VisionFocusScanRequest BuildRequest(bool useUiRange)
        {
            return new VisionFocusScanRequest
            {
                Kind = _selectedKind,
                PickerSide = _selectedPickerSide,
                PickerNo = _selectedPickerNo,
                DefaultPosition = _defaultPosition,
                MinusRange = useUiRange ? _minusRange : 0.0,
                PlusRange = useUiRange ? _plusRange : 0.0,
                Step = useUiRange ? _step : 1.0,
                FineMinusRange = useUiRange ? _fineMinusRange : 0.0,
                FinePlusRange = useUiRange ? _finePlusRange : 0.0,
                FineStep = useUiRange ? _fineStep : 1.0,
                RepeatCount = useUiRange ? _repeatCount : 1,
                MoveVelocity = _moveVelocity,
                MoveAcceleration = _moveAcceleration,
                MoveDeceleration = _moveDeceleration,
                SettleDelayMs = useUiRange ? _settleDelayMs : 0,
                MotionTimeoutMs = _motionTimeoutMs,
                VisionTimeoutMs = _visionTimeoutMs,
                VisionBestTimeoutMs = _visionBestTimeoutMs,
                FocusValueReceiveMode = _focusValueReceiveMode,
                ReturnToDefaultAfterScan = _returnToDefaultAfterScan,
                UpdatedBy = UserSession.Name
            };
        }

        private void LoadSettingsToUi()
        {
            try
            {
                _loading = true;
                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                VisionFocusScanSettings settings = ResolveSettings(host.Machine, _selectedKind);
                _minusRange = settings.MinusRange;
                _plusRange = settings.PlusRange;
                _step = settings.Step;
                _fineMinusRange = settings.FineMinusRange;
                _finePlusRange = settings.FinePlusRange;
                _fineStep = settings.FineStep;
                _repeatCount = settings.RepeatCount;
                _moveVelocity = settings.MoveVelocity;
                _moveAcceleration = settings.MoveAcceleration;
                _moveDeceleration = settings.MoveDeceleration;
                _settleDelayMs = settings.SettleDelayMs;
                _motionTimeoutMs = settings.MotionTimeoutMs;
                _visionTimeoutMs = settings.VisionTimeoutMs;
                _visionBestTimeoutMs = settings.VisionBestTimeoutMs;
                _focusValueReceiveMode = settings.FocusValueReceiveMode;
                _returnToDefaultAfterScan = settings.ReturnToDefaultAfterScan;
                _defaultPosition = ResolveSavedDefaultPosition(host.Machine);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Focus Cal 설정 로드 실패: " + ex.Message;
            }
            finally
            {
                _loading = false;
                RefreshSettingGrid();
            }
        }

        private bool SaveSettingsFromUi(bool showMessage)
        {
            try
            {
                if (!ApplyAllSettingRowsFromGrid())
                    return false;

                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                {
                    lblStatus.Text = reason;
                    return false;
                }

                VisionFocusScanSettings settings = ResolveSettings(host.Machine, _selectedKind);
                settings.MinusRange = _minusRange;
                settings.PlusRange = _plusRange;
                settings.Step = _step;
                settings.FineMinusRange = _fineMinusRange;
                settings.FinePlusRange = _finePlusRange;
                settings.FineStep = _fineStep;
                settings.RepeatCount = _repeatCount;
                settings.MoveVelocity = _moveVelocity;
                settings.MoveAcceleration = _moveAcceleration;
                settings.MoveDeceleration = _moveDeceleration;
                settings.SettleDelayMs = _settleDelayMs;
                settings.MotionTimeoutMs = _motionTimeoutMs;
                settings.VisionTimeoutMs = _visionTimeoutMs;
                settings.VisionBestTimeoutMs = _visionBestTimeoutMs;
                settings.FocusValueReceiveMode = _focusValueReceiveMode;
                settings.ReturnToDefaultAfterScan = _returnToDefaultAfterScan;

                VisionFocusPositionRecord record = ResolveSelectedRecord(host.Machine);
                if (record != null)
                {
                    record.DefaultPosition = _defaultPosition;
                    record.UpdatedAt = DateTime.Now;
                    record.UpdatedBy = UserSession.Name ?? string.Empty;
                }

                host.SaveMachineSettings();
                RefreshSavedGrid();
                if (showMessage)
                    lblStatus.Text = "Vision Focus Cal 설정값을 저장했습니다.";
                return true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Vision Focus Cal 설정 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-SAVE", lblStatus.Text);
                return false;
            }
            finally
            {
            }
        }

        private void ApplyBestFocusToInspectionPosition()
        {
            try
            {
                if (_busy)
                    return;

                if (!ApplyAllSettingRowsFromGrid())
                    return;

                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                VisionFocusPositionRecord record = ResolveSelectedRecord(host.Machine);
                if (record == null || !record.Valid)
                {
                    lblStatus.Text = "적용할 Best Focus 결과가 없습니다. 먼저 START SCAN을 실행하세요.";
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string targetName;
                double oldPosition = ResolveInspectionTeachingPosition(host.Machine, out targetName);
                double bestPosition = record.BestPosition;
                string message = "Best Focus를 검사 기준 위치에 적용하시겠습니까?" + Environment.NewLine +
                                 "Target : " + targetName + Environment.NewLine +
                                 "Current: " + oldPosition.ToString("F3") + Environment.NewLine +
                                 "Best   : " + bestPosition.ToString("F3");

                DialogResult answer = QMC.Common.MessageDialog.Show(
                    this,
                    message,
                    "VISION FOCUS CAL",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;

                ApplyInspectionTeachingPosition(host.Machine, bestPosition);
                record.DefaultPosition = bestPosition;
                record.UpdatedAt = DateTime.Now;
                record.UpdatedBy = UserSession.Name ?? string.Empty;
                _defaultPosition = bestPosition;
                host.SaveMachineSettings();
                RefreshSettingGrid();
                RefreshSavedGrid();

                lblStatus.Text = "Best Focus 적용 완료. " + targetName +
                                 " = " + bestPosition.ToString("F3");
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-APPLY-BEST",
                    lblStatus.Text + ", old=" + oldPosition.ToString("F3") +
                    ", score=" + record.BestScore.ToString("F4"));
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Best Focus 적용 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-APPLY-BEST-EX", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool ApplyAllSettingRowsFromGrid()
        {
            try
            {
                if (gridSettings.IsCurrentCellDirty)
                    gridSettings.CommitEdit(DataGridViewDataErrorContexts.Commit);
                gridSettings.EndEdit();

                foreach (DataGridViewRow row in gridSettings.Rows)
                {
                    if (row == null || row.IsNewRow || row.ReadOnly)
                        continue;

                    SettingRowInfo info = row.Tag as SettingRowInfo;
                    if (info == null)
                        continue;

                    string value = Convert.ToString(row.Cells[colSettingValue.Index].Value, CultureInfo.InvariantCulture);
                    if (info.Numeric)
                        ApplyNumericSetting(info, value);
                    else
                        ApplySettingValue(row);
                }

                RefreshSettingGrid();
                return true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Focus 설정값 적용 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-APPLY-UI", lblStatus.Text);
                return false;
            }
            finally
            {
            }
        }

        private void RefreshSettingGrid()
        {
            try
            {
                bool oldLoading = _loading;
                _loading = true;
                gridSettings.Rows.Clear();

                AddSettingRow(CreateOptionInfo(FocusSettingKey.Mode, "Mode", "Focus Scan 대상 모드입니다.", ModeOptions), KindToText(_selectedKind), true);
                bool bottomFocus = IsBottomFocusKind(_selectedKind);
                string pickerNoName = _selectedKind == VisionFocusScanKind.BottomDie ? "Picker No" : "Collet No";
                AddSettingRow(CreateOptionInfo(FocusSettingKey.PickerSide, "Picker Side", "Bottom Focus에서 사용할 Front/Rear Picker를 선택합니다.", SideOptions), SideToText(_selectedPickerSide), bottomFocus);
                AddSettingRow(CreateOptionInfo(FocusSettingKey.ColletNo, pickerNoName, "Bottom Focus에서 측정할 Picker 번호입니다.", ColletOptions), _selectedPickerNo.ToString(CultureInfo.InvariantCulture), bottomFocus);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.DefaultPosition, "Default Pos (mm)", "mm", "Focus 기준 위치입니다. 저장된 Focus Cal 등록값만 불러오며, USE CURRENT로 현재 축 위치를 덮어쓸 수 있습니다.", false), FormatDouble(_defaultPosition), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MinusRange, "Rough - Range (mm)", "mm", "Default Pos 기준 Rough 마이너스 방향으로 스캔할 거리입니다.", false), FormatDouble(_minusRange), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.PlusRange, "Rough + Range (mm)", "mm", "Default Pos 기준 Rough 플러스 방향으로 스캔할 거리입니다.", false), FormatDouble(_plusRange), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.Step, "Rough Step (mm)", "mm", "Rough Focus 측정 지점 사이의 이동 간격입니다.", false), FormatDouble(_step), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.FineMinusRange, "Fine - Range (mm)", "mm", "Rough Best Focus 기준 Fine 마이너스 방향으로 재스캔할 거리입니다.", false), FormatDouble(_fineMinusRange), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.FinePlusRange, "Fine + Range (mm)", "mm", "Rough Best Focus 기준 Fine 플러스 방향으로 재스캔할 거리입니다.", false), FormatDouble(_finePlusRange), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.FineStep, "Fine Step (mm)", "mm", "Fine Focus 측정 지점 사이의 이동 간격입니다.", false), FormatDouble(_fineStep), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.RepeatCount, "Repeat Count (ea)", "ea", "동일 스캔 범위를 반복 측정할 횟수입니다.", true), _repeatCount.ToString(CultureInfo.InvariantCulture), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MoveVelocity, "Motor Speed (mm/s)", "mm/s", "Focus Scan 축 이동에 사용할 속도입니다.", false), FormatDouble(_moveVelocity), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MoveAcceleration, "Acceleration (mm/s2)", "mm/s2", "Focus Scan 축 이동에 사용할 가속도입니다.", false), FormatDouble(_moveAcceleration), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MoveDeceleration, "Deceleration (mm/s2)", "mm/s2", "Focus Scan 축 이동에 사용할 감속도입니다.", false), FormatDouble(_moveDeceleration), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.SettleDelay, "Settle Time (ms)", "ms", "각 위치 도착 후 Vision Focus 값을 요청하기 전 대기 시간입니다.", true), _settleDelayMs.ToString(CultureInfo.InvariantCulture), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MotionTimeout, "Motion Timeout (ms)", "ms", "축 이동 완료 대기 시간입니다.", true), _motionTimeoutMs.ToString(CultureInfo.InvariantCulture), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.VisionTimeout, "Vision Timeout (ms)", "ms", "VisionPC Focus 응답 대기 시간입니다.", true), _visionTimeoutMs.ToString(CultureInfo.InvariantCulture), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.VisionBestTimeout, "Best Timeout (ms)", "ms", "VisionPC FOCUS_BEST 응답 대기 시간입니다. 백그라운드 Focus 점수 처리가 완료될 때까지 기다립니다.", true), _visionBestTimeoutMs.ToString(CultureInfo.InvariantCulture), true);
                AddSettingRow(CreateOptionInfo(FocusSettingKey.FocusValueMode, "Focus Val Mode", "Ack Only는 FOCUS_VAL 그랩 ACK만 받고 진행하며 최종 점수는 FOCUS_BEST에서만 받습니다. Wait Result는 테스트용 기존 대기 모드입니다.", FocusValueModeOptions), FocusValueModeToText(_focusValueReceiveMode), true);
                AddSettingRow(CreateOptionInfo(FocusSettingKey.ReturnDefault, "Return Default", "스캔 완료 후 Default Pos로 복귀할지 선택합니다.", BoolOptions), _returnToDefaultAfterScan ? "True" : "False", true);

                _loading = oldLoading;
            }
            catch
            {
                _loading = false;
            }
            finally
            {
            }
        }

        private void AddSettingRow(SettingRowInfo info, string value, bool enabled)
        {
            int rowIndex = gridSettings.Rows.Add();
            DataGridViewRow row = gridSettings.Rows[rowIndex];
            row.Tag = info;
            row.Cells[colSettingName.Index].Value = info.Name;
            row.Cells[colSettingUnit.Index].Value = info.Unit;

            if (info.Options != null)
            {
                var combo = new DataGridViewComboBoxCell();
                combo.FlatStyle = FlatStyle.Flat;
                combo.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                combo.Items.AddRange(info.Options);
                combo.Value = value;
                row.Cells[colSettingValue.Index] = combo;
            }
            else
            {
                row.Cells[colSettingValue.Index].Value = value;
            }

            row.ReadOnly = !enabled;
            row.Cells[colSettingValue.Index].ReadOnly = !enabled || info.Numeric;
            row.DefaultCellStyle.ForeColor = enabled ? System.Drawing.Color.Black : System.Drawing.Color.Gray;
            row.Cells[colSettingName.Index].ToolTipText = info.ToolTip;
            row.Cells[colSettingValue.Index].ToolTipText = info.ToolTip;
            row.Cells[colSettingUnit.Index].ToolTipText = info.ToolTip;
        }

        private SettingRowInfo CreateNumberInfo(FocusSettingKey key, string name, string unit, string toolTip, bool integer)
        {
            return new SettingRowInfo(key, name, unit, toolTip, true, integer, null);
        }

        private SettingRowInfo CreateOptionInfo(FocusSettingKey key, string name, string toolTip, string[] options)
        {
            return new SettingRowInfo(key, name, string.Empty, toolTip, false, false, options);
        }

        private void ApplySettingValue(DataGridViewRow row)
        {
            SettingRowInfo info = row.Tag as SettingRowInfo;
            if (info == null)
                return;

            string value = Convert.ToString(row.Cells[colSettingValue.Index].Value, CultureInfo.InvariantCulture);
            switch (info.Key)
            {
                case FocusSettingKey.Mode:
                    _selectedKind = TextToKind(value);
                    break;
                case FocusSettingKey.PickerSide:
                    _selectedPickerSide = value == "Rear" ? VisionFocusPickerSide.Rear : VisionFocusPickerSide.Front;
                    break;
                case FocusSettingKey.ColletNo:
                    _selectedPickerNo = Clamp(ParseInt(value, _selectedPickerNo), 1, 4);
                    break;
                case FocusSettingKey.ReturnDefault:
                    _returnToDefaultAfterScan = value == "True";
                    break;
                case FocusSettingKey.FocusValueMode:
                    _focusValueReceiveMode = TextToFocusValueMode(value);
                    break;
            }
        }

        private void ApplyNumericSetting(SettingRowInfo info, string text)
        {
            if (info.Integer)
            {
                int value = ParseInt(text, 0);
                ApplyIntegerSetting(info.Key, value);
                return;
            }

            double doubleValue = ParseDouble(text, 0.0);
            ApplyDoubleSetting(info.Key, doubleValue);
        }

        private void ApplyIntegerSetting(FocusSettingKey key, int value)
        {
            switch (key)
            {
                case FocusSettingKey.RepeatCount:
                    _repeatCount = Clamp(value, 1, 100);
                    break;
                case FocusSettingKey.SettleDelay:
                    _settleDelayMs = Clamp(value, 0, 10000);
                    break;
                case FocusSettingKey.MotionTimeout:
                    _motionTimeoutMs = Clamp(value, 100, 60000);
                    break;
                case FocusSettingKey.VisionTimeout:
                    _visionTimeoutMs = Clamp(value, 100, 60000);
                    break;
                case FocusSettingKey.VisionBestTimeout:
                    _visionBestTimeoutMs = Clamp(value, 1000, 300000);
                    break;
            }
        }

        private void ApplyDoubleSetting(FocusSettingKey key, double value)
        {
            switch (key)
            {
                case FocusSettingKey.DefaultPosition:
                    _defaultPosition = Clamp(value, -9999.0, 9999.0);
                    break;
                case FocusSettingKey.MinusRange:
                    _minusRange = Clamp(value, 0.0, 100.0);
                    break;
                case FocusSettingKey.PlusRange:
                    _plusRange = Clamp(value, 0.0, 100.0);
                    break;
                case FocusSettingKey.Step:
                    _step = Clamp(value, 0.001, 10.0);
                    break;
                case FocusSettingKey.FineMinusRange:
                    _fineMinusRange = Clamp(value, 0.0, 100.0);
                    break;
                case FocusSettingKey.FinePlusRange:
                    _finePlusRange = Clamp(value, 0.0, 100.0);
                    break;
                case FocusSettingKey.FineStep:
                    _fineStep = Clamp(value, 0.001, 10.0);
                    break;
                case FocusSettingKey.MoveVelocity:
                    _moveVelocity = Clamp(value, 0.001, 10000.0);
                    break;
                case FocusSettingKey.MoveAcceleration:
                    _moveAcceleration = Clamp(value, 0.001, 100000.0);
                    break;
                case FocusSettingKey.MoveDeceleration:
                    _moveDeceleration = Clamp(value, 0.001, 100000.0);
                    break;
            }
        }

        private void RefreshSavedGrid()
        {
            try
            {
                gridSaved.Rows.Clear();
                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                    return;

                VisionFocusCalibrationData data = host.Machine.VisionUnit.Config.FocusCalibration;
                data.EnsureObjects();
                if (IsBottomFocusKind(_selectedKind))
                {
                    for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                        AddSavedRow(
                            SideToText(_selectedPickerSide) + " " + ResolveBottomTargetText(_selectedKind) + " #" + pickerNo,
                            data.GetBottomRecord(_selectedKind, _selectedPickerSide, pickerNo));
                    return;
                }

                AddSavedRow("Front 0deg", data.FrontSide0);
                AddSavedRow("Front 90deg", data.FrontSide90);
                AddSavedRow("Rear 0deg", data.RearSide0);
                AddSavedRow("Rear 90deg", data.RearSide90);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void AddSavedRow(string name, VisionFocusPositionRecord record)
        {
            if (record == null)
                return;

            gridSaved.Rows.Add(
                name,
                record.DefaultPosition.ToString("F3"),
                record.BestPosition.ToString("F3"),
                record.BestScore.ToString("F4"),
                record.Valid ? "Y" : "N");
        }

        private void PopulateSamples(VisionFocusScanResult result)
        {
            gridSamples.Rows.Clear();
            if (result == null || result.Samples == null)
                return;

            foreach (VisionFocusScanSample sample in result.Samples)
            {
                gridSamples.Rows.Add(
                    sample.No,
                    sample.Position.ToString("F3"),
                    sample.Score.ToString("F4"),
                    sample.Success ? "OK" : "NG",
                    sample.Raw ?? string.Empty);
            }
        }

        private VisionFocusScanSettings ResolveSettings(CDT320_Machine machine, VisionFocusScanKind kind)
        {
            machine.VisionUnit.Config.EnsureCalibrationObjects();
            VisionFocusCalibrationData data = machine.VisionUnit.Config.FocusCalibration;
            data.EnsureObjects();
            if (kind == VisionFocusScanKind.BottomCollet)
                return data.BottomColletScan;
            if (kind == VisionFocusScanKind.BottomDie)
                return data.BottomDieScan;
            return data.SideVisionScan;
        }

        private VisionFocusPositionRecord ResolveSelectedRecord(CDT320_Machine machine)
        {
            machine.VisionUnit.Config.EnsureCalibrationObjects();
            VisionFocusCalibrationData data = machine.VisionUnit.Config.FocusCalibration;
            return IsBottomFocusKind(_selectedKind)
                ? data.GetBottomRecord(_selectedKind, _selectedPickerSide, _selectedPickerNo)
                : data.GetSideRecord(_selectedKind);
        }

        private double ResolveCurrentAxisPosition(CDT320_Machine machine)
        {
            if (IsBottomFocusKind(_selectedKind))
            {
                int pickerNo = _selectedPickerNo;
                if (_selectedPickerSide == VisionFocusPickerSide.Front)
                {
                    if (pickerNo == 1) return machine.PickerFrontUnit.PickerZ0.ActualPosition;
                    if (pickerNo == 2) return machine.PickerFrontUnit.PickerZ1.ActualPosition;
                    if (pickerNo == 3) return machine.PickerFrontUnit.PickerZ2.ActualPosition;
                    return machine.PickerFrontUnit.PickerZ3.ActualPosition;
                }

                if (pickerNo == 1) return machine.PickerRearUnit.PickerZ0.ActualPosition;
                if (pickerNo == 2) return machine.PickerRearUnit.PickerZ1.ActualPosition;
                if (pickerNo == 3) return machine.PickerRearUnit.PickerZ2.ActualPosition;
                return machine.PickerRearUnit.PickerZ3.ActualPosition;
            }

            if (_selectedKind == VisionFocusScanKind.FrontSide0 || _selectedKind == VisionFocusScanKind.FrontSide90)
                return machine.VisionUnit.FrontSideVisionY.ActualPosition;
            return machine.VisionUnit.RearSideVisionY.ActualPosition;
        }

        private double ResolveSavedDefaultPosition(CDT320_Machine machine)
        {
            VisionFocusPositionRecord record = ResolveSelectedRecord(machine);
            return HasSavedDefaultPosition(record) ? record.DefaultPosition : 0.0;
        }

        private double ResolveInspectionTeachingPosition(CDT320_Machine machine, out string targetName)
        {
            targetName = string.Empty;
            if (machine == null)
                return 0.0;

            if (IsBottomFocusKind(_selectedKind))
            {
                PickerAxis zAxis = ResolveSelectedPickerZAxis();
                string sideText = SideToText(_selectedPickerSide);
                targetName = sideText + "." + zAxis + ".BottomPosition";
                if (_selectedPickerSide == VisionFocusPickerSide.Front)
                    return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.GetPickerTeachingPosition(zAxis, "BottomPosition") : 0.0;
                return machine.PickerRearUnit != null ? machine.PickerRearUnit.GetPickerTeachingPosition(zAxis, "BottomPosition") : 0.0;
            }

            if (machine.VisionUnit == null)
                return 0.0;

            VisionAxis axis = ResolveSelectedSideVisionAxis();
            string positionName = ResolveSelectedSideVisionPositionName();
            targetName = axis + "." + positionName;
            return machine.VisionUnit.GetVisionTeachingPosition(axis, positionName);
        }

        private void ApplyInspectionTeachingPosition(CDT320_Machine machine, double position)
        {
            if (machine == null)
                return;

            if (IsBottomFocusKind(_selectedKind))
            {
                PickerAxis zAxis = ResolveSelectedPickerZAxis();
                if (_selectedPickerSide == VisionFocusPickerSide.Front && machine.PickerFrontUnit != null)
                    machine.PickerFrontUnit.SetPickerAxisTeachingPosition(zAxis, "BottomPosition", position);
                else if (_selectedPickerSide == VisionFocusPickerSide.Rear && machine.PickerRearUnit != null)
                    machine.PickerRearUnit.SetPickerAxisTeachingPosition(zAxis, "BottomPosition", position);
                return;
            }

            if (machine.VisionUnit != null)
                machine.VisionUnit.SetVisionAxisTeachingPosition(
                    ResolveSelectedSideVisionAxis(),
                    ResolveSelectedSideVisionPositionName(),
                    position);
        }

        private async Task<int> MoveSelectedPickerZToAvoidAsync(CDT320_Machine machine)
        {
            if (machine == null)
                return -1;

            PickerAxis zAxis = ResolveSelectedPickerZAxis();
            if (_selectedPickerSide == VisionFocusPickerSide.Front)
            {
                if (machine.PickerFrontUnit == null)
                    return -1;
                if (machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return 0;
                return await machine.PickerFrontUnit.MovePickerZToSafeHeight(_selectedPickerNo).ConfigureAwait(true);
            }

            if (machine.PickerRearUnit == null)
                return -1;
            if (machine.PickerRearUnit.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                return 0;
            return await machine.PickerRearUnit.MovePickerZToSafeHeight(_selectedPickerNo).ConfigureAwait(true);
        }

        private async Task<int> MoveSelectedPickerYToAvoidAsync(CDT320_Machine machine)
        {
            if (machine == null)
                return -1;

            if (_selectedPickerSide == VisionFocusPickerSide.Front)
            {
                if (machine.PickerFrontUnit == null)
                    return -1;
                if (machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                    return 0;
                return await machine.PickerFrontUnit.MovePickerAxisToTeachingPosition(PickerAxis.PickerY, "AvoidPosition").ConfigureAwait(true);
            }

            if (machine.PickerRearUnit == null)
                return -1;
            if (machine.PickerRearUnit.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                return 0;
            return await machine.PickerRearUnit.MovePickerAxisToTeachingPosition(PickerAxis.PickerY, "AvoidPosition").ConfigureAwait(true);
        }

        private bool IsSelectedPickerZInAvoidPosition(CDT320_Machine machine)
        {
            if (machine == null)
                return false;

            PickerAxis zAxis = ResolveSelectedPickerZAxis();
            if (_selectedPickerSide == VisionFocusPickerSide.Front)
                return machine.PickerFrontUnit != null &&
                       machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition");

            return machine.PickerRearUnit != null &&
                   machine.PickerRearUnit.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition");
        }

        private VisionAxis ResolveSelectedSideVisionAxis()
        {
            if (_selectedKind == VisionFocusScanKind.FrontSide0 || _selectedKind == VisionFocusScanKind.FrontSide90)
                return VisionAxis.FrontSideVisionY;
            return VisionAxis.RearSideVisionY;
        }

        private string ResolveSelectedSideVisionPositionName()
        {
            if (_selectedKind == VisionFocusScanKind.FrontSide90 ||
                _selectedKind == VisionFocusScanKind.RearSide90)
                return "Process90Position";
            return "Process0Position";
        }

        private static bool HasSavedDefaultPosition(VisionFocusPositionRecord record)
        {
            if (record == null)
                return false;

            return record.Valid ||
                   record.UpdatedAt != default(DateTime) ||
                   Math.Abs(record.DefaultPosition) > 0.0000001;
        }

        private PickerAxis ResolveSelectedPickerZAxis()
        {
            if (_selectedPickerNo <= 1)
                return PickerAxis.PickerZ0;
            if (_selectedPickerNo == 2)
                return PickerAxis.PickerZ1;
            if (_selectedPickerNo == 3)
                return PickerAxis.PickerZ2;
            return PickerAxis.PickerZ3;
        }

        private string BuildSelectedPickerAxisLabel(PickerAxis axis)
        {
            return SideToText(_selectedPickerSide) + "." + axis;
        }

        private bool CanRunManualCalibration(out string reason)
        {
            reason = string.Empty;
            try
            {
                Form1 host = ResolveHost(out reason);
                if (host == null)
                    return false;

                if (AlarmManager.HasActive)
                {
                    reason = "현재 알람 상태입니다. 알람 해제 후 Vision Focus Cal을 실행하세요.";
                    return false;
                }

                if (host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않았습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.AutoRunning)
                {
                    reason = "자동 운전 중에는 Vision Focus Cal을 실행할 수 없습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.ManualRunning || host.Controller.IsManualBusy)
                {
                    reason = "다른 수동 동작이 실행 중입니다. 완료 후 다시 실행하세요.";
                    return false;
                }

                if (host.Controller.IsSequenceRunning)
                {
                    reason = "시퀀스가 실행 중입니다. 완료 후 Vision Focus Cal을 실행하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Vision Focus Cal 실행 조건 확인 실패: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private Form1 ResolveHost(out string reason)
        {
            reason = string.Empty;
            Form1 host = Owner as Form1;
            if (host == null)
                host = FindHostForm();

            if (host == null)
            {
                reason = "Main 화면을 찾을 수 없어 Vision Focus Cal을 실행할 수 없습니다.";
                return null;
            }

            if (host.Machine == null || host.Machine.VisionUnit == null)
            {
                reason = "장비 또는 VisionUnit이 준비되지 않았습니다.";
                return null;
            }

            return host;
        }

        private Form1 FindHostForm()
        {
            Control parent = this;
            while (parent != null)
            {
                Form1 host = parent as Form1;
                if (host != null)
                    return host;
                parent = parent.Parent;
            }

            foreach (Form form in Application.OpenForms)
            {
                Form1 host = form as Form1;
                if (host != null)
                    return host;
            }

            return null;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            gridSettings.Enabled = enabled;
            btnCheck.Enabled = enabled;
            btnUseCurrent.Enabled = enabled;
            btnMoveDefault.Enabled = enabled;
            btnMoveZAvoid.Enabled = enabled;
            btnMoveYAvoid.Enabled = enabled;
            btnStartScan.Enabled = enabled;
            btnApplyBest.Enabled = enabled;
            btnReload.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnClose.Enabled = enabled;
        }

        private static VisionFocusScanKind TextToKind(string text)
        {
            if (text == "Bottom Die") return VisionFocusScanKind.BottomDie;
            if (text == "Front Side 0deg") return VisionFocusScanKind.FrontSide0;
            if (text == "Front Side 90deg") return VisionFocusScanKind.FrontSide90;
            if (text == "Rear Side 0deg") return VisionFocusScanKind.RearSide0;
            if (text == "Rear Side 90deg") return VisionFocusScanKind.RearSide90;
            return VisionFocusScanKind.BottomCollet;
        }

        private static string KindToText(VisionFocusScanKind kind)
        {
            switch (kind)
            {
                case VisionFocusScanKind.BottomDie: return "Bottom Die";
                case VisionFocusScanKind.FrontSide0: return "Front Side 0deg";
                case VisionFocusScanKind.FrontSide90: return "Front Side 90deg";
                case VisionFocusScanKind.RearSide0: return "Rear Side 0deg";
                case VisionFocusScanKind.RearSide90: return "Rear Side 90deg";
                default: return "Bottom Collet";
            }
        }

        private static string SideToText(VisionFocusPickerSide side)
        {
            return side == VisionFocusPickerSide.Rear ? "Rear" : "Front";
        }

        private static string FocusValueModeToText(VisionFocusValueReceiveMode mode)
        {
            return mode == VisionFocusValueReceiveMode.WaitResultForTest
                ? "Wait Result (Test)"
                : "Ack Only";
        }

        private static VisionFocusValueReceiveMode TextToFocusValueMode(string text)
        {
            return string.Equals(text, "Wait Result (Test)", StringComparison.OrdinalIgnoreCase)
                ? VisionFocusValueReceiveMode.WaitResultForTest
                : VisionFocusValueReceiveMode.AckOnly;
        }

        private static bool IsBottomFocusKind(VisionFocusScanKind kind)
        {
            return kind == VisionFocusScanKind.BottomCollet ||
                   kind == VisionFocusScanKind.BottomDie;
        }

        private static string ResolveBottomTargetText(VisionFocusScanKind kind)
        {
            return kind == VisionFocusScanKind.BottomDie ? "Die" : "Collet";
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static double ParseDouble(string text, double fallback)
        {
            double value;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private static int ParseInt(string text, int fallback)
        {
            int value;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
