# InputStage 리뷰 좌표 보정 검증 — 체크리스트

작성일: 2026-09-06
기준 문서: [구현 프롬프트](input-stage-review-offset__01-implementation-prompt.md)
현재 구현 및 승인 적용 상태: [구현 현황](input-stage-review-offset__03-implementation-status.md)

체크 표시는 실제 수행 결과만 반영한다. 최신 수정본은 오프라인 183개와 실제 등록 프로젝트 격리 Build(오류 0개·기존 경고 41개)를 통과했다. A~F의 체크는 정적/오프라인 확인 또는 명시된 미실시 기록이며 실제 UI/장비 통합 통과를 뜻하지 않는다. 이전 단계의 113개 검사와 Build 오류 0개/기존 경고 41개는 당시 구현 결과이며 아래 현재 회귀 검증과 구분한다. 사용법은 [05](05-operation-and-log-guide.md)를 따른다.

## A. 준비 및 동시 작업 보호

- [x] 소스 기준 D:/Source/CDT-320_New와 현재 master/ahead 93, HEAD 478ffef077ff0b95bfa64e796e3970e4810b093d를 확인했다.
- [x] 사건 로그 기준은 D:/Source/EQP_Handler/20260905/핸들러로그다. 9월 6일 읽기 전용 화면과 시점을 혼동하지 않는다.
- [x] 다른 작업 AppSettings/RecipeStore/csproj/README/Factory/reset 문서는 이번 변경에서 제외했다.
- [x] 이번 소스 7개와 이전 단계에서 승인·반영된 파일을 03에 분리했다.
- [x] 최종 저장 직전/최종 diff에서 동시 변경과 허용 범위를 다시 확인한다.
- [x] Auto 이동·축 명령·인터락·픽업 수식이 그대로인지 최종 확인한다. PickTargets 변경은 Audit 계측만 허용한다.
- [x] rollback/reset/restore/revert/checkout/소스 복사·복붙·과거 코드 덮어쓰기를 하지 않는다.
- [x] 운영 데이터·실장비 로그·설정을 변경하지 않고 장비/카메라/IO를 실행하지 않는다.

## B. 단발·누적 보정과 legacy 기준

- [x] 단발 X/Y와 누적 X/Y는 기본 20 mm와 저장된 더 큰 값을 보존한다.
- [x] 반 피치 상한·숨은 clamp를 추가하지 않는다. finite/양의 pitch/한계 검사는 유지한다.
- [x] 누적량은 후보 origin - 저장 baseline이며 APPLY/재선택/재열기로 기준을 바꾸지 않는다.
- [x] baseline 없는 구형 자료는 변경 없는 canonical Material을 최초 기준으로 채택하고 저장한다.
- [x] BASELINE-ADOPTED/SAVED에서 과거 누적량 복원 없음과 기준 채택·저장 결과를 구분한다.
- [x] 기준 저장 실패 후 재시도에서도 저장 완료를 확인하고 새 검출을 시작한다.
- [x] 기존 기준 revision 불일치를 현재 원점으로 자동 덮어쓰지 않는다.
- [x] candidate 등록/Draft 적용/Material 저장의 성공 단계가 혼동되지 않는다.

## C. 기존 CONFIRM과 승인

- [x] 3점 선택·VERIFY MAP 버튼·점별 이동/촬영·수동 대응 확인이 필수 시작 경로에서 제거되어 있다.
- [x] 기존 시작점/순서 조건을 충족하면 CONFIRM / CONTINUE AUTO를 사용할 수 있다.
- [x] CONFIRM은 현재 context/후보를 확인하며 실측 성공·GEOMETRY VERIFIED를 만들지 않는다.
- [x] 승인 marker는 CONFIRM-CONTEXT-PRODUCTION/NONPRODUCTION으로 구분하고 Measurements를 위조하지 않는다.
- [x] wafer/revision/recipe/T/pitch/후보/세션이 달라지면 이유와 함께 확인 요청을 거부한다.
- [x] 기본 승인·저장 복원·소비에서도 현재 조건과 승인 자료를 확인한다.
- [x] 저장 완료 전 소비 차단과 저장 완료 시 캐시 무효화·동일 wafer/승인 ID 재검사를 유지한다.
- [x] 부분 진행 wafer의 변경 없는 재확인은 위치/상태/전체 승인 순서를 보존한다.
- [x] 다이가 3개 미만이라는 이유만으로 새 context 확인을 차단하지 않는다. 기존 전체 순서/revision 소실 제약은 별개로 유지한다.
- [x] Simulation 기존 SkipRunReview 경로와 실운전/비실운전 결과 모드 검사는 유지한다.

