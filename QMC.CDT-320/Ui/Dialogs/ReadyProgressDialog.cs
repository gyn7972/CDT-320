using QMC.CDT320;
using QMC.Common.Ui.Controls;
using QMC.Common.Ui.Dialogs;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// READY(Avoid) 시퀀스 진행 팝업. 공용 <see cref="ProgressDialog"/> 를 재사용하고,
    /// MachineController.ReadySequenceProgressChanged 를 구독해 진행 상황만 매핑한다.
    /// </summary>
    public sealed class ReadyProgressDialog : ProgressDialog
    {
        private readonly MachineController _controller;

        public ReadyProgressDialog(MachineController controller)
        {
            _controller = controller;

            Text = "Ready";
            RunningTitle = "READY 진행 중";
            CompletedTitle = "READY 완료";
            FailedTitle = "READY 실패";
            CanceledTitle = "READY 정지";
            IdleTitle = "READY 준비";
            DefaultStepText = "Ready 시퀀스를 준비합니다.";
            DefaultMessage = "모션이 완료될 때까지 기다려 주세요.";

            if (_controller != null)
            {
                _controller.ReadySequenceProgressChanged += OnReadySequenceProgressChanged;
                ApplyProgress(_controller.ReadySequenceProgress);
            }
        }

        protected override void OnFormClosed(System.Windows.Forms.FormClosedEventArgs e)
        {
            if (_controller != null)
                _controller.ReadySequenceProgressChanged -= OnReadySequenceProgressChanged;

            base.OnFormClosed(e);
        }

        private void OnReadySequenceProgressChanged(MachineReadyProgress progress)
        {
            ApplyProgress(progress);
        }

        /// <summary>Ready 진행 모델을 공용 진행 모델로 변환해 표시한다.</summary>
        public void ApplyProgress(MachineReadyProgress progress)
        {
            if (progress == null)
                return;

            ApplyProgress(new ProgressInfo(
                MapState(progress.State),
                progress.Percent,
                progress.CompletedSteps,
                progress.TotalSteps,
                progress.CurrentStepName,
                progress.Message));
        }

        private static ProgressState MapState(MachineReadySequenceState state)
        {
            switch (state)
            {
                case MachineReadySequenceState.Completed: return ProgressState.Completed;
                case MachineReadySequenceState.Failed: return ProgressState.Failed;
                case MachineReadySequenceState.Canceled: return ProgressState.Canceled;
                case MachineReadySequenceState.Running: return ProgressState.Running;
                default: return ProgressState.Idle;
            }
        }
    }
}
