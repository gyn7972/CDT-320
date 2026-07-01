# CDT-320 Vision — CUDA 통합 가이드 / 네이티브 계약

> 원칙: **"CUDA 가 사용 가능하면 CUDA, 아니면 CPU 폴백."**
> 장비 PC(CUDA DLL 보유)에서는 GPU 경로가 자동으로 켜지고, 개발 PC(미보유)에서는 모든 호출이
> 예외 없이 CPU 로 우회된다. 한 줄의 코드 변경 없이 같은 빌드가 두 환경에서 동작한다.

---

## 1. 참고: Collet Finder(장비 PC 검증) 가 한 방식

`docs/Collet Finder` 프로젝트(장비 PC 테스트 완료)의 CUDA 구조를 그대로 차용했다.

```
ColletFinder/            # C# WinForms
  NativeCuda.cs          # P/Invoke 래퍼
  StdDevFilter.cs        # ComputeBackend{Cpu,Cuda} + CudaAvailable + CPU 폴백
ColletFinderCuda/        # 네이티브 CUDA 프로젝트(.cu) → ColletFinderCuda.dll
```

핵심 패턴(NativeCuda.cs 의 export 시그니처, 인덱스 DB 에서 확인):

| export | 시그니처 | 용도 |
|---|---|---|
| `cf_cuda_device_count` | `int cf_cuda_device_count()` | GPU 개수(가용성 프로브) |
| `cf_cuda_device_name` | `int cf_cuda_device_name(int index, byte* buf, int bufLen)` | 디바이스 이름 |
| `cf_stddev_filter_cuda` | `int cf_stddev_filter_cuda(byte* src,int w,int h, int rx,int ry,int rw,int rh, int win, double k, byte* dst)` | 표준편차 필터(GPU) |

C# 쪽은 `StdDevFilter.Apply(bitmap, roi, win, k, preferCuda, out ComputeBackend used, out byte[] gray)` 처럼
**"가능하면 CUDA, 결과로 어떤 백엔드를 썼는지 보고"** 한다. 320 도 동일하게 구성했다.

---

## 2. 320 의 현재 구조 (이번에 추가)

```
QMC.Vision/Equipment/Core/
  CudaInterop.cs   # P/Invoke. DLL=MakePixelShiftImage.dll. 익스포트 없으면 기능별 캐시로 CPU 고정
  GpuBackend.cs    # ComputeBackend{Cpu,Cuda} + CudaAvailable + DeviceName + 알고리즘별 Last() 상태
```

- `GpuBackend.CudaAvailable` / `GpuBackend.DeviceName` / `GpuBackend.StatusLine` 으로 상태 노출.
- 각 알고리즘은 GPU 시도 후 `GpuBackend.Note("<알고리즘>", Cpu|Cuda)` 로 실제 백엔드를 기록.
- 안전장치: `CudaInterop` 의 모든 GPU 호출은 try/catch + **기능별 가용성 캐시**(`_avail`, `_morph`).
  DLL 부재(`DllNotFoundException`) 또는 익스포트 부재(`EntryPointNotFoundException`) 시 한 번 실패하면
  이후 그 기능은 CPU 로 고정 → 반복 예외 없음.

### 알고리즘별 CUDA 상태 (2026-06-30 갱신 — 통합 DLL + 후크 일원화)

