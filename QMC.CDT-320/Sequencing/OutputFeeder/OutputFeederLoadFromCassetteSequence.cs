using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederLoadFromCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckOutputStageEmpty,
        CheckCassetteBinData,
        PrepareFeederUnclamp,
        PrepareFeederLiftDown,
        MoveFeederCassetteLoadPosition,
        VerifyFeederEmpty,
        VerifyBinDetected,
        ClampFeederBin,
        MoveFeederAvoidPosition,
        MoveMaterialDataToFeeder,
        UpdateCassetteData,
        Complete,
        Error
    }

    internal sealed class OutputFeederLoadFromCassetteSequence : OutputFeederSequenceBase<OutputFeederLoadFromCassetteStep>
    {
        public OutputFeederLoadFromCassetteSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.LoadFromCassette, "OutputFeederLoadFromCassetteSequence")
        {
        }

        protected override OutputFeederLoadFromCassetteStep IdleStep { get { return OutputFeederLoadFromCassetteStep.Idle; } }
        protected override OutputFeederLoadFromCassetteStep InitialStep { get { return OutputFeederLoadFromCassetteStep.CheckUnit; } }
        protected override OutputFeederLoadFromCassetteStep CompleteStep { get { return OutputFeederLoadFromCassetteStep.Complete; } }
        protected override OutputFeederLoadFromCassetteStep ErrorStep { get { return OutputFeederLoadFromCassetteStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputFeederLoadFromCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederLoadFromCassetteStep.CheckTransferReady));

                    case OutputFeederLoadFromCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    case OutputFeederLoadFromCassetteStep.CheckOutputStageEmpty:
                        return Task.FromResult(CheckTargetOutputStageEmpty());

                    case OutputFeederLoadFromCassetteStep.CheckCassetteBinData:
                        return Task.FromResult(CheckCassetteBinData());

                    case OutputFeederLoadFromCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    case OutputFeederLoadFromCassetteStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);

                    case OutputFeederLoadFromCassetteStep.MoveFeederCassetteLoadPosition:
                        return MoveFeederCassetteLoadPositionAsync(ct);

                    case OutputFeederLoadFromCassetteStep.VerifyFeederEmpty:
                        return Task.FromResult(VerifyFeederEmpty());

                    case OutputFeederLoadFromCassetteStep.VerifyBinDetected:
                        return VerifyBinDetectedAsync(ct);

                    case OutputFeederLoadFromCassetteStep.ClampFeederBin:
                        return ClampFeederBinAsync(ct);

                    case OutputFeederLoadFromCassetteStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);

                    case OutputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder:
                        return Task.FromResult(MoveMaterialDataToFeeder());

                    case OutputFeederLoadFromCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-CST-LOAD-EX", Name, "Load from cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            if (Options.SlotIndex < 0)
                return Fail("OUT-FEEDER-CST-SLOT", Feeder.Name, "Output cassette source slot is invalid. slot=" + Options.SlotIndex);

            string teachingReason;
            if (!Feeder.ValidateBinFeederYTeachingComplete(Options.Side, out teachingReason))
                return Fail("OUT-FEEDER-TEACHING", Feeder.Name, "OutputFeederY teaching is not complete. " + teachingReason);

            if (!Feeder.CheckFeederMoveReady())
                return Fail("OUT-FEEDER-MOVE-READY", Feeder.Name, "OutputFeederY is not ready to move.");

            CurrentStep = OutputFeederLoadFromCassetteStep.CheckOutputStageEmpty;
            return 0;
        }

        private int CheckTargetOutputStageEmpty()
        {
            MaterialLocationKind targetLocation = Options.Side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;

            WaferMaterial targetStageWafer = MaterialStateService.GetWaferAtLocation(targetLocation);
            if (targetStageWafer != null)
            {
                string stageName = Options.Side == BinSide.Ng ? "Output NGStage" : "Output GoodStage";
                return Fail(
                    "OUT-STAGE-DATA-OCCUPIED",
                    "Material",
                    stageName + " must be empty before cassette to feeder load. side=" + Options.Side +
                    ", waferId=" + targetStageWafer.WaferId);
            }

            CurrentStep = OutputFeederLoadFromCassetteStep.CheckCassetteBinData;
            return 0;
        }

        private int CheckCassetteBinData()
        {
            return CheckCassetteSlotReadyForLoad(OutputFeederLoadFromCassetteStep.PrepareFeederUnclamp);
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!Feeder.IsFeederUnclamped())
            {
                int unclamp = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (unclamp != 0)
                    return Fail("OUT-FEEDER-PREP-UNCLAMP", Feeder.Name, "Output feeder unclamp preparation command failed. result=" + unclamp);

                if (!Feeder.IsFeederUnclamped())
                    return Fail("OUT-FEEDER-PREP-UNCLAMP", Feeder.Name, "Output feeder unclamp preparation failed. result=" + unclamp);
            }

            CurrentStep = OutputFeederLoadFromCassetteStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!Feeder.IsFeederDown())
            {
                int down = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (down != 0)
                    return Fail("OUT-FEEDER-PREP-DOWN", Feeder.Name, "Output feeder lift down preparation command failed. result=" + down);

                if (!Feeder.IsFeederDown())
                    return Fail("OUT-FEEDER-PREP-DOWN", Feeder.Name, "Output feeder lift down preparation failed. result=" + down);
            }

            CurrentStep = OutputFeederLoadFromCassetteStep.MoveFeederCassetteLoadPosition;
            return 0;
        }

        private int VerifyFeederEmpty()
        {
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material", "Output feeder data must still be empty before cassette bin detect.");

            CurrentStep = OutputFeederLoadFromCassetteStep.VerifyBinDetected;
            return 0;
        }

        private async Task<int> MoveFeederCassetteLoadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederCassetteLoadPosition(Options.Side, Options.SlotIndex, Options.FineMove), "cassette load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInCassetteLoadPosition(Options.Side), "cassette load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadFromCassetteStep.VerifyFeederEmpty;
            return 0;
        }

        private async Task<int> ClampFeederBinAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp failed. result=" + result);

            if (Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp final check failed after cassette load. side=" + Options.Side);

            CurrentStep = OutputFeederLoadFromCassetteStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "cassette load avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "cassette load avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private async Task<int> VerifyBinDetectedAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveCassetteWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-CST-DATA-MISSING", "Material", "Output cassette wafer data disappeared before feeder material move. role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            if (!IsHardwareBypass())
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("OUT-FEEDER-RING", Feeder.Name, "Output feeder ring was not detected after cassette load. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederLoadFromCassetteStep.ClampFeederBin;
            return 0;
        }

        private int MoveMaterialDataToFeeder()
        {
            WaferMaterial wafer = ResolveCassetteWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output cassette wafer data was not found for feeder material move. role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = MaterialLocationKind.OutputFeeder }, WaferMaterialState.WorkReady);
            Feeder.UpdateFeederMaterialState(MaterialState.Occupied);
            Context.Bus.Set("OutputFeederOccupied");
            CurrentStep = OutputFeederLoadFromCassetteStep.UpdateCassetteData;
            return 0;
        }

        private int UpdateCassetteData()
        {
            WaferMaterial feederWafer = ResolveFeederWafer();
            if (feederWafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data was not found after cassette load material move.");

            WaferMaterial sourceWafer = ResolveCassetteWafer();
            if (sourceWafer != null &&
                string.Equals(sourceWafer.WaferId, feederWafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("OUT-FEEDER-CST-UPDATE", "Material", "Output cassette slot data was not cleared after feeder load. waferId=" + feederWafer.WaferId);

            Context.Bus.Set("OutputCassetteSlotUpdated");
            CurrentStep = OutputFeederLoadFromCassetteStep.Complete;
            return 0;
        }
    }
}
