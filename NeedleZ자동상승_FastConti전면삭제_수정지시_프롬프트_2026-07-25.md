# CDT-320: (1) 픽업 NeedleZ 미상승 자동 상승 (2) FastContiSegmentedPickUp 전면 삭제

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.

**작업 순서를 반드시 지킨다: 작업 1 → 빌드 확인 → 작업 2.**
작업 2가 작업 1에서 참고하는 기존 Fast 헬퍼를 삭제하므로, 순서를 바꾸면 참조가 끊긴다.

수정 파일:
- 작업 1: `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs`
- 작업 2: `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs`,
  `QMC.CDT-320\Equipment\Unit\Common\PickerTransferTypes.cs`,
  `QMC.CDT-320\Sequencing\Picker\InputCameraMarkInspectionSequence.cs`,
  `QMC.CDT-320\Sequencing\Picker\InputDieVisionPrepareSequence.cs`,
  `QMC.CDT-320\Sequencing\Picker\PickerSequenceBase.cs`(주석만)

---

# ★ 작업 규칙 (필수 — 위반 시 작업 중단하고 사용자에게 보고)

### 규칙 1. 공정 속도 구간에는 스케일 미적용 코드를 작성하지 않는다
Auto 운전에서 `Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`을 보드로
내려보내는 경로는 반드시 `MotionSpeedScale.ApplyDefaultVelocityScale` /
`ApplyDefaultAccelerationScale`을 통과한 값만 사용한다. Config 날값 직접 전달 금지.

### 규칙 2. 속도·가속·감속은 항상 같은 배율로 함께 스케일한다
속도만 스케일하고 가감속을 100%로 남기는 코드 금지. 전례: 커밋 `a690f6a5`,
2026-07-25 RearPickerX 폭주(팔로잉 최초 명령 가감속 20배).
**작업 1은 기존 `MoveInputStageAxisCommandAsync`(유닛 기본 이동 = 축 레이어에서 스케일 적용)를
그대로 재사용한다. 속도/가감속을 새로 계산하지 말 것.**

### 규칙 3. 스케일을 적용하지 않아야 하는 값과 혼동하지 않는다
명시 velocity(`velocity > 0`), Jog 속도, ScanVelocity, HomeVelocity에는 적용하지 않는다
(`MotionSpeedScale.cs:10` 규약). 이중 스케일도 결함이다.

### 규칙 4. 범위 밖에서 스케일 미적용 지점을 발견하면 고치지 말고 보고한다

### ★ 규칙 5. 보고 의무 (반드시 이행 — 하나라도 빠지면 작업 미완료)
1. 변경/삭제 지점 목록 (`파일:행` + 내용). 삭제는 **삭제한 멤버 이름 전체**를 나열한다.
2. 스케일 미적용 잔존 지점 감사 결과
   (`grep -rn "Config\.DefaultVelocity\|Config\.Acceleration\|Config\.Deceleration" --include=*.cs QMC.CDT-320 QMC.Common`).
   없으면 "없음" 명시.
3. 각 작업의 **[확인 요청]** 답변.
4. 판단이 애매해서 손대지 않은 지점. 없으면 "없음" 명시.

---

# 작업 1 — 픽업 NeedleZ가 미상승이면 상승시킨 뒤 진행

## 배경 — 실장비 확정 (2026-07-25 18:17)

Front 피커 픽업에서 **NeedleZ가 0(Avoid)인 상태로 픽업이 진행되어 다이가 픽업되지 않았다.**

```
18:16:44  MachineReadySequence > InputStage NeedleZ Avoid 이동 시작. target=0     ← Ready가 0으로 내림
18:17:09  PickUp 피커 이동 전 Z축 안전 복귀 - 공정 중 NeedleZ는 현재 위치를 유지하고 ...
18:17:09  PickUp 피커 이동 전 Z축 안전 복귀 완료. needleZActual(유지)=0.000000     ← 0인데 그대로 진행
18:17:19  PickerPickTargetVerify > ... / needleZ = 182.500000                      ← 목표는 182.5 정상
18:17:19  picker axis move complete. description=PickUp 단순 PickerZ 하강           ← Simple 경로
```
해당 구간 로그에 `AxisMoveProfile > NeedleZ`가 **0건** — NeedleZ 이동 명령이 아예 나가지 않았다.

