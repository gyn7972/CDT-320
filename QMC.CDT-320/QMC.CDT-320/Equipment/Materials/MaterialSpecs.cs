using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.CDT320.Materials
{
    /// <summary>다이 1종의 기준 사양 (310 의 DieSpec).</summary>
    [DataContract]
    public class DieSpec
    {
        [DataMember] public string Name        { get; set; } = "";
        [DataMember] public double WidthMm     { get; set; } = 1.0;
        [DataMember] public double HeightMm    { get; set; } = 1.0;
        [DataMember] public double WidthLower  { get; set; } = 0;
        [DataMember] public double WidthUpper  { get; set; } = 0;
        [DataMember] public double HeightLower { get; set; } = 0;
        [DataMember] public double HeightUpper { get; set; } = 0;
        [DataMember] public double ThicknessMm { get; set; } = 0.1;
    }

    /// <summary>웨이퍼(테이프 프레임) 1종의 기준 사양 (310 의 TapeFrameSpec).</summary>
    [DataContract]
    public class TapeFrameSpec
    {
        [DataMember] public string Name    { get; set; } = "";
        [DataMember] public int    GridX   { get; set; } = 1;
        [DataMember] public int    GridY   { get; set; } = 1;
        [DataMember] public double PitchX  { get; set; } = 1.0;
        [DataMember] public double PitchY  { get; set; } = 1.0;
        [DataMember] public double OuterDiameterMm { get; set; } = 200; // 8inch=200, 12inch=300
        [DataMember] public string DieSpecName { get; set; } = "";       // 참조할 DieSpec.Name
    }

    /// <summary>설정 모음.</summary>
    [DataContract]
    public class MaterialSpecsData
    {
        [DataMember] public List<DieSpec>       Dies   { get; set; } = new List<DieSpec>();
        [DataMember] public List<TapeFrameSpec> Frames { get; set; } = new List<TapeFrameSpec>();

        public static MaterialSpecsData CreateDefault()
        {
            return new MaterialSpecsData
            {
                Dies = new List<DieSpec>
                {
                    new DieSpec { Name="Default", WidthMm=1.0, HeightMm=1.0, WidthLower=-0.05, WidthUpper=0.05, HeightLower=-0.05, HeightUpper=0.05, ThicknessMm=0.1 },
                    new DieSpec { Name="Small",   WidthMm=0.5, HeightMm=0.5, WidthLower=-0.02, WidthUpper=0.02, HeightLower=-0.02, HeightUpper=0.02, ThicknessMm=0.08 },
                    new DieSpec { Name="Large",   WidthMm=3.0, HeightMm=3.0, WidthLower=-0.10, WidthUpper=0.10, HeightLower=-0.10, HeightUpper=0.10, ThicknessMm=0.20 },
                },
                Frames = new List<TapeFrameSpec>
                {
                    new TapeFrameSpec { Name="8inch_5x5",   GridX=5,   GridY=5,   PitchX=1.0, PitchY=1.0, OuterDiameterMm=200, DieSpecName="Default" },
                    new TapeFrameSpec { Name="12inch_50x50",GridX=50,  GridY=50,  PitchX=1.0, PitchY=1.0, OuterDiameterMm=300, DieSpecName="Default" },
                }
            };
        }
    }

    /// <summary>MaterialSpecs 영속화 (JSON).</summary>
    public static class MaterialSpecs
    {
        public static string Dir   => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ => System.IO.Path.Combine(Dir, "material_specs.json");

        private static MaterialSpecsData _data = MaterialSpecsData.CreateDefault();
        public  static MaterialSpecsData Data => _data;

        static MaterialSpecs()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            Load();
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(Path_)) { _data = MaterialSpecsData.CreateDefault(); Save(); return; }
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(MaterialSpecsData));
                    var loaded = (MaterialSpecsData)ser.ReadObject(fs);
                    if (loaded != null) _data = loaded;
                }
            }
            catch { _data = MaterialSpecsData.CreateDefault(); }
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(MaterialSpecsData));
                    ser.WriteObject(fs, _data);
                }
            }
            catch { }
        }

        public static DieSpec FindDie(string name)
            => _data.Dies?.FirstOrDefault(d => d.Name == name);

        public static TapeFrameSpec FindFrame(string name)
            => _data.Frames?.FirstOrDefault(f => f.Name == name);
    }
}
