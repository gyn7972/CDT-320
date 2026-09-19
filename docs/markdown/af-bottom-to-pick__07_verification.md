# AF 기반 PickerZ 공정높이 개편 — 검증 (Stage 7)

- 체크리스트: [05_checklist.md](af-bottom-to-pick__05_checklist.md) / 변경 상세: [06_implementation/CHANGES.md](06_implementation/CHANGES.md)
- 검증 방법: 코드 리딩 + 전수 grep + 솔루션 빌드(스크래치 OutDir)

## R-01/R-03 — 신규 산식·파라미터
- ✅ Recipe 신설 3종 + Ensure 가드 — PickerFrontUnit.cs / PickerRearUnit.cs (BottomToPickMm=0, Overdrive[4], Limit=0.3 기본, NaN/길이 가드)
- ✅ 공용 메서드 신설 — PickerSequenceBase.cs: `TryGetPickProcessZRecipe` / `ResolvePickerHeaderOverdrive` / `ResolveBottomToPickMm` / `ApplyAfDerivedPickPosition`(한계 fail-closed→기록→readback 검증→영속 로그)
- ✅ 콜렛 AF 산식 — ColletCalibrationSequence.`ComputeAndApplyAfDerivedPickZ`: `FinalPickerZ + ColletOffset(Rim/Flat) + DieThickness + BottomToPickMm` → PickPosition. Film 미포함(로그에 별도 표기)
- ✅ 다이 AF 산식 — `RunBottomRuntimeAutoFocusIfNeededAsync`: `bestZ + BottomToPickMm` → PickPosition, 기준선 저장 실패 시 PickPosition+기준선 동시 원복(`RollbackRuntimeAfPickAndBaseline`)
- ✅ 티칭 스냅샷 15항목(PickPosition 포함) + 갱신 "전" 캡처로 순서 변경 → 동기화 실패 시 전체 원복
- ✅ 검사티칭Z 동기화 무변경 — `SaveReferenceColletBottomTeachingIfNeeded`/`SaveColletInspectionZTeachingIfNeeded` 내용 그대로(diff는 호출부 롤백 방식만)

## R-02 — 구 체계 제거
- ✅ Recipe 필드 3종 제거(Front/Rear) + Ensure 블록 제거
- ✅ Base 메서드 5종 제거, ColletCal 3종 제거, Record.AfZOffset/CopyRecord 라인 제거
- ✅ Z캘 리셋 호출 제거 — PickUpZ/PlaceZ Calibration `SaveCalibrationResult`
- ✅ 잔존 참조 0건 — grep `ColletAfZOffset|ColletAfZBaselineStale|AfZOffset|AccumulateColletAfZ|ResetColletAfZ|RollbackAfZOffset|RollbackRuntimeAfOffset` → No matches
- ✅ UI 문자열 잔존 0건 — grep `COLLET AF Z OFFSET|AF Z OFS|L_COLLET_AF_Z_OFFSET|AF Z OFFSET LIMIT` → No matches

## R-04 — 소비처
- ✅ PickTargets: `coordinate.PickerZ + headerOverdrive` (유일 대입점 유지, 파생 전파 구조 무변경)
- ✅ PickVerifyManual: 동일 교체(수동 Z 테스트 = 실공정 동일 규칙)
- ✅ PlaceTargets: `coordinate.PickerZ + placeZOverDrive` — AF 항 제거, PlaceZOverDrive/ContiOverDrive 체계 무변경
- ✅ 이동/속도 코드 무변경 — 목표값 계산만 변경, MotionSpeedScale 영향 없음

## UI
- ✅ Front/Rear Recipe 페이지: "PICK PROCESS Z (AF)" 그룹 (BOTTOM TO PICK / PICK Z UPDATE LIMIT / PICKER 1~4 PICK OVERDRIVE)
- ✅ ColletCalibrationDialog: AF Z OFS 컬럼·표시 로직·Designer 필드 제거

