using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.IO
{
    /// <summary>
    /// 디지털 입력(센서/근접 스위치/리밋 스위치 등)의 추상 베이스 클래스.<br/>
    /// <list type="bullet">
    ///   <item><description>10ms 주기 백그라운드 폴링으로 실제 하드웨어 상태를 읽는다.</description></item>
    ///   <item><description>시뮬레이션 모드에서는 <see cref="SimulateInput"/>으로 외부에서 상태를 강제 주입한다.</description></item>
    ///   <item><description><see cref="WaitUntilStateAsync"/>로 시퀀스 로직에서 센서 대기를 구현한다.</description></item>
    ///   <item><description><see cref="Setup"/>의 <c>IsNormallyClosed</c>로 A/B 접점을 자동 치환한다.</description></item>
    /// </list>
    /// </summary>
    public abstract class BaseDigitalInput
        : BaseComponent<IoSetup, IoConfig, IoRecipe>
    {
        // ──────────────────────────────────────────────────────────────────────
        //  내부 전용 필드
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>백그라운드 폴링 태스크 취소 토큰 소스.</summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        protected virtual bool UseInternalStatusUpdate
        {
            get { return true; }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  상태 프로퍼티
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 논리적 ON 상태.<br/>
        /// <c>IsNormallyClosed</c> 설정에 따라 하드웨어 신호의 A/B 접점 치환이 적용된 값이다.
        /// </summary>
        public bool IsOn  { get; protected set; }

        /// <summary>논리적 OFF 상태 (<see cref="IsOn"/>의 반전값).</summary>
        public bool IsOff => !IsOn;

        // ──────────────────────────────────────────────────────────────────────
        //  생성자
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// <see cref="BaseDigitalInput"/>을 초기화하고 백그라운드 폴링 태스크를 시작한다.
        /// </summary>
        /// <param name="name">I/O 포인트 이름 (예: "Sensor_WorkPresence")</param>
        protected BaseDigitalInput(string name) : base(name)
        {
            if (UseInternalStatusUpdate)
                StartPollingTask();
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §1. 상태 업데이트 (하드웨어 폴링)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 10ms 주기로 호출되는 상태 갱신 메서드.<br/>
        /// <list type="bullet">
        ///   <item><description>시뮬레이션 모드(<c>IsSimulationMode</c> = true):
        ///     <see cref="SimulateInput"/>으로만 상태가 변경되므로 아무것도 하지 않는다.</description></item>
        ///   <item><description>실보드 모드 : 하위 클래스에서 override하여 실제 API로 하드웨어 신호를 읽고
        ///     <see cref="IsOn"/>에 반영한다.</description></item>
        /// </list>
        /// </summary>
        public virtual void UpdateStatus()
        {
            if (Config.IsSimulationMode) return;

            // ── 실보드 모드: 하위 클래스에서 override하여 아래 패턴으로 구현 ──
            //
            // bool rawSignal = BoardApi.GetInput(Setup.ModuleNo, Setup.BitNo);
            //
            // IsNormallyClosed(B접점)이면 하드웨어 신호를 반전하여 논리 상태로 변환한다.
            // IsOn = Setup.IsNormallyClosed ? !rawSignal : rawSignal;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §2. 시뮬레이션 상태 주입
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 시뮬레이션 모드에서 외부(UI/테스트 시퀀스)가 입력 상태를 강제로 주입한다.<br/>
        /// 실보드 모드(<c>IsSimulationMode</c> = false)에서는 호출해도 무시된다.
        /// </summary>
        /// <param name="state">주입할 논리 상태 (true = ON, false = OFF)</param>
        public virtual void SimulateInput(bool state)
        {
            if (!Config.IsSimulationMode) return;
            bool changed = IsOn != state;
            IsOn = state;
            if (changed) RaiseStateChanged(state);
        }

        // ──────────────────────────────────────────────
        //  상태 변경 이벤트 (외부 관찰자용)
        // ──────────────────────────────────────────────

        /// <summary>IsOn 상태가 실제로 변경될 때 발행.</summary>
        public event System.Action<BaseDigitalInput, bool> StateChanged;

        /// <summary>실보드 UpdateStatus() 구현에서 IsOn을 직접 세팅한 후 호출하면 이벤트가 발행된다.</summary>
        protected void RaiseStateChanged(bool state)
        {
            var h = StateChanged;
            if (h == null) return;
            try { h(this, state); } catch { }
        }

        protected internal void ApplyScannedState(bool logical)
        {
            if (IsOn != logical)
            {
                IsOn = logical;
                RaiseStateChanged(logical);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §3. 시퀀스 대기 로직
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// <see cref="IsOn"/>이 <paramref name="targetState"/>와 같아질 때까지 비동기로 대기한다.<br/>
        /// <list type="bullet">
        ///   <item><description>타겟 상태 도달 후 <c>Recipe.SettleTimeMs</c>만큼 추가 대기(채터링 방지).</description></item>
        ///   <item><description><paramref name="timeoutMs"/> 초과 시 에러 메시지 출력 후 <c>false</c> 반환.</description></item>
        /// </list>
        /// </summary>
        /// <param name="targetState">대기할 목표 논리 상태 (true = ON, false = OFF)</param>
        /// <param name="timeoutMs">타임아웃 시간 [ms] (기본값: 3000ms)</param>
        /// <returns>정상 도달 시 <c>true</c>, 타임아웃 시 <c>false</c></returns>
        public virtual async Task<bool> WaitUntilStateAsync(bool targetState,
                                                            int timeoutMs = 3000)
        {
            Stopwatch sw = Stopwatch.StartNew();

            while (IsOn != targetState)
            {
                if (sw.ElapsedMilliseconds >= timeoutMs)
                {
                    string stateStr = targetState ? "ON" : "OFF";
                    Console.WriteLine(
                        $"[TIMEOUT] '{Name}' ? {stateStr} 대기 중 타임아웃 " +
                        $"({timeoutMs}ms 초과)");
                    return false;
                }

                await Task.Delay(10).ContinueWith(_ => { }); // 취소 예외 억제
            }

            // ── 채터링 방지: 목표 상태 도달 후 안정화 대기 ──────────────────
            if (Recipe.SettleTimeMs > 0)
                await Task.Delay(Recipe.SettleTimeMs).ContinueWith(_ => { });

            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §4. 백그라운드 폴링 루프
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 10ms 주기로 <see cref="UpdateStatus"/>를 호출하는 백그라운드 태스크를 시작한다.
        /// </summary>
        private void StartPollingTask()
        {
            CancellationToken token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        UpdateStatus();
                        await Task.Delay(10, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // 폴링 중 예외는 루프를 중단시키지 않는다.
                        await Task.Delay(10, token).ContinueWith(_ => { });
                    }
                }
            }, token);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §5. IDisposable ? 백그라운드 태스크 정리
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 백그라운드 폴링 태스크를 취소하고 리소스를 해제한다.
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
