// Standalone verification of the pure generation model; excluded from the Handler project.
// Original oracle: FormMain.cpp TFrmMain::CreatedBinMap, AUTO/non-Zigzag branch.
// No application settings, runtime machine, GUI, production writes, or Handler assembly.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QMC.CDT320.DieMaps;

internal static class VerifyWaferMapGeneration
{
    private static int _assertions;

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length < 1) throw new ArgumentException("A dedicated verification directory is required.");
            string root = Path.GetFullPath(args[0]).TrimEnd(Path.DirectorySeparatorChar);
            Assert(Path.GetFileName(root).StartsWith("verify-wafer-map-generation-", StringComparison.Ordinal) &&
                Path.GetFileName(Path.GetDirectoryName(root)) == "_build_check_handler", "Dedicated isolated verification directory");
            Assert(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) == root, "Only the isolated standalone executable is running");
            VerifyOriginalGeometry();
            VerifyCaptured684(args.Length > 1 ? args[1] : "");
            VerifyInvalidAndEmpty();
            VerifyEdgeAdjustment();
            VerifyEdgeOverlapAndRange();
            VerifyRotation();
            VerifyInwardGeneration();
            VerifyCountReconstruction();
            VerifyRadialVersion3();
            VerifyPhaseVersion4();
            VerifyLegacySignedZero();
            Console.WriteLine("PASS: " + _assertions + " assertions. Legacy V1/V2/V3 geometry plus V4 RKE phase selection, JMB contours, exact physical coordinates, canonical replay and manual fallback verified.");
            Console.WriteLine("LIMIT: This standalone harness does not execute the Handler, UI, equipment or production storage.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL after " + _assertions + " assertions: " + ex);
            return 1;
        }
    }

    private static void VerifyInwardGeneration()
    {
        foreach (decimal diameter in new[] { 10.4m, 20.001m, 50m, 287.4m })
        foreach (decimal margin in new[] { 0m, .2m, .7m })
        {
            var settings = new WaferMapGenerationSettings(diameter, 2.301m, 1.701m, .117m, .013m, margin);
            GeneratedWaferMap map = WaferMapGeneration.Generate(settings);
            Assert(settings.GenerationVersion == 2 && settings.EdgeMarginMm == margin, "Six-argument settings select inward version 2");
            Assert(map.BoundaryRadiusMm == diameter / 2m - margin, "Margin is subtracted inside the physical wafer");
            Assert(map.OutOfBoundsCount == 0 && map.Dies.All(map.IsWithinBoundary), "Every default generated die is within its physical four-corner boundary");
            Assert(!map.RequestedTotalCount.HasValue && map.RequestedEdgeCounts == null, "Default map has no edited count request");
            long sx = ToUm(settings.CenterStepXMm), sy = ToUm(settings.CenterStepYMm);
            long firstX = -((long)map.CandidateColumns * sx / 2), firstY = -((long)map.CandidateRows * sy / 2);
            var expected = new HashSet<string>();
            for (int row = 0; row < map.CandidateRows; row++)
            for (int column = 0; column < map.CandidateColumns; column++)
            {
                decimal x = (firstX + column * sx) / 1000m, y = (firstY + row * sy) / 1000m;
                bool fits = true;
                foreach (int dx in new[] { -1, 1 })
                foreach (int dy in new[] { -1, 1 })
                {
                    decimal cornerX = x + dx * settings.DieSizeXMm / 2m;
                    decimal cornerY = y + dy * settings.DieSizeYMm / 2m;
                    if (cornerX * cornerX + cornerY * cornerY > map.BoundaryRadiusMm * map.BoundaryRadiusMm)
                        fits = false;
                }
                if (fits) expected.Add(Key(column, row));
            }
            Assert(Addresses(map).SetEquals(expected), "Independent enumeration of all four actual die corners matches every generated address");
            foreach (GeneratedWaferDie die in map.Dies)
                Assert(die.CenterXMm == (firstX + die.RawColumn * sx) / 1000m && die.CenterYMm == (firstY + die.RawRow * sy) / 1000m, "Version 2 preserves original integer lattice phase without recentering");
            if (map.Count > 0)
                Assert(map.RequiredOuterDiameterMm <= diameter, "Required diameter includes inward margin and rounds upward to 0.001 mm");
        }
        GeneratedWaferMap tangent = WaferMapGeneration.Generate(new WaferMapGenerationSettings(10.4m, 6m, 8m, 1m, 1m, .2m));
        Assert(tangent.Count == 1 && tangent.RequiredOuterDiameterMm == 10.4m, "Actual 6x8 corner on usable radius5 is accepted, independent of larger pitch7x9");
        Assert(WaferMapGeneration.Generate(new WaferMapGenerationSettings(10.399m, 6m, 8m, 1m, 1m, .2m)).Count == 0, "Shrinking diameter by 0.001 mm excludes the tangent physical die");
        var odd = WaferMapGeneration.Generate(new WaferMapGenerationSettings(.006m, .003m, .003m, .001m, .001m, 0m));
        Assert(odd.Count == 1, "Odd-micrometer die halves retain 0.5 micrometer precision");
        Rejected(() => WaferMapGeneration.Generate(new WaferMapGenerationSettings(10m, 1m, 1m, 0m, 0m, -1m)), "Negative inward margin rejected");
        Rejected(() => WaferMapGeneration.Generate(new WaferMapGenerationSettings(10m, 1m, 1m, 0m, 0m, 5.001m)), "Margin beyond radius rejected");
        Assert(Generate(287.4m, 10.370m, 7.913m, .3m, .3m).Settings.GenerationVersion == 1, "Five-argument constructor remains legacy version 1");
    }

    private static void VerifyCountReconstruction()
    {
        GeneratedWaferMap original = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.4m, 10.370m, 7.913m, .3m, .3m, .2m));
        Console.WriteLine("V2 fixture: count=" + original.Count + " used=" + original.UsedColumns + "x" + original.UsedRows + " edges=" + EdgeString(original.EdgeCounts));
        string before = Snapshot(original);
        Assert(ReferenceEquals(original, WaferMapGeneration.ApplyCounts(original, original.EdgeCounts, original.Count)), "Reapplying exact default counts preserves the original complete shape");
        foreach (int degrees in new[] { 0, 90, 180, 270 })
        {
            GeneratedWaferMap rotated = WaferMapGeneration.Rotate(original, degrees);
            foreach (int delta in new[] { -3, 20 })
            {
                GeneratedWaferMap adjusted = WaferMapGeneration.ApplyCounts(rotated, rotated.EdgeCounts, rotated.Count + delta);
                Assert(adjusted.Count == rotated.Count + delta && EdgeString(adjusted.EdgeCounts) == EdgeString(rotated.EdgeCounts), "Total count and all four displayed edges are fulfilled together");
                Assert(adjusted.OutOfBoundsCount == (delta < 0 ? 0 : 20), "Within-boundary cells are used first; only explicitly increased count remains outside");
                Assert(adjusted.RequestedTotalCount == adjusted.Count && EdgeString(adjusted.RequestedEdgeCounts) == EdgeString(rotated.EdgeCounts), "Count requests are retained for exact persistence reconstruction");
                AssertConnectedAndContinuous(adjusted);
                AssertOriginalLattice(adjusted);
                Assert(Snapshot(WaferMapGeneration.ApplyCounts(adjusted, rotated.EdgeCounts, adjusted.Count)) == Snapshot(adjusted), "Applying the same request is deterministic and not cumulative");
                Assert(ReferenceEquals(WaferMapGeneration.Rotate(adjusted, 0), original), "Absolute rotation resets edited counts to the original source");
            }
            WaferMapEdgeCounts enlargedEdges = new WaferMapEdgeCounts(rotated.EdgeCounts.Top + 2, rotated.EdgeCounts.Bottom + 3, rotated.EdgeCounts.Left + 2, rotated.EdgeCounts.Right + 3);
            GeneratedWaferMap edgeGrowth = WaferMapGeneration.ApplyCounts(rotated, enlargedEdges, rotated.Count + 40);
            Assert(EdgeString(edgeGrowth.EdgeCounts) == EdgeString(enlargedEdges) && edgeGrowth.Count == rotated.Count + 40, "All four edge counts can increase simultaneously");
            Assert(edgeGrowth.OutOfBoundsCount > 0 && edgeGrowth.RequiredOuterDiameterMm > edgeGrowth.Settings.OuterDiameterMm, "Out-of-bound increases remain visible and report required diameter");
            AssertConnectedAndContinuous(edgeGrowth);
            AssertOriginalLattice(edgeGrowth);
        }
        var largeEdges = new WaferMapEdgeCounts(original.UsedColumns + 2, original.UsedColumns + 2, 5, 5);
        GeneratedWaferMap widened = WaferMapGeneration.ApplyCounts(original, largeEdges, 1300);
        Assert(widened.UsedColumns > original.UsedColumns && widened.Count == 1300 && EdgeString(widened.EdgeCounts) == EdgeString(largeEdges), "Edges wider than original bounds expand the underlying lattice");
        AssertConnectedAndContinuous(widened);
        AssertOriginalLattice(widened);
        GeneratedWaferMap expanded = WaferMapGeneration.ApplyCounts(original, original.EdgeCounts, 1400);
        Assert(expanded.Count == 1400 && expanded.UsedColumns * expanded.UsedRows > original.UsedColumns * original.UsedRows, "Total request can expand bounds beyond original rectangular capacity");
        AssertConnectedAndContinuous(expanded);
        AssertOriginalLattice(expanded);
        GeneratedWaferMap empty = WaferMapGeneration.Generate(new WaferMapGenerationSettings(1m, 5m, 7m, .001m, .003m, .2m));
        Assert(empty.Count == 0, "Physical die larger than wafer produces an empty default map");
        GeneratedWaferMap outside = WaferMapGeneration.ApplyCounts(empty, new WaferMapEdgeCounts(1, 1, 1, 1), 1);
        Assert(outside.Count == 1 && outside.OutOfBoundsCount == 1 && outside.RequiredOuterDiameterMm > 1m, "Empty base still permits an explicitly oversized preview");
        AssertOriginalLattice(outside);
        GeneratedWaferMap emptyTwo = WaferMapGeneration.Generate(new WaferMapGenerationSettings(10m, 6m, 8m, 0m, 0m, 1m));
        Assert(emptyTwo.Count == 0 && emptyTwo.CandidateColumns == 2 && emptyTwo.CandidateRows == 2, "Empty two-by-two candidate fixture");
        GeneratedWaferMap singleFromTwo = WaferMapGeneration.ApplyCounts(emptyTwo, new WaferMapEdgeCounts(1, 1, 1, 1), 1);
        Assert(singleFromTwo.Count == 1 && singleFromTwo.Dies[0].CenterXMm == 0m && singleFromTwo.Dies[0].CenterYMm == 0m, "Empty candidate square starts from the physically nearest single cell");
        var rectangleEdges = new WaferMapEdgeCounts(original.UsedColumns, original.UsedColumns, original.UsedRows, original.UsedRows);
        int rectangleSize = original.UsedColumns * original.UsedRows;
        GeneratedWaferMap rectangle = WaferMapGeneration.ApplyCounts(original, rectangleEdges, rectangleSize);
        Assert(rectangle.Count == rectangleSize && rectangle.UsedColumns == original.UsedColumns && rectangle.UsedRows == original.UsedRows, "Full rectangle counts corners once and preserves fixed bounds");
        GeneratedWaferMap beyondRectangle = WaferMapGeneration.ApplyCounts(original, rectangleEdges, rectangleSize + 120);
        Assert(beyondRectangle.Count == rectangleSize + 120 && EdgeString(beyondRectangle.EdgeCounts) == EdgeString(rectangleEdges), "Capacity beyond a full rectangle expands until centered corner requests remain compatible");
        Rejected(() => WaferMapGeneration.ApplyCounts(original, rectangleEdges, rectangleSize + 1), "A nearly-full count conflicting with minimum continuous expanded edge support is diagnosed explicitly");
        AssertConnectedAndContinuous(beyondRectangle);
        AssertOriginalLattice(beyondRectangle);
        Rejected(() => WaferMapGeneration.ApplyCounts(original, original.EdgeCounts, 2), "Contradictory total and mandatory connected edges reject explicitly");
        Rejected(() => WaferMapGeneration.ApplyCounts(original, new WaferMapEdgeCounts(0, 1, 1, 1), 50), "A requested extreme edge cannot contain zero dies");
        Rejected(() => WaferMapGeneration.ApplyCounts(original, new WaferMapEdgeCounts(1000000, 1, 1, 1), 1000000), "Resource limit checked before expanded grid allocation");
        Rejected(() => WaferMapGeneration.ApplyCounts(original, original.EdgeCounts, 1000001), "Requested total above cap rejected");
        Rejected(() => WaferMapGeneration.ApplyCounts(Generate(20m, 2m, 2m), new WaferMapEdgeCounts(1, 1, 1, 1), 30), "Legacy map cannot silently switch generation formula through count editing");
        Rejected(() => WaferMapGeneration.ApplyEdgeCounts(original, original.EdgeCounts), "Version2 cannot bypass deterministic total-count reconstruction through legacy edge-only API");
        Assert(Snapshot(original) == before, "Successful and rejected edits leave original settings/addresses/phase unchanged");
    }

    private static void VerifyRadialVersion3()
    {
        // Frozen external coordinate profiles, read once during diagnosis; tests do not access
        // the machine's Config/Recipes or infer the expected profile using generation math.
        // YZ9XF22.11 SHA256 8E58D249115356E3B0A910D137DC85BEEA14795786185A210081038F52BC8EBB.
        // Input original X166..200/Y181..227 maps to candidate X1..35/Y1..47.
        int[] inputRows = { 3,11,15,17,19,21,23,25,27,27,29,29,31,31,31,33,33,33,33,35,35,35,35,35,35,35,35,35,33,33,33,33,31,31,31,29,29,27,27,25,23,21,19,17,15,11,3 };
        // PLACE_WAFER_1142.txt SHA256 8A47E069B617EBC95328BD95C467822A8146F9BD93494D7773275F9BE60677F0.
        // Output original X1..34/Y1..45 already matches candidate addresses.
        int[] outputRows = { 2,10,14,16,18,20,22,24,26,26,28,28,30,30,30,32,32,32,32,34,34,34,34,34,34,34,32,32,32,32,30,30,30,28,28,26,26,24,22,20,18,16,14,10,2 };
        VerifyRadialFixture(inputRows, 36, 47, .05m, new WaferMapEdgeCounts(3,3,9,9), 1257, 38, 290.314m, 35, 47, "JMB INPUT");
        VerifyRadialFixture(outputRows, 35, 45, .3m, new WaferMapEdgeCounts(2,2,7,7), 1142, 4, 288.110m, 34, 45, "JMB OUTPUT");

        // Frozen V2 edited shape intentionally remains narrower than the imported JMB input.
        // Version3 must not reinterpret already persisted V2 requests or their physical positions.
        int[] frozenV2Rows = { 3,15,17,21,23,23,25,27,29,29,31,31,31,33,33,33,33,33,35,35,35,35,35,35,35,35,35,33,33,33,33,33,31,31,31,29,29,27,25,25,23,21,17,15,3 };
        GeneratedWaferMap v2Input = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.64m,8.07m,6.07m,.05m,.05m,.2m));
        GeneratedWaferMap oldEdited = WaferMapGeneration.ApplyCounts(v2Input, new WaferMapEdgeCounts(3,3,9,9), 1257);
        Assert(Addresses(oldEdited).SetEquals(FixtureAddresses(frozenV2Rows,36,46)), "Version2 entire persisted edited input address set remains unchanged");
        Assert(oldEdited.UsedColumns == 35 && oldEdited.UsedRows == 45 && oldEdited.OutOfBoundsCount == 50 && oldEdited.RequiredOuterDiameterMm == 295.441m, "Version2 previous bounds/outside metric remains frozen");

        GeneratedWaferMap v2 = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.4m,10.37m,7.913m,.3m,.3m,.2m));
        GeneratedWaferMap v3 = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.4m,10.37m,7.913m,.3m,.3m,.2m,3));
        Assert(Snapshot(v2) == Snapshot(v3), "Version3 automatic inward boundary and original phase match Version2");
        var manualEdges = new WaferMapEdgeCounts(8,9,10,11);
        GeneratedWaferMap manualV2 = WaferMapGeneration.ApplyCounts(v2,manualEdges,724);
        GeneratedWaferMap manualV3 = WaferMapGeneration.ApplyCounts(v3,manualEdges,724);
        Assert(Snapshot(manualV2) == Snapshot(manualV3) && EdgeString(manualV3.EdgeCounts) == "8,9,10,11", "An explicit asymmetric edge request falls back to the unchanged continuous manual layout");
        AssertConnectedAndContinuous(manualV3);
        AssertOriginalLattice(manualV3);
        GeneratedWaferMap expandedV2 = WaferMapGeneration.ApplyCounts(v2,v2.EdgeCounts,1400);
        GeneratedWaferMap expandedV3 = WaferMapGeneration.ApplyCounts(v3,v3.EdgeCounts,1400);
        Assert(Snapshot(expandedV2) == Snapshot(expandedV3), "A count beyond the full candidate lattice retains existing expansion fallback");
        AssertConnectedAndContinuous(expandedV3);
        AssertOriginalLattice(expandedV3);
        VerifyRadialFixture(inputRows, 36, 47, .05m, new WaferMapEdgeCounts(3,3,9,9), 1257, 38, 290.314m, 35, 47, "JMB INPUT V4", 4);
        VerifyRadialFixture(outputRows, 35, 45, .3m, new WaferMapEdgeCounts(2,2,7,7), 1142, 4, 288.110m, 34, 45, "JMB OUTPUT V4", 4);
        foreach (int bad in new[] {-1,0,5,int.MaxValue})
            Rejected(() => new WaferMapGenerationSettings(20m,2m,2m,0m,0m,.2m,bad), "Unknown generation versions are rejected explicitly");
        Rejected(() => new WaferMapGenerationSettings(20m,2m,2m,0m,0m,.2m,1), "Version1 cannot silently ignore a supplied inward margin");
    }

    private static void VerifyRadialFixture(int[] rows, int doubleCenterColumn, int maxRawRow, decimal gap,
        WaferMapEdgeCounts edges, int total, int outside, decimal requiredDiameter, int width, int height, string label, int version = 3)
    {
        HashSet<string> expected = FixtureAddresses(rows,doubleCenterColumn,maxRawRow);
        GeneratedWaferMap original = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.64m,8.07m,6.07m,gap,gap,.2m,version));
        Assert(expected.Count == total, label + " captured profile has expected count");
        Assert(original.Settings.GenerationVersion == version && original.OutOfBoundsCount == 0, label + " explicit version has the same strict inward default");
        foreach (int degrees in new[] {0,90,180,270})
        {
            GeneratedWaferMap rotated = WaferMapGeneration.Rotate(original,degrees);
            WaferMapEdgeCounts rotatedEdges = degrees == 0 ? edges : degrees == 90 ? new WaferMapEdgeCounts(edges.Left,edges.Right,edges.Bottom,edges.Top) :
                degrees == 180 ? new WaferMapEdgeCounts(edges.Bottom,edges.Top,edges.Right,edges.Left) : new WaferMapEdgeCounts(edges.Right,edges.Left,edges.Top,edges.Bottom);
            GeneratedWaferMap map = WaferMapGeneration.ApplyCounts(rotated,rotatedEdges,total);
            bool swap = degrees == 90 || degrees == 270;
            Assert(Addresses(map).SetEquals(expected), label + " every original raw address matches captured external coordinates at " + degrees);
            Assert(map.Count == total && EdgeString(map.EdgeCounts) == EdgeString(rotatedEdges), label + " total and all four rotated edge counts match");
            Assert(map.UsedColumns == (swap ? height : width) && map.UsedRows == (swap ? width : height), label + " used grid is inferred from selected contour without manual GridX/Y");
            Assert(map.OutOfBoundsCount == outside && map.RequiredOuterDiameterMm == requiredDiameter, label + " outside count and physical required diameter remain explicit");
            Assert(map.RequestedTotalCount == total && EdgeString(map.RequestedEdgeCounts) == EdgeString(rotatedEdges), label + " version3 request is sufficient for persistence reconstruction");
            Assert(ReferenceEquals(map.SourceBaseMap,original), label + " original lattice source is retained");
            AssertConnectedAndContinuous(map);
            AssertOriginalLattice(map);
            Assert(Snapshot(map) == Snapshot(WaferMapGeneration.ApplyCounts(map,rotatedEdges,total)), label + " repeated version3 request is deterministic and not cumulative");
            Assert(ReferenceEquals(WaferMapGeneration.Rotate(map,0),original), label + " absolute rotation reset restores inward automatic base");
        }
        Console.WriteLine("V" + version + ": " + label + " count=" + total + " used=" + width + "x" + height + " edges=" + EdgeString(edges) + " outside=" + outside + " requiredD=" + requiredDiameter);
    }

    private static HashSet<string> FixtureAddresses(int[] rows, int doubleCenterColumn, int maxRawRow)
    {
        var points = new HashSet<string>();
        for (int i = 0; i < rows.Length; i++)
        {
            int firstColumn = (doubleCenterColumn - rows[i] + 1) / 2;
            for (int column = firstColumn; column < firstColumn + rows[i]; column++)
                points.Add(Key(column,maxRawRow - i));
        }
        return points;
    }

    private static void VerifyPhaseVersion4()
    {
        // Captured RKE_GoodBinDieMap / PLACE_WAFER_684 coordinate profile. X1..26/Y1..34,
        // physical X=(rawX-13.5)*10.670 and preview Y=(rawY-17.5)*8.213 mm.
        // Tests consume this frozen fixture only, with no Config/Recipes access or equipment.
        int[] rkeRows = { 6,10,12,16,18,18,20,22,22,22,24,24,24,26,26,26,26,26,26,26,26,24,24,24,22,22,22,20,18,18,16,12,10,6 };
        int[] frozenV3Rows = { 6,12,14,16,18,20,22,22,24,24,24,24,24,26,26,26,26,26,26,26,26,24,24,24,24,22,22,20,18,16,14,12,6 };
        HashSet<string> expected = FixtureAddresses(rkeRows,27,34);
        HashSet<string> oldExpected = FixtureAddresses(frozenV3Rows,27,34);
        Assert(expected.Count == 684 && oldExpected.Count == 684, "Captured RKE original and previous V3 profiles both contain 684 cells");
        var requested = new WaferMapEdgeCounts(6,6,8,8);
        foreach (decimal margin in new[] {0m,.2m})
        {
            GeneratedWaferMap source = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.64m,10.37m,7.913m,.3m,.3m,margin,4));
            GeneratedWaferMap previous = WaferMapGeneration.Generate(new WaferMapGenerationSettings(287.64m,10.37m,7.913m,.3m,.3m,margin,3));
            Assert(Snapshot(source) == Snapshot(previous), "Version4 AUTO retains the previous default candidate parity and inward circle");
            GeneratedWaferMap previousEdited = WaferMapGeneration.ApplyCounts(previous,requested,684);
            Assert(Addresses(previousEdited).SetEquals(oldExpected), "Every previous V3 RKE fallback address remains unchanged");
            Assert(previousEdited.UsedColumns == 26 && previousEdited.UsedRows == 33 && previousEdited.OutOfBoundsCount == 8,
                "Version3 RKE 26x33/outside8 is preserved for old saved maps");
            Assert(previousEdited.RequiredOuterDiameterMm == 291.263m + 2m * margin, "Previous V3 physical-coordinate metrics remain frozen");
            foreach (int degrees in new[] {0,90,180,270})
            {
                GeneratedWaferMap rotatedBase = WaferMapGeneration.Rotate(source,degrees);
                WaferMapEdgeCounts rotatedRequest = degrees == 90 || degrees == 270 ? new WaferMapEdgeCounts(8,8,6,6) : requested;
                GeneratedWaferMap result = WaferMapGeneration.ApplyCounts(rotatedBase,rotatedRequest,684);
                Assert(Addresses(result).SetEquals(expected), "Version4 selects all original RKE684 raw identities at every angle");
                Assert(result.Count == 684 && EdgeString(result.EdgeCounts) == EdgeString(rotatedRequest), "Version4 must satisfy total and all four edges before selecting a phase");
                bool swap = degrees == 90 || degrees == 270;
                Assert(result.UsedColumns == (swap ? 34 : 26) && result.UsedRows == (swap ? 26 : 34), "RKE used grid follows the chosen physical center phase");
                Assert(result.OutOfBoundsCount == 0 && result.RequiredOuterDiameterMm == 286.780m + 2m * margin,
                    "Original RKE map fits margin0 and margin0.2 without any red die");
                Assert(result.SourceOriginXMm == -144.045m && result.SourceOriginYMm == -143.7275m,
                    "Source candidate origin preserves the exact half-micrometer Y phase");
                Assert(result.SourceBaseMap.SourceOriginYMm == -147.834m && ReferenceEquals(result.SourceBaseMap,source) && ReferenceEquals(result.BaseMap,rotatedBase),
                    "Selected phase is not hidden in the canonical default AUTO BaseMap or SourceBaseMap");
                foreach (GeneratedWaferDie die in result.Dies)
                {
                    decimal x = (die.RawColumn - 13.5m) * 10.670m;
                    decimal y = (die.RawRow - 17.5m) * 8.213m;
                    decimal expectedX = degrees == 0 ? x : degrees == 90 ? y : degrees == 180 ? -x : -y;
                    decimal expectedY = degrees == 0 ? y : degrees == 90 ? -x : degrees == 180 ? -y : x;
                    Assert(die.CenterXMm == expectedX && die.CenterYMm == expectedY,
                        "Every physical RKE coordinate equals independently captured centered half-pitch positions");
                }
                AssertConnectedAndContinuous(result);
                Assert(Snapshot(result) == Snapshot(WaferMapGeneration.ApplyCounts(result,rotatedRequest,684)),
                    "Reapplying a selected phase starts from canonical AUTO and reproduces every address/position");
                AssertCanonicalReplay(result,"Selected phase result");
                AssertCanonicalReplay(result.BaseMap,"Selected result's default oriented BaseMap");
                AssertCanonicalReplay(result.SourceBaseMap,"Selected result's original AUTO source");
                foreach (int resetAngle in new[] {0,90,180,270})
                {
                    GeneratedWaferMap reset = WaferMapGeneration.Rotate(result,resetAngle);
                    Assert(Snapshot(reset) == Snapshot(WaferMapGeneration.Rotate(source,resetAngle)) && !reset.RequestedTotalCount.HasValue,
                        "All versions retain absolute Rotate-reset semantics including phase/count reset");
                    AssertCanonicalReplay(reset,"Rotation-reset AUTO map");
                }
            }
            // None of the four radial phases has these asymmetric edges at total684.
            // The old contiguous manual strategy must still handle that explicit user request.
            var manualEdges = new WaferMapEdgeCounts(5,6,7,8);
            GeneratedWaferMap manualV4 = WaferMapGeneration.ApplyCounts(source,manualEdges,684);
            GeneratedWaferMap manualV3 = WaferMapGeneration.ApplyCounts(previous,manualEdges,684);
            Assert(Snapshot(manualV4) == Snapshot(manualV3), "No matching phase falls back to unchanged manual layout on the default origin");
            Assert(manualV4.SourceOriginYMm == source.SourceOriginYMm, "An unmatched phase cannot leak its shifted origin into fallback");
            AssertConnectedAndContinuous(manualV4);
            AssertCanonicalReplay(manualV4,"No-match phase/manual fallback");
            Console.WriteLine("V4: RKE margin=" + margin + " count684 used26x34 edges6/6/8/8 outside0 requiredD=" + (286.780m + 2m * margin));
        }
    }

    private static void VerifyLegacySignedZero()
    {
        foreach (int version in new[] {1,2,3})
        {
            var settings = new WaferMapGenerationSettings(287.64m,8.07m,6.07m,.05m,.05m,version == 1 ? 0m : .2m,version);
            GeneratedWaferMap source = WaferMapGeneration.Generate(settings);
            foreach (int degrees in new[] {0,90,180,270})
            {
                GeneratedWaferMap map = WaferMapGeneration.Rotate(source,degrees);
                if (version >= 2)
                {
                    WaferMapEdgeCounts counts = degrees == 90 || degrees == 270 ? new WaferMapEdgeCounts(9,9,3,3) : new WaferMapEdgeCounts(3,3,9,9);
                    map = WaferMapGeneration.ApplyCounts(map,counts,1257);
                }
                int zeroCoordinates = 0;
                foreach (GeneratedWaferDie die in map.Dies)
                {
                    // Reproduce the original integer-before-division arithmetic independently.
                    long xUm = -((long)source.CandidateColumns * 8120L / 2L) + die.RawColumn * 8120L;
                    long yUm = -((long)source.CandidateRows * 6120L / 2L) + die.RawRow * 6120L;
                    decimal x = xUm / 1000m, y = yUm / 1000m;
                    decimal expectedX = degrees == 0 ? x : degrees == 90 ? y : degrees == 180 ? -x : -y;
                    decimal expectedY = degrees == 0 ? y : degrees == 90 ? -x : degrees == 180 ? -y : x;
                    Assert(BitConverter.DoubleToInt64Bits((double)die.CenterXMm) == BitConverter.DoubleToInt64Bits((double)expectedX),
                        "Legacy X coordinate preserves original double bits including serialized zero sign");
                    Assert(BitConverter.DoubleToInt64Bits((double)die.CenterYMm) == BitConverter.DoubleToInt64Bits((double)expectedY),
                        "Legacy Y coordinate preserves original double bits including serialized zero sign");
                    if (xUm == 0 || yUm == 0) zeroCoordinates++;
                }
                Assert(zeroCoordinates > 0, "Legacy signed-zero regression exercises axis-center cells at every angle");
            }
        }
    }

    private static void AssertCanonicalReplay(GeneratedWaferMap map, string label)
    {
        WaferMapGenerationSettings settings = map.Settings;
        var copy = new WaferMapGenerationSettings(settings.OuterDiameterMm,settings.DieSizeXMm,settings.DieSizeYMm,
            settings.GapXMm,settings.GapYMm,settings.EdgeMarginMm,settings.GenerationVersion);
        GeneratedWaferMap replayBase = WaferMapGeneration.Rotate(WaferMapGeneration.Generate(copy),map.RotationDegrees);
        GeneratedWaferMap replay = WaferMapGeneration.ApplyCounts(replayBase,map.RequestedEdgeCounts ?? map.EdgeCounts,
            map.RequestedTotalCount ?? map.Count);
        Assert(Snapshot(replay) == Snapshot(map), label + " is reproducible from settings/version/angle/edges/total without persisted hidden phase");
    }

    private static void AssertOriginalLattice(GeneratedWaferMap map)
    {
        GeneratedWaferMap source = map.SourceBaseMap;
        long sx = ToUm(source.Settings.CenterStepXMm), sy = ToUm(source.Settings.CenterStepYMm);
        long firstX = -((long)source.CandidateColumns * sx / 2), firstY = -((long)source.CandidateRows * sy / 2);
        foreach (GeneratedWaferDie die in map.Dies)
        {
            decimal x = (firstX + die.RawColumn * sx) / 1000m, y = (firstY + die.RawRow * sy) / 1000m;
            decimal expectedX = map.RotationDegrees == 0 ? x : map.RotationDegrees == 90 ? y : map.RotationDegrees == 180 ? -x : -y;
            decimal expectedY = map.RotationDegrees == 0 ? y : map.RotationDegrees == 90 ? -x : map.RotationDegrees == 180 ? -y : x;
            Assert(die.CenterXMm == expectedX && die.CenterYMm == expectedY, "Added/retained dies preserve original raw-address physical lattice even after rotation and expansion");
        }
    }

    private static void AssertConnectedAndContinuous(GeneratedWaferMap map)
    {
        foreach (var row in map.Dies.GroupBy(die => die.Row))
            Assert(row.Max(die => die.Column) - row.Min(die => die.Column) + 1 == row.Count(), "Every occupied row is continuous without holes");
        foreach (var column in map.Dies.GroupBy(die => die.Column))
            Assert(column.Max(die => die.Row) - column.Min(die => die.Row) + 1 == column.Count(), "Every occupied column is continuous without holes");
        var points = new HashSet<string>(map.Dies.Select(die => Key(die.Column, die.Row)));
        var visited = new HashSet<string>();
        var queue = new Queue<Tuple<int, int>>();
        queue.Enqueue(Tuple.Create(map.Dies[0].Column, map.Dies[0].Row));
        while (queue.Count > 0)
        {
            Tuple<int, int> point = queue.Dequeue();
            string key = Key(point.Item1, point.Item2);
            if (!points.Contains(key) || !visited.Add(key)) continue;
            queue.Enqueue(Tuple.Create(point.Item1 - 1, point.Item2));
            queue.Enqueue(Tuple.Create(point.Item1 + 1, point.Item2));
            queue.Enqueue(Tuple.Create(point.Item1, point.Item2 - 1));
            queue.Enqueue(Tuple.Create(point.Item1, point.Item2 + 1));
        }
        Assert(visited.Count == map.Count, "All dies form one orthogonally connected shape without isolated edge islands");
    }

    private static GeneratedWaferMap Generate(decimal diameter, decimal dieX, decimal dieY, decimal gapX = 0m, decimal gapY = 0m)
    {
        return WaferMapGeneration.Generate(new WaferMapGenerationSettings(diameter, dieX, dieY, gapX, gapY));
    }

    private static void VerifyOriginalGeometry()
    {
        CompareOriginal(Generate(287.64m, 10.370m, 7.913m, .3m, .3m), 287640, 10670, 8213, 682, "RKE D287.64");
        GeneratedWaferMap rke684 = Generate(287.4m, 10.370m, 7.913m, .3m, .3m);
        CompareOriginal(rke684, 287400, 10670, 8213, 684, "RKE D287.4");
        CompareOriginal(Generate(287m, 10.370m, 7.913m, .3m, .3m), 287000, 10670, 8213, 684, "RKE D287");
        CompareOriginal(Generate(287.64m, 8.07m, 6.07m, .3m, .3m), 287640, 8370, 6370, 1138, "RAD D287.64");
        CompareOriginal(Generate(300m, 8.12m, 6.12m, .05m, .05m), 300000, 8170, 6170, 1328, "300 mm example");
        Assert(rke684.Dies.Min(d => d.CenterYMm) == -135.514m && rke684.Dies.Max(d => d.CenterYMm) == 135.515m, "Odd 8,213 um pitch preserves half-product integer truncation, without recentering");
        foreach (decimal diameter in new[] { 11.2m, 20m, 20.001m, 25.3m })
            CompareOriginal(Generate(diameter, 2m, 2m), ToUm(diameter), 2000, 2000, null, "Zero gap and diameter parity");
        foreach (decimal diameter in new[] { 15m, 25.3m, 50m })
        foreach (decimal gap in new[] { 0m, .07m, .5m })
            CompareOriginal(Generate(diameter, 2.3m, 1.7m, gap, gap / 2), ToUm(diameter), 2300 + ToUm(gap), 1700 + ToUm(gap / 2), null, "Non-square diameter/gap sweep");
        CompareOriginal(Generate(20.001m, 2.001m, 1.701m), 20001, 2001, 1701, null, "Odd micrometer pitches");
        CompareOriginal(Generate(.3m, .1m, .1m), 300, 100, 100, 15, "Small-scale integer arithmetic");
        CompareOriginal(Generate(9.6m, 6m, 8m), 9600, 6000, 8000, 0, "Exact original boundary equality is excluded");
        CompareOriginal(Generate(9.602m, 6m, 8m), 9602, 6000, 8000, 1, "One micrometer radial increase admits tangent cell");
    }

    private static void VerifyRotation()
    {
        GeneratedWaferMap source = Generate(287.4m, 10.370m, 7.913m, .3m, .3m);
        var originals = source.Dies.ToDictionary(d => Key(d.RawColumn, d.RawRow));
        foreach (int degrees in new[] { 0, 90, 180, 270 })
        {
            GeneratedWaferMap rotated = WaferMapGeneration.Rotate(source, degrees);
            bool swap = degrees == 90 || degrees == 270;
            Assert(rotated.RotationDegrees == degrees && ReferenceEquals(rotated.SourceBaseMap, source), "Absolute rotation and original source identity");
            Assert(rotated.Count == 684 && Addresses(rotated).SetEquals(Addresses(source)), "Rotation preserves count and every original raw identity");
            Assert(rotated.UsedColumns == (swap ? 34 : 26) && rotated.UsedRows == (swap ? 26 : 34), "Non-square used grid rotates");
            Assert(rotated.DisplayDieSizeXMm == (swap ? 7.913m : 10.370m) && rotated.DisplayStepXMm == (swap ? 8.213m : 10.670m), "Rectangular die and physical pitch rotate together");
            Assert(rotated.Dies.Select(d => Key(d.Column, d.Row)).Distinct().Count() == rotated.Count, "Rotated local indices remain unique");
            foreach (GeneratedWaferDie die in rotated.Dies)
            {
                GeneratedWaferDie original = originals[Key(die.RawColumn, die.RawRow)];
                decimal x = degrees == 0 ? original.CenterXMm : degrees == 90 ? original.CenterYMm : degrees == 180 ? -original.CenterXMm : -original.CenterYMm;
                decimal y = degrees == 0 ? original.CenterYMm : degrees == 90 ? -original.CenterXMm : degrees == 180 ? -original.CenterYMm : original.CenterXMm;
                Assert(die.CenterXMm == x && die.CenterYMm == y, "Clockwise rotation preserves exact odd-micrometer phase");
            }
            Assert(ReferenceEquals(WaferMapGeneration.Rotate(rotated, 0), source), "Rotate back to zero restores original identity without drift");
            Assert(Snapshot(WaferMapGeneration.Rotate(rotated, degrees)) == Snapshot(rotated), "Selecting same absolute angle does not accumulate");
        }
        GeneratedWaferMap clockwise = WaferMapGeneration.Rotate(source, 90);
        Assert(EdgeString(clockwise.EdgeCounts) == "8,8,6,6", "Quarter turn maps original left/right edges to displayed top/bottom");
        GeneratedWaferMap trimmed = WaferMapGeneration.ApplyEdgeCounts(clockwise, new WaferMapEdgeCounts(4, 5, 3, 4));
        Assert(EdgeString(trimmed.EdgeCounts) == "4,5,3,4" && trimmed.RotationDegrees == 90, "Edge corrections are interpreted in displayed rotated orientation");
        AssertCoordinatesUnchanged(clockwise, trimmed);
        Assert(trimmed.Dies.Count(d => d.Row == trimmed.MaxRow) == 4 && trimmed.Dies.Count(d => d.Column == trimmed.MinColumn) == 3, "Displayed top/left edge counts use rotated indices");
        Assert(ReferenceEquals(trimmed.BaseMap, clockwise) && ReferenceEquals(trimmed.SourceBaseMap, source), "Adjusted map retains both oriented base and original source");
        Assert(WaferMapGeneration.Rotate(trimmed, 180).Count == 684, "Changing angle resets previous edge trim");
        foreach (int invalid in new[] { -90, 1, 45, 360 })
            Rejected(() => WaferMapGeneration.Rotate(source, invalid), "Reject unsupported or wrapped angle");
        GeneratedWaferMap empty = Generate(1m, 20m, 20m);
        Assert(WaferMapGeneration.Rotate(empty, 90).Count == 0, "An empty preview remains rotatable without persistence");
    }

    private static void VerifyCaptured684(string optionalPath)
    {
        // Lossless column ranges read from PLACE_WAFER_684.txt, 684 unique ROW/COL records.
        // Source SHA256 D70AC29821A3FF4EDE210CC9A0B622AAA9A98512099EE58F3A29BFEA44DC6EF7.
        // Captured from D:\Source\DATA_LOG\Config\WaferMap\PLACE_WAFER_684.txt.
        // This expected data is independent of both the model and mathematical oracle.
        int[] firstRows = { 14, 11, 8, 7, 5, 4, 4, 3, 2, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 4, 5, 7, 8, 11, 14 };
        int[] lastRows =  { 21, 24, 27, 28, 30, 31, 31, 32, 33, 33, 34, 34, 34, 34, 34, 34, 33, 33, 32, 31, 31, 30, 28, 27, 24, 21 };
        var fixture = new HashSet<string>();
        for (int column = 1; column <= firstRows.Length; column++)
        for (int row = firstRows[column - 1]; row <= lastRows[column - 1]; row++)
            fixture.Add(Key(column, row));
        Assert(fixture.Count == 684, "Captured fixture expands to 684 unique raw addresses");
        foreach (decimal diameter in new[] { 287.4m, 287m })
            Assert(Addresses(Generate(diameter, 10.370m, 7.913m, .3m, .3m)).SetEquals(fixture), "Every original raw address matches captured RKE684 for D" + diameter);
        Assert(!Addresses(Generate(287.64m, 10.370m, 7.913m, .3m, .3m)).SetEquals(fixture), "RKE D287.64 is not artificially forced to match the 684 fixture");
        if (!string.IsNullOrWhiteSpace(optionalPath))
        {
            var external = new HashSet<string>();
            foreach (string line in File.ReadLines(optionalPath).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                Assert(parts.Length == 2 && external.Add(Key(int.Parse(parts[1]), int.Parse(parts[0]))), "Optional external ROW/COL record is valid and unique");
            }
            Assert(external.SetEquals(fixture), "Optional read-only external fixture exactly matches permanent captured ranges");
            Console.WriteLine("FIXTURE: Read-only comparison matched " + optionalPath);
        }
    }

    private static void VerifyInvalidAndEmpty()
    {
        foreach (decimal invalid in new[] { 0m, -1m, .0001m, 2147483.648m, decimal.MaxValue })
        {
            Rejected(() => Generate(invalid, 2m, 3m), "Invalid diameter");
            Rejected(() => Generate(20m, invalid, 3m), "Invalid Die X");
            Rejected(() => Generate(20m, 2m, invalid), "Invalid Die Y");
        }
        foreach (decimal invalid in new[] { -1m, .0001m, 2147483.648m, decimal.MaxValue })
        {
            Rejected(() => Generate(20m, 2m, 3m, invalid, 0m), "Invalid GAP X");
            Rejected(() => Generate(20m, 2m, 3m, 0m, invalid), "Invalid GAP Y");
        }
        Rejected(() => Generate(2000m, 1m, 1m), "More than one million candidates rejected before allocation");
        Rejected(() => Generate(2147483.647m, .001m, .001m), "Candidate count rejected before int conversion");
        Rejected(() => Generate(20m, 2147483.647m, 2m, .001m, 0m), "Combined center step above original input range rejected");
        Rejected(() => WaferMapGeneration.Generate(null), "Null settings rejected");
        GeneratedWaferMap empty = Generate(10m, 6m, 8m, 100m, 100m);
        Assert(empty.Count == 0 && empty.Dies.Count == 0 && empty.UsedColumns == 0 && empty.UsedRows == 0, "Empty geometry is an explicit empty result");
        Assert(EdgeString(empty.EdgeCounts) == "0,0,0,0" && ReferenceEquals(empty.BaseMap, empty) && !empty.IsAdjusted, "Empty result has no edges and remains its own base");
        Rejected(() => WaferMapGeneration.ApplyEdgeCounts(empty, new WaferMapEdgeCounts(1, 1, 1, 1)), "Empty map cannot be adjusted");
        GeneratedWaferMap largeScale = Generate(2147483.647m, 500000m, 500000m);
        Assert(largeScale.CandidateColumns == 5 && largeScale.CandidateRows == 5 && largeScale.Count > 0, "Large supported dimensions do not overflow distance arithmetic");
    }

    private static void VerifyEdgeAdjustment()
    {
        GeneratedWaferMap original = Generate(287.4m, 10.370m, 7.913m, .3m, .3m);
        string originalSnapshot = Snapshot(original);
        Assert(EdgeString(original.EdgeCounts) == "6,6,8,8", "RKE684 original top/bottom/left/right edge counts");
        GeneratedWaferMap adjusted = WaferMapGeneration.ApplyEdgeCounts(original, new WaferMapEdgeCounts(4, 3, 5, 6));
        Assert(adjusted.Count == 674 && EdgeString(adjusted.EdgeCounts) == "4,3,5,6", "Independent edge reductions produce exactly requested counts");
        Assert(adjusted.IsAdjusted && ReferenceEquals(adjusted.BaseMap, original), "Adjusted map points to the immutable original base");
        Assert(Snapshot(original) == originalSnapshot, "Adjustment does not mutate the original result");
        Assert(Addresses(adjusted).IsSubsetOf(Addresses(original)), "Adjustment only removes original addresses");
        AssertCoordinatesUnchanged(original, adjusted);
        int minX = original.Dies.Min(d => d.RawColumn), maxX = original.Dies.Max(d => d.RawColumn);
        int minY = original.Dies.Min(d => d.RawRow), maxY = original.Dies.Max(d => d.RawRow);
        HashSet<string> adjustedAddresses = Addresses(adjusted);
        Assert(original.Dies.Where(d => d.RawColumn != minX && d.RawColumn != maxX && d.RawRow != minY && d.RawRow != maxY)
            .All(d => adjustedAddresses.Contains(Key(d.RawColumn, d.RawRow))), "Every non-edge die survives unchanged");
        Assert(EdgeKeys(adjusted, "top").SetEquals(CenteredEdge(original, "top", 4)), "Top is maximum raw Y and retains center-most requested dies");
        Assert(EdgeKeys(adjusted, "bottom").SetEquals(CenteredEdge(original, "bottom", 3)), "Bottom retains the requested center-most dies");
        Assert(EdgeKeys(adjusted, "left").SetEquals(CenteredEdge(original, "left", 5)), "Left retains center-most original positions");
        Assert(EdgeKeys(adjusted, "right").SetEquals(CenteredEdge(original, "right", 6)), "Right retains center-most original positions");
        GeneratedWaferMap next = WaferMapGeneration.ApplyEdgeCounts(adjusted, new WaferMapEdgeCounts(5, 5, 7, 7));
        GeneratedWaferMap direct = WaferMapGeneration.ApplyEdgeCounts(original, new WaferMapEdgeCounts(5, 5, 7, 7));
        Assert(Snapshot(next) == Snapshot(direct) && next.Count > adjusted.Count, "Reapplying recalculates from base and can restore previously removed dies");
        Assert(ReferenceEquals(next.BaseMap, original), "Repeated adjustment never replaces the original base");
        Assert(ReferenceEquals(WaferMapGeneration.ApplyEdgeCounts(next, original.EdgeCounts), original), "Restoring all original edge counts returns original object identity");
        Assert(ReferenceEquals(WaferMapGeneration.ApplyEdgeCounts(original, original.EdgeCounts), original), "Unchanged edge counts reuse unadjusted original");
        Assert(Snapshot(original) == originalSnapshot, "Original remains unchanged after repeated adjustments and restore");
        GeneratedWaferMap oneEach = WaferMapGeneration.ApplyEdgeCounts(original, new WaferMapEdgeCounts(1, 1, 1, 1));
        Assert(EdgeKeys(oneEach, "top").SetEquals(new[] { Key(13, 34) }), "Symmetric center tie selects the smaller raw column");
        Assert(EdgeKeys(oneEach, "left").SetEquals(new[] { Key(1, 17) }), "Odd-um physical center selects raw17 at -4.106 mm instead of raw18 at +4.107 mm");
        AssertCoordinatesUnchanged(original, oneEach);
    }

    private static void VerifyEdgeOverlapAndRange()
    {
        GeneratedWaferMap original = Generate(287.4m, 10.370m, 7.913m, .3m, .3m);
        string before = Snapshot(original);
        foreach (int invalid in new[] { -1, 0, int.MaxValue })
        {
            Rejected(() => WaferMapGeneration.ApplyEdgeCounts(original, new WaferMapEdgeCounts(invalid, 6, 8, 8)), "Invalid top count");
            Rejected(() => WaferMapGeneration.ApplyEdgeCounts(original, new WaferMapEdgeCounts(6, invalid, 8, 8)), "Invalid bottom count");
            Rejected(() => WaferMapGeneration.ApplyEdgeCounts(original, new WaferMapEdgeCounts(6, 6, invalid, 8)), "Invalid left count");
            Rejected(() => WaferMapGeneration.ApplyEdgeCounts(original, new WaferMapEdgeCounts(6, 6, 8, invalid)), "Invalid right count");
        }
        Rejected(() => WaferMapGeneration.ApplyEdgeCounts(original, new WaferMapEdgeCounts(7, 6, 8, 8)), "Cannot exceed original edge capacity");
        Rejected(() => WaferMapGeneration.ApplyEdgeCounts(null, original.EdgeCounts), "Null base map rejected");
        Rejected(() => WaferMapGeneration.ApplyEdgeCounts(original, null), "Null edge request rejected");
        GeneratedWaferMap single = Generate(9.602m, 6m, 8m);
        Assert(single.Count == 1 && ReferenceEquals(WaferMapGeneration.ApplyEdgeCounts(single, new WaferMapEdgeCounts(1, 1, 1, 1)), single), "Single-die unchanged adjustment remains valid");
        GeneratedWaferMap row = Generate(10m, 1m, 8m);
        Assert(row.UsedRows == 1 && row.UsedColumns > 1, "Single-row overlap fixture");
        Rejected(() => WaferMapGeneration.ApplyEdgeCounts(row, new WaferMapEdgeCounts(row.EdgeCounts.Top - 1, row.EdgeCounts.Bottom, 1, 1)), "Single-row contradictory top/bottom counts rejected");
        GeneratedWaferMap column = Generate(10m, 8m, 1m);
        Assert(column.UsedColumns == 1 && column.UsedRows > 1, "Single-column overlap fixture");
        Rejected(() => WaferMapGeneration.ApplyEdgeCounts(column, new WaferMapEdgeCounts(1, 1, column.EdgeCounts.Left - 1, column.EdgeCounts.Right)), "Single-column contradictory left/right counts rejected");
        GeneratedWaferMap intersectingEdges = Generate(.3m, .1m, .1m);
        Rejected(() => WaferMapGeneration.ApplyEdgeCounts(intersectingEdges, new WaferMapEdgeCounts(1, intersectingEdges.EdgeCounts.Bottom, intersectingEdges.EdgeCounts.Left, intersectingEdges.EdgeCounts.Right)), "Corner intersections cannot silently change another requested edge count");
        Assert(Snapshot(original) == before, "Rejected requests preserve original geometry and counts");
    }

    private static HashSet<string> CenteredEdge(GeneratedWaferMap map, string side, int count)
    {
        HashSet<string> edge = EdgeKeys(map, side);
        bool horizontal = side == "top" || side == "bottom";
        return new HashSet<string>(map.Dies.Where(d => edge.Contains(Key(d.RawColumn, d.RawRow)))
            .OrderBy(d => Math.Abs(horizontal ? d.CenterXMm : d.CenterYMm))
            .ThenBy(d => horizontal ? d.RawColumn : d.RawRow).Take(count).Select(d => Key(d.RawColumn, d.RawRow)));
    }

    private static HashSet<string> EdgeKeys(GeneratedWaferMap map, string side)
    {
        int minX = map.BaseMap.Dies.Min(d => d.RawColumn), maxX = map.BaseMap.Dies.Max(d => d.RawColumn);
        int minY = map.BaseMap.Dies.Min(d => d.RawRow), maxY = map.BaseMap.Dies.Max(d => d.RawRow);
        return new HashSet<string>(map.Dies.Where(d => side == "top" ? d.RawRow == maxY : side == "bottom" ? d.RawRow == minY : side == "left" ? d.RawColumn == minX : d.RawColumn == maxX).Select(d => Key(d.RawColumn, d.RawRow)));
    }

    private static void AssertCoordinatesUnchanged(GeneratedWaferMap original, GeneratedWaferMap adjusted)
    {
        var source = original.Dies.ToDictionary(d => Key(d.RawColumn, d.RawRow));
        Assert(adjusted.Dies.All(d => source.ContainsKey(Key(d.RawColumn, d.RawRow)) &&
            source[Key(d.RawColumn, d.RawRow)].CenterXMm == d.CenterXMm && source[Key(d.RawColumn, d.RawRow)].CenterYMm == d.CenterYMm), "Surviving raw addresses retain exact decimal mm coordinates");
    }

    private static void CompareOriginal(GeneratedWaferMap actual, long diameterUm, long pitchXUm, long pitchYUm, int? knownCount, string label)
    {
        OracleMap expected = OriginalCpp(diameterUm, pitchXUm, pitchYUm);
        Assert(actual.OutOfBoundsCount == 0, label + " legacy boundary metadata stays consistent with original accepted cells");
        Assert(actual.CandidateColumns == expected.Columns && actual.CandidateRows == expected.Rows, label + " original candidate counts");
        Assert(actual.Count == expected.Points.Count && actual.Count == actual.Dies.Count, label + " original target count");
        if (knownCount.HasValue) Assert(actual.Count == knownCount.Value, label + " known count");
        Assert(Addresses(actual).Count == actual.Count && Addresses(actual).SetEquals(expected.Points.Select(p => Key(p.Column, p.Row))), label + " complete unique raw-address set");
        var source = expected.Points.ToDictionary(p => Key(p.Column, p.Row));
        Assert(actual.Dies.All(d => source[Key(d.RawColumn, d.RawRow)].XUm / 1000m == d.CenterXMm && source[Key(d.RawColumn, d.RawRow)].YUm / 1000m == d.CenterYMm), label + " exact original integer-um positions and center phase");
        Assert(actual.Dies.GroupBy(d => d.RawRow).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count())
            .SequenceEqual(expected.Points.GroupBy(p => p.Row).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count())), label + " every original row population");
        int usedX = expected.Points.Count == 0 ? 0 : expected.Points.Max(p => p.Column) - expected.Points.Min(p => p.Column) + 1;
        int usedY = expected.Points.Count == 0 ? 0 : expected.Points.Max(p => p.Row) - expected.Points.Min(p => p.Row) + 1;
        Assert(actual.UsedColumns == usedX && actual.UsedRows == usedY, label + " original physical bounding box");
        Assert(ReferenceEquals(actual.BaseMap, actual) && !actual.IsAdjusted, label + " unadjusted immutable base identity");
        if (actual.Count > 0)
            Assert(actual.EdgeCounts.Top == EdgeKeys(actual, "top").Count && actual.EdgeCounts.Bottom == EdgeKeys(actual, "bottom").Count && actual.EdgeCounts.Left == EdgeKeys(actual, "left").Count && actual.EdgeCounts.Right == EdgeKeys(actual, "right").Count, label + " four actual outer-edge counts");
        Console.WriteLine("CPP: " + label + " candidates=" + expected.Columns + "x" + expected.Rows + " used=" + usedX + "x" + usedY + " targets=" + actual.Count);
    }

    private sealed class OraclePoint
    {
        internal int Column, Row;
        internal long XUm, YUm;
    }

    private sealed class OracleMap
    {
        internal int Columns, Rows;
        internal readonly List<OraclePoint> Points = new List<OraclePoint>();
    }

    private static OracleMap OriginalCpp(long diameterUm, long pitchX, long pitchY)
    {
        // Directly reproduce the C++ AUTO/non-Zigzag operations. The model uses decimal mm;
        // this oracle keeps separate integer um and sqrt/pow with the original quadrant branch.
        var result = new OracleMap { Columns = (int)(diameterUm / pitchX + 1), Rows = (int)(diameterUm / pitchY + 1) };
        long firstX = -(result.Columns * pitchX / 2), firstY = -(result.Rows * pitchY / 2);
        for (int row = 0; row < result.Rows; row++)
        for (int column = 0; column < result.Columns; column++)
        {
            long x = firstX + column * pitchX, y = firstY + row * pitchY;
            long transX = x, transY = y;
            if (transX > 0 && transY > 0) { transX += pitchX / 2; transY += pitchY / 2; }
            else if (transX <= 0 && transY > 0) { transX -= pitchX / 2; transY += pitchY / 2; }
            else if (transX < 0 && transY <= 0) { transX -= pitchX / 2; transY -= pitchY / 2; }
            else if (transX >= 0 && transY <= 0) { transX += pitchX / 2; transY -= pitchY / 2; }
            if (transX < 0) transX = -transX;
            if (transY < 0) transY = -transY;
            double distance = Math.Sqrt(Math.Pow(transX, 2) + Math.Pow(transY, 2));
            if (diameterUm / 2 + 200 <= distance) continue;
            result.Points.Add(new OraclePoint { Column = column, Row = row, XUm = x, YUm = y });
        }
        return result;
    }

    private static long ToUm(decimal mm) { return checked((long)(mm * 1000m)); }
    private static string Key(int column, int row) { return column + "," + row; }
    private static string EdgeString(WaferMapEdgeCounts edge) { return edge.Top + "," + edge.Bottom + "," + edge.Left + "," + edge.Right; }
    private static HashSet<string> Addresses(GeneratedWaferMap map) { return new HashSet<string>(map.Dies.Select(d => Key(d.RawColumn, d.RawRow))); }
    private static string Snapshot(GeneratedWaferMap map)
    {
        return map.Count + "|" + EdgeString(map.EdgeCounts) + "|" + string.Join(";", map.Dies.OrderBy(d => d.RawRow).ThenBy(d => d.RawColumn).Select(d => Key(d.RawColumn, d.RawRow) + ":" + d.CenterXMm + ":" + d.CenterYMm));
    }
    private static void Rejected(Action action, string message)
    {
        bool rejected = false;
        try { action(); }
        catch (ArgumentException) { rejected = true; }
        catch (InvalidOperationException) { rejected = true; }
        Assert(rejected, message);
    }
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _assertions++;
    }
}
