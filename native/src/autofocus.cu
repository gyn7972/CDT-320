// =============================================================================
//  autofocus.cu — 오토포커스 초점 점수  [오토포커스]
//
//  대응 CPU: AutoFocusCore.ScoreFocus.
//  사용처  : 오토포커스(AutoFocusCore.ScoreGray → af_focus_score_cuda).
//  알고리즘: 8-이웃 라플라시안 응답 → objThreshold 게이트 → margin 제외 → 응답 상위 200 평균.
//  export : af_focus_score_cuda(gray, w, h, objThreshold, marginFraction, out score)  (성공 0)
// =============================================================================
#include "../include/qmc_cuda.h"

#define FOCUS_STEP 3    // AutoFocusCore.FocusStep 과 동일
#define TOP_PIXELS 200  // AutoFocusCore.TopPixelCount 과 동일

__global__ void focusRespHist(const unsigned char* g, int w,
                              int startX, int startY, int endX, int endY,
                              int objThreshold, unsigned int* hist)
{
    int x = startX + blockIdx.x * blockDim.x + threadIdx.x;
    int y = startY + blockIdx.y * blockDim.y + threadIdx.y;
    if (x >= endX || y >= endY) return;
    int nY = y * w;
    if (g[x + nY] <= objThreshold) return;
    int w2 = w * FOCUS_STEP;
    int sum = (int)g[x + nY] * 8
            - g[x - FOCUS_STEP + nY - w2] - g[x - FOCUS_STEP + nY] - g[x - FOCUS_STEP + nY + w2]
            - g[x + FOCUS_STEP + nY - w2] - g[x + FOCUS_STEP + nY] - g[x + FOCUS_STEP + nY + w2]
            - g[x + nY - w2] - g[x + nY + w2];
    int resp = sum > 0 ? (sum > 255 ? 255 : sum) : 0;
    if (resp > 0) atomicAdd(&hist[resp], 1u);
}

DLLEXPORT int af_focus_score_cuda(const unsigned char* gray, int w, int h,
                                  int objThreshold, double marginFraction, double* score)
{
    if (!gray || !score || w <= 2 * FOCUS_STEP || h <= 2 * FOCUS_STEP) return -1;
    *score = 0.0;
    int mx = (int)(w * marginFraction);
    int my = (int)(h * marginFraction);
    int startX = FOCUS_STEP + mx; if (startX < FOCUS_STEP) startX = FOCUS_STEP;
    int startY = FOCUS_STEP + my; if (startY < FOCUS_STEP) startY = FOCUS_STEP;
    int endX = w - FOCUS_STEP - 1 - mx; if (endX > w) endX = w;
    int endY = h - FOCUS_STEP - 1 - my; if (endY > h) endY = h;
    if (endX <= startX || endY <= startY) return -1;

    size_t bytes = (size_t)w * h;
    unsigned char* dG = 0; unsigned int* dHist = 0;
    int rc = -1;
    do {
        if (cudaMalloc((void**)&dG, bytes) != cudaSuccess) break;
        if (cudaMalloc((void**)&dHist, 256 * sizeof(unsigned int)) != cudaSuccess) break;
        if (cudaMemcpy(dG, gray, bytes, cudaMemcpyHostToDevice) != cudaSuccess) break;
        if (cudaMemset(dHist, 0, 256 * sizeof(unsigned int)) != cudaSuccess) break;
        dim3 block(16, 16);
        dim3 grid((endX - startX + 15) / 16, (endY - startY + 15) / 16);
        focusRespHist<<<grid, block>>>(dG, w, startX, startY, endX, endY, objThreshold, dHist);
        if (cudaGetLastError() != cudaSuccess) break;
        if (cudaDeviceSynchronize() != cudaSuccess) break;
        unsigned int hist[256];
        if (cudaMemcpy(hist, dHist, 256 * sizeof(unsigned int), cudaMemcpyDeviceToHost) != cudaSuccess) break;
        long long sum = 0; int need = TOP_PIXELS;
        for (int v = 255; v >= 1 && need > 0; --v)
        {
            int take = (int)hist[v]; if (take > need) take = need;
            sum += (long long)v * take; need -= take;
        }
        *score = (double)sum / TOP_PIXELS;
        rc = 0;
    } while (0);
    if (dG)    cudaFree(dG);
    if (dHist) cudaFree(dHist);
    return rc;
}
