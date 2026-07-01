// =============================================================================
//  stddev_filter.cu — 표준편차 텍스처 필터  [콜렛/플랫콜렛]
//
//  대응 CPU: Collet.StdDevFilter.ComputeCpuParallel (적분영상 기반).
//  사용처  : 콜렛 파인더(ColletBlobRectFinder), 플랫콜렛(FlatColletFinder).
//  알고리즘: 적분영상(SAT)으로 블럭 합/제곱합 O(1). var=E[x^2]-E[x]^2, std>=thr→255 else 0.
//  디바이스 버퍼는 캐시(매 호출 재할당 회피). 필터는 직렬 호출 가정(스레드 세이프 아님).
//  export : cf_stddev_filter_cuda(...)  (성공 0),  cf_cuda_cleanup()  (캐시 해제, 선택)
// =============================================================================
#include "../include/qmc_cuda.h"

__global__ void rowScanKernel(const unsigned char* gray, int w, int h, long long* integ, long long* integSq)
{
    int r = blockIdx.x * blockDim.x + threadIdx.x;
    if (r >= h) return;
    int sw = w + 1;
    long long acc = 0, accSq = 0;
    const unsigned char* gRow = gray + (size_t)r * w;
    long long* iRow  = integ   + (size_t)(r + 1) * sw;
    long long* iRowS = integSq + (size_t)(r + 1) * sw;
    iRow[0] = 0; iRowS[0] = 0;
    for (int x = 1; x <= w; x++)
    {
        int g = gRow[x - 1];
        acc += g; accSq += (long long)g * g;
        iRow[x] = acc; iRowS[x] = accSq;
    }
}
__global__ void colScanKernel(int w, int h, long long* integ, long long* integSq)
{
    int c = blockIdx.x * blockDim.x + threadIdx.x;
    if (c >= w) return;
    int sw = w + 1, col = c + 1;
    long long acc = 0, accSq = 0;
    for (int y = 1; y <= h; y++)
    {
        size_t idx = (size_t)y * sw + col;
        acc += integ[idx]; accSq += integSq[idx];
        integ[idx] = acc; integSq[idx] = accSq;
    }
}
__global__ void boxStdKernel(int w, int h, const long long* integ, const long long* integSq,
                             int rx0, int ry0, int rx1, int ry1, int blockSize, double threshold,
                             unsigned char* out)
{
    int x = rx0 + blockIdx.x * blockDim.x + threadIdx.x;
    int y = ry0 + blockIdx.y * blockDim.y + threadIdx.y;
    if (x > rx1 || y > ry1) return;
    int half = blockSize / 2;
    int ax0 = x - half;                 if (ax0 < 0)     ax0 = 0;
    int ax1 = x - half + blockSize - 1; if (ax1 > w - 1) ax1 = w - 1;
    int ay0 = y - half;                 if (ay0 < 0)     ay0 = 0;
    int ay1 = y - half + blockSize - 1; if (ay1 > h - 1) ay1 = h - 1;
    int sw = w + 1;
    int top = ay0 * sw, bot = (ay1 + 1) * sw;
    long long sum   = integ[bot + ax1 + 1]   - integ[bot + ax0]   - integ[top + ax1 + 1]   + integ[top + ax0];
    long long sumSq = integSq[bot + ax1 + 1] - integSq[bot + ax0] - integSq[top + ax1 + 1] + integSq[top + ax0];
    long long n = (long long)(ax1 - ax0 + 1) * (ay1 - ay0 + 1);
    double mean = (double)sum / (double)n;
    double var  = (double)sumSq / (double)n - mean * mean;
    if (var < 0.0) var = 0.0;
    out[(size_t)y * w + x] = (sqrt(var) >= threshold) ? (unsigned char)255 : (unsigned char)0;
}

