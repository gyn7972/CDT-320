using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;
using QMC.CDT320.Sequencing.Calibration;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class ColletCalibrationDialog : Form
    {
        private enum SettingKey
        {
            Side,
            ColletNo,
            Finder,
            ThetaTolerance,
            MaxThetaIteration,
            ThetaGain,
            XyTolerance,
            MaxXyIteration,
            XyGainX,
            XyGainY,
            XyFineMax,
            XyToleranceMode,
            ScoreThreshold,
            VisionTimeout,
            SideAutoFocus,
            MoveVelocity,
            MoveAcceleration,
            MoveDeceleration,
            MoveTimeout,
            CocRotationVelocity,
            AutoFocus,
            ColletDieCalThickness,
            ColletFilmThickness,
            ColletFlatZOffset,
            ColletRimOffsetFromFlat,
        }

        private sealed class SettingInfo
        {
            public SettingKey Key;
            public string Name;
            public string Unit;
            public string ToolTip;
            public bool Numeric;
            public bool Integer;
            public string[] Options;
            public bool ReadOnly;
        }

        private sealed class ManualMoveResult
        {
            public int Result;
            public double Target;
        }

        private static readonly string[] SideOptions = { "Front", "Rear" };
        private static readonly string[] ColletOptions = { "4", "3", "2", "1" };
        private static readonly string[] BoolOptions = { "True", "False" };
        private static readonly string[] XyToleranceModeOptions = { "Diagonal", "Axis" };

        private bool _loading;
        private bool _busy;
        private VisionFocusPickerSide _side = VisionFocusPickerSide.Front;
        private int _colletNo = 4;
        private string _finder = ColletCalibrationSettings.DefaultBottomFinderName;
        private double _thetaToleranceDeg = 0.02;
        private int _maxThetaIterations = 5;
        private double _thetaGain = 1.0;
        private double _xyToleranceMm = 0.001;
        private int _maxXyIterations = 5;
        private double _xyGainX = 1.0;
        private double _xyGainY = 1.0;
        private double _xyFineMaxMm = 0.2;
        private bool _useDiagonalXyTolerance = true;
        private double _scoreThreshold = 0.0;
        private int _visionTimeoutMs = 5000;
        private double _moveVelocity = CalibrationMotionSettings.DefaultMoveVelocity;
        private double _moveAcceleration = CalibrationMotionSettings.DefaultMoveAcceleration;
        private double _moveDeceleration = CalibrationMotionSettings.DefaultMoveDeceleration;
        private int _moveTimeoutMs = CalibrationMotionSettings.DefaultMoveTimeoutMs;
        private double _cocRotationVelocityDegPerSec = 30.0;
        private bool _autoFocus = true;
        private bool _sideAutoFocus = true;
        private ColletShapeType _colletType = ColletShapeType.Flat;
        private double _colletDieCalThicknessMm;
        private double _colletFilmThicknessMm;
        private double _colletFlatZOffsetMm;
        private double _colletRimOffsetFromFlatMm;
        private CancellationTokenSource _runCts;
        private Action _activeStopRequest;
        private ColletCalibrationRecord _lastSuccessfulResultRecord;
        private VisionFocusPickerSide _lastSuccessfulResultSide;
        private int _lastSuccessfulResultColletNo;
        private DateTime _lastSuccessfulResultUpdatedAt;

        public static ColletCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "ColletCalibrationDialog",
                owner,
                () => new ColletCalibrationDialog());
        }

        public ColletCalibrationDialog()
        {
            try
            {
                InitializeComponent();
                ApplyButtonStyle();
                CalibrationDialogGridBehavior.Apply(gridSettings, gridResults);
                ConfigureEditableSettingGrid();
                ConfigureSaveHistoryList();
                LoadSettingsToUi();
                RefreshResultGrid();
                UpdateStopButtonEnabled();
                lblStatus.Text = "대기 중입니다. Collet과 보정 조건을 확인한 뒤 START를 실행하세요.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-DIALOG-INIT",
                    "Collet Calibration 창 초기화 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void ApplyButtonStyle()
        {
            CalibrationDialogButtonStyle.ApplyFooterButtons(
                new[] { btnCheck, btnSaveBottomTeaching, btnApplyHomeOffset, btnMoveZForward, btnMoveYAvoid, btnSeqStop, btnReload, btnClose },
                new[] { btnStart, btnCoc, btnCocCenter },
                new[] { btnParameterSave, btnSave });
        }

        private void ConfigureEditableSettingGrid()
        {
            gridSettings.ReadOnly = false;
            gridSettings.EditMode = DataGridViewEditMode.EditOnEnter;
            colSettingName.ReadOnly = true;
            colSettingValue.ReadOnly = false;
            colSettingUnit.ReadOnly = true;
            gridSettings.CellClick += gridSettings_CellClick;
        }

        private void ConfigureSaveHistoryList()
        {
            if (lstSaveHistory == null)
                return;

            lstSaveHistory.ForeColor = System.Drawing.Color.Black;
            lstSaveHistory.BackColor = System.Drawing.Color.White;
            lstSaveHistory.SelectionMode = SelectionMode.None;
            lstSaveHistory.HorizontalScrollbar = true;
            lstSaveHistory.IntegralHeight = false;
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
            SettingInfo info = row != null ? row.Tag as SettingInfo : null;
            if (info != null && (info.Numeric || info.ReadOnly))
                e.Cancel = true;
        }

        private void gridSettings_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_loading || e.RowIndex < 0 || e.ColumnIndex != colSettingValue.Index)
                return;

            try
            {
                DataGridViewRow row = gridSettings.Rows[e.RowIndex];
                SettingInfo info = row.Tag as SettingInfo;
                if (info == null || info.Numeric || info.ReadOnly)
                    return;

                ApplySettingValue(row);
                RefreshSettingGrid();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "설정 변경 실패: " + ex.Message;
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
            SettingInfo info = row.Tag as SettingInfo;
            if (info == null)
                return;

            if (info.ReadOnly)
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

                    double numericValue;
                    if (!TryParseSettingNumber(dialog.ValueText, out numericValue))
                    {
                        lblStatus.Text = info.Name + " 설정값이 숫자가 아닙니다. value=" + dialog.ValueText;
                        return;
                    }

                    ApplyNumericSetting(info, numericValue);
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

            DataGridViewRow row = gridSettings.Rows[e.RowIndex];
            SettingInfo info = row != null ? row.Tag as SettingInfo : null;
            if (info != null)
                e.ToolTipText = info.ToolTip;
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            string reason;
            if (!CanRunManualCalibration(out reason))
            {
                lblStatus.Text = reason;
                QMC.Common.MessageDialog.Show(this, reason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblStatus.Text = "실행 가능한 상태입니다.";
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            await RunCalibrationAsync().ConfigureAwait(true);
        }

        private async void btnCoc_Click(object sender, EventArgs e)
        {
            await RunCocCalibrationAsync(false).ConfigureAwait(true);
        }

        private async void btnCocCenter_Click(object sender, EventArgs e)
        {
            await RunCocCalibrationAsync(true).ConfigureAwait(true);
        }

        private async void btnSaveBottomTeaching_Click(object sender, EventArgs e)
        {
            await SaveCurrentBottomTeachingPositionAsync().ConfigureAwait(true);
        }

        private async void btnApplyHomeOffset_Click(object sender, EventArgs e)
        {
            await ApplySelectedTHomeOffsetAsync().ConfigureAwait(true);
        }

        private async void btnMoveZForward_Click(object sender, EventArgs e)
        {
            await RunMoveZForwardAsync().ConfigureAwait(true);
        }

        private async void btnMoveYAvoid_Click(object sender, EventArgs e)
        {
            await RunMoveYAvoidAsync().ConfigureAwait(true);
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            ClearLastSuccessfulResult();
            LoadSettingsToUi();
            RefreshResultGrid();
            lblStatus.Text = "Collet Calibration 설정과 저장값을 다시 불러왔습니다.";
        }

        private void btnParameterSave_Click(object sender, EventArgs e)
        {
            SaveParameterSettingsFromUi(true);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveLastSuccessfulResult(true);
        }

        private void btnSeqStop_Click(object sender, EventArgs e)
        {
            RequestActiveSequenceStop("SEQ STOP 버튼");
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        // 실행 중 X/Alt+F4로 창이 닫혀 시퀀스가 화면 없이 계속 도는 것을 막는다.
        // 단 사용자가 직접 닫을 때(UserClosing)만 붙잡는다 — 앱/Windows/소유자(Form1) 종료 경로에서
        // e.Cancel을 세우면 Form1 종료가 취소되어 프로그램을 끌 수 없게 된다(소유 폼에는 FormOwnerClosing으로 전달됨).
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_busy)
                {
                    if (e.CloseReason != CloseReason.UserClosing)
                    {
                        RequestRunCancelForClose("Collet Calibration 창 종료(" + e.CloseReason + ")");
                        base.OnFormClosing(e);
                        return;
                    }

                    DialogResult result = QMC.Common.MessageDialog.Show(this,
                        "Collet Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?",
                        "COLLET CAL",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        RequestRunCancelForClose("Collet Calibration 창 닫기");
                        lblStatus.Text = "정지 처리 중입니다. 완료 후 창을 닫으세요.";
                    }

                    e.Cancel = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                // 종료 경로에서는 예외가 나도 창을 붙잡지 않는다(프로그램 종료 차단 방지).
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
                EventLogger.Write(EventKind.Alarm, "UI", "COLLET-CAL-CLOSE",
                    "Collet Calibration 창 종료 확인 중 예외가 발생했습니다. error=" + ex.Message);
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
                EventLogger.Write(EventKind.Alarm, "UI", "COLLET-CAL-CLOSE-CANCEL",
                    reason + " 중 정지 요청 실패: " + ex.Message);
            }
        }

        private async Task RunCalibrationAsync()
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
                    QMC.Common.MessageDialog.Show(this, reason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                if (!SaveParameterSettingsFromUi(false))
                    return;

                ClearLastSuccessfulResult();

                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.ProcessSequence,
                    "ColletCalibration:" + _side + ":" + _colletNo);
                runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
                _runCts = runCts;
                stopHandler = CreateStopRequestAction("ColletCalibration");
                _activeStopRequest = stopHandler;
                UpdateStopButtonEnabled();
                host.Controller.StopRequested += stopHandler;

                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new ColletCalibrationSequence(context, _side, _colletNo);
                // 선택한 Picker를 Cal할 때 상대 Picker(X/Y/Z/T)를 Avoid로 자동 이동한다.
                // 이미 Avoid면 시퀀스가 재이동 없이 통과하므로 Auto Cal 안전위치 이동과 중복되지 않는다.
                PickerSequenceOptions options = PickerSequenceOptions.Default();
                options.RunMode = SequenceRunMode.Manual;
                options.StartMode = SequenceStartMode.Restart;
                options.PickerNo = _colletNo;
                options.RestrictToPickerNo = _colletNo;

                lblStatus.Text = "Collet Calibration 실행 중입니다. Side=" + _side + ", Collet=" + _colletNo;
                int result = await sequence.RunAsync(runCts.Token, options).ConfigureAwait(true);
                RefreshResultGrid();

                if (result != 0)
                {
                    lblStatus.Text = "Collet Calibration 실패. Alarm/Event Log를 확인하세요.";
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                host.SaveMachineSettings();
                ColletCalibrationRecord record = sequence.ResultRecord;
                RememberSuccessfulResult(_side, _colletNo, record);
                lblStatus.Text = "Collet Calibration 완료. OffsetX=" +
                                 (record != null ? record.OffsetX.ToString("F6") : "-") +
                                 ", OffsetY=" + (record != null ? record.OffsetY.ToString("F6") : "-") +
                                 ", TZero=" + (record != null ? record.TZeroHomeOffset.ToString("F6") : "-");
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Collet Calibration 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-STOP", lblStatus.Text);
            }
            catch (SequenceStopException ex)
            {
                lblStatus.Text = "Collet Calibration 정지: " + ex.Message;
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Collet Calibration 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-RUN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (host != null && stopHandler != null)
                    host.Controller.StopRequested -= stopHandler;

                if (ReferenceEquals(_runCts, runCts))
                    _runCts = null;
                if (ReferenceEquals(_activeStopRequest, stopHandler))
                    _activeStopRequest = null;

                if (runCts != null)
                    runCts.Dispose();

                if (actionScope != null)
                    actionScope.Dispose();

                _busy = false;
                SetButtonsEnabled(true);
                UpdateStopButtonEnabled();
            }
        }

        private async Task RunCocCalibrationAsync(bool moveToStoredCenter)
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
                    QMC.Common.MessageDialog.Show(this, reason, "COLLET COC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                if (!SaveParameterSettingsFromUi(false))
                    return;

                if (moveToStoredCenter)
                {
                    ColletCalibrationRecord record = ResolveData(host.Machine).GetRecord(_side, _colletNo);
                    if (record == null || !record.RotationCenterValid)
                    {
                        lblStatus.Text = "COC START로 선택 Collet의 1차 회전 중심을 먼저 검출하세요.";
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET COC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                    {
                        lblStatus.Text = "현재 활성 Recipe가 없어 회전 중심 기계 좌표를 저장할 수 없습니다.";
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET COC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                runCts = BeginManualCalibrationRun(host, "ColletCOC", out actionScope, out stopHandler);
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new ColletRotationCenterCalibrationSequence(context, _side, _colletNo, moveToStoredCenter);
                PickerSequenceOptions options = PickerSequenceOptions.Default();
                options.RunMode = SequenceRunMode.Manual;
                options.StartMode = SequenceStartMode.Restart;
                options.PickerNo = _colletNo;
                options.RestrictToPickerNo = _colletNo;

                lblStatus.Text = moveToStoredCenter
                    ? "저장된 회전 중심으로 X/Y 이동 후 COC를 다시 실행하고 있습니다."
                    : "COC 실행 중입니다. 기존 Collet Calibration 위치에서 T축을 " +
                      _cocRotationVelocityDegPerSec.ToString("F3") + " deg/s로 360도 회전합니다.";
                int result = await sequence.RunAsync(runCts.Token, options).ConfigureAwait(true);
                RefreshResultGrid();
                if (result != 0)
                {
                    lblStatus.Text = "COC 실패. Alarm/Event Log를 확인하세요.";
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET COC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string recipeSummary = string.Empty;
                if (moveToStoredCenter)
                {
                    if (!SaveRotationCenterToRecipe(host, sequence.RotationCenterMachineX, sequence.RotationCenterMachineY, out recipeSummary))
                    {
                        lblStatus.Text = recipeSummary;
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET COC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                host.SaveMachineSettings();
                string summary = (moveToStoredCenter ? "COC 중심 이동/재검출 완료. " : "COC 완료. ") + "Side=" + _side +
                                  ", Collet=" + _colletNo +
                                  ", CenterPixel=(" + sequence.Result.CenterPixelX.ToString("F3") +
                                  ", " + sequence.Result.CenterPixelY.ToString("F3") + ")" +
                                  ", Frames=" + sequence.Result.FrameCount +
                                  (moveToStoredCenter
                                      ? ", MachineCenter=(" + sequence.RotationCenterMachineX.ToString("F6") +
                                        ", " + sequence.RotationCenterMachineY.ToString("F6") + ")"
                                      : string.Empty);
                lblStatus.Text = summary;
                string[] history = string.IsNullOrWhiteSpace(recipeSummary)
                    ? new[] { summary }
                    : new[] { summary, recipeSummary };
                AppendSaveHistory(history);
                WriteSaveHistoryLog(history);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "COC가 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-COC-STOP", lblStatus.Text);
            }
            catch (SequenceStopException ex)
            {
                lblStatus.Text = "COC 정지: " + ex.Message;
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-COC-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "COC 실행 중 예외가 발생했습니다: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-COC-RUN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET COC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
                UpdateStopButtonEnabled();
            }
        }

        // ── BATCH: 선택 콜렛 일괄 캘리브레이션 ────────────────────────────────
        // side별 C4 → C3 → C2 → C1 순으로 [Collet Cal → (C4면 Save Bottom) → Apply T → COC]를 연속 수행하고,
        // 전체 완료 후 SaveMachineSettings 1회. 실패/정지 시 즉시 전체 중단(알람 상태 연속 동작 금지).
        private sealed class BatchColletTarget
        {
            public VisionFocusPickerSide Side;
            public int ColletNo;
            public string Label { get { return (Side == VisionFocusPickerSide.Rear ? "R" : "F") + " C" + ColletNo; } }
        }

        private void chkBatchAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading)
                return;

            bool value = chkBatchAll.Checked;
            _loading = true;
            try
            {
                chkBatchFront1.Checked = value;
                chkBatchFront2.Checked = value;
                chkBatchFront3.Checked = value;
                chkBatchFront4.Checked = value;
                chkBatchRear1.Checked = value;
                chkBatchRear2.Checked = value;
                chkBatchRear3.Checked = value;
                chkBatchRear4.Checked = value;
            }
            finally
            {
                _loading = false;
            }
        }

        private async void btnBatchStart_Click(object sender, EventArgs e)
        {
            await RunBatchCalibrationAsync().ConfigureAwait(true);
        }

        // 체크된 콜렛을 side별 실행 순서(C4 → C3 → C2 → C1)로 정렬해 반환한다.
        private System.Collections.Generic.List<BatchColletTarget> BuildBatchTargets()
        {
            var list = new System.Collections.Generic.List<BatchColletTarget>();
            AppendBatchSideTargets(list, VisionFocusPickerSide.Front,
                chkBatchFront4.Checked, chkBatchFront1.Checked, chkBatchFront2.Checked, chkBatchFront3.Checked);
            AppendBatchSideTargets(list, VisionFocusPickerSide.Rear,
                chkBatchRear4.Checked, chkBatchRear1.Checked, chkBatchRear2.Checked, chkBatchRear3.Checked);
            return list;
        }

        private static void AppendBatchSideTargets(System.Collections.Generic.List<BatchColletTarget> list,
            VisionFocusPickerSide side, bool c4, bool c1, bool c2, bool c3)
        {
            // 실행 순서: 기준 콜렛 C4 먼저 → C3 → C2 → C1 (4→3→2→1).
            if (c4) list.Add(new BatchColletTarget { Side = side, ColletNo = 4 });
            if (c3) list.Add(new BatchColletTarget { Side = side, ColletNo = 3 });
            if (c2) list.Add(new BatchColletTarget { Side = side, ColletNo = 2 });
            if (c1) list.Add(new BatchColletTarget { Side = side, ColletNo = 1 });
        }

        private bool ValidateBatchReferenceRecords(
            Form1 host,
            System.Collections.Generic.List<BatchColletTarget> targets,
            out string reason)
        {
            reason = string.Empty;
            if (host == null || host.Machine == null)
            {
                reason = "장비가 준비되지 않아 Collet Batch 기준값을 확인할 수 없습니다.";
                return false;
            }

            foreach (VisionFocusPickerSide side in new[] { VisionFocusPickerSide.Front, VisionFocusPickerSide.Rear })
            {
                VisionFocusPickerSide sideLocal = side;
                bool hasSideTarget = targets.Exists(t => t.Side == sideLocal);
                bool hasSelectedC4 = targets.Exists(t => t.Side == sideLocal && t.ColletNo == 4);
                if (!hasSideTarget || hasSelectedC4)
                    continue;

                ColletCalibrationRecord reference = ResolveData(host.Machine).GetRecord(side, 4);
                if (reference == null || !reference.Valid)
                {
                    reason = side +
                             " side는 C4가 선택되지 않았고 기존 C4 기준 저장값도 유효하지 않습니다. " +
                             "모션을 시작하지 않습니다. C4를 함께 선택하거나 먼저 C4를 캘리브레이션하세요.";
                    return false;
                }
            }

            return true;
        }

        // 현재 대상 콜렛을 전환하고 SETTING 그리드 표시를 동기화한다(수동 개별 버튼이 쓰는 "현재 대상 콜렛" 개념과 동일).
        private void SetTargetCollet(VisionFocusPickerSide side, int colletNo)
        {
            _side = side;
            _colletNo = colletNo;
            RefreshSettingGrid();
        }

        private async Task RunBatchCalibrationAsync()
        {
            if (_busy)
                return;

            var targets = BuildBatchTargets();
            if (targets.Count == 0)
            {
                lblStatus.Text = "BATCH 실행할 콜렛이 선택되지 않았습니다. F/R C1~C4 체크박스를 선택하세요.";
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET BATCH", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form1 host = null;
            Action stopHandler = null;
            IDisposable actionScope = null;
            CancellationTokenSource runCts = null;
            VisionFocusPickerSide originalSide = _side;
            int originalColletNo = _colletNo;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "COLLET BATCH", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                if (!ValidateBatchReferenceRecords(host, targets, out reason))
                {
                    lblStatus.Text = reason;
                    EventLogger.Write(EventKind.Warning, "CAL", "COLLET-BATCH-REFERENCE-BLOCK", reason);
                    QMC.Common.MessageDialog.Show(this, reason, "COLLET BATCH", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!SaveParameterSettingsFromUi(false))
                    return;

                ClearLastSuccessfulResult();

                runCts = BeginManualCalibrationRun(host, "ColletBatch", out actionScope, out stopHandler);
                CancellationToken ct = runCts.Token;

                int okCount = 0;
                bool aborted = false;
                foreach (VisionFocusPickerSide side in new[] { VisionFocusPickerSide.Front, VisionFocusPickerSide.Rear })
                {
                    if (aborted)
                        break;

                    VisionFocusPickerSide sideLocal = side;
                    var sideTargets = targets.FindAll(t => t.Side == sideLocal);
                    if (sideTargets.Count == 0)
                        continue;

                    // C4 미선택 side는 기존 C4 저장값이 유효할 때만 C1~3 진행(아니면 이 side 중단).
                    bool c4Selected = sideTargets.Exists(t => t.ColletNo == 4);
                    if (!c4Selected)
                    {
                        ColletCalibrationRecord ref4 = ResolveData(host.Machine).GetRecord(side, 4);
                        if (ref4 == null || !ref4.Valid)
                        {
                            string fail = side +
                                          " side의 C4 기준 저장값이 Batch 실행 중 유효하지 않게 변경되었습니다. " +
                                          "다음 동작을 시작하지 않고 BATCH를 중단합니다.";
                            AppendSaveHistory(new[] { "[BATCH] " + fail });
                            EventLogger.Write(EventKind.Warning, "CAL", "COLLET-BATCH-REFERENCE-LOST", fail);
                            lblStatus.Text = fail;
                            aborted = true;
                            break;
                        }
                    }

                    foreach (BatchColletTarget target in sideTargets)
                    {
                        ct.ThrowIfCancellationRequested();
                        SetTargetCollet(target.Side, target.ColletNo);
                        lblStatus.Text = "[BATCH] " + target.Label + " Collet Calibration 실행 중...";

                        int calResult = await RunSingleColletSequenceAsync(host, ct).ConfigureAwait(true);
                        if (calResult != 0)
                        {
                            string fail = target.Label + " Collet Calibration 실패(result=" + calResult + "). BATCH를 중단합니다.";
                            AppendSaveHistory(new[] { "[BATCH] " + fail });
                            EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-BATCH-CAL-FAIL", fail);
                            lblStatus.Text = fail;
                            aborted = true;
                            break;
                        }

                        // C4 측정 성공 직후 Save Bottom 자동
                        if (target.ColletNo == 4)
                        {
                            bool sbOk = await SaveBottomTeachingCoreAsync(false).ConfigureAwait(true);
                            string sbMsg = _coreResultMessage;
                            if (!sbOk)
                            {
                                string fail = target.Label + " Save Bottom 실패: " + sbMsg + ". BATCH를 중단합니다.";
                                AppendSaveHistory(new[] { "[BATCH] " + fail });
                                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-BATCH-SAVEBOTTOM-FAIL", fail);
                                lblStatus.Text = fail;
                                aborted = true;
                                break;
                            }
                            AppendSaveHistory(new[] { "[BATCH] " + target.Label + " Save Bottom: " + sbMsg });
                        }

                        // 각 콜렛 측정 성공 직후 Apply T 자동(성공/Valid 레코드만)
                        bool atOk = await ApplyTHomeOffsetCoreAsync(false).ConfigureAwait(true);
                        string atMsg = _coreResultMessage;
                        if (!atOk)
                        {
                            string fail = target.Label + " Apply T 실패: " + atMsg + ". BATCH를 중단합니다.";
                            AppendSaveHistory(new[] { "[BATCH] " + fail });
                            EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-BATCH-APPLYT-FAIL", fail);
                            lblStatus.Text = fail;
                            aborted = true;
                            break;
                        }
                        AppendSaveHistory(new[] { "[BATCH] " + target.Label + " Apply T: " + atMsg });

                        // COC(회전중심)는 ColletCalibrationSequence 내부(측정 직후, AutoFocus 켜짐 시)에서 이미 수행된다.
                        // 여기서 별도 COC를 다시 돌리면 Save Bottom이 FinalPickerZ를 검사 티칭 Z로 덮어써
                        // "COC는 기존 Collet Calibration 완료 X/Y/Z 위치에서만 시작" 위치 검사에서 실패하므로 호출하지 않는다.
                        RememberSuccessfulResult(
                            target.Side,
                            target.ColletNo,
                            ResolveData(host.Machine).GetRecord(target.Side, target.ColletNo));
                        okCount++;
                        AppendSaveHistory(new[] { "[BATCH] " + target.Label + " 완료(Collet Cal+COC" +
                            (target.ColletNo == 4 ? " → Save Bottom" : string.Empty) + " → Apply T)" });
                    }
                }

                await Task.Run(() => host.SaveMachineSettings()).ConfigureAwait(true);
                RefreshResultGrid();
                lblStatus.Text = aborted
                    ? "BATCH 중단됨. 성공 " + okCount + "/" + targets.Count + " 콜렛. Alarm/Event Log를 확인하세요."
                    : "BATCH 완료. 성공 " + okCount + "/" + targets.Count + " 콜렛.";
                EventLogger.Write(aborted ? EventKind.Warning : EventKind.Event, "CAL", "COLLET-BATCH-END", lblStatus.Text);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "BATCH가 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-BATCH-STOP", lblStatus.Text);
            }
            catch (SequenceStopException ex)
            {
                lblStatus.Text = "BATCH 정지: " + ex.Message;
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-BATCH-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "BATCH 실행 중 예외가 발생했습니다: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-BATCH-RUN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET BATCH", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                SetTargetCollet(originalSide, originalColletNo);
                _busy = false;
                SetButtonsEnabled(true);
                UpdateStopButtonEnabled();
            }
        }

        // 단일 콜렛 Collet Calibration 시퀀스 실행(스코프/정지 핸들러는 BATCH가 소유하므로 여기서는 만들지 않는다).
        private async Task<int> RunSingleColletSequenceAsync(Form1 host, CancellationToken ct)
        {
            var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            var sequence = new ColletCalibrationSequence(context, _side, _colletNo);
            // BATCH도 상대 Picker를 Avoid로 자동 이동한다(이미 Avoid면 시퀀스가 재이동 없이 통과).
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = SequenceStartMode.Restart;
            options.PickerNo = _colletNo;
            options.RestrictToPickerNo = _colletNo;
            int result = await sequence.RunAsync(ct, options).ConfigureAwait(true);
            RefreshResultGrid();
            return result;
        }

        private bool SaveRotationCenterToRecipe(Form1 host, double centerX, double centerY, out string message)
        {
            message = string.Empty;
            if (host == null || host.Machine == null || string.IsNullOrWhiteSpace(host.ActiveRecipeName))
            {
                message = "현재 활성 Recipe가 없어 Collet 회전 중심 좌표를 저장할 수 없습니다.";
                return false;
            }

            int index = Math.Max(0, Math.Min(3, _colletNo - 1));
            if (_side == VisionFocusPickerSide.Front)
            {
                if (host.Machine.PickerFrontUnit == null || host.Machine.PickerFrontUnit.Recipe == null)
                {
                    message = "Front Picker Recipe가 준비되지 않아 회전 중심 좌표를 저장할 수 없습니다.";
                    return false;
                }

                host.Machine.PickerFrontUnit.Recipe.EnsurePositionObjects();
                host.Machine.PickerFrontUnit.Recipe.ColletRotationCenterX[index] = centerX;
                host.Machine.PickerFrontUnit.Recipe.ColletRotationCenterY[index] = centerY;
                host.Machine.PickerFrontUnit.Recipe.ColletRotationCenterValid[index] = true;
            }
            else
            {
                if (host.Machine.PickerRearUnit == null || host.Machine.PickerRearUnit.Recipe == null)
                {
                    message = "Rear Picker Recipe가 준비되지 않아 회전 중심 좌표를 저장할 수 없습니다.";
                    return false;
                }

                host.Machine.PickerRearUnit.Recipe.EnsurePositionObjects();
                host.Machine.PickerRearUnit.Recipe.ColletRotationCenterX[index] = centerX;
                host.Machine.PickerRearUnit.Recipe.ColletRotationCenterY[index] = centerY;
                host.Machine.PickerRearUnit.Recipe.ColletRotationCenterValid[index] = true;
            }

            bool saved = host.SaveMachineRecipe(host.ActiveRecipeName);
            message = "COC Recipe 저장: " + _side + " C" + _colletNo +
                      ", X=" + centerX.ToString("F6") +
                      ", Y=" + centerY.ToString("F6") +
                      ", recipe=" + host.ActiveRecipeName +
                      ", saved=" + saved;
            return saved;
        }

        private async Task RunMoveZForwardAsync()
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

                string editReason;
                if (!CommitSettingGridEdits(out editReason))
                {
                    lblStatus.Text = editReason;
                    QMC.Common.MessageDialog.Show(this, editReason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("ColletCalibrationDialog.MoveZAvoid"))
                {
                    result = await MoveBothPickersZToAvoidAsync(host.Machine).ConfigureAwait(true);
                }
                runCts.Token.ThrowIfCancellationRequested();

                lblStatus.Text = result == 0
                    ? "Front/Rear 픽커 Z 전체 Avoid 이동 완료."
                    : "픽커 Z Avoid 이동 실패. Alarm/Event Log를 확인하세요.";
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-Z-AVOID",
                    lblStatus.Text + ", result=" + result);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "픽커 Z Avoid 이동이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "픽커 Z Avoid 이동 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-Z-AVOID-EX", lblStatus.Text);
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

                string editReason;
                if (!CommitSettingGridEdits(out editReason))
                {
                    lblStatus.Text = editReason;
                    QMC.Common.MessageDialog.Show(this, editReason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("ColletCalibrationDialog.MoveYAvoid"))
                {
                    if (!AreBothPickersZInAvoidPosition(host.Machine))
                    {
                        lblStatus.Text = "Picker Y Avoid 이동 전 Front/Rear 픽커 Z 전체를 먼저 Avoid 위치로 이동하세요. (Z-AVOID 버튼)";
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    result = await MoveBothPickersYToAvoidAsync(host.Machine).ConfigureAwait(true);
                }
                runCts.Token.ThrowIfCancellationRequested();

                lblStatus.Text = result == 0
                    ? "Front/Rear Picker Y Avoid 이동 완료."
                    : "Picker Y Avoid 이동 실패. Alarm/Event Log를 확인하세요.";
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-PICKER-Y-AVOID",
                    lblStatus.Text + ", result=" + result);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Picker Y Avoid 이동이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Picker Y Avoid 이동 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-PICKER-Y-AVOID-EX", lblStatus.Text);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
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

                ColletCalibrationData data = ResolveData(host.Machine);
                ColletCalibrationSettings settings = data.Settings;
                settings.EnsureDefaults();
                _finder = settings.BottomFinderName;
                _thetaToleranceDeg = settings.ThetaToleranceDeg;
                _maxThetaIterations = settings.MaxThetaIterations;
                _thetaGain = settings.ThetaMoveGain;
                _xyToleranceMm = settings.XyToleranceMm;
                _maxXyIterations = settings.MaxXyIterations;
                _xyGainX = settings.XyMoveGainX;
                _xyGainY = settings.XyMoveGainY;
                _xyFineMaxMm = settings.FineAlignMaxXyMoveMm;
                _useDiagonalXyTolerance = settings.UseDiagonalXyTolerance;
                _scoreThreshold = settings.ScoreThreshold;
                _visionTimeoutMs = settings.VisionTimeoutMs;
                _moveVelocity = settings.Motion.MoveVelocity;
                _moveAcceleration = settings.Motion.MoveAcceleration;
                _moveDeceleration = settings.Motion.MoveDeceleration;
                _moveTimeoutMs = settings.Motion.MoveTimeoutMs;
                _cocRotationVelocityDegPerSec = settings.CocRotationVelocityDegPerSec;
                _autoFocus = settings.RunAutoFocusAfterTheta;
                _sideAutoFocus = settings.RunSideAutoFocusAfterCoc;

                RecipeProject project = LoadActiveProject(host);
                if (project != null)
                {
                    if (project.ColletZ == null)
                        project.ColletZ = new ColletZConfigSubset();
                    project.ColletZ.Ensure();
                    _colletType = project.ColletZ.ColletType;
                    _colletDieCalThicknessMm = project.ColletZ.DieCalThicknessMm;
                    _colletFilmThicknessMm = project.ColletZ.FilmThicknessMm;
                    _colletFlatZOffsetMm = project.ColletZ.FlatZOffsetMm;
                    _colletRimOffsetFromFlatMm = project.ColletZ.RimOffsetFromFlatMm;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Collet Calibration 설정 로드 실패: " + ex.Message;
            }
            finally
            {
                _loading = false;
                RefreshSettingGrid();
            }
        }

        private bool SaveParameterSettingsFromUi(bool showMessage)
        {
            try
            {
                string editReason;
                if (!CommitSettingGridEdits(out editReason))
                {
                    lblStatus.Text = editReason;
                    if (showMessage)
                        QMC.Common.MessageDialog.Show(this, editReason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                {
                    lblStatus.Text = reason;
                    return false;
                }

                ColletCalibrationData data = ResolveData(host.Machine);
                data.Settings.BottomFinderName = _finder;
                data.Settings.ThetaToleranceDeg = _thetaToleranceDeg;
                data.Settings.MaxThetaIterations = _maxThetaIterations;
                data.Settings.ThetaMoveGain = _thetaGain;
                data.Settings.XyToleranceMm = _xyToleranceMm;
                data.Settings.MaxXyIterations = _maxXyIterations;
                data.Settings.XyMoveGainX = _xyGainX;
                data.Settings.XyMoveGainY = _xyGainY;
                data.Settings.FineAlignMaxXyMoveMm = _xyFineMaxMm;
                data.Settings.UseDiagonalXyTolerance = _useDiagonalXyTolerance;
                data.Settings.ScoreThreshold = _scoreThreshold;
                data.Settings.VisionTimeoutMs = _visionTimeoutMs;
                if (data.Settings.Motion == null)
                    data.Settings.Motion = new CalibrationMotionSettings();
                data.Settings.Motion.MoveVelocity = _moveVelocity;
                data.Settings.Motion.MoveAcceleration = _moveAcceleration;
                data.Settings.Motion.MoveDeceleration = _moveDeceleration;
                data.Settings.Motion.MoveTimeoutMs = _moveTimeoutMs;
                data.Settings.CocRotationVelocityDegPerSec = _cocRotationVelocityDegPerSec;
                data.Settings.RunAutoFocusAfterTheta = _autoFocus;
                data.Settings.RunSideAutoFocusAfterCoc = _sideAutoFocus;
                data.Settings.EnsureDefaults();
                _finder = data.Settings.BottomFinderName;
                _moveVelocity = data.Settings.Motion.MoveVelocity;
                _moveAcceleration = data.Settings.Motion.MoveAcceleration;
                _moveDeceleration = data.Settings.Motion.MoveDeceleration;
                _moveTimeoutMs = data.Settings.Motion.MoveTimeoutMs;
                _cocRotationVelocityDegPerSec = data.Settings.CocRotationVelocityDegPerSec;
                string projectSaveMessage;
                if (!SaveActiveProjectColletZ(host, false, out projectSaveMessage))
                {
                    lblStatus.Text = projectSaveMessage;
                    if (showMessage)
                        QMC.Common.MessageDialog.Show(this, projectSaveMessage, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                host.SaveMachineSettings();
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSaveSettings",
                    "Collet Calibration 설정 저장. side=" + _side +
                    ", colletNo=" + _colletNo +
                    ", finder=" + data.Settings.BottomFinderName +
                    ", thetaTolDeg=" + data.Settings.ThetaToleranceDeg.ToString("F6") +
                    ", thetaRetry=" + data.Settings.MaxThetaIterations +
                    ", thetaGain=" + data.Settings.ThetaMoveGain.ToString("F6") +
                    ", xyTolMm=" + data.Settings.XyToleranceMm.ToString("F6") +
                    ", xyRetry=" + data.Settings.MaxXyIterations +
                    ", xyGainX=" + data.Settings.XyMoveGainX.ToString("F6") +
                    ", xyGainY=" + data.Settings.XyMoveGainY.ToString("F6") +
                    ", xyFineMaxMm=" + data.Settings.FineAlignMaxXyMoveMm.ToString("F6") +
                    ", xyTolMode=" + (data.Settings.UseDiagonalXyTolerance ? "Diagonal" : "Axis") +
                    ", scoreMin=" + data.Settings.ScoreThreshold.ToString("F6") +
                    ", visionTimeoutMs=" + data.Settings.VisionTimeoutMs +
                    ", moveVelocity=" + data.Settings.Motion.MoveVelocity.ToString("F6") +
                    ", moveAcceleration=" + data.Settings.Motion.MoveAcceleration.ToString("F6") +
                    ", moveDeceleration=" + data.Settings.Motion.MoveDeceleration.ToString("F6") +
                    ", moveTimeoutMs=" + data.Settings.Motion.MoveTimeoutMs +
                    ", cocRotationVelocityDegPerSec=" + data.Settings.CocRotationVelocityDegPerSec.ToString("F6") +
                    ", autoFocus=" + data.Settings.RunAutoFocusAfterTheta);
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-SAVE",
                    "Collet Calibration 설정 저장. side=" + _side +
                    ", colletNo=" + _colletNo +
                    ", dieThickness=" + _colletDieCalThicknessMm.ToString("F6") +
                    ", filmThickness=" + _colletFilmThicknessMm.ToString("F6") +
                    ", flatOffset=" + _colletFlatZOffsetMm.ToString("F6") +
                    ", rimOffset=" + _colletRimOffsetFromFlatMm.ToString("F6"));
                RefreshSettingGrid();
                RefreshResultGrid();

                if (showMessage)
                    lblStatus.Text = "Collet Calibration 파라미터를 저장했습니다. 측정 결과와 Picker 티칭값은 변경하지 않았습니다.";

                return true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Collet Calibration 설정 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-SAVE", lblStatus.Text);
                return false;
            }
            finally
            {
            }
        }

        private bool SaveLastSuccessfulResult(bool showMessage)
        {
            try
            {
                if (_lastSuccessfulResultRecord == null)
                    return BlockResultSave(
                        "정상 완료된 Collet Calibration 결과가 없습니다. START 또는 BATCH START를 먼저 완료하세요.",
                        showMessage);

                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null)
                    return BlockResultSave(reason, showMessage);

                ColletCalibrationRecord current = ResolveData(host.Machine).GetRecord(
                    _lastSuccessfulResultSide,
                    _lastSuccessfulResultColletNo);
                if (current == null ||
                    !ReferenceEquals(current, _lastSuccessfulResultRecord) ||
                    !current.Valid ||
                    current.Side != _lastSuccessfulResultSide ||
                    current.ColletNo != _lastSuccessfulResultColletNo ||
                    current.UpdatedAt != _lastSuccessfulResultUpdatedAt)
                {
                    return BlockResultSave(
                        "마지막 정상 완료 결과가 이후에 변경되었거나 다시 로드되었습니다. 잘못된 대상 저장을 막기 위해 SAVE RESULT를 차단합니다. 대상=" +
                        _lastSuccessfulResultSide + " C" + _lastSuccessfulResultColletNo,
                        showMessage);
                }

                // Sequence가 만든 정확한 target record를 다시 영속화한다.
                // 현재 축 ActualPosition은 읽지 않으며 티칭 위치를 재계산하지 않는다.
                host.SaveMachineSettings();
                lblStatus.Text = "마지막 Collet Calibration 결과를 저장했습니다. target=" +
                                 _lastSuccessfulResultSide + " C" + _lastSuccessfulResultColletNo +
                                 ", currentSelector=" + _side + " C" + _colletNo +
                                 ", updatedAt=" + _lastSuccessfulResultUpdatedAt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSaveResult", lblStatus.Text);
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-SAVE-RESULT", lblStatus.Text);
                RefreshResultGrid();
                return true;
            }
            catch (Exception ex)
            {
                return BlockResultSave("Collet Calibration 결과 저장 실패: " + ex.Message, showMessage);
            }
        }

        private bool BlockResultSave(string message, bool showMessage)
        {
            lblStatus.Text = message;
            EventLogger.Write(EventKind.Warning, "CAL", "COLLET-CAL-SAVE-RESULT-BLOCK", message);
            if (showMessage)
                QMC.Common.MessageDialog.Show(this, message, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void RememberSuccessfulResult(
            VisionFocusPickerSide side,
            int colletNo,
            ColletCalibrationRecord record)
        {
            if (record == null || !record.Valid || record.Side != side || record.ColletNo != colletNo)
            {
                ClearLastSuccessfulResult();
                EventLogger.Write(EventKind.Warning, "CAL", "COLLET-CAL-RESULT-TRACK",
                    "정상 완료 결과 추적에 실패했습니다. side=" + side + ", colletNo=" + colletNo);
                return;
            }

            _lastSuccessfulResultRecord = record;
            _lastSuccessfulResultSide = side;
            _lastSuccessfulResultColletNo = colletNo;
            _lastSuccessfulResultUpdatedAt = record.UpdatedAt;
        }

        private void ClearLastSuccessfulResult()
        {
            _lastSuccessfulResultRecord = null;
            _lastSuccessfulResultSide = VisionFocusPickerSide.Front;
            _lastSuccessfulResultColletNo = 0;
            _lastSuccessfulResultUpdatedAt = DateTime.MinValue;
        }

        private bool CommitSettingGridEdits(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (gridSettings.IsCurrentCellDirty)
                    gridSettings.CommitEdit(DataGridViewDataErrorContexts.Commit);

                gridSettings.EndEdit();
                Validate();

                foreach (DataGridViewRow row in gridSettings.Rows)
                {
                    if (row == null || row.IsNewRow)
                        continue;

                    SettingInfo info = row.Tag as SettingInfo;
                    if (info == null)
                        continue;
                    if (info.ReadOnly)
                        continue;

                    if (!ApplySettingRowValue(row, info, out reason))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Collet Calibration 설정값 반영 실패: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool ApplySettingRowValue(DataGridViewRow row, SettingInfo info, out string reason)
        {
            reason = string.Empty;
            if (info.ReadOnly)
                return true;

            if (!info.Numeric)
            {
                ApplySettingValue(row);
                return true;
            }

            string valueText = Convert.ToString(row.Cells[colSettingValue.Index].Value, CultureInfo.InvariantCulture);
            double value;
            if (!TryParseSettingNumber(valueText, out value))
            {
                reason = info.Name + " 설정값이 숫자가 아닙니다. value=" + valueText;
                return false;
            }

            ApplyNumericSetting(info, value);
            return true;
        }

        private void RefreshSettingGrid()
        {
            try
            {
                bool oldLoading = _loading;
                _loading = true;
                gridSettings.Rows.Clear();
                AddSettingRow(CreateOption(SettingKey.Side, "Side", "캘리브레이션할 Picker Side입니다. Front와 Rear는 각각 별도 #4 기준과 Collet Offset을 저장합니다.", SideOptions), _side == VisionFocusPickerSide.Rear ? "Rear" : "Front");
                AddSettingRow(CreateOption(SettingKey.ColletNo, "Collet No", "캘리브레이션할 Collet 번호입니다. #4가 Bottom Camera 기준 티칭 위치이며, #3/#2/#1은 #4 기준 피치 위치에서 Offset을 저장합니다.", ColletOptions), _colletNo.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateText(SettingKey.Finder, "Finder", "Vision PC BottomInspection 채널에 요청할 Finder 이름입니다. Vision PC에 등록된 Collet 검출 이름과 같아야 합니다."), _finder);
                AddSettingRow(CreateNumber(SettingKey.ThetaTolerance, "Theta Tol", "deg", "T축 보정 완료 판정 각도입니다. Vision에서 받은 절대 Theta 값이 이 값 이하이면 T 보정 OK로 봅니다.", false), _thetaToleranceDeg.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.MaxThetaIteration, "Theta Retry", "ea", "T축 보정을 반복할 최대 횟수입니다. 이 횟수 안에 Theta Tol 안으로 들어오지 않으면 NG 처리합니다.", true), _maxThetaIterations.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateNumber(SettingKey.ThetaGain, "Theta Gain", "x", "Vision에서 측정한 Theta를 0으로 만들기 위해 반대 방향으로 곱하는 이동 비율입니다. 1.0은 측정값만큼 보정합니다.", false), _thetaGain.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyTolerance, "XY Tol", "mm", "XY 중심 보정 완료 판정 거리입니다. XY Tol Mode가 Diagonal이면 대각 거리, Axis면 X/Y 각각의 절대값으로 판정합니다.", false), _xyToleranceMm.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.MaxXyIteration, "XY Retry", "ea", "XY 중심 보정을 반복할 최대 횟수입니다. 이 횟수 안에 XY Tol 안으로 들어오지 않으면 NG 처리합니다.", true), _maxXyIterations.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateNumber(SettingKey.XyGainX, "XY Gain X", "x", "Vision에서 측정한 X 방향 보정량에 곱하는 이동 비율입니다. 1.0은 측정값만큼 그대로 보정합니다.", false), _xyGainX.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyGainY, "XY Gain Y", "x", "Vision에서 측정한 Y 방향 보정량에 곱하는 이동 비율입니다. 1.0은 측정값만큼 그대로 보정합니다.", false), _xyGainY.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyFineMax, "XY Fine Max", "mm", "Z가 검사 위치에 내려온 상태에서 그대로 XY 미세 보정을 허용할 최대 이동량입니다. X/Y 이동량이 이 값 이하이면 FineAlign으로 움직이고, 초과하면 Z/Y Avoid 후 큰 이동으로 처리합니다.", false), _xyFineMaxMm.ToString("F6"));
                AddSettingRow(CreateOption(SettingKey.XyToleranceMode, "XY Tol Mode", "XY Tol 판정 방식입니다. Diagonal은 sqrt(X^2+Y^2) 거리로 보고, Axis는 |X|와 |Y|가 각각 Tol 이하인지 봅니다.", XyToleranceModeOptions), _useDiagonalXyTolerance ? "Diagonal" : "Axis");
                AddSettingRow(CreateNumber(SettingKey.ScoreThreshold, "Score Min", "score", "Vision 검출 Score 최소값입니다. 0이면 Score 기준을 사용하지 않고, 0보다 크면 Score가 이 값보다 낮을 때 NG 처리합니다.", false), _scoreThreshold.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.VisionTimeout, "Vision Timeout", "ms", "Vision PC Collet Finder 응답을 기다리는 시간입니다. 이 시간 안에 응답이 없으면 Timeout NG 처리합니다.", true), _visionTimeoutMs.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateNumber(SettingKey.MoveVelocity, "Move Speed", "mm/s", "Collet Calibration에서 XY/T/Z 및 시작 안전 위치 이동에 사용할 전용 속도입니다. AutoFocus 스캔 속도는 Vision Focus Cal 설정을 따로 사용합니다.", false), _moveVelocity.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.MoveAcceleration, "Move Acc", "mm/s2", "Collet Calibration 전용 이동 가속도입니다. 축 인터락은 기존 규칙을 그대로 탑니다.", false), _moveAcceleration.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.MoveDeceleration, "Move Dec", "mm/s2", "Collet Calibration 전용 이동 감속도입니다. 축 인터락은 기존 규칙을 그대로 탑니다.", false), _moveDeceleration.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.MoveTimeout, "Move Timeout", "ms", "Collet Calibration 전용 이동 완료/인포지션 대기 시간입니다.", true), _moveTimeoutMs.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateNumber(SettingKey.CocRotationVelocity, "COC T Speed", "deg/s", "COC START 실행 시 선택 콜렛 T축을 360도 회전할 속도입니다. 전체 콜렛에 공통 적용하며 기본값은 30 deg/s입니다.", false), _cocRotationVelocityDegPerSec.ToString("F6"));
                AddSettingRow(CreateOption(SettingKey.AutoFocus, "AutoFocus", "True이면 Bottom AutoFocus와 Collet 보정 후 COC 회전 중심을 검출/적용하고, 해당 콜렛의 Side 0도와 90도 AutoFocus까지 순서대로 실행합니다. Side Focus에는 현재 Picker의 Bottom Die 검사 결과가 필요합니다.", BoolOptions), _autoFocus ? "True" : "False");
                AddSettingRow(CreateOption(SettingKey.SideAutoFocus, "Side AutoFocus", "True이면 COC 회전 중심 검출 후 Side 0도/90도 AutoFocus를 수행합니다. False이면 COC(회전 중심)까지만 수행하고 Side AutoFocus는 건너뜁니다. (AutoFocus가 True일 때만 의미가 있습니다.)", BoolOptions), _sideAutoFocus ? "True" : "False");
                AddSettingRow(CreateNumber(SettingKey.ColletDieCalThickness, "Die Thickness", "mm", "Collet Cal에서 AutoFocus 후 측정한 Best Z에 더할 다이 두께입니다. 저장 검사 Z = 측정 Z + Die Thickness + Film Thickness + 현재 Collet Type Offset입니다.", false), _colletDieCalThicknessMm.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.ColletRimOffsetFromFlat, "Rim Collet Offset", "mm", "Recipe Collet Type이 Rim일 때 측정 Z에 더할 콜렛 Offset입니다. 아래 방향은 -이고 위 방향은 +이므로 위로 올릴 값은 +로 입력합니다.", false), _colletRimOffsetFromFlatMm.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.ColletFlatZOffset, "Flat Collet Offset", "mm", "Recipe Collet Type이 Flat일 때 측정 Z에 더할 콜렛 Offset입니다. 아래 방향은 -이고 위 방향은 +이므로 위로 올릴 값은 +로 입력합니다.", false), _colletFlatZOffsetMm.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.ColletFilmThickness, "Film Thickness", "mm", "Collet Cal에서 AutoFocus 후 측정한 Best Z에 더할 필름 두께입니다. 저장 검사 Z = 측정 Z + Die Thickness + Film Thickness + 현재 Collet Type Offset입니다.", false), _colletFilmThicknessMm.ToString("F6"));
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

        private void AddSettingRow(SettingInfo info, string value)
        {
            int rowIndex = gridSettings.Rows.Add();
            DataGridViewRow row = gridSettings.Rows[rowIndex];
            row.Tag = info;
            row.Cells[colSettingName.Index].Value = info.Name;
            row.Cells[colSettingUnit.Index].Value = info.Unit;
            row.Cells[colSettingName.Index].ToolTipText = info.ToolTip;
            row.Cells[colSettingValue.Index].ToolTipText = info.ToolTip;
            row.Cells[colSettingUnit.Index].ToolTipText = info.ToolTip;

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

            row.Cells[colSettingValue.Index].ReadOnly = info.Numeric || info.ReadOnly;
        }

        private void ApplySettingValue(DataGridViewRow row)
        {
            SettingInfo info = row.Tag as SettingInfo;
            if (info == null)
                return;

            string value = Convert.ToString(row.Cells[colSettingValue.Index].Value, CultureInfo.InvariantCulture);
            switch (info.Key)
            {
                case SettingKey.Side:
                    _side = value == "Rear" ? VisionFocusPickerSide.Rear : VisionFocusPickerSide.Front;
                    break;
                case SettingKey.ColletNo:
                    int.TryParse(value, out _colletNo);
                    if (_colletNo < 1) _colletNo = 1;
                    if (_colletNo > 4) _colletNo = 4;
                    break;
                case SettingKey.AutoFocus:
                    _autoFocus = value == "True";
                    break;
                case SettingKey.SideAutoFocus:
                    _sideAutoFocus = value == "True";
                    break;
                case SettingKey.XyToleranceMode:
                    _useDiagonalXyTolerance = value != "Axis";
                    break;
                case SettingKey.Finder:
                    _finder = value;
                    break;
            }
        }

        private static bool TryParseSettingNumber(string valueText, out double value)
        {
            if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return double.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

            return true;
        }

        private void ApplyNumericSetting(SettingInfo info, double value)
        {
            switch (info.Key)
            {
                case SettingKey.ThetaTolerance:
                    _thetaToleranceDeg = Math.Max(0.0001, value);
                    break;
                case SettingKey.MaxThetaIteration:
                    _maxThetaIterations = Math.Max(1, Math.Min(20, (int)Math.Round(value)));
                    break;
                case SettingKey.ThetaGain:
                    _thetaGain = Math.Max(0.0001, value);
                    break;
                case SettingKey.XyTolerance:
                    _xyToleranceMm = Math.Max(0.000001, value);
                    break;
                case SettingKey.MaxXyIteration:
                    _maxXyIterations = Math.Max(1, Math.Min(20, (int)Math.Round(value)));
                    break;
                case SettingKey.XyGainX:
                    _xyGainX = Math.Abs(value) <= double.Epsilon ? 1.0 : value;
                    break;
                case SettingKey.XyGainY:
                    _xyGainY = Math.Abs(value) <= double.Epsilon ? 1.0 : value;
                    break;
                case SettingKey.XyFineMax:
                    _xyFineMaxMm = Math.Max(0.000001, Math.Min(2.0, value));
                    break;
                case SettingKey.ScoreThreshold:
                    _scoreThreshold = Math.Max(0.0, value);
                    break;
                case SettingKey.VisionTimeout:
                    _visionTimeoutMs = Math.Max(100, (int)Math.Round(value));
                    break;
                case SettingKey.MoveVelocity:
                    _moveVelocity = Math.Max(0.001, value);
                    break;
                case SettingKey.MoveAcceleration:
                    _moveAcceleration = Math.Max(0.001, value);
                    break;
                case SettingKey.MoveDeceleration:
                    _moveDeceleration = Math.Max(0.001, value);
                    break;
                case SettingKey.MoveTimeout:
                    _moveTimeoutMs = Math.Max(100, (int)Math.Round(value));
                    break;
                case SettingKey.CocRotationVelocity:
                    _cocRotationVelocityDegPerSec = Math.Max(0.1, Math.Min(360.0, value));
                    break;
                case SettingKey.ColletDieCalThickness:
                    _colletDieCalThicknessMm = Math.Max(0.0, value);
                    break;
                case SettingKey.ColletFilmThickness:
                    _colletFilmThicknessMm = Math.Max(0.0, value);
                    break;
                case SettingKey.ColletFlatZOffset:
                    _colletFlatZOffsetMm = value;
                    break;
                case SettingKey.ColletRimOffsetFromFlat:
                    _colletRimOffsetFromFlatMm = value;
                    break;
            }
        }

        private void RefreshResultGrid()
        {
            try
            {
                gridResults.Rows.Clear();
                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                    return;

                ColletCalibrationData data = ResolveData(host.Machine);
                AddRecords(host, data, VisionFocusPickerSide.Front);
                AddRecords(host, data, VisionFocusPickerSide.Rear);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void AddRecords(Form1 host, ColletCalibrationData data, VisionFocusPickerSide side)
        {
            for (int i = 1; i <= 4; i++)
            {
                ColletCalibrationRecord record = data.GetRecord(side, i);
                bool hasMachineCenter;
                double machineX, machineY;
                bool machineValid = TryGetRecipeRotationCenter(host, side, i, out machineX, out machineY, out hasMachineCenter);
                string pickZText, placeZText;
                ResolvePickPlaceZTeachingText(host, side, i, out pickZText, out placeZText);
                gridResults.Rows.Add(
                    side + " C" + i,
                    side,
                    i,
                    record.OffsetX.ToString("F6"),
                    record.OffsetY.ToString("F6"),
                    record.ThetaOffset.ToString("F6"),
                    record.TZeroHomeOffset.ToString("F6"),
                    record.FinalPickerX.ToString("F6"),
                    record.FinalPickerY.ToString("F6"),
                    record.FinalPickerZ.ToString("F6"),
                    record.Valid ? "OK" : "-",
                    record.RotationCenterValid ? "OK" : "-",
                    record.RotationCenterValid ? record.RotationCenterPixelX.ToString("F3") : "-",
                    record.RotationCenterValid ? record.RotationCenterPixelY.ToString("F3") : "-",
                    (hasMachineCenter && machineValid) ? machineX.ToString("F6") : "-",
                    (hasMachineCenter && machineValid) ? machineY.ToString("F6") : "-",
                    pickZText,
                    placeZText);
            }
        }

        // 콜렛별 현재 Pick/Place Z 티칭값(DiePickPosition/DiePlacePosition[colletIndex])을 표시용으로 읽는다.
        private static void ResolvePickPlaceZTeachingText(Form1 host, VisionFocusPickerSide side, int colletNo,
            out string pickZText, out string placeZText)
        {
            pickZText = "-";
            placeZText = "-";
            try
            {
                if (host == null || host.Machine == null)
                    return;

                // 공정 Pick/Place Z가 실제로 사용하는 티칭은 콜렛별 Z축의 스칼라 PickPosition/PlacePosition이다.
                // (DiePick/DiePlacePosition 배열은 X/Y용이라 Z 항이 비어 있음)
                PickerAxis zAxis = ResolvePickerZAxis(colletNo);
                string pickName = "PickPosition";
                string placeName = "PlacePosition";
                if (side == VisionFocusPickerSide.Front)
                {
                    if (host.Machine.PickerFrontUnit == null)
                        return;
                    pickZText = host.Machine.PickerFrontUnit.GetPickerTeachingPosition(zAxis, pickName).ToString("F6");
                    placeZText = host.Machine.PickerFrontUnit.GetPickerTeachingPosition(zAxis, placeName).ToString("F6");
                }
                else
                {
                    if (host.Machine.PickerRearUnit == null)
                        return;
                    pickZText = host.Machine.PickerRearUnit.GetPickerTeachingPosition(zAxis, pickName).ToString("F6");
                    placeZText = host.Machine.PickerRearUnit.GetPickerTeachingPosition(zAxis, placeName).ToString("F6");
                }
            }
            catch
            {
                pickZText = "-";
                placeZText = "-";
            }
        }

        // COC 회전 중심 기계 좌표(mm)는 Recipe(ColletRotationCenterX/Y/Valid)에 저장된다. side/collet 기준으로 읽어온다.
        private bool TryGetRecipeRotationCenter(Form1 host, VisionFocusPickerSide side, int colletNo,
            out double machineX, out double machineY, out bool hasCenter)
        {
            machineX = 0.0;
            machineY = 0.0;
            hasCenter = false;
            try
            {
                if (host == null || host.Machine == null)
                    return false;

                int index = Math.Max(0, Math.Min(3, colletNo - 1));
                double[] centerX;
                double[] centerY;
                bool[] centerValid;
                if (side == VisionFocusPickerSide.Front)
                {
                    if (host.Machine.PickerFrontUnit == null || host.Machine.PickerFrontUnit.Recipe == null)
                        return false;
                    centerX = host.Machine.PickerFrontUnit.Recipe.ColletRotationCenterX;
                    centerY = host.Machine.PickerFrontUnit.Recipe.ColletRotationCenterY;
                    centerValid = host.Machine.PickerFrontUnit.Recipe.ColletRotationCenterValid;
                }
                else
                {
                    if (host.Machine.PickerRearUnit == null || host.Machine.PickerRearUnit.Recipe == null)
                        return false;
                    centerX = host.Machine.PickerRearUnit.Recipe.ColletRotationCenterX;
                    centerY = host.Machine.PickerRearUnit.Recipe.ColletRotationCenterY;
                    centerValid = host.Machine.PickerRearUnit.Recipe.ColletRotationCenterValid;
                }

                if (centerX == null || centerY == null || centerValid == null ||
                    index >= centerX.Length || index >= centerY.Length || index >= centerValid.Length)
                    return false;

                hasCenter = true;
                machineX = centerX[index];
                machineY = centerY[index];
                return centerValid[index];
            }
            catch
            {
                return false;
            }
        }

        private void AppendSaveHistory(string[] lines)
        {
            if (lstSaveHistory == null || lines == null || lines.Length == 0)
                return;

            string stamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            lstSaveHistory.BeginUpdate();
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    string prefix = i == 0 ? stamp + "  " : "          ";
                    lstSaveHistory.Items.Add(prefix + lines[i]);
                }

                while (lstSaveHistory.Items.Count > 200)
                    lstSaveHistory.Items.RemoveAt(0);

                if (lstSaveHistory.Items.Count > 0)
                    lstSaveHistory.TopIndex = lstSaveHistory.Items.Count - 1;

                lstSaveHistory.ClearSelected();
                lstSaveHistory.Refresh();
            }
            finally
            {
                lstSaveHistory.EndUpdate();
            }
        }

        private static void WriteSaveHistoryLog(string[] lines)
        {
            if (lines == null)
                return;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSaveHistory", lines[i]);
                    EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-SAVE-HISTORY", lines[i]);
                }
            }
        }

        private bool SaveSelectedColletZTeachingFromCurrentAxis(Form1 host, out string[] historyLines, out string message)
        {
            historyLines = new string[0];
            message = string.Empty;
            try
            {
                if (host == null || host.Machine == null)
                {
                    message = "장비가 준비되지 않아 Collet Z를 저장할 수 없습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                {
                    message = "현재 활성 Recipe가 없어 Collet Z를 저장할 수 없습니다.";
                    return false;
                }

                CDT320_Machine machine = host.Machine;
                PickerAxis zAxisKind = ResolvePickerZAxis(_colletNo);
                BaseAxis zAxis = ResolveSelectedPickerAxis(machine, zAxisKind);
                if (zAxis == null)
                {
                    message = "선택 Collet Z축을 찾을 수 없습니다. side=" + _side + ", colletNo=" + _colletNo + ", axis=" + zAxisKind;
                    return false;
                }

                double currentZ = zAxis.ActualPosition;
                double colletZOffset;
                double inspectionTeachingZ = ResolveColletInspectionTeachingZ(currentZ, out colletZOffset);
                string dieBottomPositionName = BuildIndexedPositionName("DieBottomPosition");
                string dieSidePositionName = BuildIndexedPositionName("DieSidePosition");

                double oldBottomTeachingZ = GetSelectedPickerTeachingPosition(machine, zAxisKind, "BottomPosition");
                double oldSideTeachingZ = GetSelectedPickerTeachingPosition(machine, zAxisKind, "SidePosition");
                double oldDieBottomTeachingZ = GetSelectedPickerTeachingPosition(machine, zAxisKind, dieBottomPositionName);
                double oldDieSideTeachingZ = GetSelectedPickerTeachingPosition(machine, zAxisKind, dieSidePositionName);

                ColletCalibrationData data = ResolveData(machine);
                ColletCalibrationRecord record = data.GetRecord(_side, _colletNo);
                if (record == null)
                {
                    message = "선택한 Collet Calibration 저장 Record를 찾을 수 없습니다. side=" + _side + ", colletNo=" + _colletNo;
                    return false;
                }

                double oldFinalZ = record.FinalPickerZ;
                SetSelectedPickerTeachingPosition(machine, zAxisKind, "BottomPosition", inspectionTeachingZ);
                SetSelectedPickerTeachingPosition(machine, zAxisKind, "SidePosition", inspectionTeachingZ);
                SetSelectedPickerTeachingPosition(machine, zAxisKind, dieBottomPositionName, inspectionTeachingZ);
                SetSelectedPickerTeachingPosition(machine, zAxisKind, dieSidePositionName, inspectionTeachingZ);

                record.Side = _side;
                record.ColletNo = _colletNo;
                record.FinalPickerZ = inspectionTeachingZ;
                record.UpdatedAt = DateTime.Now;
                machine.VisionUnit.Config.CalibrationData.Touch("ColletSaveZ");

                bool recipeSaved = host.SaveMachineRecipe(host.ActiveRecipeName);
                host.SaveMachineSettings();

                string formula = "currentZ(" + currentZ.ToString("F6") +
                                 ")+Die(" + _colletDieCalThicknessMm.ToString("F6") +
                                 ")+Film(" + _colletFilmThicknessMm.ToString("F6") +
                                 ")+" + _colletType + "Offset(" + colletZOffset.ToString("F6") + ")";
                historyLines = new string[]
                {
                    "Z 저장: " + _side + " C" + _colletNo + ", axis=" + zAxisKind + ", recipe=" + host.ActiveRecipeName + ", recipeSaved=" + recipeSaved,
                    "Bottom Z: " + oldBottomTeachingZ.ToString("F6") + " -> " + inspectionTeachingZ.ToString("F6") + ", formula=" + formula,
                    "Side Z: " + oldSideTeachingZ.ToString("F6") + " -> " + inspectionTeachingZ.ToString("F6") + ", formula=Bottom Z와 동일",
                    dieBottomPositionName + ": " + oldDieBottomTeachingZ.ToString("F6") + " -> " + inspectionTeachingZ.ToString("F6"),
                    dieSidePositionName + ": " + oldDieSideTeachingZ.ToString("F6") + " -> " + inspectionTeachingZ.ToString("F6"),
                    "FINAL Z: " + oldFinalZ.ToString("F6") + " -> " + inspectionTeachingZ.ToString("F6") + ", display=저장 검사 Z"
                };

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSaveZ",
                    "Collet Z 저장. side=" + _side +
                    ", colletNo=" + _colletNo +
                    ", axis=" + zAxisKind +
                    ", recipe=" + host.ActiveRecipeName +
                    ", currentZ=" + currentZ.ToString("F6") +
                    ", dieThickness=" + _colletDieCalThicknessMm.ToString("F6") +
                    ", filmThickness=" + _colletFilmThicknessMm.ToString("F6") +
                    ", colletType=" + _colletType +
                    ", colletOffset=" + colletZOffset.ToString("F6") +
                    ", inspectionTeachingZ=" + inspectionTeachingZ.ToString("F6") +
                    ", recipeSaved=" + recipeSaved);
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-SAVE-Z",
                    "Collet Z 저장. side=" + _side +
                    ", colletNo=" + _colletNo +
                    ", axis=" + zAxisKind +
                    ", currentZ=" + currentZ.ToString("F6") +
                    ", finalZ=" + inspectionTeachingZ.ToString("F6") +
                    ", recipeSaved=" + recipeSaved);

                message = recipeSaved
                    ? "Collet Z 저장 완료. finalZ=" + inspectionTeachingZ.ToString("F6")
                    : "Collet Z는 메모리에 반영됐지만 Recipe 저장에 실패했습니다. Alarm/Event Log를 확인하세요.";
                return recipeSaved;
            }
            catch (Exception ex)
            {
                message = "Collet Z 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-SAVE-Z", message);
                return false;
            }
        }

        private ColletCalibrationData ResolveData(CDT320_Machine machine)
        {
            machine.VisionUnit.Config.EnsureCalibrationObjects();
            machine.VisionUnit.Config.CalibrationData.Collet.EnsureObjects();
            return machine.VisionUnit.Config.CalibrationData.Collet;
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
                    reason = "현재 알람 상태입니다. 알람 해제 후 Collet Calibration을 실행하세요.";
                    return false;
                }

                if (host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않았습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.AutoRunning)
                {
                    reason = "자동 운전 중에는 Collet Calibration을 실행할 수 없습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.ManualRunning || host.Controller.IsManualBusy)
                {
                    reason = "다른 수동 동작이 실행 중입니다. 완료 후 다시 실행하세요.";
                    return false;
                }

                if (host.Controller.IsSequenceRunning)
                {
                    reason = "시퀀스가 실행 중입니다. 완료 후 Collet Calibration을 실행하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Collet Calibration 실행 조건 확인 실패: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        // 직전 Core 호출의 결과 메시지(async 전환으로 out 파라미터 대체).
        private string _coreResultMessage = string.Empty;

        private async Task SaveCurrentBottomTeachingPositionAsync()
        {
            if (_busy)
                return;

            // 현재 축 위치로 티칭을 덮어쓰는 조작이므로 다른 수동/자동 동작과 동일하게 실행 조건을 확인한다.
            string gateReason;
            if (!CanRunManualCalibration(out gateReason))
            {
                lblStatus.Text = gateReason;
                QMC.Common.MessageDialog.Show(this, gateReason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                await SaveBottomTeachingCoreAsync(true).ConfigureAwait(true);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        // Save Bottom 저장 로직 본체. 수동 버튼(interactive=true)과 BATCH 자동 경로(interactive=false)가 공유한다.
        // 자동 경로에서는 확인 MessageBox를 띄우지 않고 결과를 _coreResultMessage/로그로만 남긴다. _busy는 호출자가 관리한다.
        // 파일 저장(Recipe/Settings 직렬화)은 백그라운드 스레드에서 수행해 UI 프리즈를 막는다.
        private async Task<bool> SaveBottomTeachingCoreAsync(bool interactive)
        {
            string message = string.Empty;
            try
            {
                string editReason;
                if (!CommitSettingGridEdits(out editReason))
                {
                    message = editReason;
                    lblStatus.Text = editReason;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, editReason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null)
                {
                    message = reason;
                    lblStatus.Text = reason;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                {
                    message = "현재 활성 Recipe가 없어 Bottom 검사 티칭 위치를 저장할 수 없습니다.";
                    lblStatus.Text = message;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                CDT320_Machine machine = host.Machine;
                int colletIndex = NormalizeColletIndex(_colletNo);
                if (colletIndex != 3)
                {
                    message = "SAVE BOTTOM POS는 기준 Collet 4번에서만 사용할 수 있습니다. side=" + _side + ", colletNo=" + _colletNo;
                    lblStatus.Text = message;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                PickerAxis zAxisKind = ResolvePickerZAxis(_colletNo);
                PickerAxis tAxisKind = ResolvePickerTAxis(_colletNo);
                BaseAxis xAxis = ResolveSelectedPickerAxis(machine, PickerAxis.PickerX);
                BaseAxis yAxis = ResolveSelectedPickerAxis(machine, PickerAxis.PickerY);
                BaseAxis zAxis = ResolveSelectedPickerAxis(machine, zAxisKind);
                BaseAxis tAxis = ResolveSelectedPickerAxis(machine, tAxisKind);
                if (xAxis == null || yAxis == null || zAxis == null || tAxis == null)
                {
                    message = "선택 Picker 축을 찾을 수 없습니다. side=" + _side + ", colletNo=" + _colletNo;
                    lblStatus.Text = message;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                double actualX = xAxis.ActualPosition;
                double actualY = yAxis.ActualPosition;
                double actualZ = zAxis.ActualPosition;
                double actualT = tAxis.ActualPosition;
                double colletZOffset = 0.0;
                double inspectionTeachingZ = ResolveColletInspectionTeachingZ(actualZ, out colletZOffset);
                double pitchOffsetX = ResolveBottomPitchXOffset(machine, colletIndex);
                double bottomTeachingX = actualX - pitchOffsetX;
                double pickerPitchX = ResolvePickerPitchXMagnitude(machine);
                double bottomPicker1X = bottomTeachingX + (pickerPitchX * 3.0);
                double sideTeachingX = bottomPicker1X + pickerPitchX;
                double baseBottomT = GetSelectedPickerTeachingPosition(machine, tAxisKind, "BottomPosition");
                double activeTPcHomeOffset = ResolvePickerTPcHomeOffset(tAxis);
                double tZeroResidual = actualT - baseBottomT;
                double tZeroHomeOffset = activeTPcHomeOffset + tZeroResidual;
                ColletCalibrationData data = ResolveData(machine);
                ColletCalibrationRecord record = data.GetRecord(_side, _colletNo);
                if (record == null)
                {
                    message = "선택한 Collet Calibration 저장 Record를 찾을 수 없습니다. side=" + _side + ", colletNo=" + _colletNo;
                    lblStatus.Text = message;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                double oldBottomTeachingX = GetSelectedPickerTeachingPosition(machine, PickerAxis.PickerX, "BottomPosition");
                double oldBottomTeachingY = GetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, "BottomPosition");
                double oldBottomTeachingZ = GetSelectedPickerTeachingPosition(machine, zAxisKind, "BottomPosition");
                double oldSideTeachingX = GetSelectedPickerTeachingPosition(machine, PickerAxis.PickerX, "SidePosition");
                double oldSideTeachingY = GetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, "SidePosition");
                double oldSideTeachingZ = GetSelectedPickerTeachingPosition(machine, zAxisKind, "SidePosition");
                double oldTZeroHomeOffset = record.TZeroHomeOffset;

                // 현재 기준: 4번 기준 Bottom 저장은 기존 OK 위치와 크게 다르면 오조작으로 보고 차단한다.
                string suspiciousReason;
                if (IsSuspiciousBottomTeachingPosition(record, actualX, actualY, actualZ, out suspiciousReason))
                {
                    message = suspiciousReason;
                    lblStatus.Text = suspiciousReason;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, suspiciousReason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                string confirmMessage =
                    "현재 위치를 Bottom 검사 티칭 위치로 저장하시겠습니까?\r\n" +
                    "Side=" + _side + ", Collet=" + _colletNo + "\r\n" +
                    "X Teaching=" + bottomTeachingX.ToString("F6") + " (actualX=" + actualX.ToString("F6") + ", pitch=" + pitchOffsetX.ToString("F6") + ")\r\n" +
                    "Y Teaching=" + actualY.ToString("F6") + "\r\n" +
                    "Side X Teaching=" + sideTeachingX.ToString("F6") + " (bottom#1=" + bottomPicker1X.ToString("F6") + " + pitch=" + pickerPitchX.ToString("F6") + ")\r\n" +
                    "Side Y Teaching=" + actualY.ToString("F6") + "\r\n" +
                    "Measured Best Z=" + actualZ.ToString("F6") + " (" + zAxisKind + ")\r\n" +
                    "Inspection Z=" + inspectionTeachingZ.ToString("F6") +
                    " = " + actualZ.ToString("F6") +
                    " + Die " + _colletDieCalThicknessMm.ToString("F6") +
                    " + Film " + _colletFilmThicknessMm.ToString("F6") +
                    " + " + _colletType + " Offset " + colletZOffset.ToString("F6") + "\r\n" +
                    "T Zero Offset=" + tZeroHomeOffset.ToString("F6") + " (" + tAxisKind + ")\r\n" +
                    "  Active PC Offset=" + activeTPcHomeOffset.ToString("F6") +
                    ", Residual=" + tZeroResidual.ToString("F6");
                if (interactive &&
                    QMC.Common.MessageDialog.Show(this, confirmMessage, "COLLET CAL", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    message = "사용자가 Bottom 검사 티칭 저장을 취소했습니다.";
                    lblStatus.Text = message;
                    return false;
                }

                SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerX, "BottomPosition", bottomTeachingX);
                SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, "BottomPosition", actualY);
                SetSelectedPickerTeachingPosition(machine, zAxisKind, "BottomPosition", inspectionTeachingZ);
                SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerX, "SidePosition", sideTeachingX);
                SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, "SidePosition", actualY);
                SetSelectedPickerTeachingPosition(machine, zAxisKind, "SidePosition", inspectionTeachingZ);
                if (colletIndex == 3)
                    SyncReferenceColletDieTeachingPositions(machine, bottomTeachingX, sideTeachingX, actualY, inspectionTeachingZ, zAxisKind);

                record.Side = _side;
                record.ColletNo = _colletNo;
                record.TZeroHomeOffset = tZeroHomeOffset;
                record.MeasuredTPosition = actualT;
                record.FinalPickerX = actualX;
                record.FinalPickerY = actualY;
                record.FinalPickerZ = inspectionTeachingZ;
                record.FinalPickerT = actualT;
                record.UpdatedAt = DateTime.Now;
                machine.VisionUnit.Config.CalibrationData.Touch("ColletBottomTeaching");
                string offsetSummary;
                bool offsetApplied = PickerVisionOffsetCalibrationService.TryApplyAvailableOffsets(machine, "ColletBottomTeaching", out offsetSummary);

                string colletZMessage;
                if (!SaveActiveProjectColletZ(host, true, out colletZMessage))
                {
                    message = colletZMessage;
                    lblStatus.Text = colletZMessage;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, colletZMessage, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                bool recipeSaved = await Task.Run(() => host.SaveMachineRecipe(host.ActiveRecipeName)).ConfigureAwait(true);
                await Task.Run(() => host.SaveMachineSettings()).ConfigureAwait(true);
                RefreshResultGrid();

                string[] saveHistoryLines = new string[]
                {
                    "저장: " + _side + " C" + _colletNo + ", recipe=" + host.ActiveRecipeName + ", recipeSaved=" + recipeSaved,
                    "Bottom X: " + oldBottomTeachingX.ToString("F6") + " -> " + bottomTeachingX.ToString("F6") + ", formula=actualX(" + actualX.ToString("F6") + ")-pitch(" + pitchOffsetX.ToString("F6") + ")",
                    "Bottom Y: " + oldBottomTeachingY.ToString("F6") + " -> " + actualY.ToString("F6") + ", formula=actualY",
                    "Bottom Z: " + oldBottomTeachingZ.ToString("F6") + " -> " + inspectionTeachingZ.ToString("F6") + ", formula=currentZ(" + actualZ.ToString("F6") + ")+Die(" + _colletDieCalThicknessMm.ToString("F6") + ")+Film(" + _colletFilmThicknessMm.ToString("F6") + ")+" + _colletType + "Offset(" + colletZOffset.ToString("F6") + ")",
                    "Side X: " + oldSideTeachingX.ToString("F6") + " -> " + sideTeachingX.ToString("F6") + ", formula=Bottom#1X(" + bottomPicker1X.ToString("F6") + ")+pitch(" + pickerPitchX.ToString("F6") + ")",
                    "Side Y: " + oldSideTeachingY.ToString("F6") + " -> " + actualY.ToString("F6") + ", formula=actualY",
                    "Side Z: " + oldSideTeachingZ.ToString("F6") + " -> " + inspectionTeachingZ.ToString("F6") + ", formula=BottomZ와 동일",
                    "T ZERO: " + oldTZeroHomeOffset.ToString("F6") + " -> " + tZeroHomeOffset.ToString("F6") + ", formula=activePc(" + activeTPcHomeOffset.ToString("F6") + ")+(actualT(" + actualT.ToString("F6") + ")-baseBottomT(" + baseBottomT.ToString("F6") + "))",
                    "Final Actual: X=" + actualX.ToString("F6") + ", Y=" + actualY.ToString("F6") + ", BestZ=" + actualZ.ToString("F6") + ", T=" + actualT.ToString("F6")
                };
                AppendSaveHistory(saveHistoryLines);
                WriteSaveHistoryLog(saveHistoryLines);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalBottomTeach",
                    "Bottom 검사 티칭 위치 저장. side=" + _side +
                    ", colletNo=" + _colletNo +
                    ", recipe=" + host.ActiveRecipeName +
                    ", actual=(" + actualX.ToString("F6") + "," + actualY.ToString("F6") + "," + actualZ.ToString("F6") + "," + actualT.ToString("F6") + ")" +
                    ", bottomTeachingX=actualX-pitchOffset=" + actualX.ToString("F6") + "-" + pitchOffsetX.ToString("F6") + "=" + bottomTeachingX.ToString("F6") +
                    ", bottomTeachingY=" + actualY.ToString("F6") +
                    ", measuredBestZ=" + actualZ.ToString("F6") +
                    ", inspectionTeachingZ=currentZ+dieThickness+filmThickness+colletOffset=" + actualZ.ToString("F6") + "+" + _colletDieCalThicknessMm.ToString("F6") + "+" + _colletFilmThicknessMm.ToString("F6") + "+" + colletZOffset.ToString("F6") + "=" + inspectionTeachingZ.ToString("F6") +
                    ", colletType=" + _colletType +
                    ", referenceDieTeachingSync=" + (colletIndex == 3) +
                    ", visionToPickerOffsetApplied=" + offsetApplied +
                    ", visionToPickerOffsetSummary=" + offsetSummary +
                    ", activeTPcHomeOffset=" + activeTPcHomeOffset.ToString("F6") +
                    ", tZeroResidual=actualT-baseBottomT=" + actualT.ToString("F6") + "-" + baseBottomT.ToString("F6") + "=" + tZeroResidual.ToString("F6") +
                    ", tZeroHomeOffset=activePcOffset+residual=" + activeTPcHomeOffset.ToString("F6") + "+" + tZeroResidual.ToString("F6") + "=" + tZeroHomeOffset.ToString("F6") +
                    ", validUnchanged=" + record.Valid +
                    ", colletZ=" + colletZMessage +
                    ", recipeSaved=" + recipeSaved);

                message = recipeSaved
                    ? "Bottom 검사 티칭 위치를 저장했습니다. TZero=" + tZeroHomeOffset.ToString("F6") + " (Active=" + activeTPcHomeOffset.ToString("F6") + ", Residual=" + tZeroResidual.ToString("F6") + "), " + colletZMessage
                    : "Bottom 검사 티칭 값은 메모리에 반영됐지만 Recipe 저장에 실패했습니다. Alarm/Event Log를 확인하세요.";
                lblStatus.Text = message;
                return recipeSaved;
            }
            catch (Exception ex)
            {
                message = "Bottom 검사 티칭 위치 저장 실패: " + ex.Message;
                lblStatus.Text = message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-BOTTOM-TEACH", lblStatus.Text);
                if (interactive)
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                _coreResultMessage = message;
            }
        }

        private static bool IsSuspiciousBottomTeachingPosition(ColletCalibrationRecord record, double actualX, double actualY, double actualZ, out string reason)
        {
            reason = string.Empty;
            if (record == null || !record.Valid)
                return false;

            const double maxXDeltaMm = 20.0;
            const double maxYDeltaMm = 10.0;
            const double maxZDeltaMm = 5.0;
            const double zRaisedThresholdMm = -1.0;

            double xDelta = Math.Abs(actualX - record.FinalPickerX);
            double yDelta = Math.Abs(actualY - record.FinalPickerY);
            double zDelta = Math.Abs(actualZ - record.FinalPickerZ);
            bool zLooksRaised = actualZ > zRaisedThresholdMm && record.FinalPickerZ < zRaisedThresholdMm;

            if (!zLooksRaised && xDelta <= maxXDeltaMm && yDelta <= maxYDeltaMm && zDelta <= maxZDeltaMm)
                return false;

            reason = "SAVE BOTTOM POS 차단: 현재 축 위치가 기존 Collet OK 위치와 너무 다릅니다. " +
                     "current=(" + actualX.ToString("F3") + "," + actualY.ToString("F3") + "," + actualZ.ToString("F3") + "), " +
                     "saved=(" + record.FinalPickerX.ToString("F3") + "," + record.FinalPickerY.ToString("F3") + "," + record.FinalPickerZ.ToString("F3") + "), " +
                     "delta=(" + xDelta.ToString("F3") + "," + yDelta.ToString("F3") + "," + zDelta.ToString("F3") + "), " +
                     "zRaised=" + zLooksRaised + ".";
            return true;
        }

        private async Task ApplySelectedTHomeOffsetAsync()
        {
            if (_busy)
                return;

            // T 좌표계(HomeOffset+엔코더 0점)를 바꾸는 조작이므로 다른 수동/자동 동작과 동일하게 실행 조건을 확인한다.
            string gateReason;
            if (!CanRunManualCalibration(out gateReason))
            {
                lblStatus.Text = gateReason;
                QMC.Common.MessageDialog.Show(this, gateReason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                await ApplyTHomeOffsetCoreAsync(true).ConfigureAwait(true);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        // Apply T HOME 로직 본체. 수동 버튼(interactive=true)과 BATCH 자동 경로(interactive=false)가 공유한다.
        // 자동 경로에서는 확인 MessageBox를 띄우지 않고, 유효한(Valid) 레코드일 때만 적용한다. _busy는 호출자가 관리한다.
        // 파일 저장(AxisStore/Settings 직렬화)은 백그라운드 스레드에서 수행해 UI 프리즈를 막는다.
        private async Task<bool> ApplyTHomeOffsetCoreAsync(bool interactive)
        {
            string message = string.Empty;
            try
            {
                string editReason;
                if (!CommitSettingGridEdits(out editReason))
                {
                    message = editReason;
                    lblStatus.Text = editReason;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, editReason, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null)
                {
                    message = reason;
                    lblStatus.Text = reason;
                    return false;
                }

                CDT320_Machine machine = host.Machine;
                ColletCalibrationData data = ResolveData(machine);
                ColletCalibrationRecord record = data.GetRecord(_side, _colletNo);
                if (record == null || (!interactive && !record.Valid))
                {
                    message = "Apply T HOME 대상 Collet 레코드가 없거나 유효하지 않습니다. side=" + _side + ", colletNo=" + _colletNo;
                    lblStatus.Text = message;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                PickerAxis tAxisKind = ResolvePickerTAxis(_colletNo);
                BaseAxis tAxis = ResolveSelectedPickerAxis(machine, tAxisKind);
                if (tAxis == null || tAxis.Setup == null)
                {
                    message = "선택 T축 설정을 찾을 수 없습니다. side=" + _side + ", colletNo=" + _colletNo;
                    lblStatus.Text = message;
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                tAxis.UpdateStatus();
                if (tAxis.IsAlarm)
                {
                    message = "T축 알람 상태에서는 T HOME 적용 및 엔코더 0점 설정을 할 수 없습니다. axis=" + tAxis.Name + ", alarmCode=" + tAxis.AlarmCode;
                    lblStatus.Text = message;
                    EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-APPLY-T-HOME-AXIS-ALARM", lblStatus.Text);
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (tAxis.IsMoving)
                {
                    message = "T축 이동 중에는 T HOME 적용 및 엔코더 0점 설정을 할 수 없습니다. axis=" + tAxis.Name;
                    lblStatus.Text = message;
                    EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-APPLY-T-HOME-MOVING", lblStatus.Text);
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                double oldHomeOffset = tAxis.Setup.HomeOffset;
                double newHomeOffset = record.TZeroHomeOffset;
                double oldActualPosition = tAxis.ActualPosition;
                double oldCommandPosition = tAxis.CommandPosition;
                string confirmMessage =
                    "TZeroHomeOffset을 T축 PC Zero로 적용하고 현재 T축 엔코더를 0으로 설정할까요?\r\n" +
                    "Side=" + _side + ", Collet=" + _colletNo + "\r\n" +
                    "Axis=" + tAxis.Name + "\r\n" +
                    "Old Offset=" + oldHomeOffset.ToString("F6") + "\r\n" +
                    "New Offset=" + newHomeOffset.ToString("F6") + "\r\n" +
                    "Current Actual=" + oldActualPosition.ToString("F6") + "\r\n" +
                    "Current Command=" + oldCommandPosition.ToString("F6") + "\r\n" +
                    "축 이동 없이 현재 보드 Command/Actual 좌표를 0으로 프리셋합니다.\r\n" +
                    "다음 T Home 후에도 Offset 이동 및 0점 설정 기능은 유지됩니다.";
                if (interactive &&
                    QMC.Common.MessageDialog.Show(this, confirmMessage, "COLLET CAL", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    message = "사용자가 Apply T HOME을 취소했습니다.";
                    lblStatus.Text = message;
                    return false;
                }

                tAxis.Setup.HomeOffset = newHomeOffset;
                tAxis.SetPosition(0.0);
                tAxis.UpdateStatus();
                double zeroTolerance = tAxis.Config != null && tAxis.Config.InPositionTolerance > 0.0
                    ? tAxis.Config.InPositionTolerance
                    : 0.001;
                if (Math.Abs(tAxis.ActualPosition) > zeroTolerance || Math.Abs(tAxis.CommandPosition) > zeroTolerance)
                {
                    tAxis.Setup.HomeOffset = oldHomeOffset;
                    message = "T축 엔코더 0점 설정 확인 실패. axis=" + tAxis.Name +
                                     ", actual=" + tAxis.ActualPosition.ToString("F6") +
                                     ", command=" + tAxis.CommandPosition.ToString("F6") +
                                     ", tolerance=" + zeroTolerance.ToString("F6");
                    lblStatus.Text = message;
                    EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-APPLY-T-HOME-ZERO-VERIFY", lblStatus.Text);
                    if (interactive)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                await Task.Run(() =>
                {
                    AjinFactory.AxisManager.Save(MotionAxisStore.DefaultPath);
                    host.SaveMachineSettings();
                }).ConfigureAwait(true);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalApplyTHome",
                    "T Absolute PC Zero Offset 적용 및 현재 T축 엔코더 0점 설정. side=" + _side +
                    ", colletNo=" + _colletNo +
                    ", axis=" + tAxis.Name +
                    ", oldPcOffset=" + oldHomeOffset.ToString("F6") +
                    ", newPcOffset=" + newHomeOffset.ToString("F6") +
                    ", oldActual=" + oldActualPosition.ToString("F6") +
                    ", oldCommand=" + oldCommandPosition.ToString("F6") +
                    ", newActual=" + tAxis.ActualPosition.ToString("F6") +
                    ", newCommand=" + tAxis.CommandPosition.ToString("F6") +
                    ", boardWrite=True" +
                    ", zeroSet=0.000000" +
                    ", applyMode=PickerT MovePcOffsetAfterHomeThenZero" +
                    ", motionAxisStore=" + MotionAxisStore.DefaultPath);

                message = "T 절대 PC Zero 보정값 적용 및 현재 T축 엔코더 0점 설정을 완료했습니다. axis=" + tAxis.Name +
                                 ", Offset=" + newHomeOffset.ToString("F6") +
                                 ", Actual=" + tAxis.ActualPosition.ToString("F6") +
                                 ", Command=" + tAxis.CommandPosition.ToString("F6");
                lblStatus.Text = message;
                return true;
            }
            catch (Exception ex)
            {
                message = "T PC Zero 보정값 적용 또는 엔코더 0점 설정 실패: " + ex.Message;
                lblStatus.Text = message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-APPLY-T-HOME", lblStatus.Text);
                if (interactive)
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                _coreResultMessage = message;
            }
        }

        private static int NormalizeColletIndex(int colletNo)
        {
            if (colletNo < 1)
                return 0;
            if (colletNo > 4)
                return 3;
            return colletNo - 1;
        }

        private static PickerAxis ResolvePickerZAxis(int colletNo)
        {
            int index = NormalizeColletIndex(colletNo);
            if (index == 0) return PickerAxis.PickerZ0;
            if (index == 1) return PickerAxis.PickerZ1;
            if (index == 2) return PickerAxis.PickerZ2;
            return PickerAxis.PickerZ3;
        }

        private static PickerAxis ResolvePickerTAxis(int colletNo)
        {
            int index = NormalizeColletIndex(colletNo);
            if (index == 0) return PickerAxis.PickerT0;
            if (index == 1) return PickerAxis.PickerT1;
            if (index == 2) return PickerAxis.PickerT2;
            return PickerAxis.PickerT3;
        }

        private double ResolveBottomPitchXOffset(CDT320_Machine machine, int colletIndex)
        {
            double pitch = 0.0;
            if (_side == VisionFocusPickerSide.Front && machine != null && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Setup != null)
                pitch = machine.PickerFrontUnit.Setup.PickerPitchX;
            else if (_side == VisionFocusPickerSide.Rear && machine != null && machine.PickerRearUnit != null && machine.PickerRearUnit.Setup != null)
                pitch = machine.PickerRearUnit.Setup.PickerPitchX;

            return Math.Abs(pitch) * Math.Max(0, 3 - colletIndex);
        }

        private double ResolvePickerPitchXMagnitude(CDT320_Machine machine)
        {
            try
            {
                double pitch = 0.0;
                if (_side == VisionFocusPickerSide.Front && machine != null && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Setup != null)
                    pitch = machine.PickerFrontUnit.Setup.PickerPitchX;
                else if (_side == VisionFocusPickerSide.Rear && machine != null && machine.PickerRearUnit != null && machine.PickerRearUnit.Setup != null)
                    pitch = machine.PickerRearUnit.Setup.PickerPitchX;

                return Math.Abs(pitch);
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private double GetSelectedPickerTeachingPosition(CDT320_Machine machine, PickerAxis axis, string positionName)
        {
            if (_side == VisionFocusPickerSide.Front)
            {
                if (machine == null || machine.PickerFrontUnit == null)
                    throw new InvalidOperationException("FrontPickerUnit이 준비되지 않았습니다.");
                return machine.PickerFrontUnit.GetPickerTeachingPosition(axis, positionName);
            }

            if (machine == null || machine.PickerRearUnit == null)
                throw new InvalidOperationException("RearPickerUnit이 준비되지 않았습니다.");
            return machine.PickerRearUnit.GetPickerTeachingPosition(axis, positionName);
        }

        private void SetSelectedPickerTeachingPosition(CDT320_Machine machine, PickerAxis axis, string positionName, double position)
        {
            if (_side == VisionFocusPickerSide.Front)
            {
                if (machine == null || machine.PickerFrontUnit == null)
                    throw new InvalidOperationException("FrontPickerUnit이 준비되지 않았습니다.");
                machine.PickerFrontUnit.SetPickerAxisTeachingPosition(axis, positionName, position);
                return;
            }

            if (machine == null || machine.PickerRearUnit == null)
                throw new InvalidOperationException("RearPickerUnit이 준비되지 않았습니다.");
            machine.PickerRearUnit.SetPickerAxisTeachingPosition(axis, positionName, position);
        }

        private void SyncReferenceColletDieTeachingPositions(CDT320_Machine machine, double bottomX, double sideX, double pickerY, double bottomZ, PickerAxis zAxis)
        {
            // Reference collet defines common forward Y. Side #4 X starts at Bottom #1 X plus one picker pitch.
            SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerX, BuildIndexedPositionName("DieBottomPosition"), bottomX);
            SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerX, BuildIndexedPositionName("DieSidePosition"), sideX);
            SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, BuildIndexedPositionName("DiePickPosition"), pickerY);
            SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, BuildIndexedPositionName("DieBottomPosition"), pickerY);
            SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, BuildIndexedPositionName("DieSidePosition"), pickerY);
            SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, BuildIndexedPositionName("DiePlacePosition"), pickerY);
            SetSelectedPickerTeachingPosition(machine, zAxis, BuildIndexedPositionName("DieBottomPosition"), bottomZ);
            SetSelectedPickerTeachingPosition(machine, zAxis, BuildIndexedPositionName("DieSidePosition"), bottomZ);
        }

        private double ResolveColletInspectionTeachingZ(double measuredBestZ, out double colletOffset)
        {
            double dieThickness = Math.Max(0.0, _colletDieCalThicknessMm);
            double filmThickness = Math.Max(0.0, _colletFilmThicknessMm);
            colletOffset = _colletType == ColletShapeType.Rim ? _colletRimOffsetFromFlatMm : _colletFlatZOffsetMm;
            return measuredBestZ + dieThickness + filmThickness + colletOffset;
        }

        private string BuildIndexedPositionName(string positionArrayName)
        {
            return positionArrayName + "[" + NormalizeColletIndex(_colletNo) + "]";
        }

        private BaseAxis ResolveSelectedPickerAxis(CDT320_Machine machine, PickerAxis axis)
        {
            if (_side == VisionFocusPickerSide.Front)
                return machine != null && machine.PickerFrontUnit != null ? ResolveFrontPickerAxis(machine.PickerFrontUnit, axis) : null;

            return machine != null && machine.PickerRearUnit != null ? ResolveRearPickerAxis(machine.PickerRearUnit, axis) : null;
        }

        private static double ResolvePickerTPcHomeOffset(BaseAxis axis)
        {
            try
            {
                return axis != null && axis.Setup != null ? axis.Setup.HomeOffset : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private static BaseAxis ResolveFrontPickerAxis(PickerFrontUnit picker, PickerAxis axis)
        {
            if (picker == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerX: return picker.PickerX;
                case PickerAxis.PickerY: return picker.PickerY;
                case PickerAxis.PickerT0: return picker.PickerT0;
                case PickerAxis.PickerT1: return picker.PickerT1;
                case PickerAxis.PickerT2: return picker.PickerT2;
                case PickerAxis.PickerT3: return picker.PickerT3;
                case PickerAxis.PickerZ0: return picker.PickerZ0;
                case PickerAxis.PickerZ1: return picker.PickerZ1;
                case PickerAxis.PickerZ2: return picker.PickerZ2;
                case PickerAxis.PickerZ3: return picker.PickerZ3;
                default: return null;
            }
        }

        private static BaseAxis ResolveRearPickerAxis(PickerRearUnit picker, PickerAxis axis)
        {
            if (picker == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerX: return picker.PickerX;
                case PickerAxis.PickerY: return picker.PickerY;
                case PickerAxis.PickerT0: return picker.PickerT0;
                case PickerAxis.PickerT1: return picker.PickerT1;
                case PickerAxis.PickerT2: return picker.PickerT2;
                case PickerAxis.PickerT3: return picker.PickerT3;
                case PickerAxis.PickerZ0: return picker.PickerZ0;
                case PickerAxis.PickerZ1: return picker.PickerZ1;
                case PickerAxis.PickerZ2: return picker.PickerZ2;
                case PickerAxis.PickerZ3: return picker.PickerZ3;
                default: return null;
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
                "ColletCalibration:" + actionName + ":" + _side + ":" + _colletNo);
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

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStop",
                        "Collet Calibration 정지 요청. action=" + actionName +
                        ", side=" + _side +
                        ", colletNo=" + _colletNo);
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
                    lblStatus.Text = "현재 정지 요청할 Collet Calibration 동작이 없습니다.";
                    return;
                }

                request();
                lblStatus.Text = source + "으로 Collet Calibration 정지를 요청했습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Collet Calibration 정지 요청 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-STOP-REQUEST", lblStatus.Text);
            }
            finally
            {
                UpdateStopButtonEnabled();
            }
        }

        private async Task<ManualMoveResult> MoveSelectedColletZToFocusDefaultAsync(CDT320_Machine machine)
        {
            double targetZ;
            string reason;
            if (!TryResolveBottomColletFocusDefaultPosition(machine, out targetZ, out reason))
                throw new InvalidOperationException("Collet Z 전진 목표 위치를 찾을 수 없습니다. " + reason);

            PickerAxis zAxis = ResolvePickerZAxis(_colletNo);
            int result;
            if (_side == VisionFocusPickerSide.Front)
            {
                if (machine == null || machine.PickerFrontUnit == null)
                    return new ManualMoveResult { Result = -1, Target = targetZ };
                result = await machine.PickerFrontUnit.MovePickerAxis(
                    zAxis,
                    targetZ,
                    JogSpeedType.Custom,
                    _moveVelocity,
                    "ColletFocusDefault").ConfigureAwait(true);
            }
            else
            {
                if (machine == null || machine.PickerRearUnit == null)
                    return new ManualMoveResult { Result = -1, Target = targetZ };
                result = await machine.PickerRearUnit.MovePickerAxis(
                    zAxis,
                    targetZ,
                    JogSpeedType.Custom,
                    _moveVelocity,
                    "ColletFocusDefault").ConfigureAwait(true);
            }

            return new ManualMoveResult { Result = result, Target = targetZ };
        }

        // Front+Rear 픽커 Y를 모두 Avoid로 이동한다(P-Y AVOID 버튼).
        // 안전위치 이동은 SafeMovePercent(각 축 Default × %)를 적용한다. 미설정 시 기존 Custom 속도로 폴백.
        private async Task<int> MoveBothPickersYToAvoidAsync(CDT320_Machine machine)
        {
            if (machine == null)
                return -1;

            double safePercent = QMC.CDT320.Sequencing.Calibration.CalibrationSafeMoveMotion.ResolvePercent(machine);
            if (machine.PickerFrontUnit != null)
            {
                int frontResult = await machine.PickerFrontUnit.MovePickerAxisToTeachingPositionSafeMove(
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    safePercent,
                    _moveVelocity).ConfigureAwait(true);
                if (frontResult != 0)
                    return frontResult;
            }

            if (machine.PickerRearUnit != null)
            {
                int rearResult = await machine.PickerRearUnit.MovePickerAxisToTeachingPositionSafeMove(
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    safePercent,
                    _moveVelocity).ConfigureAwait(true);
                if (rearResult != 0)
                    return rearResult;
            }

            return 0;
        }

        // Front+Rear 픽커 Z(각 4축)를 모두 Avoid로 이동한다(Z-AVOID 버튼).
        // 안전위치 이동은 SafeMovePercent(각 축 Default × %)를 적용한다. 미설정 시 기존 Custom 속도로 폴백.
        private async Task<int> MoveBothPickersZToAvoidAsync(CDT320_Machine machine)
        {
            if (machine == null)
                return -1;

            double safePercent = QMC.CDT320.Sequencing.Calibration.CalibrationSafeMoveMotion.ResolvePercent(machine);
            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            if (machine.PickerFrontUnit != null)
            {
                foreach (PickerAxis axis in zAxes)
                {
                    int result = await machine.PickerFrontUnit.MovePickerAxisToTeachingPositionSafeMove(
                        axis, "AvoidPosition", safePercent, _moveVelocity).ConfigureAwait(true);
                    if (result != 0)
                        return result;
                }
            }

            if (machine.PickerRearUnit != null)
            {
                foreach (PickerAxis axis in zAxes)
                {
                    int result = await machine.PickerRearUnit.MovePickerAxisToTeachingPositionSafeMove(
                        axis, "AvoidPosition", safePercent, _moveVelocity).ConfigureAwait(true);
                    if (result != 0)
                        return result;
                }
            }

            return 0;
        }

        // Front+Rear 픽커 Z 전체가 Avoid인지 확인(P-Y AVOID 전제조건).
        private static bool AreBothPickersZInAvoidPosition(CDT320_Machine machine)
        {
            if (machine == null)
                return false;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            foreach (PickerAxis axis in zAxes)
            {
                if (machine.PickerFrontUnit != null && !machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(axis, "AvoidPosition"))
                    return false;
                if (machine.PickerRearUnit != null && !machine.PickerRearUnit.IsPickerAxisInTeachingPosition(axis, "AvoidPosition"))
                    return false;
            }

            return true;
        }

        private bool IsSelectedColletZInAvoidPosition(CDT320_Machine machine)
        {
            if (machine == null)
                return false;

            PickerAxis zAxis = ResolvePickerZAxis(_colletNo);
            if (_side == VisionFocusPickerSide.Front)
                return machine.PickerFrontUnit != null &&
                       machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition");

            return machine.PickerRearUnit != null &&
                   machine.PickerRearUnit.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition");
        }

        private bool TryResolveBottomColletFocusDefaultPosition(CDT320_Machine machine, out double position, out string reason)
        {
            position = 0.0;
            reason = string.Empty;
            if (machine == null || machine.VisionUnit == null || machine.VisionUnit.Config == null)
            {
                reason = "Vision Focus Cal 설정 객체가 없습니다.";
                return false;
            }

            VisionFocusCalibrationData focusData = machine.VisionUnit.Config.FocusCalibration;
            if (focusData == null)
            {
                reason = "FocusCalibration 설정이 없습니다.";
                return false;
            }

            focusData.EnsureObjects();
            VisionFocusPositionRecord record = focusData.GetColletRecord(_side, _colletNo);
            if (record == null || (!record.Valid && Math.Abs(record.DefaultPosition) <= 0.0000001))
            {
                reason = "recordValid=" + (record != null && record.Valid) +
                         ", default=" + (record != null ? record.DefaultPosition.ToString("F6") : "null") +
                         ", best=" + (record != null ? record.BestPosition.ToString("F6") : "null");
                return false;
            }

            position = record.DefaultPosition;
            return true;
        }

        private static RecipeProject LoadActiveProject(Form1 host)
        {
            RecipeProject project = null;
            if (host != null && !string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                project = RecipeStore.Load(host.ActiveRecipeName);
            if (project == null)
                project = RecipeStore.LoadLastOrDefault();
            if (project != null)
            {
                if (project.ColletZ == null)
                    project.ColletZ = new ColletZConfigSubset();
                project.ColletZ.Ensure();
            }

            return project;
        }

        private bool SaveActiveProjectColletZ(Form1 host, bool applyToMachineRecipe, out string message)
        {
            message = string.Empty;
            try
            {
                RecipeProject project = LoadActiveProject(host);
                if (project == null || string.IsNullOrWhiteSpace(project.FileName))
                {
                    message = "현재 활성 Recipe Project를 찾을 수 없어 Collet Z 설정을 저장할 수 없습니다.";
                    return false;
                }

                project.ColletZ.Ensure();
                _colletType = project.ColletZ.ColletType;
                project.ColletZ.DieCalThicknessMm = Math.Max(0.0, _colletDieCalThicknessMm);
                project.ColletZ.FilmThicknessMm = Math.Max(0.0, _colletFilmThicknessMm);
                project.ColletZ.FlatZOffsetMm = _colletFlatZOffsetMm;
                project.ColletZ.RimOffsetFromFlatMm = _colletRimOffsetFromFlatMm;

                if (!RecipeStore.Save(project))
                {
                    message = "Collet Z 설정 Project 저장 실패. project=" + project.FileName;
                    return false;
                }

                _colletDieCalThicknessMm = project.ColletZ.DieCalThicknessMm;
                _colletFilmThicknessMm = project.ColletZ.FilmThicknessMm;
                _colletFlatZOffsetMm = project.ColletZ.FlatZOffsetMm;
                _colletRimOffsetFromFlatMm = project.ColletZ.RimOffsetFromFlatMm;

                message = "Collet Z 설정 저장 완료. project=" + project.FileName +
                          ", type=" + project.ColletZ.ColletType +
                          ", dieThickness=" + project.ColletZ.DieCalThicknessMm.ToString("F6") +
                          ", filmThickness=" + project.ColletZ.FilmThicknessMm.ToString("F6") +
                          ", flatOffset=" + project.ColletZ.FlatZOffsetMm.ToString("F6") +
                          ", rimOffset=" + project.ColletZ.RimOffsetFromFlatMm.ToString("F6");
                return true;
            }
            catch (Exception ex)
            {
                message = "Collet Z 설정 저장/적용 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-Z-SAVE", message);
                return false;
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
                reason = "Main 화면을 찾을 수 없어 Collet Calibration을 실행할 수 없습니다.";
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
            btnStart.Enabled = enabled;
            btnCoc.Enabled = enabled;
            btnCocCenter.Enabled = enabled;
            btnSaveBottomTeaching.Enabled = enabled;
            btnApplyHomeOffset.Enabled = enabled;
            btnMoveZForward.Enabled = enabled;
            btnMoveYAvoid.Enabled = enabled;
            btnSeqStop.Enabled = _activeStopRequest != null;
            btnReload.Enabled = enabled;
            btnParameterSave.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnClose.Enabled = enabled;
            btnBatchStart.Enabled = enabled;
            batchFlow.Enabled = enabled;
        }

        private void UpdateStopButtonEnabled()
        {
            if (btnSeqStop != null)
                btnSeqStop.Enabled = _activeStopRequest != null;
        }

        private static SettingInfo CreateOption(SettingKey key, string name, string toolTip, string[] options)
        {
            return new SettingInfo { Key = key, Name = name, Unit = string.Empty, ToolTip = toolTip, Numeric = false, Integer = false, Options = options, ReadOnly = false };
        }

        private static SettingInfo CreateText(SettingKey key, string name, string toolTip)
        {
            return new SettingInfo { Key = key, Name = name, Unit = string.Empty, ToolTip = toolTip, Numeric = false, Integer = false, Options = null, ReadOnly = false };
        }

        private static SettingInfo CreateNumber(SettingKey key, string name, string unit, string toolTip, bool integer)
        {
            return new SettingInfo { Key = key, Name = name, Unit = unit, ToolTip = toolTip, Numeric = true, Integer = integer, Options = null, ReadOnly = false };
        }

        private static SettingInfo CreateReadOnly(SettingKey key, string name, string unit, string toolTip)
        {
            return new SettingInfo { Key = key, Name = name, Unit = unit, ToolTip = toolTip, Numeric = false, Integer = false, Options = null, ReadOnly = true };
        }
    }
}
