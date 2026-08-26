# 자동화 장비 프로그램 개발 · 리팩토링 가이드

> 적용 대상: C# WinForms 기반 자동화/검사/반도체 장비 프로그램
> 목적: 기존 장비 동작을 최대한 보존하면서 코드의 안정성, 유지보수성, 가독성, 복구성을 개선한다.
> 사용 방법: 이 파일을 프로젝트 루트에 두고 Codex/ChatGPT 등 AI 코딩 도구에 먼저 읽힌 후 작업을 지시한다.

---

# 1. 최우선 원칙

이 프로젝트는 일반 업무 프로그램이 아니라 **실제 장비를 움직이는 자동화 프로그램**이다.

따라서 리팩토링 우선순위는 다음과 같다.

1. 장비 안전
2. 기존 실기 동작 보존
3. 인터록 보존
4. 알람/복구 가능성
5. 공정 흐름 가독성
6. 유지보수성
7. 코드 재사용
8. 최신 문법/디자인 패턴 적용

**예쁜 코드보다 안전하게 동작하는 코드가 우선이다.**

기존에 실제 장비에서 검증된 코드라면 단순히 구조가 마음에 들지 않는다는 이유로 대규모 재작성하지 않는다.

---

# 2. 규칙 등급

## MUST
반드시 지켜야 한다.
위반 시 장비 충돌, 데이터 손실, 복구 불가, 유지보수 위험이 발생할 수 있다.

## SHOULD
특별한 이유가 없다면 적용한다.

## MAY
프로젝트 복잡도와 필요성에 따라 선택적으로 적용한다.

---

# 3. 개발 환경 기본 규칙

## MUST

- 프로젝트가 C# 7.3을 사용한다면 C# 7.3에서 컴파일 가능한 문법만 사용한다.
- WinForms 프로젝트는 기존 구조를 최대한 유지한다.
- 실기에서 검증된 외부 장비 SDK 호출 구조는 필요 이상 변경하지 않는다.
- 기존 Axis 번호, IO 번호, 장비 이름, Teaching 이름 등의 의미를 임의로 변경하지 않는다.
- 한글이 포함된 파일은 UTF-8로 저장한다.
- 기존 파일을 수정할 때 현재 프로젝트의 인코딩을 확인한다.

## SHOULD

- 한 파일에서 너무 많은 책임을 가지고 있으면 기능 단위로 점진적으로 분리한다.
- 단, 분리 자체가 목적이 되어 클래스 수를 불필요하게 늘리지 않는다.

---

# 4. 리팩토링 기본 철학

리팩토링의 목적은 다음 질문에 YES라고 답할 수 있도록 만드는 것이다.

- 지금 장비가 어느 Step에서 동작 중인지 바로 알 수 있는가?
- 다음에 어떤 동작을 하는지 코드만 보고 알 수 있는가?
- 왜 대기하는지 알 수 있는가?
- 왜 알람이 발생했는지 로그만 보고 알 수 있는가?
- 알람 해제 후 어디서 다시 시작하는지 알 수 있는가?
- 어떤 축과 어떤 축이 동시에 움직이는지 알 수 있는가?
- 인터록이 어디서 검사되는지 알 수 있는가?
- Recipe 값이 어디에서 사용되는지 추적 가능한가?
- Material 상태가 어디에서 변경되는지 알 수 있는가?
- 장비가 멈췄을 때 최종 위치와 상태를 추적 가능한가?

YES가 아니라면 리팩토링 후보이다.

---

# 5. 절대 피해야 할 리팩토링

## MUST NOT

다음 작업은 명확한 이유와 검증 없이 수행하지 않는다.

- 전체 Sequence Framework 재작성
- 전체 Motion Layer 재작성
- 전체 Material 구조 재작성
- 검증된 인터록 제거
- 기존 Step 구조를 한 번에 전부 변경
- 수십 개 파일을 한 PR/작업에서 동시에 구조 변경
- 단순화를 이유로 안전 검사 삭제
- 동작 확인 없이 async 구조를 sync 구조로 변경
- 동작 확인 없이 병렬 동작을 순차 동작으로 변경
- 동작 확인 없이 순차 동작을 병렬 동작으로 변경
- 상태 복구 정보를 제거
- Alarm을 단순 Log로 변경
- 예외를 숨기기 위해 catch만 추가

