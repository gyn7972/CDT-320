# [수정 지시] Pick·Place 최종 목표 X/Y/T — CanSkipPickerMoveCommand 스킵 제거(미소 보정 강제 발행)

작업 대상: `D:\Source\CDT-320_New`
지시자: 팀장님 (2026-08-18 지시 — **편집은 변경 목록 고지·승인 후**)
선행 분석: 2026-08-18 세션 — Place 클로즈루프 T축 보상 미반영(Front 픽커2 −0.4° 고정) 원인 추적.
Pick/Place 최종 목표 접근 이동의 스킵 발생 지점 전수 조사 완료(아래 변경 범위가 그 결과다).

---

## 0. 이 작업의 규칙 (반드시 준수)

1. **시킨 것만 한다.** 아래 "변경 범위" 14곳 외에는 손대지 말 것. 걸림돌이 생기면
   임의로 해결하지 말고 **"문제 있다, 어떻게 할까요?"로 팀장님께 물어볼 것**.
2. **시퀀스 코드는 무조건 승인 후 진행.** 편집 전에 변경할 파일·함수·라인 목록을
   팀장님께 전부 고지하고 승인받은 뒤 편집한다.
3. **인터락 코드는 이번 작업에서 0건 수정이다(§3).** 게이트/인터락 함수 본체를
   한 줄이라도 고치게 되는 상황이면 그 즉시 중단하고 팀장님께 보고할 것.
4. **빌드 주의**: 기본 빌드 출력이 실장비 실행 폴더로 직결된다. 장비 실행 중 기본
   빌드 금지. 검증 빌드는 `/p:OutDir=<임시경로>` 우회.
5. **모션 속도 스케일 규칙**: 이번 작업은 **기존 이동 헬퍼 호출에 forceMove 인자만
   추가**한다. 신규 이동 명령/속도/가감속 코드를 작성하지 말 것. MotionSpeedScale
   미적용 코드를 발견하면 수정하지 말고 팀장님께 보고만 할 것.
6. **계측 필수(§4)**: 스킵 제거 지점마다 발행 사유·수치 로그를 남겨 실장비 1회
   실행으로 "T/X/Y 미소 보정이 실제 발행됐는지" 확정 가능하게 할 것.
7. `ActualPosition`이 지령값을 반환하는 것은 의도된 설계다. 교정하지 말 것.
8. `PickerPickUpSequence.InputStageMove.cs`의 `IsInputStageAxisAlreadyInPosition`
   내 IsMoving 가드는 팀장님 직접 수정분이다. **절대 건드리지 말 것.**
9. 시뮬레이션 모드에서 Pick→검사→Place 1사이클 로그로 발행 여부 확인 후 실장비.

---

## 1. 배경 (분석 확정 사실)

### 1-1. 증상과 원인

- Front 픽커2가 Place 후검사에서 **계속 −0.4°** 로 측정됨(수렴 안 함).
- Place 클로즈루프 산식 자체는 정상: `PickerT = Place티칭T − 런타임필터T + 기구T`
  (`DieCoordinateTransformService.CalculatePlaceTarget`, 기구T·필터T 모두 배선 확인).
- 원인: `PickerSequenceBase.cs:3701` `CanSkipPickerMoveCommand`가 축 Config의
  `InPositionTolerance` 기준으로 "이미 위치"면 이동 명령을 **조용히 생략**한다.
  허용치가 보정량(0.4°)보다 크면 보정 목표를 계산해 놓고도 축이 움직이지 않고,
  Verify도 같은 허용치라 통과한다 → 보정이 영원히 반영 안 됨.
- 팀장님 결정: **Pick·Place의 최종 목표 접근 이동(X/Y/T)은 미소량이라도 스킵 없이
  전부 발행한다.**

### 1-2. 수정 전략 (왜 이 방식인가)

- `CanSkipPickerMoveCommand` **함수 본체는 수정 금지.** 호출부가 30여 곳인데
  상당수가 회피 복귀 스킵·"위치 도달 상태 판정"용(예:
  `PickerSideInspectionSequence.cs:604`)이라 전역 수정 시 검사/회피 로직 의미가 바뀐다.
