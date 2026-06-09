using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederLoadFromCassetteStep { Idle, CheckUnit, CheckReady, MoveCassetteLoadPosition, LiftUp, Clamp, VerifyRing, MoveMaterialDataToFeeder, Complete, Error }

    internal sealed class OutputFeederLoadFromCassetteSequence : OutputFeederSequenceBase<OutputFeederLoadFromCassetteStep>
    {
        public OutputFeederLoadFromCassetteSequence(MachineSequenceContext context) : base(context, OutputFeederSequenceKind.LoadFromCassette, "OutputFeederLoadFromCassetteSequence") { }
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
                    case OutputFeederLoadFromCassetteStep.CheckUnit: return Task.FromResult(CheckUnit(OutputFeederLoadFromCassetteStep.CheckReady));
                    case OutputFeederLoadFromCassetteStep.CheckReady: return Task.FromResult(CheckReady());
                    case OutputFeederLoadFromCassetteStep.MoveCassetteLoadPosition: return MoveCassetteLoadPositionAsync(ct);
                    case OutputFeederLoadFromCassetteStep.LiftUp: return LiftUpAsync(ct);
                    case OutputFeederLoadFromCassetteStep.Clamp: return ClampAsync(ct);
                    case OutputFeederLoadFromCassetteStep.VerifyRing: return VerifyRingAsync(ct);
                    case OutputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder: return Task.FromResult(MoveMaterialDataToFeeder());
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-FEEDER-CST-LOAD-EX", Name, "Load from cassette step failed: " + ex.Message)); }
        }

        private int CheckReady()
        {
            if (!Feeder.CheckFeederCassetteReady(Options.Side, Options.SlotIndex, TransferMode.Load))
                return Fail("OUT-FEEDER-CST-LOAD-READY", Feeder.Name, "Output feeder cassette load is not ready.");
            CurrentStep = OutputFeederLoadFromCassetteStep.MoveCassetteLoadPosition;
            return 0;
        }

        private async Task<int> MoveCassetteLoadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederCassetteLoadPosition(Options.Side, Options.SlotIndex, Options.FineMove), "cassette load", ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInCassetteLoadPosition(Options.Side), "cassette load", ct).ConfigureAwait(false);
            if (result != 0) return result;
            CurrentStep = OutputFeederLoadFromCassetteStep.LiftUp;
            return 0;
        }

        private async Task<int> LiftUpAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederUp()) return Fail("OUT-FEEDER-LIFT-UP", Feeder.Name, "Output feeder lift up failed. result=" + result);
            CurrentStep = OutputFeederLoadFromCassetteStep.Clamp;
            return 0;
        }

        private async Task<int> ClampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0) return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp failed. result=" + result);
            CurrentStep = OutputFeederLoadFromCassetteStep.VerifyRing;
            return 0;
        }

        private async Task<int> VerifyRingAsync(CancellationToken ct)
        {
            if (!IsHardwareBypass())
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected) return Fail("OUT-FEEDER-RING", Feeder.Name, "Output feeder ring was not detected after cassette load.");
            }
            CurrentStep = OutputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private int MoveMaterialDataToFeeder()
        {
            WaferMaterial wafer = MaterialStateService.GetWaferInCassette(ResolveOutputCassetteRole(), Options.SlotIndex);
            if (wafer != null)
                MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = MaterialLocationKind.OutputFeeder }, WaferMaterialState.WorkReady);
            Feeder.UpdateFeederMaterialState(MaterialState.Occupied);
            CurrentStep = OutputFeederLoadFromCassetteStep.Complete;
            return 0;
        }
    }
}
