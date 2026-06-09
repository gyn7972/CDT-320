using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// R2e 스모크 — 편입 조명패널(EmbeddedMode: Save/Cancel 숨김 + GridTheme) / 라이브튜닝(통일 헤더) 렌더.
class R2eLightSmoke
{
    static int _fail = 0;
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\redesign-r2d-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");

        // 조명패널: ctor(alg,id) + EmbeddedMode=true
        try
        {
            var t = asm.GetType("QMC.Vision.Ui.Pages.InspectionLightPanel");
            var ctrl = (Control)Activator.CreateInstance(t, new object[] { "Wafer", "wafer-align" });
            t.GetProperty("EmbeddedMode").SetValue(ctrl, true);
            RenderCtrl(ctrl, new Size(375, 234), dir + @"\R2e_LightPanel.png", "InspectionLightPanel(Embedded)");
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] InspectionLightPanel : " + (ex.InnerException ?? ex).Message); }

        // 라이브튜닝: 무인자 ctor(통일 헤더)
        try
        {
            var t = asm.GetType("QMC.Vision.Ui.Controls.LightLiveTuningPanel");
            var ctrl = (Control)Activator.CreateInstance(t);
            RenderCtrl(ctrl, new Size(375, 200), dir + @"\R2e_LivePanel.png", "LightLiveTuningPanel");
        }
        catch (Exception ex) { _fail++; Console.WriteLine("  [FAIL] LightLiveTuningPanel : " + (ex.InnerException ?? ex).Message); }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static void RenderCtrl(Control ctrl, Size size, string png, string label)
    {
        ctrl.Size = size;
        using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), Width = size.Width + 40, Height = size.Height + 60 })
        {
            ctrl.Dock = DockStyle.Fill; host.Controls.Add(ctrl); host.Show(); Application.DoEvents(); Application.DoEvents();
            using (var bmp = new Bitmap(ctrl.Width, ctrl.Height))
                { ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height)); bmp.Save(png, ImageFormat.Png); }
            host.Hide(); host.Controls.Remove(ctrl);
        }
        Console.WriteLine("  [PASS] " + label + " 렌더 OK");
    }
}
