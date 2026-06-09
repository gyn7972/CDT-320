using System;
using System.Collections.Generic;

namespace QMC.CDT320.Ajin
{
    public sealed class DioDefault
    {
        public int No { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string UnitName { get; set; }
        public int Module { get; set; }
        public int Bit { get; set; }
        public bool Nc { get; set; }
    }

    public sealed class CylinderDefault
    {
        public string Name { get; set; }
        public string UnitName { get; set; }
        public DioDefault OutFwd { get; set; }
        public DioDefault OutBwd { get; set; }
        public DioDefault InFwd { get; set; }
        public DioDefault InBwd { get; set; }
        public bool SingleSolenoid { get; set; }
    }

    /// <summary>
    /// Single source for CDT-320 digital I/O names and default module/bit mappings.
    /// </summary>
    public static class AjinIoCatalog
    {
        private const int BitsPerModule = 32;
        private const int OutputModuleBase = 3;
        private static int _inputNo;
        private static int _outputNo;

        public static readonly DioDefault[] DigitalOutputs =
        {
            DO("StartLamp", 3, 0),
            DO("StopLamp", 3, 1),
            DO("ResetLamp", 3, 2),
            DO("TlRed", 3, 3),
            DO("TlYellow", 3, 4),
            DO("TlGreen", 3, 5),
            DO("Buzzer", 3, 6),
            DO("IonizerOn", 3, 15),
            DO("WaferFeederUp", 3, 16),
            DO("WaferFeederDown", 3, 17),
            DO("WaferFeederClamp", 3, 18),
            DO("WaferFeederUnclamp", 3, 19),
            DO("ReticleUp", 3, 20),
            DO("ReticleDown", 3, 21),
            DO("ReticleFrontSideFw", 3, 22),
            DO("ReticleFrontSideBw", 3, 23),
            DO("ReticleRearSideFw", 3, 24),
            DO("ReticleRearSideBw", 3, 25),
            DO("NgBinGuideUp", 3, 26),
            DO("NgBinGuideDown", 3, 27),
            DO("NgBinClampUp", 3, 28),
            DO("NgBinClampDown", 3, 29),
            DO("NgBinClamp", 3, 30),
            DO("NgBinUnclamp", 3, 31),
            DO("GoodBinGuideUp", 4, 0),
            DO("GoodBinGuideDown", 4, 1),
            DO("GoodBinClampUp", 4, 2),
            DO("GoodBinClampDown", 4, 3),
            DO("GoodBinClamp", 4, 4),
            DO("GoodBinUnclamp", 4, 5),
            DO("BinFeederUp", 4, 6),
            DO("BinFeederDown", 4, 7),
            DO("BinFeederClamp", 4, 8),
            DO("BinFeederUnclamp", 4, 9),
            DO("NgBinCassetteLock", 4, 10),
            DO("NgBinCassetteUnlock", 4, 11),
            DO("BottomVisionBlow", 4, 12),
            DO("BottomVisionBlowOff", 4, 13),
            DO("NeedleVacuum", 4, 14),
            DO("NeedleBlow", 4, 15),
            DO("FrontPicker1Vacuum", 4, 16),
            DO("FrontPicker2Vacuum", 4, 17),
            DO("FrontPicker3Vacuum", 4, 18),
            DO("FrontPicker4Vacuum", 4, 19),
            DO("FrontPicker5Vacuum", 4, 20),
            DO("FrontPicker6Vacuum", 4, 21),
            DO("FrontPicker7Vacuum", 4, 22),
            DO("FrontPicker8Vacuum", 4, 23),
            DO("FrontPicker1Blow", 4, 24),
            DO("FrontPicker2Blow", 4, 25),
            DO("FrontPicker3Blow", 4, 26),
            DO("FrontPicker4Blow", 4, 27),
            DO("FrontPicker5Blow", 4, 28),
            DO("FrontPicker6Blow", 4, 29),
            DO("FrontPicker7Blow", 4, 30),
            DO("FrontPicker8Blow", 4, 31),
            DO("RearPicker1_Vacuum", 5, 0),
            DO("RearPicker2_Vacuum", 5, 1),
            DO("RearPicker3_Vacuum", 5, 2),
            DO("RearPicker4_Vacuum", 5, 3),
            DO("RearPicker5_Vacuum", 5, 4),
            DO("RearPicker6_Vacuum", 5, 5),
            DO("RearPicker7_Vacuum", 5, 6),
            DO("RearPicker8_Vacuum", 5, 7),
            DO("RearPicker1_Blow", 5, 8),
            DO("RearPicker2_Blow", 5, 9),
            DO("RearPicker3_Blow", 5, 10),
            DO("RearPicker4_Blow", 5, 11),
            DO("RearPicker5_Blow", 5, 12),
            DO("RearPicker6_Blow", 5, 13),
            DO("RearPicker7_Blow", 5, 14),
            DO("RearPicker8_Blow", 5, 15)
        };

