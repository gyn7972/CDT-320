using System;
using System.IO;
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
    }
}
