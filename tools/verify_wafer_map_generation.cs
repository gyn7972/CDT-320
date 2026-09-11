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
            Console.WriteLine("PASS: " + _assertions + " assertions. Original integer-um geometry, RKE/RAD addresses, edge counts, immutable coordinates, noncumulative adjustment and restoration verified.");
            Console.WriteLine("LIMIT: This standalone harness does not execute the Handler, UI, equipment or production storage.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL after " + _assertions + " assertions: " + ex);
            return 1;
        }
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
