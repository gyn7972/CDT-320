using System.Drawing;

namespace QMC.Vision.Ui
{
    /// <summary>CDT-300 스타일 공용 색상/폰트 (QMC.CDT-320 UiTheme 과 동일 톤).</summary>
    public static class UiTheme
    {
        public static readonly Color HeaderBg       = Color.FromArgb(0x2D, 0x2D, 0x30);
        public static readonly Color StatusBarBg    = Color.FromArgb(0xD9, 0x77, 0x06);
        public static readonly Color MainBg         = Color.FromArgb(0xBF, 0xBF, 0xBF);
        public static readonly Color OptionPanelBg  = Color.FromArgb(0xF0, 0xF0, 0xF0);
        public static readonly Color VisionBg       = Color.Black;
        public static readonly Color VisionInfoFg   = Color.LightGreen;
        public static readonly Color Accent         = Color.FromArgb(0xE8, 0x5D, 0x1A);

        public static readonly Font SectionFont     = new Font("맑은 고딕", 11F, FontStyle.Bold);
        public static readonly Font ButtonFont      = new Font("맑은 고딕", 11F);
        public static readonly Font ValueFont       = new Font("Consolas", 10F);
    }
}
