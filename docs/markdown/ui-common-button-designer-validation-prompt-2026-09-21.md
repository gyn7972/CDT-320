# Common 공용 버튼 Designer 검증 — 구현 프롬프트

작성일: 2026-09-21

> 과거 초안 보존용이다. 2026-09-22 사용자 지시에 따라 별도 Preview는 제거하고 기존 카탈로그/템플릿을 활용한다. 아래 독립 Preview 추가 지시를 재실행하지 않는다. 현재 구조는 [Designer 카탈로그 안내](designer-catalog.md)를 따른다.

상태: **승인 대기 초안**. 이 문서의 작성·열람 자체는 구현 승인이 아니다. 사용자가 "이 프롬프트의 첫 검증 범위로 구현"하도록 명시적으로 지시한 뒤에만 아래 작업을 실행한다.

## 1. 이번 목표와 종료 지점

`D:\00.Source\CDT-320_New`의 현재 코드를 기준으로, 기본 WinForms Button을 사용하는 작은 공용 버튼 구조를 만들고 **Designer에서 보이고, 수정되고, 저장·재열기되는지** 검증한다.

공통 색상을 한 파일에서 관리하되 모든 정적 컨트롤과 배치는 Designer에서 직접 편집할 수 있어야 한다. 먼저 생산 화면과 연결하지 않은 독립 Preview에서만 검증한다.

**종료 지점은 공용 버튼 검증 결과 보고까지다. EventLogPage를 포함해 어떤 운영 화면도 이번에 변경하거나 공용 버튼으로 교체하지 않는다. 다음 단계는 별도 승인을 받는다.**

## 2. 고정 규칙

1. 모든 컨트롤과 정적 레이아웃은 Designer에서 수정 가능해야 한다.
2. UI/View와 비즈니스 코드는 분리한다.
3. 시퀀스 동작 중 UI 프리징이나 성능 저하를 유발하는 구조를 추가하지 않는다.
4. 초급자도 흐름을 따라 안전하게 수정할 수 있도록 단순하고 명시적으로 구현한다.

추가로 다음을 절대 지킨다.

- 인터락·시퀀스·장비 동작은 변경하지 않는다. 필요하다고 판단되면 구현을 멈추고 사유와 정확한 대상부터 보고한다.
- 아래 허용 목록 밖 파일은 변경하지 않는다. "UI 때문에 필요함", "컴파일 오류 해결", "코드 정리"는 범위 확대의 근거가 되지 않는다.
- 현재 파일을 기준으로 필요한 부분만 편집한다. 과거 소스 복원, 파일 통째 덮어쓰기·복사, rollback, 무관한 수정 취소를 하지 않는다.
- 다른 작업자의 변경은 보존한다. 허용 파일과 겹치면 해당 diff와 추가 변경 범위를 보고하여 승인받는다.
- 사용자 요청 없는 commit/push/pull/merge/rebase/reset/checkout 및 브랜치·worktree 변경을 하지 않는다.

## 3. 시작 전 확인

1. 사용자 지정 루트에 `.git`과 `QMC.CDT-320.sln`이 있는지, Git이 반환하는 루트가 일치하는지 확인한다. 비슷한 이름의 장비 백업 폴더로 바꾸지 않는다.
2. 현재 브랜치, HEAD, dirty/untracked 목록을 읽고 보고한다. 분석 당시 HEAD `57db566e`로 되돌리지 않는다.
3. 적용되는 `AGENTS.md`, `.editorconfig`를 읽는다. 현재 저장소에서는 `docs/markdown/AGENTS.md`도 확인한다. 문서의 과거 경로를 따라 다른 저장소를 수정하지 않는다.
4. 아래 읽기 전용 기준을 현재 소스로 확인한다.
   - `Ui/Common/Catalog/UiStandardCatalogForm.cs` 및 Designer
   - `Ui/Common/Templates/UiPageTemplate.cs` 및 Designer
   - `Ui/UiTheme.cs`
   - `docs/markdown/designer-catalog.md`
   - `docs/markdown/ui-standardization-first-step-analysis-2026-09-21.md`
