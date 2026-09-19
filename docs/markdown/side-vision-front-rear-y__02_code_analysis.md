# Side Vision Front Y / Rear Y 계산 로직 분석

- 작성일: 2026-07-29 (기준 커밋 ba6f0c06)
- 범위: `VisionAxis.FrontSideVisionY` / `VisionAxis.RearSideVisionY` 축의 목표 Y값이 어디서 어떻게 계산되는지 (분석 전용, 코드 수정 없음)

---

## 1. 축과 값 저장소

| 항목 | 위치 |
|---|---|
| 축 등록 | `VisionUnit.cs:199-200` — `FrontSideVisionY0`, `RearSideVisionY0` |
| 티칭값 저장소 | `Recipe.FrontSideVision` / `Recipe.RearSideVision` (`VisionAxisPositions`): `AvoidPosition`, `Process0Position`, `Process90Position` |
| 티칭(저장) | `VisionUnit.cs:671-694` — 버튼 티칭 시 해당 축 `ActualPosition`(=지령값 설계)을 그대로 저장 |
| 조회 | `VisionUnit.GetVisionTeachingPosition` (`VisionUnit.cs:696-703`) |
| 직접 편집 | `VisionRecipePage.cs:355-357` |

**계산식이 존재하는 곳은 레시피 티칭값 자체가 아니라 "런타임 목표값 산출"과 "콜렛Cal Side AF"이다.**

---

## 2. 경로 A — 단독 Side 검사 (`PickerSideInspectionSequence`)

- 카메라 이동: `MoveSideVisionProcessPositionAsync` → `VisionUnit.MoveBothSideVisionProcess0PositionAsync` (`PickerSideInspectionSequence.cs:1019-1058`, `VisionUnit.cs:543-549`)
- **계산 없음.** Front/Rear 두 축 모두 각자의 `Recipe.*.Process0Position`으로 이동.
- 0도/90도 **모두 Process0Position 사용** (주석 `PickerSideInspectionSequence.cs:1033`: "Side 0/90도 모두 동일한 카메라 초점 기준 위치를 사용한다"). `Process90Position` 티칭값은 이 경로에서 미사용.
- 스킵 조건: 두 축 모두 `Process0Position` ± `InPositionTolerance`(기본 0.01) 이내면 이동 생략 (`CanSkipSideVisionProcessMove`, 1060-1088).

---

## 3. 경로 B — Auto Bottom+Side 통합 (`PickerBottomAndSideInspectionSequence`) ← 실제 생산 경로

### 3.1 최종식

```
FinalY(카메라, 각도) = BaseY(카메라, 각도, pickerNo) + BottomMResultOffsetY
```

- 조립: `BuildSideVisionPositionTarget` (`:2188-2209`) → `Process0Y = Process0BaseY + offset0`, `Process90Y = Process90BaseY + offset90`
- 이동: `MoveSideVisionProcessPositionAsync(target, angle)` (`:2883-2931`) — **Front/Rear 두 카메라 병렬(Task.WhenAll)**, 각자 자기 `Process0Y`/`Process90Y`로. 0도 이동은 Side X 진입과 병렬(`:2833-2835`), 90도 이동은 T 90도 회전과 병렬(`:2867-2869`).
- 스킵: 축이 이미 targetY ± tolerance(기본 0.05) 이내 (`IsSideVisionAxisProcessPositionReady`, `:2996-3011`)
- 이동 직전 상세 로그: "SideVisionY 보정 이동" (`:2951-2966`) — process0TeachingY / focusCalBaseY / focusCalValid / offsetY / finalY 전부 찍힘.

### 3.2 BaseY 결정 — `ResolveSideVisionBasePosition` (`:2226-2287`), 우선순위 3단계

카메라(Front/Rear) × 각도(0/90) × 콜렛(pickerNo)별로:

1. **Side AF 레코드 유효 시**: `FocusCalibration.GetSideRecord(kind, pickerNo).BestPosition`
   - kind는 **카메라 쪽 기준**: Front 카메라 → `FrontSide0/90Collets[pickerNo-1]`, Rear 카메라 → `RearSide0/90Collets[pickerNo-1]`
2. **AF 없고 COC 데이터 있으면 폴백**: `fallbackY = Process0티칭Y + axisSign × correction` (`:2261-2262`)
   - `axisSign`: Front 카메라 = **+1**, Rear 카메라 = **-1**
   - 0도: `correction = coc0Sign × cocYmm` (`:2327-2328`)
   - 90도: `correction = size90Sign × (DieSizeX − DieSizeY)/2 + coc90Sign × cocXmm` (`:2351-2354`)
   - `cocXmm/cocYmm`: **검사 중인 피커 쪽** 콜렛Cal 회전중심 픽셀(`RotationCenterPixelX/Y`)을 Bottom 카메라 픽셀Cal로 mm 변환
     - `PixelToMmOffsetX = (px − ImageCenterPixelX) × PixelToMmX`, `PixelToMmOffsetY = (ImageCenterPixelY − py) × PixelToMmY` (`VisionCameraCalibrationData.cs:109-117`)
   - 다이 사이즈: 레시피 `InputFrame`(없으면 `Frame`)의 `DieSizeX/Y` → 폴백 `Controller.DieSizeX/YMm`
   - 부호 파라미터(설정 저장, 다이얼로그 편집 가능, `VisionFocusCalibrationData.cs:216-222`, 기본값 `:262-273`):
     `Size90Sign Front=-1 / Rear=+1`, `Coc0Sign F/R=+1`, `Coc90Sign F/R=+1` — "실장비 테스트로 확정, 0이면 항 비활성"
