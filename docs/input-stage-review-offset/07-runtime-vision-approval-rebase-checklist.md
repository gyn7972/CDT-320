# InputStage 런타임 Vision 승인 재기준화 체크리스트

작성일: 2026-09-07

| ID | 확인 항목 | 상태 |
|---|---|---|
| C01 | 지정 저장소/브랜치/dirty 상태 확인 | PASS |
| C02 | 최초 승인 → 4 Die 픽업 → 남은 Die offset 전파 → 재시작 사건과 소스 연결 | PASS |
| C03 | 일반 `IsSameContext`의 후보 좌표 변경 거부 유지 | PASS |
| C04 | 전파 직전 저장 승인과 현재 context 완전 일치 확인 | PASS |
| C05 | 후보 좌표 서명만 달라진 경우에만 승인 context 재기준화 | PASS |
| C06 | context-only 승인 이외 또는 다른 조건 변경 시 fail-closed | PASS |
| C07 | 좌표/승인 변경과 저장 요청의 단일 잠금 범위 연결 | PASS |
| C08 | 픽업 순서·결과·좌표식·Motion/IO/인터락 무변경 확인 | PASS |
| C09 | 정책·회귀 오프라인 시험 | PASS — 185개 |
| C10 | 실제 프로젝트 격리 `/t:Build` | PASS — 오류 0, 기존 경고 41 |
| C11 | BOM/CRLF/EOF, `git diff --check`, 최종 diff 확인 | PASS |
| C12 | 실장비 및 운영 배포 | NOT RUN |

현재 이미 Review가 재진입하며 승인/순서 필드가 초기화된 운전 중 상태는 자동 추정 복구하지 않는다. 새 코드 적용 전 생성된 해당 Wafer는 현장 절차에 따라 재확인 또는 재매핑해야 한다.

## 검증 결과

- 정책 32개, Config 12개, Material geometry 10개, 진행 보존 30개, 검증점 상태 16개, 비생산 context 15개, CONFIRM/진단 44개, 로그 정책 26개를 최신 격리 assembly로 실행했다.
- 새 정책 시험은 일반 승인 비교가 좌표 변경을 계속 거부하는지, 4개 처리 완료 뒤 허용된 남은 좌표 전파만 재기준화되는지, 그와 동시에 다른 context가 바뀌면 전부 거부되는지를 확인한다.
- Build는 `D:/Source/CDT-320_New/_build_check_handler/out`만 출력 대상으로 사용했다. Clean/Rebuild와 운영 폴더 배포는 하지 않았다.
- 새 Audit event는 `IN-REVIEW-APPROVAL-VISION-OFFSET-REBASE`이며 안전 검증 실패는 `IN-REVIEW-APPROVAL-VISION-OFFSET-REBASE-REJECT`로 구분한다.