5. 새 파일명·타입명이 이미 존재하는지 검색한다. 있으면 임의로 덮어쓰거나 별도 중복 타입을 만들지 말고 충돌을 보고한다.
6. 실제 수정 직전에 브랜치와 허용 파일의 diff를 다시 확인한다.

## 4. 수정 허용 목록 — 이 목록이 최대 범위다

다음 경로는 저장소 루트 기준이다.

| 파일 | 허용 변경 |
| --- | --- |
| `QMC.CDT-320/Ui/Common/Standards/UiStandardPalette.cs` | 신규. 새 버튼 역할의 정적 색상 정의만 |
| `QMC.CDT-320/Ui/Common/Controls/UiStandardButton.cs` | 신규. Button 파생 타입, 역할 enum/property 및 색상 적용·Reset·Designer 직렬화 판정에 필요한 최소 코드 |
| `QMC.CDT-320/Ui/Common/Catalog/UiCommonButtonPreviewForm.cs` | 신규. Form 파생 partial, InitializeComponent 호출만 있는 생성자 |
| `QMC.CDT-320/Ui/Common/Catalog/UiCommonButtonPreviewForm.Designer.cs` | 신규. 기본/공용 버튼 비교와 S/M/L 예제를 정적으로 배치 |
| `QMC.CDT-320/Ui/Common/Catalog/UiCommonButtonPreviewForm.resx` | 선택적 신규. 실제 Designer가 생성하는 기본 리소스 파일만 |
| `QMC.CDT-320/QMC.CDT-320.csproj` | 위 4개 CS 파일 Compile 등록, Form/Designer 연결 및 선택적 resx 등록만 |
| `docs/markdown/ui-common-button-designer-validation-checklist.md` | 신규. 검증 결과·사용 방법·미검증 항목 기록만 |
| `docs/markdown/README.md` | 위 검증 checklist 링크 1개 추가만. 기존 내용 보존 |

리소스가 없는 Preview에는 이미지·아이콘·지역화 데이터를 추가하지 않는다. resx는 Designer가 실제 생성할 때만 위 범위로 허용한다. Designer가 그 밖의 파일을 생성하거나 변경하면, 이를 숨기거나 임의로 삭제하지 말고 목록과 이유를 보고하여 범위를 승인받는다.

기존 파일 삭제·이름 변경은 허용하지 않는다. 검증용 임시 파일은 사전에 보고한 별도 출력 폴더에만 만들고 정확한 경로를 기록한다. 생산 소스를 임시 코드로 교체하거나 장비 프로그램을 실행하는 검증을 하지 않는다.

## 5. 명시적 변경 금지 대상

- `QMC.CDT-320/Equipment/**`, `Sequencing/**`, 인터락 관련 전체
- `QMC.Common/**` 전체. 특히 Logging, Alarm, Localization, 장비 통신
- `Form1`, `Program`, `PageBase`, `TabBase`, `HistoryTab`과 모든 운영 페이지/다이얼로그
- `EventLogPage.cs`, `.Designer.cs`, `.resx`, `EventLogDisplayBuffer`
- `UiTheme`, 기존 ActionButton 등 공유 컨트롤, 기존 카탈로그/템플릿
- WaferMap 팔레트·표시 코드, 입력/출력 맵 및 WorkMainPage
- 언어·권한·설정·레시피·Material 상태·CSV 형식·통신 계약
- `.sln`, 빌드 구성, 참조 패키지, OutputPath, 배포/설치 설정
- 운영 경로 `D:\CDT-320`과 그 아래 실행 파일·Config·Recipe·State·Log·Data

## 6. 구현 방향

### 6.1. 색상 정의는 한 곳

기존 카탈로그를 읽기 전용 기준으로 삼아 새 버튼에 사용할 값만 `UiStandardPalette`에 정의한다.

