# CDT-320: AjinAxis.WaitUntilMoveDone 이동 대기 타임아웃을 폴링 횟수 → 경과 시간(60초) 기준으로 전환

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
**수정 파일은 `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` 단 하나이며, 그 안의 `WaitUntilMoveDone()` 메서드 1개만 고친다.**
다른 파일, 다른 메서드는 절대 건드리지 않는다.

## 배경 — 실장비에서 발생한 현상

실장비 자동 운전 중 아래 알람이 발생했다.

```
GoodBinY 이동 실패. result=-3, alarm=False,
마지막 이동 실패 원인=OutputGoodStageY ABS MOVE failed. result=-3,
reason=Move wait failed., target=360.564 mm, servo=ON, alarm=OFF, pos=351.324 mm
```

servo=ON, alarm=OFF, 목표까지 9.24mm 남은 상태였다. 즉 **축은 정상적으로 이동 중이었는데 이동 완료 대기 로직이 먼저 포기한 것**이며, 하드웨어 이상이 아니다.

### 로그 발생 경로 (참고용, 수정 대상 아님)
1. 겉 메시지 — `QMC.CDT-320\Equipment\Unit\OutputStageUnit.cs` 975~981행 `MoveStageAxis()` → `ReportOutputStageMoveFailure("OS-MOVE", ...)` → `RaiseOutputStageAlarm()`
2. 속 메시지 — `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` 1015~1016행 `FailMotion(waitRet, "ABS MOVE", "Move wait failed.", ...)`, 문장 조립은 `QMC.Common\Motion\BaseAxis.cs` 497~510행 `BuildMotionFailureMessage()`
3. **`-3`의 실제 출처(= 이번 수정 대상)** — `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` 3053~3090행 `WaitUntilMoveDone()`

### 근본 원인
커밋 `c058435e "아진 딜레이 0으로 수정"` 이 이 파일 3곳의 `await Task.Delay(10)` 을 `await Task.Delay(0)` 으로 바꿨는데,
`WaitUntilMoveDone()` 의 판정은 **경과 시간이 아니라 폴링 횟수(`guard`)** 기준이라 함께 조정되지 않았다.

`Task.Delay(0)` 은 이미 완료된 Task를 반환하므로 `await` 가 스레드를 양보조차 하지 않는다. 루프는 완전한 동기 스핀이 되고,
한 바퀴에 보드 호출이 7회 발생한다(`UpdateStatus()` 내부의 `GetMotionInfo` / `GetAmpEnabled` / `GetHomeResult` /
`GetActualPosition` / `GetInMotion` / `GetInPositionValue` + 루프 자체의 `GetInMotion`).
결과적으로 6000회를 소진하는 실제 시간은 **약 2~4초**에 불과하다.

`guard` 는 두 가지 판정에 동시에 쓰이고 있고, **둘 다 무너졌다.**

| 판정 | 코드 | Delay(10) 시절 의도 | Delay(0) 현재 실효 |
|---|---|---|---|
| 이동 대기 타임아웃 | `++guard > 6000` | 60초 | 약 2~4초 |
| 이동 시작 감지 유예 | `!detectedMotion && guard > 20 && !inMotion` | 200ms | 약 7ms |

시작 유예가 7ms로 줄어든 쪽은 별개 증상을 만든다. 보드가 아직 모션을 시작하지 않아 `inMotion=false` 인 사이에 루프를
조기 이탈하면 이동을 "완료"로 보고 나가고, 직후 `AjinAxis.cs` 1020행의 Command↔Target 비교에서 걸려
`result=-5` "이동 완료 후 Command 위치가 목표와 다릅니다" 로 실패한다. 뿌리가 같으므로 **두 판정을 함께 시간 기준으로 바꾼다.**

## 수정 원칙 (사용자 확정 사항 — 임의로 바꾸지 말 것)

