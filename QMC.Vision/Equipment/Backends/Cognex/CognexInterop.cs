using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// Cognex VisionPro 어셈블리에 dynamic 으로 접근하기 위한 헬퍼.
    /// - 어셈블리/타입 캐싱
    /// - <see cref="Bitmap"/> ↔ ICogImage 변환 (temp .bmp 파일 경유)
    /// - CogRectangle 생성
    /// </summary>
    internal static class CognexInterop
    {
        private static readonly ConcurrentDictionary<string, Type> _types =
            new ConcurrentDictionary<string, Type>(StringComparer.Ordinal);

        /// <summary>주어진 풀네임 타입을 로드된 어셈블리들에서 검색. 못 찾으면 null.</summary>
        public static Type GetType(string fullName, params Assembly[] preferred)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            return _types.GetOrAdd(fullName, fn =>
            {
                // 1) 우선 명시 어셈블리에서
                foreach (var a in preferred ?? new Assembly[0])
                {
                    if (a == null) continue;
                    var t = a.GetType(fn, false);
                    if (t != null) return t;
                }
                // 2) 모든 로드된 어셈블리 스캔
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = a.GetType(fn, false);
                    if (t != null) return t;
                }
                return null;
            });
        }

        /// <summary>대안 풀네임 시리즈에서 첫 번째로 찾은 타입 반환.</summary>
        public static Type FindAny(Assembly[] preferred, params string[] candidateFullNames)
        {
            foreach (var n in candidateFullNames)
            {
                var t = GetType(n, preferred);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>
        /// Bitmap → ICogImage. 안전성을 위해 임시 .bmp 파일 경유.
        /// 픽셀 포맷에 따라 8bpp Grey / 24bpp RGB 자동 선택.
        /// </summary>
        public static dynamic BitmapToICogImage(Bitmap bmp, params Assembly[] preferred)
        {
            if (bmp == null) return null;

            // CogImageFileBMP 타입 (네임스페이스가 버전별로 약간 다름 — 둘 다 시도)
            var fileBmpType = FindAny(preferred,
                "Cognex.VisionPro.ImageFile.CogImageFileBMP",
                "Cognex.VisionPro.CogImageFileBMP");
            var modeEnum = FindAny(preferred,
                "Cognex.VisionPro.CogImageFileModeConstants",
                "Cognex.VisionPro.ImageFile.CogImageFileModeConstants");
            if (fileBmpType == null || modeEnum == null)
                throw new InvalidOperationException("Cognex CogImageFileBMP/CogImageFileModeConstants 타입을 찾지 못함");

            string tmp = Path.Combine(Path.GetTempPath(),
                "qmc_vision_" + Guid.NewGuid().ToString("N") + ".bmp");
            try
            {
                bmp.Save(tmp, ImageFormat.Bmp);
                dynamic file = Activator.CreateInstance(fileBmpType);
                object readMode = Enum.Parse(modeEnum, "Read", true);
                file.Open(tmp, (dynamic)readMode);
                dynamic img = file[0];
                file.Close();
                return img;
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        /// <summary>주어진 ICogImage 의 SubRegion ROI 용 CogRectangle 생성.</summary>
        public static dynamic NewRectangle(double x, double y, double w, double h, params Assembly[] preferred)
        {
            var rectType = FindAny(preferred, "Cognex.VisionPro.CogRectangle");
            if (rectType == null) throw new InvalidOperationException("CogRectangle 타입을 찾지 못함");
            dynamic r = Activator.CreateInstance(rectType);
            r.X      = x;
            r.Y      = y;
            r.Width  = w;
            r.Height = h;
            return r;
        }

        /// <summary>CogTransform2DLinear 생성(평행이동만, 회전/스케일 없음) — PMAlign Pattern.Origin 지정용.
        /// Origin 을 학습영역 중심으로 두면 결과 Pose.Translation 이 '찾은 위치'의 절대 이미지 좌표가 된다.</summary>
        public static dynamic NewTransform2D(double tx, double ty, params Assembly[] preferred)
        {
            var t = FindAny(preferred, "Cognex.VisionPro.CogTransform2DLinear");
            if (t == null) throw new InvalidOperationException("CogTransform2DLinear 타입을 찾지 못함");
            dynamic xf = Activator.CreateInstance(t);
            xf.TranslationX = tx;
            xf.TranslationY = ty;
            return xf;
        }

        /// <summary>property 가 존재할 때만 set (안전한 dynamic).</summary>
        public static void TrySet(object target, string prop, object value)
        {
            if (target == null) return;
            try
            {
                var pi = target.GetType().GetProperty(prop);
                if (pi != null && pi.CanWrite) pi.SetValue(target, value);
            }
            catch { }
        }

        /// <summary>property get — 없으면 default 반환.</summary>
        public static object TryGet(object target, string prop, object def = null)
        {
            if (target == null) return def;
            try
            {
                var pi = target.GetType().GetProperty(prop);
                if (pi != null && pi.CanRead) return pi.GetValue(target);
            }
            catch { }
            return def;
        }
    }
}
