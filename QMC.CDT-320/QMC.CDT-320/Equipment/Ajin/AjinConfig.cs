using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.CDT320.Ajin
{
    // ────────────────────────────────────────────────
    //  DTO
    // ────────────────────────────────────────────────

    [DataContract] public class AxisMap
    {
        /// <summary>AXL 라이브러리 글로벌 축 번호 (0-based).</summary>
        [DataMember] public int Axis      { get; set; }
        /// <summary>AJINEXTEK 보드 번호 (CAMC-QI/EtherCAT 마스터 인덱스). 0~N.</summary>
        [DataMember] public int BoardNo   { get; set; }
        /// <summary>해당 보드 상의 슬롯/채널 번호 (EtherCAT slot, hex 0~F 등).</summary>
        [DataMember] public int ChannelNo { get; set; }
    }
    [DataContract] public class DioMap         { [DataMember] public int Module { get; set; } [DataMember] public int Bit { get; set; } [DataMember] public bool Nc { get; set; } }
    [DataContract] public class CylMap
    {
        [DataMember] public DioMap OutFwd { get; set; }
        [DataMember] public DioMap OutBwd { get; set; }
        [DataMember] public DioMap InFwd  { get; set; }
        [DataMember] public DioMap InBwd  { get; set; }
        [DataMember] public bool   SingleSolenoid { get; set; }
    }

    [DataContract]
    public class AjinConfig
    {
        [DataMember] public Dictionary<string, AxisMap> Axes       { get; set; } = new Dictionary<string, AxisMap>();
        [DataMember] public Dictionary<string, DioMap>  DigitalInputs  { get; set; } = new Dictionary<string, DioMap>();
        [DataMember] public Dictionary<string, DioMap>  DigitalOutputs { get; set; } = new Dictionary<string, DioMap>();
        [DataMember] public Dictionary<string, CylMap>  Cylinders  { get; set; } = new Dictionary<string, CylMap>();
    }

    // ────────────────────────────────────────────────
    //  Store
    // ────────────────────────────────────────────────

    public static class AjinConfigStore
    {
        public static string Dir  { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
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
                    if (Current.Axes          == null) Current.Axes          = new Dictionary<string, AxisMap>();
                    if (Current.DigitalInputs == null) Current.DigitalInputs = new Dictionary<string, DioMap>();
                    if (Current.DigitalOutputs== null) Current.DigitalOutputs= new Dictionary<string, DioMap>();
                    if (Current.Cylinders     == null) Current.Cylinders     = new Dictionary<string, CylMap>();
                }
            }
            catch { Current = Default(); }
            return Current;
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AjinConfig));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }

        // ────────────────────────────────────────────
        //  CDT-320 기본 매핑 (SimulatorBridge 축 번호표 + IoMap 심볼 기준)
        // ────────────────────────────────────────────
        private static AjinConfig Default()
        {
            var c = new AjinConfig();

            // AXIS 0~37 (CDT-320 축 번호표 + IO LIST_R0 의 EtherCAT Master.Slot)
            //   Board 0: slot 9~F → 축 0~6
            //   Board 1: slot 0~F → 축 7~22
            //   Board 2: slot 0~E → 축 23~37
            //   (CameraZ 신규 axis 37 = Board 2 slot E)
            void A(string n, int no, int board, int ch) =>
                c.Axes[n] = new AxisMap { Axis = no, BoardNo = board, ChannelNo = ch };

            // Board 0 (slot 9~F)
            A("ElevatorZ_Input",   0,  0,  9);   // 0.9   WAFER LIFTER_Z
            A("FeederY_Input",     1,  0, 10);   // 0.A   WAFER FEEDER_Y
            A("StageY",            2,  0, 11);   // 0.B   WAFER STAGE_Y
            A("StageT",            3,  0, 12);   // 0.C   WAFER STAGE_T
            A("ExpanderZ",         4,  0, 13);   // 0.D   WAFER EXPANDING_Z
            A("CameraX",           5,  0, 14);   // 0.E   ALIGN VISION_X
            A("NeedleBlockX",      6,  0, 15);   // 0.F   NEEDLE_X

            // Board 1 (slot 0~F)
            A("NeedleZ",           7,  1,  0);   // 1.0   NEEDLE_Z
            A("EjectPinZ",         8,  1,  1);   // 1.1   EJECT PIN_Z
            A("LeftArm_ArmX",      9,  1,  2);   // 1.2   FRONT PICKER_X
            A("LeftArm_ArmY",     10,  1,  3);   // 1.3   FRONT PICKER_Y
            for (int i = 0; i < 4; i++)
            {
                A("LeftArm_Picker" + (i+1) + "_T", 11 + i * 2, 1, 4 + i * 2);   // 1.4/1.6/1.8/1.A
                A("LeftArm_Picker" + (i+1) + "_Z", 12 + i * 2, 1, 5 + i * 2);   // 1.5/1.7/1.9/1.B
            }
            A("LeftArm_SideVisionY",  19, 1, 12);   // 1.C   FRONT SIDE VISION_Y0
            A("RightArm_SideVisionY", 20, 1, 13);   // 1.D   REAR SIDE VISION_Y0
            A("RightArm_ArmX",    21, 1, 14);    // 1.E   REAR PICKER_X
            A("RightArm_ArmY",    22, 1, 15);    // 1.F   REAR PICKER_Y

            // Board 2 (slot 0~E)
            for (int i = 0; i < 4; i++)
            {
                A("RightArm_Picker" + (i+1) + "_T", 23 + i * 2, 2, 0 + i * 2);  // 2.0/2.2/2.4/2.6
                A("RightArm_Picker" + (i+1) + "_Z", 24 + i * 2, 2, 1 + i * 2);  // 2.1/2.3/2.5/2.7
            }
            A("NgStage_StageY",   31, 2,  8);    // 2.8   NG BIN_Y
            A("NgStage_StageZ",   32, 2,  9);    // 2.9   NG BIN_Z
            A("GoodStage_StageY", 33, 2, 10);    // 2.A   GOOD BIN_Y
            A("BinCameraX",       34, 2, 11);    // 2.B   INSPECTION VISION_X
            A("FeederY_Output",   35, 2, 12);    // 2.C   BIN FEEDER_Y
            A("ElevatorZ_Output", 36, 2, 13);    // 2.D   BIN LIFTER_Z
            A("CameraZ",          37, 2, 14);    // 2.E   ALIGN VISION_Z (Stage 61 신규)

            // DO - NeedleVacuum + Picker VAC/BLOW + Lamps
            void DO(string n, int mod, int bit) => c.DigitalOutputs[n] = new DioMap { Module = mod, Bit = bit };
            DO("StartLamp",     0, 0);
            DO("StopLamp",      0, 1);
            DO("ResetLamp",     0, 2);
            DO("TlRed",         0, 3);
            DO("TlYellow",      0, 4);
            DO("TlGreen",       0, 5);
            DO("Buzzer",        0, 6);
            DO("WaferFeederUp", 0, 16);
            DO("WaferFeederDown",0, 17);
            DO("NgBinGuideUp",  0, 26);
            DO("NgBinGuideDown",0, 27);
            DO("GoodBinGuideUp",  1, 0);
            DO("GoodBinGuideDown",1, 1);
            DO("BottomVisionBlow",1, 12);
            DO("NeedleVacuum",  1, 14);
            for (int i = 0; i < 4; i++)
            {
                DO("LeftArm_Picker" + (i+1) + "_Vacuum", 1, 16 + i);
                DO("LeftArm_Picker" + (i+1) + "_Blow",   1, 24 + i);
                DO("RightArm_Picker"+ (i+1) + "_Vacuum", 2, 0  + i);
                DO("RightArm_Picker"+ (i+1) + "_Blow",   2, 8  + i);
            }

            // DI
            void DI(string n, int mod, int bit, bool nc = false) =>
                c.DigitalInputs[n] = new DioMap { Module = mod, Bit = bit, Nc = nc };
            DI("StartButton",       0, 0);
            DI("StopButton",        0, 1);
            DI("ResetButton",       0, 2);
            DI("Emg",               0, 3);
            DI("MainCda1Check",     0, 7);
            DI("MainCda2Check",     0, 8);
            DI("MainVacuum1Check",  0, 9);
            DI("MainVacuum2Check",  0, 10);
            DI("WaferFeederUpChk",  0, 28);
            DI("WaferFeederDownChk",0, 29);
            DI("NgBinGuideUpChk",   1, 31);
            DI("NgBinGuideDownChk", 2, 0);
            DI("GoodBinGuideUpChk", 2, 4);
            DI("GoodBinGuideDownChk",2, 5);
            for (int i = 0; i < 4; i++)
            {
                DI("LeftArm_Picker" + (i+1) + "_Flow",  1, 13 + i);
                DI("RightArm_Picker"+ (i+1) + "_Flow",  1, 23 + i);
            }

            // Cylinders — CDT-320 에 주요 실린더 몇 개 미리 매핑
            c.Cylinders["FeederUpDownCyl"] = new CylMap
            {
                OutFwd = new DioMap { Module = 0, Bit = 16 },
                OutBwd = new DioMap { Module = 0, Bit = 17 },
                InFwd  = new DioMap { Module = 0, Bit = 28 },
                InBwd  = new DioMap { Module = 0, Bit = 29 }
            };
            c.Cylinders["NgBinGuideCyl"] = new CylMap
            {
                OutFwd = new DioMap { Module = 0, Bit = 26 },
                OutBwd = new DioMap { Module = 0, Bit = 27 },
                InFwd  = new DioMap { Module = 1, Bit = 31 },
                InBwd  = new DioMap { Module = 2, Bit = 0 }
            };
            c.Cylinders["GoodBinGuideCyl"] = new CylMap
            {
                OutFwd = new DioMap { Module = 1, Bit = 0 },
                OutBwd = new DioMap { Module = 1, Bit = 1 },
                InFwd  = new DioMap { Module = 2, Bit = 4 },
                InBwd  = new DioMap { Module = 2, Bit = 5 }
            };

            return c;
        }
    }
}
