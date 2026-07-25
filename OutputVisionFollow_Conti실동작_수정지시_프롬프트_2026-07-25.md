# CDT-320: Output 비전 팔로잉/최소 회피를 ContiSegmentedPlace 모드에서 실제로 동작시키기 (Input 미러)

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
수정 파일은 정확히 2개다:
- 작업 O-1 / O-2: `QMC.CDT-320\Sequencing\Picker\PickerPlaceSequence.cs`
- 작업 O-3: `QMC.CDT-320\Sequencing\OutputStage\OutputPostPlaceInspectionQueue.cs`

**AjinAxis.cs / FollowMoveAsync / 인터락 파일 / SharedRailX 파일 / Input 측 파일
(PickerPickUpSequence.cs, InputDieVisionPrepareSequence.cs)은 절대 건드리지 않는다.**

## 운전 모드 전제 (사용자 확정 2026-07-25)
**`Place.MotionMode == ContiSegmentedPlace`(Conti 모드)에서 동작해야 한다.**
- 게이트 조건 자체는 **변경하지 않는다.** `IsCoordinatedPlaceMotionMode`
  (`PickerPlaceSequence.cs:2415`)는 `mode == PickerPlaceMotionMode.ContiSegmentedPlace` 그대로 유지.
  `Default` 모드를 게이트에 추가하지 말 것.
- 이 작업은 "Conti 모드인데도 팔로잉/최소 회피가 실행되지 않는" 구조적 누락을 메우는 것이다.
- 참고(작업자 조치 불요): Front/Rear 피커의 `PLACE MOTION MODE`는 RECIPE 화면에서 설정한다
  (`Ui\Pages\Recipe\FrontPickerRecipePage.cs:510`, `RearPickerRecipePage.cs:536`). 설정값 자체는
  사용자가 관리하므로 코드/데이터로 강제하지 말 것.

## 배경 — Conti 모드인데도 동작하지 않는 이유 (코드 확정)

Output 측 구현은 Input과 미러로 **이미 완성돼 있다.** 헬퍼가 전부 존재한다:
- `MovePlacePickerXEntryByVisionFollowOrFallbackAsync` (`PickerPlaceSequence.cs:2542`) — X 단독 follow + R6 폴백
- `TryFollowPickerXBehindOutputVisionRetreatAsync` (`:2574`)
- `MovePlacePickerXTThenYWithVisionFollowAsync` (`:2488`)
- `ShouldFollowOutputVisionRetreatForPickerEntry` (`:2478`)
- 후검사 측: `TryFollowOutputVisionXBehindPickerAsync` (`OutputPostPlaceInspectionQueue.cs:1636`),
  `ShouldFollowPickerForOutputVisionEntry` (`:1580`), `ResolveConstrainingPickerXForOutputVisionEntry` (`:1612`)

문제는 **호출 위치**와 **타이밍**이다.

### 문제 O-1: Place 진입 follow 분기가 Conti 본경로에 없다
follow 분기는 `MoveOutputStageYAndPickerXYTToPlaceAsync`(`:2237`)의 `:2252` **단 한 곳**뿐이다.
`MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync`(`:2347`) 분기표:

| 분기 | 도착 | follow 분기 |
|---|---|---|
| `:2367` 재시작 안전 진입(ForceSafeYBeforeFirstPlaceMove) | `MoveOutputStageYAndPickerXTThenYToPlaceAsync`(`:2291`) → `:2303` `MovePickerAxesAndVerifyAsync` | **없음** |
| `:2376` 비Conti | XYT(`:2237`) | 있음 (게이트 false로 Task 없음) |
| `:2391` 배치 첫 Place | XYT(`:2237`) | 있음 ← 유일한 실동작 가능 지점 |
| `:2404` **Conti 정상(2번째 Place 이후)** | `...ByContiSegmentedPlaceAsync`(`:2642`) → `:2769` `MovePickerAxisAndVerifyAsync` | **없음** |
| `:2671`/`:2686`/`:2701` Conti 가드 거부 | XYT(`:2237`) | 있음 |

