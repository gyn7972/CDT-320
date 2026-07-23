using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QMC.Common;
using QMC.CDT320.Recipes;

namespace QMC.CDT320.Materials
{
    /// <summary>
    /// Bottom/Place 검사 결과를 고객 지정 CSV와 통신 원문으로 비동기 저장한다.
    /// 저장 실패가 자동 시퀀스 결과에 영향을 주지 않도록 모든 파일 I/O는 전용 Queue에서 수행한다.
    /// </summary>
    internal static class VisionInspectionResultFileWriter
    {
        private const string InputHeader =
            "Lot_ID,Material_ID,Presence,Recipe_Name,Picker_Number,Result,Fail_Code,Loading_Substrate_ID,Loading_Substrate_Y,Loading_Substrate_X,Pick_Position_X,Pick_Position_Y,Pick_Offset_X,Pick_Offset_Y,Pick_Offset_T,Pick_StartTime,Pick_EndTime,Unloading_Substrate_ID,Unloading_Substrate_Y,Unloading_Substrate_X,Pre_Place_Position_X,Pre_Place_Position_Y,Pre_Place_Offset_X,Pre_Place_Offset_Y,Pre_Place_Offset_T,Pre_Place_StartTime,Pre_Place_EndTime,Die_Width,Die_Height,Back_Chipping_Top_Size,Back_Chipping_Right_Size,Back_Chipping_Bottom_Size,Back_Chipping_Left_Size,Back_Foreign_Size,Side_Chipping_Bottom,Side_Chipping_Left,Side_Chipping_Top,Side_Chipping_Right,Post_Place_Offset_X,Post_Place_Offset_Y,Post_Place_Offset_T,Post_Place_Top_Gap_Min,Post_Place_Top_Gap_Max,Post_Place_Top_Gap_Avg,Post_Place_Left_Gap_Min,Post_Place_Left_Gap_Max,Post_Place_Left_Gap_Avg,Post_Place_Bottom_Gap_Min,Post_Place_Bottom_Gap_Max,Post_Place_Bottom_Gap_Avg,Post_Place_Right_Gap_Min,Post_Place_Right_Gap_Max,Post_Place_Right_Gap_Avg,Post_Place_Angle,Post_Place_Result_Code,";

        private const string OutputSummaryHeader =
            "LOT_ID,START_TIME,END_TIME,EQP_ID,IN_QTY,PICK_ORIGIN,PLACE_ORIGIN,PICK_CASSETTE_ID,PLACE_CASSETTE_ID,COLLETT_MODEL_NUMBER,COLLET_ID_NUMBER,DT_COUNT";

        private const string OutputDieHeader =
            "CHIP_SEQ,HEAD,PICK_WAFER_ID,PICK_WAFER_ROW,PICK_WAFER_COL,PLACE_WAFER_ID,PLACE_WAFER_ROW,PLACE_WAFER_COL,ChipSizeX,ChipSizeY,DieGapLeft,DieGapRight,DieGapTop,DieGapBottom,ANGLE,TargetBin,placement_offset_x_mm,placement_offset_y_mm";

        private const int RawCacheFileLimit = 8;
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly object QueueSyncRoot = new object();
        private static readonly object FileSyncRoot = new object();
        private static readonly object SessionSyncRoot = new object();
        private static readonly Queue<WriteRequest> PendingItems = new Queue<WriteRequest>();
        private static readonly Dictionary<string, DateTime> InputSessionStarts =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DateTime> InputDieSessionStarts =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DateTime> OutputSessionStarts =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, RecipeMetadataCacheEntry> RecipeMetadataCache =
            new Dictionary<string, RecipeMetadataCacheEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, HashSet<string>> RawLineCache =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static bool _writerRunning;

