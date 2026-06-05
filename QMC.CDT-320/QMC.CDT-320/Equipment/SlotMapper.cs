using System.Collections.Generic;

namespace QMC.CDT320
{
    /// <summary>
    /// Stage 46 — Slot Mapper (CDT-310 매뉴얼 사양).<br/>
    /// Wafer / Bin 카세트의 슬롯 매핑 결과를 통합 관리하는 데이터 컨테이너.
    /// 카세트 스캔 결과는 InputLoader.WaferMap / OutputUnloader._slotMap 에 저장되며,
    /// 본 클래스는 외부 소비자(UI/SECS/Lot 통계) 가 통일된 형식으로 접근하도록 함.
    /// </summary>
    public class SlotMapper
    {
        public string CassetteId { get; set; } = "";
        public int    SlotCount  { get; set; } = 0;
        /// <summary>슬롯별 점유 여부 (true = 웨이퍼/Bin 있음).</summary>
        public bool[] OccupancyMap { get; set; } = new bool[0];

        /// <summary>점유된 슬롯 수.</summary>
        public int OccupiedCount
        {
            get
            {
                int n = 0;
                if (OccupancyMap != null)
                    foreach (var b in OccupancyMap) if (b) n++;
                return n;
            }
        }

        /// <summary>가득 찬지 여부.</summary>
        public bool IsFull => OccupiedCount >= SlotCount && SlotCount > 0;

        /// <summary>비어있는지 여부.</summary>
        public bool IsEmpty => OccupiedCount == 0;

        public override string ToString()
        {
            return $"SlotMapper[{CassetteId}] {OccupiedCount}/{SlotCount}";
        }
    }

    /// <summary>모든 카세트 SlotMapper 통합 컨테이너.</summary>
    public static class SlotMapperRegistry
    {
        public static Dictionary<string, SlotMapper> Mappers { get; }
            = new Dictionary<string, SlotMapper>();

        public static void Update(string cassetteId, bool[] map)
        {
            if (!Mappers.ContainsKey(cassetteId))
                Mappers[cassetteId] = new SlotMapper { CassetteId = cassetteId };
            var sm = Mappers[cassetteId];
            sm.OccupancyMap = map ?? new bool[0];
            sm.SlotCount    = map?.Length ?? 0;
        }
    }
}
