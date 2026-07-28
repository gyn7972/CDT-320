# CDT-320 Code Analysis (코드 분석)

- 작성일: 2026-07-29 / 기준 커밋: `b6b23cf2` (master, 작업트리 clean)
- 목적: 2026-07-25~07-29 대규모 업데이트 이후의 코드 현황 파악 (분석 전용 — 코드 무수정)
- 소스: 직접 diff 분석 + 읽기 전용 탐색 3회(Sequencing / Equipment / UI) 종합
- 참고 문서: [`AGENTS.md`](../AGENTS.md)(작업 규칙), [`Sequencing/SEQUENCE_MAP.md`](../QMC.CDT-320/Sequencing/SEQUENCE_MAP.md), [`SEQUENCE_SAFETY_ANALYSIS.md`](../QMC.CDT-320/Sequencing/SEQUENCE_SAFETY_ANALYSIS.md)

---

## 1. 솔루션 구조

```
QMC.CDT-320.sln
├── QMC.CDT-320   (.NET Framework 4.7.2 WinForms Handler, .cs 658개)
│   ├── Equipment/    장비 추상화: Ajin 보드, Unit, Interlocks, Motion(SharedRailX),
│   │                 Materials, Vision(외부 Vision PC 브릿지), Recipes, Initialization,
│   │                 Lots, DieMaps, Jobs, Stats, Sim, Secs, Sensors, Alarms, Calibration
│   ├── Sequencing/   공정 시퀀스: root(Input/Output/PickerProcess/MachineReady) +
│   │                 Calibration, Common, InputCassette/Feeder/Stage,
│   │                 OutputCassette/Feeder/Stage, Picker(54개), Safety  — 총 168개
│   └── Ui/           Tabs(6탭) / Pages / Dialogs / Controls / Localization / Security
├── QMC.Common    (공용 라이브러리, .cs 94개: BaseAxis/AxisData/MotionSpeedScale,
│                  AjinE(AXM/AXL P/Invoke), IO, Data Store, Ui.Vision 코덱)
└── (솔루션 외) QMC-master, SP_RemoteViewer, tools, docs, _build_check_handler
```

- 빌드 규칙: 원본 저장소에서 **Clean/Rebuild 금지**, 별도 `OutDir` 지정 `/t:Build`만 허용 (`AGENTS.md` §13 — obj FileList가 운영 경로 `D:\CDT-320`을 참조).
- `D:\CDT-320` = 배포/운영 영역(EXE, Config, EquipmentData, Recipes, State, Log). 소스 아님.

---

## 2. 최근 변경 타임라인 (2026-07-25 → 07-29)

