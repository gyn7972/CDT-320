# 시퀀스 문서 04~08 ↔ 코드 Mismatch 해소 로그

- **작업 날짜**: 2026-04-30 (야간 무인 작업, R2/R3)
- **방식**: 5개 sub-agent 병렬로 각 시퀀스 문서를 코드와 대조하여 직접 수정
- **마킹 규칙**: 추가/변경된 부분에 `(Mismatch 보완 2026-04-30)` 표기
- **추측 금지**: 모든 라인번호는 grep + Read 로 직접 확인

## 보완 통계

| 문서 | 변경 전 | 변경 후 | 증가 라인 | "Mismatch 보완" 마킹 |
|---|---|---|---|---|
| 04_Feeder | 446 | 559 | +113 | 3 |
| 05_InputStage | 298 | 343 | +45 | 3 |
| 06_TransferPicker | 276 | 408 | +132 | 5 |
| 07_OutputStage | 220 | 259 | +39 | 3 |
| 08_OutputUnloader | 220 | 320 | +100 | 4 |
| **합계** | **1460** | **1889** | **+429** | **18** |

## 04_Feeder_시퀀스_상세분석.md

### §3.5 — 내부 헬퍼 메서드: Pickup / Place
- `PickupWaferAtPositionAsync(double targetPositionY)` — `OutputUnloaderUnit.cs:470`, 5-Step (전진 → 하강 → 클램프 → 센서 ON 확인 → 상승)
- `PlaceWaferAtPositionAsync(double targetPositionY)` — `OutputUnloaderUnit.cs:547`, 6-Step (전진 → 하강 → 언클램프 → 센서 OFF 확인 → 상승 → 홈 후진)
- 각 Step 실패 처리(안전 복귀 패턴) 표 기재

### §3.6 — 공개 시퀀스 메서드 (4개)
- `ScanAllCassettesAsync()` — `:630`, 호출처 `MachineController.cs:432, 726`
- `StoreFullWaferAsync(target, slotIndex)` — `:737`, 호출처 `MachineController.cs:388`
- `SupplyEmptyWaferAsync(source, slotIndex)` — `:819`, UI 수동 버튼만 (`OutputPages.cs:115`)
- `ExchangeWaferSequenceAsync(...)` — `:907`, 어댑터 진입점 (`OutputUnloaderAdapter.cs:46` 에서 미사용 — 현재 dead-code)

### §3.7 — Good1 → Good2 자동 전환 책임 분리
- `OutputUnloaderUnit` 자체엔 자동 전환 로직 **없음**
- `MachineController.StoreCompletedWaferAsync(bool isGood)` (`:352~424`) 에서 결정:
  - `OutputSlotGood1 < 25` → Good1
  - `>= 25` → Good2
  - 둘 다 가득 → `OUT-FULL-GOOD` 알람 + `_cycleCts?.Cancel()`
- 호출 시점: `MachineController.cs:1272` (다이 검사 완료 직후)

## 05_InputStage_시퀀스_상세분석.md

### §3.4 — ExecutePickupAsync 6-Step 코드 일치 검증
- 6단계 모두 코드와 **일치 확인** (`InputStageUnit.cs:850~894`)
- 메서드명 정정: `Tpu.SignalPickReady()` → `Tpu.NotifyPickReady(dieIndex)`, `Tpu.WaitPickerUp` → `Tpu.WaitPickerUpAsync`

### §10.1 — 알람 코드 발생 누락 표 (12개 발생 지점)
- `IS-FEEDER` (1) — `LoadAndPrepareWaferAsync` Step 1
- `IS-EXPZ` (2) — `LoadAndPrepare:338` + `UnloadWafer:776` (기존 문서엔 1곳만)
- `IS-BARCODE` (1) — `LoadAndPrepare` Step 3
- `IS-MAP` (1) — `LoadAndPrepare` Step 4
- `IS-ALIGN` (4) — Center / StageT / Ref1 / Ref2 (기존 문서엔 1곳만)
- `IS-MOVE` (3) — Phase A 스캔 / Phase B 픽업 / ExecutePickup NeedleZ
- 모두 **TODO**: `AlarmManager.Raise` 적용 필요 (현재 `Console.WriteLine` 만)

### §10.2 — 메서드명 Mismatch 표
- `Tpu.SignalPickReady` → `NotifyPickReady` 정정
- `WaitPickerUp` → `WaitPickerUpAsync` 정정
- `Ref1Col` 필드 표기 정정

## 06_TransferPicker_시퀀스_상세분석.md (v1.0 → v1.1)

### 메서드명 정정 (전반)
- `PickAsync` → `PickupAsync` (코드에 `PickAsync` 없음)
- `InspectBottomVisionAsync()` — **인자 없음** (문서 v1.0 의 `pickers` 인자 삭제)
- `PlaceDiesAsync()` — **인자 없음**
- `MoveArmToInputAsync` / `MoveArmToOutputAsync` 는 **존재하지 않음** — 표기 삭제

### §4.1 — Side Vision 검증
- `Task.WhenAll(rotateTask, focusMoveTask)` 위치: `TransferPickerUnit.cs:747`
- `picker.PickerT.MoveRelativeAsync(90.0)` — **상대각** (절대각 아님), 라인 739~740
- `InspectBottomVisionAsync` (523~595) / `PlaceDiesAsync` (860~898) 단계별 라인 표

### §5.1~5.3 — PickupAsync / PlaceAsync / MoveToFocus / MoveToWait 본체

### §5.4 — IVisionTpuClient 구현체
- 정의: `TransferPickerUnit.cs:70~112`
- `NullVisionTpuClient`: `CDT320Machine.cs:91~117`
- `TpuVisionAdapter`: `Equipment/Vision/VisionAdapters.cs:69~134`

