using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Ui.Controls;

internal static class PhysicalMapRenderProbe
{
    private static int _checks;

    private static void Assert(bool condition, string message)
    {
        _checks++;
        if (!condition)
            throw new InvalidOperationException("ASSERT FAILED: " + message);
    }

    private static DieMap BuildMap(int gridX, int gridY, double pitchX, double pitchY,
        double dieX, double dieY, double diameter)
    {
        var map = new DieMap
        {
            FrameObjId = "PHYSICAL-RENDER-PROBE",
            DieMapX = gridX,
            DieMapY = gridY,
            PitchX = pitchX,
            PitchY = pitchY,
            DieSizeX = dieX,
            DieSizeY = dieY,
            OuterDiameterMm = diameter
        };

        int index = 0;
        for (int row = 0; row < gridY; row++)
        for (int col = 0; col < gridX; col++)
        {
            map.Entries.Add(new DieMapEntry
            {
                Index = index++,
                DieMapX = col,
                DieMapY = row,
                OriginalMapX = 100 + col,
                OriginalMapY = 200 + row,
                EquipmentGridX = col - (gridX - 1) / 2.0,
                EquipmentGridY = (gridY - 1) / 2.0 - row,
                IsTarget = true,
                BinCode = 1
            });
        }
        return DieMapGenerator.Normalize(map);
    }

    private static object[] GetLayout(DieMapView view)
    {
        MethodInfo method = typeof(DieMapView).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(item => item.Name == "GetMapLayout" && item.GetParameters().Length == 4);
        var args = new object[] { null, null, null, null };
        method.Invoke(view, args);
        return args;
    }

    private static float ReadFloat(object value, string field)
    {
        return (float)value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(value);
    }

    private static int ReadInt(object value, string field)
    {
        return (int)value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(value);
    }

    private static void Render(string path, DieMap map)
    {
        using (var view = new DieMapView())
        using (var bitmap = new Bitmap(900, 700))
        {
            view.Size = bitmap.Size;
            view.BackColor = Color.FromArgb(30, 30, 30);
            view.ShowWaferOutline = true;
            view.ShowEquipmentAxes = true;
            view.CompactUsedBounds = true;
            view.Map = map;

            object[] layout = GetLayout(view);
            var mapRect = (RectangleF)layout[0];
            var content = (RectangleF)layout[1];
            object cell = layout[2];
            object bounds = layout[3];
            float cellW = ReadFloat(cell, "Width");
            float cellH = ReadFloat(cell, "Height");
            float dieW = ReadFloat(cell, "DieWidth");
            float dieH = ReadFloat(cell, "DieHeight");

            double scaleX = cellW / map.PitchX;
            double scaleY = cellH / map.PitchY;
            Assert(Math.Abs(scaleX - scaleY) < 0.001, "X/Y use one mm scale");
            Assert(Math.Abs(dieW / map.DieSizeX - scaleX) < 0.001, "die width uses DieSizeX");
            Assert(Math.Abs(dieH / map.DieSizeY - scaleY) < 0.001, "die height uses DieSizeY");
            Assert(content.Width > 0 && content.Height > 0, "physical content viewport exists");

            DieMapEntry first = map.Entries.FirstOrDefault(item => item != null && item.IsTarget);
            if (first != null)
            {
                int minX = ReadInt(bounds, "MinX");
                int minY = ReadInt(bounds, "MinY");
                float centerX = mapRect.Left + (DieMapGenerator.ResolveMapIndexX(first) - minX + 0.5F) * cellW;
                float centerY = mapRect.Top + (DieMapGenerator.ResolveMapIndexY(first) - minY + 0.5F) * cellH;
                MethodInfo hitTest = typeof(DieMapView).GetMethod("HitTest", BindingFlags.Instance | BindingFlags.NonPublic);
                DieMapEntry hit = (DieMapEntry)hitTest.Invoke(view,
                    new object[] { (int)Math.Round(centerX), (int)Math.Round(centerY) });
                bool nonOverlapping = map.DieSizeX <= map.PitchX + 0.000001 &&
                                      map.DieSizeY <= map.PitchY + 0.000001;
                Assert(hit != null && (!nonOverlapping ||
                       (DieMapGenerator.ResolveMapIndexX(hit) == DieMapGenerator.ResolveMapIndexX(first) &&
                        DieMapGenerator.ResolveMapIndexY(hit) == DieMapGenerator.ResolveMapIndexY(first))),
                    "render center and hit-test use the same transform");
            }

            view.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(path);
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        string output = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
        System.IO.Directory.CreateDirectory(output);

        Render(System.IO.Path.Combine(output, "correct-physical-map.png"),
            BuildMap(35, 47, 8.12, 6.12, 8.12, 6.12, 287.64));
        Render(System.IO.Path.Combine(output, "invalid-overlap-visible.png"),
            BuildMap(35, 47, 0.001, 0.001, 8.12, 6.12, 287.64));
        Render(System.IO.Path.Combine(output, "grid-larger-than-wafer.png"),
            BuildMap(5, 5, 100.0, 80.0, 70.0, 50.0, 200.0));
        if (args.Length > 1 && System.IO.File.Exists(args[1]))
            Render(System.IO.Path.Combine(output, "rad-input-gap-zero.png"), DieMapGenerator.LoadJson(args[1]));
        if (args.Length > 2 && System.IO.File.Exists(args[2]))
            Render(System.IO.Path.Combine(output, "rad-output-gap-positive.png"), DieMapGenerator.LoadJson(args[2]));

        Console.WriteLine("PHYSICAL_MAP_RENDER_PROBE_PASS checks=" + _checks);
        return 0;
    }
}