| 역할 | 배경 | 글자 | 기준 테두리 |
| --- | --- | --- | --- |
| Default | White | Black | `#808080` |
| Primary | `#D97706` | Black | `#D97706` |
| Dark | `#404040` | White | `#404040` |
| Danger | `#B22222` | White | `#B22222` |

- 동일한 역할 색상 RGB를 공용 버튼/Preview에 중복 하드코딩하지 않는다. 비교용 기존 표준 Button과 명시적 override 예제의 색상은 목적을 표시한다.
- 기존 UiTheme나 WaferMap 팔레트를 수정하거나 재사용하도록 전체 코드를 바꾸지 않는다.
- 역할은 시각적 분류일 뿐 장비 상태/권한/Enabled/Click 동작을 결정하지 않는다.
- 설정 파일·레지스트리·Lang·MessageCatalog·장비 서비스에 접근하지 않는다.

### 6.2. 공용 버튼은 단순한 Button 파생 타입

- `System.Windows.Forms.Button`을 직접 상속한다. ActionButton/장비 공통 베이스로 교체하지 않는다.
- 역할은 Designer 속성 창에서 Default/Primary/Dark/Danger로 선택할 수 있게 한다.
- 키보드 포커스, Click, Tab 이동, Enabled 등 기본 Button 동작을 재구현하지 않는다.
- 색상 역할과 개별 색상 override의 적용 순서는 짧은 주석으로 명확히 설명한다.
- 기본 WinForms 속성 메타데이터와 Reset/직렬화 판정 수준에서 해결 가능한지 검증한다. reflection, 커스텀 Designer 엔진, 속성 순회 적용기, DI 프레임워크를 도입하지 않는다.
- `OnPaint`에서 색상을 계속 덮어쓰거나 새 Font/Brush를 반복 생성하지 않는다. 기본 Button 그리기를 사용한다.
- Timer, Task, Thread, 전역 이벤트 구독, 컨트롤 재귀 순회, 주기적 스타일 재적용을 추가하지 않는다.

### 6.3. Designer 편집을 실제로 보장

- 컨트롤 선언·생성·정적 속성·배치는 Preview Designer의 InitializeComponent 안에 인라인으로 작성한다.
- Designer 안에서 `BuildControls()`, `ApplyTheme()`, `CreateButtons()` 같은 생성·배치 함수를 호출하지 않는다. 반복문으로 정적 컨트롤을 만들지 않는다.
- 공용 버튼 역할은 일반 속성 대입으로 지정한다. 공용 타입 내부의 색상 처리를 Designer 밖에 두더라도, Designer가 그 결과를 실제로 보여 주는지 검증한다.
- `Location`, `Size`, `Dock`, `Anchor`, `Margin`, `Padding`, `Font`는 Designer가 소유한다. 생성 후 역할 변경·Load·Resize·Paint 등에서 임의로 되돌리지 않는다.
- 개별 BackColor/ForeColor 편집이 저장·재열기 후 유지되어야 한다. Reset 시 역할 기본색으로 복귀해야 한다.
- 역할만 선택한 기본 사용에서는 Designer가 현재 팔레트 RGB를 고정 override로 저장해 향후 팔레트 변경을 막지 않아야 한다. 단순 컴파일이 아니라 실제 저장 결과를 확인한다.
- 역할 변경이 사용자 지정 크기·폰트·배치를 초기화해서는 안 된다. 명시적 색상 override의 유지 정책도 테스트하고 문서화한다.
- 이 조건들을 단순한 코드로 만족할 수 없으면 기능을 숨기거나 우회하지 말고 제한사항과 대안을 보고한 뒤 중단한다.

### 6.4. Preview의 범위