---

# 6. 함수 작성 규칙

기존 프로젝트에서 모든 함수에 무조건 `try / catch / finally`를 넣는 방식은 사용하지 않는다.

## MUST

`try / catch`는 **실제로 처리할 수 있는 예외가 있을 때만 사용한다.**

좋은 구조:

```csharp
public async Task<int> MoveReadyAsync()
{
    try
    {
        int result = await MoveAxisAsync();
        if (result != 0)
            return result;

        return await WaitAxisAsync();
    }
    catch (Exception ex)
    {
        LogError("MoveReadyAsync", ex);
        return -1;
    }
}
```

불필요한 구조:

```csharp
try
{
    ...
}
catch (Exception ex)
{
    throw;
}
finally
{
}
```

빈 `finally { }`는 작성하지 않는다.

단순 Getter, 상태 확인 함수, 계산 함수 등에 의미 없는 try/catch를 반복해서 넣지 않는다.

---

# 7. 예외 처리 규칙

## MUST

예외를 잡았으면 다음 중 하나를 수행해야 한다.

- 복구
- 안전 상태 정리
- 로그 기록
- Alarm 발생
- 의미 있는 결과 코드 반환
- 상위 계층으로 다시 throw

다음 코드는 금지한다.

```csharp
catch
{
}
```

또는

```csharp
catch
{
    return false;
}
```

중요한 설정/Recipe/Material/장비 상태를 읽다가 실패했는데 조용히 기본값으로 넘어가면 실제 장비에서 위험할 수 있다.

---

# 8. Sequence 구조 규칙

## MUST

Sequence는 가능한 한 **Step 흐름이 눈에 보여야 한다.**

권장 형태:

```csharp
switch (CurrentStep)
{
    case Step.Check:
        return Check();

    case Step.MoveReady:
        return await MoveReadyAsync();

    case Step.WaitReady:
        return await WaitReadyAsync();

    case Step.Process:
        return await ProcessAsync();

    case Step.Complete:
        return 0;
}
```

각 Step에서는 가능하면 하나의 의미 있는 장비 동작만 수행한다.

예:

- 조건 확인
- 이동 명령
- 이동 완료 확인
- Vacuum On
- Vacuum 확인
- Vision 요청
- 결과 확인
- Material 변경

한 Step에서 지나치게 많은 일을 하지 않는다.

---

# 9. Sequence Step 변경 규칙

## MUST

Step 변경은 성공이 확인된 후 수행한다.

```csharp
int result = await MoveReadyAsync();

if (result != 0)
    return result;

CurrentStep = Step.WaitReady;
return 0;
```

실패했는데 다음 Step으로 넘어가면 안 된다.

---

# 10. 모션 안전 규칙

이 항목은 최우선 MUST 규칙이다.

## MUST

축 이동은 아래 3단계를 명확히 구분한다.

1. 이동 가능 조건 검사
2. 이동 명령
3. 이동 완료 확인

예:

```csharp
if (!CanMovePickerX(target, out reason))
    return Alarm(...);

int result = await MovePickerXCommandAsync(target);

if (result != 0)
    return Alarm(...);

result = await WaitPickerXInPositionAsync(target);

if (result != 0)
    return Alarm(...);
```

이동 명령이 성공했다고 실제 이동 완료로 판단하지 않는다.

---

# 11. 병렬 모션 규칙

## MUST

동시에 움직여도 안전한 축인지 먼저 확인한다.

병렬 이동 가능:

```csharp
Task<int> moveX = MoveXCommandAsync(x);
Task<int> moveY = MoveYCommandAsync(y);

int[] commands = await Task.WhenAll(moveX, moveY);

if (commands.Any(v => v != 0))
    return -1;

Task<int> waitX = WaitXAsync(x);
Task<int> waitY = WaitYAsync(y);

int[] waits = await Task.WhenAll(waitX, waitY);

if (waits.Any(v => v != 0))
    return -1;
```

