using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Pages.Recipe;
using QMC.CDT_320.Ui.Pages.Work;
using QMC.Common.Data.Store;

internal static class PitchGapPhysicalMapProbe
{
    private static int _checks;

    private static void Assert(bool condition, string message)
    {
        _checks++;
        if (!condition)
            throw new InvalidOperationException("ASSERT FAILED: " + message);
    }

    private static void Near(double actual, double expected, string message)
    {
        Assert(Math.Abs(actual - expected) <= 0.000001,
            message + " actual=" + actual.ToString("0.######", CultureInfo.InvariantCulture) +
            " expected=" + expected.ToString("0.######", CultureInfo.InvariantCulture));
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = FindField(target.GetType(), name);
        Assert(field != null, target.GetType().Name + "." + name + " field exists");
        field.SetValue(target, value);
    }

    private static FieldInfo FindField(Type type, string name)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field;
            type = type.BaseType;
        }
        return null;
    }

    private static bool ValidateMapCreate(MapCreatePage page, RecipeProject project, DieMap map, out string reason)
    {
        SetField(page, "_project", project);
        SetField(page, "_map", map);
        MethodInfo method = typeof(MapCreatePage).GetMethod("ValidateCurrentMapForApply",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(method != null, "MapCreate validation method exists");
        object[] args = { null };
        bool valid = (bool)method.Invoke(page, args);
        reason = args[0] as string ?? "";
        return valid;
    }

    private static DieMap CloneWithStep(DieMap source, double stepX, double stepY)
    {
        string path = Path.Combine(DataPaths.Root, "bad-step.json");
        DieMapGenerator.SaveJson(source, path);
        DieMap clone = DieMapGenerator.LoadJson(path);
        clone.PitchX = stepX;
        clone.PitchY = stepY;
        foreach (DieMapEntry entry in clone.Entries.Where(item => item != null))
        {
            entry.PosX = entry.EquipmentGridX * stepX;
            entry.PosY = entry.EquipmentGridY * stepY;
        }
        return DieMapGenerator.Normalize(clone);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PROBE_FAILED type=" + ex.GetType().FullName);
            try { Console.Error.WriteLine("message=" + ex.Message); } catch { }
            Exception inner = ex.InnerException;
            int depth = 0;
            while (inner != null && depth++ < 8)
            {
                Console.Error.WriteLine("innerType=" + inner.GetType().FullName);
                try { Console.Error.WriteLine("innerMessage=" + inner.Message); } catch { }
                inner = inner.InnerException;
            }
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        string radPath = args.Length > 0 ? args[0] : @"D:\CDT-320\Config\WaferMap\RAD1.txt";
        string root = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "cdt320-gap-probe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        DataPaths.Root = root;

        Near(DieMapGenerator.CalculateCenterStep(8.12, 0.0), 8.12, "zero X gap");
        Near(DieMapGenerator.CalculateCenterStep(6.12, 0.3), 6.42, "positive Y gap");
        Near(DieMapGenerator.CalculatePitchGap(8.12, 8.12), 0.0, "source step to zero gap");

        var project = new RecipeProject { FileName = "PitchGapPhysicalProbe" };
        RecipeProjectConsistencyService.EnsureStructure(project);
        project.Die.DieSpecName = "PITCH-GAP-PROBE-DIE";
        project.Die.WidthMm = 8.12;
        project.Die.HeightMm = 6.12;
        project.Die.ThicknessMm = 0.25;
        project.InputFrame.PitchX = 0.0;
        project.InputFrame.PitchY = 0.0;
        project.InputFrame.OuterDiameterMm = 287.64;
        project.OutputFrame.PitchX = 0.3;
        project.OutputFrame.PitchY = 0.3;
        project.OutputFrame.OuterDiameterMm = 287.64;

        RecipeMapBuildResult built = RecipeMapBuildService.ImportBaseAndBuildAll(project, radPath);
        Assert(built.Success, "RAD import/build succeeds: " + built.Message);
        DieMap input = DieMapGenerator.LoadJson(built.InputMapPath);
        DieMap good = DieMapGenerator.LoadJson(built.GoodMapPath);
        DieMap ng = DieMapGenerator.LoadJson(built.NgMapPath);
        Assert(input != null && good != null && ng != null, "all role maps load");
        Assert(input.DieMapX == 35 && input.DieMapY == 47 && input.Entries.Count == 1257,
            "input keeps RAD domain");
        Near(project.InputFrame.PitchX, 0.0, "Input frame stores X gap");
        Near(project.OutputFrame.PitchX, 0.3, "Output frame stores X gap");
        Near(input.PitchX, 8.12, "Input map stores center step X");
        Near(input.PitchY, 6.12, "Input map stores center step Y");
        Near(good.PitchX, 8.42, "Good map stores center step X");
        Near(good.PitchY, 6.42, "Good map stores center step Y");
        Near(ng.PitchX, 8.42, "NG map stores center step X");
        Near(input.DieSizeX, 8.12, "Input die width metadata");
        Near(input.DieSizeY, 6.12, "Input die height metadata");
        Near(input.OuterDiameterMm, 287.64, "Input wafer diameter metadata");

        DieMapEntry first = input.Entries.First(item => item != null);
        Near(first.PosX, first.EquipmentGridX * input.PitchX, "Input X uses equipment grid and step");
        Near(first.PosY, first.EquipmentGridY * input.PitchY, "Input Y uses equipment grid and step");

        string mismatch;
        Assert(RecipeDieMapResolver.IsCompatibleWithFrame(input, project.InputFrame, out mismatch),
            "Input map compatible with zero-gap frame: " + mismatch);
        Assert(RecipeDieMapResolver.IsCompatibleWithFrame(good, project.OutputFrame, out mismatch),
            "Good map compatible with positive-gap frame: " + mismatch);

        // 0을 0.001로 저장했던 기존 UI 데이터와 old role-map step 0.001 조합은
        // Base RAD center step에서 Gap 0으로 안전하게 복구되어야 한다.
        DieMap legacyMinimumMap = CloneWithStep(input, 0.001, 0.001);
        DieMapGenerator.SaveJson(legacyMinimumMap, built.InputMapPath);
        project.InputFrame.PitchX = 0.001;
        project.InputFrame.PitchY = 0.001;
        RecipeMapBuildResult legacyRecovery = RecipeMapBuildService.RebuildDerivedMaps(
            project, true, false, true);
        Assert(legacyRecovery.Success, "legacy 0.001 recovery rebuild succeeds: " + legacyRecovery.Message);
        input = DieMapGenerator.LoadJson(legacyRecovery.InputMapPath);
        Near(project.InputFrame.PitchX, 0.0, "legacy Input X clamp recovers to gap zero");
        Near(project.InputFrame.PitchY, 0.0, "legacy Input Y clamp recovers to gap zero");
        Near(input.PitchX, 8.12, "legacy Input X role map rebuilt with center step");
        Near(input.PitchY, 6.12, "legacy Input Y role map rebuilt with center step");

        RecipeMapPaths.ApproveMap(project, RecipeMapKind.Input, input);
        RecipeMapPaths.ApproveMap(project, RecipeMapKind.GoodBin, good);
        RecipeMapPaths.ApproveMap(project, RecipeMapKind.NgBin, ng);

        using (var inputPage = new MapCreatePage("recipe.inputMapCreate"))
        {
            var gapControl = (NumericUpDown)FindField(typeof(MapCreatePage), "_nPitchX").GetValue(inputPage);
            var gapLabel = (Label)FindField(typeof(MapCreatePage), "lblChipPitchYKey").GetValue(inputPage);
            Near((double)gapControl.Minimum, 0.0, "MapCreate Pitch Gap minimum is zero");
            Assert(gapLabel.Text.IndexOf("GAP", StringComparison.OrdinalIgnoreCase) >= 0,
                "MapCreate label identifies Pitch Gap");
            string reason;
            Assert(ValidateMapCreate(inputPage, project, input, out reason),
                "Input MapCreate accepts Die+Gap step: " + reason);
            DieMap bad = CloneWithStep(input, 0.001, 0.001);
            Assert(!ValidateMapCreate(inputPage, project, bad, out reason),
                "MapCreate blocks legacy 0.001 center step");
            Assert(reason.IndexOf("Die Size + Pitch Gap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   reason.IndexOf("중심 간격", StringComparison.OrdinalIgnoreCase) >= 0,
                "bad-step reason is explicit: " + reason);
        }

        using (var framePage = new TapeFrameSubsetPage())
        {
            var gapControl = (NumericUpDown)FindField(typeof(TapeFrameSubsetPage), "_nPitchX").GetValue(framePage);
            var gapLabel = (Label)FindField(typeof(TapeFrameSubsetPage), "lblPitchX").GetValue(framePage);
            Near((double)gapControl.Minimum, 0.0, "Wafer spec Pitch Gap minimum is zero");
            Assert(gapLabel.Text.IndexOf("Gap", StringComparison.OrdinalIgnoreCase) >= 0,
                "Wafer spec label identifies Pitch Gap");
        }

        using (var outputPage = new MapCreatePage("recipe.outputMapCreate"))
        {
            string reason;
            Assert(ValidateMapCreate(outputPage, project, good, out reason),
                "Output MapCreate accepts Die+Gap step: " + reason);
        }

        MethodInfo basePreviewMethod = typeof(InputStageMapTransferPage).GetMethod(
            "LoadBaseWaferMapPreview", BindingFlags.Static | BindingFlags.NonPublic);
        Assert(basePreviewMethod != null, "Input transfer base-preview fallback exists");
        DieMap basePreview = (DieMap)basePreviewMethod.Invoke(null, new object[] { project });
        Assert(basePreview != null && basePreview.Entries.Count == 1257,
            "Input transfer can show Base preview when active map is unavailable");

        Console.WriteLine("PITCH_GAP_PHYSICAL_MAP_PROBE_PASS checks=" + _checks + " root=" + root);
        return 0;
    }
}
