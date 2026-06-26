using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace ColletFinder
{
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
    ///   4) 회전 캘리퍼스로 최소면적 사각형 → 중심/각도/꼭짓점
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
            // 4) 가장 큰 블랍의 행별 좌/우 극점 수집(볼록껍질 후보)
            //    행끼리 독립이므로 병렬로 각 행의 min/max x 를 구한 뒤 순서대로 모은다.
            int[] rowMin = new int[h];
            int[] rowMax = new int[h];
            Parallel.For(0, h, po, y =>
            {
                int row = y * w;
                int minx = int.MaxValue, maxx = -1;
                for (int x = 0; x < w; x++)
                {
                    if (parent[row + x] == bestRoot)
                    {
                        if (x < minx) minx = x;
                        if (x > maxx) maxx = x;
                    }
                }
                rowMin[y] = minx;
                rowMax[y] = maxx;
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

            // 4) 최소면적 사각형
            result = MinAreaRect(hull);
            result.Area = bestArea;
            result.Found = true;
            LastRectMs = sw.ElapsedMilliseconds;   // 2~4) 극점·볼록껍질·각도 계산 종료
            return result;
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

        /// <summary>볼록껍질에 대한 최소면적 외접 사각형(회전 캘리퍼스).</summary>
        private static DetectedRect MinAreaRect(List<PointF> hull)
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
