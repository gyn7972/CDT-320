# RealtimeCollisionSupervisor 구현 계획안

작성일: 2026-07-03  
대상: CDT-320 듀얼 Picker / Camera X축 실시간 충돌 방지  
관련 설계:

- `REALTIME_COLLISION_SUPERVISOR_DESIGN.md`
- `PICKER_DISTANCE_SAFETY_MONITOR_DESIGN_2026-07-03.md`

---

## 1. 최종 방향

현재 문제는 단순히 Picker 시퀀스 순서를 고치는 문제가 아니다. 최종 안전 보장은 시퀀스가 아니라 별도 안전 레이어가 가져야 한다.

최종 구조는 다음과 같다.

```text
RealtimeCollisionSupervisor
├─ Predictive Gate
│  └─ 이동 전 Allow / Wait / Block 판정
├─ Reactive Guard
│  └─ 상시 actual encoder 감시, 위반 시 정지 + 알람
├─ MotionSafetyState
│  └─ Front/Rear phase, Y, Z, Carrying, 검사존 lock 상태
└─ Distance Calculator
   └─ SharedRailX와 같은 HomeClearance/sign 기반 거리 계산
```

중요 원칙:

- `zone`은 공정 상태 설명용이다.
- 최종 충돌 판정은 actual encoder 기반 물리 거리다.
- 오토/메뉴얼/Jog/Teaching은 같은 최종 감시를 통과해야 한다.
- 오토 예외는 거리 안전 조건을 통과한 경우에만 허용한다.
- 재시작은 이전 step만 믿지 않고 실제 기구 상태를 재구성한 뒤 진행한다.

---

## 2. 현재 코드 적용 지점

### 2.1 기존 사전 인터락

파일:

- `QMC.CDT-320/Equipment/Interlocks/Common/MotionGuardRuntime.cs`
- `QMC.CDT-320/Equipment/Interlocks/Common/MotionGuardService.cs`
- `QMC.CDT-320/Equipment/Interlocks/Common/MotionGuardRuleRegistry.cs`

역할:

- 이동 명령 전 `MotionGuardRuleRegistry.Verify(...)` 호출.
- 신규 supervisor의 Predictive Gate를 이 경로에 붙인다.

계획:

```text
MotionGuardRuleRegistry
-> RealtimeCollisionSupervisor.VerifyMove(...)
-> Allow / Wait / Block 판정
```

초기 적용에서는 기존 rule을 제거하지 않고 supervisor rule을 가장 먼저 실행한다.

### 2.2 Picker zone / Y 전진 인터락

파일:

- `QMC.CDT-320/Equipment/Interlocks/PickerZoneInterlockRules.cs`

문제:

- `CanMovePickerYByFacingYInterlock(...)`는 Y 이동 시작 시점 검사에 가깝다.
- `CanShareForwardY(...)`가 거리 안전보다 앞서면 위험하다.
- `activeTargetZone` 타이밍에 의존한다.

계획:

```text
1차: 기존 로직 유지 + supervisor 거리 판정 선행
2차: Y/X 거리 판정은 supervisor로 위임
3차: zone 기반 예외는 Wait/공정 상태 보조로만 사용
```

핵심 수정 방향:

- `CanShareForwardY()`는 최종 Allow 조건이 될 수 없다.
- 다른 zone이라도 Front/Rear X actual clearance가 부족하면 상대 Y 전진 금지.
- X가 멀어질 target이어도 actual clearance 확보 전에는 Y 전진 금지.

### 2.3 SharedRailX

파일:

- `QMC.CDT-320/Equipment/Motion/SharedRailX/SharedRailXConfig.cs`
- `QMC.CDT-320/Equipment/Motion/SharedRailX/SharedRailXConfigStore.cs`
- `QMC.CDT-320/Equipment/Motion/SharedRailX/SharedRailXMotionService.cs`

현재 강점:

