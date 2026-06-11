using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.IO
{
    /// <summary>
    /// 디지털 출력(솔레노이드/릴레이/표시등 등)의 추상 베이스 클래스.<br/>
    /// <list type="bullet">
    ///   <item><description><see cref="Write"/>를 통해 논리 상태를 명령하면 A/B 접점 치환 후 하드웨어에 출력한다.</description></item>
    ///   <item><description>시뮬레이션 모드에서는 실제 하드웨어 출력 없이 <see cref="IsOn"/>만 갱신한다.</description></item>
    ///   <item><description><see cref="On"/> / <see cref="Off"/> 유틸리티로 가독성 높은 시퀀스 코드를 작성할 수 있다.</description></item>
    /// </list>
    /// </summary>
    public abstract class BaseDigitalOutput
        : BaseComponent<IoSetup, IoConfig, IoRecipe>
    {
        // ──────────────────────────────────────────────────────────────────────
        //  내부 전용 필드
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>백그라운드 상태 읽기 태스크 취소 토큰 소스.</summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        protected virtual bool UseInternalStatusUpdate
        {
            get { return true; }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  상태 프로퍼티
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 현재 논리적 출력 상태.<br/>
        /// <see cref="Write"/>로 명령된 논리 상태를 반영한다
        /// (<c>IsNormallyClosed</c>에 의한 물리 신호 반전과 무관하게 항상 논리값을 나타냄).
        /// </summary>
        public bool IsOn { get; protected set; }

        // ──────────────────────────────────────────────────────────────────────
        //  생성자
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// <see cref="BaseDigitalOutput"/>을 초기화하고 백그라운드 상태 읽기 태스크를 시작한다.
        /// </summary>
        /// <param name="name">I/O 포인트 이름 (예: "Sol_PickupVacuum")</param>
        protected BaseDigitalOutput(string name) : base(name)
        {
            if (UseInternalStatusUpdate)
                StartPollingTask();
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §1. 출력 제어 핵심 메서드
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 지정된 논리 상태를 출력한다.<br/>
        /// <list type="bullet">
        ///   <item><description>시뮬레이션 모드 : 논리 상태를 <see cref="IsOn"/>에 직접 반영한다.</description></item>
        ///   <item><description>실보드 모드    : <c>IsNormallyClosed</c>에 따라 물리 신호를 계산한 뒤
        ///     하드웨어에 출력한다. 하위 클래스에서 override하여 API 연동.</description></item>
        /// </list>
        /// </summary>
        /// <param name="state">명령할 논리 상태 (true = ON, false = OFF)</param>
        public virtual void Write(bool state)
        {
            bool changed = IsOn != state;

            // 논리 상태를 먼저 프로퍼티에 반영 (UI 바인딩 등 즉시 갱신)
            IsOn = state;

            if (Config.IsSimulationMode)
                AjinIoScanService.SetSimulatedState(this, state);

            if (changed) RaiseStateChanged(state);

            if (Config.IsSimulationMode) return;

            // ── 실보드 모드: 하위 클래스에서 override하여 아래 패턴으로 구현 ──
            //
            // B접점(IsNormallyClosed)이면 물리 신호를 반전하여 하드웨어에 출력한다.
            // bool physicalSignal = Setup.IsNormallyClosed ? !state : state;
            // BoardApi.SetOutput(Setup.ModuleNo, Setup.BitNo, physicalSignal);
        }

        // ──────────────────────────────────────────────
        //  상태 변경 이벤트 (외부 관찰자용)
        // ──────────────────────────────────────────────

        /// <summary>IsOn 상태가 실제로 변경될 때 발행.</summary>
        public event System.Action<BaseDigitalOutput, bool> StateChanged;

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
        //  §2. 유틸리티 메서드 (가독성용 래퍼)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>출력을 논리 ON 상태로 설정한다.</summary>
        public void On()
        {
            Write(true);
        }

        /// <summary>출력을 논리 OFF 상태로 설정한다.</summary>
        public void Off()
        {
            Write(false);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §3. 상태 읽기 (실보드 출력 피드백)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 10ms 주기로 호출되는 상태 갱신 메서드.<br/>
        /// <list type="bullet">
        ///   <item><description>시뮬레이션 모드 : <see cref="Write"/>가 <see cref="IsOn"/>을 직접 관리하므로 아무것도 하지 않는다.</description></item>
        ///   <item><description>실보드 모드     : 하위 클래스에서 override하여 하드웨어 출력 피드백을 읽는다.</description></item>
        /// </list>
        /// </summary>
        public virtual void UpdateStatus()
        {
            if (Config.IsSimulationMode)
            {
                bool simulatedState;
                if (AjinIoScanService.TryGetSimulatedState(this, out simulatedState))
                    ApplyScannedState(simulatedState);
                return;
            }

            // ── 실보드 모드: 하위 클래스에서 override하여 아래 패턴으로 구현 ──
            //
            // 출력 피드백(보드가 실제로 내보낸 물리 신호)을 읽어 IsOn을 갱신한다.
            // bool physicalSignal = BoardApi.GetOutput(Setup.ModuleNo, Setup.BitNo);
            // IsOn = Setup.IsNormallyClosed ? !physicalSignal : physicalSignal;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §4. 백그라운드 상태 읽기 루프
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
                        // 상태 읽기 중 예외는 루프를 중단시키지 않는다.
                        await Task.Delay(10, token).ContinueWith(_ => { });
                    }
                }
            }, token);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  §5. IDisposable ? 백그라운드 태스크 정리
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 백그라운드 상태 읽기 태스크를 취소하고 리소스를 해제한다.
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