원인: `RunPickupZMotionAsync`(`:6141`)에서
```csharp
if (config.MotionMode == PickerPickUpZMotionMode.SimpleZDownVacuumUp)
    return await RunSimplePickupZMotionAsync(...);        // ← 여기서 즉시 반환
int result = await PrepareNeedlePinZForPickAsync(ct);      // ← NeedleZ 상승이 이 뒤
```
`SimpleZDownVacuumUp` 경로가 `PrepareNeedlePinZForPickAsync`(`:6420`, NeedleZ 상승 담당)에 도달하지
못한다. 그리고 픽업 진입 단계는 "NeedleZ는 이미 픽업 높이에 있다"고 가정해 건드리지 않는다.
장비 설정은 `PickerFrontUnit.json` `PickUp.MotionMode: 1`(= SimpleZDownVacuumUp)이었다.

참고 — 다른 경로는 이미 보호된다:
- **Detailed**: `PrepareNeedlePinZForPickAsync`가 상승 수행.
- **Conti**: 적격 가드 `CanUseContiSegmentedPickUpNodesFromCurrentPosition`(`:4031` 부근)가
  "NeedleZ가 픽업 목표 높이가 아니면" Conti를 거부하고 Default로 폴백 → Default가 상승시킨다.
- **Fast**: `EnsureFastNeedleZAtPickTargetAsync`가 상승 수행 (작업 2에서 삭제 예정).

즉 **Simple 경로만 무방비**다.

## 수정 1-A. NeedleZ 픽업 목표 보장 헬퍼 신설

`PrepareNeedlePinZForPickAsync`(`:6420`) **바로 위**에 아래 메서드를 추가한다.
(작업 2에서 삭제되는 `EnsureFastNeedleZAtPickTargetAsync`(`:3394`)와 동일 패턴이지만
Fast 전용 명칭·알람코드·로그를 쓰지 않는 **독립 구현**이다. Fast 메서드를 이름만 바꿔 옮기지 말고
아래 코드대로 새로 작성한다.)

```csharp
        /// <summary>
        /// NeedleZ가 픽업 목표 높이(_targetNeedleZ)에 있는지 확인하고, 미달이면 목표까지 동기 상승한다.
        /// 기존 조건: 픽업 진입 단계는 "공정 중 NeedleZ는 현재 위치 유지"를 전제해 NeedleZ를 건드리지
        ///           않고, 상승은 PrepareNeedlePinZForPickAsync만 담당했다. 그래서
        ///           SimpleZDownVacuumUp 경로는 상승 없이 픽업을 진행해 다이를 픽업하지 못했다
        ///           (실장비 2026-07-25 18:17, needleZActual=0 / 목표 182.5).
        /// 현재 기준(사용자 지시 2026-07-25): 미상승을 감지하면 상승시킨 뒤 진행한다.
        /// 속도/가감속은 유닛 기본 이동 경로(MoveInputStageAxisCommandAsync)를 그대로 쓴다 —
        /// 축 레이어에서 MotionSpeedScale이 1회 적용되므로 여기서 별도 계산하지 않는다(작업 규칙 2).
        /// EjectPinZ와 Needle Vacuum은 이 메서드가 다루지 않는다(각 경로의 기존 처리를 유지).
        /// </summary>
        private async Task<int> EnsureNeedleZAtPickTargetAsync(InputStageUnit stage, CancellationToken ct)
        {
            BaseAxis needleZ = ResolveInputStageAxis(stage, WaferStageAxis.NeedleZ);
            if (needleZ == null)
                return Fail("PICKER-PICKUP-NEEDLEZ-AXIS", Name,
                    "PickUp NeedleZ 축을 찾을 수 없습니다.");

            if (double.IsNaN(_targetNeedleZ) || double.IsInfinity(_targetNeedleZ))
                return Fail("PICKER-PICKUP-NEEDLEZ-TARGET", Name,
                    "PickUp NeedleZ 픽업 목표가 유효하지 않습니다. target=" + _targetNeedleZ);

            if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ))
            {
                WriteLog("PickerPickUpZ",
                    Name + " PickUp NeedleZ teaching 유지. " +
                    BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) + " - Ok");
                return 0;
            }

            WriteLog("PickerPickUpZ",
                Name + " PickUp NeedleZ 픽업 준비 상승 시작(미상승 상태 감지). " +
                BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) + " - Start");

            int moveResult = await MoveInputStageAxisCommandAsync(
                stage,
                WaferStageAxis.NeedleZ,
                _targetNeedleZ,
                "PickUp NeedleZ 픽업 준비 위치(미상승 보정)",
                ct).ConfigureAwait(false);
            if (moveResult != 0)
                return moveResult;

            return CheckInputStageAxisInPosition(
                stage,
                WaferStageAxis.NeedleZ,
                _targetNeedleZ,
                "PickUp NeedleZ 픽업 준비 위치(미상승 보정)");
        }
```

