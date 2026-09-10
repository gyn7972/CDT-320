# 인수인계 지시서 — 2026-09-09 수정 2건 재구현 (동일 코드베이스의 다른 사본용)

이 문서 하나로 대화 맥락 없이 재구현 가능하도록 작성됨. 아래 두 수정을 **이 문서에 적힌 범위만** 구현하라.
라인 번호는 사본마다 다를 수 있으므로 **코드 앵커(패턴)** 기준으로 위치를 찾을 것.

---

## 공통 규칙 (반드시 준수)

- **빌드는 우회 출력으로만**: 이 솔루션은 기본 빌드 출력이 실장비 폴더(D:\CDT-320)에 직결된다. 장비 가동 중 기본 빌드 금지.
  ```
  MSBuild.exe <솔루션> /p:Configuration=Debug /p:OutDir=<임시폴더>\build_out\ /m /v:m
  ```
- 이 문서에 없는 코드는 절대 변경 금지. 특히 **다른 팔로잉 진입부 3곳**(InputDieVisionPrepareSequence.cs, OutputPostPlaceInspectionQueue.cs, PickerPickUpSequence.VisionRetreat.cs / PickerPlaceSequence.VisionRetreat.cs)은 이번 범위 밖 — 손대지 말 것.
- 신규 모션 명령 발행 없음(판정·기록·상태 정리만)이라 MotionSpeedScale 추가 적용 대상 없음.

---

## 수정 1. InputVisionX 프리포지션 팔로잉 진입 전 "도달 가능성 검사" (3파일)

### 왜 (배경)

2026-09-09 13:51 실장비 사건: 프론트 4개 픽업 직후 리어 대상 다이 촬영 **선행이동(InputVisionXPrePosition)** 이 "픽업 퇴장 팔로잉"(리딩=FrontPickerX, 유지간격 43mm)으로 진입했는데, 그 순간 픽커의 이동 목표(615.57)+간격(43)=658.57이 비전 최종 목표(662.447)보다 **3.9mm 모자란, 애초에 팔로잉으로 도달 불가능한 기하**였다. 오버라이드 10연발로 고속 추종하다 픽커가 멈추자 "조기 정지 교착(-23)"으로 세션이 죽으며 **658.57에서 급제동("쿵")** → 이 충격으로 카메라 기구가 물리적으로 틀어짐 → 이후 촬영된 다이 3개의 비전 보정이 전부 dx=+0.52mm로 동일하게 오염 → 그 가짜 보정으로 리어 P4 픽업 미스(PICKER-BOTTOM-SIDE-FLOW-NOT-DETECTED).

이차 원인: 팔로잉에 넘기는 리딩 목표로 `CommandPosition`을 썼는데, 이 값은 **이동 중 보드 순간 지령**(사건 당시 505.277)이라 실제 이동 목표(615.57)가 아니다 — 내부 교착 감시가 잘못된 종착 기준으로 동작했다.

**팀장님 확정 지시**: "프리포지션에서는 픽커가 어디까지 빠질 건지 정확히 알 수 있으니, 비전X가 이동 전 이동 가능한지 확인 후 이동하고, 이동이 불가능하면 이동하지 않고 대기하도록 수정."

동작 요약: 게이트 루프에서 `비전 최종 목표 ≤ 리딩 이동목표 + homeGap − safetyGap − 여유` 를 추가 조건으로 검사. 불성립이면 **이동 명령 없이 루프 대기**(픽커 목표가 갱신되어 성립하면 그때 진입), 기존 1초 타임아웃(-24) 시 기존 대기점 경로 위임은 그대로. 사건 재대입: 목표 615.57일 땐 불성립 대기 → 약 0.75초 뒤 픽커가 620.351 발행 → 성립 진입(1초 내) = 급제동 없음.

### 1-A. QMC.Common/Motion/BaseAxis.cs — `LastMoveTarget` 속성 신설

①앵커: `public double CommandPosition   { get; protected set; }` 선언 **바로 아래**에 추가:

```csharp
/// <summary>마지막으로 발행된 절대 이동의 최종 목표. CommandPosition은 이동 중 보드 순간
/// 지령으로 덮이지만 이 값은 이동이 끝나도 목표를 유지한다. 절대이동 발행 관문에서만
/// 기록하며 Stop 시 NaN(목표 무효).</summary>
public double LastMoveTarget    { get; protected set; } = double.NaN;
```

②앵커: `public virtual void Stop()` 본체의 `lock (_simulationSync)` 블록 **첫 줄**(`IsMoving = false;` 위)에 추가:

```csharp
LastMoveTarget  = double.NaN;
```

