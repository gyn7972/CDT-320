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
using QMC.CDT_320.Ui.Security;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class NeedlePinCalibrationDialog : Form
    {
        private bool _busy;
        private bool _loadedOnce;

        public static NeedlePinCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "NeedlePinCalibrationDialog",
                owner,
                () => new NeedlePinCalibrationDialog());
        }

        public NeedlePinCalibrationDialog()
        {
            InitializeComponent();
            ApplyButtonStyle();

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
            LoadFromMachine();
        }

        private void ApplyButtonStyle()
        {
            CalibrationDialogButtonStyle.ApplyFooterButtons(
                new[] { _btnCheck, _btnMoveReady, _btnTeach, _btnMoveTeach, _btnReload, _btnClose },
                new[] { _btnStart },
                new[] { _btnSave });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveToMachine(true);
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
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
                    _status.Text = "InputStageUnit을 찾을 수 없습니다. CalibrationPage에서 다시 열어 주세요.";
                    return;
                }

                stage.Recipe.EnsurePositionObjects();
                _settingsGrid.Rows.Clear();
                AddSetting("Vision Target", stage.Setup.NeedlePinCalVisionTargetId, "");
                AddSetting("Vision Timeout", stage.Setup.NeedlePinCalVisionTimeoutMs, "ms");
                AddSetting("VisionX Cal Position", stage.Recipe.VisionX.NeedlePinCalPosition, "mm");
                AddSetting("StageY Process Position", stage.Recipe.WaferY.ProcessPosition, "mm");
                AddSetting("NeedleX Cal Position", stage.Recipe.NeedleX.NeedlePinCalPosition, "mm");
                AddSetting("NeedleZ Cal Position", stage.Recipe.NeedleZ.NeedlePinCalPosition, "mm");
                AddSetting("EjectPinZ Cal Position", stage.Recipe.EjectPinZ.NeedlePinCalPosition, "mm");

                RefreshResultGrid(stage);
                RefreshTeachingGrid(stage);
                _status.Text = "설정을 불러왔습니다. 1) MOVE READY  2) USE CURRENT  3) MOVE TEACH  4) START CAL 순서로 진행하세요.";
            }
            catch (Exception ex)
            {
                _status.Text = "설정 로드 실패: " + ex.Message;
            }
        }

        private void RefreshResultGrid(InputStageUnit stage)
        {
            _resultGrid.Rows.Clear();
            NeedleCalibrationData needle = ResolveNeedleCalibrationData();
            AddResult("NeedleX To VisionX", needle != null && needle.Valid ? needle.NeedleXToVisionXOffset : 0.0, "mm");
            AddResult("NeedleY To VisionY", needle != null && needle.Valid ? needle.NeedleYToVisionYOffset : 0.0, "mm");
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
        }

        private void AddSetting(string name, int value, string unit)
        {
            AddRow(_settingsGrid, name, value.ToString(CultureInfo.InvariantCulture), unit);
        }

        private void AddSetting(string name, string value, string unit)
        {
            AddRow(_settingsGrid, name, value ?? string.Empty, unit);
        }

        private void AddResult(string name, double value, string unit)
        {
            AddRow(_resultGrid, name, value.ToString("0.######", CultureInfo.InvariantCulture), unit);
        }

        private void AddResult(string name, int value, string unit)
        {
            AddRow(_resultGrid, name, value.ToString(CultureInfo.InvariantCulture), unit);
        }

        private void AddResult(string name, string value, string unit)
        {
            AddRow(_resultGrid, name, value ?? string.Empty, unit);
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

        private void SettingsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            string name = Convert.ToString(_settingsGrid.Rows[e.RowIndex].Tag, CultureInfo.InvariantCulture);
            if (name == "Vision Target")
            {
                _settingsGrid.BeginEdit(true);
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

        private bool CheckReady(bool showOk)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null)
            {
                _status.Text = "InputStageUnit을 찾을 수 없습니다.";
                return false;
            }

            if (stage.Recipe == null || stage.Setup == null)
            {
                _status.Text = "InputStage Recipe/Setup이 없습니다.";
                return false;
            }

            if (ResolveHost() == null)
            {
                _status.Text = "Form1/Controller를 찾을 수 없습니다.";
                return false;
            }

            if (showOk)
                _status.Text = "실행 가능한 상태입니다.";
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

                LoadFromMachine();
                _status.Text = "현재 축 위치를 Needle Pin Cal Position으로 티칭했습니다. 다음은 MOVE TEACH로 티칭 위치 이동을 확인하세요.";
            }
            catch (Exception ex)
            {
                _status.Text = "티칭 실패: " + ex.Message;
            }
        }

        private bool SaveToMachine(bool showMessage)
        {
            try
            {
                InputStageUnit stage = ResolveStage();
                if (stage == null)
                    return false;

                stage.Recipe.EnsurePositionObjects();
                stage.Setup.NeedlePinCalVisionTargetId = ReadString("Vision Target", "NeedlePinCal");
                stage.Setup.NeedlePinCalVisionTimeoutMs = Math.Max(1000, ReadInt("Vision Timeout", 5000));
                stage.Recipe.VisionX.NeedlePinCalPosition = ReadDouble("VisionX Cal Position");
                stage.Recipe.WaferY.ProcessPosition = ReadDouble("StageY Process Position");
                stage.Recipe.NeedleX.NeedlePinCalPosition = ReadDouble("NeedleX Cal Position");
                stage.Recipe.NeedleZ.NeedlePinCalPosition = ReadDouble("NeedleZ Cal Position");
                stage.Recipe.EjectPinZ.NeedlePinCalPosition = ReadDouble("EjectPinZ Cal Position");

                Form1 host = ResolveHost();
                if (host != null)
                    host.SaveMachineSettings();

                RefreshResultGrid(stage);
                RefreshTeachingGrid(stage);
                if (showMessage)
                    _status.Text = "설정을 저장했습니다.";
                return true;
            }
            catch (Exception ex)
            {
                _status.Text = "저장 실패: " + ex.Message;
                return false;
            }
        }

        private async Task MoveToReadyPositionAsync()
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                if (!SaveToMachine(false) || !CheckReady(false))
                    return;

                Form1 host = ResolveHost();
                var sequence = new NeedlePinCalibrationSequence(new MachineSequenceContext(host.Controller, new SequenceSignalBus()), false);
                _status.Text = "Ready 위치로 이동 중입니다. OutputCamera/Picker는 Avoid, InputStage는 Process로 이동합니다.";
                int result = await sequence.MoveReadyPositionOnlyAsync(CancellationToken.None).ConfigureAwait(true);
                LoadFromMachine();
                _status.Text = result == 0
                    ? "Ready 위치 이동 완료. Needle/Camera를 조그로 맞춘 뒤 USE CURRENT로 티칭하세요."
                    : "Ready 위치 이동 실패: " + sequence.Result.Message;
            }
            catch (Exception ex)
            {
                _status.Text = "Ready 위치 이동 예외: " + ex.Message;
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task MoveToTeachingPositionAsync()
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                if (!SaveToMachine(false) || !CheckReady(false))
                    return;

                Form1 host = ResolveHost();
                var sequence = new NeedlePinCalibrationSequence(new MachineSequenceContext(host.Controller, new SequenceSignalBus()), false);
                _status.Text = "티칭 위치로 이동 중입니다.";
                int result = await sequence.MoveTeachingPositionOnlyAsync(CancellationToken.None).ConfigureAwait(true);
                LoadFromMachine();
                _status.Text = result == 0
                    ? "티칭 위치 이동 완료. 위치가 맞으면 START CAL을 실행하세요."
                    : "티칭 위치 이동 실패: " + sequence.Result.Message;
            }
            catch (Exception ex)
            {
                _status.Text = "티칭 위치 이동 예외: " + ex.Message;
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private async Task RunCalibrationAsync()
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);
                if (!SaveToMachine(false) || !CheckReady(false))
                    return;

                Form1 host = ResolveHost();
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new NeedlePinCalibrationSequence(context, false);
                _status.Text = "Needle Pin Calibration 실행 중입니다.";
                int result = await sequence.RunAsync(CancellationToken.None).ConfigureAwait(true);
                LoadFromMachine();

                if (result != 0)
                {
                    _status.Text = "Needle Pin Calibration 실패: " + sequence.Result.Message;
                    QMC.Common.MessageDialog.Show(this, _status.Text, "NEEDLE PIN CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                host.SaveMachineSettings();
                _status.Text = "완료. X Offset=" + sequence.Result.NeedleXToVisionXOffset.ToString("F6") +
                               ", Y Offset=" + sequence.Result.NeedleYToVisionYOffset.ToString("F6");
            }
            catch (Exception ex)
            {
                _status.Text = "Needle Pin Calibration 예외: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-PIN-CAL-DIALOG", _status.Text);
                QMC.Common.MessageDialog.Show(this, _status.Text, "NEEDLE PIN CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
            _btnSave.Enabled = enabled;
            _btnClose.Enabled = enabled;
            _settingsGrid.Enabled = enabled;
        }
    }
}
