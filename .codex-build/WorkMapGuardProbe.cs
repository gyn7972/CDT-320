using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Pages.Work;
using QMC.Common.Data.Store;

internal static class WorkMapGuardProbe
{
    private static int _checks;

    private static void Assert(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException("ASSERT FAILED: " + message);
    }

    private static object InvokePrivate(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(method != null, target.GetType().Name + "." + name + " exists");
        return method.Invoke(target, args);
    }

    private static object InvokePrivateStatic(Type type, string name, params object[] args)
    {
        MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert(method != null, type.Name + "." + name + " exists");
        return method.Invoke(null, args);
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert(start >= 0, "source method found: " + signature);
        int open = source.IndexOf('{', start);
        Assert(open >= 0, "source method opening brace: " + signature);
        int depth = 0;
        bool inString = false;
        bool verbatim = false;
        bool escaped = false;
        for (int i = open; i < source.Length; i++)
        {
            char c = source[i];
            if (inString)
            {
                if (verbatim)
                {
                    if (c == '"')
                    {
                        if (i + 1 < source.Length && source[i + 1] == '"') { i++; continue; }
                        inString = false;
                    }
                }
                else if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '@' && i + 1 < source.Length && source[i + 1] == '"')
            {
                inString = true;
                verbatim = true;
                i++;
                continue;
            }
            if (c == '"')
            {
                inString = true;
                verbatim = false;
                continue;
            }
            if (c == '{') depth++;
            if (c == '}' && --depth == 0)
                return source.Substring(start, i - start + 1);
        }
        throw new InvalidOperationException("Method end not found: " + signature);
    }

    private static DieMap BuildApprovedMap(bool target = true)
    {
        var map = new DieMap
        {
            FrameObjId = "APPROVED-INPUT-MAP",
            DieMapX = 3,
            DieMapY = 3,
            PitchX = 2.0,
            PitchY = 3.0,
            DieSizeX = 1.0,
            DieSizeY = 1.0,
            OriginX = -2.0,
            OriginY = 3.0,
            EdgeSkipMode = "ExternalMap"
        };
        int index = 0;
        for (int row = 0; row < 3; row++)
        for (int col = 0; col < 3; col++)
        {
            double gx = col - 1.0;
            double gy = 1.0 - row;
            map.Entries.Add(new DieMapEntry
            {
                Index = index++,
                DieMapX = col,
                DieMapY = row,
                OriginalMapX = 166 + col,
                OriginalMapY = 183 - row,
                EquipmentGridX = gx,
                EquipmentGridY = gy,
                PosX = gx * 2.0,
                PosY = gy * 3.0,
                IsTarget = target,
                BinCode = target ? 1 : 0
            });
        }
        return DieMapGenerator.Normalize(map);
    }

    private static DieMap CloneAsMachineAbsolute(DieMap approved, double centerX, double centerY)
    {
        var mapped = new DieMap
        {
            FrameObjId = "WAFER-MAPPED-ABSOLUTE",
            DieMapX = approved.DieMapX,
            DieMapY = approved.DieMapY,
            PitchX = approved.PitchX,
            PitchY = approved.PitchY,
            DieSizeX = approved.DieSizeX,
            DieSizeY = approved.DieSizeY,
            OriginX = centerX + approved.OriginX,
            OriginY = centerY + approved.OriginY,
            EdgeSkipMode = approved.EdgeSkipMode
        };
        foreach (DieMapEntry entry in approved.Entries)
        {
            mapped.Entries.Add(new DieMapEntry
            {
                Index = entry.Index,
                DieMapX = entry.DieMapX,
                DieMapY = entry.DieMapY,
                OriginalMapX = entry.OriginalMapX,
                OriginalMapY = entry.OriginalMapY,
                EquipmentGridX = entry.EquipmentGridX,
                EquipmentGridY = entry.EquipmentGridY,
                PosX = centerX + entry.PosX,
                PosY = centerY + entry.PosY,
                IsTarget = entry.IsTarget,
                BinCode = entry.BinCode
            });
        }
        return DieMapGenerator.Normalize(mapped);
    }

