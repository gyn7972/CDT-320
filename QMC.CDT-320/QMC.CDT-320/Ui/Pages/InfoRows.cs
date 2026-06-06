using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages
{
    /// <summary>
    /// 300 UI에서 반복적으로 쓰이는 "Label | Value" 행 구조 생성 헬퍼.
    /// 예: STAGE EXIST / 없음, STAGE ALIGN / 미완료, ...
    /// </summary>
    public static class InfoRows
    {
        /// <summary>좌측 라벨(주황→사실 회색 블록) + 우측 흰 값 박스.</summary>
        public static Control Pair(string i18nKey, string value, int labelW = 160, int valueW = 220, bool valueIsStatus = false, Color? valueColor = null)
        {
            var row = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount    = 1,
                Height      = 32,
                Width       = labelW + valueW,
                BackColor   = Color.Transparent,
                Margin      = new Padding(0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelW));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, valueW));
            row.Controls.Add(MakeLabelCell(i18nKey), 0, 0);
            row.Controls.Add(MakeValueCell(value, valueIsStatus, valueColor), 1, 0);
            return row;
        }

        /// <summary>Dock=Top 으로 쌓이는 row — InfoPanel 안에 사용.</summary>
        public static Panel InfoPanel(params Control[] rows)
        {
            var panel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = UiTheme.MainBg,
                Padding       = new Padding(4)
            };
            foreach (var r in rows) panel.Controls.Add(r);
            return panel;
        }

        public static Label MakeLabelCell(string i18nKey) => new Label
        {
            Dock      = DockStyle.Fill,
            Text      = Lang.T(i18nKey),
            Tag       = "i18n:" + i18nKey,
            BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
            ForeColor = Color.Black,
            Font      = new Font("맑은 고딕", 9F),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
            Margin    = new Padding(1)
        };

        public static Label MakeValueCell(string value, bool isStatus = false, Color? color = null) => new Label
        {
            Dock      = DockStyle.Fill,
            Text      = value ?? "",
            BackColor = Color.White,
            ForeColor = color ?? (isStatus ? Color.Green : Color.Black),
            Font      = isStatus ? new Font("맑은 고딕", 9F, FontStyle.Bold) : new Font("Consolas", 10F),
            TextAlign = ContentAlignment.MiddleRight,
            Padding   = new Padding(0, 0, 6, 0),
            Margin    = new Padding(1)
        };
    }
}
