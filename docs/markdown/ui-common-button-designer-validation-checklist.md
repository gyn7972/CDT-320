# Common 공용 버튼 — 1차 구현·검증 기록

작성일: 2026-09-21

기준 저장소: `D:\00.Source\CDT-320_New`, 작업 브랜치: `master`.

상태(2026-09-22): **중복 Preview를 제거하고 기존 카탈로그·템플릿에서 Common 버튼을 사용하도록 정리했다. 기존 UiTheme 접근점도 유지한다. 운영 화면의 버튼 교체는 아직 하지 않았다.**

이 문서는 공용 기반의 구현·검증 이력이다. 이후 운영 화면 적용은 [현재 인계용 프롬프트](ui-standardization-rollout-prompt-2026-09-22.md)와 [순차 적용 체크리스트](ui-standardization-rollout-checklist.md)를 사용한다. 과거 검사 통과를 새 적용 검사의 통과로 옮겨 적지 않는다.

현재 기준은 사용자의 최신 지시인 **기존 구조를 먼저 분석하고 중복을 정리하여 재사용하는 단순한 구조**다. 사용 방법은 [기존 카탈로그 안내](designer-catalog.md), 이번 정리 결과는 11절을 따른다. [첫 단계 프롬프트](ui-common-button-designer-validation-prompt-2026-09-21.md)와 아래 1~10절은 당시 기록으로 보존한다. 과거의 Preview 추가, Common 변경 금지 범위, DesignerCheck 사용 안내, OutputPath 변경 제안은 현재 실행 지시가 아니다.

## 현재 구조와 다음 순서 — 원본 프로젝트만 사용

이번 단계는 기존 카탈로그·템플릿 재사용과 중복 Preview 제거까지다. 별도 프로젝트, 스타일 자동 적용 엔진, 장비 코드 의존성을 추가하지 않는다. DesignerCheck 방식은 재개하지 않는다.

| 순서 | 작업과 수정 범위 | 완료 기준 / 현재 상태 |
| --- | --- | --- |
| 1 | `QMC.Common/Ui/Standards/UiStandardPalette.cs`에 공용 색상, `QMC.Common/Ui/Controls/UiStandardButton.cs`에 기본 Button 파생 클래스 | 완료. 기존 코드에서 namespace만 바꾸어 이동. CDT 쪽 중복 정의 없음 |
| 2 | 기존 카탈로그 활성 버튼 12개 + 템플릿 버튼 2개를 Common으로 연결 | 완료. 기존 이름·이벤트·배치 구조 유지. 비활성 기본 Button 4개는 비교 샘플로 보존 |
| 3 | 중복 Preview 제거, 기존 UiTheme와 공용 팔레트 연결 | 완료. Preview 3개 파일과 등록 제거. UiTheme 배경색 3개만 동일한 Common 값 참조 |
| 4 | 원본 솔루션에서 기존 카탈로그·템플릿의 Designer 편집·저장·닫기·재열기 | 완료. 기존 안내 라벨의 문구만 편집. 템플릿의 빈 resx는 VS가 생성·등록 |
| 5 | 다음 운영 적용 전 기존 클릭 효과·상태색·런타임 스타일 덮어쓰기 검증 | 미적용. EventLogPage 버튼 3개가 첫 후보이며 검증 전 대량 교체하지 않음 |

앞서 제안한 CDT Debug OutputPath 변경은 **적용하지 않았다**. Common은 기존 `bin/Debug`로 안전하게 Build하고, 전체 솔루션은 명시적 검증 OutDir로 Build한다. 원본 Clean/Rebuild와 운영 EXE 덮어쓰기는 계속 금지한다.

사용법은 간단하다. 기존 카탈로그에서 S/M/L을 확인하고 화면 Designer에서 Common 버튼의 Role을 선택한다. 공용 기본색은 팔레트에서, 문구·크기·글꼴·TableLayoutPanel 행·열은 각 화면 Designer에서 수정한다. S/M/L은 크기 예제이며 자동 크기 전환 속성은 아니다. Label/TextBox는 기존 카탈로그 샘플을 먼저 활용하며 중복 타입을 새로 만들지 않는다.

### Common 이동 전 원인 조사 기록 (과거)