    private static void AssertYAxis(DieMap map, string label)
    {
        Assert(map != null && map.Entries != null && map.Entries.Count > 0, label + " map exists");
        DieMapGenerator.Normalize(map);
        int minRow = map.Entries.Min(entry => entry.DieMapY);
        int maxRow = map.Entries.Max(entry => entry.DieMapY);
        double topY = map.Entries.Where(entry => entry.DieMapY == minRow).Average(entry => entry.PosY);
        double bottomY = map.Entries.Where(entry => entry.DieMapY == maxRow).Average(entry => entry.PosY);
        double topGridY = map.Entries.Where(entry => entry.DieMapY == minRow).Average(entry => entry.EquipmentGridY);
        double bottomGridY = map.Entries.Where(entry => entry.DieMapY == maxRow).Average(entry => entry.EquipmentGridY);
        Assert(topY > 0.0, label + " top PosY is positive");
        Assert(bottomY < 0.0, label + " bottom PosY is negative");
        Assert(topY > bottomY, label + " PosY decreases as screen row increases");
        Assert(topGridY > 0.0 && bottomGridY < 0.0, label + " equipment +Y is up");
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string repo = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            string inputSourcePath = Path.Combine(repo, "QMC.CDT-320", "Ui", "Pages", "Work", "InputStageMapTransferPage.cs");
            string outputSourcePath = Path.Combine(repo, "QMC.CDT-320", "Ui", "Pages", "Work", "OutputStageMapTransferPage.cs");
            string applySourcePath = Path.Combine(repo, "QMC.CDT-320", "Sequencing", "InputStage", "InputStageDieMapApplyService.cs");
            string controllerSourcePath = Path.Combine(repo, "QMC.CDT-320", "Equipment", "MachineController.cs");
            string inputSource = File.ReadAllText(inputSourcePath);
            string outputSource = File.ReadAllText(outputSourcePath);
            string applySource = File.ReadAllText(applySourcePath);
            string controllerSource = File.ReadAllText(controllerSourcePath);

            // Every UI route that can persist state or command motion must enter the common machine-map guard.
            string[] guardedInputMethods =
            {
                "private void MoveSelectedDieDataToPicker(",
                "private void ShowInputPickerOffsetSetupDialog(",
                "private void SavePickStatus()",
                "private async Task RunManualInputDieDetectAsync()",
                "private void ApplyPendingManualInputDieMapOffset()",
                "private async Task MoveSelectedDieAsync()",
                "private async Task MoveSelectedDieByPickerAsync(",
                "private void ShowPickUpTestDialogForSelectedInputDie(",
                "private void ApplySelectedDieState()"
            };
            foreach (string signature in guardedInputMethods)
            {
                string body = ExtractMethod(inputSource, signature);
                Assert(body.Contains("EnsureCurrentInputMapMotionReady("), signature + " enters machine-map guard");
            }
            string inputGuardBody = ExtractMethod(inputSource, "private bool EnsureCurrentInputMapMotionReady(");
            Assert(inputGuardBody
                .Contains("!_mapPositionsAreMachineAbsolute || wafer == null || !wafer.HasInputStageDieMappingResult"),
                "guard rejects preview/no wafer/no mapping result");
            Assert(inputGuardBody.Contains("wafer.InputMapApprovalHashAtMapping") &&
                   inputGuardBody.Contains("project.InputMapApprovalHash"),
                "guard rejects stale mapping after approval hash changes");
            Assert(inputSource.Contains("RECIPE INPUT DIE MAP (CENTER-RELATIVE PREVIEW)\", false"),
                "Recipe input map is explicitly preview-only");
            Assert(inputSource.Contains("ACTIVE INPUT DIE MAP (MACHINE ABSOLUTE)\", true"),
                "mapped input map is explicitly machine-absolute");
            Assert(inputSource.Contains("var map = managed ? mappedWaferMap"),
                "managed Input does not use Active/Saved fallback");
            Assert(inputSource.Contains("stageWafer.InputMapApprovalHashAtMapping") &&
                   inputSource.Contains("activeProject.InputMapApprovalHash"),
                "managed Input load/refresh requires mapping-time approval snapshot");
            Assert(applySource.Contains("wafer.InputMapApprovalHashAtMapping = inputMapApprovalHash ?? \"\";"),
                "mapping apply stores approval hash snapshot on WaferMaterial");
            string legacyGuardBody = ExtractMethod(controllerSource, "private bool TryPrepareManagedLegacyInputMap(");
            Assert(legacyGuardBody.Contains("wafer.InputMapApprovalHashAtMapping") &&
                   legacyGuardBody.Contains("project.InputMapApprovalHash") &&
                   legacyGuardBody.Contains("RecipeDieMapResolver.IsMappedInputCompatibleWithRecipe(mapped, approved"),
                "legacy cycle requires current approval snapshot and compatible absolute map");
            Assert(ExtractMethod(controllerSource, "public async Task CycleRunAsync(")
                .Contains("TryPrepareManagedLegacyInputMap(out managedMapReason)"),
                "legacy CycleRun enters managed input-map guard before running");

            // Managed Output must never substitute material/circle maps when resolver approval fails.
            string loadRecipeBinMapBody = ExtractMethod(outputSource, "private DieMap LoadRecipeBinMap(");
            Assert(loadRecipeBinMapBody.Contains("if (project.MapApprovalVersion > 0)"),
                "managed Output approval branch exists");
            Assert(loadRecipeBinMapBody.IndexOf("return null;", loadRecipeBinMapBody.IndexOf("if (project.MapApprovalVersion > 0)", StringComparison.Ordinal), StringComparison.Ordinal) >= 0,
                "managed Output resolver failure returns null");
            string reloadOutputBody = ExtractMethod(outputSource, "private void ReloadOutputMap()"
            );
            Assert(reloadOutputBody.Contains("? recipeMap") && reloadOutputBody.Contains(": (materialMap ?? recipeMap)"),
                "managed Output uses approved recipe map, not material fallback");

            Application.EnableVisualStyles();
            using (var inputPage = new InputStageMapTransferPage())
            using (var outputPage = new OutputStageMapTransferPage())
            {
                var recipe = new RecipeProject { FileName = "WORK-GUARD-PROBE" };
                RecipeProjectConsistencyService.EnsureStructure(recipe);
                recipe.Frame.DieMapX = 3;
                recipe.Frame.DieMapY = 3;
                recipe.Frame.PitchX = 2.0;
                recipe.Frame.PitchY = 3.0;
                recipe.Frame.OuterDiameterMm = 100.0;
                recipe.InputFrame.DieMapX = 3;
                recipe.InputFrame.DieMapY = 3;
                recipe.InputFrame.PitchX = 2.0;
                recipe.InputFrame.PitchY = 3.0;
                recipe.InputFrame.OuterDiameterMm = 100.0;
                recipe.OutputFrame.DieMapX = 3;
                recipe.OutputFrame.DieMapY = 3;
                recipe.OutputFrame.PitchX = 2.0;
                recipe.OutputFrame.PitchY = 3.0;
                recipe.OutputFrame.OuterDiameterMm = 100.0;

                var inputCircle = (DieMap)InvokePrivate(inputPage, "CreateInputCircleMapFromRecipe", recipe);
                var outputCircle = (DieMap)InvokePrivate(outputPage, "CreateOutputCircleMapFromRecipe", recipe, BinSide.Good);
                AssertYAxis(inputCircle, "Input circle preview");
                AssertYAxis(outputCircle, "Output circle preview");

                FieldInfo absoluteField = typeof(InputStageMapTransferPage).GetField(
                    "_mapPositionsAreMachineAbsolute", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(absoluteField != null, "Input coordinate-space state field exists");
                InvokePrivate(inputPage, "ApplyMap", inputCircle, "PREVIEW", false);
                Assert(!(bool)absoluteField.GetValue(inputPage), "preview ApplyMap is not machine absolute");
                InvokePrivate(inputPage, "ApplyMap", inputCircle, "MAPPED", true);
                Assert((bool)absoluteField.GetValue(inputPage), "mapped ApplyMap is machine absolute");

                DieMap approved = BuildApprovedMap();
                DieMap mapped = CloneAsMachineAbsolute(approved, 500.0, 700.0);
                bool compatible = (bool)InvokePrivateStatic(
                    typeof(InputStageMapTransferPage),
                    "IsMappedMapCompatibleWithApprovedRecipeMap",
                    mapped,
                    approved);
                Assert(compatible, "absolute mapped map matches approved raw/local/pitch domain");

                mapped.Entries[0].OriginalMapX += 100;
                compatible = (bool)InvokePrivateStatic(
                    typeof(InputStageMapTransferPage),
                    "IsMappedMapCompatibleWithApprovedRecipeMap",
                    mapped,
                    approved);
                Assert(!compatible, "raw address mismatch is blocked");
                mapped = CloneAsMachineAbsolute(approved, 500.0, 700.0);
                mapped.PitchY += 0.01;
                compatible = (bool)InvokePrivateStatic(
                    typeof(InputStageMapTransferPage),
                    "IsMappedMapCompatibleWithApprovedRecipeMap",
                    mapped,
                    approved);
                Assert(!compatible, "pitch mismatch is blocked");

                // Mapping apply stamps the exact approval hash and the stamp survives material-state serialization.
                var wafer = new WaferMaterial { WaferId = "WG-WAFER" };
                Type applyServiceType = typeof(RecipeDieMapResolver).Assembly.GetType(
                    "QMC.CDT320.Sequencing.InputStageDieMapApplyService", true);
                MethodInfo applyWaferResult = applyServiceType.GetMethod(
                    "ApplyWaferDieMapResult", BindingFlags.Static | BindingFlags.NonPublic);
                Assert(applyWaferResult != null, "mapping approval snapshot method exists");
                applyWaferResult.Invoke(null, new object[] { null, wafer, approved, 1.25, -2.5, "HASH-A" });
                Assert(wafer.HasInputStageDieMappingResult, "mapping result flag stamped");
                Assert(wafer.InputMapApprovalHashAtMapping == "HASH-A", "mapping approval hash stamped");
                using (var stream = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(WaferMaterial));
                    serializer.WriteObject(stream, wafer);
                    stream.Position = 0;
                    var loadedWafer = (WaferMaterial)serializer.ReadObject(stream);
                    Assert(loadedWafer.InputMapApprovalHashAtMapping == "HASH-A",
                        "mapping approval hash survives Material JSON round-trip");
                }
                Assert(!string.Equals(wafer.InputMapApprovalHashAtMapping, "HASH-B", StringComparison.OrdinalIgnoreCase),
                    "re-FINAL-APPLY hash differs from stale wafer mapping snapshot");

                // Invoke the actual Output loader: managed pending project returns null; legacy still gets a preview circle.
                string isolatedRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work-guard-data-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(isolatedRoot);
                DataPaths.Root = isolatedRoot;
                recipe.FileName = "WG-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                recipe.MapApprovalVersion = 1;
                recipe.GoodBinDieMapFileName = "";
                recipe.GoodBinMapApprovalHash = "";
                Assert(RecipeStore.Save(recipe), "managed pending probe Recipe saved");
                RecipeStore.SaveLastProjectName(recipe.FileName);
                var managedResult = (DieMap)InvokePrivate(outputPage, "LoadRecipeBinMap", BinSide.Good);
                Assert(managedResult == null, "managed Output does not circle-fallback when FINAL APPLY is pending");

                recipe.MapApprovalVersion = 0;
                Assert(RecipeStore.Save(recipe), "legacy probe Recipe saved");
                var legacyResult = (DieMap)InvokePrivate(outputPage, "LoadRecipeBinMap", BinSide.Good);
                Assert(legacyResult != null && legacyResult.Entries.Count > 0,
                    "legacy Output may still create a non-motion preview circle");
            }

            Console.WriteLine("PASS checks=" + _checks);
            return 0;
        }
        catch (TargetInvocationException ex)
        {
            Console.Error.WriteLine(ex.InnerException ?? ex);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
