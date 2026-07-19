# Output측 인터락 규칙 + 공통 인프라 + 실시간 충돌 감시 분석

공통 규약: 규칙 차단은 모두 `MotionGuardRuleHelpers.Block(movingName, message, out reason)`을 거치며 최종 reason은 `Interlock blocked. moving=<movingName>. <message>` 형식.

---

## A그룹 — Output 인터락 규칙

### 1. OutputCassetteInterlockRules.cs — `OutputLifterZ` 전용

```
Verify → IsMoving("OutputLifterZ","ElevatorZ_Output") → VerifyBinLifterZ
   ├ AxisMove / AxisHome  → CanHomeBinLifterZ
   ├ AxisTeachingMove     → CanMoveBinLifterZ
   └ default              → BlockUnsupportedMoveKind
```

**`CanHomeBinLifterZ` / `CanMoveBinLifterZ`** — 동일 3단계 (문구만 home/move 차이):
1. `cassette.IsBinProtrusionDetected()` → `"OutputCassette bin protrusion detected. OutputLifterZ home(/move) is blocked."`
2. `feeder.FeederY.IsMoving` → 차단
3. `!IsOutputFeederYSafeForOutputLifterZ(feeder)` → `"OutputFeederY must be at a cassette-safe position before OutputLifterZ home(/move)."`

**`IsOutputFeederYSafeForOutputLifterZ`**: FeederY가 Avoid이거나, Good/Ng 각 side의 CassetteLoad/CassetteUnload/StageLoad/StageLoadAvoid/StageUnload/StageUnloadAvoid 위치 중 하나면 안전.

### 2. OutputFeederInterlockRules.cs — `OutputFeederY / Lift / Clamp` (24개 함수)

라우팅: FeederY·Lift는 `PickerZone.VerifyPickerXStoppedForClearanceMechanismMove` 선통과 필수, Clamp는 없음.

**`CanAutoOutputFeederY`**:
1. `OutputLifterZ.IsMoving` → 차단 2. OutputVisionX Avoid 아니면 차단 3. Front/Rear `IsPickerBlockingZoneTransport(Output)` → `"...Picker가 Output zone을 사용 중이거나 위치를 확정할 수 없습니다."` 4. `IsFeederOverload()` → 차단

**`CanManualOutputFeederY`** (13단):
1. Front/Rear ZoneTransport(Output) 2. OutputVisionX not-homed-or-home 3~4. Front/RearPickerX not-homed-or-home 5. OutputLifterZ 이동중 6. OutputLifterZ Avoid 필수 7. GoodStageZ Avoid 필수 8. Good Bin Guide Down 센서(비 DryRun) 9. `VerifyOutputFeederEmptyForHome`(자재/링) 10. Overload 11. (실장비) Unclamp 12. (실장비) Feeder Up 13. (비시뮬) RingCheck

**`CanHomeOutputFeederY`**: Picker 검사가 Home 전용 `IsPickerBlockingZoneTransportForFeederHome(Output)` 완화 기준. GoodStageZ는 **Home(0)|Avoid** 허용(수동은 Avoid만). 이후 Guide Down/자재/Overload/Unclamp/Up/Ring 동일.

**`VerifyOutputFeederEmptyForHome`**: `MaterialStateService.GetWaferAtLocation(OutputFeeder)` 존재 → 차단 / 데이터 없는데 `IsBinFeederRingCheck()` ON → 차단.

**Lift**: `CanInitializeOutputFeederLift` — LifterZ/FeederY 이동중 차단, Unclamp 필수, Up(≥0.5) 시 자재 데이터/빈 감지 센서 검사. `CanMoveOutputFeederLift` — 이동중 차단 +:
```csharp
if (IsFeederUnclamp(feeder) && !feeder.IsFeederTransferDataEmpty())
    → Block("OutputFeederLift","... OutputFeeder is unclamped, so feeder is assumed to be holding material.");
```
**Clamp**: LifterZ/FeederY 이동중 차단만.

헬퍼: `IsFeederUp`(센서 or 실린더 IsFwd), `IsFeederUnclamp`(센서 or IsBwd), `ShouldBypassHardwareMechanismChecks`(BypassHardware|Simulation|!UseAjin|!IsRealBoardReady). 미사용 헬퍼: `IsFrontPickerInOutputZone`, `IsRearPickerInOutputZone`, `IsStageModuleAtAvoid`.

### 3. OutputStageInterlockRules.cs — GoodStageY/Z, NGStageY, OutputVisionX + BinGuide 실린더 6종 (40개 함수)

