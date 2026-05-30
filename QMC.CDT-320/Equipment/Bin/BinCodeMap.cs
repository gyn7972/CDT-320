using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Bin
{
    /// <summary>NG 코드 → bin 번호 / bin 번호 → 색상 매핑 (310 의 BinCodeManager).</summary>
    [DataContract]
    public class BinCodeMapData
    {
        /// <summary>NG 코드 → bin 번호. 우선순위 따라 위에서부터 검사.</summary>
        [DataMember] public List<KeyValuePair<string,int>> Codes { get; set; }
        /// <summary>bin 번호 → "#RRGGBB" 색상 코드.</summary>
        [DataMember] public List<KeyValuePair<int,string>> Colors { get; set; }

        public static BinCodeMapData CreateDefault()
        {
            return new BinCodeMapData
            {
                Codes = new List<KeyValuePair<string,int>>
                {
                    new KeyValuePair<string,int>("PickFail",         101),
                    new KeyValuePair<string,int>("VisionMatchFail",  102),
                    new KeyValuePair<string,int>("BottomInspFail",   103),
                    new KeyValuePair<string,int>("PlacementFail",    104),
                    new KeyValuePair<string,int>("DieGapOver",       105),
                    new KeyValuePair<string,int>("DieGapUnder",      106),
                    new KeyValuePair<string,int>("ChippingTopOver",  110),
                    new KeyValuePair<string,int>("ChippingRightOver",111),
                    new KeyValuePair<string,int>("ChippingBottomOver",112),
                    new KeyValuePair<string,int>("ChippingLeftOver", 113),
                    new KeyValuePair<string,int>("ForeignOver",      120),
                    new KeyValuePair<string,int>("WidthOver",        130),
                    new KeyValuePair<string,int>("HeightOver",       131),
                    new KeyValuePair<string,int>("Unknown",          255),
                },
                Colors = new List<KeyValuePair<int,string>>
                {
                    new KeyValuePair<int,string>(1,   "#33B26B"),  // Good — green
                    new KeyValuePair<int,string>(100, "#FFCC00"),  // pre-process — yellow
                    new KeyValuePair<int,string>(120, "#FF6600"),  // chipping/foreign — orange
                    new KeyValuePair<int,string>(200, "#FF3333"),  // critical — red
                    new KeyValuePair<int,string>(255, "#888888"),  // unknown — gray
                }
            };
        }
    }

    /// <summary>BinCodeMap 영속화 + 조회 (싱글톤 정적 클래스).</summary>
    public static class BinCodeMap
    {
        public const int GoodBin = 1;
        public const int MaxBin  = 255;

        private static BinCodeMapData _data = BinCodeMapData.CreateDefault();
        public static BinCodeMapData Data => _data;

        public static string Dir   => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ => System.IO.Path.Combine(Dir, "bin_codes.json");

        static BinCodeMap()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            Load();
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(Path_))
                {
                    _data = BinCodeMapData.CreateDefault();
                    Save();
                    return;
                }
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(BinCodeMapData));
                    var loaded = (BinCodeMapData)ser.ReadObject(fs);
                    if (loaded != null) _data = loaded;
                }
            }
            catch { _data = BinCodeMapData.CreateDefault(); }
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(BinCodeMapData));
                    ser.WriteObject(fs, _data);
                }
            }
            catch { }
        }

        /// <summary>다이의 NGCodes 를 보고 bin 번호 산출. NGCodes 가 비어 있으면 GoodBin(1).</summary>
        public static int ConvertToBinCode(Die die)
        {
            if (die == null) return MaxBin;
            if (die.NGCodes == null || die.NGCodes.Count == 0) return GoodBin;

            // 매핑 우선순위: Codes 리스트의 첫 매치
            if (_data.Codes != null)
            {
                foreach (var kv in _data.Codes)
                {
                    if (die.NGCodes.Contains(kv.Key)) return kv.Value;
                }
            }
            return MaxBin;
        }

        /// <summary>bin 번호 → 색상. Colors 정렬 후 첫 ≥ bin 인 색상.</summary>
        public static Color ConvertToBinCodeColor(int binCode)
        {
            if (binCode <= 0) return Color.Black;
            if (_data.Colors == null || _data.Colors.Count == 0) return Color.Gray;

            var sorted = _data.Colors.OrderBy(c => c.Key).ToList();
            foreach (var kv in sorted)
            {
                if (binCode <= kv.Key) return ParseHex(kv.Value);
            }
            return ParseHex(sorted.Last().Value);
        }

        private static Color ParseHex(string hex)
        {
            try
            {
                if (string.IsNullOrEmpty(hex)) return Color.Gray;
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return Color.FromArgb(r, g, b);
                }
            }
            catch { }
            return Color.Gray;
        }
    }
}
