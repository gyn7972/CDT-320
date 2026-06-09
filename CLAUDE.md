# CLAUDE.md

이 파일은 Claude Code가 `QMC.CDT-320` 코드베이스에서 작업할 때 참조하는 진입점이다.

## ⚠️ 작업 전 필수 확인

코드를 수정하기 전에 **반드시 [AGENTS.md](AGENTS.md)를 먼저 읽고 그 규칙을 따른다.** AGENTS.md는 이 프로젝트의 고정 코딩 규칙(UI/Designer 작성, 예외·로그·알람, 명명, `.cs` 배치 순서, 인코딩, Material, Sequence Recovery, 한글 복원)을 정의한다. 핵심만 요약:

- **Designer 규칙**: 컨트롤 선언/배치는 `.Designer.cs`에 인라인. 비즈니스 로직은 일반 `*.cs`에. Form/UserControl은 `partial`, 클래스명 = 파일명.
- **예외 처리**: 함수는 `try/catch/finally` 기준. `catch { }`로 예외 무시 금지. 실패 시 로그 + (UI 동작이면) 메시지박스 / (Sequence면) Alarm.
- **모션/시퀀스**: 비동기 `Task<int>` 반환. 성공 `0`, 실패 `-1`/장비코드. UI Thread에서 `Thread.Sleep`·무한 `while`로 시퀀스 제어 금지.
- **명명**: 컨트롤 prefix(`btn`/`lbl`/`txt`/`cmb`/`chk`/`grid` 등), 이벤트 함수 `컨트롤명_이벤트명`, 메소드는 동사 시작, bool은 `Is`/`Can`/`Check`.
- **인코딩**: 모든 파일 UTF-8 저장. 깨진 한글 발견 시 작업 범위 내에서 복원, 불확실하면 사용자 확인.
- **JSON**: pretty UTF-8 저장(`JsonPrettySerializer.WriteObject`).

관련 규칙 상세: [MATERIAL_ARCHITECTURE_PLAN.md](MATERIAL_ARCHITECTURE_PLAN.md), [SEQUENCE_RECOVERY_RULES.md](SEQUENCE_RECOVERY_RULES.md)

## 프로젝트 개요

CDT-320 듀얼 픽커 다이 본더 핸들러 + Vision PC + 3D 시뮬레이터 통합 솔루션 (자세한 기능: [README.md](README.md), 아키텍처: [ARCHITECTURE.md](ARCHITECTURE.md)).

```
QMC.CDT-320/    # 메인 핸들러 (WinForms, .NET Framework 4.7.2)
QMC.Vision/     # 비전 PC — 별도 프로세스, TCP 5100/5101/5103
QMC.Common/     # 공용 라이브러리 (Motion/IO 추상)
CDT320Simulator/ # 3D 시뮬레이터 (WPF + HelixToolkit, TCP 7001)
tools/          # Perl 검증 스크립트 + PowerShell 자동화
*.md            # STAGE 단위 PLAN/CHECKLIST/REPORT
```

솔루션: `QMC.CDT-320.sln`

## 빌드

```powershell
$MSB = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
& $MSB "QMC.Common\QMC.Common.csproj"   /t:Build /p:Configuration=Debug
& $MSB "QMC.CDT-320\QMC.CDT-320.csproj" /t:Build /p:Configuration=Debug
& $MSB "QMC.Vision\QMC.Vision.csproj"   /t:Build /p:Configuration=Debug
```

요구사항: Visual Studio 2022, .NET Framework 4.7.2 Developer Pack, (선택) Cognex VisionPro 25.2.0 — 미설치 시 OpenCV/Sim fallback.

## 실행 순서

1. **Vision**: `QMC.Vision\bin\Debug\QMC.Vision.exe` — TCP 5100/5101/5103 listen
2. **Simulator**(선택): `CDT320Simulator\bin\Debug\CDT320Simulator.exe` — [TCP START]로 7001 listen
3. **Handler**: `QMC.CDT-320\bin\Debug\QMC.CDT-320.exe` — Vision/Sim 자동 연결

자동 사이클: `QMC.CDT-320.exe --auto-cycle 10` (Init → CycleRun(10) → 종료, Lot JSON 저장)

## 검증

```powershell
perl tools/verify_all.pl                    # 전체 통합 회귀
perl tools/verify_handler_features.pl       # Handler 310 이식 검증
perl tools/verify_vision_features.pl        # Vision 기능 (Vision exe 실행 시)
$env:RUN_GUI_CYCLE=1; perl tools/runtime_cycle_test.pl  # GUI 자동화 + Cycle
```

## 통신 프로토콜

- **Vision ↔ Handler**: `MODULE|CMD|args` 라인 프로토콜 (Wafer/Inspection/Bin TCP)
- **Simulator ↔ Handler**: JSON 명령, HELLO 메시지로 master/viewer 자동 결정
- **SECS/GEM**: SecsHost (line + HSMS dual mode)
