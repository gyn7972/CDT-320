using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace QMC.CDT320.Interlocks
{
    [DataContract]
    public sealed class InterlockCheckMatrixFile
    {
        [DataMember(Order = 1)] public int Version { get; set; }
        [DataMember(Order = 2)] public string Source { get; set; }
        [DataMember(Order = 3)] public string GeneratedAt { get; set; }
        [DataMember(Order = 4)] public List<InterlockCheckPairFile> Checks { get; set; }
    }

    [DataContract]
    public sealed class InterlockCheckPairFile
    {
        [DataMember(Order = 1)] public string MovingName { get; set; }
        [DataMember(Order = 2)] public string CheckName { get; set; }
        [DataMember(Order = 3)] public string SourceCell { get; set; }
    }

    public static class InterlockCheckMatrixStore
    {
        public static string ConfigDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config"); }
        }

        public static string MatrixPath
        {
            get { return Path.Combine(ConfigDir, "interlock-check-matrix.json"); }
        }

        public static InterlockCheckMatrix LoadOrDefault()
        {
            try
            {
                EnsureDefaultFile();
                InterlockCheckMatrixFile file = Read(MatrixPath);
                if (file == null || file.Checks == null || file.Checks.Count == 0)
                    return InterlockCheckMatrix.Default;

                var pairs = new List<InterlockCheckPair>();
                foreach (InterlockCheckPairFile item in file.Checks)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.MovingName) || string.IsNullOrWhiteSpace(item.CheckName))
                        continue;

                    pairs.Add(new InterlockCheckPair(item.MovingName, item.CheckName, item.SourceCell));
                }

                return pairs.Count > 0 ? new InterlockCheckMatrix(pairs) : InterlockCheckMatrix.Default;
            }
            catch
            {
                return InterlockCheckMatrix.Default;
            }
        }

        public static void EnsureDefaultFile()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                if (File.Exists(MatrixPath))
                    return;

                Write(MatrixPath, CreateDefaultFile());
            }
            catch
            {
            }
        }

        public static InterlockCheckMatrixFile CreateDefaultFile()
        {
            var checks = new List<InterlockCheckPairFile>();
            foreach (InterlockCheckPair pair in InterlockCheckMatrix.Default.AllPairs)
            {
                checks.Add(new InterlockCheckPairFile
                {
                    MovingName = pair.MovingName,
                    CheckName = pair.CheckName,
                    SourceCell = pair.SourceCell
                });
            }

            return new InterlockCheckMatrixFile
            {
                Version = 1,
                Source = "CDT-320_Interlock_20260604_vertical_check.xlsx",
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Checks = checks
            };
        }

        private static InterlockCheckMatrixFile Read(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(InterlockCheckMatrixFile));
                return serializer.ReadObject(fs) as InterlockCheckMatrixFile;
            }
        }

        private static void Write(string path, InterlockCheckMatrixFile file)
        {
            using (var fs = File.Create(path))
            {
                var serializer = new DataContractJsonSerializer(
                    typeof(InterlockCheckMatrixFile),
                    new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true, "  "))
                {
                    serializer.WriteObject(writer, file);
                    writer.Flush();
                }
            }
        }
    }
}
