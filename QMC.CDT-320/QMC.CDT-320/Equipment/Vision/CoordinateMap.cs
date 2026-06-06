using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// 비전 이미지 좌표(픽셀) ↔ 모터 좌표(mm) 어파인 매핑.
    /// 어파인 형태: [mx]   [a b] [px]   [tx]
    ///             [my] = [c d] [py] + [ty]
    /// 310 의 CoordinateManager 와 같은 역할 (간소화).
    /// </summary>
    [DataContract]
    public class CoordinateMap
    {
        [DataMember] public double A { get; set; } = 1.0;   // px → mx (scaleX)
        [DataMember] public double B { get; set; } = 0.0;   // py → mx
        [DataMember] public double C { get; set; } = 0.0;   // px → my
        [DataMember] public double D { get; set; } = 1.0;   // py → my (scaleY)
        [DataMember] public double Tx { get; set; } = 0.0;
        [DataMember] public double Ty { get; set; } = 0.0;

        public CoordinateMap() { }
        public CoordinateMap(double a, double b, double c, double d, double tx, double ty)
        {
            A = a; B = b; C = c; D = d; Tx = tx; Ty = ty;
        }

        /// <summary>픽셀 → 모터(mm).</summary>
        public void ApplyToMotor(double pixelX, double pixelY, out double motorX, out double motorY)
        {
            motorX = A * pixelX + B * pixelY + Tx;
            motorY = C * pixelX + D * pixelY + Ty;
        }

        /// <summary>모터(mm) → 픽셀 (역변환).  det == 0 이면 false.</summary>
        public bool ApplyToPixel(double motorX, double motorY, out double pixelX, out double pixelY)
        {
            double det = A * D - B * C;
            if (Math.Abs(det) < 1e-12) { pixelX = pixelY = 0; return false; }
            double inv = 1.0 / det;
            double mx  = motorX - Tx;
            double my  = motorY - Ty;
            pixelX = ( D * mx - B * my) * inv;
            pixelY = (-C * mx + A * my) * inv;
            return true;
        }

        public override string ToString()
            => $"CoordMap[A={A:F6} B={B:F6} C={C:F6} D={D:F6} Tx={Tx:F3} Ty={Ty:F3}]";
    }

    /// <summary>CoordinateMap 영속화 (Config/coord_map.json).</summary>
    public static class CoordinateMapStore
    {
        public static string Dir   => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ => System.IO.Path.Combine(Dir, "coord_map.json");

        public static CoordinateMap Current { get; private set; } = new CoordinateMap();

        static CoordinateMapStore()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            Load();
        }

        public static CoordinateMap Load()
        {
            if (!File.Exists(Path_)) { Current = new CoordinateMap(); return Current; }
            try
            {
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(CoordinateMap));
                    var m = (CoordinateMap)ser.ReadObject(fs);
                    if (m != null) Current = m;
                }
            }
            catch { Current = new CoordinateMap(); }
            return Current;
        }

        public static void Save(CoordinateMap m = null)
        {
            try
            {
                if (m != null) Current = m;
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(CoordinateMap));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }
    }
}
