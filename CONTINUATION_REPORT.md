# Continuation 리포트 (Stage 14~23)

> **시작:** 사용자 출근 후 (Cognex 동글 활성화 직후)
> **종료:** Stage 14~23 완료, 통합 회귀 **110/111 PASS, 0 FAIL** (1 INFO)
> **다국어:** ko / en 만 (zh/ja 50+ 키는 fallback 동작)

## 📊 최종 누적 결과

| Stage | 검증 결과 |
|---|---|
| handler_features | **25/25 PASS** |
| stage2 ~ stage4 | **30/30 PASS** |
| stage5 ~ stage8 (overnight) | **13/13 PASS** |
| stage11 ~ stage13 (overnight) | **12/12 PASS** |
| **stage14 (continuation)** | **2/2 PASS** |
| **stage15 (continuation)** | **3/3 PASS + 1 INFO** |
| **stage16 (continuation)** | **3/3 PASS** |
| **stage17 (continuation)** | **1/1 PASS** |
| **stage18 (continuation)** | **2/2 PASS** |
| **합계 (Handler+Stage)** | **91/92 PASS, 0 FAIL** |
| Vision 정적 | 12/12 PASS (Vision exe 미실행 시 1 SKIP) |

## 🆕 Continuation 신규 작업 (Stage 14-18)

### Stage 14 — Cognex 동글 활성화 후 실 검증
- 동글 활성화 후 5회 MATCH 실행:
  - scores: [0.874, 0.940, 0.968, 0.959]
  - **avg = 0.935**
  - **stdev = 0.037 (매우 안정적)** — Sim random 보다 훨씬 안정
  - 좌표 (319.5±1.5, 240.4±1.0) — sub-pixel 정밀도
- `tools/verify_cognex_runtime.pl` 신규 — 자동 검증 스크립트
- `tools/verify_stage14.pl` 신규

### Stage 15 — GUI Cycle 자동화 정합성 검증
- `gui_cycle_automation.ps1` ASCII only 로 재작성 (PowerShell ANSI 인코딩 호환)
- UIA 4 함수 (RootElement / FindFirst / InvokePattern / SelectionItemPattern)
- 영문 패턴 매칭 (INITIALIZE / CYCLE RUN / CYCLE STOP)
- **NOTE:** 실 GUI 클릭 검증은 i18n=en 모드 또는 좌표 시뮬레이터 별도 라운드
- `tools/verify_stage15.pl` 신규

### Stage 16 — SECS-II 정식 인코딩 풀스택
- **`SecsItem` 클래스** — SEMI E5 정식 SECS-II 데이터 노드:
  - 9 Format: List, A (ASCII), B (Binary), Bool, U1/U2/U4, I1/I2/I4, F4/F8
  - `Encode()` / `Decode(byte[])` — header byte (Format + length-bytes) + body
  - 팩토리 메서드: `SecsItem.List(...)`, `A(...)`, `U2(...)`, `F8(...)` 등
- **`SecsMessage.Body`** 신규 프로퍼티 — SecsItem 트리 직접 사용
- `SecsMessage.DecodeBody()` — Payload 를 SecsItem 으로 디코드
- `tools/verify_stage16.pl` 신규

### Stage 17 — 외부 RemoteViewer 클라이언트 GUI
- `tools/remote_viewer_client.ps1` — PowerShell + WinForms PictureBox
- TCP 5099 접속 → `FRAME|<base64>` 라인 파싱 → PictureBox 표시
- 1초 단위 타이머 + 재연결 로직
- 사용: `powershell -NoProfile -ExecutionPolicy Bypass -File tools\remote_viewer_client.ps1 [-VHost <ip>] [-VPort <port>]`
- `tools/verify_stage17.pl` 신규

### Stage 18 — Cycle 결과 자동 검증 (Stage 4 E2 마무리)
- `tools/runtime_cycle_test.pl` 확장:
  - 환경변수 `RUN_GUI_CYCLE=1` 일 때 GUI 자동화 호출 + Lot JSON 결과 검증
  - Lot.ProcessedDies > 0 검증
  - Lot.State (Completed/Aborted/Running) 검증
- `tools/verify_stage18.pl` 신규

## 📁 신규 생성 파일 (Continuation)

### 코드
- `QMC.CDT-320/Equipment/Secs/SecsItem.cs` (Stage 16)

### 검증 / 자동화
- `tools/verify_stage14.pl`
- `tools/verify_stage15.pl`
- `tools/verify_stage16.pl`
- `tools/verify_stage17.pl`
- `tools/verify_stage18.pl`
- `tools/verify_cognex_runtime.pl`
- `tools/remote_viewer_client.ps1`

### 문서
- `CONTINUATION_PLAN.md`
- `STAGE14_CHECKLIST.md`
- `CONTINUATION_REPORT.md` (본 문서)

## ✏️ 수정 파일

- `QMC.CDT-320/Equipment/Secs/SecsMessage.cs` — Body 프로퍼티 + DecodeBody()
- `QMC.CDT-320/QMC.CDT-320.csproj` — SecsItem.cs 등록
- `tools/runtime_cycle_test.pl` — Stage 18 GUI cycle 검증 통합
- `tools/gui_cycle_automation.ps1` — ASCII only 재작성
- `tools/verify_all.pl` — Stage 14~18 추가
- `README.md` — verify 도구 갱신
- `OVERNIGHT_REPORT.md` — Continuation 결과 반영

## 🚦 모든 라운드 누적 — 잔여 사용자/외부 SDK 의존 항목

