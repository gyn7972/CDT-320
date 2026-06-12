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

// C3b-3 — 실제 흐름 재현: 빈 지정 → 새 행 추가 → 컨트롤러 선택(Page 0 리셋) → 페이지 선택 → 저장.
// 페이지가 0 으로 떨어지는지(저장 후 LightPages.Page) 확인.
class AssignNewRowSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }
    static void Clean()
    {
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData"), true); } catch { }
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
    }

    [STAThread]
    static int Main()
    {
        Application.EnableVisualStyles();
        Clean();
        LightSystemSetupStore.SetCurrent(new LightSystemSetup
        {
            Controllers = new List<LightControllerEntry> {
                new LightControllerEntry { PortName = "COM-A", Name = "A", ChannelCount = 8, PageCount = 3, MaxPower = 240 } }
        });

        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings(); mod.LoadRecipe("default");
        var node = mod.Algorithms.FirstOrDefault(a => a.Finder != null);
        (node.Setup as AlgoSetupBase).LightPages = new List<LightPageRef>();   // 빈 지정

        var panel = new InspectionLightAssignPanel { Width = 800, Height = 500 };
        var form = new Form { Width = 820, Height = 540 };
        form.Controls.Add(panel);
        form.Show(); form.Left = -2000;
        Application.DoEvents();
        panel.SelectInspection(node, "Wafer", node.Finder.Id);
        Application.DoEvents();

        var grid = (DataGridView)typeof(InspectionLightAssignPanel)
            .GetField("_grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);

        // ── 새 행: 컨트롤러 COM-A 선택 (편집 컨트롤 경유) ──
        // 새 행(IsNewRow)의 ControllerPort 셀 진입 → 편집 → COM-A
        grid.CurrentCell = grid.Rows[grid.NewRowIndex].Cells["ControllerPort"];
        grid.BeginEdit(true); Application.DoEvents();
        var ctrlCombo = grid.EditingControl as ComboBox;
        Ok("컨트롤러 편집 콤보 표시", ctrlCombo != null);
        if (ctrlCombo != null) ctrlCombo.SelectedItem = "COM-A";   // dirty → CommitEdit
        Application.DoEvents();
        grid.EndEdit();   // → OnCellEndEdit(ControllerPort): Page 0 리셋 + PersistAssign
        Application.DoEvents();

        int dataRows = grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
        Ok("행 1개 생성(COM-A)", dataRows == 1, $"(got {dataRows})");
        var row = grid.Rows.Cast<DataGridViewRow>().First(r => !r.IsNewRow);

        // ── 페이지 2 선택 (편집 컨트롤 경유) ──
        grid.CurrentCell = row.Cells["Page"];
        grid.BeginEdit(true); Application.DoEvents();
        var pageCombo = grid.EditingControl as ComboBox;
        Ok("페이지 편집 콤보 표시 + items=3", pageCombo != null && pageCombo.Items.Count == 3, $"(items {pageCombo?.Items.Count})");
        if (pageCombo != null) pageCombo.SelectedItem = "2";   // dirty → CommitEdit
        Application.DoEvents();
        int cellNow = 0; int.TryParse(row.Cells["Page"].Value?.ToString(), out cellNow);
        Ok("선택 즉시 셀=2", cellNow == 2, $"(got {cellNow})");

        // ── 저장 버튼 클릭 ──
        var saveBtn = (Button)typeof(InspectionLightAssignPanel).GetField("_btnSave", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
        saveBtn.PerformClick();   // → PersistAssign(true)
        Application.DoEvents();

        var sb = node.Setup as AlgoSetupBase;
        Ok("저장 후 LightPages 1건", sb.LightPages.Count == 1, $"(got {sb.LightPages.Count})");
        Ok("저장 후 page=2 (0 아님)", sb.LightPages.Count == 1 && sb.LightPages[0].Page == 2,
            $"(got {(sb.LightPages.Count > 0 ? sb.LightPages[0].Page : -1)})");

        try { form.Close(); form.Dispose(); mod.Dispose(); } catch { }
        Clean();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
