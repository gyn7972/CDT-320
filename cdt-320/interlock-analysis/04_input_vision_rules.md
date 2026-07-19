# Input측(Cassette/Feeder/Stage) + Vision 인터락 규칙 완전 분석

**공통 구조**: 4개 파일 모두 `static class`이며 `MotionGuardRuleRegistry`에 각각 독립 등록되는 `Verify(MotionGuardRuleContext request, out string reason)` 진입점을 가짐. `true`=허용, `false`=차단(reason에 사유). 공통 헬퍼 `MotionGuardRuleHelpers` 사용, 차단 시 `Log.Write("Main","INTERLOCK",...)`.

**파일 간 관계**: 네 파일은 **서로 직접 호출하지 않음**(각각 독립 등록). Feeder·Stage 두 파일만 외부 `PickerZoneInterlockRules`를 공유 호출.

---

## 1. InputCassetteInterlockRules.cs (238줄) — `InputLifterZ` 전용

### 분기 구조
```
Verify → IsMoving("InputLifterZ") → VerifyWaferLifterZ(MoveKind 분기)
   ├ AxisMove         → CanManualWaferLifterZ
   ├ AxisHome         → CanHomeWaferLifterZ
   ├ AxisTeachingMove → CanAutoWaferLifterZ
   └ default          → BlockUnsupportedMoveKind
```

### 함수별 검사 조건
- **`CanManualWaferLifterZ`**: ① 카세트 웨이퍼 돌출 감지(`IsWaferProtrusionDetected`) → `"InputCassette Jut detected. InputLifterZ home is blocked."` ② `FeederY.IsMoving` → 차단
- **`CanHomeWaferLifterZ`**: ①·② 동일 + ③ 8/12인치 카세트 존재 시(`IsWaferCassetteExist(8|12)`) FeederY 카세트측 안전 티칭 위치 요구:
```csharp
if (Cassette.IsWaferCassetteExist(8) || Cassette.IsWaferCassetteExist(12))
    if (!IsWaferFeederYSafeForWaferLifterZ(feeder))
        return Block("InputLifterZ", "InputFeederY must be at a cassette-side safe teaching position before InputLifterZ move.");
```
- **`CanAutoWaferLifterZ`**: ①돌출 ②FeederY이동 + ③ 카세트 유무 무관하게 항상 FeederY 안전 위치 확인
- **`IsWaferFeederYSafeForWaferLifterZ`**: ⚠️ **현재 강제 `true` 반환 (무력화)**. 원래 로직(Avoid|Exchange|Home) 주석 처리, `// Todo : 추후 FeederY 상태를 재확인 후 조건 수정할 것.` → **조건③은 실질 항상 통과**
- 데드코드: `IsFeederDown`, `IsFeederClamp` (미호출)

### 참조 상태 / 상수
`InputCassetteUnit.IsWaferProtrusionDetected/IsWaferCassetteExist(8,12)`, `InputFeederUnit.FeederY`. 임계값 상수 없음.

---

## 2. InputFeederInterlockRules.cs (787줄) — `InputFeederY / FeederLift / FeederClamp`

### 분기 구조
```
Verify
 ├ "InputFeederY"    → PickerZone.VerifyPickerXStoppedForClearanceMechanismMove 선통과
 │                   → VerifyInputFeederY (Auto/Manual/Home 분기)
 ├ "InputFeederLift" → PickerX 정지 선통과 → VerifyInputFeederLift (CylinderInitialize/CylinderMove)
 └ "InputFeederClamp"→ VerifyInputFeederClamp (Initialize→Move 위임)
```

### `CanAutoInputFeederY` (자동)
1. `InputLifterZ.IsMoving` → `"InputLifterZ is moving. InputFeederY move is blocked."`
2. InputStage: StageT Load/Unload 위치 + ExpanderZ Load/Unload 위치 + InputVisionX Avoid (StageY 검사는 주석 처리)
3. `PickerZone.IsPickerBlockingZoneTransport(machine, true, Input)` (Front) → `"InputFeederY 이동 차단. FrontPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다."`
4. 동일 Rear
5. `IsWaferFeederOverload()` → 차단

### `CanManualInputFeederY` (수동, HOME 계열 취급)
1. InputLifterZ 이동중 2. Front/Rear ZoneTransport(Input) 3. InputVisionX not-homed-or-home 4. FrontPickerX not-homed-or-home 5. RearPickerX 동일 6. `VerifyInputFeederEmptyForHome`(자재 데이터/센서) 7. Overload 8. (실장비) Unclamp 필수 9. (비시뮬) RingCheck 감지 시 차단

