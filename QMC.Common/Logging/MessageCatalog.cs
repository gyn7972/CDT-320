using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QMC.Common.Logging
{
    /// <summary>
    /// 코드/메시지 문구(한/영) 카탈로그.
    /// <list type="bullet">
    ///   <item><description>기본 문구는 코드(CreateDefaults)에 내장 — 파일이 없어도 동작.</description></item>
    ///   <item><description>화면에서 편집/저장하면 그때만 Config\Messages\message_catalog.csv 로 기록.</description></item>
    ///   <item><description>Resolve(): 카탈로그 우선, 알람이면 AlarmMaster 보조, 없으면 호출자 문구.</description></item>
    /// </list>
    /// 저장은 <b>목록(List)</b> 형태라 같은 코드의 행이 여러 개여도 모두 보존된다.
    /// (코드+디스크립션이 모두 같은 완전 중복만 제거)
    /// </summary>
    public static class MessageCatalog
    {
        private static List<MessageDefinition> _items = new List<MessageDefinition>();

        public static string Dir   => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Messages");
        public static string Path_ => Path.Combine(Dir, "message_catalog.csv");

        static MessageCatalog()
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            Load();
        }

        /// <summary>전체 목록(읽기 전용). 코드 중복 가능.</summary>
        public static IReadOnlyList<MessageDefinition> Items => _items;
        public static int Count => _items.Count;

        /// <summary>코드로 첫 번째 항목을 찾는다(중복 코드면 처음 것).</summary>
        public static MessageDefinition Get(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            foreach (var d in _items)
                if (string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase))
                    return d;
            return null;
        }

        /// <summary>코드 문구를 언어에 맞게 해석. 카탈로그 → (알람이면)AlarmMaster → fallback 순.</summary>
        public static string Resolve(EventKind kind, string code, string lang, string fallback)
        {
            try
            {
                // 카탈로그에 코드가 있고 문구가 채워져 있을 때만 그 문구를 쓴다.
                // (코드만 등록하고 문구가 비어 있으면 원본 설명을 덮지 않도록 폴백)
                var m = Get(code);
                if (m != null)
                {
                    string t = m.Text(lang);
                    if (!string.IsNullOrEmpty(t)) return t;
                }

                if (kind == EventKind.Alarm)
                {
                    var d = QMC.Common.Alarms.AlarmMaster.Get(code);
                    if (d != null)
                    {
                        string t = d.GetTitle(lang);
                        if (!string.IsNullOrEmpty(t)) return t;
                    }
                }
            }
            catch
            {
            }
            return fallback;
        }

        /// <summary>화면 편집 결과로 전체 목록을 통째로 교체. 코드 중복은 허용하되 완전 동일 행은 1개만 남긴다.</summary>
        public static void ReplaceAll(IEnumerable<MessageDefinition> items)
        {
            var list = new List<MessageDefinition>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (items != null)
            {
                foreach (var d in items)
                {
                    if (d == null || string.IsNullOrWhiteSpace(d.Code)) continue;
                    // 코드+종류+한/영이 전부 같은 완전 중복만 스킵 (디스크립션이 다르면 코드 같아도 유지)
                    string key = (d.Code ?? "") + "" + d.Kind + "" + (d.Ko ?? "") + "" + (d.En ?? "");
                    if (!seen.Add(key)) continue;
                    list.Add(d);
                }
            }
            _items = list;
        }

        public static void Load()
        {
            try
            {
                // 파일이 없으면 기본 샘플 없이 빈 목록으로 시작한다(파일은 저장 시 생성).
                if (!File.Exists(Path_))
                {
                    _items = new List<MessageDefinition>();
                    return;
                }

                var list = new List<MessageDefinition>();
                foreach (string line in File.ReadAllLines(Path_, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("Code,", StringComparison.OrdinalIgnoreCase)) continue; // 헤더

                    var parts = ParseCsv(line);
                    if (parts.Count < 4) continue;

                    // 저장 순서: Code, Kind, Ko, En
                    EventKind kind;
                    if (!Enum.TryParse(parts[1], out kind)) kind = EventKind.Event;

                    var def = new MessageDefinition
                    {
                        Code = parts[0],
                        Kind = kind,
                        Ko   = parts[2],
                        En   = parts[3]
                    };
                    if (!string.IsNullOrWhiteSpace(def.Code)) list.Add(def); // 코드 중복 허용
                }
                _items = list;
            }
            catch
            {
                _items = new List<MessageDefinition>();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var sb = new StringBuilder();
                sb.AppendLine("Code,Kind,Ko,En");
                foreach (var d in _items)
                {
                    sb.AppendLine(string.Join(",",
                        Csv(d.Code), d.Kind.ToString(), Csv(d.Ko), Csv(d.En)));
                }
                File.WriteAllText(Path_, sb.ToString(), new UTF8Encoding(true)); // UTF-8 BOM
            }
            catch
            {
            }
        }

        /// <summary>코드 내장 기본 문구(샘플 이벤트 코드 일부).</summary>
        public static List<MessageDefinition> CreateDefaults()
        {
            return new List<MessageDefinition>
            {
                new MessageDefinition { Code="AJIN-MAP-LOAD", Kind=EventKind.Event, Ko="Ajin 맵 로드됨",        En="AJIN MAP LOADED" },
                new MessageDefinition { Code="CYL-MAP-APPLY", Kind=EventKind.Event, Ko="실린더 매핑 적용됨",     En="CYLINDER MAPPING APPLIED" },
                new MessageDefinition { Code="OS-CYL-IO",     Kind=EventKind.Event, Ko="출력 실린더 IO 바인딩",  En="OUTPUT CYLINDER IO BOUND" },
                new MessageDefinition { Code="RECIPE-LOAD",   Kind=EventKind.Event, Ko="레시피 로드됨",          En="RECIPE LOADED" },
                new MessageDefinition { Code="SIM-CONNECT",   Kind=EventKind.Event, Ko="시뮬레이터 연결됨",      En="SIMULATOR CONNECTED" },
            };
        }

        // --- CSV 헬퍼 (EventRow 와 동일한 따옴표 규칙) ---

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static List<string> ParseCsv(string line)
        {
            var result = new List<string>();
            var builder = new StringBuilder();
            bool quoted = false;

            line = line ?? string.Empty;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { builder.Append('"'); i++; }
                    else if (c == '"') { quoted = false; }
                    else { builder.Append(c); }
                }
                else
                {
                    if (c == ',') { result.Add(builder.ToString()); builder.Clear(); }
                    else if (c == '"' && builder.Length == 0) { quoted = true; }
                    else { builder.Append(c); }
                }
            }
            result.Add(builder.ToString());
            return result;
        }
    }
}
