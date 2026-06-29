#include <cuda_runtime.h>
#include <device_launch_parameters.h>
#include <vector>
#include <queue>
#include <algorithm>
#include <cmath>

#ifndef ENABLE_CUDA
#define ENABLE_CUDA 0
#endif

struct BlobInfo
{
    int Area;
    int MinX;
    int MinY;
    int MaxX;
    int MaxY;
    float CenterX;
    float CenterY;
};

#if ENABLE_CUDA
extern "C" __declspec(dllexport) int CudaTopHatThreshold(
    const unsigned char* bgr,
    int width,
    int height,
    int stride,
    int radius,
    int threshold,
    unsigned char* outputMask);

extern "C" __declspec(dllexport) int CudaFullPipeline(
    const unsigned char* bgr,
    int width,
    int height,
    int stride,
    int radius,
    int threshold,
    int minSize,
    int maxSize,
    int mergeDistance,
    BlobInfo* results,
    int maxResults,
    int* resultCount);
#else
extern "C" __declspec(dllexport) int CudaTopHatThreshold(
    const unsigned char*,
    int,
    int,
    int,
    int,
    int,
    unsigned char*)
{
    return -2;
}

extern "C" __declspec(dllexport) int CudaFullPipeline(
    const unsigned char*,
    int,
    int,
    int,
    int,
    int,
    int,
    int,
    int,
    BlobInfo*,
    int,
    int*)
{
    return -2;
}
#endif

namespace
{
    __host__ __device__ inline int IMin(int a, int b)
    {
        return a < b ? a : b;
    }

    __host__ __device__ inline int IMax(int a, int b)
    {
        return a > b ? a : b;
    }

    struct Blob
    {
        int Area;
        int MinX;
        int MinY;
        int MaxX;
        int MaxY;
        double CenterX;
        double CenterY;
    };

    struct BlobAccumulator
    {
        int Area;
        long long SumX;
        long long SumY;
        int MinX;
        int MinY;
        int MaxX;
        int MaxY;

        explicit BlobAccumulator(const Blob& blob)
            : Area(blob.Area)
            , SumX(static_cast<long long>(blob.CenterX * blob.Area))
            , SumY(static_cast<long long>(blob.CenterY * blob.Area))
            , MinX(blob.MinX)
            , MinY(blob.MinY)
            , MaxX(blob.MaxX)
            , MaxY(blob.MaxY)
        {
        }

        void Add(const Blob& blob)
        {
            Area += blob.Area;
            SumX += static_cast<long long>(blob.CenterX * blob.Area);
            SumY += static_cast<long long>(blob.CenterY * blob.Area);
            MinX = std::min(MinX, blob.MinX);
            MinY = std::min(MinY, blob.MinY);
            MaxX = std::max(MaxX, blob.MaxX);
            MaxY = std::max(MaxY, blob.MaxY);
        }

        Blob ToBlob() const
        {
            Blob blob{};
            blob.Area = Area;
            blob.MinX = MinX;
            blob.MinY = MinY;
            blob.MaxX = MaxX;
            blob.MaxY = MaxY;
            blob.CenterX = static_cast<double>(SumX) / static_cast<double>(Area);
            blob.CenterY = static_cast<double>(SumY) / static_cast<double>(Area);
            return blob;
        }
    };

    struct UnionFind
    {
        std::vector<int> Parent;
        std::vector<int> Rank;

        explicit UnionFind(int size)
            : Parent(size)
            , Rank(size, 0)
        {
            for (int i = 0; i < size; i++)
            {
                Parent[i] = i;
            }
        }

        int Find(int value)
        {
            if (Parent[value] != value)
            {
                Parent[value] = Find(Parent[value]);
            }
            return Parent[value];
        }

        void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA == rootB)
            {
                return;
            }

