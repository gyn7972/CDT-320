using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;

internal static class InputStageReviewMaterialGeometryTests
{
    private static readonly BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly MethodInfo Hash = typeof(MaterialStateService).GetMethod("BuildInputStageReviewGeometrySignature", Flags);
    private static readonly MethodInfo Shape = typeof(MaterialStateService).GetMethod("CheckInputStageReviewMapGeometry", Flags);
    private static int _checks;

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        _checks++;
    }

    private static string Signature(DieMap map) { return (string)Hash.Invoke(null, new object[] { map }); }
    private static bool Valid(DieMap map)
    {
        var arguments = new object[] { map, null };
        return (bool)Shape.Invoke(null, arguments);
    }

    private static DieMap Map(int count)
    {
        var map = new DieMap
        {
            FrameObjId = "OFFLINE-MAP", DieMapX = 35, DieMapY = 47,
            PitchX = 8.37, PitchY = 6.37, DieSizeX = 8.12, DieSizeY = 6.12,
            OriginX = 379.991945, OriginY = 9.735110, OuterDiameterMm = 287.640
        };
        for (int i = 0; i < count; i++)
        {
            int x = i % 35, y = i / 35;
            map.Entries.Add(new DieMapEntry
            {
                Index = i, DieUid = "OFFLINE-DIE-" + i.ToString("D4"),
                DieMapX = x, DieMapY = y, OriginalMapX = x, OriginalMapY = y,
                PosX = map.OriginX + x * map.PitchX, PosY = map.OriginY + y * map.PitchY,
                IsTarget = true, SequenceNo = i + 1
            });
        }
        return map;
    }

    private static void Measure(int count)
    {
        DieMap map = Map(count);
        for (int i = 0; i < 5; i++) { Signature(map); Valid(map); }
        const int repetitions = 40;
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < repetitions; i++) Signature(map);
        double hashMs = watch.Elapsed.TotalMilliseconds / repetitions;
        watch.Restart();
        for (int i = 0; i < repetitions; i++) Valid(map);
        double geometryMs = watch.Elapsed.TotalMilliseconds / repetitions;
        Console.WriteLine("MEASURE entries={0}, hash_mean_ms={1:F3}, shape_mean_ms={2:F3}, repetitions={3}",
            count, hashMs, geometryMs, repetitions);
    }

    private static int Main()
    {
        try
        {
            DieMap map = Map(1257);
            Require(Valid(map), "current-shaped map must pass geometry check");
            string original = Signature(map);
            map.Entries[0].SequenceNo = 999;
            map.Entries[0].IsTarget = false;
            map.Entries[0].BinCode = 99;
            map.Entries[0].Result = (QMC.CDT320.Materials.DieResult)1;
            Require(Signature(map) == original, "normal production state/order changes must not expire geometry");
            map.Entries.Reverse();
            Require(Signature(map) == original, "storage ordering must not expire geometry");
            DieMapEntry entry = map.Entries[0];
            double x = entry.PosX;
            entry.PosX = x + 0.0001;
            Require(Signature(map) != original, "live coordinate changes must expire geometry");
            entry.PosX = x;
            string uid = entry.DieUid;
            entry.DieUid = "CHANGED-UID";
            Require(Signature(map) != original, "physical identity changes must expire geometry");
            entry.DieUid = uid;
            double pitch = map.PitchX;
            map.PitchX += 0.01;
            Require(Signature(map) != original, "pitch changes must expire geometry");
            map.PitchX = pitch;
            entry.PosX = double.NaN;
            Require(!Valid(map), "NaN position must fail");
            entry.PosX = x;
            entry.DieUid = map.Entries[1].DieUid;
            Require(!Valid(map), "duplicate physical identity must fail");
            entry.DieUid = uid;
            entry.DieMapX = map.DieMapX;
            Require(!Valid(map), "out-of-grid coordinate must fail");
            entry.DieMapX = entry.OriginalMapX;
            Require(Valid(map), "valid live geometry should still pass after independent cases");
            Measure(1257);
            Measure(1645);
            Console.WriteLine("PASS: {0} actual Material geometry assertions; no Machine, Controller, Unit or Vision object created.", _checks);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
