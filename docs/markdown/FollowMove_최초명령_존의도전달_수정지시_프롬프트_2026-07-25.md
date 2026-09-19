# CDT-320: 팔로잉 최초 이동 명령에도 존 의도(targetName) 전달 — "Manual X 목표 존을 판단할 수 없습니다" 잔여 차단 해소 (B′-1)

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
**수정 파일은 `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` 단 하나다.**

`MoveAbsoluteForFollowAsync`(현재 `:558`)와 `FollowMoveAsync` 내부의 그 호출부(현재 `:784`)만 고친다.

**인터락 파일은 절대 수정 금지 — diff 0건이어야 한다.**
`QMC.Common\Motion\BaseAxis.cs`, `QMC.Common\AjinE\AXM.cs`, 시퀀스, SharedRailX, UI, 설정 데이터,
`D:\CDT-320\*`도 건드리지 않는다.

---

# ★ 작업 규칙 (필수 — 위반 시 작업 중단하고 사용자에게 보고)

### 규칙 1. 공정 속도 구간에는 스케일 미적용 코드를 작성하지 않는다
Auto 운전에서 `Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`을 보드로
내려보내는 경로는 반드시 `MotionSpeedScale.ApplyDefaultVelocityScale` /
`ApplyDefaultAccelerationScale`을 통과한 값만 사용한다. Config 날값 직접 전달 금지.

### 규칙 2. 속도·가속·감속은 항상 같은 배율로 함께 스케일한다
전례: 커밋 `a690f6a5`, 2026-07-25 RearPickerX 폭주(팔로잉 최초 명령 가감속 20배).
**이번 작업은 속도/가감속 인자를 계산하거나 변경하지 않는다.** `targetName` 문자열 1개를 추가
전달하고 가드 스코프 한 줄을 씌우는 작업이다.
`MoveAbsoluteForFollowAsync`의 `useExplicitMotion` / `Config` 임시 치환·원복 로직은 손대지 말 것.

### 규칙 3. 범위 밖에서 스케일 미적용 지점을 발견하면 고치지 말고 보고한다

### ★ 규칙 4. 보고 의무 (반드시 이행 — 하나라도 빠지면 작업 미완료)
1. 변경 지점 목록 (`파일:행` + 변경 내용).
2. **[확인 요청 1]** 답변 — 스코프 매칭 성립 근거를 코드로 증명.
3. **[확인 요청 2]** 답변 — moveKind 변경(AxisMove → AxisTeachingMove)이 적용되는 규칙 집합을
   바꾸는지 분석. **바꾼다고 판단되면 즉시 보고하고 작업 중단.**
4. 스케일 미적용 잔존 지점 감사 결과
   (`grep -rn "Config\.DefaultVelocity\|Config\.Acceleration\|Config\.Deceleration" --include=*.cs QMC.CDT-320 QMC.Common`).
   없으면 "없음" 명시.
5. 판단이 애매해서 손대지 않은 지점. 없으면 "없음" 명시.

---

# 배경 — 실장비 확정 (2026-07-25)

## 1차 수정(A안)으로 오버라이드는 해결됐다
`TryOverridePosition`에 `targetName` 인자가 추가되어(`AjinAxis.cs:306~`), 팔로잉의 **위치 오버라이드**는
존 의도를 담은 targetName을 전달한다. 픽업 호출부(`PickerPickUpSequence.cs:1540`)와 플레이스
호출부(`PickerPlaceSequence.cs:2728`)가 `BuildFollowEntryTargetName(targetName, PickerWorkZone.Input/Output)`을
넘기고, `FollowMoveAsync`가 `trailingTargetName`으로 받아 오버라이드에 전달한다(`:805`).

## 그런데 같은 알람이 재발했다 — targetName이 **비어 있다**
```
Interlock blocked. moving=RearPickerX.
RearPickerX 이동 불가: Manual X 목표 존을 판단할 수 없습니다. target=567.785, targetName=
```
| | 1차(19:36) | 재발 |
|---|---|---|
| `targetName` | `PositionOverride` | **빈 문자열** |
| 발생 경로 | 위치 오버라이드 | **팔로잉 최초 이동 명령** |