## 수정 1-B. Simple 경로 진입 직후 호출

`RunSimplePickupZMotionAsync`(`:6279`)의 `ct.ThrowIfCancellationRequested();` **직후**,
`"PickUp 단순 PickerZ 하강"` 이동 **전**에 삽입한다. **PickerZ가 내려가기 전에 니들이 올라와야
하므로 순서를 반드시 지킨다.**

```csharp
                ct.ThrowIfCancellationRequested();

                // 사용자 지시(2026-07-25): NeedleZ 미상승이면 상승시킨 뒤 진행한다.
                // PickerZ 하강보다 반드시 앞에서 수행한다.
                InputStageUnit needleStage = ResolveInputStage();
                if (needleStage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                int needleZReady = await EnsureNeedleZAtPickTargetAsync(needleStage, ct).ConfigureAwait(false);
                if (needleZReady != 0)
                    return needleZReady;

                int result = await MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    _targetPickerZ,
                    "PickUp 단순 PickerZ 하강",
                    ct,
                    ...
```
- 메서드 뒤쪽(`:6307` 부근)에 이미 `InputStageUnit stage = ResolveInputStage();`가 있다.
  **변수명 충돌이 나지 않게** 위 삽입분은 `needleStage`로 두고, 기존 코드는 건드리지 않는다.

## 무변경 (작업 1)
- `PrepareNeedlePinZForPickAsync`(`:6420`) 본문 — Detailed 경로는 그대로.
- Conti 적격 가드(`:4031` 부근)의 NeedleZ 판정 — 폴백으로 이미 보호되므로 그대로.
- `RunPickupZMotionAsync`(`:6141`)의 분기 구조 — 그대로.
- `contiContactFlow`(`RunPickupZMotionAfterContiContactAsync`) 경로 — [확인 요청 1] 참조.
- EjectPinZ / Needle Vacuum 관련 기존 처리 전부.

## [확인 요청 1] — Conti 접촉 후 경로에 상승을 넣지 않은 이유 (확인·보고)
`contiContactFlow == true`(PickerZ가 Conti 이송 중 이미 다이에 접촉한 상태)에서는
**니들을 올리지 않는다**. 접촉 후에 니들을 올리면 이젝트 순서가 설계와 달라지기 때문이다.
이 경로는 Conti 적격 가드가 "NeedleZ 미상승이면 Conti 거부 → Default 폴백"으로 이미 막는다.
- 위 폴백 경로가 실제로 성립하는지(가드 → Default → `PrepareNeedlePinZForPickAsync` 상승) 코드로
  짚어 보고하라. 성립하지 않는 구멍이 있으면 **고치지 말고 보고**한다.

## 작업 1 검증 기준
1. 빌드 통과.
2. `PickUp.MotionMode = SimpleZDownVacuumUp`, NeedleZ를 0(Avoid)에 둔 상태로 픽업 1회:
   - `PickUp NeedleZ 픽업 준비 상승 시작(미상승 상태 감지)` 로그가 나온다.
   - `AxisMoveProfile > NeedleZ ABS MOVE. target=<픽업 목표>` 가 발행되고
     `defaultScaleApplied=True`, `acc`/`dec`가 `scalePercent`에 맞게 스케일되어 나온다.
   - 그 **뒤에** `PickUp 단순 PickerZ 하강`이 실행된다(순서 역전 없음).
3. NeedleZ가 이미 목표 높이면 `PickUp NeedleZ teaching 유지`만 나오고 이동 명령은 없다(회귀 없음).
4. `PickUp.MotionMode = Detailed` 픽업은 기존과 동일하게 동작한다.

---

# 작업 2 — FastContiSegmentedPickUp 전면 삭제

미사용 모드다(사용자 확인 2026-07-25). 코드·설정·enum을 모두 제거한다.

## 2-A. `PickerPickUpSequence.cs`

