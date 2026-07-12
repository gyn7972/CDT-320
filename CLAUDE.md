# CLAUDE.md

이 파일은 Claude Code가 `QMC.CDT-320` 코드베이스에서 작업할 때 참조하는 진입점이다.

## ⚠️ 최우선 작업 규칙 (모든 요청보다 우선)

새 대화·새 작업을 시작하면 아래 운영 규칙을 **가장 먼저** 적용한다. 이 규칙은 아래 코딩 규칙과 개별 사용자 요청보다 우선한다. (상세 동일 규칙은 [AGENTS.md](AGENTS.md) 최상단에도 있음)

1. 기준 작업 경로는 `D:\Source\CDT-320` **하나만** 사용한다.
2. Temp, `C:\Users\QMC\Documents\CDT-320`, 다운로드/문서/임시 복사본, `.codex-build`/`_codex_verify`/`_build_check` 같은 임시·복사본 코드는 기준으로 쓰지 않고, 거기 파일을 D 프로젝트로 복사·덮어쓰지 않는다.
3. 작업 시작 전 반드시 확인·보고: **현재 작업 경로 / `git status --short --branch` / 현재 브랜치 / 변경 파일 목록**.
4. **master/main 브랜치에서는 직접 코드 수정하지 않는다.** master/main이면 시작 전 사용자에게 알리고, 승인받은 로컬 작업 브랜치에서만 수정한다.
5. 사용자가 명시적으로 요청하지 않는 한 `git commit`/`push`/`pull`/`merge`/`rebase`/`reset`/`checkout -- 파일복구`를 실행하지 않는다. 커밋·푸시·병합은 사용자가 직접 한다.
6. 다른 PC·다른 작업자·기존 변경사항을 되돌리거나 덮어쓰지 않는다. 충돌 가능성이 있으면 먼저 보고·승인.
7. 인터락·시퀀스·좌표계·모션 동작 순서 수정은 코드 변경 전 먼저 분석·방향 보고 후, 승인받고 수정한다.
8. WinForms UI는 컨트롤 생성/배치를 Designer 파일에 인라인, 기능 로직은 일반 `.cs`에 둔다.
9. 작업 후 수정 파일·검증·빌드/테스트 결과를 간단히 보고한다. 커밋은 하지 않는다.

## 현재 작업 인계

2026-07-12 기준 Collet Bottom AF, COC, Side AF 및 생산 Side 보정 작업은 반드시 [HANDOFF_TO_CLAUDE_2026-07-12.md](HANDOFF_TO_CLAUDE_2026-07-12.md)를 먼저 읽고 이어서 진행한다. 현재 Clean Rebuild 오류와 실장비 검증 전 미확정 좌표 계약이 문서에 기록되어 있다.

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
