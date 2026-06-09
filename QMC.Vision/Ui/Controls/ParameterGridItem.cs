using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Vision.Core.Parameters;

namespace QMC.Vision.Ui.Controls
{
    // R1 — Handler(QMC.CDT_320.Ui.Controls.ParameterGridItem) public API 1:1 미러.
    // 향후 QMC.Common 공용화 시 드롭인 교체 대상. 순수 데이터/모델 (Designer 불요).
    public enum ParameterGridValueType
    {
        Double,
        Int,
        Bool,
        Text,
        Selection
    }

    public enum ParameterGridScope
    {
        Recipe,
        Setup,
        Config
    }

    public sealed class ParameterGridItem
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Unit { get; set; }
        public ParameterGridValueType ValueType { get; set; }
        public ParameterGridScope Scope { get; set; }
        public double DisplayScale { get; set; }
        public Func<object> Getter { get; set; }
        public Action<object> Setter { get; set; }
        public Func<object, bool> Validator { get; set; }
        public List<ParameterGridOption> Options { get; private set; }

        public ParameterGridItem()
        {
            DisplayScale = 1.0;
            Unit = string.Empty;
            Options = new List<ParameterGridOption>();
        }

        public static ParameterGridItem Micron(string displayName, ParameterGridScope scope, Func<double> getter, Action<double> setter)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Unit = "um",
                Scope = scope,
                ValueType = ParameterGridValueType.Double,
                DisplayScale = 1000.0,
                Getter = () => getter(),
                Setter = value => setter(Convert.ToDouble(value) / 1000.0)
            };
        }

        public static ParameterGridItem Double(string displayName, string unit, ParameterGridScope scope, Func<double> getter, Action<double> setter)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Unit = unit ?? string.Empty,
                Scope = scope,
                ValueType = ParameterGridValueType.Double,
                DisplayScale = 1.0,
                Getter = () => getter(),
                Setter = value => setter(Convert.ToDouble(value))
            };
        }

        public static ParameterGridItem Int(string displayName, string unit, ParameterGridScope scope, Func<int> getter, Action<int> setter)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Unit = unit ?? string.Empty,
                Scope = scope,
                ValueType = ParameterGridValueType.Int,
                DisplayScale = 1.0,
                Getter = () => getter(),
                Setter = value => setter(Convert.ToInt32(value))
            };
        }

        public static ParameterGridItem Bool(string displayName, ParameterGridScope scope, Func<bool> getter, Action<bool> setter)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Unit = string.Empty,
                Scope = scope,
                ValueType = ParameterGridValueType.Bool,
                DisplayScale = 1.0,
                Getter = () => getter(),
                Setter = value => setter(Convert.ToBoolean(value))
            };
        }

        public static ParameterGridItem Selection(string displayName, string unit, ParameterGridScope scope, Func<object> getter, Action<object> setter, IEnumerable<ParameterGridOption> options)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Unit = unit ?? string.Empty,
                Scope = scope,
                ValueType = ParameterGridValueType.Selection,
                DisplayScale = 1.0,
                Getter = getter,
                Setter = setter,
                Options = (options ?? Enumerable.Empty<ParameterGridOption>()).ToList()
            };
        }

        public static ParameterGridItem Selection<TEnum>(string displayName, string unit, ParameterGridScope scope, Func<TEnum> getter, Action<TEnum> setter)
            where TEnum : struct
        {
            Type enumType = typeof(TEnum);
            if (!enumType.IsEnum)
                throw new ArgumentException("TEnum must be an enum type.");

            var options = Enum.GetValues(enumType)
                .Cast<object>()
                .Select(value => new ParameterGridOption(value.ToString(), value))
                .ToList();

            return Selection(displayName, unit, scope, () => getter(), value => setter((TEnum)value), options);
        }

        // ── P4 — 디스크립터(SSOT) → UI 아이템 어댑터 ──
        /// <summary>ParameterDescriptor 를 ParameterGridItem 으로. SCOPE=Layer(하드코딩 제거),
        /// 편집은 store.SetValue 경유(계층·타깃 dirty 발생). 단위/스케일/Min·Max/Enum 옵션 디스크립터에서.</summary>
        public static ParameterGridItem FromDescriptor(ParameterDescriptor d, ParameterStore store)
        {
            if (d == null) return null;
            var it = new ParameterGridItem
            {
                Key = d.Target + "::" + d.Key,
                DisplayName = d.DisplayName,
                Unit = d.Unit ?? string.Empty,
                DisplayScale = d.DisplayScale <= 0 ? 1.0 : d.DisplayScale,
                Scope = MapScope(d.Layer),
                ValueType = MapType(d.Type),
                Getter = d.Getter,
                Setter = store != null ? (value => store.SetValue(d.Target, d.Key, value)) : d.Setter,
                Validator = d.Validator
            };
            if (d.Options != null)
                foreach (var o in d.Options)
                    it.Options.Add(new ParameterGridOption(o.Text, o.Value));
            return it;
        }

        private static ParameterGridScope MapScope(ParameterLayer layer)
        {
            switch (layer)
            {
                case ParameterLayer.Setup:  return ParameterGridScope.Setup;
                case ParameterLayer.Config: return ParameterGridScope.Config;
                default:                    return ParameterGridScope.Recipe;
            }
        }

        private static ParameterGridValueType MapType(ParameterType type)
        {
            switch (type)
            {
                case ParameterType.Int:  return ParameterGridValueType.Int;
                case ParameterType.Bool: return ParameterGridValueType.Bool;
                case ParameterType.Text: return ParameterGridValueType.Text;
                case ParameterType.Enum: return ParameterGridValueType.Selection;
                default:                 return ParameterGridValueType.Double;
            }
        }
    }

    public sealed class ParameterGridOption
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public ParameterGridOption()
        {
            Text = string.Empty;
        }

        public ParameterGridOption(string text, object value)
        {
            Text = text ?? string.Empty;
            Value = value;
        }
    }
}
