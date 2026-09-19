# 레시피 변경 초기화 — 구현·검증 체크리스트

작성일: 2026-09-06
기준: D:\Source\CDT-320_New / master의 현재 로컬 코드

[구현 프롬프트](D:/Source/CDT-320_New/docs/recipe-change-material-reset/01-implementation-prompt.md) · [승인 기록](D:/Source/CDT-320_New/docs/recipe-change-material-reset/03-sequence-interlock-approval.md) · [다른 작업 검토용 인계](D:/Source/CDT-320_New/docs/recipe-change-material-reset/04-implementation-handoff.md)

현재 단계: 승인된 전환·기동 초기화와 Review 겹침 연결 구현 완료. 격리 빌드·자동 검증을 수행했다. 아래 체크는 코드 구현과 명시한 격리 검증 범위이며, 현장 운전 완료를 뜻하지 않는다.

## A. 작업 기준

- [x] D:\Source\CDT-320_New의 현재 master/미커밋 파일 기준으로 작업. 브랜치 생성·전환·commit/push 없음.
- [x] 다른 작업의 InputStage Review 변경을 재확인하고 겹침 승인 후 최소 연결만 수정.
- [x] 기존 dirty InputPick/ReviewGeometry 파일의 본문 보존을 선언 변경 역변환 SHA256으로 확인.
- [x] 기존 코드 롤백, 다른 복사본 덮어쓰기, 운영 폴더 배포 없음.
- [x] 보호 대상 MotionResolvers 전체 파일 SHA256 유지: 5CF43B053B518659063BBD00883A016C9E178312C18DBFA75654C8291957E3A4.

## B. 적용 순서 및 실패 처리

- [x] ProjectPage 실제 적용 및 비기동 LoadMachineRecipe를 공통 전환 경로로 연결.
- [x] Project/전체 Unit 사전검증 후 빈 후보 준비. 확인 취소 시 파일·Material·활성 이름 보존.
- [x] 확인창 전 적용 준비를 독점하고 완료 저장까지 유지. 확인 이후 Material 참조/세대, 활성 레시피/LOT, 운전/센서 재검증.
- [x] Project/Unit 파일 집합·내용 해시 검증. Project 저장 후 Unit 변경을 새 기준으로 받아들이지 않음.
- [x] 실제 A→B에서 작업자의 모든 실제 제품 제거 확인. 실제 Front/Rear FLOW 8개 ON 또는 실입력 읽기 실패 거절.
- [x] START·수동·직접 IO·Jog·INIT 등록 경합 차단. STOP 및 원래 안전 검사 유지.
- [x] 적용/상태 교체 후 오류, 활성 Project 저장 시도 실패, Material/마커/설정 저장 실패 시 START 차단.
- [x] 실패한 초기화 대상에 대한 같은 이름 재시도도 초기화와 저장을 완료한 뒤 차단 해제.
- [x] Material flush, .last_project, AppSettings.LastProject 성공 및 최종 이름/LOT/상태 정합성 확인 후 성공 처리.

## C. 초기화와 보존

- [x] A→B와 기동 ‘아니오’의 빈 상태 생성·교체·종료 공정 정리를 공통화.
- [x] 새 레시피의 Input/Good 1~2단, 5개 카세트, 25슬롯 기본 계약과 BIN All 적용.
- [x] Wafer/Die, 슬롯 점유·매핑, Material 인덱스/캐시, Hybrid/FLOW 승인 및 요청 세대 정리.
- [x] Resume/Failure, Front/Rear 검사·픽업 허가·재시도·배치 좌표, Vision 다이 주소 정리.
- [x] InputStage/Feeder의 옛 Material·맵, 입력 픽업 순서·활성 입력 맵, 카세트/Plate 투영 정리.
- [x] 실제 종료된 Review 요청/승인/검출 정보와 실패 후 저장 대기 참조 정리.
- [x] 활성 LOT, INIT/원점, Teaching/Calibration/영속 Pick·Place·Z 보정 및 운영 이력 보존.
- [x] 같은 레시피 정상 재적용과 기동 ‘예’는 전체 초기화 제외. 이름 정규화 정책 유지.
- [x] 최초 빈 이름 적용이라도 Material 잔재가 있으면 확인 없이 삭제하지 않고 거절.
- [x] 다음 START의 기존 Recipe/Vision ACK/안전 확인 및 Output GOOD/NG 전체 준비 요청 유지.
- [x] 세대·Revision·저장 내구성 워터마크를 0으로 되돌리지 않음.

## D. Review 종료와 늦은 응답

- [x] modeless Review, OneShot, 종료 대기, Jog 시작/축/Scope, Embedded Vision 전환/Scope, Vision Test 확인.
- [x] 기존 Cleanup 함수를 초기화용으로 호출하지 않으며 Stop/Scope Dispose로 강제 종료하지 않음.
- [x] 실제 Commit/Baseline/DefaultOrder 실행을 함수 반환까지 추적. 상위 시퀀스 종료 표시만으로 초기화하지 않음.
- [x] Review 저장 실행 중 reset 진입 거절, reset 보호 중 새 Review 저장 진입 거절.
- [x] 종료 후 실패 pending 참조는 초기화 가능. 완료되지 않은 작업과 실패 표식을 구분.
- [x] 기존 세대/State/Wafer 정합성 검사 유지. 옛 요청을 새 Material 승인에 재사용하지 못함.
- [x] 등록/검증/해제 예외 및 scope 중복 Dispose의 보호 누수 확인.
- [x] 취소/시간 초과 뒤 부모 작업이 끝나도 실제 Unit/Step/READY/Place 상승/동적 이동/최종 검사 Task를 추적하고 전환·새 START를 차단. 원래 동작/취소/타임아웃 정책 유지.

