using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QMC.CDT320.Secs
{
    /// <summary>SECS-II 데이터 형식 (SEMI E5 Section 9 — Item Header).</summary>
    public enum SecsItemFormat : byte
    {
        List   = 0x00,    // 0o00 — List of items
        Binary = 0x20,    // 0o10 — B (1 byte)
        Bool   = 0x24,    // 0o11 — Boolean
        Ascii  = 0x40,    // 0o20 — A (ASCII string)
        Jis8   = 0x44,    // 0o21 — J (JIS-8)
        // Numeric
        I8 = 0x60, I1 = 0x64, I2 = 0x68, I4 = 0x70,
        F8 = 0x80, F4 = 0x90,
        U8 = 0xA0, U1 = 0xA4, U2 = 0xA8, U4 = 0xB0,
    }

    /// <summary>
    /// SECS-II 데이터 노드 (List + Leaf 노드 포함).
    /// SEMI E5 형식 직렬화/역직렬화 지원.
    /// </summary>
    public class SecsItem
    {
        public SecsItemFormat Format { get; set; } = SecsItemFormat.List;
        public List<SecsItem> Children { get; set; } = new List<SecsItem>();
        public object Value { get; set; }   // ASCII: string, Int: long, Float: double, Binary: byte[], Bool: bool

        // ── 팩토리 ──
        public static SecsItem List(params SecsItem[] children)
            => new SecsItem { Format = SecsItemFormat.List, Children = children?.ToList() ?? new List<SecsItem>() };

        public static SecsItem A(string s)
            => new SecsItem { Format = SecsItemFormat.Ascii, Value = s ?? "" };

        public static SecsItem U1(byte v)  => new SecsItem { Format = SecsItemFormat.U1, Value = (long)v };
        public static SecsItem U2(ushort v) => new SecsItem { Format = SecsItemFormat.U2, Value = (long)v };
        public static SecsItem U4(uint v)  => new SecsItem { Format = SecsItemFormat.U4, Value = (long)v };
        public static SecsItem I1(sbyte v) => new SecsItem { Format = SecsItemFormat.I1, Value = (long)v };
        public static SecsItem I2(short v) => new SecsItem { Format = SecsItemFormat.I2, Value = (long)v };
        public static SecsItem I4(int v)   => new SecsItem { Format = SecsItemFormat.I4, Value = (long)v };
        public static SecsItem F4(float v) => new SecsItem { Format = SecsItemFormat.F4, Value = (double)v };
        public static SecsItem F8(double v) => new SecsItem { Format = SecsItemFormat.F8, Value = v };
        public static SecsItem B(params byte[] data) => new SecsItem { Format = SecsItemFormat.Binary, Value = data ?? new byte[0] };
        public static SecsItem Boolean(bool v) => new SecsItem { Format = SecsItemFormat.Bool, Value = v };

        // ── 직렬화 ──
        public byte[] Encode()
        {
            using (var ms = new MemoryStream())
            {
                EncodeTo(ms);
                return ms.ToArray();
            }
        }

        private void EncodeTo(Stream s)
        {
            byte[] body = EncodeBody();
            int len = body.Length;
            // Length bytes count (1~3)
            byte lengthBytes = 1;
            if (len > 0xFF) lengthBytes = 2;
            if (len > 0xFFFF) lengthBytes = 3;
            byte header = (byte)((byte)Format | lengthBytes);
            s.WriteByte(header);
            if (lengthBytes == 1) s.WriteByte((byte)len);
            else if (lengthBytes == 2) { s.WriteByte((byte)((len >> 8) & 0xFF)); s.WriteByte((byte)(len & 0xFF)); }
            else { s.WriteByte((byte)((len >> 16) & 0xFF)); s.WriteByte((byte)((len >> 8) & 0xFF)); s.WriteByte((byte)(len & 0xFF)); }
            s.Write(body, 0, body.Length);
        }

        private byte[] EncodeBody()
        {
            switch (Format)
            {
                // SECS List item 인코딩
                case SecsItemFormat.List:
                    {
                        // List: 길이 = child 수. body = child 들의 인코딩 concat.
                        using (var ms = new MemoryStream())
                        {
                            foreach (var c in Children) c.EncodeTo(ms);
                            // List 의 length 필드는 children count 가 아니라 byte count.
                            // 그래서 EncodeTo 에서 len 을 직접 사용.
                            return ms.ToArray();
                        }
                    }
                // SECS ASCII item 인코딩
                case SecsItemFormat.Ascii:
                    return Encoding.ASCII.GetBytes((string)(Value ?? ""));
                // SECS Binary item 인코딩
                case SecsItemFormat.Binary:
                    return (byte[])(Value ?? new byte[0]);
                // SECS Boolean item 인코딩
                case SecsItemFormat.Bool:
                    return new[] { (byte)(((bool)(Value ?? false)) ? 1 : 0) };
                // SECS 1-byte 정수 item 인코딩
                case SecsItemFormat.U1: case SecsItemFormat.I1:
                    return new[] { (byte)((long)(Value ?? 0L) & 0xFF) };
                // SECS 2-byte 정수 item 인코딩
                case SecsItemFormat.U2: case SecsItemFormat.I2:
                    {
                        long v = (long)(Value ?? 0L);
                        return new[] { (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF) };
                    }
                // SECS 4-byte 정수 item 인코딩
                case SecsItemFormat.U4: case SecsItemFormat.I4:
                    {
                        long v = (long)(Value ?? 0L);
                        return new[] { (byte)((v >> 24) & 0xFF), (byte)((v >> 16) & 0xFF),
                                       (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF) };
                    }
                // SECS 4-byte float item 인코딩
                case SecsItemFormat.F4:
                    {
                        float f = (float)(double)(Value ?? 0.0);
                        var b = BitConverter.GetBytes(f);
                        if (BitConverter.IsLittleEndian) Array.Reverse(b);
                        return b;
                    }
                // SECS 8-byte float item 인코딩
                case SecsItemFormat.F8:
                    {
                        double d = (double)(Value ?? 0.0);
                        var b = BitConverter.GetBytes(d);
                        if (BitConverter.IsLittleEndian) Array.Reverse(b);
                        return b;
                    }
                default:
                    return new byte[0];
            }
        }

        // ── 역직렬화 ──
        public static SecsItem Decode(byte[] data)
        {
            if (data == null || data.Length < 2) return null;
            int pos = 0;
            return DecodeOne(data, ref pos);
        }

        private static SecsItem DecodeOne(byte[] d, ref int pos)
        {
            if (pos >= d.Length) return null;
            byte header = d[pos++];
            byte lengthBytes = (byte)(header & 0x03);
            byte fmt = (byte)(header & 0xFC);

            int len = 0;
            for (int i = 0; i < lengthBytes; i++)
            {
                if (pos >= d.Length) return null;
                len = (len << 8) | d[pos++];
            }

            var item = new SecsItem { Format = (SecsItemFormat)fmt };
            int end = pos + len;
            if (end > d.Length) return null;

            switch (item.Format)
            {
                // SECS List item 디코딩
                case SecsItemFormat.List:
                    while (pos < end)
                    {
                        var child = DecodeOne(d, ref pos);
                        if (child == null) break;
                        item.Children.Add(child);
                    }
                    break;
                // SECS ASCII item 디코딩
                case SecsItemFormat.Ascii:
                    item.Value = Encoding.ASCII.GetString(d, pos, len);
                    pos += len;
                    break;
                // SECS Binary item 디코딩
                case SecsItemFormat.Binary:
                    {
                        var bin = new byte[len];
                        Array.Copy(d, pos, bin, 0, len);
                        item.Value = bin;
                        pos += len;
                    }
                    break;
                // SECS Boolean item 디코딩
                case SecsItemFormat.Bool:
                    item.Value = (len > 0 && d[pos] != 0);
                    pos += len;
                    break;
                // SECS 1-byte 정수 item 디코딩
                case SecsItemFormat.U1: case SecsItemFormat.I1:
                    {
                        long v = 0;
                        if (len >= 1) v = d[pos];
                        if (item.Format == SecsItemFormat.I1 && len >= 1) v = (sbyte)d[pos];
                        item.Value = v;
                        pos += len;
                    }
                    break;
                // SECS 2-byte 정수 item 디코딩
                case SecsItemFormat.U2: case SecsItemFormat.I2:
                    {
                        long v = 0;
                        if (len >= 2) v = (d[pos] << 8) | d[pos + 1];
                        if (item.Format == SecsItemFormat.I2 && (v & 0x8000) != 0) v |= unchecked((long)0xFFFFFFFFFFFF0000);
                        item.Value = v;
                        pos += len;
                    }
                    break;
                // SECS 4-byte 정수 item 디코딩
                case SecsItemFormat.U4: case SecsItemFormat.I4:
                    {
                        long v = 0;
                        if (len >= 4)
                            v = ((long)d[pos] << 24) | ((long)d[pos + 1] << 16) | ((long)d[pos + 2] << 8) | d[pos + 3];
                        if (item.Format == SecsItemFormat.I4 && (v & 0x80000000) != 0) v |= unchecked((long)0xFFFFFFFF00000000);
                        item.Value = v;
                        pos += len;
                    }
                    break;
                // SECS 4-byte float item 디코딩
                case SecsItemFormat.F4:
                    if (len >= 4)
                    {
                        var b = new byte[4];
                        Array.Copy(d, pos, b, 0, 4);
                        if (BitConverter.IsLittleEndian) Array.Reverse(b);
                        item.Value = (double)BitConverter.ToSingle(b, 0);
                    }
                    pos += len;
                    break;
                // SECS 8-byte float item 디코딩
                case SecsItemFormat.F8:
                    if (len >= 8)
                    {
                        var b = new byte[8];
                        Array.Copy(d, pos, b, 0, 8);
                        if (BitConverter.IsLittleEndian) Array.Reverse(b);
                        item.Value = BitConverter.ToDouble(b, 0);
                    }
                    pos += len;
                    break;
                default:
                    pos = end;
                    break;
            }
            return item;
        }

        public override string ToString()
        {
            switch (Format)
            {
                // List item 표시 문자열
                case SecsItemFormat.List:  return $"L[{Children.Count}]";
                // ASCII item 표시 문자열
                case SecsItemFormat.Ascii: return $"A[\"{Value}\"]";
                // Binary item 표시 문자열
                case SecsItemFormat.Binary:
                    var bin = (byte[])Value;
                    return $"B[{bin?.Length ?? 0}]";
                default: return $"{Format}[{Value}]";
            }
        }
    }
}
