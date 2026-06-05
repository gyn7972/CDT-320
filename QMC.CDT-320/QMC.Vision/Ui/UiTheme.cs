using System.Drawing;

namespace QMC.Vision.Ui
{
    /// <summary>CDT-300 스타일 공용 색상/폰트 (QMC.CDT-320 Handler UiTheme 과 동일 톤).</summary>
    public static class UiTheme
    {
        // ─── 색상 ─────────────────────────────────
        public static readonly Color HeaderBg        = Color.FromArgb(0x2D, 0x2D, 0x30);
        public static readonly Color HeaderFg        = Color.White;
        public static readonly Color StatusBarBg     = Color.FromArgb(0xD9, 0x77, 0x06);
        public static readonly Color StatusBarFg     = Color.White;
        public static readonly Color MainBg          = Color.FromArgb(0xBF, 0xBF, 0xBF);
        public static readonly Color OptionPanelBg   = Color.FromArgb(0xF0, 0xF0, 0xF0);
        public static readonly Color VisionBg        = Color.Black;
        public static readonly Color VisionInfoFg    = Color.LightGreen;
        public static readonly Color Accent          = Color.FromArgb(0xE8, 0x5D, 0x1A);

        public static readonly Color BottomBarBg     = Color.FromArgb(0x2D, 0x2D, 0x30);
        public static readonly Color BottomBarFg     = Color.White;

        // 설정 페이지용 사이드바
        public static readonly Color SidebarBg       = Color.FromArgb(0x59, 0x59, 0x59);
        public static readonly Color SidebarHeaderBg = Color.FromArgb(0xF0, 0xF0, 0xF0);
        public static readonly Color SidebarHeaderFg = Color.Black;
        public static readonly Color SidebarBtnBg    = Color.FromArgb(0x59, 0x59, 0x59);
        public static readonly Color SidebarBtnFg    = Color.White;
        public static readonly Color SidebarBtnSelBg = Color.White;
        public static readonly Color SidebarBtnSelFg = Color.FromArgb(0x22, 0x22, 0x22);

        // ─── 레이아웃 ─────────────────────────────
        public const int HeaderHeight    = 70;
        public const int StatusBarHeight = 28;
        public const int BottomBarHeight = 80;
        public const int SidebarWidth    = 210;

        // ─── 폰트 ─────────────────────────────────
        public static readonly Font SectionFont   = new Font("맑은 고딕", 11F, FontStyle.Bold);
        public static readonly Font ButtonFont    = new Font("맑은 고딕", 11F);
        public static readonly Font ValueFont     = new Font("Consolas", 10F);
        public static readonly Font BottomBtnFont = new Font("맑은 고딕", 10F);
    }
}
