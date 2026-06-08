using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 96 Phase4b-3 검증 — CameraMappingPanel 파라미터리스 인스턴스화 + 렌더 무예외 + public API(SelectAlgorithm) 보존.
class CameraMappingRenderTest
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\designer-phase4b3-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");

        Render(asm, "QMC.Vision.Ui.Pages.CameraMappingPanel", new Size(1000, 900), dir + @"\CameraMappingPanel.png");
        CheckApi(asm, "QMC.Vision.Ui.Pages.CameraMappingPanel", "SelectAlgorithm", typeof(string));

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void CheckApi(Assembly asm, string typeName, string method, Type p1)
    {
        var t = asm.GetType(typeName);
        bool c0 = t.GetConstructor(Type.EmptyTypes) != null;
        var mi = t.GetMethod(method, BindingFlags.Public | BindingFlags.Instance, null, new[] { p1 }, null);
        string n = typeName.Substring(typeName.LastIndexOf('.') + 1);
        bool ok = c0 && mi != null;
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + n + " public API 보존 (()=" + c0 + ", " + method + "(string)=" + (mi != null) + ")");
        if (!ok) _fail++;
    }

    static void Render(Assembly asm, string typeName, Size size, string png)
    {
        try
        {
            var t = asm.GetType(typeName);
            var ctrl = (Control)Activator.CreateInstance(t);
            ctrl.Size = size;
            using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), Width = size.Width + 40, Height = size.Height + 60 })
            {
                ctrl.Dock = DockStyle.Fill;
                host.Controls.Add(ctrl);
                host.Show();
                Application.DoEvents();
                using (var bmp = new Bitmap(ctrl.Width, ctrl.Height))
                {
                    ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height));
                    bmp.Save(png, ImageFormat.Png);
                }
                host.Hide();
            }
            Console.WriteLine("  [PASS] " + typeName.Substring(typeName.LastIndexOf('.') + 1) + " 인스턴스화+렌더 OK");
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] " + typeName + " : " + (ex.InnerException ?? ex).Message); }
    }
}