③BaseAxis.cs 안에서 `CommandPosition = targetPos;` 대입이 나오는 곳 **4곳 전부**(MoveAbsoluteAsync의 스킵 분기·발행부, MoveAbsoluteCommandOnlyAsync의 스킵 분기·발행부) 바로 아래에 한 줄씩 추가:

```csharp
LastMoveTarget = targetPos;
```

### 1-B. QMC.CDT-320/Equipment/Ajin/AjinAxis.cs — 실장비 발행 관문 기록

AjinAxis.cs 안에서 `CommandPosition = targetPos;` 대입이 나오는 곳 **3곳 전부**(MoveAbsoluteAsync의 ExactMatch 스킵 분기·실발행부, MoveAbsoluteCommandOnlyAsync의 실발행부) 바로 아래에 한 줄씩 추가:

```csharp
LastMoveTarget = targetPos;
```

검증 grep: 두 파일에서 `CommandPosition = targetPos` 검색 → BaseAxis 4곳 + AjinAxis 3곳 = **7곳 전부** 바로 아래에 `LastMoveTarget = targetPos;` 짝이 있어야 한다. 인터락 거부/실패 return 경로에는 기록이 없어야 한다(대입문 아래에만 붙이면 자동 충족).

### 1-C. QMC.CDT-320/Sequencing/Picker/InputVisionXPrePositionCoordinator.cs — 게이트에 도달 가능성 조건

대상 함수: "픽업 퇴장 팔로잉 진입" 게이트 루프가 있는 함수 (로그 문구 `픽업 퇴장 팔로잉 진입 기회` 로 검색).

①앵커: `double leadingStartActual = leadingPickerX.ActualPosition;` **직후**에 추가:

```csharp
// 도달 판정 여유: 선행축이 InPosition 공차만큼 목표에 못 미치고 정지해도
// 팔로잉 최종 발행이 가능해야 하므로 공차 + 0.1mm를 추가로 요구한다.
double reachMargin = (leadingPickerX.Config != null && leadingPickerX.Config.InPositionTolerance > 0.0
    ? leadingPickerX.Config.InPositionTolerance
    : 0.0) + 0.1;
double confirmedLeadingGoal = double.NaN;
```

②앵커: 루프 안 `double visionActualNow = visionX.ActualPosition;` **직후**에 추가:

```csharp
// 도달 가능성: 선행축이 자기 이동 목표까지 다 가줘도 팔로잉 경계가 최종 목표에
// 못 미치면 이동(팔로잉 진입)하지 않고 대기한다. 선행축 목표가 갱신되어 도달
// 가능해지면 진입하고, 끝내 불가능하면 기존 타임아웃이 대기점 경로로 위임한다.
// (순간 지령 CommandPosition이 아닌 발행 목표 LastMoveTarget 기준 — 정지/미발행이면 실측)
double leadingGoalNow = leadingPickerX.LastMoveTarget;
if (double.IsNaN(leadingGoalNow))
    leadingGoalNow = leadingActualNow;
double reachableBoundNow = direction > 0
    ? leadingGoalNow + homeGap - safetyGap
    : leadingGoalNow - homeGap + safetyGap;
bool reachableNow = direction > 0
    ? finalTarget <= reachableBoundNow - reachMargin
    : finalTarget >= reachableBoundNow + reachMargin;
```

③진입 조건 변경 — 기존:

```csharp
if (leadingDepartureNow >= FollowStartMinLeadingDepartureMm &&
    firstMoveNow >= FollowStartMinFirstMoveMm)
{
```

을 다음으로 (조건 맨 앞에 `reachableNow &&` 추가, 블록 첫 줄에 목표 확정 저장):

```csharp
if (reachableNow &&
    leadingDepartureNow >= FollowStartMinLeadingDepartureMm &&
    firstMoveNow >= FollowStartMinFirstMoveMm)
{
    confirmedLeadingGoal = leadingGoalNow;
```

④계측 로그 3곳에 판정 수치 추가(실런 1회로 판정 근거가 확정되도록):

- "진입 기회 확보" 로그: 문구를 `...기회 확보(도달 가능+선행축 퇴장+첫 명령 이동량 확보).` 로 바꾸고 `leadingActual` 필드 다음에 `, leadingGoal=`/`, reachableBound=`/`, reachMargin=`/`, finalTarget=` (각 `.ToString("F3")`) 추가.
- "기회를 대기합니다" 로그: 문구를 `...(도달 가능성/선행축 퇴장/첫 명령 이동량 대기).` 로 바꾸고 `die=` 다음에 `, reachable=` + 위 4개 수치 추가.
- "타임아웃되어 기존 대기점 경로로 위임" 로그: `die=` 다음에 `, reachable=` + 위 4개 수치 추가.

