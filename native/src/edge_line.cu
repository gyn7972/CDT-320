// =============================================================================
//  edge_line.cu — 에지/라인 교차 검출  [측면 라인 + Bottom 다이 4변]
//
//  교차 정의(공통, 2픽셀 확인): 밝기 g 가 임계 thr 을 "안→밖" 방향으로 넘고, 넘은 뒤 2픽셀이 thr 이하.
//
//  1) FindTopBottomLineCandidates  (디바이스 포인터 입력)
//     대응 CPU : SideChippingCore.GetLineCandidates. 각 열의 상/하 라인 후보 y(-1=없음).
//     사용처   : 측면 칩핑(SideChippingCore).
//  2) cf_find_die_edges            (호스트 배열 입출력)
//     대응 CPU : BottomInspector.FindDie. 상/하(열당 int[w]) + 좌/우(행당 int[h]) 4변 에지.
//     사용처   : Bottom 다이 4변 검출.
// =============================================================================
#include "../include/qmc_cuda.h"

// ── 1) 측면 라인 후보 (디바이스 포인터) ──────────────────────────────────────
__global__ void topBottomLineKernel(const unsigned char* g, int w, int h, unsigned char thr,
                                    int topStartY, int topEndY, int botStartY, int botEndY,
                                    int* topC, int* botC)
{
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    if (x >= w) return;

    topC[x] = -1;
    for (int y = topStartY; y <= topEndY; y++)
    {
        if (y < 2 || y >= h) continue;
        if (g[y * w + x] > thr && g[(y - 1) * w + x] <= thr && g[(y - 2) * w + x] <= thr) { topC[x] = y; break; }
    }
    botC[x] = -1;
    for (int y = botStartY; y >= botEndY; y--)
    {
        if (y < 0 || y > h - 3) continue;
        if (g[y * w + x] > thr && g[(y + 1) * w + x] <= thr && g[(y + 2) * w + x] <= thr) { botC[x] = y; break; }
    }
}

DLLEXPORT int FindTopBottomLineCandidates(void* d_input, int width, int height, unsigned char threshold,
                                          int topStartY, int topEndY, int bottomStartY, int bottomEndY,
                                          void* d_topCandidates, void* d_bottomCandidates)
{
    if (!d_input || !d_topCandidates || !d_bottomCandidates || width <= 0 || height <= 0) return -1;
    int threads = 256;
    int blocks = (width + threads - 1) / threads;
    topBottomLineKernel<<<blocks, threads>>>((const unsigned char*)d_input, width, height, threshold,
                                             topStartY, topEndY, bottomStartY, bottomEndY,
                                             (int*)d_topCandidates, (int*)d_bottomCandidates);
    if (cudaGetLastError() != cudaSuccess) return -2;
    if (cudaDeviceSynchronize() != cudaSuccess) return -3;
    return 0;
}

// ── 2) Bottom 다이 4변 에지 (호스트 배열) ────────────────────────────────────
//   top : y=2..h-1   g[y]>thr && g[y-1]<=thr && g[y-2]<=thr  (첫 y)
//   bot : y=h-3..0   g[y]>thr && g[y+1]<=thr && g[y+2]<=thr  (첫 y)
//   left: x=2..w-1   g[x]>thr && g[x-1]<=thr && g[x-2]<=thr  (첫 x)
//   right:x=w-3..0   g[x]>thr && g[x+1]<=thr && g[x+2]<=thr  (첫 x)
__global__ void dieColEdgeKernel(const unsigned char* g, int w, int h, int thr, int* topE, int* botE)
{
    int x = blockIdx.x * blockDim.x + threadIdx.x;
    if (x >= w) return;
    topE[x] = -1; botE[x] = -1;
    for (int y = 2; y < h; y++)
        if (g[y * w + x] > thr && g[(y - 1) * w + x] <= thr && g[(y - 2) * w + x] <= thr) { topE[x] = y; break; }
    for (int y = h - 3; y >= 0; y--)
        if (g[y * w + x] > thr && g[(y + 1) * w + x] <= thr && g[(y + 2) * w + x] <= thr) { botE[x] = y; break; }
}
__global__ void dieRowEdgeKernel(const unsigned char* g, int w, int h, int thr, int* leftE, int* rightE)
{
    int y = blockIdx.x * blockDim.x + threadIdx.x;
    if (y >= h) return;
    int row = y * w;
    leftE[y] = -1; rightE[y] = -1;
    for (int x = 2; x < w; x++)
        if (g[row + x] > thr && g[row + x - 1] <= thr && g[row + x - 2] <= thr) { leftE[y] = x; break; }
    for (int x = w - 3; x >= 0; x--)
        if (g[row + x] > thr && g[row + x + 1] <= thr && g[row + x + 2] <= thr) { rightE[y] = x; break; }
}

// gray = 호스트 w*h 바이트. 출력 topE/botE = 호스트 int[w], leftE/rightE = 호스트 int[h]. 성공 0.
DLLEXPORT int cf_find_die_edges(const unsigned char* gray, int w, int h, int thr,
                                int* topE, int* botE, int* leftE, int* rightE)
{
    if (!gray || !topE || !botE || !leftE || !rightE || w <= 2 || h <= 2) return -1;
    unsigned char* dG = 0;
    int *dTop = 0, *dBot = 0, *dLeft = 0, *dRight = 0;
    int rc = -1;
    size_t bytes = (size_t)w * h;
    do {
        if (cudaMalloc((void**)&dG, bytes) != cudaSuccess) break;
        if (cudaMalloc((void**)&dTop,  w * sizeof(int)) != cudaSuccess) break;
        if (cudaMalloc((void**)&dBot,  w * sizeof(int)) != cudaSuccess) break;
        if (cudaMalloc((void**)&dLeft, h * sizeof(int)) != cudaSuccess) break;
        if (cudaMalloc((void**)&dRight,h * sizeof(int)) != cudaSuccess) break;
        if (cudaMemcpy(dG, gray, bytes, cudaMemcpyHostToDevice) != cudaSuccess) break;

        int t = 256;
        dieColEdgeKernel<<<(w + t - 1) / t, t>>>(dG, w, h, thr, dTop, dBot);
        if (cudaGetLastError() != cudaSuccess) break;
        dieRowEdgeKernel<<<(h + t - 1) / t, t>>>(dG, w, h, thr, dLeft, dRight);
        if (cudaGetLastError() != cudaSuccess) break;
        if (cudaDeviceSynchronize() != cudaSuccess) break;

        if (cudaMemcpy(topE,  dTop,  w * sizeof(int), cudaMemcpyDeviceToHost) != cudaSuccess) break;
        if (cudaMemcpy(botE,  dBot,  w * sizeof(int), cudaMemcpyDeviceToHost) != cudaSuccess) break;
        if (cudaMemcpy(leftE, dLeft, h * sizeof(int), cudaMemcpyDeviceToHost) != cudaSuccess) break;
        if (cudaMemcpy(rightE,dRight,h * sizeof(int), cudaMemcpyDeviceToHost) != cudaSuccess) break;
        rc = 0;
    } while (0);
    if (dG)    cudaFree(dG);
    if (dTop)  cudaFree(dTop);
    if (dBot)  cudaFree(dBot);
    if (dLeft) cudaFree(dLeft);
    if (dRight)cudaFree(dRight);
    return rc;
}
