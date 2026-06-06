# Stage 28 — InputStageUnit 시퀀스 통합

## 1. 현황 (Phase 1 조사)

InputStageUnit 의 5개 시퀀스 메서드:
- `LoadAndPrepareWaferAsync` — Stage 27 GAP-3 fix 에서 호출 추가됨
- `VisionAlignAndSetupOriginAsync` — **호출 0건**
- `WaitForUserConfirmAsync` — 호출 0건 (auto-cycle 에선 패스 가능)
- `MultiScanAndPickupAsync` — **호출 0건**
- `UnloadWaferAsync` — **호출 0건**

축 6개: `StageY` / `StageT` / `ExpanderZ` / `CameraX` / `NeedleBlockX` / `NeedleZ`
DO 1개: `NeedleVacuum`

NullObject 5개 주입:
- `NullWaferLoader` — IsFeederAtSafePosition 항상 true (실 InputLoader feeder 위치 무시)
- `NullBarcodeReader` — "WAFER-NULL-ID"
- `NullVisionTcpClient` — DeltaTheta=0, DeltaX=0, DeltaY=0 항상 반환
- `NullWaferMapHandler` — 1×1 맵
- `NullTransferPickerUnit` — IsPickerReady 항상 true

## 2. 작업 계획

### Step A. WaferLoaderAdapter 신규 생성
- `IWaferLoader` 구현 — InputLoader.FeederY 위치 검사 → IsFeederAtSafePosition 실 반환
- `Equipment/Sim/WaferLoaderAdapter.cs`

### Step B. CDT320Machine — Adapter 주입
- NullWaferLoader → WaferLoaderAdapter(InputLoader)

### Step C. MachineController — InputStage 사이클 통합
- 사이클 시작 시 (LoadNextWafer 후): VisionAlignAndSetupOriginAsync 호출
- DoOneDieAsync 에서: 다이 위치 (StageY, CameraX) 이동
- 사이클 종료 시: UnloadWaferAsync 호출

### Step D. InputStage SoftLimit 검토
- 모든 축 SoftLimitPlus 기본 200mm — 확인하여 필요 시 확장

### Step E. SimCassetteDriver 확장 (또는 신규 SimStageDriver)
- InputStage 의 NeedleVacuum 토글 시뮬

## 3. 체크리스트

| ID | 항목 | 검증 방법 |
|----|------|----------|
| A1 | WaferLoaderAdapter 클래스 신규 | 파일 존재 |
| A2 | IWaferLoader.IsFeederAtSafePosition 실 구현 | 코드 grep |
| A3 | csproj 등록 | 빌드 성공 |
| B1 | CDT320Machine NullWaferLoader → Adapter | grep 결과 |
| C1 | LoadNextWafer 후 VisionAlign 호출 | 코드 grep |
| C2 | DoOneDie 에서 InputStage 위치 이동 | 코드 grep |
| C3 | CycleRun 종료 시 UnloadWafer 호출 | 코드 grep |
| D1 | StageY / CameraX 등 SoftLimit 확장 | 생성자 변경 |
| E1 | SimDriver — NeedleVacuum 시뮬 | 추가 또는 패스 |
| F1 | Build clean | warning 0 |
| F2 | verify_all 117/118 PASS | 회귀 무결성 |
| F3 | auto-cycle InputStage 시퀀스 동작 | 로그 확인 |
| F4 | 정직 audit — 런타임 ALARM 0 | 로그 검사 |
| F5 | 문서 3종 갱신 | PPT 재생성 |

## 4. 위험

- VisionAlignAndSetupOriginAsync 가 NullVisionTcpClient 와 호환될지 확인 필요
- StageY/CameraX 의 default SoftLimit 200 이지만 다이 좌표는 0~50mm 정도이므로 OK 예상
- WaitForUserConfirmAsync 는 auto-cycle 에선 무한 대기 위험 → 호출 안 함

## 5. 산출물

- 수정 파일: `MachineController.cs`, `CDT320Machine.cs`, `InputStageUnit.cs` (Setup 일부)
- 신규 파일: `Equipment/Sim/WaferLoaderAdapter.cs`
- csproj 업데이트
- PPT 3종 자동 갱신 (Stage 28 섹션 추가)
