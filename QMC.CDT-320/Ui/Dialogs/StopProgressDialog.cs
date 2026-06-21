using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Sequencing;
using QMC.Common.Ui.Controls;
using QMC.Common.Ui.Dialogs;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 일반 정지 요청 후 현재 동작이 안전 경계에서 끝날 때까지 표시하는 진행 팝업입니다.
    /// </summary>
    public sealed class StopProgressDialog : ProgressDialog
    {
        private readonly MachineController _controller;
        private readonly Timer _refreshTimer;
        private readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
        private bool _completed;

        public StopProgressDialog(MachineController controller)
        {
            _controller = controller;

            Text = "Stop";
            RunningTitle = "정지 처리 중";
            CompletedTitle = "정지 완료";
            FailedTitle = "알람 발생";
            CanceledTitle = "정지 처리";
            IdleTitle = "정지 요청";
            DefaultStepText = "현재 동작 완료 대기";
            DefaultMessage = "정지 요청이 접수되었습니다. 현재 이동/작업 완료 후 안전 경계에서 정지합니다.";
            RunningColor = System.Drawing.Color.FromArgb(0xE8, 0x5D, 0x1A);

            _refreshTimer = new Timer { Interval = 200 };
            _refreshTimer.Tick += delegate { RefreshStopProgress(); };

            if (_controller != null)
            {
                _controller.StatusChanged += OnControllerStatusChanged;
                if (_controller.SequenceActivity != null)
                    _controller.SequenceActivity.Changed += OnSequenceActivityChanged;
            }

            RefreshStopProgress();
            _refreshTimer.Start();
        }

        public Task WaitForStopCompleteAsync()
        {
            return _completionSource.Task;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();

            if (_controller != null)
            {
                _controller.StatusChanged -= OnControllerStatusChanged;
                if (_controller.SequenceActivity != null)
                    _controller.SequenceActivity.Changed -= OnSequenceActivityChanged;
            }

            if (!_completionSource.Task.IsCompleted)
                _completionSource.TrySetResult(false);

            base.OnFormClosed(e);
        }

        private void OnControllerStatusChanged(EquipmentStatus status)
        {
            RefreshStopProgress();
        }

        private void OnSequenceActivityChanged()
        {
            RefreshStopProgress();
        }

        private void RefreshStopProgress()
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                if (!IsHandleCreated)
                    return;

                try { BeginInvoke(new Action(RefreshStopProgress)); }
                catch { }
                return;
            }

            EquipmentStatus status = _controller != null ? _controller.Status : EquipmentStatus.Idle;
            ProgressState state = MapProgressState(status);
            bool done = state == ProgressState.Completed || state == ProgressState.Failed || state == ProgressState.Canceled;

            int total = 4;
            int completed = 0;
            string stepName = "현재 동작 완료 대기";
            string message = BuildActivityMessage(out completed, out total);

            if (state == ProgressState.Completed)
            {
                completed = total;
                stepName = status == EquipmentStatus.CycleStopped ? "사이클 정지 완료" : "정지 완료";
                message = "장비가 안전 경계에서 정지했습니다.";
            }
            else if (state == ProgressState.Failed)
            {
                stepName = "알람 상태";
                message = "정지 처리 중 알람이 발생했습니다. Alarm/Event Log를 확인하세요.";
            }

            int percent = total > 0 ? Math.Min(100, Math.Max(0, completed * 100 / total)) : 0;
            if (state == ProgressState.Running && percent >= 100)
                percent = 95;

            ApplyProgress(new ProgressInfo(state, percent, completed, total, stepName, message));

            if (done && !_completed)
            {
                _completed = true;
                _completionSource.TrySetResult(state == ProgressState.Completed);
            }
        }

        private ProgressState MapProgressState(EquipmentStatus status)
        {
            switch (status)
            {
                case EquipmentStatus.Alarm:
                    return ProgressState.Failed;
                case EquipmentStatus.Stopped:
                case EquipmentStatus.CycleStopped:
                case EquipmentStatus.Ready:
                    return ProgressState.Completed;
                case EquipmentStatus.Idle:
                    return ProgressState.Canceled;
                default:
                    return ProgressState.Running;
            }
        }

        private string BuildActivityMessage(out int completed, out int total)
        {
            completed = 0;
            total = 4;

            if (_controller == null || _controller.SequenceActivity == null)
                return DefaultMessage;

            var snapshots = _controller.SequenceActivity.GetAll();
            if (snapshots == null || snapshots.Count == 0)
                return DefaultMessage;

            total = snapshots.Count;
            var builder = new StringBuilder();
            builder.Append("정지 요청이 접수되었습니다. 현재 동작 완료 후 정지합니다.");

            for (int i = 0; i < snapshots.Count; i++)
            {
                SequenceActivitySnapshot s = snapshots[i];
                bool active = s.State == SequenceActivityState.Running || s.State == SequenceActivityState.Waiting;
                if (!active)
                    completed++;

                builder.AppendLine();
                builder.Append(s.DisplayName);
                builder.Append(" : ");
                builder.Append(FormatActivityState(s.State));
                string action = !string.IsNullOrWhiteSpace(s.ActionName) ? s.ActionName : s.StepName;
                if (!string.IsNullOrWhiteSpace(action))
                {
                    builder.Append(" / ");
                    builder.Append(action);
                }
            }

            return builder.ToString();
        }

        private static string FormatActivityState(SequenceActivityState state)
        {
            switch (state)
            {
                case SequenceActivityState.Running: return "동작 중";
                case SequenceActivityState.Waiting: return "대기 중";
                case SequenceActivityState.Completed: return "완료";
                case SequenceActivityState.Stopped: return "정지";
                case SequenceActivityState.Canceled: return "취소";
                case SequenceActivityState.Alarm: return "알람";
                default: return "대기";
            }
        }
    }
}
