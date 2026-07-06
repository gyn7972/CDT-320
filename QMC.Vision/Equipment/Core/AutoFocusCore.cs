using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using QMC.Vision.Config;

namespace QMC.Vision.Core
{
    /// <summary>초점 점수 계산 백엔드.</summary>
    public enum FocusBackend { Cpu, Cuda }
    /// <summary>
    /// 오토포커스 선명도(Score) 측정 코어. CDT-310 <c>AutoFocuser.ScoreFocus</c> 기반.
    /// <para>
    /// 알고리즘: 오브젝트 임계값(<paramref name="objThreshold"/>) 초과 밝기 픽셀에서
    /// 8-이웃 라플라시안(스텝 3) |응답|의 RMS ÷ 8 을 Score 로 반환한다(0~255 스케일).
    /// Score 가 클수록 초점이 맞은 상태. 전체 프레임 채점 시 가장자리 1/3 은 제외.
    /// (구 310 방식 '255 클립 응답의 상위 200픽셀 평균'은 포화로 값이 255 에 고정되는 결함이 있어 교체.)
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

        // ── CUDA 백엔드 (장비 PC에서 GPU 가속, 없으면 CPU 폴백) ──
        // 콜렛(ColletStdDevFilter)과 동일 패턴: 기동 시 자동 감지, DLL/커널/디바이스 부재 시 CPU.

        /// <summary>CUDA 디바이스 사용 가능 여부(드라이버/디바이스 존재).</summary>
        public static bool CudaAvailable { get; private set; }

        /// <summary>CUDA 디바이스 이름(가능 시).</summary>
        public static string CudaDeviceName { get; private set; } = string.Empty;

        /// <summary>focus-score 커널이 DLL 에 export 되어 호출 가능한지(미빌드면 false → CPU).</summary>
        private static bool _focusKernelAvailable;

        /// <summary>focus-score CUDA 커널 사용 가능 여부(AutoFocusCuda.dll 에 af_focus_score_cuda export 존재). 진단용.</summary>
        public static bool FocusKernelAvailable { get { return _focusKernelAvailable; } }

        /// <summary>직전 Score 호출에 실제로 사용된 백엔드.</summary>
        public static FocusBackend LastBackend { get; private set; } = FocusBackend.Cpu;

        /// <summary>설정상 CUDA 사용 선호(기본 true). 실제 사용은 가용성 AND 이 토글.</summary>
        private static bool UseCudaPreferred
        {
            get { return VisionConfigStore.Current == null || VisionConfigStore.Current.AutoFocusUseCuda; }
        }

        static AutoFocusCore()
        {
            // 1) CUDA 디바이스 감지(오토포커스 전용 AutoFocusCuda.dll). DLL/드라이버 없으면 예외 → CPU.
            try
            {
                int n = AutoFocusNativeCuda.af_cuda_device_count();
                if (n > 0)
                {
                    byte[] buf = new byte[256];
                    if (AutoFocusNativeCuda.af_cuda_device_name(0, buf, buf.Length) == 0)
                    {
                        int len = Array.IndexOf(buf, (byte)0);
                        if (len < 0) len = buf.Length;
                        CudaDeviceName = System.Text.Encoding.ASCII.GetString(buf, 0, len);
                    }
                    CudaAvailable = true;
                }
            }
            catch { CudaAvailable = false; }

            // 2) focus-score 커널 export 존재 여부를 1회 프로브(없으면 매 호출 예외를 피하려 CPU 고정).
            if (CudaAvailable)
            {
                try
                {
                    double s;
                    byte[] tiny = new byte[16];   // 4x4 더미
                    AutoFocusNativeCuda.af_focus_score_cuda(tiny, 4, 4, 0, 0.0, out s);
                    _focusKernelAvailable = true;   // 정상 호출됨 = export 존재
                }
                catch (EntryPointNotFoundException) { _focusKernelAvailable = false; }   // 커널 미빌드
                catch (DllNotFoundException) { _focusKernelAvailable = false; }
                catch { _focusKernelAvailable = true; }   // 다른 런타임 예외면 export 는 있음 → 정상 호출에서 폴백
            }

            // 진단 1회 로그 — 왜 CPU/CUDA 인지 명확히. (device 없음 / 커널 export 없음 / 사용 가능 구분)
            try
            {
                string reason = !CudaAvailable ? "CUDA 디바이스/AutoFocusCuda.dll 없음"
                              : !_focusKernelAvailable ? "디바이스는 있으나 af_focus_score_cuda 커널 미export(네이티브 미구현)"
                              : "CUDA 사용 가능";
                QMC.Vision.Comm.VisionCommLog.Add("[AutoFocusCore] CUDA detect: device=" + CudaAvailable +
                    "(" + (string.IsNullOrEmpty(CudaDeviceName) ? "-" : CudaDeviceName) + ")" +
                    ", focusKernel=" + _focusKernelAvailable + " → " + reason);
            }
            catch { }
        }

