# Picker Distance Safety Monitor 설계안

작성일: 2026-07-03  
대상 프로젝트: CDT-320  
목적: Front/Rear Picker, Input/Output Camera X축 간섭을 오토/메뉴얼 구분 없이 실시간 거리 기반으로 감시하고, 시퀀스 재시작/병렬 동작/수동 조작 중 충돌 가능성을 최종 차단한다.

---

## 1. 문제 정의

현재 CDT-320의 간섭 방지는 주로 모션 명령이 발생하는 시점의 `MotionGuard`, `PickerZoneInterlockRules`, `SharedRailXMotionService` 검사를 통해 수행된다. 일부 SharedRailX 이동은 이동 중 실시간 guard가 있으나, 전체 장비 상태를 상시 감시하는 안전 레이어는 아니다.

최근 확인된 핵심 위험은 다음과 같다.

- 한쪽 PickerY가 전진 상태인데, Front/Rear PickerX 실제 거리가 충분히 벌어지기 전에 상대 PickerY가 전진할 수 있다.
- X축 target은 안전해 보여도 actual encoder 거리가 아직 안전거리 미만이면 Y축 전진이 위험하다.
- 정지 후 재시작 시 기존 다이/시퀀스 상태를 기준으로 중간 공정부터 진입하면서, 신규 시작에서는 발생하지 않던 간섭 가능성이 생긴다.
- 오토 공정 예외를 해결하기 위해 zone 기반 예외가 늘어나면서, 실제 물리 거리보다 논리 zone 판정이 앞서는 구멍이 생길 수 있다.
- Input/Output Camera와 Picker가 병렬로 움직일 때, 서로의 작업 영역 점유와 X축 실제 거리 감시가 항상 같은 레이어에서 보장되지 않는다.

따라서 최종 안전 기준은 `zone`이 아니라 `실제 엔코더 기반 물리 거리`가 되어야 한다.

---

## 2. 설계 원칙

1. 실시간 감시가 최종 책임을 가진다.
   - Move 전 검사는 사전 차단이다.
   - 이동 중 실제 충돌 가능성은 별도 monitor가 계속 감시한다.

2. 오토/메뉴얼/Jog/Teaching 모두 같은 안전 기준을 적용한다.
   - 오토라서 통과시키지 않는다.
   - 오토 공정상 필요한 이동도 실제 거리 조건을 만족해야 허용한다.

3. zone은 보조 정보다.
   - `Input`, `Bottom`, `Side`, `Output`, `Avoid`는 공정 흐름 판단에 사용한다.
   - 최종 허용/차단은 X actual clearance, X target/path, PickerY 전진 상태로 판단한다.

4. 재시작도 신규 시작과 같은 안전 게이트를 통과해야 한다.
   - Stop/CycleStop 이후 현재 다이 상태에서 재개하더라도, Y 전진 전 X actual clearance를 다시 확인한다.

5. 알람은 분석 가능한 수준으로 남긴다.
   - 차단/정지 로그에는 pair, actual, target, clearance, required, Y 상태, moving 상태를 반드시 포함한다.

---

## 3. 현재 코드 기준 분석

### 3.1 MotionGuard

관련 파일:

- `QMC.CDT-320/Equipment/Interlocks/Common/MotionGuardRuntime.cs`
- `QMC.CDT-320/Equipment/Interlocks/Common/MotionGuardService.cs`
- `QMC.CDT-320/Equipment/Interlocks/Common/MotionGuardRuleRegistry.cs`

현재 구조:

```text

BaseAxis.Move...
-> BaseAxis.MotionGuard
-> MotionGuardRuntime
-> MotionGuardService
-> MotionGuardRuleRegistry
-> PickerZoneInterlockRules / SharedRailXInterlockRules 등

```

장점:

- 모든 축 이동 전 사전 검사를 넣을 수 있다.
- 메뉴얼/Jog/Teaching/Auto 진입점 일부를 공통으로 통제할 수 있다.

한계:

- 기본적으로 move 시작 전 1회 검사다.
- 이동 후 actual encoder가 위험거리로 들어오는 상황을 계속 감시하지 않는다.
- targetName/zone/activeTargetZone 의존도가 높아 타이밍 레이스에 취약할 수 있다.

