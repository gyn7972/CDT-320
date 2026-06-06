using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.CDT320.Lots
{
    /// <summary>Lot 상태.</summary>
    [DataContract]
    public enum LotState
    {
        [EnumMember] Open,
        [EnumMember] Running,
        [EnumMember] Completed,
        [EnumMember] Aborted,
    }

    /// <summary>다이 lot — 생산 단위 통계 누적 (310 의 Lot 객체 단순화).</summary>
    [DataContract]
    public class Lot
    {
        [DataMember] public string   LotID         { get; set; } = "";
        [DataMember] public string   RecipeName    { get; set; } = "";
        [DataMember] public LotState State         { get; set; } = LotState.Open;
        [DataMember] public DateTime StartedAt     { get; set; } = DateTime.Now;
        [DataMember] public DateTime? FinishedAt   { get; set; }

        [DataMember] public int      TotalDies     { get; set; } = 0;
        [DataMember] public int      ProcessedDies { get; set; } = 0;
        [DataMember] public int      GoodCount     { get; set; } = 0;
        [DataMember] public int      NgCount       { get; set; } = 0;
        [DataMember] public int      SkippedCount  { get; set; } = 0;

        /// <summary>BinCode 별 카운트 (key=binCode, value=count).</summary>
        [DataMember] public Dictionary<int, int> BinDistribution { get; set; }
            = new Dictionary<int, int>();

        public double YieldPercent
            => ProcessedDies > 0 ? (GoodCount * 100.0 / ProcessedDies) : 0;

        public TimeSpan Duration
            => (FinishedAt ?? DateTime.Now) - StartedAt;

        public void RecordDie(int binCode, bool isGood)
        {
            ProcessedDies++;
            if (isGood) GoodCount++; else NgCount++;
            if (BinDistribution.ContainsKey(binCode))
                BinDistribution[binCode]++;
            else
                BinDistribution[binCode] = 1;
        }

        public override string ToString()
            => $"Lot[{LotID}] {State} processed={ProcessedDies}/{TotalDies} good={GoodCount} ng={NgCount} skipped={SkippedCount} yield={YieldPercent:F1}% bins={BinDistribution.Count}";
    }
}
