using System;
using System.Collections.Generic;
using QMC.Vision.Modules;

namespace QMC.Vision.DieMaps
{
    /// <summary>
    /// 다이 인덱스(픽업 순서 1-base) → 레시피 다이맵 셀(IndexX/IndexY) 매칭 리졸버.
    /// <para>핸들러는 실제 칩 위치를 알고 '인덱스 번호'만 보내고, Vision 은 활성 레시피의
    /// 칩위치(InputDieMap 없으면 웨이퍼 사양 생성) + Pickup 옵션으로 같은 순서를 만들어
    /// 인덱스를 셀 좌표로 환산한다(Bottom 모니터링 맵 등 표시용).</para>
    /// <para>순서 목록은 레시피(맵/픽업 옵션) 단위로 캐시하며, 변경 시 자동 재생성. 스레드 세이프.</para>
    /// </summary>
    public static class PickupOrderResolver
    {
        private static readonly object _lock = new object();
        private static List<int[]> _order;      // 픽업 순서대로 {DieMapX, DieMapY}
        private static string _key;             // 캐시 키(맵/픽업 옵션 지문)

        /// <summary>활성 레시피 기준 픽업 순서 길이(=웨이퍼 1바퀴 다이 수). 못 구하면 0.</summary>
        public static int Count
        {
            get { var o = Ensure(); return o != null ? o.Count : 0; }
        }

        /// <summary>픽업 순서 상 seq(1-base) 번째 다이의 셀 좌표. 순서를 넘어가면 순환.
        /// 순서를 못 구하면 false(호출측 폴백).</summary>
        public static bool TryGetCell(int seq, out int ix, out int iy)
        {
            ix = 0; iy = 0;
            if (seq <= 0) return false;
            var order = Ensure();
            if (order == null || order.Count == 0) return false;
            int idx = (seq - 1) % order.Count;
            ix = order[idx][0];
            iy = order[idx][1];
            return true;
        }

        /// <summary>활성 레시피 기준 순서 목록(캐시). 레시피 지문이 바뀌면 재생성.</summary>
        private static List<int[]> Ensure()
        {
            try
            {
                var r = QMC.Vision.Core.ActiveRecipeContext.Current;
                string key = BuildKey(r);
                lock (_lock)
                {
                    if (_order != null && key == _key) return _order;
                    _key = key;
                    _order = Build(r);
                    return _order;
                }
            }
            catch { lock (_lock) return _order; }
        }

        /// <summary>레시피 지문 — InputDieMap(있으면) 또는 웨이퍼 사양 + Pickup 옵션.</summary>
        private static string BuildKey(VisionMachineRecipe r)
        {
            if (r == null) return "";
            string map = r.InputDieMap != null && r.InputDieMap.Entries != null
                ? "M" + r.InputDieMap.Entries.Count + "_" + r.InputDieMap.CreatedAt.Ticks
                : "G" + r.WaferPitchX + "_" + r.WaferPitchY
                  + "_" + r.WaferOuterDiameterMm
                  + "_" + r.WaferDieSizeX + "_" + r.WaferDieSizeY
                  + "_" + (r.WaferEdgeSkipMode ?? "Grid")
                  + "_" + r.WaferSideEdgeSkip + "_" + r.WaferTopBottomEdgeSkip
                  + "_" + r.WaferSideEdgeSkipMm + "_" + r.WaferTopBottomEdgeSkipMm;
            var p = r.Pickup ?? new PickupSubset();
            return map + "|" + (int)p.StartCorner + (int)p.Direction + (int)p.Pattern;
        }

        private static List<int[]> Build(VisionMachineRecipe r)
        {
            var list = new List<int[]>();
            if (r == null) return list;
            DieMap map = (r.InputDieMap != null && r.InputDieMap.Entries != null && r.InputDieMap.Entries.Count > 0)
                ? r.InputDieMap
                : DieMapBuilder.GenerateWaferSpecMap(r, "WAFER");   // 핸들러 DieMapGenerator 동일 기하(2026-07-06)
            var ordered = PickupSequenceGenerator.Build(map, r.Pickup);
            foreach (var e in ordered)
                if (e != null) list.Add(new[] { e.DieMapX, e.DieMapY });
            return list;
        }
    }
}