## 로그 계측
- ✅ AF→PickZ 갱신: `AfPickProcessZ` 카테고리 — formulaPickZ 전체, old/new/delta/limit/source/차단 사유 영속
- ✅ 공정 목표: formula에 `headerOverdrive` 반영(0이면 생략 — 기존 스타일 유지), Place formula에서 AF 항 제거

## 빌드
- ✅ QMC.CDT-320.sln Debug: **0 error** / 신규 경고 0 (전체 경고 44건 = 기존 MotionPage.Designer CS0169만, 변경 파일 경고 없음)
- ❓ 최초 빌드는 운영 폴더(D:\CDT-320) DLL 복사 단계에서 실패 — **장비 앱 실행 중(PID 14140)이라 파일 잠김**. 소스 컴파일은 정상이며, 스크래치 OutDir로 재검증 완료. 운영 반영은 앱 종료 후 재빌드 필요(운영 폴더에는 손대지 않았음).

## 요약
- 체크리스트 25항목: ✅ 24 / ❌ 0 / ❓ 1(운영 폴더 배포 — 앱 종료 후 수행)

## R-05/R-06 검증 (동일자 추가 지시: Place Die AF + 헤드/콜렛 Overdrive)
- ✅ `BottomToPlaceMm` 신설 + `AfZUpdateLimitMm` 공용화 + `HeadPickOverdriveMm`/`ColletPickOverdriveMm[4]` — Front/Rear Recipe, Ensure 가드 포함
- ✅ 다이 AF → `PlacePosition = bestZ + BottomToPlaceMm` 갱신, Pick과 한 트랜잭션: Place 한계 차단 시 Pick 원복, 기준선 저장 실패 시 Pick/Place/기준선 동시 원복(`RollbackRuntimeAfTeachingsAndBaseline`)
- ✅ 콜렛 AF는 PlacePosition 미갱신(`ApplyAfDerivedZTeaching("PickPosition")`만 호출)
- ✅ 공정 Pick Z = PickPosition + HeadOD + ColletOD (PickTargets/PickVerifyManual 동일), 로그 분리 표기
- ✅ 알람 코드 분리: `PICKER-AF-PICKZ-*` / `PICKER-AF-PLACEZ-*`, 로그 카테고리 `AfProcessZ` 통일
- ✅ UI: "AF PROCESS Z (PICK/PLACE)" 그룹 5종 항목(Front/Rear)
- ✅ 구명칭 잔존 0건(grep), 빌드 0 error / 신규 경고 0 (verifyMs·AxisInitializePlan·MotionPage 경고는 기존 상존 — 해당 파일 git diff 없음 확인)

## 초기 도입 절차 (중요 — 실장비 첫 가동 전)
새 체계는 AF 절대값 기준이라, Bottom to Pick/Place를 맞추기 전에는 콜렛캘/다이 AF가
`PICKER-AF-PICKZ-UPDATE-LIMIT` / `PICKER-AF-PLACEZ-UPDATE-LIMIT` 알람(신규 Z vs 기존 티칭 편차 > 0.3mm)으로 fail-closed 됩니다.
1. 콜렛 AF(또는 다이 AF) 1회 실행 → 알람 메시지의 `newZ`/`oldZ` 확인.
2. `BOTTOM TO PICK = 기존 PickPosition − (AF BestZ + ColletOffset + DieThickness)` (다이 AF 기준이면 `− BestZ`),
   `BOTTOM TO PLACE = 기존 PlacePosition − 다이 AF BestZ` 로 역산 입력.
3. 재실행 → delta≈0으로 통과, 이후 AF가 PickPosition/PlacePosition을 자동 유지.
4. 의도적으로 큰 초기 편차를 허용해야 하면 AF Z UPDATE LIMIT를 일시 상향 후 원복.
5. Overdrive는 HEAD(사이드 공통) + COLLET(개별)이 합산 적용 — 공통 눌림량은 HEAD에, 콜렛 편차만 COLLET에 입력 권장.
