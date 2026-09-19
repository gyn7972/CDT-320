using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace QMC.Common.Localization
{
    /// <summary>
    /// 명시적인 번역 키와 i18n 태그가 있는 컨트롤만 갱신합니다.
    /// 임의의 입력값은 검색하지 않으며 모든 메서드는 해당 컨트롤의 UI 스레드에서 호출해야 합니다.
    /// </summary>
    public sealed class WinFormsLocalizationBinder
    {
        private readonly LocalizationService _service;
        private readonly Action<Control, string> _setText;
        private readonly ConditionalWeakTable<Control, Binding> _bindings = new ConditionalWeakTable<Control, Binding>();

        private sealed class Binding
        {
            public string Key;
            public string Literal;
        }

        /// <summary>
        /// 기본 표시 속성은 Control.Text입니다. 프로젝트 전용 컨트롤은 setText로 처리하며
        /// 서비스의 언어 변경 이벤트나 폼 수명 주기에 자동으로 구독하지 않습니다.
        /// </summary>
        public WinFormsLocalizationBinder(LocalizationService service, Action<Control, string> setText = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _setText = setText ?? ((control, text) => control.Text = text);
        }

        /// <summary>Tag를 보존하면서 번역 키를 등록하고 즉시 적용합니다. 컨트롤을 강하게 보관하지 않습니다.</summary>
        public void Bind(Control control, string key)
        {
            if (control == null || control.IsDisposed) return;
            Binding binding = _bindings.GetValue(control, item => new Binding());
            binding.Key = key ?? string.Empty;
            binding.Literal = null;
            SetDisplayText(control, _service.GetString(binding.Key));
        }

        /// <summary>번역하지 않을 고정 문구를 등록합니다. 기존 키 등록을 대체하며 Tag는 보존합니다.</summary>
        public void BindLiteral(Control control, string text)
        {
            if (control == null || control.IsDisposed) return;
            Binding binding = _bindings.GetValue(control, item => new Binding());
            binding.Key = null;
            binding.Literal = text ?? string.Empty;
            SetDisplayText(control, binding.Literal);
        }

        /// <summary>
        /// 현재 컨트롤과 자식의 i18n 태그를 우선 적용하고, 없으면 등록된 키를 적용합니다.
        /// 각 컨트롤의 ILocalizedView도 호출합니다. 값, 선택 및 Tag는 변경하지 않습니다.
        /// </summary>
        public void Apply(Control root)
        {
            if (root == null || root.IsDisposed) return;
            string key;
            Binding binding;
            if (TryGetTranslationKey(root.Tag as string, out key))
                SetDisplayText(root, _service.GetString(key));
            else if (_bindings.TryGetValue(root, out binding))
                SetDisplayText(root, binding.Key != null ? _service.GetString(binding.Key) : binding.Literal);

            if (root.IsDisposed) return;
            if (root is ILocalizedView view) view.ApplyLanguage();
            if (root.IsDisposed) return;

            var children = new Control[root.Controls.Count];
            root.Controls.CopyTo(children, 0);
            foreach (Control child in children) Apply(child);
        }

        private static bool TryGetTranslationKey(string tag, out string key)
        {
            key = null;
            if (string.IsNullOrEmpty(tag)) return false;
            foreach (string part in tag.Split(';'))
            {
                string value = part.Trim();
                if (!value.StartsWith("i18n:", StringComparison.Ordinal)) continue;
                key = value.Substring(5);
                return key.Length > 0;
            }
            return false;
        }

        private void SetDisplayText(Control control, string text)
        {
            if (control.IsDisposed) return;
            _setText(control, text);
            if (!control.IsDisposed) control.Invalidate();
        }
    }
}
