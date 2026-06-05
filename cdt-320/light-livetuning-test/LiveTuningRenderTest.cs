using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// LightLiveTuningPanel 시각/통합 검증 (computer-use 불가 → DrawToBitmap 렌더링).
//  (1) 패널 단독 렌더 PNG  (2) InspectionLightPanel 통합 시 _liveTuning Dock=Right 배치 PNG + 리플렉션 점검.
class LiveTuningRenderTest
{
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\light-livetuning-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        int fail = 0;

        // (1) 패널 단독
        var tPanel = asm.GetType("QMC.Vision.Ui.Controls.LightLiveTuningPanel");
        var panel = (Control)Activator.CreateInstance(tPanel);
        panel.Size = new Size(220, 320);
        using (var host = new Form { Width = 260, Height = 380, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) })
        {
            panel.Location = new Point(8, 8);
            host.Controls.Add(panel);
            host.Show();
            Application.DoEvents();
            using (var bmp = new Bitmap(panel.Width, panel.Height))
            {
                panel.DrawToBitmap(bmp, new Rectangle(0, 0, panel.Width, panel.Height));
                bmp.Save(dir + @"\panel_standalone.png", ImageFormat.Png);
            }
            host.Hide();
        }
        Console.WriteLine("  [OK] panel_standalone.png 저장");

        // (2) InspectionLightPanel 통합
        var tIlp = asm.GetType("QMC.Vision.Ui.Pages.InspectionLightPanel");
        var ilp = (Control)Activator.CreateInstance(tIlp);
        using (var host = new Form { Width = 1000, Height = 560, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) })
        {
            ilp.Dock = DockStyle.Fill;
            host.Controls.Add(ilp);
            host.Show();
            Application.DoEvents();

            // 리플렉션 — _liveTuning 점검
            var bf = BindingFlags.NonPublic | BindingFlags.Instance;
            var fld = tIlp.GetField("_liveTuning", bf);
            var lt = fld?.GetValue(ilp) as Control;
            bool exists = lt != null;
            bool docked = exists && lt.Dock == DockStyle.Right;
            bool width  = exists && lt.Width == 220;
            bool inControls = exists && ContainsDeep(ilp, lt);
            Check("_liveTuning 필드 생성", exists, exists ? "" : "null", ref fail);
            Check("Dock=Right", docked, docked ? "" : (exists ? lt.Dock.ToString() : "-"), ref fail);
            Check("Width=220", width, width ? "" : (exists ? lt.Width.ToString() : "-"), ref fail);
            Check("Controls 트리에 포함", inControls, "", ref fail);

            using (var bmp = new Bitmap(host.ClientSize.Width, host.ClientSize.Height))
            {
                host.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(dir + @"\inspectionpanel_integrated.png", ImageFormat.Png);
            }
            host.Hide();
        }
        Console.WriteLine("  [OK] inspectionpanel_integrated.png 저장");

        Console.WriteLine(fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + fail + " FAIL"));
        return fail == 0 ? 0 : 1;
    }

    static bool ContainsDeep(Control parent, Control target)
    {
        foreach (Control c in parent.Controls)
        {
            if (ReferenceEquals(c, target)) return true;
            if (ContainsDeep(c, target)) return true;
        }
        return false;
    }
    static void Check(string name, bool ok, string detail, ref int fail)
    {
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + name + (string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")"));
        if (!ok) fail++;
    }
}