라우팅:
- `OutputGoodStageY` → VerifyBinGoodY / `OutputGoodStageZ` → **PickerX 정지 선통과** 후 VerifyBinGoodZ / `OutputNGStageY` → VerifyBinNgY / `OutputVisionX` → VerifyBinVisionX
- 실린더 6종(GoodBinGuideLift, GoodBinGuideClampLift, GoodBinGuideClamp, NGBinGuideLift, NGBinGuideClampLift, NGBinGuideClamp) → VerifyOutputStageCylinder

**GoodStageY** — `VerifyGoodStageYMechanicalClear` 핵심:
```csharp
if (!outputStage.IsNgStageInAvoidPosition())
    → "GoodStageY 이동 전 NG Stage가 반드시 Avoid 위치여야 합니다."
bool requiresGoodZAvoid = request==null || MoveKind==AxisHome ||
                          IsGoodStageYTargetRequiringGoodZAvoid(TargetValue);  // 목표가 Avoid/Load/Unload
if (requiresGoodZAvoid && !IsGoodStageZAtAvoid())
    → "GoodStageY Load/Avoid/Unload/Home 이동 전 OutputGoodStageZ가 반드시 Avoid 위치여야 합니다."
if (!requiresGoodZAvoid && !IsGoodStageZInAvoidOrProcessPosition())
    → "GoodStageY 공정 이동 전 OutputGoodStageZ는 Avoid 또는 Process 위치여야 합니다."
```
- Auto: 위 + NgClamp Lift Up + FeederY Avoid + Ring/Overload + TransportClear + NotBusy
- Manual: MechanicalClear + NgClamp + FeederY Home|Avoid + Ring + Unclamp + Overload
- Home: HomeMechanicalClear + NgClamp + FeederY Avoid + Ring + Unclamp + Overload

**GoodStageZ**:
- Manual: ① NgClamp Lift Up ② 목표가 Avoid 아니면 NG Stage Avoid 필수 ③ `PickerZone.VerifyPickerZAtOrAboveZeroForZoneStageZMove(Output, movingPositive)` — 상승 시 Output존 PickerZ 0이상|Avoid ④ FeederY Avoid ⑤ Ring/Unclamp/Overload
- Auto = Manual + TransportClear + NotBusy / Home: NgClamp+Ring+Unclamp+Overload
- `IsGoodStageZMovingPositive`: `Target > Actual + tol` (tol 기본 0.01)

**NGStageY**:
- Auto: Guide Down 센서 + NgClamp Lift Up + TransportClear + FeederY Avoid + Ring/Overload + **GoodStageZ Avoid 필수** + NotBusy
- Manual: GoodStageZ Avoid → HW 센서 갱신(`RefreshRequiredHardwareInput`) → Guide Down → NgClamp → FeederY Avoid → Ring/Unclamp/Overload
- Home: GoodStageZ **Home(0)|Avoid**, FeederY Home|Avoid

**OutputVisionX**:
- Home = `VerifyOutputFeederAvoidAndDownForOutputVisionX`: FeederY 정지 + Avoid Dog(**X091**) ON + Feeder Lift Down
- Manual = 위 + Front/Rear Picker Output존 침입 검사 / Auto = Home + Picker검사 + TransportClear + NotBusy
- **`VerifyPickerOutputZoneClearForOutputCameraX`** 핵심:
```csharp
bool movingIntoOrInsideOutput = IsPickerOutputZoneMotionRisk(state, xMoving, yMoving);
bool blocking = state.BlocksTransport || movingIntoOrInsideOutput;
bool outputCameraRetreat = IsOutputCameraXAvoidOrPositiveDirectionMove(...);  // +방향 or Avoid 이상 유지
if (!blocking) return true;
if (!movingIntoOrInsideOutput && outputCameraRetreat) return true;   // 퇴피 허용
return Block("OutputVisionX","OutputCameraX 이동 불가: "+prefix+"Picker가 Output 영역을 점유하거나 간섭 중입니다. "+detail);
```

**실린더 6종**: Initialize/Move 모두 TransportClear + NotBusy만.

**공통 헬퍼**:
- `VerifyNgClampSafeForStageMove`: NG Bin Clamp **Lift Up** 필수 (Clamp/Unclamp 상태는 조건 미포함)
- `VerifyOutputTransportClear`: FeederY/OutputLifterZ 이동중 차단
- `VerifyOutputStageNotBusy`: GoodStageY/Z, NgStageY, OutputCameraX 중 자기 외 이동중 차단
- `TryGetNgStageMaterialPresence` (public): NgBinRingSensor + MaterialStateService(OutputStageNg)로 자재 유무
- `RefreshRequiredHardwareInput`: 실HW 모드(`UseAjin && !Simulation && !DryRun`)에서 센서 미등록/시뮬 잔존/HW read 실패 시 차단
- 미참조: `IsNgStageYAvoidTarget`, `IsGoodStageZLoadOrUnloadTarget`

