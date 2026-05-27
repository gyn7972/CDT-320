# QMC CDT-320 — 다이 트랜스퍼 시스템

> **CDT-320 듀얼 픽커 다이 본더 핸들러** + Vision PC + 3D 시뮬레이터의 통합 솔루션.

## 구성

```
QMC.CDT-320/
├─ QMC.CDT-320/         # 메인 핸들러 (WinForms, .NET 4.7.2)
├─ QMC.Vision/          # 비전 PC (별도 프로세스 — TCP 5100/5101/5103)
├─ QMC.Common/          # 공용 라이브러리 (Motion/IO 추상)
├─ CDT320Simulator/     # 3D 시뮬레이터 (WPF + HelixToolkit)
├─ tools/               # Perl 자동 검증 스크립트 + PowerShell 자동화
└─ *.md                 # 단계별 PLAN/CHECKLIST/REPORT
```

## 핵심 기능

### 메인 핸들러 (`QMC.CDT-320/`)
- **CDT-300 스타일 1920×1080 UI** (6 탭 × 50+ 페이지 + 17 다이얼로그)
- **5단계 사용자 권한** (None / Operator / Engineer / Maintenance / Admin)
- **다국어 지원** (ko / en / 향후 zh-CN)
- **AJINEXTEK AXL** 실보드 (P/Invoke + JSON 설정) + Sim 모드
- **시뮬레이터 통신** (B-plan: master/viewer with HELLO message)
- **비전 통신** (Wafer/Inspection/Bin TCP — `MODULE|CMD|args` 라인 프로토콜)
- **310 이식 기능**:
  - **Materials**: Die / DieTapeFrame / MaterialStorage / MaterialSpecs
  - **Bin**: BinCodeMap (NG → bin → color)
  - **DieMap**: 격자 생성기 + 시각화 (DieMapView)
  - **Job**: JobOrder + JobQueue (Pending + History)
  - **Interlock**: 15 종 (5 standard + 5 extended + 5 stage8)
  - **Vision Alignment**: 3-point AlignmentSolver + CoordinateMap
  - **Pick Retry**: DoOneDieAsync 내부 3회 재시도
  - **Recipe Subset** (Die/Frame/Load/Unload/Module)
  - **SECS/GEM**: SecsHost (line + HSMS dual mode), 13 표준 메시지
  - **Lot 추적**: LotStorage + ActiveLotPage
  - **Remote Viewer**: TCP 화면 캡처 송신 + 자체 미리보기
  - **Sensors**: IonizerSensor

### Vision (`QMC.Vision/`)
- **3 모듈** — Wafer (포트 5100) / BottomInspection (5101) / Bin (5103)
- **카메라 추상** (`ICamera`) — Hikvision GigE / Sim
- **백엔드 추상** (`IVisionBackend`) — Cognex / OpenCV / Sim 자동 fallback
- **Cognex VisionPro 25.2.0** 동적 로드 (Reflection)
  - PMAlign, Blob, Caliper, Histogram, ColorMatch, ImageProcessing
- **5종 Inspection Parameters** + JSON 영속화
- **MaterialTracker** 다이별 검사 결과 누적
- **DataLogSaver** (30 칼럼 CSV) + ImageLogSaver
- **UI**: 5 탭 — Operation / Configuration / Maintenance / Recipe / DataLog
  - **FinderPage** — GRAB / LOAD / SAVE / TRAIN / MATCH + ROI 마우스 드래그
  - **InspectorPage** — 검사기 실행 + PASS/FAIL + 결과 키-값 테이블
  - **SpcChartPage** — X-bar 차트 + LSL/USL + Avg/Stdev
  - **ParameterEditorHost** — 5 tool 콤보 + 편집기
  - **ZoomDialog** — 휠 줌 + 드래그 팬

### Simulator (`CDT320Simulator/`)
- **WPF + HelixToolkit 3D**
- **37 축 시뮬레이션** (CDT-320 IO map 기반)
- **TCP 서버** (포트 7001) — JSON 명령
- **Master/Viewer 모드** — HELLO 메시지로 자동 결정

