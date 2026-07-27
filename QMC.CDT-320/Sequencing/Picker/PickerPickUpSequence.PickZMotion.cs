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
        private async Task<int> RunPickupZMotionAsync(bool updateMaterialInspection, CancellationToken ct)
        {
            bool contiContactFlow = _pickerZContactedByContiPickUp;
            try
            {
                ct.ThrowIfCancellationRequested();

                PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                double pickerZAvoid = GetPickerTeachingPosition(pickerZ, "AvoidPosition");

                // [정정 2026-07-26] 현재 픽커의 Z 픽업 모션이 시작되면(하강 명령 예정)
                // 같은 픽커의 rising 추적은 종료한다 — 이후 명령이 상태의 진실.
                if (HasActivePickUpZHold && _pickUpZHoldPickerIndex == _currentPickerIndex)
                    ClearPickUpZHold("현재 픽커 Z 픽업 모션 시작 — rising 종료");

                string syncLiftSettleSource;
                int syncLiftSettleMs = ResolvePickUpSyncLiftSettleMs(config, out syncLiftSettleSource);

                WriteLog("PickerPickUpZ",
                    Name + " PickUp Z motion mode. mode=" + config.MotionMode +
                    ", syncLiftSettleMs=" + syncLiftSettleMs +
                    ", syncLiftSettleSource=" + syncLiftSettleSource +
                    ", pickSettleMs=" + config.PickSettleMs + " - Check");

                if (contiContactFlow)
                {
                    return await RunPickupZMotionAfterContiContactAsync(
                        pickerZ,
                        pickerZAvoid,
                        config,
                        updateMaterialInspection,
                        ct).ConfigureAwait(false);
                }

                if (config.MotionMode == PickerPickUpZMotionMode.SimpleZDownVacuumUp)
                    return await RunSimplePickupZMotionAsync(config, pickerZ, pickerZAvoid, updateMaterialInspection, ct).ConfigureAwait(false);

                int result = await PrepareNeedlePinZForPickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await VacuumOnBeforePickAsync(config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerZPrePickAsync(pickerZ, pickerZAvoid, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerZSlowToContactAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveEjectPinPickerZSyncLiftAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // [사용자 승인 2026-07-26] 흡착 Flow 확인을 미리 걸어두고(비동기)
                // Separate/Avoid 상승과 겹친 뒤 기존 지점에서 결과만 회수한다.
                Task<int> flowVerifyTask = VerifyPickerFlowStateAsync(
                    _currentPickerNo,
                    true,
                    "PickUp Z 모션 완료 후 흡착 Flow 확인",
                    ct);
                ObserveBackgroundResultTask(flowVerifyTask);

                result = await SeparateNeedlePickerZAsync(pickerZ, pickerZAvoid, _lastPickUpZTargets, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (config.PickSettleMs > 0)
                    await Task.Delay(config.PickSettleMs, ct).ConfigureAwait(false);

                result = VerifyDiePickedWithFlowAlarmInBackground(flowVerifyTask, updateMaterialInspection);
                if (result != 0)
                    return result;

                return await MoveZToSafeAfterPickAsync(pickerZ, pickerZAvoid, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-RUN-EX", Name,
                    "PickUp Z 세부 모션 실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                if (contiContactFlow)
                    _pickerZContactedByContiPickUp = false;
            }
        }

        private async Task<int> RunPickupZMotionAfterContiContactAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            PickerPickUpMotionConfig config,
            bool updateMaterialInspection,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int check = CheckPickerAxisInPosition(
                    pickerZ,
                    _targetPickerZ,
                    "PickUp ContiNode Contact 완료 PickerZ");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " PickUp ContiNode Contact 이후 Z 세부 모션 시작. " +
                    "Contact/EjectPinZ Ready 완료 후 SyncLift/Separate/Verify/Safe만 실행합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", targetPickerZ=" + _targetPickerZ.ToString("F6") +
                    ", pickerZAvoid=" + pickerZAvoid.ToString("F6") + " - Start");

                int result = MoveEjectPinPickerZStartPositionCheck(pickerZ);
                if (result != 0)
                    return result;

                result = await MoveEjectPinPickerZSyncLiftAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // [사용자 승인 2026-07-26] 흡착 Flow 확인 비동기 선행 — 표준 흐름과 동일.
                Task<int> flowVerifyTask = VerifyPickerFlowStateAsync(
                    _currentPickerNo,
                    true,
                    "PickUp Z 모션 완료 후 흡착 Flow 확인",
                    ct);
                ObserveBackgroundResultTask(flowVerifyTask);

                result = await SeparateNeedlePickerZAsync(pickerZ, pickerZAvoid, _lastPickUpZTargets, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (config.PickSettleMs > 0)
                    await Task.Delay(config.PickSettleMs, ct).ConfigureAwait(false);

                result = VerifyDiePickedWithFlowAlarmInBackground(flowVerifyTask, updateMaterialInspection);
                if (result != 0)
                    return result;

                return await MoveZToSafeAfterPickAsync(pickerZ, pickerZAvoid, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-CONTI-CONTACT-Z-RUN-EX", Name,
                    "PickUp ContiNode Contact 이후 Z 세부 모션 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private async Task<int> RunSimplePickupZMotionAsync(
            PickerPickUpMotionConfig config,
            PickerAxis pickerZ,
            double pickerZAvoid,
            bool updateMaterialInspection,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // 사용자 지시(2026-07-25): NeedleZ 미상승이면 상승시킨 뒤 진행한다.
                // PickerZ 하강보다 반드시 앞에서 수행한다.
                InputStageUnit needleStage = ResolveInputStage();
                if (needleStage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                int needleZReady = await EnsureNeedleZAtPickTargetAsync(needleStage, ct).ConfigureAwait(false);
                if (needleZReady != 0)
                    return needleZReady;

                int result = await MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    _targetPickerZ,
                    "PickUp 단순 PickerZ 하강",
                    ct,
                    BuildPickerTargetName("DiePickPosition", _currentPickerIndex)).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await VacuumOnForSimplePickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitAfterPickerZContactSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                InputStageUnit stage = ResolveInputStage();
                result = EnsureNeedleVacuumOffForPick(stage, "PickUp 단순 PickerZ AVOID 이동 전");
                if (result != 0)
                    return result;

                // 사용자 확정 속도 모델(2026-07-26): Avoid 복귀 = DefaultVelocity × 전역 스케일(% 미적용).
                double pickerAvoidSpeedPercent = 100.0;
                double pickerAvoidVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, pickerAvoidSpeedPercent);
                double pickerAvoidAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, true);
                double pickerAvoidDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, false);
                double pickerSafeForWaferStageDistance = config != null
                    ? PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(config.PickerSafeForWaferStageDistance)
                    : PickerPickUpMotionConfig.MinimumPickerSafeForWaferStageDistance;

                result = await MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
                    pickerZ,
                    pickerZAvoid,
                    _targetPickerZ,
                    pickerSafeForWaferStageDistance,
                    pickerAvoidVelocity,
                    pickerAvoidAcceleration,
                    pickerAvoidDeceleration,
                    "PickUp 단순 PickerZ 상승",
                    "AvoidPosition",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (config.PickSettleMs > 0)
                    await Task.Delay(config.PickSettleMs, ct).ConfigureAwait(false);

                return await VerifyDiePickedAfterZMotionAsync(updateMaterialInspection, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-SIMPLE-Z-RUN-EX", Name,
                    "PickUp 단순 Z 모션 실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int MoveEjectPinPickerZStartPositionCheck(PickerAxis pickerZ)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            int check = CheckPickerAxisInPosition(pickerZ, _targetPickerZ, "PickUp ContiNode Sync Lift 시작 PickerZ Contact 위치");
            if (check != 0)
                return check;

            check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ, "PickUp ContiNode Sync Lift 시작 NeedleZ 티칭 위치");
            if (check != 0)
                return check;

            return CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "PickUp ContiNode Sync Lift 시작 EjectPinZ 픽업 준비 위치");
        }

        private async Task<int> RunPickupZManualStepAsync(PickerPickUpZManualStep step, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                double pickerZAvoid = GetPickerTeachingPosition(pickerZ, "AvoidPosition");

                switch (step)
                {
                    case PickerPickUpZManualStep.PrepareNeedlePinZ:
                        return await PrepareNeedlePinZForPickAsync(ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.VacuumOnBeforePick:
                        return await VacuumOnBeforePickAsync(config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MovePickerZPrePick:
                        return await MovePickerZPrePickAsync(pickerZ, pickerZAvoid, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MovePickerZSlowToContact:
                        return await MovePickerZSlowToContactAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MoveEjectPinPickerZSyncLift:
                        return await MoveEjectPinPickerZSyncLiftAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.SeparateNeedlePickerZ:
                        return await SeparateNeedlePickerZAsync(pickerZ, pickerZAvoid, _lastPickUpZTargets, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.VerifyDiePicked:
                        return await VerifyDiePickedAfterZMotionAsync(true, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MoveZToSafeAfterPick:
                        return await MoveZToSafeAfterPickAsync(pickerZ, pickerZAvoid, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.UpdateMaterialToPicker:
                        return UpdateMaterialToPicker();
                    default:
                        return Fail("PICKER-PICKUP-MANUAL-STEP-UNKNOWN", Name,
                            "알 수 없는 Pick Z Step입니다. step=" + step);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MANUAL-STEP-RUN-EX", Name,
                    "Pick Z Step 실행 중 예외가 발생했습니다. step=" + step +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// NeedleZ가 픽업 목표 높이(_targetNeedleZ)에 있는지 확인하고, 미달이면 목표까지 동기 상승한다.
        /// 기존 조건: 픽업 진입 단계는 "공정 중 NeedleZ는 현재 위치 유지"를 전제해 NeedleZ를 건드리지
        ///           않고, 상승은 PrepareNeedlePinZForPickAsync만 담당했다. 그래서
        ///           SimpleZDownVacuumUp 경로는 상승 없이 픽업을 진행해 다이를 픽업하지 못했다
        ///           (실장비 2026-07-25 18:17, needleZActual=0 / 목표 182.5).
        /// 현재 기준(사용자 지시 2026-07-25): 미상승을 감지하면 상승시킨 뒤 진행한다.
        /// 속도/가감속은 유닛 기본 이동 경로(MoveInputStageAxisCommandAsync)를 그대로 쓴다 —
        /// 축 레이어에서 MotionSpeedScale이 1회 적용되므로 여기서 별도 계산하지 않는다(작업 규칙 2).
        /// EjectPinZ와 Needle Vacuum은 이 메서드가 다루지 않는다(각 경로의 기존 처리를 유지).
        /// </summary>
        private async Task<int> EnsureNeedleZAtPickTargetAsync(InputStageUnit stage, CancellationToken ct)
        {
            QMC.Common.Motion.BaseAxis needleZ = ResolveInputStageAxis(stage, WaferStageAxis.NeedleZ);
            if (needleZ == null)
                return Fail("PICKER-PICKUP-NEEDLEZ-AXIS", Name,
                    "PickUp NeedleZ 축을 찾을 수 없습니다.");

            if (double.IsNaN(_targetNeedleZ) || double.IsInfinity(_targetNeedleZ))
                return Fail("PICKER-PICKUP-NEEDLEZ-TARGET", Name,
                    "PickUp NeedleZ 픽업 목표가 유효하지 않습니다. target=" + _targetNeedleZ);

            if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ))
            {
                WriteLog("PickerPickUpZ",
                    Name + " PickUp NeedleZ teaching 유지. " +
                    BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) + " - Ok");
                return 0;
            }

            WriteLog("PickerPickUpZ",
                Name + " PickUp NeedleZ 픽업 준비 상승 시작(미상승 상태 감지). " +
                BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) + " - Start");

            int moveResult = await MoveInputStageAxisCommandAsync(
                stage,
                WaferStageAxis.NeedleZ,
                _targetNeedleZ,
                "PickUp NeedleZ 픽업 준비 위치(미상승 보정)",
                ct).ConfigureAwait(false);
            if (moveResult != 0)
                return moveResult;

            return CheckInputStageAxisInPosition(
                stage,
                WaferStageAxis.NeedleZ,
                _targetNeedleZ,
                "PickUp NeedleZ 픽업 준비 위치(미상승 보정)");
        }

        private async Task<int> PrepareNeedlePinZForPickAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                // [사용자 승인 2026-07-26, 병목 #1] 백그라운드 EjectPinZ Avoid 복귀가 아직
                // 진행 중이면 새 EjectPinZ 명령 전에 결과를 회수한다(이동 중 재명령 방지).
                Task<int> pendingEjectPinAvoid = _pickUpEjectPinAvoidTask;
                if (pendingEjectPinAvoid != null)
                {
                    _pickUpEjectPinAvoidTask = null;
                    int backgroundResult = await pendingEjectPinAvoid.ConfigureAwait(false);
                    if (backgroundResult != 0)
                        return backgroundResult;
                }

                Task<int> needleZMove;
                if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ))
                {
                    // 현재 기준: 공정 중 NeedleZ는 Pick teaching 위치를 유지하고 EjectPinZ만 왕복한다.
                    WriteLog("PickerPickUpZ",
                        Name + " PickUp NeedleZ teaching 유지. " +
                        BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) +
                        " - Ok");
                    needleZMove = Task.FromResult(0);
                }
                else
                {
                    needleZMove = MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.NeedleZ,
                        _targetNeedleZ,
                        "PickUp NeedleZ 픽업 준비 위치",
                        ct);
                }
                Task<int> ejectPinZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    _targetEjectPinZ,
                    "PickUp EjectPinZ 픽업 준비 위치",
                    ct);

                int[] results = await Task.WhenAll(needleZMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-NEEDLE-PIN-READY", Name,
                        "PickUp Needle/EjectPin 준비 위치 이동 실패. " +
                        "needleZResult=" + results[0] +
                        ", ejectPinZResult=" + results[1] +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ));
                }

                int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ, "PickUp NeedleZ 픽업 준비 위치");
                if (check != 0)
                    return check;

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "PickUp EjectPinZ 픽업 준비 위치");
                if (check != 0)
                    return check;

                return EnsureNeedleVacuumOnForPick(stage, "PickUp Needle/EjectPin 준비 완료 후");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-NEEDLE-PIN-READY-EX", Name,
                    "PickUp Needle/EjectPin 준비 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> VacuumOnBeforePickAsync(PickerPickUpMotionConfig config, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputStageUnit stage = ResolveInputStage();
                int needleVacuumResult = EnsureNeedleVacuumOnForPick(stage, "PickUp Vacuum ON Step");
                if (needleVacuumResult != 0)
                    return needleVacuumResult;

                SetPickerVacuum(_currentPickerNo, true);

                int contactSettleMs = ResolvePickerContactSettleMs(config);
                WriteLog("PickerPickUpZ",
                    Name + " PickUp Vacuum ON before contact. contactSettleMs=" + contactSettleMs +
                    ", delaySource=PickUp.VacuumOnBeforePickDelayMs" +
                    ", pickerNo=" + _currentPickerNo +
                    " - Ok");

                await Task.CompletedTask.ConfigureAwait(false);
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VACUUM-BEFORE-EX", Name,
                    "PickUp 하강 전 Vacuum ON 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private void RestorePickerVacuumBeforeDefaultFallbackIfNeeded(bool stateKnown, bool wasOn)
        {
            if (!stateKnown || wasOn)
                return;

            try
            {
                SetPickerVacuum(_currentPickerNo, false);
                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode fallback 전 Picker Vacuum을 기존 OFF 상태로 복구했습니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDieId + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode fallback 전 Picker Vacuum OFF 복구 중 예외. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDieId +
                    ", error=" + ex.Message + " - Check");
            }
        }

        private int EnsureNeedleVacuumOnForPick(InputStageUnit stage, string reason)
        {
            if (stage == null)
                return Fail("PICKER-PICKUP-NEEDLE-VAC-NO-STAGE", "InputStageUnit",
                    reason + " Needle Vacuum ON 실패: InputStageUnit is null.");

            if (stage.NeedleVacuum == null)
                return Fail("PICKER-PICKUP-NEEDLE-VAC-NO-OUTPUT", "InputStageUnit",
                    reason + " Needle Vacuum 출력이 없습니다.");

            stage.NeedleVacuum.On();
            _needleVacuumOffConfirmedAtUtc = DateTime.MinValue;
            WriteLog("PickerPickUpZ",
                reason + " Needle Vacuum ON. outputOn=" + stage.NeedleVacuum.IsOn);

            return 0;
        }

        private int EnsureNeedleVacuumOffForPick(InputStageUnit stage, string reason)
        {
            if (stage == null)
                return Fail("PICKER-PICKUP-NEEDLE-VAC-OFF-NO-STAGE", "InputStageUnit",
                    reason + " Needle Vacuum OFF 실패: InputStageUnit is null.");

            if (stage.NeedleVacuum == null)
                return Fail("PICKER-PICKUP-NEEDLE-VAC-OFF-NO-OUTPUT", "InputStageUnit",
                    reason + " Needle Vacuum 출력이 없습니다.");

            stage.NeedleVacuum.Off();
            if (stage.NeedleVacuum.IsOn)
            {
                _needleVacuumOffConfirmedAtUtc = DateTime.MinValue;
                return Fail("PICKER-PICKUP-NEEDLE-VAC-OFF-STATE", "InputStageUnit",
                    reason + " Needle Vacuum OFF 명령 후 출력이 계속 ON 상태입니다.");
            }

            _needleVacuumOffConfirmedAtUtc = DateTime.UtcNow;
            WriteLog("PickerPickUpZ",
                reason + " Needle Vacuum OFF. outputOn=" + stage.NeedleVacuum.IsOn);

            return 0;
        }

        private void TryNeedleVacuumOffForPick(InputStageUnit stage, string reason)
        {
            try
            {
                if (stage == null || stage.NeedleVacuum == null)
                {
                    WriteLog("PickerPickUpZ", reason + " Needle Vacuum OFF skip. output is null.");
                    return;
                }

                stage.NeedleVacuum.Off();
                _needleVacuumOffConfirmedAtUtc = stage.NeedleVacuum.IsOn
                    ? DateTime.MinValue
                    : DateTime.UtcNow;
                WriteLog("PickerPickUpZ",
                    reason + " Needle Vacuum OFF. outputOn=" + stage.NeedleVacuum.IsOn);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpZ", reason + " Needle Vacuum OFF 중 예외. error=" + ex.Message);
            }
        }

        private async Task<int> EnsureEjectPinZAvoidAndVacuumOffSettledBeforeXYAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                {
                    return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-NO-STAGE", "InputStageUnit",
                        description + " 실패: InputStageUnit이 없습니다.");
                }
                if (stage.NeedleVacuum == null)
                {
                    return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-NO-VAC", "InputStageUnit",
                        description + " 실패: Needle Vacuum 출력이 없습니다.");
                }
                if (stage.Recipe == null || stage.Recipe.EjectPinZ == null)
                {
                    return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-NO-RECIPE", stage.Name,
                        description + " 실패: EjectPinZ 대기(Avoid) 위치 정보가 없습니다.");
                }

                // [사용자 승인 2026-07-26, 병목 #1] 픽업 후 백그라운드 EjectPinZ Avoid 복귀가
                // 있으면 여기서 결과를 회수(join)한다 — XY 이동 전 도착 보장 지점.
                Task<int> pendingEjectPinAvoid = _pickUpEjectPinAvoidTask;
                if (pendingEjectPinAvoid != null)
                {
                    _pickUpEjectPinAvoidTask = null;
                    int backgroundResult = await pendingEjectPinAvoid.ConfigureAwait(false);
                    if (backgroundResult != 0)
                        return backgroundResult;
                }

                if (stage.NeedleVacuum.IsOn || _needleVacuumOffConfirmedAtUtc == DateTime.MinValue)
                {
                    int vacuumResult = EnsureNeedleVacuumOffForPick(stage, description);
                    if (vacuumResult != 0)
                        return vacuumResult;
                }

                // 공정 중 NeedleZ는 현재 위치를 유지하고, XY 이동 전에는 EjectPinZ만 대기(Avoid) 위치로 내린다.
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                int requiredVacuumOffMs = ResolveNeedleVacuumOffSettleBeforeXYMs();
                int result = await MoveInputStageAxisToAvoidAndVerifyIfNeededAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    description + " - EjectPinZ Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    description + " - EjectPinZ Avoid 최종 확인");
                if (result != 0)
                    return result;

                double elapsedMs = _needleVacuumOffConfirmedAtUtc == DateTime.MinValue
                    ? 0.0
                    : (DateTime.UtcNow - _needleVacuumOffConfirmedAtUtc).TotalMilliseconds;
                int remainingMs = Math.Max(
                    0,
                    requiredVacuumOffMs - (int)Math.Floor(elapsedMs));
                if (remainingMs > 0)
                    await Task.Delay(remainingMs, ct).ConfigureAwait(false);

                if (stage.NeedleVacuum.IsOn)
                {
                    _needleVacuumOffConfirmedAtUtc = DateTime.MinValue;
                    return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-VAC-ON", stage.Name,
                        description + " 실패: StageY/NeedleX 이동 직전 Needle Vacuum 출력이 ON 상태입니다.");
                }

                elapsedMs = _needleVacuumOffConfirmedAtUtc == DateTime.MinValue
                    ? 0.0
                    : (DateTime.UtcNow - _needleVacuumOffConfirmedAtUtc).TotalMilliseconds;
                if (elapsedMs < requiredVacuumOffMs)
                {
                    int finalRemainingMs = Math.Max(
                        1,
                        requiredVacuumOffMs - (int)Math.Floor(elapsedMs));
                    await Task.Delay(finalRemainingMs, ct).ConfigureAwait(false);
                    elapsedMs = (DateTime.UtcNow - _needleVacuumOffConfirmedAtUtc).TotalMilliseconds;
                }

                WriteLog("PickerPickUpSequence",
                    Name + " " + description + " 완료. StageY/NeedleX 이동을 허용합니다. " +
                    "ejectPinZAvoid=" + ejectPinZAvoid.ToString("F6") +
                    ", ejectPinZActual=" + (stage.EjectPinZ != null ? stage.EjectPinZ.ActualPosition.ToString("F6") : "null") +
                    ", needleZActual(유지)=" + (stage.NeedleZ != null ? stage.NeedleZ.ActualPosition.ToString("F6") : "null") +
                    ", needleVacuumOn=" + stage.NeedleVacuum.IsOn +
                    ", vacuumOffElapsedMs=" + elapsedMs.ToString("F1") +
                    ", requiredMs=" + requiredVacuumOffMs +
                    ", delaySource=PickUp.NeedleVacuumOffSettleBeforeXYMs - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private Task<int> VacuumOnForSimplePickAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputStageUnit stage = ResolveInputStage();
                int needleVacuumResult = EnsureNeedleVacuumOnForPick(stage, "PickUp Simple Vacuum ON Step");
                if (needleVacuumResult != 0)
                    return Task.FromResult(needleVacuumResult);

                SetPickerVacuum(_currentPickerNo, true);

                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("PICKER-PICKUP-SIMPLE-VACUUM-EX", Name,
                    "PickUp 단순 Z 모션 Vacuum ON 중 예외가 발생했습니다. error=" + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZPrePickAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (config.PickerZPrePickDistance <= 0.0)
                    return 0;

                double target = ResolveTargetToward(_targetPickerZ, pickerZAvoid, config.PickerZPrePickDistance);
                return await MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    target,
                    "PickUp PickerZ PrePick 위치",
                    ct,
                    "PickUpPrePick").ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PRE-PICK-EX", Name,
                    "PickUp PickerZ PrePick 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZSlowToContactAsync(
            PickerAxis pickerZ,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                double velocity = ResolvePickerAxisVelocityByPercent(pickerZ, config.PickerZSlowApproachSpeedPercent);
                double acceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, config.PickerZSlowApproachSpeedPercent, true);
                double deceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, config.PickerZSlowApproachSpeedPercent, false);
                return await MovePickerAxisWithMotionAndVerifyAsync(
                    pickerZ,
                    _targetPickerZ,
                    velocity,
                    acceleration,
                    deceleration,
                    "PickUp PickerZ 저속 Contact 위치",
                    BuildPickerTargetName("DiePickPosition", _currentPickerIndex),
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-SLOW-CONTACT-EX", Name,
                    "PickUp PickerZ 저속 Contact 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveEjectPinPickerZSyncLiftAsync(
            PickerAxis pickerZ,
            CancellationToken ct)
        {
            PickUpZTargets syncTargets = new PickUpZTargets();
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                stage.Config.EnsurePickUpMotionDefaults();
                double syncLiftDistance = stage.Config.PickUpNeedleSyncLiftDistance;
                double ejectPinSyncLiftOffset = ResolveEjectPinZSyncLiftOffset(stage);

                syncTargets.PickerZ = _targetPickerZ + syncLiftDistance;
                syncTargets.NeedleZ = _targetNeedleZ;
                syncTargets.EjectPinZ = _targetEjectPinZ + syncLiftDistance + ejectPinSyncLiftOffset;
                syncTargets.EjectPinSyncLiftOffset = ejectPinSyncLiftOffset;
                _lastPickUpZTargets = syncTargets;

                if (syncLiftDistance <= 0.0)
                    return 0;

                int startCheck = CheckPickerAxisInPosition(pickerZ, _targetPickerZ, "PickUp Sync Lift 시작 PickerZ 티칭 위치");
                if (startCheck != 0)
                    return startCheck;

                startCheck = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ, "PickUp Sync Lift 시작 NeedleZ 티칭 위치");
                if (startCheck != 0)
                    return startCheck;

                startCheck = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "PickUp Sync Lift 시작 EjectPinZ 티칭 위치");
                if (startCheck != 0)
                    return startCheck;

                return await MovePickerNeedleZSyncLiftFallbackAsync(
                    pickerZ,
                    stage,
                    syncTargets,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-EX", Name,
                    "PickUp PickerZ/EjectPinZ 개별 비동기 상승 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveEjectPinPickerZSyncLiftAndSettleAsync(
            PickerAxis pickerZ,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            int result = await MoveEjectPinPickerZSyncLiftAsync(pickerZ, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            await WaitAfterSyncLiftSettleAsync(config, ct).ConfigureAwait(false);
            // Sync Lift는 이동 완료/InPosition 대기 결과를 사용하고 동일 목표의 최종 중복 체크는 수행하지 않는다.
            return 0;
        }

        private async Task WaitAfterSyncLiftSettleAsync(PickerPickUpMotionConfig config, CancellationToken ct)
        {
            string source;
            int waitMs = ResolvePickUpSyncLiftSettleMs(config, out source);
            if (waitMs <= 0)
                return;

            // 현재 기준: Sync Lift 직후 Separate 전에 필요한 안정화 대기만 적용한다.
            WriteLog("PickerPickUpZ",
                Name + " PickUp Sync Lift settle wait start. waitMs=" + waitMs +
                ", source=" + source + " - Wait");
            await Task.Delay(waitMs, ct).ConfigureAwait(false);
            WriteLog("PickerPickUpZ",
                Name + " PickUp Sync Lift settle wait complete. waitMs=" + waitMs +
                ", source=" + source + " - Ok");
        }

        private int ResolvePickUpSyncLiftSettleMs(PickerPickUpMotionConfig config, out string source)
        {
            source = "None";
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage != null && stage.Config != null)
                {
                    stage.Config.EnsurePickUpMotionDefaults();
                    int inputStageWaitMs = Math.Max(0, stage.Config.PickUpNeedleSyncLiftSettleMs);
                    source = "InputStage.PickUpNeedleSyncLiftSettleMs";
                    return inputStageWaitMs;
                }

                int pickerWaitMs = config != null ? Math.Max(0, config.SyncLiftSettleMs) : 0;
                if (pickerWaitMs > 0)
                    source = "Picker.PickUp.SyncLiftSettleMs";

                return pickerWaitMs;
            }
            catch
            {
                source = "Error";
                return config != null ? Math.Max(0, config.SyncLiftSettleMs) : 0;
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZSlowToContactAndSettleAsync(
            PickerAxis pickerZ,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            int result = await MovePickerZSlowToContactAsync(pickerZ, config, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await WaitAfterPickerZContactSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
        }

        private async Task<int> WaitAfterPickerZContactSettleAsync(
            PickerAxis pickerZ,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            int check = CheckPickerAxisInPosition(
                pickerZ,
                _targetPickerZ,
                "PickUp PickerZ Die Touch 위치 완료 확인");
            if (check != 0)
                return check;

            int contactSettleMs = ResolvePickerContactSettleMs(config);
            if (contactSettleMs <= 0)
                return 0;

            WriteLog("PickerPickUpZ",
                Name + " PickUp PickerZ contact settle wait start. waitMs=" + contactSettleMs +
                ", delaySource=PickUp.VacuumOnBeforePickDelayMs" +
                ", pickerNo=" + _currentPickerNo +
                ", targetPickerZ=" + _targetPickerZ + " - Wait");
            await Task.Delay(contactSettleMs, ct).ConfigureAwait(false);
            WriteLog("PickerPickUpZ",
                Name + " PickUp PickerZ contact settle wait complete. waitMs=" + contactSettleMs +
                ", delaySource=PickUp.VacuumOnBeforePickDelayMs" +
                ", pickerNo=" + _currentPickerNo +
                ", targetPickerZ=" + _targetPickerZ + " - Ok");

            return 0;
        }

        private static int ResolvePickerContactSettleMs(PickerPickUpMotionConfig config)
        {
            return config != null ? Math.Max(0, config.VacuumOnBeforePickDelayMs) : 0;
        }

        private async Task<int> MovePickerNeedleZSyncLiftFallbackAsync(
            PickerAxis pickerZ,
            InputStageUnit stage,
            PickUpZTargets syncTargets,
            CancellationToken ct)
        {
            // 사용자 확정 속도 모델(2026-07-26): 이젝트핀과 피커가 동시에 올라오는 구동은
            // 각축 DefaultVelocity × 전역 스케일 × PICKER Z SEPARATE SPEED %를 따른다
            // (기존 InputStage PickUpNeedleSyncLiftVelocity 고정값 사용 폐지).
            PickerPickUpMotionConfig syncLiftConfig = ResolvePickUpMotionConfig();
            double syncLiftPercent = syncLiftConfig != null ? syncLiftConfig.PickerZSeparateSpeedPercent : 1.0;
            Task<int> pickerMove = MovePickerAxisWithMotionAndVerifyAsync(
                pickerZ,
                syncTargets.PickerZ,
                ResolvePickerAxisVelocityByPercent(pickerZ, syncLiftPercent),
                ResolvePickerAxisAccelerationByPercent(pickerZ, syncLiftPercent, true),
                ResolvePickerAxisAccelerationByPercent(pickerZ, syncLiftPercent, false),
                "PickUp PickerZ/EjectPinZ 개별 비동기 상승 PickerZ",
                "PickUpSyncLift",
                ct,
                true);
            Task<int> ejectPinMove = MoveInputStageAxisWithMotionAndVerifyAsync(
                stage,
                WaferStageAxis.EjectPinZ,
                syncTargets.EjectPinZ,
                ResolveInputStageAxisVelocityByPercent(stage, WaferStageAxis.EjectPinZ, syncLiftPercent),
                ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.EjectPinZ, syncLiftPercent, true),
                ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.EjectPinZ, syncLiftPercent, false),
                "PickUp PickerZ/EjectPinZ 개별 비동기 상승 EjectPinZ",
                ct,
                null,
                true);

            int[] results = await Task.WhenAll(pickerMove, ejectPinMove).ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0)
            {
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT", Name,
                    "PickUp PickerZ/EjectPinZ 개별 비동기 상승 실패. " +
                    "pickerZResult=" + results[0] +
                    ", ejectPinZResult=" + results[1] +
                    ", " + BuildPickerAxisState(pickerZ, syncTargets.PickerZ) +
                    ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, syncTargets.EjectPinZ) +
                    ", NeedleZHold=" + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, syncTargets.NeedleZ));
            }

            int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, syncTargets.NeedleZ, "PickUp Sync Lift NeedleZ 티칭 위치 유지");
            if (check != 0)
                return check;

            WriteLog("PickerPickUpSyncLift",
                Name + " PickUp PickerZ/EjectPinZ async lift move wait complete. pickerNo=" + _currentPickerNo +
                ", ejectPinSyncLiftOffset=" + syncTargets.EjectPinSyncLiftOffset.ToString("F6") +
                ", pickerZState=" + BuildPickerAxisState(pickerZ, syncTargets.PickerZ) +
                ", ejectPinZState=" + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, syncTargets.EjectPinZ) +
                ", needleZHoldState=" + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, syncTargets.NeedleZ) +
                " - WaitSettle");

            return 0;
        }

        private static bool ShouldUseSimulatedSyncLiftFallback()
        {
            QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
            return settings == null ||
                   settings.SimulationMode ||
                   settings.DryRunMode ||
                   settings.BypassHardware ||
                   !settings.UseAjin ||
                   !QMC.CDT320.Ajin.AjinFactory.IsRealBoardReady;
        }

        private int CheckAxisReadyForInterpolatedSyncLift(QMC.Common.Motion.BaseAxis axis, string axisName)
        {
            if (axis == null)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-AXIS", Name, axisName + " 축을 찾을 수 없습니다.");
            if (axis.Setup == null || axis.Setup.AxisNo < 0)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-AXIS", Name, axisName + " 축 번호가 설정되지 않았습니다.");
            if (!axis.IsServoOn)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-READY", Name, axisName + " 서보가 OFF 상태입니다.");
            if (axis.IsAlarm)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-READY", Name, axisName + " 알람이 ON 상태입니다.");
            if (axis.IsMoving)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-READY", Name, axisName + " 축이 이미 이동 중입니다.");

            return 0;
        }

        private static void StopSyncLiftAxes(params QMC.Common.Motion.BaseAxis[] axes)
        {
            if (axes == null)
                return;

            for (int i = 0; i < axes.Length; i++)
            {
                try
                {
                    if (axes[i] != null)
                        axes[i].Stop();
                }
                catch
                {
                }
            }
        }

        private async Task<int> SeparateNeedlePickerZAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            PickUpZTargets syncTargets,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                stage.Config.EnsurePickUpMotionDefaults();
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);

                // [사용자 지시 2026-07-27] Separate(피커Z 단독 1mm 저속 분리) 스텝 폐지 —
                // SyncLift 완료 후 곧바로 Avoid 상승(safe 조기 반환)으로 연결한다(~57ms/픽커 회수).
                // PickerZSeparateDistance/SpeedPercent 설정은 미사용으로 남는다(동작만 제거).
                // 사용자 확정 속도 모델(2026-07-26): Avoid 복귀 = DefaultVelocity × 전역 스케일(% 미적용).
                double pickerAvoidSpeedPercent = 100.0;
                double pickerSafeForWaferStageDistance = config != null
                    ? PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(config.PickerSafeForWaferStageDistance)
                    : PickerPickUpMotionConfig.MinimumPickerSafeForWaferStageDistance;
                double pickerAvoidVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, pickerAvoidSpeedPercent);
                double pickerAvoidAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, true);
                double pickerAvoidDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, false);
                WriteLog("PickerPickUpZ",
                    "PickerZ avoid rise resolved (Separate 폐지). axis=" + pickerZ +
                    ", avoid=" + pickerZAvoid.ToString("0.###") +
                    ", avoidVelocity=" + pickerAvoidVelocity.ToString("0.###") +
                    ", avoidAcceleration=" + pickerAvoidAcceleration.ToString("0.###") +
                    ", avoidDeceleration=" + pickerAvoidDeceleration.ToString("0.###") +
                    ", pickerSafeForWaferStageDistance=" + pickerSafeForWaferStageDistance.ToString("0.###"));

                int pickerResult = await MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
                    pickerZ,
                    pickerZAvoid,
                    _targetPickerZ,
                    pickerSafeForWaferStageDistance,
                    pickerAvoidVelocity,
                    pickerAvoidAcceleration,
                    pickerAvoidDeceleration,
                    "PickUp Sync Lift 후 PickerZ Avoid 최종 이동",
                    "AvoidPosition",
                    ct).ConfigureAwait(false);
                if (pickerResult != 0)
                    return pickerResult;

                // [사용자 승인 2026-07-26] PickerZ는 Avoid로 상승 중(safe 통과) — 이후 X 진입이
                // Avoid 도착을 기다리지 않도록 rising 상태를 기록한다.
                MarkPickUpZRising(pickerZ, pickerZAvoid);

                WriteLog("PickerPickUpZ",
                    Name + " PickerZ Stage Safe 도달 후 Needle Vacuum OFF 및 EjectPinZ Avoid 이동을 시작합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", safeDistance=" + pickerSafeForWaferStageDistance.ToString("0.###") +
                    ", ejectPinZAvoid=" + ejectPinZAvoid.ToString("0.###") + " - Start");

                // [사용자 승인 2026-07-26, 병목 #1] EjectPinZ Avoid 복귀는 백그라운드로 돌리고
                // 시퀀스는 진행한다. 도착 보장은 다음 die의 XY 게이트
                // (EnsureEjectPinZAvoidAndVacuumOffSettledBeforeXYAsync)에서 join.
                _pickUpEjectPinAvoidTask = MoveEjectPinZToAvoidKeepNeedleZAsync(
                    stage,
                    _targetNeedleZ,
                    ejectPinZAvoid,
                    ct);
                ObserveBackgroundResultTask(_pickUpEjectPinAvoidTask);
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SEPARATE-EX", Name,
                    "PickUp PickerZ/NeedlePinZ 분리 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            double pickerTouchZ,
            double pickerSafeForWaferStageDistance,
            double velocity,
            double acceleration,
            double deceleration,
            string description,
            string targetName,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                BaseAxis axis = GetPickerAxis(pickerZ);
                if (axis == null)
                    return Fail("PICKER-PICKUP-Z-SAFE-AXIS", Name,
                        description + " 실패. PickerZ 축을 찾을 수 없습니다. axis=" + pickerZ);

                double safeDistance = PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(pickerSafeForWaferStageDistance);
                double safeTarget = ResolveTargetToward(pickerTouchZ, pickerZAvoid, safeDistance);
                double tolerance = ResolveAxisTolerance(axis);
                bool fullAvoidRequired = Math.Abs(safeTarget - pickerZAvoid) <= tolerance;
                bool alreadyMovingToAvoid = axis.IsMoving && Math.Abs(axis.CommandPosition - pickerZAvoid) <= tolerance;

                WriteLog("PickerPickUpZ",
                    description + " command/wait safe. axis=" + pickerZ +
                    ", touchZ=" + pickerTouchZ.ToString("0.###") +
                    ", avoidZ=" + pickerZAvoid.ToString("0.###") +
                    ", safeDistance=" + safeDistance.ToString("0.###") +
                    ", safeTarget=" + safeTarget.ToString("0.###") +
                    ", fullAvoidRequired=" + fullAvoidRequired +
                    ", alreadyMovingToAvoid=" + alreadyMovingToAvoid +
                    ", velocity=" + velocity.ToString("0.###") +
                    ", acceleration=" + acceleration.ToString("0.###") +
                    ", deceleration=" + deceleration.ToString("0.###"));

                if (!alreadyMovingToAvoid)
                {
                    int commandResult = await MovePickerAxisCommandWithMotionAsync(
                        pickerZ,
                        pickerZAvoid,
                        velocity,
                        acceleration,
                        deceleration,
                        targetName).ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-PICKUP-Z-SAFE-CMD", Name,
                            description + " 이동 명령 실패. result=" + commandResult +
                            ", velocity=" + velocity +
                            ", acc=" + acceleration +
                            ", dec=" + deceleration +
                            ", " + BuildPickerAxisState(pickerZ, pickerZAvoid));
                }

                if (fullAvoidRequired)
                {
                    int waitResult = await WaitPickerAxisInPositionResultAsync(pickerZ, pickerZAvoid, description, ct).ConfigureAwait(false);
                    if (waitResult != 0)
                        return waitResult;

                    return CheckPickerAxisInPosition(pickerZ, pickerZAvoid, description);
                }

                return await WaitPickerZSafeForWaferStageAsync(
                    pickerZ,
                    pickerTouchZ,
                    pickerZAvoid,
                    safeTarget,
                    tolerance,
                    description,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SAFE-EX", Name,
                    description + " 안전 상승 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerZSafeForWaferStageAsync(
            PickerAxis pickerZ,
            double pickerTouchZ,
            double pickerZAvoid,
            double safeTarget,
            double tolerance,
            string description,
            CancellationToken ct)
        {
            BaseAxis axis = GetPickerAxis(pickerZ);
            if (axis == null)
                return Fail("PICKER-PICKUP-Z-SAFE-AXIS", Name,
                    description + " 안전 상승 확인 실패. PickerZ 축을 찾을 수 없습니다. axis=" + pickerZ);

            double direction = Math.Sign(pickerZAvoid - pickerTouchZ);
            if (direction == 0.0)
                return 0;

            DateTime startedAt = DateTime.UtcNow;
            DateTime moveStartGraceUntil = startedAt.AddMilliseconds(250.0);
            bool sawMoving = axis.IsMoving;
            int timeoutMs = ResolveTimeout();

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                axis = GetPickerAxis(pickerZ);
                if (axis == null)
                    return Fail("PICKER-PICKUP-Z-SAFE-AXIS", Name,
                        description + " 안전 상승 확인 실패. PickerZ 축을 찾을 수 없습니다. axis=" + pickerZ);
                if (!axis.IsServoOn)
                    return Fail("PICKER-PICKUP-Z-SAFE-SERVO", Name,
                        description + " 안전 상승 확인 실패. PickerZ 서보가 OFF입니다. " + BuildPickerAxisState(pickerZ, safeTarget));
                if (axis.IsAlarm)
                    return Fail("PICKER-PICKUP-Z-SAFE-ALARM", Name,
                        description + " 안전 상승 확인 실패. PickerZ 알람이 ON입니다. " + BuildPickerAxisState(pickerZ, safeTarget));

                if (axis.IsMoving)
                    sawMoving = true;

                double actual = axis.ActualPosition;
                bool reached = direction > 0.0
                    ? actual >= safeTarget - tolerance
                    : actual <= safeTarget + tolerance;
                if (reached)
                {
                    WriteLog("PickerPickUpZ",
                        description + " safe height reached. axis=" + pickerZ +
                        ", touchZ=" + pickerTouchZ.ToString("0.###") +
                        ", avoidZ=" + pickerZAvoid.ToString("0.###") +
                        ", safeTarget=" + safeTarget.ToString("0.###") +
                        ", actual=" + actual.ToString("0.###") +
                        ", command=" + axis.CommandPosition.ToString("0.###") +
                        ", moving=" + axis.IsMoving + " - Ok");
                    return 0;
                }

                if (!axis.IsMoving && (sawMoving || DateTime.UtcNow >= moveStartGraceUntil))
                {
                    return Fail("PICKER-PICKUP-Z-SAFE-NOT-REACHED", Name,
                        description + " 안전 상승 거리 도달 전 PickerZ가 정지했습니다. " +
                        "touchZ=" + pickerTouchZ.ToString("0.###") +
                        ", avoidZ=" + pickerZAvoid.ToString("0.###") +
                        ", safeTarget=" + safeTarget.ToString("0.###") +
                        ", " + BuildPickerAxisState(pickerZ, safeTarget));
                }

                if ((DateTime.UtcNow - startedAt).TotalMilliseconds > timeoutMs)
                {
                    return Fail("PICKER-PICKUP-Z-SAFE-TIMEOUT", Name,
                        description + " 안전 상승 거리 확인 timeout. " +
                        "timeoutMs=" + timeoutMs +
                        ", touchZ=" + pickerTouchZ.ToString("0.###") +
                        ", avoidZ=" + pickerZAvoid.ToString("0.###") +
                        ", safeTarget=" + safeTarget.ToString("0.###") +
                        ", " + BuildPickerAxisState(pickerZ, safeTarget));
                }

                await Task.Delay(10, ct).ConfigureAwait(false);
            }
        }

        private static double ResolveAxisTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.001;
        }

        // 기존 안전 복구용: 실패/비상 상황에서는 NeedleZ까지 Avoid 복귀할 수 있게 유지한다.
        private async Task<int> MoveNeedlePinZToAvoidAndVacuumOffAsync(
            InputStageUnit stage,
            double needleTarget,
            double ejectTarget,
            CancellationToken ct)
        {
            try
            {
                int needleVacuumOffResult = EnsureNeedleVacuumOffForPick(stage, "PickUp 후 NeedlePinZ/NeedleZ AVOID 이동 전");
                if (needleVacuumOffResult != 0)
                    return needleVacuumOffResult;

                int ejectResult = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    "PickUp 후 NeedlePinZ(EjectPinZ) Avoid 이동",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                int needleResult = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleTarget,
                    "PickUp 후 NeedleZ Avoid 이동",
                    ct).ConfigureAwait(false);
                if (needleResult != 0)
                    return needleResult;

                ejectResult = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    "PickUp 후 NeedlePinZ(EjectPinZ) Avoid 이동",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                needleResult = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleTarget,
                    "PickUp 후 NeedleZ Avoid 이동",
                    ct).ConfigureAwait(false);
                if (needleResult != 0)
                    return needleResult;

                int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, ejectTarget, "PickUp 후 NeedlePinZ(EjectPinZ) Avoid 이동");
                if (check != 0)
                    return check;

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, needleTarget, "PickUp 후 NeedleZ Avoid 이동");
                if (check != 0)
                    return check;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-NEEDLE-PIN-AVOID-VAC-OFF-EX", Name,
                    "PickUp 후 NeedlePinZ Avoid 및 Needle Vacuum OFF 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedlePinZSeparateAsync(
            InputStageUnit stage,
            double needleTarget,
            double ejectTarget,
            CancellationToken ct)
        {
            try
            {
                double needleVelocity = ResolveInputStageAxisVelocityByPercent(stage, WaferStageAxis.NeedleZ, stage.Config.PickUpNeedleSeparateSpeedPercent);
                double needleAcceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.NeedleZ, stage.Config.PickUpNeedleSeparateSpeedPercent, true);
                double needleDeceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.NeedleZ, stage.Config.PickUpNeedleSeparateSpeedPercent, false);
                double ejectVelocity = ResolveInputStageAxisVelocityByPercent(stage, WaferStageAxis.EjectPinZ, stage.Config.PickUpNeedleSeparateSpeedPercent);
                double ejectAcceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.EjectPinZ, stage.Config.PickUpNeedleSeparateSpeedPercent, true);
                double ejectDeceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.EjectPinZ, stage.Config.PickUpNeedleSeparateSpeedPercent, false);
                Task<int> needleMove = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleTarget,
                    needleVelocity,
                    needleAcceleration,
                    needleDeceleration,
                    "PickUp 분리 NeedleZ 하강",
                    ct);
                Task<int> ejectMove = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    ejectVelocity,
                    ejectAcceleration,
                    ejectDeceleration,
                    "PickUp 분리 EjectPinZ 하강",
                    ct);

                int[] results = await Task.WhenAll(needleMove, ejectMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-NEEDLE-PIN-SEPARATE", Name,
                        "PickUp Needle/EjectPin 분리 이동 실패. " +
                        "needleZResult=" + results[0] +
                        ", ejectPinZResult=" + results[1] +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTarget) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectTarget));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-NEEDLE-PIN-SEPARATE-EX", Name,
                    "PickUp Needle/EjectPin 분리 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        // [사용자 지시 2026-07-27] 픽업 흡착 Flow 확인은 조인하지 않는다 — 선행 시작된 백그라운드
        // Task가 픽커 I/O 타임아웃(기본 5000ms)까지 폴링하고, 실패하면 Task 내부 Fail()이 직접
        // 알람(PICKER-FLOW-CHECK)을 올린다(AlarmManager.Raise — 조인 여부와 무관). 여기서는
        // 자재 데이터 갱신만 수행하고 다음 die 진행을 막지 않는다.
        private int VerifyDiePickedWithFlowAlarmInBackground(Task<int> flowVerifyTask, bool updateMaterialInspection)
        {
            if (flowVerifyTask != null &&
                flowVerifyTask.Status == TaskStatus.RanToCompletion &&
                flowVerifyTask.Result != 0)
            {
                // 조인 없이도 이미 실패가 확정된 케이스 — 알람은 Task가 이미 올렸고, 이 die만 실패 처리한다.
                return flowVerifyTask.Result;
            }

            WriteLog("PickerPickUpZ",
                Name + " 흡착 Flow 확인을 백그라운드로 계속합니다(조인 없음, 실패 시 타임아웃 후 알람). " +
                "pickerNo=" + _currentPickerNo +
                ", flowTaskDone=" + (flowVerifyTask != null && flowVerifyTask.IsCompleted) + " - Check");

            if (!updateMaterialInspection)
                return 0;

            return VerifyDiePicked();
        }

        private async Task<int> VerifyDiePickedAfterZMotionAsync(bool updateMaterialInspection, CancellationToken ct)
        {
            return await VerifyDiePickedAfterZMotionAsync(null, updateMaterialInspection, ct).ConfigureAwait(false);
        }

        private async Task<int> VerifyDiePickedAfterZMotionAsync(
            Task<int> flowVerifyTask,
            bool updateMaterialInspection,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // [사용자 승인 2026-07-26] 선행 시작된 Flow 확인 태스크가 있으면 결과만 회수한다.
                int flowResult = flowVerifyTask != null
                    ? await flowVerifyTask.ConfigureAwait(false)
                    : await VerifyPickerFlowStateAsync(
                        _currentPickerNo,
                        true,
                        "PickUp Z 모션 완료 후 흡착 Flow 확인",
                        ct).ConfigureAwait(false);
                if (flowResult != 0)
                    return flowResult;

                if (!updateMaterialInspection)
                    return 0;

                return VerifyDiePicked();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VERIFY-PICKED-EX", Name,
                    "PickUp 흡착 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveZToSafeAfterPickAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            CancellationToken ct)
        {
            return await MovePickerEjectPinZToAvoidKeepNeedleZAsync(
                pickerZ,
                pickerZAvoid,
                _targetNeedleZ,
                "PickUp 완료 후 PickerZ/EjectPinZ 안전 복귀 및 NeedleZ teaching 유지",
                ct).ConfigureAwait(false);
        }

        private PickerPickUpMotionConfig ResolvePickUpMotionConfig()
        {
            PickerPickUpMotionConfig config = null;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                config = FrontPicker.Config.PickUp;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                config = RearPicker.Config.PickUp;

            if (config == null)
                config = new PickerPickUpMotionConfig();

            config.Ensure();
            return config;
        }

        private int ResolveNeedleVacuumOffSettleBeforeXYMs()
        {
            PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
            if (config == null)
                return 100;

            return Math.Max(0, Math.Min(60000, config.NeedleVacuumOffSettleBeforeXYMs));
        }

        private async Task<int> MovePickerEjectPinZToAvoidKeepNeedleZAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            double needleTeachingTarget,
            string description,
            CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

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

                // [정정 2026-07-26, 사용자 승인] PrePick 파킹(hold)/핸드오버 폐지 — Z는 항상
                // Avoid까지 명령하고 safe 통과 시 조기 반환(원형). 상승 중 상태는 rising 추적으로
                // 기록해 다음 die 진입이 Avoid "도착"을 기다리지 않게 한다(병목 #2 해소).
                // 현재 기준: 정상 PickUp 루프에서는 NeedleZ를 teaching 위치에 고정하고 PickerZ/EjectPinZ만 복귀한다.
                Task<int> pickerZMove = MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
                    pickerZ,
                    pickerZAvoid,
                    _targetPickerZ,
                    pickerSafeForWaferStageDistance,
                    pickerAvoidVelocity,
                    pickerAvoidAcceleration,
                    pickerAvoidDeceleration,
                    description + " PickerZ",
                    "AvoidPosition",
                    ct);

                // [사용자 승인 2026-07-26, 병목 #1] EjectPinZ가 백그라운드로 Avoid 복귀 중이면
                // 여기서 재명령/도착 대기하지 않는다 — 완료된 경우에만 결과를 회수하고,
                // 이동 중이면 다음 die XY 게이트에서 join한다.
                bool ejectPinInBackground = _pickUpEjectPinAvoidTask != null && !_pickUpEjectPinAvoidTask.IsCompleted;
                Task<int> ejectPinZMove;
                if (_pickUpEjectPinAvoidTask != null)
                    ejectPinZMove = ejectPinInBackground ? Task.FromResult(0) : _pickUpEjectPinAvoidTask;
                else
                    ejectPinZMove = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid)
                        ? Task.FromResult(0)
                        : MoveInputStageAxisCommandAsync(
                            stage,
                            WaferStageAxis.EjectPinZ,
                            ejectPinZAvoid,
                            description + " EjectPinZ",
                            ct);

                int[] results = await Task.WhenAll(pickerZMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-Z-EJECT-AVOID-KEEP-NEEDLE", Name,
                        description + " 실패. " +
                        "pickerZResult=" + results[0] +
                        ", ejectPinZResult=" + results[1] +
                        ", " + BuildPickerAxisState(pickerZ, pickerZAvoid) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                        ", needleKeep=" + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTeachingTarget));
                }

                int check;
                if (!ejectPinInBackground)
                {
                    int ejectResult = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.EjectPinZ,
                        ejectPinZAvoid,
                        description + " EjectPinZ",
                        ct).ConfigureAwait(false);
                    if (ejectResult != 0)
                        return ejectResult;

                    check = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid, description + " EjectPinZ");
                    if (check != 0)
                        return check;

                    WriteLog("PickerPickUpZ",
                        Name + " Needle Vacuum OFF 후 EjectPinZ Avoid 완료 확인. " +
                        "description=" + description +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                        " - Ok");
                }

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, needleTeachingTarget, description + " NeedleZ teaching 유지");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " " + description + ". NeedleZ teaching 유지, " +
                    BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTeachingTarget) +
                    " - Ok");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-EJECT-AVOID-KEEP-NEEDLE-EX", Name,
                    description + " 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

    }
}