### 3.2 PickerZoneInterlockRules

관련 파일:

- `QMC.CDT-320/Equipment/Interlocks/PickerZoneInterlockRules.cs`

현재 핵심 로직:

- `CanMovePickerYByFacingYInterlock(...)`
- `CanMovePickerXByFacingYInterlock(...)`
- `IsPickerYOutOrMovingOut(...)`
- `DoXMovePathsEnterFacingClearance(...)`
- `CanShareForwardY(...)`

장점:

- PickerY 전진 전 상대 PickerY 상태와 Front/Rear PickerX 거리를 확인한다.
- PickerX 이동 중 양쪽 PickerY 전진 상태를 고려한다.
- 오토 공정상 필요한 검사 연속 이동 예외가 존재한다.

한계:

- Y 전진 여부 판단이 `activeTargetZone`, actual Y, moving 상태를 조합한다.
- 두 Picker가 거의 동시에 Y 전진을 시도하면 active 상태 반영 전 통과 가능성을 완전히 배제하기 어렵다.
- `CanShareForwardY()` 같은 zone 기반 허용이 실제 거리 안전보다 앞서 보일 수 있다.
- 이동 중 상시 감시가 아니라 사전 검사 성격이 강하다.

### 3.3 SharedRailXMotionService

관련 파일:

- `QMC.CDT-320/Equipment/Motion/SharedRailX/SharedRailXConfig.cs`
- `QMC.CDT-320/Equipment/Motion/SharedRailX/SharedRailXConfigStore.cs`
- `QMC.CDT-320/Equipment/Motion/SharedRailX/SharedRailXMotionService.cs`

현재 설정 파일:

- `D:/CDT-320/Config/shared_rail_x.json`

현재 관리 pair:

```text

InputVisionX  <-> FrontPickerX
InputVisionX  <-> RearPickerX
OutputVisionX <-> FrontPickerX
OutputVisionX <-> RearPickerX

```

계산식:

```text

clearance = HomeClearance
          - AxisATowardSign * AxisA.ActualPosition
          - AxisBTowardSign * AxisB.ActualPosition

```

장점:

- `HomeClearance`, `TowardSign`, `SafetyDistance`로 서로 다른 원점/방향을 가진 축 간 실제 거리를 계산할 수 있다.
- `SharedRailXAutoMoveGuard`가 일부 그룹 이동 중 20ms 주기로 실제 clearance를 감시한다.

한계:

- SharedRailX 이동 서비스 내부에만 묶여 있다.
- 모든 오토/메뉴얼/Jog 상태를 상시 감시하지 않는다.
- `FrontPickerX <-> RearPickerX` pair가 기본 collision pair에 없다.
- PickerY 전진 상태와 연동된 실시간 감시가 아니다.

---

## 4. 목표 구조

새 안전 레이어를 추가한다.

```text

프로그램 실행
-> MotionMonitorService 축 상태 polling
-> PickerDistanceSafetyMonitor 상시 감시
-> 위험 감지 시 즉시 축 정지 + Critical Alarm

모션 명령 발생
-> MotionGuard 사전 검사
-> PickerDistanceSafetyRules 공통 거리 계산
-> 허용 시 이동
-> 이동 중 PickerDistanceSafetyMonitor가 actual encoder 감시

```

신규 서비스명:

```text

PickerDistanceSafetyMonitor

```

권장 위치:

```text

QMC.CDT-320/Equipment/Interlocks/Runtime/PickerDistanceSafetyMonitor.cs

```

공통 계산기:

```text

QMC.CDT-320/Equipment/Interlocks/Runtime/PickerDistanceSafetyRules.cs
QMC.CDT-320/Equipment/Interlocks/Runtime/PickerDistanceSafetyConfig.cs

```

---

## 5. 감시 대상

### 5.1 X축 pair

다음 pair를 실시간 감시한다.

```text

InputVisionX  <-> FrontPickerX
InputVisionX  <-> RearPickerX
OutputVisionX <-> FrontPickerX
OutputVisionX <-> RearPickerX
FrontPickerX  <-> RearPickerX

```

