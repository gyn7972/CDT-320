using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT_320.Ui.Controls
{
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

        /// <summary>접이식 그룹 헤더 행 여부.</summary>
        public bool IsGroupHeader { get; set; }
        /// <summary>이 항목이 속한 그룹 키. 헤더와 멤버가 같은 키를 공유한다. 비어 있으면 그룹 없음(항상 표시).</summary>
        public string GroupKey { get; set; }

        /// <summary>접이식 그룹 헤더 항목을 만든다.</summary>
        public static ParameterGridItem Header(string displayName, string groupKey)
        {
            return new ParameterGridItem
            {
                Key = groupKey,
                DisplayName = displayName,
                IsGroupHeader = true,
                GroupKey = groupKey,
                ValueType = ParameterGridValueType.Text,
                Scope = ParameterGridScope.Recipe
            };
        }

        public ParameterGridItem()
        {
            try
            {
                DisplayScale = 1.0;
                Unit = string.Empty;
                Options = new List<ParameterGridOption>();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static ParameterGridItem Micron(string displayName, ParameterGridScope scope, Func<double> getter, Action<double> setter)
        {
            try
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
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static ParameterGridItem Double(string displayName, string unit, ParameterGridScope scope, Func<double> getter, Action<double> setter)
        {
            try
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
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static ParameterGridItem Int(string displayName, string unit, ParameterGridScope scope, Func<int> getter, Action<int> setter)
        {
            try
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
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static ParameterGridItem Bool(string displayName, ParameterGridScope scope, Func<bool> getter, Action<bool> setter)
        {
            try
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
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static ParameterGridItem Selection(string displayName, string unit, ParameterGridScope scope, Func<object> getter, Action<object> setter, IEnumerable<ParameterGridOption> options)
        {
            try
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
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static ParameterGridItem Selection<TEnum>(string displayName, string unit, ParameterGridScope scope, Func<TEnum> getter, Action<TEnum> setter)
            where TEnum : struct
        {
            try
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
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }

    public sealed class ParameterGridOption
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public ParameterGridOption()
        {
            try
            {
                Text = string.Empty;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public ParameterGridOption(string text, object value)
        {
            try
            {
                Text = text ?? string.Empty;
                Value = value;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
