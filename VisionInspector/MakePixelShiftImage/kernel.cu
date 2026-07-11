#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include <stdio.h>
#include <stdint.h>
#include <math.h>
#include <vector> // <vector> ��� �߰�
#include <thrust/device_vector.h>
#include <thrust/host_vector.h>
#include <thrust/scan.h>
#include <thrust/sort.h>
#include <thrust/reduce.h>
#include <thrust/unique.h>
#include <thrust/sequence.h>
#include <thrust/iterator/zip_iterator.h>
#include <thrust/iterator/constant_iterator.h>
#include <thrust/tuple.h>

__host__ __device__ inline int IMin(int a, int b)
{
    return a < b ? a : b;
}

__host__ __device__ inline int IMax(int a, int b)
{
    return a > b ? a : b;
}

struct BlobInfo {
    int minX;
    int maxX;
    int minY;
    int maxY;
    int pointCount;
};

// Union-Find: Find ���� (��� ���� ����)
__device__ int find_set(int* parent, int i) {
    if (parent[i] == i)
        return i;
    // ��� ����
    return parent[i] = find_set(parent, parent[i]);
}

// Union-Find: Union ����
__device__ void unite_sets(int* parent, int a, int b) {
    a = find_set(parent, a);
    b = find_set(parent, b);
    if (a != b) {
        // �� ���� ���̺��� �θ�� ����
        if (a < b)
            parent[b] = a;
        else
            parent[a] = b;
    }
}

// 1�ܰ�: �ʱ� ���̺��� �� �̿��� Union
__global__ void ccl_union_neighbors_kernel(const uint8_t* image, int* parent, int width, int height) {
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;

    if (x >= width || y >= height) return;

    int idx = y * width + x;
    if (image[idx] == 0) {
        parent[idx] = 0; // ���
        return;
    }

    parent[idx] = idx + 1; // �ʱ� ���̺� (0�� ����̹Ƿ� +1)

    // ������ �̿� Ȯ��
    if (x + 1 < width) {
        int right_idx = y * width + (x + 1);
        if (image[right_idx] != 0) {
            unite_sets(parent, idx + 1, right_idx + 1);
        }
    }

    // �Ʒ��� �̿� Ȯ��
    if (y + 1 < height) {
        int down_idx = (y + 1) * width + x;
        if (image[down_idx] != 0) {
            unite_sets(parent, idx + 1, down_idx + 1);
        }
    }
}

// 2�ܰ�: ���̺� ���� (������ ������ �ݺ�) - size ���� �߰�
__global__ void ccl_propagate_labels_kernel(int* parent, int size) {
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx < size && parent[idx] != 0) { // ��� �˻� �߰�
        find_set(parent, parent[idx]);
    }
}

// 3�ܰ�: ���� ���̺� �Ҵ�
__global__ void ccl_final_labeling_kernel(const uint8_t* image, int* parent, int* labels, int width, int height) {
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;

    if (x >= width || y >= height) return;

    int idx = y * width + x;
    if (image[idx] != 0) {
        labels[idx] = find_set(parent, idx + 1);
    }
    else {
        labels[idx] = 0;
    }
}

// �Ӱ谪 ���� Ŀ��
__global__ void threshold_kernel(const uint8_t* input, uint8_t* output, int size, uint8_t threshold) {
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx < size) {
        output[idx] = (input[idx] >= threshold) ? 1 : 0;
    }
}

// C#���� ȣ���� ���� �Լ�
extern "C" __declspec(dllexport) int FindBlobsWithCuda(
    const uint8_t* h_inputImage,
    int width,
    int height,
    uint8_t threshold,
    int minDefectSize,
    BlobInfo** h_blobInfos,
    int* blobCount)
{
    int imageSize = width * height;
    dim3 threadsPerBlock(32, 32);
    dim3 numBlocks((width + threadsPerBlock.x - 1) / threadsPerBlock.x, (height + threadsPerBlock.y - 1) / threadsPerBlock.y);
    dim3 numBlocks1D((imageSize + 1023) / 1024, 1);

    // 1. GPU �޸� �Ҵ� �� ������ ����
    uint8_t* d_inputImage, * d_binaryImage;
    cudaMalloc(&d_inputImage, imageSize);
    cudaMemcpy(d_inputImage, h_inputImage, imageSize, cudaMemcpyHostToDevice);
    cudaMalloc(&d_binaryImage, imageSize);

    // 2. �Ӱ谪 ����
    threshold_kernel<<<numBlocks1D, 1024>>>(d_inputImage, d_binaryImage, imageSize, threshold);

    // 3. CCL (Union-Find)
    int* d_parent;
    cudaMalloc(&d_parent, (imageSize + 1) * sizeof(int)); // ���̺��� 1���� �����ϹǷ� +1

    ccl_union_neighbors_kernel<<<numBlocks, threadsPerBlock>>>(d_binaryImage, d_parent, width, height);

    // ���̺��� ����ȭ�� ������ �ݺ� (���� log(N) Ƚ���� ���)
    for (int i = 0; i < 15; ++i) {
        // ������ Ŀ�� ȣ��: size ���� ����
        ccl_propagate_labels_kernel<<<numBlocks1D, 1024>>>(d_parent, imageSize + 1);
    }

    int* d_finalLabels;
    cudaMalloc(&d_finalLabels, imageSize * sizeof(int));
    ccl_final_labeling_kernel<<<numBlocks, threadsPerBlock>>>(d_binaryImage, d_parent, d_finalLabels, width, height);

    // 4. Thrust�� ����Ͽ� �� blobs ���� ����
    thrust::device_vector<int> d_keys(d_finalLabels, d_finalLabels + imageSize);
    thrust::device_vector<int> d_values(imageSize);
    thrust::sequence(d_values.begin(), d_values.end()); // 0, 1, 2, ...

    // ���̺�(key)�� �������� ����
    thrust::sort_by_key(d_keys.begin(), d_keys.end(), d_values.begin());

    // ������ ���̺� �� ã�� (���� ��)
    thrust::device_vector<int> d_unique_keys(d_keys.size());
    thrust::device_vector<int> d_counts(d_keys.size());

    auto end_iter = thrust::reduce_by_key(
        d_keys.begin(), d_keys.end(),
        thrust::make_constant_iterator(1),
        d_unique_keys.begin(),
        d_counts.begin()
    );

    int num_blobs = thrust::distance(d_unique_keys.begin(), end_iter.first);
    if (num_blobs <= 1) { // ���(0)�� �ְų� ������ ���� ���
        *blobCount = 0;
        *h_blobInfos = nullptr;
        cudaFree(d_inputImage); cudaFree(d_binaryImage); cudaFree(d_parent); cudaFree(d_finalLabels);
        return 0;
    }

    // ���(���̺� 0) ����
    thrust::host_vector<int> h_unique_keys = d_unique_keys;
    thrust::host_vector<int> h_counts = d_counts;

    std::vector<BlobInfo> blob_results;
    for (int i = 1; i < num_blobs; ++i) { // 0�� ���̺�(���)�� �ǳʶ�
        if (h_counts[i] >= minDefectSize) {
            BlobInfo info;
            info.pointCount = h_counts[i];
            blob_results.push_back(info);
        }
    }

    *blobCount = blob_results.size();
    if (*blobCount > 0) {
        *h_blobInfos = (BlobInfo*)malloc(sizeof(BlobInfo) * (*blobCount));
        memcpy(*h_blobInfos, blob_results.data(), sizeof(BlobInfo) * (*blobCount));
    }
    else {
        *h_blobInfos = nullptr;
    }

    // GPU �޸� ����
    cudaFree(d_inputImage);
    cudaFree(d_binaryImage);
    cudaFree(d_parent);
    cudaFree(d_finalLabels);

    return 0; // ����
}

