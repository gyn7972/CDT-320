using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederUnloadToCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckFeederBinData,
        CheckCassetteTargetSlot,
        MoveFeederCassetteUnloadPosition,
        PrepareFeederUnclamp,
        MoveFeederAvoidPosition,
        VerifyBinReleasedToCassette,
        MoveMaterialDataToCassette,
        UpdateCassetteData,
        Complete,
        Error
    }

    internal sealed class OutputFeederUnloadToCassetteSequence : OutputFeederSequenceBase<OutputFeederUnloadToCassetteStep>
    {
        public OutputFeederUnloadToCassetteSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.UnloadToCassette, "OutputFeederUnloadToCassetteSequence")
        {
        }

        protected override OutputFeederUnloadToCassetteStep IdleStep { get { return OutputFeederUnloadToCassetteStep.Idle; } }
        protected override OutputFeederUnloadToCassetteStep InitialStep { get { return OutputFeederUnloadToCassetteStep.CheckUnit; } }
        protected override OutputFeederUnloadToCassetteStep CompleteStep { get { return OutputFeederUnloadToCassetteStep.Complete; } }
        protected override OutputFeederUnloadToCassetteStep ErrorStep { get { return OutputFeederUnloadToCassetteStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputFeederUnloadToCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederUnloadToCassetteStep.CheckTransferReady));

                    case OutputFeederUnloadToCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    case OutputFeederUnloadToCassetteStep.CheckFeederBinData:
                        return Task.FromResult(CheckFeederBinData());

                    case OutputFeederUnloadToCassetteStep.CheckCassetteTargetSlot:
                        return Task.FromResult(CheckCassetteTargetSlot());

                    case OutputFeederUnloadToCassetteStep.MoveFeederCassetteUnloadPosition:
                        return MoveFeederCassetteUnloadPositionAsync(ct);

                    case OutputFeederUnloadToCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    case OutputFeederUnloadToCassetteStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);

                    case OutputFeederUnloadToCassetteStep.VerifyBinReleasedToCassette:
                        return VerifyBinReleasedToCassetteAsync(ct);

                    case OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette:
                        return Task.FromResult(MoveMaterialDataToCassette());

                    case OutputFeederUnloadToCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-CST-UNLOAD-EX", Name, "Unload to cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            if (!Feeder.CheckFeederCassetteReady(Options.Side, Options.SlotIndex, TransferMode.Unload))
                return Fail("OUT-FEEDER-CST-UNLOAD-READY", Feeder.Name, "Output feeder cassette unload is not ready.");

            CurrentStep = OutputFeederUnloadToCassetteStep.CheckFeederBinData;
            return 0;
        }

        private int CheckFeederBinData()
        {
            return CheckCassetteSlotReadyForUnload(OutputFeederUnloadToCassetteStep.CheckCassetteTargetSlot);
        }

        private int CheckCassetteTargetSlot()
        {
            if (ResolveCassetteWafer() != null)
                return Fail("OUT-FEEDER-CST-SLOT-OCCUPIED", "Material", "Output cassette target slot became occupied before unload. role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveFeederCassetteUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederCassetteUnloadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederCassetteUnloadPosition(Options.Side, Options.SlotIndex, Options.FineMove), "cassette unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInCassetteUnloadPosition(Options.Side), "cassette unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadToCassetteStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp command failed. result=" + result);

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp failed. result=" + result);

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "cassette unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "cassette unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyBinReleasedToCassette;
            return 0;
        }

        private async Task<int> VerifyBinReleasedToCassetteAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data disappeared before cassette material move.");

            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("OUT-FEEDER-CST-RING", Feeder.Name, "Output feeder ring remained after cassette unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette;
            return 0;
        }

        private int MoveMaterialDataToCassette()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output feeder wafer data was not found for cassette material move.");

            MaterialStateService.PutWaferInCassette(
                wafer.WaferId,
                ResolveOutputCassetteRole(),
                Options.SlotIndex,
                wafer.CassetteLotId,
                wafer.SourceCassetteSlotPosition,
                WaferMaterialState.Finish);
            Feeder.ClearFeederMaterialState();
            CurrentStep = OutputFeederUnloadToCassetteStep.UpdateCassetteData;
            return 0;
        }

        private int UpdateCassetteData()
        {
            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (cassetteWafer == null)
                return Fail("OUT-FEEDER-CST-DATA-MISSING", "Material", "Output cassette data was not created after feeder unload. role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-CLEAR", "Material", "Output feeder data was not cleared after cassette unload. waferId=" + cassetteWafer.WaferId);

            Context.Bus.Set("OutputFeederEmpty");
            Context.Bus.Set("OutputCassetteSlotUpdated");
            CurrentStep = OutputFeederUnloadToCassetteStep.Complete;
            return 0;
        }
    }
}
