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
        private async Task<int> MoveInputVisionToAvoidForPickerMoveAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                if (stage.Recipe == null)
                    return Fail("PICKER-PICKUP-STAGE-RECIPE", stage.Name, "InputStage recipe is null.");

                stage.Recipe.EnsurePositionObjects();
                double avoid = stage.Recipe.VisionX.AvoidPosition;
                double target = avoid;
                string retreatDetail = "PickUp 목표 좌표가 없어 전체 Avoid를 사용합니다.";
                bool targetsCalculated = ArePickBatchTargetsCalculated();

                // Conti 게이트: Auto + ContiSegmentedPickUp이면 부호 인지 최소 회피(Extra 포함),
                // 미충족이면 기존 경로(-0.1/1.0) 그대로 (동작 무변경).
                PickerPickUpMotionConfig retreatPickUpConfig = ResolvePickUpMotionConfig();
                bool useMinimalRetreat =
                    Options != null && Options.RunMode == SequenceRunMode.Auto &&
                    retreatPickUpConfig != null &&
                    IsCoordinatedPickUpTransferMotionMode(retreatPickUpConfig.TransferMotionMode);
                string retreatMode = useMinimalRetreat ? "minimal" : "legacy";

                if (targetsCalculated)
                {
                    SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                        Context != null ? Context.Machine : null);
                    if (service != null)
                    {
                        var planned = new Dictionary<SharedRailXAxis, IList<double>>();
                        SharedRailXAxis pickerRailAxis = Side == PickerSequenceSide.Front
                            ? SharedRailXAxis.FrontPickerX
                            : SharedRailXAxis.RearPickerX;
                        var pickerTargets = new List<double>();
                        for (int i = 0; i < _pickBatchItems.Count; i++)
                            pickerTargets.Add(_pickBatchItems[i].TargetPickerX);
                        planned[pickerRailAxis] = pickerTargets;

                        double dynamicTarget;
                        string dynamicDetail;
                        // 회피 목표 마진(사용자 승인 2026-07-26, 4번): Extra에 +1mm — 최심 픽 목표와의
                        // 최종 간격이 팔로잉 safetyGap과 정확히 같아지는 경계치 해소(팔로잉 gap은 무변경).
                        bool resolved = useMinimalRetreat
                            ? service.TryResolveMinimalVisionRetreatTarget(
                                stage.CameraX,
                                avoid,
                                planned,
                                (service.Config != null ? service.Config.InputVisionRetreatExtraClearance : 40.0) +
                                VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm,
                                out dynamicTarget,
                                out dynamicDetail)
                            : service.TryResolveNearestVisionRetreatTarget(
                                stage.CameraX,
                                avoid,
                                -0.1,
                                planned,
                                1.0,
                                out dynamicTarget,
                                out dynamicDetail);
                        if (resolved)
                        {
                            target = dynamicTarget;
                            retreatDetail = dynamicDetail;
                        }
                        else
                        {
                            retreatDetail = dynamicDetail + " 전체 Avoid로 대체합니다.";
                        }
                    }
                }

                _inputVisionPickerEntryTarget = target;
                _inputVisionPickerEntryTargetPrepared = true;
                WriteLog("PickerPickUpSequence",
                    Name + " InputVisionX 피커 진입 회피 좌표를 확정했습니다. " +
                    "mode=" + retreatMode +
                    ", target=" + target.ToString("F6") +
                    ", fullAvoid=" + avoid.ToString("F6") +
                    ", batchPickerX=" + string.Join(",", _pickBatchItems.ConvertAll(x => x.TargetPickerX.ToString("F6")).ToArray()) +
                    ", detail=" + retreatDetail + " - Check");

                // 기존 조건(사용자 지시 2026-07-25): 이동 명령을 발행하지 않고 이연 플래그만 세워
                //   피커 X 진입 직전에 기동했다 — 비전이 촬영 위치에 홀드되는 손해가 실측됐다.
                // 현재 기준(사용자 지시 2026-07-26, 이연 폐지): 외부(선행검사) 독립 회피가 진행 중이면
                //   (a) 목표가 배치 전 목표와 페어 간격을 만족하면 그 Task/목표를 인수하고,
                //   (b) 목표가 부족하면 "외부 join → 정확 좌표 연장 회피" 합성 Task를 지금 기동한다.
                //   (c) 외부 회피가 없으면 자체 회피를 지금 즉시 비동기 기동한다.
                //   이동 명령 경로(SharedRailX 중재 경유)와 join 지점(피커 X 진입 완료 전)은 기존과 동일하다.
                bool startRetreatAsync = useMinimalRetreat && targetsCalculated;
                bool retreatRunningAsync = false;
                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, target))
                {
                    if (startRetreatAsync)
                    {
                        await JoinInputVisionRetreatMoveTaskAsync("새 회피 기동 전 이전 Task 정리", ct).ConfigureAwait(false);

                        Task<int> externalRetreatTask;
                        double externalRetreatTarget;
                        bool externalTaskAlreadyCompleted;
                        if (VisionIndependentRetreatCoordinator.TryAdoptInput(
                            Side, out externalRetreatTask, out externalRetreatTarget, out externalTaskAlreadyCompleted))
                        {
                            if (IsInputVisionTargetClearOfBatchPickerTargets(stage, externalRetreatTarget))
                            {
                                // 현재 기준(사용자 승인 2026-07-26, 3번): 인수 Task를 그대로 쓰지 않고
                                // "외부 Task join → 실위치 도착 대기" 합성으로 감싼다 — 이동 Task가
                                // 실제 도착 전에 완료 상태가 되어도 join이 곧 도착을 의미하게 된다.
                                _inputVisionPickerEntryTarget = externalRetreatTarget;
                                _inputVisionRetreatMoveTask = AwaitExternalInputVisionArrivalAsync(
                                    externalRetreatTask, stage, externalRetreatTarget, ct);
                                WriteLog("PickerPickUpSequence",
                                    Name + " 선행검사 독립 회피를 인수합니다(목표 충분 — 추가 이동 없음). " +
                                    "externalTarget=" + externalRetreatTarget.ToString("F6") +
                                    ", exactTarget=" + target.ToString("F6") +
                                    ", externalTaskCompleted=" + externalTaskAlreadyCompleted + " - Ok");
                            }
                            else
                            {
                                _inputVisionRetreatMoveTask = ExtendExternalInputVisionRetreatAsync(
                                    externalRetreatTask, stage, externalRetreatTarget, target, ct);
                                WriteLog("PickerPickUpSequence",
                                    Name + " 선행검사 독립 회피를 인수하고 정확 좌표 연장 회피를 합성 기동했습니다(목표 부족). " +
                                    "externalTarget=" + externalRetreatTarget.ToString("F6") +
                                    ", exactTarget=" + target.ToString("F6") +
                                    ", externalTaskCompleted=" + externalTaskAlreadyCompleted + " - Start");
                            }
                        }
                        else
                        {
                            _inputVisionRetreatMoveTask = RunInputVisionRetreatMoveAsync(stage, target, ct);
                            WriteLog("PickerPickUpSequence",
                                Name + " InputVisionX 최소 회피 이동을 즉시 비동기 시작했습니다(외부 독립 회피 없음). " +
                                "target=" + target.ToString("F6") + " - Start");
                        }

                        retreatRunningAsync = true;
                    }
                    else
                    {
                        int result = await MoveInputStageAxisCommandAsync(
                            stage,
                            WaferStageAxis.VisionX,
                            target,
                            "InputVisionX 최소 회피 위치 이동",
                            ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;

                        result = await WaitInputStageAxisInPositionResultAsync(
                            stage,
                            WaferStageAxis.VisionX,
                            target,
                            "InputVisionX 최소 회피 위치 이동",
                            ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }
                }

                // 비동기 회피 진행 중에는 비전이 아직 회피 전일 수 있으므로 인포지션 최종 확인을 건너뛴다.
                // (완료 확인은 피커 X 진입 경로의 join이 담당 — 기존 join 지점 무변경.)
                if (!retreatRunningAsync)
                {
                    int checkResult = CheckInputStageAxisInPosition(
                        stage,
                        WaferStageAxis.VisionX,
                        target,
                        "InputVisionX 최소 회피 위치 이동");
                    if (checkResult != 0)
                        return checkResult;
                }

                CurrentStep = targetsCalculated
                    ? PickerPickUpStep.SelectNextPickTarget
                    : PickerPickUpStep.CalculatePickTargets;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-AVOID-EX", Name,
                    "InputVisionX avoid before picker move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // R3(follow-entry): 기존 3단 헬퍼(명령→인포지션 대기→최종 확인)를 그대로 합성한
        // InputVisionX 최소 회피 이동 본체 — 명령 발행 경로(SharedRailX 중재 경유)는 동기 경로와 동일하다.
        private async Task<int> RunInputVisionRetreatMoveAsync(
            InputStageUnit stage,
            double target,
            CancellationToken ct)
        {
            int result = await MoveInputStageAxisCommandAsync(
                stage,
                WaferStageAxis.VisionX,
                target,
                "InputVisionX 최소 회피 위치 이동",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitInputStageAxisInPositionResultAsync(
                stage,
                WaferStageAxis.VisionX,
                target,
                "InputVisionX 최소 회피 위치 이동",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return CheckInputStageAxisInPosition(
                stage,
                WaferStageAxis.VisionX,
                target,
                "InputVisionX 최소 회피 위치 이동");
        }

        // R3(follow-entry): 보관된 비동기 회피 Task를 join하고 필드를 비운다. 없으면 0.
        private async Task<int> JoinInputVisionRetreatMoveTaskAsync(string context, CancellationToken ct)
        {
            Task<int> moveTask = _inputVisionRetreatMoveTask;
            if (moveTask == null)
                return 0;

            _inputVisionRetreatMoveTask = null;
            try
            {
                int result = await moveTask.ConfigureAwait(false);
                if (result != 0)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " InputVisionX 비동기 최소 회피 이동이 실패로 종료되었습니다. " +
                        "context=" + (context ?? string.Empty) +
                        ", result=" + result + " - Check");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " InputVisionX 비동기 최소 회피 이동이 취소 상태로 정리되었습니다. " +
                    "context=" + (context ?? string.Empty) + " - Check");
                ct.ThrowIfCancellationRequested();
                return -1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " InputVisionX 비동기 최소 회피 이동 정리 중 예외가 발생했습니다. " +
                    "context=" + (context ?? string.Empty) +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
        }

        // 현재 기준(사용자 지시 2026-07-26): 외부(선행검사) 독립 회피 인수 시 목표 충분성 판정 —
        // 외부 회피 목표가 배치 전 피커 X 목표와 페어 간격(SafetyDistance, RetreatExtra 미포함)을
        // 만족하면 추가 이동 없이 인수한다. IsInputVisionParkedClearOfBatchPickerTargets와 동일
        // 기준이되 현재 위치가 아닌 "도착 예정 목표"로 판정한다.
        private bool IsInputVisionTargetClearOfBatchPickerTargets(InputStageUnit stage, double visionTarget)
        {
            try
            {
                if (stage == null || stage.CameraX == null || _pickBatchItems == null || _pickBatchItems.Count == 0)
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                if (service == null || pickerX == null)
                    return false;

                for (int i = 0; i < _pickBatchItems.Count; i++)
                {
                    string detail;
                    if (!service.IsPairClearanceSatisfied(
                        pickerX, _pickBatchItems[i].TargetPickerX, stage.CameraX, visionTarget, out detail))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // 현재 기준(검증 결과 반영 2026-07-26): 배경 합성 Task용 무알람 실위치 도착 대기 —
        // Place측 WaitOutputVisionXArrivalAsync 미러. 알람을 올리는 래퍼
        // (WaitInputStageAxisInPositionResultAsync → 실패 시 Fail→Error 알람)를 배경 Task에서
        // 쓰면 배경 오탐 알람이 발생하므로 여기서는 로그+음수 코드만 반환하고, 알람 판정은
        // 전경 join 지점이 담당한다. Delay에 ct를 걸지 않는다 — 시퀀스 종료 후 dispose된
        // linked CTS 토큰으로 Task.Delay가 ObjectDisposedException을 던지는 것을 방지
        // (취소는 IsCancellationRequested 값 확인으로만 반영).
        private async Task<int> WaitInputVisionXArrivalQuietAsync(
            InputStageUnit stage,
            double target,
            string description,
            CancellationToken ct)
        {
            BaseAxis visionX = stage != null ? stage.CameraX : null;
            if (visionX == null)
                return -1;

            double tolerance = visionX.Config != null && visionX.Config.InPositionTolerance > 0.0
                ? visionX.Config.InPositionTolerance
                : 0.01;
            int timeoutMs = MotionSpeedScale.ScaleDefaultTimeoutMs(ResolveTimeout());
            DateTime start = DateTime.UtcNow;
            while (true)
            {
                if (ct.IsCancellationRequested)
                    return -1;

                if (!visionX.IsMoving && visionX.IsAtTargetPosition(target, tolerance))
                    return 0;

                if ((DateTime.UtcNow - start).TotalMilliseconds >= timeoutMs)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " " + description + " 도착 대기가 시간 초과되었습니다. " +
                        "target=" + target.ToString("F6") +
                        ", actual=" + visionX.ActualPosition.ToString("F6") +
                        ", moving=" + visionX.IsMoving +
                        ", timeoutMs=" + timeoutMs + " - Failed");
                    return -3;
                }

                await Task.Delay(10).ConfigureAwait(false);
            }
        }

        // 현재 기준(검증 결과 반영 2026-07-26): 재기동 전 무알람 정지 대기 — '외부 Task 실패=축 정지'
        // 가정이 항상 참이 아니므로(-3 타임아웃/조기 완료 시 축이 여전히 주행 중일 수 있음),
        // 이동 중인 축에 AXM.MovePosition을 재발행해 보드 busy 거부→가짜 축 알람이 되는 것을 막는다.
        // 정지 확인에 실패하면 재기동 명령을 내지 않고 음수 코드를 반환한다.
        private async Task<int> WaitInputVisionXStoppedQuietAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            BaseAxis visionX = stage != null ? stage.CameraX : null;
            if (visionX == null)
                return -1;

            int timeoutMs = MotionSpeedScale.ScaleDefaultTimeoutMs(ResolveTimeout());
            DateTime start = DateTime.UtcNow;
            while (visionX.IsMoving)
            {
                if (ct.IsCancellationRequested)
                    return -1;

                if ((DateTime.UtcNow - start).TotalMilliseconds >= timeoutMs)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " " + description + " 정지 대기가 시간 초과되었습니다. 재기동 명령을 내지 않습니다. " +
                        "actual=" + visionX.ActualPosition.ToString("F6") +
                        ", timeoutMs=" + timeoutMs + " - Failed");
                    return -3;
                }

                await Task.Delay(10).ConfigureAwait(false);
            }

            return 0;
        }

        // 현재 기준(사용자 승인 2026-07-26, 3번): 인수한 독립 회피(목표 충분)의 도착 동기화 합성 —
        // 외부 Task join 후 "실위치 도착 대기"까지 확인한다. 이동 Task가 실제 축 도착 전에 완료
        // 상태가 되는 사례(실장비 오탐 3건 원인) 방어: join 결과가 0이어도 도착을 신뢰하지 않는다.
        // 보강(검증 결과 반영): 외부 실패 시에도 '축 정지'를 가정하지 않고 정지 확인 후에만
        // 재기동한다. 도착/정지 대기는 무알람 폴러 — 배경 오탐 알람 방지.
        private async Task<int> AwaitExternalInputVisionArrivalAsync(
            Task<int> externalTask,
            InputStageUnit stage,
            double externalTarget,
            CancellationToken ct)
        {
            int externalResult;
            try
            {
                externalResult = await externalTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " 인수한 선행검사 독립 회피가 취소되어 자체 재기동합니다. - Check");
                externalResult = -1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " 인수한 선행검사 독립 회피 대기 중 예외 — 자체 재기동합니다. error=" +
                    ex.Message + " - Check");
                externalResult = -1;
            }

            if (externalResult != 0)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " 인수한 선행검사 독립 회피가 실패로 종료되어 외부 목표로 자체 재기동합니다. " +
                    "externalResult=" + externalResult +
                    ", externalTarget=" + externalTarget.ToString("F6") + " - Check");
                int stopResult = await WaitInputVisionXStoppedQuietAsync(
                    stage, "인수 회피 실패 후 재기동 전", ct).ConfigureAwait(false);
                if (stopResult != 0)
                    return stopResult;

                return await RunInputVisionRetreatMoveAsync(stage, externalTarget, ct).ConfigureAwait(false);
            }

            return await WaitInputVisionXArrivalQuietAsync(
                stage,
                externalTarget,
                "인수한 독립 회피 도착 확인",
                ct).ConfigureAwait(false);
        }

        // 현재 기준(사용자 지시 2026-07-26): 외부 독립 회피 목표가 부족할 때의 합성 회피 —
        // 외부 Task 완료를 기다린 뒤 정확 좌표로 연장 회피를 수행한다.
        // 보강(사용자 승인 2026-07-26, 3번): 외부 Task가 성공(0)으로 끝났어도 실위치 도착까지
        // 확인한 뒤에 연장 명령을 낸다 — 이동 중인 축에 재명령하는 것을 방지. 외부가 실패로
        // 종료됐으면 축이 정지한 상태이므로 즉시 연장 이동이 자체 재기동 역할을 한다.
        private async Task<int> ExtendExternalInputVisionRetreatAsync(
            Task<int> externalTask,
            InputStageUnit stage,
            double externalTarget,
            double exactTarget,
            CancellationToken ct)
        {
            int externalResult;
            try
            {
                externalResult = await externalTask.ConfigureAwait(false);
                if (externalResult != 0)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " 인수한 선행검사 독립 회피가 실패로 종료되어 연장 회피가 자체 재기동합니다. " +
                        "externalResult=" + externalResult + " - Check");
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " 인수한 선행검사 독립 회피가 취소되어 연장 회피가 자체 재기동합니다. - Check");
                externalResult = -1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " 인수한 선행검사 독립 회피 대기 중 예외 — 연장 회피가 자체 재기동합니다. error=" +
                    ex.Message + " - Check");
                externalResult = -1;
            }

            if (externalResult == 0)
            {
                // 무알람 도착 대기(검증 결과 반영) — 배경 Task에서 알람 래퍼 사용 금지.
                int arrivalResult = await WaitInputVisionXArrivalQuietAsync(
                    stage,
                    externalTarget,
                    "외부 독립 회피 도착 확인(연장 전)",
                    ct).ConfigureAwait(false);
                if (arrivalResult != 0)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " 외부 독립 회피 도착 확인에 실패했지만 연장 회피가 재기동합니다. " +
                        "arrivalResult=" + arrivalResult +
                        ", externalTarget=" + externalTarget.ToString("F6") + " - Check");
                }
            }

            // 재기동 전 정지 확인(검증 결과 반영) — 이동 중 축 재명령(보드 busy→가짜 축 알람) 방지.
            int stopBeforeExtend = await WaitInputVisionXStoppedQuietAsync(
                stage, "연장 회피 재기동 전", ct).ConfigureAwait(false);
            if (stopBeforeExtend != 0)
                return stopBeforeExtend;

            return await RunInputVisionRetreatMoveAsync(stage, exactTarget, ct).ConfigureAwait(false);
        }

        #region PickUp 진입 — InputVision 선행·PickerX Follow

        // SAFETY CONTRACT:
        // - InputVisionX가 선행하고 PickerX가 후행한다. Follow 실패 시 선행 Vision Task를 합류한 뒤 일반 이동으로 폴백한다.
        // - PickerZone=Input 의도를 담는 targetName은 MotionGuard의 존 판정 계약이므로 제거하거나 일반 문자열로 바꾸지 않는다.

        // R3(follow-entry): Abort 등 동기 경로에서 회피 Task를 관찰(observe)만 하고 흘려보낸다.
        private void ObserveInputVisionRetreatMoveTaskOnAbort()
        {
            Task<int> moveTask = _inputVisionRetreatMoveTask;
            _inputVisionRetreatMoveTask = null;
            if (moveTask == null)
                return;

            moveTask.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        t.Exception.Flatten();
                },
                TaskScheduler.Default);
        }

        // 기존 조건(#17 R3): 이미 시작된 회피 Task가 살아 있고 비전이 이동 중(IsMoving)일 때만 follow.
        // 현재 기준(사용자 지시 2026-07-26, 이연 폐지): 회피 Task(자체/인수/합성)가 미완료면 follow.
        //   합성 회피의 외부 구간처럼 Task가 살아 있어도 아직 출발 전(IsMoving=false)일 수 있으므로
        //   "미완료" 판정을 우선하고, 완료 직후 잔여 감속 구간은 기존 IsMoving 조건이 보완한다.
        //   (Task가 없으면 — 배치 2번째 이후 픽 포함 — 기존 일반 이동.)
        private bool ShouldFollowInputVisionRetreatForPickerEntry(InputStageUnit stage)
        {
            return _inputVisionPickerEntryTargetPrepared &&
                   stage != null &&
                   stage.CameraX != null &&
                   _inputVisionRetreatMoveTask != null &&
                   (!_inputVisionRetreatMoveTask.IsCompleted || stage.CameraX.IsMoving);
        }

        // R3(follow-entry): 피커 X 진입 이동 Task 공통 시작점 — 비전 회피 Task가 진행 중이면
        // follow(+R6 폴백) 합성을, 아니면 기존 명시 속도 이동 헬퍼를 그대로 사용한다.
        private async Task<int> StartPickUpPickerXEntryMoveTask(
            InputStageUnit stage,
            double velocity,
            double acceleration,
            double deceleration,
            string description,
            string targetName,
            CancellationToken ct)
        {
            if (ShouldFollowInputVisionRetreatForPickerEntry(stage))
                return await MovePickerXEntryByVisionFollowOrFallbackAsync(
                    stage, velocity, description, targetName, ct).ConfigureAwait(false);

            // 안전 불변식: 회피 미완료 상태로 일반 이동에 들어오면 비전이 아직 검사 위치(깊은 쪽)에
            // 있어 인터락(-11, VerifyInputVisionXAtAvoidOrBelowZero)에 걸린다. 위 게이트가 미완료
            // Task를 follow로 보내므로 여기에 도달할 일은 사실상 없지만, stage/CameraX null 등
            // 방어적 케이스를 위해 회피 Task 완료를 확인(join)한 뒤에만 X 명령을 낸다(A8: 외부
            // 인수 Task 포함).
            int deferredJoin = await JoinDeferredInputVisionRetreatBeforePlainMoveAsync(stage, ct).ConfigureAwait(false);
            if (deferredJoin != 0)
                return deferredJoin;

            return await MovePickerAxisWithMotionAndVerifyAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                velocity,
                acceleration,
                deceleration,
                description,
                targetName,
                ct).ConfigureAwait(false);
        }

        // 현재 기준(사용자 지시 2026-07-26): follow를 타지 않는 일반 이동 분기 공통 방어 —
        // 진행 중인 회피 Task(자체/외부 인수/합성)가 있으면 완료(join)까지 확인한다(A8: 외부 회피
        // Task도 join 대상에 포함). Task가 없으면 0(무동작).
        // 보강(사용자 승인 2026-07-26, 3번): join 성공 후에도 실위치 도착을 재확인한다 —
        // 이동 Task가 실제 도착 전에 완료 상태가 되는 사례 방어(미도착이면 도착 대기 후 진입).
        private async Task<int> JoinDeferredInputVisionRetreatBeforePlainMoveAsync(
            InputStageUnit stage,
            CancellationToken ct)
        {
            if (_inputVisionRetreatMoveTask == null)
                return 0;

            int joinResult = await JoinInputVisionRetreatMoveTaskAsync("일반 이동 전 회피 완료", ct).ConfigureAwait(false);
            if (joinResult != 0)
                return joinResult;

            if (_inputVisionPickerEntryTargetPrepared &&
                stage != null &&
                !IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, _inputVisionPickerEntryTarget))
            {
                WriteLog("PickerPickUpSequence",
                    Name + " 회피 Task join 후 실위치 미도착 감지 — 일반 X 진입 전 도착을 대기합니다. " +
                    "target=" + _inputVisionPickerEntryTarget.ToString("F6") +
                    ", actual=" + (stage.CameraX != null ? stage.CameraX.ActualPosition.ToString("F6") : "-") + " - Check");
                return await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.VisionX,
                    _inputVisionPickerEntryTarget,
                    "일반 이동 전 회피 도착 확인",
                    ct).ConfigureAwait(false);
            }

            return 0;
        }

        // R6(follow-entry): follow 실패 시 — 함수가 피커 정지를 보장하므로 비전 회피 Task를
        // join(observe)한 뒤 기존 일반 이동(공유레일 대기 게이트 포함 순차 경로)으로 1회 재시도한다.
        private async Task<int> MovePickerXEntryByVisionFollowOrFallbackAsync(
            InputStageUnit stage,
            double velocity,
            string description,
            string targetName,
            CancellationToken ct)
        {
            // 현재 기준(사용자 지시 2026-07-26, 이연 폐지): 회피 Task(자체/외부 인수/합성)는 회피
            // 좌표 확정 시점에 이미 기동되어 있다 — 여기서는 곧바로 FollowMoveAsync로 추종 진입한다.
            // R6 폴백(아래 join+일반 이동)은 Task가 이미 시작돼 있어 기존대로 동작한다.
            int followResult = await TryFollowPickerXBehindInputVisionRetreatAsync(
                stage,
                velocity,
                targetName,
                ct).ConfigureAwait(false);
            if (followResult == 0)
                return CheckPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX, description);

            int retreatJoin = await JoinInputVisionRetreatMoveTaskAsync(
                description + " follow 폴백",
                ct).ConfigureAwait(false);
            WriteLog("PickerPickUpSequence",
                Name + " " + description + " 팔로잉 진입이 실패해 기존 일반 이동으로 재시도합니다. " +
                "followResult=" + followResult +
                ", retreatJoin=" + retreatJoin + " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                description + " (follow 폴백)",
                ct,
                targetName,
                forceMove: true).ConfigureAwait(false);
        }

        // [사용자 승인 2026-07-27] 픽업 중 InputVisionX 비동기 전진 — die 이송 발행 때마다
        // "남은 픽커 기준 최소 회피 경계"까지 비전을 미리 당겨, 픽업 완료 후 검사 진입 거리를
        // 줄인다. 규칙(사용자 지시): ①비전이 이동 중이면 알람/대기 없이 조용히 스킵(재명령 금지)
        // ②Auto+Conti 외 스킵 ③유의미한 전진(+0.5mm 이상)일 때만 발행 ④fire-and-forget,
        // 실패(-11 등)는 로그만 남기고 픽업은 계속한다. 경계 산식은 기존 부호 인지 최소 회피
        // (TryResolveMinimalVisionRetreatTarget, Extra+마진 동일)를 남은 픽커 목록으로 재사용 —
        // 픽커 X 이동 인터락의 비전 간격 기준과 정합이 보장된다.
        private void TryAdvanceInputVisionForRemainingPicks()
        {
            // [진단 2026-07-27] 무발행 원인 확정용 스킵 사유 로그 — die당 1줄.
            string skipReason = null;
            double diagTarget = double.NaN;
            double diagActual = double.NaN;
            string diagDetail = null;
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    skipReason = "notAuto";
                    return;
                }
                PickerPickUpMotionConfig advanceConfig = ResolvePickUpMotionConfig();
                if (advanceConfig == null || !IsCoordinatedPickUpTransferMotionMode(advanceConfig.TransferMotionMode))
                {
                    skipReason = "notConti";
                    return;
                }
                InputStageUnit stage = ResolveInputStage();
                if (stage == null || stage.CameraX == null || stage.Recipe == null)
                {
                    skipReason = "noStage";
                    return;
                }
                if (_pickBatchItems == null || _pickCursor < 0 || _pickCursor >= _pickBatchItems.Count)
                {
                    skipReason = "cursor(" + _pickCursor + "/" +
                                 (_pickBatchItems != null ? _pickBatchItems.Count : -1) + ")";
                    return;
                }

                diagActual = stage.CameraX.ActualPosition;

                // ① 이전 명령이 아직 이동 중이면 그냥 둔다(사용자 지시 — 알람/대기/재명령 금지).
                //    [정정 2026-07-27] 이동 중 "오버라이드 연장"은 사용자 지시로 제거 — 비전-피커 간
                //    인터락 센서가 아직 정위치에 없어 위험하고, 실속도에서는 비전이 픽업보다 빨라
                //    다음 die 시점의 정지 상태 재발행으로 충분하다. 자기 전진/외부 이동 구분은
                //    스킵 사유 문자열로만 남긴다(진단용).
                if (stage.CameraX.IsMoving)
                {
                    skipReason = (_pickUpVisionAdvanceTask != null && !_pickUpVisionAdvanceTask.IsCompleted)
                        ? "ownAdvanceStillMoving"
                        : "visionMovingForeign";
                    return;
                }
                if (_pickUpVisionAdvanceTask != null && !_pickUpVisionAdvanceTask.IsCompleted)
                {
                    // 축은 멈췄는데 이동 Task 마무리가 아직인 극단 레이스 — 이번 die는 건너뛴다.
                    skipReason = "advanceTaskFinishing";
                    return;
                }

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                if (service == null)
                {
                    skipReason = "noService";
                    return;
                }

                stage.Recipe.EnsurePositionObjects();
                double fullAvoid = stage.Recipe.VisionX.AvoidPosition;
                var planned = new Dictionary<SharedRailXAxis, IList<double>>();
                SharedRailXAxis pickerRailAxis = Side == PickerSequenceSide.Front
                    ? SharedRailXAxis.FrontPickerX
                    : SharedRailXAxis.RearPickerX;
                var remaining = new List<double>();
                for (int i = _pickCursor; i < _pickBatchItems.Count; i++)
                    remaining.Add(_pickBatchItems[i].TargetPickerX);
                planned[pickerRailAxis] = remaining;

                double advanceTarget;
                string advanceDetail;
                if (!service.TryResolveMinimalVisionRetreatTarget(
                        stage.CameraX,
                        fullAvoid,
                        planned,
                        (service.Config != null ? service.Config.InputVisionRetreatExtraClearance : 40.0) +
                        VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm,
                        out advanceTarget,
                        out advanceDetail,
                        allowForwardAdvance: true))
                {
                    skipReason = "resolverFalse";
                    diagDetail = advanceDetail;
                    return;
                }

                diagTarget = advanceTarget;
                diagDetail = advanceDetail;

                // ③ 전진(+) 방향의 유의미한 이동일 때만.
                if (advanceTarget <= stage.CameraX.ActualPosition + 0.5)
                {
                    skipReason = "noForwardGain";
                    return;
                }

                double velocity = stage.CameraX.Config != null ? stage.CameraX.Config.GetDefaultVel() : 0.0;
                Task<int> advanceTask = SharedRailXMotionRuntime.MoveAxisAsync(
                    stage.CameraX, advanceTarget, velocity, false);
                // [한 번에 수정 2026-07-27] 발행 목표를 "확정 피커 진입 목표"로 등록한다 — resolver가
                // 남은 픽 전체와의 간격을 보장한 값이라 ContiNode 비전 검사(정지: 목표 일치 / 이동:
                // 자기 전진 Task)와 정합된다. 미등록이 ContiNode 연쇄 탈락(배치당 1회 이동)의 원인이었다.
                _pickUpVisionAdvanceTask = advanceTask;
                _pickUpVisionAdvanceTarget = advanceTarget;
                _inputVisionPickerEntryTarget = advanceTarget;
                _inputVisionPickerEntryTargetPrepared = true;
                WriteLog("PickerPickUpSequence",
                    Name + " 픽업 중 InputVisionX 비동기 전진 발행. target=" + advanceTarget.ToString("F3") +
                    ", actual=" + stage.CameraX.ActualPosition.ToString("F3") +
                    ", remainingPicks=" + remaining.Count +
                    ", entryTargetUpdated=true, detail=" + advanceDetail + " - Start");
                advanceTask.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted && t.Exception != null)
                            t.Exception.Flatten();
                        int code = t.IsFaulted ? -1 : (t.IsCanceled ? -2 : t.Result);
                        if (code != 0)
                            QMC.Common.Log.Write("Main", "SYSTEM", "PickerPickUpSequence",
                                Name + " 픽업 중 InputVisionX 전진 실패(무시, 픽업 계속). result=" + code + " - Check");
                    },
                    TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " 픽업 중 InputVisionX 전진 시도 중 예외(무시). error=" + ex.Message + " - Check");
            }
            finally
            {
                if (skipReason != null)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " 픽업 중 InputVisionX 전진 스킵. reason=" + skipReason +
                        ", cursor=" + _pickCursor +
                        ", actual=" + (double.IsNaN(diagActual) ? "-" : diagActual.ToString("F3")) +
                        ", target=" + (double.IsNaN(diagTarget) ? "-" : diagTarget.ToString("F3")) +
                        ", detail=" + (diagDetail ?? "-") + " - Check");
                }
            }
        }

        // R3/R5(follow-entry): 피커X(후행)가 회피 중인 InputVisionX(선행)를 추종 진입한다.
        // homeGap/safetyGap/direction/timeout 전부 SharedRailX 설정에서 런타임 조회(하드코딩 금지).
        // 안전 근거: 팔로잉 유지 간격(safetyGap=SafetyDistance+InputExtra, 기본 50) > 인터락 요구
        // (SafetyDistance, 기본 10)이므로 정상 추종 중 인터락 거부는 없다. 그럼에도 -11이면 R6 폴백.
        // 인터락 통과 체인: FollowMoveAsync 내부 MoveAbsoluteAsync→BaseAxis.VerifyMotionGuard→
        // MotionGuardRuntime.VerifyAxisMove(SharedRailX 포함) / TryOverridePosition→
        // MotionGuardRuntime.VerifyAxisTeachingMove — 우회 API 미사용.
        private async Task<int> TryFollowPickerXBehindInputVisionRetreatAsync(
            InputStageUnit stage,
            double velocity,
            string targetName,
            CancellationToken ct)
        {
            BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
            AjinAxis followPickerX = pickerX as AjinAxis;
            BaseAxis visionX = stage != null ? stage.CameraX : null;
            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                Context != null ? Context.Machine : null);
            if (followPickerX == null || visionX == null || service == null)
                return -1;

            int direction;
            double homeGap;
            double safetyGap;
            string gapDetail;
            if (!service.TryGetFollowGapParameters(
                pickerX,
                visionX,
                service.Config != null ? service.Config.InputVisionRetreatExtraClearance : 40.0,
                out direction,
                out homeGap,
                out safetyGap,
                out gapDetail))
            {
                WriteLog("PickerPickUpSequence",
                    Name + " PickUp 피커X 팔로잉 파라미터 조회에 실패해 일반 이동으로 진행합니다. " +
                    "detail=" + gapDetail + " - Check");
                return -1;
            }

            // [팔로잉 시작 게이트 2026-08-27, 팀장님 승인] 거리 = INPUT SAFETY OFFSET(0 이하 = 비활성).
            // 지연 전용 — 통과/해제/타임아웃 모두 아래 팔로잉으로 그대로 진행한다.
            await SharedRailXMotionService.WaitFollowStartGateAsync(
                pickerX,
                visionX,
                _targetPickerX,
                direction,
                homeGap,
                safetyGap,
                GetOwnPickerFollowGateDistanceMm(inputSide: true),
                "PickerPickUpSequence",
                Name + " PickUp 피커X",
                ct).ConfigureAwait(false);

            // C2(2026-07-26): 타임아웃은 100% 기준 설정값이므로 속도 스케일 역수로 확장한다
            // (5% 스케일에서 최소회피 ~330mm ≈ 6.6초 → 기존 15초는 통과하지만, 전체 Avoid급
            // 장거리 회피/저속 조합의 오탐 -21 → R6 폴백 빈발을 차단).
            int timeoutMs = MotionSpeedScale.ScaleDefaultTimeoutMs(
                service.Config != null ? service.Config.VisionFollowEntryTimeoutMs : 15000);
            // 현재 기준: follow의 명령/오버라이드 경로는 축 레이어 자동 스케일이 없으므로 여기서 1회 스케일.
            double trailingVelocity = velocity > 0.0
                ? velocity
                : MotionSpeedScale.ApplyDefaultVelocityScale(
                    pickerX.Config != null ? pickerX.Config.GetRawDefaultVelocity() : 0.0);
            double trailingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                pickerX.Config != null ? pickerX.Config.GetRawAcceleration() : 0.0);
            double trailingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                pickerX.Config != null ? pickerX.Config.GetRawDeceleration() : 0.0);
            double leadingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                visionX.Config != null ? visionX.Config.GetRawDefaultVelocity() : 0.0);
            double leadingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                visionX.Config != null ? visionX.Config.GetRawAcceleration() : 0.0);
            double leadingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                visionX.Config != null ? visionX.Config.GetRawDeceleration() : 0.0);

            WriteLog("PickerPickUpSequence",
                Name + " PickUp 피커X 팔로잉 진입을 시작합니다. leading=" + visionX.Name +
                ", visionTarget=" + _inputVisionPickerEntryTarget.ToString("F6") +
                ", pickerTarget=" + _targetPickerX.ToString("F6") +
                ", " + gapDetail +
                ", timeoutMs=" + timeoutMs + " - Start");

            QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("PICKUP", TactRequestId(), "PickerX");
            int followResult = await followPickerX.FollowMoveAsync(
                visionX,
                _inputVisionPickerEntryTarget,
                leadingVelocity,
                leadingAcceleration,
                leadingDeceleration,
                _targetPickerX,
                trailingVelocity,
                trailingAcceleration,
                trailingDeceleration,
                direction,
                safetyGap,
                homeGap,
                timeoutMs,
                BuildFollowEntryTargetName(targetName, PickerWorkZone.Input),
                ct).ConfigureAwait(false);
            if (followResult == 0)
                QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("PICKUP", TactRequestId(), "PickerX");

            return followResult;
        }

        // R3(follow-entry): X(follow)/T(단독) 분리 이동의 결과 합류 — 첫 실패 코드를 반환한다.
        private static async Task<int> JoinPickerEntryMoveResultsAsync(Task<int> pickerXMove, Task<int> pickerTMove)
        {
            int[] results = await Task.WhenAll(pickerXMove, pickerTMove).ConfigureAwait(false);
            return results[0] != 0 ? results[0] : results[1];
        }

        #endregion

    }
}
