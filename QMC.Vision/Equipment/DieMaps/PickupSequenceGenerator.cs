using System.Collections.Generic;
using System.Linq;

namespace QMC.Vision.DieMaps
{
    /// <summary>
    /// 픽업 순서 생성기(핸들러 QMC.CDT320.DieMaps.PickupSequenceGenerator 이식).
    /// PickupSubset 옵션(시작 코너 + 가로/세로 + 직선/지그재그)에 따라
    /// DieMap 의 활성 다이(IsTarget=true)를 픽업 순서대로 정렬한 목록을 반환한다.
    /// </summary>
    public static class PickupSequenceGenerator
    {
        /// <summary>DieMap 의 활성 entries 를 옵션에 따라 정렬해서 반환.</summary>
        public static List<DieMapEntry> Build(DieMap map, PickupSubset options)
        {
            var result = new List<DieMapEntry>();
            if (map == null || map.Entries == null || map.Entries.Count == 0) return result;
            if (options == null) options = new PickupSubset();

            int gx = map.DieMapX;
            int gy = map.DieMapY;
            if (gx <= 0 || gy <= 0) return result;

            var cell = new DieMapEntry[gx, gy];
            foreach (var e in map.Entries)
            {
                if (e != null && e.DieMapX >= 0 && e.DieMapX < gx && e.DieMapY >= 0 && e.DieMapY < gy)
                    cell[e.DieMapX, e.DieMapY] = e;
            }

            var targets = map.Entries.Where(e => e != null && e.IsTarget).ToList();
            if (targets.Count == 0)
                return result;

            bool startRight = options.StartCorner == PickupStartCorner.TopRight ||
                              options.StartCorner == PickupStartCorner.BottomRight;
            bool startBottom = options.StartCorner == PickupStartCorner.BottomLeft ||
                               options.StartCorner == PickupStartCorner.BottomRight;

            List<int> rows = BuildOrderedGridIndexes(targets, false, startBottom);
            List<int> cols = BuildOrderedGridIndexes(targets, true, startRight);
            if (cols.Count == 0 || rows.Count == 0)
                return result;

            bool zigzag = options.Pattern == PickupPattern.ZigZag;

            if (options.Direction == PickupDirection.Horizontal)
            {
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
    }
}
