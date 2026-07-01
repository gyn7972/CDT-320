using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion.Ajin;

namespace QMC.Common.Motion
{
    /// <summary>
    /// Ajin 보간 구동(AxmLineMove) 기반의 동시도착 모션을 검증하고 준비하는 공용 서비스입니다.
    /// 실제 이동 명령은 호출하지 않고, 좌표계/축 맵핑 가능 여부만 먼저 확인할 수 있게 분리했습니다.
    /// </summary>
    public static class AjinInterpolatedMotionService
    {
        public static InterpolatedMotionMapResult ValidateSynchronizedArrivalMap(
            int coordinate,
            IEnumerable<int> axisNumbers,
            bool relativeMode)
        {
            var result = new InterpolatedMotionMapResult
            {
                Coordinate = coordinate
            };

            try
            {
                if (axisNumbers == null)
                {
                    return Fail(result, -1, "보간 축 목록이 비어 있습니다.");
                }

                int[] requestedAxes = axisNumbers.ToArray();
                result.RequestedAxes = requestedAxes.ToArray();

                if (requestedAxes.Length < 2 || requestedAxes.Length > 4)
                {
                    return Fail(result, -1, "AxmLineMove 보간 맵핑은 2축 이상 4축 이하만 허용합니다. axes=" + result.RequestedAxesText);
                }

                if (requestedAxes.Any(x => x < 0))
                {
                    return Fail(result, -1, "보간 축 번호에 음수가 포함되어 있습니다. axes=" + result.RequestedAxesText);
                }

                if (requestedAxes.Distinct().Count() != requestedAxes.Length)
                {
                    return Fail(result, -1, "보간 축 번호가 중복되었습니다. axes=" + result.RequestedAxesText);
                }

                int[] mappedAxes = requestedAxes.OrderBy(x => x).ToArray();

                for (int i = 0; i < mappedAxes.Length; i++)
                {
                    var info = new InterpolatedMotionAxisInfo { AxisNo = mappedAxes[i] };
                    int nodeNum = 0;
                    int modulePosition = 0;
                    uint moduleId = 0;
                    int axisInfoResult = AXM.GetAxisInfo(mappedAxes[i], ref nodeNum, ref modulePosition, ref moduleId);
                    info.NodeNum = nodeNum;
                    info.ModulePosition = modulePosition;
                    info.ModuleId = moduleId;
                    result.AxisInfos.Add(info);
                    if (axisInfoResult != 0)
                    {
                        return Fail(result, axisInfoResult, "축 정보 조회 실패. axis=" + mappedAxes[i]);
                    }
                }

                int ret = AXM.SetPathAxisMap(coordinate, mappedAxes);
                if (ret != 0)
                {
                    return Fail(result, ret, "보간 축 맵핑 실패. coordinate=" + coordinate + ", axes=" + string.Join(",", mappedAxes));
                }

                ret = AXM.ClearPath(coordinate);
                if (ret != 0)
                {
                    return Fail(result, ret, "보간 좌표계 초기화 실패. coordinate=" + coordinate + ", axes=" + string.Join(",", mappedAxes));
                }

                var mode = relativeMode ? AXT_MOTION_ABSREL.POS_REL_MODE : AXT_MOTION_ABSREL.POS_ABS_MODE;
                ret = AXM.SetPathAbsRelMode(coordinate, mode);
                if (ret != 0)
                {
                    return Fail(result, ret, "보간 좌표계 절대/상대 모드 설정 실패. coordinate=" + coordinate + ", mode=" + mode);
                }

                uint mappedSize = (uint)mappedAxes.Length;
                int[] readAxes = new int[mappedAxes.Length];
                ret = AXM.GetPathAxisMap(coordinate, ref mappedSize, readAxes);
                if (ret != 0)
                {
                    return Fail(result, ret, "보간 축 맵핑 조회 실패. coordinate=" + coordinate);
                }

                AXT_MOTION_ABSREL readMode = AXT_MOTION_ABSREL.POS_ABS_MODE;
                ret = AXM.GetPathAbsRelMode(coordinate, ref readMode);
                if (ret != 0)
                {
                    return Fail(result, ret, "보간 좌표계 절대/상대 모드 조회 실패. coordinate=" + coordinate);
                }

                result.MappedSize = mappedSize;
                result.MappedAxes = readAxes.Take((int)mappedSize).ToArray();
                result.AbsRelMode = readMode;

                if (mappedSize != mappedAxes.Length || !result.MappedAxes.SequenceEqual(mappedAxes))
                {
                    return Fail(
                        result,
                        -1,
                        "보간 축 맵핑 확인값이 요청값과 다릅니다. request=" + string.Join(",", mappedAxes) +
                        ", actual=" + result.MappedAxesText +
                        ", size=" + mappedSize);
                }

                if (readMode != mode)
                {
                    return Fail(result, -1, "보간 좌표계 모드 확인값이 요청값과 다릅니다. request=" + mode + ", actual=" + readMode);
                }

                result.ResultCode = 0;
                result.Message = "보간 축 맵핑 검증 성공.";
                return result;
            }
            catch (Exception ex)
            {
                return Fail(result, -1, "보간 축 맵핑 검증 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        public static async Task<InterpolatedMotionMoveResult> RunSynchronizedArrivalRelativeMoveAsync(
            int coordinate,
            int[] axisNumbers,
            double[] relativePositions,
            double velocity,
            double acceleration,
            double deceleration,
            int timeoutMs,
            CancellationToken ct)
        {
            var result = new InterpolatedMotionMoveResult
            {
                Coordinate = coordinate,
                RequestedAxes = axisNumbers != null ? axisNumbers.ToArray() : new int[0],
                RequestedPositions = relativePositions != null ? relativePositions.ToArray() : new double[0],
                Velocity = velocity,
                Acceleration = acceleration,
                Deceleration = deceleration,
                TimeoutMs = timeoutMs
            };

            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                if (axisNumbers == null || relativePositions == null)
                    return MoveFail(result, -1, "보간 이동 축 또는 위치 목록이 비어 있습니다.", watch);

                if (axisNumbers.Length != relativePositions.Length)
                    return MoveFail(result, -1, "보간 이동 축 개수와 위치 개수가 다릅니다. axes=" + axisNumbers.Length + ", positions=" + relativePositions.Length, watch);

                if (velocity <= 0 || acceleration <= 0 || deceleration <= 0)
                    return MoveFail(result, -1, "보간 이동 속도/가감속 값은 0보다 커야 합니다. vel=" + velocity + ", acc=" + acceleration + ", dec=" + deceleration, watch);

                InterpolatedMotionMapResult map = ValidateSynchronizedArrivalMap(coordinate, axisNumbers, true);
                if (!map.Success)
                    return MoveFail(result, map.ResultCode, "보간 이동 전 축 맵핑 검증 실패. " + map.Message, watch);

                var ordered = axisNumbers
                    .Select((axis, index) => new { Axis = axis, Position = relativePositions[index] })
                    .OrderBy(x => x.Axis)
                    .ToArray();

                result.MappedAxes = ordered.Select(x => x.Axis).ToArray();
                result.MappedPositions = ordered.Select(x => x.Position).ToArray();

                int ret = AXM.MoveLine(coordinate, result.MappedAxes, result.MappedPositions, velocity, acceleration, deceleration);
                if (ret != 0)
                    return MoveFail(result, ret, "보간 이동 명령 실패. coordinate=" + coordinate, watch);

                result.CommandIssued = true;

                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs <= 0 ? 5000 : timeoutMs);
                while (DateTime.UtcNow <= deadline)
                {
                    ct.ThrowIfCancellationRequested();

                    bool moving = false;
                    ret = AXM.IsPathMoving(coordinate, ref moving);
                    if (ret != 0)
                        return MoveFail(result, ret, "보간 이동 상태 확인 실패. coordinate=" + coordinate, watch);

                    if (!moving)
                    {
                        result.ResultCode = 0;
                        result.ElapsedMs = watch.ElapsedMilliseconds;
                        result.Message = "보간 이동 완료.";
                        return result;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                return MoveFail(result, -1, "보간 이동 완료 대기 시간 초과. coordinate=" + coordinate, watch);
            }
            catch (OperationCanceledException)
            {
                return MoveFail(result, -1, "보간 이동 작업이 취소되었습니다.", watch);
            }
            catch (Exception ex)
            {
                return MoveFail(result, -1, "보간 이동 중 예외가 발생했습니다. error=" + ex.Message, watch);
            }
        }

        private static InterpolatedMotionMapResult Fail(InterpolatedMotionMapResult result, int code, string message)
        {
            result.ResultCode = code == 0 ? -1 : code;
            result.Message = message;
            return result;
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