- `HomeClearance`, `AxisATowardSign`, `AxisBTowardSign`, `SafetyDistance` 계산 구조가 이미 있다.
- `SharedRailXAutoMoveGuard`가 이동 중 실시간 guard를 일부 수행한다.

문제:

- guard가 특정 SharedRailX move 동안만 살아 있다.
- FrontPickerX <-> RearPickerX pair가 기본 설정에 없다.
- PickerY 전진 상태와 연동되지 않는다.

계획:

- SharedRailX clearance 계산식을 공통 calculator로 분리한다.
- supervisor가 같은 계산기를 사용한다.
- `FrontPickerX <-> RearPickerX` pair를 `PickerYFacing` rule로 추가한다.
- 기존 `SharedRailXAutoMoveGuard`는 당장 제거하지 않고, supervisor 안정화 후 중복 정리한다.

### 2.4 시작/정지/알람 연결

파일:

- `QMC.CDT-320/Form1.cs`
- `QMC.CDT-320/Equipment/MachineController.cs`
- `QMC.CDT-320/Equipment/Alarms/AlarmResponseService.cs`

계획:

- `Form1_Load` 또는 `MachineController` 생성 이후 supervisor 시작.
- app 종료 시 supervisor dispose.
- 위험 감지 시 `MachineController.StopInterferenceGroupAsync(...)` 또는 `StopAllAxesAsync(...)` 호출.
- 알람은 `AlarmManager.Raise(AlarmSeverity.Critical, ...)`로 발생.
- 장비 상태는 Alarm으로 진입.

초기 정지 정책:

```text
Front/Rear Picker 위험:
FrontPickerX, FrontPickerY, RearPickerX, RearPickerY 정지

Camera/Picker 위험:
해당 CameraX, 해당 PickerX, 해당 PickerY 정지
```

실장비 최종 정책은 사용자 결정에 따라 전축 하드정지로 강화 가능.

---

## 3. 신규 타입 설계

### 3.1 RealtimeCollisionSupervisor

위치:

```text
QMC.CDT-320/Equipment/Interlocks/Runtime/RealtimeCollisionSupervisor.cs
```

주요 API:

```csharp
public sealed class RealtimeCollisionSupervisor : IDisposable
{
    public void Start();
    public void Stop();
    public CollisionGateResult CanMove(CollisionMoveRequest request);
    public void SetPickerPhase(PickerSafetySide side, PickerSafetyPhase phase, bool carrying);
    public IDisposable AcquireInspectZoneLock(InspectZoneKind zone, string owner);
    public ReconcileSafetyResult ReconcileSafeState(string reason);
}
```

### 3.2 CollisionGateResult

```csharp
public enum CollisionGateDecision
{
    Allow,
    Wait,
    Block
}
```

의미:

- `Allow`: 즉시 이동 가능.
- `Wait`: 오토에서는 대기, 수동에서는 사용자에게 대기 사유 표시. 알람 아님.
- `Block`: 구조적으로 위험하거나 수동 오조작. 이동 금지.

### 3.3 MotionSafetyState

```csharp
public sealed class MotionSafetyState
{
    public PickerSafetySnapshot Front { get; set; }
    public PickerSafetySnapshot Rear { get; set; }
    public IReadOnlyList<AxisPairSafetySnapshot> Pairs { get; set; }
}
```

side별 상태:

```text
Phase
YState
ZState
Carrying
ForwardYPermitted
CorrectionWindow
WorkArea
```

### 3.4 Distance Calculator

위치:

```text
QMC.CDT-320/Equipment/Interlocks/Runtime/CollisionDistanceCalculator.cs
```

계산식:

```text
clearance = HomeClearance
          - AxisATowardSign * AxisA.ActualPosition
          - AxisBTowardSign * AxisB.ActualPosition
```

중요:

- raw encoder 단순 차이 사용 금지.
- 실제 HomeClearance/sign 파라미터가 없는 pair는 unsafe 또는 Block으로 취급.

---

## 4. 파라미터 계획

### 4.1 SharedRailX 확장

기존:

