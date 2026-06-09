using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// R2d 스모크 — InspectorTargetPage(3열) 신규 + RecipePage/VisionTargetPage 재설계 Size 렌더.
// 파라미터 없는 ctor(런타임 shell) 인스턴스화 + DrawToBitmap 무예외 + 레이아웃 PNG.
class R2dSmoke
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\redesign-r2d-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        Render(asm, "QMC.Vision.Ui.Pages.RecipePage",          new Size(1920, 902), dir + @"\RecipePage.png");
        Render(asm, "QMC.Vision.Ui.Pages.VisionTargetPage",    new Size(1710, 832), dir + @"\VisionTargetPage.png");
        Render(asm, "QMC.Vision.Ui.Pages.InspectorTargetPage", new Size(1710, 832), dir + @"\InspectorTargetPage.png");
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