| 날짜 | 커밋 | 내용 |
|---|---|---|
| 07-25 | (다수) | 단건 수정 프롬프트 시리즈: SafeMovePercent, FollowMove 속도스케일, Override 절대좌표/스테일 제거/targetName 전달, 인터락 StageY·VisionX 동시허용, Bottom존 교차작업, **작업영역 예약 원자화(M8)**, NeedleZ 자동상승/FastConti 전면 삭제 등 |
| 07-26 | `61362d59` 등 | **Vision 촬영(EPD) 후 즉시 회피 + 팔로잉 진입** 인풋/아웃풋 미러 대규모 반영, 콜렛클리닝 구현, Bottom ZT 선행구동(50mm 접근), PickUp/Place Z 선행하강 조기완료판정, 시퀀스 파일 분할 시작, 전체초기화 1차. ※ 22:08 실장비 사고(followVel=0 폭주) → Config 임시치환 폐기 |
| 07-27 | `f9210867` | **모션 명령부 스레드 안전화 + 스케일 강제**: `AxisData` 속도/가감속 getter를 `protected`로 봉인, `GetDefaultVel/Acc/Dec()`(스케일본)·`GetRaw*()`(명시)만 허용. 명시 가감속은 `BaseAxis.BeginExplicitMotionProfileScope`(AsyncLocal)로 전달 |
| 07-27 | `0e88a9b7` | **Input/Output 시퀀스 분할**: InputSequence.Steps.{Load,Align,Review}, OutputSequence.{ActionPlanner,Safety,UnitCalls}, `Safety/FeederRetreatPolicy`(피더 후퇴 순서 단일 구현), `SEQUENCE_MAP.md` 신설 |
| 07-27 | `63202d69` | 출력 die 수령 캐싱, Separate 스텝/UI 제거 |
| 07-27 | `713ee25c` | UPH 개선 + 캘리브레이션 배치 저장 + **레시피 화면 즉시저장 개선**(Recipe/Config/Setup Scope 분기), Pick/PlaceRuntimeOffsetService 확장 |
| 07-27 | `9882daa6` | **PickUp 대기 피커 동적 선행 대기점**: 허가 대기 중 200ms 모니터가 배치 비전X 극값 기반 waitX로 1회 선행 이동. 레시피 `PICKUP DYNAMic WAIT`(기본 Off)/`DYNAMIC WAIT EXTRA MARGIN`. 신규: `InputDieVisionBatchCoordinateStore`, `InputCameraPreInspectionCoordinator.IsInspectionRunning` |
| 07-27 | `d481efb1` | **FollowMoveAsync 전면 재작성** (→ §4-3) |
| 07-27 | `b69adc1c` | **Place 복귀 X 동적 대기점 직행**: 복귀 X 목표를 조건 충족 시 대기점으로 치환(왕복 제거). 산출 코어를 `PickerSequenceBase.TryResolveDynamicPickUpWaitTargetX`로 공용화 |
| 07-27 | `82f62907` | **LOT ID 체계**: `LotSessionService`/`LotStorage`, 활성 LOT 없으면 Auto START 차단, LotHistoryDialog/InputStageRunReviewDialog, WorkMainPage LOT UI |
| 07-27 | `f46040ab` | 아웃풋 카세트 교체 수정 + lot-id docs 2건 |
| 07-27~28 | `f6be9262`→`3f809493` | 인풋 로딩/언로딩 실장비 테스트→완료: `InputCassetteInterlockRules` 신설, InputFeeder Load/Unload 시퀀스 대폭 보강, 로고 리소스 |
| 07-28 | `c2bf99af` | 인풋/아웃풋 로딩언로딩 검증: `OutputCassetteInterlockRules` 신설, MachineController +407줄, MachineReadySequence +573줄, OutputFeederUnloadToCassette 대폭 |
| 07-28 | `cc560ba4` | **UPH 3120**: 선행이동의 Position Override 전면 폐지 — **"위치 오버라이드는 팔로잉(FollowMove) 전용"** 정책 확정 (완료된 이동에 AxmOverridePos가 0을 반환하는 무효 성공 레이스 → MOVE JOIN -5 원인 제거) |
| 07-28 | `9db00294` | **UPH 3200 (A안)**: ① 첫 픽만 비전 RESULT 동기 회수, 나머지는 각 픽 직전 이연 확정(`EnsureCurrentPickTargetVisionResultAsync`). 미회수 항목은 "잠정 목표"(보정 없는 다이좌표, PickerX −1mm 보수 마진)로 **회피 클리어런스만** 산출 ② 픽업완료→바텀 퇴장 시 자기 픽커X를 리딩축으로 InputVisionX 팔로잉 진입(첫 명령 20mm 게이트, 대기 1000ms). ※ 팔로잉 중 **벨로시티 오버라이드는 잠정 보류**(잔여거리 짧을 때 축 비정상 거동 의심 — 코드 미반영, 커밋 메시지만) |
| 07-29 | `b6b23cf2` (HEAD) | 바텀 비전 고정 Y 기준을 보유 조합 무관 **P4 고정**으로 변경 (기존: 최상위 보유 피커 기준이라 4개/1개 배치에서 촬영 Y가 0.23~0.55mm 달라짐) |

---

## 3. Sequencing 레이어 (총 168개 .cs)

### 3-1. 실행 구조

