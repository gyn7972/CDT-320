# OffsetX/Y 복원(B안) 영향 검증 보고서

- 작성일: 2026-07-12
- 대상 커밋: `f2e874f6` (VisionCameraCalibrationTransform bottomOffsetX/Y 선언 복원)
- 검증 방법: 멀티에이전트 코드 전수 추적(17 에이전트) + 주장별 적대적 재검증(전 항목 file:line 근거 확인)
- 이전 문서: [01_status_review.md](01_status_review.md)

## 결론 요약

| 차원 | 결론 |
|---|---|
| 소비 경로 | OffsetX/Y가 실제 모션에 도달하는 경로는 **정확히 2개** (아래 상세). 이번 복원으로 두 경로의 Place 좌표가 처음으로 실값 보정된다 |
| 이중 적용 | **없음.** OffsetX/Y(Place)와 BottomCenterOffsetX/Y(Side 카메라)는 같은 키에서 읽지만 소비 경로가 완전 분리 |
| Collet Cal 경로 | **영향 없음.** COC/Side AF는 BottomCenterOffsetX/Y와 Values만 사용, OffsetX/Y 접근 0건 |
| Vision 키 계약 | **일치.** `bottom_offset_x_mm`/`bottom_offset_y_mm`(mm)로 송신→대소문자 무시 정확 일치로 수신. 항상 0이 되는 키 불일치 없음 |

## 1. 모션 도달 경로 2개 (확정, 전 항목 적대적 검증 통과)

### 경로 A — 생산 Place 시퀀스 (주 경로)

```text
PickerBottomAndSideInspectionSequence.cs:1227 (또는 PickerBottomInspectionSequence.cs:751)
  → DieInspectionRecord(InspectionType="Bottom").Offset{X,Y,R, IsValid=true} 저장
  → PickerPlaceSequence.TryResolveBottomPlaceOffset(1454, 최신 UpdatedAt 레코드 선택)
  → DieCoordinateTransformService.CalculatePlaceTarget(163-167)
      OutputStageY = ... + OffsetY      (가산)
      PickerX      = ... - OffsetX      (감산)
      PickerT      = teaching - OffsetT (감산)
  → MovePickerXYAndTToPlaceAsync / MoveOutputStageYAndPickerXYTToPlaceAsync 실제 축 이동
```

### 경로 B — 레거시 auto-cycle (Form1 구형 버튼 / `--auto-cycle` 실행 시에만)

```text
MachineController.DoOneDieAsync → PlaceOnePickerAsync(9373-9385)
      ArmX    = placeArmX + OffsetX     (가산)
      StageY  = HomeY + OffsetY         (가산)
      PickerT = OffsetT                 (직접 지령)
```

### ⚠️ 두 경로의 부호 계약이 상반된다 (기존부터 존재하던 불일치)

- X: 시퀀스 경로는 **감산(-)**, 레거시 경로는 **가산(+)**
- T: 시퀀스 경로는 `teaching - T`, 레거시 경로는 `T` 직접 지령
- Y: 양쪽 모두 가산(+)

이 불일치는 이번 수정으로 만든 것이 아니라 기존 코드의 상태다. 지금까지는 X/Y가
항상 0.0이라 드러나지 않았을 뿐이다. OffsetT는 복원 전부터 실값(bottom_angle_deg)
이었으므로 T 불일치는 이전부터 유효한 문제였다. **어느 부호가 물리축에 맞는지
실장비 검증 필요. 코드만 보고 임의 통일 금지(인계 문서 §10 원칙과 동일).**

### 모션에 영향 없는 소비처 (확인 완료)

- `MachineController.dieOffsets` 누적: write-only 사변수, 읽는 곳 없음
- 측정 레코드/CSV/Lot JSON, EventLogger 로그, TpuVisionTestControl UI 표시
- `MatchResultDto`용 오버로드(OffsetX/Y=0 유지): 유일 호출자 `MatchBottomOffsetAsync`가
  호출부 없음(사실상 dead code)

