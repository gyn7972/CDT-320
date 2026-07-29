# Implementation changes — AF 기반 PickerZ 공정높이 개편

## 2차(동일자 추가 지시): Place Die AF + 헤드/콜렛 Overdrive
- **PickerFront/RearUnit.cs**: `BottomToPlaceMm` 신설, `PickZUpdateLimitMm`→`AfZUpdateLimitMm`(Pick/Place 공용),
  `PickerHeaderOverdriveMm[4]`→`ColletPickOverdriveMm[4]` 개명 + `HeadPickOverdriveMm`(헤드 공통) 신설.
- **PickerSequenceBase.cs**: `TryGetAfProcessZRecipe`(5출력)로 확장, `ResolveHeadPickOverdrive`/`ResolveColletPickOverdrive`/`ResolveBottomToPlaceMm` 신설,
  `ApplyAfDerivedPickPosition`→`ApplyAfDerivedZTeaching`(positionName 공용화, 알람 코드 PICKZ/PLACEZ 분리, 로그 카테고리 `AfProcessZ`),
  `RollbackRuntimeAfPickAndBaseline`→`RollbackRuntimeAfTeachingsAndBaseline`(Place 포함).
  다이 AF: `PlacePosition = bestZ + BottomToPlaceMm` 추가 — Pick과 한 트랜잭션(Place 차단 시 Pick 원복).
- **ColletCalibrationSequence.cs**: 호출부를 `ApplyAfDerivedZTeaching("PickPosition")`으로 변경(콜렛 AF는 Place 미갱신), 로그 카테고리 정리.
- **PickTargets/PickVerifyManual.cs**: Pick Z = 티칭 + HeadOD + ColletOD 합산, 로그에 headOverdrive/colletOverdrive 분리 표기.
- **PlaceTargets.cs**: 주석 정정(Place 티칭은 다이 AF가 갱신, 목표식은 티칭+PlaceZOverDrive 유지 — 코드 무변경).
- **Front/RearPickerRecipePage.cs**: 그룹 "AF PROCESS Z (PICK/PLACE)"로 확장 — BOTTOM TO PICK/PLACE, AF Z UPDATE LIMIT, HEAD PICK OVERDRIVE, COLLET 1~4 PICK OVERDRIVE.
- 빌드: 0 error / 신규 경고 0 (CS0219 verifyMs·CS0162 AxisInitializePlan·CS0169 MotionPage는 기존 상존 경고 — git diff 무관 확인).

---


- 일자: 2026-07-29 / 승인: 팀장님
- 빌드: QMC.CDT-320.sln Debug — 0 error / 신규 경고 0 (기존 MotionPage CS0169 44건만)
- 이 폴더의 .cs 파일은 검토용 사본. 실제 반영은 원본 경로에 완료.

## Equipment\Unit\PickerFrontUnit.cs — MODIFIED (R-01/R-02/R-04)
- `PickerFrontRecipe`: `ColletAfZOffset[4]`/`ColletAfZOffsetLimitMm`/`ColletAfZBaselineStale[4]` 제거.
- 신설: `BottomToPickMm`(기본 0), `PickerHeaderOverdriveMm[4]`, `PickZUpdateLimitMm`(기본 0.3) + EnsurePositionObjects NaN/배열 가드.

## Equipment\Unit\PickerRearUnit.cs — MODIFIED
- Front와 동일 (Rear Recipe).

## Equipment\Calibration\ColletCalibrationData.cs — MODIFIED (R-02)
- `ColletCalibrationRecord.AfZOffset` 제거 (DataContract 미지 멤버 무시 → 구 저장파일 역직렬화 안전).

## Sequencing\Picker\PickerSequenceBase.cs — MODIFIED (핵심)
- 제거: `TryGetColletAfZOffsetRecipe`, `ResolveColletAfZOffset`, `RollbackRuntimeAfOffsetAndBaseline`,
  `ResetColletAfZOffsetAfterZCalibration`, `AccumulateColletAfZOffsetFromRuntimeAf`.
