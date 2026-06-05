using System.Drawing;

namespace QMC.CDT_320.Ui
{
    /// <summary>
    /// CDT-320 UI 공통 색상/크기 상수.
    /// </summary>
    public static class UiTheme
    {
        // 색상
        public static readonly Color HeaderBg           = Color.FromArgb(0x2D, 0x2D, 0x30);
        public static readonly Color HeaderFg           = Color.White;
        public static readonly Color LogoOrange         = Color.FromArgb(0xE8, 0x5D, 0x1A);
        public static readonly Color StatusBarBg        = Color.FromArgb(0xD9, 0x77, 0x06);
        public static readonly Color StatusBarFg        = Color.White;
        public static readonly Color MainBg             = Color.FromArgb(0xBF, 0xBF, 0xBF);
        public static readonly Color OptionHeaderBg     = Color.FromArgb(0xD9, 0x77, 0x06);
        public static readonly Color OptionHeaderFg     = Color.White;
        public static readonly Color OptionPanelBg      = Color.FromArgb(0xF0, 0xF0, 0xF0);
        public static readonly Color SidebarBg          = Color.FromArgb(0x59, 0x59, 0x59);
        public static readonly Color SidebarHeaderBg    = Color.FromArgb(0xF0, 0xF0, 0xF0);
        public static readonly Color SidebarHeaderFg    = Color.Black;
        public static readonly Color SidebarBtnBg       = Color.FromArgb(0x59, 0x59, 0x59);
        public static readonly Color SidebarBtnFg       = Color.White;
        public static readonly Color SidebarBtnSelBg    = Color.White;
        public static readonly Color SidebarBtnSelFg    = Color.FromArgb(0x22, 0x22, 0x22);
        public static readonly Color BottomBarBg        = Color.FromArgb(0x2D, 0x2D, 0x30);
        public static readonly Color BottomBarFg        = Color.White;
        public static readonly Color MenuLabelBg        = Color.FromArgb(0x40, 0x40, 0x40);
        public static readonly Color MenuLabelFg        = Color.FromArgb(0x90, 0x90, 0x90);
        public static readonly Color VisionBg           = Color.Black;
        public static readonly Color VisionInfoFg       = Color.LightGreen;
        public static readonly Color ValueBoxBg         = Color.White;
        public static readonly Color ValueBoxFg         = Color.Black;
        public static readonly Color DotVision          = Color.FromArgb(0x00, 0xBC, 0xD4);
        public static readonly Color DotPick            = Color.FromArgb(0xF5, 0xC7, 0x18);
        public static readonly Color DotReference       = Color.FromArgb(0xBD, 0xBD, 0xBD);
        public static readonly Color DotOff             = Color.FromArgb(0x55, 0x55, 0x55);
        public static readonly Color Accent             = Color.FromArgb(0xE8, 0x5D, 0x1A);

        // 레이아웃 크기 (1920 x 1080 기준)
        public const int DesignWidth     = 1920;
        public const int DesignHeight    = 1080;
        public const int HeaderHeight    = 70;
        public const int StatusBarHeight = 30;
        public const int BottomBarHeight = 80;
        public const int MenuLabelWidth  = 16;
        public const int SidebarWidth    = 210;
        public const int OptionWidth     = 340;

        public const int ShellContentWidth  = DesignWidth - (MenuLabelWidth * 2);
        public const int ShellContentHeight = DesignHeight - HeaderHeight - StatusBarHeight - BottomBarHeight;
        public const int PageContentWidth   = ShellContentWidth - SidebarWidth;
        public const int PageContentHeight  = ShellContentHeight;

        // 폰트
        public static readonly Font TitleFont       = new Font("Segoe UI Light", 28F, FontStyle.Regular);
        public static readonly Font TitleSmallFont  = new Font("Segoe UI", 11F);
        public static readonly Font HeaderInfoFont  = new Font("맑은 고딕", 9F);
        public static readonly Font StatusBarFont   = new Font("맑은 고딕", 10F, FontStyle.Bold);
        public static readonly Font SectionFont     = new Font("맑은 고딕", 11F, FontStyle.Bold);
        public static readonly Font ButtonFont      = new Font("맑은 고딕", 11F);
        public static readonly Font ValueFont       = new Font("Consolas", 10F);
        public static readonly Font MenuLabelFont   = new Font("Segoe UI", 8F);
        public static readonly Font BottomBtnFont   = new Font("맑은 고딕", 10F);
        public static readonly Font ModeOverlayFont = new Font("맑은 고딕", 42F, FontStyle.Bold);
    }
}
