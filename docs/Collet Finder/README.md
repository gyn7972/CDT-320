# Collet Finder

ROI 국소 **표준편차 텍스처 필터** 알고리즘 테스트 도구 (C# / WinForms, .NET Framework 4.7.2).

영상에서 ROI를 지정하고, ROI 내 모든 픽셀을 중심으로 한 블럭(정사각형) 안에서
밝기의 표준편차를 구해 **임계값 이상이면 흰색, 미만이면 검정**으로 이진화한 결과 이미지를 만든다.
표면 텍스처(거칠기) 유무로 영역을 구분하는 용도(예: 콜렛 검출)에 쓴다.

이진화 후에는 **연결요소(블랍)를 찾아 가장 큰 블랍의 최소면적 외접 사각형**을 구해
**4변·중심 위치·각도**를 검출하고, 원본/결과 위에 오버레이로 표시한다(별도 입력값 없음).

## 처리 백엔드 (자동 선택)
- **CUDA(GPU)** — `ColletFinderCuda.dll`(CUDA 네이티브)이 있고 NVIDIA GPU가 감지되면 GPU로 처리.
- **CPU(폴백)** — CUDA 미지원/실패 시 자동으로 CPU 처리. **논리 코어 수만큼 멀티스레드**(`Parallel.For`)로 분산.

실행 시 상단의 `CUDA 사용` 체크박스로 강제 CPU/GPU 전환이 가능하며(미지원 PC에서는 비활성),
상태바에 감지된 GPU·CPU 스레드 수와 실제 사용된 백엔드/소요 시간이 표시된다.
두 백엔드는 **동일한 결과(비트 단위 일치)** 를 내도록 구현·검증되어 있다.

## 사용 방법
1. **이미지 열기** — png/jpg/bmp/tif 등을 불러온다.
2. 원본 위에서 마우스로 **드래그하여 ROI를 지정**한다. (지정하지 않으면 전체 이미지를 처리)
3. **블럭 크기(px)** — 각 픽셀을 중심으로 한 정사각형 한 변의 길이.
4. **임계값(표준편차)** — 0~255. 국소 표준편차가 이 값 이상이면 흰색.
5. **처리 실행** — 우측에 결과 이미지가 표시되고, 가장 큰 블랍에 맞춘 사각형(초록 4변,
   노란 꼭짓점, 빨간 중심)이 원본·결과에 겹쳐 표시된다. 상태바에 **중심 좌표·각도·크기·면적**이 출력된다.
6. **결과 저장** — png/bmp/jpg 로 저장(검출 오버레이 포함).

## 알고리즘
- 밝기 = 0.299R + 0.587G + 0.114B (휘도 근사).
- 각 픽셀의 블럭 분산 = E[x²] − (E[x])², 표준편차 = √분산 (모표준편차).
- **적분영상(Summed-Area Table)** 으로 블럭 합·제곱합을 픽셀당 O(1)에 계산 →
  블럭 크기가 커져도 속도가 일정하다. CPU·GPU 모두 동일 방식.
- 경계 픽셀의 블럭은 이미지 범위로 클램프하여 처리한다.
- 결과는 원본과 같은 크기이며, ROI 밖은 검정으로 채운다.

### 블랍 → 사각형 검출 (`BlobRectFinder.cs`)
1. 이진 마스크에서 **연결요소 라벨링(8-이웃)** 으로 블랍들을 찾고 **가장 큰 블랍**을 선택.
2. 그 블랍의 행별 좌/우 극점만 모아(볼록껍질 후보 축소) **볼록껍질**(Andrew monotone chain) 계산.
3. **회전 캘리퍼스**로 최소면적 외접 사각형 → **4개 꼭짓점·중심·각도**(긴 변의 수평 기준, −90~90°)·크기.
- 텍스처 검출 특성상 마스크는 실제 형상보다 블럭 폭(±블럭/2)만큼 커지므로, 검출 크기도 그만큼 여유가 있다.
  (중심·각도는 영향 없음.)

## 구성
```
Collet Finder/
  ColletFinder.sln            솔루션
  ColletFinder/               C# WinForms 앱
    StdDevFilter.cs           표준편차 필터 + 백엔드 선택(GPU/CPU) + CPU 멀티스레드
    BlobRectFinder.cs         블랍 검출 + 최소면적 사각형(중심/각도/4변)
    NativeCuda.cs             ColletFinderCuda.dll P/Invoke 선언
    MainForm.cs / .Designer.cs   UI
  ColletFinderCuda/           CUDA 네이티브 프로젝트
    ColletFinderCuda.cu       커널(적분영상 + 표준편차) 및 export 함수
    ColletFinderCuda.vcxproj  Visual Studio 프로젝트(v143 툴셋, x64) — 솔루션에 포함됨
    build.bat                 nvcc 직접 빌드 스크립트(VS IDE 없이 빌드용 대안)
```

`ColletFinder.sln` 에는 C# 앱과 CUDA 프로젝트가 모두 포함되며, C# 앱은 CUDA 프로젝트에 빌드
의존성이 걸려 있어 **솔루션 빌드 시 CUDA(.dll) → C# 순서로 빌드되고 DLL이 exe 옆으로 자동 복사**된다.

## 빌드
### 권장: Visual Studio 2022 로 솔루션 빌드
`ColletFinder.sln` 을 **Visual Studio 2022** 로 열고 솔루션 빌드(Ctrl+Shift+B).
CUDA 프로젝트(x64) → C# 앱(AnyCPU) 순으로 빌드되고 `ColletFinderCuda.dll` 이 exe 옆으로 자동 복사된다.

- CUDA 프로젝트는 **v143 툴셋(x64)** 을 사용한다. CUDA 12.9 의 VS 통합이 설치된 VS에서 빌드해야 한다.
  이 PC에서는 **VS2022 Professional** 에 통합이 설치되어 있다(VS2026/v18 에는 없음 → VS2022로 빌드).
- 정적 CUDA 런타임으로 링크되어 `cudart` DLL 의존성이 없다(자체 완결). NVIDIA 드라이버만 있으면 실행된다.
- CUDA 버전이 다르면 `.vcxproj` 의 `CUDA 12.9.props` / `CUDA 12.9.targets` 두 import 줄을 해당 버전으로 바꾼다.

### 대안: VS IDE 없이 DLL만 빌드
CUDA 프로젝트를 IDE로 빌드하기 어려우면(예: VS2026만 있는 경우) 다음으로 DLL만 만든다:
```
Collet Finder\ColletFinderCuda\build.bat
```
- nvcc 를 직접 호출하며 vswhere 로 CUDA 호환 VS(2017~2022)를 찾아 사용한다(VS2022 우선).
- 생성된 DLL 은 C# 출력 폴더로 자동 복사된다. 이후 C# 앱만 따로 빌드하면 된다.

### MSBuild(명령줄)
```
"C:\...\Visual Studio\2022\...\MSBuild.exe" "Collet Finder\ColletFinder.sln" /p:Configuration=Release
```

> C# 앱은 외부 의존성이 없다. `ColletFinderCuda.dll` 이 exe 옆에 있으면 GPU, 없으면 CPU 폴백.
> 64비트 프로세스로 실행해야 CUDA DLL(x64)과 연동된다(AnyCPU + Prefer32Bit=false).
