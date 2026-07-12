using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT_320.Ui.Pages.Work;
using QMC.Common.Data.Store;

internal static class ProcessCenterMapUiProbe
{
    private static int _checks;

    private static void Assert(bool condition, string message)
    {
        _checks++;
        if (!condition)
            throw new InvalidOperationException("ASSERT FAILED: " + message);
    }

    private static object Invoke(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(method != null, target.GetType().Name + "." + methodName + " exists");
        return method.Invoke(target, args);
    }

    private static T Field<T>(object target, string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(field != null, target.GetType().Name + "." + fieldName + " exists");
        return field.GetValue(target) as T;
    }

    private static DieMap BuildRadDomainMap(double gapX, double gapY)
    {
        double dieX = 8.12;
        double dieY = 6.12;
        double stepX = dieX + gapX;
        double stepY = dieY + gapY;
        var map = new DieMap
        {
            FrameObjId = "RAD1-PROBE",
            DieMapX = 35,
            DieMapY = 47,
            DieSizeX = dieX,
            DieSizeY = dieY,
            PitchX = stepX,
            PitchY = stepY,
            OuterDiameterMm = 287.64,
            OriginX = -17.0 * stepX,
            OriginY = 23.0 * stepY,
            EdgeSkipMode = "ExternalMap"
        };
        map.Entries.Add(new DieMapEntry
        {
            DieMapX = 10,
            DieMapY = 23,
            OriginalMapX = 176,
            OriginalMapY = 204,
            EquipmentGridX = -7,
            EquipmentGridY = 0,
            PosX = -7 * stepX,
            PosY = 0,
            IsTarget = true,
            BinCode = 1,
            SequenceNo = 1,
            DieUid = "RAD-176-204"
        });
        map.Entries.Add(new DieMapEntry
        {
            DieMapX = 17,
            DieMapY = 23,
            OriginalMapX = 183,
            OriginalMapY = 204,
            EquipmentGridX = 0,
            EquipmentGridY = 0,
            PosX = 0,
            PosY = 0,
            IsTarget = true,
            BinCode = 1,
            SequenceNo = 2,
            DieUid = "RAD-183-204"
        });
        return DieMapGenerator.Normalize(map);
    }

    private static void AssertColumns(DataGridView grid, string label)
    {
        Assert(grid != null, label + " grid exists");
        Assert(grid.Columns.Count == 11, label + " has raw/grid/process columns");
        Assert(grid.Columns[1].HeaderText == "DieMapX (Raw)", label + " raw X header");
        Assert(grid.Columns[2].HeaderText == "DieMapY (Raw)", label + " raw Y header");
        Assert(grid.Columns[3].HeaderText == "Grid X", label + " equipment grid X header");
        Assert(grid.Columns[4].HeaderText == "Grid Y", label + " equipment grid Y header");
        Assert(grid.Columns[8].HeaderText == "Process X(mm)", label + " process X header");
        Assert(grid.Columns[9].HeaderText == "Process Y(mm)", label + " process Y header");
    }

    private static void AssertCenterRow(DataGridView grid, string label)
    {
        DataGridViewRow row = grid.Rows.Cast<DataGridViewRow>()
            .First(r => Convert.ToString(r.Cells[1].Value) == "183" && Convert.ToString(r.Cells[2].Value) == "204");
        Assert(Convert.ToString(row.Cells[3].Value) == "0", label + " center Grid X=0");
        Assert(Convert.ToString(row.Cells[4].Value) == "0", label + " center Grid Y=0");
        Assert(Convert.ToString(row.Cells[8].Value) == "0.0000", label + " center process X relative to host center");
        Assert(Convert.ToString(row.Cells[9].Value) == "0.0000", label + " center process Y relative to host center");
    }

