using System;
using System.Globalization;
using System.Resources;

namespace QMC.Common.Localization
{
    /// <summary>.resx 리소스를 다른 언어로 자동 대체하지 않고 조회합니다.</summary>
    public sealed class ResourceLocalizationSource : ILocalizationSource
    {
        private readonly ResourceManager _resourceManager;
        private readonly string _neutralLanguage;

        /// <summary>
        /// 호출자가 소유하는 ResourceManager를 사용합니다. neutralLanguage를 지정하면
        /// 해당 언어의 정확 조회는 중립 .resx에서 처리합니다. 다른 언어로의 대체는 수행하지 않습니다.
        /// </summary>
        public ResourceLocalizationSource(ResourceManager resourceManager, string neutralLanguage = null)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _neutralLanguage = string.IsNullOrEmpty(neutralLanguage)
                ? null : CultureInfo.GetCultureInfo(neutralLanguage).Name;
        }

        /// <inheritdoc />
        public bool TryGetString(string key, CultureInfo culture, out string value)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            value = null;
            if (string.IsNullOrEmpty(key)) return false;

            CultureInfo resourceCulture = string.Equals(culture.Name, _neutralLanguage, StringComparison.OrdinalIgnoreCase)
                ? CultureInfo.InvariantCulture : culture;
            ResourceSet resources = _resourceManager.GetResourceSet(resourceCulture, true, false);
            if (resources == null) return false;
            value = resources.GetString(key, false);
            return value != null;
        }
    }
}
