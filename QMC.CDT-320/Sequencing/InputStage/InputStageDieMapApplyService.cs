using System;
using System.Collections.Generic;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

namespace QMC.CDT320.Sequencing
{
    internal sealed class InputStageDieMapApplyRequest
    {
        public InputStageUnit Stage { get; set; }
        public MachineController Controller { get; set; }
        public SequenceSignalBus Bus { get; set; }
        public DieMap DieMap { get; set; }
        public WaferMapData WaferMap { get; set; }
        public WaferMaterial ExpectedWafer { get; set; }
        public PickupSubset PickupOptions { get; set; }
        public string Source { get; set; }
        public string SaveReason { get; set; }
        public bool PublishReadySignals { get; set; }
    }

    internal sealed class InputStageDieMapApplyResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public WaferMaterial Wafer { get; set; }
        public DieMap DieMap { get; set; }
        public WaferMapData WaferMap { get; set; }
        public double MappingOffsetX { get; set; }
        public double MappingOffsetY { get; set; }
        public int CreatedDieCount { get; set; }
        public int FullDieCount { get; set; }
    }

    internal static class InputStageDieMapApplyService
    {
        public static InputStageDieMapApplyResult Apply(InputStageDieMapApplyRequest request)
        {
            var result = new InputStageDieMapApplyResult();
            try
            {
                if (request == null)
                    return Fail(result, "InputStage die map apply request is null.");
                if (request.Stage == null)
                    return Fail(result, "InputStageUnit is null.");
                if (request.DieMap == null || request.DieMap.Entries == null || request.DieMap.Entries.Count == 0)
                    return Fail(result, "Die map result is not available.");

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer == null)
                    return Fail(result, "Die Mapping 결과를 저장할 InputStage Material을 찾을 수 없습니다.");

                if (request.ExpectedWafer != null &&
                    !string.IsNullOrWhiteSpace(request.ExpectedWafer.WaferId) &&
                    !string.Equals(request.ExpectedWafer.WaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                {
                    return Fail(result,
                        "Die Mapping 대상 Wafer와 MaterialState InputStage Wafer가 다릅니다. sequenceWafer=" +
                        request.ExpectedWafer.WaferId +
                        ", stateWafer=" + wafer.WaferId);
                }

                request.Stage.SetCurrentWaferMaterial(wafer);

                PickupSubset pickup = request.PickupOptions ?? ResolveInputPickupSubset();
                PickupSequenceGenerator.ApplySequenceNumbers(request.DieMap, pickup);
                DieMapGenerator.Normalize(request.DieMap);

                WaferMapData waferMap = request.WaferMap ?? BuildWaferMapDataFromDieMap(request.DieMap, wafer);
                double alignOriginX = wafer.HasInputStageAlignResult
                    ? wafer.InputStageAlignOriginX
                    : request.Stage.OriginX;
                double alignOriginY = wafer.HasInputStageAlignResult
                    ? wafer.InputStageAlignOriginY
                    : request.Stage.OriginY;
                double mappingOffsetX = request.DieMap.OriginX - alignOriginX;
                double mappingOffsetY = request.DieMap.OriginY - alignOriginY;

                request.Stage.ApplyDieMappingResult(
                    waferMap,
                    request.DieMap.OriginX,
                    request.DieMap.OriginY,
                    request.DieMap.PitchX,
                    request.DieMap.PitchY,
                    mappingOffsetX,
                    mappingOffsetY);

                LotStorage.ActiveInputDieMap = request.DieMap;
                if (request.Controller != null)
                {
                    request.Controller.PickupOptions = pickup;
                    request.Controller.ApplyInputDieMap(
                        request.DieMap,
                        string.IsNullOrWhiteSpace(request.Source)
                            ? "InputStageDieMapApplyService"
                            : request.Source);
                }

                int fullDieCount = CountMapEntries(request.DieMap);
                int targetDieCount = ApplyDieMaterials(request.DieMap, wafer);
                ApplyWaferDieMapResult(
                    request.Stage,
                    wafer,
                    request.DieMap,
                    mappingOffsetX,
                    mappingOffsetY,
                    ResolveInputMapApprovalHash(request.Controller));

                MaterialStateService.NotifyAndSave(string.IsNullOrWhiteSpace(request.SaveReason)
                    ? "InputStageDieMapping"
                    : request.SaveReason);

                if (request.PublishReadySignals && request.Bus != null)
                {
                    request.Bus.Set("InputStageDieMapped");
                    request.Bus.Set("InputStageFinishComplete");
                    request.Bus.Set("InputStageReady");
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageDieMapApplyService",
                    "Input stage die map applied. source=" + (request.Source ?? "") +
                    ", wafer=" + (wafer != null ? wafer.WaferId : "") +
                    ", frame=" + (request.DieMap.FrameObjId ?? "") +
                    ", dieMapX=" + request.DieMap.DieMapX +
                    ", dieMapY=" + request.DieMap.DieMapY +
                    ", targetDieCount=" + targetDieCount +
                    ", fullDieCount=" + fullDieCount +
                    ", alignOriginX=" + alignOriginX.ToString("F6") +
                    ", alignOriginY=" + alignOriginY.ToString("F6") +
                    ", mappingOriginX=" + request.DieMap.OriginX.ToString("F6") +
                    ", mappingOriginY=" + request.DieMap.OriginY.ToString("F6") +
                    ", offsetX=" + mappingOffsetX.ToString("F6") +
                    ", offsetY=" + mappingOffsetY.ToString("F6") + " - Ok");

                result.Success = true;
                result.Wafer = wafer;
                result.DieMap = request.DieMap;
                result.WaferMap = waferMap;
                result.MappingOffsetX = mappingOffsetX;
                result.MappingOffsetY = mappingOffsetY;
                result.CreatedDieCount = targetDieCount;
                result.FullDieCount = fullDieCount;
                return result;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageDieMapApplyService",
                    "Input stage die map apply failed: " + ex.Message + " - Failed");
                return Fail(result, ex.Message);
            }
            finally
            {
            }
        }

        public static WaferMapData BuildWaferMapDataFromDieMap(DieMap map, WaferMaterial wafer)
        {
            if (map == null || map.DieMapX <= 0 || map.DieMapY <= 0)
                return null;

            var waferMap = new WaferMapData
            {
                WaferId = wafer != null ? wafer.WaferId : (map.FrameObjId ?? ""),
                ColumnCount = map.DieMapX,
                RowCount = map.DieMapY,
                DieMap = new bool[map.DieMapY, map.DieMapX],
                Ref1Row = map.DieMapY / 2,
                Ref1Col = Math.Max(0, map.DieMapX / 4),
                Ref2Row = map.DieMapY / 2,
                Ref2Col = map.DieMapX > 1 ? Math.Min(map.DieMapX - 1, (map.DieMapX * 3) / 4) : 0
            };

            if (map.Entries != null)
            {
                foreach (DieMapEntry entry in map.Entries)
                {
                    if (entry == null)
                        continue;

                    int mapX = DieMapGenerator.ResolveMapIndexX(entry);
                    int mapY = DieMapGenerator.ResolveMapIndexY(entry);
                    if (mapX < 0 || mapY < 0 || mapX >= waferMap.ColumnCount || mapY >= waferMap.RowCount)
                        continue;

                    waferMap.DieMap[mapY, mapX] = entry.IsTarget;
                }
            }

            return waferMap;
        }

        private static InputStageDieMapApplyResult Fail(InputStageDieMapApplyResult result, string message)
        {
            result.Success = false;
            result.ErrorMessage = message ?? "";
            return result;
        }

        private static PickupSubset ResolveInputPickupSubset()
        {
            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return new PickupSubset();

                if (project.InputPickup != null)
                    return project.InputPickup;
                if (project.Pickup != null)
                    return project.Pickup;
                return new PickupSubset();
            }
            catch
            {
                return new PickupSubset();
            }
            finally
            {
            }
        }

        private static void ApplyWaferDieMapResult(
            InputStageUnit stage,
            WaferMaterial wafer,
            DieMap map,
            double mappingOffsetX,
            double mappingOffsetY,
            string inputMapApprovalHash)
        {
            if (wafer == null || map == null)
                return;

            wafer.DieMapFrameObjId = map.FrameObjId;
            string inputSpecName = MaterialStateService.ResolveInputTapeFrameSpecName(0);
            if (!string.IsNullOrWhiteSpace(inputSpecName))
                wafer.TapeFrameSpecName = inputSpecName;
            wafer.HasInputStageAlignResult = true;
            wafer.InputStageAlignPitchX = map.PitchX;
            wafer.InputStageAlignPitchY = map.PitchY;
            wafer.InputStageDieSizeX = map.DieSizeX;
            wafer.InputStageDieSizeY = map.DieSizeY;
            wafer.InputStageOuterDiameterMm = map.OuterDiameterMm;

            if (stage != null)
            {
                wafer.InputStageAlignOffsetX = stage.WaferAlignOffsetX;
                wafer.InputStageAlignOffsetY = stage.WaferAlignOffsetY;
                if (stage.HasWaferAlignThetaResult)
                {
                    wafer.HasInputStageThetaAlignResult = true;
                    wafer.InputStageAlignReferenceT = stage.WaferAlignReferenceT;
                    wafer.InputStageAlignCorrectedT = stage.WaferAlignCorrectedT;
                    wafer.InputStageAlignOffsetT = stage.WaferAlignOffsetT;
                }
            }

            wafer.HasInputStageDieMappingResult = true;
            wafer.InputStageDieMappingOffsetX = mappingOffsetX;
            wafer.InputStageDieMappingOffsetY = mappingOffsetY;
            wafer.HasInputStageDieMappingOrigin = true;
            wafer.InputStageDieMappingOriginX = map.OriginX;
            wafer.InputStageDieMappingOriginY = map.OriginY;
            wafer.HasInputStageDieMappingThetaSnapshot = wafer.HasInputStageThetaAlignResult;
            wafer.InputStageDieMappingCorrectedT = wafer.InputStageAlignCorrectedT;
            wafer.InputStageDieMappingInvalidatedByAlignChange = false;
            wafer.InputMapApprovalHashAtMapping = inputMapApprovalHash ?? "";
            wafer.State = WaferMaterialState.Working;
            wafer.UpdatedAt = DateTime.Now;
        }

        private static string ResolveInputMapApprovalHash(MachineController controller)
        {
            try
            {
                RecipeProject project = controller != null && !string.IsNullOrWhiteSpace(controller.ActiveRecipeName)
                    ? RecipeStore.Load(controller.ActiveRecipeName)
                    : RecipeStore.LoadLastOrDefault();
                return project != null && project.MapApprovalVersion > 0
                    ? project.InputMapApprovalHash ?? ""
                    : "";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private static int ApplyDieMaterials(DieMap map, WaferMaterial wafer)
        {
            if (map == null || wafer == null)
                return 0;

            if (wafer.DieIds == null)
                wafer.DieIds = new List<string>();

            wafer.DieIds.Clear();

            int count = 0;
            var activeDieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;

                int mapX = DieMapGenerator.ResolveMapIndexX(entry);
                int mapY = DieMapGenerator.ResolveMapIndexY(entry);
                int originalX = DieMapGenerator.ResolveOriginalMapIndexX(entry);
                int originalY = DieMapGenerator.ResolveOriginalMapIndexY(entry);
                string dieId = string.IsNullOrWhiteSpace(entry.DieUid)
                    ? BuildDieId(wafer, mapY, mapX)
                    : entry.DieUid;

                entry.DieUid = dieId;
                entry.DieMapX = mapX;
                entry.DieMapY = mapY;
                entry.OriginalMapX = originalX;
                entry.OriginalMapY = originalY;

                DieMaterial die = MaterialStateService.GetOrCreateDieMaterial(dieId);
                die.WaferID_Input = wafer.WaferId;
                die.WaferID_Output = "";
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
                // 현재 기준: 새 Input 맵 적용 시 이전 wafer의 Pick/검사 이력은 사용하지 않는다.
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

                activeDieIds.Add(dieId);
                wafer.DieIds.Add(dieId);
                if (entry.IsTarget)
                    count++;
            }

            MaterialStateService.ClearStaleInputDieMaterialsForWafer(
                wafer.WaferId,
                activeDieIds,
                "InputStageDieMapApplyService.ApplyDieMaterials");

            return count;
        }

        private static int CountMapEntries(DieMap map)
        {
            if (map == null || map.Entries == null)
                return 0;

            int count = 0;
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry != null)
                    count++;
            }

            return count;
        }

        private static string BuildDieId(WaferMaterial wafer, int row, int col)
        {
            string waferId = wafer != null && !string.IsNullOrWhiteSpace(wafer.WaferId) ? wafer.WaferId : "WAFER";
            return waferId + "-D" + row.ToString("000") + "-" + col.ToString("000");
        }
    }
}
