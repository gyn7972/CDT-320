using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 오토포커스 선명도(Score) 측정 코어. CDT-310 <c>AutoFocuser.ScoreFocus</c> 이식.
    /// <para>
    /// 알고리즘: 중앙 영역(좌우상하 1/3 제외)에서 8-이웃 라플라시안 응답을 구하고,
    /// 오브젝트 임계값(<paramref name="objThreshold"/>) 위 픽셀만 채점한 뒤
    /// 응답 상위 200픽셀 평균을 Score 로 반환한다. Score 가 클수록 초점이 맞은 상태.
    /// </para>
    /// <para>
    /// QMC.Vision 은 <see cref="GrabResult.Image"/> 가 <see cref="Bitmap"/> 이므로
    /// LockBits 로 8bit grayscale 버퍼를 추출한 뒤 310 과 동일한 연산을 수행한다.
    /// 현재 <c>VisionModule.ApproxFocus</c>(간이 RGB gradient) 를 대체하는 용도.
    /// </para>
    /// </summary>
    public static class AutoFocusCore
    {
        /// <summary>라플라시안 커널 반경(310 동일). 중앙 ±nStep 이웃 사용.</summary>
        private const int FocusStep = 3;

        /// <summary>채점에 사용하는 상위 응답 픽셀 수(310 동일).</summary>
        private const int TopPixelCount = 200;

        /// <summary>
        /// 전체 프레임 기준 Score 측정.
        /// </summary>
        /// <param name="bmp">측정 대상 이미지.</param>
        /// <param name="objThreshold">오브젝트(콜렛/다이) 밝기 임계값. 이 값 초과 픽셀만 채점(310 nThreadCollet).</param>
        /// <param name="bgThreshold">배경 임계값. 310 호환을 위해 보존하나 현 알고리즘에서는 미사용(예약).</param>
        /// <returns>Score(>=0). 측정 불가/오류 시 0.</returns>
        public static double Score(Bitmap bmp, int objThreshold = 100, int bgThreshold = 100)
        {
            if (bmp == null) return 0;
            try
            {
                int w, h;
                byte[] gray = ToGrayscale(bmp, out w, out h);
                if (gray == null || w <= 2 * FocusStep || h <= 2 * FocusStep) return 0;
                // 전체 프레임: 중앙 1/3 만 채점(배경 제외 휴리스틱).
                return ScoreFocus(gray, w, h, bgThreshold, objThreshold, 1.0 / 3.0, out _);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// ROI 기준 Score 측정. 사용자가 그린 ROI 는 **전체를 채점**(중앙 1/3 제외 안 함).
        /// </summary>
        public static double Score(Bitmap bmp, Rectangle roi, int objThreshold = 100, int bgThreshold = 100)
        {
            if (bmp == null) return 0;
            try
            {
                Rectangle bounds = Rectangle.Intersect(roi, new Rectangle(0, 0, bmp.Width, bmp.Height));
                if (bounds.Width <= 2 * FocusStep || bounds.Height <= 2 * FocusStep) return 0;

                using (Bitmap sub = bmp.Clone(bounds, bmp.PixelFormat))
                {
                    int w, h;
                    byte[] gray = ToGrayscale(sub, out w, out h);
                    if (gray == null) return 0;
                    return ScoreFocus(gray, w, h, bgThreshold, objThreshold, 0.0, out _);   // 0.0 = ROI 전체
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>CDT-310 ScoreFocus(중앙 1/3 채점) — 하위호환 시그니처.</summary>
        public static double ScoreFocus(byte[] buffer, int w, int h, int bgThreshold, int objThreshold)
        {
            return ScoreFocus(buffer, w, h, bgThreshold, objThreshold, 1.0 / 3.0, out _);
        }

        /// <summary>
        /// ScoreFocus 본체. <paramref name="marginFraction"/> 만큼 가장자리를 제외하고 채점
        /// (전체프레임=1/3, ROI=0). <paramref name="respMap"/> 에 엣지 응답 맵(처리이미지)을 반환.
        /// </summary>
        public static double ScoreFocus(byte[] buffer, int w, int h, int bgThreshold, int objThreshold,
                                        double marginFraction, out byte[] respMap)
        {
            respMap = null;
            if (buffer == null || buffer.Length < w * h) return 0;

            int nStep = FocusStep;
            byte[] src = buffer;                 // 원본(응답 계산용)
            byte[] gate = (byte[])buffer.Clone();// 게이트 판정용(310 buffer2)
            byte[] resp = new byte[w * h];       // 응답 맵(310 buffer3)

            int mx = (int)(w * marginFraction);
            int my = (int)(h * marginFraction);
            int startX = Math.Max(nStep + mx, nStep);
            int startY = Math.Max(nStep + my, nStep);
            int endX = Math.Min(w - nStep - 1 - mx, w);
            int endY = Math.Min(h - nStep - 1 - my, h);

            int w2 = w * nStep;

            for (int y = startY; y < endY; y++)
            {
                int nY = y * w;
                for (int x = startX; x < endX; x++)
                {
                    if (gate[x + nY] <= objThreshold)
                    {
                        resp[x + nY] = 0;
                        continue;
                    }

                    int sum = src[x + nY] * 8;
                    sum -= src[x - nStep + nY - w2];
                    sum -= src[x - nStep + nY];
                    sum -= src[x - nStep + nY + w2];
                    sum -= src[x + nStep + nY - w2];
                    sum -= src[x + nStep + nY];
                    sum -= src[x + nStep + nY + w2];
                    sum -= src[x + nY - w2];
                    sum -= src[x + nY + w2];

                    resp[x + nY] = sum > 0 ? (byte)Math.Min(255, Math.Abs(sum)) : (byte)0;
                }
            }

            respMap = resp;
            // 응답 상위 200픽셀 평균 — 히스토그램으로 O(N)(대형 이미지 정렬 멈춤 방지, 결과 동일).
            int[] hist = new int[256];
            for (int i = 0; i < resp.Length; i++) hist[resp[i]]++;
            long sumTop = 0; int need = TopPixelCount, taken = 0;
            for (int v = 255; v >= 0 && need > 0; v--)
            {
                int take = Math.Min(hist[v], need);
                sumTop += (long)v * take;
                taken += take;
                need -= take;
            }
            return taken > 0 ? (double)sumTop / taken : 0.0;
        }

        /// <summary>
        /// 처리(엣지 응답) 이미지를 생성한다. <paramref name="roi"/> 지정 시 그 영역만(전체 채점),
        /// 없으면 전체 프레임(중앙 1/3). 팝업으로 알고리즘이 무엇을 보는지 확인하는 용도.
        /// </summary>
        public static Bitmap BuildResponseImage(Bitmap bmp, Rectangle? roi, int objThreshold = 100, int bgThreshold = 100)
        {
            double s; return BuildResponseImage(bmp, roi, objThreshold, out s);
        }

        /// <summary>처리 이미지 + Score 를 한 번의 패스로 반환(중복 채점 방지).</summary>
        public static Bitmap BuildResponseImage(Bitmap bmp, Rectangle? roi, int objThreshold, out double score)
        {
            score = 0;
            if (bmp == null) return null;
            try
            {
                Bitmap region;
                bool ownRegion = false;
                double margin;
                if (roi.HasValue)
                {
                    Rectangle bounds = Rectangle.Intersect(roi.Value, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    if (bounds.Width <= 2 * FocusStep || bounds.Height <= 2 * FocusStep) return null;
                    region = bmp.Clone(bounds, bmp.PixelFormat);
                    ownRegion = true;
                    margin = 0.0;
                }
                else { region = bmp; margin = 1.0 / 3.0; }

                try
                {
                    int w, h;
                    byte[] gray = ToGrayscale(region, out w, out h);
                    if (gray == null) return null;
                    byte[] map;
                    score = ScoreFocus(gray, w, h, objThreshold, objThreshold, margin, out map);
                    return GrayToBitmap(map, w, h);
                }
                finally { if (ownRegion) region.Dispose(); }
            }
            catch { return null; }
        }

        /// <summary>8bit grayscale 버퍼 → 24bpp Bitmap(회색 복제).</summary>
        private static Bitmap GrayToBitmap(byte[] gray, int w, int h)
        {
            if (gray == null || gray.Length < w * h) return null;
            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = bd.Stride;
                byte[] row = new byte[stride];
                for (int y = 0; y < h; y++)
                {
                    int gy = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        byte v = gray[gy + x];
                        int i = x * 3;
                        row[i] = v; row[i + 1] = v; row[i + 2] = v;
                    }
                    System.Runtime.InteropServices.Marshal.Copy(row, 0, System.IntPtr.Add(bd.Scan0, y * stride), stride);
                }
            }
            finally { bmp.UnlockBits(bd); }
            return bmp;
        }

        /// <summary>
        /// Bitmap 을 8bit grayscale 버퍼로 변환. 8bpp Indexed(Mono8) 는 직접 복사,
        /// 그 외(24/32bpp)는 휘도(0.299R+0.587G+0.114B) 변환.
        /// </summary>
        private static byte[] ToGrayscale(Bitmap bmp, out int width, out int height)
        {
            width = bmp.Width;
            height = bmp.Height;
            int w = width, h = height;
            byte[] gray = new byte[w * h];

            PixelFormat pf = bmp.PixelFormat;
            Rectangle rect = new Rectangle(0, 0, w, h);

            if (pf == PixelFormat.Format8bppIndexed)
            {
                BitmapData bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, pf);
                try
                {
                    int stride = bd.Stride;
                    IntPtr scan0 = bd.Scan0;
                    byte[] row = new byte[stride];
                    for (int y = 0; y < h; y++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(System.IntPtr.Add(scan0, y * stride), row, 0, stride);
                        Buffer.BlockCopy(row, 0, gray, y * w, w);
                    }
                }
                finally
                {
                    bmp.UnlockBits(bd);
                }
                return gray;
            }

            // 24/32bpp → 휘도 변환.
            int bpp = (pf == PixelFormat.Format32bppArgb || pf == PixelFormat.Format32bppRgb ||
                       pf == PixelFormat.Format32bppPArgb) ? 4 :
                      (pf == PixelFormat.Format24bppRgb) ? 3 : 0;

            if (bpp == 0)
            {
                // 알 수 없는 포맷 → 24bpp 복제 후 변환.
                using (Bitmap clone = bmp.Clone(rect, PixelFormat.Format24bppRgb))
                    return ToGrayscale(clone, out width, out height);
            }

            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, pf);
            try
            {
                int stride = data.Stride;
                byte[] row = new byte[stride];
                for (int y = 0; y < h; y++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(System.IntPtr.Add(data.Scan0, y * stride), row, 0, stride);
                    int gy = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int i = x * bpp;
                        byte b = row[i];
                        byte g = row[i + 1];
                        byte r = row[i + 2];
                        gray[gy + x] = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return gray;
        }
    }
}
