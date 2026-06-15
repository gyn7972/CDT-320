using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class SharedRailXSetupDialog : Form
    {
        private readonly MachineController _controller;
        private readonly ToolTip _toolTip;
        private readonly System.Windows.Forms.Timer _statusTimer;
        private DataGridView _activeGrid;
        private CancellationTokenSource _runCts;
        private bool _syncingGridSelection;
        private bool _stopping;
        private SharedRailXConfigDocument _document;
        private bool _running;
        private DataGridView _pairGrid;
        private Label _pairRuleLabel;

        public SharedRailXSetupDialog(MachineController controller)
        {
            _controller = controller;
            InitializeComponent();
            _toolTip = new ToolTip();
            _statusTimer = new System.Windows.Forms.Timer();
            _statusTimer.Interval = 200;
            _statusTimer.Tick += statusTimer_Tick;
            BuildSplitGridLayout();
            ConfigureGridColumnGroups();
            ConfigureActionButtons();
            InitializeHelpText();
        }

        private void SharedRailXSetupDialog_Load(object sender, EventArgs e)
        {
            ReloadDocument();
            InitializeHeadSubSelectors();
            RefreshStatus();
            _statusTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                _statusTimer.Stop();
                _statusTimer.Tick -= statusTimer_Tick;
            }
            finally
            {
                base.OnFormClosed(e);
            }
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            RefreshStatus();
        }

        private void BuildSplitGridLayout()
        {
            grid.Columns.Clear();
            ConfigureGridBase(grid);
            grid.Columns.Add(CreateTextColumn("colAxis", "Axis", 95F, true, DataGridViewContentAlignment.MiddleLeft));
            grid.Columns.Add(CreateTextColumn("colUnit", "Unit", 45F, true, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(CreateTextColumn("colCurrent", "Current Pos", 80F, true, DataGridViewContentAlignment.MiddleRight));
            grid.Columns.Add(CreateTextColumn("colStatus", "Axis State", 80F, true, DataGridViewContentAlignment.MiddleCenter));
            grid.Columns.Add(CreateTextColumn("colMessage", "Message", 160F, true, DataGridViewContentAlignment.MiddleLeft));

            _testGrid.Columns.Clear();
            ConfigureGridBase(_testGrid);
            _testGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _testGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
            _testGrid.CellClick += editableGrid_CellClick;
            _testGrid.Columns.Add(CreateTextColumn("colAxis", "Axis", 90F, true, DataGridViewContentAlignment.MiddleLeft));
            _testGrid.Columns.Add(CreateTextColumn("colUnit", "Unit", 45F, true, DataGridViewContentAlignment.MiddleCenter));
            _testGrid.Columns.Add(CreateTextColumn("colTarget", "Move Target", 90F, false, DataGridViewContentAlignment.MiddleRight));
            _testGrid.Columns.Add(CreateTextColumn("colVelocity", "Velocity", 80F, false, DataGridViewContentAlignment.MiddleRight));

            _settingGrid.Columns.Clear();
            ConfigureGridBase(_settingGrid);
            _settingGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _settingGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
            _settingGrid.CellClick += editableGrid_CellClick;
            _settingGrid.Columns.Add(CreateTextColumn("colAxis", "Axis", 90F, true, DataGridViewContentAlignment.MiddleLeft));
            _settingGrid.Columns.Add(CreateTextColumn("colUnit", "Unit", 45F, true, DataGridViewContentAlignment.MiddleCenter));
            _settingGrid.Columns.Add(CreateTextColumn("colBodyMin", "Body Min", 75F, false, DataGridViewContentAlignment.MiddleRight));
            _settingGrid.Columns.Add(CreateTextColumn("colBodyMax", "Body Max", 75F, false, DataGridViewContentAlignment.MiddleRight));
            _settingGrid.Columns.Add(CreateTextColumn("colOrigin", "Rail Origin", 85F, false, DataGridViewContentAlignment.MiddleRight));
            _settingGrid.Columns.Add(CreateTextColumn("colScale", "Scale", 60F, false, DataGridViewContentAlignment.MiddleRight));
            _settingGrid.Columns.Add(CreateTextColumn("colSafety", "Safety", 70F, false, DataGridViewContentAlignment.MiddleRight));

            _pairRuleLabel = new Label();
            _pairRuleLabel.BackColor = Color.FromArgb(238, 242, 246);
            _pairRuleLabel.Dock = DockStyle.Fill;
            _pairRuleLabel.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            _pairRuleLabel.Location = new Point(0, 0);
            _pairRuleLabel.Name = "lblPairRule";
            _pairRuleLabel.Padding = new Padding(6, 0, 0, 0);
            _pairRuleLabel.Text = "PAIR CLEARANCE RULE";
            _pairRuleLabel.TextAlign = ContentAlignment.MiddleLeft;

            _pairGrid = new DataGridView();
            ConfigureGridBase(_pairGrid);
            _pairGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _pairGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
            _pairGrid.CellClick += pairGrid_CellClick;
            _pairGrid.Columns.Add(CreateTextColumn("colPair", "Pair", 150F, true, DataGridViewContentAlignment.MiddleLeft));
            _pairGrid.Columns.Add(CreateTextColumn("colHomeClearance", "Home Gap", 80F, false, DataGridViewContentAlignment.MiddleRight));
            _pairGrid.Columns.Add(CreateTextColumn("colAxisASign", "A Sign", 60F, false, DataGridViewContentAlignment.MiddleRight));
            _pairGrid.Columns.Add(CreateTextColumn("colAxisBSign", "B Sign", 60F, false, DataGridViewContentAlignment.MiddleRight));
            _pairGrid.Columns.Add(CreateTextColumn("colPairSafety", "Safety", 70F, false, DataGridViewContentAlignment.MiddleRight));

            _gridGroups.ColumnStyles.Clear();
            _gridGroups.ColumnCount = 4;
            _gridGroups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            _gridGroups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            _gridGroups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _gridGroups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _gridGroups.Controls.Add(_pairRuleLabel, 3, 0);
            _gridGroups.Controls.Add(_pairGrid, 3, 1);

            _activeGrid = grid;
        }

        private void ConfigureGridBase(DataGridView view)
        {
            view.AllowUserToAddRows = false;
            view.AllowUserToDeleteRows = false;
            view.AllowUserToOrderColumns = false;
            view.AllowUserToResizeRows = false;
            view.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            view.BackgroundColor = Color.White;
            view.BorderStyle = BorderStyle.Fixed3D;
            view.ColumnHeadersHeight = 34;
            view.Dock = DockStyle.Fill;
            view.EnableHeadersVisualStyles = false;
            view.MultiSelect = false;
            view.RowHeadersVisible = false;
            view.RowTemplate.Height = 28;
            view.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            view.SelectionChanged += groupedGrid_SelectionChanged;
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string headerText, float fillWeight, bool readOnly, DataGridViewContentAlignment alignment)
        {
            DataGridViewCellStyle style = new DataGridViewCellStyle();
            style.Alignment = alignment;

            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = headerText;
            column.FillWeight = fillWeight;
            column.ReadOnly = readOnly;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.DefaultCellStyle = style;
            return column;
        }

        private void groupedGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (_syncingGridSelection)
                return;

            DataGridView source = sender as DataGridView;
            if (source == null || source.CurrentRow == null)
                return;

            SharedRailXAxis axis;
            if (!TryGetRowAxis(source.CurrentRow, out axis))
                return;

            _activeGrid = source;
            SelectAxisAcrossGrids(axis, source);
        }

        private void editableGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_running || e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                DataGridView view = sender as DataGridView;
                if (view == null)
                    return;

                DataGridViewColumn column = view.Columns[e.ColumnIndex];
                if (column == null || column.ReadOnly)
                    return;

                DataGridViewRow row = view.Rows[e.RowIndex];
                SharedRailXAxis axis;
                if (!TryGetRowAxis(row, out axis))
                    return;

                view.CurrentCell = row.Cells[e.ColumnIndex];
                _activeGrid = view;
                SelectAxisAcrossGrids(axis, view);

                string title = axis + " / " + column.HeaderText;
                string current = Convert.ToString(row.Cells[e.ColumnIndex].Value);
                string unit = ResolveParameterUnit(axis, column.Name);
                using (var dlg = new NumericKeypadDialog(title, current, unit))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    double value = ReadDouble(dlg.ValueText, 0.0);
                    row.Cells[e.ColumnIndex].Value = FormatDouble(value);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Numeric input failed:\n" + ex.Message,
                    "SharedRailX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void pairGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_running || e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                DataGridView view = sender as DataGridView;
                if (view == null)
                    return;

                DataGridViewColumn column = view.Columns[e.ColumnIndex];
                if (column == null || column.ReadOnly)
                    return;

                DataGridViewRow row = view.Rows[e.RowIndex];
                SharedRailXCollisionPairRow pair = row.Tag as SharedRailXCollisionPairRow;
                if (pair == null)
                    return;

                view.CurrentCell = row.Cells[e.ColumnIndex];
                string title = Convert.ToString(row.Cells["colPair"].Value) + " / " + column.HeaderText;
                string current = Convert.ToString(row.Cells[e.ColumnIndex].Value);
                string unit = column.Name == "colAxisASign" || column.Name == "colAxisBSign" ? "" : "mm";
                using (var dlg = new NumericKeypadDialog(title, current, unit))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;

                    double value = ReadDouble(dlg.ValueText, 0.0);
                    if (column.Name == "colAxisASign" || column.Name == "colAxisBSign")
                        value = NormalizeSign(value);
                    row.Cells[e.ColumnIndex].Value = FormatDouble(value);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Pair rule input failed:\n" + ex.Message,
                    "SharedRailX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string ResolveParameterUnit(SharedRailXAxis axis, string columnName)
        {
            switch (columnName)
            {
                // 속도 컬럼 단위 표시
                case "colVelocity":
                    return DisplayUnitForAxis(axis) + "/sec";
                // Scale 컬럼은 단위 없음
                case "colScale":
                    return "";
                default:
                    return DisplayUnitForAxis(axis);
            }
        }

        private void SelectAxisAcrossGrids(SharedRailXAxis axis)
        {
            SelectAxisAcrossGrids(axis, null);
        }

        private void SelectAxisAcrossGrids(SharedRailXAxis axis, DataGridView source)
        {
            try
            {
                _syncingGridSelection = true;
                SelectGridAxis(grid, axis, source == grid);
                SelectGridAxis(_testGrid, axis, source == _testGrid);
                SelectGridAxis(_settingGrid, axis, source == _settingGrid);
            }
            finally
            {
                _syncingGridSelection = false;
            }
        }

        private static void SelectGridAxis(DataGridView view, SharedRailXAxis axis, bool preserveCurrentCell)
        {
            if (view == null)
                return;

            foreach (DataGridViewRow row in view.Rows)
            {
                SharedRailXAxis rowAxis;
                if (TryGetRowAxis(row, out rowAxis) && rowAxis == axis)
                {
                    row.Selected = true;
                    if (!preserveCurrentCell)
                        view.CurrentCell = row.Cells[0];
                }
                else
                {
                    row.Selected = false;
                }
            }
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            if (_running) return;
            ReloadDocument();
            RefreshStatus();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_running) return;
            if (!ReadGridToDocument()) return;
            SharedRailXConfigStore.SaveDocument(_document);
            lblStatus.Text = "Saved: " + SharedRailXConfigStore.Path_;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_running) return;
            if (!ReadGridToDocument()) return;
            SharedRailXConfigStore.SaveDocument(_document);
            if (_controller != null)
                _controller.ReloadSharedRailXConfig();
            lblStatus.Text = "Applied to runtime.";
            RefreshStatus();
        }

        private void btnValidate_Click(object sender, EventArgs e)
        {
            if (_running) return;
            if (!ReadGridToDocument()) return;
            SharedRailXConfigStore.SaveDocument(_document);
            if (_controller != null)
                _controller.ReloadSharedRailXConfig();

            int checkedCount = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                SharedRailXAxis axis;
                if (!TryGetRowAxis(row, out axis))
                    continue;

                DataGridViewRow testRow = FindRow(_testGrid, axis);
                string reason;
                bool ok = ValidateTarget(axis, ReadAxisDisplayCell(testRow, "colTarget", axis, 0.0), out reason);
                SetRowStatus(row, ok ? "OK" : "BLOCK", ok ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225), reason);
                checkedCount++;
            }

            lblStatus.Text = "Validated " + checkedCount + " axis targets.";
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            ShowSharedRailXHelp();
        }

        private async void btnMoveSelected_Click(object sender, EventArgs e)
        {
            await RunSelectedMoveAsync();
        }

        private async void btnMoveHeadSub_Click(object sender, EventArgs e)
        {
            await RunHeadSubMoveAsync();
        }

        private async void btnHomeSelected_Click(object sender, EventArgs e)
        {
            await RunSelectedHomeAsync();
        }

        private async void btnHomeAll_Click(object sender, EventArgs e)
        {
            await RunAllHomeAsync();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshStatus();
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            if (_stopping || _controller == null)
                return;

            try
            {
                _stopping = true;
                btnStop.Enabled = false;
                lblStatus.Text = "STOP requested...";
                CancellationTokenSource cts = _runCts;
                if (cts != null)
                    cts.Cancel();
                int result = await _controller.StopAllAxesAsync(false).ConfigureAwait(true);
                RefreshStatus();
                lblStatus.Text = result == 0 ? "STOP complete." : "STOP failed. result=" + result;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "STOP error.";
                QMC.Common.MessageDialog.Show(this, "STOP failed:\n" + ex.Message,
                    "SharedRailX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _stopping = false;
                btnStop.Enabled = true;
            }
        }

        private void cboHeadAxis_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyHeadSubRules();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void InitializeHelpText()
        {
            grid.ShowCellToolTips = true;
            if (_testGrid != null)
                _testGrid.ShowCellToolTips = true;
            if (_settingGrid != null)
                _settingGrid.ShowCellToolTips = true;
            if (_pairGrid != null)
                _pairGrid.ShowCellToolTips = true;

            GetColumn(_settingGrid, "colBodyMin").ToolTipText = "Axis body negative side size from rail center position.";
            GetColumn(_settingGrid, "colBodyMax").ToolTipText = "Axis body positive side size from rail center position.";
            GetColumn(_settingGrid, "colOrigin").ToolTipText = "SharedRail coordinate offset for this axis zero position.";
            GetColumn(_settingGrid, "colScale").ToolTipText = "Scale used when converting axis position to SharedRail position.";
            GetColumn(_settingGrid, "colSafety").ToolTipText = "Axis-specific minimum safety distance. Empty uses Default Safety.";
            GetColumn(_testGrid, "colVelocity").ToolTipText = "Test move velocity used by this setup dialog.";
            GetColumn(_pairGrid, "colHomeClearance").ToolTipText = "Distance between the two axes at home position before they approach each other.";
            GetColumn(_pairGrid, "colAxisASign").ToolTipText = "+1 means AxisA positive direction approaches AxisB, -1 means negative direction approaches.";
            GetColumn(_pairGrid, "colAxisBSign").ToolTipText = "+1 means AxisB positive direction approaches AxisA, -1 means negative direction approaches.";
            GetColumn(_pairGrid, "colPairSafety").ToolTipText = "Pair-specific minimum clearance. Empty uses Default Safety.";

            colBodyMin.ToolTipText = "축의 실제 몸체가 Rail 좌표에서 축 중심보다 얼마나 왼쪽/마이너스 방향으로 더 차지하는지(mm)입니다.";
            colBodyMax.ToolTipText = "축의 실제 몸체가 Rail 좌표에서 축 중심보다 얼마나 오른쪽/플러스 방향으로 더 차지하는지(mm)입니다.";
            colOrigin.ToolTipText = "축 좌표 0이 SharedRail 기준 좌표에서 어디에 있는지 나타내는 기준 오프셋(mm)입니다.";
            colScale.ToolTipText = "축 좌표를 SharedRail 좌표로 환산할 때 곱하는 배율입니다. 일반 X축은 보통 1입니다.";
            colSafety.ToolTipText = "이 축에만 적용할 최소 이격 거리(mm)입니다. 비우면 Default Safety 값을 사용합니다.";
            colVelocity.ToolTipText = "이 창에서 테스트 이동할 때 사용할 속도(mm/sec)입니다. 저장하면 파일에 기록되고 이동 실행 시 이 값을 사용합니다.";

            _toolTip.SetToolTip(cboHeadAxis, "주 축입니다. Move Head/Sub 실행 시 항상 이동 대상에 포함됩니다.");
            _toolTip.SetToolTip(chkSubInputVision, "Head 축과 같이 이동할 Sub 축입니다. Head 선택에 따라 선택 가능 여부가 달라집니다.");
            _toolTip.SetToolTip(chkSubOutputVision, "Head 축과 같이 이동할 Sub 축입니다. Head 선택에 따라 선택 가능 여부가 달라집니다.");
            _toolTip.SetToolTip(chkSubFrontPicker, "Head 축과 같이 이동할 Sub 축입니다. Head 선택에 따라 선택 가능 여부가 달라집니다.");
            _toolTip.SetToolTip(chkSubRearPicker, "Head 축과 같이 이동할 Sub 축입니다. Head 선택에 따라 선택 가능 여부가 달라집니다.");
            _toolTip.SetToolTip(btnHelp, "SharedRailX 설정값 설명을 표시합니다.");
        }

        private void ConfigureGridColumnGroups()
        {
            Color statusColor = Color.FromArgb(232, 242, 255);
            Color testColor = Color.FromArgb(255, 248, 220);
            Color settingColor = Color.FromArgb(246, 246, 246);

            grid.ColumnHeadersHeight = 42;
            _testGrid.ColumnHeadersHeight = 42;
            _settingGrid.ColumnHeadersHeight = 42;

            ApplyColumnGroup(GetColumn(grid, "colAxis"), "Axis", Color.White, true);
            ApplyColumnGroup(GetColumn(grid, "colUnit"), "Unit", statusColor, true);
            ApplyColumnGroup(GetColumn(grid, "colCurrent"), "Current Pos", statusColor, true);
            ApplyColumnGroup(GetColumn(grid, "colStatus"), "Axis State", statusColor, true);
            ApplyColumnGroup(GetColumn(grid, "colMessage"), "Message", statusColor, true);

            ApplyColumnGroup(GetColumn(_testGrid, "colAxis"), "Axis", Color.White, true);
            ApplyColumnGroup(GetColumn(_testGrid, "colUnit"), "Unit", testColor, true);
            ApplyColumnGroup(GetColumn(_testGrid, "colTarget"), "Move Target", testColor, false);
            ApplyColumnGroup(GetColumn(_testGrid, "colVelocity"), "Velocity", testColor, false);

            ApplyColumnGroup(GetColumn(_settingGrid, "colAxis"), "Axis", Color.White, true);
            ApplyColumnGroup(GetColumn(_settingGrid, "colUnit"), "Unit", settingColor, true);
            ApplyColumnGroup(GetColumn(_settingGrid, "colBodyMin"), "Body Min", settingColor, false);
            ApplyColumnGroup(GetColumn(_settingGrid, "colBodyMax"), "Body Max", settingColor, false);
            ApplyColumnGroup(GetColumn(_settingGrid, "colOrigin"), "Rail Origin", settingColor, false);
            ApplyColumnGroup(GetColumn(_settingGrid, "colScale"), "Scale", settingColor, false);
            ApplyColumnGroup(GetColumn(_settingGrid, "colSafety"), "Safety", settingColor, false);

            ApplyColumnGroup(GetColumn(_pairGrid, "colPair"), "Pair", Color.White, true);
            ApplyColumnGroup(GetColumn(_pairGrid, "colHomeClearance"), "Home Gap", settingColor, false);
            ApplyColumnGroup(GetColumn(_pairGrid, "colAxisASign"), "A Sign", settingColor, false);
            ApplyColumnGroup(GetColumn(_pairGrid, "colAxisBSign"), "B Sign", settingColor, false);
            ApplyColumnGroup(GetColumn(_pairGrid, "colPairSafety"), "Safety", settingColor, false);
        }

        private static void ApplyColumnGroup(DataGridViewColumn column, string headerText, Color backColor, bool readOnly)
        {
            if (column == null)
                return;

            column.HeaderText = headerText;
            column.ReadOnly = readOnly;
            column.DefaultCellStyle.BackColor = backColor;
            column.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            column.DefaultCellStyle.SelectionForeColor = Color.White;
        }

        private static DataGridViewColumn GetColumn(DataGridView view, string name)
        {
            if (view == null || string.IsNullOrEmpty(name) || !view.Columns.Contains(name))
                return null;
            return view.Columns[name];
        }

        private void ConfigureActionButtons()
        {
            btnReload.Text = "Reload File";
            btnSave.Text = "Save File";
            btnApply.Text = "Save + Apply";
            btnValidate.Text = "Validate";
            btnHelp.Text = "Help";
            btnMoveSelected.Text = "Move Selected";
            btnMoveHeadSub.Text = "Move Head/Sub";
            btnHomeSelected.Text = "Home Selected";
            btnHomeAll.Text = "Home All";
            btnRefresh.Text = "Refresh Now";
            btnStop.Text = "STOP";
            btnClose.Text = "Close";
        }

        private void ShowSharedRailXHelp()
        {
            string message =
                "SharedRailX 설정값 설명\r\n\r\n" +
                "Body Min\r\n" +
                "- 축의 중심 위치 기준으로 장비 몸체가 마이너스 방향으로 얼마나 튀어나와 있는지 나타냅니다.\r\n" +
                "- 예: Body Min = -100 이면, 축 중심보다 왼쪽/마이너스 방향으로 100mm까지 몸체가 있다고 봅니다.\r\n\r\n" +
                "Body Max\r\n" +
                "- 축의 중심 위치 기준으로 장비 몸체가 플러스 방향으로 얼마나 튀어나와 있는지 나타냅니다.\r\n" +
                "- 예: Body Max = 100 이면, 축 중심보다 오른쪽/플러스 방향으로 100mm까지 몸체가 있다고 봅니다.\r\n\r\n" +
                "Rail Origin\r\n" +
                "- 해당 축의 0 위치가 SharedRail 공통 좌표에서 어디에 있는지 나타내는 기준 위치입니다.\r\n" +
                "- 같은 축 좌표 0이라도 Rail Origin이 다르면 SharedRail 위 실제 위치는 다르게 계산됩니다.\r\n" +
                "- 예: FrontPickerX Rail Origin = 300 이면 FrontPickerX 축 0은 SharedRail 좌표 300mm 위치로 봅니다.\r\n\r\n" +
                "Scale\r\n" +
                "- 축 좌표를 SharedRail 공통 좌표로 변환할 때 곱하는 값입니다.\r\n" +
                "- 일반적으로 mm 단위 X축은 1을 사용합니다.\r\n" +
                "- 축 방향이 반대라면 -1 같은 값이 필요할 수 있지만, 실제 장비 좌표계 확인 후 설정해야 합니다.\r\n\r\n" +
                "Velocity\r\n" +
                "- 이 창에서 테스트 이동할 때 사용할 속도입니다. 단위는 mm/sec입니다.\r\n" +
                "- 저장하면 shared_rail_x.json 파일에 남고, 이동 버튼을 누를 때도 먼저 파일에 저장한 뒤 이 값을 사용합니다.\r\n\r\n" +
                "계산 개념\r\n" +
                "- Rail 중심 좌표 = Rail Origin + (축 위치 * Scale)\r\n" +
                "- 몸체 점유 범위 = Rail 중심 좌표 + Body Min ~ Rail 중심 좌표 + Body Max\r\n" +
                "- 두 축의 몸체 점유 범위가 Safety 거리보다 가까우면 이동을 막습니다.\r\n\r\n" +
                "주의\r\n" +
                "- 이 값은 충돌 인터락 계산 기준입니다. 실제 기구 치수와 축 좌표 방향이 맞지 않으면 정상 이동도 막히거나, 위험한 이동이 허용될 수 있습니다.\r\n\r\n" +
                "Pair Clearance Rule\r\n" +
                "- Pair Clearance Rule은 두 축 사이의 실제 간섭 거리 계산을 우선 적용합니다.\r\n" +
                "- Home Gap은 두 축이 홈 위치일 때의 실제 여유거리입니다.\r\n" +
                "- A/B Sign은 각 축이 상대 축 쪽으로 가까워지는 엔코더 방향입니다. +방향 접근은 1, -방향 접근은 -1입니다.\r\n" +
                "- Clearance = Home Gap - A Sign*A Pos - B Sign*B Pos 로 계산하며 Safety 이하이면 이동을 막습니다.";

            QMC.Common.MessageDialog.Show(this, message, "SharedRailX 설정값 설명",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ReloadDocument()
        {
            _document = SharedRailXConfigStore.LoadDocumentOrCreateDefault();
            chkPathCheck.Checked = _document.EnablePathCheck;
            chkSameVelocity.Checked = _document.RequireSameVelocityForGroupMove;
            txtDefaultSafety.Text = FormatDouble(_document.DefaultSafetyDistance);
            LoadGrid();
            lblPath.Text = SharedRailXConfigStore.Path_;
            lblStatus.Text = "Loaded.";
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            if (_testGrid != null)
                _testGrid.Rows.Clear();
            if (_settingGrid != null)
                _settingGrid.Rows.Clear();
            if (_pairGrid != null)
                _pairGrid.Rows.Clear();

            if (_document == null || _document.Axes == null)
                return;

            foreach (SharedRailXAxisGeometryRow item in _document.Axes.OrderBy(x => AxisOrder(x.Axis)))
            {
                SharedRailXAxis axis;
                Enum.TryParse(item.Axis, true, out axis);
                string unit = DisplayUnitForAxis(axis);
                int statusIndex = grid.Rows.Add(
                    item.Axis,
                    unit,
                    "",
                    "Waiting",
                    "");
                grid.Rows[statusIndex].Tag = item;

                int testIndex = _testGrid.Rows.Add(
                    item.Axis,
                    unit,
                    FormatAxisDisplayValue(axis, item.TestTargetPosition),
                    FormatAxisDisplayValue(axis, item.TestVelocity));
                _testGrid.Rows[testIndex].Tag = item;

                int settingIndex = _settingGrid.Rows.Add(
                    item.Axis,
                    unit,
                    FormatAxisDisplayValue(axis, item.BodyOffsetMin),
                    FormatAxisDisplayValue(axis, item.BodyOffsetMax),
                    FormatAxisDisplayValue(axis, item.RailOriginOffset),
                    FormatDouble(item.PositionScale),
                    item.SafetyDistance.HasValue ? FormatAxisDisplayValue(axis, item.SafetyDistance.Value) : "");
                _settingGrid.Rows[settingIndex].Tag = item;
            }

            if (_document.CollisionPairs != null && _pairGrid != null)
            {
                foreach (SharedRailXCollisionPairRow pair in _document.CollisionPairs)
                {
                    if (pair == null)
                        continue;

                    int pairIndex = _pairGrid.Rows.Add(
                        pair.AxisA + " <-> " + pair.AxisB,
                        FormatDouble(pair.HomeClearance),
                        FormatDouble(pair.AxisATowardSign),
                        FormatDouble(pair.AxisBTowardSign),
                        pair.SafetyDistance.HasValue ? FormatDouble(pair.SafetyDistance.Value) : "");
                    _pairGrid.Rows[pairIndex].Tag = pair;
                }
            }

            if (grid.Rows.Count > 0)
            {
                SharedRailXAxis axis;
                if (TryGetRowAxis(grid.Rows[0], out axis))
                    SelectAxisAcrossGrids(axis);
            }
        }

        private bool ReadGridToDocument()
        {
            try
            {
                if (_document == null)
                    _document = SharedRailXConfigStore.CreateDefaultDocument();

                _document.EnablePathCheck = chkPathCheck.Checked;
                _document.RequireSameVelocityForGroupMove = chkSameVelocity.Checked;
                _document.DefaultSafetyDistance = ReadDouble(txtDefaultSafety.Text, 10.0);

                foreach (DataGridViewRow row in _settingGrid.Rows)
                {
                    SharedRailXAxisGeometryRow item = row.Tag as SharedRailXAxisGeometryRow;
                    if (item == null)
                        continue;

                    SharedRailXAxis axis;
                    DataGridViewRow testRow = TryGetRowAxis(row, out axis) ? FindRow(_testGrid, axis) : null;

                    item.TestTargetPosition = ReadAxisDisplayCell(testRow, "colTarget", axis, item.TestTargetPosition);
                    item.TestVelocity = ReadAxisDisplayCell(testRow, "colVelocity", axis, item.TestVelocity);
                    item.BodyOffsetMin = ReadAxisDisplayCell(row, "colBodyMin", axis, item.BodyOffsetMin);
                    item.BodyOffsetMax = ReadAxisDisplayCell(row, "colBodyMax", axis, item.BodyOffsetMax);
                    item.RailOriginOffset = ReadAxisDisplayCell(row, "colOrigin", axis, item.RailOriginOffset);
                    item.PositionScale = ReadDouble(GetCellValue(row, "colScale"), item.PositionScale);

                    string safetyText = Convert.ToString(GetCellValue(row, "colSafety"));
                    item.SafetyDistance = string.IsNullOrWhiteSpace(safetyText)
                        ? (double?)null
                        : ReadAxisDisplayText(safetyText, axis, _document.DefaultSafetyDistance);
                }

                if (_pairGrid != null)
                {
                    foreach (DataGridViewRow row in _pairGrid.Rows)
                    {
                        SharedRailXCollisionPairRow pair = row.Tag as SharedRailXCollisionPairRow;
                        if (pair == null)
                            continue;

                        pair.HomeClearance = ReadDouble(GetCellValue(row, "colHomeClearance"), pair.HomeClearance);
                        pair.AxisATowardSign = NormalizeSign(ReadDouble(GetCellValue(row, "colAxisASign"), pair.AxisATowardSign));
                        pair.AxisBTowardSign = NormalizeSign(ReadDouble(GetCellValue(row, "colAxisBSign"), pair.AxisBTowardSign));

                        string pairSafetyText = Convert.ToString(GetCellValue(row, "colPairSafety"));
                        pair.SafetyDistance = string.IsNullOrWhiteSpace(pairSafetyText)
                            ? (double?)null
                            : ReadDouble(pairSafetyText, _document.DefaultSafetyDistance);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "SharedRailX setting read failed:\n" + ex.Message,
                    "SharedRailX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            finally
            {
            }
        }

        private void RefreshStatus()
        {
            try
            {
                if (_controller == null || _controller.SharedRailX == null)
                    return;

                IReadOnlyList<SharedRailXAxisSetting> settings = _controller.SharedRailX.GetAxisSettings();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    SharedRailXAxis axis;
                    if (!TryGetRowAxis(row, out axis))
                        continue;

                    SharedRailXAxisSetting setting = settings.FirstOrDefault(x => x.RailAxis == axis);
                    if (setting == null || setting.Axis == null)
                        continue;

                    SetCellValue(row, "colUnit", AxisUnitConverter.DisplayUnitFor(setting.Axis));
                    SetCellValue(row, "colCurrent", FormatAxisDisplayValue(setting.Axis, setting.Axis.ActualPosition));
                    SetCellValue(row, "colStatus", setting.Axis.IsAlarm ? "Alarm" :
                        setting.Axis.IsMoving ? "Moving" :
                        setting.Axis.IsHomeDone ? "HomeDone" : "Ready");
                    SetCellValue(row, "colMessage", "servo=" + (setting.Axis.IsServoOn ? "ON" : "OFF"));
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task RunSelectedMoveAsync()
        {
            SharedRailXAxis axis;
            if (!TryGetSelectedAxis(out axis))
                return;

            await RunAsync("Move " + axis, async ct =>
            {
                if (!ReadGridToDocument()) return -1;
                SaveDocumentAndReloadRuntime();
                ct.ThrowIfCancellationRequested();

                DataGridViewRow testRow = FindRow(_testGrid, axis);
                DataGridViewRow statusRow = FindRow(grid, axis);
                double target = ReadAxisDisplayCell(testRow, "colTarget", axis, 0.0);
                double velocity = ReadVelocity(testRow, axis);
                int result = await _controller.SharedRailX.MoveAsync(axis, target, velocity);
                ct.ThrowIfCancellationRequested();
                SetRowStatus(statusRow, result == 0 ? "Done" : "Failed", result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                    result == 0 ? "" : "result=" + result);
                return result;
            });
        }

        private async Task RunHeadSubMoveAsync()
        {
            await RunAsync("Move Head/Sub SharedRailX axes", async ct =>
            {
                if (!ReadGridToDocument()) return -1;
                SaveDocumentAndReloadRuntime();
                ct.ThrowIfCancellationRequested();

                List<DataGridViewRow> rows = GetHeadSubRows();
                if (rows.Count == 0)
                {
                    QMC.Common.MessageDialog.Show(this, "Head/Sub 선택된 축이 없습니다.", "SharedRailX",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                var plan = SharedRailXMovePlan.Create("SharedRailX setup dialog move Head/Sub", ReadDefaultVelocity())
                    .UseMode(rows.Count >= 2 ? SharedRailXMoveMode.AjinMultiPosition : SharedRailXMoveMode.SoftwareParallel);

                foreach (DataGridViewRow row in rows)
                {
                    SharedRailXAxis axis;
                    if (!TryGetRowAxis(row, out axis))
                        continue;

                    DataGridViewRow testRow = FindRow(_testGrid, axis);
                    plan.Add(axis, ReadAxisDisplayCell(testRow, "colTarget", axis, 0.0), ReadVelocity(testRow, axis));
                    SetRowStatus(row, "Moving", Color.FromArgb(255, 245, 205), "");
                }

                int result = await _controller.MoveSharedRailXAsync(plan);
                ct.ThrowIfCancellationRequested();
                foreach (DataGridViewRow row in rows)
                    SetRowStatus(row, result == 0 ? "Done" : "Failed",
                        result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                        result == 0 ? "" : "result=" + result);

                return result;
            });
        }

        private async Task RunSelectedHomeAsync()
        {
            DataGridViewRow row = grid.CurrentRow;
            if (row == null)
                return;

            SharedRailXAxis axis;
            if (!TryGetRowAxis(row, out axis))
                return;

            await RunAsync("Home " + axis, async ct =>
            {
                ct.ThrowIfCancellationRequested();
                int result = await _controller.InitializeAxisAsync(AxisName(axis));
                ct.ThrowIfCancellationRequested();
                SetRowStatus(row, result == 0 ? "Home Done" : "Home Failed", result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                    result == 0 ? "" : _controller.LastActionFailureMessage);
                return result;
            });
        }

        private async Task RunAllHomeAsync()
        {
            await RunAsync("Home all SharedRailX axes", async ct =>
            {
                int firstFail = 0;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    ct.ThrowIfCancellationRequested();
                    SharedRailXAxis axis;
                    if (!TryGetRowAxis(row, out axis))
                        continue;

                    SetRowStatus(row, "Homing", Color.FromArgb(255, 245, 205), "");
                    int result = await _controller.InitializeAxisAsync(AxisName(axis));
                    ct.ThrowIfCancellationRequested();
                    SetRowStatus(row, result == 0 ? "Home Done" : "Home Failed",
                        result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                        result == 0 ? "" : _controller.LastActionFailureMessage);
                    if (firstFail == 0 && result != 0)
                        firstFail = result;
                    if (result != 0)
                        break;
                }

                return firstFail;
            });
        }

        private async Task RunAsync(string actionName, Func<CancellationToken, Task<int>> action)
        {
            if (_running || action == null || _controller == null || _controller.SharedRailX == null)
                return;

            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                _runCts = cts;
                _running = true;
                SetButtonsEnabled(false);
                lblStatus.Text = actionName + " running...";
                Task<int> actionTask = action(cts.Token);
                Task cancelTask = WaitForCancellationAsync(cts.Token);
                Task completed = await Task.WhenAny(actionTask, cancelTask).ConfigureAwait(true);
                if (completed == cancelTask)
                {
                    ObserveRunTask(actionTask);
                    RefreshStatus();
                    lblStatus.Text = actionName + " stopped.";
                    return;
                }

                int result = await actionTask.ConfigureAwait(true);
                RefreshStatus();
                lblStatus.Text = actionName + (result == 0 ? " complete." : " failed. result=" + result);
            }
            catch (OperationCanceledException)
            {
                RefreshStatus();
                lblStatus.Text = actionName + " stopped.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = actionName + " error.";
                QMC.Common.MessageDialog.Show(this, actionName + " failed:\n" + ex.Message,
                    "SharedRailX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_runCts == cts)
                    _runCts = null;
                cts.Dispose();
                _running = false;
                SetButtonsEnabled(true);
            }
        }

        private static Task WaitForCancellationAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
                return Task.Delay(Timeout.Infinite);
            if (ct.IsCancellationRequested)
                return Task.FromResult(0);

            var tcs = new TaskCompletionSource<int>();
            ct.Register(() => tcs.TrySetResult(0));
            return tcs.Task;
        }

        private static void ObserveRunTask(Task<int> task)
        {
            if (task == null)
                return;

            task.ContinueWith(t =>
            {
                Exception ignored = t.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private bool ValidateTarget(SharedRailXAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (_controller == null || _controller.SharedRailX == null)
                {
                    reason = "Controller is not ready.";
                    return false;
                }

                BaseAxis baseAxis = ResolveAxis(axis);
                if (baseAxis == null)
                {
                    reason = "Axis is not mapped.";
                    return false;
                }

                return _controller.SharedRailX.VerifySingleAxisMove(baseAxis, target, out reason);
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private void SaveDocumentAndReloadRuntime()
        {
            SharedRailXConfigStore.SaveDocument(_document);
            if (_controller != null)
                _controller.ReloadSharedRailXConfig();
            lblStatus.Text = "Saved: " + SharedRailXConfigStore.Path_;
        }

        private double ReadVelocity(DataGridViewRow row, SharedRailXAxis axis)
        {
            double velocity = row != null
                ? ReadAxisDisplayCell(row, "colVelocity", axis, 5.0)
                : 5.0;
            return velocity > 0.0 ? velocity : 5.0;
        }

        private double ReadDefaultVelocity()
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                SharedRailXAxis axis;
                if (TryGetRowAxis(row, out axis))
                    return ReadVelocity(FindRow(_testGrid, axis), axis);
            }
            return 5.0;
        }

        private BaseAxis ResolveAxis(SharedRailXAxis axis)
        {
            if (_controller == null || _controller.Machine == null)
                return null;

            CDT320_Machine machine = _controller.Machine;
            switch (axis)
            {
                // 인풋 비전 X축 객체 반환
                case SharedRailXAxis.InputVisionX:
                    return machine.InputStageUnit != null ? machine.InputStageUnit.CameraX : null;
                // 프론트 피커 X축 객체 반환
                case SharedRailXAxis.FrontPickerX:
                    return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null;
                // 리어 피커 X축 객체 반환
                case SharedRailXAxis.RearPickerX:
                    return machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerX : null;
                // 아웃풋 비전 X축 객체 반환
                case SharedRailXAxis.OutputVisionX:
                    return machine.OutputStageUnit != null ? machine.OutputStageUnit.OutputCameraX : null;
                default:
                    return null;
            }
        }

        private string DisplayUnitForAxis(SharedRailXAxis axis)
        {
            return AxisUnitConverter.DisplayUnitFor(ResolveAxis(axis));
        }

        private double ToAxisDisplayValue(SharedRailXAxis axis, double nativeValue)
        {
            return AxisUnitConverter.ToDisplay(nativeValue, ResolveAxis(axis));
        }

        private double FromAxisDisplayValue(SharedRailXAxis axis, double displayValue)
        {
            return AxisUnitConverter.FromDisplay(displayValue, ResolveAxis(axis));
        }

        private string FormatAxisDisplayValue(SharedRailXAxis axis, double nativeValue)
        {
            string unit = DisplayUnitForAxis(axis);
            return AxisUnitConverter.Format(AxisUnitConverter.ToDisplay(nativeValue, unit), unit);
        }

        private static string FormatAxisDisplayValue(BaseAxis axis, double nativeValue)
        {
            string unit = AxisUnitConverter.DisplayUnitFor(axis);
            return AxisUnitConverter.Format(AxisUnitConverter.ToDisplay(nativeValue, unit), unit);
        }

        private double ReadAxisDisplayCell(DataGridViewRow row, string columnName, SharedRailXAxis axis, double fallbackNative)
        {
            string text = Convert.ToString(GetCellValue(row, columnName));
            return ReadAxisDisplayText(text, axis, fallbackNative);
        }

        private double ReadAxisDisplayText(string text, SharedRailXAxis axis, double fallbackNative)
        {
            double displayValue;
            if (!TryReadDouble(text, out displayValue))
                return fallbackNative;
            return FromAxisDisplayValue(axis, displayValue);
        }

        private static string AxisName(SharedRailXAxis axis)
        {
            switch (axis)
            {
                // 인풋 비전 X축 표시명
                case SharedRailXAxis.InputVisionX: return "InputVisionX";
                // 프론트 피커 X축 표시명
                case SharedRailXAxis.FrontPickerX: return "FrontPickerX";
                // 리어 피커 X축 표시명
                case SharedRailXAxis.RearPickerX: return "RearPickerX";
                // 아웃풋 비전 X축 표시명
                case SharedRailXAxis.OutputVisionX: return "OutputVisionX";
                default: return axis.ToString();
            }
        }

        private static bool TryGetRowAxis(DataGridViewRow row, out SharedRailXAxis axis)
        {
            axis = SharedRailXAxis.InputVisionX;
            if (row == null)
                return false;
            if (row.DataGridView == null || !row.DataGridView.Columns.Contains("colAxis"))
                return false;

            object value = row.Cells["colAxis"].Value;
            return value != null && Enum.TryParse(value.ToString(), true, out axis);
        }

        private void InitializeHeadSubSelectors()
        {
            if (cboHeadAxis.SelectedIndex < 0 && cboHeadAxis.Items.Count > 0)
                cboHeadAxis.SelectedIndex = 0;

            ApplyHeadSubRules();
        }

        private void ApplyHeadSubRules()
        {
            SharedRailXAxis head = GetHeadAxis();
            ConfigureSubCheckBox(chkSubInputVision, head, SharedRailXAxis.InputVisionX, IsAllowedSubAxis(head, SharedRailXAxis.InputVisionX));
            ConfigureSubCheckBox(chkSubOutputVision, head, SharedRailXAxis.OutputVisionX, IsAllowedSubAxis(head, SharedRailXAxis.OutputVisionX));
            ConfigureSubCheckBox(chkSubFrontPicker, head, SharedRailXAxis.FrontPickerX, IsAllowedSubAxis(head, SharedRailXAxis.FrontPickerX));
            ConfigureSubCheckBox(chkSubRearPicker, head, SharedRailXAxis.RearPickerX, IsAllowedSubAxis(head, SharedRailXAxis.RearPickerX));
        }

        private static void ConfigureSubCheckBox(CheckBox checkBox, SharedRailXAxis head, SharedRailXAxis axis, bool allowed)
        {
            checkBox.Enabled = allowed && axis != head;
            if (!checkBox.Enabled)
                checkBox.Checked = false;
        }

        private static bool IsAllowedSubAxis(SharedRailXAxis head, SharedRailXAxis sub)
        {
            if (head == sub)
                return false;

            switch (head)
            {
                // 비전축 기준 서브축 허용 범위
                case SharedRailXAxis.InputVisionX:
                case SharedRailXAxis.OutputVisionX:
                    return sub == SharedRailXAxis.FrontPickerX || sub == SharedRailXAxis.RearPickerX;
                // 프론트 피커 기준 서브축 허용 범위
                case SharedRailXAxis.FrontPickerX:
                    return sub == SharedRailXAxis.InputVisionX ||
                        sub == SharedRailXAxis.RearPickerX ||
                        sub == SharedRailXAxis.OutputVisionX;
                // 리어 피커 기준 서브축 허용 범위
                case SharedRailXAxis.RearPickerX:
                    return sub == SharedRailXAxis.InputVisionX ||
                        sub == SharedRailXAxis.FrontPickerX ||
                        sub == SharedRailXAxis.OutputVisionX;
                default:
                    return false;
            }
        }

        private SharedRailXAxis GetHeadAxis()
        {
            SharedRailXAxis axis;
            string text = Convert.ToString(cboHeadAxis.SelectedItem);
            if (Enum.TryParse(text, true, out axis))
                return axis;
            return SharedRailXAxis.InputVisionX;
        }

        private List<DataGridViewRow> GetHeadSubRows()
        {
            var selected = new HashSet<SharedRailXAxis>();
            selected.Add(GetHeadAxis());

            AddSubAxisIfChecked(selected, chkSubInputVision, SharedRailXAxis.InputVisionX);
            AddSubAxisIfChecked(selected, chkSubOutputVision, SharedRailXAxis.OutputVisionX);
            AddSubAxisIfChecked(selected, chkSubFrontPicker, SharedRailXAxis.FrontPickerX);
            AddSubAxisIfChecked(selected, chkSubRearPicker, SharedRailXAxis.RearPickerX);

            var rows = new List<DataGridViewRow>();
            foreach (SharedRailXAxis axis in selected)
            {
                DataGridViewRow row = FindRow(axis);
                if (row != null)
                    rows.Add(row);
            }

            return rows;
        }

        private static void AddSubAxisIfChecked(HashSet<SharedRailXAxis> selected, CheckBox checkBox, SharedRailXAxis axis)
        {
            if (checkBox.Enabled && checkBox.Checked)
                selected.Add(axis);
        }

        private DataGridViewRow FindRow(SharedRailXAxis targetAxis)
        {
            return FindRow(grid, targetAxis);
        }

        private static DataGridViewRow FindRow(DataGridView view, SharedRailXAxis targetAxis)
        {
            if (view == null)
                return null;

            foreach (DataGridViewRow row in view.Rows)
            {
                SharedRailXAxis axis;
                if (TryGetRowAxis(row, out axis) && axis == targetAxis)
                    return row;
            }

            return null;
        }

        private bool TryGetSelectedAxis(out SharedRailXAxis axis)
        {
            axis = SharedRailXAxis.InputVisionX;
            DataGridView active = _activeGrid ?? grid;
            if (active != null && active.CurrentRow != null && TryGetRowAxis(active.CurrentRow, out axis))
                return true;
            if (grid.CurrentRow != null && TryGetRowAxis(grid.CurrentRow, out axis))
                return true;
            return false;
        }

        private static object GetCellValue(DataGridViewRow row, string columnName)
        {
            if (row == null || row.DataGridView == null || !row.DataGridView.Columns.Contains(columnName))
                return null;
            return row.Cells[columnName].Value;
        }

        private static void SetCellValue(DataGridViewRow row, string columnName, object value)
        {
            if (row == null || row.DataGridView == null || !row.DataGridView.Columns.Contains(columnName))
                return;
            row.Cells[columnName].Value = value;
        }

        private static int AxisOrder(string axis)
        {
            SharedRailXAxis parsed;
            if (!Enum.TryParse(axis, true, out parsed))
                return 99;
            return (int)parsed;
        }

        private static double ReadDouble(object value, double fallback)
        {
            string text = Convert.ToString(value);
            double parsed;
            if (TryReadDouble(text, out parsed))
                return parsed;
            return fallback;
        }

        private static bool TryReadDouble(string text, out double parsed)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                return true;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
                return true;
            return false;
        }

        private static int NormalizeSign(double value)
        {
            if (value > 0.0)
                return 1;
            if (value < 0.0)
                return -1;
            return 0;
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static void SetRowStatus(DataGridViewRow row, string status, Color color, string message)
        {
            if (row == null)
                return;
            row.Cells[row.DataGridView.Columns["colStatus"].Index].Value = status;
            row.Cells[row.DataGridView.Columns["colMessage"].Index].Value = message ?? string.Empty;
            row.DefaultCellStyle.BackColor = color;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnReload.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnApply.Enabled = enabled;
            btnValidate.Enabled = enabled;
            btnHelp.Enabled = enabled;
            btnMoveSelected.Enabled = enabled;
            btnMoveHeadSub.Enabled = enabled;
            btnHomeSelected.Enabled = enabled;
            btnHomeAll.Enabled = enabled;
            btnRefresh.Enabled = enabled;
            btnStop.Enabled = true;
            btnClose.Enabled = enabled;
        }
    }
}
