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

// 조명 지정 모듈 이전 후 — RecipePage 레벨 그리드(InspectionLightPanel):
//  지정(LightPages)은 모듈 Setup 이며 런타임 Form1 해석. 헤드리스에선 ActivePages 가 검사 Recipe 레벨로 폴백.
//  ⇒ 노드 Recipe 에 (COM-SIM,page0) 레벨 시드 → 채널 ChannelCount(8) 행 → 레벨 편집→저장→복원.
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

        // 1) 컨트롤러 정의(ChannelCount=8 = 채널 열거 출처).
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

        // 2) 노드 Recipe 에 (COM-SIM,page0) 레벨 시드 — ActivePages 폴백이 이 (Port,Page)로 채널 열거.
        var recipe = node.Recipe as AlgoRecipeBase;
        recipe.LightSettings = new List<InspectionLightSetting> {
            new InspectionLightSetting { ControllerPort = "COM-SIM", Page = 0, Channel = 1, Level = 0 } };
        node.SaveSettings();

        // 3) 패널 — 지정 컨트롤러의 ChannelCount(8) 만큼 행(폴백 경로)
        var panel = new InspectionLightPanel();
        panel.SelectInspection(node, "Wafer", node.Finder.Id);
        var grid = (DataGridView)typeof(InspectionLightPanel)
            .GetField("_grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
        int dataRows = grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
        Ok("그리드 행=ChannelCount 8", dataRows == 8, $"(got {dataRows})");

        // 4) ch1=150, 나머지 0(미사용) 편집 → 저장
        foreach (DataGridViewRow r in grid.Rows)
        {
            if (r.IsNewRow) continue;
            int ch = int.Parse(r.Cells["Channel"].Value.ToString());
            r.Cells["Level"].Value = ch == 1 ? 150 : 0;
        }
        panel.PersistLight();
        var s1 = recipe.LightSettings.FirstOrDefault(s => s.Channel == 1);
        Ok("ch1 Level=150 노드 Recipe 기록", s1 != null && s1.Level == 150 && s1.ControllerPort == "COM-SIM" && s1.Page == 0, $"(got {s1?.Level})");
        Ok("미사용(0) 채널 제거됨", recipe.LightSettings.All(s => s.Channel == 1));

        // 5) 재로드 → 영속
        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings(); mod2.LoadRecipe("default");
        var node2 = mod2.Algorithms.FirstOrDefault(a => a.Finder != null && a.StorageKey == node.StorageKey);
        Ok("재로드 ch1 Level=150 복원", (node2.Recipe as AlgoRecipeBase).LightSettings.Any(s => s.Channel == 1 && s.Level == 150));

        try { panel.Dispose(); mod.Dispose(); mod2.Dispose(); } catch { }
        Cleanup();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