## E. 로컬 검증 결과

검증 산출물: C:\Users\QMC\Documents\CDT-320 2\output\recipe-change-material-reset-20260906

| 검증 | 결과 | 범위/근거 |
|---|---|---|
| 전체 솔루션 Build | PASS, exit 0 | 전용 OutDir와 프로젝트별 중간 출력. 최종 빌드 중 .cs/.csproj 해시 변동 0건. 경고는 build.log |
| 빈 Material 후보 | 65 assertions PASS | 실제 어셈블리, 새 그래프/레벨/슬롯/활성 LOT/원본 불변 |
| 저장 API | 34 assertions PASS | 실제 두 소스와 격리 대역. 정상 저장/교체, 잠금 오류, 호환/동시 저장 |
| Material 교체와 Review | 67 assertions PASS | 실제 어셈블리, 저장 worker 차단. active/reset 상호 배제, 세대/캐시/잔여 pending 정리 |
| Controller/runtime | 185 assertions PASS | 실제 어셈블리, 생성자 없는 장비 대역. 전환·기동 정리, 보존, 실입력 대역, scope/START 경합, 취소된 부모와 미완료 자식 Task |
| Cassette 투영 | 15 assertions PASS | 실제 Unit 메서드, 생성자/장비 동작 없이 맵·스캔/rollback 참조 정리 |
| 파일 동일성 | 21 assertions PASS | 실제 파일 helper와 격리 파일. 내용·집합·이름·잠금, Project만 갱신 |
| 직접 모션/IO 보호 | 103 assertions PASS | 실제 어셈블리의 가드/등록 경합. 실제 명령 대역 사용 |
| Form1 적용 workflow | 50 시나리오 / 281 assertions PASS | 실제 Form1.RecipeReset.cs 직접 컴파일, 외부 의존성·WinForms는 stub. 분기/호출 순서/실패/Review UI guard |
| Review UI 실제 helper | 115 assertions PASS | 생성자 없는 실제 Form1 helper. pending/토큰/세대·늦은 응답 거절, 격리 진단 로그 |
| 기존 FLOW 복구 회귀 | 183 assertions PASS | 현재 기능 빌드의 서비스/IO control/시퀀스 읽기, Admin/현재 die/Place/다음 pickup, 실제 IO 대역 |

각 시험은 실제 연결 범위가 다르므로 이 개수를 하나의 실장비 통합 시험 수로 합산하지 않는다. 실제 메인 Form/장비 생성, 운영 Snapshot 저장, 실제 축/IO 실행은 하지 않았다.

## F. 통합 시나리오와 남은 현장 확인

| ID | 시나리오 | 확인 수준 |
|---|---|---|
| T01~T04 | 정지 A→B, 잔재 제거 확인, 캐시만 잔존, 카세트 1↔2단 | workflow + 실제 factory/MSS/runtime 검증 |
| T05~T07 | Project/Unit 실패, 취소, 확인 중 파일/상태 변경 | workflow + 실제 파일 helper 검증 |
| T08~T10 | START/수동 경합, 실물 입력 오류, FLOW OFF 잔재 | 실제 Controller/guard 대역 + 작업자 확인 분기 검증. 실제 센서는 현장 미실시 |
| T11~T13 | 같은 레시피, 기동 ‘예’/‘아니오’, 활성 LOT | workflow 및 실제 생성/runtime 보존 검증 |
| T14 | 늦은 FLOW/Review 승인·저장 | 실제 MSS 세대/실행 gate + workflow 검증. 실제 비전 지연 주입은 미실시 |
| T15~T17 | flush/마커/설정/적용 중 오류와 재시도 | workflow 실패 주입 + 저장 API + 실제 Controller START 차단 |
| T18 | 이전 Snapshot 저장 지연 | 전역 세대/워터마크 보존 검증. 실제 장기 IO 경합·종료 주입 미실시 |
| T19~T20 | 적용 직후 재시작, 저장 중 강제 종료 | 프로세스/운영 저장을 사용하는 통합 시험 미실시 |
| T21~T22 | Sim/DryRun, LOT/원점/보정 보존 | 실제 Controller/MSS 대역 및 workflow. 현장 전후 비교 미실시 |
| T23 | 기존 FLOW 승인→현재 Die Place | 별도 FLOW 회귀 183 assertions, 실제 장비 미실시 |

- [ ] 실장비에서 실제 제품 제거 후 A→B, Material·맵·표시 초기화 확인.
- [ ] 다음 START에서 새 매핑/준비 경로, Output 전체 준비 및 안전 게이트 확인.
- [ ] Front/Rear 8개 원본 FLOW 실입력과 강제 승인 초기화 확인.
- [ ] LOT·원점·Teaching/보정 전후 비교, 같은 레시피/기동 ‘예’ 자료 보존 확인.
- [ ] 적용 직후 재시작 및 저장 도중 종료 시 복원/불일치 게이트 확인.

다른 작업의 후속 수정은 이 빌드 이후 별도로 검증해야 한다. 현재 검증 결과를 나중에 바뀐 소스까지 통과한 것으로 확대하지 않는다.
