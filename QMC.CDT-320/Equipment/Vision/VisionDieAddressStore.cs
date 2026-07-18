using System;
using System.Collections.Generic;

namespace QMC.CDT320.VisionComm
{
    /// <summary>콜렛 1개의 비전 주소 정보 — 신형 와이어(die_index + gridx;gridy) 구성용.</summary>
    public sealed class VisionDieAddress
    {
        /// <summary>0=Front / 1=Back(Rear).</summary>
        public int Fb { get; set; }
        /// <summary>콜렛 1~4.</summary>
        public int Collet { get; set; }
        /// <summary>픽업 순서 1-base(=InputSequenceNo) — 결과 매칭 키(die_index). 미지정 시 음수 합성키.</summary>
        public int DieIndex { get; set; }
        /// <summary>웨이퍼 격자 인덱스 X(모름=-1).</summary>
        public int GridX { get; set; } = -1;
        /// <summary>웨이퍼 격자 인덱스 Y(모름=-1).</summary>
        public int GridY { get; set; } = -1;
        /// <summary>자재 고유 ID(로그 추적용 — 와이어에는 싣지 않는다, 2026-07-06 chip_uid 파트 폐기).</summary>
        public string DieId { get; set; } = "";
        /// <summary>자동 검사 요청의 WAFER_ID 문맥.</summary>
        public string WaferId { get; set; } = "";
        public DateTime SetAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// (fb, collet) → 현재 물고 있는 다이의 비전 주소(die_index/grid) 스토어.
    /// <para>시퀀스(Bottom/Side 검사 등)가 다이 정보를 아는 시점에 <see cref="Set"/> 으로 기록하고,
    /// 어댑터(<see cref="TpuVisionAdapter"/>)가 MATCHASYNC/INSPECTASYNC 신형 와이어를 구성할 때 조회한다.
    /// 다이 정보가 없으면 <see cref="FallbackDieIndex"/> 의 음수 합성키(-(fb*4+collet))로 콜렛 간 키 충돌을 방지한다
    /// (음수 die_index = 메뉴얼/미지정 — Vision 은 맵 매칭을 생략하고 검사만 수행).</para> 스레드 세이프.
    /// </summary>
    public static class VisionDieAddressStore
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<int, VisionDieAddress> _byCollet = new Dictionary<int, VisionDieAddress>();   // key = fb*4+collet

        /// <summary>다이 주소 기록 — 픽업/검사 진입 시점에 시퀀스가 호출.</summary>
        public static void Set(
            int fb,
            int collet,
            int dieIndex,
            int gridX,
            int gridY,
            string dieId = "",
            string waferId = "")
        {
            if (fb < 0 || collet < 1 || collet > 4)
                return;
            lock (_lock)
            {
                _byCollet[fb * 4 + collet] = new VisionDieAddress
                {
                    Fb = fb, Collet = collet,
                    DieIndex = dieIndex, GridX = gridX, GridY = gridY,
                    DieId = dieId ?? "",
                    WaferId = waferId ?? ""
                };
            }
        }

        /// <summary>다이 주소 조회 — 없으면 false(호출부는 FallbackDieIndex 사용).</summary>
        public static bool TryGet(int fb, int collet, out VisionDieAddress address)
        {
            lock (_lock)
                return _byCollet.TryGetValue(fb * 4 + collet, out address);
        }

        /// <summary>배출/초기화 시 해제.</summary>
        public static void Clear(int fb, int collet)
        {
            lock (_lock)
                _byCollet.Remove(fb * 4 + collet);
        }

        /// <summary>전체 해제(Lot 종료 등).</summary>
        public static void ClearAll()
        {
            lock (_lock)
                _byCollet.Clear();
        }

        /// <summary>다이 정보가 없을 때의 합성 die_index — 콜렛별 고유 음수(-1~-8, 메뉴얼 취급).</summary>
        public static int FallbackDieIndex(int fb, int collet) => -(fb * 4 + collet);
    }
}
