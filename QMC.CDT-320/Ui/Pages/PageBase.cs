using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Util;
using QMC.CDT320.Sequencing;

namespace QMC.CDT_320.Ui.Pages
{
    /// <summary>
    /// 서브 페이지 UserControl 공통 베이스.
    /// Host(Form1)는 생성자에서 주입받지 않고, 부모 TabBase에서 정적 참조로 접근 가능.
    /// 단, 디자이너 지원을 위해 파라미터 없는 기본 생성자를 유지한다.
    /// </summary>
    public class PageBase : UserControl
    {
        /// <summary>
        /// Stage 60 — 페이지가 처음 표시될 때 1회 호출. Click 핸들러가 부착되지 않은
        /// Button/ActionButton/SidebarButton 에 placeholder 피드백 핸들러를 자동 부착.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            try { UiClickAuditor.EnsureFeedback(this); } catch { }
        }

        /// <summary>페이지 상단 주황색 섹션 헤더 (CDT-300 스타일).</summary>
        protected Label CreateSectionHeader(string i18nKey, int width = 0)
        {
            var h = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 30,
                Text      = Lang.T(i18nKey),
                Tag       = "i18n:" + i18nKey,
                BackColor = UiTheme.StatusBarBg,
                ForeColor = UiTheme.StatusBarFg,
                Font      = UiTheme.SectionFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            if (width > 0) { h.Dock = DockStyle.None; h.Width = width; }
            return h;
        }

        public PageBase()
        {
            BackColor      = UiTheme.MainBg;
            DoubleBuffered = true;
            UiDoubleBuffer.Enable(this);
            // Stage 60 — 일부 페이지(StageRecipePage/VisionRecipePage 등) 콘텐츠가 표시 영역을 넘쳐
            // 좌측 옵션 패널 / 트리·리스트 / 하단 액션 영역이 잘려 보이는 이슈가 있다.
            // 사용자 보고: "왼쪽에 트리 컨트롤 있는 페이지들 이게 가려서 안보이는것들 있어".
            // AutoScroll = true 로 콘텐츠 초과 시 스크롤바를 자동 표시한다.
            AutoScroll     = true;
        }

        /// <summary>VS 디자이너 모드 체크.</summary>
        protected bool IsDesignerMode()
            => LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        public static bool ShouldRefreshVisible(Control control)
        {
            try
            {
                if (control == null || control.IsDisposed || !control.IsHandleCreated)
                    return false;

                Control current = control;
                while (current != null)
                {
                    if (!current.Visible)
                        return false;

                    current = current.Parent;
                }

                Form form = control.FindForm();
                if (form != null && (!form.Visible || form.WindowState == FormWindowState.Minimized))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        protected bool TryAskManualSequenceStartMode(string actionName, out SequenceStartMode startMode)
        {
            startMode = SequenceStartMode.Resume;

            DialogResult result = QMC.Common.MessageDialog.Show(
                this,
                (string.IsNullOrWhiteSpace(actionName) ? "Manual Sequence" : actionName) +
                " 시퀀스를 어떻게 시작할까요?\r\n\r\n" +
                "[예] 처음 Step부터 시작\r\n" +
                "[아니오] 현재 저장된 Step부터 진행\r\n" +
                "[취소] 시작 안 함",
                "Manual Sequence Start",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
                return false;

            startMode = result == DialogResult.Yes
                ? SequenceStartMode.Restart
                : SequenceStartMode.Resume;
            return true;
        }

        protected static void ConfigureGroup(GroupBox group, string title, Control child)
        {
            group.BackColor = UiTheme.OptionPanelBg;
            group.Controls.Add(child);
            group.Dock = DockStyle.Fill;
            group.Font = UiTheme.SectionFont;
            group.Text = title;
        }

        protected static void ConfigureGroup(GroupBox group, string title, TableLayoutPanel layout)
        {
            ConfigureGroup(group, title, (Control)layout);
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12, 18, 12, 12);
        }

        protected static void ConfigureGroup(GroupBox group, string title, TableLayoutPanel layout, int rows)
        {
            ConfigureGroup(group, title, layout);
            layout.RowCount = rows;
            for (int i = 0; i < rows; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            }
        }

        protected static void ConfigureDot(IndicatorDot dot, Color onColor)
        {
            dot.Dock = DockStyle.Fill;
            dot.Margin = new Padding(6, 8, 6, 8);
            dot.OnColor = onColor;
        }

        protected static void ConfigureValueLabel(Label label, string text)
        {
            label.BackColor = Color.White;
            label.BorderStyle = BorderStyle.FixedSingle;
            label.Dock = DockStyle.Fill;
            label.Font = UiTheme.ValueFont;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleCenter;
        }

        protected static void AddValueRow(TableLayoutPanel layout, int row, string key, Label valueLabel)
        {
            var title = new Label();
            title.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            title.BorderStyle = BorderStyle.FixedSingle;
            title.Dock = DockStyle.Fill;
            title.Font = UiTheme.ButtonFont;
            title.Padding = new Padding(8, 0, 0, 0);
            title.Text = key;
            title.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(title, 0, row);
            layout.Controls.Add(valueLabel, 1, row);
        }

        protected static void AddDotRow(TableLayoutPanel layout, int row, IndicatorDot dot, string text)
        {
            var label = new Label();
            label.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            label.BorderStyle = BorderStyle.FixedSingle;
            label.Dock = DockStyle.Fill;
            label.Font = UiTheme.ButtonFont;
            label.Padding = new Padding(8, 0, 0, 0);
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(dot, 0, row);
            layout.Controls.Add(label, 1, row);
        }

        protected static void ConfigureActionButton(ActionButton button, string text, int width)
        {
            button.Font = UiTheme.ButtonFont;
            button.Height = 50;
            button.Margin = new Padding(6);
            button.Text = text;
            button.Width = width;
        }

        protected static void ConfigureActionButton(ActionButton button, string text, string tag, int width)
        {
            ConfigureActionButton(button, text, width);
            button.Tag = tag;
        }
    }
}

