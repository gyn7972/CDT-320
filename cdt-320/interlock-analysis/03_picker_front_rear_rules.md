# PickerFront / PickerRear InterlockRules 완전 분석

분석 대상
- `QMC.CDT-320\Equipment\Interlocks\PickerFrontInterlockRules.cs` (1,616줄)
- `QMC.CDT-320\Equipment\Interlocks\PickerRearInterlockRules.cs` (1,546줄)

두 파일 모두 `namespace QMC.CDT320.Interlocks`의 `public static class`이며, 진입점은 `Verify(MotionGuardRuleContext request, out string reason)` 하나뿐이고 나머지는 전부 `private static` 헬퍼. (라인 표기: F=Front, R=Rear)

---

## 1. 두 파일의 구조 비교 (diff 관점)

### 1-1. 큰 그림: 약 90% 대칭 복제
Front/Rear는 "Front↔Rear", "PickerFrontUnit↔PickerRearUnit", 상대 Picker 참조만 뒤집힌 미러 복제. 축 라우팅, MoveKind 분기, Input/Output 진입 검사(Z/ExpanderZ/Feeder/VisionX), Reticle, Z 하강 금지, 유틸 함수군은 로직 동일.

핵심 차이는 **Home 검사 비대칭 3곳**. Front가 항상 더 많은 검사를 수행(Front는 Input 스테이지 쪽에 물리적으로 가까워 추가 조건).

### 1-2. 실제 차이 (Front에만 있고 Rear에 없는 것)

| 구분 | Front | Rear | 비고 |
|---|---|---|---|
| Home X 검사 항목 | 7개 | 5개 | 아래 상세 |
| Home Y 검사 항목 | 4개 | 2개 | 아래 상세 |
| `IsInputFeederYHomeOrAvoid()` 헬퍼 | **있음** (F1423) | 없음 | Front Home X 전용 |
| `IsGoodStageZHomeAvoidOrProcess()` 헬퍼 | **있음** (F1433) | 없음 | Front Home Y 전용 |
| Manual Y에서 InputExpandingZ 주석블록 | 있음 (F386~393) | 없음 | 죽은 주석 |

**① `CanHomeFrontPickerX` vs `CanHomeRearPickerX`**
- Front(F869): InputVisionX + ExpanderZ(Home/Avoid/Process/Ready) + FrontPickerY(Home/Avoid) + RearPickerY(Home/Avoid) + FrontPickerZ(Home/Avoid) + **InputFeederY(Home/Avoid)** + **InputFeeder Lift Down** = 7개
- Rear(R860): InputVisionX + ExpanderZ + FrontPickerY + RearPickerY + RearPickerZ = 5개 (InputFeederY / Lift Down 검사 **없음**)

**② `CanHomeFrontPickerY` vs `CanHomeRearPickerY`**
- Front(F951): `VerifyPickerYHomePairSafety` + Z(Home/Avoid) + **InputExpandingZ(Home/Avoid/Process/Ready)** + **OutputStage GoodStageZ(Home/Avoid/Process)** = 4개
- Rear(R927): `VerifyPickerYHomePairSafety` + Z(Home/Avoid) = 2개

**③ `CanManualFrontPickerY` vs `CanManualRearPickerY` — 검사 순서 차이**
- Front(F358): Z → Reticle → Jog → **`VerifyFrontPickerYMove`** → OutputStage GoodStageZ
- Rear(R724): Z → Reticle → Jog → **OutputStage GoodStageZ 먼저** → `VerifyRearPickerYMove`
- 결과 판정 집합은 거의 같으나 차단 우선순위가 다름.

### 1-3. 사소한 차이 (기능 무관)
- using 순서/System 참조 차이 (`StringComparison` 표기)
- isFront 플래그: PickerZone 계열 호출에 Front=`true`, Rear=`false`
- 상대 Picker Y-Avoid 검사 함수(`VerifyRearPickerYAvoidForFrontPickerX` F853 ↔ `VerifyFrontPickerYAvoidForRearPickerX` R493)는 **양쪽 모두 호출부 주석처리(미사용)**
- 함수 배치 순서 차이 (VisionX Avoid 블록 위치)

---

## 2. Verify 진입 후 호출 트리 (축 이름 → 분기)

