# 픽커 FLOW 일회성 복구 구현 프롬프트

기준: 2026-09-06 현재 로컬 저장소 D:\Source\CDT-320_New. 원격이나 다른 복사본을 가져오지 않고 현재 체크아웃을 직접 수정한다. 사용자의 후속 지시에 따라 브랜치를 생성하지 않고 기존 master에서 작업한다.

## 사용자 요청과 확정 사항

실장비에서 픽커가 실제 다이를 보유하고 Material도 해당 픽커에 있지만 FLOW 입력이 OFF라 진행하지 못하는 경우, Admin이 FRONT HEAD / REAR HEAD의 P1~P4 FLOW를 더블클릭해 현재 다이에 한해 ON을 인정할 수 있게 한다.
확인 문구는 “신호를 강제로 살리겠습니까?”를 포함하고 ‘예’에서만 적용한다.
해당 다이가 Place되어 Material이 OutputStage로 이동하면 해당 픽커만 해제한다. 다음 픽업에 승인 상태를 이월하지 않는다.

사용자의 후속 지시: PickerPickUpSequence.MotionResolvers.cs의 VerifyPickerHasDieDataAndFlowAfterPick 내부 flowOn = true; Test 코드는 변경하지 않는다. 픽업 동작을 전부 완료한 뒤 확인하려는 기존 의도를 유지한다. 이 메서드는 원본 동일성 검사로 보호한다.

## 구현 범위

- Front/Rear P1~P4, 총 8개 픽커를 독립적으로 관리한다.
- Admin 권한, 실장비 모드, 실제 FLOW OFF, 해당 위치의 Material을 확인한다. 확인창 이후에도 권한·운전 모드·센서·동일 다이를 다시 확인한다.
- 승인 상태는 프로세스 메모리에만 보관하고 Snapshot/Recipe/Config에 저장하지 않는다.
- Material 상태 락으로 승인과 현재 다이의 일치 여부를 보호한다. State/Die/위치 객체와 DieId, InputWaferInstanceId를 캡처해 다이 교체·수동 제거·복원으로 승인이 재사용되지 않게 한다.
- 실제 디지털 입력, 진공/Blow 출력, Unit.IsPickerFlowDetected와 기존 ReadPickerFlowState, 캘리브레이션 FLOW 검사는 변경하지 않는다.
- 생산의 현재 다이 흡착 확인에만 실제 FLOW 또는 일회성 승인 판정을 사용한다. 백그라운드 픽업 FLOW 검사는 요청 당시 다이를 고정해 다른 다이의 승인을 소비하지 않게 한다.
- Sim/DryRun 기존 통과 분기는 유지한다. 모드 전환은 일회성 승인을 무효화한다.
- UI에서는 강제 ON과 실제 센서를 구분해서 표시한다. 입력 행의 더블클릭에만 기능을 연결하고 단일 클릭/출력 토글/탱크 입력은 기존 동작을 유지한다.
- Place Material 전이 성공 시 해당 픽커 승인을 즉시 해제한다. 다른 픽커의 승인이나 미완료 Place의 승인은 해제하지 않는다.
- Material 변경 시 무효 승인을 정리하고 다음 실제 픽업 직전에도 해당 픽커의 승인을 제거한다. 새 픽업 직전에 Material이 남아 있다면 강제 상태를 지우고 진행하지 말고 기존 자재 점유 조건으로 중단한다.
- 등록/해제 로그에는 승인자, Front/Rear, 픽커 번호, DieId, Wafer instance와 사유를 기록한다.
- FLOW 대기 중에는 승인된 판정을 읽어 다음 단계로 진행한다. 이미 Alarm으로 종료된 시퀀스는 기존 RESET ALARM → START → Ready 경로로 재개한다. 다른 Alarm을 지우거나 임의 모션 재시작 경로를 만들지 않는다.

## 검증

현재 원본 대상의 별도 OutDir Build, git diff --check, 원본 Test 메서드 동일성 검사, 하드웨어에 연결하지 않는 복구 상태/권한/세대/Place/다음 픽업 검사, 실제 WinForms I/O 행 더블클릭·표시 검사를 수행한다.
실장비 축 이동·I/O 출력·자동 운전은 실행하지 않는다. 실제 설비 검증 항목은 미실시로 명시한다.
