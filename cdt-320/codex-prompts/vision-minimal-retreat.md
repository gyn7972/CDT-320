# 작업: 비전 축 최소 회피 — 웨이퍼/빈 비전이 "피커 최대 진입 + 클리어런스 + Extra(40mm)"까지만 회피

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일:
  - `QMC.CDT-320\Equipment\Motion\SharedRailX\SharedRailXConfig.cs` / `SharedRailXConfigStore.cs` (설정 신설)
  - `QMC.CDT-320\Equipment\Motion\SharedRailX\SharedRailXMotionService.cs` (최소 회피 계산 신설)
  - `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs` (웨이퍼 비전 회피 호출부)
  - `QMC.CDT-320\Sequencing\Picker\InputCameraMarkInspectionSequence.cs` (+ 필요 시
    `InputDieVisionPrepareSequence.cs`) (선행검사 후 웨이퍼 비전 회피)
  - `QMC.CDT-320\Sequencing\Picker\PickerPlaceSequence.cs` (빈 비전 회피 호출부)
  - `QMC.CDT-320\Sequencing\OutputStage\OutputPostPlaceInspectionQueue.cs` (빈 촬영 후 회피)
- **인터락/충돌 판정 코드는 수정 금지** (아래 "절대 금지" 절 참조).

## 목적

촬영이 끝난 비전 축이 전체 Avoid(티칭 끝단)까지 도망가지 않고, **피커의 최대 진입 위치에서
(페어 SafetyDistance + 신설 Extra 40mm)만큼 떨어진 위치까지만** 회피해 대기한다.
다음 촬영 복귀 거리를 줄여 사이클을 단축한다.

단, **피커 측 진입 허용 판정(인터락)은 Extra를 포함하지 않는다** — 피커는 비전이 기존
SafetyDistance만 확보되면 즉시 작업을 시작할 수 있고, 비전이 Extra 40mm 버퍼 구간을 마저
물러나는 동안 피커 작업이 병행된다(오버랩).

## 배경 — 기존 구조와 실측 설정값 (수정 전 반드시 읽을 것)

### 클리어런스 수식 (기존, 변경 금지)
`SharedRailXMotionService.CalculatePairClearance()` (약 845행):
```
간격 = HomeClearance − (AxisATowardSign × A위치) − (AxisBTowardSign × B위치)
안전 조건: 간격 ≥ required
required(인터락) = 페어 SafetyDistance (없으면 Max(축A,축B SafetyDistance))
```

### 실측 설정값 (장비 파일 `Config\shared_rail_x.json` — 검증 기준으로 사용)
| 페어 | HomeClearance | 비전부호 | 피커부호 | SafetyDistance |
|---|---|---|---|---|
| InputVisionX ↔ FrontPickerX / RearPickerX | 88.5 | +1 | −1 | 10 |
| OutputVisionX ↔ FrontPickerX / RearPickerX | 510 | −1 | +1 | 10 |

### 확정된 목표 수식 (사용자 검증 완료 — 이 결과가 그대로 나와야 한다)
```
[웨이퍼] 간격 = 88.5 − 비전위치 + 피커위치, 회피 목표 간격 = Safety(10) + InputExtra(40) = 50
        → 비전 회피 목표 = (배치·현재 피커 중 최소X) + 88.5 − 50 = 최소X + 38.5
        예) 피커 목표 {680,660,640,620} → 회피 목표 = 620 + 38.5 = 658.5
            검산: 88.5 − 658.5 + 620 = 50 ✓

[빈]    간격 = 510 + 비전위치 − 피커위치, 회피 목표 간격 = Safety(10) + OutputExtra(40) = 50
        → 비전 회피 목표 = (배치·현재 피커 중 최대X) − 510 + 50 = 최대X − 460
        예) 피커 목표 {480,500,520,540} → 회피 목표 = 540 − 460 = 80
            검산: 510 + 80 − 540 = 50 ✓

[피커 진입 게이트 — 변경 없음, Extra 미포함]
        웨이퍼: 간격 ≥ 10 → 비전이 (최소X + 78.5) 이하이면 피커 진입 허용
        빈:     간격 ≥ 10 → 비전이 (최대X − 500) 이상이면 피커 진입 허용
        → 예) 웨이퍼: 비전이 658.5 목표로 이동 중 698.5를 통과하는 순간부터 피커 작업 가능
```