```
MachineController.StartSequenceAsync
 └ AutoSequenceCoordinator.RunAsync — 4개 유닛을 Task.Run 병렬 기동
    ├ InputLoader  = InputSequence         (10스텝 고정 상태기계)
    ├ PickerFront  = FrontPickerSequence ─┐
    ├ PickerRear   = RearPickerSequence  ─┴ 루프마다 PickerProcessSequence 생성
    ├ OutputUnloader = OutputSequence      (매 사이클 ActionPlanner로 액션 선택)
    ├ WaferCompletionRunCoordinator 모니터 + InputVisionPrefetchRunner
    └ PickerFirstForwardSequencer.BeginRun (run 최초 전진 순서: Place>Bottom/Side>PickUp)
```

- **Picker 1사이클** (`PickerProcessSequence`, 145KB): CheckUnit → RunInputCameraMarkInspection(선행검사 허가) → RunPickUp → RunBottom/Side(통합: `PickerBottomAndSideInspectionSequence`) → RunPlace → Complete. 단계 진입마다 `PickerPhaseCoordinator`(phase lease) + `AutoSequenceCoordinatorGate`(WorkZone lease) 통과.
- **Input 10스텝**: Mapping→ResolveSlot→PrepareStageLoad→LoadFeederFromCassette→LoadFeederToStage→RecoverFeeder→AlignStage→DieMapping→ReviewStage(작업자 분기)→Complete. 새 웨이퍼 로딩 완료 직후(첫 Pick 전)가 콜렛 자동클리닝 실행 창.
- **Output**: 고정 순서 없음 — `ResolveNextOutputAction()`이 상태 기반으로 StoreNg/StoreGood/Resume/SupplyGood/SupplyNg/WaitReceive/Stop 중 선택, 전부 `CheckOutputWorkInterlocksBeforeExecute` + `ExecuteWithOutputPickerAvoidGateAsync` 경유.
- **Front/Rear 중재**: SequenceResourceManager(SemaphoreSlim 배타 리소스 9종) + PickerPhaseCoordinator + WorkZone/CameraZone 게이트 + `InputEntryQueue`(단조 티켓 FIFO — 라이브락 구조적 불가) + `InputCameraPickUpPermissionStore`(선행검사→픽업 허가). Rear는 Input 픽업 우선권을 Front에 양보.

### 3-2. Picker 폴더 핵심 (54개)

| 파일 | 담당 |
|---|---|
| `PickerSequenceBase.cs` (216KB) | 공통 기반: 모션/인터락 대기/작업영역 예약 API, **동적 대기점 산출 코어** `TryResolveDynamicPickUpWaitTargetX`, 런타임 AF, 진공/Flow |
| `PickerPickUpSequence.*` (9 partial) | 본체 / EntryMotion(진입, Conti 세그먼트, Z선하강) / ContinuousChecks(연속픽 Y유지 판정) / PickZMotion(Z 픽 동작·니들 싱크리프트) / **PickTargets(A안 RESULT 이연 회수·잠정 목표)** / MotionResolvers / InputStageMove / **VisionRetreat(비전 회피+픽커 팔로잉 진입)** / PickVerifyManual |
| `PickerPlaceSequence.*` (7 partial) | 본체 / OutputStageReady / PlaceTargets / ContiPlace / PlaceDown(RESULT 배리어→Z→진공Off→후검사 등록) / VisionRetreat / **Completion(동적 대기점 직행 `TryApplyPlaceReturnDynamicWaitTarget`)** |
| `InputDieVisionPrepareSequence.cs` (169KB) | 픽 배치 촬영 준비(BuildPickBatch→이동→촬영→오프셋 적용), 비전이 픽커를 추종 복귀 |
| `InputCameraMarkInspectionSequence.cs` | 선행(비침습) 마크검사 → EPD 직후 독립 회피 시작 → `GrantPickUpPermission` |
| `InputVisionXPrePositionCoordinator.cs` | PickUp 완료 후 InputVisionX 선행이동 세션(static). `FollowStartMinFirstMoveMm=20`, 오버라이드 사용 안 함(팔로잉 전용 정책) |
| `VisionIndependentRetreatCoordinator.cs` | EPD 직후 비전 자체 회피 세션 공유(Register/TryAdopt/Extend), `RetreatTargetExtraMarginMm=1.0` |
| `OutputPostPlaceInspectionQueue.cs` (124KB, OutputStage/) | Place 후검사 워커 큐 — Picker 퇴장 후 OutputPlaceArea 인수, Output 카메라 촬영·결과 적용·PlaceRuntimeOffset 갱신, OutputVisionX가 피커 추종 복귀 |
| `Pick/PlaceRuntimeOffsetService` | 폐루프 보정(EMA LowPass, Side×Picker 8세트×XYT, 클램프 X/Y ±0.5mm·T ±0.5°), Disable이어도 학습은 지속 |
| `DieCoordinateTransformService.cs` | Pick/Place 좌표 변환 단일 지점(런타임 오프셋 부호 반전 지점) |

