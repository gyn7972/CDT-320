using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.IO;

namespace QMC.CDT320.Sensors
{
    /// <summary>
    /// ?뺤쟾湲??쒓굅 ?댁삤?섏씠? ?뚮엺 ?낅젰 媛먯떆 (310 ??IonizerAlarmDetectSensor ?⑥닚??.
    /// 吏?뺣맂 DigitalInput ??OFF (?먮뒗 ON, polarity ?곕씪) 媛 ?섎㈃ AlarmManager ??寃쎄퀬 諛쒖깮.
    /// </summary>
    public class IonizerSensor : IDisposable
    {
        public string Name             { get; }
        public BaseDigitalInput Input  { get; }
        /// <summary>true=?뚮엺 ?좏샇媛 HIGH ?????몃━嫄? false=LOW ????</summary>
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
                    // ?대━???쒖젏? ?뚮엺 ?꾩쟻??留됯린 ?꾪빐 蹂꾨룄 ?≪텧 ?덊븿 (AlarmManager ?먯껜?먯꽌 dedupe)
                    _lastTriggered = triggered;
                }
                catch { }
                try { await Task.Delay(PollIntervalMs, ct); } catch { break; }
            }
        }

        public void Dispose() => Stop();
    }
}

