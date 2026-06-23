using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Vision PC 연결 끊김 감시·자동 재연결 워치독.
    /// <para>
    /// 한 번이라도 연결된 뒤(연결 의도) 끊기면 <b>3회 재연결</b>을 시도하고, 모두 실패하면 알람을 올린다.
    /// 사용자가 알람을 해제하면 다시 <b>3회 시도</b>하고 실패 시 알람 — 연결될 때까지 이 사이클을 반복한다.
    /// 재연결에 성공하면 <see cref="Start"/>에 넘긴 콜백(레시피 재전송 등)을 호출한다.
    /// 자동연결(<c>VisionAutoConnect</c>)이 꺼져 있으면 재연결을 시도하지 않는다.
    /// </para>
    /// </summary>
    public static class VisionReconnectWatchdog
    {
        private const int    MaxTriesPerCycle = 3;
        private const int    RetryDelayMs     = 2000;
        private const string AlarmCode        = "VISION-RECONNECT";
        private const string Source           = "VisionReconnect";

        private static CancellationTokenSource _cts;
        private static volatile bool _alarmActive;
        private static AlarmRecord _alarm;   // 마지막으로 올린 재연결 알람(해제용)
        private static Action _onReconnected;

        /// <summary>워치독 시작. <paramref name="onReconnected"/>는 재연결 성공 직후 호출(예: 레시피 재전송).</summary>
        public static void Start(Action onReconnected)
        {
            Stop();
            _onReconnected = onReconnected;
            _cts = new CancellationTokenSource();
            AlarmManager.AlarmCleared += OnAlarmCleared;
            var ct = _cts.Token;
            _ = Task.Run(() => LoopAsync(ct));
        }

        public static void Stop()
        {
            try { AlarmManager.AlarmCleared -= OnAlarmCleared; } catch { }
            try { _cts?.Cancel(); } catch { }
            _cts = null;
        }

        private static void OnAlarmCleared(AlarmRecord rec)
        {
            if (rec != null && string.Equals(rec.Code, AlarmCode, StringComparison.OrdinalIgnoreCase))
                _alarmActive = false;
        }

        private static async Task LoopAsync(CancellationToken ct)
        {
            bool wasConnected = false;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (VisionHub.AnyConnected)
                    {
                        wasConnected = true;
                        ClearMyAlarm();
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                        continue;
                    }

                    // 이전에 연결된 적이 없거나(연결 의도 없음) 자동연결 OFF 면 감시만.
                    if (!wasConnected || !AppSettingsStore.Current.VisionAutoConnect)
                    {
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                        continue;
                    }

                    // 끊김 감지 → 3회 재연결
                    if (await TryReconnectCycleAsync(ct).ConfigureAwait(false))
                    {
                        wasConnected = true;
                        ClearMyAlarm();
                        SafeOnReconnected();   // 레시피 재전송 등
                        continue;
                    }

                    // 3회 실패 → 알람 올리고, 해제(또는 연결 회복)될 때까지 대기
                    RaiseAlarm();
                    while (!ct.IsCancellationRequested && _alarmActive && !VisionHub.AnyConnected)
                        await Task.Delay(500, ct).ConfigureAwait(false);
                    // 해제되면 루프 상단으로 → 다시 3회 시도
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    try { EventLogger.Write(EventKind.Alarm, "SYS", AlarmCode, "watchdog error: " + ex.Message); } catch { }
                    try { await Task.Delay(2000, ct).ConfigureAwait(false); } catch { break; }
                }
            }
        }

        private static async Task<bool> TryReconnectCycleAsync(CancellationToken ct)
        {
            var cfg = AppSettingsStore.Current;
            for (int i = 1; i <= MaxTriesPerCycle; i++)
            {
                if (ct.IsCancellationRequested) return false;
                try { EventLogger.Write(EventKind.Event, "SYS", AlarmCode, $"reconnect try {i}/{MaxTriesPerCycle} -> {cfg.VisionHost}"); } catch { }
                try
                {
                    await VisionHub.ConnectAllAsync(cfg.VisionHost,
                        cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                        cfg.VisionMainPort, cfg.VisionTopSidePort, cfg.VisionBottomSidePort).ConfigureAwait(false);
                }
                catch { }
                if (VisionHub.AnyConnected) return true;
                try { await Task.Delay(RetryDelayMs, ct).ConfigureAwait(false); } catch { return false; }
            }
            return false;
        }

        private static void RaiseAlarm()
        {
            _alarmActive = true;
            try
            {
                _alarm = AlarmManager.Raise(AlarmSeverity.Warning, AlarmCode, Source,
                    "Vision PC 연결 끊김 — 재연결 3회 실패. 네트워크/Vision 프로그램 확인 후 알람 해제 시 재시도합니다.");
            }
            catch { }
        }

        private static void ClearMyAlarm()
        {
            try
            {
                if (_alarm != null && _alarm.IsActive)
                    AlarmManager.Clear(_alarm.Id);
            }
            catch { }
            _alarm = null;
            _alarmActive = false;
        }

        private static void SafeOnReconnected()
        {
            try { _onReconnected?.Invoke(); } catch { }
        }
    }
}