        public static readonly DioDefault[] DigitalInputs =
        {
            DI("StartButton", 0, 0),
            DI("StopButton", 0, 1),
            DI("ResetButton", 0, 2),
            DI("ElecEmgOn", 0, 3),
            DI("OpEmgOn", 0, 4),
            DI("RearEmgOn", 0, 5),
            DI("RightEmgOn", 0, 6),
            DI("MainCDA1Check", 0, 7),
            DI("MainCDA2Check", 0, 8),
            DI("MainVacuum1Check", 0, 9),
            DI("MainVacuum2Check", 0, 10),
            DI("MainVacuum3Check", 0, 11),
            DI("MainVacuum4Check", 0, 12),
            DI("LeftDoorCheck", 0, 13),
            DI("RearDoorCheck", 0, 14),
            DI("RightDoorCheck", 0, 15),
            DI("WaferLifterDoorCheck", 0, 16),
            DI("BinLifterDoorCheck", 0, 17),
            DI("WaferLifterIonizerAlarm", 0, 18),
            DI("BinLifterIonizerAlarm", 0, 19),
            DI("WaferStageIonizerAlarm", 0, 20),
            DI("BinStageIonizerAlarm", 0, 21),
            DI("Wafer8CassetteCheck0", 0, 22),
            DI("Wafer8CassetteCheck1", 0, 23),
            DI("Wafer12CassetteCheck0", 0, 24),
            DI("Wafer12CassetteCheck1", 0, 25),
            DI("WaferRingJUTCheck", 0, 26),
            DI("WaferMapping", 0, 27),
            DI("WaferFeederUp", 0, 28),
            DI("WaferFeederDown", 0, 29),
            DI("WaferFeederUpClamp", 0, 30),
            DI("WaferFeederRingCheck", 0, 31),
            DI("WaferFeederOverloadCheck", 1, 0),
            DI("WaferStage8RingCheck", 1, 1),
            DI("WaferStage12RingCheck", 1, 2),
            DI("WaferStageTouchSensor", 1, 3),
            DI("ReticleUp", 1, 4),
            DI("ReticleDown", 1, 5),
            DI("ReticleFrontSideFw", 1, 6),
            DI("ReticleFrontSideBw", 1, 7),
            DI("ReticleRearSideFw", 1, 8),
            DI("ReticleRearSideBw", 1, 9),
            DI("NeedleVacuum", 1, 10),
            DI("FrontPickerCdaTankPressureCheck", 1, 11),
            DI("FrontPickerVacuumTankPressureCheck", 1, 12),
            DI("FrontPicker1Flow", 1, 13),
            DI("FrontPicker2Flow", 1, 14),
            DI("FrontPicker3Flow", 1, 15),
            DI("FrontPicker4Flow", 1, 16),
            DI("FrontPicker5Flow", 1, 17),
            DI("FrontPicker6Flow", 1, 18),
            DI("FrontPicker7Flow", 1, 19),
            DI("FrontPicker8Flow", 1, 20),
            DI("RearPickerCdaTankPressureCheck", 1, 21),
            DI("RearPickerVacuumTankPressureCheck", 1, 22),
            DI("RearPicker1Flow", 1, 23),
            DI("RearPicker2Flow", 1, 24),
            DI("RearPicker3Flow", 1, 25),
            DI("RearPicker4Flow", 1, 26),
            DI("RearPicker5Flow", 1, 27),
            DI("RearPicker6Flow", 1, 28),
            DI("RearPicker7Flow", 1, 29),
            DI("RearPicker8Flow", 1, 30),
            DI("NgBinGuideUp", 1, 31),
            DI("NgBinGuideDown", 2, 0),
            DI("NgBinClampUp", 2, 1),
            DI("NgBinUnclamp", 2, 2),
            DI("NgBinRing", 2, 3),
            DI("GoodBinGuideUp", 2, 4),
            DI("GoodBinGuideDown", 2, 5),
            DI("GoodBinClampUp", 2, 6),
            DI("GoodBinClamp", 2, 7), //DI("GoodBinUnclamp", 2, 7), // 명칭 수정. Unclamp -> Clamp
            DI("GoodBinRing", 2, 8),
            DI("BinNFeederUp", 2, 9),
            DI("BinFeederDown", 2, 10),
            DI("BinFeederUnclamp", 2, 11),
            DI("BinFeederRing", 2, 12),
            DI("BinFeederOverload", 2, 13),
            DI("GoodBin8CassetteCheck0", 2, 14),
            DI("GoodBin8CassetteCheck1", 2, 15),
            DI("GoodBin12CassetteCheck0", 2, 16),
            DI("GoodBin12CassetteCheck1", 2, 17),
            DI("NgBinCassetteBw", 2, 18),
            DI("NgBinCassetteLock", 2, 19),
            DI("NgBin8CassetteCheck0", 2, 20),
            DI("NgBin8CassetteCheck1", 2, 21),
            DI("NgBin12CassetteCheck0", 2, 22),
            DI("NgBin12CassetteCheck1", 2, 23),
            DI("BinRingJUTCheck", 2, 24),
            DI("BinMapping", 2, 25)
        };