            if (Rank[rootA] < Rank[rootB])
            {
                Parent[rootA] = rootB;
            }
            else if (Rank[rootA] > Rank[rootB])
            {
                Parent[rootB] = rootA;
            }
            else
            {
                Parent[rootB] = rootA;
                Rank[rootA]++;
            }
        }
    };

    double RectDistance(const Blob& a, const Blob& b)
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

        return std::sqrt(static_cast<double>(dx * dx + dy * dy));
    }

    std::vector<Blob> FindBlobsCpu(const std::vector<unsigned char>& binary, int width, int height, int minSize, int maxSize)
    {
        std::vector<Blob> blobs;
        std::vector<unsigned char> visited(binary.size(), 0);
        std::queue<int> queue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (binary[index] == 0 || visited[index])
                {
                    continue;
                }

                visited[index] = 1;
                while (!queue.empty())
                {
                    queue.pop();
                }
                queue.push(index);

                int area = 0;
                int minX = x;
                int minY = y;
                int maxX = x;
                int maxY = y;
                long long sumX = 0;
                long long sumY = 0;

                while (!queue.empty())
                {
                    int current = queue.front();
                    queue.pop();
                    int cy = current / width;
                    int cx = current - (cy * width);
                    area++;
                    sumX += cx;
                    sumY += cy;

                    minX = std::min(minX, cx);
                    maxX = std::max(maxX, cx);
                    minY = std::min(minY, cy);
                    maxY = std::max(maxY, cy);

                    int yStart = std::max(0, cy - 1);
                    int yEnd = std::min(height - 1, cy + 1);
                    int xStart = std::max(0, cx - 1);
                    int xEnd = std::min(width - 1, cx + 1);

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
                            visited[neighbor] = 1;
                            queue.push(neighbor);
                        }
                    }
                }

                if (area < minSize || area > maxSize)
                {
                    continue;
                }

                Blob blob{};
                blob.Area = area;
                blob.MinX = minX;
                blob.MinY = minY;
                blob.MaxX = maxX;
                blob.MaxY = maxY;
                blob.CenterX = static_cast<double>(sumX) / static_cast<double>(area);
                blob.CenterY = static_cast<double>(sumY) / static_cast<double>(area);
                blobs.push_back(blob);
            }
        }

        return blobs;
    }

