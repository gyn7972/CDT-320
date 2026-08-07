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
    // MaterialStateService partial: Output receive 플랜/예약/검사 + 공용 순서 빌더 (원본 5798-7986)
    public static partial class MaterialStateService
    {
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

                var project = RecipeStore.LoadLastOrDefaultCached();
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
            var project = RecipeStore.LoadLastOrDefaultCached();
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
            List<WaferMaterial> inputWafers = State.Wafers
                .Where(wafer => wafer != null &&
                                wafer.CurrentLocation != null &&
                                wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage)
                .ToList();
            List<WaferMaterial> goodWafers = State.Wafers
                .Where(wafer => wafer != null &&
                                wafer.CurrentLocation != null &&
                                wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageGood)
                .ToList();
            List<WaferMaterial> ngWafers = State.Wafers
                .Where(wafer => wafer != null &&
                                wafer.CurrentLocation != null &&
                                wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageNg)
                .ToList();

            string reason;
            List<DieMaterial> inputRelatedDies;
            List<DieMaterial> inputRemoveDies;
            if (!TryCollectInputParentDiesNoLock(inputWafers, out inputRelatedDies, out reason) ||
                !TryCollectInputLocationClearDiesNoLock(
                    inputWafers,
                    MaterialLocationKind.InputStage,
                    out inputRemoveDies,
                    out reason))
            {
                throw new InvalidOperationException(
                    "공정 테스트 Input Stage Material을 안전하게 정리할 수 없습니다. " + reason);
            }
            if (inputRelatedDies.Count != inputRemoveDies.Count)
            {
                DieMaterial activeDie = inputRelatedDies.FirstOrDefault(die => !inputRemoveDies.Contains(die));
                throw new InvalidOperationException(
                    "공정 테스트 Input Stage와 연결된 Die가 다른 물리 위치에 있습니다. die=" +
                    (activeDie != null ? activeDie.DieId : "") + ", location=" +
                    (activeDie != null && activeDie.CurrentLocation != null
                        ? activeDie.CurrentLocation.Kind.ToString()
                        : "Unknown"));
            }

            List<DieMaterial> goodDies;
            if (!TryCollectOutputLocationClearDiesNoLock(
                goodWafers,
                MaterialLocationKind.OutputStageGood,
                out goodDies,
                out reason))
            {
                throw new InvalidOperationException(
                    "공정 테스트 GOOD Stage Material을 안전하게 정리할 수 없습니다. " + reason);
            }

            List<DieMaterial> ngDies;
            if (!TryCollectOutputLocationClearDiesNoLock(
                ngWafers,
                MaterialLocationKind.OutputStageNg,
                out ngDies,
                out reason))
            {
                throw new InvalidOperationException(
                    "공정 테스트 NG Stage Material을 안전하게 정리할 수 없습니다. " + reason);
            }

            // 모든 Stage의 사전검증이 끝난 뒤에만 실제 상태를 바꾼다.
            RemoveDieMaterialsNoLock(inputRemoveDies);
            DetachOrRemoveClearedOutputDiesNoLock(goodDies);
            DetachOrRemoveClearedOutputDiesNoLock(ngDies);

            foreach (WaferMaterial wafer in inputWafers)
            {
                ClearInputStageWaferProcessingFieldsNoLock(wafer);
                wafer.InputStageProcessingGeneration = wafer.InputStageProcessingGeneration + 1;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.State = WaferMaterialState.Empty;
                wafer.UpdatedAt = DateTime.Now;
            }
            foreach (WaferMaterial wafer in goodWafers.Concat(ngWafers))
            {
                ClearOutputStageWaferProcessingFieldsNoLock(wafer);
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.State = WaferMaterialState.Empty;
                wafer.UpdatedAt = DateTime.Now;
            }

            InputStageHybridResultSession.Clear();
            _outputReceiveOrderCache.Remove(QMC.CDT320.BinSide.Good);
            _outputReceiveOrderCache.Remove(QMC.CDT320.BinSide.Ng);
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

        private static void ValidateProcessTestStageCassetteSlotNoLock(
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            int plannedSlotCount = -1,
            bool requireUnoccupied = false)
        {
            if (slotNumber < 0)
                throw new InvalidOperationException("공정 테스트 Cassette slot 번호가 올바르지 않습니다.");

            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == cassetteRole);
            if (cassette == null)
            {
                if (plannedSlotCount > slotNumber)
                    return;
                throw new InvalidOperationException(
                    "공정 테스트 Cassette Material이 없습니다. role=" + cassetteRole);
            }

            cassette.EnsureSlots();
            if (slotNumber >= cassette.Slots.Count || cassette.Slots[slotNumber] == null)
            {
                throw new InvalidOperationException(
                    "공정 테스트 Cassette slot 데이터가 올바르지 않습니다. role=" + cassetteRole +
                    ", slot=" + (slotNumber + 1) + ", slotCount=" + cassette.Slots.Count);
            }

            if (!requireUnoccupied)
                return;

            CassetteSlotMaterial slot = cassette.Slots[slotNumber];
            bool hasPointer = !string.IsNullOrWhiteSpace(slot.WaferId) ||
                              !string.IsNullOrWhiteSpace(slot.WaferInstanceId);
            if (slot.HasWafer)
            {
                throw new InvalidOperationException(
                    "공정 테스트 source slot에 기존 physical Material이 있습니다. role=" + cassetteRole +
                    ", slot=" + (slotNumber + 1));
            }
            if (!hasPointer)
                return;

            string resolveReason;
            WaferMaterial marker = ResolveCassetteSlotWaferNoLock(slot, out resolveReason);
            if (marker == null ||
                !IsStateOnlyEmptyCassetteMaterialNoLock(marker, cassetteRole, slotNumber))
            {
                throw new InvalidOperationException(
                    "공정 테스트 source slot의 비점유 pointer가 올바르지 않습니다. role=" + cassetteRole +
                    ", slot=" + (slotNumber + 1) + ", detail=" + resolveReason);
            }
            if (IsOutputCassetteRole(cassetteRole))
            {
                List<DieMaterial> markerDies;
                string clearReason;
                if (!TryCollectOutputCassetteClearDiesNoLock(
                    new List<WaferMaterial> { marker },
                    out markerDies,
                    out clearReason))
                {
                    throw new InvalidOperationException(
                        "공정 테스트 source slot의 EMPTY Material을 안전하게 분리할 수 없습니다. " + clearReason);
                }
            }
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
                    throw new InvalidOperationException("공정 테스트 Stage Wafer Material이 없습니다.");

                CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
                if (cassette == null)
                    throw new InvalidOperationException("공정 테스트 Cassette Material이 없습니다. role=" + cassetteRole);

                cassette.EnsureSlots();
                if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                    throw new InvalidOperationException(
                        "공정 테스트 Cassette slot 범위를 벗어났습니다. role=" + cassetteRole +
                        ", slot=" + (slotNumber + 1));

                CassetteSlotMaterial slot = cassette.Slots[slotNumber];
                if (slot == null)
                    throw new InvalidOperationException("공정 테스트 Cassette slot 데이터가 없습니다.");

                bool hasPreviousPointer = !string.IsNullOrWhiteSpace(slot.WaferId) ||
                                          !string.IsNullOrWhiteSpace(slot.WaferInstanceId);
                if (hasPreviousPointer)
                {
                    string previousReason;
                    WaferMaterial previous = ResolveCassetteSlotWaferNoLock(slot, out previousReason);
                    if (previous == null)
                    {
                        throw new InvalidOperationException(
                            "공정 테스트 기존 slot Material pointer가 올바르지 않습니다. " + previousReason);
                    }
                    if (!ReferenceEquals(previous, wafer))
                    {
                        if (IsOutputCassetteRole(cassetteRole))
                            DetachOutputWaferMaterialForReplacementNoLock(previous);
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
                throw;
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
                // [리뷰 반영 2026-08-05] LoadCompatibleMap 경로가 project를 변형할 가능성이 지적되어
                // 이 지점은 캐시 인스턴스 대신 신선 로드를 유지한다 (per-wafer 캐시 뒤라 저빈도).
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

    }
}
