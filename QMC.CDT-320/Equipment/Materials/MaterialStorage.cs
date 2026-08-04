using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QMC.CDT320.Materials
{
    /// <summary>
    /// 머터리얼 스토리지 — 모든 Die / DieTapeFrame 을 ObjId 기반으로 보관.
    /// 310 의 <c>MaterialStorage</c> 와 동일 역할이지만 SoftBricks 의존성 없음.
    /// </summary>
    public static class MaterialStorage
    {
        private static readonly ConcurrentDictionary<string, Die>           _dies   = new ConcurrentDictionary<string, Die>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, DieTapeFrame>  _frames = new ConcurrentDictionary<string, DieTapeFrame>(StringComparer.Ordinal);
        private static MaterialSnapshot _state = CreateDefaultState(1, 1, 25, 25);

        public static IReadOnlyDictionary<string, Die>          Dies   => _dies;
        public static IReadOnlyDictionary<string, DieTapeFrame> Frames => _frames;
        public static MaterialSnapshot State => _state;

        public static MaterialSnapshot CreateDefaultState(int inputLevelCount, int goodLevelCount, int inputSlots, int outputSlots)
        {
            if (inputLevelCount < 1) inputLevelCount = 1;
            if (inputLevelCount > 2) inputLevelCount = 2;
            if (goodLevelCount < 1) goodLevelCount = 1;
            if (goodLevelCount > 2) goodLevelCount = 2;

            var snapshot = new MaterialSnapshot
            {
                SaveReason = "DefaultState",
                SavedAt = DateTime.Now
            };

            snapshot.Cassettes.Add(CreateCassette(CassetteMaterialRole.Input1, 1, true, inputSlots));
            snapshot.Cassettes.Add(CreateCassette(CassetteMaterialRole.Input2, 2, inputLevelCount >= 2, inputSlots));
            snapshot.Cassettes.Add(CreateCassette(CassetteMaterialRole.Good1, 1, true, outputSlots));
            snapshot.Cassettes.Add(CreateCassette(CassetteMaterialRole.Good2, 2, goodLevelCount >= 2, outputSlots));
            snapshot.Cassettes.Add(CreateCassette(CassetteMaterialRole.Ng1, 1, true, outputSlots));
            return snapshot;
        }

        public static void InitializeDefaultState(int inputLevelCount, int goodLevelCount, int inputSlots, int outputSlots)
        {
            _state = CreateDefaultState(inputLevelCount, goodLevelCount, inputSlots, outputSlots);
        }

        public static bool RestoreLastSnapshot()
        {
            var loaded = MaterialSnapshotStore.Load();
            if (loaded == null) return false;
            string reason;
            if (!TryPrepareStateForUse(loaded, out reason))
                return false;
            _state = loaded;
            return true;
        }

        public static void ReplaceState(MaterialSnapshot snapshot)
        {
            if (snapshot == null) return;
            string reason;
            if (!TryPrepareStateForUse(snapshot, out reason))
                throw new InvalidDataException("Material snapshot graph is invalid: " + reason);
            _state = snapshot;
        }

        // ── Die ──
        public static Die GetOrCreateDie(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            return _dies.GetOrAdd(uid, k => new Die { Uid = k });
        }

        public static Die GetDie(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            _dies.TryGetValue(uid, out var d);
            return d;
        }

        public static bool RemoveDie(string uid)
            => _dies.TryRemove(uid, out _);

        public static void AddDie(Die die)
        {
            if (die == null || string.IsNullOrEmpty(die.Uid)) return;
            _dies[die.Uid] = die;
        }

        // ── DieTapeFrame ──
        public static DieTapeFrame GetOrCreateFrame(string objId)
        {
            if (string.IsNullOrEmpty(objId)) return null;
            return _frames.GetOrAdd(objId, k => new DieTapeFrame { ObjId = k });
        }

        public static DieTapeFrame GetFrame(string objId)
        {
            if (string.IsNullOrEmpty(objId)) return null;
            _frames.TryGetValue(objId, out var f);
            return f;
        }

        public static void AddFrame(DieTapeFrame frame)
        {
            if (frame == null || string.IsNullOrEmpty(frame.ObjId)) return;
            _frames[frame.ObjId] = frame;
        }

        public static bool RemoveFrame(string objId)
            => _frames.TryRemove(objId, out _);

        public static object GetByObjId(string objId)
        {
            // 310 호환 — Die 또는 Frame 타입 어느 쪽이든 반환.
            if (string.IsNullOrEmpty(objId)) return null;
            if (_frames.TryGetValue(objId, out var f)) return f;
            if (_dies  .TryGetValue(objId, out var d)) return d;
            return null;
        }

        public static void Clear()
        {
            _dies.Clear();
            _frames.Clear();
            _state = CreateDefaultState(1, 1, 25, 25);
        }

        private static CassetteMaterial CreateCassette(CassetteMaterialRole role, int level, bool enabled, int slots)
        {
            var cassette = new CassetteMaterial
            {
                CassetteId = role.ToString(),
                Role = role,
                Level = level,
                IsEnabled = enabled,
                IsPresent = false,
                IsMapped = false,
                SlotCount = slots
            };
            cassette.EnsureSlots();
            return cassette;
        }

        private static void Normalize(MaterialSnapshot snapshot)
        {
            snapshot.LotId = string.IsNullOrWhiteSpace(snapshot.LotId)
                ? ""
                : snapshot.LotId.Trim();
            if (snapshot.Cassettes == null) snapshot.Cassettes = new List<CassetteMaterial>();
            if (snapshot.Wafers == null) snapshot.Wafers = new List<WaferMaterial>();
            if (snapshot.Dies == null) snapshot.Dies = new List<DieMaterial>();

            foreach (var cassette in snapshot.Cassettes.Where(item => item != null))
            {
                if (cassette.Slots == null) cassette.Slots = new List<CassetteSlotMaterial>();
                cassette.EnsureSlots();
            }

            foreach (var wafer in snapshot.Wafers.Where(item => item != null))
            {
                if (wafer.CurrentLocation == null)
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                if (wafer.SourceSlotNumber < 0)
                    wafer.SourceCassetteSlotPosition = double.NaN;
                if (wafer.CurrentLocation == null || wafer.CurrentLocation.SlotNumber < 0)
                    wafer.CurrentCassetteSlotPosition = double.NaN;
            }
            foreach (var die in snapshot.Dies.Where(d => d != null && d.CurrentLocation == null))
                die.CurrentLocation = MaterialLocation.Unknown();
        }

        internal static bool TryPrepareStateForUse(MaterialSnapshot snapshot, out string reason)
        {
            reason = "";
            if (snapshot == null)
            {
                reason = "snapshot이 null입니다.";
                return false;
            }

            if (!TryValidateRawSnapshotStructure(snapshot, out reason))
                return false;
            if (!MaterialStateCompactor.TryValidateRawMaterialValues(snapshot, out reason))
                return false;

            Normalize(snapshot);
            snapshot.SnapshotRevision = MaterialSnapshotRevisionPolicy.Normalize(snapshot.SnapshotRevision);
            if (!MaterialSnapshotRevisionPolicy.IsTrustedLoadedRevision(snapshot.SnapshotRevision))
            {
                reason = "SnapshotRevision이 신뢰 가능한 로드 범위를 벗어났습니다. revision=" +
                         snapshot.SnapshotRevision;
                return false;
            }
            NormalizeMaterialIdentities(snapshot);
            MaterialStateCompactor.Compact(snapshot);
            return MaterialStateCompactor.TryValidateForSave(snapshot, out reason);
        }

        private static bool TryValidateRawSnapshotStructure(MaterialSnapshot snapshot, out string reason)
        {
            reason = "";
            if (snapshot.Version < MaterialSnapshot.MinimumSupportedVersion ||
                snapshot.Version > MaterialSnapshot.CurrentVersion)
            {
                reason = "지원하지 않거나 누락된 Material snapshot 버전입니다. version=" + snapshot.Version;
                return false;
            }
            if (snapshot.SavedAt <= DateTime.MinValue.AddDays(1))
            {
                reason = "Material snapshot 저장 시각이 누락되었습니다.";
                return false;
            }
            if (snapshot.SnapshotRevision < 0L ||
                !MaterialSnapshotRevisionPolicy.IsTrustedLoadedRevision(snapshot.SnapshotRevision))
            {
                reason = "SnapshotRevision이 신뢰 가능한 원본 범위를 벗어났습니다. revision=" +
                         snapshot.SnapshotRevision;
                return false;
            }

            if (snapshot.Cassettes == null || snapshot.Wafers == null || snapshot.Dies == null)
            {
                reason = "Cassettes/Wafers/Dies 목록이 누락되었습니다.";
                return false;
            }

            foreach (WaferMaterial wafer in snapshot.Wafers)
            {
                if (wafer == null)
                {
                    reason = "Wafers 목록에 null 항목이 있습니다.";
                    return false;
                }
                if (wafer.CurrentLocation == null)
                {
                    reason = "Wafer의 CurrentLocation이 누락되었습니다. wafer=" + (wafer.WaferId ?? "");
                    return false;
                }
            }
            foreach (DieMaterial die in snapshot.Dies)
            {
                if (die == null)
                {
                    reason = "Dies 목록에 null 항목이 있습니다.";
                    return false;
                }
                if (die.CurrentLocation == null)
                {
                    reason = "Die의 CurrentLocation이 누락되었습니다. die=" + (die.DieId ?? "");
                    return false;
                }
            }

            foreach (CassetteMaterial cassette in snapshot.Cassettes)
            {
                if (cassette == null)
                {
                    reason = "Cassettes 목록에 null 항목이 있습니다.";
                    return false;
                }
                if (cassette.Slots == null)
                {
                    reason = "Cassette Slot 목록이 누락되었습니다. cassette=" + cassette.Role;
                    return false;
                }
                if (cassette.SlotCount < 0 || cassette.Slots.Count != cassette.SlotCount)
                {
                    reason = "정상화 전 Cassette SlotCount와 Slot 목록 크기가 일치하지 않습니다. cassette=" +
                             cassette.Role + ", slotCount=" + cassette.SlotCount +
                             ", items=" + cassette.Slots.Count;
                    return false;
                }
                for (int slotIndex = 0; slotIndex < cassette.Slots.Count; slotIndex++)
                {
                    CassetteSlotMaterial slot = cassette.Slots[slotIndex];
                    if (slot == null)
                    {
                        reason = "Cassette Slot 목록에 null 항목이 있습니다. cassette=" + cassette.Role;
                        return false;
                    }
                    if (slot.SlotNumber != slotIndex)
                    {
                        reason = "정상화 전 Cassette Slot 번호가 목록 위치와 일치하지 않습니다. cassette=" +
                                 cassette.Role + ", index=" + slotIndex +
                                 ", slotNumber=" + slot.SlotNumber;
                        return false;
                    }
                }
            }

            return true;
        }

        private static void NormalizeMaterialIdentities(MaterialSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Wafers == null)
                return;

            foreach (WaferMaterial wafer in snapshot.Wafers.Where(item => item != null))
            {
                if (!string.IsNullOrWhiteSpace(wafer.WaferInstanceId))
                    continue;
                wafer.WaferInstanceId = Guid.NewGuid().ToString("N");
                wafer.UpdatedAt = DateTime.Now;
            }

            Dictionary<string, WaferMaterial> uniqueDisplayWafers = snapshot.Wafers
                .Where(wafer => wafer != null && !string.IsNullOrWhiteSpace(wafer.WaferId))
                .GroupBy(wafer => wafer.WaferId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (CassetteMaterial cassette in snapshot.Cassettes.Where(item => item != null))
            {
                if (cassette.Slots == null)
                    continue;
                foreach (CassetteSlotMaterial slot in cassette.Slots.Where(item => item != null))
                {
                    WaferMaterial wafer;
                    if (string.IsNullOrWhiteSpace(slot.WaferInstanceId) &&
                        !string.IsNullOrWhiteSpace(slot.WaferId) &&
                        uniqueDisplayWafers.TryGetValue(slot.WaferId.Trim(), out wafer))
                    {
                        slot.WaferInstanceId = wafer.WaferInstanceId;
                    }
                }
            }

            foreach (DieMaterial die in snapshot.Dies.Where(item => item != null))
            {
                WaferMaterial wafer;
                if (string.IsNullOrWhiteSpace(die.InputWaferInstanceId) &&
                    !string.IsNullOrWhiteSpace(die.WaferID_Input) &&
                    uniqueDisplayWafers.TryGetValue(die.WaferID_Input.Trim(), out wafer))
                {
                    die.InputWaferInstanceId = wafer.WaferInstanceId;
                }
                if (string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) &&
                    !string.IsNullOrWhiteSpace(die.WaferID_Output) &&
                    uniqueDisplayWafers.TryGetValue(die.WaferID_Output.Trim(), out wafer))
                {
                    die.OutputWaferInstanceId = wafer.WaferInstanceId;
                }
            }

            foreach (WaferMaterial outputWafer in snapshot.Wafers.Where(item => item != null))
            {
                WaferMaterial sourceWafer;
                if (string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferInstanceId) &&
                    !string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferId) &&
                    uniqueDisplayWafers.TryGetValue(
                        outputWafer.OutputReceiveSourceWaferId.Trim(),
                        out sourceWafer))
                {
                    outputWafer.OutputReceiveSourceWaferInstanceId = sourceWafer.WaferInstanceId;
                }
            }
        }
    }
}
