using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

// R2c 스모크 — SidebarButton 상태점 페인트(Off/Done/Dirty) + 선택/기본 비주얼 렌더 무예외.
class SidebarBtnSmoke
{
    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        string dir = @"D:\Work\source\cdt-320\redesign-r2c-test";
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        var t = asm.GetType("QMC.Vision.Ui.Controls.SidebarButton");
        var tStatus = asm.GetType("QMC.Vision.Ui.Controls.SidebarStatus");
        try
        {
            using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), Width = 240, Height = 260, BackColor = Color.FromArgb(0x59,0x59,0x59) })
            {
                string[] texts = { "웨이퍼 비전 (미설정)", "빈 비전 (설정완료)", "바텀 검사 (변경됨)", "선택됨" };
                string[] stats = { "Off", "Done", "Dirty", "Done" };
                int y = 6;
                for (int i = 0; i < 4; i++)
                {
                    var b = (Control)Activator.CreateInstance(t);
                    t.GetProperty("Text").SetValue(b, texts[i], null);
                    t.GetProperty("Status").SetValue(b, Enum.Parse(tStatus, stats[i]), null);
                    if (i == 3) t.GetProperty("Selected").SetValue(b, true, null);
                    b.Location = new Point(6, y); b.Size = new Size(200, 46); y += 50;
                    host.Controls.Add(b);
                }
                host.Show(); Application.DoEvents(); Application.DoEvents();
                using (var bmp = new Bitmap(host.ClientSize.Width, host.ClientSize.Height))
                    { host.DrawToBitmap(bmp, new Rectangle(0,0,host.ClientSize.Width,host.ClientSize.Height)); bmp.Save(dir + "\\SidebarButtons.png", ImageFormat.Png); }
                host.Hide();
            }
            Console.WriteLine("  [PASS] SidebarButton 상태점/비주얼 렌더 OK");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine("  [FAIL] " + (ex.InnerException ?? ex).Message); return 1; }
    }
}
