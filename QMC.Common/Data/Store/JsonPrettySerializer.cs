using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace QMC.Common.Data.Store
{
    /// <summary>
    /// 공통 JSON pretty writer. JSON 저장 파일은 사람이 읽기 쉽게 UTF-8 들여쓰기로 저장한다.
    /// </summary>
    public static class JsonPrettySerializer
    {
        public static DataContractJsonSerializerSettings CreateSettings(bool simpleDictionaryFormat)
        {
            try
            {
                return new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = simpleDictionaryFormat,
                    EmitTypeInformation = EmitTypeInformation.Never
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

        public static void WriteObject(Stream stream, Type type, object value)
        {
            WriteObject(stream, type, value, null);
        }

        public static void WriteObject(Stream stream, Type type, object value, DataContractJsonSerializerSettings settings)
        {
            try
            {
                if (stream == null) throw new ArgumentNullException(nameof(stream));
                if (type == null) throw new ArgumentNullException(nameof(type));

                value = NormalizeDateTimesForJsonRoot(value);

                DataContractJsonSerializer serializer = settings == null
                    ? new DataContractJsonSerializer(type)
                    : new DataContractJsonSerializer(type, settings);

                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, "  "))
                {
                    serializer.WriteObject(writer, value);
                    writer.Flush();
                }
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static object NormalizeDateTimesForJsonRoot(object value)
        {
            DateTime safe;
            if (TryGetJsonSafeDateTime(value, out safe))
                return safe;

            NormalizeDateTimesForJson(value, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
            return value;
        }

        private static void NormalizeDateTimesForJson(object value, HashSet<object> visited, int depth)
        {
            try
            {
                if (value == null || depth > 64)
                    return;

                Type type = value.GetType();
                if (IsTerminalType(type))
                    return;

                if (!type.IsValueType && !visited.Add(value))
                    return;

                IDictionary dictionary = value as IDictionary;
                if (dictionary != null)
                {
                    var keys = new List<object>();
                    foreach (object key in dictionary.Keys)
                        keys.Add(key);

                    foreach (object key in keys)
                    {
                        object item = dictionary[key];
                        DateTime safe;
                        if (TryGetJsonSafeDateTime(item, out safe))
                            dictionary[key] = safe;
                        else
                            NormalizeDateTimesForJson(item, visited, depth + 1);
                    }
                    return;
                }

                IList list = value as IList;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        object item = list[i];
                        DateTime safe;
                        if (TryGetJsonSafeDateTime(item, out safe))
                            list[i] = safe;
                        else
                            NormalizeDateTimesForJson(item, visited, depth + 1);
                    }
                    return;
                }

                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.IsInitOnly || (!field.IsPublic && field.GetCustomAttribute<DataMemberAttribute>() == null))
                        continue;

                    object memberValue;
                    try { memberValue = field.GetValue(value); } catch { continue; }

                    DateTime safe;
                    if (TryGetJsonSafeDateTime(memberValue, out safe))
                    {
                        try { field.SetValue(value, safe); } catch { }
                    }
                    else
                    {
                        NormalizeDateTimesForJson(memberValue, visited, depth + 1);
                    }
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (property.GetIndexParameters().Length != 0)
                        continue;

                    MethodInfo getter = property.GetGetMethod(true);
                    MethodInfo setter = property.GetSetMethod(true);
                    bool serializableProperty =
                        property.GetCustomAttribute<DataMemberAttribute>() != null ||
                        (getter != null && getter.IsPublic && setter != null && setter.IsPublic);
                    if (!serializableProperty || getter == null)
                        continue;

                    object memberValue;
                    try { memberValue = property.GetValue(value, null); } catch { continue; }

                    DateTime safe;
                    if (TryGetJsonSafeDateTime(memberValue, out safe))
                    {
                        if (setter != null)
                        {
                            try { property.SetValue(value, safe, null); } catch { }
                        }
                    }
                    else
                    {
                        NormalizeDateTimesForJson(memberValue, visited, depth + 1);
                    }
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static bool TryGetJsonSafeDateTime(object value, out DateTime safe)
        {
            if (value is DateTime)
            {
                DateTime dateTime = (DateTime)value;
                if (RequiresUtcBoundaryDateTime(dateTime))
                {
                    safe = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                    return true;
                }
            }

            safe = default(DateTime);
            return false;
        }

        private static bool RequiresUtcBoundaryDateTime(DateTime value)
        {
            try
            {
                if (value.Kind == DateTimeKind.Utc)
                    return false;

                return value <= DateTime.MinValue.AddDays(1) ||
                       value >= DateTime.MaxValue.AddDays(-1);
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static bool IsTerminalType(Type type)
        {
            return type == null ||
                   type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid);
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
