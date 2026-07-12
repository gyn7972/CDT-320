using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Modules;

namespace QMC.Vision.Core
{
    /// <summary>
    /// COC START부터 END까지 Bottom 카메라 라이브 프레임을 모두 누적하고,
    /// 누적 영상의 좌우/상하 대칭 중심을 픽셀 좌표로 계산한다.
    /// </summary>
    public static class ColletRotationCenterCore
    {
        public const string ToolId = "ColletRotCenterFinder";

        private const int FirstFrameTimeoutMs = 10000;
        private const int MinimumFrameCount = 8;
        private const int PreviewMaxDimension = 1600;

        private sealed class Session
        {
            public readonly object Sync = new object();
            public readonly ManualResetEventSlim FirstFrame = new ManualResetEventSlim(false);
            public Action<GrabResult> Handler;
            public long[] PreviewSum;      // 누적 평균의 2D 다운스케일 영상(대칭 축 탐색 + 미리보기 공용)
            public int Width;
            public int Height;
            public int PreviewWidth;
            public int PreviewHeight;
            public int Count;
            public bool Accepting = true;
            public QMC.Vision.Cameras.Mil.MilCamera MilCamera;
            public int OriginalMilPreviewFps;
        }

        private sealed class LastResult
        {
            public double X;
            public double Y;
            public int Frames;
            public int SourceWidth;
            public int SourceHeight;
            public int DisplayWidth;
            public int DisplayHeight;
            public DateTime Time;
            public long Seq;
        }

        private static readonly ConcurrentDictionary<string, Session> Sessions =
            new ConcurrentDictionary<string, Session>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, LastResult> LastResults =
            new ConcurrentDictionary<string, LastResult>(StringComparer.OrdinalIgnoreCase);
        private static long _resultSequence;

        public static bool IsRunning(string moduleName)
        {
            return !string.IsNullOrWhiteSpace(moduleName) && Sessions.ContainsKey(moduleName);
        }

        public static bool TryGetLast(
            string moduleName,
            out double x,
            out double y,
            out int frames,
            out DateTime time,
            out long sequence)
        {
            x = 0.0;
            y = 0.0;
            frames = 0;
            time = DateTime.MinValue;
            sequence = 0;

            LastResult result;
            if (string.IsNullOrWhiteSpace(moduleName) || !LastResults.TryGetValue(moduleName, out result))
                return false;

            x = result.X;
            y = result.Y;
            frames = result.Frames;
            time = result.Time;
            sequence = result.Seq;
            return true;
        }

        public static bool TryGetLastDisplayInfo(
            string moduleName,
            out int sourceWidth,
            out int sourceHeight,
            out int displayWidth,
            out int displayHeight)
        {
            sourceWidth = 0;
            sourceHeight = 0;
            displayWidth = 0;
            displayHeight = 0;

            LastResult result;
            if (string.IsNullOrWhiteSpace(moduleName) || !LastResults.TryGetValue(moduleName, out result))
                return false;

            sourceWidth = result.SourceWidth;
            sourceHeight = result.SourceHeight;
            displayWidth = result.DisplayWidth;
            displayHeight = result.DisplayHeight;
            return true;
        }