## 원인 — 최초 명령은 targetName을 전달할 수단이 없다
팔로잉 최초 명령 경로는
`FollowMoveAsync`(`:784`) → `MoveAbsoluteForFollowAsync`(`:558`) → `MoveAbsoluteAsync`(`:1054`)이다.

```csharp
public override async Task<int> MoveAbsoluteAsync(double targetPos, double velocity = 0)   // :1054
```
**`targetName` 파라미터가 없다.** 가드는 `BaseAxis.VerifyMotionGuard`(`QMC.Common\Motion\BaseAxis.cs:1113`)를
타는데 여기도 targetName을 넘기지 않는다(`guard(this, targetPosition, moveKind, out reason)`).
그래서 `request.TargetName`이 빈 문자열이 되고, `ParseZone("")` → `Unknown` → 차단이다.

1차 수정 시 "최초 명령은 19:36 로그에서 정상 통과했다"고 판단해 제외했으나, 그때 통과한 것은
목표 699.486이 Avoid 존(700 근방)에 속해 **위치 기반 판정이 성공**했기 때문이다. 재발 시 목표
567.785는 어떤 티칭 존 밴드에도 속하지 않는 중간 세그먼트 좌표라 위치 판정이 Unknown이 되었고,
targetName 폴백도 없어 차단됐다.

## 해결 수단 — 기존 스코프 관례를 쓴다
`MoveAbsoluteAsync`의 시그니처를 바꾸지 않고 targetName을 전달하는 방법이 이미 코드베이스에 있다.
`MotionGuardRuntime.BeginAxisTeachingMove(axis, targetPosition, targetName)`(`:169`) 스코프다.
시퀀스의 일반 이동은 전부 이 스코프 안에서 `MoveAbsoluteAsync`를 호출한다
(예: `PickerInputStageMoveHelper.MoveStageYForPickerWorkPointCommandAsync:26`,
`PickerPickUpSequence.cs:9471` 부근).

`MotionGuardRuntime`이 스코프를 소비하는 지점(`:69~72`):
```csharp
AxisMoveScope scope = CurrentAxisMoveScope.Value;
MotionGuardResult result = IsMatchingScope(scope, axis, targetPosition)
    ? service.VerifyAxisTeachingMove(axis, targetPosition, scope.TargetName, context, executionMode)
    : service.VerifyAxisMove(axis, targetPosition, context, skipSharedRailXRule, executionMode);
```

## ★ 스코프 좌표는 "그 호출의 명령 좌표"여야 한다 (B′-1 확정 사유)
`IsMatchingScope`(`:529`)가 **좌표 일치를 요구**한다:
```csharp
return Math.Abs(scope.TargetPosition - targetPosition) <= 0.0001;
```
따라서 스코프에 **최종 목표**를 넣으면 실제 명령 좌표(중간 세그먼트)와 달라 매칭이 실패하고,
`VerifyAxisMove`로 빠져 targetName이 다시 사라진다 — 지금과 동일한 차단이 재발한다.

**그래서 스코프 좌표는 그 호출의 명령 좌표(`command`)로 두고, 존 의도는 `targetName` 문자열에
담는다(사용자 승인 2026-07-25, B′-1).** 판정 결과는 최종 목표 기준과 같다 —
`BuildFollowEntryTargetName`이 이미 최종 목표의 존 토큰(`PickerZone=Input` / `=Output`)을
targetName에 넣어 주고, `PickerZoneInterlockRules`는 그 토큰으로 존을 해석한다.
`IsMatchingScope`를 고쳐 좌표 불일치를 허용하는 방식(B′-2)은 **다른 모든 티칭 이동의 매칭 조건까지
느슨해지므로 채택하지 않는다. 인터락은 손대지 않는다.**

---

# 수정 내용

## 수정 A. `MoveAbsoluteForFollowAsync`에 targetName 인자 추가 + 가드 스코프 적용

