using System;
using System.Text;

namespace QMC.Vision.Optics.Leesos
{
    /// <summary>
    /// LeesOS LPD-6524 (12BIT) 디지털 조명 시리얼 프로토콜 (Stage 79 — 매뉴얼 §5.3 기준).
    /// <para>UART 9600/8N1, 모든 포맷 ASCII + <b>CR LF</b>(0x0D 0x0A).</para>
    /// <list type="bullet">
    ///   <item>채널 n1: <b>1~9</b>, <b>A~G</b>(10~16), <b>T</b>=전체</item>
    ///   <item>밝기 <b>LC</b>: <c>LC{n1}{val:XXX}</c> (val 000~FFF, 12-bit 0~4095) → echo <c>R{n1}{val}</c></item>
    ///   <item>On/Off <b>LH</b>: <c>LH{n1}{ON|OF}</c> → <c>R{n1}OK</c> / <c>R{n1}ER</c></item>
    ///   <item>상태 <b>LS</b>: <c>LS{n1}{00|01|02}</c> (00=밝기,01=On/Off,02=에러) → <c>R{n1}{...}</c></item>
    /// </list>
    /// ※ 에러 토큰은 <b>"ER"</b>(2자) — 구 레퍼런스의 "ERR" 아님.
    /// </summary>
    public static class LeesosProtocol
    {
        public const byte Cr = 0x0D;   // \r
        public const byte Lf = 0x0A;   // \n

        public const string ModelText  = "L";   // 12BIT 모델 prefix
        public const string VolumeCmd  = "LC";
        public const string OnOffCmd   = "LH";
        public const string StatusCmd  = "LS";
        public const string ChannelAll = "T";

        public const string StartText = "R";    // 정상 응답 prefix
        public const string OkText    = "OK";
        public const string ErrText   = "ER";   // R{n1}ER
        public const string OnText    = "ON";
        public const string OffText   = "OF";

        /// <summary>채널 인코딩 — 1~9 → "1".."9", 10~16 → "A".."G". 범위 밖이면 예외(호출 전 검증 권장).</summary>
        public static string EncodeChannel(int ch)
        {
            if (ch >= 1 && ch <= 9)  return ch.ToString();
            if (ch >= 10 && ch <= 16) return ((char)('A' + (ch - 10))).ToString();
            throw new ArgumentOutOfRangeException(nameof(ch), ch, "Leesos 채널은 1~16");
        }

        // ── 명령 빌더 ──
        /// <summary>밝기 — 예 ch=1,val=0xFFF → "LC1FFF". val 은 0~4095(12-bit).</summary>
        public static string BuildVolumeCommand(int channel, int value)
            => string.Format("{0}{1}{2:X3}", VolumeCmd, EncodeChannel(channel), value & 0xFFF);

        /// <summary>전체 밝기 — 예 val=0x800 → "LCT800".</summary>
        public static string BuildVolumeAllCommand(int value)
            => string.Format("{0}{1}{2:X3}", VolumeCmd, ChannelAll, value & 0xFFF);

        /// <summary>On/Off — 예 ch=10,on=true → "LHAON".</summary>
        public static string BuildOnOffCommand(int channel, bool on)
            => string.Format("{0}{1}{2}", OnOffCmd, EncodeChannel(channel), on ? OnText : OffText);

        /// <summary>전체 On/Off — "LHTON" / "LHTOF".</summary>
        public static string BuildOnOffAllCommand(bool on)
            => string.Format("{0}{1}{2}", OnOffCmd, ChannelAll, on ? OnText : OffText);

        /// <summary>상태 질의 — 예 ch=1,OnOff → "LS101".</summary>
        public static string BuildStatusCommand(int channel, LeesosStatusType type)
            => string.Format("{0}{1}{2:D2}", StatusCmd, EncodeChannel(channel), (int)type);

        // ── 프레임 ──
        public static byte[] WrapFrame(string payload)
        {
            var body  = Encoding.ASCII.GetBytes(payload ?? "");
            var frame = new byte[body.Length + 2];
            System.Array.Copy(body, 0, frame, 0, body.Length);
            frame[frame.Length - 2] = Cr;
            frame[frame.Length - 1] = Lf;
            return frame;
        }

        public static string UnwrapFrame(byte[] raw)
        {
            if (raw == null || raw.Length == 0) return "";
            int end = raw.Length;
            while (end > 0 && (raw[end - 1] == Lf || raw[end - 1] == Cr)) end--;
            return end > 0 ? Encoding.ASCII.GetString(raw, 0, end) : "";
        }

        // ── 응답 검증 ──
        /// <summary>밝기 echo 일치 — R{n1}{val:XXX}.</summary>
        public static bool ValidateEcho(string resp, int channel, int value)
            => resp != null && resp.Equals(string.Format("{0}{1}{2:X3}", StartText, EncodeChannel(channel), value & 0xFFF), StringComparison.OrdinalIgnoreCase);

        /// <summary>전체 밝기 echo 일치 — RT{val:XXX}.</summary>
        public static bool ValidateAllEcho(string resp, int value)
            => resp != null && resp.Equals(string.Format("{0}{1}{2:X3}", StartText, ChannelAll, value & 0xFFF), StringComparison.OrdinalIgnoreCase);

        /// <summary>에러 응답 — R{n1}ER (EndsWith "ER").</summary>
        public static bool IsErrorResponse(string resp)
            => resp != null && resp.EndsWith(ErrText, StringComparison.OrdinalIgnoreCase);

        /// <summary>성공 응답 — R{n1}OK (EndsWith "OK").</summary>
        public static bool IsOkResponse(string resp)
            => resp != null && resp.EndsWith(OkText, StringComparison.OrdinalIgnoreCase);

        /// <summary>R prefix 여부 (정상 응답군).</summary>
        public static bool IsResponse(string resp)
            => resp != null && resp.StartsWith(StartText, StringComparison.OrdinalIgnoreCase);

        /// <summary>On/Off 상태 응답(R{n1}ON/OF)에서 ON 여부 — n1 1자라 Substring(2,2).</summary>
        public static bool ParseStatusOn(string resp)
            => resp != null && resp.Length >= 4 && resp.Substring(2, 2).Equals(OnText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>LS 상태 질의 타입.</summary>
    public enum LeesosStatusType { Brightness = 0, OnOff = 1, Error = 2 }
}
