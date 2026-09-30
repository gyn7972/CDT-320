# GUI 공용화 순차 적용 — 다른 AI 인계용 프롬프트

작성 기준: 2026-09-22 / `D:\00.Source\CDT-320_New` / `master`

이 문서는 기존 공용 GUI의 **운영 화면 적용**을 위한 작업 지시서다. 공용 기반을 다시 만드는 지시서가 아니다. 함께 사용할 문서는 [적용·검증 체크리스트](ui-standardization-rollout-checklist.md)다. 과거 DesignerCheck/Preview 생성 프롬프트를 재실행하지 않는다.

## 1. 다음 AI에게 전달할 작업 지시

CDT-320 자동화장비의 현재 소스를 분석하고, 이미 있는 GUI 공용 구조를 재사용하여 화면별로 순차 적용하라. 기존 기능과 화면 배치를 유지하는 것이 우선이다. 새 스타일 체계나 별도 GUI 프로젝트부터 만들지 마라.

**기본 시작 범위는 아래 0~1단계의 분석·기준 측정·호환성 검증까지다. 운영 소스는 수정하지 말고, 2단계의 정확한 변경 목록과 검증 결과를 보고한 뒤 적용 승인을 받아라.** 사용자가 특정 단계를 명시해 승인했다면 그 단계만 수행한다. 문서 전달·읽기 또는 이전 단계 승인을 전체 단계의 구현·장비 실행 승인으로 해석하지 않는다.

기본 시작인 0~1단계는 하나의 사전검증 묶음으로 수행하고 결과 보고 후 멈춘다. 범위 충돌이나 추가 승인이 필요한 동작이 발견되면 그 전에 중단한다. 2단계부터는 한 단계의 결과를 보고하고 멈추며 다음 단계는 사용자 확인 후 진행한다. 실패나 미검증 항목을 숨기거나 빌드 성공만으로 전체 GUI 통일 완료를 선언하지 않는다.

### 반드시 지킬 사용자 기준

1. 모든 정적 컨트롤·레이아웃은 원본 Visual Studio Designer에서 보고 수정할 수 있어야 한다. 선언·생성·배치·이벤트 연결은 해당 Designer의 `InitializeComponent()`에 명시한다. Designer 안에 스타일 적용 함수, 동적 생성 루프, 장비 처리를 넣지 않는다.
2. UI/View와 비즈니스 코드를 분리한다. 외관 변경을 이유로 조회·저장·상태 판단·장비 명령을 함께 바꾸지 않는다.
3. UI 프리징이나 시퀀스 성능 저하를 유발하지 않는다. 스타일용 타이머, 반복 전체 컨트롤 순회, 대기·동기 파일 읽기를 새로 넣지 않는다.
4. 초급자도 수정할 수 있는 단순하고 명시적인 코드로 작성한다. 기존 클래스·상속·이벤트 흐름을 우선 활용한다.

저장소의 상시 규칙은 `docs/markdown/AGENTS.md`가 정본이다. 본 문서는 이번 GUI 작업의 범위와 통과 조건을 구체화한다. 특히 다음 사용자 제한을 유지한다.

- 인터락·시퀀스 변경은 별도 명시적 승인 전 금지한다. GUI 작업 승인은 장비·알람·통신·권한·영속 데이터 변경 승인이 아니다.
- 코드 롤백, 과거 소스 복원·복붙, 파일 전체 덮어쓰기, 다른 저장소의 소스 복제, 사용자 변경 취소를 하지 않는다. 현재 소스의 필요한 부분만 편집한다.
- 허용 파일 밖 변경이 필요하면 파일·이유·영향·최소 수정안을 보고하고 멈춘다. 빌드 오류 해결이나 코드 정리도 범위 확대의 근거가 아니다.
- 실제 장비/시뮬레이터/운영 EXE 실행, 축 이동, IO, 카메라 연결, Auto, 알람 해제, 설정·레시피 저장은 승인과 안전 상태 확인 없이 실행하지 않는다.
- 원본 Clean/Rebuild 금지. 명시적 안전 OutDir의 Build만 사용한다. `D:\CDT-320` 배포 파일과 운영 데이터는 건드리지 않는다.
- 새 브랜치/worktree, commit/push/reset/checkout 등은 별도 요청 없이 하지 않는다.