#if ENABLE_CUDA
    __global__ void BgrToGrayKernel(const unsigned char* bgr, int width, int height, int stride, unsigned char* gray)
    {
        int x = blockIdx.x * blockDim.x + threadIdx.x;
        int y = blockIdx.y * blockDim.y + threadIdx.y;
        if (x >= width || y >= height)
        {
            return;
        }

        int offset = y * stride + x * 3;
        unsigned char b = bgr[offset];
        unsigned char g = bgr[offset + 1];
        unsigned char r = bgr[offset + 2];
        gray[y * width + x] = static_cast<unsigned char>((r * 299 + g * 587 + b * 114 + 500) / 1000);
    }

    __global__ void DilateKernel(const unsigned char* source, int width, int height, int radius, unsigned char* output)
    {
        int x = blockIdx.x * blockDim.x + threadIdx.x;
        int y = blockIdx.y * blockDim.y + threadIdx.y;
        if (x >= width || y >= height)
        {
            return;
        }

        int xMin = IMax(0, x - radius);
        int xMax = IMin(width - 1, x + radius);
        int yMin = IMax(0, y - radius);
        int yMax = IMin(height - 1, y + radius);

        unsigned char maxValue = 0;
        for (int yy = yMin; yy <= yMax; yy++)
        {
            int row = yy * width;
            for (int xx = xMin; xx <= xMax; xx++)
            {
                unsigned char value = source[row + xx];
                if (value > maxValue)
                {
                    maxValue = value;
                }
            }
        }

        output[y * width + x] = maxValue;
    }

    __global__ void ErodeKernel(const unsigned char* source, int width, int height, int radius, unsigned char* output)
    {
        int x = blockIdx.x * blockDim.x + threadIdx.x;
        int y = blockIdx.y * blockDim.y + threadIdx.y;
        if (x >= width || y >= height)
        {
            return;
        }

        int xMin = IMax(0, x - radius);
        int xMax = IMin(width - 1, x + radius);
        int yMin = IMax(0, y - radius);
        int yMax = IMin(height - 1, y + radius);

        unsigned char minValue = 255;
        for (int yy = yMin; yy <= yMax; yy++)
        {
            int row = yy * width;
            for (int xx = xMin; xx <= xMax; xx++)
            {
                unsigned char value = source[row + xx];
                if (value < minValue)
                {
                    minValue = value;
                }
            }
        }

        output[y * width + x] = minValue;
    }

    __global__ void TopHatKernel(const unsigned char* closing, const unsigned char* gray, int length, unsigned char* output)
    {
        int index = blockIdx.x * blockDim.x + threadIdx.x;
        if (index >= length)
        {
            return;
        }

        int value = static_cast<int>(closing[index]) - static_cast<int>(gray[index]);
        output[index] = static_cast<unsigned char>(value > 0 ? value : 0);
    }

    __global__ void ThresholdKernel(const unsigned char* source, int length, int threshold, unsigned char* output)
    {
        int index = blockIdx.x * blockDim.x + threadIdx.x;
        if (index >= length)
        {
            return;
        }

        output[index] = source[index] >= threshold ? 1 : 0;
    }

    __global__ void InitLabelsKernel(const unsigned char* binary, int width, int height, int* labels)
    {
        int x = blockIdx.x * blockDim.x + threadIdx.x;
        int y = blockIdx.y * blockDim.y + threadIdx.y;
        if (x >= width || y >= height)
        {
            return;
        }

        int idx = y * width + x;
        labels[idx] = binary[idx] ? idx : -1;
    }

    __global__ void PropagateLabelsKernel(const int* inputLabels, int* outputLabels, int width, int height, int* changed)
    {
        int x = blockIdx.x * blockDim.x + threadIdx.x;
        int y = blockIdx.y * blockDim.y + threadIdx.y;
        if (x >= width || y >= height)
        {
            return;
        }

        int idx = y * width + x;
        int label = inputLabels[idx];
        if (label < 0)
        {
            outputLabels[idx] = -1;
            return;
        }

        int minLabel = label;
        int yStart = IMax(0, y - 1);
        int yEnd = IMin(height - 1, y + 1);
        int xStart = IMax(0, x - 1);
        int xEnd = IMin(width - 1, x + 1);

        for (int yy = yStart; yy <= yEnd; yy++)
        {
            int row = yy * width;
            for (int xx = xStart; xx <= xEnd; xx++)
            {
                int neighbor = inputLabels[row + xx];
                if (neighbor >= 0 && neighbor < minLabel)
                {
                    minLabel = neighbor;
                }
            }
        }

        outputLabels[idx] = minLabel;
        if (minLabel != label)
        {
            atomicExch(changed, 1);
        }
    }

    __global__ void InitStatsKernel(int length, int width, int height, int* area, int* minX, int* minY, int* maxX, int* maxY, unsigned long long* sumX, unsigned long long* sumY)
    {
        int idx = blockIdx.x * blockDim.x + threadIdx.x;
        if (idx >= length)
        {
            return;
        }

        area[idx] = 0;
        minX[idx] = width;
        minY[idx] = height;
        maxX[idx] = -1;
        maxY[idx] = -1;
        sumX[idx] = 0;
        sumY[idx] = 0;
    }

    __global__ void AccumulateStatsKernel(const int* labels, int width, int height, int* area, int* minX, int* minY, int* maxX, int* maxY, unsigned long long* sumX, unsigned long long* sumY)
    {
        int x = blockIdx.x * blockDim.x + threadIdx.x;
        int y = blockIdx.y * blockDim.y + threadIdx.y;
        if (x >= width || y >= height)
        {
            return;
        }

        int idx = y * width + x;
        int label = labels[idx];
        if (label < 0)
        {
            return;
        }

        atomicAdd(&area[label], 1);
        atomicMin(&minX[label], x);
        atomicMin(&minY[label], y);
        atomicMax(&maxX[label], x);
        atomicMax(&maxY[label], y);
        atomicAdd(&sumX[label], static_cast<unsigned long long>(x));
        atomicAdd(&sumY[label], static_cast<unsigned long long>(y));
    }

    __global__ void InitBlobLabelsKernel(int count, int* labels)
    {
        int idx = blockIdx.x * blockDim.x + threadIdx.x;
        if (idx >= count)
        {
            return;
        }

        labels[idx] = idx;
    }

    __global__ void PropagateBlobLabelsKernel(
        const int* inputLabels,
        int* outputLabels,
        const int* minX,
        const int* minY,
        const int* maxX,
        const int* maxY,
        int count,
        int mergeDistanceSquared,
        int* changed)
    {
        int i = blockIdx.x * blockDim.x + threadIdx.x;
        if (i >= count)
        {
            return;
        }

        int label = inputLabels[i];
        int minLabel = label;

        int aMinX = minX[i];
        int aMinY = minY[i];
        int aMaxX = maxX[i];
        int aMaxY = maxY[i];

        for (int j = 0; j < count; j++)
        {
            if (i == j)
            {
                continue;
            }

            int dx = 0;
            if (aMaxX < minX[j])
            {
                dx = minX[j] - aMaxX;
            }
            else if (maxX[j] < aMinX)
            {
                dx = aMinX - maxX[j];
            }

            int dy = 0;
            if (aMaxY < minY[j])
            {
                dy = minY[j] - aMaxY;
            }
            else if (maxY[j] < aMinY)
            {
                dy = aMinY - maxY[j];
            }

            int distSquared = dx * dx + dy * dy;
            if (distSquared <= mergeDistanceSquared)
            {
                int neighborLabel = inputLabels[j];
                if (neighborLabel < minLabel)
                {
                    minLabel = neighborLabel;
                }
            }
        }

        outputLabels[i] = minLabel;
        if (minLabel != label)
        {
            atomicExch(changed, 1);
        }
    }

    cudaError_t MergeCloseBlobsGpu(const std::vector<Blob>& blobs, int mergeDistance, std::vector<int>& labelsOut)
    {
        const int count = static_cast<int>(blobs.size());
        labelsOut.assign(count, -1);
        if (count <= 1 || mergeDistance <= 0)
        {
            for (int i = 0; i < count; i++)
            {
                labelsOut[i] = i;
            }
            return cudaSuccess;
        }

        std::vector<int> hostMinX(count);
        std::vector<int> hostMinY(count);
        std::vector<int> hostMaxX(count);
        std::vector<int> hostMaxY(count);
        for (int i = 0; i < count; i++)
        {
            hostMinX[i] = blobs[i].MinX;
            hostMinY[i] = blobs[i].MinY;
            hostMaxX[i] = blobs[i].MaxX;
            hostMaxY[i] = blobs[i].MaxY;
        }

        int* d_minX = nullptr;
        int* d_minY = nullptr;
        int* d_maxX = nullptr;
        int* d_maxY = nullptr;
        int* d_labelsA = nullptr;
        int* d_labelsB = nullptr;
        int* d_changed = nullptr;

        cudaError_t status = cudaSuccess;
        status = cudaMalloc(&d_minX, sizeof(int) * count);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMalloc(&d_minY, sizeof(int) * count);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMalloc(&d_maxX, sizeof(int) * count);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMalloc(&d_maxY, sizeof(int) * count);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMalloc(&d_labelsA, sizeof(int) * count);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMalloc(&d_labelsB, sizeof(int) * count);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMalloc(&d_changed, sizeof(int));
        if (status != cudaSuccess) goto Cleanup;

        status = cudaMemcpy(d_minX, hostMinX.data(), sizeof(int) * count, cudaMemcpyHostToDevice);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMemcpy(d_minY, hostMinY.data(), sizeof(int) * count, cudaMemcpyHostToDevice);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMemcpy(d_maxX, hostMaxX.data(), sizeof(int) * count, cudaMemcpyHostToDevice);
        if (status != cudaSuccess) goto Cleanup;
        status = cudaMemcpy(d_maxY, hostMaxY.data(), sizeof(int) * count, cudaMemcpyHostToDevice);
        if (status != cudaSuccess) goto Cleanup;

        int threads = 256;
        int blocks = (count + threads - 1) / threads;
        InitBlobLabelsKernel<<<blocks, threads>>>(count, d_labelsA);
        status = cudaDeviceSynchronize();
        if (status != cudaSuccess) goto Cleanup;

        const int mergeDistanceSquared = mergeDistance * mergeDistance;
        int changed = 1;
        int iterations = 0;
        const int maxIterations = count;
        while (changed && iterations < maxIterations)
        {
            changed = 0;
            status = cudaMemcpy(d_changed, &changed, sizeof(int), cudaMemcpyHostToDevice);
            if (status != cudaSuccess) goto Cleanup;

            PropagateBlobLabelsKernel<<<blocks, threads>>>(
                d_labelsA,
                d_labelsB,
                d_minX,
                d_minY,
                d_maxX,
                d_maxY,
                count,
                mergeDistanceSquared,
                d_changed);
            status = cudaDeviceSynchronize();
            if (status != cudaSuccess) goto Cleanup;

            status = cudaMemcpy(&changed, d_changed, sizeof(int), cudaMemcpyDeviceToHost);
            if (status != cudaSuccess) goto Cleanup;

            std::swap(d_labelsA, d_labelsB);
            iterations++;
        }

        status = cudaMemcpy(labelsOut.data(), d_labelsA, sizeof(int) * count, cudaMemcpyDeviceToHost);
        if (status != cudaSuccess) goto Cleanup;

    Cleanup:
        cudaFree(d_minX);
        cudaFree(d_minY);
        cudaFree(d_maxX);
        cudaFree(d_maxY);
        cudaFree(d_labelsA);
        cudaFree(d_labelsB);
        cudaFree(d_changed);

        return status;
    }

