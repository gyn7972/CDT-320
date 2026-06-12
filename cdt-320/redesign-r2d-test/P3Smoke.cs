using System;
using System.Drawing;
using System.Linq;
using QMC.Vision.Core.Parameters;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Backends.Cognex;
using QMC.Vision.Core.Inspectors;

// P3 반영 테스트 — (A) 관리코드 소비 체인: store 편집→Match 반영 / (B) Cognex ② 통합 white-box.
class P3Smoke
{
    static int _fail = 0;
    static void Ok(string n, bool cond, string extra = "") { if (!cond) _fail++; Console.WriteLine($"  [{(cond ? "OK" : "FAIL")}] {n} {extra}"); }

    static int Main()
    {
        // ── A. 관리코드 소비 체인: SimPatternFinder.Match 가 store 로 편집된 TrainRoi 를 반영 ──
        const string FT = "WaferVision/ReticleFinder";
        var f = new SimPatternFinder(FT);
        var sA = new ParameterStore();
        sA.Register(f, ParameterChannel.Snapshot);
        ParameterStoreHost.Current = sA;
        sA.SetValue(FT, "TrainRoi.CenterX", 500.0);
        sA.SetValue(FT, "TrainRoi.CenterY", 400.0);
        var r = f.Match(new Bitmap(640, 480));
        bool reflected = r.Success && r.Instances.Count > 0
            && Math.Abs(r.Instances[0].CenterX - 500.0) <= 2.5
            && Math.Abs(r.Instances[0].CenterY - 400.0) <= 2.5;
        Ok("관리코드 체인: store TrainRoi 편집 → Match 반영", reflected,
           reflected ? "" : $"(got {r.Instances.FirstOrDefault()?.CenterX},{r.Instances.FirstOrDefault()?.CenterY})");

        // ── B. Cognex ② 통합 white-box ──
        const string IT = "BottomInspection/SurfaceInspector";
        var cog = new CognexInspector(IT, null);
        var bottom = new BottomInspectionParameters();
        cog.BottomParams = bottom;                          // 부트스트랩 주입 모사

        // 주입 시 cog.DescribeParameters 는 Threshold 생략(단일 진실원)
        bool cogNoThreshold = !cog.DescribeParameters().Any(d => d.Key == "Threshold");
        Ok("Cognex 주입 시 inline Threshold 디스크립터 생략(dedupe 정식)", cogNoThreshold);

        // store 에 inspector→bottom 순 등록(부트스트랩 순서). Threshold 는 bottom 이 공급.
        var sB = new ParameterStore();
        sB.Register(cog, ParameterChannel.Snapshot);
        sB.Register(bottom, ParameterChannel.Snapshot);
        bool storeHasThreshold = sB.Get(IT, "Threshold") != null;
        Ok("store Threshold 존재(Bottom② 공급)", storeHasThreshold);

        // 편집 → 공유 인스턴스(=Inspect 가 읽는 EffectiveThreshold 소스) 반영
        sB.SetValue(IT, "Threshold", 199.0);
        Ok("편집 → cog.BottomParams.Threshold 반영(Inspect 사용값)", cog.BottomParams.Threshold == 199);

        // back-compat: 미주입 Cognex 는 inline Threshold 노출
        var cog2 = new CognexInspector("X/Y", null);
        bool cog2HasThreshold = cog2.DescribeParameters().Any(d => d.Key == "Threshold");
        Ok("미주입 Cognex 는 inline Threshold 노출(back-compat)", cog2HasThreshold);

        // 매핑 레지스트리(G3)
        Ok("레지스트리 Bottom→정규 target", InspectionParamRegistry.ByTool("BottomInspection")?.Target == IT);
        Ok("레지스트리 역조회 target→tool", InspectionParamRegistry.ByTarget(IT)?.Tool == "BottomInspection");

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
