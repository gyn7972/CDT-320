using System;
using System.IO;
using System.Linq;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Cameras.Sim;

// B — UI 배선 경로(모듈→노드 해석→Save/Load)가 실 finder 에 왕복 반영되는지.
class BUiWireSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static int Main()
    {
        var mod = new WaferVisionModule(new SimCamera("Sim/Wafer"), new SimBackend());
        var f = mod.Finders["EjectPinFinder"];
        var node = mod.Algorithms.FirstOrDefault(a => a.Finder == f);   // 페이지 해석 방식
        Ok("노드 해석(Algorithms.First a.Finder==f)", node != null);
        Ok("GetAlgorithm 일치", ReferenceEquals(node, mod.GetAlgorithm("EjectPinFinder")));

        // 그리드 편집 모사 → 런타임 finder 에 직접 쓰기
        f.SearchRoi.CenterX = 111; f.TrainRoi.CenterX = 222; f.AcceptThreshold = 0.85; f.MaxInstances = 4;
        node.SaveSettings();             // Config(MaxInstances)
        node.SaveRecipe("default");      // Recipe(SearchRoi/TrainRoi/AcceptThreshold)

        f.SearchRoi.CenterX = 0; f.TrainRoi.CenterX = 0; f.AcceptThreshold = 0.1; f.MaxInstances = 9;  // 임의 변경
        node.LoadSettings();
        node.LoadRecipe("default");      // Apply → finder 복원

        Ok("finder SearchRoi 복원(111)", Math.Abs(f.SearchRoi.CenterX - 111) < 1e-6, $"(got {f.SearchRoi.CenterX})");
        Ok("finder TrainRoi 복원(222)", Math.Abs(f.TrainRoi.CenterX - 222) < 1e-6, $"(got {f.TrainRoi.CenterX})");
        Ok("finder AcceptThreshold 복원(0.85)", Math.Abs(f.AcceptThreshold - 0.85) < 1e-6, $"(got {f.AcceptThreshold})");
        Ok("finder MaxInstances 복원(4)", f.MaxInstances == 4, $"(got {f.MaxInstances})");

        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default", "WaferVision.EjectPinFinder.recipe.json");
        Ok("Recipe 파일 생성(경로 일치)", File.Exists(path), path);

        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
        try { File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData", "Config", "WaferVision.EjectPinFinder.json")); } catch { }
        try { mod.Dispose(); } catch { }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
