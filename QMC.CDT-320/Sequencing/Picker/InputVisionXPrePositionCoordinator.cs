using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal static class InputVisionXPrePositionCoordinator
    {
        private const int GuardPollIntervalMs = 10;
        private const int GuardWaitLogIntervalMs = 1000;
        private const int StandbySearchIterations = 24;
        // [사용자 지시 2026-07-28] 픽업 퇴장 팔로잉은 "첫 명령 이동량이 이 값 이상"이 될 때까지만
        // 대기했다가 진입한다. 선행축 속도/가속 기반 대기는 사용하지 않는다(대기 시간 최소화).
        private const double FollowStartMinFirstMoveMm = 20.0;
        // [사용자 지시 2026-07-30] 선행축(픽커)이 퇴장 방향으로 이 거리 이상 실제로 이동한 뒤에만 출발한다.
        //   기존 조건: "첫 명령 이동량 ≥ 20mm"만 확인 — 진입 유지갭이 50이던 때는 그 20mm가 선행축
        //     퇴장으로만 생겨 사실상 "선행축 20mm 퇴장 후 출발"과 같았다. 유지갭 축소(2026-07-30) 후에는
        //     축소분(약 40mm)이 공짜 이동량으로 잡혀, 선행축이 정지(픽업 중)인데도 waitedMs=0으로 즉시
        //     출발해 정지 중인 픽커 옆 실시간 가드 정지선까지 따라붙었다(실장비 01:xx 등호 정지 알람).
        //   현재 기준: 게이트 시작 시점 대비 선행축 퇴장 변위 ≥ 이 값 AND 첫 명령 이동량 ≥ 20mm.
        private const double FollowStartMinLeadingDepartureMm = 20.0;
        private const int FollowStartWaitTimeoutMs = 1000;
        private const double StandbyBoundaryBackoffMm = 1.0;
        private const double MinimumStandbyTravelMm = 0.2;

        private static readonly object Sync = new object();
        private static Task<int> _runningTask;
        private static CancellationTokenSource _runningCancellation;
        private static PickerSequenceSide _runningSide;

        // onSessionCompleted(A안, 사용자 승인 2026-07-27): 세션이 정상 종료(result=0, 최종 검사
        // 위치 도착 포함)되면 1회 호출된다 — 호출자가 "도착 즉시 선행검사 재시도" 등 후속 트리거를
        // 걸 수 있게 한다. 기본 null = 기존 동작 무변경.
        public static bool EnsureStarted(
            MachineSequenceContext context,
            PickerSequenceSide side,
            PickerSequenceOptions options,
            CancellationToken ct,
            string reason,
            Action<int> onSessionCompleted = null)
        {
            if (context == null || context.Machine == null)
                return false;
            if (options != null &&
                options.RunMode == SequenceRunMode.Auto &&
                context.IsCycleStopRequested)
            {
                return false;
            }

            lock (Sync)
            {
                if (options != null &&
                    options.RunMode == SequenceRunMode.Auto &&
                    context.IsCycleStopRequested)
                {
                    return false;
                }

                ClearCompletedNoLock();
                if (_runningTask != null && !_runningTask.IsCompleted)
                {
                    WriteLog(
                        "InputVisionXPrePosition",
                        side + " InputVisionX 선행이동 요청을 기존 실행 중인 세션과 중복 실행하지 않습니다. " +
                        "runningSide=" + _runningSide +
                        ", reason=" + Safe(reason) + " - Check");
                    return false;
                }

                CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                Task<int> task = RunAsync(context, side, options, linkedCancellation.Token, reason);
                _runningSide = side;
                _runningCancellation = linkedCancellation;
                _runningTask = task;

                task.ContinueWith(
                    completed =>
                    {
                        CompleteTask(completed, linkedCancellation);
                        InvokeSessionCompletedCallback(side, completed, onSessionCompleted);
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            WriteLog(
                "InputVisionXPrePosition",
                side + " PickUp 완료 후 InputVisionX 선행이동 세션을 시작했습니다. " +
                "reason=" + Safe(reason) + " - Start");
            return true;
        }

        /// <summary>
        /// 정상 Auto Cycle Stop에서 이미 시작된 InputVisionX 선행이동 세션의 종료를 기다린다.
        /// 신규 취소를 발행하지 않으며 세션 자체의 Cycle Stop 안전 정리 완료만 확인한다.
        /// </summary>
        public static async Task<int> WaitUntilIdleAsync(
            string reason,
            int timeoutMs,
            CancellationToken ct)
        {
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "CycleStopDrain" : reason;
            int safeTimeoutMs = timeoutMs > 0 ? timeoutMs : 30000;
            DateTime startedAt = DateTime.UtcNow;
            bool waitLogged = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                Task<int> runningTask;
                PickerSequenceSide runningSide;
                lock (Sync)
                {
                    runningTask = _runningTask;
                    runningSide = _runningSide;
                }

                if (runningTask == null)
                {
                    if (waitLogged)
                    {
                        WriteLog("InputVisionXPrePosition",
                            safeReason + " InputVisionX 선행이동 drain 완료. - Ok");
                    }
                    return 0;
                }

                if (runningTask.IsCompleted)
                {
                    try
                    {
                        int result = await runningTask.ConfigureAwait(false);
                        if (result != 0)
                        {
                            WriteLog("InputVisionXPrePosition",
                                safeReason + " InputVisionX 선행이동 drain 결과 실패. side=" + runningSide +
                                ", result=" + result + " - Failed");
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog("InputVisionXPrePosition",
                            safeReason + " InputVisionX 선행이동 drain 작업 실패. side=" + runningSide +
                            ", error=" + ex.Message + " - Failed");
                        return -1;
                    }

                    await Task.Yield();
                    continue;
                }

                if ((DateTime.UtcNow - startedAt).TotalMilliseconds >= safeTimeoutMs)
                {
                    WriteLog("InputVisionXPrePosition",
                        safeReason + " InputVisionX 선행이동 drain 시간 초과. side=" + runningSide +
                        ", timeoutMs=" + safeTimeoutMs + " - Failed");
                    return -1;
                }

                if (!waitLogged)
                {
                    waitLogged = true;
                    WriteLog("InputVisionXPrePosition",
                        safeReason + " 진행 중 InputVisionX 선행이동의 안전 종료를 기다립니다. side=" +
                        runningSide + " - Wait");
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }

        // [A안 2026-07-27] 세션 정상 종료 시 호출자 콜백을 1회 실행한다. 취소/실패/예외 종료는
        // 호출하지 않는다(도착 보장이 없으므로). 콜백 예외는 로그만 남기고 무시한다.
        private static void InvokeSessionCompletedCallback(
            PickerSequenceSide side,
            Task<int> completed,
            Action<int> onSessionCompleted)
        {
            if (onSessionCompleted == null)
                return;

            try
            {
                if (completed == null || completed.Status != TaskStatus.RanToCompletion)
                    return;

                WriteLog(
                    "InputVisionXPrePosition",
                    side + " InputVisionX 선행이동 세션 종료 콜백을 호출합니다. result=" + completed.Result + " - Check");
                onSessionCompleted(completed.Result);
            }
            catch (Exception ex)
            {
                WriteLog(
                    "InputVisionXPrePosition",
                    side + " InputVisionX 선행이동 세션 종료 콜백 처리 중 예외(무시). error=" + ex.Message + " - Check");
            }
        }

        public static void Cancel(PickerSequenceSide side)
        {
            CancellationTokenSource cancellation = null;
            lock (Sync)
            {
                if (_runningTask == null || _runningTask.IsCompleted || _runningSide != side)
                    return;

                cancellation = _runningCancellation;
            }

            try
            {
                if (cancellation != null)
                    cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동 취소 토큰이 이미 해제되었습니다. - Check");
            }
            catch (Exception ex)
            {
                WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동 취소 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static async Task<int> RunAsync(
            MachineSequenceContext context,
            PickerSequenceSide side,
            PickerSequenceOptions options,
            CancellationToken ct,
            string reason)
        {
            BaseAxis visionX = null;
            Task<int> activeMoveTask = null;
            bool stopActiveMoveOnExit = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                context.StopIfCycleStopRequested("InputVisionXPrePosition.Start:" + side);

                InputStageUnit stage = context.Machine.InputStageUnit;
                if (stage == null || stage.CameraX == null)
                {
                    WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동을 생략합니다. InputStage/CameraX가 없습니다. - Check");
                    return 0;
                }

                visionX = stage.CameraX;
                if (!visionX.IsServoOn || visionX.IsAlarm)
                {
                    WriteLog(
                        "InputVisionXPrePosition",
                        side + " InputVisionX 선행이동을 생략합니다. 축 준비 상태가 아닙니다. " +
                        BuildAxisState(visionX, visionX.ActualPosition) + " - Check");
                    return 0;
                }

                InputStagePickTargetCandidate target = ResolveNextTargetCandidate();
                if (target == null)
                {
                    WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동 대상 die가 없습니다. reason=" + Safe(reason) + " - Check");
                    return 0;
                }

                double finalTarget = target.TargetX;
                MotionProfile motion = ResolveMotionProfile(visionX);
                int moveTimeoutMs = ResolveMoveTimeout(options);
                string finalTargetName = BuildTargetName(target, "Final");
                string finalGuardReason;

                WriteLog(
                    "InputVisionXPrePosition",
                    side + " InputVisionX 선행이동 대상을 선택했습니다. " +
                    "die=" + target.DieId +
                    ", grid=(" + target.DieMapX + "," + target.DieMapY + ")" +
                    ", actualX=" + visionX.ActualPosition.ToString("F6") +
                    ", finalX=" + finalTarget.ToString("F6") +
                    ", speedScalePercent=" + MotionSpeedScale.ScalePercent.ToString("F3") +
                    ", effectiveScaleFactor=" + MotionSpeedScale.EffectiveScaleFactor.ToString("F6") +
                    ", velocity=" + motion.Velocity.ToString("F6") +
                    ", acceleration=" + motion.Acceleration.ToString("F6") +
                    ", deceleration=" + motion.Deceleration.ToString("F6") +
                    ", reason=" + Safe(reason) + " - Check");

                if (CanSkipMoveCommand(visionX, finalTarget))
                {
                    WriteLog(
                        "InputVisionXPrePosition",
                        side + " InputVisionX가 이미 다음 die 검사 위치에 있습니다. die=" + target.DieId +
                        ", " + BuildAxisState(visionX, finalTarget) + " - Ok");
                    return 0;
                }

                SequenceResourceLease lease = await context.Resources.AcquireAsync(
                    SequenceResourceKind.InputStageArea,
                    "InputVisionXPrePosition:" + side,
                    0,
                    ct,
                    false).ConfigureAwait(false);
                if (lease == null)
                {
                    WriteLog("InputVisionXPrePosition", side + " InputStageArea를 확보하지 못해 선행이동을 생략합니다. - Check");
                    return 0;
                }

                bool standbyCompleted = false;
                using (lease)
                {
                    ct.ThrowIfCancellationRequested();
                    context.StopIfCycleStopRequested("InputVisionXPrePosition.Acquired:" + side);

                    // [사용자 지시 2026-07-27] 세션 기동 전 MotionDone 확인 — 픽업 중 비동기 전진이
                    // 아직 이동 중이면 정지까지 대기(최대 3초) 후 시작한다. 이동 중 재명령으로 인한
                    // 알람/의도치 않은 동작 방지. 타임아웃이어도 알람 없이 기존 경로로 진행한다.
                    if (visionX != null && visionX.IsMoving)
                    {
                        var motionDoneWait = System.Diagnostics.Stopwatch.StartNew();
                        while (visionX.IsMoving && motionDoneWait.ElapsedMilliseconds < 3000)
                        {
                            ct.ThrowIfCancellationRequested();
                            await Task.Delay(10, ct).ConfigureAwait(false);
                        }
                        WriteLog("InputVisionXPrePosition",
                            side + " 세션 기동 전 MotionDone 대기 완료. waitedMs=" + motionDoneWait.ElapsedMilliseconds +
                            ", stillMoving=" + visionX.IsMoving + " - Check");
                    }

                    if (CanMoveToTarget(context, visionX, finalTarget, finalTargetName, out finalGuardReason))
                    {
                        int directResult = await MoveAndVerifyAsync(
                            stage,
                            visionX,
                            finalTarget,
                            motion,
                            moveTimeoutMs,
                            side,
                            target.DieId,
                            "최종 위치 직접 이동",
                            ct).ConfigureAwait(false);
                        return LogOptimizationResult(side, target.DieId, directResult, "DirectMove");
                    }

                    // [사용자 승인 2026-07-28] 픽업 완료→바텀 퇴장 세션은 대기점 스텝을 쓰지 않는다.
                    //   기존 조건: 직행 가드가 픽커의 "정지 중 현재 위치" 기준이라 바텀 출발 명령
                    //             (실측 +53ms)보다 먼저 판정이 실패, 매 배치 대기점 스텝(+40mm 왕복
                    //             ~0.3s)이 발생했다.
                    //   현재 기준: 자기 픽커X를 리딩축으로 FollowMove 진입 — 픽커가 퇴장하는 만큼
                    //             비전이 간격을 유지하며 최종 촬영 위치까지 연속 추종한다(오버라이드는
                    //             팔로잉 전용 정책 부합). 실패/미출발(-11/-21 등)은 기존 대기점 경로 폴백.
                    if (IsPickUpCompleteToBottomReason(reason))
                    {
                        int followResult = await TryFollowOwnPickerToFinalAsync(
                            context,
                            stage,
                            visionX,
                            finalTarget,
                            moveTimeoutMs,
                            side,
                            target.DieId,
                            ct).ConfigureAwait(false);
                        if (followResult == 0)
                            return LogOptimizationResult(side, target.DieId, 0, "FollowBehindPicker");

                        WriteLog(
                            "InputVisionXPrePosition",
                            side + " 픽업 퇴장 팔로잉 진입이 성립하지 않아 기존 대기점 경로로 폴백합니다. " +
                            "die=" + target.DieId +
                            ", followResult=" + followResult +
                            ", finalX=" + finalTarget.ToString("F6") + " - Check");
                    }

                    double standbyTarget;
                    string standbyDetail;
                    string standbyTargetName = BuildTargetName(target, "Standby");
                    if (!TryResolveSafeStandbyTarget(
                        context,
                        visionX,
                        finalTarget,
                        standbyTargetName,
                        out standbyTarget,
                        out standbyDetail))
                    {
                        WriteLog(
                            "InputVisionXPrePosition",
                            side + " InputVisionX가 먼저 이동할 수 있는 안전 대기점을 찾지 못했습니다. " +
                            "die=" + target.DieId +
                            ", finalX=" + finalTarget.ToString("F6") +
                            ", finalGuard=" + finalGuardReason +
                            ", detail=" + standbyDetail + " - Wait");
                        standbyCompleted = true;
                    }
                    else
                    {
                        WriteLog(
                            "InputVisionXPrePosition",
                            side + " InputVisionX 중간 안전대기점 이동을 시작합니다. " +
                            "die=" + target.DieId +
                            ", standbyX=" + standbyTarget.ToString("F6") +
                            ", finalX=" + finalTarget.ToString("F6") +
                            ", finalGuard=" + finalGuardReason +
                            ", detail=" + standbyDetail + " - Start");

                        // 스케일된 DefaultVelocity를 전달하면 Axis가 원본 Config의 가속/감속에
                        // 동일한 Speed Scale을 정확히 한 번 적용한다.
                        activeMoveTask = SharedRailXMotionRuntime.MoveAxisAsync(
                            visionX,
                            standbyTarget,
                            motion.Velocity);
                        stopActiveMoveOnExit = true;
                        bool guardClearedDuringStandby = false;
                        int standbyResult;

                        try
                        {
                            while (!activeMoveTask.IsCompleted)
                            {
                                ct.ThrowIfCancellationRequested();
                                context.StopIfCycleStopRequested("InputVisionXPrePosition.Standby:" + side);

                                // 기존 조건(사용자 승인 2026-07-26, 롤링 대기점): 이동 중 가드가 열리면
                                //   Position Override로 최종 목표로 연장했다(무정지 전진).
                                // 현재 기준(사용자 지시 2026-07-28, 오버라이드 전면 폐지): 위치 오버라이드는
                                //   팔로잉(FollowMove) 전용이다 — 선행이동은 오버라이드를 쓰지 않는다.
                                //   이동이 이미 끝난 축에 AxmOverridePos가 0(성공)을 반환하며 무효가 되는
                                //   레이스(실장비 2026-07-28 14:15, MOVE JOIN -5)의 원인 제거.
                                //   가드가 이동 중 열려도 대기점 완료를 기다렸다가 완료 검증형 일반
                                //   이동(MoveAndVerifyAsync)으로 최종 진입한다(비동기 Task 구조 유지).
                                if (!guardClearedDuringStandby &&
                                    CanMoveToTarget(context, visionX, finalTarget, finalTargetName, out finalGuardReason))
                                {
                                    guardClearedDuringStandby = true;
                                    WriteLog(
                                        "InputVisionXPrePosition",
                                        side + " 대기점 이동 중 최종 가드가 열렸습니다. 오버라이드 없이 대기점 완료 후 일반 Move로 최종 진입합니다. " +
                                        "die=" + target.DieId +
                                        ", standbyX=" + standbyTarget.ToString("F6") +
                                        ", finalX=" + finalTarget.ToString("F6") + " - Check");
                                }

                                await Task.Delay(GuardPollIntervalMs, ct).ConfigureAwait(false);
                            }

                            standbyResult = await activeMoveTask.ConfigureAwait(false);
                            activeMoveTask = null;
                            stopActiveMoveOnExit = false;
                        }
                        catch (OperationCanceledException)
                        {
                            await StopAndDrainMoveTaskAsync(visionX, activeMoveTask, side).ConfigureAwait(false);
                            activeMoveTask = null;
                            stopActiveMoveOnExit = false;
                            throw;
                        }
                        catch (SequenceStopException)
                        {
                            await StopAndDrainMoveTaskAsync(visionX, activeMoveTask, side).ConfigureAwait(false);
                            activeMoveTask = null;
                            stopActiveMoveOnExit = false;
                            throw;
                        }
                        catch (Exception)
                        {
                            await StopAndDrainMoveTaskAsync(visionX, activeMoveTask, side).ConfigureAwait(false);
                            activeMoveTask = null;
                            stopActiveMoveOnExit = false;
                            throw;
                        }

                        if (standbyResult != 0)
                        {
                            WriteLog(
                                "InputVisionXPrePosition",
                                side + " InputVisionX 중간 안전대기점 이동이 실패했습니다. " +
                                "die=" + target.DieId +
                                ", result=" + standbyResult +
                                ", " + BuildAxisState(visionX, standbyTarget) + " - Failed");
                            return 0;
                        }

                        standbyCompleted = true;
                        WriteLog(
                            "InputVisionXPrePosition",
                            side + " InputVisionX 중간 안전대기점 도착 후 최종 가드 해제를 기다립니다. " +
                            "die=" + target.DieId +
                            ", standbyX=" + standbyTarget.ToString("F6") +
                            ", finalX=" + finalTarget.ToString("F6") + " - Wait");
                    }
                }

                if (!standbyCompleted)
                    return 0;

                return await WaitGuardAndMoveNormallyAsync(
                    context,
                    stage,
                    visionX,
                    target,
                    finalTarget,
                    motion,
                    moveTimeoutMs,
                    side,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                stopActiveMoveOnExit = true;
                WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동이 정지 요청으로 취소되었습니다. - Stopped");
                return 0;
            }
            catch (SequenceStopException ex)
            {
                stopActiveMoveOnExit = true;
                WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동이 Cycle Stop 경계에서 정지되었습니다. reason=" + ex.Message + " - Stopped");
                return 0;
            }
            catch (Exception ex)
            {
                stopActiveMoveOnExit = true;
                WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return 0;
            }
            finally
            {
                // 기존 조건: stopActiveMoveOnExit && visionX.IsMoving — 자기 이동이 이미 끝나 가드
                //   해제를 대기 중일 때 Cycle Stop이 오면, 그 순간 진행 중인 "남의" InputVisionX
                //   이동(예: 선행검사 촬영 접근)을 감속 정지시켜 -5(IN-STAGE-MOVE) 오탐 알람을
                //   유발했다(실장비 2026-07-26 05:21:35 — Front 선행검사 625.896 이동이 Rear
                //   프리포지션 종료 정지에 맞아 command=595.274에서 중단).
                // 현재 기준(2026-07-26): 자기 activeMoveTask가 아직 미완료일 때만 정지한다 —
                //   자기 이동이 없거나 끝났으면 축이 움직여도 남의 이동이므로 건드리지 않는다.
                bool ownMoveStillActive = activeMoveTask != null && !activeMoveTask.IsCompleted;
                if (stopActiveMoveOnExit && ownMoveStillActive && visionX != null && visionX.IsMoving)
                {
                    try
                    {
                        visionX.Stop();
                        WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동 종료 시 이동 중인 축을 감속 정지했습니다. - Stopped");
                    }
                    catch (Exception ex)
                    {
                        WriteLog("InputVisionXPrePosition", side + " InputVisionX 선행이동 종료 정지 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                    }
                }

                ObserveMoveTask(activeMoveTask, side);
            }
        }

        private static async Task<int> WaitGuardAndMoveNormallyAsync(
            MachineSequenceContext context,
            InputStageUnit stage,
            BaseAxis visionX,
            InputStagePickTargetCandidate target,
            double finalTarget,
            MotionProfile motion,
            int moveTimeoutMs,
            PickerSequenceSide side,
            CancellationToken ct)
        {
            DateTime nextWaitLog = DateTime.UtcNow;
            string targetName = BuildTargetName(target, "FinalNormalMove");

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                context.StopIfCycleStopRequested("InputVisionXPrePosition.FinalWait:" + side);

                if (CanSkipMoveCommand(visionX, finalTarget))
                    return LogOptimizationResult(side, target.DieId, 0, "AlreadyAtFinal");

                string guardReason;
                if (!CanMoveToTarget(context, visionX, finalTarget, targetName, out guardReason))
                {
                    if (DateTime.UtcNow >= nextWaitLog)
                    {
                        WriteLog(
                            "InputVisionXPrePosition",
                            side + " InputVisionX 최종 die 위치 이동 가드 해제를 기다립니다. " +
                            "die=" + target.DieId +
                            ", finalX=" + finalTarget.ToString("F6") +
                            ", reason=" + guardReason + " - Wait");
                        nextWaitLog = DateTime.UtcNow.AddMilliseconds(GuardWaitLogIntervalMs);
                    }

                    await Task.Delay(GuardPollIntervalMs, ct).ConfigureAwait(false);
                    continue;
                }

                SequenceResourceLease lease = await context.Resources.AcquireAsync(
                    SequenceResourceKind.InputStageArea,
                    "InputVisionXPrePositionFinal:" + side,
                    200,
                    ct,
                    false).ConfigureAwait(false);
                if (lease == null)
                {
                    await Task.Delay(GuardPollIntervalMs, ct).ConfigureAwait(false);
                    continue;
                }

                using (lease)
                {
                    if (!CanMoveToTarget(context, visionX, finalTarget, targetName, out guardReason))
                        continue;

                    if (CanSkipMoveCommand(visionX, finalTarget))
                        return LogOptimizationResult(side, target.DieId, 0, "AlreadyAtFinalAfterLease");

                    int result = await MoveAndVerifyAsync(
                        stage,
                        visionX,
                        finalTarget,
                        motion,
                        moveTimeoutMs,
                        side,
                        target.DieId,
                        "대기점 정지 후 일반 Move",
                        ct).ConfigureAwait(false);
                    return LogOptimizationResult(side, target.DieId, result, "NormalMoveAfterStandby");
                }
            }
        }

        private static async Task<int> MoveAndVerifyAsync(
            InputStageUnit stage,
            BaseAxis visionX,
            double target,
            MotionProfile motion,
            int timeoutMs,
            PickerSequenceSide side,
            string dieId,
            string description,
            CancellationToken ct)
        {
            WriteLog(
                "InputVisionXPrePosition",
                side + " InputVisionX " + description + " 명령을 시작합니다. " +
                "die=" + dieId +
                ", targetX=" + target.ToString("F6") +
                ", actualX=" + visionX.ActualPosition.ToString("F6") +
                ", velocity=" + motion.Velocity.ToString("F6") +
                ", acceleration=" + motion.Acceleration.ToString("F6") +
                ", deceleration=" + motion.Deceleration.ToString("F6") + " - Start");

            // 일반 Move는 Axis의 Default motion scale 판정을 사용해 가속/감속을 한 번만 스케일한다.
            Task<int> moveTask = SharedRailXMotionRuntime.MoveAxisAsync(
                visionX,
                target,
                motion.Velocity);
            try
            {
                while (!moveTask.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(GuardPollIntervalMs, ct).ConfigureAwait(false);
                }

                int result = await moveTask.ConfigureAwait(false);
                if (result != 0)
                    return result;
            }
            catch (OperationCanceledException)
            {
                await StopAndDrainMoveTaskAsync(visionX, moveTask, side).ConfigureAwait(false);
                throw;
            }

            return await stage.WaitInputStageAxisInPosition(
                WaferStageAxis.VisionX,
                target,
                timeoutMs,
                ct).ConfigureAwait(false);
        }

        #region InputVision 사전 위치 — PickUpCompleteToBottom Follow

        // SAFETY CONTRACT:
        // - PickUpCompleteToBottom 세션에서 퇴장 PickerX가 선행하고 InputVisionX가 후행한다.
        // - 선행 Picker 퇴장량·최초 이동량·시작 대기 조건은 정지 Picker 옆 선진입을 막는 한 묶음의 게이트다.
        // - 일반 이동에 Position Override를 재도입하지 않으며, Follow 실패는 기존 Standby 경로에 폴백한다.

        // 2026-07-28 사용자 지시: 위치 오버라이드는 팔로잉(FollowMove) 전용 — 선행이동의
        // TryOverrideMovingAxisToFinal(이동 중 최종 진입 오버라이드)은 무효 성공 레이스
        // (완료된 이동에 AxmOverridePos가 0을 반환, MOVE JOIN -5)로 폐지·삭제했다.

        // [사용자 승인 2026-07-28] 세션 사유가 "픽업 완료 → 바텀 퇴장"인지 판정한다.
        private static bool IsPickUpCompleteToBottomReason(string reason)
        {
            return !string.IsNullOrWhiteSpace(reason) &&
                   reason.IndexOf("PickUpCompleteToBottom", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // [사용자 승인 2026-07-28] 자기 픽커X(바텀으로 퇴장 중)를 리딩축으로 InputVisionX가 최종
        // 촬영 위치까지 팔로잉 진입한다 — Output 후검사 TryFollowOutputVisionXBehindPickerAsync의
        // 인풋 미러. 픽커가 아직 정지 상태면 FollowMove가 여유(slack) 없는 동안 명령 없이 대기하고,
        // 끝내 미출발이면 타임아웃(-21) → 호출자가 기존 대기점 경로로 폴백한다. 팔로잉 내부
        // 이동/오버라이드는 MotionGuard(SharedRailX 페어 간격 포함)를 통과하며 검증된다(위반 -11 → 폴백).
        private static async Task<int> TryFollowOwnPickerToFinalAsync(
            MachineSequenceContext context,
            InputStageUnit stage,
            BaseAxis visionX,
            double finalTarget,
            int moveTimeoutMs,
            PickerSequenceSide side,
            string dieId,
            CancellationToken ct)
        {
            AjinAxis followVisionX = visionX as AjinAxis;
            BaseAxis leadingPickerX = side == PickerSequenceSide.Front
                ? (context.Machine.PickerFrontUnit != null ? context.Machine.PickerFrontUnit.PickerX : null)
                : (context.Machine.PickerRearUnit != null ? context.Machine.PickerRearUnit.PickerX : null);
            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(context.Machine);
            if (followVisionX == null || leadingPickerX == null || service == null)
                return -1;

            int direction;
            double homeGap;
            double safetyGap;
            string gapDetail;
            if (!service.TryGetFollowGapParameters(
                visionX,
                leadingPickerX,
                2.0, // 진입 유지갭 = SafetyDistance + 경계여유 2mm(2026-07-30) — 선행검사 진입 팔로잉과 동일 기준.
                out direction,
                out homeGap,
                out safetyGap,
                out gapDetail))
            {
                WriteLog(
                    "InputVisionXPrePosition",
                    side + " 픽업 퇴장 팔로잉 파라미터 조회 실패 — 기존 대기점 경로로 폴백합니다. " +
                    "die=" + dieId +
                    ", detail=" + gapDetail + " - Check");
                return -1;
            }

            // 타임아웃/속도/가감속은 100% 기준 설정값이므로 속도 스케일을 정확히 1회 적용한다
            // (Output 후검사 팔로잉과 동일 규약).
            int followTimeoutMs = MotionSpeedScale.ScaleDefaultTimeoutMs(
                service.Config != null ? service.Config.VisionFollowEntryTimeoutMs : 15000);
            double trailingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                visionX.Config != null ? visionX.Config.GetRawDefaultVelocity() : 0.0);
            double trailingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                visionX.Config != null ? visionX.Config.GetRawAcceleration() : 0.0);
            double trailingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                visionX.Config != null ? visionX.Config.GetRawDeceleration() : 0.0);
            double leadingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.GetRawDefaultVelocity() : 0.0);
            double leadingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.GetRawAcceleration() : 0.0);
            double leadingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.GetRawDeceleration() : 0.0);

            // [사용자 지시 2026-07-28] 첫 명령 이동량이 FollowStartMinFirstMoveMm(20mm) 이상이 될
            //   때까지만 대기했다가 진입한다.
            //   기존 조건: 픽업 배치 완료 즉시 FollowMove를 걸었다. 실측(19:10:57.266)에서 팔로잉이
            //     픽커 X 이동 명령(.270)보다 3ms 빨라 진입 시 선행축이 정지 상태였고, 경계가
            //     선행축 실측의 함수라 첫 명령이 +1.0mm에 그쳤다. 이후 선행축 가속 구간 동안
            //     목표가 0.07→0.16→0.44mm씩만 늘어나 후행축이 300ms 동안 미세 이동·정지를 27회
            //     반복했다(실장비 육안: 뒤로 튀었다가 다시 전진).
            //   현재 기준: 경계(bound)와 후행축 현재 위치의 차이 = 첫 명령의 실제 이동량이므로,
            //     그 값이 20mm 이상일 때 진입한다 — 첫 명령이 연속 주행 구간을 확보한다.
            //     선행축 속도 도달을 기다리지 않는다(대기 시간 최소화).
            //   대기 실패(선행축 미출발 등)는 폴백 코드로 돌려보내 기존 대기점 경로가 처리한다.
            bool startWaitLogged = false;
            DateTime startWaitBegin = DateTime.UtcNow;
            // [사용자 지시 2026-07-30] 선행축 퇴장 변위 기준점 — 게이트 시작 시점의 선행축 실측 위치.
            double leadingStartActual = leadingPickerX.ActualPosition;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                context.StopIfCycleStopRequested("InputVisionXPrePosition.FollowStartWait:" + side);

                double leadingActualNow = leadingPickerX.ActualPosition;
                double visionActualNow = visionX.ActualPosition;
                // FollowMoveAsync의 경계식과 동일: direction>0 → bound = 선행 + homeGap − safetyGap.
                double boundNow = direction > 0
                    ? leadingActualNow + homeGap - safetyGap
                    : leadingActualNow - homeGap + safetyGap;
                // 첫 명령은 min/max(최종목표, bound)이므로 실제 이동량은 목표까지의 거리로도 제한된다.
                double firstCommandNow = direction > 0
                    ? Math.Min(finalTarget, boundNow)
                    : Math.Max(finalTarget, boundNow);
                double firstMoveNow = direction > 0
                    ? firstCommandNow - visionActualNow
                    : visionActualNow - firstCommandNow;
                // [사용자 지시 2026-07-30] 선행축이 퇴장 방향으로 실제 이동한 변위 — 이 값이
                // FollowStartMinLeadingDepartureMm 이상이어야 출발한다(정지 픽커 옆 선진입 방지).
                double leadingDepartureNow = direction > 0
                    ? leadingActualNow - leadingStartActual
                    : leadingStartActual - leadingActualNow;

                if (leadingDepartureNow >= FollowStartMinLeadingDepartureMm &&
                    firstMoveNow >= FollowStartMinFirstMoveMm)
                {
                    WriteLog(
                        "InputVisionXPrePosition",
                        side + " 픽업 퇴장 팔로잉 진입 기회 확보(선행축 퇴장+첫 명령 이동량 확보). " +
                        "die=" + dieId +
                        ", leadingActual=" + leadingActualNow.ToString("F3") +
                        ", leadingDeparture=" + leadingDepartureNow.ToString("F3") +
                        ", requiredDeparture=" + FollowStartMinLeadingDepartureMm.ToString("F3") +
                        ", visionActual=" + visionActualNow.ToString("F3") +
                        ", firstCommand=" + firstCommandNow.ToString("F3") +
                        ", firstMove=" + firstMoveNow.ToString("F3") +
                        ", requiredFirstMove=" + FollowStartMinFirstMoveMm.ToString("F3") +
                        ", waitedMs=" + ((int)(DateTime.UtcNow - startWaitBegin).TotalMilliseconds) + " - Ok");
                    break;
                }

                if (!startWaitLogged)
                {
                    WriteLog(
                        "InputVisionXPrePosition",
                        side + " 픽업 퇴장 팔로잉 진입 기회를 대기합니다(선행축 퇴장/첫 명령 이동량 대기). " +
                        "die=" + dieId +
                        ", leadingActual=" + leadingActualNow.ToString("F3") +
                        ", leadingDeparture=" + leadingDepartureNow.ToString("F3") +
                        ", requiredDeparture=" + FollowStartMinLeadingDepartureMm.ToString("F3") +
                        ", leadingMoving=" + leadingPickerX.IsMoving +
                        ", firstMove=" + firstMoveNow.ToString("F3") +
                        ", requiredFirstMove=" + FollowStartMinFirstMoveMm.ToString("F3") + " - Wait");
                    startWaitLogged = true;
                }

                if ((DateTime.UtcNow - startWaitBegin).TotalMilliseconds >= FollowStartWaitTimeoutMs)
                {
                    WriteLog(
                        "InputVisionXPrePosition",
                        side + " 픽업 퇴장 팔로잉 진입 기회 대기가 타임아웃되어 기존 대기점 경로로 위임합니다. " +
                        "die=" + dieId +
                        ", timeoutMs=" + FollowStartWaitTimeoutMs + " - Check");
                    return -24;
                }

                await Task.Delay(GuardPollIntervalMs, ct).ConfigureAwait(false);
            }

            WriteLog(
                "InputVisionXPrePosition",
                side + " 픽업 퇴장 팔로잉 진입을 시작합니다. leading=" + leadingPickerX.Name +
                ", leadingCommand=" + leadingPickerX.CommandPosition.ToString("F6") +
                ", visionActual=" + visionX.ActualPosition.ToString("F6") +
                ", visionTarget=" + finalTarget.ToString("F6") +
                ", die=" + dieId +
                ", " + gapDetail +
                ", timeoutMs=" + followTimeoutMs + " - Start");

            int followResult = await followVisionX.FollowMoveAsync(
                leadingPickerX,
                leadingPickerX.CommandPosition,
                leadingVelocity,
                leadingAcceleration,
                leadingDeceleration,
                finalTarget,
                trailingVelocity,
                trailingAcceleration,
                trailingDeceleration,
                direction,
                safetyGap,
                homeGap,
                followTimeoutMs,
                ct: ct).ConfigureAwait(false);
            if (followResult != 0)
                return followResult;

            // 도착 검증은 직행 경로(MoveAndVerifyAsync)와 동일 기준을 유지한다.
            return await stage.WaitInputStageAxisInPosition(
                WaferStageAxis.VisionX,
                finalTarget,
                moveTimeoutMs,
                ct).ConfigureAwait(false);
        }

        #endregion

        private static async Task StopAndDrainMoveTaskAsync(
            BaseAxis axis,
            Task<int> moveTask,
            PickerSequenceSide side)
        {
            try
            {
                if (axis != null && axis.IsMoving)
                    axis.Stop();

                if (moveTask == null)
                    return;

                Task completed = await Task.WhenAny(moveTask, Task.Delay(2000)).ConfigureAwait(false);
                if (!object.ReferenceEquals(completed, moveTask))
                {
                    WriteLog(
                        "InputVisionXPrePosition",
                        side + " InputVisionX 감속 정지 후 모션 Task 종료 확인 시간이 초과되었습니다. " +
                        BuildAxisState(axis, axis != null ? axis.CommandPosition : 0.0) + " - Failed");
                    ObserveMoveTask(moveTask, side);
                    return;
                }

                await moveTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                WriteLog(
                    "InputVisionXPrePosition",
                    side + " InputVisionX 감속 정지 후 모션 Task 확인 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool TryResolveSafeStandbyTarget(
            MachineSequenceContext context,
            BaseAxis axis,
            double finalTarget,
            string targetName,
            out double standbyTarget,
            out string detail)
        {
            standbyTarget = axis != null ? axis.ActualPosition : 0.0;
            detail = string.Empty;
            if (axis == null)
            {
                detail = "axis=null";
                return false;
            }

            double start = axis.ActualPosition;
            double travel = finalTarget - start;
            if (Math.Abs(travel) <= MinimumStandbyTravelMm)
            {
                detail = "최종 이동 거리가 너무 짧습니다. travel=" + travel.ToString("F6");
                return false;
            }

            double low = 0.0;
            double high = 1.0;
            string lastAllowedReason = string.Empty;
            string lastBlockedReason = string.Empty;

            for (int i = 0; i < StandbySearchIterations; i++)
            {
                double ratio = (low + high) * 0.5;
                double probe = start + (travel * ratio);
                string reason;
                if (CanMoveToTarget(context, axis, probe, targetName, out reason))
                {
                    low = ratio;
                    lastAllowedReason = reason;
                }
                else
                {
                    high = ratio;
                    lastBlockedReason = reason;
                }
            }

            double boundary = start + (travel * low);
            double direction = Math.Sign(travel);
            double backoff = Math.Min(StandbyBoundaryBackoffMm, Math.Abs(boundary - start) * 0.5);
            double candidate = boundary - (direction * backoff);

            if (Math.Abs(candidate - start) < MinimumStandbyTravelMm)
            {
                detail = "안전 경계까지 이동 가능 거리가 부족합니다. " +
                         "start=" + start.ToString("F6") +
                         ", boundary=" + boundary.ToString("F6") +
                         ", blocked=" + lastBlockedReason;
                return false;
            }

            string candidateReason;
            if (!CanMoveToTarget(context, axis, candidate, targetName, out candidateReason))
            {
                detail = "Backoff 적용 대기점이 가드를 통과하지 못했습니다. candidate=" + candidate.ToString("F6") +
                         ", reason=" + candidateReason;
                return false;
            }

            standbyTarget = candidate;
            detail = "start=" + start.ToString("F6") +
                     ", boundary=" + boundary.ToString("F6") +
                     ", standby=" + standbyTarget.ToString("F6") +
                     ", final=" + finalTarget.ToString("F6") +
                     ", allowed=" + lastAllowedReason +
                     ", blocked=" + lastBlockedReason;
            return true;
        }

        private static bool CanMoveToTarget(
            MachineSequenceContext context,
            BaseAxis axis,
            double target,
            string targetName,
            out string reason)
        {
            reason = string.Empty;
            if (axis == null)
            {
                reason = "InputVisionX 축이 없습니다.";
                return false;
            }

            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(context != null ? context.Machine : null);
            if (service != null && service.IsSharedRailAxis(axis))
            {
                string sharedRailReason;
                if (!service.VerifySingleAxisMove(axis, target, out sharedRailReason))
                {
                    reason = "SharedRailX: " + sharedRailReason;
                    return false;
                }
            }

            string guardReason;
            if (!MotionGuardRuntime.CanAxisTeachingMove(axis, target, targetName, out guardReason))
            {
                reason = "MotionGuard: " + guardReason;
                return false;
            }

            reason = "Clear";
            return true;
        }

        private static InputStagePickTargetCandidate ResolveNextTargetCandidate()
        {
            List<InputStagePickTargetCandidate> candidates = MaterialStateService.GetReadyInputStagePickTargetCandidates();
            return candidates != null && candidates.Count > 0 ? candidates[0] : null;
        }

        private static MotionProfile ResolveMotionProfile(BaseAxis axis)
        {
            double defaultVelocity = axis != null && axis.Config != null && axis.Config.GetRawDefaultVelocity() > 0.0
                ? axis.Config.GetRawDefaultVelocity()
                : 1.0;
            double defaultAcceleration = axis != null && axis.Config != null && axis.Config.GetRawAcceleration() > 0.0
                ? axis.Config.GetRawAcceleration()
                : 1.0;
            double defaultDeceleration = axis != null && axis.Config != null && axis.Config.GetRawDeceleration() > 0.0
                ? axis.Config.GetRawDeceleration()
                : 1.0;

            return new MotionProfile
            {
                Velocity = MotionSpeedScale.ApplyDefaultVelocityScale(defaultVelocity),
                Acceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(defaultAcceleration),
                Deceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(defaultDeceleration)
            };
        }

        private static int ResolveMoveTimeout(PickerSequenceOptions options)
        {
            return options != null && options.MoveTimeoutMs > 0 ? options.MoveTimeoutMs : 30000;
        }

        private static bool CanSkipMoveCommand(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;
            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return axis.IsAtTargetPosition(target, tolerance);
        }

        private static int LogOptimizationResult(PickerSequenceSide side, string dieId, int result, string mode)
        {
            WriteLog(
                "InputVisionXPrePosition",
                side + " InputVisionX 선행이동이 종료되었습니다. " +
                "die=" + dieId +
                ", mode=" + mode +
                ", result=" + result +
                (result == 0 ? " - Ok" : " - Failed"));
            return result == 0 ? 0 : 0;
        }

        private static string BuildTargetName(InputStagePickTargetCandidate target, string phase)
        {
            return "InputVisionXPrePosition;Phase=" + phase +
                   ";Die=" + (target != null ? Safe(target.DieId) : "-");
        }

        private static string BuildAxisState(BaseAxis axis, double target)
        {
            if (axis == null)
                return "axis=null";
            return "axis=" + axis.Name +
                   ", servo=" + axis.IsServoOn +
                   ", alarm=" + axis.IsAlarm +
                   ", moving=" + axis.IsMoving +
                   ", actual=" + axis.ActualPosition.ToString("F6") +
                   ", command=" + axis.CommandPosition.ToString("F6") +
                   ", target=" + target.ToString("F6");
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private sealed class MotionProfile
        {
            public double Velocity { get; set; }
            public double Acceleration { get; set; }
            public double Deceleration { get; set; }
        }

        private static void ObserveMoveTask(Task<int> task, PickerSequenceSide side)
        {
            if (task == null)
                return;

            task.ContinueWith(
                completed =>
                {
                    try
                    {
                        if (completed.IsFaulted && completed.Exception != null)
                        {
                            WriteLog(
                                "InputVisionXPrePosition",
                                side + " 정지된 InputVisionX 선행이동 Task에서 예외가 확인되었습니다. error=" +
                                completed.Exception.GetBaseException().Message + " - Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog("InputVisionXPrePosition", side + " 선행이동 Task 상태 확인 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                    }
                    finally
                    {
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private static void CompleteTask(Task<int> task, CancellationTokenSource cancellation)
        {
            lock (Sync)
            {
                if (object.ReferenceEquals(_runningTask, task))
                {
                    _runningTask = null;
                    _runningCancellation = null;
                }
            }

            try
            {
                if (task != null && task.IsFaulted && task.Exception != null)
                {
                    WriteLog(
                        "InputVisionXPrePosition",
                        "InputVisionX 선행이동 Task가 예외로 종료되었습니다. error=" +
                        task.Exception.GetBaseException().Message + " - Failed");
                }
            }
            catch (Exception ex)
            {
                WriteLog("InputVisionXPrePosition", "InputVisionX 선행이동 완료 처리 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
                if (cancellation != null)
                    cancellation.Dispose();
            }
        }

        private static void ClearCompletedNoLock()
        {
            if (_runningTask == null || !_runningTask.IsCompleted)
                return;

            CancellationTokenSource cancellation = _runningCancellation;
            _runningTask = null;
            _runningCancellation = null;
            if (cancellation != null)
                cancellation.Dispose();
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", source, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "InputVisionX 선행이동 로그 기록 실패. source=" + source +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }
    }
}
