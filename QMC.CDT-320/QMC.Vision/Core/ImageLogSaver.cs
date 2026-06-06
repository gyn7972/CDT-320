using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QMC.Vision.Config;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 다이별 그랩 이미지를 디스크에 저장.
    /// 디렉토리: &lt;ImageLogPath&gt;/yyyy-MM-dd/&lt;chipUid&gt;/&lt;module&gt;_&lt;tool&gt;.png
    /// 310 의 ImageLogSaver 와 동일 동작.
    /// </summary>
    public static class ImageLogSaver
    {
        /// <summary>설정 + 모듈/툴/uid + 이미지를 받아 PNG 저장. uid="Manual" 이면 skip.</summary>
        public static void Save(VisionSettings cfg, string moduleName, string tool, string chipUid, Bitmap bmp)
        {
            if (cfg == null || !cfg.ImageLogEnable) return;
            if (string.IsNullOrEmpty(chipUid) || chipUid == "Manual") return;
            if (bmp == null) return;

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

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "_";
            foreach (var ch in Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            return s;
        }
    }
}