#endif

    std::vector<Blob> MergeCloseBlobsCpu(const std::vector<Blob>& blobs, int distance)
    {
        if (blobs.size() <= 1)
        {
            return blobs;
        }

        UnionFind unionFind(static_cast<int>(blobs.size()));
        for (size_t i = 0; i < blobs.size(); i++)
        {
            for (size_t j = i + 1; j < blobs.size(); j++)
            {
                if (RectDistance(blobs[i], blobs[j]) <= distance)
                {
                    unionFind.Union(static_cast<int>(i), static_cast<int>(j));
                }
            }
        }

        std::vector<BlobAccumulator> accumulators;
        std::vector<int> rootIndex(blobs.size(), -1);

        for (size_t i = 0; i < blobs.size(); i++)
        {
            int root = unionFind.Find(static_cast<int>(i));
            int index = rootIndex[root];
            if (index < 0)
            {
                rootIndex[root] = static_cast<int>(accumulators.size());
                accumulators.emplace_back(blobs[i]);
            }
            else
            {
                accumulators[static_cast<size_t>(index)].Add(blobs[i]);
            }
        }

        std::vector<Blob> result;
        result.reserve(accumulators.size());
        for (const auto& acc : accumulators)
        {
            result.push_back(acc.ToBlob());
        }

        return result;
    }
}

