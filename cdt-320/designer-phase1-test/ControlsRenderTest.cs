using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// Stage 90 Phase1 검증 — 리팩터된 4개 컨트롤이 생성+렌더에 예외 없는지(동작 무변경 스모크).
// Control/Panel 파생이라 VS 디자이너로 직접 안 열림 → 생성+DrawToBitmap 로 대체 검증.
class ControlsRenderTest
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\designer-phase1-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");

        Render(asm, "QMC.Vision.Ui.Controls.JogBox",          new Size(260, 280), dir + @"\jogbox.png");
        Render(asm, "QMC.Vision.Ui.Controls.IlluminatorPanel", new Size(290, 180), dir + @"\illuminator.png");
        Render(asm, "QMC.Vision.Ui.Controls.BottomMenuButton", new Size(110, 70),  dir + @"\bottommenu.png");
        Render(asm, "QMC.Vision.Ui.Controls.CameraView",        new Size(360, 260), dir + @"\cameraview.png");

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
            using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000), Width = size.Width + 40, Height = size.Height + 60 })
            {
                ctrl.Location = new Point(4, 4);
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
            Console.WriteLine("  [PASS] " + typeName.Substring(typeName.LastIndexOf('.') + 1) + " 생성+렌더 OK → " + System.IO.Path.GetFileName(png));
        }
        catch (Exception ex)
        {
            _fail++;
            Console.WriteLine("  [FAIL] " + typeName + " : " + (ex.InnerException ?? ex).Message);
        }
    }
}
