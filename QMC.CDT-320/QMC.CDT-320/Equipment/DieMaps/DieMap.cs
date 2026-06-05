using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using QMC.CDT320.Materials;

namespace QMC.CDT320.DieMaps
{
    /// <summary>다이 맵의 한 셀.</summary>
    [DataContract]
    public class DieMapEntry
    {
        [DataMember] public int       Index    { get; set; }
        [DataMember] public int       GridX    { get; set; }
        [DataMember] public int       GridY    { get; set; }
        /// <summary>true 면 이 위치는 처리 대상 (good die candidate).</summary>
        [DataMember] public bool      IsTarget { get; set; } = true;
        [DataMember] public DieResult Result   { get; set; } = DieResult.Unknown;
        [DataMember] public int       BinCode  { get; set; } = 0;
        /// <summary>모터 좌표 (mm).</summary>
        [DataMember] public double    X        { get; set; }
        [DataMember] public double    Y        { get; set; }
        /// <summary>해당 셀에 매핑된 Die.Uid (없으면 빈 문자열).</summary>
        [DataMember] public string    DieUid   { get; set; } = "";
    }

    /// <summary>웨이퍼 다이 맵.</summary>
    [DataContract]
    public class DieMap
    {
        [DataMember] public string FrameObjId { get; set; } = "";
        [DataMember] public int    GridX      { get; set; }
        [DataMember] public int    GridY      { get; set; }
        [DataMember] public double PitchX     { get; set; }
        [DataMember] public double PitchY     { get; set; }
        [DataMember] public double OriginX    { get; set; }
        [DataMember] public double OriginY    { get; set; }
        [DataMember] public List<DieMapEntry> Entries { get; set; } = new List<DieMapEntry>();
        [DataMember] public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int TotalCells => GridX * GridY;

        /// <summary>(gx, gy) 셀을 가져옴 (없으면 null).</summary>
        public DieMapEntry GetCell(int gx, int gy)
        {
            foreach (var e in Entries)
                if (e.GridX == gx && e.GridY == gy) return e;
            return null;
        }

        /// <summary>인덱스 i 셀.</summary>
        public DieMapEntry GetByIndex(int i)
            => (i >= 0 && i < Entries.Count) ? Entries[i] : null;
    }
}
