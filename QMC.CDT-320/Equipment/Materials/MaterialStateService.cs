using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QMC.Common;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;

namespace QMC.CDT320.Materials
{
    public static class MaterialStateService
    {
        public static event Action<MaterialSnapshot> StateChanged;
        private static readonly object _stateSync = new object();
        private static readonly object _saveRequestSync = new object();
        private static readonly object _saveIoSync = new object();
        private static readonly object _stateChangedSync = new object();
        // Material snapshot is large during auto run. Keep UI/state events responsive,
        // but throttle full JSON disk saves so every die/inspection update does not
        // serialize/validate/replace the 10MB+ state file.
        private const int MaterialSaveQuietMs = 1000;
        private const int MaterialSaveMinimumIntervalMs = 5000;
        private const int MaterialStateChangedQuietMs = 200;
        private const double InputStageThetaOffsetReadyEpsilon = 0.000001;
        private const double InputStageThetaMappingSnapshotToleranceDeg = 0.000001;
        private const double ProcessTestThetaAlignOffsetDeg = 0.000010;
        private static bool _saveWorkerRunning;
        private static bool _saveRequested;
        private static string _pendingSaveReason = "";
        private static DateTime _lastSaveCompletedUtc = DateTime.MinValue;
        private static bool _lastSaveSucceeded;
        private static bool _stateChangedQueued;
        private static DateTime _lastStateChangedAt = DateTime.MinValue;

        public static MaterialSnapshot State => MaterialStorage.State;

        private static string CreateWaferInstanceId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static string EnsureWaferInstanceIdNoLock(WaferMaterial wafer)
        {
            if (wafer == null)
                return "";

            string instanceId = (wafer.WaferInstanceId ?? "").Trim();
            Guid parsed;
            if (string.IsNullOrWhiteSpace(instanceId) ||
                !Guid.TryParseExact(instanceId, "N", out parsed))
            {
                instanceId = CreateWaferInstanceId();
                wafer.WaferInstanceId = instanceId;
                wafer.UpdatedAt = DateTime.Now;
            }

            return instanceId;
        }

        public static string EnsureWaferInstanceId(WaferMaterial wafer)
        {
            lock (_stateSync)
            {
                return EnsureWaferInstanceIdNoLock(wafer);
            }
        }

        public static bool IsSameWaferInstance(WaferMaterial left, WaferMaterial right)
        {
            if (left == null || right == null)
                return false;

            string leftInstance = EnsureWaferInstanceId(left);
            string rightInstance = EnsureWaferInstanceId(right);
            return !string.IsNullOrWhiteSpace(leftInstance) &&
                   string.Equals(leftInstance, rightInstance, StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildPhysicalDieId(WaferMaterial wafer, int originalMapX, int originalMapY)
        {
            string instanceId = EnsureWaferInstanceId(wafer);
            if (string.IsNullOrWhiteSpace(instanceId))
                throw new InvalidOperationException("물리 Die ID를 만들 Wafer instance가 없습니다.");

            return "D" + instanceId +
                   "X" + originalMapX.ToString("D6", CultureInfo.InvariantCulture) +
                   "Y" + originalMapY.ToString("D6", CultureInfo.InvariantCulture);
        }

        public static bool TryAssignPhysicalDieIds(DieMap map, WaferMaterial wafer, out string reason)
        {
            reason = "";
            if (map == null || map.Entries == null)
            {
                reason = "Input Die Map이 없습니다.";
                return false;
            }
            if (wafer == null)
            {
                reason = "Input Wafer Material이 없습니다.";
                return false;
            }

            var addresses = new HashSet<string>(StringComparer.Ordinal);
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;

                int originalX = DieMapGenerator.ResolveOriginalMapIndexX(entry);
                int originalY = DieMapGenerator.ResolveOriginalMapIndexY(entry);
                string address = originalX.ToString(CultureInfo.InvariantCulture) + "," +
                                 originalY.ToString(CultureInfo.InvariantCulture);
                if (!addresses.Add(address))
                {
                    reason = "Input Die Map의 원본 좌표가 중복되었습니다. original=(" +
                             originalX + "," + originalY + ")";
                    return false;
                }
            }

            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;

                int originalX = DieMapGenerator.ResolveOriginalMapIndexX(entry);
                int originalY = DieMapGenerator.ResolveOriginalMapIndexY(entry);
                entry.DieUid = BuildPhysicalDieId(wafer, originalX, originalY);
                entry.OriginalMapX = originalX;
                entry.OriginalMapY = originalY;
            }

            return true;
        }

        private static string BuildOutputPlacementUid(WaferMaterial outputWafer, int orderIndex)
        {
            string instanceId = EnsureWaferInstanceIdNoLock(outputWafer);
            return string.IsNullOrWhiteSpace(instanceId)
                ? ""
                : "P" + instanceId + "O" + orderIndex.ToString("D6", CultureInfo.InvariantCulture);
        }

        public static string GetProductionLotId()
        {
            lock (_stateSync)
            {
                return State != null && !string.IsNullOrWhiteSpace(State.LotId)
                    ? State.LotId.Trim()
                    : "";
            }
        }

        /// <summary>
        /// 생산 LOT ID를 설정한다.
        /// [신규 2026-07-27] 기존에는 읽기(GetProductionLotId)만 있고 설정 경로가 없어,
        /// LOT ID가 최초 기동 시 레시피(RecipeProject.LotId)에서 한 번 복사되는 것 외에는 바꿀 수 없었다.
        /// 운전 중 LOT 시작/완료를 지원하기 위해 설정 API를 연다.
        /// 이 값 하나만 바꾸면 TactTime CSV / 웨이퍼·검사 CSV / 비전 요청 전문 / 생산통계 / 화면 표시가
        /// 모두 GetProductionLotId()를 통해 자동으로 따라온다(시퀀스 수정 불필요).
        ///
        /// 공백/null 정책: 빈 값은 "LOT 미지정"을 뜻하는 빈 문자열로 정규화해 저장한다.
        /// (LOT 완료 후 미지정 상태로 되돌릴 때 사용한다. 시작 시의 빈 값 거부는 호출자인
        ///  LotSessionService.TryStartLot에서 처리한다.)
        /// </summary>
        /// <returns>값이 실제로 바뀌었으면 true.</returns>
        public static bool SetProductionLotId(string lotId, string reason)
        {
            string normalized = string.IsNullOrWhiteSpace(lotId) ? "" : lotId.Trim();
            string previous;

            lock (_stateSync)
            {
                if (State == null)
                    return false;

                previous = State.LotId ?? "";
                if (string.Equals(previous, normalized, StringComparison.Ordinal))
                    return false;

                State.LotId = normalized;
            }

            NotifyAndSave(string.IsNullOrWhiteSpace(reason) ? "SetProductionLotId" : reason);
            Log.Write("Main", "SYSTEM", "SetProductionLotId",
                "생산 LOT ID를 변경했습니다. 이전=" + (string.IsNullOrEmpty(previous) ? "(없음)" : previous) +
                ", 이후=" + (string.IsNullOrEmpty(normalized) ? "(없음)" : normalized) +
                ", reason=" + (reason ?? "") + " - Ok");
            return true;
        }

        public static void InitializeForRecipe(int inputLevelCount, int goodLevelCount, int inputSlots, int outputSlots)
        {
            MaterialStorage.InitializeDefaultState(inputLevelCount, goodLevelCount, inputSlots, outputSlots);
            NotifyAndSave("InitializeForRecipe");
        }

        public static void UpdateRecipeContext(string recipeName, string reason)
        {
            string normalizedRecipeName = string.IsNullOrWhiteSpace(recipeName) ? "" : recipeName.Trim();
            bool changed;
            lock (_stateSync)
            {
                changed = !string.Equals(State.RecipeName ?? "", normalizedRecipeName, StringComparison.OrdinalIgnoreCase);
                State.RecipeName = normalizedRecipeName;
            }

            if (changed)
            {
                NotifyAndSave(string.IsNullOrWhiteSpace(reason) ? "UpdateRecipeContext" : reason);
                Log.Write("Main", "SYSTEM", "MaterialRecipeContext",
                    "Material Recipe 문맥을 갱신했습니다. recipe=" + normalizedRecipeName +
                    ", reason=" + (reason ?? "") + " - Ok");
            }
        }

        public static WaferMaterial GetOrCreateWafer(string waferId)
        {
            if (string.IsNullOrEmpty(waferId))
                waferId = "WAFER-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");

            List<WaferMaterial> candidates = State.Wafers
                .Where(w =>
                    w != null &&
                    string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (candidates.Count > 1)
            {
                throw new InvalidOperationException(
                    "같은 표시 Wafer/Bin ID의 물리 Material이 둘 이상이므로 임의로 선택할 수 없습니다. wafer=" +
                    waferId);
            }

            WaferMaterial wafer = candidates.Count == 1 ? candidates[0] : null;
            if (wafer != null)
            {
                EnsureWaferInstanceIdNoLock(wafer);
                return wafer;
            }

            wafer = new WaferMaterial
            {
                WaferId = waferId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            State.Wafers.Add(wafer);
            NotifyAndSave("CreateWafer");
            return wafer;
        }

        public static DieMaterial GetOrCreateDieMaterial(string dieId)
        {
            if (string.IsNullOrEmpty(dieId))
                dieId = Guid.NewGuid().ToString("N").Substring(0, 12);

            List<DieMaterial> candidates = State.Dies
                .Where(d =>
                    d != null &&
                    string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (candidates.Count > 1)
            {
                throw new InvalidOperationException(
                    "같은 Die ID의 물리 Material이 둘 이상이므로 임의로 선택할 수 없습니다. dieId=" +
                    dieId);
            }
            if (candidates.Count == 1)
                return candidates[0];

            DieMaterial die = new DieMaterial
            {
                DieId = dieId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            State.Dies.Add(die);
            NotifyAndSave("CreateDie");
            return die;
        }

        public static int ClearInputDieMaterialsForWafer(string waferId, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(waferId))
                    return 0;

                lock (_stateSync)
                {
                    WaferMaterial wafer = State.Wafers.FirstOrDefault(w =>
                        w != null &&
                        string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase));
                    string instanceId = wafer != null ? EnsureWaferInstanceIdNoLock(wafer) : "";
                    int removed = State.Dies.RemoveAll(d =>
                        IsInputOnlyDieForWaferInstanceNoLock(d, waferId, instanceId));

                    if (removed > 0)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Cleared previous input die materials for wafer. wafer=" + waferId +
                            ", removed=" + removed +
                            ", reason=" + (reason ?? "") + " - Ok");
                    }

                    return removed;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Clear input die materials failed. wafer=" + waferId +
                    ", reason=" + (reason ?? "") +
                    ", error=" + ex.Message + " - Failed");
                return 0;
            }
            finally
            {
            }
        }

        public static int ClearStaleInputDieMaterialsForWafer(WaferMaterial wafer, ICollection<string> activeDieIds, string reason)
        {
            try
            {
                if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                    return 0;

                lock (_stateSync)
                {
                    string waferId = wafer.WaferId;
                    string instanceId = EnsureWaferInstanceIdNoLock(wafer);
                    int removed = State.Dies.RemoveAll(d =>
                        IsInputOnlyDieForWaferInstanceNoLock(d, waferId, instanceId) &&
                        (activeDieIds == null ||
                         string.IsNullOrWhiteSpace(d.DieId) ||
                         !activeDieIds.Contains(d.DieId)));

                    if (removed > 0)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Cleared stale input die materials for wafer. wafer=" + waferId +
                            ", removed=" + removed +
                            ", reason=" + (reason ?? "") + " - Ok");
                    }

                    return removed;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Clear stale input die materials failed. wafer=" + (wafer != null ? wafer.WaferId : "") +
                    ", reason=" + (reason ?? "") +
                    ", error=" + ex.Message + " - Failed");
                return 0;
            }
            finally
            {
            }
        }

        private static bool IsInputOnlyDieForWaferInstanceNoLock(
            DieMaterial die,
            string waferId,
            string waferInstanceId)
        {
            if (die == null)
                return false;

            bool sameInputInstance =
                !string.IsNullOrWhiteSpace(waferInstanceId) &&
                !string.IsNullOrWhiteSpace(die.InputWaferInstanceId)
                ? string.Equals(
                    die.InputWaferInstanceId ?? "",
                    waferInstanceId,
                    StringComparison.OrdinalIgnoreCase)
                : string.Equals(
                    die.WaferID_Input ?? "",
                    waferId ?? "",
                    StringComparison.OrdinalIgnoreCase);
            if (!sameInputInstance || !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId))
                return false;

            MaterialLocationKind kind = die.CurrentLocation != null
                ? die.CurrentLocation.Kind
                : MaterialLocationKind.Unknown;
            return kind == MaterialLocationKind.Unknown ||
                   kind == MaterialLocationKind.InputCassette ||
                   kind == MaterialLocationKind.InputStage;
        }

        private static bool IsDieRelatedToWaferInstanceNoLock(
            DieMaterial die,
            WaferMaterial wafer)
        {
            if (die == null || wafer == null)
                return false;

            string waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
            bool hasInputInstance = !string.IsNullOrWhiteSpace(die.InputWaferInstanceId);
            bool hasOutputInstance = !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId);
            if (hasInputInstance || hasOutputInstance)
            {
                return string.Equals(
                           die.InputWaferInstanceId ?? "",
                           waferInstanceId,
                           StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(
                           die.OutputWaferInstanceId ?? "",
                           waferInstanceId,
                           StringComparison.OrdinalIgnoreCase);
            }

            string waferId = wafer.WaferId ?? "";
            return string.Equals(
                       die.WaferID_Input ?? "",
                       waferId,
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       die.WaferID_Output ?? "",
                       waferId,
                       StringComparison.OrdinalIgnoreCase);
        }

        public static DieMaterial GetDieMaterial(string dieId)
        {
            try
            {
                lock (_stateSync)
                {
                    if (string.IsNullOrWhiteSpace(dieId))
                        return null;

                    return State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Get die material failed: dieId=" + dieId +
                    ", error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static bool ApplyManualDieState(
            string dieId,
            bool isInputTarget,
            DieResult result,
            int binCode,
            string ngCode,
            string reason,
            ManualDieStateSyncScope syncScope,
            out string message)
        {
            message = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(dieId))
                {
                    message = "Die ID가 비어 있습니다.";
                    return false;
                }

                lock (_stateSync)
                {
                    List<DieMaterial> candidates = State.Dies
                        .Where(d =>
                            d != null &&
                            string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase))
                        .Take(2)
                        .ToList();
                    if (candidates.Count == 0)
                    {
                        message = "Die 정보를 찾을 수 없습니다. dieId=" + dieId;
                        return false;
                    }
                    if (candidates.Count > 1)
                    {
                        message = "같은 Die ID의 물리 Material이 둘 이상이므로 상태를 변경할 수 없습니다. dieId=" + dieId;
                        return false;
                    }

                    DieMaterial die = candidates[0];

                    if (syncScope == ManualDieStateSyncScope.InputMapOnly)
                    {
                        SyncManualDieStateTargetsNoLock(die, isInputTarget, result, binCode, syncScope);

                        MaterialLocationKind locationKind = die.CurrentLocation != null
                            ? die.CurrentLocation.Kind
                            : MaterialLocationKind.Unknown;
                        WaferMaterial inputWafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                        string inputWaferInstanceId = inputWafer != null
                            ? EnsureWaferInstanceIdNoLock(inputWafer)
                            : "";
                        bool isCurrentInputDie =
                            !string.IsNullOrWhiteSpace(inputWaferInstanceId) &&
                            string.Equals(
                                die.InputWaferInstanceId ?? "",
                                inputWaferInstanceId,
                                StringComparison.OrdinalIgnoreCase);
                        bool isStillInInputMapArea =
                            locationKind == MaterialLocationKind.InputStage ||
                            locationKind == MaterialLocationKind.Unknown;

                        // 이미 Picker/Output으로 이동한 동일 물리 Die의 이력은 Input Map 수동 편집으로 되돌리지 않는다.
                        if (isCurrentInputDie && isStillInInputMapArea)
                            ApplyManualDieStateNoLock(die, isInputTarget, result, binCode, ngCode);
                    }
                    else
                    {
                        ApplyManualDieStateNoLock(die, isInputTarget, result, binCode, ngCode);
                        SyncManualDieStateTargetsNoLock(die, isInputTarget, result, binCode, syncScope);
                    }
                }

                NotifyAndSave("ManualDieStateSync:" + dieId);
                Log.Write("Main", "MATERIAL", "ManualDieStateSync",
                    "Manual die state synchronized. dieId=" + dieId +
                     ", result=" + result +
                     ", isInputTarget=" + isInputTarget +
                     ", binCode=" + binCode +
                     ", scope=" + syncScope +
                     ", reason=" + (reason ?? "") + " - Ok");

                message = "Die 상태를 동기화했습니다. dieId=" + dieId;
                return true;
            }
            catch (Exception ex)
            {
                message = "Die 상태 동기화 실패: " + ex.Message;
                Log.Write("Main", "MATERIAL", "ManualDieStateSync",
                    "Manual die state sync failed. dieId=" + dieId +
                    ", result=" + result +
                    ", isInputTarget=" + isInputTarget +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static DieMaterial GetDieAtPicker(MaterialLocationKind pickerLocation, int pickerNo)
        {
            try
            {
                lock (_stateSync)
                {
                    return State.Dies
                        .Where(d =>
                            d != null &&
                            d.CurrentLocation != null &&
                            d.CurrentLocation.Kind == pickerLocation &&
                            d.CurrentLocation.PickerNo == pickerNo)
                        .OrderByDescending(GetPickerDieSortTime)
                        .ThenByDescending(d => d.InputSequenceNo)
                        .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Get die at picker failed: pickerLocation=" + pickerLocation +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static DateTime GetPickerDieSortTime(DieMaterial die)
        {
            if (die == null)
                return DateTime.MinValue;

            DateTime updated = die.UpdatedAt;
            DateTime picked = die.PickedAt;
            return updated >= picked ? updated : picked;
        }

        public static void ApplyDieInspectionResult(string dieId, DieResult result, string ngCode, string reason)
        {
            try
            {
                lock (_stateSync)
                {
                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));

                    if (die == null)
                        return;

                    MaterialLocation previousLocation = die.CurrentLocation;
                    die.Result = result;
                    if (result == DieResult.NG && !string.IsNullOrWhiteSpace(ngCode))
                    {
                        if (die.NgCodes == null)
                            die.NgCodes = new List<string>();
                        if (!die.NgCodes.Contains(ngCode))
                            die.NgCodes.Add(ngCode);
                    }

                    die.UpdatedAt = DateTime.Now;
                    SequenceTrace.MaterialChange(
                        "ApplyDieInspectionResult",
                        "die=" + die.DieId,
                        "from=" + previousLocation,
                        "to=" + die.CurrentLocation,
                        "result=" + die.Result,
                        "ngCode=" + ngCode);
                    InputWaferInspectionCsvSnapshotWriter.EnqueueInspection(
                        "InspectionResult",
                        State != null ? State.RecipeName : "",
                        GetProductionLotId(),
                        die,
                        null);
                    NotifyAndSave(reason);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Apply die inspection result failed: dieId=" + dieId +
                    ", result=" + result +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static bool UpdatePickerDieManualState(
            MaterialLocationKind pickerLocation,
            int pickerNo,
            DieResult result,
            bool isInputTarget,
            string ngCode,
            string reason,
            out string message)
        {
            message = string.Empty;
            string dieId = string.Empty;
            try
            {
                if (!IsPickerLocation(pickerLocation))
                {
                    message = "Picker 위치가 올바르지 않습니다. location=" + pickerLocation;
                    return false;
                }

                if (pickerNo < 1 || pickerNo > 4)
                {
                    message = "Picker 번호가 올바르지 않습니다. pickerNo=" + pickerNo;
                    return false;
                }

                lock (_stateSync)
                {
                    DieMaterial die = FindDieAtPickerNoLock(pickerLocation, pickerNo);
                    if (die == null)
                    {
                        message = "Picker에 Die 정보가 없습니다. location=" + pickerLocation + ", pickerNo=" + pickerNo;
                        return false;
                    }

                    dieId = die.DieId;
                    int binCode = ResolveManualBinCode(result, 0);
                    ApplyManualDieStateNoLock(die, isInputTarget, result, binCode, ngCode);
                    SyncManualDieStateTargetsNoLock(
                        die,
                        isInputTarget,
                        result,
                        binCode,
                        ManualDieStateSyncScope.MaterialOnly);
                    UpsertManualPickerInspectionNoLock(die, result, ngCode, reason);
                }

                NotifyAndSave("ManualPickerDieStateUpdate:" + dieId);
                Log.Write("Main", "MATERIAL", "ManualPickerDieStateUpdate",
                    "Picker die state updated. location=" + pickerLocation +
                    ", pickerNo=" + pickerNo +
                    ", dieId=" + dieId +
                    ", result=" + result +
                    ", isInputTarget=" + isInputTarget +
                    ", reason=" + (reason ?? "") + " - Ok");

                message = "Die 상태를 변경했습니다. dieId=" + dieId;
                return true;
            }
            catch (Exception ex)
            {
                message = "Picker Die 상태 변경 실패: " + ex.Message;
                Log.Write("Main", "MATERIAL", "ManualPickerDieStateUpdate",
                    "Picker die state update failed. location=" + pickerLocation +
                    ", pickerNo=" + pickerNo +
                    ", dieId=" + dieId +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool ClearPickerDieMaterial(
            MaterialLocationKind pickerLocation,
            int pickerNo,
            string reason,
            out string message)
        {
            message = string.Empty;
            string dieId = string.Empty;
            try
            {
                if (!IsPickerLocation(pickerLocation))
                {
                    message = "Picker 위치가 올바르지 않습니다. location=" + pickerLocation;
                    return false;
                }

                if (pickerNo < 1 || pickerNo > 4)
                {
                    message = "Picker 번호가 올바르지 않습니다. pickerNo=" + pickerNo;
                    return false;
                }

                lock (_stateSync)
                {
                    DieMaterial die = FindDieAtPickerNoLock(pickerLocation, pickerNo);
                    if (die == null)
                    {
                        message = "Picker에 제거할 Die 정보가 없습니다. location=" + pickerLocation + ", pickerNo=" + pickerNo;
                        return false;
                    }

                    dieId = die.DieId;
                    die.CurrentLocation = MaterialLocation.Unknown();
                    die.ReservedPickerLocation = MaterialLocationKind.Unknown;
                    die.ReservedPickerNo = -1;
                    die.IsInputTarget = false;
                    die.UpdatedAt = DateTime.Now;
                    UpsertManualPickerInspectionNoLock(die, die.Result, string.Empty, reason);
                }

                NotifyAndSave("ManualPickerDieClear:" + dieId);
                Log.Write("Main", "MATERIAL", "ManualPickerDieClear",
                    "Picker die cleared manually. location=" + pickerLocation +
                    ", pickerNo=" + pickerNo +
                    ", dieId=" + dieId +
                    ", isInputTarget=False" +
                    ", reason=" + (reason ?? "") + " - Ok");

                message = "Picker Die 정보를 제거했습니다. dieId=" + dieId;
                return true;
            }
            catch (Exception ex)
            {
                message = "Picker Die 정보 제거 실패: " + ex.Message;
                Log.Write("Main", "MATERIAL", "ManualPickerDieClear",
                    "Picker die clear failed. location=" + pickerLocation +
                    ", pickerNo=" + pickerNo +
                    ", dieId=" + dieId +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool IsPickerLocation(MaterialLocationKind pickerLocation)
        {
            return pickerLocation == MaterialLocationKind.PickerFront ||
                   pickerLocation == MaterialLocationKind.PickerRear;
        }

        private static void ApplyManualDieStateNoLock(
            DieMaterial die,
            bool isInputTarget,
            DieResult result,
            int binCode,
            string ngCode)
        {
            if (die == null)
                return;

            int normalizedBinCode = ResolveManualBinCode(result, binCode);
            die.Result = result;
            die.IsInputTarget = isInputTarget;
            die.Input_BinCode = isInputTarget ? normalizedBinCode : 0;

            MaterialLocationKind locationKind = die.CurrentLocation != null
                ? die.CurrentLocation.Kind
                : MaterialLocationKind.Unknown;
            if (IsOutputStageLocation(locationKind))
                die.Output_BinCode = normalizedBinCode;

            if (die.NgCodes == null)
                die.NgCodes = new List<string>();

            if (result == DieResult.NG)
            {
                string code = string.IsNullOrWhiteSpace(ngCode) ? "MANUAL-NG" : ngCode.Trim();
                if (!die.NgCodes.Contains(code))
                    die.NgCodes.Add(code);
            }
            else
            {
                die.NgCodes.Clear();
            }

            die.UpdatedAt = DateTime.Now;
        }

        private static void SyncManualDieStateTargetsNoLock(
            DieMaterial die,
            bool isInputTarget,
            DieResult result,
            int binCode,
            ManualDieStateSyncScope syncScope)
        {
            if (die == null)
                return;

            int normalizedBinCode = ResolveManualBinCode(result, binCode);
            if (syncScope == ManualDieStateSyncScope.InputMapOnly)
                SyncActiveInputMapEntryNoLock(die.DieId, isInputTarget, result, normalizedBinCode);
        }

        private static void SyncActiveInputMapEntryNoLock(
            string dieId,
            bool isInputTarget,
            DieResult result,
            int binCode)
        {
            try
            {
                DieMap map = LotStorage.ActiveInputDieMap;
                if (map == null || map.Entries == null || string.IsNullOrWhiteSpace(dieId))
                    return;

                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null ||
                        !string.Equals(entry.DieUid ?? "", dieId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    entry.IsTarget = isInputTarget;
                    entry.Result = isInputTarget ? result : DieResult.Unknown;
                    entry.BinCode = isInputTarget ? binCode : 0;
                    if (!isInputTarget)
                        entry.SequenceNo = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "MATERIAL", "ManualDieStateSync",
                    "Active input map sync failed. dieId=" + dieId +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool IsOutputStageLocation(MaterialLocationKind kind)
        {
            return kind == MaterialLocationKind.OutputStageGood ||
                   kind == MaterialLocationKind.OutputStageNg ||
                   kind == MaterialLocationKind.OutputFeeder ||
                   kind == MaterialLocationKind.OutputCassette;
        }

        /// <summary>
        /// 입력 계열 위치인지 여부(Unknown 포함).
        /// [입출력 분리 2026-07-29] 카세트 Data All Clear 가 WaferId 이름만으로 State.Wafers 전체를 훑어,
        /// 입력 슬롯 ID 와 겹치는 출력 Bin 웨이퍼까지 같이 지워버렸다(실장비 발생).
        /// 이름 매칭 대상은 반드시 자기 계열 위치로 제한한다.
        /// Unknown 을 포함하는 이유: 슬롯에는 남아 있으나 위치 추적이 끊긴 자기 계열 웨이퍼를 계속 정리해야 한다.
        /// </summary>
        private static bool IsInputSideLocationForClear(MaterialLocation location)
        {
            if (location == null)
                return true;   // 위치 정보가 없으면 계열 판별 불가 → 기존 동작(정리 대상) 유지

            MaterialLocationKind kind = location.Kind;
            return kind == MaterialLocationKind.Unknown ||
                   kind == MaterialLocationKind.InputCassette ||
                   kind == MaterialLocationKind.InputFeeder ||
                   kind == MaterialLocationKind.InputStage;
        }

        /// <summary>
        /// 출력 계열 위치인지 여부(Unknown 포함). IsInputSideLocationForClear 의 대칭.
        /// </summary>
        private static bool IsOutputSideLocationForClear(MaterialLocation location)
        {
            if (location == null)
                return true;

            MaterialLocationKind kind = location.Kind;
            return kind == MaterialLocationKind.Unknown ||
                   IsOutputStageLocation(kind);
        }

        private static int ResolveManualBinCode(DieResult result, int binCode)
        {
            if (result == DieResult.Good)
                return binCode > 0 ? binCode : BinCodeMap.GoodBin;
            if (result == DieResult.NG)
                return binCode > 0 ? binCode : BinCodeMap.MaxBin;

            return result == DieResult.Unknown && binCode > 0 ? binCode : 0;
        }

        private static DieMaterial FindDieAtPickerNoLock(MaterialLocationKind pickerLocation, int pickerNo)
        {
            return State.Dies
                .Where(d =>
                    d != null &&
                    d.CurrentLocation != null &&
                    d.CurrentLocation.Kind == pickerLocation &&
                    d.CurrentLocation.PickerNo == pickerNo)
                .OrderByDescending(GetPickerDieSortTime)
                .ThenByDescending(d => d.InputSequenceNo)
                .FirstOrDefault();
        }

        private static void UpsertManualPickerInspectionNoLock(
            DieMaterial die,
            DieResult result,
            string ngCode,
            string reason)
        {
            if (die == null)
                return;

            if (die.Inspections == null)
                die.Inspections = new List<DieInspectionRecord>();

            DieInspectionRecord old = die.Inspections.FirstOrDefault(x =>
                x != null &&
                string.Equals(x.InspectionType, "ManualPickerHeadEdit", StringComparison.OrdinalIgnoreCase));
            if (old != null)
                die.Inspections.Remove(old);

            DieInspectionRecord record = new DieInspectionRecord();
            record.InspectionType = "ManualPickerHeadEdit";
            record.Result = ToMaterialInspectionResult(result);
            record.CreatedAt = DateTime.Now;
            record.UpdatedAt = DateTime.Now;
            record.Measurements = new List<InspectionMeasurement>();
            record.NgCodes = new List<string>();

            if (result == DieResult.NG)
                record.NgCodes.Add(string.IsNullOrWhiteSpace(ngCode) ? "MANUAL-NG" : ngCode.Trim());

            if (!string.IsNullOrWhiteSpace(reason))
            {
                record.Measurements.Add(new InspectionMeasurement
                {
                    Name = "ManualEdit",
                    Value = 1,
                    Unit = "",
                    RawValue = reason.Trim(),
                    Result = record.Result
                });
            }

            die.Inspections.Add(record);
        }

        private static void SyncInputPickVisionReviewInspectionNoLock(
            DieMaterial die,
            DieResult result)
        {
            if (die == null)
                return;

            if (die.Inspections == null)
                die.Inspections = new List<DieInspectionRecord>();

            die.Inspections.RemoveAll(existing =>
                existing != null &&
                string.Equals(existing.InspectionType, "InputPickVision", StringComparison.OrdinalIgnoreCase));

            if (result != DieResult.Good && result != DieResult.NG)
                return;

            MaterialInspectionResult inspectionResult = ToMaterialInspectionResult(result);
            var record = new DieInspectionRecord
            {
                InspectionType = "InputPickVision",
                Result = inspectionResult,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Offset = die.WaferOffset != null && die.WaferOffset.IsValid
                    ? new VisionOffset
                    {
                        X = die.WaferOffset.X,
                        Y = die.WaferOffset.Y,
                        R = die.WaferOffset.R,
                        IsValid = true
                    }
                    : new VisionOffset(),
                Measurements = new List<InspectionMeasurement>
                {
                    new InspectionMeasurement
                    {
                        Name = "InputVisionResult",
                        Value = result == DieResult.Good ? 1.0 : 0.0,
                        Unit = "bool",
                        RawValue = result == DieResult.Good
                            ? "ManualInputMapEdit:GOOD"
                            : "ManualInputMapEdit:NG",
                        Result = inspectionResult
                    }
                },
                NgCodes = new List<string>()
            };

            if (result == DieResult.NG)
                record.NgCodes.Add("ManualInputMapEdit");

            die.Inspections.Add(record);
            InputWaferInspectionCsvSnapshotWriter.EnqueueInspection(
                "InputStageRunReview",
                State != null ? State.RecipeName : "",
                GetProductionLotId(),
                die,
                record);
        }

        private static MaterialInspectionResult ToMaterialInspectionResult(DieResult result)
        {
            switch (result)
            {
                case DieResult.Good:
                    return MaterialInspectionResult.Ok;
                case DieResult.NG:
                    return MaterialInspectionResult.Ng;
                default:
                    return MaterialInspectionResult.Unknown;
            }
        }

        public static void UpdateInputCassetteMapping(
            int levelCount,
            int slotCount,
            IReadOnlyList<bool> level1Map,
            IReadOnlyList<bool> level2Map,
            IReadOnlyList<double> level1SlotPositions,
            IReadOnlyList<double> level2SlotPositions,
            string cassetteLotId,
            string tapeFrameSpecName)
        {
            if (levelCount < 1) levelCount = 1;
            if (levelCount > 2) levelCount = 2;

            if (levelCount >= 2 && level2Map == null)
                level2Map = level1Map;

            lock (_stateSync)
            {
                CassetteMaterialRole[] lotRoles = levelCount >= 2
                    ? new[] { CassetteMaterialRole.Input1, CassetteMaterialRole.Input2 }
                    : new[] { CassetteMaterialRole.Input1 };
                string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, lotRoles);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Input1, true, slotCount, level1Map, level1SlotPositions);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Input2, levelCount >= 2, slotCount, level2Map, level2SlotPositions);

                UpdateCassetteMapping(CassetteMaterialRole.Input1, true, slotCount, level1Map, level1SlotPositions, resolvedLotId, tapeFrameSpecName);
                UpdateCassetteMapping(CassetteMaterialRole.Input2, levelCount >= 2, slotCount, level2Map, level2SlotPositions, resolvedLotId, tapeFrameSpecName);

            }
            NotifyAndSave("InputCassetteMapping");
        }

        public static void UpdateOutputCassetteMapping(
            int goodLevelCount,
            int slotCount,
            IReadOnlyList<bool> good1Map,
            IReadOnlyList<bool> good2Map,
            IReadOnlyList<bool> ngMap,
            IReadOnlyList<double> good1SlotPositions,
            IReadOnlyList<double> good2SlotPositions,
            IReadOnlyList<double> ngSlotPositions,
            string cassetteLotId,
            string tapeFrameSpecName)
        {
            if (goodLevelCount < 1) goodLevelCount = 1;
            if (goodLevelCount > 2) goodLevelCount = 2;

            lock (_stateSync)
            {
                CassetteMaterialRole[] lotRoles = goodLevelCount >= 2
                    ? new[] { CassetteMaterialRole.Good1, CassetteMaterialRole.Good2, CassetteMaterialRole.Ng1 }
                    : new[] { CassetteMaterialRole.Good1, CassetteMaterialRole.Ng1 };
                string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, lotRoles);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions);

                UpdateCassetteMapping(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions, resolvedLotId, tapeFrameSpecName);
                UpdateCassetteMapping(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions, resolvedLotId, tapeFrameSpecName);
                UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions, resolvedLotId, tapeFrameSpecName);

            }
            NotifyAndSave("OutputCassetteMapping");
        }

        public static void UpdateOutputCassetteMappingSelective(
            bool updateGood,
            bool updateNg,
            int goodLevelCount,
            int slotCount,
            IReadOnlyList<bool> good1Map,
            IReadOnlyList<bool> good2Map,
            IReadOnlyList<bool> ngMap,
            IReadOnlyList<double> good1SlotPositions,
            IReadOnlyList<double> good2SlotPositions,
            IReadOnlyList<double> ngSlotPositions,
            string cassetteLotId,
            string tapeFrameSpecName,
            bool recoverDetachedWorkingMaterial)
        {
            if (goodLevelCount < 1) goodLevelCount = 1;
            if (goodLevelCount > 2) goodLevelCount = 2;

            lock (_stateSync)
            {
                var lotRoles = new List<CassetteMaterialRole>();
                if (updateGood)
                {
                    lotRoles.Add(CassetteMaterialRole.Good1);
                    if (goodLevelCount >= 2)
                        lotRoles.Add(CassetteMaterialRole.Good2);
                }
                if (updateNg)
                    lotRoles.Add(CassetteMaterialRole.Ng1);
                string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, lotRoles.ToArray());
                if (updateGood)
                {
                    ValidateCassetteMappingRequest(
                        CassetteMaterialRole.Good1,
                        true,
                        slotCount,
                        good1Map,
                        good1SlotPositions,
                        recoverDetachedWorkingMaterial);
                    ValidateCassetteMappingRequest(
                        CassetteMaterialRole.Good2,
                        goodLevelCount >= 2,
                        slotCount,
                        good2Map,
                        good2SlotPositions,
                        recoverDetachedWorkingMaterial);
                }

                if (updateNg)
                {
                    ValidateCassetteMappingRequest(
                        CassetteMaterialRole.Ng1,
                        true,
                        slotCount,
                        ngMap,
                        ngSlotPositions,
                        recoverDetachedWorkingMaterial);
                }

                if (recoverDetachedWorkingMaterial)
                {
                    if (updateGood)
                    {
                        RemoveDetachedWorkingOutputMaterialForMapping(CassetteMaterialRole.Good1);
                        if (goodLevelCount >= 2)
                            RemoveDetachedWorkingOutputMaterialForMapping(CassetteMaterialRole.Good2);
                    }

                    if (updateNg)
                        RemoveDetachedWorkingOutputMaterialForMapping(CassetteMaterialRole.Ng1);
                }

                if (updateGood)
                {
                    UpdateCassetteMapping(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions, resolvedLotId, tapeFrameSpecName);
                    UpdateCassetteMapping(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions, resolvedLotId, tapeFrameSpecName);
                }

                if (updateNg)
                    UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions, resolvedLotId, tapeFrameSpecName);

            }
            NotifyAndSave("OutputCassetteMappingSelective");
        }

        // To do: [NG 스킵] 카세트 사용 여부를 설정 파라미터와 동기화한다.
        //        IsEnabled=false면 OutputSlotPlanner의 공급/일관성/스토어 판단에서 해당 카세트가 자동 제외된다.
        public static void SetCassetteEnabled(CassetteMaterialRole role, bool enabled)
        {
            bool changed = false;
            lock (_stateSync)
            {
                var cassette = State != null && State.Cassettes != null
                    ? State.Cassettes.FirstOrDefault(c => c != null && c.Role == role)
                    : null;
                if (cassette != null && cassette.IsEnabled != enabled)
                {
                    cassette.IsEnabled = enabled;
                    changed = true;
                }
            }

            if (changed)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Cassette enabled state changed. role=" + role + ", enabled=" + enabled + " - Ok");
                NotifyAndSave("SetCassetteEnabled:" + role);
            }
        }

        public static bool CreateProcessTestDataSet(out string message)
        {
            return CreateProcessTestDataSet(null, out message);
        }

        public static bool CreateProcessTestOutputStageWafer(QMC.CDT320.BinSide side, out string message)
        {
            message = string.Empty;
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

                    var existing = State.Wafers
                        .Where(w => w != null &&
                                    w.CurrentLocation != null &&
                                    w.CurrentLocation.Kind == location &&
                                    WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                        .ToList();
                    foreach (WaferMaterial wafer in existing)
                    {
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

                    CassetteMaterialRole cassetteRole = side == QMC.CDT320.BinSide.Ng
                        ? CassetteMaterialRole.Ng1
                        : CassetteMaterialRole.Good1;
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

                    NotifyAndSave("CreateProcessTestOutputStageWafer");
                    TryFlushPendingSave("CreateProcessTestOutputStageWafer");

                    message = "Output Stage 공정 테스트 Wafer Data 생성 완료. side=" + side +
                              ", wafer=" + (stageWafer != null ? stageWafer.WaferId : "") +
                              ", target=" + (stageWafer != null ? stageWafer.OutputReceiveTotalCount : 0) +
                              ", lot=" + lotId;
                    return true;
                }
            }
            catch (Exception ex)
            {
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

                    ClearActiveProcessLocationsNoLock();

                    UpdateCassetteMapping(CassetteMaterialRole.Input1, true, inputSlotCount, BuildProcessTestSlotMap(inputSlotCount, 2), input1Positions, lotId, inputTapeFrameSpecName, false);
                    UpdateCassetteMapping(CassetteMaterialRole.Input2, useInput2, inputSlotCount, useInput2 ? BuildProcessTestSlotMap(inputSlotCount, 1) : null, input2Positions, lotId, inputTapeFrameSpecName, false);
                    UpdateCassetteMapping(CassetteMaterialRole.Good1, true, outputSlotCount, BuildProcessTestSlotMap(outputSlotCount, 2), good1Positions, lotId, outputTapeFrameSpecName, false);
                    UpdateCassetteMapping(CassetteMaterialRole.Good2, useGood2, outputSlotCount, useGood2 ? BuildProcessTestSlotMap(outputSlotCount, 1) : null, good2Positions, lotId, outputTapeFrameSpecName, false);
                    UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, outputSlotCount, BuildProcessTestSlotMap(outputSlotCount, 2), ngPositions, lotId, outputTapeFrameSpecName, false);

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

                    NotifyAndSave("CreateProcessTestDataSet");
                    TryFlushPendingSave("CreateProcessTestDataSet");

                    message = "공정 테스트 Data 생성 완료. InputStage die=" + inputTargetCount +
                              ", GoodStage target=" + (goodStageWafer != null ? goodStageWafer.OutputReceiveTotalCount : 0) +
                              ", NgStage target=" + (ngStageWafer != null ? ngStageWafer.OutputReceiveTotalCount : 0) +
                              ", lot=" + lotId +
                              ", 카세트 포지션=" + (input1Positions != null && good1Positions != null && ngPositions != null
                                  ? "저장됨"
                                  : "미저장(카세트 티칭/유닛 확인 필요)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = "공정 테스트 Data 생성 실패: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", message + " - Failed");
                return false;
            }
            finally
            {
            }
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

        public static string ResolveRecipeTapeFrameSpecName(int inchSelect)
        {
            var project = RecipeStore.LoadLastOrDefault();
            if (project == null)
                return ResolveDefaultTapeFrameSpecName(inchSelect);

            var frame = project.InputFrame ?? project.Frame;
            if (frame == null)
                return ResolveDefaultTapeFrameSpecName(inchSelect);

            string specName = string.IsNullOrWhiteSpace(frame.FrameSpecName)
                ? ResolveDefaultTapeFrameSpecName(inchSelect)
                : frame.FrameSpecName.Trim();

            EnsureTapeFrameSpecFromFrame(project, frame, specName, "");
            return specName;
        }

        public static string ResolveInputTapeFrameSpecName(int inchSelect)
        {
            string specName = ResolveRecipeTapeFrameSpecName(inchSelect);
            if (!string.IsNullOrWhiteSpace(specName))
                return NormalizeInputTapeFrameSpecName(specName);

            return NormalizeInputTapeFrameSpecName(ResolveDefaultTapeFrameSpecName(inchSelect));
        }

        public static string NormalizeInputTapeFrameSpecName(string specName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(specName))
                    return "";

                string trimmed = specName.Trim();
                if (trimmed.IndexOf("Output", StringComparison.OrdinalIgnoreCase) < 0)
                    return trimmed;

                string candidateName = ReplaceIgnoreCase(trimmed, "Output", "Input");
                TapeFrameSpec candidate = MaterialSpecs.FindFrame(candidateName);
                if (candidate == null)
                    return trimmed;

                TapeFrameSpec current = MaterialSpecs.FindFrame(trimmed);
                if (current != null && !IsCompatibleTapeFrameSpec(current, candidate))
                    return trimmed;

                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input tape frame spec normalized. requested=" + trimmed +
                    ", normalized=" + candidate.Name + " - Ok");
                return candidate.Name;
            }
            catch
            {
                return string.IsNullOrWhiteSpace(specName) ? "" : specName.Trim();
            }
            finally
            {
            }
        }

        private static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldValue))
                return source;

            int index = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return source;

            return source.Substring(0, index) + newValue + source.Substring(index + oldValue.Length);
        }

        private static bool IsCompatibleTapeFrameSpec(TapeFrameSpec a, TapeFrameSpec b)
        {
            if (a == null || b == null)
                return false;

            return a.DieMapX == b.DieMapX &&
                   a.DieMapY == b.DieMapY &&
                   Math.Abs(a.PitchX - b.PitchX) <= 0.000001 &&
                   Math.Abs(a.PitchY - b.PitchY) <= 0.000001 &&
                   Math.Abs(a.OuterDiameterMm - b.OuterDiameterMm) <= 0.001;
        }

        private static double ResolveProcessTestThetaAlignOffset(QMC.CDT320.InputStageUnit inputStage)
        {
            try
            {
                double offset = ProcessTestThetaAlignOffsetDeg;
                if (inputStage != null)
                {
                    double limit = inputStage.ResolveWaferAlignThetaCorrectionLimit();
                    if (limit > InputStageThetaOffsetReadyEpsilon && offset > limit)
                        offset = (limit + InputStageThetaOffsetReadyEpsilon) * 0.5;
                }

                return Math.Abs(offset) > InputStageThetaOffsetReadyEpsilon
                    ? offset
                    : InputStageThetaOffsetReadyEpsilon * 10.0;
            }
            catch
            {
                return ProcessTestThetaAlignOffsetDeg;
            }
            finally
            {
            }
        }

        public static int ResolveWaferSizeInch(int inchSelect)
        {
            switch (inchSelect)
            {
                // 0 또는 8은 8인치로 해석
                case 0:
                case 8:
                    return 8;
                // 1 또는 12는 12인치로 해석
                case 1:
                case 12:
                    return 12;
                default:
                    return inchSelect > 0 ? inchSelect : 8;
            }
        }

        public static string SyncRecipeTapeFrameSpec(RecipeProject project)
        {
            try
            {
                if (project == null)
                    return "";

                TapeFrameSubset frame = project.InputFrame ?? project.Frame;
                if (frame == null)
                    return "";

                string specName = string.IsNullOrWhiteSpace(frame.FrameSpecName)
                    ? ResolveDefaultTapeFrameSpecName(0)
                    : frame.FrameSpecName.Trim();

                // 현재 기준: Input/Output wafer spec을 각각 MaterialSpecs에 동기화한다.
                EnsureTapeFrameSpecFromFrame(project, frame, specName, project.InputDieMapFileName);
                if (project.OutputFrame != null && !string.IsNullOrWhiteSpace(project.OutputFrame.FrameSpecName))
                    EnsureTapeFrameSpecFromFrame(project, project.OutputFrame, project.OutputFrame.FrameSpecName.Trim(), project.GoodBinDieMapFileName);
                if (project.Frame != null && !ReferenceEquals(project.Frame, frame) && !string.IsNullOrWhiteSpace(project.Frame.FrameSpecName))
                    EnsureTapeFrameSpecFromFrame(project, project.Frame, project.Frame.FrameSpecName.Trim(), "");
                return specName;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSpecSync", "Recipe tape frame spec sync failed: " + ex.Message + " - Failed");
                return "";
            }
            finally
            {
            }
        }

        public static string ResolveRecipeDieSpecName()
        {
            var project = RecipeStore.LoadLastOrDefault();
            if (project == null || project.Die == null)
                return "Default";

            string specName = string.IsNullOrWhiteSpace(project.Die.DieSpecName)
                ? "Default"
                : project.Die.DieSpecName.Trim();

            EnsureDieSpecFromRecipe(project, specName);
            return specName;
        }

        public static string SyncRecipeDieSpec(RecipeProject project)
        {
            try
            {
                if (project == null || project.Die == null)
                    return "";

                string specName = string.IsNullOrWhiteSpace(project.Die.DieSpecName)
                    ? "Default"
                    : project.Die.DieSpecName.Trim();

                EnsureDieSpecFromRecipe(project, specName);
                return specName;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSpecSync", "Recipe die spec sync failed: " + ex.Message + " - Failed");
                return "";
            }
            finally
            {
            }
        }

        public static void PutWaferInCassette(
            string waferId,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition = double.NaN)
        {
            PutWaferInCassette(null, waferId, cassetteRole, slotNumber, cassetteLotId, slotPosition, false, WaferMaterialState.Ready);
        }

        public static void PutWaferInCassette(
            WaferMaterial wafer,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition = double.NaN)
        {
            PutWaferInCassette(
                wafer,
                wafer != null ? wafer.WaferId : "",
                cassetteRole,
                slotNumber,
                cassetteLotId,
                slotPosition,
                false,
                WaferMaterialState.Ready);
        }

        public static void PutWaferInCassette(
            string waferId,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition,
            WaferMaterialState state)
        {
            PutWaferInCassette(null, waferId, cassetteRole, slotNumber, cassetteLotId, slotPosition, true, state);
        }

        public static void PutWaferInCassette(
            WaferMaterial wafer,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition,
            WaferMaterialState state)
        {
            PutWaferInCassette(
                wafer,
                wafer != null ? wafer.WaferId : "",
                cassetteRole,
                slotNumber,
                cassetteLotId,
                slotPosition,
                true,
                state);
        }

        private static void PutWaferInCassette(
            WaferMaterial requestedWafer,
            string waferId,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition,
            bool updateState,
            WaferMaterialState state)
        {
            lock (_stateSync)
            {
                if (string.IsNullOrWhiteSpace(waferId))
                    throw new InvalidOperationException("Cassette에 저장할 Wafer/Bin ID가 없습니다.");

                var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
                if (cassette == null || !cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
                    throw new InvalidOperationException("대상 cassette가 활성화 또는 mapping 상태가 아닙니다. cassette=" + cassetteRole);

                cassette.EnsureSlots();
                if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                    throw new ArgumentOutOfRangeException("slotNumber", "Cassette slot 범위를 벗어났습니다. cassette=" + cassetteRole + ", slot=" + (slotNumber + 1));

                CassetteSlotMaterial targetSlot = cassette.Slots[slotNumber];
                if (targetSlot == null)
                    throw new InvalidOperationException("대상 cassette slot 데이터가 없습니다. cassette=" + cassetteRole + ", slot=" + (slotNumber + 1));

                bool targetHasWaferId = !string.IsNullOrWhiteSpace(targetSlot.WaferId);
                if (targetSlot.HasWafer != targetHasWaferId)
                    throw new InvalidOperationException("대상 cassette slot의 점유/Material ID가 불일치합니다. cassette=" + cassetteRole +
                                                        ", slot=" + (slotNumber + 1) +
                                                        ", hasWafer=" + targetSlot.HasWafer +
                                                        ", waferId=" + targetSlot.WaferId);

                if (targetSlot.HasWafer && !string.Equals(targetSlot.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("대상 cassette slot에 다른 자재가 있어 덮어쓸 수 없습니다. cassette=" + cassetteRole +
                                                        ", slot=" + (slotNumber + 1) +
                                                        ", targetWafer=" + targetSlot.WaferId +
                                                        ", movingWafer=" + waferId);

                WaferMaterial wafer = null;
                if (requestedWafer != null)
                {
                    string requestedInstanceId = EnsureWaferInstanceIdNoLock(requestedWafer);
                    wafer = State.Wafers.FirstOrDefault(w =>
                        w != null &&
                        string.Equals(
                            w.WaferInstanceId ?? "",
                            requestedInstanceId,
                            StringComparison.OrdinalIgnoreCase));
                    if (wafer == null)
                    {
                        throw new InvalidOperationException(
                            "Cassette로 이동할 물리 Material 세대를 State에서 찾을 수 없습니다. wafer=" +
                            waferId + ", instance=" + requestedInstanceId);
                    }
                }
                if (targetSlot.HasWafer)
                {
                    string slotReason;
                    WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(targetSlot, out slotReason);
                    if (slotWafer == null)
                        throw new InvalidOperationException("대상 cassette slot Material pointer가 올바르지 않습니다. " + slotReason);
                    if (!string.Equals(slotWafer.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("대상 cassette slot의 물리 Material과 이동 요청 ID가 다릅니다. target=" +
                                                            slotWafer.WaferId + ", moving=" + waferId);
                    if (wafer != null && !IsSameWaferInstance(wafer, slotWafer))
                    {
                        throw new InvalidOperationException(
                            "대상 cassette slot에 같은 표시 ID의 다른 물리 Material 세대가 있습니다. wafer=" +
                            waferId +
                            ", movingInstance=" + EnsureWaferInstanceIdNoLock(wafer) +
                            ", slotInstance=" + EnsureWaferInstanceIdNoLock(slotWafer));
                    }
                    wafer = slotWafer;
                }
                if (wafer == null)
                {
                    List<WaferMaterial> activeCandidates = State.Wafers
                        .Where(w =>
                            w != null &&
                            string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase) &&
                            WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                        .ToList();
                    if (activeCandidates.Count > 1)
                    {
                        throw new InvalidOperationException(
                            "같은 표시 Wafer/Bin ID의 활성 물리 Material이 둘 이상이므로 문자열 ID만으로 Cassette에 넣을 수 없습니다. wafer=" +
                            waferId + ", candidates=" + activeCandidates.Count);
                    }
                    if (activeCandidates.Count == 1)
                        wafer = activeCandidates[0];
                }
                if (wafer == null)
                {
                    wafer = new WaferMaterial
                    {
                        WaferId = waferId,
                        CreatedAt = DateTime.Now
                    };
                    EnsureWaferInstanceIdNoLock(wafer);
                    State.Wafers.Add(wafer);
                }
                string resolvedLotId = ResolveOrCreateCassetteLotId(cassetteLotId, cassette, wafer);
                WaferMaterialState previousState = WaferMaterialStateText.Normalize(wafer.State);
                if (targetSlot.HasWafer && !IsWaferAtCassetteSlot(wafer, cassetteRole, slotNumber))
                {
                    throw new InvalidOperationException("대상 cassette slot과 이동 자재의 현재 위치가 중복/불일치 상태입니다. cassette=" + cassetteRole +
                                                        ", slot=" + (slotNumber + 1) +
                                                        ", wafer=" + wafer.WaferId +
                                                        ", currentLocation=" + wafer.CurrentLocation);
                }

                MaterialLocation targetLocation = MaterialLocation.Cassette(
                    IsOutputCassetteRole(cassetteRole) ? MaterialLocationKind.OutputCassette : MaterialLocationKind.InputCassette,
                    cassetteRole,
                    slotNumber);
                WaferMaterial otherAtTarget = FindOtherWaferAtLocation(wafer, targetLocation);
                if (otherAtTarget != null)
                {
                    throw new InvalidOperationException("대상 cassette slot에 다른 Material 위치 데이터가 있어 덮어쓸 수 없습니다. cassette=" + cassetteRole +
                                                        ", slot=" + (slotNumber + 1) +
                                                        ", targetWafer=" + otherAtTarget.WaferId +
                                                        ", movingWafer=" + wafer.WaferId);
                }

                if (wafer.SourceSlotNumber >= 0 &&
                    previousState != WaferMaterialState.Empty &&
                    (wafer.SourceCassetteRole != cassetteRole || wafer.SourceSlotNumber != slotNumber))
                {
                    throw new InvalidOperationException("자재를 원본 cassette/slot이 아닌 위치로 반환할 수 없습니다. wafer=" + wafer.WaferId +
                                                        ", source=" + wafer.SourceCassetteRole + "/" + (wafer.SourceSlotNumber + 1) +
                                                        ", target=" + cassetteRole + "/" + (slotNumber + 1));
                }

                MaterialLocation previousLocation = wafer.CurrentLocation;
                RemoveWaferFromCassetteSlot(wafer);

                if (wafer.SourceSlotNumber < 0 || previousState == WaferMaterialState.Empty)
                {
                    wafer.SourceCassetteId = cassette.CassetteId;
                    wafer.SourceCassetteRole = cassetteRole;
                    wafer.SourceSlotNumber = slotNumber;
                    if (double.IsNaN(wafer.SourceCassetteSlotPosition) && !double.IsNaN(slotPosition))
                        wafer.SourceCassetteSlotPosition = slotPosition;
                }

                wafer.CassetteLotId = resolvedLotId;
                wafer.CurrentLocation = MaterialLocation.Cassette(
                    cassetteRole == CassetteMaterialRole.Input1 || cassetteRole == CassetteMaterialRole.Input2
                        ? MaterialLocationKind.InputCassette
                        : MaterialLocationKind.OutputCassette,
                    cassetteRole,
                    slotNumber);
                if (IsOutputCassetteRole(cassetteRole))
                {
                    wafer.OutputCassetteId = cassette.CassetteId;
                    wafer.OutputCassetteRole = cassetteRole;
                    wafer.OutputSlotNumber = slotNumber;
                }
                if (!double.IsNaN(slotPosition))
                    wafer.CurrentCassetteSlotPosition = slotPosition;
                if (updateState)
                    wafer.State = WaferMaterialStateText.Normalize(state);
                wafer.UpdatedAt = DateTime.Now;

                cassette.CassetteLotId = resolvedLotId;
                targetSlot.WaferId = wafer.WaferId;
                targetSlot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                targetSlot.HasWafer = true;
                cassette.LastScanTime = DateTime.Now;

                int synchronizedDieCount = SynchronizeCassetteReturnDieLocationsNoLock(
                    wafer,
                    cassetteRole,
                    slotNumber,
                    previousLocation);

                SequenceTrace.MaterialChange(
                    "PutWaferInCassette",
                    "wafer=" + wafer.WaferId,
                    "from=" + previousLocation,
                    "to=" + wafer.CurrentLocation,
                    "state=" + wafer.State,
                    "slot=" + slotNumber,
                    "cassette=" + cassetteRole,
                    "dieLocations=" + synchronizedDieCount);
            }
            NotifyAndSave("PutWaferInCassette");
        }

        private static int SynchronizeCassetteReturnDieLocationsNoLock(
            WaferMaterial wafer,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            MaterialLocation previousWaferLocation)
        {
            if (wafer == null ||
                previousWaferLocation == null ||
                State.Dies == null)
            {
                return 0;
            }

            bool isOutputCassette = IsOutputCassetteRole(cassetteRole);
            MaterialLocationKind feederLocation = isOutputCassette
                ? MaterialLocationKind.OutputFeeder
                : MaterialLocationKind.InputFeeder;
            if (previousWaferLocation.Kind != feederLocation)
                return 0;

            string waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
            if (string.IsNullOrWhiteSpace(waferInstanceId))
                return 0;

            MaterialLocationKind cassetteLocation = isOutputCassette
                ? MaterialLocationKind.OutputCassette
                : MaterialLocationKind.InputCassette;
            MaterialLocationKind outputStageLocation =
                cassetteRole == CassetteMaterialRole.Ng1
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;
            DateTime updatedAt = DateTime.Now;
            int synchronizedCount = 0;

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null || die.CurrentLocation == null)
                    continue;

                MaterialLocationKind dieLocation = die.CurrentLocation.Kind;
                bool shouldSynchronize;
                if (isOutputCassette)
                {
                    shouldSynchronize =
                        !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) &&
                        string.Equals(
                            die.OutputWaferInstanceId,
                            waferInstanceId,
                            StringComparison.OrdinalIgnoreCase) &&
                        (dieLocation == outputStageLocation ||
                         dieLocation == MaterialLocationKind.OutputFeeder);
                }
                else
                {
                    shouldSynchronize =
                        !string.IsNullOrWhiteSpace(die.InputWaferInstanceId) &&
                        string.Equals(
                            die.InputWaferInstanceId,
                            waferInstanceId,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) &&
                        (dieLocation == MaterialLocationKind.InputStage ||
                         dieLocation == MaterialLocationKind.InputFeeder);
                }

                if (!shouldSynchronize)
                    continue;

                die.CurrentLocation = MaterialLocation.Cassette(
                    cassetteLocation,
                    cassetteRole,
                    slotNumber);
                die.UpdatedAt = updatedAt;
                synchronizedCount++;
            }

            return synchronizedCount;
        }

        public static WaferMaterial GetOrCreateWaferInMappedCassette(
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition = double.NaN)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null || !cassette.IsMapped)
                return null;

            cassette.EnsureSlots();
            if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                return null;

            var slot = cassette.Slots[slotNumber];
            string waferId = string.IsNullOrEmpty(slot.WaferId)
                ? BuildGeneratedWaferId(cassetteRole, slotNumber)
                : slot.WaferId;

            string slotReason;
            var wafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
            if (wafer == null &&
                (!string.IsNullOrWhiteSpace(slot.WaferInstanceId) ||
                 (!string.IsNullOrWhiteSpace(slot.WaferId) &&
                  State.Wafers.Count(w => w != null &&
                      string.Equals(w.WaferId, slot.WaferId, StringComparison.OrdinalIgnoreCase)) > 1)))
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Mapped cassette slot Material resolve failed. cassette=" + cassetteRole +
                    ", slot=" + (slotNumber + 1) +
                    ", reason=" + slotReason + " - Blocked");
                return null;
            }
            if (wafer == null)
            {
                wafer = new WaferMaterial
                {
                    WaferId = waferId,
                    CreatedAt = DateTime.Now
                };
                State.Wafers.Add(wafer);
            }

            ApplyWaferCassetteLocation(wafer, cassette, slotNumber, cassetteLotId, slotPosition);
            if (string.IsNullOrWhiteSpace(wafer.TapeFrameSpecName))
                wafer.TapeFrameSpecName = ResolveCassetteTapeFrameSpecName(cassette);
            slot.WaferId = wafer.WaferId;
            slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
            slot.HasWafer = true;
            NotifyAndSave("CreateWaferInMappedCassette");
            return wafer;
        }

        public static WaferMaterial GetWaferInCassette(CassetteMaterialRole cassetteRole, int slotNumber)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null || !cassette.IsMapped)
                return null;

            cassette.EnsureSlots();
            if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                return null;

            var slot = cassette.Slots[slotNumber];
            if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                return null;

            string reason;
            WaferMaterial wafer = ResolveCassetteSlotWaferNoLock(slot, out reason);
            if (wafer == null)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Cassette slot Material pointer mismatch. cassette=" + cassetteRole +
                    ", slot=" + (slotNumber + 1) +
                    ", reason=" + reason + " - Blocked");
            }
            return wafer;
        }

        public static WaferMaterial GetWaferAtLocation(MaterialLocationKind kind)
        {
            return State.Wafers.FirstOrDefault(w =>
                w.CurrentLocation != null &&
                w.CurrentLocation.Kind == kind &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
        }

        /// <summary>
        /// START/재개 전에 InputStage 또는 InputFeeder에 남아 있는 활성 wafer의 원본 Input cassette/slot
        /// Material이 안전하게 식별 가능한지 확인한다. 원본 slot은 wafer가 장비 안으로 이동한 동안 비어 있는
        /// 것이 정상이므로 Empty는 허용하고, 미매핑/범위 오류/다른 wafer 점유만 실패로 처리한다.
        /// </summary>
        public static bool TryValidateActiveInputWaferSourceState(out string reason)
        {
            lock (_stateSync)
            {
                reason = "";
                if (State == null || State.Wafers == null || State.Cassettes == null)
                {
                    reason = "Material snapshot이 초기화되지 않았습니다.";
                    return false;
                }

                List<WaferMaterial> activeWafers = State.Wafers
                    .Where(w =>
                        w != null &&
                        w.CurrentLocation != null &&
                        (w.CurrentLocation.Kind == MaterialLocationKind.InputStage ||
                         w.CurrentLocation.Kind == MaterialLocationKind.InputFeeder) &&
                        WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                    .ToList();

                foreach (WaferMaterial wafer in activeWafers)
                {
                    if (!TryValidateActiveInputWaferSourceStateNoLock(wafer, out reason))
                        return false;
                }

                return true;
            }
        }

        public static bool TryValidateActiveInputWaferSourceState(WaferMaterial wafer, out string reason)
        {
            lock (_stateSync)
            {
                return TryValidateActiveInputWaferSourceStateNoLock(wafer, out reason);
            }
        }

        private static bool TryValidateActiveInputWaferSourceStateNoLock(
            WaferMaterial wafer,
            out string reason)
        {
            reason = "";
            if (State == null || State.Wafers == null || State.Cassettes == null)
            {
                reason = "Material snapshot이 초기화되지 않았습니다.";
                return false;
            }

            if (wafer == null)
            {
                reason = "활성 Input wafer Material이 null입니다.";
                return false;
            }

            string waferId = string.IsNullOrWhiteSpace(wafer.WaferId) ? "(없음)" : wafer.WaferId.Trim();
            MaterialLocationKind location = wafer.CurrentLocation != null
                ? wafer.CurrentLocation.Kind
                : MaterialLocationKind.Unknown;
            if (location != MaterialLocationKind.InputStage &&
                location != MaterialLocationKind.InputFeeder)
            {
                reason = "활성 Input wafer 위치가 Stage/Feeder가 아닙니다. wafer=" + waferId +
                         ", location=" + location;
                return false;
            }

            if (WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
            {
                reason = "활성 Input wafer 상태가 Empty입니다. wafer=" + waferId +
                         ", location=" + location;
                return false;
            }

            if (string.IsNullOrWhiteSpace(wafer.WaferId))
            {
                reason = "활성 Input wafer ID가 없습니다. location=" + location;
                return false;
            }

            if (wafer.SourceCassetteRole != CassetteMaterialRole.Input1 &&
                wafer.SourceCassetteRole != CassetteMaterialRole.Input2)
            {
                reason = "활성 Input wafer의 원본 Cassette 역할이 유효하지 않습니다. wafer=" + waferId +
                         ", location=" + location +
                         ", sourceRole=" + wafer.SourceCassetteRole;
                return false;
            }

            if (wafer.SourceSlotNumber < 0)
            {
                reason = "활성 Input wafer의 원본 Slot 정보가 없습니다. wafer=" + waferId +
                         ", location=" + location +
                         ", sourceRole=" + wafer.SourceCassetteRole +
                         ", sourceSlot=" + wafer.SourceSlotNumber;
                return false;
            }

            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c =>
                c != null && c.Role == wafer.SourceCassetteRole);
            if (cassette == null)
            {
                reason = "활성 Input wafer의 원본 Cassette Material이 없습니다. wafer=" + waferId +
                         ", location=" + location +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00");
                return false;
            }

            if (!cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
            {
                reason = "활성 Input wafer의 원본 Cassette가 운전 가능한 상태가 아닙니다. wafer=" + waferId +
                         ", location=" + location +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00") +
                         ", enabled=" + cassette.IsEnabled +
                         ", present=" + cassette.IsPresent +
                         ", mapped=" + cassette.IsMapped;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(wafer.SourceCassetteId) &&
                !string.IsNullOrWhiteSpace(cassette.CassetteId) &&
                !string.Equals(
                    wafer.SourceCassetteId.Trim(),
                    cassette.CassetteId.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                reason = "활성 Input wafer의 원본 Cassette ID가 현재 Cassette와 다릅니다. wafer=" + waferId +
                         ", location=" + location +
                         ", sourceCassetteId=" + wafer.SourceCassetteId +
                         ", currentCassetteId=" + cassette.CassetteId +
                         ", sourceRole=" + wafer.SourceCassetteRole;
                return false;
            }

            if (cassette.SlotCount <= 0 ||
                wafer.SourceSlotNumber >= cassette.SlotCount ||
                cassette.Slots == null ||
                wafer.SourceSlotNumber >= cassette.Slots.Count)
            {
                reason = "활성 Input wafer의 원본 Slot이 Cassette 범위를 벗어났습니다. wafer=" + waferId +
                         ", location=" + location +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00") +
                         ", slotCount=" + cassette.SlotCount +
                         ", materialSlotCount=" + (cassette.Slots != null ? cassette.Slots.Count : 0);
                return false;
            }

            CassetteSlotMaterial sourceSlot = cassette.Slots[wafer.SourceSlotNumber];
            if (sourceSlot == null)
            {
                reason = "활성 Input wafer의 원본 Slot Material이 null입니다. wafer=" + waferId +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00");
                return false;
            }

            if (sourceSlot.HasWafer || !string.IsNullOrWhiteSpace(sourceSlot.WaferId))
            {
                reason = "활성 Input wafer의 원본 Slot에 다른 Cassette wafer 정보가 남아 있습니다. wafer=" +
                         waferId +
                         ", location=" + location +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00") +
                         ", slotHasWafer=" + sourceSlot.HasWafer +
                         ", slotWaferId=" + (sourceSlot.WaferId ?? "");
                return false;
            }

            WaferMaterial conflictingWafer = State.Wafers.FirstOrDefault(w =>
                w != null &&
                !ReferenceEquals(w, wafer) &&
                w.CurrentLocation != null &&
                w.CurrentLocation.Kind == MaterialLocationKind.InputCassette &&
                w.CurrentLocation.CassetteRole == wafer.SourceCassetteRole &&
                w.CurrentLocation.SlotNumber == wafer.SourceSlotNumber &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
            if (conflictingWafer != null)
            {
                reason = "활성 Input wafer의 원본 Slot을 다른 wafer 위치 정보가 점유하고 있습니다. wafer=" +
                         waferId +
                         ", conflictingWafer=" + (conflictingWafer.WaferId ?? "") +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00");
                return false;
            }

            return true;
        }

        public static WaferMaterial CreateWaferAtLocation(MaterialLocationKind kind, string waferId, WaferMaterialState state)
        {
            var wafer = GetOrCreateWafer(waferId);
            RemoveWaferFromCassetteSlot(wafer);
            wafer.CurrentLocation = new MaterialLocation { Kind = kind };
            wafer.State = WaferMaterialStateText.Normalize(state);
            if (string.IsNullOrWhiteSpace(wafer.TapeFrameSpecName))
                wafer.TapeFrameSpecName = kind == MaterialLocationKind.InputStage
                    ? ResolveInputTapeFrameSpecName(0)
                    : ResolveRecipeTapeFrameSpecName(0);
            wafer.UpdatedAt = DateTime.Now;
            NotifyAndSave("CreateWaferAtLocation");
            return wafer;
        }

        public static bool ClearWaferAtLocation(MaterialLocationKind kind)
        {
            if (kind == MaterialLocationKind.InputStage ||
                kind == MaterialLocationKind.OutputStageGood ||
                kind == MaterialLocationKind.OutputStageNg)
            {
                return ClearStageMaterialData(kind);
            }

            var wafers = State.Wafers
                .Where(w => w.CurrentLocation != null &&
                            w.CurrentLocation.Kind == kind &&
                            WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                .ToList();
            if (wafers.Count == 0)
                return false;

            foreach (var wafer in wafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.UpdatedAt = DateTime.Now;
            }

            NotifyAndSave("ClearWaferAtLocation");
            return true;
        }

        /// <summary>
        /// Stage DATA CLEAR는 화면의 Wafer 한 건만 비우지 않고 해당 Stage에 연결된
        /// Die 위치, 공정 Map/수령 계획, 예약 상태까지 함께 초기화한다.
        /// 다른 Stage 또는 Picker에 실제로 이동한 Die는 수동 Clear 범위에서 제외한다.
        /// </summary>
        private static bool ClearStageMaterialData(MaterialLocationKind kind)
        {
            int clearedWaferCount = 0;
            int removedDieCount = 0;

            lock (_stateSync)
            {
                var directStageWafers = State.Wafers
                    .Where(w =>
                        w != null &&
                        w.CurrentLocation != null &&
                        w.CurrentLocation.Kind == kind)
                    .ToList();

                var relatedWaferIds = new HashSet<string>(
                    directStageWafers
                        .Select(w => w.WaferId)
                        .Where(id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.OrdinalIgnoreCase);
                var relatedWaferInstanceIds = new HashSet<string>(
                    directStageWafers
                        .Select(EnsureWaferInstanceIdNoLock)
                        .Where(id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.OrdinalIgnoreCase);

                // 이전 코드로 Wafer만 Unknown 처리된 경우에도 Stage에 고립된 Die의
                // Wafer ID를 역추적하여 같은 DATA CLEAR 요청으로 복구할 수 있게 한다.
                foreach (DieMaterial die in State.Dies)
                {
                    if (die == null ||
                        die.CurrentLocation == null ||
                        die.CurrentLocation.Kind != kind)
                    {
                        continue;
                    }

                    string relatedWaferId = kind == MaterialLocationKind.InputStage
                        ? die.WaferID_Input
                        : die.WaferID_Output;
                    if (!string.IsNullOrWhiteSpace(relatedWaferId))
                        relatedWaferIds.Add(relatedWaferId);

                    string relatedWaferInstanceId = kind == MaterialLocationKind.InputStage
                        ? die.InputWaferInstanceId
                        : die.OutputWaferInstanceId;
                    if (!string.IsNullOrWhiteSpace(relatedWaferInstanceId))
                        relatedWaferInstanceIds.Add(relatedWaferInstanceId);
                }

                var affectedWafers = State.Wafers
                    .Where(w =>
                        w != null &&
                        ((w.CurrentLocation != null && w.CurrentLocation.Kind == kind) ||
                         relatedWaferInstanceIds.Contains(EnsureWaferInstanceIdNoLock(w)) ||
                         (!string.IsNullOrWhiteSpace(w.WaferId) &&
                          relatedWaferIds.Contains(w.WaferId) &&
                          State.Wafers.Count(candidate =>
                              candidate != null &&
                              string.Equals(
                                  candidate.WaferId,
                                  w.WaferId,
                                  StringComparison.OrdinalIgnoreCase)) == 1)))
                    .ToList();

                var removedDieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DieMaterial die in State.Dies)
                {
                    if (die == null)
                        continue;

                    MaterialLocationKind dieLocation = die.CurrentLocation != null
                        ? die.CurrentLocation.Kind
                        : MaterialLocationKind.Unknown;
                    bool remove = dieLocation == kind;

                    // Input Mapping의 비대상 Die는 위치가 Unknown으로 저장된다.
                    // Stage Wafer와 같은 입력 Wafer에 속한 Unknown Die도 Map 잔재이므로 함께 지운다.
                    if (!remove &&
                        kind == MaterialLocationKind.InputStage &&
                        dieLocation == MaterialLocationKind.Unknown &&
                        ((!string.IsNullOrWhiteSpace(die.InputWaferInstanceId) &&
                          relatedWaferInstanceIds.Contains(die.InputWaferInstanceId)) ||
                         (string.IsNullOrWhiteSpace(die.InputWaferInstanceId) &&
                          !string.IsNullOrWhiteSpace(die.WaferID_Input) &&
                          relatedWaferIds.Contains(die.WaferID_Input))))
                    {
                        remove = true;
                    }

                    if (remove && !string.IsNullOrWhiteSpace(die.DieId))
                        removedDieIds.Add(die.DieId);
                }

                if (removedDieIds.Count > 0)
                {
                    removedDieCount = State.Dies.RemoveAll(d =>
                        d != null &&
                        !string.IsNullOrWhiteSpace(d.DieId) &&
                        removedDieIds.Contains(d.DieId));

                    // Wafer의 DieIds 포인터도 함께 정리해야 재시작 후 삭제된 Die가
                    // Mapping/Output 수령 데이터로 다시 살아나지 않는다.
                    foreach (WaferMaterial wafer in State.Wafers)
                    {
                        if (wafer != null && wafer.DieIds != null)
                            wafer.DieIds.RemoveAll(id => !string.IsNullOrWhiteSpace(id) && removedDieIds.Contains(id));
                    }
                }

                foreach (WaferMaterial wafer in affectedWafers)
                {
                    if (kind == MaterialLocationKind.InputStage)
                    {
                        ClearInputStageWaferProcessingFieldsNoLock(wafer);
                        wafer.InputStageProcessingGeneration = wafer.InputStageProcessingGeneration + 1;
                        wafer.WaferInstanceId = CreateWaferInstanceId();
                    }
                    else
                    {
                        ClearOutputStageWaferProcessingFieldsNoLock(wafer);
                    }

                    wafer.State = WaferMaterialState.Empty;
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                    wafer.UpdatedAt = DateTime.Now;
                    clearedWaferCount++;
                }

                if (kind == MaterialLocationKind.InputStage)
                {
                    InputStageHybridResultSession.Clear();
                }
                else
                {
                    QMC.CDT320.BinSide side = kind == MaterialLocationKind.OutputStageNg
                        ? QMC.CDT320.BinSide.Ng
                        : QMC.CDT320.BinSide.Good;
                    _outputReceiveOrderCache.Remove(side);
                }
            }

            if (clearedWaferCount == 0 && removedDieCount == 0)
                return false;

            string saveReason = "ClearStageMaterialData:" + kind;
            NotifyAndSave(saveReason);
            Log.Write(
                "Main",
                "SYSTEM",
                "MaterialStateService",
                "Stage Material 데이터를 전체 초기화했습니다. location=" + kind +
                ", wafers=" + clearedWaferCount +
                ", removedDies=" + removedDieCount + " - Ok");
            return true;
        }

        private static void ClearOutputStageWaferProcessingFieldsNoLock(WaferMaterial wafer)
        {
            if (wafer == null)
                return;

            wafer.OutputReceiveSourceWaferId = string.Empty;
            wafer.OutputReceiveSourceWaferInstanceId = string.Empty;
            wafer.OutputReceiveDieMapX = 0;
            wafer.OutputReceiveDieMapY = 0;
            wafer.OutputReceivePitchX = 0.0;
            wafer.OutputReceivePitchY = 0.0;
            wafer.OutputReceiveDieSizeX = 0.0;
            wafer.OutputReceiveDieSizeY = 0.0;
            wafer.OutputReceiveOuterDiameterMm = 0.0;
            wafer.OutputReceiveOriginX = 0.0;
            wafer.OutputReceiveOriginY = 0.0;
            wafer.OutputReceiveNextIndex = 0;
            wafer.OutputReceiveTotalCount = 0;
            wafer.OutputReceiveStartCorner = string.Empty;
            wafer.OutputReceiveDirection = string.Empty;
            wafer.OutputReceivePattern = string.Empty;
            wafer.OutputReceiveSlots = new List<OutputReceiveSlotMaterial>();
            wafer.DieMapFrameObjId = string.Empty;
            wafer.OutputGrade = DieResult.Unknown;
        }

        public static bool ClearInputCassetteSlotData(CassetteMaterialRole cassetteRole, int slotNumber)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return false;

            cassette.EnsureSlots();
            if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                return false;

            var slot = cassette.Slots[slotNumber];
            WaferMaterial slotWafer = null;
            if (slot != null && slot.HasWafer)
            {
                string slotReason;
                slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                if (slotWafer == null)
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "Input cassette slot clear blocked: " + slotReason +
                        ", cassette=" + cassetteRole +
                        ", slot=" + (slotNumber + 1) + " - Blocked");
                    return false;
                }
            }
            string slotInstanceId = slotWafer != null
                ? EnsureWaferInstanceIdNoLock(slotWafer)
                : "";
            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((!string.IsNullOrWhiteSpace(slotInstanceId) &&
                  string.Equals(
                      EnsureWaferInstanceIdNoLock(w),
                      slotInstanceId,
                      StringComparison.OrdinalIgnoreCase)) ||
                 IsWaferAtCassetteSlot(w, cassetteRole, slotNumber)))
                .ToList();

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                // To do: 슬롯 자재를 비울 때 CassetteLotId도 함께 지워야 한다.
                // 이 값을 남기면 다음 mapping의 ResolveOrCreateCassetteLotId가
                // State.LotId와 다른 잔존 LotId를 후보로 잡아 "LOT ID 후보가 서로 달라..." 예외로 등록 실패한다.
                wafer.CassetteLotId = "";
                wafer.UpdatedAt = DateTime.Now;
            }

            slot.WaferId = "";
            slot.WaferInstanceId = "";
            slot.HasWafer = false;
            NotifyAndSave("ClearInputCassetteSlotData");
            return true;
        }

        public static bool ClearInputCassetteAllSlotData()
        {
            bool processed = false;
            ClearInputCassetteAllSlotData(CassetteMaterialRole.Input1, ref processed);
            ClearInputCassetteAllSlotData(CassetteMaterialRole.Input2, ref processed);

            if (!processed)
                return false;

            NotifyAndSave("ClearInputCassetteAllSlotData");
            return true;
        }

        private static void ClearInputCassetteAllSlotData(CassetteMaterialRole cassetteRole, ref bool processed)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return;

            cassette.EnsureSlots();
            processed = true;

            var slotWaferInstanceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CassetteSlotMaterial slot in cassette.Slots)
            {
                if (slot == null || !slot.HasWafer)
                    continue;
                string slotReason;
                WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                if (slotWafer != null)
                    slotWaferInstanceIds.Add(EnsureWaferInstanceIdNoLock(slotWafer));
            }

            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((slotWaferInstanceIds.Count > 0 &&
                  slotWaferInstanceIds.Contains(EnsureWaferInstanceIdNoLock(w))) ||
                 (w.CurrentLocation != null &&
                   w.CurrentLocation.Kind == MaterialLocationKind.InputCassette &&
                  w.CurrentLocation.CassetteRole == cassetteRole)))
                .ToList();

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                // To do: 전체 삭제 시 웨이퍼 CassetteLotId도 비워야 잔존 LotId가 다음 mapping을 막지 않는다.
                wafer.CassetteLotId = "";
                wafer.UpdatedAt = DateTime.Now;
            }

            foreach (var slot in cassette.Slots)
            {
                if (slot == null)
                    continue;

                slot.WaferId = "";
                slot.WaferInstanceId = "";
                slot.HasWafer = false;
            }

            // Material slot data를 모두 지운 뒤에는 마지막 mapping 결과를 더 이상
            // 유효한 것으로 사용할 수 없다. 다음 Auto 시작에서 실제 mapping을 다시
            // 수행하여 센서 결과와 Ready Material을 함께 재생성하도록 한다.
            cassette.IsMapped = false;
            // IsPresent는 현재 물리 센서가 아니라 Mapping으로 만든 논리 상태다.
            // 모든 Slot Data를 삭제한 뒤 true를 남기면 Present/Unmapped 잔재가
            // Recipe 변경을 영구 차단하므로 다음 Mapping 전까지 false로 초기화한다.
            cassette.IsPresent = false;

            // To do: 카세트 레코드의 CassetteLotId도 초기화해야 한다.
            // 이 값(예: Y482CB12)이 State.LotId(예: Y482CB1)와 달라지면
            // 슬롯을 모두 비운 뒤에도 ResolveOrCreateCassetteLotId가 후보 2개로 인식해
            // "카세트 LOT ID 후보가 서로 달라 LOT ID를 결정할 수 없습니다." 예외로 재mapping이 막힌다.
            cassette.CassetteLotId = "";
        }

        public static bool ClearOutputCassetteSlotData(CassetteMaterialRole cassetteRole, int slotNumber)
        {
            if (cassetteRole != CassetteMaterialRole.Good1 &&
                cassetteRole != CassetteMaterialRole.Good2 &&
                cassetteRole != CassetteMaterialRole.Ng1)
                return false;

            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return false;

            cassette.EnsureSlots();
            if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                return false;

            var slot = cassette.Slots[slotNumber];
            WaferMaterial slotWafer = null;
            if (slot != null && slot.HasWafer)
            {
                string slotReason;
                slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                if (slotWafer == null)
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "Output cassette slot clear blocked: " + slotReason +
                        ", cassette=" + cassetteRole +
                        ", slot=" + (slotNumber + 1) + " - Blocked");
                    return false;
                }
            }
            string slotInstanceId = slotWafer != null
                ? EnsureWaferInstanceIdNoLock(slotWafer)
                : "";
            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((!string.IsNullOrWhiteSpace(slotInstanceId) &&
                  string.Equals(
                      EnsureWaferInstanceIdNoLock(w),
                      slotInstanceId,
                      StringComparison.OrdinalIgnoreCase)) ||
                 IsWaferAtCassetteSlot(w, cassetteRole, slotNumber)))
                .ToList();

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.UpdatedAt = DateTime.Now;
            }

            slot.WaferId = "";
            slot.WaferInstanceId = "";
            slot.HasWafer = false;
            NotifyAndSave("ClearOutputCassetteSlotData");
            return true;
        }

        public static bool ClearOutputCassetteAllSlotData()
        {
            bool processed = false;
            ClearOutputCassetteAllSlotData(CassetteMaterialRole.Good1, ref processed);
            ClearOutputCassetteAllSlotData(CassetteMaterialRole.Good2, ref processed);
            ClearOutputCassetteAllSlotData(CassetteMaterialRole.Ng1, ref processed);

            if (!processed)
                return false;

            NotifyAndSave("ClearOutputCassetteAllSlotData");
            return true;
        }

        /// <summary>
        /// 출력 카세트를 GOOD / NG 한쪽만 골라서 Material 데이터를 초기화한다.
        /// 카세트 교체는 GOOD만 또는 NG만 진행하는 경우가 많아 반대편 데이터를 보존해야 한다.
        /// GOOD은 장비에서 한 묶음으로 취급하므로 Good1/Good2를 함께 지운다(사용자 확정 2026-07-26).
        /// </summary>
        public static bool ClearOutputCassetteSideData(QMC.CDT320.BinSide side)
        {
            try
            {
                lock (_stateSync)
                {
                    bool processed = false;
                    if (side == QMC.CDT320.BinSide.Ng)
                    {
                        ClearOutputCassetteAllSlotData(CassetteMaterialRole.Ng1, ref processed);
                    }
                    else
                    {
                        ClearOutputCassetteAllSlotData(CassetteMaterialRole.Good1, ref processed);
                        ClearOutputCassetteAllSlotData(CassetteMaterialRole.Good2, ref processed);
                    }

                    if (!processed)
                        return false;

                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "출력 카세트 " + side + " 측 Material 데이터를 초기화했습니다. " +
                        "반대편 데이터는 유지됩니다. - Ok");
                    NotifyAndSave("ClearOutputCassetteSideData:" + side);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "출력 카세트 " + side + " 측 Material 초기화 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void ClearOutputCassetteAllSlotData(CassetteMaterialRole cassetteRole, ref bool processed)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return;

            cassette.EnsureSlots();
            processed = true;

            var slotWaferInstanceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CassetteSlotMaterial slot in cassette.Slots)
            {
                if (slot == null || !slot.HasWafer)
                    continue;
                string slotReason;
                WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                if (slotWafer != null)
                    slotWaferInstanceIds.Add(EnsureWaferInstanceIdNoLock(slotWafer));
            }

            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((slotWaferInstanceIds.Count > 0 &&
                  slotWaferInstanceIds.Contains(EnsureWaferInstanceIdNoLock(w))) ||
                 (w.CurrentLocation != null &&
                   w.CurrentLocation.Kind == MaterialLocationKind.OutputCassette &&
                  w.CurrentLocation.CassetteRole == cassetteRole)))
                .ToList();

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.CassetteLotId = "";
                wafer.UpdatedAt = DateTime.Now;
            }

            foreach (var slot in cassette.Slots)
            {
                if (slot == null)
                    continue;

                slot.WaferId = "";
                slot.WaferInstanceId = "";
                slot.HasWafer = false;
            }

            // Material slot data를 모두 지운 뒤에는 마지막 mapping 결과를 더 이상
            // 유효한 것으로 사용할 수 없다. 다음 전체 준비에서 실제 mapping을 다시
            // 수행하여 센서 결과와 Ready Material을 함께 재생성하도록 한다.
            cassette.IsMapped = false;
            // 출력 카세트도 Mapping 결과가 IsPresent를 다시 설정한다.
            // Side/전체 Clear 직후에는 빈 논리 상태로 내려 잔존 Material 판정을 막는다.
            cassette.IsPresent = false;
            cassette.CassetteLotId = "";
        }

        public static void MoveWaferToInputFeeder(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            MoveWafer(
                wafer,
                new MaterialLocation { Kind = MaterialLocationKind.InputFeeder },
                WaferMaterialState.WorkReady);
        }

        public static void MoveWaferToInputStage(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            MoveWafer(
                wafer,
                new MaterialLocation { Kind = MaterialLocationKind.InputStage },
                WaferMaterialState.Working);
        }

        public static bool UpdateWaferFieldInMappedCassette(
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string fieldKey,
            string value)
        {
            var wafer = GetOrCreateWaferInMappedCassette(cassetteRole, slotNumber, "");
            if (wafer == null || string.IsNullOrEmpty(fieldKey))
                return false;

            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return false;

            string newValue = value ?? "";
            if (fieldKey == "WaferId")
            {
                if (string.IsNullOrWhiteSpace(newValue))
                    return false;

                wafer.WaferId = newValue;
                cassette.Slots[slotNumber].WaferId = newValue;
            }
            else if (fieldKey == "CassetteLotId")
            {
                wafer.CassetteLotId = newValue;
                cassette.CassetteLotId = newValue;
            }
            else if (fieldKey == "TapeFrameSpecName")
            {
                wafer.TapeFrameSpecName = newValue;
            }
            else if (fieldKey == "State")
            {
                WaferMaterialState parsed;
                if (!WaferMaterialStateText.TryParse(newValue, out parsed))
                    return false;
                wafer.State = WaferMaterialStateText.Normalize(parsed);
                cassette.Slots[slotNumber].HasWafer = wafer.State != WaferMaterialState.Empty;
            }
            else
            {
                return false;
            }

            ApplyWaferCassetteLocation(wafer, cassette, slotNumber, wafer.CassetteLotId);
            if (fieldKey == "State")
            {
                WaferMaterialState parsed;
                if (WaferMaterialStateText.TryParse(newValue, out parsed))
                {
                    wafer.State = WaferMaterialStateText.Normalize(parsed);
                    cassette.Slots[slotNumber].HasWafer = wafer.State != WaferMaterialState.Empty;
                }
            }
            NotifyAndSave("UpdateWaferField");
            return true;
        }

        // To do: [Bin 상태 편집] 자재가 스테이지/피더에 나가 있어도 상태만 안전하게 바꾼다.
        //        기존 조건: 상태 변경도 UpdateWaferFieldInMappedCassette를 탔고, 그 안의
        //                   GetOrCreateWaferInMappedCassette + ApplyWaferCassetteLocation이
        //                   CurrentLocation을 카세트 슬롯으로 덮어써서, 스테이지에 나가 있는 자재를
        //                   슬롯으로 끌어오는 부작용이 있었다(슬롯 포인터까지 다시 채움).
        //        현재 기준: 지정 Material의 State만 바꾸고 위치/슬롯 포인터는 옮기지 않는다.
        //                   그 Material을 이미 가리키는 슬롯이 있으면 점유 플래그만 State에 맞춰 정리한다.
        /// <summary>
        /// Bin/Wafer 상태만 변경합니다(위치 이동 없음). 성공 시 저장을 요청하며,
        /// 수동 UI처럼 저장 완료 확인이 필요한 호출자는 TryFlushPendingSave를 이어서 호출합니다.
        /// </summary>
        public static bool UpdateWaferStateOnly(string waferId, string stateText, string userName)
        {
            if (string.IsNullOrWhiteSpace(waferId))
                return false;

            WaferMaterialState parsed;
            if (!WaferMaterialStateText.TryParse(stateText, out parsed))
            {
                Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "UpdateWaferStateOnly",
                        "Material 상태 변경 실패: 상태 문자열을 해석할 수 없습니다. material=" + waferId +
                    ", state=" + (stateText ?? "") + " - Failed");
                return false;
            }

            WaferMaterialState normalized = WaferMaterialStateText.Normalize(parsed);
            WaferMaterialState before;
            string locationText;

            lock (_stateSync)
            {
                List<WaferMaterial> candidates = State.Wafers != null
                    ? State.Wafers.Where(w => w != null &&
                        string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase)).ToList()
                    : new List<WaferMaterial>();
                if (candidates.Count != 1)
                {
                    Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "UpdateWaferStateOnly",
                        "Material 상태 변경 실패: 표시 ID로 물리 Material을 1개로 확정할 수 없습니다. material=" +
                        waferId + ", candidates=" + candidates.Count + " - Failed");
                    return false;
                }

                WaferMaterial wafer = candidates[0];
                string waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                before = WaferMaterialStateText.Normalize(wafer.State);
                wafer.State = normalized;
                wafer.UpdatedAt = DateTime.Now;
                locationText = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

                // 위치는 그대로 두고, 이 Material을 가리키고 있는 슬롯의 점유 플래그만 정리한다.
                if (State.Cassettes != null)
                {
                    foreach (CassetteMaterial cassette in State.Cassettes)
                    {
                        if (cassette == null || cassette.Slots == null)
                            continue;

                        foreach (CassetteSlotMaterial slot in cassette.Slots)
                        {
                            if (slot == null)
                                continue;

                            bool sameInstance = !string.IsNullOrWhiteSpace(slot.WaferInstanceId)
                                ? string.Equals(
                                    slot.WaferInstanceId,
                                    waferInstanceId,
                                    StringComparison.OrdinalIgnoreCase)
                                : string.Equals(
                                    slot.WaferId,
                                    wafer.WaferId,
                                    StringComparison.OrdinalIgnoreCase);
                            if (!sameInstance)
                                continue;

                            slot.WaferInstanceId = waferInstanceId;
                            slot.HasWafer = normalized != WaferMaterialState.Empty;
                        }
                    }
                }
            }

            Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "UpdateWaferStateOnly",
                "Material 상태 변경 완료(위치 이동 없음). material=" + waferId +
                ", before=" + WaferMaterialStateText.ToDisplayName(before) +
                ", after=" + WaferMaterialStateText.ToDisplayName(normalized) +
                ", location=" + locationText + " - Ok");

            NotifyAndSave("UpdateWaferStateOnly");
            return true;
        }

        /// <summary>
        /// Stage에 있는 물리 Wafer/Bin의 임시 ID를 실제 바코드로 원자 승격합니다.
        /// 물리 식별에는 WaferInstanceId를 계속 사용하며, 연관 Die/수신 계획/slot 포인터의 표시 ID도
        /// 같은 lock 안에서 함께 갱신합니다.
        /// </summary>
        public static bool TryApplyWaferBarcode(
            string waferInstanceId,
            MaterialLocationKind expectedLocation,
            string barcode,
            string source,
            int attempts,
            out string previousWaferId,
            out string reason)
        {
            previousWaferId = "";
            reason = "";

            string normalizedInstanceId = (waferInstanceId ?? "").Trim();
            string normalizedBarcode = NormalizeBarcodeValue(barcode);
            if (string.IsNullOrWhiteSpace(normalizedInstanceId))
            {
                reason = "바코드를 적용할 WaferInstanceId가 없습니다.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(normalizedBarcode) ||
                string.Equals(normalizedBarcode, "WAFER-NULL-ID", StringComparison.OrdinalIgnoreCase))
            {
                reason = "유효한 바코드 값이 없습니다.";
                return false;
            }
            if (expectedLocation == MaterialLocationKind.Unknown)
            {
                reason = "바코드 적용 대상 Stage 위치가 지정되지 않았습니다.";
                return false;
            }

            string locationText;
            DateTime updatedAt = DateTime.Now;
            lock (_stateSync)
            {
                List<WaferMaterial> candidates = State != null && State.Wafers != null
                    ? State.Wafers.Where(w =>
                        w != null &&
                        string.Equals(
                            w.WaferInstanceId ?? "",
                            normalizedInstanceId,
                            StringComparison.OrdinalIgnoreCase)).ToList()
                    : new List<WaferMaterial>();
                if (candidates.Count != 1)
                {
                    reason = "WaferInstanceId로 물리 Material을 1개로 확정할 수 없습니다. instance=" +
                             normalizedInstanceId + ", candidates=" + candidates.Count;
                    return false;
                }

                WaferMaterial wafer = candidates[0];
                MaterialLocation location = wafer.CurrentLocation;
                if (location == null || location.Kind != expectedLocation)
                {
                    reason = "바코드 대상 Material 위치가 변경되었습니다. expected=" + expectedLocation +
                             ", actual=" + (location != null ? location.Kind.ToString() : "null") +
                             ", wafer=" + (wafer.WaferId ?? "") +
                             ", instance=" + normalizedInstanceId;
                    return false;
                }
                if (WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
                {
                    reason = "빈 Material에는 바코드를 적용할 수 없습니다. location=" + expectedLocation +
                             ", instance=" + normalizedInstanceId;
                    return false;
                }

                WaferMaterial duplicate = State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    !string.Equals(
                        w.WaferInstanceId ?? "",
                        normalizedInstanceId,
                        StringComparison.OrdinalIgnoreCase) &&
                    WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                    (string.Equals(w.WaferId ?? "", normalizedBarcode, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(w.BarcodeId ?? "", normalizedBarcode, StringComparison.OrdinalIgnoreCase)));
                if (duplicate != null)
                {
                    reason = "같은 바코드를 사용하는 다른 활성 Material이 있습니다. barcode=" +
                             normalizedBarcode + ", otherWafer=" + (duplicate.WaferId ?? "") +
                             ", otherInstance=" + (duplicate.WaferInstanceId ?? "") +
                             ", otherLocation=" +
                             (duplicate.CurrentLocation != null ? duplicate.CurrentLocation.ToString() : "null");
                    return false;
                }

                previousWaferId = wafer.WaferId ?? "";
                if (string.IsNullOrWhiteSpace(wafer.OriginalWaferId))
                    wafer.OriginalWaferId = previousWaferId;
                wafer.WaferId = normalizedBarcode;
                wafer.BarcodeId = normalizedBarcode;
                wafer.BarcodeConfirmed = true;
                wafer.BarcodeSource = string.IsNullOrWhiteSpace(source) ? "BARCODE" : source.Trim();
                wafer.BarcodeUpdatedAt = updatedAt;
                wafer.BarcodeAttemptCount = Math.Max(1, attempts);
                wafer.UpdatedAt = updatedAt;
                locationText = location.ToString();

                if (State.Dies != null)
                {
                    foreach (DieMaterial die in State.Dies)
                    {
                        if (die == null)
                            continue;

                        bool changed = false;
                        if (string.Equals(
                            die.InputWaferInstanceId ?? "",
                            normalizedInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            die.WaferID_Input = normalizedBarcode;
                            changed = true;
                        }
                        if (string.Equals(
                            die.OutputWaferInstanceId ?? "",
                            normalizedInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            die.WaferID_Output = normalizedBarcode;
                            changed = true;
                        }
                        if (changed)
                            die.UpdatedAt = updatedAt;
                    }
                }

                foreach (WaferMaterial linkedWafer in State.Wafers)
                {
                    if (linkedWafer == null ||
                        !string.Equals(
                            linkedWafer.OutputReceiveSourceWaferInstanceId ?? "",
                            normalizedInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    linkedWafer.OutputReceiveSourceWaferId = normalizedBarcode;
                    linkedWafer.UpdatedAt = updatedAt;
                }

                if (State.Cassettes != null)
                {
                    foreach (CassetteMaterial cassette in State.Cassettes)
                    {
                        if (cassette == null || cassette.Slots == null)
                            continue;
                        foreach (CassetteSlotMaterial slot in cassette.Slots)
                        {
                            if (slot != null &&
                                string.Equals(
                                    slot.WaferInstanceId ?? "",
                                    normalizedInstanceId,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                slot.WaferId = normalizedBarcode;
                            }
                        }
                    }
                }

                DieTapeFrame frame = !string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId)
                    ? MaterialStorage.GetFrame(wafer.DieMapFrameObjId)
                    : null;
                if (frame == null && !string.IsNullOrWhiteSpace(previousWaferId))
                    frame = MaterialStorage.GetFrame(previousWaferId);
                if (frame != null)
                    frame.BarcodeId = normalizedBarcode;
            }

            NotifyAndSave("ApplyWaferBarcode");
            Log.Write(
                "Main",
                string.IsNullOrWhiteSpace(source) ? "SYSTEM" : source,
                "ApplyWaferBarcode",
                "Material 바코드 적용 완료. previous=" + previousWaferId +
                ", barcode=" + normalizedBarcode +
                ", instance=" + normalizedInstanceId +
                ", location=" + locationText +
                ", attempts=" + Math.Max(1, attempts) + " - Ok");
            return true;
        }

        private static string NormalizeBarcodeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var chars = new List<char>(value.Length);
            foreach (char ch in value)
            {
                if (ch == '\x02' || ch == '\x03' || char.IsWhiteSpace(ch))
                    continue;
                if (char.IsControl(ch))
                    return "";
                chars.Add(ch);
            }
            return new string(chars.ToArray());
        }

        public static void MoveWafer(string waferId, MaterialLocation location, WaferMaterialState state)
        {
            lock (_stateSync)
            {
                if (string.IsNullOrWhiteSpace(waferId))
                    throw new InvalidOperationException("이동할 Wafer/Bin ID가 없습니다.");

                List<WaferMaterial> candidates = State.Wafers
                    .Where(w =>
                        w != null &&
                        string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase) &&
                        WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                    .ToList();
                if (candidates.Count > 1)
                {
                    throw new InvalidOperationException(
                        "같은 표시 Wafer/Bin ID의 활성 물리 Material이 둘 이상이므로 문자열 ID만으로 이동할 수 없습니다. wafer=" +
                        waferId + ", candidates=" + candidates.Count);
                }

                WaferMaterial wafer = candidates.Count == 1 ? candidates[0] : GetOrCreateWafer(waferId);
                MoveWaferNoLock(wafer, location, state);
            }
            NotifyAndSave("MoveWafer");
        }

        public static void MoveWafer(WaferMaterial wafer, MaterialLocation location, WaferMaterialState state)
        {
            lock (_stateSync)
            {
                if (wafer == null)
                    throw new InvalidOperationException("이동할 Wafer/Bin Material이 없습니다.");

                string instanceId = EnsureWaferInstanceIdNoLock(wafer);
                WaferMaterial stateWafer = State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    string.Equals(
                        w.WaferInstanceId ?? "",
                        instanceId,
                        StringComparison.OrdinalIgnoreCase));
                if (stateWafer == null)
                {
                    throw new InvalidOperationException(
                        "이동할 물리 Material 세대를 State에서 찾을 수 없습니다. wafer=" +
                        wafer.WaferId + ", instance=" + instanceId);
                }

                MoveWaferNoLock(stateWafer, location, state);
            }
            NotifyAndSave("MoveWafer");
        }

        private static void MoveWaferNoLock(
            WaferMaterial wafer,
            MaterialLocation location,
            WaferMaterialState state)
        {
            MaterialLocation targetLocation = location ?? MaterialLocation.Unknown();
            WaferMaterial occupied = FindOtherWaferAtLocation(wafer, targetLocation);
            if (occupied != null)
            {
                throw new InvalidOperationException("대상 Material 위치에 다른 자재가 있어 이동할 수 없습니다. target=" + targetLocation +
                                                    ", targetWafer=" + occupied.WaferId +
                                                    ", movingWafer=" + wafer.WaferId);
            }

            MaterialLocation previousLocation = wafer.CurrentLocation;
            RemoveWaferFromCassetteSlot(wafer);
            wafer.CurrentLocation = targetLocation;
            wafer.State = WaferMaterialStateText.Normalize(state);
            wafer.UpdatedAt = DateTime.Now;
            SequenceTrace.MaterialChange(
                "MoveWafer",
                "wafer=" + wafer.WaferId,
                "instance=" + EnsureWaferInstanceIdNoLock(wafer),
                "from=" + previousLocation,
                "to=" + wafer.CurrentLocation,
                "state=" + wafer.State);
        }

        // ===== DATA ONLY 수동 위치 이동/삭제 =====
        // 장비를 움직이지 않고 Material 위치 데이터만 변경한다. Motion/Cylinder/Vacuum/IO를 호출하지 않는다.
        // Source clear + Destination set + CurrentLocation 갱신을 하나의 lock에서 처리하고 즉시 저장한다.
        // Material ID/검사결과/DieMap은 유지한다.
        // State와 Cassette 위치 메타데이터는 실제 Auto 이송 규칙과 동일하게 목적지 기준으로 정규화한다.
        // 위치만 바꾸고 State를 유지하면 예: OutputCassette + Working 같은 불가능한 조합이 저장되어
        // 다음 Auto 시작 시 Material 일관성 검사에서 차단되므로 DATA ONLY에서도 반드시 함께 맞춘다.

        public static DataOnlyOperationResult MoveMaterialDataOnly(
            DataOnlyLocation source,
            DataOnlyLocation destination,
            string expectedMaterialId,
            string userName)
        {
            const string operation = "Move";
            try
            {
                string pathCode;
                string pathMessage;
                if (!ManualMaterialPositionService.ValidateAllowedPath(source, destination, out pathCode, out pathMessage))
                    return FailDataOnly(operation, source, destination, pathCode, pathMessage, userName);

                var result = new DataOnlyOperationResult
                {
                    Operation = operation,
                    SourceText = source.DisplayText,
                    DestinationText = destination.DisplayText
                };
                WaferMaterialState beforeState = WaferMaterialState.Empty;
                WaferMaterialState afterState = WaferMaterialState.Empty;
                WaferMaterialState displacedBeforeState = WaferMaterialState.Empty;
                WaferMaterialState displacedAfterState = WaferMaterialState.Empty;

                lock (_stateSync)
                {
                    // Apply 직전 재확인 1: Source Material 단일 존재/포인터 정합.
                    WaferMaterial wafer;
                    string failureCode;
                    string failureMessage;
                    if (!TryFindSingleWaferAtDataOnlyLocation(source, out wafer, out failureCode, out failureMessage))
                        return FailDataOnly(operation, source, destination, failureCode, failureMessage, userName);

                    // Preview 이후 Source Material이 바뀌었으면 적용하지 않는다.
                    if (!string.IsNullOrWhiteSpace(expectedMaterialId) &&
                        !string.Equals(wafer.WaferId, expectedMaterialId.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return FailDataOnly(operation, source, destination, "DATA-ONLY-SOURCE-CHANGED",
                            "선택(Preview) 이후 Source Material이 변경되었습니다. 다시 선택하십시오. expected=" +
                            expectedMaterialId + ", current=" + wafer.WaferId, userName);
                    }

                    // 기존 조건: "Apply 직전 재확인 2: Destination Empty."
                    // 현재 기준: Destination은 카세트 데이터 존재/슬롯 범위만 확인한다(점유는 교환으로 처리).
                    CassetteMaterial destinationCassette = null;
                    CassetteSlotMaterial destinationSlot = null;
                    if (destination.IsCassette)
                    {
                        destinationCassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == destination.CassetteRole);
                        if (destinationCassette == null)
                        {
                            return FailDataOnly(operation, source, destination, "DATA-ONLY-DEST-CASSETTE",
                                "Destination 카세트 상태 데이터가 없습니다. role=" + destination.CassetteRole, userName);
                        }

                        // 기존 조건: Input cassette destination은 enabled/present/mapped가 모두 참이어야 이동을 허용했다.
                        //            → 실물과 데이터가 어긋난 상태를 맞추려는데 카세트 상태 때문에 이동이 막혔다.
                        // 현재 기준: DATA ONLY는 유저가 장비 실물에 데이터를 맞추는 도구이므로 카세트 활성 상태로 막지 않고,
                        //            판정 근거만 로그로 남긴다(슬롯 범위/카세트 데이터 존재는 계속 검사한다).
                        // To do: [DATA ONLY 배선] 카세트 활성 상태 게이트 제거 - 상태 정렬 도구 목적 우선.
                        if (!destinationCassette.IsEnabled ||
                            !destinationCassette.IsPresent ||
                            !destinationCassette.IsMapped)
                        {
                            Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                                "[DATA ONLY] Destination 카세트가 비활성 상태이지만 상태 정렬 목적으로 이동을 허용합니다. role=" +
                                destination.CassetteRole +
                                ", enabled=" + destinationCassette.IsEnabled +
                                ", present=" + destinationCassette.IsPresent +
                                ", mapped=" + destinationCassette.IsMapped + " - Check");
                        }

                        destinationCassette.EnsureSlots();
                        if (destination.SlotIndex < 0 || destination.SlotIndex >= destinationCassette.Slots.Count)
                        {
                            return FailDataOnly(operation, source, destination, "DATA-ONLY-DEST-SLOT-RANGE",
                                "Destination Slot이 카세트 범위를 벗어났습니다. role=" + destination.CassetteRole +
                                ", slot=" + (destination.SlotIndex + 1) + ", slotCount=" + destinationCassette.Slots.Count, userName);
                        }

                        destinationSlot = destinationCassette.Slots[destination.SlotIndex];
                        if (destinationSlot == null)
                        {
                            return FailDataOnly(operation, source, destination, "DATA-ONLY-DEST-SLOT",
                                "Destination Slot 데이터가 없습니다. " + destination.DisplayText, userName);
                        }

                    }

                    // 기존 조건: Destination이 점유되어 있으면 DATA-ONLY-DEST-OCCUPIED로 이동을 거부했다.
                    //            (카세트 슬롯 점유 검사 + FindOtherWaferAtLocation 검사 2곳)
                    //            → 실물은 그 슬롯에 있는데 데이터가 다른 자재로 채워져 있으면 정렬이 불가능했다.
                    // 현재 기준: 점유 자재를 Source 위치로 교환(swap)한다. 어느 Material 데이터도 삭제하지 않는다.
                    // To do: [DATA ONLY 교환] 점유 Destination은 거부 대신 Source 위치와 교환한다.
                    WaferMaterial displaced = null;
                    if (destination.IsCassette &&
                        (destinationSlot.HasWafer || !string.IsNullOrWhiteSpace(destinationSlot.WaferId)))
                    {
                        string destinationSlotReason;
                        displaced = ResolveCassetteSlotWaferNoLock(
                            destinationSlot,
                            out destinationSlotReason);
                        if (displaced == null)
                        {
                            return FailDataOnly(
                                operation,
                                source,
                                destination,
                                "DATA-ONLY-DEST-POINTER",
                                "Destination Slot Material pointer가 올바르지 않습니다. " +
                                destinationSlotReason,
                                userName);
                        }
                    }

                    if (displaced == null)
                        displaced = FindOtherWaferAtLocation(wafer, destination.ToMaterialLocation());

                    // 원자 반영: 양쪽 pointer 제거 → Source 자재를 Destination에, 점유 자재를 Source 위치에 등록.
                    MaterialLocation beforeLocation = wafer.CurrentLocation;
                    MaterialLocation displacedBeforeLocation = displaced != null ? displaced.CurrentLocation : null;
                    beforeState = WaferMaterialStateText.Normalize(wafer.State);
                    displacedBeforeState = displaced != null
                        ? WaferMaterialStateText.Normalize(displaced.State)
                        : WaferMaterialState.Empty;
                    RemoveWaferFromCassetteSlot(wafer);
                    if (displaced != null)
                        RemoveWaferFromCassetteSlot(displaced);

                    ApplyDataOnlyPlacementNoLock(wafer, destination);
                    if (displaced != null)
                        ApplyDataOnlyPlacementNoLock(displaced, source);
                    afterState = WaferMaterialStateText.Normalize(wafer.State);
                    displacedAfterState = displaced != null
                        ? WaferMaterialStateText.Normalize(displaced.State)
                        : WaferMaterialState.Empty;

                    result.MaterialId = wafer.WaferId;
                    result.BeforeLocationText = beforeLocation != null ? beforeLocation.ToString() : "";
                    result.AfterLocationText = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";
                    result.SwappedMaterialId = displaced != null ? displaced.WaferId : "";
                    result.SwappedToText = displaced != null ? source.DisplayText : "";

                    SequenceTrace.MaterialChange(
                        "DataOnlyMove",
                        "wafer=" + wafer.WaferId,
                        "from=" + beforeLocation,
                        "to=" + wafer.CurrentLocation,
                        "state=" + beforeState + "->" + afterState,
                        "user=" + (userName ?? ""),
                        "noMotion=true");

                    if (displaced != null)
                    {
                        SequenceTrace.MaterialChange(
                            "DataOnlyMoveSwap",
                            "wafer=" + displaced.WaferId,
                            "from=" + displacedBeforeLocation,
                            "to=" + displaced.CurrentLocation,
                            "state=" + displacedBeforeState + "->" + displacedAfterState,
                            "user=" + (userName ?? ""),
                            "noMotion=true");
                    }
                }

                // 이동 결과를 즉시 Snapshot에 저장한다(백그라운드 스로틀 대기 없이 동기 flush).
                NotifyAndSave("DataOnlyManualMove");
                result.PersistenceSucceeded = TryFlushPendingSave("DataOnlyManualMove");
                result.Success = true;

                Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                    "[DATA ONLY] Material 데이터 이동 완료(장비 무동작). material=" + result.MaterialId +
                    ", source=" + result.SourceText +
                    ", destination=" + result.DestinationText +
                    ", state=" + beforeState + "->" + afterState +
                    ", swapped=" + (string.IsNullOrWhiteSpace(result.SwappedMaterialId) ? "-" : result.SwappedMaterialId) +
                    ", swappedTo=" + (string.IsNullOrWhiteSpace(result.SwappedToText) ? "-" : result.SwappedToText) +
                    ", swappedState=" + (string.IsNullOrWhiteSpace(result.SwappedMaterialId)
                        ? "-"
                        : displacedBeforeState + "->" + displacedAfterState) +
                    ", persisted=" + result.PersistenceSucceeded + " - Ok");
                return result;
            }
            catch (Exception ex)
            {
                return FailDataOnly(operation, source, destination, "DATA-ONLY-MOVE-EX",
                    "DATA ONLY 이동 중 예외가 발생했습니다: " + ex.Message, userName);
            }
            finally
            {
            }
        }

        public static DataOnlyOperationResult DeleteMaterialDataOnly(
            DataOnlyLocation location,
            string expectedMaterialId,
            string userName)
        {
            const string operation = "Delete";
            try
            {
                if (location == null)
                    return FailDataOnly(operation, null, null, "DATA-ONLY-DELETE-NULL", "삭제 위치가 지정되지 않았습니다.", userName);

                var result = new DataOnlyOperationResult
                {
                    Operation = operation,
                    SourceText = location.DisplayText,
                    DestinationText = ""
                };

                lock (_stateSync)
                {
                    WaferMaterial wafer;
                    string failureCode;
                    string failureMessage;
                    if (!TryFindSingleWaferAtDataOnlyLocation(location, out wafer, out failureCode, out failureMessage))
                        return FailDataOnly(operation, location, null, failureCode, failureMessage, userName);

                    if (!string.IsNullOrWhiteSpace(expectedMaterialId) &&
                        !string.Equals(wafer.WaferId, expectedMaterialId.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return FailDataOnly(operation, location, null, "DATA-ONLY-SOURCE-CHANGED",
                            "선택(Preview) 이후 대상 Material이 변경되었습니다. 다시 선택하십시오. expected=" +
                            expectedMaterialId + ", current=" + wafer.WaferId, userName);
                    }

                    MaterialLocation beforeLocation = wafer.CurrentLocation;

                    // 기존 중앙 삭제 계약(Clear*)과 동일: 위치 pointer 제거 + 논리 삭제(State=Empty, Location=Unknown).
                    // 잔존 CassetteLotId가 다음 mapping 등록을 막지 않도록 함께 비운다.
                    RemoveWaferFromCassetteSlot(wafer);
                    wafer.State = WaferMaterialState.Empty;
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                    wafer.CassetteLotId = "";
                    wafer.UpdatedAt = DateTime.Now;

                    result.MaterialId = wafer.WaferId;
                    result.BeforeLocationText = beforeLocation != null ? beforeLocation.ToString() : "";
                    result.AfterLocationText = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

                    SequenceTrace.MaterialChange(
                        "DataOnlyDelete",
                        "wafer=" + wafer.WaferId,
                        "from=" + beforeLocation,
                        "to=" + wafer.CurrentLocation,
                        "user=" + (userName ?? ""),
                        "noMotion=true");
                }

                NotifyAndSave("DataOnlyManualDelete");
                result.PersistenceSucceeded = TryFlushPendingSave("DataOnlyManualDelete");
                result.Success = true;

                Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                    "[DATA ONLY] Material 데이터 삭제 완료(장비 무동작, 실물 제거 아님). material=" + result.MaterialId +
                    ", location=" + result.SourceText +
                    ", persisted=" + result.PersistenceSucceeded + " - Ok");
                return result;
            }
            catch (Exception ex)
            {
                return FailDataOnly(operation, location, null, "DATA-ONLY-DELETE-EX",
                    "DATA ONLY 삭제 중 예외가 발생했습니다: " + ex.Message, userName);
            }
            finally
            {
            }
        }

        // To do: [DATA ONLY 교환] Source/Destination 양방향 배치를 한 함수로 처리한다(교환 시 대칭 적용).
        /// <summary>
        /// DATA ONLY 배치 반영입니다. 카세트면 슬롯 pointer와 출력 키를 등록하고, 스테이션이면 위치만 갱신합니다.
        /// 호출 전에 해당 Material의 기존 슬롯 pointer가 제거되어 있어야 합니다(_stateSync 보유 상태에서 호출).
        /// Material ID/검사결과/DieMap은 유지하고, State와 Cassette 메타데이터는
        /// 실제 Auto 이송 완료 상태와 동일하게 목적지 기준으로 맞춥니다.
        /// </summary>
        private static void ApplyDataOnlyPlacementNoLock(WaferMaterial wafer, DataOnlyLocation location)
        {
            if (wafer == null || location == null)
                return;

            if (location.IsCassette)
            {
                CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == location.CassetteRole);
                if (cassette != null)
                {
                    cassette.EnsureSlots();
                    if (location.SlotIndex >= 0 && location.SlotIndex < cassette.Slots.Count)
                    {
                        CassetteSlotMaterial slot = cassette.Slots[location.SlotIndex];
                        if (slot != null)
                        {
                            slot.WaferId = wafer.WaferId;
                            slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                            slot.HasWafer = true;
                        }
                    }

                    if (IsOutputCassetteRole(location.CassetteRole))
                    {
                        wafer.OutputCassetteId = cassette.CassetteId;
                        wafer.OutputCassetteRole = location.CassetteRole;
                        wafer.OutputSlotNumber = location.SlotIndex;
                        wafer.OutputGrade = location.CassetteRole == CassetteMaterialRole.Ng1
                            ? DieResult.NG
                            : DieResult.Good;
                    }

                    // DATA ONLY는 실제 자재 위치를 기준 정보로 복구하는 기능이다.
                    // 다른 Cassette/Slot로 옮긴 경우 다음 Auto가 이전 Source를 다시 참조하지 않도록
                    // 원본 Cassette 정보도 현재 목적지에 맞춘다.
                    wafer.SourceCassetteId = cassette.CassetteId;
                    wafer.SourceCassetteRole = location.CassetteRole;
                    wafer.SourceSlotNumber = location.SlotIndex;
                    wafer.SourceCassetteSlotPosition = double.NaN;

                    // 목적 Cassette LOT가 등록되어 있으면 그 LOT를 기준으로 맞춘다.
                    // Cassette LOT가 비어 있으면 기존 Material LOT를 Cassette에 승계한다.
                    if (!string.IsNullOrWhiteSpace(cassette.CassetteLotId))
                        wafer.CassetteLotId = cassette.CassetteLotId;
                    else if (!string.IsNullOrWhiteSpace(wafer.CassetteLotId))
                        cassette.CassetteLotId = wafer.CassetteLotId;
                }

                wafer.CurrentLocation = location.ToMaterialLocation();

                // 저장된 물리 슬롯 위치는 새 슬롯 기준으로 신뢰할 수 없다.
                // 물리 이동 목표는 중앙 Unit 계산기가 재계산하므로 여기서는 무효화만 한다.
                wafer.CurrentCassetteSlotPosition = double.NaN;
            }
            else
            {
                wafer.CurrentLocation = location.ToMaterialLocation();

                // Output Stage의 GOOD/NG 구분은 실제 배치 위치가 기준이다.
                // DATA ONLY로 Stage 위치를 복구할 때 Grade가 반대로 남아 다음 언로드 대상이
                // 틀어지지 않도록 Stage 종류와 함께 정규화한다.
                if (location.Kind == MaterialLocationKind.OutputStageGood)
                    wafer.OutputGrade = DieResult.Good;
                else if (location.Kind == MaterialLocationKind.OutputStageNg)
                    wafer.OutputGrade = DieResult.NG;
            }

            // Auto 정상 이송과 동일한 목적지 상태:
            // Cassette 반환 완료=Finish, Feeder 이송 중=WorkReady, Stage 공정 중=Working.
            wafer.State = ResolveDataOnlyTargetState(location.Kind);
            wafer.UpdatedAt = DateTime.Now;
        }

        private static WaferMaterialState ResolveDataOnlyTargetState(MaterialLocationKind kind)
        {
            switch (kind)
            {
                case MaterialLocationKind.InputCassette:
                case MaterialLocationKind.OutputCassette:
                    return WaferMaterialState.Finish;

                case MaterialLocationKind.InputFeeder:
                case MaterialLocationKind.OutputFeeder:
                    return WaferMaterialState.WorkReady;

                case MaterialLocationKind.InputStage:
                case MaterialLocationKind.OutputStageGood:
                case MaterialLocationKind.OutputStageNg:
                    return WaferMaterialState.Working;

                default:
                    throw new InvalidOperationException(
                        "DATA ONLY 목적지의 Material State를 결정할 수 없습니다. kind=" + kind);
            }
        }

        // DATA ONLY 대상 위치에서 Material을 정확히 하나 찾는다.
        // 포인터 불일치/중복이 있으면 조용히 하나를 선택하지 않고 실패를 반환한다.
        private static bool TryFindSingleWaferAtDataOnlyLocation(
            DataOnlyLocation location,
            out WaferMaterial wafer,
            out string failureCode,
            out string failureMessage)
        {
            wafer = null;
            failureCode = "";
            failureMessage = "";

            if (location.IsCassette)
            {
                var cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == location.CassetteRole);
                if (cassette == null)
                {
                    failureCode = "DATA-ONLY-SOURCE-CASSETTE";
                    failureMessage = "Source 카세트 상태 데이터가 없습니다. role=" + location.CassetteRole;
                    return false;
                }

                cassette.EnsureSlots();
                if (location.SlotIndex < 0 || location.SlotIndex >= cassette.Slots.Count)
                {
                    failureCode = "DATA-ONLY-SOURCE-SLOT-RANGE";
                    failureMessage = "Source Slot이 카세트 범위를 벗어났습니다. role=" + location.CassetteRole +
                                     ", slot=" + (location.SlotIndex + 1) + ", slotCount=" + cassette.Slots.Count;
                    return false;
                }

                var slot = cassette.Slots[location.SlotIndex];
                if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                {
                    failureCode = "DATA-ONLY-SOURCE-EMPTY";
                    failureMessage = "Source 위치에 Material 데이터가 없습니다. " + location.DisplayText;
                    return false;
                }

                string slotReason;
                var slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                if (slotWafer == null)
                {
                    failureCode = "DATA-ONLY-SOURCE-MISMATCH";
                    failureMessage = "Source Slot이 가리키는 Material 객체가 없습니다. " + location.DisplayText +
                                     ", waferId=" + slot.WaferId +
                                     ", reason=" + slotReason;
                    return false;
                }

                if (WaferMaterialStateText.Normalize(slotWafer.State) == WaferMaterialState.Empty)
                {
                    failureCode = "DATA-ONLY-SOURCE-EMPTY-STATE";
                    failureMessage = "Source Material 상태가 EMPTY입니다. " + location.DisplayText +
                                     ", waferId=" + slotWafer.WaferId;
                    return false;
                }

                MaterialLocation expected = location.ToMaterialLocation();
                if (!IsSameMaterialLocation(slotWafer.CurrentLocation, expected))
                {
                    failureCode = "DATA-ONLY-SOURCE-MISMATCH";
                    failureMessage = "Source Slot pointer와 Material 현재 위치가 불일치합니다. " + location.DisplayText +
                                     ", waferId=" + slotWafer.WaferId +
                                     ", currentLocation=" + slotWafer.CurrentLocation;
                    return false;
                }

                WaferMaterial duplicate = FindOtherWaferAtLocation(slotWafer, expected);
                if (duplicate != null)
                {
                    failureCode = "DATA-ONLY-SOURCE-DUPLICATE";
                    failureMessage = "Source 위치를 가리키는 Material이 둘 이상입니다. " + location.DisplayText +
                                     ", wafer1=" + slotWafer.WaferId + ", wafer2=" + duplicate.WaferId;
                    return false;
                }

                wafer = slotWafer;
                return true;
            }

            var candidates = State.Wafers
                .Where(w => w != null &&
                            w.CurrentLocation != null &&
                            w.CurrentLocation.Kind == location.Kind &&
                            WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                .ToList();

            if (candidates.Count == 0)
            {
                failureCode = "DATA-ONLY-SOURCE-EMPTY";
                failureMessage = "Source 위치에 Material 데이터가 없습니다. " + location.DisplayText;
                return false;
            }

            if (candidates.Count > 1)
            {
                failureCode = "DATA-ONLY-SOURCE-DUPLICATE";
                failureMessage = "Source 위치에 Material 데이터가 둘 이상입니다. " + location.DisplayText +
                                 ", materials=" + string.Join(",", candidates.Select(w => w.WaferId).ToArray());
                return false;
            }

            wafer = candidates[0];
            return true;
        }

        private static DataOnlyOperationResult FailDataOnly(
            string operation,
            DataOnlyLocation source,
            DataOnlyLocation destination,
            string failureCode,
            string failureMessage,
            string userName)
        {
            var result = DataOnlyOperationResult.Fail(operation, failureCode, failureMessage);
            result.SourceText = source != null ? source.DisplayText : "";
            result.DestinationText = destination != null ? destination.DisplayText : "";
            Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                "[DATA ONLY] " + operation + " 실패. source=" + result.SourceText +
                ", destination=" + result.DestinationText +
                ", code=" + failureCode +
                ", reason=" + failureMessage + " - Failed");
            return result;
        }

        public static bool InitializeOutputStageReceivePlan(QMC.CDT320.BinSide side)
        {
            try
            {
                WaferMaterial outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                if (outputWafer == null)
                    return false;

                // 출력 수령 계획은 레시피의 원형 빈맵(side별)에서 타겟 슬롯을 소스로 한다.
                DieMap binMap = LoadRecipeBinMap(side);
                if (binMap == null || binMap.DieMapX <= 0 || binMap.DieMapY <= 0)
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "Output receive plan initialize skipped: recipe bin map is missing. side=" + side + " - Check");
                    return false;
                }

                var project = RecipeStore.LoadLastOrDefault();
                PickupSubset pickup = ResolveOutputPickup(project);
                List<DieMapEntry> ordered = BuildOutputReceiveOrder(binMap, pickup);
                if (ordered.Count == 0)
                    return false;

                // [사용자 승인 2026-07-27] 계획 재초기화 시 수령 순서 캐시를 최신으로 갱신.
                _outputReceiveOrderCache[side] =
                    new System.Collections.Generic.KeyValuePair<string, List<DieMapEntry>>(
                        EnsureWaferInstanceIdNoLock(outputWafer),
                        ordered);

                // 입력 웨이퍼는 추적용(있으면 기록). 없어도 빈맵 기반 계획은 성립한다.
                WaferMaterial sourceWafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                outputWafer.OutputReceiveSourceWaferId = sourceWafer != null ? sourceWafer.WaferId : "";
                outputWafer.OutputReceiveSourceWaferInstanceId =
                    sourceWafer != null ? EnsureWaferInstanceIdNoLock(sourceWafer) : "";
                EnsureWaferInstanceIdNoLock(outputWafer);
                outputWafer.OutputReceiveDieMapX = binMap.DieMapX;
                outputWafer.OutputReceiveDieMapY = binMap.DieMapY;
                outputWafer.OutputReceivePitchX = binMap.PitchX;
                outputWafer.OutputReceivePitchY = binMap.PitchY;
                outputWafer.OutputReceiveDieSizeX = binMap.DieSizeX;
                outputWafer.OutputReceiveDieSizeY = binMap.DieSizeY;
                outputWafer.OutputReceiveOuterDiameterMm = binMap.OuterDiameterMm;
                // 좌표 규약: 빈맵 중심 기준 상대좌표를 유지하고, 모션 소비자가 ProcessPosition + PosX/PosY로 해석한다.
                outputWafer.OutputReceiveOriginX = binMap.OriginX;
                outputWafer.OutputReceiveOriginY = binMap.OriginY;
                outputWafer.OutputReceiveNextIndex = 0;
                outputWafer.OutputReceiveTotalCount = ordered.Count;
                outputWafer.OutputReceiveStartCorner = pickup.StartCorner.ToString();
                outputWafer.OutputReceiveDirection = pickup.Direction.ToString();
                outputWafer.OutputReceivePattern = pickup.Pattern.ToString();
                outputWafer.DieMapFrameObjId = binMap.FrameObjId ?? "";
                outputWafer.OutputReceiveSlots = BuildOutputReceiveSlots(ordered, side, binMap.PitchX, binMap.PitchY);
                if (outputWafer.DieIds == null)
                    outputWafer.DieIds = new List<string>();
                else
                    outputWafer.DieIds.Clear();
                outputWafer.State = WaferMaterialState.WorkReady;
                outputWafer.UpdatedAt = DateTime.Now;

                SequenceTrace.MaterialChange(
                    "OutputStageReceivePlanInitialize",
                    "wafer=" + outputWafer.WaferId,
                    "to=" + outputWafer.CurrentLocation,
                    "state=" + outputWafer.State,
                    "side=" + side,
                    "total=" + outputWafer.OutputReceiveTotalCount,
                    "sourceWafer=" + outputWafer.OutputReceiveSourceWaferId);
                NotifyAndSave("OutputStageReceivePlanInitialize");
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output receive plan initialize failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        // [사용자 승인 2026-07-27] 출력 수령 순서 캐시 — die마다 레시피 프로젝트/빈맵 파일
        // 로드와 전체 재정렬(BuildOutputReceiveOrder, 실측 ~16ms/die)을 반복하던 것을 출력
        // wafer 단위 1회로 줄인다. 키 = side + 출력 WaferId: wafer 교체 시 WaferId 불일치로
        // 자동 무효화되고, 수령 계획 재초기화(InitializeOutputStageReceivePlan)가 명시 갱신한다.
        // 호출은 전부 _stateSync lock 안(스레드 안전).
        // [원격 master 기준 2026-07-27] 동일 목적의 별도 파일 캐시와 병합하지 않고 이 구현 하나만 사용한다.
        private static readonly System.Collections.Generic.Dictionary<QMC.CDT320.BinSide, System.Collections.Generic.KeyValuePair<string, List<DieMapEntry>>> _outputReceiveOrderCache =
            new System.Collections.Generic.Dictionary<QMC.CDT320.BinSide, System.Collections.Generic.KeyValuePair<string, List<DieMapEntry>>>();

        private static List<DieMapEntry> ResolveOutputReceiveOrderCached(QMC.CDT320.BinSide side, WaferMaterial outputWafer)
        {
            if (outputWafer == null)
                return null;

            System.Collections.Generic.KeyValuePair<string, List<DieMapEntry>> cached;
            string outputWaferInstanceId = EnsureWaferInstanceIdNoLock(outputWafer);
            if (_outputReceiveOrderCache.TryGetValue(side, out cached) &&
                string.Equals(cached.Key, outputWaferInstanceId, StringComparison.OrdinalIgnoreCase) &&
                cached.Value != null && cached.Value.Count > 0)
                return cached.Value;

            DieMap binMap = LoadRecipeBinMap(side);
            if (binMap == null)
                return null;
            var project = RecipeStore.LoadLastOrDefault();
            PickupSubset pickup = ResolveOutputPickup(project);
            List<DieMapEntry> ordered = BuildOutputReceiveOrder(binMap, pickup);
            _outputReceiveOrderCache[side] =
                new System.Collections.Generic.KeyValuePair<string, List<DieMapEntry>>(
                    outputWaferInstanceId,
                    ordered);
            return ordered;
        }

        private static WaferMaterial ResolveCurrentInputSourceForOutputTargetNoLock()
        {
            return State.Wafers.FirstOrDefault(w =>
                w != null &&
                w.CurrentLocation != null &&
                w.CurrentLocation.Kind == MaterialLocationKind.InputStage &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
        }

        public static OutputStageReceiveTarget ReserveNextOutputStageReceiveTarget(QMC.CDT320.BinSide side)
        {
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                    if (outputWafer == null)
                        return null;

                    if (IsOutputStageReceiveComplete(outputWafer))
                        return null;

                    if (outputWafer.OutputReceiveTotalCount <= 0)
                    {
                        if (!InitializeOutputStageReceivePlan(side))
                            return null;

                        outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                        if (outputWafer == null || outputWafer.OutputReceiveTotalCount <= 0)
                            return null;
                    }

                    // 타겟 슬롯 순서는 레시피 원형 빈맵 + 출력 픽업 순서로 결정(계획 초기화와 동일).
                    // [사용자 승인 2026-07-27] wafer 단위 캐시 사용 — die당 파일 로드/재정렬 제거.
                    List<DieMapEntry> ordered = ResolveOutputReceiveOrderCached(side, outputWafer);
                    if (ordered == null || ordered.Count == 0)
                        return null;

                    int index = ResolveNextOutputReceiveIndex(outputWafer);

                    if (index >= ordered.Count)
                        return null;

                    DieMapEntry entry = ordered[index];
                    WaferMaterial sourceWafer = ResolveCurrentInputSourceForOutputTargetNoLock();
                    var target = new OutputStageReceiveTarget
                    {
                        StageLocation = ResolveOutputStageLocation(side),
                        OutputWaferId = outputWafer.WaferId,
                        OutputWaferInstanceId = EnsureWaferInstanceIdNoLock(outputWafer),
                        SourceWaferId = sourceWafer != null ? sourceWafer.WaferId : "",
                        SourceWaferInstanceId = sourceWafer != null
                            ? EnsureWaferInstanceIdNoLock(sourceWafer)
                            : "",
                        OrderIndex = index,
                        DieMapX = ResolveEntryMapX(entry),
                        DieMapY = ResolveEntryMapY(entry),
                        OffsetX = entry.PosX,
                        OffsetY = entry.PosY
                    };
                    target.TargetX = target.OffsetX;
                    target.TargetY = target.OffsetY;

                    outputWafer.OutputReceiveNextIndex = index;
                    outputWafer.UpdatedAt = DateTime.Now;
                    SequenceTrace.MaterialChange(
                        "OutputStageReceiveTargetReserve",
                        "wafer=" + outputWafer.WaferId,
                        "to=" + outputWafer.CurrentLocation,
                        "state=" + outputWafer.State,
                        "side=" + side,
                        "order=" + index,
                        "mapX=" + target.DieMapX,
                        "mapY=" + target.DieMapY);
                    NotifyAndSave("OutputStageReceiveTargetReserve");
                    return target;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output receive target reserve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static OutputStageReceiveTarget PeekNextOutputStageReceiveTarget(QMC.CDT320.BinSide side)
        {
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                    if (outputWafer == null)
                        return null;

                    int index = ResolveNextOutputReceiveIndex(outputWafer);
                    if (index < 0)
                        index = 0;

                    OutputReceiveSlotMaterial slot = null;
                    if (outputWafer.OutputReceiveSlots != null)
                    {
                        slot = outputWafer.OutputReceiveSlots
                            .Where(s => s != null && s.IsTarget)
                            .OrderBy(s => s.OrderIndex)
                            .FirstOrDefault(s => s.OrderIndex == index);

                        if (slot == null)
                        {
                            slot = outputWafer.OutputReceiveSlots
                                .Where(s => IsOutputReceiveSlotPending(s))
                                .OrderBy(s => s.OrderIndex)
                                .FirstOrDefault();
                        }
                    }

                    if (slot != null)
                    {
                        WaferMaterial sourceWafer = ResolveCurrentInputSourceForOutputTargetNoLock();
                        return new OutputStageReceiveTarget
                        {
                            StageLocation = ResolveOutputStageLocation(side),
                            OutputWaferId = outputWafer.WaferId,
                            OutputWaferInstanceId = EnsureWaferInstanceIdNoLock(outputWafer),
                            SourceWaferId = sourceWafer != null ? sourceWafer.WaferId : "",
                            SourceWaferInstanceId = sourceWafer != null
                                ? EnsureWaferInstanceIdNoLock(sourceWafer)
                                : "",
                            OrderIndex = slot.OrderIndex,
                            DieMapX = slot.DieMapX,
                            DieMapY = slot.DieMapY,
                            OffsetX = slot.PosX,
                            OffsetY = slot.PosY,
                            TargetX = slot.PosX,
                            TargetY = slot.PosY
                        };
                    }

                    if (outputWafer.OutputReceiveTotalCount <= 0)
                        return null;

                    // [사용자 승인 2026-07-27] wafer 단위 캐시 사용 — 호출당 파일 로드/재정렬 제거.
                    List<DieMapEntry> ordered = ResolveOutputReceiveOrderCached(side, outputWafer);
                    if (ordered == null || ordered.Count == 0 || index >= ordered.Count)
                        return null;

                    DieMapEntry entry = ordered[index];
                    WaferMaterial currentSourceWafer = ResolveCurrentInputSourceForOutputTargetNoLock();
                    var target = new OutputStageReceiveTarget
                    {
                        StageLocation = ResolveOutputStageLocation(side),
                        OutputWaferId = outputWafer.WaferId,
                        OutputWaferInstanceId = EnsureWaferInstanceIdNoLock(outputWafer),
                        SourceWaferId = currentSourceWafer != null ? currentSourceWafer.WaferId : "",
                        SourceWaferInstanceId = currentSourceWafer != null
                            ? EnsureWaferInstanceIdNoLock(currentSourceWafer)
                            : "",
                        OrderIndex = index,
                        DieMapX = ResolveEntryMapX(entry),
                        DieMapY = ResolveEntryMapY(entry),
                        OffsetX = entry.PosX,
                        OffsetY = entry.PosY
                    };
                    target.TargetX = target.OffsetX;
                    target.TargetY = target.OffsetY;
                    return target;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output receive target peek failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 콜렛 클리닝용: 출력 Bin 다이맵 슬롯을 "맨 끝쪽부터" 조회한다.
        /// 생산 배치는 앞(OrderIndex 오름차순)에서부터 소비하므로, 클리닝은 끝에서부터 써야 충돌이 가장 늦다.
        /// skipFromEnd는 이미 클리닝에 사용한 셀 수(끝에서부터의 커서)다.
        /// 대상 셀에 이미 die가 있으면(생산 커서와 만남) 실패로 반환해 상위에서 알람 처리한다.
        /// </summary>
        public static bool TryPeekOutputReceiveSlotFromEnd(
            QMC.CDT320.BinSide side,
            int skipFromEnd,
            out OutputStageReceiveTarget target,
            out string reason)
        {
            target = null;
            reason = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    WaferMaterial outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                    if (outputWafer == null)
                    {
                        reason = "OutputStage에 Bin이 없습니다. side=" + side;
                        return false;
                    }

                    if (outputWafer.OutputReceiveSlots == null || outputWafer.OutputReceiveSlots.Count == 0)
                    {
                        reason = "Bin 다이맵 슬롯 정보가 없습니다. side=" + side + ", bin=" + outputWafer.WaferId;
                        return false;
                    }

                    List<OutputReceiveSlotMaterial> targetSlots = outputWafer.OutputReceiveSlots
                        .Where(s => s != null && s.IsTarget)
                        .OrderByDescending(s => s.OrderIndex)
                        .ToList();
                    if (targetSlots.Count == 0)
                    {
                        reason = "Bin 다이맵에 사용 가능한 대상 셀이 없습니다. side=" + side + ", bin=" + outputWafer.WaferId;
                        return false;
                    }

                    if (skipFromEnd < 0)
                        skipFromEnd = 0;
                    if (skipFromEnd >= targetSlots.Count)
                    {
                        reason = "클리닝에 사용할 다이맵 셀이 소진되었습니다. side=" + side +
                                 ", bin=" + outputWafer.WaferId +
                                 ", cursor=" + skipFromEnd + ", targetSlots=" + targetSlots.Count;
                        return false;
                    }

                    OutputReceiveSlotMaterial slot = targetSlots[skipFromEnd];
                    if (!IsOutputReceiveSlotPending(slot))
                    {
                        reason = "클리닝 대상 셀에 이미 die가 있어 사용할 수 없습니다(생산 배치와 충돌). side=" + side +
                                 ", bin=" + outputWafer.WaferId +
                                 ", order=" + slot.OrderIndex +
                                 ", map=(" + slot.DieMapX + "," + slot.DieMapY + ")" +
                                 ", result=" + slot.Result +
                                 ", dieUid=" + (slot.DieUid ?? "");
                        return false;
                    }

                    target = new OutputStageReceiveTarget
                    {
                        StageLocation = ResolveOutputStageLocation(side),
                        OutputWaferId = outputWafer.WaferId,
                        OutputWaferInstanceId = EnsureWaferInstanceIdNoLock(outputWafer),
                        // Collet Cleaning은 Input Die를 Place하는 경로가 아니므로 source를 지정하지 않는다.
                        SourceWaferId = "",
                        SourceWaferInstanceId = "",
                        OrderIndex = slot.OrderIndex,
                        DieMapX = slot.DieMapX,
                        DieMapY = slot.DieMapY,
                        OffsetX = slot.PosX,
                        OffsetY = slot.PosY,
                        TargetX = slot.PosX,
                        TargetY = slot.PosY
                    };
                    return true;
                }
            }
            catch (Exception ex)
            {
                reason = "클리닝 대상 셀 조회 중 예외가 발생했습니다. error=" + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Collet cleaning cell peek failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool IsOutputStageReceiveTargetCurrent(
            QMC.CDT320.BinSide side,
            OutputStageReceiveTarget receiveTarget,
            out string reason)
        {
            lock (_stateSync)
            {
                WaferMaterial outputWafer;
                return IsOutputStageReceiveTargetCurrentNoLock(
                    side,
                    receiveTarget,
                    out outputWafer,
                out reason);
            }
        }

        private static bool IsOutputStageReceiveTargetCurrentNoLock(
            QMC.CDT320.BinSide side,
            OutputStageReceiveTarget receiveTarget,
            out WaferMaterial outputWafer,
            out string reason)
        {
            outputWafer = null;
            reason = "";
            if (receiveTarget == null)
            {
                reason = "Output receive target이 없습니다.";
                return false;
            }

            MaterialLocationKind expectedLocation = ResolveOutputStageLocation(side);
            if (receiveTarget.StageLocation != expectedLocation)
            {
                reason = "Output receive target side/location이 다릅니다. targetLocation=" +
                         receiveTarget.StageLocation + ", expectedLocation=" + expectedLocation;
                return false;
            }

            outputWafer = GetWaferAtLocation(expectedLocation);
            if (outputWafer == null)
            {
                reason = "OutputStage에 Bin Material이 없습니다. side=" + side;
                return false;
            }

            string currentInstanceId = EnsureWaferInstanceIdNoLock(outputWafer);
            if (string.IsNullOrWhiteSpace(receiveTarget.OutputWaferInstanceId))
            {
                reason = "Output receive target의 물리 Bin 세대 ID가 없습니다. output=" +
                         (receiveTarget.OutputWaferId ?? "");
                return false;
            }
            if (!string.Equals(
                receiveTarget.OutputWaferInstanceId,
                currentInstanceId,
                StringComparison.OrdinalIgnoreCase))
            {
                reason = "예약 후 Output Bin이 교체되었습니다. targetOutput=" +
                         (receiveTarget.OutputWaferId ?? "") +
                         ", targetInstance=" + receiveTarget.OutputWaferInstanceId +
                         ", currentOutput=" + (outputWafer.WaferId ?? "") +
                         ", currentInstance=" + currentInstanceId;
                return false;
            }
            if (!string.IsNullOrWhiteSpace(receiveTarget.OutputWaferId) &&
                !string.Equals(
                    receiveTarget.OutputWaferId,
                    outputWafer.WaferId,
                    StringComparison.OrdinalIgnoreCase))
            {
                reason = "Output receive target의 Bin 표시 ID가 현재 Bin과 다릅니다. target=" +
                         receiveTarget.OutputWaferId + ", current=" + outputWafer.WaferId;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 콜렛 클리닝에 사용한 다이맵 셀을 생산 배치 대상에서 제외한다(IsTarget=false).
        /// 설정 AllowPlaceOnCleanedCell=false일 때만 호출한다.
        /// </summary>
        public static bool TryExcludeOutputReceiveSlotForColletCleaning(
            QMC.CDT320.BinSide side,
            int orderIndex,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    WaferMaterial outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                    if (outputWafer == null || outputWafer.OutputReceiveSlots == null)
                    {
                        reason = "OutputStage Bin 또는 다이맵 슬롯 정보가 없습니다. side=" + side;
                        return false;
                    }

                    OutputReceiveSlotMaterial slot = outputWafer.OutputReceiveSlots
                        .FirstOrDefault(s => s != null && s.OrderIndex == orderIndex);
                    if (slot == null)
                    {
                        reason = "제외할 다이맵 슬롯을 찾을 수 없습니다. side=" + side + ", order=" + orderIndex;
                        return false;
                    }

                    if (!slot.IsTarget)
                        return true;

                    slot.IsTarget = false;
                    outputWafer.UpdatedAt = DateTime.Now;
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "콜렛 클리닝에 사용한 Bin 다이맵 셀을 생산 배치 대상에서 제외했습니다. side=" + side +
                        ", bin=" + outputWafer.WaferId +
                        ", order=" + orderIndex +
                        ", map=(" + slot.DieMapX + "," + slot.DieMapY + ") - Ok");
                    NotifyAndSave("ColletCleaningSlotExclude");
                    return true;
                }
            }
            catch (Exception ex)
            {
                reason = "클리닝 사용 셀 제외 중 예외가 발생했습니다. error=" + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Collet cleaning cell exclude failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool MoveDieToOutputStage(string dieId, QMC.CDT320.BinSide side)
        {
            return MoveDieToOutputStage(dieId, side, null);
        }

        public static bool MoveDieToOutputStage(string dieId, QMC.CDT320.BinSide side, OutputStageReceiveTarget receiveTarget)
        {
            return MoveDieToOutputStage(dieId, side, receiveTarget, false);
        }

        public static bool MoveDieToOutputStage(
            string dieId,
            QMC.CDT320.BinSide side,
            OutputStageReceiveTarget receiveTarget,
            bool preserveInspectionResult)
        {
            try
            {
                lock (_stateSync)
                {
                    if (string.IsNullOrWhiteSpace(dieId))
                        return false;

                    WaferMaterial outputWafer = null;
                    string targetReason;
                    if (receiveTarget != null &&
                        !IsOutputStageReceiveTargetCurrentNoLock(
                            side,
                            receiveTarget,
                            out outputWafer,
                            out targetReason))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Move die to output stage blocked: " + targetReason +
                            ", die=" + dieId + ", side=" + side + " - Blocked");
                        return false;
                    }
                    if (receiveTarget == null)
                    {
                        outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                        if (outputWafer == null)
                        {
                            Log.Write("Main", "SYSTEM", "MaterialStateService",
                                "Move die to output stage failed: output wafer is missing. die=" + dieId +
                                ", side=" + side + " - Failed");
                            return false;
                        }
                    }

                    string outputWaferInstanceId = EnsureWaferInstanceIdNoLock(outputWafer);
                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Move die to output stage failed: source DieMaterial is missing. die=" +
                            dieId + ", side=" + side + " - Failed");
                        return false;
                    }
                    if (receiveTarget != null &&
                        !string.IsNullOrWhiteSpace(receiveTarget.SourceWaferInstanceId) &&
                        !string.Equals(
                            receiveTarget.SourceWaferInstanceId ?? "",
                            die.InputWaferInstanceId ?? "",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Move die to output stage blocked: source Input Wafer 세대가 다릅니다. die=" +
                            dieId +
                            ", targetSource=" + (receiveTarget.SourceWaferId ?? "") +
                            ", targetSourceInstance=" + receiveTarget.SourceWaferInstanceId +
                            ", dieSource=" + (die.WaferID_Input ?? "") +
                            ", dieSourceInstance=" + (die.InputWaferInstanceId ?? "") +
                            ", side=" + side + " - Blocked");
                        return false;
                    }
                    MaterialLocationKind stageLocation = ResolveOutputStageLocation(side);
                    if (preserveInspectionResult &&
                        die.Result != DieResult.Good &&
                        die.Result != DieResult.NG)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Move die to output stage failed: final inspection result is not ready. die=" + dieId +
                            ", result=" + die.Result +
                            ", side=" + side + " - Failed");
                        return false;
                    }

                    MaterialLocation previousLocation = die.CurrentLocation;
                    die.CurrentLocation = new MaterialLocation { Kind = stageLocation };
                    if (!preserveInspectionResult)
                        die.Result = side == QMC.CDT320.BinSide.Ng ? DieResult.NG : DieResult.Good;
                    die.WaferID_Output = outputWafer.WaferId;
                    die.OutputWaferInstanceId = outputWaferInstanceId;
                    if (receiveTarget != null)
                    {
                        die.Bin_IndexX = receiveTarget.DieMapX;
                        die.Bin_IndexY = receiveTarget.DieMapY;
                        die.BinOffset = new VisionOffset
                        {
                            X = receiveTarget.TargetX,
                            Y = receiveTarget.TargetY,
                            IsValid = true
                        };
                    }
                    UpdateOutputReceiveSlot(outputWafer, die, side, receiveTarget);
                    die.UpdatedAt = DateTime.Now;

                    if (outputWafer.DieIds == null)
                        outputWafer.DieIds = new List<string>();
                    if (!outputWafer.DieIds.Any(id => string.Equals(id, dieId, StringComparison.OrdinalIgnoreCase)))
                        outputWafer.DieIds.Add(dieId);

                    outputWafer.OutputReceiveNextIndex = ResolveNextOutputReceiveIndex(outputWafer);
                    outputWafer.OutputGrade = side == QMC.CDT320.BinSide.Ng ? DieResult.NG : DieResult.Good;
                    outputWafer.State = IsOutputStageReceiveComplete(outputWafer)
                        ? WaferMaterialState.Finish
                        : WaferMaterialState.Working;
                    outputWafer.UpdatedAt = DateTime.Now;
                    SequenceTrace.MaterialChange(
                        "MoveDieToOutputStage",
                        "die=" + die.DieId,
                        "wafer=" + outputWafer.WaferId,
                        "from=" + previousLocation,
                        "to=" + die.CurrentLocation,
                        "state=" + outputWafer.State,
                        "side=" + side,
                        "order=" + outputWafer.OutputReceiveNextIndex,
                        "result=" + die.Result,
                        "preserveInspectionResult=" + preserveInspectionResult);
                    OutputWaferCsvSnapshotWriter.EnqueuePlacedDie(
                        "Place",
                        State != null ? State.RecipeName : "",
                        GetProductionLotId(),
                        side,
                        outputWafer,
                        die,
                        receiveTarget);
                    NotifyAndSave("MoveDieToOutputStage");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Move die to output stage failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool IsOutputStageReceiveComplete(QMC.CDT320.BinSide side)
        {
            try
            {
                lock (_stateSync)
                {
                    return IsOutputStageReceiveComplete(GetWaferAtLocation(ResolveOutputStageLocation(side)));
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output stage receive complete check failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool UpdateOutputStageDieInspection(
            string dieId,
            QMC.CDT320.BinSide side,
            OutputStageReceiveTarget receiveTarget,
            bool inspectionOk,
            VisionOffset offset,
            string raw,
            IDictionary<string, string> visionValues)
        {
            try
            {
                lock (_stateSync)
                {
                    if (string.IsNullOrWhiteSpace(dieId))
                        return false;

                    WaferMaterial outputWafer;
                    string targetReason;
                    if (!IsOutputStageReceiveTargetCurrentNoLock(
                        side,
                        receiveTarget,
                        out outputWafer,
                        out targetReason))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Output stage die inspection update blocked: " + targetReason +
                            ", die=" + dieId + ", side=" + side + " - Blocked");
                        return false;
                    }

                    string outputWaferInstanceId = EnsureWaferInstanceIdNoLock(outputWafer);
                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null ||
                        !string.Equals(
                            die.OutputWaferInstanceId ?? "",
                            outputWaferInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Output stage die inspection update blocked: Die/Output Bin 세대가 일치하지 않습니다. die=" +
                            dieId +
                            ", dieOutputInstance=" + (die != null ? die.OutputWaferInstanceId : "") +
                            ", currentOutputInstance=" + outputWaferInstanceId +
                            ", side=" + side + " - Blocked");
                        return false;
                    }

                    OutputReceiveSlotMaterial targetSlot = outputWafer.OutputReceiveSlots != null
                        ? outputWafer.OutputReceiveSlots.FirstOrDefault(s =>
                            s != null &&
                            s.OrderIndex == receiveTarget.OrderIndex)
                        : null;
                    if (targetSlot == null ||
                        !string.Equals(
                            targetSlot.DieUid ?? "",
                            dieId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Output stage die inspection update blocked: 예약 slot의 현재 Die가 다릅니다. die=" +
                            dieId +
                            ", output=" + outputWafer.WaferId +
                            ", outputInstance=" + outputWaferInstanceId +
                            ", order=" + receiveTarget.OrderIndex +
                            ", slotDie=" + (targetSlot != null ? targetSlot.DieUid : "") +
                            " - Blocked");
                        return false;
                    }

                    if (offset == null)
                        offset = new VisionOffset();

                    die.BinOffset = offset;
                    if (die.Inspections == null)
                        die.Inspections = new List<DieInspectionRecord>();

                    DieInspectionRecord record = die.Inspections.FirstOrDefault(x =>
                        x != null &&
                        string.Equals(x.InspectionType, "OutputPlaceVision", StringComparison.OrdinalIgnoreCase));
                    if (record == null)
                    {
                        record = new DieInspectionRecord { InspectionType = "OutputPlaceVision" };
                        die.Inspections.Add(record);
                    }

                    record.Result = inspectionOk ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng;
                    record.Offset = offset;
                    record.UpdatedAt = DateTime.Now;
                    record.Measurements = new List<InspectionMeasurement>
                    {
                        new InspectionMeasurement
                        {
                            Name = "OutputVisionResult",
                            Value = inspectionOk ? 1.0 : 0.0,
                            Unit = "bool",
                            RawValue = inspectionOk ? "OK" : "NG",
                            Result = inspectionOk ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng
                        },
                        new InspectionMeasurement
                        {
                            Name = "OutputVisionRaw",
                            RawValue = raw ?? "",
                            Result = inspectionOk ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng
                        }
                    };
                    AppendVisionValueMeasurements(
                        record.Measurements,
                        visionValues,
                        "OutputVision",
                        inspectionOk ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng);

                    targetSlot.DieUid = dieId;
                    targetSlot.SourceDieUid = dieId;
                    targetSlot.IsOutputInspectionDone = true;
                    targetSlot.IsOutputInspectionOk = inspectionOk;
                    targetSlot.OutputInspectionOffsetX = offset.X;
                    targetSlot.OutputInspectionOffsetY = offset.Y;
                    targetSlot.OutputInspectionOffsetT = offset.R;
                    targetSlot.OutputInspectionRaw = raw ?? "";
                    outputWafer.UpdatedAt = DateTime.Now;

                    die.UpdatedAt = DateTime.Now;
                    if (outputWafer != null)
                    {
                        OutputWaferCsvSnapshotWriter.EnqueuePlacedDie(
                            "OutputStageDieInspection",
                            State != null ? State.RecipeName : "",
                            GetProductionLotId(),
                            side,
                            outputWafer,
                            die,
                            receiveTarget);
                    }
                    NotifyAndSave("OutputStageDieInspection");
                    Log.Write("Main", "MATERIAL", "OutputStageDieInspection",
                        "Output stage die inspection updated. die=" + dieId +
                        ", side=" + side +
                        ", ok=" + inspectionOk +
                        ", offsetX=" + offset.X +
                        ", offsetY=" + offset.Y +
                        ", offsetT=" + offset.R + " - Ok");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output stage die inspection update failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void AppendVisionValueMeasurements(
            List<InspectionMeasurement> measurements,
            IDictionary<string, string> values,
            string prefix,
            MaterialInspectionResult defaultResult)
        {
            if (measurements == null || values == null || values.Count == 0)
                return;

            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "Vision" : prefix;
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (IsVisionPassKey(pair.Key))
                    continue;

                double value;
                QMC.CDT320.VisionComm.VisionProtocolResponse.TryParseDouble(pair.Value, out value);
                measurements.Add(new InspectionMeasurement
                {
                    Name = safePrefix + "_" + NormalizeVisionMeasurementKey(pair.Key),
                    Value = value,
                    Unit = "",
                    RawValue = pair.Value ?? "",
                    Result = ResolveVisionMeasurementResult(values, pair.Key, defaultResult)
                });
            }
        }

        private static bool IsVisionPassKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key) &&
                   key.EndsWith("_pass", StringComparison.OrdinalIgnoreCase);
        }

        private static MaterialInspectionResult ResolveVisionMeasurementResult(
            IDictionary<string, string> values,
            string key,
            MaterialInspectionResult defaultResult)
        {
            if (values == null || string.IsNullOrWhiteSpace(key))
                return defaultResult;

            string passText;
            if (!values.TryGetValue(key + "_pass", out passText))
                return defaultResult;

            bool pass;
            if (TryParseVisionPassValue(passText, out pass))
                return pass ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng;

            return defaultResult;
        }

        private static bool TryParseVisionPassValue(string text, out bool pass)
        {
            pass = false;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string value = text.Trim();
            if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "ok", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "pass", StringComparison.OrdinalIgnoreCase))
            {
                pass = true;
                return true;
            }

            if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "ng", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "fail", StringComparison.OrdinalIgnoreCase))
            {
                pass = false;
                return true;
            }

            return false;
        }

        private static string NormalizeVisionMeasurementKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "unknown";

            char[] chars = key.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                bool ok = (c >= 'a' && c <= 'z') ||
                          (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') ||
                          c == '_';
                if (!ok)
                    chars[i] = '_';
            }

            return new string(chars);
        }

        public static bool IsOutputStageReceiveAvailable(QMC.CDT320.BinSide side)
        {
            string reason;
            return IsOutputStageReceiveAvailable(side, out reason);
        }

        public static bool IsOutputStageReceiveAvailable(QMC.CDT320.BinSide side, out string reason)
        {
            reason = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    WaferMaterial outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                    if (outputWafer == null)
                    {
                        reason = "Output stage material is missing. side=" + side;
                        return false;
                    }

                    WaferMaterialState state = outputWafer != null
                        ? WaferMaterialStateText.Normalize(outputWafer.State)
                        : WaferMaterialState.Empty;
                    if (state == WaferMaterialState.Finish)
                    {
                        reason = "Output stage material is already finish. side=" + side +
                                 ", waferId=" + outputWafer.WaferId;
                        return false;
                    }

                    if (outputWafer.OutputReceiveTotalCount <= 0)
                    {
                        reason = "Output stage receive plan is not initialized. side=" + side +
                                 ", waferId=" + outputWafer.WaferId +
                                 ", total=" + outputWafer.OutputReceiveTotalCount;
                        return false;
                    }

                    if (IsOutputStageReceiveComplete(outputWafer))
                    {
                        reason = "Output stage receive plan is complete. side=" + side +
                                 ", waferId=" + outputWafer.WaferId +
                                 ", placed=" + (outputWafer.DieIds != null ? outputWafer.DieIds.Count(id => !string.IsNullOrWhiteSpace(id)) : 0) +
                                 ", total=" + outputWafer.OutputReceiveTotalCount;
                        return false;
                    }

                    reason = "Output stage can receive. side=" + side +
                             ", waferId=" + outputWafer.WaferId +
                             ", placed=" + (outputWafer.DieIds != null ? outputWafer.DieIds.Count(id => !string.IsNullOrWhiteSpace(id)) : 0) +
                             ", total=" + outputWafer.OutputReceiveTotalCount;
                    return true;
                }
            }
            catch (Exception ex)
            {
                reason = "Output stage receive available check failed: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool IsOutputStageReceiveComplete(WaferMaterial outputWafer)
        {
            if (outputWafer == null)
                return false;

            if (WaferMaterialStateText.Normalize(outputWafer.State) == WaferMaterialState.Finish)
                return true;

            if (outputWafer.OutputReceiveSlots != null && outputWafer.OutputReceiveSlots.Count > 0)
            {
                List<OutputReceiveSlotMaterial> targetSlots = outputWafer.OutputReceiveSlots
                    .Where(s => s != null && s.IsTarget)
                    .ToList();
                if (targetSlots.Count > 0)
                    return targetSlots.All(s => !IsOutputReceiveSlotPending(s));
            }

            int total = outputWafer.OutputReceiveTotalCount;
            if (total <= 0)
                return false;

            int placed = outputWafer.DieIds != null ? outputWafer.DieIds.Count(id => !string.IsNullOrWhiteSpace(id)) : 0;
            return placed >= total;
        }

        private static int ResolveProcessTestSlotCount(CassetteMaterialRole role)
        {
            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c.Role == role);
            if (cassette != null && cassette.SlotCount > 0)
                return cassette.SlotCount;
            return 25;
        }

        private static bool IsCassetteCurrentlyEnabled(CassetteMaterialRole role)
        {
            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c.Role == role);
            return cassette != null && cassette.IsEnabled;
        }

        private static List<bool> BuildProcessTestSlotMap(int slotCount, int waferCount)
        {
            var map = new List<bool>();
            if (slotCount < 0)
                slotCount = 0;
            if (waferCount < 0)
                waferCount = 0;

            for (int i = 0; i < slotCount; i++)
                map.Add(i < waferCount);
            return map;
        }

        private static void ClearActiveProcessLocationsNoLock()
        {
            var activeKinds = new[]
            {
                MaterialLocationKind.InputStage,
                MaterialLocationKind.OutputStageGood,
                MaterialLocationKind.OutputStageNg
            };

            List<WaferMaterial> activeWafers = State.Wafers
                .Where(w => w != null &&
                            w.CurrentLocation != null &&
                            activeKinds.Contains(w.CurrentLocation.Kind))
                .ToList();
            var activeWaferIds = activeWafers
                .Select(w => w.WaferId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var activeWaferInstanceIds = new HashSet<string>(
                activeWafers
                    .Select(EnsureWaferInstanceIdNoLock)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            if (activeWaferIds.Count > 0)
            {
                State.Dies.RemoveAll(d =>
                    d != null &&
                    ((!string.IsNullOrWhiteSpace(d.InputWaferInstanceId) &&
                      activeWaferInstanceIds.Contains(d.InputWaferInstanceId)) ||
                     (!string.IsNullOrWhiteSpace(d.OutputWaferInstanceId) &&
                      activeWaferInstanceIds.Contains(d.OutputWaferInstanceId)) ||
                     (string.IsNullOrWhiteSpace(d.InputWaferInstanceId) &&
                      string.IsNullOrWhiteSpace(d.OutputWaferInstanceId) &&
                      ((!string.IsNullOrWhiteSpace(d.WaferID_Input) && activeWaferIds.Contains(d.WaferID_Input)) ||
                       (!string.IsNullOrWhiteSpace(d.WaferID_Output) && activeWaferIds.Contains(d.WaferID_Output))))));
            }

            foreach (WaferMaterial wafer in State.Wafers)
            {
                if (wafer == null || wafer.CurrentLocation == null)
                    continue;

                if (!activeKinds.Contains(wafer.CurrentLocation.Kind))
                    continue;

                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.State = WaferMaterialState.Empty;
                wafer.UpdatedAt = DateTime.Now;
            }
        }

        private static bool IsUsableSourceMap(DieMap map)
        {
            try
            {
                return map != null &&
                       map.DieMapX > 0 &&
                       map.DieMapY > 0 &&
                       map.Entries != null &&
                       map.Entries.Count > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static DieMap LoadRecipeInputDieMapForProcessTest(RecipeProject project)
        {
            try
            {
                if (project == null)
                    return null;

                string path;
                string reason;
                // 현재 기준: Process Test도 실제 Die Mapping과 같은 레시피/외부맵 로더를 사용한다.
                DieMap map = RecipeDieMapResolver.LoadCompatibleMap(project, RecipeMapKind.Input, out path, out reason);
                if (map != null)
                {
                    PickupSequenceGenerator.ApplySequenceNumbers(map, ResolveInputPickup(project));
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "공정 테스트 입력 DieMap 로드 완료. path=" + path +
                        ", dieMap=" + map.DieMapX + "x" + map.DieMapY +
                        ", target=" + map.Entries.Count(e => e != null && e.IsTarget) + " - Ok");
                }
                else if (!string.IsNullOrWhiteSpace(reason))
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "공정 테스트 입력 DieMap 로드 보류: " + reason + " - Check");
                }
                return DieMapGenerator.Normalize(map);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "공정 테스트 입력 DieMap 로드 실패: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static DieMap CreateFallbackInputDieMapForProcessTest(RecipeProject project, string tapeFrameSpecName)
        {
            try
            {
                int dieMapX = 5;
                int dieMapY = 5;
                double dieSizeX = project != null && project.Die != null && project.Die.WidthMm > 0.0
                    ? project.Die.WidthMm
                    : 1.0;
                double dieSizeY = project != null && project.Die != null && project.Die.HeightMm > 0.0
                    ? project.Die.HeightMm
                    : 1.0;
                double pitchGapX = 0.0;
                double pitchGapY = 0.0;

                TapeFrameSpec spec = MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null
                    ? MaterialSpecs.Data.Frames.FirstOrDefault(f => string.Equals(f.Name, tapeFrameSpecName, StringComparison.OrdinalIgnoreCase))
                    : null;
                if (spec != null)
                {
                    if (spec.DieSizeX > 0.0) dieSizeX = spec.DieSizeX;
                    if (spec.DieSizeY > 0.0) dieSizeY = spec.DieSizeY;
                    pitchGapX = Math.Max(0.0, spec.PitchX);
                    pitchGapY = Math.Max(0.0, spec.PitchY);
                    dieMapX = ResolvePitchBasedGridCount(
                        spec.OuterDiameterMm,
                        DieMapGenerator.CalculateCenterStep(dieSizeX, pitchGapX),
                        dieSizeX,
                        spec.DieMapX);
                    dieMapY = ResolvePitchBasedGridCount(
                        spec.OuterDiameterMm,
                        DieMapGenerator.CalculateCenterStep(dieSizeY, pitchGapY),
                        dieSizeY,
                        spec.DieMapY);
                }
                else if (project != null && project.Frame != null)
                {
                    if (project.Frame.DieSizeX > 0.0) dieSizeX = project.Frame.DieSizeX;
                    if (project.Frame.DieSizeY > 0.0) dieSizeY = project.Frame.DieSizeY;
                    pitchGapX = Math.Max(0.0, project.Frame.PitchX);
                    pitchGapY = Math.Max(0.0, project.Frame.PitchY);
                    dieMapX = ResolvePitchBasedGridCount(
                        project.Frame.OuterDiameterMm,
                        DieMapGenerator.CalculateCenterStep(dieSizeX, pitchGapX),
                        dieSizeX,
                        project.Frame.DieMapX);
                    dieMapY = ResolvePitchBasedGridCount(
                        project.Frame.OuterDiameterMm,
                        DieMapGenerator.CalculateCenterStep(dieSizeY, pitchGapY),
                        dieSizeY,
                        project.Frame.DieMapY);
                }

                DieMap map = DieMapGenerator.GenerateRect(
                    Math.Max(1, dieMapX),
                    Math.Max(1, dieMapY),
                    dieSizeX,
                    dieSizeY,
                    pitchGapX,
                    pitchGapY,
                    "PROCESS-TEST-INPUT");
                PickupSequenceGenerator.ApplySequenceNumbers(map, ResolveInputPickup(project));
                return DieMapGenerator.Normalize(map);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "공정 테스트 입력 DieMap 생성 실패: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static void RecenterInputDieMapForProcessTest(DieMap map, QMC.CDT320.InputStageUnit inputStage)
        {
            try
            {
                if (!IsUsableSourceMap(map) || inputStage == null)
                    return;

                List<DieMapEntry> entries = map.Entries
                    .Where(e => e != null && e.IsTarget)
                    .ToList();
                if (entries.Count == 0)
                    entries = map.Entries.Where(e => e != null).ToList();
                if (entries.Count == 0)
                    return;

                double targetCenterX = inputStage.ResolveWorkAreaCenterX();
                double targetCenterY = inputStage.ResolveWorkAreaCenterY();
                string centerSource = "WorkAreaCenter";
                if (inputStage.Recipe != null)
                {
                    inputStage.Recipe.EnsurePositionObjects();
                    targetCenterX = inputStage.Recipe.VisionX.ProcessPosition;
                    targetCenterY = inputStage.Recipe.WaferY.ProcessPosition;
                    centerSource = "Recipe ProcessPosition";
                }
                double pitchX = map.PitchX > 0.0 ? map.PitchX : ResolveDieMapPitch(entries, true);
                double pitchY = map.PitchY > 0.0 ? map.PitchY : ResolveDieMapPitch(entries, false);
                if (pitchX <= 0.0)
                    pitchX = 1.0;
                if (pitchY <= 0.0)
                    pitchY = 1.0;

                double centerGridX = Math.Max(0, map.DieMapX - 1) / 2.0;
                double originX = targetCenterX - pitchX * centerGridX;
                // 장비 Y 엔코더 기준으로 local row 0은 중심보다 음수 방향에 둔다.
                double originY = targetCenterY + DieMapGenerator.CalculateCenteredOriginY(map.DieMapY, pitchY);

                map.PitchX = pitchX;
                map.PitchY = pitchY;
                map.OriginX = originX;
                map.OriginY = originY;
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;

                    double equipmentGridX = entry.EquipmentGridX;
                    double equipmentGridY = entry.EquipmentGridY;
                    if (double.IsNaN(equipmentGridX) || double.IsInfinity(equipmentGridX))
                        equipmentGridX = ResolveEntryMapX(entry) - centerGridX;
                    if (double.IsNaN(equipmentGridY) || double.IsInfinity(equipmentGridY))
                        equipmentGridY = DieMapGenerator.CalculateEquipmentGridY(ResolveEntryMapY(entry), map.DieMapY);

                    entry.EquipmentGridX = equipmentGridX;
                    entry.EquipmentGridY = equipmentGridY;
                    entry.PosX = targetCenterX + equipmentGridX * pitchX;
                    entry.PosY = targetCenterY + equipmentGridY * pitchY;
                }

                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "공정 테스트 InputStage DieMap 좌표를 공정 위치 기준으로 생성했습니다. " +
                    "source=" + centerSource +
                    ", centerX=" + targetCenterX.ToString("F3") +
                    ", centerY=" + targetCenterY.ToString("F3") +
                    ", originX=" + originX.ToString("F3") +
                    ", originY=" + originY.ToString("F3") +
                    ", pitchX=" + pitchX.ToString("F6") +
                    ", pitchY=" + pitchY.ToString("F6") + " - Ok");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "공정 테스트 InputStage DieMap 좌표 보정 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool IsExternalDieMap(DieMap map)
        {
            return map != null &&
                   !string.IsNullOrWhiteSpace(map.EdgeSkipMode) &&
                   string.Equals(map.EdgeSkipMode, "ExternalMap", StringComparison.OrdinalIgnoreCase);
        }

        private static double ResolveDieMapPitch(List<DieMapEntry> entries, bool xAxis)
        {
            try
            {
                if (entries == null || entries.Count == 0)
                    return 0.0;

                List<DieMapEntry> ordered = entries
                    .Where(e => e != null)
                    .OrderBy(e => xAxis ? ResolveEntryMapX(e) : ResolveEntryMapY(e))
                    .ThenBy(e => xAxis ? ResolveEntryMapY(e) : ResolveEntryMapX(e))
                    .ToList();

                for (int i = 1; i < ordered.Count; i++)
                {
                    int indexDelta = xAxis
                        ? ResolveEntryMapX(ordered[i]) - ResolveEntryMapX(ordered[i - 1])
                        : ResolveEntryMapY(ordered[i]) - ResolveEntryMapY(ordered[i - 1]);
                    if (indexDelta == 0)
                        continue;

                    double positionDelta = xAxis
                        ? ordered[i].PosX - ordered[i - 1].PosX
                        : ordered[i].PosY - ordered[i - 1].PosY;
                    if (Math.Abs(positionDelta) > 1e-9)
                        return Math.Abs(positionDelta / indexDelta);
                }

                return 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private static int ApplyProcessTestInputDieMaterialsNoLock(DieMap map, WaferMaterial wafer)
        {
            if (map == null || wafer == null)
                return 0;

            string identityReason;
            if (!TryAssignPhysicalDieIds(map, wafer, out identityReason))
                throw new InvalidOperationException("Process Test Input Die identity 생성 실패. " + identityReason);

            if (wafer.DieIds == null)
                wafer.DieIds = new List<string>();
            wafer.DieIds.Clear();

            int targetCount = 0;
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;

                int mapX = ResolveEntryMapX(entry);
                int mapY = ResolveEntryMapY(entry);
                int originalX = DieMapGenerator.ResolveOriginalMapIndexX(entry);
                int originalY = DieMapGenerator.ResolveOriginalMapIndexY(entry);
                string dieId = BuildPhysicalDieId(wafer, originalX, originalY);
                entry.DieUid = dieId;

                DieMaterial die = GetOrCreateDieMaterial(dieId);
                die.WaferID_Input = wafer.WaferId;
                die.InputWaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                die.WaferID_Output = "";
                die.OutputWaferInstanceId = "";
                die.Wafer_IndexX = mapX;
                die.Wafer_IndexY = mapY;
                die.Wafer_OriginalIndexX = originalX;
                die.Wafer_OriginalIndexY = originalY;
                die.InputSequenceNo = entry.SequenceNo;
                die.Input_BinCode = entry.IsTarget ? entry.BinCode : 0;
                die.IsInputTarget = entry.IsTarget;
                die.Output_BinCode = 0;
                die.Bin_IndexX = -1;
                die.Bin_IndexY = -1;
                die.CurrentLocation = new MaterialLocation { Kind = entry.IsTarget ? MaterialLocationKind.InputStage : MaterialLocationKind.Unknown };
                die.ReservedPickerLocation = MaterialLocationKind.Unknown;
                die.ReservedPickerNo = -1;
                // 현재 기준: Process Test Data 생성도 새 Input 맵과 동일하게 Pick/검사 이력을 비운다.
                die.PickedPickerLocation = MaterialLocationKind.Unknown;
                die.PickedPickerNo = -1;
                die.PickedAt = DateTime.MinValue;
                die.Result = DieResult.Unknown;
                if (die.NgCodes == null)
                    die.NgCodes = new List<string>();
                else
                    die.NgCodes.Clear();
                if (die.Inspections == null)
                    die.Inspections = new List<DieInspectionRecord>();
                else
                    die.Inspections.Clear();
                if (die.WaferOffset == null)
                    die.WaferOffset = new VisionOffset();
                die.WaferOffset.X = entry.PosX;
                die.WaferOffset.Y = entry.PosY;
                die.WaferOffset.R = 0.0;
                die.WaferOffset.IsValid = true;
                if (die.BinOffset == null)
                    die.BinOffset = new VisionOffset();
                die.BinOffset.X = 0.0;
                die.BinOffset.Y = 0.0;
                die.BinOffset.R = 0.0;
                die.BinOffset.IsValid = false;
                die.UpdatedAt = DateTime.Now;

                wafer.DieIds.Add(dieId);
                if (entry.IsTarget)
                    targetCount++;
            }

            return targetCount;
        }

        private static void BindProcessTestStageWaferToCassetteSlotNoLock(
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            WaferMaterial wafer,
            MaterialLocationKind stageLocation,
            WaferMaterialState state,
            string lotId,
            string tapeFrameSpecName,
            double slotPosition = double.NaN)
        {
            try
            {
                if (wafer == null)
                    return;

                CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
                if (cassette == null)
                    return;

                cassette.EnsureSlots();
                if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                    return;

                CassetteSlotMaterial slot = cassette.Slots[slotNumber];
                string previousWaferId = slot != null ? slot.WaferId : "";
                if (!string.IsNullOrWhiteSpace(previousWaferId) &&
                    !string.Equals(previousWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                {
                    WaferMaterial previous = State.Wafers.FirstOrDefault(w =>
                        string.Equals(w.WaferId, previousWaferId, StringComparison.OrdinalIgnoreCase));
                    if (previous != null)
                    {
                        previous.CurrentLocation = MaterialLocation.Unknown();
                        previous.State = WaferMaterialState.Empty;
                        previous.UpdatedAt = DateTime.Now;
                    }
                }

                wafer.CassetteLotId = lotId ?? wafer.CassetteLotId;
                wafer.SourceCassetteId = cassette.CassetteId;
                wafer.SourceCassetteRole = cassetteRole;
                wafer.SourceSlotNumber = slotNumber;
                wafer.CurrentLocation = new MaterialLocation { Kind = stageLocation };
                wafer.State = WaferMaterialStateText.Normalize(state);
                wafer.TapeFrameSpecName = tapeFrameSpecName ?? wafer.TapeFrameSpecName;
                // 실장비 테스트: source slot 복귀 목표로 쓸 수 있게 슬롯 포지션(검출+로딩 오프셋)을 함께 저장한다.
                ApplyWaferCassettePosition(wafer, slotPosition);

                if (cassetteRole == CassetteMaterialRole.Good1 ||
                    cassetteRole == CassetteMaterialRole.Good2 ||
                    cassetteRole == CassetteMaterialRole.Ng1)
                {
                    wafer.OutputCassetteId = cassette.CassetteId;
                    wafer.OutputCassetteRole = cassetteRole;
                    wafer.OutputSlotNumber = slotNumber;
                }

                cassette.CassetteLotId = lotId ?? cassette.CassetteLotId;
                cassette.IsMapped = true;
                cassette.IsEnabled = true;
                cassette.IsPresent = true;
                cassette.LastScanTime = DateTime.Now;

                bool isStageWaferRemovedFromSourceSlot =
                    (stageLocation == MaterialLocationKind.InputStage &&
                     (cassetteRole == CassetteMaterialRole.Input1 || cassetteRole == CassetteMaterialRole.Input2)) ||
                    (stageLocation == MaterialLocationKind.OutputStageGood &&
                     (cassetteRole == CassetteMaterialRole.Good1 || cassetteRole == CassetteMaterialRole.Good2)) ||
                    (stageLocation == MaterialLocationKind.OutputStageNg &&
                     cassetteRole == CassetteMaterialRole.Ng1);
                if (isStageWaferRemovedFromSourceSlot)
                {
                    // Stage의 테스트 Wafer/Bin은 source slot에서 이미 꺼낸 상태이므로
                    // 동일 Material을 Stage와 cassette slot에 동시에 점유시키지 않는다.
                    slot.WaferId = "";
                    slot.WaferInstanceId = "";
                    slot.HasWafer = false;
                }
                else
                {
                    slot.WaferId = wafer.WaferId;
                    slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                    slot.HasWafer = true;
                }
                wafer.UpdatedAt = DateTime.Now;

                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "공정 테스트 Stage wafer와 Cassette slot을 동기화했습니다. role=" + cassetteRole +
                    ", slot=" + (slotNumber + 1).ToString("00") +
                    ", wafer=" + wafer.WaferId +
                    ", location=" + stageLocation +
                    ", state=" + wafer.State +
                    ", sourceSlotOccupied=" + slot.HasWafer + " - Ok");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "공정 테스트 Stage/Cassette slot 동기화 실패: role=" + cassetteRole +
                    ", slot=" + (slotNumber + 1).ToString("00") +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static WaferMaterial CreateProcessTestOutputStageWaferNoLock(
            QMC.CDT320.BinSide side,
            string lotId,
            string timestamp,
            string tapeFrameSpecName,
            string sourceWaferId,
            RecipeProject project)
        {
            MaterialLocationKind location = ResolveOutputStageLocation(side);
            string waferId = side == QMC.CDT320.BinSide.Ng
                ? "TEST-NG-STAGE-" + timestamp
                : "TEST-GOOD-STAGE-" + timestamp;

            WaferMaterial wafer = GetOrCreateWafer(waferId);
            wafer.CassetteLotId = lotId;
            wafer.CurrentLocation = new MaterialLocation { Kind = location };
            wafer.State = WaferMaterialState.Working;
            wafer.TapeFrameSpecName = tapeFrameSpecName;
            wafer.OutputGrade = side == QMC.CDT320.BinSide.Ng ? DieResult.NG : DieResult.Good;
            wafer.OutputCassetteRole = side == QMC.CDT320.BinSide.Ng ? CassetteMaterialRole.Ng1 : CassetteMaterialRole.Good1;
            wafer.OutputSlotNumber = 0;
            wafer.SourceCassetteId = wafer.OutputCassetteRole.ToString();
            wafer.SourceCassetteRole = wafer.OutputCassetteRole;
            wafer.SourceSlotNumber = 0;

            DieMap binMap = LoadRecipeBinMap(side);
            if (!IsUsableSourceMap(binMap))
                binMap = DieMapGenerator.GenerateRect(5, 5, 1.0, 1.0, 0.0, 0.0, side == QMC.CDT320.BinSide.Ng ? "PROCESS-TEST-NG" : "PROCESS-TEST-GOOD");
            binMap = DieMapGenerator.Normalize(binMap);

            PickupSubset pickup = ResolveOutputPickup(project);
            List<DieMapEntry> ordered = BuildOutputReceiveOrder(binMap, pickup);

            wafer.OutputReceiveSourceWaferId = sourceWaferId ?? "";
            WaferMaterial sourceWafer = State.Wafers.FirstOrDefault(w =>
                w != null &&
                string.Equals(w.WaferId, sourceWaferId ?? "", StringComparison.OrdinalIgnoreCase) &&
                w.CurrentLocation != null &&
                w.CurrentLocation.Kind == MaterialLocationKind.InputStage);
            wafer.OutputReceiveSourceWaferInstanceId =
                sourceWafer != null ? EnsureWaferInstanceIdNoLock(sourceWafer) : "";
            wafer.OutputReceiveDieMapX = binMap.DieMapX;
            wafer.OutputReceiveDieMapY = binMap.DieMapY;
            wafer.OutputReceivePitchX = binMap.PitchX;
            wafer.OutputReceivePitchY = binMap.PitchY;
            wafer.OutputReceiveDieSizeX = binMap.DieSizeX;
            wafer.OutputReceiveDieSizeY = binMap.DieSizeY;
            wafer.OutputReceiveOuterDiameterMm = binMap.OuterDiameterMm;
            wafer.OutputReceiveOriginX = binMap.OriginX;
            wafer.OutputReceiveOriginY = binMap.OriginY;
            wafer.OutputReceiveNextIndex = 0;
            wafer.OutputReceiveTotalCount = ordered.Count;
            wafer.OutputReceiveStartCorner = pickup != null ? pickup.StartCorner.ToString() : "";
            wafer.OutputReceiveDirection = pickup != null ? pickup.Direction.ToString() : "";
            wafer.OutputReceivePattern = pickup != null ? pickup.Pattern.ToString() : "";
            wafer.DieMapFrameObjId = binMap.FrameObjId ?? "";
            wafer.OutputReceiveSlots = BuildOutputReceiveSlots(ordered, side, binMap.PitchX, binMap.PitchY);
            if (wafer.DieIds == null)
                wafer.DieIds = new List<string>();
            else
                wafer.DieIds.Clear();
            wafer.UpdatedAt = DateTime.Now;
            return wafer;
        }

        private static MaterialLocationKind ResolveOutputStageLocation(QMC.CDT320.BinSide side)
        {
            return side == QMC.CDT320.BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood;
        }

        private static List<DieMapEntry> BuildOutputReceiveOrder(DieMap sourceMap, PickupSubset pickup)
        {
            var ordered = PickupSequenceGenerator.Build(sourceMap, pickup);
            if (ordered != null && ordered.Count > 0)
                return ordered;

            if (sourceMap == null || sourceMap.Entries == null)
                return new List<DieMapEntry>();

            return sourceMap.Entries
                .Where(e => e != null && e.IsTarget && e.DieMapX >= 0 && e.DieMapY >= 0)
                .OrderBy(e => ResolveEntryMapY(e))
                .ThenBy(e => ResolveEntryMapX(e))
                .ToList();
        }

        private static List<DieMapEntry> BuildInputStagePickOrder(
            DieMap sourceMap,
            PickupSubset pickup,
            WaferMaterial wafer)
        {
            if (sourceMap == null || sourceMap.Entries == null)
                return new List<DieMapEntry>();

            if (wafer != null && wafer.HasInputStageRunReviewApproval)
            {
                List<DieMapEntry> approvedOrder;
                string approvalReason;
                if (TryBuildApprovedInputStagePickOrder(
                    sourceMap,
                    wafer,
                    out approvedOrder,
                    out approvalReason))
                {
                    return approvedOrder;
                }

                Log.Write("Main", "MATERIAL", "InputStageRunReviewOrder",
                    "승인된 Input PickUp 순서를 복원하지 못해 recipe fallback을 차단했습니다. wafer=" +
                    (wafer.WaferId ?? "") + ", reason=" + approvalReason + " - Blocked");
                return new List<DieMapEntry>();
            }

            return BuildOutputReceiveOrder(sourceMap, pickup);
        }

        private static bool TryBuildApprovedInputStagePickOrder(
            DieMap sourceMap,
            WaferMaterial wafer,
            out List<DieMapEntry> ordered,
            out string reason)
        {
            ordered = new List<DieMapEntry>();
            reason = string.Empty;
            if (sourceMap == null || sourceMap.Entries == null || wafer == null ||
                !wafer.HasInputStageRunReviewApproval)
            {
                reason = "InputStage Review 승인이 없습니다.";
                return false;
            }

            string currentRevision = ResolveInputStageRunReviewMappingRevision(wafer, sourceMap);
            if (string.IsNullOrWhiteSpace(currentRevision) ||
                string.IsNullOrWhiteSpace(wafer.InputStageRunReviewMappingRevision))
            {
                reason = "Review 승인 Mapping revision이 비어 있습니다. approved=" +
                         (wafer.InputStageRunReviewMappingRevision ?? "") + ", current=" + currentRevision;
                return false;
            }

            if (!string.Equals(
                wafer.InputStageRunReviewMappingRevision ?? string.Empty,
                currentRevision,
                StringComparison.OrdinalIgnoreCase))
            {
                reason = "Review 승인 Mapping revision이 현재 Map과 다릅니다. approved=" +
                         (wafer.InputStageRunReviewMappingRevision ?? "") + ", current=" + currentRevision;
                return false;
            }

            var entryById = new Dictionary<string, DieMapEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (DieMapEntry entry in sourceMap.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                {
                    reason = "현재 Die Map에 비어 있는 Die UID가 있습니다.";
                    return false;
                }
                if (entryById.ContainsKey(entry.DieUid))
                {
                    reason = "현재 Die Map에 중복 UID가 있습니다. die=" + entry.DieUid;
                    return false;
                }
                entryById.Add(entry.DieUid, entry);
            }

            List<string> orderedIds = wafer.InputStageRunReviewOrderedDieIds != null
                ? new List<string>(wafer.InputStageRunReviewOrderedDieIds)
                : new List<string>();
            if (orderedIds.Any(string.IsNullOrWhiteSpace))
            {
                reason = "Review 승인 PickUp 순서에 비어 있는 UID가 있습니다.";
                return false;
            }
            if (orderedIds.Count != orderedIds.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                reason = "Review 승인 PickUp 순서에 중복 UID가 있습니다.";
                return false;
            }

            var pickableIds = new HashSet<string>(
                sourceMap.Entries
                    .Where(entry => entry != null &&
                                    !string.IsNullOrWhiteSpace(entry.DieUid) &&
                                    entry.IsTarget &&
                                    entry.Result != DieResult.Good &&
                                    entry.Result != DieResult.NG)
                    .Select(entry => entry.DieUid),
                StringComparer.OrdinalIgnoreCase);

            // fail-closed: 승인 목록 밖에서 새 WAIT Target이 생기면 자동 진행을 차단한다.
            var orderedIdSet = new HashSet<string>(orderedIds, StringComparer.OrdinalIgnoreCase);
            foreach (string pickableId in pickableIds)
            {
                if (!orderedIdSet.Contains(pickableId))
                {
                    reason = "Review 승인 목록에 없는 새 WAIT Target이 있습니다. die=" + pickableId;
                    return false;
                }
            }

            // progress-aware: 승인 UID가 현재 Map에 존재해야 하며(누락은 fail-closed),
            // 이미 Good/NG/Picked 등으로 처리되어 WAIT에서 빠진 UID는 생산 진행으로 인정하고
            // 남은 Pick 대상만 원본 승인 순서를 유지한 채 복원한다. remaining 0건도 유효하다.
            int progressedCount = 0;
            foreach (string dieId in orderedIds)
            {
                DieMapEntry entry;
                if (!entryById.TryGetValue(dieId, out entry))
                {
                    reason = "Review 승인 UID를 현재 Map에서 찾을 수 없습니다. die=" + dieId;
                    return false;
                }
                if (pickableIds.Contains(dieId))
                    ordered.Add(entry);
                else
                    progressedCount++;
            }

            string startReason;
            if (!ValidateInputStageRunReviewStartSelection(
                orderedIds,
                wafer.InputStageRunReviewStartDieUid,
                wafer.InputStageRunReviewStartDieIndex,
                out startReason))
            {
                reason = startReason;
                return false;
            }

            reason = progressedCount > 0
                ? "Review 승인 순서를 생산 진행 기준으로 복원했습니다. processed=" + progressedCount +
                  ", remaining=" + ordered.Count
                : "Review 승인 PickUp 순서가 현재 Map과 일치합니다.";
            return true;
        }

        private static string ResolveInputStageRunReviewMappingRevision(WaferMaterial wafer, DieMap map)
        {
            if (wafer != null && !string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId))
                return wafer.DieMapFrameObjId;
            if (map != null && !string.IsNullOrWhiteSpace(map.FrameObjId))
                return map.FrameObjId;
            return wafer != null ? wafer.WaferId ?? string.Empty : string.Empty;
        }

        private static bool ValidateInputStageRunReviewStartSelection(
            IList<string> orderedIds,
            string startDieUid,
            int startDieIndex,
            out string reason)
        {
            reason = string.Empty;
            int orderedCount = orderedIds != null ? orderedIds.Count : 0;
            bool hasStartDie = !string.IsNullOrWhiteSpace(startDieUid);

            if (!hasStartDie)
            {
                if (startDieIndex != 0)
                {
                    reason = "Review 시작 Die UID가 없는데 시작 인덱스가 0이 아닙니다. index=" + startDieIndex;
                    return false;
                }

                return true;
            }

            if (orderedCount == 0)
            {
                reason = "Review 시작 Die가 있지만 승인 PickUp 순서가 비어 있습니다. start=" + startDieUid;
                return false;
            }

            if (startDieIndex <= 0 || startDieIndex > orderedCount)
            {
                reason = "Review 시작 Die 인덱스가 승인 PickUp 범위를 벗어났습니다. index=" +
                         startDieIndex + ", count=" + orderedCount;
                return false;
            }

            if (!string.Equals(orderedIds[0], startDieUid, StringComparison.OrdinalIgnoreCase))
            {
                reason = "Review 시작 Die와 승인 PickUp 첫 Die가 다릅니다. start=" +
                         startDieUid + ", first=" + (orderedIds[0] ?? "");
                return false;
            }

            return true;
        }

        private static int ResolveEntryMapX(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexX(entry);
        }

        private static int ResolveEntryMapY(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexY(entry);
        }

        private static List<OutputReceiveSlotMaterial> BuildOutputReceiveSlots(
            List<DieMapEntry> ordered,
            QMC.CDT320.BinSide side,
            double pitchX,
            double pitchY)
        {
            var slots = new List<OutputReceiveSlotMaterial>();
            if (ordered == null)
                return slots;

            int binCode = side == QMC.CDT320.BinSide.Ng ? 255 : 1;
            for (int i = 0; i < ordered.Count; i++)
            {
                DieMapEntry entry = ordered[i];
                if (entry == null)
                    continue;

                slots.Add(new OutputReceiveSlotMaterial
                {
                    OrderIndex = i,
                    SequenceNo = entry.SequenceNo,
                    DieMapX = ResolveEntryMapX(entry),
                    DieMapY = ResolveEntryMapY(entry),
                    OriginalMapX = DieMapGenerator.ResolveOriginalMapIndexX(entry),
                    OriginalMapY = DieMapGenerator.ResolveOriginalMapIndexY(entry),
                    IsTarget = true,
                    Result = DieResult.Unknown,
                    BinCode = binCode,
                    PosX = ResolveEntryPositionOrIndexFallback(entry.PosX, pitchX, ResolveEntryMapX(entry)),
                    PosY = ResolveEntryPositionOrIndexFallback(entry.PosY, pitchY, ResolveEntryMapY(entry)),
                    DieUid = ""
                });
            }

            return slots;
        }

        private static double ResolveEntryPositionOrIndexFallback(double position, double pitch, int index)
        {
            if (!double.IsNaN(position) && !double.IsInfinity(position))
                return position;

            return pitch * index;
        }

        private static void UpdateOutputReceiveSlot(
            WaferMaterial outputWafer,
            DieMaterial die,
            QMC.CDT320.BinSide side,
            OutputStageReceiveTarget receiveTarget)
        {
            if (outputWafer == null || die == null)
                return;

            if (outputWafer.OutputReceiveSlots == null)
                outputWafer.OutputReceiveSlots = new List<OutputReceiveSlotMaterial>();

            int index = receiveTarget != null
                ? receiveTarget.OrderIndex
                : ResolveNextOutputReceiveIndex(outputWafer);

            OutputReceiveSlotMaterial slot = outputWafer.OutputReceiveSlots
                .FirstOrDefault(s => s != null && s.OrderIndex == index);

            if (slot == null && receiveTarget != null)
            {
                slot = outputWafer.OutputReceiveSlots.FirstOrDefault(s =>
                    s != null &&
                    s.DieMapX == receiveTarget.DieMapX &&
                    s.DieMapY == receiveTarget.DieMapY);
            }

            if (slot == null)
            {
                slot = new OutputReceiveSlotMaterial
                {
                    OrderIndex = index,
                    SequenceNo = index,
                    DieMapX = receiveTarget != null ? receiveTarget.DieMapX : (die.Bin_IndexX >= 0 ? die.Bin_IndexX : index),
                    DieMapY = receiveTarget != null ? receiveTarget.DieMapY : (die.Bin_IndexY >= 0 ? die.Bin_IndexY : 0),
                    IsTarget = true,
                    BinCode = side == QMC.CDT320.BinSide.Ng ? 255 : 1,
                    PosX = receiveTarget != null ? receiveTarget.TargetX : 0.0,
                    PosY = receiveTarget != null ? receiveTarget.TargetY : 0.0
                };
                outputWafer.OutputReceiveSlots.Add(slot);
            }

            if (receiveTarget != null)
            {
                slot.OrderIndex = receiveTarget.OrderIndex;
                slot.DieMapX = receiveTarget.DieMapX;
                slot.DieMapY = receiveTarget.DieMapY;
                slot.PosX = receiveTarget.TargetX;
                slot.PosY = receiveTarget.TargetY;
            }

            slot.DieUid = die.DieId;
            slot.SourceDieUid = die.DieId;
            slot.PlacementUid = BuildOutputPlacementUid(outputWafer, slot.OrderIndex);
            slot.LegacyDieUid = "";
            slot.IdentityRecoveryNote = "";
            slot.Result = side == QMC.CDT320.BinSide.Ng ? DieResult.NG : DieResult.Good;
            slot.BinCode = side == QMC.CDT320.BinSide.Ng ? 255 : 1;
            die.Bin_IndexX = slot.DieMapX;
            die.Bin_IndexY = slot.DieMapY;
            die.Output_BinCode = slot.BinCode;
            if (die.BinOffset == null)
                die.BinOffset = new VisionOffset();
            die.BinOffset.X = slot.PosX;
            die.BinOffset.Y = slot.PosY;
            die.BinOffset.R = 0.0;
            die.BinOffset.IsValid = true;
        }

        private static int ResolveNextOutputReceiveIndex(WaferMaterial outputWafer)
        {
            if (outputWafer == null)
                return 0;

            if (outputWafer.OutputReceiveSlots != null && outputWafer.OutputReceiveSlots.Count > 0)
            {
                OutputReceiveSlotMaterial next = outputWafer.OutputReceiveSlots
                    .Where(s => IsOutputReceiveSlotPending(s))
                    .OrderBy(s => s.OrderIndex)
                    .FirstOrDefault();
                if (next != null)
                    return next.OrderIndex;

                int targetCount = outputWafer.OutputReceiveSlots.Count(s => s != null && s.IsTarget);
                if (targetCount > 0)
                    return targetCount;
            }

            return outputWafer.DieIds != null
                ? outputWafer.DieIds.Count(id => !string.IsNullOrWhiteSpace(id))
                : 0;
        }

        // 현재 기준: 수동 GOOD/NG 완료 슬롯은 실제 DieUid가 없어도 다음 place 대상에서 제외한다.
        private static bool IsOutputReceiveSlotPending(OutputReceiveSlotMaterial slot)
        {
            return slot != null &&
                   slot.IsTarget &&
                   slot.Result == DieResult.Unknown &&
                   string.IsNullOrWhiteSpace(slot.DieUid);
        }

        private static PickupSubset ResolveInputPickup(RecipeProject project)
        {
            if (project == null)
                return new PickupSubset();

            return project.InputPickup ?? project.Pickup ?? new PickupSubset();
        }

        private static PickupSubset ResolveOutputPickup(RecipeProject project)
        {
            if (project == null)
                return new PickupSubset();

            return project.OutputPickup ?? project.Pickup ?? new PickupSubset();
        }

        /// <summary>레시피에 저장된 원형 빈맵(GOOD/NG)을 로드합니다(BIN DIE MAP CREATE에서 저장한 맵).
        /// 경로는 RecipeMapPaths 공용 규칙을 사용하며, 없으면 null.</summary>
        private static DieMap LoadRecipeBinMap(QMC.CDT320.BinSide side)
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return null;

                RecipeMapKind kind = side == QMC.CDT320.BinSide.Ng ? RecipeMapKind.NgBin : RecipeMapKind.GoodBin;
                string path;
                string reason;
                // 현재 기준: 출력 Good/NG 빈맵도 Input과 같은 원본 wafer map index 기준을 사용한다.
                DieMap map = RecipeDieMapResolver.LoadCompatibleMap(project, kind, out path, out reason);
                if (map != null)
                    return DieMapGenerator.Normalize(map);

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "Recipe bin map load skipped: side=" + side + ", " + reason + " - Check");
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Recipe bin map load failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static InputStagePickTarget ReserveNextInputStagePickTarget(MaterialLocationKind pickerLocation, int pickerNo)
        {
            try
            {
                lock (_stateSync)
                {
                    if (pickerLocation != MaterialLocationKind.PickerFront &&
                        pickerLocation != MaterialLocationKind.PickerRear)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve failed: invalid picker location=" + pickerLocation + " - Failed");
                        return null;
                    }

                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    string readyReason;
                    if (!IsInputStageFinishCompleteNoLock(wafer, out readyReason))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve blocked: InputStage is not finished. reason=" + readyReason + " - Blocked");
                        return null;
                    }

                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null || map.Entries.Count == 0)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve skipped: input stage die map is empty. - Check");
                        return null;
                    }

                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = ResolveInputPickup(project);
                    List<DieMapEntry> ordered = BuildInputStagePickOrder(map, pickup, wafer);
                    if (ordered == null || ordered.Count == 0)
                        return null;

                    InputStagePickTarget existingReservedTarget = TryBuildExistingReservedInputStagePickTarget(
                        ordered,
                        wafer,
                        pickerLocation,
                        pickerNo);
                    if (existingReservedTarget != null)
                        return existingReservedTarget;

                    var skipSummary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = State.Dies.FirstOrDefault(d =>
                            d != null &&
                            string.Equals(d.DieId, entry.DieUid, StringComparison.OrdinalIgnoreCase));
                        if (die == null)
                            continue;

                        string candidateReason;
                        if (!CanUseInputPickCandidate(entry, die, out candidateReason))
                        {
                            CountPickTargetSkip(skipSummary, candidateReason);
                            continue;
                        }

                        if (IsDieReservedForPicker(die))
                            continue;

                        if (die.CurrentLocation != null &&
                            die.CurrentLocation.Kind != MaterialLocationKind.Unknown &&
                            die.CurrentLocation.Kind != MaterialLocationKind.InputStage)
                            continue;

                        die.ReservedPickerLocation = pickerLocation;
                        die.ReservedPickerNo = pickerNo;
                        die.UpdatedAt = DateTime.Now;

                        var target = new InputStagePickTarget
                        {
                            WaferId = wafer.WaferId,
                            DieId = die.DieId,
                            OrderIndex = i,
                            DieMapX = ResolveEntryMapX(entry),
                            DieMapY = ResolveEntryMapY(entry),
                            OffsetX = entry.PosX,
                            OffsetY = entry.PosY,
                            TargetX = entry.PosX,
                            TargetY = entry.PosY,
                            PickerNo = pickerNo,
                            PickerLocation = pickerLocation
                        };

                        NotifyAndSave("ReserveInputStagePickTarget");
                        return target;
                    }

                    LogInputPickTargetSkipSummary(skipSummary);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick target reserve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static InputStagePickTarget TryBuildExistingReservedInputStagePickTarget(
            List<DieMapEntry> ordered,
            WaferMaterial wafer,
            MaterialLocationKind pickerLocation,
            int pickerNo)
        {
            try
            {
                if (ordered == null || wafer == null)
                    return null;

                for (int i = 0; i < ordered.Count; i++)
                {
                    DieMapEntry entry = ordered[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                        continue;

                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, entry.DieUid, StringComparison.OrdinalIgnoreCase));
                    if (die == null)
                        continue;

                    bool reservedByRequestedPicker =
                        die.ReservedPickerLocation == pickerLocation &&
                        die.ReservedPickerNo == pickerNo;
                    if (!reservedByRequestedPicker)
                        continue;

                    string candidateReason;
                    if (!CanUseInputPickCandidate(entry, die, out candidateReason))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Existing input pick reservation ignored: " + candidateReason +
                            ", die=" + die.DieId +
                            ", pickerLocation=" + pickerLocation +
                            ", pickerNo=" + pickerNo + " - Check");
                        continue;
                    }

                    MaterialLocationKind kind = die.CurrentLocation != null
                        ? die.CurrentLocation.Kind
                        : MaterialLocationKind.Unknown;
                    if (kind != MaterialLocationKind.Unknown && kind != MaterialLocationKind.InputStage)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Existing input pick reservation ignored: die is not on InputStage. die=" + die.DieId +
                            ", location=" + kind +
                            ", pickerLocation=" + pickerLocation +
                            ", pickerNo=" + pickerNo + " - Check");
                        continue;
                    }

                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "Existing input pick reservation reused after restore/retry. die=" + die.DieId +
                        ", pickerLocation=" + pickerLocation +
                        ", pickerNo=" + pickerNo +
                        ", orderIndex=" + i +
                        ", grid=(" + ResolveEntryMapX(entry) + "," + ResolveEntryMapY(entry) + ") - Ok");

                    return new InputStagePickTarget
                    {
                        WaferId = wafer.WaferId,
                        DieId = die.DieId,
                        OrderIndex = i,
                        DieMapX = ResolveEntryMapX(entry),
                        DieMapY = ResolveEntryMapY(entry),
                        OffsetX = entry.PosX,
                        OffsetY = entry.PosY,
                        TargetX = entry.PosX,
                        TargetY = entry.PosY,
                        PickerNo = pickerNo,
                        PickerLocation = pickerLocation
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Existing input pick reservation reuse check failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static List<InputStagePickTargetCandidate> GetReadyInputStagePickTargetCandidates()
        {
            try
            {
                lock (_stateSync)
                {
                    var candidates = new List<InputStagePickTargetCandidate>();
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    string readyReason;
                    if (!IsInputStageFinishCompleteNoLock(wafer, out readyReason))
                        return candidates;

                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null || map.Entries.Count == 0)
                        return candidates;

                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = ResolveInputPickup(project);
                    List<DieMapEntry> ordered = BuildInputStagePickOrder(map, pickup, wafer);
                    if (ordered == null || ordered.Count == 0)
                        return candidates;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = State.Dies.FirstOrDefault(d =>
                            d != null &&
                            string.Equals(d.DieId, entry.DieUid, StringComparison.OrdinalIgnoreCase));
                        if (die == null)
                            continue;

                        string candidateReason;
                        if (!CanUseInputPickCandidate(entry, die, out candidateReason))
                            continue;

                        if (IsDieReservedForPicker(die))
                            continue;

                        MaterialLocationKind kind = die.CurrentLocation != null
                            ? die.CurrentLocation.Kind
                            : MaterialLocationKind.Unknown;
                        if (kind != MaterialLocationKind.Unknown && kind != MaterialLocationKind.InputStage)
                            continue;

                        candidates.Add(new InputStagePickTargetCandidate
                        {
                            WaferId = wafer.WaferId,
                            DieId = die.DieId,
                            OrderIndex = i,
                            DieMapX = ResolveEntryMapX(entry),
                            DieMapY = ResolveEntryMapY(entry),
                            TargetX = entry.PosX,
                            TargetY = entry.PosY,
                            DisplayText = "#" + (i + 1) +
                                          " [" + ResolveEntryMapX(entry) + "," + ResolveEntryMapY(entry) + "] " +
                                          die.DieId +
                                          " X=" + entry.PosX.ToString("0.###", CultureInfo.InvariantCulture) +
                                          " Y=" + entry.PosY.ToString("0.###", CultureInfo.InvariantCulture)
                        });
                    }

                    return candidates;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick target candidate query failed: " + ex.Message + " - Failed");
                return new List<InputStagePickTargetCandidate>();
            }
            finally
            {
            }
        }

        public static InputStagePickTarget ReserveInputStagePickTargetByDieId(
            MaterialLocationKind pickerLocation,
            int pickerNo,
            string dieId)
        {
            try
            {
                lock (_stateSync)
                {
                    if (pickerLocation != MaterialLocationKind.PickerFront &&
                        pickerLocation != MaterialLocationKind.PickerRear)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve by die failed: invalid picker location=" + pickerLocation + " - Failed");
                        return null;
                    }

                    if (string.IsNullOrWhiteSpace(dieId))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve by die failed: dieId is empty. - Failed");
                        return null;
                    }

                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    string readyReason;
                    if (!IsInputStageFinishCompleteNoLock(wafer, out readyReason))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve by die blocked: InputStage is not finished. reason=" + readyReason + " - Blocked");
                        return null;
                    }

                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null || map.Entries.Count == 0)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve by die skipped: input stage die map is empty. - Check");
                        return null;
                    }

                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = ResolveInputPickup(project);
                    List<DieMapEntry> ordered = BuildInputStagePickOrder(map, pickup, wafer);
                    if (ordered == null || ordered.Count == 0)
                        return null;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || !string.Equals(entry.DieUid, dieId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        DieMaterial die = State.Dies.FirstOrDefault(d =>
                            d != null &&
                            string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                        if (die == null)
                            return null;

                        string candidateReason;
                        if (!CanUseInputPickCandidate(entry, die, out candidateReason))
                        {
                            Log.Write("Main", "SYSTEM", "MaterialStateService",
                                "Input pick target reserve by die blocked: " + candidateReason + ", die=" + dieId + " - Blocked");
                            return null;
                        }

                        if (IsDieReservedForPicker(die))
                        {
                            Log.Write("Main", "SYSTEM", "MaterialStateService",
                                "Input pick target reserve by die blocked: die is already reserved. die=" + dieId +
                                ", reservedPickerNo=" + die.ReservedPickerNo + " - Blocked");
                            return null;
                        }

                        if (die.CurrentLocation != null &&
                            die.CurrentLocation.Kind != MaterialLocationKind.Unknown &&
                            die.CurrentLocation.Kind != MaterialLocationKind.InputStage)
                        {
                            Log.Write("Main", "SYSTEM", "MaterialStateService",
                                "Input pick target reserve by die blocked: die location is not InputStage. die=" + dieId +
                                ", location=" + die.CurrentLocation.Kind + " - Blocked");
                            return null;
                        }

                        die.ReservedPickerLocation = pickerLocation;
                        die.ReservedPickerNo = pickerNo;
                        die.UpdatedAt = DateTime.Now;

                        var target = new InputStagePickTarget
                        {
                            WaferId = wafer.WaferId,
                            DieId = die.DieId,
                            OrderIndex = i,
                            DieMapX = ResolveEntryMapX(entry),
                            DieMapY = ResolveEntryMapY(entry),
                            OffsetX = entry.PosX,
                            OffsetY = entry.PosY,
                            TargetX = entry.PosX,
                            TargetY = entry.PosY,
                            PickerNo = pickerNo,
                            PickerLocation = pickerLocation
                        };

                        NotifyAndSave("ReserveInputStagePickTargetByDieId");
                        return target;
                    }

                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "Input pick target reserve by die failed: die is not in input pick order. die=" + dieId + " - Failed");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick target reserve by die failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static InputStagePickTarget GetReservedInputStagePickTarget(
            MaterialLocationKind pickerLocation,
            int pickerNo,
            string dieId)
        {
            try
            {
                lock (_stateSync)
                {
                    if (string.IsNullOrWhiteSpace(dieId))
                        return null;

                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    string readyReason;
                    if (!IsInputStageFinishCompleteNoLock(wafer, out readyReason))
                        return null;

                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null || map.Entries.Count == 0)
                        return null;

                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = ResolveInputPickup(project);
                    List<DieMapEntry> ordered = BuildInputStagePickOrder(map, pickup, wafer);
                    if (ordered == null || ordered.Count == 0)
                        return null;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || !string.Equals(entry.DieUid, dieId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        DieMaterial die = State.Dies.FirstOrDefault(d =>
                            d != null &&
                            string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                        if (die == null)
                            return null;

                        bool reservedByPicker =
                            die.ReservedPickerLocation == pickerLocation &&
                            die.ReservedPickerNo == pickerNo;
                        if (!reservedByPicker)
                            return null;

                        return new InputStagePickTarget
                        {
                            WaferId = wafer.WaferId,
                            DieId = die.DieId,
                            OrderIndex = i,
                            DieMapX = ResolveEntryMapX(entry),
                            DieMapY = ResolveEntryMapY(entry),
                            OffsetX = entry.PosX,
                            OffsetY = entry.PosY,
                            TargetX = entry.PosX,
                            TargetY = entry.PosY,
                            PickerNo = pickerNo,
                            PickerLocation = pickerLocation
                        };
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Reserved input pick target query failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static bool TryGetLatestInputPickVisionOffset(string dieId, out VisionOffset offset)
        {
            offset = null;

            try
            {
                lock (_stateSync)
                {
                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null || die.Inspections == null)
                        return false;

                    DieInspectionRecord record = die.Inspections
                        .Where(x => x != null &&
                                    string.Equals(x.InspectionType, "InputPickVision", StringComparison.OrdinalIgnoreCase) &&
                                    x.Offset != null &&
                                    x.Offset.IsValid)
                        .OrderByDescending(x => x.UpdatedAt)
                        .FirstOrDefault();
                    if (record == null)
                        return false;

                    offset = new VisionOffset
                    {
                        X = record.Offset.X,
                        Y = record.Offset.Y,
                        R = record.Offset.R,
                        IsValid = record.Offset.IsValid
                    };
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick vision offset query failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool IsInputStageFinishComplete(out string reason)
        {
            reason = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    return IsInputStageFinishCompleteNoLock(
                        GetWaferAtLocation(MaterialLocationKind.InputStage),
                        out reason);
                }
            }
            catch (Exception ex)
            {
                reason = "InputStage finish complete check failed: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool IsInputStageRunReviewApprovalUsable(
            WaferMaterial wafer,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    if (wafer == null)
                    {
                        reason = "InputStage Review 승인 대상 Wafer Material이 없습니다.";
                        return false;
                    }

                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (map == null || map.Entries == null || map.Entries.Count == 0)
                    {
                        reason = "InputStage Review 승인 검증용 Die Map이 비어 있습니다. waferId=" +
                                 (wafer.WaferId ?? "");
                        return false;
                    }

                    List<DieMapEntry> ordered;
                    return TryBuildApprovedInputStagePickOrder(map, wafer, out ordered, out reason);
                }
            }
            catch (Exception ex)
            {
                reason = "InputStage Review 승인 유효성 검사 실패: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool SetInputStageRunReviewApproval(
            WaferMaterial wafer,
            bool approved,
            int startDieIndex,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    if (wafer == null)
                    {
                        reason = "InputStage 리뷰 승인 대상 Wafer Material이 없습니다.";
                        return false;
                    }

                    if (approved)
                    {
                        if (!wafer.HasInputStageAlignResult ||
                            !wafer.HasInputStageThetaAlignResult ||
                            !wafer.HasInputStageDieMappingResult ||
                            wafer.InputStageDieMappingInvalidatedByAlignChange)
                        {
                            reason = "Align/T Align/Die Mapping이 모두 유효한 상태에서만 리뷰를 승인할 수 있습니다. waferId=" +
                                     (wafer.WaferId ?? "");
                            return false;
                        }

                        string resultModeReason;
                        if (!IsStoredInputStageResultModeUsableNoLock(wafer, true, out resultModeReason))
                        {
                            reason = "저장된 Align/Die Mapping 결과를 사용할 수 없어 리뷰를 승인할 수 없습니다. " +
                                     resultModeReason;
                            return false;
                        }

                        DieMap map = BuildDieMapFromWafer(wafer);
                        if (wafer.DieIds == null || wafer.DieIds.Count == 0 ||
                            map == null || map.Entries == null || map.Entries.Count == 0)
                        {
                            reason = "InputStage Die 데이터 또는 Die Map이 비어 있어 리뷰를 승인할 수 없습니다. waferId=" +
                                     (wafer.WaferId ?? "");
                            return false;
                        }
                    }

                    wafer.HasInputStageRunReviewApproval = approved;
                    wafer.InputStageRunReviewStartDieIndex = approved ? Math.Max(0, startDieIndex) : 0;
                    if (!approved)
                    {
                        wafer.InputStageRunReviewStartDieUid = "";
                        wafer.InputStageRunReviewOrderedDieIds = new List<string>();
                        wafer.InputStageRunReviewMappingRevision = "";
                    }
                    wafer.UpdatedAt = DateTime.Now;
                    reason = approved
                        ? "InputStage 리뷰 승인이 저장되었습니다. waferId=" + (wafer.WaferId ?? "") +
                          ", startDieIndex=" + wafer.InputStageRunReviewStartDieIndex
                        : "InputStage 리뷰 승인이 해제되었습니다. waferId=" + (wafer.WaferId ?? "");
                    NotifyAndSave(approved ? "InputStageRunReviewApproved" : "InputStageRunReviewReset");
                    return true;
                }
            }
            catch (Exception ex)
            {
                reason = "InputStage 리뷰 승인 저장 실패: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool CommitInputStageRunReview(
            WaferMaterial wafer,
            UserConfirmResult review,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                if (wafer == null || review == null)
                {
                    reason = "InputStage 리뷰 확정 데이터가 없습니다.";
                    return false;
                }

                lock (_stateSync)
                {
                    WaferMaterial current = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (current == null ||
                        !string.Equals(current.WaferId ?? "", wafer.WaferId ?? "", StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(review.WaferId) &&
                         !string.Equals(current.WaferId ?? "", review.WaferId, StringComparison.OrdinalIgnoreCase)))
                    {
                        reason = "Review 대상과 현재 InputStage Wafer가 일치하지 않습니다. review=" +
                                 (review.WaferId ?? "") + ", current=" +
                                 (current != null ? current.WaferId : "-");
                        return false;
                    }

                    DieMap currentMap = BuildDieMapFromWafer(current);
                    if (currentMap == null || currentMap.Entries == null || currentMap.Entries.Count == 0)
                    {
                        reason = "현재 InputStage Die Map을 확인할 수 없습니다.";
                        return false;
                    }

                    string currentMappingRevision = ResolveInputStageRunReviewMappingRevision(current, currentMap);
                    if (string.IsNullOrWhiteSpace(currentMappingRevision))
                    {
                        reason = "현재 Die Mapping revision을 확인할 수 없습니다.";
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(review.MappingRevision) &&
                        !string.Equals(currentMappingRevision, review.MappingRevision, StringComparison.OrdinalIgnoreCase))
                    {
                        reason = "Review 중 Die Mapping revision이 변경되었습니다. review=" +
                                 review.MappingRevision + ", current=" + currentMappingRevision;
                        return false;
                    }

                    if (!current.HasInputStageAlignResult ||
                        !current.HasInputStageThetaAlignResult ||
                        !current.HasInputStageDieMappingResult ||
                        current.InputStageDieMappingInvalidatedByAlignChange)
                    {
                        reason = "Align/T Align/Die Mapping이 모두 유효한 상태에서만 Review를 확정할 수 있습니다.";
                        return false;
                    }

                    string resultModeReason;
                    if (!IsStoredInputStageResultModeUsableNoLock(current, true, out resultModeReason))
                    {
                        reason = "저장된 Align/Die Mapping 결과를 사용할 수 없습니다. " + resultModeReason;
                        return false;
                    }

                    List<DieMaterial> resolvedWaferDies = ResolveWaferDies(current);
                    var waferDies = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
                    foreach (DieMaterial die in resolvedWaferDies)
                    {
                        if (die == null || string.IsNullOrWhiteSpace(die.DieId))
                        {
                            reason = "InputStage Wafer에 UID가 비어 있는 Die Material이 있습니다.";
                            return false;
                        }

                        if (waferDies.ContainsKey(die.DieId))
                        {
                            reason = "InputStage Wafer에 중복 Die UID가 있습니다. die=" + die.DieId;
                            return false;
                        }

                        waferDies.Add(die.DieId, die);
                    }
                    if (waferDies.Count == 0)
                    {
                        reason = "InputStage Wafer의 Die Material이 비어 있습니다.";
                        return false;
                    }

                    var draftById = new Dictionary<string, InputStageRunReviewDieState>(StringComparer.OrdinalIgnoreCase);
                    foreach (InputStageRunReviewDieState draft in review.DieStates ?? new List<InputStageRunReviewDieState>())
                    {
                        if (draft == null || string.IsNullOrWhiteSpace(draft.DieId) || draftById.ContainsKey(draft.DieId))
                        {
                            reason = "Review Die 상태 데이터에 비어 있거나 중복된 UID가 있습니다.";
                            return false;
                        }

                        DieMaterial die;
                        if (!waferDies.TryGetValue(draft.DieId, out die))
                        {
                            reason = "Review Die가 현재 Wafer에 없습니다. die=" + draft.DieId;
                            return false;
                        }

                        MaterialLocationKind location = die.CurrentLocation != null
                            ? die.CurrentLocation.Kind
                            : MaterialLocationKind.Unknown;
                        if (location != MaterialLocationKind.Unknown && location != MaterialLocationKind.InputStage)
                        {
                            reason = "Review 중 이미 InputStage를 벗어난 Die가 있습니다. die=" +
                                     draft.DieId + ", location=" + location;
                            return false;
                        }

                        if (IsDieReservedForPicker(die) || HasInputPickCompletedHistory(die))
                        {
                            reason = "Review 중 이미 예약 또는 Pick 완료된 Die가 있습니다. die=" + draft.DieId;
                            return false;
                        }

                        if (draft.HasPosition &&
                            (double.IsNaN(draft.PositionX) || double.IsInfinity(draft.PositionX) ||
                             double.IsNaN(draft.PositionY) || double.IsInfinity(draft.PositionY)))
                        {
                            reason = "Review Die 좌표가 유효하지 않습니다. die=" + draft.DieId;
                            return false;
                        }

                        draftById.Add(draft.DieId, draft);
                    }

                    if (draftById.Count != waferDies.Count)
                    {
                        reason = "Review Die 상태 수와 현재 Wafer Die 수가 일치하지 않습니다. review=" +
                                 draftById.Count + ", current=" + waferDies.Count;
                        return false;
                    }

                    List<string> orderedIds = review.OrderedDieIds != null
                        ? new List<string>(review.OrderedDieIds)
                        : new List<string>();
                    if (orderedIds.Any(string.IsNullOrWhiteSpace))
                    {
                        reason = "Review PickUp 순서에 비어 있는 Die UID가 있습니다.";
                        return false;
                    }
                    if (orderedIds.Count != orderedIds.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                    {
                        reason = "Review PickUp 순서에 중복 Die UID가 있습니다.";
                        return false;
                    }

                    var expectedPickableIds = new HashSet<string>(
                        draftById.Values
                            .Where(d => d.IsTarget && d.Result != DieResult.Good && d.Result != DieResult.NG)
                            .Select(d => d.DieId),
                        StringComparer.OrdinalIgnoreCase);
                    if (orderedIds.Count != expectedPickableIds.Count ||
                        orderedIds.Any(id => !expectedPickableIds.Contains(id)))
                    {
                        reason = "Review PickUp 순서와 WAIT Target Die 집합이 일치하지 않습니다. ordered=" +
                                 orderedIds.Count + ", pickable=" + expectedPickableIds.Count;
                        return false;
                    }

                    string startReason;
                    if (!ValidateInputStageRunReviewStartSelection(
                        orderedIds,
                        review.StartDieUid,
                        review.StartDieIndex,
                        out startReason))
                    {
                        reason = startReason;
                        return false;
                    }

                    foreach (InputStageRunReviewDieState draft in draftById.Values)
                    {
                        DieMaterial die = waferDies[draft.DieId];
                        DieResult committedResult = draft.IsTarget ? draft.Result : DieResult.Unknown;
                        ApplyManualDieStateNoLock(
                            die,
                            draft.IsTarget,
                            committedResult,
                            draft.BinCode,
                            draft.Result == DieResult.NG ? "ManualInputMapEdit" : "");
                        if (draft.HasPosition)
                        {
                            if (die.WaferOffset == null)
                                die.WaferOffset = new VisionOffset();
                            die.WaferOffset.X = draft.PositionX;
                            die.WaferOffset.Y = draft.PositionY;
                            die.WaferOffset.R = 0.0;
                            die.WaferOffset.IsValid = true;
                        }
                        die.InputSequenceNo = 0;
                        SyncActiveInputMapEntryNoLock(
                            die.DieId,
                            draft.IsTarget,
                            committedResult,
                            ResolveManualBinCode(committedResult, draft.BinCode));
                        SyncInputPickVisionReviewInspectionNoLock(die, committedResult);
                    }

                    for (int i = 0; i < orderedIds.Count; i++)
                        waferDies[orderedIds[i]].InputSequenceNo = i + 1;

                    DieMap activeMap = LotStorage.ActiveInputDieMap;
                    if (activeMap != null && activeMap.Entries != null)
                    {
                        foreach (DieMapEntry entry in activeMap.Entries)
                        {
                            if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                                continue;

                            DieMaterial die;
                            if (waferDies.TryGetValue(entry.DieUid, out die))
                            {
                                entry.SequenceNo = die.InputSequenceNo;
                                if (die.WaferOffset != null && die.WaferOffset.IsValid)
                                {
                                    entry.PosX = die.WaferOffset.X;
                                    entry.PosY = die.WaferOffset.Y;
                                }
                            }
                        }
                    }

                    if (review.HasMapOrigin &&
                        !double.IsNaN(review.MapOriginX) && !double.IsInfinity(review.MapOriginX) &&
                        !double.IsNaN(review.MapOriginY) && !double.IsInfinity(review.MapOriginY))
                    {
                        current.HasInputStageDieMappingOrigin = true;
                        current.InputStageDieMappingOriginX = review.MapOriginX;
                        current.InputStageDieMappingOriginY = review.MapOriginY;
                        current.InputStageDieMappingOffsetX = review.MapOriginX - current.InputStageAlignOriginX;
                        current.InputStageDieMappingOffsetY = review.MapOriginY - current.InputStageAlignOriginY;
                        if (activeMap != null)
                        {
                            activeMap.OriginX = review.MapOriginX;
                            activeMap.OriginY = review.MapOriginY;
                        }
                    }

                    current.HasInputStageRunReviewApproval = true;
                    current.InputStageRunReviewStartDieIndex = review.StartDieIndex;
                    current.InputStageRunReviewStartDieUid = review.StartDieUid ?? "";
                    current.InputStageRunReviewOrderedDieIds = new List<string>(orderedIds);
                    current.InputStageRunReviewMappingRevision = currentMappingRevision;
                    current.UpdatedAt = DateTime.Now;

                    NotifyAndSave("InputStageRunReviewCommit");
                    reason = "InputStage Review 상태와 PickUp 순서를 확정했습니다. wafer=" +
                             (current.WaferId ?? "") + ", target=" + orderedIds.Count +
                             ", startDie=" + (current.InputStageRunReviewStartDieUid ?? "");
                    return true;
                }
            }
            catch (Exception ex)
            {
                reason = "InputStage Review 일괄 확정 실패: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool HasReadyInputStagePickTarget()
        {
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    string readyReason;
                    if (!IsInputStageFinishCompleteNoLock(wafer, out readyReason))
                        return false;

                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null || map.Entries.Count == 0)
                        return false;

                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = ResolveInputPickup(project);
                    List<DieMapEntry> ordered = BuildInputStagePickOrder(map, pickup, wafer);
                    if (ordered == null || ordered.Count == 0)
                        return false;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = State.Dies.FirstOrDefault(d =>
                            d != null &&
                            string.Equals(d.DieId, entry.DieUid, StringComparison.OrdinalIgnoreCase));
                        if (die == null)
                            continue;

                        string candidateReason;
                        if (!CanUseInputPickCandidate(entry, die, out candidateReason))
                            continue;

                        bool pickableLocation =
                            die.CurrentLocation == null ||
                            die.CurrentLocation.Kind == MaterialLocationKind.Unknown ||
                            die.CurrentLocation.Kind == MaterialLocationKind.InputStage;

                        if (IsDieReservedForPicker(die))
                        {
                            if (pickableLocation)
                                return true;

                            continue;
                        }

                        if (pickableLocation)
                            return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick target ready check failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 해당 Picker Side가 "실제로 처리 가능한" Input pick 대상이 있는지 판정한다.<br/>
        /// HasReadyInputStagePickTarget()은 상대 픽커에 예약된 die도 true를 반환해
        /// 예약 획득 경로(ReserveNextInputStagePickTarget: 예약 die skip)와 비대칭이었고,
        /// 이 비대칭이 빈 PickerProcess 무한 재진입(busy loop)의 원인이었다.<br/>
        /// 이 판정은 예약 경로와 동일 기준을 사용한다: 미예약 die 또는 "이 side에 예약된" die만 대상으로 본다.<br/>
        /// 읽기 전용 — 예약/상태를 변경하지 않는다.
        /// </summary>
        public static bool HasActionableInputStagePickTarget(MaterialLocationKind pickerLocation)
        {
            try
            {
                lock (_stateSync)
                {
                    if (pickerLocation != MaterialLocationKind.PickerFront &&
                        pickerLocation != MaterialLocationKind.PickerRear)
                        return false;

                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    string readyReason;
                    if (!IsInputStageFinishCompleteNoLock(wafer, out readyReason))
                        return false;

                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null || map.Entries.Count == 0)
                        return false;

                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = ResolveInputPickup(project);
                    List<DieMapEntry> ordered = BuildInputStagePickOrder(map, pickup, wafer);
                    if (ordered == null || ordered.Count == 0)
                        return false;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = State.Dies.FirstOrDefault(d =>
                            d != null &&
                            string.Equals(d.DieId, entry.DieUid, StringComparison.OrdinalIgnoreCase));
                        if (die == null)
                            continue;

                        string candidateReason;
                        if (!CanUseInputPickCandidate(entry, die, out candidateReason))
                            continue;

                        bool pickableLocation =
                            die.CurrentLocation == null ||
                            die.CurrentLocation.Kind == MaterialLocationKind.Unknown ||
                            die.CurrentLocation.Kind == MaterialLocationKind.InputStage;
                        if (!pickableLocation)
                            continue;

                        if (IsDieReservedForPicker(die))
                        {
                            // 상대 side 예약 die는 이 side가 처리할 수 없다(예약 경로와 동일 기준).
                            if (die.ReservedPickerLocation == pickerLocation)
                                return true;

                            continue;
                        }

                        // 미예약 + pickable = 이 side가 즉시 예약/처리 가능한 대상.
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Actionable input pick target check failed. pickerLocation=" + pickerLocation +
                    ", error=" + ex.Message + " - Failed");
                // 판정 실패 시 안전측(기존 전역 판정)으로 폴백해 정상 작업이 멈추지 않게 한다.
                return HasReadyInputStagePickTarget();
            }
            finally
            {
            }
        }

        public static bool HasInputStagePickReservationForPickerLocation(MaterialLocationKind pickerLocation)
        {
            try
            {
                lock (_stateSync)
                {
                    if (pickerLocation != MaterialLocationKind.PickerFront &&
                        pickerLocation != MaterialLocationKind.PickerRear)
                        return false;

                    for (int i = 0; i < State.Dies.Count; i++)
                    {
                        DieMaterial die = State.Dies[i];
                        if (die == null || die.ReservedPickerLocation != pickerLocation)
                            continue;

                        if (die.ReservedPickerNo <= 0)
                            continue;

                        MaterialLocationKind kind = die.CurrentLocation != null
                            ? die.CurrentLocation.Kind
                            : MaterialLocationKind.Unknown;
                        if (kind == MaterialLocationKind.Unknown || kind == MaterialLocationKind.InputStage)
                            return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick reservation check failed. pickerLocation=" + pickerLocation +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void CountPickTargetSkip(Dictionary<string, int> summary, string reason)
        {
            try
            {
                if (summary == null)
                    return;

                string key = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason;
                int count;
                summary.TryGetValue(key, out count);
                summary[key] = count + 1;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void LogInputPickTargetSkipSummary(Dictionary<string, int> summary)
        {
            try
            {
                if (summary == null || summary.Count == 0)
                    return;

                string message = string.Join("; ", summary.Select(pair => pair.Key + "=" + pair.Value));
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick target reserve skipped. usable die was not found. summary=" + message + " - Check");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick target skip summary log failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool IsStoredInputStageResultModeUsableNoLock(
            WaferMaterial wafer,
            bool requireMapping,
            out string reason)
        {
            reason = string.Empty;
            if (wafer == null)
            {
                reason = "InputStage wafer material is not available.";
                return false;
            }

            string alignMode = wafer.InputStageAlignResultMode ?? "";
            if (!InputStageResultMode.IsKnown(alignMode))
            {
                reason = "Unknown InputStage align result mode. mode=" + alignMode;
                return false;
            }

            bool hybridAlign = InputStageResultMode.IsHybrid(alignMode);
            if (hybridAlign)
            {
                if (!QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive())
                {
                    InputStageHybridResultSession.Clear();
                    reason = "Hybrid InputStage align result cannot be used outside HybridRealVisionSimMotion mode.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(wafer.InputStageAlignResultRunId) ||
                    !InputStageHybridResultSession.IsCurrentAlign(
                        wafer.WaferId,
                        wafer.InputStageAlignResultRunId))
                {
                    reason = "Hybrid InputStage align result is not from the current application session. Re-align is required.";
                    return false;
                }
            }

            if (!requireMapping)
                return true;

            string mappingMode = wafer.InputStageDieMappingResultMode ?? "";
            if (!InputStageResultMode.IsKnown(mappingMode))
            {
                reason = "Unknown InputStage die mapping result mode. mode=" + mappingMode;
                return false;
            }

            bool hybridMapping = InputStageResultMode.IsHybrid(mappingMode);
            if (hybridAlign != hybridMapping)
            {
                reason = "InputStage align/mapping result mode mismatch. alignMode=" + alignMode +
                         ", mappingMode=" + mappingMode;
                return false;
            }

            if (hybridMapping)
            {
                if (!string.Equals(
                        wafer.InputStageDieMappingAlignRunId,
                        wafer.InputStageAlignResultRunId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !InputStageHybridResultSession.IsCurrentMapping(
                        wafer.WaferId,
                        wafer.InputStageAlignResultRunId))
                {
                    reason = "Hybrid InputStage die mapping is not tied to the current-session align result. Re-align and re-map are required.";
                    return false;
                }
            }

            return true;
        }

        public static bool IsStoredInputStageResultModeUsable(
            WaferMaterial wafer,
            bool requireMapping,
            out string reason)
        {
            try
            {
                lock (_stateSync)
                {
                    return IsStoredInputStageResultModeUsableNoLock(wafer, requireMapping, out reason);
                }
            }
            catch (Exception ex)
            {
                reason = "InputStage stored result mode check failed: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool IsInputStageFinishCompleteNoLock(WaferMaterial wafer, out string reason)
        {
            reason = string.Empty;

            if (wafer == null)
            {
                reason = "InputStage wafer material is not available.";
                return false;
            }

            if (!wafer.HasInputStageAlignResult)
            {
                reason = "InputStage align is not complete. waferId=" + wafer.WaferId;
                return false;
            }

            if (!IsInputStageThetaAlignCompleteNoLock(wafer, out reason))
                return false;

            if (wafer.InputStageDieMappingInvalidatedByAlignChange)
            {
                reason = "InputStage die mapping was invalidated by align/theta change. waferId=" + wafer.WaferId;
                return false;
            }

            if (wafer.HasInputStageDieMappingThetaSnapshot &&
                Math.Abs(wafer.InputStageDieMappingCorrectedT - wafer.InputStageAlignCorrectedT) >
                    InputStageThetaMappingSnapshotToleranceDeg)
            {
                reason = "InputStage theta changed after die mapping. waferId=" + wafer.WaferId +
                         ", mappedT=" + wafer.InputStageDieMappingCorrectedT.ToString("F6") +
                         ", currentT=" + wafer.InputStageAlignCorrectedT.ToString("F6");
                return false;
            }

            if (!wafer.HasInputStageDieMappingResult)
            {
                reason = "InputStage die mapping is not complete. waferId=" + wafer.WaferId;
                return false;
            }

            if (!IsStoredInputStageResultModeUsableNoLock(wafer, true, out reason))
                return false;

            if (wafer.DieIds == null || wafer.DieIds.Count == 0)
            {
                reason = "InputStage die data is empty. waferId=" + wafer.WaferId;
                return false;
            }

            DieMap map = BuildDieMapFromWafer(wafer);
            if (map == null || map.Entries == null || map.Entries.Count == 0)
            {
                reason = "InputStage die map is empty. waferId=" + wafer.WaferId;
                return false;
            }

            if (!wafer.HasInputStageRunReviewApproval)
            {
                reason = "InputStage Align/Die Mapping 사용자 확인이 완료되지 않았습니다. waferId=" + wafer.WaferId;
                return false;
            }

            List<DieMapEntry> approvedOrder;
            string approvalReason;
            if (!TryBuildApprovedInputStagePickOrder(map, wafer, out approvedOrder, out approvalReason))
            {
                reason = "InputStage Review 승인 PickUp 순서가 유효하지 않습니다. waferId=" +
                         wafer.WaferId + ", reason=" + approvalReason;
                return false;
            }

            reason = "InputStage finish complete. waferId=" + wafer.WaferId +
                     ", dieCount=" + wafer.DieIds.Count +
                     ", pickableCount=" + approvedOrder.Count;
            return true;
        }

        public static bool IsInputStageThetaAlignComplete(WaferMaterial wafer, out string reason)
        {
            try
            {
                lock (_stateSync)
                {
                    return IsInputStageThetaAlignCompleteNoLock(wafer, out reason);
                }
            }
            catch (Exception ex)
            {
                reason = "InputStage theta align complete check failed: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static bool IsInputStageThetaAlignCompleteNoLock(WaferMaterial wafer, out string reason)
        {
            reason = string.Empty;

            if (wafer == null)
            {
                reason = "InputStage wafer material is not available.";
                return false;
            }

            if (!wafer.HasInputStageThetaAlignResult)
            {
                reason = "InputStage theta align is not complete. waferId=" + wafer.WaferId;
                return false;
            }

            if (!IsStoredInputStageResultModeUsableNoLock(wafer, false, out reason))
                return false;

            if (double.IsNaN(wafer.InputStageAlignReferenceT) ||
                double.IsInfinity(wafer.InputStageAlignReferenceT) ||
                double.IsNaN(wafer.InputStageAlignCorrectedT) ||
                double.IsInfinity(wafer.InputStageAlignCorrectedT) ||
                double.IsNaN(wafer.InputStageAlignOffsetT) ||
                double.IsInfinity(wafer.InputStageAlignOffsetT))
            {
                reason = "InputStage theta align value is invalid. waferId=" + wafer.WaferId;
                return false;
            }

            reason = "InputStage theta align complete. waferId=" + wafer.WaferId +
                     ", referenceT=" + wafer.InputStageAlignReferenceT.ToString("F6") +
                     ", correctedT=" + wafer.InputStageAlignCorrectedT.ToString("F6") +
                     ", offsetT=" + wafer.InputStageAlignOffsetT.ToString("F6");
            return true;
        }

        public static bool IsInputStagePickComplete()
        {
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (wafer == null || wafer.DieIds == null || wafer.DieIds.Count == 0)
                        return false;

                    for (int i = 0; i < wafer.DieIds.Count; i++)
                    {
                        string dieId = wafer.DieIds[i];
                        if (string.IsNullOrWhiteSpace(dieId))
                            continue;

                        DieMaterial die = State.Dies.FirstOrDefault(d =>
                            d != null &&
                            string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                        if (die == null || !die.IsInputTarget ||
                            die.Result == DieResult.NG ||
                            (die.Result == DieResult.Good && die.InputSequenceNo <= 0))
                            continue;

                        MaterialLocationKind kind = die.CurrentLocation != null
                            ? die.CurrentLocation.Kind
                            : MaterialLocationKind.Unknown;

                        if (kind == MaterialLocationKind.Unknown ||
                            kind == MaterialLocationKind.InputStage ||
                            kind == MaterialLocationKind.PickerFront ||
                            kind == MaterialLocationKind.PickerRear)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input stage pick complete check failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static void ReleaseInputStagePickReservation(string dieId, MaterialLocationKind pickerLocation, int pickerNo)
        {
            try
            {
                lock (_stateSync)
                {
                    if (string.IsNullOrWhiteSpace(dieId))
                        return;

                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null || die.CurrentLocation == null)
                        return;

                    bool reservedByPicker =
                        die.ReservedPickerLocation == pickerLocation &&
                        die.ReservedPickerNo == pickerNo;
                    bool legacyReservedLocation =
                        die.CurrentLocation.Kind == pickerLocation &&
                        die.CurrentLocation.PickerNo == pickerNo;

                    if (!reservedByPicker && !legacyReservedLocation)
                        return;

                    if (legacyReservedLocation)
                        die.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };

                    die.ReservedPickerLocation = MaterialLocationKind.Unknown;
                    die.ReservedPickerNo = -1;
                    die.UpdatedAt = DateTime.Now;
                    NotifyAndSave("ReleaseInputStagePickReservation");
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick target reservation release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>
        /// Input die vision 실패/과대 보정 Die를 "Wait"(다음 라운드 재촬영 대기) 상태로 되돌린다.
        /// [사용자 확정 2026-07-29] 기존 SKIP은 ApplyManualDieState(isInputTarget:false)로
        ///   die.IsInputTarget을 내려 CanUseInputPickCandidate에서 영구 제외됐다(=다이를 버림).
        ///   Wait는 그 다이를 픽업 후보로 살려 두고 다음 라운드에 다시 촬영·픽업하게 한다.
        ///
        /// 따라서 이 메서드는 IsInputTarget / Result를 변경하지 않는다 — 예약 해제와
        ///   InputPickVision 검사기록 제거만 수행한다(둘 다 재예약·재촬영의 전제).
        /// 픽커 위치에 남아 있는 Die는 저장 상태와 물리 상태 불일치이므로 fail-closed로 거부한다.
        /// 세 호출부(prepare / 픽업 RESULT 회수 / 픽업 직접 경로)가 이 단일 구현만 호출한다.
        /// </summary>
        public static bool ReturnInputDieToWait(
            string dieId,
            MaterialLocationKind pickerLocation,
            int pickerNo,
            string reason,
            out string message)
        {
            message = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(dieId))
                {
                    message = "Die ID가 비어 있습니다.";
                    return false;
                }

                // 예약 해제가 먼저다 — 이 호출이 legacy 예약(CurrentLocation=Picker*)을
                // InputStage로 정규화해 주므로, 아래 위치 검증이 정상 케이스를 오탐하지 않는다.
                ReleaseInputStagePickReservation(dieId, pickerLocation, pickerNo);
                RemoveInspection(dieId, "InputPickVision");

                bool isInputTarget;
                DieResult result;
                lock (_stateSync)
                {
                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null)
                    {
                        message = "Die 정보를 찾을 수 없습니다. dieId=" + dieId;
                        return false;
                    }

                    MaterialLocationKind locationKind = die.CurrentLocation != null
                        ? die.CurrentLocation.Kind
                        : MaterialLocationKind.Unknown;
                    if (locationKind == MaterialLocationKind.PickerFront ||
                        locationKind == MaterialLocationKind.PickerRear)
                    {
                        message = "Die가 아직 Picker 위치로 기록되어 있어 Wait로 되돌릴 수 없습니다. " +
                                  "dieId=" + dieId + ", location=" + locationKind +
                                  ", pickerNo=" + die.CurrentLocation.PickerNo;
                        return false;
                    }

                    // IsInputTarget / Result는 의도적으로 변경하지 않는다(Wait의 정의).
                    isInputTarget = die.IsInputTarget;
                    result = die.Result;
                    die.UpdatedAt = DateTime.Now;
                }

                NotifyAndSave("ReturnInputDieToWait:" + (reason ?? string.Empty) + ":" + dieId);

                // 후보 조건이 이미 깨져 있으면 재픽업이 되지 않으므로 그 사실을 남긴다(무음 방지).
                bool pickableAgain = isInputTarget &&
                                     result != DieResult.Good &&
                                     result != DieResult.NG;
                Log.Write("Main", "MATERIAL", "ReturnInputDieToWait",
                    "Input die를 Wait 상태로 되돌렸습니다. dieId=" + dieId +
                    ", reason=" + (reason ?? string.Empty) +
                    ", isInputTarget=" + isInputTarget +
                    ", result=" + result +
                    ", pickableAgain=" + pickableAgain +
                    (pickableAgain ? " - Ok" : " - Check"));

                message = "Die를 Wait 상태로 되돌렸습니다. dieId=" + dieId;
                return true;
            }
            catch (Exception ex)
            {
                message = "Die Wait 복귀 실패: " + ex.Message;
                Log.Write("Main", "MATERIAL", "ReturnInputDieToWait",
                    "Input die Wait 복귀 실패. dieId=" + dieId +
                    ", reason=" + (reason ?? string.Empty) +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static int ReleaseInputStagePickReservationsForPickerLocation(MaterialLocationKind pickerLocation, string reason)
        {
            try
            {
                lock (_stateSync)
                {
                    if (pickerLocation != MaterialLocationKind.PickerFront &&
                        pickerLocation != MaterialLocationKind.PickerRear)
                        return 0;

                    int releaseCount = 0;
                    DateTime now = DateTime.Now;

                    for (int i = 0; i < State.Dies.Count; i++)
                    {
                        DieMaterial die = State.Dies[i];
                        if (die == null || die.ReservedPickerLocation != pickerLocation)
                            continue;

                        MaterialLocationKind kind = die.CurrentLocation != null
                            ? die.CurrentLocation.Kind
                            : MaterialLocationKind.Unknown;

                        if (kind != MaterialLocationKind.Unknown && kind != MaterialLocationKind.InputStage)
                            continue;

                        die.ReservedPickerLocation = MaterialLocationKind.Unknown;
                        die.ReservedPickerNo = -1;
                        die.UpdatedAt = now;
                        releaseCount++;
                    }

                    if (releaseCount > 0)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "사용 불가 Picker 예약을 해제했습니다. pickerLocation=" + pickerLocation +
                            ", count=" + releaseCount +
                            ", reason=" + reason + " - Ok");
                        NotifyAndSave("ReleaseInputStagePickReservationsForPickerLocation");
                    }

                    return releaseCount;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "사용 불가 Picker 예약 해제 중 예외가 발생했습니다. pickerLocation=" + pickerLocation +
                    ", error=" + ex.Message + " - Failed");
                return 0;
            }
            finally
            {
            }
        }

        public static bool ValidateInputStagePickTarget(
            string dieId,
            MaterialLocationKind pickerLocation,
            int pickerNo,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    if (string.IsNullOrWhiteSpace(dieId))
                    {
                        reason = "dieId is empty.";
                        return false;
                    }

                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null)
                    {
                        reason = "die material not found. dieId=" + dieId;
                        return false;
                    }

                    bool reservedByPicker =
                        die.ReservedPickerLocation == pickerLocation &&
                        die.ReservedPickerNo == pickerNo;
                    if (!reservedByPicker)
                    {
                        reason = "die is not reserved by current picker. die=" + dieId +
                                 ", reservedLocation=" + die.ReservedPickerLocation +
                                 ", reservedPickerNo=" + die.ReservedPickerNo +
                                 ", requestLocation=" + pickerLocation +
                                 ", requestPickerNo=" + pickerNo;
                        return false;
                    }

                    string candidateReason;
                    if (!CanUseInputPickCandidate(null, die, out candidateReason))
                    {
                        reason = candidateReason;
                        return false;
                    }

                    MaterialLocationKind kind = die.CurrentLocation != null
                        ? die.CurrentLocation.Kind
                        : MaterialLocationKind.Unknown;

                    if (kind != MaterialLocationKind.Unknown && kind != MaterialLocationKind.InputStage)
                    {
                        reason = "die location is not InputStage. die=" + dieId + ", location=" + kind;
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                reason = "input pick target validate exception: " + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick target validate failed: " + reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void InvalidateInputStageDieMappingNoLock(WaferMaterial wafer, bool alignOrThetaChanged)
        {
            if (wafer == null)
                return;

            wafer.HasInputStageDieMappingResult = false;
            wafer.InputStageDieMappingResultMode = "";
            wafer.InputStageDieMappingAlignRunId = "";
            wafer.InputStageDieMappingOffsetX = 0.0;
            wafer.InputStageDieMappingOffsetY = 0.0;
            wafer.HasInputStageDieMappingOrigin = false;
            wafer.InputStageDieMappingOriginX = 0.0;
            wafer.InputStageDieMappingOriginY = 0.0;
            wafer.HasInputStageDieMappingThetaSnapshot = false;
            wafer.InputStageDieMappingCorrectedT = 0.0;
            wafer.InputStageDieMappingInvalidatedByAlignChange = alignOrThetaChanged;
            wafer.InputMapApprovalHashAtMapping = "";
            wafer.HasInputStageRunReviewApproval = false;
            wafer.InputStageRunReviewStartDieIndex = 0;
            wafer.InputStageRunReviewStartDieUid = "";
            wafer.InputStageRunReviewOrderedDieIds = new List<string>();
            wafer.InputStageRunReviewMappingRevision = "";
            InputStageHybridResultSession.ClearMapping();
        }

        public static void InvalidateInputStageDieMappingResult(WaferMaterial wafer, string reason)
        {
            try
            {
                lock (_stateSync)
                {
                    if (wafer == null)
                        return;

                    // Apply 중 일부 상태가 갱신된 뒤 실패한 경우 저장 맵 자동 복원이
                    // 완료 결과를 되살리지 못하도록 강제 remap 상태로 둔다.
                    InvalidateInputStageDieMappingNoLock(wafer, true);
                    wafer.UpdatedAt = DateTime.Now;
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "InputStage die mapping result invalidated. waferId=" + (wafer.WaferId ?? "") +
                        ", reason=" + (reason ?? "") + " - Check");
                    NotifyAndSave("InputStageDieMappingApplyFailed");
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "InputStage die mapping result invalidate failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void SaveInputStageAlignResult(WaferMaterial wafer, double originX, double originY, double pitchX, double pitchY, double offsetX, double offsetY)
        {
            SaveInputStageAlignResult(wafer, originX, originY, pitchX, pitchY, offsetX, offsetY, false, 0.0, 0.0, 0.0);
        }

        public static void SaveInputStageAlignResult(
            WaferMaterial wafer,
            double originX,
            double originY,
            double pitchX,
            double pitchY,
            double offsetX,
            double offsetY,
            bool hasThetaAlign,
            double referenceT,
            double correctedT,
            double offsetT,
            string resultMode = "",
            string resultRunId = "")
        {
            try
            {
                if (wafer == null)
                    return;

                wafer.HasInputStageAlignResult = true;
                wafer.InputStageAlignResultMode = InputStageResultMode.NormalizeForSave(resultMode);
                wafer.InputStageAlignResultRunId = (resultRunId ?? "").Trim();
                if (!InputStageResultMode.IsHybrid(wafer.InputStageAlignResultMode))
                    InputStageHybridResultSession.Clear();
                wafer.InputStageAlignOriginX = originX;
                wafer.InputStageAlignOriginY = originY;
                wafer.InputStageAlignPitchX = pitchX;
                wafer.InputStageAlignPitchY = pitchY;
                wafer.InputStageAlignOffsetX = offsetX;
                wafer.InputStageAlignOffsetY = offsetY;
                if (hasThetaAlign)
                {
                    wafer.HasInputStageThetaAlignResult = true;
                    wafer.InputStageAlignReferenceT = referenceT;
                    wafer.InputStageAlignCorrectedT = correctedT;
                    wafer.InputStageAlignOffsetT = offsetT;
                }
                InvalidateInputStageDieMappingNoLock(wafer, true);
                wafer.State = WaferMaterialStateText.Normalize(WaferMaterialState.Working);
                wafer.UpdatedAt = DateTime.Now;
                NotifyAndSave("InputStageAlignResult");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input stage align result save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static void SaveInputStageThetaAlignResult(WaferMaterial wafer, double referenceT, double correctedT, double offsetT)
        {
            try
            {
                if (wafer == null)
                    return;

                bool thetaChanged = !wafer.HasInputStageThetaAlignResult ||
                    Math.Abs(wafer.InputStageAlignReferenceT - referenceT) > InputStageThetaMappingSnapshotToleranceDeg ||
                    Math.Abs(wafer.InputStageAlignCorrectedT - correctedT) > InputStageThetaMappingSnapshotToleranceDeg ||
                    Math.Abs(wafer.InputStageAlignOffsetT - offsetT) > InputStageThetaMappingSnapshotToleranceDeg;

                wafer.HasInputStageThetaAlignResult = true;
                wafer.InputStageAlignReferenceT = referenceT;
                wafer.InputStageAlignCorrectedT = correctedT;
                wafer.InputStageAlignOffsetT = offsetT;
                if (thetaChanged)
                {
                    InvalidateInputStageDieMappingNoLock(wafer, true);
                    if (InputStageResultMode.IsHybrid(wafer.InputStageAlignResultMode))
                        InputStageHybridResultSession.Clear();
                }
                wafer.State = WaferMaterialStateText.Normalize(WaferMaterialState.Working);
                wafer.UpdatedAt = DateTime.Now;
                NotifyAndSave("InputStageThetaAlignResult");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input stage theta align result save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static bool RestoreInputStageDieMappingCompleteFromSavedMap(string reason)
        {
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (wafer == null || wafer.HasInputStageDieMappingResult)
                        return false;

                    string restoreReason;
                    if (!CanRestoreInputStageDieMappingCompleteNoLock(wafer, out restoreReason))
                        return false;

                    wafer.HasInputStageDieMappingResult = true;
                    wafer.InputStageDieMappingOffsetX = NormalizeFinite(wafer.InputStageDieMappingOffsetX);
                    wafer.InputStageDieMappingOffsetY = NormalizeFinite(wafer.InputStageDieMappingOffsetY);
                    if (!wafer.HasInputStageDieMappingOrigin)
                    {
                        DieMap restoredMap = BuildDieMapFromWafer(wafer);
                        if (restoredMap != null)
                        {
                            wafer.HasInputStageDieMappingOrigin = true;
                            wafer.InputStageDieMappingOriginX = restoredMap.OriginX;
                            wafer.InputStageDieMappingOriginY = restoredMap.OriginY;
                        }
                    }
                    if (!wafer.HasInputStageDieMappingThetaSnapshot)
                    {
                        wafer.HasInputStageDieMappingThetaSnapshot = true;
                        wafer.InputStageDieMappingCorrectedT = wafer.InputStageAlignCorrectedT;
                    }
                    wafer.InputStageDieMappingInvalidatedByAlignChange = false;
                    wafer.HasInputStageRunReviewApproval = false;
                    wafer.InputStageRunReviewStartDieIndex = 0;
                    wafer.InputStageRunReviewStartDieUid = "";
                    wafer.InputStageRunReviewOrderedDieIds = new List<string>();
                    wafer.InputStageRunReviewMappingRevision = "";
                    wafer.State = WaferMaterialStateText.Normalize(WaferMaterialState.Working);
                    wafer.UpdatedAt = DateTime.Now;

                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "InputStage DieMap complete restored from saved map data. waferId=" + wafer.WaferId +
                        ", dieCount=" + (wafer.DieIds != null ? wafer.DieIds.Count : 0) +
                        ", reason=" + restoreReason + " - Ok");
                    NotifyAndSave(string.IsNullOrWhiteSpace(reason)
                        ? "InputStageDieMapCompleteRestore"
                        : reason);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "InputStage DieMap complete restore failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool CanRestoreInputStageDieMappingCompleteNoLock(WaferMaterial wafer, out string reason)
        {
            reason = string.Empty;

            if (wafer == null)
            {
                reason = "wafer is null.";
                return false;
            }

            if (!wafer.HasInputStageAlignResult)
            {
                reason = "align result is not complete.";
                return false;
            }

            if (!wafer.HasInputStageThetaAlignResult)
            {
                reason = "theta align result is not complete.";
                return false;
            }

            if (wafer.InputStageDieMappingInvalidatedByAlignChange)
            {
                reason = "die mapping was invalidated by align/theta change.";
                return false;
            }

            if (!IsStoredInputStageResultModeUsableNoLock(wafer, true, out reason))
                return false;

            if (wafer.HasInputStageDieMappingThetaSnapshot &&
                Math.Abs(wafer.InputStageDieMappingCorrectedT - wafer.InputStageAlignCorrectedT) >
                    InputStageThetaMappingSnapshotToleranceDeg)
            {
                reason = "theta align value changed after die mapping. mappedT=" +
                         wafer.InputStageDieMappingCorrectedT.ToString("F6") +
                         ", currentT=" + wafer.InputStageAlignCorrectedT.ToString("F6");
                return false;
            }

            if (wafer.DieIds == null || wafer.DieIds.Count == 0)
            {
                reason = "die id list is empty.";
                return false;
            }

            DieMap map = BuildDieMapFromWafer(wafer);
            if (map == null || map.Entries == null || map.Entries.Count == 0)
            {
                reason = "die map rebuild failed.";
                return false;
            }

            reason = "die map data exists. frame=" + (map.FrameObjId ?? "") +
                     ", dieCount=" + map.Entries.Count;
            return true;
        }

        private static double NormalizeFinite(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0.0 : value;
        }

        public static void ResetInputStageThetaAlignResult(WaferMaterial wafer, string reason)
        {
            try
            {
                if (wafer == null)
                    return;

                wafer.HasInputStageThetaAlignResult = false;
                wafer.InputStageAlignReferenceT = 0.0;
                wafer.InputStageAlignCorrectedT = 0.0;
                wafer.InputStageAlignOffsetT = 0.0;
                InvalidateInputStageDieMappingNoLock(wafer, true);
                InputStageHybridResultSession.Clear();
                wafer.UpdatedAt = DateTime.Now;
                NotifyAndSave(string.IsNullOrWhiteSpace(reason) ? "InputStageThetaAlignReset" : reason);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input stage theta align result reset failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public static DieMap BuildInputDieMapFromStageWafer()
        {
            try
            {
                WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer == null || !wafer.HasInputStageDieMappingResult)
                    return null;

                string resultModeReason;
                if (!IsStoredInputStageResultModeUsable(
                        wafer,
                        true,
                        out resultModeReason))
                {
                    return null;
                }

                return BuildDieMapFromWafer(wafer);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input die map rebuild from stage wafer failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static DieMap BuildDieMapFromWafer(WaferMaterial wafer)
        {
            try
            {
                if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                    return null;

                List<DieMaterial> dies = ResolveWaferDies(wafer);
                if (dies.Count == 0)
                    return null;

                int maxX = dies.Max(d => d.Wafer_IndexX);
                int maxY = dies.Max(d => d.Wafer_IndexY);
                if (maxX < 0 || maxY < 0)
                    return null;

                double pitchX = wafer.InputStageAlignPitchX > 0.0 ? wafer.InputStageAlignPitchX : ResolvePitch(dies, true);
                double pitchY = wafer.InputStageAlignPitchY > 0.0 ? wafer.InputStageAlignPitchY : ResolvePitch(dies, false);
                double originX = wafer.HasInputStageDieMappingOrigin
                    ? wafer.InputStageDieMappingOriginX
                    : (wafer.HasInputStageAlignResult ? wafer.InputStageAlignOriginX : ResolveOrigin(dies, true));
                double originY = wafer.HasInputStageDieMappingOrigin
                    ? wafer.InputStageDieMappingOriginY
                    : (wafer.HasInputStageAlignResult ? wafer.InputStageAlignOriginY : ResolveOrigin(dies, false));
                double dieSizeX = wafer.InputStageDieSizeX;
                double dieSizeY = wafer.InputStageDieSizeY;
                double outerDiameterMm = wafer.InputStageOuterDiameterMm;
                ResolveLegacyWaferGeometry(wafer, ref dieSizeX, ref dieSizeY, ref outerDiameterMm);

                var map = new DieMap
                {
                    FrameObjId = string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId) ? wafer.WaferId : wafer.DieMapFrameObjId,
                    DieMapX = maxX + 1,
                    DieMapY = maxY + 1,
                    PitchX = pitchX,
                    PitchY = pitchY,
                    DieSizeX = dieSizeX,
                    DieSizeY = dieSizeY,
                    OuterDiameterMm = outerDiameterMm,
                    OriginX = originX,
                    OriginY = originY,
                    CreatedAt = wafer.UpdatedAt
                };

                int index = 0;
                foreach (DieMaterial die in dies.OrderBy(d => d.Wafer_IndexY).ThenBy(d => d.Wafer_IndexX))
                {
                    if (die == null || die.Wafer_IndexX < 0 || die.Wafer_IndexY < 0)
                        continue;

                    map.Entries.Add(new DieMapEntry
                    {
                        Index = index++,
                        SequenceNo = die.InputSequenceNo,
                        DieMapX = die.Wafer_IndexX,
                        DieMapY = die.Wafer_IndexY,
                        OriginalMapX = die.Wafer_OriginalIndexX >= 0 ? die.Wafer_OriginalIndexX : die.Wafer_IndexX,
                        OriginalMapY = die.Wafer_OriginalIndexY >= 0 ? die.Wafer_OriginalIndexY : die.Wafer_IndexY,
                        IsTarget = die.IsInputTarget,
                        Result = die.Result,
                        BinCode = die.Input_BinCode,
                        EquipmentGridX = die.Wafer_IndexX - Math.Max(0, maxX) / 2.0,
                        EquipmentGridY = DieMapGenerator.CalculateEquipmentGridY(die.Wafer_IndexY, maxY + 1),
                        PosX = die.WaferOffset != null && die.WaferOffset.IsValid ? die.WaferOffset.X : originX + pitchX * die.Wafer_IndexX,
                        PosY = die.WaferOffset != null && die.WaferOffset.IsValid ? die.WaferOffset.Y : originY + pitchY * die.Wafer_IndexY,
                        DieUid = die.DieId
                    });
                }

                if (!HasCompleteInputSequence(map))
                    PickupSequenceGenerator.ApplySequenceNumbers(map, ResolveInputPickup(RecipeStore.LoadLastOrDefault()));
                return DieMapGenerator.Normalize(map);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Die map rebuild from wafer failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static DieMap BuildOutputReceiveDieMapFromWafer(WaferMaterial wafer)
        {
            try
            {
                if (wafer == null || wafer.OutputReceiveSlots == null || wafer.OutputReceiveSlots.Count == 0)
                    return null;

                int maxX = wafer.OutputReceiveSlots.Max(s => s != null ? s.DieMapX : -1);
                int maxY = wafer.OutputReceiveSlots.Max(s => s != null ? s.DieMapY : -1);
                if (maxX < 0 || maxY < 0)
                    return null;

                double dieSizeX = wafer.OutputReceiveDieSizeX;
                double dieSizeY = wafer.OutputReceiveDieSizeY;
                double outerDiameterMm = wafer.OutputReceiveOuterDiameterMm;
                ResolveLegacyWaferGeometry(wafer, ref dieSizeX, ref dieSizeY, ref outerDiameterMm);

                var map = new DieMap
                {
                    FrameObjId = string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId) ? wafer.WaferId : wafer.DieMapFrameObjId,
                    // OutputReceiveSlots에는 Target cell만 남을 수 있으므로 승인 역할 맵의 전체 Grid 스냅샷을 우선한다.
                    DieMapX = Math.Max(maxX + 1, wafer.OutputReceiveDieMapX),
                    DieMapY = Math.Max(maxY + 1, wafer.OutputReceiveDieMapY),
                    PitchX = wafer.OutputReceivePitchX,
                    PitchY = wafer.OutputReceivePitchY,
                    DieSizeX = dieSizeX,
                    DieSizeY = dieSizeY,
                    OuterDiameterMm = outerDiameterMm,
                    OriginX = wafer.OutputReceiveOriginX,
                    OriginY = wafer.OutputReceiveOriginY,
                    CreatedAt = wafer.UpdatedAt
                };

                foreach (OutputReceiveSlotMaterial slot in wafer.OutputReceiveSlots.OrderBy(s => s != null ? s.OrderIndex : int.MaxValue))
                {
                    if (slot == null || slot.DieMapX < 0 || slot.DieMapY < 0)
                        continue;

                    map.Entries.Add(new DieMapEntry
                    {
                        Index = slot.OrderIndex,
                        SequenceNo = slot.SequenceNo,
                        DieMapX = slot.DieMapX,
                        DieMapY = slot.DieMapY,
                        OriginalMapX = slot.OriginalMapX >= 0 ? slot.OriginalMapX : slot.DieMapX,
                        OriginalMapY = slot.OriginalMapY >= 0 ? slot.OriginalMapY : slot.DieMapY,
                        IsTarget = slot.IsTarget,
                        Result = slot.Result,
                        BinCode = slot.BinCode,
                        EquipmentGridX = slot.DieMapX - Math.Max(0, map.DieMapX - 1) / 2.0,
                        EquipmentGridY = DieMapGenerator.CalculateEquipmentGridY(slot.DieMapY, map.DieMapY),
                        PosX = slot.PosX,
                        PosY = slot.PosY,
                        DieUid = slot.DieUid ?? ""
                    });
                }

                return DieMapGenerator.Normalize(map);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output receive die map rebuild from wafer failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static void ResolveLegacyWaferGeometry(
            WaferMaterial wafer,
            ref double dieSizeX,
            ref double dieSizeY,
            ref double outerDiameterMm)
        {
            if (wafer == null ||
                (dieSizeX > 0.0 && dieSizeY > 0.0 && outerDiameterMm > 0.0))
                return;

            try
            {
                TapeFrameSpec spec = !string.IsNullOrWhiteSpace(wafer.TapeFrameSpecName)
                    ? MaterialSpecs.FindFrame(wafer.TapeFrameSpecName)
                    : null;
                if (spec == null)
                    return;
                if (dieSizeX <= 0.0 && spec.DieSizeX > 0.0) dieSizeX = spec.DieSizeX;
                if (dieSizeY <= 0.0 && spec.DieSizeY > 0.0) dieSizeY = spec.DieSizeY;
                if (outerDiameterMm <= 0.0 && spec.OuterDiameterMm > 0.0)
                    outerDiameterMm = spec.OuterDiameterMm;
            }
            catch
            {
            }
        }

        private static bool HasCompleteInputSequence(DieMap map)
        {
            try
            {
                if (map == null || map.Entries == null)
                    return false;

                int targets = 0;
                int sequenced = 0;
                var used = new HashSet<int>();
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null || !entry.IsTarget)
                        continue;

                    targets++;
                    if (entry.SequenceNo > 0 && used.Add(entry.SequenceNo))
                        sequenced++;
                }

                return targets > 0 && sequenced == targets;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public static WaferMapData BuildWaferMapDataFromWafer(WaferMaterial wafer)
        {
            try
            {
                DieMap dieMap = BuildDieMapFromWafer(wafer);
                if (dieMap == null || dieMap.DieMapX <= 0 || dieMap.DieMapY <= 0)
                    return null;

                var map = new WaferMapData
                {
                    WaferId = wafer != null ? wafer.WaferId : "",
                    ColumnCount = dieMap.DieMapX,
                    RowCount = dieMap.DieMapY,
                    DieMap = new bool[dieMap.DieMapY, dieMap.DieMapX],
                    Ref1Row = dieMap.DieMapY / 2,
                    Ref1Col = Math.Max(0, dieMap.DieMapX / 4),
                    Ref2Row = dieMap.DieMapY / 2,
                    Ref2Col = dieMap.DieMapX > 1 ? Math.Min(dieMap.DieMapX - 1, (dieMap.DieMapX * 3) / 4) : 0
                };

                foreach (DieMapEntry entry in dieMap.Entries)
                {
                    int mapX = ResolveEntryMapX(entry);
                    int mapY = ResolveEntryMapY(entry);
                    if (entry == null || mapX < 0 || mapY < 0 || mapX >= map.ColumnCount || mapY >= map.RowCount)
                        continue;
                    map.DieMap[mapY, mapX] = entry.IsTarget;
                }

                return map;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Wafer map rebuild from wafer failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static List<DieMaterial> ResolveWaferDies(WaferMaterial wafer)
        {
            if (wafer == null)
                return new List<DieMaterial>();

            List<DieMaterial> source = State.Dies.Where(d =>
                d != null &&
                string.Equals(d.WaferID_Input, wafer.WaferId, StringComparison.OrdinalIgnoreCase) &&
                d.Wafer_IndexX >= 0 &&
                d.Wafer_IndexY >= 0).ToList();

            if (wafer.DieIds != null)
            {
                List<string> dieIds = wafer.DieIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (dieIds.Count == 0)
                    return new List<DieMaterial>();

                var byId = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
                foreach (DieMaterial die in source)
                {
                    if (die == null || string.IsNullOrWhiteSpace(die.DieId))
                        continue;

                    DieMaterial existing;
                    if (!byId.TryGetValue(die.DieId, out existing) || IsBetterWaferDie(die, existing))
                        byId[die.DieId] = die;
                }

                var ordered = new List<DieMaterial>();
                foreach (string dieId in dieIds)
                {
                    DieMaterial die;
                    if (byId.TryGetValue(dieId, out die))
                        ordered.Add(die);
                }

                return DeduplicateWaferDiesByGrid(ordered);
            }

            return DeduplicateWaferDiesByGrid(source);
        }

        private static List<DieMaterial> DeduplicateWaferDiesByGrid(IEnumerable<DieMaterial> dies)
        {
            var byGrid = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
            if (dies == null)
                return new List<DieMaterial>();

            foreach (DieMaterial die in dies)
            {
                if (die == null || die.Wafer_IndexX < 0 || die.Wafer_IndexY < 0)
                    continue;

                string key = die.Wafer_IndexY.ToString() + ":" + die.Wafer_IndexX.ToString();
                DieMaterial existing;
                if (!byGrid.TryGetValue(key, out existing) || IsBetterWaferDie(die, existing))
                    byGrid[key] = die;
            }

            return byGrid.Values
                .OrderBy(d => d.Wafer_IndexY)
                .ThenBy(d => d.Wafer_IndexX)
                .ToList();
        }

        private static bool IsBetterWaferDie(DieMaterial candidate, DieMaterial current)
        {
            if (candidate == null)
                return false;
            if (current == null)
                return true;

            bool candidateInStage = candidate.CurrentLocation != null && candidate.CurrentLocation.Kind == MaterialLocationKind.InputStage;
            bool currentInStage = current.CurrentLocation != null && current.CurrentLocation.Kind == MaterialLocationKind.InputStage;
            if (candidateInStage != currentInStage)
                return candidateInStage;

            if (candidate.IsInputTarget != current.IsInputTarget)
                return candidate.IsInputTarget;

            return candidate.UpdatedAt >= current.UpdatedAt;
        }

        private static double ResolvePitch(List<DieMaterial> dies, bool xAxis)
        {
            try
            {
                var ordered = dies
                    .Where(d => d != null && d.WaferOffset != null && d.WaferOffset.IsValid)
                    .OrderBy(d => xAxis ? d.Wafer_IndexX : d.Wafer_IndexY)
                    .ToList();
                for (int i = 1; i < ordered.Count; i++)
                {
                    int indexDelta = xAxis ? ordered[i].Wafer_IndexX - ordered[i - 1].Wafer_IndexX : ordered[i].Wafer_IndexY - ordered[i - 1].Wafer_IndexY;
                    if (indexDelta == 0)
                        continue;
                    double posDelta = xAxis ? ordered[i].WaferOffset.X - ordered[i - 1].WaferOffset.X : ordered[i].WaferOffset.Y - ordered[i - 1].WaferOffset.Y;
                    if (Math.Abs(posDelta) > 1e-9)
                        return Math.Abs(posDelta / indexDelta);
                }
            }
            catch
            {
            }
            finally
            {
            }

            return 0.0;
        }

        private static double ResolveOrigin(List<DieMaterial> dies, bool xAxis)
        {
            try
            {
                DieMaterial first = dies
                    .Where(d => d != null && d.WaferOffset != null && d.WaferOffset.IsValid)
                    .OrderBy(d => xAxis ? d.Wafer_IndexX : d.Wafer_IndexY)
                    .FirstOrDefault();
                if (first != null)
                    return xAxis ? first.WaferOffset.X : first.WaferOffset.Y;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.0;
        }

        public static void MoveDie(string dieId, MaterialLocation location)
        {
            var die = GetOrCreateDieMaterial(dieId);
            MaterialLocation previousLocation = die.CurrentLocation;
            die.CurrentLocation = location ?? MaterialLocation.Unknown();
            die.ReservedPickerLocation = MaterialLocationKind.Unknown;
            die.ReservedPickerNo = -1;
            die.UpdatedAt = DateTime.Now;
            SequenceTrace.MaterialChange(
                "MoveDie",
                "die=" + die.DieId,
                "from=" + previousLocation,
                "to=" + die.CurrentLocation,
                "result=" + die.Result);
            NotifyAndSave("MoveDie");
        }

        public static bool MoveInputDieToPickerManually(
            string dieId,
            MaterialLocationKind pickerLocation,
            int pickerNo,
            string reason,
            out string message)
        {
            message = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(dieId))
                {
                    message = "이동할 Die ID가 비어 있습니다.";
                    return false;
                }

                if (!IsPickerLocation(pickerLocation))
                {
                    message = "Picker 위치가 올바르지 않습니다. location=" + pickerLocation;
                    return false;
                }

                if (pickerNo < 1 || pickerNo > 4)
                {
                    message = "Picker 번호가 올바르지 않습니다. pickerNo=" + pickerNo;
                    return false;
                }

                MaterialLocation previousLocation;
                lock (_stateSync)
                {
                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null)
                    {
                        message = "Die 정보를 찾을 수 없습니다. dieId=" + dieId;
                        return false;
                    }

                    if (die.CurrentLocation == null ||
                        die.CurrentLocation.Kind != MaterialLocationKind.InputStage)
                    {
                        message = "InputStage에 있는 Die만 Picker로 이동할 수 있습니다. dieId=" + dieId +
                                  ", current=" + (die.CurrentLocation != null ? die.CurrentLocation.ToString() : "Unknown");
                        return false;
                    }

                    if (!die.IsInputTarget)
                    {
                        message = "SKIP/제외 상태의 Die는 Picker로 이동할 수 없습니다. dieId=" + dieId;
                        return false;
                    }

                    DieMaterial occupiedDie = FindDieAtPickerNoLock(pickerLocation, pickerNo);
                    if (occupiedDie != null &&
                        !string.Equals(occupiedDie.DieId, dieId, StringComparison.OrdinalIgnoreCase))
                    {
                        message = "선택한 Picker가 이미 Die를 가지고 있습니다. location=" + pickerLocation +
                                  ", pickerNo=" + pickerNo +
                                  ", loadedDie=" + occupiedDie.DieId;
                        return false;
                    }

                    previousLocation = die.CurrentLocation;
                    die.CurrentLocation = MaterialLocation.Picker(pickerLocation, pickerNo);
                    die.ReservedPickerLocation = MaterialLocationKind.Unknown;
                    die.ReservedPickerNo = -1;
                    die.PickedPickerLocation = pickerLocation;
                    die.PickedPickerNo = pickerNo;
                    die.PickedAt = DateTime.Now;
                    die.UpdatedAt = DateTime.Now;

                    SequenceTrace.MaterialChange(
                        "ManualInputDieToPicker",
                        "die=" + die.DieId,
                        "from=" + previousLocation,
                        "to=" + die.CurrentLocation,
                        "pickerLocation=" + pickerLocation,
                        "pickerNo=" + pickerNo,
                        "reason=" + (reason ?? ""),
                        "result=" + die.Result);
                }

                string saveReason = "MapTransferManualInputDieToPicker:" + dieId;
                NotifyAndSave(saveReason);
                Log.Write("Main", "MATERIAL", "ManualInputDieToPicker",
                    "Input die data moved to picker manually. dieId=" + dieId +
                    ", from=" + previousLocation +
                    ", pickerLocation=" + pickerLocation +
                    ", pickerNo=" + pickerNo +
                    ", reason=" + (reason ?? "") + " - Ok");

                message = "Die 데이터를 Picker로 이동했습니다. dieId=" + dieId;
                return true;
            }
            catch (Exception ex)
            {
                message = "Input Die 데이터 Picker 이동 실패: " + ex.Message;
                Log.Write("Main", "MATERIAL", "ManualInputDieToPicker",
                    "Input die data move to picker failed. dieId=" + dieId +
                    ", pickerLocation=" + pickerLocation +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool TryApplyInputMapOffsetPreservingDieState(
            DieMap map,
            double offsetX,
            double offsetY,
            string reason,
            out int updatedDieCount,
            out string detail)
        {
            updatedDieCount = 0;
            detail = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    if (map == null || map.Entries == null)
                    {
                        detail = "Input Die Map이 없습니다.";
                        return false;
                    }

                    map.OriginX += offsetX;
                    map.OriginY += offsetY;
                    foreach (DieMapEntry entry in map.Entries)
                    {
                        if (entry == null)
                            continue;

                        entry.PosX += offsetX;
                        entry.PosY += offsetY;

                        DieMaterial die = State.Dies.FirstOrDefault(x =>
                            x != null &&
                            !string.IsNullOrWhiteSpace(entry.DieUid) &&
                            string.Equals(x.DieId, entry.DieUid, StringComparison.OrdinalIgnoreCase));
                        if (die == null)
                            continue;

                        if (die.WaferOffset == null)
                            die.WaferOffset = new VisionOffset();
                        die.WaferOffset.X = entry.PosX;
                        die.WaferOffset.Y = entry.PosY;
                        die.WaferOffset.R = 0.0;
                        die.WaferOffset.IsValid = true;
                        die.UpdatedAt = DateTime.Now;
                        updatedDieCount++;
                    }

                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    if (wafer != null)
                    {
                        double alignOriginX = wafer.HasInputStageAlignResult
                            ? wafer.InputStageAlignOriginX
                            : map.OriginX;
                        double alignOriginY = wafer.HasInputStageAlignResult
                            ? wafer.InputStageAlignOriginY
                            : map.OriginY;
                        wafer.HasInputStageDieMappingOrigin = true;
                        wafer.InputStageDieMappingOriginX = map.OriginX;
                        wafer.InputStageDieMappingOriginY = map.OriginY;
                        wafer.InputStageDieMappingOffsetX = map.OriginX - alignOriginX;
                        wafer.InputStageDieMappingOffsetY = map.OriginY - alignOriginY;
                        wafer.UpdatedAt = DateTime.Now;
                    }

                    LotStorage.ActiveInputDieMap = map;
                }

                NotifyAndSave(string.IsNullOrWhiteSpace(reason)
                    ? "InputDieMapCoordinateOffsetPreserveState"
                    : reason);
                detail = "Die 상태를 유지한 채 전체 Input Die 좌표를 갱신했습니다. updated=" + updatedDieCount +
                         ", offsetX=" + offsetX.ToString("F6") +
                         ", offsetY=" + offsetY.ToString("F6");
                Log.Write("Main", "SYSTEM", "MaterialStateService", detail + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                detail = "Die 상태 유지 Input Die 좌표 갱신 실패. error=" + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", detail + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool TryApplyLastVisionOffsetToPendingInputDies(
            string referenceDieId,
            double offsetX,
            double offsetY,
            string reason,
            out int updatedDieCount,
            out int skippedDieCount,
            out string detail)
        {
            updatedDieCount = 0;
            skippedDieCount = 0;
            detail = string.Empty;

            try
            {
                lock (_stateSync)
                {
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    DieMap map = LotStorage.ActiveInputDieMap ?? BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null)
                    {
                        detail = "InputStage Wafer 또는 Die Map이 없어 미촬영 Die 좌표를 갱신할 수 없습니다.";
                        return false;
                    }

                    foreach (DieMapEntry entry in map.Entries)
                    {
                        if (entry == null || !entry.IsTarget || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = State.Dies.FirstOrDefault(x =>
                            x != null && string.Equals(x.DieId, entry.DieUid, StringComparison.OrdinalIgnoreCase));
                        if (!IsPendingUninspectedInputDie(die))
                        {
                            skippedDieCount++;
                            continue;
                        }

                        entry.PosX += offsetX;
                        entry.PosY += offsetY;
                        if (die.WaferOffset == null)
                            die.WaferOffset = new VisionOffset();
                        die.WaferOffset.X = entry.PosX;
                        die.WaferOffset.Y = entry.PosY;
                        die.WaferOffset.R = 0.0;
                        die.WaferOffset.IsValid = true;
                        die.UpdatedAt = DateTime.Now;
                        updatedDieCount++;
                    }

                    LotStorage.ActiveInputDieMap = map;
                }

                NotifyAndSave(string.IsNullOrWhiteSpace(reason)
                    ? "InputLastVisionOffsetToPendingDies"
                    : reason);
                detail = "마지막 Input Vision 결과를 미촬영·미예약 Die 좌표에 적용했습니다. referenceDie=" +
                         (referenceDieId ?? "") +
                         ", updated=" + updatedDieCount +
                         ", skipped=" + skippedDieCount +
                         ", offsetX=" + offsetX.ToString("F6") +
                         ", offsetY=" + offsetY.ToString("F6");
                Log.Write("Main", "SYSTEM", "MaterialStateService", detail + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                detail = "마지막 Input Vision 결과의 미촬영 Die 좌표 적용 실패. referenceDie=" +
                         (referenceDieId ?? "") + ", error=" + ex.Message;
                Log.Write("Main", "SYSTEM", "MaterialStateService", detail + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool IsPendingUninspectedInputDie(DieMaterial die)
        {
            if (die == null || !die.IsInputTarget)
                return false;
            if (die.CurrentLocation == null || die.CurrentLocation.Kind != MaterialLocationKind.InputStage)
                return false;
            if (die.ReservedPickerLocation != MaterialLocationKind.Unknown || die.ReservedPickerNo > 0)
                return false;
            if (die.PickedPickerLocation != MaterialLocationKind.Unknown || die.PickedPickerNo > 0)
                return false;

            if (die.Inspections != null)
            {
                for (int i = 0; i < die.Inspections.Count; i++)
                {
                    DieInspectionRecord record = die.Inspections[i];
                    if (record != null &&
                        string.Equals(record.InspectionType, "InputPickVision", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool MarkDiePickedByPicker(string dieId, MaterialLocationKind pickerLocation, int pickerNo)
        {
            try
            {
                lock (_stateSync)
                {
                    if (string.IsNullOrWhiteSpace(dieId))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Pick die state update failed: dieId is empty. - Failed");
                        return false;
                    }

                    if (pickerLocation != MaterialLocationKind.PickerFront &&
                        pickerLocation != MaterialLocationKind.PickerRear)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Pick die state update failed: invalid pickerLocation=" + pickerLocation +
                            ", dieId=" + dieId + " - Failed");
                        return false;
                    }

                    if (pickerNo <= 0)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Pick die state update failed: invalid pickerNo=" + pickerNo +
                            ", dieId=" + dieId + " - Failed");
                        return false;
                    }

                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Pick die state update failed: die material not found. dieId=" + dieId + " - Failed");
                        return false;
                    }

                    DieMaterial occupiedDie = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        !string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase) &&
                        d.CurrentLocation != null &&
                        d.CurrentLocation.Kind == pickerLocation &&
                        d.CurrentLocation.PickerNo == pickerNo);
                    if (occupiedDie != null)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Pick die state update blocked: picker already has die. " +
                            "Picker가 이미 Die를 가지고 있어 상태를 덮어쓰지 않습니다. " +
                            "pickerLocation=" + pickerLocation +
                            ", pickerNo=" + pickerNo +
                            ", loadedDie=" + occupiedDie.DieId +
                            ", requestedDie=" + dieId + " - Blocked");
                        return false;
                    }

                    MaterialLocation previousLocation = die.CurrentLocation;
                    die.CurrentLocation = MaterialLocation.Picker(pickerLocation, pickerNo);
                    die.ReservedPickerLocation = MaterialLocationKind.Unknown;
                    die.ReservedPickerNo = -1;
                    die.PickedPickerLocation = pickerLocation;
                    die.PickedPickerNo = pickerNo;
                    die.PickedAt = DateTime.Now;
                    die.UpdatedAt = DateTime.Now;
                    SequenceTrace.MaterialChange(
                        "PickDie",
                        "die=" + die.DieId,
                        "from=" + previousLocation,
                        "to=" + die.CurrentLocation,
                        "pickerLocation=" + pickerLocation,
                        "pickerNo=" + pickerNo,
                        "result=" + die.Result);
                    NotifyAndSave("PickDie");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Pick die state update failed: dieId=" + dieId +
                    ", pickerLocation=" + pickerLocation +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static string ResolveInputDieDisplayState(DieMapEntry entry)
        {
            try
            {
                if (entry == null)
                    return "";

                if (!entry.IsTarget)
                    return "SKIP";

                DieMaterial die = GetDieMaterial(entry.DieUid);
                if (die == null)
                    return "TARGET";

                if (!die.IsInputTarget)
                    return "SKIP";

                if (die.Result == DieResult.NG)
                    return "REJECT";

                if (IsDieReservedForPicker(die))
                    return die.ReservedPickerNo > 0 ? "RESERVE" + die.ReservedPickerNo : "RESERVE";

                MaterialLocation location = die.CurrentLocation;
                MaterialLocationKind kind = location != null ? location.Kind : MaterialLocationKind.Unknown;
                switch (kind)
                {
                    case MaterialLocationKind.PickerFront:
                    case MaterialLocationKind.PickerRear:
                        return location != null && location.PickerNo > 0 ? "PICK" + location.PickerNo : "PICK";
                    case MaterialLocationKind.OutputStageGood:
                        return "GOOD STAGE";
                    case MaterialLocationKind.OutputStageNg:
                        return "NG STAGE";
                    case MaterialLocationKind.OutputFeeder:
                        return "OUT FEEDER";
                    case MaterialLocationKind.OutputCassette:
                        return "FINISH";
                    case MaterialLocationKind.InputStage:
                    case MaterialLocationKind.Unknown:
                    default:
                        return "TARGET";
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Resolve input die display state failed: die=" +
                    (entry != null ? entry.DieUid : "-") +
                    ", error=" + ex.Message + " - Failed");
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        private static bool IsDieReservedForPicker(DieMaterial die)
        {
            if (die == null)
                return false;

            return (die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                    die.ReservedPickerLocation == MaterialLocationKind.PickerRear) &&
                   die.ReservedPickerNo > 0;
        }

        private static bool CanUseInputPickCandidate(DieMapEntry entry, DieMaterial die, out string reason)
        {
            reason = string.Empty;

            if (die == null)
            {
                reason = "die material is null.";
                return false;
            }

            if (entry != null)
            {
                if (!entry.IsTarget)
                {
                    reason = "die map target is disabled. die=" + entry.DieUid +
                             ", sequence=" + entry.SequenceNo +
                             ", grid=(" + ResolveEntryMapX(entry) + "," + ResolveEntryMapY(entry) + ")";
                    return false;
                }

                if (entry.Result == DieResult.Good || entry.Result == DieResult.NG)
                {
                    reason = "die map result is already completed. die=" + entry.DieUid +
                             ", result=" + entry.Result +
                             ", sequence=" + entry.SequenceNo +
                             ", grid=(" + ResolveEntryMapX(entry) + "," + ResolveEntryMapY(entry) + ")";
                    return false;
                }
            }

            if (!die.IsInputTarget)
            {
                reason = "die input target is disabled. die=" + die.DieId +
                         ", sequence=" + die.InputSequenceNo +
                         ", grid=(" + die.Wafer_IndexX + "," + die.Wafer_IndexY + ")";
                return false;
            }

            if (die.Result == DieResult.Good || die.Result == DieResult.NG)
            {
                reason = "die result is already completed. die=" + die.DieId +
                         ", result=" + die.Result +
                         ", sequence=" + die.InputSequenceNo +
                         ", grid=(" + die.Wafer_IndexX + "," + die.Wafer_IndexY + ")";
                return false;
            }

            if (HasInputPickCompletedHistory(die))
            {
                reason = "die was already picked. die=" + die.DieId +
                         ", pickedPickerLocation=" + die.PickedPickerLocation +
                         ", pickedPickerNo=" + die.PickedPickerNo +
                         ", pickedAt=" + FormatDateTimeForLog(die.PickedAt) +
                         ", sequence=" + die.InputSequenceNo +
                         ", grid=(" + die.Wafer_IndexX + "," + die.Wafer_IndexY + ")";
                return false;
            }

            return true;
        }

        private static bool HasInputPickCompletedHistory(DieMaterial die)
        {
            if (die == null)
                return false;

            if (HasValidPickedAt(die.PickedAt) ||
                die.PickedPickerNo > 0 ||
                IsPickerLocation(die.PickedPickerLocation))
                return true;

            if (die.Inspections == null || die.Inspections.Count == 0)
                return false;

            for (int i = 0; i < die.Inspections.Count; i++)
            {
                DieInspectionRecord record = die.Inspections[i];
                if (record == null)
                    continue;

                if (!string.Equals(record.InspectionType, "PickUp", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (record.Result != MaterialInspectionResult.Unknown)
                    return true;
            }

            return false;
        }

        private static bool HasValidPickedAt(DateTime pickedAt)
        {
            if (pickedAt == DateTime.MinValue)
                return false;

            // MaterialSnapshotStore stores optional empty DateTime values as 1900-01-01
            // because JSON serializers cannot safely round-trip DateTime.MinValue.
            // Treat that sentinel as "not picked" so restored input-map targets remain pickable.
            if (pickedAt <= new DateTime(1900, 1, 1, 23, 59, 59))
                return false;

            return true;
        }

        private static string FormatDateTimeForLog(DateTime value)
        {
            return !HasValidPickedAt(value)
                ? "-"
                : value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        public static void UpsertInspection(string dieId, DieInspectionRecord record)
        {
            if (record == null) return;
            var die = GetOrCreateDieMaterial(dieId);
            var old = die.Inspections.FirstOrDefault(x => x.InspectionType == record.InspectionType);
            if (old != null) die.Inspections.Remove(old);
            record.UpdatedAt = DateTime.Now;
            if (record.CreatedAt == default(DateTime)) record.CreatedAt = DateTime.Now;
            die.Inspections.Add(record);
            die.UpdatedAt = DateTime.Now;
            InputWaferInspectionCsvSnapshotWriter.EnqueueInspection(
                "InspectionUpsert",
                State != null ? State.RecipeName : "",
                GetProductionLotId(),
                die,
                record);
            NotifyAndSave("UpsertInspection");
        }

        public static void RemoveInspection(string dieId, string inspectionType)
        {
            var die = State.Dies.FirstOrDefault(d => d.DieId == dieId);
            if (die == null || string.IsNullOrEmpty(inspectionType)) return;
            die.Inspections.RemoveAll(x => x.InspectionType == inspectionType);
            die.UpdatedAt = DateTime.Now;
            NotifyAndSave("RemoveInspection");
        }

        /// <summary>
        /// 작업자가 실제 Die가 Input Stage에 복귀한 것을 확인한 뒤 실행하는 수동 재픽업 복구입니다.
        /// 선택 Die만 대상으로 하며, 이전 Pick/검사/Output 수신 상태를 새 작업 전 상태로 되돌립니다.
        /// </summary>
        public static bool PrepareInputDiesForManualRepick(
            IEnumerable<string> dieIds,
            string reason,
            out string message)
        {
            message = string.Empty;
            try
            {
                List<string> ids = (dieIds ?? Enumerable.Empty<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (ids.Count == 0)
                {
                    message = "재픽업 복구할 Die가 없습니다.";
                    return false;
                }

                List<string> auditLines = new List<string>();
                lock (_stateSync)
                {
                    if (State == null || State.Dies == null)
                    {
                        message = "Material 상태가 초기화되지 않았습니다.";
                        return false;
                    }

                    List<DieMaterial> dies = new List<DieMaterial>();
                    foreach (string dieId in ids)
                    {
                        DieMaterial die = State.Dies.FirstOrDefault(d =>
                            d != null &&
                            string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                        if (die == null)
                        {
                            message = "Material Die 정보를 찾을 수 없습니다. die=" + dieId;
                            return false;
                        }

                        dies.Add(die);
                    }

                    foreach (DieMaterial die in dies)
                    {
                        MaterialLocation previousLocation = die.CurrentLocation;
                        if (auditLines.Count < 10)
                        {
                            auditLines.Add("die=" + die.DieId +
                                ", previousLocation=" + (previousLocation != null ? previousLocation.Kind.ToString() : "Unknown") +
                                ", previousResult=" + die.Result +
                                ", previousPickedAt=" + FormatDateTimeForLog(die.PickedAt));
                        }
                        PrepareInputDieForManualRepickNoLock(die);
                    }
                }

                NotifyAndSave("ManualInputDieRepick");
                string detail = string.Join(" | ", auditLines);
                if (ids.Count > auditLines.Count)
                    detail += " | additionalDies=" + (ids.Count - auditLines.Count);
                Log.Write("Main", "MATERIAL", "ManualInputDieRepick",
                    "Input Die를 수동 재픽업 대기로 복구했습니다. count=" + ids.Count +
                    ", reason=" + (reason ?? "") +
                    ", " + detail + " - Ok");
                message = "선택 Die " + ids.Count + "개를 재픽업 대기로 복구했습니다.";
                return true;
            }
            catch (Exception ex)
            {
                message = "재픽업 복구 실패: " + ex.Message;
                Log.Write("Main", "MATERIAL", "ManualInputDieRepick",
                    message + ", reason=" + (reason ?? "") + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static void ResetInputPickCompletionHistory(string dieId, string reason)
        {
            string message;
            if (!PrepareInputDiesForManualRepick(new[] { dieId }, reason, out message))
            {
                Log.Write("Main", "MATERIAL", "ResetInputPickCompletionHistory",
                    "Input pick completion history reset failed. die=" + (dieId ?? "") +
                    ", message=" + message + " - Failed");
            }
        }

        private static void PrepareInputDieForManualRepickNoLock(DieMaterial die)
        {
            if (die == null)
                return;

            die.IsInputTarget = true;
            die.Result = DieResult.Unknown;
            die.Input_BinCode = 0;
            die.Output_BinCode = 0;
            die.WaferID_Output = string.Empty;
            die.OutputWaferInstanceId = string.Empty;
            die.Bin_IndexX = -1;
            die.Bin_IndexY = -1;
            die.BinOffset = new VisionOffset();
            if (die.NgCodes != null)
                die.NgCodes.Clear();

            // 수동 재픽업은 실물이 Input Stage에 있음을 사용자가 확인한 경우에만 UI에서 호출한다.
            die.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
            die.ReservedPickerLocation = MaterialLocationKind.Unknown;
            die.ReservedPickerNo = -1;
            die.PickedPickerLocation = MaterialLocationKind.Unknown;
            die.PickedPickerNo = -1;
            die.PickedAt = DateTime.MinValue;

            if (die.Inspections != null)
            {
                die.Inspections.RemoveAll(x =>
                    x != null &&
                    (string.Equals(x.InspectionType, "InputPickVision", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.InspectionType, "PickUp", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.InspectionType, "Bottom", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.InspectionType, "Side0", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.InspectionType, "Side90", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.InspectionType, "OutputPlaceVision", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.InspectionType, "ManualPickerHeadEdit", StringComparison.OrdinalIgnoreCase)));
            }

            SyncActiveInputMapEntryNoLock(die.DieId, true, DieResult.Unknown, 0);
            ResetOutputReceiveSlotsForManualRepickNoLock(die.DieId);
            die.UpdatedAt = DateTime.Now;
        }

        private static void ResetOutputReceiveSlotsForManualRepickNoLock(string dieId)
        {
            if (string.IsNullOrWhiteSpace(dieId) || State == null || State.Wafers == null)
                return;

            foreach (WaferMaterial wafer in State.Wafers)
            {
                if (wafer == null || wafer.OutputReceiveSlots == null)
                    continue;

                bool touched = false;
                foreach (OutputReceiveSlotMaterial slot in wafer.OutputReceiveSlots)
                {
                    if (slot == null ||
                        !string.Equals(slot.DieUid ?? "", dieId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    slot.IsTarget = true;
                    slot.Result = DieResult.Unknown;
                    slot.BinCode = 0;
                    // Die가 Input Stage로 실제 복귀했으므로 이전 Output Bin 점유를 해제한다.
                    slot.DieUid = string.Empty;
                    slot.SourceDieUid = string.Empty;
                    slot.PlacementUid = string.Empty;
                    slot.LegacyDieUid = string.Empty;
                    slot.IdentityRecoveryNote = string.Empty;
                    slot.IsOutputInspectionDone = false;
                    slot.IsOutputInspectionOk = false;
                    slot.OutputInspectionOffsetX = 0.0;
                    slot.OutputInspectionOffsetY = 0.0;
                    slot.OutputInspectionOffsetT = 0.0;
                    slot.OutputInspectionRaw = string.Empty;
                    touched = true;
                }

                if (!touched)
                    continue;

                if (wafer.DieIds != null)
                    wafer.DieIds.RemoveAll(id => string.Equals(id ?? "", dieId, StringComparison.OrdinalIgnoreCase));
                wafer.OutputReceiveNextIndex = ResolveNextOutputReceiveIndex(wafer);
                // 이전 Finish 상태가 남아 있으면 IsOutputStageReceiveComplete가 즉시 true를 반환한다.
                // 방금 해제한 슬롯을 다시 사용할 수 있도록 명시적으로 Working으로 되돌린다.
                wafer.State = WaferMaterialState.Working;
                wafer.UpdatedAt = DateTime.Now;
            }
        }

        public static void NotifyAndSave(string reason)
        {
            TryNotifyAndSave(reason);
        }

        public static bool TryNotifyAndSave(string reason)
        {
            try
            {
                RequestStateChanged();
                RequestBackgroundSave(reason);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material state save request failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool TryFlushPendingSave(string reason)
        {
            try
            {
                bool shouldSave;
                lock (_saveRequestSync)
                {
                    shouldSave = _saveRequested || _saveWorkerRunning || !_lastSaveSucceeded || _lastSaveCompletedUtc == DateTime.MinValue;
                    _saveRequested = false;
                    _pendingSaveReason = reason ?? "";
                }

                RequestStateChanged();
                if (!shouldSave)
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateSave",
                        "Material state flush skipped because latest snapshot is already saved. reason=" +
                        (reason ?? "") + " - Ok");
                    return true;
                }

                return SaveCurrentSnapshot(reason);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material state flush failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void RequestBackgroundSave(string reason)
        {
            try
            {
                lock (_saveRequestSync)
                {
                    _pendingSaveReason = reason ?? "";
                    _saveRequested = true;

                    if (_saveWorkerRunning)
                        return;

                    _saveWorkerRunning = true;
                    Task.Run(() => MaterialSaveWorkerAsync());
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material save worker start failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static async Task MaterialSaveWorkerAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(MaterialSaveQuietMs).ConfigureAwait(false);

                    string reason;
                    int waitMs;
                    lock (_saveRequestSync)
                    {
                        if (!_saveRequested)
                        {
                            _saveWorkerRunning = false;
                            return;
                        }

                        reason = _pendingSaveReason;
                        waitMs = ResolveBackgroundSaveWaitMs(reason);
                        if (waitMs <= 0)
                        {
                            _saveRequested = false;
                            _pendingSaveReason = "";
                        }
                    }

                    if (waitMs > 0)
                    {
                        await Task.Delay(waitMs).ConfigureAwait(false);
                        continue;
                    }

                    SaveCurrentSnapshot(reason);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material save worker failed: " + ex.Message + " - Failed");
                lock (_saveRequestSync)
                {
                    _saveWorkerRunning = false;
                }
            }
            finally
            {
            }
        }

        private static int ResolveBackgroundSaveWaitMs(string reason)
        {
            try
            {
                if (IsImmediateBackgroundSaveReason(reason))
                    return 0;

                if (_lastSaveCompletedUtc == DateTime.MinValue)
                    return 0;

                double elapsedMs = (DateTime.UtcNow - _lastSaveCompletedUtc).TotalMilliseconds;
                if (elapsedMs >= MaterialSaveMinimumIntervalMs)
                    return 0;

                int waitMs = MaterialSaveMinimumIntervalMs - (int)elapsedMs;
                return waitMs < 0 ? 0 : waitMs;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private static bool IsImmediateBackgroundSaveReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return false;

            if (reason.IndexOf("Initialize", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("Manual", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("Mapping", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("MapTransfer", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("Dialog", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private static bool SaveCurrentSnapshot(string reason)
        {
            bool saved = false;
            try
            {
                // [정정 2026-07-27] 저장용 사본을 _stateSync 락 안에서 만든다.
                // 기존에는 Store.Save가 라이브 State를 락 없이 순회해, 저장 중 시퀀스가 Die를 추가하면
                // "컬렉션이 수정되었습니다" 예외로 저장이 실패할 수 있었다.
                // 무거운 직렬화/디스크 쓰기는 계속 락 밖에서 수행한다.
                MaterialSnapshot saveCopy;
                lock (_stateSync)
                {
                    State.SaveReason = reason ?? "";
                    State.SavedAt = DateTime.Now;
                    NormalizeSnapshotHeader(State);
                    saveCopy = MaterialSnapshotStore.CreateSaveCopy(State);
                }

                lock (_saveIoSync)
                {
                    saved = MaterialSnapshotStore.Save(saveCopy, true);
                }

                if (saved)
                {
                    lock (_saveRequestSync)
                    {
                        _lastSaveCompletedUtc = DateTime.UtcNow;
                        _lastSaveSucceeded = true;
                    }
                }
                else
                {
                    lock (_saveRequestSync)
                    {
                        _lastSaveSucceeded = false;
                    }
                }

                if (!saved)
                {
                    int waferCount = State.Wafers != null ? State.Wafers.Count : 0;
                    int dieCount = State.Dies != null ? State.Dies.Count : 0;
                    int cassetteCount = State.Cassettes != null ? State.Cassettes.Count : 0;
                    Log.Write("Main", "SYSTEM", "MaterialStateSave",
                        "Material state save failed. reason=" + State.SaveReason +
                        ", cassettes=" + cassetteCount +
                        ", wafers=" + waferCount +
                        ", dies=" + dieCount +
                        ", file=" + MaterialSnapshotStore.SnapshotPath + " - Failed");
                }

                return saved;
            }
            catch (Exception ex)
            {
                lock (_saveRequestSync)
                {
                    _lastSaveSucceeded = false;
                }
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material state save failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void RequestStateChanged()
        {
            try
            {
                int delayMs;
                lock (_stateChangedSync)
                {
                    if (_stateChangedQueued)
                        return;

                    TimeSpan elapsed = DateTime.Now - _lastStateChangedAt;
                    delayMs = elapsed.TotalMilliseconds >= MaterialStateChangedQuietMs
                        ? 0
                        : MaterialStateChangedQuietMs - (int)elapsed.TotalMilliseconds;
                    _stateChangedQueued = true;
                }

                Task.Run(async () =>
                {
                    try
                    {
                        if (delayMs > 0)
                            await Task.Delay(delayMs).ConfigureAwait(false);

                        Action<MaterialSnapshot> handler = StateChanged;
                        if (handler != null)
                            handler(State);
                    }
                    catch (Exception ex)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateChanged",
                            "Material state changed event failed: " + ex.Message + " - Failed");
                    }
                    finally
                    {
                        lock (_stateChangedSync)
                        {
                            _lastStateChangedAt = DateTime.Now;
                            _stateChangedQueued = false;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateChanged",
                    "Material state changed event queue failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void NormalizeSnapshotHeader(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                    return;

                if (string.IsNullOrWhiteSpace(snapshot.RecipeName))
                {
                    var project = RecipeStore.LoadLastOrDefault();
                    if (project != null)
                        snapshot.RecipeName = project.FileName ?? "";
                }

                snapshot.LotId = string.IsNullOrWhiteSpace(snapshot.LotId)
                    ? ""
                    : snapshot.LotId.Trim();
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material snapshot header normalize failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static WaferMaterial ResolveCassetteSlotWaferNoLock(
            CassetteSlotMaterial slot,
            out string reason)
        {
            reason = "";
            if (slot == null)
            {
                reason = "slot 데이터가 없습니다.";
                return null;
            }
            if (!slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
            {
                reason = "slot이 비어 있습니다.";
                return null;
            }

            string slotInstanceId = (slot.WaferInstanceId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(slotInstanceId))
            {
                WaferMaterial instanceWafer = State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    string.Equals(
                        w.WaferInstanceId ?? "",
                        slotInstanceId,
                        StringComparison.OrdinalIgnoreCase));
                if (instanceWafer == null)
                {
                    reason = "slot의 WaferInstanceId를 가리키는 Material이 없습니다. instance=" +
                             slotInstanceId + ", waferId=" + slot.WaferId;
                    return null;
                }
                if (!string.Equals(
                    instanceWafer.WaferId ?? "",
                    slot.WaferId ?? "",
                    StringComparison.OrdinalIgnoreCase))
                {
                    reason = "slot의 WaferId와 WaferInstanceId 대상이 다릅니다. slotWafer=" +
                             slot.WaferId + ", instanceWafer=" + instanceWafer.WaferId +
                             ", instance=" + slotInstanceId;
                    return null;
                }
                return instanceWafer;
            }

            List<WaferMaterial> legacyCandidates = State.Wafers
                .Where(w =>
                    w != null &&
                    string.Equals(
                        w.WaferId ?? "",
                        slot.WaferId ?? "",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (legacyCandidates.Count != 1)
            {
                reason = "V1 slot의 WaferId 대상 Material 수가 1개가 아닙니다. waferId=" +
                         slot.WaferId + ", candidates=" + legacyCandidates.Count;
                return null;
            }

            WaferMaterial legacyWafer = legacyCandidates[0];
            slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(legacyWafer);
            return legacyWafer;
        }

        private static void RemoveWaferFromCassetteSlot(WaferMaterial wafer)
        {
            if (wafer == null)
                return;

            string waferId = wafer.WaferId ?? "";
            string waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
            foreach (var cassette in State.Cassettes)
            {
                foreach (var slot in cassette.Slots)
                {
                    if (slot == null)
                        continue;

                    bool sameInstance;
                    if (!string.IsNullOrWhiteSpace(slot.WaferInstanceId))
                    {
                        sameInstance = string.Equals(
                            slot.WaferInstanceId,
                            waferInstanceId,
                            StringComparison.OrdinalIgnoreCase);
                    }
                    else if (string.Equals(
                        slot.WaferId ?? "",
                        waferId,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        string slotReason;
                        WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                        sameInstance = slotWafer != null &&
                                       string.Equals(
                                           EnsureWaferInstanceIdNoLock(slotWafer),
                                           waferInstanceId,
                                           StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        sameInstance = false;
                    }
                    if (sameInstance)
                    {
                        slot.WaferId = "";
                        slot.WaferInstanceId = "";
                        slot.HasWafer = false;
                    }
                }
            }
        }

        private static WaferMaterial FindOtherWaferAtLocation(
            WaferMaterial movingWafer,
            MaterialLocation targetLocation)
        {
            if (targetLocation == null || targetLocation.Kind == MaterialLocationKind.Unknown)
                return null;

            string movingInstanceId = EnsureWaferInstanceIdNoLock(movingWafer);
            return State.Wafers.FirstOrDefault(w =>
                w != null &&
                !ReferenceEquals(w, movingWafer) &&
                !string.Equals(
                    EnsureWaferInstanceIdNoLock(w),
                    movingInstanceId,
                    StringComparison.OrdinalIgnoreCase) &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                IsSameMaterialLocation(w.CurrentLocation, targetLocation));
        }

        private static bool IsSameMaterialLocation(MaterialLocation left, MaterialLocation right)
        {
            if (left == null || right == null || left.Kind != right.Kind)
                return false;

            if (left.Kind == MaterialLocationKind.InputCassette || left.Kind == MaterialLocationKind.OutputCassette)
                return left.CassetteRole == right.CassetteRole && left.SlotNumber == right.SlotNumber;

            if (left.Kind == MaterialLocationKind.PickerFront || left.Kind == MaterialLocationKind.PickerRear)
                return left.PickerNo == right.PickerNo;

            return true;
        }

        private static void ValidateCassetteMappingRequest(
            CassetteMaterialRole role,
            bool enabled,
            int slotCount,
            IReadOnlyList<bool> map,
            IReadOnlyList<double> slotPositions,
            bool recoverDetachedWorkingMaterial = false)
        {
            if (slotCount <= 0)
                throw new InvalidOperationException("Cassette SlotCount가 유효하지 않습니다. cassette=" + role + ", slotCount=" + slotCount);

            if (!enabled)
            {
                WaferMaterial disabledRoleWafer = State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    w.SourceCassetteRole == role &&
                    WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
                if (disabledRoleWafer != null)
                {
                    throw new InvalidOperationException("진행 또는 보관 중인 자재가 있어 cassette role을 비활성화할 수 없습니다. cassette=" + role +
                                                        ", wafer=" + disabledRoleWafer.WaferId +
                                                        ", state=" + disabledRoleWafer.State +
                                                        ", location=" + disabledRoleWafer.CurrentLocation);
                }
                return;
            }

            if (map == null || map.Count != slotCount)
                throw new InvalidOperationException("Cassette mapping 결과 길이가 SlotCount와 다릅니다. cassette=" + role +
                                                    ", slotCount=" + slotCount +
                                                    ", mapCount=" + (map != null ? map.Count : 0));

            if (slotPositions == null || slotPositions.Count != slotCount)
                throw new InvalidOperationException("Cassette slot 위치 수가 SlotCount와 다릅니다. cassette=" + role +
                                                    ", slotCount=" + slotCount +
                                                    ", positionCount=" + (slotPositions != null ? slotPositions.Count : 0));

            for (int i = 0; i < slotPositions.Count; i++)
            {
                if (double.IsNaN(slotPositions[i]) || double.IsInfinity(slotPositions[i]))
                    throw new InvalidOperationException("Cassette slot 물리 위치가 유효하지 않습니다. cassette=" + role +
                                                        ", slot=" + (i + 1) + ", position=" + slotPositions[i]);
            }

            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c.Role == role);
            if (cassette != null && cassette.Slots != null)
            {
                for (int i = 0; i < cassette.Slots.Count; i++)
                {
                    CassetteSlotMaterial slot = cassette.Slots[i];
                    if (slot == null)
                        throw new InvalidOperationException("기존 cassette slot 데이터가 없습니다. cassette=" + role + ", slot=" + (i + 1));

                    bool hasWaferId = !string.IsNullOrWhiteSpace(slot.WaferId);
                    if (slot.HasWafer != hasWaferId)
                    {
                        throw new InvalidOperationException("기존 cassette slot 점유 상태와 Wafer ID가 불일치합니다. cassette=" + role +
                                                            ", slot=" + (i + 1) +
                                                            ", hasWafer=" + slot.HasWafer +
                                                            ", waferId=" + slot.WaferId);
                    }

                    if (!slot.HasWafer)
                        continue;

                    string slotReason;
                    WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                    if (slotWafer == null)
                    {
                        throw new InvalidOperationException("기존 cassette 점유 slot의 Material 데이터가 없습니다. cassette=" + role +
                                                            ", slot=" + (i + 1) +
                                                            ", waferId=" + slot.WaferId +
                                                            ", reason=" + slotReason);
                    }

                    WaferMaterialState slotWaferState = WaferMaterialStateText.Normalize(slotWafer.State);
                    if (slotWaferState == WaferMaterialState.Empty ||
                        !IsWaferAtCassetteSlot(slotWafer, role, i) ||
                        slotWafer.SourceCassetteRole != role ||
                        slotWafer.SourceSlotNumber != i)
                    {
                        throw new InvalidOperationException("기존 cassette slot과 Material 위치/source 정보가 불일치합니다. cassette=" + role +
                                                            ", slot=" + (i + 1) +
                                                            ", waferId=" + slotWafer.WaferId +
                                                            ", state=" + slotWaferState +
                                                            ", sourceRole=" + slotWafer.SourceCassetteRole +
                                                            ", sourceSlot=" + (slotWafer.SourceSlotNumber + 1) +
                                                            ", location=" + slotWafer.CurrentLocation);
                    }
                }
            }

            MaterialLocationKind cassetteLocation = IsOutputCassetteRole(role)
                ? MaterialLocationKind.OutputCassette
                : MaterialLocationKind.InputCassette;
            foreach (WaferMaterial wafer in State.Wafers)
            {
                if (wafer == null || wafer.SourceCassetteRole != role)
                    continue;

                WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                if (state == WaferMaterialState.Empty)
                    continue;

                MaterialLocation location = wafer.CurrentLocation;
                bool atSourceCassette = location != null &&
                                        location.Kind == cassetteLocation &&
                                        location.CassetteRole == role &&
                                        location.SlotNumber == wafer.SourceSlotNumber;
                if (!atSourceCassette)
                {
                    if (recoverDetachedWorkingMaterial &&
                        IsDetachedWorkingOutputMaterial(wafer, role))
                    {
                        continue;
                    }

                    throw new InvalidOperationException("공정 중 자재가 cassette 밖에 있어 재매핑할 수 없습니다. cassette=" + role +
                                                        ", wafer=" + wafer.WaferId +
                                                        ", sourceSlot=" + (wafer.SourceSlotNumber + 1) +
                                                        ", state=" + state +
                                                        ", location=" + location);
                }

                if (wafer.SourceSlotNumber < 0 || wafer.SourceSlotNumber >= slotCount)
                {
                    throw new InvalidOperationException("기존 자재의 source slot이 새 SlotCount 범위를 벗어납니다. cassette=" + role +
                                                        ", wafer=" + wafer.WaferId +
                                                        ", sourceSlot=" + (wafer.SourceSlotNumber + 1) +
                                                        ", slotCount=" + slotCount);
                }

                if (!map[wafer.SourceSlotNumber] && state != WaferMaterialState.Finish)
                {
                    throw new InvalidOperationException("Mapping 센서는 빈 slot이지만 처리 전 자재 데이터가 남아 있습니다. cassette=" + role +
                                                        ", slot=" + (wafer.SourceSlotNumber + 1) +
                                                        ", wafer=" + wafer.WaferId +
                                                        ", state=" + state);
                }
            }
        }

        private static string ResolveOrCreateCassetteMappingLotId(
            string requestedLotId,
            params CassetteMaterialRole[] roles)
        {
            var candidates = new List<string>();
            if (State != null)
            {
                candidates.Add(State.LotId);
                var roleSet = new HashSet<CassetteMaterialRole>(roles ?? new CassetteMaterialRole[0]);
                foreach (CassetteMaterial cassette in State.Cassettes.Where(c => c != null && roleSet.Contains(c.Role)))
                {
                    candidates.Add(cassette.CassetteLotId);
                    if (cassette.Slots == null)
                        continue;

                    foreach (CassetteSlotMaterial slot in cassette.Slots)
                    {
                        if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                            continue;
                        string slotReason;
                        WaferMaterial wafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                        if (wafer == null)
                        {
                            throw new InvalidOperationException(
                                "Cassette LOT 결정 중 slot의 물리 Material pointer가 올바르지 않습니다. cassette=" +
                                cassette.Role +
                                ", slot=" + (slot.SlotNumber + 1) +
                                ", waferId=" + slot.WaferId +
                                ", reason=" + slotReason);
                        }
                        if (wafer != null && !IsFinishedOutputBinWafer(cassette.Role, wafer))
                            candidates.Add(wafer.CassetteLotId);
                    }
                }
            }

            return ResolveOrCreateCassetteLotId(requestedLotId, candidates, "cassette mapping");
        }

        private static string ResolveOrCreateCassetteLotId(
            string requestedLotId,
            CassetteMaterial cassette,
            WaferMaterial wafer)
        {
            var candidates = new List<string>();
            if (wafer != null)
                candidates.Add(wafer.CassetteLotId);
            if (cassette != null)
                candidates.Add(cassette.CassetteLotId);
            if (State != null)
                candidates.Add(State.LotId);

            return ResolveOrCreateCassetteLotId(requestedLotId, candidates, "cassette wafer");
        }

        private static string ResolveOrCreateCassetteLotId(
            string requestedLotId,
            IEnumerable<string> existingLotIds,
            string context)
        {
            var candidates = new List<string> { requestedLotId };
            candidates.AddRange(existingLotIds ?? Enumerable.Empty<string>());
            List<string> existing = candidates
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (existing.Count == 1)
                return existing[0];
            if (existing.Count > 1)
            {
                throw new InvalidOperationException(
                    "카세트 LOT ID 후보가 서로 달라 LOT ID를 결정할 수 없습니다. context=" + context +
                    ", requestedLotId=" + (requestedLotId ?? "") +
                    ", lotIds=" + string.Join(",", existing.ToArray()));
            }

            string generatedLotId = "AUTOLOT-" +
                                    DateTime.Now.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture) + "-" +
                                    Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            Log.Write("Main", "SYSTEM", "MaterialLotContext",
                "카세트 LOT ID가 없어 임시 LOT ID를 생성했습니다. lotId=" + generatedLotId + " - Check");
            return generatedLotId;
        }

        private static void UpdateCassetteMapping(
            CassetteMaterialRole role,
            bool enabled,
            int slotCount,
            IReadOnlyList<bool> map,
            IReadOnlyList<double> slotPositions,
            string cassetteLotId,
            string tapeFrameSpecName,
            bool preserveExistingMaterial = true)
        {
            var cassette = EnsureCassette(role, slotCount);
            string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, role);
            cassette.IsEnabled = enabled;
            cassette.IsPresent = enabled;
            cassette.IsMapped = enabled && map != null;
            cassette.CassetteLotId = resolvedLotId;
            cassette.LastScanTime = enabled && map != null ? DateTime.Now : cassette.LastScanTime;
            cassette.SlotCount = slotCount;
            cassette.EnsureSlots();

            for (int i = 0; i < cassette.Slots.Count; i++)
            {
                CassetteSlotMaterial slot = cassette.Slots[i];
                bool mappedOccupied = enabled && map != null && i < map.Count && map[i];
                if (mappedOccupied)
                    continue;

                // 빈 slot으로 다시 매핑할 때 cassette 위치에 남은 Material도 함께 정리한다.
                // Stage/Feeder 등 cassette 밖으로 이동한 Material은 이 조건에 포함하지 않는다.
                List<WaferMaterial> removedWafers = State.Wafers
                    .Where(w => w != null && IsWaferAtCassetteSlot(w, role, i))
                    .ToList();
                foreach (WaferMaterial removedWafer in removedWafers)
                {
                    if (IsFinishedOutputBinWafer(role, removedWafer))
                    {
                        RemoveFinishedOutputBinWaferForNewCassetteMapping(removedWafer);
                    }
                    else
                    {
                        removedWafer.CurrentLocation = MaterialLocation.Unknown();
                        removedWafer.State = WaferMaterialState.Empty;
                        removedWafer.UpdatedAt = DateTime.Now;
                    }
                }

                if (slot != null)
                {
                    slot.WaferId = "";
                    slot.WaferInstanceId = "";
                    slot.HasWafer = false;
                }
            }

            if (!enabled || map == null)
                return;

            for (int i = 0; i < slotCount; i++)
            {
                if (!map[i])
                    continue;

                CassetteSlotMaterial slot = cassette.Slots[i];
                string slotResolveReason = "";
                WaferMaterial wafer = slot != null && slot.HasWafer && !string.IsNullOrWhiteSpace(slot.WaferId)
                    ? ResolveCassetteSlotWaferNoLock(slot, out slotResolveReason)
                    : null;
                if (preserveExistingMaterial &&
                    slot != null &&
                    slot.HasWafer &&
                    !string.IsNullOrWhiteSpace(slot.WaferId) &&
                    wafer == null)
                {
                    throw new InvalidOperationException(
                        "보존 Cassette Mapping의 slot Material pointer가 올바르지 않습니다. role=" +
                        role + ", slot=" + (i + 1) +
                        ", wafer=" + slot.WaferId +
                        ", reason=" + slotResolveReason);
                }
                bool preserveExisting = preserveExistingMaterial &&
                                        wafer != null &&
                                        IsWaferAtCassetteSlot(wafer, role, i) &&
                                        WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty &&
                                        !IsFinishedOutputBinWafer(role, wafer);

                if (IsFinishedOutputBinWafer(role, wafer))
                {
                    RemoveFinishedOutputBinWaferForNewCassetteMapping(wafer);
                    wafer = null;
                }

                if (!preserveExisting)
                {
                    string waferId = BuildGeneratedWaferId(role, i);
                    List<WaferMaterial> staleSlotWafers = State.Wafers
                        .Where(w =>
                            w != null &&
                            !string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase) &&
                            IsWaferAtCassetteSlot(w, role, i))
                        .ToList();
                    foreach (WaferMaterial staleWafer in staleSlotWafers)
                    {
                        if (role == CassetteMaterialRole.Input1 || role == CassetteMaterialRole.Input2)
                        {
                            ResetInputStageWaferProcessingStateNoLock(
                                staleWafer,
                                "ProcessTestMapping.ReplaceExistingWafer");
                        }

                        staleWafer.CurrentLocation = MaterialLocation.Unknown();
                        staleWafer.State = WaferMaterialState.Empty;
                        staleWafer.UpdatedAt = DateTime.Now;
                    }

                    wafer = State.Wafers.FirstOrDefault(w => string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase));
                    if (wafer == null)
                    {
                        wafer = new WaferMaterial
                        {
                            WaferId = waferId,
                            CreatedAt = DateTime.Now
                        };
                        State.Wafers.Add(wafer);
                    }

                    // Slot 고정 WaferId 재사용 시 새 physical wafer가 이전 Align/Mapping/Review 승인을
                    // 상속하지 않도록 비보존 매핑 경로에서는 항상 처리 상태를 초기화한다.
                    ResetInputStageWaferProcessingStateNoLock(wafer, "CassetteMapping.NewWafer");

                    wafer.CassetteLotId = resolvedLotId;
                    wafer.SourceCassetteId = cassette.CassetteId;
                    wafer.SourceCassetteRole = role;
                    wafer.SourceSlotNumber = i;
                    ApplyWaferCassettePosition(wafer, ResolveSlotPosition(slotPositions, i));
                    wafer.CurrentLocation = MaterialLocation.Cassette(
                        role == CassetteMaterialRole.Input1 || role == CassetteMaterialRole.Input2
                            ? MaterialLocationKind.InputCassette
                            : MaterialLocationKind.OutputCassette,
                        role,
                        i);
                    wafer.State = WaferMaterialState.Ready;
                    wafer.TapeFrameSpecName = tapeFrameSpecName ?? "";
                }
                else
                {
                    wafer.CurrentCassetteSlotPosition = ResolveSlotPosition(slotPositions, i);
                }
                wafer.CassetteLotId = resolvedLotId;
                if (IsOutputCassetteRole(role))
                {
                    wafer.OutputCassetteId = cassette.CassetteId;
                    wafer.OutputCassetteRole = role;
                    wafer.OutputSlotNumber = i;
                }
                wafer.UpdatedAt = DateTime.Now;

                slot.WaferId = wafer.WaferId;
                slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                slot.HasWafer = true;
            }
        }

        /// <summary>
        /// 새 physical wafer 투입(비보존 매핑) 시 이전 Align/T Align/Mapping/Review 승인과
        /// Pick 진행 정보를 초기화하는 중앙 helper입니다. Slot 고정 WaferId 재사용 상속을 차단합니다.
        /// 동일 wafer의 Stop→Start, snapshot 복구, 단순 재스캔(보존 매핑)에서는 호출하지 않습니다.
        /// </summary>
        private static void ResetInputStageWaferProcessingStateNoLock(WaferMaterial wafer, string cause)
        {
            if (wafer == null)
                return;

            bool hadState = wafer.HasInputStageAlignResult ||
                            wafer.HasInputStageThetaAlignResult ||
                            wafer.HasInputStageDieMappingResult ||
                            wafer.HasInputStageRunReviewApproval ||
                            (wafer.DieIds != null && wafer.DieIds.Count > 0);
            string previousInstanceId = EnsureWaferInstanceIdNoLock(wafer);

            // 이전 wafer의 Die 진행 정보(Result/Picked/예약)를 함께 제거한다.
            string waferId = wafer.WaferId ?? string.Empty;
            State.Dies.RemoveAll(d =>
                IsInputOnlyDieForWaferInstanceNoLock(d, waferId, previousInstanceId));

            ClearInputStageWaferProcessingFieldsNoLock(wafer);
            wafer.DieIds = new List<string>();
            wafer.InputStageProcessingGeneration = wafer.InputStageProcessingGeneration + 1;
            wafer.WaferInstanceId = CreateWaferInstanceId();
            InputStageHybridResultSession.Clear();

            if (hadState)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "New physical wafer processing state reset. wafer=" + (wafer.WaferId ?? "") +
                    ", previousInstance=" + previousInstanceId +
                    ", currentInstance=" + wafer.WaferInstanceId +
                    ", generation=" + wafer.InputStageProcessingGeneration +
                    ", cause=" + (cause ?? "") + " - Ok");
            }
        }

        /// <summary>
        /// Input Stage의 Align/Mapping/Review 결과 필드만 초기화한다.
        /// Die 삭제 범위는 호출 목적마다 다르므로 이 helper에서는 Die 목록을 직접 지우지 않는다.
        /// </summary>
        private static void ClearInputStageWaferProcessingFieldsNoLock(WaferMaterial wafer)
        {
            if (wafer == null)
                return;

            wafer.HasInputStageAlignResult = false;
            wafer.InputStageAlignResultMode = string.Empty;
            wafer.InputStageAlignResultRunId = string.Empty;
            wafer.InputStageAlignOriginX = 0.0;
            wafer.InputStageAlignOriginY = 0.0;
            wafer.InputStageAlignPitchX = 0.0;
            wafer.InputStageAlignPitchY = 0.0;
            wafer.InputStageDieSizeX = 0.0;
            wafer.InputStageDieSizeY = 0.0;
            wafer.InputStageOuterDiameterMm = 0.0;
            wafer.InputStageAlignOffsetX = 0.0;
            wafer.InputStageAlignOffsetY = 0.0;
            wafer.HasInputStageThetaAlignResult = false;
            wafer.InputStageAlignReferenceT = 0.0;
            wafer.InputStageAlignCorrectedT = 0.0;
            wafer.InputStageAlignOffsetT = 0.0;
            wafer.HasInputStageDieMappingResult = false;
            wafer.InputStageDieMappingResultMode = string.Empty;
            wafer.InputStageDieMappingAlignRunId = string.Empty;
            wafer.InputStageDieMappingOffsetX = 0.0;
            wafer.InputStageDieMappingOffsetY = 0.0;
            wafer.HasInputStageDieMappingOrigin = false;
            wafer.InputStageDieMappingOriginX = 0.0;
            wafer.InputStageDieMappingOriginY = 0.0;
            wafer.HasInputStageDieMappingThetaSnapshot = false;
            wafer.InputStageDieMappingCorrectedT = 0.0;
            wafer.InputStageDieMappingInvalidatedByAlignChange = false;
            wafer.InputMapApprovalHashAtMapping = string.Empty;
            wafer.DieMapFrameObjId = string.Empty;
            wafer.HasInputStageRunReviewApproval = false;
            wafer.InputStageRunReviewStartDieIndex = 0;
            wafer.InputStageRunReviewStartDieUid = string.Empty;
            wafer.InputStageRunReviewOrderedDieIds = new List<string>();
            wafer.InputStageRunReviewMappingRevision = string.Empty;
        }

        private static bool IsWaferAtCassetteSlot(WaferMaterial wafer, CassetteMaterialRole role, int slotNumber)
        {
            if (wafer == null || wafer.CurrentLocation == null)
                return false;

            MaterialLocationKind expectedKind = IsOutputCassetteRole(role)
                ? MaterialLocationKind.OutputCassette
                : MaterialLocationKind.InputCassette;
            return wafer.CurrentLocation.Kind == expectedKind &&
                   wafer.CurrentLocation.CassetteRole == role &&
                   wafer.CurrentLocation.SlotNumber == slotNumber;
        }

        private static bool IsFinishedOutputBinWafer(CassetteMaterialRole role, WaferMaterial wafer)
        {
            return IsOutputCassetteRole(role) &&
                   wafer != null &&
                   WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Finish;
        }

        private static bool IsOutputCassetteRole(CassetteMaterialRole role)
        {
            return role == CassetteMaterialRole.Good1 ||
                   role == CassetteMaterialRole.Good2 ||
                   role == CassetteMaterialRole.Ng1;
        }

        private static bool IsDetachedWorkingOutputMaterial(
            WaferMaterial wafer,
            CassetteMaterialRole role)
        {
            if (wafer == null ||
                !IsOutputCassetteRole(role) ||
                wafer.SourceCassetteRole != role ||
                WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Working)
            {
                return false;
            }

            return wafer.CurrentLocation == null ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.Unknown;
        }

        private static void RemoveDetachedWorkingOutputMaterialForMapping(
            CassetteMaterialRole role)
        {
            List<WaferMaterial> staleWafers = State.Wafers
                .Where(w => IsDetachedWorkingOutputMaterial(w, role))
                .ToList();

            foreach (WaferMaterial wafer in staleWafers)
            {
                string waferId = wafer.WaferId ?? string.Empty;
                int sourceSlot = wafer.SourceSlotNumber;
                int removedDieCount = State.Dies.RemoveAll(d =>
                    IsDieRelatedToWaferInstanceNoLock(d, wafer));

                RemoveWaferFromCassetteSlot(wafer);
                State.Wafers.Remove(wafer);

                Log.Write(
                    "Main",
                    "SYSTEM",
                    "OutputCassetteMappingRecovery",
                    "Detached Working/Unknown material removed after successful physical scan. cassette=" + role +
                    ", wafer=" + waferId +
                    ", sourceSlot=" + (sourceSlot + 1) +
                    ", removedDies=" + removedDieCount +
                    " - Check");
            }
        }

        private static void RemoveFinishedOutputBinWaferForNewCassetteMapping(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            string waferId = wafer.WaferId;
            State.Dies.RemoveAll(d =>
                IsDieRelatedToWaferInstanceNoLock(d, wafer));
            State.Wafers.Remove(wafer);
        }

        private static string ResolveDefaultTapeFrameSpecName(int inchSelect)
        {
            double targetDiameter = inchSelect == 1 ? 300 : 200;
            var spec = MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null
                ? MaterialSpecs.Data.Frames.FirstOrDefault(f => Math.Abs(f.OuterDiameterMm - targetDiameter) < 0.001)
                : null;
            return spec != null ? spec.Name : (inchSelect == 1 ? "12inch_50x50" : "8inch_5x5");
        }

        private static void EnsureTapeFrameSpecFromRecipe(RecipeProject project, string specName)
        {
            if (project == null || project.Frame == null || string.IsNullOrWhiteSpace(specName))
                return;

            EnsureTapeFrameSpecFromFrame(project, project.Frame, specName, "");
        }

        private static void EnsureTapeFrameSpecFromFrame(RecipeProject project, TapeFrameSubset frame, string specName, string mapFileName)
        {
            if (project == null || frame == null || string.IsNullOrWhiteSpace(specName))
                return;

            if (MaterialSpecs.Data == null)
                return;

            EnsureDieSpecFromRecipe(project, project.Die != null ? project.Die.DieSpecName : "");
            bool externalMap = RecipeDieMapResolver.IsExternalFrame(frame);
            int dieMapX = externalMap
                ? Math.Max(1, frame.DieMapX)
                : ResolvePitchBasedGridCount(
                    frame.OuterDiameterMm,
                    DieMapGenerator.CalculateCenterStep(frame.DieSizeX, frame.PitchX),
                    frame.DieSizeX,
                    frame.DieMapX);
            int dieMapY = externalMap
                ? Math.Max(1, frame.DieMapY)
                : ResolvePitchBasedGridCount(
                    frame.OuterDiameterMm,
                    DieMapGenerator.CalculateCenterStep(frame.DieSizeY, frame.PitchY),
                    frame.DieSizeY,
                    frame.DieMapY);

            MaterialSpecs.UpsertFrame(
                specName,
                dieMapX,
                dieMapY,
                frame.PitchX,
                frame.PitchY,
                frame.DieSizeX,
                frame.DieSizeY,
                frame.OuterDiameterMm,
                frame.EdgeSkipMode,
                frame.SideEdgeSkip,
                frame.TopBottomEdgeSkip,
                frame.SideEdgeSkipMm,
                frame.TopBottomEdgeSkipMm,
                mapFileName,
                project.Die != null ? project.Die.DieSpecName ?? "" : "");
        }

        private static int ResolvePitchBasedGridCount(
            double outerDiameterMm,
            double centerStepMm,
            double dieSizeMm,
            int fallback)
        {
            if (outerDiameterMm <= 0.0 || centerStepMm <= 0.0 || dieSizeMm <= 0.0)
                return Math.Max(1, fallback);

            return DieMapGenerator.CalculateWaferGridCount(outerDiameterMm, centerStepMm, dieSizeMm);
        }

        private static void EnsureDieSpecFromRecipe(RecipeProject project, string specName)
        {
            if (project == null || project.Die == null)
                return;

            if (MaterialSpecs.Data == null)
                return;

            var die = project.Die;
            MaterialSpecs.UpsertDie(
                string.IsNullOrWhiteSpace(specName) ? die.DieSpecName : specName,
                die.WidthMm,
                die.HeightMm,
                die.ThicknessMm,
                die.ChipLowerSpecLimitWidth,
                die.ChipUpperSpecLimitWidth,
                die.ChipLowerSpecLimitHeight,
                die.ChipUpperSpecLimitHeight,
                die.ChippingDepthMax,
                die.ChippingLengthMax,
                die.ForeignSizeMax);
        }

        private static string ResolveCassetteTapeFrameSpecName(CassetteMaterial cassette)
        {
            if (cassette == null || cassette.Slots == null)
                return ResolveRecipeTapeFrameSpecName(0);

            foreach (var slot in cassette.Slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.WaferId))
                    continue;

                var wafer = State.Wafers.FirstOrDefault(w => w.WaferId == slot.WaferId);
                if (wafer != null && !string.IsNullOrWhiteSpace(wafer.TapeFrameSpecName))
                    return wafer.TapeFrameSpecName;
            }

            return ResolveRecipeTapeFrameSpecName(0);
        }

        private static void ApplyWaferCassetteLocation(WaferMaterial wafer, CassetteMaterial cassette, int slotNumber, string cassetteLotId, double slotPosition = double.NaN)
        {
            if (wafer == null || cassette == null) return;

            string resolvedLotId = ResolveOrCreateCassetteLotId(cassetteLotId, cassette, wafer);
            wafer.CassetteLotId = resolvedLotId;
            cassette.CassetteLotId = resolvedLotId;

            wafer.SourceCassetteId = cassette.CassetteId;
            wafer.SourceCassetteRole = cassette.Role;
            wafer.SourceSlotNumber = slotNumber;
            ApplyWaferCassettePosition(wafer, slotPosition);
            // 기존 조건: Kind를 InputCassette로 고정 — Output Role(Good1/Good2/Ng1)에서 호출되면
            //           CurrentLocation.Kind가 InputCassette로 오염되어 위치 판정이 어긋났다.
            // 현재 기준: Role에 따라 Input/Output Cassette Kind를 구분한다(PutWaferInCassette와 동일 규칙).
            wafer.CurrentLocation = MaterialLocation.Cassette(
                IsOutputCassetteRole(cassette.Role) ? MaterialLocationKind.OutputCassette : MaterialLocationKind.InputCassette,
                cassette.Role,
                slotNumber);
            wafer.State = wafer.State == WaferMaterialState.Empty
                ? WaferMaterialState.Ready
                : WaferMaterialStateText.Normalize(wafer.State);
            wafer.UpdatedAt = DateTime.Now;
        }

        public static bool TryGetCassetteSlotPosition(CassetteMaterialRole cassetteRole, int slotNumber, out double slotPosition)
        {
            slotPosition = double.NaN;
            try
            {
                var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
                if (cassette == null || !cassette.IsMapped || cassette.Slots == null)
                    return false;
                if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                    return false;

                var slot = cassette.Slots[slotNumber];
                string slotReason;
                var wafer = slot != null && slot.HasWafer && !string.IsNullOrWhiteSpace(slot.WaferId)
                    ? ResolveCassetteSlotWaferNoLock(slot, out slotReason)
                    : null;
                if (wafer == null || double.IsNaN(wafer.CurrentCassetteSlotPosition))
                    return false;

                slotPosition = wafer.CurrentCassetteSlotPosition;
                return true;
            }
            catch
            {
                slotPosition = double.NaN;
                return false;
            }
            finally
            {
            }
        }

        private static void ApplyWaferCassettePosition(WaferMaterial wafer, double slotPosition)
        {
            if (wafer == null || double.IsNaN(slotPosition))
                return;

            wafer.SourceCassetteSlotPosition = slotPosition;
            wafer.CurrentCassetteSlotPosition = slotPosition;
        }

        // 공정 테스트 Data용 슬롯별 카세트 포지션 계산.
        // 티칭 미완 등으로 계산이 실패하면 포지션 없이(null) 생성하도록 하고 사유를 로그에 남긴다.
        private static double[] BuildProcessTestSlotPositions(int slotCount, Func<int, double> calculate, string label)
        {
            if (slotCount <= 0 || calculate == null)
                return null;

            try
            {
                var positions = new double[slotCount];
                for (int i = 0; i < slotCount; i++)
                    positions[i] = calculate(i);
                return positions;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "공정 테스트 슬롯 포지션 계산에 실패해 포지션 없이 생성합니다. cassette=" + label +
                    ", error=" + ex.Message + " - Check");
                return null;
            }
            finally
            {
            }
        }

        private static double ResolveSlotPosition(IReadOnlyList<double> slotPositions, int slotNumber)
        {
            if (slotPositions == null || slotNumber < 0 || slotNumber >= slotPositions.Count)
                return double.NaN;

            return slotPositions[slotNumber];
        }

        private static CassetteMaterial EnsureCassette(CassetteMaterialRole role, int slotCount)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == role);
            if (cassette != null)
            {
                cassette.CassetteId = string.IsNullOrEmpty(cassette.CassetteId) ? role.ToString() : cassette.CassetteId;
                cassette.Level = role == CassetteMaterialRole.Input2 || role == CassetteMaterialRole.Good2 ? 2 : 1;
                cassette.SlotCount = slotCount;
                cassette.EnsureSlots();
                return cassette;
            }

            cassette = new CassetteMaterial
            {
                CassetteId = role.ToString(),
                Role = role,
                Level = role == CassetteMaterialRole.Input2 || role == CassetteMaterialRole.Good2 ? 2 : 1,
                SlotCount = slotCount
            };
            cassette.EnsureSlots();
            State.Cassettes.Add(cassette);
            return cassette;
        }

        private static string BuildGeneratedWaferId(CassetteMaterialRole role, int slotNumber)
        {
            return role.ToString().ToUpperInvariant() + "-S" + (slotNumber + 1).ToString("00");
        }
    }
}
