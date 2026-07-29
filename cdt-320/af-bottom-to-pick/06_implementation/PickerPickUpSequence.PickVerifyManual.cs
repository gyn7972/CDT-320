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
        private async Task<int> EnsureZAxesAtAvoidBeforePickerMoveAsync(
            InputStageUnit stage,
            string description,
            bool skipEjectPinZAvoid,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                // [정정 2026-07-26, 사용자 승인] PickUpZRising: 직전 픽업 픽커 Z가 Avoid로
                // 상승 중이면 그 축의 "도착"을 기다리지 않고 나머지 축만 Avoid 확인한다.
                // 이미 Avoid에 도달했으면 rising을 해제하고 정상 경로로 처리하며,
                // 정지 상태인데 Avoid가 아니면(이상) rising을 해제하고 전체 Avoid 폴백 — 자가 치유.
                int result;
                bool holdHandled = false;
                if (HasActivePickUpZHold)
                {
                    PickerAxis holdZAxisKind = GetPickerZAxis(_pickUpZHoldPickerIndex);
                    BaseAxis holdAxis = GetPickerAxis(holdZAxisKind);
                    bool arrivedAtAvoid = holdAxis != null && !holdAxis.IsMoving &&
                        Math.Abs(holdAxis.ActualPosition - _pickUpZHoldZTarget) <= PickUpZHoldParkToleranceMm;
                    bool risingToAvoid = holdAxis != null && holdAxis.IsMoving;
                    if (arrivedAtAvoid)
                    {
                        ClearPickUpZHold("Avoid 도착 확인 — rising 종료, 정상 경로 처리");
                    }
                    else if (!risingToAvoid)
                    {
                        ClearPickUpZHold("rising 상태 불일치(정지·비Avoid) — 전체 Avoid 폴백");
                    }
                    else
                    {
                        PickerAxis[] zAxes =
                        {
                            PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3
                        };
                        for (int i = 0; i < zAxes.Length; i++)
                        {
                            if (zAxes[i] == holdZAxisKind)
                                continue;

                            double zAvoid = GetPickerTeachingPosition(zAxes[i], "AvoidPosition");
                            if (CanSkipPickerMoveCommand(zAxes[i], zAvoid))
                                continue;

                            result = await MovePickerAxisAndVerifyAsync(
                                zAxes[i],
                                zAvoid,
                                description + " - PickerZ Avoid(PickUpZRising 제외)",
                                ct,
                                "AvoidPosition").ConfigureAwait(false);
                            if (result != 0)
                                return result;
                        }

                        WriteLog("PickerPickUpSequence",
                            Name + " " + description +
                            " - PickUpZRising 픽커 Z는 Avoid 상승을 백그라운드로 두고 나머지만 Avoid 확인. " +
                            "risingPickerNo=" + ToPickerNo(_pickUpZHoldPickerIndex) +
                            ", avoid=" + _pickUpZHoldZTarget.ToString("F3") + " - Check");
                        holdHandled = true;
                    }
                }

                if (!holdHandled)
                {
                    result = await MoveAllPickerZToAvoidAndVerifyAsync(
                        description + " - 모든 PickerZ Avoid",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                WriteLog("PickerPickUpSequence",
                    Name + " " + description +
                    " - 공정 중 NeedleZ는 현재 위치를 유지하고, EjectPinZ 대기(Avoid)/Vacuum OFF 안정화는 StageY/NeedleX 이동 직전 전용 게이트에서 확인합니다. " +
                    "needleZActual=" + (stage.NeedleZ != null ? stage.NeedleZ.ActualPosition.ToString("F6") : "null") + " - Check");

                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                if (skipEjectPinZAvoid)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " " + description +
                        " - EjectPinZ Avoid is deferred to PickUp transfer pre-correction. " +
                        BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                        " - Check");
                }
                else
                {
                    // [정정 2026-07-26] 백그라운드 EjectPinZ Avoid 복귀 join(스냅샷 경합 차단).
                    result = await JoinPickUpEjectPinAvoidBackgroundAsync().ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputStageAxisToAvoidAndVerifyIfNeededAsync(
                        stage,
                        WaferStageAxis.EjectPinZ,
                        ejectPinZAvoid,
                        description + " - EjectPinZ Avoid",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                WriteLog("PickerPickUpSequence",
                    Name + " " + description + " 완료. " +
                    "needleZActual(유지)=" + (stage.NeedleZ != null ? stage.NeedleZ.ActualPosition.ToString("F6") : "null") +
                    ", " +
                    BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                    " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PRE-MOVE-Z-AVOID-EX", Name,
                    description + " 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageAxisToAvoidAndVerifyIfNeededAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            if (IsInputStageAxisAlreadyInPosition(stage, axis, target))
                return 0;

            int result = await MoveInputStageAxisCommandAsync(
                stage,
                axis,
                target,
                description,
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitInputStageAxisInPositionResultAsync(
                stage,
                axis,
                target,
                description,
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return CheckInputStageAxisInPosition(stage, axis, target, description);
        }

        private int VerifyPickTarget()
        {
            if (ShouldBlockNewPickForWaferCompletion())
                return StopRemainingPickBatchForWaferCompletion("VerifyPickTarget");

            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo);
            if (loadedDie != null)
            {
                return Fail("PICKER-PICKUP-PICKER-OCCUPIED", "Material",
                    "Picker가 이미 Die를 가지고 있어 Z축 Pick 동작을 진행할 수 없습니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", loadedDie=" + loadedDie.DieId +
                    ", reservedDie=" + _currentDieId +
                    ", side=" + Side);
            }

            string reason;
            if (!MaterialStateService.ValidateInputStagePickTarget(
                _currentDieId,
                PickerLocationKind,
                _currentPickerNo,
                out reason))
            {
                return Fail("PICKER-PICKUP-DIE-NOT-PICKABLE", "Material",
                    "Reserved input die changed before pick. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", reason=" + reason);
            }
            int result = 0;
            
            //result = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, _targetStageY, "pick corrected StageY");
            //if (result != 0)
            //    return result;

            //result = CheckPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX, "pick corrected PickerX");
            //if (result != 0)
            //    return result;

            //result = CheckPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY, "pick PickerY teaching position");
            //if (result != 0)
            //    return result;

            //result = CheckPickerAxisInPosition(GetPickerTAxis(_currentPickerIndex), _targetPickerT, "pick corrected PickerT");
            //if (result != 0)
            //    return result;

            string needleAreaReason;
            if (!stage.IsNeedleWorkPointInArea(_targetNeedleX, _targetStageY, out needleAreaReason))
            {
                return Fail("PICKER-PICKUP-NEEDLE-WORK-AREA", stage.Name,
                    "PickUp 전 Needle 목표 위치가 작업 가능 영역을 벗어났습니다. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", needleX=" + _targetNeedleX.ToString("F6") +
                    ", stageY=" + _targetStageY.ToString("F6") +
                    ", reason=" + needleAreaReason);
            }

            result = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleX, _targetNeedleX, "pick NeedleX");
            if (result != 0)
                return result;

            WriteLog("PickerPickTargetVerify",
                Name + " pick target verified after move. die=" + _currentDieId +
                ", pickerNo=" + _currentPickerNo +
                ", formula=" + (_targetFormula ?? "") +
                ", stageYState=" + BuildInputStageAxisState(stage, WaferStageAxis.WaferY, _targetStageY) +
                ", needleXState=" + BuildInputStageAxisState(stage, WaferStageAxis.NeedleX, _targetNeedleX) +
                ", pickerXState=" + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX) +
                ", pickerYState=" + BuildPickerAxisState(PickerAxis.PickerY, _targetPickerY) +
                ", pickerTState=" + BuildPickerAxisState(GetPickerTAxis(_currentPickerIndex), _targetPickerT) +
                " - Ok");

            if (_pickerZContactedByContiPickUp)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode Contact 완료 상태이므로 Picker Empty 사전 Flow 확인을 생략합니다. " +
                    "Vacuum은 Contact 전에 이미 ON 처리되었습니다. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo + " - Check");
                CurrentStep = PickerPickUpStep.MovePickerZPick;
                return 0;
            }

            CurrentStep = PickerPickUpStep.VerifyPickerEmptyBeforePick;
            return 0;
        }

        private async Task<int> VerifyPickerEmptyBeforePickAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (ShouldBlockNewPickForWaferCompletion())
                    return StopRemainingPickBatchForWaferCompletion("VerifyPickerEmptyBeforePick");

                await Task.CompletedTask.ConfigureAwait(false);

                if (IsPickUpProductPrecheckBypassed())
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 시작 전 Picker 제품 유/무 확인은 Simulation/DryRun 조건으로 통과합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId + " - Bypass");
                    CurrentStep = PickerPickUpStep.MovePickerZPick;
                    return 0;
                }

                bool vacuumOn;
                string vacuumStateReason;
                if (!TryReadPickerVacuumOutputOn(_currentPickerNo, out vacuumOn, out vacuumStateReason))
                {
                    return Fail("PICKER-PICKUP-PRE-VACUUM-STATE", Name,
                        "PickUp 시작 전 Picker Vacuum 출력 상태 확인 실패. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", reason=" + vacuumStateReason);
                }

                if (!vacuumOn)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 시작 전 Picker 제품 유/무 확인 생략. " +
                        "Vacuum 출력이 OFF이므로 Flow 사전 확인을 하지 않습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", vacuum=OFF" +
                        ", flowCheck=Skipped - Ok");

                    CurrentStep = PickerPickUpStep.MovePickerZPick;
                    return 0;
                }

                bool flowOn = ReadPickerFlowState(_currentPickerNo);
                if (flowOn)
                {
                    return Fail("PICKER-PICKUP-PRE-FLOW-DETECTED", Name,
                        "PickUp 시작 전 Picker 제품 유/무 확인 실패. " +
                        "기존 Vacuum ON 상태에서 Flow 신호가 ON입니다. Picker가 이미 제품을 가지고 있으므로 PickUp을 진행하지 않습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", vacuum=ON" +
                        ", expectedFlow=OFF, actualFlow=ON");
                }

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp 시작 전 Picker 제품 유/무 확인 완료. " +
                    "기존 Vacuum ON 상태에서 Flow 신호가 OFF이므로 Picker가 비어 있다고 판단합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDieId +
                    ", vacuum=ON" +
                    ", flow=OFF - Ok");

                CurrentStep = PickerPickUpStep.MovePickerZPick;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PRE-FLOW-CHECK-EX", Name,
                    "PickUp 시작 전 Picker 제품 유/무 확인 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDieId +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsPickUpProductPrecheckBypassed()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings != null && (settings.BypassHardware || settings.SimulationMode || settings.DryRunMode))
                    return true;

                return Context != null && Context.Controller != null && Context.Controller.GlobalDryRun;
            }
            catch
            {
                return false;
            }
        }

        private bool TryReadPickerVacuumOutputOn(int pickerNo, out bool vacuumOn, out string reason)
        {
            vacuumOn = false;
            reason = string.Empty;

            try
            {
                QMC.Common.IO.BaseDigitalOutput[] outputs = Side == PickerSequenceSide.Front && FrontPicker != null
                    ? FrontPicker.Vacuums
                    : Side == PickerSequenceSide.Rear && RearPicker != null
                        ? RearPicker.Vacuums
                        : null;

                int index = pickerNo - 1;
                if (outputs == null || index < 0 || index >= outputs.Length || outputs[index] == null)
                {
                    reason = "Picker Vacuum output is not configured. side=" + Side +
                             ", pickerNo=" + pickerNo +
                             ", index=" + index;
                    return false;
                }

                vacuumOn = outputs[index].IsOn;
                return true;
            }
            catch (Exception ex)
            {
                reason = "Exception occurred while reading Picker Vacuum output state. " + ex.Message;
                return false;
            }
        }

        private async Task<int> MovePickerZPickAsync(CancellationToken ct)
        {
            try
            {
                // 1-A join(사용자 승인 2026-07-26): 진입 시 선행 발행한 Z PrePick 하강을 여기서
                // 합류한다 — 이후 Z 세부 모션의 절대 이동이 잔여 구간을 이어간다(동기 재시도 겸용).
                Task<int> entryPreDown = _pickUpEntryZPreDownTask;
                if (entryPreDown != null)
                {
                    _pickUpEntryZPreDownTask = null;
                    double preDownTarget = _pickUpEntryZPreDownTarget;
                    _pickUpEntryZPreDownTarget = double.NaN;
                    DateTime joinStart = DateTime.UtcNow;

                    int preDownCommandResult = await SequenceAwaiter.AwaitAsync(entryPreDown, -1, ct).ConfigureAwait(false);
                    int preDownWaitResult = preDownCommandResult == 0 && !double.IsNaN(preDownTarget)
                        ? await WaitPickerAxisMoveDoneAsync(
                            GetPickerZAxis(_currentPickerIndex),
                            preDownTarget,
                            ResolveTimeout(),
                            ct).ConfigureAwait(false)
                        : 0;

                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 진입 Z 선행 join. pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", commandResult=" + preDownCommandResult +
                        ", waitResult=" + preDownWaitResult +
                        ", joinWaitMs=" + (DateTime.UtcNow - joinStart).TotalMilliseconds.ToString("0") +
                        " - " + (preDownCommandResult == 0 && preDownWaitResult == 0 ? "Ok" : "Check"));
                }

                int result = await RunPickupZMotionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                {
                    await TryMovePickerNeedleAndEjectPinZToAvoidAsync("PickUp Z 세부 모션 실패 후 Z축 안전 복귀", ct).ConfigureAwait(false);
                    return result;
                }

                _currentPickSafeReturnCompleted = true;
                CurrentStep = PickerPickUpStep.UpdateMaterialToPicker;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-MOTION-EX", Name,
                    "PickUp Z 세부 모션 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        internal async Task<int> RunPickupZMotionAsync(CancellationToken ct)
        {
            return await RunPickupZMotionAsync(true, ct).ConfigureAwait(false);
        }

        internal async Task<int> RunManualZMotionOnlyAsync(int pickerNo, CancellationToken ct, PickerSequenceOptions options)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);

                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-Z-TEST-STAGE", "InputStageUnit", "PickUp Z 단독 테스트 실패. InputStageUnit is null.");

                int normalizedPickerNo = pickerNo;
                if (normalizedPickerNo < 1)
                    normalizedPickerNo = 1;
                if (normalizedPickerNo > 4)
                    normalizedPickerNo = 4;

                _currentPickerNo = normalizedPickerNo;
                _currentPickerIndex = normalizedPickerNo - 1;
                _currentDieId = "ManualPickUpZTest";
                _pickTarget = null;
                _visionOffset = null;
                // 수동 Z 단독 테스트도 실공정과 동일하게 헤더별 Pick Overdrive를 가산한다.
                double manualHeaderOverdrive = ResolvePickerHeaderOverdrive(_currentPickerIndex);
                _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "PickPosition") + manualHeaderOverdrive;
                if (manualHeaderOverdrive != 0.0)
                {
                    WriteLog("PickerHeaderOverdrive",
                        Name + " ManualPickUpZTest에 헤더 Pick Overdrive 적용. colletNo=" + _currentPickerNo +
                        ", overdriveMm=" + manualHeaderOverdrive.ToString("F6") +
                        ", targetZ=" + _targetPickerZ.ToString("F6") + " - Ok");
                }
                _targetNeedleZ = stage.Recipe != null && stage.Recipe.NeedleZ != null
                    ? stage.Recipe.NeedleZ.ProcessPosition
                    : 0.0;
                _targetEjectPinZ = ResolveEjectPinZPickTarget();
                _lastPickUpZTargets = null;

                int result = await RunPickupZMotionAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp Z 단독 테스트 완료. pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-TEST-EX", Name,
                    "PickUp Z 단독 테스트 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        internal async Task<int> RunManualSelectedDiePickUpAsync(
            string dieId,
            int pickerNo,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            bool picked = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);
                ClearCurrentPickContext();
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;

                if (string.IsNullOrWhiteSpace(dieId))
                    return Fail("PICKER-PICKUP-SELECT-DIE", "Material", "PickUp 테스트 대상 Die가 선택되지 않았습니다.");

                int normalizedPickerNo = pickerNo;
                if (normalizedPickerNo < 1)
                    normalizedPickerNo = 1;
                if (normalizedPickerNo > 4)
                    normalizedPickerNo = 4;

                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                string finishReason;
                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    return Fail("PICKER-PICKUP-STAGE-NOT-FINISH", "InputStage",
                        "선택 Die PickUp 테스트 전에 InputStage Align/DieMapping/Finish가 완료되어야 합니다. " + finishReason);

                DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, normalizedPickerNo);
                if (loadedDie != null)
                {
                    return Fail("PICKER-PICKUP-PICKER-OCCUPIED", "Material",
                        "선택 Die PickUp 테스트 불가: Picker가 이미 Die를 가지고 있습니다. " +
                        "pickerNo=" + normalizedPickerNo +
                        ", loadedDie=" + loadedDie.DieId +
                        ", selectedDie=" + dieId +
                        ", side=" + Side);
                }

                int acquireResult = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
                if (acquireResult != 0)
                    return acquireResult;

                EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "ManualSelectedPickUp");

                InputStagePickTarget target = MaterialStateService.ReserveInputStagePickTargetByDieId(
                    PickerLocationKind,
                    normalizedPickerNo,
                    dieId);
                if (target == null)
                    return Fail("PICKER-PICKUP-SELECT-DIE-RESERVE", "Material",
                        "선택한 Die를 PickUp 대상으로 예약할 수 없습니다. die=" + dieId +
                        ", pickerNo=" + normalizedPickerNo +
                        ", side=" + Side);

                var item = new PickUpBatchItem
                {
                    PickerIndex = normalizedPickerNo - 1,
                    PickerNo = normalizedPickerNo,
                    DieId = target.DieId,
                    PickTarget = target
                };

                _pickBatchItems.Add(item);
                SetCurrentBatchItem(item);

                WriteLog("PickerPickUpSequence",
                    Name + " 선택 Die PickUp 테스트 시작. die=" + dieId +
                    ", pickerNo=" + normalizedPickerNo +
                    ", grid=(" + target.DieMapX + "," + target.DieMapY + ")" +
                    ", inputVisionX=" + target.TargetX +
                    ", inputStageY=" + target.TargetY + " - Start");

                int result = await MoveAllPickerZToAvoidAndVerifyAsync("선택 Die PickUp 테스트 전 Picker Z Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyReservedInputDie();
                if (result != 0)
                    return result;

                result = await MovePickersToAvoidForInputVisionMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAndVisionToDieAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await RequestInputDieVisionInspectionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
                if (CurrentStep == PickerPickUpStep.Complete)
                    return 0;

                result = ApplyInputDieVisionOffset();
                if (result != 0)
                    return result;

                result = await MoveInputVisionToAvoidForPickerMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await CalculatePickTargetsAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = SelectNextPickTarget();
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToAvoidForPickerMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerXStageYPickerTAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyPickTarget();
                if (result != 0)
                    return result;

                result = await VerifyPickerEmptyBeforePickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerZPickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = UpdateMaterialToPicker();
                if (result != 0)
                    return result;

                picked = true;
                WriteLog("PickerPickUpSequence",
                    Name + " 선택 Die PickUp 테스트 완료. die=" + dieId +
                    ", pickerNo=" + normalizedPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-SELECT-DIE-EX", Name,
                    "선택 Die PickUp 테스트 중 예외가 발생했습니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (!picked)
                    ReleaseInputReservationIfNeeded();

                ReleasePickerWorkArea();
                ReleaseInputStageArea();
            }
        }

        internal async Task<int> RunManualSelectedDiePrepareAsync(
            string dieId,
            int pickerNo,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            bool prepared = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);
                ClearCurrentPickContext();
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;

                int result = PrepareManualSelectedDieContext(dieId, pickerNo, true);
                if (result != 0)
                    return result;

                result = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveAllPickerZToAvoidAndVerifyAsync("선택 Die PickUp 준비 전 Picker Z Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyReservedInputDie();
                if (result != 0)
                    return result;

                result = await MovePickersToAvoidForInputVisionMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAndVisionToDieAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await RequestInputDieVisionInspectionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
                if (CurrentStep == PickerPickUpStep.Complete)
                    return 0;

                result = ApplyInputDieVisionOffset();
                if (result != 0)
                    return result;

                result = await MoveInputVisionToAvoidForPickerMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await CalculatePickTargetsAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = SelectNextPickTarget();
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToAvoidForPickerMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerXStageYPickerTAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyPickTarget();
                if (result != 0)
                    return result;

                prepared = true;
                WriteLog("PickerPickUpSequence",
                    Name + " 선택 Die PickUp 준비 완료. die=" + dieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", stageY=" + _targetStageY +
                    ", pickerX=" + _targetPickerX +
                    ", pickerY=" + _targetPickerY +
                    ", pickerT=" + _targetPickerT + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PREPARE-EX", Name,
                    "선택 Die PickUp 준비 중 예외가 발생했습니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (!prepared)
                    ReleaseInputReservationIfNeeded();

                ReleasePickerWorkArea();
                ReleaseInputStageArea();
            }
        }

        internal async Task<int> RunManualPreparedDiePickZAsync(
            string dieId,
            int pickerNo,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            bool picked = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);
                ClearCurrentPickContext();
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;

                int result = PrepareManualSelectedDieContext(dieId, pickerNo, false);
                if (result != 0)
                    return result;

                result = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                VisionOffset offset;
                if (!MaterialStateService.TryGetLatestInputPickVisionOffset(dieId, out offset) || offset == null || !offset.IsValid)
                {
                    return Fail("PICKER-PICKUP-PREPARED-OFFSET", "Material",
                        "선택 Die Pick Z 테스트 전에 Input Vision 검사/Align 준비가 필요합니다. die=" + dieId +
                        ", pickerNo=" + pickerNo);
                }

                _visionOffset = new VisionAlignResult
                {
                    DeltaX = offset.X,
                    DeltaY = offset.Y,
                    DeltaTheta = offset.R
                };
                SaveCurrentStateToBatchItem();

                result = await CalculatePickTargetsAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = SelectNextPickTarget();
                if (result != 0)
                    return result;

                result = VerifyPickTarget();
                if (result != 0)
                    return result;

                result = await VerifyPickerEmptyBeforePickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerZPickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = UpdateMaterialToPicker();
                if (result != 0)
                    return result;

                picked = true;
                WriteLog("PickerPickUpSequence",
                    Name + " 준비 Die Pick Z 테스트 완료. die=" + dieId +
                    ", pickerNo=" + pickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PREPARED-Z-EX", Name,
                    "준비 Die Pick Z 테스트 중 예외가 발생했습니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (!picked)
                    ReleaseInputReservationIfNeeded();

                ReleasePickerWorkArea();
                ReleaseInputStageArea();
            }
        }

        internal async Task<int> RunManualPreparedDiePickZStepAsync(
            string dieId,
            int pickerNo,
            PickerPickUpZManualStep step,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);
                ClearCurrentPickContext();
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;

                int result = PrepareManualSelectedDieContext(dieId, pickerNo, false);
                if (result != 0)
                    return result;

                result = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                VisionOffset offset;
                if (!MaterialStateService.TryGetLatestInputPickVisionOffset(dieId, out offset) || offset == null || !offset.IsValid)
                {
                    return Fail("PICKER-PICKUP-MANUAL-STEP-OFFSET", "Material",
                        "Pick Z Step 테스트 전에 Input Vision 검사/Align 준비가 필요합니다. die=" + dieId +
                        ", pickerNo=" + pickerNo +
                        ", step=" + step);
                }

                _visionOffset = new VisionAlignResult
                {
                    DeltaX = offset.X,
                    DeltaY = offset.Y,
                    DeltaTheta = offset.R
                };
                SaveCurrentStateToBatchItem();

                result = await CalculatePickTargetsAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = SelectNextPickTarget();
                if (result != 0)
                    return result;

                result = VerifyPickTarget();
                if (result != 0)
                    return result;

                result = await RunPickupZManualStepAsync(step, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPickUpSequence",
                    Name + " 준비 Die Pick Z Step 테스트 완료. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", step=" + step + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MANUAL-STEP-EX", Name,
                    "Pick Z Step 테스트 중 예외가 발생했습니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", step=" + step +
                    ", error=" + ex.Message);
            }
            finally
            {
                ReleasePickerWorkArea();
                ReleaseInputStageArea();
            }
        }

        private int PrepareManualSelectedDieContext(string dieId, int pickerNo, bool reserveIfNeeded)
        {
            if (string.IsNullOrWhiteSpace(dieId))
                return Fail("PICKER-PICKUP-SELECT-DIE", "Material", "PickUp 테스트 대상 Die가 선택되지 않았습니다.");

            int normalizedPickerNo = pickerNo;
            if (normalizedPickerNo < 1)
                normalizedPickerNo = 1;
            if (normalizedPickerNo > 4)
                normalizedPickerNo = 4;

            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            string finishReason;
            if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                return Fail("PICKER-PICKUP-STAGE-NOT-FINISH", "InputStage",
                    "선택 Die PickUp 테스트 전에 InputStage Align/DieMapping/Finish가 완료되어야 합니다. " + finishReason);

            DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, normalizedPickerNo);
            if (loadedDie != null)
            {
                return Fail("PICKER-PICKUP-PICKER-OCCUPIED", "Material",
                    "선택 Die PickUp 테스트 불가: Picker가 이미 Die를 가지고 있습니다. " +
                    "pickerNo=" + normalizedPickerNo +
                    ", loadedDie=" + loadedDie.DieId +
                    ", selectedDie=" + dieId +
                    ", side=" + Side);
            }

            InputStagePickTarget target = reserveIfNeeded
                ? MaterialStateService.ReserveInputStagePickTargetByDieId(PickerLocationKind, normalizedPickerNo, dieId)
                : MaterialStateService.GetReservedInputStagePickTarget(PickerLocationKind, normalizedPickerNo, dieId);
            if (target == null)
            {
                return Fail("PICKER-PICKUP-SELECT-DIE-RESERVE", "Material",
                    "선택한 Die를 PickUp 대상으로 확인할 수 없습니다. die=" + dieId +
                    ", pickerNo=" + normalizedPickerNo +
                    ", side=" + Side +
                    ", reserveIfNeeded=" + reserveIfNeeded);
            }

            var item = new PickUpBatchItem
            {
                PickerIndex = normalizedPickerNo - 1,
                PickerNo = normalizedPickerNo,
                DieId = target.DieId,
                PickTarget = target
            };

            _pickBatchItems.Add(item);
            SetCurrentBatchItem(item);
            EnsurePickerWorkAreaReserved(PickerWorkZone.Input, reserveIfNeeded ? "ManualSelectedPickUpPrepare" : "ManualPreparedPickZ");

            WriteLog("PickerPickUpSequence",
                Name + " 선택 Die PickUp 테스트 Context 설정. die=" + dieId +
                ", pickerNo=" + normalizedPickerNo +
                ", grid=(" + target.DieMapX + "," + target.DieMapY + ")" +
                ", inputVisionX=" + target.TargetX +
                ", inputStageY=" + target.TargetY +
                ", reserveIfNeeded=" + reserveIfNeeded + " - Ok");
            return 0;
        }

    }
}
