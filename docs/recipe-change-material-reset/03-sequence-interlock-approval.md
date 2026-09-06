# 레시피 변경 초기화 — 승인 범위와 겹침 기록

작성일: 2026-09-06
기준: D:\Source\CDT-320_New / master / HEAD 478ffef077ff0b95bfa64e796e3970e4810b093d 및 현재 로컬 변경

## 승인된 동작

사용자는 다음 3개 구체 범위에 대해 “응 진행해줘”라고 승인했다. 다른 작업과 겹치는 코드는 먼저 승인받으라는 조건도 유지한다.

1. 레시피 적용 준비·실물 제거 확인부터 적용, 생산 상태 초기화, Material·마지막 레시피 저장 완료까지 운전 진입을 보호한다. START·신규 수동 동작·Jog·직접 IO·INIT가 전환과 겹치지 않게 한다. STOP은 유지한다. 적용 이후 오류에는 START 차단 사유를 남긴다.
2. 종료된 이전 공정의 Resume/Failure, 맵, 검사 허가·재시도·Vision 다이 주소, FLOW 강제 승인을 정리한다. 다음 START는 기존 INIT/안전/Recipe/Vision ACK 검사를 거쳐 새 Material 기준으로 시작한다.
3. 실장비의 다른 레시피 A→B 전환에서 Front/Rear P1~P4 원본 FLOW 8개를 확인한다. 하나라도 ON이거나 필요한 실입력을 읽지 못하면 거절한다. 소프트웨어 강제 ON은 실물 확인 근거로 쓰지 않는다. FLOW OFF여도 작업자의 실제 제품 제거 확인을 받는다.

1번의 실제 작업 종료 조건을 위해 취소/시간 초과 이후에도 남는 Task를 추적한다. Unit/monitor/prefetch, 공통 Step 대기, READY 자체 대기, Place 상승, 동적 이동 및 Bottom/Side 최종 결과의 생성 지점에 등록하고, 실제 완료 전에는 전환과 새 START를 거절한다. 기존 이동 순서·속도·타임아웃·취소 처리 자체는 변경하지 않는다.

같은 레시피 정상 재적용, 시작 시 Material 사용 ‘예’에는 전체 초기화를 적용하지 않는다. 시작 시 ‘아니오’는 빈 상태 생성·교체·종료된 공정 정리의 공통 핵심을 사용한다.

## InputStage Review 겹침 추가 승인

별도 작업의 Review 검사·OFFSET·CONFIRM 변경과 초기화 연결이 겹쳐 사전 승인 요청을 보냈다. 사용자는 다음과 같이 승인했다.

> 이 부분을 너가 먼저 수정해. 그리고 나서 다른쪽 확인해서 검증하고 구현하라고 할테니깐.

이에 따라 다음 연결을 수행한다.

- 새 Form1.RecipeReset partial에서 Review의 기존 요청 만료 API를 호출한다. 종료된 UI 요청 세대·임시 승인·검출 정보만 정리한다.
- modeless Review 창, 미완료 OneShot, 종료 대기, Jog 시작/축/Scope, Embedded Vision 전환/Scope, Vision Test가 남으면 초기화 진입을 거절한다.
- MaterialStateService에서 완료된 Review의 검증 증거와 저장 대기 참조를 정리한다.
- 상위 시퀀스가 중지 시간 초과로 먼저 종료돼도 실제 Review Commit/Baseline/DefaultOrder 저장이 실행 중이면 레시피 전환을 거절한다. 실행 카운터와 전환 중 신규 진입 차단을 하나의 짧은 전용 잠금으로 관리한다. 그 잠금 안에서 Controller/Material/파일 IO를 호출하지 않는다. 저장 실패 후 남은 표식만으로 실행 중이라고 판단하지 않는다.
- 다른 작업 파일에서는 필요한 Review 저장 진입점 연결만 현재 소스에 최소 수정한다. 기존 검사/OFFSET/CONFIRM 본문을 다른 복사본으로 교체하지 않는다. 실제 변경과 검증 근거는 04 인계 기록에 남긴다.

## 유지하는 범위

- 진행 중 LOT, LOT 이력·통계, Teaching/Calibration/영속 보정값, 원점·INIT 완료 상태.
- 실제 축/IO/실린더 상태와 기존 충돌 회피·위치·속도·알람 해제 절차.
- PickerPickUpSequence.MotionResolvers.cs의 재픽업 완료 검사 flowOn = true; Test 코드.
- 다른 작업의 InputPick/Review/픽업 순서 편집 기능. 승인된 연결 외에는 수정·롤백하지 않는다.

새 브랜치 생성·전환, commit/push, 운영 폴더 배포, 장비 실행·이동·IO 출력은 수행하지 않는다. 실제 센서·현장 시퀀스 확인은 로컬 격리 검증과 구분한다.

[구현 프롬프트](D:/Source/CDT-320_New/docs/recipe-change-material-reset/01-implementation-prompt.md) · [체크리스트](D:/Source/CDT-320_New/docs/recipe-change-material-reset/02-validation-checklist.md) · [구현 인계](D:/Source/CDT-320_New/docs/recipe-change-material-reset/04-implementation-handoff.md)
