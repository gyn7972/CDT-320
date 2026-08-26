# 촬영각 프레임 변환 — 구현 변경 내역 (2026-08-25)

기준: [04_design.md](../04_design.md). 미커밋. 우회 빌드(`/p:OutDir`) 오류 0.

## A. 정적 편심 보상 리버트 (3cc985e8 원복 — 7파일, git diff로 보상 이전 커밋과 일치 확인)
- PickerMotionTargetResolver.cs (−255): 파라미터·산출 블록·헬퍼 전량 제거
- DieCoordinateTransformService.cs (−13): 파라미터·PickerX/Y 가산·Formula 항 제거
- PickerPickUpSequence.PickTargets.cs / InputPickerPickTargetResolver.cs: opt-in 인자 제거
- PickerTransferTypes.cs (−18): 설정 2필드·상수·Normalize·OnDeserializing/Ensure 제거
- FrontPickerRecipePage.cs / RearPickerRecipePage.cs (−10×2): UI 2항 제거

## B. 존치 (2파일 — 주석만 새 소비자 기준으로 손질)
- ColletCalibrationSequence.SaveAndApplyRotationCenter / ColletCalibrationApplyService.SaveRotationCenterToRecipe:
  SaveRecipe 후 machine.SaveSettings() (실패=캘 실패) — C 실시간 소비자(Side Y식·프레임 변환)의 재시작 구값 방지

## C. 신규 — 촬영각 프레임 변환 (3파일)

### PickRuntimeOffsetService.cs (+84)
- `OnBottomInspectionOffset` 파라미터 5개 추가(기본값 없음 — 호출부 강제 갱신):
  `capturedPickerXCommand, rotationCenterX, rotationCenterY, rotationCenterUsable, shootDeltaThetaTeachingDeg`
- 콜렛캘 무효 폐기 직후·Y 전처리 이전에 변환 블록: 0°±5° 무변환 / 180°±5° 변환 /
  그 외·C 불가 폐기(Check 로그). 변환은 이미지→기계 환산(Y 부호 반전) → `m′=2a−m` → 재환산.
- 이후 체인은 `effectiveOffsetX`/`effectiveRawOffsetY`만 소비(전처리·적분·클램프 무수정)
- `ShootAngleBandDeg=5.0`, `NormalizeDegreesPlusMinus180` 신설, PICK-RUNTIME-OFFSET 로그 확장

### PickerBottomInspectionSequence.cs (+55)
- `_bottomShotPickerXCommand` 필드 + 촬영 REQ 시점 PickerX 지령 캡처(기존 Y 캡처와 동일 지점)
- `UpdatePickRuntimeOffsetFilter`: C(유닛 Config)·usable(Config Valid ∧ record.Valid ∧
  RotationCenterUpdatedAt≥UpdatedAt)·Δθ(티칭 Pick−Bottom) 조달 후 신규 인자 전달

### PickerBottomAndSideInspectionSequence.cs (+48)
- 동일 조달 블록(X 지령은 기존 `target.BottomShotPickerXCommand` 재사용 — 이미 캡처 중)

## 무변경 확인
픽 산식(DieCoordinateTransformService — 리버트로 원복)·Place 감산·Side 검사 경로·
인터락·필터 전처리/적분/클램프/이관 로직 diff 0. 신규 이동 명령 0건(MotionSpeedScale 해당 없음).
