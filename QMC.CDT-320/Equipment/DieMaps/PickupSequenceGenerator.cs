using System.Collections.Generic;
using QMC.CDT320.Recipes;
using System.Linq;

namespace QMC.CDT320.DieMaps
{
    /// <summary>
    /// Stage 61 — 픽업 순서 생성기.
    /// PickupSubset 옵션 (시작 코너 + 가로/세로 + 직선/지그재그) 에 따라
    /// DieMap 의 활성 다이 (IsTarget=true) 들을 픽업 순서대로 정렬한 목록을 반환한다.
    ///
    /// 픽업 단위: 4 picker per cycle 유지. 본 generator 는 한 다이씩 순서만 결정.
    /// MachineController 가 dieBase ~ dieBase+3 으로 4개씩 슬라이스 해서 사용.
    /// </summary>
    public static class PickupSequenceGenerator
    {
        /// <summary>
        /// DieMap 의 활성 entries 를 옵션에 따라 정렬해서 반환.
        /// </summary>
        public static List<DieMapEntry> Build(DieMap map, PickupSubset options)
        {
            var result = new List<DieMapEntry>();
            if (map == null || map.Entries == null || map.Entries.Count == 0) return result;
            if (options == null) options = new PickupSubset();

            int gx = map.DieMapX;
            int gy = map.DieMapY;
            if (gx <= 0 || gy <= 0) return result;

            // 빠른 셀 조회를 위해 grid → entry 인덱스
            // (DieMap.GetCell 은 linear search 라 작은 그리드 외엔 비효율)
            var cell = new DieMapEntry[gx, gy];
            foreach (var e in map.Entries)
            {
                if (e.DieMapX >= 0 && e.DieMapX < gx && e.DieMapY >= 0 && e.DieMapY < gy)
                    cell[e.DieMapX, e.DieMapY] = e;
            }

            var targets = map.Entries.Where(e => e != null && e.IsTarget).ToList();
            if (targets.Count == 0)
                return result;

            // Top/Bottom/Left/Right 는 화면에 표시되는 DieMapX/Y 기준이다.
            // 원형 맵에서는 전체 격자 외곽이 아니라 활성 target 영역의 코너에서 시작해야 한다.
            bool startRight  = options.StartCorner == PickupStartCorner.TopRight    ||
                               options.StartCorner == PickupStartCorner.BottomRight;
            bool startBottom = options.StartCorner == PickupStartCorner.BottomLeft  ||
                               options.StartCorner == PickupStartCorner.BottomRight;

            List<int> rows = BuildOrderedGridIndexes(targets, false, startBottom);
            List<int> cols = BuildOrderedGridIndexes(targets, true, startRight);
            if (cols.Count == 0 || rows.Count == 0)
                return result;

            bool zigzag = options.Pattern == PickupPattern.ZigZag;

            if (options.Direction == PickupDirection.Horizontal)
            {
                // outer = row, inner = col
                bool innerForward = true;

                foreach (int r in rows)
                {
                    bool addedInLine = false;
                    List<int> innerCols = innerForward ? cols : ReverseCopy(cols);
                    foreach (int c in innerCols)
                    {
                        var e = cell[c, r];
                        if (e != null && e.IsTarget)
                        {
                            result.Add(e);
                            addedInLine = true;
                        }
                    }
                    if (zigzag && addedInLine) innerForward = !innerForward;
                }
            }
            else // Vertical
            {
                // outer = col, inner = row
                bool innerForward = true;

                foreach (int c in cols)
                {
                    bool addedInLine = false;
                    List<int> innerRows = innerForward ? rows : ReverseCopy(rows);
                    foreach (int r in innerRows)
                    {
                        var e = cell[c, r];
                        if (e != null && e.IsTarget)
                        {
                            result.Add(e);
                            addedInLine = true;
                        }
                    }
                    if (zigzag && addedInLine) innerForward = !innerForward;
                }
            }

            return result;
        }

        private static List<int> BuildOrderedGridIndexes(IEnumerable<DieMapEntry> entries, bool xAxis, bool descending)
        {
            var indexes = entries
                .Where(e => e != null)
                .Select(e => xAxis ? e.DieMapX : e.DieMapY)
                .Distinct();

            if (descending)
                return indexes.OrderByDescending(i => i).ToList();

            return indexes.OrderBy(i => i).ToList();
        }

        private static List<int> ReverseCopy(List<int> source)
        {
            var copy = new List<int>(source);
            copy.Reverse();
            return copy;
        }

        /// <summary>PickupSubset 옵션 기준으로 활성 다이에 1-base 순번을 부여한다.</summary>
        public static DieMap ApplySequenceNumbers(DieMap map, PickupSubset options)
        {
            if (map == null)
                return null;

            if (map.Entries != null)
            {
                foreach (var entry in map.Entries)
                {
                    if (entry != null)
                        entry.SequenceNo = 0;
                }
            }

            List<DieMapEntry> ordered = Build(map, options);
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] != null)
                    ordered[i].SequenceNo = i + 1;
            }

            return map;
        }
    }
}
