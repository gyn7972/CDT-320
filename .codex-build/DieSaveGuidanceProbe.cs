using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using QMC.Common.Data.Store;
using QMC.CDT320.Recipes;

internal static class DieSaveGuidanceProbe
{
    private static int _checks;

    [STAThread]
    private static int Main()
    {
        try
        {
            DataPaths.Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "die-save-probe-" + Guid.NewGuid().ToString("N"));
            DataPaths.EnsureRoot();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string name = "DIE-SAVE-PROBE-" + Guid.NewGuid().ToString("N");
            var source = new RecipeProject
            {
                FileName = name,
                Die = new DieSubset
                {
                    DieSpecName = "ProbeDie",
                    WidthMm = 1.0,
                    HeightMm = 1.0,
                    ThicknessMm = 0.1
                },
                Frame = new TapeFrameSubset(),
                InputFrame = new TapeFrameSubset(),
                OutputFrame = new TapeFrameSubset()
            };
            Check(RecipeStore.Save(source), "seed project save");
            RecipeStore.SaveLastProjectName(name);

            Type pageType = typeof(RecipeProject).Assembly.GetType("QMC.CDT_320.Ui.Pages.Recipe.DieSubsetPage", true);
            using (Control page = (Control)Activator.CreateInstance(pageType))
            {
                page.CreateControl();
                Field<TextBox>(page, "_tbName").Text = "ProbeDieSaved";
                Field<NumericUpDown>(page, "_nW").Value = 8.12M;
                Field<NumericUpDown>(page, "_nH").Value = 6.12M;
                Field<NumericUpDown>(page, "_nT").Value = 0.25M;

                Invoke(page, "SaveToRecipe");

                RecipeProject saved = RecipeStore.Load(name);
                Check(saved != null, "saved project reload");
                Check(Equal(saved.Die.WidthMm, 8.12), "recipe die width persisted");
                Check(Equal(saved.Die.HeightMm, 6.12), "recipe die height persisted");
                Check(Equal(saved.Die.ThicknessMm, 0.25), "recipe die thickness persisted");
                Check(Equal(saved.Frame.DieSizeX, 8.12) && Equal(saved.InputFrame.DieSizeX, 8.12) && Equal(saved.OutputFrame.DieSizeX, 8.12), "all frame die widths mirrored");
                Check(Equal(saved.Frame.DieSizeY, 6.12) && Equal(saved.InputFrame.DieSizeY, 6.12) && Equal(saved.OutputFrame.DieSizeY, 6.12), "all frame die heights mirrored");
                Check(Equal(saved.ChipThickness, 250.0), "legacy thickness mirrored in micrometers");

                TextBox status = Field<TextBox>(page, "_txtOperationStatus");
                Check(status.Text.IndexOf("SAVE COMPLETE", StringComparison.OrdinalIgnoreCase) >= 0, "success status visible");
                Check(Field<Label>(page, "_lblCurrentRecipeInfo").Text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0, "current recipe visible in guide");
                ComboBox library = Field<ComboBox>(page, "_cbSpecLibrary");
                Check(library.Items.Count > 0 && string.Equals(library.SelectedItem as string, "ProbeDieSaved", StringComparison.OrdinalIgnoreCase), "saved spec selected in library");

                page.Size = new Size(1094, 742);
                page.PerformLayout();
                using (var bitmap = new Bitmap(page.Width, page.Height))
                {
                    page.DrawToBitmap(bitmap, new Rectangle(Point.Empty, page.Size));
                    bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DieSaveGuidancePage.png"));
                }

                RecipeProject missingBase = RecipeStore.Load(name);
                missingBase.BaseWaferMapFileName = Path.Combine("Config", "WaferMap", "missing-base.txt");
                SetField(page, "_project", missingBase);
                bool exactFailure = false;
                try
                {
                    Invoke(page, "SaveToRecipe");
                }
                catch (TargetInvocationException ex)
                {
                    Exception cause = ex.InnerException ?? ex;
                    exactFailure = cause.Message.IndexOf("[BASE MAP CHECK]", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                   cause.Message.IndexOf("LOAD WAFER MAP", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                Check(exactFailure, "missing base map reports exact stage and recovery");
                Check(status.Text.IndexOf("BASE MAP CHECK", StringComparison.OrdinalIgnoreCase) >= 0, "failure stage remains visible in guide");
            }

            Console.WriteLine("PASS: " + _checks + " Die save/guidance checks");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static void Invoke(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, null);
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

    private static void SetField(object target, string name, object value)
    {
        Type type = target.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }
            type = type.BaseType;
        }
        throw new MissingFieldException(target.GetType().FullName, name);
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
