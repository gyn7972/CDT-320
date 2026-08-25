using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Recipes;
using QMC.Common.Motion;
using QMC.Common;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal sealed partial class PickerPlaceSequence
    {
        private async Task<int> PublishOutputStageExchangeReadyAfterSafeCompletionAsync(CancellationToken ct)
        {
            try
            {
                if (!MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide))
                    return 0;

                if (!_currentPlaceZSafeReturnCompleted || !IsCurrentPickerAtFullAvoidPosition())
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-UNSAFE", Name,
                        "OutputStage 마지막 Place 교체 준비 신호를 발행할 수 없습니다. " +
                        "마지막 Place Picker 전체 Avoid 복귀가 완료되지 않았습니다. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                if (Context == null || Context.Bus == null)
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-BUS", Name,
                        "OutputStage 마지막 Place 교체 준비 신호를 발행할 Bus가 없습니다. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                if (Context.OutputPostPlaceInspections != null)
                {
                    int idleResult = await Context.OutputPostPlaceInspections.WaitUntilIdleAsync(
                        "OutputStageExchangeReady:" + Side + ":" + _currentOutputSide,
                        0,
                        ct).ConfigureAwait(false);
                    if (idleResult != 0)
                    {
                        return Fail("PICKER-PLACE-STAGE-COMPLETE-INSPECTION", Name,
                            "OutputStage 마지막 Place 후검사 완료 대기 실패. " +
                            "side=" + Side + ", outputSide=" + _currentOutputSide +
                            ", pickerNo=" + _currentPickerNo + ", result=" + idleResult);
                    }
                }

                if (!MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide) ||
                    !IsCurrentPickerAtFullAvoidPosition())
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-FINAL-CHECK", Name,
                        "OutputStage 교체 준비 신호 직전 최종 안전 확인 실패. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                string signal = _currentOutputSide == BinSide.Ng
                    ? "OutputNgStageReceiveComplete"
                    : "OutputGoodStageReceiveComplete";

                Context.Bus.Set(signal);
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 마지막 Place 후 Picker 전체 Avoid 및 후검사 완료를 확인하고 교체 준비 신호를 발행했습니다. " +
                    "side=" + _currentOutputSide + ", signal=" + signal +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-STAGE-COMPLETE-NOTIFY", Name,
                    "OutputStage 교체 준비 신호 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsCurrentPickerAtFullAvoidPosition()
        {
            PickerAxis[] axes =
            {
                PickerAxis.PickerX,
                PickerAxis.PickerY,
                PickerAxis.PickerT0,
                PickerAxis.PickerT1,
                PickerAxis.PickerT2,
                PickerAxis.PickerT3,
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3
            };

            for (int i = 0; i < axes.Length; i++)
            {
                PickerAxis axis = axes[i];
                if (!IsPickerAxisInPosition(axis, GetPickerTeachingPosition(axis, "AvoidPosition")))
                    return false;
            }

            return true;
        }

        private int SelectNextPickerOrComplete()
        {
            _pickerCursor++;

            // [Good 선배출·NG 유예 2026-08-25 팀장님 지시] 활성 목록은 패스에 따라 다르다
            // (Good 패스=적재 픽커 전체, NG 패스=유예 목록).
            IList<int> activePickerList = _placeRoutingPass == BinSide.Ng
                ? _deferredNgPickerIndexes
                : (IList<int>)_pickedPickerIndexes;
            if (_pickerCursor >= activePickerList.Count)
            {
                // Good 패스 소진 후 유예 NG가 있으면 배치를 끝내지 않고 스테이지 전환
                // (전 픽커 Z Avoid 검증 + VisionX 재계산 후퇴 + Good 정리 + NgY 선행 정렬)
                // 1회를 거쳐 NG 패스로 이어간다.
                if (_placeRoutingPass == BinSide.Good && _deferredNgPickerIndexes.Count > 0)
                {
                    _placeRoutingPass = BinSide.Ng;
                    _pickerCursor = 0;
                    WriteLog("PickerPlaceSequence",
                        Name + " Good 선배출 패스 완료 — 유예 NG 패스로 전환합니다. " +
                        "goodPlaced=" + _goodPassPlacedCount +
                        ", deferredNgCount=" + _deferredNgPickerIndexes.Count + " - Check");
                    CurrentStep = PickerPlaceStep.TransitionOutputStageForNgPass;
                    return 0;
                }

                CurrentStep = PickerPlaceStep.MovePickerToAvoidAfterPlace;
                return 0;
            }

            CurrentStep = PickerPlaceStep.SelectNextPicker;
            return 0;
        }

        private async Task<int> MovePickerToAvoidAfterPlaceAsync(CancellationToken ct)
        {
            try
            {
                // 현재 기준(사용자 지시 2026-07-26): 후검사 핸드오버를 X 복귀 완료가 아니라
                // "Z/Y 복귀 완료 직후(X −방향 복귀 시작 전)"으로 앞당긴다 — 픽커가 복귀하는 동안
                // 후검사 워커가 스테이지 정렬을 진행하고 OutputVisionX가 퇴장 픽커를 따라 진입한다.
                // 물리 안전 무변경: 스테이지 Z/Y 인터락(픽커 존 미검사), 비전 진입의 공유레일
                // 클리어 대기/return-follow/제3분기 페어 간격이 그대로 담당한다. 존 lease를
                // 물리 퇴장 전에 반환하는 것은 픽업→바텀 전환의 확립된 관례와 동일하다.
                int result = await MovePickerToAvoidAfterPlaceFastAsync(
                    "Place 완료 후 Picker 전체 Avoid 복귀",
                    ct,
                    HandOverToPostPlaceInspectionBeforeFinalReturn).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 아래 반환/종료 호출은 전부 멱등 — 핸드오버가 이미 수행했으면 무동작(백스톱).
                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                ReleaseOutputFeederArea();
                EndOutputPostPlaceInspectionBatch();

                int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                    "Place 완료 후 후검사 대기 전");
                if (workZoneReleaseResult != 0)
                    return workZoneReleaseResult;

                int completionResult = await PublishOutputStageExchangeReadyAfterSafeCompletionAsync(ct).ConfigureAwait(false);
                if (completionResult != 0)
                    return completionResult;

                CurrentStep = PickerPlaceStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-AVOID-EX", Name,
                    "Place 완료 후 Picker Avoid 복귀 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(string description)
        {
            if (_parentOutputWorkZoneReleaseNotified)
                return 0;

            Func<string, bool> release = ReleaseParentOutputWorkZoneAfterSafeAvoid;
            if (release == null)
                return 0;

            try
            {
                bool released = release(description);
                if (!released)
                {
                    return Fail("PICKER-PLACE-PARENT-WORK-ZONE-RELEASE", Name,
                        "Place 완료 후 Output camera 후검사 대기 전 부모 Picker Output 작업영역을 해제하지 못했습니다. " +
                        "side=" + Side + ", description=" + description);
                }

                _parentOutputWorkZoneReleaseNotified = true;
                WriteLog("PickerPlaceSequence",
                    Name + " Picker 전체 Avoid 확인 후 Output camera 후검사 대기 전에 " +
                    "부모 Picker Output 작업영역을 해제했습니다. side=" + Side +
                    ", description=" + description + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-PARENT-WORK-ZONE-RELEASE-EX", Name,
                    "Place 완료 후 부모 Picker Output 작업영역 해제 중 예외가 발생했습니다. " +
                    "side=" + Side + ", description=" + description +
                    ", error=" + ex.Message);
            }
        }

        // 현재 기준(사용자 지시 2026-07-26): Z/Y 복귀 완료 직후(X 복귀 시작 전) 후검사 핸드오버 —
        // 영역(OutputPlace/Stage/Feeder) 반환 + 후검사 워커 기동 + 부모 Output 존 반환.
        // 전 호출이 멱등이라 완료 지점의 기존 백스톱 호출과 중복 실행돼도 무해하다.
        private int HandOverToPostPlaceInspectionBeforeFinalReturn()
        {
            ReleaseOutputPlaceArea();
            ReleaseOutputStageArea();
            ReleaseOutputFeederArea();
            EndOutputPostPlaceInspectionBatch();

            int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                "Place Z/Y 복귀 후 X 복귀 전 후검사 핸드오버");
            if (workZoneReleaseResult != 0)
                return workZoneReleaseResult;

            WriteLog("PickerPlaceSequence",
                Name + " Z/Y 복귀 완료 — X 복귀 전 후검사 핸드오버 완료(영역 반환+워커 기동). " +
                "OutputVisionX가 퇴장 픽커와 겹쳐 진입할 수 있습니다. side=" + Side + " - Ok");
            return 0;
        }

        private async Task<int> MovePickerToAvoidAfterPlaceFastAsync(string description, CancellationToken ct)
        {
            return await MovePickerToAvoidAfterPlaceFastAsync(description, ct, null).ConfigureAwait(false);
        }

        // [사용자 승인 2026-07-27] Z Avoid 완료 확인 — 재명령 금지 원칙(AXM 0x1038 사고 재발 방지):
        // ①백그라운드 상승 태스크가 있으면 결과를 회수(join) ②각 Z: 이동 중이면 정지까지 대기
        // ③정지 후 Avoid가 아니면 그때만 동기 Avoid 이동(정지 상태라 재명령 안전).
        // 전체 타임아웃 5초(사용자 지정).
        private const int PlaceZAvoidJoinTimeoutMs = 5000;

        private async Task<int> JoinAllPickerZAtAvoidWithRecoveryAsync(string description, CancellationToken ct)
        {
            DateTime timeoutAt = DateTime.UtcNow.AddMilliseconds(PlaceZAvoidJoinTimeoutMs);

            Task<int> riseTask = _pendingContiRetreatRiseTask;
            _pendingContiRetreatRiseTask = null;
            if (riseTask != null && !riseTask.IsCompleted)
            {
                Task finished = await Task.WhenAny(riseTask, Task.Delay(PlaceZAvoidJoinTimeoutMs, ct)).ConfigureAwait(false);
                if (finished != riseTask)
                {
                    return Fail("PICKER-PLACE-Z-JOIN-TIMEOUT", Name,
                        description + " 실패: Z Avoid 상승 태스크가 " + PlaceZAvoidJoinTimeoutMs +
                        "ms 안에 완료되지 않았습니다.");
                }
            }
            if (riseTask != null && riseTask.IsCompleted)
            {
                int riseResult = await riseTask.ConfigureAwait(false);
                if (riseResult != 0)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description +
                        " - 상승 태스크 결과가 실패였습니다. 위치 확인/복구로 계속합니다. result=" + riseResult + " - Check");
                }
            }

            PickerAxis[] zAxes =
            {
                PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3
            };
            foreach (PickerAxis zKind in zAxes)
            {
                BaseAxis zAxis = GetPickerAxis(zKind);
                if (zAxis == null)
                    continue;

                // 이동 중이면 정지까지 대기(10ms 폴링) — 재명령 금지.
                while (zAxis.IsMoving)
                {
                    ct.ThrowIfCancellationRequested();
                    if (DateTime.UtcNow > timeoutAt)
                    {
                        return Fail("PICKER-PLACE-Z-JOIN-TIMEOUT", Name,
                            description + " 실패: " + zKind + " 이동 정지 대기 타임아웃(" +
                            PlaceZAvoidJoinTimeoutMs + "ms). " +
                            BuildPickerAxisState(zKind, GetPickerTeachingPosition(zKind, "AvoidPosition")));
                    }
                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                double avoid = GetPickerTeachingPosition(zKind, "AvoidPosition");
                if (IsPickerAxisInPosition(zKind, avoid))
                    continue;

                // 정지 + 비Avoid → 이때만 동기 Avoid 이동(복구).
                int moveResult = await MovePickerAxisAndVerifyAsync(
                    zKind,
                    avoid,
                    description + " - " + zKind + " Avoid 복구 이동",
                    ct,
                    "AvoidPosition").ConfigureAwait(false);
                if (moveResult != 0)
                    return moveResult;
            }

            return 0;
        }

        // [동적 대기점 직행, 사용자 승인 2026-07-27] Place 복귀 X 목표를 동적 선행 대기점으로
        // 치환할 수 있으면 ref 인자를 갱신한다. 모든 실패/미충족은 로그 후 기존 고정 Avoid 유지
        // (알람 없음, fail-safe). 스위치 Off는 로그 없이 기존과 완전 동일(R3 원칙).
        private void TryApplyPlaceReturnDynamicWaitTarget(
            ref double pickerXReturnTarget,
            ref string xtTargetName,
            double pickerXAvoid)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto || Options.PickerMotionOnlyTestMode)
                    return;
                if (Context != null && Context.IsCycleStopRequested)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " 정상 Cycle Stop 요청 중이므로 Place 복귀 동적 대기점을 사용하지 않고 고정 X Avoid로 복귀합니다. " +
                        "side=" + Side + " - Check");
                    return;
                }

                PickerPickUpMotionConfig pickUpConfig = Side == PickerSequenceSide.Front
                    ? (FrontPicker != null && FrontPicker.Config != null ? FrontPicker.Config.PickUp : null)
                    : (RearPicker != null && RearPicker.Config != null ? RearPicker.Config.PickUp : null);
                if (pickUpConfig == null)
                    return;
                pickUpConfig.Ensure();

                if (!pickUpConfig.PickUpDynamicWaitMode)
                    return;
                if (pickUpConfig.TransferMotionMode != PickerPickUpTransferMotionMode.ContiSegmentedPickUp)
                    return;

                // [검증 P5] 출력 스테이지 수령 완료 상태면 교체 준비 신호 발행이 픽커 "전축 고정
                // Avoid"를 요구한다(PublishOutputStageExchangeReadyAfterSafeCompletionAsync의
                // IsCurrentPickerAtFullAvoidPosition 검사) — 직행하면 X가 Avoid를 벗어나
                // PICKER-PLACE-STAGE-COMPLETE-UNSAFE로 확정 실패하므로, 이 경우는 직행을 스킵하고
                // 기존 고정 Avoid로 복귀한다(OutputStageReady의 Full 대기 경로 포함 동일 게이트).
                if (MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide))
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 복귀 동적 대기점 직행 스킵(출력 스테이지 수령 완료 — 교체 준비 발행 예정, 고정 Avoid 복귀). " +
                        "outputSide=" + _currentOutputSide + " - Check");
                    return;
                }

                DynamicPickUpWaitTarget resolved;
                string failReasonKey;
                string failDetail;
                if (!TryResolveDynamicPickUpWaitTargetX(pickUpConfig, out resolved, out failReasonKey, out failDetail))
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 복귀 동적 대기점 직행 스킵(고정 Avoid 복귀). reason=" + failReasonKey +
                        ", detail=" + (failDetail ?? "-") + " - Check");
                    return;
                }

                // 진짜 단축일 때만: 대기점이 고정 Avoid보다 접근 방향으로 앞서야 한다.
                bool beyondAvoid = resolved.Direction > 0
                    ? resolved.WaitX > pickerXAvoid + 0.5
                    : resolved.WaitX < pickerXAvoid - 0.5;
                if (!beyondAvoid)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 복귀 동적 대기점 직행 스킵(단축 없음, 고정 Avoid 복귀). waitX=" +
                        resolved.WaitX.ToString("F3") +
                        ", fixedAvoid=" + pickerXAvoid.ToString("F3") + " - Check");
                    return;
                }

                // MotionGuard 전 규칙 dry-run(알람 없는 판정) — 기존 PlaceDoneSafeXT 토큰은 유지하고
                // 명시 PickerZone=Input 토큰만 추가한다(유닛은 명시 토큰 존재 시 재부착 안 함).
                string directName = "AvoidPosition;PickerPhase=PlaceDoneSafeXT;PickerZone=Input";
                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                string dryRunName = (Side == PickerSequenceSide.Front ? "FrontPicker" : "RearPicker") +
                                    ";PickerX;" + directName;
                string guardReason = "PickerX 축 참조 없음";
                if (pickerX == null ||
                    !MotionGuardRuntime.CanAxisTeachingMove(pickerX, resolved.WaitX, dryRunName, out guardReason))
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 복귀 동적 대기점 직행 스킵(가드 dry-run 차단, 고정 Avoid 복귀). reason=" +
                        (guardReason ?? "-") + " - Check");
                    return;
                }

                pickerXReturnTarget = resolved.WaitX;
                xtTargetName = directName;
                WriteLog("PickerPlaceSequence",
                    Name + " Place 복귀 X를 동적 선행 대기점으로 직행합니다. waitX=" + resolved.WaitX.ToString("F3") +
                    ", fixedAvoid=" + pickerXAvoid.ToString("F3") +
                    ", shortcut=" + Math.Abs(pickerXAvoid - resolved.WaitX).ToString("F3") +
                    ", constraintVisionX=" + resolved.ConstraintVisionX.ToString("F3") +
                    ", batchVisionXRange=" + resolved.MinVisionX.ToString("F3") + "~" + resolved.MaxVisionX.ToString("F3") +
                    ", dieCount=" + resolved.DieCount +
                    ", homeGap=" + resolved.HomeGap.ToString("F3") +
                    ", safetyGap=" + resolved.SafetyGap.ToString("F3") +
                    ", extraMargin=" + resolved.ExtraMargin.ToString("F3") +
                    ", direction=" + resolved.Direction + " - Start");
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place 복귀 동적 대기점 직행 판정 중 예외(고정 Avoid 복귀 유지). error=" +
                    ex.Message + " - Check");
            }
        }

        private async Task<int> MovePickerToAvoidAfterPlaceFastAsync(
            string description,
            CancellationToken ct,
            Func<int> beforeFinalReturnHandover)
        {
            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
            {
                return await MovePickerToAvoidAfterPlaceFastCoreAsync(
                    description,
                    ct,
                    beforeFinalReturnHandover).ConfigureAwait(false);
            }

            // 정상 STOP이 Y Avoid 직후 들어와도 X/T 최종 복귀가 Cycle Stop 경계에서 절단되지 않게 한다.
            // 안전 후퇴 보호는 정지 경계만 보류하며 기존 MotionGuard/SharedRailX/대향 PickerY 검사는 유지한다.
            return await RunSafetyRetreatMoveAsync(
                () => MovePickerToAvoidAfterPlaceFastCoreAsync(
                    description,
                    ct,
                    beforeFinalReturnHandover)).ConfigureAwait(false);
        }

        private async Task<int> MovePickerToAvoidAfterPlaceFastCoreAsync(
            string description,
            CancellationToken ct,
            Func<int> beforeFinalReturnHandover)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // [사용자 승인 2026-07-27] PLACE ENTRY Z PREDOWN 스위치 기준 정리 순서:
                //  - True: Z Avoid 도착 확인/재명령 없이 Y 먼저 이동(Z 잔여 상승과 병렬),
                //          X 이동 직전에 join+복구로 Z Avoid 완료를 보장.
                //  - False: join+복구로 Z Up 완료를 먼저 확인한 뒤 Y → X 순차.
                // 기존 MoveAllPickerZToAvoid(이동 중 재명령 → AXM 0x1038 알람)는 폐지.
                PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
                bool zPreDown = placeConfig != null && placeConfig.PlaceEntryZPreDownMode;

                int result;
                if (!zPreDown)
                {
                    result = await JoinAllPickerZAtAvoidWithRecoveryAsync(
                        description + " Z축 Avoid 완료 확인(선행)",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                    ClearPendingContiRetreat();
                }

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    description + " Y축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=PlaceDoneSafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 현재 기준(사용자 지시 2026-07-26): Y 복귀까지 끝난 시점 — X −방향 복귀 전에
                // 후검사 핸드오버(있으면)를 실행해 비전 진입/스테이지 정렬이 복귀와 겹치게 한다.
                if (beforeFinalReturnHandover != null)
                {
                    int handoverResult = beforeFinalReturnHandover();
                    if (handoverResult != 0)
                        return handoverResult;
                }

                if (zPreDown)
                {
                    result = await JoinAllPickerZAtAvoidWithRecoveryAsync(
                        description + " X 이동 전 Z축 Avoid join",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                    ClearPendingContiRetreat();
                }

                // [동적 대기점 직행, 사용자 승인 2026-07-27] Auto+Conti+스위치 On이고 다음 픽업
                // 배치 좌표가 공개돼 있으면 X 복귀 목표를 고정 Avoid 대신 동적 선행 대기점으로
                // 직행시킨다(고정점 경유 왕복 제거). 비전X 충돌 방지: 대기점 = 배치 구속 극값 +
                // 팔로잉 클리어런스(접근 내내 간격 ≥ requiredGap, FollowMove 경계식과 동일 항등식)
                // + 발행 전 MotionGuard 전 규칙 dry-run — 하나라도 안 되면 기존 고정 Avoid 복귀.
                double pickerXAvoid = GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");
                double pickerXReturnTarget = pickerXAvoid;
                string xtTargetName = "AvoidPosition;PickerPhase=PlaceDoneSafeXT";
                TryApplyPlaceReturnDynamicWaitTarget(ref pickerXReturnTarget, ref xtTargetName, pickerXAvoid);

                // 동적 목표 계산 직후 STOP이 경합한 경우에도 이번 복귀의 최종 목표를 고정 Avoid로 확정한다.
                if (Context != null && Context.IsCycleStopRequested)
                {
                    pickerXReturnTarget = pickerXAvoid;
                    xtTargetName = "AvoidPosition;PickerPhase=PlaceDoneSafeXT";
                }

                var tTargets = new Dictionary<PickerAxis, double>();
                tTargets[PickerAxis.PickerT0] = GetPickerTeachingPosition(PickerAxis.PickerT0, "AvoidPosition");
                tTargets[PickerAxis.PickerT1] = GetPickerTeachingPosition(PickerAxis.PickerT1, "AvoidPosition");
                tTargets[PickerAxis.PickerT2] = GetPickerTeachingPosition(PickerAxis.PickerT2, "AvoidPosition");
                tTargets[PickerAxis.PickerT3] = GetPickerTeachingPosition(PickerAxis.PickerT3, "AvoidPosition");
                tTargets[PickerAxis.PickerX] = pickerXReturnTarget;

                result = await MovePickerAxesAndVerifyAsync(
                    tTargets,
                    description + " X/T축 병렬 Avoid",
                    ct,
                    xtTargetName).ConfigureAwait(false);
                if (result != 0)
                    return result;

                PickerAxis[] finalAxes =
                {
                    PickerAxis.PickerX,
                    PickerAxis.PickerY,
                    PickerAxis.PickerT0,
                    PickerAxis.PickerT1,
                    PickerAxis.PickerT2,
                    PickerAxis.PickerT3,
                    PickerAxis.PickerZ0,
                    PickerAxis.PickerZ1,
                    PickerAxis.PickerZ2,
                    PickerAxis.PickerZ3
                };

                foreach (PickerAxis axis in finalAxes)
                {
                    // 직행 시 X의 최종 확인 기준은 실제 이동 목표(동적 대기점)다.
                    double target = axis == PickerAxis.PickerX
                        ? pickerXReturnTarget
                        : GetPickerTeachingPosition(axis, "AvoidPosition");
                    if (!IsPickerAxisInPosition(axis, target))
                    {
                        return Fail("PICKER-PLACE-AVOID-FINAL-POS", Name,
                            description + " 최종 Avoid 위치 확인 실패. " +
                            BuildPickerAxisState(axis, target));
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-AVOID-SEQ-EX", Name,
                    description + " 안전 순서 Avoid 복귀 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildOutputStagePlaceMoveTargetName(string outputStageStep)
        {
            return BuildPlaceMoveTargetName() + ";OutputStageStep=" + outputStageStep;
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct)
        {
            return await MoveOutputStageAxisAndVerifyAsync(axis, target, description, ct, null, false).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct, string targetName)
        {
            return await MoveOutputStageAxisAndVerifyAsync(axis, target, description, ct, targetName, false).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(
            BinStageAxis axis,
            double target,
            string description,
            CancellationToken ct,
            string targetName,
            bool forceMove)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool logResumePlaceStageMove = description != null &&
                    description.IndexOf("Place 재시작", StringComparison.Ordinal) >= 0;
                if (logResumePlaceStageMove)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description + " 이동 시작. axis=" + axis +
                        ", target=" + target +
                        ", targetName=" + (targetName ?? "-") +
                        ", forceMove=" + forceMove +
                        ", " + OutputStage.BuildStageAxisState(axis, target) +
                        " - Start");
                }

                int result = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveStageAxis(axis, target, Options.FineMove, targetName, forceMove),
                    ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail("PICKER-PLACE-STAGE-MOVE", "OutputStage",
                        description + " move command failed. result=" + result + ". " +
                        OutputStage.BuildStageAxisState(axis, target));
                }

                // 기존 조건: 이동 후 재대기 + 스냅샷 최종 확인 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3/R4).
                double tolerance = ResolveOutputStageAxisTolerance(axis);

                if (logResumePlaceStageMove)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description + " 이동 완료. 이후 Picker X/T 완료 확인 후에만 PickerY 전진을 허용합니다. axis=" + axis +
                        ", target=" + target +
                        ", tolerance=" + tolerance +
                        ", targetName=" + (targetName ?? "-") +
                        ", forceMove=" + forceMove +
                        ", " + OutputStage.BuildStageAxisState(axis, target) +
                        " - Ok");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-STAGE-EX", "OutputStage", description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private static bool CanSkipOutputFeederMoveCommand(OutputFeederUnit feeder, double target)
        {
            BaseAxis axis = feeder != null ? feeder.FeederY : null;
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return axis.IsAtTargetPosition(target, tolerance);
        }

        private double ResolveOutputStageAxisTolerance(BinStageAxis axis)
        {
            try
            {
                BaseAxis item = null;

                switch (axis)
                {
                    case BinStageAxis.GoodBinY:
                        item = OutputStage != null && OutputStage.GoodStage != null ? OutputStage.GoodStage.StageY : null;
                        break;
                    case BinStageAxis.GoodBinZ:
                        item = OutputStage != null && OutputStage.GoodStage != null ? OutputStage.GoodStage.StageZ : null;
                        break;
                    case BinStageAxis.NgBinY:
                        item = OutputStage != null && OutputStage.NgStage != null ? OutputStage.NgStage.StageY : null;
                        break;
                    case BinStageAxis.VisionX:
                        item = OutputStage != null ? OutputStage.OutputCameraX : null;
                        break;
                }

                if (item != null && item.Config != null && item.Config.InPositionTolerance > 0.0)
                    return item.Config.InPositionTolerance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.05;
        }

        private async Task<T> AwaitStepWithCancellationAsync<T>(Task<T> task, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAsync(task, default(T), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
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

            return 5; //100
        }

        private void ReleaseOutputStageArea()
        {
            try
            {
                if (_outputStageLease == null)
                    return;

                _outputStageLease.Dispose();
                _outputStageLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputStageArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ReleaseOutputPlaceArea()
        {
            try
            {
                ReleasePickerWorkArea();

                if (_outputPlaceLease == null)
                    return;

                _outputPlaceLease.Dispose();
                _outputPlaceLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputPlaceArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ReleaseOutputFeederArea()
        {
            try
            {
                if (_outputFeederLease == null)
                    return;

                _outputFeederLease.Dispose();
                _outputFeederLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputFeederArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void BeginOutputPostPlaceInspectionBatch()
        {
            try
            {
                if (_outputInspectBatchOpen)
                    return;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.BeginBatch(Name);
                _outputInspectBatchOpen = true;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 시작 처리 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void EndOutputPostPlaceInspectionBatch()
        {
            try
            {
                if (!_outputInspectBatchOpen)
                    return;

                _outputInspectBatchOpen = false;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.EndBatch(Name);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 종료 처리 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void CancelOutputPostPlaceInspectionBatch(string reason)
        {
            try
            {
                if (!_outputInspectBatchOpen)
                    return;

                _outputInspectBatchOpen = false;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.CancelBatch(Name, reason);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 취소 처리 중 예외가 발생했습니다. reason=" +
                    reason + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