- 베이스 이동 헬퍼 3종에 이미 `forceMove` 파라미터가 있고 내부 스킵 판정을
  `!forceMove && CanSkip...`으로 우회하게 되어 있다. **호출부에서 `forceMove: true`를
  넘기는 것이 정답.** 베이스 헬퍼 자체도 무수정.
- 헬퍼 시그니처 (인자 위치 주의 — `targetName` 다음이 `forceMove`):
  - `MovePickerAxisAndVerifyAsync(axis, target, description, ct, targetName = null, forceMove = false, useSafeMoveMotion = false)`
  - `MovePickerAxesAndVerifyAsync(targets, description, ct, targetName = null, forceMove = false, useSafeMoveMotion = false)`
  - `MovePickerXTThenYAndVerifyAsync(targets, description, ct, targetName = null, forceMove = false, useSafeMoveMotion = false)`
    (내부에서 X/T 묶음·Y 단독 이동과 게이트에 forceMove를 전파한다)
- **T축 특례**: Place의 T는 이동 호출 전에
  `AddLoadedPickerTPlaceTargets`(목표 딕셔너리 구성 단계)에서 걸러진다.
  **여기(§2-1)를 안 고치면 forceMove를 다 넣어도 T는 안 움직인다.**
- 축 레이어는 직전 지령과 동일 목표면 무명령 처리한다(기존 설계). 따라서 forceMove를
  넣어도 보정값이 실제로 변한 다이에서만 미소 이동이 발생하고, 중복 물리 이동·택타임
  악화는 없다.

---

## 2. 변경 범위 (이것만 — 총 14곳)

라인 번호는 2026-08-18 기준이다. **코드 내용으로 앵커를 찾고, 라인이 밀렸으면 내용
기준으로 위치를 확정할 것.**

### 2-1. Place — PickerPlaceSequence.PlaceDown.cs (T축 핵심, 1곳)

`AddLoadedPickerTPlaceTargets` (156~166행):

```csharp
foreach (int pickerIndex in _pickedPickerIndexes)
{
    PickerAxis tAxis = GetPickerTAxis(pickerIndex);
    double target = ResolvePlacePickerTTarget(pickerIndex);

    if (!CanSkipPickerMoveCommand(tAxis, target))
        targets[tAxis] = target;
}
```

변경: **현재 Place 대상 픽커(`pickerIndex == _currentPickerIndex`)는 스킵 판정 없이
무조건 `targets`에 추가**한다. 나머지 적재 픽커(순수 티칭 목표)는 기존 스킵 유지 —
보정이 실리는 건 현재 픽커뿐이고, 전부 강제하면 Place마다 불필요한 T 명령이 늘어난다.
현재 픽커가 "허용치 내 위치인데도 강제 추가"된 경우 §4-1 로그를 남길 것.

### 2-2. Place — PickerPlaceSequence.PlaceTargets.cs (4곳)

| # | 위치 | 현재 코드 앵커 | 변경 |
|---|---|---|---|
| 2 | 230행 `MovePickerXYAndTToPlaceAsync` | `MovePickerXTThenYAndVerifyAsync(targets, "place picker X/Y/T", ct, BuildPlaceMoveTargetName())` | `forceMove: true` 추가 |
| 3 | 470행 `MoveOutputStageYAndPickerXYTToPlaceAsync`의 비-follow 분기 | `: MovePickerXTThenYAndVerifyAsync(pickerTargets, "place picker X/Y/T", ct, BuildPlaceMoveTargetName())` | `forceMove: true` 추가 |
| 7 | 528행 `MoveOutputStageYAndPickerXTThenYToPlaceAsync` | `MovePickerAxesAndVerifyAsync(pickerXAndTTargets, "Place 재시작 Picker X/T", ...)` | `forceMove: true` 추가 |
| 8 | 546~555행 같은 함수의 Y 스킵 블록 + 564행 Y 이동 | `if (CanSkipPickerMoveCommand(PickerAxis.PickerY, _targetPickerY)) { ...로그... return 0; }` | 스킵 블록 **삭제**(§4-2 로그로 대체), 이어지는 564행 `MovePickerAxisAndVerifyAsync(PickerAxis.PickerY, _targetPickerY, "Place 재시작 PickerY 전진", ...)`에 `forceMove: true` 추가 — **이걸 빠뜨리면 내부 스킵(1241행)에 다시 걸린다** |

