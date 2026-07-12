using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Modules;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 콜렛 회전 중심(COC) 측정 코어.
    /// <para>
    /// 핸들러가 콜렛을 회전시키는 동안 "COC START" 로 카메라를 라이브(연속)로 켜고 프레임을
    /// 픽셀별 밝기 합/카운트로 누적한다. "COC END" 에서 라이브를 끄고, 누적 평균 영상에서
    /// 가로/세로 방향 미러 대칭 중심을 찾아 회전 중심 (x,y) 를 핸들러에 리턴한다.
    /// </para>
    /// <para>
    /// 원리: 회전하는 물체의 시간 평균 영상은 회전 중심 기준 원형 대칭이 된다 →
    /// 열 프로젝션(colSum)의 좌우 대칭 중심 = 중심 x, 행 프로젝션(rowSum)의 상하 대칭 중심 = 중심 y.
    /// </para>
    /// 프로토콜: "MODULE|COC|START" → "OK;started" / "MODULE|COC|END" → "OK;x=..;y=..;frames=..".
    /// </summary>
    public static class ColletRotationCenterCore
    {
        /// <summary>노출/조명 레시피를 읽어올 도구 노드 id(바텀 검사 콜렛 회전중심 Finder).</summary>
        public const string ToolId = "ColletRotCenterFinder";

        private sealed class Session
        {
            public Action<GrabResult> Handler;
            public int[] Sum;      // 픽셀별 그레이 합
            public int W, H;
            public int Count;      // 누적 프레임 수
            public int Busy;       // 0/1 — 프레임 처리 중이면 새 프레임 드롭(누적 평균이라 드롭 무해)
        }

        private static readonly ConcurrentDictionary<string, Session> _sessions =
            new ConcurrentDictionary<string, Session>(StringComparer.OrdinalIgnoreCase);

        // ── Public Methods ──────────────────────────────────────────

        /// <summary>COC 시작 — 도구 노출/조명 적용 후 카메라 라이브 시작 + 프레임 누적 구독.</summary>
        public static string Start(IVisionModule m)
        {
            if (m == null) return "fail:no module";
            var cam = m.Camera;
            if (cam == null) return "fail:no camera";

            try
            {
                // 이전 세션 잔존 시 정리 후 재시작(핸들러 재시도 대응).
                if (_sessions.TryRemove(m.Name, out var old))
                    try { cam.FrameReceived -= old.Handler; } catch { }

                // 콜렛 회전중심 도구의 레시피 노출/조명 적용(캐시 히트면 통신 생략).
                try { m.PrepareToolAcquisition(ToolId); } catch { }

                var s = new Session();
                s.Handler = r =>
                {
                    try
                    {
                        if (r == null || !r.IsSuccess || r.Image == null) return;
                        if (Interlocked.CompareExchange(ref s.Busy, 1, 0) != 0) return;   // 처리 중 → 드롭
                        try { Accumulate(s, r.Image); }
                        finally { Interlocked.Exchange(ref s.Busy, 0); }
                    }
                    catch (Exception ex)
                    {
                        try { QMC.Vision.Comm.VisionCommLog.Add("[COC] 프레임 누적 실패: " + ex.Message); } catch { }
                    }
                };
                _sessions[m.Name] = s;

                cam.FrameReceived += s.Handler;
                try { cam.TriggerMode = CameraTriggerMode.Continuous; } catch { }
                cam.StartLive();

                try { QMC.Vision.Comm.VisionCommLog.Add("[COC] 시작 — 누적 라이브 ON (" + m.Name + ")"); } catch { }
                return "OK;started";
            }
            catch (Exception ex)
            {
                _sessions.TryRemove(m.Name, out _);
                return "fail:" + ex.Message;
            }
        }

        /// <summary>COC 종료 — 라이브 정지 후 누적 평균 영상의 가로/세로 대칭 중심을 회전 중심으로 계산.</summary>
        public static string End(IVisionModule m)
        {
            if (m == null) return "fail:no module";
            if (!_sessions.TryRemove(m.Name, out var s)) return "fail:no session (COC START first)";

            var cam = m.Camera;
            try { cam?.StopLive(); } catch { }
            try { if (cam != null) cam.FrameReceived -= s.Handler; } catch { }

            // 마지막 프레임 누적이 진행 중이면 잠깐 대기(최대 2s).
            for (int i = 0; i < 200 && Volatile.Read(ref s.Busy) != 0; i++) Thread.Sleep(10);

            if (s.Count <= 0 || s.Sum == null) return "fail:no frames accumulated";

            try
            {
                double cx, cy;
                if (!FindSymmetryCenter(s.Sum, s.W, s.H, out cx, out cy))
                    return "fail:symmetry center not found";

                // 누적 평균 영상을 뷰어로 발행 — 작업/레시피 UI 에 결과 영상 표시(디버깅/확인용).
                try
                {
                    using (var avg = BuildAverage(s.Sum, s.W, s.H, s.Count))
                        m.PublishViewerFrame(avg);
                }
                catch { }

                var inv = System.Globalization.CultureInfo.InvariantCulture;
                string items = "x=" + cx.ToString("F2", inv) + ";y=" + cy.ToString("F2", inv) + ";frames=" + s.Count;
                try { ModuleResultStore.Record(m.Name, "COC", true, items); } catch { }
                try { ModuleResultStore.RecordMark(m.Name, "COC", cx, cy, 1.0); } catch { }
                try { QMC.Vision.Comm.VisionCommLog.Add("[COC] 종료 — 회전 중심 (" + cx.ToString("F2", inv)
                    + ", " + cy.ToString("F2", inv) + "), 누적 " + s.Count + "프레임"); } catch { }

                return "OK;" + items;
            }
            catch (Exception ex)
            {
                return "fail:" + ex.Message;
            }
        }

        // ── Private Methods ─────────────────────────────────────────

        /// <summary>프레임 1장을 그레이로 변환해 합 버퍼에 누적(행 병렬). 크기 변경 시 리셋.</summary>
        private static void Accumulate(Session s, Bitmap src)
        {
            int w = src.Width, h = src.Height;
            var fmt = src.PixelFormat;
            int bpp = fmt == PixelFormat.Format8bppIndexed ? 1
                    : fmt == PixelFormat.Format24bppRgb ? 3
                    : (fmt == PixelFormat.Format32bppRgb || fmt == PixelFormat.Format32bppArgb) ? 4 : 0;
            if (bpp == 0) return;

            if (s.Sum == null || s.W != w || s.H != h)
            {
                s.Sum = new int[w * h];
                s.W = w; s.H = h; s.Count = 0;
            }

            var bd = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, fmt);
            try
            {
                IntPtr scan0 = bd.Scan0;
                int stride = bd.Stride;
                int[] sum = s.Sum;
                Parallel.For(0, h, y =>
                {
                    var row = new byte[stride];
                    System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(scan0, y * stride), row, 0, stride);
                    int o = y * w;
                    if (bpp == 1)
                    {
                        for (int x = 0; x < w; x++) sum[o + x] += row[x];
                    }
                    else
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int i = x * bpp;
                            sum[o + x] += (row[i] * 114 + row[i + 1] * 587 + row[i + 2] * 299) / 1000;
                        }
                    }
                });
            }
            finally { src.UnlockBits(bd); }
            s.Count++;
        }

        /// <summary>합÷카운트 평균 흑백 24bpp 영상 생성(뷰어 표시용).</summary>
        private static Bitmap BuildAverage(int[] sum, int w, int h, int count)
        {
            var avg = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            var ad = avg.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                IntPtr s0 = ad.Scan0;
                int stride = ad.Stride;
                Parallel.For(0, h, y =>
                {
                    var row = new byte[stride];
                    int o = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        byte v = (byte)(sum[o + x] / count);
                        int i = x * 3;
                        row[i] = v; row[i + 1] = v; row[i + 2] = v;
                    }
                    System.Runtime.InteropServices.Marshal.Copy(row, 0, IntPtr.Add(s0, y * stride), stride);
                });
            }
            finally { avg.UnlockBits(ad); }
            return avg;
        }

        /// <summary>누적 영상의 가로/세로 대칭 중심 탐색 — 열/행 프로젝션의 미러 대칭 중심(0.5px 격자).</summary>
        private static bool FindSymmetryCenter(int[] sum, int w, int h, out double cx, out double cy)
        {
            cx = cy = 0;

            // 열/행 프로젝션(합 버퍼 그대로 사용 — 평균 나눔은 대칭 위치에 영향 없음).
            long[] colSum = new long[w];
            long[] rowSum = new long[h];
            object mergeLock = new object();
            Parallel.For(0, h,
                () => new long[w],
                (y, st, loc) =>
                {
                    long r = 0;
                    int o = y * w;
                    for (int x = 0; x < w; x++) { int v = sum[o + x]; loc[x] += v; r += v; }
                    rowSum[y] = r;
                    return loc;
                },
                loc => { lock (mergeLock) { for (int x = 0; x < w; x++) colSum[x] += loc[x]; } });

            double bx = BestMirrorCenter(colSum);
            double by = BestMirrorCenter(rowSum);
            if (bx < 0 || by < 0) return false;
            cx = bx; cy = by;
            return true;
        }

        /// <summary>1차원 프로파일의 미러 대칭 중심(0.5px 격자) — 후보 중심마다 대칭 쌍의 정규화 SAD 를
        /// 최소화한다(대칭이면 0). 오버랩이 짧거나 신호가 약한 후보는 제외(우연 대칭 방지). 실패 시 -1.</summary>
        private static double BestMirrorCenter(long[] p)
        {
            int n = p.Length;
            if (n < 16) return -1;

            long total = 0;
            for (int i = 0; i < n; i++) total += p[i];
            if (total <= 0) return -1;

            int minPairs = n / 8;          // 최소 대칭 쌍 수(오버랩)
            long minMag = total / 20;      // 최소 신호량(전체의 5%) — 빈 배경끼리의 우연 대칭 방지

            double bestC = -1, bestCost = double.MaxValue;
            // c2 = 2×중심(정수) → 중심 c = c2/2 (0.5px 격자). 쌍: (i, c2-i).
            for (int c2 = minPairs; c2 <= 2 * (n - 1) - minPairs; c2++)
            {
                int iLo = Math.Max(0, c2 - (n - 1));
                int iHi = (c2 - 1) / 2;                // i < c2-i 인 쌍만
                if (iHi - iLo + 1 < minPairs) continue;

                long sad = 0, mag = 0;
                for (int i = iLo; i <= iHi; i++)
                {
                    long a = p[i], b = p[c2 - i];
                    long d = a - b;
                    sad += d >= 0 ? d : -d;
                    mag += a + b;
                }
                if (mag < minMag) continue;

                double cost = (double)sad / mag;
                if (cost < bestCost) { bestCost = cost; bestC = c2 / 2.0; }
            }
            return bestC;
        }
    }
}