### `CanHomeInputFeederY` (홈)
- Picker 검사가 **Home 전용** `IsPickerBlockingZoneTransportForFeederHome(Input)` — 초기화 순서상 Picker 홈 전 X=0/Y=0이 Input으로 잡힐 수 있어 완화 기준 사용 (일반 검사·VisionX/PickerX 홈준비 검사는 주석 처리)
- 이후: 자재 검사, Overload, (실장비)Unclamp, RingCheck — 수동과 동일

### Lift/Clamp
- **`CanInitializeInputFeederLift`** (`direction = targetValue>=0.5 ? Fwd/Up : Bwd/Down`):
```csharp
InputLifterZ.IsMoving → 차단 / FeederY.IsMoving → 차단
!IsFeederUnclamp → "InputFeeder must be unclamped before lift initialize"
targetValue>=0.5 && IsWaferFeederTransferDataOccupied() → "material data exists before lift up."
targetValue>=0.5 && 비시뮬 && IsWaferFeederRingCheck() → "wafer detect sensor is ON before lift up."
```
- **`CanMoveInputFeederLift`**: LifterZ/FeederY 이동중 차단 + `IsFeederUnclamp && IsFeederHoldingMaterial` → `"unclamped and material is still detected"`
- **`CanMoveInputFeederClamp`**: LifterZ/FeederY 이동중 차단만

### 주요 헬퍼
- 위치 판정: `IsAt(axis,target)` = `|Actual-target| <= 0.05` — StageT/ExpanderZ Load·Unload, VisionX Avoid(`Recipe.VisionX.AvoidPosition`)
- **`VerifyInputFeederEmptyForHome`**: `MaterialStateService.GetWaferAtLocation(InputFeeder)` != null → `"material data exists. waferId=...,state=..."` / 데이터 없는데 RingCheck ON → 차단
- **`ShouldBypassHardwareMechanismChecks`**: `settings==null || BypassHardware || SimulationMode || !UseAjin || !AjinFactory.IsRealBoardReady` → 실HW 검사 우회
- 데드코드: `IsFrontPickerInAvoidPosition`, `IsRearPickerInAvoidPosition`, `IsFeederUp`

### 상수
`PositionTolerance = 0.05`, 실린더 방향 경계 `0.5`

---

## 3. InputStageInterlockRules.cs (2332줄) — 핵심 파일, 관리 축 7개

### 분기 구조
```
Verify
 ├ "WaferStageY|StageY|WaferY"       → VerifyWaferStageY (Auto/Manual/Home)
 ├ "WaferStageT|StageT|WaferT"       → VerifyWaferStageT (Auto/Manual/Home)
 ├ "WaferExpandingZ|InputExpandingZ|ExpanderZ"
 │     → PickerZone.VerifyPickerXStoppedForClearanceMechanismMove 선통과
 │     → VerifyWaferExpandingZ (Home=무조건허용 / Manual / Auto=Manual위임)
 ├ "InputVisionX|InputCameraX|CameraX" → VerifyWaferVisionX (Auto/Manual/Home)
 ├ "NeedleX|NeedleBlockX"            → VerifyNeedleX (Auto/Manual/Home)
 ├ "NeedleZ"                         → VerifyNeedleZ (Home=무조건허용)
 └ "EjectPinZ"                       → VerifyEjectPinZ (Jog=무조건허용, Home=무조건허용)
```

### (A) WaferStageY
- **Manual**: ① EjectPinZ 0이하|Avoid ② ExpanderZ Load/Unload 높이면 차단 ③ FeederY Avoid 필수 ④ Feeder Ring/Overload ⑤ 작업영역(`VerifyInputStageWorkArea(WaferY)`) ⑥ Front/Rear `VerifyPickerZAxesAvoidWhenInputRisk` (Input 위험 시 PickerZ0~3 Avoid 강제)
- **Auto** = Manual + FeederY 정지 + StageNotBusy
- **Home**: NeedleZ Home/Safe 필수 + Feeder Ready + Front/Rear PickerZ0~3 Home|Avoid

### (B) WaferStageT
- **Manual**: ① FeederY Avoid ② **EjectPinZ Avoid만 허용**(0 불허 — StageT 회전 특수) ③ ExpanderZ 비Load/Unload ④ Feeder Ready ⑤ PickerZ Avoid 강제(Input 위험 시)
- **Home**: FeederY Home|Avoid, EjectPinZ 0이하|Avoid, 이하 동일

