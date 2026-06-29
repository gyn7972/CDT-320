using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 검사 결과(InspectionResultStore) → CSV 내보내기. 한 모드(Bottom/Side/Bin)의 다이별 실적을
    /// Index X/Y · Picker · Channel · Pass + 모든 측정 지표 컬럼으로 저장한다(수동/자동 공용).
    /// </summary>
    public static class InspectionResultCsv
    {
        /// <summary>해당 모드의 현재 결과를 CSV 로 저장(성공 true). 결과 없으면 false.</summary>
        public static bool Save(string mode, string path)
        {
            if (string.IsNullOrEmpty(mode) || string.IsNullOrEmpty(path)) return false;
            var snap = InspectionResultStore.History(mode);
            if (snap == null || snap.Count == 0) return false;
            try
            {
                // 지표 컬럼 = 모든 행의 Values 키 합집합(처음 등장 순서 유지).
                var keys = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var it in snap)
                {
                    if (it?.Values == null) continue;
                    foreach (var k in it.Values.Keys)
                        if (seen.Add(k)) keys.Add(k);
                }

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var ci = CultureInfo.InvariantCulture;
                using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    var head = new StringBuilder("Index X,Index Y,Picker,Channel,Pass");
                    foreach (var k in keys) { head.Append(','); head.Append(Esc(k)); }
                    sw.WriteLine(head.ToString());

                    foreach (var it in snap)
                    {
                        if (it == null) continue;
                        var sb = new StringBuilder();
                        sb.Append(it.IndexX).Append(',').Append(it.IndexY).Append(',')
                          .Append(it.Picker).Append(',').Append(it.Channel).Append(',')
                          .Append(it.Pass ? "PASS" : "NG");
                        foreach (var k in keys)
                        {
                            sb.Append(',');
                            if (it.Values != null && it.Values.TryGetValue(k, out double v))
                                sb.Append(v.ToString("F4", ci));
                        }
                        sw.WriteLine(sb.ToString());
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>일자별 Log\Result 폴더에 자동 저장(웨이퍼 완료 시). 저장 경로 반환(실패 시 null).</summary>
        public static string AutoSave(string mode, string tag)
        {
            try
            {
                string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Result",
                                           DateTime.Now.ToString("yyyy-MM-dd"));
                string safeTag = Sanitize(tag);
                string baseName = (string.IsNullOrWhiteSpace(safeTag) ? mode : mode + "_" + safeTag)
                                  + "_" + DateTime.Now.ToString("HHmmss") + ".csv";
                string path = Path.Combine(root, baseName);
                return Save(mode, path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
