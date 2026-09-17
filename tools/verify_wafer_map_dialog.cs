// 독립 다이얼로그/Designer만 실행하는 STA 검증. Form1과 장비 초기화는 링크하지 않는다.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT_320.Ui.Dialogs;

internal static class VerifyWaferMapDialog
{
    private static int _checks;
    private static string _output;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            _output = Path.GetFullPath(args[0]);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            WindowsFormsSynchronizationContext.AutoInstall = false;
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            Control.CheckForIllegalCrossThreadCalls = true;
            VerifyLegacyRestore();
            VerifyV2Restore();
            VerifyV3RestoreAndUpgrade();
            VerifyRkeV4Maps();
            VerifyJmbV4Maps();
            VerifyCountsAndBoundary();
            VerifySpecificationReapply();
            VerifyEachSpecificationValue();
            VerifyReapplyFailureAndRecovery();
            VerifyLegacySpecificationReapply();
            VerifyRotationSaveAndRestore();
            VerifyPageEntry();
            VerifyOutsideStoragePageEntry();
            VerifyEmptyAndClose();
            Check(QMC.Common.MessageDialog.UnexpectedCount == 0, "no unexpected UI failures");
            Console.WriteLine("PASS: " + _checks + " dialog/entry assertions; standalone controls only.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static WaferMapCreateDialog Open(WaferMapGenerationSettings settings, bool outputRole = false,
        GeneratedWaferMap restored = null, Func<GeneratedWaferMap, string> save = null)
    {
        var dialog = new WaferMapCreateDialog(settings, outputRole, restored, save);
        dialog.Opacity = 0;
        dialog.ShowInTaskbar = false;
        dialog.Show();
        Application.DoEvents();
        return dialog;
    }

    private static void VerifyLegacyRestore()
    {
        var settings = new WaferMapGenerationSettings(287.400m, 10.370m, 7.913m, .300m, .300m);
        var original = WaferMapGeneration.Generate(settings);
        Check(original.Count == 684 && original.UsedColumns == 26 && original.UsedRows == 34,
            "original V1 reference geometry remains 684 / 26x34");
        using (var dialog = Open(settings, false, original, map => null))
        {
            AwaitMap(dialog, 684);
            Check(object.ReferenceEquals(dialog.GeneratedMap, original), "legacy reopening preserves immutable map");
            Check(Find<Label>(dialog, "lblEdgeMargin").Text.Contains("이전식 외곽"), "legacy outward allowance not mislabeled as inward margin");
            Check(Find<NumericUpDown>(dialog, "numEdgeMargin").Value == .200m, "legacy outward allowance displayed exactly");
            Check(!Find<GroupBox>(dialog, "groupEdges").Enabled, "legacy map requires new generation before count editor");
            Check(Find<Button>(dialog, "btnSave").Enabled, "unchanged legacy map can still be saved");
            Capture(dialog, "dialog-legacy.png");
            Click(dialog, "btnGenerate");
            Wait(() => !dialog.IsBusy, "legacy to V4 generation");
            Check(dialog.GeneratedMap != null && dialog.GeneratedMap.Settings.GenerationVersion == 4,
                "AUTO always opts into the current circular candidate formula");
            Check(Find<GroupBox>(dialog, "groupEdges").Enabled, "regenerated V4 enables total and edge editor");
            Check(Find<Label>(dialog, "lblEdgeMargin").Text == "내부 여백 (mm)", "new result labels inward margin");
        }
    }

