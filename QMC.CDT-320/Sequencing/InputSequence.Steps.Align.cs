using System.Threading;
using System.Threading.Tasks;
using System;
using QMC.CDT320.Recipes;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    // Input 자동 시퀀스 정렬 구간 스텝 [7]~[8].
    // AlignStage -> DieMapping
    //
    // 이 파일은 InputSequence.ExecuteCurrentInputStepAsync의 switch에서 순수 추출한 것이다(2026-07-27).
    public partial class InputSequence
    {
        // [7] AlignStage: InputStageArea를 점유하고 wafer align을 수행한다.
        private async Task<int> ExecuteStepAlignStageAsync(CancellationToken ct, bool requireVisionAlign)
        {
            // [P2 2026-08-22 재시작 바코드 게이트] 재개 판정(ResolveStageWaferResumeStep)은 자재 위치만
            // 보고 AlignStage로 직행하므로, 이적재 완료~바코드 완료 사이에서 알람이 났던 웨이퍼는
            // 바코드가 영구 누락된다. 바코드 사용 모드에서 스테이지 웨이퍼가 "유효 바코드 없음"이면
            // Align 전에 판독부터 수행한다(기존 이적재 후반부 스텝 재사용 — 판독 코어 복제 없음).
            // [검토수정 2026-08-22] 재개는 DieMapping/ReviewStage로도 직행할 수 있어 그 진입부에도 같은
            // 게이트를 건다(Complete 재개는 DieMapping으로 강등되므로 3지점이면 전 경로 커버).
            int barcodeGate = await EnsureStageBarcodeBeforeStageWorkAsync("AlignStage", ct).ConfigureAwait(false);
            if (barcodeGate != 0)
                return barcodeGate;

            SequenceStartMode alignStartMode = _restartAlignFromReview
                ? SequenceStartMode.Restart
                : SequenceStartMode.Resume;
            int result = await ExecuteWithInputPickerAvoidGateAsync("InputAlign", ct, async () =>
            {
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputAlign", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Align 중 InputStageArea 리소스 점유에 실패했습니다.");

                    // requireVisionAlign이 true이면 StageSequence 내부에서 vision align 조건을 함께 요구한다.
                    var stageSequence = new InputStageSequence(Context);
                    int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "Align",
                        () => stageSequence.RunAlignAsync(
                            ct,
                            BuildStageSequenceOptions(false, alignStartMode, requireVisionAlign, _autoWaferId, false)),
                        "wafer=" + _autoWaferId,
                        "requireVisionAlign=" + requireVisionAlign,
                        "startMode=" + alignStartMode).ConfigureAwait(false);
                    if (stageResult != 0)
                        return Fail("SEQ-IN-STEP-STAGE-ALIGN", "InputStage",
                            "InputStage align 실패. result=" + stageResult);
                }

                return 0;
            }).ConfigureAwait(false);
            if (result != 0)
                return result;

            _restartAlignFromReview = false;
            _autoStep = InputSequenceAutoStep.DieMapping;
            return 0;
        }

        // [P2 2026-08-22] 재시작 바코드 게이트 판정: 바코드 사용 모드 + 스테이지 웨이퍼 존재 +
        // 유효 바코드 없음이면 true. 조건이 아니면 어떤 부작용도 없다.
        // [검토수정 2026-08-22] 통과 조건은 "유효 판독값(BarcodeConfirmed + 사용 가능)"이다 —
        // BarcodeSequencePerformed까지 AND로 요구하면, 판독은 됐는데 플래그만 없는 자재(구버전 상태
        // 파일)가 영구 재발동에 걸린다. 유효 바코드는 역사적으로 바코드 시퀀스만 만들 수 있으므로
        // usable이면 수행된 것이다. performed는 판정이 아니라 기록/로그용으로 쓴다.
        private bool IsStageBarcodeRecoveryRequired(out WaferMaterial stageWafer, out string detail)
        {
            stageWafer = null;
            detail = string.Empty;

            AppSettings settings = AppSettingsStore.Current;
            InputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
            bool prefixRequired = stage != null && stage.Config != null && stage.Config.UseBarcodeLotPrefixCheck;
            if (settings != null && !settings.UseInputWaferBarcode && !RecipeInputMapSource.UsesRemoteForActiveRecipe(settings) && !prefixRequired)
                return false;

            stageWafer = ResolveStageWaferFromRuntimeState();
            if (stageWafer == null)
                return false;

            string validationReason = string.Empty;
            bool usable = stageWafer.BarcodeConfirmed &&
                          InputFeederLoadToStageSequence.TryValidateInputBarcodePolicy(stage, stageWafer.BarcodeId, out validationReason);
            if (usable)
                return false;

            detail = "sequencePerformed=" + stageWafer.BarcodeSequencePerformed +
                     ", barcodeConfirmed=" + stageWafer.BarcodeConfirmed +
                     ", barcodeId=" + (string.IsNullOrWhiteSpace(stageWafer.BarcodeId) ? "-" : stageWafer.BarcodeId) +
                     ", waferId=" + (stageWafer.WaferId ?? "-") +
                     ", instance=" + (stageWafer.WaferInstanceId ?? "-") +
                     ", validation=" + validationReason;
            return true;
        }

        // [2차 검토수정 2026-08-23] 직전 게이트 호출에서 실제 복구(판독 모션)가 수행됐는지 —
        // Review 진입부가 이 값을 보고 Align부터 재실행한다(복구 꼬리가 니들/스테이지를 공정 자세로
        // 바꾸므로, 정렬을 다시 돌려 자세·Hybrid 세션·리뷰 표시를 전부 재정립하는 것이 안전하다).
        private bool _stageBarcodeRecoveryPerformedAtLastGate;

        // [P2 2026-08-22] 스테이지 작업(Align/DieMapping/Review) 진입 전 바코드 보강 실행. 정상 이적재와
        // 동일하게 픽커 회피 게이트 + InputStageArea 점유 아래에서 이적재 후반부(CheckUnit 검증 경유 →
        // VerifyInputStageData부터)를 재사용한다 — 피더 안전 확인/소프트리밋 검증은 그 스텝들 안에 있다.
        // [검토수정 2026-08-22] Auto 전용 — 수동/STEP 실행에서 예고 없는 복구 모션을 만들지 않는다.
        private async Task<int> EnsureStageBarcodeBeforeStageWorkAsync(string entryPoint, CancellationToken ct)
        {
            _stageBarcodeRecoveryPerformedAtLastGate = false;

            if (Mode != SequenceRunMode.Auto)
                return 0;

            WaferMaterial stageWafer;
            string gateDetail;
            bool recoveryRequired = IsStageBarcodeRecoveryRequired(out stageWafer, out gateDetail);
            if (stageWafer == null)
                return 0;

            // 정상 재개는 Mapping/Review에서 네트워크를 다시 읽지 않는다. 맵 사전 검사는 얼라인 진입에서 끝낸다.
            if (!recoveryRequired && !string.Equals(entryPoint, "AlignStage", StringComparison.Ordinal))
                return 0;
            if (recoveryRequired &&
                (!string.Equals(entryPoint, "AlignStage", StringComparison.Ordinal) ||
                 InputFeederLoadToStageSequence.HasInputBarcodeProcessingStarted(stageWafer)))
                return Fail("SEQ-IN-BARCODE-AFTER-ALIGN", "InputStage",
                    "얼라인/맵 생성/픽업 이후 바코드 오류는 이 단계에서 정정할 수 없습니다. " + gateDetail);

            // 이미 판독한 후보는 얼라인 전에 값/맵만 다시 확인한다. 수동 정정으로 바코드 위치 모션을 반복하지 않는다.
            if (stageWafer.BarcodeConfirmed)
            {
                try
                {
                    return await ExecuteWithInputPickerAvoidGateAsync("InputBarcodeValidation", ct, async () =>
                    {
                        using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputBarcodeValidation", ct).ConfigureAwait(false))
                        {
                            if (lease == null)
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "얼라인 전 바코드 검사 중 InputStageArea 점유에 실패했습니다.");
                            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
                            WaferMaterial validated = await InputFeederLoadToStageSequence.ValidateAndApplyInputBarcodeAsync(
                                stage, stageWafer, stageWafer.BarcodeId, "InputSequence:BeforeAlign", stageWafer.BarcodeAttemptCount, ct).ConfigureAwait(false);
                            ct.ThrowIfCancellationRequested();
                            _autoWaferId = validated.WaferId;
                            return 0;
                        }
                    }).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    return Fail("SEQ-IN-BARCODE-VALIDATION", "InputStage", "얼라인 전 바코드 확인 실패. " + ex.Message);
                }
            }

            gateDetail = "entry=" + entryPoint + ", " + gateDetail;

            // 최소 로그 정책에서도 남도록 레벨 지정 로그 사용(가시성 — P2 지시서 §3-B-3).
            QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "InputSequence",
                "재시작 바코드 게이트 발동 — Align 전에 바코드 시퀀스를 먼저 수행합니다. " + gateDetail + " - Start");

            int result = await ExecuteWithInputPickerAvoidGateAsync("InputStageBarcodeRecovery", ct, async () =>
            {
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputStageBarcodeRecovery", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence",
                            "재시작 바코드 판독 중 InputStageArea 리소스 점유에 실패했습니다.");

                    int slotIndex = stageWafer.SourceSlotNumber >= 0 ? stageWafer.SourceSlotNumber : 0;
                    var feederSequence = new InputFeederSequence(Context);
                    InputFeederSequenceOptions feederOptions =
                        BuildFeederSequenceOptions(slotIndex, slotIndex, false, 0, SequenceStartMode.Restart);
                    int feederResult = await SequenceTrace.ChildAsync("InputFeederSequence", "StageBarcodeRecovery",
                        () => feederSequence.RunStageBarcodeRecoveryAsync(ct, feederOptions),
                        "slot=" + slotIndex).ConfigureAwait(false);
                    if (feederResult != 0)
                        return Fail("SEQ-IN-STEP-STAGE-BARCODE", "InputFeeder",
                            "재시작 바코드 판독 실패. result=" + feederResult);

                    // 바코드로 WaferId가 승격됐으므로 이후 Align/Mapping 로그가 임시 ID를 쓰지 않게 재동기화(Load 스텝 미러).
                    WaferMaterial refreshed = ResolveStageWaferFromRuntimeState();
                    if (refreshed == null || string.IsNullOrWhiteSpace(refreshed.WaferId))
                        return Fail("SEQ-IN-STAGE-WAFER-ID", "Material",
                            "재시작 바코드 판독 완료 후 Stage WaferId를 확인할 수 없습니다.");

                    _autoWaferId = refreshed.WaferId;
                    return 0;
                }
            }).ConfigureAwait(false);

            if (result == 0)
            {
                _stageBarcodeRecoveryPerformedAtLastGate = true;
                QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "InputSequence",
                    "재시작 바코드 게이트 완료. entry=" + entryPoint + ", waferId=" + (_autoWaferId ?? "-") + " - Ok");
            }

            return result;
        }

        // [2차 검토수정 2026-08-23] 바코드 복구가 실제 수행된 뒤 정렬부터 재실행한다 — 복구 꼬리
        // (MoveInputStageProcessPosition)가 니들 X/Z를 Process로 올리고 StageT를 공칭값으로 되돌리므로,
        // 리뷰(수동 세션)로 직행하면 자세 불일치로 조그 인터락에 걸리고 세타 보정 표시도 어긋난다.
        // 기존 리뷰발(發) 재정렬 북키핑(RestartWaferAlignAfterMissingDieMapPoints)과 동일한 방식을 쓴다.
        private void RestartWaferAlignAfterBarcodeRecovery(string entryPoint)
        {
            _restartAlignFromReview = true;
            _restartDieMappingFromReview = true;
            SequenceResumeStore.Clear(InputStageDieMappingSequenceStateName);
            SequenceResumeStore.Clear(InputStageAlignSequenceStateName);
            _autoStep = InputSequenceAutoStep.AlignStage;

            QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "InputSequence",
                "재시작 바코드 복구 후 웨이퍼 얼라인부터 다시 시작합니다. entry=" + entryPoint +
                ", wafer=" + (_autoWaferId ?? "-") + " - Restart");
        }

        // [8] DieMapping: Align 결과를 기반으로 Stage wafer의 die map 정보를 생성한다.
        private async Task<int> ExecuteStepDieMappingAsync(CancellationToken ct, bool requireVisionAlign)
        {
            // [검토수정 2026-08-22] 재개가 DieMapping으로 직행(정렬 결과 보유)한 웨이퍼도 바코드 게이트 커버.
            int dieMappingBarcodeGate = await EnsureStageBarcodeBeforeStageWorkAsync("DieMapping", ct).ConfigureAwait(false);
            if (dieMappingBarcodeGate != 0)
                return dieMappingBarcodeGate;

            SequenceStartMode mappingStartMode = _restartDieMappingFromReview
                ? SequenceStartMode.Restart
                : SequenceStartMode.Resume;
            string dieMappingResumeStep;
            if (mappingStartMode == SequenceStartMode.Resume &&
                ShouldRestartWaferAlignForDieMappingResume(out dieMappingResumeStep))
            {
                // Align부터 다시 하도록 _autoStep을 되돌린다. 이번 스텝은 여기서 끝낸다.
                RestartWaferAlignAfterMissingDieMapPoints(dieMappingResumeStep);
                return 0;
            }

            int result = await ExecuteWithInputPickerAvoidGateAsync("InputDieMapping", ct, async () =>
            {
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputDieMapping", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Die mapping 중 InputStageArea 리소스 점유에 실패했습니다.");

                    InputStageSequenceOptions mappingOptions =
                        BuildStageSequenceOptions(false, mappingStartMode, requireVisionAlign, _autoWaferId, false);
                    // Auto에서는 사용자 리뷰 승인 전 Picker Ready 신호를 발행하지 않는다.
                    mappingOptions.PublishReadySignals = false;

                    // DieMapping이 끝나면 MaterialStateService의 Stage finish 조건이 만족되어야 한다.
                    var stageSequence = new InputStageSequence(Context);
                    int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "DieMapping",
                        () => stageSequence.RunDieMappingAsync(
                            ct,
                            mappingOptions),
                        "wafer=" + _autoWaferId,
                        "requireVisionAlign=" + requireVisionAlign).ConfigureAwait(false);
                    if (stageResult != 0)
                        return Fail("SEQ-IN-STEP-STAGE-DIEMAP", "InputStage",
                            "InputStage die mapping 실패. result=" + stageResult);
                }

                return 0;
            }).ConfigureAwait(false);
            if (result != 0)
                return result;

            _restartDieMappingFromReview = false;
            // Picker Ready는 사용자 리뷰 승인 후에만 발행한다.
            _autoStep = InputSequenceAutoStep.ReviewStage;
            return 0;
        }
    }
}
