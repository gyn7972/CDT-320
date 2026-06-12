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

// C3b-3 버그수정 검증 — 페이지 콤보를 실제 편집 컨트롤로 선택하면 셀 값 커밋 후 page 1 로 저장되는지(0 고정 버그).
class AssignPageEditSmoke
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
        LightSystemSetupStore.SetCurrent(new LightSystemSetup
        {
            Controllers = new List<LightControllerEntry> {
                new LightControllerEntry { PortName = "COM-A", Name = "A", ChannelCount = 8, PageCount = 3, MaxPower = 240 } }
        });

        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings(); mod.LoadRecipe("default");
        var node = mod.Algorithms.FirstOrDefault(a => a.Finder != null);
        var sb = node.Setup as AlgoSetupBase;
        sb.LightPages = new List<LightPageRef> { new LightPageRef { ControllerPort = "COM-A", Page = 0 } };

        var panel = new InspectionLightAssignPanel { Width = 800, Height = 500 };
        // Form 에 올려 핸들 + 편집 컨트롤 활성화
        var form = new Form { Width = 820, Height = 540 };
        form.Controls.Add(panel);
        form.Show();
        form.Left = -2000;   // 화면 밖
        Application.DoEvents();

        panel.SelectInspection(node, "Wafer", node.Finder.Id);
        Application.DoEvents();

        var grid = (DataGridView)typeof(InspectionLightAssignPanel)
            .GetField("_grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
        var row = grid.Rows.Cast<DataGridViewRow>().First(r => !r.IsNewRow);

        // 페이지 셀을 실제 편집: BeginEdit → 편집 콤보 SelectedItem=2 → CommitEdit-on-dirty 가 셀에 반영
        grid.CurrentCell = row.Cells["Page"];
        grid.BeginEdit(true);
        Application.DoEvents();
        var editCombo = grid.EditingControl as ComboBox;
        Ok("페이지 편집 콤보 표시", editCombo != null);
        Ok("편집 콤보 items=PageCount 3", editCombo != null && editCombo.Items.Count == 3, $"(got {editCombo?.Items.Count})");
        if (editCombo != null) editCombo.SelectedItem = "2";   // page 2 선택(미커밋 상태)
        Application.DoEvents();

        // 커밋되었는지(셀 값 = 2) — 버그면 0
        int cellVal = 0; int.TryParse(row.Cells["Page"].Value?.ToString(), out cellVal);
        Ok("선택 즉시 셀 값 커밋(=2, 0 아님)", cellVal == 2, $"(got {cellVal})");
        // ★ 표시값(FormattedValue = 화면 텍스트) = 2 — 콤보 셀이 값을 표시 못 하면 빈칸/0(표시 버그)
        grid.EndEdit();
        string disp = row.Cells["Page"].FormattedValue?.ToString();
        Ok("표시값(FormattedValue)=2 (화면에 2 표시)", disp == "2", $"(got '{disp}')");

        // 저장(PersistAssign) — 미커밋이어도 EndEdit 로 확정되어야
        var persist = typeof(InspectionLightAssignPanel).GetMethod("PersistAssign", BindingFlags.NonPublic | BindingFlags.Instance);
        persist.Invoke(panel, new object[] { true });
        Ok("저장 후 LightPages page=2", sb.LightPages.Count == 1 && sb.LightPages[0].Page == 2, $"(got {(sb.LightPages.Count>0?sb.LightPages[0].Page:-1)})");

        // 재로드 영속
        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings();
        var n2 = mod2.Algorithms.FirstOrDefault(a => a.Finder != null && a.StorageKey == node.StorageKey);
        Ok("재로드 page=2 복원", (n2.Setup as AlgoSetupBase).LightPages.Any(p => p.Page == 2));

        try { form.Close(); form.Dispose(); mod.Dispose(); mod2.Dispose(); } catch { }
        Cleanup();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