    private static void VerifyV2Restore()
    {
        var settings = new WaferMapGenerationSettings(20m, 2m, 2m, 0m, 0m, 1m);
        var original = WaferMapGeneration.Generate(settings);
        var adjusted = WaferMapGeneration.ApplyCounts(original, new WaferMapEdgeCounts(2, 2, 2, 2), 44);
        var restored = GeneratedWaferMapCodec.Restore(GeneratedWaferMapCodec.CreateDefinition(adjusted));
        GeneratedWaferMap saved = null;
        CheckMapEquivalent(restored, adjusted, "legacy V2 persisted geometry remains unchanged");
        using (var dialog = Open(settings, false, restored, map => { saved = map; return null; }))
        {
            AwaitMap(dialog, 44);
            Check(object.ReferenceEquals(dialog.GeneratedMap, restored) && dialog.GeneratedMap.Settings.GenerationVersion == 2,
                "opening V2 does not silently recompute or upgrade its immutable shape");
            Click(dialog, "btnSave");
            Check(object.ReferenceEquals(saved, restored) && GeneratedWaferMapCodec.CreateDefinition(saved).Version == 2,
                "saving untouched V2 preserves V2 metadata and exact map");
            Find<ComboBox>(dialog, "cmbRotation").SelectedIndex = 1; AwaitMap(dialog, 52);
            CheckMapEquivalent(dialog.GeneratedMap, WaferMapGeneration.Rotate(original, 90), "V2 rotation alone uses V2 AUTO source");
            Check(dialog.GeneratedMap.Settings.GenerationVersion == 2 && Find<Label>(dialog, "lblStatus").Text.Contains("FINAL APPLY는 차단"),
                "V2 quarter-turn keeps its version and current equipment restriction");
            Set(dialog, "numDiameter", 20.100m);
            Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 52);
            Check(dialog.GeneratedMap.Settings.GenerationVersion == 4 && dialog.GeneratedMap.RotationDegrees == 90 &&
                GeneratedWaferMapCodec.CreateDefinition(dialog.GeneratedMap).Version == 4,
                "explicit Apply from V2 uses new V4 formula and stores V4 metadata");
            Check(Find<Label>(dialog, "lblStatus").Text.Contains("FINAL APPLY는 차단"), "new V4 preserves quarter-turn equipment restriction");
        }
    }

    private static void VerifyV3RestoreAndUpgrade()
    {
        var oldSettings = new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .300m, .300m, .200m, 3);
        var original = WaferMapGeneration.Generate(oldSettings);
        var edited = WaferMapGeneration.ApplyCounts(original, new WaferMapEdgeCounts(6, 6, 8, 8), 684);
        var restored = GeneratedWaferMapCodec.Restore(GeneratedWaferMapCodec.CreateDefinition(edited));
        Check(restored.Settings.GenerationVersion == 3 && restored.Count == 684 && restored.UsedColumns == 26 && restored.UsedRows == 33,
            "saved V3 RKE fixture retains original 26x33 shape");
        GeneratedWaferMap saved = null;
        using (var dialog = Open(oldSettings, true, restored, map => { saved = map; return null; }))
        {
            AwaitMap(dialog, 684);
            CheckMapEquivalent(dialog.GeneratedMap, edited, "V3 reopen does not silently change center phase");
            Check(object.ReferenceEquals(dialog.GeneratedMap, restored), "V3 restored result remains immutable until explicit regeneration");
            Click(dialog, "btnSave");
            Check(GeneratedWaferMapCodec.CreateDefinition(saved).Version == 3, "untouched V3 save preserves original version");
            Capture(dialog, "dialog-rke-v3-restored.png");
            Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 684);
            var corrected = dialog.GeneratedMap;
            Check(corrected.Settings.GenerationVersion == 4 && corrected.UsedColumns == 26 && corrected.UsedRows == 34 && corrected.OutOfBoundsCount == 0,
                "explicit Apply upgrades old RKE map to correct V4 center phase");
            VerifyHitTest(dialog);
            Find<ComboBox>(dialog, "cmbRotation").SelectedIndex = 1;
            AwaitMap(dialog, corrected.BaseMap.Count);
            CheckMapEquivalent(dialog.GeneratedMap, WaferMapGeneration.Rotate(corrected.BaseMap, 90),
                "rotation resets selected V4 phase to canonical AUTO");
            Check(!dialog.GeneratedMap.IsAdjusted && dialog.GeneratedMap.Settings.GenerationVersion == 4,
                "rotation does not retain hidden phase or count adjustment");
            SetCounts(dialog, 8, 8, 6, 6, 684); Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 684);
            var quarter = dialog.GeneratedMap;
            Check(quarter.UsedColumns == 34 && quarter.UsedRows == 26 && quarter.OutOfBoundsCount == 0,
                "reapplying swapped requests elects rotated center phase");
            var clockwise = corrected.Dies.Select(die => Tuple.Create(die.CenterYMm, -die.CenterXMm))
                .OrderBy(point => point.Item1).ThenBy(point => point.Item2);
            var current = quarter.Dies.Select(die => Tuple.Create(die.CenterXMm, die.CenterYMm))
                .OrderBy(point => point.Item1).ThenBy(point => point.Item2);
            Check(clockwise.SequenceEqual(current), "rotated RKE phase preserves complete physical layout under clockwise rotation");
            VerifyHitTest(dialog);
            Check(Find<Label>(dialog, "lblStatus").Text.Contains("FINAL APPLY는 차단"), "selected V4 phase keeps quarter-turn restriction");
            Set(dialog, "numDiameter", 288m);
            Find<ComboBox>(dialog, "cmbRotation").SelectedIndex = 2;
            CheckPendingSpecification(dialog, quarter, 8, 8, 6, 6, 684, "selected phase with pending diameter and rotation");
            Click(dialog, "btnRestore"); AwaitMap(dialog, quarter.BaseMap.Count);
            CheckMapEquivalent(dialog.GeneratedMap, quarter.BaseMap, "restore after phase edit uses canonical AUTO source");
            Check(Find<NumericUpDown>(dialog, "numDiameter").Value == 287.640m && Find<ComboBox>(dialog, "cmbRotation").SelectedIndex == 1,
                "canonical restore also rolls pending diameter and angle back");
        }
        using (var dialog = Open(oldSettings, true, restored, map => null))
        {
            Find<ComboBox>(dialog, "cmbRotation").SelectedIndex = 2; AwaitMap(dialog, original.Count);
            CheckMapEquivalent(dialog.GeneratedMap, WaferMapGeneration.Rotate(original, 180), "saved V3 rotation alone retains V3 geometry");
            Check(dialog.GeneratedMap.Settings.GenerationVersion == 3, "rotation alone does not upgrade V3 to V4");
            Click(dialog, "btnGenerate"); AwaitMap(dialog, original.Count);
            Check(dialog.GeneratedMap.Settings.GenerationVersion == 4 && !dialog.GeneratedMap.IsAdjusted,
                "explicit AUTO upgrades V3 to V4 while preserving default AUTO behavior");
        }
    }

    private static void VerifyRkeV4Maps()
    {
        foreach (decimal margin in new[] { 0m, .200m })
        {
            using (var host = new QMC.CDT_320.Form1 { Opacity = 0, ShowInTaskbar = false, ClientSize = new Size(1526, 764) })
            using (var page = new QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage())
            {
                page.Dock = DockStyle.Fill; host.Controls.Add(page); host.Show(); Application.DoEvents();
                typeof(QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage)
                    .GetField("_currentRoleIsOutput", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(page, true);
                Set(page, "_nDiameter", 287.640m); Set(page, "_nDieSizeX", 10.370m); Set(page, "_nDieSizeY", 7.913m);
                Set(page, "_nPitchX", .300m); Set(page, "_nPitchY", .300m);
                GeneratedWaferMap saved = null;
                RunPagePreview(page, dialog =>
                {
                    Check(Find<Label>(dialog, "lblTitle").Text.StartsWith("OUTPUT") &&
                        Find<NumericUpDown>(dialog, "numDieX").Value == 10.370m && Find<NumericUpDown>(dialog, "numDieY").Value == 7.913m,
                        "RKE output editor uses explicit RKE die specification");
                    Set(dialog, "numEdgeMargin", margin);
                    Click(dialog, "btnGenerate"); Wait(() => !dialog.IsBusy, "RKE V4 AUTO");
                    Check(dialog.GeneratedMap != null && dialog.GeneratedMap.Settings.GenerationVersion == 4, "RKE AUTO uses current V4");
                    SetCounts(dialog, 6, 6, 8, 8, 684); Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 684);
                    saved = dialog.GeneratedMap;
                    Check(saved.UsedColumns == 26 && saved.UsedRows == 34 && saved.OutOfBoundsCount == 0 && saved.Settings.EdgeMarginMm == margin,
                        "RKE margin " + margin + " selects 26x34 center phase with zero outside dies");
                    CheckCounts(dialog, 6, 6, 8, 8, 684, "RKE exact count and four edge requests");
                    VerifyHitTest(dialog);
                    Click(dialog, "btnSave");
                    Check(Find<Label>(dialog, "lblStatus").Text.Contains("FINAL APPLY 하세요") &&
                        !Find<Label>(dialog, "lblStatus").Text.Contains("장비 사용 차단"), "in-bound RKE save allows normal review and FINAL APPLY workflow");
                    Capture(dialog, "dialog-rke-v4-margin-" + ((int)(margin * 1000m)).ToString("000") + ".png");
                });
                string reason;
                Check(page.StoredMap.Generation.Version == 4 && GeneratedWaferMapCodec.Validate(page.StoredMap, out reason),
                    "saved RKE selected-phase map passes strict geometry validation");
                Check(Find<NumericUpDown>(page, "_nGridX").Value == 26 && Find<NumericUpDown>(page, "_nGridY").Value == 34 &&
                    Find<NumericUpDown>(page, "_nGridX").ReadOnly && !Find<NumericUpDown>(page, "_nGridX").Enabled,
                    "RKE saved parent grid is read-only actual 26x34");
                RunPagePreview(page, dialog =>
                {
                    AwaitMap(dialog, 684);
                    CheckMapEquivalent(dialog.GeneratedMap, saved, "RKE selected center phase roundtrips through parent reopen");
                    CheckCounts(dialog, 6, 6, 8, 8, 684, "RKE reopened requests");
                    Check(Find<NumericUpDown>(dialog, "numEdgeMargin").Value == margin && dialog.GeneratedMap.Settings.GenerationVersion == 4,
                        "RKE reopened metadata restores exact margin and V4");
                    Click(dialog, "btnSave");
                });
                Check(host.SaveCalls == 2, "RKE selected-phase map can be saved again after reopening");
                host.Close();
            }
        }
    }

    private static void VerifyJmbV4Maps()
    {
        foreach (bool outputRole in new[] { false, true })
        {
            string role = outputRole ? "OUTPUT" : "INPUT";
            int count = outputRole ? 1142 : 1257;
            int columns = outputRole ? 34 : 35;
            int rows = outputRole ? 45 : 47;
            int outside = outputRole ? 4 : 38;
            int top = outputRole ? 2 : 3;
            int side = outputRole ? 7 : 9;
            using (var host = new QMC.CDT_320.Form1 { Opacity = 0, ShowInTaskbar = false, ClientSize = new Size(1526, 764) })
            using (var page = new QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage())
            {
                page.Dock = DockStyle.Fill; host.Controls.Add(page); host.Show(); Application.DoEvents();
                typeof(QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage)
                    .GetField("_currentRoleIsOutput", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(page, outputRole);
                Set(page, "_nDiameter", 287.640m); Set(page, "_nDieSizeX", 8.070m); Set(page, "_nDieSizeY", 6.070m);
                Set(page, "_nPitchX", outputRole ? .300m : .050m); Set(page, "_nPitchY", outputRole ? .300m : .050m);
                GeneratedWaferMap saved = null;
                RunPagePreview(page, dialog =>
                {
                    Check(!dialog.Controls.Find("numGridX", true).Any() && !dialog.Controls.Find("numGridY", true).Any(),
                        "JMB " + role + " does not introduce direct grid inputs");
                    Click(dialog, "btnGenerate");
                    Wait(() => !dialog.IsBusy && dialog.GeneratedMap != null, "JMB " + role + " AUTO");
                    Check(dialog.GeneratedMap.Settings.GenerationVersion == 4, "JMB " + role + " AUTO uses V4");
                    SetCounts(dialog, top, top, side, side, count); Click(dialog, "btnApplyEdges"); AwaitMap(dialog, count);
                    saved = dialog.GeneratedMap;
                    Check(saved.Settings.GenerationVersion == 4 && saved.UsedColumns == columns && saved.UsedRows == rows,
                        "JMB " + role + " requested count generates original grid " + columns + "x" + rows);
                    CheckCounts(dialog, top, top, side, side, count, "JMB " + role + " exact count and four edges");
                    Check(saved.OutOfBoundsCount == outside, "JMB " + role + " outside count remains explicit");
                    var v3Settings = new WaferMapGenerationSettings(287.640m, 8.070m, 6.070m,
                        outputRole ? .300m : .050m, outputRole ? .300m : .050m, .200m, 3);
                    var v3 = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Generate(v3Settings),
                        new WaferMapEdgeCounts(top, top, side, side), count);
                    CheckMapEquivalent(saved, v3, "JMB " + role + " V4 retains all prior V3 physical coordinates", false);
                    Click(dialog, "btnSave");
                    CheckOutsideStorageNotice(dialog, outside, "JMB " + role + " saves original full count");
                    Capture(dialog, "dialog-jmb-v4-" + role.ToLowerInvariant() + ".png");
                });
                Check(page.StoredMap.Generation.Version == 4 && page.StoredMap.Entries.Count == count,
                    "JMB " + role + " save payload includes V4 and every die");
                Check(Find<NumericUpDown>(page, "_nGridX").ReadOnly && !Find<NumericUpDown>(page, "_nGridX").Enabled &&
                    Find<NumericUpDown>(page, "_nGridX").Value == columns && Find<NumericUpDown>(page, "_nGridY").Value == rows,
                    "JMB " + role + " parent displays actual saved read-only grid");
                RunPagePreview(page, dialog =>
                {
                    AwaitMap(dialog, count);
                    CheckMapEquivalent(dialog.GeneratedMap, saved, "JMB " + role + " parent reopen restores V4 coordinates");
                    CheckCounts(dialog, top, top, side, side, count, "JMB " + role + " reopen retains requests");
                    CheckOutsideStorageNotice(dialog, outside, "JMB " + role + " reopen retains warning");
                    Click(dialog, "btnSave");
                });
                Check(host.SaveCalls == 2, "JMB " + role + " complete V4 map saves again after reopen");
                host.Close();
            }
        }
    }

    private static void VerifyCountsAndBoundary()
    {
        // Independent small geometry: centers +/-1,3,5,7; radius9; actual die far corners2,4,6,8.
        // 13 allowed absolute quadrant combinations x4 =52. Extending four edges4->6 adds8 outside.
        var settings = new WaferMapGenerationSettings(20m, 2m, 2m, 0m, 0m, 1m);
        GeneratedWaferMap saved = null;
        int saves = 0;
        using (var dialog = Open(settings, false, null, map => { saves++; saved = map; return null; }))
        {
            Check(dialog.GeneratedMap == null && !Find<Button>(dialog, "btnSave").Enabled, "initial preview cannot save");
            Check(Find<NumericUpDown>(dialog, "numEdgeMargin").Value == 1m, "initial inward margin retained");
            Click(dialog, "btnGenerate");
            AwaitMap(dialog, 52);
            var original = dialog.GeneratedMap;
            Check(original.BoundaryRadiusMm == 9m && original.OutOfBoundsCount == 0, "inside radius is diameter/2 minus margin");
            Check(original.UsedColumns == 8 && original.UsedRows == 8, "used grid is a result");
            Check(Find<NumericUpDown>(dialog, "numTotalCount").Value == 52, "total input initialized to actual count");
            Check(Find<NumericUpDown>(dialog, "numEdgeTop").Maximum > original.EdgeCounts.Top, "edge input no longer capped at original count");
            Capture(dialog, "dialog-original.png");

            SetCounts(dialog, 6, 6, 6, 6, 60);
            Check(dialog.GeneratedMap == null && !Find<Button>(dialog, "btnSave").Enabled, "pending count inputs invalidate save result");
            Click(dialog, "btnApplyEdges");
            AwaitMap(dialog, 60);
            var expanded = dialog.GeneratedMap;
            Check(expanded.OutOfBoundsCount == 8 && expanded.Count == 60, "outside dies retained instead of silently deleted");
            Check(expanded.EdgeCounts.Top == 6 && expanded.EdgeCounts.Bottom == 6 && expanded.EdgeCounts.Left == 6 && expanded.EdgeCounts.Right == 6,
                "all four increased edge counts respected");
            Check(expanded.RequestedTotalCount == 60 && expanded.RequestedEdgeCounts.Top == 6, "requested count constraints retained");
            Check(Find<Button>(dialog, "btnSave").Enabled && saves == 0, "complete outside result permits configuration save");
            Click(dialog, "btnSave");
            Check(saves == 1 && object.ReferenceEquals(saved, expanded) && saved.OutOfBoundsCount == 8,
                "outside save callback receives every die including out-of-bound entries");
            CheckOutsideStorageNotice(dialog, 8, "outside save");
            VerifyOutsideDrawingAndFit(dialog);
            Capture(dialog, "dialog-outside.png");

            Set(dialog, "numTotalCount", 5);
            Click(dialog, "btnApplyEdges");
            Wait(() => !dialog.IsBusy, "contradictory counts complete");
            Check(dialog.GeneratedMap == null && object.ReferenceEquals(Find<WaferMapGenerationView>(dialog, "mapView").Map, expanded),
                "contradictory counts keep previous preview without a savable result");
            Check(!Find<Button>(dialog, "btnSave").Enabled && Find<Label>(dialog, "lblStatus").Text.Contains("미적용"), "contradiction is explicit and cannot save");

            SetCounts(dialog, 2, 2, 2, 2, 44);
            Click(dialog, "btnApplyEdges");
            AwaitMap(dialog, 44);
            Check(dialog.GeneratedMap.OutOfBoundsCount == 0 && Find<Button>(dialog, "btnSave").Enabled, "decreased counts restore valid save state");
            Check(dialog.GeneratedMap.EdgeCounts.Top == 2 && dialog.GeneratedMap.EdgeCounts.Left == 2, "decreased edges applied");
            Click(dialog, "btnSave");
            Check(saves == 2 && object.ReferenceEquals(saved, dialog.GeneratedMap), "subsequent save receives exact recovered preview once");
            Capture(dialog, "dialog-recovered.png");
            SetCounts(dialog, 4, 4, 4, 4, 120);
            Click(dialog, "btnApplyEdges");
            AwaitMap(dialog, 120);
            Check(dialog.GeneratedMap.OutOfBoundsCount > 0 && dialog.GeneratedMap.UsedColumns > original.UsedColumns,
                "requested total can expand the result beyond original used grid");
            Check(dialog.GeneratedMap.Dies.Any(die => Math.Abs(die.CenterXMm) + 1m > 10m || Math.Abs(die.CenterYMm) + 1m > 10m),
                "fit fixture extends beyond the outer wafer radius");
            VerifyOutsideDrawingAndFit(dialog);
            Capture(dialog, "dialog-expanded-outside.png");
            Click(dialog, "btnRestore");
            AwaitMap(dialog, 52);
            CheckMapEquivalent(dialog.GeneratedMap, original, "restore returns AUTO base");
            Check(Find<NumericUpDown>(dialog, "numTotalCount").Value == 52, "restore resets requested total");

            Set(dialog, "numEdgeMargin", 2m);
            Check(dialog.GeneratedMap == null && !Find<Button>(dialog, "btnSave").Enabled, "margin edit invalidates old result");
            Click(dialog, "btnGenerate");
            AwaitMap(dialog, 32);
            Check(dialog.GeneratedMap.BoundaryRadiusMm == 8m && dialog.GeneratedMap.Settings.GapXMm == 0m, "margin decreases usable radius without changing die gap");
            dialog.Size = dialog.MinimumSize;
            AwaitDrawing(dialog);
            Check(Find<Button>(dialog, "btnSave").Width > 50 && Find<Button>(dialog, "btnClose").Width > 30, "minimum-size footer stays usable");
            Capture(dialog, "dialog-minimum.png");
            Set(dialog, "numEdgeMargin", 10.001m);
            Click(dialog, "btnGenerate");
            Wait(() => !dialog.IsBusy, "invalid margin generation");
            Check(dialog.GeneratedMap == null && !Find<Button>(dialog, "btnSave").Enabled, "margin larger than radius is not accepted");
        }
        using (var reopened = Open(settings, false, GeneratedWaferMapCodec.Restore(GeneratedWaferMapCodec.CreateDefinition(saved)), map => null))
        {
            AwaitMap(reopened, 44);
            Check(Find<NumericUpDown>(reopened, "numTotalCount").Value == 44 &&
                Find<NumericUpDown>(reopened, "numEdgeTop").Value == 2 && Find<NumericUpDown>(reopened, "numEdgeRight").Value == 2,
                "adjusted V2 total and all edge input constraints reopen with saved values");
            Check(Find<NumericUpDown>(reopened, "numEdgeMargin").Value == 1m && Find<Button>(reopened, "btnSave").Enabled,
                "adjusted V2 margin and valid save state reopen together");
        }
    }

    private static void VerifySpecificationReapply()
    {
        // User fixture stays unchanged: increasing diameter moves the lattice phase as well as the boundary.
        // D303 is a verified in-bound case; RequiredOuterDiameter of the old layout is not a guarantee after AUTO.
        var settings = new WaferMapGenerationSettings(287.640m, 10.370m, 7.913m, .100m, .100m, .200m);
        GeneratedWaferMap saved = null;
        int saves = 0;
        using (var dialog = Open(settings, true, null, map => { saved = map; saves++; return null; }))
        {
            Click(dialog, "btnGenerate"); AwaitMap(dialog, 713);
            SetCounts(dialog, 3, 3, 8, 8, 730);
            Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 730);
            var outside = dialog.GeneratedMap;
            Check(outside.OutOfBoundsCount == 21, "user fixture retains 730 requested dies including 21 outside");
            Check(Find<Button>(dialog, "btnSave").Enabled, "complete user fixture can save outside configuration");
            Click(dialog, "btnSave");
            Check(saves == 1 && object.ReferenceEquals(saved, outside) && saved.Count == 730 && saved.OutOfBoundsCount == 21,
                "user fixture saves all 730 including the 21 red dies");
            CheckOutsideStorageNotice(dialog, 21, "user fixture configuration saved");
            Capture(dialog, "dialog-specification-before.png");
            Set(dialog, "numDiameter", 303m);
            CheckPendingSpecification(dialog, outside, 3, 3, 8, 8, 730, "diameter edit");
            Check(Find<NumericUpDown>(dialog, "numDiameter").Value == 303m && outside.Settings.OuterDiameterMm == 287.640m,
                "pending new diameter and previous immutable layout remain distinct");
            Capture(dialog, "dialog-specification-pending.png");
            Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 730);
            var applied = dialog.GeneratedMap;
            Check(applied.Settings.OuterDiameterMm == 303m && applied.OutOfBoundsCount == 0,
                "Apply regenerates requested 730 with current diameter and recovers boundary");
            CheckCounts(dialog, 3, 3, 8, 8, 730, "diameter Apply");
            Check(applied.BaseMap.Count == 800 && applied.BaseMap.Settings.OuterDiameterMm == 303m,
                "adjusted result references AUTO base for the new diameter");
            Click(dialog, "btnSave");
            Check(saves == 2 && object.ReferenceEquals(saved, applied) && saved.Settings.OuterDiameterMm == 303m,
                "save callback gets exact regenerated map and new diameter once");
            Capture(dialog, "dialog-specification-recovered.png");

            Set(dialog, "numDiameter", 304m);
            Find<ComboBox>(dialog, "cmbRotation").SelectedIndex = 2;
            CheckPendingSpecification(dialog, applied, 3, 3, 8, 8, 730, "rotation while specification pending");
            Check(!dialog.IsBusy && Find<ComboBox>(dialog, "cmbRotation").SelectedIndex == 2,
                "pending rotation does not regenerate previous specification automatically");
            Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 730);
            var rotated = dialog.GeneratedMap;
            Check(rotated.RotationDegrees == 180 && rotated.Settings.OuterDiameterMm == 304m,
                "Apply uses pending diameter and absolute selected rotation together");
            CheckCounts(dialog, 3, 3, 8, 8, 730, "pending rotation Apply");
            Set(dialog, "numDiameter", 310m); Set(dialog, "numGapX", .200m);
            Find<ComboBox>(dialog, "cmbRotation").SelectedIndex = 3;
            CheckPendingSpecification(dialog, rotated, 3, 3, 8, 8, 730, "pending settings before restore");
            Click(dialog, "btnRestore"); AwaitMap(dialog, rotated.BaseMap.Count);
            CheckMapEquivalent(dialog.GeneratedMap, rotated.BaseMap, "restore uses last successfully generated specification and AUTO base");
            Check(Find<NumericUpDown>(dialog, "numDiameter").Value == 304m &&
                Find<NumericUpDown>(dialog, "numGapX").Value == .100m &&
                Find<ComboBox>(dialog, "cmbRotation").SelectedIndex == 2,
                "restore rolls pending controls back to displayed AUTO source");
            CheckCounts(dialog, rotated.BaseMap.EdgeCounts.Top, rotated.BaseMap.EdgeCounts.Bottom,
                rotated.BaseMap.EdgeCounts.Left, rotated.BaseMap.EdgeCounts.Right, rotated.BaseMap.Count, "restored new AUTO counts");
        }
        var definition = GeneratedWaferMapCodec.CreateDefinition(saved);
        var storedMap = GeneratedWaferMapCodec.ToDieMap(saved, "SPECIFICATION_REAPPLY");
        string reason;
        Check(GeneratedWaferMapCodec.Validate(storedMap, out reason), "regenerated save payload validates: " + reason);
        var restored = GeneratedWaferMapCodec.Restore(definition);
        CheckMapEquivalent(restored, saved, "regenerated map metadata roundtrip");
        using (var reopened = Open(restored.Settings, true, restored, map => null))
        {
            AwaitMap(reopened, 730);
            CheckCounts(reopened, 3, 3, 8, 8, 730, "reopen regenerated map");
            Check(Find<NumericUpDown>(reopened, "numDiameter").Value == 303m, "reopen retains new saved diameter");
            Set(reopened, "numDiameter", 307m);
            Click(reopened, "btnRestore"); AwaitMap(reopened, 800);
            Check(Find<NumericUpDown>(reopened, "numDiameter").Value == 303m && !reopened.GeneratedMap.IsAdjusted,
                "reopened restored base belongs to saved new specification");
        }
    }

    private static void VerifyEachSpecificationValue()
    {
        var settings = new WaferMapGenerationSettings(20m, 2m, 2m, 0m, 0m, 1m);
        var source = WaferMapGeneration.Generate(settings);
        var adjusted = WaferMapGeneration.ApplyCounts(source, new WaferMapEdgeCounts(2, 2, 2, 2), 44);
        string[] controls = { "numDiameter", "numDieX", "numDieY", "numGapX", "numGapY", "numEdgeMargin" };
        decimal[] values = { 22m, 2.100m, 2.200m, .100m, .200m, 1.200m };
        for (int index = 0; index < controls.Length; index++)
        {
            using (var dialog = Open(settings, false, adjusted, map => null))
            {
                AwaitMap(dialog, 44);
                Set(dialog, controls[index], values[index]);
                CheckPendingSpecification(dialog, adjusted, 2, 2, 2, 2, 44, controls[index]);
                var edited = ReadSettings(dialog);
                var expectedBase = WaferMapGeneration.Generate(edited);
                var expected = WaferMapGeneration.ApplyCounts(expectedBase, new WaferMapEdgeCounts(2, 2, 2, 2), 44);
                Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 44);
                CheckMapEquivalent(dialog.GeneratedMap, expected, controls[index] + " propagates current upper specification");
                CheckCounts(dialog, 2, 2, 2, 2, 44, controls[index] + " preserves requests");
                Check(Find<Button>(dialog, "btnSave").Enabled,
                    controls[index] + " completed result permits configuration save including outside dies");
                CheckMapEquivalent(GeneratedWaferMapCodec.Restore(GeneratedWaferMapCodec.CreateDefinition(dialog.GeneratedMap)),
                    expected, controls[index] + " metadata captures current specification");
                Set(dialog, "numTotalCount", 45);
                Click(dialog, "btnRestore"); AwaitMap(dialog, expectedBase.Count);
                CheckMapEquivalent(dialog.GeneratedMap, expectedBase, controls[index] + " restores new AUTO base");
                Check(Find<NumericUpDown>(dialog, controls[index]).Value == values[index], controls[index] + " remains new value after restore");
                SetCounts(dialog, 2, 2, 2, 2, 44);
                Click(dialog, "btnGenerate"); AwaitMap(dialog, expectedBase.Count);
                CheckMapEquivalent(dialog.GeneratedMap, expectedBase, controls[index] + " AUTO resets custom counts");
                CheckCounts(dialog, expectedBase.EdgeCounts.Top, expectedBase.EdgeCounts.Bottom,
                    expectedBase.EdgeCounts.Left, expectedBase.EdgeCounts.Right, expectedBase.Count, controls[index] + " AUTO inputs reset");
            }
        }
    }

    private static void VerifyReapplyFailureAndRecovery()
    {
        var settings = new WaferMapGenerationSettings(20m, 2m, 2m, 0m, 0m, 1m);
        var adjusted = WaferMapGeneration.ApplyCounts(WaferMapGeneration.Generate(settings), new WaferMapEdgeCounts(2, 2, 2, 2), 44);
        using (var dialog = Open(settings, false, adjusted, map => null))
        {
            AwaitMap(dialog, 44);
            Set(dialog, "numDieX", 0m);
            CheckPendingSpecification(dialog, adjusted, 2, 2, 2, 2, 44, "invalid die size");
            Click(dialog, "btnApplyEdges"); Wait(() => !dialog.IsBusy, "invalid specification Apply");
            CheckPendingSpecification(dialog, adjusted, 2, 2, 2, 2, 44, "failed invalid specification Apply");
            Check(Find<NumericUpDown>(dialog, "numDieX").Value == 0m, "failure preserves invalid input for correction");
            Set(dialog, "numDieX", 2m);
            Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 44);
            Check(Find<Button>(dialog, "btnSave").Enabled, "valid specification recovers save without resetting count request");
            var valid = dialog.GeneratedMap;
            Set(dialog, "numDiameter", 22m); Set(dialog, "numTotalCount", 5);
            Click(dialog, "btnApplyEdges"); Wait(() => !dialog.IsBusy, "invalid count with new specification");
            CheckPendingSpecification(dialog, valid, 2, 2, 2, 2, 5, "failed inconsistent count Apply");
            Check(Find<NumericUpDown>(dialog, "numDiameter").Value == 22m, "count failure does not roll back entered new diameter");
            Set(dialog, "numTotalCount", 44);
            Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 44);
            Check(dialog.GeneratedMap.Settings.OuterDiameterMm == 22m && dialog.GeneratedMap.OutOfBoundsCount == 0,
                "corrected count recovers using pending new specification");
            valid = dialog.GeneratedMap;
            Set(dialog, "numEdgeMargin", 12m);
            Click(dialog, "btnApplyEdges"); Wait(() => !dialog.IsBusy, "invalid margin Apply");
            CheckPendingSpecification(dialog, valid, 2, 2, 2, 2, 44, "failed margin Apply");
            Click(dialog, "btnRestore"); AwaitMap(dialog, valid.BaseMap.Count);
            Check(Find<NumericUpDown>(dialog, "numEdgeMargin").Value == 1m, "restore rolls invalid margin back to last generated setting");
            CheckMapEquivalent(dialog.GeneratedMap, valid.BaseMap, "restore after failure preserves last successful AUTO source");
        }
    }

    private static void VerifyLegacySpecificationReapply()
    {
        var settings = new WaferMapGenerationSettings(287.400m, 10.370m, 7.913m, .300m, .300m);
        var legacy = WaferMapGeneration.Generate(settings);
        using (var dialog = Open(settings, false, legacy, map => null))
        {
            AwaitMap(dialog, 684);
            Set(dialog, "numDiameter", 303m);
            CheckPendingSpecification(dialog, legacy, 6, 6, 8, 8, 684, "legacy specification edit");
            Click(dialog, "btnRestore"); AwaitMap(dialog, 684);
            CheckMapEquivalent(dialog.GeneratedMap, legacy, "legacy pending edits restore original V1 shape");
            Check(Find<Label>(dialog, "lblEdgeMargin").Text.Contains("이전식 외곽") && !Find<GroupBox>(dialog, "groupEdges").Enabled,
                "restored V1 keeps truthful outward allowance and legacy count-edit restriction");
            Set(dialog, "numDiameter", 303m);
            SetCounts(dialog, 3, 3, 8, 8, 730);
            Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 730);
            Check(dialog.GeneratedMap.Settings.GenerationVersion == 4 && dialog.GeneratedMap.Settings.OuterDiameterMm == 303m &&
                dialog.GeneratedMap.Settings.EdgeMarginMm == .200m, "editing legacy source permits explicit current V4 count regeneration");
            CheckCounts(dialog, 3, 3, 8, 8, 730, "legacy to V4 Apply");
        }
    }

    private static WaferMapGenerationSettings ReadSettings(WaferMapCreateDialog dialog)
    {
        return new WaferMapGenerationSettings(Find<NumericUpDown>(dialog, "numDiameter").Value,
            Find<NumericUpDown>(dialog, "numDieX").Value, Find<NumericUpDown>(dialog, "numDieY").Value,
            Find<NumericUpDown>(dialog, "numGapX").Value, Find<NumericUpDown>(dialog, "numGapY").Value,
            Find<NumericUpDown>(dialog, "numEdgeMargin").Value, 4);
    }

    private static void CheckPendingSpecification(WaferMapCreateDialog dialog, GeneratedWaferMap preview,
        int top, int bottom, int left, int right, int total, string operation)
    {
        Check(dialog.GeneratedMap == null && !Find<Button>(dialog, "btnSave").Enabled, operation + " cannot return or save stale result");
        Check(object.ReferenceEquals(Find<WaferMapGenerationView>(dialog, "mapView").Map, preview), operation + " retains previous immutable preview");
        Check(Find<GroupBox>(dialog, "groupEdges").Enabled && Find<Button>(dialog, "btnApplyEdges").Enabled,
            operation + " leaves count reapply available");
        CheckCounts(dialog, top, bottom, left, right, total, operation + " keeps requests");
    }

    private static void CheckCounts(WaferMapCreateDialog dialog, int top, int bottom, int left, int right, int total, string operation)
    {
        Check(Find<NumericUpDown>(dialog, "numEdgeTop").Value == top && Find<NumericUpDown>(dialog, "numEdgeBottom").Value == bottom &&
            Find<NumericUpDown>(dialog, "numEdgeLeft").Value == left && Find<NumericUpDown>(dialog, "numEdgeRight").Value == right &&
            Find<NumericUpDown>(dialog, "numTotalCount").Value == total, operation + " numeric count inputs");
        if (dialog.GeneratedMap != null)
        {
            var map = dialog.GeneratedMap;
            Check(map.Count == total && map.EdgeCounts.Top == top && map.EdgeCounts.Bottom == bottom &&
                map.EdgeCounts.Left == left && map.EdgeCounts.Right == right, operation + " actual count result");
        }
    }

    private static void CheckMapEquivalent(GeneratedWaferMap actual, GeneratedWaferMap expected, string operation, bool sameVersion = true)
    {
        Check(actual != null && actual.Count == expected.Count && actual.RotationDegrees == expected.RotationDegrees &&
            actual.OutOfBoundsCount == expected.OutOfBoundsCount && actual.UsedColumns == expected.UsedColumns && actual.UsedRows == expected.UsedRows,
            operation + " result summary");
        var first = actual.Settings; var second = expected.Settings;
        Check((!sameVersion || first.GenerationVersion == second.GenerationVersion) && first.OuterDiameterMm == second.OuterDiameterMm &&
            first.DieSizeXMm == second.DieSizeXMm && first.DieSizeYMm == second.DieSizeYMm && first.GapXMm == second.GapXMm &&
            first.GapYMm == second.GapYMm && first.EdgeMarginMm == second.EdgeMarginMm, operation + " source specification");
        Check(actual.Dies.Zip(expected.Dies, (a, b) => a.RawColumn == b.RawColumn && a.RawRow == b.RawRow &&
            a.Column == b.Column && a.Row == b.Row && a.CenterXMm == b.CenterXMm && a.CenterYMm == b.CenterYMm).All(equal => equal),
            operation + " original addresses and exact physical coordinates");
    }

    private static void SetCounts(WaferMapCreateDialog dialog, int top, int bottom, int left, int right, int total)
    {
        Set(dialog, "numEdgeTop", top); Set(dialog, "numEdgeBottom", bottom);
        Set(dialog, "numEdgeLeft", left); Set(dialog, "numEdgeRight", right); Set(dialog, "numTotalCount", total);
    }

    private static void VerifyOutsideDrawingAndFit(WaferMapCreateDialog dialog)
    {
        var view = Find<WaferMapGenerationView>(dialog, "mapView");
        var layout = typeof(WaferMapGenerationView).GetField("_layout", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        double scale = Convert.ToDouble(layout.GetType().GetField("Scale").GetValue(layout));
        double cx = Convert.ToDouble(layout.GetType().GetField("CenterX").GetValue(layout));
        double cy = Convert.ToDouble(layout.GetType().GetField("CenterY").GetValue(layout));
        var bitmap = (Bitmap)typeof(WaferMapGenerationView).GetField("_bitmap", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        foreach (var die in view.Map.Dies)
        {
            double x = cx + (double)die.CenterXMm * scale;
            double y = cy - (double)die.CenterYMm * scale;
            double hw = (double)view.Map.DisplayDieSizeXMm * scale / 2.0;
            double hh = (double)view.Map.DisplayDieSizeYMm * scale / 2.0;
            Check(x - hw >= 0 && y - hh >= 0 && x + hw < view.Width && y + hh < view.Height, "fit includes complete die " + die.Column + "," + die.Row);
            if (!view.Map.IsWithinBoundary(die))
            {
                Color color = bitmap.GetPixel((int)Math.Round(x), (int)Math.Round(y));
                Check(color.R > color.G * 2, "out-of-bound die center is painted red");
            }
        }
    }

    private static void VerifyRotationSaveAndRestore()
    {
        var settings = new WaferMapGenerationSettings(20m, 2m, 1m, .5m, .1m, .750m);
        GeneratedWaferMap saved = null;
        bool failSave = false;
        int saves = 0;
        using (var dialog = Open(settings, true, null, map => { saves++; if (failSave) return "검증용 저장 실패"; saved = map; return null; }))
        {
            Click(dialog, "btnGenerate");
            Wait(() => !dialog.IsBusy && dialog.GeneratedMap != null, "rotation fixture generation");
            int count = dialog.GeneratedMap.Count;
            int columns = dialog.GeneratedMap.UsedColumns;
            int rows = dialog.GeneratedMap.UsedRows;
            Check(count > 0 && dialog.GeneratedMap.OutOfBoundsCount == 0, "rotation fixture fits boundary");
            Check(Find<Label>(dialog, "lblTitle").Text.StartsWith("OUTPUT"), "output role explicit");
            var rotation = Find<ComboBox>(dialog, "cmbRotation");
            for (int index = 0; index < 4; index++)
            {
                rotation.SelectedIndex = index;
                AwaitMap(dialog, count);
                var map = dialog.GeneratedMap;
                Check(map.RotationDegrees == index * 90 && !map.IsAdjusted, "absolute rotation resets count adjustment " + index);
                Check(map.UsedColumns == (index % 2 == 0 ? columns : rows), "rotation swaps only used axes " + index);
                Check(map.DisplayDieSizeXMm == (index % 2 == 0 ? 2m : 1m), "rotation swaps rendered die size " + index);
                Check(Find<Label>(dialog, "lblStatus").Text.Contains("현재 장비에서 사용할 수 없습니다") == (index % 2 == 1), "quarter-turn machine restriction " + index);
                VerifyHitTest(dialog);
            }
            Click(dialog, "btnSave");
            Check(saves == 1 && object.ReferenceEquals(saved, dialog.GeneratedMap), "quarter-turn in-bound draft save allowed");
            Check(Find<Label>(dialog, "lblStatus").Text.Contains("FINAL APPLY는 차단") && !Find<Label>(dialog, "lblStatus").Text.Contains("FINAL APPLY 하세요"), "quarter saved settings do not permit final apply");
            Capture(dialog, "dialog-output-saved-270.png");
            failSave = true;
            int messages = QMC.Common.MessageDialog.UnexpectedCount;
            Click(dialog, "btnSave");
            Check(QMC.Common.MessageDialog.UnexpectedCount == messages + 1 && !dialog.IsDisposed, "save failure reported without closing dialog");
            QMC.Common.MessageDialog.UnexpectedCount = messages;
        }
        var restored = GeneratedWaferMapCodec.Restore(GeneratedWaferMapCodec.CreateDefinition(saved));
        using (var dialog = Open(settings, false, restored, map => null))
        {
            AwaitMap(dialog, saved.Count);
            Check(Find<NumericUpDown>(dialog, "numEdgeMargin").Value == .750m, "saved V2 margin restored exactly");
            Check(Find<ComboBox>(dialog, "cmbRotation").SelectedIndex == 3, "saved angle restored");
            Check(Find<Label>(dialog, "lblTitle").Text.StartsWith("INPUT"), "input role explicit");
            Check(Find<Button>(dialog, "btnSave").Enabled, "complete restored V2 draft can save");
            Check(Find<Label>(dialog, "lblStatus").Text.Contains("FINAL APPLY는 차단"), "restored quarter remains unavailable on machine");
            Set(dialog, "numEdgeMargin", .8m);
            Check(dialog.GeneratedMap == null && !Find<Button>(dialog, "btnSave").Enabled, "editing restored margin invalidates saved condition");
        }
        using (var dialog = Open(new WaferMapGenerationSettings(20m, 2m, 1m, .5m, .1m, .5m), false, restored, map => null))
        {
            Check(dialog.GeneratedMap == null && !Find<Button>(dialog, "btnSave").Enabled,
                "mismatched visible margin and stored map cannot be returned or saved");
            Check(Find<NumericUpDown>(dialog, "numEdgeMargin").Value == .5m, "explicit opening input is preserved on mismatch");
        }
    }

    private static void VerifyHitTest(WaferMapCreateDialog dialog)
    {
        var view = Find<WaferMapGenerationView>(dialog, "mapView");
        var layout = typeof(WaferMapGenerationView).GetField("_layout", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        double scale = Convert.ToDouble(layout.GetType().GetField("Scale").GetValue(layout));
        double cx = Convert.ToDouble(layout.GetType().GetField("CenterX").GetValue(layout));
        double cy = Convert.ToDouble(layout.GetType().GetField("CenterY").GetValue(layout));
        var die = dialog.GeneratedMap.Dies.OrderBy(d => Math.Abs(d.CenterXMm) + Math.Abs(d.CenterYMm)).First();
        typeof(WaferMapGenerationView).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view,
            new object[] { new MouseEventArgs(MouseButtons.Left, 1, (int)Math.Round(cx + (double)die.CenterXMm * scale),
                (int)Math.Round(cy - (double)die.CenterYMm * scale), 0) });
        Check(object.ReferenceEquals(view.SelectedDie, die), "rotated hit test selects the original address");
        Check(Find<Label>(dialog, "lblSelection").Text.Contains(die.CenterXMm.ToString("0.####")) &&
            Find<Label>(dialog, "lblSelection").Text.Contains(die.CenterYMm.ToString("0.####")),
            "selected physical coordinate keeps half-micrometer precision");
    }

    private static void VerifyPageEntry()
    {
        int initialLoads = QMC.CDT320.Recipes.RecipeStore.LoadCount;
        using (var host = new QMC.CDT_320.Form1 { Opacity = 0, ShowInTaskbar = false, ClientSize = new Size(1526, 764) })
        using (var page = new QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage())
        {
            page.Dock = DockStyle.Fill; host.Controls.Add(page); host.Show(); Application.DoEvents();
            var preview = Find<Button>(page, "btnPreviewWaferMap");
            Check(preview.Visible && preview.Bottom <= preview.Parent.ClientSize.Height, "preview entry fits parent row");
            Check(Find<NumericUpDown>(page, "_nGridX").ReadOnly && !Find<NumericUpDown>(page, "_nGridX").Enabled, "parent grid remains read-only");
            Check(!page.Controls.Find("btnGridMapCreate", true).Any(), "old grid creation route removed");
            Check(Find<NumericUpDown>(page, "_nDiameter").DecimalPlaces == 3, "parent diameter retains 0.001 mm precision");
            Set(page, "_nDiameter", 20m); Set(page, "_nDieSizeX", 2m); Set(page, "_nDieSizeY", 2m);
            Set(page, "_nPitchX", 0m); Set(page, "_nPitchY", 0m);
            var source = WaferMapGeneration.Generate(new WaferMapGenerationSettings(20m, 2m, 2m, 0m, 0m, 1m));
            page.StoredMap = GeneratedWaferMapCodec.ToDieMap(WaferMapGeneration.Rotate(source, 270), "ENTRY_RESTORE");
            Exception failure = null;
            using (var timer = new System.Windows.Forms.Timer { Interval = 25 })
            {
                timer.Tick += delegate
                {
                    var dialog = Application.OpenForms.OfType<WaferMapCreateDialog>().FirstOrDefault(); if (dialog == null) return; timer.Stop();
                    try
                    {
                        Check(dialog.GeneratedMap != null && dialog.GeneratedMap.Count == 52, "parent entry restores saved V2 geometry");
                        Check(Find<NumericUpDown>(dialog, "numEdgeMargin").Value == 1m, "parent entry retains saved margin instead of default0.2");
                        Check(Find<ComboBox>(dialog, "cmbRotation").SelectedIndex == 3, "parent entry restores angle");
                        SetCounts(dialog, 2, 2, 2, 2, 44); Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 44); Click(dialog, "btnSave");
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { dialog.Close(); }
                };
                timer.Start(); preview.PerformClick();
            }
            if (failure != null) throw failure;
            Check(host.SaveCalls == 1 && QMC.CDT320.Recipes.RecipeStore.LoadCount == initialLoads + 1, "parent uses guarded save callback and reloads persisted project");
            Check(Find<NumericUpDown>(page, "_nGridX").Value == 8 && Find<NumericUpDown>(page, "_nGridY").Value == 8, "saved grid displays actual used bounds");
            Check(Find<Label>(page, "lblSavedMapSummary").Text.Contains("44") && Find<Label>(page, "lblSavedMapSummary").Text.Contains("장비 사용 불가"), "parent saved count and quarter restriction shown");
            Capture(page, "dialog-entry.png");
            Set(page, "_nDiameter", 21m);
            using (var timer = new System.Windows.Forms.Timer { Interval = 25 })
            {
                timer.Tick += delegate
                {
                    var dialog = Application.OpenForms.OfType<WaferMapCreateDialog>().FirstOrDefault(); if (dialog == null) return; timer.Stop();
                    try
                    {
                        Check(dialog.GeneratedMap == null && Find<NumericUpDown>(dialog, "numDiameter").Value == 21m, "unsaved parent edits are not overwritten by restored map");
                        Check(Find<NumericUpDown>(dialog, "numEdgeMargin").Value == 1m, "saved margin remains the default after parent specification edit");
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { dialog.Close(); }
                };
                timer.Start(); preview.PerformClick();
            }
            if (failure != null) throw failure;
            host.Close();
        }
    }

    private static void VerifyOutsideStoragePageEntry()
    {
        foreach (bool outputRole in new[] { false, true })
        {
            string role = outputRole ? "OUTPUT" : "INPUT";
            using (var host = new QMC.CDT_320.Form1 { Opacity = 0, ShowInTaskbar = false, ClientSize = new Size(1526, 764) })
            using (var page = new QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage())
            {
                page.Dock = DockStyle.Fill; host.Controls.Add(page); host.Show(); Application.DoEvents();
                typeof(QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage)
                    .GetField("_currentRoleIsOutput", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(page, outputRole);
                Set(page, "_nDiameter", 287.640m); Set(page, "_nDieSizeX", 10.370m); Set(page, "_nDieSizeY", 7.913m);
                Set(page, "_nPitchX", .100m); Set(page, "_nPitchY", .100m);
                int initialLoads = QMC.CDT320.Recipes.RecipeStore.LoadCount;
                GeneratedWaferMap saved = null;
                RunPagePreview(page, dialog =>
                {
                    Check(Find<Label>(dialog, "lblTitle").Text.StartsWith(role), role + " parent opens correct storage editor");
                    Click(dialog, "btnGenerate"); AwaitMap(dialog, 713);
                    SetCounts(dialog, 3, 3, 8, 8, 730); Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 730);
                    saved = dialog.GeneratedMap;
                    Click(dialog, "btnSave");
                    CheckOutsideStorageNotice(dialog, 21, role + " first configuration save");
                    Check(!dialog.IsDisposed && dialog.HasSaved, role + " outside save completes while editor stays open");
                    Capture(dialog, "dialog-outside-saved-" + role.ToLowerInvariant() + ".png");
                });
                Check(host.SaveCalls == 1 && QMC.CDT320.Recipes.RecipeStore.LoadCount == initialLoads + 1,
                    role + " outside save follows parent callback and project reload");
                Check(page.StoredMap != null && page.StoredMap.Entries.Count == 730,
                    role + " stored map retains all 730 entries");
                string reason;
                Check(GeneratedWaferMapCodec.ValidateForStorage(page.StoredMap, out reason), role + " outside stored map validates for editing: " + reason);
                Check(GeneratedWaferMapCodec.Validate(page.StoredMap, out reason), role + " same outside map remains valid for equipment geometry checks");
                string parentStatus = (string)typeof(QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage)
                    .GetField("_lastWaferStatus", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(page);
                Check(!parentStatus.Contains("FINAL APPLY는 차단") && parentStatus.Contains("FINAL APPLY 하세요"),
                    role + " parent status offers the normal final-apply workflow for the saved outside map");
                Check(Find<Label>(page, "lblSavedMapSummary").Text.Contains("730"), role + " parent saved count includes outside dies");
                RunPagePreview(page, dialog =>
                {
                    AwaitMap(dialog, 730);
                    CheckMapEquivalent(dialog.GeneratedMap, saved, role + " close and parent reopen retains exact outside coordinates");
                    CheckCounts(dialog, 3, 3, 8, 8, 730, role + " reopened outside count requests");
                    CheckOutsideStorageNotice(dialog, 21, role + " reopened outside warning");
                    Check(Find<Button>(dialog, "btnSave").Enabled, role + " reopened complete outside map can save again");
                    Capture(dialog, "dialog-outside-reopened-" + role.ToLowerInvariant() + ".png");
                    var reopened = dialog.GeneratedMap;
                    Set(dialog, "numDiameter", 288m);
                    CheckPendingSpecification(dialog, reopened,
                        3, 3, 8, 8, 730, role + " reopened outside edited specification");
                    Check(!Find<Label>(dialog, "lblBoundaryStatus").Text.Contains("설정 저장 가능"),
                        role + " pending warning does not claim configuration can save");
                    Set(dialog, "numDiameter", 287.640m);
                    Click(dialog, "btnApplyEdges"); AwaitMap(dialog, 730);
                    CheckMapEquivalent(dialog.GeneratedMap, saved, role + " reapply restores exact saved outside map");
                    Click(dialog, "btnSave");
                    CheckOutsideStorageNotice(dialog, 21, role + " repeated outside configuration save");
                });
                Check(host.SaveCalls == 2 && QMC.CDT320.Recipes.RecipeStore.LoadCount == initialLoads + 2,
                    role + " reopened outside map saves once again through parent path");
                host.Close();
            }
        }
    }

    private static void RunPagePreview(Control page, Action<WaferMapCreateDialog> verify)
    {
        Exception failure = null;
        bool entered = false;
        using (var timer = new System.Windows.Forms.Timer { Interval = 25 })
        {
            timer.Tick += delegate
            {
                var dialog = Application.OpenForms.OfType<WaferMapCreateDialog>().FirstOrDefault();
                if (dialog == null) return;
                timer.Stop(); entered = true;
                try { verify(dialog); }
                catch (Exception ex) { failure = ex; }
                finally { dialog.Close(); }
            };
            timer.Start(); Find<Button>(page, "btnPreviewWaferMap").PerformClick();
        }
        if (failure != null) throw failure;
        Check(entered, "parent preview opened its modal dialog");
    }

    private static void CheckOutsideStorageNotice(WaferMapCreateDialog dialog, int outsideCount, string operation)
    {
        string boundary = Find<Label>(dialog, "lblBoundaryStatus").Text;
        string status = Find<Label>(dialog, "lblStatus").Text;
        Check(boundary.Contains("설정 저장 가능") && boundary.Contains("생성된 배치 유지") &&
            boundary.Contains(outsideCount.ToString("N0") + "개") && !boundary.Contains("장비 사용 차단"),
            operation + " retains the red count as information without a boundary-based equipment block");
        bool quarter = dialog.GeneratedMap.RotationDegrees == 90 || dialog.GeneratedMap.RotationDegrees == 270;
        Check(status.Contains("초과 " + outsideCount.ToString("N0")) &&
            status.Contains("FINAL APPLY는 차단") == quarter && (quarter || !status.Contains("장비 사용 차단")),
            operation + " only unsupported quarter turns retain the final-apply restriction");
        Check(dialog.GeneratedMap != null && dialog.GeneratedMap.OutOfBoundsCount == outsideCount &&
            object.ReferenceEquals(Find<WaferMapGenerationView>(dialog, "mapView").Map, dialog.GeneratedMap),
            operation + " preserves exact red preview after saving or restoring");
    }

    private static void VerifyEmptyAndClose()
    {
        using (var empty = Open(new WaferMapGenerationSettings(1m, 20m, 20m, 0m, 0m, .2m), false, null, map => null))
        {
            Click(empty, "btnGenerate"); AwaitMap(empty, 0);
            Check(Find<GroupBox>(empty, "groupEdges").Enabled && Find<NumericUpDown>(empty, "numTotalCount").Value == 1m, "empty V2 base still allows count preview");
            SetCounts(empty, 1, 1, 1, 1, 1); Click(empty, "btnApplyEdges"); AwaitMap(empty, 1);
            Check(empty.GeneratedMap.OutOfBoundsCount == 1 && Find<Button>(empty, "btnSave").Enabled, "oversized die from empty base permits configuration save");
            Click(empty, "btnSave");
            CheckOutsideStorageNotice(empty, 1, "oversized single-die configuration saved");
            Capture(empty, "dialog-empty-outside.png");
        }
        using (var zero = Open(new WaferMapGenerationSettings(200m, 0m, 0m, 0m, 0m)))
            Check(Find<NumericUpDown>(zero, "numDieX").Value == 0m, "unset die specification remains editable");
        using (var closing = Open(new WaferMapGenerationSettings(99.8m, .1m, .1m, 0m, 0m, .2m)))
        {
            Click(closing, "btnGenerate"); closing.Close();
            var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < 500) { Application.DoEvents(); Thread.Sleep(5); }
            Check(closing.IsDisposed, "close during worker generation is safe");
        }
    }

    private static T Find<T>(Control parent, string name) where T : Control
    {
        var found = parent.Controls.Find(name, true).OfType<T>().FirstOrDefault();
        if (found == null) throw new InvalidOperationException("Missing control: " + name);
        return found;
    }

    private static void Set(Control dialog, string name, decimal value) { Find<NumericUpDown>(dialog, name).Value = value; }
    private static void Click(Control dialog, string name)
    {
        var button = Find<Button>(dialog, name);
        if (!button.Enabled || !button.Visible) throw new InvalidOperationException("Unavailable action: " + name);
        button.PerformClick();
        Application.DoEvents();
    }
    private static void AwaitMap(WaferMapCreateDialog dialog, int count)
    {
        Wait(() => !dialog.IsBusy, "generation/adjustment result");
        Check(dialog.GeneratedMap != null, "expected generated result, status=" + Find<Label>(dialog, "lblStatus").Text);
        Check(dialog.GeneratedMap.Count == count, "expected map count " + count + ", actual=" + dialog.GeneratedMap.Count + ", status=" + Find<Label>(dialog, "lblStatus").Text);
        AwaitDrawing(dialog);
    }
    private static void AwaitDrawing(WaferMapCreateDialog dialog)
    {
        var view = Find<Control>(dialog, "mapView");
        var property = view.GetType().GetProperty("IsRendering");
        Wait(() => !(bool)property.GetValue(view, null), "map bitmap rendering");
    }
    private static void Wait(Func<bool> condition, string operation)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            Application.DoEvents();
            if (timer.ElapsedMilliseconds > 30000) throw new TimeoutException(operation);
            Thread.Sleep(5);
        }
        Application.DoEvents();
    }
    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("FAIL: " + message);
        _checks++;
    }
    private static void Capture(Control control, string name)
    {
        using (var bitmap = new Bitmap(control.Width, control.Height))
        {
            control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));
            bitmap.Save(Path.Combine(_output, name), ImageFormat.Png);
        }
    }
}

namespace QMC.Common
{
    // 예상 입력 오류는 실제 다이얼로그의 상태 라벨로 처리한다. 예상 밖 메시지박스는 테스트 실패로 수집한다.
    public static class MessageDialog
    {
        public static int UnexpectedCount;
        public static DialogResult Show(IWin32Window owner, string text, string caption,
            MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            UnexpectedCount++;
            Console.Error.WriteLine("Unexpected dialog: " + text);
            return DialogResult.OK;
        }
    }
    public static class Log
    {
        public static void Write(string category, string user, string operation, string text)
        {
            Console.Error.WriteLine(operation + ": " + text);
        }
    }
}