1. **타임아웃은 경과 시간으로 정확히 측정하고, 값은 60초(60000ms)로 한다.**
2. **`await Task.Delay(0)` 은 그대로 유지한다.** 폴링 주기를 1ms 이상으로 늘리거나 `SpinWait` 를 넣지 않는다.
3. **타임아웃 시 축 정지(`Stop()` / `AXM.MoveStop`) 명령을 추가하지 않는다.** 현행 동작(정지 없이 `-3` 리턴)을 유지한다.
4. 시간 측정은 `System.Diagnostics.Stopwatch` 를 사용한다. `DateTime.UtcNow` 는 Windows 실해상도가 약 15.6ms라
   200ms 유예 판정의 눈금이 13칸밖에 되지 않아 "정확히 측정"이라는 요구에 맞지 않는다.
5. `AjinAxis.cs` 상단에 `using System.Diagnostics;` 가 **없다.** using 문을 추가하지 말고
   `System.Diagnostics.Stopwatch` 로 정규화된 이름을 그대로 쓴다.
6. 리턴 코드 체계(`0` 정상, `-3` 타임아웃, `-4` 정지 요청, `IsAlarm` 시 `AlarmCode`)와
   `detectedMotion` 래치 로직, `_motionStopSerial` 비교 로직은 의미를 바꾸지 않는다.
7. 시작 유예는 Delay(10) 시절과 동일한 **200ms** 로 복원한다.

## 수정 내용

대상: `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs`, `private async Task<int> WaitUntilMoveDone(int motionStopSerial)`
(파일 끝부분, 3053행 부근. 이 이름의 메서드는 파일에 하나뿐이다.)

### 변경 전 (현재 코드 — 이것을 찾아라)

```csharp
        private async Task<int> WaitUntilMoveDone(int motionStopSerial)
        {
            int guard = 0;
            bool detectedMotion = false;
            while (!IsAlarm)
            {
                UpdateStatus();

                bool inMotion = false;
                AXM.GetInMotion(AxisNo, ref inMotion);
                if (inMotion)
                    detectedMotion = true;

                if (detectedMotion && !inMotion)
                    break;

                if (!detectedMotion && guard > 20 && !inMotion)
                    break;

                if (Volatile.Read(ref _motionStopSerial) != motionStopSerial && !inMotion)
                    return -4;

                await Task.Delay(0).ConfigureAwait(false);
                if (++guard > 6000)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-MOVE-WAIT",
                        Name,
                        "Move wait timeout. AxisNo=" + AxisNo);
                    UpdateStatus();
                    return -3;
                }
            }

            UpdateStatus();
            return IsAlarm ? (int)AlarmCode : 0;
        }
```

### 변경 후 (이 코드로 교체하라)

```csharp
        private async Task<int> WaitUntilMoveDone(int motionStopSerial)
        {
            // 기존 조건: 폴링 횟수로 타임아웃(guard>6000)과 이동 시작 유예(guard>20)를 판정했다 —
            //           Delay(10) 전제라 각각 60초 / 200ms 였다.
            // 현재 기준: Delay(0)에서는 폴링 횟수가 경과 시간과 무관하므로(보드 폴링 속도에 좌우)
            //           Stopwatch로 경과 시간을 직접 측정해 판정한다. 타임아웃 60초, 시작 유예 200ms.
            const int MoveWaitTimeoutMs = 60000;
            const int MotionStartGraceMs = 200;

            System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
            bool detectedMotion = false;
            while (!IsAlarm)
            {
                UpdateStatus();

                bool inMotion = false;
                AXM.GetInMotion(AxisNo, ref inMotion);
                if (inMotion)
                    detectedMotion = true;

                if (detectedMotion && !inMotion)
                    break;

                if (!detectedMotion && !inMotion && elapsed.ElapsedMilliseconds > MotionStartGraceMs)
                    break;

                if (Volatile.Read(ref _motionStopSerial) != motionStopSerial && !inMotion)
                    return -4;

                await Task.Delay(0).ConfigureAwait(false);

                if (elapsed.ElapsedMilliseconds > MoveWaitTimeoutMs)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "AX-MOVE-WAIT",
                        Name,
                        "Move wait timeout. AxisNo=" + AxisNo +
                        ", elapsedMs=" + elapsed.ElapsedMilliseconds +
                        ", timeoutMs=" + MoveWaitTimeoutMs);
                    UpdateStatus();
                    return -3;
                }
            }

            UpdateStatus();
            return IsAlarm ? (int)AlarmCode : 0;
        }
```

