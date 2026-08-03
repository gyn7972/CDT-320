# QMC CDT-320

CDT-320 듀얼 픽커 다이 본더 Handler 소스 저장소다. Handler는 모션·IO·Material·Recipe·자동 시퀀스·UI를 담당하며, 외부 Vision PC와는 TCP 계약으로 연동한다. Vision 실행 프로그램, 카메라 SDK, 검사 엔진은 이 저장소에 포함하지 않는다.

## 저장소 구성

```text
QMC.CDT-320/
├─ QMC.CDT-320/      메인 Handler WinForms 애플리케이션
├─ QMC.Common/       모션, IO, Alarm, Logging, 공용 UI
├─ docs/             현재 유효한 상세 설계와 인터페이스 자료
├─ tools/            정적 검사와 개발 보조 도구
├─ AGENTS.md         저장소 전체 작업 규칙의 단일 정본
├─ README.md         프로젝트 개요와 개발 진입점
└─ QMC.CDT-320.sln   Handler + QMC.Common 솔루션
```

## 기술 구성

- C# WinForms, .NET Framework 4.7.2, Visual Studio 2022
- AJINEXTEK AXL 기반 모션·IO와 Sim 모드
- 듀얼 픽커 자동 시퀀스와 충돌 방지 인터락
- Cassette → Wafer → Die Material 상태 추적
- Recipe, DieMap, Job/Lot, Alarm, Logging, 사용자 권한, 다국어 UI
- 외부 Vision 명령·결과·프레임 TCP 통신
- SECS/GEM Host와 Remote Viewer

## 아키텍처

`QMC.CDT-320`은 UI, Equipment, Sequencing의 세 영역으로 나뉜다.

- `Ui`: 작업 화면, 설정, Recipe, 이력, 사용자 권한, 장비 조작 화면
- `Equipment`: MachineController, 축·IO, 인터락, Material, Vision 통신, SECS, 영속 상태
- `Sequencing`: Input/Output Cassette·Feeder·Stage, Picker, 검사, Calibration 자동 흐름
- `QMC.Common`: 장비 공용 축·IO 추상화, Alarm, Logging, Persistence, 공용 WinForms 컨트롤

일반 생산 흐름은 카세트 공급 → Input Stage → Wafer 정렬 → Die Pickup → Bottom/Side 검사 → Output Place → 결과 저장 → Wafer 배출 순서다. 두 픽커와 검사 존의 공유 자원은 시퀀스 락과 모션 인터락을 함께 사용한다.

## 코드 탐색 문서

- [축·IO·실린더 정의 코드 탐색표](docs/architecture/equipment-definition-code-map.md)
- [자동 시컨스 전체 흐름도](docs/architecture/auto-sequence-flow.md)
- [전체 축 초기화 시퀀스형 리팩터링 구현 프롬프트](docs/initialization/axis-initialize-sequence-refactor-implementation-prompt.txt)
- [전체 축 초기화 시퀀스형 리팩터링 검증 체크리스트](docs/initialization/axis-initialize-sequence-refactor-validation-checklist.txt)

## 외부 Vision 계약

- 명령 채널: Wafer 5100, BottomInspection 5101, Bin 5103, Main 5104, FrontSide 5105, RearSide 5106
- 영상 채널: 5200, 5201, 5203, 5205, 5206
- 라인 프로토콜: `MODULE|CMD|args...`
- Handler의 연결·명령·결과·Viewer·시퀀스 코드는 `QMC.CDT-320`에 유지한다.
- 모듈명, 명령 토큰, 포트, AlgorithmKey, CameraId와 직렬화 키는 외부 시스템 계약이므로 임의로 변경하지 않는다.

## 빌드와 검증

Visual Studio 2022와 .NET Framework 4.7.2 Developer Pack이 필요하다. 기본 솔루션은 `QMC.CDT-320.sln`이다.

원본 저장소에서 `Clean` 또는 `Rebuild`를 실행하면 과거 출력 목록을 따라 운영 폴더의 파일을 건드릴 수 있다. 검증은 `AGENTS.md`의 절차를 따라 별도 `OutDir`을 지정한 `Build`만 사용한다.

```powershell
$repo = (git rev-parse --show-toplevel).Trim()
$msbuild = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe'
$out = Join-Path $repo '_build_check_handler\out'

& $msbuild (Join-Path $repo 'QMC.CDT-320.sln') `
  /t:Build /p:Configuration=Debug '/p:Platform=Any CPU' `
  "/p:OutDir=$out\" /m /v:minimal
```

코드 변경 시 최소한 `git diff --check`, 관련 정적 검사, 별도 출력 폴더 컴파일을 수행한다. 실제 축 이동, IO 출력, 카메라 연결, 자동 사이클은 현장 상태 확인과 명시적 승인이 있을 때만 실행한다.

## 작업 시작

1. `git rev-parse --show-toplevel`로 현재 저장소를 확인한다.
2. `git status --short --branch`로 브랜치와 기존 변경을 확인한다.
3. 작업 전에 `AGENTS.md`를 읽고 장비 안전·빌드·인코딩 규칙을 따른다.
4. 상세 설계가 필요하면 `docs`에서 현재 코드와 일치하는 문서를 선택하되, 코드와 충돌하면 코드와 `AGENTS.md`를 우선한다.

## 진행 중인 UI 개선 문서

- [LOGIC TIMECHART 그래프 개선 구현 계획서](docs/TIMECHART_GRAPH_IMPLEMENTATION_PLAN.md)
- [LOGIC TIMECHART 그래프 개선 검증 체크리스트](docs/TIMECHART_GRAPH_VERIFICATION_CHECKLIST.md)
- [Calibration 개선 작업 프롬프트 & 체크리스트](docs/CALIBRATION_REFACTOR_PROMPT_CHECKLIST_2026-07-22.md)

## 라이선스

Proprietary — © QMC