```json
{
  "AxisA": "OutputVisionX",
  "AxisB": "FrontPickerX",
  "HomeClearance": 390,
  "AxisATowardSign": -1,
  "AxisBTowardSign": 1,
  "SafetyDistance": 10
}
```

추가 후보:

```json
{
  "AxisA": "FrontPickerX",
  "AxisB": "RearPickerX",
  "HomeClearance": 500,
  "AxisATowardSign": 1,
  "AxisBTowardSign": -1,
  "SafetyDistance": 150,
  "RuleKind": "PickerYFacing",
  "RealtimeGuardEnabled": true
}
```

주의:

- `HomeClearance=500`, sign은 예시다.
- 실제 장비 기준으로 Front/Rear 홈 위치와 서로 접근 방향을 확인한 뒤 입력해야 한다.

### 4.2 Supervisor config

별도 파일 후보:

```text
Config/realtime_collision_supervisor.json
```

필드:

```json
{
  "Enabled": true,
  "MonitorPeriodMs": 20,
  "LogThrottleMs": 1000,
  "UnsafeDebounceCount": 1,
  "StopPolicy": "InterferenceGroup",
  "PickerYFacingClearance": 150.0,
  "PickerYOutDistance": 1.0,
  "CorrectionWindowX": 2.0,
  "CorrectionWindowY": 2.0,
  "CorrectionWindowT": 2.0
}
```

권장:

- 초기 시뮬 검증: StopPolicy=`LogOnly` 또는 `InterferenceGroup`
- 실장비 최종: StopPolicy=`AllAxes` 또는 사용자 승인 정책

---

## 5. 핵심 규칙 보강

### 5.1 Y 전진 허용

Y 전진 전 사전 판정:

```text
if targetY is Safe:
    Allow

if otherY is Safe and otherY is not moving forward:
    Allow

if Front/Rear X actual clearance >= required:
    Allow

else:
    Wait or Block
```

중요:

```text
X target이 멀어질 예정이어도 actual clearance가 부족하면 Y 전진 금지.
```

### 5.2 Y 전진 상태에서 X 이동

X 이동 전 사전 판정:

```text
if both PickerY are Forward or moving Forward:
    target path clearance must stay >= required

if one PickerY is Forward:
    moving X must not enter unsafe facing distance while other Y is not Safe
```

실시간 판정:

```text
if both Y are not Safe and actual clearance < required:
    Stop + Critical Alarm
```

### 5.3 같은 zone 예외 제거

기존 `CanShareForwardY(targetZone, otherZone)`는 다음처럼 격하한다.

```text
CanShareForwardY == true
-> 공정상 병렬 진입이 가능할 수 있다는 뜻
-> 단, 거리 safety가 먼저 Allow여야 함
```

### 5.4 검사존 lock

인풋:

```text
InputVision inspection lock active
-> Picker Input 진입 Wait
-> 실제 침범 시 Stop + Alarm
```

아웃풋:

```text
OutputVision inspection lock active
-> Picker Output 진입 Wait
-> 실제 침범 시 Stop + Alarm
```

### 5.5 Z Down 보정

```text
ZState == Down
-> XYT 이동은 CorrectionWindow 이내만 Allow
-> 범위 초과는 Block
```

---

## 6. 단계별 구현 계획

### Phase 0. 파라미터 확정

목표:

- `FrontPickerX <-> RearPickerX`의 실제 HomeClearance/sign 확정.
- Camera/Picker pair 기존 값 검증.
- PickerY Facing clearance 확정.

산출물:

- `shared_rail_x.json` 기준값 확정.
- Supervisor config 초안.

주의:

- 이 단계에서 실장비 임의 추정값 적용 금지.

### Phase 1. 골격 추가, LogOnly

목표:

- `RealtimeCollisionSupervisor` 골격 추가.
- `MotionSafetyState` snapshot 생성.
- 상시 loop 또는 `MotionMonitorService.CycleCompleted` 연결.
- 위험 후보를 로그만 남긴다.

정지/알람:

- 아직 연결하지 않는다.

