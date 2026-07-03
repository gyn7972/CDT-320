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
                string root = cfg.EffectiveImageLogPath;   // 비우면 기본 D:\CDT-320\Image — 폴더는 저장 큐에서 생성
                string dir  = Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd"), Sanitize(chipUid));
                string ts = DateTime.Now.ToString("HHmmss_fff");
                string file = Path.Combine(dir, $"{Sanitize(moduleName)}_{Sanitize(tool)}_{ts}.png");

                // 원본 비트맵 손상 방지를 위해 클론만 호출 스레드에서 만들고, PNG 인코딩+디스크 쓰기는
                // 백그라운드 저장 큐로 넘긴다 — 고해상도 PNG 인코딩(장당 수 초)이 검사 스레드의 t 를 부풀리고
                // 4장 병렬 인코딩이 CPU 를 포화시켜 UI 까지 느려지던 문제의 핵심 완화.
                var clone = new Bitmap(bmp);
                if (!Enqueue(dir, file, clone))
                    clone.Dispose();   // 큐 포화 — 이번 장은 저장 생략(검사/UI 보호 우선)
            }
            catch { }
        }

        // ── 백그라운드 저장 큐(단일 소비 스레드) ──
        // 인코딩/쓰기는 순차 1장씩 — CPU 스파이크 없이 사이클 사이 유휴 시간에 소화된다.
        private const int MaxQueue = 16;   // 포화 시 드롭(저장 로그는 진단용 — 검사 지연보다 손실이 낫다)
        private static readonly System.Collections.Concurrent.BlockingCollection<System.Tuple<string, string, Bitmap>> _q
            = new System.Collections.Concurrent.BlockingCollection<System.Tuple<string, string, Bitmap>>(MaxQueue);
        private static int _workerStarted;

        private static bool Enqueue(string dir, string file, Bitmap clone)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _workerStarted, 1, 0) == 0)
            {
                var th = new System.Threading.Thread(SaveLoop) { IsBackground = true, Name = "ImageLogSaver", Priority = System.Threading.ThreadPriority.BelowNormal };
                th.Start();
            }
            return _q.TryAdd(System.Tuple.Create(dir, file, clone));
        }

        private static void SaveLoop()
        {
            foreach (var job in _q.GetConsumingEnumerable())
            {
                try
                {
                    Directory.CreateDirectory(job.Item1);
                    job.Item3.Save(job.Item2, ImageFormat.Png);
                }
                catch { }
                finally { try { job.Item3.Dispose(); } catch { } }
            }
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
