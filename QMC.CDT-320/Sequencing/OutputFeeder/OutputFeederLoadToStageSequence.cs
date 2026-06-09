using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederLoadToStageStep { Idle, CheckUnit, CheckReady, MoveStageLoadPosition, Unclamp, LiftDown, VerifyRingClear, MoveMaterialDataToStage, Complete, Error }

    internal sealed class OutputFeederLoadToStageSequence : OutputFeederSequenceBase<OutputFeederLoadToStageStep>
    {
        public OutputFeederLoadToStageSequence(MachineSequenceContext context) : base(context, OutputFeederSequenceKind.LoadToStage, "OutputFeederLoadToStageSequence") { }
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
                    case OutputFeederLoadToStageStep.CheckUnit: return Task.FromResult(CheckUnit(OutputFeederLoadToStageStep.CheckReady));
                    case OutputFeederLoadToStageStep.CheckReady: return Task.FromResult(CheckReady());
                    case OutputFeederLoadToStageStep.MoveStageLoadPosition: return MoveStageLoadPositionAsync(ct);
                    case OutputFeederLoadToStageStep.Unclamp: return UnclampAsync(ct);
                    case OutputFeederLoadToStageStep.LiftDown: return LiftDownAsync(ct);
                    case OutputFeederLoadToStageStep.VerifyRingClear: return VerifyRingClearAsync(ct);
                    case OutputFeederLoadToStageStep.MoveMaterialDataToStage: return Task.FromResult(MoveMaterialDataToStage());
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-FEEDER-STAGE-LOAD-EX", Name, "Load to stage step failed: " + ex.Message)); }
        }

        private int CheckReady()
        {
            if (!Feeder.CheckFeederStageReady(Options.Side, TransferMode.Load))
                return Fail("OUT-FEEDER-STAGE-LOAD-READY", Feeder.Name, "Output feeder stage load is not ready.");
            CurrentStep = OutputFeederLoadToStageStep.MoveStageLoadPosition;
            return 0;
        }

        private async Task<int> MoveStageLoadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageLoadPosition(Options.Side, Options.FineMove), "stage load", ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageLoadPosition(Options.Side), "stage load", ct).ConfigureAwait(false);
            if (result != 0) return result;
            CurrentStep = OutputFeederLoadToStageStep.Unclamp;
            return 0;
        }

        private async Task<int> UnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederUnclamped()) return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp failed. result=" + result);
            CurrentStep = OutputFeederLoadToStageStep.LiftDown;
            return 0;
        }

        private async Task<int> LiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederDown()) return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift down failed. result=" + result);
            CurrentStep = OutputFeederLoadToStageStep.VerifyRingClear;
            return 0;
        }

        private async Task<int> VerifyRingClearAsync(CancellationToken ct)
        {
            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared) return Fail("OUT-FEEDER-STAGE-RING", Feeder.Name, "Output feeder ring remained after stage load.");
            }
            CurrentStep = OutputFeederLoadToStageStep.MoveMaterialDataToStage;
            return 0;
        }

        private int MoveMaterialDataToStage()
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (wafer != null)
                MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = ResolveOutputStageLocation() }, WaferMaterialState.Working);
            Feeder.ClearFeederMaterialState();
            CurrentStep = OutputFeederLoadToStageStep.Complete;
            return 0;
        }
    }
}
