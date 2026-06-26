// ColletFinderCuda.cu
// CUDA implementation of the ROI local standard-deviation texture filter.
// Exposes C-ABI (extern "C") functions called from C# via P/Invoke.
//
// Uses an integral image (Summed-Area Table) so each output pixel costs O(1)
// regardless of block size:
//   1) rowScanKernel : per-row inclusive prefix sums (one thread per row)
//   2) colScanKernel : per-column inclusive prefix sums (one thread per column)
//   3) boxStdKernel  : per pixel, block sum/sumSq via 4 corner lookups -> std
//   var = E[x^2] - (E[x])^2,  std = sqrt(var);  std >= threshold -> 255 else 0.

#define _CRT_SECURE_NO_WARNINGS
#include <cuda_runtime.h>
#include <math.h>
#include <string.h>

#define CF_EXPORT extern "C" __declspec(dllexport)

// Padded integral images are (w+1) x (h+1); row 0 and col 0 are zero.

// 1) Row-wise inclusive prefix sums. One thread per image row.
__global__ void rowScanKernel(const unsigned char* gray, int w, int h,
                              long long* integ, long long* integSq)
{
    int r = blockIdx.x * blockDim.x + threadIdx.x; // 0..h-1
    if (r >= h) return;

    int sw = w + 1;
    long long acc = 0, accSq = 0;
    const unsigned char* gRow = gray + (size_t)r * w;
    long long* iRow  = integ   + (size_t)(r + 1) * sw;
    long long* iRowS = integSq + (size_t)(r + 1) * sw;
    iRow[0]  = 0;   // column-0 padding (lets us skip zeroing the whole integral image)
    iRowS[0] = 0;
    for (int x = 1; x <= w; x++)
    {
        int g = gRow[x - 1];
        acc   += g;
        accSq += (long long)g * g;
        iRow[x]  = acc;    // row prefix (column scan will finish it)
        iRowS[x] = accSq;
    }
}

// 2) Column-wise inclusive prefix sums (in place). One thread per image column.
__global__ void colScanKernel(int w, int h, long long* integ, long long* integSq)
{
    int c = blockIdx.x * blockDim.x + threadIdx.x; // 0..w-1
    if (c >= w) return;

    int sw = w + 1;
    int col = c + 1;
    long long acc = 0, accSq = 0;
    for (int y = 1; y <= h; y++)
    {
        size_t idx = (size_t)y * sw + col;
        acc   += integ[idx];
        accSq += integSq[idx];
        integ[idx]   = acc;
        integSq[idx] = accSq;
    }
}

// 3) Per-pixel local std via the integral image (O(1) per pixel).
__global__ void boxStdKernel(int w, int h,
                             const long long* integ, const long long* integSq,
                             int rx0, int ry0, int rx1, int ry1,
                             int blockSize, double threshold,
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
    int top = ay0 * sw;
    int bot = (ay1 + 1) * sw;
    long long sum   = integ[bot + ax1 + 1]   - integ[bot + ax0]
                    - integ[top + ax1 + 1]   + integ[top + ax0];
    long long sumSq = integSq[bot + ax1 + 1] - integSq[bot + ax0]
                    - integSq[top + ax1 + 1] + integSq[top + ax0];

    long long n = (long long)(ax1 - ax0 + 1) * (ay1 - ay0 + 1);
    double mean = (double)sum / (double)n;
    double var  = (double)sumSq / (double)n - mean * mean;
    if (var < 0.0) var = 0.0;
    double std = sqrt(var);

    out[(size_t)y * w + x] = (std >= threshold) ? (unsigned char)255 : (unsigned char)0;
}

// ---- export: device count ----------------------------------------------
CF_EXPORT int cf_cuda_device_count()
{
    int n = 0;
    cudaError_t e = cudaGetDeviceCount(&n);
    if (e != cudaSuccess) return 0;
    return n;
}

// ---- export: device name -----------------------------------------------
CF_EXPORT int cf_cuda_device_name(int device, char* buf, int bufLen)
{
    if (!buf || bufLen <= 0) return -1;
    cudaDeviceProp prop;
    cudaError_t e = cudaGetDeviceProperties(&prop, device);
    if (e != cudaSuccess) return -2;
    strncpy(buf, prop.name, bufLen - 1);
    buf[bufLen - 1] = '\0';
    return 0;
}

// ---- persistent device buffers -----------------------------------------
// Re-allocating ~GB of device memory on every call dominated the runtime, so
// the buffers are cached and only (re)grown when a larger image arrives.
// (Not thread-safe: the filter is expected to be called serially.)
static unsigned char* g_dGray    = 0;
static unsigned char* g_dOut     = 0;
static long long*     g_dInteg   = 0;
static long long*     g_dIntegSq = 0;
static size_t g_capPix   = 0;   // capacity of gray/out, in bytes
static size_t g_capInteg = 0;   // capacity of integ/integSq, in elements

static void freeBuffers()
{
    if (g_dGray)    cudaFree(g_dGray);
    if (g_dOut)     cudaFree(g_dOut);
    if (g_dInteg)   cudaFree(g_dInteg);
    if (g_dIntegSq) cudaFree(g_dIntegSq);
    g_dGray = 0; g_dOut = 0; g_dInteg = 0; g_dIntegSq = 0;
    g_capPix = 0; g_capInteg = 0;
}