### 변경 전 (`:558~588`)
```csharp
        private async Task<int> MoveAbsoluteForFollowAsync(
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration)
        {
            bool useExplicitMotion = Config != null && velocity > 0.0 && acceleration > 0.0 && deceleration > 0.0;
            double oldDefaultVelocity = useExplicitMotion ? Config.DefaultVelocity : 0.0;
            double oldAcceleration = useExplicitMotion ? Config.Acceleration : 0.0;
            double oldDeceleration = useExplicitMotion ? Config.Deceleration : 0.0;
            try
            {
                if (useExplicitMotion)
                {
                    Config.DefaultVelocity = 0.0;
                    Config.Acceleration = acceleration;
                    Config.Deceleration = deceleration;
                }

                return await MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);
            }
            finally
            {
                if (useExplicitMotion)
                {
                    Config.DefaultVelocity = oldDefaultVelocity;
                    Config.Acceleration = oldAcceleration;
                    Config.Deceleration = oldDeceleration;
                }
            }
        }
```

### 변경 후
```csharp
        /// <summary>
        /// 팔로잉 최초 이동 명령. 명시 가감속을 Config 임시 치환으로 전달하고,
        /// MotionGuard 존 판정용 targetName을 AxisTeachingMove 스코프로 전달한다.
        /// 기존 조건: MoveAbsoluteAsync에 targetName 파라미터가 없고 스코프도 열지 않아
        ///           request.TargetName이 빈 문자열이 됐다. 중간 세그먼트 좌표는 티칭 존 밴드 밖이라
        ///           위치 기반 존 판정도 Unknown이 되어 "Manual X 목표 존을 판단할 수 없습니다"로
        ///           차단됐다(실장비 2026-07-25, RearPickerX target=567.785, targetName=빈문자).
        /// 현재 기준(사용자 승인 2026-07-25, B′-1): MotionGuardRuntime.BeginAxisTeachingMove로
        ///           targetName을 전달한다. 스코프 좌표는 MotionGuardRuntime.IsMatchingScope(:529)가
        ///           좌표 일치(±0.0001)를 요구하므로 반드시 "그 호출의 명령 좌표(targetPosition)"를
        ///           쓴다. 최종 목표를 넣으면 매칭이 실패해 targetName이 다시 사라진다.
        ///           최종 목표의 존 의도는 BuildFollowEntryTargetName이 targetName에 담아 준
        ///           PickerZone= 토큰으로 전달되므로, 판정 결과는 최종 목표 기준과 같다.
        ///           중간 좌표의 실제 안전성은 SharedRailX 페어 간격/Y 대향 거리 등 위치 기반 검증이
        ///           그대로 담당한다.
        /// </summary>
        private async Task<int> MoveAbsoluteForFollowAsync(
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration,
            string targetName)
        {
            bool useExplicitMotion = Config != null && velocity > 0.0 && acceleration > 0.0 && deceleration > 0.0;
            double oldDefaultVelocity = useExplicitMotion ? Config.DefaultVelocity : 0.0;
            double oldAcceleration = useExplicitMotion ? Config.Acceleration : 0.0;
            double oldDeceleration = useExplicitMotion ? Config.Deceleration : 0.0;
            try
            {
                if (useExplicitMotion)
                {
                    Config.DefaultVelocity = 0.0;
                    Config.Acceleration = acceleration;
                    Config.Deceleration = deceleration;
                }

                // targetName이 없으면 스코프를 열지 않는다(기존 동작 유지).
                if (string.IsNullOrWhiteSpace(targetName))
                    return await MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);

                using (MotionGuardRuntime.BeginAxisTeachingMove(this, targetPosition, targetName))
                    return await MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);
            }
            finally
            {
                if (useExplicitMotion)
                {
                    Config.DefaultVelocity = oldDefaultVelocity;
                    Config.Acceleration = oldAcceleration;
                    Config.Deceleration = oldDeceleration;
                }
            }
        }
```

구현 요건:
- `targetName`은 **필수 인자(기본값 없음)** 로 둔다. 호출부가 1곳뿐이므로 누락을 컴파일 타임에 잡는다.
- `MotionGuardRuntime`은 `AjinAxis.cs:5`의 `using QMC.CDT320.Interlocks;`로 이미 접근 가능하다.
  **using을 추가하지 말 것.** 접근이 안 되면 정규화된 이름을 쓰고 그 사실을 보고한다.
