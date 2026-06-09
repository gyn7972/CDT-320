using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QMC.Common.Persistence;

namespace QMC.Vision.Core.Parameters
{
    /// <summary>파라미터 물리 저장 채널. Snapshot=신 스냅샷 스토어, 그 외=기존 스토어 위임(P2 분리 보류).</summary>
    public enum ParameterChannel { Snapshot, VisionConfig, CameraMap }

    /// <summary>
    /// P2 — SSOT 3계층 ParameterStore (설계 §2). 레지스트리 + 디스크립터 인덱스 + GetValue/SetValue +
    /// 계층·타깃 dirty + 영속화 라우팅. 실값은 도메인 객체 유지(Getter/Setter 위임 — 이중 진실원 없음).
    /// 라우팅: Snapshot=Recipe(UnitRecipeStore)/Setup(통합파일) 직접, VisionConfig/CameraMap=기존 스토어 위임.
    /// </summary>
    public sealed class ParameterStore
    {
        private sealed class Entry { public ParameterDescriptor D; public ParameterChannel Ch; }

        private readonly List<Entry> _all = new List<Entry>();
        private readonly Dictionary<string, ParameterDescriptor> _byKey = new Dictionary<string, ParameterDescriptor>();
        private readonly Dictionary<ParameterChannel, Action> _delegateSave = new Dictionary<ParameterChannel, Action>();
        private readonly HashSet<ParameterLayer> _dirtyLayers = new HashSet<ParameterLayer>();
        private readonly HashSet<string> _dirtyTargets = new HashSet<string>();

        /// <summary>제품(레시피) 이름. 제품 선택기 도입 전 단일 "&lt;default&gt;".</summary>
        public string Product { get; set; } = "<default>";
        /// <summary>Setup 통합 파일 절대경로(부팅 시 설정).</summary>
        public string SetupFilePath { get; set; }

        public event EventHandler DirtyChanged;

        private static string K(string t, string k) => (t ?? "") + "::" + (k ?? "");

        // ── 등록 ──
        public void Register(IParameterProvider p, ParameterChannel channel)
        {
            if (p == null) return;
            foreach (var d in p.DescribeParameters())
            {
                var kk = K(d.Target, d.Key);
                if (_byKey.ContainsKey(kk)) continue;   // 먼저 등록된(실 인스펙터) 키 우선 — ② 고아가 가리지 않음(P3 통합 전)
                _all.Add(new Entry { D = d, Ch = channel });
                _byKey[kk] = d;
            }
        }

        /// <summary>위임 채널의 전체 저장 동작 등록(VisionConfigStore.Save / AlgorithmCameraMapStore.Save).</summary>
        public void SetDelegateSave(ParameterChannel channel, Action save)
        {
            if (save != null) _delegateSave[channel] = save;
        }

        // ── 조회 ──
        public ParameterDescriptor Get(string target, string key)
            => _byKey.TryGetValue(K(target, key), out var d) ? d : null;
        public IEnumerable<ParameterDescriptor> All() => _all.Select(e => e.D);
        public IEnumerable<ParameterDescriptor> GetByLayer(ParameterLayer layer)
            => _all.Where(e => e.D.Layer == layer).Select(e => e.D);
        public IEnumerable<ParameterDescriptor> GetByTarget(string target)
            => _all.Where(e => e.D.Target == target).Select(e => e.D);
        public IEnumerable<ParameterDescriptor> GetByLayerAndTarget(ParameterLayer layer, string target)
            => _all.Where(e => e.D.Layer == layer && e.D.Target == target).Select(e => e.D);
        public IEnumerable<string> Targets() => _all.Select(e => e.D.Target).Distinct();

