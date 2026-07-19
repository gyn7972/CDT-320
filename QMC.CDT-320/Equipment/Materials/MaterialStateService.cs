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

            var wafer = State.Wafers.FirstOrDefault(w => w.WaferId == waferId);
            if (wafer != null) return wafer;

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

            var die = State.Dies.FirstOrDefault(d => d.DieId == dieId);
            if (die != null) return die;

            die = new DieMaterial
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
                    int removed = State.Dies.RemoveAll(d =>
                        d != null &&
                        string.Equals(d.WaferID_Input, waferId, StringComparison.OrdinalIgnoreCase));

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

        public static int ClearStaleInputDieMaterialsForWafer(string waferId, ICollection<string> activeDieIds, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(waferId))
                    return 0;

                lock (_stateSync)
                {
                    int removed = State.Dies.RemoveAll(d =>
                        d != null &&
                        string.Equals(d.WaferID_Input, waferId, StringComparison.OrdinalIgnoreCase) &&
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
                    "Clear stale input die materials failed. wafer=" + waferId +
                    ", reason=" + (reason ?? "") +
                    ", error=" + ex.Message + " - Failed");
                return 0;
            }
            finally
            {
            }
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
                    DieMaterial die = State.Dies.FirstOrDefault(d =>
                        d != null &&
                        string.Equals(d.DieId, dieId, StringComparison.OrdinalIgnoreCase));
                    if (die == null)
                    {
                        message = "Die 정보를 찾을 수 없습니다. dieId=" + dieId;
                        return false;
                    }

                    ApplyManualDieStateNoLock(die, isInputTarget, result, binCode, ngCode);
                    SyncManualDieStateTargetsNoLock(die, isInputTarget, result, binCode);
                }

                NotifyAndSave("ManualDieStateSync:" + dieId);
                Log.Write("Main", "MATERIAL", "ManualDieStateSync",
                    "Manual die state synchronized. dieId=" + dieId +
                    ", result=" + result +
                    ", isInputTarget=" + isInputTarget +
                    ", binCode=" + binCode +
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
                        State != null ? State.LotId : "",
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
                    SyncManualDieStateTargetsNoLock(die, isInputTarget, result, binCode);
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
            int binCode)
        {
            if (die == null)
                return;

            int normalizedBinCode = ResolveManualBinCode(result, binCode);
            SyncActiveInputMapEntryNoLock(die.DieId, isInputTarget, result, normalizedBinCode);
            SyncOutputReceiveSlotsNoLock(die.DieId, isInputTarget, result, normalizedBinCode);
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

        private static void SyncOutputReceiveSlotsNoLock(
            string dieId,
            bool isTarget,
            DieResult result,
            int binCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dieId) || State.Wafers == null)
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

                        slot.IsTarget = isTarget;
                        slot.Result = isTarget ? result : DieResult.Unknown;
                        slot.BinCode = isTarget ? ResolveManualBinCode(result, binCode) : 0;
                        touched = true;
                    }

                    if (touched)
                    {
                        wafer.OutputReceiveNextIndex = ResolveNextOutputReceiveIndex(wafer);
                        wafer.State = IsOutputStageReceiveComplete(wafer)
                            ? WaferMaterialState.Finish
                            : WaferMaterialState.Working;
                        wafer.UpdatedAt = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "MATERIAL", "ManualDieStateSync",
                    "Output receive slot sync failed. dieId=" + dieId +
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

                State.LotId = resolvedLotId;
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

                State.LotId = resolvedLotId;
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
            string tapeFrameSpecName)
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
                    ValidateCassetteMappingRequest(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions);
                    ValidateCassetteMappingRequest(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions);
                }

                if (updateNg)
                    ValidateCassetteMappingRequest(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions);

                if (updateGood)
                {
                    UpdateCassetteMapping(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions, resolvedLotId, tapeFrameSpecName);
                    UpdateCassetteMapping(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions, resolvedLotId, tapeFrameSpecName);
                }

                if (updateNg)
                    UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions, resolvedLotId, tapeFrameSpecName);

                State.LotId = resolvedLotId;
            }
            NotifyAndSave("OutputCassetteMappingSelective");
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
                    string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    string lotId = "TEST-LOT-" + timestamp;
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
            message = string.Empty;
            try
            {
                lock (_stateSync)
                {
                    RecipeProject project = RecipeStore.LoadLastOrDefault();
                    string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    string lotId = "TEST-LOT-" + timestamp;
                    string inputTapeFrameSpecName = ResolveInputTapeFrameSpecName(0);
                    string outputTapeFrameSpecName = ResolveRecipeTapeFrameSpecName(0);

                    int inputSlotCount = ResolveProcessTestSlotCount(CassetteMaterialRole.Input1);
                    int outputSlotCount = ResolveProcessTestSlotCount(CassetteMaterialRole.Good1);
                    bool useInput2 = IsCassetteCurrentlyEnabled(CassetteMaterialRole.Input2);
                    bool useGood2 = IsCassetteCurrentlyEnabled(CassetteMaterialRole.Good2);

                    ClearActiveProcessLocationsNoLock();

                    UpdateCassetteMapping(CassetteMaterialRole.Input1, true, inputSlotCount, BuildProcessTestSlotMap(inputSlotCount, 2), null, lotId, inputTapeFrameSpecName);
                    UpdateCassetteMapping(CassetteMaterialRole.Input2, useInput2, inputSlotCount, useInput2 ? BuildProcessTestSlotMap(inputSlotCount, 1) : null, null, lotId, inputTapeFrameSpecName);
                    UpdateCassetteMapping(CassetteMaterialRole.Good1, true, outputSlotCount, BuildProcessTestSlotMap(outputSlotCount, 2), null, lotId, outputTapeFrameSpecName);
                    UpdateCassetteMapping(CassetteMaterialRole.Good2, useGood2, outputSlotCount, useGood2 ? BuildProcessTestSlotMap(outputSlotCount, 1) : null, null, lotId, outputTapeFrameSpecName);
                    UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, outputSlotCount, BuildProcessTestSlotMap(outputSlotCount, 2), null, lotId, outputTapeFrameSpecName);

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
                    inputStageWafer.UpdatedAt = DateTime.Now;
                    BindProcessTestStageWaferToCassetteSlotNoLock(
                        CassetteMaterialRole.Input1,
                        0,
                        inputStageWafer,
                        MaterialLocationKind.InputStage,
                        WaferMaterialState.Working,
                        lotId,
                        inputTapeFrameSpecName);

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
                        outputTapeFrameSpecName);
                    BindProcessTestStageWaferToCassetteSlotNoLock(
                        CassetteMaterialRole.Ng1,
                        0,
                        ngStageWafer,
                        MaterialLocationKind.OutputStageNg,
                        WaferMaterialState.Working,
                        lotId,
                        outputTapeFrameSpecName);

                    State.LotId = lotId;
                    State.RecipeName = project != null ? project.FileName ?? "" : State.RecipeName;

                    NotifyAndSave("CreateProcessTestDataSet");
                    TryFlushPendingSave("CreateProcessTestDataSet");

                    message = "공정 테스트 Data 생성 완료. InputStage die=" + inputTargetCount +
                              ", GoodStage target=" + (goodStageWafer != null ? goodStageWafer.OutputReceiveTotalCount : 0) +
                              ", NgStage target=" + (ngStageWafer != null ? ngStageWafer.OutputReceiveTotalCount : 0) +
                              ", lot=" + lotId;
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
            PutWaferInCassette(waferId, cassetteRole, slotNumber, cassetteLotId, slotPosition, false, WaferMaterialState.Ready);
        }

        public static void PutWaferInCassette(
            string waferId,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition,
            WaferMaterialState state)
        {
            PutWaferInCassette(waferId, cassetteRole, slotNumber, cassetteLotId, slotPosition, true, state);
        }

        private static void PutWaferInCassette(
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

                var wafer = GetOrCreateWafer(waferId);
                string resolvedLotId = ResolveOrCreateCassetteLotId(cassetteLotId, cassette, wafer);
                State.LotId = resolvedLotId;
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
                WaferMaterial otherAtTarget = FindOtherWaferAtLocation(wafer.WaferId, targetLocation);
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
                RemoveWaferFromCassetteSlot(wafer.WaferId);

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
                targetSlot.HasWafer = true;
                cassette.LastScanTime = DateTime.Now;

                SequenceTrace.MaterialChange(
                    "PutWaferInCassette",
                    "wafer=" + wafer.WaferId,
                    "from=" + previousLocation,
                    "to=" + wafer.CurrentLocation,
                    "state=" + wafer.State,
                    "slot=" + slotNumber,
                    "cassette=" + cassetteRole);
            }
            NotifyAndSave("PutWaferInCassette");
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

            var wafer = State.Wafers.FirstOrDefault(w => w.WaferId == waferId);
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

            return State.Wafers.FirstOrDefault(w => w.WaferId == slot.WaferId);
        }

        public static WaferMaterial GetWaferAtLocation(MaterialLocationKind kind)
        {
            return State.Wafers.FirstOrDefault(w =>
                w.CurrentLocation != null &&
                w.CurrentLocation.Kind == kind &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
        }

        public static WaferMaterial CreateWaferAtLocation(MaterialLocationKind kind, string waferId, WaferMaterialState state)
        {
            var wafer = GetOrCreateWafer(waferId);
            RemoveWaferFromCassetteSlot(wafer.WaferId);
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

        public static bool ClearInputCassetteSlotData(CassetteMaterialRole cassetteRole, int slotNumber)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return false;

            cassette.EnsureSlots();
            if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                return false;

            var slot = cassette.Slots[slotNumber];
            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((!string.IsNullOrWhiteSpace(slot.WaferId) && w.WaferId == slot.WaferId) ||
                 (w.SourceCassetteRole == cassetteRole && w.SourceSlotNumber == slotNumber)))
                .ToList();

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.UpdatedAt = DateTime.Now;
            }

            slot.WaferId = "";
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

            var slotWaferIds = cassette.Slots
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.WaferId))
                .Select(s => s.WaferId)
                .ToList();

            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((slotWaferIds.Count > 0 && slotWaferIds.Contains(w.WaferId)) ||
                 w.SourceCassetteRole == cassetteRole))
                .ToList();

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.UpdatedAt = DateTime.Now;
            }

            foreach (var slot in cassette.Slots)
            {
                if (slot == null)
                    continue;

                slot.WaferId = "";
                slot.HasWafer = false;
            }

            // Material slot data를 모두 지운 뒤에는 마지막 mapping 결과를 더 이상
            // 유효한 것으로 사용할 수 없다. 다음 Auto 시작에서 실제 mapping을 다시
            // 수행하여 센서 결과와 Ready Material을 함께 재생성하도록 한다.
            cassette.IsMapped = false;
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
            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((!string.IsNullOrWhiteSpace(slot.WaferId) && w.WaferId == slot.WaferId) ||
                 (w.SourceCassetteRole == cassetteRole && w.SourceSlotNumber == slotNumber)))
                .ToList();

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.UpdatedAt = DateTime.Now;
            }

            slot.WaferId = "";
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

        private static void ClearOutputCassetteAllSlotData(CassetteMaterialRole cassetteRole, ref bool processed)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return;

            cassette.EnsureSlots();
            processed = true;

            var slotWaferIds = cassette.Slots
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.WaferId))
                .Select(s => s.WaferId)
                .ToList();

            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((slotWaferIds.Count > 0 && slotWaferIds.Contains(w.WaferId)) ||
                 (w.CurrentLocation != null &&
                  w.CurrentLocation.Kind == MaterialLocationKind.OutputCassette &&
                  w.CurrentLocation.CassetteRole == cassetteRole) ||
                 w.OutputCassetteRole == cassetteRole ||
                 w.SourceCassetteRole == cassetteRole))
                .ToList();

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.UpdatedAt = DateTime.Now;
            }

            foreach (var slot in cassette.Slots)
            {
                if (slot == null)
                    continue;

                slot.WaferId = "";
                slot.HasWafer = false;
            }

            // Material slot data를 모두 지운 뒤에는 마지막 mapping 결과를 더 이상
            // 유효한 것으로 사용할 수 없다. 다음 전체 준비에서 실제 mapping을 다시
            // 수행하여 센서 결과와 Ready Material을 함께 재생성하도록 한다.
            cassette.IsMapped = false;
        }

        public static void MoveWaferToInputFeeder(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            MoveWafer(
                wafer.WaferId,
                new MaterialLocation { Kind = MaterialLocationKind.InputFeeder },
                WaferMaterialState.WorkReady);
        }

        public static void MoveWaferToInputStage(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            MoveWafer(
                wafer.WaferId,
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

                var duplicate = State.Wafers.FirstOrDefault(w => w.WaferId == newValue && w != wafer);
                if (duplicate != null)
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

        public static void MoveWafer(string waferId, MaterialLocation location, WaferMaterialState state)
        {
            lock (_stateSync)
            {
                if (string.IsNullOrWhiteSpace(waferId))
                    throw new InvalidOperationException("이동할 Wafer/Bin ID가 없습니다.");

                MaterialLocation targetLocation = location ?? MaterialLocation.Unknown();
                WaferMaterial occupied = FindOtherWaferAtLocation(waferId, targetLocation);
                if (occupied != null)
                {
                    throw new InvalidOperationException("대상 Material 위치에 다른 자재가 있어 이동할 수 없습니다. target=" + targetLocation +
                                                        ", targetWafer=" + occupied.WaferId +
                                                        ", movingWafer=" + waferId);
                }

                var wafer = GetOrCreateWafer(waferId);
                MaterialLocation previousLocation = wafer.CurrentLocation;
                RemoveWaferFromCassetteSlot(wafer.WaferId);
                wafer.CurrentLocation = targetLocation;
                wafer.State = WaferMaterialStateText.Normalize(state);
                wafer.UpdatedAt = DateTime.Now;
                SequenceTrace.MaterialChange(
                    "MoveWafer",
                    "wafer=" + wafer.WaferId,
                    "from=" + previousLocation,
                    "to=" + wafer.CurrentLocation,
                    "state=" + wafer.State);
            }
            NotifyAndSave("MoveWafer");
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

                // 입력 웨이퍼는 추적용(있으면 기록). 없어도 빈맵 기반 계획은 성립한다.
                WaferMaterial sourceWafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                outputWafer.OutputReceiveSourceWaferId = sourceWafer != null ? sourceWafer.WaferId : "";
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
                    DieMap binMap = LoadRecipeBinMap(side);
                    if (binMap == null)
                        return null;
                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = ResolveOutputPickup(project);
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(binMap, pickup);
                    if (ordered.Count == 0)
                        return null;

                    int index = ResolveNextOutputReceiveIndex(outputWafer);

                    if (index >= ordered.Count)
                        return null;

                    DieMapEntry entry = ordered[index];
                    var target = new OutputStageReceiveTarget
                    {
                        StageLocation = ResolveOutputStageLocation(side),
                        OutputWaferId = outputWafer.WaferId,
                        SourceWaferId = outputWafer.OutputReceiveSourceWaferId,
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
                        return new OutputStageReceiveTarget
                        {
                            StageLocation = ResolveOutputStageLocation(side),
                            OutputWaferId = outputWafer.WaferId,
                            SourceWaferId = outputWafer.OutputReceiveSourceWaferId,
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

                    DieMap binMap = LoadRecipeBinMap(side);
                    if (binMap == null)
                        return null;
                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = ResolveOutputPickup(project);
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(binMap, pickup);
                    if (ordered.Count == 0 || index >= ordered.Count)
                        return null;

                    DieMapEntry entry = ordered[index];
                    var target = new OutputStageReceiveTarget
                    {
                        StageLocation = ResolveOutputStageLocation(side),
                        OutputWaferId = outputWafer.WaferId,
                        SourceWaferId = outputWafer.OutputReceiveSourceWaferId,
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

                    MaterialLocationKind stageLocation = ResolveOutputStageLocation(side);
                    WaferMaterial outputWafer = GetWaferAtLocation(stageLocation);
                    if (outputWafer == null)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Move die to output stage failed: output wafer is missing. die=" + dieId + ", side=" + side + " - Failed");
                        return false;
                    }

                    DieMaterial die = GetOrCreateDieMaterial(dieId);
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
                        State != null ? State.LotId : "",
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

        public static void UpdateOutputStageDieInspection(
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
                        return;

                    MaterialLocationKind stageLocation = ResolveOutputStageLocation(side);
                    WaferMaterial outputWafer = GetWaferAtLocation(stageLocation);
                    DieMaterial die = GetOrCreateDieMaterial(dieId);

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

                    if (outputWafer != null && outputWafer.OutputReceiveSlots != null)
                    {
                        OutputReceiveSlotMaterial slot = null;
                        if (receiveTarget != null)
                        {
                            slot = outputWafer.OutputReceiveSlots.FirstOrDefault(s =>
                                s != null && s.OrderIndex == receiveTarget.OrderIndex);
                        }

                        if (slot == null)
                        {
                            slot = outputWafer.OutputReceiveSlots.FirstOrDefault(s =>
                                s != null &&
                                string.Equals(s.DieUid, dieId, StringComparison.OrdinalIgnoreCase));
                        }

                        if (slot != null)
                        {
                            slot.DieUid = dieId;
                            slot.IsOutputInspectionDone = true;
                            slot.IsOutputInspectionOk = inspectionOk;
                            slot.OutputInspectionOffsetX = offset.X;
                            slot.OutputInspectionOffsetY = offset.Y;
                            slot.OutputInspectionOffsetT = offset.R;
                            slot.OutputInspectionRaw = raw ?? "";
                            outputWafer.UpdatedAt = DateTime.Now;
                        }
                    }

                    die.UpdatedAt = DateTime.Now;
                    if (outputWafer != null)
                    {
                        OutputWaferCsvSnapshotWriter.EnqueuePlacedDie(
                            "OutputStageDieInspection",
                            State != null ? State.RecipeName : "",
                            State != null ? State.LotId : "",
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
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output stage die inspection update failed: " + ex.Message + " - Failed");
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

            var activeWaferIds = State.Wafers
                .Where(w => w != null &&
                            w.CurrentLocation != null &&
                            activeKinds.Contains(w.CurrentLocation.Kind))
                .Select(w => w.WaferId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (activeWaferIds.Count > 0)
            {
                State.Dies.RemoveAll(d =>
                    d != null &&
                    ((!string.IsNullOrWhiteSpace(d.WaferID_Input) && activeWaferIds.Contains(d.WaferID_Input)) ||
                     (!string.IsNullOrWhiteSpace(d.WaferID_Output) && activeWaferIds.Contains(d.WaferID_Output))));
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
                string dieId = string.IsNullOrWhiteSpace(entry.DieUid)
                    ? BuildProcessTestDieId(wafer, mapY, mapX)
                    : entry.DieUid;
                entry.DieUid = dieId;

                DieMaterial die = GetOrCreateDieMaterial(dieId);
                die.WaferID_Input = wafer.WaferId;
                die.WaferID_Output = "";
                die.Wafer_IndexX = mapX;
                die.Wafer_IndexY = mapY;
                die.Wafer_OriginalIndexX = DieMapGenerator.ResolveOriginalMapIndexX(entry);
                die.Wafer_OriginalIndexY = DieMapGenerator.ResolveOriginalMapIndexY(entry);
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
            string tapeFrameSpecName)
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
                ApplyWaferCassettePosition(wafer, ResolveSlotPosition(null, slotNumber));

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

                bool isOutputStageWafer =
                    (stageLocation == MaterialLocationKind.OutputStageGood &&
                     (cassetteRole == CassetteMaterialRole.Good1 || cassetteRole == CassetteMaterialRole.Good2)) ||
                    (stageLocation == MaterialLocationKind.OutputStageNg &&
                     cassetteRole == CassetteMaterialRole.Ng1);
                if (isOutputStageWafer)
                {
                    // Output Stage의 테스트 Bin은 source slot에서 이미 꺼낸 상태이므로
                    // 동일 Material을 Stage와 cassette slot에 동시에 점유시키지 않는다.
                    slot.WaferId = "";
                    slot.HasWafer = false;
                }
                else
                {
                    slot.WaferId = wafer.WaferId;
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

        private static string BuildProcessTestDieId(WaferMaterial wafer, int mapY, int mapX)
        {
            string waferId = wafer != null && !string.IsNullOrWhiteSpace(wafer.WaferId) ? wafer.WaferId : "TEST";
            return waferId + "-D" + mapY.ToString("000") + "-" + mapX.ToString("000");
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
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(map, pickup);
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
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(map, pickup);
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
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(map, pickup);
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
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(map, pickup);
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
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(map, pickup);
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

            reason = "InputStage finish complete. waferId=" + wafer.WaferId +
                     ", dieCount=" + wafer.DieIds.Count;
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
                        if (die == null || !die.IsInputTarget || die.Result == DieResult.NG)
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
            wafer.InputStageDieMappingOffsetX = 0.0;
            wafer.InputStageDieMappingOffsetY = 0.0;
            wafer.HasInputStageDieMappingOrigin = false;
            wafer.InputStageDieMappingOriginX = 0.0;
            wafer.InputStageDieMappingOriginY = 0.0;
            wafer.HasInputStageDieMappingThetaSnapshot = false;
            wafer.InputStageDieMappingCorrectedT = 0.0;
            wafer.InputStageDieMappingInvalidatedByAlignChange = alignOrThetaChanged;
            wafer.InputMapApprovalHashAtMapping = "";
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
            double offsetT)
        {
            try
            {
                if (wafer == null)
                    return;

                wafer.HasInputStageAlignResult = true;
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
                    InvalidateInputStageDieMappingNoLock(wafer, true);
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
                return BuildDieMapFromWafer(GetWaferAtLocation(MaterialLocationKind.InputStage));
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
                State != null ? State.LotId : "",
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

        public static void ResetInputPickCompletionHistory(string dieId, string reason)
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
                    if (die == null)
                        return;

                    die.IsInputTarget = true;
                    die.Result = DieResult.Unknown;
                    if (die.NgCodes != null)
                        die.NgCodes.Clear();

                    if (die.CurrentLocation == null ||
                        die.CurrentLocation.Kind == MaterialLocationKind.Unknown)
                    {
                        die.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.InputStage };
                    }

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
                             string.Equals(x.InspectionType, "ManualPickerHeadEdit", StringComparison.OrdinalIgnoreCase)));
                    }

                    die.UpdatedAt = DateTime.Now;
                    NotifyAndSave("ResetInputPickCompletionHistory");
                    Log.Write("Main", "MATERIAL", "ResetInputPickCompletionHistory",
                        "Input pick completion history reset manually. die=" + dieId +
                        ", reason=" + (reason ?? "") + " - Ok");
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "MATERIAL", "ResetInputPickCompletionHistory",
                    "Input pick completion history reset failed. die=" + dieId +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
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
                lock (_stateSync)
                {
                    State.SaveReason = reason ?? "";
                    State.SavedAt = DateTime.Now;
                    NormalizeSnapshotHeader(State);
                }

                lock (_saveIoSync)
                {
                    saved = MaterialSnapshotStore.Save(State);
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

                if (string.IsNullOrWhiteSpace(snapshot.LotId))
                {
                    string lotId = ResolveSnapshotLotId(snapshot);
                    if (!string.IsNullOrWhiteSpace(lotId))
                        snapshot.LotId = lotId;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material snapshot header normalize failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static string ResolveSnapshotLotId(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                    return "";

                if (snapshot.Cassettes != null)
                {
                    var cassette = snapshot.Cassettes.FirstOrDefault(c => c != null && !string.IsNullOrWhiteSpace(c.CassetteLotId));
                    if (cassette != null)
                        return cassette.CassetteLotId.Trim();
                }

                if (snapshot.Wafers != null)
                {
                    var wafer = snapshot.Wafers.FirstOrDefault(w => w != null && !string.IsNullOrWhiteSpace(w.CassetteLotId));
                    if (wafer != null)
                        return wafer.CassetteLotId.Trim();
                }
            }
            catch
            {
            }
            finally
            {
            }

            return "";
        }

        private static void RemoveWaferFromCassetteSlot(string waferId)
        {
            if (string.IsNullOrEmpty(waferId)) return;
            foreach (var cassette in State.Cassettes)
            {
                foreach (var slot in cassette.Slots)
                {
                    if (slot.WaferId == waferId)
                    {
                        slot.WaferId = "";
                        slot.HasWafer = false;
                    }
                }
            }
        }

        private static WaferMaterial FindOtherWaferAtLocation(string waferId, MaterialLocation targetLocation)
        {
            if (targetLocation == null || targetLocation.Kind == MaterialLocationKind.Unknown)
                return null;

            return State.Wafers.FirstOrDefault(w =>
                w != null &&
                !string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase) &&
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
            IReadOnlyList<double> slotPositions)
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

                    WaferMaterial slotWafer = State.Wafers.FirstOrDefault(w =>
                        w != null && string.Equals(w.WaferId, slot.WaferId, StringComparison.OrdinalIgnoreCase));
                    if (slotWafer == null)
                    {
                        throw new InvalidOperationException("기존 cassette 점유 slot의 Material 데이터가 없습니다. cassette=" + role +
                                                            ", slot=" + (i + 1) + ", waferId=" + slot.WaferId);
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
            if (!string.IsNullOrWhiteSpace(requestedLotId))
                return requestedLotId.Trim();

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
                        WaferMaterial wafer = State.Wafers.FirstOrDefault(w =>
                            w != null && string.Equals(w.WaferId, slot.WaferId, StringComparison.OrdinalIgnoreCase));
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
            if (!string.IsNullOrWhiteSpace(requestedLotId))
                return requestedLotId.Trim();

            List<string> existing = (existingLotIds ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (existing.Count == 1)
                return existing[0];
            if (existing.Count > 1)
            {
                throw new InvalidOperationException(
                    "기존 카세트 LOT ID가 서로 달라 임시 LOT ID를 결정할 수 없습니다. context=" + context +
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
            string tapeFrameSpecName)
        {
            var cassette = EnsureCassette(role, slotCount);
            string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, role);
            State.LotId = resolvedLotId;
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
                WaferMaterial wafer = slot != null && !string.IsNullOrWhiteSpace(slot.WaferId)
                    ? State.Wafers.FirstOrDefault(w => string.Equals(w.WaferId, slot.WaferId, StringComparison.OrdinalIgnoreCase))
                    : null;
                bool preserveExisting = wafer != null &&
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
                slot.HasWafer = true;
            }
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

        private static void RemoveFinishedOutputBinWaferForNewCassetteMapping(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            string waferId = wafer.WaferId;
            State.Dies.RemoveAll(d =>
                d != null &&
                (string.Equals(d.WaferID_Input, waferId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(d.WaferID_Output, waferId, StringComparison.OrdinalIgnoreCase)));
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
            State.LotId = resolvedLotId;

            wafer.SourceCassetteId = cassette.CassetteId;
            wafer.SourceCassetteRole = cassette.Role;
            wafer.SourceSlotNumber = slotNumber;
            ApplyWaferCassettePosition(wafer, slotPosition);
            wafer.CurrentLocation = MaterialLocation.Cassette(MaterialLocationKind.InputCassette, cassette.Role, slotNumber);
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
                var wafer = slot != null && !string.IsNullOrWhiteSpace(slot.WaferId)
                    ? State.Wafers.FirstOrDefault(w => w.WaferId == slot.WaferId)
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
