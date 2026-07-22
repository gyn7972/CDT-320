# 작업: PickUp 진입 Vision 안전 게이트 재정의 + PickerX 팔로잉 진입 — Conti/Default 공통

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일:
  - `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs`
  - `QMC.CDT-320\Equipment\Interlocks\PickerFrontInterlockRules.cs`
  - `QMC.CDT-320\Equipment\Interlocks\PickerRearInterlockRules.cs`
- 참고(수정 금지) 파일:
  - `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` — `FollowMoveAsync`(약 313행),
    `MinimumFollowSafetyGap = 40.0`(295행), `FollowMoveTimeoutMs = 5000`(297행).
    **이 파일은 수정하지 않고 팔로우 명령을 그대로 호출만 한다.**
  - `QMC.CDT-320\Equipment\Motion\SharedRailX\SharedRailXMotionService.cs` —
    `TryResolveNearestVisionRetreatTarget`(약 88행), `IsVisionRetreatTargetSafe`
  - `QMC.CDT-320\Equipment\Motion\SharedRailX\SharedRailXCollisionValidator.cs` —
    페어 간극 공식(약 130행): `clearance = homeClearance - aSign*aPos - bSign*bPos`
  - `QMC.CDT-320\Equipment\Motion\SharedRailX\SharedRailXConfig.cs` / `SharedRailXConfigStore.cs` —
    pair `HomeClearance` / `SafetyDistance` / `AxisATowardSign`·`AxisBTowardSign`
  - `QMC.CDT-320\Equipment\Unit\InputStageUnit.cs` — `IsVisionXInAvoidPosition`(약 1619행)
  - `cdt-320\codex-prompts\axis-follow-move.md` — `FollowMoveAsync` 사양
    (간격 공식, `direction`/`safetyGap`/`homeGap` 정의). **인자 의미는 이 문서를 기준으로 맞출 것.**

## 배경 — 현재 동작 (수정 전 반드시 해당 코드를 읽을 것)

### PickUp 스텝 흐름 (Auto)
```
검사 배치 완료 → CalculatePickTargets(true)          ← 예약된 4개 die의 TargetPickerX 확정 (1128행)
→ MoveInputVisionToAvoidForPickerMove (1018행)       ← VisionX 최소 회피 이동 "완료까지 대기"
→ SelectNextPickTarget → MovePickerXStageYPickerT    ← 그 후에야 PickerX 진입
```
- `MoveInputVisionToAvoidForPickerMoveAsync`(1018행):
  `SharedRailXMotionService.TryResolveNearestVisionRetreatTarget(stage.CameraX, 전체Avoid, -0.1,
  배치 TargetPickerX 목록, additionalClearance=1.0, ...)`(1052~1059행)로 **동적 최소 회피 좌표**를
  계산해 `_inputVisionPickerEntryTarget`/`_inputVisionPickerEntryTargetPrepared`(60~61행)에 확정한 뒤,
  VisionX 이동 → `WaitInputStageAxisInPositionResultAsync` → `CheckInputStageAxisInPosition`
  (1082~1107행)까지 **완료 대기**하고 다음 스텝으로 넘어간다.

### Vision 안전 게이트 (이번 작업의 핵심 대상)
`IsInputVisionXSafeForContinuousPick(stage, out detail)`(약 2910행) 현재 조건:
```
CameraX.IsMoving == false
AND _inputVisionPickerEntryTargetPrepared
AND IsAxisInTarget(CameraX, _inputVisionPickerEntryTarget)
AND CameraX.ActualPosition <= 0.0          ← 하드 진입한계
```
호출부 2곳:
1. `CanUseContiSegmentedPickUpFromCurrentPosition`(2025행, 내부 약 2128행에서 호출) —
   ContiSegmented 협조 이송 진입 가드. 실패 시 default 순차 이송으로 폴백.
2. `CanKeepPickerYForwardForContinuousPick`(2685행, 내부 약 2755행에서 호출) —
   배치 내 연속 픽업에서 PickerY 전진 유지 판정. 실패 시 Y를 Avoid로 뺀다.

