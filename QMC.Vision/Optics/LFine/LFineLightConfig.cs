using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.Vision.Optics.LFine
{
    /// <summary>LFine 조명 컨트롤러 1대(시리얼 1포트)의 설정 (Stage 67).</summary>
    [DataContract]
    public class LFineLightConfig
    {
        [DataMember] public string PortName     { get; set; } = "COM1";
        [DataMember] public int    BaudRate     { get; set; } = 115200;
        [DataMember] public int    DataBits     { get; set; } = 8;
        [DataMember] public string StopBits     { get; set; } = "One";    // System.IO.Ports.StopBits 이름
        [DataMember] public string Parity       { get; set; } = "None";
        [DataMember] public string Handshake    { get; set; } = "None";
        [DataMember] public int    TimeoutMs    { get; set; } = 1000;
        [DataMember] public int    MaxPower     { get; set; } = 240;
        [DataMember] public int    MaxOnTimeUs  { get; set; } = 999;
        [DataMember] public int    ChannelCount { get; set; } = 8;
        // Stage 79 — 동작 모드(캐시 정책). 어댑터가 entry.Mode 를 주입.
        [DataMember] public QMC.Common.Recipes.LightControllerMode Mode { get; set; } = QMC.Common.Recipes.LightControllerMode.StrobeOnCommand;
        [DataMember] public List<LFineChannel> Channels { get; set; } = new List<LFineChannel>();

        public LFineLightConfig Clone()
        {
            var c = new LFineLightConfig
            {
                PortName = PortName, BaudRate = BaudRate, DataBits = DataBits,
                StopBits = StopBits, Parity = Parity, Handshake = Handshake,
                TimeoutMs = TimeoutMs, MaxPower = MaxPower, MaxOnTimeUs = MaxOnTimeUs,
                Mode = Mode, ChannelCount = ChannelCount, Channels = new List<LFineChannel>()
            };
            if (Channels != null) foreach (var ch in Channels) c.Channels.Add(ch.Clone());
            return c;
        }
    }

    /// <summary>컨트롤러 내 채널 1개 정의.</summary>
    [DataContract]
    public class LFineChannel
    {
        [DataMember] public int    Index        { get; set; }            // 1~ChannelCount
        [DataMember] public string Name         { get; set; } = "";      // "INPUT STAGE RING"
        [DataMember] public string Color        { get; set; } = "White";
        [DataMember] public string Mode         { get; set; } = "Continuous"; // Continuous/Strobe
        [DataMember] public int    DefaultLevel { get; set; } = 128;
        [DataMember] public bool   Active       { get; set; } = true;

        public LFineChannel Clone()
            => new LFineChannel { Index = Index, Name = Name, Color = Color,
                                  Mode = Mode, DefaultLevel = DefaultLevel, Active = Active };
    }

    /// <summary>
    /// Stage 67 — 다중 컨트롤러 설정 집합. 매뉴얼 기준 컨트롤러 2개 (#5 확정).
    /// 영속화: Config\lfine_light.json.
    /// </summary>
    [DataContract]
    public class LFineLightSetup
    {
        [DataMember] public List<LFineLightConfig> Controllers { get; set; } = new List<LFineLightConfig>();

        /// <summary>매뉴얼 Illuminator communicator 1·2 기본값 (컨트롤러 2개).
        /// io_set.lightSource.json 8채널을 2 컨트롤러로 분배 (COM1 4채널 / COM2 4채널 — 실 결선 시 조정).</summary>
        public static LFineLightSetup CreateDefault()
        {
            return new LFineLightSetup
            {
                Controllers = new List<LFineLightConfig>
                {
                    new LFineLightConfig
                    {
                        PortName = "COM1", ChannelCount = 4,
                        Channels = new List<LFineChannel>
                        {
                            new LFineChannel { Index = 1, Name = "INPUT STAGE RING",  DefaultLevel = 128 },
                            new LFineChannel { Index = 2, Name = "BOTTOM VISION",     DefaultLevel = 180 },
                            new LFineChannel { Index = 3, Name = "SIDE VISION 1",     DefaultLevel = 200 },
                            new LFineChannel { Index = 4, Name = "SIDE VISION 2",     DefaultLevel = 200 },
                        }
                    },
                    new LFineLightConfig
                    {
                        PortName = "COM2", ChannelCount = 4,
                        Channels = new List<LFineChannel>
                        {
                            new LFineChannel { Index = 1, Name = "BIN VISION",         DefaultLevel = 140 },
                            new LFineChannel { Index = 2, Name = "FRONT SIDE VISION",  DefaultLevel = 200 },
                            new LFineChannel { Index = 3, Name = "REAR SIDE VISION",   DefaultLevel = 200 },
                            new LFineChannel { Index = 4, Name = "ALIGN MARK ILLUM",   DefaultLevel = 100 },
                        }
                    },
                }
            };
        }
    }

    /// <summary>LFine 조명 설정 영속화 — Config\lfine_light.json.</summary>
    public static class LFineLightConfigStore
    {
        public static string Dir  { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ { get; } = System.IO.Path.Combine(Dir, "lfine_light.json");

        public static LFineLightSetup Current { get; private set; } = new LFineLightSetup();

        static LFineLightConfigStore() { Directory.CreateDirectory(Dir); }

        public static LFineLightSetup Load()
        {
            if (!File.Exists(Path_))
            {
                Current = LFineLightSetup.CreateDefault();
                Save();
                return Current;
            }
            try
            {
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(LFineLightSetup));
                    Current = (LFineLightSetup)ser.ReadObject(fs);
                }
            }
            catch { Current = LFineLightSetup.CreateDefault(); }
            if (Current == null || Current.Controllers == null || Current.Controllers.Count == 0)
                Current = LFineLightSetup.CreateDefault();
            return Current;
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(LFineLightSetup));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }
    }
}
