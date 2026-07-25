# CDT-320: 위치 오버라이드에 targetName(존 의도) 전달 — 팔로잉 중 "Manual X 목표 존을 판단할 수 없습니다" 차단 해소 (A안)

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
수정 파일은 정확히 4개다:
- `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` (`TryOverridePosition` 시그니처 + `FollowMoveAsync` 전달)
- `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs` (follow 호출부에서 targetName 전달)
- `QMC.CDT-320\Sequencing\Picker\PickerPlaceSequence.cs` (동일)
- `QMC.CDT-320\Sequencing\Picker\InputVisionXPrePositionCoordinator.cs` (기존 오버라이드 호출부)

**인터락 파일은 절대 수정하지 않는다 — diff 0건이어야 한다.**
`QMC.Common\Motion\BaseAxis.cs`, `QMC.Common\AjinE\AXM.cs`, SharedRailX, UI, 설정 데이터도 건드리지 않는다.

---

# ★ 작업 규칙 (필수 — 위반 시 작업 중단하고 사용자에게 보고)

### 규칙 1. 공정 속도 구간에는 스케일 미적용 코드를 작성하지 않는다
Auto 운전에서 `Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`을 보드로
내려보내는 경로는 반드시 `MotionSpeedScale.ApplyDefaultVelocityScale` /
`ApplyDefaultAccelerationScale`을 통과한 값만 사용한다. Config 날값 직접 전달 금지.

### 규칙 2. 속도·가속·감속은 항상 같은 배율로 함께 스케일한다
전례: 커밋 `a690f6a5`, 2026-07-25 RearPickerX 폭주(팔로잉 최초 명령 가감속 20배).
**이번 작업은 속도/가감속 인자를 계산하거나 변경하지 않는다.** `targetName` 문자열 1개를 추가로
전달하는 작업이다. 기존 velocity/acceleration/deceleration 인자와 그 값의 출처를 손대지 말 것.

### 규칙 3. 스케일을 적용하지 않아야 하는 값과 혼동하지 않는다
명시 velocity(`velocity > 0`), Jog, ScanVelocity, HomeVelocity에는 적용하지 않는다. 이중 스케일 금지.

### 규칙 4. 범위 밖에서 스케일 미적용 지점을 발견하면 고치지 말고 보고한다

### ★ 규칙 5. 보고 의무 (반드시 이행 — 하나라도 빠지면 작업 미완료)
1. 변경 지점 목록 (`파일:행` + 변경 내용).
2. 스케일 미적용 잔존 지점 감사 결과
   (`grep -rn "Config\.DefaultVelocity\|Config\.Acceleration\|Config\.Deceleration" --include=*.cs QMC.CDT-320 QMC.Common`).
   없으면 "없음" 명시.
3. **[확인 요청 1]** 답변 (오버라이드 통과 후 존 진입 조건까지 통과하는지 — 이번 작업의 최대 불확실성).
4. **[확인 요청 2]** 답변 (Y 충돌 방지 인터락 영향 없음 확인 — 아래 별도 절 참조).
5. 판단이 애매해서 손대지 않은 지점. 없으면 "없음" 명시.

---

# 배경 — 실장비 확정 (2026-07-25 19:36:18)

Auto 운전 중 RearPickerX 팔로잉 진입의 **첫 위치 오버라이드에서 인터락 차단 → 비상정지 →
전 시퀀스 취소**가 발생했다.

```
19:36:17.829  PickUp 피커X 팔로잉 진입을 시작합니다. visionTarget=353.567, pickerTarget=384.567
19:36:18.164  AxisMoveProfile > RearPickerX ABS MOVE. target=699.486   ← 팔로잉 최초 명령
19:36:18.396  ★ Interlock blocked. moving=RearPickerX.
              RearPickerX 이동 불가: Manual X 목표 존을 판단할 수 없습니다.
              target=687.786, targetName=PositionOverride
19:36:18.399  Machine status set to Alarm. code=INTERLOCK
19:36:18.400  Interference group stop requested. sourceAxis=RearPickerX, emergency=True
19:36:18.402  Front/Rear/Input/Output 전 시퀀스 취소
```