### 인터락 (팔로잉을 막는 지점)
`VerifyInputVisionXAtAvoidOrBelowZero`(`PickerFrontInterlockRules.cs` 742행, Rear에 동명 함수 별도 존재):
PickerX Input 진입 시 `CameraX.IsMoving`이면 무조건 차단, 아니면 Avoid 위치 또는 actual ≤ tol 요구.
`FollowMoveAsync`의 포지션 오버라이드도 매번 이 인터락을 통과하며 거부되면 `-11`로 정지한다
(`AjinAxis.cs` 491~498행). **이 규칙을 완화하지 않으면 팔로잉은 시작 직후 죽는다.**

### 간극 상수 충돌 (반드시 해소할 것)
- 동적 회피 좌표의 추가 여유: `additionalClearance = 1.0`mm (1057행)
- `FollowMoveAsync` 안전 간극: `safetyGap` 인자, **최소 40mm 클램프**
`FollowMoveAsync`는 최종 목표 명령도 간극 ≥ safetyGap이어야 발행하므로(461행),
회피 좌표가 pick 목표에서 1mm만 물러나 있으면 후행축이 최종 목표에 도달하지 못하고
5초 타임아웃(-21)으로 실패한다.

## 기능 사양

### S1. Vision 진입 게이트 3-상태 재정의
`IsInputVisionXSafeForContinuousPick`를 대체하는 판정을 신설한다.

```csharp
private enum InputVisionEntryGateState
{
    Safe,           // 정지 + 확정 좌표 + 예약 pick 목표 대비 클리어런스 확보 → 즉시 진입 허용
    FollowEligible, // 확정 회피 목표를 향해 후퇴 이동 중 → PickerX 팔로잉 진입 허용
    Unsafe          // 그 외 → 진입 불가
}

private InputVisionEntryGateState ResolveInputVisionEntryGateState(
    InputStageUnit stage, out string detail)
```

판정 규칙 (위에서 순서대로 평가):
- **Safe**:
  - `CameraX.IsMoving == false`
  - `_inputVisionPickerEntryTargetPrepared == true`
  - `|ActualPosition - _inputVisionPickerEntryTarget| <= tol` (tol = `Config.InPositionTolerance`, 0 이하이면 0.05)
  - **남은 예약 pick 목표 전부에 대해 페어 간극 ≥ requiredClearance**:
    - 남은 목표 = `_pickBatchItems`의 `_pickCursor` 이후 항목들의 `TargetPickerX`
      (타깃 미계산 시점이면 배치 전체).
    - 간극은 SharedRailX pair(InputVisionX ↔ 해당 side PickerX)의
      `HomeClearance`/`TowardSign`을 사용해 validator와 동일 공식으로 계산:
      `clearance = homeClearance - visionSign*visionActual - pickerSign*pickTargetX`
    - `requiredClearance` = pair `SafetyDistance`(없으면 두 축 설정 `SafetyDistance`의 최대값).
  - **기존의 `ActualPosition <= 0.0` 하드 조건은 위 간극 조건으로 대체한다**
    (요구사항 원문: "actual ≤ 픽업 예약된 위치 + 클리어런스"의 일반화 —
    부호/기하는 pair TowardSign이 담당하므로 원시 부호 비교를 직접 쓰지 말 것).
- **FollowEligible**:
  - `CameraX.IsMoving == true`
  - `_inputVisionPickerEntryTargetPrepared == true`
  - `|CommandPosition - _inputVisionPickerEntryTarget| <= tol` (지금 후퇴 명령이 확정 목표를 향함)
  - 이동 방향이 **후퇴 방향** — 목표 위치의 페어 간극이 현재 위치의 페어 간극보다 크거나 같음.
- **Unsafe**: 그 외 전부 (미확정, 목표 불일치, 전진 방향 이동, 예외).
- `detail`에는 기존 관례대로 상태/actual/command/target/간극 수치를 담는다.

