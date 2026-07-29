# AF 기반 PickerZ 공정높이 개편 — 분석 + 설계 (Stage 1~4 압축본)

- 작성일: 2026-07-29
- 기준 소스: D:\Source\CDT-320_New (master, 8340c088)
- 상태: **분석/설계만. 코드 수정 없음. 팀장님 승인 대기.**

---

## 1. 요구사항 (팀장님 지시 요약)

| ID | 요구 | 내용 |
|----|------|------|
| R-01 | 콜렛 AF 산식 | 콜렛 AF Best Z + Rim Collet Offset(-0.5) + Die Thickness(0.25) + **Bottom to Pick(신설)** = PickerZ 공정 높이 |
| R-02 | Collet AF Z Offset 제거 | 스크린샷의 COLLET AF Z OFFSET(5) 그룹(Limit + Collet1~4) 및 델타 누적 체계 전부 삭제 |
| R-03 | 다이 AF 산식 | 다이 AF Best Z + **Bottom to Pick** = PickerZ 공정 높이 (Rim/두께 가산 없음 — 이미 다이 바닥면 포커스이므로) |
| R-04 | 헤더별 Overdrive | Picker Header 당 Overdrive 신설, 각 픽커 Z 목표에 가산 |

---

## 2. 현재 코드에서 "AF로 계산해서 수정하는 곳" 전수 목록

### A. 콜렛 AF (Collet Calibration 내 Bottom AF)
`Sequencing\Calibration\ColletCalibrationSequence.cs`
- 시작 Z = Vision Focus Cal의 **Bottom Collet Best Focus 등록값** (`TryResolveBottomColletFocusStartPosition`, 없으면 Fail) — L326~333
- `RunAutoFocusIfNeededAsync`(L394, `RunAutoFocusAfterTheta` 설정) → AF Best Z = `FinalPickerZ`
- `ResolveColletInspectionTeachingZ`(L3124~3166): **검사티칭Z = BestZ + DieCalThicknessMm + FilmThicknessMm + ColletOffset(Rim이면 RimOffsetFromFlatMm)**
- `ComputeAndApplyAfZOffset`(L1628~1706): delta = 새 검사티칭Z − 기존 BottomPosition 티칭 → `Recipe.ColletAfZOffset[collet]`에 **누적**, |누적|>Limit(0.3) fail-closed, stale 플래그, 실패 롤백(L1778~1812, 티칭 스냅샷 L1712~1773)
- `SaveColletCalibration`(L1814~): 위 offset 누적 + Bottom/Side/DieBottom/DieSide **검사 티칭 동기화** + Recipe 저장(원자적)
- COC 후 Side 0°/90° AF (`RunCocAndSideAutoFocusAsync`) — Side 검사 Z 계열(이번 개편 대상 아님)

### B. 다이 AF (생산 중 Bottom Die AutoFocus)
`Sequencing\Picker\PickerSequenceBase.cs`
- `RunBottomRuntimeAutoFocusIfNeededAsync`(L261~) → `VisionFocusScanSequence(Kind=BottomDie)` → bestZ
- `AccumulateColletAfZOffsetFromRuntimeAf`(L3729~3807): delta = bestZ − 기존 Bottom 포커스 기준선 → **ColletAfZOffset 누적**, limit fail-closed, stale
- `ApplyRuntimeBottomFocusPosition(pickerIndex, bestZ)`: Bottom 기준선 갱신, 실패 시 보상 롤백(`RollbackRuntimeAfOffsetAndBaseline`, L3609~3644)
- 수동 다이 AF: `Ui\Dialogs\VisionFocusCalibrationDialog.cs` (BottomDie 스캔 Apply/Save → 기준선 갱신)

