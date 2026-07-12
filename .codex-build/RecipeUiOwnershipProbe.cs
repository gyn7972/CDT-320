using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using QMC.Common.Data.Store;
using QMC.CDT320.Recipes;

internal static class RecipeUiOwnershipProbe
{
    private static int _checks;

    [STAThread]
    private static int Main()
    {
        string dataRoot = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "recipe-ui-probe-data-" + Guid.NewGuid().ToString("N"));

        try
        {
            DataPaths.Root = dataRoot;
            DataPaths.EnsureRoot();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ProbeDieEditor();
            ProbeWaferEditor(false);
            ProbeWaferEditor(true);
            ProbeProjectReadOnlyAndLatestValuePreservation();

            Console.WriteLine("PASS: " + _checks + " UI ownership checks");
            Console.WriteLine("DataPaths.Root=" + DataPaths.Root);
            Console.WriteLine("RecipeStore.Dir=" + RecipeStore.Dir);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static void ProbeDieEditor()
    {
        const string projectName = "UI-PROBE-DIE";
        SaveProject(CreateProject(projectName, false));
        RecipeStore.SaveLastProjectName(projectName);

        using (Control page = CreatePage("QMC.CDT_320.Ui.Pages.Recipe.DieSubsetPage"))
        {
            AssertLoadedProject(page, projectName);
            AssertTextEditable(page, "_tbName", "Die spec name");
            AssertNumericEditable(page, "_nW", "Die width");
            AssertNumericEditable(page, "_nH", "Die height");
            AssertNumericEditable(page, "_nT", "Die thickness");
            AssertEnabled<ComboBox>(page, "_cbSpecLibrary", "Die spec library");
            AssertEnabled<Button>(page, "btnLoadSpec", "Die LOAD SPEC");
            AssertEnabled<Button>(page, "btnSaveSpec", "Die SAVE SPEC");
            AssertTopSaveEnabled(page, "Die top SAVE");

            RecipeProject latest = RecipeStore.Load(projectName);
            latest.Die.WidthMm = 3.141;
            SaveProject(latest);
            page.Visible = false;
            page.Visible = true;
            Application.DoEvents();
            Check(Field<NumericUpDown>(page, "_nW").Value == 3.141M,
                "Die page re-entry must reload latest disk Recipe");
        }

        Console.WriteLine("PASS: Die editor ownership");
    }

    private static void ProbeWaferEditor(bool baseConnected)
    {
        string projectName = baseConnected ? "UI-PROBE-WAFER-CONNECTED" : "UI-PROBE-WAFER-FREE";
        SaveProject(CreateProject(projectName, baseConnected));
        RecipeStore.SaveLastProjectName(projectName);

        using (Control page = CreatePage("QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage"))
        {
            AssertLoadedProject(page, projectName);
            AssertTextEditable(page, "_tbName", "Wafer spec name");
            AssertNumericEditable(page, "_nPitchX", "Wafer Pitch X");
            AssertNumericEditable(page, "_nPitchY", "Wafer Pitch Y");
            AssertNumericEditable(page, "_nDiameter", "Wafer diameter");
            AssertEnabled<ComboBox>(page, "_cbRotate", "Wafer rotate");
            AssertEnabled<ComboBox>(page, "_cbSpecLibrary", "Wafer spec library");
            AssertEnabled<Button>(page, "btnLoadSpec", "Wafer LOAD SPEC");
            AssertEnabled<Button>(page, "btnSaveSpec", "Wafer SAVE SPEC");
            AssertEnabled<Button>(page, "_btnImportWaferMap", "LOAD WAFER MAP");
            AssertTopSaveEnabled(page, "Wafer top SAVE");

            AssertNumericReadOnly(page, "_nDieSizeX", "Wafer page Die Size X");
            AssertNumericReadOnly(page, "_nDieSizeY", "Wafer page Die Size Y");

            NumericUpDown gridX = Field<NumericUpDown>(page, "_nGridX");
            NumericUpDown gridY = Field<NumericUpDown>(page, "_nGridY");
            ComboBox edgeMode = Field<ComboBox>(page, "_cbEdgeSkipMode");
            NumericUpDown edgeLr = Field<NumericUpDown>(page, "_nSideEdgeSkip");
            NumericUpDown edgeTb = Field<NumericUpDown>(page, "_nTopBottomEdgeSkip");

            if (baseConnected)
            {
                Check(!gridX.Enabled && gridX.ReadOnly, "Base-connected Grid X must be locked");
                Check(!gridY.Enabled && gridY.ReadOnly, "Base-connected Grid Y must be locked");
                Check(!edgeMode.Enabled, "Base-connected Edge mode must be locked");
                Check(!edgeLr.Enabled, "Base-connected Edge L/R must be locked");
                Check(!edgeTb.Enabled, "Base-connected Edge T/B must be locked");
            }
            else
            {
                Check(gridX.Enabled && !gridX.ReadOnly, "Base-free Grid X must be editable");
                Check(gridY.Enabled && !gridY.ReadOnly, "Base-free Grid Y must be editable");
                Check(edgeMode.Enabled, "Base-free Edge mode must be editable");
                Check(edgeLr.Enabled && !edgeLr.ReadOnly, "Base-free Edge L/R must be editable in Grid mode");
                Check(edgeTb.Enabled && !edgeTb.ReadOnly, "Base-free Edge T/B must be editable in Grid mode");
            }

            RecipeProject latest = RecipeStore.Load(projectName);
            latest.InputFrame.PitchX = 4.321;
            SaveProject(latest);
            page.Visible = false;
            page.Visible = true;
            Application.DoEvents();
            Check(Field<NumericUpDown>(page, "_nPitchX").Value == 4.321M,
                "Wafer page re-entry must reload latest disk Recipe");
        }

        Console.WriteLine("PASS: Wafer editor ownership (baseConnected=" + baseConnected + ")");
    }

    private static void ProbeProjectReadOnlyAndLatestValuePreservation()
    {
        const string projectName = "UI-PROBE-PROJECT";
        RecipeProject original = CreateProject(projectName, true);
        original.Die.DieSpecName = "INITIAL-DIE";
        SaveProject(original);
        RecipeStore.SaveLastProjectName(projectName);

        using (Control page = CreatePage("QMC.CDT_320.Ui.Pages.Recipe.ProjectPage"))
        {
            DataGridView projectGrid = Field<DataGridView>(page, "gridProject");
            string[] dieKeys = { "DieSpecName", "DieWidthMm", "DieHeightMm", "DieThicknessMm" };
            string[] pitchKeys = { "InputPitchX", "InputPitchY", "OutputPitchX", "OutputPitchY" };
            AssertReadOnlyRows(projectGrid, dieKeys, 2, "다이");
            AssertReadOnlyRows(projectGrid, pitchKeys, 2, "웨이퍼");

            DataGridView globalGrid = Field<DataGridView>(page, "gridGlobal");
            string[] frameKeys =
            {
                "ChipThickness",
                "InputFrameSpecName", "OutputFrameSpecName",
                "InputOuterDiameterMm", "OutputOuterDiameterMm",
                "InputRotate", "OutputRotate",
                "InputEdgeSkipMode", "OutputEdgeSkipMode",
                "InputSideEdgeSkip", "InputTopBottomEdgeSkip",
                "OutputSideEdgeSkip", "OutputTopBottomEdgeSkip",
                "InputSideEdgeSkipMm", "InputTopBottomEdgeSkipMm",
                "OutputSideEdgeSkipMm", "OutputTopBottomEdgeSkipMm"
            };
            AssertReadOnlyRows(globalGrid, frameKeys, 2, null);

            DataGridView mapGrid = Field<DataGridView>(page, "gridMap");
            AssertReadOnlyRows(mapGrid, new[] { "Base", "Input", "GoodBin", "NgBin" }, 2, null);

            RecipeProject latest = RecipeStore.Load(projectName);
            latest.Die.DieSpecName = "LATEST-EXTERNAL-DIE";
            latest.Die.WidthMm = 9.111;
            latest.Die.HeightMm = 8.222;
            latest.Die.ThicknessMm = 0.333;
            SetLatestFrame(latest.InputFrame, "LATEST-INPUT", 77, 88, 7.7, 8.8, 271.1, "Rotate90", "Grid", 3, 4, 0.31, 0.41);
            SetLatestFrame(latest.OutputFrame, "LATEST-OUTPUT", 99, 111, 9.9, 11.1, 299.2, "Rotate270", "Millimeter", 5, 6, 0.51, 0.61);
            latest.BaseWaferMapFileName = @"Recipes\UI-PROBE-PROJECT\Maps\latest-base.txt";
            latest.InputDieMapFileName = @"Recipes\UI-PROBE-PROJECT\Maps\latest-input.json";
            latest.GoodBinDieMapFileName = @"Recipes\UI-PROBE-PROJECT\Maps\latest-good.json";
            latest.NgBinDieMapFileName = @"Recipes\UI-PROBE-PROJECT\Maps\latest-ng.json";
            SaveProject(latest);

            RecipeProject collected = (RecipeProject)Invoke(page, "CollectFromUi");
            Check(collected.Die.DieSpecName == latest.Die.DieSpecName, "CollectFromUi must preserve latest Die name from disk");
            Check(Equal(collected.Die.WidthMm, latest.Die.WidthMm), "CollectFromUi must preserve latest Die width from disk");
            Check(Equal(collected.Die.HeightMm, latest.Die.HeightMm), "CollectFromUi must preserve latest Die height from disk");
            Check(Equal(collected.Die.ThicknessMm, latest.Die.ThicknessMm), "CollectFromUi must preserve latest Die thickness from disk");
            AssertFrameEqual(collected.InputFrame, latest.InputFrame, "InputFrame");
            AssertFrameEqual(collected.OutputFrame, latest.OutputFrame, "OutputFrame");
            Check(collected.BaseWaferMapFileName == latest.BaseWaferMapFileName, "CollectFromUi must preserve latest Base map path from disk");
            Check(collected.InputDieMapFileName == latest.InputDieMapFileName, "CollectFromUi must preserve latest Input map path from disk");
            Check(collected.GoodBinDieMapFileName == latest.GoodBinDieMapFileName, "CollectFromUi must preserve latest Good map path from disk");
            Check(collected.NgBinDieMapFileName == latest.NgBinDieMapFileName, "CollectFromUi must preserve latest NG map path from disk");
        }

        Console.WriteLine("PASS: Project read-only cells/tooltips and latest external-value preservation");
    }

    private static RecipeProject CreateProject(string name, bool baseConnected)
    {
        TapeFrameSubset input = new TapeFrameSubset
        {
            FrameSpecName = "INPUT-SPEC",
            DieMapX = 21,
            DieMapY = 22,
            PitchX = 1.2,
            PitchY = 1.3,
            DieSizeX = 1.01,
            DieSizeY = 1.02,
            OuterDiameterMm = 200,
            Rotate = "None",
            EdgeSkipMode = "Grid",
            SideEdgeSkip = 1,
            TopBottomEdgeSkip = 2
        };
        TapeFrameSubset output = new TapeFrameSubset
        {
            FrameSpecName = "OUTPUT-SPEC",
            DieMapX = 31,
            DieMapY = 32,
            PitchX = 2.2,
            PitchY = 2.3,
            DieSizeX = 1.01,
            DieSizeY = 1.02,
            OuterDiameterMm = 300,
            Rotate = "None",
            EdgeSkipMode = "Grid",
            SideEdgeSkip = 1,
            TopBottomEdgeSkip = 2
        };

        return new RecipeProject
        {
            FileName = name,
            Die = new DieSubset
            {
                DieSpecName = "PROBE-DIE",
                WidthMm = 1.01,
                HeightMm = 1.02,
                ThicknessMm = 0.15
            },
            Frame = input,
            InputFrame = input,
            OutputFrame = output,
            BaseWaferMapFileName = baseConnected ? @"Recipes\probe\Maps\base.txt" : "",
            InputDieMapFileName = @"Recipes\probe\Maps\input.json",
            GoodBinDieMapFileName = @"Recipes\probe\Maps\good.json",
            NgBinDieMapFileName = @"Recipes\probe\Maps\ng.json"
        };
    }

    private static void SetLatestFrame(
        TapeFrameSubset frame,
        string name,
        int gridX,
        int gridY,
        double pitchX,
        double pitchY,
        double diameter,
        string rotate,
        string edgeMode,
        int edgeLr,
        int edgeTb,
        double edgeLrMm,
        double edgeTbMm)
    {
        frame.FrameSpecName = name;
        frame.DieMapX = gridX;
        frame.DieMapY = gridY;
        frame.PitchX = pitchX;
        frame.PitchY = pitchY;
        frame.OuterDiameterMm = diameter;
        frame.Rotate = rotate;
        frame.EdgeSkipMode = edgeMode;
        frame.SideEdgeSkip = edgeLr;
        frame.TopBottomEdgeSkip = edgeTb;
        frame.SideEdgeSkipMm = edgeLrMm;
        frame.TopBottomEdgeSkipMm = edgeTbMm;
    }

    private static void AssertFrameEqual(TapeFrameSubset actual, TapeFrameSubset expected, string label)
    {
        Check(actual.FrameSpecName == expected.FrameSpecName, label + " spec name must be preserved");
        Check(actual.DieMapX == expected.DieMapX && actual.DieMapY == expected.DieMapY, label + " grid must be preserved");
        Check(Equal(actual.PitchX, expected.PitchX) && Equal(actual.PitchY, expected.PitchY), label + " pitch must be preserved");
        Check(Equal(actual.OuterDiameterMm, expected.OuterDiameterMm), label + " diameter must be preserved");
        Check(actual.Rotate == expected.Rotate, label + " rotate must be preserved");
        Check(actual.EdgeSkipMode == expected.EdgeSkipMode, label + " edge mode must be preserved");
        Check(actual.SideEdgeSkip == expected.SideEdgeSkip && actual.TopBottomEdgeSkip == expected.TopBottomEdgeSkip, label + " edge grid must be preserved");
        Check(Equal(actual.SideEdgeSkipMm, expected.SideEdgeSkipMm) && Equal(actual.TopBottomEdgeSkipMm, expected.TopBottomEdgeSkipMm), label + " edge mm must be preserved");
    }

    private static void SaveProject(RecipeProject project)
    {
        if (!RecipeStore.Save(project))
            throw new InvalidOperationException("Could not save isolated probe project " + project.FileName);
    }

    private static Control CreatePage(string typeName)
    {
        Type type = typeof(RecipeProject).Assembly.GetType(typeName, true);
        Control page = (Control)Activator.CreateInstance(type);
        page.CreateControl();
        return page;
    }

    private static void AssertLoadedProject(Control page, string projectName)
    {
        RecipeProject project = Field<RecipeProject>(page, "_project");
        Check(project != null, page.GetType().Name + " must load a project");
        Check(string.Equals(project.FileName, projectName, StringComparison.OrdinalIgnoreCase),
            page.GetType().Name + " loaded unexpected project: " + (project.FileName ?? "<null>"));
    }

    private static void AssertTextEditable(Control page, string fieldName, string label)
    {
        TextBox control = Field<TextBox>(page, fieldName);
        Check(control.Enabled && !control.ReadOnly, label + " must be editable");
    }

    private static void AssertNumericEditable(Control page, string fieldName, string label)
    {
        NumericUpDown control = Field<NumericUpDown>(page, fieldName);
        Check(control.Enabled && !control.ReadOnly, label + " must be editable");
    }

    private static void AssertNumericReadOnly(Control page, string fieldName, string label)
    {
        NumericUpDown control = Field<NumericUpDown>(page, fieldName);
        Check(control.ReadOnly, label + " must be read-only");
    }

    private static void AssertEnabled<T>(Control page, string fieldName, string label) where T : Control
    {
        T control = Field<T>(page, fieldName);
        Check(control.Enabled, label + " must be enabled");
    }

    private static void AssertTopSaveEnabled(Control page, string label)
    {
        Button button = Field<Button>(page, "_btnSave");
        Check(button.Enabled, label + " must be enabled");
        Check(string.Equals(button.Text, "SAVE", StringComparison.OrdinalIgnoreCase), label + " text must be SAVE");
    }

    private static void AssertReadOnlyRows(DataGridView grid, IEnumerable<string> keys, int valueColumn, string expectedToolTipPart)
    {
        foreach (string key in keys)
        {
            DataGridViewRow row = FindRow(grid, key);
            DataGridViewCell value = row.Cells[valueColumn];
            Check(value.ReadOnly, grid.Name + "/" + key + " value must be read-only");
            Check(!string.IsNullOrWhiteSpace(value.ToolTipText), grid.Name + "/" + key + " value must have location tooltip");
            Check(!string.IsNullOrWhiteSpace(row.Cells[1].ToolTipText), grid.Name + "/" + key + " label must have location tooltip");
            if (!string.IsNullOrWhiteSpace(expectedToolTipPart))
                Check(value.ToolTipText.IndexOf(expectedToolTipPart, StringComparison.OrdinalIgnoreCase) >= 0,
                    grid.Name + "/" + key + " tooltip must point to " + expectedToolTipPart + ": " + value.ToolTipText);
        }
    }

    private static DataGridViewRow FindRow(DataGridView grid, string key)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            string candidate = row.Cells[0].Value == null ? "" : row.Cells[0].Value.ToString();
            if (string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase))
                return row;
        }

        throw new InvalidOperationException("Missing row " + grid.Name + "/" + key);
    }

    private static T Field<T>(object target, string name)
    {
        Type type = target.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null)
                return (T)field.GetValue(target);
            type = type.BaseType;
        }

        throw new MissingFieldException(target.GetType().FullName, name);
    }

    private static object Invoke(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(target.GetType().FullName, methodName);
        return method.Invoke(target, null);
    }

    private static bool Equal(double left, double right)
    {
        return Math.Abs(left - right) < 0.0000001;
    }

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
