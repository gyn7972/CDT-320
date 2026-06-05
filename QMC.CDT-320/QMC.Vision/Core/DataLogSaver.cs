using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using QMC.Vision.Config;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 다이별 검사 데이터를 일자별 CSV 로 저장.
    /// 칼럼: 310 <c>DataLogSaver.Titles</c> 30 종.
    /// </summary>
    public static class DataLogSaver
    {
        // 310 DataLogSaver.Titles 30 종 (순서 유지).
        private static readonly string[] Headers = new[]
        {
            "Material_ID",
            "Loading_Substrate_ID",
            "Loading_Substrate_X",
            "Loading_Substrate_Y",
            "Unloading_Substrate_ID",
            "Unloading_Substrate_X",
            "Unloading_Substrate_Y",
            "Die_Width",
            "Die_Height",
            "ChipLowerSpecLimitWidth",
            "ChipUpperSpecLimitWidth",
            "ChipLowerSpecLimitHeight",
            "ChipUpperSpecLimitHeight",
            "Back_Chipping_Top_Size",
            "Back_Chipping_Right_Size",
            "Back_Chipping_Bottom_Size",
            "Back_Chipping_Left_Size",
            "Back_Chipping_Length",
            "Side_Chipping_Bottom",
            "Side_Chipping_Left",
            "Side_Chipping_Top",
            "Side_Chipping_Right",
            "Side_Chipping_Length",
            "Back_Foreign_Size",
            "ForeignObjectSize",
            "Post_Place_Top_Gap_Avg",
            "Post_Place_Bottom_Gap_Avg",
            "Post_Place_Left_Gap_Avg",
            "Post_Place_Right_Gap_Avg",
            "Post_Place_Gap_UpperLimit",
            "Post_Place_Gap_LowerLimit",
        };

        private static readonly object _sync = new object();

        /// <summary>chipUid 의 record 가 모든 핵심 필드(BottomDie + DieGap)를 갖췄으면 한 줄 저장.</summary>
        public static void SaveIfDieGapComplete(VisionSettings cfg, string chipUid)
        {
            if (cfg == null || !cfg.DataLogEnable) return;
            if (string.IsNullOrEmpty(chipUid)) return;
            var rec = MaterialTracker.Get(chipUid);
            if (rec == null) return;

            // DieGap 결과가 들어왔을 때 1회 저장 (310 의 DataLogSaver.Save 가 DieGap 시점에 호출됨).
            // DieGap 평균값 중 하나라도 채워졌고 아직 저장된 적 없는 경우.
            bool hasDieGap = !string.IsNullOrEmpty(rec.PlaceTopGapAverage) ||
                             !string.IsNullOrEmpty(rec.PlaceBottomGapAverage) ||
                             !string.IsNullOrEmpty(rec.PlaceLeftGapAverage) ||
                             !string.IsNullOrEmpty(rec.PlaceRightGapAverage);
            if (!hasDieGap) return;

            SaveRow(cfg, rec);
        }

        /// <summary>강제로 record 한 줄 저장.</summary>
        public static void SaveRow(VisionSettings cfg, DieRecord rec)
        {
            if (cfg == null || rec == null) return;
            try
            {
                string root = string.IsNullOrEmpty(cfg.DataLogPath) ? @".\Log\Data" : cfg.DataLogPath;
                Directory.CreateDirectory(root);
                string file = Path.Combine(root, "vision_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");

                lock (_sync)
                {
                    bool exists = File.Exists(file);
                    using (var sw = new StreamWriter(file, true, new UTF8Encoding(false)))
                    {
                        if (!exists)
                        {
                            sw.WriteLine("Timestamp," + string.Join(",", Headers));
                        }
                        var values = new[]
                        {
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                            rec.ChipUid,
                            rec.LoadingSubstrateId,
                            rec.LoadingSubstrateX,
                            rec.LoadingSubstrateY,
                            rec.UnloadingSubstrateId,
                            rec.UnloadingSubstrateX,
                            rec.UnloadingSubstrateY,
                            rec.DieWidth,
                            rec.DieHeight,
                            rec.ChipLowerSpecLimitWidth,
                            rec.ChipUpperSpecLimitWidth,
                            rec.ChipLowerSpecLimitHeight,
                            rec.ChipUpperSpecLimitHeight,
                            rec.BackChippingTopSize,
                            rec.BackChippingRightSize,
                            rec.BackChippingBottomSize,
                            rec.BackChippingLeftSize,
                            rec.BackChippingLength,
                            rec.SideChippingBottomSize,
                            rec.SideChippingLeftSize,
                            rec.SideChippingTopSize,
                            rec.SideChippingRightSize,
                            rec.SideChippingLength,
                            rec.BackForeignSize,
                            rec.ForeignObjectSize,
                            rec.PlaceTopGapAverage,
                            rec.PlaceBottomGapAverage,
                            rec.PlaceLeftGapAverage,
                            rec.PlaceRightGapAverage,
                            rec.DieGapUpperLimit,
                            rec.DieGapLowerLimit,
                        };
                        sw.WriteLine(string.Join(",", values.Select(v => Csv(v))));
                    }
                }
            }
            catch { }
        }

        private static string Csv(string s)
        {
            if (s == null) return "";
            if (s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
