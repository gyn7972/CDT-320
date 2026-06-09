using System;
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
        JogContinuous
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

        public static AxisMotionGuardHandler MotionGuard { get; set; }

        public static IDisposable BeginMotionGuardBypass()
        {
            MotionGuardBypassDepth.Value = MotionGuardBypassDepth.Value + 1;
            return new MotionGuardBypassScope();
        }

        // ─────────────────────────────────────────────
        //  내부 상태 필드
        // ─────────────────────────────────────────────

        /// <summary>백그라운드 상태 업데이트 태스크 취소 토큰 소스.</summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>시뮬레이션용 내부 목표 위치 (CommandPosition의 사본).</summary>
        private double _simTargetPosition;

        /// <summary>Override된 목표 위치. NaN이면 없음.</summary>
        private double _overrideTargetPosition = double.NaN;

        /// <summary>Override된 속도. NaN이면 없음.</summary>
        private double _overrideVelocity = double.NaN;

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
            IsMoving        = false;
            IsInPosition    = false;
            CurrentVelocity = 0.0;
            _currentMode    = MotionMode.None;
            _jogDirection   = 0;
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
            ActualPosition  = newPosition;
            CommandPosition = newPosition;
            _simTargetPosition = newPosition;
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
                ActualPosition = actualPosition;
                CommandPosition = commandPosition;
                _simTargetPosition = commandPosition;
                CurrentVelocity = 0.0;
                IsMoving = false;
                IsInPosition = isInPosition;
                IsServoOn = isServoOn;
                IsHomeDone = isHomeDone;
                IsAlarm = isAlarm;
                AlarmCode = alarmCode;
                Sensor_ORG = isHomeDone;
                _currentMode = MotionMode.None;
                _jogDirection = 0;
                RaisePositionChanged();
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
        /// <param name="velocity">이동 속도 (0 이하이면 Recipe.DefaultVelocity 사용)</param>
        /// <returns>0 = 성공, 그 외 = 오류 코드</returns>
        public virtual async Task<int> MoveAbsoluteAsync(double targetPos, double velocity = 0)
        {
            try
            {
                if (!IsServoOn || IsAlarm)
                    return FailAxisNotReady("ABS MOVE", targetPos, true);

                if (!VerifyMotionGuard(targetPos, AxisMotionGuardKind.Absolute))
                    return -11;

                ClearMotionFailure();

                double vel = velocity > 0 ? velocity : Config.DefaultVelocity;
                CommandPosition = targetPos;
                _simTargetPosition = targetPos;
                CurrentVelocity = vel;
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
        /// <param name="velocity">이동 속도 (0 이하이면 Recipe.DefaultVelocity 사용)</param>
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
                CurrentVelocity = Config.HomeVelocity;
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
            while (!ct.IsCancellationRequested)
            {
                if (IsAlarm)               
                    break;

                if (!IsMoving && IsInPosition) 
                    break;

                await Task.Delay(10, ct).ContinueWith(_ => { }); // 취소 예외 무시
            }
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
            if (!IsServoOn || IsAlarm)
            {
                FailAxisNotReady("JOG", 0, false);
                return;
            }

            double jogTarget = direction > 0 ? Setup.SoftLimitPlus : Setup.SoftLimitMinus;
            if (!VerifyMotionGuard(jogTarget, AxisMotionGuardKind.JogContinuous))
                return;

            ClearMotionFailure();

            _jogDirection   = direction;
            CurrentVelocity = GetJogVelocity(speedType, customVel);
            IsMoving        = true;
            IsInPosition    = false;
            _currentMode    = MotionMode.Jog;

            // CommandPosition은 소프트 리미트 끝으로 설정 - 시뮬레이터가 매 틱 갱신
            CommandPosition    = jogTarget;
            _simTargetPosition = CommandPosition;
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
                double vel = GetJogVelocity(speedType, customVel);
                return await MoveRelativeAsync(direction * Math.Abs(stepDistance), vel);
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
        }

        /// <summary>
        /// 이동 중 목표 속도를 실시간으로 변경(Override)합니다.
        /// </summary>
        /// <param name="newVelocity">변경할 새 속도</param>
        public virtual void OverrideVelocity(double newVelocity)
        {
            if (newVelocity <= 0) return;
            _overrideVelocity = newVelocity;
            CurrentVelocity   = newVelocity;
        }

        /// <summary>
        /// 이동 중 목표 위치를 실시간으로 변경(Override)합니다.
        /// </summary>
        /// <param name="newTargetPosition">변경할 새 목표 위치</param>
        public virtual void OverridePosition(double newTargetPosition)
        {
            _overrideTargetPosition = newTargetPosition;
            CommandPosition         = newTargetPosition;
            _simTargetPosition      = newTargetPosition;
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
        /// 10ms 단위로 호출되는 시뮬레이션 로직.<br/>
        /// <list type="bullet">
        ///   <item><description>CurrentVelocity 기반으로 매 틱 이동 거리를 갱신합니다.</description></item>
        ///   <item><description>목표 위치 도달 시 이동 완료 처리를 합니다.</description></item>
        ///   <item><description>소프트 리미트 도달 시 알람을 발생시키고 정지합니다.</description></item>
        /// </list>
        /// </summary>
        protected virtual void SimulateMotion()
        {
            if (!IsMoving) return;

            // Override 값 반영
            if (!double.IsNaN(_overrideVelocity))
            {
                CurrentVelocity     = _overrideVelocity;
                _overrideVelocity   = double.NaN;
            }

            if (!double.IsNaN(_overrideTargetPosition))
            {
                _simTargetPosition        = _overrideTargetPosition;
                _overrideTargetPosition   = double.NaN;
            }

            // 10ms 동안 이동 거리 계산 (단위: [단위/s] * 0.01s)
            const double tickSeconds = 0.01;
            double step = CurrentVelocity * tickSeconds;

            double remaining = _simTargetPosition - ActualPosition;

            if (_currentMode == MotionMode.Jog)
            {
                // Jog: 방향에 따라 step씩 이동, 목표 도달 개념 없음
                ActualPosition += _jogDirection * step;
                RaisePositionChanged();
            }
            else
            {
                // 절대/상대 이동: 목표를 향해 step만큼 접근
                if (Math.Abs(remaining) <= step)
                {
                    // 목표 도달
                    ActualPosition  = _simTargetPosition;
                    CommandPosition = _simTargetPosition;
                    IsMoving        = false;
                    IsInPosition    = true;
                    CurrentVelocity = 0.0;
                    _currentMode    = MotionMode.None;
                    RaisePositionChanged();
                    RaiseMoveCompleted();
                    return;
                }

                ActualPosition += Math.Sign(remaining) * step;
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
            IsAlarm         = true;
            AlarmCode       = alarmCode;
            _currentMode    = MotionMode.None;
            _jogDirection   = 0;

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
                try { EventLogger.Write(EventKind.Alarm, "MOTION", "AX-SOFT-LIMIT", Name + " - " + message); } catch { }
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
