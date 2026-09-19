# CDT-320: FollowMoveAsync 팔로잉 구간 속도/가감속 스케일 정합 + 오버라이드 로그 신설

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
**수정 파일은 `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` 단 하나다.**
시퀀스/유닛/인터락/SharedRailX/UI 파일은 절대 건드리지 않는다.

---

# ★ 작업 규칙 (필수 — 위반 시 작업 중단하고 사용자에게 보고)

이 규칙은 이번 작업뿐 아니라 이 저장소의 모션 코드 전반에 적용되는 상시 규칙이다.

### 규칙 1. 공정 속도 구간에는 스케일 미적용 코드를 작성하지 않는다
"공정 속도 구간"이란 **Auto 운전에서 축 `Config.DefaultVelocity` / `Config.Acceleration` /
`Config.Deceleration`을 실제 이동값으로 해석해 보드로 내려보내는 모든 경로**를 말한다.
이 구간의 코드는 반드시 `MotionSpeedScale.ApplyDefaultVelocityScale(...)` 또는
`MotionSpeedScale.ApplyDefaultAccelerationScale(...)`를 통과한 값만 사용한다.
`Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`을 **날값 그대로 보드
명령에 전달하는 코드를 새로 작성하는 것을 금지한다.**

### 규칙 2. 속도·가속·감속은 항상 같은 배율로 함께 스케일한다
속도만 스케일하고 가감속을 원본(100%)으로 남기는 코드는 절대 금지다.
이 저장소는 이미 같은 함정으로 사고가 있었다 (커밋 `a690f6a5` — 캘리브레이션 Avoid 이동에서
속도만 스케일되고 가감속이 100%로 나갔다). 세 성분을 한 세트로 다룬다.

### 규칙 3. 스케일을 적용하지 **않아야** 하는 값과 혼동하지 않는다
`MotionSpeedScale` 문서 규약(`QMC.Common\Motion\MotionSpeedScale.cs:10`)에 따라 아래에는
스케일을 적용하지 않는다. 이중 스케일은 또 다른 결함이다.
- 외부에서 명시로 전달된 velocity(`velocity > 0` 인자)
- Jog 속도, Mapping ScanVelocity, HomeVelocity
- 이미 스케일이 적용된 값(호출부가 `Apply...Scale`를 거쳐 넘긴 값)

### 규칙 4. 기존 코드에서 스케일 미적용 지점을 발견하면 고치지 말고 보고한다
이 프롬프트가 지정한 수정 대상 외의 위치에서 스케일 미적용을 발견하면 **수정하지 말고**
목록으로 정리해 보고한다. 범위 밖 수정은 금지다.

### ★ 규칙 5. 보고 의무 (반드시 이행)
작업 완료 보고에 아래 3개 항목을 **반드시** 포함한다. 하나라도 빠지면 작업 미완료로 간주한다.

1. **이번에 스케일을 적용/정합한 지점 목록** — `파일:행` + 변경 전/후 값의 출처.
2. **감사 중 발견한 스케일 미적용 잔존 지점 전체 목록** — `파일:행` + 어떤 실행 경로에서
   발현되는지 + 예상 영향(몇 배로 나가는지). **없으면 "없음"이라고 명시**한다.
   최소한 아래 감사는 수행하고 결과를 보고한다:
   ```bash
   grep -rn "Config\.DefaultVelocity\|Config\.Acceleration\|Config\.Deceleration" --include=*.cs QMC.CDT-320 QMC.Common
   ```
   각 히트가 (a) `Apply...Scale`로 감싸져 있는지 (b) 규칙 3의 예외인지 (c) 미적용 결함인지 분류한다.
3. **판단이 애매해서 손대지 않은 지점** — 있으면 이유와 함께. 없으면 "없음"이라고 명시.

---

# 배경 — 실장비 사고

Auto 운전 중 팔로잉 진입 구간에서 **PickerX가 폭주하는 모양**이 발생해 작업자가 E-Stop을 눌렀다.
아래 3건이 코드 감사로 확인된 원인/기여 요인이다.

## 결함 1 (폭주 최우선 용의자): 팔로잉 최초 명령에서 가감속이 100%로 나간다

