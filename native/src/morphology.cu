// =============================================================================
//  morphology.cu — 박스(정사각 SE) 그레이 모폴로지  [이물/Black-Hat]
//
//  대응 CPU: ContaminationDetector.SepMorph (분리형 clamped 박스 최대/최소).
//  사용처  : 이물/오염 검출의 Closing(Erode∘Dilate) → Black-Hat. Bottom/Side 표면검사 공용.
//  분리형: 가로 패스 → 세로 패스, 윈도우 [i-r, i+r] 경계 clamp. isMax!=0→dilate(최대), 0→erode(최소).
//  export : cf_morph_box_u8(src, dst, w, h, radius, isMax)  (호스트 배열 입/출력, 성공 0)
// =============================================================================
#include "../include/qmc_cuda.h"

__global__ void morphHPass(const unsigned char* src, unsigned char* tmp, int w, int h, int r, int isMax)
{
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;
    if (x >= w || y >= h) return;
    int lo = x - r; if (lo < 0) lo = 0;
    int hi = x + r; if (hi > w - 1) hi = w - 1;
    int row = y * w;
    unsigned char best = src[row + lo];
    for (int i = lo + 1; i <= hi; i++)
    {
        unsigned char v = src[row + i];
        best = isMax ? (v > best ? v : best) : (v < best ? v : best);
    }
    tmp[row + x] = best;
}
__global__ void morphVPass(const unsigned char* tmp, unsigned char* dst, int w, int h, int r, int isMax)
{
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    int y = blockIdx.y * blockDim.y + threadIdx.y;
    if (x >= w || y >= h) return;
    int lo = y - r; if (lo < 0) lo = 0;
    int hi = y + r; if (hi > h - 1) hi = h - 1;
    unsigned char best = tmp[lo * w + x];
    for (int i = lo + 1; i <= hi; i++)
    {
        unsigned char v = tmp[i * w + x];
        best = isMax ? (v > best ? v : best) : (v < best ? v : best);
    }
    dst[y * w + x] = best;
}

DLLEXPORT int cf_morph_box_u8(const unsigned char* src, unsigned char* dst,
                              int width, int height, int radius, int isMax)
{
    if (!src || !dst || width <= 0 || height <= 0) return -1;
    if (radius < 1) radius = 1;
    unsigned char *dSrc = 0, *dTmp = 0, *dDst = 0;
    int rc = -1;
    size_t bytes = (size_t)width * height;
    do {
        if (cudaMalloc((void**)&dSrc, bytes) != cudaSuccess) break;
        if (cudaMalloc((void**)&dTmp, bytes) != cudaSuccess) break;
        if (cudaMalloc((void**)&dDst, bytes) != cudaSuccess) break;
        if (cudaMemcpy(dSrc, src, bytes, cudaMemcpyHostToDevice) != cudaSuccess) break;

        dim3 blk(16, 16);
        dim3 grd((width + 15) / 16, (height + 15) / 16);
        morphHPass<<<grd, blk>>>(dSrc, dTmp, width, height, radius, isMax);
        if (cudaGetLastError() != cudaSuccess) break;
        morphVPass<<<grd, blk>>>(dTmp, dDst, width, height, radius, isMax);
        if (cudaGetLastError() != cudaSuccess) break;
        if (cudaDeviceSynchronize() != cudaSuccess) break;

        if (cudaMemcpy(dst, dDst, bytes, cudaMemcpyDeviceToHost) != cudaSuccess) break;
        rc = 0;
    } while (0);
    if (dSrc) cudaFree(dSrc);
    if (dTmp) cudaFree(dTmp);
    if (dDst) cudaFree(dDst);
    return rc;
}
