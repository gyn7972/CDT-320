using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;

namespace QMC.CDT320.Materials
{
    // MaterialStateService partial: Process Test 데이터 생성/롤백 (원본 1557-1971)
    public static partial class MaterialStateService
    {
        public static bool CreateProcessTestDataSet(out string message)
        {
            return CreateProcessTestDataSet(null, out message);
        }

        public static bool CreateProcessTestOutputStageWafer(QMC.CDT320.BinSide side, out string message)
        {
            message = string.Empty;
            MaterialSnapshot rollbackState = null;
            bool mutationStarted = false;
            try
            {
                lock (_stateSync)
                {
                    RecipeProject project = RecipeStore.LoadLastOrDefault();
                    string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" +
                                       Guid.NewGuid().ToString("N").Substring(0, 8);
                    string lotId = ResolveActiveLotIdForProcessTest();
                    string outputTapeFrameSpecName = ResolveRecipeTapeFrameSpecName(0);
                    MaterialLocationKind location = ResolveOutputStageLocation(side);
                    CassetteMaterialRole cassetteRole = side == QMC.CDT320.BinSide.Ng
                        ? CassetteMaterialRole.Ng1
                        : CassetteMaterialRole.Good1;
                    ValidateProcessTestStageCassetteSlotNoLock(cassetteRole, 0, -1, true);

                    var existing = State.Wafers
                        .Where(w => w != null &&
                                    w.CurrentLocation != null &&
                                    w.CurrentLocation.Kind == location)
                        .ToList();
                    List<DieMaterial> existingOutputDies;
                    string outputClearReason;
                    if (!TryCollectOutputLocationClearDiesNoLock(
                        existing,
                        location,
                        out existingOutputDies,
                        out outputClearReason))
                    {
                        throw new InvalidOperationException(
                            "공정 테스트 Output Stage Material을 안전하게 정리할 수 없습니다. " +
                            outputClearReason);
                    }
                    rollbackState = CreateValidatedProcessTestRollbackStateNoLock();
                    mutationStarted = true;
                    try
                    {
                        DetachOrRemoveClearedOutputDiesNoLock(existingOutputDies);
                        foreach (WaferMaterial wafer in existing)
                        {
                            ClearOutputStageWaferProcessingFieldsNoLock(wafer);
                            wafer.State = WaferMaterialState.Empty;
                            wafer.CurrentLocation = MaterialLocation.Unknown();
                            wafer.UpdatedAt = DateTime.Now;
                        }

                        WaferMaterial sourceWafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                        string sourceWaferId = sourceWafer != null ? sourceWafer.WaferId : "";
                        WaferMaterial stageWafer = CreateProcessTestOutputStageWaferNoLock(
                            side,
                            lotId,
                            timestamp,
                            outputTapeFrameSpecName,
                            sourceWaferId,
                            project);

                        BindProcessTestStageWaferToCassetteSlotNoLock(
                            cassetteRole,
                            0,
                            stageWafer,
                            location,
                            WaferMaterialState.Working,
                            lotId,
                            outputTapeFrameSpecName);

                        State.LotId = lotId;
                        State.RecipeName = project != null ? project.FileName ?? "" : State.RecipeName;

                        MaterialCompactionResult compactionResult = CompactMaterialStateNoLock();
                        LogMaterialCompaction("CreateProcessTestOutputStageWafer", compactionResult);
                        NotifyAndSave("CreateProcessTestOutputStageWafer");
                        if (!TryFlushPendingSave("CreateProcessTestOutputStageWafer"))
                            throw new IOException("공정 테스트 Output Stage Material 저장을 확인하지 못했습니다.");
                        mutationStarted = false;

                        message = "Output Stage 공정 테스트 Wafer Data 생성 완료. side=" + side +
                                  ", wafer=" + (stageWafer != null ? stageWafer.WaferId : "") +
                                  ", target=" + (stageWafer != null ? stageWafer.OutputReceiveTotalCount : 0) +
                                  ", lot=" + lotId;
                        return true;
                    }
                    catch
                    {
                        if (mutationStarted && rollbackState != null)
                            TryRestoreProcessTestRollback(rollbackState, "CreateProcessTestOutputStageWaferRollback");
                        mutationStarted = false;
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                if (mutationStarted && rollbackState != null)
                    TryRestoreProcessTestRollback(rollbackState, "CreateProcessTestOutputStageWaferRollback");
                message = "Output Stage 공정 테스트 Wafer Data 생성 실패: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool CreateProcessTestDataSet(QMC.CDT320.InputStageUnit inputStage, out string message)
        {
            return CreateProcessTestDataSet(inputStage, null, null, out message);
        }

        // 실장비 테스트용: 카세트 유닛을 함께 받으면 실제 맵핑 등록과 동일한 중앙 계산기로
        // 슬롯별 카세트 포지션(검출 위치+로딩 오프셋)까지 저장한다. 유닛이 없거나 티칭이
        // 유효하지 않으면 기존처럼 포지션 없이(NaN) 생성한다.
        public static bool CreateProcessTestDataSet(
            QMC.CDT320.InputStageUnit inputStage,
            QMC.CDT320.InputCassetteUnit inputCassette,
            QMC.CDT320.OutputCassetteUnit outputCassette,
            out string message)
        {
            message = string.Empty;
            MaterialSnapshot rollbackState = null;
            bool mutationStarted = false;
            try
            {
                lock (_stateSync)
                {
                    RecipeProject project = RecipeStore.LoadLastOrDefault();
                    string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" +
                                       Guid.NewGuid().ToString("N").Substring(0, 8);
                    string lotId = ResolveActiveLotIdForProcessTest();
                    string inputTapeFrameSpecName = ResolveInputTapeFrameSpecName(0);
                    string outputTapeFrameSpecName = ResolveRecipeTapeFrameSpecName(0);

                    int inputSlotCount = ResolveProcessTestSlotCount(CassetteMaterialRole.Input1);
                    int outputSlotCount = ResolveProcessTestSlotCount(CassetteMaterialRole.Good1);
                    bool useInput2 = IsCassetteCurrentlyEnabled(CassetteMaterialRole.Input2);
                    bool useGood2 = IsCassetteCurrentlyEnabled(CassetteMaterialRole.Good2);

                    // 실제 맵핑 등록(RegisterMappingResult)과 동일한 계산기 사용:
                    // Input = CalculateWaferCassetteSlotTargetPosition(slot, level),
                    // Output = CalculateBinCassetteSlotTargetPosition(zone, slot).
                    double[] input1Positions = inputCassette != null
                        ? BuildProcessTestSlotPositions(inputSlotCount, i => inputCassette.CalculateWaferCassetteSlotTargetPosition(i, 1), "Input1")
                        : null;
                    double[] input2Positions = inputCassette != null && useInput2
                        ? BuildProcessTestSlotPositions(inputSlotCount, i => inputCassette.CalculateWaferCassetteSlotTargetPosition(i, 2), "Input2")
                        : null;
                    double[] good1Positions = outputCassette != null
                        ? BuildProcessTestSlotPositions(outputSlotCount, i => outputCassette.CalculateBinCassetteSlotTargetPosition(QMC.CDT320.TargetCassette.Good1, i), "Good1")
                        : null;
                    double[] good2Positions = outputCassette != null && useGood2
                        ? BuildProcessTestSlotPositions(outputSlotCount, i => outputCassette.CalculateBinCassetteSlotTargetPosition(QMC.CDT320.TargetCassette.Good2, i), "Good2")
                        : null;
                    double[] ngPositions = outputCassette != null
                        ? BuildProcessTestSlotPositions(outputSlotCount, i => outputCassette.CalculateBinCassetteSlotTargetPosition(QMC.CDT320.TargetCassette.Ng, i), "Ng")
                        : null;

                    DieMap inputMap = LoadRecipeInputDieMapForProcessTest(project);
                    if (!IsUsableSourceMap(inputMap))
                        inputMap = CreateFallbackInputDieMapForProcessTest(project, inputTapeFrameSpecName);
                    if (!IsUsableSourceMap(inputMap))
                    {
                        message = "테스트 입력 DieMap을 만들 수 없습니다. Recipe DieMap 또는 Frame 설정을 확인하세요.";
                        return false;
                    }

                    RecenterInputDieMapForProcessTest(inputMap, inputStage);
                    PickupSequenceGenerator.ApplySequenceNumbers(inputMap, ResolveInputPickup(project));
                    inputMap = DieMapGenerator.Normalize(inputMap);
                    var inputMapValidationWafer = new WaferMaterial
                    {
                        WaferId = "PROCESS-TEST-INPUT-VALIDATION",
                        WaferInstanceId = CreateWaferInstanceId()
                    };
                    string inputMapIdentityReason;
                    if (!TryAssignPhysicalDieIds(inputMap, inputMapValidationWafer, out inputMapIdentityReason))
                    {
                        message = "테스트 입력 DieMap 원본 좌표가 올바르지 않습니다. " + inputMapIdentityReason;
                        return false;
                    }

                    IReadOnlyList<bool> input1Map = BuildProcessTestSlotMap(inputSlotCount, 2);
                    IReadOnlyList<bool> input2Map = useInput2 ? BuildProcessTestSlotMap(inputSlotCount, 1) : null;
                    IReadOnlyList<bool> good1Map = BuildProcessTestSlotMap(outputSlotCount, 2);
                    IReadOnlyList<bool> good2Map = useGood2 ? BuildProcessTestSlotMap(outputSlotCount, 1) : null;
                    IReadOnlyList<bool> ngMap = BuildProcessTestSlotMap(outputSlotCount, 2);

                    ResolveOrCreateCassetteMappingLotId(
                        lotId,
                        CassetteMaterialRole.Input1,
                        CassetteMaterialRole.Input2,
                        CassetteMaterialRole.Good1,
                        CassetteMaterialRole.Good2,
                        CassetteMaterialRole.Ng1);

                    // 모든 역할의 identity 회전을 먼저 검증한다. 공정 테스트가 의도적으로
                    // 초기화할 Stage Material만 사전검증에서 제외하고, 실제 mapping 전에 함께 지운다.
                    ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Input1, true, inputSlotCount, input1Map, false, false, true);
                    ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Input2, useInput2, inputSlotCount, input2Map, false, false, true);
                    ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Good1, true, outputSlotCount, good1Map, false, false, true);
                    ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Good2, useGood2, outputSlotCount, good2Map, false, false, true);
                    ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Ng1, true, outputSlotCount, ngMap, false, false, true);
                    ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Good1, true);
                    ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Good2, true);
                    ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Ng1, true);
                    ValidateProcessTestStageCassetteSlotNoLock(CassetteMaterialRole.Input1, 0, inputSlotCount);
                    ValidateProcessTestStageCassetteSlotNoLock(CassetteMaterialRole.Good1, 0, outputSlotCount);
                    ValidateProcessTestStageCassetteSlotNoLock(CassetteMaterialRole.Ng1, 0, outputSlotCount);