        public static readonly CylinderDefault[] Cylinders =
        {
            CYL("InputFeederLift", DORef(3, 16), DORef(3, 17), DIRef(0, 28), DIRef(0, 29)),
            CYL("InputFeederClamp", DORef(3, 18), DORef(3, 19), DIRef(0, 30), DIRef(0, 31)),
            CYL("ReticleLift", DORef(3, 20), DORef(3, 21), DIRef(1, 4), DIRef(1, 5)),
            CYL("ReticleSideSlideFront", DORef(3, 22), DORef(3, 23), DIRef(1, 6), DIRef(1, 7)),
            CYL("ReticleSideSlideRear", DORef(3, 24), DORef(3, 25), DIRef(1, 8), DIRef(1, 9)),
            CYL("NGBinGuideLift", DORef(3, 26), DORef(3, 27), DIRef(1, 31), DIRef(2, 0)),
            CYL("NGBinGuideClampLift", DORef(3, 28), DORef(3, 29), DIRef(2, 1), DIRef(2, 2)),
            CYL("NGBinGuideClamp", DORef(3, 30), DORef(3, 31), DIRef(2, 1), DIRef(2, 2)),
            CYL("GoodBinGuideLift", DORef(4, 0), DORef(4, 1), DIRef(2, 4), DIRef(2, 5)),
            CYL("GoodBinGuideClampLift", DORef(4, 2), DORef(4, 3), DIRef(2, 6), DIRef(2, 7)),
            CYL("GoodBinGuideClamp", DORef(4, 4), DORef(4, 5), DIRef(2, 6), DIRef(2, 7)),
            CYL("OutputFeederLift", DORef(4, 6), DORef(4, 7), DIRef(2, 9), DIRef(2, 10)),
            CYL("OutputFeederClamp", DORef(4, 8), DORef(4, 9), DIRef(2, 11), DIRef(2, 12))
        };