### S2. 호출부별 동작
1. `CanUseContiSegmentedPickUpFromCurrentPosition`(2128행 부근):
   - `Safe` → 기존대로 conti 적격.
   - `FollowEligible` → conti 부적격 처리하되, **거절 사유를 구분**해서 호출자
     (`MovePickerXStageYPickerTByContiSegmentedPickUpOrDefaultAsync`, 1660행)가
     default 폴백 대신 **팔로우-보조 진입 경로(S3)** 를 타도록 한다
     (out enum 또는 별도 out 플래그로 전달 — 문자열 reason 파싱 금지).
   - `Unsafe` → 기존과 동일하게 default 폴백 (default 경로도 S4의 게이트를 다시 본다).
2. `CanKeepPickerYForwardForContinuousPick`(2755행 부근):
   - `Safe`일 때만 기존 조건 계속 평가.
   - `FollowEligible`/`Unsafe` → `false` (PickerY 전진 유지 불가).
   - **근거(변경 금지)**: PickerY가 전진(헤드가 Input 존 내부)한 채로 VisionX가 이동 중인 상태에서
     X 팔로잉을 허용하면 방어선이 팔로잉 gap 하나뿐이 된다. Y 전진 유지 최적화는 Safe에서만.
     이 경우 기존 흐름대로 Y를 Avoid로 뺀 뒤 다음 진입에서 S3가 처리한다.

### S3. 팔로우-보조 진입 경로 (신규, X만 팔로잉)
게이트가 `FollowEligible`일 때 PickerX 접근을 다음으로 대체한다:
1. **사전 검증**: 확정 회피 목표(`_inputVisionPickerEntryTarget`) 기준으로
   "vision이 목표에 도달했다고 가정한 페어 간극"이 사용할 `safetyGap` 이상인지 확인.
   미달이면 팔로잉 불가 → 기존 default(직렬: vision 완료 대기 후 진입)로 폴백하고 로그.
2. PickerX가 `AjinAxis`인지 캐스트 확인(아니면 default 폴백) 후:
   ```
   pickerXAjin.FollowMoveAsync(
       leadingAxis:            stage.CameraX,
       leadingTargetPosition:  _inputVisionPickerEntryTarget,
       leading Vel/Acc/Dec:    CameraX Config 기본값 (0 전달 시 내부 대체 규칙 활용 가능),
       trailingTargetPosition: _targetPickerX,
       trailing Vel/Acc/Dec:   PickerX 기존 이동 프로파일,
       direction / safetyGap / homeGap: SharedRailX pair 설정에서 유도 (아래 S6),
       ct)
   ```
3. 팔로잉과 **동시에** NeedleX/StageY 안전 순서 이동과 PickerT 이동을 기존 default 구조
   (`MovePickerXStageYPickerTByDefaultAsync`의 병행 Task 구성, 1595~1621행)와 동일하게 수행한다.
   단 PickerX 항목만 FollowMove Task로 대체.
4. 모든 Task 완료 후 **게이트 재판정 → `Safe`가 아니면 실패 처리**(Fail + 알람. 재시도하지 않음).
5. 이후 PickerY 전진(1630행) / PickerZ 하강은 기존 스텝 그대로 진행한다.
   **PickerY 전진 명령 이전 시점에 게이트 `Safe`가 보장되어야 한다** (4가 그 역할).
- Conti 세그먼트 프로파일(`PickerPickUpContiSegmentedMotion`)에 팔로잉을 섞지 않는다.
  팔로우-보조 경로는 "X만 팔로잉 + 나머지는 default 구조"로 한정한다.

### S4. Default 경로 동일 적용 + Vision 이동 비대기화
1. `MoveInputVisionToAvoidForPickerMoveAsync`(1018행): 회피 좌표 확정 로직은 유지하되,
   VisionX 이동의 `Wait`/`Check`(1091~1107행)를 제거하고 **이동 명령만 발행**한다.
   이동 `Task<int>`는 필드에 보관한다 (fire-and-forget 금지 — 실패/알람 회수용.
   `InputVisionXPrePositionCoordinator`의 moveTask 보관 패턴 참조).
