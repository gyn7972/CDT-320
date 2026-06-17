using System;
using System.Collections.Generic;
using System.Text;

namespace QMC.Common.Logging
{
    public class EventRow
    {
        public DateTime When { get; set; }
        public EventKind Kind { get; set; }
        public string User { get; set; }
        public string Code { get; set; }
        public string Source { get; set; }
        public string Description { get; set; }

        public string ToCsv()
        {
            try
            {
                // 저장 순서: When, Kind, User, Code, Source, Description
                return string.Join(",",
                    When.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Kind.ToString(),
                    Csv(User),
                    Csv(Code),
                    Csv(Source),
                    Csv(Description));
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
            }
        }

        public static EventRow FromCsv(string line)
        {
            try
            {
                List<string> parts = ParseCsv(line);
                if (parts.Count < 5)
                    return null;

                EventKind kind;
                if (!Enum.TryParse(parts[1], out kind))
                    kind = EventKind.Event;

                DateTime when;
                DateTime.TryParse(parts[0], out when);

                // 신버전: When,Kind,User,Code,Source,Description (6칸)
                // 구버전: When,Kind,User,Code,Description (5칸) — Source 없음, 호환 처리
                string source;
                string description;
                if (parts.Count >= 6)
                {
                    source = parts[4];
                    description = parts[5];
                }
                else
                {
                    source = string.Empty;
                    description = parts[4];
                }

                return new EventRow
                {
                    When = when,
                    Kind = kind,
                    User = parts[2],
                    Code = parts[3],
                    Source = source,
                    Description = description
                };
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static string Csv(string value)
        {
            try
            {
                value = value ?? string.Empty;
                if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                    return "\"" + value.Replace("\"", "\"\"") + "\"";

                return value;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
            }
        }

        private static List<string> ParseCsv(string line)
        {
            try
            {
                List<string> result = new List<string>();
                StringBuilder builder = new StringBuilder();
                bool quoted = false;

                line = line ?? string.Empty;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (quoted)
                    {
                        if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            builder.Append('"');
                            i++;
                        }
                        else if (c == '"')
                        {
                            quoted = false;
                        }
                        else
                        {
                            builder.Append(c);
                        }
                    }
                    else
                    {
                        if (c == ',')
                        {
                            result.Add(builder.ToString());
                            builder.Clear();
                        }
                        else if (c == '"' && builder.Length == 0)
                        {
                            quoted = true;
                        }
                        else
                        {
                            builder.Append(c);
                        }
                    }
                }

                result.Add(builder.ToString());
                return result;
            }
            catch
            {
                return new List<string>();
            }
            finally
            {
            }
        }
    }
}
