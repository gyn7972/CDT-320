# 310 Handler 기능 카탈로그

> 분석 대상: `D:\Work\CDT-320\QMC.DieTransfers-master\QMC.DieTransfers-master\` (905 .cs 파일)
> 목적: 320 (`QMC.CDT-320`) 에 이식해야 하는 **기능** 추출.
> **주의: SoftBricks Element/Part/Module/Recipe 트리·MechaSys/SentiCore OEM 프레임워크 코드 형식은 절대 모방하지 않음. 기능만.**

---

## 1. 최상위 장비 (Equipment)

| 310 Class | 의미 | 320 매핑 |
|---|---|---|
| `DieTransferEquipment` | 단일존 (CDT-300) | — |
| `DualZoneDieTransferEquipment` | 듀얼존 (CDT-310/320) | `CDT320_Machine` |
| `GeneralEquipmentOperationPanel` | 운전 패널 (start/stop/cycle) | `MachineController` |
| `RecipeInformationInformServiceExecutor` | 호스트에 레시피 변경 알림 | (SECS 도입 시) |
| `ReworkCount` int | 재작업 카운터 | (없음) |

### Forms (CDT-300 스타일)
- DualZoneDieTransferEquipmentOperationForm
- DualZoneDieTransferEquipmentSetupForm
- DualZoneDieTransferEquipmentMaterialForm
- DualZoneDieTransferEquipmentSubstrateForm
- DualZoneDieTransferEquipmentMaintenanceForm
- DualZoneDieTransferEquipmentMonitoringForm
- DualZoneDieTransferEquipmentLoadPortForm
- DualZoneDieTransferEquipmentBottomInspectionForm
- DualZoneDieTransferEquipmentTestForm

---

## 2. Module 계층 (생산 파이프라인)

### 2.1 DieTransferModule (abstract)
- AlarmKeys: `MaterialDoesExistAfterSend`, `MaterialDoesExistBeforeReceive`, `MaterialDoesNotExistAfterReceive`, `MaterialDoesNotExistBeforeSend`, `MaterialDoesExistAfterInitialize`, `FailedPick`
- ServiceExecutorGroup + SubsetRecipeAssigner

### 2.2 구체 Module
| Class | 역할 |
|---|---|
| `DieStageTransferModule` | 다이 스테이지 (웨이퍼 척 + 정렬 + 픽인스펙션 등) |
| `PickAndPlaceDieTransferModule` | Pick→Place 통합 모듈 |
| `DieTransferBasicFeeder` | 기본 Feeder |
| `DieTransferLifterLoadPort` | 로드포트 + Lifter |

### 2.3 Module Service Executors (모듈이 할 수 있는 명령들)
| ServiceExecutor | 의미 |
|---|---|
| `ColletCleaningServiceExecutor` | 콜렛 청소 |
| `ColletInspectionServiceExecutor` | 콜렛 검사 |
| `DiePickInspectionServiceExecutor` | 픽 직후 다이 검사 |
| `DiePlaceInspectionServiceExecutor` | 플레이스 직후 검사 (배치 갭) |
| `DieSurfaceInspectionServiceExecutor` | 다이 표면 검사 |
| `DieTapeFrameAlignmentServiceExecutor` | 웨이퍼 정렬 (3점/N점) |
| `DieTapeFrameBarcodeReadingServiceExecutor` | 웨이퍼 바코드 |
| `DieTapeFrameInformationInformServiceExecutor` | 호스트에 웨이퍼 정보 통지 |
| `DieGapUpdateInformServiceExecutor` | DieGap 검사 결과 호스트 보고 |
| `DieRepeatabilityJobOrderGenerateServiceExecutor` | 반복성 측정용 Job 생성 |
| `ManualColletInspectionServiceExecutor` | 수동 콜렛 검사 |
| `ManualServiceExecutor`, `PickManualSE`, `PlaceManualSE` | 수동 동작 |
| `PickRetryServiceExecutor` | 픽 재시도 (실패 시 위치 보정) |
| `PickServiceExecutor` / `PlaceServiceExecutor` / `PlaceInLoadServiceExecutor` | 픽/플레이스/로딩 시 플레이스 |
| `ReferenceDiePositionServiceExecutor` | 기준 다이 위치 학습 |
| `VisionCorrelationServiceExecutor` | 비전-축 좌표 상관(캘리브레이션) |

---

## 3. Job 시스템 (프로세스 워크플로 객체)

310 은 모든 작업 단계를 **Job 객체**로 모델링 (state machine 보유). 22 Jobs:

| Class | 역할 |
|---|---|
| `DieJobOrder` | 다이 1개당 Job 명령 (Ready → Picking → Placing → Done) |
| `DieTransferJob (abstract)` + `DieTransferJobCalculator` | 모든 다이 전송 Job 의 부모 + 계산기 |
| `DiePickJob` | Pick 1회 |
| `DiePlaceJob` / `DiePlaceInLoadJob` | Place 1회 (일반/로딩시) |
| `DieReadyToPickJob` / `DieReadyToPlaceJob` | 준비 단계 |
| `DieInspectionJob` / `DieArrayInspectionJob` | 검사 (단일/배열) |
| `DieMapGenerateJob` | 다이 맵 생성 |
| `DieTapeFrameAlignmentJob` | 웨이퍼 정렬 |
| `DieTapeFrameBarcodeReadingJob` | 바코드 |
| `DieTapeFrameScanJob` | 웨이퍼 풀 스캔 |
| `PickerCollectCleaningJob` | 콜렛 청소 |
| `DualZoneDieTransferControlJobActivator` | 듀얼존 작업 활성화 |
| `DualZoneDieTransferControlJobOrderGenerator` | 작업 명령 생성기 |
| `DualZoneDieTransferControlJobStateController` | 컨트롤 작업 상태 |
| `DualZoneDieTransferProcessJobStateController` | 프로세스 상태 |
| `DualZoneDieTransferJobDispatcherCreator` | Dispatcher 팩토리 |
| `DualZoneDieTransferTransferVsSelfExecutionJobDispatcherJudge` | 어느 쪽이 실행할지 판정 |
| `BinLoadPortPlateJobOrderGenerator` | 빈 로드포트 플레이트 Job |

`JobPriority`, `JobOrderState (Ready/Running/Done/Failed)`, `JobCalculator` 패턴.

---

## 4. Interlock 계층 (40 모션 안전)

명시적 충돌 방지 — 각 두 파트 간 어느 쪽이 어디 있을 때 다른 쪽이 움직일 수 없는지 정의.
모두 `MotionInterlock` 상속, `OnVerifyToMove(Trajectory[])` 에서 위치 비교.

| 카테고리 | 인터록 |
|---|---|
| Vision ↔ Picker  | `BinVisionWithLinearDiePickerTransferMotionInterlock`, `LinearDiePickerTransferWithBinVisionMotionInterlock`, `LinearDiePickerTransferWithWaferVisionMotionInterlock` |
| Vision ↔ Stage   | `BottomVisionWithDieStageMotionInterlock` |
| Vision ↔ Feeder  | `FeederArmWithMotionVisionPartInterlock`, `MotionVisionPartWithFeederArmMotionInterlock` |
| Stage ↔ Stage    | `DieStageWithDieStageMotionInterlock` (듀얼존) |
| Stage ↔ Picker   | `DiePickerWithDieStageRelativeMotionInterlock`, `LinearDiePickerTransferWithDieStageMotionInterlock` |
| Stage ↔ Feeder   | `DieStageWithFeederArmMotionInterlock`, `FeederArmCylinderWithDieStageMotionInterlock`, `FeederArmWithDieStageMotionInterlock` |
| Stage ↔ Eject    | `DieEjectCapHolderWithDieStageMotionInterlock`, 등 7건 |
| Stage ↔ Reticle  | `DieStageWithReticleCylinderInterlock`, `ReticleCylinderWithDieStageMotionInterlock`, `ReticleWithEjectorCapZMotionInterlock` |
| Stage ↔ Expander | `DieStageWithMotionExpanderInterlock`, `MotionExpanderWithDieStageMotionInterlock` |
| Stage ↔ Post     | `CylinderDiePostWithDieStageMotionInterlock`, `DieStageWithDiePostMotionInterlock`, `DiePostCylinderWithSingleAxisDieStageLifterMotionInterlock`, `DieStageWithVerticalCylinderDiePostInterlock` |
| Picker ↔ Picker  | `MotionDiePickerWithLinearDiePickerTransferMotionInterlock` |
| Picker ↔ Stage Lift | `LinearDiePickerTransferMotionWithStageUpDownCylinderInterlock`, `StageUpDownCylinderWithDiePickerTransferMotionInterlock` |
| Picker ↔ Feeder  | `LinearDiePickerTransferWithFeederArmInterlock` |
| Feeder ↔ Expander | `FeederArmWithMotionExpanderMotionInterlock`, `MotionExpanderWithFeederArmMotionInterlock` |
| Sensor 기반     | `NeedMaterialSizeMotionInterlock`, `ProtrusionSensorWithFeederArmVerticalCylinderInterlock`, `SingleAxisDieEjectorWithDieStageMotionInterlock` |

각 인터록은 `OwnerRelativeSafetyDistance` 같은 ConstructConfiguration 으로 거리 임계값 관리.

---

## 5. SECS/GEM 호스트 통신

`DieTransferGemService : GeneralGemService`:
- `RecipeBodySecsItemConverter = ZipRecipeBodySecsItemConverter` (zip 압축 후 SECS S7F3/S7F5 등으로 송수신)
- 호스트 RemoteCommand 4종:
  - `ProceedWithTapeFrame(objid)` — host: 이 웨이퍼 진행 OK
  - `StoppedWithTapeFrame(objid)` — host: stop
  - `ProceedWithMap(objid, path)` — host: die map 파일 수신 후 OK 통지
  - `StoppedWithMap(objid, path)` — host: map fail
- 상태 머신: `DieTapeFrameIdentifierStateMachine` (WaitingForHost → VerificationOk/Failed)
- 상태 머신: `DieMapGenerateStateMachine` (WaitingForHost → VerificationOk/Failed)

이벤트 송신 (host 보고):
- `DieTransferSecsEquipmentEventReportProcessor` — 알람/머터리얼/잡 변경 등 이벤트 SECS 변환

---

## 6. Materials (자재 모델)

### 6.1 `Die`
- `Results` enum: Unknown / Good / NG
- `ProcessInformationData`: LotID, RecipeName, UsingPickerNumber, RetestCount
- `NGCodes` (List<string>): 검사 실패 코드 누적
- `BinCode` (int): BinCodeManager 가 산출
- `JobOrders` collection
- `Presence`: Exist / NotExist
- `Position` (X/Y/R)
- `IConvertToSecsItem` 구현 — host 보고용

### 6.2 `DieTapeFrame : TapeFrame`
- `ProcessModes`: Normal / Repeability
- `DieTapeFrameRoles`: Load / GoodUnload / NgUnload
- `Rotates`: None / Rotate90 / Rotate180 / Rotate270
- `RepeatabilityInformationData`: TotalDieCount, CurrentRepeatDie
- `IdentifierState`: WaitingForHost (호스트에 verify 요청)
- `DieMapGenerateState`: WaitingForHost (맵 파일 요청)
- `MapFileName` (string)
- 다이 격자 정보 (X/Y count, pitch)

### 6.3 `LoadDieTapeFrame` / `UnloadDieTapeFrame`
- 역할별 분기 — Load 는 픽 대상, Unload 는 플레이스 대상.

---

## 7. Recipe 시스템

### 7.1 `DieTransferGeneralUnionRecipe`
- `GeneralUnionRecipe` 상속 — 모든 SubsetRecipe 합집합.
- `Body = DieTransferGeneralUnionRecipeBody`

### 7.2 SubsetRecipes (자재 단위)
- `DieSubsetRecipe` — Die 1종 (크기/스펙/검사 임계값)
- `DieTapeFrameSubsetRecipe` — 웨이퍼 1종 (격자/회전/Roles)
- `LoadDieTapeFrameSubsetRecipe` / `UnloadDieTapeFrameSubsetRecipe`
- 각 Subset 는 `*Assigner` 와 `*Editor` 를 가짐 (HMI 편집 GUI)

### 7.3 SubsetRecipes (모듈 단위)
- `DieTransferModuleSubsetRecipe`
- `DieStageTransferModuleSubsetRecipe`
- `PickAndPlaceDieTransferModuleSubsetRecipe`

### 7.4 SECS 직렬화
- `ZipRecipeBodySecsItemConverter` — 레시피 본문 zip + base64 → SECS BINARY 로 송수신.
- `DieTransferGeneralUnionRecipeEditorForm` — 합집합 통합 편집 GUI.

---

## 8. Parts (기계 부품)

### 8.1 BinCodeManager (싱글톤)
- `ConvertToBinCode(Die)` → bin number (1=Good, 255=Max)
- `ConvertToBinCodeColor(int binCode)` → 색상 (UI 표시용)
- 설정: `Configuration.Body.BinCodes` (NG 코드 → bin), `BinCodeColors` (bin → 색)
- 320 에 **없음** — 추가 필요

### 8.2 CoordinateManager
- 좌표 페어 관리: 비전 좌표 ↔ 모터 좌표 매핑 테이블
- 320 에 **없음**

### 8.3 MaterialSizeManager
- 자재 크기별 처리 분기 (예: 픽커 너비, 카세트 슬롯 등)
- 320 에 **없음**

### 8.4 MotionExpander / Expander
- 웨이퍼 링 익스팬더 (다이싱 후 다이 간격 확장)
- 320 에 **없음** (CDT-320 은 expander 미사용)

### 8.5 OutputDieMapDataSaver
- 다이 처리 결과를 외부 파일 (CSV/binary) 로 저장
- 320 에 **부분 있음** (EventLogger CSV 만)

### 8.6 IonizerAlarmDetectSensor
- 정전기 제거 이오나이저 알람 입력 감시
- 320 에 **없음**

### 8.7 SubPortMaterialRejector
- 불량 자재 별도 슬롯 분리
- 320 에 **없음**

### 8.8 DiePickerVacuumMaterialPresenceDetector
- 픽커 진공 압력으로 다이 존재 감지
- 320 에 **없음** (IO 매핑은 있음)

### 8.9 RemoteViewer
- 외부 PC 에서 화면 모니터링
- 320 에 **없음**

### 8.10 Communicators
- `VisionPCCommunicator` — STX/ETX + TransactionId + Cmd + DataCount + Data[]
  - `ExposureDoneCommand="EPD"`, `AlarmCommand="ARM"` 기본값
  - TransactionId 1~10000 (Vision 측은 10001~20000) — 충돌 방지
  - 320 에 **있음** (`VisionTcpClient` — 단순화된 라인 프로토콜 + EPD/ARM 푸시 수신)

### 8.11 DiePicker / DiePickers
- `DiePicker` (base) + `MotionDiePicker` (모터 구동 픽커)
- `DiePickerHistory` — 픽 이력 (성공/실패 카운트)
- 320 에 **있음** (TransferPicker.LeftArm / RightArm)

### 8.12 DiePickerTransfer / DiePickerTransfers
- `DiePickerTransfer` (base) + `LinearDiePickerTransfer` (직선 이송)
- 320 에 **있음**

### 8.13 DieEjectors (Caps + Pins 하위 폴더)
- `DieEjector` (base) + `SingleAxisMotionDieEjector` (단축 모션 ejector)
- DieEjectCap / DieEjectPin 형상별 분리
- 320 에 **있음** (EjectPin Z축)

### 8.14 DieStages
- `DieStage` + `DieStageLifter` + `SingleAxisDieStageLifter`
- 320 에 **있음**

### 8.15 Services 폴더 (Parts 내부)
- `RecipeInformationInformServiceExecutor` — 호스트에 recipe 정보 통지

---

## 9. DieMaps (다이 맵 시스템)

### 9.1 DieMapGenerateServiceExecutor
- 웨이퍼 위 다이 위치를 격자로 생성 (X count × Y count, pitch X/Y, origin 기반)
- ReferenceDie 위치를 정점으로 사용
- 결과를 `DieTapeFrame.MapFileName` 으로 저장
- 320 에 **없음**

### 9.2 Assistants 하위 폴더
- 다이 맵 생성/검색/수정 보조 기능 (예: NeighborSearch, GridGenerator)

---

## 10. HMI Forms (CDT-300 스타일)

| Form | 용도 |
|---|---|
| `DualZoneDieTransferEquipmentOperationForm` | 작업 메인 (Init/Start/Stop/Cycle) |
| `DualZoneDieTransferEquipmentSetupForm` | 운전 설정 |
| `DualZoneDieTransferEquipmentMaterialForm` | 자재 (Die) 관리 |
| `DualZoneDieTransferEquipmentSubstrateForm` | 기판 (TapeFrame) 관리 |
| `DualZoneDieTransferEquipmentMaintenanceForm` | 유지보수 (수동 모션) |
| `DualZoneDieTransferEquipmentMonitoringForm` | 실시간 모니터링 |
| `DualZoneDieTransferEquipmentLoadPortForm` | 로드포트 |
| `DualZoneDieTransferEquipmentBottomInspectionForm` | 바텀 검사 |
| `DualZoneDieTransferEquipmentTestForm` | 테스트 |

각 Form 은 `OperationAssistant` (e.g. `DualZoneDieTransferEquipmentOperationAssistant`) 를 통해 비즈니스 로직 분리.

`GeneralEquipmentOperationPanel` — 모든 Form 공유 운전 패널.

---

## 11. Lots (랏 관리)

`Lots/` 단 1 파일 — Lot 기반 추적 (LotID 별 통계 누적). 320 에 **없음** (다이 단위만).

---

## 12. 핵심 동작 흐름 (요약)

```
[Host SECS] → DieTransferGemService.ProceedWithTapeFrame
            → DieTapeFrame.IdentifierState.Transit(VerificationOk)
            → DieTapeFrameAlignmentJob 시작
            → DieTapeFrameAlignmentServiceExecutor (3점 비전 정렬)
            → ReferenceDiePositionServiceExecutor (기준점 학습)
            → DieMapGenerateJob → DieMap 파일 생성
            → 호스트에 Map 통지 → ProceedWithMap

