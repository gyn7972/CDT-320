using System;
using System.Collections.Generic;
using System.Linq;

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
        Selection,
        Info,        // 읽기전용 표시(값만, 편집 불가)
        Action,      // 버튼 셀(클릭 시 OnAction 실행)
        FolderPath,  // 경로 텍스트 + 클릭 시 폴더 선택 다이얼로그
        Slider       // 정수 슬라이더(클릭 시 트랙바 다이얼로그). SliderMin/SliderMax 범위.
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

        /// <summary>Action 타입 — 버튼 클릭 시 실행할 동작.</summary>
        public System.Action OnAction { get; set; }
        /// <summary>Action 타입 — 버튼에 표시할 문구.</summary>
        public string ActionText { get; set; }

        /// <summary>Slider 타입 — 최소/최대 범위(정수).</summary>
        public int SliderMin { get; set; }
        public int SliderMax { get; set; } = 100;

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

        public static ParameterGridItem Text(string displayName, ParameterGridScope scope, Func<string> getter, Action<string> setter)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Unit = string.Empty,
                Scope = scope,
                ValueType = ParameterGridValueType.Text,
                DisplayScale = 1.0,
                Getter = () => getter() ?? string.Empty,
                Setter = value => setter(Convert.ToString(value))
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

        /// <summary>읽기전용 표시 행(백엔드 버전·진단 결과 등). 편집 불가.</summary>
        public static ParameterGridItem Info(string displayName, ParameterGridScope scope, Func<string> getter)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Scope = scope,
                ValueType = ParameterGridValueType.Info,
                Getter = () => getter() ?? string.Empty,
                Setter = null
            };
        }

        /// <summary>버튼 행(클릭 시 동작 실행). 값 셀이 버튼으로 렌더된다.</summary>
        public static ParameterGridItem Action(string displayName, string actionText, ParameterGridScope scope, System.Action onAction)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Scope = scope,
                ValueType = ParameterGridValueType.Action,
                ActionText = actionText ?? string.Empty,
                OnAction = onAction,
                Getter = () => actionText ?? string.Empty,
                Setter = null
            };
        }

        /// <summary>경로 행(텍스트 표시 + 클릭 시 폴더 선택 다이얼로그).</summary>
        public static ParameterGridItem FolderPath(string displayName, ParameterGridScope scope, Func<string> getter, Action<string> setter)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Scope = scope,
                ValueType = ParameterGridValueType.FolderPath,
                Getter = () => getter() ?? string.Empty,
                Setter = value => setter(Convert.ToString(value))
            };
        }

        /// <summary>정수 슬라이더 행(클릭 시 트랙바 다이얼로그로 min~max 조절). 조명 채널 레벨 등.</summary>
        public static ParameterGridItem Slider(string displayName, string unit, ParameterGridScope scope, int min, int max, Func<int> getter, Action<int> setter)
        {
            return new ParameterGridItem
            {
                Key = displayName,
                DisplayName = displayName,
                Unit = unit ?? string.Empty,
                Scope = scope,
                ValueType = ParameterGridValueType.Slider,
                SliderMin = min,
                SliderMax = max,
                Getter = () => getter(),
                Setter = value => setter(Convert.ToInt32(value))
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
