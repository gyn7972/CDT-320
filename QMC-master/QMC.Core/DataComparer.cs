using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using MechaSys.SoftBricks.Diagnostics;

namespace QMC
{
    public static class DataComparer
    {
        [Serializable]
        public enum DataTypes
        {
            Configuration,
            Recipe,
        }

        public static void DeepCompareLog(DataTypes dataType, string name, object oldObj, object newObj, string path = "")
        {
            if(oldObj == null && newObj == null)
                return;

            // null → 값 변경
            if(oldObj == null || newObj == null)
            {
                if(dataType == DataTypes.Configuration)
                    TraceLogger.ConfigurationLogging($"{name}, {path}: {oldObj} -> {newObj}");
                else
                    TraceLogger.RecipeLogging($"{name}, {path}: {oldObj} -> {newObj}");

                return;
            }

            var type = oldObj.GetType();

            // ❗ System.Drawing.Color 타입은 재귀 금지 (StackOverflow 방지)
            if(type == typeof(System.Drawing.Color))
            {
                if(!Equals(oldObj, newObj))
                {
                    if(dataType == DataTypes.Configuration)
                        TraceLogger.ConfigurationLogging($"{name}, {path}: {oldObj} -> {newObj}");
                    else
                        TraceLogger.RecipeLogging($"{name}, {path}: {oldObj} -> {newObj}");
                }
                return;
            }

            // 기본형, 문자열, enum → 즉시 비교
            if(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            {
                if(!Equals(oldObj, newObj))
                {
                    if(dataType == DataTypes.Configuration)
                        TraceLogger.ConfigurationLogging($"{name}, {path}: {oldObj} -> {newObj}");
                    else
                        TraceLogger.RecipeLogging($"{name}, {path}: {oldObj} -> {newObj}");
                }
                return;
            }

            // IEnumerable (List 같은 컬렉션)
            if(typeof(IEnumerable).IsAssignableFrom(type))
            {
                // string은 IEnumerable<char> 이므로 제외
                if(type == typeof(string))
                {
                    if(!Equals(oldObj, newObj))
                    {
                        if(dataType == DataTypes.Configuration)
                            TraceLogger.ConfigurationLogging($"{name}, {path}: {oldObj} -> {newObj}");
                        else
                            TraceLogger.RecipeLogging($"{name}, {path}: {oldObj} -> {newObj}");
                    }
                    return;
                }

                var oldEnum = ((IEnumerable)oldObj).Cast<object>().ToList();
                var newEnum = ((IEnumerable)newObj).Cast<object>().ToList();

                int max = Math.Max(oldEnum.Count, newEnum.Count);

                for(int i = 0; i < max; i++)
                {
                    string newPath = $"{path}[{i}]";

                    object oldVal = i < oldEnum.Count ? oldEnum[i] : null;
                    object newVal = i < newEnum.Count ? newEnum[i] : null;

                    DeepCompareLog(dataType, name, oldVal, newVal, newPath);
                }
                return;
            }

            // 클래스 타입 → Property 순회 후 재귀 비교
            foreach(var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if(!prop.CanRead) continue;
                if(prop.GetIndexParameters().Length > 0) continue; // indexer 막기

                object oldValue = prop.GetValue(oldObj);
                object newValue = prop.GetValue(newObj);

                string newPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";

                DeepCompareLog(dataType, name, oldValue, newValue, newPath);
            }
        }
    }
}
