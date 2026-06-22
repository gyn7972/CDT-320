using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 다이별 그랩 이미지를 디스크에 저장.
    /// 디렉토리: &lt;ImageLogPath&gt;/yyyy-MM-dd/&lt;chipUid&gt;/&lt;module&gt;_&lt;tool&gt;.png
    /// 310 의 ImageLogSaver 와 동일 동작.
    /// <para>저장 게이트: 전역 <see cref="VisionSettings.ImageLogEnable"/> AND 활성 레시피 <c>LogEnable</c>
    /// AND 레시피 <c>ImageSaveMode</c>(OK/NG/ALL) 필터.</para>
    /// </summary>
    public static class ImageLogSaver
    {
        /// <summary>
        /// 설정 + 모듈/툴/uid + 이미지를 받아 PNG 저장. uid="Manual" 이면 skip.
        /// </summary>
        /// <param name="isPass">검사 합부 결과. true=OK, false=NG. null 이면 합부 개념이 없는 그랩(예: Finder 정렬)
        /// — OK/NG 필터를 적용하지 않고 항상 저장한다(레시피 로그 토글은 적용).</param>
        public static void Save(VisionSettings cfg, string moduleName, string tool, string chipUid, Bitmap bmp, bool? isPass = null)
        {
            if (cfg == null || !cfg.ImageLogEnable) return;
            if (string.IsNullOrEmpty(chipUid) || chipUid == "Manual") return;
            if (bmp == null) return;

            // 레시피별 로그 토글 + OK/NG/ALL 필터. 활성 레시피 미등록 시 안전 기본값(로그 ON, ALL)로 통과.
            var recipe = ActiveRecipeContext.Current;
            if (recipe != null && !recipe.LogEnable) return;
            if (!ShouldSaveForMode(recipe?.ImageSaveMode ?? ImageSaveMode.ALL, isPass)) return;

            try
            {
                string root = string.IsNullOrEmpty(cfg.ImageLogPath) ? @".\Log\Image" : cfg.ImageLogPath;
                string dir  = Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd"), Sanitize(chipUid));
                Directory.CreateDirectory(dir);
                string ts = DateTime.Now.ToString("HHmmss_fff");
                string file = Path.Combine(dir, $"{Sanitize(moduleName)}_{Sanitize(tool)}_{ts}.png");

                // 원본 비트맵 손상 방지를 위해 클론 후 저장.
                using (var clone = new Bitmap(bmp))
                    clone.Save(file, ImageFormat.Png);
            }
            catch { }
        }

        /// <summary>레시피 이미지 저장 모드와 합부 결과로 저장 여부 판정.
        /// ALL=항상, OK=양품(PASS)만, NG=불량만. isPass=null(합부 개념 없음)이면 ALL 로 간주(항상 저장).</summary>
        public static bool ShouldSaveForMode(ImageSaveMode mode, bool? isPass)
        {
            if (isPass == null) return true;     // 합부 개념 없는 그랩(Finder 등) — 필터 미적용.
            switch (mode)
            {
                case ImageSaveMode.OK:  return isPass.Value;
                case ImageSaveMode.NG:  return !isPass.Value;
                case ImageSaveMode.ALL:
                default:                return true;
            }
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