## D. 검출 결과 수명과 화면

- [x] 검출 시 wafer/revision/UID/grid/reference/session/request 및 T/pitch/조건을 기록한다.
- [x] 늦은 결과 등록·APPLY·CONFIRM·Commit 직전에 현재 조건을 재확인한다.
- [x] Jog/T 저장/선택/STOP/창 종료/세션 교체 뒤 오래된 pending은 사용할 수 없다.
- [x] 확인 제출 뒤 표·맵·상태·순서 편집 잠금을 유지한다.
- [x] 기존 수동 callback 종료/STOP/scope 해제 보호를 보존하며 새 자동 축 경로를 만들지 않는다.
- [x] 실제 UI 지연 Vision·STOP·저장 실패 통합 시험은 미수행으로 명확히 기록한다.

## E. 현재 수정본의 오프라인 회귀 검증

최신 전체 결과: **183개 오프라인 검사 PASS, 실제 등록 프로젝트 격리 Build 오류 0개·기존 경고 41개**. 정책 30개·실제 Common 로그 정책 26개·실제 Handler Config 12개·Material geometry 10개·진행 보존 30개·기존 검증점 상태 16개·기존 비생산 marker/context 15개·새 확인/provenance/JSON 44개를 실행했다. 앞 단계 다점 정책 시험의 성공은 이번 시작 경로가 실측된다는 의미가 아니다.

| ID | 상황 | 기대 결과 | 현재 결과 |
|---|---|---|---|
| V01 | JMB 로그 원점/피치와 첫 예약 좌표 | 누적(+8.579330,+0.336365) mm는 프로그램 좌표 차이로 해석 | 이전 분석 근거 유지 |
| V02 | 새 3점 UI/이동/필수 gate 제거 | 기존 CONFIRM 조작, 실제 측정/가짜 합격 없음 | 정적 검사 PASS |
| V03 | 20 mm 경계·큰 설정·축별 다른 한계 | 기존 독립 단발/누적 규칙 유지, 반 피치 clamp 없음 | 정책/Config 검사 PASS |
| V04 | legacy baseline 없음/최초 저장 실패/재시도 | canonical 기준만 1회 채택, 저장 완료 전 검출 미시작 | backend 검사 및 관련 정적 확인 PASS |
| V05 | context/candidate/wafer/모드/세대 변경 | 오래된 확인/offset/승인 소비 차단 | backend 검사 및 관련 정적 확인 PASS |
| V06 | 부분 진행 wafer의 변경 없는 확인 | 기존 위치/결과/예약/전체 순서 유지 | 진행 보존 30개 PASS |
| V07 | 부분 진행 자재의 순서 소실 또는 좌표 변경 | 무조건 재승인하지 않고 이유 기록 | backend 검사 및 관련 정적 확인 PASS |
| V08 | 검출/APPLY/저장 실패 또는 취소 | 요청/후보/Draft적용/저장완료 이벤트 구분 | 정적 검사 PASS, 장비 통합 미실시 |
| V09 | 최소 로그 모드에서 Review 및 픽업 Audit | 허용 접두사 의존 없이 영속 대상 | actual QMC.Common 정책 26개 PASS |
| V10 | 미수신 raw, 정상 0, NaN/Infinity | unavailable/null/비유한값과 실제 0 구분 | 확인/provenance/JSON 44개 및 포맷 정적 검사 PASS |
| V11 | PICKUP-TARGET 로그 추가 전후 | 원본 계산 메시지 재사용, 수식/분기/축 명령 완전 동일 | 진단 예외 처리 보강 후에도 계측 제외 소스 동일 검사 PASS |
| V12 | Simulation SkipRunReview true/false | 기존 skip/수동 CONFIRM 경로 유지, 실측 성공 아님 | 정적 검사 PASS, 실제 UI 미실시 |
| V13 | 늦은 결과·STOP·저장 실패 | 기존 보호 유지, 로그 실패가 결과를 바꾸지 않음 | 정적 검사 PASS, 장애 주입 통합 미실시 |

