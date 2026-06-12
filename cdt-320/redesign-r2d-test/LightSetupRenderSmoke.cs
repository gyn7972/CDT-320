using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Ui.Pages;

// C3b-3 Step6 — 결선 UI 제거 후 LightSystemSetupPage 가 정상 인스턴스화·렌더되는지 + 결선 컨트롤 부재 확인.
class LightSetupRenderSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        LightSystemSetupStore.SetCurrent(new LightSystemSetup
        {
            Controllers = new System.Collections.Generic.List<LightControllerEntry> {
                new LightControllerEntry { PortName = "COM-A", Name = "A", ChannelCount = 8, PageCount = 1, MaxPower = 240 } }
        });

        LightSystemSetupPage page = null;
        try { page = new LightSystemSetupPage { Width = 1200, Height = 700 }; }
        catch (Exception ex) { Ok("인스턴스화", false, ex.Message); Console.WriteLine("RESULT: 1 FAIL"); return 1; }
        Ok("인스턴스화 (예외 없음)", true);

        // 결선 컨트롤(_treeWiring/_gridSets) 부재 확인 (필드 제거됨)
        var t = typeof(LightSystemSetupPage);
        Ok("_treeWiring 필드 제거됨", t.GetField("_treeWiring", BindingFlags.NonPublic | BindingFlags.Instance) == null);
        Ok("_gridSets 필드 제거됨", t.GetField("_gridSets", BindingFlags.NonPublic | BindingFlags.Instance) == null);
        Ok("_gridCtrl(컨트롤러 인벤토리) 유지", t.GetField("_gridCtrl", BindingFlags.NonPublic | BindingFlags.Instance) != null);

        // 렌더(DrawToBitmap) — 크래시 없이 그려지는지
        try
        {
            using (var bmp = new Bitmap(page.Width, page.Height))
            {
                page.DrawToBitmap(bmp, new Rectangle(0, 0, page.Width, page.Height));
                Ok("DrawToBitmap 렌더 (크래시 없음)", true);
            }
        }
        catch (Exception ex) { Ok("DrawToBitmap 렌더", false, ex.Message); }

        try { page.Dispose(); } catch { }
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
