using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using QMC.CDT320.Materials;

namespace QMC.CDT320.DieMaps
{
    /// <summary>원본 수식 입력과 현재 화면 기준 보정을 보관하는 버전 지정 저장 계약.</summary>
    [DataContract]
    public sealed class GeneratedWaferMapDefinition
    {
        [DataMember(IsRequired = true)] public int Version { get; set; } = 1;
        [DataMember(IsRequired = true)] public decimal OuterDiameterMm { get; set; }
        [DataMember(IsRequired = true)] public decimal DieSizeXMm { get; set; }
        [DataMember(IsRequired = true)] public decimal DieSizeYMm { get; set; }
        [DataMember(IsRequired = true)] public decimal GapXMm { get; set; }
        [DataMember(IsRequired = true)] public decimal GapYMm { get; set; }
        [DataMember(IsRequired = true)] public int RotationDegrees { get; set; }
        [DataMember(IsRequired = true)] public int EdgeTop { get; set; }
        [DataMember(IsRequired = true)] public int EdgeBottom { get; set; }
        [DataMember(IsRequired = true)] public int EdgeLeft { get; set; }
        [DataMember(IsRequired = true)] public int EdgeRight { get; set; }
        // V1에는 없던 필드다. V2 이후 null이면 누락된 조건이며 기본값으로 대체하지 않는다.
        [DataMember(EmitDefaultValue = false)] public decimal? EdgeMarginMm { get; set; }
        [DataMember(EmitDefaultValue = false)] public int? TotalCount { get; set; }

        [OnDeserialized]
        private void CheckCountSettingsRequiredFields(StreamingContext context)
        {
            if (Version >= 2 && (!EdgeMarginMm.HasValue || !TotalCount.HasValue))
                throw new SerializationException("생성 맵 V" + Version + "의 외곽 여백 또는 전체 다이 개수가 누락되었습니다.");
        }
    }

    /// <summary>미리보기의 원본 위상을 보존하여 Recipe DieMap 저장 좌표로 변환한다.</summary>
    public static class GeneratedWaferMapCodec
    {
        public const string SourceFormat = "RECIPE GENERATED WAFER V1";
        private const double CoordinateTolerance = 0.00000001;

        public static GeneratedWaferMapDefinition CreateDefinition(GeneratedWaferMap map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            WaferMapEdgeCounts edges = map.Settings.GenerationVersion >= 2
                ? (map.RequestedEdgeCounts ?? map.EdgeCounts) : map.EdgeCounts;
            return new GeneratedWaferMapDefinition
            {
                Version = map.Settings.GenerationVersion,
                OuterDiameterMm = map.Settings.OuterDiameterMm,
                DieSizeXMm = map.Settings.DieSizeXMm,
                DieSizeYMm = map.Settings.DieSizeYMm,
                GapXMm = map.Settings.GapXMm,
                GapYMm = map.Settings.GapYMm,
                RotationDegrees = map.RotationDegrees,
                EdgeTop = edges.Top,
                EdgeBottom = edges.Bottom,
                EdgeLeft = edges.Left,
                EdgeRight = edges.Right,
                EdgeMarginMm = map.Settings.GenerationVersion >= 2 ? (decimal?)map.Settings.EdgeMarginMm : null,
                TotalCount = map.Settings.GenerationVersion >= 2 ? (int?)(map.RequestedTotalCount ?? map.Count) : null
            };
        }

