using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 300 UI 공용 오렌지 모드 오버레이 다이얼로그.
    /// 상단: 오렌지 배경 + 한글 큰 텍스트. 하단: ACTION 버튼 N개.
    /// </summary>
    public class ModeOverlayDialog : Form
    {
        private readonly List<ActionButton> _actionButtons = new List<ActionButton>();
        private readonly Panel _topPanel;
        private readonly Label _titleLabel;
        private readonly FlowLayoutPanel _actionArea;

        /// <summary>특정 ActionButton이 눌렸을 때 리턴할 결과 텍스트.</summary>
        public string SelectedAction { get; private set; }

        public ModeOverlayDialog(string titleI18n)
        {
            Text            = Lang.T(titleI18n);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MinimizeBox     = MaximizeBox = false;
            ClientSize      = new Size(560, 420);
            BackColor       = Color.FromArgb(0xBB, 0xBB, 0xBB);
            ShowIcon        = false;

            _topPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 260,
                BackColor = UiTheme.StatusBarBg
            };
            _titleLabel = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = Lang.T(titleI18n),
                Tag       = "i18n:" + titleI18n,
                ForeColor = Color.White,
                BackColor = UiTheme.StatusBarBg,
                Font      = new Font("맑은 고딕", 44F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _topPanel.Controls.Add(_titleLabel);
            Controls.Add(_topPanel);

            _actionArea = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                Padding       = new Padding(20, 16, 20, 16),
                BackColor     = Color.FromArgb(0xBB, 0xBB, 0xBB)
            };
            Controls.Add(_actionArea);
            Controls.SetChildIndex(_actionArea, 0);

            Load += (s, e) => Lang.Apply(this);
        }

        public ActionButton AddAction(string text, Action onClick = null, int width = 140)
        {
            var btn = new ActionButton
            {
                Text   = text,
                Size   = new Size(width, 70),
                Margin = new Padding(6)
            };
            btn.Click += (s, e) =>
            {
                SelectedAction = text;
                try { onClick?.Invoke(); } catch { }
                DialogResult = DialogResult.OK;
                Close();
            };
            _actionButtons.Add(btn);
            _actionArea.Controls.Add(btn);
            return btn;
        }

        /// <summary>타이틀을 2줄 이상 한글 큰 글자로 표시하려면 호출.</summary>
        public void SetTitle(string titleI18n)
        {
            _titleLabel.Tag  = "i18n:" + titleI18n;
            _titleLabel.Text = Lang.T(titleI18n);
        }
    }
}
