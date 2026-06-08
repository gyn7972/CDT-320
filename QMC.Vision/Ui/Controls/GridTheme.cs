using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// R2e — DataGridView 시각 테마 통일 헬퍼. ParameterGridControl 룩(헤더 회색·가로격자·행높이 30 등)을
    /// 임의 그리드에 런타임 적용해 타깃 페이지 전체(PARAMETERS·검사조명 등)의 표 표현을 일치시킨다.
    /// 동작/컬럼 구성은 건드리지 않고 스타일만 통일(디자이너 직렬화 무관 — Code 적용).
    /// </summary>
    public static class GridTheme
    {
        public static readonly Color SurfaceBg  = Color.FromArgb(245, 246, 248);
        public static readonly Color HeaderBg   = Color.FromArgb(232, 234, 237);
        public static readonly Color HeaderFg   = Color.FromArgb(48, 52, 58);
        public static readonly Color GridLine   = Color.FromArgb(226, 228, 232);
        public static readonly Color NameFg     = Color.FromArgb(37, 41, 46);
        public static readonly Color SelectBg   = Color.FromArgb(221, 235, 255);

        public static void Apply(DataGridView g)
        {
            if (g == null) return;
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = SurfaceBg;
            g.GridColor = GridLine;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.EnableHeadersVisualStyles = false;
            g.RowHeadersVisible = false;
            g.AllowUserToResizeRows = false;
            g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.ColumnHeadersHeight = 28;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.RowTemplate.Height = 30;

            var hs = g.ColumnHeadersDefaultCellStyle;
            hs.BackColor = HeaderBg;
            hs.ForeColor = HeaderFg;
            hs.SelectionBackColor = HeaderBg;
            hs.SelectionForeColor = HeaderFg;
            hs.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            hs.Alignment = DataGridViewContentAlignment.MiddleLeft;

            var rs = g.DefaultCellStyle;
            rs.SelectionBackColor = SelectBg;
            rs.SelectionForeColor = NameFg;
            rs.Font = new Font("Consolas", 9.5F);
        }
    }
}
