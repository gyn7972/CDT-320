// =============================================================================
// af_focus.cu  —  오토포커스 전용 CUDA 네이티브 DLL (AutoFocusCuda.dll)
//
//  QMC.Vision 의 C# P/Invoke(AutoFocusNativeCuda)와 1:1 로 매칭되는 export 3개.
//  C#(관리코드)은 이 DLL 의 함수를 "호출만" 하고, GPU 연산은 여기 들어 있다.
//  알고리즘은 CPU 의 AutoFocusCore.ScoreFocus 와 동일해야 결과가 일치한다.
//
//  [빌드] x64 Native Tools Command Prompt for VS 에서:
//     nvcc -O3 --shared --cudart static -o AutoFocusCuda.dll af_focus.cu
//  [배포] 생성된 AutoFocusCuda.dll 을 QMC.Vision.exe 와 같은 폴더(bin\Debug 등)에 복사.
//  [요구] 장비 PC 에 NVIDIA 드라이버(빌드한 CUDA 툴킷과 호환). --cudart static 이면 cudart DLL 별도 배포 불필요.
//
//  ※ extern "C" + __declspec(dllexport) 로 이름맹글링 없이 Cdecl 로 내보낸다(P/Invoke 와 일치).
// =============================================================================

#include <cuda_runtime.h>
#include <cstring>
#include <cstdint>

#define DLLEXPORT extern "C" __declspec(dllexport)
#define FOCUS_STEP 3      // CPU AutoFocusCore.FocusStep 과 동일
#define TOP_PIXELS 200    // CPU TopPixelCount 과 동일

// ── 픽셀별 8-이웃 라플라시안 응답 → 256-bin 히스토그램(atomicAdd) ──
//    CPU ScoreFocus 의 중앙(margin 제외) 루프와 동일 연산.
__global__ void focusRespHist(const unsigned char* g, int w,
                              int startX, int startY, int endX, int endY,
                              int objThreshold, unsigned int* hist)
{
    int x = startX + blockIdx.x * blockDim.x + threadIdx.x;
    int y = startY + blockIdx.y * blockDim.y + threadIdx.y;
    if (x >= endX || y >= endY) return;

    int nY = y * w;
    if (g[x + nY] <= objThreshold) return;          // 오브젝트 임계값 미달 → 응답 0(미반영)

    int w2 = w * FOCUS_STEP;
    int sum = (int)g[x + nY] * 8
            - g[x - FOCUS_STEP + nY - w2] - g[x - FOCUS_STEP + nY] - g[x - FOCUS_STEP + nY + w2]
            - g[x + FOCUS_STEP + nY - w2] - g[x + FOCUS_STEP + nY] - g[x + FOCUS_STEP + nY + w2]
            - g[x + nY - w2] - g[x + nY + w2];
    int resp = sum > 0 ? (sum > 255 ? 255 : sum) : 0;
    if (resp > 0) atomicAdd(&hist[resp], 1u);
}

// 사용 가능한 CUDA 디바이스 수(0 = 없음/드라이버 없음).
DLLEXPORT int af_cuda_device_count()
{
    int n = 0;
    if (cudaGetDeviceCount(&n) != cudaSuccess) return 0;
    return n;
}

// 디바이스 이름을 buf 에 채운다. 0 = 성공.
DLLEXPORT int af_cuda_device_name(int device, char* buf, int bufLen)
{
    cudaDeviceProp p;
    if (cudaGetDeviceProperties(&p, device) != cudaSuccess) return -1;
    if (buf && bufLen > 0) { strncpy(buf, p.name, bufLen - 1); buf[bufLen - 1] = 0; }
    return 0;
}

// 초점 점수: 8bit grayscale(gray, w*h) → 응답 상위 200픽셀 평균. 0=성공, 음수=실패(C#이 CPU로 폴백).
DLLEXPORT int af_focus_score_cuda(const unsigned char* gray, int w, int h,
                                  int objThreshold, double marginFraction, double* score)
{
    if (!gray || !score || w <= 2 * FOCUS_STEP || h <= 2 * FOCUS_STEP) return -1;
    *score = 0.0;

    // 채점 영역(가장자리 marginFraction 제외) — CPU ScoreFocus 와 동일.
    int mx = (int)(w * marginFraction);
    int my = (int)(h * marginFraction);
    int startX = FOCUS_STEP + mx; if (startX < FOCUS_STEP) startX = FOCUS_STEP;
    int startY = FOCUS_STEP + my; if (startY < FOCUS_STEP) startY = FOCUS_STEP;
    int endX = w - FOCUS_STEP - 1 - mx; if (endX > w) endX = w;
    int endY = h - FOCUS_STEP - 1 - my; if (endY > h) endY = h;
    if (endX <= startX || endY <= startY) return -1;

    size_t bytes = (size_t)w * h;
    unsigned char* dG = nullptr;
    unsigned int*  dHist = nullptr;
    int rc = -1;
    do {
        if (cudaMalloc(&dG, bytes) != cudaSuccess) break;
        if (cudaMalloc(&dHist, 256 * sizeof(unsigned int)) != cudaSuccess) break;
        if (cudaMemcpy(dG, gray, bytes, cudaMemcpyHostToDevice) != cudaSuccess) break;
        if (cudaMemset(dHist, 0, 256 * sizeof(unsigned int)) != cudaSuccess) break;

        dim3 block(16, 16);
        dim3 grid((endX - startX + 15) / 16, (endY - startY + 15) / 16);
        focusRespHist<<<grid, block>>>(dG, w, startX, startY, endX, endY, objThreshold, dHist);
        if (cudaDeviceSynchronize() != cudaSuccess) break;

        unsigned int hist[256];
        if (cudaMemcpy(hist, dHist, 256 * sizeof(unsigned int), cudaMemcpyDeviceToHost) != cudaSuccess) break;

        // 응답 상위 200픽셀 평균(부족분은 0으로 채워 200으로 나눔 — CPU 와 동일).
        long long sum = 0; int need = TOP_PIXELS;
        for (int v = 255; v >= 1 && need > 0; --v) {
            int take = (int)hist[v];
            if (take > need) take = need;
            sum += (long long)v * take;
            need -= take;
        }
        *score = (double)sum / TOP_PIXELS;
        rc = 0;
    } while (0);

    if (dG)    cudaFree(dG);
    if (dHist) cudaFree(dHist);
    return rc;
}
