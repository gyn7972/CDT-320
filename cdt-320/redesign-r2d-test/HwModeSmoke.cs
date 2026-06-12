using System;
using System.Text;
using QMC.Vision.Optics.LFine;
using QMC.Vision.Optics.Sim;

// LFine 하드웨어 모드(SM) + 캐시skip 잔재 제거 검증:
//  (A) SM 프레임 = @SM0000;{0~3}\r\n / enum 값 0~3
//  (B) Sim.SetHardwareModeAsync 기록(LastHardwareMode)
//  (C) 항상 송신 — 동일 배치 재호출 시 BatchSendCount 매번 증가(skip 없음)
class HwModeSmoke
{
    static int _fail = 0;
    static void Ok(string n, bool c, string e = "") { if (!c) _fail++; Console.WriteLine($"  [{(c ? "OK" : "FAIL")}] {n} {e}"); }

    static int Main()
    {
        // (A) SM 프레임
        Ok("SetModeCommand(0) = SM0000;0", LFineProtocol.SetModeCommand(0) == "SM0000;0",
            $"(got {LFineProtocol.SetModeCommand(0)})");
        Ok("SetModeCommand(3) = SM0000;3", LFineProtocol.SetModeCommand(3) == "SM0000;3");
        string frame = Encoding.ASCII.GetString(LFineProtocol.SetModeFrame(0));
        Ok("프레임 = @SM0000;0\\r\\n", frame == "@SM0000;0\r\n", $"(got {frame.Replace("\r", "\\r").Replace("\n", "\\n")})");
        Ok("enum 값 0~3", (int)LFineHardwareMode.PageTrigger == 0 && (int)LFineHardwareMode.UserSequence == 1
            && (int)LFineHardwareMode.ChannelTrigger == 2 && (int)LFineHardwareMode.SoftwareTrigger == 3);

        // (B) Sim 기록
        var sim = new SimLightController(8);
        Ok("Sim 초기 LastHardwareMode=null", sim.LastHardwareMode == null);
        bool ok = sim.SetHardwareModeAsync(LFineHardwareMode.SoftwareTrigger).Result;
        Ok("Sim SetHardwareModeAsync=true + 기록", ok && sim.LastHardwareMode == LFineHardwareMode.SoftwareTrigger,
            $"(got {sim.LastHardwareMode})");

        // (C) 항상 송신 — 동일값 재호출도 카운트 증가
        var times = new int[8]; times[0] = 100;
        sim.SetChannelBatchAsync(0, times).Wait();
        sim.SetChannelBatchAsync(0, times).Wait();   // 동일값 — 이전엔 skip, 이제 송신
        sim.SetChannelBatchAsync(0, times).Wait();
        Ok("동일 배치 3회 = BatchSendCount 3 (skip 없음)", sim.BatchSendCount == 3, $"(got {sim.BatchSendCount})");
        Ok("배치 후 GetPower(ch1)=100 (상태 캐시 보존)", sim.GetPowerAsync(1).Result == 100);

        sim.Dispose();
        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : $"RESULT: {_fail} FAIL");
        return _fail == 0 ? 0 : 1;
    }
}
