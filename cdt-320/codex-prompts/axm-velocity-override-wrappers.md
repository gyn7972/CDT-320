# 작업: 아진(AXM) 래퍼에 벨로시티 오버라이드 함수군 public 래핑 추가

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 수정 대상 파일: `QMC.Common\AjinE\AXM.cs` (**실제 컴파일 대상** — `QMC.Common.csproj`는 `AjinE\AXM.cs`만 포함)
- 미러 파일: `QMC.Common\Ajin\AXM.cs` — 프로젝트에 미포함(컴파일 안 됨)이지만 동일 네임스페이스
  (`QMC.Common.Motion.Ajin`)의 병행 유지 파일이므로 **동일한 변경을 두 파일에 모두 적용**한다.
- 이번 작업은 **래퍼 계층만** 대상이다. `AjinAxis` 등 상위 호출부 통합은 별도 작업이며 이번에 수정하지 않는다.

## 배경 — 현재 상태 (수정 전 반드시 해당 파일을 읽고 시작할 것)

`AjinE\AXM.cs`의 `#region 오버라이드 함수`(P/Invoke 선언, 약 765~805행)에 다음 private extern들이 선언되어 있다:

| extern 함수 | 기능 | 현재 public 래퍼 |
|---|---|---|
| `AxmOverridePos(nAxisNo, dOverridePos)` | 구동 중 목표 위치 변경 | ✅ `ModifyPosition()` (약 2119행)에서 사용 중 |
| `AxmOverrideAccelVelDecel(nAxisNo, dVel, dMaxAccel, dMaxDecel)` | 구동 중 속도+가감속 변경 | ✅ `ModifyPosition()`, `ModifyVelocity()` (약 2126행)에서 사용 중 |
| `AxmOverrideSetMaxVel(nAxisNo, dOverrideMaxVel)` | 오버라이드 허용 최고속도 설정 (오버라이드 사용 전 필수 선행) | ⚠️ `SetMaxVelocity()` (약 1272행) 내부에서 `AxmMotSetMaxVel`과 함께 호출될 뿐, 단독 래퍼 없음 |
| `AxmOverrideVel(nAxisNo, dOverrideVelocity)` | 구동 중 속도만 변경 (가감속 유지) | ❌ 래퍼 없음 |
| `AxmOverrideVelAtPos(nAxisNo, dPos, dVel, dAccel, dDecel, dOverridePos, dOverrideVelocity, nTarget)` | 이동을 시작시키면서 지정 위치(`dOverridePos`) 도달 시 속도를 `dOverrideVelocity`로 자동 전환. `nTarget`: COMMAND(0)/ACTUAL(1) 위치 기준 | ❌ 래퍼 없음 |

기존 public 래퍼의 공통 패턴 (반드시 동일하게 따를 것):

```csharp
public static int ModifyVelocity(int axis, double velocity, double acceleration, double deceleration)
{
    int ret = 0;
    if ((ret = AXL.CheckErrorCode("AXM.AxmOverrideAccelVelDecel", AXM.AxmOverrideAccelVelDecel(axis, velocity, acceleration, deceleration))) != 0) return ret;
    return ret;
}
```

- 반환값: `0` = 성공, 그 외 = AXL 에러코드 (`AXL.CheckErrorCode`가 로깅/변환 담당)
- 단위: 보드 단위 그대로 전달 (단위 환산은 상위 `AjinAxis`의 `ToBoardVelocity()` 등에서 수행 — 래퍼는 관여하지 않음)
- extern은 private 유지, public 래퍼만 노출

## 요구사항

`#region 오버라이드 함수` (public 래퍼 영역, `ModifyPosition`/`ModifyVelocity`가 있는 곳)에
아래 래퍼들을 추가한다. 각 메서드에 기존 스타일과 동일한 한국어 XML doc 주석을 단다
(extern 선언부의 주의사항 주석 내용을 요약해 반영할 것).