## 빌드

요구사항:
- **Visual Studio 2022** (Community/Pro/Enterprise)
- **.NET Framework 4.7.2 Developer Pack**
- **Cognex VisionPro 25.2.0** (선택 — 미설치 시 OpenCV/Sim fallback)

```powershell
$MSB = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
& $MSB "QMC.Common\QMC.Common.csproj"   /t:Build /p:Configuration=Debug
& $MSB "QMC.CDT-320\QMC.CDT-320.csproj" /t:Build /p:Configuration=Debug
& $MSB "QMC.Vision\QMC.Vision.csproj"   /t:Build /p:Configuration=Debug
```

## 실행

### 1) Vision 먼저 실행
`QMC.Vision\bin\Debug\QMC.Vision.exe` — TCP 5100/5101/5103 listen 시작.

### 2) Simulator 실행 (선택)
`CDT320Simulator\bin\Debug\CDT320Simulator.exe` — UI 의 [TCP START] 버튼 클릭하여 7001 listen.

### 3) Handler 실행
`QMC.CDT-320\bin\Debug\QMC.CDT-320.exe` — Vision 자동 연결 + Sim 자동 연결 (설정에 따라).

## 자동 검증

`tools/` 디렉토리:
- **`verify_all.pl` — 모든 Stage 통합 회귀 (현재 117/118 PASS, 0 FAIL)**
- `verify_comm.pl` — Vision↔Handler↔Sim 종단 통신 검증 (30 항목)
- `verify_vision_features.pl` — Vision 기능 검증 (21 항목)
- `verify_handler_features.pl` — Handler 310 이식 검증 (25 항목)
- `verify_stage2.pl` ~ `verify_stage25.pl` — 단계별 추가 검증
- `verify_cognex_runtime.pl` — Cognex 동글 활성 후 실 검증
- `runtime_cycle_test.pl` — 환경 + 기동 안정성 + Cycle 결과 (`RUN_GUI_CYCLE=1`)
- `gui_cycle_automation.ps1` — UI Automation 으로 사이클 실행 (Stage 5)
- `remote_viewer_client.ps1` — Handler Remote Viewer 화면 받기 (Stage 17)
- `audit_threading.pl` / `audit_memory.pl` — 정적 audit

자동 사이클 실 동작 (Stage 24):
```bash
QMC.CDT-320.exe --auto-cycle 10  # Init → CycleRun(10) → 종료. Lot JSON 자동 저장.
```

```bash
perl tools/verify_all.pl                    # 117/118 PASS
perl tools/verify_handler_features.pl       # 25/25
perl tools/verify_vision_features.pl        # 21/21 (Vision exe 실행 시)
RUN_GUI_CYCLE=1 perl tools/runtime_cycle_test.pl  # GUI 자동화 + Cycle 검증
```

## 라이선스

Proprietary — © QMC

## 개발 단계 문서

- `STAGE1_CHECKLIST.md` — UI 연결 + Recipe Subset
- `STAGE2_CHECKLIST.md` — SPC + Editors + Zoom + Cognex 진단
- `STAGE3_CHECKLIST.md` — Lot + Reject + 5 Interlock + HSMS + Remote + Ionizer
- `STAGE4_CHECKLIST.md` — RemoteViewerDialog + ActiveLotPage + SecsHost UseHsms + Cognex test
- `STAGE5_CHECKLIST.md` — GUI Cycle 자동화 (UIA)
- `STAGE6_PLAN.md` — Cognex Caliper / Histogram / ColorMatch
- `STAGE7~10` — overnight 추가 작업
- `OVERNIGHT_REPORT.md` — 자율 작업 결과 종합 (최종)

## 아키텍처 + 사용자 가이드

- `ARCHITECTURE.md` — 컴포넌트 + 통신 다이어그램
- `USER_GUIDE.md` — 운영 매뉴얼 (Init → Cycle → 결과 확인)
