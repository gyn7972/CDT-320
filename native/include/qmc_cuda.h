// =============================================================================
//  qmc_cuda.h — CDT-320 Vision CUDA 네이티브 공용 헤더
//
//  모든 src\*.cu 가 이 헤더를 include 한다. export 매크로/공용 include 를 한곳에 모은다.
//  DLL 계약(P/Invoke 시그니처)은 각 .cu 상단 주석과 native\BUILD.md 표를 참조.
//
//  ※ 모든 export 는 extern "C" + __declspec(dllexport) + __cdecl(기본) — P/Invoke Cdecl 과 일치.
//    버퍼는 8bit grayscale, row-major. 성공=0, 실패=0이외(→ C# 이 CPU 로 폴백).
// =============================================================================
#ifndef QMC_CUDA_H
#define QMC_CUDA_H

#define _CRT_SECURE_NO_WARNINGS
#include <cuda_runtime.h>
#include <math.h>
#include <string.h>
#include <stdint.h>

#define DLLEXPORT extern "C" __declspec(dllexport)

#endif // QMC_CUDA_H