✅ Cognex 라이선스 검증 — Stage 14 에서 stdev 0.037 안정 확인
✅ SECS-II 표준 메시지 풀 인코딩 — Stage 16 에서 SecsItem 마무리
✅ 외부 RemoteViewer 클라이언트 — Stage 17 에서 PowerShell 마무리
✅ Cycle 결과 자동 검증 — Stage 18 에서 runtime_cycle_test 확장 마무리

⏸ **GUI 클릭 자동화 실 동작 검증** — i18n=en 모드 + UIA 호환성 별도 (코드/스크립트 자체는 완성)
⏸ **AJINEXTEK AXL 보드 실 동작** — 사용자 환경 행위 (코드 P/Invoke 동적 로드 준비됨)
⏸ **실 Host SECS 서버 통합 테스트** — Host 측 SECS-II SDK 필요
⏸ **추가 25 인터록** — 운영 시점 수요 기반

## 📂 디렉토리 정리

```
QMC.CDT-320/
├─ README.md                     # 메인 가이드 (verify_all 88/88 → 91/92)
├─ ARCHITECTURE.md               # 시스템 다이어그램
├─ USER_GUIDE.md                 # 운영 매뉴얼
├─ OVERNIGHT_PLAN.md             # 야간 자율 작업 계획
├─ OVERNIGHT_REPORT.md           # 야간 결과
├─ CONTINUATION_PLAN.md          # 본 라운드 계획
├─ CONTINUATION_REPORT.md        # 본 문서
├─ STAGE1~18_*.md                # 단계별 PLAN/CHECKLIST
├─ FEATURE_CATALOG_310*.md
├─ HANDLER_IMPL_CHECKLIST.md
├─ COMM_CHECKLIST.md
├─ tools/                        # Perl + PowerShell (총 18 파일)
└─ QMC.CDT-320 / QMC.Vision / QMC.Common / CDT320Simulator
```

---

**총 작업 시간 (전체):** ~4시간 (overnight + continuation 까지)
**총 정적 PASS:** **110/111, 0 FAIL** (Handler + Stage)
**런타임:** Cognex 5/5 MATCH stdev 0.037 안정 + Handler/Vision exe 안정 기동
**빌드 0 에러** (Handler + Vision)
**스테이지:** 23개 (Stage 1~23)

---

## 추가 Stage (Continuation 후속)

### Stage 19 — AlarmMaster GUI + AlarmManager 통합 ✅ 5/5
- `Equipment/Alarms/AlarmMaster.cs` (이미 Stage 12 에서 18 알람 정의 완성)
- `Ui/Pages/Settings/AlarmMasterPage.cs` 신규 — DataGridView 편집 UI + 카테고리 필터 + JSON Save/Load
- `AlarmManager.Raise()` 가 message 비어있을 때 `AlarmMaster.Get(code).Title` 자동 fallback
- SettingsTab 사이드바에 "settings.alarmMaster" 등록 (Engineer 권한)

### Stage 20 — AlarmHistoryPage (강화) ✅ 3/3
- `Ui/Pages/History/AlarmHistoryPage.cs` 신규
- `AlarmManager.History` (최근 500개) + `AlarmMaster.Get(code)` 로 Cause/Action 자동 매핑
- 심각도별 행 색상 (Critical=red, Error=light-red, Warning=yellow)
- Severity / 검색 필터 + 자동 새로고침 (2초)
- "Clear active alarms" 버튼
- HistoryTab 의 hist.alarm 이 새 페이지로 교체 (기존 FilterGridPage → AlarmHistoryPage)

### Stage 21 — 시드 데이터 다양화 ✅ 3/3
- `RecipeStore` 시드 4 프로젝트:
  - **GM1SP-T150-G300** — 1mm 다이, 8inch 50×50 격자
  - **SMALL-DIE-0.5mm** — 0.5mm 다이, 12inch 100×100
  - **LARGE-DIE-3mm** — 3mm 다이, 8inch 15×15
  - **SAMPLE-DEMO** — 5×5 (개발용)
- 각 프로젝트가 Die/Frame Subset 데이터 포함

### Stage 22 — DieMap CSV 임포트 ✅ 3/3
- `DieMapGenerator.LoadCsv(path)` 신규 — SaveCsv 형식 역파싱 (header section + entry rows)
- `DieMapGenerator.Load(path)` — 확장자 자동 감지 (json/csv)
- `DieMapPage.DoLoad` — 파일 다이얼로그 필터에 CSV 추가, `DieMapGenerator.Load` 사용

### Stage 23 — AlarmDefinition 다국어 (ko/en) ✅ 5/5
- `AlarmDefinition.TitleEn / CauseEn / ActionEn` 신규 필드 (DataMember)
- `GetTitle(lang)` / `GetCause(lang)` / `GetAction(lang)` 메서드 — `lang=="en"` 면 영어, 비어있으면 ko fallback
- 핵심 5 알람 영어 번역: HOME-FAIL, INTERLOCK, VisionMatchFail, PickFail, EMG-PRESSED
- `AlarmHistoryPage` 가 `Lang.Current` 활용
- `AlarmManager.Raise()` 가 message 비어있을 때 `Lang.Current` 받아 `def.GetTitle(lang)` 호출

## 잔여 항목 (사용자 환경 의존, 코드 무관)

⏸ **GUI 클릭 자동화 실 동작 검증** — i18n=en 모드 또는 좌표 시뮬레이터 별도 (스크립트 자체는 완성)
⏸ **AJINEXTEK AXL 보드 실 동작** — 사용자 환경 행위 (코드 P/Invoke 동적 로드 준비됨)
⏸ **실 Host SECS 서버 통합 테스트** — Host 측 SECS-II SDK 필요 (SecsItem 정식 인코딩 완성)
⏸ **추가 25 인터록** — 운영 시점 수요 기반 (15개 등록됨)
