using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using QMC.Common.Data.Store;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.CDT_320;

internal static class FinalRecipeUiProbe
{
    private static int _checks;
    private static string _artifactDir;

    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _artifactDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "final-ui-probe-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_artifactDir);
            DataPaths.Root = Path.Combine(_artifactDir, "Data");
            DataPaths.EnsureRoot();

            RecipeProject active = BuildProject("ACTIVE-RECIPE", 8.12, 6.12, 8.22, 6.22);
            RecipeProject last = BuildProject("LAST-BUT-NOT-ACTIVE", 1.11, 1.22, 1.33, 1.44);
            Check(RecipeStore.Save(active), "active project seed save");
            Check(RecipeStore.Save(last), "last project seed save");
            RecipeStore.SaveLastProjectName(last.FileName);

            using (var host = new Form1())
            {
                SetCurrentRecipeName(host, active.FileName);
                ProbeDie(host, active.FileName);
                ProbeWafer(host, active.FileName);
                ProbeMapCreate(host, active.FileName, false);
                ProbeMapCreate(host, active.FileName, true);
            }

            Console.WriteLine("PASS: " + _checks + " final Recipe UI checks");
            Console.WriteLine("ARTIFACTS=" + _artifactDir);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            if (!string.IsNullOrWhiteSpace(_artifactDir))
                Console.Error.WriteLine("ARTIFACTS=" + _artifactDir);
            return 1;
        }
    }

    private static RecipeProject BuildProject(string name, double inputPitchX, double inputPitchY, double outputPitchX, double outputPitchY)
    {
        string mapDir = Path.Combine(_artifactDir, "Maps", name);
        Directory.CreateDirectory(mapDir);
        string basePath = Path.Combine(mapDir, "RAD1_Base.json");
        string inputPath = Path.Combine(mapDir, "Input.json");
        string goodPath = Path.Combine(mapDir, "Good.json");
        string ngPath = Path.Combine(mapDir, "Ng.json");

        DieMapGenerator.Save(BuildMap("BASE", 8.12, 6.12), basePath);
        DieMapGenerator.Save(BuildMap("INPUT", inputPitchX, inputPitchY), inputPath);
        DieMapGenerator.Save(BuildMap("GOOD", outputPitchX, outputPitchY), goodPath);
        DieMapGenerator.Save(BuildMap("NG", outputPitchX, outputPitchY), ngPath);

        var inputFrame = new TapeFrameSubset
        {
            FrameSpecName = name + "_InputWafer",
            DieMapX = 3,
            DieMapY = 3,
            PitchX = inputPitchX,
            PitchY = inputPitchY,
            DieSizeX = 8.0,
            DieSizeY = 6.0,
            OuterDiameterMm = 287.6,
            Rotate = "None",
            EdgeSkipMode = "ExternalMap"
        };
        var outputFrame = new TapeFrameSubset
        {
            FrameSpecName = name + "_OutputWafer",
            DieMapX = 3,
            DieMapY = 3,
            PitchX = outputPitchX,
            PitchY = outputPitchY,
            DieSizeX = 8.0,
            DieSizeY = 6.0,
            OuterDiameterMm = 287.6,
            Rotate = "None",
            EdgeSkipMode = "ExternalMap"
        };

        return new RecipeProject
        {
            FileName = name,
            Die = new DieSubset { DieSpecName = name + "_Die", WidthMm = 8.0, HeightMm = 6.0, ThicknessMm = 0.25 },
            Frame = inputFrame,
            InputFrame = inputFrame,
            OutputFrame = outputFrame,
            BaseWaferMapFileName = basePath,
            InputDieMapFileName = inputPath,
            GoodBinDieMapFileName = goodPath,
            NgBinDieMapFileName = ngPath,
            MapApprovalVersion = 1,
            InputMapApprovalHash = "",
            GoodBinMapApprovalHash = "",
            NgBinMapApprovalHash = ""
        };
    }

    private static DieMap BuildMap(string id, double pitchX, double pitchY)
    {
        var map = new DieMap
        {
            FrameObjId = id,
            DieMapX = 3,
            DieMapY = 3,
            PitchX = pitchX,
            PitchY = pitchY,
            DieSizeX = 8.0,
            DieSizeY = 6.0,
            OuterDiameterMm = 287.6,
            EdgeSkipMode = "ExternalMap",
            OriginX = -pitchX,
            OriginY = pitchY,
            SourceFileName = "RAD1.txt",
            SourceFormat = "RAD TXT",
            SourcePitchFromFile = true,
            SourceDeclaredCount = 9,
            SourceFirstX = 168,
            SourceFirstY = 202,
            SourceFirstPosX = pitchX,
            SourceFirstPosY = 0.0
        };

        int index = 0;
        for (int localY = 0; localY < 3; localY++)
        {
            for (int localX = 0; localX < 3; localX++)
            {
                double gridX = localX - 1;
                double gridY = 1 - localY;
                map.Entries.Add(new DieMapEntry
                {
                    Index = index++,
                    SequenceNo = index,
                    DieMapX = localX,
                    DieMapY = localY,
                    OriginalMapX = 166 + localX,
                    OriginalMapY = 203 - localY,
                    EquipmentGridX = gridX,
                    EquipmentGridY = gridY,
                    PosX = gridX * pitchX,
                    PosY = gridY * pitchY,
                    IsTarget = true
                });
            }
        }
        return DieMapGenerator.Normalize(map);
    }

    private static void ProbeDie(Form1 host, string activeName)
    {
        using (Control page = CreateHostedPage(host, "QMC.CDT_320.Ui.Pages.Recipe.DieSubsetPage", null, new Size(1094, 742)))
        {
            AssertActive(page, activeName);
            GroupBox guide = Field<GroupBox>(page, "grpSaveGuide");
            Label current = Field<Label>(page, "_lblCurrentRecipeInfo");
            Label sequence = Field<Label>(page, "lblSaveSequence");
            Label meaning = Field<Label>(page, "lblButtonMeaning");
            TextBox status = Field<TextBox>(page, "_txtOperationStatus");

            Check(guide.Width > 450 && guide.Height > 600, "Die save guide must occupy the right area");
            Check(current.Text.Contains(activeName), "Die guide must show active Recipe");
            Check(sequence.Text.Contains("상단 SAVE") && sequence.Text.Contains("SAVE SPEC"), "Die guide must state save order");
            Check(meaning.Text.Contains("LOAD SPEC") && meaning.Text.Contains("Reload"), "Die guide must explain load/save buttons");
            Check(status.Text.Contains("RECIPE LOAD"), "Die status must expose current load/save stage");
            Check(status.ReadOnly && status.ScrollBars == ScrollBars.Vertical, "Die status must be read-only and scrollable");
            AssertNoOverlap(page, Field<GroupBox>(page, "grpDieSpec"), guide, "Die left editor/right guide");
            AssertLabelFits(sequence, "Die save order label");
            AssertLabelFits(meaning, "Die button meaning label");
            SaveBitmap(page, "DieSubsetPage.png");
        }
    }

    private static void ProbeWafer(Form1 host, string activeName)
    {
        using (Control page = CreateHostedPage(host, "QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage", null, new Size(1094, 742)))
        {
            AssertActive(page, activeName);
            GroupBox sourceGroup = Field<GroupBox>(page, "grpMapSource");
            TextBox info = Field<TextBox>(page, "_tbMapSourceInfo");
            string text = info.Text;
            Check(sourceGroup.Width > 480 && sourceGroup.Height > 600, "Wafer source info must occupy right area");
            Check(info.ReadOnly && info.ScrollBars == ScrollBars.Vertical, "Wafer source info must be read-only and scrollable");
            Contains(text, "Project       : " + activeName, "Wafer info active Recipe");
            Contains(text, "X = Input Camera X / Y = Input Stage Y", "Wafer Input equipment axes");
            Contains(text, "Source file   : RAD1.txt", "Wafer source filename");
            Contains(text, "Raw index X   : 166 .. 168", "Wafer raw X range");
            Contains(text, "Raw index Y   : 201 .. 203", "Wafer raw Y range");
            Contains(text, "Equipment Grid (0,0)", "Wafer equipment center");
            Contains(text, "Local array   : 3 x 3 (normalized)", "Wafer local normalized array");
            Contains(text, "Header pitch  : 8.12 x 6.12 mm (file)", "Wafer header pitch source");
            Contains(text, "Die size      : 8 x 6 mm (Recipe -> Die Spec)", "Wafer die size ownership");
            Contains(text, "Screen pitch  : 8.12 x 6.12 mm", "Wafer input role pitch");
            Contains(text, "Grid control  : center (0,0), Left-/Right+, Down-/Up+", "Wafer grid direction");
            Contains(text, "INPUT DIE MAP CREATE", "Wafer final apply flow input");
            Contains(text, "BIN DIE MAP CREATE", "Wafer final apply flow output");
            Contains(text, "Final apply   : PENDING", "Wafer approval status");
            AssertNoOverlap(page, Field<GroupBox>(page, "grpFrame"), sourceGroup, "Wafer left editor/right info");

            Field<ComboBox>(page, "_cbWaferRole").SelectedIndex = 1;
            Application.DoEvents();
            string outputText = info.Text;
            Contains(outputText, "Wafer role    : OUTPUT", "Wafer output role");
            Contains(outputText, "X = Output Camera X / Y = Output Stage Y", "Wafer Output equipment axes");
            Contains(outputText, "Screen pitch  : 8.22 x 6.22 mm", "Wafer output role pitch");
            Contains(outputText, "Output place  : Camera X", "Wafer Output camera-to-picker note");
            SaveBitmap(page, "TapeFrameSubsetPage.png");
        }
    }

    private static void ProbeMapCreate(Form1 host, string activeName, bool output)
    {
        object[] args = output ? new object[] { "recipe.binMapCreate" } : null;
        string suffix = output ? "Output" : "Input";
        using (Control page = CreateHostedPage(host, "QMC.CDT_320.Ui.Pages.Recipe.MapCreatePage", args, new Size(1678, 900)))
        {
            Invoke(page, "LoadRecipeMapOrCreatePreview");
            page.PerformLayout();
            Application.DoEvents();
            AssertActive(page, activeName);

            TextBox info = Field<TextBox>(page, "_tbMapApplyInfo");
            Control apply = Field<Control>(page, "btnSave");
            Control mapView = Field<Control>(page, "_mapView");
            string text = info.Text;
            Check(info.ReadOnly && info.ScrollBars == ScrollBars.Vertical, suffix + " map apply info must be read-only and scrollable");
            Check(apply.Text == "FINAL APPLY TO RECIPE", suffix + " map final button label");
            Contains(text, "Recipe : " + activeName, suffix + " map active Recipe");
            Contains(text, output ? "Role   : OUTPUT GOOD" : "Role   : INPUT", suffix + " map role");
            Contains(text, output ? "X=Output Camera X / Y=Output Stage Y" : "X=Input Camera X / Y=Input Stage Y", suffix + " map equipment axes");
            if (output)
                Contains(text, "Output Camera X reference is converted to Picker X", "Output camera-to-picker placement note");
            Contains(text, "Source : RAD1.txt (RAD TXT)", suffix + " map source");
            Contains(text, "Raw    : X 166..168 / Y 201..203", suffix + " map raw indices");
            Contains(text, "Local  : 3 x 3 / center Grid (0,0)", suffix + " map equipment center");
            Contains(text, output ? "Pitch  : 8.22 x 6.22 mm" : "Pitch  : 8.12 x 6.12 mm", suffix + " map role pitch");
            Contains(text, "Die    : 8 x 6 mm (Die Spec)", suffix + " map die source");
            Contains(text, "Approve: PENDING", suffix + " map approval status");
            Contains(text, "Check  : READY FOR FINAL APPLY", suffix + " map validation status");
            Check((bool)mapView.GetType().GetProperty("CompactUsedBounds").GetValue(mapView, null), suffix + " map compact bounds");
            Check((bool)mapView.GetType().GetProperty("ShowEquipmentAxes").GetValue(mapView, null), suffix + " map equipment axes overlay");
            Check(Rect(page, info).Bottom <= Rect(page, apply).Top, suffix + " map info/apply button must not overlap");
            Check(Rect(page, info).Right <= page.ClientSize.Width && Rect(page, apply).Right <= page.ClientSize.Width, suffix + " map right panel must remain in bounds");
            SaveBitmap(page, "MapCreatePage-" + suffix + ".png");
        }
    }

    private static Control CreateHostedPage(Form1 host, string typeName, object[] args, Size size)
    {
        Type type = typeof(RecipeProject).Assembly.GetType(typeName, true);
        Control page = (Control)(args == null ? Activator.CreateInstance(type) : Activator.CreateInstance(type, args));
        page.Size = size;
        page.Location = Point.Empty;
        host.Controls.Add(page);
        page.BringToFront();
        page.CreateControl();
        MethodInfo ensure = FindMethod(page.GetType(), "EnsureEditorBuilt");
        if (ensure != null)
            ensure.Invoke(page, null);
        page.PerformLayout();
        Application.DoEvents();
        return page;
    }

    private static void AssertActive(Control page, string expected)
    {
        RecipeProject project = Field<RecipeProject>(page, "_project");
        Check(project != null && string.Equals(project.FileName, expected, StringComparison.OrdinalIgnoreCase),
            page.GetType().Name + " must use host CurrentRecipeName; actual=" + (project == null ? "<null>" : project.FileName));
    }

    private static void SetCurrentRecipeName(Form1 host, string value)
    {
        PropertyInfo property = typeof(Form1).GetProperty("CurrentRecipeName", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo setter = property.GetSetMethod(true);
        setter.Invoke(host, new object[] { value });
    }

    private static void SaveBitmap(Control page, string fileName)
    {
        if (page.Parent != null)
            page.Parent.Controls.Remove(page);
        page.Visible = true;
        page.CreateControl();
        page.PerformLayout();
        Application.DoEvents();
        using (var bitmap = new Bitmap(page.Width, page.Height))
        {
            page.DrawToBitmap(bitmap, new Rectangle(Point.Empty, page.Size));
            bitmap.Save(Path.Combine(_artifactDir, fileName));
        }
    }

    private static void AssertNoOverlap(Control page, Control left, Control right, string label)
    {
        Rectangle l = Rect(page, left);
        Rectangle r = Rect(page, right);
        Check(!l.IntersectsWith(r), label + " must not overlap: left=" + l + ", right=" + r);
        Check(r.Right <= page.ClientSize.Width && r.Bottom <= page.ClientSize.Height, label + " right control must remain in page bounds: " + r);
    }

    private static void AssertLabelFits(Label label, string name)
    {
        Size proposed = new Size(Math.Max(1, label.ClientSize.Width - label.Padding.Horizontal), int.MaxValue);
        Size measured = TextRenderer.MeasureText(label.Text, label.Font, proposed, TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        int available = label.ClientSize.Height - label.Padding.Vertical;
        Check(measured.Height <= available + label.Font.Height, name + " text may be clipped: measured=" + measured.Height + ", available=" + available);
    }

    private static Rectangle Rect(Control ancestor, Control control)
    {
        Point p = control.PointToScreen(Point.Empty);
        p = ancestor.PointToClient(p);
        return new Rectangle(p, control.ClientSize);
    }

    private static void Contains(string text, string expected, string label)
    {
        Check(text != null && text.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0,
            label + " missing. expected='" + expected + "' actual='" + text + "'");
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

    private static object Invoke(object target, string name)
    {
        MethodInfo method = FindMethod(target.GetType(), name);
        if (method == null)
            throw new MissingMethodException(target.GetType().FullName, name);
        try
        {
            return method.Invoke(target, null);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    private static MethodInfo FindMethod(Type type, string name)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (method != null)
                return method;
            type = type.BaseType;
        }
        return null;
    }

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
