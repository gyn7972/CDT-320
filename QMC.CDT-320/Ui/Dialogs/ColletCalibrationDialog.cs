using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
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
            AutoFocus
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
        }

        private static readonly string[] SideOptions = { "Front", "Rear" };
        private static readonly string[] ColletOptions = { "1", "2", "3", "4" };
        private static readonly string[] BoolOptions = { "True", "False" };
        private static readonly string[] XyToleranceModeOptions = { "Diagonal", "Axis" };

        private bool _loading;
        private bool _busy;
        private VisionFocusPickerSide _side = VisionFocusPickerSide.Front;
        private int _colletNo = 1;
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
        private bool _autoFocus = true;
        private CancellationTokenSource _runCts;

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
                LoadSettingsToUi();
                RefreshResultGrid();
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
                new[] { btnCheck, btnSaveBottomTeaching, btnApplyHomeOffset, btnReload, btnClose },
                new[] { btnStart },
                new[] { btnSave });
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
                SettingInfo info = row.Tag as SettingInfo;
                if (info == null || info.Numeric)
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

            if (!info.Numeric)
            {
                gridSettings.BeginEdit(true);
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

        private void btnSaveBottomTeaching_Click(object sender, EventArgs e)
        {
            SaveCurrentBottomTeachingPosition();
        }

        private void btnApplyHomeOffset_Click(object sender, EventArgs e)
        {
            ApplySelectedTHomeOffset();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadSettingsToUi();
            RefreshResultGrid();
            lblStatus.Text = "Collet Calibration 설정과 저장값을 다시 불러왔습니다.";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettingsFromUi(true);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
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

                if (!SaveSettingsFromUi(false))
                    return;

                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.ProcessSequence,
                    "ColletCalibration:" + _side + ":" + _colletNo);
                runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
                _runCts = runCts;
                stopHandler = delegate
                {
                    try
                    {
                        CancellationTokenSource cts = _runCts;
                        if (cts != null && !cts.IsCancellationRequested)
                            cts.Cancel();

                        QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStop",
                            "메인 STOP 요청으로 Collet Calibration 정지 요청. side=" + _side +
                            ", colletNo=" + _colletNo);
                    }
                    catch
                    {
                    }
                };
                host.Controller.StopRequested += stopHandler;

                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new ColletCalibrationSequence(context, _side, _colletNo);
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

                if (runCts != null)
                    runCts.Dispose();

                if (actionScope != null)
                    actionScope.Dispose();

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
                _autoFocus = settings.RunAutoFocusAfterTheta;
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

        private bool SaveSettingsFromUi(bool showMessage)
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
                data.Settings.RunAutoFocusAfterTheta = _autoFocus;
                data.Settings.EnsureDefaults();
                _finder = data.Settings.BottomFinderName;
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
                    ", autoFocus=" + data.Settings.RunAutoFocusAfterTheta);
                RefreshSettingGrid();
                RefreshResultGrid();

                if (showMessage)
                    lblStatus.Text = "Collet Calibration 설정값을 저장했습니다.";

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
                AddSettingRow(CreateNumber(SettingKey.ThetaGain, "Theta Gain", "x", "Vision에서 측정한 Theta 보정량에 곱하는 이동 비율입니다. 1.0은 측정값만큼 그대로 보정합니다.", false), _thetaGain.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyTolerance, "XY Tol", "mm", "XY 중심 보정 완료 판정 거리입니다. XY Tol Mode가 Diagonal이면 대각 거리, Axis면 X/Y 각각의 절대값으로 판정합니다.", false), _xyToleranceMm.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.MaxXyIteration, "XY Retry", "ea", "XY 중심 보정을 반복할 최대 횟수입니다. 이 횟수 안에 XY Tol 안으로 들어오지 않으면 NG 처리합니다.", true), _maxXyIterations.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateNumber(SettingKey.XyGainX, "XY Gain X", "x", "Vision에서 측정한 X 방향 보정량에 곱하는 이동 비율입니다. 1.0은 측정값만큼 그대로 보정합니다.", false), _xyGainX.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyGainY, "XY Gain Y", "x", "Vision에서 측정한 Y 방향 보정량에 곱하는 이동 비율입니다. 1.0은 측정값만큼 그대로 보정합니다.", false), _xyGainY.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyFineMax, "XY Fine Max", "mm", "Z가 검사 위치에 내려온 상태에서 그대로 XY 미세 보정을 허용할 최대 이동량입니다. X/Y 이동량이 이 값 이하이면 FineAlign으로 움직이고, 초과하면 Z/Y Avoid 후 큰 이동으로 처리합니다.", false), _xyFineMaxMm.ToString("F6"));
                AddSettingRow(CreateOption(SettingKey.XyToleranceMode, "XY Tol Mode", "XY Tol 판정 방식입니다. Diagonal은 sqrt(X^2+Y^2) 거리로 보고, Axis는 |X|와 |Y|가 각각 Tol 이하인지 봅니다.", XyToleranceModeOptions), _useDiagonalXyTolerance ? "Diagonal" : "Axis");
                AddSettingRow(CreateNumber(SettingKey.ScoreThreshold, "Score Min", "score", "Vision 검출 Score 최소값입니다. 0이면 Score 기준을 사용하지 않고, 0보다 크면 Score가 이 값보다 낮을 때 NG 처리합니다.", false), _scoreThreshold.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.VisionTimeout, "Vision Timeout", "ms", "Vision PC Collet Finder 응답을 기다리는 시간입니다. 이 시간 안에 응답이 없으면 Timeout NG 처리합니다.", true), _visionTimeoutMs.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateOption(SettingKey.AutoFocus, "AutoFocus", "True이면 Bottom 위치 진입 후 저장된 Focus Cal 기준에서 AutoFocus를 수행하고 Best Z로 이동한 뒤 Collet 검출을 시작합니다. False이면 저장된 Focus Cal Default Z만 사용합니다.", BoolOptions), _autoFocus ? "True" : "False");
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
                row.Cells[colSettingValue.Index].ReadOnly = info.Numeric;
            }
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
                AddRecords(data, VisionFocusPickerSide.Front);
                AddRecords(data, VisionFocusPickerSide.Rear);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void AddRecords(ColletCalibrationData data, VisionFocusPickerSide side)
        {
            for (int i = 1; i <= 4; i++)
            {
                ColletCalibrationRecord record = data.GetRecord(side, i);
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
                    record.Valid ? "OK" : "-");
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

        private void SaveCurrentBottomTeachingPosition()
        {
            if (_busy)
                return;

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
                Form1 host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                if (string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                {
                    lblStatus.Text = "현재 활성 Recipe가 없어 Bottom 검사 티칭 위치를 저장할 수 없습니다.";
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CDT320_Machine machine = host.Machine;
                int colletIndex = NormalizeColletIndex(_colletNo);
                PickerAxis zAxisKind = ResolvePickerZAxis(_colletNo);
                PickerAxis tAxisKind = ResolvePickerTAxis(_colletNo);
                BaseAxis xAxis = ResolveSelectedPickerAxis(machine, PickerAxis.PickerX);
                BaseAxis yAxis = ResolveSelectedPickerAxis(machine, PickerAxis.PickerY);
                BaseAxis zAxis = ResolveSelectedPickerAxis(machine, zAxisKind);
                BaseAxis tAxis = ResolveSelectedPickerAxis(machine, tAxisKind);
                if (xAxis == null || yAxis == null || zAxis == null || tAxis == null)
                {
                    lblStatus.Text = "선택 Picker 축을 찾을 수 없습니다. side=" + _side + ", colletNo=" + _colletNo;
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double actualX = xAxis.ActualPosition;
                double actualY = yAxis.ActualPosition;
                double actualZ = zAxis.ActualPosition;
                double actualT = tAxis.ActualPosition;
                double pitchOffsetX = ResolveBottomPitchXOffset(machine, colletIndex);
                double bottomTeachingX = actualX - pitchOffsetX;
                double baseBottomT = GetSelectedPickerTeachingPosition(machine, tAxisKind, "BottomPosition");
                double activeTPcHomeOffset = ResolvePickerTPcHomeOffset(tAxis);
                double tZeroResidual = actualT - baseBottomT;
                double tZeroHomeOffset = activeTPcHomeOffset + tZeroResidual;

                string message =
                    "현재 위치를 Bottom 검사 티칭 위치로 저장하시겠습니까?\r\n" +
                    "Side=" + _side + ", Collet=" + _colletNo + "\r\n" +
                    "X Teaching=" + bottomTeachingX.ToString("F6") + " (actualX=" + actualX.ToString("F6") + ", pitch=" + pitchOffsetX.ToString("F6") + ")\r\n" +
                    "Y Teaching=" + actualY.ToString("F6") + "\r\n" +
                    "Z Teaching=" + actualZ.ToString("F6") + " (" + zAxisKind + ")\r\n" +
                    "T Zero Offset=" + tZeroHomeOffset.ToString("F6") + " (" + tAxisKind + ")\r\n" +
                    "  Active PC Offset=" + activeTPcHomeOffset.ToString("F6") +
                    ", Residual=" + tZeroResidual.ToString("F6");
                if (QMC.Common.MessageDialog.Show(this, message, "COLLET CAL", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerX, "BottomPosition", bottomTeachingX);
                SetSelectedPickerTeachingPosition(machine, PickerAxis.PickerY, "BottomPosition", actualY);
                SetSelectedPickerTeachingPosition(machine, zAxisKind, "BottomPosition", actualZ);

                ColletCalibrationData data = ResolveData(machine);
                ColletCalibrationRecord record = data.GetRecord(_side, _colletNo);
                record.Side = _side;
                record.ColletNo = _colletNo;
                record.TZeroHomeOffset = tZeroHomeOffset;
                record.MeasuredTPosition = actualT;
                record.FinalPickerX = actualX;
                record.FinalPickerY = actualY;
                record.FinalPickerZ = actualZ;
                record.FinalPickerT = actualT;
                record.UpdatedAt = DateTime.Now;
                machine.VisionUnit.Config.CalibrationData.Touch("ColletBottomTeaching");

                bool recipeSaved = host.SaveMachineRecipe(host.CurrentRecipeName);
                host.SaveMachineSettings();
                RefreshResultGrid();

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalBottomTeach",
                    "Bottom 검사 티칭 위치 저장. side=" + _side +
                    ", colletNo=" + _colletNo +
                    ", recipe=" + host.CurrentRecipeName +
                    ", actual=(" + actualX.ToString("F6") + "," + actualY.ToString("F6") + "," + actualZ.ToString("F6") + "," + actualT.ToString("F6") + ")" +
                    ", bottomTeachingX=actualX-pitchOffset=" + actualX.ToString("F6") + "-" + pitchOffsetX.ToString("F6") + "=" + bottomTeachingX.ToString("F6") +
                    ", bottomTeachingY=" + actualY.ToString("F6") +
                    ", bottomTeachingZ=" + actualZ.ToString("F6") +
                    ", activeTPcHomeOffset=" + activeTPcHomeOffset.ToString("F6") +
                    ", tZeroResidual=actualT-baseBottomT=" + actualT.ToString("F6") + "-" + baseBottomT.ToString("F6") + "=" + tZeroResidual.ToString("F6") +
                    ", tZeroHomeOffset=activePcOffset+residual=" + activeTPcHomeOffset.ToString("F6") + "+" + tZeroResidual.ToString("F6") + "=" + tZeroHomeOffset.ToString("F6") +
                    ", validUnchanged=" + record.Valid +
                    ", recipeSaved=" + recipeSaved);

                lblStatus.Text = recipeSaved
                    ? "Bottom 검사 티칭 위치를 저장했습니다. TZero=" + tZeroHomeOffset.ToString("F6") + " (Active=" + activeTPcHomeOffset.ToString("F6") + ", Residual=" + tZeroResidual.ToString("F6") + ")"
                    : "Bottom 검사 티칭 값은 메모리에 반영됐지만 Recipe 저장에 실패했습니다. Alarm/Event Log를 확인하세요.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Bottom 검사 티칭 위치 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-BOTTOM-TEACH", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private void ApplySelectedTHomeOffset()
        {
            if (_busy)
                return;

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
                Form1 host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                CDT320_Machine machine = host.Machine;
                ColletCalibrationData data = ResolveData(machine);
                ColletCalibrationRecord record = data.GetRecord(_side, _colletNo);
                PickerAxis tAxisKind = ResolvePickerTAxis(_colletNo);
                BaseAxis tAxis = ResolveSelectedPickerAxis(machine, tAxisKind);
                if (tAxis == null || tAxis.Setup == null)
                {
                    lblStatus.Text = "선택 T축 설정을 찾을 수 없습니다. side=" + _side + ", colletNo=" + _colletNo;
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double oldHomeOffset = tAxis.Setup.HomeOffset;
                double newHomeOffset = record.TZeroHomeOffset;
                string message =
                    "TZeroHomeOffset을 T축 PC Zero로 적용할까요?\r\n" +
                    "Side=" + _side + ", Collet=" + _colletNo + "\r\n" +
                    "Axis=" + tAxis.Name + "\r\n" +
                    "Old Offset=" + oldHomeOffset.ToString("F6") + "\r\n" +
                    "New Offset=" + newHomeOffset.ToString("F6") + "\r\n" +
                    "보드에는 쓰지 않습니다.\r\n" +
                    "다음 T Home 후 Offset 이동 및 0점 설정합니다.";
                if (QMC.Common.MessageDialog.Show(this, message, "COLLET CAL", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                tAxis.Setup.HomeOffset = newHomeOffset;
                AjinFactory.AxisManager.Save(MotionAxisStore.DefaultPath);
                host.SaveMachineSettings();

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalApplyTHome",
                    "T Absolute PC Zero Offset 적용. side=" + _side +
                    ", colletNo=" + _colletNo +
                    ", axis=" + tAxis.Name +
                    ", oldPcOffset=" + oldHomeOffset.ToString("F6") +
                    ", newPcOffset=" + newHomeOffset.ToString("F6") +
                    ", boardWrite=False" +
                    ", applyMode=PickerT MovePcOffsetAfterHomeThenZero" +
                    ", motionAxisStore=" + MotionAxisStore.DefaultPath);

                lblStatus.Text = "T 절대 PC Zero 보정값을 적용했습니다. axis=" + tAxis.Name +
                                 ", Offset=" + newHomeOffset.ToString("F6") +
                                 ", BoardWrite=False, Home 후 이동 뒤 0점 설정";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "T PC Zero 보정값 적용 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-APPLY-T-HOME", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
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
            btnSaveBottomTeaching.Enabled = enabled;
            btnApplyHomeOffset.Enabled = enabled;
            btnReload.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnClose.Enabled = enabled;
        }

        private static SettingInfo CreateOption(SettingKey key, string name, string toolTip, string[] options)
        {
            return new SettingInfo { Key = key, Name = name, Unit = string.Empty, ToolTip = toolTip, Numeric = false, Integer = false, Options = options };
        }

        private static SettingInfo CreateText(SettingKey key, string name, string toolTip)
        {
            return new SettingInfo { Key = key, Name = name, Unit = string.Empty, ToolTip = toolTip, Numeric = false, Integer = false, Options = null };
        }

        private static SettingInfo CreateNumber(SettingKey key, string name, string unit, string toolTip, bool integer)
        {
            return new SettingInfo { Key = key, Name = name, Unit = unit, ToolTip = toolTip, Numeric = true, Integer = integer, Options = null };
        }
    }
}