        public static GeneratedWaferMap Restore(GeneratedWaferMapDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition.Version != 1 && definition.Version != 2 && definition.Version != 3 && definition.Version != 4)
                throw new InvalidOperationException("지원하지 않는 맵 생성 저장 버전입니다: " + definition.Version);
            if (definition.Version >= 2 && (!definition.EdgeMarginMm.HasValue || !definition.TotalCount.HasValue))
                throw new InvalidOperationException("생성 맵 V" + definition.Version + "의 외곽 여백 또는 전체 다이 개수가 누락되었습니다. 맵 생성 미리보기에서 다시 생성하세요.");
            if (definition.Version == 1 && (definition.EdgeMarginMm.HasValue || definition.TotalCount.HasValue))
                throw new InvalidOperationException("생성 맵 버전과 외곽 여백·전체 개수 조건이 일치하지 않습니다.");
            WaferMapGenerationSettings settings = definition.Version == 1
                ? new WaferMapGenerationSettings(definition.OuterDiameterMm, definition.DieSizeXMm,
                    definition.DieSizeYMm, definition.GapXMm, definition.GapYMm)
                : definition.Version == 2
                    ? new WaferMapGenerationSettings(definition.OuterDiameterMm, definition.DieSizeXMm,
                        definition.DieSizeYMm, definition.GapXMm, definition.GapYMm, definition.EdgeMarginMm.Value)
                    : new WaferMapGenerationSettings(definition.OuterDiameterMm, definition.DieSizeXMm,
                        definition.DieSizeYMm, definition.GapXMm, definition.GapYMm, definition.EdgeMarginMm.Value, definition.Version);
            GeneratedWaferMap map = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(settings), definition.RotationDegrees);
            if (definition.Version == 1 && map.Count == 0)
                throw new InvalidOperationException("저장된 맵 생성 조건에 유효한 다이가 없습니다.");
            var edges = new WaferMapEdgeCounts(definition.EdgeTop, definition.EdgeBottom, definition.EdgeLeft, definition.EdgeRight);
            return definition.Version == 1 ? WaferMapGeneration.ApplyEdgeCounts(map, edges)
                : WaferMapGeneration.ApplyCounts(map, edges, definition.TotalCount.Value);
        }

        public static DieMap ToDieMap(GeneratedWaferMap generated, string frameObjId)
        {
            return ToDieMapCore(generated, frameObjId);
        }

        /// <summary>생성된 모든 다이를 보존하여 저장한다. 외곽 초과는 생성 화면 안내로 처리한다.</summary>
        public static DieMap ToDieMapForStorage(GeneratedWaferMap generated, string frameObjId)
        {
            return ToDieMapCore(generated, frameObjId);
        }

        private static DieMap ToDieMapCore(GeneratedWaferMap generated, string frameObjId)
        {
            if (generated == null)
                throw new ArgumentNullException(nameof(generated));
            if (generated.Count == 0)
                throw new InvalidOperationException("다이가 없는 미리보기는 저장할 수 없습니다.");
            // 사용자 확정: 외곽 초과는 생성 화면에서 안내하며, 생성된 배치를 그대로 사용한다.
            // 저장/적용 시 다이를 삭제하거나 외곽 초과만으로 거부하지 않고 아래 정의·좌표 정합성 검증을 유지한다.
            GeneratedWaferDie anchor = generated.Dies[0];
            decimal originX = anchor.CenterXMm - (anchor.Column - generated.MinColumn) * generated.DisplayStepXMm;
            decimal originY = -anchor.CenterYMm - (generated.MaxRow - anchor.Row) * generated.DisplayStepYMm;
            var map = new DieMap
            {
                FrameObjId = string.IsNullOrWhiteSpace(frameObjId) ? "GENERATED_WAFER" : frameObjId,
                DieMapX = generated.UsedColumns,
                DieMapY = generated.UsedRows,
                PitchX = (double)generated.DisplayStepXMm,
                PitchY = (double)generated.DisplayStepYMm,
                DieSizeX = (double)generated.DisplayDieSizeXMm,
                DieSizeY = (double)generated.DisplayDieSizeYMm,
                OuterDiameterMm = (double)generated.Settings.OuterDiameterMm,
                EdgeSkipMode = "ExternalMap",
                OriginX = (double)originX,
                OriginY = (double)originY,
                SourceFileName = "맵 생성 미리보기",
                SourceFormat = SourceFormat,
                SourceDeclaredCount = generated.Count,
                Generation = CreateDefinition(generated)
            };
            foreach (GeneratedWaferDie die in generated.Dies.OrderByDescending(d => d.Row).ThenBy(d => d.Column))
            {
                map.Entries.Add(new DieMapEntry
                {
                    Index = map.Entries.Count,
                    DieMapX = die.Column - generated.MinColumn,
                    DieMapY = generated.MaxRow - die.Row,
                    OriginalMapX = die.RawColumn,
                    OriginalMapY = die.RawRow,
                    PosX = (double)die.CenterXMm,
                    PosY = (double)-die.CenterYMm,
                    EquipmentGridX = (double)(die.CenterXMm / generated.DisplayStepXMm),
                    EquipmentGridY = (double)(-die.CenterYMm / generated.DisplayStepYMm),
                    IsTarget = true,
                    Result = DieResult.Good,
                    DieUid = map.FrameObjId + "-D" + die.RawRow.ToString("000") + "-" + die.RawColumn.ToString("000")
                });
            }
            return map;
        }

        /// <summary>외부/구형 맵은 통과시킨다. 신규 형식은 생성 정의와 모든 셀의 형상을 비교한다.</summary>
        public static bool Validate(DieMap map, out string reason)
        {
            return ValidateCore(map, out reason);
        }

        /// <summary>일반 검증과 동일하게 생성 정의·주소·좌표·개수의 저장 정합성을 모두 검사한다.</summary>
        public static bool ValidateForStorage(DieMap map, out string reason)
        {
            return ValidateCore(map, out reason);
        }

        private static bool ValidateCore(DieMap map, out string reason)
        {
            reason = "";
            if (map == null)
            {
                reason = "맵이 없습니다.";
                return false;
            }
            bool knownFormat = string.Equals(map.SourceFormat, SourceFormat, StringComparison.Ordinal);
            if (map.Generation == null && !knownFormat)
                return true;
            if (map.Generation == null || !knownFormat)
            {
                reason = "맵 생성 정의와 원본 형식이 일치하지 않습니다. 맵 생성 미리보기에서 다시 저장하십시오.";
                return false;
            }
            try
            {
                GeneratedWaferMap restored = Restore(map.Generation);
                DieMap expected = ToDieMapCore(restored, map.FrameObjId);
                if (map.DieMapX != expected.DieMapX || map.DieMapY != expected.DieMapY ||
                    !Same(map.PitchX, expected.PitchX) || !Same(map.PitchY, expected.PitchY) ||
                    !Same(map.DieSizeX, expected.DieSizeX) || !Same(map.DieSizeY, expected.DieSizeY) ||
                    !Same(map.OuterDiameterMm, expected.OuterDiameterMm) ||
                    !Same(map.OriginX, expected.OriginX) || !Same(map.OriginY, expected.OriginY) ||
                    !string.Equals(map.EdgeSkipMode, expected.EdgeSkipMode, StringComparison.OrdinalIgnoreCase) ||
                    !Same(map.SideEdgeSkip, 0) || !Same(map.TopBottomEdgeSkip, 0) ||
                    map.SourceDeclaredCount != restored.Count || map.Entries == null || map.Entries.Count != restored.Count)
                {
                    reason = "생성 맵의 격자·간격·외경·원점·개수가 저장된 생성 조건과 다릅니다.";
                    return false;
                }
                var byRaw = expected.Entries.ToDictionary(e => Address(e.OriginalMapX, e.OriginalMapY));
                var seen = new HashSet<long>();
                for (int i = 0; i < map.Entries.Count; i++)
                {
                    DieMapEntry entry = map.Entries[i];
                    DieMapEntry source;
                    if (entry == null || entry.Index != i ||
                        !seen.Add(Address(entry.OriginalMapX, entry.OriginalMapY)) ||
                        !byRaw.TryGetValue(Address(entry.OriginalMapX, entry.OriginalMapY), out source) ||
                        entry.DieMapX != source.DieMapX || entry.DieMapY != source.DieMapY ||
                        !Same(entry.PosX, source.PosX) || !Same(entry.PosY, source.PosY) ||
                        !Same(entry.EquipmentGridX, source.EquipmentGridX) || !Same(entry.EquipmentGridY, source.EquipmentGridY))
                    {
                        reason = "생성 맵의 원본 주소·격자·좌표가 미리보기 결과와 다릅니다. 셀=" + i;
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is OverflowException)
            {
                reason = "맵 생성 정의를 복원할 수 없습니다: " + ex.Message;
                return false;
            }
        }

        public static GeneratedWaferMapDefinition CloneDefinition(GeneratedWaferMapDefinition source)
        {
            if (source == null)
                return null;
            return new GeneratedWaferMapDefinition
            {
                Version = source.Version,
                OuterDiameterMm = source.OuterDiameterMm,
                DieSizeXMm = source.DieSizeXMm,
                DieSizeYMm = source.DieSizeYMm,
                GapXMm = source.GapXMm,
                GapYMm = source.GapYMm,
                RotationDegrees = source.RotationDegrees,
                EdgeTop = source.EdgeTop,
                EdgeBottom = source.EdgeBottom,
                EdgeLeft = source.EdgeLeft,
                EdgeRight = source.EdgeRight,
                EdgeMarginMm = source.EdgeMarginMm,
                TotalCount = source.TotalCount
            };
        }

        internal static string SerializeDefinition(GeneratedWaferMapDefinition definition)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(GeneratedWaferMapDefinition)).WriteObject(stream, definition);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        internal static GeneratedWaferMapDefinition DeserializeDefinition(string value)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
                return (GeneratedWaferMapDefinition)new DataContractJsonSerializer(typeof(GeneratedWaferMapDefinition)).ReadObject(stream);
        }

        private static long Address(int x, int y) => ((long)x << 32) | (uint)y;

        private static bool Same(double actual, double expected)
        {
            return !double.IsNaN(actual) && !double.IsInfinity(actual) && Math.Abs(actual - expected) <= CoordinateTolerance;
        }
    }
}
