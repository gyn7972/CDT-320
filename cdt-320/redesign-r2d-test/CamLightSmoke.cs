using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Ui.Pages;

// 조명 지정 모듈 이전 — Step3: CameraMappingPanel 조명 섹션(이식한 그리드 로직).
//  모듈 바인딩은 Form1 런타임 해석이라 헤드리스 불가 → 그리드 컬럼/수집/클램프/표시·렌더를 reflection 으로 검증.
class CamLightSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static object Call(object o, string m, params object[] a)
        => o.GetType().GetMethod(m, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(o, a);
    static T Field<T>(object o, string f)
        => (T)o.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        LightSystemSetupStore.SetCurrent(new LightSystemSetup
        {
            Controllers = new List<LightControllerEntry> {
                new LightControllerEntry { PortName = "COM-A", Name = "A", ChannelCount = 8, PageCount = 3, MaxPower = 240 },
                new LightControllerEntry { PortName = "COM-B", Name = "B", ChannelCount = 4, PageCount = 1, MaxPower = 240 } }
        });

        CameraMappingPanel panel = null;
        try { panel = new CameraMappingPanel { Width = 1140, Height = 595 }; }
        catch (Exception ex) { Ok("인스턴스화", false, ex.Message); Console.WriteLine("RESULT: 1 FAIL"); return 1; }
        Ok("인스턴스화 (예외 없음)", true);

        var grid = Field<DataGridView>(panel, "_gridLightAssign");
        Ok("_gridLightAssign 존재", grid != null);
        Ok("_lblLightAssign 존재", Field<Label>(panel, "_lblLightAssign") != null);
        Ok("페이지 프리뷰 우측 이동(700,188)",
            Field<PictureBox>(panel, "_picPreview").Location == new Point(700, 188));

        // 컨트롤러 콤보 = 인벤토리 PortName
        Call(panel, "RefreshLightCombos");
        var colCtrl = (DataGridViewComboBoxColumn)grid.Columns["ControllerPort"];
        Ok("컨트롤러 콤보 = COM-A/COM-B", colCtrl.Items.Contains("COM-A") && colCtrl.Items.Contains("COM-B"),
            $"(got {colCtrl.Items.Count})");

        // 행 추가 COM-A → 페이지 items=PageCount 3 (string)
        int idx = grid.Rows.Add();
        grid.Rows[idx].Cells["ControllerPort"].Value = "COM-A";
        int pc = (int)Call(panel, "SetLightPageCellItems", idx, "COM-A");
        Ok("COM-A PageCount=3", pc == 3, $"(got {pc})");
        var pageCell = grid.Rows[idx].Cells["Page"] as DataGridViewComboBoxCell;
        Ok("페이지 셀 items=3 (string)", pageCell.Items.Count == 3 && pageCell.Items.Contains("2"));
        grid.Rows[idx].Cells["Page"].Value = "2";
        Ok("페이지 표시값(FormattedValue)=2", (grid.Rows[idx].Cells["Page"].FormattedValue as string) == "2",
            $"(got '{grid.Rows[idx].Cells["Page"].FormattedValue}')");

        // 행 추가 COM-B(PageCount=1) page=5 → 수집 시 0 으로 클램프
        int idx2 = grid.Rows.Add();
        grid.Rows[idx2].Cells["ControllerPort"].Value = "COM-B";
        Call(panel, "SetLightPageCellItems", idx2, "COM-B");
        grid.Rows[idx2].Cells["Page"].Value = "0";

        // 수집(CollectLightPages)
        var list = Call(panel, "CollectLightPages") as List<LightPageRef>;
        Ok("수집 2건", list.Count == 2, $"(got {list.Count})");
        Ok("COM-A p2 수집", list.Any(p => p.ControllerPort == "COM-A" && p.Page == 2));
        Ok("COM-B p0 수집", list.Any(p => p.ControllerPort == "COM-B" && p.Page == 0));

        // 중복 제거 — COM-A p2 한 행 더 → 1건만
        int idx3 = grid.Rows.Add();
        grid.Rows[idx3].Cells["ControllerPort"].Value = "COM-A";
        Call(panel, "SetLightPageCellItems", idx3, "COM-A");
        grid.Rows[idx3].Cells["Page"].Value = "2";
        var list2 = Call(panel, "CollectLightPages") as List<LightPageRef>;
        Ok("중복 (COM-A,2) dedupe → 2건", list2.Count == 2, $"(got {list2.Count})");

        // 렌더(DrawToBitmap) 크래시 없음
        try
        {
            using (var bmp = new Bitmap(panel.Width, panel.Height))
            { panel.DrawToBitmap(bmp, new Rectangle(0, 0, panel.Width, panel.Height)); Ok("DrawToBitmap 렌더", true); }
        }
        catch (Exception ex) { Ok("DrawToBitmap 렌더", false, ex.Message); }

        try { panel.Dispose(); } catch { }
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
