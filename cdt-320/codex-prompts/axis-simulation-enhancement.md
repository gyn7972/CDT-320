# 작업: Axis 시뮬레이션 기능 강화 — 모션 프로파일 기반 위치/속도 오버라이드

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 수정 대상 프로젝트: `QMC.Common`, `QMC.CDT-320`
- **시뮬레이션 로직만 수정한다. 실장비 경로(AXM/AXL P/Invoke 호출, `AjinSystem`, 보드 통신)는 절대 변경 금지.**

## 배경 — 현재 구조 (수정 전 반드시 해당 파일을 읽고 시작할 것)

### `QMC.Common\Motion\BaseAxis.cs` (핵심 파일)
모든 축의 추상 베이스 클래스. `Config.IsSimulationMode == true`이면 내장 시뮬레이션 엔진이 동작한다.

- **이동 명령**: `MoveAbsoluteAsync()` → `ConfigureSimulationMotionProfile()` → `BuildSimulationMotionSegments()`가
  목표 거리·속도·가속도·감속도로 사다리꼴(가속/등속/감속) 프로파일을 생성한다.
  10ms 주기(`StartStatusUpdateTask` 또는 `WaitUntilMoveDone` 내부)의 `SimulateMotion()`이
  `Stopwatch` 경과시간 기반으로 `ActualPosition` / `CurrentVelocity`를 갱신하고,
  총 프로파일 시간 도달 시 `CompleteSimulationMove()`로 완료 처리한다.
- **오버라이드**: `OverridePosition(newTarget)` / `OverrideVelocity(newVel)`이
  `_overrideTargetPosition` / `_overrideVelocity` 필드에 값을 저장하고,
  `SimulateMotion()` 도입부에서 이 값을 소비하여 `ResetSimulationMotionReference()`로 프로파일을 재구성한다.
- 관련 내부 멤버: `_simTargetPosition`, `_simCommandVelocity`, `_simAcceleration`, `_simDeceleration`,
  `_simMotionStartPosition`, `_simMotionDirection`, `_simMotionDistance`, `_simMotionStartTimestamp`,
  `_simMotionPeakVelocity`, `_simMotionAccelerationTime/CruiseTime/DecelerationTime`,
  `_simMotionAccelerationDistance/CruiseDistance`, `_simMotionTotalTime`,
  `CalculateSimulationProfileDistance()`, `CalculateSimulationProfileVelocity()`

### `QMC.CDT-320\Equipment\SimComponents.cs`
`SimAxis : BaseAxis` — 추가 로직 없이 BaseAxis 시뮬 엔진을 그대로 사용.

### `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs`
실장비 축 구현. `Config.IsSimulationMode`이면 `UpdateStatus()`가 `base.UpdateStatus()` → `SimulateMotion()`으로 위임.
`TryOverridePosition(targetPosition, velocity, acceleration, deceleration)` (약 115행)에
`UseSimulation` 분기가 있다: `IsMoving` 확인 → `MotionGuardRuntime.VerifyAxisTeachingMove` 인터락 검증 →
`base.OverridePosition(targetPosition)` 호출.
실장비 분기는 `AXM.ModifyPosition()`(= `AxmOverridePos` + `AxmOverrideAccelVelDecel`)으로
위치·속도·가감속을 모두 하드웨어에 반영한다.

## 현재 문제점 (이번 작업으로 해결)

1. **프로파일 재구성 시 초기속도 0 가정**:
   `BuildSimulationMotionSegments()`는 항상 정지 상태(v₀=0)에서 출발하는 프로파일만 생성한다.
   이동 중 오버라이드가 발생하면 `ResetSimulationMotionReference()`가 현재 위치를 시작점으로 잡지만
   속도는 0에서 재가속하므로, 현재 속도를 유지한 채 프로파일이 이어지는 실장비 동작과 불일치한다.