- MSBuild 평가상 현재 Release/x64와 Debug/AnyCPU의 QMC.CDT-320 TargetPath는 모두 `D:\CDT-320\QMC.CDT-320.exe`다.
- 해당 파일의 PE 메타데이터를 읽기만 해서 확인한 결과 UiStandardButton, UiStandardButtonRole, UiStandardPalette, UiCommonButtonPreviewForm **4개 타입이 모두 없다**.
- 원본을 컴파일한 `_codex_verify_ui_20260922/out/QMC.CDT-320.exe`에는 같은 네임스페이스의 **4개 타입이 모두 있다**. 원본 빌드는 오류 0개/기존 경고 41개다.
- 따라서 지정된 Designer 대상 산출물과 새 소스의 불일치는 확인됐다. 실제 로드 경로 추적 및 경로 수정 후 성공 검증은 아직 하지 않았으므로 Designer 오류의 유일한 원인이라고 확정하지 않는다.
- enum 순서, 공용 버튼 생성자, 비즈니스 코드를 추측으로 고치지 않는다. 기존 원본에도 enum 다음에 사용자 컨트롤 class를 선언하고 다른 Designer에서 사용하는 사례가 있다.

운영 EXE는 실행·어셈블리 로드·복사·교체하지 않았고 타입 메타데이터만 읽었다. 현재 EventLogPage.cs와 Designer의 SHA256은 각각 `35F8266874DCE72CD63591CE560CD71F6963D63194018C9B351225ED7723BC08`, `926AD0E068489A0B68D236D76611E2DD54E95F18E3965F6AE1F6C5389C14297C`로 이전 기준과 동일하다.

## 1. 최초 구현 당시 변경 범위와 책임 (과거 기록)

아래 소스 경로는 저장소 루트 기준이다.

| 파일 | 이번 책임 |
| --- | --- |
| `QMC.CDT-320/Ui/Common/Standards/UiStandardPalette.cs` | 신규. Default/Primary/Dark/Danger의 기본 색상만 정의 |
| `QMC.CDT-320/Ui/Common/Controls/UiStandardButton.cs` | 신규. 기본 Button 상속, Role 선택, 개별 배경·글자색 보존 및 Reset/직렬화 판정 |
| `QMC.CDT-320/Ui/Common/Catalog/UiCommonButtonPreviewForm.cs` | 신규. 생성자에서 InitializeComponent만 호출하는 독립 Form |
| `QMC.CDT-320/Ui/Common/Catalog/UiCommonButtonPreviewForm.Designer.cs` | 신규. TableLayoutPanel과 비교용 버튼·라벨을 InitializeComponent에 직접 선언·배치 |
| `QMC.CDT-320/QMC.CDT-320.csproj` | 위 4개 파일과 Form/Designer 연결 등록 10줄만 추가 |
| 이 체크리스트 및 `docs/markdown/README.md` | 검증 기록과 진입 링크 1개 추가 |

리소스를 사용하지 않아 `.resx`는 생성하지 않았다. 이전 분석/프롬프트 문서와 README의 기존 링크 변경은 보존했다. 기존 소스 삭제·이름 변경·롤백·과거 코드 복사는 하지 않았다.

운영 화면, 기존 UiTheme/카탈로그/템플릿, WaferMap, EventLogPage/표시 버퍼/로그 기록기, Equipment/Sequencing/인터락, QMC.Common은 변경하지 않았다. 생산 메뉴나 Program/Form1에 Preview 진입 경로를 추가하지 않았다. 따라서 **기존 화면은 아직 새 공용 버튼을 사용하지 않는다.**

## 2. Designer에서 수정할 항목

일반 소스 파일 `D:\00.Source\CDT-320_New\QMC.CDT-320\Ui\Common\Catalog\UiCommonButtonPreviewForm.cs`가 Designer를 열 대상이다. `.Designer.cs`는 정적 컨트롤과 배치의 저장 위치다.

- `Role`: 공용 기본색 선택. 장비 상태, 권한, Enabled 또는 Click 동작을 결정하지 않는다.
- `Size`, `Font`, `Margin`, `Padding`, `Dock`, `Anchor`: 기본 WinForms 속성 그대로 편집한다. Role 변경으로 덮어쓰지 않는다.
- `Location`: 기본 속성을 유지하지만 TableLayoutPanel 안의 실제 위치는 행·열, Anchor/Dock/Margin에 따라 결정된다. 버튼을 옮길 때는 셀 배치를 함께 편집한다.
- `BackColor`, `ForeColor`: 직접 지정하면 개별 색상으로 보존한다. 현재 기본색과 같은 값을 명시해도 개별 지정으로 취급한다. 속성 창의 Reset 또는 `Color.Empty` 지정 시 현재 Role의 기본색으로 복귀한다.
- `FlatStyle`: 기본 Button처럼 사용한다. Preview에는 `Flat`을 명시했고 `UseVisualStyleBackColor = false`를 지정했다. 파생 클래스가 크기·글꼴·FlatStyle을 자동 설정하지 않는다.