### §7 — Sim 매핑 검증
- `SimulatorBridge.cs:88~156` BuildMaps() 매핑 확인
- DO 매핑: VacuumOut Y048~Y051/Y064~Y067, BlowOut Y056~Y059/Y072~Y075
- `CDT320Simulator/MotionMap.cs:27~70` 동일 #00~#36 체계 일치 확인

### §9 — 알람 코드 표 전면 교체
- 임의 코드(`TPU-PICK` 등) → 코드의 실제 Console 메시지 패턴(`'Name' -> Pickup: PickerZ 하강 실패` 등)

## 07_OutputStage_시퀀스_상세분석.md

### §6.1 — Config 파라미터 (4개)
| 파라미터 | 타입 | 기본값 | 라인 |
|---|---|---|---|
| TpuPlaceDoneTimeoutMs | int | 3000 | 216 |
| WaferChangeTimeoutMs | int | 0 (무한) | 219 |
| ColletCleaningTimeoutMs | int | 10000 | 222 |
| BinCameraVelocity | double | 200.0 | 229 |

### §6.2 — SoftLimit 확장 (Stage 30)
| 축 | SoftLimitPlus | 라인 |
|---|---|---|
| StageY (Good/Ng) | 350.0 | 283 |
| StageZ (Good/Ng) | 250.0 | 284 |
| BinCameraX | 350.0 | 466 |

### §9.1 — 코드 미발급 알람 (5개 TODO)
| 코드 | 발생 위치 라인 |
|---|---|
| OS-AVOID | 527 |
| OS-WORKZ | 341 |
| OS-MOVEY | 360 |
| OS-PLACEDONE | 633 |
| OS-BINCAM | 646, 666 (진입/후퇴 양측) |

## 08_OutputUnloader_적재_시퀀스.md

### §4.1 — MoveElevatorWithProtrusionGuardAsync
- `OutputUnloaderUnit.cs:382-451`, 5단계 동작
- InputLoader 측 동등 패턴은 메서드명이 `MoveToTargetSlotAsync` (`InputLoaderUnit.cs:249`)
- OutputUnloader 만 `Config.ProtrusionPollIntervalMs` 외부 노출, InputLoader 는 하드코딩 10ms

### §6.1 — Stage 교환 위치
- `GoodStageExchangePositionY = 150.0` (`:80`, 사용 `:359`)
- `NgStageExchangePositionY = 200.0` (`:86`, 사용 `:358`)
- `GetStageExchangePositionY` 분기 코드 (`:354-360`)

### §9.1 — 알람 코드 발급 검증 (모두 구현됨)
| 코드 | 발급 위치 |
|---|---|
| OUT-FULL-GOOD | `MachineController.cs:365` |
| OUT-FULL-NG | `:378` |
| OUT-STORE | `:391` |
| OUT-STORE-EX | `:420` |

→ 모두 `MachineController` 에서 정상 발급. `OutputUnloaderUnit` 자체에서는 Console 로그만.

### §12 — 추가 메서드 (3개, 신규 §)
- §12.1 `ScanAllCassettesAsync()` — `:630-716`. 75 슬롯 (3 × 25) 스캔
- §12.2 `SupplyEmptyWaferAsync(...)` — `:819-884`. UI PICK 버튼 단독, 메인 사이클 미사용
- §12.3 `ExchangeWaferSequenceAsync(...)` — `:907-956`. `OutputUnloaderAdapter.cs:9, 27` 어댑터 진입점

## 종합 결론

5개 시퀀스 문서가 **18건의 mismatch 보완** 후 코드와 정합성 향상. 주요 수정 카테고리:

1. **누락된 메서드/소항목 추가** (10건) — 헬퍼 메서드, 추가 공개 메서드, 책임 분리, ProtrusionGuard 등
2. **메서드명/시그니처 정정** (3건) — `PickAsync→PickupAsync`, `SignalPickReady→NotifyPickReady`, 인자 정정
3. **알람 코드 TODO 마킹** (3건) — InputStage 12개, OutputStage 5개 — 후속 작업 B 의 입력 자료
4. **검증 완료 표시** (2건) — Sim 매핑, ExecutePickup 6-Step

작업 B 의 알람 코드 매핑은 위 §10.1, §9.1 표를 그대로 활용하면 정확한 라인까지 매핑 가능.


## STAGE 62 — Vision 알고리즘별 카메라 파라미터 (2026-05-27)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-62-2 | VisionConfig.cs:30-32 | TopSide/BottomSide 카메라 ID 필드 없음 | AlgorithmCameraSubset (QMC.Common.Recipes) 신설로 보완. 구버전 VisionSettings 의 3 필드는 EnsureDefaults fallback 으로만 사용 |
| M-62-3 | AlgorithmCameraMapping | ROI 필드 누락 | Stage 62 에서 RoiOffsetX/Y/Width/Height 4 필드 추가 + IsRoiFull/ToRectangle 헬퍼 |
| M-62-5 | AlarmMaster.cs Vision 카테고리 | prefix 혼재 (VISION-/Vision/EXPOSE-/ALIGN-) | 신규 코드는 VISION-* 로 통일: VISION-MAPMISS / VISION-PARAMFAIL / VISION-CAMOPEN 3건 추가 |
| M-62-E | AlarmManager 위치 | Vision 측이 직접 호출 불가 (네임스페이스 분리) | QMC.Common.Alarms 로 이동. Lang.Current 의존성은 LanguageProvider 콜백으로 추상화. Handler/Vision 양쪽 공유 사용 |
| M-62-G | Recipe-Vision 분리 | Project별 카메라 설정 불가 | RecipeProject.VisionCameras (AlgorithmCameraSubset) 필드 추가. 모델은 QMC.Common.Recipes 에 위치 |

