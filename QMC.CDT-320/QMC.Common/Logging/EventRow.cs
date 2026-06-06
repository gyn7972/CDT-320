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
        public string Description { get; set; }

        public string ToCsv()
        {
            try
            {
                return string.Join(",",
                    When.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Kind.ToString(),
                    Csv(User),
                    Csv(Code),
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

                return new EventRow
                {
                    When = when,
                    Kind = kind,
                    User = parts[2],
                    Code = parts[3],
                    Description = parts[4]
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
