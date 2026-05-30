using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.CDT320.Ajin
{
    [DataContract]
    public class AxisMap
    {
        [DataMember] public int Axis { get; set; }
        [DataMember] public int BoardNo { get; set; }
        [DataMember] public int ChannelNo { get; set; }
    }

    [DataContract]
    public class DioMap
    {
        [DataMember] public int No { get; set; }
        [DataMember] public string Address { get; set; }
        [DataMember] public int Module { get; set; }
        [DataMember] public int Bit { get; set; }
        [DataMember] public bool Nc { get; set; }
    }

    [DataContract]
    public class CylMap
    {
        [DataMember] public DioMap OutFwd { get; set; }
        [DataMember] public DioMap OutBwd { get; set; }
        [DataMember] public DioMap InFwd { get; set; }
        [DataMember] public DioMap InBwd { get; set; }
        [DataMember] public bool SingleSolenoid { get; set; }
    }

    [DataContract]
    public class AjinConfig
    {
        [DataMember] public Dictionary<string, AxisMap> Axes { get; set; } = new Dictionary<string, AxisMap>();
        [DataMember] public Dictionary<string, DioMap> DigitalInputs { get; set; } = new Dictionary<string, DioMap>();
        [DataMember] public Dictionary<string, DioMap> DigitalOutputs { get; set; } = new Dictionary<string, DioMap>();
        [DataMember] public Dictionary<string, CylMap> Cylinders { get; set; } = new Dictionary<string, CylMap>();
    }

    public sealed class AxisDefault
    {
        public int Axis { get; set; }
        public string AxisName { get; set; }
        public string Module { get; set; }
        public int BoardNo { get; set; }
        public int ChannelNo { get; set; }
        public double Stroke { get; set; }
        public bool Brake { get; set; }
        public string Unit { get; set; }
        public double DefaultVel { get; set; }
        public string HomeDir { get; set; }
        public string[] LegacyKeys { get; set; }
    }

    public static class AjinAxisDefaults
    {
        public static readonly AxisDefault[] All =
        {
            ADD( 0, "WaferLifterZ",      "IndexCassette",    0, 0,  200, true,  "mm",  100, "NEG","ElevatorZ"),
            ADD( 1, "WaferFeederY",      "WaferFeeder",    0, 0,  300, false, "mm",  100, "NEG", "FeederY_Input", "FeederY"),
            ADD( 2, "WaferStageY",       "WaferStage",     0, 0,  400, false, "mm",  100, "NEG", "StageY"),
            ADD( 3, "WaferStageT",       "WaferStage",     0, 0,  360, false, "deg",  30, "POS", "StageT"),
            ADD( 4, "WaferExpandingZ",   "WaferStage",     0, 0,  100, false, "mm",  100, "NEG", "ExpanderZ"),
            ADD( 5, "WaferVisionX",      "WaferStage",     0, 0,  300, false, "mm",  100, "NEG", "CameraX"),
            ADD( 6, "NeedleX",           "WaferStage",     0, 0,  200, false, "mm",  100, "NEG", "NeedleBlockX"),
            ADD( 7, "NeedleZ",           "WaferStage",     0, 0,  100, true,  "mm",  100, "NEG"),
            ADD( 8, "EjectPinZ",         "WaferStage",     0, 0,   50, false, "mm",   50, "NEG"),
            ADD( 9, "FrontPickerX",      "FrontPicker",    0, 0, 1500, false, "mm",  800, "NEG", "LeftArm_ArmX"),
            ADD(10, "FrontPickerY",      "FrontPicker",    0, 0,  750, false, "mm",  100, "NEG", "LeftArm_ArmY"),
            ADD(11, "FrontPickerT0",     "FrontPicker",    0, 0,  360, false, "deg", 100, "NEG", "LeftArm_Picker1_T"),
            ADD(12, "FrontPickerZ0",     "FrontPicker",    0, 0,   50, false, "mm",  200, "NEG", "LeftArm_Picker1_Z"),
            ADD(13, "FrontPickerT1",     "FrontPicker",    0, 0,  360, false, "deg", 100, "NEG", "LeftArm_Picker2_T"),
            ADD(14, "FrontPickerZ1",     "FrontPicker",    0, 0,   50, false, "mm",  200, "NEG", "LeftArm_Picker2_Z"),
            ADD(15, "FrontPickerT2",     "FrontPicker",    0, 0,  360, false, "deg", 100, "NEG", "LeftArm_Picker3_T"),
            ADD(16, "FrontPickerZ2",     "FrontPicker",    0, 0,   50, false, "mm",  200, "NEG", "LeftArm_Picker3_Z"),
            ADD(17, "FrontPickerT3",     "FrontPicker",    0, 0,  360, false, "deg", 100, "NEG", "LeftArm_Picker4_T"),
            ADD(18, "FrontPickerZ3",     "FrontPicker",    0, 0,   50, false, "mm",  200, "NEG", "LeftArm_Picker4_Z"),
            ADD(19, "FrontSideVisionY0", "Vision",    0, 0,  200, false, "mm",  100, "NEG", "LeftArm_SideVisionY"),
            ADD(20, "RearSideVisionY0",  "Vision",     0, 0,  200, false, "mm",  100, "NEG", "RightArm_SideVisionY"),
            ADD(21, "RearPickerX",       "RearPicker",     0, 0, 1500, false, "mm",  800, "NEG", "RightArm_ArmX"),
            ADD(22, "RearPickerY",       "RearPicker",     0, 0,  750, false, "mm",  100, "NEG", "RightArm_ArmY"),
            ADD(23, "RearPickerT0",      "RearPicker",     0, 0,  360, false, "deg", 100, "NEG", "RightArm_Picker1_T"),
            ADD(24, "RearPickerZ0",      "RearPicker",     0, 0,   50, false, "mm",  200, "NEG", "RightArm_Picker1_Z"),
            ADD(25, "RearPickerT1",      "RearPicker",     0, 0,  360, false, "deg", 100, "NEG", "RightArm_Picker2_T"),
            ADD(26, "RearPickerZ1",      "RearPicker",     0, 0,   50, false, "mm",  200, "NEG", "RightArm_Picker2_Z"),
            ADD(27, "RearPickerT2",      "RearPicker",     0, 0,  360, false, "deg", 100, "NEG", "RightArm_Picker3_T"),
            ADD(28, "RearPickerZ2",      "RearPicker",     0, 0,   50, false, "mm",  200, "NEG", "RightArm_Picker3_Z"),
            ADD(29, "RearPickerT3",      "RearPicker",     0, 0,  360, false, "deg", 100, "NEG", "RightArm_Picker4_T"),
            ADD(30, "RearPickerZ3",      "RearPicker",     0, 0,   50, false, "mm",  200, "NEG", "RightArm_Picker4_Z"),
            ADD(31, "BinGoodY",          "BinStage",    0, 0,  500, false, "mm",  100, "NEG", "GoodStage_StageY"),
            ADD(32, "BinGoodZ",          "BinStage",    0, 0,  100, false, "mm",  100, "NEG", "GoodStage_StageZ"),
            ADD(33, "BinNgY",            "BinStage",    0, 0,  500, false, "mm",  100, "NEG", "NgStage_StageY"),
            ADD(34, "BinVisionX",        "BinStage",    0, 0,  300, false, "mm",  100, "NEG", "BinCameraX", "OutputStage_BinCameraX"),
            ADD(35, "BinFeederY",        "BinFeeder", 0, 0,  300, false, "mm",  100, "NEG", "FeederY_Output", "OutputUnloader_FeederY"),
            ADD(36, "BinLifterZ",        "BinCassette", 0, 0,  200, true,  "mm",  100, "NEG", "ElevatorZ_Output", "OutputUnloader_ElevatorZ")
        };

        public static string ResolveName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            for (int i = 0; i < All.Length; i++)
            {
                AxisDefault axis = All[i];
                if (string.Equals(axis.AxisName, name, StringComparison.OrdinalIgnoreCase))
                    return axis.AxisName;

                string[] keys = axis.LegacyKeys;
                if (keys == null) continue;
                for (int k = 0; k < keys.Length; k++)
                    if (string.Equals(keys[k], name, StringComparison.OrdinalIgnoreCase))
                        return axis.AxisName;
            }
            return name;
        }

        private static AxisDefault ADD(int axis, string axisName, string module, int boardNo, int channelNo,
                                     double stroke, bool brake, string unit, double defaultVel,
                                     string homeDir, params string[] legacyKeys)
        {
            return new AxisDefault
            {
                Axis = axis,
                AxisName = axisName,
                Module = module,
                BoardNo = boardNo,
                ChannelNo = channelNo,
                Stroke = stroke,
                Brake = brake,
                Unit = unit,
                DefaultVel = defaultVel,
                HomeDir = homeDir,
                LegacyKeys = legacyKeys
            };
        }
    }

    public static class AjinConfigStore
    {
        public static string Dir { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ { get; } = System.IO.Path.Combine(Dir, "ajin-map.json");

        public static AjinConfig Current { get; private set; } = new AjinConfig();

        static AjinConfigStore() { Directory.CreateDirectory(Dir); }

        public static AjinConfig Load()
        {
            if (!File.Exists(Path_))
            {
                Current = Default();
                Save();
                return Current;
            }

            try
            {
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AjinConfig));
                    Current = (AjinConfig)ser.ReadObject(fs);
                    Normalize(Current);
                }
            }
            catch
            {
                Current = Default();
            }

            EnsureDefaultAxes(Current);
            AjinIoCatalog.ApplyDefaults(Current, false);
            AjinIoCatalog.ApplyRequiredCorrections(Current);
            RemoveUnregisteredIo(Current);
            Save();
            return Current;
        }

        public static void Save()
        {
            try
            {
                Normalize(Current);
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AjinConfig));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }

        private static AjinConfig Default()
        {
            var c = new AjinConfig();
            EnsureDefaultAxes(c);
            AjinIoCatalog.ApplyDefaults(c, true);
            return c;
        }

        private static void Normalize(AjinConfig c)
        {
            if (c.Axes == null) c.Axes = new Dictionary<string, AxisMap>();
            if (c.DigitalInputs == null) c.DigitalInputs = new Dictionary<string, DioMap>();
            if (c.DigitalOutputs == null) c.DigitalOutputs = new Dictionary<string, DioMap>();
            if (c.Cylinders == null) c.Cylinders = new Dictionary<string, CylMap>();
        }

        private static void EnsureDefaultAxes(AjinConfig c)
        {
            if (c == null) return;
            Normalize(c);

            foreach (AxisDefault axis in AjinAxisDefaults.All)
            {
                AxisMap source = null;
                if (!c.Axes.TryGetValue(axis.AxisName, out source) && axis.LegacyKeys != null)
                {
                    for (int i = 0; i < axis.LegacyKeys.Length; i++)
                    {
                        if (c.Axes.TryGetValue(axis.LegacyKeys[i], out source))
                            break;
                    }
                }

                c.Axes[axis.AxisName] = new AxisMap
                {
                    Axis = axis.Axis,
                    BoardNo = source != null ? source.BoardNo : axis.BoardNo,
                    ChannelNo = source != null ? source.ChannelNo : axis.ChannelNo
                };
            }
        }

        private static void RemoveUnregisteredIo(AjinConfig c)
        {
            if (c == null) return;
            Normalize(c);

            Prune(c.DigitalInputs, name => AjinIoCatalog.FindInput(name) != null);
            Prune(c.DigitalOutputs, name => AjinIoCatalog.FindOutput(name) != null);
            Prune(c.Cylinders, name => AjinIoCatalog.FindCylinder(name) != null);
        }

        private static void Prune<T>(Dictionary<string, T> target, Predicate<string> keep)
        {
            if (target == null || keep == null) return;
            var remove = new List<string>();
            foreach (var key in target.Keys)
            {
                if (!keep(key)) remove.Add(key);
            }
            for (int i = 0; i < remove.Count; i++)
                target.Remove(remove[i]);
        }
    }
}