### (C) ExpanderZ
- **Home**: 무조건 허용 (하강 홈)
- **Manual/Auto** (`movingPositive` = 상승 판정):
  1. 상승 시 Front/Rear PickerX/Y 이동중이면 차단
  2. `PickerZone.VerifyPickerZAtOrAboveZeroForZoneStageZMove(Input, movingPositive)` — 상승 시 Input존 PickerZ 0이상|Avoid
  3. FeederY 정지
  4. **StageT 고정 위치 필수**: Home(0)|Avoid|Load|Unload|Ready|Process 중 하나 — `"ExpanderZ 이동 불가: WaferStageT가 Home(0) 또는 티칭된 고정 위치가 아닙니다. actual=... avoid=... load=... unload=... ready=... process=..."`
  5. FeederY Home|Avoid|StageUnload
  6. StageNotBusy
- `IsExpanderZMovingPositive`: 목표가 `Recipe.WaferZ.Avoid` 근처면 false, 아니면 `Target > Actual + tol`

### (D) InputVisionX
- **Manual**: ① Feeder Avoid Dog(X090)+Down(`VerifyInputFeederAvoidAndDownForInputVisionX`) ② ExpanderZ 비Load/Unload ③ Front/Rear Picker Input존 침입 검사
- **Home**: ①만
- **Auto**: Manual + FeederY 정지 + StageNotBusy
- **`VerifyPickerInputZoneClearForInputVisionX`** (방향성 있는 핵심 판정):
```csharp
state = PickerZone.ResolvePickerZoneTransportState(machine, isFront, Input, ...);
bool movingIntoOrInsideInput = IsPickerInputZoneMotionRisk(state, xMoving, yMoving);
// Avoid 복귀 예외: 목표가 Avoid + PickerY Avoid + 정지 + 위험없음 → 허용
if (targetAtAvoid && state.YAvoid && !xMoving && !yMoving && !movingIntoOrInsideInput
    && !state.WorkAreaBlocksTransport && !state.UnknownUnsafe) return true;
bool blocking = state.BlocksTransport || movingIntoOrInsideInput;
if (!blocking) return true;
if (!movingIntoOrInsideInput && IsInputVisionXAvoidOrNegativeDirectionMove(...)) return true;  // 퇴피 허용
return Block("InputVisionX","InputVisionX 이동 불가: {prefix}Picker가 Input 영역을 점유하거나 간섭 중입니다. "+detail);
```
- `IsPickerInputZoneMotionRisk`: 둘 다 정지→false; 이동 중 Current/TargetZone이 Input|Unknown 또는 UnknownUnsafe→true

### (E) NeedleX
- **Manual**: EjectPinZ 0이하|Avoid + ExpanderZ 비Load/Unload + 작업영역 + NotBusy
- **Home**: EjectPinZ 0이하|Avoid + NeedleZ Home/Safe

### (F) NeedleZ
- **Home**: 무조건 허용. **Manual/Auto**: 작업영역 + NotBusy

### (G) EjectPinZ
- **Home/Jog**: 무조건 허용 (작업자 복구용)
- **Manual**: `VerifyEjectPinZManualMoveSafe` — **Avoid 위치 복귀 또는 Avoid에서의 이동만 허용**:
```csharp
bool actualAtAvoid = |actual-pos.Avoid|<=tol; bool targetAtAvoid = |target-pos.Avoid|<=tol;
if (actualAtAvoid || targetAtAvoid) return true;
return Block("EjectPinZ","EjectPinZ 조그/수동 이동 불가: ... Avoid 위치 복귀 또는 Avoid 위치에서의 이동만 허용합니다.");
```
- **Auto**: Manual + FeederY 정지 + (연속조그·Avoid 목표 아니면) 작업영역 + NotBusy

### (H) 공통 검사·헬퍼
- **`VerifyExpanderZNotLoadOrUnloadForStagePlaneMove`**: ExpanderZ가 Load/Unload 높이면 X/Y/T 평면이동 차단 → `"먼저 ExpanderZ를 Avoid 위치로 이동하세요."`
- **`VerifyInputStageWorkArea`**: Needle 작업영역(WorkArea) 판정. WaferY 이동 시 `Intent.InputStageWorkAreaNeedleX`(또는 `InputStageWorkAreaX - NeedleXToVisionXOffset`) 기반 `IsNeedleWorkPointInArea` 판정, NeedleZ Home/Avoid 요구. 일반 축은 `stage.IsInputStageAxisTargetAllowedInWorkArea`
- **`VerifyInputStageNotBusy`** — 동시 이동 상호 배제 (예외 있음):
```csharp
if (!IsNeedleXMove && IsMovingExcept(StageY,...)) → "WaferStageY is moving."
if (IsMovingExcept(StageT,...))                   → "WaferStageT is moving."
if (IsMovingExcept(ExpanderZ,...))                → "ExpanderZ is moving."
if (!IsNeedleXMove && IsMovingExcept(CameraX,...)) → "InputVisionX is moving."
if (!IsInputVisionXMove && !IsWaferStageYMove && IsMovingExcept(NeedleBlockX,...)) → "NeedleX is moving."
// NeedleZ/EjectPinZ 동시 이동은 허용
```
→ PickUp 보정 시 WaferY+NeedleX 동시 이동 허용 예외
- **PickerZ Avoid 강제 계열**: `VerifyPickerZAxesAvoidWhenInputRisk` → `IsPickerInputRiskForZAvoid`(UnknownUnsafe/WorkArea차단/Input존 활성+Y비Avoid) → Z0~3 Avoid 강제. 단 `IsSameAutoPickUpInputStageMove`(WorkAreaOwner가 `FrontPickerPickUpSequence`/`RearPickerPickUpSequence` + `:PickUp`/`:PickUp ContiNode` 접미사, 같은 자동 PickUp 소유자)면 제외
- 데드코드/미사용: `VerifyInputVisionXClearForExpanderZ`(호출 주석), `BuildPickerZoneState`, `ResolvePickerEncoderZone`

