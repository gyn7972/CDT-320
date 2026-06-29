using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
#if OPENCV
using OpenCvSharp;
#endif

namespace SP_ContaminationInspector.ImageProcessing
{
    public static class ImageProcessor
    {
        public static Bitmap ProcessImage(
            Bitmap source,
            int kernelRadius,
            int threshold,
            int minSize,
            int maxSize,
            int mergeDistance,
            bool useCudaBlobs,
            bool useOpenCv,
            out int defectCount)
        {
#if OPENCV
            if (useOpenCv)
            {
                if (TryProcessWithOpenCv(source, kernelRadius, threshold, minSize, maxSize, mergeDistance, out var openCvBlobs))
                {
                    defectCount = openCvBlobs.Count;
                    return RenderResult(source, openCvBlobs);
                }
            }
#endif

            if (useCudaBlobs)
            {
                int maxBlobs = 5000;
                var cudaResults = new BlobInfo[maxBlobs];
                if (TryProcessWithCudaFull(source, kernelRadius, threshold, minSize, maxSize, mergeDistance, cudaResults, out int count))
                {
                    var blobs = new List<Blob>(count);
                    for (int i = 0; i < count; i++)
                    {
                        blobs.Add(new Blob(
                            cudaResults[i].Area,
                            cudaResults[i].MinX,
                            cudaResults[i].MinY,
                            cudaResults[i].MaxX,
                            cudaResults[i].MaxY,
                            cudaResults[i].CenterX,
                            cudaResults[i].CenterY));
                    }
                    defectCount = blobs.Count;
                    return RenderResult(source, blobs);
                }
            }

            if (TryProcessWithCudaBinary(source, kernelRadius, threshold, out var binary, out int width, out int height))
            {
                var cudaBlobs = FindBlobs(binary, width, height, minSize, maxSize);
                var cudaMerged = MergeCloseBlobs(cudaBlobs, mergeDistance);
                defectCount = cudaMerged.Count;
                return RenderResult(source, cudaMerged);
            }

            var grayscale = GetGrayscaleBytes(source, out int cpuWidth, out int cpuHeight);
            var closing = Erode(Dilate(grayscale, cpuWidth, cpuHeight, kernelRadius), cpuWidth, cpuHeight, kernelRadius);
            var topHat = new byte[grayscale.Length];

            Parallel.For(0, grayscale.Length, i =>
            {
                int value = closing[i] - grayscale[i];
                topHat[i] = (byte)(value > 0 ? value : 0);
            });

            var cpuBinary = new byte[topHat.Length];
            Parallel.For(0, topHat.Length, i =>
            {
                cpuBinary[i] = topHat[i] >= threshold ? (byte)1 : (byte)0;
            });

            var cpuBlobs = FindBlobs(cpuBinary, cpuWidth, cpuHeight, minSize, maxSize);
            var merged = MergeCloseBlobs(cpuBlobs, mergeDistance);
            defectCount = merged.Count;

            return RenderResult(source, merged);
        }

#if OPENCV
        private static bool TryProcessWithOpenCv(Bitmap source, int kernelRadius, int threshold, int minSize, int maxSize, int mergeDistance, out List<Blob> blobs)
        {
            blobs = null;
            var rect = new Rectangle(0, 0, source.Width, source.Height);
            var data = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                using (var mat = Mat.FromPixelData(source.Height, source.Width, MatType.CV_8UC3, data.Scan0, data.Stride))
                using (var gray = new Mat())
                using (var blackhat = new Mat())
                using (var binary = new Mat())
                using (var labels = new Mat())
                using (var stats = new Mat())
                using (var centroids = new Mat())
                {
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

                    int kernelSize = Math.Max(1, kernelRadius * 2 + 1);
                    using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(kernelSize, kernelSize)))
                    {
                        Cv2.MorphologyEx(gray, blackhat, MorphTypes.BlackHat, kernel);
                    }

                    Cv2.Threshold(blackhat, binary, threshold, 255, ThresholdTypes.Binary);