`FollowMoveAsync`(`AjinAxis.cs:501`)는 팔로잉 최초 이동을 `:662`에서 발행한다:
```csharp
double startVelocity = commandIsFinal ? trailVel : followVel;
moveTask = MoveAbsoluteAsync(command, startVelocity);
```
`followVel = Math.Min(leadVel, trailVel)`(`:561`)이고 `leadVel`은 **선행축(비전)** 의 속도다.

`MoveAbsoluteAsync`(`:956`)는 가감속 스케일 여부를 velocity 값으로 **추론**한다:
```csharp
bool useDefaultMotionScale = velocity <= 0.0 ||
    MotionSpeedScale.MatchesDefaultVelocityScale(velocity, Config.DefaultVelocity);
double acceleration = useDefaultMotionScale ? ApplyDefaultAccelerationScale(Config.Acceleration) : Config.Acceleration;
double deceleration = useDefaultMotionScale ? ApplyDefaultAccelerationScale(Config.Deceleration) : Config.Deceleration;
```
`MatchesDefaultVelocityScale`(`MotionSpeedScale.cs:188`)는 전달 속도가 **그 축 자신의**
스케일된 DefaultVelocity와 상대오차 1e-6 내로 같을 때만 true다.

비전이 더 느려 `followVel = leadVel`이 되는 정상 케이스에서 이 값은 피커의 스케일된
DefaultVelocity와 다르므로 매칭이 **false** → `useDefaultMotionScale = false` →
**가속/감속이 `Config.Acceleration`/`Config.Deceleration` 원본 100%로 보드에 나간다.**
ScalePercent 20% 운전이면 가감속이 의도의 5배다. 팔로잉은 폴링마다 위치 오버라이드로 목표를
연장하므로 세그먼트마다 100% 가속이 재차 걸려 축이 튀어나가는 모양이 된다.

## 결함 2: FollowMoveAsync 내부 폴백만 MotionSpeedScale을 적용하지 않는다

`:549~563`:
```csharp
double leadVel = leadingVelocity > 0.0 ? leadingVelocity : (leadingAxis.Config != null ? leadingAxis.Config.DefaultVelocity : 0.0);
double leadAcc = leadingAcceleration > 0.0 ? leadingAcceleration : (leadingAxis.Config != null ? leadingAxis.Config.Acceleration : 0.0);
double leadDec = leadingDeceleration > 0.0 ? leadingDeceleration : (leadingAxis.Config != null ? leadingAxis.Config.Deceleration : 0.0);
double trailVel = trailingVelocity > 0.0 ? trailingVelocity : Config.DefaultVelocity;
double trailAcc = trailingAcceleration > 0.0 ? trailingAcceleration : Config.Acceleration;
double trailDec = trailingDeceleration > 0.0 ? trailingDeceleration : Config.Deceleration;
```
날 Config 값이다. 이 파일의 다른 모든 진입점은 ≤0 폴백에 스케일을 적용한다 —
`MoveAbsoluteAsync:958`, `TryOverridePosition:357~365`, `TryOverrideVelocity:417~425`.
**`FollowMoveAsync`만 유일한 예외**이며 규칙 1 위반이다.
(현재 호출부 4곳은 모두 스케일된 값을 넘기므로 잠재 결함이지만, 어느 축이든 Config 성분이
0/미설정이면 즉시 100% 경로로 빠진다.)

## 결함 3: 최종 구간 증속 — 유지하되 로그로 검증 가능하게 한다

`:707~716`에서 최종 구간 진입 시 `TryOverrideVelocity(trailVel, trailAcc, trailDec)`로
`followVel`(느린 쪽)에서 `trailVel`(후행축 자기 속도)로 증속한다. 설계된 동작이므로
**동작은 유지**하되, 실제 명령값이 로그에 남지 않아 사고 후 검증이 불가능했다.

## 진단 공백: 오버라이드가 로그를 남기지 않는다