## 2. 현재 상태 — 시작할 때 반드시 소스와 대조

| 항목 | 인계 시점 상태 |
| --- | --- |
| 공용 기반 | 기존 QMC.Common에 `Ui/Controls/UiStandardButton.cs`, `Ui/Standards/UiStandardPalette.cs` 존재. 작성 시 untracked이지만 실제 사용 중인 소스이므로 삭제·재생성하지 않음 |
| 적용 예제 | 기존 `UiStandardCatalogForm` 활성 버튼 12개와 `UiPageTemplate` 버튼 2개에 적용. 비활성 4개는 기존 기본 Button 비교 예제 |
| 기존 테마 | `QMC.CDT-320/Ui/UiTheme.cs` 유지. StatusBarBg/OptionHeaderBg/MenuLabelBg만 Common 배경색 참조 |
| 운영 적용 | EventLogPage의 버튼 3개는 아직 기본 Button. 공용 버튼 사용처를 검색하여 현재 상태 재확인 |
| 크기/배치 | S/M/L은 96 DPI 기준 예제이며 자동 크기 속성이 아님. 기존 페이지 크기·폰트·TableLayoutPanel을 일괄 교체하지 않음 |
| 다른 컨트롤 | Label/TextBox/그리드는 카탈로그 샘플 단계. 전체 UI 공용화 완료 아님 |
| 기존 특수 기능 | ActionButton, SidebarButton, BottomMenuButton, ToggleSwitch, VerticalLabel, 메시지박스, Progress, Vision, WaferMap은 기존 역할을 분석하고 재사용 |
| 이전 검증 | 공용 버튼 독립 검사 51개, 카탈로그/템플릿 실제 Designer 저장·재열기, 별도 출력 Build 오류 0/기존 경고 41. 운영 페이지 적용 후 통과 기록은 아님 |
| 사용자 확인 | 프로그램 정상 실행을 사용자가 확인함. 전체 기능·Auto 부하 검증으로 확대 해석하지 않음 |

읽기 순서:

1. `docs/markdown/AGENTS.md`, `.editorconfig`, 현재 Git 상태.
2. `docs/markdown/designer-catalog.md`, 이 프롬프트와 연결된 체크리스트.
3. Common의 `UiStandardButton.cs`, `UiStandardPalette.cs`; CDT의 `UiTheme.cs`, 기존 카탈로그·템플릿.
4. `QMC.CDT-320/Ui/Pages/History/EventLogPage.cs`, `.Designer.cs`, `.resx`.
5. `QMC.CDT-320/Ui/Pages/PageBase.cs`, `Ui/Util/UiClickAuditor.cs`, `Ui/Tabs/HistoryTab.cs`.
6. `QMC.CDT-320/Ui/Common/History/EventLogDisplayBuffer.cs`, `docs/markdown/event-log-view.md`.
7. 다음 화면 선정 시에만 해당 페이지와 연결된 기존 스타일러·컨트롤·서비스를 추가 분석한다. 이름만 보고 조회 전용이라고 단정하지 않는다.

과거 근거는 `ui-common-button-designer-validation-checklist.md`에 있다. 과거 검사 실행 파일·로그는 이미 정리됐거나 없을 수 있으므로 존재 확인 없이 재사용·재실행·통과를 주장하지 않는다.

## 3. 순차 적용 순서와 종료 지점

2단계 이후의 후보 순서는 현재 파악한 위험에 따른 계획이다. 각 단계 시작 시 실제 코드와 사용자 작업 상황에 따라 재평가하고, 정확한 파일/컨트롤 허용 목록을 새로 확정한다.

