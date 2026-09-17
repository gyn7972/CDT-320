// Runs pure map, CSV and standalone drawing code from the isolated Handler build; never starts Form1.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Ui.Controls;

internal static class VerifyMapCoordinates
{
    private static int checks;
    private static string root;
    private static readonly Type Writer = typeof(DieMaterial).Assembly.GetType("QMC.CDT320.Materials.VisionInspectionResultFileWriter", true);
    private static readonly DateTime At = new DateTime(2026, 9, 14, 12, 30, 0);
    private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    private static object Call(string name, params object[] args)
    {
        try { return Writer.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static T Get<T>(object value, string name) { return (T)value.GetType().GetProperty(name).GetValue(value); }
    private static List<string> Fields(string line) { return (List<string>)Call("ParseCsvLine", line); }
    private static string Num(double value) { return value.ToString("0.######", CultureInfo.InvariantCulture); }
    private static void Reject(Action action, string name)
    {
        bool rejected = false;
        try { action(); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, name);
    }
    private static T Copy<T>(T value)
    {
        using (var stream = new MemoryStream())
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            QMC.Common.Data.Store.JsonPrettySerializer.WriteObject(stream, typeof(T), value); stream.Position = 0;
            return (T)serializer.ReadObject(stream);
        }
    }
    private static WaferMapProcessSettings Settings(int angle, WaferMapGridOrigin origin)
    { return new WaferMapProcessSettings { RotationDegrees = angle, GridOrigin = origin }; }
    // Independent expected grid calculation, including the even-grid center between dies.
    private static double[] Expected(DieMap raw, DieMapEntry source, int angle, WaferMapGridOrigin origin)
    {
        int x = angle == 0 ? source.DieMapX : raw.DieMapX - 1 - source.DieMapX;
        int y = angle == 0 ? source.DieMapY : raw.DieMapY - 1 - source.DieMapY;
        return new[] {
            origin == WaferMapGridOrigin.Center ? x - (raw.DieMapX - 1) / 2.0 :
                origin == WaferMapGridOrigin.TopRight || origin == WaferMapGridOrigin.BottomRight ? raw.DieMapX - 1 - x : x,
            origin == WaferMapGridOrigin.Center ? (raw.DieMapY - 1) / 2.0 - y :
                origin == WaferMapGridOrigin.BottomLeft || origin == WaferMapGridOrigin.BottomRight ? raw.DieMapY - 1 - y : y };
    }
    private static DieMap LoadRemote()
    {
        string path = Path.Combine(root, "1234.02");
        string text = "[TEST/01000/02000/%6/&6/]\r\n";
        for (int y = 0; y < 2; y++) for (int x = 0; x < 3; x++)
            text += "X=" + (10 + x) + " Y=" + (20 + y) + " B=" + (x + 3 * y) + "\r\n";
        File.WriteAllText(path, text);
        return WaferMapParserRegistry.Load(path, WaferMapSourceFormat.Samsung);
    }
    private static DieInspectionRecord Bottom(int marker)
    {
        return new DieInspectionRecord { InspectionType = "Bottom", Result = MaterialInspectionResult.Ok,
            Measurements = new List<InspectionMeasurement> {
                new InspectionMeasurement { Name = "BottomVision_bottom_width_mm", Value = marker / 1000.0 },
                new InspectionMeasurement { Name = "BottomVision_bottom_height_mm", Value = (marker + 100) / 1000.0 } } };
    }
    private static DieMaterial Die(DieMapEntry input, DieMapEntry output, WaferMapGridOrigin origin, int marker)
    {
        return new DieMaterial { DieId = input.DieUid, WaferID_Input = "1234.02", WaferID_Output = "OUT",
            OutputWaferInstanceId = "PHYSICAL-OUT", Wafer_IndexX = input.DieMapX, Wafer_IndexY = input.DieMapY,
            Wafer_OriginalIndexX = input.OriginalMapX, Wafer_OriginalIndexY = input.OriginalMapY,
            InputLogicalGridX = input.LogicalGridX, InputLogicalGridY = input.LogicalGridY, InputMapGridOrigin = origin,
            InputSourceBinCode = input.SourceBinCode, InputSourceToken = input.SourceToken,
            Bin_IndexX = output.DieMapX, Bin_IndexY = output.DieMapY, InputSequenceNo = marker, PickedPickerNo = 1,
            PickedPickerLocation = MaterialLocationKind.PickerFront,
            Inspections = new List<DieInspectionRecord> { Bottom(marker) } };
    }
    private static OutputReceiveSlotMaterial Slot(DieMapEntry output, DieMaterial die, int bin)
    {
        return new OutputReceiveSlotMaterial { DieMapX = output.DieMapX, DieMapY = output.DieMapY,
            LogicalGridX = output.LogicalGridX, LogicalGridY = output.LogicalGridY,
            DieUid = die.DieId, SourceDieUid = die.DieId, BinCode = bin, IsTarget = true };
    }
    private static WaferMaterial Wafer(DieMap map)
    { return new WaferMaterial { WaferId = "OUT", WaferInstanceId = "PHYSICAL-OUT", OutputReceivePreparedMapInstanceId = "PHYSICAL-OUT", OutputReceivePreparedMap = map }; }
    private static object Payload(WaferMaterial wafer, DieMaterial die, OutputReceiveSlotMaterial slot, BinSide side, bool flying = false)
    {
        return Call("BuildPlacePayload", "TEST", "LOT", 0, side, wafer, die, slot,
            die.Inspections.First(), null, At, At, flying);
    }
    private static object Metadata()
    { return Activator.CreateInstance(Writer.GetNestedType("RecipeMetadata", BindingFlags.NonPublic), true); }
    private static void VerifyPairs(DieMap raw)
    {
        var origins = (WaferMapGridOrigin[])Enum.GetValues(typeof(WaferMapGridOrigin));
        string sourceHash = WaferMapProcessService.ComputeMapHash(raw);
        var outputRaw = DieMapGenerator.GenerateRect(4, 3, 1.2, 2.3, .2, .4, "OUTPUT");
        foreach (var entry in outputRaw.Entries) entry.DieUid = "OUTPUT-CELL-" + entry.DieMapX + "-" + entry.DieMapY;
        foreach (int inputAngle in new[] { 0, 180 }) foreach (var inputOrigin in origins)
        foreach (int outputAngle in new[] { 0, 180 }) foreach (var outputOrigin in origins)
        foreach (BinSide side in Enum.GetValues(typeof(BinSide)))
        {
            var input = WaferMapProcessService.Prepare(raw, Settings(inputAngle, inputOrigin), "Input");
            var output = WaferMapProcessService.Prepare(outputRaw, Settings(outputAngle, outputOrigin), side.ToString());
            var wafer = Wafer(output);
            for (int i = 0; i < raw.Entries.Count; i++)
            {
                var source = raw.Entries[i];
                var sourceOut = outputRaw.Entries[(i * 5) % outputRaw.Entries.Count];
                var from = input.Entries.Single(e => e.DieUid == source.DieUid);
                var to = output.Entries.Single(e => e.DieUid == sourceOut.DieUid);
                var inXY = Expected(raw, source, inputAngle, inputOrigin);
                var outXY = Expected(outputRaw, sourceOut, outputAngle, outputOrigin);
                int marker = 101 + i;
                var die = Copy(Die(from, to, inputOrigin, marker));
                var slot = Copy(Slot(to, die, 70 + i));
                Check(die.DieId == source.DieUid && die.Wafer_OriginalIndexX == source.OriginalMapX &&
                    die.Wafer_OriginalIndexY == source.OriginalMapY && die.InputSourceBinCode == source.SourceBinCode &&
                    die.InputSourceToken == source.SourceToken, "same source die coordinates/BIN/token after transform and snapshot");
                var payload = Payload(wafer, die, slot, side);
                var row = Fields(Get<string>(payload, "DetailLine"));
                Check(row.Count == 27 && row[3] == Num(inXY[0] + 1) && row[4] == Num(inXY[1] + 1) &&
                    row[6] == Num(outXY[0] + 1) && row[7] == Num(outXY[1] + 1), "OUTPUT uses applied input/output X/Y plus one exactly once");
                Check(row[0] == marker.ToString() && row[8] == marker.ToString() && row[9] == (marker + 100).ToString() &&
                    row[15] == (70 + i).ToString(), "inspection measurements and result BIN remain on the same die/slot");
                var inputRow = Fields((string)Call("BuildInputResultLine", "TEST", "LOT", die, slot, die.Inspections[0], null));
                Check(inputRow.Count == 55 && inputRow[1] == die.DieId && inputRow[8] == row[3] && inputRow[9] == row[4] &&
                    inputRow[18] == row[6] && inputRow[19] == row[7] && inputRow[27] == row[8], "INPUT and OUTPUT agree on die, applied coordinates and measurements");
                Check(WaferMapProcessService.FormatMapPosition(from) == "X=" + row[3] + "  Y=" + row[4] &&
                    WaferMapProcessService.FormatMapPosition(to) == "X=" + row[6] + "  Y=" + row[7], "display coordinates equal exported coordinates");
                Check(die.InputLogicalGridX == inXY[0] && die.InputLogicalGridY == inXY[1] &&
                    slot.LogicalGridX == outXY[0] && slot.LogicalGridY == outXY[1] &&
                    from.LogicalGridX == inXY[0] && to.LogicalGridY == outXY[1], "formatting leaves internal zero-based coordinates unchanged");
                var summary = Fields((string)Call("BuildOutputSummaryLine", payload, Metadata(), At, 1));
                Check(summary[5] == inputOrigin.ToString() && summary[6] == outputOrigin.ToString(), "header origin uses actual applied maps");
                // Queued rows must not follow a later recipe/map edit.
                var previousOrigin = output.ProcessTransform.Settings.GridOrigin;
                output.ProcessTransform.Settings.GridOrigin = WaferMapGridOrigin.Center;
                Check(Fields((string)Call("BuildOutputSummaryLine", payload, Metadata(), At, 1))[6] == outputOrigin.ToString(), "queued header origin is immutable");
                output.ProcessTransform.Settings.GridOrigin = previousOrigin;
            }
        }
        Check(sourceHash == WaferMapProcessService.ComputeMapHash(raw), "all export combinations preserve downloaded map");
    }
    private static void VerifyMismatchAndUpsert(DieMap raw)
    {
        var input = WaferMapProcessService.Prepare(raw, Settings(180, WaferMapGridOrigin.Center), "Input");
        var output = WaferMapProcessService.Prepare(DieMapGenerator.GenerateRect(3, 3, 1, 2, 0, 0, "OUT"), Settings(0, WaferMapGridOrigin.Center), "Good");
        var to = output.GetCell(0, 2); // Valid center (-1,-1), formerly confused with Flying Die sentinel.
        var die = Die(input.Entries[0], to, WaferMapGridOrigin.Center, 101);
        var slot = Slot(to, die, 80); var wafer = Wafer(output);
        var goodSide = (BinSide)0;
        Action validate = () => Payload(wafer, die, slot, goodSide);
        slot.DieUid = "OTHER-DIE"; Reject(validate, "wrong die rejected"); slot.DieUid = die.DieId;
        slot.SourceDieUid = "OTHER-SOURCE"; Reject(validate, "wrong source identity rejected"); slot.SourceDieUid = die.DieId;
        die.OutputWaferInstanceId = "OTHER-WAFER"; Reject(validate, "wrong physical wafer rejected"); die.OutputWaferInstanceId = wafer.WaferInstanceId;
        wafer.OutputReceivePreparedMapInstanceId = "PREVIOUS-WAFER"; Reject(validate, "previous wafer prepared map rejected"); wafer.OutputReceivePreparedMapInstanceId = wafer.WaferInstanceId;
        die.Bin_IndexX++; Reject(validate, "wrong die destination rejected"); die.Bin_IndexX--;
        slot.LogicalGridX++; Reject(validate, "slot coordinate differs from pinned map rejected"); slot.LogicalGridX--;
        double? inputX = die.InputLogicalGridX;
        die.InputLogicalGridX = null; Reject(validate, "missing logical input cannot fall back to raw coordinates");
        die.InputLogicalGridX = double.NaN; Reject(validate, "nonfinite input coordinate rejected"); die.InputLogicalGridX = inputX;
        wafer.OutputReceivePreparedMap = null; Reject(validate, "missing applied output map rejected"); wafer.OutputReceivePreparedMap = output;
        slot.LogicalGridX = double.PositiveInfinity; Reject(validate, "nonfinite output rejected"); slot.LogicalGridX = to.LogicalGridX;
        var normal = Payload(wafer, die, slot, goodSide);
        var flyingA = Payload(wafer, die, null, goodSide, true);
        die.WaferID_Input = "OTHER-INPUT";
        var flyingB = Payload(wafer, die, null, goodSide, true);
        var flyingFields = Fields(Get<string>(flyingA, "DetailLine"));
        Check(flyingFields[6] == "" && flyingFields[7] == "", "unplaced die uses empty destination instead of valid negative coordinates");
        string file = Path.Combine(root, "OUTPUT-center-upsert_MapXY_V2.csv"); var metadata = Metadata();
        Call("EnsureOutputCsvPreamble", file, normal, metadata);
        foreach (object p in new[] { normal, flyingA, flyingB, normal, flyingA, flyingB }) Call("UpsertOutputCsvLine", file, p, metadata);
        var lines = File.ReadAllLines(file);
        Check(lines.Length == 6, "normal negative coordinate and two unplaced input wafers remain distinct after upserts");
        var details = lines.Skip(3).Select(Fields).ToList();
        Check(details.Count(r => r[6] == "0" && r[7] == "0") == 1 && details.Count(r => r[6] == "" && r[7] == "") == 2, "center (-1,-1) exports as (0,0), distinct from an unplaced die");
        var bottomOnly = Fields((string)Call("BuildInputResultLine", "TEST", "LOT", die, null, die.Inspections[0], null));
        Check(bottomOnly[18] == "" && bottomOnly[19] == "" && bottomOnly[8] == Num(die.InputLogicalGridX.Value + 1), "bottom-only record has applied input and no invented output");
        Check((string)Writer.GetField("MapCoordinateSuffix", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue() == "_MapXY_V2", "one-based coordinates and global heads cannot mix with legacy CSV files");
        var reference = WaferMapProcessService.Prepare(DieMapGenerator.GenerateRect(34, 45, 1, 1, 0, 0, "JMB-GEOMETRY"), Settings(0, WaferMapGridOrigin.BottomLeft), "Good");
        Check(WaferMapProcessService.FormatMapPosition(reference.GetCell(33, 19)) == "X=34  Y=26", "34x45 observed output geometry resolves array (33,19) to displayed (34,26)");
    }
    private static void VerifyHeadsAndSchema(DieMap raw)
    {
        var input = WaferMapProcessService.Prepare(raw, Settings(0, WaferMapGridOrigin.TopLeft), "Input");
        var output = WaferMapProcessService.Prepare(DieMapGenerator.GenerateRect(3, 3, 1, 1, 0, 0, "OUT"), Settings(0, WaferMapGridOrigin.BottomLeft), "Good");
        var to = output.GetCell(0, 2);
        var die = Die(input.GetCell(0, 0), to, WaferMapGridOrigin.TopLeft, 17);
        var slot = Slot(to, die, 1); var wafer = Wafer(output); var metadata = Metadata();
        var side = (BinSide)0;
        // The persisted pick context survives a move to an output wafer; CurrentLocation cannot identify HEAD.
        die.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.OutputStageGood };
        foreach (var location in new[] { MaterialLocationKind.PickerFront, MaterialLocationKind.PickerRear })
        foreach (int picker in new[] { 4, 3, 2, 1 })
        {
            die.PickedPickerLocation = location; die.PickedPickerNo = picker;
            string expected = (location == MaterialLocationKind.PickerFront ? picker : 9 - picker).ToString(CultureInfo.InvariantCulture);
            var payload = Payload(wafer, die, slot, side);
            var row = Fields(Get<string>(payload, "DetailLine"));
            var inputRow = Fields((string)Call("BuildInputResultLine", "TEST", "LOT", die, slot, die.Inspections[0], null));
            Check(row[1] == expected && inputRow[4] == expected, "INPUT/OUTPUT distinguish the same front/rear physical head");
            Check(row[0] == "17" && row[3] == "1" && row[4] == "1" && row[6] == "1" && row[7] == "1", "sequence retained; both map origins exported as 1,1");
            die.PickedPickerLocation = MaterialLocationKind.Unknown; die.PickedPickerNo = -1;
            Check(Fields(Get<string>(payload, "DetailLine"))[1] == expected, "queued HEAD is immutable");
        }
        die.PickedPickerNo = 1;
        Reject(() => Payload(wafer, die, slot, side), "unknown head side must not masquerade as front");
        Reject(() => Call("BuildInputResultLine", "TEST", "LOT", die, slot, die.Inspections[0], null), "INPUT also rejects unknown head side");
        die.PickedPickerLocation = MaterialLocationKind.PickerFront;
        foreach (int invalidNo in new[] { -1, 0, 5, 8 })
        {
            die.PickedPickerNo = invalidNo;
            Reject(() => Payload(wafer, die, slot, side), "only local P1-P4 may supply a global HEAD");
        }
        die.PickedPickerNo = 4;
        var normal = Payload(wafer, die, slot, side);
        string outputFile = Path.Combine(root, "OUTPUT-origin_MapXY_V2.csv");
        Call("EnsureOutputCsvPreamble", outputFile, normal, metadata);
        Call("UpsertOutputCsvLine", outputFile, normal, metadata);
        string outputText = File.ReadAllText(outputFile);
        var header = Fields(File.ReadAllLines(outputFile)[2]);
        Check(header[3] == "PICK_WAFER_X" && header[4] == "PICK_WAFER_Y" && header[6] == "PLACE_WAFER_X" && header[7] == "PLACE_WAFER_Y", "OUTPUT header matches X/Y field order");
        Call("EnsureOutputCsvPreamble", outputFile, normal, metadata);
        Check(File.ReadAllText(outputFile) == outputText, "current schema preamble is idempotent");
        string legacyText = outputText.Replace("PICK_WAFER_X,PICK_WAFER_Y", "PICK_WAFER_ROW,PICK_WAFER_COL")
            .Replace("PLACE_WAFER_X,PLACE_WAFER_Y", "PLACE_WAFER_ROW,PLACE_WAFER_COL");
        string legacyFile = Path.Combine(root, "legacy-output.csv"); File.WriteAllText(legacyFile, legacyText);
        Reject(() => Call("EnsureOutputCsvPreamble", legacyFile, normal, metadata), "legacy header cannot be relabeled without converting coordinates and head provenance");
        Check(File.ReadAllText(legacyFile) == legacyText, "mismatched output file preserved");
        string inputHeader = (string)Writer.GetField("InputHeader", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
        var inputColumns = Fields(inputHeader);
        Check(inputColumns[8] == "Loading_Substrate_X" && inputColumns[9] == "Loading_Substrate_Y" && inputColumns[18] == "Unloading_Substrate_X" && inputColumns[19] == "Unloading_Substrate_Y", "INPUT header matches X/Y field order");
        string inputFile = Path.Combine(root, "INPUT-origin_MapXY_V2.csv");
        Call("EnsureInputCsvPreamble", inputFile, inputHeader);
        File.AppendAllText(inputFile, (string)Call("BuildInputResultLine", "TEST", "LOT", die, slot, die.Inspections[0], null) + Environment.NewLine);
        string inputText = File.ReadAllText(inputFile);
        Call("EnsureInputCsvPreamble", inputFile, inputHeader);
        Check(File.ReadAllText(inputFile) == inputText, "INPUT preamble does not double-offset existing coordinates");
        string oldInput = inputText.Replace("Loading_Substrate_X,Loading_Substrate_Y", "Loading_Substrate_Y,Loading_Substrate_X");
        string oldInputFile = Path.Combine(root, "legacy-input.csv"); File.WriteAllText(oldInputFile, oldInput);
        Reject(() => Call("EnsureInputCsvPreamble", oldInputFile, inputHeader), "INPUT legacy header cannot be relabeled");
        Check(File.ReadAllText(oldInputFile) == oldInput, "mismatched input file preserved");
    }

    private static void VerifyDrawing(DieMap raw)
    {
        Application.EnableVisualStyles();
        var map = WaferMapProcessService.Prepare(raw, Settings(180, WaferMapGridOrigin.Center), "Input");
        using (var view = new DieMapView { Size = new Size(960, 620), Map = map, Caption = "Input / 180 / Center", ShowEquipmentAxes = true })
        {
            view.CreateControl();
            typeof(DieMapView).GetField("_hover", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, map.Entries[0]);
            using (var bitmap = new Bitmap(view.Width, view.Height))
            { view.DrawToBitmap(bitmap, new Rectangle(Point.Empty, view.Size)); bitmap.Save(Path.Combine(root, "applied-map.png"), ImageFormat.Png); }
            Check(view.Map.Entries.Count == raw.Entries.Count, "standalone applied-map control renders without equipment initialization");
        }
        Check(WaferMapProcessService.FormatMapCoordinate(-.5) == "0.5" && WaferMapProcessService.FormatMapCoordinate(-1) == "0" &&
            WaferMapProcessService.FormatMapCoordinate(-2) == "-1" && WaferMapProcessService.FormatMapCoordinate(0) == "1", "display applies +1 to corner and center coordinates without clamping or rounding");
        Check(WaferMapProcessService.FormatMapCoordinate(null) == "-" && WaferMapProcessService.FormatMapCoordinate(double.NaN) == "-", "unknown display coordinate never masquerades as an applied address");
    }
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            root = Path.GetFullPath(args[0]); Directory.CreateDirectory(root);
            var raw = LoadRemote(); VerifyPairs(raw); VerifyMismatchAndUpsert(raw); VerifyHeadsAndSchema(raw); VerifyDrawing(raw);
            Console.WriteLine("PASS: " + checks + " map/export/display assertions against actual isolated Handler build; no equipment runtime executed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
