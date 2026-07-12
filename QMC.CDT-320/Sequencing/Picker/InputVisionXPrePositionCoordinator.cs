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
        private const double StandbyBoundaryBackoffMm = 1.0;
        private const double MinimumStandbyTravelMm = 0.2;

        private static readonly object Sync = new object();
        private static Task<int> _runningTask;
        private static CancellationTokenSource _runningCancellation;
        private static PickerSequenceSide _runningSide;

        public static bool EnsureStarted(
            MachineSequenceContext context,
            PickerSequenceSide side,
            PickerSequenceOptions options,
            CancellationToken ct,
            string reason)
        {
            if (context == null || context.Machine == null)
                return false;

            lock (Sync)
            {
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
                    completed => CompleteTask(completed, linkedCancellation),
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

                if (IsAxisInPosition(visionX, finalTarget))
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

                    double standbyTarget;
                    string standbyDetail;
                    if (!TryResolveSafeStandbyTarget(
                        context,
                        visionX,
                        finalTarget,
                        BuildTargetName(target, "Standby"),
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
                        bool overrideIssued = false;
                        int standbyResult;

                        try
                        {
                            while (!activeMoveTask.IsCompleted)
                            {
                                ct.ThrowIfCancellationRequested();
                                context.StopIfCycleStopRequested("InputVisionXPrePosition.Standby:" + side);

                                if (CanMoveToTarget(context, visionX, finalTarget, finalTargetName, out finalGuardReason))
                                {
                                    int overrideResult = TryOverrideMovingAxisToFinal(
                                        visionX,
                                        finalTarget,
                                        motion,
                                        out string overrideDetail);

                                    if (overrideResult == 0)
                                    {
                                        overrideIssued = true;
                                        WriteLog(
                                            "InputVisionXPrePosition",
                                            side + " InputVisionX 이동 중 최종 die 위치로 Position Override를 적용했습니다. " +
                                            "die=" + target.DieId +
                                            ", finalX=" + finalTarget.ToString("F6") +
                                            ", detail=" + overrideDetail + " - Ok");
                                        break;
                                    }

                                    if (!visionX.IsMoving)
                                    {
                                        WriteLog(
                                            "InputVisionXPrePosition",
                                            side + " 최종 가드 해제 시점에 InputVisionX가 이미 정지해 일반 Move로 전환합니다. " +
                                            "die=" + target.DieId +
                                            ", overrideResult=" + overrideResult +
                                            ", detail=" + overrideDetail + " - Check");
                                        break;
                                    }

                                    WriteLog(
                                        "InputVisionXPrePosition",
                                        side + " InputVisionX Position Override가 실패했고 축이 계속 이동 중이라 선행이동을 중단합니다. " +
                                        "die=" + target.DieId +
                                        ", overrideResult=" + overrideResult +
                                        ", detail=" + overrideDetail + " - Failed");
                                    await StopAndDrainMoveTaskAsync(visionX, activeMoveTask, side).ConfigureAwait(false);
                                    activeMoveTask = null;
                                    stopActiveMoveOnExit = false;
                                    return 0;
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
                                ", " + BuildAxisState(visionX, overrideIssued ? finalTarget : standbyTarget) + " - Failed");
                            return 0;
                        }

                        if (overrideIssued)
                        {
                            int finalWait = await stage.WaitInputStageAxisInPosition(
                                WaferStageAxis.VisionX,
                                finalTarget,
                                moveTimeoutMs,
                                ct).ConfigureAwait(false);
                            return LogOptimizationResult(side, target.DieId, finalWait, "PositionOverride");
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
                if (stopActiveMoveOnExit && visionX != null && visionX.IsMoving)
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

                if (IsAxisInPosition(visionX, finalTarget))
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

                    if (IsAxisInPosition(visionX, finalTarget))
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

        private static int TryOverrideMovingAxisToFinal(
            BaseAxis axis,
            double target,
            MotionProfile motion,
            out string detail)
        {
            double previousActual = axis != null ? axis.ActualPosition : 0.0;
            double previousCommand = axis != null ? axis.CommandPosition : 0.0;

            if (axis == null)
            {
                detail = "axis=null";
                return -1;
            }

            if (!axis.IsMoving)
            {
                detail = "moving=False, previousActual=" + previousActual.ToString("F6") +
                         ", previousCommand=" + previousCommand.ToString("F6");
                return -4;
            }

            int result;
            AjinAxis ajinAxis = axis as AjinAxis;
            if (ajinAxis != null)
            {
                result = ajinAxis.TryOverridePosition(
                    target,
                    motion.Velocity,
                    motion.Acceleration,
                    motion.Deceleration);
            }
            else
            {
                axis.OverridePosition(target);
                result = 0;
            }

            detail = "result=" + result +
                     ", previousActual=" + previousActual.ToString("F6") +
                     ", previousCommand=" + previousCommand.ToString("F6") +
                     ", newActual=" + axis.ActualPosition.ToString("F6") +
                     ", newCommand=" + axis.CommandPosition.ToString("F6") +
                     ", velocity=" + motion.Velocity.ToString("F6") +
                     ", acceleration=" + motion.Acceleration.ToString("F6") +
                     ", deceleration=" + motion.Deceleration.ToString("F6") +
                     ", moving=" + axis.IsMoving;
            return result;
        }

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
            double defaultVelocity = axis != null && axis.Config != null && axis.Config.DefaultVelocity > 0.0
                ? axis.Config.DefaultVelocity
                : 1.0;
            double defaultAcceleration = axis != null && axis.Config != null && axis.Config.Acceleration > 0.0
                ? axis.Config.Acceleration
                : 1.0;
            double defaultDeceleration = axis != null && axis.Config != null && axis.Config.Deceleration > 0.0
                ? axis.Config.Deceleration
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

        private static bool IsAxisInPosition(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;
            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return !axis.IsMoving && Math.Abs(axis.ActualPosition - target) <= tolerance;
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