        /// <summary>
        /// grayscale 버퍼 기준 Score — CUDA 가능·선호 시 GPU, 아니면 CPU(<see cref="ScoreFocus(byte[], int, int, int, int, double, out byte[])"/>).
        /// 결과는 두 경로가 동일하도록 네이티브 커널이 CPU 알고리즘을 그대로 구현해야 한다.
        /// </summary>
        private static double ScoreGray(byte[] gray, int w, int h, int bgThreshold, int objThreshold, double marginFraction)
        {
            if (gray == null) return 0;

            // CUDA 커널(af_focus_score_cuda)은 구 알고리즘(255 클립 + 상위 200픽셀 평균) 구현이라
            // 새 채점(비클립 |라플라시안| RMS)과 결과가 달라진다. 커널을 새 알고리즘으로 재빌드해
            // CPU 와 결과 일치를 확인하기 전까지 채점은 CPU 로 고정한다(정확성 우선).
            const bool cudaKernelMatchesRmsScore = false;
#pragma warning disable 162
            if (cudaKernelMatchesRmsScore && CudaAvailable && _focusKernelAvailable && UseCudaPreferred)
            {
                try
                {
                    double s;
                    if (AutoFocusNativeCuda.af_focus_score_cuda(gray, w, h, objThreshold, marginFraction, out s) == 0)
                    {
                        LastBackend = FocusBackend.Cuda;
                        return s;
                    }
                    // 음수 반환 = GPU 실패 → CPU 폴백.
                }
                catch { /* 런타임 예외 → CPU 폴백 */ }
            }
#pragma warning restore 162

            LastBackend = FocusBackend.Cpu;
            return ScoreFocus(gray, w, h, bgThreshold, objThreshold, marginFraction, out _);
        }

        /// <summary>
        /// 비트맵에서 ROI 영역만 8bit grayscale 버퍼로 잘라낸다(전체 Clone 없음, 빠름).
        /// read loop 에서 grab 직후 4개 ROI 만 추출해 작은 버퍼로 큐잉 → 144MP 원본은 즉시 폐기.
        /// </summary>
        public static byte[] ExtractRoiGray(Bitmap bmp, Rectangle roi, out int w, out int h)
        {
            w = 0; h = 0;
            if (bmp == null) return null;
            try
            {
                Rectangle bounds = Rectangle.Intersect(roi, new Rectangle(0, 0, bmp.Width, bmp.Height));
                if (bounds.Width <= 2 * FocusStep || bounds.Height <= 2 * FocusStep) return null;
                return ToGrayscaleRegion(bmp, bounds, out w, out h);
            }
            catch { return null; }
        }

        /// <summary>잘라낸 grayscale 버퍼 채점(백그라운드용). CUDA 가능 시 GPU, 아니면 CPU. ROI 버퍼이므로 marginFraction=0.</summary>
        public static double ScoreGrayBuffer(byte[] gray, int w, int h, int objThreshold = 100)
        {
            if (gray == null || w <= 2 * FocusStep || h <= 2 * FocusStep) return 0;
            try { return ScoreGray(gray, w, h, objThreshold, objThreshold, 0.0); }
            catch { return 0; }
        }

        /// <summary>비트맵 픽셀당 바이트수(8/24/32bpp). 미지원이면 0.</summary>
        public static int BytesPerPixel(PixelFormat pf)
        {
            if (pf == PixelFormat.Format8bppIndexed) return 1;
            if (pf == PixelFormat.Format24bppRgb) return 3;
            if (pf == PixelFormat.Format32bppArgb || pf == PixelFormat.Format32bppRgb || pf == PixelFormat.Format32bppPArgb) return 4;
            return 0;
        }