`TryOverridePosition`(`:303~407`)은 **성공 시 로그가 0건**이다. 폴링마다
`AXM.ModifyPosition`이 나가는데 명령 위치/속도/가감속 기록이 전혀 없다.
`AX-FOLLOW-MOVE` 로그(`:565`, `:668`, `:712`)도 velocity만 있고 acc/dec/scalePercent가 없다.
그래서 이번 폭주 순간 보드에 실제로 나간 값을 로그로 재구성할 수 없었다.

---

# 수정 내용

## 수정 A (결함 2) — FollowMoveAsync 폴백 6개에 스케일 적용

`:549~560`의 6개 폴백을 전부 스케일 경유로 바꾼다. **호출부가 넘긴 명시값(>0)은 이미 스케일된
값이므로 그대로 사용한다 — 규칙 3에 따라 이중 스케일 금지.**

변경 후:
```csharp
                // 규칙 1/2(2026-07-25): 폴백도 반드시 MotionSpeedScale을 경유한다.
                //   기존 조건: Config 날값을 그대로 사용해 스케일 미적용 100%로 나갔다(이 파일의
                //   다른 진입점 MoveAbsoluteAsync:958 / TryOverridePosition:357 / TryOverrideVelocity:417과
                //   불일치). 명시 인자(>0)는 호출부가 이미 스케일한 값이므로 재스케일하지 않는다.
                double leadVel = leadingVelocity > 0.0
                    ? leadingVelocity
                    : MotionSpeedScale.ApplyDefaultVelocityScale(leadingAxis.Config != null ? leadingAxis.Config.DefaultVelocity : 0.0);
                double leadAcc = leadingAcceleration > 0.0
                    ? leadingAcceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(leadingAxis.Config != null ? leadingAxis.Config.Acceleration : 0.0);
                double leadDec = leadingDeceleration > 0.0
                    ? leadingDeceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(leadingAxis.Config != null ? leadingAxis.Config.Deceleration : 0.0);
                double trailVel = trailingVelocity > 0.0
                    ? trailingVelocity
                    : MotionSpeedScale.ApplyDefaultVelocityScale(Config.DefaultVelocity);
                double trailAcc = trailingAcceleration > 0.0
                    ? trailingAcceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Acceleration);
                double trailDec = trailingDeceleration > 0.0
                    ? trailingDeceleration
                    : MotionSpeedScale.ApplyDefaultAccelerationScale(Config.Deceleration);
```
`followVel`/`followAcc`/`followDec`의 `Math.Min` 계산(`:561~563`)은 무변경.

## 수정 B (결함 1) — 팔로잉 최초 명령에 가감속을 명시 전달

`MoveAbsoluteAsync`의 `useDefaultMotionScale` 추론에 의존하지 않고, 팔로잉이 계산한
`followAcc`/`followDec`(수정 A로 스케일 보장됨)를 명시로 내려보낸다.

### B-1. 명시 가감속 이동 헬퍼 신설 (`AjinAxis` 내부 private)
이 저장소에 이미 있는 확립된 패턴을 그대로 따른다 —
`SharedRailXMotionRuntime.MoveAxisWithTemporaryMotionAsync`(`:309~344`)가
`Config.Acceleration/Deceleration`을 임시 치환하고 `finally`에서 원복한 뒤
`MoveAbsoluteAsync`를 호출한다. **같은 방식으로 AjinAxis 내부에 국소 헬퍼를 만든다**
(외부 런타임 의존을 새로 만들지 않는다).

