using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    // Output 작업의 안전 전제: 리소스 점유, Picker Avoid 게이트, 축/자재 인터락 재확인, 옵션 빌더.
    // 모션을 시작하기 전에 통과해야 하는 조건들이 여기 모여 있다.
    // OutputSequence.cs에서 순수 이동한 것이다(2026-07-27, 동작 변경 없음).
    public partial class OutputSequence
    {
        private async Task<SequenceResourceLease> AcquireOutputStageAreaAsync(BinSide side, string holder, CancellationToken ct)
        {
            try
            {
                SequenceResourceKind resource = side == BinSide.Ng
                    ? SequenceResourceKind.OutputNgStageArea
                    : SequenceResourceKind.OutputGoodStageArea;

                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;
                return await AcquireResourceForRunAsync(
                    resource,
                    safeHolder + ":" + side,
                    30000,
                    ct,
                    IsAutoOutputLoaderBatchActive,
                    OutputLoaderBatchDrainReason).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
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

        private void SetOutputLoaderActive(bool active, string holder)
        {
            if (Mode == SequenceRunMode.Auto)
                return;

            if (!active || Context == null || Context.Bus == null)
                return;

            // 현재 기준: Output 로더 동작 중에는 Picker 공정 신규 진입을 막는다.
            Context.Bus.Set(OutputLoaderActiveSignal);
            WriteLog("OutputLoaderActive", holder + " 시작: Picker 신규 공정 진입을 대기시킵니다. - Set");
        }

        private void ResetOutputLoaderActive(bool active, string holder)
        {
            if (Mode == SequenceRunMode.Auto)
                return;

            if (!active || Context == null || Context.Bus == null)
                return;

            Context.Bus.Reset(OutputLoaderActiveSignal);
            WriteLog("OutputLoaderActive", holder + " 종료: Picker 신규 공정 진입 대기를 해제합니다. - Reset");
        }

        private async Task<int> ExecuteWithOutputPickerAvoidGateAsync(string holder, CancellationToken ct, Func<Task<int>> action)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;

            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await EnsureOutputPickersAvoidBeforeFeederMoveAsync(safeHolder, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                string reason;
                if (!AreOutputPickersAvoidAndStopped(out reason))
                    return Fail("OUT-PICKER-AVOID-STATE", "OutputSequence",
                        safeHolder + " 불가: Front/Rear Picker가 Avoid 정지 상태가 아닙니다. " + reason);

                return await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-PICKER-GATE-EX", "OutputSequence",
                    safeHolder + " Picker Avoid gate 처리 중 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureOutputPickersAvoidBeforeFeederMoveAsync(string holder, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                    {
                        Context.StopIfCycleStopRequested(
                            "OutputSequence.PickerAvoidGate:" + safeHolder,
                            IsAutoOutputLoaderBatchActive || ShouldDeferCycleStopForPickerHeldDieOutputDrain(),
                            OutputLoaderBatchDrainReason);
                    }

                    string reason;
                    if (AreOutputPickersAvoidAndStopped(out reason))
                    {
                        if (waitLogged)
                            WriteLog("OutputPickerAvoidGate", safeHolder + " 전 Picker Avoid 대기 완료. - Ok");
                        return 0;
                    }

                    // 현재 기준: 로더는 Picker를 직접 이동하지 않고 PickerSequence가 Avoid로 빠질 때까지 대기한다.
                    if (Mode != SequenceRunMode.Auto)
                        return Fail("OUT-PICKER-AVOID-STATE", "OutputSequence",
                            safeHolder + " 불가: Front/Rear Picker가 Avoid 정지 상태가 아닙니다. " + reason);

                    if (!waitLogged)
                    {
                        WriteLog("OutputPickerAvoidGate",
                            safeHolder + " 전 Picker Avoid 대기 중입니다. reason=" + reason + " - Wait");
                        if (Context != null)
                            Context.LogPublic("[UNIT-OUTPUT] WAIT Picker Avoid before " + safeHolder + ". " + reason);
                        waitLogged = true;
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-PICKER-AVOID-EX", "OutputSequence",
                    "Output feeder/stage 이동 전 Picker Avoid 처리 중 예외 발생. holder=" + holder +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool AreOutputPickersAvoidAndStopped(out string reason)
        {
            reason = string.Empty;

            var machine = Context != null ? Context.Machine : null;
            var front = machine != null ? machine.PickerFrontUnit : null;
            var rear = machine != null ? machine.PickerRearUnit : null;

            if (IsFrontPickerEnabled(front))
            {
                if (!front.IsFrontPickerInAvoidPosition())
                {
                    reason = "FrontPicker가 Avoid 위치가 아닙니다.";
                    return false;
                }

                string movingReason;
                if (TryDescribeMovingPickerAxes(front.Axes, "FrontPicker", out movingReason))
                {
                    reason = movingReason;
                    return false;
                }
            }

            if (IsRearPickerEnabled(rear))
            {
                if (!rear.IsRearPickerInAvoidPosition())
                {
                    reason = "RearPicker가 Avoid 위치가 아닙니다.";
                    return false;
                }

                string movingReason;
                if (TryDescribeMovingPickerAxes(rear.Axes, "RearPicker", out movingReason))
                {
                    reason = movingReason;
                    return false;
                }
            }

            return true;
        }

        private bool ShouldDeferCycleStopForPickerHeldDieOutputDrain()
        {
            if (Mode != SequenceRunMode.Auto ||
                Context == null ||
                !Context.IsCycleStopRequested)
            {
                return false;
            }

            if (Context.Controller != null && Context.Controller.Status == EquipmentStatus.Alarm)
                return false;

            return HasPickerHeldTargetDieForOutputDrain();
        }

        private bool HasPickerHeldTargetDieForOutputDrain()
        {
            AutoSequenceCoordinatorGate gate = Context != null ? Context.AutoSequenceGate : null;
            bool frontActive = gate == null || gate.IsPickerSideConfiguredForRun(PickerSequenceSide.Front);
            bool rearActive = gate == null || gate.IsPickerSideConfiguredForRun(PickerSequenceSide.Rear);

            return (frontActive && HasInputTargetDieAtPicker(MaterialLocationKind.PickerFront)) ||
                   (rearActive && HasInputTargetDieAtPicker(MaterialLocationKind.PickerRear));
        }

        private static bool HasOutputFeederMaterialForDrain()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null;
        }

        private static bool HasInputTargetDieAtPicker(MaterialLocationKind location)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                if (die != null && die.IsInputTarget)
                    return true;
            }

            return false;
        }

        private static bool IsFrontPickerEnabled(PickerFrontUnit front)
        {
            return front != null && front.Config != null && front.Config.UseUnit;
        }

        private static bool IsRearPickerEnabled(PickerRearUnit rear)
        {
            return rear != null && rear.Config != null && rear.Config.UseUnit;
        }

        private static bool TryDescribeMovingPickerAxes(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<PickerAxis, QMC.Common.Motion.BaseAxis>> axes, string pickerName, out string reason)
        {
            reason = string.Empty;
            if (axes == null)
                return false;

            foreach (var pair in axes)
            {
                if (pair.Value != null && pair.Value.IsMoving)
                {
                    reason = pickerName + " " + pair.Key + " 축이 이동 중입니다.";
                    return true;
                }
            }

            return false;
        }

        private async Task<SequenceResourceLease> AcquireOutputPlaceAreaAsync(string holder, CancellationToken ct)
        {
            try
            {
                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;
                return await AcquireResourceForRunAsync(
                    SequenceResourceKind.OutputPlaceArea,
                    safeHolder,
                    30000,
                    ct,
                    IsAutoOutputLoaderBatchActive,
                    OutputLoaderBatchDrainReason).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
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

        private OutputCassetteSequenceOptions BuildCassetteOptions(TargetCassette target, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            var options = OutputCassetteSequenceOptions.Default();
            options.TargetCassette = target;
            options.FineMove = bFine;
            options.MoveTimeoutMs = moveTimeoutMs > 0 ? moveTimeoutMs : options.MoveTimeoutMs;
            options.GoodLevelCount = Context != null && Context.Machine != null && Context.Machine.OutputCassetteUnit != null && Context.Machine.OutputCassetteUnit.Config != null
                ? Math.Max(1, Math.Min(2, Context.Machine.OutputCassetteUnit.Config.SelectedCassetteLevel))
                : 2;
            options.RunMode = Mode;
            options.StartMode = startMode;
            return options;
        }

        private OutputStageSequenceOptions BuildStageOptions(BinSide side, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            var options = OutputStageSequenceOptions.Default();
            options.Side = side;
            options.FineMove = bFine;
            options.MoveTimeoutMs = moveTimeoutMs > 0 ? moveTimeoutMs : options.MoveTimeoutMs;
            options.RunMode = Mode;
            options.StartMode = startMode;
            options.Grade = side == BinSide.Ng ? DieGrade.Ng : DieGrade.Good;
            return options;
        }

        // 재개/액션 실행 직전 인터락 재확인 (GYN 2026.07.03 TODO 구현, Output):
        // - 해당 액션이 사용하는 축의 Alarm/Servo/Home 상태를 확인한다.
        // - 액션이 전제하는 자재 배치(Feeder Bin 유무, 대상 Stage Bin 유무)를 영속 Material 기준으로 확인한다.
        // - 불일치 시 모션을 시작하지 않고 알람으로 정지한다(fail-closed).
        // Picker 안전 조건은 각 구간의 ExecuteWithOutputPickerAvoidGateAsync가 확인한다.
        private int CheckOutputWorkInterlocksBeforeExecute(
            string workLabel,
            BinSide side,
            bool expectFeederBin,
            bool? expectStageBin)
        {
            try
            {
                var machine = Context != null ? Context.Machine : null;
                var feeder = machine != null ? machine.OutputFeederUnit : null;
                var stage = machine != null ? machine.OutputStageUnit : null;
                var cassette = machine != null ? machine.OutputCassetteUnit : null;
                string safeLabel = string.IsNullOrWhiteSpace(workLabel) ? "OutputWork" : workLabel;

                var axes = new List<KeyValuePair<string, QMC.Common.Motion.BaseAxis>>();
                AddOutputAxisIfPresent(axes, "OutputFeederY", feeder != null ? feeder.FeederY : null);
                AddOutputAxisIfPresent(axes, "OutputLifterZ", cassette != null ? cassette.OutputLifterZ : null);
                if (side == BinSide.Ng)
                {
                    AddOutputAxisIfPresent(axes, "NgStageY",
                        stage != null && stage.NgStage != null ? stage.NgStage.StageY : null);
                }
                else
                {
                    AddOutputAxisIfPresent(axes, "GoodStageY",
                        stage != null && stage.GoodStage != null ? stage.GoodStage.StageY : null);
                    AddOutputAxisIfPresent(axes, "GoodStageZ",
                        stage != null && stage.GoodStage != null ? stage.GoodStage.StageZ : null);
                }

                string reason;
                if (!AreOutputAxesReadyForWork(axes, feeder, cassette, out reason))
                    return Fail("OUT-ILK-AXIS", "OutputSequence",
                        safeLabel + " 시작 전 축 인터락 조건이 맞지 않습니다. " + reason);

                WaferMaterial feederBin = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (expectFeederBin && feederBin == null)
                    return Fail("OUT-ILK-MATERIAL", "OutputSequence",
                        safeLabel + " 시작 전 OutputFeeder에 처리할 Bin이 없습니다. 자재 상태를 확인하세요.");
                if (!expectFeederBin && feederBin != null)
                    return Fail("OUT-ILK-MATERIAL", "OutputSequence",
                        safeLabel + " 시작 전 OutputFeeder에 Bin이 남아 있습니다. bin=" + (feederBin.WaferId ?? "") +
                        ". 잔류 Bin 처리(OUTPUT UNLOAD) 후 다시 시작하세요.");

                if (expectStageBin.HasValue)
                {
                    MaterialLocationKind stageLocation = side == BinSide.Ng
                        ? MaterialLocationKind.OutputStageNg
                        : MaterialLocationKind.OutputStageGood;
                    WaferMaterial stageBin = MaterialStateService.GetWaferAtLocation(stageLocation);
                    if (expectStageBin.Value && stageBin == null)
                        return Fail("OUT-ILK-MATERIAL", "OutputSequence",
                            safeLabel + " 시작 전 " + side + " OutputStage에 배출할 Bin이 없습니다. 자재 상태를 확인하세요.");
                    if (!expectStageBin.Value && stageBin != null)
                        return Fail("OUT-ILK-MATERIAL", "OutputSequence",
                            safeLabel + " 시작 전 " + side + " OutputStage에 Bin이 남아 있습니다. bin=" + (stageBin.WaferId ?? "") +
                            ". 기존 Bin 배출 후 다시 시작하세요.");
                }

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-ILK-EX", "OutputSequence",
                    workLabel + " 시작 전 인터락 재확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static void AddOutputAxisIfPresent(
            List<KeyValuePair<string, QMC.Common.Motion.BaseAxis>> axes,
            string name,
            QMC.Common.Motion.BaseAxis axis)
        {
            if (axes != null && axis != null)
                axes.Add(new KeyValuePair<string, QMC.Common.Motion.BaseAxis>(name, axis));
        }

        // 액션에서 사용할 축의 공통 안전 조건: 알람 없음, 서보 ON, 원점 복귀 완료.
        // 정지(IsMoving) 확인은 OutputSequence가 단독으로 이동시키는 FeederY/LifterZ에만 적용한다.
        // (Stage/OutputVisionX는 후검사(OutputPostPlaceInspection)와 리소스 lease로 병행될 수 있어
        //  lease 획득 전 이동 여부를 요구하면 정상 병행 동작을 오탐하게 된다.)
        private static bool AreOutputAxesReadyForWork(
            List<KeyValuePair<string, QMC.Common.Motion.BaseAxis>> axes,
            OutputFeederUnit feeder,
            OutputCassetteUnit cassette,
            out string reason)
        {
            reason = string.Empty;
            if (axes != null)
            {
                foreach (var pair in axes)
                {
                    QMC.Common.Motion.BaseAxis axis = pair.Value;
                    if (axis == null)
                        continue;

                    if (axis.IsAlarm)
                    {
                        reason = pair.Key + " 축 알람이 ON 상태입니다.";
                        return false;
                    }

                    if (!axis.IsServoOn)
                    {
                        reason = pair.Key + " 축 서보가 OFF 상태입니다.";
                        return false;
                    }

                    if (!axis.IsHomeDone)
                    {
                        reason = pair.Key + " 축 원점 복귀(Home)가 완료되지 않았습니다.";
                        return false;
                    }
                }
            }

            QMC.Common.Motion.BaseAxis feederY = feeder != null ? feeder.FeederY : null;
            if (feederY != null && feederY.IsMoving)
            {
                reason = "OutputFeederY 축이 아직 이동 중입니다.";
                return false;
            }

            QMC.Common.Motion.BaseAxis lifterZ = cassette != null ? cassette.OutputLifterZ : null;
            if (lifterZ != null && lifterZ.IsMoving)
            {
                reason = "OutputLifterZ 축이 아직 이동 중입니다.";
                return false;
            }

            return true;
        }
    }
}
