using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;

internal static class VerifyWaferMapStorage
{
    private static int _assertions;

    private static int Main()
    {
        try
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            Assert(Path.GetFileName(directory).StartsWith("verify-wafer-map-storage-", StringComparison.Ordinal), "dedicated storage sandbox");
            GeneratedWaferMap original = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.400m, 10.370m, 7.913m, 0.300m, 0.300m));
            Assert(original.Count == 684, "reference formula fixture: 684");
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                if (angle == 90 || angle == 270) VerifyUnsupportedQuarterDraft(original, angle);
                else VerifyRoleSave(original, angle);
            }
            VerifyRollbackAndMismatch(original);
            VerifyExternalLoad();
            VerifyLegacyApprovalMigration(original);
            VerifyFrameSpecNameIsolation(original);
            VerifyVersionTwoStorage();
            VerifyOverflowDraftFixtures();
            VerifyVersionThreeStorage();
            VerifyVersionFourStorage();
            Console.WriteLine("PASS: " + _assertions + " storage assertions; actual generator, JSON/CSV, role service, resolver and mask approval code.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void VerifyVersionFourStorage()
    {
        foreach (decimal margin in new[] { 0m, .2m })
        {
            GeneratedWaferMap basis = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .300m, .300m, margin, 4));
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                bool quarter = angle == 90 || angle == 270;
                var edges = quarter ? new WaferMapEdgeCounts(8, 8, 6, 6) : new WaferMapEdgeCounts(6, 6, 8, 8);
                GeneratedWaferMap generated = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Rotate(basis, angle), edges, 684);
                Assert(generated.Count == 684 && generated.OutOfBoundsCount == 0, "V4 RKE shifted phase fits all 684 dies");
                foreach (bool output in new[] { false, true })
                {
                    RecipeProject project = NewProject("v4-rke-" + margin.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + angle + "-" + output);
                    RecipeMapBuildResult other = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, basis, !output, p => true);
                    Assert(other.Success, "V4 other role fixture save");
                    foreach (RecipeMapKind otherKind in output ? new[] { RecipeMapKind.Input } : new[] { RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
                        Assert(RecipeMapBuildService.SaveRoleTargetMask(project, otherKind, DieMapGenerator.LoadJson(RecipeMapPaths.ResolveConfigured(project, otherKind)), p => true).Success, "V4 unselected role starts approved");
                    Dictionary<string, byte[]> otherFiles = ReadFiles(output ? new[] { other.BaseMapPath, other.InputMapPath } : new[] { other.BaseMapPath, other.GoodMapPath, other.NgMapPath });
                    string otherFrame = FrameSnapshot(output ? project.InputFrame : project.OutputFrame);
                    string otherHash = output ? project.InputMapApprovalHash : project.GoodBinMapApprovalHash + "|" + project.NgBinMapApprovalHash;
                    int commits = 0;
                    RecipeMapBuildResult saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, generated, output, p => { commits++; return true; });
                    Assert(saved.Success && saved.PersistenceStarted && commits == 1 && saved.TargetCount == 684, "V4 phase-corrected role save: " + saved.Message);
                    Assert(saved.RoleMap.Generation.Version == 4 && saved.RoleMap.Generation.EdgeMarginMm == margin && saved.RoleMap.Generation.TotalCount == 684, "V4 definition survives role service clones");
                    Assert(output ? project.GoodBinMapApprovalHash == "" && project.NgBinMapApprovalHash == "" : project.InputMapApprovalHash == "", "V4 newly saved role is PENDING");
                    Assert(otherFrame == FrameSnapshot(output ? project.InputFrame : project.OutputFrame) && otherHash == (output ? project.InputMapApprovalHash : project.GoodBinMapApprovalHash + "|" + project.NgBinMapApprovalHash), "V4 save preserves unselected specification and approvals");
                    AssertFiles(otherFiles, "V4 save preserves unselected map family bytes");
                    string[] paths = output ? new[] { saved.BaseMapPath, saved.GoodMapPath, saved.NgMapPath } : new[] { saved.BaseMapPath, saved.InputMapPath };
                    foreach (string path in paths)
                    {
                        VerifyGeometry(DieMapGenerator.LoadJson(path), generated);
                        VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv")), generated);
                        Assert(RecipeMapPaths.ComputeApprovalHash(DieMapGenerator.LoadJson(path)) == RecipeMapPaths.ComputeApprovalHash(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv"))), "V4 exact phase produces identical JSON and CSV approval hashes");
                    }
                    RecipeMapBuildResult rebuilt = RecipeMapBuildService.RebuildDerivedMaps(project, !output, output, true, p => true);
                    Assert(rebuilt.Success, "V4 selected-phase rebuild succeeds: " + rebuilt.Message);
                    foreach (string path in paths) VerifyGeometry(DieMapGenerator.LoadJson(path), generated);
                    foreach (RecipeMapKind kind in output ? new[] { RecipeMapKind.GoodBin, RecipeMapKind.NgBin } : new[] { RecipeMapKind.Input })
                    {
                        string path = RecipeMapPaths.ResolveConfigured(project, kind);
                        DieMap map = DieMapGenerator.LoadJson(path);
                        string reason;
                        Assert(RecipeDieMapResolver.IsCompatibleWithFrame(map, output ? project.OutputFrame : project.InputFrame, out reason), "V4 shifted phase remains compatible with the original recipe dimensions");
                        Assert(!RecipeMapPaths.IsMapApproved(project, kind, map, out reason), "V4 requires explicit FINAL APPLY after draft save");
                        map.Entries[0].IsTarget = false;
                        Dictionary<string, byte[]> beforeApply = ReadFiles(paths);
                        commits = 0;
                        RecipeMapBuildResult applied = RecipeMapBuildService.SaveRoleTargetMask(project, kind, map, p => { commits++; return true; });
                        Assert(applied.Success == !quarter && commits == (quarter ? 0 : 1), "V4 safe RKE permits 0/180 FINAL APPLY; quarter turns remain drafts");
                        if (quarter) AssertFiles(beforeApply, "V4 unsupported-angle FINAL APPLY never changes map files");
                        else
                        {
                            DieMap approved = DieMapGenerator.LoadJson(path);
                            VerifyGeometry(approved, generated);
                            Assert(!approved.Entries[0].IsTarget && RecipeMapPaths.IsMapApproved(project, kind, approved, out reason), "V4 target mask approval preserves exact selected phase");
                        }
                        string loadedPath;
                        Assert((RecipeDieMapResolver.LoadCompatibleMap(project, kind, out loadedPath, out reason) != null) == !quarter, "V4 runtime retains the supported-angle approval boundary");
                        DieMap altered = DieMapGenerator.LoadJson(path);
                        string hash = RecipeMapPaths.ComputeApprovalHash(altered);
                        altered.Generation.Version = 3;
                        Assert(RecipeMapPaths.ComputeApprovalHash(altered) != hash && !GeneratedWaferMapCodec.ValidateForStorage(altered, out reason), "V4 to V3 metadata downgrade invalidates hash and phase geometry");
                    }
                    AssertFiles(otherFiles, "V4 selected rebuild and FINAL APPLY preserve the other role");
                }
            }
        }
        foreach (bool output in new[] { false, true })
        {
            decimal gap = output ? .300m : .050m;
            int total = output ? 1142 : 1257;
            int horizontal = output ? 2 : 3;
            int vertical = output ? 7 : 9;
            GeneratedWaferMap basis = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 8.070m, 6.070m, gap, gap, .200m, 4));
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                bool quarter = angle == 90 || angle == 270;
                var edges = quarter ? new WaferMapEdgeCounts(vertical, vertical, horizontal, horizontal) : new WaferMapEdgeCounts(horizontal, horizontal, vertical, vertical);
                GeneratedWaferMap generated = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Rotate(basis, angle), edges, total);
                Assert(generated.Count == total && generated.OutOfBoundsCount == (output ? 4 : 38), "V4 default-first JMB keeps existing count and boundary-overflow condition");
                RecipeProject project = NewProject("v4-jmb-" + output + "-" + angle);
                project.Die.WidthMm = 8.070;
                project.Die.HeightMm = 6.070;
                RecipeMapBuildResult saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, generated, output, p => true);
                Assert(saved.Success && saved.RoleMap.Generation.Version == 4 && saved.TargetCount == total, "V4 JMB outside cells remain saveable: " + saved.Message);
                string[] paths = output ? new[] { saved.BaseMapPath, saved.GoodMapPath, saved.NgMapPath } : new[] { saved.BaseMapPath, saved.InputMapPath };
                foreach (string path in paths)
                {
                    VerifyGeometry(DieMapGenerator.LoadJson(path), generated);
                    VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv")), generated);
                }
                Dictionary<string, byte[]> before = ReadFiles(paths);
                foreach (RecipeMapKind kind in output ? new[] { RecipeMapKind.GoodBin, RecipeMapKind.NgBin } : new[] { RecipeMapKind.Input })
                {
                    DieMap map = DieMapGenerator.LoadJson(RecipeMapPaths.ResolveConfigured(project, kind));
                    string reason;
                    Assert(RecipeDieMapResolver.IsSupportedForEquipment(map, out reason) == !quarter, "V4 JMB overflow preserves supported-angle policy");
                    Assert(!RecipeMapPaths.IsMapApproved(project, kind, map, out reason), "V4 JMB saved map still requires explicit FINAL APPLY");
                    int commits = 0;
                    RecipeMapBuildResult applied = RecipeMapBuildService.SaveRoleTargetMask(project, kind, map, p => { commits++; return true; });
                    Assert(applied.Success == !quarter && commits == (quarter ? 0 : 1), "V4 JMB overflow permits supported-angle FINAL APPLY: " + applied.Message);
                    Assert(RecipeMapPaths.IsMapApproved(project, kind, DieMapGenerator.LoadJson(RecipeMapPaths.ResolveConfigured(project, kind)), out reason) == !quarter, "V4 JMB FINAL APPLY grants approval only at supported angles");
                    string loadedPath;
                    Assert((RecipeDieMapResolver.LoadCompatibleMap(project, kind, out loadedPath, out reason) != null) == !quarter, "V4 JMB overflow loads at supported angles after approval");
                }
                if (quarter) AssertFiles(before, "V4 unsupported-angle application leaves exact draft files unchanged");
                foreach (string path in paths) VerifyGeometry(DieMapGenerator.LoadJson(path), generated);
            }
        }
    }

    private static void VerifyVersionThreeStorage()
    {
        foreach (bool output in new[] { false, true })
        {
            decimal gap = output ? .300m : .050m;
            int total = output ? 1142 : 1257;
            int expectedOutside = output ? 4 : 38;
            int edgeHorizontal = output ? 2 : 3;
            int edgeVertical = output ? 7 : 9;
            GeneratedWaferMap basis = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 8.070m, 6.070m, gap, gap, .200m, 3));
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                bool quarter = angle == 90 || angle == 270;
                var edges = quarter ? new WaferMapEdgeCounts(edgeVertical, edgeVertical, edgeHorizontal, edgeHorizontal)
                    : new WaferMapEdgeCounts(edgeHorizontal, edgeHorizontal, edgeVertical, edgeVertical);
                GeneratedWaferMap rotated = WaferMapGeneration.Rotate(basis, angle);
                GeneratedWaferMap generated = WaferMapGeneration.ApplyCounts(rotated, edges, total);
                Assert(generated.Count == total && generated.OutOfBoundsCount == expectedOutside, "V3 JMB exact count and boundary-overflow fixture");
                int columns = output ? 34 : 35;
                int rows = output ? 45 : 47;
                Assert(generated.UsedColumns == (quarter ? rows : columns) && generated.UsedRows == (quarter ? columns : rows), "V3 JMB full row/column bounds survive rotation");
                RecipeProject project = NewProject("v3-jmb-" + output + "-" + angle);
                project.Die.WidthMm = 8.070;
                project.Die.HeightMm = 6.070;
                string otherFrame = FrameSnapshot(output ? project.InputFrame : project.OutputFrame);
                string otherHash = output ? project.InputMapApprovalHash : project.GoodBinMapApprovalHash + "|" + project.NgBinMapApprovalHash;
                RecipeMapKind kind = output ? RecipeMapKind.GoodBin : RecipeMapKind.Input;
                RecipeMapBuildResult original = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, rotated, output, p => true);
                Assert(original.Success, "V3 unadjusted save");
                RecipeMapBuildResult originalApply = RecipeMapBuildService.SaveRoleTargetMask(project, kind, original.RoleMap, p => true);
                Assert(originalApply.Success == !quarter, "V3 safe baseline permits only 0/180 operational approval");
                RecipeMapBuildResult saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, generated, output, p => true);
                Assert(saved.Success && saved.TargetCount == total && saved.RoleMap.Generation.Version == 3, "V3 JMB generated service save: " + saved.Message);
                Assert(otherFrame == FrameSnapshot(output ? project.InputFrame : project.OutputFrame), "V3 JMB save preserves other frame");
                Assert(otherHash == (output ? project.InputMapApprovalHash : project.GoodBinMapApprovalHash + "|" + project.NgBinMapApprovalHash), "V3 JMB save preserves other role approval");
                string path = output ? saved.GoodMapPath : saved.InputMapPath;
                VerifyGeometry(saved.BaseMap, generated);
                VerifyGeometry(DieMapGenerator.LoadJson(path), generated);
                VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv")), generated);
                if (output) VerifyGeometry(DieMapGenerator.LoadJson(saved.NgMapPath), generated);
                string hash = RecipeMapPaths.ComputeApprovalHash(saved.RoleMap);
                Assert(hash == RecipeMapPaths.ComputeApprovalHash(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv"))), "V3 JSON/CSV approval fingerprint matches");
                DieMap changed = DieMapGenerator.LoadJson(path);
                changed.Generation.Version = 2;
                Assert(hash != RecipeMapPaths.ComputeApprovalHash(changed), "generation V2/V3 change always invalidates approval hash");
                string reason;
                Assert(RecipeDieMapResolver.IsCompatibleWithFrame(saved.RoleMap, output ? project.OutputFrame : project.InputFrame, out reason), "V3 draft remains compatible with its role specification");
                Assert(!RecipeMapPaths.IsMapApproved(project, kind, saved.RoleMap, out reason), "V3 JMB overflow draft is PENDING and not operationally approved");
                int commits = 0;
                RecipeMapBuildResult apply = RecipeMapBuildService.SaveRoleTargetMask(project, kind, saved.RoleMap, p => { commits++; return true; });
                Assert(apply.Success == !quarter && commits == (quarter ? 0 : 1), "V3 JMB 38/4 outside dies permit supported-angle FINAL APPLY: " + apply.Message);
                Assert(RecipeMapPaths.IsMapApproved(project, kind, DieMapGenerator.LoadJson(path), out reason) == !quarter, "V3 JMB supported-angle approval survives reload");
                string actualPath;
                Assert((RecipeDieMapResolver.LoadCompatibleMap(project, kind, out actualPath, out reason) != null) == !quarter, "V3 JMB saved boundary overflow permits supported-angle runtime load");
                RecipeMapBuildResult rebuilt = RecipeMapBuildService.RebuildDerivedMaps(project, !output, output, true, p => true);
                Assert(rebuilt.Success, "V3 JMB draft rebuild: " + rebuilt.Message);
                VerifyGeometry(DieMapGenerator.LoadJson(path), generated);
            }
        }

        var edges730 = new WaferMapEdgeCounts(3, 3, 8, 8);
        GeneratedWaferMap v2 = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .1m, .1m, .2m)), edges730, 730);
        GeneratedWaferMap v3 = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .1m, .1m, .2m, 3)), edges730, 730);
        var expected = v2.Dies.ToDictionary(d => d.RawColumn + "," + d.RawRow);
        Assert(v3.Count == 730 && v3.OutOfBoundsCount == v2.OutOfBoundsCount && v3.Dies.All(d =>
            expected.ContainsKey(d.RawColumn + "," + d.RawRow) && expected[d.RawColumn + "," + d.RawRow].CenterXMm == d.CenterXMm &&
            expected[d.RawColumn + "," + d.RawRow].CenterYMm == d.CenterYMm), "V3 RKE 730 fallback reproduces the existing manual geometry");
        RecipeProject fallbackProject = NewProject("v3-rke-fallback");
        RecipeMapBuildResult fallbackSave = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(fallbackProject, v3, false, p => true);
        Assert(fallbackSave.Success, "V3 RKE fallback storage");
        VerifyGeometry(DieMapGenerator.LoadJson(fallbackSave.InputMapPath), v3);
        VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(fallbackSave.InputMapPath, ".csv")), v3);
    }

    private static void VerifyVersionTwoStorage()
    {
        GeneratedWaferMap source = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.400m, 10.370m, 7.913m, .300m, .300m, .200m));
        Assert(source.Settings.GenerationVersion == 2 && source.OutOfBoundsCount == 0, "V2 original circle is within its saved margin");
        foreach (int angle in new[] { 0, 90, 180, 270 })
        {
            GeneratedWaferMap rotated = WaferMapGeneration.Rotate(source, angle);
            GeneratedWaferMap same = WaferMapGeneration.ApplyCounts(rotated, rotated.EdgeCounts, rotated.Count);
            string initialHash = RecipeMapPaths.ComputeApprovalHash(GeneratedWaferMapCodec.ToDieMap(rotated, "V2_IDENTITY"));
            Assert(initialHash == RecipeMapPaths.ComputeApprovalHash(GeneratedWaferMapCodec.ToDieMap(same, "V2_IDENTITY")), "unchanged V2 count settings keep exact definition/geometry/hash");
            GeneratedWaferMap reduced = WaferMapGeneration.ApplyCounts(rotated, rotated.EdgeCounts, rotated.Count - 3);
            Assert(reduced.Count == rotated.Count - 3 && reduced.OutOfBoundsCount == 0, "V2 total correction fixture remains inside wafer");
            foreach (bool output in new[] { false, true })
            {
                RecipeProject project = NewProject("v2-" + angle + "-" + output);
                int commits = 0;
                RecipeMapBuildResult saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, reduced, output, p => { commits++; return true; });
                Assert(saved.Success && saved.PersistenceStarted && commits == 1, "V2 role configuration save: " + saved.Message);
                Assert(saved.RoleMap.Generation.Version == 2 && saved.RoleMap.Generation.EdgeMarginMm == .2m &&
                    saved.RoleMap.Generation.TotalCount == reduced.Count, "V2 margin and requested total survive all service clones");
                string path = output ? saved.GoodMapPath : saved.InputMapPath;
                RecipeMapKind kind = output ? RecipeMapKind.GoodBin : RecipeMapKind.Input;
                VerifyGeometry(saved.BaseMap, reduced);
                VerifyGeometry(saved.RoleMap, reduced);
                VerifyGeometry(DieMapGenerator.LoadJson(path), reduced);
                VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv")), reduced);
                Assert(RecipeMapPaths.ComputeApprovalHash(saved.RoleMap) == RecipeMapPaths.ComputeApprovalHash(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv"))), "V2 CSV preserves approval hash exactly");
                string reason;
                Assert(RecipeDieMapResolver.IsCompatibleWithFrame(saved.RoleMap, output ? project.OutputFrame : project.InputFrame, out reason), "V2 frame compatibility: " + reason);
                RecipeMapBuildResult apply = RecipeMapBuildService.SaveRoleTargetMask(project, kind, saved.RoleMap, p => true);
                Assert(apply.Success == (angle == 0 || angle == 180), "V2 only 0/180 may FINAL APPLY; 90/270 remain draft");
                string loadedPath;
                Assert((RecipeDieMapResolver.LoadCompatibleMap(project, kind, out loadedPath, out reason) != null) == (angle == 0 || angle == 180), "V2 runtime rotation support remains gated");

                DieMap changed = DieMapGenerator.LoadJson(path);
                string savedHash = RecipeMapPaths.ComputeApprovalHash(changed);
                changed.Generation.EdgeMarginMm += .001m;
                Assert(RecipeMapPaths.ComputeApprovalHash(changed) != savedHash, "V2 margin changes invalidate hash even before coordinates change");
                changed.Generation.EdgeMarginMm -= .001m;
                changed.Generation.TotalCount++;
                Assert(RecipeMapPaths.ComputeApprovalHash(changed) != savedHash, "V2 total-count changes invalidate hash");

                GeneratedWaferMap outside = WaferMapGeneration.ApplyCounts(rotated, rotated.EdgeCounts, rotated.Count + 20);
                Assert(outside.OutOfBoundsCount > 0, "V2 can preview requested count outside wafer");
                GeneratedWaferMapDefinition outsideDefinition = GeneratedWaferMapCodec.CreateDefinition(outside);
                Assert(GeneratedWaferMapCodec.Restore(outsideDefinition).OutOfBoundsCount == outside.OutOfBoundsCount, "out-of-bound settings restore for preview");
                commits = 0;
                RecipeMapBuildResult draft = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, outside, output, p => { commits++; return true; });
                Assert(draft.Success && draft.PersistenceStarted && commits == 1, "V2 out-of-bound settings can be saved as a draft: " + draft.Message);
                VerifyGeometry(draft.BaseMap, outside);
                VerifyGeometry(DieMapGenerator.LoadJson(path), outside);
                VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv")), outside);
                Dictionary<string, byte[]> before = ReadFiles(saved.BaseMapPath, path);

                changed = DieMapGenerator.LoadJson(path);
                Assert(GeneratedWaferMapCodec.ValidateForStorage(changed, out reason), "out-of-bound draft preserves valid storage geometry");
                Assert(GeneratedWaferMapCodec.Validate(changed, out reason), "V2 saved out-of-bound geometry validates: " + reason);
                bool supported = angle == 0 || angle == 180;
                Assert(RecipeDieMapResolver.IsSupportedForEquipment(changed, out reason) == supported, "V2 out-of-bound condition retains rotation support gate");
                foreach (int version in new[] { 1, 0 })
                {
                    project.MapApprovalVersion = version;
                    if (output) project.GoodBinMapApprovalHash = RecipeMapPaths.ComputeApprovalHash(changed);
                    else project.InputMapApprovalHash = RecipeMapPaths.ComputeApprovalHash(changed);
                    Assert(RecipeMapPaths.IsMapApproved(project, kind, changed, out reason) == supported, "V2 matching hash and legacy approval follow existing supported-angle contract");
                    Assert((RecipeDieMapResolver.LoadCompatibleMap(project, kind, out loadedPath, out reason) != null) == supported, "V2 saved overflow loads with valid approval at supported angles");
                }
                bool rejected = false;
                try { RecipeMapPaths.ApproveMap(project, kind, changed); }
                catch (InvalidOperationException) { rejected = true; }
                Assert(rejected == !supported, "direct V2 saved overflow approval retains angle restriction");
                commits = 0;
                RecipeMapBuildResult applied = RecipeMapBuildService.SaveRoleTargetMask(project, kind, changed, p => { commits++; return true; });
                Assert(applied.Success == supported && commits == (supported ? 1 : 0), "V2 saved out-of-bound FINAL APPLY accepts supported angles: " + applied.Message);
                if (!supported) AssertFiles(before, "V2 unsupported-angle approval calls leave saved files unchanged");
                VerifyGeometry(DieMapGenerator.LoadJson(path), outside);
                DieMap corrupted = DieMapGenerator.LoadJson(path);
                corrupted.Entries[0].PosX += .0005;
                Assert(!GeneratedWaferMapCodec.Validate(corrupted, out reason), "V2 accepting overflow does not accept altered coordinates");
                commits = 0;
                RecipeMapBuildResult denied = RecipeMapBuildService.SaveRoleTargetMask(project, kind, corrupted, p => { commits++; return true; });
                Assert(!denied.Success && commits == 0, "V2 altered-coordinate FINAL APPLY is still rejected before writes");
                changed.Generation = null;
                Assert(!RecipeDieMapResolver.IsSupportedForEquipment(changed, out reason), "known generated source without definition is not an approved recipe map");
                Assert(!RecipeMapPaths.IsMapApproved(project, kind, changed, out reason), "known generated source without definition cannot use legacy approval shortcut");
                rejected = false;
                try { RecipeMapPaths.ApproveMap(project, kind, changed); }
                catch (InvalidOperationException) { rejected = true; }
                Assert(rejected, "known generated source without definition cannot be directly approved");
            }
        }
    }

    private static void VerifyOverflowDraftFixtures()
    {
        GeneratedWaferMap basis = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .100m, .100m, .200m));
        var requested = new WaferMapEdgeCounts(3, 3, 8, 8);
        GeneratedWaferMap overflow = WaferMapGeneration.ApplyCounts(basis, requested, 730);
        Assert(overflow.Count == 730 && overflow.OutOfBoundsCount == 21, "requested RKE draft: 730 dies including 21 outside");
        foreach (bool output in new[] { false, true })
        {
            RecipeProject project = NewProject("draft-730-" + output);
            RecipeMapBuildResult unselected = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, basis, !output, p => true);
            Assert(unselected.Success, "unselected-role fixture save");
            RecipeMapKind otherKind = output ? RecipeMapKind.Input : RecipeMapKind.GoodBin;
            Assert(RecipeMapBuildService.SaveRoleTargetMask(project, otherKind, unselected.RoleMap, p => true).Success, "unselected role approved before draft save");
            if (!output)
                Assert(RecipeMapBuildService.SaveRoleTargetMask(project, RecipeMapKind.NgBin, DieMapGenerator.LoadJson(unselected.NgMapPath), p => true).Success, "unselected NG approved before input draft save");
            string[] otherPaths = output ? new[] { unselected.BaseMapPath, unselected.InputMapPath }
                : new[] { unselected.BaseMapPath, unselected.GoodMapPath, unselected.NgMapPath };
            Dictionary<string, byte[]> otherFiles = ReadFiles(otherPaths);
            string otherFrame = FrameSnapshot(output ? project.InputFrame : project.OutputFrame);
            string otherApproval = output ? project.InputMapApprovalHash : project.GoodBinMapApprovalHash + "|" + project.NgBinMapApprovalHash;
            RecipeMapBuildResult saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, overflow, output, p => true);
            Assert(saved.Success && saved.TargetCount == 730 && saved.AddressCount == 730, "730 overflow cells persist without clipping or Skip: " + saved.Message);
            Assert(saved.BaseMap.Entries.All(e => e.IsTarget) && saved.RoleMap.Entries.All(e => e.IsTarget), "overflow is not converted into a Skip mask");
            Assert(output ? project.GoodBinMapApprovalHash == "" && project.NgBinMapApprovalHash == "" : project.InputMapApprovalHash == "", "selected overflow roles are PENDING");
            Assert(otherFrame == FrameSnapshot(output ? project.InputFrame : project.OutputFrame), "overflow save preserves unselected frame");
            Assert(otherApproval == (output ? project.InputMapApprovalHash : project.GoodBinMapApprovalHash + "|" + project.NgBinMapApprovalHash), "overflow save preserves unselected approval");
            AssertFiles(otherFiles, "overflow save preserves every unselected map file");
            string[] selectedPaths = output ? new[] { saved.BaseMapPath, saved.GoodMapPath, saved.NgMapPath }
                : new[] { saved.BaseMapPath, saved.InputMapPath };
            foreach (string path in selectedPaths)
            {
                VerifyGeometry(DieMapGenerator.LoadJson(path), overflow);
                VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(path, ".csv")), overflow);
            }
            Dictionary<string, byte[]> before = ReadFiles(selectedPaths);
            var next = WaferMapGeneration.ApplyCounts(basis, requested, 731);
            RecipeMapBuildResult failed = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, next, output, p => false);
            Assert(!failed.Success && failed.PersistenceStarted, "failed overflow draft transaction reports persistence attempt");
            AssertFiles(before, "failed draft transaction restores selected files");
            AssertFiles(otherFiles, "failed draft transaction preserves unselected files");
            RecipeMapBuildResult rebuilt = RecipeMapBuildService.RebuildDerivedMaps(project, !output, output, true, p => true);
            Assert(rebuilt.Success, "same-condition overflow draft rebuild is supported: " + rebuilt.Message);
            foreach (string path in selectedPaths) VerifyGeometry(DieMapGenerator.LoadJson(path), overflow);
            AssertFiles(otherFiles, "draft rebuild preserves unselected map family");
            foreach (RecipeMapKind kind in output ? new[] { RecipeMapKind.GoodBin, RecipeMapKind.NgBin } : new[] { RecipeMapKind.Input })
            {
                DieMap map = DieMapGenerator.LoadJson(RecipeMapPaths.ResolveConfigured(project, kind));
                string reason;
                Assert(!RecipeMapPaths.IsMapApproved(project, kind, map, out reason), "730 draft has no operational approval");
                int commits = 0;
                RecipeMapBuildResult applied = RecipeMapBuildService.SaveRoleTargetMask(project, kind, map, p => { commits++; return true; });
                Assert(applied.Success && commits == 1, "730 saved overflow FINAL APPLY succeeds: " + applied.Message);
                Assert(RecipeMapPaths.IsMapApproved(project, kind, DieMapGenerator.LoadJson(RecipeMapPaths.ResolveConfigured(project, kind)), out reason), "730 approval survives reload without clipping");
                string path;
                DieMap loaded = RecipeDieMapResolver.LoadCompatibleMap(project, kind, out path, out reason);
                Assert(loaded != null, "730 approved overflow map loads for runtime: " + reason);
                VerifyGeometry(loaded, overflow);
            }
        }

        var small = WaferMapGeneration.Generate(new WaferMapGenerationSettings(20m, 3m, 3m, .1m, .1m, .2m));
        var expanded = WaferMapGeneration.ApplyCounts(small, new WaferMapEdgeCounts(32, 32, 32, 32), 1152);
        Assert(expanded.Dies.Any(d => d.RawColumn < 0 || d.RawRow < 0), "expanded draft fixture includes signed raw addresses");
        RecipeProject signed = NewProject("signed-raw-draft");
        signed.Die.WidthMm = signed.Die.HeightMm = 3;
        RecipeMapBuildResult signedSave = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(signed, expanded, false, p => true);
        Assert(signedSave.Success, "signed raw draft save: " + signedSave.Message);
        VerifyGeometry(DieMapGenerator.LoadJson(signedSave.InputMapPath), expanded);
        VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(signedSave.InputMapPath, ".csv")), expanded);
        RecipeMapBuildResult signedRebuild = RecipeMapBuildService.RebuildDerivedMaps(signed, true, false, true, p => true);
        Assert(signedRebuild.Success, "signed raw draft rebuild: " + signedRebuild.Message);
        VerifyGeometry(DieMapGenerator.LoadJson(signedSave.InputMapPath), expanded);
    }

    private static void VerifyRoleSave(GeneratedWaferMap original, int angle)
    {
        RecipeProject project = NewProject("rotation-" + angle);
        GeneratedWaferMap rotated = WaferMapGeneration.Rotate(original, angle);
        WaferMapEdgeCounts edges = rotated.EdgeCounts;
        GeneratedWaferMap adjusted = WaferMapGeneration.ApplyEdgeCounts(rotated,
            new WaferMapEdgeCounts(Math.Max(1, edges.Top - 2), edges.Bottom, edges.Left, edges.Right));
        int commits = 0;
        RecipeMapBuildResult input = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, adjusted, false, p => { commits++; return true; });
        Assert(input.Success, "input save " + angle + ": " + input.Message);
        Assert(input.PersistenceStarted && commits == 1, "input family committed once");
        Assert(project.InputMapApprovalHash == "" && project.GoodBinMapApprovalHash == "old-good" && project.NgBinMapApprovalHash == "old-ng", "input approval isolation");
        Assert(project.OutputFrame.OuterDiameterMm == 200 && project.OutputFrame.DieMapX == 9, "input frame isolation");
        Assert(project.InputFrame.Rotate == RecipeDieMapResolver.GetGeneratedRotationToken(angle), "stored rotation token");
        Assert(project.InputFrame.DieMapX == adjusted.UsedColumns && project.InputFrame.DieMapY == adjusted.UsedRows, "saved bounding grid");
        Assert(project.InputFrame.OuterDiameterMm == 287.4 && project.InputFrame.PitchX == 0.3, "original diameter and gap saved");
        VerifyGeometry(input.BaseMap, adjusted);
        VerifyGeometry(input.RoleMap, adjusted);
        VerifyGeometry(DieMapGenerator.LoadJson(input.InputMapPath), adjusted);
        VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(input.InputMapPath, ".csv")), adjusted);
        string reason;
        Assert(RecipeDieMapResolver.IsCompatibleWithFrame(input.RoleMap, project.InputFrame, out reason), "rotated role compatibility: " + reason);
        RecipeMapBuildResult inputApproval = RecipeMapBuildService.SaveRoleTargetMask(project, RecipeMapKind.Input, input.RoleMap, p => true);
        Assert(inputApproval.Success, "input final apply: " + inputApproval.Message);
        Assert(RecipeMapPaths.IsMapApproved(project, RecipeMapKind.Input, inputApproval.RoleMap, out reason), "input approved");
        string approvedPath;
        Assert(RecipeDieMapResolver.LoadCompatibleMap(project, RecipeMapKind.Input, out approvedPath, out reason) != null, "0/180 approved input is usable");
        string inputHash = project.InputMapApprovalHash;
        Dictionary<string, byte[]> beforeOutput = ReadFiles(input.BaseMapPath, input.InputMapPath);

        RecipeMapBuildResult output = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, rotated, true, p => true);
        Assert(output.Success, "output save: " + output.Message);
        Assert(project.InputMapApprovalHash == inputHash && project.GoodBinMapApprovalHash == "" && project.NgBinMapApprovalHash == "", "output approval isolation");
        AssertFiles(beforeOutput, "output preserves input bytes");
        VerifyGeometry(output.BaseMap, rotated);
        VerifyGeometry(DieMapGenerator.LoadJson(output.GoodMapPath), rotated);
        VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(output.NgMapPath, ".csv")), rotated);
        DieMap good = DieMapGenerator.LoadJson(output.GoodMapPath);
        good.Entries[0].IsTarget = false;
        RecipeMapBuildResult goodApproval = RecipeMapBuildService.SaveRoleTargetMask(project, RecipeMapKind.GoodBin, good, p => true);
        Assert(goodApproval.Success, "good final apply: " + goodApproval.Message);
        Assert(RecipeMapPaths.IsMapApproved(project, RecipeMapKind.GoodBin, goodApproval.RoleMap, out reason), "good approved independently");
        Assert(RecipeDieMapResolver.LoadCompatibleMap(project, RecipeMapKind.GoodBin, out approvedPath, out reason) != null, "0/180 approved output is usable");
        Assert(!RecipeMapPaths.IsMapApproved(project, RecipeMapKind.NgBin, DieMapGenerator.LoadJson(output.NgMapPath), out reason), "NG pending independently");
        Assert(DieMapGenerator.LoadJson(output.BaseMapPath).Entries.All(e => e.IsTarget), "base unaffected by role skip");
        RecipeMapBuildResult rebuild = RecipeMapBuildService.RebuildDerivedMaps(project, false, true, true, p => true);
        Assert(rebuild.Success, "same-condition rebuild: " + rebuild.Message);
        DieMap rebuiltGood = DieMapGenerator.LoadJson(output.GoodMapPath);
        Assert(rebuiltGood.Entries.Count(e => e.IsTarget) == rotated.Count - 1, "rebuild preserves GOOD mask");
        Assert(DieMapGenerator.LoadJson(output.NgMapPath).Entries.All(e => e.IsTarget), "rebuild preserves independent NG mask");
        VerifyGeometry(rebuiltGood, rotated);
        AssertFiles(beforeOutput, "rebuild output preserves input bytes");

        // Mapping 완료 맵은 절대 좌표로 바뀌며 생성 정의가 없을 수 있다. 기존 주소 비교는 유지한다.
        DieMap mapped = DieMapGenerator.LoadJson(input.InputMapPath);
        mapped.Generation = null;
        foreach (DieMapEntry entry in mapped.Entries) { entry.PosX += 400; entry.PosY += 600; }
        Assert(RecipeDieMapResolver.IsMappedInputCompatibleWithRecipe(mapped, inputApproval.RoleMap, out reason), "mapped runtime address compatibility");
    }

    private static void VerifyUnsupportedQuarterDraft(GeneratedWaferMap original, int angle)
    {
        GeneratedWaferMap rotated = WaferMapGeneration.Rotate(original, angle);
        WaferMapEdgeCounts edges = rotated.EdgeCounts;
        GeneratedWaferMap draft = WaferMapGeneration.ApplyEdgeCounts(rotated,
            new WaferMapEdgeCounts(Math.Max(1, edges.Top - 2), edges.Bottom, edges.Left, edges.Right));
        foreach (bool output in new[] { false, true })
        {
            RecipeProject project = NewProject("unsupported-" + angle + "-" + output);
            RecipeMapBuildResult saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, draft, output, p => true);
            Assert(saved.Success && saved.PersistenceStarted, "quarter preview configuration can be saved");
            VerifyGeometry(saved.BaseMap, draft);
            VerifyGeometry(saved.RoleMap, draft);
            RecipeMapKind kind = output ? RecipeMapKind.GoodBin : RecipeMapKind.Input;
            string rolePath = output ? saved.GoodMapPath : saved.InputMapPath;
            VerifyGeometry(DieMapGenerator.LoadJson(rolePath), draft);
            VerifyGeometry(DieMapGenerator.LoadCsv(Path.ChangeExtension(rolePath, ".csv")), draft);
            Assert(GeneratedWaferMapCodec.Restore(saved.BaseMap.Generation).RotationDegrees == angle, "quarter configuration reopens at its saved angle");
            string reason;
            TapeFrameSubset frame = output ? project.OutputFrame : project.InputFrame;
            Assert(RecipeDieMapResolver.IsCompatibleWithFrame(saved.RoleMap, frame, out reason), "quarter remains geometrically compatible for draft editing");
            Assert(!RecipeDieMapResolver.IsSupportedForEquipment(saved.RoleMap, out reason) && reason.Contains(angle + "°"), "quarter clearly reports current equipment unsupported");
            Assert(!RecipeDieMapResolver.IsMappedInputCompatibleWithRecipe(saved.RoleMap, saved.RoleMap, out reason), "quarter cannot reuse mapped runtime approval");
            Dictionary<string, byte[]> before = ReadFiles(saved.BaseMapPath, rolePath);
            int commits = 0;
            RecipeMapBuildResult apply = RecipeMapBuildService.SaveRoleTargetMask(project, kind, saved.RoleMap, p => { commits++; return true; });
            Assert(!apply.Success && commits == 0 && apply.Message.Contains("현재 장비"), "quarter FINAL APPLY blocked before persistence");
            AssertFiles(before, "quarter FINAL APPLY writes no files");
            if (output)
            {
                apply = RecipeMapBuildService.SaveRoleTargetMask(project, RecipeMapKind.NgBin, DieMapGenerator.LoadJson(saved.NgMapPath), p => { commits++; return true; });
                Assert(!apply.Success && commits == 0, "quarter NG FINAL APPLY also blocked");
            }

            // 이전 실행본/수동 편집으로 hash가 있어도 장비 지원 검사는 승인 버전보다 먼저 적용된다.
            string forcedHash = RecipeMapPaths.ComputeApprovalHash(saved.RoleMap);
            if (output) project.GoodBinMapApprovalHash = forcedHash;
            else project.InputMapApprovalHash = forcedHash;
            foreach (int version in new[] { 1, 0 })
            {
                project.MapApprovalVersion = version;
                Assert(!RecipeMapPaths.IsMapApproved(project, kind, saved.RoleMap, out reason), "quarter unsupported even with matching hash or legacy v0");
                string sourcePath;
                Assert(RecipeDieMapResolver.LoadCompatibleMap(project, kind, out sourcePath, out reason) == null && reason.Contains("현재 장비"), "quarter runtime configured load blocked");
            }
            bool approvalRejected = false;
            try { RecipeMapPaths.ApproveMap(project, kind, saved.RoleMap); }
            catch (InvalidOperationException ex) { approvalRejected = ex.Message.Contains("현재 장비"); }
            Assert(approvalRejected, "direct quarter approval API blocked");

            // JSON 파싱 실패 시 읽은 CSV와 legacy 외부 Base fallback도 사용을 허용하지 않는다.
            byte[] originalJson = File.ReadAllBytes(rolePath);
            File.WriteAllText(rolePath, "invalid json");
            project.MapApprovalVersion = 1;
            string resolvedPath;
            Assert(RecipeDieMapResolver.LoadCompatibleMap(project, kind, out resolvedPath, out reason) == null && reason.Contains("현재 장비"), "quarter CSV fallback runtime load blocked");
            File.WriteAllBytes(rolePath, originalJson);
            project.MapApprovalVersion = 0;
            RecipeMapPaths.SetConfiguredFileName(project, kind, "");
            if (output) project.OutputDieMapFileName = "";
            Assert(RecipeDieMapResolver.LoadCompatibleMap(project, kind, out resolvedPath, out reason) == null && reason.Contains("현재 장비"), "quarter external Base fallback runtime load blocked");
            AssertFiles(before, "quarter runtime checks leave all saved draft files unchanged");
        }
    }

    private static void VerifyRollbackAndMismatch(GeneratedWaferMap original)
    {
        RecipeProject project = NewProject("rollback");
        RecipeMapBuildResult initial = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, original, false, p => true);
        Assert(initial.Success, "rollback initial save");
        Dictionary<string, byte[]> before = ReadFiles(initial.BaseMapPath, initial.InputMapPath);
        int originalX = project.InputFrame.DieMapX;
        project.InputMapApprovalHash = "preserve-approval";
        GeneratedWaferMap rotated = WaferMapGeneration.Rotate(original, 90);
        RecipeMapBuildResult failed = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, rotated, false, p => false);
        Assert(!failed.Success && failed.PersistenceStarted, "commit callback failure is reported after persistence attempt");
        Assert(project.InputFrame.DieMapX == originalX && project.InputFrame.Rotate == "None" && project.InputMapApprovalHash == "preserve-approval", "failed save restores project fields");
        AssertFiles(before, "failed save restores JSON and CSV bytes");

        project.InputFrame.OuterDiameterMm += 0.001;
        RecipeMapBuildResult stale = RecipeMapBuildService.RebuildDerivedMaps(project, true, false, true, p => true);
        Assert(!stale.Success && stale.Message.Contains("미리보기"), "upper SAVE rejects diameter mismatch");
        AssertFiles(before, "diameter mismatch writes no maps");
        project.InputFrame.OuterDiameterMm = 287.4;
        project.InputFrame.PitchY += 0.001;
        stale = RecipeMapBuildService.RebuildDerivedMaps(project, true, false, true, p => true);
        Assert(!stale.Success, "upper SAVE rejects gap mismatch");
        AssertFiles(before, "gap mismatch writes no maps");
        project.InputFrame.PitchY = 0.3;
        project.InputFrame.Rotate = "Rotate180";
        stale = RecipeMapBuildService.RebuildDerivedMaps(project, true, false, true, p => true);
        Assert(!stale.Success, "upper SAVE rejects rotation mismatch");
        project.InputFrame.Rotate = "None";

        GeneratedWaferMap differentDie = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.400m, 10.371m, 7.913m, .3m, .3m));
        failed = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, differentDie, false, p => true);
        Assert(!failed.Success && !failed.PersistenceStarted && failed.Message.Contains("다이 사양"), "shared die mismatch fails before persistence");
        Assert(project.Die.WidthMm == 10.37, "preview never changes common die specification");
        AssertFiles(before, "invalid die writes no maps");

        DieMap wrongOrientation = GeneratedWaferMapCodec.ToDieMap(WaferMapGeneration.Rotate(original, 180), "wrong");
        failed = RecipeMapBuildService.SaveRoleTargetMask(project, RecipeMapKind.Input, wrongOrientation, p => true);
        Assert(!failed.Success, "same raw address set with wrong orientation rejected");
        AssertFiles(before, "wrong orientation cannot FINAL APPLY");

        RecipeProject empty = NewProject("new-family-failure");
        failed = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(empty, original, true, p => { throw new IOException("injected project failure"); });
        Assert(!failed.Success && failed.PersistenceStarted, "new family project exception reported");
        Assert(empty.OutputBaseWaferMapFileName == "" && empty.GoodBinDieMapFileName == "", "new family failure restores paths");
        string mapDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", empty.FileName, "Maps");
        Assert(!Directory.EnumerateFiles(mapDirectory).Any(), "new family rollback removes only newly created files");
    }

    private static void VerifyExternalLoad()
    {
        RecipeProject project = NewProject("external-load");
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external-place.txt");
        File.WriteAllText(path, "PLACE_WAFER_ROW\tPLACE_WAFER_COL\r\n1\t1\r\n1\t2\r\n2\t1\r\n2\t2\r\n3\t1\r\n3\t2\r\n");
        RecipeMapBuildResult imported = RecipeMapBuildService.ImportBaseAndBuildRole(project, path, false, p => true);
        Assert(imported.Success, "legacy PLACE TXT import: " + imported.Message);
        DieMap map = DieMapGenerator.LoadJson(imported.InputMapPath);
        Assert(map.Generation == null && map.SourceFormat == "PLACE GRID TXT" && map.Entries.Count == 6, "external parser contract retained");
        string reason;
        Assert(RecipeDieMapResolver.IsCompatibleWithFrame(map, project.InputFrame, out reason), "external role compatibility: " + reason);
        Assert(map.Entries.All(e => Math.Abs(e.PosY - DieMapGenerator.CalculateEquipmentGridY(e.DieMapY, map.DieMapY) * map.PitchY) < 1e-8), "external centered coordinate contract retained");
        Assert(project.OutputFrame.DieMapX == 9 && project.GoodBinMapApprovalHash == "old-good", "external input isolation retained");
    }

    private static RecipeProject NewProject(string name)
    {
        var project = new RecipeProject
        {
            FileName = name,
            Die = new DieSubset { WidthMm = 10.370, HeightMm = 7.913, ThicknessMm = .15 },
            InputFrame = new TapeFrameSubset { DieMapX = 9, DieMapY = 11, PitchX = .3, PitchY = .3, OuterDiameterMm = 200 },
            OutputFrame = new TapeFrameSubset { DieMapX = 9, DieMapY = 11, PitchX = .3, PitchY = .3, OuterDiameterMm = 200 },
            InputMapApprovalHash = "old-input", GoodBinMapApprovalHash = "old-good", NgBinMapApprovalHash = "old-ng", MapApprovalVersion = 1
        };
        RecipeProjectConsistencyService.SynchronizeDieSpecification(project);
        return project;
    }

    private static void VerifyLegacyApprovalMigration(GeneratedWaferMap generated)
    {
        string source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external-place.txt");
        RecipeProject project = CreateLegacyProject("legacy-input-save", source);
        string oldGoodPath;
        string reason;
        // 구형 공용 Output 경로가 GOOD과 NG에서 같은 파일로 해석되던 사례.
        project.OutputDieMapFileName = project.GoodBinDieMapFileName;
        project.GoodBinDieMapFileName = "";
        project.NgBinDieMapFileName = "";
        DieMap oldGood = RecipeDieMapResolver.LoadCompatibleMap(project, RecipeMapKind.GoodBin, out oldGoodPath, out reason);
        Assert(oldGood != null, "legacy GOOD fallback initially usable");
        Dictionary<string, byte[]> before = ReadFiles(oldGoodPath, RecipeMapPaths.ResolveBaseConfigured(project, RecipeMapKind.GoodBin));
        RecipeMapBuildResult saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, generated, false, p => true);
        Assert(saved.Success, "v0 input generation succeeds: " + saved.Message);
        foreach (RecipeMapKind kind in new[] { RecipeMapKind.GoodBin, RecipeMapKind.NgBin })
        {
            string path;
            DieMap map = RecipeDieMapResolver.LoadCompatibleMap(project, kind, out path, out reason);
            Assert(map != null && string.Equals(path, oldGoodPath, StringComparison.OrdinalIgnoreCase), "v1 retains exact old output resolved identity");
            Assert(RecipeMapPaths.IsMapApproved(project, kind, map, out reason), "v1 preserves old output approval");
        }
        Assert(project.InputMapApprovalHash == "", "new input remains pending during migration");
        AssertFiles(before, "legacy output files untouched by input migration");

        project = CreateLegacyProject("legacy-output-save", source);
        string oldInputPath = RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.Input);
        project.InputBaseWaferMapFileName = project.InputDieMapFileName;
        project.InputDieMapFileName = "";
        string resolvedInputPath;
        DieMap oldInput = RecipeDieMapResolver.LoadCompatibleMap(project, RecipeMapKind.Input, out resolvedInputPath, out reason);
        Assert(oldInput != null && oldInputPath == resolvedInputPath, "legacy Input external fallback initially usable");
        before = ReadFiles(oldInputPath);
        saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, generated, true, p => true);
        Assert(saved.Success, "v0 output generation succeeds: " + saved.Message);
        DieMap preservedInput = RecipeDieMapResolver.LoadCompatibleMap(project, RecipeMapKind.Input, out resolvedInputPath, out reason);
        Assert(preservedInput != null && resolvedInputPath == oldInputPath, "v1 retains exact old Input resolved identity");
        Assert(RecipeMapPaths.IsMapApproved(project, RecipeMapKind.Input, preservedInput, out reason), "v1 preserves old Input approval");
        Assert(project.GoodBinMapApprovalHash == "" && project.NgBinMapApprovalHash == "", "new output remains pending during migration");
        AssertFiles(before, "legacy input files untouched by output migration");

        project = CreateLegacyProject("legacy-rollback", source);
        project.OutputDieMapFileName = project.GoodBinDieMapFileName;
        project.GoodBinDieMapFileName = "";
        project.NgBinDieMapFileName = "";
        before = ReadFiles(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.GoodBin), RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.Input));
        saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, generated, false, p => false);
        Assert(!saved.Success && project.MapApprovalVersion == 0, "failed migration restores legacy approval version");
        Assert(project.GoodBinDieMapFileName == "" && project.NgBinDieMapFileName == "" && project.GoodBinMapApprovalHash == "", "failed migration restores implicit paths and hashes");
        AssertFiles(before, "failed legacy migration preserves all original map bytes");

        project = CreateLegacyProject("legacy-incompatible", source);
        project.OutputFrame.DieMapX += 1;
        before = ReadFiles(RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.GoodBin), RecipeMapPaths.ResolveConfigured(project, RecipeMapKind.NgBin));
        saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, generated, false, p => true);
        Assert(saved.Success, "new input still saves when old output is incompatible");
        Assert(project.GoodBinMapApprovalHash == "" && project.NgBinMapApprovalHash == "", "incompatible legacy maps are never implicitly approved");
        AssertFiles(before, "incompatible legacy files remain untouched");
    }

    private static RecipeProject CreateLegacyProject(string name, string source)
    {
        RecipeProject project = NewProject(name);
        Assert(RecipeMapBuildService.ImportBaseAndBuildRole(project, source, false, p => true).Success, "legacy fixture input");
        Assert(RecipeMapBuildService.ImportBaseAndBuildRole(project, source, true, p => true).Success, "legacy fixture output");
        project.MapApprovalVersion = 0;
        project.InputMapApprovalHash = "";
        project.GoodBinMapApprovalHash = "";
        project.NgBinMapApprovalHash = "";
        return project;
    }

    private static void VerifyFrameSpecNameIsolation(GeneratedWaferMap original)
    {
        GeneratedWaferMap rotated = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(
            new WaferMapGenerationSettings(287.400m, 10.370m, 7.913m, .400m, .500m)), 90);
        foreach (bool output in new[] { false, true })
        {
            RecipeProject project = NewProject("spec-collision-" + output);
            project.InputFrame.FrameSpecName = "SharedSpec";
            project.OutputFrame.FrameSpecName = "sharedspec";
            TapeFrameSubset other = output ? project.InputFrame : project.OutputFrame;
            string otherBefore = FrameSnapshot(other);
            RecipeMapBuildResult saved = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(project, rotated, output, p => true);
            Assert(saved.Success, "collision save: " + saved.Message);
            TapeFrameSubset selected = output ? project.OutputFrame : project.InputFrame;
            Assert(selected.FrameSpecName.EndsWith(output ? "_OUTPUT" : "_INPUT", StringComparison.Ordinal), "selected spec gets role suffix");
            Assert(!string.Equals(selected.FrameSpecName, other.FrameSpecName, StringComparison.OrdinalIgnoreCase), "physical spec keys are distinct");
            Assert(FrameSnapshot(other) == otherBefore, "name split preserves every unselected frame field");
            Assert(selected.Rotate == "CW90" && selected.PitchX == .4 && selected.PitchY == .5, "selected source gap and rotation retained");
            if (!output) Assert(FrameSnapshot(project.Frame) == FrameSnapshot(project.InputFrame), "legacy frame mirrors renamed Input");
        }

        RecipeProject aliased = NewProject("spec-shared-object");
        aliased.OutputFrame = aliased.InputFrame;
        string unselectedBefore = FrameSnapshot(aliased.OutputFrame);
        RecipeMapBuildResult result = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(aliased, original, false, p => true);
        Assert(result.Success, "shared object frame save");
        Assert(!ReferenceEquals(aliased.InputFrame, aliased.OutputFrame), "selected frame detaches legacy shared object");
        Assert(FrameSnapshot(aliased.OutputFrame) == unselectedBefore, "shared object unselected frame unchanged");

        RecipeProject failed = NewProject("spec-collision-rollback");
        string inputBefore = FrameSnapshot(failed.InputFrame);
        string outputBefore = FrameSnapshot(failed.OutputFrame);
        result = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(failed, rotated, true, p => false);
        Assert(!result.Success && result.PersistenceStarted, "spec rename transaction failure");
        Assert(FrameSnapshot(failed.InputFrame) == inputBefore && FrameSnapshot(failed.OutputFrame) == outputBefore, "failed save restores role names and all frame fields");
    }

    private static string FrameSnapshot(TapeFrameSubset frame)
    {
        using (var stream = new MemoryStream())
        {
            new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TapeFrameSubset)).WriteObject(stream, frame);
            return Convert.ToBase64String(stream.ToArray());
        }
    }

    private static void VerifyGeometry(DieMap map, GeneratedWaferMap expected)
    {
        string reason;
        Assert(GeneratedWaferMapCodec.ValidateForStorage(map, out reason), "persisted codec geometry: " + reason);
        Assert(GeneratedWaferMapCodec.Validate(map, out reason), "operational validation accepts saved geometry including boundary overflow: " + reason);
        Assert(map.Entries.Count == expected.Count, "actual die count");
        var byRaw = map.Entries.ToDictionary(e => e.OriginalMapX + "," + e.OriginalMapY);
        foreach (GeneratedWaferDie die in expected.Dies)
        {
            DieMapEntry entry = byRaw[die.RawColumn + "," + die.RawRow];
            Assert(Math.Abs(entry.PosX - (double)die.CenterXMm) < 1e-9 && Math.Abs(entry.PosY + (double)die.CenterYMm) < 1e-9, "exact original phase survives storage");
            Assert(entry.DieMapX == die.Column - expected.MinColumn && entry.DieMapY == expected.MaxRow - die.Row, "rotated local address survives storage");
        }
    }

    private static Dictionary<string, byte[]> ReadFiles(params string[] paths)
    {
        return paths.SelectMany(p => new[] { p, Path.ChangeExtension(p, ".csv") }).ToDictionary(p => p, File.ReadAllBytes);
    }

    private static void AssertFiles(Dictionary<string, byte[]> before, string message)
    {
        foreach (var pair in before) Assert(File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value), message);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _assertions++;
    }
}

// Hardware/store/log boundaries only. Geometry, parsing, serializers and service code above are real source files.
namespace QMC.CDT320.Materials
{
    [DataContract] public enum DieResult { [EnumMember] Unknown, [EnumMember] Good, [EnumMember] NG }
    public enum TapeFrameRotate { None, R90, R180, R270 }
    public sealed class DieTapeFrame
    {
        public int DieMapX, DieMapY;
        public string ObjId;
        public double PitchX, PitchY, OriginX, OriginY;
        public TapeFrameRotate Rotate;
    }
    public static class MaterialStateService { public static string NormalizeInputTapeFrameSpecName(string name) { return name; } }
}
namespace QMC.Common
{
    public static class Log { public static void Write(string group, string user, string action, string message) { Console.WriteLine(action + ": " + message); } }
}
namespace QMC.Common.Logging
{
    public enum EventKind { Warning, Event }
    public static class EventLogger { public static void Write(EventKind kind, string user, string action, string message) { Console.WriteLine(action + ": " + message); } }
}
namespace QMC.Common.Data.Store
{
    public static class RecipeDataStore
    {
        public static string DirOf(string name) { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", name); }
    }
}
