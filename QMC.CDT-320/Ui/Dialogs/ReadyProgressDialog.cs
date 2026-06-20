using QMC.CDT320;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed class ReadyProgressDialog : Form
    {
        private readonly MachineController _controller;
        private readonly CircularProgressView _progressView;
        private readonly Label _titleLabel;
        private readonly Label _stepLabel;
        private readonly Label _messageLabel;

        public ReadyProgressDialog(MachineController controller)
        {
            _controller = controller;

            Text = "Ready";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(19, 64, 111);
            ClientSize = new Size(360, 320);
            Font = new Font("Malgun Gothic", 10F, FontStyle.Regular);

            _progressView = new CircularProgressView
            {
                Dock = DockStyle.Top,
                Height = 170,
                Margin = new Padding(0)
            };

            _titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "READY 진행 중"
            };

            _stepLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Ready 시퀀스를 준비합니다."
            };

            _messageLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(220, 235, 255),
                Font = new Font("Malgun Gothic", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(18, 4, 18, 0),
                Text = "모션이 완료될 때까지 기다려 주세요."
            };

            Controls.Add(_messageLabel);
            Controls.Add(_stepLabel);
            Controls.Add(_titleLabel);
            Controls.Add(_progressView);

            if (_controller != null)
            {
                _controller.ReadySequenceProgressChanged += OnReadySequenceProgressChanged;
                ApplyProgress(_controller.ReadySequenceProgress);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_controller != null)
                _controller.ReadySequenceProgressChanged -= OnReadySequenceProgressChanged;

            base.OnFormClosed(e);
        }

        private void OnReadySequenceProgressChanged(MachineReadyProgress progress)
        {
            ApplyProgress(progress);
        }

        public void ApplyProgress(MachineReadyProgress progress)
        {
            if (progress == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<MachineReadyProgress>(ApplyProgress), progress);
                return;
            }

            _progressView.Percent = progress.Percent;
            _stepLabel.Text = string.IsNullOrWhiteSpace(progress.CurrentStepName)
                ? "Ready 시퀀스를 진행합니다."
                : progress.CurrentStepName;
            _messageLabel.Text = BuildMessage(progress);

            if (progress.State == MachineReadySequenceState.Completed)
                _titleLabel.Text = "READY 완료";
            else if (progress.State == MachineReadySequenceState.Failed)
                _titleLabel.Text = "READY 실패";
            else if (progress.State == MachineReadySequenceState.Canceled)
                _titleLabel.Text = "READY 정지";
            else
                _titleLabel.Text = "READY 진행 중";
        }

        private static string BuildMessage(MachineReadyProgress progress)
        {
            string message = string.IsNullOrWhiteSpace(progress.Message)
                ? "모션이 완료될 때까지 기다려 주세요."
                : progress.Message;

            if (progress.TotalSteps > 0)
                return message + Environment.NewLine +
                       progress.CompletedSteps + " / " + progress.TotalSteps + " 단계 완료";

            return message;
        }

        private sealed class CircularProgressView : Control
        {
            private int _percent;

            public CircularProgressView()
            {
                DoubleBuffered = true;
                BackColor = Color.Transparent;
                ForeColor = Color.White;
            }

            public int Percent
            {
                get { return _percent; }
                set
                {
                    int next = Math.Max(0, Math.Min(100, value));
                    if (_percent == next)
                        return;

                    _percent = next;
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int size = Math.Min(Width, Height) - 36;
                if (size < 32)
                    return;

                Rectangle rect = new Rectangle(
                    (Width - size) / 2,
                    (Height - size) / 2 + 4,
                    size,
                    size);

                using (var basePen = new Pen(Color.FromArgb(8, 18, 38), 18))
                using (var progressPen = new Pen(Color.FromArgb(18, 105, 255), 18))
                using (var textBrush = new SolidBrush(Color.White))
                using (var textFont = new Font("Malgun Gothic", 24F, FontStyle.Bold))
                {
                    basePen.StartCap = LineCap.Round;
                    basePen.EndCap = LineCap.Round;
                    progressPen.StartCap = LineCap.Round;
                    progressPen.EndCap = LineCap.Round;

                    e.Graphics.DrawArc(basePen, rect, -90, 360);
                    e.Graphics.DrawArc(progressPen, rect, -90, (float)(360.0 * Percent / 100.0));

                    string text = Percent + "%";
                    SizeF textSize = e.Graphics.MeasureString(text, textFont);
                    e.Graphics.DrawString(
                        text,
                        textFont,
                        textBrush,
                        rect.Left + (rect.Width - textSize.Width) / 2,
                        rect.Top + (rect.Height - textSize.Height) / 2);
                }
            }
        }
    }
}
