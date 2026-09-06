using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using QMC.CDT320.Calibration;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;
using QMC.Common;

namespace QMC.CDT320.Materials
{
    [DataContract]
    public sealed class InputStageReviewSavedVerification
    {
        [DataMember] public string VerificationId { get; set; }
        [DataMember] public InputStageReviewGeometryContext Context { get; set; }
        [DataMember] public InputStageReviewGeometryTolerance Tolerance { get; set; }
        [DataMember] public List<InputStageReviewMeasurement> Measurements { get; set; }

        public InputStageReviewSavedVerification Clone()
        {
            return new InputStageReviewSavedVerification
            {
                VerificationId = VerificationId,
                Context = Context != null ? Context.Clone() : null,
                Tolerance = Tolerance != null ? Tolerance.Clone() : null,
                Measurements = Measurements != null
                    ? Measurements.ConvertAll(point => point != null ? point.Clone() : null) : null
            };
        }
    }

    public static partial class MaterialStateService
    {
        private static long _inputStageReviewSession;
        private static long _inputStageReviewRequest;
        private static string _inputStageReviewVerificationToken;
        private static InputStageReviewGeometryEvidence _inputStageReviewEvidence;
        private static InputStageReviewGeometryContext _inputStageReviewNonProductionContext;
        private static string _inputStageReviewAcceptedSourceSignature;
        private static string _inputStageReviewAcceptedCandidateSignature;
        private static string _inputStageReviewAcceptedConditionSignature;
        private static readonly HashSet<string> _inputStageReviewPendingSave = new HashSet<string>(StringComparer.Ordinal);

        public static void InvalidateInputStageReviewVerification(long sessionGeneration, long requestGeneration)
        {
            lock (_stateSync)
            {
                if (_inputStageReviewSession != sessionGeneration)
                {
                    _inputStageReviewAcceptedSourceSignature = null;
                    _inputStageReviewAcceptedCandidateSignature = null;
                    _inputStageReviewAcceptedConditionSignature = null;
                }
                _inputStageReviewSession = sessionGeneration;
                _inputStageReviewRequest = requestGeneration;
                _inputStageReviewVerificationToken = null;
                _inputStageReviewEvidence = null;
                _inputStageReviewNonProductionContext = null;
            }
        }