### 3-3. 팔로잉(FollowMoveAsync) 사용처 5곳

| 방향 | 후행 | 선행 | 위치 |
|---|---|---|---|
| 픽커가 비전 추종 진입(Input) | PickerX | InputVisionX | `PickerPickUpSequence.VisionRetreat.cs:868` |
| 픽커가 비전 추종 진입(Output) | PickerX | OutputVisionX | `PickerPlaceSequence.VisionRetreat.cs:513` |
| 비전이 픽커 추종 복귀(Input) | InputVisionX | PickerX | `InputDieVisionPrepareSequence.cs:2922` |
| 비전이 픽커 추종 복귀(Output) | OutputVisionX | PickerX | `OutputPostPlaceInspectionQueue.cs:2235` |
| 선행이동 세션(픽업완료→바텀 퇴장) | InputVisionX | 자기 PickerX | `InputVisionXPrePositionCoordinator.cs:743` |

공통: 파라미터는 `SharedRailXMotionService.TryGetFollowGapParameters`(direction/homeGap/safetyGap) 런타임 조회, 속도는 호출부에서 `MotionSpeedScale.ApplyDefault*Scale` 1회 명시 적용, 타임아웃은 `ScaleDefaultTimeoutMs`(역수 확장). 실패 시 R6 폴백(회피 Task join 후 일반 이동 1회).

### 3-4. CycleStop / 완료 처리

- `Context.StopIfCycleStopRequested` → `SequenceStopException`. 픽커가 제품 보유 중이면 drain 지연 판정 5종(`ShouldDeferCycleStopFor*`).
- Wafer 완료: `WaferCompletionRunCoordinator`가 신규 Pick 차단 → 보유품/후검사/안전 복귀 완료 대기 → READY 종료.

---

## 4. Equipment 레이어

### 4-1. 조립과 컨트롤러

- `CDT320_Machine`: 유닛 트리(InputCassette/InputFeeder/InputStage/PickerFront/PickerRear/Vision/OutputCassette/OutputFeeder/OutputStage/OpPanel + Null 구현들).
- `MachineController`(10,924행): INIT/READY/START/STOP 오케스트레이터. 생성자에서 전역 정적 훅 — `BaseAxis.MotionGuard = VerifyAxisMotionGuard`, `MotionGuardRuntime.ContextProvider`, `SharedRailXMotionRuntime.ServiceProvider`. 축 목록 SSOT = 유닛 트리 리플렉션(`EnumerateAxes`).
- 초기화: `AxisInitializePlan`(Step 10~370, Input/Output 병렬 Lane, PickerY 페어 동시 HOME Step 150) → `AxisInitializeExecutor`/`AxisInitializeRuntime`.
- Auto 시작 게이트: 초기화 완료 + **활성 LOT 필수**(`EnsureActiveLotForAutoStart`) + 레티클 Avoid.
- 런타임 상태 영속화: 1s merge/5s retry 백그라운드 워커(픽커 오프셋, 카운터, HomeDone latch, 실린더, 초기화 Step).

### 4-2. 축 계층 (AjinAxis / AXM / MotionSpeedScale)

