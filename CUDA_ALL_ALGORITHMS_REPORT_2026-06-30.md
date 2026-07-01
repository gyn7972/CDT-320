# CDT-320 Vision — 모든 알고리즘 CUDA 실행 + DLL 생성/자동추가 작업 보고

작성일: 2026-06-30 · 대상: `C:\Project\CDT-320` (QMC.Vision)

---

## 1. 한 줄 요약

모든 Vision 알고리즘이 "**DLL 만 있으면 자동 GPU, 없으면 CPU**" 로 동작하도록 **C# 후크를 일원화**하고,
흩어져 있던 3개 CUDA DLL 을 **하나의 소스(`qmc_vision_cuda.cu`)로 통합** + **빌드/자동배포 스크립트**를 추가했다.
DLL 이 없어도 빌드·실행에 지장 없으며(안전 폴백), DLL 을 `NativeDeps\` 에 두면 빌드 시 출력 폴더로 자동 복사된다.

> ⚙️ 이 환경(리눅스 샌드박스)에서는 .NET Framework/CUDA 컴파일이 불가하여 **빌드/실행 검증은 수행하지 못했다.**
> Windows 빌드 PC 에서 4·6절의 절차로 빌드·회귀 검증이 필요하다. C# 변경은 기존에 컴파일되던 패턴을 그대로 따랐다.

---

## 2. 작업 전 현황 진단 (중요)

코드에는 이미 "가능하면 CUDA, 아니면 CPU 폴백" 구조(`GpuBackend` / `CudaInterop` / 각 `*NativeCuda`)가 있었으나,
**배포된 `NativeDeps\MakePixelShiftImage.dll` 에 실제 export 가 빠져 있어** 다음 경로가 조용히 CPU 로만 돌고 있었다:

| 알고리즘 | 기대 export | 배포 DLL 보유? | 실제 동작 |
|---|---|---|---|
| 이물 Black-Hat | `cf_morph_box_u8` | ❌ 없음 | CPU(조용히 폴백) |
| 측면 라인 | `FindTopBottomLineCandidates` | ❌ 없음 | CPU |
| 디바이스 메모리 | `AllocateDeviceMemory` 등 | ❌ 없음 | (프로브 실패→전체 CPU 고정) |
| 콜렛 표준편차 | `cf_stddev_filter_cuda` | ✅ ColletFinderCuda.dll | **GPU(정상)** |
| 오토포커스 | `af_focus_score_cuda` | ❌ AutoFocusCuda.dll 미배포 | CPU |

즉 **실제 GPU 가속은 콜렛 1개뿐**이었다. 이번 작업으로 통합 DLL 을 빌드/배포하면 위 전부가 GPU 로 전환된다.

---

## 3. 변경/추가 파일

### 신규 (네이티브 — C# 빌드에 영향 없음). 종류별 `.cu` 로 분리, 한 번 빌드해 통합 DLL 1개
- `native/src/device_info.cu` — [디바이스] `cf_/af_cuda_device_count/name`
- `native/src/device_memory.cu` — [메모리] `AllocateDeviceMemory/FreeDeviceMemory/CopyHostToDevice/CopyDeviceToHost`
- `native/src/morphology.cu` — [이물] `cf_morph_box_u8` (Black-Hat 박스 모폴로지)
- `native/src/edge_line.cu` — [라인/에지] `FindTopBottomLineCandidates`(측면라인) + `cf_find_die_edges`(Bottom 4변, 신규)
- `native/src/stddev_filter.cu` — [콜렛] `cf_stddev_filter_cuda` (+ `cf_cuda_cleanup`)
- `native/src/autofocus.cu` — [오토포커스] `af_focus_score_cuda`
- `native/include/qmc_cuda.h` — 공용 헤더(DLLEXPORT 매크로/공용 include)
- `native/build_all.bat` — src\*.cu 전체 nvcc 빌드 → `NativeDeps\` 로 3개 이름 자동 배포
- `native/QmcVisionCuda.vcxproj` — Visual Studio CUDA 프로젝트(선택, 전체 .cu 참조)
- `native/BUILD.md` — 장비 PC 빌드 순서/구조/명칭 정의 가이드

### 수정 (C# — 추가/폴백형, 기존 동작 불변)
- `QMC.Vision/Equipment/Core/CudaInterop.cs` — `cf_find_die_edges` P/Invoke + `TryFindDieEdges()` 추가(기존 `TryBoxMorph` 패턴 그대로)
- `QMC.Vision/Equipment/Core/BottomInspector.cs` — `FindDie()` 가 GPU 먼저 시도, 실패 시 기존 `Parallel.For` 폴백. `GpuBackend.Note("BottomDie", …)` 기록
- `QMC.Vision/QMC.Vision.csproj` — `CopyAutoFocusCuda` 타깃 추가(AutoFocusCuda.dll 자동 복사)
- `QMC.Vision/NativeDeps/README.txt`, `docs/CUDA_Integration.md` — 통합 DLL/현황 반영

> 설계상 CPU 유지: `PlacementGapInspector`(작은 ROI·순차 갭집계), `AlignAngleEstimator`(소형·저비용).
> H2D 전송비용이 연산이득을 넘어 GPU 화 이득이 낮다. 필요 시 동일 후크 패턴으로 확장 가능.

---

## 4. DLL 생성 방법 (요청 1: "dll 생성 방법")

### 방법 A — nvcc 한 줄 (가장 간단)
"x64 Native Tools Command Prompt for VS 2022" 에서 `native\` 로 이동 후:
```
nvcc -O3 --shared --cudart static -arch=all-major -o QmcVisionCuda.dll ^
  src\device_info.cu src\device_memory.cu src\morphology.cu ^
  src\edge_line.cu src\stddev_filter.cu src\autofocus.cu
