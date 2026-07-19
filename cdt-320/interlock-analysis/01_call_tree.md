# CDT-320 인터락 구조 — 전체 호출 트리 (마스터 문서)

- 분석일: 2026-07-19, `master` 브랜치 (읽기 전용 분석, 코드 수정 없음)
- 상세 문서: [02_picker_zone_rules.md](02_picker_zone_rules.md) / [03_picker_front_rear_rules.md](03_picker_front_rear_rules.md) / [04_input_vision_rules.md](04_input_vision_rules.md) / [05_output_common_runtime.md](05_output_common_runtime.md)

---

## 1. 인터락 3계층 구조

CDT-320의 인터락은 서로 다른 시점에 동작하는 3개 계층으로 구성된다.

| 계층 | 시점 | 구현 | 실패 시 |
|---|---|---|---|
| **① MotionGuard** (사전 게이트) | 모든 축/실린더 이동 명령 직전 | `MotionGuardRuleRegistry` 11개 규칙 | 이동 거부(-11) + INTERLOCK 알람 |
| **② 시퀀스 내장 검사** (사전+대기) | 시퀀스 스텝 진행 중 | `PickerSequenceBase` 이동 헬퍼, `AutoSequenceCoordinatorGate`, 자원/존 임대 | 대기(폴링) 또는 스텝 실패 |
| **③ RealtimeCollisionSupervisor** (사후 감시) | 상시 (Form1에서 10ms 주기 기동) | Front/Rear X 거리 실시간 감시 | **전축 하드정지(EStop)** + Critical 알람 |

②는 ①과 같은 판정 함수(`PickerZoneInterlockRules.CanMove...`, `MotionGuardRuntime.CanAxisTeachingMove` dry-run)를 공유해 "대기 폴링과 실제 이동이 동일 규칙"이 되도록 설계됨.

---

## 2. ① MotionGuard 사전 게이트 — 전체 호출 트리

```
[모든 축 이동의 시작점 — QMC.Common\Motion\BaseAxis.cs]
BaseAxis.MoveAbsoluteAsync (401)  / HomeSearchAsync (529) / Jog (769,781,885,892)
 └ VerifyMotionGuard(targetPos, moveKind)                      BaseAxis.cs:824
    ├ MotionGuardBypassDepth > 0 → 통과   ★바이패스 지점 (아래 §5)
    ├ MotionGuard 델리게이트 null → 통과 (Handler 미기동 상태)
    └ MotionGuard(this, target, kind, out reason)
       │   ← MachineController 생성자(488)에서 연결:
       │      BaseAxis.MotionGuard = VerifyAxisMotionGuard
       │      BaseCylinder.MotionGuard = VerifyCylinderMotionGuard
       ▼
[MachineController.cs:570 VerifyAxisMotionGuard]
 ├ Home          → MotionGuardRuntime.VerifyAxisHome
 ├ JogContinuous → MotionGuardRuntime.VerifyAxisContinuousJog
 ├ JogStep       → MotionGuardRuntime.VerifyAxisStepJog
 └ Absolute      → MotionGuardRuntime.VerifyAxisMove
       ▼
[MotionGuardRuntime.cs — static 런타임]
 ├ Enabled=false 또는 axis null → 통과
 ├ IsAxisAlreadyAtTarget → 통과 (이미 목표 위치)
 ├ ContextProvider() → MotionGuardContext(machine, 전체 축, 전체 실린더)
 ├ AsyncLocal 스코프 해석:
 │   ├ CurrentAxisMoveScope   (BeginAxisTeachingMove로 설정) → TeachingMove로 격상
 │   ├ CurrentExecutionModeScope (BeginAuto/ManualSequenceProcessMove) → 실행모드 태깅
 │   └ CurrentCylinderMoveScope (BeginCylinderInitializeMove)
 └ MotionGuardService.VerifyMove(...)
       ▼
[MotionGuardService.cs:241 VerifyMove]
 ├ movingKey = InterlockCheckMatrix.NormalizeName(movingName)   ← 별칭 정규화
 ├ checks = matrix.GetChecksFor(movingKey)                      ← 로그용 체크 카탈로그
 ├ effectiveMoveKind 결정:
 │   Auto/Manual시퀀스 모드 + AxisMove → AxisTeachingMove로 격상
 │   Jog → AxisMove로 정규화
 ├ ★ MotionGuardRuleRegistry.Verify(request) — 실제 차단 판정
 │      │  [등록 순서 = 검사 순서, 하나라도 false → 즉시 차단]
 │      │  MotionGuardRuleRegistry.cs:127 static 생성자에서 등록
 │      ├ 1. PickerZoneInterlockRules.VerifyFacingYDistanceFirst   ← 1차 거리 인터락(최우선)
 │      ├ 2. SharedRailXInterlockRules.Verify → SharedRailXMotionService.VerifySingleAxisMove
 │      ├ 3. InputCassetteInterlockRules.Verify   (InputLifterZ)
 │      ├ 4. InputFeederInterlockRules.Verify     (FeederY/Lift/Clamp)
 │      ├ 5. InputStageInterlockRules.Verify      (StageY/T, ExpanderZ, InputVisionX, NeedleX/Z, EjectPinZ)
 │      ├ 6. VisionInterlockRules.Verify          (SideVisionY F/R, Reticle 실린더 3종)
 │      ├ 7. PickerFrontInterlockRules.Verify     (FrontPicker X/Y/T0~3/Z0~3)
 │      ├ 8. PickerRearInterlockRules.Verify      (RearPicker X/Y/T0~3/Z0~3)
 │      ├ 9. OutputStageInterlockRules.Verify     (GoodStageY/Z, NGStageY, OutputVisionX, BinGuide 실린더 6종)
 │      ├ 10. OutputFeederInterlockRules.Verify   (FeederY/Lift/Clamp)
 │      └ 11. OutputCassetteInterlockRules.Verify (OutputLifterZ)
 └ 통과 시 checks 목록을 Message로 반환 (로그 "- Check")
       ▼
[차단 시 처리 — MotionGuardRuntime]
 ├ AlarmManager.Raise(Error, "INTERLOCK", axisName, reason)
 ├ Log.Write("Main","INTERLOCK","MotionGuard", reason + " - Blocked")
 └ BaseAxis는 -11 반환 + RecordMotionFailure
```

