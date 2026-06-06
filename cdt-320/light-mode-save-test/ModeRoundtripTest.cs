using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Json;
using QMC.Common.Recipes;

// LightControllerMode JSON 라운드트립 검증 (EmitDefaultValue 제거 수정).
// Store(Save/Load)와 동일한 DataContractJsonSerializer(typeof(LightSystemSetup)) 사용.
class ModeRoundtripTest
{
    static int _fail = 0;

    static string ToJson(LightSystemSetup s)
    {
        var ser = new DataContractJsonSerializer(typeof(LightSystemSetup));
        using (var ms = new MemoryStream()) { ser.WriteObject(ms, s); return Encoding.UTF8.GetString(ms.ToArray()); }
    }
    static LightSystemSetup FromJson(string json)
    {
        var ser = new DataContractJsonSerializer(typeof(LightSystemSetup));
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json))) return (LightSystemSetup)ser.ReadObject(ms);
    }
    static LightSystemSetup MakeSetup(LightControllerMode mode)
    {
        var s = new LightSystemSetup();
        s.Controllers.Add(new LightControllerEntry { PortName = "COM1", Vendor = "LFine", Mode = mode, Name = "L1", ChannelCount = 8 });
        return s;
    }
    static void Check(string name, bool ok, string detail)
    {
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + name + "  " + detail);
        if (!ok) _fail++;
    }

    static int Main()
    {
        // 시나리오 1+3 — 각 Mode 가 JSON 에 기록되고 라운드트립 보존
        foreach (LightControllerMode m in Enum.GetValues(typeof(LightControllerMode)))
        {
            string json = ToJson(MakeSetup(m));
            bool hasKey = Regex.IsMatch(json, "\"Mode\"\\s*:");
            var back = FromJson(json);
            var got = back.Controllers[0].Mode;
            Check("Save+Load Mode=" + m, hasKey && got == m,
                  "(key=" + hasKey + ", got=" + got + ")");
        }

        // 명시적으로 Continuous 의 JSON 키 형식 출력 (보고용 샘플)
        Console.WriteLine("  JSON(Continuous) 발췌: " +
            Regex.Match(ToJson(MakeSetup(LightControllerMode.Continuous)), "\"PortName\"[^}]*").Value + "}");

        // 시나리오 4 — 구버전 JSON (Mode 키 없음) → OnDeserializing 기본값 StrobeOnCommand
        string full = ToJson(MakeSetup(LightControllerMode.Continuous));
        string noMode = Regex.Replace(full, "\"Mode\"\\s*:\\s*[0-9]+\\s*,?", "");
        bool removed = !Regex.IsMatch(noMode, "\"Mode\"\\s*:");
        var legacy = FromJson(noMode);
        var legacyMode = legacy.Controllers[0].Mode;
        Check("Legacy JSON (no Mode key) -> default", removed && legacyMode == LightControllerMode.StrobeOnCommand,
              "(removedKey=" + removed + ", got=" + legacyMode + ")");

        Console.WriteLine(_fail == 0 ? "RESULT: ALL PASS" : ("RESULT: " + _fail + " FAIL"));
        return _fail == 0 ? 0 : 1;
    }
}