        public static class Inputs
        {
            public static readonly DioDefault StartButton = FindInput("StartButton");
            public static readonly DioDefault StopButton = FindInput("StopButton");
            public static readonly DioDefault ResetButton = FindInput("ResetButton");
            public static readonly DioDefault ElecEmgOn = FindInput("ElecEmgOn");
            public static readonly DioDefault OpEmgOn = FindInput("OpEmgOn");
            public static readonly DioDefault RearEmgOn = FindInput("RearEmgOn");
            public static readonly DioDefault RightEmgOn = FindInput("RightEmgOn");
            public static readonly DioDefault MainCDA1Check = FindInput("MainCDA1Check");
            public static readonly DioDefault MainCDA2Check = FindInput("MainCDA2Check");
            public static readonly DioDefault MainVacuum1Check = FindInput("MainVacuum1Check");
            public static readonly DioDefault MainVacuum2Check = FindInput("MainVacuum2Check");
            public static readonly DioDefault MainVacuum3Check = FindInput("MainVacuum3Check");
            public static readonly DioDefault MainVacuum4Check = FindInput("MainVacuum4Check");
            public static readonly DioDefault InputLifterIonizerAlarm = FindInput("WaferLifterIonizerAlarm");
            public static readonly DioDefault Wafer8CassetteCheck0 = FindInput("Wafer8CassetteCheck0");
            public static readonly DioDefault Wafer8CassetteCheck1 = FindInput("Wafer8CassetteCheck1");
            public static readonly DioDefault Wafer12CassetteCheck0 = FindInput("Wafer12CassetteCheck0");
            public static readonly DioDefault Wafer12CassetteCheck1 = FindInput("Wafer12CassetteCheck1");
            public static readonly DioDefault WaferRingJUTCheck = FindInput("WaferRingJUTCheck");
            public static readonly DioDefault WaferMapping = FindInput("WaferMapping");
            public static readonly DioDefault WaferFeederUp = FindInput("WaferFeederUp");
            public static readonly DioDefault WaferFeederDown = FindInput("WaferFeederDown");
            public static readonly DioDefault WaferFeederUpClamp = FindInput("WaferFeederUpClamp");
            public static readonly DioDefault WaferFeederRingCheck = FindInput("WaferFeederRingCheck");
            public static readonly DioDefault WaferFeederOverloadCheck = FindInput("WaferFeederOverloadCheck");
            public static readonly DioDefault WaferFeeder8RingCheck = FindInput("WaferStage8RingCheck");
            public static readonly DioDefault WaferFeeder12RingCheck = FindInput("WaferStage12RingCheck");
            public static readonly DioDefault WaferStageTouchSensor = FindInput("WaferStageTouchSensor");
            public static readonly DioDefault NgBin8CassetteCheck0 = FindInput("NgBin8CassetteCheck0");
            public static readonly DioDefault GoodBin8CassetteCheck0 = FindInput("GoodBin8CassetteCheck0");
            public static readonly DioDefault GoodBin8CassetteCheck1 = FindInput("GoodBin8CassetteCheck1");
            public static readonly DioDefault BinRingJUTCheck = FindInput("BinRingJUTCheck");
            public static readonly DioDefault BinMapping = FindInput("BinMapping");
            public static readonly DioDefault BinFeederUnclamp = FindInput("BinFeederUnclamp");
            public static readonly DioDefault PostPnpPickOk = null;
        }

        public static class Outputs
        {
            public static readonly DioDefault StartLamp = FindOutput("StartLamp");
            public static readonly DioDefault StopLamp = FindOutput("StopLamp");
            public static readonly DioDefault ResetLamp = FindOutput("ResetLamp");
            public static readonly DioDefault TlRed = FindOutput("TlRed");
            public static readonly DioDefault TlYellow = FindOutput("TlYellow");
            public static readonly DioDefault TlGreen = FindOutput("TlGreen");
            public static readonly DioDefault Buzzer = FindOutput("Buzzer");
            public static readonly DioDefault IonizerOn = FindOutput("IonizerOn");
            public static readonly DioDefault WaferFeederUp = FindOutput("WaferFeederUp");
            public static readonly DioDefault WaferFeederDown = FindOutput("WaferFeederDown");
            public static readonly DioDefault WaferFeederClamp = FindOutput("WaferFeederClamp");
            public static readonly DioDefault WaferFeederUnclamp = FindOutput("WaferFeederUnclamp");
            public static readonly DioDefault NeedleVacuum = FindOutput("NeedleVacuum");
            public static readonly DioDefault NeedleBlow = FindOutput("NeedleBlow");
            public static readonly DioDefault PostPnpVacuum = null;
        }

        public static class CylinderRefs
        {
            public static readonly CylinderDefault InputFeederLift = FindCylinder("InputFeederLift");
            public static readonly CylinderDefault InputFeederClamp = FindCylinder("InputFeederClamp");
            public static readonly CylinderDefault ReticleLift = FindCylinder("ReticleLift");
            public static readonly CylinderDefault ReticleSideSlideFront = FindCylinder("ReticleSideSlideFront");
            public static readonly CylinderDefault ReticleSideSlideRear = FindCylinder("ReticleSideSlideRear");
            public static readonly CylinderDefault NGBinGuideLift = FindCylinder("NGBinGuideLift");
            public static readonly CylinderDefault NGBinGuideClampLift = FindCylinder("NGBinGuideClampLift");
            public static readonly CylinderDefault NGBinGuideClamp = FindCylinder("NGBinGuideClamp");
            public static readonly CylinderDefault GoodBinGuideLift = FindCylinder("GoodBinGuideLift");
            public static readonly CylinderDefault GoodBinGuideClampLift = FindCylinder("GoodBinGuideClampLift");
            public static readonly CylinderDefault GoodBinGuideClamp = FindCylinder("GoodBinGuideClamp");
            public static readonly CylinderDefault OutputFeederLift = FindCylinder("OutputFeederLift");
            public static readonly CylinderDefault OutputFeederClamp = FindCylinder("OutputFeederClamp");