| 단계 | 대상과 작업 | 종료/확대 조건 |
| --- | --- | --- |
| 0 | 원본 루트·브랜치·dirty 변경·기존 공용 사용처 확인. 현재 화면·속성·동작·성능 기준 기록 | 다른 작업과 겹침 확인, 기준표 작성. 변경 전후 비교 없이 적용하지 않음 |
| 1 | 기존 Button ↔ Common 버튼과 PageBase/UiClickAuditor 호환성 검증 | 클릭·색상 복원·입력·수명·Role/개별색 제한을 재현하고 보고. 필요 수정은 별도 승인 |
| 2 | EventLogPage의 REFRESH → OPEN FILE → 실시간 순으로 버튼 3개만 적용·개별 확인 후 전체 회귀 검사 | 원본 Designer, 안전 Build, 8개 프리셋, 비동기/성능 검사 통과 후 결과 보고. 여기서 첫 운영 적용 종료 |
| 3 | 같은 EventLogPage의 정적 라벨 → 일반 입력 외관 → 그리드 외관을 각각 작은 단위로 검토 | 기존 공용 구현 검색이 먼저. 타입 추가가 꼭 필요하면 Common의 최소 확장안 승인. 입력/필터/바인딩 동작은 유지 |
| 4 | History/MessageEditPage의 일반 외관 | 저장/가져오기 및 Maintenance 권한이 있어 단순 조회 화면이 아님. 테스트용 데이터에서 별도 승인·검증 |
| 5 | History/AlarmHistoryPage의 일반 외관 | Clear가 실제 AlarmManager.ClearAll 호출. 알람 상태·해제 조건·타이머 보존. 실장비 Clear 클릭 금지 |
| 6 | Settings의 비조작 영역부터 한 화면씩 선정 | GeneralPage 등의 SettingsPageLayoutStyler 덮어쓰기와 즉시 설정 저장 분석이 먼저. 축/IO/Teaching/통신 연결 조작은 이 단계에서 제외 |
| 7 | Recipe/Material의 표시·입력 영역부터 한 화면씩 선정 | 저장·활성 레시피·Material 상태 계약 보존. ProjectPage/TapeFrameSubsetPage/DieMapPage 등 현재 dirty 파일을 임의로 건드리지 않음 |
| 8 | WorkInfo/Work 및 셸·수동 조작 영역 | InputStageMapTransferPage, OutputStageMapTransferPage, WorkMainPage와 상태형 버튼은 후순위. WaferMap 의미색·현재 다이·픽커 보유/안착 의미, 운전 상태·권한·장비 명령 보존 |

읽기 전용으로 보이더라도 저장·알람 해제·장비 명령이 연결되어 있으면 위험 단계로 분류한다. 위 순서는 모든 페이지의 상세 분석이 끝났다는 뜻이 아니다. 자동 일괄 치환하지 않는다.

Motion/IO/Teaching 및 Recipe/WorkInfo에 연결된 Jog·Servo·이동 명령 영역은 8단계에서도 마지막에 별도로 분석한다. 7단계의 Recipe 표시·입력 외관과 실제 장비 조작 컨트롤을 한 번에 적용하지 않는다.

## 4. 1단계에서 확인할 알려진 충돌

1단계는 운영 소스 무수정이다. 직렬화는 현재 소스 읽기와 메모리상의 직렬화 검사로 확인한다. 원본 Designer의 실제 속성 편집·저장은 2단계 승인 후 수행하며, 선행 검증 때문에 먼저 저장해야 한다면 정확한 대상·시험값을 제시하고 별도 승인을 받는다.

- PageBase.OnLoad가 UiClickAuditor를 연결한다. UiClickAuditor는 클릭 시 BackColor/ForeColor를 임시 변경하고 약 220ms 뒤 이전 값으로 복원한다.
- UiStandardButton은 이 색 대입도 개별색 지정으로 기억한다. **클릭 후 런타임 Role 변경이 기본색에 반영되지 않을 수 있다.** 이 사례를 재현하고 예상/실제 값을 기록한다. 실패를 통과로 바꾸거나 임의 Reset 호출로 숨기지 않는다.
- 현재 EventLogPage는 실행 중 Role을 변경하지 않지만, 이 제한을 이유 없이 무시하지 않는다. 필요하면 ‘고정 Role 화면에만 제한 적용’안을 사용자에게 제시하고 명시적으로 승인받는다. 동적 상태색 화면까지 검증 완료로 보지 않는다.
- 공용 버튼이나 UiClickAuditor 수정이 필요하면 그 영향 범위가 모든 Button 사용자까지 넓어질 수 있다. 해당 단계의 별도 승인 전 수정하지 않는다.
- 개별 FlatAppearance와 Role은 Designer 직렬화 순서에 영향을 받는다. 기본 역할 RGB 고정 저장, 테두리/hover/down 개별값 잔류, 개별색 Reset을 확인한다.
- SettingsPageLayoutStyler는 일부 버튼의 색·글꼴·최소 크기를 런타임에 덮어쓴다. 현재 EventLog 경로의 직접 호출은 없지만 Settings 단계의 선행 분석 대상이다. 전체 스타일러를 비활성화하지 않는다.

