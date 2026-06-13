using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using QMC.Common.IO;

namespace QMC.CDT320.Ajin
{
    [DataContract]
    public sealed class CylinderSettings
    {
        [DataMember] public Dictionary<string, CylinderItemSettings> Cylinders { get; set; } = new Dictionary<string, CylinderItemSettings>();
    }

    [DataContract]
    public sealed class CylinderItemSettings
    {
        [DataMember] public bool IsSimulationMode { get; set; } = true;
        [DataMember] public bool IsSingleSolenoid { get; set; } = false;
        [DataMember] public bool UseFwdInput { get; set; } = true;
        [DataMember] public bool UseBwdInput { get; set; } = true;
        [DataMember] public string FwdLabel { get; set; } = "FWD";
        [DataMember] public string BwdLabel { get; set; } = "BWD";
        [DataMember] public int FwdTimeoutMs { get; set; } = 3000;
        [DataMember] public int BwdTimeoutMs { get; set; } = 3000;
    }

    public static class CylinderSettingsStore
    {
        public static string RootDir { get; } = @"D:\CDT-320";
        public static string Dir { get; } = Path.Combine(RootDir, "Config");
        public static string Path_ { get; } = Path.Combine(Dir, "cylinder_settings.json");

        public static CylinderSettings Current { get; private set; } = new CylinderSettings();

        static CylinderSettingsStore()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
        }

        public static CylinderSettings Load()
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
                    var ser = new DataContractJsonSerializer(typeof(CylinderSettings), CreateSettings());
                    Current = ser.ReadObject(fs) as CylinderSettings ?? new CylinderSettings();
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
                    var ser = new DataContractJsonSerializer(typeof(CylinderSettings), CreateSettings());
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