## 2. 이중 적용 없음 (확정)

- OffsetX/Y → Die 레코드 → Place 좌표 계산에만 사용
- BottomCenterOffsetX/Y → `StoreRuntimeSideInspectionCorrection` → SideVisionY 카메라
  축(0도=CenterX, 90도=CenterY)에만 사용, Side의 Picker X/Y/T는 티칭 좌표만 사용
- Side 카메라 이동은 die 위치를 바꾸지 않고, Place 계산은 Side 보정값을 읽지 않으므로
  값이 같아도 합산 불가
- 유의점(low): 같은 와이어 키를 두 필드가 서로 다른 검증으로 읽는다
  (OffsetX/Y: 50mm 클램프 + 키 2개 / BottomCenterOffsetX/Y: 무클램프 + 폴백 키 8개).
  극단 입력이나 폴백 키 전송 시 두 필드 값이 달라질 수 있다.

## 3. Collet Calibration 경로 영향 없음 (확정)

- `TryBuildSideFocusCorrection`(1731): `BottomCenterOffsetX/Y`(1768-1769)와
  `bottom_width_mm`/`bottom_height_mm`만 사용. OffsetX/Y 접근 0건
- `ColletRotationCenterCalibrationSequence`(COC): `BottomVisionOffset` 자체를 소비하지
  않음. `VisionCocResult` 픽셀 중심만 사용

### BottomCenterOffsetX/Y 크기 검증 현황 (50mm 차단 제거 상태의 방어선)

1. Vision측 정규화: `BottomInspector.cs:228-235` — |v|>100이면 절대 픽셀로 보고 mm 변환
2. 생산 경로 2mm 게이트: `PickerBottomAndSideInspectionSequence.cs:16/1374-1398` —
   단, **Collet Calibration 경로에는 미적용**
3. 모션 소프트리밋: `VisionUnit.cs:388` VS-SOFT-LIMIT 알람 —
   단, `SoftLimitEnabled=false`인 축에서는 무력화

잔여 리스크(low): Calibration 경로 자체에는 크기 상한이 없어, Vision 정규화 게이트
(>100)를 통과하는 100 이하의 픽셀성 값이나 소프트리밋 안쪽 오류 값은 잘못된 초점
스캔 중심으로 이어질 수 있다.

## 4. Vision 키 계약 일치 (확정)

```text
Vision: BottomInspector "Offset X/Y"(mm) → VisionCommandCore.BuildBottomInspectionPayload
        → bottom_offset_x_mm / bottom_offset_y_mm (1순위) + bottom_item_offset_x/y
Handler: InspectionResultDto.Parse → OrdinalIgnoreCase 정확 일치
        → ReadValidatedBottomOffset이 bottom_offset_x_mm 첫 매칭
```

키 불일치로 항상 0.0이 되는 경로는 발견되지 않았다.

## 5. 실장비 검증 시 추가 확인 사항

1. **Place 위치가 처음으로 실값 보정된다.** 첫 생산 가동 전 DryRun/단일 die로
   Place 정밀도와 보정 방향(경로 A의 X 감산이 물리적으로 맞는지)을 확인할 것
2. **X/T 부호 계약 상반**(경로 A vs B): `--auto-cycle`을 쓰는 경우 반드시 별도 확인
3. **시뮬레이션/DryRun 모드**: `AutoVisionRequestService.cs:1317`의 ±0.015mm 난수가
   이제 Place 좌표에 유입된다(이전에는 0으로 소거). 시뮬 Place 좌표가 사이클마다
   미세하게 흔들리는 것은 정상
4. **무알람 폴백**: 50mm 초과/NaN 값은 알람 없이 0.0(무보정 Place)으로 대체된다.
   Bottom 로그(`AUTO-VISION-BOTTOM-INSPECT-*`)의 offset 값과 Place 결과를 대조할 것
5. 50mm 게이트는 픽셀 좌표(수천)는 막지만 수십 mm급 오류 mm 값은 통과시킨다