## 5. 2단계 승인 시 수정 허용 범위

운영 코드 허용 파일은 **`QMC.CDT-320/Ui/Pages/History/EventLogPage.Designer.cs` 하나**, 대상은 아래 3개뿐이다. 기록은 연결된 체크리스트에 갱신한다.

| 컨트롤 | 변경안 | 반드시 보존 |
| --- | --- | --- |
| btnRefresh | 타입을 Common UiStandardButton으로, Role=Primary | 기존 Name/Text, 크기/최소 크기, 폰트, TabIndex=12, btnRefresh_Click |
| btnOpenFile | 타입을 Common UiStandardButton으로, Role=Default | 기존 Name/Text, 크기, 폰트, TabIndex=13, btnOpenFile_Click |
| btnLive | 타입을 Common UiStandardButton으로, Role=Dark | 기존 Name/Text, 크기, 폰트, TabIndex=14, btnLive_Click |

필드 선언·생성 타입을 함께 수정한다. 기존 역할색을 가리는 BackColor/ForeColor와 세 버튼의 과거 hover/down/테두리 개별값만 검토하여 제거·조정한다. 기대 색상과 기본 Button의 눌림/hover 피드백을 사전에 기록한다. 역할 RGB를 Designer에 다시 하드코딩하지 않는다.

위치, Dock/Anchor, Margin/Padding, TableLayoutPanel 행·열·셀·자식 순서, Enabled 판단, 이벤트 연결과 횟수, 번역 키/Tag, DialogResult/키보드 동작은 보존한다. S/M/L을 맞추려고 크기를 변경하지 않는다. 기존 REFRESH Size와 MinimumSize 차이 같은 주변 문제도 이번에 수정하지 않는다.

명시적으로 제외되는 파일/동작:

- `EventLogPage.cs`, `.resx`, EventLogDisplayBuffer, EventLogger/CSV 형식 및 기록 정책.
- PageBase, TabBase, HistoryTab, UiClickAuditor, SettingsPageLayoutStyler, 공용 버튼 동작/팔레트와 UiTheme.
- 다른 페이지·특수 컨트롤·번역·권한·설정·레시피·Material·장비·시퀀스·인터락.
- csproj/솔루션/빌드 설정/OutputPath/패키지 변경. Common 참조는 기존 것을 사용한다.

Designer 저장이 허용 목록 밖 파일을 변경하거나 다른 페이지로 전파되면 저장 결과와 이유를 먼저 보고한다. 변경을 감추기 위해 롤백·삭제·복사를 하지 않는다.

## 6. 검증 환경과 빌드

- 테스트는 현재 소스를 직접 사용한다. 별도 DesignerCheck/Preview 프로젝트, 가짜 페이지로 원본 대체, 운영 코드 복사본을 만들지 않는다.
- 필요한 무장비 테스트는 기존 테스트가 있으면 먼저 재사용한다. 없으면 검증 폴더의 최소 테스트만 허용하며 원본 UI 소스를 직접 컴파일한다. 장비/파일/로그 의존성을 대신한 부분을 결과에 명시한다. 원본 이벤트·클릭 효과 자체를 대체한 검사로 그 호환성을 주장하지 않는다.
- 기준 소스를 롤백하지 않는다. 변경 전 속성·출력·스크린샷·해시·성능값을 먼저 기록하고 현재 결과와 비교한다. 당시 원본에서 컴파일한 무장비 검사 산출물은 원본 코드 복사와 구분하되 목적/경로를 기록한다.
- 실제 Designer 조작이 불가능하면 그 항목은 미검증이다. 코드 검사/컴파일로 저장·재열기를 대체하지 않는다. 사용자 수동 확인 절차를 제공하고 확대를 보류한다.
- 운영 EXE/F5 실행 없이 가능한 검사를 먼저 한다. 실제 셸 통합, OS 파일 선택기, 장비/시뮬레이터 부하 검증은 승인된 환경에서 별도로 수행한다.

