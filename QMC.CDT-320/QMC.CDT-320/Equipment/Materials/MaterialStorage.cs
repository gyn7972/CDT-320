using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            Normalize(loaded);
            _state = loaded;
            return true;
        }

        public static void ReplaceState(MaterialSnapshot snapshot)
        {
            if (snapshot == null) return;
            Normalize(snapshot);
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
            if (snapshot.Cassettes == null) snapshot.Cassettes = new List<CassetteMaterial>();
            if (snapshot.Wafers == null) snapshot.Wafers = new List<WaferMaterial>();
            if (snapshot.Dies == null) snapshot.Dies = new List<DieMaterial>();

            foreach (var cassette in snapshot.Cassettes)
            {
                if (cassette.Slots == null) cassette.Slots = new List<CassetteSlotMaterial>();
                cassette.EnsureSlots();
            }

            foreach (var wafer in snapshot.Wafers)
            {
                if (wafer.CurrentLocation == null)
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                if (wafer.SourceSlotNumber < 0)
                    wafer.SourceCassetteSlotPosition = double.NaN;
                if (wafer.CurrentLocation == null || wafer.CurrentLocation.SlotNumber < 0)
                    wafer.CurrentCassetteSlotPosition = double.NaN;
            }
            foreach (var die in snapshot.Dies.Where(d => d.CurrentLocation == null))
                die.CurrentLocation = MaterialLocation.Unknown();
        }
    }
}
