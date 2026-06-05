using System;
using System.IO;
using System.Reflection;

// Stage 89 검증 — vision.json 의 LightUseSim 라운드트립 (실 VisionConfigStore.Load/Save 경로, 자동 Save 포함).
class LightUseSimRoundtripTest
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
        var tStore = asm.GetType("QMC.Vision.Config.VisionConfigStore");
        var tSettings = asm.GetType("QMC.Vision.Config.VisionSettings");
        string path = (string)tStore.GetProperty("Path_", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        var miLoad = tStore.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
        var piCurrent = tStore.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
        var piLight = tSettings.GetProperty("LightUseSim");
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        // 시나리오 1 — false 라운드트립 (파일 false → Load → 자동 Save → 파일 여전히 false)
        File.WriteAllText(path, "{\"LightUseSim\":false}");
        miLoad.Invoke(null, null);
        bool memFalse = (bool)piLight.GetValue(piCurrent.GetValue(null));
        string after1 = File.ReadAllText(path);
        Check("S1 false: Load 후 메모리 false", memFalse == false, "mem=" + memFalse);
        Check("S1 false: 자동 Save 후 파일에 false 유지", after1.Contains("\"LightUseSim\":false"), trunc(after1));

        // 시나리오 2 — true 라운드트립
        File.WriteAllText(path, "{\"LightUseSim\":true}");
        miLoad.Invoke(null, null);
        bool memTrue = (bool)piLight.GetValue(piCurrent.GetValue(null));
        string after2 = File.ReadAllText(path);
        Check("S2 true: Load 후 메모리 true", memTrue, "mem=" + memTrue);
        Check("S2 true: 자동 Save 후 파일에 true 유지", after2.Contains("\"LightUseSim\":true"), trunc(after2));

        // 시나리오 3 — 키 누락 → default false
        File.WriteAllText(path, "{\"Language\":\"ko\"}");
        miLoad.Invoke(null, null);
        bool memMissing = (bool)piLight.GetValue(piCurrent.GetValue(null));
        Check("S3 키 누락: default false", !memMissing, "mem=" + memMissing);

        // 시나리오 4 — vision.json 없음 → 새 파일 false
        try { File.Delete(path); } catch { }
        miLoad.Invoke(null, null);
        bool memNew = (bool)piLight.GetValue(piCurrent.GetValue(null));
        bool fileMade = File.Exists(path);
        Check("S4 파일 없음: 새 객체 false + 파일 생성", !memNew && fileMade, $"mem={memNew}, fileMade={fileMade}");

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }

    static string trunc(string s) => s.Length > 80 ? s.Substring(0, 80) + "..." : s;
}
