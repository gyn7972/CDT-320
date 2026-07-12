using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Core
{
    public static class ColletRotationCenterStore
    {
        private const string FinderId = "ColletFinder";
        private const int MinimumSampleCount = 8;
        private static readonly ConcurrentDictionary<string, Session> Sessions =
            new ConcurrentDictionary<string, Session>(StringComparer.OrdinalIgnoreCase);

        public static string Process(IVisionModule module, VisionSettings settings, string[] parts)
        {
            if (module == null)
                return "fail:no module";

            string action = parts != null && parts.Length > 2
                ? parts[2].Trim().ToUpperInvariant()
                : string.Empty;
            switch (action)
            {
                case "START":
                    return Start(module, settings, parts);
                case "END":
                    return End(module);
                default:
                    return "fail:need START or END";
            }
        }

        private static string Start(IVisionModule module, VisionSettings settings, string[] parts)
        {
            if (!module.Finders.ContainsKey(FinderId))
                return "fail:ColletFinder not found";

            Session current;
            if (Sessions.TryGetValue(module.Name, out current))
                return "fail:COC already running";

            string side = parts != null && parts.Length > 3 ? parts[3] : string.Empty;
            int colletNo;
            if (parts == null || parts.Length <= 4 || !int.TryParse(parts[4], out colletNo) || colletNo < 1 || colletNo > 4)
                return "fail:bad collet number";

            var session = new Session(module, settings, side, colletNo);
            if (!Sessions.TryAdd(module.Name, session))
            {
                session.Dispose();
                return "fail:COC already running";
            }

            session.Start();
            ModuleResultStore.Record(module.Name, "COC", true,
                "회전 중심 측정을 시작했습니다. side=" + side + ";collet=" + colletNo);
            return "STARTED";
        }

        private static string End(IVisionModule module)
        {
            Session session;
            if (!Sessions.TryGetValue(module.Name, out session))
                return "fail:COC not started";

            session.RequestStop();
            if (!session.WaitForStop(5000))
            {
                ModuleResultStore.Record(module.Name, "COC", false, "회전 중심 Grab 종료 대기시간을 초과했습니다.");
                return "fail:COC capture stop timeout";
            }

            Sessions.TryRemove(module.Name, out session);
            try
            {
                double centerX;
                double centerY;
                double radius;
                int samples;
                string error;
                if (!session.TryFit(out centerX, out centerY, out radius, out samples, out error))
                {
                    ModuleResultStore.Record(module.Name, "COC", false, error);
                    return "fail:" + error;
                }

                string result = "centerX=" + centerX.ToString("F6", CultureInfo.InvariantCulture) +
                                ";centerY=" + centerY.ToString("F6", CultureInfo.InvariantCulture) +
                                ";radius=" + radius.ToString("F6", CultureInfo.InvariantCulture) +
                                ";samples=" + samples.ToString(CultureInfo.InvariantCulture);
                ModuleResultStore.Record(module.Name, "COC", true, result);
                return "OK;" + result;
            }
            finally
            {
                session.Dispose();
            }
        }

        private sealed class Session : IDisposable
        {
            private readonly IVisionModule _module;
            private readonly VisionSettings _settings;
            private readonly string _side;
            private readonly int _colletNo;
            private readonly object _sync = new object();
            private readonly List<Sample> _samples = new List<Sample>();
            private readonly CancellationTokenSource _stop = new CancellationTokenSource();
            private Task _captureTask;
            private string _lastError = string.Empty;

            public Session(IVisionModule module, VisionSettings settings, string side, int colletNo)
            {
                _module = module;
                _settings = settings;
                _side = side;
                _colletNo = colletNo;
            }

            public void Start()
            {
                _captureTask = Task.Run((Action)CaptureLoop);
            }

            public void RequestStop()
            {
                _stop.Cancel();
            }

            public bool WaitForStop(int timeoutMs)
            {
                Task task = _captureTask;
                return task == null || task.Wait(Math.Max(1, timeoutMs));
            }

            public bool TryFit(
                out double centerX,
                out double centerY,
                out double radius,
                out int sampleCount,
                out string error)
            {
                Sample[] points;
                lock (_sync)
                {
                    points = _samples.ToArray();
                    error = _lastError;
                }

                centerX = 0.0;
                centerY = 0.0;
                radius = 0.0;
                sampleCount = points.Length;
                if (points.Length < MinimumSampleCount)
                {
                    error = "COC 유효 픽셀 샘플이 부족합니다. samples=" + points.Length +
                            ", required=" + MinimumSampleCount +
                            (string.IsNullOrWhiteSpace(error) ? string.Empty : ", lastError=" + error);
                    return false;
                }

                double meanX = 0.0;
                double meanY = 0.0;
                for (int i = 0; i < points.Length; i++)
                {
                    meanX += points[i].X;
                    meanY += points[i].Y;
                }
                meanX /= points.Length;
                meanY /= points.Length;

                double suu = 0.0;
                double suv = 0.0;
                double svv = 0.0;
                double suz = 0.0;
                double svz = 0.0;
                for (int i = 0; i < points.Length; i++)
                {
                    double u = points[i].X - meanX;
                    double v = points[i].Y - meanY;
                    double z = (u * u) + (v * v);
                    suu += u * u;
                    suv += u * v;
                    svv += v * v;
                    suz += u * z;
                    svz += v * z;
                }

                double determinant = (suu * svv) - (suv * suv);
                if (Math.Abs(determinant) <= 1e-9)
                {
                    error = "COC 픽셀 궤적이 원 중심을 계산하기에 부족합니다. determinant=" +
                            determinant.ToString("G6", CultureInfo.InvariantCulture);
                    return false;
                }

                double centerU = ((suz * svv) - (svz * suv)) / (2.0 * determinant);
                double centerV = ((svz * suu) - (suz * suv)) / (2.0 * determinant);
                centerX = meanX + centerU;
                centerY = meanY + centerV;
                radius = Math.Sqrt((centerU * centerU) + (centerV * centerV) + ((suu + svv) / points.Length));
                if (!IsFinite(centerX) || !IsFinite(centerY) || !IsFinite(radius))
                {
                    error = "COC 원 중심 계산 결과가 유효한 숫자가 아닙니다.";
                    return false;
                }

                error = string.Empty;
                return true;
            }

            public void Dispose()
            {
                _stop.Dispose();
            }

            private void CaptureLoop()
            {
                while (!_stop.IsCancellationRequested)
                {
                    try
                    {
                        string result = VisionCommandCore.Match(_module, _settings, FinderId, string.Empty);
                        double x;
                        double y;
                        if (TryReadCoordinate(result, "x", out x) && TryReadCoordinate(result, "y", out y))
                        {
                            lock (_sync)
                                _samples.Add(new Sample(x, y));
                        }
                        else
                        {
                            lock (_sync)
                                _lastError = result ?? "응답 없음";
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (_sync)
                            _lastError = ex.Message;
                    }

                    if (!_stop.IsCancellationRequested)
                        Thread.Sleep(10);
                }

                ModuleResultStore.Record(_module.Name, "COC", true,
                    "회전 중심 Grab 수집을 종료했습니다. side=" + _side +
                    ";collet=" + _colletNo +
                    ";samples=" + GetSampleCount());
            }

            private int GetSampleCount()
            {
                lock (_sync)
                    return _samples.Count;
            }

            private static bool TryReadCoordinate(string result, string key, out double value)
            {
                value = 0.0;
                if (string.IsNullOrWhiteSpace(result) || !result.StartsWith("OK;", StringComparison.OrdinalIgnoreCase))
                    return false;

                string prefix = key + "=";
                string[] fields = result.Split(';');
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return double.TryParse(
                            fields[i].Substring(prefix.Length),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out value);
                    }
                }
                return false;
            }

            private static bool IsFinite(double value)
            {
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }
        }

        private struct Sample
        {
            public Sample(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X;
            public double Y;
        }
    }
}