## 차단 지점
`PickerRearInterlockRules.cs:252` `CanManualRearPickerX`:
```csharp
PickerWorkZone targetZone = PickerZoneInterlockRules.ResolveManualPickerXTargetZone(request, false);
if (targetZone == PickerWorkZone.Unknown)
    return MotionGuardRuleHelpers.Block("RearPickerX",
        "RearPickerX 이동 불가: Manual X 목표 존을 판단할 수 없습니다. ...", out reason);
```
Auto 경로(`CanAutoRearPickerX`, `:209`)도 `CanManualRearPickerX`를 먼저 통과해야 하므로 자동 운전에서
걸린다. Front도 동일 구조(`PickerFrontInterlockRules.cs:612`).

## Unknown이 된 이유
존 판정은 `PickerZoneInterlockRules.cs:2810` `ResolvePickerXZoneByNameOrPosition`이 수행한다.
세 조건이 모두 겹쳤다:

| 판정 요소 | 실제 값 | 결과 |
|---|---|---|
| `targetName` | **`"PositionOverride"`** (하드코딩) | `ParseZone` 미인식 → Unknown |
| 목표 위치 | **687.786** (팔로잉 중간 세그먼트) | 어떤 티칭 존 구간에도 속하지 않음 → Unknown |
| 인코더 존 설정 | 켜져 있음 | `targetName` 폴백 차단(`:2832~2833`) → **Unknown 확정** |

`"PositionOverride"`는 `AjinAxis.TryOverridePosition`(`:306`)이
`MotionGuardRuntime.VerifyAxisTeachingMove(this, targetPosition, "PositionOverride", out reason)`로
넘기는 고정 문자열이다(실장비 경로 `:346` 부근, 시뮬 경로 `:319` 부근). 시퀀스가 만든
`targetName`(예: `PickerPickUp;Side=Rear;PickerZone=Input;Owner=...`)이 오버라이드 경로에 전달되지 않는다.

## 이번 작업(A안)의 목표
`TryOverridePosition`에 `targetName` 인자를 추가하고, 팔로잉이 **최종 목표의 존 의도를 담은
targetName**을 전달한다. 그러면 존 판정이 최종 목표 의도로 통과하고, 중간 좌표는 그대로
위치 기반 안전 검증(SharedRailX 페어 간격, Y 대향 거리 등)을 받는다.

---

# 수정 내용

## 수정 A. `AjinAxis.TryOverridePosition` — targetName 인자 추가

### A-1. 시그니처 (`:306`)
```csharp
        /// <summary>
        /// 구동 중인 축의 목표 위치를 오버라이드한다.
        /// targetName: MotionGuard 존 판정에 쓰이는 이동 의도 문자열.
        ///   기존 조건: "PositionOverride" 고정 문자열을 넘겨, 인코더 존이 설정된 축에서
        ///             목표 존을 판단할 수 없어(Unknown) 팔로잉 오버라이드가 -11로 차단됐다
        ///             (실장비 2026-07-25 19:36, RearPickerX target=687.786).
        ///   현재 기준(사용자 승인 2026-07-25, A안): 호출자가 최종 목표의 존 의도를 담은
        ///             targetName을 전달한다. 미지정(null/빈문자)이면 기존 "PositionOverride"로 폴백해
        ///             기존 호출부 동작을 유지한다.
        /// </summary>
        public int TryOverridePosition(
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration,
            string targetName = null)
```
- **기본값 `= null`로 두어 기존 호출부가 컴파일 에러 없이 유지되게 한다.**
- 인자 순서는 기존 4개 뒤에 추가한다. 앞에 끼워넣지 말 것.

### A-2. 본문에서 가드 호출 2곳 교체
메서드 안에 `MotionGuardRuntime.VerifyAxisTeachingMove(this, targetPosition, "PositionOverride", ...)`가
**시뮬 경로(`:319` 부근)와 실장비 경로(`:346` 부근) 두 곳** 있다. 두 곳 모두 아래로 바꾼다.