```
Verify(request, out reason)
 ├ request==null || request.Machine==null            → return true (무조건 허용)
 ├ IsMoving "FrontPickerX"                            → VerifyFrontPickerX
 ├ IsMoving "FrontPickerY"                            → VerifyFrontPickerY
 ├ IsMoving "FrontPickerT0..T3"                       → VerifyFrontPickerT
 ├ IsMoving "FrontPickerZ0..Z3"                       → VerifyFrontPickerZ
 └ else                                               → return true

VerifyFrontPickerX
 ├ VerifyPickerXGlobalMachineClearance (PickerZone, 전제)
 ├ IsJogMove → CanJogFrontPickerX
 └ MoveKind: AxisTeachingMove→CanAutoFrontPickerX / AxisMove→CanManualFrontPickerX
             / AxisHome→CanHomeFrontPickerX / default→BlockUnsupportedMoveKind

VerifyFrontPickerY
 └ MoveKind: AxisTeachingMove→CanAutoFrontPickerY / AxisMove→CanManualFrontPickerY
             / AxisHome→CanHomeFrontPickerY / default→Block

VerifyFrontPickerT
 └ MoveKind: Auto→CanAutoFrontPickerT / Manual→CanManualFrontPickerT / Home→CanHomeFrontPickerT

VerifyFrontPickerZ
 └ MoveKind: Auto→CanAutoFrontPickerZ(=Manual과 동일) / Manual→CanManualFrontPickerZ
             / Home→CanHomeFrontPickerZ(무조건 true)
```
Rear는 Front→Rear 이름 치환한 완전 동형.

라우팅 특징:
- **X축만** 진입 전에 `VerifyPickerXGlobalMachineClearance`(전 장비 클리어런스)와 Jog 전용 경로
- **Jog**: X Jog는 `CanJog*PickerX`(존 판정 생략), Y Jog는 Manual 경로 안에서 `Verify*PickerYJogFacingMove`
- **Auto(AxisTeachingMove)**: 거의 모든 축에서 Manual 기본 인터락 통과 후 추가 조건. Z축은 Auto=Manual 동일
- default(미지원 MoveKind)는 `BlockUnsupportedMoveKind` 차단

---

## 3. 함수별 상세 (Front 기준, Rear 라인 병기)

### 3-1. X축 검사 (핵심)

**`CanJogFrontPickerX`** (F68 / R68)
- ① Z0~Z3 Home/Avoid ② Reticle 실린더 정지 ③ Picker Busy(전부 주석→항상 true). 목표 존 판정 생략.

**`CanAutoFrontPickerX`** (F101 / R101)
- ① `CanManualFrontPickerX` 통과 필수 → ② Z0~Z3 Avoid(`VerifyFrontPickerZAxesAvoidForMove`, InspectionZHold/FineAlign 예외) → ③ ColletCalibration Bottom VisionX Avoid → ④ Busy

**`CanManualFrontPickerX`** (F501 / R143) — 목표 존 기반 진입 검사:
```csharp
PickerWorkZone targetZone = PickerZoneInterlockRules.ResolveManualPickerXTargetZone(request, true);
PickerWorkZone currentZone = PickerZoneInterlockRules.ResolveManualPickerXCurrentZone(machine, true);
if (targetZone == PickerWorkZone.Unknown)              // 목표존 불명 → 차단
    return Block("FrontPickerX 이동 불가: Manual X 목표 존을 판단할 수 없습니다. ...");
if (targetZone == Input  && !VerifyManualFrontPickerXInputEntry(...))  return false;
if (targetZone == Output && !VerifyManualFrontPickerXOutputEntry(...)) return false;
if (IsManualPickerXProcessZone(targetZone) && !IsManualPickerXProcessZone(currentZone) &&
    !CanKeepFrontPickerZDuringXMove(request) &&
    !VerifyFrontPickerZAxesAvoidOrNonNegative(...)) return false;
if (!PickerZoneInterlockRules.VerifyFrontPickerXMove(request, out reason)) return false;  // 반대 픽커 점유 최종확인
```

**`VerifyManualFrontPickerXInputEntry`** (F573 / R213) — Input 진입 6단 체크(순차 AND):
1. Z0~Z3 Avoid 또는 ≥0
2. InputExpandingZ ≤0 또는 Avoid
3. InputFeeder Avoid Dog **X090** ON
4. InputFeeder Lift Down
5. InputVisionX Avoid 또는 ≤0
6. `VerifyPickerXOppositeYClearance` (양쪽 Y 동시 전진 금지)

