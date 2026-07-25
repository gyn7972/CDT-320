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
        // R4(follow-entry): 보관된 비동기 회피 Task를 join하고 필드를 비운다. 없으면 0.
        private async Task<int> JoinOutputVisionRetreatMoveTaskAsync(string context, CancellationToken ct)
        {
            Task<int> moveTask = _outputVisionRetreatMoveTask;
            if (moveTask == null)
                return 0;

            _outputVisionRetreatMoveTask = null;
            try
            {
                int result = await moveTask.ConfigureAwait(false);
                if (result != 0)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " OutputVisionX 비동기 최소 회피 이동이 실패로 종료되었습니다. " +
                        "context=" + (context ?? string.Empty) +
                        ", result=" + result + " - Check");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputVisionX 비동기 최소 회피 이동이 취소 상태로 정리되었습니다. " +
                    "context=" + (context ?? string.Empty) + " - Check");
                ct.ThrowIfCancellationRequested();
                return -1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputVisionX 비동기 최소 회피 이동 정리 중 예외가 발생했습니다. " +
                    "context=" + (context ?? string.Empty) +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
        }

        // R4(follow-entry): Abort 등 동기 경로에서 회피 Task를 관찰(observe)만 하고 흘려보낸다.
        private void ObserveOutputVisionRetreatMoveTaskOnAbort()
        {
            Task<int> moveTask = _outputVisionRetreatMoveTask;
            _outputVisionRetreatMoveTask = null;
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

        // 기존 조건(2026-07-26 초판): IsPairClearanceSatisfied(Safety 10mm만, Extra 미포함)로
        //   인수 충분성을 판정했다 — 팔로잉 유지 간격(Safety+Extra=50mm)보다 40mm 느슨해,
        //   선회피 게시값(≈748.3) 인수 후 피커 팔로잉이 목표 40mm 앞에서 영구 클램프되어
        //   -21 타임아웃으로 빠지는 창이 있었다(적대적 검증 확정 2026-07-26 critical).
        //   (수정 전에는 외부 목표가 항상 no-op 좌표라 판정이 항상 false → 창이 없었음.)
        // 현재 기준: 판정 요구거리를 팔로잉과 동일한 safetyGap(Safety+Extra)+1mm(경계 여유)로
        //   맞춘다 — 인수한 목표에서 피커가 배치 목표까지 팔로잉 클램프 없이 완주 가능함을 보장.
        //   파라미터 조회 실패 시 false(연장 회피 경로 폴백 — 안전).
        private bool IsOutputVisionTargetClearOfPlannedPickerTargets(
            double visionTarget,
            IList<double> plannedPickerTargets)
        {
            try
            {
                if (OutputStage == null || OutputStage.OutputCameraX == null ||
                    plannedPickerTargets == null || plannedPickerTargets.Count == 0)
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                if (service == null || pickerX == null)
                    return false;

                int direction;
                double homeGap;
                double safetyGap;
                string gapDetail;
                if (!service.TryGetFollowGapParameters(
                    OutputStage.OutputCameraX,
                    pickerX,
                    service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0,
                    out direction,
                    out homeGap,
                    out safetyGap,
                    out gapDetail))
                {
                    return false;
                }

                double required = safetyGap +
                    VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm - 0.000001;
                for (int i = 0; i < plannedPickerTargets.Count; i++)
                {
                    double picker = plannedPickerTargets[i];
                    // FollowMoveAsync와 동일한 페어식 간격.
                    double clearance = direction > 0
                        ? (picker + homeGap) - visionTarget
                        : (visionTarget + homeGap) - picker;
                    if (clearance < required)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // 현재 기준(사용자 승인 2026-07-26, 3번): 실위치 도착 대기 — 이동 Task 완료 신호를
        // 신뢰하지 않고 OutputVisionX가 실제 목표에 정지 도달할 때까지 폴링한다(속도 스케일 반영
        // 타임아웃 상한). 무알람 — 실패 시 로그+음수 코드만 반환하고 알람 판정은 전경 join
        // 지점이 담당한다. Delay에 ct를 걸지 않는다(검증 결과 반영) — 시퀀스 종료 후 dispose된
        // linked CTS 토큰으로 Task.Delay가 ObjectDisposedException을 던지는 것을 방지
        // (취소는 IsCancellationRequested 값 확인으로만 반영).
        private async Task<int> WaitOutputVisionXArrivalAsync(double target, string description, CancellationToken ct)
        {
            BaseAxis visionX = OutputStage != null ? OutputStage.OutputCameraX : null;
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
                    WriteLog("PickerPlaceSequence",
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
        // 가정이 항상 참이 아니므로(취소/타임아웃 시 축이 여전히 주행 중일 수 있음),
        // 이동 중인 축에 재명령해 보드 busy 거부→가짜 축 알람이 되는 것을 막는다.
        // 정지 확인에 실패하면 재기동 명령을 내지 않고 음수 코드를 반환한다.
        private async Task<int> WaitOutputVisionXStoppedQuietAsync(string description, CancellationToken ct)
        {
            BaseAxis visionX = OutputStage != null ? OutputStage.OutputCameraX : null;
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
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description + " 정지 대기가 시간 초과되었습니다. 재기동 명령을 내지 않습니다. " +
                        "actual=" + visionX.ActualPosition.ToString("F6") +
                        ", timeoutMs=" + timeoutMs + " - Failed");
                    return -3;
                }

                await Task.Delay(10).ConfigureAwait(false);
            }

            return 0;
        }

        // 보강(사용자 승인 2026-07-26, 3번): 자체 회피 합성 — 이동(완료 보장형) 후 실위치 도착까지
        // 확인해 이 Task의 완료가 곧 도착을 의미하게 한다.
        private async Task<int> RunOwnOutputVisionRetreatAsync(double target, CancellationToken ct)
        {
            int result = await MoveOutputStageAxisAndVerifyAsync(
                BinStageAxis.VisionX,
                target,
                "OutputVisionX 최소 회피 위치 이동",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await WaitOutputVisionXArrivalAsync(target, "자체 회피 도착 확인", ct).ConfigureAwait(false);
        }

        // 현재 기준(사용자 승인 2026-07-26, 3번): 인수한 독립 회피(목표 충분)의 도착 동기화 합성 —
        // 외부 Task join 후 실위치 도착 대기까지 확인한다. 외부가 실패로 종료됐으면 축이 정지한
        // 상태이므로 외부 목표로 자체 재기동한다.
        private async Task<int> AwaitExternalOutputVisionArrivalAsync(
            Task<int> externalTask,
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
                WriteLog("PickerPlaceSequence",
                    Name + " 인수한 후검사 독립 회피가 취소되어 자체 재기동합니다. - Check");
                externalResult = -1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " 인수한 후검사 독립 회피 대기 중 예외 — 자체 재기동합니다. error=" +
                    ex.Message + " - Check");
                externalResult = -1;
            }

            if (externalResult != 0)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " 인수한 후검사 독립 회피가 실패로 종료되어 외부 목표로 자체 재기동합니다. " +
                    "externalResult=" + externalResult +
                    ", externalTarget=" + externalTarget.ToString("F6") + " - Check");
                // 재기동 전 정지 확인(검증 결과 반영) — 취소/타임아웃 시 축이 주행 중일 수 있다
                // (SequenceAwaiter는 취소 시 이동 Task를 버리고 OCE를 던져 축은 계속 주행).
                int stopResult = await WaitOutputVisionXStoppedQuietAsync(
                    "인수 회피 실패 후 재기동 전", ct).ConfigureAwait(false);
                if (stopResult != 0)
                    return stopResult;

                return await RunOwnOutputVisionRetreatAsync(externalTarget, ct).ConfigureAwait(false);
            }

            return await WaitOutputVisionXArrivalAsync(externalTarget, "인수한 독립 회피 도착 확인", ct).ConfigureAwait(false);
        }

        // 현재 기준(사용자 지시 2026-07-26): 외부 독립 회피 목표가 부족할 때의 합성 회피 —
        // 외부 Task 완료를 기다린 뒤 정확 좌표로 연장 회피를 수행한다.
        // 보강(사용자 승인 2026-07-26, 3번): 외부 Task가 성공(0)으로 끝났어도 실위치 도착까지
        // 확인한 뒤에 연장 명령을 낸다 — 이동 중인 축에 재명령하는 것을 방지. 외부가 실패로
        // 종료됐으면 축이 정지한 상태이므로 즉시 연장 이동이 자체 재기동 역할을 한다.
        private async Task<int> ExtendExternalOutputVisionRetreatAsync(
            Task<int> externalTask,
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
                    WriteLog("PickerPlaceSequence",
                        Name + " 인수한 후검사 독립 회피가 실패로 종료되어 연장 회피가 자체 재기동합니다. " +
                        "externalResult=" + externalResult + " - Check");
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " 인수한 후검사 독립 회피가 취소되어 연장 회피가 자체 재기동합니다. - Check");
                externalResult = -1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " 인수한 후검사 독립 회피 대기 중 예외 — 연장 회피가 자체 재기동합니다. error=" +
                    ex.Message + " - Check");
                externalResult = -1;
            }

            if (externalResult == 0)
            {
                int arrivalResult = await WaitOutputVisionXArrivalAsync(
                    externalTarget, "외부 독립 회피 도착 확인(연장 전)", ct).ConfigureAwait(false);
                if (arrivalResult != 0)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " 외부 독립 회피 도착 확인에 실패했지만 연장 회피가 재기동합니다. " +
                        "arrivalResult=" + arrivalResult +
                        ", externalTarget=" + externalTarget.ToString("F6") + " - Check");
                }
            }

            // 재기동 전 정지 확인(검증 결과 반영) — 이동 중 축 재명령(보드 busy→가짜 축 알람) 방지.
            int stopBeforeExtend = await WaitOutputVisionXStoppedQuietAsync(
                "연장 회피 재기동 전", ct).ConfigureAwait(false);
            if (stopBeforeExtend != 0)
                return stopBeforeExtend;

            return await RunOwnOutputVisionRetreatAsync(exactTarget, ct).ConfigureAwait(false);
        }

        // O-2-F: follow를 타지 않는 일반 이동 분기 공통 방어 — 진행 중인 회피 Task(자체/외부
        // 인수/합성)가 있으면 완료(join)까지 확인한다. 회피 미완료 상태에서 비전이 아직 촬영/검사
        // 위치에 있는데 피커 X가 일반 이동으로 Output 존에 진입하면 인터락(-11)에 걸리기 때문이다.
        // Task가 없으면 0(무동작).
        // 보강(사용자 승인 2026-07-26, 3번): join 성공 후에도 실위치 도착을 재확인한다 —
        // 이동 Task가 실제 도착 전에 완료 상태가 되는 사례 방어(미도착이면 도착 대기 후 진입).
        private async Task<int> JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(CancellationToken ct)
        {
            if (_outputVisionRetreatMoveTask == null)
                return 0;

            int joinResult = await JoinOutputVisionRetreatMoveTaskAsync("일반 이동 전 회피 완료", ct).ConfigureAwait(false);
            if (joinResult != 0)
                return joinResult;

            BaseAxis visionX = OutputStage != null ? OutputStage.OutputCameraX : null;
            double arrivalTolerance = visionX != null && visionX.Config != null && visionX.Config.InPositionTolerance > 0.0
                ? visionX.Config.InPositionTolerance
                : 0.01;
            if (visionX != null && !visionX.IsAtTargetPosition(_outputVisionRetreatTarget, arrivalTolerance))
            {
                WriteLog("PickerPlaceSequence",
                    Name + " 회피 Task join 후 실위치 미도착 감지 — 일반 X 진입 전 도착을 대기합니다. " +
                    "target=" + _outputVisionRetreatTarget.ToString("F6") +
                    ", actual=" + visionX.ActualPosition.ToString("F6") + " - Check");
                return await WaitOutputVisionXArrivalAsync(
                    _outputVisionRetreatTarget, "일반 이동 전 회피 도착 확인", ct).ConfigureAwait(false);
            }

            return 0;
        }

        // 기존 조건(R4 follow-entry): 이미 시작된 회피 Task가 살아 있고 비전이 이동 중(IsMoving)일 때만 follow.
        // 현재 기준(사용자 지시 2026-07-26, 이연 폐지): 회피 Task(자체/인수/합성)가 미완료면 follow.
        //   합성 회피의 외부 구간처럼 Task가 살아 있어도 아직 출발 전(IsMoving=false)일 수 있으므로
        //   "미완료" 판정을 우선하고, 완료 직후 잔여 감속 구간은 기존 IsMoving 조건이 보완한다.
        private bool ShouldFollowOutputVisionRetreatForPickerEntry()
        {
            return OutputStage != null &&
                   OutputStage.OutputCameraX != null &&
                   _outputVisionRetreatMoveTask != null &&
                   (!_outputVisionRetreatMoveTask.IsCompleted || OutputStage.OutputCameraX.IsMoving);
        }

        // R4(follow-entry): MovePickerXTThenYAndVerifyAsync 구조를 미러 — X만 follow(+R6 폴백)로
        // 분리하고 T는 병렬 단독 이동, X/T 완료 후 PickerY 전진(스킵 판정 동일).
        private async Task<int> MovePlacePickerXTThenYWithVisionFollowAsync(
            IDictionary<PickerAxis, double> targets,
            string targetName,
            CancellationToken ct)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            double pickerYTarget;
            bool hasPickerY = targets.TryGetValue(PickerAxis.PickerY, out pickerYTarget);

            var tTargets = new Dictionary<PickerAxis, double>();
            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                if (pair.Key == PickerAxis.PickerX || pair.Key == PickerAxis.PickerY)
                    continue;

                tTargets[pair.Key] = pair.Value;
            }

            Task<int> pickerXEntryMove = MovePlacePickerXEntryByVisionFollowOrFallbackAsync(
                "place picker X 팔로잉 진입",
                targetName,
                ct);
            Task<int> pickerTMove = MovePickerAxesAndVerifyAsync(
                tTargets,
                "place picker T",
                ct,
                targetName);

            int[] results = await Task.WhenAll(pickerXEntryMove, pickerTMove).ConfigureAwait(false);
            int xtResult = results[0] != 0 ? results[0] : results[1];
            if (xtResult != 0)
                return xtResult;

            if (!hasPickerY || CanSkipPickerMoveCommand(PickerAxis.PickerY, pickerYTarget))
                return 0;

            WriteLog("PickerMove",
                Name + " place picker X(follow)/T 이동 완료 후 PickerY 전진을 시작합니다. " +
                "targetY=" + pickerYTarget +
                ", targetName=" + (targetName ?? "-") +
                " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                pickerYTarget,
                "place picker Y",
                ct,
                targetName).ConfigureAwait(false);
        }

        // R6(follow-entry): follow 실패 시 — 함수가 피커 정지를 보장하므로 비전 회피 Task를
        // join(observe)한 뒤 기존 일반 이동(공유레일 대기 게이트 포함 순차 경로)으로 1회 재시도한다.
        private async Task<int> MovePlacePickerXEntryByVisionFollowOrFallbackAsync(
            string description,
            string targetName,
            CancellationToken ct)
        {
            // O-2-E(사용자 지시 2026-07-26, 이연 폐지): 회피 Task(자체/외부 인수/합성)는 회피 좌표
            // 확정 시점에 이미 기동되어 있다 — 여기서는 곧바로 FollowMoveAsync로 추종 진입한다.
            // 아래 R6 폴백(join + 일반 이동)은 Task가 이미 시작돼 있어 기존대로 동작한다.
            int followResult = await TryFollowPickerXBehindOutputVisionRetreatAsync(targetName, ct).ConfigureAwait(false);
            if (followResult == 0)
                return 0;

            int retreatJoin = await JoinOutputVisionRetreatMoveTaskAsync(
                description + " follow 폴백",
                ct).ConfigureAwait(false);
            WriteLog("PickerPlaceSequence",
                Name + " " + description + " 팔로잉 진입이 실패해 기존 일반 이동으로 재시도합니다. " +
                "followResult=" + followResult +
                ", retreatJoin=" + retreatJoin + " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                description + " (follow 폴백)",
                ct,
                targetName).ConfigureAwait(false);
        }

        // R4/R5(follow-entry): 피커X(후행)가 회피 중인 OutputVisionX(선행)를 추종 진입한다.
        // homeGap/safetyGap/direction/timeout 전부 SharedRailX 설정에서 런타임 조회(하드코딩 금지).
        // 안전 근거: 팔로잉 유지 간격(safetyGap=SafetyDistance+OutputExtra, 기본 50) > 인터락 요구
        // (SafetyDistance, 기본 10)이므로 정상 추종 중 인터락 거부는 없다. 그럼에도 -11이면 R6 폴백.
        // 인터락 통과 체인: FollowMoveAsync 내부 MoveAbsoluteAsync→BaseAxis.VerifyMotionGuard→
        // MotionGuardRuntime.VerifyAxisMove(SharedRailX 포함) / TryOverridePosition→
        // MotionGuardRuntime.VerifyAxisTeachingMove — 우회 API 미사용.
        private async Task<int> TryFollowPickerXBehindOutputVisionRetreatAsync(string targetName, CancellationToken ct)
        {
            BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
            AjinAxis followPickerX = pickerX as AjinAxis;
            BaseAxis visionX = OutputStage != null ? OutputStage.OutputCameraX : null;
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
                service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0,
                out direction,
                out homeGap,
                out safetyGap,
                out gapDetail))
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place 피커X 팔로잉 파라미터 조회에 실패해 일반 이동으로 진행합니다. " +
                    "detail=" + gapDetail + " - Check");
                return -1;
            }

            // C2(2026-07-26): 타임아웃은 100% 기준 설정값이므로 속도 스케일 역수로 확장한다(저속 오탐 -21 방지).
            int timeoutMs = MotionSpeedScale.ScaleDefaultTimeoutMs(
                service.Config != null ? service.Config.VisionFollowEntryTimeoutMs : 15000);
            // 현재 기준: follow의 명령/오버라이드 경로는 축 레이어 자동 스케일이 없으므로 여기서 1회 스케일.
            double trailingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                pickerX.Config != null ? pickerX.Config.DefaultVelocity : 0.0);
            double trailingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                pickerX.Config != null ? pickerX.Config.Acceleration : 0.0);
            double trailingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                pickerX.Config != null ? pickerX.Config.Deceleration : 0.0);
            double leadingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                visionX.Config != null ? visionX.Config.DefaultVelocity : 0.0);
            double leadingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                visionX.Config != null ? visionX.Config.Acceleration : 0.0);
            double leadingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                visionX.Config != null ? visionX.Config.Deceleration : 0.0);

            WriteLog("PickerPlaceSequence",
                Name + " Place 피커X 팔로잉 진입을 시작합니다. leading=" + visionX.Name +
                ", visionTarget=" + _outputVisionRetreatTarget.ToString("F6") +
                ", pickerTarget=" + _targetPickerX.ToString("F6") +
                ", " + gapDetail +
                ", timeoutMs=" + timeoutMs + " - Start");

            return await followPickerX.FollowMoveAsync(
                visionX,
                _outputVisionRetreatTarget,
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
                BuildFollowEntryTargetName(targetName, PickerWorkZone.Output),
                ct).ConfigureAwait(false);
        }

    }
}