- **`AjinAxis`** (3,549행): 보드 호출은 인스턴스 `lock(_sync)` 안에서만. 시리얼 3종(`_motionStopSerial`/`_positionOverrideSerial`/`_motionStartBaseSerial`)으로 정지/오버라이드/모션 기준 추적.
  - **`ActualPosition` getter = `AXM.GetCommandPosition`(지령 위치) — 의도 설계.** 인터락/팔로잉/거리 계산 전부 지령 기준. 엔코더 실측은 `AXM.GetActualPosition` 직접 호출 또는 상태 스냅샷.
  - `MoveAbsoluteAsync`: SharedRail 축이면 서비스로 위임 → MotionGuard(-11) → 소프트리밋 → 발행 → `WaitUntilMoveDone`(시작 유예 5s, 기본 타임아웃 **300s TEST 임시**) → 최종 확인은 Command↔Target 톨러런스 1가지(오버라이드 발생 시 생략).
  - `TryOverridePosition`: 0/-4(정지 경합)/-11(dry-run 거부, 알람 없음)/-2. `AXM.ModifyPosition`에 **모션 시작 시리얼**을 넘겨 자기 모션일 때만 유효.
- **`AXM.ModifyPosition`**: EtherCAT `AxmOverridePos`는 relative(기준=모션 시작 위치). `MovePosition`이 시작 직전 Command를 기록·시리얼 발급, 불일치/기록없음/불합리하면 -2 거부 — 절대값이 상대값처럼 나가던 결함의 근본 차단.
- **`FollowMoveAsync`** (재설계 2026-07-27, 불변식 ①~⑥):
  ① 경계 = 선행축 실측만의 함수 `bound = leadingActual ± (homeGap − safetyGap)` ② 최초 이동 명령은 팔로잉당 정확히 1회(래치) ③ `lastCommanded` 대비 진행방향 전진일 때만 오버라이드 ④ 전 구간 Min(선행,후행) 단일 프로파일(증속 폐지) ⑤ 최종 목표 발행 후 오버라이드 영구 중단→완료 대기 ⑥ 이동 Task 조기 종료 시 -23 즉시 실패(R6 폴백 위임). 에러: -21 타임아웃/-22 선행축 알람/-23 조기종료. followVel/Acc/Dec ≤0이면 발행 전 실패(22:08 폭주 사고 봉쇄). `additionalConstraints`로 반대편 피커 페어도 매 폴링 클램프. `MinimumFollowSafetyGap=5.0`.
- **`MotionSpeedScale`**: `EffectiveScaleFactor` 우선순위 Ready 스코프 > Manual 스코프 > 전역 %. **스코프 깊이는 static int(AsyncLocal 아님 — 프로세스 전역)**. `AxisData`는 원본 getter를 `protected`로 봉인, `GetDefault*()`=스케일본/`GetRaw*()`=명시만 허용. `ScaleDefaultTimeoutMs`=역수 확장(상한 600s). Jog 속도는 스케일 미적용(`UnitJogVelocityResolver`).

### 4-3. 인터락 체계

- 진입점 `MotionGuardRuntime`(AsyncLocal 스코프 4종: AxisTeachingMove — 좌표 ±0.0001 일치 요구, Cylinder, ExecutionMode, PickerYPairHome). 차단 시 INTERLOCK 알람 + Blocked 로그.
- `MotionGuardRuleRegistry` 등록 순서: **PickerZone Facing-Y 최우선** → SharedRailX → InputCassette → InputFeeder → InputStage → Vision → PickerFront/Rear → OutputStage/Feeder/Cassette.
- `PickerZoneInterlockRules`(4,335행): 존 모델(Avoid/Input/Bottom+Side=Process/Output), 단일 `activeZoneLock`으로 활성 목표 존 + 존별 사용 카운터/소유자 보호. **`TryBeginPickerWorkAreaUseExclusive` = 확인+등록 원자화(M8)** — 늦은 쪽은 차단 아닌 대기.
- targetName 토큰 파서 `MotionGuardMoveIntent`: `AutoSequence`, `PickerZone=…`, `InputStageWorkAreaX`, `PickUpZHold=…` 등.
- 제3 분기(R5): 상대 SharedRail 축의 Actual/Command **양쪽 모두** 페어 간격식 SafetyDistance(RetreatExtra 미포함)를 만족하면 진입 허용.
- 실시간 감시: `RealtimeCollisionSupervisor`(10ms, 위험 시 등록 핸들러로 전축 하드정지).
- 구형 `MotionInterlock`/Standard/Extended 규칙군은 별도 레지스트리(MotionGuardRuleRegistry 미등록).

