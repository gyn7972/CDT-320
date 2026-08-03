using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing.Safety;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>장비 전체 모션을 안전한 Ready(Avoid) 위치로 복귀시키는 시퀀스입니다.</summary>
    internal sealed class MachineReadySequence
    {
        #region 실행 구성·Step 등록 및 진행 제어

        // ─────────────────────────────────────────────────────────────────────────────
        // [옵션 2026-07-29 / 테스트용 하드코딩] Ready 피더 후퇴에서 "제품 보유 시" +방향 이탈 거리(mm).
        //
        // 동작 분기 (사용자 지시 2026-07-29):
        //   · 제품 보유 X → 이 이동 없이 곧바로 Lift Up
        //   · 제품 보유 O → Unclamp → +방향 이동 → 재판정 → 비워졌으면 Lift Up
        //                    여전히 물고 있으면 Lift Up 하지 않고 차단(피더 파손 방지)
        //
        //   0    = 이탈 이동을 시도하지 않는다. 보유 시 기존처럼 즉시 차단.  ← 현재 기본값
        //   20   = 보유 시 +20mm 이탈을 시도한 뒤 판정
        //
        // ★Ready 시퀀스에서만 적용된다★ 생산 시퀀스와 개별 Recover(InputFeederRecoverSequence /
        //   OutputFeederRecoverSequence)는 FeederRetreatPolicy 기본값 0 으로 호출하므로 영향이 없다.
        //
        // ★안전 주의★
        //   · 이 이동은 Lift Down + 제품 보유 상태에서 Y 가 움직이는 유일한 구간이다. 거리를 짧게.
        //   · 이동 후 반드시 재판정하며, "비어 있음"이 확인되지 않으면 절대 Lift Up 하지 않는다.
        //   · InputFeederY SoftLimitPlus=629.78, Load/Unload=607.72 → +20mm 시 여유 2.06mm 뿐이다.
        //     실제 이동량은 어댑터에서 소프트리밋 -0.5mm 안쪽으로 클램프되고, 여유가 없으면 생략된다.
        //   · 실장비 검증 후 값을 조정할 것. 검증 전에는 0 을 유지한다.
        // ─────────────────────────────────────────────────────────────────────────────
        private const double ReadyRetreatBackOffMm = 0.0;

        private readonly CDT320_Machine _machine;
        private readonly Action<MachineReadyProgress> _progressChanged;
        private readonly List<ReadyStep> _steps;
        private int _completedStepCount;

        public MachineReadySequence(CDT320_Machine machine)
            : this(machine, null)
        {
        }

        public MachineReadySequence(CDT320_Machine machine, Action<MachineReadyProgress> progressChanged)
        {
            _machine = machine;
            _progressChanged = progressChanged;
            _steps = BuildReadySteps();
        }

        public string LastErrorMessage { get; private set; }

        /// <summary>현재 활성화된 Ready 단계 수. 단계 추가/주석 해제 시 자동으로 반영된다.</summary>
        public int TotalStepCount
        {
            get { return _steps != null ? _steps.Count : 0; }
        }

        /// <summary>
        /// 현재 활성 Ready 단계 목록. 동작 흐름(순서/실패 시 중단)은 기존과 동일하다.<br/>
        /// 비활성 단계는 주석으로 보관하며, 주석을 해제하면 <see cref="TotalStepCount"/> 가 자동으로 늘어난다.
        /// </summary>
        private List<ReadyStep> BuildReadySteps()
        {
            var steps = new List<ReadyStep>();

            AddReadyStep(steps, ReadyStepId.InputStageNeedleEjectZAvoid, "InputStage Needle/Eject Z Avoid", MoveInputStageNeedleEjectZAvoidAsync);
            AddReadyStep(steps, ReadyStepId.UpperHeadMoveSafetyCheck, "Upper Head Move Safety Check", CheckUpperHeadMoveSafetyAsync);
            // 현재 기준: Ready 복귀는 Picker Z/Y를 먼저 빼고 Input/Output VisionX를 Avoid로 이동한다.
            AddReadyStep(steps, ReadyStepId.PickerZAvoid, "Front/Rear Picker Z Avoid", MoveFrontRearPickerZAxesAvoidAsync);
            AddReadyStep(steps, ReadyStepId.PickerYAvoid, "Front/Rear Picker Y Avoid", MoveFrontRearPickerYAxesAvoidAsync);
            AddReadyStep(steps, ReadyStepId.OutputVisionXAvoid, "Input/Output VisionX Avoid", MoveInputOutputVisionXOnlyAvoidAsync);
            AddReadyStep(steps, ReadyStepId.ReticleAvoid, "Reticle Avoid", MoveReticleAvoidAsync);
            AddReadyStep(steps, ReadyStepId.PickerTAvoid, "Front/Rear Picker T Avoid", MoveFrontRearPickerTAxesAvoidAsync);
            AddReadyStep(steps, ReadyStepId.PickerXAvoid, "Front/Rear Picker X Avoid", MoveFrontRearPickerXAxesAvoidAsync);

            // 우선 아래는 확인 하면서 활성화하자. 주석 해제 시 TotalStepCount 가 자동으로 반영된다.
            //AddReadyStep(steps, ReadyStepId.OutputStageAvoid, "OutputStage Avoid", MoveOutputStageAvoidAsync);
            //AddReadyStep(steps, ReadyStepId.FrontPickerAvoid, "Front Picker Avoid", MoveFrontPickerAvoidAsync);
            //AddReadyStep(steps, ReadyStepId.RearPickerAvoid, "Rear Picker Avoid", MoveRearPickerAvoidAsync);
            //AddReadyStep(steps, ReadyStepId.SideVisionAvoid, "Side Vision Avoid", MoveSideVisionAvoidAsync);
            //AddReadyStep(steps, ReadyStepId.InputStageAvoid, "InputStage Avoid", MoveInputStageAvoidAsync);
            //AddReadyStep(steps, ReadyStepId.InputFeederAvoid, "Input Feeder Avoid", MoveInputFeederAvoidAsync);
            //AddReadyStep(steps, ReadyStepId.OutputFeederAvoid, "Output Feeder Avoid", MoveOutputFeederAvoidAsync);
            //AddReadyStep(steps, ReadyStepId.InputCassetteAvoid, "Input Cassette Avoid", MoveInputCassetteAvoidAsync);
            //AddReadyStep(steps, ReadyStepId.OutputCassetteAvoid, "Output Cassette Avoid", MoveOutputCassetteAvoidAsync);

            return steps;
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            using (MotionGuardRuntime.BeginManualSequenceProcessMove("MachineReadySequence.RunAsync"))
            {
            try
            {
                LastErrorMessage = string.Empty;
                _completedStepCount = 0;
                LogStep("Ready 시퀀스 시작.");

                // 활성 단계를 순서대로 실행한다. 한 단계라도 실패하면 즉시 중단한다(기존 흐름 동일).
                foreach (ReadyStep step in _steps)
                {
                    int result = await RunReadyStepAsync(step, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                int clearResult = ClearPickerWorkAreaStateAfterReady();
                if (clearResult != 0)
                    return clearResult;

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
        }

        private int ClearPickerWorkAreaStateAfterReady()
        {
            try
            {
                string detail;
                if (!PickerZoneInterlockRules.ClearPickerWorkAreasForReadyIfSafe(_machine, out detail))
                {
                    return Fail(
                        "READY-PICKER-WORK-STATE-CLEAR",
                        "MachineReadySequence",
                        "Ready 완료 후 Picker 작업 점유 상태 해제 실패: " + detail);
                }

                LogStep(detail);
                return 0;
            }
            catch (Exception ex)
            {
                return Fail(
                    "READY-PICKER-WORK-STATE-CLEAR-EX",
                    "MachineReadySequence",
                    "Ready 완료 후 Picker 작업 점유 상태 해제 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RunReadyStepAsync(ReadyStep step, CancellationToken ct)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string stepLabel = step.Label;
            LogStep(stepLabel + " 시작.");
            ReportProgress(MachineReadySequenceState.Running, stepLabel, stepLabel + " 진행 중입니다.");

            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await step.Action(ct).ConfigureAwait(false);
                stopwatch.Stop();

                if (result == 0)
                {
                    _completedStepCount++;
                    LogStep(stepLabel + " 완료. elapsedMs=" + stopwatch.ElapsedMilliseconds);
                    ReportProgress(MachineReadySequenceState.Running, stepLabel, stepLabel + " 완료.");
                    return 0;
                }

                string failure = stepLabel + " 실패. result=" + result + ", elapsedMs=" + stopwatch.ElapsedMilliseconds;
                LastErrorMessage = string.IsNullOrWhiteSpace(LastErrorMessage)
                    ? failure
                    : failure + ", cause=" + LastErrorMessage;
                Log.Write("Main", "SYSTEM", "MachineReadySequence", LastErrorMessage + " - Failed");
                ReportProgress(MachineReadySequenceState.Failed, stepLabel, LastErrorMessage);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                LogStep(stepLabel + " 정지 요청. elapsedMs=" + stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return Fail(
                    "READY-STEP-EX",
                    "MachineReadySequence",
                    stepLabel + " 예외 발생. elapsedMs=" + stopwatch.ElapsedMilliseconds + ", reason=" + ex.Message);
            }
            finally
            {
            }
        }

        private void ReportProgress(MachineReadySequenceState state, string stepName, string message)
        {
            Action<MachineReadyProgress> handler = _progressChanged;
            if (handler == null)
                return;

            int total = TotalStepCount;
            int percent = total <= 0 ? 0 : (int)Math.Round((_completedStepCount * 100.0) / total);
            handler(new MachineReadyProgress(
                state,
                percent,
                _completedStepCount,
                total,
                stepName,
                message));
        }

        private static void AddReadyStep(
            List<ReadyStep> steps,
            ReadyStepId id,
            string name,
            Func<CancellationToken, Task<int>> action)
        {
            if (steps == null)
                return;

            steps.Add(new ReadyStep(steps.Count + 1, id, name, action));
        }

        private enum ReadyStepId
        {
            InputStageNeedleEjectZAvoid,
            UpperHeadMoveSafetyCheck,
            OutputVisionXAvoid,
            ReticleAvoid,
            PickerZAvoid,
            PickerYAvoid,
            PickerTAvoid,
            PickerXAvoid,
            InputVisionXAvoid,
            OutputStageAvoid,
            FrontPickerAvoid,
            RearPickerAvoid,
            SideVisionAvoid,
            InputStageAvoid,
            InputFeederAvoid,
            OutputFeederAvoid,
            InputCassetteAvoid,
            OutputCassetteAvoid
        }

        private struct ReadyStep
        {
            public readonly int No;
            public readonly ReadyStepId Id;
            public readonly string Name;
            public readonly Func<CancellationToken, Task<int>> Action;

            public ReadyStep(int no, ReadyStepId id, string name, Func<CancellationToken, Task<int>> action)
            {
                No = no;
                Id = id;
                Name = name;
                Action = action;
            }

            public string Label
            {
                get { return "ReadyStep " + No + "/" + Id + " [" + Name + "]"; }
            }
        }

        #endregion

        #region 활성 Ready 복귀 동작 및 병렬 작업 보조

        private async Task<int> MoveReticleAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                VisionUnit vision = _machine != null ? _machine.VisionUnit : null;
                if (vision == null)
                    return Skip("VisionUnit");

                if (MotionGuardRuleHelpers.IsReticleRetracted(_machine))
                {
                    LogStep("Reticle이 이미 안전 복귀 상태입니다. " + MotionGuardRuleHelpers.BuildReticleStateDetail(vision));
                    return 0;
                }

                LogStep("Reticle 안전 복귀 시작. 순서=Rear Back -> Front Back -> Lift Down, " +
                    MotionGuardRuleHelpers.BuildReticleStateDetail(vision));

                int result = await vision.SetReticleRearSideForwardAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-RETICLE-REAR-BACK", "VisionUnit", "Ready Reticle Rear Back 이동 실패. result=" + result);

                result = await vision.SetReticleFrontSideForwardAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-RETICLE-FRONT-BACK", "VisionUnit", "Ready Reticle Front Back 이동 실패. result=" + result);

                result = await vision.SetReticleLiftUpAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("READY-RETICLE-DOWN", "VisionUnit", "Ready Reticle Down 이동 실패. result=" + result);

                if (!MotionGuardRuleHelpers.IsReticleRetracted(_machine))
                {
                    return Fail(
                        "READY-RETICLE-CHECK",
                        "VisionUnit",
                        "Ready Reticle 안전 복귀 최종 확인 실패. " + MotionGuardRuleHelpers.BuildReticleStateDetail(vision));
                }

                LogStep("Reticle 안전 복귀 완료. " + MotionGuardRuleHelpers.BuildReticleStateDetail(vision));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-RETICLE-EX", "VisionUnit", "Ready Reticle 안전 복귀 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputOutputVisionXOnlyAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                LogStep("InputStage VisionX / OutputStage VisionX 동시 Avoid 이동 시작.");

                Task<int> outputTask = MoveOutputStageVisionXOnlyAvoidAsync(ct);
                Task<int> inputTask = MoveInputStageVisionXOnlyAvoidAsync(ct);

                int[] results = await Task.WhenAll(outputTask, inputTask).ConfigureAwait(false);
                if (results[0] != 0)
                    return results[0];
                if (results[1] != 0)
                    return results[1];

                LogStep("InputStage VisionX / OutputStage VisionX 동시 Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "READY-INPUT-OUTPUT-VISION-X-EX",
                    "MachineReadySequence",
                    "InputStage VisionX / OutputStage VisionX 동시 Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputStageVisionXOnlyAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.OutputStageUnit : null;
                if (unit == null)
                    return Skip("OutputStageUnit");

                LogStep("OutputStage VisionX 선행 Avoid 이동 시작.");

                int result = await MoveOutputStageAxisAvoidAsync(unit, BinStageAxis.VisionX, "Output VisionX", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                LogStep("OutputStage VisionX 선행 Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-OUTPUT-STAGE-VISION-X-EX", "OutputStageUnit", "OutputStage VisionX 선행 Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontRearPickerZAxesAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var frontUnit = _machine != null ? _machine.PickerFrontUnit : null;
                var rearUnit = _machine != null ? _machine.PickerRearUnit : null;

                LogStep("Front/Rear Picker Z축 전체 상승 Avoid 이동 시작.");

                var tasks = new List<Task<int>>();
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerZ0, "PickerZ1", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerZ1, "PickerZ2", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerZ2, "PickerZ3", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerZ3, "PickerZ4", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerZ0, "PickerZ1", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerZ1, "PickerZ2", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerZ2, "PickerZ3", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerZ3, "PickerZ4", ct);

                int result = await AwaitReadyAxisTasksAsync(tasks).ConfigureAwait(false);
                if (result != 0)
                    return result;

                LogStep("Front/Rear Picker Z축 전체 상승 Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-PICKER-Z-EX", "MachineReadySequence", "Picker Z축 전체 상승 Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontRearPickerXAxesAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var frontUnit = _machine != null ? _machine.PickerFrontUnit : null;
                var rearUnit = _machine != null ? _machine.PickerRearUnit : null;

                int visionCheck = CheckInputVisionXAvoidBeforeReadyPickerX();
                if (visionCheck != 0)
                    return visionCheck;

                LogStep("Front/Rear PickerX 동시 Avoid 이동 시작.");

                Task<int> frontTask = frontUnit != null
                    ? MoveFrontPickerAxisAvoidAsync(frontUnit, PickerAxis.PickerX, "PickerX", ct)
                    : Task.FromResult(Skip("FrontPickerUnit"));

                Task<int> rearTask = rearUnit != null
                    ? MoveRearPickerAxisAvoidAsync(rearUnit, PickerAxis.PickerX, "PickerX", ct)
                    : Task.FromResult(Skip("RearPickerUnit"));

                int[] results = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                for (int i = 0; i < results.Length; i++)
                {
                    if (results[i] != 0)
                        return results[i];
                }

                LogStep("Front/Rear PickerX 동시 Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-PICKER-X-EX", "MachineReadySequence", "Front/Rear PickerX 동시 Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckInputVisionXAvoidBeforeReadyPickerX()
        {
            try
            {
                InputStageUnit stage = _machine != null ? _machine.InputStageUnit : null;
                if (stage == null || stage.CameraX == null)
                    return 0;

                if (stage.CameraX.IsMoving)
                {
                    return Fail(
                        "READY-PICKER-X-INPUT-VISION",
                        "InputStageUnit",
                        "Ready PickerX 이동 전 InputVisionX Avoid 확인 실패. InputVisionX가 아직 이동 중입니다.");
                }

                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                double avoid = stage.Recipe != null && stage.Recipe.VisionX != null ? stage.Recipe.VisionX.AvoidPosition : 0.0;
                return Fail(
                    "READY-PICKER-X-INPUT-VISION",
                    "InputStageUnit",
                    "Ready PickerX 이동 전 InputVisionX Avoid 확인 실패. InputVisionX가 Avoid 위치가 아닙니다. actual=" +
                    stage.CameraX.ActualPosition.ToString("F3") +
                    ", avoid=" + avoid.ToString("F3"));
            }
            catch (Exception ex)
            {
                return Fail(
                    "READY-PICKER-X-INPUT-VISION-EX",
                    "MachineReadySequence",
                    "Ready PickerX 이동 전 InputVisionX Avoid 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private async Task<int> MoveFrontRearPickerYAxesAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var frontUnit = _machine != null ? _machine.PickerFrontUnit : null;
                var rearUnit = _machine != null ? _machine.PickerRearUnit : null;

                LogStep("Front/Rear PickerY 동시 Avoid 이동 시작.");

                Task<int> frontTask = frontUnit != null
                    ? MoveFrontPickerAxisAvoidAsync(frontUnit, PickerAxis.PickerY, "PickerY", ct)
                    : Task.FromResult(Skip("FrontPickerUnit"));

                Task<int> rearTask = rearUnit != null
                    ? MoveRearPickerAxisAvoidAsync(rearUnit, PickerAxis.PickerY, "PickerY", ct)
                    : Task.FromResult(Skip("RearPickerUnit"));

                int[] results = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                for (int i = 0; i < results.Length; i++)
                {
                    if (results[i] != 0)
                        return results[i];
                }

                LogStep("Front/Rear PickerY 동시 Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-PICKER-Y-EX", "MachineReadySequence", "Front/Rear PickerY 동시 Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontRearPickerTAxesAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var frontUnit = _machine != null ? _machine.PickerFrontUnit : null;
                var rearUnit = _machine != null ? _machine.PickerRearUnit : null;

                LogStep("Front/Rear Picker T축 전체 Avoid 이동 시작.");

                var tasks = new List<Task<int>>();
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerT0, "PickerT1", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerT1, "PickerT2", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerT2, "PickerT3", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerT3, "PickerT4", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerT0, "PickerT1", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerT1, "PickerT2", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerT2, "PickerT3", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerT3, "PickerT4", ct);

                int result = await AwaitReadyAxisTasksAsync(tasks).ConfigureAwait(false);
                if (result != 0)
                    return result;

                LogStep("Front/Rear Picker T축 전체 Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-PICKER-T-EX", "MachineReadySequence", "Front/Rear Picker T축 전체 Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageVisionXOnlyAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.InputStageUnit : null;
                if (unit == null)
                    return Skip("InputStageUnit");

                LogStep("InputStage VisionX 선행 Avoid 이동 시작.");

                int result = await MoveInputStageVisionXAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                LogStep("InputStage VisionX 선행 Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-INPUT-STAGE-VISION-X-EX", "InputStageUnit", "InputStage VisionX 선행 Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AddFrontPickerAxisAvoidTask(
            List<Task<int>> tasks,
            PickerFrontUnit unit,
            PickerAxis axis,
            string label,
            CancellationToken ct)
        {
            if (tasks == null)
                return;

            tasks.Add(unit != null
                ? MoveFrontPickerAxisAvoidAsync(unit, axis, label, ct)
                : Task.FromResult(Skip("FrontPickerUnit")));
        }

        private void AddRearPickerAxisAvoidTask(
            List<Task<int>> tasks,
            PickerRearUnit unit,
            PickerAxis axis,
            string label,
            CancellationToken ct)
        {
            if (tasks == null)
                return;

            tasks.Add(unit != null
                ? MoveRearPickerAxisAvoidAsync(unit, axis, label, ct)
                : Task.FromResult(Skip("RearPickerUnit")));
        }

        private async Task<int> AwaitReadyAxisTasksAsync(List<Task<int>> tasks)
        {
            if (tasks == null || tasks.Count == 0)
                return 0;

            int[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] != 0)
                    return results[i];
            }

            return 0;
        }

        #endregion

        #region Picker·Side Vision 장치 단위 복귀

        private async Task<int> MoveFrontPickerAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.PickerFrontUnit : null;
                if (unit == null)
                    return Skip("FrontPickerUnit");

                LogStep("FrontPicker Avoid 이동 시작.");

                int result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerZ0, "PickerZ1", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerZ1, "PickerZ2", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerZ2, "PickerZ3", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerZ3, "PickerZ4", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerY, "PickerY", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerX, "PickerX", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerT0, "PickerT1", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerT1, "PickerT2", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerT2, "PickerT3", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerT3, "PickerT4", ct).ConfigureAwait(false);
                if (result != 0) return result;

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

                int result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerZ0, "PickerZ1", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerZ1, "PickerZ2", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerZ2, "PickerZ3", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerZ3, "PickerZ4", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerY, "PickerY", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerX, "PickerX", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerT0, "PickerT1", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerT1, "PickerT2", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerT2, "PickerT3", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerT3, "PickerT4", ct).ConfigureAwait(false);
                if (result != 0) return result;

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

                int result = await MoveVisionAxisAvoidAsync(unit, VisionAxis.FrontSideVisionY, "FrontSideVisionY", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveVisionAxisAvoidAsync(unit, VisionAxis.RearSideVisionY, "RearSideVisionY", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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

        #endregion

        #region Ready 선행 Z 복귀 및 안전 검사 흐름

        private async Task<int> MoveInputStageNeedleEjectZAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.InputStageUnit : null;
                if (unit == null)
                    return Skip("InputStageUnit");

                LogStep("InputStage NeedleZ/EjectPinZ Ready 선행 Avoid 이동 시작.");

                int result = await MoveInputStageEjectPinZAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageNeedleZAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                LogStep("InputStage NeedleZ/EjectPinZ Ready 선행 Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-INPUT-STAGE-NEEDLE-EJECT-Z-EX", "InputStageUnit", "InputStage NeedleZ/EjectPinZ Ready 선행 Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> CheckUpperHeadMoveSafetyAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                LogStep("Ready 상부 헤드/비전/픽커 이동 전 안전 조건 확인 시작.");

                int result = await EnsureInputFeederReadySafetyAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureOutputFeederReadySafetyAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureInputStageZReadySafetyAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = CheckGoodStageZReadySafety();
                if (result != 0)
                    return result;

                LogStep("Ready 상부 헤드/비전/픽커 이동 전 안전 조건 확인 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "READY-UPPER-HEAD-SAFETY-EX",
                    "MachineReadySequence",
                    "Ready 상부 헤드/비전/픽커 이동 전 안전 조건 확인 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        #endregion

        #region Feeder 안전 복구 및 Output 인계 검증

        // Ready 진입 시 InputFeeder가 Avoid/Down이 아니면(웨이퍼 이송 도중 정지 등) 기존에는 확인만 하고
        // 실패해 START가 수동 Recover 전까지 영구 차단되었다. 여기서는 다음 기계적 전제 아래에서만
        // 빈 피더를 자동 복구한다.
        //  - 복구 순서는 Unclamp -> Lift Up -> Y Avoid -> Lift Down 이다.
        //    (기존에는 Lift Down -> Y Avoid 순서였고, InputStage 위에서 정지한 경우 스테이지의 wafer를
        //     누르고 긁는다. Output 측에서 동일 결함이 현장 확인되어 양쪽을 함께 수정한다. 2026-07-27)
        //  - Lift Up 전에는 Unclamp와 피더 공백을 반드시 확인한다. wafer를 문 채로 들어올리면 피더가 파손된다.
        //  - wafer를 보유한 피더는 자동 복구 시 자재 위치 상실/파손 위험이 있으므로 복구하지 않고
        //    CYCLE RUN INPUT UNLOAD 수동 배출을 안내하며 fail-closed로 실패한다.
        private async Task<int> EnsureInputFeederReadySafetyAsync(CancellationToken ct)
        {
            const int RecoverIoTimeoutMs = 10000;
            const int RecoverMoveTimeoutMs = 30000;

            try
            {
                ct.ThrowIfCancellationRequested();

                InputFeederUnit unit = _machine != null ? _machine.InputFeederUnit : null;
                if (unit == null)
                    return Skip("InputFeederUnit");

                if (unit.FeederY == null)
                    return Fail("READY-SAFETY-INPUT-FEEDER-AXIS", "InputFeederUnit", "Ready 상부 헤드/비전/픽커 이동 전 InputFeederY 축을 확인할 수 없습니다.");

                if (unit.Recipe == null)
                    return Fail("READY-SAFETY-INPUT-FEEDER-RECIPE", "InputFeederUnit", "Ready 상부 헤드/비전/픽커 이동 전 InputFeeder Avoid 위치 레시피를 확인할 수 없습니다.");

                double target = unit.Recipe.AvoidPosition;
                if (unit.IsWaferFeederInAvoidPosition() && unit.IsWaferFeederDown())
                {
                    // Ready 상태와 일치하는 빈 피더는 이전 중간 모션 Step을 재개하면 안 된다.
                    // Material은 유지하고 InputFeeder UnloadFromStage 재개 정보만 무효화한다.
                    if (unit.IsWaferFeederEmpty())
                        SequenceResumeStore.Clear(InputFeederUnloadFromStageSequence.ResumeStateName);
                    return 0;
                }

                if (unit.FeederY.IsMoving)
                {
                    return Fail(
                        "READY-SAFETY-INPUT-FEEDER-MOVING",
                        "InputFeederUnit",
                        "Ready InputFeeder 복구 불가: InputFeederY가 아직 이동 중입니다. " +
                        BuildAxisState("InputFeederY", unit.FeederY, target) +
                        BuildInputFeederFailure(unit));
                }

                // wafer 보유 확인(fail-closed): Unit 런타임과 영속 Material 중 하나라도 wafer를 가리키면 자동 복구하지 않는다.
                WaferMaterial feederWafer = unit.CurrentWaferMaterial;
                if (feederWafer == null)
                    feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                if (feederWafer != null)
                {
                    return Fail(
                        "READY-INPUT-FEEDER-WAFER",
                        "InputFeederUnit",
                        "Ready InputFeeder 자동 복구 불가: InputFeeder가 wafer를 보유한 상태로 Avoid/Down이 아닙니다. wafer=" +
                        (feederWafer.WaferId ?? "") +
                        // [안내 문구 정정 2026-07-29] 기존에는 [작업 → CYCLE RUN → INPUT UNLOAD]를 안내했으나,
                        // 그 경로는 IsFeederWaferMidUnload(언로드 중단 판정)일 때만 카세트 복귀를 수행한다.
                        // 로드 방향으로 물고 있는 지금 상태에서는 동작하지 않아 작업자를 헤매게 했다.
                        // 현재 방침: 작업자가 웨이퍼를 물리적으로 제거하고 Material DATA CLEAR 후 Ready.
                        ". 웨이퍼를 제거하고 Material DATA를 CLEAR 한 뒤 Ready 바랍니다. " +
                        BuildAxisState("InputFeederY", unit.FeederY, target) +
                        BuildInputFeederFailure(unit));
                }

                LogStep("InputFeeder가 Avoid/Down 상태가 아니어서 빈 피더 안전 복구를 시작합니다. " +
                    BuildAxisState("InputFeederY", unit.FeederY, target));

                // 후퇴 순서(Unclamp -> 보유확인 -> Lift Up -> Y Avoid -> Lift Down)와 파손 방지 확인은
                // FeederRetreatPolicy가 단일 구현으로 보장한다. 여기서 순서를 다시 쓰지 않는다.
                // Avoid 이동만 Ready 전용 Jog Coarse 속도를 쓰도록 넘겨준다.
                // Ready 스코프 5% 저속은 빈 피더 복귀에 과도하게 느리다(수십 초).
                // 기계적 전제: Coarse는 운전자 조그에서 상시 사용하는 검증된 속도이고,
                // 이 시점의 피더는 wafer 미보유 + Lift Up 주행 자세라 고속 복귀에 추가 간섭이 없다.
                double coarseVelocity = unit.FeederY.Config != null ? unit.FeederY.Config.JogCoarseVelocity : 0.0;
                var retreatTarget = new InputFeederRetreatTarget(
                    unit,
                    async (moveTimeoutMs, token) =>
                    {
                        int moveResult = coarseVelocity > 0.0
                            ? await unit.MoveWaferFeederY(target, JogSpeedType.Coarse, 0.0).ConfigureAwait(false)
                            : await unit.MoveToWaferFeederAvoidPosition(false).ConfigureAwait(false);
                        if (moveResult != 0)
                            return moveResult;

                        return await unit.WaitWaferFeederYMoveDoneInPosition(target, moveTimeoutMs, token).ConfigureAwait(false);
                    });

                // [옵션 2026-07-29] Ready 전용 이탈 이동. ReadyRetreatBackOffMm 이 0 이면 기존 동작과 동일하다.
                FeederRetreatResult retreat = await FeederRetreatPolicy.RetreatToAvoidAsync(
                    retreatTarget, RecoverIoTimeoutMs, RecoverMoveTimeoutMs, ct,
                    ReadyRetreatBackOffMm).ConfigureAwait(false);
                if (!retreat.Success)
                {
                    return Fail(
                        ResolveInputFeederRetreatAlarmCode(retreat.FailedPhase),
                        "InputFeederUnit",
                        "Ready InputFeeder 복구 실패. " + retreat.Message + " " +
                        BuildAxisState("InputFeederY", unit.FeederY, target) +
                        BuildInputFeederFailure(unit));
                }

                LogStep("InputFeeder 빈 피더 안전 복구 완료. " +
                    BuildAxisState("InputFeederY", unit.FeederY, target));
                // Ready 복구가 실제 Y/Lift 상태를 변경했으므로 이전 UnloadFromStage 중간
                // Step을 그대로 재개하지 않는다. 자재 상태는 삭제하지 않는다.
                SequenceResumeStore.Clear(InputFeederUnloadFromStageSequence.ResumeStateName);
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-SAFETY-INPUT-FEEDER-EX", "InputFeederUnit", "Ready InputFeeder 안전 조건 확인/복구 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        // Ready 진입 시 OutputFeeder가 Avoid/Down이 아니면(Bin 이송 도중 정지 등) 기존에는 확인만 하고
        // 실패해 START가 수동 Recover 전까지 영구 차단되었다. InputFeeder와 동일한 기계적 전제 아래에서만
        // 빈 피더를 자동 복구한다.
        //  - 복구 순서는 Unclamp -> Lift Up -> Y Avoid -> Lift Down 이다.
        //    (기존에는 Lift Down -> Y Avoid 순서였고, OutputStage 위에서 정지한 경우 스테이지의 Bin/제품을
        //     누르고 긁었다. 현장 확인 2026-07-27. 생산 시퀀스 OutputFeederLoadToStageSequence의
        //     PrepareFeederLiftUp -> MoveFeederAvoidPosition -> PrepareFeederLiftDownAfterAvoid 순서와 일치시킨다.)
        //  - Lift Up 전에는 Unclamp와 피더 공백을 반드시 확인한다. Bin을 문 채로 들어올리면 피더가 파손된다.
        //  - Bin을 보유한 피더는 자동 복구 시 자재 위치 상실/파손 위험이 있으므로 복구하지 않고
        //    CYCLE RUN OUTPUT UNLOAD 수동 배출을 안내하며 fail-closed로 실패한다.
        //    보유 판정은 영속 Material과 물리 센서(IsFeederEmpty)를 모두 확인한다.
        private async Task<int> EnsureOutputFeederReadySafetyAsync(CancellationToken ct)
        {
            const int RecoverIoTimeoutMs = 10000;
            const int RecoverMoveTimeoutMs = 30000;

            try
            {
                ct.ThrowIfCancellationRequested();

                OutputFeederUnit unit = _machine != null ? _machine.OutputFeederUnit : null;
                if (unit == null)
                    return Skip("OutputFeederUnit");

                if (unit.FeederY == null)
                    return Fail("READY-SAFETY-OUTPUT-FEEDER-AXIS", "OutputFeederUnit", "Ready 상부 헤드/비전/픽커 이동 전 OutputFeederY 축을 확인할 수 없습니다.");

                if (unit.Recipe == null)
                    return Fail("READY-SAFETY-OUTPUT-FEEDER-RECIPE", "OutputFeederUnit", "Ready 상부 헤드/비전/픽커 이동 전 OutputFeeder Avoid 위치 레시피를 확인할 수 없습니다.");

                double target = unit.Recipe.AvoidPosition;
                unit.FeederY.UpdateStatus();

                if (!unit.FeederY.IsServoOn || unit.FeederY.IsAlarm)
                {
                    return Fail(
                        "READY-SAFETY-OUTPUT-FEEDER-AXIS-STATE",
                        "OutputFeederUnit",
                        "Ready OutputFeeder 복구 불가: OutputFeederY Servo/Alarm 상태가 정상적이지 않습니다. " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                if (unit.FeederY.IsMoving)
                {
                    return Fail(
                        "READY-SAFETY-OUTPUT-FEEDER-MOVING",
                        "OutputFeederUnit",
                        "Ready OutputFeeder 복구 불가: OutputFeederY가 아직 이동 중입니다. " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                if (unit.IsFeederOverload())
                {
                    return Fail(
                        "READY-SAFETY-OUTPUT-FEEDER-OVERLOAD",
                        "OutputFeederUnit",
                        "Ready OutputFeeder 복구 불가: OutputFeeder Overload가 감지되었습니다. " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                WaferMaterial feederBin = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                bool persistentOccupied = feederBin != null;
                bool projectedOccupied = unit.IsFeederTransferDataOccupied();
                if (persistentOccupied != projectedOccupied)
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-DATA-MISMATCH",
                        "OutputFeederUnit",
                        "Ready OutputFeeder Material과 Unit runtime 점유 상태가 서로 다릅니다. persistent=" +
                        persistentOccupied + ", projected=" + projectedOccupied +
                        ", bin=" + (feederBin != null ? feederBin.WaferId : "") +
                        ". DATA ONLY 상태를 다시 확인한 뒤 READY를 실행하세요. " +
                        unit.DescribeFeederCylinderState());
                }

                bool ringDetected;
                string ringReason;
                if (!TryReadOutputFeederRingStateForReady(unit, out ringDetected, out ringReason))
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-RING-READ",
                        "OutputFeederUnit",
                        "Ready OutputFeeder Ring 상태를 확정할 수 없습니다. " + ringReason + " " +
                        unit.DescribeFeederCylinderState());
                }

                // [Ready 오검출 수정 2026-07-29] 기존 조건에 unit.FeederY.IsInPosition 이 있었다.
                // INP 는 "지금 그 위치에 있는가"가 아니라 "명령받은 이동을 완료하고 정정착했는가" 신호다.
                // 서보 ON 후 위치 결정 이동을 한 번도 하지 않으면 좌표가 정확히 맞아도 INP 가 뜨지 않는다.
                // Ready Step2 는 아무것도 움직이기 전이므로, 갓 켠 직후·초기화 직후에는 구조적으로 통과 불가였고
                // READY-OUTPUT-FEEDER-AVOID-AMBIGUOUS 로 매번 막혔다(2026-07-29 현장: Ready 누르면 계속 발생).
                //   실측 예: actual=0, command=0, target=0, tolerance=0.01, inPosition=OFF → 좌표는 완전 일치
                // 위치 근거는 아래 둘로 충분하다.
                //   · IsAxisInPosition(FeederY, target) : 좌표가 Avoid 티칭값과 공차 내 일치
                //   · IsBinFeederAvoidPositionCheck()   : 실제 Avoid Dog(X091) 물리 확인
                //     (순수 Simulation/보드 미사용일 때만 엔코더 위치로 대체된다 — OutputFeederUnit:571)
                // 도그는 이동 이력과 무관하게 항상 유효하므로 INP 보다 강한 물리 근거다. INP 요구를 제거한다.
                bool strongAvoidAxis =
                    IsAxisInPosition(unit.FeederY, target) &&
                    unit.IsBinFeederAvoidPositionCheck();
                bool strongAvoid =
                    strongAvoidAxis &&
                    unit.IsFeederDown() &&
                    !unit.IsFeederUp();
                bool approvedPostRelease = false;
                string postReleaseReason = string.Empty;
                if (strongAvoid &&
                    persistentOccupied &&
                    !ringDetected &&
                    unit.IsFeederUnclamped())
                {
                    approvedPostRelease = TryValidateOutputPostReleaseReadyState(
                        unit,
                        feederBin,
                        out postReleaseReason);
                }

                if (persistentOccupied != ringDetected && !approvedPostRelease)
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-MATERIAL-RING-MISMATCH",
                        "OutputFeederUnit",
                        "Ready OutputFeeder Material/Ring 상태가 일치하지 않습니다. material=" +
                        persistentOccupied + ", ring=" + ringDetected +
                        ", postRelease=" + postReleaseReason +
                        ". 자동 이동하지 않습니다. " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                if (strongAvoid)
                {
                    if (persistentOccupied && ringDetected && unit.IsFeederUnclamped())
                    {
                        return Fail(
                            "READY-OUTPUT-FEEDER-OCCUPIED-UNCLAMPED",
                            "OutputFeederUnit",
                            "Ready OutputFeeder가 Bin을 감지하지만 Unclamp 상태입니다. 자동 진행하지 않습니다. bin=" +
                            (feederBin != null ? feederBin.WaferId : "") + ", " +
                            unit.DescribeFeederCylinderState());
                    }

                    if (approvedPostRelease)
                    {
                        LogStep(
                            "OutputFeeder post-release 강한 상태를 확인했습니다. READY 후 현재 물리 상태에서 " +
                            "UnloadToCassette 후속 검증을 재개합니다. bin=" +
                            (feederBin != null ? feederBin.WaferId : "") + ", " +
                            postReleaseReason);
                    }

                    return 0;
                }

                // Actual은 이미 Avoid 부근인데 Command/InPosition/Avoid dog 중 하나가 맞지 않으면
                // FeederRetreatPolicy가 Y 이동을 생략하고 Lift만 왕복할 수 있다. 이 모호 상태에서는
                // 자동 실린더 동작을 하지 않고 원인을 확인하도록 차단한다.
                double avoidTolerance = ResolveAxisTolerance(unit.FeederY);
                bool liftStateAmbiguous = unit.IsFeederUp() == unit.IsFeederDown();
                if (Math.Abs(unit.FeederY.ActualPosition - target) <= avoidTolerance &&
                    (!strongAvoidAxis || liftStateAmbiguous))
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-AVOID-AMBIGUOUS",
                        "OutputFeederUnit",
                        "Ready OutputFeeder가 물리적으로 Avoid 부근이지만 강한 완료 조건이 맞지 않습니다. " +
                        "자동 Lift 왕복을 수행하지 않습니다. " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                if (persistentOccupied)
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-BIN",
                        "OutputFeederUnit",
                        "Ready OutputFeeder 자동 복구 불가: OutputFeeder가 Bin을 보유한 상태로 강한 Avoid/Down이 아닙니다. bin=" +
                        (feederBin.WaferId ?? "") +
                        // [안내 문구 정정 2026-07-29] 입력측과 동일 방침. 작업자가 물리적으로 제거 + DATA CLEAR 후 Ready.
                        ". Bin을 제거하고 Material DATA를 CLEAR 한 뒤 Ready 바랍니다. " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                if (ringDetected)
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-SENSOR",
                        "OutputFeederUnit",
                        "Ready OutputFeeder 자동 복구 불가: Material은 비어 있으나 Ring 센서가 Bin을 감지했습니다. " +
                        // [안내 문구 정정 2026-07-29] 데이터와 센서 불일치 → 실물 확인 후 DATA CLEAR 방침으로 통일.
                        "실물을 확인해 Bin을 제거하고 Material DATA를 CLEAR 한 뒤 Ready 바랍니다. " +
                        unit.DescribeFeederCylinderState());
                }

                string cassetteReleaseReason;
                if (!VerifyOutputCassetteReleaseBeforeEmptyFeederRetreat(
                        unit,
                        out cassetteReleaseReason))
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-CASSETTE-RELEASE",
                        "OutputFeederUnit",
                        "Ready OutputFeeder 자동 복구 불가: CassetteUnload 위치의 빈 피더 후퇴 조건이 맞지 않습니다. " +
                        cassetteReleaseReason + " " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                LogStep("OutputFeeder가 Avoid/Down 상태가 아니어서 빈 피더 안전 복구를 시작합니다. " +
                    BuildAxisState("OutputFeederY", unit.FeederY, target));

                // 후퇴 순서(Unclamp -> 보유확인 -> Lift Up -> Y Avoid -> Lift Down)와 파손 방지 확인은
                // FeederRetreatPolicy가 단일 구현으로 보장한다. 여기서 순서를 다시 쓰지 않는다.
                // Avoid 이동만 Ready 전용 Jog Coarse 속도를 쓰도록 넘겨준다.
                // Ready 스코프 5% 저속은 빈 피더 복귀에 과도하게 느리다(수십 초).
                // 기계적 전제: Coarse는 운전자 조그에서 상시 사용하는 검증된 속도이고,
                // 이 시점의 피더는 Bin 미보유 + Lift Up 주행 자세라 고속 복귀에 추가 간섭이 없다.
                double coarseVelocity = unit.FeederY.Config != null ? unit.FeederY.Config.JogCoarseVelocity : 0.0;
                var retreatTarget = new OutputFeederRetreatTarget(
                    unit,
                    async (moveTimeoutMs, token) =>
                    {
                        int moveResult = coarseVelocity > 0.0
                            ? await unit.MoveBinFeederY(target, JogSpeedType.Coarse, 0.0).ConfigureAwait(false)
                            : await unit.MoveToFeederAvoidPosition(false).ConfigureAwait(false);
                        if (moveResult != 0)
                            return moveResult;

                        return await unit.WaitBinFeederYMoveDoneInPosition(target, moveTimeoutMs, token).ConfigureAwait(false);
                    });

                // [옵션 2026-07-29] Ready 전용 이탈 이동. 입력측과 동일 상수를 쓴다.
                FeederRetreatResult retreat = await FeederRetreatPolicy.RetreatToAvoidAsync(
                    retreatTarget, RecoverIoTimeoutMs, RecoverMoveTimeoutMs, ct,
                    ReadyRetreatBackOffMm).ConfigureAwait(false);
                if (!retreat.Success)
                {
                    return Fail(
                        ResolveOutputFeederRetreatAlarmCode(retreat.FailedPhase),
                        "OutputFeederUnit",
                        "Ready OutputFeeder 복구 실패. " + retreat.Message + " " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                unit.FeederY.UpdateStatus();
                bool finalStrongAvoid =
                    unit.FeederY.IsServoOn &&
                    !unit.FeederY.IsAlarm &&
                    !unit.FeederY.IsMoving &&
                    unit.FeederY.IsInPosition &&
                    IsAxisInPosition(unit.FeederY, target) &&
                    unit.IsBinFeederAvoidPositionCheck() &&
                    unit.IsFeederDown() &&
                    !unit.IsFeederUp() &&
                    !unit.IsFeederOverload();
                bool finalRingDetected = false;
                string finalRingReason = string.Empty;
                if (!finalStrongAvoid ||
                    !TryReadOutputFeederRingStateForReady(
                        unit,
                        out finalRingDetected,
                        out finalRingReason) ||
                    finalRingDetected ||
                    unit.IsFeederTransferDataOccupied())
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-RECOVER-CHECK",
                        "OutputFeederUnit",
                        "Ready OutputFeeder 빈 피더 복구 후 강한 최종 확인에 실패했습니다. " +
                        "strongAvoid=" + finalStrongAvoid +
                        ", ring=" + finalRingDetected +
                        ", ringReason=" + finalRingReason +
                        ", dataOccupied=" + unit.IsFeederTransferDataOccupied() + ". " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                LogStep("OutputFeeder 빈 피더 안전 복구 완료. " +
                    BuildAxisState("OutputFeederY", unit.FeederY, target));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-SAFETY-OUTPUT-FEEDER-EX", "OutputFeederUnit", "Ready OutputFeeder 안전 조건 확인/복구 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private static bool TryReadOutputFeederRingStateForReady(
            OutputFeederUnit feeder,
            out bool ringDetected,
            out string reason)
        {
            ringDetected = false;
            reason = string.Empty;

            if (feeder == null || feeder.BinFeederRingCheckSensor == null)
            {
                reason = "OutputFeeder Ring 센서 정보를 찾을 수 없습니다.";
                return false;
            }

            if (feeder.IsOutputFeederSimulationOrDryRun())
            {
                // Material은 물리 인계 완료 전까지 Feeder에 남는다. Simulation/DryRun에서는
                // 강한 Avoid/Down/Unclamp 상태만 제품 해제 완료로 해석한다.
                bool releasedAtAvoid =
                    feeder.IsFeederDown() &&
                    feeder.IsFeederUnclamped() &&
                    feeder.IsBinFeederYInAvoidPosition() &&
                    feeder.IsBinFeederAvoidPositionCheck();
                ringDetected =
                    !releasedAtAvoid &&
                    feeder.IsFeederTransferDataOccupied();
                return true;
            }

            if (feeder.BinFeederRingCheckSensor.Config != null &&
                (feeder.BinFeederRingCheckSensor.Config.IsSimulationMode ||
                 feeder.BinFeederRingCheckSensor.Config.IgnoreWaits))
            {
                reason = "실기 운전 중 OutputFeeder Ring 센서가 Simulation/DryRun 설정입니다.";
                return false;
            }

            int errorCode;
            if (!AjinIoScanService.TryReadHardwareInput(
                    feeder.BinFeederRingCheckSensor,
                    out errorCode))
            {
                reason = "OutputFeeder Ring 센서 실제 입력 읽기 실패. errorCode=" + errorCode;
                return false;
            }

            ringDetected = feeder.BinFeederRingCheckSensor.IsOn;
            return true;
        }

        private bool TryValidateOutputPostReleaseReadyState(
            OutputFeederUnit feeder,
            WaferMaterial feederBin,
            out string reason)
        {
            reason = string.Empty;

            if (feeder == null || feederBin == null)
            {
                reason = "OutputFeeder 또는 Feeder Material이 없습니다.";
                return false;
            }

            CassetteMaterialRole role = feederBin.SourceCassetteRole;
            int slotIndex = feederBin.SourceSlotNumber;
            TargetCassette targetCassette;
            BinSide side;
            if (!TryResolveOutputCassetteTarget(role, out targetCassette, out side))
            {
                reason = "Feeder Material의 원본 Output cassette role이 올바르지 않습니다. role=" + role;
                return false;
            }

            WaferMaterial stageBin = MaterialStateService.GetWaferAtLocation(
                side == BinSide.Ng
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood);
            if (stageBin != null)
            {
                reason = "대상 OutputStage에 다른 Material이 남아 있습니다. stageBin=" + stageBin.WaferId;
                return false;
            }

            double releaseTarget;
            if (!TryValidateOutputCassetteReleasePosition(
                    role,
                    slotIndex,
                    false,
                    out releaseTarget,
                    out reason))
            {
                return false;
            }

            reason =
                "role=" + role +
                ", slot=" + (slotIndex + 1).ToString("00") +
                ", side=" + side +
                ", releaseTarget=" + releaseTarget.ToString("0.###");
            return true;
        }

        private bool VerifyOutputCassetteReleaseBeforeEmptyFeederRetreat(
            OutputFeederUnit feeder,
            out string reason)
        {
            reason = string.Empty;

            if (feeder == null || feeder.FeederY == null || feeder.Recipe == null)
            {
                reason = "OutputFeeder Y축 또는 Recipe가 없습니다.";
                return false;
            }

            double feederTolerance = ResolveAxisTolerance(feeder.FeederY);
            bool physicallyAtGoodCassetteUnload =
                Math.Abs(
                    feeder.FeederY.ActualPosition -
                    feeder.Recipe.GoodCassetteUnloadPosition) <= feederTolerance;
            bool physicallyAtNgCassetteUnload =
                Math.Abs(
                    feeder.FeederY.ActualPosition -
                    feeder.Recipe.NGCassetteUnloadPosition) <= feederTolerance;

            // Cassette 삽입 위치가 아니면 기존 빈 피더 Ready 복구 계약을 유지한다.
            if (!physicallyAtGoodCassetteUnload && !physicallyAtNgCassetteUnload)
                return true;

            bool atGoodCassetteUnload =
                physicallyAtGoodCassetteUnload &&
                feeder.FeederY.IsInPosition &&
                IsOutputPositionMatch(
                    feeder.FeederY.ActualPosition,
                    feeder.Recipe.GoodCassetteUnloadPosition,
                    feederTolerance) &&
                IsOutputPositionMatch(
                    feeder.FeederY.CommandPosition,
                    feeder.Recipe.GoodCassetteUnloadPosition,
                    feederTolerance);
            bool atNgCassetteUnload =
                physicallyAtNgCassetteUnload &&
                feeder.FeederY.IsInPosition &&
                IsOutputPositionMatch(
                    feeder.FeederY.ActualPosition,
                    feeder.Recipe.NGCassetteUnloadPosition,
                    feederTolerance) &&
                IsOutputPositionMatch(
                    feeder.FeederY.CommandPosition,
                    feeder.Recipe.NGCassetteUnloadPosition,
                    feederTolerance);

            if (!atGoodCassetteUnload && !atNgCassetteUnload)
            {
                reason =
                    "OutputFeederY Actual은 CassetteUnload 부근이지만 Command/InPosition/F3 완료 조건이 " +
                    "맞지 않습니다. 자동 후퇴하지 않습니다. physicallyAtGood=" +
                    physicallyAtGoodCassetteUnload + ", physicallyAtNg=" +
                    physicallyAtNgCassetteUnload + ", " +
                    BuildAxisState(
                        "OutputFeederY",
                        feeder.FeederY,
                        physicallyAtNgCassetteUnload
                            ? feeder.Recipe.NGCassetteUnloadPosition
                            : feeder.Recipe.GoodCassetteUnloadPosition);
                return false;
            }

            if (!feeder.IsFeederDown() ||
                feeder.IsFeederUp() ||
                !feeder.IsFeederUnclamped() ||
                feeder.IsFeederOverload())
            {
                reason =
                    "CassetteUnload 위치에서 빈 피더의 Down/Unclamp/Overload 상태가 안전 조건과 다릅니다. " +
                    feeder.DescribeFeederCylinderState();
                return false;
            }

            CassetteMaterialRole[] candidateRoles =
            {
                CassetteMaterialRole.Good1,
                CassetteMaterialRole.Good2,
                CassetteMaterialRole.Ng1
            };
            int matchCount = 0;
            string matchedState = string.Empty;
            OutputCassetteUnit cassette = _machine != null ? _machine.OutputCassetteUnit : null;
            int slotCount = cassette != null && cassette.Config != null
                ? cassette.Config.SlotCount
                : 0;

            for (int roleIndex = 0; roleIndex < candidateRoles.Length; roleIndex++)
            {
                CassetteMaterialRole role = candidateRoles[roleIndex];
                if ((role == CassetteMaterialRole.Ng1 && !atNgCassetteUnload) ||
                    (role != CassetteMaterialRole.Ng1 && !atGoodCassetteUnload))
                {
                    continue;
                }

                for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    if (MaterialStateService.GetWaferInCassette(role, slotIndex) == null)
                        continue;

                    double releaseTarget;
                    string releaseReason;
                    if (!TryValidateOutputCassetteReleasePosition(
                            role,
                            slotIndex,
                            true,
                            out releaseTarget,
                            out releaseReason))
                    {
                        continue;
                    }

                    matchCount++;
                    matchedState =
                        "role=" + role +
                        ", slot=" + (slotIndex + 1).ToString("00") +
                        ", releaseTarget=" + releaseTarget.ToString("0.###");
                }
            }

            if (matchCount == 1)
            {
                reason = matchedState;
                return true;
            }

            reason =
                "CassetteUnload 위치의 빈 피더에 대응하는 'Material commit 완료 + Cassette release +1mm' " +
                "상태를 하나로 확정할 수 없습니다. matchCount=" + matchCount +
                ", atGoodUnload=" + atGoodCassetteUnload +
                ", atNgUnload=" + atNgCassetteUnload;
            return false;
        }

        private bool TryValidateOutputCassetteReleasePosition(
            CassetteMaterialRole role,
            int slotIndex,
            bool requireCassetteMaterial,
            out double releaseTarget,
            out string reason)
        {
            releaseTarget = 0.0;
            reason = string.Empty;

            OutputCassetteUnit cassette = _machine != null ? _machine.OutputCassetteUnit : null;
            if (cassette == null ||
                cassette.OutputLifterZ == null ||
                cassette.Config == null)
            {
                reason = "OutputCassette 또는 OutputLifterZ/Config가 없습니다.";
                return false;
            }

            TargetCassette targetCassette;
            BinSide side;
            if (!TryResolveOutputCassetteTarget(role, out targetCassette, out side))
            {
                reason = "Output cassette role이 올바르지 않습니다. role=" + role;
                return false;
            }

            if (slotIndex < 0 || slotIndex >= cassette.Config.SlotCount)
            {
                reason =
                    "Output cassette slot이 범위를 벗어났습니다. role=" + role +
                    ", slot=" + (slotIndex + 1) +
                    ", slotCount=" + cassette.Config.SlotCount;
                return false;
            }

            if (targetCassette == TargetCassette.Good2 &&
                cassette.Config.SelectedCassetteLevel < 2)
            {
                reason = "GOOD2 cassette가 비활성 상태입니다.";
                return false;
            }

            WaferMaterial cassetteBin = MaterialStateService.GetWaferInCassette(role, slotIndex);
            if (requireCassetteMaterial != (cassetteBin != null))
            {
                reason =
                    "Output cassette Material 상태가 기대와 다릅니다. role=" + role +
                    ", slot=" + (slotIndex + 1).ToString("00") +
                    ", expectedOccupied=" + requireCassetteMaterial +
                    ", actualOccupied=" + (cassetteBin != null);
                return false;
            }

            double unloadingOffset = cassette.ResolveUnloadingPositionOffset();
            double releaseDistance = cassette.Config.UnloadReleaseLiftDistance;
            if (double.IsNaN(unloadingOffset) ||
                double.IsInfinity(unloadingOffset) ||
                double.IsNaN(releaseDistance) ||
                double.IsInfinity(releaseDistance) ||
                unloadingOffset >= 0.0 ||
                releaseDistance < OutputCassetteUnit.MinUnloadReleaseLiftDistanceMm ||
                releaseDistance > OutputCassetteUnit.MaxUnloadReleaseLiftDistanceMm ||
                releaseDistance > Math.Abs(unloadingOffset))
            {
                reason =
                    "OutputCassette unload release 설정이 안전 범위를 벗어났습니다. " +
                    "UnloadingPositionOffset=" + unloadingOffset.ToString("0.###") +
                    ", UnloadReleaseLiftDistance=" + releaseDistance.ToString("0.###");
                return false;
            }

            double slotTarget = cassette.CalculateBinCassetteSlotTargetPosition(
                targetCassette,
                slotIndex);
            double unloadTarget = slotTarget + unloadingOffset;
            releaseTarget = unloadTarget + releaseDistance;

            cassette.OutputLifterZ.UpdateStatus();
            BaseAxis axis = cassette.OutputLifterZ;
            double tolerance = ResolveAxisTolerance(axis);
            if (!axis.IsServoOn ||
                axis.IsAlarm ||
                axis.IsMoving ||
                !axis.IsInPosition ||
                !OutputCassetteUnit.IsUnloadReleasePositionMatch(
                    axis.ActualPosition,
                    releaseTarget,
                    unloadTarget,
                    tolerance) ||
                !OutputCassetteUnit.IsUnloadReleasePositionMatch(
                    axis.CommandPosition,
                    releaseTarget,
                    unloadTarget,
                    tolerance))
            {
                reason =
                    "OutputCassette가 승인된 release 위치에 강하게 정지하지 않았습니다. role=" + role +
                    ", slot=" + (slotIndex + 1).ToString("00") +
                    ", releaseTarget=" + releaseTarget.ToString("0.###") +
                    ", " + BuildAxisState("OutputLifterZ", axis, releaseTarget);
                return false;
            }

            return true;
        }

        private static bool TryResolveOutputCassetteTarget(
            CassetteMaterialRole role,
            out TargetCassette targetCassette,
            out BinSide side)
        {
            switch (role)
            {
                case CassetteMaterialRole.Good1:
                    targetCassette = TargetCassette.Good1;
                    side = BinSide.Good;
                    return true;

                case CassetteMaterialRole.Good2:
                    targetCassette = TargetCassette.Good2;
                    side = BinSide.Good;
                    return true;

                case CassetteMaterialRole.Ng1:
                    targetCassette = TargetCassette.Ng;
                    side = BinSide.Ng;
                    return true;

                default:
                    targetCassette = TargetCassette.Good1;
                    side = BinSide.Good;
                    return false;
            }
        }

        private static bool IsOutputPositionMatch(double value, double target, double tolerance)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                double.IsNaN(target) ||
                double.IsInfinity(target) ||
                tolerance <= 0.0)
            {
                return false;
            }

            double valueKey = Math.Round(value, 3, MidpointRounding.AwayFromZero);
            double targetKey = Math.Round(target, 3, MidpointRounding.AwayFromZero);
            return valueKey == targetKey && Math.Abs(value - target) <= tolerance;
        }

        #endregion

        #region Stage Z 안전 복구 및 조건 검사

        // Ready 진입 시 ExpanderZ가 0보다 위 티칭 위치(예: 로딩 구간의 WaferZ.LoadPosition)에 남아 있으면
        // (로딩 도중 정지, 수동 INPUT LOAD 후 등) 기존에는 확인만 하고 실패해 START가 차단되었다.
        // 인터락 자체(상부 헤드/픽커 X 이동 전 ExpanderZ ≤ 0, 픽커 공유 레일 간섭 방지)는 유지하고,
        // 아래 전제에서만 Avoid로 안전 자동 복구한다.
        //  - 재개 안전성: 재개 경로가 ExpanderZ를 필요한 티칭 위치로 스스로 복원한다.
        //    (Align/DieMapping/PickUp/선행검사는 WaferZ.ProcessPosition으로 이동·확인, 로딩 재개 구간은
        //     WaferZ.LoadPosition 조건을 자체 확인) Pick 높이는 티칭값(ProcessPosition, 예: -0.8)을 그대로 따른다.
        //  - 기계적 전제: ExpanderZ 이동은 StageT가 고정 위치(0/Avoid/Load/Unload/Ready/Process)일 때만
        //    허용한다(Align 시퀀스의 EnsureStageTFixedBeforeExpanderZMove와 동일 조건). StageT가 임의
        //    위치이면 자동 복구하지 않고 기존대로 실패한다(fail-closed). Ready에서 StageT는 움직이지 않는다.
        private async Task<int> EnsureInputStageZReadySafetyAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputStageUnit unit = _machine != null ? _machine.InputStageUnit : null;
                if (unit == null)
                    return Skip("InputStageUnit");

                BaseAxis axis = unit.ExpanderZ;
                if (axis == null)
                    return Fail("READY-SAFETY-INPUT-STAGE-Z-AXIS", "InputStageUnit", "Ready 상부 헤드/비전/픽커 이동 전 InputStageZ(ExpanderZ) 축을 확인할 수 없습니다.");

                if (axis.ActualPosition <= 0.0)
                    return 0;

                if (axis.IsMoving)
                {
                    return Fail(
                        "READY-SAFETY-INPUT-STAGE-Z-MOVING",
                        "InputStageUnit",
                        "Ready InputStageZ(ExpanderZ) 복구 불가: 축이 아직 이동 중입니다. " +
                        BuildAxisState("ExpanderZ", axis, 0.0) +
                        BuildInputStageFailure(unit));
                }

                if (unit.Recipe == null)
                    return Fail("READY-SAFETY-INPUT-STAGE-Z-RECIPE", "InputStageUnit",
                        "Ready InputStageZ(ExpanderZ) 복구에 필요한 InputStage 레시피를 확인할 수 없습니다.");
                unit.Recipe.EnsurePositionObjects();

                string stageTReason;
                if (!IsStageTAtExpanderZFixedPositionForReady(unit, out stageTReason))
                {
                    return Fail(
                        "READY-SAFETY-INPUT-STAGE-Z",
                        "InputStageUnit",
                        "Ready 상부 헤드/비전/픽커 이동 전 InputStageZ(ExpanderZ)는 0 이하이어야 합니다. actual=" +
                        axis.ActualPosition.ToString("0.###") +
                        ", blockLimit=0.000. StageT가 ExpanderZ 이동 안전 고정 위치가 아니어서 자동 복구하지 않습니다. " +
                        stageTReason + " " +
                        BuildAxisState("ExpanderZ", axis, 0.0) +
                        BuildInputStageFailure(unit) +
                        " 알람 해제 후 InputStage 화면에서 StageT/Stage를 복구한 뒤 다시 START 하세요.");
                }

                LogStep("ExpanderZ가 0보다 위 위치에 남아 있어 Avoid로 안전 복구를 시작합니다. " +
                    "(Auto 재개 시 각 시퀀스가 필요한 티칭 위치로 복원) " +
                    BuildAxisState("ExpanderZ", axis, unit.Recipe.WaferZ != null ? unit.Recipe.WaferZ.AvoidPosition : 0.0));

                int result = await MoveInputStageExpanderZAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 복구 후 최종 확인: 0 이하 또는 Avoid 허용오차 이내.
                if (axis.ActualPosition > 0.0 && !unit.IsExpanderZInAvoidPosition())
                {
                    return Fail(
                        "READY-INPUT-STAGE-Z-RECOVER-CHECK",
                        "InputStageUnit",
                        "Ready ExpanderZ Avoid 복구 후 최종 확인 실패. " +
                        BuildAxisState("ExpanderZ", axis, unit.Recipe.WaferZ != null ? unit.Recipe.WaferZ.AvoidPosition : 0.0) +
                        BuildInputStageFailure(unit));
                }

                LogStep("ExpanderZ Avoid 안전 복구 완료. " +
                    BuildAxisState("ExpanderZ", axis, unit.Recipe.WaferZ != null ? unit.Recipe.WaferZ.AvoidPosition : 0.0));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-SAFETY-INPUT-STAGE-Z-EX", "InputStageUnit", "Ready InputStageZ 안전 조건 확인/복구 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        // Align 시퀀스의 IsStageTAtExpanderZFixedPosition과 동일 조건:
        // StageT가 티칭된 고정 위치 중 하나에 정지해 있어야 ExpanderZ를 이동할 수 있다.
        private bool IsStageTAtExpanderZFixedPositionForReady(InputStageUnit unit, out string reason)
        {
            reason = string.Empty;
            try
            {
                BaseAxis stageT = unit != null ? unit.StageT : null;
                if (stageT == null || unit.Recipe == null || unit.Recipe.WaferT == null)
                {
                    reason = "StageT 축 또는 WaferT 레시피를 확인할 수 없습니다.";
                    return false;
                }

                if (stageT.IsMoving)
                {
                    reason = "StageT가 아직 이동 중입니다.";
                    return false;
                }

                double tolerance = ResolveAxisTolerance(stageT);
                if (IsAxisInPosition(stageT, 0.0, tolerance) ||
                    IsAxisInPosition(stageT, unit.Recipe.WaferT.AvoidPosition, tolerance) ||
                    IsAxisInPosition(stageT, unit.Recipe.WaferT.LoadPosition, tolerance) ||
                    IsAxisInPosition(stageT, unit.Recipe.WaferT.UnloadPosition, tolerance) ||
                    IsAxisInPosition(stageT, unit.Recipe.WaferT.ReadyPosition, tolerance) ||
                    IsAxisInPosition(stageT, unit.Recipe.WaferT.ProcessPosition, tolerance))
                {
                    return true;
                }

                reason = "StageT actual=" + stageT.ActualPosition.ToString("0.###") +
                         " 이(가) 고정 위치(0/Avoid/Load/Unload/Ready/Process)가 아닙니다.";
                return false;
            }
            catch (Exception ex)
            {
                reason = "StageT 위치 확인 예외: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private int CheckGoodStageZReadySafety()
        {
            try
            {
                OutputStageUnit unit = _machine != null ? _machine.OutputStageUnit : null;
                if (unit == null)
                    return Skip("OutputStageUnit");

                if (unit.GoodStage == null || unit.GoodStage.StageZ == null)
                    return Fail("READY-SAFETY-GOOD-STAGE-Z-AXIS", "OutputStageUnit", "Ready 상부 헤드/비전/픽커 이동 전 GoodStageZ 축을 확인할 수 없습니다.");

                if (unit.Recipe == null || unit.Recipe.GoodStageZ == null)
                    return Fail("READY-SAFETY-GOOD-STAGE-Z-RECIPE", "OutputStageUnit", "Ready 상부 헤드/비전/픽커 이동 전 GoodStageZ Process 위치 레시피를 확인할 수 없습니다.");

                BaseAxis axis = unit.GoodStage.StageZ;
                double process = unit.Recipe.GoodStageZ.ProcessPosition;
                double tolerance = ResolveAxisTolerance(axis);
                double limit = process + tolerance;
                if (axis.ActualPosition > limit)
                {
                    return Fail(
                        "READY-SAFETY-GOOD-STAGE-Z",
                        "OutputStageUnit",
                        "Ready 상부 헤드/비전/픽커 이동 전 GoodStageZ가 Process 위치보다 높습니다. actual=" +
                        axis.ActualPosition.ToString("0.###") +
                        ", process=" + process.ToString("0.###") +
                        ", plusTolerance=" + tolerance.ToString("0.###") +
                        ", limit=" + limit.ToString("0.###") +
                        ", " + BuildAxisState("GoodStageZ", axis, process, tolerance) +
                        BuildOutputStageFailure(unit) +
                        // [안내 문구 정정 2026-07-29] CYCLE RUN 경로 안내를 제거하고 제품 조치 방침으로 통일.
                        " 제품을 조치하고 Material DATA를 확인한 뒤 Ready 바랍니다.");
                }

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("READY-SAFETY-GOOD-STAGE-Z-EX", "OutputStageUnit", "Ready GoodStageZ 안전 조건 확인 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        #endregion

        #region Stage 장치 단위 전체 복귀

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

                int result = await MoveOutputStageAxisAvoidAsync(unit, BinStageAxis.NgBinY, "NG StageY", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputStageAxisAvoidAsync(unit, BinStageAxis.GoodBinZ, "Good StageZ", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputStageAxisAvoidAsync(unit, BinStageAxis.GoodBinY, "Good StageY", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputStageAxisAvoidAsync(unit, BinStageAxis.VisionX, "Output VisionX", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (!IsOutputStageAvoidPosition(unit))
                    return Fail("READY-OUTPUT-STAGE-CHECK", "OutputStageUnit", "OutputStage Avoid 위치 최종 확인 실패." + BuildOutputStageFailure(unit));

                LogStep("OutputStage Avoid 이동 완료.");
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

        #endregion

        #region 축·Feeder·Cassette 단위 Avoid 실행

        private async Task<int> MoveFrontPickerAxisAvoidAsync(PickerFrontUnit unit, PickerAxis axis, string label, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("PickerFrontUnit");

                BaseAxis baseAxis = ResolveFrontPickerAxis(unit, axis);
                if (baseAxis == null)
                    return Fail("READY-FRONT-PICKER-AXIS", "PickerFrontUnit", "FrontPicker " + label + " 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                double target = unit.GetPickerTeachingPosition(axis, "AvoidPosition");
                LogStep("FrontPicker " + label + " Avoid 이동 시작. target=" + target);

                int result = await unit.MovePickerAxisCommand(axis, target, false, "AvoidPosition").ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-FRONT-PICKER",
                        "PickerFrontUnit",
                        "FrontPicker " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target));
                }

                double readyTolerance = ResolveReadyAxisTolerance(baseAxis);
                int waitCode = await baseAxis.WaitMoveCompleteAsync(
                    target,
                    unit.ResolvePickerAxisMoveTimeoutMs(axis),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        "READY-FRONT-PICKER-MOVE",
                        "PickerFrontUnit",
                        "FrontPicker " + label + " Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ", reason=" + (baseAxis.LastMotionFailureMessage ?? string.Empty) +
                        ". " + BuildAxisState(label, baseAxis, target, readyTolerance));
                }

                if (!IsAxisInPosition(baseAxis, target, readyTolerance))
                {
                    return Fail(
                        "READY-FRONT-PICKER-CHECK",
                        "PickerFrontUnit",
                        "FrontPicker " + label + " Avoid 위치 최종 확인 실패. " +
                        BuildAxisState(label, baseAxis, target, readyTolerance));
                }

                LogStep("FrontPicker " + label + " Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-FRONT-PICKER-EX", "PickerFrontUnit", "FrontPicker " + label + " Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveRearPickerAxisAvoidAsync(PickerRearUnit unit, PickerAxis axis, string label, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("PickerRearUnit");

                BaseAxis baseAxis = ResolveRearPickerAxis(unit, axis);
                if (baseAxis == null)
                    return Fail("READY-REAR-PICKER-AXIS", "PickerRearUnit", "RearPicker " + label + " 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                double target = unit.GetPickerTeachingPosition(axis, "AvoidPosition");
                LogStep("RearPicker " + label + " Avoid 이동 시작. target=" + target);

                int result = await unit.MovePickerAxisCommand(axis, target, false, "AvoidPosition").ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-REAR-PICKER",
                        "PickerRearUnit",
                        "RearPicker " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target));
                }

                double readyTolerance = ResolveReadyAxisTolerance(baseAxis);
                int waitCode = await baseAxis.WaitMoveCompleteAsync(
                    target,
                    unit.ResolvePickerAxisMoveTimeoutMs(axis),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        "READY-REAR-PICKER-MOVE",
                        "PickerRearUnit",
                        "RearPicker " + label + " Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ", reason=" + (baseAxis.LastMotionFailureMessage ?? string.Empty) +
                        ". " + BuildAxisState(label, baseAxis, target, readyTolerance));
                }

                if (!IsAxisInPosition(baseAxis, target, readyTolerance))
                {
                    return Fail(
                        "READY-REAR-PICKER-CHECK",
                        "PickerRearUnit",
                        "RearPicker " + label + " Avoid 위치 최종 확인 실패. " +
                        BuildAxisState(label, baseAxis, target, readyTolerance));
                }

                LogStep("RearPicker " + label + " Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-REAR-PICKER-EX", "PickerRearUnit", "RearPicker " + label + " Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveVisionAxisAvoidAsync(VisionUnit unit, VisionAxis axis, string label, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("VisionUnit");

                BaseAxis baseAxis = unit.ResolveVisionAxis(axis);
                if (baseAxis == null)
                    return Fail("READY-SIDE-VISION-AXIS", "VisionUnit", label + " 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                double target = unit.GetVisionTeachingPosition(axis, "AvoidPosition");
                LogStep("Side Vision " + label + " Avoid 이동 시작. target=" + target);

                int result = await unit.MoveVisionAxisToTeachingPosition(axis, "AvoidPosition", false).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-SIDE-VISION",
                        "VisionUnit",
                        "Side Vision " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target));
                }

                int waitCode = await AwaitWithCancellationAsync(
                    unit.WaitVisionAxisMoveDoneInPosition(axis, target, ResolveReadyMoveTimeoutMs(baseAxis)),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        "READY-SIDE-VISION-MOVE",
                        "VisionUnit",
                        "Side Vision " + label + " Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ", reason=" + (baseAxis.LastMotionFailureMessage ?? string.Empty) +
                        ". " + BuildAxisState(label, baseAxis, target));
                }

                if (!unit.IsVisionAxisInPosition(axis, target, ResolveAxisTolerance(baseAxis)) || !IsAxisInPosition(baseAxis, target))
                {
                    return Fail(
                        "READY-SIDE-VISION-CHECK",
                        "VisionUnit",
                        "Side Vision " + label + " Avoid 위치 최종 확인 실패. " +
                        BuildAxisState(label, baseAxis, target));
                }

                LogStep("Side Vision " + label + " Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-SIDE-VISION-EX", "VisionUnit", "Side Vision " + label + " Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputStageAxisAvoidAsync(OutputStageUnit unit, BinStageAxis axis, string label, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("OutputStageUnit");

                BaseAxis baseAxis = ResolveOutputStageAxis(unit, axis);
                if (baseAxis == null)
                    return Fail("READY-OUTPUT-STAGE-AXIS", "OutputStageUnit", label + " 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                double target = ResolveOutputStageAvoidPosition(unit, axis);
                LogStep("OutputStage " + label + " Avoid 이동 시작. target=" + target);

                int result = await unit.MoveStageAxis(axis, target, false).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-OUTPUT-STAGE",
                        "OutputStageUnit",
                        "OutputStage " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target) +
                        BuildOutputStageFailure(unit));
                }

                int waitCode = await unit.WaitStageAxisMoveDoneInPosition(
                    axis,
                    target,
                    ResolveReadyMoveTimeoutMs(baseAxis),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        "READY-OUTPUT-STAGE-MOVE",
                        "OutputStageUnit",
                        "OutputStage " + label + " Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildAxisState(label, baseAxis, target) +
                        BuildOutputStageFailure(unit));
                }

                if (!unit.IsStageAxisInPosition(axis, target, ResolveAxisTolerance(baseAxis)) || !IsAxisInPosition(baseAxis, target))
                {
                    return Fail(
                        "READY-OUTPUT-STAGE-CHECK",
                        "OutputStageUnit",
                        "OutputStage " + label + " Avoid 위치 최종 확인 실패. " +
                        BuildAxisState(label, baseAxis, target) +
                        BuildOutputStageFailure(unit));
                }

                LogStep("OutputStage " + label + " Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-OUTPUT-STAGE-EX", "OutputStageUnit", "OutputStage " + label + " Avoid 이동 예외: " + ex.Message);
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

                int result = await unit.MoveInputStageAxis(axis, target, false).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        alarmCode,
                        "InputStageUnit",
                        "InputStage " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target) +
                        BuildInputStageFailure(unit));
                }

                int waitCode = await unit.WaitInputStageAxisInPositionResult(
                    axis,
                    target,
                    ResolveReadyMoveTimeoutMs(unit),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        alarmCode + "-MOVE",
                        "InputStageUnit",
                        "InputStage " + label + " Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildAxisState(label, baseAxis, target) +
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
                int result = await MoveInputFeederYAxisAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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
                int result = await MoveOutputFeederYAxisAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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
                int result = await MoveInputCassetteLifterZAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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
                int result = await MoveOutputCassetteLifterZAvoidAsync(unit, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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

        private async Task<int> MoveInputFeederYAxisAvoidAsync(InputFeederUnit unit, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("InputFeederUnit");

                if (unit.FeederY == null)
                    return Fail("READY-INPUT-FEEDER-AXIS", "InputFeederUnit", "InputFeederY 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                if (unit.Recipe == null)
                    return Fail("READY-INPUT-FEEDER-RECIPE", "InputFeederUnit", "InputFeederY Avoid 위치 레시피를 찾을 수 없습니다.");

                double target = unit.Recipe.AvoidPosition;
                LogStep("InputFeederY Avoid 이동 시작. target=" + target);

                int result = await unit.MoveToWaferFeederAvoidPosition(false).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-INPUT-FEEDER",
                        "InputFeederUnit",
                        "InputFeederY Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState("InputFeederY", unit.FeederY, target) +
                        BuildInputFeederFailure(unit));
                }

                int waitCode = await unit.WaitWaferFeederYMoveDoneInPosition(
                    target,
                    ResolveReadyMoveTimeoutMs(unit.FeederY),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        "READY-INPUT-FEEDER-MOVE",
                        "InputFeederUnit",
                        "InputFeederY Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildAxisState("InputFeederY", unit.FeederY, target) +
                        BuildInputFeederFailure(unit));
                }

                if (!IsAxisInPosition(unit.FeederY, target))
                {
                    return Fail(
                        "READY-INPUT-FEEDER-CHECK",
                        "InputFeederUnit",
                        "InputFeederY Avoid 위치 최종 확인 실패. " +
                        BuildAxisState("InputFeederY", unit.FeederY, target) +
                        BuildInputFeederFailure(unit));
                }

                LogStep("InputFeederY Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-INPUT-FEEDER-EX", "InputFeederUnit", "InputFeederY Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputFeederYAxisAvoidAsync(OutputFeederUnit unit, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("OutputFeederUnit");

                if (unit.FeederY == null)
                    return Fail("READY-OUTPUT-FEEDER-AXIS", "OutputFeederUnit", "OutputFeederY 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                if (unit.Recipe == null)
                    return Fail("READY-OUTPUT-FEEDER-RECIPE", "OutputFeederUnit", "OutputFeederY Avoid 위치 레시피를 찾을 수 없습니다.");

                double target = unit.Recipe.AvoidPosition;
                LogStep("OutputFeederY Avoid 이동 시작. target=" + target);

                int result = await unit.MoveToFeederAvoidPosition(false).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER",
                        "OutputFeederUnit",
                        "OutputFeederY Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                int waitCode = await unit.WaitBinFeederYMoveDoneInPosition(
                    target,
                    ResolveReadyMoveTimeoutMs(unit.FeederY),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-MOVE",
                        "OutputFeederUnit",
                        "OutputFeederY Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                if (!IsAxisInPosition(unit.FeederY, target))
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER-CHECK",
                        "OutputFeederUnit",
                        "OutputFeederY Avoid 위치 최종 확인 실패. " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                LogStep("OutputFeederY Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-OUTPUT-FEEDER-EX", "OutputFeederUnit", "OutputFeederY Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputCassetteLifterZAvoidAsync(InputCassetteUnit unit, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("InputCassetteUnit");

                if (unit.InputLifterZ == null)
                    return Fail("READY-INPUT-CASSETTE-AXIS", "InputCassetteUnit", "InputLifterZ 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                if (unit.Recipe == null)
                    return Fail("READY-INPUT-CASSETTE-RECIPE", "InputCassetteUnit", "InputLifterZ Avoid 위치 레시피를 찾을 수 없습니다.");

                double target = unit.Recipe.AvoidPosition;
                LogStep("InputLifterZ Avoid 이동 시작. target=" + target);

                int result = await unit.MoveWaferLifterZ(target, false, ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-INPUT-CASSETTE",
                        "InputCassetteUnit",
                        "InputLifterZ Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState("InputLifterZ", unit.InputLifterZ, target));
                }

                int waitCode = await unit.WaitWaferLifterZMoveDoneInPosition(
                    target,
                    ResolveReadyMoveTimeoutMs(unit.InputLifterZ),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        "READY-INPUT-CASSETTE-MOVE",
                        "InputCassetteUnit",
                        "InputLifterZ Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildAxisState("InputLifterZ", unit.InputLifterZ, target));
                }

                if (!IsAxisInPosition(unit.InputLifterZ, target))
                {
                    return Fail(
                        "READY-INPUT-CASSETTE-CHECK",
                        "InputCassetteUnit",
                        "InputLifterZ Avoid 위치 최종 확인 실패. " +
                        BuildAxisState("InputLifterZ", unit.InputLifterZ, target));
                }

                LogStep("InputLifterZ Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-INPUT-CASSETTE-EX", "InputCassetteUnit", "InputLifterZ Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputCassetteLifterZAvoidAsync(OutputCassetteUnit unit, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (unit == null)
                    return Skip("OutputCassetteUnit");

                if (unit.OutputLifterZ == null)
                    return Fail("READY-OUTPUT-CASSETTE-AXIS", "OutputCassetteUnit", "OutputLifterZ 축을 찾을 수 없어 Ready 이동을 진행할 수 없습니다.");

                if (unit.Recipe == null)
                    return Fail("READY-OUTPUT-CASSETTE-RECIPE", "OutputCassetteUnit", "OutputLifterZ Avoid 위치 레시피를 찾을 수 없습니다.");

                double target = unit.Recipe.AvoidPosition;
                LogStep("OutputLifterZ Avoid 이동 시작. target=" + target);

                int result = await unit.MoveBinLifterZ(target, false, ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-OUTPUT-CASSETTE",
                        "OutputCassetteUnit",
                        "OutputLifterZ Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState("OutputLifterZ", unit.OutputLifterZ, target));
                }

                int waitCode = await unit.WaitBinLifterZMoveDoneInPosition(
                    target,
                    ResolveReadyMoveTimeoutMs(unit.OutputLifterZ),
                    ct).ConfigureAwait(false);
                if (waitCode != 0)
                {
                    return Fail(
                        "READY-OUTPUT-CASSETTE-MOVE",
                        "OutputCassetteUnit",
                        "OutputLifterZ Avoid 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildAxisState("OutputLifterZ", unit.OutputLifterZ, target));
                }

                if (!IsAxisInPosition(unit.OutputLifterZ, target))
                {
                    return Fail(
                        "READY-OUTPUT-CASSETTE-CHECK",
                        "OutputCassetteUnit",
                        "OutputLifterZ Avoid 위치 최종 확인 실패. " +
                        BuildAxisState("OutputLifterZ", unit.OutputLifterZ, target));
                }

                LogStep("OutputLifterZ Avoid 이동 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("READY-OUTPUT-CASSETTE-EX", "OutputCassetteUnit", "OutputLifterZ Avoid 이동 예외: " + ex.Message);
            }
            finally
            {
            }
        }

        #endregion

        #region 공통 축·Teaching 해석 및 상태 진단

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

        private static BaseAxis ResolveFrontPickerAxis(PickerFrontUnit unit, PickerAxis axis)
        {
            try
            {
                if (unit == null)
                    return null;

                switch (axis)
                {
                    case PickerAxis.PickerX:
                        return unit.PickerX;
                    case PickerAxis.PickerY:
                        return unit.PickerY;
                    case PickerAxis.PickerT0:
                        return unit.PickerT0;
                    case PickerAxis.PickerZ0:
                        return unit.PickerZ0;
                    case PickerAxis.PickerT1:
                        return unit.PickerT1;
                    case PickerAxis.PickerZ1:
                        return unit.PickerZ1;
                    case PickerAxis.PickerT2:
                        return unit.PickerT2;
                    case PickerAxis.PickerZ2:
                        return unit.PickerZ2;
                    case PickerAxis.PickerT3:
                        return unit.PickerT3;
                    case PickerAxis.PickerZ3:
                        return unit.PickerZ3;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "FrontPicker 축 해석 실패. axis=" + axis + ", error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static BaseAxis ResolveRearPickerAxis(PickerRearUnit unit, PickerAxis axis)
        {
            try
            {
                if (unit == null)
                    return null;

                switch (axis)
                {
                    case PickerAxis.PickerX:
                        return unit.PickerX;
                    case PickerAxis.PickerY:
                        return unit.PickerY;
                    case PickerAxis.PickerT0:
                        return unit.PickerT0;
                    case PickerAxis.PickerZ0:
                        return unit.PickerZ0;
                    case PickerAxis.PickerT1:
                        return unit.PickerT1;
                    case PickerAxis.PickerZ1:
                        return unit.PickerZ1;
                    case PickerAxis.PickerT2:
                        return unit.PickerT2;
                    case PickerAxis.PickerZ2:
                        return unit.PickerZ2;
                    case PickerAxis.PickerT3:
                        return unit.PickerT3;
                    case PickerAxis.PickerZ3:
                        return unit.PickerZ3;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "RearPicker 축 해석 실패. axis=" + axis + ", error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static BaseAxis ResolveOutputStageAxis(OutputStageUnit unit, BinStageAxis axis)
        {
            try
            {
                if (unit == null)
                    return null;

                switch (axis)
                {
                    case BinStageAxis.NgBinY:
                        return unit.NgStage != null ? unit.NgStage.StageY : null;
                    case BinStageAxis.GoodBinY:
                        return unit.GoodStage != null ? unit.GoodStage.StageY : null;
                    case BinStageAxis.GoodBinZ:
                        return unit.GoodStage != null ? unit.GoodStage.StageZ : null;
                    case BinStageAxis.VisionX:
                        return unit.OutputCameraX;
                    case BinStageAxis.NgBinZ:
                        return null;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "OutputStage 축 해석 실패. axis=" + axis + ", error=" + ex.Message + " - Failed");
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

        private static double ResolveOutputStageAvoidPosition(OutputStageUnit unit, BinStageAxis axis)
        {
            try
            {
                if (unit == null || unit.Recipe == null)
                    return 0.0;

                switch (axis)
                {
                    case BinStageAxis.NgBinY:
                        return unit.Recipe.NGStageY != null ? unit.Recipe.NGStageY.AvoidPosition : 0.0;
                    case BinStageAxis.GoodBinY:
                        return unit.Recipe.GoodStageY != null ? unit.Recipe.GoodStageY.AvoidPosition : 0.0;
                    case BinStageAxis.GoodBinZ:
                        return unit.Recipe.GoodStageZ != null ? unit.Recipe.GoodStageZ.AvoidPosition : 0.0;
                    case BinStageAxis.VisionX:
                        return unit.Recipe.VisionX != null ? unit.Recipe.VisionX.AvoidPosition : 0.0;
                    default:
                        return 0.0;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "OutputStage Avoid 위치 해석 실패. axis=" + axis + ", error=" + ex.Message + " - Failed");
                return 0.0;
            }
            finally
            {
            }
        }

        private static int ResolveReadyMoveTimeoutMs(BaseAxis axis)
        {
            try
            {
                if (axis != null && axis.Setup != null && axis.Setup.MoveTimeoutMs > 0)
                    return axis.Setup.MoveTimeoutMs;

                return 10000;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "Ready 축 이동 Timeout 해석 실패. axis=" + (axis != null ? axis.Name : "-") +
                    ", error=" + ex.Message + " - Failed");
                return 10000;
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

        private static async Task<T> AwaitWithCancellationAsync<T>(Task<T> task, CancellationToken ct)
        {
            try
            {
                if (task == null)
                    throw new ArgumentNullException("task");

                Task cancelTask = Task.Delay(Timeout.Infinite, ct);
                Task finishedTask = await Task.WhenAny(task, cancelTask).ConfigureAwait(false);
                if (finishedTask != task)
                    ct.ThrowIfCancellationRequested();

                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }

        private static bool IsAxisInPosition(BaseAxis axis, double target)
        {
            return IsAxisInPosition(axis, target, ResolveAxisTolerance(axis));
        }

        private static bool IsAxisInPosition(BaseAxis axis, double target, double tolerance)
        {
            try
            {
                if (axis == null)
                    return false;

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

        private static double ResolveReadyAxisTolerance(BaseAxis axis)
        {
            return ResolveAxisTolerance(axis);
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
            return BuildAxisState(label, axis, target, ResolveAxisTolerance(axis));
        }

        private static string BuildAxisState(string label, BaseAxis axis, double target, double tolerance)
        {
            try
            {
                if (axis == null)
                    return label + "=null, target=" + target;

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

        // FeederRetreatPolicy의 실패 단계를 기존 Ready 알람 코드로 되돌린다.
        // 코드 체계를 바꾸면 현장 알람 대응 문서와 어긋나므로 그대로 유지한다.
        private static string ResolveInputFeederRetreatAlarmCode(FeederRetreatPhase phase)
        {
            switch (phase)
            {
                case FeederRetreatPhase.Unclamp: return "READY-INPUT-FEEDER-RECOVER-UNCLAMP";
                case FeederRetreatPhase.LiftUpBlocked: return "READY-INPUT-FEEDER-RECOVER-LIFT-BLOCK";
                // [옵션 2026-07-29] 제품 보유 시 +방향 이탈 이동 실패(부분 이동 금지로 미이동 포함).
                case FeederRetreatPhase.BackOff: return "READY-INPUT-FEEDER-RECOVER-BACKOFF";
                case FeederRetreatPhase.LiftUp: return "READY-INPUT-FEEDER-RECOVER-UP";
                case FeederRetreatPhase.MoveAvoid: return "READY-INPUT-FEEDER-RECOVER-AVOID";
                case FeederRetreatPhase.LiftDown: return "READY-INPUT-FEEDER-RECOVER-DOWN";
                default: return "READY-INPUT-FEEDER-RECOVER-CHECK";
            }
        }

        private static string ResolveOutputFeederRetreatAlarmCode(FeederRetreatPhase phase)
        {
            switch (phase)
            {
                case FeederRetreatPhase.Unclamp: return "READY-OUTPUT-FEEDER-RECOVER-UNCLAMP";
                case FeederRetreatPhase.LiftUpBlocked: return "READY-OUTPUT-FEEDER-RECOVER-LIFT-BLOCK";
                // [옵션 2026-07-29] 제품 보유 시 +방향 이탈 이동 실패(부분 이동 금지로 미이동 포함).
                case FeederRetreatPhase.BackOff: return "READY-OUTPUT-FEEDER-RECOVER-BACKOFF";
                case FeederRetreatPhase.LiftUp: return "READY-OUTPUT-FEEDER-RECOVER-UP";
                case FeederRetreatPhase.MoveAvoid: return "READY-OUTPUT-FEEDER-RECOVER-AVOID";
                case FeederRetreatPhase.LiftDown: return "READY-OUTPUT-FEEDER-RECOVER-DOWN";
                default: return "READY-OUTPUT-FEEDER-RECOVER-CHECK";
            }
        }

        private static string BuildInputFeederFailure(InputFeederUnit unit)
        {
            try
            {
                if (unit == null)
                    return ", InputFeederUnit=null";

                string state = Convert.ToString(unit.GetWaferFeederTransferState());
                string failure = unit.LastWaferFeederMoveFailureMessage;
                return ", inputFeederState=" + state +
                       (string.IsNullOrWhiteSpace(failure) ? string.Empty : ", lastFeederMoveFailure=" + failure);
            }
            catch (Exception ex)
            {
                return ", inputFeederState read failed. error=" + ex.Message;
            }
            finally
            {
            }
        }

        private static string BuildOutputFeederFailure(OutputFeederUnit unit)
        {
            try
            {
                if (unit == null)
                    return ", OutputFeederUnit=null";

                return ", outputFeederState=" + unit.DescribeBinFeederYMoveDoneState() +
                       ", lastFeederMoveFailure=" + unit.DescribeBinFeederYLastMotionFailure();
            }
            catch (Exception ex)
            {
                return ", outputFeederState read failed. error=" + ex.Message;
            }
            finally
            {
            }
        }

        private static string BuildOutputStageFailure(OutputStageUnit unit)
        {
            try
            {
                if (unit == null)
                    return ", OutputStageUnit=null";

                return ", goodStageState=" + unit.DescribeOutputStageInterlockState(BinSide.Good) +
                       ", ngStageState=" + unit.DescribeOutputStageInterlockState(BinSide.Ng);
            }
            catch (Exception ex)
            {
                return ", outputStageState read failed. error=" + ex.Message;
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
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineReadySequence",
                    "OutputStage Avoid 위치 확인 실패. error=" + ex.Message + " - Failed");
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

        #endregion
    }
}
