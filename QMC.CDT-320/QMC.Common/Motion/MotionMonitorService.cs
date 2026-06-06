using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Motion
{
    public sealed class MotionMonitorService : IDisposable
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, AxisStatusSnapshot> _latest =
            new Dictionary<string, AxisStatusSnapshot>(StringComparer.OrdinalIgnoreCase);

        private List<BaseAxis> _axes = new List<BaseAxis>();
        private CancellationTokenSource _cts;
        private Task _loopTask;
        private int _intervalMs = 50;

        public event Action<AxisStatusSnapshot> AxisStatusUpdated;
        public event Action<IReadOnlyList<AxisStatusSnapshot>> CycleCompleted;

        public bool IsRunning
        {
            get
            {
                lock (_gate)
                {
                    return _cts != null && !_cts.IsCancellationRequested;
                }
            }
        }

        public int IntervalMs
        {
            get { lock (_gate) return _intervalMs; }
        }

        public void Start(IEnumerable<BaseAxis> axes, int intervalMs = 50)
        {
            if (axes == null) throw new ArgumentNullException(nameof(axes));
            if (intervalMs < 10) intervalMs = 10;

            Stop();

            lock (_gate)
            {
                _axes = new List<BaseAxis>(axes);
                _intervalMs = intervalMs;
                _cts = new CancellationTokenSource();
                _loopTask = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
            }
        }

        public void Stop()
        {
            CancellationTokenSource cts = null;
            Task loopTask = null;

            lock (_gate)
            {
                cts = _cts;
                loopTask = _loopTask;
                _cts = null;
                _loopTask = null;
            }

            if (cts == null) return;

            try { cts.Cancel(); } catch { }
            try { loopTask?.Wait(300); } catch { }
            cts.Dispose();
        }

        public AxisStatusSnapshot GetLatest(string axisName)
        {
            if (string.IsNullOrWhiteSpace(axisName)) return null;
            lock (_gate)
            {
                AxisStatusSnapshot snapshot;
                return _latest.TryGetValue(axisName, out snapshot) ? snapshot : null;
            }
        }

        public AxisStatusSnapshot GetLatest(string unitName, string axisName)
        {
            string key = MotionAxisManager.MakeKey(unitName, axisName);
            lock (_gate)
            {
                AxisStatusSnapshot snapshot;
                return _latest.TryGetValue(key, out snapshot) ? snapshot : null;
            }
        }

        public AxisStatusSnapshot GetLatest(BaseAxis axis)
        {
            if (axis == null) return null;
            string unitName = axis.Setup != null ? axis.Setup.UnitName : string.Empty;
            return GetLatest(unitName, axis.Name);
        }

        public AxisStatusSnapshot[] GetAllLatest()
        {
            lock (_gate)
            {
                var values = new AxisStatusSnapshot[_latest.Count];
                _latest.Values.CopyTo(values, 0);
                return values;
            }
        }

        private async Task PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<BaseAxis> axes;
                int delay;
                lock (_gate)
                {
                    axes = new List<BaseAxis>(_axes);
                    delay = _intervalMs;
                }

                var cycle = new List<AxisStatusSnapshot>(axes.Count);
                for (int i = 0; i < axes.Count; i++)
                {
                    if (token.IsCancellationRequested) break;
                    BaseAxis axis = axes[i];
                    if (axis == null) continue;

                    AxisStatusSnapshot snapshot = null;
                    try
                    {
                        axis.UpdateStatus();
                        snapshot = AxisStatusSnapshot.FromAxis(axis);
                    }
                    catch (Exception ex)
                    {
                        snapshot = BuildErrorSnapshot(axis, ex);
                    }

                    lock (_gate)
                    {
                        _latest[MakeKey(snapshot)] = snapshot;
                    }

                    cycle.Add(snapshot);
                    RaiseAxisStatusUpdated(snapshot);
                }

                RaiseCycleCompleted(cycle);

                try { await Task.Delay(delay, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        private static AxisStatusSnapshot BuildErrorSnapshot(BaseAxis axis, Exception ex)
        {
            AxisStatusSnapshot snapshot = AxisStatusSnapshot.FromAxis(axis);
            snapshot.IsAlarm = true;
            snapshot.AlarmCode = 0xFFFF;
            snapshot.DisplayName = (snapshot.DisplayName ?? snapshot.AxisName) + " [" + ex.GetType().Name + "]";
            return snapshot;
        }

        private static string MakeKey(AxisStatusSnapshot snapshot)
        {
            return MotionAxisManager.MakeKey(snapshot.UnitName, snapshot.AxisName);
        }

        private void RaiseAxisStatusUpdated(AxisStatusSnapshot snapshot)
        {
            var handler = AxisStatusUpdated;
            if (handler == null) return;
            try { handler(snapshot); } catch { }
        }

        private void RaiseCycleCompleted(IReadOnlyList<AxisStatusSnapshot> snapshots)
        {
            var handler = CycleCompleted;
            if (handler == null) return;
            try { handler(snapshots); } catch { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