### 4-4. SharedRailX (공유 X 레일)

- 축 4: InputVisionX / FrontPickerX / RearPickerX / OutputVisionX. 페어 4쌍(비전↔픽커만; **Front↔Rear 픽커X는 페어 아님** — Y 대향 규칙 + FollowConstraint가 담당).
- 간격식 `clearance = HomeClearance − aSign·aPos − bSign·bPos`. 현재 부족해도 목표가 개선이면 허용(회피 방향), 목표 부족은 차단, 페어 미설정은 fail-closed.
- Config `Config\shared_rail_x.json`: SafetyDistance 10, Input/OutputVisionRetreatExtraClearance 40(회피 목표 계산 전용 — 인터락엔 미가산), VisionFollowEntryTimeoutMs 15000, HomeClearance In 19.0/Out 390.0.
- `TryResolveMinimalVisionRetreatTarget`(닫힌 수식, 구간 상충 시 전체 Avoid 폴백), `TryGetFollowGapParameters`(팔로잉 파라미터 SSOT), 그룹 이동은 소프트웨어 병렬(AXM 멀티축 API 미사용).

### 4-5. 자재/LOT/데이터

- `MaterialStateService`(8,476행, static): 단일 기준 상태. 락 4종 분리, 저장 스로틀(quiet 1s/min 5s), `D:\CDT-320\State\material_state.json`(+bak/recovery/tmp 원자 교체). Input Pick 예약·Output 수령 예약·Die 검사 반영·RunReview 승인 API. `SetProductionLotId` 하나로 CSV/전문/통계/화면 일원화.
- `LotSessionService`: LOT 시작/완료 단일 진입점, 시작 검증 실패 시 부분 적용 금지, 기동 시 Material 복구 프롬프트 **이후** 복원(순서 고정).
- Recipe: `RecipeStore`(`Recipes\*.Project`), 유닛별 파생 맵(`RecipeMapBuildService`), 정합성(`RecipeProjectConsistencyService` — 로드만으로 저장하지 않음).

### 4-6. Vision 브릿지 (외부 Vision PC)

- `VisionHub` 6채널 TCP(5100 Wafer/5101 BottomInspection/5103 Bin/5104 Main/5105 FrontSide/5106 RearSide) + 뷰어 스트림 별도 포트(52xx). 재연결 워치독은 알람 없이 재시도.
- 결과는 Vision이 자발 Push(MRESULT/RESULT) → `VisionInspectionResultStore`/`BottomXytStore`(키=die_index). `AutoVisionRequestService`가 요청 상관.
- `VisionCameraCalibrationTransform.ToBottomVisionOffset`: Place 보정 전용 `bottom_item_offset_*`과 Side 각도용 `BottomCenterOffset*`을 **의미별 분리** 보관. `sideVisionYOffset`/`pickerZOffset`은 현재 0.0 고정 + TODO(실장비 로그 확인 후 연결 예정).

---

## 5. UI 레이어

