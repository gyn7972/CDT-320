using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Bin
{
    /// <summary>거부 자재 분리 설정 (310 의 SubPortMaterialRejector 단순화).</summary>
    [DataContract]
    public class RejectConfig
    {
        /// <summary>이 BinCode 보다 큰 다이는 별도 슬롯으로 분리.</summary>
        [DataMember] public int    RejectBinThreshold { get; set; } = 100;
        /// <summary>거부 자재 PLACE 좌표 X (mm).</summary>
        [DataMember] public double RejectX            { get; set; } = 1500.0;
        /// <summary>거부 자재 PLACE 좌표 Y.</summary>
        [DataMember] public double RejectY            { get; set; } = 200.0;
        /// <summary>활성 여부.</summary>
        [DataMember] public bool   Enable             { get; set; } = true;
    }

    /// <summary>BinCode 기반 자재 거부 분리 결정.</summary>
    public static class SubPortMaterialRejector
    {
        public static string Dir   => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ => System.IO.Path.Combine(Dir, "reject_config.json");

        public static RejectConfig Current { get; private set; } = new RejectConfig();

        static SubPortMaterialRejector()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            Load();
        }

        public static void Load()
        {
            if (!File.Exists(Path_)) { Current = new RejectConfig(); Save(); return; }
            try
            {
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(RejectConfig));
                    var loaded = (RejectConfig)ser.ReadObject(fs);
                    if (loaded != null) Current = loaded;
                }
            }
            catch { Current = new RejectConfig(); }
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(RejectConfig));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }

        /// <summary>다이의 BinCode 가 거부 임계값 초과 시 true. 거부 좌표 반환.</summary>
        public static bool ShouldReject(Die die, out double placeX, out double placeY)
        {
            placeX = placeY = 0;
            if (die == null || !Current.Enable) return false;
            if (die.BinCode <= Current.RejectBinThreshold) return false;
            placeX = Current.RejectX;
            placeY = Current.RejectY;
            return true;
        }
    }
}
