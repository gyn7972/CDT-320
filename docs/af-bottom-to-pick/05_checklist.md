# AF 기반 PickerZ 공정높이 개편 — 구현 체크리스트 (Stage 5)

- 설계 근거: [01_analysis_and_design.md](01_analysis_and_design.md)
- 승인: 2026-07-29 팀장님 ("그게 내가 의도한거야. 그렇게 설계하고 체크리스트 만들고 구현하고 확인 후 보고")

## 확정 결정 (열린 질문 → 확정)

| # | 질문 | 확정 |
|---|------|------|
| 1 | Place Z | ~~AF 미반영~~ → **동일자 추가 지시로 변경: 다이 AF 사용.** `PlacePosition = 다이 AF BestZ + BottomToPlaceMm(신설)`. 콜렛 AF는 Place 미갱신. 목표식은 티칭+PlaceZOverDrive 유지 |
| 2 | 신규 Overdrive 범위 | **Pick 전용** (오토 Pick + 수동 PickUp Z 테스트). **동일자 추가 지시: 헤드 공통(`HeadPickOverdriveMm`) + 콜렛별(`ColletPickOverdriveMm[4]`) 합산** |
| 3 | 헤더 수 | 콜렛 배열 4개(콜렛 1~4 = PickerZ0~Z3 축과 정합). 미사용 콜렛은 0 유지 |
| 4 | 안전 한계 | 유지(권장안). `AfZUpdateLimitMm`(기본 0.3, Pick/Place 공용) — \|신규 Z − 기존 티칭\| 초과 시 fail-closed 알람, 티칭 미갱신 |
| 5 | 터치 Z캘 관계 | 병행 유지. PickUpZ 캘도 PickPosition을 씀(마지막 실행 우선). offset 폴딩/리셋 호출만 제거 |
| 6 | FilmThickness | Pick 산식에서 **제외** (지시 산식 그대로: Rim + DieThickness + BottomToPick). 검사티칭Z 산식(film 포함)은 기존 유지 |
| 7 | 다이 AF 적용점 | 생산 런타임 Bottom Die AF(기존 누적 지점)를 신규 산식으로 대체. Vision Focus Cal 다이얼로그는 기준선 갱신만(현행 유지) |
| 8 | 잔재 | `ColletCalibrationRecord.AfZOffset` 필드·다이얼로그 "AF Z OFS" 컬럼 완전 제거 (DataContract는 미지 멤버 무시 → 구 파일 역직렬화 안전) |

## R-01/R-03 — 신규 산식·파라미터

- [ ] `PickerFrontRecipe`/`PickerRearRecipe`에 `BottomToPickMm`(double, 기본 0), `PickerHeaderOverdriveMm[4]`, `PickZUpdateLimitMm`(기본 0.3) 신설 + `EnsurePositionObjects` 가드(NaN/배열길이/한계>0)
- [ ] `PickerSequenceBase`에 공용 메서드 신설: 레시피 조회(`TryGetPickProcessZRecipe`) / 헤더 Overdrive 조회(`ResolvePickerHeaderOverdrive`, NaN 가드) / AF→PickPosition 갱신(`ApplyAfDerivedPickPosition`: 한계 검사 fail-closed → 티칭 기록 → readback 검증 → 산식 전체 영속 로그, 이전값 반환)
- [ ] 콜렛 AF: `ColletCalibrationSequence.SaveColletCalibration`에서 `PickZ = FinalPickerZ(AF Best) + ColletOffset(Rim/Flat) + DieThickness + BottomToPickMm` 계산 후 PickPosition 갱신 (Film 미포함)
- [ ] 다이 AF: `RunBottomRuntimeAutoFocusIfNeededAsync`에서 `PickZ = bestZ + BottomToPickMm` 계산 후 PickPosition 갱신, 이후 기준선(BottomPosition) 갱신·레시피 저장 실패 시 PickPosition/기준선 동시 원복(보상 롤백)
- [ ] 콜렛캘 티칭 스냅샷에 PickPosition 추가(15항목) — 후속 티칭 동기화 실패 시 PickPosition 포함 원복
- [ ] 검사티칭Z 동기화(`SaveReferenceColletBottomTeachingIfNeeded`/`SaveColletInspectionZTeachingIfNeeded`)는 무변경(기존 산식 유지)

## R-02 — Collet AF Z Offset 체계 제거

