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

        // 메시지 카탈로그는 로그 루트 아래 Messages 폴더에 저장한다 (예: D:\CDT-320\Log\Messages\message_catalog.csv).
        public static string Dir   => Path.Combine(EventLogger.LogRoot, "Messages");
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

        /// <summary>
        /// 코드 문구를 언어에 맞게 해석한다.
        /// <para>
        /// 카탈로그는 디스크립션 단위(코드 중복 허용)이므로, <b>코드 + 원문 디스크립션(fallback)</b> 이 정확히
        /// 일치하는 항목만 그 항목의 번역으로 치환한다(같은 코드의 다른 문구에는 영향 없음).
        /// 일치 항목이 없으면 (알람은) AlarmMaster, 그래도 없으면 원문(fallback)을 그대로 쓴다.
        /// </para>
        /// </summary>
        public static string Resolve(EventKind kind, string code, string lang, string fallback)
        {
            try
            {
                string desc = (fallback ?? string.Empty).Trim();

                // 1) 코드 + 디스크립션(KO 또는 EN 원문)이 정확히 일치하는 항목의 번역을 사용.
                if (!string.IsNullOrEmpty(code) && desc.Length > 0)
                {
                    foreach (var m in _items)
                    {
                        if (m == null) continue;
                        if (!string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals((m.Ko ?? string.Empty).Trim(), desc, StringComparison.Ordinal)
                         || string.Equals((m.En ?? string.Empty).Trim(), desc, StringComparison.Ordinal))
                        {
                            string t = m.Text(lang);
                            if (!string.IsNullOrEmpty(t)) return t;
                        }
                    }
                }

                // 2) 알람은 AlarmMaster 의 코드 기준 제목으로 보조.
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