- 일반 Form을 직접 상속한다. PageBase/TabBase를 사용하지 않는다.
- TableLayoutPanel 중심으로 구성하고 불필요한 중첩을 만들지 않는다.
- Default/Primary/Dark/Danger 역할과 S/M/L 예제를 배치한다. 비교용 기본 Button, 개별 색상 override, Enabled=false 예제도 구분한다.
- S/M/L의 96 DPI 논리 기준은 90×36 / 120×44 / 160×52px, 글꼴은 맑은 고딕 9/10/11pt Bold다. 이 값은 Preview에서 Size/Font로 명시한다. 기존 카탈로그의 120 DPI 저장 픽셀을 그대로 옮기지 않는다.
- 첫 단계에는 크기를 자동으로 바꾸는 새 SizePreset 속성이나 크기 전역 적용기를 만들지 않는다. 실제 화면 폭에 맞춘 개별 편집을 방해하지 않는 것이 우선이다.
- Preview 생성자에는 InitializeComponent 외 장비/설정/언어/파일 처리나 스타일 적용 코드를 넣지 않는다.
- 생산 메뉴, 디버그 단축키, Form1, Program에 Preview 실행 경로를 추가하지 않는다.

## 7. 검증 — 통과와 미검증을 분리

### 정적 확인

- 새 공용 타입/Preview가 UI 기본 라이브러리와 새 팔레트 외 장비·로그·언어 의존성을 가지지 않는지 확인한다.
- 역할/색상 외 동작, 자동 배치, 타이머·스레드·주기 작업이 없는지 확인한다.
- 기존 운영 화면에서 새 타입을 참조하지 않는지 확인한다.
- `.csproj` diff가 새 파일 등록만 포함하는지 확인한다.
- 변경 파일이 허용 목록 안인지, 무관한 변경이 보존됐는지 확인한다.
- UTF-8 BOM, CRLF, final newline, 변경 범위의 `git diff --check`를 확인한다.

### 독립 검증

- 실제 생산 EXE/DLL을 실행하거나 장비 어셈블리를 로드하지 않는다.
- 필요하면 새 UI 소스만 시스템 WinForms/Drawing 참조로 별도 출력 폴더에서 직접 컴파일해 테스트한다. 기존 생산 코드/운영 데이터는 테스트 입력으로 복사하지 않는다.
- 일반 버튼 입력·포커스와 Enabled 상태를 검사한다. 새 공용 버튼 때문에 추가 주기 작업이나 지속적인 재그리기가 발생하지 않는지 확인한다.
- 팔레트 기본 역할의 색상 계산, override/Reset, 역할 변경 후 배치·Font 보존을 확인한다.

### 실제 Designer 검증 — 필수 완료 조건

실제 Visual Studio Designer 검증은 **새 UI 소스만 읽기/편집 대상으로 링크한 격리 WinForms 검증 프로젝트**에서 시작할 수 있다. 이 프로젝트는 사전 보고한 검증 폴더에 만들고, 생산 소스 파일 전체를 복사하거나 생산 EXE/DLL을 참조하지 않는다. 새로 허용한 UI 파일 외 파일을 링크하지 않으며 기존 `.sln`/빌드 설정을 바꾸지 않는다. Designer 저장이 허용 목록 밖 원본을 수정하지 않는지도 확인한다.

격리 프로젝트에서 실제 VS Designer를 사용한 결과와 생산 솔루션 안에서 연 Designer 결과는 구분한다. 생산 DLL 로드나 장비 의존성 접근이 필요하면 이 단계에서 진행하지 않고 보고한다. 첫 단계는 격리 Designer 호환 검증까지이며, 생산 솔루션 내 통합 Designer 검증은 EventLog 적용 전 별도 승인 범위다.