        public static bool TryRecordInputStageReviewOffset(InputStageReviewGeometryContext context,
            DieMap draft, double offsetX, double offsetY, out string reason)
        {
            try
            {
                if (context == null)
                    return FailInputStageReviewGeometry("OFFSET 검출 기준이 없습니다.", out reason);
                lock (_stateSync)
                {
                    InputStageReviewGeometryContext current;
                    if (!TryCaptureInputStageReviewGeometryContext(context.WaferId, draft,
                        context.SessionGeneration, context.RequestGeneration, false, out current, out reason) ||
                        !InputStageReviewGeometryPolicy.IsSameContext(context, current, out reason))
                        return false;
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (!CheckInputStageReviewAuthorizedCandidateNoLock(wafer, current, out reason))
                        return false;
                    InputStageReviewOffsetCandidate offset;
                    if (!InputStageReviewGeometryPolicy.TryCreateOffsetCandidate(current, offsetX, offsetY,
                        GetInputStageReviewOffsetLimits(), out offset, out reason))
                        return false;
                    var candidate = new DieMap
                    {
                        FrameObjId = draft.FrameObjId, DieMapX = draft.DieMapX, DieMapY = draft.DieMapY,
                        PitchX = draft.PitchX, PitchY = draft.PitchY, DieSizeX = draft.DieSizeX, DieSizeY = draft.DieSizeY,
                        OuterDiameterMm = draft.OuterDiameterMm, OriginX = offset.OriginX, OriginY = offset.OriginY
                    };
                    foreach (DieMapEntry entry in draft.Entries)
                        candidate.Entries.Add(new DieMapEntry
                        {
                            DieUid = entry.DieUid, Index = entry.Index, DieMapX = entry.DieMapX, DieMapY = entry.DieMapY,
                            OriginalMapX = entry.OriginalMapX, OriginalMapY = entry.OriginalMapY,
                            PosX = entry.PosX + offsetX, PosY = entry.PosY + offsetY
                        });
                    if (!CheckInputStageReviewMapGeometry(candidate, out reason))
                        return false;
                    _inputStageReviewAcceptedSourceSignature = BuildInputStageReviewGeometrySignature(BuildDieMapFromWaferNoLock(wafer));
                    _inputStageReviewAcceptedCandidateSignature = BuildInputStageReviewGeometrySignature(candidate);
                    _inputStageReviewAcceptedConditionSignature = current.ConditionSignature;
                    reason = "단발/누적 한계 안의 후보 좌표를 기록했습니다. 다점 검증이 필요합니다.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                return FailInputStageReviewGeometry("OFFSET 후보 기록 실패: " + ex.Message, out reason);
            }
        }

        public static InputStageReviewOffsetLimits GetInputStageReviewOffsetLimits()
        {
            InputStageConfig config = ResolveInputStageReviewConfig();
            return config == null ? null : new InputStageReviewOffsetLimits
            {
                SingleX = config.ManualDieDetectOffsetLimitX,
                SingleY = config.ManualDieDetectOffsetLimitY,
                CumulativeX = config.ManualDieDetectCumulativeOffsetLimitX,
                CumulativeY = config.ManualDieDetectCumulativeOffsetLimitY
            };
        }

        public static InputStageReviewGeometryTolerance GetInputStageReviewGeometryTolerance()
        {
            InputStageConfig config = ResolveInputStageReviewConfig();
            return config == null ? null : new InputStageReviewGeometryTolerance
            {
                ResidualX = config.AlignCenterToleranceMm,
                ResidualY = config.AlignCenterToleranceMm,
                Pitch = config.AlignPitchCompareToleranceMm,
                Theta = config.MaxEffectiveThetaToleranceDeg,
                StageTheta = ResolveInputStageReviewActualThetaTolerance()
            };
        }

        public static bool IsInputStageReviewNonProductionMode(out string reason)
        {
            bool nonProduction;
            string signature;
            if (!TryResolveInputStageReviewRuntimeMode(out nonProduction, out signature, out reason))
                return false;
            if (!nonProduction)
                return FailInputStageReviewGeometry("현재 실운전 모드는 실제 다점 좌표 검증이 필요합니다.", out reason);
            reason = "현재 Simulation/DryRun 또는 장비·비전 미사용 모드입니다.";
            return true;
        }

        public static bool TryRegisterInputStageReviewNonProductionApproval(
            string waferId, DieMap draft, long sessionGeneration, long requestGeneration,
            out InputStageReviewGeometryContext context, out string verificationToken, out string reason)
        {
            context = null;
            verificationToken = null;
            try
            {
                lock (_stateSync)
                {
                    if (!IsInputStageReviewNonProductionMode(out reason))
                        return false;
                    InputStageReviewGeometryContext current;
                    if (!TryCaptureInputStageReviewGeometryContext(waferId, draft, sessionGeneration,
                        requestGeneration, true, out current, out reason))
                        return false;
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (!current.IsSimulation || !IsStoredInputStageResultModeUsableNoLock(wafer, true, out reason) ||
                        !CheckInputStageReviewAuthorizedCandidateNoLock(wafer, current, out reason))
                        return false;
                    if (!wafer.HasInputStageReviewBaseline)
                    {
                        SetInputStageReviewBaseline(wafer, draft);
                        if (!TryNotifyAndSave("InputStageReviewNonProductionBaseline"))
                            return FailInputStageReviewGeometry("비실운전 Review 기준 저장 요청에 실패했습니다.", out reason);
                    }
                    _inputStageReviewEvidence = null;
                    _inputStageReviewNonProductionContext = current.Clone();
                    _inputStageReviewVerificationToken = "NONPRODUCTION-MANUAL-" + Guid.NewGuid().ToString("N");
                    context = current.Clone();
                    verificationToken = _inputStageReviewVerificationToken;
                    reason = "현재 비실운전 모드의 수동 Review 승인 자료를 준비했습니다.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                return FailInputStageReviewGeometry("비실운전 Review 승인 준비 실패: " + ex.Message, out reason);
            }
        }

        public static bool CheckInputStageReviewVerificationDie(
            long sessionGeneration, long requestGeneration, string waferId,
            string mappingRevision, string dieUid, out string reason)
        {
            lock (_stateSync)
            {
                if (sessionGeneration <= 0 || requestGeneration <= 0 ||
                    sessionGeneration != _inputStageReviewSession || requestGeneration != _inputStageReviewRequest)
                    return FailInputStageReviewGeometry("검증점 확인 중 Review 세션 또는 요청이 변경되었습니다.", out reason);
                WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer == null || string.IsNullOrWhiteSpace(waferId) ||
                    !string.Equals(wafer.WaferId, waferId, StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(mappingRevision) ||
                    !string.Equals(ResolveInputStageRunReviewMappingRevision(wafer, null), mappingRevision, StringComparison.OrdinalIgnoreCase))
                    return FailInputStageReviewGeometry("검증점의 Wafer 또는 Mapping revision이 현재 InputStage와 다릅니다.", out reason);
                return CheckInputStageReviewVerificationDieNoLock(wafer, dieUid, out reason);
            }
        }

        public static bool TryCaptureInputStageReviewGeometryContext(
            string waferId, DieMap draft, long sessionGeneration, long requestGeneration,
            bool allowBaselineEstablishment, out InputStageReviewGeometryContext context, out string reason)
        {
            context = null;
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (wafer == null || !string.Equals(wafer.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                        return FailInputStageReviewGeometry("Review 대상 Wafer가 현재 InputStage와 다릅니다.", out reason);
                    if (sessionGeneration != _inputStageReviewSession || requestGeneration != _inputStageReviewRequest)
                        return FailInputStageReviewGeometry("Review 세션 또는 요청이 변경되어 재검출/재검증이 필요합니다.", out reason);
                    if (!CheckInputStageReviewStationaryTheta(wafer, out reason))
                        return false;
                    return TryBuildInputStageReviewContextNoLock(wafer, draft, sessionGeneration, requestGeneration,
                        allowBaselineEstablishment, out context, out reason);
                }
            }
            catch (Exception ex)
            {
                return FailInputStageReviewGeometry("Review 좌표 기준 확인 실패: " + ex.Message, out reason);
            }
        }

        public static bool TryRegisterInputStageReviewVerification(
            InputStageReviewGeometryContext context, DieMap draft, IList<InputStageReviewMeasurement> samples,
            out string verificationToken, out string reason)
        {
            verificationToken = null;
            try
            {
                if (context == null)
                    return FailInputStageReviewGeometry("검증 시작 기준이 없습니다.", out reason);
                lock (_stateSync)
                {
                    InputStageReviewGeometryContext current;
                    if (!TryCaptureInputStageReviewGeometryContext(context.WaferId, draft,
                        context.SessionGeneration, context.RequestGeneration, true, out current, out reason) ||
                        !InputStageReviewGeometryPolicy.IsSameContext(context, current, out reason))
                        return false;
                    if (!CheckInputStageReviewMeasurementIdentity(draft, samples, out reason))
                        return false;
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (!CheckInputStageReviewAuthorizedCandidateNoLock(wafer, current, out reason))
                        return false;
                    foreach (InputStageReviewMeasurement sample in samples)
                    {
                        if (!CheckInputStageReviewVerificationDieNoLock(wafer, sample.DieUid, out reason))
                            return false;
                    }
                    InputStageReviewGeometryEvidence evidence;
                    if (!InputStageReviewGeometryPolicy.TryVerify(current, samples,
                        GetInputStageReviewGeometryTolerance(), out evidence, out reason))
                        return false;
                    if (!wafer.HasInputStageReviewBaseline)
                    {
                        // 구형 저장 자료는 현재 좌표 그대로 다점 검증한 경우에만 최초 기준을 얻는다.
                        SetInputStageReviewBaseline(wafer, draft);
                        if (!TryNotifyAndSave("InputStageReviewBaselineVerified"))
                            return FailInputStageReviewGeometry("검증 기준 저장 요청에 실패했습니다.", out reason);
                    }
                    _inputStageReviewEvidence = evidence;
                    _inputStageReviewNonProductionContext = null;
                    _inputStageReviewVerificationToken = Guid.NewGuid().ToString("N");
                    verificationToken = _inputStageReviewVerificationToken;
                    reason = "다점 검증 증거를 현재 후보 좌표에 연결했습니다. verification=" + verificationToken;
                    return true;
                }
            }
            catch (Exception ex)
            {
                return FailInputStageReviewGeometry("Review 검증 등록 실패: " + ex.Message, out reason);
            }
        }

        internal static void SetInputStageReviewBaseline(WaferMaterial wafer, DieMap map)
        {
            if (wafer == null || map == null)
                return;
            lock (_stateSync)
            {
                wafer.HasInputStageReviewBaseline = true;
                wafer.InputStageReviewBaselineMappingRevision = ResolveInputStageRunReviewMappingRevision(wafer, map);
                wafer.InputStageReviewBaselineOriginX = map.OriginX;
                wafer.InputStageReviewBaselineOriginY = map.OriginY;
                wafer.InputStageReviewVerification = null;
            }
        }

        private static bool TryBuildInputStageReviewContextNoLock(
            WaferMaterial wafer, DieMap draft, long session, long request, bool allowBaselineEstablishment,
            out InputStageReviewGeometryContext context, out string reason)
        {
            context = null;
            if (!CheckInputStageReviewMapGeometry(draft, out reason))
                return false;
            if (wafer == null || draft == null || draft.Entries == null || draft.Entries.Count == 0 ||
                !wafer.HasInputStageDieMappingResult || wafer.InputStageDieMappingInvalidatedByAlignChange ||
                !wafer.HasInputStageDieMappingThetaSnapshot || !wafer.HasInputStageThetaAlignResult)
                return FailInputStageReviewGeometry("유효한 Align/T/Die Mapping 기준이 없습니다. 재얼라인/재매핑이 필요합니다.", out reason);
            string revision = ResolveInputStageRunReviewMappingRevision(wafer, draft);
            bool hasBaseline = wafer.HasInputStageReviewBaseline && string.Equals(
                wafer.InputStageReviewBaselineMappingRevision, revision, StringComparison.OrdinalIgnoreCase);
            if (wafer.HasInputStageReviewBaseline && !hasBaseline)
                return FailInputStageReviewGeometry("보정 기준의 Mapping revision이 다릅니다. 기존 누적 기준을 유지하며 재매핑을 요구합니다.", out reason);
            if (!hasBaseline && (!allowBaselineEstablishment ||
                !string.Equals(BuildInputStageReviewGeometrySignature(draft),
                    BuildInputStageReviewGeometrySignature(BuildDieMapFromWaferNoLock(wafer)), StringComparison.Ordinal)))
                return FailInputStageReviewGeometry("저장된 보정 기준이 없습니다. 현재 좌표를 먼저 VERIFY MAP 하거나 재매핑하세요.", out reason);
            if (Math.Abs(wafer.InputStageDieMappingCorrectedT - wafer.InputStageAlignCorrectedT) > InputStageThetaMappingSnapshotToleranceDeg)
                return FailInputStageReviewGeometry("Mapping 이후 저장 T가 변경되었습니다. 재매핑이 필요합니다.", out reason);
            string condition = BuildInputStageReviewConditionSignature(wafer);
            if (string.IsNullOrWhiteSpace(condition))
                return FailInputStageReviewGeometry("현재 레시피/카메라/좌표 설정을 확인할 수 없습니다.", out reason);
            context = new InputStageReviewGeometryContext
            {
                WaferId = wafer.WaferId, MappingRevision = revision,
                ConditionSignature = condition, CandidateSignature = BuildInputStageReviewGeometrySignature(draft),
                SessionGeneration = session, RequestGeneration = request,
                StageTheta = wafer.InputStageDieMappingCorrectedT,
                PitchX = draft.PitchX, PitchY = draft.PitchY, OriginX = draft.OriginX, OriginY = draft.OriginY,
                BaselineOriginX = hasBaseline ? wafer.InputStageReviewBaselineOriginX : draft.OriginX,
                BaselineOriginY = hasBaseline ? wafer.InputStageReviewBaselineOriginY : draft.OriginY,
                IsSimulation = IsInputStageReviewSimulation(wafer)
            };
            InputStageReviewOffsetCandidate candidate;
            return InputStageReviewGeometryPolicy.TryCreateOffsetCandidate(context, 0, 0,
                GetInputStageReviewOffsetLimits(), out candidate, out reason);
        }

        private static bool CheckInputStageReviewAuthorizedCandidateNoLock(
            WaferMaterial wafer, InputStageReviewGeometryContext context, out string reason)
        {
            string source = BuildInputStageReviewGeometrySignature(BuildDieMapFromWaferNoLock(wafer));
            if (string.Equals(context.CandidateSignature, source, StringComparison.Ordinal) ||
                (string.Equals(source, _inputStageReviewAcceptedSourceSignature, StringComparison.Ordinal) &&
                 string.Equals(context.CandidateSignature, _inputStageReviewAcceptedCandidateSignature, StringComparison.Ordinal) &&
                 string.Equals(context.ConditionSignature, _inputStageReviewAcceptedConditionSignature, StringComparison.Ordinal)))
            {
                reason = "현재 Material 또는 단발 한계를 확인한 OFFSET 후보입니다.";
                return true;
            }
            return FailInputStageReviewGeometry("기록되지 않은 좌표 변경입니다. DIE DETECTION/APPLY OFFSET 경로를 다시 확인하세요.", out reason);
        }

        private static bool CheckInputStageReviewMapGeometry(DieMap map, out string reason)
        {
            if (map == null || map.Entries == null || map.Entries.Count == 0 || map.DieMapX <= 0 || map.DieMapY <= 0 ||
                !IsFiniteInputStageReviewNumber(map.OriginX) || !IsFiniteInputStageReviewNumber(map.OriginY) ||
                !IsFiniteInputStageReviewNumber(map.PitchX) || !IsFiniteInputStageReviewNumber(map.PitchY) ||
                !IsFiniteInputStageReviewNumber(map.DieSizeX) || !IsFiniteInputStageReviewNumber(map.DieSizeY) ||
                !IsFiniteInputStageReviewNumber(map.OuterDiameterMm) || map.PitchX <= 0 || map.PitchY <= 0 ||
                map.DieSizeX <= 0 || map.DieSizeY <= 0 || map.OuterDiameterMm <= 0)
                return FailInputStageReviewGeometry("후보 맵의 크기/pitch/원점이 유효하지 않습니다.", out reason);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var indices = new HashSet<string>(StringComparer.Ordinal);
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid) || !ids.Add(entry.DieUid) ||
                    entry.DieMapX < 0 || entry.DieMapX >= map.DieMapX || entry.DieMapY < 0 || entry.DieMapY >= map.DieMapY ||
                    !indices.Add(entry.DieMapX.ToString(CultureInfo.InvariantCulture) + ":" + entry.DieMapY.ToString(CultureInfo.InvariantCulture)) ||
                    !IsFiniteInputStageReviewNumber(entry.PosX) || !IsFiniteInputStageReviewNumber(entry.PosY))
                    return FailInputStageReviewGeometry("후보 맵에 중복 UID/index 또는 유효하지 않은 좌표가 있습니다.", out reason);
            }
            reason = "전체 후보 좌표가 유효합니다.";
            return true;
        }

