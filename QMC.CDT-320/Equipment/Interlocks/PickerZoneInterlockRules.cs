using System;
using System.Globalization;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    internal enum PickerWorkZone
    {
        Unknown,
        Avoid,
        Input,
        Bottom,
        Side,
        Output
    }

    internal sealed class PickerZoneTransportState
    {
        public bool IsFront { get; set; }
        public PickerWorkZone RequestedZone { get; set; }
        public PickerWorkZone CurrentZone { get; set; }
        public PickerWorkZone TargetZone { get; set; }
        public bool HasWorkArea { get; set; }
        public PickerWorkZone WorkAreaZone { get; set; }
        public string WorkAreaOwner { get; set; }
        public bool YAvoid { get; set; }
        public double? TargetX { get; set; }
        public string TargetName { get; set; }
        public BaseAxis PickerX { get; set; }
        public BaseAxis PickerY { get; set; }
        public bool UnknownUnsafe { get; set; }

        public bool IsRequestedZoneActive
        {
            get
            {
                return PickerZoneInterlockRules.IsSameInterlockZone(CurrentZone, RequestedZone) ||
                       PickerZoneInterlockRules.IsSameInterlockZone(TargetZone, RequestedZone);
            }
        }

        public bool WorkAreaBlocksTransport
        {
            get
            {
                if (!HasWorkArea || !PickerZoneInterlockRules.IsSameInterlockZone(WorkAreaZone, RequestedZone))
                    return false;

                return !IsWorkAreaPhysicallyClearForTransport;
            }
        }

        public bool IsWorkAreaPhysicallyClearForTransport
        {
            get
            {
                bool yClearForRequestedTransport = RequestedZone == PickerWorkZone.Input || YAvoid;
                return yClearForRequestedTransport &&
                       !PickerZoneInterlockRules.IsSameInterlockZone(CurrentZone, RequestedZone) &&
                       !PickerZoneInterlockRules.IsSameInterlockZone(TargetZone, RequestedZone) &&
                       !IsAxisMoving(PickerX) &&
                       !IsAxisMoving(PickerY);
            }
        }

        public bool BlocksTransport
        {
            get { return IsRequestedZoneActive || WorkAreaBlocksTransport || UnknownUnsafe; }
        }

        public string Describe()
        {
            return (IsFront ? "FrontPicker" : "RearPicker") +
                " currentZone=" + CurrentZone +
                ", targetZone=" + TargetZone +
                ", requestedZone=" + RequestedZone +
                ", workArea=" + (HasWorkArea ? WorkAreaZone.ToString() : "None") +
                ", owner=" + (HasWorkArea ? WorkAreaOwner : "-") +
                ", workAreaBlocksTransport=" + WorkAreaBlocksTransport +
                ", workAreaPhysicalClear=" + IsWorkAreaPhysicallyClearForTransport +
                ", yAvoid=" + YAvoid +
                ", unknownUnsafe=" + UnknownUnsafe +
                ", x=" + FormatAxis(PickerX) +
                ", y=" + FormatAxis(PickerY) +
                ", targetX=" + (TargetX.HasValue ? TargetX.Value.ToString("0.###") : "-") +
                ", targetName=" + (string.IsNullOrWhiteSpace(TargetName) ? "-" : TargetName);
        }

        private static string FormatAxis(BaseAxis axis)
        {
            return axis != null ? axis.ActualPosition.ToString("0.###") : "<null>";
        }

        private static bool IsAxisMoving(BaseAxis axis)
        {
            return axis != null && axis.IsMoving;
        }
    }

    internal static class PickerZoneInterlockRules
    {
        private const double DefaultTolerance = 0.05;
        private const double DefaultPickerYFacingXClearance = 150.0;
        private const double DefaultPickerYOutDistance = 1.0;
        private const double DefaultAutoProcessCorrectionMaxDistance = 2.0;
        private const double DefaultAutoProcessZoneEntryYTolerance = 2.0;
        private static readonly object activeZoneLock = new object();
        private static PickerWorkZone frontPickerYActiveTargetZone = PickerWorkZone.Unknown;
        private static PickerWorkZone rearPickerYActiveTargetZone = PickerWorkZone.Unknown;
        private static int frontInputPickAreaUseCount;
        private static int rearInputPickAreaUseCount;
        private static string frontInputPickAreaOwner = string.Empty;
        private static string rearInputPickAreaOwner = string.Empty;
        private static int frontBottomAreaUseCount;
        private static int rearBottomAreaUseCount;
        private static string frontBottomAreaOwner = string.Empty;
        private static string rearBottomAreaOwner = string.Empty;
        private static int frontSideAreaUseCount;
        private static int rearSideAreaUseCount;
        private static string frontSideAreaOwner = string.Empty;
        private static string rearSideAreaOwner = string.Empty;
        private static int frontOutputAreaUseCount;
        private static int rearOutputAreaUseCount;
        private static string frontOutputAreaOwner = string.Empty;
        private static string rearOutputAreaOwner = string.Empty;
        private static DateTime lastFrontEncoderOverlapLogUtc = DateTime.MinValue;
        private static DateTime lastRearEncoderOverlapLogUtc = DateTime.MinValue;

        public static IDisposable BeginPickerZoneMove(string side, PickerAxis axis, string targetName)
        {
            bool isFront = string.Equals(side, "Front", StringComparison.OrdinalIgnoreCase);
            bool isRear = string.Equals(side, "Rear", StringComparison.OrdinalIgnoreCase);
            if ((!isFront && !isRear) || axis != PickerAxis.PickerY)
                return new ActiveZoneScope(false, PickerWorkZone.Unknown, false);

            // 현재 기준: PickerY 활성 목표 존은 Bottom/Side를 하나의 Process 존으로 정규화해서 관리한다.
            PickerWorkZone zone = NormalizeInterlockZone(ParseZone(targetName));
            lock (activeZoneLock)
            {
                PickerWorkZone previous = isFront ? frontPickerYActiveTargetZone : rearPickerYActiveTargetZone;
                if (isFront)
                    frontPickerYActiveTargetZone = zone;
                else
                    rearPickerYActiveTargetZone = zone;

                return new ActiveZoneScope(isFront, previous, true);
            }
        }

        public static IDisposable BeginInputPickAreaUse(bool isFront, string owner)
        {
            return BeginPickerWorkAreaUse(isFront, PickerWorkZone.Input, owner);
        }

        public static IDisposable BeginPickerWorkAreaUse(bool isFront, PickerWorkZone zone, string owner)
        {
            // 현재 기준: INSPECT_B/INSPECT_S 작업 점유는 같은 Process 존 점유로 관리한다.
            zone = NormalizeInterlockZone(zone);
            lock (activeZoneLock)
            {
                AddPickerWorkAreaUse(isFront, zone, owner);
            }

            return new PickerWorkAreaScope(isFront, zone);
        }

        public static bool TryGetPickerWorkArea(bool isFront, out PickerWorkZone zone, out string owner)
        {
            zone = PickerWorkZone.Unknown;
            owner = string.Empty;

            try
            {
                lock (activeZoneLock)
                {
                    if (IsPickerWorkAreaActive(isFront, PickerWorkZone.Input, out owner))
                    {
                        zone = PickerWorkZone.Input;
                        return true;
                    }

                    // 현재 기준: Bottom/Side 점유는 Process 점유 하나로 보고 Bottom을 대표값으로 반환한다.
                    if (IsPickerWorkAreaActive(isFront, PickerWorkZone.Bottom, out owner))
                    {
                        zone = PickerWorkZone.Bottom;
                        return true;
                    }

                    if (IsPickerWorkAreaActive(isFront, PickerWorkZone.Side, out owner))
                    {
                        zone = PickerWorkZone.Side;
                        return true;
                    }

                    if (IsPickerWorkAreaActive(isFront, PickerWorkZone.Output, out owner))
                    {
                        zone = PickerWorkZone.Output;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                zone = PickerWorkZone.Unknown;
                owner = string.Empty;
                return false;
            }
            finally
            {
            }
        }

        public static bool ClearPickerWorkAreasForReadyIfSafe(CDT320_Machine machine, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (machine == null)
                {
                    detail = "Machine 객체가 없어 Ready 상태 해제를 진행할 수 없습니다.";
                    return false;
                }

                string frontReason;
                if (!IsFrontPickerReadyAvoidSafe(machine, out frontReason))
                {
                    detail = "FrontPicker가 Ready Avoid 안전 상태가 아닙니다. " + frontReason;
                    return false;
                }

                string rearReason;
                if (!IsRearPickerReadyAvoidSafe(machine, out rearReason))
                {
                    detail = "RearPicker가 Ready Avoid 안전 상태가 아닙니다. " + rearReason;
                    return false;
                }

                string clearedSummary;
                bool cleared;
                lock (activeZoneLock)
                {
                    cleared = ClearAllPickerWorkAreasLocked(out clearedSummary);
                    frontPickerYActiveTargetZone = PickerWorkZone.Unknown;
                    rearPickerYActiveTargetZone = PickerWorkZone.Unknown;
                }

                detail = cleared
                    ? "Ready Avoid 안전 상태 확인 후 Picker 작업 점유 상태를 해제했습니다. " + clearedSummary
                    : "Ready Avoid 안전 상태입니다. 해제할 Picker 작업 점유 상태가 없습니다.";
                return true;
            }
            catch (Exception ex)
            {
                detail = "Ready 상태 해제 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public static PickerWorkZone GetPickerYActiveTargetZone(bool isFront)
        {
            return GetActivePickerYTargetZone(isFront);
        }

        public static PickerWorkZone GetPickerCurrentXZone(CDT320_Machine machine, bool isFront)
        {
            return ResolveCurrentXZoneWithContext(machine, isFront);
        }

        public static PickerWorkZone GetPickerXZoneByPosition(CDT320_Machine machine, bool isFront, double position)
        {
            return ResolveXZoneByPosition(machine, isFront, position);
        }

        internal static bool IsProcessZone(PickerWorkZone zone)
        {
            return zone == PickerWorkZone.Bottom || zone == PickerWorkZone.Side;
        }

        internal static PickerWorkZone NormalizeInterlockZone(PickerWorkZone zone)
        {
            // 현재 기준: INSPECT_B/INSPECT_S는 인터락에서 하나의 Process 존으로 취급한다.
            return IsProcessZone(zone) ? PickerWorkZone.Bottom : zone;
        }

        internal static bool IsSameInterlockZone(PickerWorkZone first, PickerWorkZone second)
        {
            if (first == PickerWorkZone.Unknown || second == PickerWorkZone.Unknown)
                return false;

            return NormalizeInterlockZone(first) == NormalizeInterlockZone(second);
        }

        public static bool CanMovePickerAxisByFacingYInterlock(
            CDT320_Machine machine,
            bool isFront,
            PickerAxis axis,
            double target,
            string targetName,
            double? pairedXTarget,
            double? pairedYTarget,
            out string detail)
        {
            detail = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                if (axis == PickerAxis.PickerY)
                    return CanMovePickerYByFacingYInterlock(
                        machine,
                        isFront,
                        target,
                        pairedXTarget,
                        targetName,
                        out detail);

                if (axis == PickerAxis.PickerX)
                    return CanMovePickerXByFacingYInterlock(
                        machine,
                        isFront,
                        target,
                        pairedYTarget,
                        targetName,
                        out detail);

                return true;
            }
            catch (Exception ex)
            {
                detail = "Front/Rear Picker Y 돌출 X거리 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public static bool VerifyPickerZAtOrAboveZeroForZoneStageZMove(
            CDT320_Machine machine,
            PickerWorkZone zone,
            string movingName,
            bool movingTowardPicker,
            out string reason)
        {
            reason = string.Empty;

            // 현재 기준: Stage Z가 안전 방향으로 내려가는 목표이면 PickerZ 0 이상 조건을 적용하지 않는다.
            if (!movingTowardPicker)
                return true;

            if (!VerifyPickerZAtOrAboveZeroForZoneStageZMove(machine, true, zone, movingName, out reason))
                return false;

            return VerifyPickerZAtOrAboveZeroForZoneStageZMove(machine, false, zone, movingName, out reason);
        }

        private static bool VerifyPickerZAtOrAboveZeroForZoneStageZMove(
            CDT320_Machine machine,
            bool isFront,
            PickerWorkZone zone,
            string movingName,
            out string reason)
        {
            reason = string.Empty;

            PickerZoneTransportState state = ResolvePickerZoneTransportState(machine, isFront, zone, null, string.Empty);
            if (state == null)
                return true;

            if (!state.IsRequestedZoneActive && !state.UnknownUnsafe)
                return true;

            string pickerName = isFront ? "FrontPicker" : "RearPicker";
            if (state.UnknownUnsafe && !state.IsRequestedZoneActive)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: " + pickerName + " 존을 판단할 수 없고 PickerY가 안전 위치가 아닙니다. " + state.Describe(),
                    out reason);
            }

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = GetPickerZ(machine, isFront, zAxis);
                if (axis == null)
                    continue;

                if (axis.IsMoving)
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: " + pickerName + zAxis + "가 이동 중입니다. " + state.Describe(),
                        out reason);
                }

                // 현재 기준: Picker가 해당 존에 있으면 Stage Z 상승/접근 이동 전 PickerZ는 0 이상 또는 AvoidPosition이어야 한다.
                if (!IsPickerZAtOrAboveZeroOrAvoid(machine, isFront, zAxis, axis))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: " + pickerName + zAxis + "가 0 이상 또는 Avoid 위치가 아닙니다. actual=" +
                        axis.ActualPosition.ToString("0.###") + ". " + state.Describe(),
                        out reason);
                }
            }

            return true;
        }

        private static bool IsPickerZAtOrAboveZeroOrAvoid(
            CDT320_Machine machine,
            bool isFront,
            PickerAxis zAxis,
            BaseAxis axis)
        {
            if (axis == null)
                return true;

            if (axis.ActualPosition >= -ResolveTolerance(axis))
                return true;

            if (isFront)
            {
                PickerFrontUnit picker = machine != null ? machine.PickerFrontUnit : null;
                return picker != null && picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition");
            }

            PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
            return rear != null && rear.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition");
        }

        public static bool IsPickerBlockingZoneTransport(
            CDT320_Machine machine,
            bool isFront,
            PickerWorkZone zone,
            out string detail)
        {
            return IsPickerBlockingZoneTransport(machine, isFront, zone, null, string.Empty, out detail);
        }

        public static bool IsPickerBlockingZoneTransport(
            CDT320_Machine machine,
            bool isFront,
            PickerWorkZone zone,
            double? targetX,
            string targetName,
            out string detail)
        {
            detail = string.Empty;

            try
            {
                PickerZoneTransportState state = ResolvePickerZoneTransportState(machine, isFront, zone, targetX, targetName);
                detail = state.Describe();
                return state.BlocksTransport;
            }
            catch (Exception ex)
            {
                detail = (isFront ? "FrontPicker" : "RearPicker") +
                    " zone transport check failed. error=" + ex.Message;
                return true;
            }
            finally
            {
            }
        }

        public static PickerZoneTransportState ResolvePickerZoneTransportState(
            CDT320_Machine machine,
            bool isFront,
            PickerWorkZone zone,
            double? targetX,
            string targetName)
        {
            var state = new PickerZoneTransportState
            {
                IsFront = isFront,
                RequestedZone = zone,
                CurrentZone = PickerWorkZone.Unknown,
                TargetZone = PickerWorkZone.Unknown,
                WorkAreaZone = PickerWorkZone.Unknown,
                WorkAreaOwner = string.Empty,
                YAvoid = true,
                TargetX = targetX,
                TargetName = targetName ?? string.Empty
            };

            try
            {
                if (machine == null || zone == PickerWorkZone.Unknown || zone == PickerWorkZone.Avoid)
                    return state;

                state.PickerX = GetPickerX(machine, isFront);
                state.PickerY = GetPickerY(machine, isFront);
                state.CurrentZone = ResolveCurrentXZoneWithContext(machine, isFront);
                state.TargetZone = ResolveTargetXZoneWithContext(machine, isFront, targetX, targetName);
                if (state.TargetZone == PickerWorkZone.Unknown && !targetX.HasValue)
                    state.TargetZone = state.CurrentZone;

                string owner;
                PickerWorkZone resourceZone;
                state.HasWorkArea = TryGetPickerWorkArea(isFront, out resourceZone, out owner);
                state.WorkAreaZone = resourceZone;
                state.WorkAreaOwner = owner;
                state.YAvoid = IsPickerYAtAvoid(machine, isFront);

                state.UnknownUnsafe =
                    (state.CurrentZone == PickerWorkZone.Unknown || state.TargetZone == PickerWorkZone.Unknown) &&
                    !state.YAvoid;
            }
            catch (Exception ex)
            {
                state.WorkAreaOwner = "zone transport state failed. error=" + ex.Message;
                state.UnknownUnsafe = true;
            }
            finally
            {
            }

            return state;
        }

        public static string ResolvePickerPhysicalZoneName(CDT320_Machine machine, bool isFront)
        {
            try
            {
                return ToDisplayName(ResolveCurrentXZoneWithContext(machine, isFront));
            }
            catch
            {
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        public static bool VerifyFrontPickerXMove(MotionGuardRuleContext request, out string reason)
        {
            return VerifyPickerXMove(
                request,
                true,
                "FrontPickerX",
                out reason);
        }

        public static bool VerifyRearPickerXMove(MotionGuardRuleContext request, out string reason)
        {
            return VerifyPickerXMove(
                request,
                false,
                "RearPickerX",
                out reason);
        }

        public static bool VerifyFrontPickerYMove(MotionGuardRuleContext request, out string reason)
        {
            return VerifyPickerYMove(
                request,
                true,
                "FrontPickerY",
                out reason);
        }

        public static bool VerifyRearPickerYMove(MotionGuardRuleContext request, out string reason)
        {
            return VerifyPickerYMove(
                request,
                false,
                "RearPickerY",
                out reason);
        }

        public static bool VerifyFacingYDistanceFirst(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (request == null || request.Machine == null)
                    return true;

                if (!IsAxisMotionRequest(request.MoveKind))
                    return true;

                bool isFront;
                PickerAxis axis;
                string movingName;
                if (!TryResolvePickerXYRequest(request, out isFront, out axis, out movingName))
                    return true;

                string detail;
                bool allowed = CanMovePickerAxisByFacingYInterlock(
                    request.Machine,
                    isFront,
                    axis,
                    request.TargetValue,
                    request.TargetName,
                    null,
                    null,
                    out detail);
                if (allowed)
                    return true;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 1차 거리 인터락 차단: Front/Rear PickerX 거리와 PickerY 돌출 상태가 안전하지 않습니다. " +
                    "moveKind=" + request.MoveKind +
                    ", originalMoveKind=" + request.OriginalMoveKind +
                    ", executionMode=" + request.ExecutionMode +
                    ", target=" + request.TargetValue.ToString("0.###") +
                    ", targetName=" + (string.IsNullOrWhiteSpace(request.TargetName) ? "-" : request.TargetName) +
                    ", detail=" + detail,
                    out reason);
            }
            catch (Exception ex)
            {
                string movingName = request != null ? request.MovingName : "Picker";
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Front/Rear PickerY 돌출 X거리 1차 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        private static bool IsAxisMotionRequest(MotionGuardMoveKind moveKind)
        {
            return moveKind == MotionGuardMoveKind.AxisMove ||
                   moveKind == MotionGuardMoveKind.AxisHome ||
                   moveKind == MotionGuardMoveKind.AxisTeachingMove ||
                   moveKind == MotionGuardMoveKind.AxisContinuousJog ||
                   moveKind == MotionGuardMoveKind.AxisStepJog;
        }

        private static bool TryResolvePickerXYRequest(
            MotionGuardRuleContext request,
            out bool isFront,
            out PickerAxis axis,
            out string movingName)
        {
            isFront = false;
            axis = PickerAxis.PickerX;
            movingName = request != null ? request.MovingName : string.Empty;

            if (request == null)
                return false;

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerX"))
            {
                isFront = true;
                axis = PickerAxis.PickerX;
                movingName = "FrontPickerX";
                return true;
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerY"))
            {
                isFront = true;
                axis = PickerAxis.PickerY;
                movingName = "FrontPickerY";
                return true;
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerX"))
            {
                isFront = false;
                axis = PickerAxis.PickerX;
                movingName = "RearPickerX";
                return true;
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerY"))
            {
                isFront = false;
                axis = PickerAxis.PickerY;
                movingName = "RearPickerY";
                return true;
            }

            return false;
        }

        private static bool VerifyPickerXMove(
            MotionGuardRuleContext request,
            bool isFront,
            string movingName,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                if (request == null || request.Machine == null)
                    return true;

                BaseAxis ownX = GetPickerX(request.Machine, isFront);
                BaseAxis ownY = GetPickerY(request.Machine, isFront);
                PickerWorkZone currentZone = ResolveCurrentXZoneWithContext(request.Machine, isFront);
                PickerWorkZone targetZone = ResolveTargetXZoneWithContext(
                    request.Machine,
                    isFront,
                    request.TargetValue,
                    request.TargetName);
                if (currentZone == PickerWorkZone.Unknown && targetZone != PickerWorkZone.Unknown)
                {
                    PickerWorkZone currentYZone = ResolveCurrentYZone(request.Machine, isFront);
                    if (IsSameInterlockZone(currentYZone, targetZone))
                        currentZone = currentYZone;
                }

                if (!VerifyInputStageZSafeForInputZone(
                    request.Machine,
                    isFront,
                    movingName,
                    "X",
                    currentZone,
                    targetZone,
                    ownX,
                    ownY,
                    out reason))
                    return false;

                string occupiedOwner;
                if (targetZone == PickerWorkZone.Input &&
                    IsOtherPickerWorkAreaActive(isFront, targetZone, out occupiedOwner))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(
                            movingName,
                            "Input 픽업 영역을 반대 픽커가 사용 중입니다. owner=" + occupiedOwner,
                            ownX,
                            ownY,
                            currentZone,
                            targetZone),
                        out reason);
                }

                if (targetZone != PickerWorkZone.Unknown &&
                    targetZone != PickerWorkZone.Input &&
                    !IsAvoidZone(targetZone) &&
                    IsOtherPickerWorkAreaActive(isFront, targetZone, out occupiedOwner))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(
                            movingName,
                            targetZone + " 작업 영역을 반대 픽커가 사용 중입니다. owner=" + occupiedOwner,
                            ownX,
                            ownY,
                            currentZone,
                            targetZone),
                        out reason);
                }

                if (IsAvoidZone(targetZone))
                {
                    if (IsPickerYAtAvoid(request.Machine, isFront))
                        return true;

                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(movingName, "Avoid/InputAvoid/OutputAvoid 위치로 X축 이동하려면 자기 PickerY가 Avoid 또는 0 위치여야 합니다.", ownX, ownY, currentZone, targetZone),
                        out reason);
                }

                if (targetZone == PickerWorkZone.Unknown)
                {
                    if (IsPickerYAtAvoid(request.Machine, isFront))
                        return true;

                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(movingName, "X축 목표 존을 판단할 수 없습니다. 자유 X 이동 전 PickerY가 Avoid 또는 0 위치여야 합니다.", ownX, ownY, currentZone, targetZone),
                        out reason);
                }

                string fineAlignDetail;
                if (MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, isFront, out fineAlignDetail))
                {
                    string fineFacingDetail;
                    if (!CanMovePickerXByFacingYInterlock(
                        request.Machine,
                        isFront,
                        request.TargetValue,
                        null,
                        request.TargetName,
                        out fineFacingDetail))
                    {
                        return MotionGuardRuleHelpers.Block(
                            movingName,
                            movingName + " ColletCalibrationFineAlign 이동 불가: " + fineFacingDetail,
                            out reason);
                    }

                    return true;
                }

                bool pickerYAtAvoid = IsPickerYAtAvoid(request.Machine, isFront);
                bool inspectionContinuousProcessMove = IsInspectionContinuousProcessMove(request, currentZone, targetZone);
                bool autoProcessCorrectionXMove = false;
                string autoProcessCorrectionReason;
                if (!pickerYAtAvoid &&
                    !inspectionContinuousProcessMove &&
                    IsAutoProcessCorrectionXMove(request))
                {
                    if (!CanMoveAutoProcessCorrectionX(request, isFront, ownX, ownY, currentZone, targetZone, out autoProcessCorrectionReason))
                    {
                        return MotionGuardRuleHelpers.Block(
                            movingName,
                            BuildXBlockedMessage(movingName, "오토 공정 보정 X 이동 불가: " + autoProcessCorrectionReason, ownX, ownY, currentZone, targetZone),
                            out reason);
                    }

                    autoProcessCorrectionXMove = true;
                }

                if (!autoProcessCorrectionXMove &&
                    !inspectionContinuousProcessMove &&
                    !pickerYAtAvoid)
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(movingName, "메뉴얼/단독 X축 이동 전 PickerY가 Avoid 또는 0 위치여야 합니다. 오토 검사 연속 이동은 InspectionContinuous 태그가 있을 때만 예외입니다.", ownX, ownY, currentZone, targetZone),
                        out reason);
                }

                if (!IsSameInterlockZone(currentZone, targetZone) &&
                    !pickerYAtAvoid &&
                    !autoProcessCorrectionXMove &&
                    !inspectionContinuousProcessMove)
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(movingName, "존 사이를 이동하거나 현재 존을 판단할 수 없을 때는 PickerY가 Avoid 또는 0 위치여야 합니다.", ownX, ownY, currentZone, targetZone),
                        out reason);
                }

                string facingDetail;
                if (!CanMovePickerXByFacingYInterlock(
                    request.Machine,
                    isFront,
                    request.TargetValue,
                    null,
                    request.TargetName,
                    out facingDetail))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: " + facingDetail,
                        out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 존 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
        }

        private static bool IsAutoProcessCorrectionXMove(MotionGuardRuleContext request)
        {
            return request != null &&
                   request.Intent != null &&
                   request.Intent.AutoProcessCorrection;
        }

        private static bool CanMoveAutoProcessCorrectionX(
            MotionGuardRuleContext request,
            bool isFront,
            BaseAxis ownX,
            BaseAxis ownY,
            PickerWorkZone currentZone,
            PickerWorkZone targetZone,
            out string reason)
        {
            reason = string.Empty;

            if (!IsRealEquipmentOrDryRunAutoProcess())
            {
                reason = "실장비 또는 드라이런 오토 시퀀스에서만 허용됩니다.";
                return false;
            }

            if (request == null || ownX == null)
            {
                reason = "축 상태를 확인할 수 없습니다.";
                return false;
            }

            if (targetZone == PickerWorkZone.Unknown || IsAvoidZone(targetZone))
            {
                reason = "목표 존이 공정 존이 아닙니다. targetZone=" + targetZone;
                return false;
            }

            if (!IsSameInterlockZone(currentZone, targetZone) && !IsAvoidZone(currentZone))
            {
                reason = "현재 존과 목표 존이 다릅니다. currentZone=" + currentZone + ", targetZone=" + targetZone;
                return false;
            }

            if (IsAutoProcessZoneEntryWithPickerYReady(request.Machine, isFront, ownY, targetZone))
                return true;

            double maxDistance = ResolveAutoProcessCorrectionMaxDistance(request);
            double delta = Math.Abs(request.TargetValue - ownX.ActualPosition);
            if (delta > maxDistance)
            {
                reason = "보정 이동량이 허용치를 초과했습니다. delta=" + delta.ToString("0.###") +
                         "mm, max=" + maxDistance.ToString("0.###") + "mm";
                return false;
            }

            return true;
        }

        private static bool IsAutoProcessZoneEntryWithPickerYReady(
            CDT320_Machine machine,
            bool isFront,
            BaseAxis ownY,
            PickerWorkZone targetZone)
        {
            if (machine == null || ownY == null)
                return false;

            string yPositionName = ResolvePickerYProcessPositionName(targetZone);
            if (string.IsNullOrWhiteSpace(yPositionName))
                return false;

            return IsAtPickerZonePosition(
                machine,
                isFront,
                PickerAxis.PickerY,
                yPositionName,
                ownY.ActualPosition,
                DefaultAutoProcessZoneEntryYTolerance);
        }

        private static string ResolvePickerYProcessPositionName(PickerWorkZone targetZone)
        {
            switch (targetZone)
            {
                case PickerWorkZone.Input:
                    return "PickPosition";
                case PickerWorkZone.Bottom:
                    return "BottomPosition";
                case PickerWorkZone.Side:
                    return "SidePosition";
                case PickerWorkZone.Output:
                    return "PlacePosition";
                default:
                    return string.Empty;
            }
        }

        private static bool IsRealEquipmentOrDryRunAutoProcess()
        {
            try
            {
                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings == null)
                    return false;

                return settings.UseAjin && (settings.DryRunMode || !settings.SimulationMode);
            }
            catch
            {
                return false;
            }
        }

        private static double ResolveAutoProcessCorrectionMaxDistance(MotionGuardRuleContext request)
        {
            if (request != null &&
                request.Intent != null &&
                request.Intent.AutoProcessCorrectionMax.HasValue &&
                request.Intent.AutoProcessCorrectionMax.Value > 0.0)
                return request.Intent.AutoProcessCorrectionMax.Value;

            return DefaultAutoProcessCorrectionMaxDistance;
        }

        private static bool IsInspectionContinuousProcessMove(
            MotionGuardRuleContext request,
            PickerWorkZone currentZone,
            PickerWorkZone targetZone)
        {
            if (request == null || request.Intent == null || !request.Intent.InspectionContinuous)
                return false;

            PickerWorkZone declaredFrom;
            PickerWorkZone declaredTo;
            if (TryResolveInspectionContinuousTransition(request, out declaredFrom, out declaredTo) &&
                IsSameInterlockZone(declaredTo, targetZone) &&
                IsAllowedInspectionContinuousTransition(declaredFrom, declaredTo))
                return true;

            // Auto 검사/Place 연속 동작에서는 같은 존 안에서 다음 다이로 X축만 이동할 수 있다.
            // 메뉴얼/단독 이동은 InspectionContinuous 태그가 없으므로 기존 Y Avoid 조건을 그대로 탄다.
            bool allowedTransition =
                (currentZone == PickerWorkZone.Input && targetZone == PickerWorkZone.Input) ||
                (currentZone == PickerWorkZone.Input && IsProcessZone(targetZone)) ||
                (IsProcessZone(currentZone) && IsProcessZone(targetZone)) ||
                (IsProcessZone(currentZone) && targetZone == PickerWorkZone.Output) ||
                (currentZone == PickerWorkZone.Output && IsProcessZone(targetZone)) ||
                (currentZone == PickerWorkZone.Output && targetZone == PickerWorkZone.Output);

            if (!allowedTransition)
                return false;

            return true;
        }

        private static bool TryResolveInspectionContinuousTransition(
            MotionGuardRuleContext request,
            out PickerWorkZone from,
            out PickerWorkZone to)
        {
            from = PickerWorkZone.Unknown;
            to = PickerWorkZone.Unknown;

            if (request == null || request.Intent == null)
                return false;

            from = request.Intent.InspectionFromZone;
            to = request.Intent.InspectionToZone;
            return from != PickerWorkZone.Unknown && to != PickerWorkZone.Unknown;
        }

        private static bool IsAllowedInspectionContinuousTransition(PickerWorkZone from, PickerWorkZone to)
        {
            return (from == PickerWorkZone.Input && to == PickerWorkZone.Input) ||
                   (from == PickerWorkZone.Input && IsProcessZone(to)) ||
                   (IsProcessZone(from) && IsProcessZone(to)) ||
                   (IsProcessZone(from) && to == PickerWorkZone.Output) ||
                   (from == PickerWorkZone.Output && IsProcessZone(to)) ||
                   (from == PickerWorkZone.Output && to == PickerWorkZone.Output);
        }

        private static bool TryReadTargetNameString(string targetName, string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(targetName) || string.IsNullOrWhiteSpace(key))
                return false;

            string[] tokens = targetName.Split(';');
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i] != null ? tokens[i].Trim() : string.Empty;
                if (token.Length == 0)
                    continue;

                int equal = token.IndexOf('=');
                if (equal <= 0)
                    continue;

                string tokenKey = token.Substring(0, equal).Trim();
                if (!string.Equals(tokenKey, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                value = token.Substring(equal + 1).Trim();
                return value.Length > 0;
            }

            return false;
        }

        private static PickerWorkZone ParseWorkZoneToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return PickerWorkZone.Unknown;

            string normalized = value.Trim();
            if (string.Equals(normalized, "Input", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Input;
            if (string.Equals(normalized, "Process", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Inspect", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Inspection", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Bottom;
            if (string.Equals(normalized, "Bottom", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Bottom;
            if (string.Equals(normalized, "Side", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Side;
            if (string.Equals(normalized, "Place", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Output", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Output;
            if (string.Equals(normalized, "Avoid", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Avoid;

            return PickerWorkZone.Unknown;
        }

        private static bool VerifyPickerYMove(
            MotionGuardRuleContext request,
            bool isFront,
            string movingName,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                if (request == null || request.Machine == null)
                    return true;

                PickerWorkZone targetZone = ResolveTargetYZone(request, isFront);
                PickerWorkZone currentXZone = ResolveCurrentXZoneWithContext(request.Machine, isFront);
                if (!VerifyInputStageZSafeForInputZone(
                    request.Machine,
                    isFront,
                    movingName,
                    "Y",
                    currentXZone,
                    targetZone,
                    GetPickerX(request.Machine, isFront),
                    GetPickerY(request.Machine, isFront),
                    out reason))
                    return false;

                if (IsAvoidZone(targetZone))
                    return true;

                if (targetZone == PickerWorkZone.Unknown)
                {
                    if (IsPickerYTargetAvoid(request, isFront))
                        return true;

                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Y축 이동 불가: 목표 존을 판단할 수 없습니다. 존 이름이 있는 이동을 사용하거나 먼저 Y축을 Avoid로 이동해야 합니다. target=" +
                        request.TargetValue.ToString("0.###") + ", targetName=" + request.TargetName,
                        out reason);
                }

                string occupiedOwner;
                if (targetZone == PickerWorkZone.Input &&
                    IsOtherPickerWorkAreaActive(isFront, targetZone, out occupiedOwner))
                {
                    string otherActiveName = isFront ? "RearPicker" : "FrontPicker";
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Y축 전진 이동 불가: Input 픽업 영역을 " +
                        otherActiveName + "가 사용 중입니다. owner=" + occupiedOwner +
                        ", targetZone=" + targetZone +
                        ", target=" + request.TargetValue.ToString("0.###") +
                        ", targetName=" + request.TargetName,
                        out reason);
                }

                if (targetZone != PickerWorkZone.Unknown &&
                    targetZone != PickerWorkZone.Input &&
                    !IsAvoidZone(targetZone) &&
                    IsOtherPickerWorkAreaActive(isFront, targetZone, out occupiedOwner))
                {
                    string otherActiveName = isFront ? "RearPicker" : "FrontPicker";
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Y축 전진 이동 불가: " + targetZone +
                        " 작업 영역을 " + otherActiveName +
                        "가 사용 중입니다. owner=" + occupiedOwner +
                        ", targetZone=" + targetZone +
                        ", target=" + request.TargetValue.ToString("0.###") +
                        ", targetName=" + request.TargetName,
                        out reason);
                }

                bool otherFront = !isFront;
                if (!VerifyPickerYFacingXClearance(request, isFront, movingName, targetZone, out reason))
                    return false;

                PickerWorkZone otherActiveZone = GetActivePickerYTargetZone(otherFront);
                if (otherActiveZone != PickerWorkZone.Unknown)
                {
                    if (CanShareForwardY(targetZone, otherActiveZone))
                        return true;

                    string otherActiveName = isFront ? "RearPicker" : "FrontPicker";
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Y축 전진 이동 불가: " + otherActiveName +
                        "Y가 아직 이동 중이고 같은 존 또는 판단 불가 존입니다. 다른 존 병렬 동작은 허용하지만 같은 존 진입 전에는 상대 PickerY가 Avoid 위치여야 합니다. targetZone=" + targetZone +
                        ", otherActiveTargetZone=" + otherActiveZone +
                        ", target=" + request.TargetValue.ToString("0.###") +
                        ", targetName=" + request.TargetName,
                        out reason);
                }

                if (IsPickerYAtAvoid(request.Machine, otherFront))
                    return true;

                PickerWorkZone otherZone = ResolveCurrentXZoneWithContext(request.Machine, otherFront);
                if (CanShareForwardY(targetZone, otherZone))
                    return true;

                string otherName = isFront ? "RearPicker" : "FrontPicker";
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " Y축 전진 이동 불가: " + otherName +
                    "Y가 Avoid 위치가 아니고 같은 존 또는 판단 불가 존에 있습니다. 다른 존 병렬 동작은 허용하지만 같은 존 진입 전에는 상대 PickerY가 Avoid 위치여야 합니다. targetZone=" + targetZone +
                    ", otherZone=" + otherZone +
                    ", target=" + request.TargetValue.ToString("0.###") +
                    ", targetName=" + request.TargetName,
                    out reason);
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 존 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
        }

        private static bool VerifyPickerYFacingXClearance(
            MotionGuardRuleContext request,
            bool isFront,
            string movingName,
            PickerWorkZone targetZone,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                if (request == null || request.Machine == null)
                    return true;
                if (IsAvoidZone(targetZone))
                    return true;

                string detail;
                if (CanMovePickerYByFacingYInterlock(
                    request.Machine,
                    isFront,
                    request.TargetValue,
                    null,
                    request.TargetName,
                    out detail))
                {
                    return true;
                }

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " Y축 전진 이동 불가: " + detail,
                    out reason);
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " Y축 X거리 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
        }

        private static bool CanMovePickerYByFacingYInterlock(
            CDT320_Machine machine,
            bool isFront,
            double targetY,
            double? pairedXTarget,
            string targetName,
            out string detail)
        {
            detail = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                // 현재 기준: Y 목표가 Home(0) 또는 실제 Avoid이면 안전 복귀 이동이므로 허용한다.
                bool ownTargetOut = IsPickerYOutByPosition(machine, isFront, targetY);
                if (!ownTargetOut)
                    return true;

                bool otherFront = !isFront;
                // 현재 기준: 상대 PickerY가 실제/명령 기준 Home(0) 또는 Avoid가 아니면 X 안전거리 안에서 내 Y 전진을 차단한다.
                bool otherOut = IsPickerYOutOrMovingOut(machine, otherFront, null);
                if (!otherOut)
                    return true;

                BaseAxis ownX = GetPickerX(machine, isFront);
                BaseAxis ownY = GetPickerY(machine, isFront);
                BaseAxis otherX = GetPickerX(machine, otherFront);
                BaseAxis otherY = GetPickerY(machine, otherFront);
                if (ownX == null || otherX == null)
                    return true;

                double clearance = ResolvePickerYFacingXClearance(machine);
                if (clearance <= 0.0)
                    return true;

                double ownXTarget = pairedXTarget.HasValue ? pairedXTarget.Value : ResolveAxisPathTarget(ownX);
                double otherXTarget = ResolveAxisPathTarget(otherX);
                // 현재 기준: 현재 X 엔코더 또는 이동 경로가 안전거리 안으로 들어올 때만 Y 상호 회피 조건을 적용한다.
                if (!DoXMovePathsEnterFacingClearance(ownX.ActualPosition, ownXTarget, otherX.ActualPosition, otherXTarget, clearance))
                    return true;

                detail = BuildFacingYBlockedDetail(
                    isFront,
                    "Y축 전진",
                    targetName,
                    ownX,
                    ownY,
                    otherX,
                    otherY,
                    ownXTarget,
                    targetY,
                    otherXTarget,
                    clearance);
                return false;
            }
            catch (Exception ex)
            {
                detail = "Y축 전진 X거리 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static bool CanMovePickerXByFacingYInterlock(
            CDT320_Machine machine,
            bool isFront,
            double targetX,
            double? pairedYTarget,
            string targetName,
            out string detail)
        {
            detail = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                bool ownOut = IsPickerYOutOrMovingOut(machine, isFront, pairedYTarget);
                if (!ownOut)
                    return true;

                bool otherFront = !isFront;
                bool otherOut = IsPickerYOutOrMovingOut(machine, otherFront, null);
                if (!otherOut)
                    return true;

                BaseAxis ownX = GetPickerX(machine, isFront);
                BaseAxis ownY = GetPickerY(machine, isFront);
                BaseAxis otherX = GetPickerX(machine, otherFront);
                BaseAxis otherY = GetPickerY(machine, otherFront);
                if (ownX == null || otherX == null)
                    return true;

                double clearance = ResolvePickerYFacingXClearance(machine);
                if (clearance <= 0.0)
                    return true;

                double otherXTarget = ResolveAxisPathTarget(otherX);
                if (!DoXMovePathsEnterFacingClearance(ownX.ActualPosition, targetX, otherX.ActualPosition, otherXTarget, clearance))
                    return true;

                detail = BuildFacingYBlockedDetail(
                    isFront,
                    "X축 이동",
                    targetName,
                    ownX,
                    ownY,
                    otherX,
                    otherY,
                    targetX,
                    pairedYTarget.HasValue ? pairedYTarget.Value : (ownY != null ? ownY.ActualPosition : 0.0),
                    otherXTarget,
                    clearance);
                return false;
            }
            catch (Exception ex)
            {
                detail = "X축 이동 Y돌출 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static bool IsPickerYForwardOrMovingForward(CDT320_Machine machine, bool isFront)
        {
            return IsPickerYOutOrMovingOut(machine, isFront, null);
        }

        private static bool IsPickerYOutOrMovingOut(CDT320_Machine machine, bool isFront, double? targetY)
        {
            try
            {
                if (machine == null)
                    return false;

                if (targetY.HasValue && IsPickerYOutByPosition(machine, isFront, targetY.Value))
                    return true;

                PickerWorkZone activeTargetZone = GetActivePickerYTargetZone(isFront);
                if (activeTargetZone != PickerWorkZone.Unknown && !IsAvoidZone(activeTargetZone))
                    return true;

                BaseAxis y = GetPickerY(machine, isFront);
                if (y == null)
                    return false;

                if (IsPickerYOutByPosition(machine, isFront, y.ActualPosition))
                    return true;

                return y.IsMoving && !IsPickerYAtAvoid(machine, isFront);
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static bool IsPickerYOutByPosition(CDT320_Machine machine, bool isFront, double position)
        {
            try
            {
                double outDistance = ResolvePickerYOutDistance(machine);
                if (outDistance <= 0.0)
                    outDistance = DefaultPickerYOutDistance;

                return !IsPickerYSafeByPosition(machine, isFront, position, outDistance);
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static bool IsPickerYSafeByPosition(CDT320_Machine machine, bool isFront, double position, double outDistance)
        {
            // 현재 기준: X 안전거리 안에서 PickerY 안전 위치는 Home(0) 또는 실제 AvoidPosition만 인정한다.
            if (Math.Abs(position) <= outDistance)
                return true;

            if (IsNearPickerYTeachingPosition(machine, isFront, "AvoidPosition", position, outDistance))
                return true;

            // 기존 조건: InputAvoidPosition/OutputAvoidPosition도 PickerY 안전 위치로 보았다.
            // 현재 필요 여부: 사용 안 함. X 안전거리 안에서는 Input/OutputSideAvoid도 작업존 진입으로 보고 실제 Avoid만 안전 위치로 인정한다.
            //return IsNearPickerYTeachingPosition(machine, isFront, "InputAvoidPosition", position, outDistance) ||
            //       IsNearPickerYTeachingPosition(machine, isFront, "OutputAvoidPosition", position, outDistance);

            return false;
        }

        private static bool IsNearPickerYTeachingPosition(CDT320_Machine machine, bool isFront, string positionName, double position, double tolerance)
        {
            try
            {
                double target = GetPickerTeachingPosition(machine, isFront, PickerAxis.PickerY, positionName);
                return Math.Abs(position - target) <= tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static double ResolveAxisPathTarget(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return 0.0;

                return axis.IsMoving ? axis.CommandPosition : axis.ActualPosition;
            }
            catch
            {
                return axis != null ? axis.ActualPosition : 0.0;
            }
            finally
            {
            }
        }

        private static bool DoXMovePathsEnterFacingClearance(
            double startX,
            double targetX,
            double otherStartX,
            double otherTargetX,
            double clearance)
        {
            double min = Math.Min(startX, targetX) - clearance;
            double max = Math.Max(startX, targetX) + clearance;
            double otherMin = Math.Min(otherStartX, otherTargetX);
            double otherMax = Math.Max(otherStartX, otherTargetX);
            return otherMax >= min && otherMin <= max;
        }

        private static string BuildFacingYBlockedDetail(
            bool isFront,
            string moveName,
            string targetName,
            BaseAxis ownX,
            BaseAxis ownY,
            BaseAxis otherX,
            BaseAxis otherY,
            double ownTargetX,
            double ownTargetY,
            double otherTargetX,
            double clearance)
        {
            string otherName = isFront ? "RearPicker" : "FrontPicker";
            double distance = ownX != null && otherX != null
                ? Math.Abs(ownX.ActualPosition - otherX.ActualPosition)
                : 0.0;

            return moveName + " 불가: " + otherName +
                   "Y가 전진 상태이고 Front/Rear PickerX 엔코더 경로가 마주보는 안전거리 안에 있습니다. " +
                   "한쪽 PickerY를 실제 Avoid 또는 0 위치로 이동한 뒤 진행하세요. " +
                   "xDistance=" + distance.ToString("0.###") +
                   ", requiredClearance=" + clearance.ToString("0.###") +
                   ", ownX=" + FormatAxis(ownX) +
                   ", ownY=" + FormatAxis(ownY) +
                   ", otherX=" + FormatAxis(otherX) +
                   ", otherY=" + FormatAxis(otherY) +
                   ", targetX=" + ownTargetX.ToString("0.###") +
                   ", otherTargetX=" + otherTargetX.ToString("0.###") +
                   ", targetY=" + ownTargetY.ToString("0.###") +
                   ", targetName=" + (string.IsNullOrWhiteSpace(targetName) ? "-" : targetName);
        }

        private static double ResolvePickerYFacingXClearance(CDT320_Machine machine)
        {
            double front = 0.0;
            double rear = 0.0;

            if (machine != null && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Setup != null)
                front = machine.PickerFrontUnit.Setup.PickerYFacingXClearance;
            if (machine != null && machine.PickerRearUnit != null && machine.PickerRearUnit.Setup != null)
                rear = machine.PickerRearUnit.Setup.PickerYFacingXClearance;

            double configured = Math.Max(front, rear);
            return configured > 0.0 ? configured : DefaultPickerYFacingXClearance;
        }

        private static double ResolvePickerYOutDistance(CDT320_Machine machine)
        {
            double front = 0.0;
            double rear = 0.0;

            if (machine != null && machine.PickerFrontUnit != null && machine.PickerFrontUnit.Setup != null)
                front = machine.PickerFrontUnit.Setup.PickerYOutDistance;
            if (machine != null && machine.PickerRearUnit != null && machine.PickerRearUnit.Setup != null)
                rear = machine.PickerRearUnit.Setup.PickerYOutDistance;

            double configured = Math.Max(front, rear);
            return configured > 0.0 ? configured : DefaultPickerYOutDistance;
        }

        public static bool CanShareForwardY(PickerWorkZone targetZone, PickerWorkZone otherZone)
        {
            if (targetZone == PickerWorkZone.Unknown || otherZone == PickerWorkZone.Unknown)
                return false;
            if (IsAvoidZone(targetZone) || IsAvoidZone(otherZone))
                return true;

            return !IsSameInterlockZone(targetZone, otherZone);
        }

        private static bool VerifyInputStageZSafeForInputZone(
            CDT320_Machine machine,
            bool isFront,
            string movingName,
            string moveAxisName,
            PickerWorkZone currentZone,
            PickerWorkZone targetZone,
            BaseAxis ownX,
            BaseAxis ownY,
            out string reason)
        {
            reason = string.Empty;

            if (currentZone != PickerWorkZone.Input && targetZone != PickerWorkZone.Input)
                return true;

            // 현재 기준: Picker가 Input 쪽으로 들어가거나 Input 존에 있으면 Picker Z0~Z3는 Avoid 또는 0 이상 위치여야 한다.
            if (!VerifyPickerZHomeOrAvoidForInputZone(machine, isFront, movingName, out reason))
                return false;

            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (stage == null)
                return true;

            // 현재 기준: Picker가 Input 쪽으로 들어가거나 Input 존에 있으면 InputExpandingZ가 0 이하 위치여야 한다.
            if (IsInputExpanderZAtOrBelowZero(stage))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " " + moveAxisName +
                " 이동 불가: Picker가 Input 존에 있거나 Input 존으로 이동하려는데 InputExpandingZ가 0 이하가 아닙니다. " +
                "currentZone=" + currentZone +
                ", targetZone=" + targetZone +
                ", xActual=" + FormatAxis(ownX) +
                ", yActual=" + FormatAxis(ownY) +
                ", " + BuildInputStageZState(stage),
                out reason);
        }

        private static bool IsInputExpanderZAtOrBelowZero(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null)
                return false;

            double tolerance = ResolveTolerance(stage.ExpanderZ);
            double actual = stage.ExpanderZ.ActualPosition;
            return actual <= tolerance;
        }

        // 기존 조건: InputExpandingZ가 Avoid/Process/Ready 위치면 Picker Input 존 이동을 허용했다.
        // 현재 필요 여부: 사용 안 함. 현재 기준은 InputExpandingZ actual <= 0 이다.
        //private static bool IsInputStageZAtAvoidProcessOrReady(InputStageUnit stage)
        //{
        //    if (stage == null || stage.ExpanderZ == null || stage.Recipe == null || stage.Recipe.WaferZ == null)
        //        return false;
        //
        //    double tolerance = ResolveTolerance(stage.ExpanderZ);
        //    double actual = stage.ExpanderZ.ActualPosition;
        //    StageAxisPositions waferZ = stage.Recipe.WaferZ;
        //
        //    return Math.Abs(actual - waferZ.AvoidPosition) <= tolerance ||
        //           Math.Abs(actual - waferZ.ProcessPosition) <= tolerance ||
        //           Math.Abs(actual - waferZ.ReadyPosition) <= tolerance;
        //}

        private static bool VerifyPickerZHomeOrAvoidForInputZone(
            CDT320_Machine machine,
            bool isFront,
            string movingName,
            out string reason)
        {
            reason = string.Empty;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = GetPickerZ(machine, isFront, zAxis);
                if (axis == null)
                    continue;

                if (axis.IsMoving)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Input 진입 불가: " + BuildPickerSideName(isFront) + zAxis + " 축이 이동 중입니다.",
                        out reason);

                if (!IsPickerZHomeOrAvoid(machine, isFront, zAxis, axis))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Input 진입 불가: " + BuildPickerSideName(isFront) + zAxis + " 축이 Avoid 또는 0 이상 위치가 아닙니다. actual=" +
                        axis.ActualPosition.ToString("0.###"),
                        out reason);
            }

            return true;
        }

        private static bool IsPickerZHomeOrAvoid(CDT320_Machine machine, bool isFront, PickerAxis zAxis, BaseAxis axis)
        {
            if (axis == null)
                return true;

            double tolerance = ResolveTolerance(axis);
            if (axis.ActualPosition >= -tolerance)
                return true;

            if (isFront)
            {
                PickerFrontUnit picker = machine != null ? machine.PickerFrontUnit : null;
                return picker != null && picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition");
            }

            PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
            return rear != null && rear.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition");
        }

        private static string BuildPickerSideName(bool isFront)
        {
            return isFront ? "Front" : "Rear";
        }

        private static string BuildInputStageZState(InputStageUnit stage)
        {
            if (stage == null)
                return "InputStage=null";
            if (stage.ExpanderZ == null)
                return "InputExpandingZ=null";
            if (stage.Recipe == null || stage.Recipe.WaferZ == null)
                return "InputExpandingZ actual=" + stage.ExpanderZ.ActualPosition.ToString("0.###") +
                       ", recipe.WaferZ=null";

            StageAxisPositions waferZ = stage.Recipe.WaferZ;
            return "InputExpandingZ actual=" + stage.ExpanderZ.ActualPosition.ToString("0.###") +
                   ", avoid=" + waferZ.AvoidPosition.ToString("0.###") +
                   ", process=" + waferZ.ProcessPosition.ToString("0.###") +
                   ", tolerance=" + ResolveTolerance(stage.ExpanderZ).ToString("0.###");
        }

        private static string BuildXBlockedMessage(
            string movingName,
            string detail,
            BaseAxis ownX,
            BaseAxis ownY,
            PickerWorkZone currentZone,
            PickerWorkZone targetZone)
        {
            return movingName + " 이동 불가: " + detail +
                   " currentZone=" + currentZone +
                   ", targetZone=" + targetZone +
                   ", xActual=" + FormatAxis(ownX) +
                   ", yActual=" + FormatAxis(ownY) + ".";
        }

        private static string FormatAxis(BaseAxis axis)
        {
            return axis != null ? axis.ActualPosition.ToString("0.###") : "<null>";
        }

        private static PickerWorkZone ResolveTargetXZone(MotionGuardRuleContext request, bool isFront)
        {
            return ResolveTargetXZoneWithContext(
                request != null ? request.Machine : null,
                isFront,
                request != null ? (double?)request.TargetValue : null,
                request != null ? request.TargetName : string.Empty);
        }

        private static PickerWorkZone ResolveTargetXZoneWithContext(
            CDT320_Machine machine,
            bool isFront,
            double? targetX,
            string targetName)
        {
            if (targetX.HasValue)
            {
                PickerWorkZone byPosition = ResolveXZoneByPositionWithContext(machine, isFront, targetX.Value);
                if (byPosition != PickerWorkZone.Unknown)
                    return byPosition;

                // 현재 기준: encoder zone 사용 중이면 X target 존은 설정 range만 믿고, range 밖이면 Unknown으로 차단 쪽에 맡긴다.
                if (IsPickerXEncoderZoneConfigured(machine, isFront))
                    return PickerWorkZone.Unknown;
            }

            return ParseZone(targetName);
        }

        private static PickerWorkZone ResolvePickerXZoneByNameOrPosition(
            CDT320_Machine machine,
            bool isFront,
            double? position,
            string targetName)
        {
            if (position.HasValue)
            {
                PickerWorkZone byPosition = ResolveXZoneByPosition(machine, isFront, position.Value);
                if (byPosition != PickerWorkZone.Unknown)
                    return byPosition;

                // 현재 기준: encoder zone이 켜져 있으면 targetName fallback으로 X 존을 덮어쓰지 않는다.
                if (IsPickerXEncoderZoneConfigured(machine, isFront))
                    return PickerWorkZone.Unknown;
            }

            return ParseZone(targetName);
        }

        internal static PickerWorkZone ResolveManualPickerXTargetZone(MotionGuardRuleContext request, bool isFront)
        {
            return ResolveManualPickerXZone(
                request != null ? request.Machine : null,
                isFront,
                request != null ? (double?)request.TargetValue : null,
                request != null ? request.TargetName : string.Empty);
        }

        internal static PickerWorkZone ResolveManualPickerXCurrentZone(CDT320_Machine machine, bool isFront)
        {
            BaseAxis x = GetPickerX(machine, isFront);
            return ResolveManualPickerXZone(
                machine,
                isFront,
                x != null ? (double?)x.ActualPosition : null,
                string.Empty);
        }

        internal static bool IsManualPickerXProcessZone(PickerWorkZone zone)
        {
            return zone == PickerWorkZone.Bottom || zone == PickerWorkZone.Side;
        }

        private static PickerWorkZone ResolveManualPickerXZone(
            CDT320_Machine machine,
            bool isFront,
            double? position,
            string targetName)
        {
            return ResolvePickerXZoneByNameOrPosition(machine, isFront, position, targetName);
        }

        private static PickerWorkZone ParseManualPickerXZone(string targetName)
        {
            string name = (targetName ?? string.Empty).Replace(" ", string.Empty);
            if (name.Length == 0)
                return PickerWorkZone.Unknown;

            if (Contains(name, "PickerZone=Input") ||
                Contains(name, "DiePick") ||
                Contains(name, "InputAvoidPosition") ||
                Contains(name, "PickPosition"))
                return PickerWorkZone.Input;
            if (Contains(name, "PickerZone=Output") ||
                Contains(name, "DiePlace") ||
                Contains(name, "OutputAvoidPosition") ||
                Contains(name, "PlacePosition"))
                return PickerWorkZone.Output;
            if (Contains(name, "PickerZone=Process") ||
                Contains(name, "PickerZone=Inspect") ||
                Contains(name, "PickerZone=Inspection") ||
                Contains(name, "PickerZone=Bottom") ||
                Contains(name, "PickerZone=Side") ||
                Contains(name, "DieBottom") ||
                Contains(name, "DieSide") ||
                Contains(name, "BottomPosition") ||
                Contains(name, "SidePosition") ||
                Contains(name, "INSPECT_B") ||
                Contains(name, "INSPECT_S"))
                return PickerWorkZone.Bottom;
            if (Contains(name, "PickerZone=Avoid") ||
                Contains(name, "AvoidPosition") ||
                Contains(name, "SafeRetreat"))
                return PickerWorkZone.Avoid;

            return PickerWorkZone.Unknown;
        }

        private static PickerWorkZone ResolveManualPickerXZoneByPosition(CDT320_Machine machine, bool isFront, double position)
        {
            if (machine == null)
                return PickerWorkZone.Unknown;

            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "AvoidPosition", position))
                return PickerWorkZone.Avoid;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "InputAvoidPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "OutputAvoidPosition", position) ||
                IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "PlacePosition", position))
                return PickerWorkZone.Output;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "BottomPosition", position) ||
                IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "SidePosition", position) ||
                IsManualPickerXInProcessRange(machine, isFront, position))
                return PickerWorkZone.Bottom;
            if (IsPickerTargetBelowAvoidPosition(machine, isFront, PickerAxis.PickerX, position))
                return PickerWorkZone.Input;
            if (IsManualPickerXOutputSide(machine, isFront, position))
                return PickerWorkZone.Output;

            return PickerWorkZone.Unknown;
        }

        private static bool IsManualPickerXInProcessRange(CDT320_Machine machine, bool isFront, double position)
        {
            BaseAxis x = GetPickerX(machine, isFront);
            double tolerance = ResolveTolerance(x);
            double min = double.MaxValue;
            double max = double.MinValue;

            AddManualPickerXProcessBoundary(machine, isFront, "BottomPosition", ref min, ref max);
            AddManualPickerXProcessBoundary(machine, isFront, "SidePosition", ref min, ref max);
            for (int i = 0; i < 4; i++)
            {
                AddManualPickerXProcessBoundary(machine, isFront, "BottomPosition", i, ref min, ref max);
                AddManualPickerXProcessBoundary(machine, isFront, "SidePosition", i, ref min, ref max);
            }

            if (min == double.MaxValue || max == double.MinValue)
                return false;

            return position >= min - tolerance && position <= max + tolerance;
        }

        private static void AddManualPickerXProcessBoundary(
            CDT320_Machine machine,
            bool isFront,
            string positionName,
            ref double min,
            ref double max)
        {
            double position = GetPickerTeachingPosition(machine, isFront, PickerAxis.PickerX, positionName);
            min = Math.Min(min, position);
            max = Math.Max(max, position);
        }

        private static void AddManualPickerXProcessBoundary(
            CDT320_Machine machine,
            bool isFront,
            string positionName,
            int pickerIndex,
            ref double min,
            ref double max)
        {
            double position = GetPickerTeachingPosition(machine, isFront, PickerAxis.PickerX, positionName) +
                              GetRuntimePickerZoneOffset(machine, isFront, PickerAxis.PickerX, pickerIndex);
            min = Math.Min(min, position);
            max = Math.Max(max, position);
        }

        private static bool IsManualPickerXOutputSide(CDT320_Machine machine, bool isFront, double position)
        {
            BaseAxis x = GetPickerX(machine, isFront);
            double tolerance = ResolveTolerance(x);
            double sideMax = GetPickerTeachingPosition(machine, isFront, PickerAxis.PickerX, "SidePosition");
            for (int i = 0; i < 4; i++)
            {
                double side = GetPickerTeachingPosition(machine, isFront, PickerAxis.PickerX, "SidePosition") +
                              GetRuntimePickerZoneOffset(machine, isFront, PickerAxis.PickerX, i);
                sideMax = Math.Max(sideMax, side);
            }

            return position > sideMax + tolerance;
        }

        private static PickerWorkZone ResolveTargetYZone(MotionGuardRuleContext request, bool isFront)
        {
            PickerWorkZone byName = ParseZone(request != null ? request.TargetName : string.Empty);
            if (byName != PickerWorkZone.Unknown)
                return byName;

            PickerWorkZone byPosition = ResolveYZoneByPosition(
                request != null ? request.Machine : null,
                isFront,
                request != null ? request.TargetValue : 0.0);
            if (byPosition != PickerWorkZone.Unknown)
                return byPosition;

            PickerWorkZone currentXZone = ResolveCurrentXZoneWithContext(request != null ? request.Machine : null, isFront);
            if (currentXZone != PickerWorkZone.Unknown && !IsAvoidZone(currentXZone))
                return currentXZone;

            return PickerWorkZone.Unknown;
        }

        private static PickerWorkZone ResolveCurrentXZone(CDT320_Machine machine, bool isFront)
        {
            BaseAxis x = GetPickerX(machine, isFront);
            if (x == null)
                return PickerWorkZone.Unknown;

            return ResolveXZoneByPosition(machine, isFront, x.ActualPosition);
        }

        private static PickerWorkZone ResolveCurrentXZoneWithContext(CDT320_Machine machine, bool isFront)
        {
            try
            {
                BaseAxis x = GetPickerX(machine, isFront);
                if (x == null)
                    return PickerWorkZone.Unknown;

                PickerWorkZone xZone = ResolveXZoneByPosition(machine, isFront, x.ActualPosition);
                if (xZone != PickerWorkZone.Unknown)
                    return xZone;

                PickerWorkZone resourceZone;
                string owner;
                if (TryGetPickerWorkArea(isFront, out resourceZone, out owner) &&
                    resourceZone != PickerWorkZone.Unknown &&
                    resourceZone != PickerWorkZone.Avoid)
                {
                    return resourceZone;
                }

                PickerWorkZone activeYZone = GetActivePickerYTargetZone(isFront);
                if (activeYZone != PickerWorkZone.Unknown && activeYZone != PickerWorkZone.Avoid)
                    return activeYZone;

                PickerWorkZone yZone = ResolveCurrentYZone(machine, isFront);
                if (yZone != PickerWorkZone.Unknown && yZone != PickerWorkZone.Avoid)
                    return yZone;

                return ResolveXZoneByPositionWithContext(machine, isFront, x.ActualPosition);
            }
            catch
            {
                return ResolveCurrentXZone(machine, isFront);
            }
            finally
            {
            }
        }

        private static PickerWorkZone ResolveXZoneByPositionWithContext(CDT320_Machine machine, bool isFront, double position)
        {
            PickerWorkZone zone = ResolveXZoneByPosition(machine, isFront, position);
            if (zone != PickerWorkZone.Unknown)
                return zone;

            PickerWorkZone yZone = ResolveCurrentYZone(machine, isFront);
            if (yZone != PickerWorkZone.Unknown && yZone != PickerWorkZone.Avoid)
                return yZone;

            PickerWorkZone activeYZone = GetActivePickerYTargetZone(isFront);
            if (activeYZone != PickerWorkZone.Unknown && activeYZone != PickerWorkZone.Avoid)
                return activeYZone;

            PickerWorkZone resourceZone;
            string owner;
            if (TryGetPickerWorkArea(isFront, out resourceZone, out owner) &&
                resourceZone != PickerWorkZone.Unknown &&
                resourceZone != PickerWorkZone.Avoid &&
                !IsPickerYAtAvoid(machine, isFront))
            {
                return resourceZone;
            }

            return PickerWorkZone.Unknown;
        }

        private static PickerWorkZone ResolveCurrentYZone(CDT320_Machine machine, bool isFront)
        {
            BaseAxis y = GetPickerY(machine, isFront);
            if (y == null)
                return PickerWorkZone.Unknown;

            return ResolveYZoneByPosition(machine, isFront, y.ActualPosition);
        }

        private static PickerWorkZone ResolveXZoneByPosition(CDT320_Machine machine, bool isFront, double position)
        {
            if (machine == null)
                return PickerWorkZone.Unknown;

            bool encoderConfigured;
            PickerWorkZone byEncoder;
            if (TryResolveEncoderXZoneByPosition(machine, isFront, position, out byEncoder, out encoderConfigured))
                return byEncoder;
            if (encoderConfigured)
            {
                // 현재 기준: encoder zone 사용 중에는 Picker X Zone Setup range가 유일한 X 존 기준이다.
                // 기존 조건: range 미검출 시 티칭/avoid 기준으로 Input/Process/Output fallback을 적용했다.
                // 현재 필요 여부: 사용 안 함. range 밖 또는 overlap은 Unknown으로 두어 상위 인터락이 보수적으로 차단한다.
                //PickerWorkZone byTeaching = ResolvePickerXTeachingZoneByPosition(machine, isFront, position);
                //if (byTeaching != PickerWorkZone.Unknown)
                //    return byTeaching;

                return PickerWorkZone.Unknown;
            }

            return ResolvePickerXTeachingZoneByPosition(machine, isFront, position);
        }

        private static PickerWorkZone ResolvePickerXTeachingZoneByPosition(CDT320_Machine machine, bool isFront, double position)
        {
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "AvoidPosition", position))
                return PickerWorkZone.Avoid;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "InputAvoidPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "OutputAvoidPosition", position))
                return PickerWorkZone.Output;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "PickPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "BottomPosition", position))
                return PickerWorkZone.Bottom;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "SidePosition", position))
                return PickerWorkZone.Side;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "PlacePosition", position))
                return PickerWorkZone.Output;
            // 현재 기준: Bottom 시작부터 Side 완료까지의 X 범위는 검사(Process) 존으로 본다.
            if (IsManualPickerXInProcessRange(machine, isFront, position))
                return PickerWorkZone.Bottom;
            // 현재 기준: 정확한 티칭 존이 아닌 상태에서 AvoidPosition보다 작은 X 목표는 Input 쪽 진입으로 본다.
            if (IsPickerTargetBelowAvoidPosition(machine, isFront, PickerAxis.PickerX, position))
                return PickerWorkZone.Input;
            // 현재 기준: Side 검사 완료 위치보다 큰 X 목표는 Output 쪽 진입으로 본다.
            if (IsManualPickerXOutputSide(machine, isFront, position))
                return PickerWorkZone.Output;

            return PickerWorkZone.Unknown;
        }

        private static bool TryResolveEncoderXZoneByPosition(
            CDT320_Machine machine,
            bool isFront,
            double position,
            out PickerWorkZone zone,
            out bool configured)
        {
            zone = PickerWorkZone.Unknown;
            configured = false;

            try
            {
                PickerZoneXSetup setup = GetPickerZoneXSetup(machine, isFront);
                if (setup == null || !setup.UseEncoderZone)
                    return false;

                setup.Ensure();
                double tolerance = setup.ZoneTolerance > 0.0 ? setup.ZoneTolerance : DefaultTolerance;
                int matchCount = 0;
                string matches = string.Empty;

                if (IsInZone(setup.Avoid, position, tolerance))
                {
                    SetEncoderZoneMatch(PickerWorkZone.Avoid, ref zone, ref matchCount);
                    AppendEncoderZoneMatch(ref matches, PickerWorkZone.Avoid);
                }
                if (IsInZone(setup.Input, position, tolerance))
                {
                    SetEncoderZoneMatch(PickerWorkZone.Input, ref zone, ref matchCount);
                    AppendEncoderZoneMatch(ref matches, PickerWorkZone.Input);
                }
                if (IsInZone(setup.Bottom, position, tolerance))
                {
                    SetEncoderZoneMatch(PickerWorkZone.Bottom, ref zone, ref matchCount);
                    AppendEncoderZoneMatch(ref matches, PickerWorkZone.Bottom);
                }
                if (IsInZone(setup.Side, position, tolerance))
                {
                    SetEncoderZoneMatch(PickerWorkZone.Side, ref zone, ref matchCount);
                    AppendEncoderZoneMatch(ref matches, PickerWorkZone.Side);
                }
                if (IsInZone(setup.Output, position, tolerance))
                {
                    SetEncoderZoneMatch(PickerWorkZone.Output, ref zone, ref matchCount);
                    AppendEncoderZoneMatch(ref matches, PickerWorkZone.Output);
                }

                configured = IsZoneConfigured(setup);
                if (matchCount > 1)
                    WriteEncoderZoneOverlapLog(isFront, position, tolerance, matches, setup);
                return matchCount == 1;
            }
            catch
            {
                zone = PickerWorkZone.Unknown;
                configured = false;
                return false;
            }
            finally
            {
            }
        }

        private static void SetEncoderZoneMatch(PickerWorkZone matchedZone, ref PickerWorkZone zone, ref int matchCount)
        {
            if (matchCount == 0)
            {
                zone = matchedZone;
                matchCount = 1;
                return;
            }

            // 현재 기준: INSPECT_B/INSPECT_S range가 겹쳐도 인터락 존은 같은 Process로 본다.
            if (IsSameInterlockZone(zone, matchedZone))
            {
                zone = NormalizeInterlockZone(zone);
                return;
            }

            matchCount++;
            zone = PickerWorkZone.Unknown;
        }

        private static void AppendEncoderZoneMatch(ref string matches, PickerWorkZone zone)
        {
            if (matches.Length > 0)
                matches += "|";
            matches += zone.ToString();
        }

        private static void WriteEncoderZoneOverlapLog(
            bool isFront,
            double position,
            double tolerance,
            string matches,
            PickerZoneXSetup setup)
        {
            try
            {
                if (!ShouldWriteEncoderOverlapLog(isFront))
                    return;

                QMC.Common.Log.Write(
                    "SharedRailX",
                    "PickerZone encoder overlap. side=" + (isFront ? "Front" : "Rear") +
                    ", position=" + position.ToString("0.###") +
                    ", tolerance=" + tolerance.ToString("0.###") +
                    ", matches=" + matches +
                    ", resolved=Unknown" +
                    ", avoid=" + FormatZoneRange(setup != null ? setup.Avoid : null) +
                    ", input=" + FormatZoneRange(setup != null ? setup.Input : null) +
                    ", bottom=" + FormatZoneRange(setup != null ? setup.Bottom : null) +
                    ", sideZone=" + FormatZoneRange(setup != null ? setup.Side : null) +
                    ", output=" + FormatZoneRange(setup != null ? setup.Output : null));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static bool ShouldWriteEncoderOverlapLog(bool isFront)
        {
            lock (activeZoneLock)
            {
                DateTime now = DateTime.UtcNow;
                DateTime last = isFront ? lastFrontEncoderOverlapLogUtc : lastRearEncoderOverlapLogUtc;
                if (last != DateTime.MinValue)
                    return false;

                if (isFront)
                    lastFrontEncoderOverlapLogUtc = now;
                else
                    lastRearEncoderOverlapLogUtc = now;

                return true;
            }
        }

        private static string FormatZoneRange(PickerZoneXRange range)
        {
            if (range == null)
                return "<null>";

            return "{enabled=" + range.Enabled +
                   ", min=" + range.MinX.ToString("0.###") +
                   ", max=" + range.MaxX.ToString("0.###") + "}";
        }

        private static bool IsZoneConfigured(PickerZoneXSetup setup)
        {
            return setup != null &&
                   ((setup.Avoid != null && setup.Avoid.Enabled) ||
                    (setup.Input != null && setup.Input.Enabled) ||
                    (setup.Bottom != null && setup.Bottom.Enabled) ||
                    (setup.Side != null && setup.Side.Enabled) ||
                    (setup.Output != null && setup.Output.Enabled));
        }

        private static PickerZoneXSetup GetPickerZoneXSetup(CDT320_Machine machine, bool isFront)
        {
            try
            {
                if (machine == null)
                    return null;

                PickerZoneXSetup setup = null;
                if (isFront)
                    setup = machine.PickerFrontUnit != null && machine.PickerFrontUnit.Setup != null
                        ? machine.PickerFrontUnit.Setup.ZoneX
                        : null;
                else
                    setup = machine.PickerRearUnit != null && machine.PickerRearUnit.Setup != null
                        ? machine.PickerRearUnit.Setup.ZoneX
                        : null;

                if (setup != null)
                    setup.Ensure();

                return setup;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static bool IsPickerXEncoderZoneConfigured(CDT320_Machine machine, bool isFront)
        {
            try
            {
                PickerZoneXSetup setup = GetPickerZoneXSetup(machine, isFront);
                return setup != null && setup.UseEncoderZone && IsZoneConfigured(setup);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsInZone(PickerZoneXRange range, double position, double tolerance)
        {
            return range != null && range.Enabled && range.Contains(position, tolerance);
        }

        private static string ToDisplayName(PickerWorkZone zone)
        {
            switch (zone)
            {
                case PickerWorkZone.Avoid:
                    return "AVOID";
                case PickerWorkZone.Input:
                    return "PICKUP";
                case PickerWorkZone.Bottom:
                case PickerWorkZone.Side:
                    return "PROCESS";
                case PickerWorkZone.Output:
                    return "PLACE";
                default:
                    return "UNKNOWN";
            }
        }

        private static PickerWorkZone ResolveYZoneByPosition(CDT320_Machine machine, bool isFront, double position)
        {
            if (machine == null)
                return PickerWorkZone.Unknown;

            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "AvoidPosition", position))
                return PickerWorkZone.Avoid;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "InputAvoidPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "OutputAvoidPosition", position))
                return PickerWorkZone.Output;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerY, "PickPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerY, "BottomPosition", position))
                return PickerWorkZone.Bottom;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerY, "SidePosition", position))
                return PickerWorkZone.Side;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerY, "PlacePosition", position))
                return PickerWorkZone.Output;
            // 현재 기준: 정확한 티칭 존이 아닌 상태에서 AvoidPosition보다 작은 Y 목표는 Input 쪽 진입으로 본다.
            if (IsPickerTargetBelowAvoidPosition(machine, isFront, PickerAxis.PickerY, position))
                return PickerWorkZone.Input;

            return PickerWorkZone.Unknown;
        }

        private static bool IsPickerTargetBelowAvoidPosition(CDT320_Machine machine, bool isFront, PickerAxis axis, double position)
        {
            BaseAxis baseAxis = axis == PickerAxis.PickerX ? GetPickerX(machine, isFront) : GetPickerY(machine, isFront);
            double tolerance = ResolveTolerance(baseAxis);
            double avoid = GetPickerTeachingPosition(machine, isFront, axis, "AvoidPosition");
            return position < avoid - tolerance;
        }

        private static bool IsPickerYAtAvoid(CDT320_Machine machine, bool isFront)
        {
            if (machine == null)
                return true;

            // 현재 기준: 상대 PickerY 안전 판단은 Home(0) 또는 실제 AvoidPosition만 인정한다.
            if (isFront)
            {
                PickerFrontUnit picker = machine.PickerFrontUnit;
                return picker == null ||
                       IsAxisAtHomePosition(picker.PickerY) ||
                       picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            }

            PickerRearUnit rear = machine.PickerRearUnit;
            return rear == null ||
                   IsAxisAtHomePosition(rear.PickerY) ||
                   rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
        }

        private static bool IsAxisAtHomePosition(BaseAxis axis)
        {
            if (axis == null)
                return true;

            double tolerance = ResolveTolerance(axis);
            return Math.Abs(axis.ActualPosition) <= tolerance;
        }

        private static bool IsPickerYTargetAvoid(MotionGuardRuleContext request, bool isFront)
        {
            if (request == null)
                return false;

            // 현재 기준: Y 목표가 Home(0) 또는 실제 AvoidPosition이면 안전 복귀 이동으로 허용한다.
            return IsPickerYSafeByPosition(
                request.Machine,
                isFront,
                request.TargetValue,
                ResolvePickerYOutDistance(request.Machine));
        }

        private static bool IsAtPickerPosition(
            CDT320_Machine machine,
            bool isFront,
            PickerAxis axis,
            string positionName,
            double position)
        {
            BaseAxis baseAxis = axis == PickerAxis.PickerX ? GetPickerX(machine, isFront) : GetPickerY(machine, isFront);
            double tolerance = ResolveTolerance(baseAxis);
            double target = GetPickerTeachingPosition(machine, isFront, axis, positionName);
            return Math.Abs(position - target) <= tolerance;
        }

        private static bool IsAtPickerZonePosition(
            CDT320_Machine machine,
            bool isFront,
            PickerAxis axis,
            string positionName,
            double position)
        {
            BaseAxis baseAxis = axis == PickerAxis.PickerX ? GetPickerX(machine, isFront) : GetPickerY(machine, isFront);
            double tolerance = ResolveTolerance(baseAxis);
            return IsAtPickerZonePosition(machine, isFront, axis, positionName, position, tolerance);
        }

        private static bool IsAtPickerZonePosition(
            CDT320_Machine machine,
            bool isFront,
            PickerAxis axis,
            string positionName,
            double position,
            double tolerance)
        {
            double basePosition = GetPickerTeachingPosition(machine, isFront, axis, positionName);
            if (Math.Abs(position - basePosition) <= tolerance)
                return true;

            for (int i = 0; i < 4; i++)
            {
                double offset = GetRuntimePickerZoneOffset(machine, isFront, axis, i);
                if (Math.Abs(position - (basePosition + offset)) <= tolerance)
                    return true;
            }

            return false;
        }

        private static double GetRuntimePickerZoneOffset(CDT320_Machine machine, bool isFront, PickerAxis axis, int pickerIndex)
        {
            if (machine == null)
                return 0.0;

            PickerAlignOffset offset = null;
            if (isFront)
            {
                PickerFrontUnit picker = machine.PickerFrontUnit;
                offset = picker != null ? picker.GetRuntimePickerOffset(pickerIndex) : null;
            }
            else
            {
                PickerRearUnit picker = machine.PickerRearUnit;
                offset = picker != null ? picker.GetRuntimePickerOffset(pickerIndex) : null;
            }

            if (offset == null)
                return 0.0;

            if (axis == PickerAxis.PickerX)
                return offset.AlignOffsetX;
            if (axis == PickerAxis.PickerY)
                return offset.AlignOffsetY;

            return 0.0;
        }

        private static double GetPickerTeachingPosition(
            CDT320_Machine machine,
            bool isFront,
            PickerAxis axis,
            string positionName)
        {
            if (machine == null)
                return 0.0;

            if (isFront)
            {
                PickerFrontUnit picker = machine.PickerFrontUnit;
                return picker != null ? picker.GetPickerTeachingPosition(axis, positionName) : 0.0;
            }

            PickerRearUnit rear = machine.PickerRearUnit;
            return rear != null ? rear.GetPickerTeachingPosition(axis, positionName) : 0.0;
        }

        private static BaseAxis GetPickerX(CDT320_Machine machine, bool isFront)
        {
            if (machine == null)
                return null;
            return isFront
                ? (machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null)
                : (machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerX : null);
        }

        private static BaseAxis GetPickerY(CDT320_Machine machine, bool isFront)
        {
            if (machine == null)
                return null;
            return isFront
                ? (machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerY : null)
                : (machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerY : null);
        }

        private static BaseAxis GetPickerZ(CDT320_Machine machine, bool isFront, PickerAxis zAxis)
        {
            if (machine == null)
                return null;

            if (isFront)
            {
                PickerFrontUnit picker = machine.PickerFrontUnit;
                if (picker == null)
                    return null;

                switch (zAxis)
                {
                    case PickerAxis.PickerZ0: return picker.PickerZ0;
                    case PickerAxis.PickerZ1: return picker.PickerZ1;
                    case PickerAxis.PickerZ2: return picker.PickerZ2;
                    case PickerAxis.PickerZ3: return picker.PickerZ3;
                    default: return null;
                }
            }

            PickerRearUnit rear = machine.PickerRearUnit;
            if (rear == null)
                return null;

            switch (zAxis)
            {
                case PickerAxis.PickerZ0: return rear.PickerZ0;
                case PickerAxis.PickerZ1: return rear.PickerZ1;
                case PickerAxis.PickerZ2: return rear.PickerZ2;
                case PickerAxis.PickerZ3: return rear.PickerZ3;
                default: return null;
            }
        }

        private static double ResolveTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return DefaultTolerance;
        }

        private static PickerWorkZone ParseZone(string targetName)
        {
            string name = (targetName ?? string.Empty).Replace(" ", string.Empty);
            if (name.Length == 0)
                return PickerWorkZone.Unknown;

            if (Contains(name, "PickerZone=Input") ||
                Contains(name, "DiePick") ||
                Contains(name, "InputAvoidPosition") ||
                Contains(name, "PickPosition"))
                return PickerWorkZone.Input;
            if (Contains(name, "PickerZone=Process") ||
                Contains(name, "PickerZone=Inspect") ||
                Contains(name, "PickerZone=Inspection"))
                return PickerWorkZone.Bottom;
            if (Contains(name, "PickerZone=Bottom") ||
                Contains(name, "DieBottom") ||
                Contains(name, "BottomPosition") ||
                Contains(name, "INSPECT_B"))
                return PickerWorkZone.Bottom;
            if (Contains(name, "PickerZone=Side") ||
                Contains(name, "DieSide") ||
                Contains(name, "SidePosition") ||
                Contains(name, "INSPECT_S"))
                return PickerWorkZone.Side;
            if (Contains(name, "PickerZone=Output") ||
                Contains(name, "DiePlace") ||
                Contains(name, "OutputAvoidPosition") ||
                Contains(name, "PlacePosition"))
                return PickerWorkZone.Output;
            // 기존 조건: InputAvoidPosition도 Avoid로 보았다.
            // 현재 필요 여부: 사용 안 함. Input side avoid는 Input, Output side avoid는 Output 진입으로 본다.
            if (Contains(name, "PickerZone=Avoid") ||
                Contains(name, "AvoidPosition") ||
                Contains(name, "SafeRetreat"))
                return PickerWorkZone.Avoid;

            return PickerWorkZone.Unknown;
        }

        private static bool Contains(string value, string pattern)
        {
            return value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static PickerWorkZone GetActivePickerYTargetZone(bool isFront)
        {
            lock (activeZoneLock)
            {
                return isFront ? frontPickerYActiveTargetZone : rearPickerYActiveTargetZone;
            }
        }

        private static bool IsOtherPickerWorkAreaActive(bool isFront, PickerWorkZone zone, out string owner)
        {
            lock (activeZoneLock)
            {
                return IsPickerWorkAreaActive(!isFront, zone, out owner);
            }
        }

        private static bool IsPickerWorkAreaActive(bool isFront, PickerWorkZone zone, out string owner)
        {
            owner = string.Empty;
            zone = NormalizeInterlockZone(zone);

            switch (zone)
            {
                case PickerWorkZone.Input:
                    owner = isFront ? frontInputPickAreaOwner : rearInputPickAreaOwner;
                    return isFront ? frontInputPickAreaUseCount > 0 : rearInputPickAreaUseCount > 0;
                case PickerWorkZone.Bottom:
                    // 현재 기준: Bottom/Side는 하나의 Process 작업 영역으로 점유 여부를 합산한다.
                    int bottomCount = isFront ? frontBottomAreaUseCount : rearBottomAreaUseCount;
                    int sideCount = isFront ? frontSideAreaUseCount : rearSideAreaUseCount;
                    string bottomOwner = isFront ? frontBottomAreaOwner : rearBottomAreaOwner;
                    string sideOwner = isFront ? frontSideAreaOwner : rearSideAreaOwner;
                    if (bottomCount > 0 && sideCount > 0)
                        owner = bottomOwner + "|" + sideOwner;
                    else
                        owner = bottomCount > 0 ? bottomOwner : sideOwner;
                    return bottomCount > 0 || sideCount > 0;
                case PickerWorkZone.Output:
                    owner = isFront ? frontOutputAreaOwner : rearOutputAreaOwner;
                    return isFront ? frontOutputAreaUseCount > 0 : rearOutputAreaUseCount > 0;
                default:
                    owner = string.Empty;
                    return false;
            }
        }

        private static bool IsFrontPickerReadyAvoidSafe(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            if (machine.PickerFrontUnit == null)
                return true;

            return ArePickerAxesReadyAvoidSafe(
                machine.PickerFrontUnit.Axes,
                delegate(PickerAxis axis) { return machine.PickerFrontUnit.GetPickerTeachingPosition(axis, "AvoidPosition"); },
                "FrontPicker",
                out reason);
        }

        private static bool IsRearPickerReadyAvoidSafe(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            if (machine.PickerRearUnit == null)
                return true;

            return ArePickerAxesReadyAvoidSafe(
                machine.PickerRearUnit.Axes,
                delegate(PickerAxis axis) { return machine.PickerRearUnit.GetPickerTeachingPosition(axis, "AvoidPosition"); },
                "RearPicker",
                out reason);
        }

        private static bool ArePickerAxesReadyAvoidSafe(
            System.Collections.Generic.IReadOnlyDictionary<PickerAxis, BaseAxis> axes,
            System.Func<PickerAxis, double> resolveAvoidTarget,
            string label,
            out string reason)
        {
            reason = string.Empty;

            if (axes == null)
                return true;

            foreach (System.Collections.Generic.KeyValuePair<PickerAxis, BaseAxis> pair in axes)
            {
                BaseAxis axis = pair.Value;
                if (axis == null)
                    continue;

                try
                {
                    axis.UpdateStatus();
                }
                catch
                {
                }

                if (axis.IsAlarm)
                {
                    reason = label + " " + pair.Key + " 축 알람이 ON 상태입니다.";
                    return false;
                }

                if (axis.IsMoving)
                {
                    reason = label + " " + pair.Key + " 축이 아직 이동 중입니다.";
                    return false;
                }

                double target;
                try
                {
                    target = resolveAvoidTarget != null ? resolveAvoidTarget(pair.Key) : axis.ActualPosition;
                }
                catch (Exception ex)
                {
                    reason = label + " " + pair.Key + " Avoid 목표 위치 해석 실패. error=" + ex.Message;
                    return false;
                }
                finally
                {
                }

                double tolerance = ResolveReadyAvoidTolerance(axis);
                double actual = axis.ActualPosition;
                double command = axis.CommandPosition;
                if (Math.Abs(actual - target) > tolerance || Math.Abs(command - target) > tolerance)
                {
                    reason = label + " " + pair.Key + " Avoid 위치 최종 확인 실패. " +
                             "actual=" + actual.ToString("0.###") +
                             ", command=" + command.ToString("0.###") +
                             ", target=" + target.ToString("0.###") +
                             ", tolerance=" + tolerance.ToString("0.###");
                    return false;
                }
            }

            return true;
        }

        private static double ResolveReadyAvoidTolerance(BaseAxis axis)
        {
            try
            {
                if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                    return axis.Config.InPositionTolerance;

                return DefaultTolerance;
            }
            catch
            {
                return DefaultTolerance;
            }
            finally
            {
            }
        }

        private static bool ClearAllPickerWorkAreasLocked(out string summary)
        {
            var cleared = new System.Collections.Generic.List<string>();

            ClearPickerWorkAreaLocked(true, PickerWorkZone.Input, ref frontInputPickAreaUseCount, ref frontInputPickAreaOwner, cleared);
            ClearPickerWorkAreaLocked(true, PickerWorkZone.Bottom, ref frontBottomAreaUseCount, ref frontBottomAreaOwner, cleared);
            ClearPickerWorkAreaLocked(true, PickerWorkZone.Side, ref frontSideAreaUseCount, ref frontSideAreaOwner, cleared);
            ClearPickerWorkAreaLocked(true, PickerWorkZone.Output, ref frontOutputAreaUseCount, ref frontOutputAreaOwner, cleared);

            ClearPickerWorkAreaLocked(false, PickerWorkZone.Input, ref rearInputPickAreaUseCount, ref rearInputPickAreaOwner, cleared);
            ClearPickerWorkAreaLocked(false, PickerWorkZone.Bottom, ref rearBottomAreaUseCount, ref rearBottomAreaOwner, cleared);
            ClearPickerWorkAreaLocked(false, PickerWorkZone.Side, ref rearSideAreaUseCount, ref rearSideAreaOwner, cleared);
            ClearPickerWorkAreaLocked(false, PickerWorkZone.Output, ref rearOutputAreaUseCount, ref rearOutputAreaOwner, cleared);

            summary = cleared.Count > 0 ? string.Join("; ", cleared.ToArray()) : string.Empty;
            return cleared.Count > 0;
        }

        private static void ClearPickerWorkAreaLocked(
            bool isFront,
            PickerWorkZone zone,
            ref int useCount,
            ref string owner,
            System.Collections.Generic.List<string> cleared)
        {
            if (useCount <= 0)
                return;

            string side = isFront ? "FrontPicker" : "RearPicker";
            cleared.Add(side + "/" + zone + "/owner=" + (owner ?? string.Empty) + "/count=" + useCount);
            useCount = 0;
            owner = string.Empty;
        }

        private static void AddPickerWorkAreaUse(bool isFront, PickerWorkZone zone, string owner)
        {
            string safeOwner = owner ?? string.Empty;
            zone = NormalizeInterlockZone(zone);

            switch (zone)
            {
                case PickerWorkZone.Input:
                    if (isFront)
                    {
                        frontInputPickAreaUseCount++;
                        frontInputPickAreaOwner = safeOwner;
                    }
                    else
                    {
                        rearInputPickAreaUseCount++;
                        rearInputPickAreaOwner = safeOwner;
                    }
                    break;
                case PickerWorkZone.Bottom:
                    // 현재 기준: Side 검사도 Process 대표 카운터(Bottom)에 점유한다.
                    if (isFront)
                    {
                        frontBottomAreaUseCount++;
                        frontBottomAreaOwner = safeOwner;
                    }
                    else
                    {
                        rearBottomAreaUseCount++;
                        rearBottomAreaOwner = safeOwner;
                    }
                    break;
                case PickerWorkZone.Side:
                    // 기존 조건: Bottom/Side 작업 영역을 별도 카운터로 관리했다.
                    // 현재 필요 여부: 사용 안 함. NormalizeInterlockZone에서 Process 대표값(Bottom)으로 합쳐진다.
                    if (isFront)
                    {
                        frontSideAreaUseCount++;
                        frontSideAreaOwner = safeOwner;
                    }
                    else
                    {
                        rearSideAreaUseCount++;
                        rearSideAreaOwner = safeOwner;
                    }
                    break;
                case PickerWorkZone.Output:
                    if (isFront)
                    {
                        frontOutputAreaUseCount++;
                        frontOutputAreaOwner = safeOwner;
                    }
                    else
                    {
                        rearOutputAreaUseCount++;
                        rearOutputAreaOwner = safeOwner;
                    }
                    break;
            }
        }

        private static void RemovePickerWorkAreaUse(bool isFront, PickerWorkZone zone)
        {
            zone = NormalizeInterlockZone(zone);
            switch (zone)
            {
                case PickerWorkZone.Input:
                    if (isFront)
                    {
                        if (frontInputPickAreaUseCount > 0)
                            frontInputPickAreaUseCount--;
                        if (frontInputPickAreaUseCount == 0)
                            frontInputPickAreaOwner = string.Empty;
                    }
                    else
                    {
                        if (rearInputPickAreaUseCount > 0)
                            rearInputPickAreaUseCount--;
                        if (rearInputPickAreaUseCount == 0)
                            rearInputPickAreaOwner = string.Empty;
                    }
                    break;
                case PickerWorkZone.Bottom:
                    // 현재 기준: Process 점유 해제는 대표 카운터(Bottom)를 해제한다.
                    if (isFront)
                    {
                        if (frontBottomAreaUseCount > 0)
                            frontBottomAreaUseCount--;
                        if (frontBottomAreaUseCount == 0)
                            frontBottomAreaOwner = string.Empty;
                    }
                    else
                    {
                        if (rearBottomAreaUseCount > 0)
                            rearBottomAreaUseCount--;
                        if (rearBottomAreaUseCount == 0)
                            rearBottomAreaOwner = string.Empty;
                    }
                    break;
                case PickerWorkZone.Side:
                    // 기존 조건: Bottom/Side 작업 영역을 별도 카운터로 해제했다.
                    // 현재 필요 여부: 사용 안 함. NormalizeInterlockZone에서 Process 대표값(Bottom)으로 합쳐진다.
                    if (isFront)
                    {
                        if (frontSideAreaUseCount > 0)
                            frontSideAreaUseCount--;
                        if (frontSideAreaUseCount == 0)
                            frontSideAreaOwner = string.Empty;
                    }
                    else
                    {
                        if (rearSideAreaUseCount > 0)
                            rearSideAreaUseCount--;
                        if (rearSideAreaUseCount == 0)
                            rearSideAreaOwner = string.Empty;
                    }
                    break;
                case PickerWorkZone.Output:
                    if (isFront)
                    {
                        if (frontOutputAreaUseCount > 0)
                            frontOutputAreaUseCount--;
                        if (frontOutputAreaUseCount == 0)
                            frontOutputAreaOwner = string.Empty;
                    }
                    else
                    {
                        if (rearOutputAreaUseCount > 0)
                            rearOutputAreaUseCount--;
                        if (rearOutputAreaUseCount == 0)
                            rearOutputAreaOwner = string.Empty;
                    }
                    break;
            }
        }

        private static bool IsAvoidZone(PickerWorkZone zone)
        {
            return zone == PickerWorkZone.Avoid;
        }

        private sealed class ActiveZoneScope : IDisposable
        {
            private readonly bool isFront;
            private readonly bool active;
            private readonly PickerWorkZone previous;
            private bool disposed;

            public ActiveZoneScope(bool isFront, PickerWorkZone previous, bool active)
            {
                this.isFront = isFront;
                this.previous = previous;
                this.active = active;
            }

            public void Dispose()
            {
                if (disposed || !active)
                    return;

                lock (activeZoneLock)
                {
                    if (isFront)
                        frontPickerYActiveTargetZone = previous;
                    else
                        rearPickerYActiveTargetZone = previous;
                }

                disposed = true;
            }
        }

        private sealed class PickerWorkAreaScope : IDisposable
        {
            private readonly bool isFront;
            private readonly PickerWorkZone zone;
            private bool disposed;

            public PickerWorkAreaScope(bool isFront, PickerWorkZone zone)
            {
                this.isFront = isFront;
                this.zone = zone;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                lock (activeZoneLock)
                {
                    RemovePickerWorkAreaUse(isFront, zone);
                }

                disposed = true;
            }
        }
    }
}
