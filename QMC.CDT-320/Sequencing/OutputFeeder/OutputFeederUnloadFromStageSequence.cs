using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederUnloadFromStageStep { Idle, CheckUnit, CheckReady, MoveStageUnloadPosition, LiftUp, Clamp, VerifyRing, MoveMaterialDataToFeeder, Complete, Error }

    internal sealed class OutputFeederUnloadFromStageSequence : OutputFeederSequenceBase<OutputFeederUnloadFromStageStep>
    {
        public OutputFeederUnloadFromStageSequence(MachineSequenceContext context) : base(context, OutputFeederSequenceKind.UnloadFromStage, "OutputFeederUnloadFromStageSequence") { }
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
                    case OutputFeederUnloadFromStageStep.CheckUnit: return Task.FromResult(CheckUnit(OutputFeederUnloadFromStageStep.CheckReady));
                    case OutputFeederUnloadFromStageStep.CheckReady: return Task.FromResult(CheckReady());
                    case OutputFeederUnloadFromStageStep.MoveStageUnloadPosition: return MoveStageUnloadPositionAsync(ct);
                    case OutputFeederUnloadFromStageStep.LiftUp: return LiftUpAsync(ct);
                    case OutputFeederUnloadFromStageStep.Clamp: return ClampAsync(ct);
                    case OutputFeederUnloadFromStageStep.VerifyRing: return VerifyRingAsync(ct);
                    case OutputFeederUnloadFromStageStep.MoveMaterialDataToFeeder: return Task.FromResult(MoveMaterialDataToFeeder());
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-FEEDER-STAGE-UNLOAD-EX", Name, "Unload from stage step failed: " + ex.Message)); }
        }

        private int CheckReady()
        {
            if (!Feeder.CheckFeederStageReady(Options.Side, TransferMode.Unload))
                return Fail("OUT-FEEDER-STAGE-UNLOAD-READY", Feeder.Name, "Output feeder stage unload is not ready.");
            CurrentStep = OutputFeederUnloadFromStageStep.MoveStageUnloadPosition;
            return 0;
        }

        private async Task<int> MoveStageUnloadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageUnloadPosition(Options.Side, Options.FineMove), "stage unload", ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageUnloadPosition(Options.Side), "stage unload", ct).ConfigureAwait(false);
            if (result != 0) return result;
            CurrentStep = OutputFeederUnloadFromStageStep.LiftUp;
            return 0;
        }

        private async Task<int> LiftUpAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederUp()) return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift up failed. result=" + result);
            CurrentStep = OutputFeederUnloadFromStageStep.Clamp;
            return 0;
        }

        private async Task<int> ClampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0) return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp failed. result=" + result);
            CurrentStep = OutputFeederUnloadFromStageStep.VerifyRing;
            return 0;
        }

        private async Task<int> VerifyRingAsync(CancellationToken ct)
        {
            if (!IsHardwareBypass())
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected) return Fail("OUT-FEEDER-STAGE-UNLOAD-RING", Feeder.Name, "Output feeder ring was not detected after stage unload.");
            }
            CurrentStep = OutputFeederUnloadFromStageStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private int MoveMaterialDataToFeeder()
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(ResolveOutputStageLocation());
            if (wafer != null)
                MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = MaterialLocationKind.OutputFeeder }, WaferMaterialState.WorkReady);
            Feeder.UpdateFeederMaterialState(MaterialState.Occupied);
            CurrentStep = OutputFeederUnloadFromStageStep.Complete;
            return 0;
        }
    }
}
