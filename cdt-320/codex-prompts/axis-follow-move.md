# 작업: 2축 팔로잉 모션 함수 구현 (`FollowMoveAsync`) — 포지션 오버라이드 기반 추종 이동

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일: `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs`
- 참고(수정 금지) 파일:
  - `QMC.Common\Motion\BaseAxis.cs` — 축 베이스 클래스, 시뮬레이션 엔진, `OverridePosition`/`OverrideVelocity`
  - `QMC.Common\Motion\AxisMoveWaiter.cs` — 모션 완료 대기 유틸
  - `QMC.Common\AjinE\AXM.cs` — 아진 보드 래퍼 (`ModifyPosition`, `ModifyVelocity` 등. **이 파일 수정 금지**)
  - `QMC.CDT-320\Sequencing\Picker\InputVisionXPrePositionCoordinator.cs` — 백그라운드 이동 태스크 + 폴링 + 오버라이드 패턴의 기존 예 (`TryOverrideMovingAxisToFinal`, `StopAndDrainMoveTaskAsync`)
  - `QMC.CDT-320\Equipment\Interlocks\Common\MotionGuardRuntime.cs` — 이동 인터락 검증

## 배경 — 기존 코드에서 활용할 것

- `AjinAxis.TryOverridePosition(targetPosition, velocity, acceleration, deceleration)` (약 115행):
  구동 중인 축의 목표 위치/속도/가감속을 변경한다. 시뮬레이션 분기(`UseSimulation`)와
  실장비 분기(`AXM.ModifyPosition`) 모두 구현되어 있고, `MotionGuardRuntime.VerifyAxisTeachingMove`
  인터락 검증을 포함한다. 반환: `0`=성공, `-4`=정지 상태, `-11`=인터락 거부, 그 외 에러코드.
- `AXM.ModifyVelocity(axis, velocity, acceleration, deceleration)` (`AjinE\AXM.cs` 약 2126행):
  구동 중 속도/가감속만 변경하는 public 래퍼. 이미 존재하며 현재 미사용.
- `BaseAxis.OverrideVelocity(newVelocity)`: 시뮬레이션 모드에서 이동 중 속도 변경.
- `BaseAxis.MoveAbsoluteAsync(targetPos, velocity)`: 이동 완료까지 await하는 이동 명령.
  **완료까지 대기하므로 팔로잉 루프에서는 await하지 말고 백그라운드 `Task<int>`로 잡아둔 채
  폴링해야 한다** (`InputVisionXPrePositionCoordinator.MoveVisionXAsync`의 `moveTask` 패턴 참조).
- `AxisMoveWaiter.WaitMoveDoneInPositionAsync(axis, target, tolerance, timeoutMs, settleMs)`:
  목표 도달 + 정지 확인 대기 (`BaseAxis.MoveJogStepAsync`의 사용 예 참조).
- 위치 비교 허용오차: `Config.InPositionTolerance` (0 이하이면 0.01 사용) — 기존 관례.

## 기능 사양

같은 레일 위에서 움직이는 **선행축**과 **후행축** 2축의 추종 이동.
선행축은 **외부에서 이미 이동 명령이 내려진 상태**이고, 이 함수는 **후행축(this)** 에서 호출되어
물리 간격이 안전거리 미만으로 줄어들지 않는 한도 내에서 선행축을 따라가다가,
후행축이 자기 목표 위치에 도달하면 리턴한다.

### 함수 시그니처 (AjinAxis의 public 인스턴스 메서드, this = 후행축)

```csharp
/// <summary>
/// 선행축을 따라가며 후행축(this)을 목표 위치까지 이동시킨다.
/// 선행축에는 어떤 명령도 내리지 않는다(읽기 전용).
/// </summary>
public async Task<int> FollowMoveAsync(
    BaseAxis leadingAxis,            // 선행축 (읽기 전용 — ActualPosition/IsAlarm/IsMoving만 참조)
    double leadingTargetPosition,    // 선행축 목표 위치 (검증/로깅용 — 명령에 사용 금지)
    double leadingVelocity,          // 선행축 속도
    double leadingAcceleration,      // 선행축 가속도
    double leadingDeceleration,      // 선행축 감속도
    double trailingTargetPosition,   // 후행축 목표 위치
    double trailingVelocity,         // 후행축 속도
    double trailingAcceleration,     // 후행축 가속도
    double trailingDeceleration,     // 후행축 감속도
    int direction,                   // 진행 방향: +1 또는 -1
    double safetyGap,                // 두 축 간 안전거리 [mm]
    double homeGap,                  // 두 축 좌표계 원점 간 기구 오프셋 [mm]
    CancellationToken ct = default(CancellationToken))
```

