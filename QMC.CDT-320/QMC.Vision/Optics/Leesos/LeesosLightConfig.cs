using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.Vision.Optics.Leesos
{
    /// <summary>
    /// LeesOS 디지털 조명 컨트롤러 1대(시리얼 1포트)의 설정 (Stage 77).
    /// 참조: LightControl\Optics\Leesos\DigitalIlluminatorConfig.cs 기본값.
    /// 밝기 = Volume(0~255, hex 2자리). Strobe/Page 미지원.
    /// </summary>
    [DataContract]
    public class LeesosLightConfig
    {
        [DataMember] public string PortName     { get; set; } = "COM2";
        [DataMember] public int    BaudRate     { get; set; } = 9600;
        [DataMember] public int    DataBits     { get; set; } = 8;
        [DataMember] public string StopBits     { get; set; } = "One";   // System.IO.Ports.StopBits 이름
        [DataMember] public string Parity       { get; set; } = "None";
        [DataMember] public string Handshake    { get; set; } = "None";
        [DataMember] public int    TimeoutMs    { get; set; } = 1000;    // 응답형 → 적용됨
        [DataMember] public int    MaxPower     { get; set; } = 4095;    // 12-bit (000~FFF)
        [DataMember] public int    ChannelCount { get; set; } = 4;       // LPD-6524-4CH 기본 (2CH 모델은 사용자 변경)
        [DataMember] public List<LeesosChannel> Channels { get; set; } = new List<LeesosChannel>();

        public LeesosLightConfig Clone()
        {
            var c = new LeesosLightConfig
            {
                PortName = PortName, BaudRate = BaudRate, DataBits = DataBits,
                StopBits = StopBits, Parity = Parity, Handshake = Handshake,
                TimeoutMs = TimeoutMs, MaxPower = MaxPower,
                ChannelCount = ChannelCount, Channels = new List<LeesosChannel>()
            };
            if (Channels != null) foreach (var ch in Channels) c.Channels.Add(ch.Clone());
            return c;
        }
    }

    /// <summary>컨트롤러 내 채널 1개 정의.</summary>
    [DataContract]
    public class LeesosChannel
    {
        [DataMember] public int    Index        { get; set; }            // 1~ChannelCount
        [DataMember] public string Name         { get; set; } = "";
        [DataMember] public string Color        { get; set; } = "White";
        [DataMember] public int    DefaultLevel { get; set; } = 128;
        [DataMember] public bool   Active       { get; set; } = true;

        public LeesosChannel Clone()
            => new LeesosChannel { Index = Index, Name = Name, Color = Color,
                                   DefaultLevel = DefaultLevel, Active = Active };
    }
}