검증:

- Auto 운전 중 snapshot 값이 정상 기록되는지 확인.
- Front/Rear YState가 실제와 맞는지 확인.
- X pair clearance 계산이 기존 SharedRailX 로그와 일치하는지 확인.

### Phase 2. Distance Calculator 공통화

목표:

- SharedRailX 계산식을 공통 calculator로 분리.
- supervisor와 SharedRailX가 같은 계산을 사용.

수정 대상:

- `SharedRailXMotionService.cs`
- 신규 `CollisionDistanceCalculator.cs`

검증:

- 기존 SharedRailX 알람 clearance 값과 신규 snapshot clearance 값 일치.

### Phase 3. Predictive Gate 연결

목표:

- MotionGuard 경로에 supervisor rule 추가.
- Y 전진과 X 이동 사전 검사 강화.

정책:

- `Wait`은 MotionGuard에서는 우선 Block 메시지로 변환할 수 있다.
- 오토 시퀀스에서는 별도 wait loop로 처리하는 API를 추가한다.

수정 대상:

- `MotionGuardRuleRegistry.cs`
- `PickerZoneInterlockRules.cs`
- `PickerSequenceBase.cs`

검증:

- X actual clearance 부족 상태에서 상대 Y 전진 명령 차단.
- zone이 달라도 distance unsafe면 차단.

### Phase 4. Reactive Guard 정지 연결

목표:

- 실시간 unsafe 감지 시 관련 축 정지 + Critical Alarm.
- latch 처리로 알람 폭주 방지.

수정 대상:

- `RealtimeCollisionSupervisor.cs`
- `MachineController.cs`
- `AlarmResponseService.cs`, 필요 시

검증:

- 시뮬에서 강제 unsafe 상태를 만들어 정지 발생 확인.
- 로그에 pair/actual/required/YState가 남는지 확인.

### Phase 5. Phase 상태 연동

목표:

- PickUp/Bottom/Side/Place phase를 supervisor에 통지.
- 오토 정상 Y 전진 유지가 정상 상태로 인식되도록 한다.

수정 대상:

- `PickerPickUpSequence.cs`
- `PickerBottomAndSideInspectionSequence.cs`
- `PickerPlaceSequence.cs`
- `PickerProcessSequence.cs`
- `PickerPhaseCoordinator.cs`

검증:

- 정상 연속 운전에서 불필요한 Y Avoid 복귀가 생기지 않음.
- Bottom/Side 중 Y 전진 유지가 알람으로 오판되지 않음.

### Phase 6. ReconcileSafeState

목표:

- 정지 후 재시작 전 actual encoder와 material 상태로 현재 안전 상태 재구성.
- unsafe 상태면 재시작 금지 또는 안전 복귀 요구.

수정 대상:

- `MachineController.StartSequenceAsync(...)` 진입부
- `SequenceResumeStore` 연동부
- Supervisor state

검증:

- Bottom/Side/Place 중 Stop 후 재시작.
- 재시작 첫 동작 전 supervisor 상태 로그 확인.

### Phase 7. 검사존 lock + Z 보정 window

목표:

- Input/Output Camera 검사 lock 통합.
- Z Down 중 XYT CorrectionWindow 적용.

수정 대상:

- Input vision prepare/inspection sequence
- Output place inspection sequence
- Picker bottom/side sequence
- Supervisor lock API

검증:

- Camera 검사 중 Picker 진입 Wait.
- Picker 점유 중 Camera 진입 Wait.
- Z Down 중 허용 범위 보정 Allow, 초과 Block.

### Phase 8. 기존 예외 정리

목표:

- `PickerZoneInterlockRules`의 중복 거리/zone 예외를 supervisor 위임으로 단순화.
- `SharedRailXAutoMoveGuard`와 supervisor 중복 범위 정리.

주의:

- 한 번에 제거하지 않는다.
- supervisor가 충분히 로그/검증된 후 단계적으로 정리한다.

---

## 7. 검증 시나리오

