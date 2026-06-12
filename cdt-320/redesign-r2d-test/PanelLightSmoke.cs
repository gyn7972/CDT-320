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

// C3b-3 — 결선 폐기 후: 노드 Setup.LightPages(컨트롤러/페이지 지정) → 채널 ChannelCount 행 → 레벨 편집→저장→복원.
//  + MigrateLightPages: 기존 Recipe.LightSettings(Port,Page) → LightPages 도출.
class PanelLightSmoke
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

        // 1) 컨트롤러 정의 주입(ChannelCount=8 = 채널 열거 출처). 결선(AlgorithmWirings) 없음.
        var setup = new LightSystemSetup
        {
            Controllers = new List<LightControllerEntry> {
                new LightControllerEntry { PortName = "COM-SIM", Name = "SimCtrl", ChannelCount = 8, MaxPower = 240, PageCount = 1 } }
        };
        LightSystemSetupStore.SetCurrent(setup);

        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings(); mod.LoadRecipe("default");
        var node = mod.Algorithms.FirstOrDefault(a => a.Finder != null);
        Ok("finder 노드", node != null);

        // 2) 노드 Setup 에 컨트롤러/페이지 지정 + 저장 (SettingsPage UI(Step4) 가 할 일을 모사)
        var setupBase = node.Setup as AlgoSetupBase;
        setupBase.LightPages = new List<LightPageRef> { new LightPageRef { ControllerPort = "COM-SIM", Page = 0 } };
        node.SaveSettings();

        // 3) 패널 — 지정 컨트롤러의 ChannelCount(8) 만큼 행
        var panel = new InspectionLightPanel();
        panel.SelectInspection(node, "Wafer", node.Finder.Id);
        var grid = (DataGridView)typeof(InspectionLightPanel)
            .GetField("_grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
        int dataRows = grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
        Ok("그리드 행=ChannelCount 8 (결선 아님)", dataRows == 8, $"(got {dataRows})");

        // 4) ch1=150, 나머지 0(미사용) 편집 → 저장
        foreach (DataGridViewRow r in grid.Rows)
        {
            if (r.IsNewRow) continue;
            int ch = int.Parse(r.Cells["Channel"].Value.ToString());
            r.Cells["Level"].Value = ch == 1 ? 150 : 0;
        }
        panel.PersistLight();
        var recipe = node.Recipe as AlgoRecipeBase;
        var s1 = recipe.LightSettings.FirstOrDefault(s => s.Channel == 1);
        Ok("ch1 Level=150 노드 Recipe 기록", s1 != null && s1.Level == 150 && s1.ControllerPort == "COM-SIM" && s1.Page == 0, $"(got {s1?.Level})");
        Ok("미사용(0) 채널 제거됨", recipe.LightSettings.All(s => s.Channel == 1));

        // 5) 재로드 → 영속
        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings(); mod2.LoadRecipe("default");
        var node2 = mod2.Algorithms.FirstOrDefault(a => a.Finder != null && a.StorageKey == node.StorageKey);
        Ok("재로드 ch1 Level=150 복원", (node2.Recipe as AlgoRecipeBase).LightSettings.Any(s => s.Channel == 1 && s.Level == 150));
        Ok("재로드 LightPages(지정) 복원(COM-SIM p0)", (node2.Setup as AlgoSetupBase).LightPages.Any(p => p.ControllerPort == "COM-SIM" && p.Page == 0));

        // 6) MigrateLightPages — LightPages 비우고 LightSettings 만 있을 때 도출
        var sb2 = node2.Setup as AlgoSetupBase;
        sb2.LightPages = new List<LightPageRef>();   // 지정 제거(마이그 전 상태 모사)
        bool changed = mod2.MigrateLightPages();
        Ok("MigrateLightPages 변경 발생", changed);
        Ok("마이그 LightPages 도출(COM-SIM p0)", sb2.LightPages.Any(p => p.ControllerPort == "COM-SIM" && p.Page == 0));

        try { panel.Dispose(); mod.Dispose(); mod2.Dispose(); } catch { }
        Cleanup();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
