using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Windows.Forms;
using QMC.Common.Localization;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Localization
{
    /// <summary>기존 화면을 공통 표시 갱신 계약에 연결하는 호환 인터페이스입니다.</summary>
    public interface ILocalizedView : QMC.Common.Localization.ILocalizedView
    {
    }

    /// <summary>
    /// CDT-320 화면의 언어 처리 진입점입니다. 번역 조회와 컨트롤 갱신은 QMC.Common에 위임하고,
    /// 프로젝트 리소스 구성과 전용 컨트롤 처리만 이 클래스에 둡니다.
    /// 문구는 Resources의 resx 파일에서 관리하며, 새 화면은 T 또는 BindKey로 고정 키를 사용합니다.
    /// </summary>
    public static class Lang
    {
        public const string Ko = "ko";
        public const string En = "en";
        public const string Zh = "zh-CN";
        public const string Ja = "ja";

        /// <summary>기존 언어 선택 화면과의 호환을 위한 언어 목록입니다.</summary>
        public static readonly string[] Supported = new[] { Ko, En, Zh, Ja };

        private static readonly LocalizationService _localization = CreateLocalization();
        private static readonly WinFormsLocalizationBinder _bindings
            = new WinFormsLocalizationBinder(_localization, SetDisplayText);
        private static readonly Dictionary<string, string> _displayAliases = CreateDisplayAliases();

        public static string Current => _localization.Current;

        public static event Action LanguageChanged
        {
            add { _localization.LanguageChanged += value; }
            remove { _localization.LanguageChanged -= value; }
        }

        public static void SetLanguage(string lang)
        {
            if (HasLanguage(lang)) _localization.SetLanguage(lang);
        }

        public static bool HasLanguage(string lang)
        {
            // 기존 설정의 언어 코드 대소문자 판정도 그대로 유지합니다.
            foreach (string supported in _localization.Supported)
                if (string.Equals(supported, lang, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>현재 언어, 영어, 한국어 순으로 조회하며 미등록 키는 그대로 반환합니다.</summary>
        public static string T(string key)
        {
            return _localization.GetString(key);
        }

        /// <summary>현재 언어와 무관하게 영어만 조회합니다. 미등록 키는 그대로 반환합니다.</summary>
        public static string TEn(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            return _localization.TryGetString(key, En, out var text) ? text : key;
        }

        /// <summary>
        /// 기존 표시 원문을 고정 리소스 키로 연결하는 호환 경로입니다.
        /// 명령, 설정값, enum, 모델 키에는 적용하지 않습니다. 새 문구는 T의 키 기반 조회를 사용합니다.
        /// </summary>
        public static string Display(string originalText)
        {
            if (string.IsNullOrEmpty(originalText)) return originalText ?? string.Empty;
            return _displayAliases.TryGetValue(originalText, out var key)
                ? _localization.GetString(key) : originalText;
        }

        /// <summary>기존 화면의 원문 바인딩을 유지합니다. 컨트롤 Tag와 내부 동작 값은 보존합니다.</summary>
        public static void Bind(Control control, string originalText)
        {
            if (control == null || control.IsDisposed) return;
            if (!string.IsNullOrEmpty(originalText) && _displayAliases.TryGetValue(originalText, out var key))
                _bindings.Bind(control, key);
            else _bindings.BindLiteral(control, originalText);
        }

        /// <summary>표시 컨트롤에 고정 번역 키를 연결합니다. 새 화면에서는 이 API를 사용합니다.</summary>
        public static void BindKey(Control control, string key)
        {
            _bindings.Bind(control, key);
        }

        /// <summary>명시적인 표시 바인딩과 i18n 태그만 갱신하며 입력값과 선택값을 변경하지 않습니다.</summary>
        public static void Apply(Control root)
        {
            _bindings.Apply(root);
        }

        private static LocalizationService CreateLocalization()
        {
            var sources = new ILocalizationSource[]
            {
                new ResourceLocalizationSource(new ResourceManager(
                    "QMC.CDT_320.Ui.Localization.Resources.Strings", typeof(Lang).Assembly), En),
                new ResourceLocalizationSource(new ResourceManager(
                    "QMC.CDT_320.Ui.Localization.Resources.DisplayStrings", typeof(Lang).Assembly), En),
                new ResourceLocalizationSource(new ResourceManager(
                    "QMC.Common.Localization.Resources.CommonStrings", typeof(LocalizationService).Assembly), En)
            };
            return new LocalizationService(sources, Supported, Ko, new[] { En, Ko });
        }

        private static Dictionary<string, string> CreateDisplayAliases()
        {
            // resgen은 이름의 대소문자 차이를 중복으로 처리하므로 리소스에는 고정 키 → 원문으로 보관합니다.
            // 여기서 원문 → 키로 뒤집어 기존 Display의 대소문자 및 공백 구분을 보존합니다.
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
            var manager = new ResourceManager(
                "QMC.CDT_320.Ui.Localization.Resources.DisplayAliases", typeof(Lang).Assembly);
            using (ResourceSet resources = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false))
            {
                if (resources == null) throw new MissingManifestResourceException("표시 문구 호환 리소스가 없습니다.");
                foreach (DictionaryEntry entry in resources)
                    aliases.Add((string)entry.Value, (string)entry.Key);
            }
            return aliases;
        }

        private static void SetDisplayText(Control control, string text)
        {
            if (control is BottomMenuButton button) button.Label = text;
            else control.Text = text;
        }
    }
}
