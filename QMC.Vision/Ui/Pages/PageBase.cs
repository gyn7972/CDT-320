using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;
using QMC.Vision.Ui.Localization;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 서브 페이지 UserControl 공통 베이스 — Handler(QMC.CDT_320.Ui.Pages.PageBase) 미러.
    /// 모든 비전 서브 페이지가 이를 상속해 배경/스크롤/섹션헤더/버튼규격/값행을 일관되게 구성한다.
    /// 디자이너 지원을 위해 파라미터 없는 기본 생성자를 유지한다.
    /// </summary>
    public class PageBase : UserControl
    {
        public PageBase()
        {
            BackColor      = UiTheme.MainBg;
            DoubleBuffered = true;
            // 콘텐츠가 표시 영역을 넘치면 좌측 트리/하단 액션이 잘리는 이슈 방지(Handler 동일).
            AutoScroll     = true;
        }

        /// <summary>VS 디자이너 모드 체크.</summary>
        protected bool IsDesignerMode()
            => LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        /// <summary>페이지 상단 주황색 섹션 헤더 (CDT-300 스타일). i18n 키로 생성.</summary>
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
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
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
            var title = new Label
            {
                BackColor   = Color.FromArgb(0xD0, 0xD0, 0xD0),
                BorderStyle = BorderStyle.FixedSingle,
                Dock        = DockStyle.Fill,
                Font        = UiTheme.ButtonFont,
                Padding     = new Padding(8, 0, 0, 0),
                Text        = key,
                TextAlign   = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(title, 0, row);
            layout.Controls.Add(valueLabel, 1, row);
        }

        protected static void AddDotRow(TableLayoutPanel layout, int row, IndicatorDot dot, string text)
        {
            var label = new Label
            {
                BackColor   = Color.FromArgb(0xD0, 0xD0, 0xD0),
                BorderStyle = BorderStyle.FixedSingle,
                Dock        = DockStyle.Fill,
                Font        = UiTheme.ButtonFont,
                Padding     = new Padding(8, 0, 0, 0),
                Text        = text,
                TextAlign   = ContentAlignment.MiddleLeft
            };
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