본문 진입부(첫 `try` 직후)에 폴백 1줄 추가:
```csharp
                // 존 판정용 이동 의도. 미지정이면 기존 동작(폴백)을 유지한다.
                string guardTargetName = string.IsNullOrWhiteSpace(targetName)
                    ? "PositionOverride"
                    : targetName;
```
그리고 두 가드 호출의 세 번째 인자를 `"PositionOverride"` → `guardTargetName`으로 교체한다.
**가드 호출의 나머지 인자, 반환 코드(-11), 호출 순서, `CheckSoftLimitTarget` 위치는 손대지 말 것.**

### A-3. 오버라이드 로그에 targetName 추가
기존 `LogOverrideCall`(또는 `AXM-OVERRIDE` 로그) 경로는 `QMC.Common\AjinE\AXM.cs`에 있어
**수정 금지**다. 대신 `AjinAxis` 쪽 실패 로그(`FailMotion("POSITION OVERRIDE", ...)`)의 메시지에
`guardTargetName`을 덧붙여 다음 차단 시 어떤 의도로 요청했는지 남긴다. 성공 경로에는 로그를
추가하지 않는다(폴링마다 발생하므로).

## 수정 B. `FollowMoveAsync` — 후행축 최종 목표의 targetName 전달

### B-1. 시그니처에 인자 추가
`FollowMoveAsync`(`:555` 부근)의 파라미터 목록 **맨 끝(`CancellationToken ct` 앞)** 에 추가:
```csharp
            int timeoutMs = 0,
            string trailingTargetName = null,
            CancellationToken ct = default(CancellationToken))
```
- 기본값 `null`로 기존 호출부 호환을 유지한다.
- **`ct`는 반드시 마지막 파라미터로 남긴다.** 기존 호출부가 `ct`를 위치 인자로 넘기고 있으면
  컴파일 에러가 나므로, 에러가 나는 호출부는 **명명 인자(`ct: ct`)로 고치거나 새 인자를 함께
  전달**해 해결한다. 호출부 4곳은 수정 C에서 모두 손대므로 문제되지 않는다.

### B-2. 루프 안 `TryOverridePosition` 호출 (`:755` 부근)
```csharp
                                int overrideResult = TryOverridePosition(
                                    command, followVel, followAcc, followDec, trailingTargetName);
```
- `MoveAbsoluteForFollowAsync`(팔로잉 최초 명령)는 **손대지 않는다.** 그 경로는
  `MoveAbsoluteAsync` → `BaseAxis.VerifyMotionGuard`가 자체 targetName 체계를 쓰고, 실장비 로그에서
  `target=699.486` 최초 명령은 정상 통과했다.
- `TryOverrideVelocity`(최종 구간 증속)는 위치를 바꾸지 않아 존 판정과 무관하다. 손대지 않는다.

### B-3. 시작 로그에 targetName 추가
`AX-FOLLOW-MOVE ... 팔로잉 이동을 시작합니다` 로그(`:621` 부근)에
`", trailingTargetName=" + (trailingTargetName ?? "<null>")`를 덧붙인다.

## 수정 C. 호출부 4곳에서 targetName 전달

각 호출부는 **이미 해당 이동의 targetName을 알고 있거나 만들 수 있다.** 새로 문자열을 조립하지 말고
**그 경로가 일반 이동에 쓰는 것과 동일한 targetName을 그대로 전달**한다.

### C-1. `PickerPickUpSequence.cs` — 픽업 피커X 팔로잉
`TryFollowPickerXBehindInputVisionRetreatAsync`(`:1468` 부근)에서 `FollowMoveAsync` 호출 시
`trailingTargetName`을 전달한다. 값은 **같은 진입에서 일반 이동이 쓰는 targetName**이다:
- `MovePickerXEntryByVisionFollowOrFallbackAsync`가 이미 `targetName` 파라미터를 받아
  폴백 시 `MovePickerAxisAndVerifyAsync(..., targetName)`으로 넘긴다. **그 값을 그대로 follow에도
  전달**하도록 `TryFollowPickerXBehindInputVisionRetreatAsync`에 `string targetName` 파라미터를 추가해
  이어준다.
