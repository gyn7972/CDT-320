# CDT-320: 비전↔피커 팔로잉 2건 수정 — (1) 픽업 진입 오버랩 미성립 (2) 비전 복귀가 중간 위치에 들렀다 감

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
수정 파일은 정확히 2개다:
- 작업 1: `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs`
- 작업 2: `QMC.CDT-320\Sequencing\Picker\InputDieVisionPrepareSequence.cs`

**AjinAxis.cs / FollowMoveAsync / 인터락 파일 / SharedRailX 파일 / PickerPlaceSequence.cs /
OutputPostPlaceInspectionQueue.cs는 절대 건드리지 않는다.** (Output 측에도 같은 패턴이 있으나
이번 지시 범위가 아니다 — 별도 지시 예정.)

전제: `AjinAxis.cs`의 폴링은 `Task.Delay(1)`로 이미 수정되어 축 이동 async 메서드가 명령 발행 직후
호출자에게 제어를 돌려준다(비동기 계약 복원됨). 이 전제 위에서 아래 두 문제가 남아 있다.

---

# 작업 1 — 픽업: 비전 회피 ∥ 피커 X 진입 오버랩이 성립하지 않음

## 증상 (실장비)
Auto + Conti/FastConti 픽업에서 InputVisionX가 최소 회피를 **다 끝낸 뒤에** 피커 X가 진입한다.
의도(#17 프롬프트)는 "비전이 회피 이동 중일 때 피커 X가 FollowMoveAsync 포지션 오버라이드로
추종하며 동시 진입"이다. 로그에 `"PickUp 피커X 팔로잉 진입을 시작합니다"`가 나오지 않는다.

## 원인 (코드 확정)
비전 회피 비동기 시작 지점과 피커 X 진입 지점 사이에 **완료를 await하는 선행 모션들이 끼어 있어**,
짧은 최소 회피(예: 650→638.5, 11.5mm)가 그 사이에 끝나버린다:

1. `PickerPickUpSequence.cs:1188` — `MoveInputVisionToAvoidForPickerMoveAsync`에서
   `_inputVisionRetreatMoveTask = RunInputVisionRetreatMoveAsync(...)` 비동기 시작.
2. 그 후 피커 X 출발 전까지 순차 await되는 것들:
   - `:2212` `EnsureWaferAlignThetaPositionAsync` (StageT 보정)
   - FastConti: `:2829` PickerY/T 선행 보정, `:2844` `EnsureFastPickerZAxesSafeAsync`
   - Conti: `:2432` `MovePickerYPickerTAndEjectPinZBeforeContiPickUpAsync`
   - Default: `:2191` Z Avoid, `:2286`/`:2293` EjectPinZ/PickerY 확인
3. 피커 X 진입 시점(`:2859` FastConti / `:2506` Conti / `:2307` Default)의 follow 게이트
   `ShouldFollowInputVisionRetreatForPickerEntry` (`:1336`)가 `stage.CameraX.IsMoving`을 요구하는데,
   이미 회피가 끝나 `IsMoving == false` → **항상 일반 이동으로 탈락**한다.

## 수정 — 회피 시작을 피커 X 진입 직전으로 이연(deferral)

원칙: **회피 좌표 계산은 기존 위치에서 그대로 하되, 이동 명령 발행을 피커 X 진입 시점까지 미룬다.**
그러면 회피와 피커 X follow가 같은 순간에 기동되어 오버랩이 실제로 성립한다.
(FollowMoveAsync는 선행축이 아직 출발 전이어도 현재 간격의 여유만큼 즉시 전진 명령을 내고,
선행축이 움직이는 만큼 오버라이드로 연장하므로 시작 순서 역전 문제가 없다.)

### 1-A. 상태 필드 신설
`:62` `_inputVisionPickerEntryTargetPrepared` / `:65` `_inputVisionRetreatMoveTask` 옆에:
```csharp
        // R3-이연(2026-07-25): 최소 회피 이동을 피커 X 진입 직전까지 미룬다(오버랩 성립).
        private bool _inputVisionRetreatDeferred;
```

### 1-B. `MoveInputVisionToAvoidForPickerMoveAsync` (`:1095`) — 비동기 시작 → 이연으로 교체
`:1181~1225` 블록 변경:
- `startRetreatAsync`(= `useMinimalRetreat && targetsCalculated`) && 미인포지션이면:
  기존의 `JoinInputVisionRetreatMoveTaskAsync(...)` + `_inputVisionRetreatMoveTask = Run...(...)` 대신
  **이동 명령을 내지 않고** `_inputVisionRetreatDeferred = true;`만 설정하고 로그를 남긴다
  (예: `"InputVisionX 최소 회피 이동을 피커 X 진입 시점까지 이연합니다(팔로잉 오버랩). target=..."`).
  스텝 전이(`SelectNextPickTarget`)는 기존과 동일.
- 이연했을 때는 `:1216~1225`의 `CheckInputStageAxisInPosition` 최종 확인을 **건너뛴다**
  (비전이 의도적으로 아직 회피 전이므로 체크하면 실패한다). 기존 `retreatStartedAsync` 변수의
  역할을 이연 플래그가 대신하게 정리한다.
- 비이연 경로(legacy/수동/targetsCalculated=false, 동기 이동 경로)는 **완전 무변경**.
- 이 메서드 진입 시(배치당 1회) `_inputVisionRetreatDeferred = false;`로 스테일 플래그를 먼저 지운다.

### 1-C. 이연 회피 기동 헬퍼 신설
```csharp
        // R3-이연: 미뤄둔 최소 회피 이동을 지금 비동기 시작한다. 피커 X 진입(팔로잉/일반) 직전에 호출.
        private async Task StartDeferredInputVisionRetreatIfPendingAsync(InputStageUnit stage, CancellationToken ct)
        {
            if (!_inputVisionRetreatDeferred)
                return;

            _inputVisionRetreatDeferred = false;
            await JoinInputVisionRetreatMoveTaskAsync("이연 회피 시작 전 정리", ct).ConfigureAwait(false);
            if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, _inputVisionPickerEntryTarget))
                return;

            _inputVisionRetreatMoveTask = RunInputVisionRetreatMoveAsync(stage, _inputVisionPickerEntryTarget, ct);
            WriteLog("PickerPickUpSequence",
                Name + " InputVisionX 이연 최소 회피 이동을 비동기 시작했습니다(피커 진입 팔로잉 오버랩). " +
                "target=" + _inputVisionPickerEntryTarget.ToString("F6") + " - Start");
        }
```

### 1-D. 게이트 수정 — `ShouldFollowInputVisionRetreatForPickerEntry` (`:1336`)
```csharp
            return _inputVisionPickerEntryTargetPrepared &&
                   stage != null &&
                   stage.CameraX != null &&
                   (_inputVisionRetreatDeferred ||
                    (_inputVisionRetreatMoveTask != null && stage.CameraX.IsMoving));
```
(이연 대기 중이면 무조건 follow 경로. 이미 시작된 Task가 살아 있고 비전이 이동 중인 기존 조건은 유지.)

### 1-E. 기동 지점 — `MovePickerXEntryByVisionFollowOrFallbackAsync` (`:1372`)
메서드 첫 줄(기존 `TryFollowPickerXBehindInputVisionRetreatAsync` 호출 **직전**)에:
```csharp
            await StartDeferredInputVisionRetreatIfPendingAsync(stage, ct).ConfigureAwait(false);
```
이로써 "회피 명령 발행 → 즉시 FollowMoveAsync" 순서가 한 지점에서 보장된다.
R6 폴백(follow 실패 → `JoinInputVisionRetreatMoveTaskAsync` → 일반 이동 1회 재시도)은 그대로 두면
된다 — 이 시점에는 Task가 이미 시작돼 있으므로 기존 join이 그대로 동작한다.

### 1-F. 안전 불변식 — 일반 이동 전 이연 회피 완료 보장 (필수)
이연 상태에서 비전이 아직 검사 위치(깊은 쪽)에 있는데 피커 X가 **일반 이동**으로 진입하면
인터락(-11, `VerifyInputVisionXAtAvoidOrBelowZero`)에 걸려 알람이 난다. 따라서 follow를 타지 않는
모든 일반 이동 분기 앞에 방어를 넣는다:
- `StartPickUpPickerXEntryMoveTask` (`:1347`)의 else(일반 이동) 분기 진입 시:
  ```csharp
            if (_inputVisionRetreatDeferred)
            {
                await StartDeferredInputVisionRetreatIfPendingAsync(stage, ct).ConfigureAwait(false);
                int deferredJoin = await JoinInputVisionRetreatMoveTaskAsync("일반 이동 전 이연 회피 완료", ct).ConfigureAwait(false);
                if (deferredJoin != 0)
                    return deferredJoin; // Task<int> 시그니처에 맞게 async 전환 필요 시 전환
            }
  ```
  (게이트가 1-D처럼 바뀌면 이 분기에 이연 상태로 들어올 일은 사실상 없지만, `stage`/`CameraX` null 등
  방어적 케이스를 위한 안전망이다. 메서드가 현재 non-async면 async로 전환하고 호출부 영향 없음을 확인.)
- Default 경로 `:2321` else(X/T 묶음 일반 이동) 분기에도 동일한 2줄 방어를 넣는다.

### 1-G. 리셋/정리 지점에 플래그 클리어 추가
- `:522` 부근(배치 준비 초기화, `_inputVisionPickerEntryTargetPrepared = false;` 옆): `_inputVisionRetreatDeferred = false;`
- `ObserveInputVisionRetreatMoveTaskOnAbort` (`:1318`): 첫 줄에 `_inputVisionRetreatDeferred = false;`
- 기존 `:2249` join(피커 X 진입 완료 후)은 무변경 — 이연이 소비된 뒤이므로 그대로 동작한다.

### 1-H. 무변경 확인 대상
- `RunInputVisionRetreatMoveAsync`(`:1248`) 본체, 회피 좌표 계산(`TryResolveMinimalVisionRetreatTarget` 호출부),
  `TryFollowPickerXBehindInputVisionRetreatAsync`(`:1409`) — 전부 그대로.
- 주석 스타일: 기존 관례대로 "기존 조건 / 현재 기준" 형식으로 변경 사유를 남긴다
  (#17 프롬프트 R3의 '회피 스텝에서 비동기 시작' 지시를 사용자 지시(2026-07-25)로 대체함을 명시).

---

# 작업 2 — 비전 복귀: 다음 촬영 위치로 바로 가지 않고 중간 위치에 들렀다 감

## 증상 (실장비)
피커가 회피(퇴장)할 때 InputVisionX가 따라 들어가는데(return-follow #18), **다음 촬영 위치로
직행하지 않고 엉뚱한 중간 위치까지 갔다가 멈춰 있다가, 나중에 다시 촬영 위치로 이동**한다.

## 원인 (코드 확정 — 설계된 동작이 증상 그 자체)
`InputDieVisionPrepareSequence.cs:2327`의 follow 진입 판정 `ShouldFollowPickerForInputVisionEntry`
(`:2506`)는 "촬영 목표가 피커 페어 간격을 아직 못 만족하면" **피커의 상태와 무관하게 무조건**
follow를 시작한다(#18 프롬프트 R2: "판단 로직 없이 항상 follow 시도, 정지 선행축이어도 시작" —
당시 확정 사양). FollowMoveAsync는 선행 피커가 **정지해 있어도** 현재 간격의 여유만큼
후행축을 전진시키므로:

- 피커가 아직 존 안에서 **작업 중**(픽 이동/정지)일 때 비전 이동 차례가 오면,
  비전이 진입 한계(= 제약피커 + HomeClearance − safetyGap, 예: 658.5)까지 **먼저 전진해 정지**하고
  피커가 실제로 퇴장할 때까지 그 자리에서 대기한다.
- 피커가 퇴장하면 그제서야 나머지를 이동해 촬영 위치(예: 680)에 도달한다.

이 "한계 위치 선진입 → 대기 → 재이동"이 사용자가 본 "다른데로 갔다가 다시 촬영 위치로 온다"이다.
(#17 체크리스트의 S1 하네스 케이스 "정지 선행축 — 한계 658.5 대기 후 외부 기동 시 680 도달"이
정확히 이 동작을 검증하고 있다 — 즉 구현 결함이 아니라 사양 R2의 실기 결과이며, 사용자가
2026-07-25 이 동작의 변경을 지시했다. **이 지시가 #18 R2를 대체한다.**)

## 수정 — "제약 피커의 퇴장 명령이 촬영 목표를 열어줄 때만" follow, 그 전에는 대기

원칙: 비전은 **촬영 위치까지 한 번에 도달 가능해질 때만** 출발한다.
- 제약 피커의 **CommandPosition**(이미 발행된 이동 목표)이 촬영 목표에 대해 follow 안전간격
  (safetyGap)을 만족하면 → 피커가 퇴장 중이라는 뜻 → follow로 추종 진입(오버랩 유지).
- 그렇지 않으면(피커 정지/작업 중) → **출발하지 않고 대기**. 촬영 목표가 페어 간격을 이미
  만족해지면(존이 빈 상태) 기존 일반 이동으로 직행.

### 2-A. 기회 대기 헬퍼 신설
`ShouldFollowPickerForInputVisionEntry`(`:2506`)를 아래 헬퍼로 대체·확장한다
(기존 clearance 판정 코드를 (a)로 재사용):

```csharp
        // 기존 조건(#18 R2): 목표가 페어 간격 미충족이면 정지 선행축이어도 항상 follow —
        //   비전이 진입 한계까지 선진입 후 대기해 "중간 위치에 들렀다 가는" 동작이 됐다.
        // 현재 기준(사용자 지시 2026-07-25): 촬영 목표까지 한 번에 도달 가능할 때만 출발한다.
        //   (a) 목표가 양 피커 Actual/Command 페어 간격을 이미 만족 → false(일반 이동 직행)
        //   (b) 제약 피커의 Command가 목표를 safetyGap까지 열어줌(퇴장 명령 발행됨)
        //       그리고 반대 피커 Command 기준 목표 간격 충족 → true(follow 추종 진입)
        //   (c) 둘 다 아니면 폴링 대기. 타임아웃 규칙은 기존 존 대기와 동일
        //       (선행검사 모드 무한대기+주기 로그, 그 외 ResolveTimeout 후 일반 경로 위임).
        private async Task<bool> WaitInputVisionReturnFollowOpportunityAsync(
            InputStageUnit stage, double target, string description, CancellationToken ct)
```

구현 요건:
1. 게이트: `IsVisionReturnFollowGateSatisfied()`(`:2383`) false 또는 `stage.CameraX as AjinAxis` null
   또는 service null이면 즉시 `false` (기존과 동일하게 일반 경로).
2. 루프 본문:
   - `ct.ThrowIfCancellationRequested()` + `Context.StopIfCycleStopRequested(...)`
     (기존 `WaitInputStagePlaneGuardClearBeforeVisionOverlapAsync` `:2406`의 관례 미러).
   - **(a)** 기존 `ShouldFollowPickerForInputVisionEntry`의 판정 그대로: Front/Rear 양 피커의
     Actual/Command 4판정(`service.IsPairClearanceSatisfied`)을 목표가 전부 만족하면 → `return false;`
   - **(b)** `ResolveConstrainingPickerXForInputVisionEntry()`(`:2540`)로 제약 피커 선정 →
     `service.TryGetFollowGapParameters(stage.CameraX, 제약피커, InputExtra, out direction/homeGap/safetyGap/detail)`
     (`:2582` 기존 호출과 동일 인자). 조회 실패 시 → `return false;` (기존과 동일하게 일반 경로).
     간격 계산 (FollowMoveAsync `:629`의 페어식과 동일, 부호는 direction으로):
     ```csharp
     double leadingCommand = constrainingPickerX.CommandPosition;
     double gapAtCommand = direction > 0
         ? (leadingCommand + homeGap) - target
         : (target + homeGap) - leadingCommand;
     bool leadingOpensTarget = gapAtCommand + 0.000001 >= safetyGap;
     ```
     반대(비제약) 피커는 `service.IsPairClearanceSatisfied(반대피커, 반대피커.CommandPosition, stage.CameraX, target, out _)`
     한 가지만 확인한다(Actual은 잔여 이동으로 곧 열리며, 위반 시도는 follow 내부 MotionGuard가
     -11로 거부 → 기존 R5 폴백 안전망이 받는다 — 이 근거를 주석으로 명시).
     `leadingOpensTarget && 반대피커 Command 충족`이면 → `return true;`
   - **(c)** `await Task.Delay(20, ct).ConfigureAwait(false);` 후 재판정.
3. 타임아웃: `ResolveTimeout()` 기준. `IsInputCameraPreInspectionMode()`면 기존 `:2464~2474`처럼
   주기 로그를 남기고 무한 대기(start 리셋 후 continue). 그 외에는 타임아웃 시 `return false;`
   (일반 이동 경로가 자체 대기/인터락/실패 처리를 갖고 있으므로 거기에 위임 — 신규 Fail 코드를
   만들지 않는다).
4. 최초 대기 진입 시 1회 Wait 로그, 해소 시 Ok 로그 (기존 `:2450~2458` 관례 미러).

### 2-B. 호출부 교체 — `MoveInputVisionXAndVerifyAsync` (`:2311`)
`:2327`의 `if (ShouldFollowPickerForInputVisionEntry(stage, target))` 를
```csharp
                    if (await WaitInputVisionReturnFollowOpportunityAsync(stage, target, description, ct).ConfigureAwait(false))
```
로 교체한다. true면 기존 `TryFollowInputVisionXBehindPickerAsync`(`:2565`) 호출 + 성공 시 Check,
실패 시 기존 폴백 로그 + 일반 이동 — **이 아래쪽 코드는 무변경**.

### 2-C. 정리
- `ShouldFollowPickerForInputVisionEntry`는 (a) 판정용 private 헬퍼로 남기거나 새 헬퍼에 흡수한다
  — 어느 쪽이든 참조가 깨지지 않게. (`grep ShouldFollowPickerForInputVisionEntry`로 잔여 참조 0 확인)
- `TryFollowInputVisionXBehindPickerAsync` / `ResolveConstrainingPickerXForInputVisionEntry` /
  `IsVisionReturnFollowGateSatisfied` 본체는 무변경.
- 주석에 "기존 조건/현재 기준" 형식으로 #18 R2 대체(사용자 지시 2026-07-25)를 명시한다.

---

## 변경 금지 (두 작업 공통)
- `AjinAxis.cs` 전체 (FollowMoveAsync, WaitUntilMoveDone, TryOverridePosition 포함).
- 인터락 파일 전체 (`Equipment\Interlocks\*`) — diff 0건이어야 한다. `BeginMotionGuardBypass` 사용 금지.
- `SharedRailXMotionService` / `SharedRailXConfig` / 설정 스토어 — `TryGetFollowGapParameters` 등
  기존 API를 호출만 하고 수정하지 않는다.
- `#16` 최소 회피 좌표 계산(`TryResolveMinimalVisionRetreatTarget` 및 호출부 계산 로직).
- Output 측(PlaceSequence의 OutputVision 회피 이연, OutputPostPlaceInspectionQueue의 return-follow) —
  같은 패턴이 존재하지만 이번 범위 아님. 손대지 말 것.
- 비Conti/수동/캘리브레이션 경로, 레시피, UI.

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과.
2. `git diff --stat`이 정확히 2개 파일만 표시:
   `PickerPickUpSequence.cs`, `InputDieVisionPrepareSequence.cs`.
3. 인터락 폴더 diff 0건: `git diff --stat -- QMC.CDT-320/Equipment/Interlocks` 빈 출력.
4. `grep -n "BeginMotionGuardBypass" QMC.CDT-320/Sequencing/Picker/*.cs` → 0건.
5. 작업 1 논리 확인: 이연 플래그가 설정되는 경로에서 (i) follow 경로는
   `StartDeferredInputVisionRetreatIfPendingAsync` → `FollowMoveAsync` 순서로 호출되는지,
   (ii) 일반 이동 경로는 이연 회피 시작+join 후에만 X 명령이 나가는지, (iii) 배치 초기화/Abort에서
   플래그가 지워지는지 — 세 가지를 코드로 짚어 보고서에 명시한다.
6. 작업 2 논리 확인: (a)/(b)/(c) 세 분기 각각의 반환/대기 경로와 타임아웃(선행검사 무한대기 포함)을
   코드로 짚어 보고서에 명시한다.

## 실장비/시뮬 검증 기준 (작업자는 시뮬 모드로 확인, 실장비는 사용자가 확인)
작업 1:
- Auto Conti/FastConti 픽업 로그에 아래 순서가 나타난다:
  `"InputVisionX 최소 회피 이동을 피커 X 진입 시점까지 이연합니다"` →
  `"InputVisionX 이연 최소 회피 이동을 비동기 시작했습니다"` ≈
  `"PickUp 피커X 팔로잉 진입을 시작합니다"` (수 ms 이내) →
  `AX-FOLLOW-MOVE ... 팔로잉 최초 이동 명령을 발행했습니다`.
- `AxisMoveProfile InputVisionX ABS MOVE - Start`와 피커X follow 시작 로그의 타임스탬프가 겹친다
  (기존: 비전 회피 완료 후 피커 X Start).
- 배치 2번째 픽부터는 이연/팔로잉 없이 기존 일반 이동(비전 이미 회피 위치).
- 인터락 위반/알람 0건, `-11`/`-5` 미발생.

작업 2:
- 피커가 존 안에서 작업 중일 때 비전 이동 차례가 와도 InputVisionX의 `AxisMoveProfile ABS MOVE`가
  **발행되지 않고** `- Wait` 로그로 대기한다 (기존: 한계 위치로 선진입).
- 피커 퇴장 명령이 발행된 직후 `"VisionX 팔로잉 진입을 시작합니다"` → 비전이 촬영 위치까지
  중간 정지 없이 연속 추종(오버라이드 연장) → 도달.
- 존이 이미 빈 경우(2번째 이후 촬영) follow 없이 일반 이동으로 직행 — 기존과 동일.
- follow 실패(-21/-11) 시 기존 폴백 로그 후 일반 이동으로 정상 완료.

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. Output 측 동일 패턴 2곳: PickerPlaceSequence의 OutputVision 회피 비동기 시작(`:1810`)도 선행
   모션에 오버랩을 잠식당하는 구조이고, OutputPostPlaceInspectionQueue의 return-follow도 정지
   선행축 선진입 동작을 갖는다. Input 검증 후 별도 지시로 미러 적용 예정.
2. 작업 1 이연 후에도 픽 배치 첫 픽에서 비전 회피 거리가 피커 X 진입 거리보다 훨씬 길면
   follow 타임아웃(`VisionFollowEntryTimeoutMs`, 기본 15000ms) 안에서 완주 못 할 가능성이
   이론상 있으나, 최소 회피(#16)가 회피 거리를 수 mm~수십 mm로 줄이므로 실질 위험은 낮다.