                    rollbackState = CreateValidatedProcessTestRollbackStateNoLock();
                    mutationStarted = true;
                    try
                    {
                        ClearActiveProcessLocationsNoLock();

                    UpdateCassetteMapping(CassetteMaterialRole.Input1, true, inputSlotCount, input1Map, input1Positions, lotId, inputTapeFrameSpecName, false);
                    UpdateCassetteMapping(CassetteMaterialRole.Input2, useInput2, inputSlotCount, input2Map, input2Positions, lotId, inputTapeFrameSpecName, false);
                    UpdateCassetteMapping(CassetteMaterialRole.Good1, true, outputSlotCount, good1Map, good1Positions, lotId, outputTapeFrameSpecName, false);
                    UpdateCassetteMapping(CassetteMaterialRole.Good2, useGood2, outputSlotCount, good2Map, good2Positions, lotId, outputTapeFrameSpecName, false);
                    UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, outputSlotCount, ngMap, ngPositions, lotId, outputTapeFrameSpecName, false);

                    WaferMaterial inputStageWafer = GetOrCreateWafer("TEST-IN-STAGE-" + timestamp);
                    inputStageWafer.CassetteLotId = lotId;
                    inputStageWafer.SourceCassetteId = CassetteMaterialRole.Input1.ToString();
                    inputStageWafer.SourceCassetteRole = CassetteMaterialRole.Input1;
                    inputStageWafer.SourceSlotNumber = 0;
                    inputStageWafer.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                    inputStageWafer.State = WaferMaterialState.Working;
                    inputStageWafer.TapeFrameSpecName = inputTapeFrameSpecName;
                    inputStageWafer.DieMapFrameObjId = string.IsNullOrWhiteSpace(inputMap.FrameObjId) ? inputStageWafer.WaferId : inputMap.FrameObjId;
                    inputStageWafer.HasInputStageAlignResult = true;
                    inputStageWafer.InputStageAlignOriginX = inputMap.OriginX;
                    inputStageWafer.InputStageAlignOriginY = inputMap.OriginY;
                    inputStageWafer.InputStageAlignPitchX = inputMap.PitchX;
                    inputStageWafer.InputStageAlignPitchY = inputMap.PitchY;
                    inputStageWafer.InputStageDieSizeX = inputMap.DieSizeX;
                    inputStageWafer.InputStageDieSizeY = inputMap.DieSizeY;
                    inputStageWafer.InputStageOuterDiameterMm = inputMap.OuterDiameterMm;
                    inputStageWafer.InputStageAlignOffsetX = 0.0;
                    inputStageWafer.InputStageAlignOffsetY = 0.0;
                    inputStageWafer.HasInputStageThetaAlignResult = true;
                    inputStageWafer.InputStageAlignReferenceT =
                        inputStage != null && inputStage.Recipe != null && inputStage.Recipe.WaferT != null
                            ? inputStage.Recipe.WaferT.ProcessPosition
                            : 0.0;
                    inputStageWafer.InputStageAlignOffsetT = ResolveProcessTestThetaAlignOffset(inputStage);
                    inputStageWafer.InputStageAlignCorrectedT =
                        inputStageWafer.InputStageAlignReferenceT + inputStageWafer.InputStageAlignOffsetT;
                    inputStageWafer.HasInputStageDieMappingResult = true;
                    inputStageWafer.InputStageDieMappingOffsetX = 0.0;
                    inputStageWafer.InputStageDieMappingOffsetY = 0.0;
                    inputStageWafer.HasInputStageDieMappingOrigin = true;
                    inputStageWafer.InputStageDieMappingOriginX = inputMap.OriginX;
                    inputStageWafer.InputStageDieMappingOriginY = inputMap.OriginY;
                    inputStageWafer.HasInputStageDieMappingThetaSnapshot = true;
                    inputStageWafer.InputStageDieMappingCorrectedT = inputStageWafer.InputStageAlignCorrectedT;
                    inputStageWafer.InputStageDieMappingInvalidatedByAlignChange = false;
                    inputStageWafer.InputMapApprovalHashAtMapping = project != null && project.MapApprovalVersion > 0
                        ? project.InputMapApprovalHash ?? ""
                        : "";
                    inputStageWafer.HasInputStageRunReviewApproval = false;
                    inputStageWafer.InputStageRunReviewStartDieIndex = 0;
                    inputStageWafer.InputStageRunReviewStartDieUid = "";
                    inputStageWafer.InputStageRunReviewOrderedDieIds = new List<string>();
                    inputStageWafer.InputStageRunReviewMappingRevision = "";
                    inputStageWafer.UpdatedAt = DateTime.Now;
                    BindProcessTestStageWaferToCassetteSlotNoLock(
                        CassetteMaterialRole.Input1,
                        0,
                        inputStageWafer,
                        MaterialLocationKind.InputStage,
                        WaferMaterialState.Working,
                        lotId,
                        inputTapeFrameSpecName,
                        ResolveSlotPosition(input1Positions, 0));

