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
        EnsureOutputVisionAvoid,
        EnsurePickerAvoidPosition,
        EnsureStageMutualInterlock,
        MoveOutputStageLoadPosition,
        EnsureOutputStageGuideUp,
        EnsureOutputStageClampLiftDown,
        EnsureOutputStageUnclamp,
        VerifyOutputStageReceiveReady,
        VerifyFeederHoldingBin,
        MoveFeederStageLoadPosition,
        UnclampFeederBin,
        MoveFeederStageLoadAvoidPosition,
        LiftOutputStageClamp,
        ClampOutputStageBin,
        MoveMaterialDataToStage,
        PrepareFeederLiftUp,
        MoveFeederAvoidPosition,
        PrepareFeederLiftDownAfterAvoid,
        MoveNgStageAvoidAfterLoad,
        LowerNgStageGuideAfterAvoid,
        VerifyBinTransferredToStage,
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

                    case OutputFeederLoadToStageStep.EnsureOutputVisionAvoid:
                        return EnsureOutputVisionAvoidAsync(ct);

                    case OutputFeederLoadToStageStep.EnsurePickerAvoidPosition:
                        return EnsurePickerAvoidPositionAsync(ct);

                    case OutputFeederLoadToStageStep.EnsureStageMutualInterlock:
                        return EnsureStageMutualInterlockAsync(ct);

                    case OutputFeederLoadToStageStep.MoveOutputStageLoadPosition:
                        return MoveOutputStageLoadPositionAsync(ct);

                    case OutputFeederLoadToStageStep.EnsureOutputStageGuideUp:
                        return EnsureOutputStageGuideUpAsync(ct);

                    case OutputFeederLoadToStageStep.EnsureOutputStageClampLiftDown:
                        return EnsureOutputStageClampLiftDownAsync(ct);

                    case OutputFeederLoadToStageStep.EnsureOutputStageUnclamp:
                        return EnsureOutputStageUnclampAsync(ct);

                    case OutputFeederLoadToStageStep.VerifyOutputStageReceiveReady:
                        return Task.FromResult(VerifyOutputStageReceiveReady());

                    case OutputFeederLoadToStageStep.VerifyFeederHoldingBin:
                        return Task.FromResult(VerifyFeederHoldingBin());

                    case OutputFeederLoadToStageStep.MoveFeederStageLoadPosition:
                        return MoveFeederStageLoadPositionAsync(ct);

                    case OutputFeederLoadToStageStep.UnclampFeederBin:
                        return UnclampFeederBinAsync(ct);

                    case OutputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition:
                        return MoveFeederStageLoadAvoidPositionAsync(ct);

                    case OutputFeederLoadToStageStep.LiftOutputStageClamp:
                        return LiftOutputStageClampAsync(ct);

                    case OutputFeederLoadToStageStep.ClampOutputStageBin:
                        return ClampOutputStageBinAsync(ct);

                    case OutputFeederLoadToStageStep.MoveMaterialDataToStage:
                        return Task.FromResult(MoveMaterialDataToStage());

                    case OutputFeederLoadToStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);

                    case OutputFeederLoadToStageStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);

                    case OutputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid:
                        return PrepareFeederLiftDownAfterAvoidAsync(ct);

                    case OutputFeederLoadToStageStep.MoveNgStageAvoidAfterLoad:
                        return MoveNgStageAvoidAfterLoadAsync(ct);

                    case OutputFeederLoadToStageStep.LowerNgStageGuideAfterAvoid:
                        return LowerNgStageGuideAfterAvoidAsync(ct);

                    case OutputFeederLoadToStageStep.VerifyBinTransferredToStage:
                        return VerifyBinTransferredToStageAsync(ct);

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
            string readyReason;
            if (!Feeder.CheckFeederStageReady(Options.Side, TransferMode.Load, out readyReason))
                return Fail("OUT-FEEDER-STAGE-LOAD-READY", Feeder.Name, "Output feeder stage load is not ready. " + readyReason);

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

            if (Stage == null)
                return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available. side=" + Options.Side);

            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputVisionAvoid;
            return 0;
        }

        private async Task<int> EnsureOutputVisionAvoidAsync(CancellationToken ct)
        {
            if (Stage == null)
                return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available. side=" + Options.Side);

            if (!Stage.IsVisionXInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(Stage.MoveVisionXToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-VISION-AVOID", Stage.Name, "OutputVisionX avoid move failed. side=" + Options.Side + ", result=" + result);
            }

            if (!Stage.IsVisionXInAvoidPosition())
                return Fail("OUT-STAGE-VISION-AVOID", Stage.Name, "OutputVisionX is not in avoid position before feeder to stage load. side=" + Options.Side);

            CurrentStep = OutputFeederLoadToStageStep.EnsurePickerAvoidPosition;
            return 0;
        }

        private async Task<int> EnsurePickerAvoidPositionAsync(CancellationToken ct)
        {
            if (FrontPicker != null && !FrontPicker.IsFrontPickerInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(FrontPicker.MoveToFrontPickerAvoidPosition(Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-FEEDER-FRONT-PICKER-AVOID", FrontPicker.Name, "FrontPicker avoid move command failed. result=" + result + ", pickerX=" + FrontPicker.PickerX.ActualPosition);

                bool arrived = await WaitUntilAsync(() => FrontPicker.IsFrontPickerInAvoidPosition(), ResolveTimeout(), ct).ConfigureAwait(false);
                if (!arrived)
                    return Fail("OUT-FEEDER-FRONT-PICKER-AVOID", FrontPicker.Name, "FrontPicker avoid position timeout before feeder to stage load.");
            }

            if (RearPicker != null && !RearPicker.IsRearPickerInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(RearPicker.MoveToRearPickerAvoidPosition(Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-FEEDER-REAR-PICKER-AVOID", RearPicker.Name, "RearPicker avoid move command failed. result=" + result + ", pickerX=" + RearPicker.PickerX.ActualPosition);

                bool arrived = await WaitUntilAsync(() => RearPicker.IsRearPickerInAvoidPosition(), ResolveTimeout(), ct).ConfigureAwait(false);
                if (!arrived)
                    return Fail("OUT-FEEDER-REAR-PICKER-AVOID", RearPicker.Name, "RearPicker avoid position timeout before feeder to stage load.");
            }

            CurrentStep = OutputFeederLoadToStageStep.EnsureStageMutualInterlock;
            return 0;
        }

        private async Task<int> EnsureStageMutualInterlockAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureStageMutualInterlockForLoadAsync(Options.Side, ResolveTimeout(), Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-LOAD-INTERLOCK", Stage.Name, "Output stage mutual interlock failed before feeder to stage load. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name, "NG stage clamp lift must be up before stage load movement. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsGoodStageZInAvoidOrProcessPosition())
                return Fail("OUT-STAGE-GOOD-Z-SAFE", Stage.Name, "Good stage Z must be avoid or process before stage load movement. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (Options.Side != BinSide.Ng && !Stage.IsNgStageInAvoidPosition())
                return Fail("OUT-STAGE-NG-AVOID", Stage.Name, "NG stage must be avoid before GOOD stage receives bin. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.MoveOutputStageLoadPosition;
            return 0;
        }

        private async Task<int> MoveOutputStageLoadPositionAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.MoveToStageLoadPositionAndVerifyAsync(Options.Side, ResolveTimeout(), Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-LOAD-POS", Stage.Name, "Output stage load position move failed. side=" + Options.Side + ", result=" + result);

            if (!Stage.IsStageInLoadPosition(Options.Side))
                return Fail("OUT-STAGE-LOAD-POS", Stage.Name, "Output stage is not in load position after move. side=" + Options.Side);

            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputStageGuideUp;
            return 0;
        }

        private async Task<int> EnsureOutputStageGuideUpAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideUpAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-GUIDE-UP", Stage.Name, "Output stage bin guide up failed. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUp(Options.Side))
                return Fail("OUT-STAGE-GUIDE-UP", Stage.Name, "Output stage bin guide is not up. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputStageClampLiftDown;
            return 0;
        }

        private async Task<int> EnsureOutputStageClampLiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift down failed. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift is not down. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputStageUnclamp;
            return 0;
        }

        private async Task<int> EnsureOutputStageUnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide unclamp failed. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide is not unclamped. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.VerifyOutputStageReceiveReady;
            return 0;
        }

        private int VerifyOutputStageReceiveReady()
        {
            if (!Stage.IsBinGuideUp(Options.Side))
                return Fail("OUT-STAGE-RECEIVE-GUIDE-UP", Stage.Name,
                    "Output stage must have bin guide up before receiving bin. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-RECEIVE-CLAMP-DOWN", Stage.Name,
                    "Output stage clamp lift must be down before receiving bin. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-RECEIVE-UNCLAMP", Stage.Name,
                    "Output stage must be unclamped before receiving bin. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.VerifyFeederHoldingBin;
            return 0;
        }

        private int VerifyFeederHoldingBin()
        {
            if (Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-CLAMP-CHECK", Feeder.Name,
                    "Output feeder must already be clamped before feeder to stage load. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "Output feeder must already be down before feeder to stage load. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());

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

            CurrentStep = OutputFeederLoadToStageStep.UnclampFeederBin;
            return 0;
        }

        private async Task<int> UnclampFeederBinAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp command failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageLoadAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageLoadAvoidPosition(Options.Side, Options.FineMove), "stage load avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageLoadAvoidPosition(Options.Side), "stage load avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadToStageStep.LiftOutputStageClamp;
            return 0;
        }

        private async Task<int> LiftOutputStageClampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideClampLiftUpAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-UP", Stage.Name, "Output stage bin clamp lift up failed. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftUp(Options.Side))
                return Fail("OUT-STAGE-CLAMP-UP", Stage.Name, "Output stage bin clamp lift is not up after feeder stage load avoid. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.ClampOutputStageBin;
            return 0;
        }

        private async Task<int> ClampOutputStageBinAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideClampedAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP", Stage.Name, "Output stage bin clamp failed after feeder stage load avoid. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClamped(Options.Side))
                return Fail("OUT-STAGE-CLAMP", Stage.Name, "Output stage bin clamp final check failed after feeder stage load avoid. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.MoveMaterialDataToStage;
            return 0;
        }

        private async Task<int> PrepareFeederLiftUpAsync(CancellationToken ct)
        {
            if (Feeder.IsFeederUp())
            {
                CurrentStep = OutputFeederLoadToStageStep.MoveFeederAvoidPosition;
                return 0;
            }

            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift up after stage load avoid failed. side=" + Options.Side + ", result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUp())
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift is not up after stage load avoid. side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederLoadToStageStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "stage load feeder avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "stage load feeder avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAfterAvoidAsync(CancellationToken ct)
        {
            if (Feeder.IsFeederDown())
            {
                CurrentStep = Options.Side == BinSide.Ng
                    ? OutputFeederLoadToStageStep.MoveNgStageAvoidAfterLoad
                    : OutputFeederLoadToStageStep.VerifyBinTransferredToStage;
                return 0;
            }

            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift down after feeder avoid failed. side=" + Options.Side + ", result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift is not down after feeder avoid. side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = Options.Side == BinSide.Ng
                ? OutputFeederLoadToStageStep.MoveNgStageAvoidAfterLoad
                : OutputFeederLoadToStageStep.VerifyBinTransferredToStage;
            return 0;
        }

        private async Task<int> MoveNgStageAvoidAfterLoadAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-NG-AVOID-AFTER-LOAD", Stage.Name, "NG stage avoid move failed after bin load. result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsNgStageInAvoidPosition())
                return Fail("OUT-STAGE-NG-AVOID-AFTER-LOAD", Stage.Name, "NG stage is not at avoid after bin load. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.LowerNgStageGuideAfterAvoid;
            return 0;
        }

        private async Task<int> LowerNgStageGuideAfterAvoidAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideDownAsync(BinSide.Ng, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-NG-GUIDE-DOWN", Stage.Name, "NG stage guide down failed after avoid. result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideDown(BinSide.Ng))
                return Fail("OUT-STAGE-NG-GUIDE-DOWN", Stage.Name, "NG stage guide is not down after avoid. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.VerifyBinTransferredToStage;
            return 0;
        }

        private async Task<int> VerifyBinTransferredToStageAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("OUT-STAGE-DATA-MISSING", "Material", "Output stage data was not created before transfer verification. side=" + Options.Side);

            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("OUT-FEEDER-STAGE-RING", Feeder.Name, "Output feeder ring remained after stage load. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederLoadToStageStep.UpdateFeederData;
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
            MaterialStateService.InitializeOutputStageReceivePlan(Options.Side);
            Feeder.ClearFeederMaterialState();
            CurrentStep = OutputFeederLoadToStageStep.PrepareFeederLiftUp;
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
