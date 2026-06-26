using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ColletFinder
{
    /// <summary>처리에 사용된 백엔드.</summary>
    public enum ComputeBackend { Cpu, Cuda }

    /// <summary>
    /// ROI 내 모든 픽셀을 중심으로 한 블럭(정사각형) 안에서 밝기의 표준편차를 구해,
    /// 임계값 이상이면 흰색(255), 미만이면 검정(0)으로 이진화한 결과 이미지를 만든다.
    ///
    /// CUDA 가 가능하면 GPU(ColletFinderCuda.dll)로, 아니면 CPU(코어 수만큼 멀티스레드)로 처리한다.
    ///   var = E[x^2] - (E[x])^2,  std = sqrt(var)
    /// </summary>
    public static class StdDevFilter
    {
        /// <summary>CPU 논리 코어 수(멀티스레드 처리에 사용).</summary>
        public static int CpuThreads { get; private set; }

        /// <summary>CUDA 사용 가능 여부.</summary>
        public static bool CudaAvailable { get; private set; }

        /// <summary>CUDA 디바이스 이름(가능 시).</summary>
        public static string CudaDeviceName { get; private set; }

        /// <summary>직전 Apply 호출의 단계별 소요시간(ms): 그레이 변환.</summary>
        public static long LastToGrayMs { get; private set; }
        /// <summary>직전 Apply 호출의 단계별 소요시간(ms): 필터 연산(GPU/CPU).</summary>
        public static long LastComputeMs { get; private set; }
        /// <summary>직전 Apply 호출의 단계별 소요시간(ms): 결과 비트맵 생성.</summary>
        public static long LastFromGrayMs { get; private set; }

        static StdDevFilter()
        {
            CpuThreads = Math.Max(1, Environment.ProcessorCount);
            CudaDeviceName = string.Empty;

            // DLL 부재/드라이버 부재 등은 예외로 잡아 CPU 폴백.
            try
            {
                int n = NativeCuda.cf_cuda_device_count();
                if (n > 0)
                {
                    var buf = new byte[256];
                    if (NativeCuda.cf_cuda_device_name(0, buf, buf.Length) == 0)
                    {
                        int len = Array.IndexOf(buf, (byte)0);
                        if (len < 0) len = buf.Length;
                        CudaDeviceName = Encoding.ASCII.GetString(buf, 0, len);
                    }
                    CudaAvailable = true;
                }
            }
            catch
            {
                CudaAvailable = false;
            }
        }

        /// <summary>
        /// 표준편차 텍스처 필터를 적용한다.
        /// </summary>
        /// <param name="source">원본 이미지.</param>
        /// <param name="roi">처리 영역(이미지 좌표). 비어 있으면 전체 이미지.</param>
        /// <param name="blockSize">블럭 한 변의 길이(픽셀).</param>
        /// <param name="threshold">표준편차 임계값. std &gt;= threshold 이면 흰색.</param>
        /// <param name="preferCuda">true 이고 CUDA 가능 시 GPU 사용, 실패하면 CPU 폴백.</param>
        /// <param name="used">실제로 사용된 백엔드.</param>
        /// <returns>원본과 같은 크기의 24bpp 결과 이미지. ROI 밖은 검정.</returns>
        public static Bitmap Apply(Bitmap source, Rectangle roi, int blockSize, double threshold,
                                   bool preferCuda, out ComputeBackend used, out byte[] mask)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (blockSize < 1) blockSize = 1;

            int w = source.Width;
            int h = source.Height;

            roi = Rectangle.Intersect(roi, new Rectangle(0, 0, w, h));
            if (roi.Width <= 0 || roi.Height <= 0)
                roi = new Rectangle(0, 0, w, h);

            var sw = Stopwatch.StartNew();
            byte[] gray = ToGray(source, w, h);
            LastToGrayMs = sw.ElapsedMilliseconds;

            byte[] outGray = null;
            used = ComputeBackend.Cpu;

            sw.Restart();
            // 1) GPU 시도
            if (preferCuda && CudaAvailable)
            {
                try
                {
                    var buf = new byte[w * h];
                    int rc = NativeCuda.cf_stddev_filter_cuda(
                        gray, w, h, roi.X, roi.Y, roi.Width, roi.Height,
                        blockSize, threshold, buf);
                    if (rc == 0)
                    {
                        outGray = buf;
                        used = ComputeBackend.Cuda;
                    }
                }
                catch
                {
                    outGray = null; // CPU 폴백
                }
            }

            // 2) CPU 폴백(멀티스레드)
            if (outGray == null)
            {
                outGray = ComputeCpuParallel(gray, w, h, roi, blockSize, threshold);
                used = ComputeBackend.Cpu;
            }
            LastComputeMs = sw.ElapsedMilliseconds;

            mask = outGray;
            sw.Restart();
            Bitmap result = FromGray(outGray, w, h);
            LastFromGrayMs = sw.ElapsedMilliseconds;
            return result;
        }

        /// <summary>
        /// CPU 처리: 적분영상(Summed-Area Table)으로 블럭 합/제곱합을 O(1)에 구하고,
        /// 픽셀별 표준편차 계산을 코어 수만큼 멀티스레드(Parallel.For)로 분산한다.
        /// </summary>
        private static byte[] ComputeCpuParallel(byte[] gray, int w, int h, Rectangle roi,
                                                 int blockSize, double threshold)
        {
            int sw = w + 1;
            long[] integ = new long[sw * (h + 1)];
            long[] integSq = new long[sw * (h + 1)];

            // 적분영상 생성(행 누적은 순차 의존성이 있어 단일 패스)
            for (int y = 0; y < h; y++)
            {
                long rowSum = 0;
                long rowSumSq = 0;
                int gRow = y * w;
                int iRow = (y + 1) * sw;
                int iPrev = y * sw;
                for (int x = 0; x < w; x++)
                {
                    int g = gray[gRow + x];
                    rowSum += g;
                    rowSumSq += (long)g * g;
                    integ[iRow + x + 1] = integ[iPrev + x + 1] + rowSum;
                    integSq[iRow + x + 1] = integSq[iPrev + x + 1] + rowSumSq;
                }
            }

            byte[] outGray = new byte[w * h];
            int half = blockSize / 2;
            int rx0 = roi.X;
            int ry0 = roi.Y;
            int rx1 = roi.Right - 1;
            int ry1 = roi.Bottom - 1;

            // 픽셀별 계산은 행 단위로 병렬화(각 스레드가 서로 다른 출력 행을 담당 → 경합 없음).
            var po = new ParallelOptions { MaxDegreeOfParallelism = CpuThreads };
            Parallel.For(ry0, ry1 + 1, po, y =>
            {
                int ay0 = y - half;
                if (ay0 < 0) ay0 = 0;
                int ay1 = y - half + blockSize - 1;
                if (ay1 > h - 1) ay1 = h - 1;

                int top = ay0 * sw;
                int bot = (ay1 + 1) * sw;
                int outRow = y * w;

                for (int x = rx0; x <= rx1; x++)
                {
                    int ax0 = x - half;
                    if (ax0 < 0) ax0 = 0;
                    int ax1 = x - half + blockSize - 1;
                    if (ax1 > w - 1) ax1 = w - 1;

                    long n = (long)(ax1 - ax0 + 1) * (ay1 - ay0 + 1);
                    long sum = integ[bot + ax1 + 1] - integ[bot + ax0]
                             - integ[top + ax1 + 1] + integ[top + ax0];
                    long sumSq = integSq[bot + ax1 + 1] - integSq[bot + ax0]
                               - integSq[top + ax1 + 1] + integSq[top + ax0];

                    double mean = (double)sum / n;
                    double var = (double)sumSq / n - mean * mean;
                    if (var < 0) var = 0;
                    double std = Math.Sqrt(var);

                    outGray[outRow + x] = std >= threshold ? (byte)255 : (byte)0;
                }
            });

            return outGray;
        }

        /// <summary>
        /// 원본에서 밝기(그레이) 배열을 만든다.
        /// 24/32bpp 는 원본을 그대로 LockBits 하여 직접 변환(중간 비트맵·DrawImage 없이 → 큰 이미지에서 수백 ms 절약),
        /// 그 외 포맷은 안전하게 24bpp 로 한번 그려서 처리한다.
        /// </summary>
        private static byte[] ToGray(Bitmap src, int w, int h)
        {
            PixelFormat pf = src.PixelFormat;
            int bpp;
            if (pf == PixelFormat.Format32bppArgb || pf == PixelFormat.Format32bppRgb || pf == PixelFormat.Format32bppPArgb)
                bpp = 4;
            else if (pf == PixelFormat.Format24bppRgb)
                bpp = 3;
            else
                return ToGrayViaDraw(src, w, h); // 인덱스/그 외 포맷 폴백

            var rect = new Rectangle(0, 0, w, h);
            BitmapData data = src.LockBits(rect, ImageLockMode.ReadOnly, pf);
            int stride = data.Stride;
            IntPtr scan0 = data.Scan0;
            byte[] gray = new byte[w * h];
            try
            {
                int bytesPerPixel = bpp;
                var po = new ParallelOptions { MaxDegreeOfParallelism = CpuThreads };
                // 행 단위로 처리하며, 스레드별 행 버퍼만 할당(전체 stride*h 중간 버퍼를 만들지 않음)
                Parallel.For(0, h, po, () => new byte[stride], (y, state, rowBuf) =>
                {
                    Marshal.Copy(IntPtr.Add(scan0, y * stride), rowBuf, 0, stride);
                    int gr = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int p = x * bytesPerPixel;     // BGR(A)
                        int b = rowBuf[p];
                        int gg = rowBuf[p + 1];
                        int r = rowBuf[p + 2];
                        // 휘도 근사: 0.299R + 0.587G + 0.114B  (정수 연산)
                        gray[gr + x] = (byte)((r * 77 + gg * 150 + b * 29) >> 8);
                    }
                    return rowBuf;
                }, rowBuf => { });
            }
            finally
            {
                src.UnlockBits(data);
            }
            return gray;
        }

        /// <summary>인덱스/비표준 포맷용 폴백: 24bpp 로 한번 그린 뒤 그레이 변환.</summary>
        private static byte[] ToGrayViaDraw(Bitmap src, int w, int h)
        {
            using (var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.DrawImage(src, new Rectangle(0, 0, w, h));
                }

                var rect = new Rectangle(0, 0, w, h);
                BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                int stride = data.Stride;
                IntPtr scan0 = data.Scan0;
                byte[] gray = new byte[w * h];
                var po = new ParallelOptions { MaxDegreeOfParallelism = CpuThreads };
                Parallel.For(0, h, po, () => new byte[stride], (y, state, rowBuf) =>
                {
                    Marshal.Copy(IntPtr.Add(scan0, y * stride), rowBuf, 0, stride);
                    int gr = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int p = x * 3;                 // 24bpp = BGR
                        int b = rowBuf[p];
                        int gg = rowBuf[p + 1];
                        int r = rowBuf[p + 2];
                        gray[gr + x] = (byte)((r * 77 + gg * 150 + b * 29) >> 8);
                    }
                    return rowBuf;
                }, rowBuf => { });
                bmp.UnlockBits(data);
                return gray;
            }
        }

        /// <summary>그레이 배열로부터 24bpp 결과 비트맵을 만든다.</summary>
        private static Bitmap FromGray(byte[] gray, int w, int h)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, w, h);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            int stride = data.Stride;
            IntPtr scan0 = data.Scan0;
            var po = new ParallelOptions { MaxDegreeOfParallelism = CpuThreads };
            // 행 단위로 BGR 을 채워 곧바로 비트맵에 기록(전체 stride*h 중간 버퍼 미사용).
            Parallel.For(0, h, po, () => new byte[stride], (y, state, rowBuf) =>
            {
                int gr = y * w;
                for (int x = 0; x < w; x++)
                {
                    byte v = gray[gr + x];
                    int p = x * 3;
                    rowBuf[p] = v;
                    rowBuf[p + 1] = v;
                    rowBuf[p + 2] = v;
                }
                Marshal.Copy(rowBuf, 0, IntPtr.Add(scan0, y * stride), stride);
                return rowBuf;
            }, rowBuf => { });
            bmp.UnlockBits(data);
            return bmp;
        }
    }
}