반드시 다음을 구분한다.

- Command 동시 실행
- Command 결과 확인
- Wait 동시 실행
- Wait 결과 확인

간섭 위험이 있는 축은 병렬 처리하지 않는다.

---

# 12. Polling / Delay 규칙

## MUST

UI Thread에서 아래 코드는 사용하지 않는다.

```csharp
while (...)
{
}

Thread.Sleep(...);
```

## SHOULD

장비 상태 Polling이 필요하면 다음 형태를 사용한다.

```csharp
while (!condition)
{
    ct.ThrowIfCancellationRequested();

    if (timeout)
        return -1;

    await Task.Delay(10, ct);
}
```

1ms Polling은 실제 필요성이 명확한 경우에만 사용한다.

불필요한 초고속 Polling은 CPU 사용량과 Thread Scheduling 문제를 만들 수 있다.

---

# 13. CancellationToken 규칙

## MUST

장시간 실행되는 Sequence / Wait / Motion 함수는 가능한 한 CancellationToken을 전달한다.

다음 경로가 끊기지 않아야 한다.

```text
Machine Controller
    ↓
Unit Sequence
    ↓
Process Sequence
    ↓
Motion / Wait
```

Stop 명령이 들어왔는데 하위 Wait가 CancellationToken을 받지 않아 계속 대기하는 구조를 만들지 않는다.

---

# 14. Cycle Stop / Stop / Alarm 구분

세 가지를 동일하게 처리하지 않는다.

## Cycle Stop

현재 안전한 작업 경계까지 진행한 후 정지한다.

- 현재 Step 또는 재개 위치 보존
- 재시작 시 해당 위치부터 계속 가능

## Stop

현재 자동 운전을 종료한다.

- 필요한 경우 진행 중 Motion 정지
- 예약/임시 상태 정리
- 다음 시작은 초기 조건에서 시작 가능

## Alarm

장비 이상 상태이다.

- 원인 기록
- 위험 동작 금지
- Alarm Reset 후 현재 상태 재검사
- 가능하면 실패 Step 재개

---

# 15. Sequence 복구 규칙

## MUST

알람 발생 시 최소한 다음 정보를 알 수 있어야 한다.

- Unit
- Sequence
- Step
- 동작
- 대상 Axis / IO / Material
- 실패 원인
- 실패 코드

예:

```text
[FrontPicker]
Sequence: PickUp
Step: MovePickerZPick
Axis: FRONT_PICKER_Z1
Target: -12.350
Result: -3
Reason: InPosition Timeout
```

---

# 16. Interlock 규칙

## MUST

Interlock은 Motion 실행 직전에 최종적으로 한 번 더 검사한다.

UI에서 버튼 Enable을 막았다고 해서 실제 Motion 함수의 Interlock 검사를 제거하지 않는다.

UI는 1차 방어일 뿐이다.

실제 Motion Layer / Unit Layer가 최종 방어선이다.

---

# 17. Interlock 설계 기준

Interlock 이름만 보고 무엇을 막는지 알 수 있어야 한다.

좋은 예:

```text
PickerFrontToInputStageCollision
PickerRearToOutputStageCollision
InputVisionXAxisPickerClearance
SharedRailXOwnership
```

나쁜 예:

```text
Interlock1
Check2
ConditionA
```

---

# 18. Material 관리 규칙

## MUST

Material의 실제 상태는 하나의 공식 데이터 소스에서 관리한다.

Grid, Label, UI Control 값이 Material 상태의 기준이 되면 안 된다.

권장:

```text
MaterialStateService
       ↓
MaterialStorage
       ↓
UI View
```

UI가 Material을 직접 임의 수정하지 않는다.

---

# 19. Material 상태 변경 규칙

Material 이동은 가능하면 명시적인 함수로 표현한다.

예:

```csharp
MoveDieInputStageToPicker(...)
MoveDiePickerToGoodBin(...)
MoveDiePickerToNgBin(...)
ReleaseReservation(...)
```

여러 곳에서 List/Dictionary를 직접 수정하지 않는다.

---