- `Program.cs`: 단일 인스턴스 Mutex, 1ms 타이머, 전역 예외→크래시덤프, `--auto-cycle/--auto-init/--start-page` 등 인수.
- `Form1`(3,654행): 기동 순서 = 설정→Ajin Open→IO/실린더→Vision 6채널→Machine→Controller→Material 복구 프롬프트→**LOT 복원**→모니터 4종(Motion 50ms/Collision 10ms/IO 10ms/OpPanel)→SECS(5000)→6탭 생성. **자동 로그인 `admin/Admin` (TEST 임시)**. 종료는 EXIT 버튼만 허용, `OnFormClosing`에서 Material flush → **`SaveMachineSettings()`= EquipmentData Setup/Config 전체를 메모리 값으로 재작성**(IO 포함 — "IO Setup은 앱 종료 시 덮어써진다"의 코드 근거) → 런타임 상태 저장 → 서비스 역순 Dispose.
- 탭: Work(운전+INIT/READY/START/STOP/CYCLE RUN)·WorkInfo(유닛별 상태/수동)·History·Recipe·Settings·User. 페이지는 lazy 생성+캐시.
- **레시피 저장 방식(07-27 개선)**: 그리드 `ParameterValueChanged`에서 **즉시 저장** — `Scope.Recipe`→활성 레시피, `Scope.Config/Setup`→`SaveMachineSettings()`. 별도 SAVE 버튼 없음. 활성 레시피 없으면 예외로 차단.
- Picker 레시피 페이지: 티칭 위치(Recipe) + 모션 스위치(Config: PICKUP TRANSFER MODE, Z PRE DOWN, **PICKUP DYNAMIC WAIT**, PLACE CONTI 계열…) + Setup(피치/클리어런스). 기구 보정은 운전 중 변경 차단.
- 시퀀스 호출 3패턴: ① Work 탭 `RunSafe`→Controller ② 페이지 `RunSafeAsync`→`BeginManualSequenceProcessMove` 스코프+유닛 메서드 ③ 다이얼로그 `BeginManualActionScope`+LinkedCTS+시퀀스 직접 실행(콜렛클리닝 등). UI 복귀는 `ConfigureAwait(true)`+`InvokeRequired/BeginInvoke` 표준 가드.
- 주요 다이얼로그: ColletCalibrationDialog(2,989행 — BATCH 측정, PARAMETER SAVE/SAVE 분리), ColletCleaningControlDialog(설정 21항목+트리거 3종), ManualSequenceDialog(Input/Output Load·Unload, PickUp/Bottom/Side/Place 수동, 속도 % 저장), LotHistoryDialog, InputStageRunReviewDialog, MotionTestDialog.

---

## 6. 설정/영속 파일 지도