### 기존 동적 회피 함수의 한계 (반드시 인지할 것)
`SharedRailXMotionService.TryResolveNearestVisionRetreatTarget()` (약 88~235행)이 유사 기능을
하지만 **그대로 재사용하면 안 되는 두 가지 문제**가 있다:
1. 호출부들이 `maximumPickerEntryPosition = -0.1`, `additionalClearance = 1.0`을 하드코딩으로
   전달한다. `-0.1` 상한 때문에 InputVisionX는 안전 경계(예: 658.5)가 아니라 **−0.1 부근까지
   과도하게 회피**한다 (`preferred = Min(actual, maxEntry)`, 160행).
2. 이 함수의 `Min`/`>` 비교는 **회피 방향이 −인 축(InputVisionX) 기준**으로 작성되어 있다.
   OutputVisionX(회피 방향 +)는 계산 결과가 `maximumPickerEntryPosition(-0.1)`보다 항상 커서
   218행의 상한 검사에 걸려 **사실상 항상 전체 Avoid로 폴백**된다.
   → OutputVisionX에서도 동작하는 **부호 인지(direction-aware) 최소 회피 계산이 신규로 필요**하다.

### 비전 회피가 일어나는 지점 (적용 대상)
- 웨이퍼(Input) 비전:
  - `PickerPickUpSequence.MoveInputVisionToAvoidForPickerMoveAsync` (약 1000~1112행) —
    현재 `TryResolveNearestVisionRetreatTarget(stage.CameraX, avoid, -0.1, planned, 1.0, ...)` 사용.
    planned = 배치 전체의 `TargetPickerX` 목록 (이미 구성돼 있음).
  - `InputCameraMarkInspectionSequence.MoveInputVisionXToAvoidAsync` — 선행검사 촬영 종료 후
    회피 (구현을 읽고 전체 Avoid인지 확인 후 적용).
- 빈(Output) 비전:
  - `PickerPlaceSequence.MoveOutputStageAvoidPositionAsync` 내 OutputVisionX 회피 좌표 결정부
    (약 1205~1251행) — 현재 동일 함수를 -0.1/1.0으로 호출 (위 한계 2번 때문에 실제로는 전체
    Avoid로 폴백 중일 가능성이 높음 — 로그로 확인).
    planned = `{pickerRailAxis: [_targetPickerX]}` (현재 1개만 — R4에서 배치 전체로 보강).
  - `OutputPostPlaceInspectionQueue` — 빈 촬영 종료 후 OutputVisionX가 Avoid로 복귀하는 지점
    (`OutputStage.MoveVisionXToAvoidAndVerifyAsync` 호출부 등 — 전수 검색해서 확인).

### Conti 모드 판정 (적용 게이트)
- 픽업 계열(웨이퍼 비전): `Options.RunMode == Auto` **그리고**
  `PickerPickUpMotionConfig.TransferMotionMode`가 Conti 계열
  (`ContiSegmentedPickUp` 또는 `FastContiSegmentedPickUp` — enum에 존재하는 값 기준).
- 플레이스 계열(빈 비전): `Options.RunMode == Auto` **그리고**
  `PickerPlaceMotionConfig.MotionMode`가 coordinated/Conti 계열
  (`IsCoordinatedPlaceMotionMode` 판정 재사용).
- 게이트 미충족(수동/캘리브레이션/비Conti) 시: **기존 코드 경로 그대로** (동작 무변경).

## 요구사항

### R1. 설정 신설 (Input/Output 분리)
`SharedRailXConfig`(+`SharedRailXConfigDocument`, 스토어 직렬화)에 추가:
| 설정 | 기본값 | 용도 |
|---|---|---|
| `InputVisionRetreatExtraClearance` | 40.0 | 웨이퍼 비전 최소 회피의 추가 여유 |
| `OutputVisionRetreatExtraClearance` | 40.0 | 빈 비전 최소 회피의 추가 여유 |
- `Normalize()`에서 0 미만이면 기본값 보정. 기존 `shared_rail_x.json`에 필드가 없어도
  로드 시 기본값 40.0이 적용되도록 하위호환 처리 (기존 문서 로드 회귀 없음).
- UI 노출은 범위 외 (파일 편집으로 조정).