# 20. Material Reservation 규칙

병렬 Sequence 환경에서 동일 Material을 두 Unit이 동시에 가져가지 않도록 Reservation 개념을 사용한다.

Reservation 생성 시 기록:

- Material ID
- Owner
- Source
- Target
- Time

Release가 반드시 존재해야 한다.

Abort / Alarm / Stop에서도 예약이 정리되는지 확인한다.

---

# 21. Recipe 규칙

## MUST

Recipe는 공정 조건이다.

Recipe 값과 Runtime 상태를 섞지 않는다.

Recipe 예:

- Teaching Position
- Speed
- Delay
- Inspection Score
- Thickness
- Offset
- 사용 여부
- 공정 옵션

Runtime 예:

- 현재 Step
- 현재 Axis Position
- 현재 Material 위치
- Pick 완료 여부
- Alarm 상태

---

# 22. Recipe Validation

Recipe Load 이후 반드시 Validation 단계가 있어야 한다.

검사 예:

```text
속도 > 0
Pitch > 0
Z 위치가 SoftLimit 범위 내부
Score 범위 0 ~ 1
Delay >= 0
필수 Teaching Position 존재
사용하는 Axis가 실제 구성에 존재
```

잘못된 Recipe를 그대로 자동 운전에서 사용하지 않는다.

---

# 23. 데이터 저장 안전 규칙

중요 JSON/Recipe/Runtime 파일은 직접 덮어쓰지 않는 것을 권장한다.

권장 방식:

```text
원본 파일
   ↓
.tmp 저장
   ↓
저장 성공 확인
   ↓
기존 파일 → .bak
   ↓
.tmp → 원본
```

중간에 프로그램이 죽어도 최소한 `.bak` 또는 기존 파일이 남아 있어야 한다.

---

# 24. 데이터 저장 실패 처리

다음과 같은 저장 코드는 중요한 데이터에 사용하지 않는다.

```csharp
try
{
    Save();
}
catch
{
}
```

Recipe 저장 실패, Runtime 저장 실패, Material 저장 실패는 반드시 로그에 남긴다.

운영에 중요한 데이터라면 사용자에게 알려야 한다.

---

# 25. Logging 규칙

## MUST

로그에는 다음이 포함되어야 한다.

- 누가
- 어디서
- 무엇을
- 어떤 대상으로
- 어떤 결과로 수행했는지

예:

```text
[FrontPicker][PickUp]
MovePickerZ
PickerNo=2
Target=-12.350
Result=OK
Elapsed=184ms
```

실패:

```text
[FrontPicker][PickUp]
MovePickerZ Failed
PickerNo=2
Target=-12.350
Reason=InPosition Timeout
Elapsed=3000ms
```

---

# 26. Alarm 규칙

Alarm 메시지는 개발자만 이해할 수 있는 메시지가 아니라 **설비 작업자가 원인을 파악할 수 있는 문장**이어야 한다.

나쁜 예:

```text
Error -3
```

좋은 예:

```text
Front Picker Z2가 Pick 위치에 도착하지 않았습니다.
Servo 상태와 간섭 여부를 확인하십시오.
```

내부 로그에는 추가로 Axis, Target, Actual, ResultCode 등을 기록한다.

---

# 27. UI 규칙

WinForms UI는 View 역할을 우선한다.

UI 이벤트에 긴 장비 동작을 넣지 않는다.

나쁜 구조:

```csharp
private async void btnStart_Click(...)
{
    // 200줄짜리 장비 시퀀스
}
```

좋은 구조:

```csharp
private async void btnStart_Click(...)
{
    await StartAutoAsync();
}
```

실제 장비 동작은 Controller / Unit / Sequence에 둔다.

---

# 28. Designer 규칙

## MUST

`.Designer.cs`에는 다음만 둔다.

- Control 생성
- 속성
- Layout
- Event 연결

다음을 넣지 않는다.

- Motion
- IO
- Recipe Load/Save
- Material 변경
- Sequence
- 계산 로직

---

# 29. 클래스 크기 기준

파일 Line 수만으로 무조건 분리하지 않는다.

