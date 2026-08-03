using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace QMC.CDT320.Ajin
{
    // io_settings.json 직렬화 모델입니다. DI와 DO의 이름별 Simulation 설정만 저장합니다.
    [DataContract]
    public sealed class IoSettings
    {
        [DataMember] public Dictionary<string, IoPortSettings> DigitalInputs { get; set; } = new Dictionary<string, IoPortSettings>();
        [DataMember] public Dictionary<string, IoPortSettings> DigitalOutputs { get; set; } = new Dictionary<string, IoPortSettings>();
    }

    [DataContract]
    public sealed class IoPortSettings
    {
        [DataMember] public bool IsSimulationMode { get; set; } = true;
    }

    /// <summary>
    /// DI/DO별 Simulation 사용 여부를 저장하고 조회합니다.
    /// 물리 주소(Module/Bit/Nc)는 이 파일이 아니라 AjinConfig에서 관리합니다.
    /// </summary>
    public static class IoSettingsStore
    {
        public static string RootDir { get; } = @"D:\CDT-320";
        public static string Dir { get; } = Path.Combine(RootDir, "Config");
        public static string Path_ { get; } = Path.Combine(Dir, "io_settings.json");

        public static IoSettings Current { get; private set; } = new IoSettings();

        static IoSettingsStore()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
        }

        // 저장 파일 읽기 / 쓰기
        public static IoSettings Load()
        {
            try
            {
                if (!File.Exists(Path_))
                {
                    Current = Default();
                    Save();
                    return Current;
                }

                using (FileStream fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(IoSettings), CreateSettings());
                    Current = ser.ReadObject(fs) as IoSettings ?? new IoSettings();
                    Normalize(Current);
                }
            }
            catch
            {
                Current = Default();
            }
            finally
            {
            }

            return Current;
        }

        public static void Save()
        {
            try
            {
                if (Current == null)
                    Current = Default();

                Normalize(Current);
                Directory.CreateDirectory(Dir);
                using (FileStream fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(IoSettings), CreateSettings());
                    using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true, "  "))
                    {
                        ser.WriteObject(writer, Current);
                        writer.Flush();
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

        // 이름별 DI/DO Simulation 설정 조회
        public static bool InputSimulation(string name, bool defaultValue)
        {
            try
            {
                EnsureLoaded();
                IoPortSettings settings;
                if (Current.DigitalInputs.TryGetValue(name ?? string.Empty, out settings) && settings != null)
                    return settings.IsSimulationMode;
            }
            catch
            {
            }
            finally
            {
            }

            return defaultValue;
        }

        public static bool OutputSimulation(string name, bool defaultValue)
        {
            try
            {
                EnsureLoaded();
                IoPortSettings settings;
                if (Current.DigitalOutputs.TryGetValue(name ?? string.Empty, out settings) && settings != null)
                    return settings.IsSimulationMode;
            }
            catch
            {
            }
            finally
            {
            }

            return defaultValue;
        }

        // 이름별 DI/DO Simulation 설정 변경
        public static void SetInputSimulation(string name, bool value)
        {
            try
            {
                EnsureLoaded();
                if (string.IsNullOrWhiteSpace(name)) return;
                Current.DigitalInputs[name] = new IoPortSettings { IsSimulationMode = value };
            }
            finally
            {
            }
        }

        public static void SetOutputSimulation(string name, bool value)
        {
            try
            {
                EnsureLoaded();
                if (string.IsNullOrWhiteSpace(name)) return;
                Current.DigitalOutputs[name] = new IoPortSettings { IsSimulationMode = value };
            }
            finally
            {
            }
        }

        private static void EnsureLoaded()
        {
            try
            {
                if (Current == null)
                    Load();
            }
            finally
            {
            }
        }

        private static IoSettings Default()
        {
            IoSettings settings = new IoSettings();
            Normalize(settings);
            return settings;
        }

        // 카탈로그에 존재하지만 저장 파일에 없는 DI/DO를 현재 보드 준비 상태에 맞춰 보완합니다.
        private static void Normalize(IoSettings settings)
        {
            try
            {
                if (settings == null) return;
                if (settings.DigitalInputs == null) settings.DigitalInputs = new Dictionary<string, IoPortSettings>();
                if (settings.DigitalOutputs == null) settings.DigitalOutputs = new Dictionary<string, IoPortSettings>();
                foreach (DioDefault item in AjinIoCatalog.DigitalInputs)
                    if (!settings.DigitalInputs.ContainsKey(item.Name))
                        settings.DigitalInputs[item.Name] = new IoPortSettings { IsSimulationMode = !AjinFactory.IsRealBoardReady };

                foreach (DioDefault item in AjinIoCatalog.DigitalOutputs)
                    if (!settings.DigitalOutputs.ContainsKey(item.Name))
                        settings.DigitalOutputs[item.Name] = new IoPortSettings { IsSimulationMode = !AjinFactory.IsRealBoardReady };

            }
            finally
            {
            }
        }

        private static DataContractJsonSerializerSettings CreateSettings()
        {
            return new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true,
                EmitTypeInformation = EmitTypeInformation.Never
            };
        }
    }
}
