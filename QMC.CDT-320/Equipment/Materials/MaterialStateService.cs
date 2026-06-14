using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Common;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Recipes;

namespace QMC.CDT320.Materials
{
    public static class MaterialStateService
    {
        public static event Action<MaterialSnapshot> StateChanged;
        private static readonly object _stateSync = new object();

        public static MaterialSnapshot State => MaterialStorage.State;

        public static void InitializeForRecipe(int inputLevelCount, int goodLevelCount, int inputSlots, int outputSlots)
        {
            MaterialStorage.InitializeDefaultState(inputLevelCount, goodLevelCount, inputSlots, outputSlots);
            NotifyAndSave("InitializeForRecipe");
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

        public static DieMaterial GetDieAtPicker(MaterialLocationKind pickerLocation, int pickerNo)
        {
            try
            {
                lock (_stateSync)
                {
                    return State.Dies.FirstOrDefault(d =>
                        d != null &&
                        d.CurrentLocation != null &&
                        d.CurrentLocation.Kind == pickerLocation &&
                        d.CurrentLocation.PickerNo == pickerNo);
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

                    die.Result = result;
                    if (result == DieResult.NG && !string.IsNullOrWhiteSpace(ngCode))
                    {
                        if (die.NgCodes == null)
                            die.NgCodes = new List<string>();
                        if (!die.NgCodes.Contains(ngCode))
                            die.NgCodes.Add(ngCode);
                    }

                    die.UpdatedAt = DateTime.Now;
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
            if (slotCount < 0) slotCount = 0;

            if (levelCount >= 2 && level2Map == null)
                level2Map = level1Map;

            UpdateCassetteMapping(CassetteMaterialRole.Input1, true, slotCount, level1Map, level1SlotPositions, cassetteLotId, tapeFrameSpecName);
            UpdateCassetteMapping(CassetteMaterialRole.Input2, levelCount >= 2, slotCount, level2Map, level2SlotPositions, cassetteLotId, tapeFrameSpecName);

            State.LotId = cassetteLotId ?? State.LotId;
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
            if (slotCount < 0) slotCount = 0;

            UpdateCassetteMapping(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions, cassetteLotId, tapeFrameSpecName);
            UpdateCassetteMapping(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions, cassetteLotId, tapeFrameSpecName);
            UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions, cassetteLotId, tapeFrameSpecName);

            State.LotId = cassetteLotId ?? State.LotId;
            NotifyAndSave("OutputCassetteMapping");
        }

        public static string ResolveRecipeTapeFrameSpecName(int inchSelect)
        {
            var project = RecipeStore.LoadLastOrDefault();
            if (project == null || project.Frame == null)
                return ResolveDefaultTapeFrameSpecName(inchSelect);

            var frame = project.Frame;
            string specName = string.IsNullOrWhiteSpace(frame.FrameSpecName)
                ? ResolveDefaultTapeFrameSpecName(inchSelect)
                : frame.FrameSpecName.Trim();

            EnsureTapeFrameSpecFromRecipe(project, specName);
            return specName;
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
                if (project == null || project.Frame == null)
                    return "";

                string specName = string.IsNullOrWhiteSpace(project.Frame.FrameSpecName)
                    ? ResolveDefaultTapeFrameSpecName(0)
                    : project.Frame.FrameSpecName.Trim();

                EnsureTapeFrameSpecFromRecipe(project, specName);
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
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null) return;
            cassette.EnsureSlots();
            if (slotNumber < 0 || slotNumber >= cassette.Slots.Count) return;

            var wafer = GetOrCreateWafer(waferId);
            wafer.CassetteLotId = cassetteLotId ?? "";
            wafer.CurrentLocation = MaterialLocation.Cassette(
                cassetteRole == CassetteMaterialRole.Input1 || cassetteRole == CassetteMaterialRole.Input2
                    ? MaterialLocationKind.InputCassette
                    : MaterialLocationKind.OutputCassette,
                cassetteRole,
                slotNumber);
            ApplyWaferCassettePosition(wafer, slotPosition);
            if (updateState)
                wafer.State = WaferMaterialStateText.Normalize(state);
            wafer.UpdatedAt = DateTime.Now;

            cassette.CassetteLotId = cassetteLotId ?? cassette.CassetteLotId;
            cassette.Slots[slotNumber].WaferId = wafer.WaferId;
            cassette.Slots[slotNumber].HasWafer = true;
            cassette.IsMapped = true;
            cassette.LastScanTime = DateTime.Now;

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
                wafer.TapeFrameSpecName = ResolveRecipeTapeFrameSpecName(0);
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
            var wafer = GetOrCreateWafer(waferId);
            RemoveWaferFromCassetteSlot(wafer.WaferId);
            wafer.CurrentLocation = location ?? MaterialLocation.Unknown();
            wafer.State = WaferMaterialStateText.Normalize(state);
            wafer.UpdatedAt = DateTime.Now;
            NotifyAndSave("MoveWafer");
        }

        public static bool InitializeOutputStageReceivePlan(QMC.CDT320.BinSide side)
        {
            try
            {
                WaferMaterial outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                if (outputWafer == null)
                    return false;

                WaferMaterial sourceWafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                DieMap sourceMap = BuildDieMapFromWafer(sourceWafer);
                if (sourceWafer == null || sourceMap == null || sourceMap.GridX <= 0 || sourceMap.GridY <= 0)
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "Output receive plan initialize skipped: input stage die map is empty. side=" + side + " - Check");
                    return false;
                }

                var project = RecipeStore.LoadLastOrDefault();
                PickupSubset pickup = project != null && project.Pickup != null ? project.Pickup : new PickupSubset();
                List<DieMapEntry> ordered = BuildOutputReceiveOrder(sourceMap, pickup);
                if (ordered.Count == 0)
                    return false;

                outputWafer.OutputReceiveSourceWaferId = sourceWafer.WaferId;
                outputWafer.OutputReceiveGridX = sourceMap.GridX;
                outputWafer.OutputReceiveGridY = sourceMap.GridY;
                outputWafer.OutputReceivePitchX = sourceMap.PitchX;
                outputWafer.OutputReceivePitchY = sourceMap.PitchY;
                outputWafer.OutputReceiveOriginX = 0.0;
                outputWafer.OutputReceiveOriginY = 0.0;
                outputWafer.OutputReceiveNextIndex = 0;
                outputWafer.OutputReceiveTotalCount = ordered.Count;
                outputWafer.OutputReceiveStartCorner = pickup.StartCorner.ToString();
                outputWafer.OutputReceiveDirection = pickup.Direction.ToString();
                outputWafer.OutputReceivePattern = pickup.Pattern.ToString();
                if (outputWafer.DieIds == null)
                    outputWafer.DieIds = new List<string>();
                else
                    outputWafer.DieIds.Clear();
                outputWafer.State = WaferMaterialState.WorkReady;
                outputWafer.UpdatedAt = DateTime.Now;

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

                    if (outputWafer.OutputReceiveTotalCount <= 0 || string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferId))
                    {
                        if (!InitializeOutputStageReceivePlan(side))
                            return null;

                        outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                        if (outputWafer == null || outputWafer.OutputReceiveTotalCount <= 0)
                            return null;
                    }