**`VerifyManualFrontPickerXOutputEntry`** (F609 / R249) — Output 진입 6단:
1. Z Avoid/≥0 → 2. GoodStageZ ≤ ProcessPos → 3. OutputFeeder Avoid Dog **X091** ON → 4. Output Feeder Down → 5. OutputVisionX Avoid/≤0 → 6. OppositeYClearance

**`CanHomeFrontPickerX`** (F869, 7단 / R860, 5단):
```csharp
InputVisionX not-homed-or-home → "InputVisionX must be not homed yet or at Home position."
InputExpandingZ Home/Avoid/Process/Ready → "InputExpandingZ must be at Home(0), Avoid, Process or Ready position."
FrontPickerY Home/Avoid → "FrontPickerY must be at Home(0) or Avoid position."
RearPickerY Home/Avoid
FrontPickerZ0~3 Home/Avoid
[Front 전용] InputFeederY Home/Avoid → "InputFeederY must be at Home(0) or Avoid position."
[Front 전용] InputFeeder Lift Down → "InputFeeder lift cylinder must be down."
```

### 3-2. Y축 검사

**`CanAutoFrontPickerY`** (F338 / R704) — `CanManualFrontPickerY` 통과 후 Busy만.

**`CanManualFrontPickerY`** (F358 / R724):
```csharp
if (!CanKeepFrontPickerZDuringYMove(request) &&                 // ZHold/FineAlign 예외 아니면
    !VerifyFrontPickerZAxesHomeOrAvoid(...)) return false;      // Z Home/Avoid
if (!VerifyReticleCylinderClear(...)) return false;
if (IsJogMove(request))
    return PickerZoneInterlockRules.VerifyFrontPickerYJogFacingMove(request, out reason);
if (!PickerZoneInterlockRules.VerifyFrontPickerYMove(request, out reason)) return false;
PickerWorkZone targetZone = ResolvePickerZTargetZone(request);
if (RequiresOutputStageZSafeForPickerY(targetZone) &&
    !outputStage.IsGoodStageZInAvoidOrProcessPosition())
    return Block("FrontPickerY 이동 불가: OutputStage GoodStageZ가 Avoid 또는 Process 위치가 아닙니다. ...");
```

**`CanHomeFrontPickerY`** (F951 / R927) — `VerifyPickerYHomePairSafety` + Z(Home/Avoid) [+ Front만: InputExpandingZ, OutputGoodStageZ]

### 3-3. T축 검사 (T-Z 페어)
- `CanAutoFrontPickerT` (F443 / R802): Busy만
- `CanManualFrontPickerT` (F466 / R825): T→대응 Z(`TryResolvePairedZAxis`, T0→Z0…T3→Z3)가 **Avoid 위치** 필수
- `CanHomeFrontPickerT` (F1002 / R960): 대응 Z가 **Home(0) 또는 Avoid**

### 3-4. Z축 검사

**`CanManualFrontPickerZ`** (F1068 / R1026) = `CanAutoFrontPickerZ`:
```csharp
if (!CanHomeFrontPickerZ(...)) return false;                 // 항상 true
if (!VerifyFrontPickerYAvoidBlocksZDown(request, out reason)) return false;   // Y Avoid계열이면 Z하강 금지
if (!VerifyReticleRetractedBeforePickerZWorkMove(request, out reason)) return false;  // Reticle Retract 필수
if (RequiresInputStageZSafe(targetZone)  && !VerifyInputExpanderZAtOrBelowZero(...))  return false;
if (RequiresOutputStageZSafeForPickerY(targetZone) && !VerifyGoodStageZAtOrBelowProcess(...)) return false;
return VerifyFrontPickerNotBusy(...);
```

**`VerifyFrontPickerYAvoidBlocksZDown`** (F1118 / R1076) — Y-Z 연동 하강 금지:
```csharp
if (!TryResolveMovingZAxis(request.MovingName, out zAxis)) return true;
if (IsFrontPickerZSafeRetreatTarget(...)) return true;   // 목표가 0 또는 Z Avoid면 허용
if (!IsZTargetDown(request.TargetValue, zItem)) return true;  // 실제 하강 아니면 허용
string yAvoidName = ResolveFrontPickerYAvoidPositionName(picker);  // Input/Output/공통 Avoid
if (string.IsNullOrWhiteSpace(yAvoidName)) return true;
return Block("...FrontPickerY가 {yAvoidName} 위치일 때 PickerZ 하강 이동은 금지됩니다. pickerY=..., zActual=..., zTarget=...");
```
- `IsZTargetDown`: `target < ActualPosition - 0.000001` (**1µm 하강도 차단**, InPositionTolerance 미적용)
- `CanHomeFrontPickerZ` (F1061 / R1019): **무조건 true** (Z 홈 차단 없음)

