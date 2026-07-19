using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Diagnostics.TactTime
{
    public sealed class TactTimeCsvReadProgress
    {
        public string Phase { get; set; } = "";
        public long BytesRead { get; set; }
        public long TotalBytes { get; set; }
        public int RecordCount { get; set; }

        public int Percent
        {
            get
            {
                if (TotalBytes <= 0)
                    return 0;

                return (int)Math.Max(0, Math.Min(100, BytesRead * 100L / TotalBytes));
            }
        }
    }

    public sealed class TactTimeRunInfo
    {
        public string RunId { get; set; } = "";
        public string EquipmentId { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string LotId { get; set; } = "";
        public string Mode { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public int RecordCount { get; set; }
        public int FailedCount { get; set; }
        public int StoppedCount { get; set; }

        public override string ToString()
        {
            string start = StartedAt == DateTime.MinValue ? "-" : StartedAt.ToString("HH:mm:ss");
            string end = EndedAt == DateTime.MinValue ? "-" : EndedAt.ToString("HH:mm:ss");
            string lot = string.IsNullOrWhiteSpace(LotId) ? "LOT -" : LotId;
            string mode = string.IsNullOrWhiteSpace(Mode) ? "MODE -" : Mode;
            string shortRun = string.IsNullOrWhiteSpace(RunId)
                ? "RUN -"
                : (RunId.Length > 8 ? RunId.Substring(0, 8) : RunId);
            return start + " ~ " + end + " / " + mode + " / " + lot +
                   " / " + RecordCount.ToString("N0") + "건 / " + shortRun;
        }
    }

    public sealed class TactTimeCsvIndexResult
    {
        public string FilePath { get; set; } = "";
        public IReadOnlyList<TactTimeRunInfo> Runs { get; set; } = new List<TactTimeRunInfo>().AsReadOnly();
        public int ParsedRecordCount { get; set; }
        public int SkippedRecordCount { get; set; }
        public bool IncompleteLastRecord { get; set; }
        public IReadOnlyList<string> Warnings { get; set; } = new List<string>().AsReadOnly();
    }

    public sealed class TactTimeCsvLoadResult
    {
        public string FilePath { get; set; } = "";
        public string RunId { get; set; } = "";
        public IReadOnlyList<TactTimeRecord> Records { get; set; } = new List<TactTimeRecord>().AsReadOnly();
        public int ParsedRecordCount { get; set; }
        public int SkippedRecordCount { get; set; }
        public bool IncompleteLastRecord { get; set; }
        public IReadOnlyList<string> Warnings { get; set; } = new List<string>().AsReadOnly();
    }

    public static class TactTimeCsvReader
    {
        public const string NoRunId = "<NO-RUN>";
        private const int MaxWarnings = 20;

        public static Task<TactTimeCsvIndexResult> IndexRunsAsync(
            string filePath,
            CancellationToken ct,
            IProgress<TactTimeCsvReadProgress> progress = null)
        {
            return Task.Run(() => IndexRuns(filePath, ct, progress), ct);
        }

        public static Task<TactTimeCsvLoadResult> LoadRunAsync(
            string filePath,
            string runId,
            CancellationToken ct,
            IProgress<TactTimeCsvReadProgress> progress = null)
        {
            return Task.Run(() => LoadRun(filePath, runId, ct, progress), ct);
        }

        public static TactTimeCsvIndexResult IndexRuns(
            string filePath,
            CancellationToken ct,
            IProgress<TactTimeCsvReadProgress> progress = null)
        {
            ValidateFilePath(filePath);

            var runs = new Dictionary<string, TactTimeRunInfo>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            int parsed = 0;
            int skipped = 0;
            bool incompleteLastRecord = false;

            using (var stream = OpenReadShared(filePath))
            using (var reader = new CsvRecordReader(stream))
            {
                CsvRecord headerRecord = reader.ReadRecord();
                if (headerRecord == null || !headerRecord.IsComplete)
                    throw new InvalidDataException("택타임 CSV 헤더를 읽을 수 없습니다.");

                Dictionary<string, int> columns = BuildColumnMap(headerRecord.Fields);
                ValidateRequiredColumns(columns);
                AddMissingOptionalColumnWarnings(columns, warnings);

                CsvRecord csvRecord;
                while ((csvRecord = reader.ReadRecord()) != null)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!csvRecord.IsComplete)
                    {
                        incompleteLastRecord = true;
                        AddWarning(warnings, "파일 끝의 미완성 CSV 레코드를 제외했습니다. record=" + csvRecord.RecordNumber);
                        break;
                    }

                    TactTimeRecord record;
                    string error;
                    string recordWarning;
                    if (!TryParseRecord(csvRecord.Fields, columns, out record, out error, out recordWarning))
                    {
                        skipped++;
                        AddWarning(warnings, "CSV 레코드를 제외했습니다. record=" + csvRecord.RecordNumber + ", reason=" + error);
                        ReportProgress(progress, "Run 목록 확인", stream, parsed + skipped, false);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(recordWarning))
                        AddWarning(warnings, "CSV 레코드 호환 처리. record=" + csvRecord.RecordNumber + ", reason=" + recordWarning);

                    parsed++;
                    string key = NormalizeRunId(record.RunId);
                    TactTimeRunInfo info;
                    if (!runs.TryGetValue(key, out info))
                    {
                        info = new TactTimeRunInfo
                        {
                            RunId = key,
                            EquipmentId = record.EquipmentId,
                            ProjectName = record.ProjectName,
                            LotId = record.LotId,
                            Mode = record.Mode,
                            StartedAt = record.StartedAt,
                            EndedAt = record.EndedAt
                        };
                        runs.Add(key, info);
                    }

                    IncludeRunRecord(info, record);
                    ReportProgress(progress, "Run 목록 확인", stream, parsed + skipped, false);
                }

                ReportProgress(progress, "Run 목록 확인", stream, parsed + skipped, true);
            }

            List<TactTimeRunInfo> orderedRuns = runs.Values
                .OrderByDescending(x => x.StartedAt)
                .ToList();
            return new TactTimeCsvIndexResult
            {
                FilePath = filePath,
                Runs = orderedRuns.AsReadOnly(),
                ParsedRecordCount = parsed,
                SkippedRecordCount = skipped,
                IncompleteLastRecord = incompleteLastRecord,
                Warnings = warnings.AsReadOnly()
            };
        }

        public static TactTimeCsvLoadResult LoadRun(
            string filePath,
            string runId,
            CancellationToken ct,
            IProgress<TactTimeCsvReadProgress> progress = null)
        {
            ValidateFilePath(filePath);

            string normalizedRunId = NormalizeRunId(runId);
            var records = new List<TactTimeRecord>();
            var warnings = new List<string>();
            var repeatedStrings = new Dictionary<string, string>(StringComparer.Ordinal);
            int parsed = 0;
            int skipped = 0;
            bool incompleteLastRecord = false;

            using (var stream = OpenReadShared(filePath))
            using (var reader = new CsvRecordReader(stream))
            {
                CsvRecord headerRecord = reader.ReadRecord();
                if (headerRecord == null || !headerRecord.IsComplete)
                    throw new InvalidDataException("택타임 CSV 헤더를 읽을 수 없습니다.");

                Dictionary<string, int> columns = BuildColumnMap(headerRecord.Fields);
                ValidateRequiredColumns(columns);
                AddMissingOptionalColumnWarnings(columns, warnings);

                CsvRecord csvRecord;
                while ((csvRecord = reader.ReadRecord()) != null)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!csvRecord.IsComplete)
                    {
                        incompleteLastRecord = true;
                        AddWarning(warnings, "파일 끝의 미완성 CSV 레코드를 제외했습니다. record=" + csvRecord.RecordNumber);
                        break;
                    }

                    TactTimeRecord record;
                    string error;
                    string recordWarning;
                    if (!TryParseRecord(csvRecord.Fields, columns, out record, out error, out recordWarning))
                    {
                        skipped++;
                        AddWarning(warnings, "CSV 레코드를 제외했습니다. record=" + csvRecord.RecordNumber + ", reason=" + error);
                        ReportProgress(progress, "Run 기록 불러오기", stream, parsed + skipped, false);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(recordWarning))
                        AddWarning(warnings, "CSV 레코드 호환 처리. record=" + csvRecord.RecordNumber + ", reason=" + recordWarning);

                    parsed++;
                    if (string.Equals(NormalizeRunId(record.RunId), normalizedRunId, StringComparison.OrdinalIgnoreCase))
                    {
                        CompactRepeatedStrings(record, repeatedStrings);
                        records.Add(record);
                    }

                    ReportProgress(progress, "Run 기록 불러오기", stream, parsed + skipped, false);
                }

                ReportProgress(progress, "Run 기록 불러오기", stream, parsed + skipped, true);
            }

            records.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
            return new TactTimeCsvLoadResult
            {
                FilePath = filePath,
                RunId = normalizedRunId,
                Records = records.AsReadOnly(),
                ParsedRecordCount = parsed,
                SkippedRecordCount = skipped,
                IncompleteLastRecord = incompleteLastRecord,
                Warnings = warnings.AsReadOnly()
            };
        }

        private static void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("택타임 CSV 경로가 비어 있습니다.", "filePath");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("택타임 CSV 파일을 찾을 수 없습니다.", filePath);
        }

        private static FileStream OpenReadShared(string filePath)
        {
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                64 * 1024,
                FileOptions.SequentialScan);
        }

        private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string> header)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                string name = (header[i] ?? "").Trim().TrimStart('\uFEFF');
                if (!string.IsNullOrWhiteSpace(name) && !result.ContainsKey(name))
                    result.Add(name, i);
            }

            return result;
        }

        private static void ValidateRequiredColumns(Dictionary<string, int> columns)
        {
            string[] required = { "RunId", "StartedAt", "EndedAt", "Category", "Result" };
            for (int i = 0; i < required.Length; i++)
            {
                if (!columns.ContainsKey(required[i]))
                    throw new InvalidDataException("택타임 CSV 필수 열이 없습니다. column=" + required[i]);
            }
        }

        private static void AddMissingOptionalColumnWarnings(
            Dictionary<string, int> columns,
            List<string> warnings)
        {
            string[] optional =
            {
                "When", "ParentId", "CorrelationId", "EquipmentId", "ProjectName", "LotId", "Mode",
                "UnitName", "SequenceName", "ProcessName", "StepName", "ElapsedMs", "AlarmCode", "Detail"
            };

            List<string> missing = optional.Where(x => !columns.ContainsKey(x)).ToList();
            if (missing.Count > 0)
                AddWarning(warnings, "선택 열이 없어 기본값으로 처리합니다. columns=" + string.Join(",", missing));
        }

        private static bool TryParseRecord(
            IReadOnlyList<string> fields,
            Dictionary<string, int> columns,
            out TactTimeRecord record,
            out string error,
            out string warning)
        {
            record = null;
            error = "";
            warning = "";

            DateTime startedAt;
            DateTime endedAt;
            TactTimeCategory category;
            TactTimeResult result;

            string startedText = GetField(fields, columns, "StartedAt");
            if (!TryParseDateTime(startedText, out startedAt))
            {
                error = "StartedAt 변환 실패: " + startedText;
                return false;
            }

            string endedText = GetField(fields, columns, "EndedAt");
            if (!TryParseDateTime(endedText, out endedAt))
            {
                error = "EndedAt 변환 실패: " + endedText;
                return false;
            }

            string categoryText = GetField(fields, columns, "Category");
            if (!Enum.TryParse(categoryText, true, out category) || !Enum.IsDefined(typeof(TactTimeCategory), category))
            {
                error = "Category 변환 실패: " + categoryText;
                return false;
            }

            string resultText = GetField(fields, columns, "Result");
            if (!Enum.TryParse(resultText, true, out result) || !Enum.IsDefined(typeof(TactTimeResult), result))
            {
                error = "Result 변환 실패: " + resultText;
                return false;
            }

            long elapsedMs;
            string elapsedText = GetField(fields, columns, "ElapsedMs");
            if (!long.TryParse(elapsedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out elapsedMs) || elapsedMs < 0)
            {
                elapsedMs = Math.Max(0, (long)(endedAt - startedAt).TotalMilliseconds);
                warning = "ElapsedMs를 StartedAt/EndedAt 차이로 재계산했습니다.";
            }

            record = new TactTimeRecord
            {
                RunId = GetField(fields, columns, "RunId"),
                ParentId = GetField(fields, columns, "ParentId"),
                CorrelationId = GetField(fields, columns, "CorrelationId"),
                EquipmentId = GetField(fields, columns, "EquipmentId"),
                ProjectName = GetField(fields, columns, "ProjectName"),
                LotId = GetField(fields, columns, "LotId"),
                Mode = GetField(fields, columns, "Mode"),
                UnitName = GetField(fields, columns, "UnitName"),
                SequenceName = GetField(fields, columns, "SequenceName"),
                ProcessName = GetField(fields, columns, "ProcessName"),
                StepName = GetField(fields, columns, "StepName"),
                Category = category,
                StartedAt = startedAt,
                EndedAt = endedAt,
                ElapsedMs = elapsedMs,
                Result = result,
                AlarmCode = GetField(fields, columns, "AlarmCode"),
                Detail = GetField(fields, columns, "Detail")
            };
            return true;
        }

        private static bool TryParseDateTime(string value, out DateTime result)
        {
            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out result);
        }

        private static string GetField(
            IReadOnlyList<string> fields,
            Dictionary<string, int> columns,
            string name)
        {
            int index;
            if (!columns.TryGetValue(name, out index) || index < 0 || index >= fields.Count)
                return "";

            return fields[index] ?? "";
        }

        private static string NormalizeRunId(string runId)
        {
            return string.IsNullOrWhiteSpace(runId) ? NoRunId : runId.Trim();
        }

        private static void CompactRepeatedStrings(
            TactTimeRecord record,
            Dictionary<string, string> repeatedStrings)
        {
            if (record == null || repeatedStrings == null)
                return;

            record.RunId = ReuseString(record.RunId, repeatedStrings);
            record.EquipmentId = ReuseString(record.EquipmentId, repeatedStrings);
            record.ProjectName = ReuseString(record.ProjectName, repeatedStrings);
            record.LotId = ReuseString(record.LotId, repeatedStrings);
            record.Mode = ReuseString(record.Mode, repeatedStrings);
            record.UnitName = ReuseString(record.UnitName, repeatedStrings);
            record.SequenceName = ReuseString(record.SequenceName, repeatedStrings);
            record.ProcessName = ReuseString(record.ProcessName, repeatedStrings);
            record.StepName = ReuseString(record.StepName, repeatedStrings);
            record.AlarmCode = ReuseString(record.AlarmCode, repeatedStrings);
        }

        private static string ReuseString(string value, Dictionary<string, string> repeatedStrings)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            string existing;
            if (repeatedStrings.TryGetValue(value, out existing))
                return existing;

            repeatedStrings.Add(value, value);
            return value;
        }

        private static void IncludeRunRecord(TactTimeRunInfo info, TactTimeRecord record)
        {
            if (info == null || record == null)
                return;

            info.RecordCount++;
            if (info.StartedAt == DateTime.MinValue || record.StartedAt < info.StartedAt)
                info.StartedAt = record.StartedAt;
            if (info.EndedAt == DateTime.MinValue || record.EndedAt > info.EndedAt)
                info.EndedAt = record.EndedAt;
            if (string.IsNullOrWhiteSpace(info.EquipmentId))
                info.EquipmentId = record.EquipmentId;
            if (string.IsNullOrWhiteSpace(info.ProjectName))
                info.ProjectName = record.ProjectName;
            if (string.IsNullOrWhiteSpace(info.LotId))
                info.LotId = record.LotId;
            if (string.IsNullOrWhiteSpace(info.Mode))
                info.Mode = record.Mode;

            if (record.Result == TactTimeResult.Failed)
                info.FailedCount++;
            else if (record.Result == TactTimeResult.Stopped || record.Result == TactTimeResult.Canceled)
                info.StoppedCount++;
        }

        private static void AddWarning(List<string> warnings, string message)
        {
            if (warnings != null && warnings.Count < MaxWarnings)
                warnings.Add(message ?? "");
        }

        private static void ReportProgress(
            IProgress<TactTimeCsvReadProgress> progress,
            string phase,
            FileStream stream,
            int recordCount,
            bool force)
        {
            if (progress == null || stream == null)
                return;
            if (!force && recordCount % 1000 != 0)
                return;

            long position = force ? stream.Length : Math.Min(stream.Position, stream.Length);
            progress.Report(new TactTimeCsvReadProgress
            {
                Phase = phase ?? "",
                BytesRead = position,
                TotalBytes = stream.Length,
                RecordCount = recordCount
            });
        }

        private sealed class CsvRecord
        {
            public IReadOnlyList<string> Fields { get; set; }
            public long RecordNumber { get; set; }
            public bool IsComplete { get; set; }
        }

        private sealed class CsvRecordReader : IDisposable
        {
            private readonly StreamReader _reader;
            private long _recordNumber;

            public CsvRecordReader(Stream stream)
            {
                _reader = new StreamReader(
                    stream,
                    new UTF8Encoding(false, true),
                    true,
                    64 * 1024,
                    true);
            }

            public CsvRecord ReadRecord()
            {
                var fields = new List<string>();
                var field = new StringBuilder();
                bool inQuotes = false;
                bool anyCharacter = false;

                while (true)
                {
                    int value = _reader.Read();
                    if (value < 0)
                    {
                        if (!anyCharacter && fields.Count == 0 && field.Length == 0)
                            return null;

                        fields.Add(field.ToString());
                        _recordNumber++;
                        return new CsvRecord
                        {
                            Fields = fields.AsReadOnly(),
                            RecordNumber = _recordNumber,
                            IsComplete = !inQuotes
                        };
                    }

                    anyCharacter = true;
                    char ch = (char)value;
                    if (inQuotes)
                    {
                        if (ch == '"')
                        {
                            if (_reader.Peek() == '"')
                            {
                                _reader.Read();
                                field.Append('"');
                            }
                            else
                            {
                                inQuotes = false;
                            }
                        }
                        else
                        {
                            field.Append(ch);
                        }

                        continue;
                    }

                    if (ch == '"' && field.Length == 0)
                    {
                        inQuotes = true;
                        continue;
                    }

                    if (ch == ',')
                    {
                        fields.Add(field.ToString());
                        field.Clear();
                        continue;
                    }

                    if (ch == '\r' || ch == '\n')
                    {
                        if (ch == '\r' && _reader.Peek() == '\n')
                            _reader.Read();

                        fields.Add(field.ToString());
                        _recordNumber++;
                        return new CsvRecord
                        {
                            Fields = fields.AsReadOnly(),
                            RecordNumber = _recordNumber,
                            IsComplete = true
                        };
                    }

                    field.Append(ch);
                }
            }

            public void Dispose()
            {
                _reader.Dispose();
            }
        }
    }
}
