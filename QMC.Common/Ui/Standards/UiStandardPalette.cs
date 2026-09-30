using System.Drawing;

namespace QMC.Common.Ui.Standards
{
    // 기존 카탈로그의 버튼 기본색을 한 곳에서 관리한다.
    // CDT의 UiTheme도 공유 배경색만 참조하며, 헤더 글자색/배치/글꼴은 기존 값을 유지한다.
    public static class UiStandardPalette
    {
        public static readonly Color DefaultBackColor = Color.White;
        public static readonly Color DefaultForeColor = Color.Black;
        public static readonly Color DefaultBorderColor = Color.FromArgb(128, 128, 128);

        public static readonly Color PrimaryBackColor = Color.FromArgb(217, 119, 6);
        public static readonly Color PrimaryForeColor = Color.Black;

        public static readonly Color DarkBackColor = Color.FromArgb(64, 64, 64);
        public static readonly Color DarkForeColor = Color.White;

        public static readonly Color DangerBackColor = Color.FromArgb(178, 34, 34);
        public static readonly Color DangerForeColor = Color.White;
    }
}
