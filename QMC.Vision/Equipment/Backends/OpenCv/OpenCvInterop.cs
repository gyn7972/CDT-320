using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace QMC.Vision.Backends.OpenCv
{
    /// <summary>
    /// EmguCV(Emgu.CV.dll) 에 리플렉션으로 접근하는 헬퍼 — csproj 하드 참조 없이 D:\CDT-320\EmguCV 등에서 런타임 로드.
    /// 핵심 API(Mat / CvInvoke.MatchTemplate / MinMaxLoc / Canny / Mean / CountNonZero)는 4.x 전반에서 안정적이라
    /// 버전 차이에 비교적 견고하다. 어떤 단계든 실패하면 <see cref="Ready"/>=false 로 두고 호출측이 순수 C# 로 폴백한다.
    /// </summary>
    internal sealed class OpenCvInterop
    {
        private static OpenCvInterop _inst;
        public static OpenCvInterop Instance => _inst ?? (_inst = new OpenCvInterop());

        public bool Ready { get; private set; }

        private Type _mat, _cvInvoke;
        private object _depth8U;       // DepthType.Cv8U
        private object _tmCcoeffNormed; // TemplateMatchingType.CcoeffNormed
        private MethodInfo _matchTemplate, _minMaxLoc, _canny, _countNonZero, _mean;
        private PropertyInfo _matDataPtr, _matStep;

        private OpenCvInterop()
        {
            try { Init(); Ready = true; }
            catch { Ready = false; }
        }

        private static Assembly EmguAsm()
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var n = a.GetName().Name;
                if (string.Equals(n, "Emgu.CV", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(n, "Emgu.CV.World", StringComparison.OrdinalIgnoreCase))
                    return a;
            }
            return null;
        }

        private void Init()
        {
            var asm = EmguAsm();
            if (asm == null) throw new InvalidOperationException("Emgu.CV assembly not loaded");

            _mat      = asm.GetType("Emgu.CV.Mat", true);
            _cvInvoke = asm.GetType("Emgu.CV.CvInvoke", true);
            var depthT = asm.GetType("Emgu.CV.CvEnum.DepthType", true);
            var tmT    = asm.GetType("Emgu.CV.CvEnum.TemplateMatchingType", true);

            _depth8U        = Enum.Parse(depthT, "Cv8U");
            _tmCcoeffNormed = Enum.Parse(tmT, "CcoeffNormed");

            _matDataPtr = _mat.GetProperty("DataPointer");
            _matStep    = _mat.GetProperty("Step");

            foreach (var m in _cvInvoke.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var ps = m.GetParameters();
                switch (m.Name)
                {
                    case "MatchTemplate": if (ps.Length >= 4 && _matchTemplate == null) _matchTemplate = m; break;
                    case "MinMaxLoc":     if (ps.Length == 6 && _minMaxLoc == null)     _minMaxLoc = m;     break;
                    case "Canny":         if (ps.Length >= 4 && _canny == null)         _canny = m;         break;
                    case "CountNonZero":  if (ps.Length == 1 && _countNonZero == null)  _countNonZero = m;  break;
                    case "Mean":          if (ps.Length >= 1 && _mean == null)          _mean = m;          break;
                }
            }
            if (_matchTemplate == null || _minMaxLoc == null)
                throw new InvalidOperationException("CvInvoke.MatchTemplate/MinMaxLoc not found");
        }

        /// <summary>8bit 그레이 byte[] 로 Mat(1ch) 생성 후 행 단위 복사(스트라이드 패딩 대응).</summary>
        private object MakeGrayMat(byte[] gray, int w, int h)
        {
            object m = Activator.CreateInstance(_mat, new object[] { h, w, _depth8U, 1 });
            IntPtr dp = (IntPtr)_matDataPtr.GetValue(m, null);
            int step = (int)_matStep.GetValue(m, null);
            for (int y = 0; y < h; y++)
                Marshal.Copy(gray, y * w, IntPtr.Add(dp, y * step), w);
            return m;
        }

        private object NewMat() => Activator.CreateInstance(_mat);

        private void Dispose(object mat)
        {
            try { (mat as IDisposable)?.Dispose(); } catch { }
        }

        /// <summary>NCC(CCoeff-normed) 템플릿 매칭 — 전역 최고 1개. 성공 시 score(-1~1) + 검색영역 내 좌상단 loc.</summary>
        public bool TryMatch(byte[] search, int sw, int sh, byte[] templ, int tw, int th,
                             out double score, out int locX, out int locY)
        {
            score = 0; locX = 0; locY = 0;
            if (!Ready) return false;
            object src = null, tpl = null, res = null;
            try
            {
                src = MakeGrayMat(search, sw, sh);
                tpl = MakeGrayMat(templ, tw, th);
                res = NewMat();
                // MatchTemplate(image, templ, result, method[, mask])
                var mp = _matchTemplate.GetParameters();
                object[] ma = mp.Length >= 5
                    ? new object[] { src, tpl, res, _tmCcoeffNormed, null }
                    : new object[] { src, tpl, res, _tmCcoeffNormed };
                _matchTemplate.Invoke(null, ma);

                double minV = 0, maxV = 0;
                Point minL = Point.Empty, maxL = Point.Empty;
                object[] la = { res, minV, maxV, minL, maxL, null };
                _minMaxLoc.Invoke(null, la);
                maxV = (double)la[2];
                maxL = (Point)la[4];

                score = maxV; locX = maxL.X; locY = maxL.Y;
                return true;
            }
            catch { return false; }
            finally { Dispose(src); Dispose(tpl); Dispose(res); }
        }

        /// <summary>그레이 영역 통계 — 평균 밝기 + Canny 에지 픽셀 수(결함 지표).</summary>
        public bool TryEdgeStats(byte[] gray, int w, int h, double t1, double t2,
                                 out double meanGray, out long edgePixels)
        {
            meanGray = 0; edgePixels = 0;
            if (!Ready) return false;
            object src = null, edges = null;
            try
            {
                src = MakeGrayMat(gray, w, h);

                if (_mean != null)
                {
                    var mp = _mean.GetParameters();
                    object[] aa = mp.Length >= 2 ? new object[] { src, null } : new object[] { src };
                    object scalar = _mean.Invoke(null, aa);   // MCvScalar
                    var v0 = scalar.GetType().GetField("V0");
                    if (v0 != null) meanGray = Convert.ToDouble(v0.GetValue(scalar));
                }

                if (_canny != null && _countNonZero != null)
                {
                    edges = NewMat();
                    var cp = _canny.GetParameters();
                    object[] ca = new object[cp.Length];
                    ca[0] = src; ca[1] = edges; ca[2] = t1; ca[3] = t2;
                    for (int i = 4; i < cp.Length; i++)              // apertureSize/l2gradient 등 기본값
                        ca[i] = cp[i].HasDefaultValue ? cp[i].DefaultValue : (cp[i].ParameterType.IsValueType ? Activator.CreateInstance(cp[i].ParameterType) : null);
                    _canny.Invoke(null, ca);
                    edgePixels = Convert.ToInt64(_countNonZero.Invoke(null, new object[] { edges }));
                }
                return true;
            }
            catch { return false; }
            finally { Dispose(src); Dispose(edges); }
        }
    }
}
