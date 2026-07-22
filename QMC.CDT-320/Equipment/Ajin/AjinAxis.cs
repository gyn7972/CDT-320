using QMC.Common.Motion;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion.Ajin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Motion.SharedRailX;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Ajin
{
    public class AjinAxis : BaseAxis
    {
        private static readonly bool ForceTestBoard = false;
        private static readonly bool BlockSetupWriteToBoard = true;
        private const double ForcedTestBoardVelocity = 20.0;

        private readonly object _sync = new object();
        private static int _sharedRailXHomeSearchCount;
        private int _motionDirection;
        private bool _isHomeSearching;
        private int _motionStopSerial;
        private int _hardwareLimitSearchDirection;

        // 소프트리밋은 보드 센서가 아니므로 알람 리셋 전까지 소프트웨어 latch 로 유지한다.
        private bool _softLimitAlarmLatched;
        private uint _softLimitAlarmLatchedCode;
        private bool _limitRecoveryActive;
        private int _limitRecoveryDirection;

        // 저장된 모터 초기화(HomeDone) 신호 latch.
        // 실장비는 프로그램 재실행 시 아진 보드가 HomeDone 을 off 로 보고하지만 실제로는 홈이 유지된다.
        // latch 가 살아있으면 보드의 off 보고를 무시하고 저장된 완료 상태를 유지한다.
        // 서보 알람 또는 서보 OFF 시 latch 를 해제하여 재초기화가 필요하도록 한다.
        // 리밋 알람은 위치 기준이 사라진 것은 아니므로 latch 를 유지한다.
        private bool _homeDoneLatched;

        public int AxisNo { get; }

        public bool IsInitializeHardwareLimitSearchActive(int direction)
        {
            int expectedDirection = direction < 0 ? -1 : 1;
            if (Volatile.Read(ref _hardwareLimitSearchDirection) != expectedDirection)
                return false;

            return IsMoving || IsTargetHardwareLimitActive(expectedDirection);
        }

        // 보드 raw enum 값 캐시. ReadSetupFromBoard 시 채워지고,
        // WriteSetupToBoard 시 모델 → AXL enum 매핑이 동일 카테고리이면 raw 를 그대로 재사용한다.
        // 모델 enum 종류수 < AXL enum 종류수 인 항목들의 정보 손실을 라운드트립에서 방지한다.
        private AXM.MotorOutputMethod? _rawPulseOutput;
        private AXM.EncoderInputMethod? _rawEncoderInput;
        private AXT_MOTION_PROFILE_MODE? _rawProfileMode;

        protected override bool UseInternalStatusUpdate
        {
            get { return false; }
        }

        public async Task<int> SearchHardwareLimitForInitializeAsync(
            int direction,
            double velocity,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            int searchDirection = direction < 0 ? -1 : 1;
            bool completed = false;
            try
            {
                if (timeoutMs <= 0)
                    timeoutMs = 30000;

                if (UseSimulation)
                {
                    if (!IsServoOn || IsAlarm)
                        return FailAjinAxisNotReady("INITIALIZE LIMIT SEARCH", 0.0, false);

                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    if (searchDirection < 0)
                        Sensor_MEL = true;
                    else
                        Sensor_PEL = true;
                    Volatile.Write(ref _hardwareLimitSearchDirection, searchDirection);
                    completed = true;
                    return 0;
                }

                if (!AjinSystem.IsOpen)
                    return FailMotion(-2, "INITIALIZE LIMIT SEARCH", "AXL is not open.", 0.0, false);

                UpdateStatus();
                if (!IsServoOn)
                    return FailMotion(-2, "INITIALIZE LIMIT SEARCH", "Servo is OFF.", 0.0, false);
                if (IsAlarm && !IsExpectedHardwareLimitAlarm(searchDirection))
                    return FailAjinAxisNotReady("INITIALIZE LIMIT SEARCH", 0.0, false);

                ClearExpectedHardwareLimitAlarm(searchDirection);
                Volatile.Write(ref _hardwareLimitSearchDirection, searchDirection);

                if (IsTargetHardwareLimitActive(searchDirection))
                {
                    completed = true;
                    return 0;
                }

                if (IsOppositeHardwareLimitActive(searchDirection))
                    return FailMotion(-12, "INITIALIZE LIMIT SEARCH", "Opposite hardware limit is active.", 0.0, false);

                double safeVelocity = velocity > 0.0
                    ? Math.Abs(velocity)
                    : Math.Abs(Config != null ? Config.JogFineVelocity : 1.0);
                double signedVelocity = searchDirection * Math.Max(0.000001, safeVelocity);
                int motionStopSerial = Volatile.Read(ref _motionStopSerial);

                CurrentVelocity = signedVelocity;
                IsMoving = true;
                IsInPosition = false;
                _motionDirection = searchDirection;

                int ret;
                lock (_sync)
                    ret = AXM.MoveVelocity(
                        AxisNo,
                        ToBoardVelocity(signedVelocity),
                        ToBoardAcceleration(ResolveJogAcceleration()),
                        ToBoardAcceleration(ResolveJogDeceleration()));
                if (ret != 0)
                {
                    IsMoving = false;
                    _motionDirection = 0;
                    return FailMotion(ret, "INITIALIZE LIMIT SEARCH", "AXM.MoveVelocity failed. ret=0x" + ret.ToString("X4"), 0.0, false);
                }

                RaiseMoveStarted();
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (DateTime.UtcNow < deadline)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    UpdateStatus();

                    if (!IsServoOn)
                        return FailMotion(-2, "INITIALIZE LIMIT SEARCH", "Servo turned OFF during limit search.", 0.0, false);
                    if (IsOppositeHardwareLimitActive(searchDirection))
                        return FailMotion(-12, "INITIALIZE LIMIT SEARCH", "Opposite hardware limit was detected.", 0.0, false);
                    if (IsAlarm)
                        return FailMotion((int)AlarmCode, "INITIALIZE LIMIT SEARCH", "Axis fault occurred during limit search.", 0.0, false);
                    if (Volatile.Read(ref _motionStopSerial) != motionStopSerial && !IsMoving)
                        return FailMotion(-4, "INITIALIZE LIMIT SEARCH", "Axis stop was requested during limit search.", 0.0, false);

                    if (IsTargetHardwareLimitActive(searchDirection))
                    {
                        Stop();
                        int stopWait = 0;
                        do
                        {
                            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
                            UpdateStatus();
                        }
                        while (IsMoving && ++stopWait < 100);

                        if (IsMoving)
                            return FailMotion(-13, "INITIALIZE LIMIT SEARCH", "Axis did not stop after target limit detection.", 0.0, false);

                        completed = true;
                        ClearExpectedHardwareLimitAlarm(searchDirection);
                        return 0;
                    }

                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }

                return FailMotion(-3, "INITIALIZE LIMIT SEARCH", "Hardware limit search timeout.", 0.0, false);
            }
            catch (OperationCanceledException)
            {
                return FailMotion(-4, "INITIALIZE LIMIT SEARCH", "Hardware limit search was canceled.", 0.0, false);
            }
            catch (Exception ex)
            {
                return FailMotion(-1, "INITIALIZE LIMIT SEARCH", ex.Message, 0.0, false);
            }
            finally
            {
                if (!completed)
                    StopInitializeHardwareLimitSearch();
            }
        }

        public void StopInitializeHardwareLimitSearch()
        {
            try
            {
                Stop();
            }
            finally
            {
                ReleaseInitializeHardwareLimitSearch();
            }
        }

        public void ReleaseInitializeHardwareLimitSearch()
        {
            Volatile.Write(ref _hardwareLimitSearchDirection, 0);
        }

        private bool IsTargetHardwareLimitActive(int direction)
        {
            return direction < 0 ? Sensor_MEL : Sensor_PEL;
        }

        private bool IsOppositeHardwareLimitActive(int direction)
        {
            return direction < 0 ? Sensor_PEL : Sensor_MEL;
        }

        private bool IsExpectedHardwareLimitAlarm(int direction)
        {
            return IsAlarm && AlarmCode == (direction < 0 ? 21u : 20u);
        }

        private void ClearExpectedHardwareLimitAlarm(int direction)
        {
            if (!IsExpectedHardwareLimitAlarm(direction))
                return;

            IsAlarm = false;
            AlarmCode = 0;
        }

        private bool UseSimulation
        {
            get { return Config != null && Config.IsSimulationMode; }
        }

        private bool IsSharedRailXHomeAxis()
        {
            return string.Equals(Name, "InputVisionX", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Name, "CameraX", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Name, "FrontPickerX", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Name, "RearPickerX", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Name, "OutputVisionX", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Name, "OutVisionX", StringComparison.OrdinalIgnoreCase);
        }

        private bool BeginSharedRailXHomeLimitSuppress()
        {
            if (!IsSharedRailXHomeAxis())
                return false;

            Interlocked.Increment(ref _sharedRailXHomeSearchCount);
            return true;
        }

        private void EndSharedRailXHomeLimitSuppress()
        {
            int value = Interlocked.Decrement(ref _sharedRailXHomeSearchCount);
            if (value < 0)
                Interlocked.Exchange(ref _sharedRailXHomeSearchCount, 0);
        }

        private bool IsSharedRailXHomeLimitSuppressed()
        {
            return IsSharedRailXHomeAxis() &&
                   Volatile.Read(ref _sharedRailXHomeSearchCount) > 0;
        }

        private double AxisHomeTarget()
        {
            return Setup != null ? Setup.HomeOffset : 0.0;
        }

        private int FailAjinAxisNotReady(string action, double targetPosition, bool hasTarget)
        {
            string reason;
            if (!AjinSystem.IsOpen)
                reason = "AXL is not open.";
            else if (IsAlarm)
                reason = "Axis alarm is ON. AlarmCode=0x" + AlarmCode.ToString("X4");
            else if (!IsServoOn)
                reason = "Servo is OFF.";
            else
                reason = "Axis is not ready.";

            return FailMotion(-2, action, reason, targetPosition, hasTarget);
        }

        public AjinAxis(string name, int axisNo) : base(name)
        {
            AxisNo = axisNo;
            Config.IsSimulationMode = false;
        }

        public int TryOverridePosition(
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration)
        {
            try
            {
                if (UseSimulation)
                {
                    if (!IsMoving)
                        return -4;

                    string simulationGuardReason;
                    if (!MotionGuardRuntime.VerifyAxisTeachingMove(
                        this,
                        targetPosition,
                        "PositionOverride",
                        out simulationGuardReason))
                    {
                        return -11;
                    }

                    base.OverridePosition(targetPosition);
                    if (velocity > 0.0)
                        base.OverrideVelocity(velocity);
                    return 0;
                }

                if (!AjinSystem.IsOpen)
                    return FailMotion(-2, "POSITION OVERRIDE", "AXL 라이브러리가 열려 있지 않습니다.", targetPosition, true);
                if (IsAlarm)
                    return FailMotion(-2, "POSITION OVERRIDE", "축 알람이 ON 상태입니다. alarmCode=0x" + AlarmCode.ToString("X4"), targetPosition, true);
                if (!IsServoOn)
                    return FailMotion(-2, "POSITION OVERRIDE", "축 서보가 OFF 상태입니다.", targetPosition, true);

                UpdateStatus();
                if (!IsMoving)
                    return -4;

                string guardReason;
                if (!MotionGuardRuntime.VerifyAxisTeachingMove(
                    this,
                    targetPosition,
                    "PositionOverride",
                    out guardReason))
                {
                    return -11;
                }

                int limitCheck = CheckSoftLimitTarget(targetPosition);
                if (limitCheck != 0)
                    return limitCheck;

                double safeVelocity = velocity > 0.0
                    ? velocity
                    : MotionSpeedScale.ApplyDefaultVelocityScale(Config.DefaultVelocity);
                double safeAcceleration = acceleration > 0.0
                    ? acceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Acceleration);
                double safeDeceleration = deceleration > 0.0
                    ? deceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Deceleration);

                int ret;
                lock (_sync)
                {
                    ret = AXM.ModifyPosition(
                        AxisNo,
                        ToBoardPosition(targetPosition),
                        ToBoardVelocity(safeVelocity),
                        ToBoardAcceleration(safeAcceleration),
                        ToBoardAcceleration(safeDeceleration));
                }

                if (ret != 0)
                {
                    return FailMotion(
                        ret,
                        "POSITION OVERRIDE",
                        "AXM 위치 오버라이드 명령이 실패했습니다. ret=0x" + ret.ToString("X4"),
                        targetPosition,
                        true);
                }

                base.OverridePosition(targetPosition);
                CurrentVelocity = safeVelocity;
                _motionDirection = targetPosition > ActualPosition
                    ? 1
                    : targetPosition < ActualPosition ? -1 : 0;
                return 0;
            }
            catch (Exception ex)
            {
                return FailMotion(
                    -1,
                    "POSITION OVERRIDE",
                    "위치 오버라이드 처리 중 예외가 발생했습니다. error=" + ex.Message,
                    targetPosition,
                    true);
            }
            finally
            {
            }
        }

        /// <summary>
        /// 구동 중인 축의 속도/가감속만 변경한다 (목표 위치 유지).
        /// 정지 상태면 -4, 인자가 0 이하인 성분은 Config 기본값에 MotionSpeedScale을 적용해 대체한다.
        /// </summary>
        public int TryOverrideVelocity(double velocity, double acceleration, double deceleration)
        {
            try
            {
                double safeVelocity = velocity > 0.0
                    ? velocity
                    : MotionSpeedScale.ApplyDefaultVelocityScale(Config.DefaultVelocity);
                double safeAcceleration = acceleration > 0.0
                    ? acceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Acceleration);
                double safeDeceleration = deceleration > 0.0
                    ? deceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Deceleration);

                if (UseSimulation)
                {
                    if (!IsMoving)
                        return -4;

                    base.OverrideVelocity(safeVelocity);
                    return 0;
                }

                if (!AjinSystem.IsOpen)
                    return FailMotion(-2, "VELOCITY OVERRIDE", "AXL 라이브러리가 열려 있지 않습니다.");
                if (IsAlarm)
                    return FailMotion(-2, "VELOCITY OVERRIDE", "축 알람이 ON 상태입니다. alarmCode=0x" + AlarmCode.ToString("X4"));
                if (!IsServoOn)
                    return FailMotion(-2, "VELOCITY OVERRIDE", "축 서보가 OFF 상태입니다.");

                UpdateStatus();
                if (!IsMoving)
                    return -4;

                int ret;
                lock (_sync)
                {
                    ret = AXM.ModifyVelocity(
                        AxisNo,
                        ToBoardVelocity(safeVelocity),
                        ToBoardAcceleration(safeAcceleration),
                        ToBoardAcceleration(safeDeceleration));
                }

                if (ret != 0)
                {
                    return FailMotion(
                        ret,
                        "VELOCITY OVERRIDE",
                        "AXM 속도 오버라이드 명령이 실패했습니다. ret=0x" + ret.ToString("X4"));
                }

                base.OverrideVelocity(safeVelocity);
                return 0;
            }
            catch (Exception ex)
            {
                return FailMotion(
                    -1,
                    "VELOCITY OVERRIDE",
                    "속도 오버라이드 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        #region 팔로잉 이동 (FollowMove)

        // 팔로잉 안전거리 하한. safetyGap 인자가 이 값보다 작으면 이 값으로 클램프한다.
        private const double MinimumFollowSafetyGap = 40.0;
        // 팔로잉 이동 전체 타임아웃(고정). 팔로잉 루프와 최종 완료 대기를 합쳐 적용한다.
        private const int FollowMoveTimeoutMs = 5000;
        // 팔로잉 루프 폴링 주기.
        private const int FollowMovePollIntervalMs = 10;
        // 타임아웃 전용 에러코드.
        private const int FollowMoveTimeoutErrorCode = -21;
        // 선행축 알람 전용 에러코드.
        private const int FollowMoveLeadingAlarmErrorCode = -22;

        /// <summary>
        /// 선행축을 따라가며 후행축(this)을 목표 위치까지 이동시킨다.
        /// 선행축에는 어떤 명령도 내리지 않는다(읽기 전용 — ActualPosition/IsMoving/IsAlarm만 참조).
        /// 두 축의 물리 간격이 safetyGap(최소 40mm) 미만으로 줄어들지 않는 한도 내에서
        /// 포지션 오버라이드로 추종하고, 후행축이 목표에 도달하면 0을 반환한다.
        /// 반환: 0=성공, -1=인자 오류, -2=축 미준비, -11=인터락 거부,
        /// -21=타임아웃(5초 고정), -22=선행축 알람, 그 외=하위 에러코드.
        /// </summary>
        public async Task<int> FollowMoveAsync(
            BaseAxis leadingAxis,
            double leadingTargetPosition,
            double leadingVelocity,
            double leadingAcceleration,
            double leadingDeceleration,
            double trailingTargetPosition,
            double trailingVelocity,
            double trailingAcceleration,
            double trailingDeceleration,
            int direction,
            double safetyGap,
            double homeGap,
            CancellationToken ct = default(CancellationToken))
        {
            Task<int> moveTask = null;

            try
            {
                if (leadingAxis == null)
                    return FailMotion(-1, "FOLLOW MOVE", "선행축이 지정되지 않았습니다.", trailingTargetPosition, true);
                if (direction != 1 && direction != -1)
                    return FailMotion(-1, "FOLLOW MOVE", "direction 인자는 +1 또는 -1이어야 합니다. direction=" + direction, trailingTargetPosition, true);
                if (!IsServoOn || IsAlarm)
                    return FailAxisNotReady("FOLLOW MOVE", trailingTargetPosition, true);

                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.01;

                UpdateStatus();
                if (Math.Abs(ActualPosition - trailingTargetPosition) <= tolerance && !IsMoving)
                    return 0;

                if (direction > 0 && trailingTargetPosition < ActualPosition - tolerance)
                    return FailMotion(-1, "FOLLOW MOVE", "목표 위치가 진행 방향(+)과 반대입니다. actual=" + ActualPosition.ToString("F3") + ", target=" + trailingTargetPosition.ToString("F3"), trailingTargetPosition, true);
                if (direction < 0 && trailingTargetPosition > ActualPosition + tolerance)
                    return FailMotion(-1, "FOLLOW MOVE", "목표 위치가 진행 방향(-)과 반대입니다. actual=" + ActualPosition.ToString("F3") + ", target=" + trailingTargetPosition.ToString("F3"), trailingTargetPosition, true);

                bool safetyGapClamped = safetyGap < MinimumFollowSafetyGap;
                if (safetyGapClamped)
                    safetyGap = MinimumFollowSafetyGap;

                // 팔로잉 프로파일: 선행/후행 인자 중 성분별 작은 값.
                // 0 이하 성분은 해당 축 Config 기본값으로 대체한 뒤 Min을 취한다.
                double leadVel = leadingVelocity > 0.0
                    ? leadingVelocity
                    : (leadingAxis.Config != null ? leadingAxis.Config.DefaultVelocity : 0.0);
                double leadAcc = leadingAcceleration > 0.0
                    ? leadingAcceleration
                    : (leadingAxis.Config != null ? leadingAxis.Config.Acceleration : 0.0);
                double leadDec = leadingDeceleration > 0.0
                    ? leadingDeceleration
                    : (leadingAxis.Config != null ? leadingAxis.Config.Deceleration : 0.0);
                double trailVel = trailingVelocity > 0.0 ? trailingVelocity : Config.DefaultVelocity;
                double trailAcc = trailingAcceleration > 0.0 ? trailingAcceleration : Config.Acceleration;
                double trailDec = trailingDeceleration > 0.0 ? trailingDeceleration : Config.Deceleration;
                double followVel = Math.Min(leadVel, trailVel);
                double followAcc = Math.Min(leadAcc, trailAcc);
                double followDec = Math.Min(leadDec, trailDec);

                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                    Name + " 팔로잉 이동을 시작합니다. leading=" + leadingAxis.Name +
                    ", leadingTarget=" + leadingTargetPosition.ToString("F3") +
                    ", trailingTarget=" + trailingTargetPosition.ToString("F3") +
                    ", direction=" + direction +
                    ", safetyGap=" + safetyGap.ToString("F3") + (safetyGapClamped ? "(클램프됨)" : "") +
                    ", homeGap=" + homeGap.ToString("F3") +
                    ", followVel=" + followVel.ToString("F3") + " - Start");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                double lastCommanded = double.NaN;
                bool firstCommandLogged = false;
                bool finalEntered = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    if (stopwatch.ElapsedMilliseconds >= FollowMoveTimeoutMs)
                        return await FailFollowTimeoutAsync(moveTask, trailingTargetPosition).ConfigureAwait(false);

                    UpdateStatus();

                    if (IsAlarm)
                    {
                        Stop();
                        await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                        moveTask = null;
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                            Name + " 팔로잉 이동 중 후행축 알람이 발생했습니다. alarmCode=0x" + AlarmCode.ToString("X4") + " - Failed");
                        return (int)AlarmCode;
                    }

                    if (leadingAxis.IsAlarm)
                    {
                        Stop();
                        await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                        moveTask = null;
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                            Name + " 팔로잉 이동 중 선행축(" + leadingAxis.Name + ") 알람이 발생했습니다. - Failed");
                        return FailMotion(FollowMoveLeadingAlarmErrorCode, "FOLLOW MOVE",
                            "선행축 알람이 발생했습니다. leading=" + leadingAxis.Name, trailingTargetPosition, true);
                    }

                    // 백그라운드 이동 Task가 오류로 끝났으면 해당 코드로 종료한다.
                    if (moveTask != null && moveTask.IsCompleted)
                    {
                        int backgroundResult = ObserveFollowMoveResult(moveTask);
                        moveTask = null;
                        if (backgroundResult != 0)
                        {
                            Stop();
                            QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                Name + " 팔로잉 백그라운드 이동이 실패했습니다. result=" + backgroundResult + " - Failed");
                            return backgroundResult;
                        }
                    }

                    // 최종 목표 도달 완료 판정 (백그라운드 이동이 목표에서 정상 완료된 경우).
                    if (finalEntered && AxisMoveWaiter.IsMoveCompletedAtTarget(this, trailingTargetPosition, tolerance))
                        break;

                    // 간격/여유 계산 (실측 위치 기준).
                    double leadingActual = leadingAxis.ActualPosition;
                    double gap = direction > 0
                        ? (leadingActual + homeGap) - ActualPosition
                        : (ActualPosition + homeGap) - leadingActual;
                    double slack = gap - safetyGap;

                    if (slack > 0.0)
                    {
                        double intermediate = direction > 0
                            ? ActualPosition + slack
                            : ActualPosition - slack;
                        double command = direction > 0
                            ? Math.Min(trailingTargetPosition, intermediate)
                            : Math.Max(trailingTargetPosition, intermediate);

                        // 명령 위치로 이동 완료를 가정한 간격 재검증.
                        double gapAfter = direction > 0
                            ? (leadingActual + homeGap) - command
                            : (command + homeGap) - leadingActual;
                        bool commandForward = direction > 0
                            ? command > ActualPosition + tolerance
                            : command < ActualPosition - tolerance;
                        bool commandIsFinal = Math.Abs(command - trailingTargetPosition) <= tolerance;

                        if (gapAfter + 0.000001 >= safetyGap && (commandForward || commandIsFinal))
                        {
                            bool commandIssued = false;

                            if (!IsMoving)
                            {
                                if (double.IsNaN(lastCommanded) || Math.Abs(command - lastCommanded) > tolerance ||
                                    !AxisMoveWaiter.IsMoveCompletedAtTarget(this, command, tolerance))
                                {
                                    double startVelocity = commandIsFinal ? trailVel : followVel;
                                    moveTask = MoveAbsoluteAsync(command, startVelocity);
                                    lastCommanded = command;
                                    commandIssued = true;
                                    if (!firstCommandLogged)
                                    {
                                        firstCommandLogged = true;
                                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                            Name + " 팔로잉 최초 이동 명령을 발행했습니다. command=" + command.ToString("F3") +
                                            ", velocity=" + startVelocity.ToString("F3") + " - Ok");
                                    }
                                }
                            }
                            else if (double.IsNaN(lastCommanded) || Math.Abs(command - lastCommanded) > tolerance)
                            {
                                int overrideResult = TryOverridePosition(command, followVel, followAcc, followDec);
                                if (overrideResult == 0)
                                {
                                    lastCommanded = command;
                                    commandIssued = true;
                                }
                                else if (overrideResult == -11)
                                {
                                    Stop();
                                    await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                                    moveTask = null;
                                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                        Name + " 팔로잉 위치 오버라이드가 인터락으로 거부되었습니다. command=" + command.ToString("F3") + " - Failed");
                                    return -11;
                                }
                                else if (overrideResult != -4)
                                {
                                    Stop();
                                    await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                                    moveTask = null;
                                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                        Name + " 팔로잉 위치 오버라이드가 실패했습니다. result=" + overrideResult + " - Failed");
                                    return overrideResult;
                                }
                                // -4(정지 경합)는 무시하고 다음 루프에서 재시도한다.
                            }

                            // 최종 구간 진입: 자기 프로파일로 속도 복귀 후 완료 대기 단계로 전환.
                            bool finalCommandActive = commandIsFinal &&
                                !double.IsNaN(lastCommanded) &&
                                Math.Abs(lastCommanded - trailingTargetPosition) <= tolerance;
                            if (finalCommandActive && !finalEntered)
                            {
                                finalEntered = true;
                                if (IsMoving)
                                    TryOverrideVelocity(trailVel, trailAcc, trailDec);
                                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                    Name + " 팔로잉 최종 구간에 진입했습니다. target=" + trailingTargetPosition.ToString("F3") +
                                    ", velocity=" + trailVel.ToString("F3") +
                                    (commandIssued ? "" : " (명령 유지)") + " - Ok");
                                break;
                            }
                        }
                    }

                    await Task.Delay(FollowMovePollIntervalMs, ct).ConfigureAwait(false);
                }

                // 최종 완료 대기 (남은 타임아웃 적용).
                int remainingMs = FollowMoveTimeoutMs - (int)stopwatch.ElapsedMilliseconds;
                if (remainingMs <= 0)
                    return await FailFollowTimeoutAsync(moveTask, trailingTargetPosition).ConfigureAwait(false);

                AxisMoveWaitResult waitResult = await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                    this,
                    trailingTargetPosition,
                    tolerance,
                    remainingMs,
                    0,
                    ct).ConfigureAwait(false);

                if (waitResult == null || !waitResult.Success)
                {
                    if (waitResult != null && waitResult.Failure == AxisMoveWaitFailure.Timeout)
                        return await FailFollowTimeoutAsync(moveTask, trailingTargetPosition).ConfigureAwait(false);

                    Stop();
                    await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                    moveTask = null;
                    int failCode = waitResult != null ? waitResult.Code : -1;
                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                        Name + " 팔로잉 최종 완료 대기가 실패했습니다. " + AxisMoveWaiter.FormatResult(waitResult, Name) + " - Failed");
                    return FailMotion(failCode, "FOLLOW MOVE",
                        "팔로잉 최종 완료 대기가 실패했습니다. " + (waitResult != null ? waitResult.Reason : ""), trailingTargetPosition, true);
                }

                await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                moveTask = null;
                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                    Name + " 팔로잉 이동이 정상 완료되었습니다. target=" + trailingTargetPosition.ToString("F3") +
                    ", actual=" + ActualPosition.ToString("F3") + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                Stop();
                await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                    Name + " 팔로잉 이동이 취소되었습니다. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                Stop();
                await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                return FailMotion(-1, "FOLLOW MOVE",
                    "팔로잉 이동 처리 중 예외가 발생했습니다. error=" + ex.Message, trailingTargetPosition, true);
            }
            finally
            {
            }
        }

        /// <summary>타임아웃 공통 처리: 정지 → 백그라운드 Task drain → -21 기록/반환.</summary>
        private async Task<int> FailFollowTimeoutAsync(Task<int> moveTask, double trailingTargetPosition)
        {
            Stop();
            await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
            QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                Name + " 팔로잉 이동이 타임아웃(" + FollowMoveTimeoutMs + "ms)되었습니다. actual=" + ActualPosition.ToString("F3") + " - Failed");
            return FailMotion(FollowMoveTimeoutErrorCode, "FOLLOW MOVE",
                "팔로잉 이동이 " + FollowMoveTimeoutMs + "ms 안에 완료되지 않았습니다.", trailingTargetPosition, true);
        }

        /// <summary>
        /// 백그라운드 이동 Task를 최대 2초 대기 후 관찰(observe)한다.
        /// 어떤 종료 경로에서도 unobserved exception이 남지 않도록 한다.
        /// </summary>
        private static async Task DrainFollowMoveTaskAsync(Task<int> moveTask)
        {
            if (moveTask == null)
                return;

            try
            {
                Task completed = await Task.WhenAny(moveTask, Task.Delay(2000)).ConfigureAwait(false);
                if (!object.ReferenceEquals(completed, moveTask))
                {
                    // 시간 내 종료하지 않으면 백그라운드로 관찰만 예약한다.
                    Task observeOnly = moveTask.ContinueWith(
                        t => { var _ = t.Exception; },
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                    return;
                }

                await moveTask.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // drain 중 예외는 팔로잉 종료 흐름을 막지 않는다.
            }
            finally
            {
            }
        }

        /// <summary>완료된 이동 Task의 결과를 예외 없이 회수한다.</summary>
        private static int ObserveFollowMoveResult(Task<int> moveTask)
        {
            try
            {
                return moveTask.GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return -1;
            }
            finally
            {
            }
        }

        #endregion

        public override void ServoOn()
        {
            if (UseSimulation)
            {
                base.ServoOn();
                return;
            }

            if (!AjinSystem.IsOpen) return;
            UpdateStatus();
            // 리밋 밖 복구를 위해 리밋 알람만 있는 경우에는 Servo ON을 허용한다.
            if (IsAlarm && !IsRecoverableLimitAlarmActive()) return;
            int ret;
            lock (_sync)
                ret = AXM.SetAmpEnabled(AxisNo, true);
            if (ret == 0)
                IsServoOn = true;
        }

        public override void ServoOff()
        {
            // 서보 OFF 는 저장된 초기화 신호를 무효화한다. (재초기화 필요)
            _homeDoneLatched = false;

            if (UseSimulation)
            {
                base.ServoOff();
                return;
            }

            Stop();
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
                AXM.SetAmpEnabled(AxisNo, false);
            IsServoOn = false;
        }

        public override void ResetAlarm()
        {
            _motionDirection = 0;
            _softLimitAlarmLatched = false;
            _softLimitAlarmLatchedCode = 0;
            _limitRecoveryActive = false;
            _limitRecoveryDirection = 0;

            if (UseSimulation || !AjinSystem.IsOpen)
            {
                base.ResetAlarm();
                return;
            }

            lock (_sync)
                AXM.AlarmReset(AxisNo, true);
            IsAlarm = false;
            AlarmCode = 0;
        }

        public override async Task<int> MoveAbsoluteAsync(double targetPos, double velocity = 0)
        {
            try
            {
                // SharedRailX axes must pass the centralized pair-clearance guard.
                if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                    SharedRailXMotionRuntime.IsSharedRailAxis(this))
                {
                    return await SharedRailXMotionRuntime.MoveAxisAsync(this, targetPos, velocity).ConfigureAwait(false);
                }

                if (UseSimulation)
                {
                    // 현재 기준: 시뮬레이션 절대 이동도 실장비와 동일하게 MotionGuard를 통과해야 한다.
                    string simulationInterlockReason;
                    if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                        !MotionGuardRuntime.VerifyAxisMove(this, targetPos, out simulationInterlockReason))
                        return FailMotion(-11, "ABS MOVE", simulationInterlockReason, targetPos, true);

                    return await base.MoveAbsoluteAsync(targetPos, velocity);
                }

                // 이 아래가 시뮬과의 차이를 만든다.
                UpdateStatus();
                bool limitRecoveryTarget = IsLimitRecoveryTarget(targetPos);
                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.01;
                if (!BaseAxis.IsForceMoveActive &&
                    AxisMoveWaiter.CanSkipMoveCommandAtTarget(this, targetPos, tolerance))
                {
                    CommandPosition = targetPos;
                    CurrentVelocity = 0.0;
                    IsMoving = false;
                    IsInPosition = true;
                    _motionDirection = 0;
                    ClearMotionFailure();
                    return 0;
                }

                string interlockReason;
                if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                    !MotionGuardRuntime.VerifyAxisMove(this, targetPos, out interlockReason))
                    return FailMotion(-11, "ABS MOVE", interlockReason, targetPos, true);

                if (!IsServoOn || !AjinSystem.IsOpen)
                    return FailAjinAxisNotReady("ABS MOVE", targetPos, true);
                if (IsAlarm && !limitRecoveryTarget)
                    return FailAjinAxisNotReady("ABS MOVE", targetPos, true);
                if (limitRecoveryTarget)
                    BeginLimitRecovery(targetPos > ActualPosition ? 1 : -1);

                if (!limitRecoveryTarget)
                {
                    int limitCheck = CheckSoftLimitTarget(targetPos);
                    if (limitCheck != 0)
                        return limitCheck;
                }

                // 명시 velocity 가 없거나, 기존 시퀀스 헬퍼가 스케일된 DefaultVelocity 를 명시값으로 넘긴 경우에는
                // DefaultVelocity 기반 일반 이동으로 보고 가속/감속도 동일한 비율로 스케일한다.
                bool useDefaultMotionScale = velocity <= 0.0 ||
                    MotionSpeedScale.MatchesDefaultVelocityScale(velocity, Config.DefaultVelocity);
                double vel = velocity > 0 ? velocity : MotionSpeedScale.ApplyDefaultVelocityScale(Config.DefaultVelocity);
                double acceleration = useDefaultMotionScale
                    ? MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Acceleration)
                    : Config.Acceleration;
                double deceleration = useDefaultMotionScale
                    ? MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Deceleration)
                    : Config.Deceleration;
                double boardTargetPos = ToBoardPosition(targetPos);
                double boardVelocity = ToBoardVelocity(vel);
                double boardAcceleration = ToBoardAcceleration(acceleration);
                double boardDeceleration = ToBoardAcceleration(deceleration);

                // To do: [모션 프로파일 로그] 실제 보드에 명령되는 등속/가감속을 남겨 스케일 적용 상태를 검증한다.
                QMC.Common.Log.Write("Main", "SYSTEM", "AxisMoveProfile",
                    Name + " ABS MOVE. target=" + targetPos.ToString("0.###") +
                    ", vel=" + vel.ToString("0.###") +
                    ", acc=" + acceleration.ToString("0.###") +
                    ", dec=" + deceleration.ToString("0.###") +
                    ", defaultScaleApplied=" + useDefaultMotionScale +
                    ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Start");

                CommandPosition = targetPos;
                CurrentVelocity = vel;
                IsMoving = true;
                IsInPosition = false;
                _motionDirection = targetPos > ActualPosition ? 1 : targetPos < ActualPosition ? -1 : 0;
                int motionStopSerial = Volatile.Read(ref _motionStopSerial);

                int ret = 0;
                lock (_sync)
                {
                    AXM.SetAbsRelMode(AxisNo, true);
                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(1000);
                    while (DateTime.UtcNow < deadline)
                    {
                        ret = AXM.MovePosition(AxisNo, boardTargetPos, boardVelocity, boardAcceleration, boardDeceleration);
                        if (ret == 0)
                        {
                            break;

                        }
                    }
                   
                        
                }
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    _motionDirection = 0;
                    return FailMotion(
                        ret,
                        "ABS MOVE",
                        "AXM.MovePosition failed. ret=0x" + ret.ToString("X4"),
                        targetPos,
                        true);
                }

                RaiseMoveStarted();
                int waitRet = await WaitUntilMoveDone(motionStopSerial);
                if (waitRet == 0 && !IsAlarm)
                    _motionDirection = 0;
                if (IsAlarm)
                    return FailMotion((int)AlarmCode, "ABS MOVE", "Axis alarm occurred during move.", targetPos, true);
                if (waitRet == -4)
                    return FailMotion(waitRet, "ABS MOVE", "축 정지 요청으로 이동 대기를 중단했습니다.", targetPos, true);
                if (waitRet != 0)
                    return FailMotion(waitRet, "ABS MOVE", "Move wait failed.", targetPos, true);
                ClearMotionFailure();
                return 0;
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                _motionDirection = 0;
                return FailMotion(-1, "ABS MOVE", ex.Message, targetPos, true);
            }
            finally
            {
                UpdateStatus();
            }
        }

        // To do: [명령 전용 절대이동] FastContiSegmentedPickUp의 이동 중 감시/저속 오버라이드를 위해
        //        MoveAbsoluteAsync의 명령 발행부만 수행하고 즉시 리턴한다(WaitUntilMoveDone 없음).
        //        velocity/acceleration/deceleration은 스케일 완료된 "최종값" 그대로 보드에 전달한다.
        //        도달/정지 감시는 호출자 책임이며, 감시 루프는 UpdateStatus를 주기 호출해야 한다.
        public override Task<int> MoveAbsoluteCommandOnlyAsync(double targetPos, double velocity, double acceleration, double deceleration)
        {
            try
            {
                // 현재 기준: 공유레일 축(X 계열)은 페어 클리어런스 중앙 중재가 필요해 명령 전용을 지원하지 않는다.
                if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                    SharedRailXMotionRuntime.IsSharedRailAxis(this))
                    return Task.FromResult(FailMotion(-1, "ABS MOVE CMD",
                        "SharedRailX 축은 명령 전용 절대이동을 지원하지 않습니다.", targetPos, true));

                if (UseSimulation)
                {
                    string simulationInterlockReason;
                    if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                        !MotionGuardRuntime.VerifyAxisMove(this, targetPos, out simulationInterlockReason))
                        return Task.FromResult(FailMotion(-11, "ABS MOVE CMD", simulationInterlockReason, targetPos, true));

                    return base.MoveAbsoluteCommandOnlyAsync(targetPos, velocity, acceleration, deceleration);
                }

                UpdateStatus();
                bool limitRecoveryTarget = IsLimitRecoveryTarget(targetPos);
                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.01;
                if (!BaseAxis.IsForceMoveActive &&
                    AxisMoveWaiter.CanSkipMoveCommandAtTarget(this, targetPos, tolerance))
                {
                    CommandPosition = targetPos;
                    CurrentVelocity = 0.0;
                    IsMoving = false;
                    IsInPosition = true;
                    _motionDirection = 0;
                    ClearMotionFailure();
                    return Task.FromResult(0);
                }

                string interlockReason;
                if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                    !MotionGuardRuntime.VerifyAxisMove(this, targetPos, out interlockReason))
                    return Task.FromResult(FailMotion(-11, "ABS MOVE CMD", interlockReason, targetPos, true));

                if (!IsServoOn || !AjinSystem.IsOpen)
                    return Task.FromResult(FailAjinAxisNotReady("ABS MOVE CMD", targetPos, true));
                if (IsAlarm && !limitRecoveryTarget)
                    return Task.FromResult(FailAjinAxisNotReady("ABS MOVE CMD", targetPos, true));
                if (limitRecoveryTarget)
                    BeginLimitRecovery(targetPos > ActualPosition ? 1 : -1);

                if (!limitRecoveryTarget)
                {
                    int limitCheck = CheckSoftLimitTarget(targetPos);
                    if (limitCheck != 0)
                        return Task.FromResult(limitCheck);
                }

                // 현재 기준: 전달값이 최종값. 0 이하일 때만 Config 기반 스케일 폴백.
                double vel = velocity > 0 ? velocity : MotionSpeedScale.ApplyDefaultVelocityScale(Config.DefaultVelocity);
                double acc = acceleration > 0 ? acceleration : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Acceleration);
                double dec = deceleration > 0 ? deceleration : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Deceleration);
                double boardTargetPos = ToBoardPosition(targetPos);
                double boardVelocity = ToBoardVelocity(vel);
                double boardAcceleration = ToBoardAcceleration(acc);
                double boardDeceleration = ToBoardAcceleration(dec);

                QMC.Common.Log.Write("Main", "SYSTEM", "AxisMoveProfile",
                    Name + " ABS MOVE CMD. target=" + targetPos.ToString("0.###") +
                    ", vel=" + vel.ToString("0.###") +
                    ", acc=" + acc.ToString("0.###") +
                    ", dec=" + dec.ToString("0.###") +
                    ", commandOnly=True" +
                    ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Start");

                CommandPosition = targetPos;
                CurrentVelocity = vel;
                IsMoving = true;
                IsInPosition = false;
                _motionDirection = targetPos > ActualPosition ? 1 : targetPos < ActualPosition ? -1 : 0;

                int ret;
                lock (_sync)
                {
                    AXM.SetAbsRelMode(AxisNo, true);
                    ret = AXM.MovePosition(AxisNo, boardTargetPos, boardVelocity, boardAcceleration, boardDeceleration);
                }
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    _motionDirection = 0;
                    return Task.FromResult(FailMotion(
                        ret,
                        "ABS MOVE CMD",
                        "AXM.MovePosition failed. ret=0x" + ret.ToString("X4"),
                        targetPos,
                        true));
                }

                RaiseMoveStarted();
                ClearMotionFailure();
                // 기존 MoveAbsoluteAsync와의 차이: WaitUntilMoveDone을 호출하지 않고 즉시 리턴한다.
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                _motionDirection = 0;
                return Task.FromResult(FailMotion(-1, "ABS MOVE CMD", ex.Message, targetPos, true));
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override async Task<int> MoveRelativeAsync(double distance, double velocity = 0)
        {
            try
            {
                if (UseSimulation || !AjinSystem.IsOpen)
                {
                    double simulationTargetPos = ActualPosition + distance;
                    // 현재 기준: 시뮬레이션/드라이런 상대 이동도 실장비와 동일하게 MotionGuard를 통과해야 한다.
                    string simulationInterlockReason;
                    if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                        !MotionGuardRuntime.VerifyAxisMove(this, simulationTargetPos, out simulationInterlockReason))
                        return FailMotion(-11, "REL MOVE", simulationInterlockReason, simulationTargetPos, true);

                    return await base.MoveRelativeAsync(distance, velocity);
                }

                UpdateStatus();
                double targetPos = ActualPosition + distance;
                return await MoveAbsoluteAsync(targetPos, velocity);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-MOVE-REL",
                    Name,
                    ex.Message);
                return -1;
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override void Stop()
        {
            Interlocked.Increment(ref _motionStopSerial);
            _motionDirection = 0;
            _limitRecoveryActive = false;
            _limitRecoveryDirection = 0;

            if (UseSimulation)
            {
                base.Stop();
                return;
            }

            base.Stop();
            if (!AjinSystem.IsOpen) return;
            // 일반 정지는 Stop 전용 감속을 사용한다. (미설정 시 일반 감속으로 폴백)
            lock (_sync)
                AXM.Stop(AxisNo, ToBoardAcceleration(ResolveStopDeceleration()));
        }

        public override void EStop()
        {
            Interlocked.Increment(ref _motionStopSerial);
            _motionDirection = 0;
            _limitRecoveryActive = false;
            _limitRecoveryDirection = 0;

            if (UseSimulation)
            {
                base.EStop();
                return;
            }

            base.EStop();
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
                AXM.StopEmergency(AxisNo);
        }

        public override async Task<int> HomeSearchAsync()
        {
            bool sharedRailXHomeLimitSuppress = false;
            try
            {
                if (UseSimulation)
                {
                    sharedRailXHomeLimitSuppress = BeginSharedRailXHomeLimitSuppress();
                    // 현재 기준: 시뮬레이션 홈 동작도 실장비와 동일하게 MotionGuard를 통과해야 한다.
                    string simulationInterlockReason;
                    if (!MotionGuardRuntime.VerifyAxisHome(this, out simulationInterlockReason))
                        return FailMotion(-11, "HOME", simulationInterlockReason, AxisHomeTarget(), true);

                    return await base.HomeSearchAsync();
                }

                _isHomeSearching = true;
                sharedRailXHomeLimitSuppress = BeginSharedRailXHomeLimitSuppress();
                ClearHomeSearchLimitAlarmState();
                UpdateStatus();

                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisHome(this, out interlockReason))
                    return FailMotion(-11, "HOME", interlockReason, AxisHomeTarget(), true);

                if (IsHomeSearchLimitAlarmActive())
                {
                    ClearHomeSearchLimitAlarmState();
                    UpdateStatus();
                }

                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                    return FailAjinAxisNotReady("HOME", AxisHomeTarget(), true);

                IsHomeDone = false;
                IsMoving = true;
                IsInPosition = false;
                _motionDirection = 0;
                int motionStopSerial = Volatile.Read(ref _motionStopSerial);

                int ret;
                lock (_sync)
                    ret = AXM.SetHomeStart(AxisNo);
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-HOME",
                        Name,
                        "AXM.SetHomeStart failed. ret=0x" + ret.ToString("X4"));
                    return ret;
                }

                RaiseMoveStarted();

                int guard = 0;
                while (!IsHomeDone && !IsAlarm)
                {
                    UpdateStatus();
                    if (Volatile.Read(ref _motionStopSerial) != motionStopSerial && !IsMoving)
                    {
                        IsMoving = false;
                        return FailMotion(-4, "HOME", "축 정지 요청으로 HOME 대기를 중단했습니다.", AxisHomeTarget(), true);
                    }

                    await Task.Delay(20).ConfigureAwait(false);
                    if (++guard > 3000)
                    {
                        IsMoving = false;
                        AlarmManager.Raise(
                            AlarmSeverity.Error,
                            "AX-HOME",
                            Name,
                            "Home search timeout. AxisNo=" + AxisNo);
                        return -3;
                    }
                }

                IsMoving = false;
                IsInPosition = IsHomeDone;
                if (IsHomeDone)
                {
                    int pcOffsetResult = await ApplyPickerThetaPcHomeOffsetAfterHomeAsync().ConfigureAwait(false);
                    if (pcOffsetResult != 0)
                        return pcOffsetResult;

                    // 홈 완료 신호를 latch 하여 재실행 후에도 유지한다.
                    _homeDoneLatched = true;
                    RaiseMoveCompleted();
                    ClearMotionFailure();
                    return 0;
                }
                if (IsAlarm)
                    return FailMotion((int)AlarmCode, "HOME", "Axis alarm occurred during home search.", AxisHomeTarget(), true);
                return FailMotion(-1, "HOME", "Home search failed before HomeDone.", AxisHomeTarget(), true);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-HOME",
                    Name,
                    ex.Message);
                return -1;
            }
            finally
            {
                try
                {
                    UpdateStatus();
                }
                finally
                {
                    _isHomeSearching = false;
                    if (sharedRailXHomeLimitSuppress)
                        EndSharedRailXHomeLimitSuppress();
                }
            }
        }

        private async Task<int> ApplyPickerThetaPcHomeOffsetAfterHomeAsync()
        {
            try
            {
                if (!ShouldApplyPickerThetaPcHomeOffset())
                    return 0;

                double pcHomeOffset = Setup.HomeOffset;
                double startPosition = ActualPosition;
                double targetPosition = startPosition + pcHomeOffset;
                int moveResult = 0;

                if (pcHomeOffset != 0.0)
                {
                    moveResult = await MoveRelativeAsync(pcHomeOffset).ConfigureAwait(false);
                    if (moveResult != 0 || IsAlarm)
                    {
                        return FailMotion(
                            moveResult != 0 ? moveResult : (int)AlarmCode,
                            "HOME PC OFFSET",
                            "Picker T HOME 완료 후 PC Offset 이동 실패. pcHomeOffset=" + pcHomeOffset.ToString("F6") +
                            ", start=" + startPosition.ToString("F6") +
                            ", target=" + targetPosition.ToString("F6"),
                            targetPosition,
                            true);
                    }
                }

                SetPosition(0.0);
                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-HOME-PC-OFFSET",
                    Name + " HOME 완료 후 Picker T PC Offset 이동 및 0점 재설정. pcHomeOffset=" +
                    pcHomeOffset.ToString("F6") +
                    ", start=" + startPosition.ToString("F6") +
                    ", target=" + targetPosition.ToString("F6") +
                    ", zeroSet=0.000000");
                return 0;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-HOME-PC-OFFSET",
                    Name,
                    "Picker T PC Offset 이동/0점 재설정 실패. error=" + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private bool ShouldApplyPickerThetaPcHomeOffset()
        {
            return Setup != null && IsPickerThetaAxisName(Name);
        }

        private static bool IsPickerThetaAxisName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) &&
                   (name.StartsWith("FrontPickerT", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("RearPickerT", StringComparison.OrdinalIgnoreCase));
        }

        public override void SetPosition(double newPosition)
        {
            if (UseSimulation)
            {
                base.SetPosition(newPosition);
                return;
            }

            base.SetPosition(newPosition);
            if (!AjinSystem.IsOpen) return;
            lock (_sync)
            {
                double boardPosition = ToBoardPosition(newPosition);
                AXM.SetCommandPosition(AxisNo, boardPosition);
                AXM.SetActualPosition(AxisNo, boardPosition);
            }
        }

        public override void RestoreRuntimeState(
            double actualPosition,
            double commandPosition,
            bool isServoOn,
            bool isHomeDone,
            bool isInPosition,
            bool isAlarm,
            uint alarmCode)
        {
            base.RestoreRuntimeState(actualPosition, commandPosition, isServoOn, isHomeDone, isInPosition, isAlarm, alarmCode);
            // 저장된 정상 상태(알람 없음)일 때만 HomeDone 신호를 latch 한다.
            _homeDoneLatched = isHomeDone && !isAlarm;
        }

        /// <summary>
        /// 실장비 재실행 시 보드의 HomeDone=off 보고를 무시하고 저장된 모터 초기화 신호를 복원한다.<br/>
        /// 위치/서보 상태는 보드 값을 따르고, 여기서는 HomeDone latch 만 복원한다.
        /// 알람이 저장돼 있으면 복원하지 않는다(재초기화 필요).
        /// </summary>
        public void RestoreHomeDoneSignal(bool isHomeDone, bool isAlarm)
        {
            _homeDoneLatched = isHomeDone && !isAlarm;
            if (_homeDoneLatched)
            {
                IsHomeDone = true;
                Sensor_ORG = true;
            }
        }

        public override void MoveJogContinuous(int direction, JogSpeedType speedType, double customVel = 0)
        {
            try
            {
                bool sharedRailJog = !SharedRailXMotionRuntime.IsInternalDispatch &&
                    SharedRailXMotionRuntime.IsSharedRailAxis(this);

                if (!UseSimulation)
                    UpdateStatus();

                // 이동 중 반복 입력은 새 Jog 명령은 막고, 인터락은 현재 방향 기준으로 재확인한다.
                if (IsMoving)
                {
                    if (sharedRailJog)
                        SharedRailXMotionRuntime.VerifyJogSafetyWhileMoving(this, direction);
                    else if (!SharedRailXMotionRuntime.IsInternalDispatch)
                        VerifyJogSafetyWhileMoving(direction);
                    return;
                }

                if (sharedRailJog)
                {
                    SharedRailXMotionRuntime.MoveJogContinuous(this, direction, ResolveJogSpeed(speedType, customVel));
                    return;
                }

                if (UseSimulation)
                {
                    double simulationJogTarget = ResolveJogGuardTarget(direction);
                    // 현재 기준: 시뮬레이션 Continuous Jog도 실장비와 동일하게 MotionGuard를 통과해야 한다.
                    string simulationInterlockReason;
                    if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                        !MotionGuardRuntime.VerifyAxisContinuousJog(this, simulationJogTarget, "ContinuousJog", out simulationInterlockReason))
                    {
                        RecordMotionFailure(-11, "JOG", simulationInterlockReason, simulationJogTarget, true);
                        return;
                    }

                    base.MoveJogContinuous(direction, speedType, customVel);
                    return;
                }

                if (!IsServoOn || IsAlarm || !AjinSystem.IsOpen)
                {
                    bool limitRecoveryJog = IsLimitRecoveryDirection(direction);
                    if (!IsServoOn || !AjinSystem.IsOpen || !limitRecoveryJog)
                    {
                        FailAjinAxisNotReady("JOG", 0, false);
                        return;
                    }

                    BeginLimitRecovery(direction);
                }

                double jogTarget = ResolveJogGuardTarget(direction);
                string interlockReason;
                if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                    !MotionGuardRuntime.VerifyAxisContinuousJog(this, jogTarget, "ContinuousJog", out interlockReason))
                {
                    RecordMotionFailure(-11, "JOG", interlockReason, jogTarget, true);
                    return;
                }

                if (!IsLimitRecoveryActiveForDirection(direction))
                    UpdateStatus();

                int jogDirection = direction < 0 ? -1 : 1;
                double vel = GetJogVelocity(speedType, customVel);
                double signedVel = jogDirection * Math.Abs(vel);
                double boardSignedVel = ToBoardVelocity(signedVel);
                // Jog 구동은 Jog 가감속을 사용한다. (미설정 시 일반 가감속으로 폴백)
                double boardAcceleration = ToBoardAcceleration(ResolveJogAcceleration());
                double boardDeceleration = ToBoardAcceleration(ResolveJogDeceleration());
                CurrentVelocity = signedVel;
                IsMoving = true;
                IsInPosition = false;
                _motionDirection = jogDirection;

                int ret;
                lock (_sync)
                    ret = AXM.MoveVelocity(AxisNo, boardSignedVel, boardAcceleration, boardDeceleration);
                if (ret != 0)
                {
                    IsMoving = false;
                    IsAlarm = true;
                    AlarmCode = (uint)ret;
                    _motionDirection = 0;
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-JOG",
                        Name,
                        "AXM.MoveVelocity failed. ret=0x" + ret.ToString("X4"));
                    return;
                }

                RaiseMoveStarted();
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                _motionDirection = 0;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-JOG",
                    Name,
                    ex.Message);
            }
            finally
            {
                UpdateStatus();
            }
        }

        private double ResolveJogSpeed(JogSpeedType speedType, double customVel)
        {
            return GetJogVelocity(speedType, customVel);
        }

        private void VerifyJogSafetyWhileMoving(int direction)
        {
            try
            {
                double jogTarget = ResolveJogGuardTarget(direction);
                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisContinuousJog(this, jogTarget, "ContinuousJog", out interlockReason))
                {
                    // 현재 기준: 조그 중 실시간 재검사에서 차단되면 해당 축을 즉시 비상정지한다.
                    EStop();
                }
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "AX-JOG-GUARD", Name, ex.Message);
            }
            finally
            {
            }
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

        /// <summary>Jog 구동 가속도. JogAcceleration 미설정(0 이하) 시 일반 Acceleration 으로 폴백한다.</summary>
        private double ResolveJogAcceleration()
        {
            return Config != null && Config.JogAcceleration > 0.0
                ? Config.JogAcceleration
                : (Config != null ? Config.Acceleration : 0.0);
        }

        /// <summary>Jog 구동 감속도. JogDeceleration 미설정(0 이하) 시 일반 Deceleration 으로 폴백한다.</summary>
        private double ResolveJogDeceleration()
        {
            return Config != null && Config.JogDeceleration > 0.0
                ? Config.JogDeceleration
                : (Config != null ? Config.Deceleration : 0.0);
        }

        /// <summary>Jog 정지(StopJog) 전용 감속도. 미설정(0 이하) 시 JogDeceleration→Deceleration 순으로 폴백한다.</summary>
        private double ResolveJogStopDeceleration()
        {
            return Config != null && Config.JogStopDeceleration > 0.0
                ? Config.JogStopDeceleration
                : ResolveJogDeceleration();
        }

        /// <summary>일반 정지(Stop) 전용 감속도. 미설정(0 이하) 시 일반 Deceleration 으로 폴백한다.</summary>
        private double ResolveStopDeceleration()
        {
            return Config != null && Config.StopDeceleration > 0.0
                ? Config.StopDeceleration
                : (Config != null ? Config.Deceleration : 0.0);
        }

        public override async Task<int> MoveJogStepAsync(int direction, JogSpeedType speedType,
                                                         double stepDistance, double customVel = 0)
        {
            try
            {
                bool sharedRailJog = !SharedRailXMotionRuntime.IsInternalDispatch &&
                    SharedRailXMotionRuntime.IsSharedRailAxis(this);

                if (!UseSimulation)
                    UpdateStatus();

                // 이동 중 반복 Step Jog 입력은 새 명령은 막고, 인터락은 현재 방향 기준으로 재확인한다.
                if (IsMoving)
                {
                    if (sharedRailJog)
                        SharedRailXMotionRuntime.VerifyJogSafetyWhileMoving(this, direction);
                    else if (!SharedRailXMotionRuntime.IsInternalDispatch)
                        VerifyJogSafetyWhileMoving(direction);
                    return 0;
                }

                if (sharedRailJog)
                {
                    return await SharedRailXMotionRuntime.MoveJogStepAsync(
                        this,
                        direction,
                        speedType,
                        stepDistance,
                        customVel).ConfigureAwait(false);
                }

                if (UseSimulation)
                {
                    double simulationDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(stepDistance);
                    double simulationTarget = ActualPosition + simulationDistance;
                    // 현재 기준: 시뮬레이션 Step Jog도 실장비와 동일하게 MotionGuard를 통과해야 한다.
                    string simulationInterlockReason;
                    if (!SharedRailXMotionRuntime.IsInternalDispatch &&
                        !MotionGuardRuntime.VerifyAxisStepJog(this, simulationTarget, "StepJog", out simulationInterlockReason))
                        return FailMotion(-11, "JOG STEP", simulationInterlockReason, simulationTarget, true);

                    return await base.MoveJogStepAsync(direction, speedType, stepDistance, customVel);
                }

                if (!IsServoOn || IsAlarm)
                {
                    bool limitRecoveryStep = IsLimitRecoveryDirection(direction);
                    if (!IsServoOn || !limitRecoveryStep)
                        return FailAjinAxisNotReady("JOG STEP", 0, false);

                    BeginLimitRecovery(direction);
                }

                int jogDirection = direction < 0 ? -1 : 1;
                double vel = GetJogVelocity(speedType, customVel);
                double distance = jogDirection * Math.Abs(stepDistance);
                if (distance == 0)
                    return 0;
                double target = ActualPosition + distance;
                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisStepJog(this, target, "StepJog", out interlockReason))
                    return FailMotion(-11, "JOG STEP", interlockReason);

                if (IsRecoverableLimitAlarmActive() && !IsLimitRecoveryTarget(target))
                    return FailAjinAxisNotReady("JOG STEP", target, true);

                if (!IsLimitRecoveryActiveForDirection(direction))
                    UpdateStatus();
                if (IsMoving)
                    return 0;

                int result;
                using (BaseAxis.BeginMotionGuardBypass())
                using (BaseAxis.BeginForceMoveScope())
                {
                    result = await MoveRelativeAsync(distance, vel);
                }
                if (result != 0)
                    return result;

                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.05;
                AxisMoveWaitResult wait = await AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                    this,
                    target,
                    tolerance,
                    60000,
                    0).ConfigureAwait(false);
                if (wait != null && wait.Success)
                    return 0;

                return FailMotion(
                    wait != null ? wait.Code : -1,
                    "JOG STEP",
                    "Step Jog 위치 확인 실패. " + AxisMoveWaiter.FormatResult(wait, Name),
                    target,
                    true);
            }
            catch (Exception ex)
            {
                IsMoving = false;
                IsAlarm = true;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-JOG-STEP",
                    Name,
                    ex.Message);
                return -1;
            }
            finally
            {
                UpdateStatus();
            }
        }

        public override void StopJog()
        {
            _motionDirection = 0;

            if (UseSimulation || !AjinSystem.IsOpen)
            {
                base.StopJog();
                return;
            }

            // Jog 정지는 Jog 정지 전용 감속을 사용한다. (미설정 시 JogDeceleration→Deceleration 폴백)
            lock (_sync)
                AXM.Stop(AxisNo, ToBoardAcceleration(ResolveJogStopDeceleration()));
            base.StopJog();
            UpdateStatus();
        }

        public override void UpdateStatus()
        {

            //Log.Write("AjinAxis", "UpdateStatus", "Updating status for AxisNo=" + AxisNo);
            if (Config.IsSimulationMode)
            {
                base.UpdateStatus();
                return;
            }

            if (!AjinSystem.IsOpen)
                return;

            double cmd = 0;
            double act = 0;
            bool mot = false;
            bool inp = false;
            bool fault = false;
            bool pel = false;
            bool mel = false;
            bool org = false;
            bool svOn = false;
            var homeResult = AXT_MOTION_HOME_RESULT.HOME_SEARCHING;
            int homeRet;
            int servoRet;

            MOTION_INFO info = new MOTION_INFO();

            //외부 센서 및 모터 관련 신호 상태값: AXT_MOTION_QIMECHANICAL_SIGNAL_DEF 
            //    - [00001h]Bit 0, +Limit 급정지 신호 현재 상태 
            //    - [00002h] Bit 1, -Limit 급정지 신호 현재 상태 
            //    - [00004h]Bit 2, +limit 감속정지 현재 상태
            //    - [00008h]Bit 3, -limit 감속정지 현재 상태
            //    - [00010h]Bit 4, Alarm 신호 신호 현재 상태
            //    - [00020h]Bit 5, InPos 신호 현재 상태
            //    - [00040h]Bit 6, 비상 정지 신호(ESTOP) 현재 상태
            //    - [00080h]Bit 7, 원점 신호 헌재 상태
            //    - [00100h]Bit 8, Z 상 입력 신호 현재 상태
            //    - [00200h]Bit 9, ECUP 터미널 신호 상태
            //    - [00400h]Bit 10, ECDN 터미널 신호 상태
            //    - [00800h]Bit 11, EXPP 터미널 신호 상태
            //    - [01000h]Bit 12, EXMP 터미널 신호 상태
            //    - [02000h]Bit 13, SQSTR1 터미널 신호 상태
            //    - [04000h]Bit 14, SQSTR2 터미널 신호 상태
            //    - [08000h]Bit 15, SQSTP1 터미널 신호 상태
            //    - [10000h]Bit 16, SQSTP2 터미널 신호 상태
            //    - [20000h]Bit 17, MODE 터미널 신호 상태
            const uint PlustLimitMask = 0x00001;
            const uint MinusLimitMask = 0x00002;
            const uint AlarmMask = 0x00010;
            const uint InPositionMask = 0x00020;
            const uint OriginMask = 0x00080;

            lock (_sync)
            {
                info.uMask = 0x1F;
                AXM.GetMotionInfo(AxisNo, ref info);
                cmd = info.dCmdPos;
                act = info.dActPos;
                mot = (info.uMechSig & 0x1) != 0;
                inp = (info.uMechSig & InPositionMask) != 0;
                fault = (info.uMechSig & AlarmMask) != 0;
                pel = (info.uMechSig & PlustLimitMask) != 0;
                mel = (info.uMechSig & MinusLimitMask) != 0;
                org = (info.uMechSig & OriginMask) != 0;
                servoRet = AXM.GetAmpEnabled(AxisNo, ref svOn);
                homeRet = AXM.GetHomeResult(AxisNo, ref homeResult);
                // Todo : 구부장 아래 내용  AXM.GetMotionInfo(AxisNo, ref info); 이것으로 대체 되는 것들은 삭제 했음.
                // 주석 확인 했으면 아래 주석 삭제 할것.

                //AXM.GetCommandPosition(AxisNo, ref cmd);
                AXM.GetActualPosition(AxisNo, ref act);
                AXM.GetInMotion(AxisNo, ref mot);
                AXM.GetInPositionValue(AxisNo, ref inp);
                //AXM.GetAmpFaultValue(AxisNo, ref fault);
                //AXM.GetPositiveLimitValue(AxisNo, ref pel);
                //AXM.GetNegativeLimitValue(AxisNo, ref mel);
                //AXM.GetHomeSensorValue(AxisNo, ref org);
                //ApplyReadStatus(cmd, act, mot, inp, fault, homeRet, homeResult, pel, mel, org, servoRet, svOn);
            }

            ApplyReadStatus(cmd, act, mot, inp, fault, homeRet, homeResult, pel, mel, org, servoRet, svOn);

            //Log.Write("AjinAxis", "UpdateStatus", "Updated status End");
        }


        private void ApplyReadStatus(
            double cmd,
            double act,
            bool mot,
            bool inp,
            bool fault,
            int homeRet,
            AXT_MOTION_HOME_RESULT homeResult,
            bool pel,
            bool mel,
            bool org,
            int servoRet,
            bool svOn)
        {
            CommandPosition = FromBoardPosition(cmd);

            double prev = ActualPosition;
            ActualPosition = FromBoardPosition(act);
            if (prev != act)
                RaisePositionChanged();

            bool wasMoving = IsMoving;
            int statusMotionDirection = _motionDirection;
            IsMoving = mot;
            IsInPosition = inp;

            bool limitAlarmSuppressed = _isHomeSearching || IsSharedRailXHomeLimitSuppressed();
            bool wasAlarm = IsAlarm;
            double softLimitTolerance = ResolveSoftLimitStatusTolerance();
            bool rawSoftLimitPositive = !limitAlarmSuppressed && Setup != null && Setup.SoftLimitEnabled &&
                ((ActualPosition >= Setup.SoftLimitPlus - softLimitTolerance && statusMotionDirection > 0) ||
                 ActualPosition > Setup.SoftLimitPlus + softLimitTolerance);
            bool rawSoftLimitNegative = !limitAlarmSuppressed && Setup != null && Setup.SoftLimitEnabled &&
                ((ActualPosition <= Setup.SoftLimitMinus + softLimitTolerance && statusMotionDirection < 0) ||
                 ActualPosition < Setup.SoftLimitMinus - softLimitTolerance);
            int hardwareLimitSearchDirection = Volatile.Read(ref _hardwareLimitSearchDirection);
            bool expectedInitializeLimitPositive = hardwareLimitSearchDirection > 0 && pel;
            bool expectedInitializeLimitNegative = hardwareLimitSearchDirection < 0 && mel;
            bool rawHardLimitPositive = !limitAlarmSuppressed && pel && !expectedInitializeLimitPositive;
            bool rawHardLimitNegative = !limitAlarmSuppressed && mel && !expectedInitializeLimitNegative;
            bool suppressLimitAlarmForRecovery = ShouldSuppressLimitAlarmForRecovery(
                rawSoftLimitPositive,
                rawSoftLimitNegative,
                rawHardLimitPositive,
                rawHardLimitNegative);
            bool softLimitPositive = rawSoftLimitPositive && !suppressLimitAlarmForRecovery;
            bool softLimitNegative = rawSoftLimitNegative && !suppressLimitAlarmForRecovery;
            bool hardLimitPositive = rawHardLimitPositive && !suppressLimitAlarmForRecovery;
            bool hardLimitNegative = rawHardLimitNegative && !suppressLimitAlarmForRecovery;
            if (!rawSoftLimitPositive && !rawSoftLimitNegative && !rawHardLimitPositive && !rawHardLimitNegative)
            {
                _limitRecoveryActive = false;
                _limitRecoveryDirection = 0;
            }
            else if (_limitRecoveryActive && !suppressLimitAlarmForRecovery)
            {
                _limitRecoveryActive = false;
                _limitRecoveryDirection = 0;
            }

            if (softLimitPositive || softLimitNegative)
            {
                _softLimitAlarmLatched = true;
                _softLimitAlarmLatchedCode = softLimitPositive ? 10u : 11u;
            }

            bool softLimitLatched = _softLimitAlarmLatched;
            bool limitAlarm = softLimitPositive || softLimitNegative || hardLimitPositive || hardLimitNegative || softLimitLatched;

            IsAlarm = fault || limitAlarm;
            if (IsAlarm)
            {
                if (fault)
                    AlarmCode = AlarmCode == 0 ? 1u : AlarmCode;
                else if (softLimitPositive)
                    AlarmCode = 10;
                else if (softLimitNegative)
                    AlarmCode = 11;
                else if (hardLimitPositive)
                    AlarmCode = 20;
                else if (hardLimitNegative)
                    AlarmCode = 21;
                else if (softLimitLatched)
                    AlarmCode = _softLimitAlarmLatchedCode != 0 ? _softLimitAlarmLatchedCode : 10;

                if (!wasAlarm)
                {
                    RaiseAxisAlarmForCurrentStatus(fault, softLimitPositive, softLimitNegative, hardLimitPositive, hardLimitNegative);
                }
            }
            else
            {
                AlarmCode = 0;
            }

            bool wasPel = Sensor_PEL;
            bool wasMel = Sensor_MEL;
            Sensor_PEL = pel;
            Sensor_MEL = mel;
            Sensor_ORG = org;
            bool expectedInitializeLimitEdge =
                (expectedInitializeLimitPositive && Sensor_PEL && !wasPel) ||
                (expectedInitializeLimitNegative && Sensor_MEL && !wasMel);
            if (!limitAlarmSuppressed && !expectedInitializeLimitEdge && !IsAlarm &&
                ((Sensor_PEL && !wasPel) || (Sensor_MEL && !wasMel)))
            {
                string side = Sensor_PEL ? "PEL(+)" : "MEL(-)";
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "LIMIT-HIT",
                    Name,
                    "Limit sensor reached [" + side + "] AxisNo=" + AxisNo);
            }

            if (servoRet == 0)
                IsServoOn = svOn;

            // 리밋 알람은 HomeDone 기준을 유지하고, 서보 알람/서보 OFF일 때만 latch를 해제한다.
            if (!IsServoOn || fault || (IsAlarm && !limitAlarm))
                _homeDoneLatched = false;

            if (wasMoving && !IsMoving && IsInPosition)
            {
                _motionDirection = 0;
                if (!IsAlarm)
                    RaiseMoveCompleted();
            }
            else if (!IsMoving)
            {
                _motionDirection = 0;
            }

            // latch 가 살아있으면 보드의 재실행 후 HomeDone=off 보고를 무시하고 저장된 완료 상태를 유지한다.
            // latch 가 없으면 보드의 HomeResult 를 그대로 반영한다.
            if (_homeDoneLatched)
                IsHomeDone = true;
            else if (homeRet == 0)
                IsHomeDone = homeResult == AXT_MOTION_HOME_RESULT.HOME_SUCCESS;
        }

        private double ResolveSoftLimitStatusTolerance()
        {
            try
            {
                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.001;
                if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance <= 0.0)
                    tolerance = 0.001;
                return Math.Min(tolerance, 0.001);
            }
            catch
            {
                return 0.001;
            }
            finally
            {
            }
        }

        private bool IsRecoverableLimitAlarmActive()
        {
            return IsPositiveLimitActive() || IsNegativeLimitActive();
        }

        private bool IsHomeSearchLimitAlarmActive()
        {
            return IsRecoverableLimitAlarmActive() || IsLimitAlarmCode(AlarmCode);
        }

        private static bool IsLimitAlarmCode(uint alarmCode)
        {
            return alarmCode == 10 || alarmCode == 11 || alarmCode == 20 || alarmCode == 21;
        }

        private void ClearHomeSearchLimitAlarmState()
        {
            if (!IsHomeSearchLimitAlarmActive() && !_softLimitAlarmLatched)
                return;

            _softLimitAlarmLatched = false;
            _softLimitAlarmLatchedCode = 0;
            _limitRecoveryActive = false;
            _limitRecoveryDirection = 0;

            if (IsLimitAlarmCode(AlarmCode))
            {
                IsAlarm = false;
                AlarmCode = 0;
                ClearMotionFailure();
            }
        }

        public bool CanRecoverLimitByJogDirection(int direction)
        {
            try
            {
                if (UseSimulation)
                    return false;

                UpdateStatus();
                // 리밋 알람 복구는 리밋에서 빠져나가는 Jog 방향일 때만 외부 UI에서 허용한다.
                return IsRecoverableLimitAlarmActive() && IsLimitRecoveryDirection(direction);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsPositiveLimitActive()
        {
            try
            {
                if (Sensor_PEL || AlarmCode == 20)
                    return true;
                if (_softLimitAlarmLatched && _softLimitAlarmLatchedCode == 10)
                    return true;
                if (AlarmCode == 10)
                    return true;
                return Setup != null &&
                       Setup.SoftLimitEnabled &&
                       ActualPosition > Setup.SoftLimitPlus + ResolveSoftLimitStatusTolerance();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsNegativeLimitActive()
        {
            try
            {
                if (Sensor_MEL || AlarmCode == 21)
                    return true;
                if (_softLimitAlarmLatched && _softLimitAlarmLatchedCode == 11)
                    return true;
                if (AlarmCode == 11)
                    return true;
                return Setup != null &&
                       Setup.SoftLimitEnabled &&
                       ActualPosition < Setup.SoftLimitMinus - ResolveSoftLimitStatusTolerance();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsLimitRecoveryTarget(double targetPosition)
        {
            try
            {
                bool positiveLimit = IsPositiveLimitActive();
                bool negativeLimit = IsNegativeLimitActive();
                if (positiveLimit == negativeLimit)
                    return false;

                // +리밋에서는 -방향, -리밋에서는 +방향으로 빠져나가는 이동만 복구로 인정한다.
                return positiveLimit
                    ? targetPosition < ActualPosition
                    : targetPosition > ActualPosition;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsLimitRecoveryDirection(int direction)
        {
            try
            {
                int normalizedDirection = direction < 0 ? -1 : direction > 0 ? 1 : 0;
                if (normalizedDirection == 0)
                    return false;

                bool positiveLimit = IsPositiveLimitActive();
                bool negativeLimit = IsNegativeLimitActive();
                if (positiveLimit == negativeLimit)
                    return false;

                // +리밋에서는 -방향, -리밋에서는 +방향 Jog만 복구로 허용한다.
                return positiveLimit
                    ? normalizedDirection < 0
                    : normalizedDirection > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsLimitRecoveryActiveForDirection(int direction)
        {
            int normalizedDirection = direction < 0 ? -1 : direction > 0 ? 1 : 0;
            return _limitRecoveryActive &&
                   normalizedDirection != 0 &&
                   _limitRecoveryDirection == normalizedDirection;
        }

        private void BeginLimitRecovery(int direction)
        {
            int normalizedDirection = direction < 0 ? -1 : direction > 0 ? 1 : 0;
            if (normalizedDirection == 0)
                return;

            _limitRecoveryActive = true;
            _limitRecoveryDirection = normalizedDirection;
            _softLimitAlarmLatched = false;
            _softLimitAlarmLatchedCode = 0;
            IsAlarm = false;
            AlarmCode = 0;
            ClearMotionFailure();
        }

        private bool ShouldSuppressLimitAlarmForRecovery(
            bool softLimitPositive,
            bool softLimitNegative,
            bool hardLimitPositive,
            bool hardLimitNegative)
        {
            try
            {
                if (!_limitRecoveryActive || !IsMoving)
                    return false;

                bool positiveLimit = softLimitPositive || hardLimitPositive;
                bool negativeLimit = softLimitNegative || hardLimitNegative;
                if (positiveLimit == negativeLimit)
                    return false;

                // 복구 방향으로 실제 이동 중일 때만 리밋 알람 재발생을 잠시 억제한다.
                return positiveLimit
                    ? _limitRecoveryDirection < 0
                    : _limitRecoveryDirection > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private int CheckSoftLimitTarget(double targetPos)
        {
            try
            {
                if (Setup == null || !Setup.SoftLimitEnabled)
                    return 0;

                if (targetPos > Setup.SoftLimitPlus)
                {
                    return FailSoftLimit(10, "positive", targetPos, Setup.SoftLimitPlus);
                }

                if (targetPos < Setup.SoftLimitMinus)
                {
                    return FailSoftLimit(11, "negative", targetPos, Setup.SoftLimitMinus);
                }

                return 0;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "AX-SOFT-LIMIT-CHECK", Name, ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private void RaiseSoftLimitAlarm(uint alarmCode, string side, double position, double limit)
        {
            try
            {
                IsMoving = false;
                IsInPosition = false;
                IsAlarm = true;
                AlarmCode = alarmCode;
                _softLimitAlarmLatched = true;
                _softLimitAlarmLatchedCode = alarmCode;
                _motionDirection = 0;

                string message = "Soft limit reached (" + side + "). Position=" +
                    position.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                    ", Limit=" +
                    limit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                    ", AxisNo=" + AxisNo;

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
            }
        }

        private int FailSoftLimit(int alarmCode, string side, double position, double limit)
        {
            string message = "Soft limit reached (" + side + "). Position=" +
                position.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", Limit=" +
                limit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", AxisNo=" + AxisNo;

            RaiseSoftLimitAlarm((uint)alarmCode, side, position, limit);
            return FailMotion(alarmCode, "ABS MOVE", message, position, true);
        }

        private void RaiseAxisAlarmForCurrentStatus(
            bool fault,
            bool softLimitPositive,
            bool softLimitNegative,
            bool hardLimitPositive,
            bool hardLimitNegative)
        {
            try
            {
                if (fault)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-" + AxisNo,
                        Name,
                        "Servo alarm 0x" + AlarmCode.ToString("X4"));
                    return;
                }

                if (softLimitPositive || softLimitNegative)
                {
                    string side = softLimitPositive ? "positive" : "negative";
                    double limit = softLimitPositive ? Setup.SoftLimitPlus : Setup.SoftLimitMinus;
                    string message = "Soft limit reached (" + side + "). Position=" +
                        ActualPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        ", Limit=" +
                        limit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        ", AxisNo=" + AxisNo;

                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        softLimitPositive ? "AX-SOFT-LIMIT-P" : "AX-SOFT-LIMIT-N",
                        Name,
                        message);
                    return;
                }

                if (hardLimitPositive || hardLimitNegative)
                {
                    string side = hardLimitPositive ? "PEL(+)" : "MEL(-)";
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "LIMIT-HIT",
                        Name,
                        "Limit sensor reached [" + side + "] AxisNo=" + AxisNo);
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
        /// 보드에서 모니터링 전용 라이브 값을 한 번에 읽어 <see cref="AxisLiveStatus"/> 로 반환한다.<br/>
        /// 시뮬레이션이거나 보드가 닫혀 있으면 <c>null</c>.
        /// </summary>
        public AxisLiveStatus ReadLiveStatus()
        {
            if (Config.IsSimulationMode || !AjinSystem.IsOpen) return null;

            var s = new AxisLiveStatus();
            try
            {
                lock (_sync)
                {
                    AXM.MotorOutputMethod outMethod = 0;
                    AXM.EncoderInputMethod encMethod = 0;
                    ActiveLevel zLvl = 0, srvLvl = 0;
                    double maxVel = 0, unit = 0;
                    int pulse = 0;
                    AXM.GetOutputMethod(AxisNo, ref outMethod);
                    AXM.GetEncoderMethod(AxisNo, ref encMethod);
                    AXM.GetZPhaseLevel(AxisNo, ref zLvl);
                    AXM.GetAmpEnableLevel(AxisNo, ref srvLvl);
                    AXM.GetMaxVelocity(AxisNo, ref maxVel);
                    AXM.GetMoveUnitPerPulse(AxisNo, ref unit, ref pulse);
                    s.OutputMethod = outMethod;
                    s.EncoderMethod = encMethod;
                    s.ZPhaseLevel = zLvl;
                    s.ServoOnLevel = srvLvl;
                    s.MaxVelocity = FromBoardVelocity(maxVel);
                    s.MoveUnit = unit;
                    s.PulsePerUnit = pulse;

                    bool inpEnable = false, inpValue = false;
                    ActiveLevel inpLvl = 0;
                    AXM.GetInPositionEnable(AxisNo, ref inpEnable);
                    AXM.GetInPositionLevel(AxisNo, ref inpLvl);
                    AXM.GetInPositionValue(AxisNo, ref inpValue);
                    s.InPositionEnabled = inpEnable;
                    s.InPositionLevel = inpLvl;
                    s.InPositionValue = inpValue;

                    MotorEventAction posAct = 0, negAct = 0;
                    ActiveLevel posLvl = 0, negLvl = 0;
                    bool posVal = false, negVal = false;
                    double swPos = 0, swNeg = 0;
                    AXM.GetPositiveLimitAction(AxisNo, ref posAct);
                    AXM.GetPositiveLimitLevel(AxisNo, ref posLvl);
                    AXM.GetPositiveLimitValue(AxisNo, ref posVal);
                    AXM.GetNegativeLimitAction(AxisNo, ref negAct);
                    AXM.GetNegativeLimitLevel(AxisNo, ref negLvl);
                    AXM.GetNegativeLimitValue(AxisNo, ref negVal);
                    AXM.GetPositivePosition(AxisNo, ref swPos);
                    AXM.GetNegativePosition(AxisNo, ref swNeg);
                    s.PositiveLimitAction = posAct;
                    s.PositiveLimitLevel = posLvl;
                    s.PositiveLimitValue = posVal;
                    s.NegativeLimitAction = negAct;
                    s.NegativeLimitLevel = negLvl;
                    s.NegativeLimitValue = negVal;
                    s.SoftLimitPositive = FromBoardPosition(swPos);
                    s.SoftLimitNegative = FromBoardPosition(swNeg);

                    ActiveLevel ampFaultLvl = 0, ampResetLvl = 0, homeLvl = 0;
                    bool ampFaultVal = false, homeVal = false;
                    AXM.GetAmpFaultLevel(AxisNo, ref ampFaultLvl);
                    AXM.GetAmpFaultValue(AxisNo, ref ampFaultVal);
                    AXM.GetAmpResetLevel(AxisNo, ref ampResetLvl);
                    AXM.GetHomeSensorLevel(AxisNo, ref homeLvl);
                    AXM.GetHomeSensorValue(AxisNo, ref homeVal);
                    s.AmpFaultLevel = ampFaultLvl;
                    s.AmpFaultValue = ampFaultVal;
                    s.AmpResetLevel = ampResetLvl;
                    s.HomeSensorLevel = homeLvl;
                    s.HomeSensorValue = homeVal;
                }

                s.IsAlarm = IsAlarm;
                s.AlarmCode = AlarmCode;
                s.ActualPosition = ActualPosition;
                s.CommandPosition = CommandPosition;
                s.PositionError = CommandPosition - ActualPosition;
                return s;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-READ-LIVE",
                    Name,
                    "ReadLiveStatus failed: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 보드에서 setup/config 에 매핑되는 파라미터를 읽어 현재 축의
        /// <see cref="AxisSetup"/> 및 <see cref="AxisConfig"/> 에 덮어쓴다.<br/>
        /// .mot 파일 LoadParameters 후 보드 → 모델 동기화 용도.
        /// </summary>
        /// <returns>적용 성공 여부.</returns>
        public bool ReadSetupFromBoard()
        {
            if (Config.IsSimulationMode || !AjinSystem.IsOpen) return false;

            try
            {
                AxisConfig c = Config;
                AxisSetup setup = Setup;

                lock (_sync)
                {
                    // Home velocities / accelerations
                    double v1 = 0, v2 = 0, vLast = 0, vIndex = 0, a1 = 0, a2 = 0;
                    if (AXM.GetHomeVelocity(AxisNo, ref v1, ref v2, ref vLast, ref vIndex, ref a1, ref a2) == 0)
                    {
                        c.HomeFirstVelocity = FromBoardVelocity(v1);
                        c.HomeSecondVelocity = FromBoardVelocity(v2);
                        c.HomeThirdVelocity = FromBoardVelocity(vLast);
                        c.HomeLastVelocity = FromBoardVelocity(vLast);
                        c.HomeIndexSearchVelocity = FromBoardVelocity(vIndex);
                        c.HomeFirstAcceleration = FromBoardAcceleration(a1);
                        c.HomeSecondAcceleration = FromBoardAcceleration(a2);
                        c.HomeVelocity = FromBoardVelocity(v1);
                    }

                    // Home method
                    HomeDirection hDir = HomeDirection.Ccw;
                    HomeSignal hSig = HomeSignal.HomeSensor;
                    HomeZPhase hZ = 0;
                    double hClr = 0, hOff = 0;
                    if (AXM.GetHomeMethod(AxisNo, ref hDir, ref hSig, ref hZ, ref hClr, ref hOff) == 0 && setup != null)
                    {
                        setup.HomeDirection = hDir;
                        setup.HomeSignal = hSig;
                        // 현재 기준: Picker T HomeOffset은 보드값이 아니라 홈 후 PC 보정값이므로 보드 읽기로 덮지 않는다.
                        if (!ShouldApplyPickerThetaPcHomeOffset())
                            setup.HomeOffset = FromBoardPosition(hOff);
                    }

                    // Max velocity
                    double maxVel = 0;
                    if (AXM.GetMaxVelocity(AxisNo, ref maxVel) == 0 && maxVel > 0)
                        c.MaxVelocity = FromBoardVelocity(maxVel);

                    // Unit / pulse
                    double unit = 0; int pulse = 0;
                    if (AXM.GetMoveUnitPerPulse(AxisNo, ref unit, ref pulse) == 0 && pulse > 0 && setup != null)
                        setup.PulsesPerUnit = pulse / (unit > 0 ? unit : 1.0);

                    // Signal levels
                    if (setup != null)
                    {
                        ActiveLevel srvLvl = 0, ampFault = 0, ampReset = 0, posLvl = 0, negLvl = 0;
                        AXM.GetAmpEnableLevel(AxisNo, ref srvLvl);
                        AXM.GetAmpFaultLevel(AxisNo, ref ampFault);
                        AXM.GetAmpResetLevel(AxisNo, ref ampReset);
                        AXM.GetPositiveLimitLevel(AxisNo, ref posLvl);
                        AXM.GetNegativeLimitLevel(AxisNo, ref negLvl);
                        setup.ServoOnLevel = srvLvl;
                        setup.AlarmLevel = ampFault;
                        setup.AlarmResetLevel = ampReset;
                        setup.PositiveLimitLevel = posLvl;
                        setup.NegativeLimitLevel = negLvl;

                        // Soft limits
                        double swPos = 0, swNeg = 0;
                        if (AXM.GetPositivePosition(AxisNo, ref swPos) == 0)
                            setup.SoftLimitPlus = FromBoardPosition(swPos);
                        if (AXM.GetNegativePosition(AxisNo, ref swNeg) == 0)
                            setup.SoftLimitMinus = FromBoardPosition(swNeg);

                        // Soft limit Enable 플래그
                        try
                        {
                            uint swUse = 0;
                            if (AXM.GetSoftLimitEnable(AxisNo, ref swUse) == 0)
                                setup.SoftLimitEnabled = swUse != 0;
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Error, "AX-READ-SETUP", Name,
                                "SoftLimit enable read failed: " + ex.Message);
                        }

                        // Pulse out method (AXL 10 종류 → 프로젝트 PulseOutput 3 종류로 축약 매핑)
                        try
                        {
                            AXM.MotorOutputMethod outMethod = AXM.MotorOutputMethod.OneHighLowHigh;
                            if (AXM.GetOutputMethod(AxisNo, ref outMethod) == 0)
                            {
                                _rawPulseOutput = outMethod;
                                setup.PulseOutput = MapPulseOutput(outMethod);
                            }
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Error, "AX-READ-SETUP", Name,
                                "PulseOutput read failed: " + ex.Message);
                        }

                        // Encoder input method (AXL 8 종류 → 프로젝트 EncoderInput 3 종류로 축약 매핑)
                        try
                        {
                            AXM.EncoderInputMethod encMethod = AXM.EncoderInputMethod.ObverseUpDownMode;
                            if (AXM.GetEncoderMethod(AxisNo, ref encMethod) == 0)
                            {
                                _rawEncoderInput = encMethod;
                                setup.EncoderInput = MapEncoderInput(encMethod);
                            }
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Error, "AX-READ-SETUP", Name,
                                "EncoderInput read failed: " + ex.Message);
                        }

                        // Inposition level (Low/High만 매핑, Used/Unused는 보존)
                        try
                        {
                            ActiveLevel inpLvl = ActiveLevel.Low;
                            if (AXM.GetInPositionLevel(AxisNo, ref inpLvl) == 0)
                                setup.InPosition = inpLvl == ActiveLevel.High ? InPosition.High : InPosition.Low;
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Error, "AX-READ-SETUP", Name,
                                "InPosition level read failed: " + ex.Message);
                        }

                        // Stop mode (AXL: 0=EMG, 1=Slowdown) - 리밋 정지 모드 기준
                        try
                        {
                            uint stopRaw = 0;
                            if (AXM.GetLimitStopMode(AxisNo, ref stopRaw) == 0)
                                setup.StopMode = stopRaw == 0 ? StopMode.Emergency : StopMode.DecelStop;
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Error, "AX-READ-SETUP", Name,
                                "StopMode read failed: " + ex.Message);
                        }

                        // Emergency stop level
                        try
                        {
                            uint estopMode = 0;
                            ActiveLevel estopLvl = ActiveLevel.Low;
                            if (AXM.GetSignalStop(AxisNo, ref estopMode, ref estopLvl) == 0)
                                setup.EmergencyLevel = estopLvl;
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Error, "AX-READ-SETUP", Name,
                                "EmergencyLevel read failed: " + ex.Message);
                        }

                        // Profile mode (AXL 0~4 → AxisProfileMode 2종류로 축약: Trapezoid/SCurve)
                        try
                        {
                            uint profRaw = 0;
                            if (AXM.GetProfileModeRaw(AxisNo, ref profRaw) == 0)
                            {
                                _rawProfileMode = (AXT_MOTION_PROFILE_MODE)profRaw;
                                setup.ProfileMode = profRaw <= 1 ? AxisProfileMode.Trapezoid : AxisProfileMode.SCurve;
                            }
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Error, "AX-READ-SETUP", Name,
                                "ProfileMode read failed: " + ex.Message);
                        }

                        // Acc/Dec Jerk %
                        try
                        {
                            double accJerk = 0, decJerk = 0;
                            if (AXM.GetAccelerationJerk(AxisNo, ref accJerk) == 0)
                                setup.AccJerkPercent = (int)Math.Round(accJerk);
                            if (AXM.GetDecelerationJerk(AxisNo, ref decJerk) == 0)
                                setup.DecJerkPercent = (int)Math.Round(decJerk);
                        }
                        catch (Exception ex)
                        {
                            AlarmManager.Raise(AlarmSeverity.Error, "AX-READ-SETUP", Name,
                                "Jerk read failed: " + ex.Message);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-READ-SETUP",
                    Name,
                    "ReadSetupFromBoard failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 현재 축의 <see cref="AxisSetup"/> / <see cref="AxisConfig"/> 값을 보드에 기록한다.<br/>
        /// SaveSpeedRows / Apply 흐름에서 호출되어 모델 → 보드 방향 동기화를 보장한다.
        /// 시뮬레이션 모드이거나 보드가 닫혀 있으면 아무 일도 하지 않고 false 를 반환한다.
        /// </summary>
        /// <returns>적용 성공 여부.</returns>
        public bool WriteSetupToBoard()
        {
            // 현재 장비 테스트 중에는 Ajin 보드 Setup/Config Write를 전면 금지한다.
            // Speed/Axis 설정 저장은 메모리와 motion_axes.json까지만 반영해야 하며,
            // 아래 AXM.Set* 호출들은 보드 파라미터 검증 절차를 마친 뒤에만 다시 열어야 한다.
            if (BlockSetupWriteToBoard)
                return false;

            if (Config == null || Config.IsSimulationMode || !AjinSystem.IsOpen)
                return false;

            // 우선 보드에 Write하지말고 진행하자.
            if (ForceTestBoard == true)
            {
                return false;
            }

            try
            {
                AxisConfig c = Config;
                AxisSetup setup = Setup;

                lock (_sync)
                {
                    // Move unit / pulse. Ajin board unit is fixed to the internal control unit:
                    // length axes = mm, theta axes = deg. Setup.Unit is display-only.
                    if (setup != null && setup.PulsesPerUnit > 0)
                    {
                        try { AXM.SetMoveUnitPerPulse(AxisNo, 1, (int)System.Math.Round(setup.PulsesPerUnit)); }
                        catch (Exception ex) { LogWriteWarn("MoveUnitPerPulse", ex); }
                    }

                    // Max velocity
                    if (c.MaxVelocity > 0)
                    {
                        try { AXM.SetMaxVelocity(AxisNo, ToBoardVelocity(c.MaxVelocity)); }
                        catch (Exception ex) { LogWriteWarn("MaxVelocity", ex); }
                    }

                    // Home velocity / acceleration.
                    // During board bring-up tests, keep the home profile already applied in Ajin.
                    if (!ForceTestBoard)
                    {
                        try
                        {
                            AXM.SetHomeVelocity(AxisNo,
                                ToBoardVelocity(c.HomeFirstVelocity),
                                ToBoardVelocity(c.HomeSecondVelocity),
                                ToBoardVelocity(c.HomeLastVelocity),
                                ToBoardVelocity(c.HomeIndexSearchVelocity),
                                ToBoardAcceleration(c.HomeFirstAcceleration),
                                ToBoardAcceleration(c.HomeSecondAcceleration));
                        }
                        catch (Exception ex) { LogWriteWarn("HomeVelocity", ex); }
                    }

                    if (setup != null)
                    {
                        // Home method
                        try
                        {
                            // 현재 기준: Picker T PC HomeOffset은 보드에 쓰지 않고 홈 후 상대 이동으로만 적용한다.
                            double boardHomeOffset = ShouldApplyPickerThetaPcHomeOffset()
                                ? 0.0
                                : setup.HomeOffset;
                            AXM.SetHomeMethod(AxisNo,
                                setup.HomeDirection,
                                setup.HomeSignal,
                                HomeZPhase.None,
                                0.0,
                                ToBoardPosition(boardHomeOffset));
                        }
                        catch (Exception ex) { LogWriteWarn("HomeMethod", ex); }

                        // Signal levels
                        try { AXM.SetAmpEnableLevel(AxisNo, setup.ServoOnLevel); } catch (Exception ex) { LogWriteWarn("ServoOnLevel", ex); }
                        try { AXM.SetAmpFaultLevel(AxisNo, setup.AlarmLevel); } catch (Exception ex) { LogWriteWarn("AlarmLevel", ex); }
                        try { AXM.SetAmpResetLevel(AxisNo, setup.AlarmResetLevel); } catch (Exception ex) { LogWriteWarn("AlarmResetLevel", ex); }
                        try { AXM.SetPositiveLimitLevel(AxisNo, setup.PositiveLimitLevel); } catch (Exception ex) { LogWriteWarn("PositiveLimitLevel", ex); }
                        try { AXM.SetNegativeLimitLevel(AxisNo, setup.NegativeLimitLevel); } catch (Exception ex) { LogWriteWarn("NegativeLimitLevel", ex); }

                        // Soft limits (Use + Pos/Neg 통합 적용)
                        try { AXM.SetSoftLimits(AxisNo, setup.SoftLimitEnabled, ToBoardPosition(setup.SoftLimitPlus), ToBoardPosition(setup.SoftLimitMinus)); }
                        catch (Exception ex) { LogWriteWarn("SoftLimits", ex); }

                        // Pulse out / Encoder input (모델 enum → AXL enum 매핑, 라운드트립 시 raw 보존)
                        try
                        {
                            AXM.MotorOutputMethod outValue;
                            if (_rawPulseOutput.HasValue && MapPulseOutput(_rawPulseOutput.Value) == setup.PulseOutput)
                                outValue = _rawPulseOutput.Value;
                            else
                                outValue = MapPulseOutputToAxl(setup.PulseOutput);
                            AXM.SetOutputMethod(AxisNo, outValue);
                        }
                        catch (Exception ex) { LogWriteWarn("PulseOutput", ex); }
                        try
                        {
                            AXM.EncoderInputMethod encValue;
                            if (_rawEncoderInput.HasValue && MapEncoderInput(_rawEncoderInput.Value) == setup.EncoderInput)
                                encValue = _rawEncoderInput.Value;
                            else
                                encValue = MapEncoderInputToAxl(setup.EncoderInput);
                            AXM.SetEncoderMethod(AxisNo, encValue);
                        }
                        catch (Exception ex) { LogWriteWarn("EncoderInput", ex); }

                        // Inposition level (Low/High만 적용)
                        try
                        {
                            if (setup.InPosition == InPosition.Low || setup.InPosition == InPosition.High)
                                AXM.SetInPositionLevel(AxisNo, setup.InPosition);
                        }
                        catch (Exception ex) { LogWriteWarn("InPosition", ex); }

                        // Limit stop mode (Emergency=0, DecelStop=1)
                        try { AXM.SetLimitStopMode(AxisNo, setup.StopMode == StopMode.Emergency ? 0u : 1u); }
                        catch (Exception ex) { LogWriteWarn("LimitStopMode", ex); }

                        // Emergency stop signal level
                        try
                        {
                            uint estopMode = setup.StopMode == StopMode.Emergency ? 0u : 1u;
                            uint estopLvl = setup.EmergencyLevel == ActiveLevel.High ? 1u : 0u;
                            AXM.SetSignalStop(AxisNo, estopMode, estopLvl);
                        }
                        catch (Exception ex) { LogWriteWarn("EmergencyLevel", ex); }

                        // Profile mode (모델 2종 → AXL 5종 매핑, 라운드트립 시 raw 보존)
                        try
                        {
                            AXT_MOTION_PROFILE_MODE prof;
                            bool rawIsTrap = _rawProfileMode.HasValue && (uint)_rawProfileMode.Value <= 1;
                            bool modelIsTrap = setup.ProfileMode == AxisProfileMode.Trapezoid;
                            if (_rawProfileMode.HasValue && rawIsTrap == modelIsTrap)
                                prof = _rawProfileMode.Value;
                            else
                                prof = modelIsTrap
                                    ? AXT_MOTION_PROFILE_MODE.SYM_TRAPEZOIDE_MODE
                                    : AXT_MOTION_PROFILE_MODE.SYM_S_CURVE_MODE;
                            AXM.SetProfileMode(AxisNo, prof);
                        }
                        catch (Exception ex) { LogWriteWarn("ProfileMode", ex); }

                        // Acc/Dec Jerk %
                        try { AXM.SetAccelerationJerk(AxisNo, setup.AccJerkPercent); } catch (Exception ex) { LogWriteWarn("AccJerk", ex); }
                        try { AXM.SetDecelerationJerk(AxisNo, setup.DecJerkPercent); } catch (Exception ex) { LogWriteWarn("DecJerk", ex); }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "AX-WRITE-SETUP",
                    Name,
                    "WriteSetupToBoard failed: " + ex.Message);
                return false;
            }
        }

        private void LogWriteWarn(string field, Exception ex)
        {
            AlarmManager.Raise(AlarmSeverity.Error, "AX-WRITE-SETUP", Name,
                field + " write failed: " + ex.Message);
        }

        /// <summary>
        /// 프로젝트 PulseOutput(3 종) → AXL MotorOutputMethod(8 종) 역매핑.
        /// 모델은 정보가 적으므로 AXL 의 대표값을 선택한다.
        /// </summary>
        private static AXM.MotorOutputMethod MapPulseOutputToAxl(PulseOutput m)
        {
            switch (m)
            {
                case PulseOutput.TwoPulse_High_CCW_CW: return AXM.MotorOutputMethod.TwoCcwCwHigh;
                // 프로젝트 Low CCW/CW 출력을 AXL Low TwoPulse로 변환
                case PulseOutput.TwoPulse_Low_CCW_CW: return AXM.MotorOutputMethod.TwoCcwCwLow;
                default: return AXM.MotorOutputMethod.OneHighLowHigh; // AB_Phase 등은 1펄스 대표값
            }
        }

        /// <summary>
        /// 프로젝트 EncoderInput(3 종) → AXL EncoderInputMethod(8 종) 역매핑.
        /// </summary>
        private static AXM.EncoderInputMethod MapEncoderInputToAxl(EncoderInput m)
        {
            switch (m)
            {
                // 프로젝트 Normal 엔코더를 AXL 정방향 SQR4로 변환
                case EncoderInput.Normal: return AXM.EncoderInputMethod.ObverseSqr4Mode;
                // 프로젝트 Reverse SQR4 엔코더를 AXL 역방향 SQR4로 변환
                case EncoderInput.Reverse_SQR4: return AXM.EncoderInputMethod.ReverseSqr4Mode;
                default: return AXM.EncoderInputMethod.ReverseUpDownMode;
            }
        }

        /// <summary>
        /// AXL 의 펄스 출력 방식(10 종류)을 프로젝트 PulseOutput(3 종류)으로 축약 매핑한다.
        /// 1펄스(One*) 계열은 AB_Phase 가 가장 가깝고, TwoCwCcw* 계열은 보드 기본값 TwoPulse_High_CCW_CW 와 매칭.
        /// </summary>
        private static PulseOutput MapPulseOutput(AXM.MotorOutputMethod m)
        {
            switch (m)
            {
                // AXL High TwoPulse 계열을 프로젝트 High CCW/CW로 축약
                case AXM.MotorOutputMethod.TwoCcwCwHigh:
                case AXM.MotorOutputMethod.TwoCwCcwHigh:
                    return PulseOutput.TwoPulse_High_CCW_CW;
                // AXL Low TwoPulse 계열을 프로젝트 Low CCW/CW로 축약
                case AXM.MotorOutputMethod.TwoCcwCwLow:
                case AXM.MotorOutputMethod.TwoCwCcwLow:
                    return PulseOutput.TwoPulse_Low_CCW_CW;
                default:
                    return PulseOutput.AB_Phase;
            }
        }

        /// <summary>
        /// AXL 의 엔코더 입력 방식(8 종류)을 프로젝트 EncoderInput(3 종류)으로 축약 매핑한다.
        /// </summary>
        private static EncoderInput MapEncoderInput(AXM.EncoderInputMethod m)
        {
            switch (m)
            {
                // AXL 정방향 엔코더 계열을 프로젝트 Normal로 축약
                case AXM.EncoderInputMethod.ObverseUpDownMode:
                case AXM.EncoderInputMethod.ObverseSqr1Mode:
                case AXM.EncoderInputMethod.ObverseSqr2Mode:
                case AXM.EncoderInputMethod.ObverseSqr4Mode:
                    return EncoderInput.Normal;
                // AXL 역방향 SQR4를 프로젝트 Reverse_SQR4로 축약
                case AXM.EncoderInputMethod.ReverseSqr4Mode:
                    return EncoderInput.Reverse_SQR4;
                default:
                    return EncoderInput.Reverse;
            }
        }

        internal double ToBoardPosition(double nativePosition)
        {
            return nativePosition;
        }

        internal double FromBoardPosition(double boardPosition)
        {
            return boardPosition;
        }

        internal double ToBoardVelocity(double nativeVelocity)
        {
            if (ForceTestBoard && nativeVelocity != 0.0)
                return nativeVelocity < 0.0 ? -ForcedTestBoardVelocity : ForcedTestBoardVelocity;

            return nativeVelocity;
        }

        internal double FromBoardVelocity(double boardVelocity)
        {
            return boardVelocity;
        }

        internal double ToBoardAcceleration(double nativeAcceleration)
        {
            return nativeAcceleration;
        }

        internal double FromBoardAcceleration(double boardAcceleration)
        {
            return boardAcceleration;
        }

        private async Task<int> WaitUntilMoveDone(int motionStopSerial)
        {
            int guard = 0;
            bool detectedMotion = false;
            while (!IsAlarm)
            {
                UpdateStatus();
                if (IsMoving)
                    detectedMotion = true;

                if (detectedMotion && !IsMoving && IsInPosition)
                    break;

                if (!detectedMotion && guard > 20 && IsInPosition)
                    break;

                if (Volatile.Read(ref _motionStopSerial) != motionStopSerial && !IsMoving)
                    return -4;

                await Task.Delay(10).ConfigureAwait(false);
                if (++guard > 6000)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-MOVE-WAIT",
                        Name,
                        "Move wait timeout. AxisNo=" + AxisNo);
                    UpdateStatus();
                    return -3;
                }
            }

            UpdateStatus();
            return IsAlarm ? (int)AlarmCode : 0;
        }
    }
}

