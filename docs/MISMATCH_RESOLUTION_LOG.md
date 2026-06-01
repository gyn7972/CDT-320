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


## STAGE 63 — TopSide/BottomSide → FrontSide/RearSide 리네임 (2026-05-28)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-63-1 | 전 코드베이스 (Common/Vision/Handler) | 검사 이름 의미 불일치 — Top/Bottom 은 수직면을 암시하나 실제는 다이 앞/뒤 측면 카메라 검사 | TopSide/BottomSide → FrontSide/RearSide 로 전수 리네임. 라벨 "상면/하면 검사" → "앞쪽/뒤쪽 측면 검사" |
| M-63-2 | 설정 파일 (algorithm_camera.json / vision.json / settings.json / Recipe) | 구버전 키 잔존 시 로드 깨짐 | 자동 마이그레이션: AlgorithmCameraSubset.MigrateLegacyAlgorithmNames + [DataMember(Name=old)] Legacy 프로퍼티 + OnDeserialized. Load 가 정규화 재저장 → 구 키 1회 로드 후 소멸 |
| M-63-3 | inspector 내부 id (TopSurfaceInspector 등) | 모듈은 FrontSide 인데 내부 id 는 Top* | 백엔드 호환 위해 inspector 내부 id 는 의도적 유지 (외부 노출 X). RESULT 보고서에 명시 |

## STAGE 64 — 검사별 카메라 오버라이드 SPEC (2026-05-28)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-64-1 | Ui\Editors\ 5종 ParameterEditor vs 본 Stage | 레이어 혼동 주의 — 편집기 5종은 **알고리즘 파라미터**(Threshold/Template), 본 Stage 는 **카메라 파라미터의 검사별 오버라이드**. 서로 다른 레이어 | SPEC §12 에 구분 명시. 본 Stage 는 편집기 5종 미변경 |
| M-64-2 | 중복 InspectionId (ReticleFinder/DieFinder/ScaleFinder/FocusFinder/DieEdgeFinder) | 여러 알고리즘이 동일 검사 ID 보유 | override 는 (Algorithm, InspectionId) 조합으로 유일 — Inspections 가 AlgorithmCameraMapping 내부라 알고리즘 스코프에서 충돌 없음 (SPEC §2.2) |

## STAGE 65 — Maintenance → Recipe 통합 (2026-05-29)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-65-1 | QMC.Vision UI | MaintenancePage ↔ RecipePage 중복 | Maintenance 페이지를 Recipe 로 개명(파일/클래스/헤더), 기존 placeholder RecipePage 삭제, Tab.Maintenance/btnMaintenance 제거. 하단바 좌측 4→3 (운영/환경설정/레시피) |
| M-65-2 | RecipePage.PopulateTree | 트리에 3 모듈만 노출 (FrontSide/RearSide 누락) | 5 모듈 전부 등록 — Wafer 7 / Bin 4 / Bottom 7 / FrontSide 4 / RearSide 4 = 26 검사 노드 |

## STAGE 66 — LFine 조명 컨트롤러 SPEC (2026-05-29)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-66-1 | LightControl/Optics/LFine/LFinePSDigitalIlluminator.cs | LFinePSDigitalIlluminatorCommunicator 파일 부재 (LFine 폴더에 없음; LS/Leesos만 존재) | PS 변종 본 Stage 범위 외로 확정 |
| M-66-2 | 매뉴얼(2) vs io_set.lightSource.json(COM1/2/3) vs LFineConfig(단일) | 컨트롤러 개수 불일치 | 확인 필요 #5 — 단수/List 결정 후 Config 확정 |
| M-66-3 | IlluminatorPanel.cs(4채널 더미) vs io_set.json(8채널) | 채널 수 불일치 | 채널 가변화 (구현 Stage) |
| M-66-4 | LightControl 코드 = Part/IlluminatorConfig/ListParam/SerialComm 레거시 의존 | 직접 포팅 불가 | 현 CDT-320 패턴으로 신규 작성 (프로토콜만 차용) |
| M-66-5 | io_set.json 채널 6/7 = "TOP/BOTTOM SIDE VISION" | Stage 63 FrontSide/RearSide 리네임과 라벨 불일치 | 라벨 정합 검토 (구현 Stage) |

## STAGE 67 — LFine 조명 컨트롤러 구현 (2026-05-29)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-67-1 | io_set.lightSource.json(8채널/COM1·2·3) → 컨트롤러 2개 | #5 컨트롤러 2개 확정에 따라 8채널을 COM1 4 + COM2 4 로 분배 | LFineLightSetup.CreateDefault 기본값. 실 결선 시 포트/채널 조정 필요 |
| M-67-2 | io_set 채널 6/7 "TOP/BOTTOM SIDE VISION" | Stage 63 리네임 정합 | 기본값에서 "FRONT SIDE VISION"/"REAR SIDE VISION" 으로 반영 |

## STAGE 68 — 검사별 조명 매핑 SPEC (2026-05-29)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-68-1 | IlluminatorPanel.cs:29 | 4채널 더미 — ValueChanged 가 라벨 텍스트만, 하드웨어 명령 0 | 구현 Stage 69 에서 InspectionLightPanel 신설로 대체 |
| M-68-2 | FinderPage:55 / InspectorPage:53 | IlluminatorPanel 에 검사 컨텍스트 미전달 | Stage 64 검사 노드 결합으로 컨텍스트 주입 (Stage 69) |
| M-68-3 | LightControllerPage:180-198 | 시리얼 Write 1회만, 검사별 매핑/응답 없음, Vision 비결선 | LightHub + InspectionLightPanel 로 정식 결선 (Stage 69) |
| M-68-4 | 매뉴얼(2) / io_set(3포트) / 본 결정(2 컨트롤러) | io_set 8채널 3포트를 2 컨트롤러로 재매핑 | LightSystemMigrator + 확인 필요 #1 채널 풀 배정 |
| M-68-5 | Stage 67 ILightController | SwitchPageAsync 부재 | Page 축 사용 시 구현 Stage 에서 추가 (PageCount==1 이면 no-op) |

## STAGE 69 — 검사별 조명 매핑 구현 (2026-05-29)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-69-1 | io_set(3포트) vs 결정(2 컨트롤러) | 자동 마이그레이션은 io_set 실 포트(3개) 충실 반영 | 사용자가 Setup 결선 표 + 포트 일괄 변경/컨트롤러 삭제로 2개로 통합. M-68-4 후속 |
| M-69-2 | LFineLightController.SwitchPageAsync | Stage 67 Config 에 페이지 명령 포맷 미정의 | 현재 no-op(true). 실 페이지 모델 컨트롤러 도입 시 명령 추가 (Stage 68 #3) |

## STAGE 70 — Light Panel 5건 fixup (2026-05-29)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-70-1 | LightSystemSetup / InspectionLightSubset | Wiring.Page(Setup) → Setting.Page(Recipe) 이동 | AlgorithmLightWiring.Page → LegacyPage(읽기전용) + AlgorithmCameraSubset.MigrateWiringPageToSettings 자동 1회. 소비 후 LegacyPage=0 → 재저장 시 구 키 소멸 |
| M-70-2 | FinderPage:55 / InspectorPage:53 | 더미 IlluminatorPanel(4채널) 사용 | InspectionLightPanel(algorithmKey, inspectionId) 로 교체. VisionModule.AlgorithmKey 추가 (5 모듈 override) |