- 그 targetName에 `PickerZone=Input`이 포함되는지 확인하고, 포함되지 않으면 **[확인 요청 1]로 보고**한다.

### C-2. `InputDieVisionPrepareSequence.cs` — 비전X return-follow
`TryFollowInputVisionXBehindPickerAsync`의 `FollowMoveAsync` 호출.
후행축이 **InputVisionX**이므로 Picker 존 판정 규칙과 무관하다. 하지만 일관성을 위해
같은 경로의 일반 이동이 쓰는 targetName(`AutoInputDieVisionPrepare;Side=...;VisionX;...` 형태)을
전달한다.
> ⚠ 이 파일은 "수정 파일" 목록에 없다. **C-2 적용이 필요하다고 판단되면 파일을 고치지 말고
> [확인 요청 1]에 함께 보고**한다. 이번 작업의 필수 대상은 Picker X 팔로잉(C-1, C-3)뿐이다.

### C-3. `PickerPlaceSequence.cs` — 플레이스 피커X 팔로잉
`TryFollowPickerXBehindOutputVisionRetreatAsync`(`:2664` 부근)에서 동일 처리.
`MovePlacePickerXEntryByVisionFollowOrFallbackAsync`가 이미 `targetName`을 받아 폴백에 넘기므로
그 값을 이어준다. `PickerZone=Output` 포함 여부를 확인해 보고한다.

### C-4. `InputVisionXPrePositionCoordinator.cs` — 선행이동 오버라이드 (`:553`)
`ajinAxis.TryOverridePosition(target, motion.Velocity, motion.Acceleration, motion.Deceleration)`
호출에 targetName을 추가한다. 이 경로는 **InputVisionX** 오버라이드이므로 Picker 존 규칙과 무관하나,
`"PositionOverride"` 고정값을 벗어나게 해 진단성을 높인다. 같은 코디네이터가 일반 이동에 쓰는
targetName이 있으면 그것을, 없으면
`"InputVisionXPrePosition;VisionX;중간 안전대기점"` 형태의 고정 문자열을 쓴다.
- `axis.OverridePosition(target)`(`:561`, 비-Ajin 폴백)은 가드를 타지 않으므로 손대지 않는다.

---

## [확인 요청 1] — 오버라이드가 존 판정을 통과한 뒤 "존 진입 조건"까지 통과하는가 (필수 분석)

**이번 작업의 최대 불확실성이며, 분석 결과에 따라 추가 작업이 필요할 수 있다.**

`targetName`에 `PickerZone=Input`을 담아 넘기면 존 판정은 `Unknown`을 벗어나지만, 그 다음
`VerifyManualRearPickerXInputEntry`(`PickerRearInterlockRules.cs:262`) /
`VerifyManualFrontPickerXInputEntry`(Front 대응)가 **Input 존 진입 조건**을 검사한다.
그 조건에는 `InputVisionX가 Avoid 또는 0 이하`가 포함된다
(`VerifyInputVisionXAtAvoidOrBelowZero`, `PickerFrontInterlockRules.cs:842` 부근).

팔로잉의 목적 자체가 "비전이 아직 회피 중일 때 피커가 진입"이므로, 이 조건이 그대로 걸리면
오버라이드마다 다시 `-11`이 난다. 다만 그 함수에는 **페어 간격 제3 분기**
(`MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry`)가 먼저 있어, 간격이 충족되면 통과한다.

아래를 코드로 짚어 보고하라:
1. C-1/C-3이 전달할 targetName의 **실제 문자열**과 `ParseZone` 판정 결과.
2. 그 존으로 판정되면 어떤 진입 검증 함수가 호출되는지, 그 안에서 **페어 간격 제3 분기가 먼저
   평가되는지**.
3. 팔로잉이 유지하는 간격(`safetyGap` = SafetyDistance + Extra, 기본 50)이 제3 분기가 요구하는
   간격(SafetyDistance, 기본 10)보다 크므로 **정상 추종 중에는 통과한다**는 논리가 성립하는지.