현재 설치된 MSBuild 경로를 확인한 뒤, 지정 루트에서 다음 형태의 Build만 사용한다. 이 명령은 실행 파일을 구동하지 않는다.

```powershell
Set-Location -LiteralPath 'D:\00.Source\CDT-320_New'
$uiRolloutRepo = (git rev-parse --show-toplevel).Trim()
if ($uiRolloutRepo.Replace('/', '\') -ne 'D:\00.Source\CDT-320_New') { throw '작업 루트 불일치' }
$uiRolloutMsbuild = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe'
if (-not (Test-Path -LiteralPath $uiRolloutMsbuild)) { throw 'MSBuild 설치 경로 확인 필요' }
$uiRolloutOut = Join-Path $uiRolloutRepo '_codex_verify_ui_rollout\out'
& $uiRolloutMsbuild (Join-Path $uiRolloutRepo 'QMC.CDT-320.sln') /t:Build /p:Configuration=Debug '/p:Platform=Any CPU' "/p:OutDir=$uiRolloutOut\" /m:1 /v:minimal
if ($LASTEXITCODE -ne 0) { throw '빌드 실패: 범위를 넓히지 말고 원인 보고' }
```

빌드 결과는 실제 오류·경고 수로 기록한다. 과거 41개 경고를 무조건 현재 기준으로 단정하거나, 무관한 경고를 이번에 정리하지 않는다. `.editorconfig`의 UTF-8 BOM/CRLF/final newline을 수정 파일에만 적용하고 대상 diff를 검사한다.

## 7. Designer 중복 재발 주의

화면은 본체 `.cs`의 Designer로 연다. `*.MapProcessing.cs`, `*.Speed.cs` 등 기능 분할 partial 파일을 별도 화면으로 편집하지 않는다.

직전 MSB3577은 TapeFrameSubsetPage의 기존 resx와 빈 MapProcessing.resx가 같은 이름으로 출력된 문제였다. 중복 InitializeComponent/빈 리소스·등록을 승인받아 제거했고 Build를 통과했다. **본 문서 작성 시 재확인한 csproj에서는 MapProcessing의 SubType이 다시 UserControl로 보인다.** 변경 주체/원인은 확인하지 않았고, 현재 중복 메서드와 빈 resx는 재발하지 않은 상태다. SubType 값만으로 현재 빌드 실패를 단정하지 않는다.

인계 후 해당 등록과 중복 선언/리소스 출력 이름을 다시 확인하되, GUI 적용 범위를 넓혀 자동 수정하지 않는다. 본체는 `TapeFrameSubsetPage.cs`이며 기존 Designer/리소스를 유지한다. `[DesignerCategory]`를 partial 클래스에 붙여 본체 Designer까지 숨기는 방식은 사용하지 않는다.

## 8. 매 단계 보고 형식

1. 현재 루트·브랜치·단계·승인된 파일/컨트롤.
2. 기존 코드 사용처·책임 및 중복을 만들지 않은 근거.
3. 변경 파일·속성·이유, 보존한 기능/사용자 변경.
4. 체크리스트 ID별 변경 전/후 결과·실행 방법·측정값·증거 경로.
5. 통과 / 실패 / 미검증 / 해당 없음(사유)을 구분. 대체 의존성·실장비 미검증 범위를 명시.
6. 범위 밖 diff, 신규 오류, Designer 저장 실패, 색상 고착, 이벤트 중복/누락, 비동기 결과 역전, 반복되는 성능 악화가 있으면 다음 단계로 넘어가지 않음.
7. 다음 단계 제안은 한 개만 제시하고 승인 전 구현하지 않음. 검증 임시 산출물은 용도가 끝나면 정확한 경로만 정리하고 결과 기록은 남김.

시작 지시 예시: “이 문서와 체크리스트를 읽고 0~1단계만 진행해 주세요. 운영 소스는 수정하지 말고, EventLogPage 버튼 3개 적용 가능 여부와 필요한 최소 수정 범위를 먼저 보고해 주세요.”