#if ENABLE_CUDA
extern "C" __declspec(dllexport) int CudaTopHatThreshold(
    const unsigned char* bgr,
    int width,
    int height,
    int stride,
    int radius,
    int threshold,
    unsigned char* outputMask)
{

    if (!bgr || !outputMask || width <= 0 || height <= 0 || stride <= 0)
    {
        return -1;
    }

    radius = radius < 1 ? 1 : radius;
    threshold = threshold < 0 ? 0 : (threshold > 255 ? 255 : threshold);

    const size_t bgrBytes = static_cast<size_t>(stride) * height;
    const size_t grayBytes = static_cast<size_t>(width) * height;

    unsigned char* d_bgr = nullptr;
    unsigned char* d_gray = nullptr;
    unsigned char* d_temp = nullptr;
    unsigned char* d_closing = nullptr;
    unsigned char* d_tophat = nullptr;
    unsigned char* d_binary = nullptr;

    cudaError_t status = cudaSuccess;
    status = cudaMalloc(&d_bgr, bgrBytes);
    if (status != cudaSuccess) return static_cast<int>(status);
    status = cudaMalloc(&d_gray, grayBytes);
    if (status != cudaSuccess) goto Cleanup;
    status = cudaMalloc(&d_temp, grayBytes);
    if (status != cudaSuccess) goto Cleanup;
    status = cudaMalloc(&d_closing, grayBytes);
    if (status != cudaSuccess) goto Cleanup;
    status = cudaMalloc(&d_tophat, grayBytes);
    if (status != cudaSuccess) goto Cleanup;
    status = cudaMalloc(&d_binary, grayBytes);
    if (status != cudaSuccess) goto Cleanup;

    status = cudaMemcpy(d_bgr, bgr, bgrBytes, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Cleanup;

    dim3 block(16, 16);
    dim3 grid((width + block.x - 1) / block.x, (height + block.y - 1) / block.y);
    BgrToGrayKernel<<<grid, block>>>(d_bgr, width, height, stride, d_gray);
    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Cleanup;

    DilateKernel<<<grid, block>>>(d_gray, width, height, radius, d_temp);
    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Cleanup;

    ErodeKernel<<<grid, block>>>(d_temp, width, height, radius, d_closing);
    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Cleanup;

    int length = width * height;
    int threads = 256;
    int blocks = (length + threads - 1) / threads;
    TopHatKernel<<<blocks, threads>>>(d_closing, d_gray, length, d_tophat);
    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Cleanup;

    ThresholdKernel<<<blocks, threads>>>(d_tophat, length, threshold, d_binary);
    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Cleanup;

    status = cudaMemcpy(outputMask, d_binary, grayBytes, cudaMemcpyDeviceToHost);

Cleanup:
    cudaFree(d_bgr);
    cudaFree(d_gray);
    cudaFree(d_temp);
    cudaFree(d_closing);
    cudaFree(d_tophat);
    cudaFree(d_binary);

    return static_cast<int>(status);
}

extern "C" __declspec(dllexport) int CudaFullPipeline(
    const unsigned char* bgr,
    int width,
    int height,
    int stride,
    int radius,
    int threshold,
    int minSize,
    int maxSize,
    int mergeDistance,
    BlobInfo* results,
    int maxResults,
    int* resultCount)
{
    if (!bgr || !results || !resultCount || width <= 0 || height <= 0 || stride <= 0 || maxResults <= 0)
    {
        return -1;
    }

    *resultCount = 0;
    minSize = std::max(1, minSize);
    maxSize = std::max(minSize, maxSize);
    mergeDistance = std::max(0, mergeDistance);

    const int length = width * height;
    const size_t grayBytes = static_cast<size_t>(length);

    std::vector<unsigned char> binary(static_cast<size_t>(length));
    std::vector<int> area(length);
    std::vector<int> minX(length);
    std::vector<int> minY(length);
    std::vector<int> maxX(length);
    std::vector<int> maxY(length);
    std::vector<unsigned long long> sumX(length);
    std::vector<unsigned long long> sumY(length);
    std::vector<Blob> blobs;
    std::vector<int> blobLabels;
    std::vector<Blob> merged;

    int status = CudaTopHatThreshold(bgr, width, height, stride, radius, threshold, binary.data());
    if (status != 0)
    {
        return status;
    }

    int* d_labelsA = nullptr;
    int* d_labelsB = nullptr;
    int* d_changed = nullptr;
    int* d_area = nullptr;
    int* d_minX = nullptr;
    int* d_minY = nullptr;
    int* d_maxX = nullptr;
    int* d_maxY = nullptr;
    unsigned long long* d_sumX = nullptr;
    unsigned long long* d_sumY = nullptr;

    cudaError_t cudaStatus = cudaSuccess;
    cudaStatus = cudaMalloc(&d_labelsA, sizeof(int) * length);
    if (cudaStatus != cudaSuccess) return static_cast<int>(cudaStatus);
    cudaStatus = cudaMalloc(&d_labelsB, sizeof(int) * length);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMalloc(&d_changed, sizeof(int));
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMalloc(&d_area, sizeof(int) * length);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMalloc(&d_minX, sizeof(int) * length);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMalloc(&d_minY, sizeof(int) * length);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMalloc(&d_maxX, sizeof(int) * length);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMalloc(&d_maxY, sizeof(int) * length);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMalloc(&d_sumX, sizeof(unsigned long long) * length);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMalloc(&d_sumY, sizeof(unsigned long long) * length);
    if (cudaStatus != cudaSuccess) goto Cleanup;

    unsigned char* d_binary = nullptr;
    cudaStatus = cudaMalloc(&d_binary, grayBytes);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMemcpy(d_binary, binary.data(), grayBytes, cudaMemcpyHostToDevice);
    if (cudaStatus != cudaSuccess) goto Cleanup;

    dim3 block(16, 16);
    dim3 grid((width + block.x - 1) / block.x, (height + block.y - 1) / block.y);
    InitLabelsKernel<<<grid, block>>>(d_binary, width, height, d_labelsA);
    cudaStatus = cudaDeviceSynchronize();
    if (cudaStatus != cudaSuccess) goto Cleanup;

    int changed = 1;
    int iterations = 0;
    const int maxIterations = width + height;
    while (changed && iterations < maxIterations)
    {
        changed = 0;
        cudaStatus = cudaMemcpy(d_changed, &changed, sizeof(int), cudaMemcpyHostToDevice);
        if (cudaStatus != cudaSuccess) goto Cleanup;

        PropagateLabelsKernel<<<grid, block>>>(d_labelsA, d_labelsB, width, height, d_changed);
        cudaStatus = cudaDeviceSynchronize();
        if (cudaStatus != cudaSuccess) goto Cleanup;

        cudaStatus = cudaMemcpy(&changed, d_changed, sizeof(int), cudaMemcpyDeviceToHost);
        if (cudaStatus != cudaSuccess) goto Cleanup;

        std::swap(d_labelsA, d_labelsB);
        iterations++;
    }

    int threads = 256;
    int blocks = (length + threads - 1) / threads;
    InitStatsKernel<<<blocks, threads>>>(length, width, height, d_area, d_minX, d_minY, d_maxX, d_maxY, d_sumX, d_sumY);
    cudaStatus = cudaDeviceSynchronize();
    if (cudaStatus != cudaSuccess) goto Cleanup;

    AccumulateStatsKernel<<<grid, block>>>(d_labelsA, width, height, d_area, d_minX, d_minY, d_maxX, d_maxY, d_sumX, d_sumY);
    cudaStatus = cudaDeviceSynchronize();
    if (cudaStatus != cudaSuccess) goto Cleanup;

    cudaStatus = cudaMemcpy(area.data(), d_area, sizeof(int) * length, cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMemcpy(minX.data(), d_minX, sizeof(int) * length, cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMemcpy(minY.data(), d_minY, sizeof(int) * length, cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMemcpy(maxX.data(), d_maxX, sizeof(int) * length, cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMemcpy(maxY.data(), d_maxY, sizeof(int) * length, cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMemcpy(sumX.data(), d_sumX, sizeof(unsigned long long) * length, cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) goto Cleanup;
    cudaStatus = cudaMemcpy(sumY.data(), d_sumY, sizeof(unsigned long long) * length, cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) goto Cleanup;

    blobs.reserve(256);
    for (int i = 0; i < length; i++)
    {
        if (area[i] < minSize || area[i] > maxSize)
        {
            continue;
        }

        if (maxX[i] < 0 || maxY[i] < 0)
        {
            continue;
        }

        Blob blob{};
        blob.Area = area[i];
        blob.MinX = minX[i];
        blob.MinY = minY[i];
        blob.MaxX = maxX[i];
        blob.MaxY = maxY[i];
        blob.CenterX = static_cast<double>(sumX[i]) / static_cast<double>(area[i]);
        blob.CenterY = static_cast<double>(sumY[i]) / static_cast<double>(area[i]);
        blobs.push_back(blob);
    }

    cudaError_t mergeStatus = MergeCloseBlobsGpu(blobs, mergeDistance, blobLabels);

    if (mergeStatus == cudaSuccess && blobLabels.size() == blobs.size())
    {
        std::vector<int> labelIndex(blobs.size(), -1);
        std::vector<BlobAccumulator> accumulators;
        for (size_t i = 0; i < blobs.size(); i++)
        {
            int root = blobLabels[i];
            if (root < 0 || root >= static_cast<int>(blobs.size()))
            {
                continue;
            }

            int index = labelIndex[static_cast<size_t>(root)];
            if (index < 0)
            {
                labelIndex[static_cast<size_t>(root)] = static_cast<int>(accumulators.size());
                accumulators.emplace_back(blobs[i]);
            }
            else
            {
                accumulators[static_cast<size_t>(index)].Add(blobs[i]);
            }
        }

        merged.reserve(accumulators.size());
        for (const auto& acc : accumulators)
        {
            merged.push_back(acc.ToBlob());
        }
    }
    else
    {
        merged = MergeCloseBlobsCpu(blobs, mergeDistance);
    }

    int count = static_cast<int>(merged.size());
    int writeCount = std::min(count, maxResults);
    for (int i = 0; i < writeCount; i++)
    {
        const auto& blob = merged[static_cast<size_t>(i)];
        results[i].Area = blob.Area;
        results[i].MinX = blob.MinX;
        results[i].MinY = blob.MinY;
        results[i].MaxX = blob.MaxX;
        results[i].MaxY = blob.MaxY;
        results[i].CenterX = static_cast<float>(blob.CenterX);
        results[i].CenterY = static_cast<float>(blob.CenterY);
    }

    *resultCount = writeCount;

Cleanup:
    cudaFree(d_labelsA);
    cudaFree(d_labelsB);
    cudaFree(d_changed);
    cudaFree(d_area);
    cudaFree(d_minX);
    cudaFree(d_minY);
    cudaFree(d_maxX);
    cudaFree(d_maxY);
    cudaFree(d_sumX);
    cudaFree(d_sumY);
    cudaFree(d_binary);

    return static_cast<int>(cudaStatus);
}

#endif
