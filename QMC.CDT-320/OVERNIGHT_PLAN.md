# Overnight 작업 전체 계획 (Stage 5~11)

> **시작:** 2026-04-27 저녁
> **목표:** 사용자 퇴근 후 익일 아침까지 미완료 항목 + 추가 개선 자율 진행.
> **프로세스:** 각 Stage 마다 PLAN → CHECKLIST → 구현 → 검증 → G1 정합성 1차 → 차이 시 업데이트 → G3 정합성 2차 → 재검증.

## Stage 5 — GUI Cycle 자동화 (Stage 4 E2 후속)

- 본 라운드 미완료된 cycle 실행 후 결과 자동 검증.
- **접근:** `System.Windows.Automation` (UIA — UI Automation API) 사용.
  - 리눅스/맥용 PIP-PA 시뮬은 X — Windows Native UIA 만.
  - 추후 .NET 4.7.2 에서 `UIAutomationClient.dll` 참조.
- **결과물:**
  - `tools/gui_cycle_test.cs` — UIA 기반 자동 클릭 (Init → CycleRun(10) → Stop)
  - `tools/runtime_cycle_test.pl` 확장 — gui_cycle_test 결과 검증 + JobQueue/Lot/DieMap 결과 grep

## Stage 6 — 추가 Cognex Backend Tool

- 현재 Cognex backend 는 PMAlign + Blob 만.
- **추가:** Caliper (edge measurement), Histogram (밝기 통계), ColorMatch (컬러 검사).
- **결과물:**
  - `QMC.Vision/Backends/Cognex/CognexCaliper.cs` — IEdgeFinder
  - `QMC.Vision/Backends/Cognex/CognexHistogram.cs` — IInspector 추가 메서드
  - `QMC.Vision/Backends/Cognex/CognexColorMatch.cs` — color trained match
  - `QMC.Vision/Core/IEdgeFinder.cs` — 추상 인터페이스

## Stage 7 — 추가 SECS 표준 메시지

- 현재 S1F1 (Are you there) + S1F2 (online data) + S1F13/14 (Establish Comm) + S2F41/42 (Host Cmd) + S6F11 (Event Report).
- **추가:** S5F1 (Alarm Send), S5F3 (Alarm En/Disable), S6F12 (Event Ack), S7F3 (Process Program Send).
- **결과물:**
  - `QMC.CDT-320/Equipment/Secs/SecsMessage.cs` 확장 — 신규 헬퍼 4개
  - `SecsHost` 가 AlarmManager.AlarmRaised 이벤트 구독 → S5F1 자동 송신

## Stage 8 — 추가 5 Interlock (총 15)

- 310 의 40개 중 우리 10개 → 15개 확장.
- **추가 5개:**
  - `LifterVsExpanderInterlock`
  - `BarcodeVsLoaderInterlock`
  - `SubPortVsPickerInterlock`
  - `ColletCleanerVsPickerInterlock`
  - `EmgStopVsAllInterlock` — 비상 정지 시 모든 모션 차단
- **결과물:** `Equipment/Interlocks/ExtendedInterlocks2.cs`

## Stage 9 — 문서화

- README.md (메인) — 빌드/실행/주요 기능 + 디렉토리 구조
- ARCHITECTURE.md — 컴포넌트 다이어그램(텍스트) + 통신 레이어
- USER_GUIDE.md — 운영 매뉴얼 (Init → Cycle → 결과 확인 시나리오)

## Stage 10 — Threading / Memory Audit

- 빌드 산출물 정적 분석 + 실 코드 grep
- ConcurrentDictionary 사용 곳, lock 락 일관성, IDisposable 대상 누락
- `tools/audit_threading.pl` — 정적 grep 기반 audit
- `tools/audit_memory.pl` — `using` 패턴 + Dispose 호출 매칭

## Stage 11 — i18n 중국어 추가

- `Lang.cs` 의 모든 키에 `zh-CN` 추가
- 한국어/영어 매핑 → 중국어 번역 (간이 — 산업용 표준 용어)
- LanguageOption combo 에 추가

## 진행 순서

1. Stage 5 → 7 → 6 (의존성 적은 것부터)
2. Stage 8 (Interlock, 자체완결)
3. Stage 9 (문서화 — 다른 작업 후 최신 상태로)
4. Stage 10 (audit)
5. Stage 11 (i18n)
6. 최종 보고서 (`OVERNIGHT_REPORT.md`)

## 비범위 (NON-GOAL)

- **실 카메라 하드웨어** (Hikvision MVS GigE 실제 연결 시험)
- **실 SECS Host 와 통신** (라이선스된 SECS-II SDK 필요)
- **AJINEXTEK AXL 보드 실 동작** (사용자 행위)
- **외부 RemoteViewer 클라이언트 GUI 앱** (별도 솔루션)
- **PR/Git push** (사용자 의사 필요)

## 자체 정지 조건

- 모든 Stage 완료 시
- 빌드 실패 후 fix 시도 3회 실패 시 (그 항목 skip + 다음 진행)
- 토큰 한도 임박 시 진행 중단 + 부분 보고서 작성