            public static readonly CylinderDefault WaferFeederUpDownCyl = InputFeederLift;
            public static readonly CylinderDefault WaferFeederClampCyl = InputFeederClamp;
            public static readonly CylinderDefault NgBinGuideCyl = NGBinGuideLift;
            public static readonly CylinderDefault GoodBinGuideCyl = GoodBinGuideLift;
            public static readonly CylinderDefault BinFeederUpDownCyl = OutputFeederLift;
            public static readonly CylinderDefault BinFeederClampCyl = OutputFeederClamp;
        }

        public static DioDefault FrontPickerVacuum(int pickerNo)
        {
            return FindOutput("FrontPicker" + pickerNo + "Vacuum");
        }

        public static DioDefault FrontPickerBlow(int pickerNo)
        {
            return FindOutput("FrontPicker" + pickerNo + "Blow");
        }

        public static DioDefault RearPickerVacuum(int pickerNo)
        {
            return FindOutput("RearPicker" + pickerNo + "_Vacuum");
        }

        public static DioDefault RearPickerBlow(int pickerNo)
        {
            return FindOutput("RearPicker" + pickerNo + "_Blow");
        }

        public static void ApplyDefaults(AjinConfig config, bool overwriteExisting)
        {
            if (config == null) return;
            EnsureCollections(config);

            foreach (DioDefault item in DigitalInputs)
                Apply(config.DigitalInputs, item, overwriteExisting);
            foreach (DioDefault item in DigitalOutputs)
                Apply(config.DigitalOutputs, item, overwriteExisting);
            foreach (CylinderDefault item in Cylinders)
                Apply(config.Cylinders, item, overwriteExisting);
        }

        public static void ApplyRequiredCorrections(AjinConfig config)
        {
            if (config == null) return;
            EnsureCollections(config);

            foreach (DioDefault item in DigitalInputs)
                Apply(config.DigitalInputs, item, true);
            foreach (DioDefault item in DigitalOutputs)
                Apply(config.DigitalOutputs, item, true);
            foreach (CylinderDefault item in Cylinders)
                Apply(config.Cylinders, item, true);
        }

