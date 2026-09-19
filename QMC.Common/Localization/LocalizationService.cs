using System;
using System.Collections.Generic;
using System.Globalization;

namespace QMC.Common.Localization
{
    /// <summary>
    /// 화면 표시용 언어와 문구 조회를 관리합니다. 숫자 처리에 영향을 주는
    /// 스레드의 CurrentCulture 및 CurrentUICulture는 변경하지 않습니다.
    /// </summary>
    public sealed class LocalizationService
    {
        private readonly ILocalizationSource[] _sources;
        private readonly Dictionary<string, string> _supportedNames;
        private readonly Dictionary<string, CultureInfo[]> _lookupCultures;
        private volatile string _current;

        /// <summary>현재 선택한 지원 언어의 정규화된 문화권 이름입니다.</summary>
        public string Current => _current;

        /// <summary>생성 시 전달된 지원 언어를 복사한 읽기 전용 목록입니다.</summary>
        public IReadOnlyList<string> Supported { get; }

        /// <summary>
        /// 언어가 실제로 바뀌면 SetLanguage 호출 스레드에서 발생합니다.
        /// 화면 갱신 및 UI 스레드 전환은 구독자가 담당합니다.
        /// </summary>
        public event Action LanguageChanged;

        /// <summary>
        /// 소스는 앞쪽 항목부터 우선합니다. 현재 언어 및 부모 문화권, 대체 언어 및
        /// 각 부모 문화권, 중립 리소스 순서로 조회하며 같은 문화권 안에서 소스 우선순위를 적용합니다.
        /// 모든 배열은 복사하며 초기 언어는 지원 언어에 포함되어야 합니다.
        /// </summary>
        public LocalizationService(ILocalizationSource[] sources, string[] supportedLanguages,
            string initialLanguage, string[] fallbackLanguages)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (supportedLanguages == null) throw new ArgumentNullException(nameof(supportedLanguages));
            if (fallbackLanguages == null) throw new ArgumentNullException(nameof(fallbackLanguages));
            if (supportedLanguages.Length == 0)
                throw new ArgumentException("지원 언어가 하나 이상 필요합니다.", nameof(supportedLanguages));

            _sources = (ILocalizationSource[])sources.Clone();
            foreach (ILocalizationSource source in _sources)
                if (source == null)
                    throw new ArgumentException("문구 소스에는 null을 넣을 수 없습니다.", nameof(sources));

            _supportedNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _lookupCultures = new Dictionary<string, CultureInfo[]>(StringComparer.OrdinalIgnoreCase);
            var names = new string[supportedLanguages.Length];
            var fallbacks = new CultureInfo[fallbackLanguages.Length];
            for (int i = 0; i < fallbacks.Length; i++)
                fallbacks[i] = ReadLanguage(fallbackLanguages[i], nameof(fallbackLanguages));

            for (int i = 0; i < names.Length; i++)
            {
                CultureInfo culture = ReadLanguage(supportedLanguages[i], nameof(supportedLanguages));
                if (_supportedNames.ContainsKey(culture.Name))
                    throw new ArgumentException("지원 언어가 중복되었습니다.", nameof(supportedLanguages));
                names[i] = culture.Name;
                _supportedNames.Add(culture.Name, culture.Name);
                _lookupCultures.Add(culture.Name, CreateLookupCultures(culture, fallbacks));
            }
            Supported = Array.AsReadOnly(names);

            string current;
            if (string.IsNullOrEmpty(initialLanguage) || !_supportedNames.TryGetValue(initialLanguage, out current))
                throw new ArgumentException("초기 언어가 지원 언어에 포함되어야 합니다.", nameof(initialLanguage));
            _current = current;
        }

        /// <summary>문화권 이름의 대소문자를 구분하지 않고 지원 여부를 확인합니다.</summary>
        public bool HasLanguage(string language)
        {
            return !string.IsNullOrEmpty(language) && _supportedNames.ContainsKey(language);
        }

        /// <summary>지원하지 않거나 현재와 같은 언어는 무시합니다.</summary>
        public void SetLanguage(string language)
        {
            string supported;
            if (string.IsNullOrEmpty(language) || !_supportedNames.TryGetValue(language, out supported)) return;
            if (string.Equals(_current, supported, StringComparison.Ordinal)) return;
            _current = supported;
            LanguageChanged?.Invoke();
        }

        /// <summary>현재 언어의 문구를 조회합니다. 빈 키는 빈 문자열, 누락된 문구는 키를 반환합니다.</summary>
        public string GetString(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string value;
            return TryGetString(key, _lookupCultures[_current], out value) ? value : key;
        }

        /// <summary>
        /// 지정한 언어, 부모 문화권, 중립 리소스만 조회합니다.
        /// 서비스의 대체 언어 목록은 사용하지 않으며 지원 목록 밖의 유효한 문화권도 조회할 수 있습니다.
        /// 잘못된 문화권 이름이나 빈 키는 false를 반환합니다.
        /// </summary>
        public bool TryGetString(string key, string language, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key) || language == null) return false;
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
            return TryGetString(key, CreateLookupCultures(culture, new CultureInfo[0]), out value);
        }

        private bool TryGetString(string key, CultureInfo[] cultures, out string value)
        {
            foreach (CultureInfo culture in cultures)
                foreach (ILocalizationSource source in _sources)
                    if (source.TryGetString(key, culture, out value)) return true;
            value = null;
            return false;
        }

        private static CultureInfo ReadLanguage(string language, string parameterName)
        {
            if (string.IsNullOrEmpty(language))
                throw new ArgumentException("비어 있지 않은 문화권 이름이 필요합니다.", parameterName);
            return CultureInfo.GetCultureInfo(language);
        }

        private static CultureInfo[] CreateLookupCultures(CultureInfo culture, CultureInfo[] fallbacks)
        {
            var cultures = new List<CultureInfo>();
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddCultureAndParents(culture, cultures, added);
            foreach (CultureInfo fallback in fallbacks)
                AddCultureAndParents(fallback, cultures, added);
            cultures.Add(CultureInfo.InvariantCulture);
            return cultures.ToArray();
        }

        private static void AddCultureAndParents(CultureInfo culture, List<CultureInfo> cultures, HashSet<string> added)
        {
            for (CultureInfo candidate = culture; !string.IsNullOrEmpty(candidate.Name); candidate = candidate.Parent)
                if (added.Add(candidate.Name)) cultures.Add(candidate);
        }
    }
}