        private static bool IsFiniteInputStageReviewNumber(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool TryResolveInputStageReviewRuntimeMode(
            out bool nonProduction, out string signature, out string reason)
        {
            nonProduction = false;
            signature = null;
            AppSettings settings = AppSettingsStore.Current;
            CDT320_Machine machine = CalibrationCoordinateService.ResolveMachine();
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            RecipeProject recipe = RecipeStore.LoadLastOrDefaultCached();
            if (settings == null || stage == null || stage.Setup == null || stage.Config == null || recipe == null ||
                stage.CameraX == null || stage.CameraX.Config == null || stage.StageY == null || stage.StageY.Config == null ||
                stage.StageT == null || stage.StageT.Config == null)
                return FailInputStageReviewGeometry("Review 운전 모드를 확인할 설정/장비/레시피/축 정보가 없습니다.", out reason);
            nonProduction = settings.SimulationMode || settings.DryRunMode || !settings.UseVision || !settings.UseAjin ||
                stage.Setup.IsSimulationMode || stage.Config.bDryRun || recipe.DryRun ||
                stage.CameraX.Config.IsSimulationMode || stage.StageY.Config.IsSimulationMode || stage.StageT.Config.IsSimulationMode;
            var mode = new StringBuilder();
            AppendInputStageReviewValue(mode, settings.SimulationMode, settings.DryRunMode, settings.UseVision,
                settings.UseAjin, settings.UseRealVisionInSimulation, stage.Setup.IsSimulationMode, stage.Config.bDryRun,
                recipe.DryRun, stage.CameraX.Config.IsSimulationMode, stage.StageY.Config.IsSimulationMode,
                stage.StageT.Config.IsSimulationMode);
            signature = mode.ToString();
            reason = "현재 개별 운전 모드 설정을 확인했습니다.";
            return true;
        }

        private static bool IsInputStageReviewSimulation(WaferMaterial wafer)
        {
            AppSettings settings = AppSettingsStore.Current;
            CDT320_Machine machine = CalibrationCoordinateService.ResolveMachine();
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            RecipeProject recipe = RecipeStore.LoadLastOrDefaultCached();
            return settings == null || settings.SimulationMode || settings.DryRunMode || !settings.UseVision || !settings.UseAjin ||
                stage == null || stage.IsInputStageSimulationOrDryRun() || recipe == null || recipe.DryRun ||
                stage.CameraX == null || stage.CameraX.Config == null || stage.CameraX.Config.IsSimulationMode ||
                stage.StageY == null || stage.StageY.Config == null || stage.StageY.Config.IsSimulationMode ||
                stage.StageT == null || stage.StageT.Config == null || stage.StageT.Config.IsSimulationMode ||
                InputStageResultMode.IsHybrid(wafer.InputStageAlignResultMode) || InputStageResultMode.IsHybrid(wafer.InputStageDieMappingResultMode);
        }

        private static bool CheckInputStageReviewProgressPreservation(
            WaferMaterial wafer, DieMap currentMap, UserConfirmResult review,
            string currentRevision, out string reason)
        {
            if (wafer == null || currentMap == null || currentMap.Entries == null || review == null ||
                review.DieStates == null || review.OrderedDieIds == null ||
                string.IsNullOrWhiteSpace(currentRevision) ||
                !string.Equals(wafer.InputStageRunReviewMappingRevision, currentRevision, StringComparison.OrdinalIgnoreCase))
                return FailInputStageReviewGeometry("진행된 Wafer의 기존 승인 Mapping revision을 확인할 수 없습니다.", out reason);
            if (!review.HasMapOrigin || !IsFiniteInputStageReviewNumber(review.MapOriginX) ||
                !IsFiniteInputStageReviewNumber(review.MapOriginY) ||
                review.MapOriginX != currentMap.OriginX || review.MapOriginY != currentMap.OriginY)
                return FailInputStageReviewGeometry("진행된 Wafer는 원점을 변경할 수 없습니다. 기존 좌표 그대로 VERIFY MAP을 진행하세요.", out reason);

            var entries = new Dictionary<string, DieMapEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (DieMapEntry entry in currentMap.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid) || entries.ContainsKey(entry.DieUid) ||
                    !IsFiniteInputStageReviewNumber(entry.PosX) || !IsFiniteInputStageReviewNumber(entry.PosY))
                    return FailInputStageReviewGeometry("진행된 Wafer의 현재 UID/좌표를 확인할 수 없습니다.", out reason);
                entries.Add(entry.DieUid, entry);
            }
            if (entries.Count == 0 || review.DieStates.Count != entries.Count)
                return FailInputStageReviewGeometry("진행된 Wafer의 전체 Die 목록이 변경되었습니다.", out reason);
            var submittedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (InputStageRunReviewDieState draft in review.DieStates)
            {
                DieMapEntry entry;
                if (draft == null || string.IsNullOrWhiteSpace(draft.DieId) || !submittedIds.Add(draft.DieId) ||
                    !entries.TryGetValue(draft.DieId, out entry) || !draft.HasPosition ||
                    draft.PositionX != entry.PosX || draft.PositionY != entry.PosY ||
                    draft.IsTarget != entry.IsTarget || draft.Result != entry.Result || draft.BinCode != entry.BinCode)
                    return FailInputStageReviewGeometry("진행된 Wafer는 Die 목록/상태/Bin/좌표를 변경할 수 없습니다. 현재 Map을 다시 여세요.", out reason);
            }

