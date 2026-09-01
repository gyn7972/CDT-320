using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal sealed partial class PickerPickUpSequence
    {
        private async Task<int> MovePickerAxisWithMotionAndVerifyAsync(
            PickerAxis axis,
            double target,
            double velocity,
            double acceleration,
            double deceleration,
            string description,
            string targetName,
            CancellationToken ct,
            bool deferFinalPositionCheck = false)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int commandResult = await MovePickerAxisCommandWithMotionAsync(axis, target, velocity, acceleration, deceleration, targetName).ConfigureAwait(false);
                if (commandResult != 0)
                    return Fail("PICKER-PICKUP-MOVE-CMD", Name,
                        description + " 이동 명령 실패. result=" + commandResult +
                        ", velocity=" + velocity +
                        ", acc=" + acceleration +
                        ", dec=" + deceleration +
                        ", " + BuildPickerAxisState(axis, target));

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("PICKUP", TactRequestId(), axis.ToString());
                int waitResult = await WaitPickerAxisInPositionResultAsync(axis, target, description, ct).ConfigureAwait(false);
                if (waitResult != 0)
                    return waitResult;

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("PICKUP", TactRequestId(), axis.ToString());
                if (deferFinalPositionCheck)
                    return 0;

                return CheckPickerAxisInPosition(axis, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOVE-VEL-EX", Name,
                    description + " 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageAxisWithMotionAndVerifyAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            double velocity,
            double acceleration,
            double deceleration,
            string description,
            CancellationToken ct,
            string guardTargetName = null,
            bool deferFinalPositionCheck = false)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int commandResult;
                BaseAxis item = ResolveInputStageAxis(stage, axis);
                if (item != null && !string.IsNullOrWhiteSpace(guardTargetName))
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(item, target, guardTargetName))
                    {
                        commandResult = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration),
                            ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    commandResult = await AwaitStepWithCancellationAsync(
                        stage.MoveInputStageAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration),
                        ct).ConfigureAwait(false);
                }

                if (commandResult != 0)
                {
                    return Fail("PICKER-PICKUP-STAGE-MOVE", stage.Name,
                        description + " 이동 명령 실패. result=" + commandResult +
                        ", velocity=" + velocity +
                        ", acc=" + acceleration +
                        ", dec=" + deceleration +
                        ", " + BuildInputStageAxisState(stage, axis, target) +
                        PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));
                }

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("PICKUP", TactRequestId(), axis.ToString());
                int waitResult = await WaitInputStageAxisInPositionResultAsync(stage, axis, target, description, ct).ConfigureAwait(false);
                if (waitResult != 0)
                    return waitResult;

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("PICKUP", TactRequestId(), axis.ToString());
                if (deferFinalPositionCheck)
                    return 0;

                return CheckInputStageAxisInPosition(stage, axis, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-MOVE-VEL-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private double ResolvePickerAxisVelocityByPercent(PickerAxis axis, double percent)
        {
            QMC.Common.Motion.BaseAxis item = GetPickerAxis(axis);
            return ResolveAxisVelocityByPercent(item, percent);
        }

        private double ResolvePickerAxisAccelerationByPercent(PickerAxis axis, double percent, bool acceleration)
        {
            QMC.Common.Motion.BaseAxis item = GetPickerAxis(axis);
            return ResolveAxisAccelerationByPercent(item, percent, acceleration);
        }

        private static double ResolveInputStageAxisVelocityByPercent(InputStageUnit stage, WaferStageAxis axis, double percent)
        {
            QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
            return ResolveAxisVelocityByPercent(item, percent);
        }

        private static double ResolveInputStageAxisAccelerationByPercent(InputStageUnit stage, WaferStageAxis axis, double percent, bool acceleration)
        {
            QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
            return ResolveAxisAccelerationByPercent(item, percent, acceleration);
        }

        // 기존 조건: 퍼센트 경로는 전역 MotionSpeedScale을 곱하지 않아 저속 테스트(5%)에서도
        //   접촉/분리 구간이 풀속도로 나갔다(과속 위험) — 반대로 이송은 스케일이 걸려 체감 불일치.
        // 현재 기준(사용자 확정 속도 모델 2026-07-26): 모든 퍼센트 경로 =
        //   축 DefaultVelocity × 전역 스케일 × percent. (명시 발행 시 유닛 레이어가 기본속도
        //   추론을 차단해 가감속 이중 스케일을 방지한다.)
        private static double ResolveAxisVelocityByPercent(QMC.Common.Motion.BaseAxis axis, double percent)
        {
            double normalizedPercent = PickerPickUpMotionConfig.NormalizePercent(percent, 1.0);
            double baseVelocity = 1.0;
            if (axis != null && axis.Config != null && axis.Config.GetRawDefaultVelocity() > 0.0)
                baseVelocity = axis.Config.GetRawDefaultVelocity();

            return Math.Max(0.001, QMC.Common.Motion.MotionSpeedScale.ApplyDefaultVelocityScale(baseVelocity) * normalizedPercent / 100.0);
        }

        private static double ResolveAxisAccelerationByPercent(QMC.Common.Motion.BaseAxis axis, double percent, bool acceleration)
        {
            double normalizedPercent = PickerPickUpMotionConfig.NormalizePercent(percent, 1.0);
            double baseAcceleration = 1.0;
            if (axis != null && axis.Config != null)
            {
                double configured = acceleration ? axis.Config.GetRawAcceleration() : axis.Config.GetRawDeceleration();
                if (configured > 0.0)
                    baseAcceleration = configured;
            }

            return Math.Max(0.001, QMC.Common.Motion.MotionSpeedScale.ApplyDefaultAccelerationScale(baseAcceleration) * normalizedPercent / 100.0);
        }

        private static double ResolveTargetToward(double fromTarget, double towardTarget, double distance)
        {
            if (distance <= 0.0)
                return fromTarget;

            double delta = towardTarget - fromTarget;
            if (Math.Abs(delta) <= distance)
                return towardTarget;

            return fromTarget + Math.Sign(delta) * distance;
        }

        private async Task<int> MovePickerNeedleEjectZToPickAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                Task<int> pickerZMove = MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    _targetPickerZ,
                    "pick Z down",
                    ct,
                    BuildPickerTargetName("DiePickPosition", _currentPickerIndex));
                Task<int> needleZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    _targetNeedleZ,
                    "픽업 NeedleZ 상승",
                    ct);
                Task<int> ejectPinZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    _targetEjectPinZ,
                    "픽업 EjectPinZ 상승",
                    ct);

                int[] results = await Task.WhenAll(pickerZMove, needleZMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0 || results[2] != 0)
                {
                    return Fail("PICKER-PICKUP-Z-SYNC", Name,
                        "PickUp Z 동기 이동 실패. " +
                        "pickerZResult=" + results[0] +
                        ", needleZResult=" + results[1] +
                        ", ejectPinZResult=" + results[2] +
                        ", " + BuildPickerAxisState(pickerZ, _targetPickerZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ));
                }

                int check = CheckPickerAxisInPosition(pickerZ, _targetPickerZ, "pick Z down");
                if (check != 0)
                    return check;

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ, "픽업 NeedleZ 상승");
                if (check != 0)
                    return check;

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "픽업 EjectPinZ 상승");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SYNC-EX", Name,
                    "PickUp Z 동기 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerNeedleEjectZToAvoidAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            string description,
            CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                double needleZAvoid = ResolveNeedleZAvoidTarget(stage);
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
                // 사용자 확정 속도 모델(2026-07-26): Avoid 복귀 = DefaultVelocity × 전역 스케일(% 미적용).
                double pickerAvoidSpeedPercent = 100.0;
                double pickerAvoidVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, pickerAvoidSpeedPercent);
                double pickerAvoidAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, true);
                double pickerAvoidDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, false);
                double pickerSafeForWaferStageDistance = config != null
                    ? PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(config.PickerSafeForWaferStageDistance)
                    : PickerPickUpMotionConfig.MinimumPickerSafeForWaferStageDistance;
                int needleVacuumOffResult = EnsureNeedleVacuumOffForPick(stage, description + " 이동 전");
                if (needleVacuumOffResult != 0)
                    return needleVacuumOffResult;

                Task<int> pickerZMove = MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
                    pickerZ,
                    pickerZAvoid,
                    _targetPickerZ,
                    pickerSafeForWaferStageDistance,
                    pickerAvoidVelocity,
                    pickerAvoidAcceleration,
                    pickerAvoidDeceleration,
                    "pick Z avoid after pickup",
                    "AvoidPosition",
                    ct);
                Task<int> needleZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleZAvoid,
                    "픽업 후 NeedleZ 안전 위치 복귀",
                    ct);
                Task<int> ejectPinZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    "픽업 후 EjectPinZ 안전 위치 복귀",
                    ct);

                int[] results = await Task.WhenAll(pickerZMove, needleZMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0 || results[2] != 0)
                {
                    return Fail("PICKER-PICKUP-Z-AVOID-SYNC", Name,
                        description + " 실패. " +
                        "pickerZResult=" + results[0] +
                        ", needleZResult=" + results[1] +
                        ", ejectPinZResult=" + results[2] +
                        ", " + BuildPickerAxisState(pickerZ, pickerZAvoid) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleZAvoid) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-AVOID-SYNC-EX", Name,
                    description + " 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> TryMovePickerNeedleAndEjectPinZToAvoidAsync(string description, CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return -1;

                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                double pickerZAvoid = GetPickerTeachingPosition(pickerZ, "AvoidPosition");
                double needleZAvoid = ResolveNeedleZAvoidTarget(stage);
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                TryNeedleVacuumOffForPick(stage, description + " 이동 전");

                Task<int> pickerZMove = MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    pickerZAvoid,
                    description + " PickerZ",
                    ct,
                    "AvoidPosition");
                Task<int> needleZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleZAvoid,
                    description + " NeedleZ",
                    ct);
                Task<int> ejectPinZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    description + " EjectPinZ",
                    ct);

                int[] results = await Task.WhenAll(pickerZMove, needleZMove, ejectPinZMove).ConfigureAwait(false);
                WriteLog("PickerPickUpSequence",
                    Name + " " + description +
                    ". pickerZResult=" + results[0] +
                    ", needleZResult=" + results[1] +
                    ", ejectPinZResult=" + results[2] + " - Check");
                return results[0] == 0 && results[1] == 0 && results[2] == 0 ? 0 : -1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " " + description + " 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> VacuumOnAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                int needleVacuumResult = EnsureNeedleVacuumOnForPick(stage, "PickUp Vacuum ON");
                if (needleVacuumResult != 0)
                    return needleVacuumResult;

                SetPickerVacuum(_currentPickerNo, true);
                await Task.Delay(ResolveVacuumSettleMs(), ct).ConfigureAwait(false);

                int flowResult = await VerifyPickerFlowStateAsync(
                    _currentPickerNo,
                    true,
                    "PickUp Vacuum ON 후 흡착 Flow 확인",
                    ct).ConfigureAwait(false);
                if (flowResult != 0)
                    return flowResult;

                CurrentStep = PickerPickUpStep.VerifyDiePicked;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VACUUM-EX", Name, "Picker vacuum on failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyDiePicked()
        {
            _diePicked = true;
            SaveCurrentStateToBatchItem();

            MaterialStateService.UpsertInspection(_currentDieId, new DieInspectionRecord
            {
                InspectionType = "PickUp",
                Result = MaterialInspectionResult.Ok,
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot(
                        "PickUp",
                        _currentPickerIndex,
                        _targetPickerX,
                        _targetStageY,
                        _targetPickerT,
                        _targetPickerZ,
                        _visionOffset != null
                            ? new VisionOffset
                            {
                                X = _visionOffset.DeltaX,
                                Y = _visionOffset.DeltaY,
                                R = _visionOffset.DeltaTheta,
                                IsValid = true
                            }
                            : new VisionOffset())
                },
                Measurements = new List<InspectionMeasurement>
                {
                    BuildBooleanMeasurement("VacuumOn", true),
                    BuildBooleanMeasurement("FlowCheckOn", true),
                    BuildMeasurement("PickerNo", _currentPickerNo, "no", MaterialInspectionResult.Ok),
                    BuildMeasurement("NeedleX", _targetNeedleX, "mm", MaterialInspectionResult.Ok),
                    BuildMeasurement("NeedleZ", _targetNeedleZ, "mm", MaterialInspectionResult.Ok),
                    BuildMeasurement("EjectPinZ", _targetEjectPinZ, "mm", MaterialInspectionResult.Ok)
                }
            });

            CurrentStep = PickerPickUpStep.MovePickerZToAvoid;
            return 0;
        }

        private async Task<int> MovePickerZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerNeedleEjectZToAvoidAsync(
                zAxis,
                avoid,
                "PickUp 완료 후 PickerZ/NeedleZ/EjectPinZ 안전 복귀",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            _currentPickSafeReturnCompleted = true;
            CurrentStep = PickerPickUpStep.UpdateMaterialToPicker;
            return 0;
        }

        private int UpdateMaterialToPicker()
        {
            string pickedDieId = _currentDieId;
            int pickedPickerNo = _currentPickerNo;
            int pickedPickerIndex = _currentPickerIndex;

            bool materialUpdated = MaterialStateService.MarkDiePickedByPicker(_currentDieId, PickerLocationKind, _currentPickerNo);
            if (!materialUpdated)
                return Fail("PICKER-PICKUP-MATERIAL", Name, "Picked die material state update failed. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo);

            int verifyResult = VerifyPickerHasDieDataAndFlowAfterPick(pickedDieId, pickedPickerNo, pickedPickerIndex);
            if (verifyResult != 0)
                return verifyResult;

            RecordColletUse(_currentPickerNo);
            RecordBottomAutoFocusPickCount(_currentPickerNo, MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo));
            // [사용자 지시 2026-07-27] die당 동기 디스크 저장(~10ms) → 비동기 전환(핫패스 제거).
            SaveRuntimeStateAsync(Name + ":PickUp:ColletUse:" + _currentPickerNo);
            WriteLog("PickerPickUpSequence", Name + " picked die. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo + " - Ok");

            int completionResult = PublishInputStageCompletionAfterSafePickReturn();
            if (completionResult != 0)
                return completionResult;

            // CycleTime 정상 종결 — 컨텍스트(dieId) 클리어 전에 기록해야 키가 일치한다.
            QMC.CDT320.Diagnostics.HandlerTactLog.CycleResult("PICKUP", TactRequestId());

            if (_currentBatchItem != null)
                _currentBatchItem.DiePicked = true;

            _currentDieId = "";
            _pickTarget = null;
            _diePicked = false;
            CurrentStep = PickerPickUpStep.SelectNextPickTargetOrComplete;
            return 0;
        }

        private int PublishInputStageCompletionAfterSafePickReturn()
        {
            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                return 0;

            if (!MaterialStateService.IsInputStagePickComplete())
                return 0;

            if (!_currentPickSafeReturnCompleted)
            {
                return Fail("PICKER-PICKUP-STAGE-COMPLETE-UNSAFE", Name,
                    "InputStage 마지막 Pick 완료 신호를 발행할 수 없습니다. " +
                    "PickerZ/NeedleZ/EjectPinZ 안전 복귀가 완료되지 않았습니다. " +
                    "side=" + Side + ", pickerNo=" + _currentPickerNo);
            }

            if (Context == null || Context.Bus == null)
            {
                return Fail("PICKER-PICKUP-STAGE-COMPLETE-BUS", Name,
                    "InputStage 마지막 Pick 안전 복귀 완료 신호를 발행할 Bus가 없습니다. " +
                    "side=" + Side + ", pickerNo=" + _currentPickerNo);
            }

            Context.Bus.Set("InputStageDieComplete");
            WriteLog("PickerPickUpSequence",
                Name + " InputStage 마지막 Pick 안전 복귀 완료 후 완료 신호를 발행했습니다. " +
                "signal=InputStageDieComplete, side=" + Side +
                ", pickerNo=" + _currentPickerNo + " - Ok");
            return 0;
        }

        private int VerifyPickerHasDieDataAndFlowAfterPick(string dieId, int pickerNo, int pickerIndex)
        {
            try
            {
                DieMaterial dieOnPicker = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                if (dieOnPicker == null || !string.Equals(dieOnPicker.DieId, dieId, StringComparison.OrdinalIgnoreCase))
                {
                    return Fail("PICKER-PICKUP-MATERIAL-FLOW-MISMATCH", Name,
                        "PickUp 완료 후 제품 보유 데이터 확인 실패. " +
                        "Flow 확인 전에 Material 데이터가 Picker 위치와 일치해야 합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", expectedDie=" + dieId +
                        ", actualDie=" + (dieOnPicker != null ? dieOnPicker.DieId : "null"));
                }

                if (IsPickUpProductPrecheckBypassed())
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 완료 후 제품 보유 Flow/Data 확인은 Simulation/DryRun 조건으로 Flow 확인을 통과합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", die=" + dieId +
                        ", data=OK - Bypass");
                    return 0;
                }

                // SAFETY WARNING:
                // - 아래 flowOn=true는 현행 Test override이므로 이 블록과 완료 로그를 실센서 흡착 확인으로 해석하지 않는다.
                // - 실제 Flow 확인은 PickUp Z 경로와 다음 검사 진입 precheck에 별도로 존재한다.
                // - override 제거·변경은 이번 가독성 정리 범위가 아니며 실장비 검증과 별도 승인이 필요하다.
                bool flowOn = ReadPickerFlowState(pickerNo);
                //Todo : Test Flow code
                flowOn = true;
                if (!flowOn)
                {
                    return Fail("PICKER-PICKUP-COMPLETE-FLOW-NOT-DETECTED", Name,
                        "PickUp 완료 후 제품 보유 Flow/Data 확인 실패. " +
                        "Material 데이터는 Picker에 있지만 실제 Flow 신호가 ON이 아닙니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", die=" + dieId +
                        ", expectedFlow=ON, actualFlow=OFF");
                }

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp 완료 후 제품 보유 Flow/Data 확인 완료. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", data=OK, flow=ON - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-COMPLETE-FLOW-DATA-EX", Name,
                    "PickUp 완료 후 제품 보유 Flow/Data 확인 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", error=" + ex.Message);
            }
        }

        private int SelectNextPickTargetOrComplete()
        {
            _pickCursor++;

            if (ShouldBlockNewPickForWaferCompletion())
                return StopRemainingPickBatchForWaferCompletion("SelectNextPickTargetOrComplete");

            if (_pickCursor >= _pickBatchItems.Count)
            {
                CurrentStep = PickerPickUpStep.Complete;
                ReleaseInputStageArea();
                return 0;
            }

            CurrentStep = PickerPickUpStep.SelectNextPickTarget;
            return 0;
        }

        private bool ShouldBlockNewPickForWaferCompletion()
        {
            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                return false;

            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion == null || !completion.Enabled)
                return false;

            completion.ObserveCompletionSignals();
            return completion.IsDrainRequested;
        }

        private int StopRemainingPickBatchForWaferCompletion(string boundary)
        {
            // [정정 2026-07-26] 드레인 경계에서 PickUpZRising(Avoid 상승 중)이 남아 있으면
            // 도착 미보장 상태로 배치가 종료된다 — 이동 발행 없이 "불안전 종료"만 기록하고,
            // PickerProcessSequence가 Full-Avoid 생략을 하지 않아 Bottom 첫 스텝의
            // 전 Z Avoid 강제가 도착을 보장한다(알람 없음). EjectPinZ 백그라운드 복귀도 관찰 정리.
            if (HasActivePickUpZHold)
            {
                DrainLeftPickerZHoldUnsafe = true;
                ClearPickUpZHold("웨이퍼 완료 드레인 경계 — Avoid 도착은 Bottom Full-Avoid 강제가 보장");
            }
            if (_pickUpEjectPinAvoidTask != null)
            {
                ObserveBackgroundResultTask(_pickUpEjectPinAvoidTask);
                _pickUpEjectPinAvoidTask = null;
            }

            WriteLog("WaferCompletionRun",
                Name + " Stop After Drain 요청으로 현재 안전 경계에서 남은 신규 Pick 대상을 해제합니다. " +
                "side=" + Side +
                ", boundary=" + (boundary ?? "-") +
                ", zHoldUnsafe=" + DrainLeftPickerZHoldUnsafe +
                ", currentPickerNo=" + _currentPickerNo + " - Ok");
            // 조기 허가 경로: 드레인 중단된 배치의 RESULT 미회수 핸들 정리 (스토어 잔여분은 Clear→ReleaseItems가 정리).
            DrainPickBatchVisionHandles("웨이퍼 완료 드레인 정리");
            InputCameraPickUpPermissionStore.Clear(Side);
            ReleaseInputReservationIfNeeded();
            ReleaseInputStageArea();
            CurrentStep = PickerPickUpStep.Complete;
            return 0;
        }

        private void SetCurrentBatchItem(PickUpBatchItem item)
        {
            _currentBatchItem = item;
            _currentPickerIndex = item != null ? item.PickerIndex : -1;
            _currentPickerNo = item != null ? item.PickerNo : 0;
            _currentDieId = item != null ? item.DieId : "";
            _pickTarget = item != null ? item.PickTarget : null;
            _visionOffset = item != null ? item.VisionOffset : null;
            _targetStageY = item != null ? item.TargetStageY : 0.0;
            _targetPickerX = item != null ? item.TargetPickerX : 0.0;
            _targetPickerY = item != null ? item.TargetPickerY : 0.0;
            _targetPickerT = item != null ? item.TargetPickerT : 0.0;
            _targetPickerZ = item != null ? item.TargetPickerZ : 0.0;
            _targetNeedleX = item != null ? item.TargetNeedleX : 0.0;
            _targetNeedleZ = item != null ? item.TargetNeedleZ : 0.0;
            _targetEjectPinZ = item != null ? item.TargetEjectPinZ : 0.0;
            _targetFormula = item != null ? item.TargetFormula ?? "" : "";
            _diePicked = item != null && item.DiePicked;
            _pickerZContactedByContiPickUp = false;
            _currentPickSafeReturnCompleted = false;
        }

        private void SaveCurrentStateToBatchItem()
        {
            if (_currentBatchItem == null)
                return;

            _currentBatchItem.PickerIndex = _currentPickerIndex;
            _currentBatchItem.PickerNo = _currentPickerNo;
            _currentBatchItem.DieId = _currentDieId;
            _currentBatchItem.PickTarget = _pickTarget;
            _currentBatchItem.VisionOffset = _visionOffset;
            _currentBatchItem.TargetStageY = _targetStageY;
            _currentBatchItem.TargetPickerX = _targetPickerX;
            _currentBatchItem.TargetPickerY = _targetPickerY;
            _currentBatchItem.TargetPickerT = _targetPickerT;
            _currentBatchItem.TargetPickerZ = _targetPickerZ;
            _currentBatchItem.TargetNeedleX = _targetNeedleX;
            _currentBatchItem.TargetNeedleZ = _targetNeedleZ;
            _currentBatchItem.TargetEjectPinZ = _targetEjectPinZ;
            _currentBatchItem.TargetFormula = _targetFormula;
            _currentBatchItem.DiePicked = _diePicked || _currentBatchItem.DiePicked;
        }

        private void ClearCurrentPickContext()
        {
            _currentBatchItem = null;
            _currentPickerIndex = -1;
            _currentPickerNo = 0;
            _currentDieId = "";
            _pickTarget = null;
            _visionOffset = null;
            _targetStageY = 0.0;
            _targetPickerX = 0.0;
            _targetPickerY = 0.0;
            _targetPickerT = 0.0;
            _targetPickerZ = 0.0;
            _targetNeedleX = 0.0;
            _targetNeedleZ = 0.0;
            _targetEjectPinZ = 0.0;
            _targetFormula = "";
            _diePicked = false;
            _pickerZContactedByContiPickUp = false;
        }

        private double ResolveNeedleXForVisionX(double visionX, double visionOffsetX = 0.0)
        {
            double offset = ResolveNeedleCalibrationOffsetX();
            return visionX + visionOffsetX - offset;
        }

        private double ResolveNeedleYForVisionYOffset()
        {
            return ResolveNeedleCalibrationOffsetY();
        }

        private double ResolveNeedleCalibrationOffsetX()
        {
            if (Context == null ||
                Context.Machine == null ||
                Context.Machine.VisionUnit == null ||
                Context.Machine.VisionUnit.Config == null ||
                Context.Machine.VisionUnit.Config.CalibrationData == null ||
                Context.Machine.VisionUnit.Config.CalibrationData.Needle == null ||
                !Context.Machine.VisionUnit.Config.CalibrationData.Needle.Valid)
                return 0.0;

            return Context.Machine.VisionUnit.Config.CalibrationData.Needle.NeedleXToVisionXOffset;
        }

        private double ResolveNeedleCalibrationOffsetY()
        {
            if (Context == null ||
                Context.Machine == null ||
                Context.Machine.VisionUnit == null ||
                Context.Machine.VisionUnit.Config == null ||
                Context.Machine.VisionUnit.Config.CalibrationData == null ||
                Context.Machine.VisionUnit.Config.CalibrationData.Needle == null ||
                !Context.Machine.VisionUnit.Config.CalibrationData.Needle.Valid)
                return 0.0;

            return Context.Machine.VisionUnit.Config.CalibrationData.Needle.NeedleYToVisionYOffset;
        }

        private double ResolveNeedleZPickTarget()
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            try
            {
                CalibrationData data = Context != null &&
                                       Context.Machine != null &&
                                       Context.Machine.VisionUnit != null &&
                                       Context.Machine.VisionUnit.Config != null
                    ? Context.Machine.VisionUnit.Config.CalibrationData
                    : null;
                if (data != null)
                {
                    data.EnsureObjects();
                    if (data.Needle != null &&
                        data.Needle.NeedleZCalibrationValid)
                    {
                        WriteLog("PickerPickUpSequence",
                            Name + " NeedleZ target uses NeedleCalibrationData.NeedlePinReadyPosition=" +
                            data.Needle.NeedlePinReadyPosition.ToString("F6") + " - Check");
                        return data.Needle.NeedlePinReadyPosition;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " NeedleZ calibration target resolve failed, recipe fallback used. error=" +
                    ex.Message + " - Check");
            }

            return stage.Recipe.NeedleZ.ProcessPosition;
        }

        private double ResolveEjectPinZPickTarget()
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            return stage.Recipe.EjectPinZ.ProcessPosition;
        }

        private static double ResolveEjectPinZSyncLiftOffset(InputStageUnit stage)
        {
            return stage != null
                ? stage.ResolvePickUpMotionRecipe().EjectPinOffset
                : 0.0;
        }

        private static double ResolveNeedleZAvoidTarget(InputStageUnit stage)
        {
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            return stage.Recipe.NeedleZ.AvoidPosition;
        }

        private static double ResolveEjectPinZAvoidTarget(InputStageUnit stage)
        {
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            return stage.Recipe.EjectPinZ.AvoidPosition;
        }

        private async Task<VisionAlignResult> RequestInputVisionOffsetAsync(CancellationToken ct, bool applySettleDelay)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return null;

            if (IsSimulationOrDryRun(stage))
            {
                VisionAlignResult dryRunVisionResult = await RequestDryRunInputVisionOffsetAsync(stage, ct, applySettleDelay).ConfigureAwait(false);
                if (dryRunVisionResult != null)
                    return dryRunVisionResult;

                return SimulateInputVisionOffset();
            }

            if (applySettleDelay)
                await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

            if (stage.Vision == null)
                return null;

            ct.ThrowIfCancellationRequested();
            return await stage.Vision.TriggerAlignAsync(VisionAlignTargetIds.InputPickDie).ConfigureAwait(false);
        }

        private async Task<VisionAlignResult> RequestDryRunInputVisionOffsetAsync(
            InputStageUnit stage,
            CancellationToken ct,
            bool applySettleDelay)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (!IsDryRunWithWaferVisionConnected())
                    return null;

                if (stage == null || stage.Vision == null)
                    return null;

                if (applySettleDelay)
                    await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

                VisionAlignResult result = await stage.Vision.TriggerAlignAsync(VisionAlignTargetIds.InputPickDie).ConfigureAwait(false);
                WriteLog(Name,
                    "DryRun " + VisionAlignTargetIds.InputPickDie + " Vision GRAB request completed. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", result=" + (result != null ? "OK" : "NG"));
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog(Name,
                    "DryRun " + VisionAlignTargetIds.InputPickDie + " Vision GRAB request exception. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message + " - SimFallback");
                return null;
            }
            finally
            {
            }
        }

        private VisionAlignResult SimulateInputVisionOffset()
        {
            if (IsDryRunWithVisionDisabled())
                return CreateZeroInputVisionOffset();

            lock (SimVisionRandomLock)
            {
                return new VisionAlignResult
                {
                    DeltaX = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    DeltaY = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    DeltaTheta = (SimVisionRandom.NextDouble() - 0.5) * 0.02
                };
            }
        }

        private VisionAlignResult CreateZeroInputVisionOffset()
        {
            return new VisionAlignResult
            {
                DeltaX = 0.0,
                DeltaY = 0.0,
                DeltaTheta = 0.0
            };
        }

        private async Task<int> MoveInputStageToDiePositionForPickerMotionOnlyAsync(
            InputStageUnit stage,
            double targetX,
            double targetY,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await EnsureEjectPinZAtAvoidBeforePickStageMoveAsync(
                    stage,
                    "Picker Motion Only Test StageY 이동",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageYAndVerifyAsync(
                    stage,
                    targetX,
                    targetY,
                    "Picker Motion Only Test StageY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.WaferY,
                    targetY,
                    "Picker Motion Only Test StageY 최종 위치 확인");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOTION-ONLY-STAGEY-EX", stage != null ? stage.Name : "InputStageUnit",
                    "Picker Motion Only Test StageY 이동 중 예외가 발생했습니다. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsSimulationOrDryRun(InputStageUnit stage)
        {
            // 비전 미사용(UseVision=false) — Wafer/Input die 비전은 비전 작업이므로 합성 결과로 통과.
            if (QMC.CDT320.AppSettingsStore.Current != null && !QMC.CDT320.AppSettingsStore.Current.UseVision)
                return true;

            if (QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive())
                return false;

            if (Options != null && Options.SimulateVisionResult)
                return true;

            if (stage != null && stage.IsInputStageSimulationOrDryRun())
                return true;

            return IsPickerSimulationOrDryRun();
        }

        private static bool IsDryRunWithWaferVisionConnected()
        {
            try
            {
                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings == null || !settings.DryRunMode || !settings.UseVision)
                    return false;

                return QMC.CDT320.VisionComm.VisionCommandService.IsConnected(
                    QMC.CDT320.VisionComm.AutoVisionChannel.Wafer);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private int ResolveVacuumSettleMs()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                return FrontPicker.ResolvePickerVacuumSettleMs(_currentPickerNo);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                return RearPicker.ResolvePickerVacuumSettleMs(_currentPickerNo);

            return 5; //50
        }

        private void RecordColletUse(int pickerNo)
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                FrontPicker.RecordColletUse(pickerNo);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                RearPicker.RecordColletUse(pickerNo);
        }

        private InputStageUnit ResolveInputStage()
        {
            return Context != null && Context.Machine != null
                ? Context.Machine.InputStageUnit
                : null;
        }

        private async Task<int> EnsureInputStageZProcessForVisionAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");
                if (stage.Recipe == null || stage.Recipe.WaferZ == null)
                    return Fail("PICKER-PICKUP-STAGEZ-RECIPE", stage.Name,
                        description + " 전 InputStage Z Process 위치 정보가 없습니다.");

                double target = stage.Recipe.WaferZ.ProcessPosition;
                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferExpandingZ, target))
                {
                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.WaferExpandingZ,
                        target,
                        description + " StageZ process",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.WaferExpandingZ,
                        target,
                        description + " StageZ process",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.WaferExpandingZ,
                    target,
                    description + " StageZ process");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGEZ-PROCESS-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 전 InputStage Z Process 위치 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private async Task<int> EnsureWaferAlignThetaPositionAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                string materialReason;
                if (!MaterialStateService.IsInputStageThetaAlignComplete(wafer, out materialReason))
                    return Fail("PICKER-PICKUP-THETA-ALIGN", stage.Name,
                        description + " 실패. " + materialReason);

                stage.ApplyWaferAlignThetaResult(
                    wafer.InputStageAlignReferenceT,
                    wafer.InputStageAlignCorrectedT,
                    wafer.InputStageAlignOffsetT);

                string readyReason;
                if (!stage.IsWaferAlignThetaResultReady(out readyReason))
                    return Fail("PICKER-PICKUP-THETA-ALIGN", stage.Name,
                        description + " 실패. " + readyReason);

                double targetT;
                if (!stage.TryResolveWaferAlignThetaTarget(out targetT))
                    return Fail("PICKER-PICKUP-THETA-TARGET", stage.Name,
                        description + " 실패. StageT 보정 목표값을 찾을 수 없습니다.");

                if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferT, targetT))
                    return 0;

                int result = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.WaferT,
                    targetT,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.WaferT,
                    targetT,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferT, targetT, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-THETA-POS-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 확인/복귀 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

    }
}