`FollowMoveAsync` 바로 위에 추가:
```csharp
        // 규칙 2(2026-07-25): 팔로잉 명령은 속도·가속·감속을 한 세트로 명시 전달한다.
        // 기존 조건: MoveAbsoluteAsync(command, velocity) 2인자 호출 — 가감속 스케일 여부를
        //   MatchesDefaultVelocityScale 추론에 맡겼고, followVel=Min(선행,후행)은 후행축의 스케일
        //   DefaultVelocity와 일치하지 않아 가감속이 Config 원본 100%로 나갔다(실장비 폭주 원인).
        // 현재 기준: SharedRailXMotionRuntime.MoveAxisWithTemporaryMotionAsync(:309)와 동일한
        //   Config 임시 치환 패턴으로 명시 가감속을 보장한다. acc/dec는 호출 전에 이미
        //   MotionSpeedScale을 경유한 값이어야 한다(수정 A가 보장).
        // 주의: Config를 이동 구간 동안 임시 치환하므로 반드시 finally에서 원복한다.
        //       이 헬퍼는 FollowMoveAsync 전용이며 다른 곳에서 호출하지 않는다.
        private async Task<int> MoveAbsoluteForFollowAsync(
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration)
        {
            bool useExplicitMotion = Config != null && acceleration > 0.0 && deceleration > 0.0;
            double oldAcceleration = useExplicitMotion ? Config.Acceleration : 0.0;
            double oldDeceleration = useExplicitMotion ? Config.Deceleration : 0.0;
            try
            {
                if (useExplicitMotion)
                {
                    Config.Acceleration = acceleration;
                    Config.Deceleration = deceleration;
                }

                return await MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);
            }
            finally
            {
                if (useExplicitMotion)
                {
                    Config.Acceleration = oldAcceleration;
                    Config.Deceleration = oldDeceleration;
                }
            }
        }
```

### B-2. 호출부 교체 (`:658~671` 블록)
변경 전:
```csharp
                                    double startVelocity = commandIsFinal ? trailVel : followVel;
                                    moveTask = MoveAbsoluteAsync(command, startVelocity);
                                    lastCommanded = command;
                                    commandIssued = true;
                                    if (!firstCommandLogged)
                                    {
                                        firstCommandLogged = true;
                                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                            Name + " 팔로잉 최초 이동 명령을 발행했습니다. command=" + command.ToString("F3") +
                                            ", velocity=" + startVelocity.ToString("F3") + " - Ok");
                                    }
```
변경 후:
```csharp
                                    double startVelocity = commandIsFinal ? trailVel : followVel;
                                    double startAcceleration = commandIsFinal ? trailAcc : followAcc;
                                    double startDeceleration = commandIsFinal ? trailDec : followDec;
                                    moveTask = MoveAbsoluteForFollowAsync(
                                        command, startVelocity, startAcceleration, startDeceleration);
                                    lastCommanded = command;
                                    commandIssued = true;
                                    if (!firstCommandLogged)
                                    {
                                        firstCommandLogged = true;
                                        QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                            Name + " 팔로잉 최초 이동 명령을 발행했습니다. command=" + command.ToString("F3") +
                                            ", velocity=" + startVelocity.ToString("F3") +
                                            ", acc=" + startAcceleration.ToString("F3") +
                                            ", dec=" + startDeceleration.ToString("F3") +
                                            ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Ok");
                                    }
```
`commandIsFinal`일 때 후행축 자기 프로파일(trail*), 중간 세그먼트는 팔로잉 프로파일(follow*)을
쓰는 기존 의도를 그대로 유지한다.

## 수정 C — 팔로잉 시작 로그에 가감속/스케일 추가

`:565~572` Start 로그에 항목을 추가한다(기존 항목은 유지):
```csharp
                    ", followVel=" + followVel.ToString("F3") +
                    ", followAcc=" + followAcc.ToString("F3") +
                    ", followDec=" + followDec.ToString("F3") +
                    ", trailVel=" + trailVel.ToString("F3") +
                    ", trailAcc=" + trailAcc.ToString("F3") +
                    ", trailDec=" + trailDec.ToString("F3") +
                    ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Start");
```

## 수정 D — 최종 구간 증속 로그 보강 (결함 3, 동작 유지)

`:710~715`. `TryOverrideVelocity(trailVel, trailAcc, trailDec)` 호출은 **그대로 두고**
반환값을 받아 로그에 남긴다:
```csharp
                                int finalOverrideResult = 0;
                                if (IsMoving)
                                    finalOverrideResult = TryOverrideVelocity(trailVel, trailAcc, trailDec);
                                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-MOVE",
                                    Name + " 팔로잉 최종 구간에 진입했습니다. target=" + trailingTargetPosition.ToString("F3") +
                                    ", velocity=" + trailVel.ToString("F3") +
                                    ", acc=" + trailAcc.ToString("F3") +
                                    ", dec=" + trailDec.ToString("F3") +
                                    ", followVel=" + followVel.ToString("F3") +
                                    ", overrideResult=" + finalOverrideResult +
                                    ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") +
                                    (commandIssued ? "" : " (명령 유지)") + " - Ok");
```
**증속 동작 자체(followVel → trailVel)는 변경하지 않는다.** 유지 여부는 사용자 결정 사항이므로
보고서에 "최종 구간 증속 폭 = trailVel/followVel 배수"를 명시해 보고한다.