### 참조 상태 / 상수
- 축: `StageY/StageT/ExpanderZ/CameraX/NeedleBlockX/NeedleZ/EjectPinZ`; `Recipe.{WaferY,WaferT,WaferZ,VisionX,NeedleZ,EjectPinZ}`
- `VisionUnit.Config.CalibrationData.Needle.NeedleXToVisionXOffset`
- tolerance 기본 **0.05** (NeedleZ 진단은 0.01), Home=0.0
- WorkAreaOwner 문자열: `"FrontPickerPickUpSequence"`/`"RearPickerPickUpSequence"` + `":PickUp"`/`":PickUp ContiNode"`

---

## 4. VisionInterlockRules.cs (402줄) — SideVisionY(F/R) + Reticle 실린더 3종

### 분기 구조
```
Verify
 ├ "FrontSideVisionY(0)" → Auto/Manual/Home(무조건 true)
 ├ "RearSideVisionY(0)"  → 동일
 ├ "ReticleLift"          → CylinderInitialize/Move
 ├ "ReticleSideSlideFront"→ CylinderInitialize/Move
 └ "ReticleSideSlideRear" → 동일
```

### 함수별
- **SideVisionY**: Home 무조건 true. Manual/Auto = Home + `VerifyVisionNotBusy` — SideVisionY는 InputStage와 기구 간섭 없는 독립 검사축
- **`CanInitialize/MoveReticleLift`**:
```csharp
if (!vision.IsVisionReticleFrontSideBackward() || !vision.IsVisionReticleRearSideBackward())
    → Block("ReticleLift","ReticleLift move "+direction+" blocked. ReticleSideSlide Front/Rear가 모두 Backward 상태가 아닙니다.");
```
→ Slide 둘 다 Backward여야 Lift 이동 가능
- **ReticleSlide Front/Rear**: `VerifyVisionNotBusy`만
- ⚠️ **`VerifyVisionNotBusy` 실질 무력화**: 내부 Busy 검사 전부 주석 처리 → 항상 true
- ⚠️ **잠재 결함**: `CanInitialize/MoveReticleLift`에서 `VisionUnit` null 체크 없이 메서드 호출 → VisionUnit null이면 NRE (상위 try/catch가 잡아 차단으로 귀결, 크래시는 아님)
- 데드코드: `VerifyInputStageClear`(호출부 전부 주석), `IsMovingExcept`, `IsCylinderMovingExcept`

### 상수
실린더 방향 경계 `0.5`. 위치 임계값 상수 없음.

---

## 5. 종합 표

| 파일 | 관리 대상 | PickerZone 호출 | 상호 호출 |
|---|---|---|---|
| InputCassette | InputLifterZ | ✗ | ✗ |
| InputFeeder | FeederY, Lift, Clamp | ✓ (X정지 게이트, ZoneTransport, FeederHome) | ✗ |
| InputStage | StageY/T, ExpanderZ, InputVisionX, NeedleX/Z, EjectPinZ | ✓ (X정지 게이트, PickerZ≥0, ZoneTransportState 등) | ✗ |
| Vision | SideVisionY(F/R), ReticleLift/Slide | ✗ | ✗ |

**현재 무력화/주석 처리된 조건 (수정 계획 시 주의)**
- InputCassette: `IsWaferFeederYSafeForWaferLifterZ` 강제 true (Todo 주석)
- InputFeeder: StageY Load/Unload 검사 주석
- InputStage: `VerifyInputVisionXClearForExpanderZ` 미사용, ExpanderZ의 PickerZ Avoid 구식 경로 주석
- Vision: `VerifyVisionNotBusy` 전체 주석(항상 true), `VerifyInputStageClear` 미사용
