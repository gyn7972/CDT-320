using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    public class OutputSequence : UnitSequenceBase
    {
        public OutputSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.OutputUnloader, "OutputUnloader")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            int result = await ExecuteNextOutputStepAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
            if (result != 0)
                throw new InvalidOperationException("Output auto sequence failed. result=" + result);
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

                if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) != null)
                    return await ExecuteStoreStageToCassetteAsync(ct, DieGrade.Ng, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) != null)
                    return await ExecuteStoreStageToCassetteAsync(ct, DieGrade.Good, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null)
                    return await ExecuteRecoverAsync(ct, BinSide.Good, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                return await ExecuteSupplyCassetteToStageAsync(ct, BinSide.Good, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
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

        public async Task<int> ExecuteSupplyCassetteToStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                OutputSlotPlan plan;
                if (!OutputSlotPlanner.TryResolveNextSupplySlot(side, out plan))
                    return Fail("OUT-SUPPLY-NO-SLOT", "OutputSequence", "Output cassette has no ready slot. side=" + side);

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

                return 0;
            }
            catch (OperationCanceledException)
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
    }
}