### 3-5. 공유 상태검사 유틸 (Front·Rear 동일 로직)

| 함수 | 라인(F/R) | 검사 조건 | 차단 reason |
|---|---|---|---|
| `VerifyFrontPickerZAxesAvoidForMove` | 242/608 | Z0~Z3 모두 Avoid. InspectionZHold·FineAlign 예외 | `"...Z# 축이 Avoid 위치가 아닙니다."` |
| `VerifyFrontPickerZAxesAvoidOrNonNegative` | 645/285 | Z 이동중 차단, Avoid 또는 Actual≥0 | `"...Avoid 또는 0 이상 위치가 아닙니다. actual=..."` |
| `VerifyFrontPickerZAxesAvoid` | 1337/1291 | Z0~Z3 모두 Avoid (엄격) | `"...must be at Avoid position."` |
| `VerifyFrontPickerZAxesHomeOrAvoid` | 1360/1314 | Z0~Z3 각각 Home(0)/Avoid | `"...must be at Home(0) or Avoid position."` |
| `VerifyInputExpanderZAtOrBelowZero` | 680/320 | 이동중 차단, Actual≤tol 또는 ≈WaferZ.Avoid | `"...InputExpandingZ가 0 이하 또는 Avoid 위치가 아닙니다."` |
| `VerifyInputFeederAvoidDog` | 706/346 | FeederY 이동중 차단, X090 ON | `"...InputFeeder Avoid Dog(X090)가 ON이 아닙니다."` |
| `VerifyInputFeederDown` | 727/367 | `IsWaferFeederDown()` | `"...InputFeeder Lift가 Down 상태가 아닙니다."` |
| `VerifyInputVisionXAtAvoidOrBelowZero` | 742/382 | 이동중 차단, Avoid 또는 ≤tol | `"...InputVisionX가 Avoid 또는 0 이하 위치가 아닙니다."` |
| `VerifyGoodStageZAtOrBelowProcess` | 765/405 | 이동중/정보없음 차단, ≤ProcessPos+tol | `"...OutputGoodStageZ가 ProcessPos 이하가 아닙니다."` |
| `VerifyOutputFeederAvoidDog` | 794/433 | 이동중 차단, X091 ON | `"...OutputFeeder Avoid Dog(X091)가 ON이 아닙니다."` |
| `VerifyOutputFeederDown` | 815/455 | `IsFeederDown()` | `"...OutputFeeder Lift가 Down 상태가 아닙니다."` |
| `VerifyOutputVisionXAtAvoidOrBelowZero` | 830/470 | 이동중 차단, Avoid 또는 ≤tol | `"...OutputVisionX가 Avoid 또는 0 이하 위치가 아닙니다."` |
| `VerifyReticleCylinderClear` | 1257/1215 | ReticleLift/FrontSideSlide/RearSideSlide 이동중 차단 | `"ReticleLift is moving."` 등 |
| `VerifyInputVisionXAvoidForPickerX` | 143/509 | 이동중 차단, Avoid 필수 | `"...InputVisionX가 Avoid 위치에 있어야 합니다."` |
| `VerifyVisionXAvoidForColletCalibrationBottomMove` | 183/549 | ColletCal+Bottom일 때 Input/Output VisionX Avoid | `"...VisionX가 Avoid 위치에 있어야 합니다..."` |
| `VerifyFront/RearPickerNotBusy` | 1284/1242 | **전 항목 주석처리 → 항상 true** | (없음) |
| 상대 PickerY Avoid 검사 | 853/493 | **호출부 주석처리(미사용)** | — |

### 3-6. 판정 헬퍼(Boolean 조건자)

