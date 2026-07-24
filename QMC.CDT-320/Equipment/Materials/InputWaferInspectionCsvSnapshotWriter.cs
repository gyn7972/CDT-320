using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.Materials
{
    internal static class InputWaferInspectionCsvSnapshotWriter
    {
        private const string Header =
            "When,Event,InputWaferId,RecipeName,LotId,DieId,InputSequenceNo,InputMapX,InputMapY,InputBinCode,CurrentResult,CurrentLocation,OutputWaferId,OutputMapX,OutputMapY,OutputBinCode,NgCodes,InspectionType,InspectionResult,InspectionOffsetX,InspectionOffsetY,InspectionOffsetT,InspectionOffsetValid,InspectionNgCodes,InspectionMeasurements,AllInspections";

        private static readonly object QueueSyncRoot = new object();
        private static readonly object FileSyncRoot = new object();
        private static readonly Queue<CsvWriteItem> PendingItems = new Queue<CsvWriteItem>();
        private static bool _writerRunning;

        public static void EnqueueInspection(
            string eventName,
            string recipeName,
            string lotId,
            DieMaterial die,
            DieInspectionRecord updatedRecord)
        {
            try
            {
                if (die == null)
                    return;

                lotId = MaterialStateService.GetProductionLotId();
                VisionInspectionResultFileWriter.EnqueueBottomResult(
                    recipeName,
                    lotId,
                    die,
                    updatedRecord);

                CsvWriteItem item = BuildItem(eventName, recipeName, lotId, die, updatedRecord);
                if (item == null || string.IsNullOrWhiteSpace(item.Path) || string.IsNullOrWhiteSpace(item.Line))
                    return;

                lock (QueueSyncRoot)
                {
                    PendingItems.Enqueue(item);
                    if (_writerRunning)
                        return;

                    _writerRunning = true;
                    Task.Run((Action)WriteQueuedItems);
                }
            }
            catch
            {
            }
        }

        private static CsvWriteItem BuildItem(
            string eventName,
            string recipeName,
            string lotId,
            DieMaterial die,
            DieInspectionRecord updatedRecord)
        {
            string inputWaferId = string.IsNullOrWhiteSpace(die.WaferID_Input)
                ? "UNKNOWN_INPUT_WAFER"
                : die.WaferID_Input.Trim();
            string path = BuildPath(inputWaferId);
            VisionOffset offset = updatedRecord != null ? updatedRecord.Offset : null;

            string line = string.Join(",",
                Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)),
                Csv(eventName),
                Csv(inputWaferId),
                Csv(recipeName),
                Csv(lotId),
                Csv(die.DieId),
                Csv(die.InputSequenceNo),
                Csv(die.Wafer_IndexX),
                Csv(die.Wafer_IndexY),
                Csv(die.Input_BinCode),
                Csv(die.Result),
                Csv(die.CurrentLocation),
                Csv(die.WaferID_Output),
                Csv(die.Bin_IndexX),
                Csv(die.Bin_IndexY),
                Csv(die.Output_BinCode),
                Csv(JoinList(die.NgCodes)),
                Csv(updatedRecord != null ? updatedRecord.InspectionType : ""),
                Csv(updatedRecord != null ? updatedRecord.Result.ToString() : ""),
                Csv(offset != null ? offset.X : double.NaN),
                Csv(offset != null ? offset.Y : double.NaN),
                Csv(offset != null ? offset.R : double.NaN),
                Csv(offset != null && offset.IsValid),
                Csv(updatedRecord != null ? JoinList(updatedRecord.NgCodes) : ""),
                Csv(updatedRecord != null ? BuildMeasurementSummary(updatedRecord.Measurements) : ""),
                Csv(BuildInspectionSummary(die.Inspections)));

            return new CsvWriteItem { Path = path, Line = line };
        }

        private static string BuildPath(string inputWaferId)
        {
            string dir = Path.Combine(EventLogger.LogRoot, "Temp", "InputWaferInspectionCsv", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            string file = SafeFileName(inputWaferId) + "_inspection.csv";
            return Path.Combine(dir, file);
        }

        private static string BuildInspectionSummary(List<DieInspectionRecord> inspections)
        {
            if (inspections == null || inspections.Count == 0)
                return "";

            var parts = new List<string>();
            foreach (DieInspectionRecord record in inspections.Where(r => r != null).OrderBy(r => r.InspectionType))
            {
                string ng = record.NgCodes != null && record.NgCodes.Count > 0
                    ? "ng=[" + JoinList(record.NgCodes) + "]"
                    : "ng=[]";
                parts.Add((record.InspectionType ?? "") + ":" + record.Result + ":" + ng);
            }

            return string.Join(" | ", parts);
        }

        private static string BuildMeasurementSummary(List<InspectionMeasurement> measurements)
        {
            if (measurements == null || measurements.Count == 0)
                return "";

            var parts = new List<string>();
            foreach (InspectionMeasurement measurement in measurements.Where(m => m != null).OrderBy(m => m.Name))
            {
                string value = !string.IsNullOrWhiteSpace(measurement.RawValue)
                    ? measurement.RawValue
                    : Format(measurement.Value);
                string unit = string.IsNullOrWhiteSpace(measurement.Unit) ? "" : measurement.Unit;
                parts.Add((measurement.Name ?? "") + "=" + value + unit + "(" + measurement.Result + ")");
            }

            return string.Join(" | ", parts);
        }

        private static void WriteQueuedItems()
        {
            try
            {
                while (true)
                {
                    List<CsvWriteItem> items = new List<CsvWriteItem>();
                    lock (QueueSyncRoot)
                    {
                        while (PendingItems.Count > 0)
                            items.Add(PendingItems.Dequeue());

                        if (items.Count == 0)
                        {
                            _writerRunning = false;
                            return;
                        }
                    }

                    WriteItems(items);
                }
            }
            catch
            {
                lock (QueueSyncRoot)
                {
                    _writerRunning = false;
                }
            }
        }

        private static void WriteItems(List<CsvWriteItem> items)
        {
            if (items == null || items.Count == 0)
                return;

            try
            {
                lock (FileSyncRoot)
                {
                    foreach (IGrouping<string, CsvWriteItem> group in items.GroupBy(i => i.Path))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(group.Key));
                        if (!File.Exists(group.Key))
                            File.WriteAllText(group.Key, Header + Environment.NewLine, Encoding.UTF8);

                        StringBuilder buffer = new StringBuilder();
                        foreach (CsvWriteItem item in group)
                            buffer.AppendLine(item.Line);

                        File.AppendAllText(group.Key, buffer.ToString(), Encoding.UTF8);
                    }
                }
            }
            catch
            {
            }
        }

        private static string JoinList(IEnumerable<string> values)
        {
            if (values == null)
                return "";

            return string.Join("|", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()));
        }

        private static string SafeFileName(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "UNKNOWN" : value.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                text = text.Replace(c, '_');
            return text;
        }

        private static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "";

            return value.ToString("0.######", CultureInfo.InvariantCulture);
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

        private sealed class CsvWriteItem
        {
            public string Path { get; set; }
            public string Line { get; set; }
        }
    }
}
