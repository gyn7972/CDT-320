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
    internal sealed class PickerPlaceContiNode
    {
        public PickerPlaceContiNode(int index, double stageY, double pickerX, double previousPickerZ, double pickerZ)
        {
            Index = index;
            StageY = stageY;
            PickerX = pickerX;
            PreviousPickerZ = previousPickerZ;
            PickerZ = pickerZ;
        }

        public int Index { get; private set; }
        public double StageY { get; private set; }
        public double PickerX { get; private set; }
        public double PreviousPickerZ { get; private set; }
        public double PickerZ { get; private set; }
    }

    internal static class PickerPlaceContiSegmentedMotion
    {
        public static async Task<InterpolatedMotionMoveResult> MoveStageYPickerXAndPickerZByNodesAsync(
            BaseAxis outputStageY,
            BaseAxis pickerX,
            BaseAxis previousPickerZ,
            BaseAxis pickerZ,
            IList<PickerPlaceContiNode> nodes,
            PickerPlaceMotionConfig config,
            CancellationToken ct)
        {
            var result = new InterpolatedMotionMoveResult();
            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                if (config == null)
                    config = new PickerPlaceMotionConfig();
                config.Ensure();

                result.Coordinate = config.InterpolationCoordinate;
                result.TimeoutMs = config.SynchronizedTimeoutMs;
                result.Velocity = config.ContiNode4Velocity;
                result.Acceleration = config.ContiNode4Acceleration;
                result.Deceleration = config.ContiNode4Deceleration;

                string readyReason;
                if (!IsAxisReady(outputStageY, "OutputStageY", out readyReason) ||
                    !IsAxisReady(pickerX, "PickerX", out readyReason) ||
                    !IsAxisReady(previousPickerZ, "PreviousPickerZ", out readyReason) ||
                    !IsAxisReady(pickerZ, "PickerZ", out readyReason))
                {
                    return MoveFail(result, -1, "Place ContiNode 구동 전 축 준비 상태가 맞지 않습니다. " + readyReason, watch);
                }

                if (nodes == null || nodes.Count == 0)
                    return MoveFail(result, -1, "Place ContiNode 노드 목록이 비어 있습니다.", watch);

                int[] requestedAxes =
                {
                    outputStageY.Setup.AxisNo,
                    pickerX.Setup.AxisNo,
                    previousPickerZ.Setup.AxisNo,
                    pickerZ.Setup.AxisNo
                };

                if (requestedAxes.Distinct().Count() != requestedAxes.Length)
                    return MoveFail(result, -1, "Place ContiNode 축 번호가 중복되었습니다. axes=" + string.Join(",", requestedAxes), watch);

                int[] mappedAxes = requestedAxes.OrderBy(x => x).ToArray();
                result.RequestedAxes = requestedAxes;
                result.MappedAxes = mappedAxes;
                result.RequestedPositions = FlattenNodes(nodes);

                int ret = AXM.SetPathAxisMap(config.InterpolationCoordinate, mappedAxes);
                if (ret != 0)
                    return MoveFail(result, ret, "Place ContiNode 축 맵 설정 실패. coordinate=" + config.InterpolationCoordinate, watch);

                ret = AXM.ClearPath(config.InterpolationCoordinate);
                if (ret != 0)
                    return MoveFail(result, ret, "Place ContiNode 버퍼 초기화 실패. coordinate=" + config.InterpolationCoordinate, watch);

                ret = AXM.SetPathAbsRelMode(config.InterpolationCoordinate, AXT_MOTION_ABSREL.POS_ABS_MODE);
                if (ret != 0)
                    return MoveFail(result, ret, "Place ContiNode 절대좌표 모드 설정 실패. coordinate=" + config.InterpolationCoordinate, watch);

                ret = AXM.BeginPath(config.InterpolationCoordinate);
                if (ret != 0)
                    return MoveFail(result, ret, "Place ContiNode BeginNode 실패. coordinate=" + config.InterpolationCoordinate, watch);

                for (int i = 0; i < nodes.Count; i++)
                {
                    PickerPlaceContiNode node = nodes[i];
                    double[] mappedPosition = MapNodePosition(node, requestedAxes, mappedAxes);
                    ret = AXM.MoveLine(
                        config.InterpolationCoordinate,
                        mappedAxes,
                        mappedPosition,
                        config.GetContiNodeVelocity(node.Index),
                        config.GetContiNodeAcceleration(node.Index),
                        config.GetContiNodeDeceleration(node.Index));
                    if (ret != 0)
                        return MoveFail(result, ret, "Place ContiNode node" + node.Index + " 등록 실패. coordinate=" + config.InterpolationCoordinate, watch);
                }

                ret = AXM.EndPath(config.InterpolationCoordinate);
                if (ret != 0)
                    return MoveFail(result, ret, "Place ContiNode EndNode 실패. coordinate=" + config.InterpolationCoordinate, watch);

                ret = AXM.StartPath(config.InterpolationCoordinate, 0, 0);
                if (ret != 0)
                    return MoveFail(result, ret, "Place ContiNode Start 실패. coordinate=" + config.InterpolationCoordinate, watch);

                result.CommandIssued = true;
                result.MappedPositions = MapNodePosition(nodes[nodes.Count - 1], requestedAxes, mappedAxes);

                DateTime deadline = DateTime.UtcNow.AddMilliseconds(config.SynchronizedTimeoutMs <= 0 ? 5000 : config.SynchronizedTimeoutMs);
                while (DateTime.UtcNow <= deadline)
                {
                    ct.ThrowIfCancellationRequested();

                    bool moving = false;
                    ret = AXM.IsPathMoving(config.InterpolationCoordinate, ref moving);
                    if (ret != 0)
                        return MoveFail(result, ret, "Place ContiNode 구동 상태 확인 실패. coordinate=" + config.InterpolationCoordinate, watch);

                    if (!moving)
                    {
                        result.ResultCode = 0;
                        result.ElapsedMs = watch.ElapsedMilliseconds;
                        result.Message = "Place ContiNode 구동 완료. nodeCount=" + nodes.Count;
                        return result;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                return MoveFail(result, -1, "Place ContiNode 구동 완료 대기 시간 초과. coordinate=" + config.InterpolationCoordinate, watch);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return MoveFail(result, -1, "Place ContiNode 구동 중 예외가 발생했습니다. error=" + ex.Message, watch);
            }
        }

        private static double[] FlattenNodes(IList<PickerPlaceContiNode> nodes)
        {
            var values = new List<double>();
            foreach (PickerPlaceContiNode node in nodes)
            {
                values.Add(node.StageY);
                values.Add(node.PickerX);
                values.Add(node.PreviousPickerZ);
                values.Add(node.PickerZ);
            }

            return values.ToArray();
        }

        private static double[] MapNodePosition(PickerPlaceContiNode node, int[] requestedAxes, int[] mappedAxes)
        {
            var positionByAxis = new Dictionary<int, double>();
            positionByAxis[requestedAxes[0]] = node.StageY;
            positionByAxis[requestedAxes[1]] = node.PickerX;
            positionByAxis[requestedAxes[2]] = node.PreviousPickerZ;
            positionByAxis[requestedAxes[3]] = node.PickerZ;

            return mappedAxes.Select(axis => positionByAxis[axis]).ToArray();
        }

        private static bool IsAxisReady(BaseAxis axis, string name, out string reason)
        {
            reason = string.Empty;

            if (axis == null)
            {
                reason = name + " 축을 찾을 수 없습니다.";
                return false;
            }

            if (axis.Setup == null || axis.Setup.AxisNo < 0)
            {
                reason = name + " 축 번호가 설정되지 않았습니다.";
                return false;
            }

            if (!axis.IsServoOn)
            {
                reason = name + " 서보가 OFF 상태입니다.";
                return false;
            }

            if (axis.IsAlarm)
            {
                reason = name + " 축 알람이 ON 상태입니다.";
                return false;
            }

            if (axis.IsMoving)
            {
                reason = name + " 축이 이미 이동 중입니다.";
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