**상수**: tolerance 기본 `0.01`(Output측; Input측 0.05와 다름), `IsNgStageYAvoidTarget` 고정 `0.001`, 실린더 경계 `0.5`, X091.

---

## B그룹 — 공통 인프라

### 4. InterlockCheckMatrix.cs — 축↔축/실린더 체크 쌍 카탈로그

- `InterlockTargetKind`: Axis / Cylinder. `InterlockCheckPair`: MovingName/CheckName/SourceCell(엑셀 셀)
- **CylinderKeys 13종**: InputFeederLift, InputFeederClamp, ReticleLift, ReticleSideSlideFront/Rear, GoodBinGuideLift, GoodBinGuideClampLift, GoodBinGuideClamp, NGBinGuideLift, NGBinGuideClampLift, NGBinGuideClamp, OutputFeederLift, OutputFeederClamp
- **NormalizeName 별칭**: FeederY→InputFeederY, StageY/WaferY/InputStageY→WaferStageY, StageT/WaferT/InputStageT→WaferStageT, ExpanderZ/InputExpandingZ→WaferExpandingZ, CameraX→InputVisionX, NeedleBlockX→NeedleX, GoodBinY→OutputGoodStageY, GoodBinZ→OutputGoodStageZ, NgBinY→OutputNGStageY, FrontSideVisionY0→FrontSideVisionY 등 + 엑셀 표기 실린더명 매핑(`Feeder Up/Down→InputFeederLift` 등)
- 원본: `CDT-320_Interlock_20260604_vertical_check.xlsx` (docs의 IO LIST와 함께 계약 문서)

**기본 체크 쌍 (총 111쌍)** — Moving별 요약:

| Moving | Check 대상 |
|---|---|
| InputLifterZ | InputFeederY, Feeder Up/Down, Feeder Clamp, InputVisionX |
| InputFeederY | InputLifterZ, Feeder Up/Down, Feeder Clamp, WaferY, WaferT, WaferExpandingZ |
| Feeder Up/Down | InputLifterZ, InputFeederY, Feeder Clamp, WaferY, WaferT, WaferExpandingZ |
| Feeder Clamp/UnClamp | InputLifterZ, InputFeederY, Feeder Up/Down, WaferY, WaferExpandingZ |
| WaferY | InputFeederY, Feeder Up/Down, Feeder Clamp, WaferExpandingZ, NeedleX, NeedleZ, EjectPinZ |
| WaferT | InputFeederY, Feeder Up/Down |
| WaferExpandingZ | InputFeederY, Feeder Up/Down |
| InputVisionX | InputFeederY, FrontPickerX, RearPickerX |
| NeedleX | WaferY, NeedleZ, EjectPinZ |
| NeedleZ | WaferY, NeedleX |
| EjectPinZ | WaferY, NeedleX |
| Reticle Up/Down | Reticle Front/Back Slide, Front/RearPickerZ0~3 (10개) |
| Reticle Front FW/BW | Reticle Up/Down, Front/RearPickerZ0~3 (9개) |
| Reticle Back FW/BW | Reticle Up/Down, Front/RearPickerZ0~3 (9개) |
| FrontPickerX | InputFeederY, InputVisionX, Reticle 3종, RearPickerX |
| FrontPickerY | Reticle 3종, RearPickerY |
| NgBinY | Front/RearPickerZ0~3, GoodBin Guide/Clamp 3종, NgBin Guide/Clamp 3종, OutputFeederY/Lift/Clamp (17개) |
| OutputVisionX | FrontPickerX, RearPickerX |
| OutputFeederY | FrontPickerX, RearPickerX, OutputVisionX, OutputFeeder Lift/Clamp, OutputLifterZ |
| OutputFeeder Up/Down | OutputFeederY, Clamp, OutputLifterZ |
| OutputFeeder Clamp | OutputFeederY, Lift, OutputLifterZ |
| OutputLifterZ | OutputFeederY, OutputFeeder Lift/Clamp |

(전체 셀 좌표 포함 원본 테이블은 `InterlockCheckMatrix.CreateDefaultPairs()` 참조)