        public static string Start(IVisionModule module)
        {
            if (module == null)
                return "fail:COC 대상 Vision 모듈이 없습니다.";
            if (module.Camera == null)
                return "fail:COC 대상 카메라가 없습니다.";
            if (module.GetAlgorithm(ToolId) == null)
                return "fail:COC 촬상 도구가 없습니다. tool=" + ToolId;
            if (Sessions.ContainsKey(module.Name))
                return "fail:COC가 이미 실행 중입니다.";

            ICamera camera = module.Camera;
            Session session = null;
            bool subscribed = false;

            try
            {
                if (camera.IsGrabbing)
                {
                    Log(module.Name, "COC 시작 전 기존 Live를 정지합니다.");
                    camera.StopLive();
                }

                module.PrepareToolAcquisition(ToolId);

                session = new Session();
                session.Handler = delegate(GrabResult frame)
                {
                    try
                    {
                        if (frame == null || !frame.IsSuccess || frame.Image == null)
                            return;

                        lock (session.Sync)
                        {
                            if (!session.Accepting)
                                return;

                            Accumulate(session, frame.Image);
                            session.FirstFrame.Set();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(module.Name, "COC 프레임 누적 실패. error=" + ex.Message);
                    }
                };

                if (!Sessions.TryAdd(module.Name, session))
                {
                    session.FirstFrame.Dispose();
                    return "fail:COC가 이미 실행 중입니다.";
                }

                camera.FrameReceived += session.Handler;
                subscribed = true;

                session.MilCamera = camera as QMC.Vision.Cameras.Mil.MilCamera;
                if (session.MilCamera != null)
                {
                    session.OriginalMilPreviewFps = session.MilCamera.LivePreviewFps;
                    session.MilCamera.LivePreviewFps = 0;
                    Log(module.Name,
                        "COC 누적 중 MIL Live 프레임 상한을 해제합니다. originalFps=" +
                        session.OriginalMilPreviewFps);
                }
                else
                {
                    camera.TriggerMode = CameraTriggerMode.Continuous;
                }

                camera.StartLive();
                if (!camera.IsGrabbing)
                    throw new InvalidOperationException("카메라 Live 시작 상태를 확인할 수 없습니다.");
                if (!session.FirstFrame.Wait(FirstFrameTimeoutMs))
                    throw new TimeoutException("COC Live 첫 프레임 수신시간을 초과했습니다. timeoutMs=" + FirstFrameTimeoutMs);

                Log(module.Name,
                    "COC START 완료. 누적 Live=ON, firstFrame=수신, tool=" + ToolId + " - Ok");
                return "OK;started=1";
            }
            catch (Exception ex)
            {
                Session removed;
                Sessions.TryRemove(module.Name, out removed);

                if (session != null)
                {
                    if (subscribed)
                    {
                        try { camera.FrameReceived -= session.Handler; } catch { }
                    }
                    lock (session.Sync)
                        session.Accepting = false;
                    session.FirstFrame.Dispose();
                }

                try
                {
                    if (camera.IsGrabbing)
                        camera.StopLive();
                }
                catch { }
                RestoreCameraFramePolicy(session);

                Log(module.Name, "COC START 실패. error=" + ex.Message);
                return "fail:" + ex.Message;
            }
        }

        public static string End(IVisionModule module)
        {
            if (module == null)
                return "fail:COC 대상 Vision 모듈이 없습니다.";

            Session session;
            if (!Sessions.TryRemove(module.Name, out session))
                return "fail:COC START 세션이 없습니다.";

            ICamera camera = module.Camera;
            try
            {
                bool liveActiveAtEnd = camera != null && camera.IsGrabbing;
                if (camera != null)
                    camera.FrameReceived -= session.Handler;

                lock (session.Sync)
                    session.Accepting = false;

                if (!liveActiveAtEnd)
                {
                    const string reason = "COC END 전에 카메라 Live가 이미 정지되었습니다.";
                    Log(module.Name, reason);
                    return "fail:" + reason;
                }

                if (camera != null && camera.IsGrabbing)
                    camera.StopLive();

                if (session.Count < MinimumFrameCount || session.PreviewSum == null)
                {
                    string reason = "COC 누적 프레임이 부족합니다. frames=" + session.Count +
                                    ", required=" + MinimumFrameCount;
                    Log(module.Name, reason);
                    return "fail:" + reason;
                }

                double centerX;
                double centerY;
                double symmetryX;
                double symmetryY;
                if (!FindSymmetryCenter(session, out centerX, out centerY, out symmetryX, out symmetryY))
                {
                    const string reason = "COC 누적 영상에서 좌우/상하 대칭 중심을 찾지 못했습니다.";
                    Log(module.Name, reason);
                    return "fail:" + reason;
                }

                int displayWidth = session.Width;
                int displayHeight = session.Height;
                bool previewPublished = false;
                try
                {
                    using (Bitmap preview = BuildAveragePreview(session))
                    {
                        if (preview != null)
                        {
                            displayWidth = preview.Width;
                            displayHeight = preview.Height;
                            module.PublishViewerFrame(preview);
                            previewPublished = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(module.Name, "COC 누적 평균 미리보기 생성 실패. 중심 결과는 유지합니다. error=" + ex.Message);
                }

                var result = new LastResult
                {
                    X = centerX,
                    Y = centerY,
                    Frames = session.Count,
                    SourceWidth = session.Width,
                    SourceHeight = session.Height,
                    DisplayWidth = previewPublished ? displayWidth : session.Width,
                    DisplayHeight = previewPublished ? displayHeight : session.Height,
                    Time = DateTime.Now,
                    Seq = Interlocked.Increment(ref _resultSequence)
                };
                LastResults[module.Name] = result;

                double displayX = centerX * result.DisplayWidth / Math.Max(1.0, result.SourceWidth);
                double displayY = centerY * result.DisplayHeight / Math.Max(1.0, result.SourceHeight);
                string values = "centerX=" + centerX.ToString("F3", CultureInfo.InvariantCulture) +
                                ";centerY=" + centerY.ToString("F3", CultureInfo.InvariantCulture) +
                                ";frames=" + session.Count.ToString(CultureInfo.InvariantCulture) +
                                ";width=" + session.Width.ToString(CultureInfo.InvariantCulture) +
                                ";height=" + session.Height.ToString(CultureInfo.InvariantCulture) +
                                ";symmetryX=" + symmetryX.ToString("F6", CultureInfo.InvariantCulture) +
                                ";symmetryY=" + symmetryY.ToString("F6", CultureInfo.InvariantCulture);

                try { ModuleResultStore.Record(module.Name, "COC", true, values); } catch { }
                try { ModuleResultStore.RecordMark(module.Name, "COC", displayX, displayY, 1.0); } catch { }
                Log(module.Name,
                    "COC END 완료. centerPixel=(" + centerX.ToString("F3", CultureInfo.InvariantCulture) +
                    "," + centerY.ToString("F3", CultureInfo.InvariantCulture) + ")" +
                    ", frames=" + session.Count +
                    ", symmetry=(" + symmetryX.ToString("F6", CultureInfo.InvariantCulture) +
                    "," + symmetryY.ToString("F6", CultureInfo.InvariantCulture) + ") - Ok");
                return "OK;" + values;
            }
            catch (Exception ex)
            {
                Log(module.Name, "COC END 실패. error=" + ex.Message);
                return "fail:" + ex.Message;
            }
            finally
            {
                try
                {
                    if (camera != null)
                        camera.FrameReceived -= session.Handler;
                }
                catch { }
                try
                {
                    if (camera != null && camera.IsGrabbing)
                        camera.StopLive();
                }
                catch { }
                RestoreCameraFramePolicy(session);
                session.FirstFrame.Dispose();
            }
        }

        public static bool Abort(IVisionModule module, string reason)
        {
            if (module == null)
                return false;

            Session session;
            if (!Sessions.TryRemove(module.Name, out session))
                return false;

            ICamera camera = module.Camera;
            try
            {
                if (camera != null)
                    camera.FrameReceived -= session.Handler;
                lock (session.Sync)
                    session.Accepting = false;
                if (camera != null && camera.IsGrabbing)
                    camera.StopLive();

                Log(module.Name,
                    "COC 세션을 결과 저장 없이 정리했습니다. reason=" +
                    (string.IsNullOrWhiteSpace(reason) ? "지정되지 않음" : reason));
                return true;
            }
            catch (Exception ex)
            {
                Log(module.Name, "COC 비정상 종료 정리 실패. error=" + ex.Message);
                return false;
            }
            finally
            {
                try
                {
                    if (camera != null)
                        camera.FrameReceived -= session.Handler;
                }
                catch { }
                try
                {
                    if (camera != null && camera.IsGrabbing)
                        camera.StopLive();
                }
                catch { }
                RestoreCameraFramePolicy(session);
                session.FirstFrame.Dispose();
            }
        }

        private static void RestoreCameraFramePolicy(Session session)
        {
            if (session == null || session.MilCamera == null)
                return;

            try
            {
                session.MilCamera.LivePreviewFps = session.OriginalMilPreviewFps;
            }
            catch { }
            session.MilCamera = null;
        }

        private static unsafe void Accumulate(Session session, Bitmap source)
        {
            int width = source.Width;
            int height = source.Height;
            PixelFormat format = source.PixelFormat;
            int bytesPerPixel = ResolveBytesPerPixel(format);
            if (bytesPerPixel == 0)
                throw new InvalidOperationException("COC에서 지원하지 않는 픽셀 형식입니다. format=" + format);

            EnsureBuffers(session, width, height);
            byte[] grayLookup = bytesPerPixel == 1 ? BuildGrayLookup(source) : null;

            BitmapData data = source.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                format);
            try
            {
                byte* scan0 = (byte*)data.Scan0.ToPointer();
                int stride = data.Stride;

                // 누적은 2D 다운스케일 평균 영상 한 장에만 더한다. 이 영상을 END에서 그대로
                // 좌우/상하로 접어 대칭 축을 찾는다(별도 투영/에지/배경 처리 없음).
                Parallel.For(0, session.PreviewHeight, delegate(int previewY)
                {
                    int sourceY = Math.Min(height - 1,
                        (int)(((previewY + 0.5) * height) / session.PreviewHeight));
                    byte* row = scan0 + (sourceY * stride);
                    int previewOffset = previewY * session.PreviewWidth;
                    for (int previewX = 0; previewX < session.PreviewWidth; previewX++)
                    {
                        int sourceX = Math.Min(width - 1,
                            (int)(((previewX + 0.5) * width) / session.PreviewWidth));
                        session.PreviewSum[previewOffset + previewX] +=
                            ReadGray(row, sourceX, bytesPerPixel, grayLookup);
                    }
                });
            }
            finally
            {
                source.UnlockBits(data);
            }

            session.Count++;
        }

        private static void EnsureBuffers(Session session, int width, int height)
        {
            if (session.PreviewSum != null && session.Width == width && session.Height == height)
                return;

            double previewScale = Math.Min(
                1.0,
                (double)PreviewMaxDimension / Math.Max(width, height));
            session.Width = width;
            session.Height = height;
            session.PreviewWidth = Math.Max(1, (int)Math.Round(width * previewScale));
            session.PreviewHeight = Math.Max(1, (int)Math.Round(height * previewScale));
            session.PreviewSum = new long[session.PreviewWidth * session.PreviewHeight];
            session.Count = 0;
        }

        private static int ResolveBytesPerPixel(PixelFormat format)
        {
            if (format == PixelFormat.Format8bppIndexed)
                return 1;
            if (format == PixelFormat.Format24bppRgb)
                return 3;
            if (format == PixelFormat.Format32bppRgb ||
                format == PixelFormat.Format32bppArgb ||
                format == PixelFormat.Format32bppPArgb)
                return 4;
            return 0;
        }

        private static byte[] BuildGrayLookup(Bitmap source)
        {
            var lookup = new byte[256];
            try
            {
                Color[] entries = source.Palette.Entries;
                for (int i = 0; i < lookup.Length; i++)
                {
                    if (i < entries.Length)
                    {
                        Color color = entries[i];
                        lookup[i] = (byte)((color.B * 114 + color.G * 587 + color.R * 299) / 1000);
                    }
                    else
                    {
                        lookup[i] = (byte)i;
                    }
                }
            }
            catch
            {
                for (int i = 0; i < lookup.Length; i++)
                    lookup[i] = (byte)i;
            }
            return lookup;
        }

        private static unsafe int ReadGray(byte* row, int x, int bytesPerPixel, byte[] grayLookup)
        {
            if (bytesPerPixel == 1)
                return grayLookup[row[x]];

            int offset = x * bytesPerPixel;
            return (row[offset] * 114 + row[offset + 1] * 587 + row[offset + 2] * 299) / 1000;
        }

        private static Bitmap BuildAveragePreview(Session session)
        {
            if (session.PreviewSum == null || session.Count <= 0)
                return null;

            int width = session.PreviewWidth;
            int height = session.PreviewHeight;
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);
            try
            {
                IntPtr scan0 = data.Scan0;
                int stride = data.Stride;
                Parallel.For(0, height, delegate(int y)
                {
                    var row = new byte[Math.Abs(stride)];
                    int sourceOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        long average = session.PreviewSum[sourceOffset + x] / session.Count;
                        byte value = (byte)Math.Max(0, Math.Min(255, average));
                        int offset = x * 3;
                        row[offset] = value;
                        row[offset + 1] = value;
                        row[offset + 2] = value;
                    }
                    System.Runtime.InteropServices.Marshal.Copy(
                        row,
                        0,
                        IntPtr.Add(scan0, y * stride),
                        row.Length);
                });
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return bitmap;
        }

        // 대칭 축 후보 탐색 범위 — 회전 중심은 시야 중앙 부근이므로 가장자리(우연 대칭)를 제외한다.
        private const double AxisSearchLow = 0.15;
        private const double AxisSearchHigh = 0.85;

        /// <summary>누적 평균의 2D 영상을 그대로 좌우/상하로 접어, 접힘이 가장 잘 맞는(대칭인) 축을 찾는다.
        /// 배경 제거·에지 추출·투영 없이 원본 밝기값을 미러 비교한다. 좌우 대칭 축=중심 X, 상하 대칭 축=중심 Y.</summary>
        private static bool FindSymmetryCenter(
            Session session,
            out double centerX,
            out double centerY,
            out double symmetryX,
            out double symmetryY)
        {
            centerX = 0.0;
            centerY = 0.0;
            symmetryX = 0.0;
            symmetryY = 0.0;

            long[] image = session.PreviewSum;
            int width = session.PreviewWidth;
            int height = session.PreviewHeight;
            if (image == null || width < 8 || height < 8)
                return false;

            double axisX = FindVerticalMirrorAxis(image, width, height, out symmetryX);   // 좌우 대칭 축(열)
            double axisY = FindHorizontalMirrorAxis(image, width, height, out symmetryY);  // 상하 대칭 축(행)
            if (axisX < 0.0 || axisY < 0.0)
                return false;

            // 다운스케일 프리뷰 좌표 → 원본 픽셀 좌표.
            centerX = axisX * session.Width / (double)width;
            centerY = axisY * session.Height / (double)height;
            return true;
        }

        /// <summary>좌우 대칭 축(세로선 x=c) 탐색 — 후보 열 c 마다 좌우로 접어 |왼쪽 - 오른쪽| 을 전 행에 대해 합산,
        /// 겹치는 픽셀당 평균 차이가 최소인 c 가 대칭 축이다. 포물선 보간으로 서브픽셀까지 구한다.</summary>
        private static double FindVerticalMirrorAxis(long[] image, int width, int height, out double bestCost)
        {
            int lo = Math.Max(2, (int)(width * AxisSearchLow));
            int hi = Math.Min(width - 3, (int)(width * AxisSearchHigh));
            if (hi <= lo) { lo = 2; hi = width - 3; }

            var cost = new double[width];
            for (int i = 0; i < width; i++)
                cost[i] = double.NaN;

            Parallel.For(lo, hi + 1, delegate(int c)
            {
                int reach = Math.Min(c, width - 1 - c);
                if (reach < 1)
                    return;

                double diffSum = 0.0;
                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * width;
                    for (int d = 1; d <= reach; d++)
                        diffSum += Math.Abs((double)(image[rowOffset + c - d] - image[rowOffset + c + d]));
                }
                cost[c] = diffSum / ((double)reach * height);   // 겹치는 픽셀당 평균 차이
            });

            return SelectBestAxis(cost, lo, hi, out bestCost);
        }