다만 다음 조건이면 리팩토링 후보로 본다.

- 클래스가 여러 장비 Unit을 직접 제어
- UI + Motion + Recipe + Material + Logging을 모두 담당
- 특정 기능을 수정할 때 파일 전체를 이해해야 함
- private 함수가 수십 개 이상이며 기능 영역이 서로 다름
- 2~3개의 독립된 책임이 명확하게 존재

---

# 30. 과도한 추상화 금지

다음 이유만으로 Interface / Factory / Manager / Coordinator를 만들지 않는다.

```text
"나중에 쓸 수도 있으니까"
"디자인 패턴이니까"
"테스트하기 좋아 보이니까"
```

장비 프로그램에서는 호출 구조가 너무 깊어지면 현장 디버깅이 어려워진다.

권장 호출 깊이:

```text
UI
 → Controller
 → Unit/Sequence
 → Motion/IO
```

필요한 경우 일부 Service를 추가할 수 있다.

하지만:

```text
Controller
 → Coordinator
 → Manager
 → Provider
 → Factory
 → Adapter
 → Handler
 → Service
```

형태로 과도하게 깊어지지 않도록 한다.

---

# 31. Coordinator 사용 기준

Coordinator는 다음 경우에만 사용하는 것이 좋다.

- 여러 Unit 병렬 실행
- 공유 자원 조정
- 실행 시작/종료 관리
- Cycle Stop 전체 전달

실제 공정 상세 로직까지 Coordinator에 넣지 않는다.

---

# 32. Resource Lock 규칙

Picker, Stage, Vision 등의 공유 영역에 동시 접근 위험이 있으면 Resource Lock을 사용할 수 있다.

다만 Lock은 반드시 다음 정보를 제공해야 한다.

- Resource Name
- Current Holder
- Waiting Owner
- Timeout
- Release

무한 Lock 대기는 금지한다.

---

# 33. 상태 Signal 규칙

문자열 Signal을 사용할 경우 오타가 런타임에서만 발견되는 문제가 있다.

예:

```csharp
Bus.IsSet("InputStageReady");
```

## SHOULD

가능하면 상수 또는 enum 성격의 정의를 사용한다.

```csharp
SequenceSignals.InputStageReady
```

---

# 34. Magic Number 규칙

장비 코드 내부에 의미 없는 숫자를 직접 사용하지 않는다.

나쁜 예:

```csharp
await Task.Delay(200);
if (Math.Abs(pos - target) < 0.007)
```

좋은 예:

```csharp
private const int MotionSettlingMs = 200;
private const double AxisPositionToleranceMm = 0.007;
```

Recipe 값이어야 하는 숫자는 코드 상수가 아니라 Recipe로 이동한다.

---

# 35. 시간 단위 규칙

변수명에 단위를 포함한다.

좋은 예:

```text
TimeoutMs
PositionMm
VelocityMmPerSec
AccelerationMmPerSec2
AngleDeg
ExposureUs
```

---

# 36. 좌표계 규칙

자동화 장비는 좌표계 혼동이 매우 위험하다.

반드시 구분한다.

```text
Machine Coordinate
Stage Coordinate
Scanner Coordinate
Camera Coordinate
Recipe Coordinate
Die Map Coordinate
```

좌표 변환은 전용 함수에 둔다.

예:

```csharp
StageToCamera(...)
CameraToStage(...)
MapToStage(...)
StageToMap(...)
```

---

# 37. 시뮬레이션 규칙

Simulation / DryRun은 실제 장비와 가능한 한 동일한 Sequence 흐름을 사용한다.

Simulation이라고 다음 검사를 무조건 생략하지 않는다.

- Step 변경
- Wait
- Material 이동
- Interlock 구조
- Alarm 흐름
- Recovery 흐름

Hardware 명령 부분만 시뮬레이션 구현으로 대체하는 것이 좋다.

---

# 38. 성능 규칙

자동화 프로그램에서 무조건 빠르게 실행하는 것이 목표가 아니다.

중요한 것은:

```text
결정성
안정성
예측 가능성
복구 가능성
```

불필요한 Task.Run 남발을 피한다.

