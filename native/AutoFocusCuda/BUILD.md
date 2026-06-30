# AutoFocusCuda.dll 빌드 가이드

오토포커스 GPU 가속용 **네이티브 CUDA DLL**. C#(QMC.Vision)은 이 DLL의 함수를 P/Invoke로 호출만 한다.
이 DLL이 없으면 C#은 자동으로 CPU 경로로 폴백한다(정상). DLL을 빌드해 배포하면 C# 변경 없이 GPU로 전환된다.

## 0. 왜 별도 DLL인가
- C# 코드(`AutoFocusCore`, `AutoFocusNativeCuda`)는 **관리코드(.NET)** 라 GPU 커널을 직접 못 돈다.
- GPU 연산(CUDA 커널)은 **C++/CUDA로 컴파일된 네이티브 DLL** 안에 있어야 한다.
- C#은 `[DllImport("AutoFocusCuda.dll")]`로 그 DLL의 함수를 호출한다(이게 P/Invoke).
- 즉 `AutoFocusCuda.dll`은 **이 C# 솔루션 빌드와 별개로**, CUDA 컴파일러(nvcc)로 따로 만든다.

## 1. 필요한 것 (빌드 PC)
- **Visual Studio** (C++ 데스크톱 개발 워크로드 — cl.exe).
- **CUDA Toolkit** (예: 12.x) — `nvcc` 포함. https://developer.nvidia.com/cuda-downloads
- (둘 다 설치하면 `nvcc`가 VS의 cl.exe를 자동으로 사용)

## 2. 빌드 (가장 간단: 명령 한 줄)
"x64 Native Tools Command Prompt for VS"를 열고 이 폴더에서:

```
nvcc -O3 --shared --cudart static -o AutoFocusCuda.dll af_focus.cu
```

- `--shared` : DLL 생성
- `--cudart static` : CUDA 런타임을 DLL 안에 정적 포함 → 배포 시 `cudart64_*.dll` 따로 안 넣어도 됨
- 결과물: `AutoFocusCuda.dll`

(원하면 Visual Studio에 "CUDA Runtime" 프로젝트 템플릿으로 만들어 .cu를 추가하고, 구성을 x64 / Dynamic Library(.dll)로 빌드해도 된다.)

## 3. 배포 (장비 PC)
- `AutoFocusCuda.dll`을 **`QMC.Vision.exe`와 같은 폴더**(예: `bin\Debug`)에 복사.
- 장비 PC에 **NVIDIA 드라이버**가 설치되어 있어야 함(빌드한 CUDA 툴킷 버전과 호환).

## 4. 동작 확인
- QMC.Vision 실행 후 오토포커스 1회 사용 → 통신 로그에:
  - `device=True(...), focusKernel=True → CUDA 사용 가능` 이면 GPU 경로 동작.
  - `focusKernel=False` 면 DLL의 export 이름/시그니처가 안 맞는 것(아래 계약 확인).
- 택타임 로그의 `backend=Cuda` 로도 확인.

## 5. P/Invoke 계약 (반드시 일치)
C# `AutoFocusNativeCuda` 와 정확히 같아야 한다 (이름/인자/Cdecl):

| C# 선언 | 네이티브 export (extern "C", __declspec(dllexport)) |
|---|---|
| `int af_cuda_device_count()` | `int af_cuda_device_count()` |
| `int af_cuda_device_name(int, byte[], int)` | `int af_cuda_device_name(int device, char* buf, int bufLen)` |
| `int af_focus_score_cuda(byte[] gray, int w, int h, int objThreshold, double marginFraction, out double score)` | `int af_focus_score_cuda(const unsigned char* gray, int w, int h, int objThreshold, double marginFraction, double* score)` |

- 반환 0 = 성공, 음수 = 실패(C#이 CPU로 폴백).
- 알고리즘은 CPU `AutoFocusCore.ScoreFocus` 와 동일(8-이웃 라플라시안, objThreshold 게이트, margin 제외, 응답 상위 200 평균) — 그래야 CPU/GPU 결과가 같다.

## 6. 성능 튜닝(선택)
`af_focus.cu`의 현재 구현은 매 호출 cudaMalloc/Memcpy 하는 단순 버전이다. 더 빠르게 하려면:
- 디바이스 버퍼/히스토그램을 재사용(매번 malloc 안 함),
- 핀드 메모리(cudaHostAlloc)로 H2D 전송 가속,
- 블록 단위 shared-memory 히스토그램 후 합산,
- 스트림으로 다음 ROI 전송과 겹치기.