3. **둘 다 없으면**: 그냥 `Process0Position` 티칭값.

### 3.3 런타임 OffsetY — Bottom MRESULT 연동

- 출처: Bottom 검사 MRESULT의 `bottom_offset_y_mm` (`TryResolveBottomMResultSideVisionOffsetY`, `:1975-2006`)
- 저장: `StoreRuntimeSideInspectionCorrection` (`:1865-1913`) — **0도/90도에 같은 값**을 가산, Picker X/Y/Z/T에는 미적용.
- 유효조건 (`BuildSideTarget :2107-2114`, `ValidateRuntimeSideInspectionCorrection :1915-1963`):
  - 보정의 `SourceDieId` == 현재 피커의 DieId (Ordinal 일치)
  - `|OffsetY| ≤ 7.0mm` (`MaxSideVisionCenterCorrectionMm`, `:17`)
  - 무효면 offset 0으로 진행하지만, Validate 단계에서는 실패 코드(`PICKER-BOTTOM-SIDE-MRESULT-OFFSET-*`)로 차단.

---

## 4. BestPosition의 출처 — 콜렛Cal Side AF

`ColletCalibrationSequence` COC 완료 후, `RunSideAutoFocusAfterCoc` 설정 시 0도/90도 AF 실행 (`:1975-1993`).

- **AF 시작 Y**: `defaultY = Process0티칭Y + axisSign × focusCorrection` (`:2378`) — focusCorrection은 3.2 폴백과 **동일식** (`TryBuildSideFocusCorrectionFromCoc :2137-2217`; Bottom 재측정 기반 구식 `TryBuildSideFocusCorrection :2219-2281`은 현재 본선에서 미호출)
- **AF 시 PickerZ** 우선순위 (`:2326-2375`): ① `UseBottomToSideZOffset`이면 Bottom Die AF BestZ + `BottomToSideZOffsetMm` (없으면 콜렛Cal FinalPickerZ + 옵셋) ② 저장된 Side AF PickerZ(BestScore>0일 때만) ③ `SidePosition` 티칭
- **스캔**: `VisionFocusScanSequence` — defaultY 중심 ±Minus/PlusRange를 Step으로 러프 스캔 → 비전 `FOCUS_BEST` 응답(BestZ/BestScore) → BestZ 중심 Fine 스캔 → `SaveBestStep`(`:1952-1997`)에서 `record.ApplyBest(DefaultPosition, BestPosition, BestScore, ...)` + 촬영 당시 PickerZ 저장(score>0일 때만).
- 저장 단위: kind(FrontSide0/90, RearSide0/90) × colletNo(1~4). AF는 자기 쪽 카메라만 스캔 (Front 피커 캘리 → Front 카메라만).

### 관찰(구현 시 인지할 점)

- 런타임에서 **양쪽 카메라를 모두** 움직이는데, Front 카메라 레코드는 Front 피커 콜렛 캘리에서, Rear 카메라 레코드는 Rear 피커 콜렛 캘리에서 만들어진다. Rear 피커 검사 중 Front 카메라 BaseY는 "Front 피커 colletNo 캘리 값"을 그대로 쓴다(콜렛 대칭 가정). 계산식 수정 시 이 교차 참조를 깨지 않도록 주의.
- `ResolveSideVisionTargetY`에서 target 없을 때 pickerNo=1로 폴백 (`:2380-2393`).

---

## 5. 구현 시 준수 사항

1. **MotionSpeedScale**: `VisionUnit.MoveVisionAxis`는 `GetDefaultVel/Acc/Dec()`로 **스케일 1회 적용 완료값**을 사용 (`VisionUnit.cs:1299-1327`, 2026-07-26 정정 주석). 새 이동 코드는 이 API를 재사용할 것 — Config 원시값 직접 사용 금지, 스케일 중복 적용 금지.
2. **ActualPosition = 지령 반환 설계**: 티칭/AF 저장의 ActualPosition은 지령값 캡처다. 교정 금지.
3. **로그 계측**: 기존 "SideVisionY 보정 이동" 로그(`:2951-2966`)가 base/offset/final을 전부 남긴다. 신규 계산도 같은 수준으로 발행/스킵/실패 사유를 남길 것.
4. 설정 영속화: AF/부호/옵셋은 `VisionUnit.Config.FocusCalibration` → `VisionUnit.SaveSettings()`. Setup 계열 파일은 앱 종료 시 덮어써지는 규칙 유의.