4. 성립하지 않는 경로가 있으면 **어느 조건에서 걸리는지 구체적으로** 적고, 코드 수정 없이 보고한다
   (인터락 수정은 별도 승인 사항).

---

## ★ [확인 요청 2] — 공유레일 피커 헤드간 Y 충돌 방지 인터락 정상 동작 확인 (필수)

사용자 요청(2026-07-25): 이번 변경이 **Front/Rear 피커 헤드간 Y 충돌 방지 인터락**에 영향을 주지
않는지 재확인한다. **코드 수정 없이 분석·보고만 한다.**

### 확인 대상 (2중 구조)

**(1) 사전 차단 — `PickerZoneInterlockRules.VerifyFacingYDistanceFirst`**
- `MotionGuardRuleRegistry`(`:129`)에 **가장 먼저 등록**되어 모든 축 이동 요청에서 최우선 평가된다.
- `IsAxisMotionRequest`(`:1345`)가 `AxisTeachingMove`를 포함하므로 **위치 오버라이드도 이 게이트를
  통과해야 한다**(오버라이드는 `VerifyAxisTeachingMove` = `AxisTeachingMove`).
- 두 단계: `VerifyExactPickerYAvoidForFacingMove`(X 안전거리 안에서는 최소 한쪽 PickerY가 정확한
  teaching Avoid에 정지) → `CanMovePickerAxisByFacingYInterlock`(`:640`).
- 기준 거리: `DefaultPickerYFacingXClearance = 150.0`(`:113`), `ResolvePickerYFacingXClearance`.

**(2) 실시간 감시 — `RealtimeCollisionSupervisor.EvaluateRealtime`(`:185`)**
- `bothYNotRetracted && xPathUnsafe && !safePairInitialize` → `RaiseHardStop`(전축 하드정지).
- `DoesXPathEnterFacingClearance`(`:268`)가 **현재 위치 + 이동 경로 구간 겹침**으로 판정한다
  (`XCommand`를 경로 끝으로 사용).
- 실장비 13:55 로그에 이 규칙이 실제로 동작한 기록이 있다
  (`PICKER-FACING-X-INTERLOCK ... required=150, xDistance=30.987`).

### 보고할 항목
1. **(1)이 오버라이드 경로에서도 평가되는지** — `TryOverridePosition` →
   `MotionGuardRuntime.VerifyAxisTeachingMove` → `MotionGuardService` → 룰 레지스트리 순회에
   `VerifyFacingYDistanceFirst`가 포함되는지 호출 체인으로 증명한다.
2. **이번 수정이 (1)의 판정을 바꾸지 않는지** — `VerifyFacingYDistanceFirst`는
   `request.TargetValue`(위치)와 양쪽 PickerY 상태로 판정하며 `targetName`을 **존 판정에 쓰지 않는다**.
   `CanMovePickerAxisByFacingYInterlock`에 `targetName`이 전달되는 경로가 있는지 확인하고,
   있으면 targetName 변경이 판정을 바꿀 수 있는지 분석해 보고한다.
   **바꿀 수 있다면 즉시 보고하고 작업을 중단한다.**
3. **(2)가 오버라이드와 무관하게 계속 동작하는지** — 실시간 감시는 인터락 통과 여부와 독립적으로
   폴링하므로 이번 변경의 영향을 받지 않아야 한다. `XCommand`가 오버라이드로 갱신될 때
   `DoesXPathEnterFacingClearance`의 경로 판정이 **더 넓은 구간**을 보게 되는지(안전 방향) 아니면
   **좁아지는지**(위험 방향) 확인해 보고한다.
4. 시뮬에서 아래 2건을 실행해 결과를 보고한다:
   - 양쪽 PickerY를 전진 상태로 두고 X 거리를 150mm 안으로 접근시키는 이동 → **차단되어야 한다.**
   - 팔로잉 진입 중 상대 피커 Y가 전진 상태가 되는 상황 → 실시간 감시가 하드정지시키는지.

---

