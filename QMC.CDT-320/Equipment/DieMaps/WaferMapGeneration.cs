using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QMC.CDT320.DieMaps
{
    /// <summary>독립 미리보기의 mm 입력. 모든 길이는 정수 µm로 정확히 표현되어야 한다.</summary>
    public sealed class WaferMapGenerationSettings
    {
        public decimal OuterDiameterMm { get; }
        public decimal DieSizeXMm { get; }
        public decimal DieSizeYMm { get; }
        public decimal GapXMm { get; }
        public decimal GapYMm { get; }
        public decimal CenterStepXMm => checked(DieSizeXMm + GapXMm);
        public decimal CenterStepYMm => checked(DieSizeYMm + GapYMm);

        public WaferMapGenerationSettings(decimal outerDiameterMm, decimal dieSizeXMm,
            decimal dieSizeYMm, decimal gapXMm, decimal gapYMm)
        {
            OuterDiameterMm = outerDiameterMm;
            DieSizeXMm = dieSizeXMm;
            DieSizeYMm = dieSizeYMm;
            GapXMm = gapXMm;
            GapYMm = gapYMm;
        }
    }

    /// <summary>원본 raw 주소와 웨이퍼 중심 기준 미리보기 좌표. 장비 좌표가 아니다.</summary>
    public sealed class GeneratedWaferDie
    {
        public int RawColumn { get; }
        public int RawRow { get; }
        public decimal CenterXMm { get; }
        public decimal CenterYMm { get; }

        internal GeneratedWaferDie(int rawColumn, int rawRow, decimal centerXMm, decimal centerYMm)
        {
            RawColumn = rawColumn;
            RawRow = rawRow;
            CenterXMm = centerXMm;
            CenterYMm = centerYMm;
        }
    }

    /// <summary>상단(+Y), 하단(-Y), 좌측(-X), 우측(+X)의 원본 끝줄에 남길 다이 개수.</summary>
    public sealed class WaferMapEdgeCounts
    {
        public int Top { get; }
        public int Bottom { get; }
        public int Left { get; }
        public int Right { get; }

        public WaferMapEdgeCounts(int top, int bottom, int left, int right)
        {
            if (top < 0 || bottom < 0 || left < 0 || right < 0)
                throw new ArgumentOutOfRangeException("top/bottom/left/right", "외곽 다이 개수는 음수일 수 없습니다.");
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }
    }

    /// <summary>후보 격자와 유효 다이만 보관하는 불변 미리보기 결과.</summary>
    public sealed class GeneratedWaferMap
    {
        private readonly ReadOnlyCollection<GeneratedWaferDie> _dies;
        private readonly GeneratedWaferMap _baseMap;

        public WaferMapGenerationSettings Settings { get; }
        public int CandidateColumns { get; }
        public int CandidateRows { get; }
        public int UsedColumns { get; }
        public int UsedRows { get; }
        public int MinColumn { get; }
        public int MaxColumn { get; }
        public int MinRow { get; }
        public int MaxRow { get; }
        public IReadOnlyList<GeneratedWaferDie> Dies => _dies;
        public int Count => _dies.Count;
        public decimal BoundaryRadiusMm { get; }
        public WaferMapEdgeCounts EdgeCounts { get; }
        public bool IsAdjusted => _baseMap != null;
        public GeneratedWaferMap BaseMap => _baseMap ?? this;

        internal GeneratedWaferMap(WaferMapGenerationSettings settings, int candidateColumns,
            int candidateRows, decimal boundaryRadiusMm, IEnumerable<GeneratedWaferDie> dies,
            GeneratedWaferMap baseMap = null)
        {
            Settings = settings;
            CandidateColumns = candidateColumns;
            CandidateRows = candidateRows;
            BoundaryRadiusMm = boundaryRadiusMm;
            _baseMap = baseMap;
            // 호출자의 List를 보관하지 않고, 요소도 불변인 독립 컬렉션으로 공개한다.
            _dies = new List<GeneratedWaferDie>(dies).AsReadOnly();
            if (_dies.Count == 0)
            {
                MinColumn = MaxColumn = MinRow = MaxRow = -1;
                UsedColumns = UsedRows = 0;
                EdgeCounts = new WaferMapEdgeCounts(0, 0, 0, 0);
                return;
            }

            MinColumn = _dies.Min(die => die.RawColumn);
            MaxColumn = _dies.Max(die => die.RawColumn);
            MinRow = _dies.Min(die => die.RawRow);
            MaxRow = _dies.Max(die => die.RawRow);
            UsedColumns = MaxColumn - MinColumn + 1;
            UsedRows = MaxRow - MinRow + 1;
            EdgeCounts = new WaferMapEdgeCounts(
                _dies.Count(die => die.RawRow == MaxRow),
                _dies.Count(die => die.RawRow == MinRow),
                _dies.Count(die => die.RawColumn == MinColumn),
                _dies.Count(die => die.RawColumn == MaxColumn));
        }
    }

    /// <summary>
    /// 원본 D:\Source\ASE_CDT300_BinMap\BinMapCreateTest\BINMAP CREATE VIEWER\Form\FormMain.cpp
    /// CreatedBinMap의 MapCountEnable=false, Zigzag=false 분기를 재현한다.
    /// 공개 입력/출력은 mm이며, 내부 정수 µm 계산은 원본 /2 절사를 보존하기 위한 것이다.
    /// 좌표는 원본 +Y 위 방향의 미리보기 상대 좌표이며 레시피·장비 좌표 변환을 수행하지 않는다.
    /// </summary>
    public static class WaferMapGeneration
    {
        private const long MaximumCandidateCells = 1000000L;
        private const decimal MaximumLengthMm = 2147483.647m;

        public static GeneratedWaferMap Generate(WaferMapGenerationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // 미설정 값으로도 입력창을 열 수 있도록 설정 객체는 값만 보관하고 생성 시 검증한다.
            CheckLength(settings.OuterDiameterMm, nameof(settings.OuterDiameterMm), "웨이퍼 외경", false);
            CheckLength(settings.DieSizeXMm, nameof(settings.DieSizeXMm), "다이 X 크기", false);
            CheckLength(settings.DieSizeYMm, nameof(settings.DieSizeYMm), "다이 Y 크기", false);
            CheckLength(settings.GapXMm, nameof(settings.GapXMm), "다이 X 간격", true);
            CheckLength(settings.GapYMm, nameof(settings.GapYMm), "다이 Y 간격", true);
            CheckLength(settings.CenterStepXMm, nameof(settings.CenterStepXMm), "다이 X 중심 간격(다이+간격)", false);
            CheckLength(settings.CenterStepYMm, nameof(settings.CenterStepYMm), "다이 Y 중심 간격(다이+간격)", false);

            long diameter = checked((long)(settings.OuterDiameterMm * 1000m));
            long stepX = checked((long)(settings.CenterStepXMm * 1000m));
            long stepY = checked((long)(settings.CenterStepYMm * 1000m));
            long candidateColumns = checked(diameter / stepX + 1L);
            long candidateRows = checked(diameter / stepY + 1L);
            if (candidateColumns > MaximumCandidateCells || candidateRows > MaximumCandidateCells ||
                checked(candidateColumns * candidateRows) > MaximumCandidateCells)
                throw new InvalidOperationException("후보 격자가 너무 큽니다. 최대 후보 셀 수는 1,000,000개입니다.");

            int columns = checked((int)candidateColumns);
            int rows = checked((int)candidateRows);
            long firstX = -(checked(candidateColumns * stepX) / 2L);
            long firstY = -(checked(candidateRows * stepY) / 2L);
            long halfX = stepX / 2L;
            long halfY = stepY / 2L;
            long radius = checked(diameter / 2L + 200L);
            long radiusSquared = checked(radius * radius);
            var dies = new List<GeneratedWaferDie>();
            for (int row = 0; row < rows; row++)
            {
                long y = checked(firstY + row * stepY);
                long farY = checked(Math.Abs(y) + halfY);
                for (int column = 0; column < columns; column++)
                {
                    long x = checked(firstX + column * stepX);
                    long farX = checked(Math.Abs(x) + halfX);
                    // 원본 non-Zigzag는 경계에 닿은 셀도 제외한다. 먼저 축별로 제외하여
                    // 큰 입력의 거리 제곱이 오버플로하는 경우에도 외곽 셀이 포함되지 않게 한다.
                    if (farX >= radius || farY >= radius)
                        continue;
                    long distanceSquared = checked(farX * farX + farY * farY);
                    if (distanceSquared >= radiusSquared)
                        continue;
                    dies.Add(new GeneratedWaferDie(column, row, x / 1000m, y / 1000m));
                }
            }
            return new GeneratedWaferMap(settings, columns, rows, radius / 1000m, dies);
        }

        /// <summary>
        /// 항상 원본 결과의 네 끝줄에서만 삭제한다. 내부 셀·주소·좌표는 유지하며 보정은 누적하지 않는다.
        /// 각 끝줄은 중심에 가까운 연속 N개를 남기고 동거리이면 작은 raw 주소를 먼저 선택한다.
        /// </summary>
        public static GeneratedWaferMap ApplyEdgeCounts(GeneratedWaferMap map, WaferMapEdgeCounts counts)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (counts == null)
                throw new ArgumentNullException(nameof(counts));
            GeneratedWaferMap baseMap = map.BaseMap;
            if (baseMap.Count == 0)
                throw new InvalidOperationException("다이가 없는 맵에는 외곽 개수 보정을 적용할 수 없습니다.");
            CheckEdgeCount(counts.Top, baseMap.EdgeCounts.Top, "상단");
            CheckEdgeCount(counts.Bottom, baseMap.EdgeCounts.Bottom, "하단");
            CheckEdgeCount(counts.Left, baseMap.EdgeCounts.Left, "좌측");
            CheckEdgeCount(counts.Right, baseMap.EdgeCounts.Right, "우측");

            HashSet<GeneratedWaferDie> top = SelectEdgeDies(baseMap.Dies.Where(die => die.RawRow == baseMap.MaxRow), counts.Top, true);
            HashSet<GeneratedWaferDie> bottom = SelectEdgeDies(baseMap.Dies.Where(die => die.RawRow == baseMap.MinRow), counts.Bottom, true);
            HashSet<GeneratedWaferDie> left = SelectEdgeDies(baseMap.Dies.Where(die => die.RawColumn == baseMap.MinColumn), counts.Left, false);
            HashSet<GeneratedWaferDie> right = SelectEdgeDies(baseMap.Dies.Where(die => die.RawColumn == baseMap.MaxColumn), counts.Right, false);
            var dies = new List<GeneratedWaferDie>();
            foreach (GeneratedWaferDie die in baseMap.Dies)
            {
                if (die.RawRow == baseMap.MaxRow && !top.Contains(die))
                    continue;
                if (die.RawRow == baseMap.MinRow && !bottom.Contains(die))
                    continue;
                if (die.RawColumn == baseMap.MinColumn && !left.Contains(die))
                    continue;
                if (die.RawColumn == baseMap.MaxColumn && !right.Contains(die))
                    continue;
                dies.Add(die);
            }

            int actualTop = dies.Count(die => die.RawRow == baseMap.MaxRow);
            int actualBottom = dies.Count(die => die.RawRow == baseMap.MinRow);
            int actualLeft = dies.Count(die => die.RawColumn == baseMap.MinColumn);
            int actualRight = dies.Count(die => die.RawColumn == baseMap.MaxColumn);
            if (actualTop != counts.Top || actualBottom != counts.Bottom ||
                actualLeft != counts.Left || actualRight != counts.Right)
                throw new InvalidOperationException("외곽 줄이 서로 겹쳐 요청한 개수를 동시에 유지할 수 없습니다. " +
                    "상/하/좌/우 요청=" + counts.Top + "/" + counts.Bottom + "/" + counts.Left + "/" + counts.Right +
                    ", 결과=" + actualTop + "/" + actualBottom + "/" + actualLeft + "/" + actualRight +
                    ". 모서리 또는 단일 행·열의 개수를 함께 확인하십시오.");
            if (dies.Count == baseMap.Count)
                return baseMap;
            return new GeneratedWaferMap(baseMap.Settings, baseMap.CandidateColumns, baseMap.CandidateRows,
                baseMap.BoundaryRadiusMm, dies, baseMap);
        }

        private static HashSet<GeneratedWaferDie> SelectEdgeDies(IEnumerable<GeneratedWaferDie> dies,
            int count, bool horizontal)
        {
            return new HashSet<GeneratedWaferDie>(dies
                .OrderBy(die => Math.Abs(horizontal ? die.CenterXMm : die.CenterYMm))
                .ThenBy(die => horizontal ? die.RawColumn : die.RawRow)
                .Take(count));
        }

        private static void CheckEdgeCount(int count, int maximum, string label)
        {
            if (count < 1 || count > maximum)
                throw new ArgumentOutOfRangeException(nameof(count), label + " 외곽 개수는 1 이상 " + maximum + " 이하이어야 합니다.");
        }

        private static void CheckLength(decimal value, string parameterName, string label, bool allowZero)
        {
            if (value < 0m || (!allowZero && value == 0m) || value > MaximumLengthMm)
                throw new ArgumentOutOfRangeException(parameterName, label + "는 " +
                    (allowZero ? "0 이상" : "0 초과") + " 2,147,483.647 mm 이하이어야 합니다.");
            decimal micrometers = value * 1000m;
            if (decimal.Truncate(micrometers) != micrometers)
                throw new ArgumentException(label + "는 0.001 mm 단위로 입력하십시오.", parameterName);
        }
    }
}
