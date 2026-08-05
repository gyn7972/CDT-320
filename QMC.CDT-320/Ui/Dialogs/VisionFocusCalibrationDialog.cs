using System;
using System.Collections.Generic;
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
    internal enum VisionFocusCalibrationDialogProfile
    {
        BottomOnly,
        SideOnly
    }

    public partial class VisionFocusCalibrationDialog : Form
    {
        private enum FocusSettingKey
        {
            Mode,
            PickerSide,
            ColletNo,
            PickerReferenceX,
            PickerReferenceY,
            PickerReferenceZ,
            PickerReferenceT,
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
            ReturnDefault,
            AutoFocusOnStart,
            AutoFocusOnWaferChange,
            AutoFocusOnPickCount,
            AutoFocusPickInterval,
            BottomVisionDelay,
            AutoFocusToBottomInspectionDelay,
            UseBottomToSideZOffset,
            BottomToSideZOffset,
            SideFocusSize90SignFront,
            SideFocusSize90SignRear,
            SideFocusCoc0SignFront,
            SideFocusCoc0SignRear,
            SideFocusCoc90SignFront,
            SideFocusCoc90SignRear
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

        private sealed class BatchFocusTarget
        {
            public VisionFocusPickerSide Side;
            public int PickerNo;

            public string Label
            {
                get { return (Side == VisionFocusPickerSide.Front ? "F" : "R") + " P" + PickerNo; }
            }
        }

        private sealed class FocusResultSnapshot
        {
            public VisionFocusScanKind Kind;
            public VisionFocusPickerSide Side;
            public int PickerNo;
            public double DefaultPosition;
            public double BestPosition;
            public double BestScore;
            public int SampleCount;
            public double PickerZPosition;
            public bool PickerZValid;
            public string UpdatedBy;
        }

        private static readonly string[] BottomModeOptions =
        {
            "Bottom Collet",
            "Bottom Die"
        };

        private static readonly string[] SideModeOptions =
        {
            "Front Side 0deg",
            "Front Side 90deg",
            "Rear Side 0deg",
            "Rear Side 90deg"
        };

        private static readonly string[] SideOptions = { "Front", "Rear" };
        private static readonly string[] ColletOptions = { "1", "2", "3", "4" };
        private static readonly string[] BoolOptions = { "True", "False" };
        private static readonly string[] FocusValueModeOptions = { "Ack Only", "Wait Result (Test)" };

        private readonly VisionFocusCalibrationDialogProfile _profile;
        private bool _loading;
        private bool _busy;
        private VisionFocusScanKind _selectedKind = VisionFocusScanKind.BottomDie;
        private VisionFocusPickerSide _selectedPickerSide = VisionFocusPickerSide.Front;
        private int _selectedPickerNo = 1;
        private double _pickerReferenceX;
        private double _pickerReferenceY;
        private double _pickerReferenceZ;
        private double _pickerReferenceT;
        private string _pickerReferenceFormula = string.Empty;
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
        private bool _autoFocusOnStartEnabled;
        private bool _autoFocusOnWaferChange = true;
        private bool _autoFocusOnPickCountEnabled;
        private int _autoFocusPickInterval;
        private int _bottomVisionDelayMs;
        private int _autoFocusToBottomInspectionDelayMs = 300;
        // Side 전용: Bottom↔Side 공용 Z옵셋과 COC/다이사이즈 보정 부호 (VisionFocusCalibrationData 최상위 저장)
        private bool _useBottomToSideZOffset;
        private double _bottomToSideZOffsetMm;
        private double _sideFocusSize90SignFront = -1.0;
        private double _sideFocusSize90SignRear = 1.0;
        private double _sideFocusCoc0SignFront = 1.0;
        private double _sideFocusCoc0SignRear = 1.0;
        private double _sideFocusCoc90SignFront = 1.0;
        private double _sideFocusCoc90SignRear = 1.0;
        private CancellationTokenSource _runCts;
        private Action _activeStopRequest;
        private System.Windows.Forms.Timer _runtimeRefreshTimer;
        private bool _suppressBatchAllChange;
        private FocusResultSnapshot _lastSuccessfulResult;

        public static VisionFocusCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "VisionFocusCalibrationDialog",
                owner,
                () => new VisionFocusCalibrationDialog());
        }

        public VisionFocusCalibrationDialog()
            : this(VisionFocusCalibrationDialogProfile.BottomOnly)
        {
        }

        internal VisionFocusCalibrationDialog(VisionFocusCalibrationDialogProfile profile)
        {
            try
            {
                _profile = profile;
                if (_profile == VisionFocusCalibrationDialogProfile.SideOnly)
                    _selectedKind = VisionFocusScanKind.FrontSide0;

                InitializeComponent();
                ApplyDialogProfile();
                CalibrationDialogGridBehavior.Apply(gridSettings, gridSamples, gridSaved);
                ConfigureEditableSettingGrid();
                InitializeRuntime();
                StartRuntimeRefreshTimer();
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
                UpdateStopButtonEnabled();
                UpdateResultSaveButtonEnabled();
                lblStatus.Text = IsSideOnlyProfile
                    ? "대기 중입니다. Side Focus 기준 위치를 확인한 뒤 START SCAN을 실행하세요."
                    : "대기 중입니다. Focus 기준 위치를 확인한 뒤 START SCAN을 실행하세요.";
            }
            finally
            {
                _loading = false;
                RefreshSettingGrid();
            }
        }

        private bool IsSideOnlyProfile
        {
            get { return _profile == VisionFocusCalibrationDialogProfile.SideOnly; }
        }

        private string[] ResolveModeOptions()
        {
            return IsSideOnlyProfile ? SideModeOptions : BottomModeOptions;
        }

        private void ApplyDialogProfile()
        {
            if (!IsSideOnlyProfile)
                return;

            Text = "Side Vision Focus Calibration";
            lblHeader.Text = "SIDE VISION FOCUS CAL";

            // Side 전용 창에서는 Bottom Picker 복귀/Runtime AF 전용 명령을 표시하지 않는다.
            btnMoveZAvoid.Visible = false;
            btnMoveYAvoid.Visible = false;
            btnResetAutoFocus.Visible = false;
        }

        private void ApplyButtonStyle()
        {
            CalibrationDialogButtonStyle.ApplyFooterButtons(
                new[] { btnCheck, btnUseCurrent, btnMoveDefault, btnMoveZAvoid, btnMoveYAvoid, btnSeqStop, btnApplyBest, btnResetAutoFocus, btnReload, btnClose },
                new[] { btnStartScan, btnBatchStart },
                new[] { btnSaveParameters, btnSave });
        }

        private void StartRuntimeRefreshTimer()
        {
            _runtimeRefreshTimer = new System.Windows.Forms.Timer();
            _runtimeRefreshTimer.Interval = 2000;
            _runtimeRefreshTimer.Tick += delegate
            {
                if (_busy || _loading || IsDisposed)
                    return;
                RefreshSavedGrid();
            };
            _runtimeRefreshTimer.Start();
        }

        private void ConfigureEditableSettingGrid()
        {
            gridSettings.ReadOnly = false;
            gridSettings.EditMode = DataGridViewEditMode.EditOnEnter;
            colSettingName.ReadOnly = true;
            colSettingValue.ReadOnly = false;
            colSettingUnit.ReadOnly = true;
        }

        private void gridSettings_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != colSettingValue.Index)
                return;

            DataGridViewComboBoxCell comboCell = gridSettings.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
            if (comboCell == null || comboCell.ReadOnly)
                return;

            gridSettings.CurrentCell = comboCell;
            if (gridSettings.BeginEdit(true) && gridSettings.EditingControl is ComboBox combo)
                combo.DroppedDown = true;
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
                if (info.Key == FocusSettingKey.Mode)
                {
                    if (IsSideOnlyProfile)
                    {
                        ReloadSelectedTargetReference();
                        RefreshSettingGrid();
                    }
                    else
                    {
                        LoadSettingsToUi();
                    }
                    RefreshSavedGrid();
                }
                else if (info.Key == FocusSettingKey.PickerSide ||
                         info.Key == FocusSettingKey.ColletNo)
                {
                    ReloadSelectedTargetReference();
                    RefreshSettingGrid();
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

        private void gridSaved_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || !IsSideOnlyProfile || e.RowIndex < 0 || e.ColumnIndex != colBestPos.Index)
                return;

            DataGridViewRow row = gridSaved.Rows[e.RowIndex];
            VisionFocusPositionRecord record = row != null ? row.Tag as VisionFocusPositionRecord : null;
            if (record == null)
                return;

            // BEST 위치 직접 수정은 실측 없이 캘리브레이션 값을 바꾸는 조작이며 즉시 저장된다.
            // NeedlePinCalibrationDialog의 결과값 수동 수정과 동일하게 Admin 권한 + 명시적 확인을 요구한다.
            if (!UserSession.Has(UserLevel.Admin))
            {
                lblStatus.Text = "Admin 권한에서만 BEST 위치를 수동 수정할 수 있습니다.";
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "SIDE VISION FOCUS CAL",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (QMC.Common.MessageDialog.Show(
                    this,
                    "BEST 위치를 수동으로 덮어씁니다.\r\n" +
                    "실제 측정 없이 캘리브레이션 값을 바꾸는 조작이며 즉시 저장됩니다.\r\n\r\n진행할까요?",
                    "SIDE VISION FOCUS CAL",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _busy = true;
            try
            {
                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                string itemName = Convert.ToString(row.Cells[colItem.Index].Value, CultureInfo.InvariantCulture);
                string current = record.BestPosition.ToString("0.###", CultureInfo.InvariantCulture);
                using (NumericKeypadDialog dialog = new NumericKeypadDialog(itemName + " BEST", current, "mm"))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    double bestPosition;
                    if (!double.TryParse(dialog.ValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out bestPosition) ||
                        double.IsNaN(bestPosition) || double.IsInfinity(bestPosition))
                    {
                        lblStatus.Text = "BEST 입력값이 올바른 숫자가 아닙니다. value=" + dialog.ValueText;
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "SIDE VISION FOCUS CAL",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    bool frontCamera = _selectedKind == VisionFocusScanKind.FrontSide0 ||
                                       _selectedKind == VisionFocusScanKind.FrontSide90;
                    var axis = frontCamera
                        ? host.Machine.VisionUnit.FrontSideVisionY
                        : host.Machine.VisionUnit.RearSideVisionY;
                    if (axis != null && axis.Setup != null && axis.Setup.SoftLimitEnabled &&
                        (bestPosition < axis.Setup.SoftLimitMinus || bestPosition > axis.Setup.SoftLimitPlus))
                    {
                        lblStatus.Text = "BEST 입력값이 " + axis.Name + " 소프트리밋을 벗어났습니다. target=" +
                                         bestPosition.ToString("F3") +
                                         ", min=" + axis.Setup.SoftLimitMinus.ToString("F3") +
                                         ", max=" + axis.Setup.SoftLimitPlus.ToString("F3");
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "SIDE VISION FOCUS CAL",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    double oldBestPosition = record.BestPosition;
                    bool oldValid = record.Valid;
                    DateTime oldUpdatedAt = record.UpdatedAt;
                    string oldUpdatedBy = record.UpdatedBy;

                    record.BestPosition = bestPosition;
                    record.Valid = true;
                    record.UpdatedAt = DateTime.Now;
                    record.UpdatedBy = UserSession.Name ?? string.Empty;
                    if (!host.Machine.SaveSettings())
                    {
                        record.BestPosition = oldBestPosition;
                        record.Valid = oldValid;
                        record.UpdatedAt = oldUpdatedAt;
                        record.UpdatedBy = oldUpdatedBy;
                        RefreshSavedGrid();
                        lblStatus.Text = "BEST 수동 입력값 저장에 실패했습니다. Machine 설정 저장 상태를 확인하세요.";
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "SIDE VISION FOCUS CAL",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    RefreshSavedGrid();
                    lblStatus.Text = itemName + " BEST 수동 입력 저장 완료. old=" +
                                     oldBestPosition.ToString("F3") + ", new=" + bestPosition.ToString("F3");
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-BEST-MANUAL",
                        lblStatus.Text + ", kind=" + _selectedKind +
                        ", pickerSide=" + _selectedPickerSide +
                        ", valid=True");
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "BEST 수동 입력 처리 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-BEST-MANUAL-EX", lblStatus.Text);
            }
            finally
            {
                _busy = false;
            }
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

        private void btnResetAutoFocus_Click(object sender, EventArgs e)
        {
            ResetSelectedAutoFocusCount();
        }

        private void btnSeqStop_Click(object sender, EventArgs e)
        {
            RequestActiveSequenceStop("SEQ STOP 버튼");
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadSettingsToUi();
            RefreshSavedGrid();
            lblStatus.Text = "Vision Focus Cal 설정값을 다시 불러왔습니다.";
        }

        private void btnSaveParameters_Click(object sender, EventArgs e)
        {
            SaveSettingsFromUi(true);
        }

        private void btnSaveResult_Click(object sender, EventArgs e)
        {
            SaveLastSuccessfulResult();
        }

        private void chkBatchAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressBatchAllChange)
                return;

            SetAllBatchTargets(chkBatchAll.Checked);
        }

        private async void btnBatchStart_Click(object sender, EventArgs e)
        {
            await RunBatchScanAsync().ConfigureAwait(true);
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
                        RequestRunCancelForClose("Vision Focus Calibration 창 종료(" + e.CloseReason + ")");
                        base.OnFormClosing(e);
                        return;
                    }

                    DialogResult result = QMC.Common.MessageDialog.Show(this,
                        "Vision Focus Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?",
                        "VISION FOCUS CAL",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        RequestRunCancelForClose("Vision Focus Calibration 창 닫기");
                        lblStatus.Text = "정지 처리 중입니다. 완료 후 창을 닫으세요.";
                    }

                    e.Cancel = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "VISION-FOCUS-CAL-CLOSE",
                    "Vision Focus Calibration 창 종료 확인 중 예외가 발생했습니다. error=" + ex.Message);
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
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "VISION-FOCUS-CAL-CLOSE-CANCEL",
                    reason + " 중 정지 요청 실패: " + ex.Message);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (_runtimeRefreshTimer != null)
                {
                    _runtimeRefreshTimer.Stop();
                    _runtimeRefreshTimer.Dispose();
                    _runtimeRefreshTimer = null;
                }
            }
            finally
            {
                base.OnFormClosed(e);
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
                "VisionFocusCalibration:" + actionName + ":" + _selectedKind + ":" + _selectedPickerSide + ":" + _selectedPickerNo);
            CancellationTokenSource runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
            _runCts = runCts;
            stopHandler = CreateStopRequestAction(actionName);
            _activeStopRequest = stopHandler;
            UpdateStopButtonEnabled();
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
            if (ReferenceEquals(_activeStopRequest, stopHandler))
                _activeStopRequest = null;

            if (runCts != null)
                runCts.Dispose();

            if (actionScope != null)
                actionScope.Dispose();

            UpdateStopButtonEnabled();
        }

        private Action CreateStopRequestAction(string actionName)
        {
            return delegate
            {
                try
                {
                    CancellationTokenSource cts = _runCts;
                    if (cts != null && !cts.IsCancellationRequested)
                        cts.Cancel();

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionFocusCalStop",
                        "Vision Focus Calibration 정지 요청. action=" + actionName +
                        ", kind=" + _selectedKind +
                        ", side=" + _selectedPickerSide +
                        ", pickerNo=" + _selectedPickerNo);
                }
                catch
                {
                }
            };
        }

        private void RequestActiveSequenceStop(string source)
        {
            try
            {
                Action request = _activeStopRequest;
                if (request == null)
                {
                    lblStatus.Text = "현재 정지 요청할 Vision Focus Calibration 동작이 없습니다.";
                    return;
                }

                request();
                lblStatus.Text = source + "으로 Vision Focus Calibration 정지를 요청했습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Vision Focus Calibration 정지 요청 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-STOP-REQUEST", lblStatus.Text);
            }
            finally
            {
                UpdateStopButtonEnabled();
            }
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

        private void SetAllBatchTargets(bool selected)
        {
            _suppressBatchAllChange = true;
            try
            {
                chkBatchFront4.Checked = selected;
                chkBatchFront3.Checked = selected;
                chkBatchFront2.Checked = selected;
                chkBatchFront1.Checked = selected;
                chkBatchRear4.Checked = selected;
                chkBatchRear3.Checked = selected;
                chkBatchRear2.Checked = selected;
                chkBatchRear1.Checked = selected;
            }
            finally
            {
                _suppressBatchAllChange = false;
            }
        }

        private List<BatchFocusTarget> BuildBatchTargets()
        {
            var targets = new List<BatchFocusTarget>();
            if (chkBatchFront4.Checked) targets.Add(new BatchFocusTarget { Side = VisionFocusPickerSide.Front, PickerNo = 4 });
            if (chkBatchFront3.Checked) targets.Add(new BatchFocusTarget { Side = VisionFocusPickerSide.Front, PickerNo = 3 });
            if (chkBatchFront2.Checked) targets.Add(new BatchFocusTarget { Side = VisionFocusPickerSide.Front, PickerNo = 2 });
            if (chkBatchFront1.Checked) targets.Add(new BatchFocusTarget { Side = VisionFocusPickerSide.Front, PickerNo = 1 });
            if (chkBatchRear4.Checked) targets.Add(new BatchFocusTarget { Side = VisionFocusPickerSide.Rear, PickerNo = 4 });
            if (chkBatchRear3.Checked) targets.Add(new BatchFocusTarget { Side = VisionFocusPickerSide.Rear, PickerNo = 3 });
            if (chkBatchRear2.Checked) targets.Add(new BatchFocusTarget { Side = VisionFocusPickerSide.Rear, PickerNo = 2 });
            if (chkBatchRear1.Checked) targets.Add(new BatchFocusTarget { Side = VisionFocusPickerSide.Rear, PickerNo = 1 });
            return targets;
        }

        private static VisionFocusScanKind ResolveBatchKind(
            VisionFocusScanKind fixedKind,
            VisionFocusPickerSide side)
        {
            if (fixedKind != VisionFocusScanKind.FrontSide0 &&
                fixedKind != VisionFocusScanKind.FrontSide90 &&
                fixedKind != VisionFocusScanKind.RearSide0 &&
                fixedKind != VisionFocusScanKind.RearSide90)
                return fixedKind;

            bool angle90 = fixedKind == VisionFocusScanKind.FrontSide90 ||
                           fixedKind == VisionFocusScanKind.RearSide90;
            if (side == VisionFocusPickerSide.Front)
                return angle90 ? VisionFocusScanKind.FrontSide90 : VisionFocusScanKind.FrontSide0;
            return angle90 ? VisionFocusScanKind.RearSide90 : VisionFocusScanKind.RearSide0;
        }

        private async Task RunBatchScanAsync()
        {
            if (_busy)
                return;

            List<BatchFocusTarget> targets = BuildBatchTargets();
            if (targets.Count == 0)
            {
                lblStatus.Text = "Batch 측정 대상을 하나 이상 선택하세요.";
                QMC.Common.MessageDialog.Show(
                    this,
                    lblStatus.Text,
                    "VISION FOCUS CAL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Form1 host = null;
            Action stopHandler = null;
            IDisposable actionScope = null;
            CancellationTokenSource runCts = null;
            VisionFocusScanKind originalKind = _selectedKind;
            VisionFocusPickerSide originalSide = _selectedPickerSide;
            int originalPickerNo = _selectedPickerNo;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                _lastSuccessfulResult = null;
                UpdateResultSaveButtonEnabled();

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

                originalKind = _selectedKind;
                originalSide = _selectedPickerSide;
                originalPickerNo = _selectedPickerNo;
                gridSamples.Rows.Clear();

                runCts = BeginManualCalibrationRun(host, "BatchStart", out actionScope, out stopHandler);
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());

                for (int index = 0; index < targets.Count; index++)
                {
                    runCts.Token.ThrowIfCancellationRequested();
                    BatchFocusTarget target = targets[index];
                    _selectedKind = ResolveBatchKind(originalKind, target.Side);
                    _selectedPickerSide = target.Side;
                    _selectedPickerNo = target.PickerNo;
                    ReloadSelectedTargetReference();
                    RefreshSettingGrid();
                    RefreshSavedGrid();

                    lblStatus.Text = "Batch " + (index + 1) + "/" + targets.Count +
                                     " " + target.Label + " Focus 측정 중입니다.";
                    VisionFocusScanRequest request = BuildRequest(true);
                    var sequence = new VisionFocusScanSequence(host.Machine, request);
                    int result = await sequence.RunAsync(runCts.Token, SequenceRunMode.Manual).ConfigureAwait(true);
                    PopulateSamples(sequence.Result);
                    RefreshSavedGrid();

                    if (result != 0 || sequence.Result == null || !sequence.Result.Success)
                    {
                        lblStatus.Text = "Batch " + target.Label + " Focus 측정 실패: " +
                                         (sequence.Result != null ? sequence.Result.Message : "결과 없음");
                        QMC.Common.MessageDialog.Show(
                            this,
                            lblStatus.Text,
                            "VISION FOCUS CAL",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    CaptureLastSuccessfulResult(host.Machine, sequence.Result);
                    host.SaveMachineSettings();

                    // ====================================================================
                    // [중복 안전 이동 제거 2026-08-06]  ★실장비 미검증 — 실장비에서 테스트 필요★
                    //
                    // 사용자 지시(2026-08-06): "Vision Focus cal 은 픽커 다른거 할때마다
                    //   안전 위치 이동했다가 다시 또 이동했다가 이러고 있다.
                    //   콜렛 캘리브레이션 안전 위치 조건 시컨스 참고해서 수정해줘."
                    //
                    // 기존 조건: 대상마다 무조건 AutoCalibrationSafePositionSequence 를 돌렸다.
                    //   → 픽커당 [Avoid 진입 → Bottom 진입 → 스캔 → Default 복귀 → 전체 Avoid 복귀]
                    //     가 되어, 다음 픽커에서 다시 Bottom 으로 들어가느라 왕복이 두 번씩 났다.
                    //
                    // Collet 배치 방식(ColletCalibrationDialog.RunSingleColletSequenceAsync:946 주석):
                    //   "BATCH도 상대 Picker를 Avoid로 자동 이동한다(이미 Avoid면 시퀀스가 재이동 없이 통과)."
                    //   즉 대상 사이에 별도 안전 시퀀스를 넣지 않고, 각 시퀀스의 준비 단계가
                    //   필요한 안전 조건만 확보하도록 위임한다(idempotent skip).
                    //
                    // 현재 기준: 대상 사이에서는 안전 시퀀스를 돌리지 않는다.
                    //   다음 대상의 VisionFocusScanSequence.PrepareFocusReadyPositionAsync 가
                    //   EnsureInputOutputVisionAvoidAsync / MoveNonSelectedPickerOutputAvoidAsync /
                    //   MoveSelectedPickerYAndZSafeForOppositePickerXAsync 로 안전 조건을 이미 확보하며,
                    //   각 이동 헬퍼가 IsAxisIdleAtExactPosition 으로 이미 도달한 축은 건너뛴다.
                    //   ★마지막 대상 후에는 그대로 안전 Avoid 로 복귀한다★ — 배치 종료 상태는 안전해야 한다.
                    // ====================================================================
                    bool hasNextTarget = index < targets.Count - 1;
                    if (hasNextTarget)
                    {
                        lblStatus.Text = "Batch " + target.Label +
                                         " 완료. 다음 대상 준비 단계가 안전 조건을 확보합니다(중복 Avoid 복귀 생략).";
                        EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-BATCH-SAFE-SKIP",
                            "Batch 대상 사이 안전 Avoid 복귀를 생략합니다(다음 대상 준비 단계가 확보). " +
                            "completed=" + target.Label +
                            ", next=" + targets[index + 1].Label);
                        continue;
                    }

                    lblStatus.Text = "Batch " + target.Label + " 완료. 최종 안전 Avoid 복귀 중입니다.";
                    var safe = new AutoCalibrationSafePositionSequence(context, target.Side);
                    PickerSequenceOptions options = PickerSequenceOptions.Default();
                    options.RunMode = SequenceRunMode.Manual;
                    options.StartMode = SequenceStartMode.Restart;
                    options.PickerNo = target.PickerNo;
                    options.RestrictToPickerNo = target.PickerNo;
                    int safeResult = await safe.RunAsync(runCts.Token, options).ConfigureAwait(true);
                    if (safeResult != 0)
                    {
                        lblStatus.Text = "Batch " + target.Label +
                                         " 완료 후 최종 안전 Avoid 복귀에 실패했습니다. 최종 안전 상태를 확인하세요." +
                                         " code=" + safeResult;
                        QMC.Common.MessageDialog.Show(
                            this,
                            lblStatus.Text,
                            "VISION FOCUS CAL",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }

                lblStatus.Text = "Batch Focus 측정을 모두 완료했습니다. 대상=" + targets.Count +
                                 ". SAVE RESULT는 마지막 정상 측정 대상을 확인 저장합니다.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Batch Focus 측정이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-BATCH-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Batch Focus 측정 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-BATCH", lblStatus.Text);
                QMC.Common.MessageDialog.Show(
                    this,
                    lblStatus.Text,
                    "VISION FOCUS CAL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _selectedKind = originalKind;
                _selectedPickerSide = originalSide;
                _selectedPickerNo = originalPickerNo;
                ReloadSelectedTargetReference();
                RefreshSettingGrid();
                RefreshSavedGrid();
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
                _lastSuccessfulResult = null;
                UpdateResultSaveButtonEnabled();

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

                CaptureLastSuccessfulResult(host.Machine, sequence.Result);
                host.SaveMachineSettings();
                lblStatus.Text = "Focus Scan 완료. SAVE RESULT로 마지막 정상 측정값을 확인 저장하세요. Best=" +
                                 sequence.Result.BestPosition.ToString("F3") +
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
                // 다이얼로그 수동 Side 스캔은 스캔 전에 Picker X/Y/Z/T를 DieSidePosition 기준으로 이동시킨다.
                PrepareSidePickerPosition = !IsBottomFocusKind(_selectedKind),
                UpdatedBy = UserSession.Name
            };
        }

        private void CaptureLastSuccessfulResult(
            CDT320_Machine machine,
            VisionFocusScanResult result)
        {
            if (machine == null || result == null || !result.Success)
                return;

            VisionFocusPositionRecord record = ResolveRecord(
                machine,
                _selectedKind,
                _selectedPickerSide,
                _selectedPickerNo);
            _lastSuccessfulResult = new FocusResultSnapshot
            {
                Kind = _selectedKind,
                Side = _selectedPickerSide,
                PickerNo = _selectedPickerNo,
                DefaultPosition = record != null ? record.DefaultPosition : _defaultPosition,
                BestPosition = result.BestPosition,
                BestScore = result.BestScore,
                SampleCount = result.SampleCount,
                PickerZPosition = record != null ? record.PickerZPosition : 0.0,
                PickerZValid = record != null && record.PickerZValid,
                UpdatedBy = UserSession.Name ?? string.Empty
            };
            UpdateResultSaveButtonEnabled();
        }

        private void SaveLastSuccessfulResult()
        {
            FocusResultSnapshot snapshot = _lastSuccessfulResult;
            if (snapshot == null)
            {
                lblStatus.Text = "저장할 Focus 측정 결과가 없습니다. START SCAN 또는 BATCH START를 정상 완료한 뒤 SAVE RESULT를 누르세요.";
                // [로그 보강 2026-07-27] 차단 사실을 이력에 남긴다(Collet BlockResultSave와 동일 기준).
                QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionFocusCalSaveResultBlocked", lblStatus.Text + " - Check");
                EventLogger.Write(EventKind.Warning, "CAL", "VISION-FOCUS-CAL-SAVE-RESULT-BLOCKED", lblStatus.Text);
                QMC.Common.MessageDialog.Show(
                    this,
                    lblStatus.Text,
                    "VISION FOCUS CAL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                    throw new InvalidOperationException(reason);

                VisionFocusPositionRecord record = ResolveRecord(
                    host.Machine,
                    snapshot.Kind,
                    snapshot.Side,
                    snapshot.PickerNo);
                if (record == null)
                    throw new InvalidOperationException("마지막 정상 측정 대상의 저장 레코드를 찾을 수 없습니다.");

                if (!DoesRecordMatchSnapshot(record, snapshot))
                {
                    _lastSuccessfulResult = null;
                    lblStatus.Text = "마지막 정상 측정 이후 해당 Focus 결과가 변경되어 SAVE RESULT를 차단했습니다. 다시 측정하세요. target=" +
                                     BuildTargetLabel(snapshot.Kind, snapshot.Side, snapshot.PickerNo);
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-SAVE-RESULT-STALE", lblStatus.Text);
                    QMC.Common.MessageDialog.Show(
                        this,
                        lblStatus.Text,
                        "VISION FOCUS CAL",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                host.SaveMachineSettings();
                RefreshSavedGrid();
                lblStatus.Text = "마지막 정상 Focus 결과를 저장했습니다. target=" +
                                 BuildTargetLabel(snapshot.Kind, snapshot.Side, snapshot.PickerNo) +
                                 ", best=" + snapshot.BestPosition.ToString("F6") +
                                 ", score=" + snapshot.BestScore.ToString("F6");
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-SAVE-RESULT", lblStatus.Text);
                _lastSuccessfulResult = null;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Focus 측정 결과 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-SAVE-RESULT", lblStatus.Text);
                QMC.Common.MessageDialog.Show(
                    this,
                    lblStatus.Text,
                    "VISION FOCUS CAL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                UpdateResultSaveButtonEnabled();
            }
        }

        private static bool DoesRecordMatchSnapshot(
            VisionFocusPositionRecord record,
            FocusResultSnapshot snapshot)
        {
            if (record == null || snapshot == null || !record.Valid)
                return false;
            if (record.SampleCount != snapshot.SampleCount ||
                record.PickerZValid != snapshot.PickerZValid)
                return false;
            if (!AreNearlyEqual(record.DefaultPosition, snapshot.DefaultPosition) ||
                !AreNearlyEqual(record.BestPosition, snapshot.BestPosition) ||
                !AreNearlyEqual(record.BestScore, snapshot.BestScore))
                return false;
            return !snapshot.PickerZValid ||
                   AreNearlyEqual(record.PickerZPosition, snapshot.PickerZPosition);
        }

        private static bool AreNearlyEqual(double left, double right)
        {
            double scale = Math.Max(1.0, Math.Max(Math.Abs(left), Math.Abs(right)));
            return Math.Abs(left - right) <= (1e-9 * scale);
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
                if (IsSideOnlyProfile)
                {
                    if (IsSideFocusKind(settings.CalibrationKind))
                        _selectedKind = settings.CalibrationKind;
                    _selectedPickerSide = settings.CalibrationPickerSide;
                    _selectedPickerNo = Clamp(settings.CalibrationPickerNo, 1, 4);
                }
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
                _autoFocusOnStartEnabled = settings.AutoFocusOnStartEnabled;
                _autoFocusOnWaferChange = settings.AutoFocusOnWaferChange;
                _autoFocusOnPickCountEnabled = settings.AutoFocusOnPickCountEnabled;
                _autoFocusPickInterval = settings.AutoFocusPickInterval;
                host.Machine.VisionUnit.Recipe.EnsurePositionObjects();
                _bottomVisionDelayMs = host.Machine.VisionUnit.Recipe.BottomVisionPreGrabDelayMs;
                _autoFocusToBottomInspectionDelayMs = host.Machine.VisionUnit.Recipe.RuntimeAutoFocusToBottomInspectionDelayMs;

                VisionFocusCalibrationData focusData = host.Machine.VisionUnit.Config.FocusCalibration;
                focusData.EnsureObjects();
                _useBottomToSideZOffset = focusData.UseBottomToSideZOffset;
                _bottomToSideZOffsetMm = focusData.BottomToSideZOffsetMm;
                _sideFocusSize90SignFront = focusData.SideFocusSize90SignFront;
                _sideFocusSize90SignRear = focusData.SideFocusSize90SignRear;
                _sideFocusCoc0SignFront = focusData.SideFocusCoc0SignFront;
                _sideFocusCoc0SignRear = focusData.SideFocusCoc0SignRear;
                _sideFocusCoc90SignFront = focusData.SideFocusCoc90SignFront;
                _sideFocusCoc90SignRear = focusData.SideFocusCoc90SignRear;
                _defaultPosition = ResolveSavedDefaultPosition(host.Machine);
                LoadSelectedPickerReference(host.Machine);
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

                if (_selectedKind == VisionFocusScanKind.BottomDie &&
                    _autoFocusOnPickCountEnabled &&
                    _autoFocusPickInterval <= 0)
                {
                    lblStatus.Text = "총 Pick 횟수 AutoFocus를 사용할 때 AF Total Pick Interval은 1 이상이어야 합니다.";
                    return false;
                }

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
                if (IsSideOnlyProfile)
                {
                    settings.CalibrationKind = _selectedKind;
                    settings.CalibrationPickerSide = _selectedPickerSide;
                    settings.CalibrationPickerNo = _selectedPickerNo;
                }
                settings.AutoFocusOnStartEnabled = _autoFocusOnStartEnabled;
                settings.AutoFocusOnWaferChange = _autoFocusOnWaferChange;
                settings.AutoFocusOnPickCountEnabled = _autoFocusOnPickCountEnabled;
                settings.AutoFocusPickInterval = _autoFocusPickInterval;
                host.Machine.VisionUnit.Recipe.EnsurePositionObjects();
                host.Machine.VisionUnit.Recipe.BottomVisionPreGrabDelayMs = _bottomVisionDelayMs;
                host.Machine.VisionUnit.Recipe.RuntimeAutoFocusToBottomInspectionDelayMs = _autoFocusToBottomInspectionDelayMs;
                host.Machine.VisionUnit.Recipe.RuntimeAutoFocusToBottomInspectionDelayInitialized = true;

                VisionFocusCalibrationData focusData = host.Machine.VisionUnit.Config.FocusCalibration;
                focusData.EnsureObjects();
                focusData.UseBottomToSideZOffset = _useBottomToSideZOffset;
                focusData.BottomToSideZOffsetMm = _bottomToSideZOffsetMm;
                focusData.SideFocusSize90SignFront = _sideFocusSize90SignFront;
                focusData.SideFocusSize90SignRear = _sideFocusSize90SignRear;
                focusData.SideFocusCoc0SignFront = _sideFocusCoc0SignFront;
                focusData.SideFocusCoc0SignRear = _sideFocusCoc0SignRear;
                focusData.SideFocusCoc90SignFront = _sideFocusCoc90SignFront;
                focusData.SideFocusCoc90SignRear = _sideFocusCoc90SignRear;

                VisionFocusPositionRecord record = ResolveSelectedRecord(host.Machine);
                if (record != null)
                {
                    record.DefaultPosition = _defaultPosition;
                    record.UpdatedAt = DateTime.Now;
                    record.UpdatedBy = UserSession.Name ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(host.ActiveRecipeName) || !host.SaveMachineRecipe(host.ActiveRecipeName))
                {
                    lblStatus.Text = "Bottom Vision/AF To Bottom Delay Recipe 저장에 실패했습니다. 활성 Recipe를 확인하세요.";
                    return false;
                }
                host.SaveMachineSettings();
                RefreshSavedGrid();
                if (showMessage)
                    lblStatus.Text = "Vision Focus Cal 설정값과 Bottom Vision/AF To Bottom Delay Recipe 값을 저장했습니다. Teaching Z는 변경하지 않았습니다.";
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

                double bestPosition = record.BestPosition;
                double oldDefaultPosition = record.DefaultPosition;
                string message = "Best Focus를 Focus Cal 기준값으로 저장하시겠습니까?" + Environment.NewLine +
                                 "실제 Recipe/Teaching Z축 위치는 변경하지 않습니다." + Environment.NewLine +
                                 "Current Default: " + oldDefaultPosition.ToString("F3") + Environment.NewLine +
                                 "Best           : " + bestPosition.ToString("F3");

                DialogResult answer = QMC.Common.MessageDialog.Show(
                    this,
                    message,
                    "VISION FOCUS CAL",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return;

                record.DefaultPosition = bestPosition;
                record.UpdatedAt = DateTime.Now;
                record.UpdatedBy = UserSession.Name ?? string.Empty;
                _defaultPosition = bestPosition;
                host.SaveMachineSettings();
                RefreshSettingGrid();
                RefreshSavedGrid();

                lblStatus.Text = "Best Focus 기준값 저장 완료. Default=" + bestPosition.ToString("F3") +
                                 ", Recipe/Teaching Z 변경 없음";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-APPLY-BEST",
                    lblStatus.Text + ", oldDefault=" + oldDefaultPosition.ToString("F3") +
                    ", score=" + record.BestScore.ToString("F4") +
                    ", recipeTeachingZChanged=False");
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

                AddSettingRow(CreateOptionInfo(FocusSettingKey.Mode, "Mode", "Focus Scan 대상 모드입니다.", ResolveModeOptions()), KindToText(_selectedKind), true);
                bool bottomFocus = IsBottomFocusKind(_selectedKind);
                bool runtimeBottomFocus = _selectedKind == VisionFocusScanKind.BottomDie;
                bool pickerSelectionEnabled = bottomFocus || IsSideOnlyProfile;
                string pickerNoName = _selectedKind == VisionFocusScanKind.BottomDie ? "Picker No" : "Collet No";
                if (IsSideOnlyProfile)
                    pickerNoName = "Picker No";
                AddSettingRow(CreateOptionInfo(FocusSettingKey.PickerSide, "Picker Side", "Focus 기준 제품을 보유한 Front/Rear Picker를 선택합니다.", SideOptions), SideToText(_selectedPickerSide), pickerSelectionEnabled);
                AddSettingRow(CreateOptionInfo(FocusSettingKey.ColletNo, pickerNoName, "Focus 기준으로 사용할 Picker 번호를 선택합니다.", ColletOptions), _selectedPickerNo.ToString(CultureInfo.InvariantCulture), pickerSelectionEnabled);
                if (IsSideOnlyProfile)
                {
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.PickerReferenceX, "Picker X Ref (mm)", "mm", "자동 통합 Side 검사와 동일한 Picker 번호의 DieSidePosition 기준 X입니다. " + _pickerReferenceFormula, false), FormatDouble(_pickerReferenceX), false);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.PickerReferenceY, "Picker Y Ref (mm)", "mm", "자동 통합 Side 검사와 동일한 Picker 번호의 DieSidePosition 기준 Y입니다. " + _pickerReferenceFormula, false), FormatDouble(_pickerReferenceY), false);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.PickerReferenceZ, "Picker Z Ref (mm)", "mm", "선택 PickerZ의 SidePosition 기준 Z이며 Focus 스캔 중에는 고정합니다.", false), FormatDouble(_pickerReferenceZ), false);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.PickerReferenceT, "Picker T Ref (deg)", "deg", "생산 Side 검사와 동일한 DieSidePosition 기준 T입니다.", false), FormatDouble(_pickerReferenceT), false);
                }
                string defaultPositionName = IsSideOnlyProfile ? "Side Camera Y Default" : "Default Pos (mm)";
                 string defaultPositionTip = IsSideOnlyProfile
                     ? "Side Camera Y Focus 기준입니다. 저장값이 없으면 선택 카메라/각도의 Process0/90 티칭값을 사용하며 USE CURRENT로 현재 Camera Y를 적용할 수 있습니다."
                     : "Focus 기준 위치입니다. 저장된 Focus Cal 등록값만 불러오며, USE CURRENT로 현재 축 위치를 덮어쓸 수 있습니다.";
                 AddSettingRow(CreateNumberInfo(FocusSettingKey.DefaultPosition, defaultPositionName, "mm", defaultPositionTip, false), FormatDouble(_defaultPosition), true);
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
                AddSettingRow(CreateOptionInfo(FocusSettingKey.AutoFocusOnStart, "AF On Start", "장비를 정지한 뒤 START 버튼으로 다시 시작할 때 AutoFocus 선택창을 표시합니다. 다른 조건과 동시에 사용할 수 있습니다.", BoolOptions), _autoFocusOnStartEnabled ? "True" : "False", runtimeBottomFocus);
                AddSettingRow(CreateOptionInfo(FocusSettingKey.AutoFocusOnWaferChange, "AF On Wafer Change", "새 Input Wafer의 Die가 처음 Bottom 촬영에 진입할 때 Rough+Fine AutoFocus를 실행합니다. 다른 조건과 동시에 사용할 수 있습니다.", BoolOptions), _autoFocusOnWaferChange ? "True" : "False", runtimeBottomFocus);
                AddSettingRow(CreateOptionInfo(FocusSettingKey.AutoFocusOnPickCount, "AF By Total Pick Count", "Front/Rear 전체 Pick 완료 Die 누적 수가 설정 횟수에 도달하면 Rough+Fine AutoFocus를 실행합니다. 다른 조건과 동시에 사용할 수 있습니다.", BoolOptions), _autoFocusOnPickCountEnabled ? "True" : "False", runtimeBottomFocus);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.AutoFocusPickInterval, "AF Total Pick Interval (ea)", "ea", "Front/Rear 전체에서 Pick 완료한 총 Die 수 기준 AutoFocus 실행 간격입니다.", true), _autoFocusPickInterval.ToString(CultureInfo.InvariantCulture), runtimeBottomFocus);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.BottomVisionDelay, "Bottom Vision Delay (ms)", "ms", "생산 Bottom Vision 검사에서 INSPECTASYNC/Grab 명령을 보내기 전에 대기할 시간입니다. Front/Rear 공통 Recipe 값입니다.", true), _bottomVisionDelayMs.ToString(CultureInfo.InvariantCulture), runtimeBottomFocus);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.AutoFocusToBottomInspectionDelay, "AF To Bottom Delay (ms)", "ms", "런타임 AutoFocus 후 Bottom Grab 명령의 ACK를 받은 다음, 다음 모션을 시작하기 전에 대기할 시간입니다.", true), _autoFocusToBottomInspectionDelayMs.ToString(CultureInfo.InvariantCulture), runtimeBottomFocus);
                if (IsSideOnlyProfile)
                {
                    AddSettingRow(CreateOptionInfo(FocusSettingKey.UseBottomToSideZOffset, "B->S Z Offset Use", "사용 시 Side 촬영 PickerZ를 SidePosition 티칭 대신 '콜렛별 Bottom AF Best Z + Z Offset'으로 계산합니다(Front/Rear 공용).", BoolOptions), _useBottomToSideZOffset ? "True" : "False", true);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.BottomToSideZOffset, "B->S Z Offset (mm)", "mm", "Bottom 카메라 초점 Z와 Side 카메라 광축 사이의 기계적 Z 옵셋입니다(Front/Rear 공용).", false), FormatDouble(_bottomToSideZOffsetMm), true);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.SideFocusSize90SignFront, "Sign Size90 Front", "", "90도 다이사이즈 항((가로-세로)/2) 부호(Front). -1/0/+1. 0은 항 비활성. 실장비 테스트로 확정합니다.", false), FormatDouble(_sideFocusSize90SignFront), true);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.SideFocusSize90SignRear, "Sign Size90 Rear", "", "90도 다이사이즈 항((가로-세로)/2) 부호(Rear). -1/0/+1.", false), FormatDouble(_sideFocusSize90SignRear), true);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.SideFocusCoc0SignFront, "Sign COC0 Front", "", "0도 COC(Y성분) 항 부호(Front). -1/0/+1.", false), FormatDouble(_sideFocusCoc0SignFront), true);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.SideFocusCoc0SignRear, "Sign COC0 Rear", "", "0도 COC(Y성분) 항 부호(Rear). -1/0/+1.", false), FormatDouble(_sideFocusCoc0SignRear), true);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.SideFocusCoc90SignFront, "Sign COC90 Front", "", "90도 COC(X성분, 회전 후) 항 부호(Front). -1/0/+1.", false), FormatDouble(_sideFocusCoc90SignFront), true);
                    AddSettingRow(CreateNumberInfo(FocusSettingKey.SideFocusCoc90SignRear, "Sign COC90 Rear", "", "90도 COC(X성분, 회전 후) 항 부호(Rear). -1/0/+1.", false), FormatDouble(_sideFocusCoc90SignRear), true);
                }

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
                    if (_selectedKind == VisionFocusScanKind.FrontSide0 || _selectedKind == VisionFocusScanKind.FrontSide90)
                        _selectedPickerSide = VisionFocusPickerSide.Front;
                    else if (_selectedKind == VisionFocusScanKind.RearSide0 || _selectedKind == VisionFocusScanKind.RearSide90)
                        _selectedPickerSide = VisionFocusPickerSide.Rear;
                    break;
                case FocusSettingKey.PickerSide:
                    _selectedPickerSide = value == "Rear" ? VisionFocusPickerSide.Rear : VisionFocusPickerSide.Front;
                    if (IsSideOnlyProfile && IsSideFocusKind(_selectedKind))
                    {
                        bool angle90 = _selectedKind == VisionFocusScanKind.FrontSide90 ||
                                       _selectedKind == VisionFocusScanKind.RearSide90;
                        _selectedKind = _selectedPickerSide == VisionFocusPickerSide.Front
                            ? (angle90 ? VisionFocusScanKind.FrontSide90 : VisionFocusScanKind.FrontSide0)
                            : (angle90 ? VisionFocusScanKind.RearSide90 : VisionFocusScanKind.RearSide0);
                    }
                    break;
                case FocusSettingKey.ColletNo:
                    _selectedPickerNo = Clamp(ParseInt(value, _selectedPickerNo), 1, 4);
                    break;
                case FocusSettingKey.ReturnDefault:
                    _returnToDefaultAfterScan = value == "True";
                    break;
                case FocusSettingKey.AutoFocusOnStart:
                    _autoFocusOnStartEnabled = value == "True";
                    break;
                case FocusSettingKey.AutoFocusOnWaferChange:
                    _autoFocusOnWaferChange = value == "True";
                    break;
                case FocusSettingKey.AutoFocusOnPickCount:
                    _autoFocusOnPickCountEnabled = value == "True";
                    break;
                case FocusSettingKey.FocusValueMode:
                    _focusValueReceiveMode = TextToFocusValueMode(value);
                    break;
                case FocusSettingKey.UseBottomToSideZOffset:
                    _useBottomToSideZOffset = value == "True";
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
                case FocusSettingKey.AutoFocusPickInterval:
                    _autoFocusPickInterval = Clamp(value, 0, 1000000);
                    break;
                case FocusSettingKey.BottomVisionDelay:
                    _bottomVisionDelayMs = Clamp(value, 0, 60000);
                    break;
                case FocusSettingKey.AutoFocusToBottomInspectionDelay:
                    _autoFocusToBottomInspectionDelayMs = Clamp(value, 0, 60000);
                    break;
            }
        }

        private void ResetSelectedAutoFocusCount()
        {
            try
            {
                if (_busy)
                    return;

                if (!ApplyAllSettingRowsFromGrid())
                    return;

                if (_selectedKind != VisionFocusScanKind.BottomDie)
                {
                    lblStatus.Text = "AutoFocus Count Reset은 생산 Runtime Focus 기준인 Bottom Die 모드에서만 사용할 수 있습니다.";
                    return;
                }

                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                VisionFocusCalibrationData data = host.Machine.VisionUnit.Config.FocusCalibration;
                data.EnsureObjects();
                data.ResetRuntimeAutoFocusTracking();
                host.SaveMachineSettings();
                RefreshSavedGrid();

                lblStatus.Text = "생산 Bottom Die AutoFocus 전체 Pick 누적 수와 마지막 Wafer 기준을 초기화했습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-AUTO-RESET",
                    lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "AutoFocus Count Reset 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-AUTO-RESET-EX", lblStatus.Text);
            }
            finally
            {
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
                case FocusSettingKey.BottomToSideZOffset:
                    _bottomToSideZOffsetMm = Clamp(value, -100.0, 100.0);
                    break;
                case FocusSettingKey.SideFocusSize90SignFront:
                    _sideFocusSize90SignFront = Clamp(value, -1.0, 1.0);
                    break;
                case FocusSettingKey.SideFocusSize90SignRear:
                    _sideFocusSize90SignRear = Clamp(value, -1.0, 1.0);
                    break;
                case FocusSettingKey.SideFocusCoc0SignFront:
                    _sideFocusCoc0SignFront = Clamp(value, -1.0, 1.0);
                    break;
                case FocusSettingKey.SideFocusCoc0SignRear:
                    _sideFocusCoc0SignRear = Clamp(value, -1.0, 1.0);
                    break;
                case FocusSettingKey.SideFocusCoc90SignFront:
                    _sideFocusCoc90SignFront = Clamp(value, -1.0, 1.0);
                    break;
                case FocusSettingKey.SideFocusCoc90SignRear:
                    _sideFocusCoc90SignRear = Clamp(value, -1.0, 1.0);
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
                    bool runtimeBottomDie = _selectedKind == VisionFocusScanKind.BottomDie;
                    colAutoFocusCount.HeaderText = runtimeBottomDie ? "TOTAL PICK" : "AF CNT";
                    colAutoFocusWafer.HeaderText = runtimeBottomDie ? "LAST AF WAFER" : "AF WAFER";
                    for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                        AddSavedRow(
                            SideToText(_selectedPickerSide) + " " + ResolveBottomTargetText(_selectedKind) + " #" + pickerNo,
                            data.GetBottomRecord(_selectedKind, _selectedPickerSide, pickerNo),
                            runtimeBottomDie ? data.RuntimeAutoFocusTotalPickCount.ToString(CultureInfo.InvariantCulture) : null,
                            runtimeBottomDie ? data.RuntimeAutoFocusLastWaferId : null);
                    return;
                }

                colAutoFocusCount.HeaderText = "AF CNT";
                colAutoFocusWafer.HeaderText = "AF WAFER";
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                    AddSavedRow(KindToText(_selectedKind) + " C" + pickerNo,
                        data.GetSideRecord(_selectedKind, pickerNo));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void AddSavedRow(
            string name,
            VisionFocusPositionRecord record,
            string autoFocusCount = null,
            string autoFocusWafer = null)
        {
            if (record == null)
                return;

            int rowIndex = gridSaved.Rows.Add(
                name,
                record.DefaultPosition.ToString("F3"),
                record.BestPosition.ToString("F3"),
                record.BestScore.ToString("F4"),
                record.PickerZValid ? record.PickerZPosition.ToString("F3") : "-",
                autoFocusCount ?? record.AutoFocusPickCountSinceLast.ToString(CultureInfo.InvariantCulture),
                autoFocusWafer ?? record.LastAutoFocusWaferId ?? string.Empty,
                record.Valid ? "Y" : "N");
            DataGridViewRow row = gridSaved.Rows[rowIndex];
            row.Tag = record;
            if (IsSideOnlyProfile)
                row.Cells[colBestPos.Index].ToolTipText = "더블클릭하여 BEST 위치를 수동 입력합니다.";
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
            return ResolveRecord(machine, _selectedKind, _selectedPickerSide, _selectedPickerNo);
        }

        private static VisionFocusPositionRecord ResolveRecord(
            CDT320_Machine machine,
            VisionFocusScanKind kind,
            VisionFocusPickerSide side,
            int pickerNo)
        {
            if (machine == null || machine.VisionUnit == null || machine.VisionUnit.Config == null)
                return null;

            machine.VisionUnit.Config.EnsureCalibrationObjects();
            VisionFocusCalibrationData data = machine.VisionUnit.Config.FocusCalibration;
            data.EnsureObjects();
            return kind == VisionFocusScanKind.BottomCollet || kind == VisionFocusScanKind.BottomDie
                ? data.GetBottomRecord(kind, side, pickerNo)
                : data.GetSideRecord(kind, pickerNo);
        }

        private static string BuildTargetLabel(
            VisionFocusScanKind kind,
            VisionFocusPickerSide side,
            int pickerNo)
        {
            return kind + ":" + (side == VisionFocusPickerSide.Front ? "F" : "R") + " P" + pickerNo;
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
            if (HasSavedDefaultPosition(record))
                return record.DefaultPosition;

            if (IsSideOnlyProfile)
            {
                string targetName;
                return ResolveInspectionTeachingPosition(machine, out targetName);
            }

            return 0.0;
        }

        private void ReloadSelectedTargetReference()
        {
            string reason;
            Form1 host = ResolveHost(out reason);
            if (host == null || host.Machine == null)
            {
                lblStatus.Text = reason;
                return;
            }

            _defaultPosition = ResolveSavedDefaultPosition(host.Machine);
            LoadSelectedPickerReference(host.Machine);
        }

        private void LoadSelectedPickerReference(CDT320_Machine machine)
        {
            _pickerReferenceX = 0.0;
            _pickerReferenceY = 0.0;
            _pickerReferenceZ = 0.0;
            _pickerReferenceT = 0.0;
            _pickerReferenceFormula = string.Empty;

            if (!IsSideOnlyProfile || machine == null)
                return;

            int pickerIndex = Clamp(_selectedPickerNo, 1, 4) - 1;
            PickerSideFocusReferenceTarget target = CalibrationCoordinateService.ResolveSideFocusReferenceTarget(
                machine,
                _selectedPickerSide,
                pickerIndex);
            if (target == null)
                return;

            _pickerReferenceX = target.X;
            _pickerReferenceY = target.Y;
            _pickerReferenceZ = target.Z;
            _pickerReferenceT = target.T;
            _pickerReferenceFormula = target.Formula ?? string.Empty;
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
            QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionFocusCalTeachingApplyBlocked",
                "Vision Focus Cal에서 실제 Recipe/Teaching Z 적용은 차단됨. kind=" + _selectedKind +
                ", side=" + _selectedPickerSide +
                ", pickerNo=" + _selectedPickerNo +
                ", requestedPosition=" + position.ToString("F3"));
        }

        private async Task<int> MoveSelectedPickerZToAvoidAsync(CDT320_Machine machine)
        {
            if (machine == null)
                return -1;

            if (_selectedPickerSide == VisionFocusPickerSide.Front)
            {
                if (machine.PickerFrontUnit == null)
                    return -1;
                return await machine.PickerFrontUnit.MovePickerZToSafeHeight(_selectedPickerNo).ConfigureAwait(true);
            }

            if (machine.PickerRearUnit == null)
                return -1;
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
                return await machine.PickerFrontUnit.MovePickerAxisToTeachingPosition(PickerAxis.PickerY, "AvoidPosition").ConfigureAwait(true);
            }

            if (machine.PickerRearUnit == null)
                return -1;
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
            batchGroup.Enabled = enabled;
            btnCheck.Enabled = enabled;
            btnUseCurrent.Enabled = enabled;
            btnMoveDefault.Enabled = enabled;
            btnMoveZAvoid.Enabled = enabled;
            btnMoveYAvoid.Enabled = enabled;
            btnStartScan.Enabled = enabled;
            btnSeqStop.Enabled = _activeStopRequest != null;
            btnApplyBest.Enabled = enabled;
            btnResetAutoFocus.Enabled = enabled;
            btnReload.Enabled = enabled;
            btnSaveParameters.Enabled = enabled;
            btnSave.Enabled = enabled && _lastSuccessfulResult != null;
            btnClose.Enabled = enabled;
        }

        private void UpdateResultSaveButtonEnabled()
        {
            if (btnSave != null)
                btnSave.Enabled = !_busy && _lastSuccessfulResult != null;
        }

        private void UpdateStopButtonEnabled()
        {
            if (btnSeqStop != null)
                btnSeqStop.Enabled = _activeStopRequest != null;
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

        private static bool IsSideFocusKind(VisionFocusScanKind kind)
        {
            return kind == VisionFocusScanKind.FrontSide0 ||
                   kind == VisionFocusScanKind.FrontSide90 ||
                   kind == VisionFocusScanKind.RearSide0 ||
                   kind == VisionFocusScanKind.RearSide90;
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
