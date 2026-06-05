using System.Collections.Generic;
using QMC.CDT320.Recipes;

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

            int gx = map.GridX;
            int gy = map.GridY;
            if (gx <= 0 || gy <= 0) return result;

            // 빠른 셀 조회를 위해 grid → entry 인덱스
            // (DieMap.GetCell 은 linear search 라 작은 그리드 외엔 비효율)
            var cell = new DieMapEntry[gx, gy];
            foreach (var e in map.Entries)
            {
                if (e.GridX >= 0 && e.GridX < gx && e.GridY >= 0 && e.GridY < gy)
                    cell[e.GridX, e.GridY] = e;
            }

            // 시작 코너에 따른 outer/inner 초기값/증가 부호
            //   TopLeft     : col 0  →  gx-1,  row 0  →  gy-1
            //   TopRight    : col gx-1 → 0,    row 0  →  gy-1
            //   BottomLeft  : col 0  →  gx-1,  row gy-1 → 0
            //   BottomRight : col gx-1 → 0,    row gy-1 → 0
            bool startRight  = options.StartCorner == PickupStartCorner.TopRight    ||
                               options.StartCorner == PickupStartCorner.BottomRight;
            bool startBottom = options.StartCorner == PickupStartCorner.BottomLeft  ||
                               options.StartCorner == PickupStartCorner.BottomRight;

            int colStart = startRight  ? gx - 1 : 0;
            int colEnd   = startRight  ? -1     : gx;   // exclusive 종점
            int colStep  = startRight  ? -1     : +1;

            int rowStart = startBottom ? gy - 1 : 0;
            int rowEnd   = startBottom ? -1     : gy;
            int rowStep  = startBottom ? -1     : +1;

            bool zigzag = options.Pattern == PickupPattern.ZigZag;

            if (options.Direction == PickupDirection.Horizontal)
            {
                // outer = row, inner = col
                int innerDir = colStep;
                int innerStart = colStart;
                int innerEnd   = colEnd;

                for (int r = rowStart; r != rowEnd; r += rowStep)
                {
                    for (int c = innerStart; c != innerEnd; c += innerDir)
                    {
                        var e = cell[c, r];
                        if (e != null && e.IsTarget) result.Add(e);
                    }
                    if (zigzag)
                    {
                        // 다음 row 는 반대 방향으로
                        innerDir   = -innerDir;
                        int tmp    = innerStart;
                        innerStart = (innerEnd == gx ? gx - 1 : 0);  // 양 끝값 토글
                        // 위 단순 토글 대신 명확히:
                        if (innerDir > 0) { innerStart = 0;        innerEnd = gx; }
                        else              { innerStart = gx - 1;   innerEnd = -1; }
                    }
                }
            }
            else // Vertical
            {
                // outer = col, inner = row
                int innerDir = rowStep;
                int innerStart = rowStart;
                int innerEnd   = rowEnd;

                for (int c = colStart; c != colEnd; c += colStep)
                {
                    for (int r = innerStart; r != innerEnd; r += innerDir)
                    {
                        var e = cell[c, r];
                        if (e != null && e.IsTarget) result.Add(e);
                    }
                    if (zigzag)
                    {
                        innerDir = -innerDir;
                        if (innerDir > 0) { innerStart = 0;        innerEnd = gy; }
                        else              { innerStart = gy - 1;   innerEnd = -1; }
                    }
                }
            }

            return result;
        }
    }
}
