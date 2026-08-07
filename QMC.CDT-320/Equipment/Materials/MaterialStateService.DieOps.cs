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
    // MaterialStateService partial: Wafer/Die CRUD, 수동 Die 상태, Picker die 조작 (원본 476-1337)
    public static partial class MaterialStateService
    {
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
            // 공백 id는 인덱스에 등록되지 않아 매 호출 신규 생성으로 이어지므로 GUID로 대체한다.
            if (string.IsNullOrWhiteSpace(dieId))
                dieId = Guid.NewGuid().ToString("N").Substring(0, 12);

            // [2026-08-05] 기존에는 락 없이 State.Dies를 열거/추가했다 — 게이트 스레드의 열거와
            // 경합하면 "컬렉션이 수정되었습니다"가 가능했던 경로다. 인덱스 도입과 함께 락으로 보호한다.
            lock (_stateSync)
            {
                List<DieMaterial> candidates;
                if (!GetDieByIdIndexNoLock().TryGetValue(dieId, out candidates))
                    candidates = null;

                if (candidates != null && candidates.Count > 1)
                {
                    throw new InvalidOperationException(
                        "같은 Die ID의 물리 Material이 둘 이상이므로 임의로 선택할 수 없습니다. dieId=" +
                        dieId);
                }
                if (candidates != null && candidates.Count == 1)
                    return candidates[0];

                DieMaterial die = new DieMaterial
                {
                    DieId = dieId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                State.Dies.Add(die);
                RegisterDieInIndexNoLock(die);
                NotifyAndSave("CreateDie");
                return die;
            }
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
                        InvalidateDieByIdIndexNoLock();

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
                        InvalidateDieByIdIndexNoLock();

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
            bool hasOutputParent =
                !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) ||
                !string.IsNullOrWhiteSpace(die.WaferID_Output);
            bool hasActiveReservation =
                die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                die.ReservedPickerLocation == MaterialLocationKind.PickerRear ||
                die.ReservedPickerNo > 0;
            if (!sameInputInstance || hasOutputParent || hasActiveReservation)
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

                    return FindDieByIdNoLock(dieId);
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

            // [리뷰 반영 2026-08-05] 수동 die 상태 변경(IsTarget/Result)은 wafer 키 필드를 바꾸지
            // 않으므로 캐시를 명시 무효화한다 — 승인 목록 밖 die 활성화의 fail-closed 차단과
            // 재활성 die의 픽업 재개가 다음 게이트에서 즉시 재평가되게 한다.
            InvalidateInputPickContextCacheNoLock();
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

    }
}