    private static void Render(Control control, string path)
    {
        control.Size = new Size(1920, 900);
        control.CreateControl();
        control.PerformLayout();
        using (var bitmap = new Bitmap(control.Width, control.Height))
        {
            control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(path);
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string repo = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            string isolated = Path.Combine(repo, ".codex-build", "process-center-ui", "probe-data");
            Directory.CreateDirectory(isolated);
            string renderRoot = Path.Combine(repo, ".codex-build", "process-center-ui-final", "renders");
            Directory.CreateDirectory(renderRoot);
            DataPaths.Root = isolated;
            Application.EnableVisualStyles();

            using (var input = new InputStageMapTransferPage())
            {
                DieMap map = BuildRadDomainMap(0.0, 0.0);
                Invoke(input, "ApplyMap", map, "RAD1 INPUT PREVIEW", false);
                Assert(Field<Label>(input, "lblChipW").Text == "8.1200", "Input die size X");
                Assert(Field<Label>(input, "lblChipH").Text == "6.1200", "Input die size Y");
                Assert(Field<Label>(input, "lblPitchX").Text == "0.0000", "Input pitch gap X can be zero");
                Assert(Field<Label>(input, "lblPitchY").Text == "0.0000", "Input pitch gap Y can be zero");
                Assert(Field<Label>(input, "lblWaferDia").Text == "287.640", "Input wafer diameter");
                DataGridView grid = Field<DataGridView>(input, "gridDieList");
                AssertColumns(grid, "Input");
                AssertCenterRow(grid, "Input");
                DataGridViewRow left = grid.Rows.Cast<DataGridViewRow>()
                    .First(r => Convert.ToString(r.Cells[1].Value) == "176");
                Assert(Convert.ToString(left.Cells[3].Value) == "-7", "Input left Grid X=-7");
                Assert(Convert.ToString(left.Cells[8].Value) == "-56.8400", "Input preview process X uses center + grid*step");
                Invoke(input, "SelectEntry", map.Entries.First(e => e.OriginalMapX == 183));
                Assert(Field<Label>(input, "lblBinRank").Text == "0 / 0", "Input selected Equipment Grid");
                Assert(Field<Label>(input, "lblDieNum").Text == "183 / 204", "Input selected original RAD index");
                Render(input, Path.Combine(renderRoot, "InputStageMapTransferPage.png"));
            }

            using (var output = new OutputStageMapTransferPage())
            {
                DieMap map = BuildRadDomainMap(0.3, 0.3);
                Invoke(output, "ApplyMap", map, null, null);
                Assert(Field<Label>(output, "lblChipW").Text == "8.1200", "Output die size X");
                Assert(Field<Label>(output, "lblChipH").Text == "6.1200", "Output die size Y");
                Assert(Field<Label>(output, "lblPitchX").Text == "0.3000", "Output pitch gap X");
                Assert(Field<Label>(output, "lblPitchY").Text == "0.3000", "Output pitch gap Y");
                Assert(Field<Label>(output, "lblWaferDia").Text == "287.640", "Output wafer diameter");
                DataGridView grid = Field<DataGridView>(output, "gridDieList");
                AssertColumns(grid, "Output");
                AssertCenterRow(grid, "Output");
                Invoke(output, "SelectEntry", map.Entries.First(e => e.OriginalMapX == 183));
                Assert(Field<Label>(output, "lblBinRank").Text == "0 / 0", "Output selected Equipment Grid");
                Assert(Field<Label>(output, "lblDieNum").Text == "183 / 204", "Output selected original RAD index");
                Render(output, Path.Combine(renderRoot, "OutputStageMapTransferPage.png"));
            }

            string materialSource = File.ReadAllText(Path.Combine(repo, "QMC.CDT-320", "Equipment", "Materials", "MaterialStateService.cs"));
            Assert(materialSource.Contains("entry.PosX = targetCenterX + equipmentGridX * pitchX;"), "Process test X uses equipment grid");
            Assert(materialSource.Contains("entry.PosY = targetCenterY + equipmentGridY * pitchY;"), "Process test Y uses equipment grid");
            Assert(materialSource.Contains("double originY = targetCenterY + pitchY * centerGridY;"), "Process test top-row origin is +Y");

            Console.WriteLine("PROCESS_CENTER_MAP_UI_PROBE_PASS checks=" + _checks);
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
