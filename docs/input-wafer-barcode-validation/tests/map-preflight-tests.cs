using System;
using System.Collections.Generic;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.Common.Logging;

internal static class MapPreflightTests
{
    private static int _passed;
    private static int _failed;
    private static InputStageUnit _stage;

    private static int Main()
    {
        Test("network-off-does-not-fetch-or-require-stage", delegate
        {
            AppSettingsStore.Current.UseLotNetworkWaferMap = false;
            string reason;
            Check(InputWaferMapPreflightService.TryValidate("candidate", null, out reason) &&
                  LotWaferMapFetchService.FetchCalls == 0 && RecipeStore.LoadCalls == 0, "OFF should be a read-free success.");
        });
        Test("settings-missing", delegate { AppSettingsStore.Current = null; ExpectFailure(); });
        Test("enabled-empty-folder", delegate
        {
            AppSettingsStore.Current.NetworkWaferMapFolder = " ";
            ExpectFailure();
            Check(LotWaferMapFetchService.FetchCalls == 0, "Empty folder caused a fetch.");
        });
        Test("stage-missing", delegate { _stage = null; ExpectFailure(); });
        Test("recipe-missing", delegate { _stage.Recipe = null; ExpectFailure(); });
        Test("wafer-missing", delegate { _stage.Wafer = null; ExpectFailure(); });
        Test("frame-missing", delegate
        {
            MaterialSpecs.Frame = null;
            ExpectFailure();
            Check(LotWaferMapFetchService.FetchCalls == 0, "Missing frame caused a fetch.");
        });
        Test("fetch-failure-preserves-reason", delegate
        {
            LotWaferMapFetchService.FetchResult = false;
            Check(ExpectFailure().Contains("SMB timeout: source-specific detail"), "Original fetch reason was lost.");
            Check(LotWaferMapFetchService.ParseCalls == 0, "Failed fetch still parsed a map.");
        });
        Test("missing-local-path", delegate { LotWaferMapFetchService.MissingLocalPath = true; ExpectFailure(); });
        Test("parse-failure-preserves-reason", delegate
        {
            LotWaferMapFetchService.ParseError = new System.IO.InvalidDataException("RAD duplicate address 1,2");
            Check(ExpectFailure().Contains("RAD duplicate address 1,2"), "Parser failure was lost.");
        });
        Test("cancellation-propagates", delegate
        {
            LotWaferMapFetchService.ParseError = new OperationCanceledException();
            string reason;
            try { InputWaferMapPreflightService.TryValidate("candidate", _stage, out reason); }
            catch (OperationCanceledException) { return; }
            throw new Exception("Cancellation was converted to a normal validation failure.");
        });
        Test("RKE-center-pitch-and-candidate-file", delegate
        {
            ExpectSuccess("YZAMH.02");
            Check(LotWaferMapFetchService.FetchCalls == 1 && LotWaferMapFetchService.ParseCalls == 1 &&
                  LotWaferMapFetchService.LastParsePath == "memory-cache/YZAMH.02", "Candidate map did not reach the parser.");
            Check(RecipeStore.LoadCalls == 0, "Existing wafer frame should precede recipe fallback.");
        });
        Test("new-barcode-selects-new-file", delegate
        {
            ExpectSuccess("YZAMH.02");
            ExpectSuccess("YZAMH.03");
            Check(LotWaferMapFetchService.LastBarcode == "YZAMH.03" &&
                  LotWaferMapFetchService.LastParsePath == "memory-cache/YZAMH.03", "Previous barcode map was reused.");
        });
        Test("no-barcode-frame-map-or-sequence-mutation", delegate
        {
            DieMap original = LotWaferMapFetchService.Map;
            ExpectSuccess("YZAMH.02");
            AssertUnchanged(original);
        });
        Test("wrong-product-pitch", delegate
        {
            LotWaferMapFetchService.Map.PitchX = 8.12;
            ExpectMapFailure("LOT-MAP-FRAME-MISMATCH");
        });
        Test("die-body-pitch-remains-compatible", delegate
        {
            LotWaferMapFetchService.Map.PitchX = 10.37;
            LotWaferMapFetchService.Map.PitchY = 7.913;
            ExpectSuccess("candidate");
        });
        Test("nonfinite-pitch-rejected", delegate
        {
            LotWaferMapFetchService.Map.PitchY = double.NaN;
            ExpectMapFailure("LOT-MAP-FRAME-MISMATCH");
        });
        Test("LOT-filter-no-target", delegate
        {
            MaterialStateService.Selected = new HashSet<int> { 2 };
            ExpectMapFailure("LOT-MAP-BIN-NO-TARGET");
            AssertUnchanged(LotWaferMapFetchService.Map);
        });
        Test("LOT-recipe-filter-intersection-empty", delegate
        {
            MaterialStateService.Selected = new HashSet<int> { 18 };
            ExpectMapFailure("MAP-RECIPE-BIN-NO-TARGET");
        });
        Test("LOT-recipe-filter-intersection-has-target", delegate
        {
            MaterialStateService.Selected = new HashSet<int> { 1, 18 };
            ExpectSuccess("candidate");
            Check(MaterialStateService.Selected.Count == 2, "LOT selection was modified.");
        });
        Test("recipe-filter-disabled", delegate
        {
            _stage.Recipe.DieMap.PickupBinFilterCsv = "";
            MaterialStateService.Selected = new HashSet<int> { 18 };
            ExpectSuccess("candidate");
        });
        Test("existing-non-target-not-revived", delegate
        {
            foreach (DieMapEntry entry in LotWaferMapFetchService.Map.Entries)
                entry.IsTarget = false;
            ExpectMapFailure("LOT-MAP-BIN-NO-TARGET");
            Check(!LotWaferMapFetchService.Map.Entries[0].IsTarget, "Preflight revived a skipped die.");
        });
        Test("map-records-missing", delegate
        {
            LotWaferMapFetchService.Map.Entries.Clear();
            ExpectMapFailure("LOT-MAP-FILE-MISSING");
        });
        Test("recipe-input-frame-fallback-is-read-only", delegate
        {
            MaterialSpecs.Frame = null;
            _stage.Wafer.TapeFrameSpecName = "";
            RecipeStore.Project = new RecipeProject { InputFrame = new TapeFrameSubset() };
            ExpectSuccess("candidate");
            Check(_stage.Wafer.TapeFrameSpecName == "" && MaterialSpecs.Frame == null && RecipeStore.LoadCalls == 2,
                "Frame fallback committed Material state.");
        });
        Test("recipe-input-frame-size-and-gap-are-used", delegate
        {
            MaterialSpecs.Frame = null;
            RecipeStore.Project = new RecipeProject
            {
                InputFrame = new TapeFrameSubset { DieSizeX = 9.0, DieSizeY = 6.0, PitchX = 0.2, PitchY = 0.3 },
                Frame = new TapeFrameSubset()
            };
            LotWaferMapFetchService.Map.PitchX = 9.2;
            LotWaferMapFetchService.Map.PitchY = 6.3;
            ExpectSuccess("candidate");
            Check(RecipeStore.Project.InputFrame.DieSizeX == 9.0 && RecipeStore.Project.InputFrame.PitchX == 0.2,
                "Recipe frame was mutated.");
        });
        Test("legacy-recipe-frame-fallback", delegate
        {
            MaterialSpecs.Frame = null;
            RecipeStore.Project = new RecipeProject { Frame = new TapeFrameSubset() };
            ExpectSuccess("candidate");
        });
        Test("in-place-Material-frame-change-rejected", delegate
        {
            LotWaferMapFetchService.DuringFetch = delegate { MaterialSpecs.Frame.PitchX = 0.2; };
            Check(ExpectFailure().Contains("사양이 변경"), "Changed Material frame was accepted.");
        });
        Test("recipe-fallback-frame-change-rejected", delegate
        {
            MaterialSpecs.Frame = null;
            RecipeStore.Project = new RecipeProject { InputFrame = new TapeFrameSubset() };
            LotWaferMapFetchService.DuringFetch = delegate { RecipeStore.Project.InputFrame.PitchY = 0.2; };
            Check(ExpectFailure().Contains("사양이 변경"), "Changed fallback recipe frame was accepted.");
        });
        Console.WriteLine("SUMMARY passed=" + _passed + ", failed=" + _failed);
        return _failed == 0 ? 0 : 1;
    }