        /// <summary>상하 대칭 축(가로선 y=c) 탐색 — 후보 행 c 마다 상하로 접어 |위 - 아래| 를 전 열에 대해 합산.</summary>
        private static double FindHorizontalMirrorAxis(long[] image, int width, int height, out double bestCost)
        {
            int lo = Math.Max(2, (int)(height * AxisSearchLow));
            int hi = Math.Min(height - 3, (int)(height * AxisSearchHigh));
            if (hi <= lo) { lo = 2; hi = height - 3; }

            var cost = new double[height];
            for (int i = 0; i < height; i++)
                cost[i] = double.NaN;

            Parallel.For(lo, hi + 1, delegate(int c)
            {
                int reach = Math.Min(c, height - 1 - c);
                if (reach < 1)
                    return;

                double diffSum = 0.0;
                for (int d = 1; d <= reach; d++)
                {
                    int upOffset = (c - d) * width;
                    int downOffset = (c + d) * width;
                    for (int x = 0; x < width; x++)
                        diffSum += Math.Abs((double)(image[upOffset + x] - image[downOffset + x]));
                }
                cost[c] = diffSum / ((double)reach * width);
            });

            return SelectBestAxis(cost, lo, hi, out bestCost);
        }

        /// <summary>비용 배열에서 최소 축을 고르고 포물선 보간으로 서브픽셀 위치를 반환. 유효 축이 없으면 -1.</summary>
        private static double SelectBestAxis(double[] cost, int lo, int hi, out double bestCost)
        {
            bestCost = double.MaxValue;
            int best = -1;
            for (int c = lo; c <= hi; c++)
            {
                if (double.IsNaN(cost[c]) || cost[c] >= bestCost)
                    continue;
                bestCost = cost[c];
                best = c;
            }
            if (best < 0)
                return -1.0;

            // 포물선 보간(양옆 비용) — 접힘 차이가 최소인 지점의 서브픽셀 위치.
            if (best > lo && best < hi && !double.IsNaN(cost[best - 1]) && !double.IsNaN(cost[best + 1]))
            {
                double left = cost[best - 1];
                double center = cost[best];
                double right = cost[best + 1];
                double denom = left - (2.0 * center) + right;
                if (denom > 1e-9)
                {
                    double delta = 0.5 * (left - right) / denom;
                    if (delta > -1.0 && delta < 1.0)
                        return best + delta;
                }
            }
            return best;
        }

        private static void Log(string moduleName, string message)
        {
            try { QMC.Vision.Comm.VisionCommLog.Add("[COC] " + message); } catch { }
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "VISION",
                    "ColletRotationCenter",
                    (moduleName ?? "-") + " " + message);
            }
            catch { }
        }
    }
}