- [ ] Recipe 필드 제거: `ColletAfZOffset[4]`, `ColletAfZOffsetLimitMm`, `ColletAfZBaselineStale[4]` (Front/Rear) + EnsurePositionObjects 내 관련 블록
- [ ] `PickerSequenceBase` 메서드 제거: `TryGetColletAfZOffsetRecipe`, `ResolveColletAfZOffset`, `RollbackRuntimeAfOffsetAndBaseline`, `ResetColletAfZOffsetAfterZCalibration`, `AccumulateColletAfZOffsetFromRuntimeAf`
- [ ] `ColletCalibrationSequence` 제거: `TryGetColletAfZOffsetRecipeArrays`, `ComputeAndApplyAfZOffset`, `RollbackAfZOffsetAccumulation` + Save 단계 호출부
- [ ] Z캘 리셋 호출 제거: `PickerPickUpZCalibrationSequence.cs:1657`, `PickerPlaceZCalibrationSequence.cs:1926`
- [ ] `ColletCalibrationRecord.AfZOffset` 제거 + `CopyRecord` 라인 제거
- [ ] 잔존 참조 0건: `ColletAfZOffset|ColletAfZBaselineStale|AfZOffset` grep 결과 없음

## R-04 — 소비처(공정 Z 계산) 교체

- [ ] Pick(오토) `PickTargets.cs`: `_targetPickerZ = coordinate.PickerZ + PickerHeaderOverdriveMm[collet]` (offset 제거), 산식 로그 갱신
- [ ] Pick(수동 Z 테스트) `PickVerifyManual.cs`: 동일 교체
- [ ] Place `PlaceTargets.cs`: `_targetPickerZ = coordinate.PickerZ + placeZOverDrive` (offset 항 제거), 산식 로그 갱신
- [ ] 이동 명령/속도 코드 무변경 (MotionSpeedScale 영향 없음)

## UI

- [ ] Front/Rear Recipe 페이지: "COLLET AF Z OFFSET" 그룹 제거 → "PICK PROCESS Z (AF)" 그룹 신설(BOTTOM TO PICK / PICK Z UPDATE LIMIT / PICKER 1~4 PICK OVERDRIVE, 각 설명 포함)
- [ ] `ColletCalibrationDialog`: "AF Z OFS" 컬럼(Designer 포함)·`ResolveAppliedAfZOffsetText`·행 추가 인자 제거

## R-05/R-06 — 동일자 추가 지시 (Place Die AF + 헤드 Overdrive)

- [ ] `BottomToPlaceMm` 신설(Front/Rear Recipe) + Ensure 가드, `PickZUpdateLimitMm`→`AfZUpdateLimitMm`(Pick/Place 공용) 개명
- [ ] `PickerHeaderOverdriveMm[4]`→`ColletPickOverdriveMm[4]` 개명 + `HeadPickOverdriveMm`(헤드 공통) 신설
- [ ] 다이 AF 완료 시 `PlacePosition = bestZ + BottomToPlaceMm` 갱신 — Pick과 **한 트랜잭션**(Place 차단 시 Pick 원복, 기준선 저장 실패 시 Pick/Place/기준선 동시 원복)
- [ ] 콜렛 AF는 PlacePosition을 갱신하지 않음
- [ ] 공정 Pick Z = PickPosition + Head OD + Collet OD (오토/수동 동일), 산식 로그에 두 값 분리 표기
- [ ] `ApplyAfDerivedZTeaching` 공용화(PICKZ/PLACEZ 알람 코드 분리: `PICKER-AF-PICKZ-*`/`PICKER-AF-PLACEZ-*`), 로그 카테고리 `AfProcessZ`
- [ ] UI 그룹 "AF PROCESS Z (PICK/PLACE)": BOTTOM TO PICK / BOTTOM TO PLACE / AF Z UPDATE LIMIT / HEAD PICK OVERDRIVE / COLLET 1~4 PICK OVERDRIVE
- [ ] 구명칭 잔존 0건 + 빌드 0 에러

## 로그/검증

- [ ] AF→PickZ 갱신 로그: bestZ, colletOffset, dieThickness, bottomToPick, 신규 PickZ, 기존 PickPosition, delta, limit, 적용/차단 사유 — Calibration 카테고리 영속
- [ ] 공정 목표 로그: pickerZTeaching + headerOverdrive = final 산식 포함
- [ ] 빌드 경고/오류 없음 (기존 경고 수준 유지)
- [ ] `06_implementation/CHANGES.md` 작성 + 변경 파일 사본 보관
- [ ] `07_verification.md`에 전 항목 ✅/❌ 판정