I/O Bound async 함수에 Task.Run을 감싸지 않는다.

---

# 39. Task.Run 사용 기준

다음은 일반적으로 불필요하다.

```csharp
Task.Run(() => sequence.RunAsync(ct));
```

이미 `RunAsync`가 비동기로 동작한다면 직접 호출을 우선 검토한다.

단, 내부가 실제 CPU Blocking / Sync Blocking 코드라면 별도 검토한다.

---

# 40. 메소드 네이밍

권장 Prefix:

```text
Is      현재 상태
Can     동작 가능 여부
Check   조건 검사
Get     값 조회
Set     값 설정
Build   데이터 생성
Calculate 계산
Move    모션
Wait    완료 대기
Start   실행 시작
Stop    정지
Reset   초기화
Load    파일 읽기
Save    파일 저장
Update  상태 갱신
Apply   데이터 적용
Release 자원 해제
Acquire 자원 획득
```

---

# 41. bool 함수 규칙

함수명과 실제 동작이 맞아야 한다.

`IsReady()` 안에서 Alarm을 발생시키거나 상태를 변경하지 않는다.

상태 확인:

```csharp
bool IsReady()
```

검사 + Alarm:

```csharp
int CheckReady()
```

또는

```csharp
bool CheckReady(out string reason)
```

---

# 42. 한 함수 한 책임 원칙

예:

```csharp
MovePickerToPickPositionAsync()
```

이 함수에서 다음까지 전부 하지 않는다.

- Vision 검사
- Material 변경
- Recipe Save
- UI Update

Move 함수는 이동 중심으로 유지한다.

---

# 43. 반복 코드 리팩토링 기준

3회 이상 반복되는 코드가 보이면 공통화 후보이다.

단, 공통화했을 때 호출자가 이해하기 더 어려워진다면 유지할 수 있다.

장비 코드에서는 약간의 중복보다 지나친 추상화가 더 위험할 수 있다.

---

# 44. 리팩토링 우선순위

다음 순서로 검토한다.

## Priority 1 — Safety

- 인터록 누락
- Motion 완료 확인 누락
- Cancellation 누락
- Alarm 시 안전 정리 누락
- 데이터 손실 가능성

## Priority 2 — Reliability

- catch 무시
- 무한 Wait
- Timeout 없음
- Resource Release 누락
- Reservation Release 누락
- 파일 직접 덮어쓰기

## Priority 3 — Sequence Readability

- Step 흐름 불명확
- 함수 호출 깊이 과다
- 상태 변경 위치 불명확
- Magic Signal 문자열

## Priority 4 — Maintainability

- Massive Controller
- Massive Sequence
- 반복 코드
- 공통 Helper 정리
- 네이밍 개선

## Priority 5 — Style

- 코드 배치
- 주석
- Formatting

---

# 45. 기존 프로젝트 분석 방법

AI가 기존 코드를 분석할 때 다음 순서로 진행한다.

## STEP 1. 전체 구조 파악

먼저 다음 디렉토리/클래스를 찾는다.

```text
Machine / Controller
Unit
Sequence
Motion
IO
Interlock
Material
Recipe
Vision
Alarm
Log
Runtime State
UI
```

전체 파일을 무작정 읽지 않는다.

핵심 진입점부터 읽는다.

---

# 46. 진입점 분석

다음 흐름을 찾는다.

```text
프로그램 시작
↓
Machine 생성
↓
장비 초기화
↓
READY
↓
Auto Start
↓
Sequence Start
↓
Unit Sequence
↓
Motion / Vision
↓
Material Update
↓
Cycle Complete
```

---

# 47. 핵심 Sequence 1개 추적

가장 중요한 공정 하나를 끝까지 추적한다.

예:

```text
InputStage
 → FrontPicker Pick
 → Bottom Vision
 → Side Vision
 → Place
 → Output
```

그리고 각 단계에서 다음을 기록한다.

- 호출 함수
- CurrentStep
- Interlock
- Motion
- Wait
- Alarm
- Material 변경
- Recovery 정보

---

# 48. 분석 시 찾을 Code Smell

