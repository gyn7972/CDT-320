using System;
using QMC.CDT_320.Ui.Localization;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Sequencing;
using QMC.CDT320.Sequencing.Calibration;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class NeedlePinCalibrationDialog : Form, ILocalizedView
    {
        private const string ResultVisionOffsetX = "Vision Pixel Offset X";
        private const string ResultVisionOffsetY = "Vision Pixel Offset Y";
        private const string ResultNeedleXToVisionX = "NeedleX To VisionX";
        private const string ResultNeedleYToVisionY = "NeedleY To VisionY";

        private bool _busy;
        private bool _loadedOnce;
        private CancellationTokenSource _runCts;
        private NeedlePinCalibrationResult _lastSuccessfulResult;
        private NeedleCalibrationData _lastSuccessfulDataRecord;
        private DateTime _lastSuccessfulResultUpdatedAt;

        public static NeedlePinCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "NeedlePinCalibrationDialog",
                owner,
                () => new NeedlePinCalibrationDialog());
        }

        private void InitializeLocalization()
        {
            Lang.BindKey(lblHeader, "calibration.needlepin.lblHeader");
            Lang.BindKey(groupSetting, "calibration.needlepin.groupSetting");
            Lang.BindKey(colSettingItem, "calibration.needlepin.colSettingItem");
            Lang.BindKey(colSettingValue, "calibration.needlepin.colSettingValue");
            Lang.BindKey(colSettingUnit, "calibration.needlepin.colSettingUnit");
            Lang.BindKey(groupSaved, "calibration.needlepin.groupSaved");
            Lang.BindKey(colResultItem, "calibration.needlepin.colResultItem");
            Lang.BindKey(colResultValue, "calibration.needlepin.colResultValue");
            Lang.BindKey(colResultUnit, "calibration.needlepin.colResultUnit");
            Lang.BindKey(groupTeaching, "calibration.needlepin.groupTeaching");
            Lang.BindKey(colTeachingItem, "calibration.needlepin.colTeachingItem");
            Lang.BindKey(colTeachingTarget, "calibration.needlepin.colTeachingTarget");
            Lang.BindKey(colTeachingActual, "calibration.needlepin.colTeachingActual");
            Lang.BindKey(colTeachingUnit, "calibration.needlepin.colTeachingUnit");
            Lang.BindKey(_status, "calibration.needlepin.status");
            Lang.BindKey(_btnCheck, "calibration.needlepin.btnCheck");
            Lang.BindKey(_btnMoveReady, "calibration.needlepin.btnMoveReady");
            Lang.BindKey(_btnTeach, "calibration.needlepin.btnTeach");
            Lang.BindKey(_btnMoveTeach, "calibration.needlepin.btnMoveTeach");
            Lang.BindKey(_btnStart, "calibration.needlepin.btnStart");
            Lang.BindKey(_btnReload, "calibration.needlepin.btnReload");
            Lang.BindKey(_btnParameterSave, "calibration.needlepin.btnParameterSave");
            Lang.BindKey(_btnSave, "calibration.needlepin.btnSave");
            Lang.BindKey(_btnClose, "calibration.needlepin.btnClose");
            Lang.BindKey(this, "calibration.needlepin.this");
            CalibrationDialogText.BindGrid(_settingsGrid);
            CalibrationDialogText.BindGrid(_resultGrid);
            CalibrationDialogText.BindGrid(_teachingGrid);
        }

        public void ApplyLanguage()
        {
            // 언어 변경은 표시만 무효화하며 선택/입력/설정값을 다시 불러오지 않습니다.
            foreach (DataGridViewRow row in _settingsGrid.Rows)
                ApplySettingToolTip(row, GetSettingToolTip(Convert.ToString(row.Tag, CultureInfo.InvariantCulture)));
            foreach (DataGridViewRow row in _resultGrid.Rows)
                if (IsEditableResultOffset(Convert.ToString(row.Tag, CultureInfo.InvariantCulture)))
                    row.Cells[1].ToolTipText = Lang.T("calibration.tip.manualOffset");
            Invalidate(true);
        }

        public NeedlePinCalibrationDialog()
        {
            InitializeComponent();
            InitializeLocalization();
            ApplyButtonStyle();
            CalibrationDialogGridBehavior.Apply(_settingsGrid, _resultGrid, _teachingGrid);
            _settingsGrid.CellToolTipTextNeeded += SettingsGrid_CellToolTipTextNeeded;
            _resultGrid.CellDoubleClick += ResultGrid_CellDoubleClick;

#if false
            Text = "Needle Pin Calibration";
            Size = new Size(1180, 720);
            MinimumSize = new Size(980, 620);
            Font = new Font("맑은 고딕", 9F);
            BackColor = Color.FromArgb(238, 238, 238);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(8)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            Controls.Add(root);

            var header = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(230, 126, 0),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                Padding = new Padding(12, 0, 0, 0),
                Text = "NEEDLE PIN CAL",
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(header, 0, 0);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 540F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.Controls.Add(body, 0, 1);

            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            body.Controls.Add(left, 0, 0);

            _settingsGrid = CreateGrid();
            left.Controls.Add(Wrap("CAL SETTING", _settingsGrid), 0, 0);

            _resultGrid = CreateGrid();
            _resultGrid.ReadOnly = true;
            left.Controls.Add(Wrap("SAVED RESULT", _resultGrid), 0, 1);

            _teachingGrid = CreateTeachingGrid();
            _teachingGrid.ReadOnly = true;
            body.Controls.Add(Wrap("TEACHING DATA", _teachingGrid), 1, 0);

            _status = new Label
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(12, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "1) MOVE READY  2) USE CURRENT로 티칭  3) MOVE TEACH  4) START CAL 순서로 진행하세요."
            };
            root.Controls.Add(_status, 0, 2);

            var buttons = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 1,
                Padding = new Padding(0, 8, 0, 0)
            };
            for (int i = 0; i < 8; i++)
                buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            root.Controls.Add(buttons, 0, 3);

            _btnCheck = MakeButton("CHECK READY");
            _btnMoveReady = MakeButton("MOVE READY");
            _btnTeach = MakeButton("USE CURRENT");
            _btnMoveTeach = MakeButton("MOVE TEACH");
            _btnStart = MakeButton("START CAL");
            _btnReload = MakeButton("RELOAD");
            _btnSave = MakeButton("SAVE");
            _btnClose = MakeButton("CLOSE");
            _btnStart.BackColor = Color.FromArgb(230, 126, 0);
            _btnStart.ForeColor = Color.White;
            _btnSave.BackColor = Color.FromArgb(64, 64, 64);
            _btnSave.ForeColor = Color.White;

            buttons.Controls.Add(_btnCheck, 0, 0);
            buttons.Controls.Add(_btnMoveReady, 1, 0);
            buttons.Controls.Add(_btnTeach, 2, 0);
            buttons.Controls.Add(_btnMoveTeach, 3, 0);
            buttons.Controls.Add(_btnStart, 4, 0);
            buttons.Controls.Add(_btnReload, 5, 0);
            buttons.Controls.Add(_btnSave, 6, 0);
            buttons.Controls.Add(_btnClose, 7, 0);

            _settingsGrid.CellDoubleClick += SettingsGrid_CellDoubleClick;
            _btnCheck.Click += delegate { CheckReady(true); };
            _btnMoveReady.Click += async delegate { await MoveToReadyPositionAsync().ConfigureAwait(true); };
            _btnTeach.Click += delegate { TeachCurrentPosition(); };
            _btnMoveTeach.Click += async delegate { await MoveToTeachingPositionAsync().ConfigureAwait(true); };
            _btnStart.Click += async delegate { await RunCalibrationAsync().ConfigureAwait(true); };
            _btnReload.Click += delegate { LoadFromMachine(); };
            _btnSave.Click += delegate { SaveToMachine(true); };
            _btnClose.Click += delegate { Close(); };
