using System;
using System.Threading;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Interlocks
{
    public static class MotionGuardRuntime
    {
        #region 공유 상태 및 기본 축 이동 검증

        private static readonly object Sync = new object();
        private static readonly AsyncLocal<AxisMoveScope> CurrentAxisMoveScope = new AsyncLocal<AxisMoveScope>();
        private static readonly AsyncLocal<CylinderMoveScope> CurrentCylinderMoveScope = new AsyncLocal<CylinderMoveScope>();
        private static readonly AsyncLocal<ExecutionModeScope> CurrentExecutionModeScope = new AsyncLocal<ExecutionModeScope>();
        private static readonly AsyncLocal<PickerYPairHomeScope> CurrentPickerYPairHomeScope = new AsyncLocal<PickerYPairHomeScope>();
        private static PickerYPairLimitSearchScope _pickerYPairLimitSearchScope;
        private static PickerYCollisionRecoveryJogScope _pickerYCollisionRecoveryJogScope;
        private static MotionGuardService _service;

        public static Func<MotionGuardContext> ContextProvider { get; set; }
        public static bool Enabled { get; set; } = true;

        public static bool VerifyAxisMove(BaseAxis axis, double targetPosition, out string reason)
        {
            return VerifyAxisMove(axis, targetPosition, false, out reason);
        }

        public static bool VerifyAxisMoveWithoutSharedRailX(BaseAxis axis, double targetPosition, out string reason)
        {
            return VerifyAxisMove(axis, targetPosition, true, out reason);
        }

        public static bool VerifyAxisContinuousJog(BaseAxis axis, double probeTargetPosition, string targetName, out string reason)
        {
            return VerifyAxisJog(axis, probeTargetPosition, targetName, MotionGuardMoveKind.AxisContinuousJog, false, out reason);
        }

        public static bool VerifyAxisContinuousJogWithoutSharedRailX(BaseAxis axis, double probeTargetPosition, string targetName, out string reason)
        {
            return VerifyAxisJog(axis, probeTargetPosition, targetName, MotionGuardMoveKind.AxisContinuousJog, true, out reason);
        }

        public static bool VerifyAxisStepJog(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            return VerifyAxisJog(axis, targetPosition, targetName, MotionGuardMoveKind.AxisStepJog, false, out reason);
        }

        public static bool VerifyAxisStepJogWithoutSharedRailX(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            return VerifyAxisJog(axis, targetPosition, targetName, MotionGuardMoveKind.AxisStepJog, true, out reason);
        }

        private static bool VerifyAxisMove(
            BaseAxis axis,
            double targetPosition,
            bool skipSharedRailXRule,
            out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                if (IsAxisAlreadyAtTarget(axis, targetPosition))
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                AxisMoveScope scope = CurrentAxisMoveScope.Value;
                MotionGuardExecutionMode executionMode = ResolveExecutionMode();
                MotionGuardResult result = IsMatchingScope(scope, axis, targetPosition)
                    ? service.VerifyAxisTeachingMove(axis, targetPosition, scope.TargetName, context, executionMode)
                    : service.VerifyAxisMove(axis, targetPosition, context, skipSharedRailXRule, executionMode);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Check");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Blocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. axis=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", axis != null ? axis.Name : "Axis", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Failed");
                return false;
            }
        }

        #endregion

        #region Picker Y 초기화 상태 및 조그 검증

        public static bool IsPickerYPairInitializeHomeActive(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            lock (Sync)
            {
                PickerYPairLimitSearchScope scope = _pickerYPairLimitSearchScope;
                return scope != null &&
                       scope.IsHomeActive &&
                       ReferenceEquals(scope.FrontPickerY, frontPickerY) &&
                       ReferenceEquals(scope.RearPickerY, rearPickerY);
            }
        }

        public static bool BeginPickerYPairInitializeHome(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            lock (Sync)
            {
                PickerYPairLimitSearchScope scope = _pickerYPairLimitSearchScope;
                if (scope == null ||
                    !ReferenceEquals(scope.FrontPickerY, frontPickerY) ||
                    !ReferenceEquals(scope.RearPickerY, rearPickerY))
                {
                    return false;
                }

                scope.IsHomeActive = true;
                return true;
            }
        }

        private static bool VerifyAxisJog(
            BaseAxis axis,
            double targetPosition,
            string targetName,
            MotionGuardMoveKind moveKind,
            bool skipSharedRailXRule,
            out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardExecutionMode executionMode = ResolveExecutionMode();
                MotionGuardResult result = moveKind == MotionGuardMoveKind.AxisStepJog
                    ? service.VerifyAxisStepJog(axis, targetPosition, targetName, context, skipSharedRailXRule, executionMode)
                    : service.VerifyAxisContinuousJog(axis, targetPosition, targetName, context, skipSharedRailXRule, executionMode);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - JogCheck");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - JogBlocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. axis jog=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", axis != null ? axis.Name : "Axis", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - JogFailed");
                return false;
            }
        }

        #endregion

        #region Teaching·Home·실린더 검증

        public static IDisposable BeginAxisTeachingMove(BaseAxis axis, double targetPosition, string targetName)
        {
            AxisMoveScope previous = CurrentAxisMoveScope.Value;
            CurrentAxisMoveScope.Value = new AxisMoveScope(axis, targetPosition, targetName, previous);
            return new AxisMoveScopeToken(previous);
        }

        public static bool VerifyAxisTeachingMove(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardResult result = service.VerifyAxisTeachingMove(
                    axis,
                    targetPosition,
                    targetName,
                    context,
                    ResolveExecutionMode());
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - TeachingCheck");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - TeachingBlocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. axis teaching=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", axis != null ? axis.Name : "Axis", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - TeachingFailed");
                return false;
            }
        }

        /// <summary>
        /// 구동 중 위치 오버라이드(팔로잉 중간 세그먼트)가 지금 MotionGuard를 통과하는지 확인한다.
        /// 사용자 승인 2026-07-25 — 두 가지가 일반 Teaching 이동과 다르다:
        ///   (1) 존 판정/진입 조건 생략 — 중간 좌표는 티칭 존 밖이라 목표 존이 Unknown이 되고,
        ///       최종 목표의 진입 조건은 팔로잉 최초 명령(AxisMove)에서 이미 1회 검증됐다.
        ///       Y 대향 거리·SharedRailX 페어 간격·Z 상승/Reticle/Busy는 그대로 확인한다.
        ///   (2) 알람을 올리지 않는다 — 차단이 INTERLOCK 알람으로 승격되면 간섭그룹 비상정지+
        ///       전 시퀀스 취소가 되어 팔로잉의 폴백(일반 이동 재시도)이 실행될 수 없었다
        ///       (실장비 2026-07-25 19:36). 거부는 -11로만 반환해 호출자 폴백에 맡긴다.
        /// </summary>
        public static bool CanAxisPositionOverride(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardResult result = service.VerifyAxisPositionOverride(
                    axis,
                    targetPosition,
                    targetName,
                    context,
                    ResolveExecutionMode());
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                return result.Allowed;
            }
            catch (Exception ex)
            {
                reason = "Motion guard 위치 오버라이드 판정 예외. axis=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 실제 이동을 발행하지 않고(부작용 없음) 지정한 Teaching 이동이 지금 MotionGuard 전체 판정을 통과하는지 확인한다.
        /// 대기 폴링과 실제 이동이 동일한 인터락 규칙(PickerZone·SharedRailX 포함)을 공유하도록 하기 위한 dry-run 판정이다.
        /// 실제 이동 경로와 달리 알람을 발생시키지 않고 Blocked 로그도 남기지 않는다.
        /// </summary>
        public static bool CanAxisTeachingMove(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                if (IsAxisAlreadyAtTarget(axis, targetPosition))
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardResult result = service.VerifyAxisTeachingMove(
                    axis,
                    targetPosition,
                    targetName,
                    context,
                    ResolveExecutionMode());
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                return result.Allowed;
            }
            catch (Exception ex)
            {
                reason = "Motion guard dry-run 예외. axis=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public static bool VerifyAxisHome(BaseAxis axis, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                double target = axis.Setup != null ? axis.Setup.HomeOffset : 0.0;
                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardResult result = service.VerifyAxisHome(axis, target, context);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - HomeCheck");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - HomeBlocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. axis home=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", axis != null ? axis.Name : "Axis", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - HomeFailed");
                return false;
            }
        }

        public static bool VerifyCylinderMove(QMC.Common.IO.BaseCylinder cylinder, bool moveFwd, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || cylinder == null)
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                CylinderMoveScope scope = CurrentCylinderMoveScope.Value;
                MotionGuardResult result = IsMatchingScope(scope, cylinder, moveFwd)
                    ? service.VerifyCylinderInitialize(cylinder, moveFwd, context)
                    : service.VerifyCylinderMove(cylinder, moveFwd, context);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Check");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", cylinder.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Blocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. cylinder=" + (cylinder != null ? cylinder.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", cylinder != null ? cylinder.Name : "Cylinder", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Failed");
                return false;
            }
        }

        #endregion

        #region 실행 Scope 진입

        public static IDisposable BeginCylinderInitializeMove(QMC.Common.IO.BaseCylinder cylinder, bool moveFwd, string targetName)
        {
            CylinderMoveScope previous = CurrentCylinderMoveScope.Value;
            CurrentCylinderMoveScope.Value = new CylinderMoveScope(cylinder, moveFwd, targetName, previous);
            return new CylinderMoveScopeToken(previous);
        }

        public static IDisposable BeginManualSequenceProcessMove(string reason)
        {
            ExecutionModeScope previous = CurrentExecutionModeScope.Value;
            CurrentExecutionModeScope.Value = new ExecutionModeScope(
                MotionGuardExecutionMode.ManualSequenceProcess,
                reason,
                previous);
            return new ExecutionModeScopeToken(previous);
        }

        public static IDisposable BeginAutoSequenceProcessMove(string reason)
        {
            ExecutionModeScope previous = CurrentExecutionModeScope.Value;
            CurrentExecutionModeScope.Value = new ExecutionModeScope(
                MotionGuardExecutionMode.AutoSequenceProcess,
                reason,
                previous);
            return new ExecutionModeScopeToken(previous);
        }

        public static IDisposable BeginSequenceProcessMove(bool autoMode, string reason)
        {
            return autoMode
                ? BeginAutoSequenceProcessMove(reason)
                : BeginManualSequenceProcessMove(reason);
        }

        public static IDisposable BeginPickerYPairHome(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            if (frontPickerY == null)
                throw new ArgumentNullException("frontPickerY");
            if (rearPickerY == null)
                throw new ArgumentNullException("rearPickerY");
            if (ReferenceEquals(frontPickerY, rearPickerY))
                throw new ArgumentException("PickerY PairHome requires two distinct axes.");

            PickerYPairHomeScope previous = CurrentPickerYPairHomeScope.Value;
            CurrentPickerYPairHomeScope.Value = new PickerYPairHomeScope(
                frontPickerY,
                rearPickerY,
                previous);
            return new PickerYPairHomeScopeToken(previous);
        }

        public static IDisposable BeginPickerYPairLimitSearch(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            if (frontPickerY == null)
                throw new ArgumentNullException("frontPickerY");
            if (rearPickerY == null)
                throw new ArgumentNullException("rearPickerY");
            if (ReferenceEquals(frontPickerY, rearPickerY))
                throw new ArgumentException("PickerY Pair limit search requires two distinct axes.");

            var scope = new PickerYPairLimitSearchScope(frontPickerY, rearPickerY);
            lock (Sync)
            {
                PrunePickerYCollisionRecoveryJogScopeNoLock(DateTime.UtcNow);
                if (_pickerYCollisionRecoveryJogScope != null)
                    throw new InvalidOperationException(
                        "PickerY manual collision recovery Jog scope is already active.");
                if (_pickerYPairLimitSearchScope != null)
                    throw new InvalidOperationException("PickerY Pair limit search scope is already active.");
                _pickerYPairLimitSearchScope = scope;
            }
            return new PickerYPairLimitSearchScopeToken(scope);
        }

        public static bool IsPickerYPairLimitSearchActive(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            lock (Sync)
            {
                PickerYPairLimitSearchScope scope = _pickerYPairLimitSearchScope;
                return scope != null &&
                       ReferenceEquals(scope.FrontPickerY, frontPickerY) &&
                       ReferenceEquals(scope.RearPickerY, rearPickerY);
            }
        }

        #endregion

        // To do: [Feeder HOME 카메라 퇴피 폐지 2026-08-11] Step 180/260의 VisionX 물리 퇴피를
        //        제거했으므로, 퇴피를 근거로 FeederY AxisHome의 카메라 위치 조건을 대체했던
        //        BeginFeederHomeVisionRetreat / IsFeederHomeVisionRetreatActive 예외 스코프도 폐지한다.
        //        이후 FeederY HOME의 안전 근거는 Feeder Avoid Dog 실입력 하나로 통일한다.
        //        (InputFeederInterlockRules / OutputFeederInterlockRules 참조)

        #region Picker Y 충돌 복구 조그

        // 충돌 복구 전용 방향: FrontPickerY는 -방향, RearPickerY는 +방향만 인정한다.
        internal static bool IsPickerYCollisionRecoveryDirection(BaseAxis axis, int direction)
        {
            if (axis == null || direction == 0)
                return false;

            try
            {
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                CDT320_Machine machine = context != null ? context.Machine : null;
                if (machine == null || machine.PickerFrontUnit == null || machine.PickerRearUnit == null)
                    return false;

                if (ReferenceEquals(axis, machine.PickerFrontUnit.PickerY))
                    return direction < 0;
                if (ReferenceEquals(axis, machine.PickerRearUnit.PickerY))
                    return direction > 0;

                return false;
            }
            catch
            {
                return false;
            }
        }

        // HOME 미완료 상태에서도 수동 조그 화면이 복구 권한을 요청할 수 있는지 확인한다.
        // 실제 MotionGuard 예외는 이 사전 확인이 아니라 Begin...으로 발급된 활성 scope만 인정한다.
        internal static bool CanBeginPickerYCollisionRecoveryJog(BaseAxis axis, int direction)
        {
            if (!IsPickerYCollisionRecoveryDirection(axis, direction))
                return false;

            try
            {
                lock (Sync)
                {
                    PrunePickerYCollisionRecoveryJogScopeNoLock(DateTime.UtcNow);
                    if (_pickerYCollisionRecoveryJogScope != null ||
                        _pickerYPairLimitSearchScope != null)
                        return false;
                }

                return IsPickerYCollisionRecoveryStartStateValid(axis, direction);
            }
            catch
            {
                return false;
            }
        }

        // 수동 조그 UI만 호출하는 충돌 복구 실행 권한이다.
        // AsyncLocal을 사용하지 않는 이유는 실시간 감시 루프가 별도 Task에서 실행되기 때문이다.
        internal static IDisposable BeginPickerYCollisionRecoveryJog(BaseAxis axis, int direction)
        {
            if (axis == null || direction == 0)
                return null;

            int normalizedDirection = direction < 0 ? -1 : 1;
            if (!CanBeginPickerYCollisionRecoveryJog(axis, normalizedDirection))
                return null;

            lock (Sync)
            {
                PrunePickerYCollisionRecoveryJogScopeNoLock(DateTime.UtcNow);
                if (_pickerYCollisionRecoveryJogScope != null ||
                    _pickerYPairLimitSearchScope != null)
                    return null;

                var scope = new PickerYCollisionRecoveryJogScope(axis, normalizedDirection);
                _pickerYCollisionRecoveryJogScope = scope;
                return new PickerYCollisionRecoveryJogScopeToken(scope);
            }
        }

        internal static bool IsPickerYCollisionRecoveryJogActive(BaseAxis axis, int direction)
        {
            if (axis == null || direction == 0)
                return false;

            int normalizedDirection = direction < 0 ? -1 : 1;
            PickerYCollisionRecoveryJogScope scope;
            lock (Sync)
            {
                PrunePickerYCollisionRecoveryJogScopeNoLock(DateTime.UtcNow);
                scope = _pickerYCollisionRecoveryJogScope;
                if (scope == null ||
                    !ReferenceEquals(scope.Axis, axis) ||
                    scope.Direction != normalizedDirection)
                {
                    return false;
                }
            }

            if (!IsPickerYCollisionRecoveryContinueStateValid(scope))
            {
                ClearPickerYCollisionRecoveryJogScope(scope);
                return false;
            }

            lock (Sync)
            {
                if (!ReferenceEquals(_pickerYCollisionRecoveryJogScope, scope))
                    return false;

                DateTime now = DateTime.UtcNow;
                double configuredTolerance =
                    axis.Config != null && axis.Config.InPositionTolerance > 0.0
                        ? axis.Config.InPositionTolerance
                        : 0.01;
                double tolerance = Math.Max(0.001, Math.Min(0.01, configuredTolerance));
                double actual = axis.ActualPosition;
                double delta = actual - scope.LastObservedActualPosition;
                if ((scope.Direction < 0 && delta > tolerance) ||
                    (scope.Direction > 0 && delta < -tolerance))
                {
                    _pickerYCollisionRecoveryJogScope = null;
                    return false;
                }
                if (Math.Abs(delta) > tolerance)
                    scope.LastObservedActualPosition = actual;

                if (scope.ReleaseRequested)
                {
                    if (!axis.IsMoving ||
                        (now - scope.ReleaseRequestedUtc).TotalMilliseconds > 2000.0)
                    {
                        _pickerYCollisionRecoveryJogScope = null;
                        return false;
                    }

                    // MouseUp/Stop 후 실제 InMotion OFF까지 허용 방향 감속만 보호한다.
                    return true;
                }

                // 명령 발행 전 1초의 짧은 시작 구간과 실제 이동 중에만 활성이다.
                if (axis.IsMoving ||
                    (now - scope.StartedUtc).TotalMilliseconds <= 1000.0)
                {
                    return true;
                }

                _pickerYCollisionRecoveryJogScope = null;
                return false;
            }
        }

        // 조그 정지 실패/예외 시 감속 유예 없이 복구 권한을 즉시 폐기한다.
        internal static void CancelPickerYCollisionRecoveryJog(BaseAxis axis)
        {
            lock (Sync)
            {
                PickerYCollisionRecoveryJogScope scope =
                    _pickerYCollisionRecoveryJogScope;
                if (scope == null)
                    return;
                if (axis != null && !ReferenceEquals(scope.Axis, axis))
                    return;

                _pickerYCollisionRecoveryJogScope = null;
            }
        }

        private static bool IsPickerYCollisionRecoveryStartStateValid(BaseAxis axis, int direction)
        {
            CDT320_Machine machine;
            BaseAxis frontX;
            BaseAxis rearX;
            BaseAxis frontY;
            BaseAxis rearY;
            if (!TryResolvePickerYCollisionRecoveryAxes(
                out machine,
                out frontX,
                out rearX,
                out frontY,
                out rearY))
            {
                return false;
            }

            if (!ReferenceEquals(axis, frontY) && !ReferenceEquals(axis, rearY))
                return false;

            if (!TryUpdatePickerYCollisionRecoveryAxis(frontX) ||
                !TryUpdatePickerYCollisionRecoveryAxis(rearX) ||
                !TryUpdatePickerYCollisionRecoveryAxis(frontY) ||
                !TryUpdatePickerYCollisionRecoveryAxis(rearY))
            {
                return false;
            }

            if (frontX.IsMoving || rearX.IsMoving || frontY.IsMoving || rearY.IsMoving)
                return false;
            if (HasPickerMaterialForCollisionRecovery())
                return false;
            if (Math.Abs(frontX.ActualPosition - rearX.ActualPosition) >
                ResolvePickerYCollisionRecoveryClearance(machine))
            {
                return false;
            }

            // 실시간 감시와 동일하게 양쪽 모두 exact Avoid가 아닐 때만 복구 모드가 필요하다.
            return !MotionGuardRuleHelpers.IsPickerYAtExactTeachingAvoid(
                       machine,
                       true,
                       frontY.ActualPosition) &&
                   !MotionGuardRuleHelpers.IsPickerYAtExactTeachingAvoid(
                       machine,
                       false,
                       rearY.ActualPosition) &&
                   IsPickerYCollisionRecoveryDirection(axis, direction);
        }

        private static bool HasPickerMaterialForCollisionRecovery()
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (MaterialStateService.GetDieAtPicker(
                        MaterialLocationKind.PickerFront,
                        pickerNo) != null ||
                    MaterialStateService.GetDieAtPicker(
                        MaterialLocationKind.PickerRear,
                        pickerNo) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryUpdatePickerYCollisionRecoveryAxis(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return false;

                axis.UpdateStatus();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPickerYCollisionRecoveryContinueStateValid(
            PickerYCollisionRecoveryJogScope scope)
        {
            if (scope == null || scope.Axis == null)
                return false;

            CDT320_Machine machine;
            BaseAxis frontX;
            BaseAxis rearX;
            BaseAxis frontY;
            BaseAxis rearY;
            if (!TryResolvePickerYCollisionRecoveryAxes(
                out machine,
                out frontX,
                out rearX,
                out frontY,
                out rearY))
            {
                return false;
            }

            bool isFront = ReferenceEquals(scope.Axis, frontY);
            bool isRear = ReferenceEquals(scope.Axis, rearY);
            if (!isFront && !isRear)
                return false;
            if (!IsPickerYCollisionRecoveryDirection(scope.Axis, scope.Direction))
                return false;
            if (HasPickerMaterialForCollisionRecovery())
                return false;
            if (frontX.IsMoving || rearX.IsMoving)
                return false;
            if (isFront && rearY.IsMoving)
                return false;
            if (isRear && frontY.IsMoving)
                return false;

            return Math.Abs(frontX.ActualPosition - rearX.ActualPosition) <=
                   ResolvePickerYCollisionRecoveryClearance(machine);
        }

        private static bool TryResolvePickerYCollisionRecoveryAxes(
            out CDT320_Machine machine,
            out BaseAxis frontX,
            out BaseAxis rearX,
            out BaseAxis frontY,
            out BaseAxis rearY)
        {
            machine = null;
            frontX = null;
            rearX = null;
            frontY = null;
            rearY = null;

            MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
            machine = context != null ? context.Machine : null;
            if (machine == null ||
                machine.PickerFrontUnit == null ||
                machine.PickerRearUnit == null)
            {
                return false;
            }

            frontX = machine.PickerFrontUnit.PickerX;
            rearX = machine.PickerRearUnit.PickerX;
            frontY = machine.PickerFrontUnit.PickerY;
            rearY = machine.PickerRearUnit.PickerY;
            return frontX != null && rearX != null && frontY != null && rearY != null;
        }

        private static double ResolvePickerYCollisionRecoveryClearance(CDT320_Machine machine)
        {
            double frontClearance =
                machine != null &&
                machine.PickerFrontUnit != null &&
                machine.PickerFrontUnit.Setup != null
                    ? machine.PickerFrontUnit.Setup.PickerYFacingXClearance
                    : 0.0;
            double rearClearance =
                machine != null &&
                machine.PickerRearUnit != null &&
                machine.PickerRearUnit.Setup != null
                    ? machine.PickerRearUnit.Setup.PickerYFacingXClearance
                    : 0.0;
            double clearance = Math.Max(frontClearance, rearClearance);
            return clearance > 0.0 ? clearance : 150.0;
        }

        private static void PrunePickerYCollisionRecoveryJogScopeNoLock(DateTime now)
        {
            PickerYCollisionRecoveryJogScope scope = _pickerYCollisionRecoveryJogScope;
            if (scope == null)
                return;

            if (scope.ReleaseRequested)
            {
                if (!scope.Axis.IsMoving ||
                    (now - scope.ReleaseRequestedUtc).TotalMilliseconds > 2000.0)
                {
                    _pickerYCollisionRecoveryJogScope = null;
                }
                return;
            }

            if (!scope.Axis.IsMoving &&
                (now - scope.StartedUtc).TotalMilliseconds > 1000.0)
            {
                _pickerYCollisionRecoveryJogScope = null;
            }
        }

        private static void ClearPickerYCollisionRecoveryJogScope(
            PickerYCollisionRecoveryJogScope scope)
        {
            lock (Sync)
            {
                if (ReferenceEquals(_pickerYCollisionRecoveryJogScope, scope))
                    _pickerYCollisionRecoveryJogScope = null;
            }
        }

        #endregion

        #region Picker Y Pair Home 승인

        public static bool IsPickerYPairHomeActive(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            PickerYPairHomeScope scope = CurrentPickerYPairHomeScope.Value;
            return scope != null &&
                   ReferenceEquals(scope.FrontPickerY, frontPickerY) &&
                   ReferenceEquals(scope.RearPickerY, rearPickerY);
        }

        public static bool IsPickerYPairHomeAuthorized(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            PickerYPairHomeScope scope = CurrentPickerYPairHomeScope.Value;
            return scope != null &&
                   scope.IsAuthorized &&
                   ReferenceEquals(scope.FrontPickerY, frontPickerY) &&
                   ReferenceEquals(scope.RearPickerY, rearPickerY);
        }

        public static bool AuthorizePickerYPairHome(
            BaseAxis frontPickerY,
            BaseAxis rearPickerY,
            out string reason)
        {
            reason = string.Empty;
            PickerYPairHomeScope scope = CurrentPickerYPairHomeScope.Value;
            if (scope == null ||
                !ReferenceEquals(scope.FrontPickerY, frontPickerY) ||
                !ReferenceEquals(scope.RearPickerY, rearPickerY))
            {
                reason = "PickerY PairHome 실행 범위와 대상 축 쌍이 일치하지 않습니다.";
                return false;
            }

            if (!VerifyAxisHome(frontPickerY, out reason))
                return false;
            if (!VerifyAxisHome(rearPickerY, out reason))
                return false;

            scope.IsAuthorized = true;
            return true;
        }

        #endregion

        #region 서비스 수명주기 및 공통 판정

        public static void Reload()
        {
            lock (Sync)
                _service = new MotionGuardService(InterlockCheckMatrixStore.LoadOrDefault());
        }

        private static MotionGuardService GetService()
        {
            lock (Sync)
            {
                if (_service == null)
                    _service = new MotionGuardService(InterlockCheckMatrixStore.LoadOrDefault());
                return _service;
            }
        }

        private static bool IsAxisAlreadyAtTarget(BaseAxis axis, double targetPosition)
        {
            try
            {
                if (axis == null || axis.IsMoving || axis.IsAlarm)
                    return false;

                double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.01;

                return Math.Abs(axis.ActualPosition - targetPosition) <= tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsMatchingScope(AxisMoveScope scope, BaseAxis axis, double targetPosition)
        {
            if (scope == null || axis == null || !object.ReferenceEquals(scope.Axis, axis))
                return false;

            return Math.Abs(scope.TargetPosition - targetPosition) <= 0.0001;
        }

        private static bool IsMatchingScope(CylinderMoveScope scope, QMC.Common.IO.BaseCylinder cylinder, bool moveFwd)
        {
            if (scope == null || cylinder == null || !object.ReferenceEquals(scope.Cylinder, cylinder))
                return false;

            return scope.MoveFwd == moveFwd;
        }

        private static MotionGuardExecutionMode ResolveExecutionMode()
        {
            ExecutionModeScope scope = CurrentExecutionModeScope.Value;
            return scope != null ? scope.Mode : MotionGuardExecutionMode.Default;
        }

        #endregion

        #region Scope 및 해제 Token 형식

        private sealed class AxisMoveScope
        {
            public AxisMoveScope(BaseAxis axis, double targetPosition, string targetName, AxisMoveScope previous)
            {
                Axis = axis;
                TargetPosition = targetPosition;
                TargetName = targetName ?? string.Empty;
                Previous = previous;
            }

            public BaseAxis Axis { get; private set; }
            public double TargetPosition { get; private set; }
            public string TargetName { get; private set; }
            public AxisMoveScope Previous { get; private set; }
        }

        private sealed class AxisMoveScopeToken : IDisposable
        {
            private readonly AxisMoveScope _previous;
            private bool _disposed;

            public AxisMoveScopeToken(AxisMoveScope previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                CurrentAxisMoveScope.Value = _previous;
                _disposed = true;
            }
        }

        private sealed class PickerYPairLimitSearchScope
        {
            public PickerYPairLimitSearchScope(BaseAxis frontPickerY, BaseAxis rearPickerY)
            {
                FrontPickerY = frontPickerY;
                RearPickerY = rearPickerY;
            }

            public BaseAxis FrontPickerY { get; private set; }
            public BaseAxis RearPickerY { get; private set; }
            public bool IsHomeActive { get; set; }
        }

        private sealed class PickerYPairLimitSearchScopeToken : IDisposable
        {
            private readonly PickerYPairLimitSearchScope _scope;
            private bool _disposed;

            public PickerYPairLimitSearchScopeToken(PickerYPairLimitSearchScope scope)
            {
                _scope = scope;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                lock (Sync)
                {
                    if (ReferenceEquals(_pickerYPairLimitSearchScope, _scope))
                        _pickerYPairLimitSearchScope = null;
                }
                _disposed = true;
            }
        }

        private sealed class PickerYCollisionRecoveryJogScope
        {
            public PickerYCollisionRecoveryJogScope(BaseAxis axis, int direction)
            {
                Axis = axis;
                Direction = direction;
                StartedUtc = DateTime.UtcNow;
                LastObservedActualPosition = axis != null ? axis.ActualPosition : 0.0;
            }

            public BaseAxis Axis { get; private set; }
            public int Direction { get; private set; }
            public DateTime StartedUtc { get; private set; }
            public double LastObservedActualPosition { get; set; }
            public bool ReleaseRequested { get; set; }
            public DateTime ReleaseRequestedUtc { get; set; }
        }

        private sealed class PickerYCollisionRecoveryJogScopeToken : IDisposable
        {
            private readonly PickerYCollisionRecoveryJogScope _scope;
            private bool _disposed;

            public PickerYCollisionRecoveryJogScopeToken(PickerYCollisionRecoveryJogScope scope)
            {
                _scope = scope;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                lock (Sync)
                {
                    if (ReferenceEquals(_pickerYCollisionRecoveryJogScope, _scope))
                    {
                        if (_scope.Axis == null || !_scope.Axis.IsMoving)
                        {
                            _pickerYCollisionRecoveryJogScope = null;
                        }
                        else
                        {
                            _scope.ReleaseRequested = true;
                            _scope.ReleaseRequestedUtc = DateTime.UtcNow;
                        }
                    }
                }

                _disposed = true;
            }
        }

        private sealed class CylinderMoveScope
        {
            public CylinderMoveScope(QMC.Common.IO.BaseCylinder cylinder, bool moveFwd, string targetName, CylinderMoveScope previous)
            {
                Cylinder = cylinder;
                MoveFwd = moveFwd;
                TargetName = targetName ?? string.Empty;
                Previous = previous;
            }

            public QMC.Common.IO.BaseCylinder Cylinder { get; private set; }
            public bool MoveFwd { get; private set; }
            public string TargetName { get; private set; }
            public CylinderMoveScope Previous { get; private set; }
        }

        private sealed class CylinderMoveScopeToken : IDisposable
        {
            private readonly CylinderMoveScope _previous;
            private bool _disposed;

            public CylinderMoveScopeToken(CylinderMoveScope previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                CurrentCylinderMoveScope.Value = _previous;
                _disposed = true;
            }
        }

        private sealed class ExecutionModeScope
        {
            public ExecutionModeScope(
                MotionGuardExecutionMode mode,
                string reason,
                ExecutionModeScope previous)
            {
                Mode = mode;
                Reason = reason ?? string.Empty;
                Previous = previous;
            }

            public MotionGuardExecutionMode Mode { get; private set; }
            public string Reason { get; private set; }
            public ExecutionModeScope Previous { get; private set; }
        }

        private sealed class ExecutionModeScopeToken : IDisposable
        {
            private readonly ExecutionModeScope _previous;
            private bool _disposed;

            public ExecutionModeScopeToken(ExecutionModeScope previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                CurrentExecutionModeScope.Value = _previous;
                _disposed = true;
            }
        }

        private sealed class PickerYPairHomeScope
        {
            public PickerYPairHomeScope(
                BaseAxis frontPickerY,
                BaseAxis rearPickerY,
                PickerYPairHomeScope previous)
            {
                FrontPickerY = frontPickerY;
                RearPickerY = rearPickerY;
                Previous = previous;
            }

            public BaseAxis FrontPickerY { get; private set; }
            public BaseAxis RearPickerY { get; private set; }
            public PickerYPairHomeScope Previous { get; private set; }
            public bool IsAuthorized { get; set; }
        }

        private sealed class PickerYPairHomeScopeToken : IDisposable
        {
            private readonly PickerYPairHomeScope _previous;
            private bool _disposed;

            public PickerYPairHomeScopeToken(PickerYPairHomeScope previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                CurrentPickerYPairHomeScope.Value = _previous;
                _disposed = true;
            }
        }

        #endregion
    }
}
