using System.Text;

namespace QMC.Vision.Optics.LFine
{
    /// <summary>
    /// LFine PS 디지털 조명 시리얼 프로토콜 인코딩.
    /// <para>레퍼런스 <c>LightControl\Optics\LFine\LFinePSDigitalIlluminator.cs</c> 의
    /// <c>LFinePSDigitalIlluminatorCommunicator</c> 데이터 프로토콜을 그대로 반영 (Stage 74 정정).</para>
    /// <list type="bullet">
    ///   <item>프레임 = <b>Stx(0x40 '@')</b> + ASCII payload + <b>Etx(0x0D 0x0A '\r\n')</b></item>
    ///   <item>밝기 = <b>strobe on-time</b> (별도 power 명령 없음). 페이지 전환 명령 없음 — page 는 매 명령에 포함.</item>
    ///   <item><b>SC</b>(단일 채널 on-time): <c>SC{page:00}{ch:00};{time:000}</c></item>
    ///   <item><b>SP</b>(페이지 전체 16채널 on-time): <c>SP{page:00}00</c> + 채널마다 <c>;{time/10:000}</c></item>
    /// </list>
    /// 무응답(fire-and-forget) — 송신 전용, 응답 파싱 없음. OFF = on-time 0.
    /// </summary>
    public static class LFineProtocol
    {
        // 프레임 경계 — 레퍼런스 Communicator.arrStx / arrEtx 와 동일한 실제 바이트값.
        // (레퍼런스 주석의 //\r //\n 은 오기. 0x40='@', 0x0D=CR, 0x0A=LF.)
        public const byte Stx  = 0x40;
        public const byte Etx1 = 0x0D;
        public const byte Etx2 = 0x0A;

        // 명령 prefix — 레퍼런스 PageOnTime="SP", ChannelOnTime="SC".
        public const string PageOnTimeText    = "SP";
        public const string ChannelOnTimeText = "SC";

        /// <summary>단일 채널 on-time 명령 payload — 예 page=1,ch=3,time=50 → "SC0103;050", 채널 번호(“00” ~ “15”).
        /// (레퍼런스 GetChannelOnTimeCommand 와 동일: time 은 그대로 3자리.)</summary>
        public static string BuildChannelOnTimeCommand(int page, int channel, int time)
            => string.Format("{0}{1:00}{2:00};{3:000}", ChannelOnTimeText, page, channel > 0 ? channel - 1 : 0, time);

        /// <summary>페이지 전체(다채널) on-time 명령 payload — 예 page=1,times=[500,0,..] → "SP0100;050;000;..".
        /// (레퍼런스 GetPageOnTimeCommand 와 동일: 채널 값은 <b>time/10</b> 3자리.)</summary>
        public static string BuildPageOnTimeCommand(int page, int[] times)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}{1:00}00", PageOnTimeText, page);
            if (times != null)
                foreach (int t in times) sb.AppendFormat(";{0:000}", t / 10);
            return sb.ToString();
        }

        /// <summary>payload 를 Stx + body + Etx1 Etx2 프레임 바이트로 감쌈 (레퍼런스 SendFrame(text, arrStx, arrEtx)).</summary>
        public static byte[] WrapFrame(string payload)
        {
            var body  = Encoding.ASCII.GetBytes(payload ?? "");
            var frame = new byte[body.Length + 3];
            frame[0] = Stx;
            System.Array.Copy(body, 0, frame, 1, body.Length);
            frame[frame.Length - 2] = Etx1;
            frame[frame.Length - 1] = Etx2;
            return frame;
        }

        /// <summary>수신 프레임 → payload 문자열. 선행 Stx(0x40)·후행 Etx(0x0D/0x0A)를 벗겨 ASCII 로 디코딩.
        /// (레퍼런스 ReceiveFrame(out data, arrStx, arrEtx) + BytesConverter.ToString 대칭.)</summary>
        public static string UnwrapFrame(byte[] raw)
        {
            if (raw == null || raw.Length == 0) return "";
            int start = 0, end = raw.Length;
            if (raw[start] == Stx) start++;                                  // 선행 '@' 제거
            while (end > start && (raw[end - 1] == Etx2 || raw[end - 1] == Etx1)) end--;  // 후행 \r \n 제거
            int len = end - start;
            return len > 0 ? Encoding.ASCII.GetString(raw, start, len) : "";
        }

        /// <summary>단일 채널 on-time 프레임 (SC 명령 단축).</summary>
        public static byte[] ChannelOnTimeFrame(int page, int channel, int time)
            => WrapFrame(BuildChannelOnTimeCommand(page, channel, time));

        /// <summary>페이지 전체 on-time 프레임 (SP 명령 단축).</summary>
        public static byte[] PageOnTimeFrame(int page, int[] times)
            => WrapFrame(BuildPageOnTimeCommand(page, times));
    }
}