        /// <summary>
        /// 여러 ROI 의 **raw 픽셀**을 단 한 번의 LockBits 로 잘라낸다(휘도 변환 없이 memcpy 만 — read loop 최소화).
        /// 휘도 변환·채점은 백그라운드에서 <see cref="ScoreRawBuffer"/> 로 수행한다.
        /// <paramref name="rois"/> 는 이미지 내부로 클리핑된 사각형이어야 한다. 반환 배열은 rois 와 1:1(무효는 null).
        /// </summary>
        public static byte[][] ExtractRoisRaw(Bitmap bmp, Rectangle[] rois, out int bpp)
        {
            bpp = 0;
            if (bmp == null || rois == null) return null;
            bpp = BytesPerPixel(bmp.PixelFormat);
            if (bpp == 0) return null;   // 미지원 포맷 → 호출자가 폴백

            var result = new byte[rois.Length][];
            Rectangle full = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bd = bmp.LockBits(full, ImageLockMode.ReadOnly, bmp.PixelFormat);
            try
            {
                int stride = bd.Stride;
                IntPtr scan0 = bd.Scan0;
                for (int r = 0; r < rois.Length; r++)
                {
                    Rectangle roi = Rectangle.Intersect(rois[r], full);
                    if (roi.Width <= 2 * FocusStep || roi.Height <= 2 * FocusStep) { result[r] = null; continue; }
                    int w = roi.Width, h = roi.Height, rowBytes = w * bpp;
                    byte[] raw = new byte[rowBytes * h];
                    for (int y = 0; y < h; y++)
                        System.Runtime.InteropServices.Marshal.Copy(
                            System.IntPtr.Add(scan0, (roi.Y + y) * stride + roi.X * bpp), raw, y * rowBytes, rowBytes);
                    result[r] = raw;
                }
            }
            finally { bmp.UnlockBits(bd); }
            return result;
        }

        /// <summary>raw ROI 버퍼(bpp) 채점(백그라운드용). 휘도 변환 후 CUDA/CPU 채점.</summary>
        public static double ScoreRawBuffer(byte[] raw, int w, int h, int bpp, int objThreshold = 100)
        {
            if (raw == null || w <= 2 * FocusStep || h <= 2 * FocusStep) return 0;
            try
            {
                byte[] gray = (bpp == 1) ? raw : RawToGray(raw, w, h, bpp);
                return ScoreGray(gray, w, h, objThreshold, objThreshold, 0.0);
            }
            catch { return 0; }
        }