- `using` 문(스코프)은 `MoveAbsoluteAsync` 호출**만** 감싼다. `Config` 치환/원복은 기존 위치 유지.
- **`Config` 원복이 스코프 종료보다 나중에 일어나도 무해**하다(원복은 `finally`, 스코프는 `using`
  블록 종료 시). 순서를 바꾸려 하지 말 것.
- `targetName` 미지정 시 스코프를 열지 않는 분기를 반드시 둔다 — 빈 스코프를 열면
  `IsMatchingScope`가 성립해 `VerifyAxisTeachingMove`에 빈 targetName이 전달되고, 오히려
  `VerifyAxisMove` 경로(현재 동작)와 달라진다.

## 수정 B. 호출부에 targetName 전달 (`FollowMoveAsync`, `:784`)

### 변경 전
```csharp
                                    moveTask = MoveAbsoluteForFollowAsync(
                                        command, startVelocity, startAcceleration, startDeceleration);
```
### 변경 후
```csharp
                                    moveTask = MoveAbsoluteForFollowAsync(
                                        command, startVelocity, startAcceleration, startDeceleration,
                                        trailingTargetName);
```
- `trailingTargetName`은 `FollowMoveAsync`가 이미 받는 파라미터(`:612`)다. **새로 만들지 말 것.**
- 오버라이드 경로(`:805` `TryOverridePosition(..., trailingTargetName)`)와 **같은 문자열**을 쓰게 되어
  최초 명령과 이후 오버라이드가 동일한 존 의도로 판정된다.

## 수정 C. 최초 명령 로그에 targetName 추가
`:790` 부근 `"팔로잉 최초 이동 명령을 발행했습니다."` 로그에 항목을 추가한다(기존 항목 유지):
```csharp
                                            ", targetName=" + (trailingTargetName ?? "<null>") +
```

---

## 변경 금지
- **인터락 파일 전체(`QMC.CDT-320\Equipment\Interlocks\*`) — diff 0건.**
  특히 `MotionGuardRuntime.cs`의 `IsMatchingScope`(`:529`) / `BeginAxisTeachingMove`(`:169`) /
  스코프 소비부(`:69~72`), `PickerZoneInterlockRules.cs`의 `ParseZone` /
  `ResolvePickerXZoneByNameOrPosition` / `HasExplicitPickerZoneIntent`,
  `PickerFrontInterlockRules.cs` / `PickerRearInterlockRules.cs`,
  `RealtimeCollisionSupervisor.cs`, `MotionGuardRuleRegistry.cs`.
  **존 판정에 새 키워드를 추가하거나 좌표 매칭을 느슨하게 해서 해결하려 하지 말 것.**
- `BeginMotionGuardBypass` 등 우회 API 사용 절대 금지.
- `QMC.Common\Motion\BaseAxis.cs` — `MoveAbsoluteAsync`, `VerifyMotionGuard` 시그니처/본문 무변경.
  **`MoveAbsoluteAsync`에 targetName 파라미터를 추가하는 방식(A′)은 채택하지 않는다** —
  공용 진입점이라 호출부 수십 곳에 파급된다.
- `QMC.Common\AjinE\AXM.cs` 전체 (오버라이드 base/절대좌표 모드 로직).
- `TryOverridePosition`(`:306`) 본문 — 1차 수정으로 이미 targetName을 받는다. 무변경.
- `FollowMoveAsync`의 간격/여유 계산, `direction` 검증, `safetyGap` 클램프, 타임아웃/정지/알람 처리,
  리턴 코드 체계, `TryOverrideVelocity` 호출(최종 구간 증속).
- `MoveAbsoluteForFollowAsync`의 `useExplicitMotion` 판정과 `Config` 치환/원복.
- 시퀀스 파일 전체 — `BuildFollowEntryTargetName`(`PickerSequenceBase.cs:772`)과 호출부
  (`PickerPickUpSequence.cs:1540`, `PickerPlaceSequence.cs:2728`)는 1차 수정에서 이미 완료됐다.
  **읽고 확인만 하고 수정하지 말 것.**

