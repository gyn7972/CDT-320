#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include <stdio.h>
#include <stdint.h>
#include <math.h>
#include <vector> // <vector> 헤더 추가
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

// Union-Find: Find 연산 (경로 압축 포함)
__device__ int find_set(int* parent, int i) {
    if (parent[i] == i)
        return i;
    // 경로 압축
    return parent[i] = find_set(parent, parent[i]);
}

// Union-Find: Union 연산
__device__ void unite_sets(int* parent, int a, int b) {
    a = find_set(parent, a);
    b = find_set(parent, b);
    if (a != b) {
        // 더 작은 레이블을 부모로 설정
        if (a < b)
            parent[b] = a;
        else
            parent[a] = b;
    }
}

// 1단계: 초기 레이블링 및 이웃과 Union
__global__ void ccl_union_neighbors_kernel(const uint8_t* image, int* parent, int width, int height) {
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;

    if (x >= width || y >= height) return;

    int idx = y * width + x;
    if (image[idx] == 0) {
        parent[idx] = 0; // 배경
        return;
    }

    parent[idx] = idx + 1; // 초기 레이블 (0은 배경이므로 +1)

    // 오른쪽 이웃 확인
    if (x + 1 < width) {
        int right_idx = y * width + (x + 1);
        if (image[right_idx] != 0) {
            unite_sets(parent, idx + 1, right_idx + 1);
        }
    }

    // 아래쪽 이웃 확인
    if (y + 1 < height) {
        int down_idx = (y + 1) * width + x;
        if (image[down_idx] != 0) {
            unite_sets(parent, idx + 1, down_idx + 1);
        }
    }
}

// 2단계: 레이블 전파 (수렴할 때까지 반복) - size 인자 추가
__global__ void ccl_propagate_labels_kernel(int* parent, int size) {
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx < size && parent[idx] != 0) { // 경계 검사 추가
        find_set(parent, parent[idx]);
    }
}

// 3단계: 최종 레이블 할당
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

// 임계값 적용 커널
__global__ void threshold_kernel(const uint8_t* input, uint8_t* output, int size, uint8_t threshold) {
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx < size) {
        output[idx] = (input[idx] >= threshold) ? 1 : 0;
    }
}

// C#에서 호출할 메인 함수
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

    // 1. GPU 메모리 할당 및 데이터 복사
    uint8_t* d_inputImage, * d_binaryImage;
    cudaMalloc(&d_inputImage, imageSize);
    cudaMemcpy(d_inputImage, h_inputImage, imageSize, cudaMemcpyHostToDevice);
    cudaMalloc(&d_binaryImage, imageSize);

    // 2. 임계값 적용
    threshold_kernel<<<numBlocks1D, 1024>>>(d_inputImage, d_binaryImage, imageSize, threshold);

    // 3. CCL (Union-Find)
    int* d_parent;
    cudaMalloc(&d_parent, (imageSize + 1) * sizeof(int)); // 레이블이 1부터 시작하므로 +1

    ccl_union_neighbors_kernel<<<numBlocks, threadsPerBlock>>>(d_binaryImage, d_parent, width, height);

    // 레이블이 안정화될 때까지 반복 (보통 log(N) 횟수면 충분)
    for (int i = 0; i < 15; ++i) {
        // 수정된 커널 호출: size 인자 전달
        ccl_propagate_labels_kernel<<<numBlocks1D, 1024>>>(d_parent, imageSize + 1);
    }

    int* d_finalLabels;
    cudaMalloc(&d_finalLabels, imageSize * sizeof(int));
    ccl_final_labeling_kernel<<<numBlocks, threadsPerBlock>>>(d_binaryImage, d_parent, d_finalLabels, width, height);

    // 4. Thrust를 사용하여 블 blobs 정보 추출
    thrust::device_vector<int> d_keys(d_finalLabels, d_finalLabels + imageSize);
    thrust::device_vector<int> d_values(imageSize);
    thrust::sequence(d_values.begin(), d_values.end()); // 0, 1, 2, ...

    // 레이블(key)을 기준으로 정렬
    thrust::sort_by_key(d_keys.begin(), d_keys.end(), d_values.begin());

    // 고유한 레이블 수 찾기 (블롭 수)
    thrust::device_vector<int> d_unique_keys(d_keys.size());
    thrust::device_vector<int> d_counts(d_keys.size());

    auto end_iter = thrust::reduce_by_key(
        d_keys.begin(), d_keys.end(),
        thrust::make_constant_iterator(1),
        d_unique_keys.begin(),
        d_counts.begin()
    );

    int num_blobs = thrust::distance(d_unique_keys.begin(), end_iter.first);
    if (num_blobs <= 1) { // 배경(0)만 있거나 블롭이 없는 경우
        *blobCount = 0;
        *h_blobInfos = nullptr;
        cudaFree(d_inputImage); cudaFree(d_binaryImage); cudaFree(d_parent); cudaFree(d_finalLabels);
        return 0;
    }

    // 배경(레이블 0) 제외
    thrust::host_vector<int> h_unique_keys = d_unique_keys;
    thrust::host_vector<int> h_counts = d_counts;

    std::vector<BlobInfo> blob_results;
    for (int i = 1; i < num_blobs; ++i) { // 0번 레이블(배경)은 건너뜀
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

    // GPU 메모리 해제
    cudaFree(d_inputImage);
    cudaFree(d_binaryImage);
    cudaFree(d_parent);
    cudaFree(d_finalLabels);

    return 0; // 성공
}

