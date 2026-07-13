using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStagePrepareUnloadStep
    {
        Idle,
        CheckUnit,
        CheckTargetSide,
        EnsureOutputFeederSafeBeforeStageMove,
        MoveTargetStageZToAvoid,
        CheckTargetStageZAvoid,
        MoveTargetStageYToUnload,
        CheckTargetStageYUnload,
        Complete,
        Error
    }

    internal sealed class OutputStagePrepareUnloadSequence : OutputStageSequenceBase<OutputStagePrepareUnloadStep>
    {
        public OutputStagePrepareUnloadSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.PrepareUnload, "OutputStagePrepareUnloadSequence")
        {
        }

        protected override OutputStagePrepareUnloadStep IdleStep { get { return OutputStagePrepareUnloadStep.Idle; } }
        protected override OutputStagePrepareUnloadStep InitialStep { get { return OutputStagePrepareUnloadStep.CheckUnit; } }
        protected override OutputStagePrepareUnloadStep CompleteStep { get { return OutputStagePrepareUnloadStep.Complete; } }
        protected override OutputStagePrepareUnloadStep ErrorStep { get { return OutputStagePrepareUnloadStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputStagePrepareUnloadStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStagePrepareUnloadStep.CheckTargetSide));

                    // 대상 사이드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetSide:
                        return CheckTargetSideAsync(ct);

                    // 메뉴얼 동작에서는 피더 Y를 움직이지 않고 어보이드 위치만 재확인
                    case OutputStagePrepareUnloadStep.EnsureOutputFeederSafeBeforeStageMove:
                        return Task.FromResult(EnsureOutputFeederSafeBeforeStageMove());

                    // 대상 스테이지 Z로 어보이드 이동
                    case OutputStagePrepareUnloadStep.MoveTargetStageZToAvoid:
                        return MoveTargetStageZToAvoidAsync(ct);

                    // 대상 스테이지 Z 어보이드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetStageZAvoid:
                        return Task.FromResult(CheckTargetStageZAvoid());

                    // 대상 스테이지 Y로 언로드 이동
                    case OutputStagePrepareUnloadStep.MoveTargetStageYToUnload:
                        return MoveTargetStageYToUnloadAsync(ct);

                    // 대상 스테이지 Y 언로드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetStageYUnload:
                        return Task.FromResult(CheckTargetStageYUnload());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-PREP-UNLOAD-EX", Name, "Prepare unload step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> CheckTargetSideAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options.Side != BinSide.Good && Options.Side != BinSide.Ng)
                    return Fail("OUT-STAGE-SIDE", Name, "Invalid output stage side: " + Options.Side);

                int pickerReady = await WaitPickersClearForOutputTransportAsync("OutputStage Unload 준비", ct).ConfigureAwait(false);
                if (pickerReady != 0)
                    return pickerReady;

                CurrentStep = OutputStagePrepareUnloadStep.EnsureOutputFeederSafeBeforeStageMove;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-SIDE-EX", Name, "Target side check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int EnsureOutputFeederSafeBeforeStageMove()
        {
            try
            {
                if (Options.AllowOutputFeederActuation)
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageZToAvoid;
                    return 0;
                }

                if (OutputFeeder == null)
                    return Fail("OUT-STAGE-FEEDER-NO-UNIT", "OutputFeederUnit",
                        "OutputStage Unload 준비 중 OutputFeederUnit을 찾을 수 없습니다. side=" + Options.Side);

                if (!OutputFeeder.IsBinFeederYInAvoidPosition())
                    return Fail("OUT-STAGE-FEEDER-Y-MANUAL-POS", OutputFeeder.Name,
                        "메뉴얼 OutputStage Unload는 OutputFeederY를 이동하지 않습니다. 시작 전 FeederY를 Avoid 위치로 이동하십시오. side=" +
                        Options.Side + ", " + OutputFeeder.DescribeBinFeederYMoveDoneState());

                CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageZToAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-FEEDER-SAFE-EX", "OutputFeederUnit",
                    "OutputStage Unload 전 OutputFeeder 안전 위치 확인 중 예외가 발생했습니다. side=" +
                    Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid before unload"))
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Avoid"),
                    Options.Side + " Z avoid before unload",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetStageZAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-AVOID-EX", Name, "Target stage Z avoid move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZAvoid()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid final check before unload"))
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Z-AVOID-CHECK", Stage.Name,
                        Options.Side + " Z avoid final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-AVOID-CHECK-EX", Name, "Target stage Z avoid check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageYToUnloadAsync(CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(
                    ResolveYAxis(Options.Side),
                    ResolveSideTarget(Options.Side, "Unload"),
                    Options.Side + " Y unload",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetStageYUnload;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Y-UNLOAD-EX", Name, "Target stage Y unload move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageYUnload()
        {
            try
            {
                BinStageAxis axis = ResolveYAxis(Options.Side);
                double target = ResolveSideTarget(Options.Side, "Unload");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Y-UNLOAD-CHECK", Stage.Name,
                        Options.Side + " Y unload final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Y-UNLOAD-CHECK-EX", Name, "Target stage Y unload check failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}