⑤팔로잉 호출 인자 교체 — "픽업 퇴장 팔로잉 진입을 시작합니다" 로그의 `", leadingCommand=" + leadingPickerX.CommandPosition.ToString("F6")` 를 `", leadingGoal=" + confirmedLeadingGoal.ToString("F6")` 로 바꾸고, 바로 아래 `FollowMoveAsync(...)` 호출의 두 번째 인자 `leadingPickerX.CommandPosition` 을 `confirmedLeadingGoal` 로 교체. 호출 위에 주석:

```csharp
// 순간 지령(CommandPosition)이 아닌 발행 목표를 전달 — 내부 교착 감시가 올바른
// 리딩 종착 기준으로 동작한다.
```

---

## 수정 2. OUTPUT 맵 WAIT/대기 적용 시 수납 슬롯 DieUid 소거 — 플라잉 다이 재플레이스 (1파일 1곳)

### 왜 (배경)

OutStage에서 플라잉 다이(플레이스 후 실물 유실)가 난 자리를 다시 놓으려 했는데, OUTPUT Die Map 화면의 DIE STATE EDIT에서 어떤 상태를 찍어도 재수납이 안 됐다. 원인: 자동 수납 재개 판정(`IsOutputReceiveSlotPending`)은 **"IsTarget + Result Unknown + DieUid 빈 슬롯"** 만 다시 놓는데, WAIT/대기 적용은 Result만 Unknown으로 되돌리고 **DieUid를 안 지웠다**(SKIP 분기만 소거) → 슬롯이 계속 "이미 놓인 자리"로 인식됨. 팀장님 지시로 WAIT 분기에 DieUid 소거를 추가 — 슬롯이 미수납으로 복귀하고, 함수 말미의 기존 다음 수납 인덱스 재계산(`CalculateNextOutputReceiveIndex`)으로 다음 place 배치가 그 자리부터 다시 놓는다. 날아간 기존 다이의 기록은 되돌리지 않는다(실물 유실이라 불가피한 잔재).

### 2-A. QMC.CDT-320/Ui/Pages/Work/OutputStageMapTransferPage.cs

대상 함수: `SyncManualOutputReceiveSlotState`. 앵커 — Wait 분기(주의: 바로 아래 Skip 분기에는 이미 `slot.DieUid = "";` 가 있음, 헷갈리지 말 것):

기존:

```csharp
if (state == OutputDieManualState.Wait)
{
    slot.IsOutputInspectionDone = false;
    slot.IsOutputInspectionOk = false;
}
```

을 다음으로:

```csharp
if (state == OutputDieManualState.Wait)
{
    slot.IsOutputInspectionDone = false;
    slot.IsOutputInspectionOk = false;
    // 수납 재개 조건은 "DieUid 빈 슬롯"이므로 UID를 소거해야 그 자리에 다시
    // 플레이스한다(플라잉 다이 재수납). 날아간 기존 다이 기록은 되돌리지 않는다.
    slot.DieUid = "";
}
```

---

## 구현 후 검증

1. `CommandPosition = targetPos` 7곳 전부에 `LastMoveTarget = targetPos;` 짝 확인 (BaseAxis 4 + AjinAxis 3).
2. 우회 빌드 오류 0 확인 (기본 빌드 절대 금지 — 출력이 실장비 폴더 직결).
3. 실장비 기대 로그:
   - 수정 1: 프론트 픽업 직후 Main 로그에 `픽업 퇴장 팔로잉 진입 기회를 대기합니다(도달 가능성/...)... reachable=False` → 이어서 `진입 기회 확보(도달 가능+...)` 또는 1초 타임아웃 후 대기점 경로 — 어느 쪽이든 InputVisionX 급정지("쿵") 없이 진행.
   - 수정 2: 정지 → 플라잉 다이 자리 선택 → WAIT/대기 → APPLY → 맵에서 대기색 복귀·그리드 Result=Unknown 확인 → 시작 → 다음 배치가 그 자리부터 재플레이스.

## 원본 커밋 (참조용, 이 사본에는 없음)

- ac8555aa: `InputVisionX 프리포지션 팔로잉 진입 전 도달 가능성 검사 — 불가면 이동 없이 대기` (3파일, +55/−5)
- f80115a1: `OUTPUT 맵 WAIT/대기 적용 시 수납 슬롯 DieUid 소거 — 플라잉 다이 자리 재플레이스 허용` (1파일, +3)
