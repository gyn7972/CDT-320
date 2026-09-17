using System;
using System.Collections.Generic;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.Common.Logging;

namespace QMC.CDT320.Lots
{
    /// <summary>
    /// 얼라인 전에 바코드 후보의 네트워크 맵과 제품/BIN 조건을 확인한다.
    /// 기존 수신 캐시는 사용하지만 바코드, Material, 활성 맵, 픽업 순서는 변경하지 않는다.
    /// </summary>
    internal static class InputWaferMapPreflightService
    {
        internal const double PitchCompareToleranceMm = 0.05;

        internal static bool TryValidate(string barcode, InputStageUnit stage, out string reason)
        {
            DieMap prepared;
            return TryValidate(barcode, stage, out prepared, out reason);
        }

        internal static bool TryValidate(string barcode, InputStageUnit stage, out DieMap prepared, out string reason)
        {
            prepared = null;
            reason = "";
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                {
                    reason = "네트워크 웨이퍼맵 설정을 읽을 수 없습니다.";
                    return false;
                }
                if (!RecipeInputMapSource.UsesRemoteForActiveRecipe(settings))
                    return true;
                if (string.IsNullOrWhiteSpace(LotWaferMapFetchService.ResolveNetworkFolder()))
                {
                    reason = "네트워크 웨이퍼맵 사용이 켜져 있지만 폴더 경로가 비어 있습니다. 설정을 확인하세요.";
                    return false;
                }
                if (stage == null || stage.Recipe == null || stage.Recipe.DieMap == null)
                {
                    reason = "웨이퍼맵 사전 확인에 필요한 InputStage 레시피가 없습니다.";
                    return false;
                }

                WaferMaterial wafer = stage.GetCurrentStageWaferMaterial();
                if (wafer == null)
                {
                    reason = "웨이퍼맵을 확인할 InputStage 자재가 없습니다.";
                    return false;
                }
                // 도입 전부터 매핑된 자재는 저장된 Material 맵으로 재개한다. 새 원본을 덮어쓰지 않는다.
                if (wafer.HasInputStageDieMappingResult && wafer.InputPreparedMap == null) return true;
                TapeFrameSpec frame = ResolveInputFrameSpec(wafer);
                if (frame == null)
                {
                    reason = "웨이퍼맵을 대조할 입력 웨이퍼 사양을 찾을 수 없습니다. spec=" +
                             (wafer.TapeFrameSpecName ?? "");
                    return false;
                }

                RecipeProject project = RecipeStore.LoadLastOrDefaultCached();
                WaferMapProcessSettings profile = project != null ? project.InputMapProcessing : null;
                DieMap pinned = MaterialStateService.GetPreparedInputMap(wafer, barcode, true);
                if (pinned != null)
                {
                    prepared = WaferMapProcessService.Prepare(pinned, profile, "Input");
                    string pinnedCode;
                    return TryValidateParsedMap(prepared, frame, stage, out pinnedCode, out reason);
                }

                LotWaferMapSlotInfo info;
                string fetchReason;
                if (!LotWaferMapFetchService.TryFetchWaferMapByBarcode(barcode, out info, out fetchReason))
                {
                    reason = "바코드에 해당하는 웨이퍼맵을 확보하지 못했습니다. barcode=" +
                             (barcode ?? "") + ", reason=" + fetchReason;
                    return false;
                }
                if (info == null || string.IsNullOrWhiteSpace(info.LocalPath))
                {
                    reason = "수신한 웨이퍼맵의 로컬 파일 경로가 없습니다. barcode=" + (barcode ?? "");
                    return false;
                }

                // 캐시 통계만 믿지 않고 Mapping과 동일한 포맷 파서로 새 객체를 읽는다.
                DieMap map = WaferMapProcessService.Prepare(
                    LotWaferMapFetchService.LoadConfiguredFormatOrThrow(info.LocalPath), profile, "Input");
                string failureCode;
                if (!TryValidateParsedMap(map, frame, stage, out failureCode, out reason))
                {
                    reason = reason + ", barcode=" + (barcode ?? "") + ", file=" + info.LocalPath;
                    return false;
                }
                if (!HasSameValidationFrame(frame, ResolveInputFrameSpec(wafer)))
                {
                    reason = "웨이퍼맵 사전 확인 중 입력 웨이퍼 사양이 변경되었습니다. 현재 사양으로 다시 확인하세요.";
                    return false;
                }
                prepared = map;
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                reason = "바코드 웨이퍼맵 사전 확인에 실패했습니다. barcode=" + (barcode ?? "") +
                         ", error=" + ex.Message;
                EventLogger.Write(EventKind.Warning, "SYSTEM", "INPUT-MAP-PREFLIGHT", reason);
                return false;
            }
        }

