using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT_320.Ui.Security
{
    /// <summary>
    /// 권한 매트릭스(레벨 × 기능). 기능키 = 사이드바 i18n 키.
    /// <para>
    /// 카탈로그(기능 → 코드 기본 최소레벨)는 <see cref="RegisterFeature"/> 로 자동 수집되고,
    /// 사용자 오버라이드(레벨 자유 조합)는 Config\permissions.json 에 영속화된다.
    /// 오버라이드가 없는 기능은 코드 기본값(누적: level ≥ 최소레벨)으로 폴백한다.
    /// </para>
    /// Admin 은 항상 허용, None(비로그인)은 항상 차단.
    /// </summary>
    public static class AccessPolicy
    {
        public sealed class Feature
        {
            public string    Key;
            public string    Group;
            public UserLevel DefaultMin;
        }

        // 편집 가능한 레벨(Admin=항상허용, None=항상차단 이므로 매트릭스 열에서 제외).
        public static readonly UserLevel[] EditableLevels =
            { UserLevel.Operator, UserLevel.Engineer, UserLevel.Maintenance };

        private static readonly Dictionary<string, Feature> _catalog =
            new Dictionary<string, Feature>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, HashSet<UserLevel>> _overrides =
            new Dictionary<string, HashSet<UserLevel>>(StringComparer.OrdinalIgnoreCase);

        public static event Action Changed;

        public static string Dir   => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ => Path.Combine(Dir, "permissions.json");

        static AccessPolicy()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            // 사이드바 버튼이 없어 자동 수집되지 않는 권한 항목을 내장 시드한다(예: JOG 탭).
            RegisterFeature("jog.tab", UserLevel.Engineer);
            Load();
        }

        // ── 카탈로그(코드에서 자동 수집) ───────────────────────────
        public static void RegisterFeature(string key, UserLevel defaultMin)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_catalog.ContainsKey(key)) return;
            _catalog[key] = new Feature { Key = key, Group = GroupOf(key), DefaultMin = defaultMin };
        }

        public static bool IsKnown(string key) => !string.IsNullOrEmpty(key) && _catalog.ContainsKey(key);

        public static IReadOnlyList<Feature> Catalog =>
            _catalog.Values
                    .OrderBy(f => GroupOrder(f.Group))
                    .ThenBy(f => f.Key, StringComparer.Ordinal)
                    .ToList();

        // ── 조회 ───────────────────────────────────────────────────
        public static bool IsAllowed(string key, UserLevel level)
        {
            if (level == UserLevel.Admin) return true;
            if (level == UserLevel.None)  return false;
            if (_overrides.TryGetValue(key, out var set)) return set.Contains(level);
            if (_catalog.TryGetValue(key, out var f))     return (int)level >= (int)f.DefaultMin;
            return true;
        }

        public static bool Can(string key) => IsAllowed(key, UserSession.Level);

        // ── 편집 ───────────────────────────────────────────────────
        public static void SetAllowed(string key, UserLevel level, bool allowed)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (level == UserLevel.Admin || level == UserLevel.None) return;

            if (!_overrides.TryGetValue(key, out var set))
            {
                set = new HashSet<UserLevel>(EditableLevels.Where(l => IsAllowed(key, l)));
                _overrides[key] = set;
            }
            if (allowed) set.Add(level); else set.Remove(level);
        }

        // ── 저장/로드 ──────────────────────────────────────────────
        [DataContract] internal sealed class Entry
        {
            [DataMember] public string   Key;
            [DataMember] public string[] Levels;
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var list = _overrides
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .Select(kv => new Entry
                    {
                        Key    = kv.Key,
                        Levels = kv.Value.OrderBy(l => (int)l).Select(l => l.ToString()).ToArray()
                    })
                    .ToList();

                using (var fs = File.Create(Path_))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(List<Entry>), list);
                }
                Changed?.Invoke();
            }
            catch { }
        }

        public static void Load()
        {
            _overrides.Clear();
            try
            {
                if (!File.Exists(Path_)) return;
                using (var fs = File.OpenRead(Path_))
                {
                    var ser  = new DataContractJsonSerializer(typeof(List<Entry>));
                    var list = ser.ReadObject(fs) as List<Entry>;
                    if (list == null) return;
                    foreach (var e in list)
                    {
                        if (string.IsNullOrEmpty(e.Key)) continue;
                        var set = new HashSet<UserLevel>();
                        if (e.Levels != null)
                            foreach (var s in e.Levels)
                                if (Enum.TryParse<UserLevel>(s, ignoreCase: true, out var lv)) set.Add(lv);
                        _overrides[e.Key] = set;
                    }
                }
            }
            catch { _overrides.Clear(); }
        }

        // ── 그룹 분류 ──────────────────────────────────────────────
        private static string GroupOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return "etc";
            int dot = key.IndexOf('.');
            return dot > 0 ? key.Substring(0, dot) : key;
        }

        private static int GroupOrder(string group)
        {
            switch (group)
            {
                case "work":     return 0;
                case "wi":       return 1;
                case "hist":     return 2;
                case "recipe":   return 3;
                case "jog":      return 4;
                case "set":      return 5;
                case "settings": return 6;
                case "user":     return 7;
                default:         return 9;
            }
        }

        /// <summary>그룹 prefix → 한글 라벨.</summary>
        public static string GroupLabel(string group)
        {
            switch (group)
            {
                case "work":     return "작업";
                case "wi":       return "작업정보";
                case "hist":     return "이력";
                case "recipe":   return "레시피";
                case "jog":      return "조그";
                case "set":
                case "settings": return "설정";
                case "user":     return "사용자";
                default:         return group;
            }
        }
    }
}
