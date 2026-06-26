using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 이물/오염 검출 — CDT-310 <c>SP_ContaminationInspector.ImageProcessor</c> CPU 경로 충실 포팅.
    /// 모폴로지 Closing(Erode∘Dilate) 후 Black-Hat(=Closing − 원본)으로 어두운 결함을 강조,
    /// 임계 이진화 → 8-연결 블롭(면적 minSize~maxSize) → 근접 블롭 병합(mergeDistance).
    /// Bottom/Side 표면 검사기가 공유한다. 입력은 그레이 배열(ROI 단위).
    /// </summary>
    public static class ContaminationDetector
    {
        public sealed class Blob
        {
            public int Area, MinX, MinY, MaxX, MaxY;
            public double CenterX, CenterY;
            public int Width  => MaxX - MinX + 1;
            public int Height => MaxY - MinY + 1;
        }

        /// <summary>이물 블롭 검출. radius=커널반경, threshold=Black-Hat 임계, minSize/maxSize=면적[px²], mergeDistance=근접병합 px.</summary>
        public static List<Blob> Detect(byte[] gray, int w, int h, int radius, int threshold, int minSize, int maxSize, int mergeDistance)
            => Detect(gray, w, h, radius, threshold, minSize, maxSize, mergeDistance, out _);

        /// <summary>위와 동일 + Black-Hat 이진 결과(디버그 단계 이미지용)를 out 으로 반환.</summary>
        public static List<Blob> Detect(byte[] gray, int w, int h, int radius, int threshold, int minSize, int maxSize, int mergeDistance, out byte[] binary)
        {
            binary = null;
            if (gray == null || w <= 0 || h <= 0) return new List<Blob>();
            radius = Math.Max(1, radius);

            byte[] closing = Erode(Dilate(gray, w, h, radius), w, h, radius);
            var bin = new byte[gray.Length];
            Parallel.For(0, gray.Length, i =>
            {
                int blackhat = closing[i] - gray[i];      // Black-Hat: 어두운 점일수록 큼
                bin[i] = (blackhat >= threshold) ? (byte)1 : (byte)0;
            });
            binary = bin;

            var blobs = FindBlobs(bin, w, h, minSize, maxSize);
            return MergeCloseBlobs(blobs, mergeDistance);
        }

        // CUDA 가용 시 GPU 박스 모폴로지, 아니면 CPU(분리형). 원칙: "CUDA 가능하면 CUDA, 아니면 CPU 폴백".
        // 박스(정사각) 모폴로지는 가로·세로로 분리 가능 → 1D 슬라이딩 윈도우 최대/최소(단조 deque, 픽셀당 O(1)).
        // 커널 반경과 무관하게 O(w·h) — naive O(w·h·r²) 대비 r=21 이면 ~1800배 적은 내부연산(TopHatRadius 증가에도 평탄).
        // 결과는 기존 clamped 박스 모폴로지와 완전히 동일(Python 등가성 검증).
        private static byte[] Dilate(byte[] src, int w, int h, int radius)
        {
            if (CudaInterop.TryBoxMorph(src, w, h, radius, true, out var gpu)) { GpuBackend.Note("Contamination", ComputeBackend.Cuda); return gpu; }
            GpuBackend.Note("Contamination", ComputeBackend.Cpu);
            return SepMorph(src, w, h, radius, true);
        }
        private static byte[] Erode(byte[] src, int w, int h, int radius)
        {
            if (CudaInterop.TryBoxMorph(src, w, h, radius, false, out var gpu)) { GpuBackend.Note("Contamination", ComputeBackend.Cuda); return gpu; }
            GpuBackend.Note("Contamination", ComputeBackend.Cpu);
            return SepMorph(src, w, h, radius, false);
        }

        private static byte[] SepMorph(byte[] src, int w, int h, int radius, bool isMax)
        {
            var tmp = new byte[src.Length];
            Parallel.For(0, h, y => Line1D(src, tmp, y * w, 1, w, radius, isMax));  // 가로 패스
            var res = new byte[src.Length];
            Parallel.For(0, w, x => Line1D(tmp, res, x, w, h, radius, isMax));       // 세로 패스
            return res;
        }

        /// <summary>한 라인(baseIdx+stride 간격, n개)에 대해 윈도우 [i-r, i+r](경계 clamp) 최대/최소를 단조 deque 로 O(n) 계산.</summary>
        private static void Line1D(byte[] src, byte[] dst, int baseIdx, int stride, int n, int r, bool isMax)
        {
            var dq = new int[n];   // 라인 위치 인덱스 deque(앞=현 윈도우의 최댓/최솟값 후보)
            int head = 0, tail = 0, j = 0;
            for (int i = 0; i < n; i++)
            {
                int hi = Math.Min(n - 1, i + r), lo = Math.Max(0, i - r);
                while (j <= hi)
                {
                    byte v = src[baseIdx + j * stride];
                    while (tail > head)
                    {
                        byte b = src[baseIdx + dq[tail - 1] * stride];
                        if (isMax ? (b <= v) : (b >= v)) tail--; else break;
                    }
                    dq[tail++] = j; j++;
                }
                while (dq[head] < lo) head++;
                dst[baseIdx + i * stride] = src[baseIdx + dq[head] * stride];
            }
        }

        private static List<Blob> FindBlobs(byte[] binary, int w, int h, int minSize, int maxSize)
        {
            var blobs = new List<Blob>();
            var visited = new bool[binary.Length];
            var queue = new Queue<int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int index = y * w + x;
                    if (binary[index] == 0 || visited[index]) continue;
                    visited[index] = true; queue.Clear(); queue.Enqueue(index);
                    int area = 0, minX = x, minY = y, maxX = x, maxY = y; long sumX = 0, sumY = 0;
                    while (queue.Count > 0)
                    {
                        int cur = queue.Dequeue();
                        int cy = cur / w, cx = cur - cy * w;
                        area++; sumX += cx; sumY += cy;
                        if (cx < minX) minX = cx; if (cx > maxX) maxX = cx;
                        if (cy < minY) minY = cy; if (cy > maxY) maxY = cy;
                        int ys = Math.Max(0, cy - 1), ye = Math.Min(h - 1, cy + 1);
                        int xs = Math.Max(0, cx - 1), xe = Math.Min(w - 1, cx + 1);
                        for (int ny = ys; ny <= ye; ny++)
                        {
                            int row = ny * w;
                            for (int nx = xs; nx <= xe; nx++)
                            {
                                int nb = row + nx;
                                if (binary[nb] == 0 || visited[nb]) continue;
                                visited[nb] = true; queue.Enqueue(nb);
                            }
                        }
                    }
                    if (area < minSize || area > maxSize) continue;
                    blobs.Add(new Blob { Area = area, MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY, CenterX = sumX / (double)area, CenterY = sumY / (double)area });
                }
            return blobs;
        }

        private static List<Blob> MergeCloseBlobs(List<Blob> blobs, int distance)
        {
            if (blobs.Count <= 1) return blobs;
            var uf = new int[blobs.Count];
            for (int i = 0; i < uf.Length; i++) uf[i] = i;
            Func<int, int> find = null;
            find = a => { while (uf[a] != a) { uf[a] = uf[uf[a]]; a = uf[a]; } return a; };
            for (int i = 0; i < blobs.Count; i++)
                for (int j = i + 1; j < blobs.Count; j++)
                    if (RectDistance(blobs[i], blobs[j]) <= distance) uf[find(i)] = find(j);

            var map = new Dictionary<int, Blob>();
            for (int i = 0; i < blobs.Count; i++)
            {
                int root = find(i);
                if (!map.TryGetValue(root, out var acc)) { map[root] = Clone(blobs[i]); }
                else
                {
                    var b = blobs[i];
                    acc.Area += b.Area;
                    if (b.MinX < acc.MinX) acc.MinX = b.MinX; if (b.MaxX > acc.MaxX) acc.MaxX = b.MaxX;
                    if (b.MinY < acc.MinY) acc.MinY = b.MinY; if (b.MaxY > acc.MaxY) acc.MaxY = b.MaxY;
                    acc.CenterX = (acc.CenterX + b.CenterX) / 2.0;
                    acc.CenterY = (acc.CenterY + b.CenterY) / 2.0;
                }
            }
            return new List<Blob>(map.Values);
        }

        private static Blob Clone(Blob b) => new Blob { Area = b.Area, MinX = b.MinX, MinY = b.MinY, MaxX = b.MaxX, MaxY = b.MaxY, CenterX = b.CenterX, CenterY = b.CenterY };

        private static double RectDistance(Blob a, Blob b)
        {
            int dx = 0;
            if (a.MaxX < b.MinX) dx = b.MinX - a.MaxX; else if (b.MaxX < a.MinX) dx = a.MinX - b.MaxX;
            int dy = 0;
            if (a.MaxY < b.MinY) dy = b.MinY - a.MaxY; else if (b.MaxY < a.MinY) dy = a.MinY - b.MaxY;
            return Math.Sqrt(dx * (double)dx + dy * (double)dy);
        }
    }
}
