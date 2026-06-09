using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 95 Phase4b-2 검증 — FinderPage + InspectorPage 파라미터리스 인스턴스화 + 렌더 무예외 + 양 생성자 보존.
class PagesRenderTest
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\designer-phase4b2-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");

        Render(asm, "QMC.Vision.Ui.Pages.FinderPage", new Size(1280, 840), dir + @"\FinderPage.png");
        Render(asm, "QMC.Vision.Ui.Pages.InspectorPage", new Size(1280, 840), dir + @"\InspectorPage.png");

        CheckCtors(asm, "QMC.Vision.Ui.Pages.FinderPage", "QMC.Vision.Modules.VisionModule", "QMC.Vision.Core.IPatternFinder");
        CheckCtors(asm, "QMC.Vision.Ui.Pages.InspectorPage", "QMC.Vision.Modules.VisionModule", "QMC.Vision.Core.IInspector");

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void CheckCtors(Assembly asm, string typeName, string p1, string p2)
    {
        var t = asm.GetType(typeName);
        bool c0 = t.GetConstructor(Type.EmptyTypes) != null;
        bool c2 = t.GetConstructor(new[] { asm.GetType(p1), asm.GetType(p2) }) != null;
        string n = typeName.Substring(typeName.LastIndexOf('.') + 1);
        Console.WriteLine((c0 && c2 ? "  [PASS] " : "  [FAIL] ") + n + " 양 생성자 보존 (()=" + c0 + ", 주입=" + c2 + ")");
        if (!(c0 && c2)) _fail++;
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
