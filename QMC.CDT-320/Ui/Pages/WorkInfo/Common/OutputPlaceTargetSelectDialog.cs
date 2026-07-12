using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.Common;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal sealed class OutputPlaceTargetSelectDialog : Form
    {
        private readonly MachineController _controller;
        private readonly PickerSequenceSide _side;
        private readonly BinSide _initialOutputSide;
        private readonly DieMapEntry _selectedEntry;
        private readonly int _selectedOrderIndex;
        private readonly int _selectedMapX;
        private readonly int _selectedMapY;
        private readonly double _selectedRelativeX;
        private readonly double _selectedRelativeY;
        private bool _busy;
        private bool _syncing;

        private TableLayoutPanel _root;
        private Label _header;
        private TableLayoutPanel _optionLayout;
        private ComboBox _cmbPickerNo;
        private ComboBox _cmbTargetMode;
        private ComboBox _cmbOutputSide;
        private Button _btnRefresh;
        private Label _lblLoadedDie;
        private Label _lblTarget;
        private DataGridView _gridTarget;
        private DataGridView _gridSteps;
        private Button _btnRunStep;
        private Button _btnNextStep;
        private Button _btnRunAll;
        private Button _btnClose;
        private Label _lblStatus;

        private sealed class PlaceStepItem
        {
            public int No { get; set; }
            public PickerPlaceManualStep Step { get; set; }
            public string Text { get; set; }
        }

        public OutputPlaceTargetSelectDialog(
            MachineController controller,
            PickerSequenceSide side,
            int defaultPickerNo,
            BinSide initialOutputSide,
            DieMapEntry selectedEntry,
            int selectedOrderIndex)
        {
            _controller = controller;
            _side = side;
            _initialOutputSide = initialOutputSide;
            _selectedEntry = selectedEntry;
            _selectedOrderIndex = Math.Max(0, selectedOrderIndex);
            _selectedMapX = ResolveEntryMapX(selectedEntry);
            _selectedMapY = ResolveEntryMapY(selectedEntry);
            _selectedRelativeX = selectedEntry != null ? selectedEntry.PosX - ResolveOutputVisionProcessX() : 0.0;
            _selectedRelativeY = selectedEntry != null ? selectedEntry.PosY - ResolveOutputStageProcessY(initialOutputSide) : 0.0;

            BuildUi();
            Text = SideName + " Output Place Test";
            _header.Text = SideName.ToUpperInvariant() + " OUTPUT DIE PLACE TEST";
            SetDefaultPickerNo(defaultPickerNo);
            ApplyRecommendedOutputSide();
            InitializeSteps();
            Load += (s, e) => RefreshTargetInfo();
        }

        private string SideName
        {
            get { return _side == PickerSequenceSide.Front ? "Front Picker" : "Rear Picker"; }
        }

        private void BuildUi()
        {
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(980, 640);
            MinimumSize = new Size(860, 560);

            _root = new TableLayoutPanel();
            _root.Dock = DockStyle.Fill;
            _root.ColumnCount = 1;
            _root.RowCount = 7;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));

            _header = new Label();
            _header.Dock = DockStyle.Fill;
            _header.BackColor = Color.FromArgb(230, 126, 0);
            _header.ForeColor = Color.White;
            _header.Font = new Font("맑은 고딕", 13F, FontStyle.Bold);
            _header.TextAlign = ContentAlignment.MiddleLeft;
            _header.Padding = new Padding(16, 0, 0, 0);

            _optionLayout = new TableLayoutPanel();
            _optionLayout.Dock = DockStyle.Fill;
            _optionLayout.ColumnCount = 9;
            _optionLayout.RowCount = 1;
            _optionLayout.Padding = new Padding(12, 8, 12, 6);
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            _optionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _cmbPickerNo = CreateCombo();
            _cmbTargetMode = CreateCombo();
            _cmbOutputSide = CreateCombo();
            _btnRefresh = CreateButton("REFRESH");
            _cmbTargetMode.Items.Add("AUTO TARGET");
            _cmbTargetMode.Items.Add("SELECTED SLOT");
            _cmbTargetMode.SelectedIndex = 0;
            _cmbOutputSide.Items.Add("GOOD");
            _cmbOutputSide.Items.Add("NG");
            _cmbOutputSide.SelectedIndex = _initialOutputSide == BinSide.Ng ? 1 : 0;
            _cmbTargetMode.SelectedIndexChanged += (s, e) => RefreshTargetInfo();
            _cmbOutputSide.SelectedIndexChanged += (s, e) => RefreshTargetInfo();
            _cmbPickerNo.SelectedIndexChanged += (s, e) => RefreshTargetInfo();
            _btnRefresh.Click += (s, e) => RefreshTargetInfo();

            _optionLayout.Controls.Add(CreateOptionLabel("Picker"), 0, 0);
            _optionLayout.Controls.Add(_cmbPickerNo, 1, 0);
            _optionLayout.Controls.Add(CreateOptionLabel("Mode"), 2, 0);
            _optionLayout.Controls.Add(_cmbTargetMode, 3, 0);
            _optionLayout.Controls.Add(CreateOptionLabel("Stage"), 4, 0);
            _optionLayout.Controls.Add(_cmbOutputSide, 5, 0);
            _optionLayout.Controls.Add(_btnRefresh, 6, 0);

            _lblLoadedDie = CreateInfoLabel();
            _lblTarget = CreateInfoLabel();
            _gridTarget = CreateGrid();
            _gridTarget.Columns.Add(CreateTextColumn("Item", "ITEM", 210));
            _gridTarget.Columns.Add(CreateTextColumn("Value", "VALUE", 700));

            _gridSteps = CreateGrid();
            _gridSteps.Columns.Add(CreateTextColumn("No", "No", 80));
            _gridSteps.Columns.Add(CreateTextColumn("Step", "Place Step", 820));
            _gridSteps.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridSteps.MultiSelect = false;

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(8, 8, 8, 8);
            _btnClose = CreateButton("CLOSE");
            _btnRunAll = CreateButton("RUN ALL");
            _btnNextStep = CreateButton("NEXT STEP");
            _btnRunStep = CreateButton("RUN STEP");
            _btnClose.Click += (s, e) => Close();
            _btnRunStep.Click += async (s, e) => await RunSelectedStepAsync(false).ConfigureAwait(true);
            _btnNextStep.Click += async (s, e) => await RunSelectedStepAsync(true).ConfigureAwait(true);
            _btnRunAll.Click += async (s, e) => await RunAllAsync().ConfigureAwait(true);
            buttons.Controls.Add(_btnClose);
            buttons.Controls.Add(_btnRunAll);
            buttons.Controls.Add(_btnNextStep);
            buttons.Controls.Add(_btnRunStep);

            _lblStatus = CreateInfoLabel();
            _lblStatus.Text = "대기 중입니다.";

            _root.Controls.Add(_header, 0, 0);
            _root.Controls.Add(_optionLayout, 0, 1);
            _root.Controls.Add(_lblLoadedDie, 0, 2);
            _root.Controls.Add(_lblTarget, 0, 3);
            _root.Controls.Add(_gridTarget, 0, 4);
            _root.Controls.Add(_gridSteps, 0, 5);
            _root.Controls.Add(buttons, 0, 6);
            Controls.Add(_root);
        }

        private static ComboBox CreateCombo()
        {
            ComboBox combo = new ComboBox();
            combo.Dock = DockStyle.Fill;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Font = new Font("맑은 고딕", 9F);
            return combo;
        }

        private static Label CreateOptionLabel(string text)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            return label;
        }

        private static Label CreateInfoLabel()
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            label.Padding = new Padding(12, 0, 12, 0);
            return label;
        }

        private static Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 128;
            button.Height = 34;
            button.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            return button;
        }

        private static DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            return grid;
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string header, int width)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = header;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            return column;
        }

        private void SetDefaultPickerNo(int pickerNo)
        {
            _cmbPickerNo.Items.Clear();
            for (int i = 1; i <= 4; i++)
                _cmbPickerNo.Items.Add(i.ToString(CultureInfo.InvariantCulture));

            int normalized = Math.Max(1, Math.Min(4, pickerNo));
            _cmbPickerNo.SelectedIndex = normalized - 1;
        }

        private void InitializeSteps()
        {
            _gridSteps.Rows.Clear();
            AddStep(1, PickerPlaceManualStep.PreparePlaceTarget, "Prepare Place Target");
            AddStep(2, PickerPlaceManualStep.MoveStagePickerToPlace, "Move OutputStage / Picker To Place");
            AddStep(3, PickerPlaceManualStep.VerifyPlaceTarget, "Verify Place Target");
            AddStep(4, PickerPlaceManualStep.MovePickerZPlace, "Move PickerZ Place");
            AddStep(5, PickerPlaceManualStep.VacuumOffBlow, "Vacuum OFF / Blow");
            AddStep(6, PickerPlaceManualStep.MovePickerZToAvoid, "Move PickerZ Avoid");
            AddStep(7, PickerPlaceManualStep.UpdateMaterialToOutputStage, "Update Material / Output Map");
            AddStep(8, PickerPlaceManualStep.RecoverAfterPlace, "Recover After Place");
            if (_gridSteps.Rows.Count > 0)
                _gridSteps.Rows[0].Selected = true;
        }

        private void AddStep(int no, PickerPlaceManualStep step, string text)
        {
            int row = _gridSteps.Rows.Add(no.ToString("00", CultureInfo.InvariantCulture), text);
            _gridSteps.Rows[row].Tag = new PlaceStepItem { No = no, Step = step, Text = text };
        }

        private void RefreshTargetInfo()
        {
            if (_syncing)
                return;

            try
            {
                _syncing = true;
                DieMaterial die = GetLoadedDie();
                if (die != null && _cmbOutputSide.SelectedIndex < 0)
                    _cmbOutputSide.SelectedIndex = die.Result == DieResult.NG ? 1 : 0;

                OutputStageReceiveTarget target;
                PlaceCoordinateResult coordinate;
                string reason;
                bool ok = TryBuildCurrentTarget(out target, out coordinate, out reason);

                _gridTarget.Rows.Clear();
                _lblLoadedDie.Text = die != null
                    ? "Loaded Die: " + die.DieId + " / Result=" + die.Result + " / Picker=" + SideName + " #" + GetPickerNo()
                    : "Loaded Die: - / Picker=" + SideName + " #" + GetPickerNo();

                if (!ok)
                {
                    _lblTarget.Text = "Target: " + reason;
                    AddInfo("Status", reason);
                    return;
                }

                string mode = IsAutoTargetMode() ? "AUTO TARGET" : "SELECTED SLOT";
                _lblTarget.Text =
                    "Target: " + mode +
                    " / " + GetOutputSide() +
                    " / order=" + target.OrderIndex +
                    " / map=" + target.DieMapX + "," + target.DieMapY +
                    " / target=" + target.TargetX.ToString("F3") + "," + target.TargetY.ToString("F3") + " mm";

                AddInfo("Target Mode", mode);
                AddInfo("Output Stage", GetOutputSide().ToString());
                AddInfo("Target Order", target.OrderIndex.ToString(CultureInfo.InvariantCulture));
                AddInfo("Target Map X/Y", target.DieMapX + " / " + target.DieMapY);
                AddInfo("Target X/Y", target.TargetX.ToString("F6") + " / " + target.TargetY.ToString("F6") + " mm");
                AddInfo("Final OutputStageY", coordinate.OutputStageY.ToString("F6") + " mm");
                AddInfo("Final PickerX", coordinate.PickerX.ToString("F6") + " mm");
                AddInfo("Final PickerY", coordinate.PickerY.ToString("F6") + " mm");
                AddInfo("Final PickerT", coordinate.PickerT.ToString("F6") + " deg");
                AddInfo("Final PickerZ", coordinate.PickerZ.ToString("F6") + " mm");
                AddInfo("Formula", coordinate.Formula ?? "");
            }
            finally
            {
                _syncing = false;
            }
        }

        private void ApplyRecommendedOutputSide()
        {
            try
            {
                _syncing = true;
                DieMaterial die = GetLoadedDie();
                if (die != null && die.Result == DieResult.NG)
                    _cmbOutputSide.SelectedIndex = 1;
                else if (die != null && die.Result == DieResult.Good)
                    _cmbOutputSide.SelectedIndex = 0;
                else
                    _cmbOutputSide.SelectedIndex = _initialOutputSide == BinSide.Ng ? 1 : 0;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void AddInfo(string item, string value)
        {
            _gridTarget.Rows.Add(item, value ?? "");
        }

        private async Task RunSelectedStepAsync(bool moveNext)
        {
            if (_busy)
                return;

            OutputStageReceiveTarget target;
            PlaceCoordinateResult coordinate;
            string reason;
            if (!TryBuildCurrentTarget(out target, out coordinate, out reason))
            {
                MessageDialog.Show(this, reason, "Output Place Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PlaceStepItem item = GetSelectedStep();
            if (item == null)
            {
                MessageDialog.Show(this, "실행할 Place Step이 선택되지 않았습니다.", "Output Place Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (item.Step == PickerPlaceManualStep.UpdateMaterialToOutputStage)
            {
                DialogResult confirm = MessageDialog.Show(this,
                    "Material/Output Map 상태를 저장합니다.\r\n" +
                    "Die=" + ResolveLoadedDieId() + "\r\n" +
                    "Stage=" + GetOutputSide() + "\r\n" +
                    "Order=" + target.OrderIndex + "\r\n" +
                    "진행하시겠습니까?",
                    "Output Place Test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;
            }

            await RunWithBusyAsync(async () =>
            {
                int result = await _controller.RunManualPickerSelectedOutputSlotPlaceStepAsync(
                    _side,
                    GetPickerNo(),
                    GetOutputSide(),
                    target,
                    item.Step).ConfigureAwait(true);
                if (result != 0)
                {
                    ShowRunFailed(result);
                    return;
                }

                _lblStatus.Text = "Step complete: " + item.Text;
                if (moveNext)
                    MoveNextStepSelection();
                RefreshTargetInfo();
            }).ConfigureAwait(true);
        }

        private async Task RunAllAsync()
        {
            if (_busy)
                return;

            OutputStageReceiveTarget target;
            PlaceCoordinateResult coordinate;
            string reason;
            if (!TryBuildCurrentTarget(out target, out coordinate, out reason))
            {
                MessageDialog.Show(this, reason, "Output Place Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageDialog.Show(this,
                "선택 조건으로 Place 전체 동작을 실행하고 Material/Output Map을 저장합니다.\r\n" +
                "Mode=" + (IsAutoTargetMode() ? "AUTO TARGET" : "SELECTED SLOT") + "\r\n" +
                "Die=" + ResolveLoadedDieId() + "\r\n" +
                "Stage=" + GetOutputSide() + "\r\n" +
                "Order=" + target.OrderIndex + "\r\n" +
                "Map=" + target.DieMapX + "," + target.DieMapY + "\r\n" +
                "진행하시겠습니까?",
                "Output Place Test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            await RunWithBusyAsync(async () =>
            {
                int result = await _controller.RunManualPickerSelectedOutputSlotPlaceAsync(
                    _side,
                    GetPickerNo(),
                    GetOutputSide(),
                    target).ConfigureAwait(true);
                if (result != 0)
                {
                    ShowRunFailed(result);
                    return;
                }

                _lblStatus.Text = "Place 전체 동작 완료.";
                RefreshTargetInfo();
            }).ConfigureAwait(true);
        }

        private async Task RunWithBusyAsync(Func<Task> action)
        {
            try
            {
                _busy = true;
                SetControlsEnabled(false);
                if (action != null)
                    await action().ConfigureAwait(true);
            }
            finally
            {
                _busy = false;
                SetControlsEnabled(true);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            _cmbPickerNo.Enabled = enabled;
            _cmbTargetMode.Enabled = enabled;
            _cmbOutputSide.Enabled = enabled;
            _btnRefresh.Enabled = enabled;
            _btnRunStep.Enabled = enabled;
            _btnNextStep.Enabled = enabled;
            _btnRunAll.Enabled = enabled;
            _btnClose.Enabled = enabled;
        }

        private void ShowRunFailed(int result)
        {
            string message = _controller != null ? _controller.LastActionFailureMessage : "";
            MessageDialog.Show(this,
                "Place 테스트 실패.\r\nresult=" + result +
                (string.IsNullOrWhiteSpace(message) ? "" : "\r\n" + message),
                "Output Place Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void MoveNextStepSelection()
        {
            if (_gridSteps.CurrentRow == null)
                return;

            int next = _gridSteps.CurrentRow.Index + 1;
            if (next >= _gridSteps.Rows.Count)
                return;

            _gridSteps.ClearSelection();
            _gridSteps.Rows[next].Selected = true;
            _gridSteps.CurrentCell = _gridSteps.Rows[next].Cells[0];
        }

        private PlaceStepItem GetSelectedStep()
        {
            if (_gridSteps.CurrentRow != null)
                return _gridSteps.CurrentRow.Tag as PlaceStepItem;

            if (_gridSteps.SelectedRows.Count > 0)
                return _gridSteps.SelectedRows[0].Tag as PlaceStepItem;

            return null;
        }

        private bool TryBuildCurrentTarget(
            out OutputStageReceiveTarget target,
            out PlaceCoordinateResult coordinate,
            out string reason)
        {
            target = null;
            coordinate = null;
            reason = string.Empty;

            if (_controller == null || _controller.Machine == null || _controller.Machine.OutputStageUnit == null)
            {
                reason = "Controller 또는 OutputStage 장비 정보를 찾을 수 없습니다.";
                return false;
            }

            DieMaterial die = GetLoadedDie();
            if (die == null)
            {
                reason = "선택 Picker가 들고 있는 Die가 없습니다.";
                return false;
            }

            BinSide outputSide = GetOutputSide();
            target = IsAutoTargetMode()
                ? MaterialStateService.PeekNextOutputStageReceiveTarget(outputSide)
                : BuildSelectedSlotReceiveTarget(outputSide);
            if (target == null)
            {
                reason = IsAutoTargetMode()
                    ? "AUTO TARGET 대상 슬롯을 찾을 수 없습니다. Output receive plan을 확인하세요."
                    : "선택 Slot 대상 정보를 만들 수 없습니다.";
                return false;
            }

            int pickerIndex = GetPickerNo() - 1;
            double offsetX;
            double offsetY;
            string offsetReason;
            if (!PickerCoordinateTransformHelper.TryResolveOutputVisionToPickerOffsets(
                _controller.Machine,
                _side,
                pickerIndex,
                outputSide,
                out offsetX,
                out offsetY,
                out offsetReason))
            {
                reason = offsetReason;
                return false;
            }

            coordinate = PickerMotionTargetResolver.CalculateOutputPlaceTarget(
                _controller.Machine,
                _side,
                pickerIndex,
                "OutputPlaceTargetSelectDialog",
                die.DieId,
                outputSide,
                ResolveOutputStageProcessY(outputSide),
                target.TargetX,
                target.TargetY,
                ResolveOutputVisionProcessX(),
                offsetX,
                offsetY);
            return true;
        }

        private OutputStageReceiveTarget BuildSelectedSlotReceiveTarget(BinSide outputSide)
        {
            if (_selectedEntry == null)
                return null;

            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(
                outputSide == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood);
            if (wafer == null)
                return null;

            return new OutputStageReceiveTarget
            {
                StageLocation = outputSide == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood,
                OutputWaferId = wafer != null ? wafer.WaferId : "",
                SourceWaferId = wafer != null ? wafer.OutputReceiveSourceWaferId : "",
                OrderIndex = _selectedOrderIndex,
                DieMapX = _selectedMapX,
                DieMapY = _selectedMapY,
                OffsetX = _selectedRelativeX,
                OffsetY = _selectedRelativeY,
                TargetX = _selectedRelativeX,
                TargetY = _selectedRelativeY
            };
        }

        private DieMaterial GetLoadedDie()
        {
            return MaterialStateService.GetDieAtPicker(
                _side == PickerSequenceSide.Front ? MaterialLocationKind.PickerFront : MaterialLocationKind.PickerRear,
                GetPickerNo());
        }

        private string ResolveLoadedDieId()
        {
            DieMaterial die = GetLoadedDie();
            return die != null ? die.DieId : "-";
        }

        private int GetPickerNo()
        {
            int pickerNo;
            if (_cmbPickerNo != null &&
                _cmbPickerNo.SelectedItem != null &&
                int.TryParse(_cmbPickerNo.SelectedItem.ToString(), out pickerNo))
            {
                if (pickerNo < 1)
                    return 1;
                if (pickerNo > 4)
                    return 4;
                return pickerNo;
            }

            return 1;
        }

        private bool IsAutoTargetMode()
        {
            return _cmbTargetMode == null || _cmbTargetMode.SelectedIndex <= 0;
        }

        private BinSide GetOutputSide()
        {
            return _cmbOutputSide != null && _cmbOutputSide.SelectedIndex == 1 ? BinSide.Ng : BinSide.Good;
        }

        private double ResolveOutputVisionProcessX()
        {
            if (_controller == null || _controller.Machine == null || _controller.Machine.OutputStageUnit == null)
                return 0.0;

            OutputStageUnit unit = _controller.Machine.OutputStageUnit;
            return unit.Recipe != null && unit.Recipe.VisionX != null
                ? unit.Recipe.VisionX.ProcessPosition
                : 0.0;
        }

        private double ResolveOutputStageProcessY(BinSide side)
        {
            if (_controller == null || _controller.Machine == null || _controller.Machine.OutputStageUnit == null)
                return 0.0;

            OutputStageUnit unit = _controller.Machine.OutputStageUnit;
            if (unit.Recipe == null)
                return 0.0;

            return side == BinSide.Ng
                ? unit.Recipe.NGStageY.ProcessPosition
                : unit.Recipe.GoodStageY.ProcessPosition;
        }

        private static int ResolveEntryMapX(DieMapEntry entry)
        {
            return entry != null ? DieMapGenerator.ResolveMapIndexX(entry) : 0;
        }

        private static int ResolveEntryMapY(DieMapEntry entry)
        {
            return entry != null ? DieMapGenerator.ResolveMapIndexY(entry) : 0;
        }
    }
}