### C. 공정 소비처 (ColletAfZOffset이 실제 Z에 가산되는 곳)
- **Pick(오토)**: `PickerPickUpSequence.PickTargets.cs` L525~536 — `_targetPickerZ = coordinate.PickerZ + colletAfZOffset` (유일 대입점, 파생 이동/검증/배치 저장·복원에 자동 전파). `coordinate.PickerZ`의 원천 = **PickPosition 티칭**(`PickerMotionTargetResolver.cs` L138~142)
- **Pick(수동 Verify)**: `PickerPickUpSequence.PickVerifyManual.cs` L493~497 — PickPosition 티칭 + offset
- **Place**: `PickerPlaceSequence.PlaceTargets.cs` L159~165 — `coordinate.PickerZ + PlaceZOverDrive + colletAfZOffset` (ContiPlace는 여기서 파생 + `ContiOverDrive`)
- 공용 조회: `PickerSequenceBase.ResolveColletAfZOffset`(L3575~3603, limit fail-closed)

### D. Z 캘리브레이션 연동
- `PickerPickUpZCalibrationSequence.cs` L1657 / `PickerPlaceZCalibrationSequence.cs` L1926 — `ResetColletAfZOffsetAfterZCalibration`(PickerSequenceBase L3655~3720): Z캘 저장 시 잔존 offset을 반대편 티칭에 폴딩 후 0 리셋 + stale 세움

### E. 데이터/UI
- `PickerFrontUnit.cs` L555~556 / `PickerRearUnit.cs` L321~322 — `Recipe.ColletAfZOffset[4]`, `ColletAfZOffsetLimitMm`, (+`ColletAfZBaselineStale[4]`)
- `Front/RearPickerRecipePage.AddColletAfZOffsetItems` — 스크린샷의 "COLLET AF Z OFFSET (5)" 그룹
- `ColletCalibrationDialog(.Designer).cs` — "AF Z OFS" 표시 컬럼
- `ColletCalibrationData.cs` L111 — `Record.AfZOffset`
- 기존 파라미터: `RecipeStore.cs` ColletZ — `RimOffsetFromFlatMm`(Rim Collet Offset), `DieCalThicknessMm`(Die Thickness), `FilmThicknessMm`, `FlatZOffsetMm`
- 기존 Overdrive: `PickerTransferTypes.cs` L431~432 — `PlaceZOverDrive`(side당 1개, Place 전용), `ContiOverDrive`

### 현재 구조 요약 (문제의 핵심)
AF 결과를 **절대값으로 쓰지 않고**, "이전 기준선 대비 델타"를 `ColletAfZOffset`에 **누적**해서 Pick/Place Z에 얹는 상대 보정 체계다. 그래서 Limit/stale/폴딩/롤백 같은 부속 로직이 붙어 있고, 팀장님 의도(AF 절대값 + 고정 기구 오프셋 = 공정 Z)와 완전히 다르다.

---

## 3. 신규 설계 (팀장님 의도 구현안)

### 3.1 신규 파라미터
| 파라미터 | 위치(제안) | 단위 | 설명 |
|----------|-----------|------|------|
| `BottomToPickMm` (Bottom to Pick) | Picker Recipe (Front/Rear 각 1개) | mm | Bottom 카메라 포커스 평면 Z → Pick 공정 Z 변환 고정값. 부호 그대로 가산 |
| `PickerHeaderOverdriveMm[4]` | Picker Recipe (Front/Rear 각 배열) | mm | 헤더(콜렛)별 Z 가산값. 최종 Pick Z에 헤더별로 가산 |

### 3.2 새 산식
- **콜렛 AF 완료 시** (Collet Calibration AF):
  `PickZ = AF BestZ + RimColletOffset(ColletZ.RimOffsetFromFlatMm, 예 -0.5) + DieThickness(ColletZ.DieCalThicknessMm, 예 0.25) + BottomToPickMm`
  → 해당 콜렛의 **PickPosition 티칭에 기록** (산식 전체 로그 필수)
- **다이 AF 완료 시** (생산 Bottom Die AF / 수동 다이 AF Apply):
  `PickZ = AF BestZ + BottomToPickMm` → PickPosition 티칭에 기록