앞 4개는 Camera/Picker 간섭 방지다.  
마지막 1개는 PickerY 전진 상태와 묶인 Picker 간 충돌 방지다.

### 5.2 Y축 상태

감시 대상:

```text

FrontPickerY
RearPickerY

```

상태 구분:

```text

Safe/Avoid
Forward
MovingToSafe
MovingToForward
Unknown

```

Y축 안전 위치 기준:

- `AvoidPosition`
- `InputAvoidPosition`
- `OutputAvoidPosition`
- 0 근처, 설정 threshold 이내

Y축 전진 판단:

- 안전 위치에서 `PickerYOutDistance` 이상 벗어나면 전진 상태
- target Y가 전진 위치이면 moving 전부터 전진 예정 상태로 본다
- 상태를 모르면 안전하지 않은 것으로 본다

---

## 6. 파라미터 설계

기존 `shared_rail_x.json`를 확장하는 방향을 우선 검토한다. 이미 X축 pair 거리 계산에 필요한 개념이 들어 있기 때문이다.

### 6.1 기존 필드

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

### 6.2 추가 필요 pair

```json

{
  "AxisA": "FrontPickerX",
  "AxisB": "RearPickerX",
  "HomeClearance": 500,
  "AxisATowardSign": 1,
  "AxisBTowardSign": -1,
  "SafetyDistance": 150,
  "RuleKind": "PickerYFacing"
}

```

주의:

- `HomeClearance`, sign은 반드시 실제 장비 기준으로 확인 후 등록한다.
- 임시 추정값으로 실장비에 적용하지 않는다.
- 현재 사용자가 설명한 기구 관계상 Front/Rear 홈 기준 물리 거리 차이가 존재하므로 raw encoder 단순 차이는 금지한다.

### 6.3 신규 설정 필드 후보

`SharedRailXCollisionPairRow` 확장:

```text

RuleKind
Enabled
RealtimeGuardEnabled
StopGroup

```

권장 enum:

```text

NormalXClearance
PickerYFacing
CameraPickerZone

```

추가 전역 설정:

```text

RealtimeMonitorEnabled
RealtimeMonitorPeriodMs
PickerYForwardClearance
PickerYOutDistance
UnsafeDebounceCount
LogThrottleMs

```

---

## 7. 안전 규칙

### 7.1 FrontPickerX <-> RearPickerX + PickerY 규칙

| 조건 | 결과 |
|---|---|
| FrontY Safe, RearY Safe | X 거리 가까워도 허용 |
| FrontY Forward, RearY Safe | 허용, 단 X 이동 중 실시간 감시 |
| FrontY Safe, RearY Forward | 허용, 단 X 이동 중 실시간 감시 |
| FrontY Forward, RearY Forward, X clearance >= required | 허용 |
| FrontY Forward, RearY Forward, X clearance < required | 즉시 정지 + Critical Alarm |
| 한쪽 Y Forward 상태에서 상대 Y Forward 명령, 현재 X clearance < required | 명령 차단 |
| X target은 멀어지지만 현재 X clearance < required | 상대 Y Forward 금지 |
| X 이동 중 clearance가 required 미만으로 감소 | 즉시 정지 + Critical Alarm |

핵심:

```text

X가 멀어질 예정이어도 actual clearance가 아직 부족하면 Y 전진 금지

```

### 7.2 CameraX <-> PickerX 규칙

| 조건 | 결과 |
|---|---|
| InputVisionX 검사 영역 점유 중 Picker가 Input pickup 영역 진입 | 대기 또는 차단 |
| OutputVisionX 검사 영역 점유 중 Picker가 Output place 영역 진입 | 대기 또는 차단 |
| CameraX/PickerX target path clearance 부족 | 명령 차단 |
| 이동 중 actual clearance 부족 | 즉시 정지 + Critical Alarm |
| 점유 해제 후 clearance 안전 | 이동 허용 |

### 7.3 Z Down + XYT 보정 규칙

오토 공정에서는 다음 움직임이 필요하다.

