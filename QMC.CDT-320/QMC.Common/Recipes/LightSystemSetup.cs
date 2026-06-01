using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.Common.Recipes
{
    /// <summary>
    /// Stage 69 — 조명 시스템 Setup (기구적, 제품 무관).
    /// 컨트롤러 인벤토리 + 알고리즘 결선. 영속화: Config\light_system.json.
    /// PortName 이 컨트롤러 자연키(FK) — Setup 내 유일.
    /// </summary>
    [DataContract]
    public class LightSystemSetup
    {
        [DataMember] public List<LightControllerEntry> Controllers      { get; set; } = new List<LightControllerEntry>();
        [DataMember] public List<AlgorithmLightWiring> AlgorithmWirings { get; set; } = new List<AlgorithmLightWiring>();

        public LightControllerEntry GetController(string portName)
            => Controllers?.FirstOrDefault(c => string.Equals(c.PortName, portName, StringComparison.OrdinalIgnoreCase));

        public AlgorithmLightWiring GetWiring(string algorithm)
            => AlgorithmWirings?.FirstOrDefault(w => string.Equals(w.Algorithm, algorithm, StringComparison.OrdinalIgnoreCase));

        /// <summary>5 알고리즘 결선 항목이 빠짐없이 존재하도록 보장 — 누락은 빈 풀(조명 미사용).</summary>
        public void EnsureWirings()
        {
            if (AlgorithmWirings == null) AlgorithmWirings = new List<AlgorithmLightWiring>();
            foreach (var alg in VisionAlgorithm.All)
                if (!AlgorithmWirings.Any(w => string.Equals(w.Algorithm, alg, StringComparison.OrdinalIgnoreCase)))
                    AlgorithmWirings.Add(new AlgorithmLightWiring { Algorithm = alg });
        }

        /// <summary>Stage 68 #8 — 포트 일괄 변경. 컨트롤러 + 모든 결선의 ControllerPort 를 원자적으로 갱신.</summary>
        public bool RenamePort(string oldPort, string newPort)
        {
            if (string.IsNullOrEmpty(oldPort) || string.IsNullOrEmpty(newPort)) return false;
            if (GetController(newPort) != null) return false;   // 대상 포트가 이미 존재하면 충돌
            var ctrl = GetController(oldPort);
            if (ctrl == null) return false;
            ctrl.PortName = newPort;
            if (AlgorithmWirings != null)
                foreach (var w in AlgorithmWirings)
                    if (string.Equals(w.ControllerPort, oldPort, StringComparison.OrdinalIgnoreCase))
                        w.ControllerPort = newPort;
            return true;
        }

        /// <summary>저장 전 정합성 — PortName 중복 제거(첫 항목 우선) + 결선의 미존재 포트 검출.</summary>
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
            if (AlgorithmWirings != null)
                foreach (var w in AlgorithmWirings)
                    if (!string.IsNullOrEmpty(w.ControllerPort) && GetController(w.ControllerPort) == null)
                        errs.Add($"결선 '{w.Algorithm}' 의 포트 '{w.ControllerPort}' 가 인벤토리에 없음");
            return errs;
        }
    }

    [DataContract]
    public class LightControllerEntry
    {
        [DataMember] public string PortName     { get; set; }          // 자연키(FK), 유일
        [DataMember] public string Name         { get; set; } = "";    // 사람용 라벨
        [DataMember] public int    BaudRate     { get; set; } = 9600;
        [DataMember] public int    ChannelCount { get; set; } = 8;
        [DataMember] public int    PageCount    { get; set; } = 1;     // 1 = 페이지 미사용
        [DataMember] public int    MaxPower     { get; set; } = 240;
        [DataMember] public int    MaxOnTimeUs  { get; set; } = 999;
        [DataMember] public List<LightChannelLabel> ChannelLabels { get; set; } = new List<LightChannelLabel>();

        public LightControllerEntry Clone()
        {
            var c = new LightControllerEntry
            {
                PortName = PortName, Name = Name, BaudRate = BaudRate, ChannelCount = ChannelCount,
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

    /// <summary>알고리즘 1 ↔ 컨트롤러 1 + 사용 채널 풀. 기본 빈 풀(조명 미사용) — 사용자 UI 에서 배정.</summary>
    [DataContract]
    public class AlgorithmLightWiring
    {
        [DataMember] public string    Algorithm      { get; set; } = "";
        [DataMember] public string    ControllerPort { get; set; }            // null/빈 = 조명 미사용
        [DataMember] public List<int> Channels       { get; set; } = new List<int>();
        [DataMember] public int       Page           { get; set; } = 0;
    }

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
            Current.EnsureWirings();
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