[Cycle 시작]
[Loop per Die]
  DieReadyToPickJob → DiePickJob → PickServiceExecutor (Z↓ + Vacuum + Z↑)
                                 → DiePickInspectionServiceExecutor (콜렛 검사)
                                 → 실패 시 PickRetryServiceExecutor
  DieSurfaceInspectionServiceExecutor (Bottom Vision)
  DieReadyToPlaceJob → DiePlaceJob → PlaceServiceExecutor (Z↓ + Vacuum off)
                                   → DiePlaceInspectionServiceExecutor (Bin Vision Gap)
  BinCodeManager.ConvertToBinCode(die) → bin 결정 → SubPortMaterialRejector or 일반 Bin
  Die.Results = Good/NG
  EventReport → DieTransferSecsEquipmentEventReportProcessor → SECS 송신

[Cycle 끝]
  OutputDieMapDataSaver — 결과 맵 파일 저장
  RecipeInformationInform — 변경된 레시피 호스트 통지
```

---

## 13. Tier 분류 (320 이식 우선순위)

### Tier 1 — 필수 (생산 운영 필수)
- **BinCodeManager** + 매핑 테이블 + 색상 표시 (UI 카운터)
- **DieMap (Wafer Map)** 생성 + 시각화
- **Material 모델 보강** — `Die` 에 NGCodes/Results/Position/JobOrders 필드 추가
- **DieTapeFrame** 모델 (현재 320 미존재) — 격자/회전/ProcessMode
- **Job 시스템** (간이) — 다이 단위 JobOrder + state (Ready/Running/Done/Failed)
- **Interlock 5종 (핵심)**:
  - Picker × DieStage 거리 인터록
  - Picker × Picker (듀얼존) 충돌 인터록
  - Vision × Picker 인터록
  - DieStage × Eject 인터록
  - DieStage × Stage Lifter 인터록

### Tier 2 — 강력 권장 (운영 품질/추적성)
- **PickRetry** 로직 (실패 시 위치 보정 후 재픽)
- **ReferenceDiePosition** (기준 다이 학습으로 좌표 보정)
- **VisionCorrelation** (비전-축 좌표 상관 캘리브레이션)
- **OutputDieMapDataSaver** (생산 후 다이 맵 결과 저장)
- **CoordinateManager** (좌표 페어 매핑)
- **MaterialSizeManager** (다이/기판 크기 파라미터화)
- **DiePickerHistory** (픽커별 통계)
- **DieTapeFrameAlignment** (3점 정렬 → 좌표 보정 매트릭스)
- **DieTapeFrameBarcodeReading** (웨이퍼 바코드)
- **DieRepeatability** (반복성 측정 모드)
- **SECS/GEM 도입** (호스트 통신) — 단계적
- **ZipRecipe** SECS 송수신
- **Lot** 기반 통계

### Tier 3 — 선택
- RemoteViewer
- IonizerAlarmDetectSensor
- SubPortMaterialRejector (현재 320 BinGuide 액세서리로 충분)
- 31 추가 인터록
- 다양한 Job 변형 (DieArrayInspection, ColletCleaning 등)

---

## 14. 320 매핑 가이드 (이식 시)

| 310 개념 | 320 우리 아키텍처 |
|---|---|
| `DualZoneDieTransferEquipment` | `CDT320_Machine` (이미 있음) |
| `DieTransferModule.ServiceExecutorGroup` | **만들지 않음**. `MachineController` 의 메서드로 직접 구현 또는 `Equipment/Services/` 폴더에 정적 클래스 |
| `Job` (state machine 객체) | 간이: `class JobOrder { State, DieRef, Type }` + `JobScheduler` 큐 |
| `MotionInterlock` (40종) | `class MotionInterlock` 추상 + 거리/위치 비교 메서드. 핵심 5건만 구현. |
| `BinCodeManager` (싱글톤) | `static class BinCodeMap` + JSON 설정 |
| `DieMapGenerateServiceExecutor` | `static class DieMapGenerator` + `class DieMap` (Grid + DieRecord[]) |
| `DieTapeFrame` material | `class DieTapeFrame` + JSON 직렬화 |
| `Die.NGCodes / Results / BinCode` | 기존 `Die` material 확장 |
| `OutputDieMapDataSaver` | `static class DieMapSaver` (CSV) |
| `CoordinateManager` | `class CoordinateMap` (CSV 기반 캘리브레이션 테이블) |
| `MaterialSizeManager` | `class MaterialSpecs` (JSON 설정) |
| `DieTapeFrameAlignment` | 비전 3점 정렬 → affine 매트릭스 산출 (`AlignmentSolver`) |
| `PickRetry` | `MachineController.DoOneDieAsync` 내부 retry 루프 (현재 없음) |
| `DieTransferGeneralUnionRecipe` | 기존 `RecipeStore` JSON 확장 |
| `DieTransferGemService` | (별도 라운드) `Comm/SecsHost.cs` |
| `ZipRecipeBodySecsItemConverter` | (별도 라운드) `RecipeStore.ExportZip / ImportZip` |
| `DieTapeFrameIdentifierStateMachine` | `enum + transitions` (단순 enum + Transition() 메서드) |
| `RemoteViewer` | (별도 라운드) |

---

## 15. 320 현재 상태 vs 이식 필요 (요약 매트릭스)

| 영역 | 320 현재 | 310 기준 | 이식 필요? |
|---|---|---|---|
| Equipment 기본 트리 | ✅ CDT320_Machine | DualZone* | (변환 불필요, 동등) |
| 자재 (Die) 모델 | ✅ 단순 | NGCodes/Results/Position/JobOrders | **확장 필요** |
| TapeFrame 모델 | ❌ | DieTapeFrame (격자/회전/State) | **신규** |
| Job 시스템 | ❌ inline cycle | 22개 Job 클래스 | **간이 신규** |
| Interlock | ❌ | 40 개 | **5개 신규** |
| BinCode 관리 | ❌ | BinCodeManager + Map | **신규** |
| DieMap 생성 | ❌ | DieMapGenerateServiceExecutor | **신규** |
| DieMap 시각화 | ❌ | DieMap Form/Control | **신규** |
| Vision 통신 | ✅ VisionTcpClient | VisionPCCommunicator | (동등) |
| Vision 정렬 | ❌ | DieTapeFrameAlignment 3점 | **신규** |
| Vision 코릴레이션 | ❌ | VisionCorrelation | **신규** |
| ReferenceDiePosition | ❌ | ServiceExecutor | **신규** |
| PickRetry | ❌ | ServiceExecutor + 위치 보정 | **신규** |
| MaterialSizeManager | ❌ | Element | **신규** |
| CoordinateManager | ❌ | Element | **신규** |
| OutputDieMapDataSaver | 부분 | EventLogger CSV | **확장** |
| SECS/GEM | ❌ | DieTransferGemService | (Tier 2 — 별도 라운드) |
| Recipe (Union/Subset) | 부분 | 4 SubsetRecipe + Editor | **확장** |
| Lot | ❌ | Lot 객체 | (Tier 3) |
| RemoteViewer | ❌ | RemoteViewer Element | (Tier 3) |
| HMI Forms (Material/Substrate/Bottom 등) | 부분 | 9 폼 | (현재 6 탭 50+ 페이지로 충분; 일부 추가 가능) |
| 듀얼존 활성화/조정 | ✅ TransferPicker.Left/Right | DualZoneDispatcher* | (간단화 됨) |

다음 단계: `HANDLER_IMPL_CHECKLIST.md` 작성 + Tier 1 핵심 항목 구현.