Preview는 역할별 S/M/L 12개, 일반 Button 비교, 개별 색상 지정, Enabled=false 예제로 구성했다. 크기는 96 DPI 기준 S 90×36 / M 120×44 / L 160×52, 글꼴은 맑은 고딕 Bold 9/10/11pt다. **S/M/L은 Designer 예제값이며 자동 크기 전환 속성은 아니다.**

## 3. 색상 중앙 관리와 알려진 제한

기본 배경·글자색과 역할 기본 테두리는 `UiStandardPalette`를 읽는다. 기본 사용 예제에는 역할 RGB를 중복 저장하지 않았다. 팔레트를 바꾼 후에는 재컴파일이 필요하다. 런타임 테마 자동 갱신 기능은 추가하지 않았다.

자동 검사에서 기본 역할색은 고정 RGB로 직렬화되지 않았고, 개별 BackColor/ForeColor는 저장·재생성 후 유지됐다. 다른 팔레트를 사용하는 격리 컴파일에서도 기본색 전파 및 개별 색상 보존/Reset을 확인했다.

남은 제한:

1. `FlatAppearance`의 hover 등 일부 속성만 수정해도 기본 WinForms Content 직렬화가 BorderColor/BorderSize를 함께 저장한다. 그 저장 블록에서는 이후 팔레트의 테두리 변경이 전파되지 않을 수 있다. **BackColor/ForeColor의 중앙 관리에는 영향이 없다.**
2. 테두리의 개별 지정 여부는 이전 역할의 기본값과 값이 같은지로 판단한다. 따라서 사용자가 일부러 이전 기본값과 똑같은 테두리를 지정한 경우에는 Role 변경 시 함께 바뀔 수 있다. BackColor/ForeColor에는 별도의 명시적 지정 여부를 사용한다.
3. 비활성·포커스·키보드 입력과 그리기는 기본 Button 동작을 사용한다. 자체 OnPaint, 타이머, 스레드, 주기적 스타일 적용기, 재귀 컨트롤 탐색은 추가하지 않았다.

위 제한을 숨기기 위한 별도 Designer 엔진이나 복잡한 속성 래퍼를 만들지 않았다. 운영 화면 적용 전에 필요한 테두리/hover 정책과 실제 Designer 저장 결과를 확인해야 한다.

## 4. 수행한 검증

검증 산출물은 운영 폴더와 분리한 다음 위치에 두었다.

`C:\Users\QMC\Documents\CDT-320 2\output\ui-common-button-20260921`

| 검증 | 결과와 근거 |
| --- | --- |
| 정적 범위/의존성 검사 | 신규 UI 파일만 장비 의존성 없이 구성. 운영 화면의 새 타입 사용처 없음. csproj는 새 파일 등록만 변경 |
| 기본 격리 검사 | **106개 통과**. `checks.log` |
| 대체 팔레트 검사 | **21개 통과**. `palette-checks.log` |
| 독립 Preview 렌더링 | 96×96 DPI에서 S/M/L 및 비교 예제 표시 확인. `preview.png` |
| UI 소스 전용 격리 프로젝트 Build | 성공. 경고 0, 오류 0 |
| 전체 솔루션 Debug / Any CPU Build | 성공. 오류 0, 기존 파일에서 경고 41. `solution-build.log` |

기본 격리 검사에는 다음을 포함한다.

- Role/개별 색상/Reset/Designer 속성 메타데이터 검사.
- Role 변경 시 크기·위치·글꼴·여백·Dock/Anchor·Text·Tag·TabIndex/TabStop·Enabled·DialogResult 보존.
- 기본 Click, Disabled 상태의 클릭 억제, Tab 이동, Enter/AcceptButton 동작.
- .NET `CodeDomComponentSerializationService`의 SerializationStore를 메모리로 저장하고 다시 로드·재생성하는 왕복 검사.
- DesignSurface의 CodeDomSerializer가 생성한 속성 저장문 검사. 기본 Role의 불필요한 색상 override 미저장, 개별 색상 저장, hover 설정 시 테두리 동반 저장 확인.
- 검사 중 생산 어셈블리가 로드되지 않았는지 확인.

위 직렬화 검사는 **실제 VS Designer 저장·닫기·다시 열기와 다르며, 생성된 전체 Designer 파일을 재컴파일한 검사도 아니다.** 이미지 역시 실제 Designer 캡처가 아닌 격리 런타임 렌더링이다.

대체 팔레트 검사는 출력 폴더의 작은 테스트 팔레트와 **실제 신규 UiStandardButton.cs를 직접 컴파일 입력으로 사용**했다. 생산 팔레트를 변경했다가 복구하거나 과거 소스를 복사하지 않았다. 4개 역할의 배경·글자·테두리, 이전 색상을 명시한 개별 지정 보존, Reset 후 새 팔레트 복귀를 확인했다.