// C#에서 할당된 메모리를 해제하기 위한 함수
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

    int mag = abs(gx) + abs(gy); // 빠른 근사 (sqrt 대신)
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

    // 1. 메모리 할당
    status = cudaMalloc(&d_input, imageSize);
    if (status != cudaSuccess) goto Error;
    
    status = cudaMalloc(&d_output, imageSize);
    if (status != cudaSuccess) goto Error;

    // 2. 데이터 복사
    status = cudaMemcpy(d_input, h_inputImage, imageSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;

    // 3. 커널 실행
    {
        dim3 block(32, 32);
        dim3 grid((width + block.x - 1) / block.x, (height + block.y - 1) / block.y);
        SobelKernel<<<grid, block>>>(d_input, d_output, width, height);
    }

    status = cudaGetLastError();
    if (status != cudaSuccess) goto Error;

    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Error;

    // 4. 결과 복사
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

    // 1. 메모리 할당
    status = cudaMalloc(&d_input, inSize);
    if (status != cudaSuccess) goto Error;

    status = cudaMalloc(&d_output, outSize);
    if (status != cudaSuccess) goto Error;

    // 2. 데이터 복사
    status = cudaMemcpy(d_input, input, inSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;

    // 3. 커널 실행
    {
        dim3 block(32, 32);
        dim3 grid((outWidth + block.x - 1) / block.x, (outHeight + block.y - 1) / block.y);
        Upscale2xBilinearKernel<<<grid, block>>>(d_input, inWidth, inHeight, d_output, outWidth, outHeight);
    }

    status = cudaGetLastError();
    if (status != cudaSuccess) goto Error;

    status = cudaDeviceSynchronize();
    if (status != cudaSuccess) goto Error;

    // 4. 결과 복사
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

    // 1. 메모리 할당
    status = cudaMalloc(&d_input, inSize);
    if (status != cudaSuccess) goto Error;

    status = cudaMalloc(&d_output, outSize);
    if (status != cudaSuccess) goto Error;

    // 2. 데이터 복사
    status = cudaMemcpy(d_input, input, inSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;

    // 3. 커널 실행
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

    // 4. 결과 복사
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

    // 1. 메모리 할당
    status = cudaMalloc(&d_input, inSize);
    if (status != cudaSuccess) goto Error;

    status = cudaMalloc(&d_output, outSize);
    if (status != cudaSuccess) goto Error;

    status = cudaMalloc(&d_outputSobel, outSize);
    if (status != cudaSuccess) goto Error;

    // 2. 데이터 복사
    status = cudaMemcpy(d_input, input, inSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;


    // 3. Upscale 커널 실행
    {
        dim3 blockDim(16, 16);
        dim3 gridDim((outWidth + blockDim.x - 1) / blockDim.x, (outHeight + blockDim.y - 1) / blockDim.y);
        UpscaleROI2xBilinearKernel<<<gridDim, blockDim>>>(
            d_input, inWidth, inHeight,
            roiX, roiY, roiWidth, roiHeight,
            d_output, outWidth, outHeight);
    }

    // 4. Sobel 커널 실행
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

    // 5. 결과 복사
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

// 외곽 영역 마스킹 커널: 라인+마진 바깥 픽셀을 fillValue로 채움
// 내부 픽셀은 원본값을 복사
// Closing 전에 적용하면, 외곽에서 Closing결과 ? fillValue → TopHat ? 0
// 외곽 치핑의 밝거나 어두운 픽셀이 Dilate를 통해 내부로 침투하는 것을 방지
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

// C#에서 호출할 수 있는 래퍼 함수
extern "C" __declspec(dllexport)
cudaError_t FindChipping(
    const uint8_t* h_inputImage, // Host의 입력 이미지
    uint8_t* h_outputMask,       // Host의 출력 마스크
    uint8_t* h_outputMask2,       // Host의 출력 마스크
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
    
    // 로컬 변수로 선언
    uint8_t* d_input = nullptr;
    uint8_t* d_output = nullptr;
    uint8_t* d_output2 = nullptr;
    uint8_t* d_temp = nullptr;
    uint8_t* d_closing = nullptr;

    // 1. 메모리 할당
    status = cudaMalloc(&d_input, requiredSize); if (status != cudaSuccess) goto Error;
    status = cudaMalloc(&d_output, requiredSize); if (status != cudaSuccess) goto Error;
    status = cudaMalloc(&d_output2, requiredSize); if (status != cudaSuccess) goto Error;
    status = cudaMalloc(&d_temp, requiredSize); if (status != cudaSuccess) goto Error;
    status = cudaMalloc(&d_closing, requiredSize); if (status != cudaSuccess) goto Error;

    // 2. 입력 데이터 복사
    status = cudaMemcpy(d_input, h_inputImage, requiredSize, cudaMemcpyHostToDevice);
    if (status != cudaSuccess) goto Error;

    topHatRadius = topHatRadius < 1 ? 1 : topHatRadius;

    // Block configuration for Row kernels (Horizontal Strip)
    int blockX = 256;
    size_t sharedMemSize = (blockX + 2 * topHatRadius) * sizeof(uint8_t);

    // -------------------------------------------------------------
    // 외곽 마스킹
    // -------------------------------------------------------------
    {
        dim3 maskBlock(32, 32);
        dim3 maskGrid((width + maskBlock.x - 1) / maskBlock.x, (height + maskBlock.y - 1) / maskBlock.y);
        MaskOutsideRegionKernel<<<maskGrid, maskBlock>>>(
            d_input, d_closing, // d_closing을 초기 버퍼로 사용
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
        d_input,    // Original input (마스킹 안된 원본)
        d_output,
        d_output2,   // Final output (Output 1)
        width, height, topHatRadius,
        lineTop, lineBottom, lineLeft, lineRight, threshold, margin,
        topHatThreshold
    );
    //
    //// 이전에 호출된 커널과 동일 출력을 d_output2에 저장 (필요한 경우 수정)
    //Fused_ErodeRow_TopHat_Chipping_Kernel << <gridRow, blockX, sharedMemSize >> > (
    //    d_temp,     // Source (ErodeCol result)
    //    d_input,    // Original input (마스킹 안된 원본)
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
