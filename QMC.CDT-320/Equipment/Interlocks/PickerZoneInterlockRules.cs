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

    internal static class PickerZoneInterlockRules
    {
        private const double DefaultTolerance = 0.05;
        private static readonly object activeZoneLock = new object();
        private static PickerWorkZone frontPickerYActiveTargetZone = PickerWorkZone.Unknown;
        private static PickerWorkZone rearPickerYActiveTargetZone = PickerWorkZone.Unknown;

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
                PickerWorkZone currentZone = ResolveCurrentXZone(request.Machine, isFront);
                PickerWorkZone targetZone = ResolveTargetXZone(request, isFront);

                if (IsAvoidZone(targetZone))
                {
                    if (IsPickerYAtAvoid(request.Machine, isFront))
                        return true;

                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(movingName, "X move to Avoid/InputAvoid/OutputAvoid requires own PickerY at Avoid.", ownX, ownY, currentZone, targetZone),
                        out reason);
                }

                if (targetZone == PickerWorkZone.Unknown)
                {
                    if (IsPickerYAtAvoid(request.Machine, isFront))
                        return true;

                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(movingName, "X target zone is unknown. PickerY must be at Avoid before free X move.", ownX, ownY, currentZone, targetZone),
                        out reason);
                }

                if (currentZone != targetZone &&
                    !IsPickerYAtAvoid(request.Machine, isFront))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        BuildXBlockedMessage(movingName, "X zone crossing or unknown current zone requires own PickerY at Avoid.", ownX, ownY, currentZone, targetZone),
                        out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " zone interlock check failed. error=" + ex.Message,
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
                if (IsAvoidZone(targetZone))
                    return true;

                if (targetZone == PickerWorkZone.Unknown)
                {
                    if (IsPickerYTargetAvoid(request, isFront))
                        return true;

                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Y move blocked. Target zone is unknown. Use a named picker zone move or move Y to Avoid first. target=" +
                        request.TargetValue.ToString("0.###") + ", targetName=" + request.TargetName,
                        out reason);
                }

                bool otherFront = !isFront;
                PickerWorkZone otherActiveZone = GetActivePickerYTargetZone(otherFront);
                if (otherActiveZone != PickerWorkZone.Unknown)
                {
                    if (CanShareForwardY(targetZone, otherActiveZone))
                        return true;

                    string otherActiveName = isFront ? "RearPicker" : "FrontPicker";
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " Y forward move blocked. " + otherActiveName +
                        "Y is moving to the same or unknown work zone. targetZone=" + targetZone +
                        ", otherActiveTargetZone=" + otherActiveZone +
                        ", target=" + request.TargetValue.ToString("0.###") +
                        ", targetName=" + request.TargetName,
                        out reason);
                }

                if (IsPickerYAtAvoid(request.Machine, otherFront))
                    return true;

                PickerWorkZone otherZone = ResolveCurrentXZone(request.Machine, otherFront);
                if (CanShareForwardY(targetZone, otherZone))
                    return true;

                string otherName = isFront ? "RearPicker" : "FrontPicker";
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " Y forward move blocked. " + otherName +
                    "Y is not at Avoid and zones are not compatible. targetZone=" + targetZone +
                    ", otherZone=" + otherZone +
                    ", target=" + request.TargetValue.ToString("0.###") +
                    ", targetName=" + request.TargetName,
                    out reason);
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " zone interlock check failed. error=" + ex.Message,
                    out reason);
            }
        }

        private static bool CanShareForwardY(PickerWorkZone targetZone, PickerWorkZone otherZone)
        {
            if (targetZone == PickerWorkZone.Unknown || otherZone == PickerWorkZone.Unknown)
                return false;
            if (IsAvoidZone(targetZone) || IsAvoidZone(otherZone))
                return true;

            return targetZone != otherZone;
        }

        private static string BuildXBlockedMessage(
            string movingName,
            string detail,
            BaseAxis ownX,
            BaseAxis ownY,
            PickerWorkZone currentZone,
            PickerWorkZone targetZone)
        {
            return movingName + " move blocked. " + detail +
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
            PickerWorkZone byName = ParseZone(request != null ? request.TargetName : string.Empty);
            if (byName != PickerWorkZone.Unknown)
                return byName;

            return ResolveXZoneByPosition(
                request != null ? request.Machine : null,
                isFront,
                request != null ? request.TargetValue : 0.0);
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

            PickerWorkZone currentXZone = ResolveCurrentXZone(request != null ? request.Machine : null, isFront);
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

        private static PickerWorkZone ResolveXZoneByPosition(CDT320_Machine machine, bool isFront, double position)
        {
            if (machine == null)
                return PickerWorkZone.Unknown;

            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "AvoidPosition", position) ||
                IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "InputAvoidPosition", position) ||
                IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "OutputAvoidPosition", position))
                return PickerWorkZone.Avoid;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "PickPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "BottomPosition", position))
                return PickerWorkZone.Bottom;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "SidePosition", position))
                return PickerWorkZone.Side;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerX, "PlacePosition", position))
                return PickerWorkZone.Output;

            return PickerWorkZone.Unknown;
        }

        private static PickerWorkZone ResolveYZoneByPosition(CDT320_Machine machine, bool isFront, double position)
        {
            if (machine == null)
                return PickerWorkZone.Unknown;

            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "AvoidPosition", position) ||
                IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "InputAvoidPosition", position) ||
                IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "OutputAvoidPosition", position))
                return PickerWorkZone.Avoid;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "PickPosition", position))
                return PickerWorkZone.Input;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "BottomPosition", position))
                return PickerWorkZone.Bottom;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "SidePosition", position))
                return PickerWorkZone.Side;
            if (IsAtPickerPosition(machine, isFront, PickerAxis.PickerY, "PlacePosition", position))
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
                       picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition") ||
                       picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "InputAvoidPosition") ||
                       picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "OutputAvoidPosition");
            }

            PickerRearUnit rear = machine.PickerRearUnit;
            return rear == null ||
                   rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition") ||
                   rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "InputAvoidPosition") ||
                   rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "OutputAvoidPosition");
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
    }
}
