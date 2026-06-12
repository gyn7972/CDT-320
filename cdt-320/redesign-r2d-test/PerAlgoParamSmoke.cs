using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using QMC.Common;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Ui.Controls;

// ①② per-algorithm 파라미터 인프라 증명 — 테스트 전용 타입(프로덕션 오염 0).

/// <summary>테스트 전용 검사 Recipe — AlgoRecipeBase + 전용필드 TestField.</summary>
[DataContract]
class TestFinderRecipe : AlgoRecipeBase
{
    [DataMember] public double TestField { get; set; }
}

/// <summary>① 훅 구현 테스트 백엔드 — 자기 구체 Recipe(TestFinderRecipe)로 캐스트해 전용필드 sync.</summary>
class TestSyncFinder : SimPatternFinder, IAlgoParamSync
{
    public double ReceivedField = -1;     // ApplyParams 가 POCO→백엔드 수신
    public double RuntimeField  = -999;   // >-999 면 CollectParams 가 백엔드→POCO 기록
    public TestSyncFinder(string id) : base(id) { }
    public void ApplyParams(IRecipeData recipe, IConfigData config, ISetupData setup)
    { if (recipe is TestFinderRecipe r) ReceivedField = r.TestField; }
    public void CollectParams(IRecipeData recipe, IConfigData config, ISetupData setup)
    { if (recipe is TestFinderRecipe r && RuntimeField > -999) r.TestField = RuntimeField; }
}

class PerAlgoParamSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }
    static void Clean() { try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { } }

    static int Main()
    {
        Clean();
        const string KEY = "WaferVision.TestFinder";

        // ── ② 그리드 POCO 바인딩 (백엔드 비소비) : 편집→저장→재로드 복원 ──
        var f0 = new SimPatternFinder("WaferVision/TestFinder");
        var n0 = new FinderAlgorithm<FinderAlgoSetup, FinderAlgoConfig, TestFinderRecipe>(KEY, f0);
        var r0 = n0.Recipe as TestFinderRecipe;
        var item = ParameterGridItem.Double("TestField", "", ParameterGridScope.Recipe, () => r0.TestField, v => { r0.TestField = v; });
        item.Setter(50.0);   // 그리드 편집 시뮬
        Ok("② setter→POCO 50", Math.Abs(r0.TestField - 50) < 1e-9, $"(got {r0.TestField})");
        Ok("② getter 읽기 50", Math.Abs(Convert.ToDouble(item.Getter()) - 50) < 1e-9);
        n0.SaveRecipe("default");
        var n0b = new FinderAlgorithm<FinderAlgoSetup, FinderAlgoConfig, TestFinderRecipe>(KEY, new SimPatternFinder("WaferVision/TestFinder"));
        n0b.LoadRecipe("default");
        Ok("② 재로드 TestField=50 복원(POCO)", Math.Abs((n0b.Recipe as TestFinderRecipe).TestField - 50) < 1e-9, $"(got {(n0b.Recipe as TestFinderRecipe).TestField})");

        // ── ① ApplyParams 훅 : 백엔드가 POCO 전용필드 수신 ──
        Clean();
        var fa = new TestSyncFinder("WaferVision/TestFinder");
        var na = new FinderAlgorithm<FinderAlgoSetup, FinderAlgoConfig, TestFinderRecipe>(KEY, fa);
        (na.Recipe as TestFinderRecipe).TestField = 42;
        na.SaveRecipe("default");   // CollectParams: RuntimeField=-999 → POCO(42) 보존
        Ok("① 저장 시 미설정 CollectParams POCO 보존", Math.Abs((na.Recipe as TestFinderRecipe).TestField - 42) < 1e-9);

        var fb = new TestSyncFinder("WaferVision/TestFinder");
        var nb = new FinderAlgorithm<FinderAlgoSetup, FinderAlgoConfig, TestFinderRecipe>(KEY, fb);
        nb.LoadRecipe("default");   // ApplyToRuntime → ApplyParams → fb.ReceivedField=42
        Ok("① ApplyParams 훅: 백엔드 ReceivedField=42", Math.Abs(fb.ReceivedField - 42) < 1e-9, $"(got {fb.ReceivedField})");

        // ── ① CollectParams 훅 : 백엔드 런타임값 → POCO ──
        fb.RuntimeField = 77;
        nb.SaveRecipe("default");   // CollectParams → POCO.TestField=77
        var nc = new FinderAlgorithm<FinderAlgoSetup, FinderAlgoConfig, TestFinderRecipe>(KEY, new SimPatternFinder("x"));
        nc.LoadRecipe("default");
        Ok("① CollectParams 훅: 런타임77 → POCO 재로드", Math.Abs((nc.Recipe as TestFinderRecipe).TestField - 77) < 1e-9, $"(got {(nc.Recipe as TestFinderRecipe).TestField})");

        // ── 미구현 백엔드 no-op (back-compat, 동작 불변) ──
        var plain = new SimPatternFinder("WaferVision/Plain");
        var np = new FinderAlgorithm<FinderAlgoSetup, FinderAlgoConfig, TestFinderRecipe>("WaferVision.Plain", plain);
        try { np.LoadRecipe("default"); Ok("미구현 백엔드 no-op (크래시 없음)", true); }
        catch (Exception ex) { Ok("미구현 백엔드 no-op", false, ex.Message); }

        Clean();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
