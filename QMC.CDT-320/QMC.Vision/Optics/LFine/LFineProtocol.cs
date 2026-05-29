using System.Text;

namespace QMC.Vision.Optics.LFine
{
    /// <summary>
    /// LFine 디지털 조명 시리얼 프로토콜 인코딩 (Stage 67).
    /// 이전 LightControl\Optics\LFine\LFineDigitalIlluminator.cs:205-285 의 포맷을 그대로 차용:
    ///   Stx=0x02, Etx=0x03, Power="{ch}P{power:000}R", StrobeTime="{ch}T{time:000}R".
    /// 무응답(fire-and-forget) — 응답 파싱 없음 (Stage 66 #4 확정).
    /// On/Off 전용 명령 없음 — OFF = Power 0 (Stage 66 #3 확정).
    /// </summary>
    public static class LFineProtocol
    {
        public const byte Stx = 0x02;
        public const byte Etx = 0x03;

        public const string PowerText      = "P";
        public const string StrobeTimeText = "T";
        public const string RemoteText     = "R";

        /// <summary>전력 명령 payload — 예 ch=1,power=200 → "1P200R".</summary>
        public static string BuildPowerCommand(int channel, int power)
            => string.Format("{0}{1}{2:000}{3}", channel, PowerText, power, RemoteText);

        /// <summary>Strobe 시간 명령 payload — 예 ch=1,us=50 → "1T050R".</summary>
        public static string BuildStrobeTimeCommand(int channel, int onTimeUs)
            => string.Format("{0}{1}{2:000}{3}", channel, StrobeTimeText, onTimeUs, RemoteText);

        /// <summary>payload 를 Stx/Etx 프레임으로 감싼 바이트 배열.</summary>
        public static byte[] WrapFrame(string payload)
        {
            var body = Encoding.ASCII.GetBytes(payload ?? "");
            var frame = new byte[body.Length + 2];
            frame[0] = Stx;
            System.Array.Copy(body, 0, frame, 1, body.Length);
            frame[frame.Length - 1] = Etx;
            return frame;
        }

        /// <summary>payload 를 프레임으로 감싼 바이트 배열 (Power 명령 단축).</summary>
        public static byte[] PowerFrame(int channel, int power)
            => WrapFrame(BuildPowerCommand(channel, power));

        /// <summary>payload 를 프레임으로 감싼 바이트 배열 (StrobeTime 명령 단축).</summary>
        public static byte[] StrobeTimeFrame(int channel, int onTimeUs)
            => WrapFrame(BuildStrobeTimeCommand(channel, onTimeUs));
    }
}