                    WaferMaterial sourceWafer = State.Wafers.FirstOrDefault(w =>
                        w != null &&
                        string.Equals(w.WaferId, outputWafer.OutputReceiveSourceWaferId, StringComparison.OrdinalIgnoreCase));
                    DieMap sourceMap = BuildDieMapFromWafer(sourceWafer);
                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = project != null && project.Pickup != null ? project.Pickup : new PickupSubset();
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(sourceMap, pickup);
                    if (ordered.Count == 0)
                        return null;

                    int index = outputWafer.DieIds != null
                        ? outputWafer.DieIds.Count(id => !string.IsNullOrWhiteSpace(id))
                        : 0;

                    if (index >= ordered.Count)
                        return null;

                    DieMapEntry entry = ordered[index];
                    double pitchX = outputWafer.OutputReceivePitchX > 0.0 ? outputWafer.OutputReceivePitchX : sourceMap.PitchX;
                    double pitchY = outputWafer.OutputReceivePitchY > 0.0 ? outputWafer.OutputReceivePitchY : sourceMap.PitchY;

                    var target = new OutputStageReceiveTarget
                    {
                        StageLocation = ResolveOutputStageLocation(side),
                        OutputWaferId = outputWafer.WaferId,
                        SourceWaferId = outputWafer.OutputReceiveSourceWaferId,
                        OrderIndex = index,
                        GridX = entry.GridX,
                        GridY = entry.GridY,
                        OffsetX = outputWafer.OutputReceiveOriginX + pitchX * entry.GridX,
                        OffsetY = outputWafer.OutputReceiveOriginY + pitchY * entry.GridY
                    };
                    target.TargetX = target.OffsetX;
                    target.TargetY = target.OffsetY;

                    outputWafer.OutputReceiveNextIndex = index;
                    outputWafer.UpdatedAt = DateTime.Now;
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

        public static bool MoveDieToOutputStage(string dieId, QMC.CDT320.BinSide side)
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
                    die.CurrentLocation = new MaterialLocation { Kind = stageLocation };
                    die.Result = side == QMC.CDT320.BinSide.Ng ? DieResult.NG : DieResult.Good;
                    die.WaferID_Output = outputWafer.WaferId;
                    die.UpdatedAt = DateTime.Now;

