using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace QMC.Vision.Core.Collet
{
    // docs\Collet Finder 원본(ColletFinder.BlobRectFinder/DetectedRect) 이식 — 로직 무변경, 네임스페이스만 변경.

    /// <summary>검출된 회전 사각형(최소면적 외접 사각형).</summary>
    public struct DetectedRect
    {
        public bool Found;
        public PointF Center;      // 중심(이미지 픽셀 좌표)
        public PointF[] Corners;   // 4개 꼭짓점(시계/반시계 순)
        public double Width;       // u축 변 길이
        public double Height;      // v축 변 길이
        public double AngleDeg;    // 긴 변의 수평 기준 각도, (-90, 90]
        public long Area;          // 블랍 픽셀 수
    }

    /// <summary>
    /// 이진 마스크(255=전경)에서 연결요소(블랍)를 찾아, 가장 큰 블랍의
    /// 최소면적 외접 사각형(중심/각도/4변)을 구한다.
    ///   1) 연결요소 라벨링(8-이웃)으로 가장 큰 블랍 선택
    ///   2) 행별 좌/우 극점만 모아 후보 점 축소(볼록껍질에 충분)
    ///   3) 볼록껍질(Andrew monotone chain)
    ///   4) 회전 캘리퍼스로 최소면적 사각형 → 중심/각도/4변
    /// </summary>
    public static class BlobRectFinder
    {
        /// <summary>직전 호출에서 연결요소 라벨링(블랍 찾기)에 걸린 시간(ms).</summary>
        public static long LastLabelMs { get; private set; }
        /// <summary>직전 호출에서 최소면적 사각형·각도 계산에 걸린 시간(ms).</summary>
        public static long LastRectMs { get; private set; }

        // 라벨링용 버퍼는 호출마다 재할당하지 않고 재사용한다(12000² 기준 약 1.15GB 할당/GC 제거).
        // _labels=union-find parent, _stack=루트별 area 카운터. 직렬 호출 전제(UI 스레드).
        private static int[] _labels;
        private static int[] _stack;

        /// <summary>
        /// 블랍(연결요소) 라벨링 없이 전경 픽셀의 2차 모멘트로 센터·각도·외접사각형을 구한다.
        /// 큰 배열 할당이 없고 마스크를 2번 병렬 스캔만 하므로 라벨링보다 훨씬 빠르다.
        /// 단, 전경에 노이즈/다중 객체가 섞이면 편향될 수 있다(단일 우세 객체 가정).
        /// </summary>
        public static DetectedRect FindByMoments(byte[] mask, int w, int h)
        {
            LastLabelMs = 0;
            LastRectMs = 0;
            var result = new DetectedRect { Found = false };
            if (mask == null || w <= 0 || h <= 0) return result;

            int threads = Math.Max(1, Environment.ProcessorCount);
            var po = new ParallelOptions { MaxDegreeOfParallelism = threads };
            var sw = Stopwatch.StartNew();

            // 1) 전경 픽셀의 모멘트 누적(스레드별 부분합 → 합산). [N, Σx, Σy, Σx², Σxy, Σy²]
            long gN = 0;
            double gX = 0, gY = 0, gXX = 0, gXY = 0, gYY = 0;
            object lk = new object();
            Parallel.For(0, h, po, () => new double[6], (y, state, acc) =>
            {
                int row = y * w;
                double n = acc[0], sx = acc[1], sy = acc[2], sxx = acc[3], sxy = acc[4], syy = acc[5];
                for (int x = 0; x < w; x++)
                {
                    if (mask[row + x] != 0)
                    {
                        n += 1;
                        sx += x; sy += y;
                        sxx += (double)x * x;
                        sxy += (double)x * y;
                        syy += (double)y * y;
                    }
                }
                acc[0] = n; acc[1] = sx; acc[2] = sy; acc[3] = sxx; acc[4] = sxy; acc[5] = syy;
                return acc;
            }, acc =>
            {
                lock (lk)
                {
                    gN += (long)acc[0];
                    gX += acc[1]; gY += acc[2];
                    gXX += acc[3]; gXY += acc[4]; gYY += acc[5];
                }
            });

            LastLabelMs = sw.ElapsedMilliseconds;   // 전경 모멘트 분석 시간
            if (gN < 1) return result;

            double cx = gX / gN, cy = gY / gN;
            double mu20 = gXX / gN - cx * cx;
            double mu02 = gYY / gN - cy * cy;
            double mu11 = gXY / gN - cx * cy;
            double theta = 0.5 * Math.Atan2(2.0 * mu11, mu20 - mu02);  // 주축 각도

            // 2) 주축(u)·수직축(v)에 전경을 투영해 외접사각형 범위(min/max) 산출
            sw.Restart();
            double ux = Math.Cos(theta), uy = Math.Sin(theta);
            double vx = -uy, vy = ux;
            double minU = double.MaxValue, maxU = double.MinValue;
            double minV = double.MaxValue, maxV = double.MinValue;
            Parallel.For(0, h, po,
                () => new double[4] { double.MaxValue, double.MinValue, double.MaxValue, double.MinValue },
                (y, state, ext) =>
                {
                    int row = y * w;
                    double a0 = ext[0], a1 = ext[1], a2 = ext[2], a3 = ext[3];
                    double yU = y * uy, yV = y * vy;   // x 루프 밖으로 뺀 행 상수
                    for (int x = 0; x < w; x++)
                    {
                        if (mask[row + x] != 0)
                        {
                            double pu = x * ux + yU;
                            double pv = x * vx + yV;
                            if (pu < a0) a0 = pu;
                            if (pu > a1) a1 = pu;
                            if (pv < a2) a2 = pv;
                            if (pv > a3) a3 = pv;
                        }
                    }
                    ext[0] = a0; ext[1] = a1; ext[2] = a2; ext[3] = a3;
                    return ext;
                }, ext =>
                {
                    lock (lk)
                    {
                        if (ext[0] < minU) minU = ext[0];
                        if (ext[1] > maxU) maxU = ext[1];
                        if (ext[2] < minV) minV = ext[2];
                        if (ext[3] > maxV) maxV = ext[3];
                    }
                });

            // u,v 는 정규직교 기저 → (cu,cv) 좌표를 이미지 좌표로 복원: P = cu·u + cv·v
            Func<double, double, PointF> corner = (cu, cv) => new PointF(
                (float)(cu * ux + cv * vx),
                (float)(cu * uy + cv * vy));

            result.Corners = new PointF[]
            {
                corner(minU, minV),
                corner(maxU, minV),
                corner(maxU, maxV),
                corner(minU, maxV),
            };
            result.Center = corner((minU + maxU) / 2.0, (minV + maxV) / 2.0);

            double wU = maxU - minU;
            double hV = maxV - minV;
            result.Width = wU;
            result.Height = hV;

            double ax = ux, ay = uy;
            if (hV > wU) { ax = vx; ay = vy; }   // 긴 변 방향
            double deg = Math.Atan2(ay, ax) * 180.0 / Math.PI;
            while (deg <= -90) deg += 180;
            while (deg > 90) deg -= 180;
            result.AngleDeg = deg;

            result.Area = gN;
            result.Found = true;
            LastRectMs = sw.ElapsedMilliseconds;
            return result;
        }

        /// <summary>Union-Find: 경로 절반 압축으로 루트 탐색.</summary>
        private static int FindSet(int[] p, int i)
        {
            while (p[i] != i) { p[i] = p[p[i]]; i = p[i]; }
            return i;
        }

        /// <summary>Union-Find: 두 집합 병합(작은 인덱스를 루트로 → 결정적).</summary>
        private static void UnionSet(int[] p, int a, int b)
        {
            int ra = FindSet(p, a);
            int rb = FindSet(p, b);
            if (ra == rb) return;
            if (ra < rb) p[rb] = ra; else p[ra] = rb;
        }

        public static DetectedRect FindLargestRect(byte[] mask, int w, int h)
        {
            LastLabelMs = 0;
            LastRectMs = 0;
            var result = new DetectedRect { Found = false };
            if (mask == null || w <= 0 || h <= 0) return result;

            var sw = Stopwatch.StartNew();

            int n = w * h;
            int threads = Math.Max(1, Environment.ProcessorCount);
            var po = new ParallelOptions { MaxDegreeOfParallelism = threads };

            // 재사용 버퍼: parent(union-find 부모), area(루트별 픽셀 수)
            if (_labels == null || _labels.Length < n)
            {
                _labels = new int[n];
                _stack = new int[n];
            }
            int[] parent = _labels;   // union-find 부모
            int[] area = _stack;      // 루트별 면적 카운터

            // parent[i] = i 초기화(병렬)
            Parallel.For(0, threads, po, t =>
            {
                int lo = (int)((long)n * t / threads);
                int hi = (int)((long)n * (t + 1) / threads);
                for (int i = lo; i < hi; i++) parent[i] = i;
            });

            // 1) 연결요소 라벨링(8-이웃) — 행 스트립을 코어별로 병렬 union-find.
            //    각 스트립은 자기 내부 픽셀만 union 하므로 스레드 간 쓰기 영역이 겹치지 않는다(경합 없음).
            //    스트립 경계는 이후 순차로 한 번 병합하고, 마지막에 루트로 평탄화하며 최대 블랍을 고른다.
            int strips = Math.Min(threads, h);
            int[] stripStart = new int[strips + 1];
            for (int s = 0; s <= strips; s++) stripStart[s] = (int)((long)h * s / strips);

            Parallel.For(0, strips, po, s =>
            {
                int ys = stripStart[s];
                int ye = stripStart[s + 1];
                for (int y = ys; y < ye; y++)
                {
                    int row = y * w;
                    int up = row - w;
                    for (int x = 0; x < w; x++)
                    {
                        int idx = row + x;
                        if (mask[idx] == 0) continue;
                        // 래스터 순서상 이미 처리된 뒤쪽 이웃(W, N, NW, NE)만 union → 8-이웃 전체 포함
                        if (x > 0 && mask[idx - 1] != 0) UnionSet(parent, idx, idx - 1);                 // W
                        if (y > ys)   // 이전 행이 스트립 내부일 때만(경계는 2단계에서 처리)
                        {
                            if (mask[up + x] != 0) UnionSet(parent, idx, up + x);                          // N
                            if (x > 0 && mask[up + x - 1] != 0) UnionSet(parent, idx, up + x - 1);         // NW
                            if (x < w - 1 && mask[up + x + 1] != 0) UnionSet(parent, idx, up + x + 1);     // NE
                        }
                    }
                }
            });

            // 2) 스트립 경계 병합(윗 스트립 마지막 행 ↔ 아랫 스트립 첫 행)만 순차로 union
            for (int s = 1; s < strips; s++)
            {
                int y = stripStart[s];
                if (y <= 0 || y >= h) continue;
                int row = y * w;
                int up = row - w;
                for (int x = 0; x < w; x++)
                {
                    int idx = row + x;
                    if (mask[idx] == 0) continue;
                    if (mask[up + x] != 0) UnionSet(parent, idx, up + x);                          // N
                    if (x > 0 && mask[up + x - 1] != 0) UnionSet(parent, idx, up + x - 1);         // NW
                    if (x < w - 1 && mask[up + x + 1] != 0) UnionSet(parent, idx, up + x + 1);     // NE
                }
            }

            // 3) 평탄화 + 루트별 면적 집계(순차 선형 스캔). 각 전경 픽셀을 루트로 직접 연결(이후 병렬 읽기 안전).
            Array.Clear(area, 0, n);
            int bestRoot = -1;
            long bestArea = 0;
            for (int i = 0; i < n; i++)
            {
                if (mask[i] == 0) continue;
                int r = FindSet(parent, i);
                parent[i] = r;
                int a = ++area[r];
                if (a > bestArea) { bestArea = a; bestRoot = r; }
            }

            LastLabelMs = sw.ElapsedMilliseconds;   // 1) 블랍 찾기(라벨링) 종료
            if (bestRoot < 0) return result; // 전경 없음

            sw.Restart();
            // 4) 가장 큰 블랍의 행별 좌/우 극점 + 열별 상/하 극점 수집.
            //    행 극점 = 볼록껍질 후보(껍질 꼭짓점은 반드시 그 행의 min/max x). 열 극점은
            //    수평에 가까운 변(상/하)의 경계점 — 라인 피팅 입력으로 함께 쓴다.
            //    행끼리 독립이라 병렬. 열 극점은 스레드 로컬 버퍼로 모아 마지막에 병합(경합 없음).
            int[] rowMin = new int[h];
            int[] rowMax = new int[h];
            int[] colMin = new int[w];
            int[] colMax = new int[w];
            for (int x = 0; x < w; x++) { colMin[x] = int.MaxValue; colMax[x] = -1; }
            object colLock = new object();
            Parallel.For(0, h, po,
                () =>
                {
                    var loc = new int[2][] { new int[w], new int[w] };
                    for (int x = 0; x < w; x++) { loc[0][x] = int.MaxValue; loc[1][x] = -1; }
                    return loc;
                },
                (y, state, loc) =>
                {
                    int row = y * w;
                    int minx = int.MaxValue, maxx = -1;
                    for (int x = 0; x < w; x++)
                    {
                        if (parent[row + x] == bestRoot)
                        {
                            if (x < minx) minx = x;
                            if (x > maxx) maxx = x;
                            if (y < loc[0][x]) loc[0][x] = y;
                            if (y > loc[1][x]) loc[1][x] = y;
                        }
                    }
                    rowMin[y] = minx;
                    rowMax[y] = maxx;
                    return loc;
                },
                loc =>
                {
                    lock (colLock)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            if (loc[0][x] < colMin[x]) colMin[x] = loc[0][x];
                            if (loc[1][x] > colMax[x]) colMax[x] = loc[1][x];
                        }
                    }
                });

            var pts = new List<PointF>();
            for (int y = 0; y < h; y++)
            {
                int maxx = rowMax[y];
                if (maxx >= 0)
                {
                    int minx = rowMin[y];
                    pts.Add(new PointF(minx, y));
                    if (maxx != minx) pts.Add(new PointF(maxx, y));
                }
            }

            // 3) 볼록껍질
            var hull = ConvexHull(pts);
            if (hull.Count < 2) { LastRectMs = sw.ElapsedMilliseconds; return result; }

            // 4) 최소면적 사각형(회전 캘리퍼스) — 초기 방향/범위 추정.
            RectFrame frame;
            result = MinAreaRect(hull, out frame);
            result.Area = bestArea;
            result.Found = true;

            // 5) 외곽 라인 피팅 정련 — 최소면적 사각형은 가장 삐져나온 점(스파이크)에 '접해' 외곽이
            //    지저분하면 변/각도가 끌려간다. 변마다 경계점 '전체'를 최소제곱 직선으로 피팅해
            //    평균 외곽 라인으로 사각형을 다시 구한다(점 부족/퇴화 시 초기 사각형 유지).
            var edgePts = new List<PointF>(pts);
            for (int x = 0; x < w; x++)
            {
                int maxy = colMax[x];
                if (maxy >= 0)
                {
                    int miny = colMin[x];
                    edgePts.Add(new PointF(x, miny));
                    if (maxy != miny) edgePts.Add(new PointF(x, maxy));
                }
            }
            try
            {
                DetectedRect fitted = RefineByEdgeLineFit(edgePts, frame);
                if (fitted.Found)
                {
                    fitted.Area = bestArea;
                    result = fitted;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[BlobRectFinder] 외곽 라인 피팅 실패 → min-area rect 유지: " + ex.Message);
            }

            LastRectMs = sw.ElapsedMilliseconds;   // 2~5) 극점·볼록껍질·각도·라인피팅 계산 종료
            return result;
        }

        /// <summary>초기 최소면적 사각형의 방향/범위(u=변 방향 단위벡터, v=u 수직). 라인 피팅의 기준 좌표계.</summary>
        private struct RectFrame
        {
            public double Ux, Uy;                  // u축 단위벡터(v축 = (-Uy, Ux))
            public double MinU, MaxU, MinV, MaxV;  // u/v 투영 범위
        }

        /// <summary>변별 외곽 라인 피팅 — 경계점을 초기 사각형의 4변에 귀속시키고(코너 부근 제외),
        /// 각 변을 점 '전체'의 최소제곱 직선으로 피팅한 뒤 인접 변 교점으로 코너/중심/각도를 재계산한다.
        /// 스파이크(삐죽 튀어나온 점)는 다수 점 평균에 희석되어 외곽 라인이 본체 에지를 따른다.
        /// 변 점 부족/기울기 폭주/교점 퇴화 시 Found=false(호출측이 초기 사각형 유지).</summary>
        private static DetectedRect RefineByEdgeLineFit(List<PointF> edgePts, RectFrame f)
        {
            var rect = new DetectedRect { Found = false };
            if (edgePts == null || edgePts.Count < 16) return rect;

            double ux = f.Ux, uy = f.Uy;
            double vx = -f.Uy, vy = f.Ux;
            double rw = f.MaxU - f.MinU;
            double rh = f.MaxV - f.MinV;
            if (rw < 8 || rh < 8) return rect;

            double bandU = 0.25 * rw;      // 변 소속 최대 거리 — 스파이크 깊이(안쪽 진짜 에지까지) 흡수
            double bandV = 0.25 * rh;
            double endCutU = 0.10 * rw;    // 코너 부근(양끝 10%) 제외 — 인접 변 점 오염 방지
            double endCutV = 0.10 * rh;

            // 변별 최소제곱 누적: [n, Σx, Σy, Σx², Σxy]. 독립변수 = 변 방향, 종속변수 = 수직 방향.
            //   side 0=V-(pv=min쪽), 1=V+, 2=U-, 3=U+
            var acc = new double[4][];
            for (int i = 0; i < 4; i++) acc[i] = new double[5];

            for (int k = 0; k < edgePts.Count; k++)
            {
                PointF p = edgePts[k];
                double pu = p.X * ux + p.Y * uy;
                double pv = p.X * vx + p.Y * vy;
                double dVm = pv - f.MinV, dVp = f.MaxV - pv;
                double dUm = pu - f.MinU, dUp = f.MaxU - pu;

                double dV = Math.Min(dVm, dVp);
                double dU = Math.Min(dUm, dUp);
                if (dV <= dU)
                {
                    if (dV > bandV) continue;
                    if (pu < f.MinU + endCutU || pu > f.MaxU - endCutU) continue;
                    Accumulate(acc[dVm <= dVp ? 0 : 1], pu, pv);
                }
                else
                {
                    if (dU > bandU) continue;
                    if (pv < f.MinV + endCutV || pv > f.MaxV - endCutV) continue;
                    Accumulate(acc[dUm <= dUp ? 2 : 3], pv, pu);
                }
            }

            // 각 변 직선: (종속) = slope×(독립) + icept. 점 부족/퇴화 시 초기 사각형의 변으로 폴백(slope=0).
            double[] slope = new double[4];
            double[] icept = new double[4];
            double[] fallback = { f.MinV, f.MaxV, f.MinU, f.MaxU };
            for (int i = 0; i < 4; i++)
                if (!FitLine(acc[i], out slope[i], out icept[i])) { slope[i] = 0; icept[i] = fallback[i]; }

            // 코너 = U변(pu = c·pv + d) ↔ V변(pv = a·pu + b) 교점 → 이미지 좌표 복원(P = pu·u + pv·v).
            PointF c00, c10, c11, c01;
            if (!IntersectUV(slope[2], icept[2], slope[0], icept[0], ux, uy, vx, vy, out c00)) return rect;
            if (!IntersectUV(slope[3], icept[3], slope[0], icept[0], ux, uy, vx, vy, out c10)) return rect;
            if (!IntersectUV(slope[3], icept[3], slope[1], icept[1], ux, uy, vx, vy, out c11)) return rect;
            if (!IntersectUV(slope[2], icept[2], slope[1], icept[1], ux, uy, vx, vy, out c01)) return rect;

            rect.Corners = new PointF[] { c00, c10, c11, c01 };
            rect.Center = new PointF((c00.X + c10.X + c11.X + c01.X) / 4f,
                                     (c00.Y + c10.Y + c11.Y + c01.Y) / 4f);

            double wLen = (Dist(c00, c10) + Dist(c01, c11)) / 2.0;   // u 방향 변 길이(마주보는 변 평균)
            double hLen = (Dist(c00, c01) + Dist(c10, c11)) / 2.0;   // v 방향 변 길이
            rect.Width = wLen;
            rect.Height = hLen;

            // 긴 변 기준 각도 (-90, 90] — 기존 표기와 동일.
            PointF e0, e1;
            if (wLen >= hLen) { e0 = c00; e1 = c10; } else { e0 = c00; e1 = c01; }
            double deg = Math.Atan2(e1.Y - e0.Y, e1.X - e0.X) * 180.0 / Math.PI;
            while (deg <= -90) deg += 180;
            while (deg > 90) deg -= 180;
            rect.AngleDeg = deg;

            rect.Found = true;
            return rect;
        }

        private static void Accumulate(double[] s, double x, double y)
        {
            s[0] += 1; s[1] += x; s[2] += y; s[3] += x * x; s[4] += x * y;
        }

        /// <summary>최소제곱 직선 y = slope·x + icept. 점 8개 미만/x 분산 퇴화/기울기 폭주(초기 방향 대비
        /// 약 ±10° 초과 — 스파이크가 아니라 방향 추정 자체가 틀린 경우)면 false.</summary>
        private static bool FitLine(double[] s, out double slope, out double icept)
        {
            slope = 0; icept = 0;
            double n = s[0];
            if (n < 8) return false;
            double den = n * s[3] - s[1] * s[1];
            if (Math.Abs(den) < 1e-9) return false;
            slope = (n * s[4] - s[1] * s[2]) / den;
            icept = (s[2] - slope * s[1]) / n;
            if (Math.Abs(slope) > 0.18) return false;   // tan(10°) ≈ 0.176
            return true;
        }

        /// <summary>u/v 평면에서 U변(pu = c·pv + d)과 V변(pv = a·pu + b)의 교점 → 이미지 좌표.</summary>
        private static bool IntersectUV(double c, double d, double a, double b,
                                        double ux, double uy, double vx, double vy, out PointF p)
        {
            p = PointF.Empty;
            double den = 1.0 - a * c;
            if (Math.Abs(den) < 1e-6) return false;
            double pv = (a * d + b) / den;
            double pu = c * pv + d;
            p = new PointF((float)(pu * ux + pv * vx), (float)(pu * uy + pv * vy));
            return true;
        }

        private static double Dist(PointF a, PointF b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Andrew's monotone chain. 결과는 반시계 방향 껍질(중복 끝점 제외).</summary>
        private static List<PointF> ConvexHull(List<PointF> pts)
        {
            if (pts.Count < 3) return new List<PointF>(pts);
            pts.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            int n = pts.Count;
            var hull = new PointF[2 * n];
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                while (k >= 2 && Cross(hull[k - 2], hull[k - 1], pts[i]) <= 0) k--;
                hull[k++] = pts[i];
            }
            int lower = k + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (k >= lower && Cross(hull[k - 2], hull[k - 1], pts[i]) <= 0) k--;
                hull[k++] = pts[i];
            }
            var res = new List<PointF>();
            for (int i = 0; i < k - 1; i++) res.Add(hull[i]); // 마지막=시작점 → 제외
            return res;
        }

        private static double Cross(PointF o, PointF a, PointF b)
        {
            return (a.X - o.X) * (double)(b.Y - o.Y) - (a.Y - o.Y) * (double)(b.X - o.X);
        }

        /// <summary>볼록껍질에 대한 최소면적 외접 사각형(회전 캘리퍼스).
        /// frame = 선택된 방향/투영 범위 — 외곽 라인 피팅(RefineByEdgeLineFit)의 기준 좌표계.</summary>
        private static DetectedRect MinAreaRect(List<PointF> hull, out RectFrame frame)
        {
            int m = hull.Count;
            double bestArea = double.MaxValue;
            double bUx = 1, bUy = 0, bMinU = 0, bMaxU = 0, bMinV = 0, bMaxV = 0;

            for (int i = 0; i < m; i++)
            {
                PointF a = hull[i];
                PointF b = hull[(i + 1) % m];
                double ex = b.X - a.X;
                double ey = b.Y - a.Y;
                double len = Math.Sqrt(ex * ex + ey * ey);
                if (len < 1e-9) continue;

                double ux = ex / len, uy = ey / len; // u축(현재 변 방향)
                double vx = -uy, vy = ux;             // v축(수직)

                double minU = double.MaxValue, maxU = -double.MaxValue;
                double minV = double.MaxValue, maxV = -double.MaxValue;
                for (int j = 0; j < m; j++)
                {
                    double pu = hull[j].X * ux + hull[j].Y * uy;
                    double pv = hull[j].X * vx + hull[j].Y * vy;
                    if (pu < minU) minU = pu;
                    if (pu > maxU) maxU = pu;
                    if (pv < minV) minV = pv;
                    if (pv > maxV) maxV = pv;
                }

                double area = (maxU - minU) * (maxV - minV);
                if (area < bestArea)
                {
                    bestArea = area;
                    bUx = ux; bUy = uy;
                    bMinU = minU; bMaxU = maxU; bMinV = minV; bMaxV = maxV;
                }
            }

            frame = new RectFrame { Ux = bUx, Uy = bUy, MinU = bMinU, MaxU = bMaxU, MinV = bMinV, MaxV = bMaxV };

            double Vx = -bUy, Vy = bUx;
            Func<double, double, PointF> corner = (cu, cv) => new PointF(
                (float)(cu * bUx + cv * Vx),
                (float)(cu * bUy + cv * Vy));

            var rect = new DetectedRect();
            rect.Corners = new PointF[]
            {
                corner(bMinU, bMinV),
                corner(bMaxU, bMinV),
                corner(bMaxU, bMaxV),
                corner(bMinU, bMaxV),
            };
            rect.Center = corner((bMinU + bMaxU) / 2.0, (bMinV + bMaxV) / 2.0);

            double wU = bMaxU - bMinU;
            double hV = bMaxV - bMinV;
            rect.Width = wU;
            rect.Height = hV;

            // 긴 변의 각도(수평 기준)
            double ax = bUx, ay = bUy;
            if (hV > wU) { ax = Vx; ay = Vy; }
            double deg = Math.Atan2(ay, ax) * 180.0 / Math.PI;
            while (deg <= -90) deg += 180;
            while (deg > 90) deg -= 180;
            rect.AngleDeg = deg;

            return rect;
        }
    }
}
