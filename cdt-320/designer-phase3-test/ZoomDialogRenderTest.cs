using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 92 Phase3 검증 — ZoomDialog 양 생성자 인스턴스화 + 렌더 무예외 + public 시그니처 보존.
class ZoomDialogRenderTest
{
    static int _fail = 0;
    static void Check(string n, bool ok, string d) { Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + n + (string.IsNullOrEmpty(d) ? "" : "  (" + d + ")")); if (!ok) _fail++; }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\designer-phase3-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        var t = asm.GetType("QMC.Vision.Ui.Dialogs.ZoomDialog");

        // public 시그니처: 파라미터리스(디자이너) + (Bitmap, string) 둘 다 존재
        bool ctorDesigner = t.GetConstructor(Type.EmptyTypes) != null;
        bool ctorRuntime  = t.GetConstructor(new[] { typeof(Bitmap), typeof(string) }) != null;
        Check("생성자 보존 (() + (Bitmap,string))", ctorDesigner && ctorRuntime, $"designer={ctorDesigner}, runtime={ctorRuntime}");

        // 디자이너 경로: 파라미터리스 인스턴스화 + 렌더
        RenderForm(() => (Form)Activator.CreateInstance(t), new Size(700, 520), dir + @"\zoom_designer.png", "파라미터리스(디자이너)");

        // 런타임 경로: (Bitmap, title) 인스턴스화 + 렌더
        using (var bmp = new Bitmap(320, 240))
        {
            using (var g = Graphics.FromImage(bmp)) { g.Clear(Color.SteelBlue); g.DrawString("TEST", new Font("Arial", 28), Brushes.White, 90, 90); }
            var img = (Bitmap)bmp.Clone();
            RenderForm(() => (Form)Activator.CreateInstance(t, new object[] { img, "Zoom Test" }), new Size(800, 600), dir + @"\zoom_runtime.png", "(Bitmap,title) 런타임");
        }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void RenderForm(Func<Form> make, Size size, string png, string label)
    {
        try
        {
            var f = make();
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(-3000, -3000);
            f.Size = size;
            f.Show();
            Application.DoEvents();
            using (var bmp = new Bitmap(f.Width, f.Height))
            {
                f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                bmp.Save(png, ImageFormat.Png);
            }
            f.Close();
            Console.WriteLine("  [PASS] ZoomDialog " + label + " 인스턴스화+렌더 OK");
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] " + label + " : " + (ex.InnerException ?? ex).Message); }
    }
}