        public static CylinderDefault FindCylinder(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < Cylinders.Length; i++)
            {
                if (string.Equals(Cylinders[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return Cylinders[i];
            }
            return null;
        }

        public static DioDefault FindInput(string name)
        {
            return Find(DigitalInputs, name);
        }

        public static DioDefault FindInput(int module, int bit)
        {
            return Find(DigitalInputs, module, bit);
        }

        public static DioDefault FindOutput(string name)
        {
            return Find(DigitalOutputs, name);
        }

        public static DioDefault FindOutput(int module, int bit)
        {
            return Find(DigitalOutputs, module, bit);
        }

        public static string InputAddress(int module, int bit)
        {
            return "X" + (module * BitsPerModule + bit).ToString("000");
        }

        public static string OutputAddress(int module, int bit)
        {
            int outputModule = module >= OutputModuleBase ? module - OutputModuleBase : module;
            return "Y" + (outputModule * BitsPerModule + bit).ToString("000");
        }

        private static DioDefault Find(DioDefault[] items, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < items.Length; i++)
            {
                if (string.Equals(items[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return items[i];
            }
            return null;
        }

        private static DioDefault Find(DioDefault[] items, int module, int bit)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Module == module && items[i].Bit == bit)
                    return items[i];
            }
            return null;
        }

        private static void Apply(Dictionary<string, DioMap> target, DioDefault item, bool overwriteExisting)
        {
            DioMap existing;
            if (target.TryGetValue(item.Name, out existing) && !overwriteExisting)
            {
                if (existing.No == 0) existing.No = item.No;
                if (string.IsNullOrEmpty(existing.Address)) existing.Address = item.Address;
                return;
            }

            target[item.Name] = new DioMap
            {
                No = item.No,
                Address = item.Address,
                Module = item.Module,
                Bit = item.Bit,
                Nc = item.Nc
            };
        }

        private static void Apply(Dictionary<string, CylMap> target, CylinderDefault item, bool overwriteExisting)
        {
            if (target.ContainsKey(item.Name) && !overwriteExisting) return;
            target[item.Name] = new CylMap
            {
                OutFwd = Map(item.OutFwd),
                OutBwd = Map(item.OutBwd),
                InFwd = Map(item.InFwd),
                InBwd = Map(item.InBwd),
                UseFwdInput = item.InFwd != null,
                UseBwdInput = item.InBwd != null,
                SingleSolenoid = item.SingleSolenoid
            };
        }

        private static DioMap Map(DioDefault item)
        {
            if (item == null) return null;
            return new DioMap { No = item.No, Address = item.Address, Module = item.Module, Bit = item.Bit, Nc = item.Nc };
        }

        private static void EnsureCollections(AjinConfig config)
        {
            if (config.Axes == null) config.Axes = new Dictionary<string, AxisMap>();
            if (config.DigitalInputs == null) config.DigitalInputs = new Dictionary<string, DioMap>();
            if (config.DigitalOutputs == null) config.DigitalOutputs = new Dictionary<string, DioMap>();
            if (config.Cylinders == null) config.Cylinders = new Dictionary<string, CylMap>();
        }

        private static DioDefault DO(string name, int module, int bit)
        {
            return new DioDefault
            {
                No = ++_outputNo,
                Address = OutputAddress(module, bit),
                Name = name,
                UnitName = DeriveUnitName(name),
                Module = module,
                Bit = bit
            };
        }

        private static DioDefault DI(string name, int module, int bit, bool nc = false)
        {
            return new DioDefault
            {
                No = ++_inputNo,
                Address = InputAddress(module, bit),
                Name = name,
                UnitName = DeriveUnitName(name),
                Module = module,
                Bit = bit,
                Nc = nc
            };
        }

        private static DioDefault DORef(int module, int bit)
        {
            DioDefault found = FindOutput(module, bit);
            return new DioDefault
            {
                Address = OutputAddress(module, bit),
                Name = found != null ? found.Name : string.Empty,
                UnitName = found != null ? found.UnitName : string.Empty,
                Module = module,
                Bit = bit
            };
        }

        private static DioDefault DIRef(int module, int bit, bool nc = false)
        {
            DioDefault found = FindInput(module, bit);
            return new DioDefault
            {
                Address = InputAddress(module, bit),
                Name = found != null ? found.Name : string.Empty,
                UnitName = found != null ? found.UnitName : string.Empty,
                Module = module,
                Bit = bit,
                Nc = nc
            };
        }

        private static CylinderDefault CYL(string name, DioDefault outFwd, DioDefault outBwd, DioDefault inFwd, DioDefault inBwd)
        {
            return new CylinderDefault
            {
                Name = name,
                UnitName = DeriveUnitName(name),
                OutFwd = outFwd,
                OutBwd = outBwd,
                InFwd = inFwd,
                InBwd = inBwd
            };
        }

        private static string DeriveUnitName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return string.Empty;
                if (Contains(name, "WaferFeeder") || Contains(name, "InputFeeder")) return "WaferFeeder";
                if (Contains(name, "WaferStage") || Contains(name, "Reticle") || Contains(name, "Needle") || Contains(name, "Eject")) return "WaferStage";
                if (Contains(name, "FrontPicker")) return "FrontPicker";
                if (Contains(name, "RearPicker")) return "RearPicker";
                if (Contains(name, "NgBin") || Contains(name, "NGBin")) return "OutputStage";
                if (Contains(name, "GoodBin")) return "OutputStage";
                if (Contains(name, "OutputFeeder") || Contains(name, "OutputFeeder")) return "OutputFeeder";
                if (Contains(name, "BinLifter") || Contains(name, "OutputCassette")) return "OutputCassette";
                if (Contains(name, "Vision") || Contains(name, "Camera") || Contains(name, "Light")) return "Vision";
                if (Contains(name, "Lamp") || Contains(name, "Buzzer") || Contains(name, "Button") || Contains(name, "Emg")) return "OperationPanel";
                if (Contains(name, "Door") || Contains(name, "CDA") || Contains(name, "Vacuum") || Contains(name, "Ionizer")) return "Utility";
            }
            catch
            {
            }
            finally
            {
            }

            return "Common";
        }

        private static bool Contains(string text, string token)
        {
            return text != null && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
