using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace QMC.CDT320.DieMaps
{
    public enum WaferMapGridOrigin { TopLeft, BottomLeft, TopRight, BottomRight, Center }
    public enum WaferMapSourceFormat { Legacy, Rad, Camtek, Samsung, Other, Circle }

    [DataContract]
    public sealed class WaferMapProcessSettings
    {
        /// <summary>원격 Input 다운로드 파일의 구분자. 등록 Input/Output 맵에는 적용하지 않는다.</summary>
        [DataMember] public WaferMapSourceFormat Format { get; set; }
        /// <summary>등록된 기준 맵에 추가 적용하는 공정 회전. 생성 미리보기의 저장 각도와 별도다.</summary>
        [DataMember] public int RotationDegrees { get; set; }
        [DataMember] public WaferMapGridOrigin GridOrigin { get; set; }
    }

    [DataContract]
    public sealed class WaferMapProcessTransform
    {
        [DataMember] public int Version { get; set; } = 1;
        [DataMember] public string Role { get; set; }
        [DataMember] public string SettingsKey { get; set; }
        [DataMember] public string SourceMapHash { get; set; }
        [DataMember] public WaferMapProcessSettings Settings { get; set; }
        [DataMember(EmitDefaultValue = false)] public GeneratedWaferMapDefinition SourceGeneration { get; set; }
        [DataMember] public bool IsAbsolutePosition { get; set; }
    }

    /// <summary>원본 다이 연결을 유지하는 공정용 맵 준비. 장비·Material·기준 파일을 변경하지 않는다.</summary>
    public static class WaferMapProcessService
    {
        public static string GetSettingsKey(WaferMapProcessSettings settings)
        {
            if (settings == null) return "Legacy";
            ValidateSettings(settings);
            return "V1|" + settings.Format + "|" + settings.RotationDegrees.ToString(CultureInfo.InvariantCulture) + "|" + settings.GridOrigin;
        }

        /// <summary>내부 적용 좌표(0 기준)를 뷰어/결과 CSV 공통 표시 좌표(1 기준)로 변환한다.
        /// 원본 주소·배열 인덱스·공정 좌표는 변경하지 않으며, 표시된 값을 다시 입력하지 않는다.</summary>
        public static string FormatMapCoordinate(double? value, string missing = "-")
        {
            return value.HasValue && Finite(value.Value)
                ? CanonicalZero(value.Value + 1.0).ToString("0.######", CultureInfo.InvariantCulture) : missing;
        }

        public static string FormatMapPosition(DieMapEntry entry)
        {
            return "X=" + FormatMapCoordinate(entry != null ? entry.LogicalGridX : null) +
                "  Y=" + FormatMapCoordinate(entry != null ? entry.LogicalGridY : null);
        }

        public static WaferMapProcessSettings CloneSettings(WaferMapProcessSettings settings)
        {
            return settings == null ? null : new WaferMapProcessSettings
            {
                Format = settings.Format, RotationDegrees = settings.RotationDegrees, GridOrigin = settings.GridOrigin
            };
        }

        public static void ValidateSettings(WaferMapProcessSettings settings)
        {
            if (settings == null) return;
            if (!Enum.IsDefined(typeof(WaferMapSourceFormat), settings.Format) ||
                !Enum.IsDefined(typeof(WaferMapGridOrigin), settings.GridOrigin))
                throw new InvalidDataException("맵 구분자 또는 Grid 원점 설정이 올바르지 않습니다.");
            // 사용자가 확정한 장비 사용 범위다. 기구 T축 90/270 이동과는 무관하다.
            if (settings.RotationDegrees != 0 && settings.RotationDegrees != 180)
                throw new InvalidDataException("공정 맵 회전은 0° 또는 180°만 사용할 수 있습니다. 90°/270° 공정 사용은 차단됩니다.");
        }

        public static DieMap Prepare(DieMap source, WaferMapProcessSettings settings, string role)
        {
            if (source == null) throw new InvalidDataException("공정에 사용할 기준 맵이 없습니다.");
            ValidateSettings(settings);
            string key = GetSettingsKey(settings);
            if (source.ProcessTransform != null)
            {
                WaferMapProcessTransform transform = source.ProcessTransform;
                if (transform.Version != 1 || transform.IsAbsolutePosition ||
                    !string.Equals(transform.SettingsKey, key, StringComparison.Ordinal) ||
                    !string.Equals(transform.Role, role, StringComparison.Ordinal))
                    throw new InvalidDataException("이미 준비된 맵에 다른 설정 또는 장비 좌표 변환을 중복 적용할 수 없습니다. 기준 맵에서 다시 준비하세요.");
                ValidateGeometry(source);
                return CloneMap(source);
            }
            ValidateGeometry(source);
            // 공통 맵 준비는 회전·원점만 적용한다. 구분자 검사는 원격 수신 파서 경계에서 수행한다.
            if (source.Generation != null || string.Equals(source.SourceFormat, GeneratedWaferMapCodec.SourceFormat, StringComparison.Ordinal))
            {
                string reason;
                if (!GeneratedWaferMapCodec.Validate(source, out reason)) throw new InvalidDataException(reason);
                if (source.Generation.RotationDegrees == 90 || source.Generation.RotationDegrees == 270)
                    throw new InvalidDataException("90°/270°로 저장된 생성 맵은 공정에 사용할 수 없습니다.");
            }
            DieMap result = CloneMap(source);
            result.ProcessTransform = new WaferMapProcessTransform
            {
                Role = role, SettingsKey = key, Settings = CloneSettings(settings),
                SourceMapHash = ComputeMapHash(source), SourceGeneration = GeneratedWaferMapCodec.CloneDefinition(source.Generation)
            };
            // 이후 절대좌표 계산 맵에 생성 미리보기 정의를 검증하는 일을 방지한다.
            result.Generation = null;
            int angle = settings == null ? 0 : settings.RotationDegrees;
            if (angle == 180)
            {
                result.OriginX = CanonicalZero(-source.OriginX - (source.DieMapX - 1) * source.PitchX);
                result.OriginY = CanonicalZero(-source.OriginY - (source.DieMapY - 1) * source.PitchY);
            }
            foreach (DieMapEntry entry in result.Entries)
            {
                // 구형 맵의 -1 원본 주소는 회전 전 배열 주소로 확정해야 다이 연결이 뒤집히지 않는다.
                if (entry.OriginalMapX < 0) entry.OriginalMapX = entry.DieMapX;
                if (entry.OriginalMapY < 0) entry.OriginalMapY = entry.DieMapY;
                if (!entry.SourceBinCode.HasValue) entry.SourceBinCode = entry.BinCode;
                if (angle == 180)
                {
                    entry.DieMapX = source.DieMapX - 1 - entry.DieMapX;
                    entry.DieMapY = source.DieMapY - 1 - entry.DieMapY;
                    entry.PosX = CanonicalZero(-entry.PosX);
                    entry.PosY = CanonicalZero(-entry.PosY);
                    entry.EquipmentGridX = entry.DieMapX - (source.DieMapX - 1) / 2.0;
                    entry.EquipmentGridY = DieMapGenerator.CalculateEquipmentGridY(entry.DieMapY, source.DieMapY);
                }
                SetLogicalGrid(entry, result, settings == null ? WaferMapGridOrigin.TopLeft : settings.GridOrigin);
                // 최종 형상 뒤에 호출자가 대상 필터와 공정 순서를 확정한다.
                entry.SequenceNo = 0;
            }
            ValidateGeometry(result);
            return result;
        }

        /// <summary>원격 다운로드 파서의 형식 검증. 등록 맵/미리보기/역할 맵 승인에는 사용하지 않는다.</summary>
        public static void ValidateSourceFormat(DieMap map, WaferMapProcessSettings settings)
        {
            if (settings == null || settings.Format == WaferMapSourceFormat.Legacy) return;
            string expected;
            switch (settings.Format)
            {
                case WaferMapSourceFormat.Rad:
                case WaferMapSourceFormat.Samsung: expected = "RAD TXT"; break;
                case WaferMapSourceFormat.Camtek: expected = "CAMTEK RowData"; break;
                case WaferMapSourceFormat.Circle:
                    // 기존 Recipe Grid 생성 경로도 원형 외곽 마스크를 가진 등록 서클 맵이다.
                    if (string.Equals(map.SourceFormat, "RECIPE GRID", StringComparison.Ordinal)) return;
                    expected = GeneratedWaferMapCodec.SourceFormat;
                    if (string.Equals(map.SourceFormat, expected, StringComparison.Ordinal) && map.Generation == null)
                        throw new InvalidDataException("서클 생성 맵의 생성 정의가 누락되었습니다.");
                    break;
                default:
                    throw new InvalidDataException(settings.Format + " 맵은 실제 파일 형식이 확인되지 않아 공정 사용이 지원되지 않습니다. 구분자와 파일 사양을 확인하세요.");
            }
            if (!string.Equals(map.SourceFormat, expected, StringComparison.Ordinal))
                throw new InvalidDataException("설정된 맵 형식과 기준 맵이 다릅니다. 설정=" + settings.Format + ", 파일 형식=" + (map.SourceFormat ?? "없음"));
        }

        public static void ValidateGeometry(DieMap map)
        {
            if (map.DieMapX <= 0 || map.DieMapY <= 0 || map.Entries == null || map.Entries.Count == 0 ||
                !Finite(map.PitchX) || !Finite(map.PitchY) || map.PitchX <= 0 || map.PitchY <= 0 ||
                !Finite(map.OriginX) || !Finite(map.OriginY))
                throw new InvalidDataException("맵의 격자·피치·원점 또는 다이 목록이 올바르지 않습니다.");
            var cells = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var originals = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null || entry.DieMapX < 0 || entry.DieMapX >= map.DieMapX ||
                    entry.DieMapY < 0 || entry.DieMapY >= map.DieMapY || !Finite(entry.PosX) || !Finite(entry.PosY))
                    throw new InvalidDataException("맵에 범위를 벗어난 배열 주소 또는 잘못된 다이 위치가 있습니다.");
                if (!cells.Add(entry.DieMapX + "," + entry.DieMapY))
                    throw new InvalidDataException("맵에 중복 배열 주소가 있습니다. 서로 다른 다이를 같은 셀에 적용할 수 없습니다.");
                int rawX = entry.OriginalMapX >= 0 ? entry.OriginalMapX : entry.DieMapX;
                int rawY = entry.OriginalMapY >= 0 ? entry.OriginalMapY : entry.DieMapY;
                if (!originals.Add(rawX + "," + rawY))
                    throw new InvalidDataException("맵에 중복 원본 주소가 있습니다. 원본 다이 연결을 확인하세요.");
            }
        }

        public static DieMap CloneMap(DieMap map)
        {
            if (map == null) return null;
            // Material 저장마다 호출되므로 전체 JSON 직렬화/파싱을 반복하지 않고 필드를 깊은 복사한다.
            return new DieMap
            {
                FrameObjId = map.FrameObjId, DieMapX = map.DieMapX, DieMapY = map.DieMapY,
                PitchX = map.PitchX, PitchY = map.PitchY, DieSizeX = map.DieSizeX, DieSizeY = map.DieSizeY,
                OuterDiameterMm = map.OuterDiameterMm, EdgeSkipMode = map.EdgeSkipMode,
                SideEdgeSkip = map.SideEdgeSkip, TopBottomEdgeSkip = map.TopBottomEdgeSkip,
                OriginX = map.OriginX, OriginY = map.OriginY, SourceFileName = map.SourceFileName,
                SourceFormat = map.SourceFormat, SourceContentHash = map.SourceContentHash,
                SourcePitchFromFile = map.SourcePitchFromFile, SourceDeclaredCount = map.SourceDeclaredCount,
                SourceFirstX = map.SourceFirstX, SourceFirstY = map.SourceFirstY,
                SourceFirstPosX = map.SourceFirstPosX, SourceFirstPosY = map.SourceFirstPosY,
                Generation = GeneratedWaferMapCodec.CloneDefinition(map.Generation),
                ProcessTransform = CloneTransform(map.ProcessTransform), CreatedAt = map.CreatedAt,
                Entries = map.Entries == null ? null : map.Entries.Select(entry => entry == null ? null : new DieMapEntry
                {
                    Index = entry.Index, SequenceNo = entry.SequenceNo, DieMapX = entry.DieMapX, DieMapY = entry.DieMapY,
                    OriginalMapX = entry.OriginalMapX, OriginalMapY = entry.OriginalMapY,
                    IsTarget = entry.IsTarget, Result = entry.Result, BinCode = entry.BinCode,
                    SourceBinCode = entry.SourceBinCode, SourceToken = entry.SourceToken,
                    LogicalGridX = entry.LogicalGridX, LogicalGridY = entry.LogicalGridY,
                    PosX = entry.PosX, PosY = entry.PosY, EquipmentGridX = entry.EquipmentGridX,
                    EquipmentGridY = entry.EquipmentGridY, DieUid = entry.DieUid
                }).ToList()
            };
        }

        public static WaferMapProcessTransform CloneTransform(WaferMapProcessTransform source)
        {
            return source == null ? null : new WaferMapProcessTransform
            {
                Version = source.Version, Role = source.Role, SettingsKey = source.SettingsKey,
                SourceMapHash = source.SourceMapHash, Settings = CloneSettings(source.Settings),
                SourceGeneration = GeneratedWaferMapCodec.CloneDefinition(source.SourceGeneration),
                IsAbsolutePosition = source.IsAbsolutePosition
            };
        }

        public static string SerializeTransform(WaferMapProcessTransform value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(WaferMapProcessTransform)).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static WaferMapProcessTransform DeserializeTransform(string value)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
                return (WaferMapProcessTransform)new DataContractJsonSerializer(typeof(WaferMapProcessTransform)).ReadObject(stream);
        }

        public static string ComputeMapHash(DieMap map)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(DieMap)).WriteObject(stream, map);
                return ComputeHash(stream.ToArray());
            }
        }

        public static string ComputeHash(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(data).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        // JSON 재저장 시 -0이 0으로 바뀌어 준비 맵 동일성 해시가 달라지지 않도록 한다.
        private static double CanonicalZero(double value) { return value == 0 ? 0 : value; }

        private static void SetLogicalGrid(DieMapEntry entry, DieMap map, WaferMapGridOrigin origin)
        {
            double x = entry.DieMapX;
            double y = entry.DieMapY;
            if (origin == WaferMapGridOrigin.TopRight || origin == WaferMapGridOrigin.BottomRight) x = map.DieMapX - 1 - x;
            if (origin == WaferMapGridOrigin.BottomLeft || origin == WaferMapGridOrigin.BottomRight) y = map.DieMapY - 1 - y;
            if (origin == WaferMapGridOrigin.Center)
            {
                x -= (map.DieMapX - 1) / 2.0;
                y = (map.DieMapY - 1) / 2.0 - y;
            }
            entry.LogicalGridX = x;
            entry.LogicalGridY = y;
        }
    }
}