- PickUp 후 Y 전진 유지
- Bottom 검사 중 Z Down
- Bottom -> Side 피치 이동
- Side 완료 후 Z Up
- Place까지 Y 전진 유지
- Align offset 반영으로 X/Y/T 미세 보정

허용 조건:

```text

현재 phase가 해당 공정 phase
Z Down 상태에서 보정 이동량이 CorrectionWindow 이내
Front/Rear PickerX clearance 안전
Camera/Picker clearance 안전
해당 작업 영역 점유 상태 정상

```

차단 조건:

```text

Z Down 상태에서 보정 허용 범위를 벗어난 X/Y/T 이동
phase와 맞지 않는 Z Down 이동
상대 Picker 또는 Camera와 거리 unsafe

```

---

## 8. Runtime 상태 모델

`PickerDistanceSafetyMonitor`는 다음 snapshot을 매 주기 만든다.

### 8.1 Axis snapshot

```text

AxisName
ActualPosition
CommandPosition
IsMoving
IsAlarm
TargetPosition, if known

```

### 8.2 Pair snapshot

```text

PairName
AxisA
AxisB
HomeClearance
AxisATowardSign
AxisBTowardSign
ActualClearance
RequiredClearance
AxisAActual
AxisBActual
AxisACommand
AxisBCommand
AxisAMoving
AxisBMoving
RuleKind
IsUnsafe
Reason

```

### 8.3 Picker snapshot

```text

Side
PickerXActual
PickerXCommand
PickerYActual
PickerYCommand
PickerYState
ZState
Phase
CarryingDie
WorkArea

```

---

## 9. Stop/Alarm 정책

위험 감지 시 기본 정책:

```text

1. 관련 축 즉시 Stop
2. Critical Alarm 발생
3. Machine status Alarm 진입
4. 로그 기록
5. latch 처리하여 같은 알람 반복 발생 방지

```

Picker 간 위험 정지 대상:

```text

FrontPickerX
FrontPickerY
RearPickerX
RearPickerY

```

Camera/Picker 위험 정지 대상:

```text

해당 CameraX
해당 PickerX
해당 PickerY

```

알람 코드 후보:

```text

PICKER-DISTANCE-SAFETY
PICKER-Y-FACING-CLEARANCE
VISION-PICKER-X-CLEARANCE

```

로그 예시:

```text

Front/Rear PickerY 전진 거리 인터락 감지. pair=FrontPickerX<->RearPickerX,
frontXActual=690.000, rearXActual=700.000,
frontYActual=-29.130, rearYActual=-29.130,
frontYState=Forward, rearYState=Forward,
clearance=8.500, required=150.000,
frontXMoving=True, rearXMoving=False,
homeClearance=500.000, signs=FrontPickerX:1,RearPickerX:-1

```

---

## 10. 기존 코드 적용 방안

### 10.1 1단계: 거리 계산 공통화

추가:

- `PickerDistanceSafetyRules`
- `PickerDistancePairRule`
- `PickerDistancePairSnapshot`

목표:

- `SharedRailXMotionService`
- `PickerZoneInterlockRules`
- 신규 `PickerDistanceSafetyMonitor`

세 곳이 같은 clearance 계산을 사용하게 한다.

### 10.2 2단계: FrontPickerX <-> RearPickerX pair 추가

수정:

- `SharedRailXConfig`
- `SharedRailXConfigStore`
- `shared_rail_x.json` normalize 로직
- SharedRailX Setup UI, 필요 시

단, 실장비 적용 전 HomeClearance/sign 확인 필수.

### 10.3 3단계: Monitor 추가

추가:

- `PickerDistanceSafetyMonitor`

연결:

- `Form1_Load` 또는 `MachineController` 초기화 시 시작
- `MotionMonitorService.CycleCompleted` 이벤트 구독 또는 자체 loop 사용
- 종료 시 Dispose

권장:

```text

실장비: 20~50ms
시뮬: 50~100ms

```

### 10.4 4단계: MotionGuard 정리

수정:

- `PickerZoneInterlockRules.CanMovePickerYByFacingYInterlock`
- `PickerZoneInterlockRules.CanMovePickerXByFacingYInterlock`
- `VerifyPickerYMove`
- `VerifyPickerXMove`

방향:

- zone 기반 최종 허용 제거
- 거리 기반 판단을 먼저 수행
- `CanShareForwardY()`는 공정상 대기 여부 판단 보조로만 사용

### 10.5 5단계: 오토 phase/state 연동

수정 후보:

- `Sequencing/Picker/PickerPickUpSequence.cs`
- `Sequencing/Picker/PickerBottomAndSideInspectionSequence.cs`
- `Sequencing/Picker/PickerPlaceSequence.cs`
- `Sequencing/Common/PickerPhaseCoordinator.cs`

목표:

- PickUp/Bottom/Side/Place phase를 monitor가 알 수 있게 한다.
- 재시작 시 `ReconcileSafeState()`로 실제 엔코더 기반 상태를 복원한다.

---

## 11. 검증 계획

### 11.1 시뮬레이션 검증

반복 테스트:

1. 정상 오토 1 cycle 이상
2. PickUp 중 Stop 후 재시작
3. Bottom 검사 중 Stop 후 재시작
4. Side 검사 중 Stop 후 재시작
5. Place 진입 전 Stop 후 재시작
6. Front Y 전진 상태에서 Rear Y 전진 시도
7. Rear Y 전진 상태에서 Front Y 전진 시도
8. Front/Rear X가 가까운 상태에서 한쪽 Y 전진 시도
9. InputVisionX 검사 중 Picker Input 진입 시도
10. OutputVisionX 검사 중 Picker Output 진입 시도
11. Z Down 상태에서 보정 허용 범위 이내 X/Y/T 이동
12. Z Down 상태에서 보정 허용 범위 초과 X/Y/T 이동

### 11.2 PASS 기준

- X actual clearance가 required 미만이면 양쪽 Y가 동시에 전진하지 않는다.
- X가 멀어질 target이어도 actual clearance 확보 전에는 상대 Y가 전진하지 않는다.
- 이동 중 clearance 위반이 발생하면 즉시 정지 및 Critical Alarm이 발생한다.
- 오토 정상 공정의 PickUp -> Bottom -> Side -> Place 흐름은 유지된다.
- 정지 후 재시작해도 신규 시작과 같은 안전 게이트를 통과한다.
- Input/Output Camera 검사와 Picker 진입이 서로 대기/차단된다.

### 11.3 FAIL 기준

- 양쪽 PickerY가 전진인데 Front/Rear X clearance가 required 미만이다.
- 상대 PickerY 전진 상태에서 X actual clearance 확보 전 다른 PickerY가 전진한다.
- zone이 다르다는 이유만으로 거리 unsafe 상태를 통과한다.
- 실시간 monitor가 아닌 move 전 검사만으로 위험 상황을 놓친다.
- 알람 로그에 actual/required/pair/Y state가 없어 사후 분석이 불가능하다.

---

## 12. 적용 우선순위

1. `FrontPickerX <-> RearPickerX` 물리 거리 pair 파라미터 확정
2. 공통 clearance 계산기 분리
3. Move 전 Y 전진 차단 로직을 actual clearance 기준으로 강화
4. `PickerDistanceSafetyMonitor` 상시 감시 추가
5. Input/Output Camera 점유와 Picker 진입 rule 통합
6. Z Down 보정 window rule 추가
7. 기존 zone 예외 정리
8. 시뮬 반복 테스트 후 실장비 적용

---

## 13. 결론

이번 문제는 시퀀스 순서만 맞춘다고 해결되지 않는다. 시퀀스가 정상 흐름을 만들더라도, 정지/재시작/병렬 동작/수동 조작에서 한 번이라도 타이밍이 꼬이면 같은 사고 가능성이 남는다.

따라서 CDT-320에는 다음 안전 원칙이 필요하다.

```text

최종 충돌 방지는 zone이 아니라 실제 물리 거리 기반 실시간 감시가 책임진다.

```

`PickerDistanceSafetyMonitor`를 추가하고, 기존 `MotionGuard`, `PickerZoneInterlockRules`, `SharedRailXMotionService`가 같은 거리 계산기를 사용하도록 통합해야 한다. 그 뒤 오토 공정 예외는 거리 안전을 통과한 경우에만 허용한다.