### R2. 부호 인지 최소 회피 계산 신설 (`SharedRailXMotionService`)
새 public 메서드 (기존 `TryResolveNearestVisionRetreatTarget`는 **수정하지 말고 그대로 두고**
별도 추가 — 기존 호출부의 비Conti 동작 보존):
```csharp
public bool TryResolveMinimalVisionRetreatTarget(
    BaseAxis visionAxis,
    double fullAvoidPosition,
    IDictionary<SharedRailXAxis, IList<double>> plannedAxisPositions,
    double extraClearance,           // 신설 Extra (Input/Output 값을 호출부가 전달)
    out double retreatTarget,
    out string detail)
```
동작 (닫힌 수식 — 이진 탐색 불필요):
1. 기존 함수와 동일하게 비전 레일 축 확인, CollisionPairs에서 비전이 포함된 페어 수집,
   장애물 위치 수집(상대 축의 `ActualPosition` + `CommandPosition` + planned 목록) —
   88~158행의 수집 로직을 재사용/공용화한다.
2. 각 페어·각 장애물 위치에 대해 경계 계산:
   ```
   required_i = (페어 SafetyDistance ?? Max(축 SafetyDistance)) + Max(0, extraClearance)
   bound_i    = HomeClearance − (피커부호 × 장애물위치_i) − required_i
   제약: (비전부호 × 비전위치) ≤ bound_i
   ```
3. 모든 제약을 만족하는 경계 = `boundMin = Min(bound_i)` →
   ```
   비전부호 = +1 이면: retreatTarget = boundMin            (비전 ≤ boundMin)
   비전부호 = −1 이면: retreatTarget = −boundMin           (비전 ≥ −boundMin)
   ```
   (비전부호는 해당 페어의 TowardSign에서 취득 — 페어마다 비전부호가 다르면 각 제약을
   비전부호 곱 형태로 정규화해 동일하게 처리)
4. 클램프/검증:
   - `retreatTarget`이 **전체 Avoid보다 더 물러나야 하는 값**이면(즉 회피 방향으로 fullAvoid를
     넘어서면) `retreatTarget = fullAvoidPosition` 사용.
   - `retreatTarget`이 회피 방향 기준으로 **현재 작업 위치보다 진입 쪽**이면(계산상 지금 그대로
     있어도 안전) 현재 `ActualPosition` 유지 결정을 호출부에 맡기지 말고 `retreatTarget`을
     그대로 반환 (호출부는 이미 위치면 이동 생략하는 기존 관례 사용).
   - 축 Setup 소프트리밋 범위로 클램프 (리밋 밖 목표 방지).
   - 최종적으로 기존 `IsVisionRetreatTargetSafe(...)`에 `additionalClearance = extraClearance`를
     넣어 재검증 — 불일치하면 전체 Avoid 폴백 + 로그 (belt-and-braces).
5. 실패 조건(페어 없음/ClearanceRule 없음 등)은 기존 함수와 동일하게 false 반환 →
   호출부는 전체 Avoid 폴백. detail 문자열에 계산 근거(boundMin, 장애물 최솟/최댓값,
   required, extra)를 F6 포맷으로 남긴다.

### R3. 웨이퍼 비전 호출부 적용
1. `PickerPickUpSequence.MoveInputVisionToAvoidForPickerMoveAsync`:
   - Conti 게이트 충족 시: `TryResolveMinimalVisionRetreatTarget(stage.CameraX, fullAvoid,
     planned(배치 전체 TargetPickerX), config.InputVisionRetreatExtraClearance, ...)` 사용.
   - 게이트 미충족 시: 기존 코드(기존 함수 + -0.1/1.0) 그대로.
   - 로그에 mode(minimal/legacy), 목표, 계산 근거를 남긴다.
2. `InputCameraMarkInspectionSequence.MoveInputVisionXToAvoidAsync`:
   - 구현을 읽고 현재 전체 Avoid라면, Conti 게이트 충족 시 최소 회피로 교체.
   - 이 시점에는 pick 좌표(CalculatePickTargets)가 아직 없으므로 planned는 **근사값**으로
     구성한다: 배치 각 아이템의 `PickTarget.TargetX`(비전 기준 die 위치)에
     `PickerCoordinateTransformHelper.TryResolveInputVisionToPickerOffsets`의 X 오프셋을 더한
     근사 피커 X. 근사 실패 시 전체 Avoid 폴백.
   - 이후 픽업 시퀀스의 1번 지점이 정확한 좌표로 재계산·재이동하므로 근사여도 안전하다
     (Extra 40mm 버퍼 + 인터락은 그대로 살아있음 — 주석으로 명시).

