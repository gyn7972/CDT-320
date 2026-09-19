# InputStage 런타임 Vision 좌표 전파 후 Review 승인 유지 계획

작성일: 2026-09-07  
기준 저장소: `D:/Source/CDT-320_New`  
승인 근거: 사용자가 원인 분석 결과를 확인한 뒤 "수정되어야돼"라고 구현을 명시 승인함.

## 사건과 목표

- 최초 Wafer Align/Die Mapping Review 승인 뒤 4개 Die를 픽업했다.
- 마지막 준비 Die의 Input Vision offset을 남은 1,224개 미촬영 Die 좌표에 전파하면서 후보 좌표 서명이 변경됐다.
- 2026-09-06에 추가된 엄격한 승인 context 검사가 이 의도된 변경도 임의 변경과 동일하게 거부해 Auto 재시작 때 Review가 다시 열렸다.
- 동일 Wafer와 동일 Mapping revision에서는 최초 Review 한 번만 요구하되, 임의 좌표·레시피·세션·T·pitch 변경에 대한 기존 재승인 보호는 유지한다.

## 구현 범위

1. 저장 승인 context와 Vision 전파 직전 context가 완전히 같은지 먼저 확인한다.
2. 기존 `TryApplyLastVisionOffsetToPendingInputDies` 안에서만 미촬영·미예약 Die 좌표를 갱신한다.
3. 전파 직후 context에서 후보 좌표 서명 이외 값이 모두 동일할 때만 저장 승인 context를 새 후보 서명으로 재기준화한다.
4. 다른 조건이 함께 바뀌거나 context-only 승인이 아니면 승인 유지 없이 fail-closed 처리한다.
5. 좌표와 승인 서명은 같은 Material 잠금 범위에서 변경하고 하나의 저장 요청으로 통지한다.
6. 기존 픽업 순서, 4개 완료 결과, 좌표 계산식, Motion/IO/인터락은 변경하지 않는다.

## 검증 계획

- 정책 단위 시험: 엄격한 일반 context 비교는 후보 좌표 변경을 계속 거부한다.
- 정책 단위 시험: 승인과 전파 직전 context가 같고 후보 서명만 바뀐 경우에만 재기준화한다.
- 정책 단위 시험: Wafer/revision/조건/세션/request/T/pitch/origin/baseline/mode 중 하나라도 바뀌면 재기준화를 거부한다.
- 정적 시험: 재기준화 호출이 `TryApplyLastVisionOffsetToPendingInputDies` 경로에만 연결됐는지 확인한다.
- 최신 소스로 오프라인 시험을 다시 실행하고, 운영 출력과 분리한 `OutDir`로 `/t:Build`만 실행한다.
- 실제 장비 운전, 운영 폴더 배포, 현재 손상 상태 JSON 자동 복구는 수행하지 않는다.
