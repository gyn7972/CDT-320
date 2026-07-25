# CDT-320: 위치 오버라이드 기준(startBase) 스테일 제거 — 절대값이 상대값으로 나가는 결함 차단

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
**수정 파일은 `QMC.Common\AjinE\AXM.cs`를 기본으로 하고, 조사 결과에 따라
`QMC.CDT-320\Equipment\Ajin\AjinAxis.cs`가 추가될 수 있다(수정 C).**

**`QMC.Common\Ajin\AXM.cs`(빌드 제외 죽은 코드 — csproj에 `AjinE\*`만 포함)는 절대 수정 금지.**
인터락 파일, 시퀀스, SharedRailX, UI, 설정 데이터, `D:\CDT-320\*`는 건드리지 않는다.

---

# ★ 작업 규칙 (필수 — 위반 시 작업 중단하고 사용자에게 보고)

### 규칙 1. 공정 속도 구간에는 스케일 미적용 코드를 작성하지 않는다
Auto 운전에서 `Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`을 보드로
내려보내는 경로는 반드시 `MotionSpeedScale.ApplyDefaultVelocityScale` /
`ApplyDefaultAccelerationScale`을 통과한 값만 사용한다. Config 날값 직접 전달 금지.

### 규칙 2. 속도·가속·감속은 항상 같은 배율로 함께 스케일한다
전례: 커밋 `a690f6a5`, 2026-07-25 RearPickerX 폭주(팔로잉 최초 명령 가감속 20배).
**이번 작업은 속도/가감속 인자를 계산하거나 변경하지 않는다.** 오버라이드 기준 좌표(base)만 다룬다.

### 규칙 3. 범위 밖에서 스케일 미적용 지점을 발견하면 고치지 말고 보고한다

### ★ 규칙 4. 보고 의무 (반드시 이행 — 하나라도 빠지면 작업 미완료)
1. 변경 지점 목록 (`파일:행` + 변경 내용).
2. **[조사 1]** 결과 — InputVisionX가 base를 갱신하지 못하는 경로 추적. **이 작업의 핵심 산출물이다.**
3. 스케일 미적용 잔존 지점 감사 결과
   (`grep -rn "Config\.DefaultVelocity\|Config\.Acceleration\|Config\.Deceleration" --include=*.cs QMC.CDT-320 QMC.Common`).
   없으면 "없음" 명시.
4. **[확인 요청 1]** 답변.
5. 판단이 애매해서 손대지 않은 지점. 없으면 "없음" 명시.

---

# 배경 — 실장비 확정 (2026-07-25)

`D:\CDT-320\Log\Motion_2026-07-25.log`의 오버라이드 기록을 축별로 대조하면 **InputVisionX만 기준이
0으로 고정**되어 있다.

```
19:01:28  AXM OVERRIDE POSITION. axisNo=5,  position=422.461460, startBase=0.000000,   relative=422.461460
19:35:12  AXM OVERRIDE POSITION. axisNo=5,  position=682.735010, startBase=0.000000,   relative=682.735010
20:17:09  AXM OVERRIDE POSITION. axisNo=5,  position=682.735010, startBase=0.000000,   relative=682.735010
20:24:06  AXM OVERRIDE POSITION. axisNo=5,  position=674.615010, startBase=0.000000,   relative=674.615010
--- 정상 비교군 ---
18:17:14  AXM OVERRIDE POSITION. axisNo=9,  position=150.703000, startBase=700.300000, relative=-549.597000
20:25:14  AXM OVERRIDE POSITION. axisNo=21, position=648.916000, startBase=700.000000, relative=-51.084000
```
축 5 = InputVisionX, 축 9 = FrontPickerX, 축 21 = RearPickerX (`D:\CDT-320\Config\motion_axes.json`).

