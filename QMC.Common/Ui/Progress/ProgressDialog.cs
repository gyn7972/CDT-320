using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Ui.Controls;

namespace QMC.Common.Ui.Dialogs
{
    /// <summary>
    /// 진행 상황을 표시할 때 외부에서 전달하는 한 번 분량의 스냅샷.
    /// </summary>
    public sealed class ProgressInfo
    {
        public ProgressInfo(
            ProgressState state,
            int percent,
            int completedSteps,
            int totalSteps,
            string stepName,
            string message,
            string title = null)
        {
            State = state;
            Percent = Math.Max(0, Math.Min(100, percent));
            CompletedSteps = Math.Max(0, completedSteps);
            TotalSteps = Math.Max(0, totalSteps);
            StepName = stepName ?? string.Empty;
            Message = message ?? string.Empty;
            Title = title;
        }

        public ProgressState State { get; private set; }
        public int Percent { get; private set; }
        public int CompletedSteps { get; private set; }
        public int TotalSteps { get; private set; }
        public string StepName { get; private set; }
        public string Message { get; private set; }

        /// <summary>제목 직접 지정. null 이면 상태별 기본 제목을 사용한다.</summary>
        public string Title { get; private set; }
    }

    /// <summary>
    /// 공용 진행 팝업. 원형 Progress + 제목/단계/메시지 라벨로 구성된다.<br/>
    /// 특정 도메인(Ready/초기화/작업 등)에 의존하지 않으며, 외부에서 <see cref="ApplyProgress(ProgressInfo)"/> 로 구동한다.
    /// <list type="bullet">
    ///   <item><description>상태별 색/제목은 프로퍼티로 설정(Settings) 가능하다.</description></item>
    ///   <item><description>Failed 상태는 빨간 ✗(알람) 으로 표시한다.</description></item>
    ///   <item><description>이벤트가 비-UI 스레드에서 와도 안전하게 marshaling 한다.</description></item>
    /// </list>
    /// </summary>
    public class ProgressDialog : Form
    {
        private static readonly Color DefaultRunning = Color.FromArgb(33, 118, 210);
        private static readonly Color DefaultCompleted = Color.FromArgb(46, 125, 50);
        private static readonly Color DefaultFailed = Color.FromArgb(198, 40, 40);
        private static readonly Color DefaultCanceled = Color.FromArgb(120, 120, 120);
        private static readonly Color DialogBackColor = Color.FromArgb(248, 249, 251);
        private static readonly Color TextColor = Color.FromArgb(40, 44, 52);
        private static readonly Color SubTextColor = Color.FromArgb(96, 104, 116);

        private readonly CircularProgressView _progressView;
        private readonly Label _titleLabel;
        private readonly Label _stepLabel;
        private readonly Label _messageLabel;

        public ProgressDialog()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            // 메인 화면이 최대화/kiosk 일 때 뒤로 깔리지 않도록 항상 위에 표시.
            TopMost = true;
            BackColor = DialogBackColor;
            ClientSize = new Size(380, 372);
            Font = new Font("Malgun Gothic", 10F, FontStyle.Regular);
            Padding = new Padding(16, 14, 16, 14);

            _progressView = new CircularProgressView
            {
                Dock = DockStyle.Top,
                Height = 210,
                BackColor = DialogBackColor,
                ArcColor = DefaultRunning,
                Margin = new Padding(0)
            };

            _titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                ForeColor = DefaultRunning,
                Font = new Font("Malgun Gothic", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = RunningTitle
            };

            _stepLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = TextColor,
                Font = new Font("Malgun Gothic", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = DefaultStepText
            };

            _messageLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = SubTextColor,
                Font = new Font("Malgun Gothic", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(8, 4, 8, 0),
                Text = DefaultMessage
            };

            Controls.Add(_messageLabel);
            Controls.Add(_stepLabel);
            Controls.Add(_titleLabel);
            Controls.Add(_progressView);
        }

        // ─── Settings: 상태별 색상 ───
        public Color RunningColor { get; set; } = DefaultRunning;
        public Color CompletedColor { get; set; } = DefaultCompleted;
        public Color FailedColor { get; set; } = DefaultFailed;
        public Color CanceledColor { get; set; } = DefaultCanceled;

        // ─── Settings: 상태별 기본 제목/문구 ───
        public string IdleTitle { get; set; } = "준비 중";
        public string RunningTitle { get; set; } = "진행 중";
        public string CompletedTitle { get; set; } = "완료";
        public string FailedTitle { get; set; } = "실패";
        public string CanceledTitle { get; set; } = "정지";
        public string DefaultStepText { get; set; } = "진행 중입니다.";
        public string DefaultMessage { get; set; } = "잠시만 기다려 주세요.";

        /// <summary>진행 상황을 화면에 반영한다. 비-UI 스레드에서 호출해도 안전하다.</summary>
        public void ApplyProgress(ProgressInfo info)
        {
            if (info == null || IsDisposed)
                return;

            if (InvokeRequired)
            {
                if (!IsHandleCreated)
                    return;

                try
                {
                    BeginInvoke(new Action<ProgressInfo>(ApplyProgress), info);
                }
                catch
                {
                    // 폼이 닫히는 중이면 marshaling 이 실패할 수 있다. 무시한다.
                }
                return;
            }

            try
            {
                Color accent = ResolveAccent(info.State);

                _progressView.SetState(info.State, info.Percent, accent, info.CompletedSteps, info.TotalSteps);
                _titleLabel.ForeColor = accent;
                _titleLabel.Text = string.IsNullOrEmpty(info.Title) ? ResolveTitle(info.State) : info.Title;
                _stepLabel.Text = string.IsNullOrWhiteSpace(info.StepName) ? DefaultStepText : info.StepName;
                _messageLabel.Text = BuildMessage(info);
            }
            catch
            {
                // 표시 갱신 중 일시적 실패(폼 종료 등)는 무시한다.
            }
        }

        /// <summary>편의 오버로드.</summary>
        public void ApplyProgress(ProgressState state, int percent, int completedSteps, int totalSteps,
            string stepName, string message, string title = null)
        {
            ApplyProgress(new ProgressInfo(state, percent, completedSteps, totalSteps, stepName, message, title));
        }

        protected Color ResolveAccent(ProgressState state)
        {
            switch (state)
            {
                case ProgressState.Completed: return CompletedColor;
                case ProgressState.Failed: return FailedColor;
                case ProgressState.Canceled: return CanceledColor;
                default: return RunningColor;
            }
        }

        protected string ResolveTitle(ProgressState state)
        {
            switch (state)
            {
                case ProgressState.Completed: return CompletedTitle;
                case ProgressState.Failed: return FailedTitle;
                case ProgressState.Canceled: return CanceledTitle;
                case ProgressState.Idle: return IdleTitle;
                default: return RunningTitle;
            }
        }

        private string BuildMessage(ProgressInfo info)
        {
            string message = string.IsNullOrWhiteSpace(info.Message) ? DefaultMessage : info.Message;

            if (info.TotalSteps > 0)
                return message + Environment.NewLine +
                       info.CompletedSteps + " / " + info.TotalSteps + " 단계 완료";

            return message;
        }
    }
}