---

# ★ 인터락 사전 점검 결과 (프롬프트 작성 전 전수 조사)

팔로잉 PickerX 이동(`moveKind=AxisTeachingMove`)이 통과해야 하는 규칙을
`MotionGuardRuleRegistry`(`:127~139`) **등록 순서대로** 전부 대입 검사했다.
Rear 픽업 진입(목표 존 Input) 기준이며 Front/Output도 대응 함수가 같은 구조다.

| # | 규칙 | 판정 | 근거 / 위험 |
|---|---|---|---|
| 1 | `PickerZoneInterlockRules.VerifyFacingYDistanceFirst`(`:1156`) | **통과** | 위치·PickerY 상태만 보고 `targetName`은 존 판정에 쓰지 않는다. 이번 수정이 판정을 바꾸지 않음. X 150mm 안이면 한쪽 PickerY가 정확 Avoid여야 하는 조건은 그대로 유효 |
| 2 | `SharedRailXInterlockRules.Verify`(`Common\SharedRailXInterlockRules.cs:8`) | **통과(단, 조건부)** | `MoveKind`가 `AxisMove`/`AxisTeachingMove` 양쪽 모두 대상이라 **TeachingMove 전환으로 빠지지 않는다**. `SkipSharedRailXRule` 또는 `IsInternalDispatch`면 스킵되므로 [확인 요청 2]-2에서 실제 값 확인 필요 |
| 3~6 | Cassette / Feeder / InputStage / Vision | **통과** | 자기 축 대상 규칙. PickerX 이동에는 적용되지 않음 |
| 7·8 | `PickerFront/RearInterlockRules.Verify` → `CanAutoRearPickerX`(`:199`) | 아래 세분 | |
| 8-a | └ `CanManualRearPickerX` 목표 존 Unknown 차단(`:252`) | **이번 수정으로 해소** | targetName의 `PickerZone=` 토큰으로 판정 |
| 8-b | └ `VerifyManualRearPickerXInputEntry`(`:354`) → `VerifyRearPickerZAxesAvoidOrNonNegative` | **통과** | 픽업 진입 전 전 PickerZ Avoid 복귀가 선행됨 |
| 8-c | └ `VerifyInputExpanderZAtOrBelowZero`(`:363`) | **통과** | 공정 중 ExpandingZ는 −0.8 유지 |
| 8-d | └ `VerifyInputFeederAvoidDog`(`:367`) / `VerifyInputFeederDown`(`:371`) | **통과** | 공정 중 피더 Down/Avoid 상태 |
| 8-e | └ **`VerifyInputVisionXAtAvoidOrBelowZero`(`:375`)** | **⚠ 위험 — 최대 불확실** | 팔로잉의 목적 자체가 "비전이 회피 중일 때 진입"이라 이 조건과 정면 충돌. **페어 간격 제3 분기**(`MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry`)가 먼저 평가되어 통과할 것으로 보이나 검증 필요 |
| 8-f | └ `PickerZoneInterlockRules.VerifyPickerXOppositeYClearance`(`:379`) | **통과** | Y 대향 거리. #1과 동일 계열 |
| 8-g | └ Process 존 경유 시 `VerifyRearPickerZAxesAvoidOrNonNegative`(`:274`) | **통과** | Z Avoid 선행 |
| 8-h | └ **`PickerZoneInterlockRules.VerifyRearPickerXMove`(`:278`)** | **⚠ 위험** | 내부에 **작업영역 점유 차단**이 있다. 팔로잉 중간 좌표가 Bottom/Side 밴드를 지나면 `"<zone> 작업 영역을 반대 픽커가 사용 중입니다"`로 차단된다(실장비 2026-07-25 20:27:24 발생) |
| 8-i | └ `VerifyRearPickerZAxesAvoidForMove`(`:213`) | **통과** | Z Avoid |
| 8-j | └ `VerifyVisionXAvoidForColletCalibrationBottomMove`(`:217`) | **통과** | 캘리브레이션 Bottom 이동 전용. 공정 무관 |
| 8-k | └ `VerifyRearPickerNotBusy`(`:225`) | **통과** | 자기 축 busy |
| 9~11 | OutputStage / OutputFeeder / OutputCassette | **통과** | 자기 축 대상 |

