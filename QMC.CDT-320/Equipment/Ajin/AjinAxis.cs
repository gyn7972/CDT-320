using QMC.Common.Motion;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion.Ajin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Motion.SharedRailX;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Ajin
{
    public class AjinAxis : BaseAxis
    {
        #region 공통 상태 및 초기화 리밋 검색

        private static readonly bool ForceTestBoard = false;
        private static readonly bool BlockSetupWriteToBoard = true;
        private const double ForcedTestBoardVelocity = 20.0;
        private const double SharedRailXInitializeSoftLimitOverrunMm = 5.0;

        private readonly object _sync = new object();
        private static int _sharedRailXHomeSearchCount;
        private int _motionDirection;
        private bool _isHomeSearching;
        private int _initializeHomePreparationActive;
        private int _motionStopSerial;
        // 이 축의 위치 오버라이드(리다이렉트) 성공 횟수. MoveAbsoluteAsync가 자신의 이동 중
        // 오버라이드가 있었는지 판정해 마지막 Command↔Target 확인(-5)을 건너뛰는 데 쓴다.
        private int _positionOverrideSerial;
        // 이 축이 마지막으로 시작한 모션의 오버라이드 기준 시리얼(AXM.MovePosition이 발급).
        // TryOverridePosition이 AXM.ModifyPosition에 넘겨 "자기 모션의 기준"으로만 오버라이드하고,
        // ApplyReadStatus가 모션 종료(하강 전이) 시 이 시리얼의 기록만 조건부 무효화한다.
        // 스테일 기준(예: 원점 직후의 0)으로 절대값이 상대값처럼 보드에 나가던 결함의 차단 장치
        // (실장비 2026-07-25, InputVisionX startBase=0 / 사용자 승인 2026-07-25).
        private long _motionStartBaseSerial = -1L;
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

        public override double ActualPosition {
            get
            {
                double pos = base.ActualPosition;
                AXM.GetCommandPosition(AxisNo, ref pos);
                return pos;
            }
            protected set => base.ActualPosition = value; }
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
            CancellationToken cancellationToken,
            bool allowMotion = true)
        {
            int searchDirection = direction < 0 ? -1 : 1;
            bool useBoundedSoftLimitBypass = IsFeederVisionRetreatAxis() &&
                Setup != null && Setup.SoftLimitEnabled;
            double softLimitSearchBoundary = useBoundedSoftLimitBypass
                ? (searchDirection < 0
                    ? Setup.SoftLimitMinus - SharedRailXInitializeSoftLimitOverrunMm
                    : Setup.SoftLimitPlus + SharedRailXInitializeSoftLimitOverrunMm)
                : 0.0;
            bool completed = false;
            try
            {
                if (timeoutMs <= 0)
                    timeoutMs = 30000;

                cancellationToken.ThrowIfCancellationRequested();
                if (UseSimulation)
                {
                    if (!IsServoOn || IsAlarm)
                        return FailAjinAxisNotReady("INITIALIZE LIMIT SEARCH", 0.0, false);

                    Volatile.Write(ref _hardwareLimitSearchDirection, searchDirection);
                    if (IsTargetHardwareLimitActive(searchDirection))
                    {
                        completed = true;
                        return 0;
                    }
                    if (!allowMotion)
                    {
                        return FailMotion(
                            -14,
                            "INITIALIZE LIMIT SEARCH",
                            "현재 목표 하드리밋이 OFF이므로 무이동 확인에 실패했습니다.",
                            0.0,
                            false);
                    }

                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    if (searchDirection < 0)
                        Sensor_MEL = true;
                    else
                        Sensor_PEL = true;
                    completed = true;
                    return 0;
                }

                if (!AjinSystem.IsOpen)
                    return FailMotion(-2, "INITIALIZE LIMIT SEARCH", "AXL is not open.", 0.0, false);

                // 초기화가 의도한 외측 Limit은 일반 LIMIT-HIT로 기록되면 안 됩니다.
                // 상태를 갱신하기 전에 방향을 먼저 등록하고, 실패 시 finally에서 해제합니다.
                Volatile.Write(ref _hardwareLimitSearchDirection, searchDirection);
                UpdateStatus();
                if (!IsServoOn)
                    return FailMotion(-2, "INITIALIZE LIMIT SEARCH", "Servo is OFF.", 0.0, false);
                if (IsAlarm && !IsExpectedHardwareLimitAlarm(searchDirection))
                    return FailAjinAxisNotReady("INITIALIZE LIMIT SEARCH", 0.0, false);

                ClearExpectedHardwareLimitAlarm(searchDirection);
                if (IsTargetHardwareLimitActive(searchDirection))
                {
                    completed = true;
                    return 0;
                }

                if (IsOppositeHardwareLimitActive(searchDirection))
                    return FailMotion(-12, "INITIALIZE LIMIT SEARCH", "Opposite hardware limit is active.", 0.0, false);

                if (!allowMotion)
                {
                    return FailMotion(
                        -14,
                        "INITIALIZE LIMIT SEARCH",
                        "현재 목표 하드리밋이 OFF이므로 무이동 확인에 실패했습니다.",
                        0.0,
                        false);
                }

                double safeVelocity = velocity > 0.0
                    ? Math.Abs(velocity)
                    : Math.Abs(Config != null ? Config.JogFineVelocity : 1.0);
                double signedVelocity = searchDirection * Math.Max(0.000001, safeVelocity);
                int motionStopSerial = Volatile.Read(ref _motionStopSerial);

                cancellationToken.ThrowIfCancellationRequested();
                CurrentVelocity = signedVelocity;
                IsMoving = true;
                IsInPosition = false;
                _motionDirection = searchDirection;

                int ret;
                lock (_sync)
                {
                    // 반대 Lane의 Stop이 먼저 완료됐다면 보드 이동 명령을 새로 발행하지 않습니다.
                    cancellationToken.ThrowIfCancellationRequested();
                    ret = AXM.MoveVelocity(
                        AxisNo,
                        ToBoardVelocity(signedVelocity),
                        ToBoardAcceleration(ResolveJogAcceleration()),
                        ToBoardAcceleration(ResolveJogDeceleration()));
                }
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

                    // Input/Output Vision X 초기화에서만 목표 방향 SoftLimit 통과를 허용합니다.
                    // 목표 하드리밋 센서가 고장 나도 5 mm를 넘어서 계속 이동하지 않도록 제한합니다.
                    double actualPosition = base.ActualPosition;
                    if (useBoundedSoftLimitBypass &&
                        ((searchDirection < 0 && actualPosition < softLimitSearchBoundary) ||
                         (searchDirection > 0 && actualPosition > softLimitSearchBoundary)))
                    {
                        Stop();
                        return FailMotion(
                            -15,
                            "INITIALIZE LIMIT SEARCH",
                            "목표 하드리밋을 SoftLimit 초과 허용 범위 안에서 감지하지 못했습니다. " +
                            "direction=" + searchDirection +
                            ", position=" + actualPosition.ToString("0.###") +
                            ", boundary=" + softLimitSearchBoundary.ToString("0.###"),
                            0.0,
                            false);
                    }

                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// 기존 Step Jog를 재사용해 감지된 하드리밋의 반대 방향으로만 이탈합니다.
        /// 좌표가 유실된 SharedRail 초기화이므로 이동 전후 실제 엔코더와 Limit OFF를 별도로 확인합니다.
        /// </summary>
        internal async Task<int> BackOffHardwareLimitForInitializeAsync(
            int searchedDirection,
            double distance,
            double velocity,
            CancellationToken cancellationToken)
        {
            int searchDirection = searchedDirection < 0 ? -1 : 1;
            double safeDistance = Math.Abs(distance);
            double startActual = ActualPosition;
            bool startPel = Sensor_PEL;
            bool startMel = Sensor_MEL;
            int readError = 0;

            try
            {
                if (Volatile.Read(ref _hardwareLimitSearchDirection) != searchDirection ||
                    safeDistance <= 0.0)
                {
                    return FailMotion(
                        -14,
                        "INITIALIZE LIMIT BACKOFF",
                        "활성 하드리밋 탐색 또는 이탈 거리가 올바르지 않습니다.",
                        0.0,
                        false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!UseSimulation &&
                    !TryReadInitializeHardwareFeedback(
                        out startActual,
                        out startPel,
                        out startMel,
                        out readError))
                {
                    return FailMotion(
                        readError,
                        "INITIALIZE LIMIT BACKOFF",
                        "AJIN 실제 위치/리밋 조회에 실패했습니다.",
                        0.0,
                        false);
                }

                bool targetLimitOn = searchDirection < 0 ? startMel : startPel;
                bool oppositeLimitOn = searchDirection < 0 ? startPel : startMel;
                if (!targetLimitOn || oppositeLimitOn)
                {
                    return FailMotion(
                        -14,
                        "INITIALIZE LIMIT BACKOFF",
                        "이탈 시작 전 목표 하드리밋 상태가 올바르지 않습니다.",
                        0.0,
                        false);
                }

                int result;
                // To do: [원점복귀 리밋 이탈] 이탈 이동 구간에만 소프트리밋 목표 검사 면제 스코프를 씌운다.
                // 기존 조건: 스코프 없이 이동해 이탈 목표가 소프트리밋 밖이면 AX-SOFT-LIMIT-N으로 거부됐다.
                using (BaseAxis.BeginInitializeLimitBackoffScope())
                {
                    if (UseSimulation)
                    {
                        result = await MoveJogStepAsync(
                            -searchDirection,
                            JogSpeedType.Custom,
                            safeDistance,
                            velocity).ConfigureAwait(false);
                    }
                    else
                    {
                        // 좌표 기반 SharedRail 재배치만 건너뛰고, MoveJogStep 내부의 일반 MotionGuard는 그대로 확인합니다.
                        using (SharedRailXMotionRuntime.EnterInternalDispatch())
                        {
                            result = await MoveJogStepAsync(
                                -searchDirection,
                                JogSpeedType.Custom,
                                safeDistance,
                                velocity).ConfigureAwait(false);
                        }
                    }
                }

                if (result != 0)
                    return result;

                if (UseSimulation)
                {
                    if (searchDirection < 0)
                        Sensor_MEL = false;
                    else
                        Sensor_PEL = false;
                }

                double endActual = ActualPosition;
                bool endPel = Sensor_PEL;
                bool endMel = Sensor_MEL;
                if (!UseSimulation &&
                    !TryReadInitializeHardwareFeedback(
                        out endActual,
                        out endPel,
                        out endMel,
                        out readError))
                {
                    return FailMotion(
                        readError,
                        "INITIALIZE LIMIT BACKOFF",
                        "이탈 후 AJIN 실제 위치/리밋 조회에 실패했습니다.",
                        0.0,
                        false);
                }

                UpdateStatus();
                double expectedDelta = -searchDirection * safeDistance;
                double actualDelta = endActual - startActual;
                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.01;
                bool limitReleased = searchDirection < 0 ? !endMel : !endPel;

                // To do: [원점복귀 리밋 이탈] 판정 기준을 "이탈 확인"으로 되돌린다.
                // 기존 조건: Math.Abs(actualDelta - expectedDelta) > tolerance  (tolerance = InPositionTolerance 0.01)
                //            && returnedInsideSoftLimit
                //            → 5mm 상대이동에 In-Position용 0.01mm 정밀도를 요구해, 감속·서보 잔차 0.055mm에도
                //              실패했다(2026-08-05 actualDelta=4.945). 소프트리밋 안쪽 복귀 요구도 홈 완료 위치가
                //              소프트리밋 밖인 축에서는 영구 실패였다.
                // 현재 기준: 이 단계의 목적은 하드리밋에서 빠져나왔는지 확인하는 것이다.
                //            리밋 해제 + 이탈 방향으로 실제 이동했는지만 본다(이동량 정밀도는 요구하지 않는다).
                bool movedAwayFromLimit = -searchDirection > 0 ? actualDelta > 0.0 : actualDelta < 0.0;
                if (!limitReleased || IsMoving || !IsInPosition || !IsServoOn || IsAlarm ||
                    !movedAwayFromLimit)
                {
                    return FailMotion(
                        -14,
                        "INITIALIZE LIMIT BACKOFF",
                        "5mm 이탈 실측 확인 실패. actualDelta=" +
                        actualDelta.ToString("0.###") +
                        ", expectedDelta=" + expectedDelta.ToString("0.###") +
                        ", limitReleased=" + limitReleased +
                        ", moving=" + IsMoving +
                        ", inPosition=" + IsInPosition +
                        ", servo=" + IsServoOn +
                        ", alarm=" + IsAlarm +
                        ", movedAwayFromLimit=" + movedAwayFromLimit +
                        ", startActual=" + startActual.ToString("0.###") +
                        ", endActual=" + endActual.ToString("0.###"),
                        0.0,
                        false);
                }

                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    "InitializeLimitBackoff",
                    "Vision X 하드리밋 이탈 완료. axis=" + Name +
                    ", actualDelta=" + actualDelta.ToString("0.###") +
                    ", limitReleased=True - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                Stop();
                return FailMotion(
                    -4,
                    "INITIALIZE LIMIT BACKOFF",
                    "하드리밋 이탈이 취소되었습니다.",
                    0.0,
                    false);
            }
            catch (Exception ex)
            {
                Stop();
                return FailMotion(-1, "INITIALIZE LIMIT BACKOFF", ex.Message, 0.0, false);
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
            int searchDirection = Interlocked.Exchange(ref _hardwareLimitSearchDirection, 0);
            if (!UseSimulation)
                return;

            // Simulation Limit는 검색 함수가 만든 합성값이므로 다음 초기화에 남기지 않습니다.
            if (searchDirection < 0)
                Sensor_MEL = false;
            else if (searchDirection > 0)
                Sensor_PEL = false;
        }

        /// <summary>
        /// 하드리밋 이탈 전후에 필요한 실제 엔코더와 MEL/PEL만 보드에서 직접 읽습니다.
        /// </summary>
        internal bool TryReadInitializeHardwareFeedback(
            out double actualPosition,
            out bool sensorPel,
            out bool sensorMel,
            out int errorCode)
        {
            actualPosition = 0.0;
            sensorPel = false;
            sensorMel = false;
            errorCode = AjinSystem.IsOpen ? 0 : -2;
            if (errorCode != 0)
                return false;

            lock (_sync)
            {
                errorCode = AXM.GetActualPosition(AxisNo, ref actualPosition);
                if (errorCode == 0)
                    errorCode = AXM.GetPositiveLimitValue(AxisNo, ref sensorPel);
                if (errorCode == 0)
                    errorCode = AXM.GetNegativeLimitValue(AxisNo, ref sensorMel);
            }

            actualPosition = FromBoardPosition(actualPosition);
            return errorCode == 0;
        }

        internal void BeginInitializeHomePreparation()
        {
            Interlocked.Exchange(ref _initializeHomePreparationActive, 1);
        }

        internal void EndInitializeHomePreparation()
        {
            Interlocked.Exchange(ref _initializeHomePreparationActive, 0);
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

        private bool IsFeederVisionRetreatAxis()
        {
            return string.Equals(Name, "InputVisionX", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Name, "OutputVisionX", StringComparison.OrdinalIgnoreCase);
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

        #endregion

        #region 생성 및 이동 오버라이드

        public AjinAxis(string name, int axisNo) : base(name)
        {
            AxisNo = axisNo;
            Config.IsSimulationMode = false;
        }

        /// <summary>
        /// 구동 중인 축의 목표 위치를 오버라이드한다.
        /// targetName: MotionGuard 존 판정에 쓰이는 이동 의도 문자열.
        ///   기존 조건: "PositionOverride" 고정 문자열을 넘겨, 인코더 존이 설정된 축에서
        ///             목표 존을 판단할 수 없어(Unknown) 팔로잉 오버라이드가 -11로 차단됐다
        ///             (실장비 2026-07-25 19:36, RearPickerX target=687.786).
        ///   현재 기준(사용자 승인 2026-07-25, A안): 호출자가 최종 목표의 존 의도를 담은
        ///             targetName을 전달한다. 미지정(null/빈문자)이면 기존 "PositionOverride"로 폴백해
        ///             기존 호출부 동작을 유지한다.
        /// </summary>
        public int TryOverridePosition(
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration,
            string targetName = null)
        {
            try
            {
                // 존 판정용 이동 의도. 미지정이면 기존 동작(폴백)을 유지한다.
                string guardTargetName = string.IsNullOrWhiteSpace(targetName)
                    ? "PositionOverride"
                    : targetName;

                if (UseSimulation)
                {
                    if (!IsMoving)
                        return -4;

                    // 기존 조건: VerifyAxisTeachingMove — 차단 시 INTERLOCK 알람(Error)이 올라가
                    //           간섭그룹 비상정지+전 시퀀스 취소로 승격되어, 팔로잉의 R6 폴백(일반
                    //           이동 재시도)이 실행될 기회가 없었다(실장비 2026-07-25 19:36).
                    // 현재 기준(사용자 지시 2026-07-25): 동일 규칙을 평가하되 알람을 올리지 않는
                    //           CanAxisTeachingMove(dry-run 판정)로 교체한다. 거부 조건은 동일하고
                    //           -11만 조용히 반환해 호출자 폴백에 맡긴다. 일반 이동 경로의 알람
                    //           승격은 무변경. 차단 사유는 여기서 Motion 로그로 남긴다(차단 시
                    //           follow가 즉시 중단되므로 폴링 폭주 없음).
                    string simulationGuardReason;
                    if (!MotionGuardRuntime.CanAxisPositionOverride(
                        this,
                        targetPosition,
                        guardTargetName,
                        out simulationGuardReason))
                    {
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-OVERRIDE-GUARD",
                            Name + " 위치 오버라이드가 인터락으로 거부되었습니다(알람 승격 없음, 폴백 위임). " +
                            "target=" + targetPosition.ToString("F6") +
                            ", targetName=" + guardTargetName +
                            ", reason=" + simulationGuardReason + " - Check");
                        return -11;
                    }

                    base.OverridePosition(targetPosition);
                    if (velocity > 0.0)
                        base.OverrideVelocity(velocity);
                    System.Threading.Interlocked.Increment(ref _positionOverrideSerial);
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

                // 현재 기준(사용자 지시 2026-07-25): 존 판정 생략 + 알람 미발생 판정 — 위 시뮬 경로 주석 참조.
                string guardReason;
                if (!MotionGuardRuntime.CanAxisPositionOverride(
                    this,
                    targetPosition,
                    guardTargetName,
                    out guardReason))
                {
                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-OVERRIDE-GUARD",
                        Name + " 위치 오버라이드가 인터락으로 거부되었습니다(알람 승격 없음, 폴백 위임). " +
                        "target=" + targetPosition.ToString("F6") +
                        ", targetName=" + guardTargetName +
                        ", reason=" + guardReason + " - Check");
                    return -11;
                }

                int limitCheck = CheckSoftLimitTarget(targetPosition);
                if (limitCheck != 0)
                    return limitCheck;

                double safeVelocity = velocity > 0.0
                    ? velocity
                    : Config.GetDefaultVel();
                double safeAcceleration = acceleration > 0.0
                    ? acceleration
                    : Config.GetDefaultAcc();
                double safeDeceleration = deceleration > 0.0
                    ? deceleration
                    : Config.GetDefaultDec();

                int ret;
                lock (_sync)
                {
                    // 자기 모션의 기준으로만 오버라이드한다 — 시리얼 불일치(다른 모션의 스테일
                    // 기준)는 AXM.ModifyPosition이 -2로 거부하고 호출자 폴백에 맡긴다.
                    ret = AXM.ModifyPosition(
                        AxisNo,
                        ToBoardPosition(targetPosition),
                        ToBoardVelocity(safeVelocity),
                        ToBoardAcceleration(safeAcceleration),
                        ToBoardAcceleration(safeDeceleration),
                        Volatile.Read(ref _motionStartBaseSerial));
                }

                if (ret != 0)
                {
                    return FailMotion(
                        ret,
                        "POSITION OVERRIDE",
                        "AXM 위치 오버라이드 명령이 실패했습니다. ret=0x" + ret.ToString("X4") +
                        ", targetName=" + guardTargetName,
                        targetPosition,
                        true);
                }

                base.OverridePosition(targetPosition);
                System.Threading.Interlocked.Increment(ref _positionOverrideSerial);
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
                    : Config.GetDefaultVel();
                double safeAcceleration = acceleration > 0.0
                    ? acceleration
                    : Config.GetDefaultAcc();
                double safeDeceleration = deceleration > 0.0
                    ? deceleration
                    : Config.GetDefaultDec();

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

        #endregion

        #region 팔로잉 이동 (FollowMove)

        // 팔로잉 안전거리 하한. safetyGap 인자가 이 값보다 작으면 이 값으로 클램프한다.
        // 팔로잉 유지 간격 하한. 기존 40.0 고정은 설정(SafetyDistance+Extra, UI 튜닝)이 40 미만일 때
        // 이를 무력화했다 — 현재 기준(사용자 지시 2026-07-26): 간격은 설정값을 그대로 존중하고,
        // 여기는 설정 오류(0/음수 등 퇴화값) 방어용 최소 바닥만 남긴다.
        private const double MinimumFollowSafetyGap = 5.0;
        // 팔로잉 이동 전체 타임아웃(고정). 팔로잉 루프와 최종 완료 대기를 합쳐 적용한다.
        private const int FollowMoveTimeoutMs = 5000;
        // TEST 임시 기준(사용자 승인 2026-07-27): 전 축 일반 이동 완료 기본 timeout 300초.
        // 현장 TEST 완료 후 거리/속도 기반 timeout으로 재조정한다.
        private const int DefaultAxisMoveTimeoutMs = 300000;
        // 팔로잉 루프 폴링 주기.
        private const int FollowMovePollIntervalMs = 10;
        // 타임아웃 전용 에러코드.
        private const int FollowMoveTimeoutErrorCode = -21;
        // 선행축 알람 전용 에러코드.
        private const int FollowMoveLeadingAlarmErrorCode = -22;
        // 백그라운드 이동 조기 종료 전용 에러코드(재설계 2026-07-27): 최초 이동 Task가 최종
        // 오버라이드 발행 전에 끝나 축이 중간 좌표에 정지한 상태 — 이후 오버라이드가 전부
        // -4(정지 경합)가 되는 교착이므로, 내부 재발행 없이 즉시 실패해 호출자 폴백(R6)에
        // 위임한다(사용자 확정 2026-07-27, Q2=①).
        private const int FollowMoveEarlyStopErrorCode = -23;

        // 규칙 2(2026-07-25): 팔로잉 명령은 속도·가속·감속을 한 세트로 명시 전달한다.
        // 기존 조건: MoveAbsoluteAsync(command, velocity) 2인자 호출 — 가감속 스케일 여부를
        //   MatchesDefaultVelocityScale 추론에 맡겼고, followVel=Min(선행,후행)은 후행축의 스케일
        //   DefaultVelocity와 일치하지 않아 가감속이 Config 원본 100%로 나갔다(실장비 폭주 원인).
        // 현재 기준(정정 2026-07-26): 명시 가감속은 BaseAxis.BeginExplicitMotionProfileScope
        //   (AsyncLocal)로 전달한다 — Config 임시 치환(DefaultVelocity=0)은 다른 스레드의 팔로잉
        //   속도 계산이 0을 읽는 경합으로 실장비 사고(22:08)를 내 폐기했다. 스코프 활성 이동은
        //   기본속도 추론 없이 전달 가감속을 그대로 쓴다(S² 차단 동일 보장).
        //   acc/dec는 호출 전에 이미 MotionSpeedScale을 경유한 값이어야 한다.
        //   이 헬퍼는 FollowMoveAsync 전용이며 다른 곳에서 호출하지 않는다.
        //   velocity<=0이면 스코프 없이 기존 폴백(축 레이어 단일 스케일)에 위임한다.
        /// <summary>
        /// 팔로잉 최초 이동 명령. 명시 가감속을 Config 임시 치환으로 전달하고,
        /// MotionGuard 존 판정용 targetName을 AxisTeachingMove 스코프로 전달한다.
        /// 기존 조건: MoveAbsoluteAsync에 targetName 파라미터가 없고 스코프도 열지 않아
        ///           request.TargetName이 빈 문자열이 됐다. 중간 세그먼트 좌표는 티칭 존 밴드 밖이라
        ///           위치 기반 존 판정도 Unknown이 되어 "Manual X 목표 존을 판단할 수 없습니다"로
        ///           차단됐다(실장비 2026-07-25, RearPickerX target=567.785, targetName=빈문자).
        /// 현재 기준(사용자 승인 2026-07-25, B′-1): MotionGuardRuntime.BeginAxisTeachingMove로
        ///           targetName을 전달한다. 스코프 좌표는 MotionGuardRuntime.IsMatchingScope(:529)가
        ///           좌표 일치(±0.0001)를 요구하므로 반드시 "그 호출의 명령 좌표(targetPosition)"를
        ///           쓴다. 최종 목표를 넣으면 매칭이 실패해 targetName이 다시 사라진다.
        ///           최종 목표의 존 의도는 BuildFollowEntryTargetName이 targetName에 담아 준
        ///           PickerZone= 토큰으로 전달되므로, 판정 결과는 최종 목표 기준과 같다.
        ///           중간 좌표의 실제 안전성은 SharedRailX 페어 간격/Y 대향 거리 등 위치 기반 검증이
        ///           그대로 담당한다.
        /// </summary>
        private async Task<int> MoveAbsoluteForFollowAsync(
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration,
            string targetName)
        {
            // 기존 조건: 최초 이동 검증을 MoveAbsoluteAsync 내부 VerifyAxisMove에만 맡겼다 — 차단 시
            //           MotionGuardRuntime이 즉시 INTERLOCK 알람(Critical)을 올려, 설계 의도였던
            //           "-11 → 호출자 폴백(대기+일반 이동)"이 받기 전에 장비 전체가 정지했다
            //           (실장비 2026-07-29 02:19/15:29, 2026-07-30 재발 — 팔로잉 첫 명령 거부 Critical).
            // 현재 기준(사용자 승인 2026-07-30): 발행 전에 조용한 Can 검사로 선확인하고, 차단이면 알람 없이
            //           -11을 반환해 폴백에 위임한다. 통과 후 MoveAbsoluteAsync 내부 Verify는 백스톱으로 유지
            //           (선확인~발행 사이 극소 레이스만 기존 알람 경로로 남는다).
            string quietGuardReason;
            if (!MotionGuardRuntime.CanAxisTeachingMove(this, targetPosition, targetName, out quietGuardReason))
            {
                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-FIRST",
                    Name + " 팔로잉 최초 이동이 MotionGuard에 차단되어 알람 없이 -11로 폴백에 위임합니다. " +
                    "target=" + targetPosition.ToString("0.###") +
                    ", reason=" + quietGuardReason + " - Check");
                return FailMotion(-11, "FOLLOW FIRST MOVE", quietGuardReason, targetPosition, true);
            }

            // [정정 2026-07-26] Config 임시 치환(DefaultVelocity=0) 폐기 — 공유 Config를 다른
            // 스레드(팔로잉 속도 계산 등)가 읽어 0이 관측되는 경합이 실장비 사고를 냈다(22:08).
            // 명시 가감속은 AsyncLocal 스코프로 전달하고 Config는 절대 변형하지 않는다.
            bool useExplicitMotion = Config != null && velocity > 0.0 && acceleration > 0.0 && deceleration > 0.0;
            IDisposable profileScope = useExplicitMotion
                ? BaseAxis.BeginExplicitMotionProfileScope(acceleration, deceleration)
                : null;
            try
            {
                // targetName이 없으면 스코프를 열지 않는다(기존 동작 유지).
                // 빈 스코프를 열면 IsMatchingScope가 성립해 빈 targetName이 TeachingMove로 전달되어
                // 현재의 VerifyAxisMove 경로와 달라진다.
                if (string.IsNullOrWhiteSpace(targetName))
                    return await MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);

                using (MotionGuardRuntime.BeginAxisTeachingMove(this, targetPosition, targetName))
                    return await MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);
            }
            finally
            {
                if (profileScope != null)
                    profileScope.Dispose();
            }
        }

        /// <summary>
        /// 팔로잉 중 후행축 위치를 함께 제한하는 "추가 제약 페어" 1건.
        /// 선행축(leadingAxis)이 아닌 다른 공유 레일 축(예: 반대편 피커)과의 페어 간격을 뜻한다.
        /// Direction은 해당 페어에서 후행축의 접근 부호(TowardSign)이며, 세션 direction과 다르면
        /// 그 페어는 이 이동으로 오히려 안전해지므로 클램프에서 제외한다.
        /// </summary>
        public sealed class FollowConstraint
        {
            public readonly BaseAxis Axis;
            public readonly double HomeGap;
            public readonly double SafetyGap;
            public readonly int Direction;

            public FollowConstraint(BaseAxis axis, double homeGap, double safetyGap, int direction)
            {
                Axis = axis;
                HomeGap = homeGap;
                SafetyGap = safetyGap;
                Direction = direction;
            }
        }

        // 기존 조건(~2026-07-26): 팔로잉은 leadingAxis 단 하나의 페어로만 전진 한계를 계산했다.
        //   반대편 피커는 이 식에 등장하지 않아 유지 간격(safetyGap=Safety+Extra)이 걸리지 않았고,
        //   명령마다 걸리는 인터락(페어 SafetyDistance, R5로 Extra 미포함)만이 유일한 하한이었다.
        //   그 결과 비전은 반대편 피커 앞 SafetyDistance(10mm)까지 파고든 뒤 -11로 정지·주차했다.
        // 현재 기준(사용자 승인 2026-07-26, 2안): 매 폴링마다 모든 제약 페어의 상한을 계산해
        //   가장 불리한 값으로 명령을 클램프한다. 선행축은 속도 프로파일 산출용으로만 쓰고,
        //   위치 한계는 관련 페어 전부가 건다. additionalConstraints 미전달 시 기존 동작 무변경.
        private double ClampFollowCommandByConstraints(
            double command,
            int direction,
            IList<FollowConstraint> constraints,
            out string bindingDetail)
        {
            bindingDetail = null;
            if (constraints == null || constraints.Count == 0)
                return command;

            double clamped = command;
            for (int i = 0; i < constraints.Count; i++)
            {
                FollowConstraint constraint = constraints[i];
                if (constraint == null || constraint.Axis == null)
                    continue;
                if (constraint.Direction != direction)
                    continue;

                double otherActual = constraint.Axis.ActualPosition;
                // 페어 간격식과 동일: direction>0 → 후행 ≤ 상대+homeGap−safetyGap
                //                     direction<0 → 후행 ≥ 상대−homeGap+safetyGap
                double bound = direction > 0
                    ? otherActual + constraint.HomeGap - constraint.SafetyGap
                    : otherActual - constraint.HomeGap + constraint.SafetyGap;

                bool binds = direction > 0 ? bound < clamped : bound > clamped;
                if (binds)
                {
                    clamped = bound;
                    bindingDetail = constraint.Axis.Name +
                        " actual=" + otherActual.ToString("F3") +
                        ", homeGap=" + constraint.HomeGap.ToString("F3") +
                        ", safetyGap=" + constraint.SafetyGap.ToString("F3") +
                        ", bound=" + bound.ToString("F3");
                }
            }

            return clamped;
        }

        /// <summary>
        /// 선행축을 따라가며 후행축(this)을 목표 위치까지 이동시킨다.
        /// [전면 재설계 2026-07-27, 사용자 승인]
        /// 기존 조건: 매 폴링마다 후행축 ActualPosition을 읽어 gap/slack(여유)을 계산하고,
        ///           !IsMoving이면 신규 이동을 재발행했다 — 보드 InMotion 반영 지연(이 파일의
        ///           시작 유예 5초로 인정된 특성) 동안 재진입해 최초 이동이 중복 발행되고
        ///           (2번째부터는 무로그), 이전 moveTask가 고아가 되어 LastMotionFailureMessage를
        ///           오염시켰으며, SharedRailX AutoMoveGuard가 중첩 기동됐다. 또한 경계식
        ///           intermediate = trailingActual + slack은 수학적으로 trailingActual이 소거되는
        ///           식이라, 이중 읽기 시 (A2-A1) 오염항이 경계에 유입될 구조적 위험이 있었다.
        /// 현재 기준(사용자 정의 2026-07-27):
        ///   ① 경계는 선행축 실측만의 함수 — bound = 선행Actual ± (homeGap − safetyGap) 부호식.
        ///      후행축 자기 위치는 팔로잉 계산에 사용하지 않는다(진입 검증에서만 1회 읽음).
        ///   ② 최초 이동 명령은 팔로잉당 정확히 1회(래치). 이후는 위치 오버라이드만 발행한다.
        ///   ③ lastCommanded 시드는 최초 이동의 명령 좌표. 새 command가 lastCommanded 대비
        ///      진행 방향으로 전진일 때만 오버라이드를 발행한다(역방향/무변화 발행 금지).
        ///   ④ 후행축은 선행축보다 속도·가속·감속이 클 수 없다 — 전 구간 Min(선행,후행)
        ///      단일 프로파일. 최종 구간 증속(TryOverrideVelocity) 폐지.
        ///   ⑤ command가 최종 목표와 일치하면 그 발행을 끝으로 오버라이드를 영구 중단하고,
        ///      모션 완료(WaitMoveCompleteAsync)까지 대기 후 리턴한다.
        ///   ⑥ 최초 이동 Task가 최종 발행 전에 끝나면(-4 무한 재시도 교착) -23으로 즉시
        ///      실패해 호출자 폴백(R6)에 위임한다 — 함수 내부 재발행 없음.
        /// 선행축에는 어떤 명령도 내리지 않는다(읽기 전용 — ActualPosition/IsAlarm만 참조).
        /// additionalConstraints를 주면 선행축 외 페어(반대편 피커 등)의 상한으로도 명령을 클램프한다.
        /// 반환: 0=성공, -1=인자 오류, -2=축 미준비, -11=인터락 거부,
        /// -21=타임아웃(timeoutMs 미지정 시 기본 5초, 발행+도달 전체 예산), -22=선행축 알람,
        /// -23=백그라운드 이동 조기 종료, 그 외=하위 에러코드.
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
            int timeoutMs = 0,
            string trailingTargetName = null,
            CancellationToken ct = default(CancellationToken),
            // 2안(사용자 승인 2026-07-26): 선행축 외 제약 페어. 기존 호출부는 미전달 → 동작 무변경.
            IList<FollowConstraint> additionalConstraints = null)
        {
            Task<int> moveTask = null;
            // R1(follow-entry): 타임아웃 인자화 — 0 이하면 기존 기본값(5000ms) 유지, 기존 호출부 무변경.
            int effectiveTimeoutMs = timeoutMs > 0 ? timeoutMs : FollowMoveTimeoutMs;

            try
            {
                // ---- Phase 0. 진입 검증 — 후행축 ActualPosition을 읽는 유일한 구간 ----
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
                double entryActual = ActualPosition;
                if (Math.Abs(entryActual - trailingTargetPosition) <= tolerance && !IsMoving)
                {
                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                        Name + " 팔로잉 진입 시 이미 목표 위치입니다(무발행 종료). target=" + trailingTargetPosition.ToString("F3") +
                        ", actual=" + entryActual.ToString("F3") + " - Ok");
                    return 0;
                }

                if (direction > 0 && trailingTargetPosition < entryActual - tolerance)
                    return FailMotion(-1, "FOLLOW MOVE", "목표 위치가 진행 방향(+)과 반대입니다. actual=" + entryActual.ToString("F3") + ", target=" + trailingTargetPosition.ToString("F3"), trailingTargetPosition, true);
                if (direction < 0 && trailingTargetPosition > entryActual + tolerance)
                    return FailMotion(-1, "FOLLOW MOVE", "목표 위치가 진행 방향(-)과 반대입니다. actual=" + entryActual.ToString("F3") + ", target=" + trailingTargetPosition.ToString("F3"), trailingTargetPosition, true);

                bool safetyGapClamped = safetyGap < MinimumFollowSafetyGap;
                if (safetyGapClamped)
                    safetyGap = MinimumFollowSafetyGap;

                // 팔로잉 프로파일: 성분별 Min(선행, 후행) — 후행축은 선행축보다 속도·가속·감속이
                // 클 수 없다(전 구간 불변식, 사용자 지시 2026-07-27). 최종 구간 증속 폐지.
                // 규칙 1/2(2026-07-25): 폴백도 반드시 MotionSpeedScale을 경유한다(GetDefault*는
                // 스케일 적용본). 명시 인자(>0)는 호출부가 이미 스케일한 값이므로 재스케일하지 않는다.
                double leadVel = leadingVelocity > 0.0
                    ? leadingVelocity
                    : (leadingAxis.Config != null ? leadingAxis.Config.GetDefaultVel() : 0.0);
                double leadAcc = leadingAcceleration > 0.0
                    ? leadingAcceleration
                    : (leadingAxis.Config != null ? leadingAxis.Config.GetDefaultAcc() : 0.0);
                double leadDec = leadingDeceleration > 0.0
                    ? leadingDeceleration
                    : (leadingAxis.Config != null ? leadingAxis.Config.GetDefaultDec() : 0.0);
                double trailVel = trailingVelocity > 0.0
                    ? trailingVelocity
                    : Config.GetDefaultVel();
                double trailAcc = trailingAcceleration > 0.0
                    ? trailingAcceleration
                    : Config.GetDefaultAcc();
                double trailDec = trailingDeceleration > 0.0
                    ? trailingDeceleration
                    : Config.GetDefaultDec();
                double followVel = Math.Min(leadVel, trailVel);
                double followAcc = Math.Min(leadAcc, trailAcc);
                double followDec = Math.Min(leadDec, trailDec);

                // 프로파일 해석이 0 이하로 떨어지면(설정 오염/경합) velocity=0 명령이 드라이버
                // 폴백으로 더 빠른 속도가 되는 사고(실장비 2026-07-26 22:08, followVel=0→
                // 100mm/s 추종·제자리 진동·서보 알람)를 원천 봉쇄하기 위해 즉시 실패한다.
                if (followVel <= 0.0 || followAcc <= 0.0 || followDec <= 0.0)
                {
                    return FailMotion(-1, "FOLLOW MOVE",
                        "팔로잉 프로파일 해석 실패(0 이하) — 이동을 시작하지 않습니다. " +
                        "leadVel=" + leadVel.ToString("F3") +
                        ", trailVel=" + trailVel.ToString("F3") +
                        ", followVel=" + followVel.ToString("F3") +
                        ", followAcc=" + followAcc.ToString("F3") +
                        ", followDec=" + followDec.ToString("F3") +
                        ", leading=" + leadingAxis.Name,
                        trailingTargetPosition, true);
                }

                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                    Name + " 팔로잉 이동을 시작합니다(재설계 2026-07-27). leading=" + leadingAxis.Name +
                    ", leadingTarget=" + leadingTargetPosition.ToString("F3") +
                    ", trailingTarget=" + trailingTargetPosition.ToString("F3") +
                    ", entryActual=" + entryActual.ToString("F3") +
                    ", direction=" + direction +
                    ", safetyGap=" + safetyGap.ToString("F3") + (safetyGapClamped ? "(클램프됨)" : "") +
                    ", homeGap=" + homeGap.ToString("F3") +
                    ", followVel=" + followVel.ToString("F3") +
                    ", followAcc=" + followAcc.ToString("F3") +
                    ", followDec=" + followDec.ToString("F3") +
                    ", leadVel=" + leadVel.ToString("F3") +
                    ", trailVel=" + trailVel.ToString("F3") +
                    ", timeoutMs=" + effectiveTimeoutMs +
                    ", constraints=" + (additionalConstraints != null ? additionalConstraints.Count : 0) +
                    ", trailingTargetName=" + (trailingTargetName ?? "<null>") +
                    ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Start");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // ---- Phase 1. 최초 이동 명령 — 팔로잉당 정확히 1회(래치) ----
                // 경계는 선행축 실측만의 함수(사용자 정의). 후행축 위치는 개입하지 않는다.
                // 간격이 이미 safetyGap 미만이면 command가 현재 위치보다 뒤가 되어 후퇴 명령이
                // 나간다 — 안전거리를 회복하는 방향이므로 허용(사용자 확인 2026-07-27).
                double firstLeadingActual = leadingAxis.ActualPosition;
                double firstBound = direction > 0
                    ? firstLeadingActual + homeGap - safetyGap
                    : firstLeadingActual - homeGap + safetyGap;
                double firstCommand = direction > 0
                    ? Math.Min(trailingTargetPosition, firstBound)
                    : Math.Max(trailingTargetPosition, firstBound);

                string firstConstraintDetail;
                double firstConstrained = ClampFollowCommandByConstraints(
                    firstCommand, direction, additionalConstraints, out firstConstraintDetail);
                if (firstConstraintDetail != null)
                {
                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-CONSTRAINT",
                        Name + " 팔로잉 최초 명령이 추가 제약 페어로 클램프되었습니다. " +
                        "raw=" + firstCommand.ToString("F3") +
                        ", clamped=" + firstConstrained.ToString("F3") +
                        ", " + firstConstraintDetail + " - Check");
                    firstCommand = firstConstrained;
                }

                // ③ lastCommanded 시드 = 최초 이동의 명령 좌표(사용자 지시 2026-07-27).
                //    이후 전진 판정은 오직 이 값 대비로만 한다 — 후행축 실위치 미개입.
                double lastCommanded = firstCommand;
                bool finalIssued = Math.Abs(firstCommand - trailingTargetPosition) <= tolerance;
                long overrideCount = 0;
                long skipHoldCount = 0;
                long busyRetryCount = 0;
                long lastOverrideLogMs = -1;
                long lastSkipLogMs = -1;
                long lastConstraintLogMs = -1;

                moveTask = MoveAbsoluteForFollowAsync(
                    firstCommand, followVel, followAcc, followDec, trailingTargetName);

                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-FIRST",
                    Name + " 팔로잉 최초 이동 명령을 발행했습니다(세션당 1회, 재발행 금지). " +
                    "command=" + firstCommand.ToString("F3") +
                    ", bound=" + firstBound.ToString("F3") +
                    ", leadingActual=" + firstLeadingActual.ToString("F3") +
                    ", isFinal=" + finalIssued +
                    ", velocity=" + followVel.ToString("F3") +
                    ", acc=" + followAcc.ToString("F3") +
                    ", dec=" + followDec.ToString("F3") +
                    ", targetName=" + (trailingTargetName ?? "<null>") +
                    ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Ok");

                if (finalIssued)
                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-FINAL",
                        Name + " 최초 명령이 곧 최종 목표입니다(추종 루프 생략, 완료 대기로 직행). " +
                        "command=" + firstCommand.ToString("F3") + " - Ok");

                // ---- Phase 2. 오버라이드 추종 루프 — 후행축 ActualPosition을 읽지 않는다 ----
                while (!finalIssued)
                {
                    ct.ThrowIfCancellationRequested();

                    if (stopwatch.ElapsedMilliseconds >= effectiveTimeoutMs)
                        return await FailFollowTimeoutAsync(moveTask, trailingTargetPosition, effectiveTimeoutMs).ConfigureAwait(false);

                    // 알람/IsMoving 관측 갱신용 — Actual은 팔로잉 계산에 쓰지 않는다.
                    UpdateStatus();

                    if (IsAlarm)
                    {
                        Stop();
                        await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                        moveTask = null;
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                            Name + " 팔로잉 이동 중 후행축 알람이 발생했습니다. alarmCode=0x" + AlarmCode.ToString("X4") +
                            ", lastCommanded=" + lastCommanded.ToString("F3") +
                            ", overrideCount=" + overrideCount + " - Failed");
                        return (int)AlarmCode;
                    }

                    if (leadingAxis.IsAlarm)
                    {
                        Stop();
                        await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                        moveTask = null;
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                            Name + " 팔로잉 이동 중 선행축(" + leadingAxis.Name + ") 알람이 발생했습니다. " +
                            "lastCommanded=" + lastCommanded.ToString("F3") +
                            ", overrideCount=" + overrideCount + " - Failed");
                        return FailMotion(FollowMoveLeadingAlarmErrorCode, "FOLLOW MOVE",
                            "선행축 알람이 발생했습니다. leading=" + leadingAxis.Name, trailingTargetPosition, true);
                    }

                    // ⑥ 백그라운드 이동 Task 종료 감시.
                    //    실패 → 해당 코드로 종료. 성공(0)인데 최종 미발행 → 축이 중간 좌표에서
                    //    정지해 이후 오버라이드가 전부 -4가 되는 교착 — 재발행 없이 -23으로
                    //    실패해 호출자 폴백(R6)에 위임한다(사용자 확정 2026-07-27, Q2=①).
                    if (moveTask != null && moveTask.IsCompleted)
                    {
                        int backgroundResult = ObserveFollowMoveResult(moveTask);
                        moveTask = null;
                        if (backgroundResult != 0)
                        {
                            Stop();
                            QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                Name + " 팔로잉 백그라운드 이동이 실패했습니다. result=" + backgroundResult +
                                ", lastCommanded=" + lastCommanded.ToString("F3") +
                                ", overrideCount=" + overrideCount +
                                ", elapsedMs=" + stopwatch.ElapsedMilliseconds + " - Failed");
                            return backgroundResult;
                        }

                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                            Name + " 팔로잉 백그라운드 이동이 최종 발행 전에 종료되었습니다(조기 정지 교착). " +
                            "lastCommanded=" + lastCommanded.ToString("F3") +
                            ", target=" + trailingTargetPosition.ToString("F3") +
                            ", overrideCount=" + overrideCount +
                            ", busyRetryCount=" + busyRetryCount +
                            ", elapsedMs=" + stopwatch.ElapsedMilliseconds + " - Failed");
                        return FailMotion(FollowMoveEarlyStopErrorCode, "FOLLOW MOVE",
                            "팔로잉 백그라운드 이동이 최종 발행 전에 종료되었습니다. lastCommanded=" +
                            lastCommanded.ToString("F3"), trailingTargetPosition, true);
                    }

                    // ① 경계/명령 산출 — 선행축 실측만 사용(사용자 정의 2026-07-27).
                    //    bound = 선행Actual ± (homeGap − safetyGap), command = 목표와 bound 중
                    //    덜 진행한 쪽(+방향 Min / -방향 Max).
                    double leadingActual = leadingAxis.ActualPosition;
                    double bound = direction > 0
                        ? leadingActual + homeGap - safetyGap
                        : leadingActual - homeGap + safetyGap;
                    double command = direction > 0
                        ? Math.Min(trailingTargetPosition, bound)
                        : Math.Max(trailingTargetPosition, bound);

                    // 2안 클램프(사용자 확정 2026-07-27, Q3=유지): 선행축 외 제약 페어(반대편
                    // 피커 등)의 상한을 함께 적용한다. 클램프로 역방향이 되면 아래 전진 가드가
                    // 발행을 보류한다(상대가 열릴 때까지 대기).
                    string constraintDetail;
                    double constrainedCommand = ClampFollowCommandByConstraints(
                        command, direction, additionalConstraints, out constraintDetail);
                    if (constraintDetail != null)
                    {
                        bool constraintLogDue = lastConstraintLogMs < 0 ||
                            stopwatch.ElapsedMilliseconds - lastConstraintLogMs >= 1000;
                        if (constraintLogDue)
                        {
                            lastConstraintLogMs = stopwatch.ElapsedMilliseconds;
                            QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-CONSTRAINT",
                                Name + " 팔로잉 명령이 추가 제약 페어로 클램프되었습니다. " +
                                "raw=" + command.ToString("F3") +
                                ", clamped=" + constrainedCommand.ToString("F3") +
                                ", " + constraintDetail + " - Check");
                        }

                        command = constrainedCommand;
                    }

                    // ③ 전진 가드(사용자 정의 2026-07-27): 직전 발행값(lastCommanded) 대비
                    //    진행 방향 전진일 때만 발행한다. 역방향(선행 후퇴/제약 클램프)·무변화는
                    //    발행 금지 — 보류하고 다음 사이클에 재평가한다.
                    bool commandAdvances = direction > 0
                        ? command > lastCommanded + tolerance
                        : command < lastCommanded - tolerance;
                    if (!commandAdvances)
                    {
                        skipHoldCount++;
                        bool skipLogDue = lastSkipLogMs < 0 ||
                            stopwatch.ElapsedMilliseconds - lastSkipLogMs >= 1000;
                        if (skipLogDue)
                        {
                            lastSkipLogMs = stopwatch.ElapsedMilliseconds;
                            QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-SKIP",
                                Name + " 팔로잉 오버라이드 보류(전진 아님). command=" + command.ToString("F3") +
                                ", lastCommanded=" + lastCommanded.ToString("F3") +
                                ", bound=" + bound.ToString("F3") +
                                ", leadingActual=" + leadingActual.ToString("F3") +
                                ", skipHoldCount=" + skipHoldCount + " - Check");
                        }

                        await Task.Delay(FollowMovePollIntervalMs, ct).ConfigureAwait(false);
                        continue;
                    }

                    // ④ 전 구간 단일 프로파일(Min) — 최종 오버라이드도 동일. 증속 없음.
                    // 존 판정용 이동 의도를 함께 넘긴다 — 중간 세그먼트 좌표는 티칭 존 밖이라
                    // targetName 없이는 목표 존이 Unknown이 되어 -11로 차단된다(2026-07-25 사고).
                    bool commandIsFinal = Math.Abs(command - trailingTargetPosition) <= tolerance;
                    int overrideResult = TryOverridePosition(
                        command, followVel, followAcc, followDec, trailingTargetName);

                    // 오버라이드 진단 로그: 성공은 최초 1건 + 이후 1초 1건, 실패는 제한 없이 매번.
                    // (사고 시 보드에 실제로 나간 명령값을 로그로 재구성하기 위한 진단 로그.)
                    // 현재 기준(사용자 지시 2026-07-28): 진단 상세(DiagnosticVerbose/ENABLE) 중에는
                    //   스로틀을 해제해 발행 오버라이드를 전건 기록한다(디스크 저장 여부는 LogPolicy가 판정).
                    bool overrideLogDue = overrideResult != 0 ||
                        QMC.Common.Logging.LogPolicy.IsDiagnosticVerbose ||
                        lastOverrideLogMs < 0 ||
                        stopwatch.ElapsedMilliseconds - lastOverrideLogMs >= 1000;
                    if (overrideLogDue)
                    {
                        if (overrideResult == 0)
                            lastOverrideLogMs = stopwatch.ElapsedMilliseconds;
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-OVERRIDE",
                            Name + " 팔로잉 위치 오버라이드. command=" + command.ToString("F3") +
                            ", bound=" + bound.ToString("F3") +
                            ", leadingActual=" + leadingActual.ToString("F3") +
                            ", lastCommanded=" + lastCommanded.ToString("F3") +
                            ", isFinal=" + commandIsFinal +
                            ", vel=" + followVel.ToString("F3") +
                            ", acc=" + followAcc.ToString("F3") +
                            ", dec=" + followDec.ToString("F3") +
                            ", result=" + overrideResult +
                            ", overrideCount=" + overrideCount +
                            ", busyRetryCount=" + busyRetryCount +
                            ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Check");
                    }

                    if (overrideResult == 0)
                    {
                        lastCommanded = command;
                        overrideCount++;

                        // ⑤ 최종 목표 오버라이드 발행 완료 — 이후 발행 영구 금지, 완료 대기로 전환.
                        if (commandIsFinal)
                        {
                            finalIssued = true;
                            QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-FINAL",
                                Name + " 팔로잉 최종 목표 오버라이드를 발행했습니다(이후 발행 금지, 완료 대기 전환). " +
                                "target=" + trailingTargetPosition.ToString("F3") +
                                ", overrideCount=" + overrideCount +
                                ", skipHoldCount=" + skipHoldCount +
                                ", busyRetryCount=" + busyRetryCount +
                                ", elapsedMs=" + stopwatch.ElapsedMilliseconds +
                                ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Ok");
                            break;
                        }
                    }
                    else if (overrideResult == -4)
                    {
                        // 정지 경합(보드 InMotion 미관측) — lastCommanded 미갱신, 다음 루프 재시도.
                        // 실제 정지 교착이면 위 moveTask 종료 감시(-23)가 회수한다.
                        busyRetryCount++;
                    }
                    else if (overrideResult == -11)
                    {
                        Stop();
                        await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                        moveTask = null;
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                            Name + " 팔로잉 위치 오버라이드가 인터락으로 거부되었습니다. command=" + command.ToString("F3") +
                            ", overrideCount=" + overrideCount + " - Failed");
                        return -11;
                    }
                    else
                    {
                        Stop();
                        await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                        moveTask = null;
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                            Name + " 팔로잉 위치 오버라이드가 실패했습니다. result=" + overrideResult +
                            ", command=" + command.ToString("F3") +
                            ", overrideCount=" + overrideCount + " - Failed");
                        return overrideResult;
                    }

                    await Task.Delay(FollowMovePollIntervalMs, ct).ConfigureAwait(false);
                }

                // ---- Phase 3. 최종 완료 대기 (남은 타임아웃 적용) — 모션돈까지 대기 후 리턴 ----
                int remainingMs = effectiveTimeoutMs - (int)stopwatch.ElapsedMilliseconds;
                if (remainingMs <= 0)
                    return await FailFollowTimeoutAsync(moveTask, trailingTargetPosition, effectiveTimeoutMs).ConfigureAwait(false);

                // 기존 조건: AxisMoveWaiter 결과(실패 7종)로 분기 — 현재 기준: WaitMoveCompleteAsync int 결과(R3).
                int waitCode = await WaitMoveCompleteAsync(
                    trailingTargetPosition,
                    remainingMs,
                    ct).ConfigureAwait(false);

                if (waitCode != 0)
                {
                    if (waitCode == -3)
                        return await FailFollowTimeoutAsync(moveTask, trailingTargetPosition, effectiveTimeoutMs).ConfigureAwait(false);

                    Stop();
                    await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                    moveTask = null;
                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                        Name + " 팔로잉 최종 완료 대기가 실패했습니다. waitCode=" + waitCode +
                        ", overrideCount=" + overrideCount +
                        ", " + LastMotionFailureMessage + " - Failed");
                    return FailMotion(waitCode, "FOLLOW MOVE",
                        "팔로잉 최종 완료 대기가 실패했습니다. " + LastMotionFailureMessage, trailingTargetPosition, true);
                }

                await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
                moveTask = null;
                // 완료 후 진단용 실측 1회(팔로잉 계산에는 미사용).
                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                    Name + " 팔로잉 이동이 정상 완료되었습니다. target=" + trailingTargetPosition.ToString("F3") +
                    ", actual=" + ActualPosition.ToString("F3") +
                    ", overrideCount=" + overrideCount +
                    ", skipHoldCount=" + skipHoldCount +
                    ", busyRetryCount=" + busyRetryCount +
                    ", elapsedMs=" + stopwatch.ElapsedMilliseconds + " - Ok");
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
        private async Task<int> FailFollowTimeoutAsync(Task<int> moveTask, double trailingTargetPosition, int timeoutMs = FollowMoveTimeoutMs)
        {
            Stop();
            await DrainFollowMoveTaskAsync(moveTask).ConfigureAwait(false);
            QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                Name + " 팔로잉 이동이 타임아웃(" + timeoutMs + "ms)되었습니다. actual=" + ActualPosition.ToString("F3") + " - Failed");
            return FailMotion(FollowMoveTimeoutErrorCode, "FOLLOW MOVE",
                "팔로잉 이동이 " + timeoutMs + "ms 안에 완료되지 않았습니다.", trailingTargetPosition, true);
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

        #region 서보·알람 및 기본 이동

        public override void ServoOn()
        {
            ServoOnCore(false);
        }

        // Alarm EStop 직전에 초기화가 직접 Servo OFF한 Brake 없는 축의 낙하 방지에만 사용합니다.
        // HOME/이동을 재개하지 않고 Amp ON으로 현재 위치 유지력만 복구합니다.
        internal void ServoOnForInitializeSafetyHold()
        {
            ServoOnCore(true);
        }

        private void ServoOnCore(bool allowActiveAlarmSafetyHold)
        {
            if (!allowActiveAlarmSafetyHold && AlarmManager.HasActive)
                return;

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
            {
                // 호출부의 사전 검사 직후 Alarm이 발생하는 경합에서도 일반 Servo ON은 발행하지 않습니다.
                if (!allowActiveAlarmSafetyHold && AlarmManager.HasActive)
                    return;
                ret = AXM.SetAmpEnabled(AxisNo, true);
            }
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

        // 보완(사용자 지시 2026-07-26): 이동 명령 스킵의 "일치" 판정 엡실론(mm) —
        // InPositionTolerance보다 훨씬 엄격한 값으로, 이 안이어야만 명령을 생략한다.
        // 톨러런스 내 미소 잔차는 명령을 내보내 보드로 수렴시킨다.
        private const double ExactMatchEpsilonMm = 0.0001;

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
                // 기존 조건: AxisMoveWaiter.CanSkipMoveCommandAtTarget — 현재 기준: BaseAxis 내부 스킵 헬퍼로 통일(R2).
                // 보완(사용자 지시 2026-07-26): 톨러런스 안이어도 Actual/Command가 목표와 "일치"
                // (ExactMatchEpsilonMm)하지 않으면 명령을 생략하지 않고 무조건 내보낸다 — 미소
                // 잔차도 보드로 수렴시킨다. 이때 발생하는 초단거리 이동의 인모션 미관측 문제는
                // WaitUntilMoveDone의 "Actual↔Target 일치 완료" 판정이 보완한다.
                if (!BaseAxis.IsForceMoveActive &&
                    CanSkipMoveToTarget(targetPos, tolerance) &&
                    Math.Abs(ActualPosition - targetPos) <= ExactMatchEpsilonMm &&
                    Math.Abs(CommandPosition - targetPos) <= ExactMatchEpsilonMm)
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

                // 명시 프로파일 스코프(2026-07-26, Config 임시 치환 대체): 스코프가 활성이면
                // 전달된 가감속을 그대로 쓰고 기본속도 추론(재스케일)을 하지 않는다 — S² 차단.
                double explicitAcceleration;
                double explicitDeceleration;
                bool hasExplicitProfile = BaseAxis.TryGetExplicitMotionProfile(
                    out explicitAcceleration, out explicitDeceleration);

                // 명시 velocity 가 없거나, 기존 시퀀스 헬퍼가 스케일된 DefaultVelocity 를 명시값으로 넘긴 경우에는
                // DefaultVelocity 기반 일반 이동으로 보고 가속/감속도 동일한 비율로 스케일한다.
                bool useDefaultMotionScale = !hasExplicitProfile &&
                    (velocity <= 0.0 ||
                     MotionSpeedScale.MatchesDefaultVelocityScale(velocity, Config.GetRawDefaultVelocity()));
                double vel = velocity > 0 ? velocity : Config.GetDefaultVel();
                double acceleration = hasExplicitProfile
                    ? explicitAcceleration
                    : useDefaultMotionScale
                        ? Config.GetDefaultAcc()
                        : Config.GetDefaultAcc();
                double deceleration = hasExplicitProfile
                    ? explicitDeceleration
                    : useDefaultMotionScale
                        ? Config.GetDefaultDec()
                        : Config.GetDefaultDec();
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
                // 이동 시작 시점의 오버라이드 시리얼 — 이 이동 중 위치 오버라이드(리다이렉트)가
                // 있었는지 마지막 확인에서 판정하는 기준.
                int overrideSerial = Volatile.Read(ref _positionOverrideSerial);

                // 기존 조건: AXM.MovePosition을 1초 busy-retry(커밋 292c8ab4) — 현재 기준: 프롬프트 지시로 1회 호출.
                int ret;
                lock (_sync)
                {
                    AXM.SetAbsRelMode(AxisNo, true);
                    long motionStartBaseSerial;
                    ret = AXM.MovePosition(AxisNo, boardTargetPos, boardVelocity, boardAcceleration, boardDeceleration, out motionStartBaseSerial);
                    Volatile.Write(ref _motionStartBaseSerial, motionStartBaseSerial);
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
                int waitRet = await WaitUntilMoveDone(motionStopSerial, targetPos);
                if (waitRet == 0 && !IsAlarm)
                    _motionDirection = 0;
                if (IsAlarm)
                    return FailMotion((int)AlarmCode, "ABS MOVE", "Axis alarm occurred during move.", targetPos, true);
                if (waitRet == -4)
                    return FailMotion(waitRet, "ABS MOVE", "축 정지 요청으로 이동 대기를 중단했습니다.", targetPos, true);
                if (waitRet != 0)
                    return FailMotion(waitRet, "ABS MOVE", "Move wait failed.", targetPos, true);

                // 현재 기준: 리턴 전 최종 확인은 Command↔Target 톨러런스 1가지만 (INP/Actual/settle 재확인 제거 —
                // Actual−Command 잔차는 서보 책임이라는 설계 결정, 사용자 승인).
                // 리다이렉트 인지(2026-07-25): 이동 중 위치 오버라이드가 성공했으면 CommandPosition은
                // 오버라이드 목표로 갱신되므로 원래 targetPos와의 비교는 무의미하다 — 확인을 생략하고
                // 0(정상)으로 반환한다. 최종 목표 도달 확인은 오버라이드를 발행한 쪽(선행이동
                // 코디네이터의 인포지션 대기, 팔로잉의 완료 대기)이 이미 수행한다.
                bool redirectedByOverride =
                    Volatile.Read(ref _positionOverrideSerial) != overrideSerial;
                if (!redirectedByOverride && Math.Abs(CommandPosition - targetPos) > tolerance)
                    return FailMotion(-5, "ABS MOVE",
                        "이동 완료 후 Command 위치가 목표와 다릅니다. command=" + CommandPosition.ToString("0.######") +
                        ", target=" + targetPos.ToString("0.######") +
                        ", tolerance=" + tolerance.ToString("0.######"), targetPos, true);
                if (redirectedByOverride)
                    QMC.Common.Log.Write("Motion", "SYSTEM", "AX-MOVE-REDIRECT",
                        Name + " 이동 중 위치 오버라이드로 목표가 변경되어 원래 목표 확인을 생략합니다. " +
                        "originalTarget=" + targetPos.ToString("F6") +
                        ", command=" + CommandPosition.ToString("F6") + " - Check");

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
                //if (!BaseAxis.IsForceMoveActive &&
                //    CanSkipMoveToTarget(targetPos, tolerance))
                //{
                //    CommandPosition = targetPos;
                //    CurrentVelocity = 0.0;
                //    IsMoving = false;
                //    IsInPosition = true;
                //    _motionDirection = 0;
                //    ClearMotionFailure();
                //    return Task.FromResult(0);
                //}

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
                double vel = velocity > 0 ? velocity : Config.GetDefaultVel();
                double acc = acceleration > 0 ? acceleration : Config.GetDefaultAcc();
                double dec = deceleration > 0 ? deceleration : Config.GetDefaultDec();
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
                    long motionStartBaseSerial;
                    ret = AXM.MovePosition(AxisNo, boardTargetPos, boardVelocity, boardAcceleration, boardDeceleration, out motionStartBaseSerial);
                    Volatile.Write(ref _motionStartBaseSerial, motionStartBaseSerial);
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

        #endregion

        #region 원점 검색 및 위치 상태 복원

        public override async Task<int> HomeSearchAsync()
        {
            bool sharedRailXHomeLimitSuppress = false;
            try
            {
                if (AlarmManager.HasActive)
                    return FailMotion(
                        -4,
                        "HOME",
                        "Active equipment alarm blocked a new HOME command.",
                        AxisHomeTarget(),
                        true);

                if (UseSimulation)
                {
                    sharedRailXHomeLimitSuppress = BeginSharedRailXHomeLimitSuppress();
                    // 현재 기준: 시뮬레이션 홈 동작도 실장비와 동일하게 MotionGuard를 통과해야 한다.
                    string simulationInterlockReason;
                    if (!MotionGuardRuntime.VerifyAxisHome(this, out simulationInterlockReason))
                        return FailMotion(-11, "HOME", simulationInterlockReason, AxisHomeTarget(), true);

                    if (AlarmManager.HasActive)
                        return FailMotion(
                            -4,
                            "HOME",
                            "Active equipment alarm blocked a new simulated HOME command.",
                            AxisHomeTarget(),
                            true);

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
                bool homeBlockedByAlarm;
                lock (_sync)
                {
                    homeBlockedByAlarm = AlarmManager.HasActive;
                    ret = homeBlockedByAlarm ? -4 : AXM.SetHomeStart(AxisNo);
                }
                if (homeBlockedByAlarm)
                {
                    IsMoving = false;
                    return FailMotion(
                        -4,
                        "HOME",
                        "Active equipment alarm blocked AXM.SetHomeStart.",
                        AxisHomeTarget(),
                        true);
                }
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

        #endregion

        #region 조그 이동 및 조그 정지

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
                : (Config != null ? Config.GetRawAcceleration() : 0.0);
        }

        /// <summary>Jog 구동 감속도. JogDeceleration 미설정(0 이하) 시 일반 Deceleration 으로 폴백한다.</summary>
        private double ResolveJogDeceleration()
        {
            return Config != null && Config.JogDeceleration > 0.0
                ? Config.JogDeceleration
                : (Config != null ? Config.GetRawDeceleration() : 0.0);
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
                : (Config != null ? Config.GetRawDeceleration() : 0.0);
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
                // 기존 조건: 이동 후 AxisMoveWaiter로 위치를 재확인했다.
                // 현재 기준: MoveRelativeAsync(→MoveAbsoluteAsync)가 완료를 보장하므로 재대기를 제거한다(R3).
                return result;
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

        #endregion

        #region 위치 판정 및 실시간 상태 갱신

        public override bool IsAtTargetPosition(double target, double tolerance)
        {
            bool bret = false;
            bool inMotion = false;
            bool inMotionReadOk = AXM.GetInMotion(AxisNo, ref inMotion) == 0;
            
            
            double idleBoardCommand = 0.0;
            if (AXM.GetCommandPosition(AxisNo, ref idleBoardCommand) == 0 &&
                Math.Abs(idleBoardCommand - target) <= tolerance)
            {
                bret = true;
            }
             
            return bret;
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
            // Servo OFF/Reset/Servo ON으로 이어지는 HOME 준비 구간은 아직 _isHomeSearching이 아닙니다.
            // 이 구간에는 SoftLimit만 보류하고, 물리 PEL/MEL과 Amp Fault 판정은 그대로 유지합니다.
            bool softLimitAlarmSuppressed = limitAlarmSuppressed ||
                Volatile.Read(ref _initializeHomePreparationActive) != 0;
            bool wasAlarm = IsAlarm;
            double softLimitTolerance = ResolveSoftLimitStatusTolerance();
            int hardwareLimitSearchDirection = Volatile.Read(ref _hardwareLimitSearchDirection);
            bool expectedInitializeSoftLimitPositive =
                IsFeederVisionRetreatAxis() && hardwareLimitSearchDirection > 0;
            bool expectedInitializeSoftLimitNegative =
                IsFeederVisionRetreatAxis() && hardwareLimitSearchDirection < 0;
            bool rawSoftLimitPositive = !softLimitAlarmSuppressed && Setup != null && Setup.SoftLimitEnabled &&
                !expectedInitializeSoftLimitPositive &&
                ((ActualPosition >= Setup.SoftLimitPlus - softLimitTolerance && statusMotionDirection > 0) ||
                 ActualPosition > Setup.SoftLimitPlus + softLimitTolerance);
            bool rawSoftLimitNegative = !softLimitAlarmSuppressed && Setup != null && Setup.SoftLimitEnabled &&
                !expectedInitializeSoftLimitNegative &&
                ((ActualPosition <= Setup.SoftLimitMinus + softLimitTolerance && statusMotionDirection < 0) ||
                 ActualPosition < Setup.SoftLimitMinus - softLimitTolerance);
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

            if (wasMoving && !IsMoving)
            {
                // 모션 종료 시 오버라이드 기준 무효화(스테일 제거): 자기 모션 시리얼의 기록만
                // 조건부로 지운다 — 다음 모션이 이미 새 기준을 기록했다면(다른 시리얼) 보존된다.
                // 경합 최악 케이스는 신선한 기준이 지워져 오버라이드가 -2로 거부되는 것뿐이며,
                // 잘못된 기준으로 상대값이 나가는 방향의 실패는 없다.
                AXM.ClearMotionStartCommand(AxisNo, Volatile.Read(ref _motionStartBaseSerial));
            }

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

        #endregion

        #region 리밋 복구 및 축 알람 처리

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

                // To do: [원점복귀 리밋 이탈] 홈 초기화의 하드리밋 이탈 구간에서는 소프트리밋 목표 검사를 면제한다.
                // 기존 조건: 실보드에는 홈 예외가 없어, 리밋 이탈 목표가 소프트리밋 밖이면 이동이 거부되고
                //            AX-SOFT-LIMIT-N이 떴다. 리밋 센서가 이미 해제된 뒤라 IsLimitRecoveryTarget도 false였다.
                // 현재 기준: InitializeLimitBackoff 스코프 안에서만 통과시킨다(일반 운전 이동에는 영향 없음).
                if (BaseAxis.IsInitializeLimitBackoffActive)
                {
                    if (targetPos > Setup.SoftLimitPlus || targetPos < Setup.SoftLimitMinus)
                    {
                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-SOFT-LIMIT-HOME-BYPASS",
                            Name + " 원점복귀 리밋 이탈 구간이라 소프트리밋 목표 검사를 면제합니다. " +
                            "actual=" + ActualPosition.ToString("0.###") +
                            ", target=" + targetPos.ToString("0.###") +
                            ", minus=" + Setup.SoftLimitMinus.ToString("0.###") +
                            ", plus=" + Setup.SoftLimitPlus.ToString("0.###") +
                            ", axisNo=" + AxisNo + " - Check");
                    }

                    return 0;
                }

                // To do: [소프트리미트 복구] 이미 리미트 밖에 있을 때, 리미트 쪽으로 가까워지는 이동은 허용한다.
                // 기존 조건: 목표값이 리미트 밖이면 무조건 거부했다(이동 방향을 보지 않음).
                //            → 홈 시퀀스가 MEL/PEL을 친 뒤 반대방향 5mm로 빠져나오는데, 그 지점이
                //              소프트 리미트 밖이면 그 이동 자체가 거부돼 축이 리미트 밖에 갇혔다.
                //              (InputVisionX: 백오프 목표 -1.944 vs SoftLimitMinus -1.2)
                //              IsLimitRecoveryTarget은 리미트 센서가 ON일 때만 참이라, 센서를 벗어난
                //              뒤에는 복구 우회도 걸리지 않아 자물쇠가 완성됐다.
                // 현재 기준: 현재 위치가 이미 그 방향으로 리미트를 넘어서 있고, 목표가 현재보다
                //            리미트에 가까우면(위반이 줄어들면) 통과시킨다. 더 바깥으로 나가는 이동은 그대로 거부한다.
                if (targetPos > Setup.SoftLimitPlus)
                {
                    if (ActualPosition > Setup.SoftLimitPlus && targetPos < ActualPosition)
                    {
                        LogSoftLimitRecoveryAllowed("positive", targetPos, Setup.SoftLimitPlus);
                        return 0;
                    }

                    return FailSoftLimit(10, "positive", targetPos, Setup.SoftLimitPlus);
                }

                if (targetPos < Setup.SoftLimitMinus)
                {
                    if (ActualPosition < Setup.SoftLimitMinus && targetPos > ActualPosition)
                    {
                        LogSoftLimitRecoveryAllowed("negative", targetPos, Setup.SoftLimitMinus);
                        return 0;
                    }

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

        // To do: [소프트리미트 복구] 위반 완화 이동을 통과시킨 사실과 수치를 남긴다(무단 완화가 아님을 추적 가능하게).
        private void LogSoftLimitRecoveryAllowed(string side, double targetPos, double limit)
        {
            try
            {
                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-SOFT-LIMIT-RECOVER",
                    Name + " 소프트리미트 밖에서 리미트 쪽으로 복귀하는 이동이라 통과시킵니다. " +
                    "side=" + side +
                    ", actual=" + ActualPosition.ToString("0.###") +
                    ", target=" + targetPos.ToString("0.###") +
                    ", limit=" + limit.ToString("0.###") +
                    ", violationBefore=" + Math.Abs(ActualPosition - limit).ToString("0.###") +
                    ", violationAfter=" + Math.Abs(targetPos - limit).ToString("0.###") +
                    ", axisNo=" + AxisNo + " - Check");
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

        #endregion

        #region 보드 상태 및 설정 읽기·쓰기

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

        #endregion

        #region 보드 Enum 매핑 및 단위 변환

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

        #endregion

        #region 이동 완료 대기

        // 기존 조건: BaseAxis 공용 합류 대기(IsMoving/CommandPosition 캐시 관측)를 그대로 사용했다.
        // 현재 기준: 실장비 경로는 보드를 직접 조회한다 — 완료 판정은 AXM.GetInMotion 10ms 폴링,
        //           리턴 전 확인은 AXM.GetCommandPosition의 보드 Command↔Target 톨러런스 1가지만.
        //           시뮬레이션은 base(사다리꼴 프로파일 공용 경로)로 위임한다(R5).
        //           명령 전용 발행 직후 합류하는 레이스는 WaitUntilMoveDone과 동일하게
        //           detectedMotion 래치 + 20폴 유예로 방어하되, 이미 보드 Command가 목표에 있으면
        //           완료된 이동 합류로 보고 즉시 최종 확인으로 진행한다(유예 200ms 지연 방지).
        public override async Task<int> WaitMoveCompleteAsync(double target, int timeoutMs, CancellationToken ct)
        {
            if (UseSimulation || !AjinSystem.IsOpen)
                return await base.WaitMoveCompleteAsync(target, timeoutMs, ct).ConfigureAwait(false);

            try
            {
                if (timeoutMs <= 0)
                    timeoutMs = DefaultAxisMoveTimeoutMs;
                double tolerance = Config != null && Config.InPositionTolerance > 0.0
                    ? Config.InPositionTolerance
                    : 0.01;
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

                int idlePolls = 0;
                bool detectedMotion = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    UpdateStatus();

                    if (IsAlarm)
                        return FailMotion((int)AlarmCode != 0 ? (int)AlarmCode : -1, "MOVE JOIN",
                            "이동 합류 대기 중 축 알람. alarmCode=0x" + AlarmCode.ToString("X4"), target, true);

                    // 읽기 실패 시 판정에 쓰지 않는다 — 실패한 읽기의 false를 "정지"로 오인해
                    // 이동 중 조기 break되는 것을 막고, 계속 폴링(지속 실패는 타임아웃으로 귀결).
                    bool inMotion = false;
                    bool inMotionReadOk = AXM.GetInMotion(AxisNo, ref inMotion) == 0;
                    if (inMotionReadOk && inMotion)
                        detectedMotion = true;

                    if (inMotionReadOk && !inMotion)
                    {
                        if (detectedMotion)
                            break;

                        double idleBoardCommand = 0.0;
                        if (AXM.GetCommandPosition(AxisNo, ref idleBoardCommand) == 0 &&
                            Math.Abs(FromBoardPosition(idleBoardCommand) - target) <= tolerance)
                            break;

                        if (++idlePolls > 20)
                            break;
                    }

                    if (DateTime.UtcNow >= deadline)
                        return FailMotion(-3, "MOVE JOIN",
                            "이동 합류 대기 timeout. timeoutMs=" + timeoutMs, target, true);

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                UpdateStatus();
                if (!IsServoOn)
                    return FailMotion(-2, "MOVE JOIN", "이동 합류 대기 후 서보가 OFF 상태입니다.", target, true);

                double boardCommand = 0.0;
                int readRet = AXM.GetCommandPosition(AxisNo, ref boardCommand);
                if (readRet != 0)
                    return FailMotion(readRet, "MOVE JOIN",
                        "이동 합류 완료 후 보드 Command 위치 조회 실패. ret=" + readRet, target, true);

                double command = FromBoardPosition(boardCommand);
                if (Math.Abs(command - target) > tolerance)
                    return FailMotion(-5, "MOVE JOIN",
                        "이동 합류 완료 후 보드 Command 위치가 목표와 다릅니다. command=" + command.ToString("0.######") +
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

        // 기존 조건: IsMoving+INP(IsInPosition) 조합으로 완료를 판정했다.
        // 현재 기준: AXM.GetInMotion(보드 InMotion 비트) Delay(1) 폴링만으로 완료를 판정한다 — INP 신호는
        //           완료 조건에서 제외(설계 결정, 사용자 승인). UpdateStatus로 위치 관측값 갱신은 유지.
        //           시작 유예(detectedMotion 래치 + 200ms)는 명령 직후 InMotion 미반영 레이스 방어로 유지.
        private async Task<int> WaitUntilMoveDone(int motionStopSerial, double targetPos)
        {
            // 기존 조건: 폴링 횟수로 타임아웃(guard>6000)과 이동 시작 유예(guard>20)를 판정했다 —
            //           Delay(10) 전제라 각각 60초 / 200ms 였다.
            // 현재 기준: Delay(1)에서는 폴링 횟수가 경과 시간과 무관하므로(보드 폴링 속도에 좌우)
            //           Stopwatch로 경과 시간을 직접 측정해 판정한다. TEST 임시 타임아웃 300초.
            // 시작 유예(사용자 지시 2026-07-26): 200ms → 5000ms — 보드 InMotion 플래그 지연 시
            //           이동 Task가 실제 완료 전에 조기 '완료(0)'로 끝나는 것을 방지한다
            //           (PICKER-PICKUP-PERMISSION-VISIONX-NOT-AVOID 오탐 3건 원인 분석 대응).
            // 보완(사용자 지시 2026-07-26): 인모션 미관측 미소 이동의 5초 대기 방지 —
            //           보드가 이동 중이 아니고 Actual이 목표와 일치(톨러런스 내)하면 완료로
            //           판정한다(위치 기반 완료). 이동 중(inMotion)에는 기존 완료 판정 유지.
            const int MoveWaitTimeoutMs = DefaultAxisMoveTimeoutMs;
            const int MotionStartGraceMs = 5000;
            double arrivalTolerance = Config != null && Config.InPositionTolerance > 0.0
                ? Config.InPositionTolerance
                : 0.01;

            System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
            bool detectedMotion = false;
            while (!IsAlarm)
            {
                UpdateStatus();

                // 읽기 실패 시 판정에 쓰지 않는다(WaitMoveCompleteAsync와 동일 규칙 — 2026-07-26 정합화):
                // 실패한 읽기의 false를 "정지"로 오인해 이동 중 조기 break(조기 완료 0)되는 것을 막고,
                // 계속 폴링한다(지속 실패는 300초 타임아웃으로 귀결).
                bool inMotion = false;
                bool inMotionReadOk = AXM.GetInMotion(AxisNo, ref inMotion) == 0;
                if (inMotionReadOk && inMotion)
                    detectedMotion = true;

                if (inMotionReadOk && detectedMotion && !inMotion)
                    break;

                if (inMotionReadOk && !inMotion && Math.Abs(ActualPosition - targetPos) <= arrivalTolerance)
                    break;

                if (inMotionReadOk && !detectedMotion && !inMotion && elapsed.ElapsedMilliseconds > MotionStartGraceMs)
                    break;

                if (Volatile.Read(ref _motionStopSerial) != motionStopSerial && inMotionReadOk && !inMotion)
                    return -4;

                await Task.Delay(1).ConfigureAwait(false);

                if (elapsed.ElapsedMilliseconds > MoveWaitTimeoutMs)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-MOVE-WAIT",
                        Name,
                        "Move wait timeout. AxisNo=" + AxisNo +
                        ", elapsedMs=" + elapsed.ElapsedMilliseconds +
                        ", timeoutMs=" + MoveWaitTimeoutMs);
                    UpdateStatus();
                    return -3;
                }
            }

            UpdateStatus();
            return IsAlarm ? (int)AlarmCode : 0;
        }

        #endregion
    }
}

