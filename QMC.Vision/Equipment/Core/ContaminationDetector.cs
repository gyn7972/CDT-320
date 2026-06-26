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
        {
            if (gray == null || w <= 0 || h <= 0) return new List<Blob>();
            radius = Math.Max(1, radius);

            byte[] closing = Erode(Dilate(gray, w, h, radius), w, h, radius);
            var binary = new byte[gray.Length];
            Parallel.For(0, gray.Length, i =>
            {
                int blackhat = closing[i] - gray[i];      // Black-Hat: 어두운 점일수록 큼
                binary[i] = (blackhat >= threshold) ? (byte)1 : (byte)0;
            });

            var blobs = FindBlobs(binary, w, h, minSize, maxSize);
            return MergeCloseBlobs(blobs, mergeDistance);
        }

        private static byte[] Dilate(byte[] src, int w, int h, int radius)
        {
            var res = new byte[src.Length];
            Parallel.For(0, h, y =>
            {
                int yMin = Math.Max(0, y - radius), yMax = Math.Min(h - 1, y + radius);
                int rowIndex = y * w;
                for (int x = 0; x < w; x++)
                {
                    int xMin = Math.Max(0, x - radius), xMax = Math.Min(w - 1, x + radius);
                    byte max = 0;
                    for (int yy = yMin; yy <= yMax; yy++)
                    {
                        int row = yy * w;
                        for (int xx = xMin; xx <= xMax; xx++) { byte v = src[row + xx]; if (v > max) max = v; }
                    }
                    res[rowIndex + x] = max;
                }
            });
            return res;
        }

        private static byte[] Erode(byte[] src, int w, int h, int radius)
        {
            var res = new byte[src.Length];
            Parallel.For(0, h, y =>
            {
                int yMin = Math.Max(0, y - radius), yMax = Math.Min(h - 1, y + radius);
                int rowIndex = y * w;
                for (int x = 0; x < w; x++)
                {
                    int xMin = Math.Max(0, x - radius), xMax = Math.Min(w - 1, x + radius);
                    byte min = 255;
                    for (int yy = yMin; yy <= yMax; yy++)
                    {
                        int row = yy * w;
                        for (int xx = xMin; xx <= xMax; xx++) { byte v = src[row + xx]; if (v < min) min = v; }
                    }
                    res[rowIndex + x] = min;
                }
            });
            return res;
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
