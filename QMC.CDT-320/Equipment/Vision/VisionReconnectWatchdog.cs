using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Vision PC 연결 끊김 감시·자동 재연결 워치독.
    /// <para>
    /// 연결이 끊기면 <b>알람 없이</b> 일정 간격(<see cref="RetryIntervalMs"/>)으로 <b>붙을 때까지 계속</b>
    /// 재연결을 시도한다. 비전 미사용(Sim / UseVision=false) 이거나 자동연결(VisionAutoConnect) 이 꺼져 있으면
    /// 시도하지 않고 상태만 감시한다. 재연결에 성공하면 <see cref="Start"/> 에 넘긴 콜백(레시피 재전송 등)을 호출한다.
    /// </para>
    /// <para>
    /// UI 부하 방지: 루프는 백그라운드 Task 에서만 돌고, 재시도가 실패해 상태가 그대로면 UI 이벤트를 일으키지
    /// 않는다(VisionHub.RaiseChanged 가 엣지 트리거 — 상태 실제 변경 시에만 ConnectionChanged 발행).
    /// 재연결 시도 자체도 connect 타임아웃(3초) + 간격으로 저부하이며 UI 스레드를 점유하지 않는다.
    /// </para>
    /// </summary>
    public static class VisionReconnectWatchdog
    {
        /// <summary>연결돼 있을 때 상태 감시 주기(저부하 폴링).</summary>
        private const int PollMs = 1000;

        /// <summary>끊김 시 재연결 시도 간격. UI 부하는 엣지 트리거로 제거되므로 3초로 두되,
        /// 필요하면 5~10초로 올려도 된다(값만 조정).</summary>
        private const int RetryIntervalMs = 3000;

        private static CancellationTokenSource _cts;
        private static Action _onReconnected;

        /// <summary>워치독 시작. <paramref name="onReconnected"/> 는 재연결 성공 직후 호출(예: 레시피 재전송).</summary>
        public static void Start(Action onReconnected)
        {
            Stop();
            _onReconnected = onReconnected;
            _cts = new CancellationTokenSource();
            CancellationToken ct = _cts.Token;
            _ = Task.Run(() => LoopAsync(ct));
        }

        public static void Stop()
        {
            try { _cts?.Cancel(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionReconnect] Stop: " + ex.Message); }
            _cts = null;
        }

        private static async Task LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // 비전 미사용(Sim / UseVision=false) 또는 자동연결 OFF → 시도하지 않고 감시만.
                    if (IsVisionLinkBypassed() || !AppSettingsStore.Current.VisionAutoConnect)
                    {
                        await Task.Delay(PollMs, ct).ConfigureAwait(false);
                        continue;
                    }

                    // 이미 연결돼 있으면 저부하 감시만(재연결 시도 없음).
                    if (VisionHub.AnyConnected)
                    {
                        await Task.Delay(PollMs, ct).ConfigureAwait(false);
                        continue;
                    }

                    // 끊김 → 알람 없이 조용히 재연결 시도. 성공하면 레시피 재전송.
                    if (await TryReconnectOnceAsync(ct).ConfigureAwait(false))
                    {
                        SafeOnReconnected();
                        await Task.Delay(PollMs, ct).ConfigureAwait(false);
                        continue;
                    }

                    // 실패 → 과부하 방지 간격 뒤 재시도(붙을 때까지 무한 반복, 알람 없음).
                    await Task.Delay(RetryIntervalMs, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    try { EventLogger.Write(EventKind.Event, "SYS", "VISION-RECONNECT", "watchdog error: " + ex.Message); } catch { }
                    try { await Task.Delay(RetryIntervalMs, ct).ConfigureAwait(false); } catch { break; }
                }
            }
        }

        /// <summary>한 번 재연결 시도. 연결 실패는 정상 상황(호스트 다운)이므로 로그 스팸을 피하려 조용히 삼킨다.</summary>
        private static async Task<bool> TryReconnectOnceAsync(CancellationToken ct)
        {
            AppSettings cfg = AppSettingsStore.Current;
            if (ct.IsCancellationRequested || IsVisionLinkBypassed()) return false;
            try
            {
                await VisionHub.ConnectAllAsync(cfg.VisionHost,
                    cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                    cfg.VisionMainPort, cfg.VisionFrontSidePort, cfg.VisionRearSidePort).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 재연결 실패는 예상된 상황 — 매 시도 로그 기록 시 I/O 부하가 커지므로 Debug 채널로만.
                System.Diagnostics.Debug.WriteLine("[VisionReconnect] attempt failed: " + ex.Message);
            }
            return VisionHub.AnyConnected;
        }

        private static void SafeOnReconnected()
        {
            try { _onReconnected?.Invoke(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionReconnect] onReconnected: " + ex.Message); }
        }

        private static bool IsVisionLinkBypassed()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && !settings.UseVision;
        }
    }
}