AI는 다음 항목을 검색한다.

```text
catch { }
catch (Exception) { return false; }
finally { }
Task.Run
Task.Delay(1
Thread.Sleep
while (true)
async void
File.Create
File.WriteAllText
"Ready"
"Complete"
"Start"
"Stop"
CurrentStep =
MaterialStateService
MoveAsync
WaitAsync
Alarm
```

단순히 발견했다고 무조건 수정하지 않는다.

문맥을 확인한다.

---

# 49. 리팩토링 평가표

각 파일/클래스는 다음 형태로 평가한다.

| 항목 | 상태 | 설명 |
|---|---|---|
| Safety | Good / Check / Risk | |
| Sequence Readability | Good / Check / Risk | |
| Motion Completion | Good / Check / Risk | |
| Interlock | Good / Check / Risk | |
| Cancellation | Good / Check / Risk | |
| Alarm | Good / Check / Risk | |
| Recovery | Good / Check / Risk | |
| Data Safety | Good / Check / Risk | |
| Maintainability | Good / Check / Risk | |

---

# 50. 리팩토링 변경 등급

## Level A — 안전한 정리

동작 변경 가능성이 거의 없다.

예:

- 빈 finally 제거
- 의미 없는 try/catch 제거
- 상수 이름 부여
- 로그 문구 개선
- 함수명 개선
- 중복 코드 Helper화

우선 적용 가능.

---

## Level B — 구조 개선

동작 테스트가 필요하다.

예:

- Massive Method 분리
- Step 함수 분리
- Signal 상수화
- Recipe Validation 추가
- Data Save 안전화

---

## Level C — 동작 구조 변경

실장비 검증 전에는 적용하지 않는다.

예:

- Sequence 병렬 구조 변경
- Motion 순서 변경
- Resource Lock 구조 변경
- Material Flow 변경
- Recovery 방식 변경
- Coordinator 구조 변경

---

# 51. AI에게 리팩토링을 시킬 때 기본 지시

다음 내용을 그대로 사용할 수 있다.

```text
이 프로젝트는 C# WinForms 기반 자동화 장비 프로그램이다.

프로젝트 루트의 PROGRAM_REFACTORING_GUIDE.md를 먼저 읽고
해당 규칙을 기준으로 현재 소스를 분석해라.

중요:
1. 현재 장비 동작을 임의로 변경하지 마라.
2. 대규모 재작성하지 마라.
3. 먼저 분석만 수행해라.
4. Safety / Reliability / Sequence / Material / Recipe / Data 관점에서 평가해라.
5. MUST 규칙 위반을 가장 먼저 찾아라.
6. 리팩토링 후보를 Level A / B / C로 분류해라.
7. 어떤 파일을 왜 바꿔야 하는지 구체적으로 설명해라.
8. 실제 코드 수정 전 변경 계획을 먼저 제시해라.
9. 현재 코드가 이미 잘 되어 있는 부분도 명확하게 표시해라.
10. 불필요한 디자인 패턴이나 클래스 추가는 하지 마라.

분석 결과는 다음 순서로 출력해라.

1. 현재 Architecture 요약
2. 잘 되어 있는 부분
3. 위험 요소
4. 유지보수 문제
5. 우선 리팩토링 TOP 10
6. 파일별 변경 후보
7. 예상 위험도
8. 추천 작업 순서
```

---

# 52. 특정 Sequence 분석용 지시

```text
[Sequence 이름]을 처음부터 끝까지 추적해서 분석해라.

다음 항목을 반드시 표시해라.

- Entry Point
- CurrentStep 흐름
- Motion 명령
- Motion 완료 확인
- Interlock
- 공유 자원
- Signal
- Material 변경
- Alarm
- Cancellation
- Cycle Stop
- Stop
- Recovery

그리고 실제 공정 흐름을 아래처럼 사람이 읽기 쉽게 정리해라.

Step 10 Check
 → Step 20 Move
 → Step 30 Wait
 → Step 40 Vision
 → Step 50 Material Update

코드를 바로 수정하지 말고,
먼저 잘못된 점과 개선안을 보여줘.
```

---

