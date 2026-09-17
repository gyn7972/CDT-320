using System;
using System.IO;
using System.Collections.Generic;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

internal static class VerifyGeneratedFrameProjection
{
    private static int _assertions;
    private static int Main()
    {
        try
        {
            string root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            Assert(Path.GetFileName(root).StartsWith("verify-generated-frame-projection-"), "Isolated executable directory");
            foreach (int angle in new[] { 0, 90, 180, 270 })
                VerifyAngle(root, angle);
            VerifyLegacyAndInvalid(root);
            VerifyLegacyAliasSync(root);
            VerifySavedBoundarySpecWrite(root);
            VerifyJmbSavedBoundarySpecWrite(root);
            VerifySelectedPhaseSpec(root);
            Console.WriteLine("PASS: " + _assertions + " assertions. Supported 0/180 specs, blocked 90/270 drafts and actual extracted MaterialSpecs adapter verified without runtime equipment.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL after " + _assertions + ": " + ex);
            return 1;
        }
    }

    private static void VerifyAngle(string root, int angle)
    {
        var generated = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(
            new WaferMapGenerationSettings(287.4m, 10.370m, 7.913m, .1m, .3m)), angle);
        DieMap map = GeneratedWaferMapCodec.ToDieMap(generated, "INPUT");
        RecipeProject project = CreateProject(map);
        string inputPath = Path.Combine(root, "input-" + angle + ".json");
        DieMapGenerator.SaveJson(map, inputPath);
        project.InputBaseWaferMapFileName = inputPath;
        project.InputDieMapFileName = inputPath;
        string before = Snapshot(project.InputFrame);
        if (angle == 90 || angle == 270)
        {
            Assert(GeneratedWaferMapCodec.Restore(DieMapGenerator.LoadJson(inputPath).Generation).RotationDegrees == angle,
                "Quarter-turn draft remains persistable and reopenable");
            MaterialSpecs.Reset();
            Rejected(() => GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, ""), "Quarter-turn input draft is not an equipment specification");
            Rejected(() => MaterialStateService.VerifyEnsureFrame(project, project.InputFrame, "INPUT", ""), "Quarter-turn input blocked before MaterialSpecs write");
            Assert(MaterialSpecs.Writes == 0 && MaterialStateService.DieSpecWrites == 0, "Unsupported input writes no physical spec");
            project.OutputFrame = FrameFor(map);
            project.OutputBaseWaferMapFileName = Path.Combine(root, "output-draft-" + angle + ".json");
            project.GoodBinDieMapFileName = project.OutputBaseWaferMapFileName;
            DieMapGenerator.SaveJson(map, project.OutputBaseWaferMapFileName);
            Rejected(() => MaterialStateService.VerifyEnsureFrame(project, project.OutputFrame, "OUTPUT", project.GoodBinDieMapFileName), "Quarter-turn output also blocked before MaterialSpecs write");
            Assert(MaterialSpecs.Writes == 0 && MaterialStateService.DieSpecWrites == 0, "Unsupported output writes no physical spec");
            Assert(Snapshot(project.InputFrame) == before && project.Die.WidthMm == 10.370 && project.Die.HeightMm == 7.913, "Blocked draft leaves original frame and shared die unchanged");
            Assert(project.InputMapApprovalHash == "" && project.GoodBinMapApprovalHash == "", "Draft remains pending without equipment approval");
            return;
        }
        TapeFrameSubset physical = GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, "");
        Assert(Same(physical.DieSizeX + physical.PitchX, map.PitchX) && Same(physical.DieSizeY + physical.PitchY, map.PitchY), "Physical center step matches rotated map " + angle);
        Assert(Same(physical.DieSizeX, map.DieSizeX) && Same(physical.DieSizeY, map.DieSizeY), "Rectangular die follows physical map " + angle);
        Assert(physical.DieMapX == map.DieMapX && physical.DieMapY == map.DieMapY && physical.OuterDiameterMm == map.OuterDiameterMm, "Saved final bounds and diameter preserved");
        Assert(Snapshot(project.InputFrame) == before && project.Die.WidthMm == 10.370 && project.Die.HeightMm == 7.913, "Projection leaves source frame and shared die unchanged");
        Assert(project.InputMapApprovalHash == "" && project.GoodBinMapApprovalHash == "", "PENDING does not require or acquire approval");
        Assert(ReferenceEquals(physical, project.InputFrame), "Supported map retains existing physical frame contract");
        Assert(Snapshot(GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, "")) == Snapshot(physical), "Repeated projection from recipe is stable");
        MaterialSpecs.Reset();
        MaterialStateService.VerifyEnsureFrame(project, project.InputFrame, "INPUT", "");
        Assert(MaterialSpecs.Writes == 1 && Same(MaterialSpecs.LastFrame.DieSizeX + MaterialSpecs.LastFrame.PitchX, map.PitchX) &&
            Same(MaterialSpecs.LastFrame.DieSizeY + MaterialSpecs.LastFrame.PitchY, map.PitchY), "Actual extracted MaterialSpecs adapter receives physical step");
        Assert(Snapshot(project.InputFrame) == before, "Actual adapter does not overwrite original frame");

        // Output reference and explicit NG file both resolve Output Base even when the supplied frame is a clone.
        var outputGenerated = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(
            new WaferMapGenerationSettings(220m, 10.370m, 7.913m, .5m, .7m)), angle);
        DieMap outputMap = GeneratedWaferMapCodec.ToDieMap(outputGenerated, "OUTPUT");
        project.OutputFrame = FrameFor(outputMap);
        string outputPath = Path.Combine(root, "output-" + angle + ".json");
        DieMapGenerator.SaveJson(outputMap, outputPath);
        project.OutputBaseWaferMapFileName = outputPath;
        project.GoodBinDieMapFileName = outputPath;
        project.NgBinDieMapFileName = outputPath;
        TapeFrameSubset output = GeneratedWaferFrameProjection.Resolve(project, project.OutputFrame, "");
        Assert(Same(output.DieSizeX + output.PitchX, outputMap.PitchX) && output.OuterDiameterMm == 220, "Output frame resolves separate base");
        TapeFrameSubset copiedOutput = RecipeProjectConsistencyService.CloneFrame(project.OutputFrame);
        output = GeneratedWaferFrameProjection.Resolve(project, copiedOutput, project.NgBinDieMapFileName);
        Assert(Same(output.DieSizeY + output.PitchY, outputMap.PitchY), "Explicit output file chooses output role for cloned frame");
        Assert(Snapshot(project.InputFrame) == before, "Output projection leaves input unchanged");

        // Legacy Frame reference can resolve Input Base, and missing JSON can load its CSV sibling.
        project.Frame = RecipeProjectConsistencyService.CloneFrame(project.InputFrame);
        Assert(Same(GeneratedWaferFrameProjection.Resolve(project, project.Frame, "").DieSizeX, map.DieSizeX), "Legacy frame reference resolves generated input base");
        string csv = Path.Combine(root, "csv-only-" + angle + ".csv");
        DieMapGenerator.SaveCsv(map, csv);
        project.InputBaseWaferMapFileName = Path.ChangeExtension(csv, ".json");
        Assert(Same(GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, "").DieSizeY, map.DieSizeY), "Missing base JSON uses CSV fallback");
    }

    private static void VerifyLegacyAndInvalid(string root)
    {
        var source = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(new WaferMapGenerationSettings(200m, 10.370m, 7.913m, .1m, .3m)), 90);
        DieMap map = GeneratedWaferMapCodec.ToDieMap(source, "CHECK");
        RecipeProject project = CreateProject(map);
        project.InputDieMapFileName = Path.Combine(root, "source-only.json");
        map.Generation = null;
        DieMapGenerator.SaveJson(map, project.InputDieMapFileName);
        Rejected(() => GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, ""), "Known source-only map cannot silently use original pitch");
        MaterialSpecs.Reset();
        Rejected(() => MaterialStateService.VerifyEnsureFrame(project, project.InputFrame, "INPUT", ""), "Invalid map blocks actual MaterialSpecs adapter before write");
        Assert(MaterialSpecs.Writes == 0 && MaterialStateService.DieSpecWrites == 0, "No spec writes occur before generated validation");

        map = GeneratedWaferMapCodec.ToDieMap(source, "CHECK");
        project.InputBaseWaferMapFileName = Path.Combine(root, "valid-before-frame-mismatch.json");
        DieMapGenerator.SaveJson(map, project.InputBaseWaferMapFileName);
        project.InputFrame.PitchX += .001;
        Rejected(() => GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, ""), "Stale frame gap rejected");
        project.InputFrame.PitchX -= .001;
        map.Entries[0].PosX += .0005;
        project.InputBaseWaferMapFileName = Path.Combine(root, "bad-coordinate.json");
        DieMapGenerator.SaveJson(map, project.InputBaseWaferMapFileName);
        Rejected(() => GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, ""), "Corrupted generated coordinate rejected");

        map.Generation = null;
        map.SourceFormat = "PLACE GRID TXT";
        project.InputBaseWaferMapFileName = Path.Combine(root, "legacy.json");
        DieMapGenerator.SaveJson(map, project.InputBaseWaferMapFileName);
        Assert(ReferenceEquals(GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, ""), project.InputFrame), "Legacy rotation token alone never swaps physical specification");
        project.InputBaseWaferMapFileName = "";
        project.InputDieMapFileName = "";
        Assert(ReferenceEquals(GeneratedWaferFrameProjection.Resolve(project, project.InputFrame, ""), project.InputFrame), "Unconfigured legacy frame unchanged");
    }

    private static RecipeProject CreateProject(DieMap map)
    {
        return new RecipeProject
        {
            Die = new DieSubset { WidthMm = 10.370, HeightMm = 7.913 },
            InputFrame = FrameFor(map), OutputFrame = new TapeFrameSubset(), MapApprovalVersion = 1,
            InputMapApprovalHash = "", GoodBinMapApprovalHash = "", NgBinMapApprovalHash = ""
        };
    }

    private static void VerifySavedBoundarySpecWrite(string root)
    {
        foreach (int version in new[] { 2, 3, 4 })
            VerifySavedBoundarySpecWrite(root, version);
    }

    private static void VerifySavedBoundarySpecWrite(string root, int version)
    {
        var basis = WaferMapGeneration.Generate(new WaferMapGenerationSettings(
            200m, 10.370m, 7.913m, .1m, .3m, .2m, version));
        var rectangle = WaferMapGeneration.ApplyCounts(basis, new WaferMapEdgeCounts(
            basis.UsedColumns, basis.UsedColumns, basis.UsedRows, basis.UsedRows),
            basis.UsedColumns * basis.UsedRows);
        Assert(rectangle.OutOfBoundsCount > 0, "Full bounding rectangle contains visible out-of-bound dies");
        foreach (bool output in new[] { false, true })
        {
            DieMap map = GeneratedWaferMapCodec.ToDieMap(basis, output ? "OUTPUT_BOUNDARY" : "INPUT_BOUNDARY");
            RecipeProject project = CreateProject(map);
            TapeFrameSubset frame = output ? FrameFor(map) : project.InputFrame;
            if (output) project.OutputFrame = frame;
            string path = Path.Combine(root, (output ? "output" : "input") + "-boundary-v" + version + ".json");
            if (output)
            {
                project.OutputBaseWaferMapFileName = path;
                project.GoodBinDieMapFileName = path;
            }
            else
            {
                project.InputBaseWaferMapFileName = path;
                project.InputDieMapFileName = path;
            }
            DieMapGenerator.SaveJson(map, path);
            MaterialSpecs.Reset();
            MaterialStateService.VerifyEnsureFrame(project, frame, map.FrameObjId, path);
            Assert(MaterialSpecs.Writes == 1, "Valid inward-margin map reaches MaterialSpecs: " + output);

            // Saved boundary overflow is accepted without clipping; definition and coordinate checks remain.
            map = GeneratedWaferMapCodec.ToDieMap(rectangle, map.FrameObjId);
            string reason;
            Assert(GeneratedWaferMapCodec.ValidateForStorage(map, out reason), "Complete outside draft is valid for storage: " + reason);
            Assert(GeneratedWaferMapCodec.Validate(map, out reason), "Saved outside geometry passes runtime validation: " + reason);
            Assert(map.Entries.Count == rectangle.Count && GeneratedWaferMapCodec.Restore(map.Generation).OutOfBoundsCount == rectangle.OutOfBoundsCount,
                "Runtime conversion preserves all out-of-bound cells and saved definition");
            DieMapGenerator.SaveJson(map, path);
            string before = Snapshot(frame);
            MaterialSpecs.Reset();
            MaterialStateService.VerifyEnsureFrame(project, frame, map.FrameObjId, path);
            Assert(MaterialSpecs.Writes == 1 && MaterialStateService.DieSpecWrites == 1,
                "Saved boundary overflow reaches the normal specification adapter: " + output);
            Assert(Snapshot(frame) == before, "Boundary acceptance preserves original frame: " + output);

            string csv = Path.ChangeExtension(path, ".csv");
            DieMapGenerator.SaveCsv(map, csv);
            if (output) project.OutputBaseWaferMapFileName = csv;
            else project.InputBaseWaferMapFileName = csv;
            MaterialSpecs.Reset();
            MaterialStateService.VerifyEnsureFrame(project, frame, map.FrameObjId, path);
            Assert(MaterialSpecs.Writes == 1 && MaterialStateService.DieSpecWrites == 1,
                "CSV boundary definition reaches the same normal adapter: " + output);

            map.Entries[0].PosX += .0005;
            if (output) project.OutputBaseWaferMapFileName = path;
            else project.InputBaseWaferMapFileName = path;
            DieMapGenerator.SaveJson(map, path);
            MaterialSpecs.Reset();
            Rejected(() => MaterialStateService.VerifyEnsureFrame(project, frame, map.FrameObjId, path),
                "Boundary acceptance still rejects corrupted saved coordinates: " + output);
            Assert(MaterialSpecs.Writes == 0 && MaterialStateService.DieSpecWrites == 0 && Snapshot(frame) == before,
                "Corrupted coordinates are rejected before specification writes or frame changes: " + output);
        }
    }

    private static void VerifyJmbSavedBoundarySpecWrite(string root)
    {
        foreach (int version in new[] { 3, 4 })
        foreach (int angle in new[] { 0, 180 })
        {
            var basis = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(
                new WaferMapGenerationSettings(287.640m, 8.070m, 6.070m, .050m, .050m, .200m, version)), angle);
            var generated = WaferMapGeneration.ApplyCounts(basis, new WaferMapEdgeCounts(3, 3, 9, 9), 1257);
            Assert(generated.Count == 1257 && generated.OutOfBoundsCount == 38,
                "Production JMB settings reproduce exactly 38 boundary-overflow dies");
            DieMap map = GeneratedWaferMapCodec.ToDieMap(generated, "JMB_INPUT");
            RecipeProject project = CreateProject(map);
            project.Die.WidthMm = 8.070;
            project.Die.HeightMm = 6.070;
            string path = Path.Combine(root, "jmb-v" + version + "-" + angle + ".json");
            project.InputBaseWaferMapFileName = path;
            project.InputDieMapFileName = path;
            DieMapGenerator.SaveJson(map, path);
            DieMap restored = DieMapGenerator.LoadJson(path);
            Assert(restored.Entries.Count == 1257 && GeneratedWaferMapCodec.Restore(restored.Generation).OutOfBoundsCount == 38,
                "Saved JMB reload keeps all 1257 dies, including 38 outside");
            string before = Snapshot(project.InputFrame);
            MaterialSpecs.Reset();
            MaterialStateService.VerifyEnsureFrame(project, project.InputFrame, "JMB_INPUT", path);
            Assert(MaterialSpecs.Writes == 1 && MaterialStateService.DieSpecWrites == 1 &&
                MaterialSpecs.LastFrame.DieMapX == 35 && MaterialSpecs.LastFrame.DieMapY == 47,
                "Saved JMB with 38 outside dies reaches the normal frame specification adapter");
            Assert(Snapshot(project.InputFrame) == before && project.InputMapApprovalHash == "",
                "JMB projection preserves frame geometry and does not bypass separate FINAL APPLY approval");
        }
    }

    private static void VerifySelectedPhaseSpec(string root)
    {
        foreach (decimal margin in new[] { 0m, .2m })
        foreach (int angle in new[] { 0, 180 })
        {
            var basis = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(
                new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .3m, .3m, margin, 4)), angle);
            var generated = WaferMapGeneration.ApplyCounts(basis, new WaferMapEdgeCounts(6, 6, 8, 8), 684);
            Assert(generated.Count == 684 && generated.UsedColumns == 26 && generated.UsedRows == 34 &&
                generated.OutOfBoundsCount == 0, "RKE selected phase is physically valid before spec projection");
            DieMap map = GeneratedWaferMapCodec.ToDieMap(generated, "RKE_OUTPUT_PHASE");
            RecipeProject project = CreateProject(map);
            project.OutputFrame = FrameFor(map);
            string path = Path.Combine(root, "rke-phase-" + angle + "-" + (int)(margin * 1000m) + ".json");
            project.OutputBaseWaferMapFileName = path;
            project.GoodBinDieMapFileName = path;
            DieMapGenerator.SaveJson(map, path);
            MaterialSpecs.Reset();
            string before = Snapshot(project.OutputFrame);
            MaterialStateService.VerifyEnsureFrame(project, project.OutputFrame, "RKE_OUTPUT_PHASE", path);
            Assert(MaterialSpecs.Writes == 1 && MaterialSpecs.LastFrame.DieMapX == 26 &&
                MaterialSpecs.LastFrame.DieMapY == 34, "Valid V4 RKE map reaches existing physical specification adapter");
            Assert(Same(MaterialSpecs.LastFrame.DieSizeX + MaterialSpecs.LastFrame.PitchX, 10.670) &&
                Same(MaterialSpecs.LastFrame.DieSizeY + MaterialSpecs.LastFrame.PitchY, 8.213),
                "Selected phase preserves die and gap dimensions");
            Assert(Snapshot(project.OutputFrame) == before,
                "Selected phase does not rewrite original recipe dimensions during projection");
        }
    }

    private static void VerifyLegacyAliasSync(string root)
    {
        var generated = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(
            new WaferMapGenerationSettings(200m, 10.370m, 7.913m, .1m, .3m)), 180);
        DieMap map = GeneratedWaferMapCodec.ToDieMap(generated, "INPUT_ALIAS");
        RecipeProject project = CreateProject(map);
        project.InputDieMapFileName = Path.Combine(root, "alias-role.json");
        project.InputBaseWaferMapFileName = project.InputDieMapFileName;
        DieMapGenerator.SaveJson(map, project.InputDieMapFileName);
        project.Frame = RecipeProjectConsistencyService.CloneFrame(project.InputFrame);
        project.Frame.PitchX = 99; // Stale legacy alias must not overwrite the newly synchronized Input specification.
        MaterialSpecs.Reset();
        Assert(MaterialStateService.SyncRecipeTapeFrameSpec(project) == "INPUT_ALIAS", "Sync accepts stale duplicate legacy alias");
        Assert(MaterialSpecs.Writes == 2, "Duplicate legacy alias does not cause a third Upsert");
        Assert(MaterialSpecs.MapFiles["INPUT_ALIAS"] == project.InputDieMapFileName, "Input map file is retained instead of blanked by legacy alias");
        Assert(Same(MaterialSpecs.Frames["INPUT_ALIAS"].DieSizeX + MaterialSpecs.Frames["INPUT_ALIAS"].PitchX, map.PitchX), "Supported Input pitch remains after sync");
        project.Frame.FrameSpecName = "SEPARATE_LEGACY";
        MaterialSpecs.Reset();
        Assert(MaterialStateService.SyncRecipeTapeFrameSpec(project) == "INPUT_ALIAS", "Separate legacy name remains supported");
        Assert(MaterialSpecs.Writes == 3 && MaterialSpecs.Frames["SEPARATE_LEGACY"].PitchX == 99, "Separate legacy specification preserves its prior values");
    }

    private static TapeFrameSubset FrameFor(DieMap map)
    {
        var d = map.Generation;
        return new TapeFrameSubset
        {
            FrameSpecName = map.FrameObjId, DieMapX = map.DieMapX, DieMapY = map.DieMapY,
            DieSizeX = (double)d.DieSizeXMm, DieSizeY = (double)d.DieSizeYMm,
            PitchX = (double)d.GapXMm, PitchY = (double)d.GapYMm,
            OuterDiameterMm = (double)d.OuterDiameterMm, Rotate = RecipeDieMapResolver.GetGeneratedRotationToken(d.RotationDegrees),
            EdgeSkipMode = "ExternalMap"
        };
    }
    private static string Snapshot(TapeFrameSubset f) => f.DieMapX + "," + f.DieMapY + "," + f.DieSizeX + "," + f.DieSizeY + "," + f.PitchX + "," + f.PitchY + "," + f.OuterDiameterMm + "," + f.Rotate;
    private static bool Same(double a, double b) => Math.Abs(a - b) < .00000001;
    private static void Rejected(Action action, string message)
    {
        bool failed = false;
        try { action(); } catch (InvalidDataException) { failed = true; }
        Assert(failed, message);
    }
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _assertions++;
    }
}