        /// <summary>raw(24/32bpp, BGR(A) 순) → 8bit grayscale 휘도 변환.</summary>
        private static byte[] RawToGray(byte[] raw, int w, int h, int bpp)
        {
            byte[] g = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                int row = y * w * bpp, gy = y * w;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x * bpp;
                    byte b = raw[i], gr = raw[i + 1], r = raw[i + 2];
                    g[gy + x] = (byte)((r * 299 + gr * 587 + b * 114) / 1000);
                }
            }
            return g;
        }

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
                // 전체 프레임: 중앙 1/3 만 채점(배경 제외 휴리스틱). CUDA 가능 시 GPU, 아니면 CPU.
                return ScoreGray(gray, w, h, bgThreshold, objThreshold, 1.0 / 3.0);
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

                // ROI 만 직접 LockBits 로 grayscale 추출(144MP 전체를 Clone 하지 않음 → 속도).
                int w, h;
                byte[] gray = ToGrayscaleRegion(bmp, bounds, out w, out h);
                if (gray == null) return 0;
                return ScoreGray(gray, w, h, bgThreshold, objThreshold, 0.0);   // 0.0 = ROI 전체. CUDA 가능 시 GPU.
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// ROI 영역만 LockBits 로 직접 8bit grayscale 추출. 전체 비트맵을 Clone 하지 않으므로
        /// 대형(예: 144MP) 이미지에서 ROI 채점이 크게 빨라진다. <paramref name="roi"/> 는 이미지 내부로 가정.
        /// </summary>
        private static byte[] ToGrayscaleRegion(Bitmap bmp, Rectangle roi, out int width, out int height)
        {
            width = roi.Width;
            height = roi.Height;
            int w = width, h = height;
            byte[] gray = new byte[w * h];
            PixelFormat pf = bmp.PixelFormat;

            if (pf == PixelFormat.Format8bppIndexed)
            {
                BitmapData bd = bmp.LockBits(roi, ImageLockMode.ReadOnly, pf);
                try
                {
                    int stride = bd.Stride;
                    byte[] row = new byte[w];
                    for (int y = 0; y < h; y++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(System.IntPtr.Add(bd.Scan0, y * stride), row, 0, w);
                        Buffer.BlockCopy(row, 0, gray, y * w, w);
                    }
                }
                finally { bmp.UnlockBits(bd); }
                return gray;
            }

            int bpp = (pf == PixelFormat.Format32bppArgb || pf == PixelFormat.Format32bppRgb ||
                       pf == PixelFormat.Format32bppPArgb) ? 4 :
                      (pf == PixelFormat.Format24bppRgb) ? 3 : 0;

            if (bpp == 0)
            {
                // 드문 포맷 → ROI 만 24bpp 로 clone 후 변환(전체 clone 아님).
                using (Bitmap clone = bmp.Clone(roi, PixelFormat.Format24bppRgb))
                    return ToGrayscale(clone, out width, out height);
            }

            BitmapData data = bmp.LockBits(roi, ImageLockMode.ReadOnly, pf);
            try
            {
                int stride = data.Stride;
                int copy = w * bpp;
                byte[] row = new byte[copy];
                for (int y = 0; y < h; y++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(System.IntPtr.Add(data.Scan0, y * stride), row, 0, copy);
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
            finally { bmp.UnlockBits(data); }
            return gray;
        }

        /// <summary>CDT-310 ScoreFocus(중앙 1/3 채점) — 하위호환 시그니처.</summary>
        public static double ScoreFocus(byte[] buffer, int w, int h, int bgThreshold, int objThreshold)
        {
            return ScoreFocus(buffer, w, h, bgThreshold, objThreshold, 1.0 / 3.0, out _);
        }

        /// <summary>
        /// ScoreFocus 본체. <paramref name="marginFraction"/> 만큼 가장자리를 제외하고 채점
        /// (전체프레임=1/3, ROI=0). <paramref name="respMap"/> 에 엣지 응답 맵(처리이미지)을 반환.
        /// <para>
        /// Score = 게이트(objThreshold 초과 밝기) 픽셀의 8-이웃 라플라시안 |응답| RMS ÷ 8 (0~255 스케일).
        /// 구 알고리즘(255 클립 응답의 상위 200픽셀 평균)은 두 가지 결함으로 값이 뭉개졌다:
        /// ① 강한 엣지/노이즈에서 응답이 255 로 포화 → 상위 200개가 전부 255 = Z 를 바꿔도 점수가 255 로 고정,
        /// ② 양(+) 응답만 채점하고 음(−) 응답(어두운 중심 엣지)은 버림 → 엣지 절반 무시.
        /// RMS 는 게이트 픽셀 전체를 반영하므로 포화/노이즈 꼬리에 좌우되지 않고 초점 곡선이 매끄럽게 나온다.
        /// </para>
        /// </summary>
        public static double ScoreFocus(byte[] buffer, int w, int h, int bgThreshold, int objThreshold,
                                        double marginFraction, out byte[] respMap)
        {
            respMap = null;
            if (buffer == null || buffer.Length < w * h) return 0;

            int nStep = FocusStep;
            byte[] src = buffer;                 // 원본(응답 계산용)
            byte[] gate = (byte[])buffer.Clone();// 게이트 판정용(310 buffer2)
            byte[] resp = new byte[w * h];       // 응답 맵(처리이미지 표시용 — 255 클립)

            int mx = (int)(w * marginFraction);
            int my = (int)(h * marginFraction);
            int startX = Math.Max(nStep + mx, nStep);
            int startY = Math.Max(nStep + my, nStep);
            int endX = Math.Min(w - nStep - 1 - mx, w);
            int endY = Math.Min(h - nStep - 1 - my, h);

            int w2 = w * nStep;

            long sumSq = 0;   // |응답|² 누적(비클립) — RMS 채점용
            long gated = 0;   // 게이트 통과 픽셀 수

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

                    int a = Math.Abs(sum);                      // 음(−) 응답도 엣지 — 부호 무시
                    resp[x + nY] = (byte)Math.Min(255, a);      // 표시용 맵만 클립
                    sumSq += (long)a * a;
                    gated++;
                }
            }

            respMap = resp;
            // RMS ÷ 8 — 라플라시안 최대치(8×255)를 0~255 로 정규화해 구 점수와 비슷한 자릿수 유지.
            return gated > 0 ? Math.Sqrt((double)sumSq / gated) / 8.0 : 0.0;
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
