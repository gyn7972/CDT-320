using System;
using System.Collections.Generic;
using System.IO;

namespace QMC.CDT320.DieMaps
{
    /// <summary>파일 확장자와 무관한 명시적 형식 선택. 업체 추가는 이 등록부에서 연결한다.</summary>
    public static class WaferMapParserRegistry
    {
        private static readonly Dictionary<WaferMapSourceFormat, Func<string, DieMap>> Parsers =
            new Dictionary<WaferMapSourceFormat, Func<string, DieMap>>
            {
                // 사용자 확인: 삼성 파일은 기존 Network Wafer Map의 RAD 형식이다.
                { WaferMapSourceFormat.Samsung, DieMapGenerator.LoadWaferMapTextOrThrow },
                { WaferMapSourceFormat.Rad, DieMapGenerator.LoadWaferMapTextOrThrow },
                { WaferMapSourceFormat.Camtek, DieMapGenerator.LoadCamtekWaferMapTextOrThrow },
                { WaferMapSourceFormat.Circle, DieMapGenerator.LoadJson }
            };

        public static DieMap Load(string path, WaferMapSourceFormat format)
        {
            Func<string, DieMap> parser;
            if (!Parsers.TryGetValue(format, out parser))
                throw new InvalidDataException("선택한 맵 형식은 지원되지 않습니다. 구분자=" + format);
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidDataException("맵 파일 경로가 없습니다.");
            // 동일 바이트로 해시와 맵을 만든다. 공유 캐시가 나중에 교체되어도 준비한 데이터는 바뀌지 않는다.
            byte[] content = File.ReadAllBytes(path);
            string snapshot = Path.Combine(Path.GetTempPath(), "CDT320-WaferMap-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllBytes(snapshot, content);
                DieMap map = parser(snapshot);
                if (map == null) throw new InvalidDataException("선택한 형식으로 맵을 읽을 수 없습니다. 구분자=" + format);
                map.SourceFileName = Path.GetFileName(path);
                map.FrameObjId = map.SourceFileName;
                if (format == WaferMapSourceFormat.Samsung || format == WaferMapSourceFormat.Rad || format == WaferMapSourceFormat.Camtek)
                {
                    // 파서에 전달한 임시 파일명이 다이 식별자로 남지 않도록 전체 바코드 파일명으로 확정한다.
                    foreach (DieMapEntry entry in map.Entries)
                        entry.DieUid = DieMapGenerator.BuildExternalMapDieUid(map.FrameObjId, entry.OriginalMapX, entry.OriginalMapY);
                }
                map.SourceContentHash = WaferMapProcessService.ComputeHash(content);
                WaferMapProcessService.ValidateSourceFormat(map, new WaferMapProcessSettings { Format = format });
                return map;
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is FormatException || ex is System.Runtime.Serialization.SerializationException)
            {
                throw new InvalidDataException("맵 형식 불일치: 설정=" + format + ", 파일=" + Path.GetFileName(path) + ". " + ex.Message, ex);
            }
            finally
            {
                // 이 호출에서 생성한 고유 파일만 정리한다.
                if (File.Exists(snapshot)) File.Delete(snapshot);
            }
        }

        public static DieMap LoadRegistration(string path, WaferMapProcessSettings settings)
        {
            // 등록 LOAD는 원격 Input 구분자와 무관하게 파일 자체의 저장 형식을 읽는다.
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)) return DieMapGenerator.Load(path);
            foreach (string line in File.ReadLines(path))
            {
                string text = line.TrimStart();
                if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^(ROWCT|COLCT|RowData)\s*:",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return Load(path, WaferMapSourceFormat.Camtek);
                if (text.StartsWith("PLACE_WAFER_ROW", StringComparison.OrdinalIgnoreCase))
                    return DieMapGenerator.LoadWaferMapTextOrThrow(path);
            }
            if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)) return DieMapGenerator.Load(path);
            return Load(path, WaferMapSourceFormat.Samsung);
        }
    }
}
