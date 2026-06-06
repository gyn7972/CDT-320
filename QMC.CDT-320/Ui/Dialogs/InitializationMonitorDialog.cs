using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Initialization;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class InitializationMonitorDialog : Form
    {
        private readonly MachineController _controller;
        private bool _running;

        public InitializationMonitorDialog(MachineController controller)
        {
            _controller = controller;
            InitializeComponent();
            if (_controller != null)
                _controller.AxisInitializeStepProgressChanged += OnAxisInitializeStepProgressChanged;
        }

        private void InitializationMonitorDialog_Load(object sender, EventArgs e)
        {
            LoadPlanToGrid();
        }

        private void InitializationMonitorDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_controller != null)
                _controller.AxisInitializeStepProgressChanged -= OnAxisInitializeStepProgressChanged;
        }

        private void LoadPlanToGrid()
        {
            try
            {
                grid.Rows.Clear();
                if (_controller == null)
                    return;

                AxisInitializePlan plan = _controller.GetAxisInitializePlan();
                if (plan == null || plan.Steps == null)
                    return;

                foreach (AxisInitializeStep step in plan.Steps
                    .Where(x => x != null)
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName))
                {
                    AddActionRows(step, step.PreActions, "PreActions");
                    AddHomeRow(step);
                    AddActionRows(step, step.PostActions, "PostActions");
                }

                grid.ClearSelection();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "초기화 Plan 로딩 실패:\n" + ex.Message,
                    "Init Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AddActionRows(AxisInitializeStep step, IList<AxisInitializeAction> actions, string phase)
        {
            if (step == null || actions == null)
                return;

            foreach (AxisInitializeAction action in actions)
            {
                if (action == null)
                    continue;

                int rowIndex = grid.Rows.Add(
                    step.StepNo,
                    step.GroupName,
                    phase,
                    action.TargetType,
                    action.Name,
                    action.Command,
                    action.Enabled && step.Enabled ? "대기" : "비활성",
                    action.Description);
                DataGridViewRow row = grid.Rows[rowIndex];
                row.Tag = step.StepNo;
                ApplyStatusStyle(row, action.Enabled && step.Enabled ? "대기" : "비활성");
            }
        }

        private void AddHomeRow(AxisInitializeStep step)
        {
            if (step == null || step.AxisNames == null || step.AxisNames.Count == 0)
                return;

            int rowIndex = grid.Rows.Add(
                step.StepNo,
                step.GroupName,
                "Home",
                "Axis",
                string.Join("; ", step.AxisNames.ToArray()),
                "Home",
                step.Enabled ? "대기" : "비활성",
                step.Comment);
            DataGridViewRow row = grid.Rows[rowIndex];
            row.Tag = step.StepNo;
            ApplyStatusStyle(row, step.Enabled ? "대기" : "비활성");
        }

        private async void btnRunSelected_Click(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Tag == null)
                return;

            int stepNo;
            if (!int.TryParse(grid.CurrentRow.Tag.ToString(), out stepNo))
                return;

            await RunAsync(() => _controller.InitializePlanStepAsync(stepNo));
        }

        private async void btnRunAll_Click(object sender, EventArgs e)
        {
            ResetStatuses();
            await RunAsync(() => _controller.InitializeAllAxesForMonitorAsync());
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (_running)
                return;

            LoadPlanToGrid();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async Task RunAsync(Func<Task<int>> action)
        {
            if (_running || action == null || _controller == null)
                return;

            try
            {
                _running = true;
                SetButtonsEnabled(false);
                int result = await action();
                if (result != 0)
                {
                    string message = string.IsNullOrEmpty(_controller.LastActionFailureMessage)
                        ? "초기화 실행에 실패했습니다."
                        : _controller.LastActionFailureMessage;
                    QMC.Common.MessageDialog.Show(this, message, "Init Monitor",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "초기화 실행 오류:\n" + ex.Message,
                    "Init Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _running = false;
                SetButtonsEnabled(true);
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnRunSelected.Enabled = enabled;
            btnRunAll.Enabled = enabled;
            btnRefresh.Enabled = enabled;
            btnClose.Enabled = enabled;
        }

        private void ResetStatuses()
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                string current = Convert.ToString(row.Cells[colStatus.Index].Value);
                if (string.Equals(current, "비활성", StringComparison.OrdinalIgnoreCase))
                    continue;

                row.Cells[colStatus.Index].Value = "대기";
                ApplyStatusStyle(row, "대기");
            }
        }

        private void OnAxisInitializeStepProgressChanged(AxisInitializeStepProgress progress)
        {
            if (progress == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<AxisInitializeStepProgress>(OnAxisInitializeStepProgressChanged), progress);
                return;
            }

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Tag == null)
                    continue;

                int stepNo;
                if (!int.TryParse(row.Tag.ToString(), out stepNo) || stepNo != progress.StepNo)
                    continue;

                string groupName = Convert.ToString(row.Cells[colGroupName.Index].Value);
                if (!string.Equals(groupName, progress.GroupName, StringComparison.OrdinalIgnoreCase))
                    continue;

                row.Cells[colStatus.Index].Value = progress.Status;
                ApplyStatusStyle(row, progress.Status);
                if (!string.IsNullOrWhiteSpace(progress.Message))
                    row.Cells[colDescription.Index].Value = progress.Message;
            }
        }

        private void ApplyStatusStyle(DataGridViewRow row, string status)
        {
            if (row == null)
                return;

            Color backColor = Color.White;
            Color foreColor = Color.FromArgb(30, 30, 30);

            if (string.Equals(status, "진행중", StringComparison.OrdinalIgnoreCase))
                backColor = Color.FromArgb(255, 245, 157);
            else if (string.Equals(status, "완료", StringComparison.OrdinalIgnoreCase))
                backColor = Color.FromArgb(200, 230, 201);
            else if (string.Equals(status, "실패", StringComparison.OrdinalIgnoreCase))
                backColor = Color.FromArgb(255, 205, 210);
            else if (string.Equals(status, "비활성", StringComparison.OrdinalIgnoreCase))
            {
                backColor = Color.FromArgb(238, 238, 238);
                foreColor = Color.FromArgb(120, 120, 120);
            }

            row.DefaultCellStyle.BackColor = backColor;
            row.DefaultCellStyle.SelectionBackColor = ControlPaint.Dark(backColor);
            row.DefaultCellStyle.ForeColor = foreColor;
            row.DefaultCellStyle.SelectionForeColor = foreColor;
        }
    }
}
