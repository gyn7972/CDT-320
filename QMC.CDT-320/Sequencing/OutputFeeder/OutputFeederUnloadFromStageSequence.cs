using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederUnloadFromStageStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckOutputStageBinData,
        CheckFeederEmpty,
        MoveFeederStageUnloadPosition,
        PrepareFeederLiftUp,
        ClampFeederBin,
        VerifyBinDetected,
        MoveMaterialDataToFeeder,
        UpdateStageData,
        Complete,
        Error
    }

    internal sealed class OutputFeederUnloadFromStageSequence : OutputFeederSequenceBase<OutputFeederUnloadFromStageStep>
    {
        public OutputFeederUnloadFromStageSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.UnloadFromStage, "OutputFeederUnloadFromStageSequence")
        {
        }

        protected override OutputFeederUnloadFromStageStep IdleStep { get { return OutputFeederUnloadFromStageStep.Idle; } }
        protected override OutputFeederUnloadFromStageStep InitialStep { get { return OutputFeederUnloadFromStageStep.CheckUnit; } }
        protected override OutputFeederUnloadFromStageStep CompleteStep { get { return OutputFeederUnloadFromStageStep.Complete; } }
        protected override OutputFeederUnloadFromStageStep ErrorStep { get { return OutputFeederUnloadFromStageStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputFeederUnloadFromStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederUnloadFromStageStep.CheckTransferReady));

                    case OutputFeederUnloadFromStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    case OutputFeederUnloadFromStageStep.CheckOutputStageBinData:
                        return Task.FromResult(CheckOutputStageBinData());

                    case OutputFeederUnloadFromStageStep.CheckFeederEmpty:
                        return Task.FromResult(CheckFeederEmpty());

                    case OutputFeederUnloadFromStageStep.MoveFeederStageUnloadPosition:
                        return MoveFeederStageUnloadPositionAsync(ct);

                    case OutputFeederUnloadFromStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);

                    case OutputFeederUnloadFromStageStep.ClampFeederBin:
                        return ClampFeederBinAsync(ct);

                    case OutputFeederUnloadFromStageStep.VerifyBinDetected:
                        return VerifyBinDetectedAsync(ct);

                    case OutputFeederUnloadFromStageStep.MoveMaterialDataToFeeder:
                        return Task.FromResult(MoveMaterialDataToFeeder());

                    case OutputFeederUnloadFromStageStep.UpdateStageData:
                        return Task.FromResult(UpdateStageData());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-STAGE-UNLOAD-EX", Name, "Unload from stage step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            if (!Feeder.CheckFeederStageReady(Options.Side, TransferMode.Unload))
                return Fail("OUT-FEEDER-STAGE-UNLOAD-READY", Feeder.Name, "Output feeder stage unload is not ready.");

            CurrentStep = OutputFeederUnloadFromStageStep.CheckOutputStageBinData;
            return 0;
        }

        private int CheckOutputStageBinData()
        {
            return CheckStageReadyForFeederUnload(OutputFeederUnloadFromStageStep.CheckFeederEmpty);
        }

        private int CheckFeederEmpty()
        {
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material", "Output feeder data became occupied before stage unload. side=" + Options.Side);

            if (!IsHardwareBypass() && !Feeder.IsFeederEmpty())
                return Fail("OUT-FEEDER-SENSOR-OCCUPIED", Feeder.Name, "Output feeder sensor must be empty before stage unload.");

            CurrentStep = OutputFeederUnloadFromStageStep.MoveFeederStageUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageUnloadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageUnloadPosition(Options.Side, Options.FineMove), "stage unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageUnloadPosition(Options.Side), "stage unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadFromStageStep.PrepareFeederLiftUp;
            return 0;
        }

        private async Task<int> PrepareFeederLiftUpAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift up command failed. result=" + result);

            if (!Feeder.IsFeederUp())
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift up failed. result=" + result);

            CurrentStep = OutputFeederUnloadFromStageStep.ClampFeederBin;
            return 0;
        }

        private async Task<int> ClampFeederBinAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp failed. result=" + result);

            CurrentStep = OutputFeederUnloadFromStageStep.VerifyBinDetected;
            return 0;
        }

        private async Task<int> VerifyBinDetectedAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("OUT-STAGE-DATA-MISSING", "Material", "Output stage data disappeared before feeder material move. side=" + Options.Side);

            if (!IsHardwareBypass())
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("OUT-FEEDER-STAGE-UNLOAD-RING", Feeder.Name, "Output feeder ring was not detected after stage unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederUnloadFromStageStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private int MoveMaterialDataToFeeder()
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output stage wafer data was not found for feeder material move. side=" + Options.Side);
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material", "Output feeder data became occupied before stage to feeder material move.");

            MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = MaterialLocationKind.OutputFeeder }, WaferMaterialState.WorkReady);
            Feeder.UpdateFeederMaterialState(MaterialState.Occupied);
            CurrentStep = OutputFeederUnloadFromStageStep.UpdateStageData;
            return 0;
        }

        private int UpdateStageData()
        {
            if (ResolveStageWafer() != null)
                return Fail("OUT-STAGE-DATA-CLEAR", "Material", "Output stage data was not cleared after stage unload. side=" + Options.Side);

            if (ResolveFeederWafer() == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data was not created after stage unload. side=" + Options.Side);

            Context.Bus.Set("OutputStageEmpty");
            Context.Bus.Set("OutputFeederOccupied");
            CurrentStep = OutputFeederUnloadFromStageStep.Complete;
            return 0;
        }
    }
}