            List<string> approved = wafer.InputStageRunReviewOrderedDieIds;
            if (approved == null || approved.Count == 0 || approved.Any(string.IsNullOrWhiteSpace) ||
                approved.Distinct(StringComparer.OrdinalIgnoreCase).Count() != approved.Count ||
                approved.Any(id => !entries.ContainsKey(id)))
                return FailInputStageReviewGeometry("진행된 Wafer의 기존 전체 승인 PickUp 목록이 없거나 현재 Map과 다릅니다.", out reason);
            var waiting = new HashSet<string>(entries.Values.Where(entry => entry.IsTarget &&
                entry.Result != DieResult.Good && entry.Result != DieResult.NG).Select(entry => entry.DieUid),
                StringComparer.OrdinalIgnoreCase);
            List<string> remaining = approved.Where(waiting.Contains).ToList();
            if (remaining.Count != waiting.Count || !remaining.SequenceEqual(review.OrderedDieIds, StringComparer.OrdinalIgnoreCase))
                return FailInputStageReviewGeometry("진행된 Wafer의 남은 PickUp 순서를 변경하거나 건너뛸 수 없습니다. 기존 승인 순서를 유지하세요.", out reason);
            if (!ValidateInputStageRunReviewStartSelection(approved, wafer.InputStageRunReviewStartDieUid,
                    wafer.InputStageRunReviewStartDieIndex, out reason) ||
                !ValidateInputStageRunReviewStartSelection(remaining, review.StartDieUid, review.StartDieIndex, out reason))
                return false;
            // 화면의 시작 인덱스는 남은 순서 기준이다. 원래 승인 시작점/전체 순서는 저장된 값을 보존한다.
            reason = "기존 Die 상태/좌표와 남은 승인 순서가 일치하여 검증 증거만 갱신할 수 있습니다.";
            return true;
        }

        private static bool TryValidateInputStageReviewCommitNoLock(
            WaferMaterial wafer, DieMap currentMap, UserConfirmResult review,
            out InputStageReviewSavedVerification savedVerification, out string reason)
        {
            savedVerification = null;
            if (review.ReviewSessionGeneration != _inputStageReviewSession ||
                review.ReviewRequestGeneration != _inputStageReviewRequest ||
                string.IsNullOrWhiteSpace(review.GeometryVerificationToken) ||
                !string.Equals(review.GeometryVerificationToken, _inputStageReviewVerificationToken, StringComparison.Ordinal) ||
                (_inputStageReviewEvidence == null && _inputStageReviewNonProductionContext == null))
                return FailInputStageReviewGeometry("현재 요청의 다점 검증 증거가 없습니다. VERIFY MAP 후 다시 확인하세요.", out reason);
            if (!review.HasMapOrigin || review.DieStates == null)
                return FailInputStageReviewGeometry("검증 후보 원점/좌표가 제출되지 않았습니다.", out reason);
            var byId = review.DieStates.Where(item => item != null && !string.IsNullOrWhiteSpace(item.DieId))
                .GroupBy(item => item.DieId, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            var candidate = new DieMap
            {
                FrameObjId = currentMap.FrameObjId, DieMapX = currentMap.DieMapX, DieMapY = currentMap.DieMapY,
                PitchX = currentMap.PitchX, PitchY = currentMap.PitchY,
                DieSizeX = currentMap.DieSizeX, DieSizeY = currentMap.DieSizeY, OuterDiameterMm = currentMap.OuterDiameterMm,
                OriginX = review.MapOriginX, OriginY = review.MapOriginY
            };
            foreach (DieMapEntry entry in currentMap.Entries)
            {
                List<InputStageRunReviewDieState> submitted;
                if (entry == null || !byId.TryGetValue(entry.DieUid, out submitted) || submitted.Count != 1 || !submitted[0].HasPosition)
                    return FailInputStageReviewGeometry("검증한 전체 Die 좌표와 제출 목록이 다릅니다.", out reason);
                candidate.Entries.Add(new DieMapEntry
                {
                    DieUid = entry.DieUid, Index = entry.Index, DieMapX = entry.DieMapX, DieMapY = entry.DieMapY,
                    OriginalMapX = entry.OriginalMapX, OriginalMapY = entry.OriginalMapY,
                    PosX = submitted[0].PositionX, PosY = submitted[0].PositionY
                });
            }
            InputStageReviewGeometryContext current;
            if (!CheckInputStageReviewStationaryTheta(wafer, out reason) ||
                !TryBuildInputStageReviewContextNoLock(wafer, candidate, review.ReviewSessionGeneration,
                    review.ReviewRequestGeneration, false, out current, out reason))
                return false;
            if (_inputStageReviewNonProductionContext != null)
            {
                if (!IsInputStageReviewNonProductionMode(out reason) ||
                    !CheckInputStageReviewNonProductionContext(_inputStageReviewVerificationToken,
                        _inputStageReviewNonProductionContext, current, true, out reason))
                    return false;
                savedVerification = new InputStageReviewSavedVerification
                {
                    VerificationId = _inputStageReviewVerificationToken,
                    Context = _inputStageReviewNonProductionContext.Clone(),
                    Measurements = new List<InputStageReviewMeasurement>()
                };
                return true;
            }
            if (!InputStageReviewGeometryPolicy.IsEvidenceUsable(_inputStageReviewEvidence, current,
                !current.IsSimulation, out reason))
                return false;
            savedVerification = new InputStageReviewSavedVerification
            {
                VerificationId = _inputStageReviewVerificationToken,
                Context = _inputStageReviewEvidence.Context,
                Tolerance = _inputStageReviewEvidence.Tolerance,
                Measurements = _inputStageReviewEvidence.Measurements.Select(item => item.Clone()).ToList()
            };
            return true;
        }

        private static bool IsInputStageReviewGeometryApprovalUsableNoLock(WaferMaterial wafer, DieMap map, out string reason)
        {
            if (wafer == null || _inputStageReviewPendingSave.Contains(wafer.WaferInstanceId ?? ""))
                return FailInputStageReviewGeometry("Review 승인 자료 저장 완료를 기다리고 있습니다.", out reason);
            InputStageReviewSavedVerification saved = wafer.InputStageReviewVerification;
            if (saved == null || saved.Context == null || string.IsNullOrWhiteSpace(saved.VerificationId))
                return FailInputStageReviewGeometry("저장된 다점 좌표 검증 증거가 없습니다. VERIFY MAP이 필요합니다.", out reason);
            if (!CheckInputStageReviewLiveCoordinatesNoLock(wafer, map, out reason))
                return false;
            InputStageReviewGeometryContext current;
            if (!TryBuildInputStageReviewContextNoLock(wafer, map, saved.Context.SessionGeneration,
                saved.Context.RequestGeneration, false, out current, out reason))
                return false;
            if (saved.VerificationId.StartsWith("NONPRODUCTION-MANUAL-", StringComparison.Ordinal))
                return IsStoredInputStageResultModeUsableNoLock(wafer, true, out reason) &&
                    IsInputStageReviewNonProductionMode(out reason) &&
                    CheckInputStageReviewNonProductionContext(saved.VerificationId, saved.Context, current, true, out reason);
            if (saved.VerificationId.StartsWith("SIMULATION-DEFAULT-", StringComparison.Ordinal))
            {
                AppSettings settings = AppSettingsStore.Current;
                return settings != null && settings.SimulationMode && current.IsSimulation &&
                    InputStageReviewGeometryPolicy.IsSameContext(saved.Context, current, out reason);
            }
            InputStageReviewGeometryEvidence evidence;
            return InputStageReviewGeometryPolicy.TryVerify(saved.Context, saved.Measurements, saved.Tolerance, out evidence, out reason) &&
                InputStageReviewGeometryPolicy.IsEvidenceUsable(evidence, current, !current.IsSimulation, out reason);
        }

        private static bool CheckInputStageReviewNonProductionContext(
            string verificationId, InputStageReviewGeometryContext saved, InputStageReviewGeometryContext current,
            bool explicitNonProductionMode, out string reason)
        {
            if (!explicitNonProductionMode || string.IsNullOrWhiteSpace(verificationId) ||
                !verificationId.StartsWith("NONPRODUCTION-MANUAL-", StringComparison.Ordinal) ||
                saved == null || current == null || !saved.IsSimulation || !current.IsSimulation)
                return FailInputStageReviewGeometry("비실운전 수동 승인은 동일한 비실운전 조건에서만 사용할 수 있습니다.", out reason);
            return InputStageReviewGeometryPolicy.IsSameContext(saved, current, out reason);
        }

        private static bool TryCompleteInputStageReviewCommitSave(
            WaferMaterial wafer, string committedVerificationId, out string reason)
        {
            if (string.IsNullOrWhiteSpace(committedVerificationId))
                return FailInputStageReviewGeometry("저장할 Review 검증 식별자가 없습니다.", out reason);
            // 저장 IO는 Material 잠금 밖에서 수행하고, 저장 완료 전에는 승인 소비를 막는다.
            bool durable = TryFlushPendingSave("InputStageRunReviewCommit");
            lock (_stateSync)
            {
                string key = wafer.WaferInstanceId ?? "";
                InputStageReviewSavedVerification saved = wafer.InputStageReviewVerification;
                if (saved == null || !string.Equals(saved.VerificationId, committedVerificationId, StringComparison.Ordinal))
                {
                    // 다른 확정이 더 새 증거를 등록했다면 그 승인/저장 대기 상태를 이전 작업이 해제하지 않는다.
                    return FailInputStageReviewGeometry("저장 중 Review 검증 대상이 변경되었습니다. 이전 확정을 중단했습니다.", out reason);
                }
                if (durable)
                {
                    _inputStageReviewPendingSave.Remove(key);
                    // 저장 대기 중 생성된 미승인 순서 캐시는 완료 후 현재 증거로 다시 판단한다.
                    InvalidateInputPickContextCacheNoLock();
                    WaferMaterial current = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    bool sameRequest = saved != null && saved.Context != null &&
                        ((saved.VerificationId ?? "").StartsWith("SIMULATION-DEFAULT-", StringComparison.Ordinal) ||
                         (saved.Context.SessionGeneration == _inputStageReviewSession && saved.Context.RequestGeneration == _inputStageReviewRequest));
                    if (ReferenceEquals(current, wafer) && sameRequest &&
                        IsInputStageReviewGeometryApprovalUsableNoLock(wafer, BuildDieMapFromWaferNoLock(wafer), out reason))
                        return true;
                }
                wafer.HasInputStageRunReviewApproval = false;
                wafer.InputStageReviewVerification = null;
                wafer.UpdatedAt = DateTime.Now;
                _inputStageReviewPendingSave.Remove(key);
                InvalidateInputPickContextCacheNoLock();
                TryNotifyAndSave("InputStageRunReviewCommitNotReady");
                return FailInputStageReviewGeometry("Review 저장 또는 저장 중 좌표 조건 확인에 실패했습니다. 좌표는 보존하고 승인을 해제했습니다.", out reason);
            }
        }

        private static bool CheckInputStageReviewLiveCoordinatesNoLock(WaferMaterial wafer, DieMap map, out string reason)
        {
            if (map == null || map.Entries == null || !wafer.HasInputStageDieMappingOrigin ||
                map.OriginX != wafer.InputStageDieMappingOriginX || map.OriginY != wafer.InputStageDieMappingOriginY ||
                map.PitchX != wafer.InputStageAlignPitchX || map.PitchY != wafer.InputStageAlignPitchY)
                return FailInputStageReviewGeometry("현재 Material 원점/pitch와 승인 소비 맵이 다릅니다.", out reason);
            // 캐시 hit에서도 실제 Die 좌표를 비교한다. 정상 픽 진행의 상태/위치 변경은 비교하지 않는다.
            foreach (DieMapEntry entry in map.Entries)
            {
                DieMaterial die = entry != null ? FindDieByIdNoLock(entry.DieUid) : null;
                if (die == null || die.Wafer_IndexX != entry.DieMapX || die.Wafer_IndexY != entry.DieMapY)
                    return FailInputStageReviewGeometry("현재 Material Die/index가 승인 좌표와 다릅니다.", out reason);
                double liveX = die.WaferOffset != null && die.WaferOffset.IsValid
                    ? die.WaferOffset.X : wafer.InputStageDieMappingOriginX + wafer.InputStageAlignPitchX * die.Wafer_IndexX;
                double liveY = die.WaferOffset != null && die.WaferOffset.IsValid
                    ? die.WaferOffset.Y : wafer.InputStageDieMappingOriginY + wafer.InputStageAlignPitchY * die.Wafer_IndexY;
                if (liveX != entry.PosX || liveY != entry.PosY)
                    return FailInputStageReviewGeometry("현재 Material Die 좌표가 승인 소비 맵과 다릅니다. die=" + die.DieId, out reason);
            }
            reason = "현재 Material Die 좌표가 승인 소비 맵과 일치합니다.";
            return true;
        }

        private static bool CheckInputStageReviewVerificationDieNoLock(WaferMaterial wafer, string dieUid, out string reason)
        {
            DieMaterial die = wafer == null || string.IsNullOrWhiteSpace(dieUid) ? null :
                ResolveWaferDies(wafer).FirstOrDefault(item => item != null &&
                    string.Equals(item.DieId, dieUid, StringComparison.OrdinalIgnoreCase));
            return CheckInputStageReviewVerificationDieState(die, out reason);
        }

        private static bool CheckInputStageReviewVerificationDieState(DieMaterial die, out string reason)
        {
            if (die == null || die.CurrentLocation == null || die.CurrentLocation.Kind != MaterialLocationKind.InputStage)
                return FailInputStageReviewGeometry("검증점 Die가 현재 InputStage에 없습니다. 남아 있는 WAIT Die를 선택하세요.", out reason);
            if (!die.IsInputTarget || die.Result == DieResult.Good || die.Result == DieResult.NG ||
                IsDieReservedForPicker(die) || HasInputPickCompletedHistory(die))
                return FailInputStageReviewGeometry("완료/제외/예약/픽업된 Die는 검증점으로 사용할 수 없습니다. die=" + die.DieId, out reason);
            reason = "검증점 Die가 현재 InputStage의 미예약 WAIT 상태입니다.";
            return true;
        }

        private static bool CheckInputStageReviewMeasurementIdentity(DieMap map, IList<InputStageReviewMeasurement> samples, out string reason)
        {
            if (map == null || map.Entries == null || samples == null)
                return FailInputStageReviewGeometry("검증점 또는 후보 맵이 없습니다.", out reason);
            foreach (InputStageReviewMeasurement point in samples)
            {
                List<DieMapEntry> entries = point == null ? new List<DieMapEntry>() : map.Entries.Where(entry => entry != null &&
                    string.Equals(entry.DieUid, point.DieUid, StringComparison.OrdinalIgnoreCase)).ToList();
                if (entries.Count != 1 || entries[0].DieMapX != point.GridX || entries[0].DieMapY != point.GridY ||
                    Math.Abs(entries[0].PosX - point.ExpectedX) > 0.000001 || Math.Abs(entries[0].PosY - point.ExpectedY) > 0.000001)
                    return FailInputStageReviewGeometry("검증점 UID/index/예상 위치가 후보 맵과 다릅니다.", out reason);
            }
            reason = "검증점이 후보 맵에 대응합니다.";
            return true;
        }

        private static bool CheckInputStageReviewStationaryTheta(WaferMaterial wafer, out string reason)
        {
            CDT320_Machine machine = CalibrationCoordinateService.ResolveMachine();
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (stage == null || stage.CameraX == null || stage.StageY == null || stage.StageT == null ||
                stage.CameraX.IsMoving || stage.StageY.IsMoving || stage.StageT.IsMoving ||
                !stage.CameraX.IsInPosition || !stage.StageY.IsInPosition || !stage.StageT.IsInPosition ||
                stage.CameraX.IsAlarm || stage.StageY.IsAlarm || stage.StageT.IsAlarm)
                return FailInputStageReviewGeometry("Review 검출/검증 전에 Camera X, Stage Y/T 정지 상태를 확인하세요.", out reason);
            if (!IsFiniteInputStageReviewNumber(stage.CameraX.ActualPosition) ||
                !IsFiniteInputStageReviewNumber(stage.StageY.ActualPosition))
                return FailInputStageReviewGeometry("Review 촬영/확정 시 실제 X/Y 좌표를 읽을 수 없습니다.", out reason);
            double actual = stage.StageT.ActualPosition;
            double difference = (actual - wafer.InputStageDieMappingCorrectedT) % 360.0;
            if (difference > 180.0) difference -= 360.0;
            if (difference < -180.0) difference += 360.0;
            double tolerance = ResolveInputStageReviewActualThetaTolerance();
            if (!IsFiniteInputStageReviewNumber(tolerance) || double.IsNaN(difference) || double.IsInfinity(difference) || Math.Abs(difference) > tolerance)
                return FailInputStageReviewGeometry("실제 Stage T가 Mapping T와 다릅니다. T 저장 후 재매핑이 필요합니다.", out reason);
            reason = "최종 Mapping T에서 축이 정지했습니다.";
            return true;
        }

        private static double ResolveInputStageReviewActualThetaTolerance()
        {
            CDT320_Machine machine = CalibrationCoordinateService.ResolveMachine();
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            double tolerance = stage != null && stage.StageT != null && stage.StageT.Config != null
                ? stage.StageT.Config.InPositionTolerance : double.NaN;
            // 저장 T 두 값의 동일성 공차와 실제 축의 InPosition 허용치를 구분한다.
            return IsFiniteInputStageReviewNumber(tolerance) && tolerance >= 0.0
                ? Math.Max(0.000001, tolerance) : double.NaN;
        }

        private static InputStageConfig ResolveInputStageReviewConfig()
        {
            CDT320_Machine machine = CalibrationCoordinateService.ResolveMachine();
            return machine != null && machine.InputStageUnit != null ? machine.InputStageUnit.Config : null;
        }

        private static string BuildInputStageReviewConditionSignature(WaferMaterial wafer)
        {
            CDT320_Machine machine = CalibrationCoordinateService.ResolveMachine();
            RecipeProject recipe = RecipeStore.LoadLastOrDefaultCached();
            InputStageConfig config = ResolveInputStageReviewConfig();
            CalibrationData calibration = machine != null && machine.VisionUnit != null && machine.VisionUnit.Config != null
                ? machine.VisionUnit.Config.CalibrationData : null;
            if (machine == null || recipe == null || config == null || calibration == null || calibration.Camera == null)
                return null;
            bool nonProduction;
            string runtimeModeSignature;
            string runtimeModeReason;
            if (!TryResolveInputStageReviewRuntimeMode(out nonProduction, out runtimeModeSignature, out runtimeModeReason))
                return null;
            var text = new StringBuilder();
            AppendInputStageReviewValue(text, runtimeModeSignature);
            AppendInputStageReviewValue(text, wafer.WaferInstanceId, wafer.InputStageProcessingGeneration,
                wafer.InputStageAlignResultRunId, wafer.InputStageDieMappingAlignRunId, wafer.InputStageAlignResultMode,
                wafer.InputStageDieMappingResultMode, wafer.InputMapApprovalHashAtMapping,
                wafer.InputStageAlignCorrectedT, wafer.InputStageDieMappingCorrectedT,
                wafer.InputStageAlignPitchX, wafer.InputStageAlignPitchY, wafer.InputStageDieSizeX, wafer.InputStageDieSizeY,
                recipe.FileName, recipe.MapDirection, recipe.MapApprovalVersion, recipe.InputMapApprovalHash,
                recipe.InputDieMapFileName, recipe.InputBaseWaferMapFileName,
                IsInputStageReviewSimulation(wafer),
                config.ManualDieDetectOffsetLimitX, config.ManualDieDetectOffsetLimitY,
                config.ManualDieDetectCumulativeOffsetLimitX, config.ManualDieDetectCumulativeOffsetLimitY,
                config.AlignCenterToleranceMm, config.AlignPitchCompareToleranceMm, config.MaxEffectiveThetaToleranceDeg,
                ResolveInputStageReviewActualThetaTolerance(),
                calibration.Camera.InputToBottomOffsetX, calibration.Camera.InputToBottomOffsetY, calibration.Camera.Valid);
            AppendInputStageReviewSerialized(text, recipe.Die);
            AppendInputStageReviewSerialized(text, recipe.InputFrame ?? recipe.Frame);
            AppendInputStageReviewSerialized(text, machine.InputStageUnit.Recipe != null ? machine.InputStageUnit.Recipe.DieMap : null);
            VisionCameraPixelCalibration camera = calibration.Camera.InputCamera;
            if (camera == null)
                return null;
            // 촬영마다 바뀌는 ResolutionUpdatedAt은 geometry 변경이 아니다. 실제 변환값만 비교한다.
            AppendInputStageReviewValue(text, camera.ImageWidthPixel, camera.ImageHeightPixel,
                camera.ImageCenterPixelX, camera.ImageCenterPixelY, camera.PixelToMmX, camera.PixelToMmY);
            return HashInputStageReviewText(text.ToString());
        }

        private static string BuildInputStageReviewGeometrySignature(DieMap map)
        {
            if (map == null || map.Entries == null)
                return "";
            var text = new StringBuilder();
            AppendInputStageReviewValue(text, map.FrameObjId, map.DieMapX, map.DieMapY, map.PitchX, map.PitchY,
                map.DieSizeX, map.DieSizeY, map.OuterDiameterMm, map.OriginX, map.OriginY);
            // 생산 진행 중 Result/IsTarget/순번은 변한다. 물리 UID와 좌표만 검증 증거에 연결한다.
            foreach (DieMapEntry entry in map.Entries.Where(item => item != null).OrderBy(item => item.DieUid, StringComparer.Ordinal))
                AppendInputStageReviewValue(text, entry.DieUid, entry.Index, entry.DieMapX, entry.DieMapY,
                    entry.OriginalMapX, entry.OriginalMapY, entry.PosX, entry.PosY);
            return HashInputStageReviewText(text.ToString());
        }

        private static void AppendInputStageReviewValue(StringBuilder text, params object[] values)
        {
            foreach (object value in values)
            {
                string item = value is double ? ((double)value).ToString("R", CultureInfo.InvariantCulture)
                    : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                text.Append(item.Length).Append(':').Append(item).Append('|');
            }
        }

        private static void AppendInputStageReviewSerialized(StringBuilder text, object value)
        {
            if (value == null)
            {
                text.Append("null|");
                return;
            }
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(value.GetType()).WriteObject(stream, value);
                text.Append(Convert.ToBase64String(stream.ToArray())).Append('|');
            }
        }

        private static string HashInputStageReviewText(string text)
        {
            using (SHA256 hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        private static bool FailInputStageReviewGeometry(string message, out string reason)
        {
            reason = message;
            return false;
        }
    }
}
