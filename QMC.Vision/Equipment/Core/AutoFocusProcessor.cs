using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using QMC.Vision.Comm;
using QMC.Vision.Config;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 오토포커스 백그라운드 채점기.
    /// <para>
    /// FOCUS_VAL 의 read loop 는 <b>grab + 큐에 넣기만</b> 하고 즉시 ACK 한다(저장만).
    /// 무거운 작업(ROI 잘라내기=LockBits + 휘도변환 + 채점)은 전부 이 처리기의 백그라운드 워커가 한다.
    /// 그래서 ACK 지연 = grab 시간뿐(crop/채점은 다음 grab 과 겹쳐 처리).
    /// </para>
    /// <para>큐는 144MP 원본을 들고 있으므로 상한(<see cref="QueueCapacity"/>)으로 메모리/backpressure 를 제한한다.
    /// FOCUS_BEST 는 <see cref="WaitForDrain"/> 로 완료를 기다린 뒤 best 를 회수한다.</para>
    /// </summary>
    public static class AutoFocusProcessor
    {
        private sealed class Job
        {
            public string Module;
            public FocusCamera Camera;
            public FocusTarget Target;
            public double MotorZ;
            public bool IsInitial;
            public long GrabMs;        // read loop grab 시간
            public GrabResult Grab;    // 144MP 원본(처리 후 Dispose)
            public Rectangle[] Rects;  // 잘라낼 ROI 영역(이미지 좌표)
            public int[] Series;       // 각 ROI 의 시리즈 번호(1~4)
            public int ImgW;
            public int ImgH;
        }

        // 144MP 원본을 큐에 들고 있으므로 상한을 작게(메모리 + backpressure). 메모리 ≈ (상한 + 워커) × 프레임.
        private const int QueueCapacity = 4;
        private static readonly BlockingCollection<Job> _queue = new BlockingCollection<Job>(QueueCapacity);
        private static long _enqueued;
        private static long _processed;

        static AutoFocusProcessor()
        {
            int workers = Environment.ProcessorCount - 1;
            if (workers < 2) workers = 2;
            if (workers > 4) workers = 4;
            for (int i = 0; i < workers; i++)
            {
                Thread t = new Thread(Run) { IsBackground = true, Name = "AutoFocusProcessor" + i };
                t.Start();
            }
        }

        /// <summary>지금까지 큐에 들어온 작업 수(완료 포함).</summary>
        public static long EnqueuedCount { get { return Interlocked.Read(ref _enqueued); } }

        /// <summary>처리 완료 작업 수.</summary>
        public static long ProcessedCount { get { return Interlocked.Read(ref _processed); } }

        // ── Public Methods ──

        /// <summary>grab 원본(소유권 이전)과 잘라낼 ROI 들을 백그라운드 채점 큐에 넣는다. 처리 후 <paramref name="grab"/> 는 Dispose 된다.</summary>
        public static void Enqueue(string module, FocusCamera camera, FocusTarget target,
                                   double motorZ, bool isInitial, long grabMs,
                                   GrabResult grab, Rectangle[] rects, int[] series, int imgW, int imgH)
        {
            if (grab == null) return;
            if (rects == null || rects.Length == 0) { try { grab.Dispose(); } catch { } return; }

            var job = new Job
            {
                Module = module,
                Camera = camera,
                Target = target,
                MotorZ = motorZ,
                IsInitial = isInitial,
                GrabMs = grabMs,
                Grab = grab,
                Rects = rects,
                Series = series,
                ImgW = imgW,
                ImgH = imgH
            };

            // 블로킹 Add — backpressure. 큐가 가득이면 자리 날 때까지 대기(프레임 드롭 없음).
            // 송신자(테스트 SendRecv / 핸들러)는 ACK 를 기다리므로, 이 대기만큼 자연 페이싱되어 큐가 넘치지 않는다.
            // (송신자의 FOCUS_VAL 응답 타임아웃이 이 대기보다 커야 함 — 테스트는 길게 설정.)
            try
            {
                Interlocked.Increment(ref _enqueued);
                _queue.Add(job);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _processed);   // enqueue/processed 카운터 정합 유지
                try { grab.Dispose(); } catch { }
                Debug.WriteLine("[AutoFocusProcessor] Enqueue 실패: " + ex.Message);
            }
        }

        /// <summary>큐의 모든 작업이 처리될 때까지 대기. 완료 시 true, 타임아웃 시 false. timeoutMs&lt;0 이면 무한 대기.</summary>
        public static bool WaitForDrain(int timeoutMs)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                while (Interlocked.Read(ref _processed) < Interlocked.Read(ref _enqueued))
                {
                    if (timeoutMs >= 0 && sw.ElapsedMilliseconds > timeoutMs) return false;
                    Thread.Sleep(5);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AutoFocusProcessor] WaitForDrain 실패: " + ex.Message);
                return false;
            }
        }

        // ── Sequence / Worker ──

        private static void Run()
        {
            foreach (Job job in _queue.GetConsumingEnumerable())
            {
                try
                {
                    ProcessOne(job);
                }
                catch (Exception ex)
                {
                    try { VisionCommLog.Add("[AutoFocusProcessor] 처리 오류: " + ex.Message); } catch { }
                }
                finally
                {
                    try { if (job != null && job.Grab != null) job.Grab.Dispose(); } catch { }
                    Interlocked.Increment(ref _processed);
                }
            }
        }

        // ── Private Methods ──

        private static void ProcessOne(Job job)
        {
            if (job == null || job.Grab == null || !job.Grab.IsSuccess || job.Grab.Image == null) return;

            var inv = CultureInfo.InvariantCulture;
            int afTh = VisionConfigStore.Current != null ? VisionConfigStore.Current.AutoFocusThreshold : 100;

            // ROI 잘라내기(LockBits) + 채점 — 모두 백그라운드에서.
            var swCrop = Stopwatch.StartNew();
            int bpp;
            byte[][] raws = AutoFocusCore.ExtractRoisRaw(job.Grab.Image, job.Rects, out bpp);
            swCrop.Stop();

            var swAlgo = Stopwatch.StartNew();
            double sum = 0; int cnt = 0;
            var rd = new StringBuilder();
            if (raws != null && bpp > 0)
            {
                for (int i = 0; i < raws.Length; i++)
                {
                    if (raws[i] == null) continue;
                    Rectangle bb = job.Rects[i];
                    int seriesNo = (job.Series != null && i < job.Series.Length) ? job.Series[i] : (i + 1);
                    double s = AutoFocusCore.ScoreRawBuffer(raws[i], bb.Width, bb.Height, bpp, afTh);
                    AutoFocusStore.AddSample(job.Camera, job.Target, seriesNo, job.MotorZ, s, job.IsInitial);
                    sum += s; cnt++;
                    // 진단: 입력 raw 버퍼의 최대 픽셀값(0이면 검정/빈 버퍼 → 추출/조명 문제, >0이면 내용 있음).
                    byte rawMax = 0;
                    byte[] rb = raws[i];
                    for (int k = 0; k < rb.Length; k++) if (rb[k] > rawMax) rawMax = rb[k];
                    rd.Append(" roi" + seriesNo + "=" + s.ToString("F0", inv) +
                              "[" + bb.X + "," + bb.Y + " " + bb.Width + "x" + bb.Height + " max=" + rawMax + "]");
                }
            }
            swAlgo.Stop();

            try
            {
                double score = cnt > 0 ? sum / cnt : 0;
                long total = job.GrabMs + swCrop.ElapsedMilliseconds + swAlgo.ElapsedMilliseconds;
                AutoFocusTactLog.Add("z=" + job.MotorZ.ToString("F2", inv) +
                                     "  img=" + job.ImgW + "x" + job.ImgH + " th=" + afTh +
                                     "  total=" + total + "ms(grab " + job.GrabMs + "+crop " + swCrop.ElapsedMilliseconds + "+algo " + swAlgo.ElapsedMilliseconds + ")" +
                                     "  backend=" + AutoFocusCore.LastBackend +
                                     "  score=" + score.ToString("F1", inv) + rd.ToString());
            }
            catch { /* 로그 실패는 처리에 영향 없음 */ }
        }
    }
}
