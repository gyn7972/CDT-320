using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederUnloadToCassetteStep { Idle, CheckUnit, CheckReady, MoveCassetteUnloadPosition, LiftUp, Unclamp, LiftDown, VerifyRingClear, MoveMaterialDataToCassette, Complete, Error }

    internal sealed class OutputFeederUnloadToCassetteSequence : OutputFeederSequenceBase<OutputFeederUnloadToCassetteStep>
    {
        public OutputFeederUnloadToCassetteSequence(MachineSequenceContext context) : base(context, OutputFeederSequenceKind.UnloadToCassette, "OutputFeederUnloadToCassetteSequence") { }
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
                    case OutputFeederUnloadToCassetteStep.CheckUnit: return Task.FromResult(CheckUnit(OutputFeederUnloadToCassetteStep.CheckReady));
                    case OutputFeederUnloadToCassetteStep.CheckReady: return Task.FromResult(CheckReady());
                    case OutputFeederUnloadToCassetteStep.MoveCassetteUnloadPosition: return MoveCassetteUnloadPositionAsync(ct);
                    case OutputFeederUnloadToCassetteStep.LiftUp: return LiftUpAsync(ct);
                    case OutputFeederUnloadToCassetteStep.Unclamp: return UnclampAsync(ct);
                    case OutputFeederUnloadToCassetteStep.LiftDown: return LiftDownAsync(ct);
                    case OutputFeederUnloadToCassetteStep.VerifyRingClear: return VerifyRingClearAsync(ct);
                    case OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette: return Task.FromResult(MoveMaterialDataToCassette());
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-FEEDER-CST-UNLOAD-EX", Name, "Unload to cassette step failed: " + ex.Message)); }
        }

        private int CheckReady()
        {
            if (!Feeder.CheckFeederCassetteReady(Options.Side, Options.SlotIndex, TransferMode.Unload))
                return Fail("OUT-FEEDER-CST-UNLOAD-READY", Feeder.Name, "Output feeder cassette unload is not ready.");
            CurrentStep = OutputFeederUnloadToCassetteStep.MoveCassetteUnloadPosition;
            return 0;
        }

        private async Task<int> MoveCassetteUnloadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederCassetteUnloadPosition(Options.Side, Options.SlotIndex, Options.FineMove), "cassette unload", ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInCassetteUnloadPosition(Options.Side), "cassette unload", ct).ConfigureAwait(false);
            if (result != 0) return result;
            CurrentStep = OutputFeederUnloadToCassetteStep.LiftUp;
            return 0;
        }

        private async Task<int> LiftUpAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederUp()) return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift up failed. result=" + result);
            CurrentStep = OutputFeederUnloadToCassetteStep.Unclamp;
            return 0;
        }

        private async Task<int> UnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederUnclamped()) return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp failed. result=" + result);
            CurrentStep = OutputFeederUnloadToCassetteStep.LiftDown;
            return 0;
        }

        private async Task<int> LiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederDown()) return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift down failed. result=" + result);
            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyRingClear;
            return 0;
        }

        private async Task<int> VerifyRingClearAsync(CancellationToken ct)
        {
            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared) return Fail("OUT-FEEDER-CST-RING", Feeder.Name, "Output feeder ring remained after cassette unload.");
            }
            CurrentStep = OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette;
            return 0;
        }

        private int MoveMaterialDataToCassette()
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (wafer != null)
                MaterialStateService.PutWaferInCassette(wafer.WaferId, ResolveOutputCassetteRole(), Options.SlotIndex, wafer.CassetteLotId, wafer.SourceCassetteSlotPosition);
            Feeder.ClearFeederMaterialState();
            CurrentStep = OutputFeederUnloadToCassetteStep.Complete;
            return 0;
        }
    }
}
