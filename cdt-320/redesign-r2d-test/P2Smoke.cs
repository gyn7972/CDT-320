using System;
using System.IO;
using QMC.Vision.Core.Parameters;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Core.Inspectors;

// P2 왕복 테스트 — 값 변경 → SaveLayer → 새 store LoadAll → 복원 확인(G1 해소 입증, Sim 에서도 파일 생성).
class P2Smoke
{
    static int _fail = 0;
    static void Chk(string name, object expect, object actual)
    {
        bool ok = Math.Abs(Convert.ToDouble(expect) - Convert.ToDouble(actual)) < 1e-6;
        if (!ok) _fail++;
        Console.WriteLine($"  [{(ok ? "OK" : "FAIL")}] {name}: expect={expect} actual={actual}");
    }

    static int Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "p2smoke_" + Guid.NewGuid().ToString("N").Substring(0, 6));
        string setup = Path.Combine(dir, "vision_setup.json");
        string product = "p2test";

        const string FT = "WaferVision/ReticleFinder";
        const string IT = "BottomInspection/SurfaceInspector";   // SimInspector(ROI) + ②Bottom(Threshold) 공존

        // ── store1: 값 변경 + 저장 ──
        var f1 = new SimPatternFinder(FT);
        var i1 = new SimInspector(IT);
        var b1 = new BottomInspectionParameters();
        var s1 = new ParameterStore { SetupFilePath = setup, Product = product };
        s1.Register(f1, ParameterChannel.Snapshot);
        s1.Register(i1, ParameterChannel.Snapshot);
        s1.Register(b1, ParameterChannel.Snapshot);   // ② Threshold 등(인스펙터에 없는 키)

        s1.SetValue(FT, "SearchRoi.CenterX", 111.0);   // Setup
        s1.SetValue(FT, "TrainRoi.CenterX", 222.0);    // Recipe
        s1.SetValue(FT, "AcceptThreshold", 0.66);      // Recipe
        s1.SetValue(IT, "InspectionRoi.CenterX", 333.0); // Setup
        s1.SetValue(IT, "Threshold", 77.0);            // ② Bottom Recipe (Sim inspector 엔 없음)
        s1.SaveLayer(ParameterLayer.Setup);
        s1.SaveLayer(ParameterLayer.Recipe);

        Console.WriteLine("saved → " + dir);
        Console.WriteLine("  setup exists=" + File.Exists(setup));
        string recDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", product);
        Console.WriteLine("  recipe files=" + (Directory.Exists(recDir) ? string.Join(",", Directory.GetFiles(recDir, "*.recipe.json").Length.ToString()) : "0"));

        // ── store2: 새 인스턴스(기본값) + LoadAll → 복원 ──
        var f2 = new SimPatternFinder(FT);
        var i2 = new SimInspector(IT);
        var b2 = new BottomInspectionParameters();
        var s2 = new ParameterStore { SetupFilePath = setup, Product = product };
        s2.Register(f2, ParameterChannel.Snapshot);
        s2.Register(i2, ParameterChannel.Snapshot);
        s2.Register(b2, ParameterChannel.Snapshot);
        s2.LoadAll();

        Chk("finder SearchRoi.CenterX (Setup)", 111.0, f2.SearchRoi.CenterX);
        Chk("finder TrainRoi.CenterX (Recipe)", 222.0, f2.TrainRoi.CenterX);
        Chk("finder AcceptThreshold (Recipe)", 0.66, f2.AcceptThreshold);
        Chk("inspector InspectionRoi.CenterX (Setup)", 333.0, i2.InspectionRoi.CenterX);
        Chk("② Bottom Threshold (Recipe)", 77.0, b2.Threshold);

        // 정리(테스트 파일)
        try { Directory.Delete(dir, true); } catch { }
        try { if (Directory.Exists(recDir)) Directory.Delete(recDir, true); } catch { }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
