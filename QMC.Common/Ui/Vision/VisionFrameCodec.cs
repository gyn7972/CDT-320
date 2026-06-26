using System;
using System.Drawing;
using System.IO;

namespace QMC.Common.Ui.Controls
{
    /// <summary>
    /// 비전 영상 프레임 와이어 코덱 — 한 프레임 = [4B metaLen LE][meta JSON][4B jpegLen LE][JPEG].
    /// 송신(비전)·수신(핸들러)이 동일 포맷을 공유한다.
    /// </summary>
    public static class VisionFrameCodec
    {
        /// <summary>스트림에 메타+JPEG 한 프레임을 기록.</summary>
        public static void WriteFrame(Stream s, VisionFrameMeta meta, byte[] jpeg)
        {
            byte[] metaBytes = (meta ?? new VisionFrameMeta()).ToBytes();
            s.Write(BitConverter.GetBytes(metaBytes.Length), 0, 4);   // int32 LE
            s.Write(metaBytes, 0, metaBytes.Length);
            s.Write(BitConverter.GetBytes(jpeg.Length), 0, 4);
            s.Write(jpeg, 0, jpeg.Length);
            s.Flush();
        }

        /// <summary>스트림에서 한 프레임(메타+비트맵)을 읽는다. 끊김/오류 시 false.</summary>
        public static bool ReadFrame(Stream s, out VisionFrameMeta meta, out Bitmap bmp)
        {
            meta = null; bmp = null;

            byte[] mlen = ReadExact(s, 4); if (mlen == null) return false;
            int metaLen = BitConverter.ToInt32(mlen, 0);
            if (metaLen < 0 || metaLen > 4 * 1024 * 1024) return false;
            byte[] metaBytes = ReadExact(s, metaLen); if (metaBytes == null) return false;

            byte[] jlen = ReadExact(s, 4); if (jlen == null) return false;
            int jpegLen = BitConverter.ToInt32(jlen, 0);
            if (jpegLen <= 0 || jpegLen > 64 * 1024 * 1024) return false;
            byte[] jpeg = ReadExact(s, jpegLen); if (jpeg == null) return false;

            meta = VisionFrameMeta.FromBytes(metaBytes, 0, metaBytes.Length);
            try
            {
                using (var ms = new MemoryStream(jpeg))
                using (var img = Image.FromStream(ms, false, false))
                    bmp = new Bitmap(img);
            }
            catch { bmp = null; return false; }
            return true;
        }

        private static byte[] ReadExact(Stream s, int n)
        {
            if (n == 0) return new byte[0];
            var buf = new byte[n];
            int off = 0;
            while (off < n)
            {
                int r;
                try { r = s.Read(buf, off, n - off); } catch { return null; }
                if (r <= 0) return null;
                off += r;
            }
            return buf;
        }
    }
}