`startBase = 0`이면 `relative = position − 0 = position`, 즉 **절대값이 그대로 상대값으로 보드에
전달**된다. EtherCAT `AxmOverridePos`는 이를 "모션 시작 위치 + relative"로 해석하므로 축이 의도한
좌표를 크게 지나친다. 2026-07-25 13:53 RearPickerX 폭주(서보 알람 0x0001)가 정확히 이 형태였고,
InputVisionX는 선행이동 거리가 짧아 아직 사고로 드러나지 않았을 뿐이다.

## 왜 0이 기록되었는가

`AjinE\AXM.cs`의 현재 구현:
- `MovePosition`(`:2040~`)이 이동 명령 직전에
  `if (GetCommandPosition(axis, ref startCommand) == 0) MotionStartCommandByAxis[axis] = startCommand;`
  로 기록한다(`:2049~2052`).
- `ModifyPosition`(`:2221~`)은 `MotionStartCommandByAxis.TryGetValue`로 읽고, **기록이 없으면 `-2`로
  거부**한다(`:2226~2232`).

로그에 `startBase=0.000000`이 찍혔다는 것은 **기록이 없는 것이 아니라 값 `0`이 들어 있었다**는 뜻이다
(없으면 `-2` + `failedAt=NoMotionStartBase`가 찍힌다).

핵심 결함: **`MotionStartCommandByAxis`는 모션이 끝나도 지워지지 않는다.** 축별로 "마지막
`MovePosition` 시점의 Command"가 무한정 남는다. 원점 복귀 직후 Command가 0인 상태에서 어떤
`MovePosition`이 한 번 나가 0이 기록되면, 이후 **`MovePosition`을 거치지 않는 경로로 시작된 모션**의
오버라이드가 그 스테일한 0을 base로 쓴다.

피커축(9/21)이 정상인 이유는 매 이동이 `AjinAxis.MoveAbsoluteAsync` → `AXM.MovePosition`을 거쳐
base가 최신으로 갱신되기 때문이다.

## 결함이 더 위험한 이유 — 실패로 잡히지 않는다
19:01:31 로그에 `AX-MOVE-REDIRECT > InputVisionX 이동 중 위치 오버라이드로 목표가 변경되어 원래 목표
확인을 생략합니다.`가 찍혀 있다. 오버라이드가 **성공으로 처리되어 `-5` 최종 확인까지 우회**됐다.
잘못된 좌표로 이동해도 시퀀스가 이상을 감지하지 못한다.

---

# 조사 (수정 전 필수)

## [조사 1] — InputVisionX가 base를 갱신하지 못하는 경로 추적
아래를 코드로 추적해 **보고서에 호출 체인으로 명시**한다. 수정 방향이 이 결과에 달려 있다.

1. `InputVisionXPrePositionCoordinator.MoveAndVerifyAsync`(`:476~`)는
   `SharedRailXMotionRuntime.MoveAxisAsync(visionX, target, motion.Velocity)`(`:498`)로 명령한다.
   이 경로가 최종적으로 `AXM.MovePosition(int, double, double, double, double)`에 도달하는지 확인한다.
2. 도달하지 않는다면 어디서 갈라지는지 특정한다. 후보:
   - `AXM.MovePosition(int, double, double, TimeSpan, TimeSpan)` 오버로드(`:2048` 부근) — **base 기록이
     추가되지 않은 오버로드다.**
   - `AjinAxis.MoveAbsoluteCommandOnlyAsync`(`:1211` 부근)의 보드 명령 지점(`:1294` 부근)
   - `AXM`의 다른 이동 함수(`AxmMoveStartPos` 외)
3. `GetCommandPosition`(`AjinE\AXM.cs:1877`, `AxmStatusGetCmdPos`)이 InputVisionX에 대해 0을 반환하는
   시점이 있는지(원점 복귀 직후 등) 확인한다.
4. 결론을 **"경로 A는 기록됨 / 경로 B는 기록 안 됨"** 형태로 정리해 보고한다.