#endif
        }

        private void BtnCheck_Click(object sender, EventArgs e)
        {
            CheckReady(true);
        }

        private async void BtnMoveReady_Click(object sender, EventArgs e)
        {
            await MoveToReadyPositionAsync().ConfigureAwait(true);
        }

        private void BtnTeach_Click(object sender, EventArgs e)
        {
            TeachCurrentPosition();
        }

        private async void BtnMoveTeach_Click(object sender, EventArgs e)
        {
            await MoveToTeachingPositionAsync().ConfigureAwait(true);
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            await RunCalibrationAsync().ConfigureAwait(true);
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            ClearLastSuccessfulResult();
            LoadFromMachine();
        }

        private void ApplyButtonStyle()
        {
            CalibrationDialogButtonStyle.ApplyFooterButtons(
                new[] { _btnCheck, _btnMoveReady, _btnTeach, _btnMoveTeach, _btnReload, _btnClose },
                new[] { _btnStart },
                new[] { _btnParameterSave, _btnSave });
        }

        private void BtnParameterSave_Click(object sender, EventArgs e)
        {
            SaveParameterSettingsFromUi(true);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveLastSuccessfulResult(true);
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
                        RequestRunCancelForClose("Needle Pin Calibration 창 종료(" + e.CloseReason + ")");
                        base.OnFormClosing(e);
                        return;
                    }

                    DialogResult result = QMC.Common.MessageDialog.Show(this,
                        Lang.T("calibration.message.m016"),
                        Lang.T("calibration.message.m017"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                        RequestRunCancelForClose("Needle Pin Calibration 창 닫기");

                    e.Cancel = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "NEEDLE-PIN-CAL-CLOSE",
                    "Needle Pin Calibration 창 종료 확인 중 예외가 발생했습니다. error=" + ex.Message);
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
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "NEEDLE-PIN-CAL-CLOSE-CANCEL",
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

        private static Control Wrap(string title, Control content)
        {
            var box = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = title,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Padding = new Padding(6)
            };
            content.Font = new Font("맑은 고딕", 9F);
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
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Item", "ITEM");
            grid.Columns.Add("Value", "VALUE");
            grid.Columns.Add("Unit", "UNIT");
            grid.Columns[0].ReadOnly = true;
            grid.Columns[2].ReadOnly = true;
            grid.Columns[0].FillWeight = 48F;
            grid.Columns[1].FillWeight = 34F;
            grid.Columns[2].FillWeight = 18F;
            CalibrationDialogGridBehavior.Apply(grid);
            return grid;
        }

        private static DataGridView CreateTeachingGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Item", "ITEM");
            grid.Columns.Add("Target", "TARGET");
            grid.Columns.Add("Actual", "ACTUAL");
            grid.Columns.Add("Unit", "UNIT");
            grid.Columns[0].ReadOnly = true;
            grid.Columns[1].ReadOnly = true;
            grid.Columns[2].ReadOnly = true;
            grid.Columns[3].ReadOnly = true;
            grid.Columns[0].FillWeight = 42F;
            grid.Columns[1].FillWeight = 24F;
            grid.Columns[2].FillWeight = 24F;
            grid.Columns[3].FillWeight = 10F;
            CalibrationDialogGridBehavior.Apply(grid);
            return grid;
        }

        private static Button MakeButton(string text)
        {
            return new Button
            {
                Dock = DockStyle.Fill,
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                Margin = new Padding(6, 0, 6, 0)
            };
        }

        private void LoadFromMachine()
        {
            try
            {
                InputStageUnit stage = ResolveStage();
                if (stage == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s168");
                    return;
                }

                stage.Recipe.EnsurePositionObjects();
                _settingsGrid.Rows.Clear();
                AddSetting("Vision Target", stage.Setup.NeedlePinCalVisionTargetId, "");
                AddSetting("Vision Timeout", stage.Setup.NeedlePinCalVisionTimeoutMs, "ms");
                NeedleCalibrationData needleData = ResolveNeedleCalibrationData();
                CalibrationMotionSettings motion = needleData != null ? needleData.Motion : new CalibrationMotionSettings();
                motion.EnsureDefaults();
                AddSetting("Move Speed", motion.MoveVelocity, "mm/s");
                AddSetting("Move Acc", motion.MoveAcceleration, "mm/s2");
                AddSetting("Move Dec", motion.MoveDeceleration, "mm/s2");
                AddSetting("Move Timeout", motion.MoveTimeoutMs, "ms");
                AddSetting("VisionX Cal Position", stage.Recipe.VisionX.NeedlePinCalPosition, "mm");
                AddSetting("StageY Process Position", stage.Recipe.WaferY.ProcessPosition, "mm");
                AddSetting("NeedleX Cal Position", stage.Recipe.NeedleX.NeedlePinCalPosition, "mm");
                AddSetting("NeedleZ Cal Position", stage.Recipe.NeedleZ.NeedlePinCalPosition, "mm");
                AddSetting("EjectPinZ Cal Position", stage.Recipe.EjectPinZ.NeedlePinCalPosition, "mm");

                RefreshResultGrid(stage);
                RefreshTeachingGrid(stage);
                Lang.BindFormat(_status, "calibration.status.s169");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s049", ex.Message);
            }
        }

        private void RefreshResultGrid(InputStageUnit stage)
        {
            _resultGrid.Rows.Clear();
            NeedleCalibrationData needle = ResolveNeedleCalibrationData();
            AddResult(ResultVisionOffsetX, needle != null && needle.Valid ? needle.VisionOffsetX : 0.0, "mm");
            AddResult(ResultVisionOffsetY, needle != null && needle.Valid ? needle.VisionOffsetY : 0.0, "mm");
            AddResult(ResultNeedleXToVisionX, needle != null && needle.Valid ? needle.NeedleXToVisionXOffset : 0.0, "mm");
            AddResult(ResultNeedleYToVisionY, needle != null && needle.Valid ? needle.NeedleYToVisionYOffset : 0.0, "mm");
            AddResult("Calibration Valid", needle != null && needle.Valid ? "OK" : "-", "");
            AddResult("Vision Target", stage.Setup.NeedlePinCalVisionTargetId, "");
            AddResult("Vision Timeout", stage.Setup.NeedlePinCalVisionTimeoutMs, "ms");
        }

        private NeedleCalibrationData ResolveNeedleCalibrationData()
        {
            Form1 host = ResolveHost();
            if (host == null ||
                host.Machine == null ||
                host.Machine.VisionUnit == null ||
                host.Machine.VisionUnit.Config == null)
                return null;

            host.Machine.VisionUnit.Config.EnsureCalibrationObjects();
            host.Machine.VisionUnit.Config.CalibrationData.EnsureObjects();
            host.Machine.VisionUnit.Config.CalibrationData.Needle.EnsureObjects();
            return host.Machine.VisionUnit.Config.CalibrationData.Needle;
        }

        private void RefreshTeachingGrid(InputStageUnit stage)
        {
            _teachingGrid.Rows.Clear();
            AddTeaching("OutputCameraX Avoid", GetOutputVisionAvoid(), GetAxisActual("OutputCameraX"), "mm");
            AddTeaching("FrontPickerX Avoid", GetFrontPickerAvoid(), GetAxisActual("FrontPickerX"), "mm");
            AddTeaching("RearPickerX Avoid", GetRearPickerAvoid(), GetAxisActual("RearPickerX"), "mm");
            AddTeaching("StageY Process", stage.Recipe.WaferY.ProcessPosition, stage.StageY != null ? stage.StageY.ActualPosition : 0.0, "mm");
            AddTeaching("StageT Process", stage.Recipe.WaferT.ProcessPosition, stage.StageT != null ? stage.StageT.ActualPosition : 0.0, "deg");
            AddTeaching("ExpanderZ Process", stage.Recipe.WaferZ.ProcessPosition, stage.ExpanderZ != null ? stage.ExpanderZ.ActualPosition : 0.0, "mm");
            AddTeaching("VisionX Cal", stage.Recipe.VisionX.NeedlePinCalPosition, stage.CameraX != null ? stage.CameraX.ActualPosition : 0.0, "mm");
            AddTeaching("NeedleX Cal", stage.Recipe.NeedleX.NeedlePinCalPosition, stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : 0.0, "mm");
            AddTeaching("NeedleZ Cal", stage.Recipe.NeedleZ.NeedlePinCalPosition, stage.NeedleZ != null ? stage.NeedleZ.ActualPosition : 0.0, "mm");
            AddTeaching("EjectPinZ Cal", stage.Recipe.EjectPinZ.NeedlePinCalPosition, stage.EjectPinZ != null ? stage.EjectPinZ.ActualPosition : 0.0, "mm");
        }

        private void AddSetting(string name, double value, string unit)
        {
            AddRow(_settingsGrid, name, value.ToString("0.######", CultureInfo.InvariantCulture), unit);
            ApplySettingToolTip(_settingsGrid.Rows[_settingsGrid.Rows.Count - 1], GetSettingToolTip(name));
        }

        private void AddSetting(string name, int value, string unit)
        {
            AddRow(_settingsGrid, name, value.ToString(CultureInfo.InvariantCulture), unit);
            ApplySettingToolTip(_settingsGrid.Rows[_settingsGrid.Rows.Count - 1], GetSettingToolTip(name));
        }

        private void AddSetting(string name, string value, string unit)
        {
            AddRow(_settingsGrid, name, value ?? string.Empty, unit);
            ApplySettingToolTip(_settingsGrid.Rows[_settingsGrid.Rows.Count - 1], GetSettingToolTip(name));
        }

        private void AddResult(string name, double value, string unit)
        {
            AddRow(_resultGrid, name, value.ToString("0.######", CultureInfo.InvariantCulture), unit);
            ApplyResultRowStyle(_resultGrid.Rows[_resultGrid.Rows.Count - 1]);
        }

        private void AddResult(string name, int value, string unit)
        {
            AddRow(_resultGrid, name, value.ToString(CultureInfo.InvariantCulture), unit);
            ApplyResultRowStyle(_resultGrid.Rows[_resultGrid.Rows.Count - 1]);
        }

        private void AddResult(string name, string value, string unit)
        {
            AddRow(_resultGrid, name, value ?? string.Empty, unit);
            ApplyResultRowStyle(_resultGrid.Rows[_resultGrid.Rows.Count - 1]);
        }

        private void AddTeaching(string name, double target, double actual, string unit)
        {
            int row = _teachingGrid.Rows.Add(
                name,
                target.ToString("0.######", CultureInfo.InvariantCulture),
                actual.ToString("0.######", CultureInfo.InvariantCulture),
                unit);
            _teachingGrid.Rows[row].Tag = name;
        }

        private static void AddRow(DataGridView grid, string name, string value, string unit)
        {
            int row = grid.Rows.Add(name, value, unit);
            grid.Rows[row].Tag = name;
        }

        private static void ApplyResultRowStyle(DataGridViewRow row)
        {
            if (row == null)
                return;

            string name = Convert.ToString(row.Tag, CultureInfo.InvariantCulture);
            bool editable = IsEditableResultOffset(name);
            if (editable)
            {
                row.Cells[1].ToolTipText = Lang.T("calibration.tip.manualOffset");
                row.Cells[1].Style.BackColor = Color.FromArgb(255, 255, 230);
            }
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
                case "Vision Target":
                    return Lang.T("calibration.tip.t091");
                case "Vision Timeout":
                    return Lang.T("calibration.tip.t092");
                case "Move Speed":
                    return Lang.T("calibration.tip.t093");
                case "Move Acc":
                    return Lang.T("calibration.tip.t094");
                case "Move Dec":
                    return Lang.T("calibration.tip.t095");
                case "Move Timeout":
                    return Lang.T("calibration.tip.t096");
                case "VisionX Cal Position":
                    return Lang.T("calibration.tip.t097");
                case "StageY Process Position":
                    return Lang.T("calibration.tip.t098");
                case "NeedleX Cal Position":
                    return Lang.T("calibration.tip.t099");
                case "NeedleZ Cal Position":
                    return Lang.T("calibration.tip.t100");
                case "EjectPinZ Cal Position":
                    return Lang.T("calibration.tip.t101");
                default:
                    return string.Empty;
            }
        }

        private void SettingsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            string name = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Tag, CultureInfo.InvariantCulture);
            if (name == "Vision Target")
            {
                Lang.BindFormat(_status, "calibration.status.s170");
                return;
            }

            string unit = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Cells[2].Value, CultureInfo.InvariantCulture);
            string current = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Cells[1].Value, CultureInfo.InvariantCulture);
            using (var keypad = new QMC.CDT_320.Ui.Controls.NumericKeypadDialog(name, current, unit))
            {
                if (keypad.ShowDialog(this) == DialogResult.OK)
                    _settingsGrid.Rows[e.RowIndex].Cells[1].Value = keypad.ValueText;
            }
        }

        private void ResultGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            string name = Convert.ToString(_resultGrid.Rows[e.RowIndex].Tag, CultureInfo.InvariantCulture);
            if (!IsEditableResultOffset(name))
                return;

            // [측정 가드 보강 2026-07-27] SAVED RESULT 그리드 직접 수정은 SAVE RESULT의 측정 가드를
            // 완전히 우회해 캘리브레이션 결과를 임의 값으로 영속화할 수 있었다.
            // 결과값 수정은 되돌릴 수 없는 조작이므로 Admin 권한 + 명시적 확인을 요구한다.
            if (!UserSession.Has(UserLevel.Admin))
            {
                Lang.BindFormat(_status, "calibration.status.s171");
                QMC.Common.MessageDialog.Show(this, _status.Text, Lang.T("calibration.message.m017"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (QMC.Common.MessageDialog.Show(
                    this,
                    Lang.Format("calibration.message.m018", name),
                    Lang.T("calibration.message.m017"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            string unit = Convert.ToString(_resultGrid.Rows[e.RowIndex].Cells[2].Value, CultureInfo.InvariantCulture);
            string currentText = Convert.ToString(_resultGrid.Rows[e.RowIndex].Cells[1].Value, CultureInfo.InvariantCulture);
            using (var keypad = new QMC.CDT_320.Ui.Controls.NumericKeypadDialog(name, currentText, unit))
            {
                if (keypad.ShowDialog(this) != DialogResult.OK)
                    return;

                double value;
                if (!TryParseDouble(keypad.ValueText, out value))
                {
                    QMC.Common.MessageDialog.Show(this, Lang.Format("calibration.message.m019", name), Lang.T("calibration.message.m017"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ApplyManualResultOffset(name, value))
                    return;

                _resultGrid.Rows[e.RowIndex].Cells[1].Value = value.ToString("0.######", CultureInfo.InvariantCulture);
                Lang.BindFormat(_status, "calibration.status.s172", name, value.ToString("0.######", CultureInfo.InvariantCulture), unit);
            }
        }

        private bool ApplyManualResultOffset(string name, double value)
        {
            try
            {
                Form1 host = ResolveHost();
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null || host.Machine.VisionUnit.Config == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s173");
                    return false;
                }

                host.Machine.VisionUnit.Config.EnsureCalibrationObjects();
                host.Machine.VisionUnit.Config.CalibrationData.EnsureObjects();
                NeedleCalibrationData needle = host.Machine.VisionUnit.Config.CalibrationData.Needle;
                if (needle == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s152");
                    return false;
                }

                if (name == ResultVisionOffsetX)
                    needle.VisionOffsetX = value;
                else if (name == ResultVisionOffsetY)
                    needle.VisionOffsetY = value;
                else if (name == ResultNeedleXToVisionX)
                    needle.NeedleXToVisionXOffset = value;
                else if (name == ResultNeedleYToVisionY)
                    needle.NeedleYToVisionYOffset = value;
                else
                    return false;

                needle.Valid = true;
                needle.UpdatedAt = DateTime.Now;
                needle.UpdatedBy = "NeedlePinCalibrationManual";
                host.Machine.VisionUnit.Config.CalibrationData.Touch("NeedlePinCalibrationManual");
                host.SaveMachineSettings();

                // 수동 수정으로 측정 결과가 바뀌었으므로 직전 측정 스냅샷은 더 이상 유효하지 않다.
                // (SAVE RESULT가 스테일 스냅샷으로 다시 덮어쓰는 것을 막는다.)
                ClearLastSuccessfulResult();

                string auditMessage = "Needle Pin Calibration 결과값을 수동 수정했습니다. item=" + name +
                                      ", value=" + value.ToString("0.######", CultureInfo.InvariantCulture) +
                                      ", user=" + UserSession.Name;
                QMC.Common.Log.Write("Calibration", "SYSTEM", "NeedlePinCalManualOffset", auditMessage + " - Check");
                EventLogger.Write(EventKind.Warning, "CAL", "NEEDLE-PIN-CAL-MANUAL-OFFSET", auditMessage);
                return true;
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s174", ex.Message);
                return false;
            }
        }

        private static bool IsEditableResultOffset(string name)
        {
            return name == ResultVisionOffsetX ||
                   name == ResultVisionOffsetY ||
                   name == ResultNeedleXToVisionX ||
                   name == ResultNeedleYToVisionY;
        }

        private static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private bool CheckReady(bool showOk)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null)
            {
                Lang.BindFormat(_status, "calibration.status.s158");
                return false;
            }

            if (stage.Recipe == null || stage.Setup == null)
            {
                Lang.BindFormat(_status, "calibration.status.s175");
                return false;
            }

            if (ResolveHost() == null)
            {
                Lang.BindFormat(_status, "calibration.status.s176");
                return false;
            }

            if (showOk)
                Lang.BindFormat(_status, "calibration.status.s006");
            return true;
        }

        private void TeachCurrentPosition()
        {
            try
            {
                InputStageUnit stage = ResolveStage();
                if (stage == null)
                    return;

                stage.Recipe.EnsurePositionObjects();
                if (stage.CameraX != null)
                    stage.Recipe.VisionX.NeedlePinCalPosition = stage.CameraX.ActualPosition;
                if (stage.NeedleBlockX != null)
                    stage.Recipe.NeedleX.NeedlePinCalPosition = stage.NeedleBlockX.ActualPosition;
                if (stage.NeedleZ != null)
                    stage.Recipe.NeedleZ.NeedlePinCalPosition = stage.NeedleZ.ActualPosition;
                if (stage.EjectPinZ != null)
                    stage.Recipe.EjectPinZ.NeedlePinCalPosition = stage.EjectPinZ.ActualPosition;

                // [정정 2026-07-27] Cal Position은 InputStageRecipe(IRecipeData) 소속이라
                // SaveMachineSettings로는 저장되지 않는다(Setup/Config만 기록).
                // 기존에는 이 다이얼로그가 SaveMachineRecipe를 한 번도 호출하지 않아
                // 티칭한 값이 메모리에만 남고 재기동 시 사라졌다.
                LoadFromMachine();
                Form1 teachHost = ResolveHost();
                if (teachHost == null)
                {
                    Lang.BindFormat(_status, "calibration.status.s177");
                    return;
                }

                bool recipeSaved = teachHost.SaveMachineRecipe(teachHost.ActiveRecipeName);
                teachHost.SaveMachineSettings();
                if (!recipeSaved)
                {
                    Lang.BindFormat(_status, "calibration.status.s178", teachHost.ActiveRecipeName);
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "NeedlePinCalTeach", _status.Text + " - Failed");
                    return;
                }

                Lang.BindFormat(_status, "calibration.status.s179", teachHost.ActiveRecipeName);
                QMC.Common.Log.Write("Calibration", "SYSTEM", "NeedlePinCalTeach", _status.Text + " - Ok");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s180", ex.Message);
            }
        }

        private bool SaveParameterSettingsFromUi(bool showMessage)
        {
            try
            {
                InputStageUnit stage = ResolveStage();
                if (stage == null)
                    return false;

                stage.Recipe.EnsurePositionObjects();
                stage.Setup.NeedlePinCalVisionTargetId = ReadString("Vision Target", VisionToolIds.Wafer.EjectPinFinder);
                stage.Setup.NeedlePinCalVisionTimeoutMs = Math.Max(1000, ReadInt("Vision Timeout", 5000));
                NeedleCalibrationData needleData = ResolveNeedleCalibrationData();
                if (needleData != null)
                {
                    if (needleData.Motion == null)
                        needleData.Motion = new CalibrationMotionSettings();
                    needleData.Motion.MoveVelocity = Math.Max(0.001, ReadDouble("Move Speed"));
                    needleData.Motion.MoveAcceleration = Math.Max(0.001, ReadDouble("Move Acc"));
                    needleData.Motion.MoveDeceleration = Math.Max(0.001, ReadDouble("Move Dec"));
                    needleData.Motion.MoveTimeoutMs = Math.Max(100, ReadInt("Move Timeout", CalibrationMotionSettings.DefaultMoveTimeoutMs));
                    needleData.Motion.EnsureDefaults();
                }
                // [정정 2026-07-27] 여기서 Recipe의 Cal Position(티칭값)을 쓰지 않는다.
                // 기존에는 PARAMETER SAVE가 VisionX/StageY/NeedleX/NeedleZ/EjectPinZ 티칭 위치까지
                // 그리드 표시값으로 되돌려, 파라미터만 고치려 해도 티칭이 덮어써졌다.
                // 티칭 변경은 USE CURRENT(TeachCurrentPosition) / SAVE TEACHING 전용 경로에서만 수행한다.

                Form1 host = ResolveHost();
                if (host != null)
                    host.SaveMachineSettings();

                RefreshResultGrid(stage);
                RefreshTeachingGrid(stage);
                if (showMessage)
                    Lang.BindFormat(_status, "calibration.status.s181");
                return true;
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s182", ex.Message);
                return false;
            }
        }

        private bool SaveLastSuccessfulResult(bool showMessage)
        {
            try
            {
                if (_lastSuccessfulResult == null || !_lastSuccessfulResult.Success)
                    return BlockResultSave(
                        "정상 완료된 Needle Pin Calibration 결과가 없습니다. START CAL을 먼저 완료하세요.",
                        showMessage);

                Form1 host = ResolveHost();
                NeedleCalibrationData current = ResolveNeedleCalibrationData();
                if (host == null || current == null)
                    return BlockResultSave("장비 또는 Needle CalibrationData가 준비되지 않았습니다.", showMessage);

                if (!ReferenceEquals(current, _lastSuccessfulDataRecord) ||
                    !current.Valid ||
                    current.UpdatedAt != _lastSuccessfulResultUpdatedAt ||
                    !MatchesLastSuccessfulResult(current, _lastSuccessfulResult))
                {
                    return BlockResultSave(
                        "마지막 정상 완료 결과가 이후에 변경되었거나 다시 로드되었습니다. 잘못된 결과 저장을 막기 위해 SAVE RESULT를 차단합니다.",
                        showMessage);
                }

                // Sequence가 만든 정확한 Needle Pin 결과 record를 다시 영속화한다.
                // 현재 축 ActualPosition은 읽지 않으며 측정값을 다시 계산하지 않는다.
                host.SaveMachineSettings();
                Lang.BindFormat(_status, "calibration.status.s183", _lastSuccessfulResultUpdatedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                QMC.Common.Log.Write("Calibration", "SYSTEM", "NeedlePinCalSaveResult", _status.Text);
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL-SAVE-RESULT", _status.Text);
                InputStageUnit stage = ResolveStage();
                if (stage != null)
                    RefreshResultGrid(stage);
                return true;
            }
            catch (Exception ex)
            {
                return BlockResultSave("Needle Pin Calibration 결과 저장 실패: " + ex.Message, showMessage);
            }
        }

        private bool BlockResultSave(string message, bool showMessage)
        {
            CalibrationDialogText.BindStatus(_status, message);
            EventLogger.Write(EventKind.Warning, "CAL", "NEEDLE-PIN-CAL-SAVE-RESULT-BLOCK", message);
            if (showMessage)
                QMC.Common.MessageDialog.Show(this, message, Lang.T("calibration.message.m017"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void RememberSuccessfulResult(NeedlePinCalibrationResult result)
        {
            NeedleCalibrationData data = ResolveNeedleCalibrationData();
            if (result == null || !result.Success || data == null || !data.Valid)
            {
                ClearLastSuccessfulResult();
                EventLogger.Write(EventKind.Warning, "CAL", "NEEDLE-PIN-CAL-RESULT-TRACK",
                    "정상 완료 Needle Pin Calibration 결과 추적에 실패했습니다.");
                return;
            }

            _lastSuccessfulResult = new NeedlePinCalibrationResult
            {
                Success = true,
                VisionXPosition = result.VisionXPosition,
                StageYPosition = result.StageYPosition,
                NeedleXPosition = result.NeedleXPosition,
                NeedleZPosition = result.NeedleZPosition,
                EjectPinZPosition = result.EjectPinZPosition,
                VisionOffsetX = result.VisionOffsetX,
                VisionOffsetY = result.VisionOffsetY,
                NeedleXToVisionXOffset = result.NeedleXToVisionXOffset,
                NeedleYToVisionYOffset = result.NeedleYToVisionYOffset,
                Message = result.Message
            };
            _lastSuccessfulDataRecord = data;
            _lastSuccessfulResultUpdatedAt = data.UpdatedAt;
        }

        private void ClearLastSuccessfulResult()
        {
            _lastSuccessfulResult = null;
            _lastSuccessfulDataRecord = null;
            _lastSuccessfulResultUpdatedAt = DateTime.MinValue;
        }

        private static bool MatchesLastSuccessfulResult(
            NeedleCalibrationData data,
            NeedlePinCalibrationResult result)
        {
            return data.VisionXPosition == result.VisionXPosition &&
                   data.StageYPosition == result.StageYPosition &&
                   data.NeedleXPosition == result.NeedleXPosition &&
                   data.NeedleZPosition == result.NeedleZPosition &&
                   data.EjectPinZPosition == result.EjectPinZPosition &&
                   data.VisionOffsetX == result.VisionOffsetX &&
                   data.VisionOffsetY == result.VisionOffsetY &&
                   data.NeedleXToVisionXOffset == result.NeedleXToVisionXOffset &&
                   data.NeedleYToVisionYOffset == result.NeedleYToVisionYOffset;
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
                "NeedlePinCalibration:" + actionName);
            CancellationTokenSource runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
            _runCts = runCts;
            stopHandler = delegate
            {
                try
                {
                    CancellationTokenSource cts = _runCts;
                    if (cts != null && !cts.IsCancellationRequested)
                        cts.Cancel();

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "NeedlePinCalStop",
                        "메인 STOP 요청으로 Needle Pin Calibration 정지 요청. action=" + actionName);
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

        private async Task MoveToReadyPositionAsync()
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
                if (!SaveParameterSettingsFromUi(false) || !CheckReady(false))
                    return;

                host = ResolveHost();
                runCts = BeginManualCalibrationRun(host, "MoveReady", out actionScope, out stopHandler);
                var sequence = new NeedlePinCalibrationSequence(new MachineSequenceContext(host.Controller, new SequenceSignalBus()));
                Lang.BindFormat(_status, "calibration.status.s184");
                int result = await sequence.MoveReadyPositionOnlyAsync(runCts.Token, SequenceRunMode.Manual).ConfigureAwait(true);
                LoadFromMachine();
                { if (result == 0) Lang.BindFormat(_status, "calibration.status.s185"); else Lang.BindFormat(_status, "calibration.status.s186", sequence.Result.Message); }
            }
            catch (OperationCanceledException)
            {
                Lang.BindFormat(_status, "calibration.status.s187");
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL-STOP", _status.Text);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s188", ex.Message);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task MoveToTeachingPositionAsync()
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
                if (!SaveParameterSettingsFromUi(false) || !CheckReady(false))
                    return;

                host = ResolveHost();
                runCts = BeginManualCalibrationRun(host, "MoveTeach", out actionScope, out stopHandler);
                var sequence = new NeedlePinCalibrationSequence(new MachineSequenceContext(host.Controller, new SequenceSignalBus()));
                Lang.BindFormat(_status, "calibration.status.s189");
                int result = await sequence.MoveTeachingPositionOnlyAsync(runCts.Token, SequenceRunMode.Manual).ConfigureAwait(true);
                LoadFromMachine();
                { if (result == 0) Lang.BindFormat(_status, "calibration.status.s190"); else Lang.BindFormat(_status, "calibration.status.s191", sequence.Result.Message); }
            }
            catch (OperationCanceledException)
            {
                Lang.BindFormat(_status, "calibration.status.s192");
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL-STOP", _status.Text);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s193", ex.Message);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
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
                if (!SaveParameterSettingsFromUi(false) || !CheckReady(false))
                    return;

                ClearLastSuccessfulResult();
                host = ResolveHost();
                runCts = BeginManualCalibrationRun(host, "StartCal", out actionScope, out stopHandler);
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new NeedlePinCalibrationSequence(context);
                Lang.BindFormat(_status, "calibration.status.s194");
                int result = await sequence.RunAsync(runCts.Token, SequenceRunMode.Manual).ConfigureAwait(true);
                LoadFromMachine();

                if (result != 0)
                {
                    Lang.BindFormat(_status, "calibration.status.s195", sequence.Result.Message);
                    QMC.Common.MessageDialog.Show(this, _status.Text, Lang.T("calibration.message.m017"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                host.SaveMachineSettings();
                RememberSuccessfulResult(sequence.Result);
                Lang.BindFormat(_status, "calibration.status.s196", sequence.Result.VisionOffsetX.ToString("F6"), sequence.Result.VisionOffsetY.ToString("F6"), sequence.Result.NeedleXToVisionXOffset.ToString("F6"));
            }
            catch (OperationCanceledException)
            {
                Lang.BindFormat(_status, "calibration.status.s197");
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL-STOP", _status.Text);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(_status, "calibration.status.s198", ex.Message);
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-PIN-CAL-DIALOG", _status.Text);
                QMC.Common.MessageDialog.Show(this, _status.Text, Lang.T("calibration.message.m017"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EndManualCalibrationRun(host, stopHandler, runCts, actionScope);
                _busy = false;
                SetButtonsEnabled(true);
            }
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

            Form form = FindForm();
            host = form as Form1;
            if (host != null)
                return host;

            foreach (Form open in Application.OpenForms)
            {
                host = open as Form1;
                if (host != null)
                    return host;
            }

            return null;
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

        private string ReadString(string name, string fallback)
        {
            foreach (DataGridViewRow row in _settingsGrid.Rows)
            {
                if (Convert.ToString(row.Tag, CultureInfo.InvariantCulture) == name)
                    return Convert.ToString(row.Cells[1].Value, CultureInfo.InvariantCulture);
            }
            return fallback;
        }

        private double GetOutputVisionAvoid()
        {
            Form1 host = ResolveHost();
            return host != null && host.Machine.OutputStageUnit != null && host.Machine.OutputStageUnit.Recipe != null
                ? host.Machine.OutputStageUnit.Recipe.VisionX.AvoidPosition
                : 0.0;
        }

        private double GetFrontPickerAvoid()
        {
            Form1 host = ResolveHost();
            return host != null && host.Machine.PickerFrontUnit != null
                ? host.Machine.PickerFrontUnit.GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition")
                : 0.0;
        }

        private double GetRearPickerAvoid()
        {
            Form1 host = ResolveHost();
            return host != null && host.Machine.PickerRearUnit != null
                ? host.Machine.PickerRearUnit.GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition")
                : 0.0;
        }

        private double GetAxisActual(string axisName)
        {
            Form1 host = ResolveHost();
            if (host == null || host.Machine == null)
                return 0.0;

            if (axisName == "OutputCameraX" && host.Machine.OutputStageUnit != null && host.Machine.OutputStageUnit.OutputCameraX != null)
                return host.Machine.OutputStageUnit.OutputCameraX.ActualPosition;
            if (axisName == "FrontPickerX" && host.Machine.PickerFrontUnit != null && host.Machine.PickerFrontUnit.PickerX != null)
                return host.Machine.PickerFrontUnit.PickerX.ActualPosition;
            if (axisName == "RearPickerX" && host.Machine.PickerRearUnit != null && host.Machine.PickerRearUnit.PickerX != null)
                return host.Machine.PickerRearUnit.PickerX.ActualPosition;
            return 0.0;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _btnCheck.Enabled = enabled;
            _btnMoveReady.Enabled = enabled;
            _btnTeach.Enabled = enabled;
            _btnMoveTeach.Enabled = enabled;
            _btnStart.Enabled = enabled;
            _btnReload.Enabled = enabled;
            _btnParameterSave.Enabled = enabled;
            // [측정 가드 2026-07-27] SAVE RESULT는 정상 완료된 측정 결과가 있을 때만 활성화한다.
            // 기존에는 _busy 여부만 반영해 측정 전에도 눌러볼 수 있었고, 코드 가드에만 의존했다.
            // (PickUpZ/PlaceZ의 UpdateResultSaveButtonEnabled와 동일 기준)
            _btnSave.Enabled = enabled && HasSavableSuccessfulResult();
            _btnClose.Enabled = enabled;
            _settingsGrid.Enabled = enabled;
        }

        private bool HasSavableSuccessfulResult()
        {
            return _lastSuccessfulResult != null && _lastSuccessfulResult.Success;
        }
    }
}
