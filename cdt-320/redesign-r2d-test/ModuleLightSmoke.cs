using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using QMC.Common.Recipes;
using QMC.Common.Data.Store;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;

// 조명 지정 모듈 이전 — Step1/2 검증:
//  (A) 모듈 Setup.LightPages 왕복(저장→재로드 복원)
//  (B) 마이그: 노드 Recipe.LightSettings (Port,Page) → 모듈 합집합
//  (C) 마이그: 구 노드 Setup json 의 LightPages 배열 raw 직독 → 모듈 합집합 + dedupe
class ModuleLightSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }
    static void Clean()
    {
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData"), true); } catch { }
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
    }

    // 구 노드 Setup json 모사용 DTO(LightPages 만) — 마이그 직독 대상.
    [DataContract] public class OldNodeSetup { [DataMember] public List<LightPageRef> LightPages { get; set; } }

    static VisionModuleSetupBase MSetup(IVisionModule m) => m.Setup as VisionModuleSetupBase;

    static int Main()
    {
        Clean();

        // ── (A) 모듈 Setup.LightPages 왕복 ──
        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings(); mod.LoadRecipe("default");
        var ms = MSetup(mod);
        Ok("모듈 Setup = VisionModuleSetupBase", ms != null);
        ms.LightPages = new List<LightPageRef> {
            new LightPageRef { ControllerPort = "COM-A", Page = 1 },
            new LightPageRef { ControllerPort = "COM-B", Page = 0 } };
        mod.SaveSettings();

        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings();
        var ms2 = MSetup(mod2);
        Ok("재로드 모듈 LightPages 2건 복원", ms2.LightPages.Count == 2, $"(got {ms2.LightPages.Count})");
        Ok("COM-A p1 + COM-B p0 복원",
            ms2.LightPages.Any(p => p.ControllerPort == "COM-A" && p.Page == 1)
            && ms2.LightPages.Any(p => p.ControllerPort == "COM-B" && p.Page == 0));

        // 이미 지정 있으면 마이그 스킵
        Ok("기존 지정 있으면 MigrateLightPages=false(스킵)", mod2.MigrateLightPages() == false);

        // ── (B) 마이그: 노드 Recipe 레벨 → 모듈 ──
        Clean();
        var modB = new WaferVisionModule(null, new SimBackend());
        modB.LoadSettings(); modB.LoadRecipe("default");
        var nodeB = modB.Algorithms.FirstOrDefault(a => a.Finder != null);
        var recB = nodeB.Recipe as AlgoRecipeBase;
        recB.LightSettings = new List<InspectionLightSetting> {
            new InspectionLightSetting { ControllerPort = "COM-SIM", Page = 2, Channel = 1, Level = 100 },
            new InspectionLightSetting { ControllerPort = "COM-SIM", Page = 2, Channel = 2, Level = 0 } };
        nodeB.SaveSettings();
        MSetup(modB).LightPages = new List<LightPageRef>();   // 모듈 비움(마이그 전)
        bool changedB = modB.MigrateLightPages();
        Ok("(B) Recipe 레벨 → 모듈 마이그 발생", changedB);
        Ok("(B) 모듈 LightPages = COM-SIM p2 (dedupe 1건)",
            MSetup(modB).LightPages.Count == 1
            && MSetup(modB).LightPages[0].ControllerPort == "COM-SIM"
            && MSetup(modB).LightPages[0].Page == 2,
            $"(got {MSetup(modB).LightPages.Count})");

        // ── (C) 마이그: 구 노드 Setup json 직독 + 합집합 dedupe ──
        Clean();
        var modC = new WaferVisionModule(null, new SimBackend());
        modC.LoadSettings(); modC.LoadRecipe("default");
        // 두 노드의 구 Setup json 에 LightPages 직접 기록(프로퍼티 제거 후의 구 파일 모사)
        var nodes = modC.Algorithms.ToList();
        EquipmentDataStore.Save(new OldNodeSetup { LightPages = new List<LightPageRef> {
            new LightPageRef { ControllerPort = "COM-X", Page = 0 } } }, nodes[0].StorageKey, "Setup");
        if (nodes.Count > 1)
            EquipmentDataStore.Save(new OldNodeSetup { LightPages = new List<LightPageRef> {
                new LightPageRef { ControllerPort = "COM-X", Page = 0 },   // 중복 → dedupe
                new LightPageRef { ControllerPort = "COM-Y", Page = 1 } } }, nodes[1].StorageKey, "Setup");
        MSetup(modC).LightPages = new List<LightPageRef>();
        bool changedC = modC.MigrateLightPages();
        Ok("(C) 구 노드 json 직독 마이그 발생", changedC);
        var lpC = MSetup(modC).LightPages;
        Ok("(C) COM-X p0 포함", lpC.Any(p => p.ControllerPort == "COM-X" && p.Page == 0));
        if (nodes.Count > 1)
        {
            Ok("(C) COM-Y p1 포함(합집합)", lpC.Any(p => p.ControllerPort == "COM-Y" && p.Page == 1));
            Ok("(C) COM-X p0 dedupe(1건만)", lpC.Count(p => p.ControllerPort == "COM-X" && p.Page == 0) == 1);
        }

        try { mod.Dispose(); mod2.Dispose(); modB.Dispose(); modC.Dispose(); } catch { }
        Clean();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
