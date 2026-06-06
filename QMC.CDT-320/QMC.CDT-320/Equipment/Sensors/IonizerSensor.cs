using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.IO;

namespace QMC.CDT320.Sensors
{
    /// <summary>
    /// 정전기 제거 이오나이저 알람 입력 감시 (310 의 IonizerAlarmDetectSensor 단순화).
    /// 지정된 DigitalInput 이 OFF (또는 ON, polarity 따라) 가 되면 AlarmManager 에 경고 발생.
    /// </summary>
    public class IonizerSensor : IDisposable
    {
        public string Name             { get; }
        public BaseDigitalInput Input  { get; }
        /// <summary>true=알람 신호가 HIGH 일 때 트리거, false=LOW 일 때.</summary>
        public bool   ActiveHigh       { get; set; } = false;
        public int    PollIntervalMs   { get; set; } = 100;

        private CancellationTokenSource _cts;
        private bool _lastTriggered;

        public IonizerSensor(string name, BaseDigitalInput input)
        {
            Name  = name ?? "IONIZER";
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public void Start()
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => Loop(_cts.Token));
        }

        public void Stop() { _cts?.Cancel(); _cts = null; }

        private async Task Loop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    bool state = Input.IsOn;
                    bool triggered = ActiveHigh ? state : !state;
                    if (triggered && !_lastTriggered)
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "IONIZER",
                            Name, "Ionizer alarm signal active");
                    }
                    // 클리어 시점은 알람 누적을 막기 위해 별도 송출 안함 (AlarmManager 자체에서 dedupe)
                    _lastTriggered = triggered;
                }
                catch { }
                try { await Task.Delay(PollIntervalMs, ct); } catch { break; }
            }
        }

        public void Dispose() => Stop();
    }
}
