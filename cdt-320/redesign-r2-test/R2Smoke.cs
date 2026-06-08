using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// R2 스모크 — VisionTargetPage(3열) / RecipePage(사이드바 쉘) 인스턴스화+DrawToBitmap 무예외 + 레이아웃 렌더.
class R2Smoke
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\redesign-r2-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        Render(asm, "QMC.Vision.Ui.Pages.VisionTargetPage", new Size(1500, 820), dir + @"\VisionTargetPage.png");
        Render(asm, "QMC.Vision.Ui.Pages.RecipePage", new Size(1500, 820), dir + @"\RecipePage.png");
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
                ctrl.Dock = DockStyle.Fill; host.Controls.Add(ctrl); host.Show(); Application.DoEvents(); Application.DoEvents();
                using (var bmp = new Bitmap(ctrl.Width, ctrl.Height))
                    { ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height)); bmp.Save(png, ImageFormat.Png); }
                host.Hide(); host.Controls.Remove(ctrl);
            }
            Console.WriteLine("  [PASS] " + typeName.Substring(typeName.LastIndexOf('.') + 1) + " 인스턴스화+렌더 OK");
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] " + typeName + " : " + (ex.InnerException ?? ex).Message); }
    }
}
