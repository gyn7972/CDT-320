using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputSequenceAutoAction
    {
        None,
        StoreNgStageToCassette,
        StoreGoodStageToCassette,
        ResumeOccupiedFeeder,
        SupplyGoodCassetteToStage,
        SupplyNgCassetteToStage,
        WaitOutputStageReceiveComplete
    }

    public class OutputSequence : UnitSequenceBase
    {
        public OutputSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.OutputUnloader, "OutputUnloader")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                int result = await ExecuteNextOutputStepAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Output auto sequence failed. result=" + result);
            }
        }

        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            int result = await ExecuteNextOutputStepAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
            if (result != 0)
                throw new InvalidOperationException("Output step sequence failed. result=" + result);
        }

        public async Task<int> ExecuteNextOutputStepAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                OutputSequenceAutoAction action = ResolveNextOutputAction();
                Context.LogPublic("[OUTPUT] next action=" + action);

                switch (action)
                {
                    case OutputSequenceAutoAction.StoreNgStageToCassette:
                        return await ExecuteCompletedStageStoreAsync(
                            ct,
                            BinSide.Ng,
                            DieGrade.Ng,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    case OutputSequenceAutoAction.StoreGoodStageToCassette:
                        return await ExecuteCompletedStageStoreAsync(
                            ct,
                            BinSide.Good,
                            DieGrade.Good,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    case OutputSequenceAutoAction.ResumeOccupiedFeeder:
                        return await ExecuteOccupiedFeederActionAsync(
                            ct,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    case OutputSequenceAutoAction.SupplyGoodCassetteToStage:
                        return await ExecuteSupplyCassetteToStageAsync(
                            ct,
                            BinSide.Good,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    case OutputSequenceAutoAction.SupplyNgCassetteToStage:
                        return await ExecuteSupplyCassetteToStageAsync(
                            ct,
                            BinSide.Ng,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    case OutputSequenceAutoAction.WaitOutputStageReceiveComplete:
                        SetOutputStageReadySignals();
                        await WaitAnyOutputReceiveCompleteAsync(ct).ConfigureAwait(false);
                        return 0;

                    default:
                        return StopAutoSequence("Output sequence could not resolve next action.");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-NEXT-EX", "OutputSequence", "Output next step exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private OutputSequenceAutoAction ResolveNextOutputAction()
        {
            if (IsStageReceiveComplete(BinSide.Ng))
                return OutputSequenceAutoAction.StoreNgStageToCassette;

            if (IsStageReceiveComplete(BinSide.Good))
                return OutputSequenceAutoAction.StoreGoodStageToCassette;

            if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null)
                return OutputSequenceAutoAction.ResumeOccupiedFeeder;

            if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) == null)
                return OutputSequenceAutoAction.SupplyGoodCassetteToStage;

            if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) == null)
                return OutputSequenceAutoAction.SupplyNgCassetteToStage;

            return OutputSequenceAutoAction.WaitOutputStageReceiveComplete;
        }

        private static bool IsStageReceiveComplete(BinSide side)
        {
            MaterialLocationKind location = side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;

            return MaterialStateService.GetWaferAtLocation(location) != null &&
                   MaterialStateService.IsOutputStageReceiveComplete(side);
        }

        private async Task<int> ExecuteCompletedStageStoreAsync(
            CancellationToken ct,
            BinSide side,
            DieGrade grade,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            ResetOutputStageReadyForStore(side);
            return await ExecuteStoreStageToCassetteAsync(
                ct,
                grade,
                bFine,
                moveTimeoutMs,
                startMode).ConfigureAwait(false);
        }

        private async Task<int> ExecuteOccupiedFeederActionAsync(
            CancellationToken ct,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (feederWafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "OutputSequence", "Output feeder data disappeared before occupied feeder action.");

            return await ExecuteOutputFeederOccupiedAsync(
                feederWafer,
                ct,
                bFine,
                moveTimeoutMs,
                startMode).ConfigureAwait(false);
        }

        private void ResetOutputStageReadyForStore(BinSide side)
        {
            if (side == BinSide.Ng)
            {
                Context.Bus.Reset("OutputNgStageReady");
                Context.Bus.Reset("OutputNgStageReceiveComplete");
                return;
            }

            Context.Bus.Reset("OutputGoodStageReady");
            Context.Bus.Reset("OutputGoodStageReceiveComplete");
        }

        private async Task WaitAnyOutputReceiveCompleteAsync(CancellationToken ct)
        {
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                Task goodTask = Context.Bus.WaitAsync("OutputGoodStageReceiveComplete", linkedCts.Token);
                Task ngTask = Context.Bus.WaitAsync("OutputNgStageReceiveComplete", linkedCts.Token);
                Task completed = await Task.WhenAny(goodTask, ngTask).ConfigureAwait(false);
                linkedCts.Cancel();
                await completed.ConfigureAwait(false);
            }
        }

        private void SetOutputStageReadySignals()
        {
            if (MaterialStateService.IsOutputStageReceiveAvailable(BinSide.Good))
                Context.Bus.Set("OutputGoodStageReady");
            else
                Context.Bus.Reset("OutputGoodStageReady");

            if (MaterialStateService.IsOutputStageReceiveAvailable(BinSide.Ng))
                Context.Bus.Set("OutputNgStageReady");
            else
                Context.Bus.Reset("OutputNgStageReady");

            if (MaterialStateService.IsOutputStageReceiveAvailable(BinSide.Good) ||
                MaterialStateService.IsOutputStageReceiveAvailable(BinSide.Ng))
            {
                Context.Bus.Set("OutputStageReady");
            }
            else
            {
                Context.Bus.Reset("OutputStageReady");
            }
        }

        public Task<int> ExecuteCassetteLoadingAsync(CancellationToken ct, TargetCassette target = TargetCassette.Good1, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            return sequence.RunLoadingAsync(ct, BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteCassetteMappingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            return sequence.RunMappingAsync(ct, BuildCassetteOptions(TargetCassette.Good1, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteCassetteUnloadingAsync(CancellationToken ct, TargetCassette target = TargetCassette.Good1, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            return sequence.RunUnloadingAsync(ct, BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteCassetteMoveToSlotAsync(CancellationToken ct, TargetCassette target, int slotIndex, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            var options = BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode);
            options.SlotIndex = slotIndex;
            return sequence.RunMoveSlotAsync(ct, options);
        }

        public Task<int> ExecuteStagePrepareLoadAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return sequence.RunPrepareLoadAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteStagePrepareUnloadAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return sequence.RunPrepareUnloadAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteStageReceiveDieAsync(
            CancellationToken ct,
            DieGrade grade,
            double tpuOffsetX = 0.0,
            double tpuOffsetY = 0.0,
            double visionOffsetX = 0.0,
            double visionOffsetY = 0.0,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            OutputStageSequenceOptions options = BuildStageOptions(
                grade == DieGrade.Ng ? BinSide.Ng : BinSide.Good,
                bFine,
                moveTimeoutMs,
                startMode);

            options.Grade = grade;
            options.TpuOffsetX = tpuOffsetX;
            options.TpuOffsetY = tpuOffsetY;
            options.VisionOffsetX = visionOffsetX;
            options.VisionOffsetY = visionOffsetY;
            return sequence.RunReceiveDieAsync(ct, options);
        }

        public Task<int> ExecuteStageInspectBinAsync(
            CancellationToken ct,
            BinSide side = BinSide.Good,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return sequence.RunInspectBinAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteStageMoveAvoidAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return sequence.RunMoveAvoidAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteStageMoveProcessAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return sequence.RunMoveProcessAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteFeederLoadFromCassetteAsync(CancellationToken ct, int slotIndex, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return sequence.RunLoadFromCassetteAsync(ct, BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteFeederLoadFromCassetteAsync(CancellationToken ct, int slotIndex, CassetteMaterialRole cassetteRole, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            BinSide side = cassetteRole == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            var options = BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode);
            options.CassetteRole = cassetteRole;
            return sequence.RunLoadFromCassetteAsync(ct, options);
        }


        public Task<int> ExecuteFeederLoadToStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return sequence.RunLoadToStageAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteFeederUnloadFromStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return sequence.RunUnloadFromStageAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteFeederUnloadToCassetteAsync(CancellationToken ct, int slotIndex, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return sequence.RunUnloadToCassetteAsync(ct, BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode));
        }

        public Task<int> ExecuteFeederUnloadToCassetteAsync(CancellationToken ct, int slotIndex, CassetteMaterialRole cassetteRole, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            BinSide side = cassetteRole == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            var options = BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode);
            options.CassetteRole = cassetteRole;
            return sequence.RunUnloadToCassetteAsync(ct, options);
        }

        public Task<int> ExecuteRecoverAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return sequence.RunRecoverAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode));
        }

        public async Task<int> ExecuteStoreStageToCassetteAsync(CancellationToken ct, DieGrade grade, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                OutputSlotPlan plan;
                string slotPlanReason;
                if (!OutputSlotPlanner.TryResolveNextStoreSlot(grade, out plan, out slotPlanReason))
                    return Fail("OUT-SLOT-UNAVAILABLE", "OutputSequence", "Output cassette same source slot is not available. grade=" + grade + ", reason=" + slotPlanReason);

                using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(plan.Side, "OutputStore", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "Output stage area resource acquire failed. side=" + plan.Side);

                    int result = await ExecuteStagePrepareUnloadAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteFeederUnloadFromStageAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteCassetteMoveToSlotAsync(ct, plan.TargetCassette, plan.SlotIndex, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteFeederUnloadToCassetteAsync(ct, plan.SlotIndex, plan.CassetteRole, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteStageMoveAvoidAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STORE-EX", "OutputSequence", "Output store stage to cassette exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteOutputFeederOccupiedAsync(WaferMaterial feederWafer, CancellationToken ct, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            try
            {
                if (feederWafer == null)
                    return Fail("OUT-FEEDER-DATA-MISSING", "OutputSequence", "Output feeder data is missing.");

                BinSide side;
                if (!TryResolveBinSide(feederWafer, out side))
                    return Fail("OUT-FEEDER-SIDE", "Material", "Output feeder bin side cannot be resolved. wafer=" + feederWafer.WaferId);

                if (IsOutputBinReceiveComplete(feederWafer))
                    return await ExecuteOutputFeederStoreToCassetteAsync(feederWafer, side, ct, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                MaterialLocationKind stageLocation = side == BinSide.Ng
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;

                WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(stageLocation);
                if (stageWafer != null)
                    return Fail("OUT-FEEDER-STAGE-OCCUPIED", "Material", "Output feeder has unfinished bin but target stage is occupied. side=" + side + ", feeder=" + feederWafer.WaferId + ", stage=" + stageWafer.WaferId);

                using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(side, "OutputFeederResumeLoad", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "Output stage area resource acquire failed. side=" + side);

                    int result = await ExecuteStagePrepareLoadAsync(ct, side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteFeederLoadToStageAsync(ct, side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;
                }

                SetOutputStageReadySignals();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-RESUME-EX", "OutputSequence", "Output feeder occupied resume exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteOutputFeederStoreToCassetteAsync(WaferMaterial feederWafer, BinSide side, CancellationToken ct, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            try
            {
                CassetteMaterialRole role;
                TargetCassette target;
                if (!TryResolveOutputCassetteTarget(feederWafer, side, out role, out target))
                    return Fail("OUT-FEEDER-CST-TARGET", "Material", "Output feeder cassette target cannot be resolved. wafer=" + feederWafer.WaferId + ", side=" + side);

                if (feederWafer.SourceSlotNumber < 0)
                    return Fail("OUT-FEEDER-CST-SLOT", "Material", "Output feeder source slot is invalid. wafer=" + feederWafer.WaferId + ", slot=" + feederWafer.SourceSlotNumber);

                int result = await ExecuteCassetteMoveToSlotAsync(ct, target, feederWafer.SourceSlotNumber, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                if (result != 0) return result;

                result = await ExecuteFeederUnloadToCassetteAsync(ct, feederWafer.SourceSlotNumber, role, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                if (result != 0) return result;

                Context.Bus.Reset(side == BinSide.Ng ? "OutputNgStageReceiveComplete" : "OutputGoodStageReceiveComplete");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-STORE-EX", "OutputSequence", "Output feeder store to cassette exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteSupplyCassetteToStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                OutputSlotPlan plan;
                if (!OutputSlotPlanner.TryResolveNextSupplySlot(side, out plan))
                    return StopAutoSequence("Output cassette has no ready slot. side=" + side);

                using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(plan.Side, "OutputSupply", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "Output stage area resource acquire failed. side=" + plan.Side);

                    int result = await ExecuteStagePrepareLoadAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteCassetteMoveToSlotAsync(ct, plan.TargetCassette, plan.SlotIndex, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteFeederLoadFromCassetteAsync(ct, plan.SlotIndex, plan.CassetteRole, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteFeederLoadToStageAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;

                    result = await ExecuteRecoverAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                    if (result != 0) return result;
                }

                SetOutputStageReadySignals();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-SUPPLY-EX", "OutputSequence", "Output supply cassette to stage exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private OutputFeederSequenceOptions BuildFeederOptions(int slotIndex, int nextSlotIndex, BinSide side, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            var options = OutputFeederSequenceOptions.Default();
            options.SlotIndex = slotIndex;
            options.NextSlotIndex = nextSlotIndex;
            options.Side = side;
            options.CassetteRole = side == BinSide.Ng ? CassetteMaterialRole.Ng1 : CassetteMaterialRole.Good1;
            options.FineMove = bFine;
            options.MoveTimeoutMs = moveTimeoutMs > 0 ? moveTimeoutMs : options.MoveTimeoutMs;
            options.RunMode = Mode;
            options.StartMode = startMode;
            return options;
        }

        private static bool TryResolveBinSide(WaferMaterial wafer, out BinSide side)
        {
            side = BinSide.Good;
            if (wafer == null)
                return false;

            if (wafer.OutputGrade == DieResult.NG)
            {
                side = BinSide.Ng;
                return true;
            }

            if (wafer.OutputGrade == DieResult.Good)
            {
                side = BinSide.Good;
                return true;
            }

            if (wafer.SourceCassetteRole == CassetteMaterialRole.Ng1 ||
                wafer.OutputCassetteRole == CassetteMaterialRole.Ng1 ||
                (wafer.CurrentLocation != null && wafer.CurrentLocation.CassetteRole == CassetteMaterialRole.Ng1))
            {
                side = BinSide.Ng;
                return true;
            }

            if (wafer.SourceCassetteRole == CassetteMaterialRole.Good1 ||
                wafer.SourceCassetteRole == CassetteMaterialRole.Good2 ||
                wafer.OutputCassetteRole == CassetteMaterialRole.Good1 ||
                wafer.OutputCassetteRole == CassetteMaterialRole.Good2 ||
                (wafer.CurrentLocation != null &&
                    (wafer.CurrentLocation.CassetteRole == CassetteMaterialRole.Good1 ||
                     wafer.CurrentLocation.CassetteRole == CassetteMaterialRole.Good2)))
            {
                side = BinSide.Good;
                return true;
            }

            return false;
        }

        private static bool IsOutputBinReceiveComplete(WaferMaterial wafer)
        {
            if (wafer == null)
                return false;

            if (WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Finish)
                return true;

            int total = wafer.OutputReceiveTotalCount;
            int count = wafer.DieIds != null ? wafer.DieIds.Count : 0;
            return total > 0 && count >= total;
        }

        private static bool TryResolveOutputCassetteTarget(WaferMaterial wafer, BinSide side, out CassetteMaterialRole role, out TargetCassette target)
        {
            role = side == BinSide.Ng ? CassetteMaterialRole.Ng1 : CassetteMaterialRole.Good1;
            target = side == BinSide.Ng ? TargetCassette.Ng : TargetCassette.Good1;
            if (wafer == null)
                return false;

            CassetteMaterialRole candidate = wafer.SourceCassetteRole;
            if (candidate != CassetteMaterialRole.Good1 &&
                candidate != CassetteMaterialRole.Good2 &&
                candidate != CassetteMaterialRole.Ng1)
            {
                candidate = wafer.OutputCassetteRole;
            }

            switch (candidate)
            {
                case CassetteMaterialRole.Good1:
                    role = CassetteMaterialRole.Good1;
                    target = TargetCassette.Good1;
                    return side == BinSide.Good;
                case CassetteMaterialRole.Good2:
                    role = CassetteMaterialRole.Good2;
                    target = TargetCassette.Good2;
                    return side == BinSide.Good;
                case CassetteMaterialRole.Ng1:
                    role = CassetteMaterialRole.Ng1;
                    target = TargetCassette.Ng;
                    return side == BinSide.Ng;
                default:
                    return false;
            }
        }

        private Task<SequenceResourceLease> AcquireOutputStageAreaAsync(BinSide side, string holder, CancellationToken ct)
        {
            SequenceResourceKind resource = side == BinSide.Ng
                ? SequenceResourceKind.OutputNgStageArea
                : SequenceResourceKind.OutputGoodStageArea;

            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;
            return Context.Resources.AcquireAsync(resource, safeHolder + ":" + side, 30000, ct);
        }

        private OutputCassetteSequenceOptions BuildCassetteOptions(TargetCassette target, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            var options = OutputCassetteSequenceOptions.Default();
            options.TargetCassette = target;
            options.FineMove = bFine;
            options.MoveTimeoutMs = moveTimeoutMs > 0 ? moveTimeoutMs : options.MoveTimeoutMs;
            options.GoodLevelCount = Context != null && Context.Machine != null && Context.Machine.OutputCassetteUnit != null && Context.Machine.OutputCassetteUnit.Config != null
                ? Math.Max(1, Math.Min(2, Context.Machine.OutputCassetteUnit.Config.SelectedCassetteLevel))
                : 2;
            options.RunMode = Mode;
            options.StartMode = startMode;
            return options;
        }

        private OutputStageSequenceOptions BuildStageOptions(BinSide side, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            var options = OutputStageSequenceOptions.Default();
            options.Side = side;
            options.FineMove = bFine;
            options.MoveTimeoutMs = moveTimeoutMs > 0 ? moveTimeoutMs : options.MoveTimeoutMs;
            options.RunMode = Mode;
            options.StartMode = startMode;
            options.Grade = side == BinSide.Ng ? DieGrade.Ng : DieGrade.Good;
            return options;
        }

        private int Fail(string alarmCode, string source, string message)
        {
            try
            {
                SequenceFailureStore.Record("OutputSequence", Kind.ToString(), "", alarmCode, source, message);
                Log.Write("Main", "SYSTEM", source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                Context.LogPublic("[UNIT-OUTPUT] FAIL " + alarmCode + " - " + message);
            }
            catch
            {
            }

            return -1;
        }

        private int StopAutoSequence(string reason)
        {
            try
            {
                Log.Write("Main", "SYSTEM", "OutputSequence", "Output sequence stopped: " + reason + " - Stopped");
                Context.LogPublic("[UNIT-OUTPUT] STOP " + reason);
            }
            catch
            {
            }
            finally
            {
            }

            throw new SequenceStopException(reason);
        }
    }
}
