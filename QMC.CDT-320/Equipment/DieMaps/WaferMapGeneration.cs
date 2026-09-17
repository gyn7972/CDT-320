using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QMC.CDT320.DieMaps
{
    /// <summary>독립 미리보기의 mm 입력. 모든 길이는 정수 µm로 정확히 표현되어야 한다.</summary>
    public sealed class WaferMapGenerationSettings
    {
        public int GenerationVersion { get; }
        public decimal EdgeMarginMm { get; }
        public decimal OuterDiameterMm { get; }
        public decimal DieSizeXMm { get; }
        public decimal DieSizeYMm { get; }
        public decimal GapXMm { get; }
        public decimal GapYMm { get; }
        public decimal CenterStepXMm => checked(DieSizeXMm + GapXMm);
        public decimal CenterStepYMm => checked(DieSizeYMm + GapYMm);

        public WaferMapGenerationSettings(decimal outerDiameterMm, decimal dieSizeXMm,
            decimal dieSizeYMm, decimal gapXMm, decimal gapYMm)
            : this(outerDiameterMm, dieSizeXMm, dieSizeYMm, gapXMm, gapYMm, 0m, 1)
        {
        }

        /// <summary>V2: 실제 다이 네 모서리와 안쪽 여백을 사용한다.</summary>
        public WaferMapGenerationSettings(decimal outerDiameterMm, decimal dieSizeXMm,
            decimal dieSizeYMm, decimal gapXMm, decimal gapYMm, decimal edgeMarginMm)
            : this(outerDiameterMm, dieSizeXMm, dieSizeYMm, gapXMm, gapYMm, edgeMarginMm, 2)
        {
        }

        /// <summary>버전 지정 복원/생성. V3부터 원형 윤곽을 사용하고 V4는 중심/반 피치도 비교한다.</summary>
        public WaferMapGenerationSettings(decimal outerDiameterMm, decimal dieSizeXMm,
            decimal dieSizeYMm, decimal gapXMm, decimal gapYMm, decimal edgeMarginMm, int version)
        {
            if (version != 1 && version != 2 && version != 3 && version != 4)
                throw new ArgumentOutOfRangeException(nameof(version), "지원하는 맵 생성 버전은 1, 2, 3, 4입니다.");
            if (version == 1 && edgeMarginMm != 0m)
                throw new ArgumentException("이전 수식 버전 1에는 안쪽 여백을 지정할 수 없습니다.", nameof(edgeMarginMm));
            GenerationVersion = version;
            EdgeMarginMm = edgeMarginMm;
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
        public int Column { get; }
        public int Row { get; }
        public decimal CenterXMm { get; }
        public decimal CenterYMm { get; }

        internal GeneratedWaferDie(int rawColumn, int rawRow, decimal centerXMm, decimal centerYMm)
            : this(rawColumn, rawRow, rawColumn, rawRow, centerXMm, centerYMm)
        {
        }

        internal GeneratedWaferDie(int rawColumn, int rawRow, int column, int row,
            decimal centerXMm, decimal centerYMm)
        {
            RawColumn = rawColumn;
            RawRow = rawRow;
            Column = column;
            Row = row;
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
        private readonly GeneratedWaferMap _sourceBaseMap;

        public WaferMapGenerationSettings Settings { get; }
        public int RotationDegrees { get; }
        public decimal DisplayDieSizeXMm => IsQuarterTurn ? Settings.DieSizeYMm : Settings.DieSizeXMm;
        public decimal DisplayDieSizeYMm => IsQuarterTurn ? Settings.DieSizeXMm : Settings.DieSizeYMm;
        public decimal DisplayStepXMm => IsQuarterTurn ? Settings.CenterStepYMm : Settings.CenterStepXMm;
        public decimal DisplayStepYMm => IsQuarterTurn ? Settings.CenterStepXMm : Settings.CenterStepYMm;
        private bool IsQuarterTurn => RotationDegrees == 90 || RotationDegrees == 270;
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
        public int OutOfBoundsCount { get; }
        public decimal RequiredOuterDiameterMm { get; }
        public int? RequestedTotalCount { get; }
        public WaferMapEdgeCounts RequestedEdgeCounts { get; }
        public WaferMapEdgeCounts EdgeCounts { get; }
        public bool IsAdjusted => _baseMap != null;
        public GeneratedWaferMap BaseMap => _baseMap ?? this;
        public GeneratedWaferMap SourceBaseMap => _sourceBaseMap ?? this;
        // 회전 전 raw (0,0)의 물리 원점이다. V4의 반 피치 선택은 0.0005 mm도 유지하며,
        // BaseMap/SourceBaseMap은 저장 정의만으로 다시 만들 수 있는 기본 AUTO로 남긴다.
        internal decimal SourceOriginXMm { get; }
        internal decimal SourceOriginYMm { get; }

        internal GeneratedWaferMap(WaferMapGenerationSettings settings, int candidateColumns,
            int candidateRows, decimal boundaryRadiusMm, IEnumerable<GeneratedWaferDie> dies,
            GeneratedWaferMap baseMap = null, int rotationDegrees = 0, GeneratedWaferMap sourceBaseMap = null,
            WaferMapEdgeCounts requestedEdgeCounts = null, int? requestedTotalCount = null,
            decimal? sourceOriginXMm = null, decimal? sourceOriginYMm = null)
        {
            Settings = settings;
            RotationDegrees = rotationDegrees;
            _sourceBaseMap = sourceBaseMap;
            CandidateColumns = candidateColumns;
            CandidateRows = candidateRows;
            BoundaryRadiusMm = boundaryRadiusMm;
            _baseMap = baseMap;
            RequestedEdgeCounts = requestedEdgeCounts;
            RequestedTotalCount = requestedTotalCount;
            GeneratedWaferMap originBase = baseMap ?? sourceBaseMap;
            SourceOriginXMm = sourceOriginXMm ?? (originBase != null ? originBase.SourceOriginXMm :
                -(checked((long)candidateColumns * (long)(settings.CenterStepXMm * 1000m)) / 2L) / 1000m);
            SourceOriginYMm = sourceOriginYMm ?? (originBase != null ? originBase.SourceOriginYMm :
                -(checked((long)candidateRows * (long)(settings.CenterStepYMm * 1000m)) / 2L) / 1000m);
            // 호출자의 List를 보관하지 않고, 요소도 불변인 독립 컬렉션으로 공개한다.
            _dies = new List<GeneratedWaferDie>(dies).AsReadOnly();
            OutOfBoundsCount = _dies.Count(die => !IsWithinBoundary(die));
            decimal requiredRadius = _dies.Count == 0 ? 0m :
                (decimal)Math.Sqrt((double)_dies.Max(GetFarCornerDistanceSquared));
            decimal requiredDiameter = settings.GenerationVersion >= 2
                ? 2m * (requiredRadius + settings.EdgeMarginMm)
                : Math.Max(0m, 2m * requiredRadius - 0.400m);
            // 필요한 외경은 실제 네 모서리와 안쪽 여백을 포함하며 0.001 mm 위쪽으로 표시한다.
            RequiredOuterDiameterMm = decimal.Ceiling(requiredDiameter * 1000m) / 1000m;
            if (_dies.Count == 0)
            {
                MinColumn = MaxColumn = MinRow = MaxRow = -1;
                UsedColumns = UsedRows = 0;
                EdgeCounts = new WaferMapEdgeCounts(0, 0, 0, 0);
                return;
            }

            MinColumn = _dies.Min(die => die.Column);
            MaxColumn = _dies.Max(die => die.Column);
            MinRow = _dies.Min(die => die.Row);
            MaxRow = _dies.Max(die => die.Row);
            UsedColumns = MaxColumn - MinColumn + 1;
            UsedRows = MaxRow - MinRow + 1;
            EdgeCounts = new WaferMapEdgeCounts(
                _dies.Count(die => die.Row == MaxRow),
                _dies.Count(die => die.Row == MinRow),
                _dies.Count(die => die.Column == MinColumn),
                _dies.Count(die => die.Column == MaxColumn));
        }

        public bool IsWithinBoundary(GeneratedWaferDie die)
        {
            if (die == null)
                throw new ArgumentNullException(nameof(die));
            decimal distanceSquared = GetFarCornerDistanceSquared(die);
            decimal radiusSquared = BoundaryRadiusMm * BoundaryRadiusMm;
            return Settings.GenerationVersion >= 2
                ? BoundaryRadiusMm >= 0m && distanceSquared <= radiusSquared
                : distanceSquared < radiusSquared;
        }

        private decimal GetFarCornerDistanceSquared(GeneratedWaferDie die)
        {
            // 이전 저장 맵은 정수 µm 피치 절반과 바깥쪽 0.2 mm의 원본 판정을 유지한다.
            decimal halfX = Settings.GenerationVersion >= 2 ? DisplayDieSizeXMm / 2m :
                decimal.Truncate(DisplayStepXMm * 1000m / 2m) / 1000m;
            decimal halfY = Settings.GenerationVersion >= 2 ? DisplayDieSizeYMm / 2m :
                decimal.Truncate(DisplayStepYMm * 1000m / 2m) / 1000m;
            decimal farX = Math.Abs(die.CenterXMm) + halfX;
            decimal farY = Math.Abs(die.CenterYMm) + halfY;
            return checked(farX * farX + farY * farY);
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

            if (settings.GenerationVersion >= 2)
            {
                CheckLength(settings.EdgeMarginMm, nameof(settings.EdgeMarginMm), "웨이퍼 안쪽 여백", true);
                if (settings.EdgeMarginMm > settings.OuterDiameterMm / 2m)
                    throw new ArgumentOutOfRangeException(nameof(settings.EdgeMarginMm), "안쪽 여백은 웨이퍼 반지름보다 클 수 없습니다.");
            }

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
            decimal usableRadius = settings.GenerationVersion >= 2
                ? settings.OuterDiameterMm / 2m - settings.EdgeMarginMm : radius / 1000m;
            decimal usableRadiusSquared = usableRadius * usableRadius;
            var dies = new List<GeneratedWaferDie>();
            for (int row = 0; row < rows; row++)
            {
                long y = checked(firstY + row * stepY);
                long farY = checked(Math.Abs(y) + halfY);
                for (int column = 0; column < columns; column++)
                {
                    long x = checked(firstX + column * stepX);
                    long farX = checked(Math.Abs(x) + halfX);
                    if (settings.GenerationVersion >= 2)
                    {
                        // 격자 위상과 raw 주소는 이전 계산을 유지한다. 판정만 실제 다이 크기와
                        // 안쪽 여백으로 계산하며, 홀수 µm 다이의 0.5 µm 절반도 버리지 않는다.
                        decimal bodyFarX = Math.Abs(x / 1000m) + settings.DieSizeXMm / 2m;
                        decimal bodyFarY = Math.Abs(y / 1000m) + settings.DieSizeYMm / 2m;
                        if (bodyFarX * bodyFarX + bodyFarY * bodyFarY <= usableRadiusSquared)
                            dies.Add(new GeneratedWaferDie(column, row, x / 1000m, y / 1000m));
                        continue;
                    }
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
            return new GeneratedWaferMap(settings, columns, rows, usableRadius, dies);
        }

        /// <summary>원본 0도 결과를 기준으로 시계방향 절대 회전한다. 기존 끝줄 보정은 초기화한다.</summary>
        public static GeneratedWaferMap Rotate(GeneratedWaferMap map, int degrees)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270)
                throw new ArgumentOutOfRangeException(nameof(degrees), "회전 각도는 0°, 90°, 180°, 270° 중에서 선택하십시오.");
            GeneratedWaferMap source = map.SourceBaseMap;
            if (degrees == 0)
                return source;
            var dies = new List<GeneratedWaferDie>(source.Count);
            foreach (GeneratedWaferDie die in source.Dies)
            {
                int column;
                int row;
                decimal x;
                decimal y;
                if (degrees == 90)
                {
                    column = die.RawRow;
                    row = source.CandidateColumns - 1 - die.RawColumn;
                    x = die.CenterYMm;
                    y = -die.CenterXMm;
                }
                else if (degrees == 180)
                {
                    column = source.CandidateColumns - 1 - die.RawColumn;
                    row = source.CandidateRows - 1 - die.RawRow;
                    x = -die.CenterXMm;
                    y = -die.CenterYMm;
                }
                else
                {
                    column = source.CandidateRows - 1 - die.RawRow;
                    row = die.RawColumn;
                    x = -die.CenterYMm;
                    y = die.CenterXMm;
                }
                dies.Add(new GeneratedWaferDie(die.RawColumn, die.RawRow, column, row, x, y));
            }
            bool swap = degrees == 90 || degrees == 270;
            return new GeneratedWaferMap(source.Settings,
                swap ? source.CandidateRows : source.CandidateColumns,
                swap ? source.CandidateColumns : source.CandidateRows,
                source.BoundaryRadiusMm, dies, null, degrees, source);
        }

        /// <summary>
        /// 항상 원본 결과의 네 끝줄에서만 삭제한다. 내부 셀·주소·좌표는 유지하며 보정은 누적하지 않는다.
        /// 각 끝줄은 중심에 가까운 연속 N개를 남기고 동거리이면 현재 화면의 작은 격자 주소를 먼저 선택한다.
        /// </summary>
        public static GeneratedWaferMap ApplyEdgeCounts(GeneratedWaferMap map, WaferMapEdgeCounts counts)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (counts == null)
                throw new ArgumentNullException(nameof(counts));
            GeneratedWaferMap baseMap = map.BaseMap;
            if (baseMap.Settings.GenerationVersion != 1)
                throw new InvalidOperationException("안쪽 여백 맵은 전체 개수와 네 끝줄을 함께 지정하는 개수 재생성을 사용하십시오.");
            if (baseMap.Count == 0)
                throw new InvalidOperationException("다이가 없는 맵에는 외곽 개수 보정을 적용할 수 없습니다.");
            CheckEdgeCount(counts.Top, baseMap.EdgeCounts.Top, "상단");
            CheckEdgeCount(counts.Bottom, baseMap.EdgeCounts.Bottom, "하단");
            CheckEdgeCount(counts.Left, baseMap.EdgeCounts.Left, "좌측");
            CheckEdgeCount(counts.Right, baseMap.EdgeCounts.Right, "우측");

            HashSet<GeneratedWaferDie> top = SelectEdgeDies(baseMap.Dies.Where(die => die.Row == baseMap.MaxRow), counts.Top, true);
            HashSet<GeneratedWaferDie> bottom = SelectEdgeDies(baseMap.Dies.Where(die => die.Row == baseMap.MinRow), counts.Bottom, true);
            HashSet<GeneratedWaferDie> left = SelectEdgeDies(baseMap.Dies.Where(die => die.Column == baseMap.MinColumn), counts.Left, false);
            HashSet<GeneratedWaferDie> right = SelectEdgeDies(baseMap.Dies.Where(die => die.Column == baseMap.MaxColumn), counts.Right, false);
            var dies = new List<GeneratedWaferDie>();
            foreach (GeneratedWaferDie die in baseMap.Dies)
            {
                if (die.Row == baseMap.MaxRow && !top.Contains(die))
                    continue;
                if (die.Row == baseMap.MinRow && !bottom.Contains(die))
                    continue;
                if (die.Column == baseMap.MinColumn && !left.Contains(die))
                    continue;
                if (die.Column == baseMap.MaxColumn && !right.Contains(die))
                    continue;
                dies.Add(die);
            }

            int actualTop = dies.Count(die => die.Row == baseMap.MaxRow);
            int actualBottom = dies.Count(die => die.Row == baseMap.MinRow);
            int actualLeft = dies.Count(die => die.Column == baseMap.MinColumn);
            int actualRight = dies.Count(die => die.Column == baseMap.MaxColumn);
            if (actualTop != counts.Top || actualBottom != counts.Bottom ||
                actualLeft != counts.Left || actualRight != counts.Right)
                throw new InvalidOperationException("외곽 줄이 서로 겹쳐 요청한 개수를 동시에 유지할 수 없습니다. " +
                    "상/하/좌/우 요청=" + counts.Top + "/" + counts.Bottom + "/" + counts.Left + "/" + counts.Right +
                    ", 결과=" + actualTop + "/" + actualBottom + "/" + actualLeft + "/" + actualRight +
                    ". 모서리 또는 단일 행·열의 개수를 함께 확인하십시오.");
            if (dies.Count == baseMap.Count)
                return baseMap;
            return new GeneratedWaferMap(baseMap.Settings, baseMap.CandidateColumns, baseMap.CandidateRows,
                baseMap.BoundaryRadiusMm, dies, baseMap, baseMap.RotationDegrees, baseMap.SourceBaseMap);
        }

        /// <summary>
        /// V2/V3/V4의 네 끝줄과 총수를 동시에 지정한다. 원본 위상과 주소를 유지한 채 필요한 격자만
        /// 확장하며, 원 밖 다이도 미리보기에 남긴다. 이전 보정 결과에는 누적하지 않는다.
        /// </summary>
        public static GeneratedWaferMap ApplyCounts(GeneratedWaferMap map, WaferMapEdgeCounts counts, int totalCount)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (counts == null)
                throw new ArgumentNullException(nameof(counts));
            GeneratedWaferMap baseMap = map.BaseMap;
            if (baseMap.Settings.GenerationVersion < 2)
                throw new InvalidOperationException("이전 수식 맵은 새 안쪽 여백 방식으로 자동 생성한 뒤 총수와 끝줄 개수를 변경하십시오.");
            CheckRequestedCount(totalCount, "전체 다이");
            CheckRequestedCount(counts.Top, "상단");
            CheckRequestedCount(counts.Bottom, "하단");
            CheckRequestedCount(counts.Left, "좌측");
            CheckRequestedCount(counts.Right, "우측");
            if (totalCount < Math.Max(Math.Max(counts.Top, counts.Bottom), Math.Max(counts.Left, counts.Right)))
                throw new InvalidOperationException("전체 개수는 각 끝줄의 요청 개수보다 작을 수 없습니다.");
            if (totalCount == baseMap.Count && SameEdgeCounts(counts, baseMap.EdgeCounts))
                return baseMap;
            if (baseMap.Settings.GenerationVersion >= 3)
            {
                GeneratedWaferMap radial = TryCreateRadialCounts(baseMap, counts, totalCount);
                if (radial != null)
                    return radial;
                if (baseMap.Settings.GenerationVersion == 4)
                {
                    radial = TryCreatePhaseCounts(baseMap, counts, totalCount);
                    if (radial != null)
                        return radial;
                }
            }

            int minColumn = baseMap.Count == 0 ? 0 : baseMap.MinColumn;
            int maxColumn = baseMap.Count == 0 ? baseMap.CandidateColumns - 1 : baseMap.MaxColumn;
            int minRow = baseMap.Count == 0 ? 0 : baseMap.MinRow;
            int maxRow = baseMap.Count == 0 ? baseMap.CandidateRows - 1 : baseMap.MaxRow;
            if (baseMap.Count == 0)
            {
                // 원 안에 들어가는 다이가 없어도 중심에 가장 가까운 원본 격자 한 칸부터
                // 요청 미리보기를 만들 수 있다. 후보 사각형 전체를 끝줄로 강제하지 않는다.
                minColumn = maxColumn = GetCenteredStart(baseMap, minColumn, maxColumn, 1, true);
                minRow = maxRow = GetCenteredStart(baseMap, minRow, maxRow, 1, false);
            }
            int originalWidth = maxColumn - minColumn + 1;
            int originalHeight = maxRow - minRow + 1;
            int neededColumns = Math.Max(counts.Top, counts.Bottom);
            int neededRows = Math.Max(counts.Left, counts.Right);
            long initialWidth = neededColumns > originalWidth ? neededColumns + 2L : originalWidth;
            long initialHeight = neededRows > originalHeight ? neededRows + 2L : originalHeight;
            if (initialWidth * initialHeight > MaximumCandidateCells)
                throw new InvalidOperationException("확장 격자가 너무 큽니다. 최대 후보 셀 수는 1,000,000개입니다.");
            // 끝줄이 기존 폭을 넘으면 양 옆 연결 셀까지 확보한다. 기존 bounds 안의 raw 주소는
            // 이동시키지 않고, 물리 중심에 가까운 방향부터 바깥 격자를 한 칸씩 추가한다.
            if (neededColumns > maxColumn - minColumn + 1)
                ExpandToSize(baseMap, ref minColumn, ref maxColumn, checked(neededColumns + 2), true);
            if (neededRows > maxRow - minRow + 1)
                ExpandToSize(baseMap, ref minRow, ref maxRow, checked(neededRows + 2), false);
            CheckCandidateBounds(minColumn, maxColumn, minRow, maxRow);

            // 고정 끝줄을 제외한 내부가 부족할 때만 bounds를 확장한다. 각 축의 실제 mm 폭을
            // 비교하므로 직사각 다이도 물리적으로 원에 가까운 방향으로 확장한다.
            bool expanded = maxColumn - minColumn + 1 != originalWidth || maxRow - minRow + 1 != originalHeight;
            while (true)
            {
                bool compatible;
                long capacity = GetMaximumCount(baseMap, minColumn, maxColumn, minRow, maxRow, counts, out compatible);
                if (capacity >= totalCount && compatible)
                    break;
                if (capacity >= totalCount && !expanded)
                    throw new InvalidOperationException("끝줄이 모서리 또는 단일 행·열에서 겹쳐 요청 개수를 동시에 만들 수 없습니다. " +
                        "상/하/좌/우 개수와 전체 개수를 함께 확인하십시오.");
                bool horizontal = (maxColumn - minColumn + 1) * baseMap.DisplayStepXMm <=
                    (maxRow - minRow + 1) * baseMap.DisplayStepYMm;
                if (horizontal)
                    ExpandOne(baseMap, ref minColumn, ref maxColumn, true);
                else
                    ExpandOne(baseMap, ref minRow, ref maxRow, false);
                CheckCandidateBounds(minColumn, maxColumn, minRow, maxRow);
                expanded = true;
            }

            int topStart = GetCenteredStart(baseMap, minColumn, maxColumn, counts.Top, true);
            int bottomStart = GetCenteredStart(baseMap, minColumn, maxColumn, counts.Bottom, true);
            int leftStart = GetCenteredStart(baseMap, minRow, maxRow, counts.Left, false);
            int rightStart = GetCenteredStart(baseMap, minRow, maxRow, counts.Right, false);
            int centerColumn = GetCenteredStart(baseMap, minColumn, maxColumn, 1, true);
            int centerRow = GetCenteredStart(baseMap, minRow, maxRow, 1, false);
            var required = new List<GeneratedWaferDie>();
            var interior = new List<GeneratedWaferDie>();
            for (int row = minRow; row <= maxRow; row++)
            {
                for (int column = minColumn; column <= maxColumn; column++)
                {
                    bool inTop = column >= topStart && column < topStart + counts.Top;
                    bool inBottom = column >= bottomStart && column < bottomStart + counts.Bottom;
                    bool inLeft = row >= leftStart && row < leftStart + counts.Left;
                    bool inRight = row >= rightStart && row < rightStart + counts.Right;
                    bool requested = (row == maxRow && inTop) || (row == minRow && inBottom) ||
                        (column == minColumn && inLeft) || (column == maxColumn && inRight);
                    bool forbidden = (row == maxRow && !inTop) || (row == minRow && !inBottom) ||
                        (column == minColumn && !inLeft) || (column == maxColumn && !inRight);
                    if (requested && forbidden)
                        throw new InvalidOperationException("끝줄이 모서리 또는 단일 행·열에서 겹쳐 요청 개수를 동시에 만들 수 없습니다. " +
                            "상/하/좌/우 개수와 전체 개수를 함께 확인하십시오.");
                    if (forbidden)
                        continue;
                    GeneratedWaferDie die = CreateAtGrid(baseMap, column, row);
                    // 네 끝줄에서 중앙까지 연속 구간을 확보한다. 이 연결 영역과 중앙부터 채우는
                    // 원형 영역의 합은 행·열에 빈 구멍이나 고립된 끝줄 다이를 만들지 않는다.
                    bool connection = (row >= centerRow && inTop) || (row <= centerRow && inBottom) ||
                        (column <= centerColumn && inLeft) || (column >= centerColumn && inRight);
                    if (connection || requested)
                        required.Add(die);
                    else
                        interior.Add(die);
                }
            }
            if (required.Count > totalCount)
                throw new InvalidOperationException("요청한 끝줄을 중심까지 연속으로 연결하려면 전체 개수가 최소 " +
                    required.Count + "개 필요합니다. 현재 전체 요청은 " + totalCount + "개입니다.");
            if (required.Count + interior.Count < totalCount)
                throw new InvalidOperationException("현재 끝줄 경계를 유지하면서 요청한 전체 개수를 채울 수 없습니다. " +
                    "끝줄 개수와 전체 개수를 함께 확인하십시오.");
            required.AddRange(interior.OrderBy(die => baseMap.IsWithinBoundary(die) ? 0 : 1)
                .ThenBy(die => die.CenterXMm * die.CenterXMm + die.CenterYMm * die.CenterYMm)
                .ThenBy(die => die.Row).ThenBy(die => die.Column).Take(totalCount - required.Count));
            var result = new GeneratedWaferMap(baseMap.Settings, baseMap.CandidateColumns, baseMap.CandidateRows,
                baseMap.BoundaryRadiusMm, required.OrderBy(die => die.Row).ThenBy(die => die.Column),
                baseMap, baseMap.RotationDegrees, baseMap.SourceBaseMap, counts, totalCount);
            if (result.Count != totalCount || !SameEdgeCounts(result.EdgeCounts, counts))
                throw new InvalidOperationException("요청한 전체/상/하/좌/우 개수가 생성 결과와 일치하지 않습니다. 요청값을 적용하지 않았습니다.");
            return result;
        }

        private static GeneratedWaferMap TryCreateRadialCounts(GeneratedWaferMap baseMap,
            WaferMapEdgeCounts counts, int totalCount, decimal? sourceOriginXMm = null, decimal? sourceOriginYMm = null)
        {
            long capacity = (long)baseMap.CandidateColumns * baseMap.CandidateRows;
            if (totalCount > capacity)
                return null;
            var candidates = new List<GeneratedWaferDie>(checked((int)capacity));
            for (int row = 0; row < baseMap.CandidateRows; row++)
            for (int column = 0; column < baseMap.CandidateColumns; column++)
                candidates.Add(CreateAtGrid(baseMap, column, row, sourceOriginXMm, sourceOriginYMm));
            decimal halfX = baseMap.DisplayDieSizeXMm / 2m;
            decimal halfY = baseMap.DisplayDieSizeYMm / 2m;
            // V2 저장 맵의 좁혀진 Used bounds와 중앙거리 재배치는 그대로 보존한다.
            // V3/V4는 원내/원외를 포함한 후보 전체에서 실제 다이의 가장 먼 모서리 거리순으로
            // 선택한다. 총수에 맞춰 외곽 행·열도 자연스럽게 늘어나며, 원 밖 다이는 경고로 남는다.
            // 동거리 정렬은 원본 주소를 사용하여 회전만 바꿔도 선택 raw 주소가 흔들리지 않는다.
            var selected = candidates.OrderBy(die =>
                {
                    decimal farX = Math.Abs(die.CenterXMm) + halfX;
                    decimal farY = Math.Abs(die.CenterYMm) + halfY;
                    return farX * farX + farY * farY;
                })
                .ThenBy(die => die.RawRow).ThenBy(die => die.RawColumn).Take(totalCount)
                .OrderBy(die => die.Row).ThenBy(die => die.Column);
            var result = new GeneratedWaferMap(baseMap.Settings, baseMap.CandidateColumns, baseMap.CandidateRows,
                baseMap.BoundaryRadiusMm, selected, baseMap, baseMap.RotationDegrees, baseMap.SourceBaseMap,
                counts, totalCount, sourceOriginXMm, sourceOriginYMm);
            // 원형 윤곽과 네 끝줄이 일치할 때만 채택한다. 별도 끝줄 모양을 요청했다면
            // 아래의 기존 연속 배치 방식으로 요청 총수/네 방향 개수를 함께 검증한다.
            return SameEdgeCounts(result.EdgeCounts, counts) ? result : null;
        }

        private static GeneratedWaferMap TryCreatePhaseCounts(GeneratedWaferMap baseMap,
            WaferMapEdgeCounts counts, int totalCount)
        {
            GeneratedWaferMap source = baseMap.SourceBaseMap;
            // 현재 후보 원점이 요청을 만족하면 이 경로에 오지 않는다. 기존 위상이 실패한 경우에만
            // X/Y 중심과 반 피치의 나머지 세 조합을 검사한다. 각도와 무관하게 원본 축에서 고른다.
            decimal alternateX = -(source.CandidateColumns - 1) * source.Settings.CenterStepXMm / 2m;
            decimal alternateY = -(source.CandidateRows - 1) * source.Settings.CenterStepYMm / 2m;
            GeneratedWaferMap best = null;
            decimal bestDistanceSquared = decimal.MaxValue;
            for (int phase = 1; phase <= 3; phase++)
            {
                decimal originX = (phase & 1) != 0 ? alternateX : source.SourceOriginXMm;
                decimal originY = (phase & 2) != 0 ? alternateY : source.SourceOriginYMm;
                GeneratedWaferMap candidate = TryCreateRadialCounts(baseMap, counts, totalCount, originX, originY);
                if (candidate == null)
                    continue;
                decimal halfX = candidate.DisplayDieSizeXMm / 2m;
                decimal halfY = candidate.DisplayDieSizeYMm / 2m;
                decimal distanceSquared = candidate.Dies.Max(die =>
                {
                    decimal farX = Math.Abs(die.CenterXMm) + halfX;
                    decimal farY = Math.Abs(die.CenterYMm) + halfY;
                    return farX * farX + farY * farY;
                });
                // 총수와 네 끝줄을 모두 만족한 후보끼리만 필요한 반경을 비교한다.
                // 동률은 X 변경, Y 변경, X/Y 변경 순서를 유지하여 저장 복원을 결정적으로 만든다.
                if (best == null || distanceSquared < bestDistanceSquared)
                {
                    best = candidate;
                    bestDistanceSquared = distanceSquared;
                }
            }
            return best;
        }

        private static GeneratedWaferDie CreateAtGrid(GeneratedWaferMap map, int column, int row,
            decimal? sourceOriginXMm = null, decimal? sourceOriginYMm = null)
        {
            GeneratedWaferMap source = map.SourceBaseMap;
            int rawColumn = column;
            int rawRow = row;
            if (map.RotationDegrees == 90)
            {
                rawColumn = source.CandidateColumns - 1 - row;
                rawRow = column;
            }
            else if (map.RotationDegrees == 180)
            {
                rawColumn = source.CandidateColumns - 1 - column;
                rawRow = source.CandidateRows - 1 - row;
            }
            else if (map.RotationDegrees == 270)
            {
                rawColumn = row;
                rawRow = source.CandidateRows - 1 - column;
            }
            long stepX = checked((long)(source.Settings.CenterStepXMm * 1000m));
            long stepY = checked((long)(source.Settings.CenterStepYMm * 1000m));
            decimal x;
            decimal y;
            bool defaultOrigin = !sourceOriginXMm.HasValue && !sourceOriginYMm.HasValue &&
                map.SourceOriginXMm == source.SourceOriginXMm && map.SourceOriginYMm == source.SourceOriginYMm;
            if (map.Settings.GenerationVersion < 4 || defaultOrigin)
            {
                // V1~V3와 기본 원점은 정수 합산 뒤 나누던 순서까지 유지한다. decimal 원점과 더하면
                // 같은 좌표 0도 음수 0이 되어 기존 JSON의 double 직렬화 부호가 달라질 수 있다.
                x = (-(checked((long)source.CandidateColumns * stepX) / 2L) + (long)rawColumn * stepX) / 1000m;
                y = (-(checked((long)source.CandidateRows * stepY) / 2L) + (long)rawRow * stepY) / 1000m;
            }
            else
            {
                x = (sourceOriginXMm ?? map.SourceOriginXMm) + (long)rawColumn * stepX / 1000m;
                y = (sourceOriginYMm ?? map.SourceOriginYMm) + (long)rawRow * stepY / 1000m;
            }
            decimal displayX = map.RotationDegrees == 90 ? y : map.RotationDegrees == 270 ? -y : map.RotationDegrees == 180 ? -x : x;
            decimal displayY = map.RotationDegrees == 90 ? -x : map.RotationDegrees == 270 ? x : map.RotationDegrees == 180 ? -y : y;
            return new GeneratedWaferDie(rawColumn, rawRow, column, row, displayX, displayY);
        }

        private static int GetCenteredStart(GeneratedWaferMap map, int minimum, int maximum, int count, bool horizontal)
        {
            GeneratedWaferDie zero = CreateAtGrid(map, 0, 0);
            decimal origin = horizontal ? zero.CenterXMm : zero.CenterYMm;
            decimal step = horizontal ? map.DisplayStepXMm : map.DisplayStepYMm;
            decimal ideal = -origin / step - (count - 1) / 2m;
            decimal lower = decimal.Floor(ideal);
            int start = checked((int)(ideal - lower > 0.5m ? lower + 1m : lower));
            return Math.Max(minimum, Math.Min(maximum - count + 1, start));
        }

        private static void ExpandToSize(GeneratedWaferMap map, ref int minimum, ref int maximum, int size, bool horizontal)
        {
            if (size > MaximumCandidateCells)
                throw new InvalidOperationException("확장 격자가 너무 큽니다. 최대 후보 셀 수는 1,000,000개입니다.");
            while (maximum - minimum + 1 < size)
                ExpandOne(map, ref minimum, ref maximum, horizontal);
        }

        private static void ExpandOne(GeneratedWaferMap map, ref int minimum, ref int maximum, bool horizontal)
        {
            GeneratedWaferDie low = CreateAtGrid(map, horizontal ? minimum : 0, horizontal ? 0 : minimum);
            GeneratedWaferDie high = CreateAtGrid(map, horizontal ? maximum : 0, horizontal ? 0 : maximum);
            decimal lowPosition = horizontal ? low.CenterXMm : low.CenterYMm;
            decimal highPosition = horizontal ? high.CenterXMm : high.CenterYMm;
            if (Math.Abs(lowPosition) <= Math.Abs(highPosition))
                minimum--;
            else
                maximum++;
        }

        private static long GetMaximumCount(GeneratedWaferMap map, int minColumn, int maxColumn,
            int minRow, int maxRow, WaferMapEdgeCounts counts, out bool compatible)
        {
            long width = maxColumn - minColumn + 1L;
            long height = maxRow - minRow + 1L;
            int topStart = GetCenteredStart(map, minColumn, maxColumn, counts.Top, true);
            int bottomStart = GetCenteredStart(map, minColumn, maxColumn, counts.Bottom, true);
            int leftStart = GetCenteredStart(map, minRow, maxRow, counts.Left, false);
            int rightStart = GetCenteredStart(map, minRow, maxRow, counts.Right, false);
            compatible = true;
            if (width < 3L || height < 3L)
            {
                long capacity = 0L;
                for (int row = minRow; row <= maxRow; row++)
                for (int column = minColumn; column <= maxColumn; column++)
                {
                    bool inTop = column >= topStart && column < topStart + counts.Top;
                    bool inBottom = column >= bottomStart && column < bottomStart + counts.Bottom;
                    bool inLeft = row >= leftStart && row < leftStart + counts.Left;
                    bool inRight = row >= rightStart && row < rightStart + counts.Right;
                    bool requested = (row == maxRow && inTop) || (row == minRow && inBottom) ||
                        (column == minColumn && inLeft) || (column == maxColumn && inRight);
                    bool forbidden = (row == maxRow && !inTop) || (row == minRow && !inBottom) ||
                        (column == minColumn && !inLeft) || (column == maxColumn && !inRight);
                    if (requested && forbidden) compatible = false;
                    if (!forbidden) capacity++;
                }
                return capacity;
            }
            long result = (width - 2L) * (height - 2L) + counts.Top + counts.Bottom + counts.Left + counts.Right;
            foreach (int column in new[] { minColumn, maxColumn })
            foreach (int row in new[] { minRow, maxRow })
            {
                bool horizontal = row == maxRow ? column >= topStart && column < topStart + counts.Top :
                    column >= bottomStart && column < bottomStart + counts.Bottom;
                bool vertical = column == minColumn ? row >= leftStart && row < leftStart + counts.Left :
                    row >= rightStart && row < rightStart + counts.Right;
                if (horizontal != vertical) compatible = false;
                // 두 끝줄이 함께 요청한 모서리는 한 개이며, 한쪽만 요청한 모서리는 사용할 수 없다.
                if (horizontal || vertical) result--;
            }
            return result;
        }

        private static void CheckCandidateBounds(int minColumn, int maxColumn, int minRow, int maxRow)
        {
            long width = maxColumn - minColumn + 1L;
            long height = maxRow - minRow + 1L;
            if (width < 1L || height < 1L || width * height > MaximumCandidateCells)
                throw new InvalidOperationException("확장 격자가 너무 큽니다. 최대 후보 셀 수는 1,000,000개입니다.");
        }

        private static bool SameEdgeCounts(WaferMapEdgeCounts first, WaferMapEdgeCounts second)
        {
            return first.Top == second.Top && first.Bottom == second.Bottom &&
                first.Left == second.Left && first.Right == second.Right;
        }

        private static void CheckRequestedCount(int value, string label)
        {
            if (value < 1 || value > MaximumCandidateCells)
                throw new ArgumentOutOfRangeException(nameof(value), label + " 개수는 1 이상 1,000,000 이하이어야 합니다.");
        }

        private static HashSet<GeneratedWaferDie> SelectEdgeDies(IEnumerable<GeneratedWaferDie> dies,
            int count, bool horizontal)
        {
            return new HashSet<GeneratedWaferDie>(dies
                .OrderBy(die => Math.Abs(horizontal ? die.CenterXMm : die.CenterYMm))
                .ThenBy(die => horizontal ? die.Column : die.Row)
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