1. 일반 `.cs`에서 Designer 보기를 열어 모든 예제가 실제 표시되는지 확인한다.
2. 버튼을 선택하고 역할·Size·Location·Font를 속성 창에서 변경한다. 화면에 반영되는지 확인한다.
3. 저장 → 닫기 → 다시 열기 후 표시와 편집값이 유지되는지 확인한다.
4. 개별 BackColor/ForeColor override가 같은 과정을 통과하는지, Reset하면 기본 역할색으로 돌아가는지 확인한다.
5. 역할만 사용하는 버튼의 저장 코드에 기본 팔레트 색상 override가 불필요하게 굳어지지 않는지 확인한다.
6. 팔레트 변경의 전파 검증은 작업 트리의 기준색을 변경했다가 복구하는 방식으로 하지 않는다. 필요하면 별도 격리 테스트에서만 다른 색상을 가진 작은 테스트 팔레트를 정의하고 실제 새 버튼 소스는 읽기 전용 컴파일 입력으로 사용한다. 기본 역할색이 새 팔레트를 따르고 개별 override는 유지되는지 확인한다. 이 격리 검사는 실제 VS Designer 검사를 대신하지 않으므로 두 결과를 구분한다. 실제 Designer에서 재빌드 후 전파까지 확인하지 못했다면 그 항목은 미검증으로 명시한다.
7. 96/120 DPI와 실제 사용 배율 중 확인 가능한 환경을 기록하고, S/M/L 문구·테두리·포커스가 잘리지 않는지 확인한다. 확인하지 못한 배율을 통과로 기록하지 않는다.

Designer를 실행할 수 없으면 "컴파일/격리 실행 검사 완료, VS Designer 미검증"으로 보고하고 **운영 화면 적용으로 진행하지 않는다**. 이전 대화의 EventLogPage 테스트 58개나 과거 빌드 결과를 이번 새 공용 버튼 검증 결과로 재사용하지 않는다.

### 솔루션 컴파일 안전

- 원본 저장소에서 Clean/Rebuild를 실행하지 않는다. OutDir 지정 여부와 무관하게 금지한다.
- 설치된 MSBuild 경로와 사용자에게 보고한 별도 출력 폴더를 확인한 뒤 `/t:Build`, Debug, 솔루션 플랫폼 `Any CPU`로만 컴파일한다.
- 출력은 `D:\CDT-320`과 분리한다. 프로젝트 OutputPath/참조/설정을 빌드 편의상 수정하지 않는다.
- 기존 빌드 오류가 다른 파일에 있으면 원인과 새 타입 관련 여부만 보고한다. 무관한 파일을 고치지 않는다.
- 실제 장비·시뮬레이터 프로그램 시작, 축/IO/카메라/자동 운전은 하지 않는다.

## 8. 즉시 중단하고 보고할 조건

- 허용 목록 밖 파일 수정이나 신규 의존성이 필요함
- 같은 파일의 기존/동시 변경과 충돌하거나 브랜치가 달라짐
- 공용 버튼을 만들기 위해 기존 UiTheme/PageBase/운영 페이지 변경이 필요함
- Designer에 보이게 하려면 런타임 동적 컨트롤 생성·레이아웃 덮어쓰기가 필요함
- Designer 저장 후 기본색 중앙 관리 또는 개별 편집이 깨짐
- 크기·배치를 자동으로 바꾸는 구조 없이는 요구를 만족할 수 없음
- 인터락·시퀀스·알람 해제·로그 기록·번역·설정·저장 로직 변경이 필요함
- 검증하려면 운영 파일이나 장비 프로그램에 접근해야 함

중단은 실패를 숨기거나 임의로 우회하라는 뜻이 아니다. 확인한 원인, 가장 작은 대안, 필요한 추가 파일·승인 범위를 보고한다.

## 9. 최종 보고 형식

1. 변경 파일과 각 파일의 책임
2. Designer에서 열 일반 `.cs` 파일 경로와 역할·크기·개별 색상을 수정하는 방법
3. 색상 중앙 관리가 검증된 범위와 아직 적용하지 않은 기존 화면
4. 정적 검사/격리 검사/실제 Designer/솔루션 Build 결과를 각각 구분
5. 미검증 항목과 남은 제한사항
6. 운영 화면·인터락·시퀀스·로그 기록기 변경 없음 확인
7. 다음 후보 EventLogPage의 수정 예정 파일·속성은 제안만 하고 종료

검증이 끝나도 전면 UI 통일 완료라고 보고하지 않는다. 이번 완료 범위는 공용 버튼 기반과 독립 Preview 검증뿐이다.
