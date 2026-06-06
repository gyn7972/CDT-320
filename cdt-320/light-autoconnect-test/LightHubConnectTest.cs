using System;
using System.Linq;
using System.Threading.Tasks;
using QMC.Common.Recipes;
using QMC.Vision.Comm;

// Form1.Load 자동연결이 호출하는 LightHub.ConnectAllAsync 경로 검증.
// (UpdateLightStartupStatus 의 ok/total/fails 계산 입력을 그대로 확인.)
class LightHubConnectTest
{
    static int _fail = 0;
    static void Check(string name, bool ok, string detail)
    {
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + name + "  " + detail);
        if (!ok) _fail++;
    }
    static LightSystemSetup Setup(params (string port, string vendor)[] ctrls)
    {
        var s = new LightSystemSetup();
        foreach (var c in ctrls)
            s.Controllers.Add(new LightControllerEntry { PortName = c.port, Vendor = c.vendor, ChannelCount = 8 });
        return s;
    }

    static async Task<int> Main()
    {
        // 시나리오 1 — Sim 모드: 모든 포트 true
        LightHub.Initialize(Setup(("COM1", "LFine"), ("COM2", "Leesos")), true);
        var r1 = await LightHub.ConnectAllAsync();
        int ok1 = r1.Count(kv => kv.Value);
        Check("S1 Sim 2 controllers all OK", r1.Count == 2 && ok1 == 2, $"(total={r1.Count}, ok={ok1})");

        // 시나리오 4 — 빈 인벤토리: total=0, 예외 없음
        LightHub.Initialize(Setup(), true);
        var r4 = await LightHub.ConnectAllAsync();
        Check("S4 empty inventory total=0", r4.Count == 0, $"(total={r4.Count})");

        // 시나리오 3 — 실장비 모드 + 존재하지 않는 COM99: 해당 포트 false (예외는 hub 가 흡수)
        LightHub.Initialize(Setup(("COM99", "LFine")), false);
        try
        {
            var d = await LightHub.ConnectAllAsync();
            bool com99False = d.ContainsKey("COM99") && d["COM99"] == false;
            Check("S3 real-mode COM99 -> false (no throw)", d.Count == 1 && com99False, $"(total={d.Count}, COM99={(d.ContainsKey("COM99") ? d["COM99"].ToString() : "missing")})");
        }
        catch (Exception ex)
        {
            Check("S3 real-mode COM99 -> false (no throw)", false, "threw: " + ex.GetType().Name + " " + ex.Message);
        }

        // 정리
        try { await LightHub.DisconnectAllAsync(); } catch { }
        LightHub.DisposeAll();

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }
}
