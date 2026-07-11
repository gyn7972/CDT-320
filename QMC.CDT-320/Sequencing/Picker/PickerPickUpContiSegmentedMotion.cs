using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.Common.Motion.Ajin;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerPickUpContiNode
    {
        public PickerPickUpContiNode(int index, double pickerX, double needleX, double stageY, double pickerZ)
        {
            Index = index;
            PickerX = pickerX;
            NeedleX = needleX;
            StageY = stageY;
            PickerZ = pickerZ;
        }

        public int Index { get; private set; }
        public double PickerX { get; private set; }
        public double NeedleX { get; private set; }
        public double StageY { get; private set; }
        public double PickerZ { get; private set; }
    }

    internal static class PickerPickUpContiSegmentedMotion
    {
        private const int SplineNodeCountPerSegment = 40;
        private const double ShortSegmentVelocitySafetyRatio = 0.8;
        private const double MinimumContiVelocity = 0.1;
        private const double MinimumContiAcceleration = 1.0;

        public static async Task<InterpolatedMotionMoveResult> MovePickerXStageYAndPickerZBySplineWriteAsync(
            BaseAxis pickerX,
            BaseAxis stageY,
            BaseAxis pickerZ,
            IList<PickerPickUpContiNode> nodes,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            var result = new InterpolatedMotionMoveResult();
            Stopwatch watch = Stopwatch.StartNew();
            AjinVirtualCoordinateLease coordinateLease = null;

            try
            {
                if (config == null)
                    config = new PickerPickUpMotionConfig();
                config.Ensure();

                result.Coordinate = config.TransferContiCoordinate;
                result.TimeoutMs = config.TransferContiTimeoutMs;
                result.Velocity = config.GetTransferContiNodeVelocity(3);
                result.Acceleration = config.GetTransferContiNodeAcceleration(3);
                result.Deceleration = config.GetTransferContiNodeDeceleration(3);

                string readyReason;
                if (!IsAxisReady(pickerX, "PickerX", out readyReason) ||
                    !IsAxisReady(stageY, "StageY", out readyReason) ||
                    !IsAxisReady(pickerZ, "PickerZ", out readyReason))
                {
                    return MoveFail(result, -1, "PickUp ContiNode axis is not ready. " + readyReason, watch);
                }

                if (nodes == null || nodes.Count < 3)
                    return MoveFail(result, -1, "PickUp ContiNode node list is empty.", watch);

                IList<PickerPickUpContiNode> motionNodes = nodes;

                int[] requestedAxes =
                {
                    pickerX.Setup.AxisNo,
                    stageY.Setup.AxisNo,
                    pickerZ.Setup.AxisNo
                };

                if (requestedAxes.Distinct().Count() != requestedAxes.Length)
                    return MoveFail(result, -1, "PickUp ContiNode axis number duplicated. axes=" + string.Join(",", requestedAxes), watch);

                int[] mappedAxes = requestedAxes.OrderBy(x => x).ToArray();
                result.RequestedAxes = requestedAxes;
                result.MappedAxes = mappedAxes;
                result.RequestedPositions = FlattenNodes(motionNodes);

                coordinateLease = await AjinVirtualCoordinatePool.Instance
                    .RentAsync(config.TransferContiCoordinate, config.TransferContiTimeoutMs, ct)
                    .ConfigureAwait(false);
                if (coordinateLease == null)
                {
                    return MoveFail(
                        result,
                        -1,
                        "PickUp ContiNode usable virtual coordinate is empty. preferred=" + config.TransferContiCoordinate +
                        ", pool=" + AjinVirtualCoordinatePool.Instance.BuildStateText(),
                        watch);
                }

                int coordinate = coordinateLease.Coordinate;
                result.Coordinate = coordinate;

                int mapRetryCount = 0;
                int ret = AjinInterpolatedMotionService.SetPathAxisMapWithRetry(coordinate, mappedAxes, out mapRetryCount);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode SetPathAxisMap failed. coordinate=" + coordinate + ", retryCount=" + mapRetryCount, watch);

                ret = AXM.ClearPath(coordinate);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode ClearPath failed. coordinate=" + coordinate, watch);

                ret = AXM.SetPathAbsRelMode(coordinate, AXT_MOTION_ABSREL.POS_ABS_MODE);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode abs mode setup failed. coordinate=" + coordinate, watch);

                ret = ApplyContiAxisProfile(mappedAxes);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode S-Curve profile setup failed. coordinate=" + coordinate, watch);

                double[] splineX = MapNodeAxisPositions(motionNodes, mappedAxes[0], requestedAxes);
                double[] splineY = MapNodeAxisPositions(motionNodes, mappedAxes[1], requestedAxes);
                double splineZ = GetNodeAxisPosition(motionNodes[motionNodes.Count - 1], mappedAxes[2], requestedAxes);
                ret = AXM.SplineWrite(
                    coordinate,
                    splineX,
                    splineY,
                    splineZ,
                    result.Velocity,
                    result.Acceleration,
                    result.Deceleration,
                    1);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode SplineWrite failed. coordinate=" + coordinate, watch);

                ret = AXM.StartPath(coordinate, (uint)AXT_MOTION_CONTISTART_NODE.CONTI_NODE_VELOCITY, 0);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode StartPath failed. coordinate=" + coordinate, watch);

                result.CommandIssued = true;
                result.MappedPositions = MapNodePosition(motionNodes[motionNodes.Count - 1], requestedAxes, mappedAxes);

                DateTime deadline = DateTime.UtcNow.AddMilliseconds(config.TransferContiTimeoutMs <= 0 ? 5000 : config.TransferContiTimeoutMs);
                while (DateTime.UtcNow <= deadline)
                {
                    ct.ThrowIfCancellationRequested();

                    bool moving = false;
                    ret = AXM.IsPathMoving(coordinate, ref moving);
                    if (ret != 0)
                        return MoveFail(result, ret, "PickUp ContiNode moving-state check failed. coordinate=" + coordinate, watch);

                    if (!moving)
                    {
                        ret = AjinInterpolatedMotionService.ReleasePathAndReturnCoordinateIfIdle(coordinateLease, out bool skippedMoving);
                        if (ret != 0 || skippedMoving)
                            return MoveFail(result, ret != 0 ? ret : -1, "PickUp ContiNode coordinate release failed after complete. coordinate=" + coordinate + ", skippedMoving=" + skippedMoving, watch);
                        coordinateLease = null;

                        result.ResultCode = 0;
                        result.ElapsedMs = watch.ElapsedMilliseconds;
                        result.Message = "PickUp ContiNode SplineWrite completed. nodeCount=" + motionNodes.Count +
                            ", coordinate=" + coordinate + " returned.";
                        return result;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                return MoveFail(result, -1, "PickUp ContiNode timeout. coordinate=" + coordinate, watch);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return MoveFail(result, -1, "PickUp ContiNode exception. error=" + ex.Message, watch);
            }
            finally
            {
                if (coordinateLease != null)
                    AjinInterpolatedMotionService.ReleasePathAndReturnCoordinateIfIdle(coordinateLease, out _);
            }
        }

        private static double[] FlattenNodes(IList<PickerPickUpContiNode> nodes)
        {
            var values = new List<double>();
            foreach (PickerPickUpContiNode node in nodes)
            {
                values.Add(node.PickerX);
                values.Add(node.StageY);
                values.Add(node.PickerZ);
            }

            return values.ToArray();
        }

        private static IList<PickerPickUpContiNode> ExpandSplineNodes(IList<PickerPickUpContiNode> nodes, double splineCurvePercent)
        {
            var expanded = new List<PickerPickUpContiNode>();
            if (nodes == null || nodes.Count == 0)
                return expanded;

            double curveRatio = PickerPickUpMotionConfig.NormalizeSplineCurvePercent(splineCurvePercent, 100.0) / 100.0;
            double xyCurveRatio = Math.Min(1.0, curveRatio);

            expanded.Add(nodes[0]);
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                PickerPickUpContiNode p0 = nodes[Math.Max(0, i - 1)];
                PickerPickUpContiNode p1 = nodes[i];
                PickerPickUpContiNode p2 = nodes[i + 1];
                PickerPickUpContiNode p3 = nodes[Math.Min(nodes.Count - 1, i + 2)];
                bool useLinearInterpolation = i == nodes.Count - 2;

                if (!IsSamePosition(p1, p2))
                {
                    for (int step = 1; step <= SplineNodeCountPerSegment; step++)
                    {
                        double t = step / (double)(SplineNodeCountPerSegment + 1);
                        expanded.Add(new PickerPickUpContiNode(
                            p2.Index,
                            useLinearInterpolation ? LinearValue(p1.PickerX, p2.PickerX, t) : SplineValue(p0.PickerX, p1.PickerX, p2.PickerX, p3.PickerX, t, xyCurveRatio),
                            useLinearInterpolation ? LinearValue(p1.NeedleX, p2.NeedleX, t) : SplineValue(p0.NeedleX, p1.NeedleX, p2.NeedleX, p3.NeedleX, t, xyCurveRatio),
                            useLinearInterpolation ? LinearValue(p1.StageY, p2.StageY, t) : SplineValue(p0.StageY, p1.StageY, p2.StageY, p3.StageY, t, xyCurveRatio),
                            useLinearInterpolation ? LinearValue(p1.PickerZ, p2.PickerZ, t) : CurvedZValue(p1.PickerZ, p2.PickerZ, t, curveRatio)));
                    }
                }

                expanded.Add(p2);
            }

            return expanded;
        }

        private static ContiNodeMotionProfile ResolveDistanceLimitedProfile(
            int nodeIndex,
            double segmentDistance,
            PickerPickUpMotionConfig config)
        {
            double velocity = config.GetTransferContiNodeVelocity(nodeIndex);
            double acceleration = config.GetTransferContiNodeAcceleration(nodeIndex);
            double deceleration = config.GetTransferContiNodeDeceleration(nodeIndex);

            if (segmentDistance <= 0.0 ||
                acceleration <= 0.0 ||
                deceleration <= 0.0)
            {
                return new ContiNodeMotionProfile(velocity, acceleration, deceleration);
            }

            double denominator = acceleration + deceleration;
            if (denominator <= 0.0)
                return new ContiNodeMotionProfile(velocity, acceleration, deceleration);

            double distanceLimitedVelocity = Math.Sqrt((2.0 * acceleration * deceleration * segmentDistance) / denominator) *
                ShortSegmentVelocitySafetyRatio;
            if (double.IsNaN(distanceLimitedVelocity) || double.IsInfinity(distanceLimitedVelocity) || distanceLimitedVelocity <= 0.0)
                return new ContiNodeMotionProfile(velocity, acceleration, deceleration);

            if (distanceLimitedVelocity < velocity)
            {
                velocity = Math.Max(MinimumContiVelocity, distanceLimitedVelocity);
                double accelerationScale = ShortSegmentVelocitySafetyRatio * ShortSegmentVelocitySafetyRatio;
                acceleration = Math.Max(MinimumContiAcceleration, acceleration * accelerationScale);
                deceleration = Math.Max(MinimumContiAcceleration, deceleration * accelerationScale);
            }

            return new ContiNodeMotionProfile(velocity, acceleration, deceleration);
        }

        private static double CalculateNodeDistance(PickerPickUpContiNode start, PickerPickUpContiNode end)
        {
            double pickerX = end.PickerX - start.PickerX;
            double needleX = end.NeedleX - start.NeedleX;
            double stageY = end.StageY - start.StageY;
            double pickerZ = end.PickerZ - start.PickerZ;
            return Math.Sqrt(
                (pickerX * pickerX) +
                (needleX * needleX) +
                (stageY * stageY) +
                (pickerZ * pickerZ));
        }

        private static double[] MapNodePosition(PickerPickUpContiNode node, int[] requestedAxes, int[] mappedAxes)
        {
            var positionByAxis = new Dictionary<int, double>();
            positionByAxis[requestedAxes[0]] = node.PickerX;
            positionByAxis[requestedAxes[1]] = node.StageY;
            positionByAxis[requestedAxes[2]] = node.PickerZ;

            return mappedAxes.Select(axis => positionByAxis[axis]).ToArray();
        }

        private static double[] MapNodeAxisPositions(IList<PickerPickUpContiNode> nodes, int axis, int[] requestedAxes)
        {
            return nodes.Select(node => GetNodeAxisPosition(node, axis, requestedAxes)).ToArray();
        }

        private static double GetNodeAxisPosition(PickerPickUpContiNode node, int axis, int[] requestedAxes)
        {
            if (axis == requestedAxes[0])
                return node.PickerX;
            if (axis == requestedAxes[1])
                return node.StageY;
            if (axis == requestedAxes[2])
                return node.PickerZ;

            return 0.0;
        }

        private static int ApplyContiAxisProfile(int[] mappedAxes)
        {
            if (mappedAxes == null)
                return -1;

            foreach (int axis in mappedAxes.Distinct())
            {
                int ret = AXM.SetProfileMode(axis, AXT_MOTION_PROFILE_MODE.SYM_S_CURVE_MODE);
                if (ret != 0)
                    return ret;
            }

            return 0;
        }

        private static bool IsSamePosition(PickerPickUpContiNode a, PickerPickUpContiNode b)
        {
            const double tolerance = 0.000001;
            if (a == null || b == null)
                return false;

            return Math.Abs(a.PickerX - b.PickerX) <= tolerance &&
                Math.Abs(a.NeedleX - b.NeedleX) <= tolerance &&
                Math.Abs(a.StageY - b.StageY) <= tolerance &&
                Math.Abs(a.PickerZ - b.PickerZ) <= tolerance;
        }

        private static double SplineValue(double p0, double p1, double p2, double p3, double t, double curveRatio)
        {
            double t2 = t * t;
            double t3 = t2 * t;
            if (curveRatio <= 0.0)
                return LinearValue(p1, p2, t);

            double m1 = 0.5 * (p2 - p0) * curveRatio;
            double m2 = 0.5 * (p3 - p1) * curveRatio;
            double value =
                ((2.0 * t3) - (3.0 * t2) + 1.0) * p1 +
                (t3 - (2.0 * t2) + t) * m1 +
                ((-2.0 * t3) + (3.0 * t2)) * p2 +
                (t3 - t2) * m2;

            if (double.IsNaN(value) || double.IsInfinity(value))
                value = p1 + ((p2 - p1) * t);

            return ClampToSegment(value, p1, p2);
        }

        private static double CurvedZValue(double start, double end, double t, double curveRatio)
        {
            if (curveRatio <= 0.0)
                return LinearValue(start, end, t);

            double exponent = 1.0 + Math.Min(2.0, curveRatio * 0.4);
            double progress = end >= start
                ? 1.0 - Math.Pow(1.0 - t, exponent)
                : Math.Pow(t, exponent);

            return LinearValue(start, end, Clamp01(progress));
        }

        private static double LinearValue(double start, double end, double t)
        {
            double value = start + ((end - start) * t);
            if (double.IsNaN(value) || double.IsInfinity(value))
                return start;
            return value;
        }

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0.0;
            if (value < 0.0)
                return 0.0;
            if (value > 1.0)
                return 1.0;
            return value;
        }

        private static double ClampToSegment(double value, double start, double end)
        {
            double min = Math.Min(start, end);
            double max = Math.Max(start, end);
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private sealed class ContiNodeMotionProfile
        {
            public ContiNodeMotionProfile(double velocity, double acceleration, double deceleration)
            {
                Velocity = velocity;
                Acceleration = acceleration;
                Deceleration = deceleration;
            }

            public double Velocity { get; private set; }
            public double Acceleration { get; private set; }
            public double Deceleration { get; private set; }
        }

        private static bool IsAxisReady(BaseAxis axis, string name, out string reason)
        {
            reason = string.Empty;

            if (axis == null)
            {
                reason = name + " axis is null.";
                return false;
            }

            if (axis.Setup == null || axis.Setup.AxisNo < 0)
            {
                reason = name + " axis number is not configured.";
                return false;
            }

            if (!axis.IsServoOn)
            {
                reason = name + " servo is off.";
                return false;
            }

            if (axis.IsAlarm)
            {
                reason = name + " alarm is on.";
                return false;
            }

            if (axis.IsMoving)
            {
                reason = name + " is moving.";
                return false;
            }

            return true;
        }

        private static InterpolatedMotionMoveResult MoveFail(InterpolatedMotionMoveResult result, int code, string message, Stopwatch watch)
        {
            result.ResultCode = code == 0 ? -1 : code;
            result.ElapsedMs = watch != null ? watch.ElapsedMilliseconds : 0;
            result.Message = message;
            return result;
        }
    }
}
