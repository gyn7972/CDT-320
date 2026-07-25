# CDT-320: AjinAxis 폴링 루프의 Task.Delay(0)을 스레드풀 양보로 교체 (async 비동기 계약 복원)

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
**수정 파일은 `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` 단 하나이며, 그 안의 `Task.Delay(0)` 3곳과
헬퍼 메서드 1개 신설만 수행한다.** 다른 파일은 절대 건드리지 않는다.

## 배경 — 무엇이 깨졌는가

커밋 `c058435e "아진 딜레이 0으로 수정"`(2026-07-25)이 이 파일의 `await Task.Delay(10)` 3곳을
`await Task.Delay(0)`으로 바꿨다. `Task.Delay(0)`은 **이미 완료된 Task를 반환**하므로 `await`가
스레드를 양보하지 않는다. 그 결과 축 이동 async 메서드가 **호출자에게 제어를 돌려주지 않고
물리 이동이 끝날 때까지 동기 실행**된다.

이 머신의 .NET Framework에서 직접 측정한 결과:

| 방식 | 호출 직후 Task 완료? | SyncContext.Post | 폴링 1회 실제 소요 |
|---|---|---|---|
| `await Task.Delay(0).ConfigureAwait(false)` | **True (양보 안 함)** | 0회 | 0.0003ms |
| `await Task.Yield()` | False (양보함) | **매회 Post** | 0.0033ms |
| `await Task.Run(() => { }).ConfigureAwait(false)` | False (양보함) | 0회 | 0.0013ms |
| `await Task.Delay(1).ConfigureAwait(false)` | False (양보함) | 0회 | **15.97ms** |
| `await Task.Delay(10).ConfigureAwait(false)` | False (양보함) | 0회 | **16.00ms** |

`Delay(1)`과 `Delay(10)`이 같은 ~16ms인 것은 Windows 기본 타이머 해상도(15.6ms) 때문이다.
그래서 **딜레이로 되돌리는 것은 답이 아니다** — 이동 완료 감지가 매 이동마다 최대 16ms 늦어지고
택타임으로 누적된다. 딜레이 0으로 바꾼 원래 의도(빠른 완료 감지)는 유지해야 한다.

### 실제로 발생한 증상 2건

1. **비전 회피 ∥ 피커 진입 팔로잉 기능이 100% 무력화됐다.**
   `PickerPickUpSequence.cs:1188`에서 `_inputVisionRetreatMoveTask = RunInputVisionRetreatMoveAsync(...)`로
   비전 회피를 "비동기 시작"하지만, 위 이유로 그 줄에서 회피가 끝까지 동기 실행된다. 다음 스텝에
   도달하면 `stage.CameraX.IsMoving == false`이므로 `ShouldFollowInputVisionRetreatForPickerEntry()`
   (`PickerPickUpSequence.cs:1338`)가 false를 반환하고 팔로잉 대신 기존 일반 이동으로 빠진다.
   결과: 비전이 회피를 다 끝낸 뒤에 피커가 이동하는 완전 순차 동작. Output(Place) 쪽
   `ShouldFollowOutputVisionRetreatForPickerEntry()`(`PickerPlaceSequence.cs:2478`)도 동일.

2. **다축 "동시" 이동이 축 순차 이동으로 바뀌었다.**
   `PickerSequenceBase.cs:997~1036`은 foreach로 축별 `MovePickerAxisCommandAsync(...)` Task를 모아
   `Task.WhenAll`한다. 양보가 없으면 foreach 한 바퀴마다 그 축이 물리 이동까지 완주하므로 X/T
   동시 이동이 X 완료 후 T 이동이 된다. 같은 구조가 FastConti의
   `pickerXMoveTask ∥ pickerZPrePickTask`(`PickerPickUpSequence.cs:2859`, `:2514`),
   Conti의 `Task.WhenAll(needleStageMove, pickerMove)`(`:2341`),
   SharedRailX 그룹 이동(`SharedRailXMotionService.cs:950~984`)에도 적용된다.

즉 깨진 것은 특정 기능이 아니라 **"축 이동 async 메서드는 이동 중에 호출자에게 제어를 돌려준다"는
계약 자체**다. 이 작업은 그 계약을 복원한다.

## 수정 원칙 (임의로 바꾸지 말 것)

1. **딜레이(대기 시간)는 도입하지 않는다.** `Task.Delay(1)` / `Task.Delay(10)` / `SpinWait` /
   `Thread.Sleep` 전부 금지. 폴링은 지금처럼 최대 속도로 돌되 **양보만** 하게 만든다.
2. **`await Task.Yield()`를 쓰지 말 것.** 양보는 하지만 `YieldAwaitable`이 현재
   `SynchronizationContext`로 continuation을 Post한다(위 측정에서 매회 Post 확인). 이 코드는
   UI 스레드에서도 호출된다 — `Ui\Dialogs\MotionTestDialog.cs:157`의
   `btnMoveStart.Click += async (s, e) => await MoveSingleAsync(...)` → `:551`
   `await axis.MoveAbsoluteAsync(...).ConfigureAwait(true)` 경로가 WinForms
   SynchronizationContext 위에서 시작된다. 이 경우 폴링 continuation이 매회 UI 메시지 큐에
   Post되어 페인트/입력 메시지를 굶긴다.
