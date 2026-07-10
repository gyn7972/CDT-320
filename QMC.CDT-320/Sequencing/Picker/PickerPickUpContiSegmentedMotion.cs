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
        public static async Task<InterpolatedMotionMoveResult> MovePickerXNeedleXStageYAndPickerZByNodesAsync(
            BaseAxis pickerX,
            BaseAxis needleX,
            BaseAxis stageY,
            BaseAxis pickerZ,
            IList<PickerPickUpContiNode> nodes,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            var result = new InterpolatedMotionMoveResult();
            Stopwatch watch = Stopwatch.StartNew();

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
                    !IsAxisReady(needleX, "NeedleX", out readyReason) ||
                    !IsAxisReady(stageY, "StageY", out readyReason) ||
                    !IsAxisReady(pickerZ, "PickerZ", out readyReason))
                {
                    return MoveFail(result, -1, "PickUp ContiNode axis is not ready. " + readyReason, watch);
                }

                if (nodes == null || nodes.Count == 0)
                    return MoveFail(result, -1, "PickUp ContiNode node list is empty.", watch);

                int[] requestedAxes =
                {
                    pickerX.Setup.AxisNo,
                    needleX.Setup.AxisNo,
                    stageY.Setup.AxisNo,
                    pickerZ.Setup.AxisNo
                };

                if (requestedAxes.Distinct().Count() != requestedAxes.Length)
                    return MoveFail(result, -1, "PickUp ContiNode axis number duplicated. axes=" + string.Join(",", requestedAxes), watch);

                int[] mappedAxes = requestedAxes.OrderBy(x => x).ToArray();
                result.RequestedAxes = requestedAxes;
                result.MappedAxes = mappedAxes;
                result.RequestedPositions = FlattenNodes(nodes);

                int ret = AXM.SetPathAxisMap(config.TransferContiCoordinate, mappedAxes);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode SetPathAxisMap failed. coordinate=" + config.TransferContiCoordinate, watch);

                ret = AXM.ClearPath(config.TransferContiCoordinate);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode ClearPath failed. coordinate=" + config.TransferContiCoordinate, watch);

                ret = AXM.SetPathAbsRelMode(config.TransferContiCoordinate, AXT_MOTION_ABSREL.POS_ABS_MODE);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode abs mode setup failed. coordinate=" + config.TransferContiCoordinate, watch);

                ret = AXM.BeginPath(config.TransferContiCoordinate);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode BeginPath failed. coordinate=" + config.TransferContiCoordinate, watch);

                for (int i = 0; i < nodes.Count; i++)
                {
                    PickerPickUpContiNode node = nodes[i];
                    double[] mappedPosition = MapNodePosition(node, requestedAxes, mappedAxes);
                    ret = AXM.MoveLine(
                        config.TransferContiCoordinate,
                        mappedAxes,
                        mappedPosition,
                        config.GetTransferContiNodeVelocity(node.Index),
                        config.GetTransferContiNodeAcceleration(node.Index),
                        config.GetTransferContiNodeDeceleration(node.Index));
                    if (ret != 0)
                        return MoveFail(result, ret, "PickUp ContiNode node" + node.Index + " registration failed. coordinate=" + config.TransferContiCoordinate, watch);
                }

                ret = AXM.EndPath(config.TransferContiCoordinate);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode EndPath failed. coordinate=" + config.TransferContiCoordinate, watch);

                ret = AXM.StartPath(config.TransferContiCoordinate, 0, 0);
                if (ret != 0)
                    return MoveFail(result, ret, "PickUp ContiNode StartPath failed. coordinate=" + config.TransferContiCoordinate, watch);

                result.CommandIssued = true;
                result.MappedPositions = MapNodePosition(nodes[nodes.Count - 1], requestedAxes, mappedAxes);

                DateTime deadline = DateTime.UtcNow.AddMilliseconds(config.TransferContiTimeoutMs <= 0 ? 5000 : config.TransferContiTimeoutMs);
                while (DateTime.UtcNow <= deadline)
                {
                    ct.ThrowIfCancellationRequested();

                    bool moving = false;
                    ret = AXM.IsPathMoving(config.TransferContiCoordinate, ref moving);
                    if (ret != 0)
                        return MoveFail(result, ret, "PickUp ContiNode moving-state check failed. coordinate=" + config.TransferContiCoordinate, watch);

                    if (!moving)
                    {
                        result.ResultCode = 0;
                        result.ElapsedMs = watch.ElapsedMilliseconds;
                        result.Message = "PickUp ContiNode completed. nodeCount=" + nodes.Count;
                        return result;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                return MoveFail(result, -1, "PickUp ContiNode timeout. coordinate=" + config.TransferContiCoordinate, watch);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return MoveFail(result, -1, "PickUp ContiNode exception. error=" + ex.Message, watch);
            }
        }

        private static double[] FlattenNodes(IList<PickerPickUpContiNode> nodes)
        {
            var values = new List<double>();
            foreach (PickerPickUpContiNode node in nodes)
            {
                values.Add(node.PickerX);
                values.Add(node.NeedleX);
                values.Add(node.StageY);
                values.Add(node.PickerZ);
            }

            return values.ToArray();
        }

        private static double[] MapNodePosition(PickerPickUpContiNode node, int[] requestedAxes, int[] mappedAxes)
        {
            var positionByAxis = new Dictionary<int, double>();
            positionByAxis[requestedAxes[0]] = node.PickerX;
            positionByAxis[requestedAxes[1]] = node.NeedleX;
            positionByAxis[requestedAxes[2]] = node.StageY;
            positionByAxis[requestedAxes[3]] = node.PickerZ;

            return mappedAxes.Select(axis => positionByAxis[axis]).ToArray();
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
