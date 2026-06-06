using System;
using System.IO;
using System.Text;

namespace QMC.CDT320.Secs
{
    /// <summary>
    /// SECS-II 메시지 (Stream/Function) — 골격 (실 인코딩은 별도 단계).
    /// 본 라운드는 텍스트 페이로드 + Stream/Function 식별만 지원.
    /// </summary>
    public class SecsMessage
    {
        /// <summary>Stream (S1, S2, S6, S7 등 — 1~127).</summary>
        public byte Stream     { get; set; }
        /// <summary>Function (1~255). 짝수면 reply.</summary>
        public byte Function   { get; set; }
        /// <summary>W-bit (응답 요구 — 보통 1=요청, 0=응답).</summary>
        public bool ReplyExpected { get; set; }
        /// <summary>System bytes (트랜잭션 ID).</summary>
        public uint SystemBytes { get; set; }
        /// <summary>Device ID (보통 0).</summary>
        public ushort DeviceId  { get; set; }
        /// <summary>본문 (raw bytes — 실 SECS-II encoding 은 SecsItem 트리 또는 텍스트).</summary>
        public byte[] Payload   { get; set; } = new byte[0];

        /// <summary>SECS-II SecsItem 트리 (Stage 16). 설정 시 자동으로 Payload 인코딩.</summary>
        public SecsItem Body
        {
            get { return _body; }
            set
            {
                _body = value;
                if (value != null) Payload = value.Encode();
            }
        }
        private SecsItem _body;

        /// <summary>Payload 를 SecsItem 으로 디코드 (저장 안함, 반환만).</summary>
        public SecsItem DecodeBody()
        {
            if (Payload == null || Payload.Length == 0) return null;
            _body = SecsItem.Decode(Payload);
            return _body;
        }

        public bool IsRequest => (Function % 2) == 1;
        public bool IsReply   => (Function % 2) == 0;

        public string TextPayload
        {
            get { return Payload != null ? Encoding.UTF8.GetString(Payload) : ""; }
            set { Payload = value != null ? Encoding.UTF8.GetBytes(value) : new byte[0]; }
        }

        public override string ToString()
            => $"S{Stream}F{Function}{(ReplyExpected ? "W" : "")} sys={SystemBytes} dev={DeviceId} bytes={Payload?.Length ?? 0}";

        // ── 인코딩 (간이) ──
        // [Header 10 bytes]:  DeviceID(2) | Stream(1, MSB=W) | Function(1) | Pt(1=00) | Pt2(1=00) | SystemBytes(4)
        // [Body N bytes]
        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)(DeviceId >> 8));
                bw.Write((byte)(DeviceId & 0xFF));
                byte streamByte = (byte)(Stream & 0x7F);
                if (ReplyExpected) streamByte |= 0x80;
                bw.Write(streamByte);
                bw.Write((byte)Function);
                bw.Write((byte)0);   // PType
                bw.Write((byte)0);   // SType
                bw.Write((byte)((SystemBytes >> 24) & 0xFF));
                bw.Write((byte)((SystemBytes >> 16) & 0xFF));
                bw.Write((byte)((SystemBytes >> 8) & 0xFF));
                bw.Write((byte)(SystemBytes & 0xFF));
                if (Payload != null && Payload.Length > 0) bw.Write(Payload);
                return ms.ToArray();
            }
        }

        public static SecsMessage Parse(byte[] data)
        {
            if (data == null || data.Length < 10) return null;
            var m = new SecsMessage
            {
                DeviceId      = (ushort)((data[0] << 8) | data[1]),
                ReplyExpected = (data[2] & 0x80) != 0,
                Stream        = (byte)(data[2] & 0x7F),
                Function      = data[3],
                SystemBytes   = (uint)((data[6] << 24) | (data[7] << 16) | (data[8] << 8) | data[9]),
            };
            int bodyLen = data.Length - 10;
            if (bodyLen > 0)
            {
                m.Payload = new byte[bodyLen];
                Buffer.BlockCopy(data, 10, m.Payload, 0, bodyLen);
            }
            return m;
        }

        // ── 표준 메시지 헬퍼 (Stage 7 확장) ──
        public static SecsMessage S1F1()
            => new SecsMessage { Stream = 1, Function = 1, ReplyExpected = true };
        public static SecsMessage S1F2()
            => new SecsMessage { Stream = 1, Function = 2, ReplyExpected = false };
        public static SecsMessage S1F13()
            => new SecsMessage { Stream = 1, Function = 13, ReplyExpected = true };
        public static SecsMessage S1F14()
            => new SecsMessage { Stream = 1, Function = 14, ReplyExpected = false };

        // S2F41/42 — Host Command Send / Acknowledge
        public static SecsMessage S2F41(string commandText)
            => new SecsMessage { Stream = 2, Function = 41, ReplyExpected = true, TextPayload = commandText };
        public static SecsMessage S2F42(uint sysBytes, string ack)
            => new SecsMessage { Stream = 2, Function = 42, ReplyExpected = false, SystemBytes = sysBytes, TextPayload = ack };

        // S5F1/2 — Alarm Send / Acknowledge
        public static SecsMessage S5F1(int alarmCode, byte severity, string text)
            => new SecsMessage
            {
                Stream = 5, Function = 1, ReplyExpected = true,
                TextPayload = $"ALARM|{alarmCode}|{severity}|{text}"
            };
        public static SecsMessage S5F2(uint sysBytes)
            => new SecsMessage { Stream = 5, Function = 2, ReplyExpected = false, SystemBytes = sysBytes, TextPayload = "ACK" };

        // S5F3/4 — Alarm En/Disable / Acknowledge
        public static SecsMessage S5F3(int alarmCode, bool enable)
            => new SecsMessage
            {
                Stream = 5, Function = 3, ReplyExpected = true,
                TextPayload = $"ALEN|{alarmCode}|{(enable ? "1" : "0")}"
            };

        // S6F11/12 — Event Report Send / Acknowledge
        public static SecsMessage S6F11(string ceid)
            => new SecsMessage { Stream = 6, Function = 11, ReplyExpected = true, TextPayload = ceid };
        public static SecsMessage S6F12(uint sysBytes)
            => new SecsMessage { Stream = 6, Function = 12, ReplyExpected = false, SystemBytes = sysBytes, TextPayload = "0" };

        // S7F3/4 — Process Program (Recipe) Send / Acknowledge
        public static SecsMessage S7F3(string ppid, string base64Body)
            => new SecsMessage
            {
                Stream = 7, Function = 3, ReplyExpected = true,
                TextPayload = $"PP|{ppid}|{base64Body}"
            };
        public static SecsMessage S7F4(uint sysBytes, byte ack)
            => new SecsMessage { Stream = 7, Function = 4, ReplyExpected = false, SystemBytes = sysBytes, TextPayload = ack.ToString() };

        // S9F1/3/5 — Errors
        public static SecsMessage S9F1()
            => new SecsMessage { Stream = 9, Function = 1, ReplyExpected = false, TextPayload = "Unknown DeviceID" };
        public static SecsMessage S9F3()
            => new SecsMessage { Stream = 9, Function = 3, ReplyExpected = false, TextPayload = "Unknown Stream" };
        public static SecsMessage S9F5()
            => new SecsMessage { Stream = 9, Function = 5, ReplyExpected = false, TextPayload = "Unknown Function" };
    }
}
