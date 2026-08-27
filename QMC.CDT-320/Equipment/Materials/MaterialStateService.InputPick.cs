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
    // MaterialStateService partial: Input pick 예약/해제, Review 승인, Align/Mapping 결과, DieMap 빌더 (원본 7987-11242)
    public static partial class MaterialStateService
    {
        public static InputStagePickTarget ReserveNextInputStagePickTarget(MaterialLocationKind pickerLocation, int pickerNo)
        {
            long probeToken = MaterialPerfProbe.BeginSample();
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

                    InputPickContext pickContext;
                    if (wafer == null || !TryResolveInputPickContextNoLock(wafer, out pickContext))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve skipped: input stage die map is empty. - Check");
                        return null;
                    }

                    List<DieMapEntry> ordered = pickContext.Ordered;
                    if (ordered == null || ordered.Count == 0)
                        return null;

                    InputStagePickTarget existingReservedTarget = TryBuildExistingReservedInputStagePickTarget(
                        ordered,
                        wafer,
                        pickerLocation,
                        pickerNo);
                    if (existingReservedTarget != null)
                        return existingReservedTarget;

                    AppSettings settings = AppSettingsStore.Current;
                    if (settings != null && settings.UseOutputGoodPickupCap)
                    {
                        int pending;
                        int held;
                        int reserved;
                        int allowance = ResolveOutputGoodNewPickAllowanceNoLock(
                            out pending,
                            out held,
                            out reserved);
                        if (allowance <= 0)
                        {
                            Log.Write("Main", "SYSTEM", "MaterialStateService",
                                "GOOD 배출 픽업 캡으로 신규 Input pick 예약을 보류합니다. " +
                                "pending=" + pending +
                                ", held=" + held +
                                ", reserved=" + reserved +
                                ", allowance=" + allowance +
                                ", pickerLocation=" + pickerLocation +
                                ", pickerNo=" + pickerNo + " - Wait");
                            return null;
                        }
                    }

                    var skipSummary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = FindDieByIdNoLock(entry.DieUid);
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
                MaterialPerfProbe.EndSample("ReserveNextInputStagePickTarget", probeToken);
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

                    DieMaterial die = FindDieByIdNoLock(entry.DieUid);
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

                    InputPickContext pickContext;
                    if (wafer == null || !TryResolveInputPickContextNoLock(wafer, out pickContext))
                        return candidates;

                    List<DieMapEntry> ordered = pickContext.Ordered;
                    if (ordered == null || ordered.Count == 0)
                        return candidates;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = FindDieByIdNoLock(entry.DieUid);
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

                    InputPickContext pickContext;
                    if (wafer == null || !TryResolveInputPickContextNoLock(wafer, out pickContext))
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve by die skipped: input stage die map is empty. - Check");
                        return null;
                    }

                    List<DieMapEntry> ordered = pickContext.Ordered;
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

                    InputPickContext pickContext;
                    if (wafer == null || !TryResolveInputPickContextNoLock(wafer, out pickContext))
                        return null;

                    List<DieMapEntry> ordered = pickContext.Ordered;
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

        /// <summary>
        /// [시뮬 Review 건너뛰기 2026-08-07] 작업자 확인 없이 "기본 선택"으로 Review를 승인한다.
        /// 작업자가 Review 화면에서 아무것도 바꾸지 않고 확인만 누른 것과 같은 상태를 만든다:
        /// PickUp 순서는 레시피 기본 순서, 시작 Die 지정 없음(index=0), Die 상태 변경 없음.
        /// 호출 측(InputSequence)이 SimulationMode를 이미 확인한 뒤에만 호출한다.
        /// </summary>
        public static bool TryApproveInputStageRunReviewWithDefaultOrder(
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
                        reason = "InputStage 리뷰 자동 승인 대상 Wafer Material이 없습니다.";
                        return false;
                    }

                    if (!wafer.HasInputStageAlignResult ||
                        !wafer.HasInputStageThetaAlignResult ||
                        !wafer.HasInputStageDieMappingResult ||
                        wafer.InputStageDieMappingInvalidatedByAlignChange)
                    {
                        reason = "Align/T Align/Die Mapping이 모두 유효한 상태에서만 리뷰를 자동 승인할 수 있습니다. waferId=" +
                                 (wafer.WaferId ?? "");
                        return false;
                    }

                    string resultModeReason;
                    if (!IsStoredInputStageResultModeUsableNoLock(wafer, true, out resultModeReason))
                    {
                        reason = "저장된 Align/Die Mapping 결과를 사용할 수 없어 리뷰를 자동 승인할 수 없습니다. " +
                                 resultModeReason;
                        return false;
                    }

                    DieMap map = BuildDieMapFromWaferNoLock(wafer);
                    if (wafer.DieIds == null || wafer.DieIds.Count == 0 ||
                        map == null || map.Entries == null || map.Entries.Count == 0)
                    {
                        reason = "InputStage Die 데이터 또는 Die Map이 비어 있어 리뷰를 자동 승인할 수 없습니다. waferId=" +
                                 (wafer.WaferId ?? "");
                        return false;
                    }

                    string mappingRevision = ResolveInputStageRunReviewMappingRevision(wafer, map);
                    if (string.IsNullOrWhiteSpace(mappingRevision))
                    {
                        reason = "현재 Die Mapping revision을 확인할 수 없어 리뷰를 자동 승인할 수 없습니다. waferId=" +
                                 (wafer.WaferId ?? "");
                        return false;
                    }

                    // 승인이 없을 때 사용하던 것과 동일한 레시피 기본 PickUp 순서를 그대로 승인 순서로 쓴다.
                    RecipeProject project = RecipeStore.LoadLastOrDefaultCached();
                    PickupSubset pickup = ResolveInputPickup(project);
                    List<DieMapEntry> defaultOrder = BuildOutputReceiveOrder(map, pickup) ?? new List<DieMapEntry>();

                    var orderedIds = new List<string>();
                    var orderedIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (DieMapEntry entry in defaultOrder)
                    {
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;
                        if (orderedIdSet.Add(entry.DieUid))
                            orderedIds.Add(entry.DieUid);
                    }

                    // TryBuildApprovedInputStagePickOrder는 승인 목록 밖의 WAIT Target을 fail-closed로 막는다.
                    // 레시피 순서가 Pick 가능 Die를 모두 담지 못하면 Map 순서로 보충하고 사실을 로그로 남긴다.
                    var appendedIds = new List<string>();
                    foreach (DieMapEntry entry in map.Entries)
                    {
                        if (entry == null ||
                            string.IsNullOrWhiteSpace(entry.DieUid) ||
                            !entry.IsTarget ||
                            entry.Result == DieResult.Good ||
                            entry.Result == DieResult.NG)
                        {
                            continue;
                        }

                        if (orderedIdSet.Add(entry.DieUid))
                        {
                            orderedIds.Add(entry.DieUid);
                            appendedIds.Add(entry.DieUid);
                        }
                    }

                    if (appendedIds.Count > 0)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "InputStage 리뷰 자동 승인: 레시피 PickUp 순서에 없던 WAIT Target을 Map 순서로 보충했습니다. waferId=" +
                            (wafer.WaferId ?? "") + ", appended=" + appendedIds.Count + " - Check");
                    }

                    // 시작 Die 미지정(index=0) — ValidateInputStageRunReviewStartSelection의 기본 통과 조건이다.
                    wafer.HasInputStageRunReviewApproval = true;
                    wafer.InputStageRunReviewStartDieIndex = 0;
                    wafer.InputStageRunReviewStartDieUid = "";
                    wafer.InputStageRunReviewOrderedDieIds = orderedIds;
                    wafer.InputStageRunReviewMappingRevision = mappingRevision;
                    wafer.UpdatedAt = DateTime.Now;
                    // 승인/순서가 바뀌었으므로 파생 PickUp 컨텍스트 캐시를 버린다.
                    InvalidateInputPickContextCacheNoLock();

                    reason = "InputStage 리뷰를 기본 순서로 자동 승인했습니다. waferId=" + (wafer.WaferId ?? "") +
                             ", orderedCount=" + orderedIds.Count +
                             ", mappingRevision=" + mappingRevision;
                    NotifyAndSave("InputStageRunReviewAutoApproved");
                    return true;
                }
            }
            catch (Exception ex)
            {
                reason = "InputStage 리뷰 자동 승인 실패: " + ex.Message;
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
            long probeToken = MaterialPerfProbe.BeginSample();
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    string readyReason;
                    if (!IsInputStageFinishCompleteNoLock(wafer, out readyReason))
                        return false;

                    InputPickContext pickContext;
                    if (wafer == null || !TryResolveInputPickContextNoLock(wafer, out pickContext))
                        return false;

                    List<DieMapEntry> ordered = pickContext.Ordered;
                    if (ordered == null || ordered.Count == 0)
                        return false;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = FindDieByIdNoLock(entry.DieUid);
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
                MaterialPerfProbe.EndSample("HasReadyInputStagePickTarget", probeToken);
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
            long probeToken = MaterialPerfProbe.BeginSample();
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

                    InputPickContext pickContext;
                    if (wafer == null || !TryResolveInputPickContextNoLock(wafer, out pickContext))
                        return false;

                    List<DieMapEntry> ordered = pickContext.Ordered;
                    if (ordered == null || ordered.Count == 0)
                        return false;

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        DieMapEntry entry = ordered[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.DieUid))
                            continue;

                        DieMaterial die = FindDieByIdNoLock(entry.DieUid);
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
                MaterialPerfProbe.EndSample("HasActionableInputStagePickTarget", probeToken);
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

        public static int CountInputStagePickReservationsForPickerLocation(MaterialLocationKind pickerLocation)
        {
            try
            {
                lock (_stateSync)
                {
                    if (pickerLocation != MaterialLocationKind.PickerFront &&
                        pickerLocation != MaterialLocationKind.PickerRear)
                    {
                        return 0;
                    }

                    int count = 0;
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
                            count++;
                    }

                    return count;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick 예약 수 조회에 실패했습니다. pickerLocation=" + pickerLocation +
                    ", error=" + ex.Message + " - Failed");
                return 0;
            }
            finally
            {
            }
        }

        public static int GetOutputGoodNewPickAllowance(out int pending, out int held, out int reserved)
        {
            pending = 0;
            held = 0;
            reserved = 0;

            try
            {
                lock (_stateSync)
                {
                    return ResolveOutputGoodNewPickAllowanceNoLock(out pending, out held, out reserved);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "GOOD 배출 신규 pick 허용량 조회에 실패했습니다. error=" + ex.Message + " - Failed");
                pending = 0;
                held = 0;
                reserved = 0;
                return 0;
            }
            finally
            {
            }
        }

        private static int ResolveOutputGoodNewPickAllowanceNoLock(
            out int pending,
            out int held,
            out int reserved)
        {
            pending = CountPendingOutputReceiveSlotsNoLock(QMC.CDT320.BinSide.Good);
            held = 0;
            reserved = 0;

            if (State.Dies != null)
            {
                for (int i = 0; i < State.Dies.Count; i++)
                {
                    DieMaterial die = State.Dies[i];
                    if (die == null)
                        continue;

                    MaterialLocation location = die.CurrentLocation;
                    MaterialLocationKind kind = location != null
                        ? location.Kind
                        : MaterialLocationKind.Unknown;

                    bool isPickerLocation =
                        kind == MaterialLocationKind.PickerFront ||
                        kind == MaterialLocationKind.PickerRear;
                    int pickerNo = location != null ? location.PickerNo : 0;
                    if (die.IsInputTarget &&
                        isPickerLocation &&
                        pickerNo >= 1 &&
                        pickerNo <= 4)
                    {
                        held++;
                    }

                    if (kind == MaterialLocationKind.InputStage && IsDieReservedForPicker(die))
                        reserved++;
                }
            }

            return Math.Max(0, pending - held - reserved);
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

            InputPickContext pickContext;
            if (!TryResolveInputPickContextNoLock(wafer, out pickContext))
            {
                reason = "InputStage die map is empty. waferId=" + wafer.WaferId;
                return false;
            }

            if (!wafer.HasInputStageRunReviewApproval)
            {
                reason = "InputStage Align/Die Mapping 사용자 확인이 완료되지 않았습니다. waferId=" + wafer.WaferId;
                return false;
            }

            if (!pickContext.ReviewApprovalValid)
            {
                reason = "InputStage Review 승인 PickUp 순서가 유효하지 않습니다. waferId=" +
                         wafer.WaferId + ", reason=" + pickContext.ReviewApprovalReason;
                return false;
            }

            reason = "InputStage finish complete. waferId=" + wafer.WaferId +
                     ", dieCount=" + wafer.DieIds.Count +
                     ", pickableCount=" + (pickContext.Ordered != null ? pickContext.Ordered.Count : 0);
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
            long probeToken = MaterialPerfProbe.BeginSample();
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

                        DieMaterial die = FindDieByIdNoLock(dieId);
                        if (die == null || !die.IsInputTarget ||
                            die.Result == DieResult.NG ||
                            (die.Result == DieResult.Good && die.InputSequenceNo <= 0))
                            continue;

                        MaterialLocationKind kind = die.CurrentLocation != null
                            ? die.CurrentLocation.Kind
                            : MaterialLocationKind.Unknown;

                        if (kind == MaterialLocationKind.Unknown)
                        {
                            // Output Cassette Clear 시 Source Input wafer가 아직 작업 집합이면 완료 Die를
                            // 복구용으로 보존하면서 위치만 Unknown으로 분리한다. 실제 Pick 완료 이력이
                            // 확인된 Die만 InputStage에서 이미 배출된 것으로 인정하고, 이력이 없는
                            // Unknown Die는 실제 미처리 가능성이 있으므로 기존처럼 완료를 차단한다.
                            if (HasInputPickCompletedHistory(die))
                                continue;

                            return false;
                        }

                        if (kind == MaterialLocationKind.InputStage ||
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
                MaterialPerfProbe.EndSample("IsInputStagePickComplete", probeToken);
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
            // [계약 보강 2026-08-07] UI 등 락 밖 호출자를 위해 전체를 락 안에서 수행한다.
            // (내부의 GetWaferAtLocation/IsStoredInputStageResultModeUsable/BuildDieMapFromWafer는 재진입)
            try
            {
                lock (_stateSync)
                {
                    return BuildInputDieMapFromStageWaferNoLock();
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input die map rebuild from stage wafer failed: " + ex.Message + " - Failed");
                return null;
            }
        }

        private static DieMap BuildInputDieMapFromStageWaferNoLock()
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
            // [계약 보강 2026-08-07] State.Dies 전체 스캔(ResolveWaferDies)이 락 밖에서 수행되지 않도록
            // 락을 잡는다. 시퀀스 경로(InputPickContext 등)는 이미 락 보유 상태라 재진입이다.
            lock (_stateSync)
            {
                return BuildDieMapFromWaferNoLock(wafer);
            }
        }

        private static DieMap BuildDieMapFromWaferNoLock(WaferMaterial wafer)
        {
            long probeToken = MaterialPerfProbe.BeginSample();
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
                    PickupSequenceGenerator.ApplySequenceNumbers(map, ResolveInputPickup(RecipeStore.LoadLastOrDefaultCached()));
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
                MaterialPerfProbe.EndSample("BuildDieMapFromWafer", probeToken);
            }
        }

        public static DieMap BuildOutputReceiveDieMapFromWafer(WaferMaterial wafer)
        {
            // [계약 보강 2026-08-07] 시퀀스가 변이하는 OutputReceiveSlots를 락 밖에서 순회하지 않도록 락을 잡는다.
            lock (_stateSync)
            {
                return BuildOutputReceiveDieMapFromWaferNoLock(wafer);
            }
        }

        private static DieMap BuildOutputReceiveDieMapFromWaferNoLock(WaferMaterial wafer)
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

            string waferInstanceId = wafer.WaferInstanceId ?? "";
            List<DieMaterial> source = State.Dies.Where(d =>
                d != null &&
                string.Equals(d.WaferID_Input, wafer.WaferId, StringComparison.OrdinalIgnoreCase) &&
                IsDieOwnedByInputWaferInstance(d, waferInstanceId) &&
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

        /// <summary>
        /// Die가 해당 Input wafer "세대"의 소유인지 판정한다.
        /// 표시 WaferId는 Slot 고정 재사용 시 세대 간 중복되므로
        /// (ResetInputStageWaferProcessingStateNoLock가 출력 부모 die를 남기고 새 instance를 발급),
        /// 세대 구분은 InputWaferInstanceId로 확정한다.
        /// 레거시 폴백: 어느 한쪽이라도 instance id가 없으면 기존 표시-ID 판정을 유지한다.
        /// </summary>
        private static bool IsDieOwnedByInputWaferInstance(DieMaterial die, string waferInstanceId)
        {
            if (string.IsNullOrWhiteSpace(waferInstanceId))
                return true;

            string dieInstanceId = die != null ? die.InputWaferInstanceId : null;
            if (string.IsNullOrWhiteSpace(dieInstanceId))
                return true;

            // 다른 instance 판정들(ValidateWaferInstancePointer 등)과 동일하게 Trim 후 비교한다.
            return string.Equals(dieInstanceId.Trim(), waferInstanceId.Trim(), StringComparison.OrdinalIgnoreCase);
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

                    // [리뷰 반영 2026-08-05] 좌표 전파는 die.WaferOffset/entry.PosX,Y를 바꾸지만
                    // wafer 키 필드를 건드리지 않는다 — 캐시된 pick 좌표가 굳지 않도록 명시 무효화한다.
                    if (updatedDieCount > 0)
                        InvalidateInputPickContextCacheNoLock();
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

        /// <summary>
        /// [검사 재실행 2026-08-27 팀장님 지시] 픽커 헤드(1~4)에 물려 있는(아직 Place 전) 다이들의
        /// Bottom/Side 검사 데이터만 삭제해 재검사 가능 상태로 되돌립니다. 픽업 이력/위치/맵 정보는
        /// 유지합니다. Result를 Unknown으로 리셋하므로 NG 래치(기존 NG면 재검사 OK여도 NG 유지)가
        /// 풀리고, Place는 검사 흐름 미완료 차단으로 재검사 전 진행이 안전하게 막힙니다.
        /// </summary>
        public static bool ClearPickerHeadInspectionData(
            MaterialLocationKind pickerLocation,
            string reason,
            out string message,
            out List<string> clearedDieIds)
        {
            message = string.Empty;
            clearedDieIds = new List<string>();
            try
            {
                if (pickerLocation != MaterialLocationKind.PickerFront &&
                    pickerLocation != MaterialLocationKind.PickerRear)
                {
                    message = "픽커 위치가 아닙니다. location=" + pickerLocation;
                    return false;
                }

                var targets = new List<DieMaterial>();
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    DieMaterial die = GetDieAtPicker(pickerLocation, pickerNo);
                    if (die != null)
                        targets.Add(die);
                }

                if (targets.Count == 0)
                {
                    message = "픽커에 물려 있는 Die가 없습니다.";
                    return false;
                }

                var auditLines = new List<string>();
                lock (_stateSync)
                {
                    foreach (DieMaterial die in targets)
                    {
                        auditLines.Add("die=" + die.DieId +
                            ", previousResult=" + die.Result +
                            ", bottom=" + (die.Inspections != null && die.Inspections.Any(x => x != null && string.Equals(x.InspectionType, "Bottom", StringComparison.OrdinalIgnoreCase))) +
                            ", side0=" + (die.Inspections != null && die.Inspections.Any(x => x != null && string.Equals(x.InspectionType, "Side0", StringComparison.OrdinalIgnoreCase))) +
                            ", side90=" + (die.Inspections != null && die.Inspections.Any(x => x != null && string.Equals(x.InspectionType, "Side90", StringComparison.OrdinalIgnoreCase))));

                        if (die.Inspections != null)
                        {
                            die.Inspections.RemoveAll(x =>
                                x != null &&
                                (string.Equals(x.InspectionType, "Bottom", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(x.InspectionType, "Side0", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(x.InspectionType, "Side90", StringComparison.OrdinalIgnoreCase)));
                        }

                        die.Result = DieResult.Unknown;
                        if (die.NgCodes != null)
                            die.NgCodes.Clear();
                        die.UpdatedAt = DateTime.Now;
                        clearedDieIds.Add(die.DieId);
                        // 맵 엔트리 동기화(SyncActiveInputMapEntryNoLock)는 의도적으로 생략 —
                        // IsTarget/BinCode까지 덮어써 픽업 완료 표시를 훼손할 수 있고,
                        // 맵 표시는 재검사 완료 시 기존 반영 경로가 다시 갱신한다.
                    }
                }

                NotifyAndSave("ClearPickerHeadInspectionData");
                Log.Write("Main", "MATERIAL", "ClearPickerHeadInspection",
                    "픽커 헤드 Die의 Bottom/Side 검사 데이터를 삭제했습니다(재검사 대기). location=" + pickerLocation +
                    ", count=" + clearedDieIds.Count +
                    ", reason=" + (reason ?? "") +
                    ", " + string.Join(" | ", auditLines) + " - Ok");
                message = "Die " + clearedDieIds.Count + "개의 Bottom/Side 검사 데이터를 삭제했습니다.";
                return true;
            }
            catch (Exception ex)
            {
                message = "검사 데이터 삭제 실패: " + ex.Message;
                Log.Write("Main", "MATERIAL", "ClearPickerHeadInspection",
                    message + ", location=" + pickerLocation + " - Failed");
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

            // [리뷰 반영 2026-08-05] 재픽업 복구 die가 캐시된 Ordered에 없을 수 있으므로
            // (재기동/컴팩션 후 재구축된 캐시), 다음 게이트에서 재평가되도록 명시 무효화한다.
            InvalidateInputPickContextCacheNoLock();
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

    }
}
