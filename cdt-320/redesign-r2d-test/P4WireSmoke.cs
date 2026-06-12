using System;
using System.IO;
using QMC.Common.Recipes;
using QMC.Vision.Modules;
using QMC.Vision.Backends.Sim;

// C1 Step 4 — CameraMappingPanel 로드/저장 경로(ExportCameraMapping ↔ ImportCameraMapping) 대칭 검증.
// 패널은 module.ExportCameraMapping() 으로 버퍼를 로드하고, 저장 시 module.ImportCameraMapping()+SaveSettings.
class P4WireSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static int Main()
    {
        var mod = new WaferVisionModule(null, new SimBackend());
        mod.LoadSettings();

        // 모듈 Config/Recipe = SSOT
        mod.Config.CameraId = "Sim/Wafer"; mod.Config.Gain = 3.0;
        mod.Config.RoiOffsetX = 5; mod.Config.RoiWidth = 800; mod.Config.RoiHeight = 600;
        mod.Config.DelayBeforeGrabMs = 12;
        mod.Recipe.Exposure = 7000;

        // ── 패널 로드: Export → 버퍼 ──
        var buf = mod.ExportCameraMapping();
        Ok("Export CameraId", buf.CameraId == "Sim/Wafer", $"(got {buf.CameraId})");
        Ok("Export Gain=3.0", Math.Abs(buf.Gain - 3.0) < 1e-6, $"(got {buf.Gain})");
        Ok("Export RoiWidth=800", buf.RoiWidth == 800, $"(got {buf.RoiWidth})");
        Ok("Export Delay=12", buf.DelayBeforeGrabMs == 12, $"(got {buf.DelayBeforeGrabMs})");
        Ok("Export Exposure=7000", Math.Abs(buf.ExposureUs - 7000) < 1e-6, $"(got {buf.ExposureUs})");

        // ── 패널 편집(버퍼) ──
        buf.Gain = 4.5; buf.RoiWidth = 1024; buf.ExposureUs = 9500; buf.CameraId = "Sim/Bin";

        // ── 패널 저장: Import → 모듈 Config/Recipe + Save ──
        mod.ImportCameraMapping(buf);
        Ok("Import → Config.Gain=4.5", Math.Abs(mod.Config.Gain - 4.5) < 1e-6, $"(got {mod.Config.Gain})");
        Ok("Import → Config.RoiWidth=1024", mod.Config.RoiWidth == 1024, $"(got {mod.Config.RoiWidth})");
        Ok("Import → Config.CameraId=Sim/Bin", mod.Config.CameraId == "Sim/Bin", $"(got {mod.Config.CameraId})");
        Ok("Import → Recipe.Exposure=9500", Math.Abs(mod.Recipe.Exposure - 9500) < 1e-6, $"(got {mod.Recipe.Exposure})");
        mod.SaveSettings();
        mod.SaveRecipe("default");

        // ── 재로드 → Export 가 영속값 반영 ──
        var mod2 = new WaferVisionModule(null, new SimBackend());
        mod2.LoadSettings();
        mod2.LoadRecipe("default");
        var buf2 = mod2.ExportCameraMapping();
        Ok("재로드 Export Gain=4.5", Math.Abs(buf2.Gain - 4.5) < 1e-6, $"(got {buf2.Gain})");
        Ok("재로드 Export RoiWidth=1024", buf2.RoiWidth == 1024, $"(got {buf2.RoiWidth})");
        Ok("재로드 Export CameraId=Sim/Bin", buf2.CameraId == "Sim/Bin", $"(got {buf2.CameraId})");
        Ok("재로드 Export Exposure=9500", Math.Abs(buf2.ExposureUs - 9500) < 1e-6, $"(got {buf2.ExposureUs})");

        try { File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EquipmentData", "Config", "WaferVision.json")); } catch { }
        try { Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", "default"), true); } catch { }
        try { mod.Dispose(); mod2.Dispose(); } catch { }

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