- 모든 위치/거리/속도 인자는 축 사용자 단위(mm 기준). 보드 단위 환산은 기존
  `ToBoardPosition`/`ToBoardVelocity` 경로가 담당하므로 이 함수에서 직접 하지 않는다.
- 상수 정의:
  - `MinimumFollowSafetyGap = 40.0` — `safetyGap < 40.0`이면 `40.0`으로 클램프.
  - `FollowMoveTimeoutMs = 5000` — 함수 전체 타임아웃 5초 **고정** (인자로 받지 않음).
  - 폴링 주기 10ms.

### 팔로잉 구간 프로파일

팔로잉(중간명령) 단계에서 후행축에 적용할 속도/가감속은 **선행·후행 인자 중 성분별 작은 값**:

```
followVel = Min(leadingVelocity,     trailingVelocity)
followAcc = Min(leadingAcceleration, trailingAcceleration)
followDec = Min(leadingDeceleration, trailingDeceleration)
```

인자가 0 이하인 성분은 해당 축 `Config` 기본값(`DefaultVelocity`/`Acceleration`/`Deceleration`)으로
대체한 뒤 Min을 취한다.

### 간격 계산 (매 루프, 실측 위치 기준)

```
-방향: 두축간의 거리 = (후행축.ActualPosition + homeGap) - 선행축.ActualPosition
+방향: 두축간의 거리 = (선행축.ActualPosition + homeGap) - 후행축.ActualPosition

여유거리 = 두축간의 거리 - safetyGap
```

### 중간명령 위치

```
여유거리 <= 0 이면: 이번 루프에서는 후행축에 새 명령을 내리지 않는다 (대기).

여유거리 > 0 이면:
  +방향: 중간명령 위치 = 후행축.ActualPosition + 여유거리
                       = 선행축.ActualPosition + homeGap - safetyGap   (전개형, 동치)
  -방향: 중간명령 위치 = 후행축.ActualPosition - 여유거리
                       = 선행축.ActualPosition - homeGap + safetyGap   (전개형, 동치)
```

### 실제 명령 위치 (방향별 Min/Max — 목표를 넘어가지 않도록)

```
+방향: 명령 위치 = Min(trailingTargetPosition, 중간명령 위치)
-방향: 명령 위치 = Max(trailingTargetPosition, 중간명령 위치)
```

명령 전 재검증: 명령 위치로 이동을 완료했다고 가정했을 때의 간격이
`safetyGap` 이상인지 간격 공식으로 다시 확인하고, 미달이면 이번 루프는 건너뛴다.

### 명령 방법

- 후행축 **정지 상태**(`IsMoving == false`): `MoveAbsoluteAsync(명령 위치, followVel)`을
  **await하지 않고** `Task<int>`로 시작해 보관한다 (백그라운드 이동). 가감속은 기존
  `MoveAbsoluteAsync` 동작을 따르고, 다음 루프부터의 오버라이드에서 followAcc/followDec가 적용된다.
- 후행축 **이동 중**: `TryOverridePosition(명령 위치, followVel, followAcc, followDec)` 호출.
  - 반환 `-4`(정지 상태 경합)는 무시하고 다음 루프에서 재시도.
  - 반환 `-11`(인터락 거부)이면 후행축 정지 후 `-11` 반환하고 종료.
- **오버라이드 스팸 방지**: 직전에 명령한 위치와 새 명령 위치의 차이가
  `InPositionTolerance` 이내면 이번 루프는 명령을 생략한다.

### 최종 구간 진입과 종료

명령 위치가 `trailingTargetPosition`과 같아지는(허용오차 이내) 루프에서:

1. 후행축이 이동 중이면 **후행축 자신의** `trailingVelocity`/`trailingAcceleration`/`trailingDeceleration`으로
   **벨로시티 오버라이드**를 수행한다 (팔로잉용 Min 프로파일 → 자기 프로파일 복귀).
   이를 위해 보조 메서드를 함께 구현한다 (아래 R2).
2. `AxisMoveWaiter.WaitMoveDoneInPositionAsync(this, trailingTargetPosition, tolerance, 남은 타임아웃, 0)`으로
   모션 완료를 대기한다.
3. 성공 시 백그라운드 moveTask를 정리(observe)하고 `0`을 반환한다.

### 타임아웃 / 취소 / 이상 처리

- **타임아웃**: 함수 진입부터 `Stopwatch`로 측정, 팔로잉 루프와 최종 대기를 합쳐 5초(5000ms) 고정.
  초과 시 후행축 `Stop()` → 백그라운드 moveTask drain(최대 2초 대기 후 observe,
  `StopAndDrainMoveTaskAsync` 패턴) → 타임아웃 에러코드 반환 (새 코드 `-21`로 정의하고
  `RecordMotionFailure`로 사유를 남길 것).
- **취소(ct)**: 후행축 정지 + moveTask drain 후 `OperationCanceledException` 전파
  (`InputVisionXPrePositionCoordinator`의 기존 취소 처리 관례).
- **알람**: 루프마다 후행축/선행축의 `IsAlarm` 확인. 어느 쪽이든 알람이면 후행축 정지 후
  에러코드 반환 (후행축 알람이면 `(int)AlarmCode`, 선행축 알람이면 `-22`로 정의).
- **초기 검증**:
  - `leadingAxis == null` → `-1`
  - `direction`이 +1/-1이 아니면 → `-1`
  - 후행축 서보 OFF/알람 → 기존 `FailAxisNotReady` 관례로 `-2`
  - `trailingTargetPosition`이 현재 위치 대비 `direction`과 반대 방향이면 → `-1`
    (허용오차 이내로 이미 목표에 있으면 즉시 `0` 반환)
- 백그라운드 moveTask는 어떤 종료 경로에서도 unobserved exception이 남지 않도록 observe한다.

### 선행축 절대 불가침 (최우선 제약)

**이 함수는 선행축에 어떠한 명령도 내리지 않는다.**
`MoveAbsoluteAsync`/`Stop`/`TryOverridePosition`/`OverrideVelocity`/`ServoOn` 등
상태를 변경하는 모든 호출 금지. 허용되는 것은 `ActualPosition`, `IsMoving`, `IsAlarm` 등
**상태 읽기뿐**이다. 선행축의 이동 명령은 외부(호출자)가 이미 내린 상태를 전제로 한다.

## 구현 항목

### R1. `AjinAxis.FollowMoveAsync` — 위 사양 전체 구현

### R2. `AjinAxis.TryOverrideVelocity(velocity, acceleration, deceleration)` — 보조 메서드 신규 구현
`TryOverridePosition`과 같은 구조의 시뮬/실장비 분기:
- 공통: `IsMoving == false`이면 `-4` 반환.
- 시뮬 분기(`UseSimulation`): `base.OverrideVelocity(velocity)` 호출 후 `0` 반환.
- 실장비 분기: `AjinSystem.IsOpen`/알람/서보 확인(기존 `TryOverridePosition`과 동일한 검사와
  에러 메시지 관례) 후, `lock (_sync)` 하에
  `AXM.ModifyVelocity(AxisNo, ToBoardVelocity(v), ToBoardAcceleration(a), ToBoardAcceleration(d))` 호출.
  실패 시 `FailMotion` 관례로 기록 후 에러코드 반환. 성공 시 `CurrentVelocity` 갱신 후 `0`.
- 인자가 0 이하인 성분은 `Config` 기본값에 `MotionSpeedScale` 스케일을 적용해 대체
  (`TryOverridePosition`의 safeVelocity/safeAcceleration/safeDeceleration 처리와 동일하게).

### R3. 로깅
시작(인자 요약), 최초 이동 명령, 최종 구간 진입(벨로시티 오버라이드), 정상 완료,
타임아웃/알람/인터락 실패 시점에 기존 `AjinAxis`/시퀀스 코드의 로깅 관례
(`Log.Write` 또는 `EventLogger`, 한국어 메시지, Start/End/Failed 접미)를 따라 로그를 남긴다.