        // ── 값 ──
        public object GetValue(string target, string key) => Get(target, key)?.Getter();
        public void SetValue(string target, string key, object value)
        {
            var d = Get(target, key);
            if (d == null) return;
            d.Setter(value);
            _dirtyLayers.Add(d.Layer);
            _dirtyTargets.Add(target);
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        // ── dirty ──
        public bool IsDirty(ParameterLayer layer) => _dirtyLayers.Contains(layer);
        public bool IsDirty(string target) => _dirtyTargets.Contains(target);
        public void ClearDirty() { _dirtyLayers.Clear(); _dirtyTargets.Clear(); }

        // ── 인코딩/디코딩 ──
        private static string Encode(object v) => v == null ? "" : Convert.ToString(v, CultureInfo.InvariantCulture);
        private static object Decode(string typeStr, string val)
        {
            switch (typeStr)
            {
                case "Double": return double.Parse(val, NumberStyles.Float, CultureInfo.InvariantCulture);
                case "Int":    return int.Parse(val, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "Bool":   return bool.Parse(val);
                default:       return val;   // Text/Enum → Setter 가 변환(Enum.Parse)
            }
        }

        private ParameterSnapshot ToSnapshot(IEnumerable<ParameterDescriptor> descs)
        {
            var snap = new ParameterSnapshot();
            foreach (var d in descs)
                snap.Entries.Add(new ParameterSnapshotEntry
                { Target = d.Target, Key = d.Key, Type = d.Type.ToString(), Value = Encode(d.Getter()) });
            return snap;
        }

        private void Apply(ParameterSnapshot snap)
        {
            if (snap?.Entries == null) return;
            foreach (var e in snap.Entries)
            {
                var d = Get(e.Target, e.Key);
                if (d == null) continue;
                try { d.Setter(Decode(e.Type, e.Value)); } catch { /* 타입 변경/구파일 — 스킵 */ }
            }
        }

        // ── 저장 ──
        private IEnumerable<string> SnapshotRecipeTargets()
            => _all.Where(e => e.Ch == ParameterChannel.Snapshot && e.D.Layer == ParameterLayer.Recipe)
                   .Select(e => e.D.Target).Distinct();

        private void SaveSetupUnified()
        {
            if (string.IsNullOrEmpty(SetupFilePath)) return;
            var descs = _all.Where(e => e.Ch == ParameterChannel.Snapshot && e.D.Layer == ParameterLayer.Setup).Select(e => e.D);
            SnapshotFileStore.Save(ToSnapshot(descs), SetupFilePath);
        }

        private void SaveRecipeTarget(string target)
        {
            var descs = _all.Where(e => e.Ch == ParameterChannel.Snapshot && e.D.Target == target && e.D.Layer == ParameterLayer.Recipe)
                            .Select(e => e.D).ToList();
            if (descs.Count == 0) return;
            UnitRecipeStore.Save(ToSnapshot(descs), Product, target);
        }

        /// <summary>한 타깃의 전 계층 저장(finder/inspector Save 래퍼가 호출).</summary>
        public void SaveTarget(string target)
        {
            var entries = _all.Where(e => e.D.Target == target).ToList();
            if (entries.Count == 0) return;
            if (entries.Any(e => e.Ch == ParameterChannel.Snapshot && e.D.Layer == ParameterLayer.Recipe)) SaveRecipeTarget(target);
            if (entries.Any(e => e.Ch == ParameterChannel.Snapshot && e.D.Layer == ParameterLayer.Setup)) SaveSetupUnified();
            foreach (var ch in entries.Where(e => e.Ch != ParameterChannel.Snapshot).Select(e => e.Ch).Distinct())
                if (_delegateSave.TryGetValue(ch, out var act)) try { act(); } catch { }
            _dirtyTargets.Remove(target);
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>한 계층 저장(UI 페이지=계층 단위 저장).</summary>
        public void SaveLayer(ParameterLayer layer)
        {
            if (layer == ParameterLayer.Recipe)
                foreach (var t in SnapshotRecipeTargets()) SaveRecipeTarget(t);
            else if (layer == ParameterLayer.Setup)
                SaveSetupUnified();
            foreach (var ch in _all.Where(e => e.Ch != ParameterChannel.Snapshot && e.D.Layer == layer).Select(e => e.Ch).Distinct())
                if (_delegateSave.TryGetValue(ch, out var act)) try { act(); } catch { }
            _dirtyLayers.Remove(layer);
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        public void LoadTarget(string target)
        {
            Apply(UnitRecipeStore.Load<ParameterSnapshot>(Product, target));   // Recipe
            var setup = SnapshotFileStore.Load(SetupFilePath);                  // Setup 통합 → 이 타깃만
            if (setup?.Entries != null)
                Apply(new ParameterSnapshot { Entries = setup.Entries.Where(e => e.Target == target).ToList() });
        }

        /// <summary>전 계층 로드(부팅). Snapshot=신 파일 → 디스크립터 주입. 위임 채널=기존 스토어가 이미 로드(no-op).</summary>
        public void LoadAll()
        {
            Apply(SnapshotFileStore.Load(SetupFilePath));            // Setup 통합
            foreach (var t in SnapshotRecipeTargets())               // Recipe 타깃별
                Apply(UnitRecipeStore.Load<ParameterSnapshot>(Product, t));
            ClearDirty();
        }

        /// <summary>마이그레이션 보조 — 신 Recipe 파일이 비었는지(=미생성). bootstrap 이 구파일 이전 판단에 사용.</summary>
        public bool RecipeTargetEmpty(string target)
            => UnitRecipeStore.Load<ParameterSnapshot>(Product, target).Entries.Count == 0;
    }
}