## 수정 E — 위치 오버라이드 로그 신설 (진단 공백 해소)

`:674~701`의 `TryOverridePosition(command, followVel, followAcc, followDec)` 호출부에 로그를
추가한다. **폴링마다 남기면 로그가 폭주하므로 아래 조건으로 제한한다:**
- 팔로잉 1회당 **최초 오버라이드 1건**은 반드시 남긴다.
- 이후는 **1초에 1건 이하**로 제한한다(`Stopwatch` 기반 — 이 메서드에 이미 `stopwatch`가 있으므로
  마지막 로그 시각을 담는 `long lastOverrideLogMs = -1;` 지역 변수를 루프 밖에 추가해 사용).
- 실패(0 이외 반환)는 **제한 없이 매번** 남긴다.

로그 항목: `command`, `followVel`, `followAcc`, `followDec`, `leadingActual`, `gap`, `slack`,
`overrideResult`, `scalePercent`.
```csharp
                                QMC.Common.Log.Write("Motion", "SYSTEM", "AX-FOLLOW-OVERRIDE",
                                    Name + " 팔로잉 위치 오버라이드. command=" + command.ToString("F3") +
                                    ", vel=" + followVel.ToString("F3") +
                                    ", acc=" + followAcc.ToString("F3") +
                                    ", dec=" + followDec.ToString("F3") +
                                    ", leadingActual=" + leadingActual.ToString("F3") +
                                    ", gap=" + gap.ToString("F3") +
                                    ", slack=" + slack.ToString("F3") +
                                    ", result=" + overrideResult +
                                    ", scalePercent=" + (MotionSpeedScale.EffectiveScaleFactor * 100.0).ToString("0.#") + " - Check");
```
기존 `-11` / 기타 실패 로그(`:687`, `:696`)는 그대로 유지한다(중복 로그 허용).

---

## 변경 금지
- `MoveAbsoluteAsync`(`:894~`) 본문 — 특히 `useDefaultMotionScale` 판정(`:956`)과
  `AxisMoveProfile` 로그(`:971~977`)는 **손대지 않는다.** 다른 수십 개 호출부의 동작이 바뀐다.
- `TryOverridePosition`(`:303`) / `TryOverrideVelocity`(`:413`) 본문 — 스케일 폴백이 이미 정상이다.
- `MotionSpeedScale`(`QMC.Common\Motion\MotionSpeedScale.cs`) 전체 — 판정식/상수 무변경.
- `WaitUntilMoveDone` / `WaitMoveCompleteAsync` / 리미트 서치 / `Task.Delay` 폴링 주기 —
  별도 지시로 진행 중이므로 이번 작업에서 건드리지 않는다.
- `FollowMoveAsync`의 간격/여유 계산(`:627~652`), `direction` 검증, `safetyGap` 클램프(`:543~545`),
  타임아웃/알람/정지 처리, 리턴 코드 체계 — 전부 무변경.
- 팔로잉 호출부 4곳 — **읽기만 하고 수정 금지.** (감사 대상이며 결과는 보고에 포함)
  - `Sequencing\Picker\PickerPickUpSequence.cs:1409` (`:1443~1456` 스케일부)
  - `Sequencing\Picker\InputDieVisionPrepareSequence.cs:2565` (`:2599~2610`)
  - `Sequencing\Picker\PickerPlaceSequence.cs:2574` (`:2605~2616`)
  - `Sequencing\OutputStage\OutputPostPlaceInspectionQueue.cs:1636`

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과.
2. `git diff --stat`이 `AjinAxis.cs` 한 파일만 표시.
3. `FollowMoveAsync` 안에서 `MoveAbsoluteAsync(` 직접 호출이 **0건**(전부
   `MoveAbsoluteForFollowAsync` 경유)인지 확인.