## 검증용 수치 예시 (그대로 테스트 시나리오로 사용할 것)

공통: `safetyGap = 50`, `homeGap = 100`

**예시 A (-방향, 여유 없음 → 대기)**
- 선행축 Actual = 600, 후행축 Actual = 500
- 간격 = (500 + 100) − 600 = 0 → 여유 = −50 ≤ 0 → 후행축 명령 없음.

**예시 B (-방향, 여유 있음 → 중간명령)**
- 선행축 Actual = 500, 후행축 Actual = 500
- 간격 = (500 + 100) − 500 = 100 → 여유 = 50
- 중간명령 위치 = 500 − 50 = **450** (= 500 − 100 + 50 전개형 일치)
- 검증: 이동 후 간격 = (450 + 100) − 500 = 50 ≥ 50 → 수락
- 명령 위치 = Max(후행축 Target, 450). Target이 450보다 작으면(더 먼 -방향) 450까지만 이동.

**예시 C (+방향)**
- 선행축 Actual = 600, 후행축 Actual = 500
- 간격 = (600 + 100) − 500 = 200 → 여유 = 150
- 중간명령 위치 = 500 + 150 = **650** (= 600 + 100 − 50 전개형 일치)
- 검증: 이동 후 간격 = (600 + 100) − 650 = 50 ≥ 50 → 수락
- 명령 위치 = Min(후행축 Target, 650).

**예시 D (최종 구간)**
- 명령 위치 == 후행축 Target이 된 루프에서 후행축 자신의 Vel/Acc/Dec로 벨로시티 오버라이드
  → 모션 완료 대기 → `0` 반환.

## 제약 사항
- `QMC.Common\AjinE\AXM.cs`, `QMC.Common\Ajin\AXM.cs`, `BaseAxis.cs` 등 다른 파일은 수정하지 않는다.
  (`AXM.ModifyVelocity` public 래퍼는 이미 존재하므로 그대로 사용)
- 선행축 상태 변경 호출 금지 (위 "선행축 절대 불가침" 절).
- 기존 public API 시그니처 변경 금지. 신규 추가는 `FollowMoveAsync`, `TryOverrideVelocity` 및
  필요한 private 헬퍼/상수만.
- 코드 스타일: 기존 `AjinAxis.cs`를 따른다 — 한국어 XML doc 주석, `_camelCase` 필드,
  최신 C# 문법(null 조건 연산자, 패턴 매칭 등) 사용 자제, `lock (_sync)` 관례,
  에러 기록은 `FailMotion`/`RecordMotionFailure` 관례.
- 시뮬레이션 모드(`Config.IsSimulationMode == true`)에서도 동일 로직으로 동작해야 한다
  (명령 경로가 `MoveAbsoluteAsync`/`TryOverridePosition`/`TryOverrideVelocity`의
  시뮬 분기를 타므로 별도 분기 불필요 — 이를 검증할 것).

## 검증 / 수용 기준
1. 솔루션 전체가 빌드 에러 없이 컴파일된다 (MSBuild).
2. 시뮬레이션 모드에서 예시 A~D 시나리오를 재현하는 테스트 하네스
   (축 2개를 `IsSimulationMode = true`로 생성, 선행축은 외부에서 `MoveAbsoluteAsync` 시작)를
   작성·실행하고 결과를 보고한다:
   - 예시 B/C: 후행축이 안전거리를 침범하지 않으면서 선행축을 따라가고, 최종 목표에서 `0` 반환.
   - 예시 A 상황 지속: 후행축이 움직이지 않다가 5초 타임아웃 시 `-21` 반환, 후행축 정지 상태.
   - 전 구간에서 매 폴링 시점의 간격 ≥ safetyGap(허용오차 이내)임을 로그로 확인.
   - 선행축에는 함수 호출 전후로 명령 이력이 없음(선행축의 `CommandPosition`이 외부 명령 값에서
     변하지 않음)을 확인.
3. `direction` 및 목표 방향 불일치, 서보 OFF, 이미 목표 도달 등 초기 검증 각 케이스가
   지정된 코드로 반환된다.
4. `safetyGap = 30` 전달 시 내부적으로 40mm로 클램프되어 동작함을 확인.