### A-1. Fast 전용 region 전체 삭제
`#region FastContiSegmentedPickUp (고속 픽업 모드)`(`:2792`) ~ 대응 `#endregion`(`:3830`) **전체 삭제.**
포함 멤버 14개(모두 삭제 대상):
`MovePickerXStageYPickerTByFastContiSegmentedPickUpAsync`, `RunFastContiSegmentedPickUpCycleAsync`,
`EnsureFastPickerZAxesSafeAsync`, `EnsureFastEjectPinZAtProcessAsync`,
`EnsureFastNeedleZAtPickTargetAsync`, `RunFastStageNeedleTransferAndVacuumOnAsync`,
`WaitFastPickerXApproachAsync`, `MoveFastPickerZToPickWithSlowZoneAsync`,
`WaitFastPickerZStageSafeAsync`, `ResolveFastAxisVelocity`, `ResolveFastMoveAcceleration`,
`ResolveFastOverrideAcceleration`, `IsOwnPickerYForwardForFastEntry`, `ObserveFastMoveTaskAsync`
- `#endregion` 행 번호는 삭제 전 반드시 재확인한다(작업 1로 행이 밀린다).
  `#region FastContiSegmentedPickUp`과 **짝이 맞는** `#endregion`까지만 지운다.

### A-2. 분기 판정·호출 제거 (`MovePickerXStageYPickerTAsync`, `:2230` 부근)
- `useFastContiTransfer` 지역 변수 선언(`:2239` 부근) 삭제.
- `if (useFastContiTransfer) { branchResult = await MovePickerXStageYPickerTByFastContiSegmentedPickUpAsync(...); }`
  분기(`:2281` 부근) 삭제 → `useContiTransfer` / else(Default) 2분기만 남긴다.
- **`if (!useFastContiTransfer)`로 감싸인 Z Avoid 선행 복귀(`:2247~2255` 부근)는 조건을 제거하고
  무조건 실행되게 한다.** Fast만 이 선행 복귀를 건너뛰었으므로, Fast가 없어지면 항상 수행이 맞다.
  주변 주석("Fast 모드는 자체 PickerZ 안전 확인을 수행하므로 …")도 함께 삭제한다.

### A-3. 최소 회피 게이트 조건 정리 (`:1123~1131` 부근)
```csharp
                bool useMinimalRetreat =
                    Options != null && Options.RunMode == SequenceRunMode.Auto &&
                    retreatPickUpConfig != null &&
                    (IsCoordinatedPickUpTransferMotionMode(retreatPickUpConfig.TransferMotionMode) ||
                     retreatPickUpConfig.TransferMotionMode == PickerPickUpTransferMotionMode.FastContiSegmentedPickUp);
```
→ `FastContiSegmentedPickUp` 항을 제거하고 `IsCoordinatedPickUpTransferMotionMode(...)`만 남긴다.
괄호도 정리한다. 주석의 "ContiSegmentedPickUp/FastContiSegmentedPickUp" 표기도 수정한다.

### A-4. 잔여 Fast 참조 전수 제거
```bash
grep -n "Fast" QMC.CDT-320/Sequencing/Picker/PickerPickUpSequence.cs
```
결과가 **0건**이 되어야 한다. 주석·로그 문구 포함.

## 2-B. `PickerTransferTypes.cs`

### B-1. enum 멤버 삭제
```csharp
    public enum PickerPickUpTransferMotionMode
    {
        Default = 0,
        ContiSegmentedPickUp = 2
    }
```
`FastContiSegmentedPickUp = 3`과 그 위 주석("// 고속 픽업 모드 — …") 삭제.
**`Default = 0` / `ContiSegmentedPickUp = 2`의 숫자값은 절대 바꾸지 말 것** (저장된 JSON과 호환).

### B-2. Fast 전용 설정 3개만 삭제 (`:91~95` 부근)
```csharp
        [DataMember] public double FastPickerZSafePosition { get; set; } = -3.0;
        [DataMember] public double FastPickerXApproachDistance { get; set; } = 20.0;
        [DataMember] public double FastContactSlowZoneDistance { get; set; } = 0.3;
```
이 3개와 이들에 붙은 주석만 삭제한다.