        public static CylinderItemSettings Get(string name)
        {
            try
            {
                EnsureLoaded();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    CylinderItemSettings settings;
                    if (Current.Cylinders.TryGetValue(name, out settings) && settings != null)
                        return settings;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return CreateDefault(null);
        }

        public static bool Simulation(string name, bool defaultValue)
        {
            try
            {
                CylinderItemSettings settings = Get(name);
                return settings == null ? defaultValue : settings.IsSimulationMode;
            }
            catch
            {
                return defaultValue;
            }
            finally
            {
            }
        }

        public static void SetSimulation(string name, bool value)
        {
            try
            {
                EnsureLoaded();
                if (string.IsNullOrWhiteSpace(name)) return;
                CylinderItemSettings settings = Get(name);
                settings.IsSimulationMode = value;
                Current.Cylinders[name] = settings;
            }
            finally
            {
            }
        }

        public static void Apply(BaseCylinder cylinder)
        {
            try
            {
                if (cylinder == null) return;
                CylinderItemSettings settings = Get(cylinder.Name);
                cylinder.Setup.IsSingleSolenoid = settings.IsSingleSolenoid;
                cylinder.Setup.UseFwdSensor = settings.UseFwdInput && HasValidInput(cylinder.InFwd);
                cylinder.Setup.UseBwdSensor = settings.UseBwdInput && HasValidInput(cylinder.InBwd);
                cylinder.Recipe.FwdTimeoutMs = settings.FwdTimeoutMs;
                cylinder.Recipe.BwdTimeoutMs = settings.BwdTimeoutMs;

                bool dryRunMode = IsApplicationDryRunMode();
                bool simulationMode = settings.IsSimulationMode;
                if (IsApplicationSimulationMode())
                    simulationMode = true;

                if (dryRunMode)
                    AjinFactory.ApplyCylinderDryRun(cylinder, true);
                else
                    AjinFactory.ApplyCylinderSimulation(cylinder, simulationMode);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static bool HasValidInput(BaseDigitalInput input)
        {
            try
            {
                if (input == null || input.Setup == null)
                    return false;

                return input.Setup.ModuleNo >= 0 && input.Setup.BitNo >= 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsApplicationSimulationMode()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                    return false;

                return settings.SimulationMode ||
                       !settings.UseAjin ||
                       !AjinFactory.IsRealBoardReady;
            }
            catch
            {
                return !AjinFactory.IsRealBoardReady;
            }
            finally
            {
            }
        }

        private static bool IsApplicationDryRunMode()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                    return false;

                return settings.DryRunMode &&
                       !settings.SimulationMode &&
                       settings.UseAjin &&
                       AjinFactory.IsRealBoardReady;
            }
            catch
            {
                return false;
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

        private static CylinderSettings Default()
        {
            CylinderSettings settings = new CylinderSettings();
            Normalize(settings);
            return settings;
        }

        private static void Normalize(CylinderSettings settings)
        {
            try
            {
                if (settings == null) return;
                if (settings.Cylinders == null)
                    settings.Cylinders = new Dictionary<string, CylinderItemSettings>();

                foreach (CylinderDefault item in AjinIoCatalog.Cylinders)
                {
                    if (!settings.Cylinders.ContainsKey(item.Name) || settings.Cylinders[item.Name] == null)
                    {
                        settings.Cylinders[item.Name] = CreateDefault(item);
                    }
                    else if (!settings.Cylinders[item.Name].UseFwdInput && !settings.Cylinders[item.Name].UseBwdInput)
                    {
                        CylMap map;
                        if (AjinConfigStore.Current.Cylinders.TryGetValue(item.Name, out map) && map != null)
                        {
                            settings.Cylinders[item.Name].UseFwdInput = map.InFwd != null;
                            settings.Cylinders[item.Name].UseBwdInput = map.InBwd != null;
                        }
                        else
                        {
                            settings.Cylinders[item.Name].UseFwdInput = item.InFwd != null;
                            settings.Cylinders[item.Name].UseBwdInput = item.InBwd != null;
                        }
                    }

                    FillDefaultLabels(item, settings.Cylinders[item.Name]);
                }
            }
            finally
            {
            }
        }

        private static CylinderItemSettings CreateDefault(CylinderDefault item)
        {
            return new CylinderItemSettings
            {
                IsSimulationMode = !AjinFactory.IsRealBoardReady,
                IsSingleSolenoid = item != null && item.SingleSolenoid,
                UseFwdInput = item == null || item.InFwd != null,
                UseBwdInput = item == null || item.InBwd != null,
                FwdLabel = DefaultFwdLabel(item),
                BwdLabel = DefaultBwdLabel(item),
                FwdTimeoutMs = 3000,
                BwdTimeoutMs = 3000
            };
        }

        private static void FillDefaultLabels(CylinderDefault item, CylinderItemSettings settings)
        {
            try
            {
                if (settings == null) return;
                string defaultFwd = DefaultFwdLabel(item);
                string defaultBwd = DefaultBwdLabel(item);

                if (string.IsNullOrWhiteSpace(settings.FwdLabel)
                    || (string.Equals(settings.FwdLabel, "FWD", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(defaultFwd, "FWD", StringComparison.OrdinalIgnoreCase)))
                    settings.FwdLabel = defaultFwd;

                if (string.IsNullOrWhiteSpace(settings.BwdLabel)
                    || (string.Equals(settings.BwdLabel, "BWD", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(defaultBwd, "BWD", StringComparison.OrdinalIgnoreCase)))
                    settings.BwdLabel = defaultBwd;
            }
            finally
            {
            }
        }

        private static string DefaultFwdLabel(CylinderDefault item)
        {
            try
            {
                string text = ((item != null ? item.Name : string.Empty) + " " + OutputName(item != null ? item.OutFwd : null)).ToUpperInvariant();
                if (text.Contains("LIFT") || text.Contains("UP")) return "UP";
                if (text.Contains("CLAMP")) return "CLAMP";
                if (text.Contains("FRONT") || text.Contains("FW")) return "FWD";
            }
            finally
            {
            }

            return "FWD";
        }

        private static string DefaultBwdLabel(CylinderDefault item)
        {
            try
            {
                string text = ((item != null ? item.Name : string.Empty) + " " + OutputName(item != null ? item.OutBwd : null)).ToUpperInvariant();
                if (text.Contains("LIFT") || text.Contains("DOWN")) return "DOWN";
                if (text.Contains("UNCLAMP")) return "UNCLAMP";
                if (text.Contains("CLAMP")) return "UNCLAMP";
                if (text.Contains("REAR") || text.Contains("BW")) return "BWD";
            }
            finally
            {
            }

            return "BWD";
        }

        private static string OutputName(DioDefault item)
        {
            try
            {
                if (item == null) return string.Empty;
                DioDefault found = AjinIoCatalog.FindOutput(item.Module, item.Bit);
                return found != null ? found.Name : string.Empty;
            }
            catch
            {
                return string.Empty;
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
