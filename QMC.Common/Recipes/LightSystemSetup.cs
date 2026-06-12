using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.Common.Recipes
{
    /// <summary>
    /// Stage 69 — 조명 시스템 Setup (기구적, 제품 무관). 컨트롤러 인벤토리. 영속화: Config\light_system.json.
    /// PortName 이 컨트롤러 자연키(FK) — Setup 내 유일. (C3b-3) 알고리즘 결선(AlgorithmWirings) 폐기 —
    /// 검사별 컨트롤러/페이지는 노드 Setup(LightPages) 지정.
    /// </summary>
    [DataContract]
    public class LightSystemSetup
    {
        [DataMember] public List<LightControllerEntry> Controllers { get; set; } = new List<LightControllerEntry>();

        public LightControllerEntry GetController(string portName)
            => Controllers?.FirstOrDefault(c => string.Equals(c.PortName, portName, StringComparison.OrdinalIgnoreCase));

        /// <summary>Stage 68 #8 — 포트 일괄 변경. 컨트롤러 PortName 갱신(결선은 노드 측이므로 무관).</summary>
        public bool RenamePort(string oldPort, string newPort)
        {
            if (string.IsNullOrEmpty(oldPort) || string.IsNullOrEmpty(newPort)) return false;
            if (GetController(newPort) != null) return false;   // 대상 포트가 이미 존재하면 충돌
            var ctrl = GetController(oldPort);
            if (ctrl == null) return false;
            ctrl.PortName = newPort;
            return true;
        }

        /// <summary>저장 전 정합성 — PortName 중복/빈값/ChannelCount 검출.</summary>
        public List<string> Validate()
        {
            var errs = new List<string>();
            if (Controllers != null)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in Controllers)
                {
                    if (string.IsNullOrEmpty(c.PortName)) { errs.Add("빈 PortName 컨트롤러"); continue; }
                    if (!seen.Add(c.PortName)) errs.Add("PortName 중복: " + c.PortName);
                    if (c.ChannelCount <= 0) errs.Add(c.PortName + " ChannelCount<=0");
                }
            }
            return errs;
        }
    }

    [DataContract]
    public class LightControllerEntry
    {
        [DataMember] public string PortName     { get; set; }          // 자연키(FK), 유일
        // Stage 77 — 벤더 ("LFine" | "Leesos"). 기본 "LFine".
        // DataContract 이니셜라이저는 역직렬화 때 실행되지 않으므로 OnDeserializing 으로 기본값 주입.
        [DataMember(EmitDefaultValue = false)] public string Vendor { get; set; } = "LFine";
        // (LightControllerMode 잔재 제거 — 캐시 skip 정책 폐지, 항상 송신. 구 JSON 의 Mode 키는 로드 시 무시(비파괴).
        //  LFine 실 하드웨어 모드 0~3 은 Vision 측 SM 런타임 명령으로 별도 처리 — 영속 아님.)
        [DataMember] public string Name         { get; set; } = "";    // 사람용 라벨
        [DataMember] public int    BaudRate     { get; set; } = 9600;
        [DataMember] public int    ChannelCount { get; set; } = 8;
        [DataMember] public int    PageCount    { get; set; } = 1;     // 1 = 페이지 미사용
        [DataMember] public int    MaxPower     { get; set; } = 240;
        [DataMember] public int    MaxOnTimeUs  { get; set; } = 999;
        [DataMember] public List<LightChannelLabel> ChannelLabels { get; set; } = new List<LightChannelLabel>();

        // Stage 79 — 구버전 JSON 에 키 없으면 Vendor=LFine 주입.
        [OnDeserializing] internal void OnDeserializing(StreamingContext c) { Vendor = "LFine"; }
        [OnDeserialized]  internal void OnDeserialized (StreamingContext c) { if (string.IsNullOrEmpty(Vendor)) Vendor = "LFine"; }

        public LightControllerEntry Clone()
        {
            var c = new LightControllerEntry
            {
                PortName = PortName, Vendor = Vendor, Name = Name, BaudRate = BaudRate, ChannelCount = ChannelCount,
                PageCount = PageCount, MaxPower = MaxPower, MaxOnTimeUs = MaxOnTimeUs,
                ChannelLabels = new List<LightChannelLabel>()
            };
            if (ChannelLabels != null) foreach (var l in ChannelLabels) c.ChannelLabels.Add(l.Clone());
            return c;
        }
    }

    [DataContract]
    public class LightChannelLabel
    {
        [DataMember] public int    Channel { get; set; }     // 1~ChannelCount
        [DataMember] public string Name    { get; set; } = "";
        [DataMember] public string Color   { get; set; } = "White";

        public LightChannelLabel Clone() => new LightChannelLabel { Channel = Channel, Name = Name, Color = Color };
    }

    // (C3b-3) AlgorithmLightWiring / ControllerChannels(알고리즘 결선 풀) 제거 — 결선 개념 폐기.
    //  검사별 컨트롤러/페이지는 노드 Setup(LightPageRef), 채널 레벨은 노드 Recipe(InspectionLightSetting).

    /// <summary>조명 시스템 Setup 영속화 — Config\light_system.json.</summary>
    public static class LightSystemSetupStore
    {
        public static string Dir   { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ { get; } = System.IO.Path.Combine(Dir, "light_system.json");

        public static LightSystemSetup Current { get; private set; } = new LightSystemSetup();

        static LightSystemSetupStore() { Directory.CreateDirectory(Dir); }

        public static LightSystemSetup Load()
        {
            if (File.Exists(Path_))
            {
                try
                {
                    using (var fs = File.OpenRead(Path_))
                    {
                        var ser = new DataContractJsonSerializer(typeof(LightSystemSetup));
                        Current = (LightSystemSetup)ser.ReadObject(fs);
                    }
                }
                catch { Current = new LightSystemSetup(); }
            }
            else Current = new LightSystemSetup();   // 마이그레이션은 호출자(LightSystemMigrator)가 별도 수행

            if (Current == null) Current = new LightSystemSetup();
            return Current;
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(LightSystemSetup));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }

        public static void SetCurrent(LightSystemSetup setup) { Current = setup ?? new LightSystemSetup(); }
    }
}
