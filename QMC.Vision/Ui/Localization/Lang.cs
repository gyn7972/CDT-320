using System.Windows.Forms;

namespace QMC.Vision.Ui.Localization
{
    /// <summary>
    /// 다국어 텍스트 — 핸들러 QMC.CDT_320.Ui.Localization.Lang 정렬(경량 stub).
    /// 현재 Vision 은 한국어 단일이라 키(=표시 문자열)를 그대로 반환한다.
    /// 다국어가 필요해지면 여기서 사전(ko/en)을 채우고 Apply 를 구현한다.
    /// </summary>
    public static class Lang
    {
        /// <summary>현재 언어 코드(기본 "ko").</summary>
        public static string Current { get; set; } = "ko";

        /// <summary>i18n 키 → 표시 문자열. (stub: 키를 그대로 반환)</summary>
        public static string T(string key) => key;

        /// <summary>컨트롤 트리의 문구 일괄 번역. (stub: no-op)</summary>
        public static void Apply(Control root) { }
    }
}
