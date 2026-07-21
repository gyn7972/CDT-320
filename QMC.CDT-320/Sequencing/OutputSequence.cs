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
        private const string OutputLoaderActiveSignal = "OutputLoaderActive";
        private const int MaxOutputLoaderBatchActions = 12;
        private const string OutputLoaderBatchDrainReason = "Output loader 교체를 Feeder Avoid 및 최종 안전 자세까지 완료";
        private int _autoOutputLoaderBatchDepth;
        private bool _stopAfterDrainCapacityLogWritten;

        public OutputSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.OutputUnloader, "Output")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            try
            {
                int preparationResult = await EnsureInitialOrRecipeOutputPreparationAsync(
                    ct,
                    false,
                    0,
                    SequenceStartMode.Resume).ConfigureAwait(false);
                if (preparationResult != 0)
                {
                    throw new InvalidOperationException(
                        SequenceFailureStore.AppendRecentDetail(
                            "Output 초기/레시피 변경 전체 준비 실패. result=" + preparationResult,
                            "OutputSequence",
                            "OUTPUT-FULL-PREPARATION"));
                }

                while (!ct.IsCancellationRequested)
                {
                    if (await WaitForStopAfterDrainCompletionIfRequestedAsync(
                        "OutputSequence.AutoLoop",
                        ct).ConfigureAwait(false))
                    {
                        return;
                    }

                    int result = await ExecuteNextOutputStepAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
                    if (result != 0)
                        throw new InvalidOperationException(
                            SequenceFailureStore.AppendRecentDetail(
                                "Output 자동 시퀀스 실패. result=" + result,
                                "OutputSequence",
                                "OUTPUT-AUTO"));

                    Context.StopIfCycleStopRequested(
                        "OutputSequence.AutoActionComplete",
                        ShouldDeferCycleStopForPickerHeldDieOutputDrain(),
                        OutputLoaderBatchDrainReason);
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

                string consistencyReason;
                if (!ValidateOutputSupplyConsistency(out consistencyReason))
                    return Fail("OUT-SLOT-CONSISTENCY", "OutputSequence", consistencyReason);

                OutputSequenceAutoAction action = ResolveNextOutputAction();
                Context.LogPublic("[OUTPUT] next action=" + action);

                // 일반 Auto 운전 중에는 선택된 GOOD 또는 NG 한쪽의 언로드/재로드만 한 묶음으로 처리한다.
                if (Mode == SequenceRunMode.Auto && IsOutputLoaderWorkAction(action))
                {
                    return await ExecuteCoordinatorOutputLoaderBatchAsync(
                        action,
                        ct,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                }

                switch (action)
                {
                    // Todo: GYN 2026.07.03 - 여기서 순번대로 재개할때 항상 인터락 확인 후에 재개하도록 해야 한다. (Feeder/Stage/Picker)
                    // 재개 Step시에 필요한 인터락 / 안전 상태 확인 후에 작업을 재개하는데 만약 안전 상태로 모션이 가능하면
                    // 안전상태로 모션 시키고 재개하고 그렇지 않으면 알람 발생 후 장비를 멈춘다.

                    // NG 스테이지 완료품을 카세트로 배출
                    case OutputSequenceAutoAction.StoreNgStageToCassette:
                        return await ExecuteCoordinatorOutputLoaderWorkAsync(
                            "OutputStoreNgStageToCassette",
                            ct,
                            () => ExecuteCompletedStageStoreAsync(
                                ct,
                                BinSide.Ng,
                                DieGrade.Ng,
                                bFine,
                                moveTimeoutMs,
                                startMode)).ConfigureAwait(false);

                    // GOOD 스테이지 완료품을 카세트로 배출
                    case OutputSequenceAutoAction.StoreGoodStageToCassette:
                        return await ExecuteCoordinatorOutputLoaderWorkAsync(
                            "OutputStoreGoodStageToCassette",
                            ct,
                            () => ExecuteCompletedStageStoreAsync(
                                ct,
                                BinSide.Good,
                                DieGrade.Good,
                                bFine,
                                moveTimeoutMs,
                                startMode)).ConfigureAwait(false);

                    // 피더 보유품 상태를 이어서 처리
                    case OutputSequenceAutoAction.ResumeOccupiedFeeder:
                        return await ExecuteCoordinatorOutputLoaderWorkAsync(
                            "OutputResumeOccupiedFeeder",
                            ct,
                            () => ExecuteOccupiedFeederActionAsync(
                                ct,
                                bFine,
                                moveTimeoutMs,
                                startMode)).ConfigureAwait(false);

                    // GOOD 카세트에서 스테이지로 공급
                    case OutputSequenceAutoAction.SupplyGoodCassetteToStage:
                        return await ExecuteCoordinatorOutputLoaderWorkAsync(
                            "OutputSupplyGoodCassetteToStage",
                            ct,
                            () => ExecuteSupplyCassetteToStageAsync(
                                ct,
                                BinSide.Good,
                                bFine,
                                moveTimeoutMs,
                                startMode)).ConfigureAwait(false);

                    // NG 카세트에서 스테이지로 공급
                    case OutputSequenceAutoAction.SupplyNgCassetteToStage:
                        return await ExecuteCoordinatorOutputLoaderWorkAsync(
                            "OutputSupplyNgCassetteToStage",
                            ct,
                            () => ExecuteSupplyCassetteToStageAsync(
                                ct,
                                BinSide.Ng,
                                bFine,
                                moveTimeoutMs,
                                startMode)).ConfigureAwait(false);

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

        private async Task<int> ExecuteCoordinatorOutputLoaderWorkAsync(
            string holder,
            CancellationToken ct,
            Func<Task<int>> action)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;

            try
            {
                if (Mode != SequenceRunMode.Auto)
                    return await action().ConfigureAwait(false);

                using (AutoSequenceLoaderWorkLease loaderLease = await Context.AutoLoaderGate
                    .BeginOutputWorkAsync(
                        safeHolder,
                        ct,
                        EnsureOutputPickersAvoidBeforeFeederMoveAsync,
                        AreOutputPickersAvoidAndStopped)
                    .ConfigureAwait(false))
                {
                    return await action().ConfigureAwait(false);
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
                return Fail("OUT-COORDINATOR-GATE", "AutoSequenceCoordinator",
                    safeHolder + " 시작 승인/실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteCoordinatorOutputLoaderBatchAsync(
            OutputSequenceAutoAction firstAction,
            CancellationToken ct,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            return await ExecuteCoordinatorOutputLoaderWorkAsync(
                "OutputLoaderBatch",
                ct,
                async () =>
                {
                    Interlocked.Increment(ref _autoOutputLoaderBatchDepth);
                    try
                    {
                        // Picker 대기 중 바뀐 자재 상태를 반영하되, 일반 운전 중에는 최초 선택 side만 완료한다.
                        OutputSequenceAutoAction action = ResolveNextOutputAction();
                        BinSide batchSide;
                        if (!TryResolveOutputActionSide(action, out batchSide))
                        {
                            return Fail(
                                "OUT-LOADER-BATCH-SIDE",
                                "OutputSequence",
                                "Output Loader 단일 side batch의 GOOD/NG 구분을 확인할 수 없습니다. " +
                                "firstAction=" + firstAction + ", resolvedAction=" + action);
                        }
                        int actionCount = 0;

                        Context.LogPublic(
                            "[OUTPUT] Loader single-side batch start. side=" + batchSide +
                            ", firstAction=" + firstAction +
                            ", resolvedAction=" + action +
                            ", canSupplyGood=" + CanSupplyOutputStage(BinSide.Good) +
                            ", canSupplyNg=" + CanSupplyOutputStage(BinSide.Ng));

                        while (IsOutputLoaderWorkAction(action))
                        {
                            ct.ThrowIfCancellationRequested();
                            actionCount++;
                            if (actionCount > MaxOutputLoaderBatchActions)
                            {
                                return Fail(
                                    "OUT-LOADER-BATCH-LIMIT",
                                    "OutputSequence",
                                    "Output Loader batch 작업 횟수가 제한을 초과했습니다. " +
                                    "firstAction=" + firstAction +
                                    ", currentAction=" + action +
                                    ", limit=" + MaxOutputLoaderBatchActions);
                            }

                            Context.LogPublic(
                                "[OUTPUT] Loader batch action start. index=" + actionCount +
                                ", action=" + action);

                            int result = await ExecuteOutputLoaderActionCoreAsync(
                                action,
                                ct,
                                bFine,
                                moveTimeoutMs,
                                startMode).ConfigureAwait(false);
                            if (result != 0)
                                return result;

                            string consistencyReason;
                            if (!ValidateOutputSupplyConsistency(out consistencyReason))
                                return Fail("OUT-SLOT-CONSISTENCY", "OutputSequence", consistencyReason);

                            OutputSequenceAutoAction nextAction = ResolveNextOutputActionForSide(batchSide);
                            Context.LogPublic(
                                "[OUTPUT] Loader batch action complete. index=" + actionCount +
                                ", side=" + batchSide +
                                ", action=" + action +
                                ", nextAction=" + nextAction);

                            if (nextAction == action)
                            {
                                return Fail(
                                    "OUT-LOADER-BATCH-NO-PROGRESS",
                                    "OutputSequence",
                                    "Output Loader batch가 자재 상태를 갱신하지 못했습니다. " +
                                    "action=" + action +
                                    ", actionCount=" + actionCount);
                            }

                            action = nextAction;
                        }

                        // 현재 기준: 중간 Ready 공개는 막고 교체 묶음의 최종 안전 자세 완료 후 한 번만 공개한다.
                        SetOutputStageReadySignals();
                        Context.LogPublic(
                            "[OUTPUT] Loader single-side batch complete. side=" + batchSide +
                            ", actionCount=" + actionCount +
                            ", nextAction=" + action);
                        return 0;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _autoOutputLoaderBatchDepth);
                    }
                }).ConfigureAwait(false);
        }

        private async Task<int> EnsureInitialOrRecipeOutputPreparationAsync(
            CancellationToken ct,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            MachineController controller = Context != null ? Context.Controller : null;
            string requestReason = string.Empty;
            string requestRecipeName = controller != null ? controller.ActiveRecipeName : string.Empty;
            bool requested = controller != null &&
                             controller.TryGetOutputFullPreparationRequest(out requestReason, out requestRecipeName);
            if (string.IsNullOrWhiteSpace(requestRecipeName) && controller != null)
                requestRecipeName = controller.ActiveRecipeName ?? string.Empty;
            bool outputMapped = AreRequiredOutputCassettesMapped();

            if (!requested && outputMapped)
                return 0;

            if (!requested && controller != null)
            {
                requestReason = "InitialUnmappedOutputCassette";
                requestRecipeName = controller.ActiveRecipeName ?? string.Empty;
                controller.RequestOutputFullPreparation(requestReason, requestRecipeName);
                requested = true;
            }

            string materialRecipeName;
            bool materialRecipeChanged = IsMaterialRecipeDifferent(requestRecipeName, out materialRecipeName);
            bool recipeChange = requestReason.StartsWith("RecipeChange:", StringComparison.OrdinalIgnoreCase) ||
                                materialRecipeChanged;
            if (materialRecipeChanged)
            {
                requestReason = (requestReason ?? string.Empty) +
                                ";MaterialRecipeChange:" + materialRecipeName + "->" + requestRecipeName;
            }
            if (!recipeChange && HasOutputActiveMaterial())
            {
                Context.LogPublic(
                    "[OUTPUT] 초기 전체 준비 요청이 있으나 복구할 진행 자재가 있어 기존 단일 side 재개 흐름을 유지합니다. " +
                    "reason=" + requestReason + ", recipe=" + requestRecipeName);
                if (controller != null)
                    controller.CompleteOutputFullPreparation(controller.ActiveRecipeName ?? string.Empty);
                return 0;
            }

            Context.LogPublic(
                "[OUTPUT] GOOD/NG 전체 준비 시작. reason=" + requestReason +
                ", recipe=" + requestRecipeName +
                ", recipeChange=" + recipeChange +
                ", outputMapped=" + outputMapped);

            return await ExecuteCoordinatorOutputLoaderWorkAsync(
                "OutputFullPreparation",
                ct,
                async () =>
                {
                    Interlocked.Increment(ref _autoOutputLoaderBatchDepth);
                    try
                    {
                        int result = await ExecuteFullOutputPreparationCoreAsync(
                            ct,
                            recipeChange,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);
                        if (result != 0)
                            return result;

                        WaferMaterial goodStage = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood);
                        WaferMaterial ngStage = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg);
                        if (goodStage == null || ngStage == null)
                        {
                            return Fail(
                                "OUT-FULL-PREP-STAGE",
                                "OutputSequence",
                                "초기/레시피 변경 Output 전체 준비 후 GOOD/NG Stage가 모두 채워지지 않았습니다. " +
                                "goodStage=" + (goodStage != null ? goodStage.WaferId : "-") +
                                ", ngStage=" + (ngStage != null ? ngStage.WaferId : "-") +
                                ", reason=" + requestReason);
                        }

                        SetOutputStageReadySignals();
                        if (controller != null &&
                            !string.Equals(controller.ActiveRecipeName ?? string.Empty, requestRecipeName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                        {
                            return Fail(
                                "OUT-FULL-PREP-RECIPE-CHANGED",
                                "OutputSequence",
                                "Output 전체 준비 중 활성 Recipe가 다시 변경되어 완료 상태를 확정하지 않았습니다. " +
                                "requestedRecipe=" + requestRecipeName +
                                ", activeRecipe=" + controller.ActiveRecipeName);
                        }

                        if (controller != null)
                        {
                            MaterialStateService.UpdateRecipeContext(
                                requestRecipeName,
                                "OutputFullPreparationComplete");
                        }
                        if (controller != null &&
                            !controller.CompleteOutputFullPreparation(requestRecipeName))
                        {
                            return Fail(
                                "OUT-FULL-PREP-RECIPE-CHANGED",
                                "OutputSequence",
                                "Output 전체 준비 중 활성 Recipe가 다시 변경되어 완료 상태를 확정하지 않았습니다. " +
                                "requestedRecipe=" + requestRecipeName +
                                ", activeRecipe=" + controller.ActiveRecipeName);
                        }

                        Context.LogPublic(
                            "[OUTPUT] GOOD/NG 전체 준비 완료. goodWafer=" + goodStage.WaferId +
                            ", ngWafer=" + ngStage.WaferId +
                            ", reason=" + requestReason);
                        return 0;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _autoOutputLoaderBatchDepth);
                    }
                }).ConfigureAwait(false);
        }

        private static bool IsMaterialRecipeDifferent(string activeRecipeName, out string materialRecipeName)
        {
            MaterialSnapshot state = MaterialStateService.State;
            materialRecipeName = state != null ? (state.RecipeName ?? string.Empty).Trim() : string.Empty;
            string active = (activeRecipeName ?? string.Empty).Trim();
            return !string.IsNullOrWhiteSpace(materialRecipeName) &&
                   !string.IsNullOrWhiteSpace(active) &&
                   !string.Equals(materialRecipeName, active, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<int> ExecuteFullOutputPreparationCoreAsync(
            CancellationToken ct,
            bool recipeChange,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            if (recipeChange)
            {
                if (!AreRequiredOutputCassettesMapped() && HasOutputActiveMaterial())
                {
                    return Fail(
                        "OUT-FULL-PREP-UNMAPPED-ACTIVE",
                        "OutputSequence",
                        "Recipe 변경 전체 교체 대상 Bin이 있지만 원본 Output cassette mapping 정보가 없습니다. " +
                        "자동 복귀를 중단하고 Material/cassette 상태를 확인하세요.");
                }

                WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (feederWafer != null)
                {
                    BinSide feederSide;
                    if (!TryResolveBinSide(feederWafer, out feederSide))
                        return Fail("OUT-FULL-PREP-FEEDER-SIDE", "Material", "Recipe 변경 시 OutputFeeder Bin의 GOOD/NG를 확인할 수 없습니다. wafer=" + feederWafer.WaferId);

                    int feederResult = await ExecuteOutputFeederStoreToCassetteAsync(
                        feederWafer,
                        feederSide,
                        ct,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                    if (feederResult != 0)
                        return feederResult;
                }

                if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) != null)
                {
                    int ngUnloadResult = await ExecuteCompletedStageStoreAsync(
                        ct,
                        BinSide.Ng,
                        DieGrade.Ng,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                    if (ngUnloadResult != 0)
                        return ngUnloadResult;
                }

                if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) != null)
                {
                    int goodUnloadResult = await ExecuteCompletedStageStoreAsync(
                        ct,
                        BinSide.Good,
                        DieGrade.Good,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                    if (goodUnloadResult != 0)
                        return goodUnloadResult;
                }
            }

            if (!AreRequiredOutputCassettesMapped())
            {
                int mappingResult = await ExecuteCassetteMappingAsync(
                    ct,
                    bFine,
                    moveTimeoutMs,
                    startMode).ConfigureAwait(false);
                if (mappingResult != 0)
                    return mappingResult;
            }

            string consistencyReason;
            if (!ValidateOutputSupplyConsistency(out consistencyReason))
                return Fail("OUT-FULL-PREP-CONSISTENCY", "OutputSequence", consistencyReason);

            if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) == null)
            {
                OutputSlotPlan ngPlan;
                string ngReason;
                if (!OutputSlotPlanner.TryResolveNextSupplySlot(BinSide.Ng, out ngPlan, out ngReason))
                    return Fail("OUT-FULL-PREP-NG-NO-READY", "OutputSequence", "NG Stage 전체 준비용 Ready Bin이 없습니다. reason=" + ngReason);

                int ngLoadResult = await ExecuteSupplyCassetteToStageAsync(
                    ct,
                    BinSide.Ng,
                    bFine,
                    moveTimeoutMs,
                    startMode).ConfigureAwait(false);
                if (ngLoadResult != 0)
                    return ngLoadResult;
            }

            if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) == null)
            {
                OutputSlotPlan goodPlan;
                string goodReason;
                if (!OutputSlotPlanner.TryResolveNextSupplySlot(BinSide.Good, out goodPlan, out goodReason))
                    return Fail("OUT-FULL-PREP-GOOD-NO-READY", "OutputSequence", "GOOD Stage 전체 준비용 Ready Bin이 없습니다. reason=" + goodReason);

                int goodLoadResult = await ExecuteSupplyCassetteToStageAsync(
                    ct,
                    BinSide.Good,
                    bFine,
                    moveTimeoutMs,
                    startMode).ConfigureAwait(false);
                if (goodLoadResult != 0)
                    return goodLoadResult;
            }

            return 0;
        }

        private async Task<int> ExecuteOutputLoaderActionCoreAsync(
            OutputSequenceAutoAction action,
            CancellationToken ct,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            if (action != OutputSequenceAutoAction.ResumeOccupiedFeeder &&
                IsStopAfterDrainRequested())
            {
                bool heldDieDrain = HasPickerHeldTargetDieForOutputDrain();
                bool goodCapacityAction =
                    action == OutputSequenceAutoAction.StoreGoodStageToCassette ||
                    action == OutputSequenceAutoAction.SupplyGoodCassetteToStage;
                if (!heldDieDrain || !goodCapacityAction)
                {
                    WriteLog("WaferCompletionRun",
                        "Stop After Drain 요청으로 신규 Output 작업을 시작하지 않습니다. " +
                        "Picker 보유 Die 배출 중에는 GOOD Stage 용량 확보 작업만 허용합니다. " +
                        "action=" + action + ", heldDieDrain=" + heldDieDrain + " - Ok");
                    return 0;
                }
            }

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

                default:
                    return Fail(
                        "OUT-LOADER-BATCH-ACTION",
                        "OutputSequence",
                        "Output Loader batch에서 처리할 수 없는 작업입니다. action=" + action);
            }
        }

        private bool IsStopAfterDrainRequested()
        {
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion == null || !completion.Enabled)
                return false;

            completion.ObserveCompletionSignals();
            return completion.IsDrainRequested;
        }

        private static bool IsOutputLoaderWorkAction(OutputSequenceAutoAction action)
        {
            return action == OutputSequenceAutoAction.StoreNgStageToCassette ||
                   action == OutputSequenceAutoAction.StoreGoodStageToCassette ||
                   action == OutputSequenceAutoAction.ResumeOccupiedFeeder ||
                   action == OutputSequenceAutoAction.SupplyGoodCassetteToStage ||
                   action == OutputSequenceAutoAction.SupplyNgCassetteToStage;
        }

        private bool IsAutoOutputLoaderBatchActive
        {
            get
            {
                return Mode == SequenceRunMode.Auto &&
                       Volatile.Read(ref _autoOutputLoaderBatchDepth) > 0;
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
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion != null && completion.Enabled)
            {
                completion.ObserveCompletionSignals();
                if (completion.IsDrainRequested)
                {
                    // 이미 OutputFeeder에 올라온 자재는 중간에 방치하지 않고 현재 이송만 안전하게 마무리합니다.
                    if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null)
                        return OutputSequenceAutoAction.ResumeOccupiedFeeder;

                    if (HasPickerHeldTargetDieForOutputDrain())
                        return ResolvePickerHeldDieDrainOutputAction();

                    return OutputSequenceAutoAction.WaitOutputStageReceiveComplete;
                }
            }

            if (IsOutputStageCompletionSignalSet(BinSide.Ng))
                return OutputSequenceAutoAction.StoreNgStageToCassette;

            if (IsOutputStageCompletionSignalSet(BinSide.Good))
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

        private OutputSequenceAutoAction ResolvePickerHeldDieDrainOutputAction()
        {
            // 현재 Place 정책은 검사 결과와 무관하게 Picker 보유 Die를 GOOD Stage로 배출한다.
            // Stop After Drain에서는 그 배출에 필요한 GOOD side 용량만 확보하고 반대 side 신규 교체는 시작하지 않는다.
            if (IsOutputStageCompletionSignalSet(BinSide.Good))
                return OutputSequenceAutoAction.StoreGoodStageToCassette;

            if (CanSupplyOutputStage(BinSide.Good))
                return OutputSequenceAutoAction.SupplyGoodCassetteToStage;

            if (!CanMaintainGoodStageForPickerHeldDieDrain())
                return OutputSequenceAutoAction.StopNoOutputBinWork;

            return OutputSequenceAutoAction.WaitOutputStageReceiveComplete;
        }

        private static bool CanMaintainGoodStageForPickerHeldDieDrain()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) != null ||
                   CanSupplyOutputStage(BinSide.Good);
        }

        private OutputSequenceAutoAction ResolveNextOutputActionForSide(BinSide side)
        {
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion != null && completion.Enabled)
            {
                completion.ObserveCompletionSignals();
                if (completion.IsDrainRequested)
                {
                    WaferMaterial drainFeederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                    BinSide drainFeederSide;
                    if (drainFeederWafer != null &&
                        TryResolveBinSide(drainFeederWafer, out drainFeederSide) &&
                        drainFeederSide == side)
                    {
                        return OutputSequenceAutoAction.ResumeOccupiedFeeder;
                    }

                    if (!HasPickerHeldTargetDieForOutputDrain() || side == BinSide.Ng)
                        return OutputSequenceAutoAction.None;
                }
            }

            if (IsOutputStageCompletionSignalSet(side))
            {
                return side == BinSide.Ng
                    ? OutputSequenceAutoAction.StoreNgStageToCassette
                    : OutputSequenceAutoAction.StoreGoodStageToCassette;
            }

            WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (feederWafer != null)
            {
                BinSide feederSide;
                if (TryResolveBinSide(feederWafer, out feederSide) && feederSide == side)
                    return OutputSequenceAutoAction.ResumeOccupiedFeeder;
                return OutputSequenceAutoAction.None;
            }

            if (CanSupplyOutputStage(side))
            {
                return side == BinSide.Ng
                    ? OutputSequenceAutoAction.SupplyNgCassetteToStage
                    : OutputSequenceAutoAction.SupplyGoodCassetteToStage;
            }

            return OutputSequenceAutoAction.None;
        }

        private bool TryResolveOutputActionSide(OutputSequenceAutoAction action, out BinSide side)
        {
            side = BinSide.Good;
            switch (action)
            {
                case OutputSequenceAutoAction.StoreNgStageToCassette:
                case OutputSequenceAutoAction.SupplyNgCassetteToStage:
                    side = BinSide.Ng;
                    return true;

                case OutputSequenceAutoAction.StoreGoodStageToCassette:
                case OutputSequenceAutoAction.SupplyGoodCassetteToStage:
                    side = BinSide.Good;
                    return true;

                case OutputSequenceAutoAction.ResumeOccupiedFeeder:
                    return TryResolveBinSide(
                        MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder),
                        out side);

                default:
                    return false;
            }
        }

        private static bool AreRequiredOutputCassettesMapped()
        {
            MaterialSnapshot state = MaterialStateService.State;
            if (state == null || state.Cassettes == null)
                return false;

            CassetteMaterial good1 = null;
            CassetteMaterial good2 = null;
            CassetteMaterial ng1 = null;
            foreach (CassetteMaterial cassette in state.Cassettes)
            {
                if (cassette == null)
                    continue;

                if (cassette.Role == CassetteMaterialRole.Good1)
                    good1 = cassette;
                else if (cassette.Role == CassetteMaterialRole.Good2)
                    good2 = cassette;
                else if (cassette.Role == CassetteMaterialRole.Ng1)
                    ng1 = cassette;
            }

            if (!IsOutputCassetteMapped(good1) || !IsOutputCassetteMapped(ng1))
                return false;
            if (good2 != null && good2.IsEnabled && !IsOutputCassetteMapped(good2))
                return false;

            return true;
        }

        private static bool IsOutputCassetteMapped(CassetteMaterial cassette)
        {
            return cassette != null && cassette.IsEnabled && cassette.IsPresent && cassette.IsMapped;
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

        private bool IsOutputStageCompletionSignalSet(BinSide side)
        {
            string signal = side == BinSide.Ng
                ? "OutputNgStageReceiveComplete"
                : "OutputGoodStageReceiveComplete";

            return Context != null && Context.Bus != null && Context.Bus.IsSet(signal);
        }

        private async Task<bool> TryRestoreOutputStageCompletionSignalAfterSafeRecoveryAsync(
            BinSide side,
            CancellationToken ct)
        {
            if (!IsStageReceiveComplete(side))
                return false;

            string reason;
            if (!AreOutputPickersAvoidAndStopped(out reason))
                return false;

            if (Context == null || Context.Bus == null)
                return false;

            if (Context.OutputPostPlaceInspections != null)
            {
                int idleResult = await Context.OutputPostPlaceInspections.WaitUntilIdleAsync(
                    "OutputSequenceRecovery:" + side,
                    0,
                    ct).ConfigureAwait(false);
                if (idleResult != 0)
                {
                    throw new InvalidOperationException(
                        side + " OutputStage 교체 준비 신호 복구 전 후검사 완료 대기 실패. result=" + idleResult);
                }
            }

            if (!IsStageReceiveComplete(side) || !AreOutputPickersAvoidAndStopped(out reason))
                return false;

            string signal = side == BinSide.Ng
                ? "OutputNgStageReceiveComplete"
                : "OutputGoodStageReceiveComplete";
            Context.Bus.Set(signal);
            WriteLog("OutputStageCompletionSignal",
                side + " OutputStage 복구 시 Material 완료, Front/Rear Picker 전체 Avoid, 후검사 완료를 확인한 후 교체 준비 신호를 복구했습니다. " +
                "signal=" + signal + " - Ok");
            return true;
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

                    if (await WaitForStopAfterDrainCompletionIfRequestedAsync(
                        "OutputSequence.WaitReceiveComplete",
                        ct).ConfigureAwait(false))
                    {
                        return;
                    }

                    if (IsOutputStageCompletionSignalSet(BinSide.Good) ||
                        await TryRestoreOutputStageCompletionSignalAfterSafeRecoveryAsync(BinSide.Good, ct).ConfigureAwait(false))
                    {
                        WriteLog("WaitAnyOutputReceiveCompleteAsync", "GOOD OutputStage 수령 완료 신호를 확인했습니다. - Ok");
                        return;
                    }

                    bool drainHeldDieToGood =
                        IsStopAfterDrainRequested() &&
                        HasPickerHeldTargetDieForOutputDrain();

                    if (!drainHeldDieToGood &&
                        (IsOutputStageCompletionSignalSet(BinSide.Ng) ||
                         await TryRestoreOutputStageCompletionSignalAfterSafeRecoveryAsync(BinSide.Ng, ct).ConfigureAwait(false)))
                    {
                        WriteLog("WaitAnyOutputReceiveCompleteAsync", "NG OutputStage 수령 완료 신호를 확인했습니다. - Ok");
                        return;
                    }

                    if (IsOutputAutoNoBinWorkComplete() &&
                        !(drainHeldDieToGood &&
                          CanMaintainGoodStageForPickerHeldDieDrain()))
                    {
                        StopOutputAutoNoBinWork();
                    }

                    Context.StopIfCycleStopRequested(
                        "OutputSequence.WaitReceiveComplete",
                        ShouldDeferCycleStopForPickerHeldDieOutputDrain(),
                        OutputLoaderBatchDrainReason);
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

        private async Task<bool> WaitForStopAfterDrainCompletionIfRequestedAsync(
            string boundary,
            CancellationToken ct)
        {
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion == null || !completion.Enabled)
                return false;

            completion.ObserveCompletionSignals();
            if (!completion.IsDrainRequested)
                return false;

            if (HasPickerHeldTargetDieForOutputDrain() || HasOutputFeederMaterialForDrain())
            {
                if (!_stopAfterDrainCapacityLogWritten)
                {
                    _stopAfterDrainCapacityLogWritten = true;
                    WriteLog("WaferCompletionRun",
                        "Stop After Drain 요청 상태지만 Picker 보유 제품 배출 또는 OutputFeeder 잔여 이송을 계속합니다. " +
                        "boundary=" + (boundary ?? "-") + " - Drain");
                }
                return false;
            }

            WriteLog("WaferCompletionRun",
                "Stop After Drain 요청으로 Output Wafer 교체를 중단하고 Picker 보유 제품 Place/후검사 완료를 기다립니다. " +
                "boundary=" + (boundary ?? "-") + " - Wait");

            while (!completion.IsRunComplete)
            {
                ct.ThrowIfCancellationRequested();
                // Drain 요청과 마지막 Pick의 Material 이동 사이 race를 닫는다.
                // 대기 진입 뒤 Picker 보유 제품이 생기면 즉시 Output 용량 확보 루프로 복귀한다.
                if (HasPickerHeldTargetDieForOutputDrain() || HasOutputFeederMaterialForDrain())
                {
                    if (!_stopAfterDrainCapacityLogWritten)
                    {
                        _stopAfterDrainCapacityLogWritten = true;
                        WriteLog("WaferCompletionRun",
                            "Stop After Drain 완료 대기 중 Picker 보유 제품 또는 OutputFeeder 잔여 자재가 확인되어 Output 처리를 재개합니다. " +
                            "boundary=" + (boundary ?? "-") + " - Drain");
                    }
                    return false;
                }

                SetOutputStageReadySignals();
                await Task.Delay(50, ct).ConfigureAwait(false);
            }

            WriteLog("WaferCompletionRun",
                "Stop After Drain 안전 배출 완료를 확인하여 Output 자동 시퀀스를 종료합니다. " +
                "boundary=" + (boundary ?? "-") + " - Ok");
            return true;
        }

        private int StopOutputAutoNoBinWork()
        {
            try
            {
                ResetOutputStageReadySignals();

                string reason = BuildOutputNoBinWorkReason();
                OutputCassetteOperatorMessageHelper.RequestReplacement(Context, BinSide.Good, "OK 출력 카세트 전체", reason);
                OutputCassetteOperatorMessageHelper.RequestReplacement(Context, BinSide.Ng, "NG 출력 카세트", reason);
                Log.Write("Main", "SYSTEM", "OutputSequence", reason + " - Failed");

                // Picker가 보유 Die를 가진 상태에서도 sibling 종료가 전파되도록 정상 SequenceStop이 아니라
                // 명시적 unit failure로 처리한다. Coordinator가 CycleStop 경계 후 timeout abort까지 담당한다.
                return Fail("OUT-STAGE-SUPPLY-UNAVAILABLE", "OutputSequence", reason);
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
                if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null)
                    return false;

                bool goodStagePresent = MaterialStateService.GetWaferAtLocation(
                    MaterialLocationKind.OutputStageGood) != null;
                bool ngStagePresent = MaterialStateService.GetWaferAtLocation(
                    MaterialLocationKind.OutputStageNg) != null;
                bool canSupplyGood = CanSupplyOutputStage(BinSide.Good);
                bool canSupplyNg = CanSupplyOutputStage(BinSide.Ng);

                // 자동 운전은 GOOD/NG Stage를 모두 준비하는 계약이다. 한 side가 비었는데
                // 공급 가능한 Bin도 없으면 다른 side가 남아 있어도 기다리지 않고 교체를 요청한다.
                if (!goodStagePresent && !canSupplyGood)
                    return true;

                if (!ngStagePresent && !canSupplyNg)
                    return true;

                return false;
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
                bool feederPresent = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null;
                bool goodStagePresent = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood) != null;
                bool ngStagePresent = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) != null;

                return "자동 운전에 필요한 GOOD/NG OutputStage를 모두 유지할 수 없습니다. " +
                       "출력 카세트를 교체하거나 매핑/자재 상태를 확인하세요. " +
                       "feederPresent=" + feederPresent +
                       ", goodStagePresent=" + goodStagePresent +
                       ", ngStagePresent=" + ngStagePresent +
                       ", goodSupply=" + goodSupply +
                       ", ngSupply=" + ngSupply +
                       ", goodReceiveAvailable=" + goodReceive +
                       ", ngReceiveAvailable=" + ngReceive;
            }
            catch (Exception ex)
            {
                return "자동 운전에 필요한 OutputStage 공급 가능 상태를 확인할 수 없습니다. " +
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
            return SequenceTrace.ChildAsync("OutputCassetteSequence", "Loading",
                () => sequence.RunLoadingAsync(ct, BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode)),
                "target=" + target);
        }

        public Task<int> ExecuteCassetteMappingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            return SequenceTrace.ChildAsync("OutputCassetteSequence", "Mapping",
                () => sequence.RunMappingAsync(ct, BuildCassetteOptions(TargetCassette.Good1, bFine, moveTimeoutMs, startMode)));
        }

        public Task<int> ExecuteCassetteUnloadingAsync(CancellationToken ct, TargetCassette target = TargetCassette.Good1, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            return SequenceTrace.ChildAsync("OutputCassetteSequence", "Unloading",
                () => sequence.RunUnloadingAsync(ct, BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode)),
                "target=" + target);
        }

        public Task<int> ExecuteCassetteMoveToSlotAsync(CancellationToken ct, TargetCassette target, int slotIndex, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputCassetteSequence(Context);
            var options = BuildCassetteOptions(target, bFine, moveTimeoutMs, startMode);
            options.SlotIndex = slotIndex;
            return SequenceTrace.ChildAsync("OutputCassetteSequence", "MoveSlot",
                () => sequence.RunMoveSlotAsync(ct, options),
                "target=" + target,
                "slot=" + slotIndex);
        }

        public Task<int> ExecuteStagePrepareLoadAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "PrepareLoad",
                () => sequence.RunPrepareLoadAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteStagePrepareUnloadAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "PrepareUnload",
                () => sequence.RunPrepareUnloadAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
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
            return SequenceTrace.ChildAsync("OutputStageSequence", "ReceiveDie",
                () => sequence.RunReceiveDieAsync(ct, options),
                "grade=" + grade,
                "side=" + options.Side);
        }

        public Task<int> ExecuteStageInspectBinAsync(
            CancellationToken ct,
            BinSide side = BinSide.Good,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "InspectBin",
                () => sequence.RunInspectBinAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteStageMoveAvoidAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "MoveAvoid",
                () => sequence.RunMoveAvoidAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteStageMoveProcessAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputStageSequence(Context);
            return SequenceTrace.ChildAsync("OutputStageSequence", "MoveProcess",
                () => sequence.RunMoveProcessAsync(ct, BuildStageOptions(side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteFeederLoadFromCassetteAsync(CancellationToken ct, int slotIndex, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "LoadFromCassette",
                () => sequence.RunLoadFromCassetteAsync(ct, BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side,
                "slot=" + slotIndex);
        }

        public Task<int> ExecuteFeederLoadFromCassetteAsync(CancellationToken ct, int slotIndex, CassetteMaterialRole cassetteRole, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            return ExecuteFeederLoadFromCassetteAsync(ct, slotIndex, cassetteRole, "", bFine, moveTimeoutMs, startMode);
        }

        private static bool ValidateOutputSupplyConsistency(out string reason)
        {
            string goodReason;
            if (!OutputSlotPlanner.ValidateSupplyCassetteConsistency(BinSide.Good, out goodReason))
            {
                reason = "GOOD 출력 카세트 센서/Material 데이터가 불일치합니다. " + goodReason;
                return false;
            }

            string ngReason;
            if (!OutputSlotPlanner.ValidateSupplyCassetteConsistency(BinSide.Ng, out ngReason))
            {
                reason = "NG 출력 카세트 센서/Material 데이터가 불일치합니다. " + ngReason;
                return false;
            }

            reason = "";
            return true;
        }

        public Task<int> ExecuteFeederLoadFromCassetteAsync(CancellationToken ct, int slotIndex, CassetteMaterialRole cassetteRole, string expectedWaferId, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            BinSide side = cassetteRole == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            var options = BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode);
            options.CassetteRole = cassetteRole;
            options.ExpectedWaferId = string.IsNullOrWhiteSpace(expectedWaferId)
                ? ResolveExpectedOutputWaferId(side, cassetteRole, slotIndex)
                : expectedWaferId;
            return SequenceTrace.ChildAsync("OutputFeederSequence", "LoadFromCassette",
                () => sequence.RunLoadFromCassetteAsync(ct, options),
                "side=" + side,
                "slot=" + slotIndex,
                "cassetteRole=" + cassetteRole);
        }


        public Task<int> ExecuteFeederLoadToStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "LoadToStage",
                () => sequence.RunLoadToStageAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteFeederUnloadFromStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "UnloadFromStage",
                () => sequence.RunUnloadFromStageAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        public Task<int> ExecuteFeederUnloadToCassetteAsync(CancellationToken ct, int slotIndex, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "UnloadToCassette",
                () => sequence.RunUnloadToCassetteAsync(ct, BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side,
                "slot=" + slotIndex);
        }

        public Task<int> ExecuteFeederUnloadToCassetteAsync(CancellationToken ct, int slotIndex, CassetteMaterialRole cassetteRole, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            BinSide side = cassetteRole == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            var options = BuildFeederOptions(slotIndex, slotIndex, side, bFine, moveTimeoutMs, startMode);
            options.CassetteRole = cassetteRole;
            return SequenceTrace.ChildAsync("OutputFeederSequence", "UnloadToCassette",
                () => sequence.RunUnloadToCassetteAsync(ct, options),
                "side=" + side,
                "slot=" + slotIndex,
                "cassetteRole=" + cassetteRole);
        }

        public Task<int> ExecuteRecoverAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            var sequence = new OutputFeederSequence(Context);
            return SequenceTrace.ChildAsync("OutputFeederSequence", "Recover",
                () => sequence.RunRecoverAsync(ct, BuildFeederOptions(0, 0, side, bFine, moveTimeoutMs, startMode)),
                "side=" + side);
        }

        private async Task<int> ExecuteOutputCompletePostureAsync(string holder, BinSide loadedSide, CancellationToken ct, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputCompletePosture" : holder;

            try
            {
                if (loadedSide == BinSide.Good)
                {
                    return await ExecuteWithOutputPickerAvoidGateAsync(safeHolder, ct,
                        () => ExecuteOutputCompletePostureCoreAsync(ct, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                }

                using (SequenceResourceLease goodStageLease = await AcquireOutputStageAreaAsync(BinSide.Good, safeHolder, ct).ConfigureAwait(false))
                {
                    if (goodStageLease == null)
                        return Fail("OUT-RESOURCE-GOOD-STAGE", "OutputSequence",
                            safeHolder + " 중 GoodStage 영역 리소스 점유에 실패했습니다.");

                    return await ExecuteWithOutputPickerAvoidGateAsync(safeHolder, ct,
                        () => ExecuteOutputCompletePostureCoreAsync(ct, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
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
                return Fail("OUT-COMPLETE-POSTURE-EX", "OutputSequence",
                    safeHolder + " 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private Task<int> ExecuteOutputCompletePostureCoreAsync(CancellationToken ct, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            var sequence = new OutputStageSequence(Context);
            OutputStageSequenceOptions options = BuildStageOptions(BinSide.Good, bFine, moveTimeoutMs, startMode);
            // 현재 기준: Output 완료 전 GoodStage는 Process, OutputVisionX는 Avoid 자세로 고정한다.
            options.KeepVisionXAvoidOnProcessMove = true;

            return SequenceTrace.ChildAsync("OutputStageSequence", "CompletePosture",
                () => sequence.RunMoveProcessAsync(ct, options),
                "side=Good",
                "vision=Avoid");
        }

        public async Task<int> ExecuteStoreStageToCassetteAsync(CancellationToken ct, DieGrade grade, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            bool loaderActive = false;
            try
            {
                OutputSlotPlan plan;
                string slotPlanReason;
                if (!OutputSlotPlanner.TryResolveNextStoreSlot(grade, out plan, out slotPlanReason))
                    return Fail("OUT-SLOT-UNAVAILABLE", "OutputSequence", "Output 카세트의 동일 Source Slot을 사용할 수 없습니다. grade=" + grade + ", reason=" + slotPlanReason);

                loaderActive = true;
                SetOutputLoaderActive(loaderActive, "OutputStore");

                using (SequenceResourceLease placeLease = await AcquireOutputPlaceAreaAsync("OutputStore", ct).ConfigureAwait(false))
                {
                    if (placeLease == null)
                        return Fail("OUT-RESOURCE-PLACE", "OutputSequence", "Output Place 영역 리소스 점유에 실패했습니다. side=" + plan.Side);

                    using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(plan.Side, "OutputStore", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "OutputStage 영역 리소스 점유에 실패했습니다. side=" + plan.Side);

                        int result = await ExecuteWithOutputPickerAvoidGateAsync("OutputStore.StagePrepareUnload", ct,
                            () => ExecuteStagePrepareUnloadAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                        if (result != 0) return result;

                        result = await ExecuteWithOutputPickerAvoidGateAsync("OutputStore.FeederUnloadFromStage", ct,
                            () => ExecuteFeederUnloadFromStageAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                        if (result != 0) return result;

                        result = await ExecuteWithOutputPickerAvoidGateAsync("OutputStore.FeederUnloadToCassette", ct,
                            () => ExecuteFeederUnloadToCassetteAsync(ct, plan.SlotIndex, plan.CassetteRole, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                        if (result != 0) return result;

                        result = await ExecuteWithOutputPickerAvoidGateAsync("OutputStore.StageMoveAvoid", ct,
                            () => ExecuteStageMoveAvoidAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
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
                ResetOutputLoaderActive(loaderActive, "OutputStore");
            }
        }

        private async Task<int> ExecuteOutputFeederOccupiedAsync(WaferMaterial feederWafer, CancellationToken ct, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            bool loaderActive = false;
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

                loaderActive = true;
                SetOutputLoaderActive(loaderActive, "OutputFeederResumeLoad");

                using (SequenceResourceLease placeLease = await AcquireOutputPlaceAreaAsync("OutputFeederResumeLoad", ct).ConfigureAwait(false))
                {
                    if (placeLease == null)
                        return Fail("OUT-RESOURCE-PLACE", "OutputSequence", "Output Place 영역 리소스 점유에 실패했습니다. side=" + side);

                    using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(side, "OutputFeederResumeLoad", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "OutputStage 영역 리소스 점유에 실패했습니다. side=" + side);

                        // 기존 조건: 재개 시에도 StagePrepareLoad를 호출 - 피더가 bin을 클램프한 상태라
                        //           Unclamp 인터락으로 스테이지 이동이 차단되어 교착이 발생했다.
                        // int result = await ExecuteWithOutputPickerAvoidGateAsync("OutputFeederResumeLoad.StagePrepareLoad", ct,
                        //     () => ExecuteStagePrepareLoadAsync(ct, side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                        // if (result != 0) return result;
                        // 현재 기준: 순서 규칙(StagePrepareLoad -> 피더 카세트 픽)에 따라 픽이 끝난 재개 시점에는
                        //           스테이지가 이미 Load 위치여야 한다. 클램프 상태에서는 스테이지를 이동하지 않고 검증만 한다.
                        // To do: 오토 재개 교착 해소 - 스테이지 미준비 시 이동 대신 알람으로 복구 유도.
                        OutputStageUnit resumeStage = Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null;
                        if (resumeStage == null)
                            return Fail("OUT-STAGE-MISSING", "OutputSequence", "Output stage unit is not available for feeder resume load.");

                        if (!resumeStage.IsStageInLoadPosition(side))
                            return Fail("OUT-STAGE-LOAD-POS", resumeStage.Name,
                                "재개 시 OutputStage가 Load 위치에 준비되지 않았습니다. 피더가 bin을 클램프한 상태에서는 스테이지를 이동할 수 없으니 복구(빈 반환/언클램프) 후 다시 시작하세요. side=" + side +
                                ", " + resumeStage.DescribeStageLoadMoveState(side));

                        int result = await ExecuteWithOutputPickerAvoidGateAsync("OutputFeederResumeLoad.FeederLoadToStage", ct,
                            () => ExecuteFeederLoadToStageAsync(ct, side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                        if (result != 0) return result;

                        if (side == BinSide.Ng && !CanSupplyOutputStage(BinSide.Good))
                        {
                            result = await ExecuteOutputCompletePostureAsync(
                                "OutputFeederResumeLoad.RestoreGoodProcess",
                                side,
                                ct,
                                bFine,
                                moveTimeoutMs,
                                startMode).ConfigureAwait(false);
                            if (result != 0) return result;
                        }

                    }
                }

                if (!IsAutoOutputLoaderBatchActive)
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
                ResetOutputLoaderActive(loaderActive, "OutputFeederResumeLoad");
            }
        }

        private async Task<int> ExecuteOutputFeederStoreToCassetteAsync(WaferMaterial feederWafer, BinSide side, CancellationToken ct, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            bool loaderActive = false;
            try
            {
                CassetteMaterialRole role;
                TargetCassette target;
                if (!TryResolveOutputCassetteTarget(feederWafer, side, out role, out target))
                    return Fail("OUT-FEEDER-CST-TARGET", "Material", "Output feeder cassette target cannot be resolved. wafer=" + feederWafer.WaferId + ", side=" + side);

                if (feederWafer.SourceSlotNumber < 0)
                    return Fail("OUT-FEEDER-CST-SLOT", "Material", "Output feeder source slot is invalid. wafer=" + feederWafer.WaferId + ", slot=" + feederWafer.SourceSlotNumber);

                loaderActive = true;
                SetOutputLoaderActive(loaderActive, "OutputFeederStoreToCassette");

                int result = await ExecuteWithOutputPickerAvoidGateAsync("OutputFeederStore.FeederUnloadToCassette", ct,
                    () => ExecuteFeederUnloadToCassetteAsync(ct, feederWafer.SourceSlotNumber, role, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
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
                ResetOutputLoaderActive(loaderActive, "OutputFeederStoreToCassette");
            }
        }

        public async Task<int> ExecuteSupplyCassetteToStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            bool loaderActive = false;
            try
            {
                OutputSlotPlan plan;
                string consistencyReason;
                if (!OutputSlotPlanner.ValidateSupplyCassetteConsistency(side, out consistencyReason))
                    return Fail("OUT-SLOT-CONSISTENCY", "OutputSequence", "Output cassette 센서/Material 데이터가 불일치합니다. side=" + side + ", reason=" + consistencyReason);

                string slotPlanReason;
                if (!OutputSlotPlanner.TryResolveNextSupplySlot(side, out plan, out slotPlanReason))
                    return StopAutoSequence("Output cassette has no ready slot. side=" + side + ", reason=" + slotPlanReason);

                loaderActive = true;
                SetOutputLoaderActive(loaderActive, "OutputSupply");

                using (SequenceResourceLease placeLease = await AcquireOutputPlaceAreaAsync("OutputSupply", ct).ConfigureAwait(false))
                {
                    if (placeLease == null)
                        return Fail("OUT-RESOURCE-PLACE", "OutputSequence", "Output Place 영역 리소스 점유에 실패했습니다. side=" + plan.Side);

                    using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(plan.Side, "OutputSupply", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "OutputStage 영역 리소스 점유에 실패했습니다. side=" + plan.Side);

                        int result = await ExecuteWithOutputPickerAvoidGateAsync("OutputSupply.StagePrepareLoad", ct,
                            () => ExecuteStagePrepareLoadAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                        if (result != 0) return result;

                        result = await ExecuteWithOutputPickerAvoidGateAsync("OutputSupply.FeederLoadFromCassette", ct,
                            () => ExecuteFeederLoadFromCassetteAsync(ct, plan.SlotIndex, plan.CassetteRole, plan.WaferId, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                        if (result != 0) return result;

                        result = await ExecuteWithOutputPickerAvoidGateAsync("OutputSupply.FeederLoadToStage", ct,
                            () => ExecuteFeederLoadToStageAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                        if (result != 0) return result;

                        if (plan.Side == BinSide.Ng)
                        {
                            if (CanSupplyOutputStage(BinSide.Good))
                            {
                                Context.LogPublic("[OUTPUT] NG Bin 교체 완료: NG Stage는 Avoid를 유지하고 GOOD Bin 연속 로딩을 진행합니다.");
                            }
                            else
                            {
                                result = await ExecuteOutputCompletePostureAsync(
                                    "OutputSupply.RestoreGoodProcessAfterNgLoad",
                                    plan.Side,
                                    ct,
                                    bFine,
                                    moveTimeoutMs,
                                    startMode).ConfigureAwait(false);
                                if (result != 0) return result;
                            }
                        }

                    }
                }

                if (!IsAutoOutputLoaderBatchActive)
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
                ResetOutputLoaderActive(loaderActive, "OutputSupply");
            }
        }

        private OutputFeederSequenceOptions BuildFeederOptions(int slotIndex, int nextSlotIndex, BinSide side, bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            var options = OutputFeederSequenceOptions.Default();
            options.SlotIndex = slotIndex;
            options.NextSlotIndex = nextSlotIndex;
            options.Side = side;
            options.CassetteRole = side == BinSide.Ng ? CassetteMaterialRole.Ng1 : CassetteMaterialRole.Good1;
            options.ExpectedWaferId = ResolveExpectedOutputWaferId(side, options.CassetteRole, slotIndex);
            options.FineMove = bFine;
            options.MoveTimeoutMs = moveTimeoutMs > 0 ? moveTimeoutMs : options.MoveTimeoutMs;
            options.RunMode = Mode;
            options.StartMode = startMode;
            return options;
        }

        private static string ResolveExpectedOutputWaferId(BinSide side, CassetteMaterialRole cassetteRole, int slotIndex)
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (wafer == null)
            {
                MaterialLocationKind stageLocation = side == BinSide.Ng
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;
                wafer = MaterialStateService.GetWaferAtLocation(stageLocation);
            }
            if (wafer == null && slotIndex >= 0)
                wafer = MaterialStateService.GetWaferInCassette(cassetteRole, slotIndex);
            return wafer != null ? (wafer.WaferId ?? "") : "";
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
                    ct,
                    IsAutoOutputLoaderBatchActive,
                    OutputLoaderBatchDrainReason).ConfigureAwait(false);
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

        private void SetOutputLoaderActive(bool active, string holder)
        {
            if (Mode == SequenceRunMode.Auto)
                return;

            if (!active || Context == null || Context.Bus == null)
                return;

            // 현재 기준: Output 로더 동작 중에는 Picker 공정 신규 진입을 막는다.
            Context.Bus.Set(OutputLoaderActiveSignal);
            WriteLog("OutputLoaderActive", holder + " 시작: Picker 신규 공정 진입을 대기시킵니다. - Set");
        }

        private void ResetOutputLoaderActive(bool active, string holder)
        {
            if (Mode == SequenceRunMode.Auto)
                return;

            if (!active || Context == null || Context.Bus == null)
                return;

            Context.Bus.Reset(OutputLoaderActiveSignal);
            WriteLog("OutputLoaderActive", holder + " 종료: Picker 신규 공정 진입 대기를 해제합니다. - Reset");
        }

        private async Task<int> ExecuteWithOutputPickerAvoidGateAsync(string holder, CancellationToken ct, Func<Task<int>> action)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;

            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await EnsureOutputPickersAvoidBeforeFeederMoveAsync(safeHolder, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                string reason;
                if (!AreOutputPickersAvoidAndStopped(out reason))
                    return Fail("OUT-PICKER-AVOID-STATE", "OutputSequence",
                        safeHolder + " 불가: Front/Rear Picker가 Avoid 정지 상태가 아닙니다. " + reason);

                return await action().ConfigureAwait(false);
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
                return Fail("OUT-PICKER-GATE-EX", "OutputSequence",
                    safeHolder + " Picker Avoid gate 처리 중 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureOutputPickersAvoidBeforeFeederMoveAsync(string holder, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                    {
                        Context.StopIfCycleStopRequested(
                            "OutputSequence.PickerAvoidGate:" + safeHolder,
                            IsAutoOutputLoaderBatchActive || ShouldDeferCycleStopForPickerHeldDieOutputDrain(),
                            OutputLoaderBatchDrainReason);
                    }

                    string reason;
                    if (AreOutputPickersAvoidAndStopped(out reason))
                    {
                        if (waitLogged)
                            WriteLog("OutputPickerAvoidGate", safeHolder + " 전 Picker Avoid 대기 완료. - Ok");
                        return 0;
                    }

                    // 현재 기준: 로더는 Picker를 직접 이동하지 않고 PickerSequence가 Avoid로 빠질 때까지 대기한다.
                    if (Mode != SequenceRunMode.Auto)
                        return Fail("OUT-PICKER-AVOID-STATE", "OutputSequence",
                            safeHolder + " 불가: Front/Rear Picker가 Avoid 정지 상태가 아닙니다. " + reason);

                    if (!waitLogged)
                    {
                        WriteLog("OutputPickerAvoidGate",
                            safeHolder + " 전 Picker Avoid 대기 중입니다. reason=" + reason + " - Wait");
                        if (Context != null)
                            Context.LogPublic("[UNIT-OUTPUT] WAIT Picker Avoid before " + safeHolder + ". " + reason);
                        waitLogged = true;
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
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
                return Fail("OUT-PICKER-AVOID-EX", "OutputSequence",
                    "Output feeder/stage 이동 전 Picker Avoid 처리 중 예외 발생. holder=" + holder +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool AreOutputPickersAvoidAndStopped(out string reason)
        {
            reason = string.Empty;

            var machine = Context != null ? Context.Machine : null;
            var front = machine != null ? machine.PickerFrontUnit : null;
            var rear = machine != null ? machine.PickerRearUnit : null;

            if (IsFrontPickerEnabled(front))
            {
                if (!front.IsFrontPickerInAvoidPosition())
                {
                    reason = "FrontPicker가 Avoid 위치가 아닙니다.";
                    return false;
                }

                string movingReason;
                if (TryDescribeMovingPickerAxes(front.Axes, "FrontPicker", out movingReason))
                {
                    reason = movingReason;
                    return false;
                }
            }

            if (IsRearPickerEnabled(rear))
            {
                if (!rear.IsRearPickerInAvoidPosition())
                {
                    reason = "RearPicker가 Avoid 위치가 아닙니다.";
                    return false;
                }

                string movingReason;
                if (TryDescribeMovingPickerAxes(rear.Axes, "RearPicker", out movingReason))
                {
                    reason = movingReason;
                    return false;
                }
            }

            return true;
        }

        private bool ShouldDeferCycleStopForPickerHeldDieOutputDrain()
        {
            if (Mode != SequenceRunMode.Auto ||
                Context == null ||
                !Context.IsCycleStopRequested)
            {
                return false;
            }

            if (Context.Controller != null && Context.Controller.Status == EquipmentStatus.Alarm)
                return false;

            return HasPickerHeldTargetDieForOutputDrain();
        }

        private bool HasPickerHeldTargetDieForOutputDrain()
        {
            AutoSequenceCoordinatorGate gate = Context != null ? Context.AutoSequenceGate : null;
            bool frontActive = gate == null || gate.IsPickerSideConfiguredForRun(PickerSequenceSide.Front);
            bool rearActive = gate == null || gate.IsPickerSideConfiguredForRun(PickerSequenceSide.Rear);

            return (frontActive && HasInputTargetDieAtPicker(MaterialLocationKind.PickerFront)) ||
                   (rearActive && HasInputTargetDieAtPicker(MaterialLocationKind.PickerRear));
        }

        private static bool HasOutputFeederMaterialForDrain()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder) != null;
        }

        private static bool HasInputTargetDieAtPicker(MaterialLocationKind location)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                if (die != null && die.IsInputTarget)
                    return true;
            }

            return false;
        }

        private static bool IsFrontPickerEnabled(PickerFrontUnit front)
        {
            return front != null && front.Config != null && front.Config.UseUnit;
        }

        private static bool IsRearPickerEnabled(PickerRearUnit rear)
        {
            return rear != null && rear.Config != null && rear.Config.UseUnit;
        }

        private static bool TryDescribeMovingPickerAxes(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<PickerAxis, QMC.Common.Motion.BaseAxis>> axes, string pickerName, out string reason)
        {
            reason = string.Empty;
            if (axes == null)
                return false;

            foreach (var pair in axes)
            {
                if (pair.Value != null && pair.Value.IsMoving)
                {
                    reason = pickerName + " " + pair.Key + " 축이 이동 중입니다.";
                    return true;
                }
            }

            return false;
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
                    ct,
                    IsAutoOutputLoaderBatchActive,
                    OutputLoaderBatchDrainReason).ConfigureAwait(false);
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
                SequenceTrace.StepFail("OutputSequence", source, -1,
                    "alarm=" + alarmCode,
                    "message=" + message);
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