### 2-3. Place — PickerPlaceSequence.VisionRetreat.cs (3곳)

| # | 위치 | 현재 코드 앵커 | 변경 |
|---|---|---|---|
| 4 | 402행 `MovePlacePickerXTThenYWithVisionFollowAsync` | `MovePickerAxesAndVerifyAsync(tTargets, "place picker T", ct, targetName)` | `forceMove: true` 추가 |
| 5 | 413행 같은 함수 Y 스킵 판정 + 422행 Y 이동 | `if (!hasPickerY \|\| CanSkipPickerMoveCommand(PickerAxis.PickerY, pickerYTarget)) return 0;` | `if (!hasPickerY) return 0;` 으로 변경, 이어지는 422행 `MovePickerAxisAndVerifyAsync(PickerAxis.PickerY, pickerYTarget, "place picker Y", ...)`에 `forceMove: true` 추가 — **#8과 동일 이유** |
| 6 | 452행 `MovePlacePickerXEntryByVisionFollowOrFallbackAsync`의 R6 폴백 | `MovePickerAxisAndVerifyAsync(PickerAxis.PickerX, _targetPickerX, description + " (follow 폴백)", ct, targetName)` | `forceMove: true` 추가 |

### 2-4. Place — PickerPlaceSequence.ContiPlace.cs (1곳)

| # | 위치 | 현재 코드 앵커 | 변경 |
|---|---|---|---|
| 9 | 716행 `MovePickerYAndTToPlaceBeforeContiSegmentedPlaceAsync` | `MovePickerAxesAndVerifyAsync(targets, "place picker Y/T before synchronized arrival", ct, BuildPlaceMoveTargetName())` | `forceMove: true` 추가 |

### 2-5. Pick — PickerPickUpSequence.EntryMotion.cs (4곳)

| # | 위치 | 현재 코드 앵커 | 변경 |
|---|---|---|---|
| 10 | 432행 `MovePickerXStageYPickerTByDefaultAsync` follow 분기 | `MovePickerAxisAndVerifyAsync(tAxis, _targetPickerT, "pick corrected PickerT", ct, targetName)` | `forceMove: true` 추가 |
| 11 | 453행 같은 함수 default 분기 | `MovePickerAxesAndVerifyAsync(pickerTargets, "pick corrected Picker X/T", ct, targetName)` | `forceMove: true` 추가 |
| 12 | 494행 같은 함수 Y 전진 | `MovePickerAxisAndVerifyAsync(PickerAxis.PickerY, _targetPickerY, "pick corrected PickerY", ct, targetName)` | `forceMove: true` 추가 |
| 13 | 915행 ContiNode 경로 | `MovePickerAxesAndVerifyAsync(pickerYtTargets, "PickUp ContiNode PickerY/T 동시 발행", ct, targetName)` | `forceMove: true` 추가 |

### 2-6. Pick — PickerPickUpSequence.VisionRetreat.cs (1곳)

| # | 위치 | 현재 코드 앵커 | 변경 |
|---|---|---|---|
| 14 | 644행 `MovePickerXEntryByVisionFollowOrFallbackAsync`의 R6 폴백 | `MovePickerAxisAndVerifyAsync(PickerAxis.PickerX, _targetPickerX, description + " (follow 폴백)", ct, targetName)` | `forceMove: true` 추가 |

---

## 3. 무변경 확정 목록 (건드리면 안 되는 곳)

1. **인터락/게이트 함수 전부 무수정**: `WaitPickerXSharedRailDistanceBeforeAutoMoveAsync`,
   `WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync`,
   `WaitPickerFacingYInterlockBeforeAutoMoveAsync`, `VerifyPickerYAvoidBlocksZDown`,
   `JoinDeferredInput/OutputVisionRetreat...` 등. forceMove로 인해 "이미 위치라서
   선스킵되던 좁은 경우"에도 이 게이트들이 평가되게 되는 것은 **의도된 동작**이다
   (기존 게이트 코드가 그대로 돌 뿐, 로직 변경 아님).
