# Stage 5 PLAN — GUI Cycle 자동화 (Stage 4 E2 후속)

## 목표
Handler exe 자동 기동 후 UI Automation 으로 Init → CycleRun → Stop 클릭 실행 → JobQueue/Lot/DieMap 결과 자동 검증.

## 접근
1. Windows UIA (`UIAutomationClient`, `UIAutomationTypes`) 참조
2. 별도 도구 프로젝트 `Tools/GuiAutomation/` 또는 `tools/gui_cycle_test.exe` 신규 콘솔 앱
3. 또는 PowerShell 스크립트 (`Add-Type` 으로 UIA 사용) — 더 가벼움

**선택:** PowerShell + UIA — 별도 프로젝트 추가 없이 가능.

## 작업

### 5.1 PowerShell GUI Cycle 자동화
- `tools/gui_cycle_automation.ps1` — UIA 로 Form 의 버튼 찾고 클릭
  - Initialize 버튼 클릭 → 5초 대기
  - CycleRun 버튼 클릭 → 다이 N개 처리 대기
  - Stop 버튼 클릭

### 5.2 결과 검증
- `Log/Lots/yyyyMMdd_LOT-*.json` 생성 확인 (Lot.cs 자동 저장)
- ProcessedDies, GoodCount, NgCount 합리적 값
- `Log/DieMap/yyyy-MM-dd/*.csv` 또는 `*.json` 존재 확인

### 5.3 Perl 통합
- `tools/runtime_cycle_test.pl` 확장 — `gui_cycle_automation.ps1` 호출 + 결과 파일 grep

## NON-GOAL
- 실 클릭 좌표 시뮬레이션 (UIA 가 더 안정적)
- Handler 코드 변경 (현재 GUI 가 충분)