static cudaError_t ensureBuffers(size_t nPix, size_t nInteg)
{
    cudaError_t e;
    if (g_capPix < nPix)
    {
        if (g_dGray) { cudaFree(g_dGray); g_dGray = 0; }
        if (g_dOut)  { cudaFree(g_dOut);  g_dOut  = 0; }
        g_capPix = 0;
        e = cudaMalloc((void**)&g_dGray, nPix); if (e != cudaSuccess) return e;
        e = cudaMalloc((void**)&g_dOut,  nPix); if (e != cudaSuccess) { cudaFree(g_dGray); g_dGray = 0; return e; }
        g_capPix = nPix;
    }
    if (g_capInteg < nInteg)
    {
        if (g_dInteg)   { cudaFree(g_dInteg);   g_dInteg   = 0; }
        if (g_dIntegSq) { cudaFree(g_dIntegSq); g_dIntegSq = 0; }
        g_capInteg = 0;
        e = cudaMalloc((void**)&g_dInteg,   nInteg * sizeof(long long)); if (e != cudaSuccess) return e;
        e = cudaMalloc((void**)&g_dIntegSq, nInteg * sizeof(long long)); if (e != cudaSuccess) { cudaFree(g_dInteg); g_dInteg = 0; return e; }
        g_capInteg = nInteg;
    }
    return cudaSuccess;
}

// ---- export: release cached device buffers (optional; called on shutdown) -
CF_EXPORT void cf_cuda_cleanup()
{
    freeBuffers();
}

// ---- export: the filter ------------------------------------------------
// Returns 0 on success, negative on failure. gray/out are w*h bytes (row-major).
CF_EXPORT int cf_stddev_filter_cuda(const unsigned char* gray, int w, int h,
                                    int roiX, int roiY, int roiW, int roiH,
                                    int blockSize, double threshold,
                                    unsigned char* outGray)
{
    if (!gray || !outGray || w <= 0 || h <= 0) return -1;
    if (blockSize < 1) blockSize = 1;

    // clamp/normalize ROI
    int rx0 = roiX < 0 ? 0 : roiX;
    int ry0 = roiY < 0 ? 0 : roiY;
    int rx1 = roiX + roiW - 1; if (rx1 > w - 1) rx1 = w - 1;
    int ry1 = roiY + roiH - 1; if (ry1 > h - 1) ry1 = h - 1;
    if (rx1 < rx0 || ry1 < ry0) { rx0 = 0; ry0 = 0; rx1 = w - 1; ry1 = h - 1; }

    size_t nPix   = (size_t)w * h;
    size_t nInteg = (size_t)(w + 1) * (h + 1);
    int    sw     = w + 1;

    cudaError_t e = ensureBuffers(nPix, nInteg);
    if (e != cudaSuccess) { freeBuffers(); return -100 - (int)e; }

    unsigned char* dGray    = g_dGray;
    unsigned char* dOut     = g_dOut;
    long long*     dInteg   = g_dInteg;
    long long*     dIntegSq = g_dIntegSq;

    e = cudaMemcpy(dGray, gray, nPix, cudaMemcpyHostToDevice); if (e != cudaSuccess) goto fail;
    e = cudaMemset(dOut, 0, nPix);                            if (e != cudaSuccess) goto fail; // outside ROI = black
    // Only the padding row 0 must be pre-zeroed; rowScanKernel zeroes column 0
    // and the scans overwrite the rest. (Avoids memset-ing the whole ~GB integral.)
    e = cudaMemset(dInteg,   0, (size_t)sw * sizeof(long long)); if (e != cudaSuccess) goto fail;
    e = cudaMemset(dIntegSq, 0, (size_t)sw * sizeof(long long)); if (e != cudaSuccess) goto fail;

    {
        int threads = 256;
        rowScanKernel<<<(h + threads - 1) / threads, threads>>>(dGray, w, h, dInteg, dIntegSq);
        e = cudaGetLastError(); if (e != cudaSuccess) goto fail;

        colScanKernel<<<(w + threads - 1) / threads, threads>>>(w, h, dInteg, dIntegSq);
        e = cudaGetLastError(); if (e != cudaSuccess) goto fail;

        int roiCols = rx1 - rx0 + 1;
        int roiRows = ry1 - ry0 + 1;
        dim3 blk(16, 16);
        dim3 grd((roiCols + blk.x - 1) / blk.x, (roiRows + blk.y - 1) / blk.y);
        boxStdKernel<<<grd, blk>>>(w, h, dInteg, dIntegSq, rx0, ry0, rx1, ry1, blockSize, threshold, dOut);
        e = cudaGetLastError();      if (e != cudaSuccess) goto fail;
        e = cudaDeviceSynchronize(); if (e != cudaSuccess) goto fail;
    }

    e = cudaMemcpy(outGray, dOut, nPix, cudaMemcpyDeviceToHost); if (e != cudaSuccess) goto fail;
    return 0;

fail:
    freeBuffers();   // drop cache so the next call starts clean
    return -100 - (int)e;
}