> **권장 배포**: `native\build_all.bat` 로 통합 DLL(`QmcVisionCuda.dll`)을 빌드하면
> 아래 모든 export 가 들어간 DLL 1개가 나온다. build.bat 가 그것을 3개 이름
> (`MakePixelShiftImage.dll` / `ColletFinderCuda.dll` / `AutoFocusCuda.dll`)으로
> `QMC.Vision\NativeDeps\` 에 배포 → csproj Copy 타깃이 출력 폴더로 자동 복사.

| 알고리즘 | 위치 | C# 후크 | 폴백(CPU) | 네이티브 export |
|---|---|---|---|---|
| 이물(Black-Hat 모폴로지) | `ContaminationDetector` (Bottom+Side 공용) | ✅ 연결됨 | 분리형 O(w·h) | `cf_morph_box_u8` ✅통합DLL |
| 측면 라인 검출 | `SideChippingCore.GetLineCandidates` | ✅ 연결됨 | 310 FindLine 교차 | `FindTopBottomLineCandidates` ✅통합DLL |
| **Bottom 다이 4변 검출** | `BottomInspector.FindDie` | ✅ **이번 연결** | Parallel.For 교차 | `cf_find_die_edges` ✅통합DLL(신규) |
| 표준편차 필터(콜렛/플랫콜렛) | `Collet.StdDevFilter` / `FlatColletFinder` | ✅ 연결됨 | SAT 멀티스레드 | `cf_stddev_filter_cuda` ✅통합DLL |
| 오토포커스 점수 | `AutoFocusCore.ScoreGray` | ✅ 연결됨 | 8-이웃 라플라시안 | `af_focus_score_cuda` ✅통합DLL |
| 배치/갭 | `PlacementGapInspector` | ⬜ CPU 유지(설계) | 현행 CPU | 소형 ROI·순차집계라 GPU 이득 낮음 |
| 정렬각 추정 | `AlignAngleEstimator` | ⬜ CPU 유지(설계) | 현행 CPU | 소형·저비용 |

✅ = C# 후크 연결 완료(통합 DLL export 존재 시 자동 GPU, 없으면 CPU).
⬜ = 의도적 CPU 유지(ROI 가 작아 H2D 전송비용 > 연산이득). 필요 시 동일 패턴으로 후크 추가 가능.

> ⚠️ **중요(현황)**: 기존 `NativeDeps\MakePixelShiftImage.dll` 은
> `cf_morph_box_u8` / `FindTopBottomLineCandidates` / `cf_find_die_edges` export 가 **없어서**
> 이물·측면라인·Bottom 다이 경로가 그동안 **조용히 CPU 로 폴백**해 왔다(실제 GPU 는 콜렛뿐).
> 통합 DLL 로 교체해야 위 경로가 실제 GPU 로 동작한다.

---

## 3. 장비 PC DLL 이 제공해야 할 네이티브 계약

`MakePixelShiftImage.dll`(또는 동등 DLL, 같은 이름) 에 아래를 `extern "C" __declspec(dllexport)`,
`__cdecl` 로 추가하면 320 이 자동으로 GPU 를 사용한다. **모든 버퍼는 host(pinned) 포인터, 8bit gray, row-major.**
**성공 시 0, 실패 시 0이외** 를 반환한다(0이외면 320 이 CPU 로 폴백).

```c
// (선택) 디바이스 이름 — GpuBackend.DeviceName 표시용. 미구현이어도 무방.
extern "C" __declspec(dllexport)
int __cdecl cf_cuda_device_name(int index, unsigned char* buf, int bufLen);

// 박스(정사각 SE) 그레이 모폴로지. radius=반경(SE=2r+1). isMax!=0 → dilate(최대), 0 → erode(최소).
//   - 경계는 clamp(이미지 밖은 윈도우에서 제외) — CPU 분리형과 동일 결과여야 함.
//   - Black-Hat = Erode(Dilate(src)) - src 형태로 320 이 두 번 호출(닫힘) 후 차분/임계.
extern "C" __declspec(dllexport)
int __cdecl cf_morph_box_u8(const unsigned char* src, unsigned char* dst,
                            int width, int height, int radius, int isMax);
```

### (이미 보유 — 변경 불필요)
```c
int AllocateDeviceMemory(void** devPtr, unsigned long long size);
int FreeDeviceMemory(void* devPtr);
int CopyHostToDevice(void* dst, const void* src, unsigned long long size);
int CopyDeviceToHost(void* dst, const void* src, unsigned long long size);
int FindTopBottomLineCandidates(void* d_input, int w, int h, unsigned char thr,
                                int topStartY,int topEndY,int botStartY,int botEndY,
                                void* d_topCandidates, void* d_bottomCandidates);
```

### (확장 후보 — 추가 시 320 후크만 붙이면 연결)
```c
// 좌/우 라인 후보(각 행의 좌/우 첫 교차 x, -1=없음) — Bottom 4변 검출 GPU 화
int __cdecl cf_find_left_right_candidates(const unsigned char* src,int w,int h,unsigned char thr,
                                          int leftStartX,int leftEndX,int rightStartX,int rightEndX,
                                          int* d_left,int* d_right);
// ROI 포커스 메트릭(분산/선명도) — AutoFocus GPU 화
int __cdecl cf_focus_metric(const unsigned char* src,int w,int h,int rx,int ry,int rw,int rh,double* outMetric);
```

---

## 4. 검증 / 운용 체크리스트

1. **개발 PC**: CUDA DLL/익스포트 없음 → INSPECT 결과의 `Foreign Backend` = `Cpu`, 정상 동작(분리형 모폴로지).
2. **장비 PC**: DLL 이 `cf_morph_box_u8` 제공 → `Foreign Backend` = `Cuda`, GPU 가속.
3. GPU/CPU **결과 동일성**: `cf_morph_box_u8` 는 경계 clamp 박스 모폴로지여야 하며,
   320 CPU 분리형(`ContaminationDetector.SepMorph`)과 픽셀 단위로 일치해야 한다(회귀 시 기준).
4. 상태 표시: `GpuBackend.StatusLine` ("CUDA: <device>" / "CPU") 를 시작 로그·진단 화면에 노출 권장.

> 주의: CUDA 는 **결과를 바꾸지 않고 속도만** 높이는 것이 목표다. 알고리즘/파라미터는 CDT-310 기준을 그대로 유지하고,
> GPU 커널도 동일 수식/경계조건을 구현해야 한다(이 저장소의 CPU 경로가 정답지 역할).
