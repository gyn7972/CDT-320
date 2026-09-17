// Offline rendering and hit testing only. Never create a LiveLotMapView window handle or start its timer.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Ui.Controls;
using QMC.CDT_320.Ui.Common.WaferMaps;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Pages.Recipe;
using System.Windows.Forms;

internal static class VerifyWaferMapViewStyle
{
    private static int checks;
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static object Call(DieMapView view, string method, params object[] args)
    { return typeof(DieMapView).GetMethod(method, PrivateInstance).Invoke(view, args); }
    private static string Snapshot(DieMap map)
    {
        using (var stream = new MemoryStream())
        { QMC.Common.Data.Store.JsonPrettySerializer.WriteObject(stream, typeof(DieMap), map); return Convert.ToBase64String(stream.ToArray()); }
    }
    private static Bitmap Render(DieMapView view)
    {
        var bitmap = new Bitmap(view.Width, view.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
            Call(view, "OnPaint", new PaintEventArgs(graphics, new Rectangle(Point.Empty, view.Size)));
        return bitmap;
    }
    private static byte[] Pixels(Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try { var pixels = new byte[data.Stride * data.Height]; Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); return pixels; }
        finally { bitmap.UnlockBits(data); }
    }
    private static void CompareViews(DieMapView recipe, LiveLotMapView main, string name, string directory, bool save)
    {
        using (var a = Render(recipe)) using (var b = Render(main))
        {
            Check(Pixels(a).SequenceEqual(Pixels(b)), name + ": equal map/state produces identical pixels in recipe and main");
            if (save) { a.Save(Path.Combine(directory, name + "-recipe.png")); b.Save(Path.Combine(directory, name + "-main.png")); }
        }
        Check(!main.IsHandleCreated, "offline verification never initializes live state/timer");
    }
    private static void VerifyHitTests(DieMapView view)
    {
        MethodInfo layout = typeof(DieMapView).GetMethods(PrivateInstance).Single(m => m.Name == "GetMapLayout" && m.GetParameters().Length == 4);
        object[] args = { null, null, null, null }; layout.Invoke(view, args);
        var mapRect = (RectangleF)args[0]; var contentRect = (RectangleF)args[1]; object cell = args[2], bounds = args[3];
        float cellWidth = (float)cell.GetType().GetField("Width").GetValue(cell);
        float cellHeight = (float)cell.GetType().GetField("Height").GetValue(cell);
        int tested = 0;
        foreach (var entry in view.Map.Entries)
        {
            float x = mapRect.Left + (int)typeof(DieMapView).GetMethod("ToViewX", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { (object)entry, bounds }) * cellWidth;
            float y = mapRect.Top + (int)typeof(DieMapView).GetMethod("ToViewY", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { (object)entry, bounds }) * cellHeight;
            var rect = (RectangleF)Call(view, "GetDieRect", x, y, cell);
            int px = (int)(rect.Left + rect.Width / 2), py = (int)(rect.Top + rect.Height / 2);
            if (rect.Width < 2 || rect.Height < 2 || px < 30 || px >= view.Width - 30 || py < 48 || py >= Math.Min(contentRect.Bottom, view.Height - 80)) continue;
            var hit = (DieMapEntry)Call(view, "HitTest", px, py);
            Check(object.ReferenceEquals(hit, entry), "drawn cell center hit returns exactly the same die entry"); tested++;
        }
        Check(tested > 0, "visible dies tested after fit/zoom/pan");
    }
    private static void ConfigureMain(LiveLotMapView main, DieMap map)
    {
        var states = (Dictionary<string, WaferMapCellState>)typeof(LiveLotMapView).GetField("_displayStates", PrivateInstance).GetValue(main);
        foreach (var entry in map.Entries) states[entry.DieMapX + ":" + entry.DieMapY] = entry.IsTarget ? WaferMapCellState.Wait : WaferMapCellState.Skip;
    }
    private static void Verify(string path, string output, int index)
    {
        var map = DieMapGenerator.LoadJson(path);
        Check(map != null && map.Entries.Count > 0, "registered map fixture loaded read-only");
        string before = Snapshot(map);
        foreach (var size in new[] { new Size(1200, 780), new Size(310, 560) })
        using (var recipe = new WaferMapView { Size = size, Caption = "Wafer Map", Map = map })
        using (var main = new LiveLotMapView { Size = size, Caption = "Wafer Map", Map = map })
        {
            ConfigureMain(main, map);
            foreach (var entry in map.Entries)
            {
                Check(recipe.CellColorResolver(entry) == main.CellColorResolver(entry) && recipe.CellStatusResolver(entry) == main.CellStatusResolver(entry), "recipe plan and main waiting/skip use same colors/status");
            }
            string name = "role-" + index + "-" + size.Width;
            CompareViews(recipe, main, name, output, true);
            VerifyHitTests(recipe); VerifyHitTests(main);
            var selected = map.Entries[map.Entries.Count / 2];
            recipe.SelectedEntry = selected; main.SelectedEntry = selected;
            typeof(DieMapView).GetField("_hover", PrivateInstance).SetValue(recipe, selected);
            typeof(DieMapView).GetField("_hover", PrivateInstance).SetValue(main, selected);
            CompareViews(recipe, main, name + "-selection", output, false);
            var wheel = new MouseEventArgs(MouseButtons.None, 0, size.Width / 2, size.Height / 2, 120);
            Call(recipe, "OnMouseWheelEvt", recipe, wheel); Call(main, "OnMouseWheelEvt", main, wheel);
            foreach (DieMapView view in new DieMapView[] { recipe, main })
            {
                // Start at the same drag state without creating a window handle through Focus().
                typeof(DieMapView).GetField("_dragging", PrivateInstance).SetValue(view, true);
                typeof(DieMapView).GetField("_dragStart", PrivateInstance).SetValue(view, Point.Empty);
                typeof(DieMapView).GetField("_dragStartPan", PrivateInstance).SetValue(view, PointF.Empty);
                Call(view, "OnMouseMoveEvt", view, new MouseEventArgs(MouseButtons.Middle, 0, 31, -17, 0));
                Call(view, "OnMouseUpEvt", view, new MouseEventArgs(MouseButtons.Middle, 0, 31, -17, 0));
                VerifyHitTests(view);
            }
            CompareViews(recipe, main, name + "-zoom-pan", output, false);
            Check(before == Snapshot(map), "paint/selection/zoom/pan preserves all die data and map geometry");
            Check(!main.IsHandleCreated, "no equipment state hookup");
        }
        // Test the existing editor data action separately from window initialization and file save.
        var editor = (MapCreatePage)FormatterServices.GetUninitializedObject(typeof(MapCreatePage));
        var target = map.Entries[map.Entries.Count / 2];
        var savedUid = target.DieUid; var savedBin = target.SourceBinCode; var savedToken = target.SourceToken;
        var x = target.OriginalMapX; var y = target.OriginalMapY; var gx = target.LogicalGridX; var gy = target.LogicalGridY;
        bool wasTarget = target.IsTarget;
        typeof(MapCreatePage).GetMethod("ApplyEntryTarget", PrivateInstance).Invoke(editor, new object[] { target, !wasTarget });
        using (var view = new WaferMapView { Map = map })
        {
            Check(target.IsTarget != wasTarget && target.DieUid == savedUid && target.SourceBinCode == savedBin && target.SourceToken == savedToken && target.OriginalMapX == x && target.OriginalMapY == y && target.LogicalGridX == gx && target.LogicalGridY == gy, "existing target edit preserves original identity and coordinates");
            Check(view.CellColorResolver(target) == (target.IsTarget ? WaferMapPalette.Wait : WaferMapPalette.Skip), "edited target/skip immediately uses common state color");
            Check((bool)typeof(MapCreatePage).GetField("_maskDirty", PrivateInstance).GetValue(editor), "existing editor still marks mask dirty");
        }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string output = Path.GetFullPath(args[0]); Directory.CreateDirectory(output);
            for (int i = 1; i < args.Length; i++) Verify(args[i], output, i);
            Check(args.Length > 1, "at least one map tested");
            Console.WriteLine("PASS: " + checks + " viewer assertions; pixel equality, die hit tests, edit identity; no live handles/equipment runtime.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