namespace QMC.CDT320.Materials
{
    public static partial class MaterialStateService
    {
        public static int DieSpecWrites;
        public static void VerifyEnsureFrame(RecipeProject project, TapeFrameSubset frame, string name, string path)
        {
            DieSpecWrites = 0;
            EnsureTapeFrameSpecFromFrame(project, frame, name, path);
        }
        private static void EnsureDieSpecFromRecipe(RecipeProject project, string name) { DieSpecWrites++; }
        private static string ResolveDefaultTapeFrameSpecName(int inches) { return "DEFAULT"; }
    }
    public static class MaterialSpecs
    {
        public static object Data = new object();
        public static int Writes;
        public static TapeFrameSubset LastFrame;
        public static Dictionary<string, TapeFrameSubset> Frames = new Dictionary<string, TapeFrameSubset>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, string> MapFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static void Reset() { Writes = 0; LastFrame = null; Frames.Clear(); MapFiles.Clear(); }
        public static void UpsertFrame(string name, int x, int y, double gapX, double gapY, double dieX, double dieY,
            double diameter, string mode, int sides, int ends, double sideMm, double endMm, string path, string dieName)
        {
            Writes++;
            LastFrame = new TapeFrameSubset { FrameSpecName = name, DieMapX = x, DieMapY = y, PitchX = gapX, PitchY = gapY,
                DieSizeX = dieX, DieSizeY = dieY, OuterDiameterMm = diameter, EdgeSkipMode = mode };
            Frames[name] = LastFrame;
            MapFiles[name] = path;
        }
    }
}
