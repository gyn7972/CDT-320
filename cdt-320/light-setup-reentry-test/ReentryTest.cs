using System;
using System.Reflection;
using System.Windows.Forms;

// LightSystemSetupPage 결선 그리드 재진입 예외 검증 하니스.
// 시퀀스: 알고리즘 노드 선택 → 결선행 추가 → ChannelsCsv 셀 편집 중 → 다른 노드로 전환.
// 기대: 예외 없음 (BindSetsGrid BeginInvoke 수정 후).
class ReentryTest
{
    static Exception _threadEx;
    static string _result = "RESULT: (test did not complete)";

    [STAThread]
    static int Main(string[] args)
    {
        string exePath = args.Length > 0 ? args[0]
            : @"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe";
        Application.EnableVisualStyles();
        Application.ThreadException += (s, e) => { if (_threadEx == null) _threadEx = e.Exception; };

        var asm = Assembly.LoadFrom(exePath);
        var t = asm.GetType("QMC.Vision.Ui.Pages.LightSystemSetupPage");
        if (t == null) { Console.WriteLine("RESULT: TYPE NOT FOUND"); return 2; }

        var bf = BindingFlags.NonPublic | BindingFlags.Instance;
        var page = (Control)Activator.CreateInstance(t);
        var form = new Form { Width = 1100, Height = 760, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) };
        page.Dock = DockStyle.Fill;
        form.Controls.Add(page);

        form.Shown += (s, e) =>
        {
            try { RunScenario(t, bf, page); }
            catch (Exception ex)
            {
                var inner = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                _result = "RESULT: DIRECT EXCEPTION: " + inner.GetType().Name + ": " + inner.Message;
            }
            finally
            {
                if (_threadEx != null && _result.StartsWith("RESULT: PASS"))
                    _result = "RESULT: THREAD EXCEPTION: " + _threadEx.GetType().Name + ": " + _threadEx.Message;
                form.Close();
            }
        };
        Application.Run(form);
        Console.WriteLine(_result);
        return _result.StartsWith("RESULT: PASS") ? 0 : 1;
    }

    static void RunScenario(Type t, BindingFlags bf, Control page)
    {
        var tree = (TreeView)t.GetField("_treeWiring", bf).GetValue(page);
        var grid = (DataGridView)t.GetField("_gridSets", bf).GetValue(page);
        if (tree == null || grid == null) { _result = "RESULT: FIELDS NOT FOUND"; return; }
        if (tree.Nodes.Count < 2) { _result = "RESULT: NEED >=2 ALG NODES, got " + tree.Nodes.Count; return; }

        // 1) 첫 알고리즘 노드 선택
        tree.SelectedNode = tree.Nodes[0];
        Pump();

        // 2) 결선행 추가 (AddControllerSet) → gridSets 에 편집 가능 행 생성
        var addMethod = t.GetMethod("AddControllerSet", bf);
        addMethod.Invoke(page, null);
        Pump();

        // BindSetsGrid 가 BeginInvoke 라 한 박자 뒤 채워짐 — 펌프 추가
        Pump(); Pump();
        if (grid.Rows.Count == 0 || (grid.Rows.Count == 1 && grid.Rows[0].IsNewRow))
        { _result = "RESULT: NO EDITABLE ROW after AddControllerSet (rows=" + grid.Rows.Count + ")"; return; }

        // 3) ChannelsCsv 셀 편집 모드 진입 + 값 입력 (편집 중 상태 구성)
        grid.CurrentCell = grid.Rows[0].Cells["ChannelsCsv"];
        grid.BeginEdit(true);
        var tb = grid.EditingControl as TextBox;
        if (tb != null) { tb.Text = "3,4"; }
        Pump();
        bool wasEditing = grid.IsCurrentCellInEditMode;

        // 4) 편집 중인 상태에서 다른 알고리즘 노드로 전환 (재진입 트리거 시퀀스)
        tree.SelectedNode = tree.Nodes[1];
        Pump(); Pump(); Pump();   // AfterSelect → BindSetsGrid(BeginInvoke) → BindSetsGridCore 완료까지

        // 5) 빠른 연속 전환 (중복 스케줄 가드 확인)
        for (int i = 0; i < tree.Nodes.Count; i++) { tree.SelectedNode = tree.Nodes[i]; Pump(); }
        Pump(); Pump();

        _result = "RESULT: PASS (editingAtSwitch=" + wasEditing + ", finalRows=" + grid.Rows.Count + ")";
    }

    static void Pump()
    {
        Application.DoEvents();
        System.Threading.Thread.Sleep(15);
        Application.DoEvents();
    }
}