| 함수 | 라인(F/R) | 반환 조건 |
|---|---|---|
| `IsColletCalibrationBottomMove` | 232/598 | `Intent.ColletCalibration && Zone==Bottom` |
| `IsInspectionZHoldMove` | 274/640 | `Intent.InspectionZHold && Zone∈{Bottom,Side,Output}` |
| `CanKeep*PickerZDuringYMove` | 287/653 | AxisTeachingMove & (InspectionZHold OR FineAlign) |
| `CanKeep*PickerZDuringXMove` | 301/667 | AxisTeachingMove & ((InspectionContinuous&InspectionZHold) OR FineAlign) |
| `Is*PickerZSafeRetreatTarget` | 1161/1119 | Target≈0(tol) 또는 ≈Z Avoid 티칭값 |
| `Resolve*PickerYAvoidPositionName` | 1178/1136 | Y가 InputAvoid/OutputAvoid/Avoid → 해당 이름, 아니면 "" |
| `IsZTargetDown` | 1199/1157 | `target < Actual - 0.000001` |
| `ResolvePickerZTargetZone` | 1209/1167 | `Intent.PickerZone` 없으면 Unknown |
| `RequiresInputFeederAvoid`/`RequiresInputStageZSafe` | 1219,1235/1177,1193 | Zone∈{Input,Unknown} |
| `RequiresOutputFeederAvoid`/`RequiresOutputStageZSafeForPickerY` | 1227,1243/1185,1201 | Zone∈{Output,Unknown} |
| `IsAxisAtHomeOrTeachingAvoid` | 1384/1338 | \|Actual\|≤tol 또는 티칭 Avoid |
| `IsAxisAtOrAboveZero` | 1402/1355 | `Actual ≥ -tol` |
| `IsInputFeederYHomeOrAvoid` | 1423/없음 | Feeder Home 또는 X090 |
| `IsGoodStageZHomeAvoidOrProcess` | 1433/없음 | ≈0 또는 Avoid/Process |
| `IsExpanderZHomeAvoidProcessOrReady` | 1446/1376 | ≈0 또는 WaferZ Avoid/Process/Ready |

### 3-7. 순수 유틸
`ResolveAxisTolerance`(Config 없으면 **0.05**), `ResolveFront/RearPickerAxis`(PickerAxis enum→BaseAxis), `TryResolvePairedZAxis`(T#→Z#), `TryResolveMovingZAxis`, `LogBlockedReason`(`Log.Write("Main","INTERLOCK","PickerFront/RearInterlock", reason+" - Blocked")`)

---

## 4. 참조하는 외부 상태와 설정값 출처

- **요청**: `request.Machine/MovingName/MoveKind/TargetValue/TargetName`, `Intent.PickerZone/ColletCalibration/InspectionZHold/InspectionContinuous`
- **유닛**: Picker*Unit(축, 티칭), InputStageUnit(CameraX/ExpanderZ/Recipe), OutputStageUnit(OutputCameraX/GoodStage.StageZ/Recipe), Input/OutputFeederUnit(FeederY, X090/X091 Dog, Lift Down), VisionUnit(Reticle 실린더 3종)
- **티칭**: `AvoidPosition`, `InputAvoidPosition`, `OutputAvoidPosition`
- **Recipe**: `WaferZ.Avoid/Process/Ready`, `VisionX.AvoidPosition`, `GoodStageZ.ProcessPosition`
- **위임**: 교차 픽커 점유·안전거리·존 판정은 전부 `PickerZoneInterlockRules`에 위임
- **자재(MaterialStateService) 참조 없음** — 순수 축 위치·실린더·티칭/레시피·존 기반

---

## 5. 상수 / 임계값 목록

| 값 | 의미 |
|---|---|
| **0.05** | Config.InPositionTolerance 없을 때 기본 tolerance |
| **0.000001 (1µm)** | Z 하강 판정 최소 오차 (1µm 하강도 차단) |
| **0.0** | Home 기준 위치 |
| **X090 / X091** | Input/Output Feeder Avoid Dog 센서 |
| MoveKind 매핑 | AxisTeachingMove=Auto / AxisMove=Manual / AxisHome=Home / 그 외 차단 |

---

## 6. 요약: Front vs Rear 비대칭 3줄

1. **Home X**: Front만 InputFeederY(Home/Avoid)·InputFeeder Lift Down 추가 (7 vs 5)
2. **Home Y**: Front만 InputExpandingZ·OutputGoodStageZ 추가 (4 vs 2)
3. **Manual Y**: OutputStageZ 검사와 PickerYMove 룰 실행 순서가 Front/Rear 반대

그 외는 이름 치환 대칭. `Verify*PickerNotBusy`와 상대 PickerY Avoid 검사는 본문/호출부 주석처리로 **항상 통과(비활성)**.