2. `CanSkipPickerMoveCommand` 함수 본체(PickerSequenceBase.cs:3701).
3. 베이스 이동 헬퍼 내부(1241·1454·1595·1837·1867행의 `!forceMove && CanSkip...` 구조).
4. Conti Place의 X/StageY 판정(`RequiresContiPlaceForceMove`/
   `IsContiPlaceAxisSkipEligible`, ContiPlace.cs:723~739) — 이미 "실측·지령 둘 다
   F3 일치"일 때만 스킵이라 수정 불요.
5. Pick Conti X 본선 `MovePickerAxisWithMotionAndVerifyAsync`(MotionResolvers.cs:17) —
   스킵 게이트 자체가 없음. follow 성공 경로(`TryFollowPickerX...`)도 항상 자체 발행.
6. 위 14곳 외의 모든 `CanSkipPickerMoveCommand` 호출부(검사 시퀀스·회피·Z Avoid·
   수동 경로·MapTransferPage 등) — 전부 기존 유지.
7. 비-현재 픽커의 Place T 스킵(§2-1에서 유지 결정).
8. Verify 계열(`VerifyPickTarget`/`VerifyPlaceTarget`/`IsPickerAxisInPosition`) —
   허용치 판정 기존 그대로.

---

## 4. 계측 (수정과 한 몸 — 빼먹지 말 것)

기존 `SequenceTrace.MotionStart` 로그에 `forceMove=true`가 찍히므로 forceMove 추가
지점은 별도 로그 불요. 다음 3곳만 추가한다:

1. **§2-1** `AddLoadedPickerTPlaceTargets`: 현재 픽커 T가 허용치 내인데 강제 추가된
   경우 1줄 — `pickerNo, target, actual, diff` 포함, 태그 `PickerPlaceSequence`,
   메시지 예: `"Place T 목표를 허용치 내에서도 강제 발행합니다(미소 보정 반영)."`
2. **§2-2 #8** 삭제한 Y 스킵 블록 자리: 기존 스킵 로그를 대체해
   `"Place 재시작 PickerY를 허용치 내에서도 강제 발행합니다."` (diff 수치 포함,
   허용치 내였을 때만 출력 — 매 사이클 노이즈 금지).
3. **§2-3 #5** 동일 요령으로 1줄.

목적: 실장비 1런에서 "Front P2 T가 실제로 발행됐고 몇 도 움직였는지"를 로그만으로 확정.

---

## 5. 검증 절차

1. **빌드**: `/p:OutDir=<임시경로>` 우회 빌드로 컴파일 확인(기본 빌드 금지).
2. **시뮬레이션**: Auto 1사이클 — Pick 진입 X/T·Y, Place 진입 X/Y/T에서
   `forceMove=true` MotionStart 로그와 §4 로그가 나오는지, 스킵 로그
   (`move skipped. Axis already in position`)가 최종 목표 접근에서 사라졌는지 확인.
3. **실장비(팀장님 진행)**: Front 픽커2 다이에서
   - `DIE-COORD-CALC phase=PLACE pickerNo=2`의 pickerT 수식에 `placeRuntimeOffsetT`/
     `placeMechanicalOffsetT`가 0이 아닌 값으로 반영되는지,
   - T축 이동 명령이 실제 발행되는지(§4-1 로그),
   - 이후 후검사 `PLACE-RUNTIME-OFFSET`의 measuredT가 0으로 수렴하는지.

주의: 게이트가 항상 평가되므로 Y 재발행 시 반대측 픽커 대기 등으로 사이클 소요가
미세하게 변할 수 있다. 시뮬/실장비에서 이상 대기(수 초 이상)가 보이면 중단하고 보고.

---

## 6. 범위 밖 (이번에 하지 않는다 — 별도 지시 예정)

- Bin 후검사에서 `placement_angle_deg`/`placement_item_angle` 키 부재 시 T에 0.0을
  주입하는 문제(OutputPostPlaceInspectionQueue.cs:1607~1613) — 별도 분석/지시.
- 이상치 한계(`OutlierLimitTDeg`)·필터 설정 점검 — 장비 설정 확인 사항.
- T축 `InPositionTolerance` 축소 여부 — 기구 판단 필요, 이번 수정으로 불필요해질
  가능성이 높다.
