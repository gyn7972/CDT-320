using System;
using System.IO;
using QMC.Vision.Cameras.Sim;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;

// 버그 회귀 — 카메라 "연결" 상태에서 파라미터 편집 저장이 라이브 카메라 값으로 덮어써지던 문제.
//  (구) SaveSettings/SaveRecipe 가 저장 직전 CollectCameraSettings(카메라→Config/Recipe) 호출 →
//  ImportCameraMapping(편집값) 이 카메라 현재값으로 원복돼 영속. 수정 후: Config/Recipe(SSOT) 그대로 영속.
class CamSaveClobberSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }
    static void Clean()
    {
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData"), true); } catch { }
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
    }

    static int Main()
    {
        Clean();

        // ── ① 카메라 연결(Camera != null) 상태 — 편집값이 카메라 현재값으로 원복되지 않아야 함 ──
        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings(); mod.LoadRecipe("default");
        mod.SetCamera(new SimCamera("Sim/Wafer"));   // 연결 모사 — 라이브 카메라 값(Gain 등)은 편집값과 다름
        Ok("카메라 연결 상태(Camera != null)", mod.Camera != null);

        // UI 편집 모사: Export → 버퍼 편집 → Import (SaveAll 경로와 동일)
        var buf = mod.ExportCameraMapping();
        buf.Gain = 12.5; buf.FrameRate = 77; buf.ExposureUs = 9100;
        buf.RoiOffsetX = 4; buf.RoiWidth = 640; buf.RoiHeight = 480;
        buf.DelayBeforeGrabMs = 33;
        mod.ImportCameraMapping(buf);

        bool s1 = mod.SaveSettings();
        bool s2 = mod.SaveRecipe("default");
        Ok("저장 성공", s1 && s2);

        // 저장 직후 Config 가 편집값 유지(덮어쓰기 없음)
        var c = mod.Config as VisionModuleConfigBase;
        Ok("저장 직후 Config.Gain=12.5 (카메라값 원복 안 됨)", Math.Abs(c.Gain - 12.5) < 1e-6, $"(got {c.Gain})");

        // 재로드 → 영속값 = 편집값
        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings(); mod2.LoadRecipe("default");
        var c2 = mod2.Config as VisionModuleConfigBase;
        var r2 = mod2.Recipe as VisionModuleRecipeBase;
        Ok("재로드 Gain=12.5", Math.Abs(c2.Gain - 12.5) < 1e-6, $"(got {c2.Gain})");
        Ok("재로드 FrameRate=77", Math.Abs(c2.FrameRate - 77) < 1e-6, $"(got {c2.FrameRate})");
        Ok("재로드 ROI 4/640/480", c2.RoiOffsetX == 4 && c2.RoiWidth == 640 && c2.RoiHeight == 480,
            $"(got {c2.RoiOffsetX}/{c2.RoiWidth}/{c2.RoiHeight})");
        Ok("재로드 Delay=33", c2.DelayBeforeGrabMs == 33, $"(got {c2.DelayBeforeGrabMs})");
        Ok("재로드 Exposure=9100 (Recipe)", Math.Abs(r2.Exposure - 9100) < 1e-6, $"(got {r2.Exposure})");

        // ── ② 미연결(Camera == null) — 기존 정상 케이스 그대로 ──
        Clean();
        var mod3 = new WaferVisionModule(null, new SimBackend());
        mod3.LoadSettings(); mod3.LoadRecipe("default");
        Ok("미연결 상태(Camera == null)", mod3.Camera == null);
        var buf3 = mod3.ExportCameraMapping();
        buf3.Gain = 7.5; buf3.ExposureUs = 4321;
        mod3.ImportCameraMapping(buf3);
        mod3.SaveSettings(); mod3.SaveRecipe("default");
        var mod4 = new WaferVisionModule(null, new SimBackend());
        mod4.LoadSettings(); mod4.LoadRecipe("default");
        Ok("미연결 재로드 Gain=7.5", Math.Abs((mod4.Config as VisionModuleConfigBase).Gain - 7.5) < 1e-6);
        Ok("미연결 재로드 Exposure=4321", Math.Abs((mod4.Recipe as VisionModuleRecipeBase).Exposure - 4321) < 1e-6);

        try { mod.Dispose(); mod2.Dispose(); mod3.Dispose(); mod4.Dispose(); } catch { }
        Clean();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