- **공정 Pick 목표 계산 시** (PickTargets / PickVerifyManual):
  `최종 Z = coordinate.PickerZ(=PickPosition 티칭) + PickerHeaderOverdriveMm[collet]`
  (기존 colletAfZOffset 가산 제거)

물리 정합성: 콜렛 AF는 림 바닥면 포커스 → −0.5로 평면(진공면) 환산 → +0.25로 "다이를 물었을 때의 다이 바닥면" 환산. 다이 AF는 다이 바닥면 직접 포커스이므로 두 경로가 같은 기준면으로 수렴하고, 거기에 BottomToPick 1개로 Pick 공정 Z가 나온다. (Flat 콜렛이면 RimOffset 대신 FlatZOffsetMm 사용 — 기존 검사Z 산식과 동일 분기)

### 3.3 제거 목록 (R-02)
- Recipe: `ColletAfZOffset[4]`, `ColletAfZOffsetLimitMm`, `ColletAfZBaselineStale[4]`
- 로직: `ResolveColletAfZOffset`, `AccumulateColletAfZOffsetFromRuntimeAf`, `ResetColletAfZOffsetAfterZCalibration`, `RollbackRuntimeAfOffsetAndBaseline`, `ComputeAndApplyAfZOffset`, `RollbackAfZOffsetAccumulation`, `TryGetColletAfZOffsetRecipe(Arrays)`
- UI: Recipe 페이지 "COLLET AF Z OFFSET (5)" 그룹, ColletCalibrationDialog "AF Z OFS" 컬럼
- 데이터: `ColletCalibrationRecord.AfZOffset` (하위호환 위해 필드 자체는 두고 미사용 처리 가능 — 질문 8)
- 소비처 가산: PickTargets / PickVerifyManual / PlaceTargets의 colletAfZOffset 항 제거

### 3.4 유지되는 것 (혼동 방지)
- Collet Cal의 **Bottom/Side/DieBottom/DieSide 검사 티칭 동기화** (검사 Z 계열은 그대로)
- `PlaceZOverDrive`, `ContiOverDrive` (Place 계열 기존 파라미터)
- Vision Focus Cal 기준선(FocusPosition/BottomPosition) 갱신 자체는 유지 (AF 스캔 시작점 관리용)
- MotionSpeedScale: 이번 개편은 **목표 좌표 계산만** 변경 — 신규 이동 명령 없음, 속도 스케일 영향 없음

### 3.5 안전장치 (권장 — 승인 필요)
기존 Limit(0.3, fail-closed)가 사라지면 AF 오판(엉뚱한 면 포커스) 시 티칭이 한 번에 크게 틀어져 충돌 위험. 대체 안전장치로:
- **신규 PickZ vs 직전 PickPosition 차이 한계** (예: 기본 0.3mm, 초과 시 fail-closed + 알람, 티칭 미갱신)
- 파라미터 1개(`PickZUpdateLimitMm`) 추가 필요 — "없애 버리라"는 지시가 안전 한계까지 포함인지 확인 필요 (질문 7)

### 3.6 로그 계측 (필수 규칙 반영)
- AF→PickZ 갱신마다: bestZ, rimOffset, dieThickness, bottomToPick, 산출 PickZ, 이전 PickPosition, 차이, 적용/차단 사유를 Calibration 카테고리 영속 로그로 기록
- 공정 목표 계산마다: pickerZTeaching + headerOverdrive[i] = final 산식 로그 (기존 formula 로그 스타일 유지)

---

## 4. 수정 대상 파일 (구현 승인 시 전체 변경 목록)

