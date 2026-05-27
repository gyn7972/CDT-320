# Continuation 전체 계획 (Stage 14~18)

> **시작:** Cognex 동글 활성화 직후
> **목표:** 모든 미완료 항목 마무리. 다국어는 ko/en 만.
> **프로세스:** Stage 마다 PLAN → CHECKLIST → 구현 → 검증 → G1 정합성 → 차이 시 업데이트 → G3 정합성 → 재검증.

## Stage 14 — Cognex 동글 활성화 후 실 검증
- ConfigurationPage 의 "Run Cognex test" 버튼 → 실 CogPMAlignTool Train+Match 동작 확인
- `verify_cognex_runtime.pl` 신규 — Vision exe 자동 기동 + MATCH 결과의 random 변동 확인 (Sim 보다 안정적인 패턴 매칭)

## Stage 15 — GUI Cycle 자동화 실 동작 검증
- Stage 5 의 `gui_cycle_automation.ps1` 실 환경 검증
- Handler 자동 기동 → 초기화 → 사이클 실행 → Lot JSON 결과 확인
- 결과 파일 검증 → JobQueue.History.Count, Lot.YieldPercent, DieMap 파일 생성

## Stage 16 — SECS-II 표준 인코딩 풀스택
- 현재 SecsMessage 는 `TextPayload` 만. SECS-II 정식은 List/ASCII/Binary/Int/Float 노드 트리.
- **신규:**
  - `SecsItem` 클래스 — 노드 (Type + Value + Children)
  - `SecsItemFormat` enum — A (ASCII), L (List), B (Binary), I/U (Int), F (Float), Bool, JIS-8
  - `SecsItem.ToBytes()` / `Parse()` — 정식 SEMI E5 형식
- `SecsMessage.Body` 새 프로퍼티 (SecsItem 트리)

## Stage 17 — 외부 RemoteViewer 클라이언트
- Handler 의 RemoteViewer 가 송신하는 base64 화면을 받아 표시하는 별도 viewer
- **선택:** PowerShell 스크립트 (UIA 없이 단순 PictureBox) 또는 별도 콘솔 앱
- 본 라운드: PowerShell 스크립트 (배포 단순)

## Stage 18 — Cycle 실행 후 결과 자동 검증 (Stage 4 E2 마무리)
- Stage 15 의 GUI 자동화 결과 위에서:
  - Lot JSON 의 ProcessedDies, GoodCount, NgCount 검증
  - DieMap CSV 생성 확인
  - Event log CYCLE 시작/종료 행 grep
  - JobQueue 요약 (가능한 경우 외부 노출)
- `tools/runtime_cycle_test.pl` 확장 — gui_cycle_automation 호출 + 결과 검증

## 비범위
- 다국어 추가 번역 (ko/en 만)
- 나머지 16 인터록 (운영 수요 기반 추가)

## 자체 정지 조건
- Stage 14~18 모두 완료
- 빌드 실패 후 fix 시도 3회 실패 → 그 항목 skip + 다음 진행