                    int inputTargetCount = ApplyProcessTestInputDieMaterialsNoLock(inputMap, inputStageWafer);

                    WaferMaterial goodStageWafer = CreateProcessTestOutputStageWaferNoLock(QMC.CDT320.BinSide.Good, lotId, timestamp, outputTapeFrameSpecName, inputStageWafer.WaferId, project);
                    WaferMaterial ngStageWafer = CreateProcessTestOutputStageWaferNoLock(QMC.CDT320.BinSide.Ng, lotId, timestamp, outputTapeFrameSpecName, inputStageWafer.WaferId, project);
                    BindProcessTestStageWaferToCassetteSlotNoLock(
                        CassetteMaterialRole.Good1,
                        0,
                        goodStageWafer,
                        MaterialLocationKind.OutputStageGood,
                        WaferMaterialState.Working,
                        lotId,
                        outputTapeFrameSpecName,
                        ResolveSlotPosition(good1Positions, 0));
                    BindProcessTestStageWaferToCassetteSlotNoLock(
                        CassetteMaterialRole.Ng1,
                        0,
                        ngStageWafer,
                        MaterialLocationKind.OutputStageNg,
                        WaferMaterialState.Working,
                        lotId,
                        outputTapeFrameSpecName,
                        ResolveSlotPosition(ngPositions, 0));

