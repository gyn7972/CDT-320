# QMC CDT-320 — 다이 트랜스퍼 시스템

> CDT-320 듀얼 픽커 다이 본더 Handler 소스 저장소. Vision PC는 외부 시스템으로 TCP 인터페이스만 유지하며, Vision 실행 프로젝트와 검사 구현은 이 저장소에 포함하지 않는다.

## 구성

```
QMC.CDT-320/
├─ QMC.CDT-320/         # 메인 Handler (WinForms, .NET Framework 4.7.2)
├─ QMC.Common/          # Handler 공용 라이브러리
├─ tools/               # Handler 진단·자동화 도구
├─ docs/                # 설계·인터페이스 문서
└─ QMC.CDT-320.sln      # Handler + QMC.Common
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

### 외부 Vision PC 인터페이스
- 명령 채널: Wafer 5100, BottomInspection 5101, Bin 5103, Main 5104, FrontSide 5105, RearSide 5106
- 영상 채널: 5200, 5201, 5203, 5205, 5206
- 라인 프로토콜: `MODULE|CMD|args...`
- Handler의 연결·명령·결과·Viewer·시퀀스 코드는 `QMC.CDT-320`에 유지한다.
- 외부 Vision 프로그램의 소스·SDK·검사 엔진은 별도 관리한다.

## 빌드

요구사항:
- **Visual Studio 2022** (Community/Pro/Enterprise)
- **.NET Framework 4.7.2 Developer Pack**

```powershell
$MSB = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
& $MSB "QMC.Common\QMC.Common.csproj"   /t:Build /p:Configuration=Debug
& $MSB "QMC.CDT-320\QMC.CDT-320.csproj" /t:Build /p:Configuration=Debug
```

일부 프로젝트 구성의 기본 `OutputPath`가 운영 폴더를 가리킬 수 있으므로 실제 검증은 `AGENTS.md`에 따라 격리 복제본과 별도 `OutDir`를 사용한다.

## 실행

1. 필요하면 외부 Vision PC를 먼저 준비한다.
2. `QMC.CDT-320.exe`를 실행한다.
3. `UseVision=true`이면 설정된 외부 Vision에 자동 연결하고, `UseVision=false`이면 Handler의 Vision 바이패스 정책을 사용한다.

## 자동 검증

기본 검증:
- 격리된 Handler/Common Rebuild
- `git diff --check`
- `tools/audit_threading.pl`, `tools/audit_memory.pl`
- 외부 Vision이 준비된 환경에서는 Handler TCP 연결·명령·프레임 왕복 확인

자동 사이클 실 동작 (Stage 24):
```bash
QMC.CDT-320.exe --auto-cycle 10  # Init → CycleRun(10) → 종료. Lot JSON 자동 저장.
```

## 라이선스

Proprietary — © QMC

## 개발 단계 문서

- `STAGE1_CHECKLIST.md` — UI 연결 + Recipe Subset
- `STAGE3_CHECKLIST.md` — Lot + Reject + 5 Interlock + HSMS + Remote + Ionizer
- `STAGE4_CHECKLIST.md` — RemoteViewerDialog + ActiveLotPage + SecsHost UseHsms
- `STAGE5_CHECKLIST.md` — GUI Cycle 자동화 (UIA)

## 아키텍처 + 사용자 가이드

- `ARCHITECTURE.md` — 컴포넌트 + 통신 다이어그램
- `USER_GUIDE.md` — 운영 매뉴얼 (Init → Cycle → 결과 확인)