---

# 수정 내용

## 수정 A (필수) — 모션 종료 시 base 무효화 (스테일 제거)

기준이 남아 있어서 다음 모션이 잘못 쓰는 것이 근본 문제다. **모션이 끝나면 base를 지운다.**

### A-1. 무효화 API 신설 (`AjinE\AXM.cs`, `MotionStartCommandByAxis` 선언 아래)
```csharp
        /// <summary>
        /// 축의 오버라이드 기준(모션 시작 Command)을 무효화한다.
        /// 기존 조건: MovePosition이 기록한 기준이 모션 종료 후에도 남아, MovePosition을 거치지 않고
        ///           시작된 다음 모션의 오버라이드가 스테일한 기준(예: 원점 직후의 0)을 사용했다.
        ///           그 결과 relative = position − 0 = position 이 되어 절대값이 상대값으로 보드에
        ///           전달됐다(실장비 2026-07-25, InputVisionX startBase=0).
        /// 현재 기준: 모션 종료 시 기준을 무효화해, 기준 없는 오버라이드는 ModifyPosition이 -2로
        ///           거부하도록 한다(잘못된 기준으로 보드에 상대값을 보내지 않는다).
        /// </summary>
        public static void ClearMotionStartCommand(int axis)
        {
            double removed;
            MotionStartCommandByAxis.TryRemove(axis, out removed);
        }
```

### A-2. `MovePosition` 기록 실패 시 로그 추가
현재 `GetCommandPosition` 실패 시 조용히 `TryRemove`만 한다(`:2053~2057`). 실패를 알 수 있게
로그를 남긴다(`LogOverrideCall`을 재사용하지 말고 별도 1줄).
```csharp
            else
            {
                double removed;
                MotionStartCommandByAxis.TryRemove(axis, out removed);
                try
                {
                    QMC.Common.Log.Write("Motion", "SYSTEM", "AXM-OVERRIDE",
                        "AXM MOTION START BASE 기록 실패. axisNo=" + axis +
                        ", position=" + position.ToString("F6") +
                        " — 이 모션의 위치 오버라이드는 -2로 거부됩니다. - Check");
                }
                catch { }
            }
```
- `TimeSpan` 오버로드(`:2048` 부근)에도 **동일한 기록 로직이 있는지 확인**하고, 없으면
  **[확인 요청 1]로 보고**한다(호출부가 있는지 먼저 확인 — 없으면 추가하지 말 것).

## 수정 B (필수) — 기준 유효성 검증 강화 (`ModifyPosition`)

기록된 기준이 **현재 축 상태와 모순**이면 거부한다. 스테일 0을 잡아내는 2차 방어다.