| # | 파일 | 변경 |
|---|------|------|
| 1 | `Equipment\Unit\PickerFrontUnit.cs` | Recipe: AfZOffset 3종 제거, `BottomToPickMm`·`PickerHeaderOverdriveMm[4]` 신설 + Ensure |
| 2 | `Equipment\Unit\PickerRearUnit.cs` | 동일 |
| 3 | `Sequencing\Picker\PickerSequenceBase.cs` | AfZOffset 계열 5개 메서드 제거, `ResolvePickerHeaderOverdrive` + AF→PickZ 산식/갱신 공용 메서드 신설, 런타임 다이 AF에서 누적 대신 PickZ 갱신 |
| 4 | `Sequencing\Calibration\ColletCalibrationSequence.cs` | `ComputeAndApplyAfZOffset`/롤백 제거, Save 단계에서 R-01 산식으로 PickPosition 갱신 |
| 5 | `Sequencing\Picker\PickerPickUpSequence.PickTargets.cs` | offset 가산 → HeaderOverdrive 가산 |
| 6 | `Sequencing\Picker\PickerPickUpSequence.PickVerifyManual.cs` | 동일 |
| 7 | `Sequencing\Picker\PickerPlaceSequence.PlaceTargets.cs` | offset 항 제거 (Place 정책은 질문 1 결과 반영) |
| 8 | `Sequencing\Calibration\PickerPickUpZCalibrationSequence.cs` | Reset 호출 제거 |
| 9 | `Sequencing\Calibration\PickerPlaceZCalibrationSequence.cs` | Reset 호출 제거 |
| 10 | `Ui\Pages\Recipe\FrontPickerRecipePage.cs` | AF Z OFFSET 그룹 제거 → BOTTOM TO PICK / HEADER OVERDRIVE 그룹 신설 |
| 11 | `Ui\Pages\Recipe\RearPickerRecipePage.cs` | 동일 |
| 12 | `Ui\Dialogs\ColletCalibrationDialog(.Designer).cs` | AF Z OFS 컬럼 처리 |
| 13 | `Equipment\Calibration\ColletCalibrationData.cs` | `Record.AfZOffset` 처리(질문 8) |
| 14 | `Ui\Dialogs\VisionFocusCalibrationDialog.cs` | 수동 다이 AF Apply 경로에 R-03 산식 반영(질문 9) |

---

## 5. 열린 질문 (구현 전 팀장님 결정 필요)

1. **Place Z**: 기존 ColletAfZOffset은 Pick/Place 공용이었음. 제거하면 Place는 티칭+PlaceZOverDrive만 남음. 다이/콜렛 AF 결과를 Place에도 반영할지? (예: Bottom to Place 별도 신설 여부)
2. **신규 Header Overdrive 적용 범위**: Pick 전용인지, Pick+Place 공용인지?
3. **헤더 수**: 코드/축은 콜렛 4개(`MaxPickerCount=4`, 스크린샷도 1~4). 지시엔 "피커 3개" — 배열은 4개로 만들고 실사용 안 하는 헤더는 0 유지로 이해하면 되는지?
4. **BottomToPick 스코프**: Front/Rear side별 각 1개(권장) vs 장비 전체 1개 vs 콜렛별?
5. **FilmThickness**: 기존 검사Z 산식엔 Film도 가산. 신규 Pick 산식은 지시대로 Rim+DieThickness만(Film 제외)으로 확정?
6. **PickUpZ 캘리브레이션(터치)과의 관계**: AF가 PickPosition을 쓰게 되면 터치 Z캘과 이원화됨. 터치 Z캘 유지(나중 실행이 이김)인지, AF 전용으로 갈지?
7. **안전 한계**: 3.5의 신규 갱신 한계(fail-closed) 넣을지, 한계 없이 AF 결과 무조건 적용인지?
8. **잔재 처리**: `Record.AfZOffset` 필드/다이얼로그 컬럼 — 완전 삭제 vs 미사용 유지(구 데이터 호환)?
9. **다이 AF 범위**: R-03이 생산 중 런타임 다이 AF에만 적용인지, Vision Focus Cal 수동 다이 AF Apply에도 적용인지?
