using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QMC.CDT320.Materials;

namespace QMC.CDT320.DieMaps
{
    /// <summary>
    /// 다이 맵 결과를 디스크에 저장하는 전용 헬퍼.
    /// 310 의 OutputDieMapDataSaver 와 동등 기능.
    /// </summary>
    public static class DieMapSaver
    {
        /// <summary>일자별 출력 디렉토리.</summary>
        public static string GetTodayDir()
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "DieMap",
                                       DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(root);
            return root;
        }

        /// <summary>
        /// 사이클 종료 시 호출 — DieMap CSV+JSON 저장 + LotID/FrameId 기반 명명.
        /// 반환: 생성된 CSV 경로 (실패 시 null).
        /// </summary>
        public static string SaveCycleResult(DieMap map, string lotId, string frameId)
        {
            if (map == null) return null;
            try
            {
                string root = GetTodayDir();
                string baseName = $"{Sanitize(lotId)}_{Sanitize(frameId)}_{DateTime.Now:HHmmss}";
                string csv  = Path.Combine(root, baseName + ".csv");
                string json = Path.Combine(root, baseName + ".json");
                DieMapGenerator.SaveCsv(map, csv);
                DieMapGenerator.SaveJson(map, json);
                return csv;
            }
            catch { return null; }
        }

        /// <summary>
        /// 통계 요약 (Lot 단위) — 별도 summary CSV 에 한 줄 누적.
        /// </summary>
        public static void AppendLotSummary(DieMap map, string lotId, string frameId)
        {
            if (map == null) return;
            try
            {
                string path = Path.Combine(GetTodayDir(), "lot_summary.csv");
                bool exists = File.Exists(path);
                using (var sw = new StreamWriter(path, true, new UTF8Encoding(false)))
                {
                    if (!exists)
                        sw.WriteLine("Timestamp,LotID,FrameID,Total,Good,NG,Unknown,YieldPct");
                    int total = map.Entries.Count;
                    int good  = map.Entries.Count(e => e.Result == DieResult.Good);
                    int ng    = map.Entries.Count(e => e.Result == DieResult.NG);
                    int unk   = total - good - ng;
                    double yield = total > 0 ? (good * 100.0 / total) : 0;
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}," +
                                 $"{lotId},{frameId},{total},{good},{ng},{unk}," +
                                 $"{yield.ToString("F2", CultureInfo.InvariantCulture)}");
                }
            }
            catch { }
        }

        /// <summary>특정 다이 결과를 맵에 반영.</summary>
        public static void Apply(DieMap map, int dieMapX, int dieMapY, DieResult result, int binCode, string dieUid = "")
        {
            if (map == null) return;
            DieMapGenerator.Normalize(map);
            var cell = map.GetCell(dieMapX, dieMapY);
            if (cell == null) return;
            cell.Result  = result;
            cell.BinCode = binCode;
            if (!string.IsNullOrEmpty(dieUid)) cell.DieUid = dieUid;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "_";
            foreach (var ch in Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            return s;
        }
    }
}
