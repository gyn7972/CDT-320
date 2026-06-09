using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.Vision.Core.Parameters
{
    /// <summary>
    /// SSOT 파라미터 디스크립터 — 파라미터 1개를 타입+계층 메타로 1회 정의(설계 §1-2).
    /// 값은 보유하지 않고 실 도메인 객체에 Getter/Setter 클로저로 바인딩(이중 진실원 금지).
    /// WinForms 비의존(Core). UI(ParameterGridItem)·스토어는 이 디스크립터에서 파생(P2~P4).
    /// </summary>
    public sealed class ParameterDescriptor
    {
        public string Target { get; set; }          // 정규 id (예 "WaferVision/ReticleFinder")
        public string Key { get; set; }             // 타깃 내 고유 (예 "SearchRoi.CenterX")
        public string DisplayName { get; set; }
        public string Unit { get; set; }
        public ParameterType Type { get; set; }
        public ParameterLayer Layer { get; set; }   // ← 선언 1회 (UI SCOPE·저장소 라우팅 근거)
        public object Default { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double DisplayScale { get; set; } = 1.0;
        public IReadOnlyList<ParameterOption> Options { get; set; }
        public Func<object> Getter { get; set; }
        public Action<object> Setter { get; set; }
        public Func<object, bool> Validator { get; set; }

        public ParameterDescriptor()
        {
            Unit = string.Empty;
            Options = new List<ParameterOption>();
        }

        // ── 팩토리 (호출부 간결화) ──
        public static ParameterDescriptor Double(string target, string key, string display, string unit,
            ParameterLayer layer, Func<double> getter, Action<double> setter,
            double? min = null, double? max = null, double scale = 1.0)
            => new ParameterDescriptor
            {
                Target = target, Key = key, DisplayName = display, Unit = unit ?? string.Empty,
                Type = ParameterType.Double, Layer = layer, DisplayScale = scale,
                Min = min, Max = max, Default = getter(),
                Getter = () => getter(),
                Setter = v => setter(Convert.ToDouble(v)),
                Validator = MakeRangeValidator(min, max)
            };

        public static ParameterDescriptor Int(string target, string key, string display, string unit,
            ParameterLayer layer, Func<int> getter, Action<int> setter, double? min = null, double? max = null)
            => new ParameterDescriptor
            {
                Target = target, Key = key, DisplayName = display, Unit = unit ?? string.Empty,
                Type = ParameterType.Int, Layer = layer, Min = min, Max = max, Default = getter(),
                Getter = () => getter(),
                Setter = v => setter(Convert.ToInt32(v)),
                Validator = MakeRangeValidator(min, max)
            };

        public static ParameterDescriptor Bool(string target, string key, string display,
            ParameterLayer layer, Func<bool> getter, Action<bool> setter)
            => new ParameterDescriptor
            {
                Target = target, Key = key, DisplayName = display, Unit = string.Empty,
                Type = ParameterType.Bool, Layer = layer, Default = getter(),
                Getter = () => getter(),
                Setter = v => setter(Convert.ToBoolean(v))
            };

        public static ParameterDescriptor Text(string target, string key, string display,
            ParameterLayer layer, Func<string> getter, Action<string> setter)
            => new ParameterDescriptor
            {
                Target = target, Key = key, DisplayName = display, Unit = string.Empty,
                Type = ParameterType.Text, Layer = layer, Default = getter(),
                Getter = () => getter(),
                Setter = v => setter(Convert.ToString(v))
            };

        public static ParameterDescriptor Enum<TEnum>(string target, string key, string display,
            ParameterLayer layer, Func<TEnum> getter, Action<TEnum> setter) where TEnum : struct
        {
            var options = System.Enum.GetValues(typeof(TEnum)).Cast<object>()
                .Select(v => new ParameterOption(v.ToString(), v)).ToList();
            return new ParameterDescriptor
            {
                Target = target, Key = key, DisplayName = display, Unit = string.Empty,
                Type = ParameterType.Enum, Layer = layer, Default = getter(),
                Options = options,
                Getter = () => getter(),
                Setter = v => setter((TEnum)System.Enum.Parse(typeof(TEnum), Convert.ToString(v)))
            };
        }

        private static Func<object, bool> MakeRangeValidator(double? min, double? max)
        {
            if (min == null && max == null) return null;
            return v =>
            {
                try
                {
                    double d = Convert.ToDouble(v);
                    if (min.HasValue && d < min.Value) return false;
                    if (max.HasValue && d > max.Value) return false;
                    return true;
                }
                catch { return false; }
            };
        }
    }
}