> ⚠ **함정 — 삭제 금지 필드.** `:89`의 `// FastContiSegmentedPickUp 전용 설정.` 주석 **아래로
> 이어지는 나머지 필드들은 Fast 전용이 아니다.** 아래는 Detailed 경로가 쓰거나 Normalize 마이그레이션
> 대상이므로 **절대 삭제하지 말 것**:
> `PickerZSlowApproachVelocity/Acceleration/Deceleration`, `SyncLiftDistance`, `SyncLiftSpeedPercent`,
> `NeedleSeparateDistance`, `PickerSeparateDistance`, `SeparateSpeedPercent`,
> `PickerZSeparateVelocity/Acceleration/Deceleration`,
> 그리고 `TransferConti*` 전체(Conti가 사용), `NeedleVacuumOffSettleBeforeXYMs`,
> `SyncLiftSettleMs`, `PickSettleMs`.
> `:89` 주석은 Fast 3개 필드에만 걸리도록 위치·문구를 정리한다(오해 재발 방지).

### B-3. `Normalize()` 폴백 조건 (`:151~156` 부근)
```csharp
            if (TransferMotionMode != PickerPickUpTransferMotionMode.Default &&
                TransferMotionMode != PickerPickUpTransferMotionMode.ContiSegmentedPickUp &&
                TransferMotionMode != PickerPickUpTransferMotionMode.FastContiSegmentedPickUp)
            {
                TransferMotionMode = PickerPickUpTransferMotionMode.Default;
            }
```
→ `FastContiSegmentedPickUp` 조건줄만 삭제. 결과적으로 **저장된 값 3은 Default로 정규화**된다.
주석으로 명시할 것: "기존 값 3(FastContiSegmentedPickUp, 삭제됨)은 Default로 정규화한다."

## 2-C. 게이트 조건 2곳
- `InputCameraMarkInspectionSequence.cs:667` 부근 — `IsMinimalRetreatGateSatisfied`의
  `|| config.TransferMotionMode == PickerPickUpTransferMotionMode.FastContiSegmentedPickUp` 제거.
  `:651` 부근 주석의 Fast 표기도 수정.
- `InputDieVisionPrepareSequence.cs:2400` 부근 — `IsVisionReturnFollowGateSatisfied`의 동일 조건 제거.

**두 게이트의 나머지 조건(`Auto` + `ContiSegmentedPickUp`)은 그대로 유지한다.**

## 2-D. 주석만 정리
- `PickerSequenceBase.cs:2926` 부근 — "FastContiSegmentedPickUp 경로 전용" 주석 문구 수정.
  **해당 메서드 코드는 건드리지 말 것** (Conti/Default가 쓰는지 먼저 확인하고, 쓰이면 주석만 수정,
  참조가 완전히 사라졌으면 [확인 요청 2]로 보고).

## ⚠ 삭제 금지 — Fast로 오인하기 쉬운 무관 코드
아래는 FastContiSegmentedPickUp과 **무관**하다. grep으로 일괄 삭제하다 함께 지우지 말 것:
- `QMC.CDT-320\Equipment\Materials\MaterialSnapshotStore.cs` — `TryLoadPrimarySnapshotFast`,
  `LogLoadElapsed("Fast", …)` (스냅샷 빠른 로드)
- `QMC.Common\Ui\Vision\CameraViewBase.cs` — `DrawFrameFast` (화면 렌더링)
- `QMC.CDT-320\Program.cs` — `Environment.FailFast` (.NET API)
- `QMC.CDT-320\Sequencing\Picker\PickerPlaceSequence.cs` — `MovePickerToAvoidAfterPlaceFastAsync`
  (플레이스 후 회피, Fast 픽업과 무관)
- `PickerPickUpMotionConfig.TransferContiTimeoutMs` (Conti 공용)
- `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs:1166`, `QMC.Common\Motion\BaseAxis.cs:586`의
  `To do:` 주석 — 두 파일은 **이번 작업에서 수정 금지**. FastConti 언급이 남는 것을 보고만 한다.

## [확인 요청 2] — 삭제로 미사용이 된 멤버 (보고 후 대기)
Fast region 삭제로 **다른 곳에서 더 이상 호출되지 않는 메서드/필드**가 생기면, 목록만 보고하고
**삭제하지 말 것.** 특히 `PickerSequenceBase`의 Fast 전용이라고 주석된 메서드, Conti와 공유하던
헬퍼가 대상이다. 사용자 승인 후 별도 작업으로 처리한다.

