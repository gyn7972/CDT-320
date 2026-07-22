using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.Common.Motion
{
    public enum AxisMotionGuardKind
    {
        Absolute,
        Home,
        JogContinuous,
        JogStep
    }

    public delegate bool AxisMotionGuardHandler(
        BaseAxis axis,
        double targetPosition,
        AxisMotionGuardKind moveKind,
        out string reason);

    /// <summary>
    /// 모든 축(Axis) 구현체의 공통 추상 베이스 클래스.<br/>
    /// <list type="bullet">
    ///   <item><description>시뮬레이션 모드 지원 - 실제 하드웨어 없이도 UI/로직 검증 가능.</description></item>
    ///   <item><description>상태·이동 메서드는 <c>virtual</c> - 실제 구현 클래스에서 override하여 API 확장.</description></item>
    ///   <item><description><see cref="BaseComponent{TSetup,TConfig,TRecipe}"/> 기반 - 장비 트리의 Leaf 노드 역할.</description></item>
    /// </list>
    /// </summary>
    public abstract class BaseAxis
        : BaseComponent<AxisSetup, AxisConfig, AxisRecipe>
    {
        private static readonly AsyncLocal<int> MotionGuardBypassDepth = new AsyncLocal<int>();
        private static readonly AsyncLocal<int> ForceMoveDepth = new AsyncLocal<int>();

        public static AxisMotionGuardHandler MotionGuard { get; set; }

        public static bool IsForceMoveActive
        {
            get { return ForceMoveDepth.Value > 0; }
        }

        public static IDisposable BeginMotionGuardBypass()
        {
            MotionGuardBypassDepth.Value = MotionGuardBypassDepth.Value + 1;
            return new MotionGuardBypassScope();
        }

        public static IDisposable BeginForceMoveScope()
        {
            ForceMoveDepth.Value = ForceMoveDepth.Value + 1;
            return new ForceMoveScope();
        }

        // ─────────────────────────────────────────────
        //  내부 상태 필드
        // ─────────────────────────────────────────────

        /// <summary>백그라운드 상태 업데이트 태스크 취소 토큰 소스.</summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>시뮬레이션 프로파일 재구성과 틱 갱신을 직렬화하는 동기화 객체.</summary>
        private readonly object _simulationSync = new object();

        /// <summary>시뮬레이션용 내부 목표 위치 (CommandPosition의 사본).</summary>
        private double _simTargetPosition;

        /// <summary>Override된 목표 위치. NaN이면 없음.</summary>
        private double _overrideTargetPosition = double.NaN;

        /// <summary>Override된 속도. NaN이면 없음.</summary>
        private double _overrideVelocity = double.NaN;

        /// <summary>시뮬레이션 모션 프로파일의 목표 최고속도.</summary>
        private double _simCommandVelocity;

        /// <summary>시뮬레이션 모션 프로파일의 가속도.</summary>
        private double _simAcceleration;

        /// <summary>시뮬레이션 모션 프로파일의 감속도.</summary>
        private double _simDeceleration;

        /// <summary>시뮬레이션 모션 시작 위치.</summary>
        private double _simMotionStartPosition;

        /// <summary>시뮬레이션 모션 이동 방향.</summary>
        private double _simMotionDirection;

        /// <summary>시뮬레이션 모션 총 이동 거리.</summary>
        private double _simMotionDistance;

        /// <summary>시뮬레이션 모션 시작 시각.</summary>
        private long _simMotionStartTimestamp;

        /// <summary>시뮬레이션 모션의 실제 최고 도달 속도.</summary>
        private double _simMotionPeakVelocity;

        /// <summary>시뮬레이션 모션 시작 시점의 속도 크기.</summary>
        private double _simMotionInitialVelocity;

        /// <summary>현재 시뮬레이션 모션의 축 좌표계 기준 부호 있는 속도.</summary>
        private double _simMotionSignedVelocity;

        /// <summary>시뮬레이션 모션 첫 구간의 가속도. 감속 구간이면 음수입니다.</summary>
        private double _simMotionFirstPhaseAcceleration;

        /// <summary>시뮬레이션 모션 등속 구간 속도.</summary>
        private double _simMotionCruiseVelocity;

        /// <summary>시뮬레이션 모션 가속 시간.</summary>
        private double _simMotionAccelerationTime;

        /// <summary>시뮬레이션 모션 등속 시간.</summary>
        private double _simMotionCruiseTime;

        /// <summary>시뮬레이션 모션 감속 시간.</summary>
        private double _simMotionDecelerationTime;

        /// <summary>시뮬레이션 모션 가속 구간 거리.</summary>
        private double _simMotionAccelerationDistance;

        /// <summary>시뮬레이션 모션 등속 구간 거리.</summary>
        private double _simMotionCruiseDistance;

        /// <summary>시뮬레이션 모션 총 소요 시간.</summary>
        private double _simMotionTotalTime;

        /// <summary>감속 완료 후 최종 목표 프로파일을 이어서 생성해야 하는지 여부.</summary>
        private bool _simMotionContinuationPending;

        // ─────────────────────────────────────────────
        //  상태 프로퍼티 (protected set - 파생 클래스에서만 변경 가능)
        // ─────────────────────────────────────────────

        /// <summary>현재(실측) 축의 실제 물리 위치.</summary>
        public double ActualPosition    { get; protected set; }

        /// <summary>이동 명령으로 지정된 목표 위치.</summary>
        public double CommandPosition   { get; protected set; }

        /// <summary>현재 이동 속도.</summary>
        public double CurrentVelocity   { get; protected set; }

        /// <summary>서보 ON 상태 여부.</summary>
        public bool IsServoOn           { get; protected set; }

        /// <summary>이동 중 여부.</summary>
        public bool IsMoving            { get; protected set; }

        /// <summary>In-Position(INP) 신호 - 목표 위치 도달 후 안정화 완료.</summary>
        public bool IsInPosition        { get; protected set; }

        /// <summary>알람 발생 여부.</summary>
        public bool IsAlarm             { get; protected set; }

        /// <summary>원점 복귀 완료 여부.</summary>
        public bool IsHomeDone          { get; protected set; }

        /// <summary>알람 코드.</summary>
        public uint AlarmCode           { get; protected set; }

        public int LastMotionFailureCode { get; protected set; }
        public string LastMotionFailureMessage { get; protected set; }
        public DateTime LastMotionFailureTime { get; protected set; }

        /// <summary>양(+) 방향 하드웨어 리미트 센서 신호.</summary>
        public bool Sensor_PEL          { get; protected set; }

        /// <summary>음(-) 방향 하드웨어 리미트 센서 신호.</summary>
        public bool Sensor_MEL          { get; protected set; }

        /// <summary>원점 센서 신호.</summary>
        public bool Sensor_ORG          { get; protected set; }

        /// <summary>현재 모션 모드.</summary>
        protected MotionMode _currentMode;

        /// <summary>Jog 이동 방향 (+1 또는 -1, 0이면 정지).</summary>
        protected int _jogDirection;

        protected virtual bool UseInternalStatusUpdate
        {
            get { return true; }
        }

        // ─────────────────────────────────────────────
        //  상태 변경 이벤트 (외부 관찰자: UI / Simulator Bridge 등)
        // ─────────────────────────────────────────────

        /// <summary>ActualPosition이 변경될 때마다 발생.</summary>
        public event System.Action<BaseAxis, double> ActualPositionChanged;

        /// <summary>이동 시작 시점에 1회 발생 (IsMoving false→true).</summary>
        public event System.Action<BaseAxis> MoveStarted;

        /// <summary>이동 완료 시점에 1회 발생 (IsMoving true→false, 정상 완료).</summary>
        public event System.Action<BaseAxis> MoveCompleted;

        /// <summary>_lastBroadcastPosition: ActualPositionChanged 이벤트 중복 방지용 캐시.</summary>
        private double _lastBroadcastPosition = double.NaN;

        // ─────────────────────────────────────────────
        //  생성자
        // ─────────────────────────────────────────────

        /// <summary>
        /// <see cref="BaseAxis"/>를 초기화하고 백그라운드 상태 업데이트 태스크를 시작합니다.
        /// </summary>
        /// <param name="name">축 이름 (예: "Z_Axis_Motor")</param>
        protected BaseAxis(string name) : base(name)
        {
            if (UseInternalStatusUpdate)
                StartStatusUpdateTask();
        }

        /// <summary>
        /// Axis setup/config is persisted only through MotionAxisStore.
        /// Machine/Unit settings must not create another EquipmentData axis config source.
        /// </summary>
        public override bool SaveSettings()
        {
            return true;
        }

        /// <summary>
        /// Axis setup/config is loaded by the motion axis registry/store, not UnitDataStore.
        /// </summary>
        public override void LoadSettings()
        {
        }

        // ─────────────────────────────────────────────
        //  §1. 기본 제어 메서드
        // ─────────────────────────────────────────────

        /// <summary>서보를 활성화(ON)합니다.</summary>
        public virtual void ServoOn()
        {
            if (IsAlarm) return;
            IsServoOn = true;
        }

        /// <summary>서보를 비활성화(OFF)합니다.</summary>
        public virtual void ServoOff()
        {
            Stop();
            IsServoOn = false; 
        }

        /// <summary>
        /// 현재 모션을 정지합니다.
        /// 모션 모드와 현재 이동을 초기화하고 IsMoving을 false로 설정합니다.
        /// </summary>
        public virtual void Stop()
        {
            lock (_simulationSync)
            {
                IsMoving        = false;
                IsInPosition    = false;
                CurrentVelocity = 0.0;
                _simCommandVelocity = 0.0;
                _simMotionSignedVelocity = 0.0;
                _simMotionContinuationPending = false;
                _currentMode    = MotionMode.None;
                _jogDirection   = 0;
                ClearSimulationOverrides();
                ResetSimulationClock();
            }
        }

        /// <summary>
        /// 비상 정지(Emergency Stop)를 실행합니다.
        /// 즉시 정지하며 AlarmCode 1을 설정합니다.
        /// </summary>
        public virtual void EStop()
        {
            Stop();
            IsAlarm   = true;
            AlarmCode = 1;
        }

        /// <summary>알람을 해제합니다. 하드웨어 알람 원인이 제거된 뒤 호출해야 합니다.</summary>
        public virtual void ResetAlarm()
        {
            IsAlarm   = false;
            AlarmCode = 0;
        }

        /// <summary>
        /// 현재 위치(ActualPosition, CommandPosition)를 지정된 값으로 강제 설정합니다.
        /// 원점 보정 또는 좌표계 재설정 시 사용됩니다.
        /// </summary>
        /// <param name="newPosition">새로 설정할 위치 값</param>
        public virtual void SetPosition(double newPosition)
        {
            lock (_simulationSync)
            {
                ActualPosition  = newPosition;
                CommandPosition = newPosition;
                _simTargetPosition = newPosition;
                CurrentVelocity = 0.0;
                _simCommandVelocity = 0.0;
                _simMotionSignedVelocity = 0.0;
                _simMotionContinuationPending = false;
                ClearSimulationOverrides();
                ResetSimulationClock();
            }
        }

        public virtual void RestoreRuntimeState(
            double actualPosition,
            double commandPosition,
            bool isServoOn,
            bool isHomeDone,
            bool isInPosition,
            bool isAlarm,
            uint alarmCode)
        {
            try
            {
                lock (_simulationSync)
                {
                    ActualPosition = actualPosition;
                    CommandPosition = commandPosition;
                    _simTargetPosition = commandPosition;
                    CurrentVelocity = 0.0;
                    _simCommandVelocity = 0.0;
                    _simMotionSignedVelocity = 0.0;
                    _simMotionContinuationPending = false;
                    IsMoving = false;
                    IsInPosition = isInPosition;
                    IsServoOn = isServoOn;
                    IsHomeDone = isHomeDone;
                    IsAlarm = isAlarm;
                    AlarmCode = alarmCode;
                    Sensor_ORG = isHomeDone;
                    _currentMode = MotionMode.None;
                    _jogDirection = 0;
                    ClearSimulationOverrides();
                    ResetSimulationClock();
                    RaisePositionChanged();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// 현재 Config 속도/가감속 프로파일을 일괄 설정합니다.
        /// </summary>
        /// <param name="velocity">이동 속도 [단위/s]</param>
        /// <param name="acc">가속도 [단위/s²]</param>
        /// <param name="dec">감속도 [단위/s²]</param>
        public virtual void SetMotionProfile(double velocity, double acc, double dec)
        {
            Config.DefaultVelocity = velocity;
            Config.Acceleration    = acc;
            Config.Deceleration    = dec;
        }

        public void ClearMotionFailure()
        {
            LastMotionFailureCode = 0;
            LastMotionFailureMessage = string.Empty;
            LastMotionFailureTime = DateTime.MinValue;
        }

        // ─────────────────────────────────────────────
        //  이동 완료/스킵 판정 헬퍼 (AxisMoveWaiter 대체 — 단일 원칙: "이동 함수가 완료를 보장한다")
        // ─────────────────────────────────────────────

        /// <summary>
        /// 이동 명령 생략(스킵) 가능 판정 — 정지 상태이고 Actual/Command 모두 목표의 톨러런스 이내면 생략(R2).
        /// </summary>
        protected bool CanSkipMoveToTarget(double target, double tolerance)
        {
            return !IsMoving && !IsAlarm && IsServoOn &&
                   Math.Abs(ActualPosition - target) <= tolerance &&
                   Math.Abs(CommandPosition - target) <= tolerance;
        }

        /// <summary>정지 상태에서 목표 도달(Actual/Command 톨러런스 이내) 여부 — 스냅샷 판정용 공개 헬퍼.</summary>
        public bool IsAtTargetPosition(double target, double tolerance)
        {
            if (tolerance <= 0.0)
                tolerance = Config != null && Config.InPositionTolerance > 0.0 ? Config.InPositionTolerance : 0.01;
            return !IsMoving && !IsAlarm && IsServoOn &&
                   Math.Abs(ActualPosition - target) <= tolerance &&
                   Math.Abs(CommandPosition - target) <= tolerance;
        }

        /// <summary>
        /// 다른 곳에서 발행된 이동(비동기/명령 전용)에 합류해 완료까지 대기한다 — AxisMoveWaiter 대체(R3).
        /// 10ms 폴링(UpdateStatus로 상태 갱신 — 시뮬 프로파일도 이 호출로 전진), 알람/취소/타임아웃 처리 후
        /// Command↔Target 톨러런스 확인만 수행. 0=완료, 음수=실패(사유는 LastMotionFailureMessage).
        /// 실장비 축은 오버라이드해 보드 상태를 직접 조회할 수 있다(AjinAxis: GetInMotion/GetCommandPosition).
        /// </summary>
        public virtual async Task<int> WaitMoveCompleteAsync(double target, int timeoutMs, CancellationToken ct)
        {
            try
            {
                if (timeoutMs <= 0)
                    timeoutMs = 60000;
                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.01;
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    UpdateStatus();

                    if (IsAlarm)
                        return FailMotion((int)AlarmCode != 0 ? (int)AlarmCode : -1, "MOVE JOIN",
                            "이동 합류 대기 중 축 알람. alarmCode=0x" + AlarmCode.ToString("X4"), target, true);

                    if (!IsMoving)
                        break;

                    if (DateTime.UtcNow >= deadline)
                        return FailMotion(-3, "MOVE JOIN",
                            "이동 합류 대기 timeout. timeoutMs=" + timeoutMs, target, true);

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                if (!IsServoOn)
                    return FailMotion(-2, "MOVE JOIN", "이동 합류 대기 후 서보가 OFF 상태입니다.", target, true);

                if (Math.Abs(CommandPosition - target) > tolerance)
                    return FailMotion(-5, "MOVE JOIN",
                        "이동 합류 완료 후 Command 위치가 목표와 다릅니다. command=" + CommandPosition.ToString("0.######") +
                        ", target=" + target.ToString("0.######") +
                        ", tolerance=" + tolerance.ToString("0.######"), target, true);

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FailMotion(-1, "MOVE JOIN", "이동 합류 대기 중 예외. " + ex.Message, target, true);
            }
            finally
            {
            }
        }

        protected void RecordMotionFailure(int code, string action, string reason)
        {
            RecordMotionFailure(code, action, reason, 0, false);
        }

        protected void RecordMotionFailure(int code, string action, string reason, double targetPosition, bool hasTarget)
        {
            LastMotionFailureCode = code;
            LastMotionFailureTime = DateTime.Now;
            LastMotionFailureMessage = BuildMotionFailureMessage(code, action, reason, targetPosition, hasTarget);
        }

        protected int FailMotion(int code, string action, string reason)
        {
            RecordMotionFailure(code, action, reason);
            return code;
        }

        protected int FailMotion(int code, string action, string reason, double targetPosition, bool hasTarget)
        {
            RecordMotionFailure(code, action, reason, targetPosition, hasTarget);
            return code;
        }

        protected int FailAxisNotReady(string action, double targetPosition, bool hasTarget)
        {
            string reason;
            if (IsAlarm)
                reason = "Axis alarm is ON. AlarmCode=0x" + AlarmCode.ToString("X4");
            else if (!IsServoOn)
                reason = "Servo is OFF.";
            else
                reason = "Axis is not ready.";

            return FailMotion(-2, action, reason, targetPosition, hasTarget);
        }

        protected string BuildMotionFailureMessage(int code, string action, string reason, double targetPosition, bool hasTarget)
        {
            string message = Name + " " + (action ?? "motion") + " failed. result=" + code;
            if (!string.IsNullOrWhiteSpace(reason))
                message += ", reason=" + reason;
            if (hasTarget)
                message += ", target=" + targetPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " " + (Setup != null ? Setup.Unit : "");
            message += ", servo=" + (IsServoOn ? "ON" : "OFF");
            message += ", alarm=" + (IsAlarm ? "ON" : "OFF");
            if (IsAlarm || AlarmCode != 0)
                message += ", alarmCode=0x" + AlarmCode.ToString("X4");
            message += ", pos=" + ActualPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " " + (Setup != null ? Setup.Unit : "");
            return message;
        }

        // ─────────────────────────────────────────────
        //  §2. 이동 메서드
        // ─────────────────────────────────────────────

        /// <summary>
        /// 절대 좌표로 비동기 이동합니다.
        /// 이동 완료(InPosition) 또는 알람 발생 시점까지 대기합니다.
        /// </summary>
        /// <param name="targetPos">목표 절대 위치</param>
        /// <param name="velocity">이동 속도 (0 이하이면 AxisConfig.DefaultVelocity 에 전체 퍼센트 스케일을 적용한 값 사용)</param>
        /// <returns>0 = 성공, 그 외 = 오류 코드</returns>
        public virtual async Task<int> MoveAbsoluteAsync(double targetPos, double velocity = 0)
        {
            try
            {
                if (!IsServoOn || IsAlarm)
                    return FailAxisNotReady("ABS MOVE", targetPos, true);

                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.01;
                if (!IsForceMoveActive &&
                    CanSkipMoveToTarget(targetPos, tolerance))
                {
                    ClearMotionFailure();
                    CommandPosition = targetPos;
                    CurrentVelocity = 0.0;
                    _simCommandVelocity = 0.0;
                    IsMoving = false;
                    IsInPosition = true;
                    _currentMode = MotionMode.None;
                    return 0;
                }

                if (!VerifyMotionGuard(targetPos, AxisMotionGuardKind.Absolute))
                    return -11;

                ClearMotionFailure();

                // 명시 velocity 가 없거나 스케일된 DefaultVelocity 로 전달된 경우에는
                // 실장비와 동일하게 가속/감속도도 같은 전체 퍼센트 스케일을 적용한다.
                bool useDefaultMotionScale = velocity <= 0.0 ||
                    MotionSpeedScale.MatchesDefaultVelocityScale(velocity, Config.DefaultVelocity);
                double vel = ApplySimulationSpeedScale(velocity > 0 ? velocity : MotionSpeedScale.ApplyDefaultVelocityScale(Config.DefaultVelocity));
                double acceleration = useDefaultMotionScale
                    ? MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Acceleration)
                    : Config.Acceleration;
                double deceleration = useDefaultMotionScale
                    ? MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Deceleration)
                    : Config.Deceleration;
                CommandPosition = targetPos;
                _simTargetPosition = targetPos;
                ConfigureSimulationMotionProfile(vel, acceleration, deceleration, true);
                IsMoving = true;
                IsInPosition = false;
                _currentMode = MotionMode.Absolute;

                RaiseMoveStarted();
                await WaitUntilMoveDone(_cts.Token);

                return IsAlarm ? (int)AlarmCode : 0;
            }
            catch (Exception)
            {
                IsAlarm = true;
                if (AlarmCode == 0) AlarmCode = 1;
                return -1;
            }
            finally
            {
                // 상태 갱신은 파생 클래스의 UpdateStatus에 위임
            }
        }

        // To do: [명령 전용 절대이동] FastContiSegmentedPickUp처럼 이동 중 감시/속도 오버라이드가 필요한
        //        경로용. MoveAbsoluteAsync와 달리 명령 발행까지만 수행하고 완료를 기다리지 않는다.
        //        velocity/acceleration/deceleration은 스케일이 끝난 "최종값"으로 전달해야 하며(자동 스케일 없음,
        //        0 이하만 Config 기반 스케일 폴백), 도달 감시는 호출자가 담당한다.
        //        (시뮬 축은 프로파일이 벽시계 시간 기반이라 이후 UpdateStatus 호출 시 경과분을 따라잡는다)
        public virtual Task<int> MoveAbsoluteCommandOnlyAsync(double targetPos, double velocity, double acceleration, double deceleration)
        {
            try
            {
                if (!IsServoOn || IsAlarm)
                    return Task.FromResult(FailAxisNotReady("ABS MOVE CMD", targetPos, true));

                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.01;
                if (!IsForceMoveActive &&
                    CanSkipMoveToTarget(targetPos, tolerance))
                {
                    ClearMotionFailure();
                    CommandPosition = targetPos;
                    CurrentVelocity = 0.0;
                    _simCommandVelocity = 0.0;
                    IsMoving = false;
                    IsInPosition = true;
                    _currentMode = MotionMode.None;
                    return Task.FromResult(0);
                }

                if (!VerifyMotionGuard(targetPos, AxisMotionGuardKind.Absolute))
                    return Task.FromResult(-11);

                ClearMotionFailure();

                // 현재 기준: 전달값이 최종값. 0 이하일 때만 Config 기반 스케일 폴백.
                double vel = ApplySimulationSpeedScale(velocity > 0
                    ? velocity
                    : MotionSpeedScale.ApplyDefaultVelocityScale(Config.DefaultVelocity));
                double acc = acceleration > 0
                    ? acceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Acceleration);
                double dec = deceleration > 0
                    ? deceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Deceleration);

                CommandPosition = targetPos;
                _simTargetPosition = targetPos;
                ConfigureSimulationMotionProfile(vel, acc, dec, true);
                IsMoving = true;
                IsInPosition = false;
                _currentMode = MotionMode.Absolute;

                RaiseMoveStarted();
                // 기존 MoveAbsoluteAsync와의 차이: WaitUntilMoveDone을 호출하지 않고 즉시 리턴한다.
                return Task.FromResult(0);
            }
            catch (Exception)
            {
                IsAlarm = true;
                if (AlarmCode == 0) AlarmCode = 1;
                return Task.FromResult(-1);
            }
            finally
            {
            }
        }

        // ─────────────────────────────────────────────
        //  이벤트 발행 헬퍼
        // ─────────────────────────────────────────────

        protected void RaisePositionChanged()
        {
            // 이벤트 구독자가 없거나 위치가 동일하면 스킵
            var h = ActualPositionChanged;
            if (h == null) return;
            if (!double.IsNaN(_lastBroadcastPosition) && _lastBroadcastPosition == ActualPosition) return;
            _lastBroadcastPosition = ActualPosition;
            try { h(this, ActualPosition); } catch { }
        }

        protected void RaiseMoveStarted()
        {
            var h = MoveStarted;
            if (h == null) 
                return;
            try { h(this); } catch { }
        }

        protected void RaiseMoveCompleted()
        {
            var h = MoveCompleted;
            if (h == null) return;
            try { h(this); } catch { }
        }

        /// <summary>
        /// 현재 위치에서 상대 거리만큼 비동기 이동합니다.
        /// </summary>
        /// <param name="distance">이동 거리 (음수이면 반대 방향 이동)</param>
        /// <param name="velocity">이동 속도 (0 이하이면 AxisConfig.DefaultVelocity 에 전체 퍼센트 스케일을 적용한 값 사용)</param>
        /// <returns>0 = 성공, 그 외 = 오류 코드</returns>
        public virtual async Task<int> MoveRelativeAsync(double distance, double velocity = 0)
        {
            try
            {
                double targetPos = ActualPosition + distance;
                return await MoveAbsoluteAsync(targetPos, velocity);
            }
            catch (Exception)
            {
                IsAlarm = true;
                if (AlarmCode == 0) AlarmCode = 1;
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 원점 복귀 시퀀스를 비동기로 실행합니다.<br/>
        /// 시뮬레이션: 음(-) 방향으로 가상 이동 후 HomeOffset을 적용하여 좌표를 설정합니다.
        /// </summary>
        /// <returns>0 = 성공, 그 외 = 오류 코드</returns>
        public virtual async Task<int> HomeSearchAsync()
        {
            try
            {
                if (!IsServoOn || IsAlarm)
                    return FailAxisNotReady("HOME", Setup.HomeOffset, true);

                if (!VerifyMotionGuard(Setup.HomeOffset, AxisMotionGuardKind.Home))
                    return -11;

                ClearMotionFailure();

                _currentMode = MotionMode.Homing;
                IsHomeDone = false;

                // 간단 시뮬레이션: SoftLimitMinus 근처로 이동하여 원점 센서 도달 가정
                double homeTarget = Setup.SoftLimitMinus + 1.0;
                CommandPosition = homeTarget;
                _simTargetPosition = homeTarget;
                ConfigureSimulationMotionProfile(
                    ApplySimulationSpeedScale(Config.HomeVelocity),
                    ResolveSimulationHomeAcceleration(),
                    ResolveSimulationHomeDeceleration(),
                    true);
                IsMoving = true;
                IsInPosition = false;

                await WaitUntilMoveDone(_cts.Token);

                if (IsAlarm) return (int)AlarmCode;

                // 원점 검출 후 오프셋 적용 (좌표 보정)
                SetPosition(Setup.HomeOffset);
                Sensor_ORG = true;
                IsHomeDone = true;
                _currentMode = MotionMode.None;
                return 0;
            }
            catch (Exception)
            {
                IsAlarm = true;
                if (AlarmCode == 0) AlarmCode = 1;
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>
        /// IsMoving이 false가 되고 IsInPosition이 true가 될 때까지,
        /// 또는 IsAlarm이 발생할 때까지 10ms 주기로 폴링합니다.
        /// </summary>
        /// <param name="ct">취소 토큰</param>
        protected async Task WaitUntilMoveDone(CancellationToken ct)
        {
            bool detectedMotion = false;
            while (!ct.IsCancellationRequested)
            {
                if (Config != null && Config.IsSimulationMode)
                    UpdateStatus();

                if (IsAlarm)               
                    break;

                if (IsMoving)
                    detectedMotion = true;

                if (!IsMoving && IsInPosition) 
                    break;

                if (detectedMotion && !IsMoving)
                    break;

                await Task.Delay(1, ct).ContinueWith(_ => { }); // 취소 예외 무시
            }
        }

        private double ApplySimulationSpeedScale(double velocity)
        {
            if (Config == null || !Config.IsSimulationMode || velocity <= 0.0)
                return velocity;

            double scale = Config.SimulationSpeedScale;
            if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0.0)
                scale = 1.0;

            return velocity * scale;
        }

        private void ConfigureSimulationMotionProfile(
            double commandVelocity,
            double acceleration,
            double deceleration,
            bool resetCurrentVelocity)
        {
            lock (_simulationSync)
            {
                _simCommandVelocity = NormalizePositive(commandVelocity, Config != null ? Config.DefaultVelocity : 1.0);
                _simAcceleration = NormalizePositive(acceleration, Config != null ? Config.Acceleration : 1.0);
                _simDeceleration = NormalizePositive(deceleration, Config != null ? Config.Deceleration : 1.0);

                ClearSimulationOverrides();
                _simMotionContinuationPending = false;
                if (resetCurrentVelocity)
                {
                    CurrentVelocity = 0.0;
                    _simMotionSignedVelocity = 0.0;
                }

                ResetSimulationMotionReference(resetCurrentVelocity ? 0.0 : ResolveSimulationSignedVelocity());
            }
        }

        private double ResolveSimulationHomeAcceleration()
        {
            if (Config == null)
                return 1.0;
            if (Config.HomeFirstAcceleration > 0.0)
                return Config.HomeFirstAcceleration;
            return Config.Acceleration;
        }

        private double ResolveSimulationHomeDeceleration()
        {
            if (Config == null)
                return 1.0;
            if (Config.HomeFirstDeceleration > 0.0)
                return Config.HomeFirstDeceleration;
            return Config.Deceleration;
        }

        private double ResolveSimulationJogAcceleration()
        {
            if (Config == null)
                return 1.0;
            if (Config.JogAcceleration > 0.0)
                return Config.JogAcceleration;
            return Config.Acceleration;
        }

        private double ResolveSimulationJogDeceleration()
        {
            if (Config == null)
                return 1.0;
            if (Config.JogDeceleration > 0.0)
                return Config.JogDeceleration;
            return Config.Deceleration;
        }

        private static double NormalizePositive(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
                value = fallback;
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
                value = 1.0;
            return value;
        }

        private void ResetSimulationClock()
        {
            _simMotionStartTimestamp = Stopwatch.GetTimestamp();
            _simMotionStartPosition = ActualPosition;
        }

        private void ResetSimulationMotionReference(double initialSignedVelocity)
        {
            double currentPosition = ActualPosition;
            double targetDirection = ResolveDirection(_simTargetPosition - currentPosition);
            double velocityDirection = ResolveDirection(initialSignedVelocity);
            double initialVelocity = Math.Abs(initialSignedVelocity);
            double targetDistance = Math.Abs(_simTargetPosition - currentPosition);
            double deceleration = NormalizePositive(_simDeceleration, _simAcceleration);
            double stoppingDistance = initialVelocity * initialVelocity / (2.0 * deceleration);

            _simMotionContinuationPending = false;
            bool movingAwayFromTarget = initialVelocity > 0.0 &&
                (targetDirection == 0.0 || velocityDirection != targetDirection);
            bool cannotStopBeforeTarget = initialVelocity > 0.0 &&
                velocityDirection == targetDirection &&
                targetDistance < stoppingDistance;

            if (movingAwayFromTarget || cannotStopBeforeTarget)
            {
                _simMotionContinuationPending = true;
                ConfigureSimulationMotionReference(
                    currentPosition + velocityDirection * stoppingDistance,
                    velocityDirection,
                    initialVelocity);
                return;
            }

            ConfigureSimulationMotionReference(
                _simTargetPosition,
                targetDirection,
                velocityDirection == targetDirection ? initialVelocity : 0.0);
        }

        private void ConfigureSimulationMotionReference(
            double profileTargetPosition,
            double direction,
            double initialVelocity)
        {
            _simMotionStartTimestamp = Stopwatch.GetTimestamp();
            _simMotionStartPosition = ActualPosition;
            _simMotionDirection = direction;
            _simMotionDistance = Math.Abs(profileTargetPosition - ActualPosition);
            _simMotionSignedVelocity = direction * Math.Max(0.0, initialVelocity);
            BuildSimulationMotionSegments(initialVelocity);
        }

        private void BuildSimulationMotionSegments(double initialVelocity)
        {
            double distance = Math.Max(0.0, _simMotionDistance);
            double velocity = NormalizePositive(_simCommandVelocity, Math.Abs(CurrentVelocity));
            double acceleration = NormalizePositive(_simAcceleration, velocity);
            double deceleration = NormalizePositive(_simDeceleration, acceleration);
            double startVelocity = Math.Max(0.0, initialVelocity);

            _simMotionPeakVelocity = 0.0;
            _simMotionInitialVelocity = startVelocity;
            _simMotionFirstPhaseAcceleration = 0.0;
            _simMotionCruiseVelocity = 0.0;
            _simMotionAccelerationTime = 0.0;
            _simMotionCruiseTime = 0.0;
            _simMotionDecelerationTime = 0.0;
            _simMotionAccelerationDistance = 0.0;
            _simMotionCruiseDistance = 0.0;
            _simMotionTotalTime = 0.0;

            if (distance <= 0.0)
                return;

            if (startVelocity > velocity)
            {
                double reduceVelocityTime = (startVelocity - velocity) / deceleration;
                double reduceVelocityDistance =
                    (startVelocity + velocity) * 0.5 * reduceVelocityTime;
                double commandDecelerationDistance = velocity * velocity / (2.0 * deceleration);

                _simMotionPeakVelocity = startVelocity;
                _simMotionFirstPhaseAcceleration = -deceleration;
                _simMotionCruiseVelocity = velocity;
                _simMotionAccelerationTime = reduceVelocityTime;
                _simMotionAccelerationDistance = reduceVelocityDistance;
                _simMotionCruiseDistance = Math.Max(
                    0.0,
                    distance - reduceVelocityDistance - commandDecelerationDistance);
                _simMotionCruiseTime = _simMotionCruiseDistance / velocity;
                _simMotionDecelerationTime = velocity / deceleration;
                _simMotionTotalTime = _simMotionAccelerationTime +
                    _simMotionCruiseTime +
                    _simMotionDecelerationTime;
                return;
            }

            double fullAccelerationDistance =
                (velocity * velocity - startVelocity * startVelocity) / (2.0 * acceleration);
            double fullDecelerationDistance = velocity * velocity / (2.0 * deceleration);
            if (distance >= fullAccelerationDistance + fullDecelerationDistance)
            {
                _simMotionPeakVelocity = velocity;
                _simMotionFirstPhaseAcceleration = acceleration;
                _simMotionCruiseVelocity = velocity;
                _simMotionAccelerationTime = (velocity - startVelocity) / acceleration;
                _simMotionDecelerationTime = velocity / deceleration;
                _simMotionAccelerationDistance = fullAccelerationDistance;
                _simMotionCruiseDistance = distance - fullAccelerationDistance - fullDecelerationDistance;
                _simMotionCruiseTime = _simMotionCruiseDistance / velocity;
                _simMotionTotalTime = _simMotionAccelerationTime + _simMotionCruiseTime + _simMotionDecelerationTime;
                return;
            }

            double peakVelocitySquared =
                (2.0 * distance * acceleration * deceleration +
                 startVelocity * startVelocity * deceleration) /
                (acceleration + deceleration);
            _simMotionPeakVelocity = Math.Sqrt(Math.Max(startVelocity * startVelocity, peakVelocitySquared));
            _simMotionFirstPhaseAcceleration = acceleration;
            _simMotionCruiseVelocity = _simMotionPeakVelocity;
            _simMotionAccelerationTime = (_simMotionPeakVelocity - startVelocity) / acceleration;
            _simMotionDecelerationTime = _simMotionPeakVelocity / deceleration;
            _simMotionAccelerationDistance =
                (_simMotionPeakVelocity * _simMotionPeakVelocity - startVelocity * startVelocity) /
                (2.0 * acceleration);
            _simMotionCruiseDistance = 0.0;
            _simMotionCruiseTime = 0.0;
            _simMotionTotalTime = _simMotionAccelerationTime + _simMotionDecelerationTime;
        }

        private double ResolveSimulationSignedVelocity()
        {
            if (Math.Abs(_simMotionSignedVelocity) > 0.0)
                return _simMotionSignedVelocity;
            if (Math.Abs(CurrentVelocity) > 0.0 && _simMotionDirection != 0.0)
                return _simMotionDirection * Math.Abs(CurrentVelocity);
            return 0.0;
        }

        private static double ResolveDirection(double value)
        {
            return value > 0.0 ? 1.0 : value < 0.0 ? -1.0 : 0.0;
        }

        private void ClearSimulationOverrides()
        {
            _overrideTargetPosition = double.NaN;
            _overrideVelocity = double.NaN;
        }

        // ─────────────────────────────────────────────
        //  §3. Jog 및 Override 제어
        // ─────────────────────────────────────────────

        /// <summary>
        /// JogSpeedType에 따라 실제 적용할 속도 값을 변환하는 내부 유틸리티.
        /// </summary>
        /// <param name="speedType">속도 종류</param>
        /// <param name="customVel">Custom 모드일 때 사용할 속도 값</param>
        /// <returns>적용할 Jog 속도</returns>
        protected double GetJogVelocity(JogSpeedType speedType, double customVel)
        {
            switch (speedType)
            {
                case JogSpeedType.Coarse:
                    return Config.JogCoarseVelocity;
                case JogSpeedType.Fine:
                    return Config.JogFineVelocity;
                case JogSpeedType.Custom:
                    return customVel > 0 ? customVel : Config.JogFineVelocity;
                default:
                    return Config.JogFineVelocity;
            }
        }

        /// <summary>
        /// Jog 버튼을 누르고 있는 동안 연속적으로 이동합니다.
        /// 시뮬레이션 모드는 _jogDirection과 CurrentVelocity를 기반으로 매 틱 위치를 갱신합니다.
        /// </summary>
        /// <param name="direction">+1 (양(+) 방향) 또는 -1 (음(-) 방향)</param>
        /// <param name="speedType">속도 종류</param>
        /// <param name="customVel">Custom 모드일 때 속도 값 (기본값: 0)</param>
        public virtual void MoveJogContinuous(int direction, JogSpeedType speedType,
                                              double customVel = 0)
        {
            UpdateStatus();
            // 조그 중 반복 입력은 새 명령은 막고, 인터락은 현재 방향 기준으로 재확인한다.
            if (IsMoving)
            {
                VerifyMotionGuard(ResolveJogGuardTarget(direction), AxisMotionGuardKind.JogContinuous);
                return;
            }

            if (!IsServoOn || IsAlarm)
            {
                FailAxisNotReady("JOG", 0, false);
                return;
            }

            double jogTarget = direction > 0 ? Setup.SoftLimitPlus : Setup.SoftLimitMinus;
            double guardTarget = ResolveJogGuardTarget(direction);
            if (!VerifyMotionGuard(guardTarget, AxisMotionGuardKind.JogContinuous))
                return;

            ClearMotionFailure();

            _jogDirection   = direction;
            ConfigureSimulationMotionProfile(
                Math.Abs(GetJogVelocity(speedType, customVel)),
                ResolveSimulationJogAcceleration(),
                ResolveSimulationJogDeceleration(),
                true);
            IsMoving        = true;
            IsInPosition    = false;
            _currentMode    = MotionMode.Jog;

            // CommandPosition은 소프트 리미트 끝으로 설정 - 시뮬레이터가 매 틱 갱신
            CommandPosition    = jogTarget;
            _simTargetPosition = CommandPosition;
        }

        private double ResolveJogGuardTarget(int direction)
        {
            if (Setup == null)
                return ActualPosition;

            double tolerance = Config != null && Config.InPositionTolerance > 0.0
                ? Config.InPositionTolerance
                : 0.01;
            double sign = direction > 0 ? 1.0 : -1.0;
            double target = ActualPosition + (sign * Math.Max(1.0, tolerance * 10.0));

            if (Setup.SoftLimitEnabled)
            {
                if (target > Setup.SoftLimitPlus)
                    target = Setup.SoftLimitPlus;
                if (target < Setup.SoftLimitMinus)
                    target = Setup.SoftLimitMinus;
            }

            return target;
        }

        private bool VerifyMotionGuard(double targetPosition, AxisMotionGuardKind moveKind)
        {
            if (MotionGuardBypassDepth.Value > 0)
                return true;

            AxisMotionGuardHandler guard = MotionGuard;
            if (guard == null)
                return true;

            string reason;
            bool allowed = guard(this, targetPosition, moveKind, out reason);
            if (!allowed)
                RecordMotionFailure(-11, moveKind.ToString().ToUpperInvariant(), reason, targetPosition, true);
            return allowed;
        }

        private sealed class MotionGuardBypassScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                MotionGuardBypassDepth.Value = Math.Max(0, MotionGuardBypassDepth.Value - 1);
            }
        }

        private sealed class ForceMoveScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                ForceMoveDepth.Value = Math.Max(0, ForceMoveDepth.Value - 1);
            }
        }

        /// <summary>
        /// 클릭 1회에 stepDistance만큼 한 번 이동하는 Step Jog를 비동기로 실행합니다.
        /// </summary>
        /// <param name="direction">+1 (양(+) 방향) 또는 -1 (음(-) 방향)</param>
        /// <param name="speedType">속도 종류</param>
        /// <param name="stepDistance">1회 이동 거리 (항상 절댓값으로 처리)</param>
        /// <param name="customVel">Custom 모드일 때 속도 값 (기본값: 0)</param>
        /// <returns>0 = 성공, 그 외 = 오류 코드</returns>
        public virtual async Task<int> MoveJogStepAsync(int direction, JogSpeedType speedType,
                                                        double stepDistance, double customVel = 0)
        {
            try
            {
                UpdateStatus();
                // 이동 중 추가 Step Jog 입력은 새 명령은 막고, 인터락은 현재 방향 기준으로 재확인한다.
                if (IsMoving)
                {
                    VerifyMotionGuard(ResolveJogGuardTarget(direction), AxisMotionGuardKind.JogContinuous);
                    return 0;
                }

                double vel = GetJogVelocity(speedType, customVel);
                double distance = direction * Math.Abs(stepDistance);
                double target = ActualPosition + distance;
                if (!VerifyMotionGuard(target, AxisMotionGuardKind.JogStep))
                    return -1;

                int result;
                using (BeginMotionGuardBypass())
                using (BeginForceMoveScope())
                {
                    result = await MoveRelativeAsync(distance, vel);
                }
                if (result != 0)
                    return result;

                // 기존 조건: MoveRelativeAsync 성공 후 AxisMoveWaiter로 재대기했다.
                // 현재 기준: MoveRelativeAsync가 완료를 보장하므로(리턴 0 = 완료) 재대기를 제거한다(R3).
                return 0;
            }
            catch (Exception)
            {
                IsAlarm = true;
                if (AlarmCode == 0) AlarmCode = 1;
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Jog 이동을 정지합니다. <see cref="Stop"/>과 동일하지만 의미를 명확히 표현합니다.
        /// </summary>
        public virtual void StopJog()
        {
            Stop();
            CommandPosition = ActualPosition;
            _simTargetPosition = ActualPosition;
            if (IsServoOn && !IsAlarm)
                IsInPosition = true;
        }

        /// <summary>
        /// 이동 중 목표 속도를 실시간으로 변경(Override)합니다.
        /// </summary>
        /// <param name="newVelocity">변경할 새 속도</param>
        public virtual void OverrideVelocity(double newVelocity)
        {
            if (newVelocity <= 0) return;
            lock (_simulationSync)
            {
                _overrideVelocity = newVelocity;
                if (Config == null || !Config.IsSimulationMode)
                    CurrentVelocity = newVelocity;
            }
        }

        /// <summary>
        /// 이동 중 목표 위치를 실시간으로 변경(Override)합니다.
        /// </summary>
        /// <param name="newTargetPosition">변경할 새 목표 위치</param>
        public virtual void OverridePosition(double newTargetPosition)
        {
            lock (_simulationSync)
            {
                _overrideTargetPosition = newTargetPosition;
                CommandPosition         = newTargetPosition;
                _simTargetPosition      = newTargetPosition;
            }
        }

        // ─────────────────────────────────────────────
        //  §4. 백그라운드 상태 업데이트
        // ─────────────────────────────────────────────

        /// <summary>
        /// 10ms 주기로 <see cref="UpdateStatus"/>를 호출하는 백그라운드 태스크를 시작합니다.
        /// 태스크는 객체 폐기 시 <see cref="Dispose"/>를 통해 종료됩니다.
        /// </summary>
        private void StartStatusUpdateTask()
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
                        // 상태 업데이트 중 예외는 루프를 중단시키지 않는다.
                        await Task.Delay(10, token).ContinueWith(_ => { });
                    }
                }
            }, token);
        }

        /// <summary>
        /// 10ms 주기로 호출되는 상태 갱신 메서드.<br/>
        /// <list type="bullet">
        ///   <item><description>시뮬레이션 모드(<see cref="AxisConfig.IsSimulationMode"/> = true):
        ///     <see cref="SimulateMotion"/>을 호출하여 가상 위치를 갱신합니다.</description></item>
        ///   <item><description>실제 모드: override하여 실제 API를 폴링해 갱신.</description></item>
        /// </list>
        /// </summary>
        public virtual void UpdateStatus()
        {
            if (Config.IsSimulationMode)
            {
                SimulateMotion();
            }
            // else: 실제 모드 - 파생 클래스에서 override하여 구현
        }

        // ─────────────────────────────────────────────
        //  §5. 시뮬레이션 로직
        // ─────────────────────────────────────────────

        /// <summary>
        /// 실제 경과시간 기준으로 호출되는 시뮬레이션 로직.<br/>
        /// <list type="bullet">
        ///   <item><description>명령 최고속도, 가속도, 감속도와 실제 경과시간으로 이동 거리를 갱신합니다.</description></item>
        ///   <item><description>남은 거리의 제동거리를 기준으로 가속/등속/감속 구간을 자동 계산합니다.</description></item>
        ///   <item><description>목표 위치 도달 시 이동 완료 처리를 합니다.</description></item>
        ///   <item><description>소프트 리미트 도달 시 알람을 발생시키고 정지합니다.</description></item>
        /// </list>
        /// </summary>
        protected virtual void SimulateMotion()
        {
            lock (_simulationSync)
            {
                if (!IsMoving) return;

                bool hasVelocityOverride = !double.IsNaN(_overrideVelocity);
                bool hasPositionOverride = !double.IsNaN(_overrideTargetPosition);
                if (hasVelocityOverride || hasPositionOverride)
                {
                    double initialSignedVelocity = ResolveSimulationSignedVelocity();
                    if (hasVelocityOverride)
                        _simCommandVelocity = NormalizePositive(_overrideVelocity, _simCommandVelocity);
                    if (hasPositionOverride)
                        _simTargetPosition = _overrideTargetPosition;
                    ClearSimulationOverrides();

                    if (_currentMode == MotionMode.Jog)
                    {
                        _simMotionStartTimestamp = Stopwatch.GetTimestamp();
                        _simMotionStartPosition = ActualPosition;
                    }
                    else
                    {
                        ResetSimulationMotionReference(initialSignedVelocity);
                    }
                }

                double elapsedSeconds = GetSimulationProfileElapsedSeconds();
                if (elapsedSeconds < 0.0)
                    return;

                if (_currentMode == MotionMode.Jog)
                {
                    double jogDistance = CalculateSimulationJogDistance(elapsedSeconds);
                    CurrentVelocity = CalculateSimulationJogVelocity(elapsedSeconds);
                    _simMotionSignedVelocity = _jogDirection * CurrentVelocity;
                    ActualPosition = _simMotionStartPosition + _jogDirection * jogDistance;
                    RaisePositionChanged();
                }
                else
                {
                    if (_simMotionTotalTime <= 0.0 || elapsedSeconds >= _simMotionTotalTime)
                    {
                        if (ContinueSimulationMotionAfterDeceleration())
                            return;

                        CompleteSimulationMove();
                        return;
                    }

                    double traveled = CalculateSimulationProfileDistance(elapsedSeconds);
                    if (traveled >= _simMotionDistance - ResolveSimulationInPositionTolerance())
                    {
                        if (_simMotionContinuationPending)
                        {
                            if (traveled >= _simMotionDistance &&
                                ContinueSimulationMotionAfterDeceleration())
                                return;
                        }
                        else
                        {
                            CompleteSimulationMove();
                            return;
                        }
                    }

                    CurrentVelocity = CalculateSimulationProfileVelocity(elapsedSeconds);
                    _simMotionSignedVelocity = _simMotionDirection * CurrentVelocity;
                    ActualPosition = _simMotionStartPosition + _simMotionDirection * traveled;
                    RaisePositionChanged();
                }

                // 소프트 리미트 검사
                if (_currentMode != MotionMode.Homing && Setup.SoftLimitEnabled && ActualPosition >= Setup.SoftLimitPlus)
                {
                    ActualPosition = Setup.SoftLimitPlus;
                    TriggerSoftLimitAlarm(alarmCode: 10);
                    return;
                }

                if (_currentMode != MotionMode.Homing && Setup.SoftLimitEnabled && ActualPosition <= Setup.SoftLimitMinus)
                {
                    ActualPosition = Setup.SoftLimitMinus;
                    TriggerSoftLimitAlarm(alarmCode: 11);
                    return;
                }
            }
        }

        private double GetSimulationProfileElapsedSeconds()
        {
            long now = Stopwatch.GetTimestamp();
            long startedAt = _simMotionStartTimestamp;
            if (startedAt <= 0 || now < startedAt)
                return 0.0;

            double elapsedSeconds = (now - startedAt) / (double)Stopwatch.Frequency;
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds))
                return 0.0;

            return elapsedSeconds;
        }

        private double CalculateSimulationProfileDistance(double elapsedSeconds)
        {
            if (elapsedSeconds <= 0.0 || _simMotionDistance <= 0.0)
                return 0.0;

            if (elapsedSeconds <= _simMotionAccelerationTime)
            {
                return _simMotionInitialVelocity * elapsedSeconds +
                       0.5 * _simMotionFirstPhaseAcceleration * elapsedSeconds * elapsedSeconds;
            }

            double cruiseStartTime = _simMotionAccelerationTime;
            double decelerationStartTime = cruiseStartTime + _simMotionCruiseTime;
            if (elapsedSeconds <= decelerationStartTime)
                return _simMotionAccelerationDistance +
                       _simMotionCruiseVelocity * (elapsedSeconds - cruiseStartTime);

            double decelerationElapsed = elapsedSeconds - decelerationStartTime;
            if (decelerationElapsed <= _simMotionDecelerationTime)
            {
                return _simMotionAccelerationDistance +
                       _simMotionCruiseDistance +
                       _simMotionCruiseVelocity * decelerationElapsed -
                       0.5 * _simDeceleration * decelerationElapsed * decelerationElapsed;
            }

            return _simMotionDistance;
        }

        private double CalculateSimulationProfileVelocity(double elapsedSeconds)
        {
            if (_simMotionDistance <= 0.0)
                return 0.0;
            if (elapsedSeconds <= 0.0)
                return _simMotionInitialVelocity;

            if (elapsedSeconds <= _simMotionAccelerationTime)
            {
                double firstPhaseVelocity = _simMotionInitialVelocity +
                    _simMotionFirstPhaseAcceleration * elapsedSeconds;
                return Math.Max(0.0, firstPhaseVelocity);
            }

            double decelerationStartTime = _simMotionAccelerationTime + _simMotionCruiseTime;
            if (elapsedSeconds <= decelerationStartTime)
                return _simMotionCruiseVelocity;

            double decelerationElapsed = elapsedSeconds - decelerationStartTime;
            return Math.Max(0.0, _simMotionCruiseVelocity - _simDeceleration * decelerationElapsed);
        }

        private double CalculateSimulationJogVelocity(double elapsedSeconds)
        {
            if (elapsedSeconds <= 0.0)
                return 0.0;

            double velocity = NormalizePositive(_simCommandVelocity, Math.Abs(CurrentVelocity));
            double acceleration = NormalizePositive(_simAcceleration, velocity);
            return Math.Min(velocity, acceleration * elapsedSeconds);
        }

        private double CalculateSimulationJogDistance(double elapsedSeconds)
        {
            if (elapsedSeconds <= 0.0)
                return 0.0;

            double velocity = NormalizePositive(_simCommandVelocity, Math.Abs(CurrentVelocity));
            double acceleration = NormalizePositive(_simAcceleration, velocity);
            double accelerationTime = velocity / acceleration;
            if (elapsedSeconds <= accelerationTime)
                return 0.5 * acceleration * elapsedSeconds * elapsedSeconds;

            double accelerationDistance = 0.5 * acceleration * accelerationTime * accelerationTime;
            return accelerationDistance + velocity * (elapsedSeconds - accelerationTime);
        }

        private double ResolveSimulationInPositionTolerance()
        {
            if (Config != null && Config.InPositionTolerance > 0.0)
                return Config.InPositionTolerance;
            return 0.01;
        }

        private bool ContinueSimulationMotionAfterDeceleration()
        {
            if (!_simMotionContinuationPending)
                return false;

            ActualPosition = _simMotionStartPosition +
                _simMotionDirection * _simMotionDistance;
            CurrentVelocity = 0.0;
            _simMotionSignedVelocity = 0.0;
            _simMotionContinuationPending = false;
            RaisePositionChanged();
            ResetSimulationMotionReference(0.0);
            return true;
        }

        private void CompleteSimulationMove()
        {
            ActualPosition  = _simTargetPosition;
            CommandPosition = _simTargetPosition;
            IsMoving        = false;
            IsInPosition    = true;
            CurrentVelocity = 0.0;
            _simCommandVelocity = 0.0;
            _simMotionSignedVelocity = 0.0;
            _simMotionContinuationPending = false;
            _currentMode    = MotionMode.None;
            ClearSimulationOverrides();
            RaisePositionChanged();
            RaiseMoveCompleted();
        }

        /// <summary>
        /// 소프트 리미트 도달 시 알람을 발생시키고 모션을 정지합니다.
        /// </summary>
        /// <param name="alarmCode">설정할 알람 코드 (10: PEL, 11: MEL)</param>
        private void TriggerSoftLimitAlarm(uint alarmCode)
        {
            bool shouldRaiseAlarm = !IsAlarm || AlarmCode != alarmCode;
            string side = alarmCode == 10 ? "positive" : "negative";
            double limit = alarmCode == 10 ? Setup.SoftLimitPlus : Setup.SoftLimitMinus;

            IsMoving        = false;
            IsInPosition    = false;
            CurrentVelocity = 0.0;
            _simCommandVelocity = 0.0;
            _simMotionSignedVelocity = 0.0;
            _simMotionContinuationPending = false;
            IsAlarm         = true;
            AlarmCode       = alarmCode;
            _currentMode    = MotionMode.None;
            _jogDirection   = 0;
            ClearSimulationOverrides();

            if (!shouldRaiseAlarm)
                return;

            string message = "Soft limit reached (" + side + "). Position=" +
                ActualPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", Limit=" +
                limit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

            try
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    alarmCode == 10 ? "AX-SOFT-LIMIT-P" : "AX-SOFT-LIMIT-N",
                    Name,
                    message);
            }
            catch
            {
            }
            finally
            {
                try { EventLogger.Write(EventKind.Alarm, "MOTION", "AX-SOFT-LIMIT", Name, message); } catch { }
            }
        }

        // ─────────────────────────────────────────────
        //  §6. IDisposable - 백그라운드 태스크 정리
        // ─────────────────────────────────────────────

        /// <summary>
        /// 백그라운드 상태 업데이트 태스크를 취소하고 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
