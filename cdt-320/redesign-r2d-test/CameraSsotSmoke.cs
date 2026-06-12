using System;
using System.IO;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;
using QMC.Vision.Cameras.Sim;

// C1 — 카메라 SSOT(모듈 BaseUnit Config/Recipe) 왕복: 편집→Apply→Save→새 모듈 Load→Apply→카메라 복원.
class CameraSsotSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static int Main()
    {
        var mod = new WaferVisionModule(null, new SimBackend());   // 카메라 없이 생성
        Ok("ctor null 카메라 허용", mod.Camera == null);
        mod.LoadSettings();

        // 모듈 Config/Recipe 편집(카메라 SSOT)
        mod.Config.CameraId = "Sim/Wafer";
        mod.Config.Gain = 2.5;
        mod.Config.RoiOffsetX = 10; mod.Config.RoiOffsetY = 20; mod.Config.RoiWidth = 640; mod.Config.RoiHeight = 480;
        mod.Recipe.Exposure = 8000;

        var cam = new SimCamera("Sim/Wafer"); mod.SetCamera(cam); try { cam.Open(); } catch { }
        mod.ApplyCameraSettings();
        Ok("Apply → camera.Gain=2.5", Math.Abs(cam.Gain - 2.5) < 1e-6, $"(got {cam.Gain})");
        Ok("Apply → camera.ExposureUs=8000", Math.Abs(cam.ExposureUs - 8000) < 1e-6, $"(got {cam.ExposureUs})");

        mod.SaveSettings();
        mod.SaveRecipe("default");

        // 새 모듈 → Load → 복원
        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings();
        mod2.LoadRecipe("default");
        Ok("재로드 Config.CameraId 복원", mod2.CameraId == "Sim/Wafer", $"(got {mod2.CameraId})");
        Ok("재로드 Config.Gain 복원(2.5)", Math.Abs(mod2.Config.Gain - 2.5) < 1e-6, $"(got {mod2.Config.Gain})");
        Ok("재로드 Config.RoiWidth 복원(640)", mod2.Config.RoiWidth == 640, $"(got {mod2.Config.RoiWidth})");
        Ok("재로드 Recipe.Exposure 복원(8000)", Math.Abs(mod2.Recipe.Exposure - 8000) < 1e-6, $"(got {mod2.Recipe.Exposure})");

        var cam2 = new SimCamera("Sim/Wafer"); mod2.SetCamera(cam2); try { cam2.Open(); } catch { }
        mod2.ApplyCameraSettings();
        Ok("재로드 후 Apply → camera2.Gain=2.5", Math.Abs(cam2.Gain - 2.5) < 1e-6, $"(got {cam2.Gain})");
        Ok("재로드 후 Apply → camera2.ExposureUs=8000", Math.Abs(cam2.ExposureUs - 8000) < 1e-6, $"(got {cam2.ExposureUs})");

        try { File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData", "Config", "WaferVision.json")); } catch { }
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
        try { mod.Dispose(); mod2.Dispose(); } catch { }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
