using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal abstract class OutputCassetteSequenceBase<TStep> where TStep : struct
    {
        private const string SequenceNamePrefix = "OutputCassetteSequence";

        protected OutputCassetteSequenceBase(
            MachineSequenceContext context,
            OutputCassetteSequenceKind kind,
            string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected OutputCassetteSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected OutputCassetteSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected OutputCassetteUnit Cassette
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputCassetteUnit : null; }
        }

        protected OutputFeederUnit Feeder
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputFeederUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, OutputCassetteSequenceOptions options)
        {
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.OutputSeq, Name, () => CurrentStep.ToString()))
            try
            {
                Options = options ?? OutputCassetteSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[OUTPUT-CASSETTE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    TStep executingStep = CurrentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                }

                Context.LogPublic("[OUTPUT-CASSETTE] " + Options.RunMode + " " + Kind + " complete");
                WriteLog("RunAsync", "Output cassette " + Kind + " sequence completed. - Ok");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Output cassette " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-EXCEPTION", Name, "Output cassette " + Kind + " sequence exception at step=" + CurrentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        protected int CheckLot(TStep nextStep)
        {
            try
            {
                if (Options.RequireActiveLot && LotStorage.ActiveLot == null)
                    return Fail("OUT-CST-NO-LOT", Name, "Active lot is required for output cassette mapping.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-LOT-EX", Name, "Lot check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteDetected(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("OUT-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");

                int size = ResolveCassetteSize(cassette);
                bool goodDetected = cassette.IsBinCassetteExist(TargetCassette.Good1, size);
                bool ngDetected = cassette.IsBinCassetteExist(TargetCassette.Ng, size);
                if (!IsHardwareBypassed() && (!goodDetected || !ngDetected))
                    return Fail("OUT-CST-MISSING", cassette.Name,
                        "Output good/ng cassette is not detected. size=" + size +
                        ", goodDetected=" + goodDetected + ", ngDetected=" + ngDetected +
                        ", good1Sensor=" + cassette.IsBinCassetteExist(TargetCassette.Good1, size) +
                        ", good2Sensor=" + cassette.IsBinCassetteExist(TargetCassette.Good2, size) +
                        ", ngSensor=" + cassette.IsBinCassetteExist(TargetCassette.Ng, size));
                if (IsHardwareBypassed() && (!goodDetected || !ngDetected))
                    Context.LogPublic("[OUTPUT-CASSETTE] Hardware bypass: cassette detect sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-DETECT-EX", Name, "Cassette detect check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteMaterial(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("OUT-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");
                if (cassette.Config == null || cassette.Config.SlotCount <= 0)
                    return Fail("OUT-CST-MATERIAL-SLOT", cassette.Name, "Output cassette slot config is invalid.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-MATERIAL-EX", Name, "Output cassette material check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteSize(TStep nextStep, bool allowMismatch)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("OUT-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");

                int size = ResolveCassetteSize(cassette);
                bool goodMatched = cassette.IsBinCassetteExist(TargetCassette.Good1, size);
                bool ngMatched = cassette.IsBinCassetteExist(TargetCassette.Ng, size);
                bool matched = goodMatched && ngMatched;
                if (!IsHardwareBypassed() && !matched)
                {
                    if (!allowMismatch)
                        return Fail("OUT-CST-SIZE", cassette.Name,
                            "Output cassette size does not match recipe/config. size=" + size +
                            ", goodMatched=" + goodMatched + ", ngMatched=" + ngMatched +
                            ", allowMismatch=" + allowMismatch);

                    Context.LogPublic("[OUTPUT-CASSETTE] Unloading continues although cassette size does not match recipe/config.");
                }

                if (IsHardwareBypassed() && !matched)
                    Context.LogPublic("[OUTPUT-CASSETTE] Hardware bypass: cassette size sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-SIZE-EX", Name, "Output cassette size check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckMappingStartCondition(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("OUT-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");
                string readyReason;
                if (!cassette.CheckBinLifterZMoveReady(out readyReason))
                    return Fail("OUT-CST-MAP-MOVE-READY", cassette.Name, "Output cassette is not ready for mapping move. " + readyReason);

                if (!IsHardwareBypassed() && cassette.IsBinProtrusionDetected())
                    return Fail("OUT-CST-MAP-PROTRUSION", cassette.Name, "Output cassette product/protrusion sensor must be OFF before mapping.");

                if (!cassette.ValidateBinLifterZTeachingComplete(out readyReason))
                    return Fail("OUT-CST-MAP-TEACHING", cassette.Name, "Output cassette teaching data is not complete. " + readyReason);

                if (!IsHardwareBypassed() && !cassette.CheckBinCassetteMappingReady(TargetCassette.Good1, out readyReason))
                    return Fail("OUT-CST-MAP-GOOD-READY", cassette.Name, "Good cassette is not ready for mapping. " + readyReason);
                if (!IsHardwareBypassed() && !cassette.CheckBinCassetteMappingReady(TargetCassette.Ng, out readyReason))
                    return Fail("OUT-CST-MAP-NG-READY", cassette.Name, "NG cassette is not ready for mapping. " + readyReason);
                if (IsHardwareBypassed())
                    Context.LogPublic("[OUTPUT-CASSETTE] Hardware bypass: cassette mapping sensor checks skipped, teaching/move readiness validated.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-MAP-READY-EX", Name, "Mapping start condition check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckFeederPosition(TStep nextStep)
        {
            try
            {
                var feeder = Feeder;
                bool ready = feeder != null && feeder.IsBinFeederYInAvoidPosition();
                if (!IsHardwareBypassed() && !ready)
                    return Fail("OUT-CST-FEEDER-POS", feeder != null ? feeder.Name : "OutputFeeder",
                        "Output feeder must be in avoid position before output cassette mapping/move. feederNull=" + (feeder == null) +
                        (feeder != null
                            ? ", avoid=" + feeder.IsBinFeederYInAvoidPosition() +
                              ", feederY=" + feeder.DescribeBinFeederYMoveDoneState()
                            : ""));
                if (IsHardwareBypassed() && !ready)
                    Context.LogPublic("[OUTPUT-CASSETTE] Hardware bypass: feeder position sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-FEEDER-EX", Name, "Feeder position check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveLoadingPositionAsync(CancellationToken ct)
        {
            try
            {
                double target = ResolveLoadingPosition();
                int result = await MoveLifterZAndVerifyAsync(target, "loading", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = CompleteStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-LOAD-EX", Name, "Move loading position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveUnloadingPositionAsync(CancellationToken ct)
        {
            try
            {
                double target = ResolveUnloadingPosition();
                int result = await MoveLifterZAndVerifyAsync(target, "unloading", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = CompleteStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-UNLOAD-EX", Name, "Move unloading position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveMappingStartPositionAsync(TStep nextStep, CancellationToken ct)
        {
            double target = Cassette.Recipe.MappingStartPosition;
            int result = await MoveLifterZAndVerifyAsync(target, "mapping start", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = nextStep;
            return 0;
        }

        protected async Task<int> MoveMappingEndPositionAsync(TStep nextStep, CancellationToken ct)
        {
            double target = Cassette.Recipe.MappingEndPosition;
            int result = await MoveLifterZAndVerifyAsync(target, "mapping end", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = nextStep;
            return 0;
        }

        protected async Task<int> ScanSlotsAsync(TStep nextStep, CancellationToken ct)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("OUT-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");

                bool ok = await AwaitStepWithCancellationAsync(cassette.ScanAllCassettesFromCurrentStartAsync(ct), ct).ConfigureAwait(false);
                if (!ok)
                    return Fail("OUT-CST-SCAN", cassette.Name, "Output cassette scan failed.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-SCAN-EX", Name, "Scan slots exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int BuildBinInfo(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                int result = RegisterMappingResult(cassette);
                if (result != 0)
                    return Fail("OUT-CST-BUILD-BIN", cassette != null ? cassette.Name : "OutputCassette", "Output cassette material mapping registration failed. result=" + result);

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-BUILD-BIN-EX", Name, "Build bin information exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveFirstEmptySlotAsync(CancellationToken ct)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("OUT-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");

                TargetCassette target = ResolveMappingReturnCassette();
                int slotCount = cassette.Config != null ? cassette.Config.SlotCount : 0;
                if (slotCount > 0)
                {
                    double targetPosition = cassette.CalculateBinCassetteSlotTargetPosition(target, 0);
                    int result = await MoveLifterZAndVerifyAsync(targetPosition, target + " slot 1", ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                CurrentStep = CompleteStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-FIRST-SLOT-EX", Name, "Move first output slot exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveConfiguredSlotAsync(CancellationToken ct)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("OUT-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");
                if (cassette.Config == null || cassette.Config.SlotCount <= 0)
                    return Fail("OUT-CST-SLOT-CONFIG", cassette.Name, "Output cassette slot config is invalid.");
                if (Options.SlotIndex < 0 || Options.SlotIndex >= cassette.Config.SlotCount)
                    return Fail("OUT-CST-SLOT-INDEX", cassette.Name, "Output cassette slot index is out of range. slot=" + Options.SlotIndex);

                double targetPosition = cassette.CalculateBinCassetteSlotTargetPosition(Options.TargetCassette, Options.SlotIndex);
                int result = await MoveLifterZAndVerifyAsync(targetPosition, Options.TargetCassette + " slot " + (Options.SlotIndex + 1), ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = CompleteStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-SLOT-MOVE-EX", Name, "Move output cassette slot exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int FailUnsupportedStep()
        {
            return Fail("OUT-CST-STEP", Name, "Unsupported output cassette step: " + CurrentStep);
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    WriteLog(source, message + " - Stopped");
                    Context.LogPublic("[OUTPUT-CASSETTE] STOP " + message);
                    throw new SequenceStopException(message);
                }

                TStep failedStep = CurrentStep;
                CurrentStep = ErrorStep;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                SequenceFailureStore.Record(SequenceStateName, Kind.ToString(), failedStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                Context.LogPublic("[OUTPUT-CASSETTE] FAIL " + alarmCode + " - " + message);
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog(source, "Failure handling failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private async Task<int> MoveLifterZAndVerifyAsync(double target, string description, CancellationToken ct)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("OUT-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");
                string readyReason;
                if (!cassette.CheckBinLifterZMoveReady(out readyReason))
                    return Fail("OUT-CST-MOVE-READY", cassette.Name, "Output cassette is not ready to move. " + readyReason);

                ct.ThrowIfCancellationRequested();
                int commandResult = await MoveLifterZCommandAsync(cassette, target, ct).ConfigureAwait(false);
                if (commandResult != 0)
                    return commandResult;

                ct.ThrowIfCancellationRequested();

                AxisMoveWaitResult waitResult = await cassette.WaitBinLifterZMoveDoneInPosition(
                    target,
                    ResolveMoveTimeout(cassette),
                    ct).ConfigureAwait(false);
                if (!waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("OUT-CST-MOVE", waitResult.Failure), cassette.Name,
                        description + " move/in-position wait failed. waitResult=" + waitResult.Code +
                        ", reason=" + waitResult.Reason + ". " + waitResult.AxisState);

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-MOVE-EX", Name, description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveLifterZCommandAsync(OutputCassetteUnit cassette, double target, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                await cassette.MoveBinLifterZ(target, Options.FineMove).ConfigureAwait(false);
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-CST-MOVE-CMD-EX", cassette != null ? cassette.Name : "OutputCassette",
                    "Output cassette Z move command exception. target=" + target + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static string ResolveAxisMoveWaitAlarmCode(string prefix, AxisMoveWaitFailure failure)
        {
            return AxisMoveWaiter.ResolveAlarmCode(prefix, failure);
        }

        private int RegisterMappingResult(OutputCassetteUnit cassette)
        {
            try
            {
                if (cassette == null)
                    return -1;

                int slotCount = cassette.Config != null ? cassette.Config.SlotCount : 0;
                string goodReason;
                string ngReason;
                bool updateGood = CanUpdateOutputCassetteMapping(BinSide.Good, out goodReason);
                bool updateNg = CanUpdateOutputCassetteMapping(BinSide.Ng, out ngReason);
                if (!updateGood && !updateNg)
                    return Fail("OUT-CST-MAP-SIDE-BLOCK", Name,
                        "Output cassette mapping 결과를 반영할 수 있는 side가 없습니다. goodReason=" + goodReason +
                        ", ngReason=" + ngReason);

                MaterialStateService.UpdateOutputCassetteMappingSelective(
                    updateGood,
                    updateNg,
                    ResolveGoodLevelCount(cassette),
                    slotCount,
                    ResolveSlotMap(cassette, TargetCassette.Good1),
                    ResolveSlotMap(cassette, TargetCassette.Good2),
                    ResolveSlotMap(cassette, TargetCassette.Ng),
                    BuildSlotPositions(cassette, TargetCassette.Good1),
                    BuildSlotPositions(cassette, TargetCassette.Good2),
                    BuildSlotPositions(cassette, TargetCassette.Ng),
                    LotStorage.ActiveLot != null ? LotStorage.ActiveLot.LotID : "",
                    MaterialStateService.ResolveRecipeTapeFrameSpecName(cassette.Config != null ? cassette.Config.InchSelect : 0));
                WriteLog("RegisterMappingResult",
                    "Output cassette mapping result registered. updateGood=" + updateGood +
                    ", goodReason=" + goodReason +
                    ", updateNg=" + updateNg +
                    ", ngReason=" + ngReason + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                WriteLog("RegisterMappingResult", "Output cassette mapping result registration exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private bool CanUpdateOutputCassetteMapping(BinSide side, out string reason)
        {
            try
            {
                MaterialLocationKind stageLocation = side == BinSide.Ng
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;
                WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(stageLocation);
                if (stageWafer != null)
                {
                    reason = FormatOutputSideName(side) + " OutputStage에 진행 중인 자재가 있습니다. waferId=" + stageWafer.WaferId;
                    return false;
                }

                WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                BinSide feederSide;
                if (TryResolveBinSide(feederWafer, out feederSide) && feederSide == side)
                {
                    reason = FormatOutputSideName(side) + " OutputFeeder에 진행 중인 자재가 있습니다. waferId=" + feederWafer.WaferId;
                    return false;
                }

                OutputSlotPlan plan;
                if (OutputSlotPlanner.TryResolveNextSupplySlot(side, out plan))
                {
                    reason = FormatOutputSideName(side) + " 출력 카세트에 아직 사용할 Bin이 남아 있습니다. cassette=" +
                             plan.CassetteRole + ", slot=" + plan.SlotIndex;
                    return false;
                }

                reason = FormatOutputSideName(side) + " 출력 카세트 교체 상태로 판단되어 mapping 반영 가능합니다.";
                return true;
            }
            catch (Exception ex)
            {
                reason = FormatOutputSideName(side) + " mapping 반영 조건 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
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

        private static string FormatOutputSideName(BinSide side)
        {
            return side == BinSide.Ng ? "NG" : "OK";
        }

        private static IReadOnlyList<bool> ResolveSlotMap(OutputCassetteUnit cassette, TargetCassette target)
        {
            if (cassette == null || cassette.SlotMap == null)
                return null;

            bool[] map;
            if (cassette.SlotMap.TryGetValue(target, out map))
                return map;
            return null;
        }

        private static double[] BuildSlotPositions(OutputCassetteUnit cassette, TargetCassette target)
        {
            int count = cassette != null && cassette.Config != null ? cassette.Config.SlotCount : 0;
            if (count < 0)
                count = 0;

            var positions = new double[count];
            for (int i = 0; i < positions.Length; i++)
                positions[i] = cassette.CalculateBinCassetteSlotTargetPosition(target, i);
            return positions;
        }

        private TargetCassette ResolveFirstAvailableOutputCassette(OutputCassetteUnit cassette)
        {
            if (Options.TargetCassette == TargetCassette.Ng ||
                Options.TargetCassette == TargetCassette.Good1 ||
                Options.TargetCassette == TargetCassette.Good2)
                return Options.TargetCassette;

            return cassette != null && cassette.FindFirstEmptySlot(TargetCassette.Good1) >= 0
                ? TargetCassette.Good1
                : TargetCassette.Good2;
        }

        private TargetCassette ResolveMappingReturnCassette()
        {
            if (Options.TargetCassette == TargetCassette.Ng ||
                Options.TargetCassette == TargetCassette.Good1 ||
                Options.TargetCassette == TargetCassette.Good2)
                return Options.TargetCassette;

            return TargetCassette.Good1;
        }

        private double ResolveLoadingPosition()
        {
            return Options.TargetCassette == TargetCassette.Ng
                ? Cassette.Recipe.NGLoaingPosition
                : Cassette.Recipe.GoodLoaingPosition;
        }

        private double ResolveUnloadingPosition()
        {
            return Options.TargetCassette == TargetCassette.Ng
                ? Cassette.Recipe.NGUnloadingPosition
                : Cassette.Recipe.GoodUnloadingPosition;
        }

        private int ResolveCassetteSize(OutputCassetteUnit cassette)
        {
            if (Options.RequiredCassetteSize == 8 || Options.RequiredCassetteSize == 12)
                return Options.RequiredCassetteSize;
            return MaterialStateService.ResolveWaferSizeInch(cassette.Config.InchSelect);
        }

        private int ResolveGoodLevelCount(OutputCassetteUnit cassette)
        {
            int configured = Options.GoodLevelCount > 0
                ? Options.GoodLevelCount
                : cassette != null && cassette.Config != null ? cassette.Config.SelectedCassetteLevel : 2;
            return configured >= 2 ? 2 : 1;
        }

        private bool IsHardwareBypassed()
        {
            AppSettings settings = AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (Context.Controller != null && Context.Controller.GlobalDryRun) ||
                   (Cassette != null && Cassette.Setup != null && Cassette.Setup.IsSimulationMode) ||
                   (Cassette != null && Cassette.Config != null && Cassette.Config.bDryRun);
        }

        private int ResolveMoveTimeout(OutputCassetteUnit cassette)
        {
            if (Options.MoveTimeoutMs > 0)
                return Options.MoveTimeoutMs;

            return cassette != null && cassette.OutputLifterZ != null && cassette.OutputLifterZ.Setup != null && cassette.OutputLifterZ.Setup.MoveTimeoutMs > 0
                ? cassette.OutputLifterZ.Setup.MoveTimeoutMs
                : 10000;
        }

        private TStep ResolveStartStep(TStep defaultStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    WriteLog("ResolveStartStep", "Output cassette " + Kind + " sequence forced restart from step=" + defaultStep + ". - Ok");
                    return defaultStep;
                }

                string stepText = SequenceResumeStore.ResolveStartStep(SequenceStateName, defaultStep.ToString());
                TStep parsed;
                if (Enum.TryParse(stepText, out parsed) &&
                    !IsStep(parsed, IdleStep) &&
                    !IsStep(parsed, CompleteStep) &&
                    !IsStep(parsed, ErrorStep))
                {
                    WriteLog("ResolveStartStep", "Output cassette " + Kind + " sequence resume step=" + parsed + ". - Ok");
                    return parsed;
                }

                return defaultStep;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStartStep", "Output cassette " + Kind + " sequence resume step resolve failed: " + ex.Message + " - Failed");
                return defaultStep;
            }
            finally
            {
            }
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + Kind; }
        }

        protected static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitIntAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        protected static async Task<bool> AwaitStepWithCancellationAsync(Task<bool> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitBoolAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        protected static async Task<AxisMoveWaitResult> AwaitStepWithCancellationAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
        {
            try
            {
                AxisMoveWaitResult defaultValue =
                    new AxisMoveWaitResult(AxisMoveWaitFailure.AxisMissing, "Step task is null.", string.Empty);
                return await SequenceAwaiter.AwaitAsync(stepTask, defaultValue, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private static bool IsStep(TStep left, TStep right)
        {
            return object.Equals(left, right);
        }

        protected static void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.OutputSeq, source, message);
        }
    }
}
