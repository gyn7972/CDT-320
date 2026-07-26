using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>로그 전용 공용 설정 판넬(LogSettingsPanelControl)을 담는 팝업 창.
    /// 하단 SAVE/CLOSE 버튼 제공. SAVE 는 설정을 저장하고 창을 닫는다.
    /// 타이틀바 X(닫기)는 동작하지 않으며 CLOSE 버튼으로만 닫는다.</summary>
    public class LogSettingsDialog : Form
    {
        private readonly LogSettingsPanelControl _panel;
        private bool _allowClose;

        public LogSettingsDialog()
        {
            Text = "LOG SETTINGS";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            // [레이아웃 정정 2026-07-27] 기존 674 클라이언트 높이로는
            //   DIAGNOSTIC LOG(86) + 판넬 고정행 합계 + SAVE/CLOSE 바(52) 가 들어가지 않아
            //   VISION IMAGE 그룹 하단이 잘려 보였다.
            // 판넬이 요구하는 최소 높이를 담을 수 있도록 키우고, 그 아래로는 줄일 수 없게 MinimumSize 를 맞춘다.
            //   DIAGNOSTIC 86 + LOG MAINTENANCE 130 + MATERIAL SNAPSHOT 78
            //   + LOG FILE PATH(가변, 최소 200) + VISION IMAGE 176 + 버튼바 52 = 722
            ClientSize = new Size(760, 800);
            MinimumSize = new Size(776, 761);
            BackColor = Color.White;
            Font = new Font("맑은 고딕", 9F);

            _panel = new LogSettingsPanelControl { Dock = DockStyle.Fill };

            var bottomBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var btnClose = new Button
            {
                Text = "CLOSE",
                Size = new Size(140, 36),
                Margin = new Padding(6, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = true,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => { _allowClose = true; Close(); };

            var btnSave = new Button
            {
                Text = "SAVE",
                Size = new Size(140, 36),
                Margin = new Padding(6, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(230, 88, 31),
                ForeColor = Color.White,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            btnSave.Click += (s, e) =>
            {
                _panel.Save();
                MessageBox.Show(this, "저장되었습니다.", "LOG SETTINGS",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _allowClose = true;
                Close();
            };

            bottomBar.Controls.Add(btnClose); // 우측 끝
            bottomBar.Controls.Add(btnSave);  // 그 왼쪽

            Controls.Add(_panel);     // Fill (먼저 추가)
            Controls.Add(bottomBar);  // Bottom

            // 타이틀바 X(사용자 닫기)는 막고, CLOSE/SAVE 버튼으로만 닫히게 한다.
            FormClosing += (s, e) =>
            {
                if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
            };
        }
    }
}
