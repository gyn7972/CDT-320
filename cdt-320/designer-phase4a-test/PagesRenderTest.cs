using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 93 Phase4a 검증 — 6 Pages 인스턴스화 + 렌더 무예외(동작 무변경 스모크).
class PagesRenderTest
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\designer-phase4a-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        foreach (var n in new[] { "OperationPage", "DataLogPage", "RecipePage", "ConfigurationPage", "SettingsPage", "SpcChartPage" })
            Render(asm, "QMC.Vision.Ui.Pages." + n, new Size(1200, 760), dir + @"\" + n + ".png");
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