- 신설: `TryGetPickProcessZRecipe`, `ResolvePickerHeaderOverdrive`, `ResolveBottomToPickMm`,
  `ApplyAfDerivedPickPosition`(한계 fail-closed → PickPosition 기록 → readback 검증 → 산식 영속 로그),
  `RollbackRuntimeAfPickAndBaseline`(PickPosition+기준선 동시 원복).
- `RunBottomRuntimeAutoFocusIfNeededAsync`(다이 AF): 누적 대신 `PickPosition = bestZ + BottomToPickMm` 직접 갱신(R-03).
  기준선 저장 실패 시 보상 롤백 유지. 레시피 영속은 기존 `ApplyRuntimeBottomFocusPosition`→`SaveRecipe`에 함께 실림.

## Sequencing\Calibration\ColletCalibrationSequence.cs — MODIFIED (R-01)
- 제거: `TryGetColletAfZOffsetRecipeArrays`, `ComputeAndApplyAfZOffset`, `RollbackAfZOffsetAccumulation`, CopyRecord의 AfZOffset 라인.
- 신설: `ComputeAndApplyAfDerivedPickZ` — `PickPosition = FinalPickerZ(AF Best) + ColletOffset(Rim/Flat) + DieThickness + BottomToPickMm` (Film 미포함).
- `SaveColletCalibration`: 스냅샷을 갱신 "전"에 캡처(순서 변경) → PickZ 갱신 → 기준/검사 티칭 동기화, 실패 시 스냅샷 전체 원복.
- 티칭 스냅샷 14→15항목(PickPosition 추가) + `SetPickerPickTeachingPosition` 헬퍼 신설.
- 안전망 Recipe 저장 조건 `afOffsetApplied`→`afPickApplied`, 코드 `COLLET-CAL-AF-PICKZ-SAVE`.
- 검사티칭Z 동기화(Bottom/Side/DieBottom/DieSide, film 포함 산식)는 무변경.

## Sequencing\Calibration\PickerPickUpZCalibrationSequence.cs — MODIFIED (R-02)
- `ResetColletAfZOffsetAfterZCalibration("PickUpZCalibration")` 호출 블록 제거. 터치 Z캘의 PickPosition 저장은 그대로(AF와 병행, 마지막 실행 우선).

## Sequencing\Calibration\PickerPlaceZCalibrationSequence.cs — MODIFIED (R-02)
- 동일 리셋 호출 블록 제거.

## Sequencing\Picker\PickerPickUpSequence.PickTargets.cs — MODIFIED (R-04)
- `_targetPickerZ = coordinate.PickerZ + ResolvePickerHeaderOverdrive(collet)` (offset 가산·limit Fail 제거), 산식/로그 `headerOverdrive`로 갱신.

## Sequencing\Picker\PickerPickUpSequence.PickVerifyManual.cs — MODIFIED (R-04)
- 수동 PickUp Z 테스트 동일 교체.

## Sequencing\Picker\PickerPlaceSequence.PlaceTargets.cs — MODIFIED (확정결정 1)
- `_targetPickerZ = coordinate.PickerZ + placeZOverDrive` (colletAfZOffset 항 제거). AF 보정은 Pick 전용.

## Ui\Pages\Recipe\FrontPickerRecipePage.cs / RearPickerRecipePage.cs — MODIFIED (UI)
- "COLLET AF Z OFFSET(5)" 그룹 제거 → "PICK PROCESS Z (AF)" 그룹 신설:
  BOTTOM TO PICK / PICK Z UPDATE LIMIT / PICKER 1~4 PICK OVERDRIVE (Recipe 스코프, 설명 포함).

## Ui\Dialogs\ColletCalibrationDialog.cs / .Designer.cs — MODIFIED (UI)
- "AF Z OFS" 컬럼(colAfZOffset) 및 `ResolveAppliedAfZOffsetText` 제거, 행 추가 인자 정리.
