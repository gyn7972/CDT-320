using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 94 Phase4b-1 검증 — InspectionOverridePanel + InspectionLightPanel 인스턴스화 + 렌더 무예외.
class PanelsRenderTest
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\designer-phase4b1-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        Render(asm, "QMC.Vision.Ui.Pages.InspectionOverridePanel", new Size(900, 560), dir + @"\InspectionOverridePanel.png");
        Render(asm, "QMC.Vision.Ui.Pages.InspectionLightPanel", new Size(900, 560), dir + @"\InspectionLightPanel.png");
        // 양 ctor 시그니처 보존 확인 (InspectionLightPanel)
        var t = asm.GetType("QMC.Vision.Ui.Pages.InspectionLightPanel");
        bool c0 = t.GetConstructor(Type.EmptyTypes) != null;
        bool c2 = t.GetConstructor(new[] { typeof(string), typeof(string) }) != null;
        Console.WriteLine((c0 && c2 ? "  [PASS] " : "  [FAIL] ") + "InspectionLightPanel 양 생성자 보존 (()=" + c0 + ", (s,s)=" + c2 + ")");
        if (!(c0 && c2)) _fail++;
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
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