2. `MovePickerXStageYPickerTByDefaultAsync`(1571행): PickerX/T 이동 전에 게이트를 판정해
   - `Safe` → 기존 `MovePickerAxesAndVerifyAsync` 유지,
   - `FollowEligible` → S3와 동일하게 PickerX만 FollowMove로 대체,
   - `Unsafe` → 보관한 vision 이동 Task를 await(완료 대기)한 뒤 재판정. 그래도 Safe/FollowEligible이
     아니면 Fail (기존 `PICKER-PICKUP-VISION-AVOID` 계열 알람 관례).
3. PickerY 전진 직전(1630행 이전)에 "vision 이동 Task 결과 확인 + 게이트 `Safe` 확인"을 넣는다.
   (기존 1101행의 `CheckInputStageAxisInPosition` 최종 검증을 이 시점으로 이동하는 효과.)

### S5. 인터락 완화 — X 이동 관련 규칙만
`VerifyInputVisionXAtAvoidOrBelowZero` (Front 742행 + Rear 동명 함수, **두 파일 모두**):
- 기존 허용 조건(정지 + Avoid 또는 actual ≤ tol)은 그대로 유지.
- **추가 허용 조건** (팔로잉 중 오버라이드 통과용): `CameraX.IsMoving == true`이더라도
  아래를 전부 만족하면 허용:
  1. `CameraX.CommandPosition`이 진입 한계 이하 — Avoid 위치이거나 `CommandPosition <= tol`
     (인터락은 시퀀스 필드 `_inputVisionPickerEntryTarget`을 볼 수 없으므로 축 상태만으로 판정),
  2. 후퇴 방향 이동 — `CommandPosition`의 페어 간극 ≥ `ActualPosition`의 페어 간극,
  3. 현재 시점 페어 간극(이동 축의 target 포함) ≥ pair `SafetyDistance`.
- **다른 조건은 절대 변경 금지**: `VerifyManualFrontPickerXInputEntry`(573행)의
  PickerZ/ExpandingZ/Feeder Dog·Down/상대 PickerY 조건, PickerY 전진 인터락, Z 인터락 전부 불변.
- SharedRailX 페어 규칙(`SharedRailXInterlockRules.Verify`)은 수정하지 않는다 —
  팔로잉의 중간 명령은 간극 ≥ safetyGap ≥ pair SafetyDistance이므로 자연 통과해야 하며,
  통과하지 못하면 설정(S6)이 잘못된 것이다.

### S6. 간극/인자 정합 (단일 소스)
1. 1057행의 `additionalClearance = 1.0` 하드코딩을 제거하고, **팔로잉에 사용할 `safetyGap`과
   동일한 값**을 사용한다. 값의 원천은 SharedRailX pair `SafetyDistance`로 하되,
   `AjinAxis` 클램프(40mm) 미만이면 40mm로 올려서 사용한다:
   `effectiveGap = Max(pair.SafetyDistance, 40.0)` — 이 값을 [회피 좌표 계산 additionalClearance]와
   [FollowMoveAsync safetyGap 인자] 양쪽에 동일하게 전달한다.
2. `direction`/`homeGap` 인자는 pair 설정(`HomeClearance`, `AxisATowardSign`/`AxisBTowardSign`)에서
   유도한다. `axis-follow-move.md`의 간격 공식과 validator 공식(130행)이 같은 값을 내도록
   유도식을 주석으로 남기고, 시작 로그에 direction/homeGap/safetyGap을 기록한다.
3. `FollowMoveTimeoutMs`는 5초 고정(AjinAxis 수정 금지)이므로, 시뮬레이션 검증에서
   "최대 진입 거리 / followVel" 기준 5초 내 완주 가능함을 확인하고 결과를 보고한다.
   불가능한 프로파일이면 팔로잉 사전 검증(S3-1)에서 default 폴백시킨다.

