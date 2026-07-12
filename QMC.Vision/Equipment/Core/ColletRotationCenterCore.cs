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
            public readonly object ColumnMergeSync = new object();
            public readonly ManualResetEventSlim FirstFrame = new ManualResetEventSlim(false);
            public Action<GrabResult> Handler;
            public long[] ColumnSum;
            public long[] RowSum;
            public long[] PreviewSum;
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

                if (session.Count < MinimumFrameCount || session.ColumnSum == null || session.RowSum == null)
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
                if (!FindSymmetryCenter(
                    session.ColumnSum,
                    session.RowSum,
                    out centerX,
                    out centerY,
                    out symmetryX,
                    out symmetryY))
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

                Parallel.For<long[]>(
                    0,
                    height,
                    delegate { return new long[width]; },
                    delegate(int y, ParallelLoopState state, long[] localColumns)
                    {
                        byte* row = scan0 + (y * stride);
                        long rowTotal = 0;
                        for (int x = 0; x < width; x++)
                        {
                            int gray = ReadGray(row, x, bytesPerPixel, grayLookup);
                            rowTotal += gray;
                            localColumns[x] += gray;
                        }
                        session.RowSum[y] += rowTotal;
                        return localColumns;
                    },
                    delegate(long[] localColumns)
                    {
                        lock (session.ColumnMergeSync)
                        {
                            for (int x = 0; x < width; x++)
                                session.ColumnSum[x] += localColumns[x];
                        }
                    });

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
            if (session.ColumnSum != null && session.Width == width && session.Height == height)
                return;

            double previewScale = Math.Min(
                1.0,
                (double)PreviewMaxDimension / Math.Max(width, height));
            session.Width = width;
            session.Height = height;
            session.PreviewWidth = Math.Max(1, (int)Math.Round(width * previewScale));
            session.PreviewHeight = Math.Max(1, (int)Math.Round(height * previewScale));
            session.ColumnSum = new long[width];
            session.RowSum = new long[height];
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

        private static bool FindSymmetryCenter(
            long[] columnProfile,
            long[] rowProfile,
            out double centerX,
            out double centerY,
            out double symmetryX,
            out double symmetryY)
        {
            centerX = BestMirrorCenter(columnProfile, out symmetryX);
            centerY = BestMirrorCenter(rowProfile, out symmetryY);
            return centerX >= 0.0 && centerY >= 0.0;
        }

        private static double BestMirrorCenter(long[] profile, out double bestCost)
        {
            bestCost = double.MaxValue;
            if (profile == null || profile.Length < 16)
                return -1.0;

            // 대칭 이동평균으로 픽셀 노이즈를 먼저 줄인다. 대칭 커널이므로 중심 위치는 이동하지 않는다.
            int profileLength = profile.Length;
            int smoothRadius = Math.Max(2, Math.Min(24, profileLength / 1000));
            var prefix = new double[profileLength + 1];
            for (int i = 0; i < profileLength; i++)
                prefix[i + 1] = prefix[i] + profile[i];
            var smoothed = new double[profileLength];
            for (int i = 0; i < profileLength; i++)
            {
                int first = Math.Max(0, i - smoothRadius);
                int last = Math.Min(profileLength - 1, i + smoothRadius);
                smoothed[i] = (prefix[last + 1] - prefix[first]) / (last - first + 1);
            }

            // 선형 조명 기울기는 인접 차분의 중앙값으로 제거한다. 이후 에지 세기 프로파일을
            // 미러 비교하면 밝은/어두운 콜렛 모두 같은 방식으로 중심을 찾을 수 있다.
            int length = profileLength - 1;
            var slopes = new double[length];
            for (int i = 0; i < length; i++)
                slopes[i] = smoothed[i + 1] - smoothed[i];
            var sortedSlopes = (double[])slopes.Clone();
            Array.Sort(sortedSlopes);
            double backgroundSlope = sortedSlopes[length / 2];

            var residuals = new double[length];
            for (int i = 0; i < length; i++)
                residuals[i] = Math.Abs(slopes[i] - backgroundSlope);
            var sortedResiduals = (double[])residuals.Clone();
            Array.Sort(sortedResiduals);
            double residualMedian = sortedResiduals[length / 2];
            var deviations = new double[length];
            for (int i = 0; i < length; i++)
                deviations[i] = Math.Abs(residuals[i] - residualMedian);
            Array.Sort(deviations);
            double residualMad = deviations[length / 2];
            double noiseThreshold = residualMedian + (3.0 * residualMad);

            var edgeSignal = new double[length];
            double totalWeight = 0.0;
            double weightedPosition = 0.0;
            for (int i = 0; i < length; i++)
            {
                double weight = Math.Max(0.0, residuals[i] - noiseThreshold);
                edgeSignal[i] = weight;
                totalWeight += weight;
                weightedPosition += i * weight;
            }
            if (totalWeight <= double.Epsilon)
            {
                for (int i = 0; i < length; i++)
                {
                    edgeSignal[i] = residuals[i];
                    totalWeight += residuals[i];
                    weightedPosition += i * residuals[i];
                }
            }
            if (totalWeight <= double.Epsilon)
                return -1.0;

            double coarseCenter = weightedPosition / totalWeight;
            double variance = 0.0;
            for (int i = 0; i < length; i++)
            {
                double distance = i - coarseCenter;
                variance += edgeSignal[i] * distance * distance;
            }
            double sigma = Math.Sqrt(variance / totalWeight);
            if (sigma < 1.0)
                return -1.0;

            double searchHalfWidth = Math.Max(4.0, Math.Min(length / 8.0, sigma * 0.75));
            int firstCenter2 = Math.Max(1, (int)Math.Floor((coarseCenter - searchHalfWidth) * 2.0));
            int lastCenter2 = Math.Min((2 * (length - 1)) - 1,
                (int)Math.Ceiling((coarseCenter + searchHalfWidth) * 2.0));
            int desiredPairs = Math.Max(8,
                Math.Min((length / 2) - 1, (int)Math.Ceiling(Math.Max(16.0, sigma * 3.0))));

            int bestCenter2 = -1;
            for (int center2 = firstCenter2; center2 <= lastCenter2; center2++)
            {
                int nearestLeft = (center2 - 1) / 2;
                int nearestRight = center2 - nearestLeft;
                int availablePairs = Math.Min(nearestLeft + 1, length - nearestRight);
                int pairCount = Math.Min(desiredPairs, availablePairs);
                if (pairCount < 8)
                    continue;

                double difference = 0.0;
                double magnitude = 0.0;
                for (int pair = 0; pair < pairCount; pair++)
                {
                    double left = edgeSignal[nearestLeft - pair];
                    double right = edgeSignal[nearestRight + pair];
                    difference += Math.Abs(left - right);
                    magnitude += Math.Abs(left) + Math.Abs(right);
                }
                if (magnitude <= double.Epsilon)
                    continue;

                double cost = difference / magnitude;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestCenter2 = center2;
                }
            }

            // edgeSignal[i]는 원본 픽셀 i와 i+1 사이(i+0.5)에 있으므로 0.5px를 복원한다.
            return bestCenter2 >= 0 ? (bestCenter2 / 2.0) + 0.5 : -1.0;
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