> 주의: 이 매트릭스는 **직접 차단에 쓰이지 않고** `MotionGuardResult.RequiredChecks`로 로그(`- Check`)에만 표시된다. 실제 차단은 11개 규칙 함수가 수행. 매트릭스는 "무엇을 검사해야 하는가"의 계약 카탈로그.

### 5. InterlockCheckMatrixStore.cs
- 경로: `{BaseDirectory}\Config\interlock-check-matrix.json`
- `LoadOrDefault()`: 파일 없으면 기본값 생성, 실패 시 `InterlockCheckMatrix.Default` fallback
- `Version=1`, `Source="CDT-320_Interlock_20260604_vertical_check.xlsx"`

### 6. MotionGuardMoveIntent.cs — targetName 파싱 intent 토큰

`;` 구분 토큰. `key=value`는 딕셔너리, 단순 토큰은 HasToken.

| 프로퍼티 | 파싱 소스 | 의미 |
|---|---|---|
| `AutoSequence` | 토큰 | 자동 시퀀스 이동 |
| `AutoProcessCorrection` / `AutoProcessCorrectionMax` | 토큰 / `=<double>` | 자동 공정 보정 이동 / 최대 이동량 |
| `InspectionContinuous` | 토큰 | 연속 검사 이동 |
| `InspectionZHold` | `PickerPhase=InspectionZHold` | 검사 중 Z 유지 |
| `ColletCalibration` | 문자열 포함 | 콜렛 캘리브레이션 |
| `ContinuousJog` | Normalized 일치 | 연속 조그 |
| `PickerZone` | `PickerZone=...` 또는 이름 추론 | 픽커 작업 존 |
| `InspectionFromZone` / `InspectionToZone` | `From=` / `To=` | 검사 존 전환 |
| `InputStageWorkAreaX` / `InputStageWorkAreaNeedleX` | `=<double>` | InputStage 작업영역 X |

**PickerZone 추론 규칙** (우선순위): `PickerZone=` 명시 → InputAvoidPosition→Input / OutputAvoidPosition→Output → DiePick/PickPosition→Input, DiePlace/PlacePosition→Output → Process/Inspect→Bottom → DieBottom/BottomPosition/INSPECT_B→Bottom → DieSide/SidePosition/INSPECT_S→Side → AvoidPosition/SafeRetreat→Avoid → Unknown

### 7. MotionGuardRuleHelpers.cs — 공통 헬퍼

상수: `DefaultPositionTolerance = 0.05`, ColletFineAlign 최대 이동 fallback `0.2`.

| 함수 | 역할 |
|---|---|
| `IsMoving(request, names)` | NormalizeName 후 MovingKey/MovingName 일치 |
| `Block(moving, msg, out reason)` | `"Interlock blocked. moving=<>. <msg>"` + false |
| `BlockUnsupportedMoveKind` | Block + `AlarmManager.Raise(Error,"MOTION-GUARD","INTERLOCK",...)` |
| `IsAt(axis, target[, tol])` | `\|Actual-target\| <= tol` |
| `IsAxisNotHomedOrAtHomePosition` | 이동중=false / 미홈=true / Home(0)±tol=true / 홈완료+비홈=false |
| `IsCylinderMoving` | `!IsFwd && !IsBwd` (중간 상태) |
| `IsSafeTeachingTarget` | Avoid/Ready/Safe/Home/Exchange 계열 이름 |
| `IsReticleRetracted` | Down + FrontBack + RearBack |
| `VerifyReticleRetractedBeforePickerZWorkMove` | PickerZ 공정/하강 전 Reticle 완전 후퇴 필수 (Home/안전 티칭/후퇴 목표는 예외) |
| `IsColletCalibrationFineAlignMove` | ColletCalibrationFineAlign + TeachingMove + Bottom존 + WorkArea owner=ColletCalibration + 이동량 ≤ FineAlignMaxXyMoveMm(기본 0.2) |
| `ResolveTolerance(axis)` | InPositionTolerance>0 ? 값 : 0.05 |

### 8. SharedRailXInterlockRules.cs (전체 1.3KB) — 위임 껍데기

