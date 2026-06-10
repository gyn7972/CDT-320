using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederLoadToStageStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckFeederBinData,
        CheckOutputStageEmpty,
        MoveFeederStageLoadPosition,
        PrepareFeederUnclamp,
        PrepareFeederLiftDown,
        VerifyBinTransferredToStage,
        MoveMaterialDataToStage,
        UpdateFeederData,
        Complete,
        Error
    }

    internal sealed class OutputFeederLoadToStageSequence : OutputFeederSequenceBase<OutputFeederLoadToStageStep>
    {
        public OutputFeederLoadToStageSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.LoadToStage, "OutputFeederLoadToStageSequence")
        {
        }

        protected override OutputFeederLoadToStageStep IdleStep { get { return OutputFeederLoadToStageStep.Idle; } }
        protected override OutputFeederLoadToStageStep InitialStep { get { return OutputFeederLoadToStageStep.CheckUnit; } }
        protected override OutputFeederLoadToStageStep CompleteStep { get { return OutputFeederLoadToStageStep.Complete; } }
        protected override OutputFeederLoadToStageStep ErrorStep { get { return OutputFeederLoadToStageStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputFeederLoadToStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederLoadToStageStep.CheckTransferReady));

                    case OutputFeederLoadToStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    case OutputFeederLoadToStageStep.CheckFeederBinData:
                        return Task.FromResult(CheckFeederBinData());

                    case OutputFeederLoadToStageStep.CheckOutputStageEmpty:
                        return Task.FromResult(CheckOutputStageEmpty());

                    case OutputFeederLoadToStageStep.MoveFeederStageLoadPosition:
                        return MoveFeederStageLoadPositionAsync(ct);

                    case OutputFeederLoadToStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    case OutputFeederLoadToStageStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);

                    case OutputFeederLoadToStageStep.VerifyBinTransferredToStage:
                        return VerifyBinTransferredToStageAsync(ct);

                    case OutputFeederLoadToStageStep.MoveMaterialDataToStage:
                        return Task.FromResult(MoveMaterialDataToStage());

                    case OutputFeederLoadToStageStep.UpdateFeederData:
                        return Task.FromResult(UpdateFeederData());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-STAGE-LOAD-EX", Name, "Load to stage step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            if (!Feeder.CheckFeederStageReady(Options.Side, TransferMode.Load))
                return Fail("OUT-FEEDER-STAGE-LOAD-READY", Feeder.Name, "Output feeder stage load is not ready.");

            CurrentStep = OutputFeederLoadToStageStep.CheckFeederBinData;
            return 0;
        }

        private int CheckFeederBinData()
        {
            return CheckFeederReadyForStageLoad(OutputFeederLoadToStageStep.CheckOutputStageEmpty);
        }

        private int CheckOutputStageEmpty()
        {
            if (ResolveStageWafer() != null)
                return Fail("OUT-STAGE-DATA-OCCUPIED", "Material", "Output stage data became occupied before feeder to stage load. side=" + Options.Side);

            CurrentStep = OutputFeederLoadToStageStep.MoveFeederStageLoadPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageLoadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageLoadPosition(Options.Side, Options.FineMove), "stage load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageLoadPosition(Options.Side), "stage load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadToStageStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp command failed. result=" + result);

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp failed. result=" + result);

            CurrentStep = OutputFeederLoadToStageStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift down command failed. result=" + result);

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift down failed. result=" + result);

            CurrentStep = OutputFeederLoadToStageStep.VerifyBinTransferredToStage;
            return 0;
        }

        private async Task<int> VerifyBinTransferredToStageAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data disappeared before stage material move.");

            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("OUT-FEEDER-STAGE-RING", Feeder.Name, "Output feeder ring remained after stage load. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederLoadToStageStep.MoveMaterialDataToStage;
            return 0;
        }

        private int MoveMaterialDataToStage()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output feeder wafer data was not found for stage material move.");
            if (ResolveStageWafer() != null)
                return Fail("OUT-STAGE-DATA-OCCUPIED", "Material", "Output stage data became occupied before feeder to stage material move. side=" + Options.Side);

            MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = ResolveOutputStageLocation() }, WaferMaterialState.Working);
            Feeder.ClearFeederMaterialState();
            CurrentStep = OutputFeederLoadToStageStep.UpdateFeederData;
            return 0;
        }

        private int UpdateFeederData()
        {
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-CLEAR", "Material", "Output feeder data was not cleared after stage load. side=" + Options.Side);

            if (ResolveStageWafer() == null)
                return Fail("OUT-STAGE-DATA-MISSING", "Material", "Output stage data was not created after feeder to stage load. side=" + Options.Side);

            Context.Bus.Set("OutputFeederEmpty");
            Context.Bus.Set("OutputStageOccupied");
            CurrentStep = OutputFeederLoadToStageStep.Complete;
            return 0;
        }
    }
}