## 작업 2 검증 기준
1. `QMC.CDT-320.sln` 빌드 통과 — 경고 신규 발생 0건(특히 미사용 변수/도달 불가 코드).
2. `grep -rn "FastConti\|FastPickerZSafePosition\|FastPickerXApproachDistance\|FastContactSlowZoneDistance" --include=*.cs QMC.CDT-320 QMC.Common`
   → `AjinAxis.cs` / `BaseAxis.cs`의 `To do:` 주석 2건 **외에는 0건**.
3. `grep -n "Fast" QMC.CDT-320/Sequencing/Picker/PickerPickUpSequence.cs` → 0건.
4. RECIPE 화면 `PICKUP TRANSFER MOTION MODE` 드롭다운에 **`Default` / `ContiSegmentedPickUp` 2개만**
   표시된다(`FrontPickerRecipePage.cs:422`, `RearPickerRecipePage.cs:448`의
   `Selection<PickerPickUpTransferMotionMode>` — **코드 수정 없이** enum 변경만으로 반영되어야 한다.
   UI 파일을 고쳐야 한다면 그 사실을 보고하고 승인 요청).
5. **마이그레이션 검증(필수)**: `PickUp.TransferMotionMode`가 `3`으로 저장된 JSON을 로드했을 때
   예외 없이 `Default`로 정규화되는지 확인한다. 현재 실장비 설정은 `0`이지만 백업·타 호기에 `3`이
   남아 있을 수 있다. 역직렬화 자체가 실패하면 **즉시 보고**하고 처리 방안을 질의한다.
   (테스트용 JSON은 스크래치 폴더에 만들고 `D:\CDT-320\EquipmentData`는 건드리지 말 것.)
6. 시뮬: `TransferMotionMode = ContiSegmentedPickUp`과 `Default` 각각 픽업 1배치가 기존과 동일하게
   완주하는지 확인(Conti/Default 회귀 없음).

---

## 변경 금지 (공통)
- 인터락 파일 전체(`Equipment\Interlocks\*`) — diff 0건. `BeginMotionGuardBypass` 사용 금지.
- `AjinAxis.cs`, `QMC.Common\Motion\*`, `QMC.Common\AjinE\*`, `SharedRailX*` — diff 0건.
- `PickerPlaceSequence.cs`, `OutputPostPlaceInspectionQueue.cs`,
  `InputVisionXPrePositionCoordinator.cs` — diff 0건.
- UI 파일 — 검증 4에서 수정이 필요해 보이면 **보고 후 승인** 받고 진행.
- `D:\CDT-320\*`(실장비 배포/설정/로그) — 어떤 파일도 수정하지 말 것.
- `Config\interlock-check-matrix.json`, `motion_axes.json`, 레시피 데이터.
- 팔로잉/이연 관련 로직(`FollowMoveAsync` 호출부, `_inputVisionRetreatDeferred` 등) — 그대로.

## 작업 후 확인 사항
1. 빌드 통과 + 신규 경고 0건.
2. `git diff --stat`이 위 "수정 파일" 목록을 넘지 않는지.
3. 작업 1의 삽입 위치가 `PickerZ 하강` **앞**인지 (순서 검증).
4. 작업 2 검증 2/3의 grep 결과 첨부.
5. 규칙 5의 보고 항목 4개 + [확인 요청 1]/[확인 요청 2] 답변.

## 실장비 검증 (사용자 실행 — 작업자는 문서로만 제공)
1. `PickUp.MotionMode`를 현행 `SimpleZDownVacuumUp`으로 두고 NeedleZ가 0인 상태에서 픽업 1회 →
   `PickUp NeedleZ 픽업 준비 상승 시작(미상승 상태 감지)` → NeedleZ 182.5 도달 → 픽업 성공 확인.
2. `PICKUP TRANSFER MOTION MODE` 드롭다운에 Fast 항목이 사라졌는지 확인.
3. 다이 픽업 성공률을 1배치 확인. 실패가 남으면 그 시점 로그를 보존해 보고.

> 참고: `PickUp.MotionMode`를 `Detailed`로 바꾸는 것이 정공법이라는 별도 제안이 있었으나,
> 사용자 지시는 **"미상승이면 상승시킨 뒤 동작"** 이다. 이 작업은 지시대로 자동 상승을 구현하며,
> 모드 설정 변경은 사용자 판단 사항이므로 **코드/데이터로 강제하지 않는다.**