통과 조건: request/Machine null, MoveKind가 AxisMove/AxisTeachingMove 아님(Home/Jog 제외), `SkipSharedRailXRule`, `SharedRailXMotionRuntime.IsInternalDispatch`, 서비스 미해석, 축 해석 실패.
그 외 → **`SharedRailXMotionService.VerifySingleAxisMove(axis, targetValue, out reason)`로 위임** (실제 X레일 충돌 판정·임계값은 `Equipment\Motion\SharedRailX\` 모듈에 존재).

---

## C그룹 — 실시간 충돌 감시 (MotionGuard와 독립)

### 9. RealtimeCollisionSupervisor.cs

Front/Rear Picker의 마주보는 X 거리를 백그라운드 태스크로 감시, 위험 시 **사후 하드정지** (사전 게이트 아님 — `CanMove()`는 항상 Allow).

**상수/임계값**:
| 상수 | 값 |
|---|---|
| `DefaultMonitorPeriodMs` | 100 (Start 인자로 override, 최소 5ms — Form1은 10ms로 기동) |
| `RiskLogThrottleMs` / `StateLogThrottleMs` | 10000 / 30000 |
| `HardStopRepeatStopMs` / `HardStopRepeatAlarmMs` | 250 / 1000 |
| clearance / out distance fallback | 150.0 / 1.0 (Setup 값 없을 때) |

**판정 로직** (`EvaluateRealtime`):
```csharp
bool bothYNotRetracted = IsYNotRetracted(front.YState) && IsYNotRetracted(rear.YState);
bool xPathUnsafe = DoesXPathEnterFacingClearance(front, rear, pair.RequiredClearance);
if (bothYNotRetracted && xPathUnsafe)
{
    reason = "실시간 충돌 감시 정지. Front/Rear PickerY가 둘 다 안전 위치가 아닌 상태에서 " +
             "PickerX 거리가 안전거리 안으로 들어옵니다. " + ...;
    RaiseHardStop(reason);   // 전축 EStop 핸들러 or 폴백 PickerX/Y EStop
}
```
- `IsYNotRetracted`: YState가 Forward/Moving/**Unknown**이면 위험(Retracted만 안전)
- `ResolveYState`: 이동중=Moving, `IsPickerYSafeByPosition`(Home(0)±out거리 또는 AvoidPosition)이면 Retracted, 아니면 Forward
- `DoesXPathEnterFacingClearance`: 현재 `|frontX-rearX| <= clearance` 즉시 true; 이동 중이면 이동 구간을 clearance 확장하여 겹침 검사
- 알람: `AlarmManager.Raise(Critical, "PICKER-FACING-X-INTERLOCK", ...)`, EStop 250ms 반복, 알람 1000ms 스로틀
- 시뮬 모드에서는 `UpdateStatus` 미호출(레이스 방지), 실장비만 호출
- 정지 핸들러: `SetStopAllAxesHandler`로 등록된 전축 정지(Form1에서 등록), 폴백은 Picker X/Y EStop

### 10. CollisionSafetyTypes.cs
- `CollisionGateDecision`(Allow/Wait/Block), `PickerSafetySide`(Front/Rear), `PickerSafetyPhase`(Idle/PickUp/BottomInspect/SideInspect/Place/Retracting), `PickerSafetyYState`(Unknown/Retracted/Forward/Moving)
- `PickerSafetySnapshot`/`AxisPairSafetySnapshot`/`MotionSafetyState` — 감시 스냅샷 DTO

### 11. CollisionDistanceCalculator.cs
SharedRailX 설정 기반 축 쌍 clearance 계산:
```csharp
ActualClearance = homeClearance - (axisATowardSign * posA) - (axisBTowardSign * posB);
```
- pair 미설정 시 `Configured=false` + `"SharedRailX 거리 pair 설정이 없습니다. HomeClearance/sign 등록이 필요합니다."`
- `required = pair.SafetyDistance ?? fallback`

---

## 종합 메모

1. A그룹 → `PickerZoneInterlockRules` 의존이 강함 (X정지 게이트, ZoneTransport, PickerZ≥0)
2. `InterlockCheckMatrix`는 차단이 아니라 로그용 체크 카탈로그 (원본 엑셀 `CDT-320_Interlock_20260604_vertical_check.xlsx`)
3. SharedRailX 룰(사전 게이트)과 CollisionDistanceCalculator(스냅샷 계산)는 같은 pair/clearance 개념 공유
4. RealtimeCollisionSupervisor는 사전 차단이 아닌 **사후 정지** — MotionGuard가 뚫려도 마지막 안전망
5. Output측 tolerance 기본값(0.01)이 Input측(0.05)과 다름 — 수정 계획 시 통일 여부 검토 대상
6. 미사용 헬퍼: OutputFeeder의 `Is*PickerInOutputZone`/`IsStageModuleAtAvoid`, OutputStage의 `IsNgStageYAvoidTarget`/`IsGoodStageZLoadOrUnloadTarget`