| 파일 | 위치 | 내용 |
|---|---|---|
| `settings.json` | `<AppBase>\Config\` | AppSettings: Simulation/DryRun/UseAjin/UseVision, **속도 3종 %**(Default/Manual/Ready), PickerMotionOnlyTestMode, WaferCompleteRunMode, 로그 옵션, 포트. `[OnDeserializing]` 하위호환 |
| `shared_rail_x.json` | `<AppBase>\Config\` | SharedRailX 페어/SafetyDistance/RetreatExtraClearance/**VisionFollowEntryTimeoutMs** (팔로잉·회피 관련 설정의 실제 위치) |
| `io_settings.json` | **`D:\CDT-320\Config\` 하드코딩** | 포트별 시뮬 토글(IoListPage 저장 시에만 기록 — 종료 시 미기록) |
| EquipmentData `Setup\*.json`/`Config\*.json` | `<AppBase>\EquipmentData\` | 유닛/컴포넌트(축·IO 포함) Setup/Config — **앱 종료 시 전체 덮어쓰기**. 파일명=컴포넌트 Name |
| `Recipes\*.Project` + 유닛 레시피/맵 | `<AppBase>\Recipes\` | 레시피. LotId도 Project에 기록 |
| `material_state.json` | `D:\CDT-320\State\` | 자재 스냅샷(원자 교체+백업) |
| `Log\Lots\*.json` | `<AppBase>\Log\Lots\` | LOT 이력 |
| motion_axes / 캘리브레이션 / 클리닝 트리거 | Config/State | 사용자 편집 축값, CalibrationData, 클리닝 카운터 영속 |

`<AppBase>` = 실행 폴더(운영 배포는 `D:\CDT-320\`).

---

## 7. 관례·규약 (신규 코드 작성 시 준수)

- 시퀀스/모션 표준 반환 `Task<int>`: 0 성공 / -1 인자·예외 / -2 미준비 / -3 타임아웃 / -4 정지·경합 / -5 Command↔Target 불일치 / -11 인터락 / -21·-22·-23 팔로잉 전용. 라이브러리 계층 `ConfigureAwait(false)`, UI 계층 `ConfigureAwait(true)`.
- 이동 3단 분리(명령 발행→결과 확인→완료·위치 검증), 병렬은 WhenAll 2회(명령/완료).
- **속도는 반드시 스케일 경유**: 기본 경로는 `GetDefault*()` 자동, 팔로잉/명시 경로는 호출부 1회 적용+`ScaleDefaultTimeoutMs`. `GetRaw*` 신규 사용 금지(정당 사유 없이).
- 로그: `Log.Write(카테고리, 사용자, 코드, "한국어 메시지 … - Start/Ok/Check/Failed")` + 수치 계측 포함(발동/스킵/조인 사유). 안전 조건 완화 시 조건 위에 한글 사유 주석.
- IDisposable 스코프 패턴(BeginXxx…using), static store는 최내측 lock, 백그라운드 Task는 반드시 observe.
- WinForms: Designer 인라인, 컨트롤 prefix(btn/lbl/txt/…), `컨트롤명_이벤트명`. 새 .cs는 csproj `<Compile Include>` 수동 등록.
- 저장: JSON pretty(`JsonPrettySerializer`), 스냅샷 원자 교체, 직렬화 키 변경 시 마이그레이션.

---

## 8. 미검증·보류·주의 항목 (현재 코드 기준)

| # | 항목 | 위치/근거 |
|---|---|---|
| 1 | **팔로잉 중 벨로시티 오버라이드 잠정 보류** — 잔여거리 짧을 때 축 비정상 거동 의심(미검증). 코드에는 미반영, 커밋 메시지에만 기록 | `9db00294` 메시지 |
| 2 | 일반 이동 기본 타임아웃 300s는 "TEST 임시 기준" — 현장 TEST 후 거리/속도 기반으로 재조정 예정 | `AjinAxis.cs:540~542` |
| 3 | `MachineReadySequence` 활성 스텝 8개 외 나머지(OutputStage/Feeder/Cassette Avoid 등)는 주석 보관("확인하면서 활성화하자") | `MachineReadySequence.cs:64~72` |
| 4 | Form1 자동 로그인 admin(TEST 임시) — Release에서도 Admin 진입 | `Form1.cs` (i18n 부착 직후) |
| 5 | Side 비전 `sideVisionYOffset`/`pickerZOffset` 0.0 고정 + TODO(실장비 로그 확인 후 연결) | `VisionCameraCalibrationTransform.cs` |
| 6 | `MotionSpeedScale` Manual/Ready 스코프는 프로세스 전역(static) — Auto 병행 상황(Review 등)에서는 의도적으로 잡지 않음 | `MotionSpeedScale.cs`, `MachineController.cs:6374` |
| 7 | 선행검사 허가 대기 side당 1ms 폴링 — 기능 문제 없음, CPU/로그 개선 여지(무변경 관찰) | 동적 대기점 레포트 §5 |
| 8 | SECS는 프레임 골격만(실 SECS-II 인코딩 미구현) | `Equipment\Secs\` |
| 9 | `PickerPickUpTransferMotionMode` 값 3(FastConti)은 삭제됨 — 역직렬화 시 Default로 정규화 | `PickerTransferTypes.cs` |
| 10 | 후검사 X +1.15mm는 측정기준 상수(다이-포켓 아님) — 방치 결정, PlaceRuntime 필터 Enable 금지 조건과 연동 주의 | 메모리/07-27 결정 |

---

*이 문서는 git 미추적 분석 산출물이다. 삭제해도 무방하며, 장기 보존이 필요하면 `docs/` 이하로 이동한다 (AGENTS.md §14).*
