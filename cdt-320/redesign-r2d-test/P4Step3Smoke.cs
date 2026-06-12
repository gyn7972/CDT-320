using System;
using System.IO;
using System.Linq;
using QMC.Vision.Core.Parameters;
using QMC.Vision.Core.Inspectors;
using QMC.Vision.Ui.Controls;

// P4 Step3 — 에디터 흡수 검증: orphan ②(VisionScale)도 그리드로 편집·저장·복원(기능 누락 없음).
class P4Step3Smoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static int Main()
    {
        string setup = Path.Combine(Path.GetTempPath(), "p4s3_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".json");
        string product = "p4s3";
        string T = InspectionParamRegistry.ByTool("VisionScale").Target;   // "VisionScale" (orphan)

        // store1: VisionScale② 등록 → 그리드 아이템 생성·편집·저장
        var vs1 = (VisionScaleParameters)InspectionParamRegistry.ByTool("VisionScale").Create();
        var s1 = new ParameterStore { SetupFilePath = setup, Product = product };
        s1.Register(vs1, ParameterChannel.Snapshot);
        ParameterStoreHost.Current = s1;

        var items = s1.GetByTarget(T).Select(d => ParameterGridItem.FromDescriptor(d, s1)).Where(x => x != null).ToList();
        Ok("orphan ② VisionScale 그리드 아이템 생성(4)", items.Count == 4, $"(got {items.Count})");

        var cw = items.FirstOrDefault(i => i.DisplayName == "Chip Width");
        Ok("Chip Width 아이템 존재(SCOPE=Recipe)", cw != null && cw.Scope == ParameterGridScope.Recipe);
        cw.Setter(2.5);                              // 그리드 편집 → store 경유
        Ok("그리드 편집 → vs.ChipWidth 반영", Math.Abs(vs1.ChipWidth - 2.5) < 1e-6);
        s1.SaveTarget(T);                            // SettingsPage 저장 버튼 동작

        // store2: 새 인스턴스 + LoadTarget → 복원(기능 누락 없음)
        var vs2 = (VisionScaleParameters)InspectionParamRegistry.ByTool("VisionScale").Create();
        var s2 = new ParameterStore { SetupFilePath = setup, Product = product };
        s2.Register(vs2, ParameterChannel.Snapshot);
        s2.LoadTarget(T);
        Ok("저장→복원: vs2.ChipWidth=2.5", Math.Abs(vs2.ChipWidth - 2.5) < 1e-6);

        try { File.Delete(setup); } catch { }
        string recDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", product);
        try { if (Directory.Exists(recDir)) Directory.Delete(recDir, true); } catch { }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
