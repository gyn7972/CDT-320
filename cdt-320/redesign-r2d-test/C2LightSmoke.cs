using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using QMC.Common.Recipes;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;

// C2 Step 1 — 노드 조명 BaseUnit 왕복 + 마이그레이션.
//  (1) 노드 Setup.LightWirings / Recipe.LightSettings 편집→저장→재로드→복원.
//  (2) MigrateLegacyLights: 구 AlgorithmCameraMapping.InspectionLights + 결선 → 노드(빈 것만).
class C2LightSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static void Cleanup()
    {
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData"), true); } catch { }
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
    }

    static int Main()
    {
        Cleanup();

        // ── (1) 왕복 ──
        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings(); mod.LoadRecipe("default");
        var node = mod.Algorithms.FirstOrDefault(a => a.Finder != null);
        Ok("Finder 노드 존재", node != null);
        var setup  = node?.Setup  as AlgoSetupBase;
        var recipe = node?.Recipe as AlgoRecipeBase;
        Ok("Setup=AlgoSetupBase", setup != null);
        Ok("Recipe=AlgoRecipeBase", recipe != null);
        Ok("초기 LightSettings 빈 리스트(비-null)", recipe.LightSettings != null && recipe.LightSettings.Count == 0);

        recipe.LightSettings = new List<InspectionLightSetting> {
            new InspectionLightSetting { ControllerPort = "COM3", Channel = 1, Level = 120, On = true, Page = 0 },
            new InspectionLightSetting { ControllerPort = "COM3", Channel = 2, Level = 80,  On = true, StrobeTimeUs = 500, StabilizeDelayMs = 3, Page = 1 } };
        node.SaveSettings(); node.SaveRecipe("default");

        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings(); mod2.LoadRecipe("default");
        var node2 = mod2.Algorithms.FirstOrDefault(a => a.Finder != null && a.StorageKey == node.StorageKey);
        var recipe2 = node2?.Recipe as AlgoRecipeBase;
        Ok("재로드 LightSettings 복원(2개)", recipe2?.LightSettings?.Count == 2);
        var s2 = recipe2?.LightSettings?.FirstOrDefault(s => s.Channel == 2);
        Ok("재로드 ch2 Level=80", s2 != null && s2.Level == 80, $"(got {s2?.Level})");
        Ok("재로드 ch2 Strobe=500/Stab=3/Page=1", s2 != null && s2.StrobeTimeUs == 500 && s2.StabilizeDelayMs == 3 && s2.Page == 1);

        // (C3a: 구 algorithm_camera.json 마이그 다리 은퇴 — MigrateLegacyLights 테스트 제거.)
        Cleanup();
        try { mod.Dispose(); mod2.Dispose(); } catch { }
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
