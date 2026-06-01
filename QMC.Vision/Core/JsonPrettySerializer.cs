using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace QMC.Vision.Core
{
    public static class JsonPrettySerializer
    {
        public static void WriteObject(Stream stream, Type type, object value)
        {
            try
            {
                if (stream == null) throw new ArgumentNullException(nameof(stream));
                if (type == null) throw new ArgumentNullException(nameof(type));

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
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
