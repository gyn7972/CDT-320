using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using QMC.Common.Ui.Controls;
using QMC.Common.Ui.Dialogs;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// READY(Avoid) 시퀀스 진행 팝업입니다. 공용 <see cref="ProgressDialog"/>를 재사용하고
    /// MachineController.ReadySequenceProgressChanged를 구독해 진행 상태만 표시합니다.
    /// </summary>
    public sealed class ReadyProgressDialog : ProgressDialog, ILocalizedView
    {
        private readonly MachineController _controller;

        public ReadyProgressDialog(MachineController controller)
        {
            _controller = controller;

            TextFormatter = TranslateProgressText;
            ApplyLanguage();
            Load += (sender, args) => Lang.Apply(this);

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

        /// <summary>Ready 진행 모델을 공용 진행 모델로 변환해 표시합니다.</summary>
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
                case MachineReadySequenceState.Completed:
                    return ProgressState.Completed;
                case MachineReadySequenceState.Failed:
                    return ProgressState.Failed;
                case MachineReadySequenceState.Canceled:
                    return ProgressState.Canceled;
                case MachineReadySequenceState.Running:
                    return ProgressState.Running;
                default:
                    return ProgressState.Idle;
            }
        }
        public void ApplyLanguage()
        {
            Text = Lang.T("extraDialog.progress.readyTitle");
            RunningTitle = Lang.T("extraDialog.progress.readyRunning");
            CompletedTitle = Lang.T("extraDialog.progress.readyComplete");
            FailedTitle = Lang.T("extraDialog.progress.readyFailed");
            CanceledTitle = Lang.T("extraDialog.progress.readyCanceled");
            IdleTitle = Lang.T("extraDialog.progress.readyIdle");
            DefaultStepText = Lang.T("extraDialog.progress.readyStep");
            DefaultMessage = Lang.T("extraDialog.progress.readyMessage");
            CompletedStepsFormat = Lang.T("extraDialog.progress.completedSteps");
            RefreshDisplay();
        }

        private static string TranslateProgressText(string text)
        {
            return ProgressDialogText.Translate(text);
        }

    }

    internal static class ProgressDialogText
    {
        public static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
            {
                // Keep each source line separator, including mixed SDK message endings.
                string[] parts = System.Text.RegularExpressions.Regex.Split(text, "(\r\n|\r|\n)");
                for (int i = 0; i < parts.Length; i += 2)
                    parts[i] = Translate(parts[i]);
                return string.Concat(parts);
            }
            if (text.StartsWith("이동 중 축 : ", System.StringComparison.Ordinal))
                return Lang.Format("extraDialog.progress.movingAxes", text.Substring("이동 중 축 : ".Length));
            switch (text)
            {
                case "준비 중": return Lang.T("extraDialog.progress.idle");
                case "Preparing": return Lang.T("extraDialog.progress.idle");
                case "Ready": return Lang.T("extraDialog.progress.readyTitle");
                case "준비 동작": return Lang.T("extraDialog.progress.readyTitle");
                case "READY 진행 중": return Lang.T("extraDialog.progress.readyRunning");
                case "READY running": return Lang.T("extraDialog.progress.readyRunning");
                case "준비 동작 진행 중": return Lang.T("extraDialog.progress.readyRunning");
                case "READY 완료": return Lang.T("extraDialog.progress.readyComplete");
                case "READY completed": return Lang.T("extraDialog.progress.readyComplete");
                case "준비 동작 완료": return Lang.T("extraDialog.progress.readyComplete");
                case "READY 실패": return Lang.T("extraDialog.progress.readyFailed");
                case "READY failed": return Lang.T("extraDialog.progress.readyFailed");
                case "준비 동작 실패": return Lang.T("extraDialog.progress.readyFailed");
                case "READY 정지": return Lang.T("extraDialog.progress.readyCanceled");
                case "READY stopped": return Lang.T("extraDialog.progress.readyCanceled");
                case "준비 동작 정지": return Lang.T("extraDialog.progress.readyCanceled");
                case "READY 준비": return Lang.T("extraDialog.progress.readyIdle");
                case "READY preparing": return Lang.T("extraDialog.progress.readyIdle");
                case "준비 동작 준비": return Lang.T("extraDialog.progress.readyIdle");
                case "Ready 시퀀스를 준비합니다.": return Lang.T("extraDialog.progress.readyStep");
                case "Preparing the Ready sequence.": return Lang.T("extraDialog.progress.readyStep");
                case "준비 동작 시퀀스를 준비합니다.": return Lang.T("extraDialog.progress.readyStep");
                case "모션이 완료될 때까지 기다려 주세요.": return Lang.T("extraDialog.progress.readyMessage");
                case "Wait for motion to complete.": return Lang.T("extraDialog.progress.readyMessage");
                case "Stop": return Lang.T("extraDialog.progress.stopTitle");
                case "정지": return Lang.T("extraDialog.progress.stopTitle");
                case "정지 처리 중": return Lang.T("extraDialog.progress.stopRunning");
                case "Stopping": return Lang.T("extraDialog.progress.stopRunning");
                case "정지 완료": return Lang.T("extraDialog.progress.stopComplete");
                case "Stop completed": return Lang.T("extraDialog.progress.stopComplete");
                case "알람 발생": return Lang.T("extraDialog.progress.stopFailed");
                case "Alarm occurred": return Lang.T("extraDialog.progress.stopFailed");
                case "정지 처리": return Lang.T("extraDialog.progress.stopCanceled");
                case "Stop processing": return Lang.T("extraDialog.progress.stopCanceled");
                case "정지 요청": return Lang.T("extraDialog.progress.stopIdle");
                case "Stop requested": return Lang.T("extraDialog.progress.stopIdle");
                case "현재 동작 완료 대기": return Lang.T("extraDialog.progress.stopStep");
                case "Waiting for current operation to finish": return Lang.T("extraDialog.progress.stopStep");
                case "정지 요청이 접수되었습니다. 현재 이동/작업 완료 후 안전 경계에서 정지합니다.": return Lang.T("extraDialog.progress.stopMessage");
                case "Stop requested. The machine stops at a safe boundary after current motion/work completes.": return Lang.T("extraDialog.progress.stopMessage");
                case "정지 요청이 접수되었습니다. 현재 동작 완료 후 정지합니다.": return Lang.T("extraDialog.progress.stopAccepted");
                case "Stop requested. Stopping after the current operation finishes.": return Lang.T("extraDialog.progress.stopAccepted");
                case "사이클 정지 완료": return Lang.T("extraDialog.progress.cycleStopped");
                case "Cycle stop completed": return Lang.T("extraDialog.progress.cycleStopped");
                case "장비가 안전 경계에서 정지했습니다.": return Lang.T("extraDialog.progress.safeStopped");
                case "The machine stopped at a safe boundary.": return Lang.T("extraDialog.progress.safeStopped");
                case "알람 상태": return Lang.T("extraDialog.progress.alarmState");
                case "Alarm state": return Lang.T("extraDialog.progress.alarmState");
                case "정지 처리 중 알람이 발생했습니다. Alarm/Event Log를 확인하세요.": return Lang.T("extraDialog.progress.stopAlarm");
                case "An alarm occurred while stopping. Check the Alarm/Event Log.": return Lang.T("extraDialog.progress.stopAlarm");
                case "정지 처리 중 알람이 발생했습니다. 알람/이벤트 로그를 확인하세요.": return Lang.T("extraDialog.progress.stopAlarm");
                case "축 이동 완료 대기": return Lang.T("extraDialog.progress.waitAxes");
                case "Waiting for axis motion to finish": return Lang.T("extraDialog.progress.waitAxes");
                case "축 정지 안정 확인": return Lang.T("extraDialog.progress.settleAxes");
                case "Checking axes have settled": return Lang.T("extraDialog.progress.settleAxes");
                case "모든 축 이동 완료 확인 중입니다.": return Lang.T("extraDialog.progress.axesCompleting");
                case "Checking all axis motions have completed.": return Lang.T("extraDialog.progress.axesCompleting");
                default: return text;
            }
        }
    }
}
