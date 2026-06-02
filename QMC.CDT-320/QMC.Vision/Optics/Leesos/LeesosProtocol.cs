using System;
using System.Text;

namespace QMC.Vision.Optics.Leesos
{
    /// <summary>
    /// LeesOS 디지털 조명 시리얼 프로토콜 인코딩 (Stage 77).
    /// <para>레퍼런스 <c>LightControl\Optics\Leesos\DigitalIlluminatorCommunicator.cs</c> 의
    /// 실제 코드(GetCommandText + 송신부)를 반영. ※ 주석의 H/C/SPWR 가 아니라 코드의 <b>LH/LC/LS</b>.</para>
    /// <list type="bullet">
    ///   <item>프레임 = ASCII payload + <b>Etx(0x0D 0x0A '\r\n')</b></item>
    ///   <item>밝기 = <b>Volume</b> (0~255, hex 2자리). Strobe/Page 미지원.</item>
    ///   <item><b>LC</b>(Volume): <c>LC{ch}{power:X2}</c>  (예 ch=1,power=255 → "LC1FF")</item>
    ///   <item><b>LH</b>(On/Off): <c>LH{ch}{ON|OF}</c>     (예 ch=1,on → "LH1ON")</item>
    ///   <item><b>LS</b>(Status): <c>LS{ch}01</c>          (예 ch=1 → "LS101"), 응답 Substring(2,2)=="ON"</item>
    /// </list>
    /// 응답형: 정상 echo 는 prefix <b>"R"</b>, 에러는 <b>"RERR"</b>(EndsWith "ERR").
    /// </summary>
    public static class LeesosProtocol
    {
        // 프레임 종료 — 레퍼런스 Etx1/Etx2.
        public const byte Etx1 = 0x0D;   // \r
        public const byte Etx2 = 0x0A;   // \n

        public const string OnOffText  = "LH";
        public const string VolumeText = "LC";
        public const string StatusText = "LS";

        public const string StartText = "R";     // 정상 응답 prefix
        public const string ErrorCode = "ERR";   // "RERR" = NAK
        public const string OnText    = "ON";
        public const string OffText   = "OF";

        /// <summary>Volume(밝기) 명령 — 예 ch=1,power=255 → "LC1FF". power 는 호출자가 0~255 클램프.</summary>
        public static string BuildVolumeCommand(int channel, int power)
            => string.Format("{0}{1}{2:X2}", VolumeText, channel, power & 0xFF);

        /// <summary>On/Off 명령 — 예 ch=1,on=true → "LH1ON".</summary>
        public static string BuildOnOffCommand(int channel, bool on)
            => string.Format("{0}{1}{2}", OnOffText, channel, on ? OnText : OffText);

        /// <summary>Status(점등확인) 명령 — 예 ch=1 → "LS101".</summary>
        public static string BuildStatusCommand(int channel)
            => string.Format("{0}{1}01", StatusText, channel);

        /// <summary>payload 를 ASCII + Etx1 Etx2 프레임 바이트로 감쌈.</summary>
        public static byte[] WrapFrame(string payload)
        {
            var body  = Encoding.ASCII.GetBytes(payload ?? "");
            var frame = new byte[body.Length + 2];
            System.Array.Copy(body, 0, frame, 0, body.Length);
            frame[frame.Length - 2] = Etx1;
            frame[frame.Length - 1] = Etx2;
            return frame;
        }

        /// <summary>수신 원시 바이트 → payload 문자열 (후행 \r \n 제거).</summary>
        public static string UnwrapFrame(byte[] raw)
        {
            if (raw == null || raw.Length == 0) return "";
            int end = raw.Length;
            while (end > 0 && (raw[end - 1] == Etx2 || raw[end - 1] == Etx1)) end--;
            return end > 0 ? Encoding.ASCII.GetString(raw, 0, end) : "";
        }

        /// <summary>응답 분류. (정상 R prefix / NAK "RERR" / 무효).</summary>
        public enum RespKind { Ok, Nak, Invalid }

        /// <summary>응답 문자열 분류 — null/빈값=Invalid, EndsWith "ERR"=Nak, StartsWith "R"=Ok, 그 외 Invalid.</summary>
        public static RespKind Classify(string resp)
        {
            if (string.IsNullOrEmpty(resp)) return RespKind.Invalid;
            if (resp.EndsWith(ErrorCode, StringComparison.OrdinalIgnoreCase)) return RespKind.Nak;     // "RERR"
            if (resp.StartsWith(StartText, StringComparison.OrdinalIgnoreCase)) return RespKind.Ok;
            return RespKind.Invalid;
        }

        /// <summary>Status 응답에서 점등(ON) 여부 — 레퍼런스 OnCheckPowerOn: Substring(2,2)=="ON".</summary>
        public static bool ParseStatusOn(string resp)
            => !string.IsNullOrEmpty(resp) && resp.Length >= 4
               && resp.Substring(2, 2).Equals(OnText, StringComparison.OrdinalIgnoreCase);
    }
}