                    State.LotId = lotId;
                    State.RecipeName = project != null ? project.FileName ?? "" : State.RecipeName;

                    MaterialCompactionResult compactionResult = CompactMaterialStateNoLock();
                    LogMaterialCompaction("CreateProcessTestDataSet", compactionResult);
                    NotifyAndSave("CreateProcessTestDataSet");
                    if (!TryFlushPendingSave("CreateProcessTestDataSet"))
                        throw new IOException("공정 테스트 Material 저장을 확인하지 못했습니다.");
                    mutationStarted = false;

                        message = "공정 테스트 Data 생성 완료. InputStage die=" + inputTargetCount +
                                  ", GoodStage target=" + (goodStageWafer != null ? goodStageWafer.OutputReceiveTotalCount : 0) +
                                  ", NgStage target=" + (ngStageWafer != null ? ngStageWafer.OutputReceiveTotalCount : 0) +
                                  ", lot=" + lotId +
                                  ", 카세트 포지션=" + (input1Positions != null && good1Positions != null && ngPositions != null
                                      ? "저장됨"
                                      : "미저장(카세트 티칭/유닛 확인 필요)");
                        return true;
                    }
                    catch
                    {
                        if (mutationStarted && rollbackState != null)
                            TryRestoreProcessTestRollback(rollbackState, "CreateProcessTestDataSetRollback");
                        mutationStarted = false;
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                if (mutationStarted && rollbackState != null)
                    TryRestoreProcessTestRollback(rollbackState, "CreateProcessTestDataSetRollback");
                message = "공정 테스트 Data 생성 실패: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool TryRestoreProcessTestRollback(MaterialSnapshot rollbackState, string reason)
        {
            if (rollbackState == null)
                return false;

            try
            {
                lock (_stateSync)
                {
                    MaterialStorage.ReplaceState(rollbackState);
                    InputStageHybridResultSession.Clear();
                    _outputReceiveOrderCache.Clear();
                }

                NotifyAndSave(reason);
                bool saved = TryFlushPendingSave(reason);
                Log.Write(
                    "Main",
                    "SYSTEM",
                    "MaterialStateService",
                    "Process Test Material rollback restored. reason=" + (reason ?? "") +
                    ", saved=" + saved + (saved ? " - Ok" : " - Failed"));
                return saved;
            }
            catch (Exception ex)
            {
                Log.Write(
                    "Main",
                    "SYSTEM",
                    "MaterialStateService",
                    "Process Test Material rollback failed. reason=" + (reason ?? "") +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static MaterialSnapshot CreateValidatedProcessTestRollbackStateNoLock()
        {
            MaterialSnapshot rollbackState = MaterialSnapshotStore.CreateStateCopy(State);
            if (rollbackState == null)
                throw new InvalidOperationException("공정 테스트 rollback snapshot을 만들 수 없습니다.");

            string validationReason;
            if (!MaterialStorage.TryPrepareStateForUse(rollbackState, out validationReason))
            {
                throw new InvalidOperationException(
                    "현재 Material 상태가 올바르지 않아 공정 테스트를 시작할 수 없습니다. " +
                    validationReason);
            }

            return rollbackState;
        }

        private static string ResolveActiveLotIdForProcessTest()
        {
            string lotId = State != null ? (State.LotId ?? string.Empty).Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(lotId))
            {
                throw new InvalidOperationException(
                    "공정 테스트 Data는 활성 LOT ID가 필요합니다. Cassette Mapping으로 LOT ID를 먼저 설정하세요.");
            }

            return lotId;
        }

    }
}
