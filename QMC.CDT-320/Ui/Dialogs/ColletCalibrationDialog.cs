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
using QMC.CDT_320.Ui.Security;
using QMC.Common.Alarms;
using QMC.Common.Logging;

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
        private string _finder = "COLLET";
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
                new[] { btnCheck, btnApplyHomeOffset, btnReload, btnClose },
                new[] { btnStart },
                new[] { btnSave });
        }

        private void gridSettings_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridSettings.IsCurrentCellDirty)
                gridSettings.CommitEdit(DataGridViewDataErrorContexts.Commit);
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

        private void btnApplyHomeOffset_Click(object sender, EventArgs e)
        {
            QMC.Common.MessageDialog.Show(
                this,
                "T Home Offset 실제 적용은 아직 축 설정에 쓰지 않습니다.\r\n현재 버튼은 저장된 TZeroHomeOffset 확인용으로 분리되어 있습니다.",
                "COLLET CAL",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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

                Form1 host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                SaveSettingsFromUi(false);
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new ColletCalibrationSequence(context, _side, _colletNo);

                lblStatus.Text = "Collet Calibration 실행 중입니다. Side=" + _side + ", Collet=" + _colletNo;
                int result = await sequence.RunAsync(CancellationToken.None, PickerSequenceOptions.Default()).ConfigureAwait(true);
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
            catch (Exception ex)
            {
                lblStatus.Text = "Collet Calibration 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-RUN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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

        private void SaveSettingsFromUi(bool showMessage)
        {
            try
            {
                string reason;
                Form1 host = ResolveHost(out reason);
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null)
                {
                    lblStatus.Text = reason;
                    return;
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
                RefreshResultGrid();

                if (showMessage)
                    lblStatus.Text = "Collet Calibration 설정값을 저장했습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Collet Calibration 설정 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CAL-SAVE", lblStatus.Text);
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
                AddSettingRow(CreateOption(SettingKey.Side, "Side", SideOptions), _side == VisionFocusPickerSide.Rear ? "Rear" : "Front");
                AddSettingRow(CreateOption(SettingKey.ColletNo, "Collet No", ColletOptions), _colletNo.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateText(SettingKey.Finder, "Finder"), _finder);
                AddSettingRow(CreateNumber(SettingKey.ThetaTolerance, "Theta Tol", "deg", false), _thetaToleranceDeg.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.MaxThetaIteration, "Theta Retry", "ea", true), _maxThetaIterations.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateNumber(SettingKey.ThetaGain, "Theta Gain", "x", false), _thetaGain.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyTolerance, "XY Tol", "mm", false), _xyToleranceMm.ToString("F6"));
                AddSettingRow(CreateNumber(SettingKey.MaxXyIteration, "XY Retry", "ea", true), _maxXyIterations.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateNumber(SettingKey.XyGainX, "XY Gain X", "x", false), _xyGainX.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyGainY, "XY Gain Y", "x", false), _xyGainY.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.XyFineMax, "XY Fine Max", "mm", false), _xyFineMaxMm.ToString("F6"));
                AddSettingRow(CreateOption(SettingKey.XyToleranceMode, "XY Tol Mode", XyToleranceModeOptions), _useDiagonalXyTolerance ? "Diagonal" : "Axis");
                AddSettingRow(CreateNumber(SettingKey.ScoreThreshold, "Score Min", "score", false), _scoreThreshold.ToString("F3"));
                AddSettingRow(CreateNumber(SettingKey.VisionTimeout, "Vision Timeout", "ms", true), _visionTimeoutMs.ToString(CultureInfo.InvariantCulture));
                AddSettingRow(CreateOption(SettingKey.AutoFocus, "AutoFocus", BoolOptions), _autoFocus ? "True" : "False");
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

        private void ApplyNumericSetting(SettingInfo info, string valueText)
        {
            double value;
            if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                double.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

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
            btnApplyHomeOffset.Enabled = enabled;
            btnReload.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnClose.Enabled = enabled;
        }

        private static SettingInfo CreateOption(SettingKey key, string name, string[] options)
        {
            return new SettingInfo { Key = key, Name = name, Unit = string.Empty, Numeric = false, Integer = false, Options = options };
        }

        private static SettingInfo CreateText(SettingKey key, string name)
        {
            return new SettingInfo { Key = key, Name = name, Unit = string.Empty, Numeric = false, Integer = false, Options = null };
        }

        private static SettingInfo CreateNumber(SettingKey key, string name, string unit, bool integer)
        {
            return new SettingInfo { Key = key, Name = name, Unit = unit, Numeric = true, Integer = integer, Options = null };
        }
    }
}
