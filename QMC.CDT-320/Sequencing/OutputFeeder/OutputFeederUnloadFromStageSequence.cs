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
        EnsureOutputVisionAvoid,
        EnsurePickerAvoidPosition,
        EnsureStageMutualInterlock,
        MoveOutputStageUnloadPosition,
        EnsureOutputStageGuideUp,
        EnsureOutputStageUnclamp,
        EnsureOutputStageClampLiftDown,
        VerifyOutputStageUnloadReady,
        VerifyFeederReadyAtAvoid,
        PrepareFeederUnclamp,
        PrepareFeederLiftUp,
        MoveFeederStageUnloadAvoidPosition,
        PrepareFeederLiftDown,
        MoveFeederStageUnloadPosition,
        ClampFeederBin,
        UnclampOutputStageBin,
        LowerOutputStageClamp,
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

                    case OutputFeederUnloadFromStageStep.EnsureOutputVisionAvoid:
                        return EnsureOutputVisionAvoidAsync(ct);

                    case OutputFeederUnloadFromStageStep.EnsurePickerAvoidPosition:
                        return EnsurePickerAvoidPositionAsync(ct);

                    case OutputFeederUnloadFromStageStep.EnsureStageMutualInterlock:
                        return EnsureStageMutualInterlockAsync(ct);

                    case OutputFeederUnloadFromStageStep.MoveOutputStageUnloadPosition:
                        return MoveOutputStageUnloadPositionAsync(ct);

                    case OutputFeederUnloadFromStageStep.EnsureOutputStageGuideUp:
                        return EnsureOutputStageGuideUpAsync(ct);

                    case OutputFeederUnloadFromStageStep.EnsureOutputStageUnclamp:
                        return EnsureOutputStageUnclampAsync(ct);

                    case OutputFeederUnloadFromStageStep.EnsureOutputStageClampLiftDown:
                        return EnsureOutputStageClampLiftDownAsync(ct);

                    case OutputFeederUnloadFromStageStep.VerifyOutputStageUnloadReady:
                        return Task.FromResult(VerifyOutputStageUnloadReady());

                    case OutputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid:
                        return Task.FromResult(VerifyFeederReadyAtAvoid());

                    case OutputFeederUnloadFromStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    case OutputFeederUnloadFromStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);

                    case OutputFeederUnloadFromStageStep.MoveFeederStageUnloadAvoidPosition:
                        return MoveFeederStageUnloadAvoidPositionAsync(ct);

                    case OutputFeederUnloadFromStageStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);

                    case OutputFeederUnloadFromStageStep.MoveFeederStageUnloadPosition:
                        return MoveFeederStageUnloadPositionAsync(ct);

                    case OutputFeederUnloadFromStageStep.ClampFeederBin:
                        return ClampFeederBinAsync(ct);

                    case OutputFeederUnloadFromStageStep.UnclampOutputStageBin:
                        return UnclampOutputStageBinAsync(ct);

                    case OutputFeederUnloadFromStageStep.LowerOutputStageClamp:
                        return LowerOutputStageClampAsync(ct);

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
            string readyReason;
            if (!Feeder.CheckFeederStageReady(Options.Side, TransferMode.Unload, out readyReason))
                return Fail("OUT-FEEDER-STAGE-UNLOAD-READY", Feeder.Name, "Output feeder stage unload is not ready. " + readyReason);

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
            {
                WaferMaterial feederWafer = ResolveFeederWafer();
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material",
                    "Output feeder data became occupied before stage unload. side=" + Options.Side +
                    ", waferId=" + feederWafer.WaferId + ", state=" + feederWafer.State +
                    ", loc=" + feederWafer.CurrentLocation);
            }

            if (!IsHardwareBypass() && !Feeder.IsFeederEmpty())
                return Fail("OUT-FEEDER-SENSOR-OCCUPIED", Feeder.Name,
                    "Output feeder sensor must be empty before stage unload. sensorOccupied=" + Feeder.IsFeederOccupied() +
                    ", sensorEmpty=" + Feeder.IsFeederEmpty());

            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputVisionAvoid;
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
                    return Fail("OUT-STAGE-VISION-AVOID", Stage.Name, "OutputVisionX avoid move failed before stage unload. side=" + Options.Side + ", result=" + result);
            }

            if (!Stage.IsVisionXInAvoidPosition())
                return Fail("OUT-STAGE-VISION-AVOID", Stage.Name, "OutputVisionX is not in avoid position before stage unload. side=" + Options.Side);

            CurrentStep = OutputFeederUnloadFromStageStep.EnsurePickerAvoidPosition;
            return 0;
        }

        private async Task<int> EnsurePickerAvoidPositionAsync(CancellationToken ct)
        {
            if (FrontPicker != null && !FrontPicker.IsFrontPickerInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(FrontPicker.MoveToFrontPickerAvoidPosition(Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-FEEDER-FRONT-PICKER-AVOID", FrontPicker.Name, "FrontPicker avoid move command failed before stage unload. result=" + result);

                bool arrived = await WaitUntilAsync(() => FrontPicker.IsFrontPickerInAvoidPosition(), ResolveTimeout(), ct).ConfigureAwait(false);
                if (!arrived)
                    return Fail("OUT-FEEDER-FRONT-PICKER-AVOID", FrontPicker.Name, "FrontPicker avoid position timeout before stage unload.");
            }

            if (RearPicker != null && !RearPicker.IsRearPickerInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(RearPicker.MoveToRearPickerAvoidPosition(Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-FEEDER-REAR-PICKER-AVOID", RearPicker.Name, "RearPicker avoid move command failed before stage unload. result=" + result);

                bool arrived = await WaitUntilAsync(() => RearPicker.IsRearPickerInAvoidPosition(), ResolveTimeout(), ct).ConfigureAwait(false);
                if (!arrived)
                    return Fail("OUT-FEEDER-REAR-PICKER-AVOID", RearPicker.Name, "RearPicker avoid position timeout before stage unload.");
            }

            CurrentStep = OutputFeederUnloadFromStageStep.EnsureStageMutualInterlock;
            return 0;
        }

        private async Task<int> EnsureStageMutualInterlockAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureStageMutualInterlockForLoadAsync(Options.Side, ResolveTimeout(), Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-UNLOAD-INTERLOCK", Stage.Name, "Output stage mutual interlock failed before stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name, "NG stage clamp lift must be up before stage unload movement. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsGoodStageZInAvoidOrProcessPosition())
                return Fail("OUT-STAGE-GOOD-Z-SAFE", Stage.Name, "Good stage Z must be avoid or process before stage unload movement. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (Options.Side != BinSide.Ng && !Stage.IsNgStageInAvoidPosition())
                return Fail("OUT-STAGE-NG-AVOID", Stage.Name, "NG stage must be avoid before GOOD stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.MoveOutputStageUnloadPosition;
            return 0;
        }

        private async Task<int> MoveOutputStageUnloadPositionAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.MoveToStageUnloadPositionAndVerifyAsync(Options.Side, ResolveTimeout(), Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-UNLOAD-POS", Stage.Name, "Output stage unload position move failed. side=" + Options.Side + ", result=" + result);

            if (!Stage.IsStageInUnloadPosition(Options.Side))
                return Fail("OUT-STAGE-UNLOAD-POS", Stage.Name, "Output stage is not in unload position after move. side=" + Options.Side);

            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageGuideUp;
            return 0;
        }

        private async Task<int> EnsureOutputStageGuideUpAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideUpAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-GUIDE-UP", Stage.Name, "Output stage bin guide up failed before stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUp(Options.Side))
                return Fail("OUT-STAGE-GUIDE-UP", Stage.Name, "Output stage bin guide is not up before stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageUnclamp;
            return 0;
        }

        private async Task<int> EnsureOutputStageUnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide unclamp failed before stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide is not unclamped before stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageClampLiftDown;
            return 0;
        }

        private async Task<int> EnsureOutputStageClampLiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift down failed before stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift is not down before stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.VerifyOutputStageUnloadReady;
            return 0;
        }

        private int VerifyOutputStageUnloadReady()
        {
            if (IsHardwareBypass() && ResolveStageWafer() != null)
            {
                CurrentStep = OutputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid;
                return 0;
            }

            if (!Stage.IsBinGuideUp(Options.Side))
                return Fail("OUT-STAGE-UNLOAD-GUIDE-UP", Stage.Name,
                    "Output stage bin guide must be up before feeder starts stage unload. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-UNLOAD-CLAMP-DOWN", Stage.Name,
                    "Output stage bin clamp lift must be down before feeder starts stage unload. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNLOAD-UNCLAMP", Stage.Name,
                    "Output stage bin guide must be unclamped before feeder starts stage unload. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid;
            return 0;
        }

        private int VerifyFeederReadyAtAvoid()
        {
            if (!Feeder.IsBinFeederInAvoidPosition())
                return Fail("OUT-FEEDER-AVOID-CHECK", Feeder.Name,
                    "Output feeder must already be at avoid position before stage unload. side=" + Options.Side + ", " +
                    Feeder.DescribeBinFeederYMoveDoneState());

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "Output feeder must already be down before stage unload. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());

            CurrentStep = Feeder.IsFeederUnclamped()
                ? OutputFeederUnloadFromStageStep.PrepareFeederLiftUp
                : OutputFeederUnloadFromStageStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name,
                    "Output feeder unclamp command failed before stage unload. result=" + result + ", " +
                    Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name,
                    "Output feeder unclamp failed before stage unload. result=" + result + ", " +
                    Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadFromStageStep.PrepareFeederLiftUp;
            return 0;
        }

        private async Task<int> PrepareFeederLiftUpAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift up before stage unload avoid failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUp())
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift is not up before stage unload avoid. " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadFromStageStep.MoveFeederStageUnloadAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageUnloadAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageUnloadAvoidPosition(Options.Side, Options.FineMove), "stage unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageUnloadAvoidPosition(Options.Side), "stage unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadFromStageStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift down before stage unload failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift is not down before stage unload. " + Feeder.DescribeFeederCylinderState());

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

            CurrentStep = OutputFeederUnloadFromStageStep.ClampFeederBin;
            return 0;
        }

        private async Task<int> ClampFeederBinAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(true, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp final check failed. side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadFromStageStep.UnclampOutputStageBin;
            return 0;
        }

        private async Task<int> UnclampOutputStageBinAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide unclamp failed after feeder clamped bin. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide unclamp final check failed after feeder clamped bin. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.LowerOutputStageClamp;
            return 0;
        }

        private async Task<int> LowerOutputStageClampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift down failed after stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift down final check failed after stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

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
