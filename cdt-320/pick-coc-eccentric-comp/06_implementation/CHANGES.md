# 픽업 편심 보상 — 구현 변경 내역 (Stage 6, 2026-08-25)

기준: [04_design.md](../04_design.md), [05_checklist.md](../05_checklist.md).
팀장님 확답(2026-08-25): ① 게이트 3 = 기존 `UpdatedAt` 비교 ② `record.Valid` 게이트 포함
③ SaveSettings 실패 = 캘 실패 처리. 커밋 전(미커밋).

## Sequencing/Picker/PickerMotionTargetResolver.cs — 핵심 (+255)
체크리스트 A 전부.
- `TryCalculateInputPickTarget`·`CalculateInputPickTarget`에 `bool applyColletEccentricCompensation = false` 추가(기본 false → 기존 호출부 무변경 호환).
- 신설 `ResolveColletEccentricCompensation`: e=C−O(레코드 조합만), Δθ=티칭 PickPosition−BottomPosition(±360 정규화), R=[[cos,+sin],[−sin,cos]](T+=CW), ΔP=(I−R(Δθ))·e.
- 게이트 6단(사유 문자열): machine/유닛/설정 null → `disabled` → 회전중심 배열/Valid → 레코드 null → `collet-cal-invalid`(record.Valid, 팀장님 승인 확장) → `calibration-generation-mismatch`(RotationCenterUpdatedAt < UpdatedAt) → `delta-theta-out-of-band`(±5°) → `magnitude-over-limit`(성분별, Warning).
- 로그: 적용 시 매 산출 1줄(EventKind.Event, "COORD", "PICK-COC-COMP", C/O/e/θ/Δθ/ΔP/limit), 폴백은 콜렛(side+picker)당 사유 변화 시에만 1줄(크기 게이트만 Warning), 적용 복귀 시 상태 클리어.
- InputPickTarget 좌표 로그에 `colletEccentricCompRequested/X/Y` 항 추가.
- 예외는 comp 0 + `exception:<형>` 폴백(픽 자체는 계속).

## Sequencing/Picker/DieCoordinateTransformService.cs — 적용 (+15)
체크리스트 B 전부.
- `CalculatePickTarget`에 `colletEccentricCompX/Y = 0.0` 추가, PickerX·PickerY에만 가산(StageY/NeedleX/T 무변경).
- Formula에 두 항 + "[colletEccentricComp=teachingDeltaTheta 기준(런타임 T 미반영), PickerX/PickerY에만 가산]" 명기 → DIE-COORD-CALC 다이별 기록.

## Sequencing/Picker/PickerPickUpSequence.PickTargets.cs (+1)
- 자동 픽업 607행 호출에 `applyColletEccentricCompensation: true`.

## Sequencing/Picker/InputPickerPickTargetResolver.cs (+2)
- `CalculateManualInputMapTarget` 내부 호출에 `true`(수동 확인 위치=실제 픽 위치, 팀장님 확정).
- PickZ 캘(397행)·레시피 이동(123행)은 무인자(false) 유지 — 무변경 확인.

## Equipment/Unit/Common/PickerTransferTypes.cs (+18)
체크리스트 D 설정부.
- `UsePickRotationCenterCompensation`(기본 false), `ColletEccentricCompensationLimitMm`(기본 0.2, 상수 `DefaultColletEccentricCompensationLimitMm`).
- `OnDeserializing` 안전측 초기화, `Ensure()` 정규화, `NormalizeColletEccentricCompensationLimit`(≤0·NaN·∞→0.2, 3자리 반올림) 신설 — UI 세터와 공용.

## Ui/Pages/Recipe/FrontPickerRecipePage.cs · RearPickerRecipePage.cs (+10 ×2)
체크리스트 D UI부.
- PICKUP MECHANICAL T 루프 직후에 `PICKUP COLLET ECCENTRIC COMP USE`(Bool)·`PICKUP COLLET ECCENTRIC COMP LIMIT`(Double, Normalize 세터) 2항 추가. `ResolveLivePickUpConfig()` 라이브 리졸버 사용(LoadSettings 교체 대응), 기존 항목 이동 없음.

## Sequencing/Calibration/ColletCalibrationSequence.cs (+10)
체크리스트 E.
- `SaveAndApplyRotationCenter`: SaveRecipe 성공 후 `Context.Machine.SaveSettings()` 추가, 실패 시 `COLLET-CAL-COC-SETTINGS-SAVE` Fail(팀장님 확정 ③). 성공 로그에 `settingsSaved=true`.

## Equipment/Calibration/ColletCalibrationApplyService.cs (+10)
- `SaveRotationCenterToRecipe`: 동일 보강(실패 시 -1 + 메시지).

## 무변경 확인 (체크리스트 F)
SideVisionYTargetCalculator·Place 산식·플레이스 필터·T 채널·COC/콜렛 캘 측정 로직·InputVisionToPicker 저장값 — diff 0.

## 범위 밖 주의
`Equipment/Unit/OutputStageUnit.cs` +18은 **이 작업의 변경이 아님** — NG 클램프 정착 이력 보완 건(2026-08-25 팀장님 승인 주석, 별도 세션 작업)이 워킹트리에 공존. 본 작업에서 미접촉.
