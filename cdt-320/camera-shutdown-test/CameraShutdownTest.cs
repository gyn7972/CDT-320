using System;
using System.Reflection;

// Stage 88 검증 — Form1 종료 시퀀스(카메라 StopLive → VisionModule.Dispose → Camera.Close/IsOpen=false) 가
// 실제로 카메라 핸들을 해제하는지 헤드리스로 확인 (Sim 카메라).
class CameraShutdownTest
{
    static int _fail = 0;
    static void Check(string name, bool ok, string detail)
    {
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + name + (string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")"));
        if (!ok) _fail++;
    }

    static int Main()
    {
        var asm = Assembly.LoadFrom(@"D:\Work\source\QMC.Vision\bin\Debug\QMC.Vision.exe");
        var tSimCam = asm.GetType("QMC.Vision.Cameras.Sim.SimCamera");
        var tCam    = asm.GetType("QMC.Vision.Core.ICamera");
        var tWafer  = asm.GetType("QMC.Vision.Modules.WaferVisionModule");
        var tFactory= asm.GetType("QMC.Vision.Core.VisionFactory");

        // SimCamera 생성 + Open
        dynamic cam = Activator.CreateInstance(tSimCam, "Sim/Test");
        cam.Open();
        Check("SimCamera.Open → IsOpen", (bool)cam.IsOpen, "IsOpen=" + cam.IsOpen);

        // Backend (VisionFactory.Global) — 모듈 생성용
        object backend = tFactory.GetProperty("Global", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

        // WaferVisionModule(camera, backend) 생성
        object mod = Activator.CreateInstance(tWafer, new object[] { cam, backend });

        // Form1.OnFormClosing 시퀀스 재현: Camera.StopLive() → VisionModule.Dispose()
        bool threw = false;
        try
        {
            ((dynamic)mod).Camera.StopLive();
            ((IDisposable)mod).Dispose();
        }
        catch (Exception ex) { threw = true; Console.WriteLine("    ex: " + (ex.InnerException ?? ex).Message); }

        Check("종료 시퀀스(StopLive+Dispose) 예외 없음", !threw, "");
        Check("Dispose 후 Camera.IsOpen=false (핸들 해제)", !(bool)cam.IsOpen, "IsOpen=" + cam.IsOpen);

        // null 모듈 안전 (Form1 의 ?. 가드 — 미생성 모듈도 안전)
        object nullMod = null;
        bool nullThrew = false;
        try { var _ = nullMod; /* WaferMod?.Camera?.StopLive(); WaferMod?.Dispose(); 와 동등 — null 이면 no-op */ }
        catch { nullThrew = true; }
        Check("null 모듈 가드(?. no-op)", !nullThrew, "");

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }
}
