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
    internal static class OutputWaferCsvSnapshotWriter
    {
        private const string Header =
            "When,Event,OutputSide,OutputWaferId,SourceWaferId,RecipeName,LotId,DieId,InputSequenceNo,InputWaferId,InputMapX,InputMapY,InputBinCode,OutputMapX,OutputMapY,OutputOrder,OutputBinCode,Result,Location,PickerLocation,PickedPickerNo,PickedAt,TargetX,TargetY,BinOffsetX,BinOffsetY,BinOffsetT,BinOffsetValid,NgCodes,Inspections,Measurements";

        private static readonly object QueueSyncRoot = new object();
        private static readonly object FileSyncRoot = new object();
        private static readonly Queue<CsvWriteItem> PendingItems = new Queue<CsvWriteItem>();
        private static bool _writerRunning;

        public static void EnqueuePlacedDie(
            string eventName,
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

                lotId = MaterialStateService.GetProductionLotId();
                if (string.Equals(eventName, "OutputStageDieInspection", StringComparison.OrdinalIgnoreCase))
                {
                    VisionInspectionResultFileWriter.EnqueuePlaceResult(
                        recipeName,
                        lotId,
                        outputSide,
                        outputWafer,
                        die,
                        receiveTarget);
                }

                CsvWriteItem item = BuildItem(eventName, recipeName, lotId, outputSide, outputWafer, die, receiveTarget);
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
            QMC.CDT320.BinSide outputSide,
            WaferMaterial outputWafer,
            DieMaterial die,
            OutputStageReceiveTarget receiveTarget)
        {
            string outputWaferId = outputWafer != null ? outputWafer.WaferId : "";
            if (string.IsNullOrWhiteSpace(outputWaferId))
                outputWaferId = "UNKNOWN_OUTPUT_WAFER";

            OutputReceiveSlotMaterial slot = ResolveSlot(outputWafer, die, receiveTarget);
            string path = BuildPath(outputWaferId, outputSide);
            string line = string.Join(",",
                Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)),
                Csv(eventName),
                Csv(outputSide),
                Csv(outputWaferId),
                Csv(outputWafer != null ? outputWafer.OutputReceiveSourceWaferId : ""),
                Csv(recipeName),
                Csv(lotId),
                Csv(die.DieId),
                Csv(die.InputSequenceNo),
                Csv(die.WaferID_Input),
                Csv(die.Wafer_IndexX),
                Csv(die.Wafer_IndexY),
                Csv(die.Input_BinCode),
                Csv(slot != null ? slot.DieMapX : die.Bin_IndexX),
                Csv(slot != null ? slot.DieMapY : die.Bin_IndexY),
                Csv(slot != null ? slot.OrderIndex : (receiveTarget != null ? receiveTarget.OrderIndex : -1)),
                Csv(slot != null ? slot.BinCode : die.Output_BinCode),
                Csv(die.Result),
                Csv(die.CurrentLocation),
                Csv(die.PickedPickerLocation),
                Csv(die.PickedPickerNo),
                Csv(die.PickedAt == DateTime.MinValue ? "" : die.PickedAt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)),
                Csv(receiveTarget != null ? receiveTarget.TargetX : (slot != null ? slot.PosX : double.NaN)),
                Csv(receiveTarget != null ? receiveTarget.TargetY : (slot != null ? slot.PosY : double.NaN)),
                Csv(die.BinOffset != null ? die.BinOffset.X : double.NaN),
                Csv(die.BinOffset != null ? die.BinOffset.Y : double.NaN),
                Csv(die.BinOffset != null ? die.BinOffset.R : double.NaN),
                Csv(die.BinOffset != null && die.BinOffset.IsValid),
                Csv(JoinList(die.NgCodes)),
                Csv(BuildInspectionSummary(die.Inspections)),
                Csv(BuildMeasurementSummary(die.Inspections)));

            return new CsvWriteItem { Path = path, Line = line };
        }

        private static OutputReceiveSlotMaterial ResolveSlot(WaferMaterial outputWafer, DieMaterial die, OutputStageReceiveTarget receiveTarget)
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

            if (die != null && !string.IsNullOrWhiteSpace(die.DieId))
            {
                OutputReceiveSlotMaterial byDie = outputWafer.OutputReceiveSlots.FirstOrDefault(s =>
                    s != null && string.Equals(s.DieUid, die.DieId, StringComparison.OrdinalIgnoreCase));
                if (byDie != null)
                    return byDie;
            }

            return null;
        }

        private static string BuildPath(string outputWaferId, QMC.CDT320.BinSide outputSide)
        {
            string dir = Path.Combine(EventLogger.LogRoot, "OutputWaferCsv", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            string file = SafeFileName(outputWaferId) + "_" + outputSide + ".csv";
            return Path.Combine(dir, file);
        }

        private static string BuildInspectionSummary(List<DieInspectionRecord> inspections)
        {
            if (inspections == null || inspections.Count == 0)
                return "";

            var parts = new List<string>();
            foreach (DieInspectionRecord record in inspections.Where(r => r != null).OrderBy(r => r.InspectionType))
            {
                string offset = record.Offset != null && record.Offset.IsValid
                    ? "offset=(" + Format(record.Offset.X) + "/" + Format(record.Offset.Y) + "/" + Format(record.Offset.R) + ")"
                    : "offset=-";
                string ng = record.NgCodes != null && record.NgCodes.Count > 0
                    ? "ng=[" + JoinList(record.NgCodes) + "]"
                    : "ng=[]";
                parts.Add((record.InspectionType ?? "") + ":" + record.Result + ":" + offset + ":" + ng);
            }

            return string.Join(" | ", parts);
        }

        private static string BuildMeasurementSummary(List<DieInspectionRecord> inspections)
        {
            if (inspections == null || inspections.Count == 0)
                return "";

            var parts = new List<string>();
            foreach (DieInspectionRecord record in inspections.Where(r => r != null).OrderBy(r => r.InspectionType))
            {
                if (record.Measurements == null)
                    continue;

                foreach (InspectionMeasurement measurement in record.Measurements.Where(m => m != null).OrderBy(m => m.Name))
                {
                    string value = !string.IsNullOrWhiteSpace(measurement.RawValue)
                        ? measurement.RawValue
                        : Format(measurement.Value);
                    string unit = string.IsNullOrWhiteSpace(measurement.Unit) ? "" : measurement.Unit;
                    parts.Add((record.InspectionType ?? "") + "." + (measurement.Name ?? "") + "=" + value + unit + "(" + measurement.Result + ")");
                }
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
