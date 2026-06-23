using System;
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
                return CurrentZone == RequestedZone ||
                       TargetZone == RequestedZone;
            }
        }

        public bool WorkAreaBlocksTransport
        {
            get
            {
                if (!HasWorkArea || WorkAreaZone != RequestedZone)
                    return false;

                return !IsWorkAreaPhysicallyClearForTransport;
            }
        }

        public bool IsWorkAreaPhysicallyClearForTransport
        {
            get
            {
                return YAvoid &&
                       CurrentZone != RequestedZone &&
                       TargetZone != RequestedZone &&
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

        public static IDisposable BeginPickerZoneMove(string side, PickerAxis axis, string targetName)
        {
            bool isFront = string.Equals(side, "Front", StringComparison.OrdinalIgnoreCase);
            bool isRear = string.Equals(side, "Rear", StringComparison.OrdinalIgnoreCase);
            if ((!isFront && !isRear) || axis != PickerAxis.PickerY)
                return new ActiveZoneScope(false, PickerWorkZone.Unknown, false);

            PickerWorkZone zone = ParseZone(targetName);
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
                    if (currentYZone == targetZone)
                        currentZone = currentYZone;
                }

                if (!VerifyInputStageZSafeForInputZone(
                    request.Machine,
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

                if (currentZone != targetZone &&
                    !IsPickerYAtAvoid(request.Machine, isFront))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(movingName, "존 사이를 이동하거나 현재 존을 판단할 수 없을 때는 PickerY가 Avoid 또는 0 위치여야 합니다.", ownX, ownY, currentZone, targetZone),
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
                    string otherActiveName = isFront ? "RearPicker" : "FrontPicker";
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Y축 전진 이동 불가: " + otherActiveName +
                        "Y가 아직 이동 중입니다. 한쪽 PickerY가 전진 존으로 이동할 때는 상대 PickerY가 반드시 Avoid 위치에 있어야 합니다. targetZone=" + targetZone +
                        ", otherActiveTargetZone=" + otherActiveZone +
                        ", target=" + request.TargetValue.ToString("0.###") +
                        ", targetName=" + request.TargetName,
                        out reason);
                }

                if (IsPickerYAtAvoid(request.Machine, otherFront))
                    return true;

                PickerWorkZone otherZone = ResolveCurrentXZoneWithContext(request.Machine, otherFront);
                string otherName = isFront ? "RearPicker" : "FrontPicker";
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " Y축 전진 이동 불가: " + otherName +
                    "Y가 Avoid 위치가 아닙니다. 존 내부 검사 동작 중에는 자기 PickerY가 전진 위치에 있어도 되지만, 다른 PickerY가 새로 전진 이동하려면 상대 PickerY는 Avoid 위치여야 합니다. targetZone=" + targetZone +
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

                bool otherFront = !isFront;
                if (!IsPickerYForwardOrMovingForward(request.Machine, otherFront))
                    return true;

                BaseAxis ownX = GetPickerX(request.Machine, isFront);
                BaseAxis ownY = GetPickerY(request.Machine, isFront);
                BaseAxis otherX = GetPickerX(request.Machine, otherFront);
                BaseAxis otherY = GetPickerY(request.Machine, otherFront);
                if (ownX == null || otherX == null)
                    return true;

                double clearance = ResolvePickerYFacingXClearance(request.Machine);
                if (clearance <= 0.0)
                    return true;

                double distance = Math.Abs(ownX.ActualPosition - otherX.ActualPosition);
                if (distance > clearance)
                    return true;

                string otherName = isFront ? "RearPicker" : "FrontPicker";
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " Y축 전진 이동 불가: " + otherName +
                    "Y가 이미 전진 위치이거나 전진 이동 중이고, Front/Rear PickerX가 마주보는 위치입니다. " +
                    "한쪽 PickerY를 Avoid 또는 0 위치로 이동한 뒤 진행해야 합니다. " +
                    "targetZone=" + targetZone +
                    ", xDistance=" + distance.ToString("0.###") +
                    ", requiredClearance=" + clearance.ToString("0.###") +
                    ", ownX=" + FormatAxis(ownX) +
                    ", ownY=" + FormatAxis(ownY) +
                    ", otherX=" + FormatAxis(otherX) +
                    ", otherY=" + FormatAxis(otherY) +
                    ", target=" + request.TargetValue.ToString("0.###") +
                    ", targetName=" + request.TargetName,
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

        private static bool IsPickerYForwardOrMovingForward(CDT320_Machine machine, bool isFront)
        {
            if (machine == null)
                return false;

            PickerWorkZone activeTargetZone = GetActivePickerYTargetZone(isFront);
            if (activeTargetZone != PickerWorkZone.Unknown && !IsAvoidZone(activeTargetZone))
                return true;

            if (!IsPickerYAtAvoid(machine, isFront))
                return true;

            BaseAxis y = GetPickerY(machine, isFront);
            return y != null && y.IsMoving;
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

        private static bool CanShareForwardY(PickerWorkZone targetZone, PickerWorkZone otherZone)
        {
            if (targetZone == PickerWorkZone.Unknown || otherZone == PickerWorkZone.Unknown)
                return false;
            if (IsAvoidZone(targetZone) || IsAvoidZone(otherZone))
                return true;

            return targetZone != otherZone;
        }

        private static bool VerifyInputStageZSafeForInputZone(
            CDT320_Machine machine,
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

            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (stage == null)
                return true;

            if (IsInputStageZAtAvoidProcessOrReady(stage))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " " + moveAxisName +
                " 이동 불가: Picker가 Input 존에 있거나 Input 존으로 이동하려는데 InputExpandingZ가 Avoid/Process 위치가 아닙니다. " +
                "currentZone=" + currentZone +
                ", targetZone=" + targetZone +
                ", xActual=" + FormatAxis(ownX) +
                ", yActual=" + FormatAxis(ownY) +
                ", " + BuildInputStageZState(stage),
                out reason);
        }

        private static bool IsInputStageZAtAvoidProcessOrReady(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null || stage.Recipe == null || stage.Recipe.WaferZ == null)
                return false;

            double tolerance = ResolveTolerance(stage.ExpanderZ);
            double actual = stage.ExpanderZ.ActualPosition;
            StageAxisPositions waferZ = stage.Recipe.WaferZ;

            return Math.Abs(actual - waferZ.AvoidPosition) <= tolerance ||
                   Math.Abs(actual - waferZ.ProcessPosition) <= tolerance ||
                   Math.Abs(actual - waferZ.ReadyPosition) <= tolerance;
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
            PickerWorkZone byName = ParseZone(targetName);
            if (byName != PickerWorkZone.Unknown)
                return byName;

            if (targetX.HasValue)
                return ResolveXZoneByPositionWithContext(machine, isFront, targetX.Value);

            return PickerWorkZone.Unknown;
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

                BaseAxis x = GetPickerX(machine, isFront);
                if (x == null)
                    return PickerWorkZone.Unknown;

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
                return PickerWorkZone.Unknown;

            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "AvoidPosition", position) ||
                IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "InputAvoidPosition", position) ||
                IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "OutputAvoidPosition", position))
                return PickerWorkZone.Avoid;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "PickPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "BottomPosition", position))
                return PickerWorkZone.Bottom;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "SidePosition", position))
                return PickerWorkZone.Side;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerX, "PlacePosition", position))
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

                if (IsInZone(setup.Avoid, position, tolerance))
                    SetEncoderZoneMatch(PickerWorkZone.Avoid, ref zone, ref matchCount);
                if (IsInZone(setup.Input, position, tolerance))
                    SetEncoderZoneMatch(PickerWorkZone.Input, ref zone, ref matchCount);
                if (IsInZone(setup.Bottom, position, tolerance))
                    SetEncoderZoneMatch(PickerWorkZone.Bottom, ref zone, ref matchCount);
                if (IsInZone(setup.Side, position, tolerance))
                    SetEncoderZoneMatch(PickerWorkZone.Side, ref zone, ref matchCount);
                if (IsInZone(setup.Output, position, tolerance))
                    SetEncoderZoneMatch(PickerWorkZone.Output, ref zone, ref matchCount);

                configured = IsZoneConfigured(setup);
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
            matchCount++;
            if (matchCount == 1)
                zone = matchedZone;
            else
                zone = PickerWorkZone.Unknown;
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
                    return "INSPECT_B";
                case PickerWorkZone.Side:
                    return "INSPECT_S";
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

            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "AvoidPosition", position) ||
                IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "InputAvoidPosition", position) ||
                IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "OutputAvoidPosition", position))
                return PickerWorkZone.Avoid;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerY, "PickPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerY, "BottomPosition", position))
                return PickerWorkZone.Bottom;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerY, "SidePosition", position))
                return PickerWorkZone.Side;
            if (IsAtPickerZonePosition(machine, isFront, PickerAxis.PickerY, "PlacePosition", position))
                return PickerWorkZone.Output;

            return PickerWorkZone.Unknown;
        }

        private static bool IsPickerYAtAvoid(CDT320_Machine machine, bool isFront)
        {
            if (machine == null)
                return true;

            if (isFront)
            {
                PickerFrontUnit picker = machine.PickerFrontUnit;
                return picker == null ||
                       IsAxisAtHomePosition(picker.PickerY) ||
                       picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition") ||
                       picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "InputAvoidPosition") ||
                       picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "OutputAvoidPosition");
            }

            PickerRearUnit rear = machine.PickerRearUnit;
            return rear == null ||
                   IsAxisAtHomePosition(rear.PickerY) ||
                   rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition") ||
                   rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "InputAvoidPosition") ||
                   rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "OutputAvoidPosition");
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

            PickerWorkZone targetZone = ResolveYZoneByPosition(request.Machine, isFront, request.TargetValue);
            return IsAvoidZone(targetZone);
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
            if (IsAtPickerPosition(machine, isFront, axis, positionName, position))
                return true;

            BaseAxis baseAxis = axis == PickerAxis.PickerX ? GetPickerX(machine, isFront) : GetPickerY(machine, isFront);
            double tolerance = ResolveTolerance(baseAxis);
            double basePosition = GetPickerTeachingPosition(machine, isFront, axis, positionName);
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
                Contains(name, "PickPosition"))
                return PickerWorkZone.Input;
            if (Contains(name, "PickerZone=Bottom") ||
                Contains(name, "DieBottom") ||
                Contains(name, "BottomPosition"))
                return PickerWorkZone.Bottom;
            if (Contains(name, "PickerZone=Side") ||
                Contains(name, "DieSide") ||
                Contains(name, "SidePosition"))
                return PickerWorkZone.Side;
            if (Contains(name, "PickerZone=Output") ||
                Contains(name, "DiePlace") ||
                Contains(name, "PlacePosition"))
                return PickerWorkZone.Output;
            if (Contains(name, "PickerZone=Avoid") ||
                Contains(name, "AvoidPosition") ||
                Contains(name, "InputAvoidPosition") ||
                Contains(name, "OutputAvoidPosition") ||
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

            switch (zone)
            {
                case PickerWorkZone.Input:
                    owner = isFront ? frontInputPickAreaOwner : rearInputPickAreaOwner;
                    return isFront ? frontInputPickAreaUseCount > 0 : rearInputPickAreaUseCount > 0;
                case PickerWorkZone.Bottom:
                    owner = isFront ? frontBottomAreaOwner : rearBottomAreaOwner;
                    return isFront ? frontBottomAreaUseCount > 0 : rearBottomAreaUseCount > 0;
                case PickerWorkZone.Side:
                    owner = isFront ? frontSideAreaOwner : rearSideAreaOwner;
                    return isFront ? frontSideAreaUseCount > 0 : rearSideAreaUseCount > 0;
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

            if (!machine.PickerFrontUnit.IsFrontPickerInAvoidPosition())
            {
                reason = "FrontPicker Avoid 위치 최종 확인이 되지 않았습니다.";
                return false;
            }

            return ArePickerAxesReadyAvoidSafe(machine.PickerFrontUnit.Axes, "FrontPicker", out reason);
        }

        private static bool IsRearPickerReadyAvoidSafe(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            if (machine.PickerRearUnit == null)
                return true;

            if (!machine.PickerRearUnit.IsRearPickerInAvoidPosition())
            {
                reason = "RearPicker Avoid 위치 최종 확인이 되지 않았습니다.";
                return false;
            }

            return ArePickerAxesReadyAvoidSafe(machine.PickerRearUnit.Axes, "RearPicker", out reason);
        }

        private static bool ArePickerAxesReadyAvoidSafe(
            System.Collections.Generic.IReadOnlyDictionary<PickerAxis, BaseAxis> axes,
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
            }

            return true;
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