# 53. Motion 분석용 지시

```text
프로젝트의 모든 Motion 호출을 검사해라.

특히 다음을 찾아라.

1. Move 명령 후 Wait 없이 다음 Step으로 넘어가는 코드
2. Move 결과값을 확인하지 않는 코드
3. InPosition을 확인하지 않는 코드
4. Timeout 없는 Wait
5. CancellationToken 없는 Wait
6. 병렬 이동 시 각 축 결과를 개별 확인하지 않는 코드
7. Interlock 검사 없이 이동하는 코드

위험도가 높은 순서로 정리해라.
```

---

# 54. Data/Recipe 분석용 지시

```text
Recipe / Config / Runtime / Material 파일 저장 코드를 분석해라.

다음 항목을 확인해라.

- catch 무시
- File.Create 직접 덮어쓰기
- tmp 저장 여부
- backup 여부
- JSON pretty format
- UTF-8
- Load 실패 처리
- Validation
- Default 값 처리
- Version 호환

데이터 손실 가능성이 있는 코드를 가장 먼저 표시해라.
```

---

# 55. 최종 리팩토링 목표 Architecture

권장 구조는 복잡한 Enterprise Architecture가 아니다.

```text
WinForms UI
    │
    ▼
MachineController
    │
    ├── Unit
    │    ├── Input
    │    ├── PickerFront
    │    ├── PickerRear
    │    └── Output
    │
    ├── Sequence
    │
    ├── Motion / IO
    │
    ├── Interlock
    │
    ├── Material
    │
    ├── Recipe
    │
    └── Alarm / Log
```

핵심은:

```text
공정 흐름은 단순하게
안전 계층은 강하게
데이터 소스는 하나로
복구 정보는 명확하게
```

---

# 56. 코드 리뷰 체크리스트

코드 수정 후 다음을 확인한다.

## Safety

- [ ] Motion 전에 Interlock 확인
- [ ] Motion Command 결과 확인
- [ ] Motion 완료 Wait 존재
- [ ] InPosition 확인
- [ ] Timeout 존재
- [ ] Alarm 발생 시 안전 정리
- [ ] Cancellation 가능

## Sequence

- [ ] CurrentStep 흐름 명확
- [ ] 성공 후에만 다음 Step 이동
- [ ] Cycle Stop 처리
- [ ] Stop 처리
- [ ] Alarm Recovery 처리

## Material

- [ ] 공식 Material State만 변경
- [ ] Reservation 충돌 없음
- [ ] Abort 시 Reservation 해제

## Data

- [ ] 저장 실패 로그
- [ ] 중요 파일 Backup
- [ ] UTF-8
- [ ] Pretty JSON
- [ ] Load Validation

## UI

- [ ] UI Event에 긴 장비 로직 없음
- [ ] async void는 UI 이벤트 이외 최소화
- [ ] UI Thread Blocking 없음

## Maintainability

- [ ] 함수명이 동작을 설명
- [ ] Magic Number 최소화
- [ ] Magic String 최소화
- [ ] 과도한 추상화 없음
- [ ] 불필요한 try/catch/finally 없음

---

# 57. 최종 판단 원칙

리팩토링을 할지 고민될 때 다음 기준을 사용한다.

```text
이 변경이
장비 동작을 더 안전하게 만드는가?
문제 발생 시 원인을 더 빨리 찾게 만드는가?
현장에서 수정하기 쉬워지는가?
알람 후 복구하기 쉬워지는가?
코드 흐름을 더 쉽게 이해하게 만드는가?
```

YES라면 리팩토링 가치가 있다.

단순히

```text
최신 패턴이라서
코드가 짧아져서
클래스가 많아져서
DI를 쓰기 위해서
```

라면 장비 프로그램에서는 반드시 필요한 변경인지 다시 검토한다.

---

# 한 줄 핵심 원칙

> **자동화 장비 프로그램은 “복잡한 구조를 만드는 것”이 아니라
> “공정 순서와 안전 조건을 누구나 빠르게 이해하고, 문제 발생 시 정확히 복구할 수 있게 만드는 것”이 좋은 설계다.**