## ⚠ 선행 과제 2건 (이 수정만으로는 팔로잉이 완주하지 못할 수 있다)

**선행 과제 1 — 8-h 작업영역 점유 차단**
이번 수정으로 존 판정이 해소되면 **그 다음에 이 차단이 나온다.** 실장비에서 이미 관측된 경로다
(`Bottom 작업 영역을 반대 픽커가 사용 중입니다. owner=FrontPicker...ProcessSide`).
별도 프롬프트 `Interlock_Bottom존_교차작업허용_수정지시_프롬프트_2026-07-25.md`가 이를 다룬다.
**두 프롬프트를 함께 적용하지 않으면 팔로잉은 여전히 차단된다.** 적용 순서는 무관하나 둘 다 필요하다.

**선행 과제 2 — 8-e 비전 Avoid 조건**
`VerifyInputVisionXAtAvoidOrBelowZero`에서 페어 간격 제3 분기가 먼저 평가되지 않으면 팔로잉은
구조적으로 통과할 수 없다. **[확인 요청 3]에서 반드시 확인**하고, 통과하지 못하는 것으로 판정되면
코드를 고치지 말고 즉시 보고한다(인터락 수정은 별도 승인 사항).

## [확인 요청 1] — 스코프 매칭 성립 증명 (필수)
1. `MoveAbsoluteForFollowAsync`가 여는 스코프의 좌표(`targetPosition`)와, 그 안에서
   `MoveAbsoluteAsync`가 가드에 넘기는 좌표(`targetPos`)가 **동일한 값**임을 코드로 확인한다.
   중간에 좌표를 변환·보정하는 지점이 있으면(예: `ToBoardPosition`, 소프트리밋 클램프)
   `IsMatchingScope`의 ±0.0001 허용치를 벗어나는지 확인해 보고한다.
   **벗어난다면 매칭이 실패하므로 즉시 보고하고 작업 중단한다.**
2. `MoveAbsoluteAsync`가 `IsAxisAlreadyAtTarget`(`MotionGuardRuntime.cs:65`)로 조기 통과하는 경우,
   스코프가 열려 있어도 무해한지 확인한다.
3. `trailingTargetName`에 실제로 담기는 문자열(픽업/플레이스 각각)을 적고,
   `PickerZone=Input` / `PickerZone=Output` 토큰이 포함되는지 확인해 보고한다.

## ★ [확인 요청 2] — moveKind 변경 영향 분석 (필수, 위험 항목)
스코프가 매칭되면 판정이 `VerifyAxisMove` → **`VerifyAxisTeachingMove`** 로 바뀐다.
즉 팔로잉 최초 명령의 `moveKind`가 `AxisMove`에서 `AxisTeachingMove`로 변한다.
아래를 분석해 보고하라. **적용되는 규칙이 줄어들거나 안전 검증이 빠진다고 판단되면
즉시 보고하고 작업을 중단한다.**
1. `VerifyAxisMove`와 `VerifyAxisTeachingMove`가 각각 어떤 규칙 집합을 평가하는지
   (`MotionGuardService` 내부). 특히 **SharedRailX 페어 간격 규칙**과
   `PickerZoneInterlockRules.VerifyFacingYDistanceFirst`(Y 대향 충돌 방지)가 **양쪽 모두에서
   평가되는지**.
2. `VerifyAxisMove`에는 `skipSharedRailXRule` 인자가 있고 `VerifyAxisTeachingMove`에는 없다
   (`MotionGuardRuntime.cs:70~72`). 팔로잉 최초 명령이 기존에 `skipSharedRailXRule`을 어떤 값으로
   넘겼는지 확인하고, TeachingMove 전환으로 **SharedRailX 검증이 추가되는지 빠지는지** 판정한다.