### 각 규칙 파일의 내부 분기 (요약)

각 규칙은 `MotionGuardRuleHelpers.IsMoving(request, "축이름", ...)`으로 자기 담당 축인지 판별 → 담당 아니면 `true`(통과). 담당이면 MoveKind별 분기:

| MoveKind | 의미 | 함수 명명 |
|---|---|---|
| `AxisTeachingMove` | 자동/시퀀스 이동 | `CanAuto...` |
| `AxisMove` | 수동 이동 | `CanManual...` |
| `AxisHome` | 원점 복귀 | `CanHome...` |
| `AxisContinuousJog`/`AxisStepJog` | 조그 | `CanJog...` (또는 Manual 내 Jog 분기) |
| `CylinderMove`/`CylinderInitialize` | 실린더 | `CanMove.../CanInitialize...` |
| 그 외 | 미지원 | `BlockUnsupportedMoveKind` (MOTION-GUARD 알람) |

### 규칙 간 의존 (PickerZone이 허브)

```
PickerZoneInterlockRules  ← 공유 판정 허브 (존/거리/점유 상태의 SSOT)
 ▲ VerifyPickerXStoppedForClearanceMechanismMove ← InputFeeder(2), OutputFeeder(2), InputStage(1), OutputStage(1)
 ▲ IsPickerBlockingZoneTransport(± ForFeederHome) ← Input/OutputFeeder 규칙 + 다수 시퀀스
 ▲ VerifyPickerZAtOrAboveZeroForZoneStageZMove ← InputStage(ExpanderZ), OutputStage(GoodStageZ)
 ▲ ResolvePickerZoneTransportState ← InputStage, OutputStage, MachineController, Picker 시퀀스들
 ▲ VerifyFront/RearPickerXMove·YMove·YJogFacingMove, VerifyPickerXOppositeYClearance,
   VerifyPickerYHomePairSafety, VerifyPickerXGlobalMachineClearance ← PickerFront/Rear 규칙
InputCassette·Vision 규칙은 PickerZone 미참조 (완전 독립)
```

---

## 3. ② 시퀀스 계층과의 연결

시퀀스는 이동 전 **실행 모드 스코프**를 열어 MotionGuard 판정을 태깅한다 (42개 파일, 75개 사용처):

