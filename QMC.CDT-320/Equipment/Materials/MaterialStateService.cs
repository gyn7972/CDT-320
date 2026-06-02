using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Common;
using QMC.CDT320.Recipes;

namespace QMC.CDT320.Materials
{
    public static class MaterialStateService
    {
        public static event Action<MaterialSnapshot> StateChanged;

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

        public static void PutWaferInCassette(
            string waferId,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition = double.NaN)
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

        public static void MoveDie(string dieId, MaterialLocation location)
        {
            var die = GetOrCreateDieMaterial(dieId);
            die.CurrentLocation = location ?? MaterialLocation.Unknown();
            die.UpdatedAt = DateTime.Now;
            NotifyAndSave("MoveDie");
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
                wafer.CurrentLocation = MaterialLocation.Cassette(MaterialLocationKind.InputCassette, role, i);
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

            if (MaterialSpecs.Data.Frames == null)
                MaterialSpecs.Data.Frames = new List<TapeFrameSpec>();

            var frame = project.Frame;
            var spec = MaterialSpecs.Data.Frames.FirstOrDefault(f => f.Name == specName);
            if (spec == null)
            {
                spec = new TapeFrameSpec { Name = specName };
                MaterialSpecs.Data.Frames.Add(spec);
            }

            spec.GridX = frame.GridX;
            spec.GridY = frame.GridY;
            spec.PitchX = frame.PitchX;
            spec.PitchY = frame.PitchY;
            spec.OuterDiameterMm = frame.OuterDiameterMm;
            spec.DieSpecName = project.Die != null ? project.Die.DieSpecName ?? "" : "";
            MaterialSpecs.Save();
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