3. **양보는 반드시 스레드풀로, `ConfigureAwait(false)`와 함께 한다.** 아래 헬퍼를 신설해 3곳이
   같은 방식을 쓰게 한다.
4. `YieldAwaitable`에는 `ConfigureAwait`가 없다 — `await Task.Yield().ConfigureAwait(false)`는
   **컴파일 에러**다. 혹시 시도하지 말 것.
5. 취소(CancellationToken) 관측을 잃지 않는다. 179행/3014행의 기존 `Task.Delay(0, token)`은 토큰을
   관측하지만 신설 헬퍼는 관측하지 않는다. **두 루프 모두 반복 선두에서 이미
   `ThrowIfCancellationRequested()`를 호출하므로**(179행 루프는 `:148`, 3014행 루프는 `:2982`)
   교체만으로 취소 동작이 유지된다. 추가 검사를 넣지 말고, 그 선두 검사를 지우지도 말 것.
6. 리턴 코드 체계, `detectedMotion` / `idlePolls` / `guard` 판정, `_motionStopSerial` 비교,
   타임아웃 판정 로직은 **일절 건드리지 않는다.** 이 작업은 await 한 줄씩만 바꾸는 작업이다.
7. `using System.Diagnostics;` 등 using 문을 추가하지 않는다. `System.Threading.Tasks`는 이미
   `:9`에 있다.

## 수정 내용

### A. 헬퍼 신설 (`AjinAxis` 클래스 내부, private static)

`WaitUntilMoveDone` 메서드 바로 위에 아래 헬퍼를 추가한다. 위치가 다른 곳이어도 무방하나
클래스 내부 private static이어야 한다.

```csharp
        // 폴링 루프 1회 양보. 대기 시간은 넣지 않는다(택타임 유지).
        // 기존 조건: await Task.Delay(0) — 완료된 Task라 await가 양보하지 않아 축 이동 async
        //           메서드 전체가 동기 실행되고, "비동기 시작 후 나중에 join" 패턴과 다축 동시
        //           이동(Task.WhenAll)이 모두 순차화됐다.
        // 현재 기준: Task.Run + ConfigureAwait(false)로 스레드풀에 양보해 호출자에게 즉시 제어를
        //           돌려준다. Task.Yield()는 현재 SynchronizationContext로 Post하므로 UI 스레드
        //           호출 경로(MotionTestDialog 등)에서 메시지 큐를 포화시켜 사용하지 않는다.
        //           실측: Delay(0) 0.0003ms/회(양보 없음), Yield() 0.0033ms/회(매회 Post),
        //                 Task.Run 0.0013ms/회(Post 없음).
        private static Task YieldPollAsync()
        {
            return Task.Run(() => { });
        }
```

### B. 교체 3곳

세 곳 모두 **한 줄 교체**다. 앞뒤 코드는 그대로 둔다.

#### B-1. `InitializeHardwareLimitSearchAsync` 폴링 (179행)

변경 전:
```csharp
                    await Task.Delay(0, cancellationToken).ConfigureAwait(false);
```
변경 후:
```csharp
                    await YieldPollAsync().ConfigureAwait(false);
```
(취소는 같은 루프 `:148`의 `cancellationToken.ThrowIfCancellationRequested()`가 계속 관측한다.
같은 메서드 `:166`의 `await Task.Delay(20, cancellationToken)`은 **정지 확인용 실제 대기이므로
건드리지 않는다.**)

#### B-2. `WaitMoveCompleteAsync` (MOVE JOIN) 폴링 (3014행)

변경 전:
```csharp
                    await Task.Delay(0, ct).ConfigureAwait(false);
```
변경 후:
```csharp
                    await YieldPollAsync().ConfigureAwait(false);
```
(취소는 같은 루프 `:2982`의 `ct.ThrowIfCancellationRequested()`가 계속 관측한다.)

#### B-3. `WaitUntilMoveDone` 폴링 (3082행)

변경 전:
```csharp
                await Task.Delay(0).ConfigureAwait(false);
```
변경 후:
```csharp
                await YieldPollAsync().ConfigureAwait(false);
```

## 다른 수정지시와의 관계 (중요)

같은 메서드를 건드리는 별도 프롬프트
`MoveWaitTimeout_수정지시_프롬프트_2026-07-25.md`(WaitUntilMoveDone의 타임아웃을 폴링 횟수 →
Stopwatch 60초로 전환)가 있다. **두 작업은 서로 독립이며 적용 순서와 무관하다** — 그 프롬프트도
`await Task.Delay(0).ConfigureAwait(false);` 줄을 그대로 유지하기 때문이다.
따라서 B-3은 `guard` 기반 코드든 `Stopwatch` 기반 코드든 **해당 한 줄만 찾아 교체**하면 된다.
그 프롬프트의 다른 변경 사항을 이 작업에서 함께 수행하지 말 것.

## 변경 금지 (절대 손대지 말 것)

