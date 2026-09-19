using System.Globalization;

namespace QMC.Common.Localization
{
    /// <summary>프로젝트 또는 공통 리소스에서 지정한 문화권의 문구만 조회합니다.</summary>
    public interface ILocalizationSource
    {
        /// <summary>
        /// 부모 문화권이나 다른 언어로 대체하지 않고 정확히 지정한 문화권의 키를 조회합니다.
        /// 해당 문구가 있으면 빈 문자열을 포함하여 true를 반환합니다.
        /// </summary>
        bool TryGetString(string key, CultureInfo culture, out string value);
    }
}