3. 오버라이드 경로는 이미 `VerifyAxisTeachingMove`를 쓴다(`TryOverridePosition`).
   최초 명령도 TeachingMove가 되면 **팔로잉 전 구간이 동일한 규칙 집합**으로 검증된다 —
   이것이 일관성 측면에서 개선인지 확인해 보고한다.
4. `MotionGuardMoveKind.AxisTeachingMove`가 `IsAxisMotionRequest`(`PickerZoneInterlockRules.cs:1345`)에
   포함되므로 Y 대향 게이트는 계속 평가된다. 이를 코드로 확인해 보고한다.

## ★ [확인 요청 3] — 8-e 비전 Avoid 조건 통과 여부 (필수, 최대 불확실)
`VerifyInputVisionXAtAvoidOrBelowZero`(`PickerFrontInterlockRules.cs:842` / Rear 대응 `:482`)를 읽고
아래를 코드 순서대로 짚어 보고하라.
1. `MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry(machine, pickerX, request.TargetValue,
   stage.CameraX, out detail)`가 **`stage.CameraX.IsMoving` 검사보다 먼저** 평가되는지.
2. 그 제3 분기가 요구하는 간격(페어 `SafetyDistance`, 기본 10)과 팔로잉이 유지하는 간격
   (`safetyGap` = SafetyDistance + Extra, 기본 50)을 비교해, **정상 추종 중에는 항상 통과**하는지.
3. 제3 분기는 상대 축의 **Actual과 Command 양쪽**을 검사한다
   (`MotionGuardRuleHelpers.cs:37~42`). 비전이 회피(멀어지는) 방향으로 이동 중일 때 Command가 더
   멀어 통과하는지, 접근 방향이면 차단되는지(fail-closed) 확인한다.
4. **팔로잉 최초 명령의 `request.TargetValue`는 중간 좌표**다. 제3 분기가 그 중간 좌표로 간격을
   계산하므로 최종 목표보다 여유가 크다 — 즉 최종 목표에서는 통과할 조건이 중간 좌표에서는 더
   쉽게 통과한다. 반대로 **최종 목표에서 통과하지 못하는 경우**가 있는지(마지막 세그먼트) 확인한다.
5. 통과하지 못하는 경로가 있으면 **어느 조건에서 걸리는지 구체적으로** 적고 코드를 고치지 말고 보고한다.

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과.
2. `git diff --stat` = `AjinAxis.cs` **한 파일만**.
   `git diff --stat -- QMC.CDT-320/Equipment/Interlocks` → **빈 출력**.
   `git diff --stat -- QMC.Common` → **빈 출력**.
3. `MoveAbsoluteForFollowAsync` 호출부가 정확히 1곳이고 `trailingTargetName`을 전달하는지.
4. 스코프의 좌표 인자가 `targetPosition`(= 명령 좌표)인지. **최종 목표를 넣지 않았는지 확인** —
   넣으면 매칭 실패로 이번 수정이 무효가 된다.
5. `targetName` 빈 값일 때 스코프를 열지 않는 분기가 있는지.
6. `using` 스코프가 `MoveAbsoluteAsync` 호출만 감싸고, `Config` 원복 `finally`가 그대로인지.
7. 규칙 4의 보고 항목 5개.

## ★ 인터락 시뮬 검증 (필수 — 코드 수정 전/후 각각 수행하고 결과 보고)

사용자 지시(2026-07-25): 수정 → 인터락 차단 → 재조사 반복으로 작업이 지체된다. **인터락 쪽을
시뮬레이션으로 항상 확인**한 뒤 실장비에 올린다.

### S-1. 규칙별 통과 여부 하네스 (수정 후 필수)
시뮬 모드에서 팔로잉 1사이클을 돌리고, `MotionGuard | Matrix check required` 및
`Interlock blocked` 로그를 전량 수집해 **위 "인터락 사전 점검 결과" 표의 11개 항목 각각이
실제로 통과했는지** 대조표로 보고한다. 차단이 하나라도 나오면 그 규칙명·조건·좌표를 적어 보고한다.

