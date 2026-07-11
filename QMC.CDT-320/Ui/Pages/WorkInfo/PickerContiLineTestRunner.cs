using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Sequencing;
using QMC.CDT320.VisionComm;
using QMC.Common.Logging;
using QMC.Common.Motion;
using QMC.Common.Motion.Ajin;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal static class PickerContiLineTestRunner
    {
        public static bool EnsureAjinReady(out string reason)
        {
            reason = string.Empty;

            if (!AjinSystem.IsOpen)
            {
                reason = "AXL 라이브러리가 열려 있지 않아 ContiNode LineMap/LineMove 테스트를 실행할 수 없습니다. " +
                         "시뮬레이션 모드에서는 자동 시퀀스가 기존 이동 방식으로 fallback됩니다. 실제 장비에서 UseAjin/AXL Open 상태를 확인한 뒤 실행하세요.";
                return false;
            }

            return true;
        }

        public static PickerPlaceMotionConfig ResolvePlaceConfig(CDT320_Machine machine, PickerSequenceSide side)
        {
            PickerPlaceMotionConfig config = null;
            if (machine != null)
            {
                if (side == PickerSequenceSide.Front &&
                    machine.PickerFrontUnit != null &&
                    machine.PickerFrontUnit.Config != null)
                {
                    config = machine.PickerFrontUnit.Config.Place;
                }
                else if (side == PickerSequenceSide.Rear &&
                         machine.PickerRearUnit != null &&
                         machine.PickerRearUnit.Config != null)
                {
                    config = machine.PickerRearUnit.Config.Place;
                }
            }

            if (config == null)
                config = new PickerPlaceMotionConfig();
            config.Ensure();
            return config;
        }

        public static List<PickerContiLineMapTestResult> RunGoodStageYLineMapTests(CDT320_Machine machine, PickerSequenceSide side)
        {
            var results = new List<PickerContiLineMapTestResult>();
            BaseAxis goodStageY = machine != null &&
                                  machine.OutputStageUnit != null &&
                                  machine.OutputStageUnit.GoodStage != null
                ? machine.OutputStageUnit.GoodStage.StageY
                : null;

            int goodStageYAxisNo = ResolveAxisNo(goodStageY, "OutputGoodStageY");
            PickerPlaceMotionConfig placeConfig = ResolvePlaceConfig(machine, side);
            int pickerXAxisNo = ResolveAxisNo(ResolvePickerX(machine, side), SideName(side) + "PickerX");
            int[] pickerZAxisNos =
            {
                ResolveAxisNo(ResolvePickerZAxis(machine, side, 0), SideName(side) + "PickerZ1"),
                ResolveAxisNo(ResolvePickerZAxis(machine, side, 1), SideName(side) + "PickerZ2"),
                ResolveAxisNo(ResolvePickerZAxis(machine, side, 2), SideName(side) + "PickerZ3"),
                ResolveAxisNo(ResolvePickerZAxis(machine, side, 3), SideName(side) + "PickerZ4")
            };

            int[,] pairs =
            {
                { 3, 2 },
                { 2, 1 },
                { 1, 0 }
            };

            for (int i = 0; i < pairs.GetLength(0); i++)
            {
                int previousIndex = pairs[i, 0];
                int currentIndex = pairs[i, 1];
                string name = SideName(side) + " GOOD-Y/X/P" + (previousIndex + 1) + "-P" + (currentIndex + 1) +
                    " Z" + previousIndex + "-Z" + currentIndex +
                    " coord=" + placeConfig.ContiCoordinate;
                int[] axes = { goodStageYAxisNo, pickerXAxisNo, pickerZAxisNos[previousIndex], pickerZAxisNos[currentIndex] };
                InterpolatedMotionMapResult result = ValidateContiLineMap(placeConfig.ContiCoordinate, axes);
                results.Add(new PickerContiLineMapTestResult(name, result));
            }

            return results;
        }

        public static async Task<PickerContiLineMoveRunResult> RunGoodStagePlaceLineMoveTestAsync(
            CDT320_Machine machine,
            PickerSequenceSide side,
            CancellationToken ct)
        {
            if (machine == null)
                return PickerContiLineMoveRunResult.Fail("장비 객체를 찾을 수 없습니다.");

            try
            {
                PickerPlaceMotionConfig placeConfig = ResolvePlaceConfig(machine, side);
                PlaceLineMoveTarget[] targets = ResolvePlaceLineMoveTargets(machine, side);
                ValidatePlaceLineMoveTargets(side, targets);

                int prepareResult = await PreparePlaceLineMoveStartAsync(machine, side, targets, ct).ConfigureAwait(true);
                if (prepareResult != 0)
                    return PickerContiLineMoveRunResult.Fail("Place teaching start position 이동 실패. result=" + prepareResult);

                for (int i = 0; i < targets.Length - 1; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    PlaceLineMoveTarget previous = targets[i];
                    PlaceLineMoveTarget current = targets[i + 1];
                    LineMoveAxisSet axes = ResolveGoodStagePickerLineMoveAxes(
                        machine,
                        side,
                        previous.PickerIndex,
                        current.PickerIndex);

                    InterpolatedMotionMoveResult moveResult =
                        await RunContiLineMoveAsync(
                            axes,
                            placeConfig,
                            previous,
                            current,
                            ct).ConfigureAwait(true);

                    string detail = SideName(side) + " Place teaching ContiNode line move. fromPicker=" + previous.PickerNo +
                        ", toPicker=" + current.PickerNo +
                        ", targetStageY=" + current.StageY.ToString("F3") +
                        ", targetPickerX=" + current.PickerX.ToString("F3") +
                        ", previousZTarget=Avoid" +
                        ", currentZTarget=" + current.PickerZ.ToString("F3") +
                        ", " + moveResult;

                    EventLogger.Write(
                        moveResult.Success ? EventKind.Event : EventKind.Warning,
                        "UI",
                        "AJIN-LINE-MOVE-TEST",
                        SideName(side) + "PickerPage",
                        detail);

                    if (!moveResult.Success)
                    {
                        return PickerContiLineMoveRunResult.Fail(
                            "Picker #" + previous.PickerNo + " -> #" + current.PickerNo +
                            " ContiNode 이동 테스트 실패.\r\n" + moveResult.Message);
                    }
                }

                return PickerContiLineMoveRunResult.Ok(
                    SideName(side) + "Picker Place teaching ContiNode 이동 테스트가 완료되었습니다.\r\n" +
                    "마지막 위치는 Picker #1 Place 상태입니다.\r\n상세 내용은 Alarm/Event Log를 확인하세요.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return PickerContiLineMoveRunResult.Fail(
                    SideName(side) + "Picker GOOD StageY 기준 ContiNode 이동 테스트 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private static InterpolatedMotionMapResult ValidateContiLineMap(int coordinate, int[] requestedAxes)
        {
            var result = new InterpolatedMotionMapResult
            {
                Coordinate = coordinate,
                RequestedAxes = requestedAxes != null ? requestedAxes.ToArray() : new int[0]
            };

            try
            {
                if (coordinate <= 0)
                    return MapFail(result, -1, "ContiNode coordinate는 1 이상이어야 합니다. coordinate=" + coordinate);

                if (requestedAxes == null || requestedAxes.Length < 2 || requestedAxes.Length > 4)
                    return MapFail(result, -1, "ContiNode LineMap 축 개수가 맞지 않습니다. axes=" + result.RequestedAxesText);

                if (requestedAxes.Any(x => x < 0))
                    return MapFail(result, -1, "ContiNode LineMap 축 번호에 음수가 포함되어 있습니다. axes=" + result.RequestedAxesText);

                if (requestedAxes.Distinct().Count() != requestedAxes.Length)
                    return MapFail(result, -1, "ContiNode LineMap 축 번호가 중복되었습니다. axes=" + result.RequestedAxesText);

                int[] mappedAxes = requestedAxes.OrderBy(x => x).ToArray();

                int ret = AXM.SetPathAxisMap(coordinate, mappedAxes);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 축 맵 설정 실패. coordinate=" + coordinate + ", axes=" + string.Join(",", mappedAxes) + ", ret=" + DescribeAxlResult(ret));

                ret = AXM.ClearPath(coordinate);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 버퍼 초기화 실패. coordinate=" + coordinate + ", ret=" + DescribeAxlResult(ret));

                ret = AXM.SetPathAbsRelMode(coordinate, AXT_MOTION_ABSREL.POS_ABS_MODE);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 절대좌표 모드 설정 실패. coordinate=" + coordinate + ", ret=" + DescribeAxlResult(ret));

                uint mappedSize = (uint)mappedAxes.Length;
                int[] readAxes = new int[mappedAxes.Length];
                ret = AXM.GetPathAxisMap(coordinate, ref mappedSize, readAxes);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 축 맵 조회 실패. coordinate=" + coordinate + ", ret=" + DescribeAxlResult(ret));

                AXT_MOTION_ABSREL readMode = AXT_MOTION_ABSREL.POS_ABS_MODE;
                ret = AXM.GetPathAbsRelMode(coordinate, ref readMode);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 좌표 모드 조회 실패. coordinate=" + coordinate + ", ret=" + DescribeAxlResult(ret));

                result.MappedSize = mappedSize;
                result.MappedAxes = readAxes.Take((int)mappedSize).ToArray();
                result.AbsRelMode = readMode;

                if (mappedSize != mappedAxes.Length || !result.MappedAxes.SequenceEqual(mappedAxes))
                    return MapFail(result, -1, "ContiNode LineMap 확인값이 요청값과 다릅니다. request=" + string.Join(",", mappedAxes) + ", actual=" + result.MappedAxesText);

                if (readMode != AXT_MOTION_ABSREL.POS_ABS_MODE)
                    return MapFail(result, -1, "ContiNode LineMap 좌표 모드 확인값이 ABS가 아닙니다. actual=" + readMode);

                result.ResultCode = 0;
                result.Message = "ContiNode LineMap 검증 성공.";
                return result;
            }
            catch (Exception ex)
            {
                return MapFail(result, -1, "ContiNode LineMap 검증 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private static async Task<InterpolatedMotionMoveResult> RunContiLineMoveAsync(
            LineMoveAxisSet axes,
            PickerPlaceMotionConfig placeConfig,
            PlaceLineMoveTarget previous,
            PlaceLineMoveTarget current,
            CancellationToken ct)
        {
            if (axes == null)
                throw new InvalidOperationException("Conti LineMove 축 정보를 찾을 수 없습니다.");

            if (placeConfig == null)
                placeConfig = new PickerPlaceMotionConfig();
            placeConfig.Ensure();

            List<PickerPlaceContiNode> nodes = BuildLineMoveNodes(
                axes,
                placeConfig,
                previous,
                current);

            return await PickerPlaceContiSegmentedMotion.MoveStageYPickerXAndPickerZByNodesAsync(
                axes.StageY,
                axes.PickerX,
                axes.PickerZ,
                nodes,
                placeConfig,
                ct).ConfigureAwait(true);
        }

        private static List<PickerPlaceContiNode> BuildLineMoveNodes(
            LineMoveAxisSet axes,
            PickerPlaceMotionConfig placeConfig,
            PlaceLineMoveTarget previous,
            PlaceLineMoveTarget current)
        {
            double startStageY = axes.StageY.ActualPosition;
            double startPickerX = axes.PickerX.ActualPosition;
            double startPickerZ = axes.PickerZ.ActualPosition;
            double targetStageY = current.StageY;
            double targetPickerX = current.PickerX;
            double previousZStep1 = previous.PickerZ + placeConfig.ContiZ1Step1Clearance;
            double previousZStep2 = previous.PickerZ + placeConfig.ContiZ1Step1Clearance + placeConfig.ContiZ1Step2Clearance;
            double previousZNearAvoid = ResolveNearAvoidPosition(previous.PickerZAvoid, previous.PickerZ, placeConfig.ContiNearAvoidDistance);
            double currentZNearAvoid = ResolveNearAvoidPosition(current.PickerZAvoid, current.PickerZ, placeConfig.ContiNearAvoidDistance);
            double currentZStep = current.PickerZ + placeConfig.ContiZ1Step1Clearance + placeConfig.ContiZ1Step2Clearance;
            double ratio = placeConfig.ContiXYMidRatio;

            return new List<PickerPlaceContiNode>
            {
                new PickerPlaceContiNode(0, startStageY, startPickerX, previousZStep1, startPickerZ),
                new PickerPlaceContiNode(1, startStageY, startPickerX, previousZStep2, startPickerZ),
                new PickerPlaceContiNode(2, Lerp(startStageY, targetStageY, ratio), Lerp(startPickerX, targetPickerX, ratio), previousZNearAvoid, currentZNearAvoid),
                new PickerPlaceContiNode(3, targetStageY, targetPickerX, previousZNearAvoid, currentZStep),
                new PickerPlaceContiNode(4, targetStageY, targetPickerX, previous.PickerZAvoid, current.PickerZ)
            };
        }

        private static PlaceLineMoveTarget[] ResolvePlaceLineMoveTargets(CDT320_Machine machine, PickerSequenceSide side)
        {
            if (machine == null)
                throw new InvalidOperationException("Machine is missing.");
            if (side == PickerSequenceSide.Front && machine.PickerFrontUnit == null)
                throw new InvalidOperationException("FrontPicker unit is missing.");
            if (side == PickerSequenceSide.Rear && machine.PickerRearUnit == null)
                throw new InvalidOperationException("RearPicker unit is missing.");
            if (machine.OutputStageUnit == null || machine.OutputStageUnit.Recipe == null)
                throw new InvalidOperationException("OutputStage recipe is missing.");

            machine.OutputStageUnit.Recipe.EnsurePositionObjects();

            int[] order = { 3, 2, 1, 0 };
            var targets = new List<PlaceLineMoveTarget>();
            for (int i = 0; i < order.Length; i++)
            {
                int pickerIndex = order[i];
                PickerCalibratedZoneTarget pickerTarget =
                    CalibrationCoordinateService.ResolvePickerZoneTarget(
                        machine,
                        side == PickerSequenceSide.Front ? VisionFocusPickerSide.Front : VisionFocusPickerSide.Rear,
                        "DiePlacePosition",
                        pickerIndex,
                        null,
                        false,
                        false);

                PickerAxis zAxis = CalibrationCoordinateService.ResolvePickerZAxis(pickerIndex);
                targets.Add(new PlaceLineMoveTarget
                {
                    PickerIndex = pickerIndex,
                    PickerNo = pickerIndex + 1,
                    PickerTAxis = pickerTarget.PickerTAxis,
                    PickerZAxis = pickerTarget.PickerZAxis,
                    StageY = machine.OutputStageUnit.Recipe.GoodStageY.ProcessPosition - Math.Abs(pickerTarget.Y),
                    PickerX = pickerTarget.X,
                    PickerY = pickerTarget.Y,
                    PickerT = pickerTarget.T,
                    PickerZ = pickerTarget.Z,
                    PickerZAvoid = GetPickerTeachingPosition(machine, side, zAxis, "AvoidPosition")
                });
            }

            return targets.ToArray();
        }

        private static void ValidatePlaceLineMoveTargets(PickerSequenceSide side, PlaceLineMoveTarget[] targets)
        {
            if (targets == null || targets.Length < 2)
                throw new InvalidOperationException("Place teaching target count is invalid.");

            double pickerY = targets[0].PickerY;
            for (int i = 1; i < targets.Length; i++)
            {
                if (Math.Abs(targets[i].PickerY - pickerY) > 0.001)
                {
                    throw new InvalidOperationException(
                        SideName(side) + " Place teaching PickerY differs by picker. Conti line move test requires same PickerY. " +
                        "picker#" + targets[0].PickerNo + "=" + pickerY.ToString("F3") +
                        ", picker#" + targets[i].PickerNo + "=" + targets[i].PickerY.ToString("F3"));
                }
            }
        }

        private static async Task<int> PreparePlaceLineMoveStartAsync(
            CDT320_Machine machine,
            PickerSequenceSide side,
            PlaceLineMoveTarget[] targets,
            CancellationToken ct)
        {
            if (machine == null || machine.OutputStageUnit == null)
                return -1;
            if (side == PickerSequenceSide.Front && machine.PickerFrontUnit == null)
                return -1;
            if (side == PickerSequenceSide.Rear && machine.PickerRearUnit == null)
                return -1;

            PlaceLineMoveTarget first = targets[0];

            var zAvoidTargets = new Dictionary<PickerAxis, double>();
            for (int i = 0; i < targets.Length; i++)
                zAvoidTargets[targets[i].PickerZAxis] = targets[i].PickerZAvoid;

            string sideName = SideName(side);
            ct.ThrowIfCancellationRequested();
            int result = await MovePickerAxes(
                machine,
                side,
                zAvoidTargets,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;" + sideName + ";PlaceStart;PickerZone=Output;Step=SafeZ").ConfigureAwait(true);
            if (result != 0)
                return result;

            var yAvoidTarget = new Dictionary<PickerAxis, double>();
            yAvoidTarget[PickerAxis.PickerY] = GetPickerTeachingPosition(machine, side, PickerAxis.PickerY, "AvoidPosition");

            ct.ThrowIfCancellationRequested();
            result = await MovePickerAxes(
                machine,
                side,
                yAvoidTarget,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;" + sideName + ";PlaceStart;PickerZone=Output;Step=SafeY").ConfigureAwait(true);
            if (result != 0)
                return result;

            machine.OutputStageUnit.Recipe.EnsurePositionObjects();

            ct.ThrowIfCancellationRequested();
            result = await machine.OutputStageUnit.MoveStageAxis(
                BinStageAxis.GoodBinZ,
                machine.OutputStageUnit.Recipe.GoodStageZ.AvoidPosition,
                true,
                "LineMoveTest;" + sideName + ";GoodZAvoid").ConfigureAwait(true);
            if (result != 0)
                return result;

            ct.ThrowIfCancellationRequested();
            result = await machine.OutputStageUnit.MoveStageAxis(
                BinStageAxis.GoodBinY,
                first.StageY,
                true,
                "LineMoveTest;" + sideName + ";GoodYPlaceCenter").ConfigureAwait(true);
            if (result != 0)
                return result;

            var xAndTTargets = new Dictionary<PickerAxis, double>();
            xAndTTargets[PickerAxis.PickerX] = first.PickerX;
            for (int i = 0; i < targets.Length; i++)
                xAndTTargets[targets[i].PickerTAxis] = targets[i].PickerT;

            ct.ThrowIfCancellationRequested();
            result = await MovePickerAxes(
                machine,
                side,
                xAndTTargets,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;" + sideName + ";PlaceStart;PickerZone=Output;Step=XT").ConfigureAwait(true);
            if (result != 0)
                return result;

            var yPlaceTarget = new Dictionary<PickerAxis, double>();
            yPlaceTarget[PickerAxis.PickerY] = first.PickerY;

            ct.ThrowIfCancellationRequested();
            result = await MovePickerAxes(
                machine,
                side,
                yPlaceTarget,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;" + sideName + ";PlaceStart;PickerZone=Output;Step=PlaceY").ConfigureAwait(true);
            if (result != 0)
                return result;

            ct.ThrowIfCancellationRequested();
            result = await machine.OutputStageUnit.MoveNgStageToAvoidAndVerifyAsync(
                10000,
                true,
                ct).ConfigureAwait(true);
            if (result != 0)
                return result;

            if (!machine.OutputStageUnit.IsNgStageInAvoidPosition())
                return -11;

            ct.ThrowIfCancellationRequested();
            result = await machine.OutputStageUnit.MoveStageAxis(
                BinStageAxis.GoodBinZ,
                machine.OutputStageUnit.Recipe.GoodStageZ.ProcessPosition,
                true,
                "LineMoveTest;" + sideName + ";GoodZProcess").ConfigureAwait(true);
            if (result != 0)
                return result;

            var firstZTarget = new Dictionary<PickerAxis, double>();
            firstZTarget[first.PickerZAxis] = first.PickerZ;

            ct.ThrowIfCancellationRequested();
            return await MovePickerAxes(
                machine,
                side,
                firstZTarget,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;" + sideName + ";PlaceStart;PickerZone=Output;Step=PlaceZ;Picker" + first.PickerNo).ConfigureAwait(true);
        }

        private static LineMoveAxisSet ResolveGoodStagePickerLineMoveAxes(CDT320_Machine machine, PickerSequenceSide side, int previousPickerIndex, int currentPickerIndex)
        {
            var axes = new LineMoveAxisSet
            {
                StageY = machine.OutputStageUnit != null && machine.OutputStageUnit.GoodStage != null ? machine.OutputStageUnit.GoodStage.StageY : null,
                PickerX = ResolvePickerX(machine, side),
                PreviousPickerZ = ResolvePickerZAxis(machine, side, previousPickerIndex),
                PickerZ = ResolvePickerZAxis(machine, side, currentPickerIndex),
                PreviousPickerIndex = previousPickerIndex,
                PickerIndex = currentPickerIndex
            };

            ResolveAxisNo(axes.StageY, "OutputGoodStageY");
            ResolveAxisNo(axes.PickerX, SideName(side) + "PickerX");
            ResolveAxisNo(axes.PreviousPickerZ, SideName(side) + "PickerZ" + (previousPickerIndex + 1));
            ResolveAxisNo(axes.PickerZ, SideName(side) + "PickerZ" + (currentPickerIndex + 1));
            return axes;
        }

        private static BaseAxis ResolvePickerX(CDT320_Machine machine, PickerSequenceSide side)
        {
            if (machine == null)
                return null;
            return side == PickerSequenceSide.Front
                ? (machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null)
                : (machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerX : null);
        }

        private static BaseAxis ResolvePickerZAxis(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            if (machine == null)
                return null;

            if (side == PickerSequenceSide.Front)
            {
                if (machine.PickerFrontUnit == null)
                    return null;
                if (pickerIndex <= 0)
                    return machine.PickerFrontUnit.PickerZ0;
                if (pickerIndex == 1)
                    return machine.PickerFrontUnit.PickerZ1;
                if (pickerIndex == 2)
                    return machine.PickerFrontUnit.PickerZ2;
                return machine.PickerFrontUnit.PickerZ3;
            }

            if (machine.PickerRearUnit == null)
                return null;
            if (pickerIndex <= 0)
                return machine.PickerRearUnit.PickerZ0;
            if (pickerIndex == 1)
                return machine.PickerRearUnit.PickerZ1;
            if (pickerIndex == 2)
                return machine.PickerRearUnit.PickerZ2;
            return machine.PickerRearUnit.PickerZ3;
        }

        private static Task<int> MovePickerAxes(
            CDT320_Machine machine,
            PickerSequenceSide side,
            Dictionary<PickerAxis, double> targets,
            JogSpeedType speedType,
            double customSpeed,
            string targetName)
        {
            if (side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit.MovePickerAxes(targets, speedType, customSpeed, targetName);
            return machine.PickerRearUnit.MovePickerAxes(targets, speedType, customSpeed, targetName);
        }

        private static double GetPickerTeachingPosition(CDT320_Machine machine, PickerSequenceSide side, PickerAxis axis, string positionName)
        {
            if (side == PickerSequenceSide.Front)
                return machine.PickerFrontUnit.GetPickerTeachingPosition(axis, positionName);
            return machine.PickerRearUnit.GetPickerTeachingPosition(axis, positionName);
        }

        private static int ResolveAxisNo(BaseAxis axis, string axisName)
        {
            if (axis == null)
                throw new InvalidOperationException(axisName + " 축 객체를 찾을 수 없습니다.");

            if (axis.Setup == null)
                throw new InvalidOperationException(axisName + " 축 설정을 찾을 수 없습니다.");

            if (axis.Setup.AxisNo < 0)
                throw new InvalidOperationException(axisName + " 축 번호가 설정되지 않았습니다. axisNo=" + axis.Setup.AxisNo);

            return axis.Setup.AxisNo;
        }

        private static InterpolatedMotionMapResult MapFail(InterpolatedMotionMapResult result, int code, string message)
        {
            result.ResultCode = code == 0 ? -1 : code;
            result.Message = message;
            return result;
        }

        private static double ResolveNearAvoidPosition(double avoidPosition, double placePosition, double distanceFromAvoid)
        {
            if (distanceFromAvoid <= 0.0)
                return avoidPosition;

            double directionToPlace = placePosition >= avoidPosition ? 1.0 : -1.0;
            return avoidPosition + (directionToPlace * distanceFromAvoid);
        }

        private static double Lerp(double start, double target, double ratio)
        {
            return start + ((target - start) * ratio);
        }

        private static string SideName(PickerSequenceSide side)
        {
            return side == PickerSequenceSide.Front ? "Front" : "Rear";
        }

        private static string DescribeAxlResult(int result)
        {
            switch (result)
            {
                case 0: return "0(AXT_RT_SUCCESS)";
                case 1001: return "1001(AXT_RT_OPEN_ERROR)";
                case 1053: return "1053(AXT_RT_NOT_OPEN)";
                case 4051: return "4051(AXT_RT_MOTION_NOT_MODULE)";
                case 4101: return "4101(AXT_RT_MOTION_INVALID_AXIS_NO)";
                case 4537: return "4537(AXT_RT_MOTION_ERROR_INVALID_CONTIMAPAXIS)";
                case 4538: return "4538(AXT_RT_MOTION_ERROR_INVALID_CONTIMAPSIZE)";
                default: return result.ToString();
            }
        }

        private sealed class PlaceLineMoveTarget
        {
            public int PickerIndex { get; set; }
            public int PickerNo { get; set; }
            public PickerAxis PickerTAxis { get; set; }
            public PickerAxis PickerZAxis { get; set; }
            public double StageY { get; set; }
            public double PickerX { get; set; }
            public double PickerY { get; set; }
            public double PickerT { get; set; }
            public double PickerZ { get; set; }
            public double PickerZAvoid { get; set; }
        }

        private sealed class LineMoveAxisSet
        {
            public BaseAxis StageY { get; set; }
            public BaseAxis PickerX { get; set; }
            public BaseAxis PreviousPickerZ { get; set; }
            public BaseAxis PickerZ { get; set; }
            public int PreviousPickerIndex { get; set; }
            public int PickerIndex { get; set; }
        }
    }

    internal sealed class PickerContiLineMapTestResult
    {
        public PickerContiLineMapTestResult(string name, InterpolatedMotionMapResult result)
        {
            Name = name;
            Result = result;
        }

        public string Name { get; private set; }
        public InterpolatedMotionMapResult Result { get; private set; }
    }

    internal sealed class PickerContiLineMoveRunResult
    {
        private PickerContiLineMoveRunResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        public bool Success { get; private set; }
        public string Message { get; private set; }

        public static PickerContiLineMoveRunResult Ok(string message)
        {
            return new PickerContiLineMoveRunResult(true, message);
        }

        public static PickerContiLineMoveRunResult Fail(string message)
        {
            return new PickerContiLineMoveRunResult(false, message);
        }
    }
}