Input은 Default(`PickerPickUpSequence.cs:2307`)/Conti(`:2506`)/FastConti(`:2859`) **세 경로 전부**에
follow를 심었는데, Output은 XYT 폴백 경로에만 심겨 있다. 그래서 Conti 정상 운전(배치 2번째 Place
이후)에서는 `:2769`의 평범한 PickerX 이동이 나가고 팔로잉이 전혀 걸리지 않는다.

### 문제 O-2: 회피 시작이 너무 일러 게이트가 탈락한다 (Input 작업 1과 동일)
`:1806~1818`에서 회피를 비동기 시작한 뒤, 피커 X가 실제 출발하기까지 완료를 await하는 것들이 끼어 있다:
- `MoveOutputStageReceivePositionAsync`(`:1898`)의 `EnsureOutputFeederSafeBeforePlaceStageMoveAsync`(`:1900`),
  `EnsurePickerWorkAreaReserved`(`:1910`), `PreparePlaceTargetValues`(`:1914`)
- Conti 경로 내부: `MovePickerYAndTToPlaceBeforeContiSegmentedPlaceAsync`(`:2704`),
  `EnsureOutputStageZReadyForPlaceAsync`(`:2708`), `CompletePendingContiRetreatIfNeededAsync`(`:2717`)

짧은 최소 회피는 그 사이에 끝나므로 `ShouldFollowOutputVisionRetreatForPickerEntry`(`:2478`)의
`OutputCameraX.IsMoving`이 false → 일반 이동으로 탈락한다.