// C#���� �Ҵ�� �޸𸮸� �����ϱ� ���� �Լ�
extern "C" __declspec(dllexport) void FreeCudaHostMemory(void* ptr) {
    if (ptr != nullptr) {
        free(ptr);
    }
}
extern "C" __declspec(dllexport)
void UpscaleROI2xBilinear_Release()
{
}

__global__ void SobelKernel(const uint8_t* input, uint8_t* output, int width, int height)
{
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;

    if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1) {
        if (x < width && y < height)
            output[y * width + x] = 0;
        return;
    }

    int gx =
        -input[(y - 1) * width + (x - 1)] - 2 * input[y * width + (x - 1)] - input[(y + 1) * width + (x - 1)]
        + input[(y - 1) * width + (x + 1)] + 2 * input[y * width + (x + 1)] + input[(y + 1) * width + (x + 1)];

    int gy =
        -input[(y - 1) * width + (x - 1)] - 2 * input[(y - 1) * width + x] - input[(y - 1) * width + (x + 1)]
        + input[(y + 1) * width + (x - 1)] + 2 * input[(y + 1) * width + x] + input[(y + 1) * width + (x + 1)];

    int mag = abs(gx) + abs(gy); // ���� �ٻ� (sqrt ���)
    mag = IMin(mag, 255);
    output[y * width + x] = (uint8_t)mag;
}
extern "C" __declspec(dllexport)
cudaError_t ApplySobelFilter(
    const uint8_t* h_inputImage,
    uint8_t* h_outputImage,
    int width,
    int height)
{
    uint8_t* d_input = nullptr;
    uint8_t* d_output = nullptr;
    size_t imageSize = (size_t)width * height * sizeof(uint8_t);
    cudaError_t status;

    // 1. �޸� �Ҵ�
    status = cudaMalloc(&d_input, imageSize);
    if (status != cudaSuccess) goto Error;
    
    status = cudaMalloc(&d_output, imageSize);
    if (status != cudaSuccess) goto Error;

    // 2. ������ ����
    status = cudaMemcpy(d_input, h_inputImage, imageSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;

    // 3. Ŀ�� ����
    {
        dim3 block(32, 32);
        dim3 grid((width + block.x - 1) / block.x, (height + block.y - 1) / block.y);
        SobelKernel<<<grid, block>>>(d_input, d_output, width, height);
    }

    status = cudaGetLastError();
    if (status != cudaSuccess) goto Error;

    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Error;

    // 4. ��� ����
    status = cudaMemcpy(h_outputImage, d_output, imageSize, cudaMemcpyDeviceToHost);

Error:
    if (d_input) cudaFree(d_input);
    if (d_output) cudaFree(d_output);
    return status;
}

__global__ void Upscale2xBilinearKernel(
    const uint8_t* input, int inWidth, int inHeight,
    uint8_t* output, int outWidth, int outHeight)
{
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;

    if (x >= outWidth || y >= outHeight) return;

    float srcX = (x + 0.5f) * 0.5f - 0.5f;
    float srcY = (y + 0.5f) * 0.5f - 0.5f;

    int x1 = floorf(srcX); 
    int y1 = floorf(srcY);
    int x2 = x1 + 1;
    int y2 = y1 + 1;

    float dx = srcX - x1;
    float dy = srcY - y1;

    x1 = max(0, min(x1, inWidth - 1));
    y1 = max(0, min(y1, inHeight - 1));
    x2 = max(0, min(x2, inWidth - 1));
    y2 = max(0, min(y2, inHeight - 1));

    uint8_t q11 = input[y1 * inWidth + x1];
    uint8_t q21 = input[y1 * inWidth + x2];
    uint8_t q12 = input[y2 * inWidth + x1];
    uint8_t q22 = input[y2 * inWidth + x2];

    float val = (1.0f - dx) * (1.0f - dy) * q11 + 
                dx * (1.0f - dy) * q21 +
                (1.0f - dx) * dy * q12 + 
                dx * dy * q22;
                
    output[y * outWidth + x] = (uint8_t)(val + 0.5f);
}

__global__ void UpscaleROI2xBilinearKernel(
    const uint8_t* input, int inWidth, int inHeight,
    int roiX, int roiY, int roiWidth, int roiHeight,
    uint8_t* output, int outWidth, int outHeight)
{
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;

    if (x >= outWidth || y >= outHeight) return;

    float srcX = roiX + (x + 0.5f) * 0.5f - 0.5f;
    float srcY = roiY + (y + 0.5f) * 0.5f - 0.5f;

    int x1 = floorf(srcX);
    int y1 = floorf(srcY);
    int x2 = x1 + 1;
    int y2 = y1 + 1;

    float dx = srcX - x1;
    float dy = srcY - y1;

    x1 = max(0, min(x1, inWidth - 1));
    y1 = max(0, min(y1, inHeight - 1));
    x2 = max(0, min(x2, inWidth - 1));
    y2 = max(0, min(y2, inHeight - 1));

    uint8_t q11 = input[y1 * inWidth + x1];
    uint8_t q21 = input[y1 * inWidth + x2];
    uint8_t q12 = input[y2 * inWidth + x1];
    uint8_t q22 = input[y2 * inWidth + x2];

    float val = (1.0f - dx) * (1.0f - dy) * q11 + 
                dx * (1.0f - dy) * q21 +
                (1.0f - dx) * dy * q12 + 
                dx * dy * q22;

    output[y * outWidth + x] = (uint8_t)(val + 0.5f);
}

extern "C" __declspec(dllexport)
cudaError_t Upscale2xBilinear(
    const uint8_t* input, int inWidth, int inHeight,
    uint8_t* output)
{
    int outWidth = inWidth * 2;
    int outHeight = inHeight * 2;
    size_t inSize = inWidth * inHeight * sizeof(uint8_t);
    size_t outSize = outWidth * outHeight * sizeof(uint8_t);

    uint8_t* d_input = nullptr;
    uint8_t* d_output = nullptr;
    cudaError_t status;

    // 1. �޸� �Ҵ�
    status = cudaMalloc(&d_input, inSize);
    if (status != cudaSuccess) goto Error;

    status = cudaMalloc(&d_output, outSize);
    if (status != cudaSuccess) goto Error;

    // 2. ������ ����
    status = cudaMemcpy(d_input, input, inSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;

    // 3. Ŀ�� ����
    {
        dim3 block(32, 32);
        dim3 grid((outWidth + block.x - 1) / block.x, (outHeight + block.y - 1) / block.y);
        Upscale2xBilinearKernel<<<grid, block>>>(d_input, inWidth, inHeight, d_output, outWidth, outHeight);
    }

    status = cudaGetLastError();
    if (status != cudaSuccess) goto Error;

    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Error;

    // 4. ��� ����
    status = cudaMemcpy(output, d_output, outSize, cudaMemcpyDeviceToHost);

Error:
    if (d_input) cudaFree(d_input);
    if (d_output) cudaFree(d_output);
    return status;
}

extern "C" __declspec(dllexport)
cudaError_t UpscaleROI2xBilinear(
    const uint8_t* input, int inWidth, int inHeight,
    int roiX, int roiY, int roiWidth, int roiHeight,
    uint8_t* output)
{
    size_t inSize = (size_t)inWidth * inHeight * sizeof(uint8_t);
    int outWidth = roiWidth * 2;
    int outHeight = roiHeight * 2;
    size_t outSize = (size_t)outWidth * outHeight * sizeof(uint8_t);

    uint8_t* d_input = nullptr;
    uint8_t* d_output = nullptr;
    cudaError_t status;

    // 1. �޸� �Ҵ�
    status = cudaMalloc(&d_input, inSize);
    if (status != cudaSuccess) goto Error;

    status = cudaMalloc(&d_output, outSize);
    if (status != cudaSuccess) goto Error;

    // 2. ������ ����
    status = cudaMemcpy(d_input, input, inSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;

    // 3. Ŀ�� ����
    {
        dim3 blockDim(16, 16);
        dim3 gridDim((outWidth + blockDim.x - 1) / blockDim.x, (outHeight + blockDim.y - 1) / blockDim.y);
        UpscaleROI2xBilinearKernel<<<gridDim, blockDim>>>(
            d_input, inWidth, inHeight,
            roiX, roiY, roiWidth, roiHeight,
            d_output, outWidth, outHeight);
    }

    status = cudaGetLastError();
    if (status != cudaSuccess) goto Error;

    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Error;

    // 4. ��� ����
    status = cudaMemcpy(output, d_output, outSize, cudaMemcpyDeviceToHost);

Error:
    if (d_input) cudaFree(d_input);
    if (d_output) cudaFree(d_output);
    return status;
}

extern "C" __declspec(dllexport)
cudaError_t UpscaleROI2xBilinearAndSobel(
    const uint8_t* input, int inWidth, int inHeight,
    int roiX, int roiY, int roiWidth, int roiHeight,
    uint8_t* output, uint8_t* outputSobel)
{
    size_t inSize = (size_t)inWidth * inHeight * sizeof(uint8_t);
    int outWidth = roiWidth * 2;
    int outHeight = roiHeight * 2;
    size_t outSize = (size_t)outWidth * outHeight * sizeof(uint8_t);

    uint8_t* d_input = nullptr;
    uint8_t* d_output = nullptr;
    uint8_t* d_outputSobel = nullptr;
    cudaError_t status;

    // 1. �޸� �Ҵ�
    status = cudaMalloc(&d_input, inSize);
    if (status != cudaSuccess) goto Error;

    status = cudaMalloc(&d_output, outSize);
    if (status != cudaSuccess) goto Error;

    status = cudaMalloc(&d_outputSobel, outSize);
    if (status != cudaSuccess) goto Error;

    // 2. ������ ����
    status = cudaMemcpy(d_input, input, inSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;


    // 3. Upscale Ŀ�� ����
    {
        dim3 blockDim(16, 16);
        dim3 gridDim((outWidth + blockDim.x - 1) / blockDim.x, (outHeight + blockDim.y - 1) / blockDim.y);
        UpscaleROI2xBilinearKernel<<<gridDim, blockDim>>>(
            d_input, inWidth, inHeight,
            roiX, roiY, roiWidth, roiHeight,
            d_output, outWidth, outHeight);
    }

    // 4. Sobel Ŀ�� ����
    {
        dim3 sobelBlock(16, 16);
        dim3 sobelGrid((outWidth + sobelBlock.x - 1) / sobelBlock.x, (outHeight + sobelBlock.y - 1) / sobelBlock.y);
        SobelKernel<<<sobelGrid, sobelBlock>>>(
            d_output, d_outputSobel, outWidth, outHeight);
    }

    status = cudaGetLastError();
    if (status != cudaSuccess) goto Error;

    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Error;

    // 5. ��� ����
    status = cudaMemcpy(output, d_output, outSize, cudaMemcpyDeviceToHost);
    if (status != cudaSuccess) goto Error;
    status = cudaMemcpy(outputSobel, d_outputSobel, outSize, cudaMemcpyDeviceToHost);
    if (status != cudaSuccess) goto Error;

Error:
    if (d_input) cudaFree(d_input);
    if (d_output) cudaFree(d_output);
    if (d_outputSobel) cudaFree(d_outputSobel);
    return status;
}

extern "C" __declspec(dllexport)
cudaError_t UpscaleROI2xBilinear_Init(int inWidth, int inHeight, int roiWidth, int roiHeight)
{
    return cudaSuccess;
}

// C-style struct for line parameters (y = slope * x + intercept)
struct LineParams {
    float slope;
    float intercept;
};

__device__ float getY(LineParams line, float x) {
    return line.slope * x + line.intercept;
}

__device__ float getX(LineParams line, float y) {
    if (fabsf(line.slope) < 1e-6f) {
        return -1.0f;
    }
    if (line.slope > 999999)
    {
        return line.intercept;
    }
    return (y - line.intercept) / line.slope;
}

// -------------------------------------------------------------------------
// Optimized Pipeline with Transpose and Shared Memory
// -------------------------------------------------------------------------

// Tiled Transpose Kernel
__global__ void TransposeKernel(const uint8_t* __restrict__ idata, uint8_t* __restrict__ odata, int width, int height)
{
    __shared__ uint8_t tile[32][33];

    int x = blockIdx.x * 32 + threadIdx.x;
    int y = blockIdx.y * 32 + threadIdx.y;
    int width_in = width;

    if (x < width && y < height)
    {
        tile[threadIdx.y][threadIdx.x] = idata[y * width_in + x];
    }

    __syncthreads();

    x = blockIdx.y * 32 + threadIdx.x;
    y = blockIdx.x * 32 + threadIdx.y;
    int width_out = height;

    if (x < height && y < width)
    {
        odata[y * width_out + x] = tile[threadIdx.x][threadIdx.y];
    }
}

// Row Kernel: Uses Shared Memory
extern __shared__ uint8_t s_row_data[];

__global__ void DilateRowKernel_Shared(const uint8_t* __restrict__ input, int width, int height, int radius, uint8_t* __restrict__ output)
{
    int tid = threadIdx.x;
    int gx = blockIdx.x * blockDim.x + tid;
    int gy = blockIdx.y;

    if (gy >= height) return;

    int sm_width = blockDim.x + 2 * radius;
    int row_offset = gy * width;
    int global_start_x = blockIdx.x * blockDim.x - radius;

    for (int i = tid; i < sm_width; i += blockDim.x)
    {
        int src_x = global_start_x + i;
        uint8_t val = 0;
        if (src_x >= 0 && src_x < width)
        {
            val = input[row_offset + src_x];
        }
        s_row_data[i] = val;
    }

    __syncthreads();

    if (gx < width)
    {
        uint8_t maxVal = 0;
        for (int i = 0; i <= 2 * radius; ++i)
        {
            uint8_t val = s_row_data[tid + i];
            if (val > maxVal) maxVal = val;
        }
        output[row_offset + gx] = maxVal;
    }
}

__global__ void ErodeRowKernel_Shared(const uint8_t* __restrict__ input, int width, int height, int radius, uint8_t* __restrict__ output)
{
    int tid = threadIdx.x;
    int gx = blockIdx.x * blockDim.x + tid;
    int gy = blockIdx.y;

    if (gy >= height) return;

    int sm_width = blockDim.x + 2 * radius;
    int row_offset = gy * width;
    int global_start_x = blockIdx.x * blockDim.x - radius;

    for (int i = tid; i < sm_width; i += blockDim.x)
    {
        int src_x = global_start_x + i;
        uint8_t val = 255;
        if (src_x >= 0 && src_x < width)
        {
            val = input[row_offset + src_x];
        }
        s_row_data[i] = val;
    }

    __syncthreads();

    if (gx < width)
    {
        uint8_t minVal = 255;
        for (int i = 0; i <= 2 * radius; ++i)
        {
            uint8_t val = s_row_data[tid + i];
            if (val < minVal) minVal = val;
        }
        output[row_offset + gx] = minVal;
    }
}

// FUSED KERNEL: Row Erode + BlackTopHat + FindChipping
__global__ void Fused_ErodeRow_TopHat_Chipping_Kernel(
    const uint8_t* __restrict__ erode_col_src,
    const uint8_t* __restrict__ original_input,
    uint8_t* __restrict__ outputMask,
    uint8_t* __restrict__ outputMask2,
    int width, int height, int radius,
    LineParams lineTop, LineParams lineBottom, LineParams lineLeft, LineParams lineRight,
    uint8_t threshold, int margin, uint8_t topHatThreshold)
{
    int tid = threadIdx.x;
    int gx = blockIdx.x * blockDim.x + tid;
    int gy = blockIdx.y;

    if (gy >= height) return;

    int sm_width = blockDim.x + 2 * radius;
    int row_offset = gy * width;
    int global_start_x = blockIdx.x * blockDim.x - radius;

    for (int i = tid; i < sm_width; i += blockDim.x)
    {
        int src_x = global_start_x + i;
        uint8_t val = 255;
        if (src_x >= 0 && src_x < width)
        {
            val = erode_col_src[row_offset + src_x];
        }
        s_row_data[i] = val;
    }

    __syncthreads();

    if (gx < width)
    {
        // 1. Erode Row Operation
        uint8_t closing_val = 255;
        for (int i = 0; i <= 2 * radius; ++i)
        {
            uint8_t val = s_row_data[tid + i];
            if (val < closing_val) closing_val = val;
        }

        // 2. Black Top Hat
        uint8_t input_val = original_input[row_offset + gx];
        int tophat_diff = (int)closing_val - (int)input_val;
        uint8_t tophat_val = (tophat_diff > 0) ? (uint8_t)tophat_diff : 0;

        float leftY = getY(lineTop, gx);
        float topX = getX(lineLeft, gy);
        float bottomY = getY(lineBottom, gx);
        float RightX = getX(lineRight, gy);
        // 3. Find Chipping Logic
        bool isInside = (gy > (leftY + margin)) &&
                        (gy < (bottomY - margin)) &&
                        (gx > (topX + margin)) &&
                        (gx < (bottomY - margin));

        bool isInsideMast2 = (gy > (leftY )) &&
            (gy < (bottomY )) &&
            (gx > (topX )) &&
            (gx < (bottomY ));

        uint8_t pixelValue = 0;
        if (isInsideMast2) {
            if (input_val < threshold || input_val > 250) {
                pixelValue = 255;
            }
            else {
                if (tophat_val > topHatThreshold) {
                    pixelValue = 255;
                } else {
                    if (input_val > 250)
                    {
                        pixelValue = 255;
                    }
                    else
                    {
                        pixelValue = 0;
                    }
                }
            }
        }
        else {
            if (input_val < threshold) {
                pixelValue = 0;
            }
        }
        outputMask2[row_offset + gx] = pixelValue;
        if (isInside)
        {
            outputMask[row_offset + gx] = pixelValue;
        }
    }
}

// Helper to launch transpose
cudaError_t LaunchTranspose(const uint8_t* in, uint8_t* out, int width, int height)
{
    dim3 dimBlock(32, 32);
    dim3 dimGrid((width + 31) / 32, (height + 31) / 32);
    TransposeKernel<<<dimGrid, dimBlock>>>(in, out, width, height);
    return cudaGetLastError();
}

// �ܰ� ���� ����ŷ Ŀ��: ����+���� �ٱ� �ȼ��� fillValue�� ä��
// ���� �ȼ��� �������� ����
// Closing ���� �����ϸ�, �ܰ����� Closing��� ? fillValue �� TopHat ? 0
// �ܰ� ġ���� ��ų� ��ο� �ȼ��� Dilate�� ���� ���η� ħ���ϴ� ���� ����
__global__ void MaskOutsideRegionKernel(
    const uint8_t* __restrict__ original,
    uint8_t* __restrict__ image,
    int width, int height,
    LineParams lineTop, LineParams lineBottom,
    LineParams lineLeft, LineParams lineRight,
    int margin,
    uint8_t fillValue)
{
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;

    if (x >= width || y >= height) return;

    bool isInside = (y > (getY(lineTop, (float)x) + margin)) &&
                    (y < (getY(lineBottom, (float)x) - margin)) &&
                    (x > (getX(lineLeft, (float)y) + margin)) &&
                    (x < (getX(lineRight, (float)y) - margin));

    if (isInside) {
        image[y * width + x] = original[y * width + x];
    } else {
        image[y * width + x] = fillValue;
    }
}

// C#���� ȣ���� �� �ִ� ���� �Լ�
extern "C" __declspec(dllexport)
cudaError_t FindChipping(
    const uint8_t* h_inputImage, // Host�� �Է� �̹���
    uint8_t* h_outputMask,       // Host�� ��� ����ũ
    uint8_t* h_outputMask2,       // Host�� ��� ����ũ
    int width, int height,
    LineParams lineTop,
    LineParams lineBottom,
    LineParams lineLeft,
    LineParams lineRight,
    uint8_t threshold,
    int margin,
    int topHatRadius,
    uint8_t topHatThreshold)
{
    size_t requiredSize = (size_t)width * height * sizeof(uint8_t);
    cudaError_t status;
    
    // ���� ������ ����
    uint8_t* d_input = nullptr;
    uint8_t* d_output = nullptr;
    uint8_t* d_output2 = nullptr;
    uint8_t* d_temp = nullptr;
    uint8_t* d_closing = nullptr;

    // 1. �޸� �Ҵ�
    status = cudaMalloc(&d_input, requiredSize); if (status != cudaSuccess) goto Error;
    status = cudaMalloc(&d_output, requiredSize); if (status != cudaSuccess) goto Error;
    status = cudaMalloc(&d_output2, requiredSize); if (status != cudaSuccess) goto Error;
    status = cudaMalloc(&d_temp, requiredSize); if (status != cudaSuccess) goto Error;
    status = cudaMalloc(&d_closing, requiredSize); if (status != cudaSuccess) goto Error;

    // 2. �Է� ������ ����
    status = cudaMemcpy(d_input, h_inputImage, requiredSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;

    topHatRadius = topHatRadius < 1 ? 1 : topHatRadius;

    // Block configuration for Row kernels (Horizontal Strip)
    int blockX = 256;
    size_t sharedMemSize = (blockX + 2 * topHatRadius) * sizeof(uint8_t);

    // -------------------------------------------------------------
    // �ܰ� ����ŷ
    // -------------------------------------------------------------
    {
        dim3 maskBlock(32, 32);
        dim3 maskGrid((width + maskBlock.x - 1) / maskBlock.x, (height + maskBlock.y - 1) / maskBlock.y);
        MaskOutsideRegionKernel<<<maskGrid, maskBlock>>>(
            d_input, d_closing, // d_closing�� �ʱ� ���۷� ���
            width, height,
            lineTop, lineBottom, lineLeft, lineRight,
            margin,
            threshold);
    }

    // -------------------------------------------------------------
    // PIPELINE: Dilate2D -> Erode2D -> TopHat -> Logic
    // -------------------------------------------------------------

    // 1. Dilate Row: d_closing(W,H) -> d_temp(W,H)
    dim3 gridRow((width + blockX - 1) / blockX, height);
    DilateRowKernel_Shared<<<gridRow, blockX, sharedMemSize>>>(d_closing, width, height, topHatRadius, d_temp);

    // 2. Transpose: d_temp(W,H) -> d_closing(H,W)
    LaunchTranspose(d_temp, d_closing, width, height);

    // 3. Dilate Row (effectively Dilate Col): d_closing(H,W) -> d_temp(H,W)
    dim3 gridCol((height + blockX - 1) / blockX, width); 
    DilateRowKernel_Shared<<<gridCol, blockX, sharedMemSize>>>(d_closing, height, width, topHatRadius, d_temp);
    
    // Now d_temp contains [Dilate2D Transposed]
    
    // 4. Erode Row (effectively Erode Col): d_temp(H,W) -> d_closing(H,W)
    ErodeRowKernel_Shared<<<gridCol, blockX, sharedMemSize>>>(d_temp, height, width, topHatRadius, d_closing);

    // Now d_closing contains [ErodeCol(Dilate2D) Transposed]

    // 5. Transpose: d_closing(H,W) -> d_temp(W,H)
    LaunchTranspose(d_closing, d_temp, height, width);

    // Now d_temp contains [ErodeCol(Dilate2D)] which is half-way done Closing.

    // 6. Fused Erode Row + Logic
    Fused_ErodeRow_TopHat_Chipping_Kernel<<<gridRow, blockX, sharedMemSize>>>(
        d_temp,     // Source (ErodeCol result)
        d_input,    // Original input (����ŷ �ȵ� ����)
        d_output,
        d_output2,   // Final output (Output 1)
        width, height, topHatRadius,
        lineTop, lineBottom, lineLeft, lineRight, threshold, margin,
        topHatThreshold
    );
    //
    //// ������ ȣ��� Ŀ�ΰ� ���� ����� d_output2�� ���� (�ʿ��� ��� ����)
    //Fused_ErodeRow_TopHat_Chipping_Kernel << <gridRow, blockX, sharedMemSize >> > (
    //    d_temp,     // Source (ErodeCol result)
    //    d_input,    // Original input (����ŷ �ȵ� ����)
    //    d_output2,   // Final output (Output 2)
    //    width, height, topHatRadius,
    //    lineTop, lineBottom, lineLeft, lineRight, threshold, 0,
    //    topHatThreshold
    //    );

    status = cudaGetLastError();
    if (status != cudaSuccess) goto Error;

    status = cudaMemcpy(h_outputMask, d_output, requiredSize, cudaMemcpyDeviceToHost);
    if (status != cudaSuccess) goto Error;
    
    if (h_outputMask2 != nullptr) {
        status = cudaMemcpy(h_outputMask2, d_output2, requiredSize, cudaMemcpyDeviceToHost);
        if (status != cudaSuccess) goto Error;
    }

Error:
    if (d_input) cudaFree(d_input);
    if (d_output) cudaFree(d_output);
    if (d_output2) cudaFree(d_output2);
    if (d_temp) cudaFree(d_temp);
    if (d_closing) cudaFree(d_closing);
    return status;
}

// =========================================================================
// QmcCudaContext — 디바이스 버퍼 풀 컨텍스트 (2026-07-11)
//
// 기존 익스포트는 호출마다 cudaMalloc/cudaFree 를 반복한다(호출당 수 ms + 병렬 시 할당 락 경합).
// 컨텍스트는 함수별 역할 고정 슬롯의 디바이스 버퍼와 전용 스트림을 보유하고,
// "요청 크기가 이전과 같으면 그대로 재사용 / 다르면(ROI 변경 등) 그 슬롯만 재할당" 규칙으로 동작한다.
// C# 쪽 CudaContextPool 이 기본 8개를 만들어 빌려주고(검사 1건당 1개), 사용 후 반환한다.
// 기존(무-ctx) 익스포트는 그대로 유지 — 구버전 DLL/폴백 경로와 호환.
//
// 슬롯 배치(함수 간 크기 충돌로 인한 핑퐁 재할당 방지 — 함수별 전용 슬롯):
//   0: Upscale 입력(원본 전체)   1: Upscale 출력(2x)   2: Upscale Sobel 출력(2x)
//   3: FindChipping 입력          4: mask1              5: mask2
//   6: temp                       7: closing
// =========================================================================

#define QMC_CTX_BUF_COUNT 8

struct QmcCtxBuf { void* ptr; size_t bytes; };

struct QmcCudaContext {
    QmcCtxBuf bufs[QMC_CTX_BUF_COUNT];
    cudaStream_t stream;
};

// 슬롯 버퍼 확보 — 크기가 같으면 재사용(할당 0회), 다르면 해제 후 재할당.
static cudaError_t QmcEnsureBuf(QmcCudaContext* ctx, int slot, size_t need, void** outPtr)
{
    if (slot < 0 || slot >= QMC_CTX_BUF_COUNT || need == 0) return cudaErrorInvalidValue;
    QmcCtxBuf* b = &ctx->bufs[slot];
    if (b->ptr != nullptr && b->bytes == need) { *outPtr = b->ptr; return cudaSuccess; }
    if (b->ptr != nullptr) { cudaFree(b->ptr); b->ptr = nullptr; b->bytes = 0; }
    cudaError_t st = cudaMalloc(&b->ptr, need);
    if (st != cudaSuccess) { b->ptr = nullptr; return st; }
    b->bytes = need;
    *outPtr = b->ptr;
    return cudaSuccess;
}

extern "C" __declspec(dllexport)
int QmcCtxCreate(void** outCtx)
{
    if (outCtx == nullptr) return cudaErrorInvalidValue;
    *outCtx = nullptr;
    QmcCudaContext* ctx = new QmcCudaContext();
    for (int i = 0; i < QMC_CTX_BUF_COUNT; ++i) { ctx->bufs[i].ptr = nullptr; ctx->bufs[i].bytes = 0; }
    cudaError_t st = cudaStreamCreate(&ctx->stream);
    if (st != cudaSuccess) { delete ctx; return (int)st; }
    *outCtx = ctx;
    return (int)cudaSuccess;
}

extern "C" __declspec(dllexport)
void QmcCtxDestroy(void* ctxPtr)
{
    QmcCudaContext* ctx = (QmcCudaContext*)ctxPtr;
    if (ctx == nullptr) return;
    for (int i = 0; i < QMC_CTX_BUF_COUNT; ++i)
        if (ctx->bufs[i].ptr != nullptr) { cudaFree(ctx->bufs[i].ptr); ctx->bufs[i].ptr = nullptr; ctx->bufs[i].bytes = 0; }
    cudaStreamDestroy(ctx->stream);
    delete ctx;
}

// FindChipping 의 컨텍스트 버전 — 디바이스 할당/해제 없이 ctx 슬롯 재사용, ctx 전용 스트림에서 실행.
extern "C" __declspec(dllexport)
cudaError_t FindChippingCtx(
    void* ctxPtr,
    const uint8_t* h_inputImage,
    uint8_t* h_outputMask,
    uint8_t* h_outputMask2,
    int width, int height,
    LineParams lineTop,
    LineParams lineBottom,
    LineParams lineLeft,
    LineParams lineRight,
    uint8_t threshold,
    int margin,
    int topHatRadius,
    uint8_t topHatThreshold)
{
    QmcCudaContext* ctx = (QmcCudaContext*)ctxPtr;
    if (ctx == nullptr) return cudaErrorInvalidValue;

    size_t requiredSize = (size_t)width * height * sizeof(uint8_t);
    cudaError_t status;
    void* p;

    uint8_t* d_input;   status = QmcEnsureBuf(ctx, 3, requiredSize, &p); if (status != cudaSuccess) return status; d_input   = (uint8_t*)p;
    uint8_t* d_output;  status = QmcEnsureBuf(ctx, 4, requiredSize, &p); if (status != cudaSuccess) return status; d_output  = (uint8_t*)p;
    uint8_t* d_output2; status = QmcEnsureBuf(ctx, 5, requiredSize, &p); if (status != cudaSuccess) return status; d_output2 = (uint8_t*)p;
    uint8_t* d_temp;    status = QmcEnsureBuf(ctx, 6, requiredSize, &p); if (status != cudaSuccess) return status; d_temp    = (uint8_t*)p;
    uint8_t* d_closing; status = QmcEnsureBuf(ctx, 7, requiredSize, &p); if (status != cudaSuccess) return status; d_closing = (uint8_t*)p;

    cudaStream_t s = ctx->stream;
    status = cudaMemcpyAsync(d_input, h_inputImage, requiredSize, cudaMemcpyHostToDevice, s);
    if (status != cudaSuccess) return status;

    topHatRadius = topHatRadius < 1 ? 1 : topHatRadius;
    int blockX = 256;
    size_t sharedMemSize = (blockX + 2 * topHatRadius) * sizeof(uint8_t);

    {
        dim3 maskBlock(32, 32);
        dim3 maskGrid((width + maskBlock.x - 1) / maskBlock.x, (height + maskBlock.y - 1) / maskBlock.y);
        MaskOutsideRegionKernel<<<maskGrid, maskBlock, 0, s>>>(
            d_input, d_closing, width, height,
            lineTop, lineBottom, lineLeft, lineRight, margin, threshold);
    }

    dim3 gridRow((width + blockX - 1) / blockX, height);
    DilateRowKernel_Shared<<<gridRow, blockX, sharedMemSize, s>>>(d_closing, width, height, topHatRadius, d_temp);

    {
        dim3 dimBlock(32, 32);
        dim3 dimGrid((width + 31) / 32, (height + 31) / 32);
        TransposeKernel<<<dimGrid, dimBlock, 0, s>>>(d_temp, d_closing, width, height);
    }

    dim3 gridCol((height + blockX - 1) / blockX, width);
    DilateRowKernel_Shared<<<gridCol, blockX, sharedMemSize, s>>>(d_closing, height, width, topHatRadius, d_temp);
    ErodeRowKernel_Shared<<<gridCol, blockX, sharedMemSize, s>>>(d_temp, height, width, topHatRadius, d_closing);

    {
        dim3 dimBlock(32, 32);
        dim3 dimGrid((height + 31) / 32, (width + 31) / 32);
        TransposeKernel<<<dimGrid, dimBlock, 0, s>>>(d_closing, d_temp, height, width);
    }

    Fused_ErodeRow_TopHat_Chipping_Kernel<<<gridRow, blockX, sharedMemSize, s>>>(
        d_temp, d_input, d_output, d_output2,
        width, height, topHatRadius,
        lineTop, lineBottom, lineLeft, lineRight, threshold, margin,
        topHatThreshold);

    status = cudaGetLastError();
    if (status != cudaSuccess) return status;

    status = cudaMemcpyAsync(h_outputMask, d_output, requiredSize, cudaMemcpyDeviceToHost, s);
    if (status != cudaSuccess) return status;
    if (h_outputMask2 != nullptr) {
        status = cudaMemcpyAsync(h_outputMask2, d_output2, requiredSize, cudaMemcpyDeviceToHost, s);
        if (status != cudaSuccess) return status;
    }
    return cudaStreamSynchronize(s);
}

// UpscaleROI2xBilinearAndSobel 의 컨텍스트 버전 — 입력(원본 전체)/출력(2x ROI) 슬롯 재사용.
extern "C" __declspec(dllexport)
cudaError_t UpscaleROI2xBilinearAndSobelCtx(
    void* ctxPtr,
    const uint8_t* input, int inWidth, int inHeight,
    int roiX, int roiY, int roiWidth, int roiHeight,
    uint8_t* output, uint8_t* outputSobel)
{
    QmcCudaContext* ctx = (QmcCudaContext*)ctxPtr;
    if (ctx == nullptr) return cudaErrorInvalidValue;

    size_t inSize = (size_t)inWidth * inHeight * sizeof(uint8_t);
    int outWidth = roiWidth * 2;
    int outHeight = roiHeight * 2;
    size_t outSize = (size_t)outWidth * outHeight * sizeof(uint8_t);

    cudaError_t status;
    void* p;
    uint8_t* d_input;       status = QmcEnsureBuf(ctx, 0, inSize,  &p); if (status != cudaSuccess) return status; d_input       = (uint8_t*)p;
    uint8_t* d_output;      status = QmcEnsureBuf(ctx, 1, outSize, &p); if (status != cudaSuccess) return status; d_output      = (uint8_t*)p;
    uint8_t* d_outputSobel; status = QmcEnsureBuf(ctx, 2, outSize, &p); if (status != cudaSuccess) return status; d_outputSobel = (uint8_t*)p;

    cudaStream_t s = ctx->stream;
    status = cudaMemcpyAsync(d_input, input, inSize, cudaMemcpyHostToDevice, s);
    if (status != cudaSuccess) return status;

    {
        dim3 blockDim(16, 16);
        dim3 gridDim((outWidth + blockDim.x - 1) / blockDim.x, (outHeight + blockDim.y - 1) / blockDim.y);
        UpscaleROI2xBilinearKernel<<<gridDim, blockDim, 0, s>>>(
            d_input, inWidth, inHeight,
            roiX, roiY, roiWidth, roiHeight,
            d_output, outWidth, outHeight);
        SobelKernel<<<gridDim, blockDim, 0, s>>>(d_output, d_outputSobel, outWidth, outHeight);
    }

    status = cudaGetLastError();
    if (status != cudaSuccess) return status;

    status = cudaMemcpyAsync(output, d_output, outSize, cudaMemcpyDeviceToHost, s);
    if (status != cudaSuccess) return status;
    status = cudaMemcpyAsync(outputSobel, d_outputSobel, outSize, cudaMemcpyDeviceToHost, s);
    if (status != cudaSuccess) return status;
    return cudaStreamSynchronize(s);
}

// ApplySobelFilter 의 컨텍스트 버전 — FindChipping 입력/mask1 슬롯 재사용(동시 사용 없음).
extern "C" __declspec(dllexport)
cudaError_t ApplySobelFilterCtx(
    void* ctxPtr,
    const uint8_t* h_inputImage,
    uint8_t* h_outputImage,
    int width,
    int height)
{
    QmcCudaContext* ctx = (QmcCudaContext*)ctxPtr;
    if (ctx == nullptr) return cudaErrorInvalidValue;

    size_t imageSize = (size_t)width * height * sizeof(uint8_t);
    cudaError_t status;
    void* p;
    uint8_t* d_input;  status = QmcEnsureBuf(ctx, 3, imageSize, &p); if (status != cudaSuccess) return status; d_input  = (uint8_t*)p;
    uint8_t* d_output; status = QmcEnsureBuf(ctx, 4, imageSize, &p); if (status != cudaSuccess) return status; d_output = (uint8_t*)p;

    cudaStream_t s = ctx->stream;
    status = cudaMemcpyAsync(d_input, h_inputImage, imageSize, cudaMemcpyHostToDevice, s);
    if (status != cudaSuccess) return status;

    {
        dim3 block(32, 32);
        dim3 grid((width + block.x - 1) / block.x, (height + block.y - 1) / block.y);
        SobelKernel<<<grid, block, 0, s>>>(d_input, d_output, width, height);
    }

    status = cudaGetLastError();
    if (status != cudaSuccess) return status;

    status = cudaMemcpyAsync(h_outputImage, d_output, imageSize, cudaMemcpyDeviceToHost, s);
    if (status != cudaSuccess) return status;
    return cudaStreamSynchronize(s);
}
