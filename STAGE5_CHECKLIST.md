# Stage 5 체크리스트 — ✅ 완료

## A. UIA 자동화 스크립트
- [x] **A1.** `tools/gui_cycle_automation.ps1` 신규 — PowerShell + UIAutomationClient
  - Handler exe 자동 기동 (없을 때)
  - 메인 윈도우 핸들 획득 (Title="CDT-320")
  - WORK 탭 활성화 (이미 기본)
  - "Initialize" / "초기화" 버튼 찾기 → 클릭
  - "CycleRun" / "사이클 실행" 버튼 찾기 → 클릭
  - 다이 처리 대기 (사이클 길이 의존, 약 30초 ~ 2분)
  - "CycleStop" / "사이클 정지" 버튼 찾기 → 클릭
  - Log file 결과 path 출력

## B. 결과 검증
- [x] **B1.** `tools/runtime_cycle_test.pl` 확장
  - gui_cycle_automation.ps1 invoke (옵션)
  - `Log/Lots/yyyyMMdd_LOT-*.json` 1개 이상 존재
  - `Log/DieMap/yyyy-MM-dd/*` 1개 이상 (옵션)
  - Event log CSV 에 CYCLE 시작/종료 행

## C. 빌드 + 검증
- [x] **C1.** PowerShell 스크립트 syntax 검증 (`-NoExecute -Syntax`)
- [x] **C2.** verify_stage5.pl 신규 작성
- [x] **C3.** 회귀 — 기존 verify (Stage 1~4 + handler + vision) 모두 PASS

## D. 정합성
- [x] **D1.** PLAN ↔ CHECKLIST 1차
- [x] **D2.** 차이 시 업데이트
- [x] **D3.** CHECKLIST ↔ 구현 2차
- [x] **D4.** 차이 시 재구현/재검증

진행 상태: ✅ COMPLETED (verify 3/3 PASS)