4. `FollowMoveAsync` 안에서 `Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`이
   `MotionSpeedScale.Apply...`로 감싸지지 않은 채 등장하는 곳이 **0건**인지 확인.
5. `MoveAbsoluteForFollowAsync`의 `finally` 원복이 모든 경로(정상/예외)에서 실행되는지 확인.
   그리고 이 헬퍼의 호출부가 `FollowMoveAsync` 1곳뿐인지 확인.
6. `MoveAbsoluteAsync` / `TryOverridePosition` / `TryOverrideVelocity` / `MotionSpeedScale` diff 0건 확인.
7. 시뮬 모드에서 팔로잉 진입 1사이클을 돌려 아래 로그가 모두 나오는지 확인:
   `AX-FOLLOW-MOVE ... followAcc=... scalePercent=...`,
   `AX-FOLLOW-MOVE ... 팔로잉 최초 이동 명령 ... acc=... dec=...`,
   `AX-FOLLOW-OVERRIDE ... acc=... dec=...`,
   `AX-FOLLOW-MOVE ... 최종 구간 ... overrideResult=...`

## 검증 기준 (핵심 — 이것이 이번 작업의 수용 기준)
`AppSettings`의 전체 속도 ScalePercent를 **100%가 아닌 값(예: 20%)** 으로 두고 팔로잉 진입을 실행한다.
1. `AxisMoveProfile ... PickerX ABS MOVE` 로그의 `acc=` / `dec=` 값이
   **축 Config 원본의 약 20%** 로 나와야 한다 (수정 전에는 100% 원본이 나갔다).
   `defaultScaleApplied=False`여도 무관하다 — 명시 가감속을 전달하므로 그 플래그와 무관하게
   스케일된 값이 나가는 것이 이번 수정의 목적이다.
2. `AX-FOLLOW-MOVE` / `AX-FOLLOW-OVERRIDE` 로그의 `acc=` / `dec=` / `scalePercent=`가 서로 정합해야 한다.
3. ScalePercent 100%에서는 수정 전과 동일한 값이 나와 회귀가 없어야 한다.

## 실장비 로그 요청 (사용자 조치 — 작업자는 대기)
이번 폭주 사고의 원인 확정을 위해 현장 PC의 `Log\Main_2026-07-25.log`가 필요하다.
로컬에는 `2026-07-19`자 로그만 있어 E-Stop 직전 구간을 확인할 수 없었다.
확인할 항목:
```bash
grep -n "AxisMoveProfile.*PickerX" Main_2026-07-25.log | tail -40
grep -n "AX-FOLLOW-MOVE" Main_2026-07-25.log | tail -30
```
`defaultScaleApplied=False`이면서 `acc=`/`dec=`가 `scalePercent=`와 무관한 큰 값이면 결함 1 확정이다.
**로그 확보 전에도 위 수정 A~E는 그대로 진행한다** — 결함 1/2는 로그 없이 코드만으로 확정된 사항이다.

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. `MoveAbsoluteAsync`의 `useDefaultMotionScale` 추론 방식 자체가 취약하다(전달 속도값으로
   가감속 스케일 여부를 역추론). 팔로잉 외에도 "스케일된 명시 속도를 넘기는데 그 값이 자기 축의
   기본 스케일값과 다른" 호출부가 있으면 같은 결함이 재현된다. 규칙 5-2의 감사 결과에
   해당 호출부 목록을 포함해 보고한다. **수정은 하지 않는다.**
2. `MoveAbsoluteForFollowAsync`는 이동 구간 동안 `Config.Acceleration/Deceleration`을 임시
   치환한다(기존 `MoveAxisWithTemporaryMotionAsync`와 동일 한계). 같은 축에 대해 동시 이동이
   없으므로 실사용 문제는 없으나, UI가 그 순간 Config를 표시하면 임시값이 보인다. 구조 개선
   (명시 가감속 오버로드 신설)은 별도 결정 사항이다.
