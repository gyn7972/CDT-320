using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Ui.Pages;

// C3b-3 Step4 — InspectionLightAssignPanel: 노드 LightPages 바인딩 → 그리드, 행 추가/편집 → 저장 → 노드 Setup 반영.
class AssignPanelSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }
    static void Cleanup()
    {
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData"), true); } catch { }
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
    }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        Cleanup();

        var setup = new LightSystemSetup
        {
            Controllers = new List<LightControllerEntry> {
                new LightControllerEntry { PortName = "COM-A", Name = "A", ChannelCount = 8, PageCount = 2, MaxPower = 240 },
                new LightControllerEntry { PortName = "COM-B", Name = "B", ChannelCount = 4, PageCount = 1, MaxPower = 240 } }
        };
        LightSystemSetupStore.SetCurrent(setup);

        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings(); mod.LoadRecipe("default");
        var node = mod.Algorithms.FirstOrDefault(a => a.Finder != null);
        var sb = node.Setup as AlgoSetupBase;
        sb.LightPages = new List<LightPageRef> { new LightPageRef { ControllerPort = "COM-A", Page = 1 } };

        var panel = new InspectionLightAssignPanel();
        panel.SelectInspection(node, "Wafer", node.Finder.Id);

        var grid = (DataGridView)typeof(InspectionLightAssignPanel)
            .GetField("_grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
        int rows = grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
        Ok("초기 그리드 행=LightPages 1", rows == 1, $"(got {rows})");
        var r0 = grid.Rows.Cast<DataGridViewRow>().First(r => !r.IsNewRow);
        Ok("행0 = COM-A / page1", (r0.Cells["ControllerPort"].Value as string) == "COM-A"
            && r0.Cells["Page"].Value?.ToString() == "1");

        // 행 추가 (COM-B page0) — 프로그램적
        int idx = grid.Rows.Add();
        grid.Rows[idx].Cells["ControllerPort"].Value = "COM-B";
        grid.Rows[idx].Cells["Page"].Value = "0";

        // 저장(PersistAssign) — private, reflection 호출
        var persist = typeof(InspectionLightAssignPanel).GetMethod("PersistAssign", BindingFlags.NonPublic | BindingFlags.Instance);
        persist.Invoke(panel, new object[] { true });

        Ok("저장 후 LightPages 2건", sb.LightPages.Count == 2, $"(got {sb.LightPages.Count})");
        Ok("COM-A p1 + COM-B p0 기록",
            sb.LightPages.Any(p => p.ControllerPort == "COM-A" && p.Page == 1)
            && sb.LightPages.Any(p => p.ControllerPort == "COM-B" && p.Page == 0));

        // 재로드 영속
        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings();
        var node2 = mod2.Algorithms.FirstOrDefault(a => a.Finder != null && a.StorageKey == node.StorageKey);
        var sb2 = node2.Setup as AlgoSetupBase;
        Ok("재로드 LightPages 2건 복원", sb2.LightPages.Count == 2);

        // 페이지 클램프: COM-B(PageCount=1)에 page5 넣으면 0 으로 클램프
        grid.Rows.Cast<DataGridViewRow>().First(r => (r.Cells["ControllerPort"].Value as string) == "COM-B").Cells["Page"].Value = "1";
        persist.Invoke(panel, new object[] { false });
        Ok("COM-B page 클램프(PageCount=1→0)", sb.LightPages.First(p => p.ControllerPort == "COM-B").Page == 0);

        try { panel.Dispose(); mod.Dispose(); mod2.Dispose(); } catch { }
        Cleanup();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
