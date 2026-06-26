using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.Vision.DieMaps
{
    /// <summary>
    /// 명명된 웨이퍼(TapeFrame) 사양 1종(핸들러 TapeFrameSpec 대응, Vision 자체 저장용).
    /// </summary>
    [DataContract]
    public class TapeFrameSpec
    {
        [DataMember] public string Name { get; set; } = "";
        [DataMember] public int DieMapX { get; set; } = 45;
        [DataMember] public int DieMapY { get; set; } = 45;
        [DataMember] public double PitchX { get; set; } = 1.0;
        [DataMember] public double PitchY { get; set; } = 1.0;
        [DataMember] public double OuterDiameterMm { get; set; } = 290.0;
        [DataMember] public string Rotate { get; set; } = "None";
        [DataMember] public int SideEdgeSkip { get; set; } = 0;
        [DataMember] public int TopBottomEdgeSkip { get; set; } = 0;
    }

    /// <summary>
    /// 웨이퍼 사양 라이브러리 — 명명된 TapeFrameSpec 목록을 JSON 으로 영속화(LOAD/SAVE SPEC).
    /// 파일: {앱}\Config\frame_specs.json.
    /// </summary>
    public static class TapeFrameSpecStore
    {
        private static List<TapeFrameSpec> _specs;

        private static string FilePath
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "frame_specs.json");

        /// <summary>전체 스펙 목록(지연 로드).</summary>
        public static List<TapeFrameSpec> All()
        {
            if (_specs == null) Load();
            return _specs;
        }

        /// <summary>스펙명 목록.</summary>
        public static List<string> Names()
            => All().Where(s => s != null && !string.IsNullOrWhiteSpace(s.Name))
                    .Select(s => s.Name).ToList();

        /// <summary>이름으로 스펙 조회(없으면 null).</summary>
        public static TapeFrameSpec Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return All().FirstOrDefault(s => s != null &&
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>스펙을 추가하거나 동명 스펙을 갱신한 뒤 저장한다.</summary>
        public static bool AddOrUpdate(TapeFrameSpec spec)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.Name)) return false;
            var list = All();
            int idx = list.FindIndex(s => s != null &&
                string.Equals(s.Name, spec.Name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) list[idx] = spec;
            else list.Add(spec);
            return Save();
        }

        /// <summary>디스크에서 목록을 로드한다(실패/없음 시 빈 목록).</summary>
        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    using (var fs = File.OpenRead(FilePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(List<TapeFrameSpec>));
                        _specs = (List<TapeFrameSpec>)ser.ReadObject(fs) ?? new List<TapeFrameSpec>();
                        return;
                    }
                }
            }
            catch
            {
                // 손상/형식오류 시 빈 목록으로 시작.
            }
            _specs = new List<TapeFrameSpec>();
        }

        /// <summary>목록을 JSON pretty 로 저장한다.</summary>
        public static bool Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using (var fs = File.Create(FilePath))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(List<TapeFrameSpec>), _specs ?? new List<TapeFrameSpec>());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
