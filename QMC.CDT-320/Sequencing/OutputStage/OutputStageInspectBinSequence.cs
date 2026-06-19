using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStageInspectBinStep
    {
        Idle,
        CheckUnit,
        WaitTpuPlaceDone,
        MoveVisionXToProcess,
        CheckVisionXProcess,
        TriggerInspection,
        MoveVisionXToAvoid,
        CheckVisionXAvoid,
        NotifyTpuReadyForNextDie,
        Complete,
        Error
    }

    internal sealed class OutputStageInspectBinSequence : OutputStageSequenceBase<OutputStageInspectBinStep>
    {
        public OutputStageInspectBinSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.InspectBin, "OutputStageInspectBinSequence")
        {
        }

        protected override OutputStageInspectBinStep IdleStep { get { return OutputStageInspectBinStep.Idle; } }
        protected override OutputStageInspectBinStep InitialStep { get { return OutputStageInspectBinStep.CheckUnit; } }
        protected override OutputStageInspectBinStep CompleteStep { get { return OutputStageInspectBinStep.Complete; } }
        protected override OutputStageInspectBinStep ErrorStep { get { return OutputStageInspectBinStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputStageInspectBinStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStageInspectBinStep.WaitTpuPlaceDone));

                    // TPU 플레이스 완료 대기
                    case OutputStageInspectBinStep.WaitTpuPlaceDone:
                        return WaitTpuPlaceDoneAsync(ct);

                    // 비전 X로 프로세스 이동
                    case OutputStageInspectBinStep.MoveVisionXToProcess:
                        return MoveVisionXAsync("Process", "VisionX process for bin inspect", OutputStageInspectBinStep.CheckVisionXProcess, ct);

                    // 비전 X 프로세스 확인
                    case OutputStageInspectBinStep.CheckVisionXProcess:
                        return Task.FromResult(CheckVisionX("Process", "VisionX process for bin inspect", OutputStageInspectBinStep.TriggerInspection));

                    // 검사 트리거
                    case OutputStageInspectBinStep.TriggerInspection:
                        return TriggerInspectionAsync(ct);

                    // 비전 X로 어보이드 이동
                    case OutputStageInspectBinStep.MoveVisionXToAvoid:
                        return MoveVisionXAsync("Avoid", "VisionX avoid after bin inspect", OutputStageInspectBinStep.CheckVisionXAvoid, ct);

                    // 비전 X 어보이드 확인
                    case OutputStageInspectBinStep.CheckVisionXAvoid:
                        return Task.FromResult(CheckVisionX("Avoid", "VisionX avoid after bin inspect", OutputStageInspectBinStep.NotifyTpuReadyForNextDie));

                    // TPU 준비 위한 다음 다이 알림
                    case OutputStageInspectBinStep.NotifyTpuReadyForNextDie:
                        return Task.FromResult(NotifyTpuReadyForNextDie());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-INSPECT-EX", Name, "Inspect bin step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> WaitTpuPlaceDoneAsync(CancellationToken ct)
        {
            try
            {
                if (Stage.Tpu == null)
                    return Fail("OUT-STAGE-TPU-MISSING", Stage.Name, "TPU interface is not available.");

                bool done = await Stage.Tpu.WaitPlaceDoneAsync(ResolveTimeout(), ct).ConfigureAwait(false);
                if (!done)
                    return Fail("OUT-STAGE-TPU-PLACE-DONE", Stage.Name, "TPU place done timeout.");

                CurrentStep = OutputStageInspectBinStep.MoveVisionXToProcess;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TPU-PLACE-DONE-EX", Name, "TPU place done wait failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveVisionXAsync(
            string positionName,
            string description,
            OutputStageInspectBinStep nextStep,
            CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(
                    BinStageAxis.VisionX,
                    ResolveTarget(BinStageAxis.VisionX, positionName),
                    description,
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-INSPECT-VISION-MOVE-EX", Name, description + " move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckVisionX(
            string positionName,
            string description,
            OutputStageInspectBinStep nextStep)
        {
            try
            {
                double target = ResolveTarget(BinStageAxis.VisionX, positionName);
                if (!Stage.IsStageAxisInPosition(BinStageAxis.VisionX, target, ResolveTolerance(BinStageAxis.VisionX)))
                    return Fail("OUT-STAGE-INSPECT-VISION-CHECK", Stage.Name,
                        description + " final check failed. target=" + target + ". " +
                        BuildAxisState(BinStageAxis.VisionX, target));

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-INSPECT-VISION-CHECK-EX", Name, description + " check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> TriggerInspectionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                SimulatorBridge.Instance?.CameraExposeFlash("BIN");
                await Task.Delay(20, ct).ConfigureAwait(false);
                CurrentStep = OutputStageInspectBinStep.MoveVisionXToAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-INSPECT-TRIGGER-EX", Name, "Bin inspection trigger failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int NotifyTpuReadyForNextDie()
        {
            try
            {
                if (Stage.Tpu == null)
                    return Fail("OUT-STAGE-TPU-MISSING", Stage.Name, "TPU interface is not available.");

                Stage.Tpu.NotifyReadyForNextDie();
                CurrentStep = OutputStageInspectBinStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TPU-NEXT-EX", Name, "TPU ready for next die notify failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}

