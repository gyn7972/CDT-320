// =============================================================================
//  device_memory.cu — 디바이스 메모리 헬퍼
//
//  용도: CudaInterop 이 측면 라인 검출(edge_line.cu 의 FindTopBottomLineCandidates)에
//        디바이스 포인터를 직접 넘길 때 사용한다(할당→업로드→커널→다운로드→해제).
//  export: AllocateDeviceMemory / FreeDeviceMemory / CopyHostToDevice / CopyDeviceToHost
//          반환 0=성공, 0이외=실패(C# 폴백).
// =============================================================================
#include "../include/qmc_cuda.h"

DLLEXPORT int AllocateDeviceMemory(void** devPtr, unsigned long long size)
{
    if (!devPtr) return -1;
    *devPtr = 0;
    return (cudaMalloc(devPtr, (size_t)size) == cudaSuccess) ? 0 : -2;
}
DLLEXPORT int FreeDeviceMemory(void* devPtr)
{
    if (!devPtr) return 0;
    return (cudaFree(devPtr) == cudaSuccess) ? 0 : -1;
}
DLLEXPORT int CopyHostToDevice(void* dst, const void* src, unsigned long long size)
{
    return (cudaMemcpy(dst, src, (size_t)size, cudaMemcpyHostToDevice) == cudaSuccess) ? 0 : -1;
}
DLLEXPORT int CopyDeviceToHost(void* dst, const void* src, unsigned long long size)
{
    return (cudaMemcpy(dst, src, (size_t)size, cudaMemcpyDeviceToHost) == cudaSuccess) ? 0 : -1;
}