`ModifyPosition`의 `TryGetValue` 성공 직후, `relative` 계산 **전**에 삽입:
```csharp
            // 기준 유효성 2차 검증: 기록된 기준이 현재 축 상태와 모순이면 거부한다.
            // 스테일 기준(예: 원점 직후의 0)이 남아 relative가 절대값처럼 나가는 것을 막는다.
            double currentCommand = 0.0;
            if (GetCommandPosition(axis, ref currentCommand) != 0)
            {
                LogOverrideCall("POSITION", axis, position, velocity, acceleration, deceleration,
                    -2, "CommandReadFailed");
                return -2;
            }

            // 오버라이드는 "구동 중"에만 유효하다. 구동 중이면 현재 Command는 시작 기준과
            // 목표 사이(또는 그 근방)에 있어야 한다. 기준이 현재 Command로부터
            // MotionStartBaseToleranceMm 이상 떨어진 방향 밖에 있으면 스테일로 판정한다.
            if (!IsMotionStartBasePlausible(startBase, currentCommand, position))
            {
                LogOverrideCall("POSITION", axis, position, velocity, acceleration, deceleration,
                    -2, "StaleMotionStartBase(startBase=" + startBase.ToString("F3") +
                        ", currentCommand=" + currentCommand.ToString("F3") + ")");
                return -2;
            }
```
그리고 판정 헬퍼를 신설한다:
```csharp
        /// <summary>오버라이드 기준 스테일 판정 여유(mm).</summary>
        private const double MotionStartBaseToleranceMm = 1.0;

        /// <summary>
        /// 기록된 모션 시작 기준이 현재 축 상태로 설명 가능한지 판정한다.
        /// 구동 중인 축의 현재 Command는 "시작 기준 ↔ 목표" 구간 안(여유 포함)에 있어야 한다.
        /// 구간을 벗어나면 다른 모션의 스테일 기준이 남은 것으로 보고 거부한다.
        /// </summary>
        private static bool IsMotionStartBasePlausible(double startBase, double currentCommand, double targetPosition)
        {
            double low = Math.Min(startBase, targetPosition) - MotionStartBaseToleranceMm;
            double high = Math.Max(startBase, targetPosition) + MotionStartBaseToleranceMm;
            return currentCommand >= low && currentCommand <= high;
        }
```
검증 예 (실장비 값):
- 정상(축 21): `startBase=700.0`, `target=648.916`, 구동 중 `currentCommand`≈660 → 구간 [647.9, 701.0] 안 → **통과**
- 결함(축 5): `startBase=0.0`, `target=682.735`, 구동 중 `currentCommand`≈150 → 구간 [-1.0, 683.7] 안 → **통과해버림** ⚠

> ⚠ **위 예시가 보여주듯 이 검증만으로는 축 5 케이스를 잡지 못한다.** `startBase=0`이 우연히 유효
> 구간을 만들기 때문이다. 그래서 **수정 A(스테일 제거)가 근본 대책이고 수정 B는 보조**다.
> 수정 B의 판정식이 실제로 어떤 케이스를 잡고 어떤 케이스를 놓치는지 보고서에 명시하라.
> 더 강한 판정이 필요하다고 판단되면 **직접 바꾸지 말고 [확인 요청 1]로 제안만** 한다.

## 수정 C (조사 결과에 따라 — 승인 후 진행)

[조사 1]에서 "InputVisionX 이동이 `AXM.MovePosition`을 거치지 않는다"로 결론이 나면, 그 경로에도
base 기록을 추가해야 한다. 다만 대상 파일이 `AjinAxis.cs` 또는 다른 곳일 수 있으므로
**[조사 1] 결과와 수정안을 먼저 보고하고 사용자 승인 후 진행한다. 승인 없이 수정하지 말 것.**

수정 A만 적용하면 그 경로의 오버라이드는 `-2`로 거부되고, `FollowMoveAsync` / 선행이동 코디네이터의
기존 폴백(일반 이동)이 동작한다 — **잘못된 좌표로 움직이는 것보다 안전하다.** 그래서 수정 A는
승인 대기 없이 진행한다.

---

## 변경 금지
- `QMC.Common\Ajin\AXM.cs` (죽은 코드) — diff 0건.
- `AxmOverridePos` / `AxmOverrideAccelVelDecel` / `AxmMoveStartPos` / `AxmStatusGetCmdPos`의
  `DllImport` 선언과 파라미터 — 절대 변경 금지.
- `ModifyPosition`의 절대좌표 모드 지정(`SetAbsRelMode(axis, true)`) 호출과 그 순서.
- `ModifyVelocity` 계열 전체(위치 기준과 무관).
- `AxmOverrideSetMaxVel` 관련 코드 신설 금지 (EtherCAT 미지원 — 사용자 확인 2026-07-25).
- `AjinAxis.TryOverridePosition`의 인터락 검증 순서·반환 코드(-11/-4/-2)·`CheckSoftLimitTarget` 위치.
- `AjinAxis`의 `_positionOverrideSerial` / `AX-MOVE-REDIRECT` 로직 — **[확인 요청 1] 참조.**
- 인터락 파일 전체, 시퀀스, SharedRailX, UI, 설정 데이터, `D:\CDT-320\*`.

