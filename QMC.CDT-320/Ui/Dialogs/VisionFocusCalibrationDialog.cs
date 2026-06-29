using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
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
            RepeatCount,
            MoveVelocity,
            MoveAcceleration,
            MoveDeceleration,
            SettleDelay,
            MotionTimeout,
            VisionTimeout,
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
            "Front Side 0deg",
            "Front Side 90deg",
            "Rear Side 0deg",
            "Rear Side 90deg"
        };

        private static readonly string[] SideOptions = { "Front", "Rear" };
        private static readonly string[] ColletOptions = { "1", "2", "3", "4" };
        private static readonly string[] BoolOptions = { "True", "False" };

        private bool _loading;
        private bool _busy;
        private VisionFocusScanKind _selectedKind = VisionFocusScanKind.BottomCollet;
        private VisionFocusPickerSide _selectedPickerSide = VisionFocusPickerSide.Front;
        private int _selectedPickerNo = 1;
        private double _defaultPosition;
        private double _minusRange = 0.2;
        private double _plusRange = 0.2;
        private double _step = 0.02;
        private int _repeatCount = 1;
        private double _moveVelocity = 30.0;
        private double _moveAcceleration = 300.0;
        private double _moveDeceleration = 300.0;
        private int _settleDelayMs = 50;
        private int _motionTimeoutMs = 5000;
        private int _visionTimeoutMs = 5000;
        private bool _returnToDefaultAfterScan = true;

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

                lblStatus.Text = "실행 가능한 상태입니다. 선택한 축의 티칭 위치가 Default Pos로 적용됩니다.";
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

        private async void btnStartScan_Click(object sender, EventArgs e)
        {
            await RunScanAsync().ConfigureAwait(true);
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

        private async Task RunMoveDefaultAsync()
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
                    QMC.Common.MessageDialog.Show(this, reason, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                VisionFocusScanRequest request = BuildRequest(false);
                var sequence = new VisionFocusScanSequence(host.Machine, request);
                int result = await sequence.MoveDefaultOnlyAsync(CancellationToken.None).ConfigureAwait(true);
                lblStatus.Text = result == 0
                    ? "Default Pos 이동 완료."
                    : "Default Pos 이동 실패. Alarm/Event Log를 확인하세요.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Default Pos 이동 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-MOVE-DEFAULT", lblStatus.Text);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunScanAsync()
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
                    QMC.Common.MessageDialog.Show(this, reason, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form1 host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                SaveSettingsFromUi(false);
                gridSamples.Rows.Clear();

                VisionFocusScanRequest request = BuildRequest(true);
                var sequence = new VisionFocusScanSequence(host.Machine, request);
                int result = await sequence.RunAsync(CancellationToken.None).ConfigureAwait(true);
                PopulateSamples(sequence.Result);
                RefreshSavedGrid();

                if (result != 0)
                {
                    lblStatus.Text = "Focus Scan 실패: " + sequence.Result.Message;
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                host.SaveMachineSettings();
                lblStatus.Text = "Focus Scan 완료. Best=" + sequence.Result.BestPosition.ToString("F4") +
                                 ", Score=" + sequence.Result.BestScore.ToString("F2") +
                                 ", Sample=" + sequence.Result.SampleCount;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Focus Scan 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-SCAN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION FOCUS CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
                RepeatCount = useUiRange ? _repeatCount : 1,
                MoveVelocity = _moveVelocity,
                MoveAcceleration = _moveAcceleration,
                MoveDeceleration = _moveDeceleration,
                SettleDelayMs = useUiRange ? _settleDelayMs : 0,
                MotionTimeoutMs = _motionTimeoutMs,
                VisionTimeoutMs = _visionTimeoutMs,
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
                _repeatCount = settings.RepeatCount;
                _moveVelocity = settings.MoveVelocity;
                _moveAcceleration = settings.MoveAcceleration;
                _moveDeceleration = settings.MoveDeceleration;
                _settleDelayMs = settings.SettleDelayMs;
                _motionTimeoutMs = settings.MotionTimeoutMs;
                _visionTimeoutMs = settings.VisionTimeoutMs;
                _returnToDefaultAfterScan = settings.ReturnToDefaultAfterScan;
                _defaultPosition = ResolveTeachingDefaultPosition(host.Machine);
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

                VisionFocusScanSettings settings = ResolveSettings(host.Machine, _selectedKind);
                settings.MinusRange = _minusRange;
                settings.PlusRange = _plusRange;
                settings.Step = _step;
                settings.RepeatCount = _repeatCount;
                settings.MoveVelocity = _moveVelocity;
                settings.MoveAcceleration = _moveAcceleration;
                settings.MoveDeceleration = _moveDeceleration;
                settings.SettleDelayMs = _settleDelayMs;
                settings.MotionTimeoutMs = _motionTimeoutMs;
                settings.VisionTimeoutMs = _visionTimeoutMs;
                settings.ReturnToDefaultAfterScan = _returnToDefaultAfterScan;

                VisionFocusPositionRecord record = ResolveSelectedRecord(host.Machine);
                if (record != null)
                    record.DefaultPosition = _defaultPosition;

                host.SaveMachineSettings();
                RefreshSavedGrid();
                if (showMessage)
                    lblStatus.Text = "Vision Focus Cal 설정값을 저장했습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Vision Focus Cal 설정 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-FOCUS-CAL-SAVE", lblStatus.Text);
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
                AddSettingRow(CreateOptionInfo(FocusSettingKey.PickerSide, "Picker Side", "Bottom Collet Focus에서 사용할 Front/Rear Picker를 선택합니다.", SideOptions), SideToText(_selectedPickerSide), _selectedKind == VisionFocusScanKind.BottomCollet);
                AddSettingRow(CreateOptionInfo(FocusSettingKey.ColletNo, "Collet No", "Bottom Collet Focus에서 측정할 Collet 번호입니다.", ColletOptions), _selectedPickerNo.ToString(CultureInfo.InvariantCulture), _selectedKind == VisionFocusScanKind.BottomCollet);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.DefaultPosition, "Default Pos (mm)", "mm", "Focus 기준 위치입니다. Bottom은 선택 Collet의 Bottom Z 티칭값, Side는 선택 Side 카메라 Y 티칭값을 불러옵니다. USE CURRENT로 현재 축 위치를 덮어쓸 수 있습니다.", false), FormatDouble(_defaultPosition), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MinusRange, "- Range (mm)", "mm", "Default Pos 기준 마이너스 방향으로 스캔할 거리입니다.", false), FormatDouble(_minusRange), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.PlusRange, "+ Range (mm)", "mm", "Default Pos 기준 플러스 방향으로 스캔할 거리입니다.", false), FormatDouble(_plusRange), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.Step, "Step (mm)", "mm", "각 Focus 측정 지점 사이의 이동 간격입니다.", false), FormatDouble(_step), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.RepeatCount, "Repeat Count (ea)", "ea", "동일 스캔 범위를 반복 측정할 횟수입니다.", true), _repeatCount.ToString(CultureInfo.InvariantCulture), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MoveVelocity, "Motor Speed (mm/s)", "mm/s", "Focus Scan 축 이동에 사용할 속도입니다.", false), FormatDouble(_moveVelocity), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MoveAcceleration, "Acceleration (mm/s2)", "mm/s2", "Focus Scan 축 이동에 사용할 가속도입니다.", false), FormatDouble(_moveAcceleration), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MoveDeceleration, "Deceleration (mm/s2)", "mm/s2", "Focus Scan 축 이동에 사용할 감속도입니다.", false), FormatDouble(_moveDeceleration), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.SettleDelay, "Settle Time (ms)", "ms", "각 위치 도착 후 Vision Focus 값을 요청하기 전 대기 시간입니다.", true), _settleDelayMs.ToString(CultureInfo.InvariantCulture), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.MotionTimeout, "Motion Timeout (ms)", "ms", "축 이동 완료 대기 시간입니다.", true), _motionTimeoutMs.ToString(CultureInfo.InvariantCulture), true);
                AddSettingRow(CreateNumberInfo(FocusSettingKey.VisionTimeout, "Vision Timeout (ms)", "ms", "VisionPC Focus 응답 대기 시간입니다.", true), _visionTimeoutMs.ToString(CultureInfo.InvariantCulture), true);
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
                if (_selectedKind == VisionFocusScanKind.BottomCollet)
                {
                    for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                        AddSavedRow(SideToText(_selectedPickerSide) + " #" + pickerNo, data.GetColletRecord(_selectedPickerSide, pickerNo));
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
                record.DefaultPosition.ToString("F4"),
                record.BestPosition.ToString("F4"),
                record.BestScore.ToString("F2"),
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
                    sample.Position.ToString("F4"),
                    sample.Score.ToString("F2"),
                    sample.Success ? "OK" : "NG",
                    sample.Raw ?? string.Empty);
            }
        }

        private VisionFocusScanSettings ResolveSettings(CDT320_Machine machine, VisionFocusScanKind kind)
        {
            machine.VisionUnit.Config.EnsureCalibrationObjects();
            VisionFocusCalibrationData data = machine.VisionUnit.Config.FocusCalibration;
            data.EnsureObjects();
            return kind == VisionFocusScanKind.BottomCollet ? data.BottomColletScan : data.SideVisionScan;
        }

        private VisionFocusPositionRecord ResolveSelectedRecord(CDT320_Machine machine)
        {
            machine.VisionUnit.Config.EnsureCalibrationObjects();
            VisionFocusCalibrationData data = machine.VisionUnit.Config.FocusCalibration;
            return _selectedKind == VisionFocusScanKind.BottomCollet
                ? data.GetColletRecord(_selectedPickerSide, _selectedPickerNo)
                : data.GetSideRecord(_selectedKind);
        }

        private double ResolveCurrentAxisPosition(CDT320_Machine machine)
        {
            if (_selectedKind == VisionFocusScanKind.BottomCollet)
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

        private double ResolveTeachingDefaultPosition(CDT320_Machine machine)
        {
            if (machine == null)
                return 0.0;

            if (_selectedKind == VisionFocusScanKind.BottomCollet)
            {
                PickerAxis zAxis = ResolveSelectedPickerZAxis();
                if (_selectedPickerSide == VisionFocusPickerSide.Front)
                    return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.GetPickerTeachingPosition(zAxis, "BottomPosition") : 0.0;
                return machine.PickerRearUnit != null ? machine.PickerRearUnit.GetPickerTeachingPosition(zAxis, "BottomPosition") : 0.0;
            }

            if (machine.VisionUnit == null || machine.VisionUnit.Recipe == null)
                return 0.0;

            if (_selectedKind == VisionFocusScanKind.FrontSide0)
                return machine.VisionUnit.Recipe.FrontSideVision.Process0Position;
            if (_selectedKind == VisionFocusScanKind.FrontSide90)
                return machine.VisionUnit.Recipe.FrontSideVision.Process90Position;
            if (_selectedKind == VisionFocusScanKind.RearSide0)
                return machine.VisionUnit.Recipe.RearSideVision.Process0Position;
            return machine.VisionUnit.Recipe.RearSideVision.Process90Position;
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
            btnStartScan.Enabled = enabled;
            btnReload.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnClose.Enabled = enabled;
        }

        private static VisionFocusScanKind TextToKind(string text)
        {
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