```
UnitSequenceBase.RunAsync
 └ using MotionGuardRuntime.BeginAutoSequenceProcessMove(reason)   ← AsyncLocal 스코프
     → 이 흐름의 모든 AxisMove가 AxisTeachingMove(=CanAuto...)로 격상 판정

PickerSequenceBase.MovePickerAxisAndVerifyAsync 등
 ├ MotionGuardRuntime.BeginAxisTeachingMove(axis, target, targetName)  ← 목표명(Intent 토큰) 전달
 │    targetName 예: "DiePickPosition;PickerZone=Input;AutoSequence"
 │    → MotionGuardMoveIntent.Parse가 PickerZone/보정/검사 플래그 해석
 ├ MotionGuardRuntime.CanAxisTeachingMove(...)   ← dry-run (알람 없이 통과 여부만)
 │    → 대기 폴링과 실제 이동이 동일 인터락 규칙 공유
 └ PickerZoneInterlockRules.CanMovePickerAxisByFacingYInterlock 직접 호출 (사전 대기)

존 점유 (이동 게이트와 별개의 논리 점유):
 ├ PickerFront/RearUnit → PickerZoneInterlockRules.BeginPickerZoneMove (Y 전진 시 목표 존 선언)
 ├ 검사 시퀀스 → BeginPickerWorkAreaUse (Bottom/Side 작업영역 점유)
 └ AutoSequenceCoordinator/MachineReadySequence → ClearPickerWorkAreasForReadyIfSafe (안전 확인 후 해제)
```

UI(레시피 페이지 등)도 `MotionGuardRuleRegistry.Verify`를 직접 호출해 사전 검증에 사용 (OutputCassette/OutputStageRecipePage).

---

## 4. ③ RealtimeCollisionSupervisor (사후 안전망)

```
Form1_Load → CollisionSupervisor.Start(10ms) + SetStopAllAxesHandler(전축 정지)
MonitorLoopAsync (백그라운드 Task, 10ms 주기)
 └ EvaluateRealtime:
    양쪽 PickerY가 모두 비후퇴(Forward/Moving/Unknown) && X경로가 안전거리(기본 150) 안
    → RaiseHardStop: 전축 EStop(폴백: Picker X/Y EStop) + Critical 알람 "PICKER-FACING-X-INTERLOCK"
```
사전 차단이 아니라 **①·②가 뚫렸을 때의 마지막 안전망**. `CanMove()`는 항상 Allow.

---

## 5. 바이패스 지점 (수정 계획 시 반드시 확인)

| 위치 | 방식 | 용도 |
|---|---|---|
| [AjinAxis.cs:987](../../QMC.CDT-320/Equipment/Ajin/AjinAxis.cs) | `BeginMotionGuardBypass()` + `BeginForceMoveScope()` | 드라이버 내부 재시도/보정 이동 |
| [SharedRailXMotionRuntime.cs:442](../../QMC.CDT-320/Equipment/Motion/SharedRailX/SharedRailXMotionRuntime.cs) | `BeginMotionGuardBypass()` | 그룹 이동 내부 디스패치 (규칙2가 `IsInternalDispatch`로도 이중 우회) |
| SharedRailXMotionService.cs:579 / SharedRailXMotionRuntime.cs:369 | `BeginForceMoveScope()` | 이미 위치해도 강제 이동 |
| Input/OutputCassetteUnit (2168/1964) | `BeginForceMoveScope()` | 카세트 리프터 강제 이동 |
| `MotionGuardRuntime.Enabled = false` | 전역 스위치 | (호출처 확인 필요 — 발견 시 위험) |
| `ShouldBypassHardwareMechanismChecks()` | Sim/DryRun/보드미준비 | Feeder Unclamp 등 실HW 검사만 우회 (위치 검사는 유지) |

---

## 6. 레거시 / 죽은 경로 (중요 발견)