                    int count = Cv2.ConnectedComponentsWithStats(binary, labels, stats, centroids, PixelConnectivity.Connectivity8, MatType.CV_32S);
                    var found = new List<Blob>(count);
                    for (int i = 1; i < count; i++)
                    {
                        int area = stats.Get<int>(i, (int)ConnectedComponentsTypes.Area);
                        if (area < minSize || area > maxSize)
                        {
                            continue;
                        }

                        int left = stats.Get<int>(i, (int)ConnectedComponentsTypes.Left);
                        int top = stats.Get<int>(i, (int)ConnectedComponentsTypes.Top);
                        int width = stats.Get<int>(i, (int)ConnectedComponentsTypes.Width);
                        int height = stats.Get<int>(i, (int)ConnectedComponentsTypes.Height);
                        double centerX = centroids.Get<double>(i, 0);
                        double centerY = centroids.Get<double>(i, 1);

                        int minX = left;
                        int minY = top;
                        int maxX = left + width - 1;
                        int maxY = top + height - 1;

                        found.Add(new Blob(area, minX, minY, maxX, maxY, centerX, centerY));
                    }

                    blobs = MergeCloseBlobs(found, mergeDistance);
                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                source.UnlockBits(data);
            }
        }
#endif

        private static bool TryProcessWithCudaFull(Bitmap source, int kernelRadius, int threshold, int minSize, int maxSize, int mergeDistance, BlobInfo[] results, out int count)
        {
            count = 0;
            int width = source.Width;
            int height = source.Height;

            try
            {
                var bgr = GetBgrBytes(source, out int stride);
                int result = CudaFullPipeline(bgr, width, height, stride, kernelRadius, threshold, minSize, maxSize, mergeDistance, results, results.Length, out count);
                return result == 0;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        private static bool TryProcessWithCudaBinary(Bitmap source, int kernelRadius, int threshold, out byte[] binary, out int width, out int height)
        {
            binary = null;
            width = source.Width;
            height = source.Height;

            try
            {
                var bgr = GetBgrBytes(source, out int stride);
                binary = new byte[width * height];
                int result = CudaTopHatThreshold(bgr, width, height, stride, kernelRadius, threshold, binary);
                return result == 0;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        private static Bitmap ConvertTo24bpp(Bitmap bitmap)
        {
            if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
            {
                return (Bitmap)bitmap.Clone();
            }

            var converted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(converted))
            {
                g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
            }

            return converted;
        }

        private static byte[] GetBgrBytes(Bitmap bitmap, out int stride)
        {
            stride = 0;
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                stride = data.Stride;
                var raw = new byte[stride * bitmap.Height];
                Marshal.Copy(data.Scan0, raw, 0, raw.Length);
                return raw;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static byte[] GetGrayscaleBytes(Bitmap bitmap, out int width, out int height)
        {
            width = bitmap.Width;
            height = bitmap.Height;
            var rect = new Rectangle(0, 0, width, height);
            var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                int stride = data.Stride;
                var raw = new byte[stride * height];
                Marshal.Copy(data.Scan0, raw, 0, raw.Length);

                var gray = new byte[width * height];
                int localWidth = width;
                int localStride = stride;
                Parallel.For(0, height, y =>
                {
                    int row = y * localStride;
                    int outputIndex = y * localWidth;
                    for (int x = 0; x < localWidth; x++)
                    {
                        int offset = row + x * 3;
                        byte b = raw[offset];
                        byte g = raw[offset + 1];
                        byte r = raw[offset + 2];
                        gray[outputIndex + x] = (byte)((r * 299 + g * 587 + b * 114 + 500) / 1000);
                    }
                });

                return gray;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static byte[] Dilate(byte[] source, int width, int height, int radius)
        {
            var result = new byte[source.Length];
            Parallel.For(0, height, y =>
            {
                int yMin = Math.Max(0, y - radius);
                int yMax = Math.Min(height - 1, y + radius);
                int rowIndex = y * width;
                for (int x = 0; x < width; x++)
                {
                    int xMin = Math.Max(0, x - radius);
                    int xMax = Math.Min(width - 1, x + radius);
                    byte max = 0;
                    for (int yy = yMin; yy <= yMax; yy++)
                    {
                        int row = yy * width;
                        for (int xx = xMin; xx <= xMax; xx++)
                        {
                            byte value = source[row + xx];
                            if (value > max)
                            {
                                max = value;
                            }
                        }
                    }
                    result[rowIndex + x] = max;
                }
            });

            return result;
        }

        private static byte[] Erode(byte[] source, int width, int height, int radius)
        {
            var result = new byte[source.Length];
            Parallel.For(0, height, y =>
            {
                int yMin = Math.Max(0, y - radius);
                int yMax = Math.Min(height - 1, y + radius);
                int rowIndex = y * width;
                for (int x = 0; x < width; x++)
                {
                    int xMin = Math.Max(0, x - radius);
                    int xMax = Math.Min(width - 1, x + radius);
                    byte min = 255;
                    for (int yy = yMin; yy <= yMax; yy++)
                    {
                        int row = yy * width;
                        for (int xx = xMin; xx <= xMax; xx++)
                        {
                            byte value = source[row + xx];
                            if (value < min)
                            {
                                min = value;
                            }
                        }
                    }
                    result[rowIndex + x] = min;
                }
            });

            return result;
        }

        private static List<Blob> FindBlobs(byte[] binary, int width, int height, int minSize, int maxSize)
        {
            var blobs = new List<Blob>();
            var visited = new bool[binary.Length];
            var queue = new Queue<int>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (binary[index] == 0 || visited[index])
                    {
                        continue;
                    }

                    visited[index] = true;
                    queue.Clear();
                    queue.Enqueue(index);

                    int area = 0;
                    int minX = x;
                    int minY = y;
                    int maxX = x;
                    int maxY = y;
                    long sumX = 0;
                    long sumY = 0;

                    while (queue.Count > 0)
                    {
                        int current = queue.Dequeue();
                        int cy = current / width;
                        int cx = current - (cy * width);
                        area++;
                        sumX += cx;
                        sumY += cy;

                        if (cx < minX) minX = cx;
                        if (cx > maxX) maxX = cx;
                        if (cy < minY) minY = cy;
                        if (cy > maxY) maxY = cy;

                        int yStart = Math.Max(0, cy - 1);
                        int yEnd = Math.Min(height - 1, cy + 1);
                        int xStart = Math.Max(0, cx - 1);
                        int xEnd = Math.Min(width - 1, cx + 1);

                        for (int ny = yStart; ny <= yEnd; ny++)
                        {
                            int row = ny * width;
                            for (int nx = xStart; nx <= xEnd; nx++)
                            {
                                int neighbor = row + nx;
                                if (binary[neighbor] == 0 || visited[neighbor])
                                {
                                    continue;
                                }
                                visited[neighbor] = true;
                                queue.Enqueue(neighbor);
                            }
                        }
                    }

                    if (area < minSize || area > maxSize)
                    {
                        continue;
                    }

                    blobs.Add(new Blob(area, minX, minY, maxX, maxY, sumX / (double)area, sumY / (double)area));
                }
            }

            return blobs;
        }

        private static List<Blob> MergeCloseBlobs(List<Blob> blobs, int distance)
        {
            if (blobs.Count <= 1)
            {
                return blobs;
            }

            var unionFind = new UnionFind(blobs.Count);
            for (int i = 0; i < blobs.Count; i++)
            {
                for (int j = i + 1; j < blobs.Count; j++)
                {
                    if (RectDistance(blobs[i], blobs[j]) <= distance)
                    {
                        unionFind.Union(i, j);
                    }
                }
            }

            var merged = new Dictionary<int, BlobAccumulator>();
            for (int i = 0; i < blobs.Count; i++)
            {
                int root = unionFind.Find(i);
                if (!merged.TryGetValue(root, out var accumulator))
                {
                    accumulator = new BlobAccumulator(blobs[i]);
                    merged[root] = accumulator;
                }
                else
                {
                    accumulator.Add(blobs[i]);
                }
            }

            var result = new List<Blob>(merged.Count);
            foreach (var entry in merged.Values)
            {
                result.Add(entry.ToBlob());
            }

            return result;
        }

        private static double RectDistance(Blob a, Blob b)
        {
            int dx = 0;
            if (a.MaxX < b.MinX)
            {
                dx = b.MinX - a.MaxX;
            }
            else if (b.MaxX < a.MinX)
            {
                dx = a.MinX - b.MaxX;
            }

            int dy = 0;
            if (a.MaxY < b.MinY)
            {
                dy = b.MinY - a.MaxY;
            }
            else if (b.MaxY < a.MinY)
            {
                dy = a.MinY - b.MaxY;
            }

            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static Bitmap RenderResult(Bitmap source, List<Blob> blobs)
        {
            var result = ConvertTo24bpp(source);
            using (var g = Graphics.FromImage(result))
            using (var pen = new Pen(Color.Red, 2))
            {
                foreach (var blob in blobs)
                {
                    g.DrawRectangle(pen, blob.Bounds);
                }
            }

            return result;
        }

        private sealed class Blob
        {
            public Blob(int area, int minX, int minY, int maxX, int maxY, double centerX, double centerY)
            {
                Area = area;
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
                CenterX = centerX;
                CenterY = centerY;
            }

            public int Area { get; }
            public int MinX { get; }
            public int MinY { get; }
            public int MaxX { get; }
            public int MaxY { get; }
            public double CenterX { get; }
            public double CenterY { get; }
            public Rectangle Bounds => Rectangle.FromLTRB(MinX, MinY, MaxX + 1, MaxY + 1);
        }

        private sealed class BlobAccumulator
        {
            private int _area;
            private long _sumX;
            private long _sumY;
            private int _minX;
            private int _minY;
            private int _maxX;
            private int _maxY;

            public BlobAccumulator(Blob blob)
            {
                _area = blob.Area;
                _sumX = (long)(blob.CenterX * blob.Area);
                _sumY = (long)(blob.CenterY * blob.Area);
                _minX = blob.MinX;
                _minY = blob.MinY;
                _maxX = blob.MaxX;
                _maxY = blob.MaxY;
            }

            public void Add(Blob blob)
            {
                _area += blob.Area;
                _sumX += (long)(blob.CenterX * blob.Area);
                _sumY += (long)(blob.CenterY * blob.Area);
                _minX = Math.Min(_minX, blob.MinX);
                _minY = Math.Min(_minY, blob.MinY);
                _maxX = Math.Max(_maxX, blob.MaxX);
                _maxY = Math.Max(_maxY, blob.MaxY);
            }

            public Blob ToBlob()
            {
                double centerX = _sumX / (double)_area;
                double centerY = _sumY / (double)_area;
                return new Blob(_area, _minX, _minY, _maxX, _maxY, centerX, centerY);
            }
        }

        private sealed class UnionFind
        {
            private readonly int[] _parent;
            private readonly int[] _rank;

            public UnionFind(int size)
            {
                _parent = new int[size];
                _rank = new int[size];
                for (int i = 0; i < size; i++)
                {
                    _parent[i] = i;
                }
            }

            public int Find(int value)
            {
                if (_parent[value] != value)
                {
                    _parent[value] = Find(_parent[value]);
                }
                return _parent[value];
            }

            public void Union(int a, int b)
            {
                int rootA = Find(a);
                int rootB = Find(b);
                if (rootA == rootB)
                {
                    return;
                }

                if (_rank[rootA] < _rank[rootB])
                {
                    _parent[rootA] = rootB;
                }
                else if (_rank[rootA] > _rank[rootB])
                {
                    _parent[rootB] = rootA;
                }
                else
                {
                    _parent[rootB] = rootA;
                    _rank[rootA]++;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BlobInfo
        {
            public int Area;
            public int MinX;
            public int MinY;
            public int MaxX;
            public int MaxY;
            public float CenterX;
            public float CenterY;
        }

        [DllImport("CudaBackend.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CudaFullPipeline(
            byte[] bgr,
            int width,
            int height,
            int stride,
            int radius,
            int threshold,
            int minSize,
            int maxSize,
            int mergeDistance,
            [In, Out] BlobInfo[] results,
            int maxResults,
            out int resultCount);

        [DllImport("CudaBackend.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CudaTopHatThreshold(
            byte[] bgr,
            int width,
            int height,
            int stride,
            int radius,
            int threshold,
            byte[] outputMask);
    }
}