        /// <summary>사전 확인과 실제 Mapping이 공유하는 비변경 검증. IsTarget과 순번을 수정하지 않는다.</summary>
        internal static bool TryValidateParsedMap(
            DieMap map,
            TapeFrameSpec frame,
            InputStageUnit stage,
            out string failureCode,
            out string reason)
        {
            failureCode = "";
            reason = "";
            if (map == null || map.Entries == null || map.Entries.Count == 0)
            {
                failureCode = "LOT-MAP-FILE-MISSING";
                reason = "LOT 웨이퍼맵에 다이 레코드가 없습니다.";
                return false;
            }
            if (frame == null)
            {
                failureCode = "LOT-MAP-FRAME-MISMATCH";
                reason = "LOT 웨이퍼맵을 대조할 입력 웨이퍼 사양이 없습니다.";
                return false;
            }
            if (stage == null || stage.Recipe == null || stage.Recipe.DieMap == null)
            {
                failureCode = "IN-STAGE-DIEMAP-RECIPE";
                reason = "픽업 BIN 조건을 확인할 InputStage 레시피가 없습니다.";
                return false;
            }

            double centerStepX = DieMapGenerator.CalculateCenterStep(frame.DieSizeX, frame.PitchX);
            double centerStepY = DieMapGenerator.CalculateCenterStep(frame.DieSizeY, frame.PitchY);
            // 기존 RAD 제품은 헤더 피치가 중심 간격 또는 다이 크기일 수 있어 두 의미를 그대로 허용한다.
            if (!IsPitchCompatible(map.PitchX, centerStepX, frame.DieSizeX) ||
                !IsPitchCompatible(map.PitchY, centerStepY, frame.DieSizeY))
            {
                failureCode = "LOT-MAP-FRAME-MISMATCH";
                reason = "LOT 웨이퍼맵의 다이 피치가 레시피 제품과 다릅니다(다른 제품 맵 의심). " +
                         "mapPitch=(" + map.PitchX.ToString("F3") + "," + map.PitchY.ToString("F3") + ")" +
                         ", frameCenterStep=(" + centerStepX.ToString("F3") + "," + centerStepY.ToString("F3") + ")" +
                         ", frameDieSize=(" + frame.DieSizeX.ToString("F3") + "," + frame.DieSizeY.ToString("F3") + ")";
                return false;
            }

            HashSet<int> lotBins;
            bool lotFilterActive = MaterialStateService.IsPickupBinFilterActive(out lotBins);
            HashSet<int> recipeBins;
            bool recipeFilterActive = stage.Recipe.DieMap.TryGetPickupBinFilter(out recipeBins);
            int afterLotSelection = 0;
            int afterRecipeSelection = 0;
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null || !entry.IsTarget || (lotFilterActive && !lotBins.Contains(entry.BinCode)))
                    continue;
                afterLotSelection++;
                if (!recipeFilterActive || recipeBins.Contains(entry.BinCode))
                    afterRecipeSelection++;
            }

            if (afterLotSelection == 0)
            {
                failureCode = "LOT-MAP-BIN-NO-TARGET";
                reason = "이 웨이퍼맵에 LOT BIN 선택 조건을 만족하는 픽업 대상 다이가 없습니다. binSelection=" +
                         MaterialStateService.DescribePickupBinSelection();
                return false;
            }
            if (afterRecipeSelection == 0)
            {
                failureCode = "MAP-RECIPE-BIN-NO-TARGET";
                reason = "레시피 픽업 BIN 필터 적용 후 대상 다이가 없습니다. recipeBinFilter=" +
                         (stage.Recipe.DieMap.PickupBinFilterCsv ?? "");
                return false;
            }
            return true;
        }

        private static bool IsPitchCompatible(double actual, double centerStep, double dieSize)
        {
            return actual <= 0.0 ||
                   (centerStep > 0.0 && Math.Abs(actual - centerStep) <= PitchCompareToleranceMm) ||
                   (dieSize > 0.0 && Math.Abs(actual - dieSize) <= PitchCompareToleranceMm);
        }

        private static TapeFrameSpec ResolveInputFrameSpec(WaferMaterial wafer)
        {
            string specName = MaterialStateService.NormalizeInputTapeFrameSpecName(wafer.TapeFrameSpecName);
            TapeFrameSpec existing = string.IsNullOrWhiteSpace(specName) ? null : MaterialSpecs.FindFrame(specName);
            if (existing != null)
            {
                // 네트워크 대기 중 기존 사양 객체가 수정되어도 검증 시작 시점의 값을 보존한다.
                return new TapeFrameSpec
                {
                    Name = existing.Name,
                    DieMapX = existing.DieMapX,
                    DieMapY = existing.DieMapY,
                    DieSizeX = existing.DieSizeX,
                    DieSizeY = existing.DieSizeY,
                    PitchX = existing.PitchX,
                    PitchY = existing.PitchY
                };
            }

            // 기존 사양 복구 API는 MaterialSpecs 저장까지 수행하므로 후보 검사에서는 읽은 레시피의 임시 사양만 쓴다.
            RecipeProject project = RecipeStore.LoadLastOrDefault();
            TapeFrameSubset frame = project != null ? (project.InputFrame ?? project.Frame) : null;
            if (frame == null)
                return null;
            return new TapeFrameSpec
            {
                Name = frame.FrameSpecName,
                DieMapX = frame.DieMapX,
                DieMapY = frame.DieMapY,
                DieSizeX = frame.DieSizeX,
                DieSizeY = frame.DieSizeY,
                PitchX = frame.PitchX,
                PitchY = frame.PitchY
            };
        }

        private static bool HasSameValidationFrame(TapeFrameSpec expected, TapeFrameSpec current)
        {
            return expected != null && current != null &&
                   string.Equals(expected.Name, current.Name, StringComparison.Ordinal) &&
                   expected.DieMapX == current.DieMapX && expected.DieMapY == current.DieMapY &&
                   expected.DieSizeX == current.DieSizeX && expected.DieSizeY == current.DieSizeY &&
                   expected.PitchX == current.PitchX && expected.PitchY == current.PitchY;
        }
    }
}
