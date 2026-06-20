using QMC.CDT320.Bin;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>장비 전체 모션을 안전한 Ready(Avoid) 위치로 복귀시키는 시퀀스입니다.</summary>
    internal sealed class MachineReadySequence
    {
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
            return new List<ReadyStep>
            {
                new ReadyStep("OutputStage VisionX Avoid", ct => MoveOutputStageVisionXOnlyAvoidAsync(ct)),
                new ReadyStep("Front/Rear Picker Z Avoid", ct => MoveFrontRearPickerZAxesAvoidAsync(ct)),
                new ReadyStep("Front/Rear Picker T Avoid", ct => MoveFrontRearPickerTAxesAvoidAsync(ct)),
                new ReadyStep("Front/Rear Picker Y Avoid", ct => MoveFrontRearPickerYAxesAvoidAsync(ct)),
                new ReadyStep("Front/Rear Picker X Avoid", ct => MoveFrontRearPickerXAxesAvoidAsync(ct)),
                new ReadyStep("InputStage VisionX Avoid", ct => MoveInputStageVisionXOnlyAvoidAsync(ct)),

                // 우선 아래는 확인 하면서 활성화하자. 주석 해제 시 TotalStepCount 가 자동으로 반영된다.
                //new ReadyStep("OutputStage Avoid", ct => MoveOutputStageAvoidAsync(ct)),
                //new ReadyStep("Front Picker Avoid", ct => MoveFrontPickerAvoidAsync(ct)),
                //new ReadyStep("Rear Picker Avoid", ct => MoveRearPickerAvoidAsync(ct)),
                //new ReadyStep("Side Vision Avoid", ct => MoveSideVisionAvoidAsync(ct)),
                //new ReadyStep("InputStage Avoid", ct => MoveInputStageAvoidAsync(ct)),
                //new ReadyStep("Input Feeder Avoid", ct => MoveInputFeederAvoidAsync(ct)),
                //new ReadyStep("Output Feeder Avoid", ct => MoveOutputFeederAvoidAsync(ct)),
                //new ReadyStep("Input Cassette Avoid", ct => MoveInputCassetteAvoidAsync(ct)),
                //new ReadyStep("Output Cassette Avoid", ct => MoveOutputCassetteAvoidAsync(ct)),
            };
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                LastErrorMessage = string.Empty;
                _completedStepCount = 0;
                LogStep("Ready 시퀀스 시작.");

                // 활성 단계를 순서대로 실행한다. 한 단계라도 실패하면 즉시 중단한다(기존 흐름 동일).
                foreach (ReadyStep step in _steps)
                {
                    int result = await RunReadyStepAsync(step.Name, () => step.Action(ct)).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

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

        private async Task<int> RunReadyStepAsync(string stepName, Func<Task<int>> action)
        {
            ReportProgress(MachineReadySequenceState.Running, stepName, stepName + " 진행 중입니다.");

            int result = await action().ConfigureAwait(false);
            if (result == 0)
            {
                _completedStepCount++;
                ReportProgress(MachineReadySequenceState.Running, stepName, stepName + " 완료.");
            }

            return result;
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

        private struct ReadyStep
        {
            public readonly string Name;
            public readonly Func<CancellationToken, Task<int>> Action;

            public ReadyStep(string name, Func<CancellationToken, Task<int>> action)
            {
                Name = name;
                Action = action;
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
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerZ0, "PickerZ0", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerZ1, "PickerZ1", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerZ2, "PickerZ2", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerZ3, "PickerZ3", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerZ0, "PickerZ0", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerZ1, "PickerZ1", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerZ2, "PickerZ2", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerZ3, "PickerZ3", ct);

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
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerT0, "PickerT0", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerT1, "PickerT1", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerT2, "PickerT2", ct);
                AddFrontPickerAxisAvoidTask(tasks, frontUnit, PickerAxis.PickerT3, "PickerT3", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerT0, "PickerT0", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerT1, "PickerT1", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerT2, "PickerT2", ct);
                AddRearPickerAxisAvoidTask(tasks, rearUnit, PickerAxis.PickerT3, "PickerT3", ct);

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

        private async Task<int> MoveFrontPickerAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var unit = _machine != null ? _machine.PickerFrontUnit : null;
                if (unit == null)
                    return Skip("FrontPickerUnit");

                LogStep("FrontPicker Avoid 이동 시작.");

                int result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerZ0, "PickerZ0", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerZ1, "PickerZ1", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerZ2, "PickerZ2", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerZ3, "PickerZ3", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerY, "PickerY", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerX, "PickerX", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerT0, "PickerT0", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerT1, "PickerT1", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerT2, "PickerT2", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveFrontPickerAxisAvoidAsync(unit, PickerAxis.PickerT3, "PickerT3", ct).ConfigureAwait(false);
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

                int result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerZ0, "PickerZ0", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerZ1, "PickerZ1", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerZ2, "PickerZ2", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerZ3, "PickerZ3", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerY, "PickerY", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerX, "PickerX", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerT0, "PickerT0", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerT1, "PickerT1", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerT2, "PickerT2", ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveRearPickerAxisAvoidAsync(unit, PickerAxis.PickerT3, "PickerT3", ct).ConfigureAwait(false);
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

                int result = await unit.MovePickerAxisToTeachingPosition(axis, "AvoidPosition", true).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-FRONT-PICKER",
                        "PickerFrontUnit",
                        "FrontPicker " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target));
                }

                AxisMoveWaitResult waitResult = await unit.WaitPickerAxisMoveDoneInPosition(
                    axis,
                    target,
                    unit.ResolvePickerAxisMoveTimeoutMs(axis),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode("READY-FRONT-PICKER", waitResult),
                        "PickerFrontUnit",
                        "FrontPicker " + label + " Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState(label, baseAxis, target)));
                }

                if (!unit.IsPickerAxisInPosition(axis, target, ResolveAxisTolerance(baseAxis)) || !IsAxisInPosition(baseAxis, target))
                {
                    return Fail(
                        "READY-FRONT-PICKER-CHECK",
                        "PickerFrontUnit",
                        "FrontPicker " + label + " Avoid 위치 최종 확인 실패. " +
                        BuildAxisState(label, baseAxis, target));
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

                int result = await unit.MovePickerAxisToTeachingPosition(axis, "AvoidPosition", true).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-REAR-PICKER",
                        "PickerRearUnit",
                        "RearPicker " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target));
                }

                AxisMoveWaitResult waitResult = await unit.WaitPickerAxisMoveDoneInPosition(
                    axis,
                    target,
                    unit.ResolvePickerAxisMoveTimeoutMs(axis),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode("READY-REAR-PICKER", waitResult),
                        "PickerRearUnit",
                        "RearPicker " + label + " Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState(label, baseAxis, target)));
                }

                if (!unit.IsPickerAxisInPosition(axis, target, ResolveAxisTolerance(baseAxis)) || !IsAxisInPosition(baseAxis, target))
                {
                    return Fail(
                        "READY-REAR-PICKER-CHECK",
                        "PickerRearUnit",
                        "RearPicker " + label + " Avoid 위치 최종 확인 실패. " +
                        BuildAxisState(label, baseAxis, target));
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

                int result = await unit.MoveVisionAxisToTeachingPosition(axis, "AvoidPosition", true).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-SIDE-VISION",
                        "VisionUnit",
                        "Side Vision " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target));
                }

                AxisMoveWaitResult waitResult = await AwaitWithCancellationAsync(
                    unit.WaitVisionAxisMoveDoneInPosition(axis, target, ResolveReadyMoveTimeoutMs(baseAxis)),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode("READY-SIDE-VISION", waitResult),
                        "VisionUnit",
                        "Side Vision " + label + " Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState(label, baseAxis, target)));
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

                int result = await unit.MoveStageAxis(axis, target, true).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-OUTPUT-STAGE",
                        "OutputStageUnit",
                        "OutputStage " + label + " Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState(label, baseAxis, target) +
                        BuildOutputStageFailure(unit));
                }

                AxisMoveWaitResult waitResult = await unit.WaitStageAxisMoveDoneInPosition(
                    axis,
                    target,
                    ResolveReadyMoveTimeoutMs(baseAxis),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode("READY-OUTPUT-STAGE", waitResult),
                        "OutputStageUnit",
                        "OutputStage " + label + " Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState(label, baseAxis, target)) +
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

                int result = await unit.MoveToWaferFeederAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-INPUT-FEEDER",
                        "InputFeederUnit",
                        "InputFeederY Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState("InputFeederY", unit.FeederY, target) +
                        BuildInputFeederFailure(unit));
                }

                AxisMoveWaitResult waitResult = await unit.WaitWaferFeederYMoveDoneInPosition(
                    target,
                    ResolveReadyMoveTimeoutMs(unit.FeederY),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode("READY-INPUT-FEEDER", waitResult),
                        "InputFeederUnit",
                        "InputFeederY Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState("InputFeederY", unit.FeederY, target)) +
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

                int result = await unit.MoveToFeederAvoidPosition(true).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-OUTPUT-FEEDER",
                        "OutputFeederUnit",
                        "OutputFeederY Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState("OutputFeederY", unit.FeederY, target) +
                        BuildOutputFeederFailure(unit));
                }

                AxisMoveWaitResult waitResult = await unit.WaitBinFeederYMoveDoneInPosition(
                    target,
                    ResolveReadyMoveTimeoutMs(unit.FeederY),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode("READY-OUTPUT-FEEDER", waitResult),
                        "OutputFeederUnit",
                        "OutputFeederY Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState("OutputFeederY", unit.FeederY, target)) +
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

                int result = await unit.MoveWaferLifterZ(target, true, ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-INPUT-CASSETTE",
                        "InputCassetteUnit",
                        "InputLifterZ Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState("InputLifterZ", unit.InputLifterZ, target));
                }

                AxisMoveWaitResult waitResult = await unit.WaitWaferLifterZMoveDoneInPosition(
                    target,
                    ResolveReadyMoveTimeoutMs(unit.InputLifterZ),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode("READY-INPUT-CASSETTE", waitResult),
                        "InputCassetteUnit",
                        "InputLifterZ Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState("InputLifterZ", unit.InputLifterZ, target)));
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

                int result = await unit.MoveBinLifterZ(target, true, ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "READY-OUTPUT-CASSETTE",
                        "OutputCassetteUnit",
                        "OutputLifterZ Avoid 이동 명령 실패. result=" + result + ". " +
                        BuildAxisState("OutputLifterZ", unit.OutputLifterZ, target));
                }

                AxisMoveWaitResult waitResult = await unit.WaitBinLifterZMoveDoneInPosition(
                    target,
                    ResolveReadyMoveTimeoutMs(unit.OutputLifterZ),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(
                        AxisMoveWaiter.ResolveAlarmCode("READY-OUTPUT-CASSETTE", waitResult),
                        "OutputCassetteUnit",
                        "OutputLifterZ Avoid 이동 완료/위치 확인 실패. " +
                        AxisMoveWaiter.FormatResult(waitResult, BuildAxisState("OutputLifterZ", unit.OutputLifterZ, target)));
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
    }
}
