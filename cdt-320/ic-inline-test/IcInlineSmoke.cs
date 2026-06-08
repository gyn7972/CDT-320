using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// IC 헬퍼호출 인라인 후 6개 영향 컨트롤 인스턴스화+DrawToBitmap 무예외 확인.
class IcInlineSmoke
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\ic-inline-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        string[] types = {
            "QMC.Vision.Ui.Pages.CameraMappingPanel",
            "QMC.Vision.Ui.Pages.LightSystemSetupPage",
            "QMC.Vision.Ui.Pages.InspectionOverridePanel",
            "QMC.Vision.Ui.Pages.InspectionLightPanel",
            "QMC.Vision.Ui.Pages.FinderPage",
            "QMC.Vision.Ui.Pages.InspectorPage",
        };
        foreach (var t in types) Render(asm, t, new Size(1280, 880), dir + "\\" + t.Substring(t.LastIndexOf('.') + 1) + ".png");
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void Render(Assembly asm, string typeName, Size size, string png)
    {
        try
        {
            var t = asm.GetType(typeName);
            if (t == null) { _fail++; Console.WriteLine("  [FAIL] " + typeName + " : type not found"); return; }
            var ctrl = (Control)Activator.CreateInstance(t);
            ctrl.Size = size;
            using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), Width = size.Width + 40, Height = size.Height + 60 })
            {
                ctrl.Dock = DockStyle.Fill;
                host.Controls.Add(ctrl);
                host.Show();
                Application.DoEvents();
                using (var bmp = new Bitmap(ctrl.Width, ctrl.Height))
                    { ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height)); bmp.Save(png, ImageFormat.Png); }
                host.Hide();
                host.Controls.Remove(ctrl);
            }
            Console.WriteLine("  [PASS] " + typeName.Substring(typeName.LastIndexOf('.') + 1) + " 인스턴스화+렌더 OK");
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] " + typeName + " : " + (ex.InnerException ?? ex).Message); }
    }
}
