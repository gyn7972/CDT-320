using QMC.CDT_320.Ui.Localization;
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
    public partial class InitializationMonitorDialog : Form, ILocalizedView
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
        private readonly Dictionary<DataGridViewCell, string> _routeTooltipText = new Dictionary<DataGridViewCell, string>();

        public InitializationMonitorDialog(MachineController controller)
        {
            _controller = controller;
            InitializeComponent();
            InitializeLanguageBindings();
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
                _routeTooltipText.Clear();
                grid.Rows.Clear();
                if (_controller == null)
                    return;

                AxisInitializePlan plan = _controller.GetAxisInitializePlan();
                if (plan == null || plan.Steps == null)
                    return;

                // 실행 이력(Status)과 현재 물리 상태 판정을 섞지 않고 별도 열에 표시합니다.
                AxisInitializeRouteResult routePreview =
                    _controller.GetAxisInitializeRoutePreview();

                foreach (AxisInitializeStep step in plan.Steps
                    .Where(x => x != null)
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName))
                {
                    AxisInitializeRouteStep routeStep = routePreview != null
                        ? routePreview.FindStep(step.StepNo, step.GroupName)
                        : null;
                    AddActionRows(step, step.PreActions, "PreActions", routeStep);
                    AddHomeRow(step, routeStep);
                    AddActionRows(step, step.PostActions, "PostActions", routeStep);
                }

                ApplyStoredStatuses();
                grid.ClearSelection();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, Lang.Format("extraDialog.initialize.planFailed", ex.Message),
                    Lang.T("extraDialog.initialize.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AddActionRows(
            AxisInitializeStep step,
            IList<AxisInitializeAction> actions,
            string phase,
            AxisInitializeRouteStep routeStep)
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
                    routeStep != null ? routeStep.BuildCheckText() : "확인 불가",
                    status,
                    action.Description);
                DataGridViewRow row = grid.Rows[rowIndex];
                row.Tag = step.StepNo;
                // 진행 메시지가 비었을 때 되살릴 원본 설명을 셀 Tag에 보관한다(아래 ApplyProgressToRows 참조).
                row.Cells[colDescription.Index].Tag = action.Description ?? string.Empty;
                ApplyStatusStyle(row, status);
                ApplyRouteCheckStyle(row, routeStep);
            }
        }

        private void AddHomeRow(
            AxisInitializeStep step,
            AxisInitializeRouteStep routeStep)
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
                routeStep != null ? routeStep.BuildCheckText() : "확인 불가",
                status,
                step.Comment);
            DataGridViewRow row = grid.Rows[rowIndex];
            row.Tag = step.StepNo;
            // 진행 메시지가 비었을 때 되살릴 원본 설명을 셀 Tag에 보관한다(아래 ApplyProgressToRows 참조).
            row.Cells[colDescription.Index].Tag = step.Comment ?? string.Empty;
            ApplyStatusStyle(row, status);
            ApplyRouteCheckStyle(row, routeStep);
        }

        private void ApplyRouteCheckStyle(
            DataGridViewRow row,
            AxisInitializeRouteStep routeStep)
        {
            if (row == null || colCurrentCheck == null)
                return;

            DataGridViewCell cell = row.Cells[colCurrentCheck.Index];
            cell.ToolTipText = routeStep != null
                ? routeStep.BuildDisplayText()
                : "현재 상태를 확인하지 못했습니다.";
            _routeTooltipText[cell] = cell.ToolTipText;
            cell.ToolTipText = AdditionalDialogText.Display(cell.ToolTipText);

            if (routeStep == null)
            {
                cell.Style.BackColor = Color.FromArgb(238, 238, 238);
                return;
            }

            if (string.Equals(
                routeStep.State,
                AxisInitializeRouteState.ReadyNow,
                StringComparison.OrdinalIgnoreCase))
                cell.Style.BackColor = Color.FromArgb(200, 230, 201);
            else if (string.Equals(
                routeStep.State,
                AxisInitializeRouteState.Disabled,
                StringComparison.OrdinalIgnoreCase))
                cell.Style.BackColor = Color.FromArgb(238, 238, 238);
            else
                cell.Style.BackColor = Color.FromArgb(255, 245, 157);
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
                    Lang.T("extraDialog.initialize.confirmAll"), Lang.T("extraDialog.initialize.allTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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
                        ? Lang.T("extraDialog.initialize.runFailed")
                        : _controller.LastActionFailureMessage;
                }
            }
            catch (Exception ex)
            {
                errorMessage = Lang.Format("extraDialog.initialize.runError", ex.Message);
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
                    Lang.T("extraDialog.initialize.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrWhiteSpace(warningMessage))
            {
                QMC.Common.MessageDialog.Show(this, warningMessage,
                    Lang.T("extraDialog.initialize.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    TextFormatter = AdditionalDialogText.DisplayInitialization,
                    CompletedStepsFormat = Lang.T("extraDialog.progress.completedSteps"),
                    RunningTitle = contextTitle + " 진행 중",
                    CompletedTitle = contextTitle + " 완료",
                    FailedTitle = contextTitle + " 실패",
                    CanceledTitle = contextTitle + " 정지",
                    IdleTitle = contextTitle + " 준비",
                    DefaultStepText = "초기화 시퀀스를 준비합니다.",
                    DefaultMessage = "초기화가 완료될 때까지 기다려 주세요."
                };
                AdditionalDialogText.Bind(_progressDialog, contextTitle);
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

                // To do: [초기화 모니터] Status와 Description을 항상 같은 시점 값으로 표시한다.
                // 기존 조건: Message가 비어 있으면 Description을 갱신하지 않아 이전 회차 문구가 남았다.
                //            → Status는 갱신되고 Description만 옛 값이라 "Done + 중단 메시지" 같은
                //              모순된 조합이 화면에 남았다(2026-08-05).
                // 현재 기준: 메시지가 있으면 그 메시지를, 비어 있으면 행 생성 시 보관한 원본 설명(셀 Tag)을
                //            표시한다 — 이전 회차 런타임 문구는 남지 않고, 기본 설명은 지워지지 않는다.
                row.Cells[colDescription.Index].Value = !string.IsNullOrWhiteSpace(progress.Message)
                    ? progress.Message
                    : Convert.ToString(row.Cells[colDescription.Index].Tag);
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
        public void ApplyLanguage()
        {
            foreach (KeyValuePair<DataGridViewCell, string> item in _routeTooltipText)
                item.Key.ToolTipText = AdditionalDialogText.Display(item.Value);
            if (_progressDialog != null && !_progressDialog.IsDisposed)
            {
                _progressDialog.CompletedStepsFormat = Lang.T("extraDialog.progress.completedSteps");
                _progressDialog.RefreshDisplay();
            }
        }

        private void InitializeLanguageBindings()
        {
            Lang.BindReadOnlyCells(grid, AdditionalDialogText.Display, cell => cell.ColumnIndex == colPhase.Index ||
                cell.ColumnIndex == colTargetType.Index || cell.ColumnIndex == colStatus.Index || cell.ColumnIndex == colCurrentCheck.Index);
            Lang.BindKey(lblTitle, "extraDialog.initializationMonitorDialog.lblTitle.caption");
            Lang.BindKey(colStepNo, "extraDialog.initializationMonitorDialog.colStepNo.caption");
            Lang.BindKey(colGroupName, "extraDialog.initializationMonitorDialog.colGroupName.caption");
            Lang.BindKey(colPhase, "extraDialog.initializationMonitorDialog.colPhase.caption");
            Lang.BindKey(colTargetType, "extraDialog.initializationMonitorDialog.colTargetType.caption");
            Lang.BindKey(colTarget, "extraDialog.initializationMonitorDialog.colTarget.caption");
            Lang.BindKey(colCommand, "extraDialog.initializationMonitorDialog.colCommand.caption");
            Lang.BindKey(colCurrentCheck, "extraDialog.initializationMonitorDialog.colCurrentCheck.caption");
            Lang.BindKey(colStatus, "extraDialog.initializationMonitorDialog.colStatus.caption");
            Lang.BindKey(colDescription, "extraDialog.initializationMonitorDialog.colDescription.caption");
            Lang.BindKey(btnRunSelected, "extraDialog.initializationMonitorDialog.btnRunSelected.caption");
            Lang.BindKey(btnRunAll, "extraDialog.initializationMonitorDialog.btnRunAll.caption");
            Lang.BindKey(btnRefresh, "extraDialog.initializationMonitorDialog.btnRefresh.caption");
            Lang.BindKey(btnClose, "extraDialog.initializationMonitorDialog.btnClose.caption");
            Load += (sender, args) => Lang.Apply(this);
        }

    }
}