### 변경 요약
- `int guard` 제거 (두 용도 모두 시간 기준으로 대체되어 참조하는 곳이 없어짐)
- `System.Diagnostics.Stopwatch elapsed` 추가
- `guard > 20` → `elapsed.ElapsedMilliseconds > MotionStartGraceMs` (200ms)
- `++guard > 6000` → `elapsed.ElapsedMilliseconds > MoveWaitTimeoutMs` (60000ms)
- `AX-MOVE-WAIT` 알람 메시지에 `elapsedMs`, `timeoutMs` 추가 (재발 시 실제 경과 시간 확인용)
- `await Task.Delay(0).ConfigureAwait(false);` 는 그대로

## 변경 금지 (절대 손대지 말 것)

- `AjinAxis.cs` 179행 `Task.Delay(0, cancellationToken)` — 리미트 서치 루프. 이미 `DateTime` deadline 기준(145~146행)이라
  조기 타임아웃이 없다. 그대로 둔다.
- `AjinAxis.cs` 3014행 `Task.Delay(0, ct)` — `WaitMoveCompleteAsync` (MOVE JOIN). 이미 `DateTime` deadline
  기준(2976행, 3010행)이라 조기 타임아웃이 없다. 그대로 둔다.
- `AjinAxis.cs` 1008~1027행 `MoveAbsoluteAsync` 의 실패 판정부 및 1020행 Command↔Target 톨러런스 확인 — 그대로 둔다.
- `QMC.Common\Motion\BaseAxis.cs` 의 `WaitMoveCompleteAsync`, `FailMotion`, `BuildMotionFailureMessage` — 그대로 둔다.
- `QMC.CDT-320\Equipment\Unit\OutputStageUnit.cs` 전체 — 그대로 둔다. 이번 알람의 표시 경로일 뿐 원인이 아니다.
- 인터락/시퀀스 로직, `MotionSpeedScale`, `SafeMovePercent`, 레시피, UI — 무관하므로 건드리지 않는다.

## 작업 후 확인 사항

1. `QMC.CDT-320.sln` 빌드가 통과해야 한다. 특히 `guard` 를 제거했으므로 미사용 변수/미정의 참조가 남지 않았는지 확인한다.
2. `WaitUntilMoveDone` 안에서 `guard` 라는 식별자가 더 이상 나오지 않아야 한다.
3. `using System.Diagnostics;` 가 파일 상단에 추가되지 않았는지 확인한다(정규화된 이름만 사용).
4. `await Task.Delay(0).ConfigureAwait(false);` 가 그대로 남아 있어야 한다(딜레이 유지 지시).
5. 타임아웃 경로에 축 정지 명령이 추가되지 않았어야 한다(현행 유지 지시).
6. 변경 라인 수는 `WaitUntilMoveDone` 메서드 내부로 한정되어야 한다. `git diff --stat` 이 `AjinAxis.cs` 한 파일만 보여야 한다.

## 실장비 검증 방법 (참고 — 작업자가 실행할 필요는 없음)

- 재발 시 `AX-MOVE-WAIT` 알람의 `elapsedMs` 가 60000 이상인지 확인한다. 60000 근처면 진짜 축 문제이고,
  그 이하로는 더 이상 나오지 않아야 한다.
- 같은 시각의 `AxisMoveProfile` 로그(`vel` / `acc` / `dec` / `scalePercent`)로 이동 속도 스케일이
  비정상적으로 낮지 않은지 교차 확인한다.

## 알려진 잔여 사항 (이번 작업 범위 아님, 보고만)

`Task.Delay(0)` 유지 지시에 따라 이 대기 루프는 최대 60초까지 동기 스핀으로 코어 하나를 점유한다.
정상 이동은 수백 ms 내에 끝나므로 실사용에는 문제가 없으나, 실제로 축이 걸려 60초를 소진하는 상황에서는
그 시간 동안 CPU 부하가 걸리고 동시에 대기 중인 다른 축의 폴링이 느려질 수 있다.
사용자가 딜레이 유지를 명시적으로 지시했으므로 이번에는 손대지 않는다.