- `AjinAxis.cs:166` `await Task.Delay(20, cancellationToken)` — 리미트 검출 후 정지 확인용 실제
  대기. 그대로 둔다.
- `AjinAxis.cs` `FollowMoveAsync`(`:501~`) 내부 로직 및 `FollowMovePollIntervalMs` 상수 — 그대로 둔다.
- `WaitUntilMoveDone` / `WaitMoveCompleteAsync` / 리미트 서치의 **타임아웃·유예·완료 판정 조건 전부**
  (`guard > 6000`, `guard > 20`, `idlePolls > 20`, `DateTime deadline` 비교, `detectedMotion` 래치).
  이 작업은 await 방식만 바꾼다.
- `QMC.Common\Motion\BaseAxis.cs` 전체 (`base.WaitMoveCompleteAsync` 시뮬 경로 포함).
- 시퀀스/유닛/인터락/SharedRailX 파일 전체. 증상은 그쪽에서 보이지만 원인과 수정은 이 파일이다.

## 작업 후 확인 사항

1. `QMC.CDT-320.sln` 빌드 통과.
2. `AjinAxis.cs`에 `Task.Delay(0`이 **0건**이어야 한다:
   ```bash
   grep -n "Task.Delay(0" QMC.CDT-320/Equipment/Ajin/AjinAxis.cs
   ```
3. `Task.Yield()`를 사용하지 않았는지 확인 (0건이어야 한다).
4. `await YieldPollAsync().ConfigureAwait(false);`가 정확히 3곳이어야 한다.
5. `Task.Delay(20, cancellationToken)`(166행)은 그대로 남아 있어야 한다.
6. 179행 루프의 `cancellationToken.ThrowIfCancellationRequested()`(148행)와 3014행 루프의
   `ct.ThrowIfCancellationRequested()`(2982행)가 삭제되지 않았는지 확인.
7. `git diff --stat`이 `AjinAxis.cs` 한 파일만 보여야 하고, 변경은 헬퍼 신설 + 3줄 교체뿐이어야 한다.

## 실장비 검증 방법 (참고 — 작업자가 실행할 필요는 없음)

1. **팔로잉 복원 확인**: Auto + Conti/FastConti 픽업 사이클 로그에서
   `"PickUp 피커X 팔로잉 진입을 시작합니다"`와 `AX-FOLLOW-MOVE ... "팔로잉 이동을 시작합니다"`가
   나타나야 한다(수정 전에는 두 로그가 아예 없었다).
   `"팔로잉 진입이 실패해 기존 일반 이동으로 재시도합니다"`가 나오면 별건(인터락 -11 또는
   타임아웃 -21)이므로 보고만 하고 이 작업 범위에서 대응하지 말 것.
2. **로그 순서 확인**: `"InputVisionX 최소 회피 이동을 비동기 시작했습니다"`가
   `AxisMoveProfile ... InputVisionX ABS MOVE - Start`보다 **먼저** 찍혀야 한다
   (수정 전에는 순서가 반대였다).
3. **다축 동시 이동 확인**: `PickerMoveCommand` / 그룹 이동 elapsed 로그에서 X와 T의 명령 발행
   시각이 겹쳐야 한다.
4. **UI 확인**: MotionTestDialog에서 단축 이동을 실행해 UI가 멈추거나 느려지지 않는지 확인.

## 알려진 잔여 사항 (이번 작업 범위 아님 — 수정하지 말고 보고만)

1. `WaitMoveCompleteAsync`의 `if (++idlePolls > 20) break;`(3006행)와 `WaitUntilMoveDone`의
   `guard > 20`(이동 시작 유예)은 폴링 **횟수** 기준이라, 양보 방식이 바뀌어도 시간 의미는
   복원되지 않는다(Delay(10) 시절 각각 ~320ms / ~200ms → 현재 수십 µs). `WaitUntilMoveDone`
   쪽은 별도 프롬프트(`MoveWaitTimeout_...`)가 다루고, `WaitMoveCompleteAsync` 쪽은 아직
   미결이다. 다만 `:3001~3004`에 "보드 Command가 이미 목표면 완료로 보고 break" 완화 분기가
   있어 실사용 영향은 제한적이다.
2. 비전 회피를 비동기 시작하는 지점(`PickerPickUpSequence.cs:1188`)과 피커 X가 실제 출발하는
   지점(`:2859` / `:2506` / `:2307`) 사이에 완료를 기다리는 선행 모션(StageT 보정 `:2212`,
   PickerT/Y 선행 보정 `:2829` / `:2432`, PickerZ 안전 확인 `:2844`)이 있다. 최소 회피 거리가
   짧으면 이 작업으로 비동기가 복원된 뒤에도 그 사이에 회피가 끝나 팔로잉 게이트가 다시
   탈락할 수 있다. 순서 조정은 별도 결정 사항이다.
3. 이 루프들은 대기 없이 폴링하므로 축이 걸려 타임아웃까지 가는 동안 스레드풀 스레드 하나를
   계속 점유한다. 사용자가 딜레이 미도입을 명시했으므로 이번에는 손대지 않는다.
