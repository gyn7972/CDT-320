using QMC.CDT320.Bin;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Motion;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>장비 전체 모션을 안전한 Ready(Avoid) 위치로 복귀시키는 시퀀스입니다.</summary>
    internal sealed class MachineReadySequence
    {
        private readonly CDT320_Machine _machine;

        public MachineReadySequence(CDT320_Machine machine)
        {
            _machine = machine;
        }

        public string LastErrorMessage { get; private set; }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                LastErrorMessage = string.Empty;
                LogStep("Ready 시퀀스 시작.");

                int result = 0;

                //result = await MoveOutputStageVisonXAvoidAsync(ct).ConfigureAwait(false);
                //if (result != 0)
                //    return result;

                result = await MoveOutputStageAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveSideVisionAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                

                result = await MoveInputFeederAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputFeederAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputCassetteAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputCassetteAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                LogStep("Ready 시퀀스 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                LastErrorMessage = "Ready 시퀀스가 정지되었습니다.";
                LogStep(LastErrorMessage);
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-EX", "MachineReadySequence", "Ready 시퀀스 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontPickerAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.PickerFrontUnit : null;
                if (unit == null)
                    return Skip("FrontPickerUnit");

                LogStep("FrontPicker Avoid 이동 시작.");
                int result = await unit.MoveToFrontPickerAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-FRONT-PICKER", "PickerFrontUnit", "FrontPicker Avoid 이동 실패. result=" + result);

                if (!unit.IsFrontPickerInAvoidPosition())
                    return Fail("READY-FRONT-PICKER-CHECK", "PickerFrontUnit", "FrontPicker Avoid 위치 최종 확인 실패.");

                LogStep("FrontPicker Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-FRONT-PICKER-EX", "PickerFrontUnit", "FrontPicker Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveRearPickerAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.PickerRearUnit : null;
                if (unit == null)
                    return Skip("RearPickerUnit");

                LogStep("RearPicker Avoid 이동 시작.");
                int result = await unit.MoveToRearPickerAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-REAR-PICKER", "PickerRearUnit", "RearPicker Avoid 이동 실패. result=" + result);

                if (!unit.IsRearPickerInAvoidPosition())
                    return Fail("READY-REAR-PICKER-CHECK", "PickerRearUnit", "RearPicker Avoid 위치 최종 확인 실패.");

                LogStep("RearPicker Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-REAR-PICKER-EX", "PickerRearUnit", "RearPicker Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveSideVisionAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.VisionUnit : null;
                if (unit == null)
                    return Skip("VisionUnit");

                LogStep("Side Vision Avoid 이동 시작.");
                int result = await unit.MoveToVisionAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-SIDE-VISION", "VisionUnit", "Side Vision Avoid 이동 실패. result=" + result);

                if (!unit.IsVisionInAvoidPosition())
                    return Fail("READY-SIDE-VISION-CHECK", "VisionUnit", "Side Vision Avoid 위치 최종 확인 실패.");

                LogStep("Side Vision Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-SIDE-VISION-EX", "VisionUnit", "Side Vision Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.InputStageUnit : null;
                if (unit == null)
                    return Skip("InputStageUnit");

                LogStep("InputStage Avoid 이동 시작.");

                int result = await MoveInputStageNeedleZAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageEjectPinZAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageNeedleXAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageExpanderZAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageVisionXAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageWaferYAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageWaferTAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                LogStep("InputStage Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-INPUT-STAGE-EX", "InputStageUnit", "InputStage Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        //private async Task<int> MoveOutputStageVisonXAvoidAsync(CancellationToken ct)
        //{
        //    try
        //    {
        //        ct.ThrowIfCancellationRequested();
        //        var unit = _machine != null ? _machine.OutputStageUnit : null;
        //        if (unit == null)
        //            return Skip("OutputStageUnit");

        //        LogStep("OutputStage Avoid 이동 시작.");
        //        int result = await unit.MoveToStageAvoidPosition(true).ConfigureAwait(false);
        //        if (result != 0)
        //            return Fail("READY-OUTPUT-STAGE", "OutputStageUnit", "OutputStage Avoid 이동 실패. result=" + result);

        //        if (!IsOutputStageAvoidPosition(unit))
        //            return Fail("READY-OUTPUT-STAGE-CHECK", "OutputStageUnit", "OutputStage Avoid 위치 최종 확인 실패.");

        //        LogStep("OutputStage Avoid 이동 완료.");
        //        return 0;
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        return Fail("READY-OUTPUT-STAGE-EX", "OutputStageUnit", "OutputStage Avoid 이동 예외: " + ex.Message);
        //    }
        //    finally
        //    {
        //    }
        //}

        private async Task<int> MoveOutputStageAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.OutputStageUnit : null;
                if (unit == null)
                    return Skip("OutputStageUnit");

                LogStep("OutputStage Avoid 이동 시작.");
                //int result = await unit.MoveToStageAvoidPosition(true).ConfigureAwait(false);
                int result = await unit.MoveStageAxis(BinStageAxis.VisionX, unit.Recipe.VisionX.AvoidPosition);
                if (result != 0)
                    return Fail("READY-OUTPUT-STAGE_VISIONX", "OutputStageUnit", "OutputStage VisionX Avoid 이동 실패. result=" + result);

                if (!IsOutputStageAvoidPosition(unit))
                    return Fail("READY-OUTPUT-STAGE-CHECK", "OutputStageUnit", "OutputStage VisionX Avoid 위치 최종 확인 실패.");

                LogStep("OutputStage VisionX Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-OUTPUT-STAGE-EX", "OutputStageUnit", "OutputStage Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private Task<int> MoveInputStageNeedleZAvoidAsync(InputStageUnit unit, CancellationToken ct)
        {
            return MoveInputStageAxisAvoidAsync(
                unit,
                WaferStageAxis.NeedleZ,
                "NeedleZ",
                "READY-INPUT-STAGE-NEEDLE-Z",
                ct);
        }

        private Task<int> MoveInputStageEjectPinZAvoidAsync(InputStageUnit unit, CancellationToken ct)
        {
            return MoveInputStageAxisAvoidAsync(
                unit,
                WaferStageAxis.EjectPinZ,
                "EjectPinZ",
                "READY-INPUT-STAGE-EJECT-Z",
                ct);
        }

        private Task<int> MoveInputStageNeedleXAvoidAsync(InputStageUnit unit, CancellationToken ct)
        {
            return MoveInputStageAxisAvoidAsync(
                unit,
                WaferStageAxis.NeedleX,
                "NeedleX",
                "READY-INPUT-STAGE-NEEDLE-X",
                ct);
        }

        private Task<int> MoveInputStageExpanderZAvoidAsync(InputStageUnit unit, CancellationToken ct)
        {
            return MoveInputStageAxisAvoidAsync(
                unit,
                WaferStageAxis.WaferExpandingZ,
                "ExpanderZ",
                "READY-INPUT-STAGE-EXPANDER-Z",
                ct);
        }

        private Task<int> MoveInputStageVisionXAvoidAsync(InputStageUnit unit, CancellationToken ct)
        {
            return MoveInputStageAxisAvoidAsync(
                unit,
                WaferStageAxis.VisionX,
                "VisionX",
                "READY-INPUT-STAGE-VISION-X",
                ct);
        }

        private Task<int> MoveInputStageWaferYAvoidAsync(InputStageUnit unit, CancellationToken ct)
        {
            return MoveInputStageAxisAvoidAsync(
                unit,
                WaferStageAxis.WaferY,
                "StageY",
                "READY-INPUT-STAGE-Y",
                ct);
        }

        private Task<int> MoveInputStageWaferTAvoidAsync(InputStageUnit unit, CancellationToken ct)
        {
            return MoveInputStageAxisAvoidAsync(
                unit,
                WaferStageAxis.WaferT,
                "StageT",
                "READY-INPUT-STAGE-T",
                ct);
        }

        private async Task<int> MoveInputStageAxisAvoidAsync(
            InputStageUnit unit,
            WaferStageAxis axis,
            string label,
            string alarmCode,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("InputStageUnit");

                BaseAxis baseAxis = ResolveInputStageAxis(unit, axis);
                if (baseAxis == null)
                    return Fail(alarmCode + "-AXIS", "InputStageUnit", label + " 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                StageAxisPositions positions = ResolveInputStageAxisPositions(unit, axis);
                if (positions == null)
                    return Fail(alarmCode + "-RECIPE", "InputStageUnit", label + " Avoid 레시피 위치를 찾을 수 없습니다.");

                double target = positions.AvoidPosition;
                LogStep("InputStage " + label + " Avoid 이동 시작. target=" + target);

                int result = await unit.MoveInputStageAxis(axis, target, true).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        alarmCode,
                        "InputStageUnit",
                        "InputStage " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target) +
                        BuildInputStageFailure(unit));
                }

                AxisMoveWaitResult waitResult = await unit.WaitInputStageAxisInPositionResult(
                    axis,
                    target,
                    ResolveReadyMoveTimeoutMs(unit),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode(alarmCode, waitResult),
                        "InputStageUnit",
                        "InputStage " + label + " Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState(label, baseAxis, target)) +
                        BuildInputStageFailure(unit));
                }

                if (!IsAxisInPosition(baseAxis, target))
                {
                    return Fail(
                        alarmCode + "-CHECK",
                        "InputStageUnit",
                        "InputStage " + label + " Avoid 위치 최종 확인 실패. " +
                        BuildAxisState(label, baseAxis, target) +
                        BuildInputStageFailure(unit));
                }

                LogStep("InputStage " + label + " Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(alarmCode + "-EX", "InputStageUnit", "InputStage " + label + " Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputFeederAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.InputFeederUnit : null;
                if (unit == null)
                    return Skip("InputFeederUnit");

                LogStep("InputFeeder Avoid 이동 시작.");
                int result = await unit.MoveToWaferFeederAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-INPUT-FEEDER", "InputFeederUnit", "InputFeeder Avoid 이동 실패. result=" + result);

                if (!unit.IsWaferFeederInAvoidPosition())
                    return Fail("READY-INPUT-FEEDER-CHECK", "InputFeederUnit", "InputFeeder Avoid 위치 최종 확인 실패.");

                LogStep("InputFeeder Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-INPUT-FEEDER-EX", "InputFeederUnit", "InputFeeder Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputFeederAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.OutputFeederUnit : null;
                if (unit == null)
                    return Skip("OutputFeederUnit");

                LogStep("OutputFeeder Avoid 이동 시작.");
                int result = await unit.MoveToFeederAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-OUTPUT-FEEDER", "OutputFeederUnit", "OutputFeeder Avoid 이동 실패. result=" + result);

                if (!unit.IsBinFeederInAvoidPosition())
                    return Fail("READY-OUTPUT-FEEDER-CHECK", "OutputFeederUnit", "OutputFeeder Avoid 위치 최종 확인 실패.");

                LogStep("OutputFeeder Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-OUTPUT-FEEDER-EX", "OutputFeederUnit", "OutputFeeder Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputCassetteAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.InputCassetteUnit : null;
                if (unit == null)
                    return Skip("InputCassetteUnit");

                LogStep("InputCassette Avoid 이동 시작.");
                int result = await unit.MoveToWaferCassetteAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-INPUT-CASSETTE", "InputCassetteUnit", "InputCassette Avoid 이동 실패. result=" + result);

                if (!unit.IsWaferLifterZInAvoidPosition())
                    return Fail("READY-INPUT-CASSETTE-CHECK", "InputCassetteUnit", "InputCassette Avoid 위치 최종 확인 실패.");

                LogStep("InputCassette Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-INPUT-CASSETTE-EX", "InputCassetteUnit", "InputCassette Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputCassetteAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.OutputCassetteUnit : null;
                if (unit == null)
                    return Skip("OutputCassetteUnit");

                LogStep("OutputCassette Avoid 이동 시작.");
                int result = await unit.MoveToBinCassetteAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-OUTPUT-CASSETTE", "OutputCassetteUnit", "OutputCassette Avoid 이동 실패. result=" + result);

                if (!unit.IsBinLifterZInAvoidPosition())
                    return Fail("READY-OUTPUT-CASSETTE-CHECK", "OutputCassetteUnit", "OutputCassette Avoid 위치 최종 확인 실패.");

                LogStep("OutputCassette Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-OUTPUT-CASSETTE-EX", "OutputCassetteUnit", "OutputCassette Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private static BaseAxis ResolveInputStageAxis(InputStageUnit unit, WaferStageAxis axis)
        {
            try
            {
                if (unit == null)
                    return null;

                switch (axis)
                {
                    case WaferStageAxis.WaferY:
                        return unit.StageY;
                    case WaferStageAxis.WaferT:
                        return unit.StageT;
                    case WaferStageAxis.WaferExpandingZ:
                        return unit.ExpanderZ;
                    case WaferStageAxis.VisionX:
                        return unit.CameraX;
                    case WaferStageAxis.NeedleX:
                        return unit.NeedleBlockX;
                    case WaferStageAxis.NeedleZ:
                        return unit.NeedleZ;
                    case WaferStageAxis.EjectPinZ:
                        return unit.EjectPinZ;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "InputStage 축 해석 실패. axis=" + axis + ", error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static StageAxisPositions ResolveInputStageAxisPositions(InputStageUnit unit, WaferStageAxis axis)
        {
            try
            {
                if (unit == null || unit.Recipe == null)
                    return null;

                switch (axis)
                {
                    case WaferStageAxis.WaferY:
                        return unit.Recipe.WaferY;
                    case WaferStageAxis.WaferT:
                        return unit.Recipe.WaferT;
                    case WaferStageAxis.WaferExpandingZ:
                        return unit.Recipe.WaferZ;
                    case WaferStageAxis.VisionX:
                        return unit.Recipe.VisionX;
                    case WaferStageAxis.NeedleX:
                        return unit.Recipe.NeedleX;
                    case WaferStageAxis.NeedleZ:
                        return unit.Recipe.NeedleZ;
                    case WaferStageAxis.EjectPinZ:
                        return unit.Recipe.EjectPinZ;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "InputStage Avoid 레시피 해석 실패. axis=" + axis + ", error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static int ResolveReadyMoveTimeoutMs(InputStageUnit unit)
        {
            try
            {
                if (unit != null && unit.Config != null && unit.Config.SequenceMoveTimeoutMs > 0)
                    return unit.Config.SequenceMoveTimeoutMs;

                return 10000;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "Ready 이동 Timeout 해석 실패. error=" + ex.Message + " - Failed");
                return 10000;
            }
            finally
            {
            }
        }

        private static bool IsAxisInPosition(BaseAxis axis, double target)
        {
            try
            {
                if (axis == null)
                    return false;

                double tolerance = ResolveAxisTolerance(axis);
                return !axis.IsAlarm &&
                       !axis.IsMoving &&
                       Math.Abs(axis.ActualPosition - target) <= tolerance &&
                       Math.Abs(axis.CommandPosition - target) <= tolerance;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "축 위치 확인 실패. axis=" + (axis != null ? axis.Name : "-") +
                    ", target=" + target + ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static double ResolveAxisTolerance(BaseAxis axis)
        {
            try
            {
                if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                    return axis.Config.InPositionTolerance;

                return 0.05;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "축 InPosition 허용오차 해석 실패. axis=" + (axis != null ? axis.Name : "-") +
                    ", error=" + ex.Message + " - Failed");
                return 0.05;
            }
            finally
            {
            }
        }

        private static string BuildAxisState(string label, BaseAxis axis, double target)
        {
            try
            {
                if (axis == null)
                    return label + "=null, target=" + target;

                double tolerance = ResolveAxisTolerance(axis);
                return label +
                    "[name=" + axis.Name +
                    ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                    ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                    ", moving=" + (axis.IsMoving ? "Y" : "N") +
                    ", inPosition=" + (axis.IsInPosition ? "ON" : "OFF") +
                    ", actual=" + axis.ActualPosition +
                    ", command=" + axis.CommandPosition +
                    ", target=" + target +
                    ", tolerance=" + tolerance +
                    "]";
            }
            catch (Exception ex)
            {
                return label + " state build failed. target=" + target + ", error=" + ex.Message;
            }
            finally
            {
            }
        }

        private static string BuildInputStageFailure(InputStageUnit unit)
        {
            try
            {
                if (unit == null || string.IsNullOrWhiteSpace(unit.LastStageMoveFailureMessage))
                    return string.Empty;

                return ", lastStageMoveFailure=" + unit.LastStageMoveFailureMessage;
            }
            catch (Exception ex)
            {
                return ", lastStageMoveFailure read failed. error=" + ex.Message;
            }
            finally
            {
            }
        }

        private static bool IsOutputStageAvoidPosition(OutputStageUnit unit)
        {
            try
            {
                if (unit == null)
                    return true;

                if (!unit.IsVisionXInAvoidPosition())
                    return false;

                if (!unit.IsGoodStageZInAvoidPosition())
                    return false;

                if (!unit.IsNgStageInAvoidPosition())
                    return false;

                if (unit.GoodStage != null &&
                    unit.GoodStage.StageY != null &&
                    unit.Recipe != null &&
                    unit.Recipe.GoodStageY != null)
                {
                    double tolerance = unit.GoodStage.StageY.Config != null
                        ? unit.GoodStage.StageY.Config.InPositionTolerance
                        : 0.05;

                    if (!unit.IsStageAxisInPosition(BinStageAxis.GoodBinY, unit.Recipe.GoodStageY.AvoidPosition, tolerance))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private int Skip(string source)
        {
            LogStep(source + "이 없어 Ready 이동을 건너뜁니다.");
            return 0;
        }

        private int Fail(string code, string source, string message)
        {
            LastErrorMessage = message;
            Log.Write("Main", "SYSTEM", "MachineReadySequence", message + " - Failed");
            AlarmManager.Raise(AlarmSeverity.Error, code, source, message);
            return -1;
        }

        private static void LogStep(string message)
        {
            Log.Write("Main", "SYSTEM", "MachineReadySequence", message + " - Ok");
        }
    }
}