        public static void EnqueueBottomResult(
            string recipeName,
            string lotId,
            DieMaterial die,
            DieInspectionRecord bottomRecord)
        {
            try
            {
                if (die == null ||
                    bottomRecord == null ||
                    !string.Equals(bottomRecord.InspectionType, "Bottom", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                DateTime eventAt = ResolveEventTime(bottomRecord.UpdatedAt);
                DateTime firstPickAt = ResolveInputPickStart(die, eventAt);
                DateTime sessionStartedAt = ResolveInputSessionStart(
                    recipeName,
                    lotId,
                    die,
                    firstPickAt);
                string inputWaferId = string.IsNullOrWhiteSpace(die.WaferID_Input)
                    ? "UNKNOWN_INPUT_WAFER"
                    : die.WaferID_Input.Trim();
                string stem = SafeFileName(inputWaferId) + "_" +
                    sessionStartedAt.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
                string inputDir = Path.Combine(MaterialSnapshotStore.RootDir, "INPUT");
                string raw = ReadRawMeasurement(bottomRecord, "BottomVisionRaw");

                Enqueue(new WriteRequest
                {
                    CsvPath = Path.Combine(inputDir, stem + ".csv"),
                    CsvPreamble = InputHeader,
                    CsvLine = BuildInputResultLine(
                        recipeName,
                        lotId,
                        die,
                        null,
                        bottomRecord,
                        null),
                    CsvKey = die.DieId,
                    RawPath = Path.Combine(inputDir, "Raw", stem + ".txt"),
                    RawLine = raw,
                    FailureCode = "INPUT-BOTTOM-RESULT-WRITE"
                });
            }
            catch (Exception ex)
            {
                LogFailure("INPUT-BOTTOM-RESULT-QUEUE", "", ex);
            }
        }

        public static void EnqueuePlaceResult(
            string recipeName,
            string lotId,
            QMC.CDT320.BinSide outputSide,
            WaferMaterial outputWafer,
            DieMaterial die,
            OutputStageReceiveTarget receiveTarget)
        {
            try
            {
                if (outputWafer == null || die == null)
                    return;

                DieInspectionRecord placeRecord = FindInspection(die, "OutputPlaceVision");
                if (placeRecord == null)
                    return;

                DateTime eventAt = ResolveEventTime(placeRecord.UpdatedAt);
                DieInspectionRecord bottomRecord = FindInspection(die, "Bottom");
                string inputWaferId = string.IsNullOrWhiteSpace(die.WaferID_Input)
                    ? "UNKNOWN_INPUT_WAFER"
                    : die.WaferID_Input.Trim();
                DateTime inputSessionStartedAt = ResolveInputSessionStart(
                    recipeName,
                    lotId,
                    die,
                    ResolveInputPickStart(die, eventAt));
                string inputStem = SafeFileName(inputWaferId) + "_" +
                    inputSessionStartedAt.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
                string inputDir = Path.Combine(MaterialSnapshotStore.RootDir, "INPUT");
                OutputReceiveSlotMaterial slot = ResolveSlot(outputWafer, die, receiveTarget);

                if (bottomRecord != null)
                {
                    Enqueue(new WriteRequest
                    {
                        CsvPath = Path.Combine(inputDir, inputStem + ".csv"),
                        CsvPreamble = InputHeader,
                        CsvLine = BuildInputResultLine(
                            recipeName,
                            lotId,
                            die,
                            slot,
                            bottomRecord,
                            placeRecord),
                        CsvKey = die.DieId,
                        FailureCode = "INPUT-BOTTOM-RESULT-WRITE"
                    });
                }

                string outputSessionKey = BuildOutputSessionKey(recipeName, lotId, outputWafer);
                DateTime outputSessionStartedAt = ResolveSessionStart(
                    OutputSessionStarts,
                    outputSessionKey,
                    IsValidDateTime(die.PickedAt) ? die.PickedAt : eventAt);

                Enqueue(new WriteRequest
                {
                    Place = BuildPlacePayload(
                        recipeName,
                        lotId,
                        outputSide,
                        outputWafer,
                        die,
                        slot,
                        bottomRecord,
                        placeRecord,
                        outputSessionStartedAt,
                        eventAt),
                    FailureCode = "OUTPUT-PLACE-RESULT-WRITE"
                });
            }
            catch (Exception ex)
            {
                LogFailure("OUTPUT-PLACE-RESULT-QUEUE", "", ex);
            }
        }

        private static void Enqueue(WriteRequest item)
        {
            if (item == null)
                return;

            lock (QueueSyncRoot)
            {
                PendingItems.Enqueue(item);
                if (_writerRunning)
                    return;

                _writerRunning = true;
                try
                {
                    Task.Run((Action)WriteQueuedItems);
                }
                catch
                {
                    _writerRunning = false;
                    throw;
                }
            }
        }

        private static void WriteQueuedItems()
        {
            try
            {
                while (true)
                {
                    WriteRequest item;
                    lock (QueueSyncRoot)
                    {
                        if (PendingItems.Count == 0)
                            return;

                        item = PendingItems.Dequeue();
                    }

                    try
                    {
                        if (item.Place != null)
                            WritePlaceItem(item);
                        else
                            WriteGenericItem(item);
                    }
                    catch (Exception ex)
                    {
                        LogFailure(item.FailureCode, ResolveRequestPath(item), ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogFailure("INSPECTION-RESULT-WORKER", "", ex);
            }
            finally
            {
                bool restart;
                lock (QueueSyncRoot)
                {
                    _writerRunning = false;
                    restart = PendingItems.Count > 0;
                    if (restart)
                        _writerRunning = true;
                }

                if (restart)
                {
                    try
                    {
                        Task.Run((Action)WriteQueuedItems);
                    }
                    catch (Exception ex)
                    {
                        lock (QueueSyncRoot)
                        {
                            _writerRunning = false;
                        }
                        LogFailure("INSPECTION-RESULT-WORKER-RESTART", "", ex);
                    }
                }
            }
        }

        private static void WriteGenericItem(WriteRequest item)
        {
            if (item == null)
                return;

            lock (FileSyncRoot)
            {
                if (!string.IsNullOrWhiteSpace(item.CsvPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(item.CsvPath));
                    EnsureInputCsvPreamble(item.CsvPath, item.CsvPreamble);

                    if (!string.IsNullOrWhiteSpace(item.CsvLine))
                    {
                        UpsertCsvLine(
                            item.CsvPath,
                            1,
                            new[] { 1 },
                            item.CsvKey,
                            item.CsvLine);
                    }
                }

                AppendRaw(item.RawPath, item.RawLine);
            }
        }

        private static void WritePlaceItem(WriteRequest request)
        {
            PlaceWritePayload place = request.Place;
            RecipeMetadata metadata = ResolveRecipeMetadata(place.RecipeName);
            string equipmentId = string.IsNullOrWhiteSpace(metadata.MachineNumber)
                ? "CDT-320"
                : metadata.MachineNumber.Trim();
            string stem = "AK_DT_" + SafeFileName(place.OutputWaferId) + "_" +
                SafeFileName(equipmentId) + "_" +
                place.SessionStartedAt.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
            string outputDir = Path.Combine(MaterialSnapshotStore.RootDir, "OUTPUT");
            string csvPath = Path.Combine(outputDir, stem + ".csv");
            string rawPath = Path.Combine(outputDir, "Raw", stem + ".txt");

            lock (FileSyncRoot)
            {
                AppendRaw(rawPath, place.RawLine);
                if (!place.HasOutputCoordinates)
                {
                    LogFailure(
                        "OUTPUT-PLACE-TARGET-MISSING",
                        place.OutputWaferId,
                        new InvalidOperationException(
                            "Place 결과의 Output map Y/X가 결정되지 않아 결과 CSV 행을 기록하지 않았습니다."));
                    return;
                }

                if (!place.HasBottomCorrection)
                {
                    LogFailure(
                        "OUTPUT-PLACE-BOTTOM-DATA-MISSING",
                        place.OutputWaferId,
                        new InvalidOperationException(
                            "Place 결과에 대응하는 Bottom item offset X/Y가 없어 결과 CSV 행을 기록하지 않았습니다."));
                    return;
                }

                Directory.CreateDirectory(outputDir);
                EnsureOutputCsvPreamble(csvPath, place, metadata);

                UpsertOutputCsvLine(csvPath, place, metadata);
            }
        }

        private static void EnsureInputCsvPreamble(string path, string preamble)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(preamble))
                return;

            if (!File.Exists(path))
            {
                WriteAllLinesAtomic(path, new[] { preamble });
                return;
            }

            List<string> lines = File.ReadAllLines(path, Utf8WithoutBom).ToList();
            if (lines.Count == 0)
                lines.Add(preamble);
            else if (!string.Equals(lines[0], preamble, StringComparison.Ordinal))
                lines[0] = preamble;
            else
                return;

            WriteAllLinesAtomic(path, lines);
        }

        private static void EnsureOutputCsvPreamble(
            string path,
            PlaceWritePayload place,
            RecipeMetadata metadata)
        {
            string summary = BuildOutputSummaryLine(place, metadata, place.EventAt, place.TotalCount);
            if (!File.Exists(path))
            {
                WriteAllLinesAtomic(
                    path,
                    new[] { OutputSummaryHeader, summary, OutputDieHeader });
                return;
            }

            List<string> lines = File.ReadAllLines(path, Utf8WithoutBom).ToList();
            if (lines.Count < 3)
            {
                WriteAllLinesAtomic(
                    path,
                    new[] { OutputSummaryHeader, summary, OutputDieHeader });
                return;
            }

            bool changed = false;
            if (!string.Equals(lines[0], OutputSummaryHeader, StringComparison.Ordinal))
            {
                lines[0] = OutputSummaryHeader;
                changed = true;
            }
            if (!string.Equals(lines[2], OutputDieHeader, StringComparison.Ordinal))
            {
                lines[2] = OutputDieHeader;
                changed = true;
            }
            if (changed)
                WriteAllLinesAtomic(path, lines);
        }

        private static void UpsertOutputCsvLine(
            string csvPath,
            PlaceWritePayload place,
            RecipeMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
                return;

            List<string> lines = File.ReadAllLines(csvPath, Utf8WithoutBom).ToList();
            if (lines.Count < 3)
                throw new InvalidDataException("OUTPUT 검사 결과 CSV 기본 헤더가 올바르지 않습니다. path=" + csvPath);

            bool replaced = false;
            for (int i = 3; i < lines.Count; i++)
            {
                string currentKey = BuildCsvKey(lines[i], new[] { 5, 6, 7 });
                if (!string.Equals(currentKey, place.DetailKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                lines[i] = place.DetailLine;
                replaced = true;
                break;
            }

            if (!replaced)
                lines.Add(place.DetailLine);

            int detailCount = Math.Max(0, lines.Count - 3);
            lines[1] = BuildOutputSummaryLine(place, metadata, place.EventAt, detailCount);
            WriteAllLinesAtomic(csvPath, lines);
        }

        private static void AppendRaw(string path, string raw)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(raw))
                return;

            string text = raw;
            if (!text.EndsWith("\r\n", StringComparison.Ordinal) &&
                !text.EndsWith("\n", StringComparison.Ordinal))
            {
                text += Environment.NewLine;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            HashSet<string> knownLines;
            if (!RawLineCache.TryGetValue(path, out knownLines))
            {
                TrimRawLineCache(path);
                knownLines = File.Exists(path)
                    ? new HashSet<string>(File.ReadLines(path, Utf8WithoutBom), StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal);
                RawLineCache[path] = knownLines;
            }

            string normalizedRaw = raw.TrimEnd('\r', '\n');
            if (knownLines.Contains(normalizedRaw))
                return;

            File.AppendAllText(path, text, Utf8WithoutBom);
            knownLines.Add(normalizedRaw);
        }

        private static void TrimRawLineCache(string currentPath)
        {
            while (RawLineCache.Count >= RawCacheFileLimit)
            {
                string removeKey = RawLineCache.Keys.FirstOrDefault(key =>
                    !string.Equals(key, currentPath, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(removeKey))
                    return;
                RawLineCache.Remove(removeKey);
            }
        }

        private static void UpsertCsvLine(
            string path,
            int headerLineCount,
            int[] keyColumns,
            string expectedKey,
            string line)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                string.IsNullOrWhiteSpace(line) ||
                string.IsNullOrWhiteSpace(expectedKey))
            {
                return;
            }

            List<string> lines = File.Exists(path)
                ? File.ReadAllLines(path, Utf8WithoutBom).ToList()
                : new List<string>();
            bool replaced = false;
            for (int i = Math.Max(0, headerLineCount); i < lines.Count; i++)
            {
                string currentKey = BuildCsvKey(lines[i], keyColumns);
                if (!string.Equals(currentKey, expectedKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                lines[i] = MergeInputResultLine(lines[i], line);
                replaced = true;
                break;
            }

            if (!replaced)
                lines.Add(line);

            WriteAllLinesAtomic(path, lines);
        }

        private static string MergeInputResultLine(string existingLine, string incomingLine)
        {
            List<string> existing = ParseCsvLine(existingLine);
            List<string> incoming = ParseCsvLine(incomingLine);
            const int placeResultCodeIndex = 54;
            bool existingHasPlace = existing.Count > placeResultCodeIndex &&
                !string.IsNullOrWhiteSpace(existing[placeResultCodeIndex]);
            bool incomingHasPlace = incoming.Count > placeResultCodeIndex &&
                !string.IsNullOrWhiteSpace(incoming[placeResultCodeIndex]);

            if (!existingHasPlace || incomingHasPlace)
                return incomingLine;

            // Bottom RESULT가 재수신되어도 이미 채워진 Place 결과 17열은 보존한다.
            int prePlaceColumnCount = 38;
            while (existing.Count < prePlaceColumnCount)
                existing.Add("");
            for (int i = 0; i < Math.Min(prePlaceColumnCount, incoming.Count); i++)
                existing[i] = incoming[i];

            return CsvLine(existing.Cast<object>().ToList());
        }

        private static string BuildCsvKey(string line, int[] keyColumns)
        {
            if (string.IsNullOrWhiteSpace(line) || keyColumns == null || keyColumns.Length == 0)
                return "";

            List<string> fields = ParseCsvLine(line);
            var parts = new List<string>(keyColumns.Length);
            for (int i = 0; i < keyColumns.Length; i++)
            {
                int index = keyColumns[i];
                if (index < 0 || index >= fields.Count)
                    return "";
                parts.Add(fields[index] ?? "");
            }

            return string.Join("\u001f", parts);
        }

        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < (line ?? "").Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (c == ',' && !quoted)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            fields.Add(field.ToString());
            return fields;
        }

        private static void WriteAllLinesAtomic(string path, IList<string> lines)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string tempPath = Path.Combine(
                directory,
                Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllLines(tempPath, lines, Utf8WithoutBom);
                if (File.Exists(path))
                    File.Replace(tempPath, path, null);
                else
                    File.Move(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (Exception ex)
                    {
                        LogFailure("INSPECTION-RESULT-TEMP-CLEANUP", tempPath, ex);
                    }
                }
            }
        }

        private static string BuildInputResultLine(
            string recipeName,
            string lotId,
            DieMaterial die,
            OutputReceiveSlotMaterial outputSlot,
            DieInspectionRecord bottomRecord,
            DieInspectionRecord placeRecord)
        {
            DieInspectionRecord inputVisionRecord = FindInspection(die, "InputPickVision");
            DieInspectionRecord pickRecord = FindInspection(die, "PickUp");
            InspectionAlignmentSnapshot pickAlignment =
                FindAlignment(inputVisionRecord) ?? FindAlignment(pickRecord);
            InspectionAlignmentSnapshot bottomAlignment = FindAlignment(bottomRecord);
            VisionOffset pickOffset = inputVisionRecord != null ? inputVisionRecord.Offset : null;

            int pickerIndex = die.PickedPickerNo > 0
                ? die.PickedPickerNo - 1
                : ReadIntMeasurement(bottomRecord, "BottomPickerNo", -1) - 1;
            int inputMapY = ResolveMapIndex(die.Wafer_OriginalIndexY, die.Wafer_IndexY);
            int inputMapX = ResolveMapIndex(die.Wafer_OriginalIndexX, die.Wafer_IndexX);
            int outputMapSourceY = outputSlot != null ? outputSlot.DieMapY : die.Bin_IndexY;
            int outputMapSourceX = outputSlot != null ? outputSlot.DieMapX : die.Bin_IndexX;
            int outputMapY = outputMapSourceY >= 0 ? outputMapSourceY + 1 : -1;
            int outputMapX = outputMapSourceX >= 0 ? outputMapSourceX + 1 : -1;

            double bottomItemOffsetX = ReadVisionDouble(
                bottomRecord,
                "BottomVision_",
                "bottom_item_offset_x",
                ReadMeasurement(bottomRecord, "BottomItemOffsetX"));
            double bottomItemOffsetY = ReadVisionDouble(
                bottomRecord,
                "BottomVision_",
                "bottom_item_offset_y",
                ReadMeasurement(bottomRecord, "BottomItemOffsetY"));
            double bottomAngle = ReadVisionDouble(
                bottomRecord,
                "BottomVision_",
                "bottom_item_angle",
                bottomRecord != null && bottomRecord.Offset != null ? bottomRecord.Offset.R : double.NaN);

            var values = new List<object>(55);

            // 기본 정보: 7열
            values.Add(lotId);
            values.Add(die.DieId);
            values.Add("Exist");
            values.Add(recipeName);
            values.Add(pickerIndex >= 0 ? pickerIndex.ToString(CultureInfo.InvariantCulture) : "");
            values.Add(ToLegacyResult(bottomRecord));
            values.Add(JoinList(bottomRecord != null ? bottomRecord.NgCodes : die.NgCodes));

            // Loading/Pick 정보: 10열
            values.Add(die.WaferID_Input);
            values.Add(FormatIndex(inputMapY));
            values.Add(FormatIndex(inputMapX));
            values.Add(FormatMicrometers(pickAlignment != null ? pickAlignment.X : double.NaN));
            values.Add(FormatMicrometers(pickAlignment != null ? pickAlignment.Y : double.NaN));
            values.Add(FormatMicrometers(pickOffset != null ? pickOffset.X : double.NaN));
            values.Add(FormatMicrometers(pickOffset != null ? pickOffset.Y : double.NaN));
            values.Add(Format(pickOffset != null ? pickOffset.R : double.NaN));
            values.Add(FormatDateTime(inputVisionRecord != null ? inputVisionRecord.CreatedAt :
                (pickRecord != null ? pickRecord.CreatedAt : DateTime.MinValue)));
            values.Add(FormatDateTime(IsValidDateTime(die.PickedAt) ? die.PickedAt :
                (pickRecord != null ? pickRecord.UpdatedAt : DateTime.MinValue)));

            // Unloading/Pre-Place 정보: 10열
            values.Add(die.WaferID_Output);
            values.Add(FormatIndex(outputMapY));
            values.Add(FormatIndex(outputMapX));
            values.Add(FormatMicrometers(bottomAlignment != null ? bottomAlignment.X : double.NaN));
            values.Add(FormatMicrometers(bottomAlignment != null ? bottomAlignment.Y : double.NaN));
            values.Add(FormatMicrometers(bottomItemOffsetX));
            values.Add(FormatMicrometers(bottomItemOffsetY));
            values.Add(Format(bottomAngle));
            values.Add(FormatDateTime(bottomRecord != null ? bottomRecord.CreatedAt : DateTime.MinValue));
            values.Add(FormatDateTime(bottomRecord != null ? bottomRecord.UpdatedAt : DateTime.MinValue));

            // Bottom 결과: 7열
            values.Add(FormatBottomSize(bottomRecord, "bottom_item_width", "bottom_width_mm"));
            values.Add(FormatBottomSize(bottomRecord, "bottom_item_height", "bottom_height_mm"));
            values.Add(FormatBottomMetric(bottomRecord, "bottom_item_chipping_top", true));
            values.Add(FormatBottomMetric(bottomRecord, "bottom_item_chipping_right", true));
            values.Add(FormatBottomMetric(bottomRecord, "bottom_item_chipping_bottom", true));
            values.Add(FormatBottomMetric(bottomRecord, "bottom_item_chipping_left", true));
            // 샘플의 Size와 Vision의 t_foreign(count)는 의미가 다르므로 임의 변환하지 않는다.
            values.Add("");

            // 현재 Side 프로토콜에는 샘플 4방향 Chipping과 직접 대응하는 값이 없다: 4열
            values.Add("");
            values.Add("");
            values.Add("");
            values.Add("");

            // Place 결과: 17열
            values.Add(FormatPlaceMetric(placeRecord, "placement_offset_x_mm", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_offset_y_mm", true));
            values.Add(FormatPlaceAngle(placeRecord));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_top_gap_min", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_top_gap_max", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_top_gap_avg", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_left_min", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_left_max", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_left_gap_avg", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_bottom_min", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_bottom_gap", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_bottom_gap_avg", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_right_min", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_right_max", true));
            values.Add(FormatPlaceMetric(placeRecord, "placement_item_right_gap_avg", true));
            values.Add(FormatPlaceAngle(placeRecord));
            values.Add(placeRecord == null
                ? ""
                : (placeRecord.Result == MaterialInspectionResult.Ok ? "0" : "1"));

            if (values.Count != 55)
                throw new InvalidOperationException("INPUT 검사 결과 CSV 열 수가 55가 아닙니다. count=" + values.Count);

            return CsvLine(values);
        }

        private static PlaceWritePayload BuildPlacePayload(
            string recipeName,
            string lotId,
            QMC.CDT320.BinSide outputSide,
            WaferMaterial outputWafer,
            DieMaterial die,
            OutputReceiveSlotMaterial outputSlot,
            DieInspectionRecord bottomRecord,
            DieInspectionRecord placeRecord,
            DateTime sessionStartedAt,
            DateTime eventAt)
        {
            double bottomItemOffsetX = ReadVisionDouble(
                bottomRecord,
                "BottomVision_",
                "bottom_item_offset_x",
                ReadMeasurement(bottomRecord, "BottomItemOffsetX"));
            double bottomItemOffsetY = ReadVisionDouble(
                bottomRecord,
                "BottomVision_",
                "bottom_item_offset_y",
                ReadMeasurement(bottomRecord, "BottomItemOffsetY"));
            int inputMapY = ResolveMapIndex(die.Wafer_OriginalIndexY, die.Wafer_IndexY);
            int inputMapX = ResolveMapIndex(die.Wafer_OriginalIndexX, die.Wafer_IndexX);
            int outputMapSourceY = outputSlot != null ? outputSlot.DieMapY : die.Bin_IndexY;
            int outputMapSourceX = outputSlot != null ? outputSlot.DieMapX : die.Bin_IndexX;
            int outputMapY = outputMapSourceY >= 0 ? outputMapSourceY + 1 : -1;
            int outputMapX = outputMapSourceX >= 0 ? outputMapSourceX + 1 : -1;
            int targetBin = outputSlot != null ? outputSlot.BinCode : die.Output_BinCode;
            int totalCount = ResolveOutputTotalCount(outputWafer);
            string outputWaferId = string.IsNullOrWhiteSpace(outputWafer.WaferId)
                ? "UNKNOWN_OUTPUT_WAFER"
                : outputWafer.WaferId.Trim();
            string formattedOutputMapY = FormatPaddedIndex(outputMapY);
            string formattedOutputMapX = FormatPaddedIndex(outputMapX);

            var detail = new List<object>(18)
            {
                die.InputSequenceNo,
                die.PickedPickerNo > 0 ? die.PickedPickerNo - 1 : -1,
                die.WaferID_Input,
                FormatIndex(inputMapY),
                FormatIndex(inputMapX),
                outputWaferId,
                formattedOutputMapY,
                formattedOutputMapX,
                FormatBottomSize(bottomRecord, "bottom_item_width", "bottom_width_mm"),
                FormatBottomSize(bottomRecord, "bottom_item_height", "bottom_height_mm"),
                FormatPlaceMetric(placeRecord, "placement_item_left_gap_avg", true),
                FormatPlaceMetric(placeRecord, "placement_item_right_gap_avg", true),
                FormatPlaceMetric(placeRecord, "placement_item_top_gap_avg", true),
                FormatPlaceMetric(placeRecord, "placement_item_bottom_gap_avg", true),
                FormatPlaceAngle(placeRecord),
                targetBin,
                Format(bottomItemOffsetX),
                Format(bottomItemOffsetY)
            };

            if (detail.Count != 18)
                throw new InvalidOperationException("OUTPUT 검사 결과 CSV Die 열 수가 18이 아닙니다. count=" + detail.Count);

            return new PlaceWritePayload
            {
                RecipeName = recipeName ?? "",
                LotId = lotId ?? "",
                OutputSide = outputSide,
                OutputWaferId = outputWaferId,
                OutputCassetteId = outputWafer.OutputCassetteId ?? "",
                SessionStartedAt = sessionStartedAt,
                EventAt = eventAt,
                TotalCount = totalCount,
                DetailLine = CsvLine(detail),
                DetailKey = string.Join(
                    "\u001f",
                    new[] { outputWaferId, formattedOutputMapY, formattedOutputMapX }),
                RawLine = ReadRawMeasurement(placeRecord, "OutputVisionRaw"),
                HasBottomCorrection = IsFinite(bottomItemOffsetX) && IsFinite(bottomItemOffsetY),
                HasOutputCoordinates = outputMapSourceY >= 0 && outputMapSourceX >= 0
            };
        }

        private static string BuildOutputSummaryLine(
            PlaceWritePayload place,
            RecipeMetadata metadata,
            DateTime endAt,
            int totalCount)
        {
            string lotId = !string.IsNullOrWhiteSpace(place.LotId)
                ? place.LotId
                : metadata.LotId;
            string machineNumber = string.IsNullOrWhiteSpace(metadata.MachineNumber)
                ? "CDT-320"
                : metadata.MachineNumber;
            string placeCassetteId = !string.IsNullOrWhiteSpace(place.OutputCassetteId)
                ? place.OutputCassetteId
                : metadata.OutputCassetteId;

            var values = new List<object>(12)
            {
                lotId,
                place.SessionStartedAt.ToString("yyyy-MM-dd-HH:mm", CultureInfo.InvariantCulture),
                endAt.ToString("yyyy-MM-dd-HH:mm", CultureInfo.InvariantCulture),
                machineNumber,
                totalCount,
                metadata.PickOrigin,
                metadata.PlaceOrigin,
                metadata.InputCassetteId,
                placeCassetteId,
                metadata.ColletModelNumber,
                metadata.ColletIdNumber,
                1
            };

            return CsvLine(values);
        }

        private static RecipeMetadata ResolveRecipeMetadata(string recipeName)
        {
            string key = recipeName ?? "";
            string fileName = key.EndsWith(".Project", StringComparison.OrdinalIgnoreCase)
                ? key
                : key + ".Project";
            string recipePath = "";
            DateTime lastWriteUtc = DateTime.MinValue;
            long fileLength = -1L;
            try
            {
                recipePath = Path.Combine(RecipeStore.Dir, fileName);
                if (File.Exists(recipePath))
                {
                    lastWriteUtc = File.GetLastWriteTimeUtc(recipePath);
                    fileLength = new FileInfo(recipePath).Length;
                }
            }
            catch (Exception ex)
            {
                LogFailure("OUTPUT-PLACE-RECIPE-FILE-INFO", recipePath, ex);
            }
            RecipeMetadataCacheEntry cached;
            lock (RecipeMetadataCache)
            {
                if (RecipeMetadataCache.TryGetValue(key, out cached) &&
                    cached != null &&
                    cached.LastWriteUtc == lastWriteUtc &&
                    cached.FileLength == fileLength)
                {
                    return cached.Metadata;
                }
            }

            var metadata = new RecipeMetadata();
            bool loaded = false;
            try
            {
                RecipeProject project = !string.IsNullOrWhiteSpace(recipeName)
                    ? RecipeStore.Load(recipeName)
                    : null;
                if (project != null)
                {
                    metadata.LotId = project.LotId ?? "";
                    metadata.MachineNumber = project.MachineNumber ?? "";
                    metadata.InputCassetteId = project.InputCassetteId ?? "";
                    metadata.OutputCassetteId = project.OutputCassetteId ?? "";
                    metadata.ColletModelNumber = project.ColletModelNum ?? "";
                    metadata.ColletIdNumber = project.ColletLotNum ?? "";
                    metadata.PickOrigin = project.InputPickup != null
                        ? project.InputPickup.StartCorner.ToString()
                        : "";
                    metadata.PlaceOrigin = project.OutputPickup != null
                        ? project.OutputPickup.StartCorner.ToString()
                        : "";
                    loaded = true;
                }
            }
            catch (Exception ex)
            {
                LogFailure("OUTPUT-PLACE-RECIPE-METADATA", recipeName, ex);
            }

            if (loaded)
            {
                lock (RecipeMetadataCache)
                {
                    RecipeMetadataCache[key] = new RecipeMetadataCacheEntry
                    {
                        LastWriteUtc = lastWriteUtc,
                        FileLength = fileLength,
                        Metadata = metadata
                    };
                }
            }

            return metadata;
        }

        private static OutputReceiveSlotMaterial ResolveSlot(
            WaferMaterial outputWafer,
            DieMaterial die,
            OutputStageReceiveTarget receiveTarget)
        {
            if (outputWafer == null || outputWafer.OutputReceiveSlots == null)
                return null;

            if (receiveTarget != null)
            {
                OutputReceiveSlotMaterial byOrder = outputWafer.OutputReceiveSlots.FirstOrDefault(s =>
                    s != null && s.OrderIndex == receiveTarget.OrderIndex);
                if (byOrder != null)
                    return byOrder;
            }

            return outputWafer.OutputReceiveSlots.FirstOrDefault(s =>
                s != null &&
                !string.IsNullOrWhiteSpace(s.DieUid) &&
                string.Equals(s.DieUid, die.DieId, StringComparison.OrdinalIgnoreCase));
        }

        private static int ResolveOutputTotalCount(WaferMaterial outputWafer)
        {
            if (outputWafer == null)
                return 0;
            if (outputWafer.OutputReceiveTotalCount > 0)
                return outputWafer.OutputReceiveTotalCount;
            if (outputWafer.OutputReceiveSlots == null)
                return 0;
            return outputWafer.OutputReceiveSlots.Count(s => s != null && s.IsTarget);
        }

        private static DieInspectionRecord FindInspection(DieMaterial die, string inspectionType)
        {
            if (die == null || die.Inspections == null)
                return null;

            return die.Inspections.FirstOrDefault(r =>
                r != null &&
                string.Equals(r.InspectionType, inspectionType, StringComparison.OrdinalIgnoreCase));
        }

        private static InspectionAlignmentSnapshot FindAlignment(DieInspectionRecord record)
        {
            if (record == null || record.Alignments == null)
                return null;
            return record.Alignments.FirstOrDefault(a => a != null && a.IsValid) ??
                record.Alignments.FirstOrDefault(a => a != null);
        }

        private static string ReadRawMeasurement(DieInspectionRecord record, string name)
        {
            InspectionMeasurement measurement = FindMeasurement(record, name);
            return measurement != null ? measurement.RawValue ?? "" : "";
        }

        private static double ReadMeasurement(DieInspectionRecord record, string name)
        {
            InspectionMeasurement measurement = FindMeasurement(record, name);
            if (measurement == null)
                return double.NaN;

            double value;
            if (!string.IsNullOrWhiteSpace(measurement.RawValue) &&
                double.TryParse(
                    measurement.RawValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value) &&
                IsFinite(value))
            {
                return value;
            }

            return IsFinite(measurement.Value) ? measurement.Value : double.NaN;
        }

        private static int ReadIntMeasurement(
            DieInspectionRecord record,
            string name,
            int fallback)
        {
            double value = ReadMeasurement(record, name);
            if (!IsFinite(value))
                return fallback;
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static InspectionMeasurement FindMeasurement(
            DieInspectionRecord record,
            string name)
        {
            if (record == null || record.Measurements == null)
                return null;

            return record.Measurements.FirstOrDefault(m =>
                m != null &&
                string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static double ReadVisionDouble(
            DieInspectionRecord record,
            string prefix,
            string key,
            double fallback)
        {
            double value = ReadMeasurement(record, (prefix ?? "") + (key ?? ""));
            return IsFinite(value) ? value : fallback;
        }

        private static string FormatBottomSize(
            DieInspectionRecord record,
            string itemKey,
            string fallbackKey)
        {
            double value = ReadVisionDouble(
                record,
                "BottomVision_",
                itemKey,
                double.NaN);
            if (!IsFinite(value))
            {
                value = ReadVisionDouble(
                    record,
                    "BottomVision_",
                    fallbackKey,
                    double.NaN);
            }

            return FormatMicrometers(value);
        }

        private static string FormatBottomMetric(
            DieInspectionRecord record,
            string key,
            bool convertMillimeterToMicrometer)
        {
            double value = ReadVisionDouble(
                record,
                "BottomVision_",
                key,
                double.NaN);
            return convertMillimeterToMicrometer
                ? FormatMicrometers(value)
                : Format(value);
        }

        private static string FormatPlaceMetric(
            DieInspectionRecord record,
            string key,
            bool convertMillimeterToMicrometer)
        {
            double value = ReadVisionDouble(
                record,
                "OutputVision_",
                key,
                double.NaN);
            return convertMillimeterToMicrometer
                ? FormatMicrometers(value)
                : Format(value);
        }

        private static string FormatPlaceAngle(DieInspectionRecord record)
        {
            double value = ReadVisionDouble(
                record,
                "OutputVision_",
                "placement_angle_deg",
                double.NaN);
            if (!IsFinite(value))
            {
                value = ReadVisionDouble(
                    record,
                    "OutputVision_",
                    "placement_item_angle",
                    double.NaN);
            }

            return Format(value);
        }

        private static string ToLegacyResult(DieInspectionRecord record)
        {
            if (record == null)
                return "Unknown";
            if (record.Result == MaterialInspectionResult.Ok)
                return "Good";
            if (record.Result == MaterialInspectionResult.Ng)
                return "NG";
            return "Unknown";
        }

        private static string JoinList(IEnumerable<string> values)
        {
            if (values == null)
                return "";
            return string.Join("|", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()));
        }

        private static int ResolveMapIndex(int original, int local)
        {
            return original >= 0 ? original : local;
        }

        private static string FormatIndex(int value)
        {
            return value >= 0 ? value.ToString(CultureInfo.InvariantCulture) : "";
        }

        private static string FormatPaddedIndex(int value)
        {
            return value >= 0 ? value.ToString("D3", CultureInfo.InvariantCulture) : "";
        }

        private static string FormatMicrometers(double millimeters)
        {
            return IsFinite(millimeters)
                ? Format(millimeters * 1000.0)
                : "";
        }

        private static string Format(double value)
        {
            if (!IsFinite(value))
                return "";
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string FormatDateTime(DateTime value)
        {
            return IsValidDateTime(value)
                ? value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                : "";
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsValidDateTime(DateTime value)
        {
            return value > new DateTime(2000, 1, 1);
        }

        private static DateTime ResolveEventTime(DateTime value)
        {
            return IsValidDateTime(value) ? value : DateTime.Now;
        }

        private static DateTime ResolveInputPickStart(DieMaterial die, DateTime fallback)
        {
            DieInspectionRecord inputVisionRecord = FindInspection(die, "InputPickVision");
            if (inputVisionRecord != null && IsValidDateTime(inputVisionRecord.CreatedAt))
                return inputVisionRecord.CreatedAt;

            DieInspectionRecord pickRecord = FindInspection(die, "PickUp");
            if (pickRecord != null && IsValidDateTime(pickRecord.CreatedAt))
                return pickRecord.CreatedAt;

            if (die != null && IsValidDateTime(die.PickedAt))
                return die.PickedAt;

            return ResolveEventTime(fallback);
        }

        private static DateTime ResolveSessionStart(
            Dictionary<string, DateTime> sessions,
            string key,
            DateTime candidate)
        {
            lock (SessionSyncRoot)
            {
                DateTime existing;
                if (sessions.TryGetValue(key, out existing))
                    return existing;

                DateTime resolved = ResolveEventTime(candidate);
                sessions[key] = resolved;
                return resolved;
            }
        }

        private static DateTime ResolveInputSessionStart(
            string recipeName,
            string lotId,
            DieMaterial die,
            DateTime candidate)
        {
            lock (SessionSyncRoot)
            {
                string dieSessionKey = BuildInputDieSessionKey(die);
                DateTime existing;
                if (InputDieSessionStarts.TryGetValue(dieSessionKey, out existing))
                    return existing;

                string waferSessionKey = BuildInputSessionKey(recipeName, lotId, die);
                if (!InputSessionStarts.TryGetValue(waferSessionKey, out existing))
                {
                    existing = ResolveEventTime(candidate);
                    InputSessionStarts[waferSessionKey] = existing;
                }

                InputDieSessionStarts[dieSessionKey] = existing;
                return existing;
            }
        }

        private static string BuildInputDieSessionKey(DieMaterial die)
        {
            if (die == null)
                return "UNKNOWN:0";

            DateTime pickedAt = IsValidDateTime(die.PickedAt)
                ? die.PickedAt
                : die.CreatedAt;
            return (die.DieId ?? "UNKNOWN") + ":" +
                pickedAt.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        private static string BuildInputSessionKey(
            string recipeName,
            string lotId,
            DieMaterial die)
        {
            string generation = ResolveInputWaferGeneration(die);
            return (recipeName ?? "") + "\u001f" +
                (lotId ?? "") + "\u001f" +
                (die != null ? die.WaferID_Input ?? "" : "") + "\u001f" +
                generation;
        }

        private static string ResolveInputWaferGeneration(DieMaterial die)
        {
            try
            {
                string waferId = die != null ? die.WaferID_Input : "";
                WaferMaterial wafer = MaterialStateService.State != null &&
                    MaterialStateService.State.Wafers != null
                    ? MaterialStateService.State.Wafers.FirstOrDefault(w =>
                        w != null &&
                        string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                    : null;
                if (wafer == null)
                    return "0";

                return wafer.InputStageProcessingGeneration.ToString(CultureInfo.InvariantCulture) +
                    ":" + wafer.CreatedAt.Ticks.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                LogFailure("INPUT-RESULT-SESSION-CONTEXT", die != null ? die.DieId : "", ex);
                return "0";
            }
        }

        private static string BuildOutputSessionKey(
            string recipeName,
            string lotId,
            WaferMaterial outputWafer)
        {
            return (recipeName ?? "") + "\u001f" +
                (lotId ?? "") + "\u001f" +
                (outputWafer != null ? outputWafer.WaferId ?? "" : "") + "\u001f" +
                (outputWafer != null
                    ? outputWafer.CreatedAt.Ticks.ToString(CultureInfo.InvariantCulture)
                    : "0");
        }

        private static string CsvLine(IList<object> values)
        {
            return string.Join(",", values.Select(Csv));
        }

        private static string Csv(object value)
        {
            string text;
            if (value == null)
                text = "";
            else if (value is double)
                text = Format((double)value);
            else if (value is float)
                text = ((float)value).ToString("0.######", CultureInfo.InvariantCulture);
            else if (value is IFormattable)
                text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            else
                text = value.ToString();

            if (text == null)
                text = "";
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            return text;
        }

        private static string SafeFileName(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "UNKNOWN" : value.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                text = text.Replace(c, '_');
            return text;
        }

        private static string ResolveRequestPath(WriteRequest item)
        {
            if (item == null)
                return "";
            if (!string.IsNullOrWhiteSpace(item.CsvPath))
                return item.CsvPath;
            if (item.Place != null)
                return item.Place.OutputWaferId;
            return item.RawPath ?? "";
        }

        private static void LogFailure(string code, string path, Exception ex)
        {
            try
            {
                Log.Write(
                    "Main",
                    "SYSTEM",
                    "InspectionResultFileWriter",
                    (code ?? "INSPECTION-RESULT-WRITE") +
                    " 검사 결과 파일 저장 실패. path=" + (path ?? "") +
                    ", error=" + (ex != null ? ex.Message : "") +
                    " - Failed");
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Trace.WriteLine(
                    "InspectionResultFileWriter 로그 기록 실패. error=" + logEx.Message);
            }
        }

        private sealed class WriteRequest
        {
            public string CsvPath { get; set; }
            public string CsvPreamble { get; set; }
            public string CsvLine { get; set; }
            public string CsvKey { get; set; }
            public string RawPath { get; set; }
            public string RawLine { get; set; }
            public PlaceWritePayload Place { get; set; }
            public string FailureCode { get; set; }
        }

        private sealed class PlaceWritePayload
        {
            public string RecipeName { get; set; }
            public string LotId { get; set; }
            public QMC.CDT320.BinSide OutputSide { get; set; }
            public string OutputWaferId { get; set; }
            public string OutputCassetteId { get; set; }
            public DateTime SessionStartedAt { get; set; }
            public DateTime EventAt { get; set; }
            public int TotalCount { get; set; }
            public string DetailLine { get; set; }
            public string DetailKey { get; set; }
            public string RawLine { get; set; }
            public bool HasBottomCorrection { get; set; }
            public bool HasOutputCoordinates { get; set; }
        }

        private sealed class RecipeMetadata
        {
            public string LotId { get; set; } = "";
            public string MachineNumber { get; set; } = "";
            public string InputCassetteId { get; set; } = "";
            public string OutputCassetteId { get; set; } = "";
            public string ColletModelNumber { get; set; } = "";
            public string ColletIdNumber { get; set; } = "";
            public string PickOrigin { get; set; } = "";
            public string PlaceOrigin { get; set; } = "";
        }

        private sealed class RecipeMetadataCacheEntry
        {
            public DateTime LastWriteUtc { get; set; }
            public long FileLength { get; set; }
            public RecipeMetadata Metadata { get; set; }
        }
    }
}
