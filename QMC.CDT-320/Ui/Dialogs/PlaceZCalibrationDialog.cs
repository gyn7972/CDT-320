using System;
using QMC.CDT_320.Ui.Localization;
using System.Collections.Generic;
using System.Drawing;
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
    public sealed partial class PlaceZCalibrationDialog : Form, ILocalizedView
    {
        private const string MoveSpeedKey = "Move Speed";
        private const string MoveAccKey = "Move Acc";
        private const string MoveDecKey = "Move Dec";
        private const string MoveTimeoutKey = "Move Timeout";
        private const string StartZKey = "Start Z";
        private const string CoarseSearchSpeedKey = "Coarse Search Speed";
        private const string CoarseSearchAccKey = "Coarse Search Acc";
        private const string CoarseSearchDecKey = "Coarse Search Dec";
        private const string FineSearchSpeedKey = "Fine Search Speed";
        private const string FineSearchAccKey = "Fine Search Acc";
        private const string FineSearchDecKey = "Fine Search Dec";
        private const string SearchStartOffsetKey = "Search Start Offset";
        private const string SearchMaxDistanceKey = "Search Max Distance";
        private const string BackOffDistanceKey = "BackOff Distance";
        private const string FilmThicknessKey = "Film Thickness";
        private const string DieThicknessKey = "Die Thickness";
        private const string PositionOffsetXKey = "Position Offset X";
        private const string PositionOffsetYKey = "Position Offset Y";
        private const string ContactOffsetKey = "Contact Offset";
        private const string VacuumOnDelayKey = "Vacuum On Delay";
        private const string VacuumReOnDelayKey = "Vacuum Re-On Delay";
        private const string BlowPulseTimeKey = "Blow Pulse Time";
        private const string BlowSettleTimeKey = "Blow Settle Time";
        private const string FlowOffConfirmTimeoutKey = "Flow Off Confirm Timeout";
        private const string FlowStableKey = "Flow Stable";
        private const string FlowPollIntervalKey = "Flow Poll Interval";
        private const string RepeatCountKey = "Repeat Count";
        private const string RepeatToleranceKey = "Repeat Tolerance";
        private const string MoveAvoidAfterScanKey = "Move Avoid After Scan";
        private const string FailIfFlowAlreadyOnKey = "Fail If Flow Already On";

        private bool _busy;
        private bool _loadedOnce;
        private bool _updatingBatchChecks;
        private bool _hasLastSuccessfulResult;
        private VisionFocusPickerSide _lastSuccessfulSide;
        private BinSide _lastSuccessfulOutputSide;
        private int _lastSuccessfulPickerNo;
        private double _lastSuccessfulSavedPlacePosition;
        private DateTime _lastSuccessfulResultUpdatedAt;
        private string _lastSuccessfulRecipeName;
        private CancellationTokenSource _runCts;
        private Action<string> _activeStopRequest;
        private PickerPlaceZCalibrationSequence _activeCalibrationSequence;
        private AutoCalibrationSafePositionSequence _activeSafePositionSequence;

        public static PlaceZCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "PlaceZCalibrationDialog",
                owner,
                () => new PlaceZCalibrationDialog());
        }

        private void InitializeLocalization()
        {
            Lang.BindKey(_headerLabel, "calibration.placez.headerLabel");
            Lang.BindKey(_batchGroup, "calibration.placez.batchGroup");
            Lang.BindKey(_chkBatchAll, "calibration.placez.chkBatchAll");
            Lang.BindKey(_btnBatchStart, "calibration.placez.btnBatchStart");
            Lang.BindKey(_sideLabel, "calibration.placez.sideLabel");
            Lang.BindKey(_outputLabel, "calibration.placez.outputLabel");
            Lang.BindKey(_pickerLabel, "calibration.placez.pickerLabel");
            Lang.BindKey(_settingsGroup, "calibration.placez.settingsGroup");
            Lang.BindKey(_settingsParameterColumn, "calibration.placez.settingsParameterColumn");
            Lang.BindKey(_settingsValueColumn, "calibration.placez.settingsValueColumn");
            Lang.BindKey(_settingsUnitColumn, "calibration.placez.settingsUnitColumn");
            Lang.BindKey(_btnParameterSave, "calibration.placez.btnParameterSave");
            Lang.BindKey(_resultGroup, "calibration.placez.resultGroup");
            Lang.BindKey(_resultItemColumn, "calibration.placez.resultItemColumn");
            Lang.BindKey(_resultSideColumn, "calibration.placez.resultSideColumn");
            Lang.BindKey(_resultOutputColumn, "calibration.placez.resultOutputColumn");
            Lang.BindKey(_resultPickerColumn, "calibration.placez.resultPickerColumn");
            Lang.BindKey(_resultOldPlaceColumn, "calibration.placez.resultOldPlaceColumn");
            Lang.BindKey(_resultStartZColumn, "calibration.placez.resultStartZColumn");
            Lang.BindKey(_resultFlowZColumn, "calibration.placez.resultFlowZColumn");
            Lang.BindKey(_resultDieColumn, "calibration.placez.resultDieColumn");
            Lang.BindKey(_resultFilmColumn, "calibration.placez.resultFilmColumn");
            Lang.BindKey(_resultSavedPlaceColumn, "calibration.placez.resultSavedPlaceColumn");
            Lang.BindKey(_resultValidColumn, "calibration.placez.resultValidColumn");
            Lang.BindKey(_status, "calibration.placez.status");
            Lang.BindKey(_btnCheck, "calibration.placez.btnCheck");
            Lang.BindKey(_btnMoveStart, "calibration.placez.btnMoveStart");
            Lang.BindKey(_btnStartScan, "calibration.placez.btnStartScan");
            Lang.BindKey(_btnMoveAvoid, "calibration.placez.btnMoveAvoid");
            Lang.BindKey(_btnVacOff, "calibration.placez.btnVacOff");
            Lang.BindKey(_btnSeqStop, "calibration.placez.btnSeqStop");
            Lang.BindKey(_btnReload, "calibration.placez.btnReload");
            Lang.BindKey(_btnSave, "calibration.placez.btnSave");
            Lang.BindKey(_btnClose, "calibration.placez.btnClose");
            Lang.BindKey(this, "calibration.placez.this");
            CalibrationDialogText.BindGrid(_settingsGrid);
            CalibrationDialogText.BindGrid(_resultGrid);
            CalibrationDialogText.BindCombo(_cmbSide);
            CalibrationDialogText.BindCombo(_cmbOutputSide);
            CalibrationDialogText.BindCombo(_cmbPickerNo);
        }

        public void ApplyLanguage()
        {
            // 언어 변경은 표시만 무효화하며 선택/입력/설정값을 다시 불러오지 않습니다.
            foreach (DataGridViewRow row in _settingsGrid.Rows)
                ApplySettingToolTip(row, GetSettingToolTip(Convert.ToString(row.Tag, CultureInfo.InvariantCulture)));
            Invalidate(true);
        }

        public PlaceZCalibrationDialog()
        {
            InitializeComponent();
            InitializeLocalization();
            CalibrationDialogGridBehavior.Apply(_settingsGrid, _resultGrid);
            CalibrationDialogButtonStyle.ApplyFooterButtons(
                new[] { _btnCheck, _btnMoveStart, _btnMoveAvoid, _btnVacOff, _btnSeqStop, _btnReload, _btnClose },
                new[] { _btnStartScan },
                new[] { _btnSave });
            CalibrationDialogButtonStyle.ApplyFooterButtons(null, null, new[] { _btnParameterSave });
            ApplyStopButtonStyle();
            UpdateResultSaveButtonEnabled();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!_loadedOnce)
            {
                _loadedOnce = true;
                LoadFromMachine();
                UpdateVacFlowButton();
                _flowStatusTimer.Start();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_flowStatusTimer != null)
                _flowStatusTimer.Stop();
            base.OnFormClosed(e);
        }

        private void BtnCheck_Click(object sender, EventArgs e)
        {
            CheckReady(true);
        }

        private async void BtnMoveStart_Click(object sender, EventArgs e)
        {
            await MoveScanStartAsync().ConfigureAwait(true);
        }

        private async void BtnStartScan_Click(object sender, EventArgs e)
        {
            await RunCalibrationAsync().ConfigureAwait(true);
        }

        private async void BtnMoveAvoid_Click(object sender, EventArgs e)
        {
            await MoveZAvoidAsync().ConfigureAwait(true);
        }

        private void BtnVacOff_Click(object sender, EventArgs e)
        {
            VacuumOff();
        }

        private void BtnSeqStop_Click(object sender, EventArgs e)
        {
            RequestActiveSequenceStop("창 STOP 버튼 요청");
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            LoadFromMachine();
        }

        private void BtnParameterSave_Click(object sender, EventArgs e)
        {
            SaveSettingsFromUi(true);
        }

        private void BtnSaveResult_Click(object sender, EventArgs e)
        {
            SaveLastSuccessfulResult();
        }

        private void BtnClose_Click(object sender, EventArgs e)
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
                        RequestRunCancelForClose("Place Z Calibration 창 종료(" + e.CloseReason + ")");
                        base.OnFormClosing(e);
                        return;
                    }

                    DialogResult result = QMC.Common.MessageDialog.Show(this,
                        Lang.T("calibration.message.m004"),
                        Lang.T("calibration.message.m005"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                        RequestRunCancelForClose("Place Z Calibration 창 닫기");

                    e.Cancel = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "PLACE-Z-CAL-CLOSE",
                    "Place Z Calibration 창 종료 확인 중 예외가 발생했습니다. error=" + ex.Message);
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
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "PLACE-Z-CAL-CLOSE-CANCEL",
                    reason + " 중 정지 요청 실패: " + ex.Message);
            }
        }

        private void PickerSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshResultGrid();
            UpdateVacFlowButton();
        }

        private void OutputSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshResultGrid();
        }

        private void FlowStatusTimer_Tick(object sender, EventArgs e)
        {
            UpdateVacFlowButton();
        }

        private void BatchAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingBatchChecks)
                return;

            SetAllBatchTargets(_chkBatchAll.Checked);
        }

        private void BatchTarget_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingBatchChecks)
                return;

            CheckBox[] checks = GetBatchTargetChecks();
            bool allChecked = true;
            for (int i = 0; i < checks.Length; i++)
            {
                if (!checks[i].Checked)
                {
                    allChecked = false;
                    break;
                }
            }

            _updatingBatchChecks = true;
            try
            {
                _chkBatchAll.Checked = allChecked;
            }
            finally
            {
                _updatingBatchChecks = false;
            }
        }

        private async void BtnBatchStart_Click(object sender, EventArgs e)
        {
            await RunBatchCalibrationAsync().ConfigureAwait(true);
        }

        private void SetAllBatchTargets(bool isChecked)
        {
            _updatingBatchChecks = true;
            try
            {
                CheckBox[] checks = GetBatchTargetChecks();
                for (int i = 0; i < checks.Length; i++)
                    checks[i].Checked = isChecked;
            }
            finally
            {
                _updatingBatchChecks = false;
            }
        }

        private CheckBox[] GetBatchTargetChecks()
        {
            return new[]
            {
                _chkBatchFront1,
                _chkBatchFront2,
                _chkBatchFront3,
                _chkBatchFront4,
                _chkBatchRear1,
                _chkBatchRear2,
                _chkBatchRear3,
                _chkBatchRear4
            };
        }

        private void LoadFromMachine()
        {
            try
            {
                PlaceZCalibrationData data = ResolveData();
                if (data == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s047");
                    return;
                }

                PlaceZCalibrationSettings settings = data.Settings;
                settings.EnsureDefaults();
                _settingsGrid.Rows.Clear();
                AddSetting(MoveSpeedKey, settings.Motion.MoveVelocity, "mm/s");
                AddSetting(MoveAccKey, settings.Motion.MoveAcceleration, "mm/s2");
                AddSetting(MoveDecKey, settings.Motion.MoveDeceleration, "mm/s2");
                AddSetting(MoveTimeoutKey, settings.Motion.MoveTimeoutMs, "ms");
                AddSetting(StartZKey, settings.StartZMm, "mm");
                AddSetting(CoarseSearchSpeedKey, settings.CoarseSearchVelocityMmPerSec, "mm/s");
                AddSetting(CoarseSearchAccKey, settings.CoarseSearchAccelerationMmPerSec2, "mm/s2");
                AddSetting(CoarseSearchDecKey, settings.CoarseSearchDecelerationMmPerSec2, "mm/s2");
                AddSetting(FineSearchSpeedKey, settings.FineSearchVelocityMmPerSec, "mm/s");
                AddSetting(FineSearchAccKey, settings.FineSearchAccelerationMmPerSec2, "mm/s2");
                AddSetting(FineSearchDecKey, settings.FineSearchDecelerationMmPerSec2, "mm/s2");
                AddSetting(SearchMaxDistanceKey, settings.SearchMaxDistanceMm, "mm");
                AddSetting(BackOffDistanceKey, settings.BackOffDistanceMm, "mm");
                AddSetting(DieThicknessKey, settings.DieThicknessMm, "mm");
                AddSetting(FilmThicknessKey, settings.FilmThicknessMm, "mm");
                AddSetting(PositionOffsetXKey, settings.PositionOffsetXmm, "mm");
                AddSetting(PositionOffsetYKey, settings.PositionOffsetYmm, "mm");
                AddSetting(VacuumOnDelayKey, settings.VacuumOnDelayMs, "ms");
                AddSetting(VacuumReOnDelayKey, settings.VacuumReOnDelayMs, "ms");
                AddSetting(BlowPulseTimeKey, settings.BlowPulseTimeMs, "ms");
                AddSetting(BlowSettleTimeKey, settings.BlowSettleTimeMs, "ms");
                AddSetting(FlowOffConfirmTimeoutKey, settings.FlowOffConfirmTimeoutMs, "ms");
                AddSetting(FlowStableKey, settings.FlowStableMs, "ms");
                AddSetting(FlowPollIntervalKey, settings.FlowPollIntervalMs, "ms");
                AddSetting(RepeatCountKey, settings.RepeatCount, "count");
                AddSetting(RepeatToleranceKey, settings.RepeatToleranceMm, "mm");
                AddSetting(MoveAvoidAfterScanKey, settings.MoveAvoidAfterScan ? "True" : "False", "");
                AddSetting(FailIfFlowAlreadyOnKey, settings.FailIfFlowAlreadyOn ? "True" : "False", "");
                RefreshResultGrid();
                Lang.BindFormat(_status, "calibration.status.s048");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s049", ex.Message);
            }
        }

        private void RefreshResultGrid()
        {
            try
            {
                _resultGrid.Rows.Clear();
                PlaceZCalibrationData data = ResolveData();
                if (data == null)
                    return;

                AddRecords(data, VisionFocusPickerSide.Front);
                AddRecords(data, VisionFocusPickerSide.Rear);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s050", ex.Message);
            }
        }

        private void AddRecords(PlaceZCalibrationData data, VisionFocusPickerSide side)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                PlaceZCalibrationRecord record = data.GetRecord(side, pickerNo);
                int row = _resultGrid.Rows.Add(
                    side + " P" + pickerNo,
                    side.ToString(),
                    record.OutputSide.ToString(),
                    pickerNo.ToString(CultureInfo.InvariantCulture),
                    record.OldPlacePosition.ToString("0.######", CultureInfo.InvariantCulture),
                    record.StartZMm.ToString("0.######", CultureInfo.InvariantCulture),
                    record.DetectedFlowPosition.ToString("0.######", CultureInfo.InvariantCulture),
                    record.DieThicknessMm.ToString("0.######", CultureInfo.InvariantCulture),
                    record.FilmThicknessMm.ToString("0.######", CultureInfo.InvariantCulture),
                    record.SavedPlacePosition.ToString("0.######", CultureInfo.InvariantCulture),
                    record.Valid ? "OK" : "-");

                if (side == ResolveSide() && record.OutputSide == ResolveOutputSide() && pickerNo == ResolvePickerNo())
                    _resultGrid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(210, 232, 255);
            }
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
                    return Lang.T("calibration.tip.t026");
                case CoarseSearchSpeedKey:
                    return Lang.T("calibration.tip.t027");
                case CoarseSearchAccKey:
                    return Lang.T("calibration.tip.t028");
                case CoarseSearchDecKey:
                    return Lang.T("calibration.tip.t029");
                case FineSearchSpeedKey:
                    return Lang.T("calibration.tip.t030");
                case MoveAccKey:
                    return Lang.T("calibration.tip.t031");
                case MoveDecKey:
                    return Lang.T("calibration.tip.t032");
                case FineSearchAccKey:
                    return Lang.T("calibration.tip.t033");
                case FineSearchDecKey:
                    return Lang.T("calibration.tip.t034");
                case MoveTimeoutKey:
                    return Lang.T("calibration.tip.t035");
                case StartZKey:
                    return Lang.T("calibration.tip.t036");
                case DieThicknessKey:
                    return Lang.T("calibration.tip.t037");
                case FilmThicknessKey:
                    return Lang.T("calibration.tip.t038");
                case PositionOffsetXKey:
                    return Lang.T("calibration.tip.t039");
                case PositionOffsetYKey:
                    return Lang.T("calibration.tip.t040");
                case SearchStartOffsetKey:
                    return Lang.T("calibration.tip.t041");
                case SearchMaxDistanceKey:
                    return Lang.T("calibration.tip.t042");
                case BackOffDistanceKey:
                    return Lang.T("calibration.tip.t043");
                case ContactOffsetKey:
                    return Lang.T("calibration.tip.t044");
                case VacuumOnDelayKey:
                    return Lang.T("calibration.tip.t045");
                case VacuumReOnDelayKey:
                    return Lang.T("calibration.tip.t046");
                case BlowPulseTimeKey:
                    return Lang.T("calibration.tip.t047");
                case BlowSettleTimeKey:
                    return Lang.T("calibration.tip.t048");
                case FlowOffConfirmTimeoutKey:
                    return Lang.T("calibration.tip.t049");
                case FlowStableKey:
                    return Lang.T("calibration.tip.t050");
                case FlowPollIntervalKey:
                    return Lang.T("calibration.tip.t051");
                case RepeatCountKey:
                    return Lang.T("calibration.tip.t052");
                case RepeatToleranceKey:
                    return Lang.T("calibration.tip.t053");
                case MoveAvoidAfterScanKey:
                    return Lang.T("calibration.tip.t054");
                case FailIfFlowAlreadyOnKey:
                    return Lang.T("calibration.tip.t055");
                default:
                    return string.Empty;
            }
        }

        private void SettingsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            string name = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Tag, CultureInfo.InvariantCulture);
            if (name == MoveAvoidAfterScanKey || name == FailIfFlowAlreadyOnKey)
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

        private bool SaveSettingsFromUi(bool showMessage)
        {
            try
            {
                PlaceZCalibrationData data = ResolveData();
                if (data == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s047");
                    return false;
                }

                PlaceZCalibrationSettings settings = data.Settings;
                if (settings.Motion == null)
                    settings.Motion = new CalibrationMotionSettings();
                settings.Motion.MoveVelocity = Math.Max(0.001, ReadDouble(MoveSpeedKey));
                settings.Motion.MoveAcceleration = Math.Max(0.001, ReadDouble(MoveAccKey));
                settings.Motion.MoveDeceleration = Math.Max(0.001, ReadDouble(MoveDecKey));
                settings.Motion.MoveTimeoutMs = Math.Max(100, ReadInt(MoveTimeoutKey, CalibrationMotionSettings.DefaultMoveTimeoutMs));
                settings.StartZMm = ReadDouble(StartZKey);
                settings.CoarseSearchVelocityMmPerSec = Math.Max(0.001, ReadDouble(CoarseSearchSpeedKey));
                settings.CoarseSearchAccelerationMmPerSec2 = Math.Max(0.001, ReadDouble(CoarseSearchAccKey));
                settings.CoarseSearchDecelerationMmPerSec2 = Math.Max(0.001, ReadDouble(CoarseSearchDecKey));
                settings.FineSearchVelocityMmPerSec = Math.Max(0.001, ReadDouble(FineSearchSpeedKey));
                settings.FineSearchAccelerationMmPerSec2 = Math.Max(0.001, ReadDouble(FineSearchAccKey));
                settings.FineSearchDecelerationMmPerSec2 = Math.Max(0.001, ReadDouble(FineSearchDecKey));
                settings.SearchMaxDistanceMm = Math.Max(0.001, ReadDouble(SearchMaxDistanceKey));
                settings.BackOffDistanceMm = Math.Max(0.001, ReadDouble(BackOffDistanceKey));
                settings.DieThicknessMm = Math.Max(0.0, ReadDouble(DieThicknessKey));
                settings.FilmThicknessMm = Math.Max(0.0, ReadDouble(FilmThicknessKey));
                settings.ContactOffsetMm = settings.DieThicknessMm;
                settings.PositionOffsetXmm = ReadDouble(PositionOffsetXKey);
                settings.PositionOffsetYmm = ReadDouble(PositionOffsetYKey);
                settings.VacuumOnDelayMs = Math.Max(0, ReadInt(VacuumOnDelayKey, 100));
                settings.VacuumReOnDelayMs = Math.Max(0, ReadInt(VacuumReOnDelayKey, 100));
                settings.BlowPulseTimeMs = Math.Max(0, ReadInt(BlowPulseTimeKey, 50));
                settings.BlowSettleTimeMs = Math.Max(0, ReadInt(BlowSettleTimeKey, 50));
                settings.FlowOffConfirmTimeoutMs = Math.Max(1, ReadInt(FlowOffConfirmTimeoutKey, 1000));
                settings.FlowStableMs = Math.Max(0, ReadInt(FlowStableKey, 30));
                settings.FlowPollIntervalMs = Math.Max(1, ReadInt(FlowPollIntervalKey, 5));
                settings.RepeatCount = Math.Max(1, ReadInt(RepeatCountKey, 2));
                settings.RepeatToleranceMm = Math.Max(0.0001, ReadDouble(RepeatToleranceKey));
                settings.MoveAvoidAfterScan = ReadBool(MoveAvoidAfterScanKey, true);
                settings.FailIfFlowAlreadyOn = ReadBool(FailIfFlowAlreadyOnKey, true);
                settings.EnsureDefaults();

                Form1 host = ResolveHost();
                if (host != null)
                    host.SaveMachineSettings();

                LoadFromMachine();
                if (showMessage)
                    Lang.BindFormat(_status, "calibration.status.s051");
                return true;
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s052", ex.Message);
                EventLogger.Write(EventKind.Alarm, "CAL", "PLACE-Z-CAL-SAVE-SETTING", _status.Text);
                return false;
            }
        }

        private void SaveLastSuccessfulResult()
        {
            try
            {
                if (!_hasLastSuccessfulResult)
                {
                    Lang.BindFormat(_status, "calibration.status.s053");
                    QMC.Common.MessageDialog.Show(
                        this,
                        _status.Text,
                        Lang.T("calibration.message.m005"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = ResolveHost();
                if (host == null || string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                {
                    Lang.BindFormat(_status, "calibration.status.s054");
                    return;
                }

                if (!string.Equals(host.ActiveRecipeName, _lastSuccessfulRecipeName, StringComparison.Ordinal))
                {
                    Lang.BindFormat(_status, "calibration.status.s055", _lastSuccessfulRecipeName, host.ActiveRecipeName);
                    QMC.Common.MessageDialog.Show(
                        this,
                        _status.Text,
                        Lang.T("calibration.message.m005"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                PlaceZCalibrationData data = ResolveData();
                PlaceZCalibrationRecord record = data != null
                    ? data.GetRecord(_lastSuccessfulSide, _lastSuccessfulPickerNo)
                    : null;
                if (record == null ||
                    !record.Valid ||
                    record.Side != _lastSuccessfulSide ||
                    record.PickerNo != _lastSuccessfulPickerNo ||
                    record.OutputSide != _lastSuccessfulOutputSide ||
                    record.UpdatedAt != _lastSuccessfulResultUpdatedAt ||
                    Math.Abs(record.SavedPlacePosition - _lastSuccessfulSavedPlacePosition) > 0.000001)
                {
                    Lang.BindFormat(_status, "calibration.status.s056", _lastSuccessfulSide, _lastSuccessfulOutputSide, _lastSuccessfulPickerNo, _lastSuccessfulSavedPlacePosition.ToString("F6"), (record != null
                                       ? record.OutputSide + "/" + record.SavedPlacePosition.ToString("F6")
                                       : "null"));
                    QMC.Common.MessageDialog.Show(
                        this,
                        _status.Text,
                        Lang.T("calibration.message.m005"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                bool recipeSaved = host.SaveMachineRecipe(_lastSuccessfulRecipeName);
                if (!recipeSaved)
                {
                    Lang.BindFormat(_status, "calibration.status.s057", _lastSuccessfulRecipeName, _lastSuccessfulSide, _lastSuccessfulOutputSide, _lastSuccessfulPickerNo);
                    QMC.Common.MessageDialog.Show(
                        this,
                        _status.Text,
                        Lang.T("calibration.message.m005"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                host.SaveMachineSettings();
                ClearLastSuccessfulResult();
                Lang.BindFormat(_status, "calibration.status.s058", _lastSuccessfulRecipeName, _lastSuccessfulSide, _lastSuccessfulOutputSide, _lastSuccessfulPickerNo, _lastSuccessfulSavedPlacePosition.ToString("F6"));
                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-SAVE-RESULT", _status.Text);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s059", ex.Message);
                EventLogger.Write(EventKind.Alarm, "CAL", "PLACE-Z-CAL-SAVE-RESULT-EX", _status.Text);
            }
        }

        private void RegisterLastSuccessfulResult(
            PlaceZCalibrationResult result,
            string recipeName)
        {
            PlaceZCalibrationData data = ResolveData();
            PlaceZCalibrationRecord record = data != null && result != null
                ? data.GetRecord(result.Side, result.PickerNo)
                : null;
            if (result == null ||
                !result.Success ||
                result.PickerNo < 1 ||
                result.PickerNo > 4 ||
                string.IsNullOrWhiteSpace(recipeName) ||
                record == null ||
                !record.Valid ||
                record.Side != result.Side ||
                record.OutputSide != result.OutputSide ||
                record.PickerNo != result.PickerNo ||
                Math.Abs(record.SavedPlacePosition - result.SavedPlacePosition) > 0.000001)
            {
                ClearLastSuccessfulResult();
                return;
            }

            _lastSuccessfulSide = result.Side;
            _lastSuccessfulOutputSide = result.OutputSide;
            _lastSuccessfulPickerNo = result.PickerNo;
            _lastSuccessfulSavedPlacePosition = result.SavedPlacePosition;
            _lastSuccessfulResultUpdatedAt = record.UpdatedAt;
            _lastSuccessfulRecipeName = recipeName;
            _hasLastSuccessfulResult = true;
            UpdateResultSaveButtonEnabled();
        }

        private void ClearLastSuccessfulResult()
        {
            _hasLastSuccessfulResult = false;
            _lastSuccessfulResultUpdatedAt = DateTime.MinValue;
            UpdateResultSaveButtonEnabled();
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
                        QMC.Common.MessageDialog.Show(this, reason, Lang.T("calibration.message.m005"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                Form1 host = ResolveHost();
                if (host == null || host.Machine == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s060");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                {
                    Lang.BindFormat(_status, "calibration.status.s061");
                    return false;
                }

                BaseAxis zAxis = ResolvePickerZAxisObject(host);
                if (zAxis == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s062");
                    return false;
                }

                if (!zAxis.IsServoOn || zAxis.IsAlarm)
                {
                    Lang.BindFormat(_status, "calibration.status.s063", (zAxis.IsServoOn ? "ON" : "OFF"), (zAxis.IsAlarm ? "ON" : "OFF"));
                    return false;
                }

                if (showOk)
                    Lang.BindFormat(_status, "calibration.status.s064");
                return true;
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s065", ex.Message);
                return false;
            }
        }

        private async Task MoveScanStartAsync()
        {
            await RunSequenceActionAsync(
                "MoveStart",
                "Scan Start 위치로 이동 중입니다.",
                async (sequence, token, options) => await sequence.MoveScanStartOnlyAsync(token, options).ConfigureAwait(false),
                false).ConfigureAwait(true);
        }

        private async Task MoveZAvoidAsync()
        {
            await RunSequenceActionAsync(
                "MoveAvoid",
                "선택 PickerZ를 Avoid 위치로 이동 중입니다.",
                async (sequence, token, options) => await sequence.MoveAvoidOnlyAsync(token, options).ConfigureAwait(false),
                false).ConfigureAwait(true);
        }

        private async Task RunCalibrationAsync()
        {
            await RunSequenceActionAsync(
                "StartScan",
                "PlaceZ Calibration Scan 실행 중입니다. 현재 위치 반복 테스트가 가능하면 이동 없이 Flow 감지 위치를 찾습니다.",
                async (sequence, token, options) => await sequence.RunCurrentPoseTestOrDefaultAsync(token, options).ConfigureAwait(false),
                true).ConfigureAwait(true);
        }

        private List<BatchTarget> BuildSelectedBatchTargets()
        {
            var targets = new List<BatchTarget>();
            AppendBatchTarget(targets, _chkBatchFront4, VisionFocusPickerSide.Front, 4);
            AppendBatchTarget(targets, _chkBatchFront3, VisionFocusPickerSide.Front, 3);
            AppendBatchTarget(targets, _chkBatchFront2, VisionFocusPickerSide.Front, 2);
            AppendBatchTarget(targets, _chkBatchFront1, VisionFocusPickerSide.Front, 1);
            AppendBatchTarget(targets, _chkBatchRear4, VisionFocusPickerSide.Rear, 4);
            AppendBatchTarget(targets, _chkBatchRear3, VisionFocusPickerSide.Rear, 3);
            AppendBatchTarget(targets, _chkBatchRear2, VisionFocusPickerSide.Rear, 2);
            AppendBatchTarget(targets, _chkBatchRear1, VisionFocusPickerSide.Rear, 1);
            return targets;
        }

        private static void AppendBatchTarget(
            ICollection<BatchTarget> targets,
            CheckBox checkBox,
            VisionFocusPickerSide side,
            int pickerNo)
        {
            if (checkBox != null && checkBox.Checked)
                targets.Add(new BatchTarget(side, pickerNo));
        }

        private async Task RunBatchCalibrationAsync()
        {
            if (_busy)
                return;

            List<BatchTarget> targets = BuildSelectedBatchTargets();
            if (targets.Count == 0)
            {
                Lang.BindFormat(_status, "calibration.status.s066");
                QMC.Common.MessageDialog.Show(
                    this,
                    _status.Text,
                    Lang.T("calibration.message.m005"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Form1 host = null;
            Action stopHandler = null;
            IDisposable actionScope = null;
            CancellationTokenSource runCts = null;
            int originalSideIndex = _cmbSide.SelectedIndex;
            int originalOutputIndex = _cmbOutputSide.SelectedIndex;
            int originalPickerIndex = _cmbPickerNo.SelectedIndex;
            BinSide batchOutputSide = ResolveOutputSide();

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                SelectBatchTarget(targets[0]);
                if (!SaveSettingsFromUi(false) || !CheckReady(false))
                    return;
                ClearLastSuccessfulResult();

                host = ResolveHost();
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                runCts = BeginManualCalibrationRun(
                    host,
                    "Batch-" + batchOutputSide,
                    null,
                    out actionScope,
                    out stopHandler);

                for (int index = 0; index < targets.Count; index++)
                {
                    runCts.Token.ThrowIfCancellationRequested();
                    BatchTarget target = targets[index];
                    SelectBatchTarget(target);

                    if (ResolveOutputSide() != batchOutputSide)
                        throw new InvalidOperationException("PlaceZ Batch 실행 중 Output Side가 변경되었습니다.");

                    PickerSequenceOptions options = PickerSequenceOptions.Default();
                    options.RunMode = SequenceRunMode.Manual;
                    options.StartMode = SequenceStartMode.Restart;
                    options.PickerNo = target.PickerNo;
                    options.RestrictToPickerNo = target.PickerNo;

                    Lang.BindFormat(_status, "calibration.status.s067", (index + 1), targets.Count, batchOutputSide, target.Side, target.PickerNo);

                    var sequence = new PickerPlaceZCalibrationSequence(
                        context,
                        target.Side,
                        target.PickerNo,
                        batchOutputSide);
                    _activeCalibrationSequence = sequence;
                    int result = await sequence.RunCurrentPoseTestOrDefaultAsync(
                        runCts.Token,
                        options).ConfigureAwait(true);
                    _activeCalibrationSequence = null;
                    RefreshResultGrid();

                    if (result != 0)
                    {
                        Lang.BindFormat(_status, "calibration.status.s068", batchOutputSide, target.Side, target.PickerNo, sequence.Result.Message);
                        QMC.Common.MessageDialog.Show(
                            this,
                            _status.Text,
                            Lang.T("calibration.message.m005"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    if (sequence.Result.OutputSide != batchOutputSide)
                    {
                        Lang.BindFormat(_status, "calibration.status.s069", batchOutputSide, sequence.Result.OutputSide, target.Side, target.PickerNo);
                        QMC.Common.MessageDialog.Show(
                            this,
                            _status.Text,
                            Lang.T("calibration.message.m005"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    RegisterLastSuccessfulResult(sequence.Result, host.ActiveRecipeName);
                    bool recipeSaved = host.SaveMachineRecipe(host.ActiveRecipeName);
                    host.SaveMachineSettings();
                    if (!recipeSaved)
                    {
                        Lang.BindFormat(_status, "calibration.status.s070", batchOutputSide, target.Side, target.PickerNo, host.ActiveRecipeName);
                        QMC.Common.MessageDialog.Show(
                            this,
                            _status.Text,
                            Lang.T("calibration.message.m005"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // ====================================================================
                    // [중복 안전 이동 제거 2026-08-06]  ★실장비 미검증 — 실장비에서 테스트 필요★
                    // 상세: docs/cal-safe-position-redundancy-2026-08-06.txt
                    //
                    // 기존 조건: 대상마다 무조건 AutoCalibrationSafePositionSequence 를 돌려
                    //   픽커 사이에서 Avoid 왕복이 두 번씩 났다(PickUpZ/VisionFocus 와 동일 문제).
                    //
                    // 위임이 성립하는 근거 — PickerPlaceZCalibrationSequence
                    //   PrepareSafeStartPositionAsync(:778) 가 다음 대상 시작 시
                    //   PickerZ 전체 Avoid / PickerY Avoid / PickerT 전체 Avoid /
                    //   상대 Picker Avoid / Input·Output 카메라 Avoid 를 모두 확보하고
                    //   OutputStage 를 공정 위치로 보낸다.
                    //   같은 파일 :780 주석: "시작 안전이동은 forceMove를 쓰지 않는다:
                    //   이미 Avoid(정지+무알람+톨러런스)면 확인만 하고 통과한다."
                    //
                    // ★마지막 대상 후에는 그대로 안전 복귀한다★
                    // ====================================================================
                    bool hasNextTarget = index < targets.Count - 1;
                    if (hasNextTarget)
                    {
                        Lang.BindFormat(_status, "calibration.status.s071", target.Side, target.PickerNo);
                        EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-BATCH-SAFE-SKIP",
                            "Batch 대상 사이 안전 Avoid 복귀를 생략합니다(다음 대상 준비 단계가 확보). " +
                            "output=" + batchOutputSide +
                            ", completed=" + target.Side + " P" + target.PickerNo +
                            ", next=" + targets[index + 1].Side + " P" + targets[index + 1].PickerNo);
                        continue;
                    }

                    Lang.BindFormat(_status, "calibration.status.s072", batchOutputSide, target.Side, target.PickerNo);
                    var safe = new AutoCalibrationSafePositionSequence(context, target.Side);
                    _activeSafePositionSequence = safe;
                    int safeResult = await safe.RunAsync(runCts.Token, options).ConfigureAwait(true);
                    _activeSafePositionSequence = null;
                    if (safeResult != 0)
                    {
                        Lang.BindFormat(_status, "calibration.status.s073", batchOutputSide, target.Side, target.PickerNo, safeResult);
                        QMC.Common.MessageDialog.Show(
                            this,
                            _status.Text,
                            Lang.T("calibration.message.m005"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }

                Lang.BindFormat(_status, "calibration.status.s074", batchOutputSide, targets.Count);
                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-BATCH-COMPLETE", _status.Text);
            }
            catch (OperationCanceledException)
            {
                Lang.BindFormat(_status, "calibration.status.s075");
                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-BATCH-STOP", _status.Text);
            }
            catch (SequenceStopException ex)
            {
                Lang.BindFormat(_status, "calibration.status.s076", ex.Message);
                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-BATCH-STOP", _status.Text);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s077", ex.Message);
                EventLogger.Write(EventKind.Alarm, "CAL", "PLACE-Z-CAL-BATCH-EX", _status.Text);
                QMC.Common.MessageDialog.Show(
                    this,
                    _status.Text,
                    Lang.T("calibration.message.m005"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _activeCalibrationSequence = null;
                _activeSafePositionSequence = null;
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _cmbSide.SelectedIndex = originalSideIndex;
                _cmbOutputSide.SelectedIndex = originalOutputIndex;
                _cmbPickerNo.SelectedIndex = originalPickerIndex;
                RefreshResultGrid();
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private void SelectBatchTarget(BatchTarget target)
        {
            _cmbSide.SelectedIndex = target.Side == VisionFocusPickerSide.Rear ? 1 : 0;
            _cmbPickerNo.SelectedItem = target.PickerNo.ToString(CultureInfo.InvariantCulture);
            RefreshResultGrid();
            UpdateVacFlowButton();
        }

        private async Task RunSequenceActionAsync(
            string actionName,
            string runningMessage,
            Func<PickerPlaceZCalibrationSequence, CancellationToken, PickerSequenceOptions, Task<int>> action,
            bool saveRecipeAfterSuccess)
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
                if (!SaveSettingsFromUi(false) || !CheckReady(false))
                    return;
                if (saveRecipeAfterSuccess)
                    ClearLastSuccessfulResult();

                host = ResolveHost();
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new PickerPlaceZCalibrationSequence(context, ResolveSide(), ResolvePickerNo(), ResolveOutputSide());
                _activeCalibrationSequence = sequence;
                runCts = BeginManualCalibrationRun(host, actionName, sequence, out actionScope, out stopHandler);
                PickerSequenceOptions options = PickerSequenceOptions.Default();
                options.RunMode = SequenceRunMode.Manual;
                options.StartMode = SequenceStartMode.Restart;
                options.PickerNo = ResolvePickerNo();
                options.RestrictToPickerNo = ResolvePickerNo();

                CalibrationDialogText.BindStatus(_status, runningMessage);
                int result = await action(sequence, runCts.Token, options).ConfigureAwait(true);
                _activeCalibrationSequence = null;
                LoadFromMachine();

                if (result != 0)
                {
                    Lang.BindFormat(_status, "calibration.status.s078", sequence.Result.Message);
                    QMC.Common.MessageDialog.Show(this, _status.Text, Lang.T("calibration.message.m005"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (saveRecipeAfterSuccess)
                {
                    bool recipeSaved = host.SaveMachineRecipe(host.ActiveRecipeName);
                    host.SaveMachineSettings();
                    RegisterLastSuccessfulResult(sequence.Result, host.ActiveRecipeName);
                    Lang.BindFormat(_status, "calibration.status.s079", sequence.Result.DetectedFlowPosition.ToString("F6"), sequence.Result.SavedPlacePosition.ToString("F6"), (recipeSaved ? "OK" : "NG"));
                }
                else
                {
                    CalibrationDialogText.BindStatus(_status, sequence.Result.Message);
                }
            }
            catch (OperationCanceledException)
            {
                Lang.BindFormat(_status, "calibration.status.s080");
                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-STOP", _status.Text);
            }
            catch (SequenceStopException ex)
            {
                Lang.BindFormat(_status, "calibration.status.s081", ex.Message);
                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-STOP", _status.Text);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s082", ex.Message);
                EventLogger.Write(EventKind.Alarm, "CAL", "PLACE-Z-CAL-RUN", _status.Text);
                QMC.Common.MessageDialog.Show(this, _status.Text, Lang.T("calibration.message.m005"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _activeCalibrationSequence = null;
                _activeSafePositionSequence = null;
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private void VacuumOff()
        {
            try
            {
                Form1 host = ResolveHost();
                if (host == null || host.Machine == null)
                    return;

                int pickerNo = ResolvePickerNo();
                if (ResolveSide() == VisionFocusPickerSide.Front && host.Machine.PickerFrontUnit != null)
                    host.Machine.PickerFrontUnit.SetPickerVacuum(pickerNo, false);
                if (ResolveSide() == VisionFocusPickerSide.Rear && host.Machine.PickerRearUnit != null)
                    host.Machine.PickerRearUnit.SetPickerVacuum(pickerNo, false);

                Lang.BindFormat(_status, "calibration.status.s083", ResolveSide(), pickerNo);
                UpdateVacFlowButton();
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s084", ex.Message);
            }
        }

        private void RequestActiveSequenceStop(string reason)
        {
            Action<string> request = _activeStopRequest;
            if (request == null)
            {
                Lang.BindFormat(_status, "calibration.status.s085");
                return;
            }

            request(reason);
            Lang.BindFormat(_status, "calibration.status.s086");
        }

        private CancellationTokenSource BeginManualCalibrationRun(
            Form1 host,
            string actionName,
            PickerPlaceZCalibrationSequence sequence,
            out IDisposable actionScope,
            out Action stopHandler)
        {
            if (host == null || host.Controller == null)
                throw new InvalidOperationException("MachineController가 준비되지 않았습니다.");

            actionScope = host.Controller.BeginManualActionScope(
                ManualMotionScopeKind.ProcessSequence,
                "PlaceZCalibration:" + actionName + ":" + ResolveSide() + ":" + ResolvePickerNo());
            CancellationTokenSource runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
            _runCts = runCts;
            _activeCalibrationSequence = sequence;
            _activeStopRequest = delegate(string reason)
            {
                try
                {
                    CancellationTokenSource cts = _runCts;
                    if (cts != null && !cts.IsCancellationRequested)
                        cts.Cancel();

                    PickerPlaceZCalibrationSequence activeCalibration = _activeCalibrationSequence;
                    if (activeCalibration != null)
                        activeCalibration.RequestImmediateStop(reason);

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "PlaceZCalStop",
                        reason + "으로 PlaceZ Calibration 정지 요청. action=" + actionName +
                        ", side=" + ResolveSide() +
                        ", pickerNo=" + ResolvePickerNo());
                }
                catch
                {
                }
            };
            stopHandler = delegate { _activeStopRequest("메인 STOP 요청"); };
            host.Controller.StopRequested += stopHandler;
            UpdateStopButtonEnabled();
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
            _activeStopRequest = null;
            _activeCalibrationSequence = null;
            _activeSafePositionSequence = null;
            UpdateStopButtonEnabled();

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
                    reason = "Main 화면을 찾을 수 없어 PlaceZ Calibration을 실행할 수 없습니다.";
                    return false;
                }

                if (AlarmManager.HasActive)
                {
                    reason = "현재 알람 상태입니다. 알람 해제 후 PlaceZ Calibration을 실행하세요.";
                    return false;
                }

                if (host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않았습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.AutoRunning)
                {
                    reason = "자동 운전 중에는 PlaceZ Calibration을 실행할 수 없습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.ManualRunning || host.Controller.IsManualBusy)
                {
                    reason = "다른 수동 동작이 실행 중입니다. 완료 후 다시 실행하세요.";
                    return false;
                }

                if (host.Controller.IsSequenceRunning)
                {
                    reason = "시퀀스가 실행 중입니다. 완료 후 PlaceZ Calibration을 실행하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "PlaceZ Calibration 실행 조건 확인 실패: " + ex.Message;
                return false;
            }
        }

        private PlaceZCalibrationData ResolveData()
        {
            Form1 host = ResolveHost();
            if (host == null || host.Machine == null || host.Machine.VisionUnit == null || host.Machine.VisionUnit.Config == null)
                return null;

            host.Machine.VisionUnit.Config.EnsureCalibrationObjects();
            host.Machine.VisionUnit.Config.CalibrationData.EnsureObjects();
            host.Machine.VisionUnit.Config.CalibrationData.PlaceZ.EnsureObjects();
            return host.Machine.VisionUnit.Config.CalibrationData.PlaceZ;
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

        private BaseAxis ResolvePickerZAxisObject(Form1 host)
        {
            if (host == null || host.Machine == null)
                return null;

            PickerAxis axis = CalibrationCoordinateService.ResolvePickerZAxis(ResolvePickerNo() - 1);
            if (ResolveSide() == VisionFocusPickerSide.Front &&
                host.Machine.PickerFrontUnit != null &&
                host.Machine.PickerFrontUnit.Axes.ContainsKey(axis))
                return host.Machine.PickerFrontUnit.Axes[axis];

            if (ResolveSide() == VisionFocusPickerSide.Rear &&
                host.Machine.PickerRearUnit != null &&
                host.Machine.PickerRearUnit.Axes.ContainsKey(axis))
                return host.Machine.PickerRearUnit.Axes[axis];

            return null;
        }

        private VisionFocusPickerSide ResolveSide()
        {
            return _cmbSide.SelectedIndex == 1 ? VisionFocusPickerSide.Rear : VisionFocusPickerSide.Front;
        }

        private BinSide ResolveOutputSide()
        {
            return _cmbOutputSide.SelectedIndex == 1 ? BinSide.Ng : BinSide.Good;
        }

        private int ResolvePickerNo()
        {
            int pickerNo;
            if (_cmbPickerNo.SelectedItem != null &&
                int.TryParse(_cmbPickerNo.SelectedItem.ToString(), out pickerNo))
                return Math.Max(1, Math.Min(4, pickerNo));

            return 1;
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
            _cmbSide.Enabled = enabled;
            _cmbOutputSide.Enabled = enabled;
            _cmbPickerNo.Enabled = enabled;
            _settingsGrid.Enabled = enabled;
            _chkBatchAll.Enabled = enabled;
            _chkBatchFront1.Enabled = enabled;
            _chkBatchFront2.Enabled = enabled;
            _chkBatchFront3.Enabled = enabled;
            _chkBatchFront4.Enabled = enabled;
            _chkBatchRear1.Enabled = enabled;
            _chkBatchRear2.Enabled = enabled;
            _chkBatchRear3.Enabled = enabled;
            _chkBatchRear4.Enabled = enabled;
            _btnBatchStart.Enabled = enabled;
            _btnCheck.Enabled = enabled;
            _btnMoveStart.Enabled = enabled;
            _btnStartScan.Enabled = enabled;
            _btnMoveAvoid.Enabled = enabled;
            _btnVacOff.Enabled = enabled;
            _btnReload.Enabled = enabled;
            _btnParameterSave.Enabled = enabled;
            _btnClose.Enabled = enabled;
            UpdateResultSaveButtonEnabled();
            UpdateStopButtonEnabled();
        }

        private void UpdateResultSaveButtonEnabled()
        {
            if (_btnSave == null)
                return;

            _btnSave.Enabled = !_busy && _hasLastSuccessfulResult;
        }

        private void UpdateStopButtonEnabled()
        {
            if (_btnSeqStop == null)
                return;

            _btnSeqStop.Enabled = _activeStopRequest != null;
            ApplyStopButtonStyle();
        }

        private void ApplyStopButtonStyle()
        {
            if (_btnSeqStop == null)
                return;

            _btnSeqStop.BackColor = _btnSeqStop.Enabled ? Color.FromArgb(192, 57, 43) : Color.FromArgb(180, 180, 180);
            _btnSeqStop.ForeColor = Color.White;
            _btnSeqStop.FlatStyle = FlatStyle.Flat;
            _btnSeqStop.FlatAppearance.BorderSize = 1;
            _btnSeqStop.FlatAppearance.BorderColor = Color.FromArgb(128, 128, 128);
        }

        private void UpdateVacFlowButton()
        {
            if (_btnVacOff == null || IsDisposed)
                return;

            bool flowOn;
            string reason;
            if (TryReadPickerFlow(out flowOn, out reason))
            {
                { if (flowOn) Lang.BindFormat(_btnVacOff, "calibration.status.s087"); else Lang.BindFormat(_btnVacOff, "calibration.status.s088"); }
                _btnVacOff.BackColor = flowOn ? Color.FromArgb(46, 160, 67) : Color.White;
                _btnVacOff.ForeColor = flowOn ? Color.White : Color.Black;
                _btnVacOff.FlatAppearance.BorderColor = flowOn ? Color.FromArgb(28, 120, 48) : Color.FromArgb(176, 176, 176);
                _btnVacOff.Tag = flowOn;
                return;
            }

            Lang.BindFormat(_btnVacOff, "calibration.status.s089");
            _btnVacOff.BackColor = Color.FromArgb(245, 245, 245);
            _btnVacOff.ForeColor = Color.Black;
            _btnVacOff.FlatAppearance.BorderColor = Color.FromArgb(176, 176, 176);
            _btnVacOff.Tag = reason;
        }

        private bool TryReadPickerFlow(out bool flowOn, out string reason)
        {
            flowOn = false;
            reason = string.Empty;
            try
            {
                Form1 host = ResolveHost();
                if (host == null || host.Machine == null)
                {
                    reason = "장비 연결 없음";
                    return false;
                }

                int pickerNo = ResolvePickerNo();
                if (ResolveSide() == VisionFocusPickerSide.Front)
                {
                    if (host.Machine.PickerFrontUnit == null)
                    {
                        reason = "Front Picker Unit 없음";
                        return false;
                    }

                    flowOn = host.Machine.PickerFrontUnit.IsPickerFlowDetected(pickerNo, true);
                    return true;
                }

                if (host.Machine.PickerRearUnit == null)
                {
                    reason = "Rear Picker Unit 없음";
                    return false;
                }

                flowOn = host.Machine.PickerRearUnit.IsPickerFlowDetected(pickerNo, true);
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
        }

        private sealed class BatchTarget
        {
            public BatchTarget(VisionFocusPickerSide side, int pickerNo)
            {
                Side = side;
                PickerNo = pickerNo;
            }

            public VisionFocusPickerSide Side { get; private set; }
            public int PickerNo { get; private set; }
        }
    }
}