## 변경 금지
- **인터락 파일 전체(`QMC.CDT-320\Equipment\Interlocks\*`) — diff 0건.**
  특히 `PickerRearInterlockRules.cs`, `PickerFrontInterlockRules.cs`,
  `PickerZoneInterlockRules.cs`, `RealtimeCollisionSupervisor.cs`,
  `MotionGuardRuntime.cs`, `MotionGuardRuleRegistry.cs`, `MotionGuardRuleHelpers.cs`.
  `ParseZone`/`ResolvePickerXZoneByNameOrPosition`에 새 키워드를 추가해 해결하려 하지 말 것.
- `BeginMotionGuardBypass` 등 우회 API 사용 절대 금지.
- `QMC.Common\AjinE\AXM.cs` — `ModifyPosition` / `LogOverrideCall` / 절대좌표 모드 로직 무변경.
- `QMC.Common\Motion\BaseAxis.cs` — `OverridePosition`, `VerifyMotionGuard` 무변경.
- `MoveAbsoluteForFollowAsync`, `TryOverrideVelocity`, `FollowMoveAsync`의 간격/속도/타임아웃 로직.
- `AlarmResponseService` / 알람 severity·scope — 이번 차단이 `Critical`/`InterferenceGroup`으로
  승격되어 라인을 세우는 문제는 **별도 결정 사항**이다. 손대지 말 것.
- 설정 데이터, `D:\CDT-320\*`, UI.

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과.
2. `git diff --stat` = 위 "수정 파일" 4개 이내. **인터락 폴더가 목록에 없어야 한다**:
   `git diff --stat -- QMC.CDT-320/Equipment/Interlocks` → 빈 출력.
3. `grep -rn "\"PositionOverride\"" --include=*.cs QMC.CDT-320` → `AjinAxis.cs`의 **폴백 1곳만** 남는다.
4. `TryOverridePosition`의 새 인자가 **마지막 위치 + 기본값 null**인지, 기존 4인자 호출이 그대로
   컴파일되는지 확인.
5. `FollowMoveAsync`의 `ct`가 여전히 **마지막 파라미터**인지, 모든 호출부가 컴파일되는지 확인.
6. 규칙 5의 보고 항목 5개 (특히 [확인 요청 1]/[확인 요청 2]).

## 검증 기준
**시뮬**
1. Auto + Conti 픽업 1배치에서 `Manual X 목표 존을 판단할 수 없습니다` 차단이 **0건**.
2. `AX-FOLLOW-MOVE ... 팔로잉 이동을 시작합니다`에 `trailingTargetName=`이 찍히고, 그 값에
   존 의도(`PickerZone=Input` 등)가 포함된다.
3. 팔로잉이 `-11` 없이 최종 목표까지 완주한다.
4. 오버라이드를 쓰지 않는 일반 이동의 인터락 판정은 기존과 동일(회귀 없음).
5. [확인 요청 2]의 시뮬 2건 결과가 "차단됨"으로 나온다.

**실장비 (사용자 실행 — 작업자는 문서로만 제공)**
6. ScalePercent 5%, Auto + Conti 1배치. `Manual X 목표 존` 차단 0건, 비상정지 0건.
7. 차단이 재발하면 그 시점 로그의 `targetName=` 값과 `Interlock blocked` 전문을 보존해 보고.
   ([확인 요청 1]의 존 진입 조건에 걸린 것일 수 있다.)

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. 이 인터락 차단이 단순 `-11` 반환이 아니라 `Critical` / `InterferenceGroup` 알람으로 승격되어
   **라인 전체를 정지**시킨다(19:36:18.399~18.402 로그). 그래서 팔로잉의 R6 폴백(일반 이동 재시도)이
   실행될 기회가 없었다. 차단 severity를 낮춰 폴백이 동작하게 할지는 별도 결정 사항이다.
2. 팔로잉은 본질적으로 "존 사이 중간 좌표"로 여러 번 명령하는데 인터락은 "목표 존을 알아야 통과"를
   요구한다. A안은 최종 목표 의도를 전달해 이 간극을 메우지만, 근본적으로는 오버라이드 전용
   판정 경로(중간 좌표는 위치 기반 안전만 검증)가 더 정합적이다. 인터락 수정이 필요하므로
   승인 후 별도 작업으로 다룬다.
