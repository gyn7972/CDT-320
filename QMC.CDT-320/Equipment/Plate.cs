using System.Collections.Generic;

namespace QMC.CDT320
{
    /// <summary>
    /// Stage 49 — Plate (CDT-310 매뉴얼 사양: NG plate / Good plate).<br/>
    /// 본 클래스는 OutputStage 의 GoodStage / NgStage 모듈과 별개로
    /// **물리적 적재 상태** (다이 개수 + Bin 코드 분포) 를 추적하는 데이터 객체.
    /// </summary>
    public class Plate
    {
        public string PlateId   { get; set; } = "";
        public string Type      { get; set; } = "";   // "Good" | "NG"
        public int    MaxSlots  { get; set; } = 25;
        /// <summary>슬롯별 BinCode (0 = 비어있음).</summary>
        public int[]  Slots     { get; set; } = new int[25];

        public int FilledCount
        {
            get
            {
                int n = 0;
                foreach (var b in Slots) if (b > 0) n++;
                return n;
            }
        }

        public bool IsFull  => FilledCount >= MaxSlots;
        public bool IsEmpty => FilledCount == 0;

        /// <summary>다음 빈 슬롯 인덱스 (-1 이면 가득).</summary>
        public int NextEmptySlot
        {
            get
            {
                for (int i = 0; i < Slots.Length; i++)
                    if (Slots[i] == 0) return i;
                return -1;
            }
        }

        /// <summary>슬롯에 BinCode 적재.</summary>
        public bool TryFillSlot(int slotIndex, int binCode)
        {
            if (slotIndex < 0 || slotIndex >= Slots.Length) return false;
            if (Slots[slotIndex] != 0) return false;
            Slots[slotIndex] = binCode;
            return true;
        }
    }

    /// <summary>모든 Plate 통합 레지스트리.</summary>
    public static class PlateRegistry
    {
        public static Plate GoodPlate { get; } = new Plate
        {
            PlateId = "GoodPlate", Type = "Good", MaxSlots = 25,
            Slots = new int[25]
        };

        public static Plate NgPlate { get; } = new Plate
        {
            PlateId = "NgPlate", Type = "NG", MaxSlots = 25,
            Slots = new int[25]
        };

        /// <summary>Good 다이 적재 — Plate 자동 선택 후 슬롯 채움.</summary>
        public static int RecordGoodDie(int binCode)
        {
            int slot = GoodPlate.NextEmptySlot;
            if (slot < 0) return -1;
            GoodPlate.TryFillSlot(slot, binCode);
            return slot;
        }

        /// <summary>NG 다이 적재.</summary>
        public static int RecordNgDie(int binCode)
        {
            int slot = NgPlate.NextEmptySlot;
            if (slot < 0) return -1;
            NgPlate.TryFillSlot(slot, binCode);
            return slot;
        }

        public static void Reset()
        {
            for (int i = 0; i < GoodPlate.Slots.Length; i++) GoodPlate.Slots[i] = 0;
            for (int i = 0; i < NgPlate.Slots.Length;   i++) NgPlate.Slots[i]   = 0;
        }
    }
}