1. **`InterlockRegistry` + `MotionInterlock` 20개 클래스 — 완전 죽은 경로**
   - `StandardInterlocks.cs`(5개) + `ExtendedInterlocks1~3.cs`(15개)의 `MotionInterlock` 파생 클래스는 **어디서도 인스턴스화/등록되지 않음** (`new XxxInterlock` 0건)
   - `InterlockRegistry`는 항상 빈 리스트 → [MachineController.cs:2392](../../QMC.CDT-320/Equipment/MachineController.cs)의 `MoveAxisAsync` 내 `InterlockRegistry.VerifyMove`는 **항상 통과**
   - `SystemSelfTestDialog.cs:147`이 `registered=0`을 그대로 보고
   - → 실질 보호는 MotionGuard 경로가 담당 중. 이 클래스들의 조건(EjectVsStage, DoorVsAll, EmgStopVsAll, ServoOff, VacuumVsPicker 등)이 MotionGuard 규칙에 모두 흡수됐는지 **갭 확인 필요** (예: Door/EMG/Servo Off/Vacuum 조건은 11개 규칙에서 미발견 → 별도 경로(AlarmResponseService/OperationPanel)로 처리되는지 확인 대상)

2. **규칙 내부의 무력화된 조건들** (상세는 각 문서 참조)
   - `InputCassetteInterlockRules.IsWaferFeederYSafeForWaferLifterZ` → 강제 `true` (Todo 주석)
   - `VisionInterlockRules.VerifyVisionNotBusy` → 내부 전부 주석, 항상 true
   - `PickerFront/Rear.Verify*PickerNotBusy` → 전부 주석, 항상 true
   - PickerFront/Rear의 상대 PickerY Avoid 검사 → 호출부 주석 (PickerZone 1차 거리 인터락으로 대체된 것으로 보임)
   - Input/OutputFeeder Home의 일반 ZoneTransport 검사 → Home 전용 완화 버전으로 대체

---

## 7. 핵심 임계값 요약 (수정 계획의 파라미터 후보)

| 값 | 위치 | 의미 |
|---|---|---|
| **PickerYFacingXClearance = 300/150 (Max→300)** | Picker Setup | Y 동시 전진 시 X 최소 안전거리 |
| **PickerYOutDistance = 1.0** | Picker Setup | Y "돌출" 판정 거리 |
| **DefaultTolerance = 0.05** | 규칙 공통 fallback | 위치 비교 (Output측 일부는 0.01) |
| **1µm (0.000001)** | PickerZ 하강 판정 | Z 하강 미세 판정 |
| **AutoProcessCorrectionMax = 2.0** | PickerZone | 자동 보정 X 이동 한계 |
| **FineAlignMaxXyMoveMm = 0.2** | Vision Config | 콜렛 미세정렬 이동 한계 |
| **0.5** | 실린더 규칙 | Fwd/Bwd 방향 판정 경계 |
| **감시주기 10ms / clearance 150** | Supervisor | 실시간 감시 |
| ZoneX 엔코더 존 + ZoneTolerance 1.0 | PickerZoneXSetup | X 존 판정 |
| interlock-check-matrix.json (111쌍) | Config\ | 체크 카탈로그 (로그용, 차단 아님) |

---

## 8. 인터락 조건 수정 시 영향 범위 체크리스트

1. **PickerZoneInterlockRules를 고치면** → 11개 규칙 중 7개 + Picker 시퀀스 + Coordinator Gate + Supervisor(같은 Y안전 판정 `IsPickerYSafeByPosition` 공유)까지 파급. 반드시 §2 트리와 02 문서의 호출자 표 확인
2. **MoveKind 격상 규칙** (Auto모드에서 AxisMove→TeachingMove) 변경 시 → 모든 CanAuto/CanManual 분기 의미가 바뀜
3. **targetName(Intent 토큰) 형식 변경 시** → `MotionGuardMoveIntent.Parse` + 42개 시퀀스 파일의 targetName 문자열 전수 확인
4. **규칙 등록 순서 변경 시** → 1번(FacingY 1차 거리)이 최우선인 설계 의도 유지 (AGENTS.md §4: 인터락은 최후 안전망, 시퀀스가 위험 궤적을 먼저 발행하지 않는다)
5. **완화(조건 제거) 시** → AGENTS.md §3: "안전 조건을 느슨하게 바꿀 때는 조건 바로 위에 변경 이유와 기계적 전제를 설명하는 한글 주석" 필수, 사전 분석·보고·승인 필요
6. **Supervisor는 건드리지 않아도 동작** — MotionGuard를 완화해도 Supervisor가 하드정지시킬 수 있음 (반대로 Supervisor 값만 바꾸면 사전 게이트와 불일치 발생)
