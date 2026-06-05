using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.CDT320.Materials
{
    /// <summary>다이 검사 결과.</summary>
    [DataContract]
    public enum DieResult
    {
        [EnumMember] Unknown,
        [EnumMember] Good,
        [EnumMember] NG
    }

    /// <summary>Lot/Recipe/Picker 사용 정보 (310 의 ProcessInformationData).</summary>
    [DataContract]
    public class ProcessInformationData
    {
        [DataMember] public string LotID            { get; set; } = "";
        [DataMember] public string RecipeName       { get; set; } = "";
        [DataMember] public int    UsingPickerNumber{ get; set; } = -1;
        [DataMember] public int    RetestCount      { get; set; } = 0;
    }

    /// <summary>다이 자재 (310 의 Die 와 같은 정보 — 단순 POCO).</summary>
    [DataContract]
    public class Die
    {
        /// <summary>고유 ID (chipUid). MaterialStorage 키.</summary>
        [DataMember] public string Uid { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 12);

        /// <summary>웨이퍼 격자 인덱스 (0 ~ Grid-1).</summary>
        [DataMember] public int    GridX  { get; set; }
        [DataMember] public int    GridY  { get; set; }

        /// <summary>모터 좌표(mm) — 비전 정렬 후 산출.</summary>
        [DataMember] public double X { get; set; }
        [DataMember] public double Y { get; set; }
        /// <summary>회전(deg). MATCH 응답의 r.</summary>
        [DataMember] public double R { get; set; }

        /// <summary>검사 결과.</summary>
        [DataMember] public DieResult Result { get; set; } = DieResult.Unknown;

        /// <summary>NG 코드 누적 (검사기별 fail 사유).</summary>
        [DataMember] public List<string> NGCodes { get; set; } = new List<string>();

        /// <summary>BinCodeMap 으로 산출된 빈 번호 (1=Good, 255=Max).</summary>
        [DataMember] public int    BinCode { get; set; } = 0;

        /// <summary>처리 정보 (Lot/Recipe/Picker).</summary>
        [DataMember] public ProcessInformationData Process { get; set; } = new ProcessInformationData();

        /// <summary>발견된 결함의 누적 메트릭 (Width/Height/Chipping 등 검사 결과 키-값).</summary>
        [DataMember] public Dictionary<string, string> Metrics { get; set; } = new Dictionary<string, string>();

        public void AddNG(string code)
        {
            if (!string.IsNullOrEmpty(code) && !NGCodes.Contains(code))
                NGCodes.Add(code);
            Result = DieResult.NG;
        }

        public override string ToString()
            => $"Die[{Uid}] grid=({GridX},{GridY}) pos=({X:F2},{Y:F2}) result={Result} bin={BinCode} ng=[{string.Join(",", NGCodes)}]";
    }
}