    private static void Reset()
    {
        AppSettingsStore.Current = new AppSettings { UseLotNetworkWaferMap = true };
        _stage = new InputStageUnit();
        MaterialSpecs.Frame = new TapeFrameSpec();
        MaterialStateService.Selected = null;
        RecipeStore.Project = null;
        RecipeStore.LoadCalls = 0;
        EventLogger.Warnings.Clear();
        LotWaferMapFetchService.FetchResult = true;
        LotWaferMapFetchService.FetchCalls = 0;
        LotWaferMapFetchService.ParseCalls = 0;
        LotWaferMapFetchService.MissingLocalPath = false;
        LotWaferMapFetchService.ParseError = null;
        LotWaferMapFetchService.DuringFetch = null;
        LotWaferMapFetchService.Map = new DieMap();
        LotWaferMapFetchService.Map.Entries.Add(new DieMapEntry { BinCode = 1 });
        LotWaferMapFetchService.Map.Entries.Add(new DieMapEntry { BinCode = 18 });
    }

    private static void ExpectSuccess(string barcode)
    {
        string reason;
        Check(InputWaferMapPreflightService.TryValidate(barcode, _stage, out reason), "Unexpected failure: " + reason);
    }

    private static string ExpectFailure()
    {
        string reason;
        Check(!InputWaferMapPreflightService.TryValidate("candidate", _stage, out reason), "Invalid candidate was accepted.");
        Check(!string.IsNullOrWhiteSpace(reason), "Failure omitted its reason.");
        return reason;
    }

    private static void ExpectMapFailure(string expectedCode)
    {
        string code;
        string reason;
        Check(!InputWaferMapPreflightService.TryValidateParsedMap(LotWaferMapFetchService.Map, MaterialSpecs.Frame, _stage,
                  out code, out reason) && code == expectedCode && !string.IsNullOrWhiteSpace(reason),
            "Unexpected map failure: " + code + "; " + reason);
    }

    private static void AssertUnchanged(DieMap original)
    {
        Check(ReferenceEquals(original, LotWaferMapFetchService.Map) && original.Entries.Count == 2 &&
              original.Entries[0].BinCode == 1 && original.Entries[1].BinCode == 18 &&
              original.Entries[0].IsTarget && original.Entries[1].IsTarget &&
              original.Entries[0].SequenceNo == 17 && original.Entries[1].SequenceNo == 17 &&
              _stage.Wafer.BarcodeId == "UNCHANGED" && _stage.Wafer.TapeFrameSpecName == "RKE" &&
              MaterialSpecs.Frame.DieSizeX == 10.37 && MaterialSpecs.Frame.PitchX == 0.1,
            "Preflight mutated barcode, frame, map, eligibility, or pickup sequence.");
    }

    private static void Test(string name, Action action)
    {
        Reset();
        try { action(); _passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { _failed++; Console.Error.WriteLine("FAIL " + name + ": " + error); }
    }

    private static void Check(bool condition, string reason)
    {
        if (!condition)
            throw new InvalidOperationException(reason);
    }
}
