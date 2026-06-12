using System;
using System.IO;
using System.Linq;
using QMC.Vision.Core.Parameters;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Config;
using QMC.Common.Recipes;
using QMC.Vision.Ui.Controls;

// P4-fix — 조명 디스크립터 그리드 제외 + 조명 스토어/저장·복원 유지.
class P4FixSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static int Main()
    {
        string setup = Path.Combine(Path.GetTempPath(), "p4fix_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".json");
        string product = "p4fix";
        const string T = "BottomInspection/SurfaceInspector";

        var insp = new SimInspector(T);                                   // InspectionRoi 4 (General)
        var ls = new InspectionLightSetting { ControllerPort = "COM3", Channel = 2, Level = 10 };
        var light = new LightingParameters(T, ls);                        // 7 (Lighting)

        var s1 = new ParameterStore { SetupFilePath = setup, Product = product };
        s1.Register(insp, ParameterChannel.Snapshot);
        s1.Register(light, ParameterChannel.Snapshot);                    // 테스트: 저장 확인 위해 Snapshot
        ParameterStoreHost.Current = s1;

        // 그리드 필터(조명 제외)
        var gridItems = s1.GetByTarget(T)
            .Where(d => d.Domain != ParameterDomain.Lighting)
            .Select(d => ParameterGridItem.FromDescriptor(d, s1))
            .Where(x => x != null).ToList();
        Ok("그리드: 조명 행 제외(InspectionRoi 4만)", gridItems.Count == 4, $"(got {gridItems.Count})");
        Ok("그리드: 조명 DisplayName 없음(Level/Channel/Strobe)",
           !gridItems.Any(i => i.DisplayName == "Level" || i.DisplayName == "Channel" || i.DisplayName.Contains("Strobe")));

        // 스토어엔 조명 디스크립터 유지(7)
        int lightCount = s1.GetByTarget(T).Count(d => d.Domain == ParameterDomain.Lighting);
        Ok("스토어: 조명 디스크립터 유지(7)", lightCount == 7, $"(got {lightCount})");

        // 조명 편집(전용패널 경로 모사) → 저장 → 복원
        s1.SetValue(T, "COM3/2.Level", 88.0);
        Ok("조명 편집 → setting.Level 반영", ls.Level == 88);
        Ok("조명 편집 → store dirty(타깃)", s1.IsDirty(T));
        s1.SaveTarget(T);

        var ls2 = new InspectionLightSetting { ControllerPort = "COM3", Channel = 2 };  // 기본 Level 0
        var light2 = new LightingParameters(T, ls2);
        var insp2 = new SimInspector(T);
        var s2 = new ParameterStore { SetupFilePath = setup, Product = product };
        s2.Register(insp2, ParameterChannel.Snapshot);
        s2.Register(light2, ParameterChannel.Snapshot);
        s2.LoadTarget(T);
        Ok("저장→복원: 조명 Level=88", ls2.Level == 88);

        try { File.Delete(setup); } catch { }
        string recDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", product);
        try { if (Directory.Exists(recDir)) Directory.Delete(recDir, true); } catch { }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
