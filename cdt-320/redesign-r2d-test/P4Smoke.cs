using System;
using System.Linq;
using QMC.Vision.Core.Parameters;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Ui.Controls;

// P4 Step1/2 — FromDescriptor 매핑 + store 라우팅 + G5 노출 검증.
class P4Smoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static int Main()
    {
        const string FT = "WaferVision/ReticleFinder";
        var f = new SimPatternFinder(FT);
        var store = new ParameterStore();
        store.Register(f, ParameterChannel.Snapshot);
        ParameterStoreHost.Current = store;

        var items = store.GetByTarget(FT)
            .Select(d => ParameterGridItem.FromDescriptor(d, store))
            .Where(x => x != null).ToList();

        Ok("타깃 디스크립터 → 그리드 아이템(10개)", items.Count == 10, $"(got {items.Count})");

        var searchX = items.FirstOrDefault(i => i.DisplayName == "Search X");
        var trainX  = items.FirstOrDefault(i => i.DisplayName == "Train X");
        var accept  = items.FirstOrDefault(i => i.DisplayName == "Accept Threshold");
        Ok("SCOPE=계층: Search X=Setup", searchX != null && searchX.Scope == ParameterGridScope.Setup);
        Ok("SCOPE=계층: Train X=Recipe",  trainX  != null && trainX.Scope  == ParameterGridScope.Recipe);
        Ok("G5 노출: Accept Threshold(Recipe) 그리드 등장", accept != null && accept.Scope == ParameterGridScope.Recipe);

        // 편집(그리드 Setter) → store 경유 → 실 객체 + dirty
        searchX.Setter(123.0);
        Ok("그리드 편집 → finder.SearchRoi.CenterX 반영", Math.Abs(f.SearchRoi.CenterX - 123.0) < 1e-6);
        Ok("그리드 편집 → store Setup dirty", store.IsDirty(ParameterLayer.Setup));
        trainX.Setter(456.0);
        Ok("그리드 편집 → finder.TrainRoi.CenterX 반영", Math.Abs(f.TrainRoi.CenterX - 456.0) < 1e-6);
        Ok("그리드 편집 → store Recipe dirty", store.IsDirty(ParameterLayer.Recipe));
        Ok("그리드 편집 → store 타깃 dirty", store.IsDirty(FT));

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
