using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Claims;

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
            ADD( 0, "WaferLifterZ",      "WaferCassette",    0, 0,  200, true,  "mm",  100, "NEG", "ElevatorZ_Input", "ElevatorZ"),
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

            void DO(string n, int mod, int bit) => c.DigitalOutputs[n] = new DioMap { Module = mod, Bit = bit };
            DO("StartLamp", 3, 0);
            DO("StopLamp", 3, 1);
            DO("ResetLamp", 3, 2);
            DO("TlRed", 3, 3);
            DO("TlYellow", 3, 4);
            DO("TlGreen", 3, 5);
            DO("Buzzer", 3, 6);
            DO("IonizerOn", 3, 15);
            DO("WaferFeederUp", 3, 16);
            DO("WaferFeederDown", 3, 17);
            DO("WaferFeederClamp", 3, 18);
            DO("WaferFeederUnclamp", 3, 19);
            DO("ReticleUp", 3, 20);
            DO("ReticleDown", 3, 21);
            DO("ReticleFrontSideFw", 3, 22);
            DO("ReticleFrontSideBw", 3, 23);
            DO("ReticleRearSideFw", 3, 24);
            DO("ReticleRearSideBw", 3, 25);
            DO("NgBinGuideUp", 3, 26);
            DO("NgBinGuideDown", 3, 27);
            DO("NgBinClampUp", 3, 28);
            DO("NgBinClampDown", 3, 29);
            DO("NgBinClamp", 3, 30);
            DO("NgBinUnclamp", 3, 31);

            DO("GoodBinGuideUp", 4, 0);
            DO("GoodBinGuideDown", 4, 1);

            DO("GoodBinClampUp", 4, 2);
            DO("GoodBinClampDown", 4, 3);
            DO("GoodBinClamp", 4, 4);
            DO("GoodBinUnclamp", 4, 5);
            DO("BinFeederUp", 4, 6);
            DO("BinFeederDown", 4, 7);
            DO("BinFeederClamp", 4, 8);
            DO("BinFeederUnclamp", 4, 9);
            DO("NgBinCassetteLock", 4, 10);
            DO("NgBinCassetteUnlock", 4, 11);
            DO("BottomVisionBlow", 4, 12);
            DO("BottomVisionBlowOff", 4, 13);
            DO("NeedleVacuum", 4, 14);
            DO("NeedleBlow", 4, 15);
            for (int i = 0; i < 4; i++)
            {
                DO("FrontPicker" + (i + 1) + "_Vacuum", 4, 16 + i);
                DO("FrontPicker" + (i + 1) + "_Blow", 4, 24 + i);
                DO("RearPicker" + (i + 1) + "_Vacuum", 5, 0 + i);
                DO("RearPicker" + (i + 1) + "_Blow", 5, 8 + i);
            }

            void DI(string n, int mod, int bit, bool nc = false) =>
                c.DigitalInputs[n] = new DioMap { Module = mod, Bit = bit, Nc = nc };
            DI("StartButton", 0, 0);
            DI("StopButton", 0, 1);
            DI("ResetButton", 0, 2);
            DI("Emg", 0, 3);
            DI("MainCda1Check", 0, 7);
            DI("MainCda2Check", 0, 8);
            DI("MainVacuum1Check", 0, 9);
            DI("MainVacuum2Check", 0, 10);
            DI("WaferFeederUpChk", 0, 28);
            DI("WaferFeederDownChk", 0, 29);
            DI("NgBinGuideUpChk", 1, 31);
            DI("NgBinGuideDownChk", 2, 0);
            DI("GoodBinGuideUpChk", 2, 4);
            DI("GoodBinGuideDownChk", 2, 5);
            for (int i = 0; i < 4; i++)
            {
                DI("LeftArm_Picker" + (i + 1) + "_Flow", 1, 13 + i);
                DI("RightArm_Picker" + (i + 1) + "_Flow", 1, 23 + i);
            }

            c.Cylinders["FeederUpDownCyl"] = new CylMap
            {
                OutFwd = new DioMap { Module = 0, Bit = 16 },
                OutBwd = new DioMap { Module = 0, Bit = 17 },
                InFwd = new DioMap { Module = 0, Bit = 28 },
                InBwd = new DioMap { Module = 0, Bit = 29 }
            };
            c.Cylinders["NgBinGuideCyl"] = new CylMap
            {
                OutFwd = new DioMap { Module = 0, Bit = 26 },
                OutBwd = new DioMap { Module = 0, Bit = 27 },
                InFwd = new DioMap { Module = 1, Bit = 31 },
                InBwd = new DioMap { Module = 2, Bit = 0 }
            };
            c.Cylinders["GoodBinGuideCyl"] = new CylMap
            {
                OutFwd = new DioMap { Module = 1, Bit = 0 },
                OutBwd = new DioMap { Module = 1, Bit = 1 },
                InFwd = new DioMap { Module = 2, Bit = 4 },
                InBwd = new DioMap { Module = 2, Bit = 5 }
            };

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
    }
}