실행 도구: 정책 (정리된 테스트 파일), Config (정리된 테스트 파일), Material (정리된 테스트 파일), 진행/context (정리된 테스트 파일), 로그 정책 (정리된 테스트 파일). 새 CONFIRM context 검사 (정리된 테스트 파일) 44개도 최신 Handler assembly에서 PASS다. 최종 근거는 03에 연결했다.

## F. 코드·빌드·로그 확인

- [x] 이번 7개 소스 이외 다른 채팅 변경을 이번 성과로 포함하지 않는다.
- [x] 이번 소스 7개의 BOM/CRLF/EOF와 git diff --check를 확인했다. 다른 작업의 변경과 분리한다.
- [x] 별도 OutDir/중간 경로 /t:Build만 실행한다. Clean/Rebuild/소스 복제/운영 출력/배포는 금지한다.
- [x] 최신 전체 테스트 수치와 최종 assembly 반영 여부를 03에 갱신한다.
- [x] IN-REVIEW-*는 input-stage-review-v1과 현재 조건/원점/한계/실측 여부를 포함한다.
- [x] DETECTION_RAW의 captureXYT와 DETECTION_PENDING의 이동 후 actual을 구분한다.
- [x] APPLY_COMMITTED는 Draft 성공이며 materialSaved=false, SAVE-COMPLETE만 최종 저장 성공이다.
- [x] PICKUP-TARGET는 실제 계산 시점의 UID/head/wafer/grid/order/run 및 기존 수식 메시지를 보존한다.
- [x] 새 Audit은 최소모드에서도 영속 대상이며, 로그 실패는 승인/픽업 계산 반환을 바꾸지 않는다.
- [x] 승인 폴링 매 50 ms 반복 기록 없이 이벤트/거부 이유 변경 중심으로 기록한다.
- [x] 최종 사용법/이벤트명/제약이 실제 소스와 일치한다.

## G. 실장비 검증 — 이번 작업에서 수행하지 않음

- [ ] 별도 장비 실행 권한과 실제 장비 상태를 확인한 뒤 현장 시험한다.
- [ ] RAD↔JMB 전환, 동일 recipe 재개, Material 재사용, 프로그램 재실행을 구분한다.
- [ ] 기존 CONFIRM 시작과 필요 시 단일 DIE DETECTION/APPLY가 추가 3점 절차 없이 동작하는지 확인한다.
- [ ] 큰 정상 OFFSET 및 한계 초과/오래된 pending/저장 실패 거부를 구분한다.
- [ ] 각 head slot의 첫 픽업에 대해 Input raw·PICKUP-TARGET·Bottom 결과와 실제 XY/T를 비교한다.
- [ ] 새 로그와 context 확인을 실제 맵/픽업 정확도 합격으로 확대 해석하지 않는다.

## H. 변경 및 결과 기록

| 구분 | 기록 |
|---|---|
| 현재 요구 | 수동 3점 절차 제거, 기존 CONFIRM, 단발/누적/저장 보호 유지, 추후 분석용 Audit |
| 소스 범위 | UI 4개 + Material 2개 + 픽업 계산 로그 1개; 03 참조 |
| 기존 동작 불변 | Auto 이동/축 명령/인터락/픽업 수식 변경 금지, 계산 메시지 재사용 Audit만 추가 |
| baseline | 구형 현재 canonical 기준 1회 채택·저장, 과거 누적량 미복원 |
| 최종 검증 | 현재 오프라인 183개 PASS, 4차 격리 Build 오류 0개·기존 경고 41개. 이전 113개는 이력으로 구분 |
| 문서 | 01~04 최신화, 05-operation-and-log-guide.md 신설 |
| 실제 장비 | 미운전·미배포·미검증, 간헐 쉬프트 해결 확답 불가 |