                    if (outputWafer.DieIds == null)
                        outputWafer.DieIds = new List<string>();
                    if (!outputWafer.DieIds.Any(id => string.Equals(id, dieId, StringComparison.OrdinalIgnoreCase)))
                        outputWafer.DieIds.Add(dieId);

                    outputWafer.OutputReceiveNextIndex = outputWafer.DieIds.Count(id => !string.IsNullOrWhiteSpace(id));
                    outputWafer.OutputGrade = side == QMC.CDT320.BinSide.Ng ? DieResult.NG : DieResult.Good;
                    outputWafer.State = IsOutputStageReceiveComplete(outputWafer)
                        ? WaferMaterialState.Finish
                        : WaferMaterialState.Working;
                    outputWafer.UpdatedAt = DateTime.Now;
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

        public static bool IsOutputStageReceiveAvailable(QMC.CDT320.BinSide side)
        {
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial outputWafer = GetWaferAtLocation(ResolveOutputStageLocation(side));
                    WaferMaterialState state = outputWafer != null
                        ? WaferMaterialStateText.Normalize(outputWafer.State)
                        : WaferMaterialState.Empty;
                    return outputWafer != null &&
                           state != WaferMaterialState.Finish &&
                           !IsOutputStageReceiveComplete(outputWafer);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output stage receive available check failed: " + ex.Message + " - Failed");
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

            int total = outputWafer.OutputReceiveTotalCount;
            if (total <= 0)
                return false;

            int placed = outputWafer.DieIds != null ? outputWafer.DieIds.Count(id => !string.IsNullOrWhiteSpace(id)) : 0;
            return placed >= total;
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
                .Where(e => e != null && e.GridX >= 0 && e.GridY >= 0)
                .OrderBy(e => e.GridY)
                .ThenBy(e => e.GridX)
                .ToList();
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
                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null || map.Entries.Count == 0)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateService",
                            "Input pick target reserve skipped: input stage die map is empty. - Check");
                        return null;
                    }

                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = project != null && project.Pickup != null ? project.Pickup : new PickupSubset();
                    List<DieMapEntry> ordered = BuildOutputReceiveOrder(map, pickup);
                    if (ordered == null || ordered.Count == 0)
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

                        if (die.Result == DieResult.NG)
                            continue;

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
                            GridX = entry.GridX,
                            GridY = entry.GridY,
                            OffsetX = entry.X,
                            OffsetY = entry.Y,
                            TargetX = entry.X,
                            TargetY = entry.Y,
                            PickerNo = pickerNo,
                            PickerLocation = pickerLocation
                        };

                        NotifyAndSave("ReserveInputStagePickTarget");
                        return target;
                    }

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

        public static bool HasReadyInputStagePickTarget()
        {
            try
            {
                lock (_stateSync)
                {
                    WaferMaterial wafer = GetWaferAtLocation(MaterialLocationKind.InputStage);
                    DieMap map = BuildDieMapFromWafer(wafer);
                    if (wafer == null || map == null || map.Entries == null || map.Entries.Count == 0)
                        return false;

                    var project = RecipeStore.LoadLastOrDefault();
                    PickupSubset pickup = project != null && project.Pickup != null ? project.Pickup : new PickupSubset();
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

                        if (die.Result == DieResult.NG)
                            continue;

                        if (IsDieReservedForPicker(die))
                            continue;

                        if (die.CurrentLocation == null ||
                            die.CurrentLocation.Kind == MaterialLocationKind.Unknown ||
                            die.CurrentLocation.Kind == MaterialLocationKind.InputStage)
                        {
                            return true;
                        }
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
                        if (die == null || die.Result == DieResult.NG)
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

        public static void SaveInputStageAlignResult(WaferMaterial wafer, double originX, double originY, double pitchX, double pitchY, double offsetX, double offsetY)
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
                double originX = wafer.HasInputStageAlignResult ? wafer.InputStageAlignOriginX : ResolveOrigin(dies, true);
                double originY = wafer.HasInputStageAlignResult ? wafer.InputStageAlignOriginY : ResolveOrigin(dies, false);

                var map = new DieMap
                {
                    FrameObjId = string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId) ? wafer.WaferId : wafer.DieMapFrameObjId,
                    GridX = maxX + 1,
                    GridY = maxY + 1,
                    PitchX = pitchX,
                    PitchY = pitchY,
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
                        GridX = die.Wafer_IndexX,
                        GridY = die.Wafer_IndexY,
                        IsTarget = die.Result != DieResult.NG,
                        Result = die.Result,
                        BinCode = die.Input_BinCode,
                        X = die.WaferOffset != null && die.WaferOffset.IsValid ? die.WaferOffset.X : originX + pitchX * die.Wafer_IndexX,
                        Y = die.WaferOffset != null && die.WaferOffset.IsValid ? die.WaferOffset.Y : originY + pitchY * die.Wafer_IndexY,
                        DieUid = die.DieId
                    });
                }

                return map;
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

        public static WaferMapData BuildWaferMapDataFromWafer(WaferMaterial wafer)
        {
            try
            {
                DieMap dieMap = BuildDieMapFromWafer(wafer);
                if (dieMap == null || dieMap.GridX <= 0 || dieMap.GridY <= 0)
                    return null;

                var map = new WaferMapData
                {
                    WaferId = wafer != null ? wafer.WaferId : "",
                    ColumnCount = dieMap.GridX,
                    RowCount = dieMap.GridY,
                    DieMap = new bool[dieMap.GridY, dieMap.GridX],
                    Ref1Row = dieMap.GridY / 2,
                    Ref1Col = Math.Max(0, dieMap.GridX / 4),
                    Ref2Row = dieMap.GridY / 2,
                    Ref2Col = dieMap.GridX > 1 ? Math.Min(dieMap.GridX - 1, (dieMap.GridX * 3) / 4) : 0
                };

                foreach (DieMapEntry entry in dieMap.Entries)
                {
                    if (entry == null || entry.GridX < 0 || entry.GridY < 0 || entry.GridX >= map.ColumnCount || entry.GridY >= map.RowCount)
                        continue;
                    map.DieMap[entry.GridY, entry.GridX] = entry.IsTarget;
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

            IEnumerable<DieMaterial> query = State.Dies.Where(d =>
                d != null &&
                string.Equals(d.WaferID_Input, wafer.WaferId, StringComparison.OrdinalIgnoreCase) &&
                d.Wafer_IndexX >= 0 &&
                d.Wafer_IndexY >= 0);

            if (wafer.DieIds != null && wafer.DieIds.Count > 0)
            {
                var dieIds = new HashSet<string>(wafer.DieIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
                query = query.Where(d => dieIds.Contains(d.DieId));
            }

            return query.ToList();
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
            die.CurrentLocation = location ?? MaterialLocation.Unknown();
            die.ReservedPickerLocation = MaterialLocationKind.Unknown;
            die.ReservedPickerNo = -1;
            die.UpdatedAt = DateTime.Now;
            NotifyAndSave("MoveDie");
        }

        private static bool IsDieReservedForPicker(DieMaterial die)
        {
            if (die == null)
                return false;

            return (die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                    die.ReservedPickerLocation == MaterialLocationKind.PickerRear) &&
                   die.ReservedPickerNo > 0;
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

        public static void NotifyAndSave(string reason)
        {
            try
            {
                State.SaveReason = reason ?? "";
                State.SavedAt = DateTime.Now;
                NormalizeSnapshotHeader(State);
                if (!MaterialSnapshotStore.Save(State))
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material state save failed. reason=" + State.SaveReason + ", file=" + MaterialSnapshotStore.SnapshotPath + " - Failed");
                }

                try { StateChanged?.Invoke(State); } catch { }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material state save failed: " + ex.Message + " - Failed");
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
            cassette.IsEnabled = enabled;
            cassette.IsPresent = enabled;
            cassette.IsMapped = enabled && map != null;
            cassette.CassetteLotId = cassetteLotId ?? "";
            cassette.LastScanTime = enabled && map != null ? DateTime.Now : cassette.LastScanTime;
            cassette.SlotCount = slotCount;
            cassette.EnsureSlots();

            foreach (var slot in cassette.Slots)
            {
                slot.WaferId = "";
                slot.HasWafer = false;
            }

            if (!enabled || map == null)
                return;

            int count = Math.Min(slotCount, map.Count);
            for (int i = 0; i < count; i++)
            {
                if (!map[i])
                    continue;

                string waferId = BuildGeneratedWaferId(role, i);
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

                wafer.CassetteLotId = cassetteLotId ?? "";
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
                wafer.UpdatedAt = DateTime.Now;

                cassette.Slots[i].WaferId = wafer.WaferId;
                cassette.Slots[i].HasWafer = true;
            }
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

            if (MaterialSpecs.Data == null)
                return;

            EnsureDieSpecFromRecipe(project, project.Die != null ? project.Die.DieSpecName : "");

            var frame = project.Frame;
            MaterialSpecs.UpsertFrame(
                specName,
                frame.GridX,
                frame.GridY,
                frame.PitchX,
                frame.PitchY,
                frame.OuterDiameterMm,
                project.Die != null ? project.Die.DieSpecName ?? "" : "");
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

            if (!string.IsNullOrEmpty(cassetteLotId))
            {
                wafer.CassetteLotId = cassetteLotId;
                cassette.CassetteLotId = cassetteLotId;
            }
            else if (!string.IsNullOrEmpty(cassette.CassetteLotId))
            {
                wafer.CassetteLotId = cassette.CassetteLotId;
            }

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