## 구현 항목
- **R1.** `ResolveInputVisionEntryGateState` 신설 + `IsInputVisionXSafeForContinuousPick` 대체
  (기존 함수는 새 판정을 호출하는 어댑터로 남기거나 제거 — 호출부 일관성 유지).
- **R2.** `CanUseContiSegmentedPickUpFromCurrentPosition` / `CanKeepPickerYForwardForContinuousPick`
  게이트 연동 (S2).
- **R3.** 팔로우-보조 진입 경로 (S3) — conti 폴백 분기와 default 분기 공용 헬퍼로 구현.
- **R4.** Vision 회피 스텝 비대기화 + Task 보관 + PickerY 전 동기화 지점 (S4).
- **R5.** 인터락 완화 Front/Rear 2개 함수 (S5).
- **R6.** 간극/인자 정합 및 유도 주석 (S6).
- **R7.** 로깅 — 게이트 상태 전이(Safe/FollowEligible/Unsafe), 팔로잉 시작/완료/폴백 사유를
  기존 시퀀스 로깅 관례(한국어, `- Start/Ok/Check/Failed` 접미)로 기록.

## 제약 사항
- `AjinAxis.cs`, `SharedRailXCollisionValidator.cs`, `SharedRailXInterlockRules.cs`,
  `MotionGuardRuleRegistry.cs`, `RealtimeCollisionSupervisor.cs`는 수정하지 않는다.
- 인터락 수정은 Front/Rear의 `VerifyInputVisionXAtAvoidOrBelowZero` 2개 함수로 한정한다.
- `FollowMoveAsync`는 선행축(CameraX)에 명령하지 않으므로, VisionX 후퇴 명령은
  기존 시퀀스 발행분(S4-1)을 유지한다. 팔로잉 경로에서 CameraX에 추가 명령 금지.
- 기존 public API 시그니처 변경 금지. `PickerPickUpStep` enum 순서/값 변경 금지
  (재개 스토어 호환).
- 코드 스타일: 기존 `PickerPickUpSequence.cs` 관례 — 한국어 주석, `_camelCase` 필드,
  최신 C# 문법 자제, `Fail(...)` 알람 관례, `WriteLog` 로깅 관례.

## 검증 / 수용 기준
1. 솔루션 전체가 빌드 에러 없이 컴파일된다 (MSBuild).
2. 시뮬레이션 모드 시나리오 (BypassHardware/시뮬 축 사용, 4개 die 배치 기준):
   - **Safe**: VisionX가 확정 회피 좌표에 정지 + 전 예약 목표 간극 확보 → conti 적격 판정 유지,
     기존과 동일하게 진입.
   - **FollowEligible**: VisionX가 확정 목표로 후퇴 이동 중 → PickerX가 FollowMove로 진입 시작,
     매 폴링 간극 ≥ safetyGap 로그 확인, VisionX 정지·게이트 Safe 후 PickerY 전진.
   - **Unsafe(전진 이동)**: VisionX가 스테이지 쪽(간극 감소 방향)으로 이동 중 → 진입 불가,
     default 경로도 vision Task 완료 대기 후 재판정.
   - **간극 미달 설정**: pair SafetyDistance를 크게 설정해 최종 간극 미달을 만들었을 때
     팔로잉 사전 검증이 default 폴백을 선택하고 -21 타임아웃이 발생하지 않는다.
   - **연속 픽업**: `CanKeepPickerYForwardForContinuousPick`이 vision 이동 중에는 false를
     반환해 Y가 Avoid로 빠진다.
3. 인터락: 완화 조건 밖(전진 방향 이동, command가 진입 한계 초과)에서
   PickerX 이동/오버라이드가 여전히 차단(-11)됨을 확인.
4. 게이트 상태 전이와 팔로잉 시작/완료/폴백이 로그로 추적 가능하다.
