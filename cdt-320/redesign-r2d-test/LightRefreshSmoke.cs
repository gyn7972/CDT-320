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

// 조명 지정 모듈 이전 후 — 레벨 그리드 재바인딩(RefreshLightAssignment 효과)이 지정 변화에 따라 채널을 다시 불러오는지.
//  지정(LightPages)은 모듈 Setup(런타임 Form1 해석). 헤드리스에선 ActivePages 가 검사 Recipe 레벨로 폴백 →
//  노드 Recipe 레벨 (Port,Page) 유무로 재바인딩 검증(SelectInspection 재호출 = RefreshLightAssignment 효과).
class LightRefreshSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }
    static void Clean()
    {
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData"), true); } catch { }
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
    }

    static int Rows(DataGridView g) => g.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        Clean();
        LightSystemSetupStore.SetCurrent(new LightSystemSetup
        {
            Controllers = new List<LightControllerEntry> {
                new LightControllerEntry { PortName = "COM-A", Name = "A", ChannelCount = 6, PageCount = 1, MaxPower = 240 } }
        });

        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings(); mod.LoadRecipe("default");
        var node = mod.Algorithms.FirstOrDefault(a => a.Finder != null);
        var rec = node.Recipe as AlgoRecipeBase;
        rec.LightSettings = new List<InspectionLightSetting>();   // 초기: 지정/레벨 없음

        // 레벨 패널 — 최초 바인딩(지정 없음 → 빈 그리드)
        var panel = new InspectionLightPanel { EmbeddedMode = true };
        panel.SelectInspection(node, "Wafer", node.Finder.Id);
        var grid = (DataGridView)typeof(InspectionLightPanel)
            .GetField("_grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
        Ok("초기 지정 없음 → 그리드 빈", Rows(grid) == 0, $"(got {Rows(grid)})");

        // 지정 추가 모사(노드 Recipe 레벨 (COM-A,page0) → ActivePages 폴백이 채널 열거)
        rec.LightSettings = new List<InspectionLightSetting> {
            new InspectionLightSetting { ControllerPort = "COM-A", Page = 0, Channel = 1, Level = 0 } };

        // RefreshLightAssignment 효과 = SelectInspection 재호출(재바인딩)
        panel.SelectInspection(node, "Wafer", node.Finder.Id);
        Ok("재바인딩 후 그리드=COM-A ChannelCount 6", Rows(grid) == 6, $"(got {Rows(grid)})");
        var ch1 = grid.Rows.Cast<DataGridViewRow>().First(r => !r.IsNewRow);
        Ok("행 컨트롤러=COM-A", (ch1.Cells["Ctrl"].Value as string ?? "").StartsWith("COM-A"));

        // 지정 제거 → 재바인딩 → 다시 빈
        rec.LightSettings = new List<InspectionLightSetting>();
        panel.SelectInspection(node, "Wafer", node.Finder.Id);
        Ok("지정 제거 후 재바인딩 → 빈", Rows(grid) == 0, $"(got {Rows(grid)})");

        try { panel.Dispose(); mod.Dispose(); } catch { }
        Clean();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