### 문제 O-3: 후검사 return-follow가 중간 위치에 들렀다 간다 (Input 작업 2와 동일)
`ShouldFollowPickerForOutputVisionEntry`(`:1580`)는 검사 목표가 페어 간격을 못 만족하면 **피커 상태와
무관하게 항상** follow를 시작한다(#18 R2 확정 사양). FollowMoveAsync는 선행 피커가 정지해 있어도
현재 여유만큼 후행축을 전진시키므로, 비전이 진입 한계까지 먼저 가서 정지→대기→피커 퇴장 후 재이동한다.
Input 측에서 사용자가 이 동작의 변경을 지시했으므로(2026-07-25) Output도 동일하게 바꾼다.
**이 지시가 #18 R2를 대체한다.**

---

# 작업 O-2 — 회피 이동을 피커 X 진입 직전으로 이연

원칙: **회피 좌표 계산은 기존 위치(`:1721~1800`)에서 그대로 하고, 이동 명령 발행만 미룬다.**
FollowMoveAsync는 선행축이 출발 전이어도 현재 간격의 여유만큼 즉시 전진 명령을 내고 선행축이
움직이는 만큼 오버라이드로 연장하므로 시작 순서 역전 문제가 없다.

### O-2-A. 상태 필드 신설
`:57` `_outputVisionRetreatMoveTask` 옆에:
```csharp
        // O-2(2026-07-25): 최소 회피 이동을 피커 X 진입 직전까지 미룬다(오버랩 성립).
        private bool _outputVisionRetreatDeferred;
```

### O-2-B. `MoveOutputStageAvoidPositionAsync` — 비동기 시작 → 이연으로 교체
`:1806~1818`의 `useMinimalRetreat` 분기를 변경한다:
- `JoinOutputVisionRetreatMoveTaskAsync("이전 회피 Task 정리", ct)`는 그대로 먼저 호출한다.
- `_outputVisionRetreatTarget = visionTarget;`은 그대로 유지한다(이연 시 목표 보관용으로 필수).
- `_outputVisionRetreatMoveTask = MoveOutputStageAxisAndVerifyAsync(...)` 호출을 **제거**하고
  `_outputVisionRetreatDeferred = true;`로 바꾼다. 로그 문구를 교체:
  `"OutputVisionX 최소 회피 이동을 피커 X 진입 시점까지 이연합니다(팔로잉 오버랩). target=... - Check"`
- `else`(legacy 동기 이동) 분기는 **완전 무변경.**
- 이 메서드 진입부(`:1721` `fullAvoid` 계산 직전)에서 `_outputVisionRetreatDeferred = false;`로
  스테일 플래그를 먼저 지운다.

### O-2-C. 이연 회피 기동 헬퍼 신설
```csharp
        // O-2: 미뤄둔 최소 회피 이동을 지금 비동기 시작한다. 피커 X 진입(팔로잉/일반) 직전에 호출.
        private async Task StartDeferredOutputVisionRetreatIfPendingAsync(CancellationToken ct)
        {
            if (!_outputVisionRetreatDeferred)
                return;

            _outputVisionRetreatDeferred = false;
            await JoinOutputVisionRetreatMoveTaskAsync("이연 회피 시작 전 정리", ct).ConfigureAwait(false);

            _outputVisionRetreatMoveTask = MoveOutputStageAxisAndVerifyAsync(
                BinStageAxis.VisionX,
                _outputVisionRetreatTarget,
                "OutputVisionX 최소 회피 위치 이동",
                ct);
            WriteLog("PickerPlaceSequence",
                Name + " OutputVisionX 이연 최소 회피 이동을 비동기 시작했습니다(피커 진입 팔로잉 오버랩). " +
                "target=" + _outputVisionRetreatTarget.ToString("F6") + " - Start");
        }
```
(`MoveOutputStageAxisAndVerifyAsync`는 인포지션이면 자체적으로 즉시 완료되므로 Input처럼 별도
인포지션 단락 검사를 넣지 않는다. 기존 `:1810` 호출과 인자를 동일하게 유지할 것.)

### O-2-D. 게이트 수정 — `ShouldFollowOutputVisionRetreatForPickerEntry` (`:2478`)
```csharp
            return OutputStage != null &&
                   OutputStage.OutputCameraX != null &&
                   (_outputVisionRetreatDeferred ||
                    (_outputVisionRetreatMoveTask != null && OutputStage.OutputCameraX.IsMoving));
```

### O-2-E. 기동 지점 — `MovePlacePickerXEntryByVisionFollowOrFallbackAsync` (`:2542`)
메서드 첫 줄(기존 `TryFollowPickerXBehindOutputVisionRetreatAsync` 호출 **직전**)에:
```csharp
            await StartDeferredOutputVisionRetreatIfPendingAsync(ct).ConfigureAwait(false);
```
이로써 "회피 명령 발행 → 즉시 FollowMoveAsync" 순서가 한 지점에서 보장된다. 기존 R6 폴백
(`JoinOutputVisionRetreatMoveTaskAsync` → 일반 이동 재시도)은 그대로 두면 된다.

### O-2-F. 안전 불변식 — 일반 이동 전 이연 회피 완료 보장 (필수)
이연 상태에서 비전이 아직 검사/촬영 위치에 있는데 피커 X가 **일반 이동**으로 Output 존에 진입하면
인터락(-11)에 걸린다. follow를 타지 않는 모든 피커 X 이동 분기 앞에 아래 2단계를 넣는다:
```csharp
            if (_outputVisionRetreatDeferred)
            {
                await StartDeferredOutputVisionRetreatIfPendingAsync(ct).ConfigureAwait(false);
                int deferredJoin = await JoinOutputVisionRetreatMoveTaskAsync("일반 이동 전 이연 회피 완료", ct).ConfigureAwait(false);
                if (deferredJoin != 0)
                    return deferredJoin;
            }
```
적용 지점:
1. `MoveOutputStageYAndPickerXYTToPlaceAsync`(`:2237`) — `:2252` 삼항의 else(일반 이동)를 타기 전.
   삼항식을 if/else로 풀어서 else 쪽에만 방어를 넣는다.
2. `MoveOutputStageYAndPickerXTThenYToPlaceAsync`(`:2291`) — `:2303` `MovePickerAxesAndVerifyAsync`
   호출 전 (재시작 경로. follow를 심지 않고 방어만 넣는다 — O-1-B 참조).
3. `MovePickerXYAndTToPlaceAsync`(`:2217`) — `:2225` 호출 전. (스텝 `:522` 직접 진입 경로)
4. O-1에서 Conti 경로에 follow를 심은 뒤, 그 분기가 `pickerXForceMove == true` 등으로 follow를
   쓰지 않고 일반 이동으로 가는 경우.

### O-2-G. 리셋/정리 지점에 플래그 클리어 추가
- `:310` 및 `:839`의 `_pickerZPlacedByContiSegmentedPlace = false;` 옆 (배치/사이클 초기화 지점):
  `_outputVisionRetreatDeferred = false;`
- `ObserveOutputVisionRetreatMoveTaskOnAbort`(`:2460`): 첫 줄에 `_outputVisionRetreatDeferred = false;`
- `JoinOutputVisionRetreatMoveTaskAsync`(`:2421`)는 무변경 — 이연 플래그를 여기서 건드리면
  O-2-F의 2단계 순서가 깨진다.
- `:1903`, `:1917`, `:1926`의 기존 join 호출은 무변경. 단 `:1926`("피커 X 진입 완료") 시점에
  이연이 소비되지 않은 채 남아 있으면 회피가 아예 실행되지 않은 것이므로, 그 앞에
  `if (_outputVisionRetreatDeferred)` 경고 로그 1줄을 남기고 `StartDeferred...` + join으로
  마무리한다(방어적 — 정상 흐름에서는 발생하지 않아야 한다).

---

# 작업 O-1 — Conti 본경로 및 재시작 경로에 follow 분기 배치

### O-1-A. Conti 정상 경로 (`MoveOutputStageYPickerXAndPickerZByContiSegmentedPlaceAsync`, `:2642`)
`:2769~2775`의 PickerX 이동 Task 생성부를 조건 분기로 바꾼다.

변경 전:
```csharp
            Task<int> pickerXMove = MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                pickerXTarget,
                "Place ContiNode PickerX 비동기 이동",
                ct,
                placeTargetName,
                pickerXForceMove);
```
변경 후:
```csharp
            // O-1(2026-07-25): 비전 회피가 이연/진행 중이면 X만 follow로 진입한다(Input Conti 경로 미러).
            //   forceMove가 필요한 경우는 follow가 forceMove 의미를 보장하지 못하므로 기존 일반 이동을 쓴다.
            Task<int> pickerXMove;
            if (!pickerXForceMove && ShouldFollowOutputVisionRetreatForPickerEntry())
            {
                pickerXMove = MovePlacePickerXEntryByVisionFollowOrFallbackAsync(
                    "Place ContiNode PickerX 팔로잉 진입",
                    placeTargetName,
                    ct);
            }
            else
            {
                pickerXMove = MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    pickerXTarget,
                    "Place ContiNode PickerX 비동기 이동",
                    ct,
                    placeTargetName,
                    pickerXForceMove);
            }
```
필수 확인 사항 (보고서에 명시):
- `MovePlacePickerXEntryByVisionFollowOrFallbackAsync`는 목표로 `_targetPickerX`를 사용한다
  (`:2561`). 이 경로의 `pickerXTarget`은 `:2727`에서 `_targetPickerX`를 복사한 값이므로 동일하다 —
  **다르면 follow를 쓰지 말고 기존 일반 이동을 유지**하고 그 사실을 보고한다.
- `pickerXMove` Task는 `MovePickerZPlaceAfterContiProgressAsync`(`:2776`)에 그대로 전달된다.
  Z PrePlace 트리거는 PickerX 실위치 진행률로 판정하므로 follow의 분할 이동에서도 유효하다.
  이 함수가 `pickerXMove`의 완료만 관찰하는지(내부에서 X에 추가 명령을 내지 않는지) 코드로
  확인하고 결과를 보고한다. X에 별도 명령을 내는 구조라면 follow와 충돌하므로 **적용을 보류하고
  보고**한다.
- `:2733~2744`의 시작 전 상태 검사(`pickerX.IsMoving`이면 Fail)는 **무변경**. follow는 그 이후에
  시작되므로 영향 없다.

### O-1-B. 재시작 경로 (`MoveOutputStageYAndPickerXTThenYToPlaceAsync`, `:2291`)
이 경로는 `ForceSafeYBeforeFirstPlaceMove`(재시작 첫 접근)로만 진입하며 안전 우선 경로다.
**follow를 심지 않는다.** O-2-F의 방어(이연 회피 시작 + join)만 넣어 일반 이동 전에 회피가
완료되도록 한다. 그 이유를 주석으로 남긴다.

### O-1-C. 무변경 확인 대상
`IsCoordinatedPlaceMotionMode`(`:2415`), `CanUseContiSegmentedPlaceFromCurrentPosition`(`:2899`),
`BuildContiSegmentedPlaceNodes`(`:2856`), `WaitContiSegmentedPlaceFinalPositionAsync`(`:3023`),
`TryFollowPickerXBehindOutputVisionRetreatAsync`(`:2574`), 최소 회피 좌표 계산부(`:1721~1800`) — 전부 그대로.

---

# 작업 O-3 — 후검사 return-follow: 검사 위치까지 한 번에 갈 수 있을 때만 출발

파일: `QMC.CDT-320\Sequencing\OutputStage\OutputPostPlaceInspectionQueue.cs`
**Input 작업 2(`InputDieVisionPrepareSequence`의 `WaitInputVisionReturnFollowOpportunityAsync`)와
동일한 구조로 미러 구현한다.** Input 작업이 먼저 적용돼 있으면 그 구현을 그대로 미러할 것.

### O-3-A. 기회 대기 헬퍼 신설
`ShouldFollowPickerForOutputVisionEntry`(`:1580`)를 아래 헬퍼로 대체·확장한다
(기존 clearance 4판정 코드를 (a)로 재사용):

```csharp
        // 기존 조건(#18 R2): 목표가 페어 간격 미충족이면 정지 선행축이어도 항상 follow —
        //   비전이 진입 한계까지 선진입 후 대기해 "중간 위치에 들렀다 가는" 동작이 됐다.
        // 현재 기준(사용자 지시 2026-07-25): 검사 목표까지 한 번에 도달 가능할 때만 출발한다.
        //   (a) 목표가 양 피커 Actual/Command 페어 간격을 이미 만족 → false(기존 대기+일반 이동)
        //   (b) 제약 피커의 Command가 목표를 safetyGap까지 열어줌(퇴장 명령 발행됨)
        //       그리고 반대 피커 Command 기준 목표 간격 충족 → true(follow 추종 진입)
        //   (c) 둘 다 아니면 폴링 대기 → 타임아웃 시 false(기존 대기+일반 이동에 위임)
        private async Task<bool> WaitOutputVisionReturnFollowOpportunityAsync(
            OutputStageUnit stage, double targetVisionX, OutputPostPlaceInspectionRequest request,
            int timeoutMs, CancellationToken ct)
```

구현 요건:
1. 진입 즉시 `stage.OutputCameraX is AjinAxis` 아니거나 service/`_context` null이면 `false`
   (기존 `:1584~1590`과 동일).
2. 루프 본문:
   - `ct.ThrowIfCancellationRequested()` + `if (IsStopOrAlarmActive()) return false;`
     (이 파일의 기존 관례 — `:863` 등을 따른다).
   - **(a)** 기존 `:1595~1602`의 Front/Rear Actual/Command 4판정을 그대로 수행. 전부 만족 → `return false;`
   - **(b)** `ResolveConstrainingPickerXForOutputVisionEntry()`(`:1612`)로 제약 피커 선정 →
     `service.TryGetFollowGapParameters(stage.OutputCameraX, 제약피커, OutputVisionRetreatExtraClearance,
     out direction, out homeGap, out safetyGap, out detail)` (`:1653` 기존 호출과 동일 인자).
     조회 실패 시 → `return false;`
     간격 계산 (`AjinAxis.FollowMoveAsync:629`의 페어식과 동일, 부호는 direction으로):
     ```csharp
     double leadingCommand = constrainingPickerX.CommandPosition;
     double gapAtCommand = direction > 0
         ? (leadingCommand + homeGap) - targetVisionX
         : (targetVisionX + homeGap) - leadingCommand;
     bool leadingOpensTarget = gapAtCommand + 0.000001 >= safetyGap;
     ```
     반대(비제약) 피커는 `service.IsPairClearanceSatisfied(반대피커, 반대피커.CommandPosition,
     stage.OutputCameraX, targetVisionX, out _)` 한 가지만 확인한다(Actual은 잔여 이동으로 곧
     열리며, 위반 시도는 follow 내부 MotionGuard가 -11로 거부 → 기존 R5 폴백이 받는다 —
     이 근거를 주석으로 명시).
     `leadingOpensTarget && 반대피커 Command 충족` → `return true;`
   - **(c)** `await Task.Delay(20, ct).ConfigureAwait(false);` 후 재판정.
3. 타임아웃: 인자로 받은 `timeoutMs`(호출부의 기존 `timeout` 전달) 경과 시 `return false;`
   — 신규 알람/Fail 코드를 만들지 않는다. 기존 `WaitOutputVisionXSharedRailClearAsync` +
   일반 이동 경로가 자체 대기/실패 처리를 갖고 있으므로 거기에 위임한다.
4. 최초 대기 진입 시 1회 Wait 로그, 해소 시 Ok 로그. 로그는 이 파일 관례
   (`Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection", ...)`)를 따르고 `die`/`side`를 포함한다.

### O-3-B. 호출부 교체 (`:870~871`)
변경 전:
```csharp
                if (IsMinimalRetreatGateSatisfied(request) &&
                    ShouldFollowPickerForOutputVisionEntry(stage, targetVisionX))
```
변경 후:
```csharp
                if (IsMinimalRetreatGateSatisfied(request) &&
                    await WaitOutputVisionReturnFollowOpportunityAsync(
                        stage, targetVisionX, request, timeout, ct).ConfigureAwait(false))
```
이후 `TryFollowOutputVisionXBehindPickerAsync` 호출과 `followEntryUsed` 폴백 구조(`:873~904`)는
**무변경**.

### O-3-C. 정리
- `ShouldFollowPickerForOutputVisionEntry`는 (a) 판정용 private 헬퍼로 남기거나 새 헬퍼에 흡수한다.
  `grep ShouldFollowPickerForOutputVisionEntry`로 잔여 참조 0을 확인한다.
- `IsMinimalRetreatGateSatisfied`(`:1557`), `TryFollowOutputVisionXBehindPickerAsync`(`:1636`),
  `ResolveConstrainingPickerXForOutputVisionEntry`(`:1612`), `MoveVisionXToAvoidAsync`(`:1471`)의
  촬영 종료 후 최소 회피 로직 — 전부 무변경.

---

## 변경 금지 (전 작업 공통)
- `AjinAxis.cs` 전체 (FollowMoveAsync, WaitUntilMoveDone, TryOverridePosition 포함).
- 인터락 파일 전체(`Equipment\Interlocks\*`) — diff 0건. `BeginMotionGuardBypass` 사용 금지.
- `SharedRailXMotionService` / `SharedRailXConfig` / 설정 스토어 —
  `TryGetFollowGapParameters` / `IsPairClearanceSatisfied` / `TryResolveMinimalVisionRetreatTarget`을
  호출만 하고 수정하지 않는다.
- `IsCoordinatedPlaceMotionMode` 판정 조건, `PickerPlaceMotionMode` enum, 레시피/설정 데이터
  (`EquipmentData`, `Recipes`), UI 페이지.
- Input 측 파일 2개(`PickerPickUpSequence.cs`, `InputDieVisionPrepareSequence.cs`) — 별도 지시로 진행 중.
- 최소 회피 좌표 계산 로직(`PickerPlaceSequence.cs:1721~1800`, `OutputPostPlaceInspectionQueue.cs:1488~1520`).

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과.
2. `git diff --stat`이 정확히 2개 파일만 표시: `PickerPlaceSequence.cs`, `OutputPostPlaceInspectionQueue.cs`.
3. 인터락 폴더 diff 0건: `git diff --stat -- QMC.CDT-320/Equipment/Interlocks` 빈 출력.
4. `grep -n "BeginMotionGuardBypass" QMC.CDT-320/Sequencing/Picker/PickerPlaceSequence.cs
   QMC.CDT-320/Sequencing/OutputStage/OutputPostPlaceInspectionQueue.cs` → 0건.
5. `IsCoordinatedPlaceMotionMode` 본문이 `mode == PickerPlaceMotionMode.ContiSegmentedPlace` 그대로인지 확인.
6. O-2 논리 확인: 이연 플래그가 설정되는 경로에서 (i) follow 경로는
   `StartDeferredOutputVisionRetreatIfPendingAsync` → `FollowMoveAsync` 순서로 호출되는지,
   (ii) 일반 이동 경로 4곳 전부가 이연 회피 시작+join 후에만 X 명령을 내는지,
   (iii) 배치 초기화/Abort에서 플래그가 지워지는지 — 세 가지를 코드로 짚어 보고한다.
7. O-1 논리 확인: Conti 정상 경로에서 `pickerXTarget == _targetPickerX` 동일성과
   `MovePickerZPlaceAfterContiProgressAsync`가 PickerX에 추가 명령을 내지 않음을 코드로 확인해 보고한다.
8. O-3 논리 확인: (a)/(b)/(c) 세 분기의 반환/대기 경로와 타임아웃 위임을 코드로 짚어 보고한다.

## 시뮬 검증 기준 (실장비는 사용자가 확인)
Conti 모드(`Place.MotionMode = ContiSegmentedPlace`) + Auto 운전 기준.

O-1/O-2:
- Place 사이클 로그 순서:
  `"OutputVisionX 피커 진입 회피 좌표를 확정했습니다. mode=minimal"` →
  `"OutputVisionX 최소 회피 이동을 피커 X 진입 시점까지 이연합니다"` →
  `"OutputVisionX 이연 최소 회피 이동을 비동기 시작했습니다"` ≈
  `"Place 피커X 팔로잉 진입을 시작합니다"` (수 ms 이내) →
  `AX-FOLLOW-MOVE ... "팔로잉 최초 이동 명령을 발행했습니다"`.
- **배치 2번째 Place 이후(Conti 본경로)에서도** 위 팔로잉 로그가 나타난다 — 이번 작업의 핵심 수용 기준.
- `AxisMoveProfile OutputVisionX ABS MOVE - Start`와 PickerX follow 시작 로그의 타임스탬프가 겹친다.
- 인터락 위반/알람 0건, `-11`/`-5` 미발생.

O-3:
- 피커가 Output 존 안에서 작업 중일 때 후검사 차례가 와도 `OutputVisionX`의
  `AxisMoveProfile ABS MOVE`가 발행되지 않고 Wait 로그로 대기한다 (기존: 한계 위치로 선진입).
- 피커 퇴장 명령 발행 직후 `"Output camera 후검사 VisionX 팔로잉 진입"` → 검사 위치까지
  중간 정지 없이 연속 추종 → 도달.
- 존이 이미 빈 경우(2번째 이후 검사)는 기존 대기+일반 이동 그대로.
- follow 실패(-21/-11) 시 기존 폴백 로그 후 정상 완료.

공통:
- `Place.MotionMode = Default`로 두면 O-1/O-2/O-3 전부 게이트 false → 기존 경로 완전 무변경
  (회귀 없음)을 로그로 확인한다.

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. `MoveOutputStageYAndPickerXTThenYToPlaceAsync`(재시작 경로)는 follow 미적용으로 남는다(O-1-B 결정).
2. Output 페어의 `HomeClearance`가 Input(19)보다 크므로(로컬 설정 390) 최소 회피 목표가
   전체 Avoid를 넘어가면 `TryResolveMinimalVisionRetreatTarget`이 fullAvoid로 클램프한다
   (`SharedRailXMotionService.cs:569`). 그 경우 로그 `detail`에 근거가 남으므로, 현장에서
   `mode=minimal`인데 회피량이 기대와 다르면 그 `detail` 문구를 사용자에게 보고한다.
   **좌표 계산 로직은 이번 작업에서 수정하지 않는다.**
