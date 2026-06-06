# Stage 29 — TransferPickerUnit 시퀀스 통합

## 1. 현황 (Phase 1 조사)

TransferPickerUnit (TPU):
- LeftArm (front) + RightArm (rear) — 2 arm
- 각 arm: ArmX, ArmY axes + 4 PickerComponents
- 각 PickerComponent: PickerZ, PickerT axes + VacuumOut, BlowOut DO

PickerComponent 메서드 (기 구현):
- `PickupAsync` — PickerZ 하강 + Vacuum ON
- `MoveToFocusAsync` / `MoveToWaitAsync`
- `PlaceAsync` — PickerZ 하강 + Vacuum OFF + Blow + 상승

TpuArmUnit 메서드 (기 구현):
- `InspectBottomVisionAsync` — 4 picker Bottom 비전 촬상
- `InspectSideVisionAsync` — 4 면 검사 (90도 회전 포함)
- `PlaceDiesAsync` — 다이 일괄 배치

현재 사이클 (DoOneDie):
- `front.ArmX.ServoOn()` 만 사용
- `front.Pickers[0].VacuumOn() / VacuumOff()` 직접 호출
- **PickupAsync / PlaceAsync 메서드 호출 0건**
- **RightArm 사용 0건**
- **InspectBottomVision 호출 0건**

## 2. 작업 계획

### A. SoftLimit 확장
- ArmX 사용 범위: 0~1500mm (InputStage→Vision→OutputStage)
- ArmY: 0~300mm
- PickerZ: 0~50mm

### B. DoOneDieAsync 재작성
- ArmX 이동 (Pick 위치 300mm) — InterlockRegistry 검증 유지
- LeftArm.Pickers[0].PickupAsync() 호출 (PickerZ 하강 + Vacuum)
- ArmX 이동 (Bottom 비전 위치 700mm)
- LeftArm.InspectBottomVisionAsync() 호출
- ArmX 이동 (Place 위치 1200mm)
- LeftArm.Pickers[0].PlaceAsync() 호출 (PickerZ 하강 + Vacuum OFF + Blow + 상승)

### C. 다중 Picker 활용 (선택)
- 4-picker 동시 픽업 → 4 다이 배치 (속도 4x)
- 처음에는 picker[0] 1개만 (점진 확장)

## 3. 체크리스트

| ID | 항목 | 검증 |
|----|------|------|
| A1 | LeftArm.ArmX SoftLimit 확장 | 코드 검토 |
| A2 | LeftArm.ArmY SoftLimit 확장 | 코드 검토 |
| A3 | LeftArm.Pickers[*].PickerZ SoftLimit 적정 | 코드 검토 |
| A4 | RightArm 동일 처리 | 코드 검토 |
| B1 | DoOneDieAsync — PickupAsync 호출 | grep |
| B2 | DoOneDieAsync — PlaceAsync 호출 | grep |
| B3 | LeftArm.InspectBottomVisionAsync 호출 | grep |
| F1 | Build clean | warning 0 |
| F2 | verify_all 117/118 PASS | 회귀 무결성 |
| F3 | auto-cycle TPU 시퀀스 동작 | 로그 |
| F4 | 정직 audit — 런타임 ALARM 0 | 로그 검사 |
| F5 | 문서 갱신 | PPT |
