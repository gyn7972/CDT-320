# Stage 4 작업 계획 (PLAN)

> **목표:** 이전 라운드(Stage 1/2/3) 에서 "다음 라운드로 미룬" 후순위 항목 모두 마무리.
> **새 프로세스:** PLAN → CHECKLIST → 구현 → 검증 → **PLAN ↔ CHECKLIST 정합성 확인** → 차이 시 업데이트 → 재검증.

## 1. 미완료 항목 (이전 단계 솔직 표시 분)

### 1.1 Stage 3 A2 — RemoteViewerDialog
- 위치: `QMC.Vision/Ui/Dialogs/` 또는 `QMC.CDT-320/Ui/Dialogs/` (둘 다 검토 → Handler 측이 더 적합 — Form1 캡처 송신을 받아 표시하는 별도 viewer 앱)
- **결정:** 별도 앱 만들지 말고 **Handler 자체 메뉴**(Settings 탭 → 모달 다이얼로그)에서 RemoteViewer 송신 ON/OFF + 포트 설정 + 자체 미리보기.
- 이유: 외부 뷰어 클라이언트는 별도 프로세스가 필요해서 본 라운드 범위 초과.

### 1.2 Stage 3 D4 — WorkInfoTab "Active Lot" 패널
- 위치: `QMC.CDT-320/Ui/Tabs/WorkInfoTab.cs` 또는 `Ui/Pages/WorkInfo/`
- 표시: LotID / Recipe / State / Started / Processed × Total / Good × NG / Yield% / Bin 분포
- 새로 고침: LotStorage.ActiveLotChanged 이벤트 구독 + 1초 타이머

### 1.3 Stage 3 F3 — SecsHost ↔ HsmsConnection 통합
- `SecsHost` 에 `UseHsms` 플래그 + 옵션 사용
- `UseHsms = true` 면 `HsmsConnection` 으로 SECS 메시지 송수신
- `UseHsms = false` 면 기존 line-protocol 시뮬 모드 (호환성 유지)

## 2. 추가 마무리 항목 (이전 단계 미진행 잔여)

### 2.1 Tier 3 — Cognex CogPMAlignTool 실호출 진단 (선택)
- ConfigurationPage 에 "Run Cognex MATCH/INSPECT test" 버튼 추가
- 클릭 시 이미지 1장 grab → CogPMAlignTool Train + Match 시도 → 결과 표시
- 실패 시 fallback 사실 표시

### 2.2 런타임 사이클 검증 자동화 (Stage 1 L3 부분)
- `tools/runtime_cycle_test.pl` 신규
- Handler exe 자동 기동 (없으면) → 명령 라인으로 Init → CycleRun(N) → 종료 후 이벤트 로그 / Lot JSON 결과 검증
- 검증 항목:
  - JobQueue.History.Count == N 또는 Lot JSON.ProcessedDies == N
  - Lot.YieldPercent 합리적 범위
  - DieMap 결과 파일 생성 여부
  - Bin 분포 누적

## 3. 작업 단계 (실행 순서)

1. STAGE4_CHECKLIST.md 작성 (이 PLAN 기반)
2. 코드 구현 (A: RemoteViewerDialog, B: ActiveLotPanel, C: SecsHost-HSMS, D: Cognex 진단 버튼)
3. 빌드 0 에러 확인
4. tools/verify_stage4.pl 작성 + 실행
5. **PLAN ↔ CHECKLIST 정합성 1차 확인** (모든 PLAN 항목이 체크리스트에 있는지)
6. 차이 시 체크리스트 업데이트
7. **PLAN ↔ 구현 정합성 2차 확인** (체크리스트 모든 항목이 코드에 반영됐는지)
8. Vision/Handler 런타임 기동 안정성 + 기존 회귀 (44/44 PASS 유지)
9. 최종 보고

## 4. 비범위 (NON-GOAL)

- 외부 RemoteViewer **별도 프로세스 GUI 앱** (별도 솔루션 필요)
- Cognex **라이선스 동글 활성화** (사용자 행위)
- SECS-II **표준 메시지 풀스택** (S1F1~S6F11 만 골격, 전체 Stream 인코딩은 SEMI SDK 필요)
- 31 추가 인터록 (10개로 충분, 운영 시점 추가)
- **(G1 정합성 결과 추가)** PLAN 2.2 의 cycle 실행 후 결과 자동 검증 — GUI 클릭 자동화 도구가 필요해 본 라운드 범위 외. 환경 + 기동 안정성 검증으로 단순화.
