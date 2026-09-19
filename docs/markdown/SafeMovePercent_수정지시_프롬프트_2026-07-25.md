# CDT-320: 캘리브레이션 안전위치(Avoid) 이동에 SafeMovePercent(속도+가감속) 전면 적용

## 배경
로컬 폴더 D:\Source\CDT-320_New (C# WinForms, QMC.CDT-320.sln) 기준으로만 작업한다.
CALIBRATION 페이지의 "안전위치(Avoid) 이동 속도 %"(CalibrationData.SafeMovePercent, 범위 1~100, 기본 7)는
캘리브레이션 중 안전위치(Avoid) 이동의 속도·가속·감속을 "각 축 Config.Default × (%/100)"로 제어하는 값이다.
현재 이 값이 적용되는 경로는 PickerSequenceBase.MovePickerAxisCommandAsync(useSafeMoveMotion=true) 한 곳뿐이며
(QMC.CDT-320\Sequencing\Picker\PickerSequenceBase.cs 2754~2845행 — 속도/가속/감속 모두 스케일, 이것이 기준 구현),
나머지 안전위치 이동 경로는 SafeMovePercent를 무시하고, 특히 가감속은 축 기본값 100%로 나간다.
실장비에서 "설정이 안 먹는 느낌 + 가감속이 너무 큼"으로 확인됨.

## 수정 원칙
1. 안전이동 모션 = 각 축 Config.DefaultVelocity / Config.Acceleration / Config.Deceleration × (SafeMovePercent/100).
   속도만이 아니라 가속·감속 모두 반드시 같은 %로 스케일한다.
2. SafeMovePercent는 Machine.VisionUnit.Config.CalibrationData.SafeMovePercent에서 라이브로 읽는다.
   1 미만/NaN이면 적용하지 않고 기존 동작 폴백, 100 초과면 100으로 클램프
   (기준: PickerSequenceBase.ResolveCalibrationSafeMovePercent, 2826행).
3. 명시(explicit) 모션 경로로 전달하여 전역 MotionSpeedScale과 중첩되지 않게 한다(기존 설계 유지).
4. 측정 이동 속도(CalibrationMotionSettings.MoveVelocity 등)는 건드리지 않는다. 안전위치 이동만 대상.
5. 공정(Auto run) 경로의 Avoid 이동 동작은 절대 바뀌면 안 된다. 기존 유닛 API 시그니처의 동작은 유지하고,
   안전이동용 오버로드/신규 메서드를 추가해 캘리브레이션 호출부만 교체한다.
6. Z 우선 하강/상승 등 기존 이동 순서·인터록(MovePickerAvoidGroupSafely, MovePickerSideAvoidGroupSafely,
   MoveZAxesToSafeFirst)은 그대로 유지한다.
7. 로그: 기준 구현의 PickerMoveCommand 로그처럼 velocity/acceleration/deceleration/safeMovePercent/
   safeMoveApplied를 남겨 실장비 로그로 검증 가능하게 한다.

## 수정 대상 (전부)

### A. 유닛 레벨 Custom 속도 Avoid 이동 — SafeMovePercent 미적용 + 가감속 100% (핵심)
원인: PickerFrontUnit.cs 1600~1608행(MovePickerAxis)에서 velocity만 customSpeed를 쓰고
acceleration/deceleration은 UnitJogVelocityResolver.ResolveAcceleration/Deceleration
(QMC.CDT-320\Equipment\Unit\Common\IUnitJogController.cs 43~61행 — Jog/기본 가감속 100%)를 쓴다.
PickerRearUnit도 동일 구조.

PickerFrontUnit/PickerRearUnit에 percent 기반 안전이동 API를 추가하고(각 축별 Default × % 명시 모션),
아래 호출부를 교체한다. motion.MoveVelocity(측정 속도)를 안전이동에 쓰는 것 자체가 오용이므로 제거:
- Sequencing\Calibration\VisionCameraCalibrationSequence.cs 834~835행, 872~873행
  (Front/Rear MoveToOutputSideAvoidPosition / MoveToInputSideAvoidPosition)
- Sequencing\Calibration\NeedlePinCalibrationSequence.cs 222행, 227행
  (MoveToFrontPickerAvoidPosition / MoveToRearPickerAvoidPosition)
- Sequencing\Calibration\ColletCalibrationSequence.cs 762행, 813행 (MoveToOutputSideAvoidPosition)
- Ui\Dialogs\ColletCalibrationDialog.cs 2571~2575행(P-Y AVOID), 2594~2595행·2605~2606행(Z-AVOID)
  (MovePickerAxisToTeachingPosition(..., JogSpeedType.Custom, _moveVelocity) 호출들)

### B. 카메라 X축 Avoid 이동 — CalibrationMotionSettings 고정값(속도10/가감속200) 사용, % 미반영
- Sequencing\Calibration\AutoCalibrationSafePositionSequence.cs 150~155행(Input Camera X),
  188~193행(Output Camera X): _motion.MoveVelocity/MoveAcceleration/MoveDeceleration 대신
  해당 축 Config.Default × SafeMovePercent 값을 계산해 전달.
- Sequencing\Calibration\NeedlePinCalibrationSequence.cs 212~217행
  (OutputStageUnit.MoveVisionXToAvoidAndVerifyAsync)도 동일하게 교체.

### C. Auto Cal 안전복귀의 상대(반대편) Picker Avoid — Jog Fine/Coarse 속도 + 기본 가감속 사용
- Sequencing\Picker\PickerSequenceBase.cs MoveOppositePickerToAvoidAndVerifyAsync(2117행) →
  MoveFrontPickerToAvoidSequentialAsync / MoveRearPickerToAvoidSequentialAsync(2205행~)가
  MoveFrontPickerAxes(targets, fine, ...) 등 유닛 기본 경로를 사용한다.
  캘리브레이션 컨텍스트(CalibrationMotion != null)일 때는 A에서 만든 percent 기반 안전이동 API를
  사용하도록 분기한다. 공정 경로(CalibrationMotion == null)는 기존 그대로.

## 제외(변경 금지)
- PickerSequenceBase.MovePickerAxisCommandAsync의 기존 safe-move 로직(기준 구현)
- 레시피 파라미터 PickerZAvoidReturnSpeedPercent(공정용, PickerTransferTypes.cs) — 별개 기능
- 공정 시퀀스(PickerPickUp/Place/Inspection 등)의 Avoid 이동 동작
- CalibrationPage UI 및 SafeMovePercent 저장/로드 로직(정상 동작 확인됨)

## 완료 조건
- QMC.CDT-320.sln 빌드 성공(경고 증가 없음).
- 위 A/B/C 모든 호출부에서 안전이동 로그에 safeMovePercent와 스케일된 velocity/acceleration/deceleration이 찍힌다.
- SafeMovePercent=10과 100으로 각각 설정 시 로그의 속도·가감속 값이 정확히 10배 차이난다.

---

# 체크리스트

## 구현
- [ ] `PickerFrontUnit` / `PickerRearUnit`에 percent 기반 안전이동 API 추가 (축별 `Default × %` 명시 모션, 기존 이동 순서 유지)
- [ ] A-1: VisionCameraCalibrationSequence.cs 834~835, 872~873행 — 4개 호출 교체
- [ ] A-2: NeedlePinCalibrationSequence.cs 222, 227행 — 2개 호출 교체
- [ ] A-3: ColletCalibrationSequence.cs 762, 813행 — 2개 호출 교체
- [ ] A-4: ColletCalibrationDialog.cs 2571~2575, 2594~2595, 2605~2606행 — P-Y AVOID / Z-AVOID 버튼 경로 교체
- [ ] B-1: AutoCalibrationSafePositionSequence.cs 150~155, 188~193행 — Input/Output Camera X를 % 스케일 값으로 교체
- [ ] B-2: NeedlePinCalibrationSequence.cs 212~217행 — Output VisionX Avoid 교체
- [ ] C: PickerSequenceBase.cs 2117행 상대 Picker Avoid — 캘리브레이션 컨텍스트에서만 % 경로 분기
- [ ] 모든 신규 이동 명령에 `safeMovePercent`, 스케일된 vel/acc/dec 로그 추가

## 검증
- [ ] 솔루션 빌드 성공
- [ ] 로그에서 A/B/C 각 지점 `safeMoveApplied=True` + 스케일 값 확인
- [ ] % 값 10 ↔ 100 변경 시 로그 속도·가감속 10배 차이 확인
- [ ] 회귀: Auto run(공정) 중 Avoid 이동 속도/가감속 로그가 수정 전과 동일한지 확인
- [ ] 회귀: 측정 이동(각 캘의 Move Speed)이 변하지 않았는지 확인
- [ ] 실장비: AUTO CALIBRATION 시작 시 양쪽 Picker + 카메라 X 전부 저속·저가감속으로 이동하는지 육안 확인
