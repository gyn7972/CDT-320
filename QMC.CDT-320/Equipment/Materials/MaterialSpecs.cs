using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

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
        [DataMember] public double ChippingDepthMax { get; set; } = 0.05;
        [DataMember] public double ChippingLengthMax { get; set; } = 0.20;
        [DataMember] public double ForeignSizeMax { get; set; } = 0.005;
    }

    /// <summary>웨이퍼(테이프 프레임) 1종의 기준 사양 (310 의 TapeFrameSpec).</summary>
    [DataContract]
    public class TapeFrameSpec
    {
        [DataMember] public string Name    { get; set; } = "";
        [DataMember] public int    DieMapX   { get; set; } = 1;
        [DataMember] public int    DieMapY   { get; set; } = 1;
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
                    new DieSpec { Name="Default", WidthMm=1.0, HeightMm=1.0, WidthLower=-0.05, WidthUpper=0.05, HeightLower=-0.05, HeightUpper=0.05, ThicknessMm=0.1, ChippingDepthMax=0.05, ChippingLengthMax=0.20, ForeignSizeMax=0.005 },
                    new DieSpec { Name="Small",   WidthMm=0.5, HeightMm=0.5, WidthLower=-0.02, WidthUpper=0.02, HeightLower=-0.02, HeightUpper=0.02, ThicknessMm=0.08, ChippingDepthMax=0.03, ChippingLengthMax=0.10, ForeignSizeMax=0.003 },
                    new DieSpec { Name="Large",   WidthMm=3.0, HeightMm=3.0, WidthLower=-0.10, WidthUpper=0.10, HeightLower=-0.10, HeightUpper=0.10, ThicknessMm=0.20, ChippingDepthMax=0.08, ChippingLengthMax=0.30, ForeignSizeMax=0.010 },
                },
                Frames = new List<TapeFrameSpec>
                {
                    new TapeFrameSpec { Name="8inch_5x5",   DieMapX=5,   DieMapY=5,   PitchX=1.0, PitchY=1.0, OuterDiameterMm=200, DieSpecName="Default" },
                    new TapeFrameSpec { Name="12inch_50x50",DieMapX=50,  DieMapY=50,  PitchX=1.0, PitchY=1.0, OuterDiameterMm=300, DieSpecName="Default" },
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
                    JsonPrettySerializer.WriteObject(fs, typeof(MaterialSpecsData), _data);
                }
            }
            catch { }
        }

        public static DieSpec FindDie(string name)
            => _data.Dies?.FirstOrDefault(d => d.Name == name);

        public static TapeFrameSpec FindFrame(string name)
            => _data.Frames?.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

        public static DieSpec UpsertDie(
            string name,
            double widthMm,
            double heightMm,
            double thicknessMm,
            double widthLower,
            double widthUpper,
            double heightLower,
            double heightUpper,
            double chippingDepthMax,
            double chippingLengthMax,
            double foreignSizeMax)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Die_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (_data.Dies == null)
                _data.Dies = new List<DieSpec>();

            string specName = name.Trim();
            var spec = _data.Dies.FirstOrDefault(d => string.Equals(d.Name, specName, StringComparison.OrdinalIgnoreCase));
            if (spec == null)
            {
                spec = new DieSpec { Name = specName };
                _data.Dies.Add(spec);
            }

            spec.WidthMm = widthMm > 0.0 ? widthMm : 1.0;
            spec.HeightMm = heightMm > 0.0 ? heightMm : 1.0;
            spec.ThicknessMm = thicknessMm > 0.0 ? thicknessMm : 0.1;
            spec.WidthLower = widthLower;
            spec.WidthUpper = widthUpper;
            spec.HeightLower = heightLower;
            spec.HeightUpper = heightUpper;
            spec.ChippingDepthMax = chippingDepthMax;
            spec.ChippingLengthMax = chippingLengthMax;
            spec.ForeignSizeMax = foreignSizeMax;
            Save();
            return spec;
        }

        public static TapeFrameSpec UpsertFrame(string name, int dieMapX, int dieMapY, double pitchX, double pitchY, double outerDiameterMm, string dieSpecName)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Frame_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (_data.Frames == null)
                _data.Frames = new List<TapeFrameSpec>();

            string specName = name.Trim();
            var spec = _data.Frames.FirstOrDefault(f => string.Equals(f.Name, specName, StringComparison.OrdinalIgnoreCase));
            if (spec == null)
            {
                spec = new TapeFrameSpec { Name = specName };
                _data.Frames.Add(spec);
            }

            spec.DieMapX = Math.Max(1, dieMapX);
            spec.DieMapY = Math.Max(1, dieMapY);
            spec.PitchX = pitchX > 0.0 ? pitchX : 1.0;
            spec.PitchY = pitchY > 0.0 ? pitchY : 1.0;
            spec.OuterDiameterMm = outerDiameterMm > 0.0 ? outerDiameterMm : 200.0;
            spec.DieSpecName = dieSpecName ?? "";
            Save();
            return spec;
        }
    }
}