static unsigned char* g_dGray = 0; static unsigned char* g_dOut = 0;
static long long* g_dInteg = 0;    static long long* g_dIntegSq = 0;
static size_t g_capPix = 0, g_capInteg = 0;
static void freeStdBuffers()
{
    if (g_dGray) cudaFree(g_dGray);   if (g_dOut) cudaFree(g_dOut);
    if (g_dInteg) cudaFree(g_dInteg); if (g_dIntegSq) cudaFree(g_dIntegSq);
    g_dGray = g_dOut = 0; g_dInteg = g_dIntegSq = 0; g_capPix = g_capInteg = 0;
}
static cudaError_t ensureStdBuffers(size_t nPix, size_t nInteg)
{
    cudaError_t e;
    if (g_capPix < nPix)
    {
        if (g_dGray) { cudaFree(g_dGray); g_dGray = 0; }
        if (g_dOut)  { cudaFree(g_dOut);  g_dOut = 0; }
        g_capPix = 0;
        e = cudaMalloc((void**)&g_dGray, nPix); if (e != cudaSuccess) return e;
        e = cudaMalloc((void**)&g_dOut,  nPix); if (e != cudaSuccess) { cudaFree(g_dGray); g_dGray = 0; return e; }
        g_capPix = nPix;
    }
    if (g_capInteg < nInteg)
    {
        if (g_dInteg)   { cudaFree(g_dInteg);   g_dInteg = 0; }
        if (g_dIntegSq) { cudaFree(g_dIntegSq); g_dIntegSq = 0; }
        g_capInteg = 0;
        e = cudaMalloc((void**)&g_dInteg,   nInteg * sizeof(long long)); if (e != cudaSuccess) return e;
        e = cudaMalloc((void**)&g_dIntegSq, nInteg * sizeof(long long)); if (e != cudaSuccess) { cudaFree(g_dInteg); g_dInteg = 0; return e; }
        g_capInteg = nInteg;
    }
    return cudaSuccess;
}
DLLEXPORT void cf_cuda_cleanup() { freeStdBuffers(); }

DLLEXPORT int cf_stddev_filter_cuda(const unsigned char* gray, int w, int h,
                                    int roiX, int roiY, int roiW, int roiH,
                                    int blockSize, double threshold, unsigned char* outGray)
{
    if (!gray || !outGray || w <= 0 || h <= 0) return -1;
    if (blockSize < 1) blockSize = 1;
    int rx0 = roiX < 0 ? 0 : roiX;
    int ry0 = roiY < 0 ? 0 : roiY;
    int rx1 = roiX + roiW - 1; if (rx1 > w - 1) rx1 = w - 1;
    int ry1 = roiY + roiH - 1; if (ry1 > h - 1) ry1 = h - 1;
    if (rx1 < rx0 || ry1 < ry0) { rx0 = 0; ry0 = 0; rx1 = w - 1; ry1 = h - 1; }

    size_t nPix = (size_t)w * h;
    size_t nInteg = (size_t)(w + 1) * (h + 1);
    int sw = w + 1;
    cudaError_t e = ensureStdBuffers(nPix, nInteg);
    if (e != cudaSuccess) { freeStdBuffers(); return -100 - (int)e; }

    e = cudaMemcpy(g_dGray, gray, nPix, cudaMemcpyHostToDevice); if (e != cudaSuccess) goto fail;
    e = cudaMemset(g_dOut, 0, nPix);                             if (e != cudaSuccess) goto fail;
    e = cudaMemset(g_dInteg,   0, (size_t)sw * sizeof(long long)); if (e != cudaSuccess) goto fail;
    e = cudaMemset(g_dIntegSq, 0, (size_t)sw * sizeof(long long)); if (e != cudaSuccess) goto fail;
    {
        int threads = 256;
        rowScanKernel<<<(h + threads - 1) / threads, threads>>>(g_dGray, w, h, g_dInteg, g_dIntegSq);
        e = cudaGetLastError(); if (e != cudaSuccess) goto fail;
        colScanKernel<<<(w + threads - 1) / threads, threads>>>(w, h, g_dInteg, g_dIntegSq);
        e = cudaGetLastError(); if (e != cudaSuccess) goto fail;
        int roiCols = rx1 - rx0 + 1, roiRows = ry1 - ry0 + 1;
        dim3 blk(16, 16);
        dim3 grd((roiCols + blk.x - 1) / blk.x, (roiRows + blk.y - 1) / blk.y);
        boxStdKernel<<<grd, blk>>>(w, h, g_dInteg, g_dIntegSq, rx0, ry0, rx1, ry1, blockSize, threshold, g_dOut);
        e = cudaGetLastError();      if (e != cudaSuccess) goto fail;
        e = cudaDeviceSynchronize(); if (e != cudaSuccess) goto fail;
    }
    e = cudaMemcpy(outGray, g_dOut, nPix, cudaMemcpyDeviceToHost); if (e != cudaSuccess) goto fail;
    return 0;
fail:
    freeStdBuffers();
    return -200 - (int)e;
}