```
- `--shared` DLL 생성 · `--cudart static` CUDA 런타임 정적포함(의존 DLL 불필요)
- `-arch=all-major` 주요 GPU arch + PTX 포함 → **빌드 PC 와 다른 GPU 에서도 동작**(빠지면 `no kernel image` 로 조용히 실패)
- 필요한 것: VS C++ 빌드도구(cl.exe) + CUDA Toolkit(nvcc). 실행 PC 는 NVIDIA 드라이버만.

### 방법 B — `build_all.bat` (A + 자동배포, 권장)
```
native\build_all.bat
```
컴파일 후 결과를 `QMC.Vision\NativeDeps\` 에 `MakePixelShiftImage.dll`, `ColletFinderCuda.dll`,
`AutoFocusCuda.dll` 3개 이름으로 복사한다(내용 동일·모든 export 포함이라 어느 이름으로 로드돼도 해석됨).

### 방법 C — Visual Studio CUDA 프로젝트
`QmcVisionCuda.vcxproj` 를 솔루션에 추가. `CUDA <ver>.props/.targets` 경로를 설치 버전으로 맞추고
x64/Release 빌드 → `x64\Release\QmcVisionCuda.dll`. (CI/솔루션 통합 빌드를 원할 때)

| | A: nvcc 한 줄 | B: build_all.bat | C: vcxproj |
|---|---|---|---|
| 난이도 | 낮음 | 낮음 | 중간 |
| 자동 배포 | ✗ | ✅ | ✗(별도 복사) |
| 솔루션 통합 | ✗ | ✗ | ✅ |
| 권장 | 빠른 테스트 | **일반 배포** | CI |

---

## 5. DLL 자동 추가 방법 (요청 2: "dll을 자동으로 추가하는 방법")

### 방법 ① MSBuild Copy 타깃 — `NativeDeps\` → 출력 폴더 (**현재 채택·권장**)
`QMC.Vision.csproj` 에 `AfterTargets="Build"` Copy 타깃이 있어, `NativeDeps\` 에 DLL 을 두기만 하면
빌드할 때마다 `bin\Debug`(또는 Release)로 자동 복사된다. 이번에 `CopyAutoFocusCuda` 타깃을 추가해
3개 DLL(MakePixelShiftImage / ColletFinderCuda / AutoFocusCuda) 모두 커버한다.
→ **개발자는 `NativeDeps\` 에 DLL 만 갱신**하면 끝(수동 복사 불필요). DLL 없으면 자동 skip(CPU 폴백).

### 방법 ② 빌드 시 nvcc 자동 컴파일 (선택 — nvcc 있는 PC 만)
빌드 PC 에 nvcc 가 있으면 `BeforeTargets="Build"` 에서 .cu 를 자동 컴파일하도록 할 수 있다.
(dev PC 빌드 속도/환경 편차 우려로 기본 미적용. 필요 시 csproj 에 아래 추가)
```xml
<Target Name="BuildQmcVisionCuda" BeforeTargets="Build"
        Condition="Exists('$(ProjectDir)..\native\src\autofocus.cu')">
  <Exec Condition="'$(NvccAvailable)'!='false'"
        ContinueOnError="true"
        WorkingDirectory="$(ProjectDir)..\native"
        Command="nvcc -O3 --shared --cudart static -arch=all-major -o &quot;$(ProjectDir)NativeDeps\MakePixelShiftImage.dll&quot; src\device_info.cu src\device_memory.cu src\morphology.cu src\edge_line.cu src\stddev_filter.cu src\autofocus.cu" />
  <!-- 이어서 ColletFinderCuda.dll / AutoFocusCuda.dll 로 copy -->