2. **`TryOverridePosition` 시뮬 분기에서 velocity 인자 무시**:
   `CurrentVelocity = velocity`로 속성만 1회 대입하는데, 다음 10ms 틱에서 `SimulateMotion()`이
   기존 `_simCommandVelocity` 기반 프로파일 값으로 `CurrentVelocity`를 덮어쓴다.
   실장비 분기는 velocity/acc/dec을 `AXM.ModifyPosition`에 전달하므로 시뮬과 실장비 동작이 다르다.
3. **잔류(stale) 오버라이드 버그**:
   `SimulateMotion()`은 `!IsMoving`이면 오버라이드 필드를 소비하지 않고 리턴한다.
   정지 상태에서 `OverrideVelocity()`가 호출되면 `_overrideVelocity`가 남아 있다가
   다음 이동의 첫 틱에 적용되어 의도하지 않은 속도로 이동한다.
   (`ConfigureSimulationMotionProfile`, `Stop`, `SetPosition` 어디에서도 초기화하지 않음)

## 요구사항

### R1. 이동 명령 → 모션 프로파일 기반 위치 갱신 (기존 동작 유지 + 초기속도 지원으로 확장)
- 이동 명령(`MoveAbsoluteAsync`/`MoveRelativeAsync`) 시 적용된 목표 위치·속도·가속도·감속도로
  모션 프로파일을 생성하고, `SimulateMotion()` 틱마다 경과시간에 따라 위치를 갱신하는
  기존 구조는 유지한다.
- 단, 프로파일 생성기를 **초기속도 v₀ ≠ 0을 지원하도록 확장**한다
  (예: `BuildSimulationMotionSegments(double initialVelocity)` 형태 또는 `_simMotionInitialVelocity` 필드 추가).
  - v₀ < 순항속도: v₀→v 가속 → 등속 → 감속(→0) 사다리꼴.
  - v₀ > 순항속도: v₀→v 감속 → 등속 → 감속(→0).
  - 거리가 짧아 등속 구간이 없는 삼각형 프로파일도 v₀ 반영.
  - `CalculateSimulationProfileDistance()` / `CalculateSimulationProfileVelocity()`를
    v₀ 포함 수식으로 갱신한다.
- 새 이동 명령은 지금처럼 정지 상태에서 시작하므로 v₀=0으로 호출하면 기존과 동일한 결과가 나와야 한다.

### R2. PositionOverride 적용 시 프로파일 재구성 → R1 틱 루프에서 반영
- `OverridePosition()`(또는 `SimulateMotion()`의 소비 지점)에서 프로파일 재구성 시
  **현재 `ActualPosition`과 현재 `CurrentVelocity`를 초기 조건으로** 새 목표까지의 프로파일을 만든다.
- **방향 반전 처리**: 새 목표가 현재 진행 방향의 반대쪽이거나, 남은 거리가 현재 속도의
  제동거리(v₀²/2·dec)보다 짧으면 → 먼저 `_simDeceleration`으로 0까지 감속(목표를 지나쳐도 됨) 후,
  그 지점에서 새 목표를 향한 표준 프로파일을 자동으로 이어서 구성한다.
  (다구간 처리 방식은 자유 — 세그먼트 리스트로 일반화해도 되고, "감속 완료 시점에 프로파일 1회 재구성"
  방식으로 구현해도 된다. 단 `SimulateMotion()` 틱에서 결정론적으로 계산될 것.)
- 최종적으로 `ActualPosition == 새 목표`에서 `CompleteSimulationMove()`가 호출되어
  `IsMoving=false`, `IsInPosition=true`, `MoveCompleted` 이벤트가 정확히 1회 발생해야 하고,
  진행 중이던 `MoveAbsoluteAsync`의 `WaitUntilMoveDone` 대기가 정상 완료되어야 한다.

### R3. VelocityOverride 적용 시 프로파일 재구성 → R1 틱 루프에서 반영
- `OverrideVelocity()` 소비 시 **현재 위치·현재 속도를 유지한 채** 순항속도만 새 값으로 바꾼
  프로파일을 재구성한다. 속도 상향(추가 가속)과 하향(감속 후 등속) 모두 지원한다.
