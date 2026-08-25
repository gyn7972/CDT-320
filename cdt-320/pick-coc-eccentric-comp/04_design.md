# 픽업 회전중심·콜렛원점 편심 보상 — 설계 확정 및 앵커 검증 (Stage 4)

기준 문서: [픽업_회전중심_콜렛원점_편심보상_수정지시_프롬프트_2026-08-25.md](../../픽업_회전중심_콜렛원점_편심보상_수정지시_프롬프트_2026-08-25.md)
(선행 멀티에이전트 전수 조사 + 3렌즈 검증 완료본 — Stage 1~3을 이 문서로 갈음)
검증일: 2026-08-25, 검증 방법: 인용 앵커 전부 현재 코드 라인 재확인

## 1. 앵커 검증 결과 — 전부 일치 (편집 가능 상태)

| 지시서 앵커 | 현재 코드 확인 | 판정 |
|---|---|---|
| §3-1 `PickerMotionTargetResolver.TryCalculateInputPickTarget` 21~75행 / `CalculateInputPickTarget` 79~179행 | 시그니처·행번호 일치, `pickRuntimeOffset* = 0.0` 옵션 파라미터 선례 확인 | ✅ |
| §3-2 `DieCoordinateTransformService.CalculatePickTarget` 68~130행, PickerX=106행, PickerY=113~114행, StageY=105행, NeedleX=111행, Formula=117~126행 | 전부 일치 | ✅ |
| §3-3 자동 픽업 `PickerPickUpSequence.PickTargets.cs:607` | `TryCalculateInputPickTarget` 호출 확인 | ✅ |
| §3-3 수동 맵 `InputPickerPickTargetResolver.CalculateManualInputMapTarget` 25~51행 | align 3항 0.0 고정 전달 + 기본 pickRuntime 0 확인. 소비자 3곳: InputStageMapTransferPage.cs:902·3811, InputPickerOffsetSetupDialog.cs:297 | ✅ |
| §3-3 무변경 호출부: PickerPickUpZCalibrationSequence.cs:397, RecipePickerMoveTarget.cs:123 | 호출 확인 — 옵션 파라미터 기본 false라 무변경 컴파일 호환 | ✅ |
| 깔때기 전수: `Try/CalculateInputPickTarget` 외부 호출 = 위 4곳뿐 | grep 재확인 (F11 유지) | ✅ |
| F1 회전중심 `Config.ColletRotationCenterX/Y/Valid[4]` | PickerFrontUnit.cs:282-284 확인 | ✅ |
| F3 콜렛 원점 O = 레코드 `FinalPickerX/Y` | ColletCalibrationSequence.cs:1541-1571 레코드 생성 확인 | ✅ |
| 레코드 접근 경로 | `machine.VisionUnit.Config.CalibrationData.Collet.GetRecord(side, colletNo)` (선례 CalibrationCoordinateService.cs:129) | ✅ |
| §3-4 `PickerPickUpMotionConfig` | PickerTransferTypes.cs:43~, `[DataMember]`+`OnDeserializing` 초기화+`Ensure()` 정규화 패턴 확인 | ✅ |
| §3-4 UI 선례 | FrontPickerRecipePage.cs:463~484 PickUp 기구 오프셋 바인딩 블록 확인 (Rear 동형) | ✅ |
| §3-5 `SaveAndApplyRotationCenter` | ColletCalibrationSequence.cs:1982-2014, SaveRecipe만 호출 확인 (F13) | ✅ |
| §3-5 `SaveRotationCenterToRecipe` | ColletCalibrationApplyService.cs:106-177, SaveRecipe만 호출 확인 | ✅ |
| `machine.SaveSettings()` 존재 | bool 반환, 선례 ColletCalibrationApplyService.cs:76 | ✅ |
| F14 죽은 코드 `ResolveManualInputMapTarget` | 부활 금지 대상 확인 (grep 호출 0건) | ✅ |

## 2. 각도 티칭 소스 확정 (θ_pick / θ_cal)

- 촬영 T0 실사용: `ResolvePickerZoneT("DieBottomPosition", pickerIndex)`
  (PickerBottomAndSideInspectionSequence.cs:1050, PickerBottomInspectionSequence.cs:380)
  → `ResolveCarryZoneTarget`(CarryRuntimeAndCollet) 경유 = **teachingT + runtimeT** (콜렛T는 이동식 제외).
- 지시서 §2·§8 확정: Δθ는 **티칭값만** 사용 (runtime T ±0.45°는 |e| 수십 µm에서 무시 가능, Formula에 "티칭 Δθ 기준" 명기).
- 구현 소스:
  - θ_pick = `InputPickerPickTargetResolver.ResolvePickerTeachingPosition(machine, side, ResolvePickerTAxis(pickerIndex), "PickPosition")` — 픽 산식과 동일 앵커(PickerMotionTargetResolver.cs:133-137).
  - θ_cal = `CalibrationCoordinateService.ResolvePickerZoneTarget(machine, sideEnum, "DieBottomPosition", pickerIndex, null, false, false).TeachingT` — 촬영 존과 동일 티칭 소스, 보정 미포함 raw.
- `record.MeasuredTPosition` 사용 금지 (지시서 §2 ★ — T홈 제로 이전 좌표계).

## 3. 게이트 3(캘 세대 정합) — 미확정 해소

`ColletCalibrationRecord`에 **`UpdatedAt` 필드가 이미 존재** (ColletCalibrationData.cs:117).
- 콜렛 캘 저장 시 갱신: ColletCalibrationSequence.cs:1570 (`UpdatedAt = DateTime.Now`), 다이얼로그 수동 경로 ColletCalibrationDialog.cs:1878·2167.
- COC 저장 시 갱신: `RotationCenterUpdatedAt` (ColletRotationCenterCalibrationSequence.cs:368).
- → **신규 필드 추가 불필요.** 게이트 3 = `record.RotationCenterUpdatedAt >= record.UpdatedAt`.

## 4. 팀장님 결정 필요 3건 (편집 전 확답 대기)

1. **게이트 3 구현**: 위 §3대로 기존 `UpdatedAt` 사용 — 승인 여부.
2. **게이트 2 확장 제안**: `record.Valid`(콜렛 캘 유효)도 게이트에 포함할지.
   Valid=false면 FinalPickerX/Y가 0/구값이라 e=C−O가 오염됨. 지시서 게이트 목록에는 없어 임의 추가하지 않고 문의. (미포함 시에도 세대 게이트가 대부분 걸러주나, 미캘 레코드의 UpdatedAt=2000-01-01 < RotationCenterUpdatedAt이면 통과 구멍 존재 → 포함 권장.)
3. **§3-5 SaveSettings 실패 처리**: 같은 파일 T-Home 적용 선례(ColletCalibrationApplyService.cs:76-80)는 실패 시 -1 반환. 동일하게 실패 처리 제안 (경고 로그만 남기는 대안 있음).

## 5. 부호 유보·게이트·계측·검증 절차

지시서 §2(부호 유보 — 스위치 없이 구현, 실장비 1런으로 확정), §5(PICK-COC-COMP 계측), §6(필터 리셋 등 동반 절차), §7(검증)을 그대로 따른다. 재기술 생략.