### R4. 빈 비전 호출부 적용
1. `PickerPlaceSequence.MoveOutputStageAvoidPositionAsync`의 OutputVisionX 회피 좌표 결정부:
   - Conti 게이트 충족 시: 새 메서드 사용. planned는 현재 `_targetPickerX` 1개만 넣고 있는데,
     **플레이스 배치 전체의 계산된 PickerX 목표 목록으로 보강**한다 (배치 아이템들의 place
     대상 X — `_pickedPickerIndexes` 순회로 수집 가능한 값 사용; 아직 미계산 아이템이 있으면
     현재 아이템 값 + 상대 피커 현재 위치로 충분 — 서비스가 Actual/Command도 장애물로
     포함하므로 안전).
   - 게이트 미충족 시: 기존 코드 그대로.
2. `OutputPostPlaceInspectionQueue`: 빈 촬영 종료 후 OutputVisionX를 Avoid로 되돌리는 모든
   지점을 전수 검색해서, Conti 게이트(플레이스 계열) 충족 시 동일한 최소 회피를 적용.
   이 시점의 planned는 다음 플레이스 대상 좌표를 알 수 없으므로 **피커 현재
   Actual/Command만으로 계산**(planned 전달 생략 가능 — 서비스가 현재 위치를 자동 포함).
   다음 플레이스가 시작되면 R4-1이 정확한 배치 좌표로 재계산한다.

### R5. 절대 금지 — 인터락에 Extra 유입 금지
- `IsVisionRetreatTargetSafe`의 **기존 호출부**, `SharedRailXCollisionValidator`,
  `VerifySingleAxisMove`, `MotionGuardRuntime`, `WaitInputVisionXSharedRailClearAsync`,
  `WaitOutputVisionXSharedRailClearAsync` 등 **피커/비전 진입 허용을 판정하는 어떤 코드에도
  신설 Extra 값을 더하지 않는다.** 인터락 요구거리는 기존 그대로
  `SafetyDistance`(페어/축 설정)만 사용한다.
- Extra는 오직 R2 신설 메서드의 **회피 목표 계산**에만 존재한다.
- 이 원칙을 신설 메서드와 설정 프로퍼티의 XML 주석에 명시한다.

## 제약 사항
- 기존 `TryResolveNearestVisionRetreatTarget`와 그 비Conti 호출 경로는 무변경 (회귀 방지).
- 코드 스타일: 기존 관례 (한국어 로그/주석, F6 포맷, Fail/폴백 로그, 최신 C# 문법 자제).
- 시뮬레이션 모드에서도 동일 계산으로 동작해야 한다 (축 위치가 시뮬 값일 뿐).
- `shared_rail_x.json` 하위호환: 새 필드 없는 기존 파일 로드 시 기본값 40.0.

## 검증 / 수용 기준 (숫자까지 일치해야 함)
1. 솔루션 전체 빌드 통과.
2. **단위 계산 검증** (하네스/테스트로 실측 설정값 재현, 결과 보고):
   - Input: HomeClearance=88.5, 부호(+1,−1), Safety=10, Extra=40,
     장애물(planned) = {680,660,640,620} → **retreatTarget = 658.5** (검산 간격 50).
   - Output: HomeClearance=510, 부호(−1,+1), Safety=10, Extra=40,
     장애물 = {480,500,520,540} → **retreatTarget = 80** (검산 간격 50).
   - Output 케이스가 전체 Avoid로 폴백하지 않고 **80을 반환하는 것**이 기존 함수와의 핵심
     차이임을 확인 (기존 함수는 -0.1 상한 때문에 폴백했음).
   - Extra=0으로 주면 Input 698.5 / Output 40 이 나오는지 확인 (경계 일반화 검증).
   - 페어 설정 없음/ClearanceRule 없음 → false + 전체 Avoid 폴백.
   - 소프트리밋 클램프 동작.
3. **시뮬 통합 검증**:
   - Auto+Conti 픽업 사이클: 웨이퍼 비전이 전체 Avoid가 아니라 계산 좌표로 회피하고,
     피커 진입 인터락이 기존 SafetyDistance 기준으로 통과(비전이 최종 위치 도착 전에 피커
     이동 시작 가능)함을 로그 타임스탬프로 확인.
   - Auto+Conti 플레이스 사이클: 빈 비전 동일 확인.
   - 비Conti/수동 모드: 기존 회피 좌표와 동일(회귀 없음).
   - 충돌 인터락 위반/알람 0건.
4. 인터락 코드 diff가 0건임을 보고 (R5 준수 증명).
5. 적용 지점 전수 목록(파일:라인)과 각 지점의 게이트 판정 방식을 보고서로 제출.
