# CDT-320 Vision — CUDA 네이티브 DLL 빌드 가이드 (장비 PC)

`native\` 아래 종류별 `.cu` 소스를 **한 번 컴파일해 통합 DLL 1개**(`QmcVisionCuda.dll`)를 만들고,
그것을 P/Invoke 3개 이름으로 배포한다. C#(QMC.Vision)은 DLL 함수를 "호출만" 하며, DLL 이 없으면
모든 알고리즘이 자동으로 CPU 로 폴백한다(안전).

---

## 1. 폴더/파일 구조와 명칭 정의

```
native\
  build_all.bat            ← 빌드 + 배포 (이거 하나만 실행하면 됨)
  QmcVisionCuda.vcxproj    ← (선택) Visual Studio 로 빌드할 때
  BUILD.md                 ← 이 문서
  include\
    qmc_cuda.h             공용 헤더 (DLLEXPORT 매크로, 공용 include)
  src\
    device_info.cu         [디바이스]   GPU 개수/이름 프로브        cf_/af_cuda_device_count·name
    device_memory.cu       [메모리]     디바이스 할당/복사          Allocate/Free/CopyHostToDevice/CopyDeviceToHost
    morphology.cu          [이물]       박스 모폴로지(Black-Hat)    cf_morph_box_u8
    edge_line.cu           [라인/에지]  측면라인 + Bottom 다이 4변  FindTopBottomLineCandidates, cf_find_die_edges
    stddev_filter.cu       [콜렛]       표준편차 텍스처 필터        cf_stddev_filter_cuda, cf_cuda_cleanup
    autofocus.cu           [오토포커스] 초점 점수                   af_focus_score_cuda
```

명칭 규칙: **`src\<종류>.cu`** = 한 파일 = 한 알고리즘군. 새 알고리즘을 GPU 화할 때는
`src\<새이름>.cu` 를 추가하고 `build_all.bat` 의 `SRCS=` 목록과 `.vcxproj` 의 `<CudaCompile>` 에만 등록하면 된다.

> 세 배포 DLL(MakePixelShiftImage / ColletFinderCuda / AutoFocusCuda)은 **내용이 같은 사본**이다.
> 통합 DLL 이 모든 export 를 포함하므로 어느 이름으로 로드돼도 전부 해석된다. (래퍼 C# 은 수정 불필요.)

---

## 2. 사전 준비 (최초 1회)

1. **Visual Studio 2022** — 설치 시 "**C++를 사용한 데스크톱 개발**" 워크로드 포함(cl.exe).
2. **CUDA Toolkit** (예: 12.x) 설치 — `nvcc` 포함. https://developer.nvidia.com/cuda-downloads
   (VS 설치 후 CUDA 를 설치하면 VS 통합이 자동으로 잡힌다.)
3. 장비(실행) PC 에는 **NVIDIA 그래픽 드라이버**만 있으면 됨(`--cudart static` 이라 cudart DLL 별도 배포 불필요).

확인:
```
nvcc --version
```
버전이 출력되면 준비 완료.

---

## 3. 빌드 순서 (권장: build_all.bat)

1. 시작 메뉴 → **Visual Studio 2022** → **x64 Native Tools Command Prompt for VS 2022** 실행.
   (일반 cmd 아님 — 반드시 "x64 Native Tools" 여야 cl.exe/nvcc 가 잡힌다.)

2. `native` 폴더로 이동:
   ```
   cd /d C:\Project\CDT-320\native
   ```

3. 빌드 + 배포 실행:
   ```
   build_all.bat
   ```
   내부 동작:
   - `[1/3]` `nvcc -O3 --shared --cudart static -arch=all-major -o QmcVisionCuda.dll src\*.cu`
   - `[2/3]` `..\QMC.Vision\NativeDeps\` 폴더 확인/생성
   - `[3/3]` `QmcVisionCuda.dll` 을 `MakePixelShiftImage.dll` / `ColletFinderCuda.dll` / `AutoFocusCuda.dll`
     3개 이름으로 `NativeDeps\` 에 복사
   - `[완료]` 메시지가 나오면 성공.

4. QMC.Vision 빌드(솔루션):
   ```
   set MSB="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
   %MSB% "C:\Project\CDT-320\QMC.Vision\QMC.Vision.csproj" /t:Build /p:Configuration=Debug
   ```
   빌드 로그에 아래가 보이면 자동 배포 정상:
   - `[Collet] ColletFinderCuda.dll → ...`
   - `[VisionInspector] native dll → ...`
   - `[AutoFocus] AutoFocusCuda.dll → ...`
   → `bin\Debug` 에 3개 DLL 이 복사된다.

### (대안) nvcc 수동 한 줄 — 배포 없이 DLL 만
```
nvcc -O3 --shared --cudart static -arch=all-major -o QmcVisionCuda.dll ^
  src\device_info.cu src\device_memory.cu src\morphology.cu ^
  src\edge_line.cu src\stddev_filter.cu src\autofocus.cu
```
그 뒤 `QmcVisionCuda.dll` 을 위 3개 이름으로 `..\QMC.Vision\NativeDeps\` 에 복사(build_all.bat 의 copy 부분).

### (대안) Visual Studio 로 빌드
`QmcVisionCuda.vcxproj` 를 솔루션에 추가 → `ImportGroup` 의 `CUDA 12.6.props/.targets` 를 설치 버전으로 수정
→ 구성 **x64 / Release** 빌드 → `x64\Release\QmcVisionCuda.dll` → 위와 같이 3개 이름 복사.

> `-arch=all-major` 는 주요 GPU 아키텍처 + PTX 를 모두 포함시킨다. 빠지면 **빌드 PC 와 다른 GPU**에서
> 커널이 `no kernel image` 로 조용히 실패하고 결과가 0 이 된다. 특정 GPU 만 쓸 거면
> `-gencode arch=compute_86,code=sm_86 -gencode arch=compute_86,code=compute_86` 식으로 축소 가능.

---

## 4. 동작 확인

QMC.Vision 실행 후:
- 진단 로그: `[AutoFocusCore] CUDA detect: device=True(...), focusKernel=True`
- INSPECT 결과: `Foreign Backend = Cuda`
- 상태줄: `GpuBackend.StatusLine = "CUDA: <device 이름>"`

DLL 이 없거나 GPU 가 없으면 위가 모두 `Cpu` 로 표시되고 **결과는 동일**(속도만 차이).

---

## 5. 결과 동등성 회귀 (최초 1회 권장)

CUDA 는 "속도만" 높이는 것이 목표다. 같은 입력으로 **DLL 없이(CPU) / 있게(GPU)** 검사해
결과(모폴로지·라인·다이에지·표준편차·초점)가 픽셀/수치 단위로 동일한지 확인한다.
불일치 시 해당 `src\*.cu` 커널의 경계조건을 CPU 정답지(각 파일 상단의 "대응 CPU")에 맞춰 조정한다.

---

## 6. DLL 갱신 흐름 (알고리즘 .cu 수정 시)

1. `src\<종류>.cu` 수정
2. `native` 에서 `build_all.bat` 재실행 → `NativeDeps\` 3개 DLL 갱신
3. QMC.Vision 재빌드 → 출력 폴더 자동 반영