### R1. 속도 단독 오버라이드
```csharp
public static int ModifyVelocity(int axis, double velocity)
```
- `AxmOverrideVel(axis, velocity)` 호출.
- 기존 3-인자 `ModifyVelocity`의 오버로드로 추가한다.
- XML 주석에 명시: 구동 중에만 호출해야 하며, 사전에 `AxmOverrideSetMaxVel`로 설정된
  최고속도(현재는 보드 셋업 시 `SetMaxVelocity()`가 축 MaxVelocity로 설정) 이하만 유효.

### R2. 오버라이드 최고속도 단독 설정
```csharp
public static int SetOverrideMaxVelocity(int axis, double velocity)
```
- `AxmOverrideSetMaxVel(axis, velocity)` 호출.
- 기존 `SetMaxVelocity()`는 수정하지 않는다 (모터 최고속도와 오버라이드 최고속도를 함께 설정하는 기존 동작 유지).
- XML 주석에 명시: 속도 오버라이드를 여러 번 사용할 경우 그중 최고 속도 이상으로 미리 설정해야 함.

### R3. 위치 예약 속도 오버라이드 (지정 위치 도달 시 속도 자동 전환)
```csharp
public enum OverridePositionTarget
{
    Command = 0,
    Actual = 1,
}

public static int MoveWithVelocityOverrideAtPosition(
    int axis,
    double position,            // 최종 목표 위치
    double velocity,            // 초기 구동 속도
    double acceleration,
    double deceleration,
    double overridePosition,    // 이 위치 도달 시 속도 전환
    double overrideVelocity,    // 전환 후 속도
    OverridePositionTarget target)
```
- `AxmOverrideVelAtPos(axis, position, velocity, acceleration, deceleration, overridePosition, overrideVelocity, (int)target)` 호출.
- enum은 AXM 클래스 내 기존 enum들(`GantryHomingMethods` 등)과 같은 위치/스타일로 선언한다.
- XML 주석에 명시:
  - 이 함수는 **이동 시작 함수**다 — 이미 구동 중인 축에 거는 오버라이드가 아니라,
    `position`까지 `velocity`로 구동을 개시하면서 `overridePosition` 지점에서
    `overrideVelocity`로 전환되도록 예약한다.
  - 사용 전 `AxmOverrideSetMaxVel`로 `velocity`와 `overrideVelocity` 중 큰 값 이상이
    설정되어 있어야 한다.
  - `target`: 전환 위치 판정 기준 (Command 위치 / Actual(엔코더) 위치).

## 제약 사항
- private extern 선언(시그니처·주석)은 변경하지 않는다.
- 기존 public 래퍼(`ModifyPosition`, `ModifyVelocity(3-인자)`, `SetMaxVelocity`)의 동작을 변경하지 않는다.
- `AXL.CheckErrorCode` 패턴, `int ret = 0; ... return ret;` 스타일, 리전 구조를 그대로 따른다.
- 이 작업에서 `AjinAxis`, `BaseAxis` 등 다른 파일은 수정하지 않는다.
- `QMC.Common\Ajin\AXM.cs`(미컴파일 미러 파일)에도 동일 변경을 적용한다.
  두 파일의 extern 선언 행번호는 다르지만 구조는 같다 (`Ajin`: extern 약 733~770행, 래퍼 약 1969~1983행).

## 검증 / 수용 기준
1. 솔루션 전체가 빌드 에러 없이 컴파일된다 (MSBuild).
2. `AjinE\AXM.cs`와 `Ajin\AXM.cs` 두 파일에 동일한 래퍼 3종 + enum 1종이 추가되어 있다.
3. 기존 래퍼/extern/다른 파일에는 diff가 없다.
4. 새 래퍼는 아직 호출부가 없어도 된다 (상위 통합은 별도 작업). 컴파일 경고가 새로 발생하지 않는지 확인한다.
