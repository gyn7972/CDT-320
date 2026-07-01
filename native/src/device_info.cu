// =============================================================================
//  device_info.cu — CUDA 디바이스 프로브/이름
//
//  용도: C# 각 래퍼(CudaInterop / ColletNativeCuda / AutoFocusNativeCuda)가 기동 시 1회
//        "GPU 가 있는가 / 이름은?" 을 확인하는 가용성 프로브.
//  export:
//    cf_cuda_device_count / cf_cuda_device_name   (CudaInterop, Collet)
//    af_cuda_device_count / af_cuda_device_name   (AutoFocus)  — 동작 동일, 이름만 분리
// =============================================================================
#include "../include/qmc_cuda.h"

static int device_count_impl()
{
    int n = 0;
    if (cudaGetDeviceCount(&n) != cudaSuccess) return 0;
    return n;
}
static int device_name_impl(int device, char* buf, int bufLen)
{
    if (!buf || bufLen <= 0) return -1;
    cudaDeviceProp p;
    if (cudaGetDeviceProperties(&p, device) != cudaSuccess) return -2;
    strncpy(buf, p.name, bufLen - 1);
    buf[bufLen - 1] = '\0';
    return 0;
}

DLLEXPORT int cf_cuda_device_count()                     { return device_count_impl(); }
DLLEXPORT int cf_cuda_device_name(int d, char* b, int n) { return device_name_impl(d, b, n); }
DLLEXPORT int af_cuda_device_count()                     { return device_count_impl(); }
DLLEXPORT int af_cuda_device_name(int d, char* b, int n) { return device_name_impl(d, b, n); }