필수 반복 검증:

1. 정상 Auto 1 cycle 이상 반복.
2. PickUp 중 Stop 후 재시작.
3. Bottom 검사 중 Stop 후 재시작.
4. Side 검사 중 Stop 후 재시작.
5. Place 진입 전/대기 중 Stop 후 재시작.
6. FrontY Forward 상태에서 RearY Forward 시도.
7. RearY Forward 상태에서 FrontY Forward 시도.
8. Front/Rear X clearance 부족 상태에서 상대 Y 전진 시도.
9. 양쪽 Y Forward 상태에서 X 접근 시도.
10. InputVision 검사 중 Picker Input 진입 시도.
11. OutputVision 검사 중 Picker Output 진입 시도.
12. Z Down 상태에서 X/Y/T 보정 허용 범위 이내 이동.
13. Z Down 상태에서 X/Y/T 보정 허용 범위 초과 이동.
14. 수동 Jog로 X 접근.
15. 수동 Jog로 Y 전진.

PASS:

- actual clearance 부족 상태에서 양쪽 Y Forward 동시 상태가 되지 않는다.
- actual clearance 확보 전 상대 Y 전진이 Wait/Block 된다.
- 이동 중 actual unsafe가 생기면 즉시 정지 + Critical Alarm.
- 정상 오토 공정은 유지된다.
- 재시작은 신규 시작과 같은 안전 게이트를 통과한다.
- 로그로 원인 분석이 가능하다.

FAIL:

- zone이 다르다는 이유로 distance unsafe가 통과된다.
- X target이 멀어질 예정이라는 이유로 actual unsafe 상태에서 Y가 전진한다.
- 실시간 unsafe인데 알람/정지가 발생하지 않는다.
- 오토 정상 공정이 supervisor 오판으로 계속 막힌다.

---

## 8. 로그/알람 기준

신규 로그는 한글로 작성한다.

로그 키워드:

```text
실시간 충돌 감시 시작
실시간 충돌 감시 정지
충돌 예측 게이트 대기
충돌 예측 게이트 차단
실시간 충돌 위험 감지
PickerY 마주보기 거리 부족
검사존 점유 대기
Z 하강 보정 범위 초과
재시작 안전 상태 재구성
```

알람 코드 후보:

```text
PICKER-DISTANCE-SAFETY
PICKER-Y-FACING-CLEARANCE
VISION-PICKER-X-CLEARANCE
COLLISION-SUPERVISOR
```

필수 로그 필드:

```text
pair
axisAActual
axisBActual
axisACommand
axisBCommand
clearance
required
homeClearance
signs
frontYState
rearYState
frontPhase
rearPhase
reason
decision
```

---

## 9. 미결 사항

사용자 확인 필요:

1. `FrontPickerX <-> RearPickerX` 실제 HomeClearance와 sign.
2. PickerY Facing clearance 최종값.
3. 실시간 감시 주기: 10ms / 20ms / 50ms.
4. 위험 감지 시 정지 정책: 관련 축 정지 / 전축 하드정지.
5. Z Down 중 XYT 보정 허용 범위.
6. 재시작 unsafe 상태에서 자동 안전 복귀를 할지, 작업자 확인 후 복귀할지.

---

## 10. 1차 구현 추천 범위

처음부터 모든 기능을 넣으면 위험하다. 1차는 사고 원인을 직접 막는 범위로 제한한다.

1차 구현:

- Supervisor skeleton.
- Front/Rear PickerX pair 추가.
- Front/Rear PickerY 동시 Forward actual clearance 감시.
- 상대 Y Forward 전 actual clearance 사전 차단.
- 상세 로그.
- 시뮬에서만 정지 연결 검증.

1차 제외:

- Z Down CorrectionWindow.
- 검사존 lock 통합.
- 기존 rule 대규모 정리.
- UI 설정 페이지.

1차 목적:

```text
X가 붙어 있는데 양쪽 PickerY가 동시에 전진하는 사고 가능성을 먼저 제거한다.
```
