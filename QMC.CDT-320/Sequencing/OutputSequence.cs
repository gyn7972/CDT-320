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

    // 이 클래스는 partial로 나뉘어 있다(순수 이동, 동작 변경 없음. 2026-07-27):
    //   OutputSequence.cs                  Auto/Step 진입점, 액션 실행 코어, 정지/알람
    //   OutputSequence.ActionPlanner.cs    다음 작업 판정 (모션 없음)
    //   OutputSequence.UnitCalls.cs        Cassette/Stage/Feeder 하위 시퀀스 호출
    //   OutputSequence.Safety.cs           리소스 점유, Picker Avoid 게이트, 인터락 재확인, 옵션 빌더
    public partial class OutputSequence : UnitSequenceBase
    {
        // 하위 시퀀스/액션에서 이미 Fail()로 Alarm을 발생시킨 실패를 상위 계층이 중복 Alarm 없이
        // 전파하기 위한 내부 예외입니다. (규칙: 동일 실패의 중복 Alarm 금지)
        private sealed class StepAlreadyAlarmedException : Exception
        {
            public StepAlreadyAlarmedException(string message)
                : base(message)
            {
            }
        }

        private const string OutputLoaderActiveSignal = "OutputLoaderActive";
        // 무한 대기 진단용: Auto 대기 루프가 무언정지처럼 보이지 않도록 주기적으로 상태를 남기는 간격.
        private const int AutoWaitStatusLogIntervalMs = 30000;
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
                    // 준비 실패 내부의 Fail()이 이미 Alarm을 발생시켰으므로 중복 Alarm 없이 전파한다.
                    throw new StepAlreadyAlarmedException(
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
                        // 액션 내부의 Fail()이 이미 Alarm을 발생시켰으므로 중복 Alarm 없이 전파한다.
                        throw new StepAlreadyAlarmedException(
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
            catch (StepAlreadyAlarmedException ex)
            {
                // 하위 액션에서 이미 Alarm을 발생시킨 실패이므로 로그만 남기고 전파한다.
                WriteLog("ExecuteAutoAsync", "Output 자동 시퀀스가 하위 실패로 중단되었습니다. " + ex.Message + " - Failed");
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
                    // 액션 내부의 Fail()이 이미 Alarm을 발생시켰으므로 중복 Alarm 없이 전파한다.
                    throw new StepAlreadyAlarmedException(
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
            catch (StepAlreadyAlarmedException ex)
            {
                WriteLog("ExecuteStepAsync", "Output 수동/스텝 시퀀스가 하위 실패로 중단되었습니다. " + ex.Message + " - Failed");
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
                    // GYN 2026.07.03 TODO 반영: 재개를 포함한 모든 액션 실행 직전 인터락/자재 정합성 재확인은
                    // 각 작업 진입점(Supply/Store/FeederResume/FeederStore)의
                    // CheckOutputWorkInterlocksBeforeExecute에서 공통 수행한다.
                    // Picker 안전 조건은 각 구간의 ExecuteWithOutputPickerAvoidGateAsync가 확인한다.

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
                if (outputMapped)
                {
                    if (controller != null &&
                        !controller.CompleteOutputFullPreparation(requestRecipeName))
                    {
                        return Fail(
                            "OUT-FULL-PREP-RECIPE-CHANGED",
                            "OutputSequence",
                            "Output 준비 요청 완료 중 활성 Recipe가 변경되었습니다. " +
                            "requestedRecipe=" + requestRecipeName +
                            ", activeRecipe=" + controller.ActiveRecipeName);
                    }

                    Context.LogPublic(
                        "[OUTPUT] 진행 자재와 기존 Mapping을 유지하고 Output 준비 요청만 완료했습니다. reason=" +
                        requestReason);
                    return 0;
                }

                Context.LogPublic(
                    "[OUTPUT] 진행 자재를 유지한 채 Mapping이 무효화된 Output Cassette Side만 다시 Mapping합니다. " +
                    "reason=" + requestReason + ", recipe=" + requestRecipeName);

                string preflightAlarmCode;
                string preflightReason;
                if (!TryValidateRequiredCassetteMappingPreconditions(out preflightAlarmCode, out preflightReason))
                    return Fail(preflightAlarmCode, "OutputSequence", preflightReason);

                int selectiveMappingResult = await ExecuteCoordinatorOutputLoaderWorkAsync(
                    "OutputCassetteSelectiveMapping",
                    ct,
                    () => ExecuteRequiredCassetteMappingsAsync(
                        ct,
                        bFine,
                        moveTimeoutMs,
                        startMode)).ConfigureAwait(false);
                if (selectiveMappingResult != 0)
                    return selectiveMappingResult;

                if (!AreRequiredOutputCassettesMapped())
                {
                    return Fail(
                        "OUT-CST-SELECTIVE-MAP-STATE",
                        "OutputSequence",
                        "진행 자재 유지 상태에서 선택적 Output Cassette Mapping이 완료되지 않았습니다. reason=" +
                        requestReason);
                }

                if (controller != null &&
                    !controller.CompleteOutputFullPreparation(requestRecipeName))
                {
                    return Fail(
                        "OUT-CST-SELECTIVE-MAP-RECIPE-CHANGED",
                        "OutputSequence",
                        "선택적 Output Cassette Mapping 중 활성 Recipe가 변경되어 준비 완료 상태를 확정하지 않았습니다. " +
                        "requestedRecipe=" + requestRecipeName +
                        ", activeRecipe=" + controller.ActiveRecipeName);
                }

                Context.LogPublic(
                    "[OUTPUT] 진행 자재 유지 상태의 선택적 Output Cassette Mapping을 완료했습니다. reason=" +
                    requestReason);
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

                        // [NG 스킵 2026-07-27] 준비 본체(ExecuteFullOutputPreparationCoreAsync)는 UseNgCassette=false면
                        // NG Stage 공급을 의도적으로 건너뛰고 성공(0)으로 돌아온다. 그런데 이 완료 검사만 조건 없이
                        // NG Stage를 요구해서, NG 미사용 장비는 초기/레시피 변경 준비 직후 항상 OUT-FULL-PREP-STAGE로
                        // 실패했다(2026-07-27 현장 발생). 준비 본체와 같은 기준으로 맞춘다.
                        bool ngRequired = IsNgCassetteUsed();
                        if (goodStage == null || (ngRequired && ngStage == null))
                        {
                            return Fail(
                                "OUT-FULL-PREP-STAGE",
                                "OutputSequence",
                                "초기/레시피 변경 Output 전체 준비 후 필요한 Stage가 채워지지 않았습니다. " +
                                "goodStage=" + (goodStage != null ? goodStage.WaferId : "-") +
                                ", ngStage=" + (ngStage != null ? ngStage.WaferId : "-") +
                                ", ngRequired=" + ngRequired +
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

                        // NG 카세트 미사용 구성에서는 ngStage가 정상적으로 null일 수 있으므로 완료 로그도 같은 정책으로 기록한다.
                        Context.LogPublic(
                            "[OUTPUT] GOOD/NG 전체 준비 완료. goodWafer=" + goodStage.WaferId +
                            ", ngWafer=" + (ngStage != null ? ngStage.WaferId : "-") +
                            ", ngRequired=" + ngRequired +
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
            // 현재 기준: 전체 준비 시작 시 NG 사용 여부를 Material 상태에 반영한다. (변경 없으면 no-op)
            SyncNgCassetteEnabledWithConfig();

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
                int mappingResult = await ExecuteRequiredCassetteMappingsAsync(
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

            // 기존 조건: NG Stage가 비어 있으면 NG Ready Bin 공급을 무조건 요구했다(없으면 하드 실패).
            // 현재 기준: Config.UseNgCassette=false면 NG 준비를 건너뛰고 GOOD 준비로 진행한다.
            // To do: [NG 스킵] 전체 준비의 NG 공급 요구를 사용 여부 조건부로 완화.
            if (!IsNgCassetteUsed())
            {
                Context.LogPublic("[OUTPUT] NG 카세트 미사용(UseNgCassette=false) - 전체 준비에서 NG Stage 공급을 건너뜁니다.");
            }
            else if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) == null)
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

        // Manual Sequence 화면의 side 지정 OUTPUT LOAD: 선택한 GOOD/NG side만 별개로 로딩한다.
        // 공급 본체는 Auto와 동일한 ExecuteSupplyCassetteToStageAsync(side)를 사용하므로
        // 액션 인터락 재확인(CheckOutputWorkInterlocksBeforeExecute)도 그대로 적용된다.
        public Task<int> ExecuteManualOutputLoadAsync(
            CancellationToken ct,
            BinSide side,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            // slot이 음수이면 자동 순번 공급이므로 role은 사용되지 않는다.
            return ExecuteManualOutputLoadAsync(ct, side, CassetteMaterialRole.Good1, -1, bFine, moveTimeoutMs, startMode);
        }

        /// <summary>
        /// Manual Sequence OUTPUT LOAD. requestedSlotIndex가 0 이상이면 작업자가 지정한 Bin을 공급하고,
        /// 음수이면 자동 순번으로 공급한다. (UNLOAD는 원본 슬롯 고정이라 지정 대상이 없다.)
        /// </summary>
        public async Task<int> ExecuteManualOutputLoadAsync(
            CancellationToken ct,
            BinSide side,
            CassetteMaterialRole requestedRole,
            int requestedSlotIndex,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (feederWafer != null)
                {
                    BinSide feederSide;
                    if (!TryResolveBinSide(feederWafer, out feederSide))
                        return Fail("OUT-MANUAL-LOAD-FEEDER-SIDE", "OutputSequence",
                            "OutputFeeder Bin의 GOOD/NG를 확인할 수 없습니다. bin=" + (feederWafer.WaferId ?? ""));

                    if (feederSide != side)
                        return Fail("OUT-MANUAL-LOAD-FEEDER-MISMATCH", "OutputSequence",
                            "OutputFeeder에 " + feederSide + " Bin이 남아 있어 " + side + " LOAD를 진행할 수 없습니다. " +
                            feederSide + " side를 먼저 처리하세요. bin=" + (feederWafer.WaferId ?? ""));

                    if (IsOutputBinReceiveComplete(feederWafer))
                        return Fail("OUT-MANUAL-LOAD-FEEDER-COMPLETE", "OutputSequence",
                            "OutputFeeder에 완료된 Bin이 있습니다. OUTPUT UNLOAD를 먼저 실행하세요.");

                    // 로딩 도중 중단된 동일 side Bin은 Auto와 동일한 재개 경로로 Stage 로딩을 이어서 수행한다.
                    return await ExecuteOccupiedFeederActionAsync(
                        ct,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                }

                MaterialLocationKind targetStage = side == BinSide.Ng
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;
                if (MaterialStateService.GetWaferAtLocation(targetStage) != null)
                    return Fail("OUT-MANUAL-LOAD-STAGE-OCCUPIED", "OutputSequence",
                        side + " OutputStage에 이미 Bin이 있습니다. 먼저 OUTPUT UNLOAD로 배출하세요.");

                // CYCLE RUN 자동 순번 LOAD는 Input과 동일하게, 선택 Side의 카세트가 아직
                // Mapping되지 않았고 진행 중 자재/기존 슬롯 데이터가 없을 때 Mapping부터 수행한다.
                // 이미 Mapping된 상태에서 Ready Bin만 없는 경우에는 기존 NO-SLOT 알람을 유지한다.
                if (requestedSlotIndex < 0 && !IsRequiredOutputCassetteSideMapped(side))
                {
                    string mappingBlockedReason;
                    if (IsSelectiveCassetteMappingBlockedByActiveMaterial(side, out mappingBlockedReason))
                    {
                        return Fail("OUT-MANUAL-LOAD-MAP-ACTIVE", "OutputSequence",
                            side + " OUTPUT LOAD 전 Mapping을 시작할 수 없습니다. " + mappingBlockedReason);
                    }

                    if (HasOutputCassetteWaferInfo(side))
                    {
                        return Fail("OUT-MANUAL-LOAD-MAP-DATA", "OutputSequence",
                            side + " 카세트가 미매핑 상태이지만 기존 Bin Material 데이터가 남아 있어 " +
                            "자동 Mapping으로 덮어쓸 수 없습니다.");
                    }

                    Context.LogPublic(
                        "[OUTPUT] CYCLE RUN " + side +
                        " LOAD 전 미매핑 카세트를 먼저 Mapping합니다.");

                    int mappingResult = await ExecuteCassetteMappingForSideAsync(
                        side,
                        ct,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                    if (mappingResult != 0)
                        return mappingResult;

                    if (!IsRequiredOutputCassetteSideMapped(side))
                    {
                        return Fail("OUT-MANUAL-LOAD-MAP-INCOMPLETE", "OutputSequence",
                            side + " OUTPUT LOAD 전 카세트 Mapping이 완료되지 않았습니다.");
                    }
                }

                // 작업자 지정 Bin 공급: 자동 순번과 동일한 Ready/일관성 조건을 통과해야 한다.
                OutputSlotPlan requestedPlan = null;
                if (requestedSlotIndex >= 0)
                {
                    string planReason;
                    if (!OutputSlotPlanner.TryResolveSupplySlot(
                        side, requestedRole, requestedSlotIndex, out requestedPlan, out planReason))
                    {
                        return Fail("OUT-MANUAL-LOAD-SLOT", "OutputSequence",
                            "지정한 Bin을 공급할 수 없습니다. " + planReason);
                    }
                }
                else if (!CanSupplyOutputStage(side))
                {
                    return Fail("OUT-MANUAL-LOAD-NO-SLOT", "OutputSequence",
                        side + " OUTPUT LOAD 가능한 Bin이 없습니다. " + side +
                        " Stage 빈 상태와 카세트 매핑/슬롯 상태를 확인하세요.");
                }

                return await ExecuteSupplyCassetteToStageAsync(
                    ct, side, requestedPlan, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteManualOutputLoadAsync", side + " Output Manual LOAD가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-MANUAL-LOAD-EX", "OutputSequence",
                    side + " Output Manual LOAD 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        // Manual Sequence 화면의 side 지정 OUTPUT UNLOAD: 선택한 GOOD/NG side의 Stage/Feeder Bin만 카세트로 배출한다.
        // 수령 완료 여부와 무관하게 해당 side Stage Bin을 원본 슬롯으로 배출한다(별개 동작 테스트 목적).
        public async Task<int> ExecuteManualOutputUnloadAsync(
            CancellationToken ct,
            BinSide side,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                MaterialLocationKind stageLocation = side == BinSide.Ng
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;
                if (MaterialStateService.GetWaferAtLocation(stageLocation) != null)
                {
                    return await ExecuteCompletedStageStoreAsync(
                        ct,
                        side,
                        side == BinSide.Ng ? DieGrade.Ng : DieGrade.Good,
                        bFine,
                        moveTimeoutMs,
                        startMode).ConfigureAwait(false);
                }

                WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (feederWafer != null)
                {
                    BinSide feederSide;
                    if (TryResolveBinSide(feederWafer, out feederSide) && feederSide == side)
                    {
                        if (IsOutputBinReceiveComplete(feederWafer))
                            return await ExecuteOccupiedFeederActionAsync(
                                ct,
                                bFine,
                                moveTimeoutMs,
                                startMode).ConfigureAwait(false);

                        return Fail("OUT-MANUAL-UNLOAD-FEEDER-INCOMPLETE", "OutputSequence",
                            side + " OutputFeeder에 미완료 Bin이 있습니다. OUTPUT LOAD로 Stage 로딩을 완료한 뒤 처리하세요. bin=" +
                            (feederWafer.WaferId ?? ""));
                    }
                }

                return Fail("OUT-MANUAL-UNLOAD-NO-BIN", "OutputSequence",
                    side + " OUTPUT UNLOAD 가능한 Bin이 없습니다. " + side +
                    " Stage/Feeder Bin 상태를 확인하세요.");
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteManualOutputUnloadAsync", side + " Output Manual UNLOAD가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-MANUAL-UNLOAD-EX", "OutputSequence",
                    side + " Output Manual UNLOAD 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        // To do: [NG 스킵] NG 카세트 사용 여부 - OutputCassette Config.UseNgCassette 파라미터를 단일 기준으로 사용한다.
        private bool IsNgCassetteUsed()
        {
            var cassette = Context != null && Context.Machine != null ? Context.Machine.OutputCassetteUnit : null;
            return cassette == null || cassette.Config == null || cassette.Config.UseNgCassette;
        }

        // To do: [NG 스킵] Ng1 Material의 IsEnabled를 설정 파라미터와 동기화 -
        //        OutputSlotPlanner(공급 후보/일관성/스토어)가 IsEnabled 필터로 NG를 자동 제외하게 된다.
        private void SyncNgCassetteEnabledWithConfig()
        {
            try
            {
                MaterialStateService.SetCassetteEnabled(CassetteMaterialRole.Ng1, IsNgCassetteUsed());
            }
            catch (Exception ex)
            {
                WriteLog("SyncNgCassetteEnabledWithConfig", "NG cassette enabled 동기화 실패: " + ex.Message + " - Failed");
            }
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
                // Picker Place 진행 시간만큼 걸리는 생산 길이 대기이므로 고정 timeout 대신
                // 주기 상태 로그로 무언정지를 진단한다. (탈출은 ct/CycleStop/완료 신호)
                WriteLog("WaitAnyOutputReceiveCompleteAsync",
                    "OutputStage 수령 완료 신호 대기를 시작합니다. - Wait");
                int waitStatusTick = Environment.TickCount;
                while (!ct.IsCancellationRequested)
                {
                    SetOutputStageReadySignals();

                    if (unchecked(Environment.TickCount - waitStatusTick) >= AutoWaitStatusLogIntervalMs)
                    {
                        waitStatusTick = Environment.TickCount;
                        WriteLog("WaitAnyOutputReceiveCompleteAsync",
                            "OutputStage 수령 완료 신호를 대기 중입니다. goodReady=" +
                            (Context.Bus.IsSet("OutputGoodStageReady") ? "Y" : "N") +
                            ", ngReady=" + (Context.Bus.IsSet("OutputNgStageReady") ? "Y" : "N") +
                            ", goodComplete=" + (IsOutputStageCompletionSignalSet(BinSide.Good) ? "Y" : "N") +
                            ", ngComplete=" + (IsOutputStageCompletionSignalSet(BinSide.Ng) ? "Y" : "N") + " - Wait");
                    }

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

                // [NG 스킵 2026-07-27] NG 미사용 장비에 NG 카세트 교체를 요구하지 않는다.
                if (IsNgCassetteUsed())
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

        // [NG 스킵 2026-07-27] static 이면 인스턴스 메서드인 IsNgCassetteUsed()를 부를 수 없어
        // NG 사용 여부 판정이 이 메서드까지 반영되지 못했다. 인스턴스 메서드로 바꾼다.
        // (호출부 2곳 ResolveNextOutputAction / WaitAnyOutputReceiveCompleteAsync 모두 인스턴스 컨텍스트다.)
        private bool IsOutputAutoNoBinWorkComplete()
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

                // NG 미사용(UseNgCassette=false)이면 NG Stage 부재는 정상이다.
                // 이 조건을 걸지 않으면 Ng1 카세트가 disabled 라 canSupplyNg 도 false 가 되어,
                // 자동 운전 시작 직후 곧바로 "출력 카세트 교체 필요"로 빠진다.
                if (IsNgCassetteUsed() && !ngStagePresent && !canSupplyNg)
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

        // [NG 스킵 2026-07-27] 메시지에 ngUsed 를 함께 남기기 위해 인스턴스 메서드로 바꾼다.
        // (호출부 StopOutputAutoNoBinWork 는 인스턴스 컨텍스트다.)
        private string BuildOutputNoBinWorkReason()
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

                bool ngUsed = IsNgCassetteUsed();

                return "자동 운전에 필요한 OutputStage를 유지할 수 없습니다. " +
                       "출력 카세트를 교체하거나 매핑/자재 상태를 확인하세요. " +
                       "ngUsed=" + ngUsed +
                       ", feederPresent=" + feederPresent +
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

        public async Task<int> ExecuteStoreStageToCassetteAsync(CancellationToken ct, DieGrade grade, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            bool loaderActive = false;
            try
            {
                OutputSlotPlan plan;
                string slotPlanReason;
                if (!OutputSlotPlanner.TryResolveNextStoreSlot(grade, out plan, out slotPlanReason))
                    return Fail("OUT-SLOT-UNAVAILABLE", "OutputSequence", "Output 카세트의 동일 Source Slot을 사용할 수 없습니다. grade=" + grade + ", reason=" + slotPlanReason);

                // 배출 시작 직전 인터락/자재 정합성 재확인: 대상 Stage에 Bin이 있고 Feeder는 비어 있어야 한다.
                int interlockResult = CheckOutputWorkInterlocksBeforeExecute(
                    "OutputStore(" + plan.Side + ")", plan.Side, false, true);
                if (interlockResult != 0)
                    return interlockResult;

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

                        // 연속 이송: 같은 Loader 배치에서 동일 Side 재공급이 이어질 수 있으면 카세트 리프터를
                        // Avoid로 되돌리지 않고 슬롯 -> 슬롯 직행으로 진행한다.
                        // 연속 공급이 확정되지 않으면 아래 else 경로에서 리프터를 Avoid로 복구한다.
                        bool keepCassetteForChain = CanChainOutputCassetteSupply(plan.Side);
                        result = await ExecuteWithOutputPickerAvoidGateAsync("OutputStore.FeederUnloadToCassette", ct,
                            () => ExecuteFeederUnloadToCassetteWithHeldResourcesAsync(
                                ct,
                                plan.SlotIndex,
                                plan.CassetteRole,
                                placeLease,
                                lease,
                                bFine,
                                moveTimeoutMs,
                                startMode,
                                keepCassetteForChain)).ConfigureAwait(false);
                        if (result != 0) return result;

                        OutputSequenceAutoAction nextAction;
                        OutputSlotPlan immediateSupplyPlan;
                        string immediateSupplyReason;
                        if (TryResolveImmediateSameSideSupply(
                            plan.Side,
                            out nextAction,
                            out immediateSupplyPlan,
                            out immediateSupplyReason))
                        {
                            WriteLog("OutputStore.StageMoveAvoid",
                                "동일 Side 즉시 재공급을 확정하여 중간 Stage MoveAvoid를 생략하고 " +
                                "현재 Stage/Place 리소스 점유 안에서 Supply를 연속 실행합니다. " +
                                "side=" + plan.Side + ", nextAction=" + nextAction +
                                ", slot=" + immediateSupplyPlan.SlotIndex +
                                ", cassetteKeptAtSlot=" + keepCassetteForChain + " - Start");

                            result = await ExecuteImmediateSameSideSupplyOrMoveAvoidWithHeldResourcesAsync(
                                immediateSupplyPlan,
                                "OutputStore",
                                ct,
                                bFine,
                                moveTimeoutMs,
                                startMode).ConfigureAwait(false);
                            if (result != 0) return result;
                        }
                        else
                        {
                            WriteLog("OutputStore.StageMoveAvoid",
                                "동일 Side 즉시 재공급이 확정되지 않아 Stage MoveAvoid를 실행합니다. " +
                                "side=" + plan.Side + ", nextAction=" + nextAction +
                                ", reason=" + immediateSupplyReason + " - Check");

                            // 연속 공급이 취소되었으므로 lease를 놓기 전에 카세트 리프터를 Avoid로 복구한다.
                            result = await EnsureOutputCassetteAvoidAsync("OutputStore.CassetteAvoidRestore", ct).ConfigureAwait(false);
                            if (result != 0) return result;
                            result = await ExecuteWithOutputPickerAvoidGateAsync("OutputStore.StageMoveAvoid", ct,
                                () => ExecuteStageMoveAvoidAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                            if (result != 0) return result;
                        }
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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

                // 잔류 Bin 재개 직전 인터락 재확인: Feeder에 Bin이 있어야 하며 축 상태가 정상이어야 한다.
                int interlockResult = CheckOutputWorkInterlocksBeforeExecute(
                    "OutputFeederResume(" + side + ")", side, true, null);
                if (interlockResult != 0)
                    return interlockResult;

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
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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

                // 완료 Bin 카세트 복귀 직전 인터락 재확인: Feeder에 Bin이 있어야 하며 축 상태가 정상이어야 한다.
                int interlockResult = CheckOutputWorkInterlocksBeforeExecute(
                    "OutputFeederStore(" + side + ")", side, true, null);
                if (interlockResult != 0)
                    return interlockResult;

                loaderActive = true;
                SetOutputLoaderActive(loaderActive, "OutputFeederStoreToCassette");

                // 정상 Store 경로(ExecuteStoreStageToCassetteAsync)와 동일하게 OutputPlaceArea + 측 OutputStageArea 락을
                // Feeder -> Cassette 이송 전 구간 동안 보유한다. 이 락이 없으면 카세트 리프터/피더Y/언클램프가 수초간 움직이는
                // 사이 OutputPostPlaceInspectionQueue나 PickerPlace가 비어 있는 Place/StageArea를 점유해 OutputVisionX/Picker를
                // Output 존으로 진입시킬 수 있다. (H-01: 피더 잔류 완료품 재개 저장 경로 보호 누락)
                using (SequenceResourceLease placeLease = await AcquireOutputPlaceAreaAsync("OutputFeederStoreToCassette", ct).ConfigureAwait(false))
                {
                    if (placeLease == null)
                        return Fail("OUT-RESOURCE-PLACE", "OutputSequence", "Output Place 영역 리소스 점유에 실패했습니다. side=" + side);

                    using (SequenceResourceLease lease = await AcquireOutputStageAreaAsync(side, "OutputFeederStoreToCassette", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("OUT-RESOURCE-STAGE", "OutputSequence", "OutputStage 영역 리소스 점유에 실패했습니다. side=" + side);

                        int result = await ExecuteWithOutputPickerAvoidGateAsync("OutputFeederStore.FeederUnloadToCassette", ct,
                            () => ExecuteFeederUnloadToCassetteWithHeldResourcesAsync(
                                ct,
                                feederWafer.SourceSlotNumber,
                                role,
                                placeLease,
                                lease,
                                bFine,
                                moveTimeoutMs,
                                startMode)).ConfigureAwait(false);
                        if (result != 0) return result;

                        Context.Bus.Reset(side == BinSide.Ng ? "OutputNgStageReceiveComplete" : "OutputGoodStageReceiveComplete");

                        OutputSequenceAutoAction nextAction;
                        OutputSlotPlan immediateSupplyPlan;
                        string immediateSupplyReason;
                        if (TryResolveImmediateSameSideSupply(
                            side,
                            out nextAction,
                            out immediateSupplyPlan,
                            out immediateSupplyReason))
                        {
                            WriteLog("OutputFeederStore.StageMoveAvoid",
                                "완료품 Feeder 재개 저장 후 동일 Side 즉시 재공급을 확정하여 중간 Stage MoveAvoid를 " +
                                "생략하고 현재 Stage/Place 리소스 점유 안에서 Supply를 연속 실행합니다. " +
                                "side=" + side + ", nextAction=" + nextAction +
                                ", slot=" + immediateSupplyPlan.SlotIndex + " - Start");

                            result = await ExecuteImmediateSameSideSupplyOrMoveAvoidWithHeldResourcesAsync(
                                immediateSupplyPlan,
                                "OutputFeederStore",
                                ct,
                                bFine,
                                moveTimeoutMs,
                                startMode).ConfigureAwait(false);
                            if (result != 0) return result;
                        }
                        else
                        {
                            WriteLog("OutputFeederStore.StageMoveAvoid",
                                "완료품 Feeder 재개 저장 후 동일 Side 즉시 재공급이 확정되지 않아 Stage MoveAvoid를 실행합니다. " +
                                "side=" + side + ", nextAction=" + nextAction +
                                ", reason=" + immediateSupplyReason + " - Check");
                            result = await ExecuteWithOutputPickerAvoidGateAsync("OutputFeederStore.StageMoveAvoid", ct,
                                () => ExecuteStageMoveAvoidAsync(ct, side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
                            if (result != 0) return result;
                        }
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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

        public Task<int> ExecuteSupplyCassetteToStageAsync(CancellationToken ct, BinSide side = BinSide.Good, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            return ExecuteSupplyCassetteToStageAsync(ct, side, null, bFine, moveTimeoutMs, startMode);
        }

        /// <summary>
        /// Output 카세트 -> Stage 공급. requestedPlan이 있으면 작업자가 지정한 Bin을 공급하고,
        /// 없으면 자동 순번(TryResolveNextSupplySlot)으로 결정한다.
        /// </summary>
        public async Task<int> ExecuteSupplyCassetteToStageAsync(
            CancellationToken ct,
            BinSide side,
            OutputSlotPlan requestedPlan,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            bool loaderActive = false;
            try
            {
                OutputSlotPlan plan;
                string consistencyReason;
                if (!OutputSlotPlanner.ValidateSupplyCassetteConsistency(side, out consistencyReason))
                    return Fail("OUT-SLOT-CONSISTENCY", "OutputSequence", "Output cassette 센서/Material 데이터가 불일치합니다. side=" + side + ", reason=" + consistencyReason);

                string slotPlanReason;
                if (requestedPlan != null)
                {
                    // 작업자가 지정한 Bin. 자동 순번과 동일한 Ready/일관성 조건을 이미 통과한 계획이다.
                    plan = requestedPlan;
                    Context.LogPublic("[OUTPUT] 지정 Bin 공급을 실행합니다. side=" + plan.Side +
                        ", role=" + plan.CassetteRole +
                        ", slot=" + (plan.SlotIndex + 1).ToString("00") +
                        ", bin=" + (plan.WaferId ?? ""));
                }
                else if (!OutputSlotPlanner.TryResolveNextSupplySlot(side, out plan, out slotPlanReason))
                {
                    return StopAutoSequence("Output cassette has no ready slot. side=" + side + ", reason=" + slotPlanReason);
                }

                // 공급 시작 직전 인터락/자재 정합성 재확인: Feeder와 대상 Stage가 모두 비어 있어야 한다.
                int interlockResult = CheckOutputWorkInterlocksBeforeExecute(
                    "OutputSupply(" + plan.Side + ")", plan.Side, false, false);
                if (interlockResult != 0)
                    return interlockResult;

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

                        int result = await ExecuteSupplyCassetteToStageWithHeldResourcesAsync(
                            plan,
                            ct,
                            bFine,
                            moveTimeoutMs,
                            startMode).ConfigureAwait(false);
                        if (result != 0) return result;
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

        private async Task<int> ExecuteSupplyCassetteToStageWithHeldResourcesAsync(
            OutputSlotPlan plan,
            CancellationToken ct,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            if (plan == null)
                return Fail("OUT-SUPPLY-PLAN-MISSING", "OutputSequence",
                    "Output 공급 계획이 없어 동일 리소스 점유 내 Supply를 실행할 수 없습니다.");

            int result = await ExecuteWithOutputPickerAvoidGateAsync("OutputSupply.StagePrepareLoad", ct,
                () => ExecuteStagePrepareLoadAsync(ct, plan.Side, bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
            if (result != 0) return result;

            result = await ExecuteWithOutputPickerAvoidGateAsync("OutputSupply.FeederLoadFromCassette", ct,
                () => ExecuteFeederLoadFromCassetteAsync(
                    ct,
                    plan.SlotIndex,
                    plan.CassetteRole,
                    plan.WaferId,
                    bFine,
                    moveTimeoutMs,
                    startMode)).ConfigureAwait(false);
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

            return 0;
        }

        private async Task<int> ExecuteImmediateSameSideSupplyOrMoveAvoidWithHeldResourcesAsync(
            OutputSlotPlan reservedPlan,
            string holder,
            CancellationToken ct,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputImmediateSupply" : holder;
            OutputSequenceAutoAction latestAction = OutputSequenceAutoAction.None;
            OutputSlotPlan latestPlan = null;
            string latestReason = string.Empty;
            if (reservedPlan == null ||
                !TryResolveImmediateSameSideSupply(
                    reservedPlan.Side,
                    out latestAction,
                    out latestPlan,
                    out latestReason) ||
                !IsSameOutputSlotPlan(reservedPlan, latestPlan))
            {
                WriteLog(safeHolder + ".ImmediateSupply",
                    "실제 Supply 시작 직전 계획/Drain 상태 재검증이 실패하여 Stage MoveAvoid로 전환합니다. " +
                    "reserved=" + DescribeOutputSlotPlan(reservedPlan) +
                    ", latest=" + DescribeOutputSlotPlan(latestPlan) +
                    ", nextAction=" + latestAction +
                    ", reason=" + latestReason + " - Check");

                // 연속 공급이 취소되었으므로 lease를 놓기 전에 카세트 리프터를 Avoid로 복구한다.
                // (직전 언로드에서 연속 이송을 위해 Avoid 복귀를 생략했을 수 있다.)
                int cassetteRestore = await EnsureOutputCassetteAvoidAsync(
                    safeHolder + ".CassetteAvoidRestore", ct).ConfigureAwait(false);
                if (cassetteRestore != 0)
                    return cassetteRestore;

                return await ExecuteWithOutputPickerAvoidGateAsync(
                    safeHolder + ".StageMoveAvoidAfterRecheck",
                    ct,
                    () => ExecuteStageMoveAvoidAsync(
                        ct,
                        reservedPlan != null ? reservedPlan.Side : BinSide.Good,
                        bFine,
                        moveTimeoutMs,
                        startMode)).ConfigureAwait(false);
            }

            return await ExecuteSupplyCassetteToStageWithHeldResourcesAsync(
                latestPlan,
                ct,
                bFine,
                moveTimeoutMs,
                startMode).ConfigureAwait(false);
        }

        // 연속 이송 가능 판정: 배출 직후 같은 Loader 배치 안에서 동일 Side 재공급이 이어질 수 있는지 본다.
        // 조건을 만족하지 못하면 기존처럼 카세트 리프터를 Avoid로 되돌린다.
        private bool CanChainOutputCassetteSupply(BinSide side)
        {
            try
            {
                // Auto 배치(단일 Loader lease) 안에서만 사용한다. Manual/Step은 한 동작 단위로 끝나야 한다.
                if (!IsAutoOutputLoaderBatchActive)
                    return false;

                if (Context == null || Context.IsCycleStopRequested)
                    return false;

                if (IsStopAfterDrainRequested())
                    return false;

                // 같은 side에 공급 가능한 Ready Bin이 있어야 한다. (Stage 점유 여부와 무관한 조회)
                OutputSlotPlan plan;
                return OutputSlotPlanner.TryResolveNextSupplySlot(side, out plan);
            }
            catch (Exception ex)
            {
                WriteLog("CanChainOutputCassetteSupply",
                    "연속 이송 가능 판정 중 예외가 발생해 기존 Avoid 복귀 경로를 사용합니다. error=" + ex.Message + " - Check");
                return false;
            }
            finally
            {
            }
        }

        // 카세트 리프터를 Avoid로 복구한다. 이미 Avoid면 아무 동작도 하지 않는다.
        // Picker X 이동이 리프터 Avoid를 요구하므로, Loader lease를 놓기 전에 반드시 이 상태를 만든다.
        private async Task<int> EnsureOutputCassetteAvoidAsync(string holder, CancellationToken ct)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "OutputSequence" : holder;

            try
            {
                ct.ThrowIfCancellationRequested();

                OutputCassetteUnit cassette = Context != null && Context.Machine != null
                    ? Context.Machine.OutputCassetteUnit
                    : null;
                if (cassette == null || cassette.OutputLifterZ == null || cassette.Recipe == null)
                    return Fail("OUT-CST-AVOID-MISSING", "OutputCassette",
                        safeHolder + " 중 OutputCassette 유닛/축/레시피를 확인할 수 없어 리프터 Avoid 복구를 할 수 없습니다.");

                if (!cassette.OutputLifterZ.IsMoving && cassette.IsBinLifterZInAvoidPosition())
                    return 0;

                // 기구 간섭 방지: 리프터 이동 전 OutputFeeder가 정지된 실제 Avoid 위치여야 한다.
                OutputFeederUnit feeder = Context.Machine.OutputFeederUnit;
                if (feeder == null ||
                    feeder.FeederY == null ||
                    feeder.FeederY.IsMoving ||
                    !feeder.IsBinFeederAvoidPositionCheck())
                {
                    return Fail("OUT-CST-AVOID-FEEDER", "OutputCassette",
                        safeHolder + " 중 카세트 리프터 Avoid 복구 불가: OutputFeeder가 정지된 Avoid 위치가 아닙니다. " +
                        (feeder != null ? feeder.DescribeFeederCylinderState() : "OutputFeeder=null"));
                }

                double target = cassette.Recipe.AvoidPosition;
                int result = await cassette.MoveBinLifterZ(target, false, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-CST-AVOID-MOVE", cassette.Name,
                        safeHolder + " 중 카세트 리프터 Avoid 복구 이동 실패. result=" + result + ", " +
                        cassette.DescribeOutputLifterZState(target));

                if (!cassette.IsBinLifterZInAvoidPosition())
                    return Fail("OUT-CST-AVOID-CHECK", cassette.Name,
                        safeHolder + " 중 카세트 리프터 Avoid 복구 최종 확인 실패. " +
                        cassette.DescribeOutputLifterZState(target));

                WriteLog("EnsureOutputCassetteAvoidAsync",
                    safeHolder + ": 연속 공급이 취소되어 카세트 리프터를 Avoid로 복구했습니다. " +
                    cassette.DescribeOutputLifterZState(target) + " - Ok");
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
                return Fail("OUT-CST-AVOID-EX", "OutputCassette",
                    safeHolder + " 중 카세트 리프터 Avoid 복구 처리에서 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsSameOutputSlotPlan(OutputSlotPlan expected, OutputSlotPlan actual)
        {
            return expected != null &&
                   actual != null &&
                   expected.Side == actual.Side &&
                   expected.CassetteRole == actual.CassetteRole &&
                   expected.TargetCassette == actual.TargetCassette &&
                   expected.SlotIndex == actual.SlotIndex &&
                   string.Equals(expected.WaferId, actual.WaferId, StringComparison.OrdinalIgnoreCase);
        }

        private static string DescribeOutputSlotPlan(OutputSlotPlan plan)
        {
            if (plan == null)
                return "null";

            return plan.Side + "/" + plan.CassetteRole + "/slot=" + plan.SlotIndex +
                   "/wafer=" + (plan.WaferId ?? string.Empty);
        }

        private OutputFeederSequenceOptions BuildFeederOptions(
            int slotIndex,
            int nextSlotIndex,
            BinSide side,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode,
            bool keepCassetteAtSlotForNextAccess = false)
        {
            var options = OutputFeederSequenceOptions.Default();
            options.KeepCassetteAtSlotForNextAccess = keepCassetteAtSlotForNextAccess;
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