## [확인 요청 1] — 함께 보고할 항목
1. 수정 B 판정식이 놓치는 케이스(위 축 5 예시 포함)와, 더 강한 판정을 위한 제안.
   예: "구동 중 `IsMoving`이 아니면 거부", "`startBase`와 `currentCommand` 차이가 이미 이동한
   거리와 부호가 맞는지 검사" 등. **제안만 하고 구현하지 말 것.**
2. `MovePosition`의 `TimeSpan` 오버로드에 base 기록이 없는데 호출부가 있는지.
3. `AX-MOVE-REDIRECT`가 `-5` 최종 확인을 우회하는 현재 동작 때문에 **잘못된 오버라이드가 실패로
   잡히지 않는다.** 오버라이드가 `-2`로 거부된 경우에는 리다이렉트 플래그가 서지 않아 `-5`가
   정상 발동하는지 코드로 확인해 보고한다.
4. [조사 1] 결론과 수정 C 제안.

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과.
2. `git diff --stat` = `QMC.Common/AjinE/AXM.cs` **한 파일만**(수정 C 미승인 상태).
   `QMC.Common/Ajin/AXM.cs`가 목록에 없어야 한다.
3. `ClearMotionStartCommand`가 신설되었고, **`AXM.cs` 안에서 호출되지 않는지** 확인
   (호출 지점 추가는 수정 C 승인 후 별도 진행).
4. `ModifyPosition`에서 `startBase` 사용 전에 2차 검증이 수행되고, 실패 시 `-2`와
   `failedAt=StaleMotionStartBase(...)` / `CommandReadFailed`가 로그에 남는지 확인.
5. 규칙 4의 보고 항목 5개.

## 검증 기준
**시뮬**
1. 빌드 후 팔로잉 픽업 1사이클 — `AXM-OVERRIDE ... startBase=`가 **0이 아닌 실제 시작 좌표**로 찍히고
   `relative = position − startBase` 관계가 성립한다.
2. 오버라이드가 `-2`로 거부되는 케이스를 인위적으로 만들어(예: base 미기록 경로 사용) 폴백
   일반 이동으로 정상 완료되는지, 그리고 축이 잘못된 좌표로 가지 않는지 확인한다.

**실장비 (사용자 실행 — 작업자는 문서로만 제공)**
3. Auto 1배치 후 `Motion_*.log`에서 **`axisNo=5`(InputVisionX)의 `startBase=0.000000`이 0건**인지 확인.
   여전히 0이면 [조사 1]/수정 C가 미해결이라는 뜻이므로 로그를 보존해 보고.
4. `failedAt=NoMotionStartBase` 또는 `StaleMotionStartBase`가 찍히면 그 시점 시퀀스가 폴백으로
   정상 완료되는지 확인.
5. 어떤 축이든 명령 반대 방향 이동이나 목표 초과가 관측되면 즉시 정지하고 로그 보존.

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. EtherCAT `AxmOverridePos`의 좌표 기준(절대/상대)은 벤더 문서로 확정되지 않았다. 현재 구현은
   실장비 3세션 실측(축이 정확히 `시작위치+relative`에 정지)에 근거한다. 벤더 확인이 필요하다.
2. 한 모션에 오버라이드를 2회 이상 발행할 때 보드가 기준을 리베이스하는지 미확정(이전 프롬프트의
   확인 요청). 팔로잉이 해당하므로 실장비 로그 대조가 필요하다.
3. `AX-MOVE-REDIRECT`로 `-5` 확인을 생략하는 설계는 잘못된 오버라이드를 은폐할 수 있다.
   최종 목표 도달 확인을 오버라이드 발행 측에 맡기는 현재 구조가 적절한지 재검토가 필요하다.
