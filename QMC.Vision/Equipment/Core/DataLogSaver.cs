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
            // 레시피별 로그 토글 — 활성 레시피 LogEnable=false 면 데이터 로그도 남기지 않는다(미등록 시 통과).
            var recipe = ActiveRecipeContext.Current;
            if (recipe != null && !recipe.LogEnable) return;
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

        /// <summary>강제로 record 한 줄 저장 — 값 캡처만 호출 스레드에서 하고, 파일 쓰기는
        /// <see cref="DataSaveQueue"/>(백그라운드 스레드)로 넘겨 검사/시퀀스 스레드에 디스크 지연이 더해지지 않게 한다.</summary>
        public static void SaveRow(VisionSettings cfg, DieRecord rec)
        {
            if (cfg == null || rec == null) return;
            try
            {
                string root = cfg.EffectiveDataLogPath;   // 비우면 기본 D:\CDT-320\Data — 폴더는 쓰기 시 생성
                string file = Path.Combine(root, "vision_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");
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
                // 디스크 쓰기는 백그라운드 큐에서 수행 — 값은 위에서 이미 캡처했으므로 이후 record 변경과 무관.
                DataSaveQueue.Enqueue("DataLog:" + rec.ChipUid, () => WriteRow(root, file, values));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[DataLogSaver] SaveRow 실패(" + rec.ChipUid + "): " + ex.Message);
            }
        }

        /// <summary>일자 CSV 한 줄 추가(백그라운드 큐 전용). 폴더 없으면 생성, 헤더는 파일 신규 생성 시에만 기록.</summary>
        private static void WriteRow(string root, string file, string[] values)
        {
            lock (_sync)
            {
                Directory.CreateDirectory(root);
                bool exists = File.Exists(file);
                using (var sw = new StreamWriter(file, true, new UTF8Encoding(false)))
                {
                    if (!exists)
                    {
                        sw.WriteLine("Timestamp," + string.Join(",", Headers));
                    }
                    sw.WriteLine(string.Join(",", values.Select(v => Csv(v))));
                }
            }
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
