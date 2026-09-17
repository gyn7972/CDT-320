using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using QMC.CDT320.DieMaps;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>원본 파일과 분리된 화면용 회전 맵을 만든다. 공정 맵이나 장비 상태에는 적용하지 않는다.</summary>
    internal sealed class WaferMapViewerData
    {
        private static readonly Regex RadPointRegex = new Regex(
            @"^\s*X=\s*(?<x>[-+]?\d+)\s+Y=\s*(?<y>[-+]?\d+)\s+B=\s*(?<b>[-+]?\d+)(?=\s|$)");
        private readonly Dictionary<Tuple<int, int>, string> _sourceTokens;

        public string FilePath { get; private set; }
        public bool IsCamtek { get; private set; }
        public DieMap SourceMap { get; private set; }
        public IReadOnlyDictionary<string, string> Headers { get; private set; }
        public int ProcessRotationDegrees => IsCamtek ? 180 : 0;

        private WaferMapViewerData(string filePath, bool camtek, DieMap sourceMap,
            IReadOnlyDictionary<string, string> headers, Dictionary<Tuple<int, int>, string> sourceTokens)
        {
            FilePath = filePath;
            IsCamtek = camtek;
            SourceMap = sourceMap;
            Headers = headers;
            _sourceTokens = sourceTokens;
        }

        public static WaferMapViewerData Load(string path, bool camtek)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("확인할 웨이퍼맵 파일 경로가 비어 있습니다.", nameof(path));

            string fullPath = Path.GetFullPath(path);
            // 확장자 대신 사용자가 선택한 형식의 기존 파서를 사용한다. 실패 사유는 화면에 전달한다.
            DieMap source = camtek
                ? DieMapGenerator.LoadCamtekWaferMapTextOrThrow(fullPath)
                : DieMapGenerator.LoadWaferMapTextOrThrow(fullPath);
            IReadOnlyDictionary<string, string> headers;
            Dictionary<Tuple<int, int>, string> sourceTokens;
            ReadSourceDetails(fullPath, camtek, out headers, out sourceTokens);
            return new WaferMapViewerData(fullPath, camtek, source, headers, sourceTokens);
        }

        /// <summary>원본 local 배열을 기준으로 시계 방향 회전한 새 맵을 반환한다. 각 호출은 독립적이다.</summary>
        public DieMap CreateView(int clockwiseDegrees)
        {
            if (clockwiseDegrees != 0 && clockwiseDegrees != 90 &&
                clockwiseDegrees != 180 && clockwiseDegrees != 270)
            {
                throw new ArgumentOutOfRangeException(nameof(clockwiseDegrees),
                    "표시 회전은 0, 90, 180, 270도 중에서 선택하십시오.");
            }

            DieMap source = SourceMap;
            bool quarterTurn = clockwiseDegrees == 90 || clockwiseDegrees == 270;
            int width = quarterTurn ? source.DieMapY : source.DieMapX;
            int height = quarterTurn ? source.DieMapX : source.DieMapY;
            double pitchX = quarterTurn ? source.PitchY : source.PitchX;
            double pitchY = quarterTurn ? source.PitchX : source.PitchY;
            double centerX = (width - 1) / 2.0;
            double centerY = (height - 1) / 2.0;

            var view = new DieMap
            {
                FrameObjId = source.FrameObjId,
                DieMapX = width,
                DieMapY = height,
                PitchX = pitchX,
                PitchY = pitchY,
                DieSizeX = quarterTurn ? source.DieSizeY : source.DieSizeX,
                DieSizeY = quarterTurn ? source.DieSizeX : source.DieSizeY,
                OuterDiameterMm = source.OuterDiameterMm,
                EdgeSkipMode = source.EdgeSkipMode,
                SideEdgeSkip = quarterTurn ? source.TopBottomEdgeSkip : source.SideEdgeSkip,
                TopBottomEdgeSkip = quarterTurn ? source.SideEdgeSkip : source.TopBottomEdgeSkip,
                // 이 Pos/Origin은 화면 확인용 중심 상대 mm이며, 실제 장비의 축 목표가 아니다.
                OriginX = -centerX * pitchX,
                OriginY = -centerY * pitchY,
                SourceFileName = source.SourceFileName,
                SourceFormat = source.SourceFormat,
                SourcePitchFromFile = source.SourcePitchFromFile,
                SourceDeclaredCount = source.SourceDeclaredCount,
                SourceFirstX = source.SourceFirstX,
                SourceFirstY = source.SourceFirstY,
                SourceFirstPosX = source.SourceFirstPosX,
                SourceFirstPosY = source.SourceFirstPosY,
                CreatedAt = source.CreatedAt,
                Entries = new List<DieMapEntry>(source.Entries.Count)
            };

            foreach (DieMapEntry entry in source.Entries)
            {
                int x = entry.DieMapX;
                int y = entry.DieMapY;
                // RAD와 CAMTEK의 원본 주소 규약은 다르므로, 파서가 만든 local 배열만 회전한다.
                switch (clockwiseDegrees)
                {
                    case 90:
                        x = source.DieMapY - 1 - entry.DieMapY;
                        y = entry.DieMapX;
                        break;
                    case 180:
                        x = source.DieMapX - 1 - entry.DieMapX;
                        y = source.DieMapY - 1 - entry.DieMapY;
                        break;
                    case 270:
                        x = entry.DieMapY;
                        y = source.DieMapX - 1 - entry.DieMapX;
                        break;
                }

                double gridX = x - centerX;
                double gridY = y - centerY;
                view.Entries.Add(new DieMapEntry
                {
                    Index = entry.Index,
                    SequenceNo = entry.SequenceNo,
                    DieMapX = x,
                    DieMapY = y,
                    // 독립 파일 뷰어의 현재 보기 좌표. 실제 공정/Recipe에는 적용하지 않는다.
                    LogicalGridX = x,
                    LogicalGridY = view.DieMapY - 1 - y,
                    OriginalMapX = entry.OriginalMapX,
                    OriginalMapY = entry.OriginalMapY,
                    IsTarget = entry.IsTarget,
                    Result = entry.Result,
                    BinCode = entry.BinCode,
                    PosX = gridX * pitchX,
                    PosY = gridY * pitchY,
                    EquipmentGridX = gridX,
                    EquipmentGridY = gridY,
                    DieUid = entry.DieUid
                });
            }

            // Normalize나 순번 생성은 다시 실행하지 않는다. 원본 셀 상태와 순번을 그대로 보존한다.
            return view;
        }

        /// <summary>파서 정규화 전의 토큰을 원본 주소로 찾는다. @@@와 숫자 000/255를 구분할 때 사용한다.</summary>
        public string GetSourceToken(DieMapEntry entry)
        {
            if (entry == null)
                return "";
            string token;
            return _sourceTokens.TryGetValue(Tuple.Create(entry.OriginalMapX, entry.OriginalMapY), out token)
                ? token
                : "";
        }

        private static void ReadSourceDetails(string path, bool camtek,
            out IReadOnlyDictionary<string, string> headers,
            out Dictionary<Tuple<int, int>, string> sourceTokens)
        {
            var headerValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            sourceTokens = new Dictionary<Tuple<int, int>, string>();
            int radHeaderNumber = 0;
            int camtekRow = 0;
            bool recordsStarted = false;
            foreach (string line in File.ReadLines(path))
            {
                string text = (line ?? "").Trim();
                if (text.Length == 0)
                    continue;

                if (camtek)
                {
                    int colon = text.IndexOf(':');
                    if (colon <= 0)
                        continue;
                    string key = text.Substring(0, colon).Trim();
                    if (key.Equals("RowData", StringComparison.OrdinalIgnoreCase))
                    {
                        recordsStarted = true;
                        string[] tokens = text.Substring(colon + 1)
                            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int col = 0; col < tokens.Length; col++)
                            sourceTokens.Add(Tuple.Create(col, camtekRow), tokens[col]);
                        camtekRow++;
                    }
                    else if (!recordsStarted && !headerValues.ContainsKey(key))
                    {
                        headerValues.Add(key, text.Substring(colon + 1).Trim());
                    }
                }
                else
                {
                    Match point = RadPointRegex.Match(text);
                    int originalX;
                    int originalY;
                    if (point.Success &&
                        int.TryParse(point.Groups["x"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out originalX) &&
                        int.TryParse(point.Groups["y"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out originalY))
                    {
                        recordsStarted = true;
                        sourceTokens[Tuple.Create(originalX, originalY)] = point.Groups["b"].Value;
                    }
                    else if (!recordsStarted)
                    {
                        // RAD 헤더의 LOT/장비/수량 필드 의미를 추정하지 않고, 다이 레코드 앞 원문을 보존한다.
                        radHeaderNumber++;
                        headerValues.Add("HEADER " + radHeaderNumber.ToString(CultureInfo.InvariantCulture), text);
                    }
                }
            }
            headers = new ReadOnlyDictionary<string, string>(headerValues);
        }
    }
}