- **`AjinAxis.TryOverridePosition`의 `UseSimulation` 분기 수정**:
  `velocity > 0`이면 `base.OverrideVelocity(velocity)`를 함께 호출(또는 동등한 방법으로
  `_simCommandVelocity`에 반영)하여, 실장비 분기(`AXM.ModifyPosition`에 velocity 전달)와
  동일하게 위치+속도가 모두 반영되도록 한다.
  `CurrentVelocity = velocity` 직접 대입은 제거한다(다음 틱에 프로파일이 올바른 값을 계산).

### R4. 잔류 오버라이드 초기화
- 새 모션 시작(`ConfigureSimulationMotionProfile`), `Stop()`, `StopJog()`, `SetPosition()`,
  `RestoreRuntimeState()` 시 `_overrideTargetPosition`/`_overrideVelocity`를 `double.NaN`으로 초기화하여
  이전 오버라이드 값이 다음 모션에 유입되지 않도록 한다.

## 제약 사항
- `BaseAxis`의 public API 시그니처는 변경하지 않는다 (`OverridePosition`, `OverrideVelocity`,
  `MoveAbsoluteAsync` 등). 파생 클래스(`AjinAxis`, `SimAxis`)가 깨지지 않아야 한다.
- Jog 모드(`MotionMode.Jog`) 시뮬레이션 로직은 기존 동작을 유지한다.
- 소프트 리미트 검사(`TriggerSoftLimitAlarm`), `ActualPositionChanged`/`MoveStarted`/`MoveCompleted`
  이벤트 발행 시점은 기존 의미를 유지한다.
- `SimulateMotion()`은 백그라운드 태스크(10ms)와 `WaitUntilMoveDone` 루프 양쪽에서 호출될 수 있다.
  기존 코드와 동일한 수준의 동시성 가정을 유지하고, 새 공유 상태를 추가할 때 일관성이 깨지지 않게 한다.
- 코드 스타일은 기존 파일을 따른다: 한국어 XML doc 주석, `var` 최소화, 오래된 C# 문법
  (null 조건 연산자 등 최신 문법 사용 자제), 필드 네이밍 `_camelCase`.
- 실장비 분기(`AXM.*` 호출부), `QMC.Common\AjinE\AXM.cs`, `QMC.Common\Ajin\AXM.cs`는 수정하지 않는다.

## 검증 / 수용 기준
1. 솔루션 전체가 빌드 에러 없이 컴파일된다 (MSBuild).
2. v₀=0 신규 이동: 기존과 동일한 사다리꼴/삼각형 프로파일로 목표 도달, `MoveCompleted` 1회 발생.
3. 이동 중 `OverridePosition(더 먼 목표)`: 현재 속도를 유지한 채 프로파일이 연장되어 새 목표에 도달.
4. 이동 중 `OverridePosition(반대 방향 목표)`: 감속 → 반전 → 새 목표 도달. 알람/이벤트 오동작 없음.
5. 이동 중 `OverrideVelocity(상향/하향)`: 현재 속도에서 새 순항속도로 가/감속 후 목표 도달.
6. `AjinAxis.TryOverridePosition`(시뮬 모드, velocity 지정): 위치와 속도가 모두 반영됨.
7. 정지 상태에서 `OverrideVelocity()` 호출 후 새 이동: 이전 오버라이드 값이 적용되지 않음.
8. 가능하면 위 3~7 시나리오를 검증하는 간단한 테스트 코드(콘솔 하네스 또는 단위 테스트)를
   작성해 실행 결과를 보고한다. 테스트 인프라가 없으면 `SimAxis`를 직접 생성해
   `Config.IsSimulationMode=true`로 두고 시나리오를 돌리는 임시 하네스로 검증 후 결과를 요약한다.