### S-2. 경계 조건 시뮬 (각 케이스 결과 보고)
| 케이스 | 기대 결과 |
|---|---|
| 팔로잉 최초 명령 좌표가 티칭 존 밴드 **밖**(예: 567.785) | **통과** (이번 수정 목적) |
| 팔로잉 최초 명령 좌표가 Bottom/Side 밴드 **안** + 반대 피커가 그 존 점유 | 차단 예상 → **선행 과제 1** 필요 확인 |
| 비전이 회피 이동 **중**일 때 팔로잉 진입 | 통과해야 함 → **선행 과제 2**([확인 요청 3]) 검증 |
| 양쪽 PickerY 전진 + X 거리 150mm 내 접근 | **차단**(Y 대향 게이트 정상) |
| 반대 피커 Y가 Avoid, 자기 Y도 Avoid, 정상 팔로잉 | **통과** |
| 팔로잉 미사용 일반 픽업/플레이스 | 기존과 동일(회귀 없음) |

### S-3. 실장비 투입 전 게이트
S-1에서 차단 0건, S-2의 6개 케이스가 모두 기대 결과와 일치할 때만 실장비에 올린다.
**하나라도 불일치하면 실장비 투입 전에 사용자에게 보고한다.**

## 검증 기준
**시뮬**
1. Auto + Conti 픽업/플레이스 1배치 — `Manual X 목표 존을 판단할 수 없습니다` 차단이 **0건**.
2. `AX-FOLLOW-MOVE ... 팔로잉 최초 이동 명령을 발행했습니다`에 `targetName=`이 찍히고
   `PickerZone=Input`(플레이스는 `=Output`) 토큰이 포함된다.
3. `MotionGuard | Matrix check required` 로그에서 팔로잉 최초 명령의 `moveKind=AxisTeachingMove`,
   `targetName=`에 존 토큰이 보인다.
4. 팔로잉이 `-11` 없이 최종 목표까지 완주한다.
5. **회귀 확인**: 오버라이드를 쓰지 않는 일반 이동, 그리고 팔로잉을 쓰지 않는 픽업/플레이스의
   인터락 판정이 기존과 동일하다.
6. Y 대향 회귀: 양쪽 PickerY 전진 상태에서 X 거리를 150mm 안으로 접근시키면 **여전히 차단**된다.

**실장비 (사용자 실행 — 작업자는 문서로만 제공)**
7. ScalePercent 5%, Auto 1배치. `Manual X 목표 존` 차단 0건, 비상정지 0건.
8. 차단이 재발하면 `targetName=` 값과 `Interlock blocked` 전문을 보존해 보고.
   - `targetName=`이 여전히 비어 있으면 스코프 매칭 실패([확인 요청 1]).
   - `targetName`에 존 토큰이 있는데도 차단되면 **존 진입 조건**에 걸린 것이다
     (예: `InputVisionX가 Avoid/0 이하가 아니고 페어 간격도 부족합니다`) — 별건이므로 보고만.

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. 팔로잉은 중간 좌표를 반복 명령하는데 인터락은 "목표 존을 알아야 통과"를 요구한다. 이번 수정은
   targetName에 존 토큰을 실어 그 간극을 메우는 우회이며, 근본적으로는 **오버라이드/팔로잉 전용
   판정 경로**(중간 좌표는 위치 기반 안전만 검증, 존 진입 조건은 최종 목표 1회만 검증)가 정합적이다.
   인터락 구조 변경이므로 승인 후 별도 작업으로 다룬다.
2. 이 차단이 `Critical` / `InterferenceGroup` 알람으로 승격되어 라인 전체를 세운다. 그래서 팔로잉의
   R6 폴백(일반 이동 재시도)이 실행될 기회가 없다. 차단 severity 정책은 별도 결정 사항이다.
3. `MoveAbsoluteAsync`가 targetName을 받지 못하는 구조 자체가 이런 누락의 원인이다. 공용 진입점
   시그니처 변경은 파급이 크므로 이번에는 스코프 관례로 우회했다. 장기적으로는 `BaseAxis` 레벨에서
   존 의도를 전달하는 설계 정리가 필요하다.
