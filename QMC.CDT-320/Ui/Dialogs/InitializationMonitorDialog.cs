using QMC.CDT320;
using QMC.CDT320.Initialization;
using QMC.Common.Logging;
using QMC.Common.Ui.Controls;
using QMC.Common.Ui.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class InitializationMonitorDialog : Form
    {
        private const string StatusWaiting = "Waiting";
        private const string StatusDisabled = "Disabled";
        private const string StatusRunning = "Running";
        private const string StatusDone = "Done";
        private const string StatusFailed = "Failed";
        private const string StatusReinitializeRequired = "Reinitialize Required";

        private readonly MachineController _controller;
        private bool _running;
        private ProgressDialog _progressDialog;

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

            CloseInitProgressDialog();
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

                ApplyStoredStatuses();
                grid.ClearSelection();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Initialize plan load failed:\n" + ex.Message,
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

                string status = action.Enabled && step.Enabled ? StatusWaiting : StatusDisabled;
                int rowIndex = grid.Rows.Add(
                    step.StepNo,
                    step.GroupName,
                    phase,
                    action.TargetType,
                    action.Name,
                    action.Command,
                    status,
                    action.Description);
                DataGridViewRow row = grid.Rows[rowIndex];
                row.Tag = step.StepNo;
                ApplyStatusStyle(row, status);
            }
        }

        private void AddHomeRow(AxisInitializeStep step)
        {
            if (step == null || step.AxisNames == null || step.AxisNames.Count == 0)
                return;

            string status = step.Enabled ? StatusWaiting : StatusDisabled;
            int rowIndex = grid.Rows.Add(
                step.StepNo,
                step.GroupName,
                "Home",
                "Axis",
                string.Join("; ", step.AxisNames.ToArray()),
                "Home",
                status,
                step.Comment);
            DataGridViewRow row = grid.Rows[rowIndex];
            row.Tag = step.StepNo;
            ApplyStatusStyle(row, status);
        }

        private async void btnRunSelected_Click(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Tag == null)
                return;

            int stepNo;
            if (!int.TryParse(grid.CurrentRow.Tag.ToString(), out stepNo))
                return;

            await RunAsync(() => _controller.InitializePlanStepAsync(stepNo), "스텝 초기화");
        }

        private async void btnRunAll_Click(object sender, EventArgs e)
        {
            DialogResult result = QMC.Common.MessageDialog.Show(
                    "전체 초기화를 진행하시겠습니까?", "전체 초기화", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                EventLogger.Write(EventKind.Event, "UI", "전체 초기화", "btnRunAll_Click canceled.");
                return;
            }

            ResetStatuses();
            await RunAsync(() => _controller.InitializeAllAxesForMonitorAsync(), "전체 초기화");
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

        private async Task RunAsync(Func<Task<int>> action, string contextTitle)
        {
            if (_running || action == null || _controller == null)
                return;

            string warningMessage = null;
            string errorMessage = null;
            int result = -1;

            try
            {
                _running = true;
                SetButtonsEnabled(false);
                OpenInitProgressDialog(contextTitle);

                result = await action();
                ApplyStoredStatuses();
                if (result != 0)
                {
                    warningMessage = string.IsNullOrEmpty(_controller.LastActionFailureMessage)
                        ? "Initialize execution failed."
                        : _controller.LastActionFailureMessage;
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Initialize execution error:\n" + ex.Message;
            }
            finally
            {
                await CloseInitProgressDialogAsync(result, contextTitle, warningMessage ?? errorMessage);
                _running = false;
                SetButtonsEnabled(true);
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                QMC.Common.MessageDialog.Show(this, errorMessage,
                    "Init Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrWhiteSpace(warningMessage))
            {
                QMC.Common.MessageDialog.Show(this, warningMessage,
                    "Init Monitor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(SetButtonsEnabled), enabled);
                return;
            }

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
                if (string.Equals(current, StatusDisabled, StringComparison.OrdinalIgnoreCase))
                    continue;

                row.Cells[colStatus.Index].Value = StatusWaiting;
                ApplyStatusStyle(row, StatusWaiting);
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

            ApplyProgressToRows(progress);
            UpdateInitProgressDialogRunning(progress.GroupName);
        }

        /// <summary>초기화 실행 중 공용 진행 팝업을 띄운다. (스텝/전체 공통)</summary>
        private void OpenInitProgressDialog(string contextTitle)
        {
            try
            {
                CloseInitProgressDialog();

                _progressDialog = new ProgressDialog
                {
                    Text = contextTitle,
                    RunningTitle = contextTitle + " 진행 중",
                    CompletedTitle = contextTitle + " 완료",
                    FailedTitle = contextTitle + " 실패",
                    CanceledTitle = contextTitle + " 정지",
                    IdleTitle = contextTitle + " 준비",
                    DefaultStepText = "초기화 시퀀스를 준비합니다.",
                    DefaultMessage = "초기화가 완료될 때까지 기다려 주세요."
                };
                _progressDialog.ApplyProgress(BuildInitProgressInfo(ProgressState.Running, "초기화 시퀀스를 시작합니다.", null));
                _progressDialog.Show(this);
                _progressDialog.BringToFront();
                _progressDialog.Activate();
                _progressDialog.Refresh();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "InitProgressDialog",
                    "Init progress dialog open failed: " + ex.Message);
            }
        }

        private void UpdateInitProgressDialogRunning(string stepName)
        {
            if (_progressDialog == null || _progressDialog.IsDisposed)
                return;

            _progressDialog.ApplyProgress(BuildInitProgressInfo(ProgressState.Running,
                string.IsNullOrWhiteSpace(stepName) ? "초기화 시퀀스를 진행합니다." : stepName, null));
        }

        private async Task CloseInitProgressDialogAsync(int result, string contextTitle, string failureMessage)
        {
            if (_progressDialog == null || _progressDialog.IsDisposed)
            {
                _progressDialog = null;
                return;
            }

            try
            {
                if (result == 0)
                {
                    _progressDialog.ApplyProgress(new ProgressInfo(ProgressState.Completed, 100,
                        CountInitDoneRows(), CountInitTotalRows(), contextTitle, contextTitle + " 가 완료되었습니다."));
                }
                else
                {
                    int total = CountInitTotalRows();
                    int done = CountInitDoneRows();
                    int percent = total > 0 ? (int)Math.Round(done * 100.0 / total) : 0;
                    _progressDialog.ApplyProgress(new ProgressInfo(ProgressState.Failed, percent, done, total,
                        contextTitle,
                        string.IsNullOrWhiteSpace(failureMessage) ? contextTitle + " 가 실패했습니다." : failureMessage));
                }

                await Task.Delay(result == 0 ? 700 : 600);
            }
            catch
            {
            }
            finally
            {
                CloseInitProgressDialog();
            }
        }

        private void CloseInitProgressDialog()
        {
            try
            {
                if (_progressDialog != null && !_progressDialog.IsDisposed)
                {
                    _progressDialog.Close();
                    _progressDialog.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                _progressDialog = null;
            }
        }

        private ProgressInfo BuildInitProgressInfo(ProgressState state, string stepName, string message)
        {
            int total = CountInitTotalRows();
            int done = CountInitDoneRows();
            int percent = total > 0 ? (int)Math.Round(done * 100.0 / total) : 0;
            return new ProgressInfo(state, percent, done, total, stepName,
                message ?? "초기화 시퀀스를 진행합니다.");
        }

        private int CountInitTotalRows()
        {
            int total = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                string st = Convert.ToString(row.Cells[colStatus.Index].Value);
                if (string.Equals(st, StatusDisabled, StringComparison.OrdinalIgnoreCase))
                    continue;
                total++;
            }
            return total;
        }

        private int CountInitDoneRows()
        {
            int done = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                string st = Convert.ToString(row.Cells[colStatus.Index].Value);
                if (string.Equals(st, StatusDone, StringComparison.OrdinalIgnoreCase))
                    done++;
            }
            return done;
        }

        private void ApplyStoredStatuses()
        {
            if (_controller == null)
                return;

            IList<AxisInitializeStepProgress> statuses = _controller.GetAxisInitializeStepStatusSnapshot();
            if (statuses == null)
                return;

            foreach (AxisInitializeStepProgress progress in statuses)
                ApplyProgressToRows(progress);
        }

        private void ApplyProgressToRows(AxisInitializeStepProgress progress)
        {
            if (progress == null)
                return;

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

                string status = NormalizeStatus(progress.Status);
                row.Cells[colStatus.Index].Value = status;
                ApplyStatusStyle(row, status);
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

            if (string.Equals(status, StatusRunning, StringComparison.OrdinalIgnoreCase))
                backColor = Color.FromArgb(255, 245, 157);
            else if (string.Equals(status, StatusDone, StringComparison.OrdinalIgnoreCase))
                backColor = Color.FromArgb(200, 230, 201);
            else if (string.Equals(status, StatusFailed, StringComparison.OrdinalIgnoreCase))
                backColor = Color.FromArgb(255, 205, 210);
            else if (string.Equals(status, StatusReinitializeRequired, StringComparison.OrdinalIgnoreCase))
                backColor = Color.FromArgb(255, 224, 178);
            else if (string.Equals(status, StatusDisabled, StringComparison.OrdinalIgnoreCase))
            {
                backColor = Color.FromArgb(238, 238, 238);
                foreColor = Color.FromArgb(120, 120, 120);
            }

            row.DefaultCellStyle.BackColor = backColor;
            row.DefaultCellStyle.SelectionBackColor = ControlPaint.Dark(backColor);
            row.DefaultCellStyle.ForeColor = foreColor;
            row.DefaultCellStyle.SelectionForeColor = foreColor;
        }

        private static string NormalizeStatus(string status)
        {
            if (string.Equals(status, "진행중", StringComparison.OrdinalIgnoreCase))
                return StatusRunning;
            if (string.Equals(status, "완료", StringComparison.OrdinalIgnoreCase))
                return StatusDone;
            if (string.Equals(status, "실패", StringComparison.OrdinalIgnoreCase))
                return StatusFailed;
            if (string.Equals(status, "비활성", StringComparison.OrdinalIgnoreCase))
                return StatusDisabled;
            if (string.Equals(status, "대기", StringComparison.OrdinalIgnoreCase))
                return StatusWaiting;
            if (string.Equals(status, AxisInitializeStepStatus.Waiting, StringComparison.OrdinalIgnoreCase))
                return StatusWaiting;
            if (string.Equals(status, AxisInitializeStepStatus.Running, StringComparison.OrdinalIgnoreCase))
                return StatusRunning;
            if (string.Equals(status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase))
                return StatusDone;
            if (string.Equals(status, AxisInitializeStepStatus.Failed, StringComparison.OrdinalIgnoreCase))
                return StatusFailed;
            if (string.Equals(status, AxisInitializeStepStatus.Disabled, StringComparison.OrdinalIgnoreCase))
                return StatusDisabled;
            if (string.Equals(status, AxisInitializeStepStatus.ReinitializeRequired, StringComparison.OrdinalIgnoreCase))
                return StatusReinitializeRequired;
            return string.IsNullOrWhiteSpace(status) ? StatusWaiting : status;
        }
    }
}
