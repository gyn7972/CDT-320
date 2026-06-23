# CDT-320 작업 MAIN — 작업 시간/UPH 표시 요구 정리

## Source
- 작업 MAIN 화면 "작업 시간" 패널 스크린샷 (사용자 제공, 2026-06-23)
- 구두 요구: "오토 시퀀스가 돌면서 DATA가 뿌려져야 한다. 특히 **사이클 타임**과 **UPH**가 정확하게 계산되어 표시되어야 하고, UI가 느려지지 않아야 한다."

## Scope summary
오토 사이클(`MachineController` 자동 운전) 진행 중, 작업 MAIN 화면(`WorkMainPage`)의 "작업 시간" 패널에
생산 통계를 실시간 표시한다. 핵심은 사이클 타임/UPH의 **정확한 계산**과 UI 스레드 **무지연(non-blocking) 갱신**.

## Requirements
| ID | Category | Requirement | Source | Priority |
|----|----------|-------------|--------|----------|
| R-001 | Business Logic | 사이클 타임을 실제 사이클 경계(1 사이클 = pickers개 다이) 기준으로 정확히 측정 | 구두 | Must |
| R-002 | Business Logic | UPH = 양품/시간 기준으로 사이클 타임과 일관되게 계산 | 구두 | Must |
| R-003 | Data | Total chip / Good / Bin 등 생산 카운트가 오토 사이클 중 실시간 반영 | 화면 | Must |
| R-004 | Business Logic | 부하/가동/연속 가동/통상 정지/이상 정지/이상 복귀 시간을 상태기반으로 누적 | 화면 | Must |
| R-005 | Business Logic | 이상 정지 횟수 / MTBF / MTTR 누적 | 화면 | Should |
| R-006 | Business Logic | 가동률(%) = 가동시간 / 부하시간 기준 (현재 yield로 잘못 계산됨) | 화면 | Must |
| R-007 | UI | 위 모든 값을 UI 스레드 블로킹 없이 250~500ms 주기로 표시 | 구두 | Must |
| R-008 | Data | 작업중인 LOT ID 표시 | 화면 | Must |

## Open questions (Stage 4에서 확정)
- 사이클 타임 정의: "직전 1 사이클 소요(instant)" vs "이동 평균(rolling N)" vs "누적 평균" — 어느 것을 표시?
- UPH 정의: 순수 가동시간 기준 vs 부하시간(정지 포함) 기준?
- 가동률 정의: 가동시간/부하시간(설비종합효율 통상 정의) 확정 필요
- 부하 시간 시작 기준: 사이클 Start 시점 vs 설비 Ready 시점?
