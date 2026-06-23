using System;
using System.Collections.Generic;
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
        WaitOutputStageReceiveComplete,
        StopNoOutputBinWork
    }

    internal static class OutputCassetteOperatorMessageHelper
    {
        private static readonly object _messageLock = new object();
        private static readonly Dictionary<string, DateTime> _lastMessageTimeUtcByTitle = new Dictionary<string, DateTime>();

        public static void RequestReplacement(MachineSequenceContext context, BinSide side, CassetteMaterialRole cassetteRole, string detail)
        {
            RequestReplacement(context, side, FormatCassetteLabel(side, cassetteRole), detail);
        }

        public static void RequestReplacement(MachineSequenceContext context, BinSide side, string cassetteLabel, string detail)
        {
            try
            {
                if (context == null)
                    return;

                string sideName = FormatSideName(side);
                string label = string.IsNullOrWhiteSpace(cassetteLabel) ? sideName + " 출력 카세트" : cassetteLabel;
                string message = sideName + " 출력 카세트(" + label + ") 작업이 완료되었습니다.\r\n" +
                                 "카세트를 교체한 뒤 필요한 작업을 진행하세요.";
                if (!string.IsNullOrWhiteSpace(detail))
                    message += "\r\n" + detail;

                string title = "출력 카세트 교체 - " + sideName;
                if (ShouldSuppressDuplicate(title))
                    return;

                context.RequestOperatorMessage(title, message);
                Log.Write("Main", "SYSTEM", "OutputCassette",
                    message.Replace("\r\n", " ") + " - Notice");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "OutputCassette",
                    "출력 카세트 교체 안내 메시지 요청 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static string FormatSideName(BinSide side)
        {
            return side == BinSide.Ng ? "NG" : "OK";
        }

        private static bool ShouldSuppressDuplicate(string title)
        {
            lock (_messageLock)
            {
                DateTime now = DateTime.UtcNow;
                DateTime lastTime;
                if (_lastMessageTimeUtcByTitle.TryGetValue(title, out lastTime) &&
                    (now - lastTime).TotalSeconds < 5.0)
                    return true;

                _lastMessageTimeUtcByTitle[title] = now;
                return false;
            }
        }

        private static string FormatCassetteLabel(BinSide side, CassetteMaterialRole cassetteRole)
        {
            if (cassetteRole == CassetteMaterialRole.Good1 ||
                cassetteRole == CassetteMaterialRole.Good2 ||
                cassetteRole == CassetteMaterialRole.Ng1)
                return cassetteRole.ToString();

            return side == BinSide.Ng ? "Ng1" : "Good";
        }
    }

    public class OutputSequence : UnitSequenceBase
    {
        public OutputSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.OutputUnloader, "Output")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int result = await ExecuteNextOutputStepAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
                    if (result != 0)
                        throw new InvalidOperationException(
                            SequenceFailureStore.AppendRecentDetail(
                                "Output 자동 시퀀스 실패. result=" + result,
                                "OutputSequence",
                                "OUTPUT-AUTO"));

                    Context.StopIfCycleStopRequested("OutputSequence.AutoActionComplete");
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteAutoAsync", "Output 자동 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("OUTPUT-AUTO-EX", "OutputSequence", "Output 자동 시퀀스 예외 발생: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            try
            {
                int result = await ExecuteNextOutputStepAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException(
                        SequenceFailureStore.AppendRecentDetail(
                            "Output 수동/스텝 시퀀스 실패. result=" + result,
                            "OutputSequence",
                            "OUTPUT-STEP"));
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteStepAsync", "Output 수동/스텝 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("OUTPUT-STEP-EX", "OutputSequence", "Output 수동/스텝 시퀀스 예외 발생: " + ex.Message);
                throw;
            }
            finally
            {
            }
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
                    // NG 스테이지 완료품을 카세트로 배출
                    case OutputSequenceAutoAction.StoreNgStageToCassette:
                        return await ExecuteCompletedStageStoreAsync(
                            ct,
                            BinSide.Ng,
                            DieGrade.Ng,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    // GOOD 스테이지 완료품을 카세트로 배출
                    case OutputSequenceAutoAction.StoreGoodStageToCassette:
                        return await ExecuteCompletedStageStoreAsync(
                            ct,
                            BinSide.Good,
                            DieGrade.Good,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    // 피더 보유품 상태를 이어서 처리
                    case OutputSequenceAutoAction.ResumeOccupiedFeeder:
                        return await ExecuteOccupiedFeederActionAsync(
                            ct,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    // GOOD 카세트에서 스테이지로 공급
                    case OutputSequenceAutoAction.SupplyGoodCassetteToStage:
                        return await ExecuteSupplyCassetteToStageAsync(
                            ct,
                            BinSide.Good,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    // NG 카세트에서 스테이지로 공급
                    case OutputSequenceAutoAction.SupplyNgCassetteToStage:
                        return await ExecuteSupplyCassetteToStageAsync(
                            ct,
                            BinSide.Ng,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);

                    // 아웃풋 스테이지 수령 완료 대기
                    case OutputSequenceAutoAction.WaitOutputStageReceiveComplete:
                        SetOutputStageReadySignals();
                        await WaitAnyOutputReceiveCompleteAsync(ct).ConfigureAwait(false);
                        return 0;

                    // 출력 카세트에 더 이상 진행 가능한 Bin이 없음
                    case OutputSequenceAutoAction.StopNoOutputBinWork:
                        return StopOutputAutoNoBinWork();

                    default:
                        return StopAutoSequence("Output 시퀀스 다음 작업을 결정할 수 없습니다.");
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
                return Fail("OUT-NEXT-EX", "OutputSequence", "Output 다음 작업 결정 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteNextOutputLoadAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (feederWafer != null)
                {
                    if (IsOutputBinReceiveComplete(feederWafer))
                        return Fail("OUT-MANUAL-LOAD-FEEDER-COMPLETE", "OutputSequence", "OutputFeeder에 완료된 Bin이 있습니다. OUTPUT UNLOAD를 먼저 실행하세요.");

                    return await ExecuteOccupiedFeederActionAsync(
                        ct,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                }

                bool canSupplyGood = CanSupplyOutputStage(BinSide.Good);
                bool canSupplyNg = CanSupplyOutputStage(BinSide.Ng);

                if (canSupplyNg && (!canSupplyGood || AreBothOutputStagesEmpty()))
                    return await ExecuteSupplyCassetteToStageAsync(ct, BinSide.Ng, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                if (canSupplyGood)
                    return await ExecuteSupplyCassetteToStageAsync(ct, BinSide.Good, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                if (canSupplyNg)
                    return await ExecuteSupplyCassetteToStageAsync(ct, BinSide.Ng, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                return Fail("OUT-MANUAL-LOAD-NO-SLOT", "OutputSequence", "OUTPUT LOAD 가능한 Bin이 없습니다. OutputStage 빈 상태와 Output Cassette 매핑/슬롯 상태를 확인하세요.");
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteNextOutputLoadAsync", "Output Manual LOAD가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-MANUAL-LOAD-EX", "OutputSequence", "Output Manual LOAD 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteNextOutputUnloadAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsStageReceiveComplete(BinSide.Ng))
                    return await ExecuteCompletedStageStoreAsync(ct, BinSide.Ng, DieGrade.Ng, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                if (IsStageReceiveComplete(BinSide.Good))
                    return await ExecuteCompletedStageStoreAsync(ct, BinSide.Good, DieGrade.Good, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);

                WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (feederWafer != null && IsOutputBinReceiveComplete(feederWafer))
                    return await ExecuteOccupiedFeederActionAsync(
                        ct,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);

                return Fail("OUT-MANUAL-UNLOAD-NO-BIN", "OutputSequence", "OUTPUT UNLOAD 가능한 완료 Bin이 없습니다. OutputStage 수령 완료 상태 또는 OutputFeeder 보유 Bin 상태를 확인하세요.");
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteNextOutputUnloadAsync", "Output Manual UNLOAD가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-MANUAL-UNLOAD-EX", "OutputSequence", "Output Manual UNLOAD 중 예외가 발생했습니다: " + ex.Message);
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

            bool canSupplyGood = CanSupplyOutputStage(BinSide.Good);
            bool canSupplyNg = CanSupplyOutputStage(BinSide.Ng);

            if (canSupplyNg && (!canSupplyGood || AreBothOutputStagesEmpty()))
                return OutputSequenceAutoAction.SupplyNgCassetteToStage;

            if (canSupplyGood)
                return OutputSequenceAutoAction.SupplyGoodCassetteToStage;

            if (canSupplyNg)
                return OutputSequenceAutoAction.SupplyNgCassetteToStage;

            if (IsOutputAutoNoBinWorkComplete())
                return OutputSequenceAutoAction.StopNoOutputBinWork;

            return OutputSequenceAutoAction.WaitOutputStageReceiveComplete;
        }

        private static bool AreBothOutputStagesEmpty()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) == null &&
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) == null;
        }

        private static bool CanSupplyOutputStage(BinSide side)
        {
            MaterialLocationKind location = side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;

            OutputSlotPlan plan;
            return MaterialStateService.GetWaferAtLocation(location) == null &&
                   OutputSlotPlanner.TryResolveNextSupplySlot(side, out plan);
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
                return Fail("OUT-FEEDER-DATA-MISSING", "OutputSequence", "OutputFeeder 보유 Bin 처리 전에 자재 데이터가 사라졌습니다.");

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
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    SetOutputStageReadySignals();

                    if (Context.Bus.IsSet("OutputGoodStageReceiveComplete") ||
                        IsStageReceiveComplete(BinSide.Good))
                    {
                        Context.Bus.Set("OutputGoodStageReceiveComplete");
                        WriteLog("WaitAnyOutputReceiveCompleteAsync", "GOOD OutputStage 수령 완료 신호를 확인했습니다. - Ok");
                        return;
                    }

                    if (Context.Bus.IsSet("OutputNgStageReceiveComplete") ||
                        IsStageReceiveComplete(BinSide.Ng))
                    {
                        Context.Bus.Set("OutputNgStageReceiveComplete");
                        WriteLog("WaitAnyOutputReceiveCompleteAsync", "NG OutputStage 수령 완료 신호를 확인했습니다. - Ok");
                        return;
                    }

                    if (IsOutputAutoNoBinWorkComplete())
                    {
                        StopOutputAutoNoBinWork();
                    }

                    Context.StopIfCycleStopRequested("OutputSequence.WaitReceiveComplete");
                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                WriteLog("WaitAnyOutputReceiveCompleteAsync", "OutputStage 수령 완료 대기가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("OutputStage 수령 완료 대기 중 예외가 발생했습니다: " + ex.Message, ex);
            }
            finally
            {
            }
        }

        private int StopOutputAutoNoBinWork()
        {
            try
            {
                ResetOutputStageReadySignals();

                string reason = BuildOutputNoBinWorkReason();
                OutputCassetteOperatorMessageHelper.RequestReplacement(Context, BinSide.Good, "OK 출력 카세트 전체", reason);
                OutputCassetteOperatorMessageHelper.RequestReplacement(Context, BinSide.Ng, "NG 출력 카세트", reason);
                Log.Write("Main", "SYSTEM", "OutputSequence", reason + " - Stopped");

                return StopAutoSequence(reason);
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "OUT-NO-BIN-WORK-CHECK",
                    "OutputSequence",
                    "출력 Bin 작업 완료 상태 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsOutputAutoNoBinWorkComplete()
        {
            try
            {
                if (HasOutputActiveMaterial())
                    return false;

                if (CanSupplyOutputStage(BinSide.Good))
                    return false;

                if (CanSupplyOutputStage(BinSide.Ng))
                    return false;

                if (MaterialStateService.IsOutputStageReceiveAvailable(BinSide.Good))
                    return false;

                if (MaterialStateService.IsOutputStageReceiveAvailable(BinSide.Ng))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "OutputSequence",
                    "출력 자동 시퀀스 완료 상태 확인 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool HasOutputActiveMaterial()
        {
            try
            {
                return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null ||
                       MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) != null ||
                       MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) != null;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "OutputSequence",
                    "출력 자재 진행 상태 확인 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return true;
            }
            finally
            {
            }
        }

        private static string BuildOutputNoBinWorkReason()
        {
            try
            {
                OutputSlotPlan goodPlan;
                OutputSlotPlan ngPlan;
                bool goodSupply = OutputSlotPlanner.TryResolveNextSupplySlot(BinSide.Good, out goodPlan);
                bool ngSupply = OutputSlotPlanner.TryResolveNextSupplySlot(BinSide.Ng, out ngPlan);
                bool goodReceive = MaterialStateService.IsOutputStageReceiveAvailable(BinSide.Good);
                bool ngReceive = MaterialStateService.IsOutputStageReceiveAvailable(BinSide.Ng);

                return "출력 카세트에 공급 가능한 Bin이 없고 OutputFeeder/OutputStage에 진행 중인 Bin도 없습니다. " +
                       "출력 카세트를 교체하거나 매핑/자재 상태를 확인하세요. " +
                       "goodSupply=" + goodSupply +
                       ", ngSupply=" + ngSupply +
                       ", goodReceiveAvailable=" + goodReceive +
                       ", ngReceiveAvailable=" + ngReceive;
            }
            catch (Exception ex)
            {
                return "출력 카세트에 공급 가능한 Bin이 없고 OutputFeeder/OutputStage에 진행 중인 Bin도 없습니다. " +
                       "출력 카세트를 교체하거나 매핑/자재 상태를 확인하세요. detail=" + ex.Message;
            }
            finally
            {
            }
        }

        private void ResetOutputStageReadySignals()
        {
            try
            {
                Context.Bus.Reset("OutputGoodStageReady");
                Context.Bus.Reset("OutputNgStageReady");
                Context.Bus.Reset("OutputStageReady");
            }
            catch (Exception ex)
            {
                WriteLog("OutputSequence", "OutputStage Ready 신호 초기화 중 예외가 발생했습니다: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void SetOutputStageReadySignals()
        {
            if (EnsureOutputStageReadyForPlace(BinSide.Good))
                Context.Bus.Set("OutputGoodStageReady");
            else
                Context.Bus.Reset("OutputGoodStageReady");

            if (EnsureOutputStageReadyForPlace(BinSide.Ng))
                Context.Bus.Set("OutputNgStageReady");
            else
                Context.Bus.Reset("OutputNgStageReady");

            if (Context.Bus.IsSet("OutputGoodStageReady") ||
                Context.Bus.IsSet("OutputNgStageReady"))
            {
                Context.Bus.Set("OutputStageReady");
            }
            else
            {
                Context.Bus.Reset("OutputStageReady");
            }
        }

        private bool EnsureOutputStageReadyForPlace(BinSide side)
        {
            try
            {
                string reason;
                if (MaterialStateService.IsOutputStageReceiveAvailable(side, out reason))
                    return true;

                WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(
                    side == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood);
                if (stageWafer == null)
                    return false;

                if (stageWafer.OutputReceiveTotalCount <= 0)
                {
                    bool initialized = MaterialStateService.InitializeOutputStageReceivePlan(side);
                    WriteLog("EnsureOutputStageReadyForPlace",
                        "OutputStage 수령 계획이 없어 재초기화를 시도했습니다. side=" + side +
                        ", wafer=" + stageWafer.WaferId +
                        ", initialized=" + initialized +
                        ", reason=" + reason + " - Check");
                    if (!initialized)
                        return false;
                }

                WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (feederWafer != null)
                {
                    WriteLog("EnsureOutputStageReadyForPlace",
                        "OutputFeeder에 진행 중인 자재가 있어 OutputStageReady를 보류합니다. side=" + side +
                        ", feederWafer=" + feederWafer.WaferId + " - Check");
                    return false;
                }

                return MaterialStateService.IsOutputStageReceiveAvailable(side, out reason);
            }
            catch (Exception ex)
            {
                WriteLog("EnsureOutputStageReadyForPlace",
                    "OutputStage Ready 상태 확인 중 예외가 발생했습니다. side=" + side +
                    ", error=" + ex.Message + " - Failed");
                return false;
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
                    return Fail("OUT-SLOT-UNAVAILABLE", "OutputSequence", "Output 카세트의 동일 Source Slot을 사용할 수 없습니다. grade=" + grade + ", reason=" + slotPlanReason);

                using (SequenceResourceLease placeLease = await AcquireOutputPlaceAreaAsync("OutputStore", ct).ConfigureAwait(false))
                {
                    if (placeLease == null)
                        return Fail("OUT-RESOURCE-PLACE", "OutputSequence", "Output Place 영역 리소스 점유에 실패했습니다. side=" + plan.Side);

                    using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(plan.Side, "OutputStore", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "OutputStage 영역 리소스 점유에 실패했습니다. side=" + plan.Side);

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

                using (SequenceResourceLease placeLease = await AcquireOutputPlaceAreaAsync("OutputFeederResumeLoad", ct).ConfigureAwait(false))
                {
                    if (placeLease == null)
                        return Fail("OUT-RESOURCE-PLACE", "OutputSequence", "Output Place 영역 리소스 점유에 실패했습니다. side=" + side);

                    using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(side, "OutputFeederResumeLoad", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "OutputStage 영역 리소스 점유에 실패했습니다. side=" + side);

                        int result = await ExecuteStagePrepareLoadAsync(ct, side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                        if (result != 0) return result;

                        result = await ExecuteFeederLoadToStageAsync(ct, side, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                        if (result != 0) return result;
                    }
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

                using (SequenceResourceLease placeLease = await AcquireOutputPlaceAreaAsync("OutputSupply", ct).ConfigureAwait(false))
                {
                    if (placeLease == null)
                        return Fail("OUT-RESOURCE-PLACE", "OutputSequence", "Output Place 영역 리소스 점유에 실패했습니다. side=" + plan.Side);

                    using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(plan.Side, "OutputSupply", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "OutputStage 영역 리소스 점유에 실패했습니다. side=" + plan.Side);

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
                // GOOD 1단 카세트 처리
                case CassetteMaterialRole.Good1:
                    role = CassetteMaterialRole.Good1;
                    target = TargetCassette.Good1;
                    return side == BinSide.Good;
                // GOOD 2단 카세트 처리
                case CassetteMaterialRole.Good2:
                    role = CassetteMaterialRole.Good2;
                    target = TargetCassette.Good2;
                    return side == BinSide.Good;
                // NG 1단 카세트 처리
                case CassetteMaterialRole.Ng1:
                    role = CassetteMaterialRole.Ng1;
                    target = TargetCassette.Ng;
                    return side == BinSide.Ng;
                default:
                    return false;
            }
        }

        private async Task<SequenceResourceLease> AcquireOutputStageAreaAsync(BinSide side, string holder, CancellationToken ct)
        {
            try
            {
                SequenceResourceKind resource = side == BinSide.Ng
                    ? SequenceResourceKind.OutputNgStageArea
                    : SequenceResourceKind.OutputGoodStageArea;

                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;
                return await AcquireResourceForRunAsync(
                    resource,
                    safeHolder + ":" + side,
                    30000,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<SequenceResourceLease> AcquireOutputPlaceAreaAsync(string holder, CancellationToken ct)
        {
            try
            {
                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;
                return await AcquireResourceForRunAsync(
                    SequenceResourceKind.OutputPlaceArea,
                    safeHolder,
                    30000,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
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
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    Log.Write("Main", "SYSTEM", source, message + " - Stopped");
                    Context.LogPublic("[UNIT-OUTPUT] STOP " + message);
                    throw new SequenceStopException(message);
                }

                message = SequenceFailureStore.AppendRecentDetail(message, "OutputSequence", alarmCode);
                SequenceFailureStore.Record("OutputSequence", Kind.ToString(), "", alarmCode, source, message);
                Log.Write("Main", "SYSTEM", source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                Context.LogPublic("[UNIT-OUTPUT] FAIL " + alarmCode + " - " + message);
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog(source, "Output 실패 처리 중 예외가 발생했습니다: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private int StopAutoSequence(string reason)
        {
            try
            {
                Log.Write("Main", "SYSTEM", "OutputSequence", "Output 시퀀스 정지: " + reason + " - Stopped");
                Context.LogPublic("[UNIT-OUTPUT] STOP " + reason);
            }
            catch (Exception ex)
            {
                WriteLog("OutputSequence", "Output 시퀀스 정지 로그 기록 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            throw new SequenceStopException(reason);
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OutputSequence log failed: " + ex.Message);
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.OutputSeq, source, message);
        }
    }
}