전체 솔루션은 설치된 `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`의 `/t:Build`만 사용했다. OutDir는 위 검증 폴더의 `solution-build\`로 지정했다. 원본 Clean/Rebuild, OutputPath/참조/빌드 설정 변경, 운영 프로그램 또는 시뮬레이터 실행은 하지 않았다. 기존 파일 경고를 고치기 위한 범위 밖 수정도 하지 않았다.

## 5. 최초 구현 당시 남은 검증 (과거 기록 — 현재 진입점은 10절)

아래 항목은 아직 통과로 판정하지 않는다.

- [ ] 실제 Visual Studio Designer에서 Preview 열기.
- [ ] 속성 창에서 Role·Size·Font·개별 색상 수정 → 저장 → 닫기 → 다시 열기.
- [ ] 실제 Designer 저장 코드에서 기본 팔레트 값이 불필요하게 고정되지 않는지 확인.
- [ ] 실제 Designer에서 색상 Reset 및 재빌드 후 팔레트 전파 확인.
- [ ] 120 DPI 및 장비에서 사용하는 배율의 문구·테두리·포커스 확인.
- [ ] 별도 승인 후 생산 솔루션 내 Designer 통합 검증.
- [ ] 별도 승인 후 실제 화면 적용 범위의 기존 기능 회귀 검증.

실제 Designer 조작을 이 환경에서 수행하지 못했으므로 다음 단계로 넘어가지 않았다. 격리 프로젝트는 다음 위치에 준비했고 컴파일을 확인했다.

`C:\Users\QMC\Documents\CDT-320 2\output\ui-common-button-20260921\DesignerCheck.csproj`

Visual Studio에서 위 프로젝트를 열고 **새 UI 소스 전용 프로젝트만 Build**한 다음 `UiCommonButtonPreviewForm.cs`의 Designer 보기를 연다. 이 프로젝트는 생산 EXE/DLL 참조 없이 신규 UI 소스 4개만 링크한 Library이며 실행할 장비 프로그램이 없다.

주의: 링크 파일의 Designer 저장은 복사본이 아니라 **D 경로의 신규 Preview 원본**을 편집한다. 필요한 편집만 저장하고 전후 diff를 확인한다. 테스트를 위해 원본을 바꿨다가 rollback하는 방식은 사용하지 않는다. Designer가 `.resx`를 생성하면 생성 파일과 프로젝트 등록을 확인하고, 그 외 허용 범위 밖 파일 변경이 필요하면 먼저 보고한다.

검증용 프로젝트·로그·이미지는 남은 수동 Designer 검증의 진입점/근거로 출력 폴더에 보존했다. 원본 저장소에 테스트 프로젝트나 운영 프로그램 실행 경로를 추가하지 않았다.

## 6. 다음 단계 후보 — 아직 미구현

먼저 위 실제 Designer 검증과 테두리/hover 제한을 확인한다. 그 다음 별도 범위 승인 시 `QMC.CDT-320/Ui/Pages/History/EventLogPage.Designer.cs`의 `btnRefresh`, `btnOpenFile`, `btnLive` 세 버튼을 첫 적용 후보로 삼는다.

후보 변경은 버튼 타입과 역할·색상/테두리 표현으로 한정한다. 기존 이벤트 연결, 이름, Text, 위치·크기, TabIndex, Enabled 제어, 파일 조회/실시간 모드 전환은 보존한다. 현재 세 버튼에는 개별 hover/down 색상이 있으므로 이를 공용화할지 유지할지 먼저 정하고 승인받는다. S/M/L 예제 크기를 기존 24px 높이의 화면에 강제로 적용하지 않는다.

EventLogPage.cs와 EventLogDisplayBuffer, 로그 기록기, 파일 형식, 시퀀스·인터락은 후보 변경에 포함하지 않는다. 통합 스타일 적용이나 기존 타입 의존성 때문에 다른 파일 수정이 필요하면 그때 대상을 보고한다. 실제 기능 회귀 검증 없이 다음 화면으로 확장하지 않는다.

## 7. 2026-09-22 — 2단계 사전 검증 진행 기록

사용자의 2단계 시작 지시에 따라 EventLogPage 버튼 적용 전 검증을 시작했다. **아직 EventLogPage의 버튼 타입·색상·이벤트·기능 코드는 변경하지 않았다.**

### 실제 Visual Studio Designer에서 확인한 내용

- 별도로 연 Visual Studio 18 Community의 `DesignerCheck` 프로젝트에서 링크된 Preview가 정상 표시됐다. 기존 생산 솔루션을 열어 둔 다른 Visual Studio 창은 수정하지 않았다.
- 속성 창에서 공용 버튼의 Role, Size, Font, BackColor/ForeColor를 확인했다. 독립 예제 `btnOverrideSample`의 크기 128×44, 글꼴 10.5pt Bold, 배경 SteelBlue, Role Dark 편집 및 ForeColor Reset을 조작했다. Role 변경 후에도 개별 배경색과 크기가 유지됐다.
- 예제 설명을 `개별 색상 · 직접 지정`으로 편집했다. 이는 운영 화면 변경이 아니라 Designer 편집 검사용 변경이다.
- **저장 시 `UiCommonButtonPreviewForm.resx` 파일/폴더 이름이 이미 있다는 Visual Studio 오류가 발생했다. 수정값은 원본 소스에 저장되지 않았으며, 저장·닫기·재열기는 통과하지 못했다.** 위 편집값을 소스에 수동으로 덮어써 Designer 저장 성공처럼 처리하지 않았다.
- 실제 Designer가 생성한 빈 resx는 격리 프로젝트 출력 폴더에만 있다. 생산 폴더에 resx를 복사·이동하거나 생산 csproj를 추가 변경하지 않았다.

### 검증 프로젝트 조치와 현재 중단 지점

격리 프로젝트 디스크에는 resx 등록이 없고, VS 메모리/프로젝트 폴더에는 생성된 resx가 있었다. 리소스 등록 불일치가 유력하지만, 아직 원인 확정이나 해결 완료로 판단하지 않는다.

`C:\Users\QMC\Documents\CDT-320 2\output\ui-common-button-20260921\DesignerCheck.csproj`에만 기존 생성 resx를 EmbeddedResource로 등록했다. DependentUpon을 Preview 이름으로 지정하고, 링크 소스 경로 추론을 피하도록 ManifestResourceName을 `QMC.CDT_320.Ui.Common.UiCommonButtonPreviewForm`으로 명시했다. 수정 후 이 격리 프로젝트의 `/t:Build`는 성공했다.

VS의 오류 대화상자가 반복되어 프로젝트 재로드와 저장 재검증은 완료하지 못했다. **현재 열려 있는 검증용 Designer의 저장되지 않은 편집은 강제 종료·버리기·롤백하지 않았다.** 사용자에게 검증용 `DesignerCheck` 창의 오류 메시지를 닫을 수 있는지 확인을 요청했다. 기존 `QMC.CDT-320` 창을 닫거나 장비 프로그램을 실행하지 않는다. 오류 해소 후 프로젝트에 리소스 항목이 하나인지 확인하고 저장·재열기를 다시 검증해야 한다.

### 변경 전 EventLogPage 회귀 기준 확보

검증 폴더: `C:\Users\QMC\Documents\CDT-320 2\output\ui-eventlog-buttons-20260922`

현재 EventLogPage, PageBase, UiClickAuditor, 표시 버퍼 등의 소스를 원본 경로에서 직접 컴파일한 격리 검사 **65개가 통과**했다. 주 검토자가 `run-eventlog-checks.ps1 -Label preflight-confirm`으로 재실행해 동일한 결과를 확인했다. 이는 **버튼 교체 전 기준 검사**이며, 교체 후 통과 결과가 아니다.

검사 범위는 세 버튼의 이벤트 경로, Tab/Enter 전달, 파일 스냅샷과 실시간 표시 분리, 검색 필터, 연속 재조회, 조회 중 실시간 복귀와 오래된 결과 폐기, UI 타이머 응답, Enabled, 오류 후 복귀, Hide/Dispose다. 실제 UiClickAuditor의 220ms 색상 복원도 포함했다.

파일 선택기·로그 기록기·설정·번역 등 외부 의존성은 작은 테스트 더블을 사용했다. 실제 OS 파일 선택 UI, 운영 로그 파일 읽기 성능, 물리 키보드/운영 셸 통합, 실장비 부하 검증을 대신하지 않는다. 운영 EXE/DLL이나 장비 데이터는 사용하지 않았다.

배치 비교 기준 SHA256: `112803D3CE27C0CD2514E2EE18B6CF96A61E54AA0A8F9A32F41FDAC041E6519B`.

후보 역할은 REFRESH=Primary, OPEN FILE=Default, 실시간=Dark다. 기존 hover/down 개별색은 역할 공용화를 가리므로 적용 시 제거하고 기본 Button 피드백을 사용할 계획이다. 아직 이 변경은 하지 않았으며, 배치·크기·이벤트·조회 코드는 보존한다.

추가 제한: 기존 UiClickAuditor가 클릭 시 BackColor/ForeColor를 직접 지정하므로 공용 버튼의 런타임 개별색 지정 상태가 켜진다. 색상은 복원되지만 클릭한 같은 인스턴스에서 나중에 Role을 변경하면 이전 색이 유지될 수 있다. EventLogPage에는 런타임 Role 변경이 없으며, 이번 작업에서 공통 클릭 피드백 코드를 변경하지 않는다.

## 8. 2026-09-22 — 순차 적용 승인 후 저장 재검증

사용자가 순차 적용을 승인했다. EventLogPage의 버튼 3개만 적용 대상으로 유지하고, 먼저 실제 Designer 저장 검증을 재개했다.

- 변경 전 검사를 `before-apply-0922`로 다시 실행해 65개 통과를 확인했다. 배치 비교 SHA256은 앞선 기준과 동일하다.
- 검증 창에서 resx가 Form 하위 항목 하나로 표시되는 것을 확인했다. 원본 Preview는 이전 시험값이 저장되지 않은 Size 120×44, RoyalBlue, Primary 상태였다.
- 이번에는 예제 버튼의 Size만 128×44로 편집했다. 화면에는 반영됐으나 저장 시 “같은 이름의 resx가 이미 있으며 편집기에 열려 있다”는 오류가 다시 발생했다.
- 기존 빈 리소스의 다른 이름으로 저장 메뉴를 통해 Form과 같은 폴더로 저장하려 했으나, 자동 복구 저장 중에도 같은 오류가 반복됐다. 새 경로의 리소스 생성·저장은 완료하지 못했다.
- 외부 링크된 D의 Form/Designer와 C의 로컬 resx 간 물리 경로 차이가 유력한 원인 후보다. 논리적인 하위 항목 표시만으로 실제 Designer 저장 호환성이 보장되지 않는 것을 확인했다. 정확한 내부 원인과 해결 성공은 아직 확정하지 않는다.
- 원본 Preview Designer의 저장값은 여전히 120×44이며 EventLogPage 소스와 Designer는 변경하지 않았다. 미저장 128×44 시험값은 검증 창에 남겼고 강제 종료·버리기·롤백하지 않았다.

다음 작업에는 검증용 창의 미저장 시험값 정리 및 Preview와 resx의 연결 수정이 먼저 필요하다. 기존 C 리소스를 삭제하거나 생산 코드를 복사·덮어쓰지 않는다. 실제 저장·닫기·재열기가 확인될 때까지 운영 버튼 적용은 보류한다. 실장비/시뮬레이터 실행, 시퀀스·인터락·로그 기록기 변경은 하지 않았다.

## 9. 2026-09-22 — 사용자 지시에 따른 원본 프로젝트 기준 전환

사용자가 `D:\00.Source\CDT-320_New` 로컬 프로젝트만 기준으로 작업하도록 명시했다. 이에 따라 앞선 DesignerCheck 재검증/리소스 연결 수정 계획은 중단한다. 별도 프로젝트를 원본 솔루션에 붙이거나, C의 소스/리소스를 원본으로 복사하지 않는다. 앞선 절들은 당시 결과의 기록이며 현재 작업 방향은 이 절을 따른다.

- Git이 반환한 원본 루트와 `master`를 확인했다. 운영 EventLogPage 두 파일에는 여전히 변경이 없다. 원본 솔루션과 csproj에 DesignerCheck 참조도 없다.
- 원본 Visual Studio 솔루션의 Preview를 직접 열었다. 현재 `Release / x64` 구성에서는 `UiStandardButton` 형식을 찾지 못하는 Designer 오류가 표시됐다. "무시 후 계속"을 누르거나 저장하지 않았으며, 변경 없는 Preview 탭을 닫았다.
- 원본 솔루션을 `Debug / Any CPU`, `/t:Build`로 직접 컴파일했다. 출력만 원본 폴더 아래 `_codex_verify_ui_20260922/out`으로 분리했다. 오류 0개, 기존 파일 경고 41개이며 상세 기록은 같은 검증 폴더의 `build.log`에 있다. 별도 csproj나 소스 복제본을 만들지 않았다.
- 현재 Visual Studio 구성의 MSBuild 평가 결과 TargetPath는 `D:\CDT-320\QMC.CDT-320.exe`다. 안전한 검증 출력과 Designer의 현재 구성/기본 출력이 다르므로, 검증 Build 성공만으로 Designer 로드 성공을 주장하지 않는다. 정확한 타입 로드 실패 원인은 아직 확정하지 않는다.
- 운영 실행 파일 덮어쓰기, OutputPath/플랫폼 설정 변경, Clean/Rebuild, 장비/시뮬레이터 실행은 하지 않았다. 공용 버튼의 운영 화면 적용도 진행하지 않았다.

작업 규칙 AGENTS.md의 과거 기준 경로 한 줄을 사용자가 지정한 현재 경로로 정정했다. 다음 코드 적용 전에는 원본 솔루션의 Designer 타입 로드·저장·재열기 검증이 필요하다. 이를 위해 운영 출력이나 빌드 설정 변경이 필요하면 먼저 정확한 범위를 보고한다.

## 10. 2026-09-22 — 기존 Common 프로젝트로 정리 및 원본 Designer 저장 성공

사용자의 Common 공용화 지시에 따라 버튼과 팔레트 2개 파일을 기존 `QMC.Common/Ui` 아래로 이동했다. 버튼 동작과 팔레트 값은 유지했고 namespace만 `QMC.Common.Ui.Controls`, `QMC.Common.Ui.Standards`로 바꿨다. Common csproj에는 두 파일을 등록하고 CDT csproj에서는 이전 위치의 등록을 제거했다. 기존 CDT → Common 참조는 그대로 사용한다.

CDT의 `Ui/Common/Catalog/UiCommonButtonPreviewForm.Designer.cs`는 Common 타입을 사용한다. Form 생성자는 InitializeComponent만 호출한다. 운영 메뉴와 연결하지 않았으며 EventLogPage, WaferMap, 기존 UiTheme, 시퀀스·인터락·장비 로직은 수정하지 않았다.

### 실제 원본 Visual Studio 확인

- `D:\00.Source\CDT-320_New\QMC.CDT-320.sln`의 Debug/Any CPU에서 Preview가 정상 표시됐다. Common 버튼 14개와 일반 Button 비교 예제가 보인다.
- 속성 창에서 `lblOverrideExample.Text`를 `개별 색상 · 직접 지정`으로 편집하고 저장 → 닫기 → 재열기를 완료했다. resx 중복 오류가 발생하지 않았다.
- 속성 창이 `btnOverrideSample:QMC.Common.Ui.Controls.UiStandardButton`을 표시하는 것을 확인했다. 예제의 Role을 Primary에서 Dark로 변경해 저장 → 닫기 → 재열기를 완료했다. 개별 RoyalBlue/White, 크기 120×44, 맑은 고딕 Bold 10pt는 보존됐다.
- VS가 원본 Preview 폴더에 빈 `UiCommonButtonPreviewForm.resx`를 생성했다. 프로젝트 저장으로 EmbeddedResource/DependentUpon 등록이 하나 추가됐다. C 경로의 파일을 복사하지 않았다.
- 실제 Designer 직렬화 결과 기본 역할 버튼의 BackColor/ForeColor/BorderColor 고정 대입은 추가되지 않았다. 개별색 예제만 RoyalBlue/White를 저장한다. BorderSize=0은 저장되므로 테두리 크기의 향후 기본값 변경은 이 저장값에 가려질 수 있다.
- TableLayoutPanel 행·열·자식 순서, S/M/L 크기, Anchor/Dock/Margin/Padding은 유지됐다. Designer가 계산된 Location·라벨 Size를 저장하고 기본값 인수를 생략한 것은 정상 재직렬화 결과다. 기존 운영 화면의 배치를 변경한 것이 아니다.

### 현재 코드 검증과 남은 범위

- Common 공용 소스 두 개를 직접 컴파일한 독립 검사 **51개 통과**. 역할색·테두리, 개별색/동일색 보존, Reset, Designer 속성, 배치·글꼴·입력 속성 보존, Click/Enabled를 확인했다.
- 기존 Common 프로젝트 `/t:Build` 및 원본 솔루션 Debug/Any CPU `/t:Build` 성공. 실제 Designer 저장 및 resx 등록 후에도 다시 Build하여 오류 0개 / 기존 경고 41개를 확인했다. 운영 경로가 아닌 명시적 출력 폴더만 사용했다.
- 검증 로그: 원본 루트의 `_codex_verify_ui_common_20260922/common-button-checks.log`, `common-build.log`, `solution-build.log`, `solution-build-after-designer.log`. 별도 테스트 csproj는 만들지 않았다.
- 실제 장비/시뮬레이터 실행, 장비 부하 중 성능, 모든 배율, 실제 Designer 색상 Reset/팔레트 재빌드 전파는 이번 결과에 포함하지 않는다. 자동 검사 결과와 실제 장비 검증을 구분한다.

이번 단계는 Common 기반 정리까지다. 다음 적용은 EventLogPage 버튼 3개로 한정하고 기존 기능을 검증한 뒤에만 확대한다.

## 11. 2026-09-22 — 기존 구조 분석 및 중복 정리 (현재)

사용자 지시에 따라 공용 색상/테마, 기존 컨트롤의 사용처/기능, Designer 및 회귀 검증으로 나누어 분석했다. 기존 구조를 재사용하고 아래 범위만 정리했다. 새 운영 클래스나 GUI 프로젝트는 추가하지 않았다.

### 정리한 내용

- 기존 `UiStandardCatalogForm`을 단일 카탈로그로 사용한다. 기존 활성 S/M/L 버튼 12개와 `UiPageTemplate`의 버튼 2개만 Common 버튼으로 연결했다. 컨트롤 이름, 버튼 문구, TableLayoutPanel 구조를 보존했다.
- 중복으로 추가했던 `UiCommonButtonPreviewForm.cs`, `.Designer.cs`, `.resx` 및 해당 프로젝트 등록을 제거했다. 생산 코드 사용처가 없는 것을 먼저 확인했다. 삭제 대상은 Git에 추적되지 않던 추가 파일이며 Git 이력으로 복원할 수 있는 파일이 아니다. 기존 카탈로그/템플릿을 삭제하거나 과거 코드로 덮어쓰지 않았다.
- 기존 `UiTheme`의 타입/위치/호출부는 그대로 두고 `StatusBarBg`, `OptionHeaderBg`, `MenuLabelBg`만 동일한 Common 팔레트 배경값을 참조한다. 공개 필드 51개의 이름·형식·상수/readonly 여부·현재 값은 모두 동일하다.
- ActionButton의 배지/다국어, SidebarButton의 선택/장비 상태, BottomMenuButton의 아이콘/메뉴 기능은 중복이 아니다. 기존 컨트롤, PageBase/TabBase, 메시지박스, Progress, Vision, WaferMap 구현을 보존한다.
- 카탈로그의 비활성 기본 Button 4개는 기존 회색·1px 테두리 비교 샘플로 유지한다. Common Role과 명시적 테두리의 Designer 저장 순서 문제를 숨기거나 별도 직렬화 엔진으로 확장하지 않는다.

### 원본 Designer 및 코드 검증

- Computer Use로 원본 솔루션 Debug/Any CPU에서 기존 카탈로그와 템플릿을 열고, 기존 안내 라벨 문구를 수정하여 저장·닫기·재열기를 확인했다. 두 화면 모두 정상 표시됐으며 resx 중복 오류는 없었다.
- 템플릿에 없던 빈 `UiPageTemplate.resx`는 VS가 생성했다. CDT 프로젝트에는 이 리소스 등록 1개만 추가됐고, 기존 카탈로그 리소스는 유지했다. 신규 프로젝트/참조/OutputPath/플랫폼 변경은 없다.
- 실제 Designer 저장 후 공용 버튼 14개에 BackColor/ForeColor/BorderColor RGB 고정 대입이 생기지 않았다. BorderSize=0은 저장되므로 향후 기본 테두리 크기 변경 시 저장값도 확인한다. 기본색/테두리 직렬화 판정 42개는 모두 false다.
- 카탈로그 저장 기준은 120 DPI에서 현재 96 DPI로 재기록됐다. 전후 소스로 만든 UI 전용 검증 실행 파일에서 컨트롤 핸들을 생성하고 재배치한 뒤, 명명된 자식 컨트롤의 배치·크기·최소 크기·글꼴·색·Enabled·TabIndex·DialogResult·자식 순서가 동일함을 확인했다. 프로그램 창이나 운영 EXE를 실행한 검사가 아니다.
- 총 96개 컨트롤의 2,246개 속성 중 차이는 안내 문구 2개, 두께 0인 템플릿 Primary 테두리색, OS가 실행마다 정하는 최상위 Form 초기 위치, 미표시 그리드의 내부 스크롤바 초기 Bounds 2개다. 마지막 두 내부 스크롤바 상태 및 120 DPI 실제 화면은 검증 완료로 간주하지 않는다.
- Common 버튼 독립 검사 51개 통과. 원본 솔루션의 명시적 안전 OutDir `/t:Build`는 최종 Designer 저장 후 오류 0개 / 기존 경고 41개로 성공했다. `git diff --check`도 통과했다.

운영 EventLogPage/표시 버퍼, 다른 운영 화면, UiClickAuditor, SettingsPageLayoutStyler, 장비·인터락·시퀀스는 수정하지 않았다. 실장비/시뮬레이터 및 장비 부하 중 성능은 미검증이다. 다음 단계는 운영 페이지를 교체하기 전에 기존 클릭 피드백 및 상태색과의 호환성을 확인하는 것이며, 이번 결과를 전체 UI 공용화 완료로 해석하지 않는다.
