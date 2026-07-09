using System;
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
    public sealed class PlaceZCalibrationDialog : Form
    {
        private const string MoveSpeedKey = "Move Speed";
        private const string MoveAccKey = "Move Acc";
        private const string MoveDecKey = "Move Dec";
        private const string MoveTimeoutKey = "Move Timeout";
        private const string StartZKey = "Start Z";
        private const string FineSearchSpeedKey = "Fine Search Speed";
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

        private readonly ComboBox _cmbSide;
        private readonly ComboBox _cmbOutputSide;
        private readonly ComboBox _cmbPickerNo;
        private readonly DataGridView _settingsGrid;
        private readonly DataGridView _resultGrid;
        private readonly Label _status;
        private readonly CalibrationDialogButton _btnCheck;
        private readonly CalibrationDialogButton _btnMoveStart;
        private readonly CalibrationDialogButton _btnStartScan;
        private readonly CalibrationDialogButton _btnMoveAvoid;
        private readonly CalibrationDialogButton _btnVacOff;
        private readonly CalibrationDialogButton _btnReload;
        private readonly CalibrationDialogButton _btnSave;
        private readonly CalibrationDialogButton _btnClose;

        private bool _busy;
        private bool _loadedOnce;
        private CancellationTokenSource _runCts;

        public static PlaceZCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "PlaceZCalibrationDialog",
                owner,
                () => new PlaceZCalibrationDialog());
        }

        public PlaceZCalibrationDialog()
        {
            Text = "Place Z Calibration";
            Size = new Size(1120, 720);
            MinimumSize = new Size(980, 620);
            Font = new Font("Malgun Gothic", 9F);
            BackColor = Color.FromArgb(238, 238, 238);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(8)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            Controls.Add(root);

            var header = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(230, 126, 0),
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 14F, FontStyle.Bold),
                Padding = new Padding(14, 0, 0, 0),
                Text = "PLACE Z CAL",
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(header, 0, 0);

            var selector = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 6, 4, 4) };
            root.Controls.Add(selector, 0, 1);

            var lblSide = new Label
            {
                Text = "Side",
                Location = new Point(8, 9),
                Size = new Size(48, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Malgun Gothic", 9F, FontStyle.Bold)
            };
            selector.Controls.Add(lblSide);

            _cmbSide = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(60, 7),
                Size = new Size(120, 25)
            };
            _cmbSide.Items.Add("Front");
            _cmbSide.Items.Add("Rear");
            _cmbSide.SelectedIndex = 0;
            selector.Controls.Add(_cmbSide);

            var lblOutput = new Label
            {
                Text = "Output",
                Location = new Point(202, 9),
                Size = new Size(62, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Malgun Gothic", 9F, FontStyle.Bold)
            };
            selector.Controls.Add(lblOutput);

            _cmbOutputSide = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(266, 7),
                Size = new Size(90, 25)
            };
            _cmbOutputSide.Items.Add("Good");
            _cmbOutputSide.Items.Add("NG");
            _cmbOutputSide.SelectedIndex = 0;
            selector.Controls.Add(_cmbOutputSide);

            var lblPicker = new Label
            {
                Text = "Picker No",
                Location = new Point(382, 9),
                Size = new Size(80, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Malgun Gothic", 9F, FontStyle.Bold)
            };
            selector.Controls.Add(lblPicker);

            _cmbPickerNo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(466, 7),
                Size = new Size(80, 25)
            };
            _cmbPickerNo.Items.Add("1");
            _cmbPickerNo.Items.Add("2");
            _cmbPickerNo.Items.Add("3");
            _cmbPickerNo.Items.Add("4");
            _cmbPickerNo.SelectedIndex = 0;
            selector.Controls.Add(_cmbPickerNo);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.Controls.Add(body, 0, 2);

            _settingsGrid = CreateGrid();
            _settingsGrid.CellDoubleClick += SettingsGrid_CellDoubleClick;
            _settingsGrid.CellToolTipTextNeeded += SettingsGrid_CellToolTipTextNeeded;
            body.Controls.Add(Wrap("CAL SETTING", _settingsGrid), 0, 0);

            _resultGrid = CreateResultGrid();
            body.Controls.Add(Wrap("SAVED PLACE Z", _resultGrid), 1, 0);

            _status = new Label
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(12, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Output Place 위치 위에 Picker를 위치시킨 후 START SCAN을 실행하세요."
            };
            root.Controls.Add(_status, 0, 3);

            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 1,
                Padding = new Padding(0, 8, 0, 0)
            };
            for (int i = 0; i < 8; i++)
                footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            root.Controls.Add(footer, 0, 4);

            _btnCheck = MakeButton("CHECK");
            _btnMoveStart = MakeButton("MOVE START");
            _btnStartScan = MakeButton("START SCAN");
            _btnMoveAvoid = MakeButton("Z AVOID");
            _btnVacOff = MakeButton("VAC OFF");
            _btnReload = MakeButton("RELOAD");
            _btnSave = MakeButton("SAVE");
            _btnClose = MakeButton("CLOSE");
            _btnStartScan.Role = CalibrationDialogButtonRole.Primary;
            _btnSave.Role = CalibrationDialogButtonRole.Dark;

            footer.Controls.Add(_btnCheck, 0, 0);
            footer.Controls.Add(_btnMoveStart, 1, 0);
            footer.Controls.Add(_btnStartScan, 2, 0);
            footer.Controls.Add(_btnMoveAvoid, 3, 0);
            footer.Controls.Add(_btnVacOff, 4, 0);
            footer.Controls.Add(_btnReload, 5, 0);
            footer.Controls.Add(_btnSave, 6, 0);
            footer.Controls.Add(_btnClose, 7, 0);

            CalibrationDialogButtonStyle.ApplyFooterButtons(
                new[] { _btnCheck, _btnMoveStart, _btnMoveAvoid, _btnVacOff, _btnReload, _btnClose },
                new[] { _btnStartScan },
                new[] { _btnSave });

            _btnCheck.Click += delegate { CheckReady(true); };
            _btnMoveStart.Click += async delegate { await MoveScanStartAsync().ConfigureAwait(true); };
            _btnStartScan.Click += async delegate { await RunCalibrationAsync().ConfigureAwait(true); };
            _btnMoveAvoid.Click += async delegate { await MoveZAvoidAsync().ConfigureAwait(true); };
            _btnVacOff.Click += delegate { VacuumOff(); };
            _btnReload.Click += delegate { LoadFromMachine(); };
            _btnSave.Click += delegate { SaveSettingsFromUi(true); };
            _btnClose.Click += delegate { Close(); };
            _cmbSide.SelectedIndexChanged += delegate { RefreshResultGrid(); };
            _cmbOutputSide.SelectedIndexChanged += delegate { RefreshResultGrid(); };
            _cmbPickerNo.SelectedIndexChanged += delegate { RefreshResultGrid(); };
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

        private static Control Wrap(string title, Control content)
        {
            var box = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = title,
                Font = new Font("Malgun Gothic", 9F, FontStyle.Bold),
                Padding = new Padding(6)
            };
            content.Font = new Font("Malgun Gothic", 9F);
            box.Controls.Add(content);
            return box;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true
            };
            grid.Columns.Add("Parameter", "PARAMETER");
            grid.Columns.Add("Value", "VALUE");
            grid.Columns.Add("Unit", "UNIT");
            grid.Columns[0].FillWeight = 52F;
            grid.Columns[1].FillWeight = 30F;
            grid.Columns[2].FillWeight = 18F;
            CalibrationDialogGridBehavior.Apply(grid);
            return grid;
        }

        private static DataGridView CreateResultGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true
            };
            grid.Columns.Add("Item", "ITEM");
            grid.Columns.Add("Side", "SIDE");
            grid.Columns.Add("Output", "OUTPUT");
            grid.Columns.Add("Picker", "PICKER");
            grid.Columns.Add("OldPlace", "OLD PLACE");
            grid.Columns.Add("StartZ", "START Z");
            grid.Columns.Add("FlowZ", "FLOW Z");
            grid.Columns.Add("Die", "DIE");
            grid.Columns.Add("Film", "FILM");
            grid.Columns.Add("SavedPlace", "SAVED PLACE");
            grid.Columns.Add("Valid", "VALID");
            grid.Columns[0].FillWeight = 32F;
            grid.Columns[1].FillWeight = 18F;
            grid.Columns[2].FillWeight = 18F;
            grid.Columns[3].FillWeight = 16F;
            grid.Columns[4].FillWeight = 24F;
            grid.Columns[5].FillWeight = 24F;
            grid.Columns[6].FillWeight = 24F;
            grid.Columns[7].FillWeight = 18F;
            grid.Columns[8].FillWeight = 18F;
            grid.Columns[9].FillWeight = 24F;
            grid.Columns[10].FillWeight = 14F;
            CalibrationDialogGridBehavior.Apply(grid);
            return grid;
        }

        private static CalibrationDialogButton MakeButton(string text)
        {
            return new CalibrationDialogButton
            {
                Dock = DockStyle.Fill,
                Text = text,
                Margin = new Padding(6, 0, 6, 0),
                Font = new Font("Malgun Gothic", 9F, FontStyle.Bold)
            };
        }

        private void LoadFromMachine()
        {
            try
            {
                PlaceZCalibrationData data = ResolveData();
                if (data == null)
                {
                    _status.Text = "PlaceZ CalibrationData를 찾을 수 없습니다.";
                    return;
                }

                PlaceZCalibrationSettings settings = data.Settings;
                settings.EnsureDefaults();
                _settingsGrid.Rows.Clear();
                AddSetting(MoveSpeedKey, settings.Motion.MoveVelocity, "mm/s");
                AddSetting(FineSearchSpeedKey, settings.FineSearchVelocityMmPerSec, "mm/s");
                AddSetting(MoveAccKey, settings.Motion.MoveAcceleration, "mm/s2");
                AddSetting(MoveDecKey, settings.Motion.MoveDeceleration, "mm/s2");
                AddSetting(MoveTimeoutKey, settings.Motion.MoveTimeoutMs, "ms");
                AddSetting(StartZKey, settings.StartZMm, "mm");
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
                _status.Text = "설정을 불러왔습니다. Output Good/NG와 Picker를 선택한 후 START SCAN을 실행하세요.";
            }
            catch (Exception ex)
            {
                _status.Text = "설정 로드 실패: " + ex.Message;
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
                _status.Text = "결과 표시 실패: " + ex.Message;
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
                    return "PlaceZ Calibration에서 Picker Z를 탐색 시작 위치와 접촉 위치로 이동할 때 사용하는 속도입니다.";
                case FineSearchSpeedKey:
                    return "BackOff, Blow, Flow OFF 확인 후 최종 Flow 위치를 다시 찾을 때 사용하는 정밀 탐색 속도입니다.";
                case MoveAccKey:
                    return "PlaceZ Calibration 전용 이동 가속도입니다. 값이 너무 크면 Place 접촉 탐색 중 충격이 커질 수 있습니다.";
                case MoveDecKey:
                    return "PlaceZ Calibration 전용 이동 감속도입니다. 접촉 감지 후 정지 안정성에 영향을 줍니다.";
                case MoveTimeoutKey:
                    return "각 Z 이동 명령 후 인포지션 완료를 기다리는 최대 시간입니다. 초과하면 캘리브레이션을 실패 처리합니다.";
                case StartZKey:
                    return "PickerZ가 Flow 탐색을 시작할 Z 위치입니다. 안전 위치 정렬 후 선택 PickerZ가 이 위치로 먼저 이동합니다.";
                case DieThicknessKey:
                    return "Die thickness. Saved Place Z = Flow Z + Die Thickness + Film Thickness.";
                case FilmThicknessKey:
                    return "Film thickness. Saved Place Z = Flow Z + Die Thickness + Film Thickness.";
                case PositionOffsetXKey:
                    return "Output Process 기준 위치에서 X 방향으로 추가 이동할 거리입니다. OutputVision 기준 Place 계산식의 receiveTargetX로 적용됩니다.";
                case PositionOffsetYKey:
                    return "Output Process 기준 위치에서 Y 방향으로 추가 이동할 거리입니다. OutputStageY Place 계산식의 receiveTargetY로 적용됩니다.";
                case SearchStartOffsetKey:
                    return "현재 Place 위치보다 이 거리만큼 위쪽 안전 위치에서 탐색을 시작합니다. 대상면에 바로 닿지 않도록 여유를 둡니다.";
                case SearchMaxDistanceKey:
                    return "Vacuum/Flow 접촉 신호를 찾기 위해 Z를 내릴 수 있는 최대 거리입니다. 이 거리 안에 신호가 없으면 실패합니다.";
                case BackOffDistanceKey:
                    return "Flow ON 감지 후 정밀 재탐색 전에 PickerZ를 되돌리는 거리입니다. 잔진공/잔에어 제거 구간입니다.";
                case ContactOffsetKey:
                    return "기존 호환용 값입니다. 현재는 Die Thickness와 같은 값으로 저장됩니다.";
                case VacuumOnDelayKey:
                    return "탐색 시작 전 Vacuum을 켠 뒤 Flow 신호가 안정될 때까지 기다리는 시간입니다.";
                case VacuumReOnDelayKey:
                    return "BackOff/Blow 후 Vacuum을 다시 켠 뒤 Flow OFF 확인 전 기다리는 시간입니다.";
                case BlowPulseTimeKey:
                    return "BackOff 후 Vacuum을 끄고 잔진공/잔에어를 제거하기 위해 Blow를 짧게 켜는 시간입니다.";
                case BlowSettleTimeKey:
                    return "Blow OFF 후 다시 Vacuum ON 하기 전에 압력이 안정되도록 기다리는 시간입니다.";
                case FlowOffConfirmTimeoutKey:
                    return "BackOff/Blow/Vacuum ON 후 Flow OFF 상태가 확인될 때까지 기다리는 최대 시간입니다.";
                case FlowStableKey:
                    return "Flow 접촉 신호가 이 시간 동안 유지되어야 접촉으로 인정합니다.";
                case FlowPollIntervalKey:
                    return "접촉 탐색 중 Flow 신호를 다시 확인하는 주기입니다.";
                case RepeatCountKey:
                    return "정밀 Flow 탐색 반복 횟수입니다. 반복 결과의 최대-최소 차이가 Repeat Tolerance 안이어야 합니다.";
                case RepeatToleranceKey:
                    return "정밀 Flow 탐색 반복 결과 사이에 허용되는 최대 편차입니다.";
                case MoveAvoidAfterScanKey:
                    return "캘리브레이션 완료 또는 실패 후 Picker Z를 Avoid 위치로 복귀할지 선택합니다.";
                case FailIfFlowAlreadyOnKey:
                    return "탐색 시작 전에 Flow가 이미 ON이면 시작 상태 이상으로 보고 즉시 실패 처리할지 선택합니다.";
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
                    _status.Text = "PlaceZ CalibrationData를 찾을 수 없습니다.";
                    return false;
                }

                PlaceZCalibrationSettings settings = data.Settings;
                if (settings.Motion == null)
                    settings.Motion = new CalibrationMotionSettings();
                settings.Motion.MoveVelocity = Math.Max(0.001, ReadDouble(MoveSpeedKey));
                settings.FineSearchVelocityMmPerSec = Math.Max(0.001, ReadDouble(FineSearchSpeedKey));
                settings.Motion.MoveAcceleration = Math.Max(0.001, ReadDouble(MoveAccKey));
                settings.Motion.MoveDeceleration = Math.Max(0.001, ReadDouble(MoveDecKey));
                settings.Motion.MoveTimeoutMs = Math.Max(100, ReadInt(MoveTimeoutKey, CalibrationMotionSettings.DefaultMoveTimeoutMs));
                settings.StartZMm = ReadDouble(StartZKey);
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
                    _status.Text = "PlaceZ Calibration 설정값을 저장했습니다.";
                return true;
            }
            catch (Exception ex)
            {
                _status.Text = "설정 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "PLACE-Z-CAL-SAVE-SETTING", _status.Text);
                return false;
            }
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
                        QMC.Common.MessageDialog.Show(this, reason, "PLACE Z CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                Form1 host = ResolveHost();
                if (host == null || host.Machine == null)
                {
                    _status.Text = "장비가 준비되지 않았습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                {
                    _status.Text = "활성 Recipe가 없습니다. PlacePosition 저장을 위해 Recipe를 먼저 로드하세요.";
                    return false;
                }

                BaseAxis zAxis = ResolvePickerZAxisObject(host);
                if (zAxis == null)
                {
                    _status.Text = "선택 Picker Z축을 찾을 수 없습니다.";
                    return false;
                }

                if (!zAxis.IsServoOn || zAxis.IsAlarm)
                {
                    _status.Text = "선택 Picker Z축 상태가 준비되지 않았습니다. servo=" +
                                   (zAxis.IsServoOn ? "ON" : "OFF") +
                                   ", alarm=" + (zAxis.IsAlarm ? "ON" : "OFF");
                    return false;
                }

                if (showOk)
                    _status.Text = "실행 가능한 상태입니다. 선택 Output Stage와 Picker 기준으로 PlaceZ Calibration을 실행할 수 있습니다.";
                return true;
            }
            catch (Exception ex)
            {
                _status.Text = "준비 확인 실패: " + ex.Message;
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
                "PlaceZ Calibration Scan 실행 중입니다. Flow 감지 위치를 찾고 PlacePosition에 저장합니다.",
                async (sequence, token, options) => await sequence.RunAsync(token, options).ConfigureAwait(false),
                true).ConfigureAwait(true);
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

                host = ResolveHost();
                runCts = BeginManualCalibrationRun(host, actionName, out actionScope, out stopHandler);
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new PickerPlaceZCalibrationSequence(context, ResolveSide(), ResolvePickerNo(), ResolveOutputSide());
                PickerSequenceOptions options = PickerSequenceOptions.Default();
                options.RunMode = SequenceRunMode.Manual;
                options.StartMode = SequenceStartMode.Restart;
                options.PickerNo = ResolvePickerNo();
                options.RestrictToPickerNo = ResolvePickerNo();

                _status.Text = runningMessage;
                int result = await action(sequence, runCts.Token, options).ConfigureAwait(true);
                LoadFromMachine();

                if (result != 0)
                {
                    _status.Text = "PlaceZ Calibration 실패. Alarm/Event Log를 확인하세요. " + sequence.Result.Message;
                    QMC.Common.MessageDialog.Show(this, _status.Text, "PLACE Z CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (saveRecipeAfterSuccess)
                {
                    bool recipeSaved = host.SaveMachineRecipe(host.CurrentRecipeName);
                    host.SaveMachineSettings();
                    _status.Text = "완료. FlowZ=" + sequence.Result.DetectedFlowPosition.ToString("F6") +
                                   ", SavedPlaceZ=" + sequence.Result.SavedPlacePosition.ToString("F6") +
                                   ", RecipeSave=" + (recipeSaved ? "OK" : "NG");
                }
                else
                {
                    _status.Text = sequence.Result.Message;
                }
            }
            catch (OperationCanceledException)
            {
                _status.Text = "PlaceZ Calibration이 정지 요청으로 중단되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-STOP", _status.Text);
            }
            catch (SequenceStopException ex)
            {
                _status.Text = "PlaceZ Calibration 정지: " + ex.Message;
                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-STOP", _status.Text);
            }
            catch (Exception ex)
            {
                _status.Text = "PlaceZ Calibration 예외: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "PLACE-Z-CAL-RUN", _status.Text);
                QMC.Common.MessageDialog.Show(this, _status.Text, "PLACE Z CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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

                _status.Text = ResolveSide() + " Picker #" + pickerNo + " Vacuum OFF 완료.";
            }
            catch (Exception ex)
            {
                _status.Text = "Vacuum OFF 실패: " + ex.Message;
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
                "PlaceZCalibration:" + actionName + ":" + ResolveSide() + ":" + ResolvePickerNo());
            CancellationTokenSource runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
            _runCts = runCts;
            stopHandler = delegate
            {
                try
                {
                    CancellationTokenSource cts = _runCts;
                    if (cts != null && !cts.IsCancellationRequested)
                        cts.Cancel();

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "PlaceZCalStop",
                        "메인 STOP 요청으로 PlaceZ Calibration 정지 요청. action=" + actionName +
                        ", side=" + ResolveSide() +
                        ", pickerNo=" + ResolvePickerNo());
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
            _btnCheck.Enabled = enabled;
            _btnMoveStart.Enabled = enabled;
            _btnStartScan.Enabled = enabled;
            _btnMoveAvoid.Enabled = enabled;
            _btnVacOff.Enabled = enabled;
            _btnReload.Enabled = enabled;
            _btnSave.Enabled = enabled;
            _btnClose.Enabled = enabled;
        }
    }
}