</Target>
```

### 방법 ③ NuGet 네이티브 런타임 패키지 (배포 표준화 시)
OpenCvSharp 처럼 `runtimes/win-x64/native/QmcVisionCuda.dll` 구조의 사내 NuGet 패키지로 만들면
`PackageReference` 만으로 복원·복사가 자동화된다(여러 솔루션이 공유할 때 유리). 초기 구성비용 있음.

### 방법 ④ Post-build event
csproj/프로젝트 속성의 빌드 후 이벤트에 `copy /Y …\NativeDeps\*.dll "$(TargetDir)"`. 타깃 방식(①)과
기능 동일하나 조건/메시지 제어가 약해 ① 을 권장.

### 방법 ⑤ 런타임 디렉터리 지정 (DLL 을 옮기지 않고 로드)
앱 시작 시 `SetDllDirectory` / `AddDllDirectory` 로 DLL 폴더를 검색경로에 추가하면 출력폴더 복사 없이
지정 폴더에서 P/Invoke 가 로드된다(공용 드라이버 폴더 등). 배포 단순화용 대안.

**정리/권장**: 일반 운용은 **① (이미 적용)** 으로 충분하다. CI 자동화까지 원하면 **②** 또는 **③** 을 추가.

---

## 6. 빌드 PC 에서 할 일 (체크리스트)

1. (이 저장소가 V:\Source 기준이면 거기서) Windows 빌드 PC 로 동기화.
2. CUDA Toolkit + VS2022 C++ 워크로드 설치 확인 → `native\build_all.bat` 실행.
   - `QMC.Vision\NativeDeps\` 에 3개 DLL 생성 확인.
3. 솔루션 빌드(CLAUDE.md 의 MSBuild 명령). C# 변경 컴파일 확인.
   - 빌드 로그에 `[AutoFocus] AutoFocusCuda.dll → …`, `[Collet] … → …` 메시지 확인.
4. 실행 후 진단:
   - `[AutoFocusCore] CUDA detect: device=True(...), focusKernel=True`
   - INSPECT 결과 `Foreign Backend = Cuda`, `GpuBackend.StatusLine = CUDA: <device>`
5. **CPU/GPU 결과 동등성 회귀**(필수): 같은 입력으로 DLL 없이(CPU)·있게(GPU) 검사 → 결과 동일 확인.
   - 모폴로지/라인/다이에지/표준편차/초점 모두 "수치 동일, 속도만 차이" 가 정상.
   - 불일치 시 해당 커널의 경계조건을 CPU 정답지에 맞춰 조정(커널은 CPU 코드를 1:1 이식).

---

## 7. 위험/주의

- 본 환경에서 **컴파일·실행 검증 미수행**. 4·6절 빌드/회귀가 검증 게이트다.
- 통신 와이어 포맷·기존 DLL 이름은 **변경하지 않았다**(메모리 규칙 준수). 통합 DLL 은 같은 이름들로 배포된다.
- 기존 `MakePixelShiftImage.dll`(754KB, export 누락본)은 통합 DLL 로 **덮어써야** GPU 가 켜진다.
- 작업/빌드 기준 저장소가 `V:\Source` 라면 이 편집을 그쪽으로 반영해야 동기화로 되돌려지지 않는다.
