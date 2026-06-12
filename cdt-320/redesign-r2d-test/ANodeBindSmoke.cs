using System;
using System.IO;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Backends.Cognex;

// A — 노드 POCO ↔ 런타임 왕복 반영 테스트: 편집 → SaveRecipe → 임의변경 → LoadRecipe → 런타임 복원(Apply).
class ANodeBindSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }
    const string R = "Ttest_A";

    static int Main()
    {
        // ── Finder ──
        var f = new SimPatternFinder("WaferVision/EjectPinFinder");
        var fnode = new FinderAlgorithm<FinderAlgoSetup, FinderAlgoConfig, FinderAlgoRecipe>("WaferVision.EjectPinFinder", f);
        f.SearchRoi.CenterX = 111; f.TrainRoi.CenterX = 222; f.AcceptThreshold = 0.9; f.MaxInstances = 3;
        fnode.SaveRecipe(R);      // Collect(Recipe) + 파일
        fnode.SaveSettings();     // Collect(Config: MaxInstances)

        f.SearchRoi.CenterX = 999; f.TrainRoi.CenterX = 999; f.AcceptThreshold = 0.1; f.MaxInstances = 9;  // 임의 변경
        fnode.LoadRecipe(R);      // 파일 → Recipe → Apply
        fnode.LoadSettings();     // Config → Apply

        Ok("finder SearchRoi 복원(111)", Math.Abs(f.SearchRoi.CenterX - 111) < 1e-6, $"(got {f.SearchRoi.CenterX})");
        Ok("finder TrainRoi 복원(222)",  Math.Abs(f.TrainRoi.CenterX - 222) < 1e-6, $"(got {f.TrainRoi.CenterX})");
        Ok("finder AcceptThreshold 복원(0.9)", Math.Abs(f.AcceptThreshold - 0.9) < 1e-6, $"(got {f.AcceptThreshold})");
        Ok("finder MaxInstances 복원(3, Config)", f.MaxInstances == 3, $"(got {f.MaxInstances})");

        string fpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", R, "WaferVision.EjectPinFinder.recipe.json");
        Ok("Recipe 파일 생성", File.Exists(fpath), fpath);

        // ── Inspector (Cognex Threshold 백엔드한정) ──
        var ins = new CognexInspector("BottomInspection/SurfaceInspector", null);
        var inode = new InspectorAlgorithm<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("BottomInspection.SurfaceInspector", ins);
        ins.InspectionRoi.CenterX = 333; ins.Threshold = 200;
        inode.SaveRecipe(R);

        ins.InspectionRoi.CenterX = 0; ins.Threshold = 50;   // 임의 변경
        inode.LoadRecipe(R);

        Ok("inspector InspectionRoi 복원(333)", Math.Abs(ins.InspectionRoi.CenterX - 333) < 1e-6, $"(got {ins.InspectionRoi.CenterX})");
        Ok("inspector Cognex Threshold 복원(200)", ins.Threshold == 200, $"(got {ins.Threshold})");

        // 정리
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", R), true); } catch { }
        try { File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData", "Config", "WaferVision.EjectPinFinder.json")); } catch { }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
