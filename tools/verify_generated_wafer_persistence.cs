// Standalone actual model/codec/serializer verification; only parser event logging is stubbed.
// No Handler assembly, settings, machine or operational directory is loaded.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;

internal static class VerifyGeneratedWaferMapPersistence
{
    private static int _assertions;

    private static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]).TrimEnd(Path.DirectorySeparatorChar);
            Assert(Path.GetFileName(root).StartsWith("verify-generated-wafer-persistence-", StringComparison.Ordinal) &&
                Path.GetFileName(Path.GetDirectoryName(root)) == "_build_check_handler", "Isolated verification directory");
            Assert(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) == root, "Standalone executable is isolated");
            var source = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.4m, 10.370m, 7.913m, .3m, .3m));
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                GeneratedWaferMap generated = WaferMapGeneration.Rotate(source, angle);
                VerifyRoundtrip(root, generated, "base-" + angle);
                WaferMapEdgeCounts e = generated.EdgeCounts;
                generated = WaferMapGeneration.ApplyEdgeCounts(generated, new WaferMapEdgeCounts(e.Top - 1, e.Bottom - 2, e.Left - 2, e.Right - 1));
                VerifyRoundtrip(root, generated, "trim-" + angle);
            }
            VerifyTampering(source);
            VerifyMalformedMetadata(root, source);
            VerifyLegacyAndRuntime(root);
            VerifyVersionTwo(root);
            VerifyVersionThree(root);
            VerifyVersionFour(root);
            Console.WriteLine("PASS: " + _assertions + " assertions. All rotations, odd-um phase, sparse JSON/CSV, edges, masks and runtime/legacy compatibility verified.");
            Console.WriteLine("LIMIT: Equipment, Handler runtime, Recipe transactions and UI are not executed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL after " + _assertions + " assertions: " + ex);
            return 1;
        }
    }

    private static void VerifyVersionThree(string root)
    {
        foreach (bool output in new[] { false, true })
        {
            decimal gap = output ? .300m : .050m;
            int total = output ? 1142 : 1257;
            var originalEdges = output ? new WaferMapEdgeCounts(2, 2, 7, 7) : new WaferMapEdgeCounts(3, 3, 9, 9);
            var basis = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 8.070m, 6.070m, gap, gap, .200m, 3));
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                bool quarter = angle == 90 || angle == 270;
                WaferMapEdgeCounts edges = quarter ? new WaferMapEdgeCounts(originalEdges.Left, originalEdges.Right, originalEdges.Top, originalEdges.Bottom) : originalEdges;
                var generated = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Rotate(basis, angle), edges, total);
                Assert(generated.Count == total && generated.Settings.GenerationVersion == 3, "V3 JMB target count/version");
                VerifyVersionThreeRoundtrip(root, generated, "jmb-" + output + "-" + angle);
            }
        }
        var source = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .100m, .100m, .200m, 3));
        var fallback = WaferMapGeneration.ApplyCounts(source, new WaferMapEdgeCounts(3, 3, 8, 8), 730);
        Assert(fallback.Count == 730, "V3 RKE manual fallback retains requested total");
        VerifyVersionThreeRoundtrip(root, fallback, "rke-730-fallback");
        foreach (string extension in new[] { ".json", ".csv" })
        {
            string path = Path.Combine(root, "v3-required" + extension);
            DieMap map = GeneratedWaferMapCodec.ToDieMapForStorage(fallback, "V3_REQUIRED");
            if (extension == ".json") DieMapGenerator.SaveJson(map, path); else DieMapGenerator.SaveCsv(map, path);
            string original = File.ReadAllText(path);
            foreach (string field in new[] { "EdgeMarginMm", "TotalCount", "Version" })
            {
                File.WriteAllText(path, original.Replace(field, "Missing" + field));
                Assert(DieMapGenerator.Load(path) == null, "V3 required field cannot be omitted: " + field + extension);
            }
        }
        foreach (Action<DieMap> corrupt in new Action<DieMap>[]
        {
            m => m.Generation.Version = 5, m => m.Generation.Version = 1,
            m => m.Generation.EdgeMarginMm = null, m => m.Generation.TotalCount = null,
            m => m.Entries[0].PosX += .001, m => m.Entries[0].OriginalMapX++,
            m => m.Generation.RotationDegrees = 45, m => m.SourceFormat = "CSV"
        })
        {
            DieMap map = GeneratedWaferMapCodec.ToDieMapForStorage(fallback, "V3_INVALID");
            corrupt(map);
            string reason;
            Assert(!GeneratedWaferMapCodec.ValidateForStorage(map, out reason), "V3 metadata/coordinate tampering remains rejected");
        }
    }

    private static void VerifyVersionThreeRoundtrip(string root, GeneratedWaferMap generated, string name)
    {
        int version = generated.Settings.GenerationVersion;
        string tag = "V" + version;
        DieMap map = GeneratedWaferMapCodec.ToDieMapForStorage(generated, tag + "_" + name);
        GeneratedWaferMapDefinition clone = GeneratedWaferMapCodec.CloneDefinition(map.Generation);
        Assert(clone.Version == version && clone.EdgeMarginMm == generated.Settings.EdgeMarginMm && clone.TotalCount == generated.Count, tag + " definition clone preserves version and count settings");
        Assert(map.SourceFormat == GeneratedWaferMapCodec.SourceFormat, tag + " preserves the established wire format token");
        string expected = Fingerprint(map);
        foreach (string extension in new[] { ".json", ".csv" })
        {
            string path = Path.Combine(root, name + extension);
            if (extension == ".json") DieMapGenerator.SaveJson(map, path); else DieMapGenerator.SaveCsv(map, path);
            DieMap loaded = DieMapGenerator.Load(path);
            string reason;
            Assert(loaded != null && GeneratedWaferMapCodec.ValidateForStorage(loaded, out reason), tag + " JSON/CSV draft validates");
            Assert(GeneratedWaferMapCodec.Validate(loaded, out reason), tag + " stored boundary overflow no longer rejects an unchanged generated map");
            Assert(Fingerprint(loaded) == expected && loaded.Generation.Version == version, tag + " JSON/CSV preserves all coordinates and version");
            GeneratedWaferMap restored = GeneratedWaferMapCodec.Restore(loaded.Generation);
            Assert(restored.Count == generated.Count && restored.OutOfBoundsCount == generated.OutOfBoundsCount &&
                restored.RotationDegrees == generated.RotationDegrees, tag + " preview restore preserves all count/rotation conditions");
            Assert(Fingerprint(GeneratedWaferMapCodec.ToDieMapForStorage(restored, map.FrameObjId)) == expected, tag + " definition restore exactly reproduces every coordinate");
            VerifyExactGeneratedGeometry(restored, generated, tag + " decimal phase survives definition restore");
            loaded.Entries[0].PosX += .001;
            Assert(!GeneratedWaferMapCodec.Validate(loaded, out reason), tag + " coordinate tampering still rejects after a valid reload, including boundary-overflow JMB maps");
        }
    }

    private static void VerifyVersionFour(string root)
    {
        foreach (decimal margin in new[] { 0m, .2m })
        {
            var basis = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .300m, .300m, margin, 4));
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                bool quarter = angle == 90 || angle == 270;
                var edges = quarter ? new WaferMapEdgeCounts(8, 8, 6, 6) : new WaferMapEdgeCounts(6, 6, 8, 8);
                var generated = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Rotate(basis, angle), edges, 684);
                Assert(generated.Count == 684 && generated.OutOfBoundsCount == 0, "V4 RKE phase correction fits all 684 dies inside the wafer");
                Assert(generated.UsedColumns == (quarter ? 34 : 26) && generated.UsedRows == (quarter ? 26 : 34), "V4 RKE phase correction retains exact rotated 26x34 bounds");
                Assert(generated.Dies.Any(d => d.CenterXMm * 1000m % 1m != 0m || d.CenterYMm * 1000m % 1m != 0m), "V4 fixture exercises half-micrometre phase precision");
                string name = "v4-rke-" + margin.ToString(CultureInfo.InvariantCulture) + "-" + angle;
                VerifyVersionThreeRoundtrip(root, generated, name);
                VerifyExactGeneratedGeometry(generated.BaseMap, WaferMapGeneration.Rotate(basis, angle), "V4 adjusted map retains canonical rotated AUTO base");
                VerifyExactGeneratedGeometry(generated.SourceBaseMap, basis, "V4 adjusted map retains canonical original AUTO base");
                int nextAngle = (angle + 90) % 360;
                var reset = WaferMapGeneration.Rotate(generated, nextAngle);
                VerifyExactGeneratedGeometry(reset, WaferMapGeneration.Rotate(basis, nextAngle), "V4 Rotate adjusted map resets phase and count edits to default AUTO");
                VerifyVersionThreeRoundtrip(root, reset, name + "-reset");
                foreach (Action<DieMap> corrupt in new Action<DieMap>[]
                {
                    m => m.Generation.Version = 3, m => m.Generation.Version = 5,
                    m => m.Generation.EdgeMarginMm = null, m => m.Generation.TotalCount = null,
                    m => m.Entries[0].PosX += .0005, m => m.Entries[0].OriginalMapY++,
                    m => m.Generation.RotationDegrees = 45
                })
                {
                    DieMap map = GeneratedWaferMapCodec.ToDieMap(generated, "V4_INVALID");
                    corrupt(map);
                    string reason;
                    Assert(!GeneratedWaferMapCodec.ValidateForStorage(map, out reason), "V4 metadata/address/sub-um tampering cannot pass storage validation");
                }
            }
        }
        foreach (bool output in new[] { false, true })
        {
            decimal gap = output ? .300m : .050m;
            int total = output ? 1142 : 1257;
            int horizontal = output ? 2 : 3;
            int vertical = output ? 7 : 9;
            var basis = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 8.070m, 6.070m, gap, gap, .200m, 4));
            var legacy = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 8.070m, 6.070m, gap, gap, .200m, 3));
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                bool quarter = angle == 90 || angle == 270;
                var edges = quarter ? new WaferMapEdgeCounts(vertical, vertical, horizontal, horizontal) : new WaferMapEdgeCounts(horizontal, horizontal, vertical, vertical);
                var generated = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Rotate(basis, angle), edges, total);
                VerifyExactGeneratedGeometry(generated, WaferMapGeneration.ApplyCounts(WaferMapGeneration.Rotate(legacy, angle), edges, total), "V4 default-first phase selection preserves the existing JMB V3 geometry");
                Assert(generated.OutOfBoundsCount == (output ? 4 : 38), "V4 JMB overflow remains a saved draft rather than clipped cells");
                VerifyVersionThreeRoundtrip(root, generated, "v4-jmb-" + output + "-" + angle);
            }
        }
        var source = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .100m, .100m, .200m, 4));
        var fallback = WaferMapGeneration.ApplyCounts(source, new WaferMapEdgeCounts(3, 3, 8, 8), 730);
        VerifyVersionThreeRoundtrip(root, fallback, "v4-rke-730");
        foreach (string extension in new[] { ".json", ".csv" })
        {
            string path = Path.Combine(root, "v4-required" + extension);
            DieMap map = GeneratedWaferMapCodec.ToDieMapForStorage(fallback, "V4_REQUIRED");
            if (extension == ".json") DieMapGenerator.SaveJson(map, path); else DieMapGenerator.SaveCsv(map, path);
            string content = File.ReadAllText(path);
            foreach (string field in new[] { "EdgeMarginMm", "TotalCount", "Version" })
            {
                File.WriteAllText(path, content.Replace(field, "Missing" + field));
                Assert(DieMapGenerator.Load(path) == null, "V4 cannot omit required metadata: " + field + extension);
            }
        }
    }

    private static void VerifyExactGeneratedGeometry(GeneratedWaferMap actual, GeneratedWaferMap expected, string message)
    {
        Assert(actual.Count == expected.Count && actual.UsedColumns == expected.UsedColumns && actual.UsedRows == expected.UsedRows, message + " bounds/count");
        var expectedDies = expected.Dies.ToDictionary(d => d.RawColumn + "," + d.RawRow);
        foreach (GeneratedWaferDie die in actual.Dies)
        {
            GeneratedWaferDie reference;
            Assert(expectedDies.TryGetValue(die.RawColumn + "," + die.RawRow, out reference), message + " raw address");
            Assert(die.Column == reference.Column && die.Row == reference.Row && die.CenterXMm == reference.CenterXMm && die.CenterYMm == reference.CenterYMm, message + " exact local and decimal physical coordinates");
        }
    }

    private static void VerifyVersionTwo(string root)
    {
        var legacy = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.4m, 10.370m, 7.913m, .3m, .3m));
        GeneratedWaferMapDefinition v1 = GeneratedWaferMapCodec.CreateDefinition(legacy);
        string oldDefinitionJson = GeneratedWaferMapCodec.SerializeDefinition(v1);
        Assert(v1.Version == 1 && !v1.EdgeMarginMm.HasValue && !v1.TotalCount.HasValue, "V1 definition does not adopt new generation conditions");
        Assert(!oldDefinitionJson.Contains("EdgeMarginMm") && !oldDefinitionJson.Contains("TotalCount"), "V1 JSON/CSV metadata keeps its original field contract");
        Assert(Fingerprint(GeneratedWaferMapCodec.ToDieMap(GeneratedWaferMapCodec.Restore(v1), "V1")) ==
            Fingerprint(GeneratedWaferMapCodec.ToDieMap(legacy, "V1")), "V1 geometry remains bit-identical");
        foreach (decimal margin in new[] { 0m, .2m, .3m })
        {
            var source = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.4m, 10.370m, 7.913m, .3m, .3m, margin));
            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                GeneratedWaferMap rotated = WaferMapGeneration.Rotate(source, angle);
                Assert(rotated.OutOfBoundsCount == 0, "V2 unadjusted map fits saved boundary");
                VerifyRoundtrip(root, rotated, "v2-base-" + margin.ToString(CultureInfo.InvariantCulture) + "-" + angle);
                var reduced = WaferMapGeneration.ApplyCounts(rotated, rotated.EdgeCounts, rotated.Count - 3);
                Assert(reduced.Count == rotated.Count - 3 && reduced.OutOfBoundsCount == 0, "V2 corrected total remains within circle");
                VerifyRoundtrip(root, reduced, "v2-total-" + margin.ToString(CultureInfo.InvariantCulture) + "-" + angle);
                GeneratedWaferMapDefinition definition = GeneratedWaferMapCodec.CreateDefinition(reduced);
                GeneratedWaferMapDefinition clone = GeneratedWaferMapCodec.CloneDefinition(definition);
                Assert(clone.Version == 2 && clone.EdgeMarginMm == margin && clone.TotalCount == reduced.Count, "V2 clone preserves margin and total");
                clone.EdgeMarginMm += .001m;
                Assert(definition.EdgeMarginMm == margin, "V2 cloned conditions are independent");
            }
        }

        var original = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.4m, 10.370m, 7.913m, .3m, .3m, .2m));
        var outside = WaferMapGeneration.ApplyCounts(original, original.EdgeCounts, original.Count + 20);
        Assert(outside.OutOfBoundsCount > 0, "V2 extended total remains available as an out-of-bound preview");
        GeneratedWaferMap restoredOutside = GeneratedWaferMapCodec.Restore(GeneratedWaferMapCodec.CreateDefinition(outside));
        Assert(restoredOutside.Count == outside.Count && restoredOutside.OutOfBoundsCount == outside.OutOfBoundsCount, "V2 unsafe preview definition restores without clipping");
        Assert(GeneratedWaferMapCodec.ToDieMap(restoredOutside, "OUTSIDE").Entries.Count == outside.Count,
            "operational conversion preserves every generated die including boundary overflow");
        VerifyOverflowStorage(root, restoredOutside, "overflow-default");

        var empty = WaferMapGeneration.Generate(new WaferMapGenerationSettings(5m, 6m, 6m, .2m, .2m, .2m));
        Assert(empty.Count == 0, "V2 small-wafer fixture has no original die");
        var expandedEmpty = WaferMapGeneration.ApplyCounts(empty, new WaferMapEdgeCounts(1, 1, 1, 1), 1);
        var restoredEmpty = GeneratedWaferMapCodec.Restore(GeneratedWaferMapCodec.CreateDefinition(expandedEmpty));
        Assert(restoredEmpty.Count == 1 && restoredEmpty.OutOfBoundsCount == 1, "V2 empty-base expansion can restore its unsafe preview");
        Assert(GeneratedWaferMapCodec.ToDieMap(restoredEmpty, "EMPTY_BASE").Entries.Count == 1,
            "explicitly generated empty-base expansion is not rejected only for boundary overflow");
        VerifyOverflowStorage(root, restoredEmpty, "overflow-empty-base");

        var basis730 = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .100m, .100m, .200m));
        var draft730 = WaferMapGeneration.ApplyCounts(basis730, new WaferMapEdgeCounts(3, 3, 8, 8), 730);
        Assert(draft730.Count == 730 && draft730.OutOfBoundsCount == 21, "RKE 730 fixture includes exactly 21 boundary-overflow dies");
        VerifyOverflowStorage(root, draft730, "overflow-730");

        foreach (string extension in new[] { ".json", ".csv" })
        {
            string path = Path.Combine(root, "v2-required" + extension);
            DieMap map = GeneratedWaferMapCodec.ToDieMap(original, "V2_REQUIRED");
            if (extension == ".json") DieMapGenerator.SaveJson(map, path); else DieMapGenerator.SaveCsv(map, path);
            string content = File.ReadAllText(path);
            foreach (string field in new[] { "EdgeMarginMm", "TotalCount" })
            {
                File.WriteAllText(path, content.Replace(field, "Missing" + field));
                Assert(DieMapGenerator.Load(path) == null, "V2 missing " + field + " is rejected during " + extension + " load");
            }
            File.WriteAllText(path, content);
            DieMap loaded = DieMapGenerator.Load(path);
            Assert(loaded != null && loaded.Generation.Version == 2, "V2 complete definition restores after malformed-field checks");
        }
        foreach (Action<GeneratedWaferMapDefinition> corrupt in new Action<GeneratedWaferMapDefinition>[]
        {
            d => d.EdgeMarginMm = null, d => d.TotalCount = null, d => d.EdgeMarginMm = -.001m,
            d => d.TotalCount = -1, d => d.Version = 1
        })
        {
            DieMap map = GeneratedWaferMapCodec.ToDieMap(original, "INVALID_V2");
            corrupt(map.Generation);
            string reason;
            Assert(!GeneratedWaferMapCodec.Validate(map, out reason), "V2 missing/invalid/downgraded definition cannot pass validation");
        }
    }

    private static void VerifyOverflowStorage(string root, GeneratedWaferMap generated, string name)
    {
        DieMap map = GeneratedWaferMapCodec.ToDieMapForStorage(generated, "DRAFT_" + name);
        string reason;
        Assert(GeneratedWaferMapCodec.ValidateForStorage(map, out reason), "explicit draft adapter validates complete geometry: " + reason);
        Assert(GeneratedWaferMapCodec.Validate(map, out reason), "complete generated geometry remains valid despite boundary overflow");
        Assert(map.Entries.Count == generated.Count && map.Entries.All(e => e.IsTarget), "draft preserves every die without outside-to-Skip conversion");
        foreach (string extension in new[] { ".json", ".csv" })
        {
            string path = Path.Combine(root, name + extension);
            if (extension == ".json") DieMapGenerator.SaveJson(map, path); else DieMapGenerator.SaveCsv(map, path);
            DieMap loaded = DieMapGenerator.Load(path);
            Assert(loaded != null && GeneratedWaferMapCodec.ValidateForStorage(loaded, out reason), "outside draft reload retains valid storage geometry " + extension);
            Assert(GeneratedWaferMapCodec.Validate(loaded, out reason), "reloaded boundary-overflow map passes the same geometry validation");
            Assert(Fingerprint(loaded) == Fingerprint(map), "every outside raw/local/physical coordinate survives " + extension);
            GeneratedWaferMap restored = GeneratedWaferMapCodec.Restore(loaded.Generation);
            Assert(restored.Count == generated.Count && restored.OutOfBoundsCount == generated.OutOfBoundsCount, "draft preview reopens with identical die/overflow counts");
        }
        foreach (Action<DieMap> corrupt in new Action<DieMap>[]
        {
            m => m.Entries[0].PosX += .001, m => m.Entries[0].OriginalMapX++,
            m => m.Entries[0].DieMapY++, m => m.Entries.RemoveAt(0), m => m.Generation = null,
            m => m.Generation.EdgeMarginMm = null, m => m.Generation.TotalCount = null
        })
        {
            DieMap changed = GeneratedWaferMapCodec.ToDieMapForStorage(generated, "DRAFT_INVALID");
            corrupt(changed);
            Assert(!GeneratedWaferMapCodec.ValidateForStorage(changed, out reason), "draft storage permission never relaxes structural or metadata validation");
            Assert(!GeneratedWaferMapCodec.Validate(changed, out reason), "boundary overflow permission never relaxes runtime geometry or metadata validation");
        }
    }

    private static void VerifyRoundtrip(string root, GeneratedWaferMap generated, string name)
    {
        DieMap map = GeneratedWaferMapCodec.ToDieMap(generated, "RKE_" + name);
        string reason;
        Assert(GeneratedWaferMapCodec.Validate(map, out reason), "New map validates: " + reason);
        Assert(map.Entries.Count == generated.Count && map.Entries.Count < map.TotalCells, "Sparse circle stores only accepted dies");
        var byRaw = generated.Dies.ToDictionary(d => d.RawColumn + "," + d.RawRow);
        foreach (DieMapEntry entry in map.Entries)
        {
            GeneratedWaferDie die = byRaw[entry.OriginalMapX + "," + entry.OriginalMapY];
            Assert(Same(entry.PosX, (double)die.CenterXMm) && Same(entry.PosY, (double)-die.CenterYMm), "Adapter keeps X and converts +Y up to equipment +Y down");
            Assert(Same(entry.PosX, map.OriginX + entry.DieMapX * map.PitchX) && Same(entry.PosY, map.OriginY + entry.DieMapY * map.PitchY), "Every position lies on the saved physical lattice");
        }
        Assert(Math.Abs(map.OriginX + (map.DieMapX - 1) / 2.0 * map.PitchX) > .0001 ||
            Math.Abs(map.OriginY + (map.DieMapY - 1) / 2.0 * map.PitchY) > .0001, "Odd-um phase is not recentered to bounding box");
        map.Entries[0].IsTarget = false;
        map.Entries[0].Result = DieResult.NG;
        map.Entries[0].BinCode = 255;
        map.Entries[1].SequenceNo = 3;
        Assert(GeneratedWaferMapCodec.Validate(map, out reason), "Target mask and pickup order can differ from base");
        foreach (string extension in new[] { ".json", ".csv" })
        {
            string path = Path.Combine(root, name + extension);
            if (extension == ".json") DieMapGenerator.SaveJson(map, path); else DieMapGenerator.SaveCsv(map, path);
            DieMap loaded = DieMapGenerator.Load(path);
            Assert(loaded != null && GeneratedWaferMapCodec.Validate(loaded, out reason), extension + " roundtrip validates: " + reason);
            Assert(loaded.Generation.RotationDegrees == generated.RotationDegrees && loaded.Generation.OuterDiameterMm == 287.4m, "Original inputs and angle survive");
            Assert(loaded.Generation.Version == generated.Settings.GenerationVersion &&
                (loaded.Generation.Version == 1 || (loaded.Generation.EdgeMarginMm == generated.Settings.EdgeMarginMm &&
                    loaded.Generation.TotalCount == (generated.RequestedTotalCount ?? generated.Count))), "Version-specific margin and total survive");
            Assert(!loaded.Entries[0].IsTarget && loaded.Entries[1].SequenceNo == 3, "Mask and order survive");
            Assert(Fingerprint(loaded) == Fingerprint(map), "Every raw/local/physical coordinate survives " + extension);
            GeneratedWaferMap restored = GeneratedWaferMapCodec.Restore(loaded.Generation);
            Assert(restored.Count == generated.Count && restored.RotationDegrees == generated.RotationDegrees, "Restore keeps final count and angle");
            Assert(restored.EdgeCounts.Top == generated.EdgeCounts.Top && restored.EdgeCounts.Bottom == generated.EdgeCounts.Bottom &&
                restored.EdgeCounts.Left == generated.EdgeCounts.Left && restored.EdgeCounts.Right == generated.EdgeCounts.Right, "Four displayed edge counts restore");
            Assert(restored.Dies.All(d => byRaw.ContainsKey(d.RawColumn + "," + d.RawRow) &&
                byRaw[d.RawColumn + "," + d.RawRow].CenterXMm == d.CenterXMm && byRaw[d.RawColumn + "," + d.RawRow].CenterYMm == d.CenterYMm), "Restored decimal geometry is exact");
        }
    }

    private static void VerifyTampering(GeneratedWaferMap source)
    {
        Action<DieMap>[] corruptions =
        {
            m => m.Generation = null, m => m.SourceFormat = "CSV", m => m.Generation.Version = 2,
            m => m.Generation.RotationDegrees = 45, m => m.Generation.EdgeTop = 0,
            m => m.Generation.OuterDiameterMm += .0001m, m => m.Generation.GapXMm += .1m,
            m => m.PitchX += .00001, m => m.DieSizeY += .00001, m => m.OuterDiameterMm += .001,
            m => m.OriginY += .0005, m => m.DieMapY++, m => m.Entries.RemoveAt(0),
            m => m.Entries[0] = null, m => m.Entries[0].OriginalMapX++, m => m.Entries[0].DieMapY++,
            m => m.Entries[0].PosY += .0005, m => m.Entries[0].PosX = double.NaN,
            m => m.Entries[0].EquipmentGridY = double.NaN, m => m.Entries[0].Index = 999,
            m => m.Entries[0] = m.Entries[1], m => m.SourceDeclaredCount++
        };
        foreach (Action<DieMap> corrupt in corruptions)
        {
            DieMap map = GeneratedWaferMapCodec.ToDieMap(source, "RKE");
            corrupt(map);
            string reason;
            Assert(!GeneratedWaferMapCodec.Validate(map, out reason) && !string.IsNullOrWhiteSpace(reason), "Tampered geometry/definition fails explicit recipe validation");
            Assert(!GeneratedWaferMapCodec.ValidateForStorage(map, out reason), "storage draft validation also rejects tampered geometry/definition");
        }
        GeneratedWaferMapDefinition definition = GeneratedWaferMapCodec.CreateDefinition(source);
        GeneratedWaferMapDefinition cloned = GeneratedWaferMapCodec.CloneDefinition(definition);
        cloned.RotationDegrees = 270;
        Assert(definition.RotationDegrees == 0, "Definition clone is independent");
        Assert(GeneratedWaferMapCodec.CloneDefinition(null) == null, "Legacy definition remains absent");
    }

    private static void VerifyMalformedMetadata(string root, GeneratedWaferMap source)
    {
        DieMap map = GeneratedWaferMapCodec.ToDieMap(source, "RKE");
        string json = Path.Combine(root, "malformed.json");
        DieMapGenerator.SaveJson(map, json);
        string content = File.ReadAllText(json);
        Assert(content.Contains("\"Version\""), "JSON contains required version");
        File.WriteAllText(json, content.Replace("\"Version\"", "\"MissingVersion\""));
        Assert(DieMapGenerator.LoadJson(json) == null, "Missing generation version cannot silently default");
        string csv = Path.Combine(root, "malformed.csv");
        DieMapGenerator.SaveCsv(map, csv);
        string[] lines = File.ReadAllLines(csv);
        int record = Array.FindIndex(lines, l => l.StartsWith("Index,")) + 1;
        string[] fields = lines[record].Split(',');
        fields[9] = "invalid";
        lines[record] = string.Join(",", fields);
        File.WriteAllLines(csv, lines);
        Assert(DieMapGenerator.LoadCsv(csv) == null, "Malformed generated CSV coordinate is not replaced by zero");
    }

    private static void VerifyLegacyAndRuntime(string root)
    {
        var legacy = new DieMap
        {
            FrameObjId = "LEGACY", DieMapX = 3, DieMapY = 2, PitchX = 2, PitchY = 3,
            Entries = { new DieMapEntry { DieMapX = 1, DieMapY = 0, PosX = 18, PosY = 31 } }
        };
        string reason;
        DieMapGenerator.Normalize(legacy);
        Assert(GeneratedWaferMapCodec.Validate(legacy, out reason) && legacy.Generation == null, "Legacy maps remain outside generated validation contract");
        Assert(legacy.Entries[0].OriginalMapX == 1 && legacy.Entries[0].EquipmentGridX == 0, "Legacy normalization retains behavior");
        // InputStage copies SourceFormat but stores aligned absolute positions without Generation.
        legacy.SourceFormat = GeneratedWaferMapCodec.SourceFormat;
        legacy.OriginX = 400;
        legacy.OriginY = 900;
        Assert(!GeneratedWaferMapCodec.Validate(legacy, out reason), "Source-only runtime map cannot pass recipe definition validation");
        foreach (string extension in new[] { ".json", ".csv" })
        {
            string path = Path.Combine(root, "runtime" + extension);
            if (extension == ".json") DieMapGenerator.SaveJson(legacy, path); else DieMapGenerator.SaveCsv(legacy, path);
            DieMap loaded = DieMapGenerator.Load(path);
            Assert(loaded != null && loaded.Generation == null && loaded.OriginX == 400 && loaded.Entries[0].PosY == 31, "Shared serializer preserves source-only absolute runtime map " + extension);
        }
        string place = Path.Combine(root, "legacy-place.txt");
        File.WriteAllText(place, "PLACE_WAFER_ROW PLACE_WAFER_COL\n1 1\n1 2\n2 1\n");
        DieMap external = DieMapGenerator.LoadWaferMapTextOrThrow(place);
        Assert(external != null && external.Generation == null && external.Entries.Count == 3 && external.SourceFormat == "PLACE GRID TXT", "External LOAD parser and sparse addresses remain unchanged");
    }

    private static string Fingerprint(DieMap map)
    {
        return string.Join(";", map.Entries.Select(e => e.OriginalMapX + "," + e.OriginalMapY + ":" + e.DieMapX + "," + e.DieMapY + ":" +
            e.PosX.ToString("F9", CultureInfo.InvariantCulture) + "," + e.PosY.ToString("F9", CultureInfo.InvariantCulture)));
    }
    private static bool Same(double actual, double expected) => Math.Abs(actual - expected) < .00000001;
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _assertions++;
    }
}

namespace QMC.Common.Logging
{
    public enum EventKind { Warning, Event }
    public static class EventLogger
    {
        public static void Write(EventKind kind, string user, string operation, string message)
        {
            System.Console.WriteLine("PARSER: " + operation + " / " + message);
        }
    }
}
