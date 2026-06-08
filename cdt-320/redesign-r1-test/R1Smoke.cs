using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// R1 스모크 — ParameterGridControl 인스턴스화 + SetItems(샘플) + DrawToBitmap 무예외 / NumericKeypadDialog 렌더 무예외.
class R1Smoke
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\redesign-r1-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");

        TestParameterGrid(asm, dir);
        RenderDialog(asm, dir);

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void TestParameterGrid(Assembly asm, string dir)
    {
        try
        {
            var tCtrl = asm.GetType("QMC.Vision.Ui.Controls.ParameterGridControl");
            var tItem = asm.GetType("QMC.Vision.Ui.Controls.ParameterGridItem");
            var tScope = asm.GetType("QMC.Vision.Ui.Controls.ParameterGridScope");
            object recipe = Enum.Parse(tScope, "Recipe");
            object setup = Enum.Parse(tScope, "Setup");

            // backing store
            double dv = 12.5; int iv = 7; bool bv = true;
            var items = new List<object>();
            // Double(name,unit,scope,Func<double>,Action<double>)
            items.Add(tItem.GetMethod("Double").Invoke(null, new object[] {
                "Exposure", "ms", recipe, (Func<double>)(() => dv), (Action<double>)(x => dv = x) }));
            // Int(name,unit,scope,Func<int>,Action<int>)
            items.Add(tItem.GetMethod("Int").Invoke(null, new object[] {
                "Gain", "dB", setup, (Func<int>)(() => iv), (Action<int>)(x => iv = x) }));
            // Bool(name,scope,Func<bool>,Action<bool>)
            items.Add(tItem.GetMethod("Bool").Invoke(null, new object[] {
                "Enable", recipe, (Func<bool>)(() => bv), (Action<bool>)(x => bv = x) }));
            // Micron(name,scope,Func<double>,Action<double>)
            items.Add(tItem.GetMethod("Micron").Invoke(null, new object[] {
                "Offset", setup, (Func<double>)(() => dv/1000.0), (Action<double>)(x => dv = x*1000.0) }));

            var ctrl = (Control)Activator.CreateInstance(tCtrl);
            ctrl.Size = new Size(640, 300);
            // SetItems(IEnumerable<ParameterGridItem>) — build typed array
            var arr = Array.CreateInstance(tItem, items.Count);
            for (int i = 0; i < items.Count; i++) arr.SetValue(items[i], i);
            tCtrl.GetMethod("SetItems").Invoke(ctrl, new object[] { arr });

            using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), Width = 700, Height = 360 })
            {
                ctrl.Dock = DockStyle.Fill; host.Controls.Add(ctrl); host.Show(); Application.DoEvents();
                using (var bmp = new Bitmap(ctrl.Width, ctrl.Height))
                    { ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height)); bmp.Save(dir + "\\ParameterGridControl.png", ImageFormat.Png); }
                // RefreshValues no-exception
                tCtrl.GetMethod("RefreshValues").Invoke(ctrl, null);
                host.Hide(); host.Controls.Remove(ctrl);
            }
            // verify row count via grid field
            var grid = tCtrl.GetField("grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ctrl) as DataGridView;
            int rows = grid?.Rows.Count ?? -1;
            bool ok = rows == 4;
            Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + "ParameterGridControl SetItems(4)+render rows=" + rows);
            if (!ok) _fail++;
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] ParameterGridControl : " + (ex.InnerException ?? ex).Message); }
    }

    static void RenderDialog(Assembly asm, string dir)
    {
        try
        {
            var t = asm.GetType("QMC.Vision.Ui.Controls.NumericKeypadDialog");
            var dlg = (Form)Activator.CreateInstance(t, new object[] { "Exposure", "12.5", "ms" });
            dlg.StartPosition = FormStartPosition.Manual; dlg.Location = new Point(-3000, -3000);
            dlg.Show(); Application.DoEvents();
            using (var bmp = new Bitmap(dlg.Width, dlg.Height))
                { dlg.DrawToBitmap(bmp, new Rectangle(0, 0, dlg.Width, dlg.Height)); bmp.Save(dir + "\\NumericKeypadDialog.png", ImageFormat.Png); }
            string vt = (string)t.GetProperty("ValueText").GetValue(dlg, null);
            dlg.Close(); dlg.Dispose();
            bool ok = vt == "12.5";
            Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + "NumericKeypadDialog ctor+render+ValueText=" + vt);
            if (!ok) _fail++;
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] NumericKeypadDialog : " + (ex.InnerException ?? ex).Message); }
    }
}
