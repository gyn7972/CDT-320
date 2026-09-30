# Designer 공통 카탈로그 사용 안내

기존 `UiStandardCatalogForm`을 UI 표준의 단일 카탈로그로 사용한다. 별도로 추가했던 `UiCommonButtonPreviewForm`은 중복 정리하여 제거했다. 카탈로그와 템플릿은 생산 메뉴에 연결하지 않은 개발용 샘플이며 장비 서비스, 타이머, 이벤트 핸들러가 없다. 저장소 작업 규칙은 [AGENTS.md](AGENTS.md)를 따른다.

## 공용 코드와 기존 구조의 역할

| 위치 | 책임 |
|---|---|
| `QMC.Common/Ui/Standards/UiStandardPalette.cs` | 카탈로그에서 사용하던 일반 버튼의 기본 역할 색상 정의 |
| `QMC.Common/Ui/Controls/UiStandardButton.cs` | 기본 WinForms Button 동작 + Designer에서 선택하는 Role. 배치·크기·글꼴은 변경하지 않음 |
| 기존 `QMC.CDT-320/Ui/UiTheme.cs` | 기존 화면의 테마 접근점 유지. StatusBarBg/OptionHeaderBg/MenuLabelBg 배경 3개만 Common을 참조하고 나머지 색상·글꼴·치수는 보존 |
| 기존 `UiStandardCatalogForm` / `UiPageTemplate` | 공용 버튼을 실제 사용하는 예제와 TableLayoutPanel 배치 골격. 정적 속성은 각 Designer에서 직접 편집 |

의존 방향은 기존 `CDT → Common` 그대로다. Common에서 CDT를 참조하지 않는다. 같은 RGB라고 모든 의미색을 합치지 않는다. 예를 들어 헤더의 흰 글자와 Primary 버튼의 검정 글자는 다르며, 기존 Accent 주황과 상태·WaferMap 색상도 유지한다.

ActionButton(배지·다국어), SidebarButton(선택·장비 상태), BottomMenuButton(아이콘·메뉴)은 기존 기능이 있는 별도 컨트롤이다. 일반 공용 버튼의 중복 구현으로 취급하지 않는다. PageBase/TabBase, MessageBox, Progress, Vision, WaferMap의 기존 공용 코드를 계속 활용한다.

## Visual Studio에서 여는 위치

솔루션 탐색기의 `QMC.CDT-320 → Ui → Common`에서 아래의 일반 `.cs` 파일을 선택하고 **디자이너 보기**를 연다. `.Designer.cs`를 코드 편집기로 여는 것과 구분한다.

- `Catalog/UiStandardCatalogForm.cs`: 버튼, 라벨, 입력창, 상태 표시, 그리드 샘플
- `Templates/UiPageTemplate.cs`: 제목 / 본문 / 명령 영역의 배치 예시

기존 카탈로그와 템플릿은 기본 `Form`, `UserControl`을 직접 상속한다. `PageBase`, 장비 객체, 기존 테마 초기화에 의존하지 않는다. 카탈로그의 기존 `.resx`와 프로젝트 등록은 유지한다. 별도 검증 프로젝트를 만들거나 붙이지 않는다.

## 카탈로그에 포함된 샘플

활성 버튼 12개는 Common `UiStandardButton`으로 색상 역할 4종(Default / Primary / Dark / Danger)과 크기 3종(S / M / L)을 보여준다. 이름은 기존 `btnPrimaryMedium` 등을 유지한다. 템플릿의 일반/주요 버튼 2개도 같은 Common 타입을 사용한다.

카탈로그의 비활성 예제 4개는 **기존 기본 Button + 기존 회색/1px 테두리**를 유지한다. 이는 기본 WinForms 비활성 표현의 참고 샘플이며 Role 연동 예제가 아니다. 개별 테두리와 Role을 함께 지정할 때 Designer 저장 순서에 따라 테두리가 달라질 수 있어 이 단계에서 강제 교체하지 않았다.

| 크기 | 버튼 너비 × 높이 | 글꼴 | 사용 예 |
|---|---|---|---|
| S | 90 × 36 px | 맑은 고딕 9 pt Bold | 공간이 제한된 보조 기능 |
| M | 120 × 44 px | 맑은 고딕 10 pt Bold | 일반 기능 |
| L | 160 × 52 px | 맑은 고딕 11 pt Bold | 주요 확인 및 장비 동작 |

위 값은 기존 카탈로그의 96 DPI 기준 샘플이며 운영 화면의 크기를 바꾸지 않는다. 긴 문구에는 너비를 확보해야 한다. 터치가 필요한 주요 기능에는 S를 기준으로 삼지 않는다.

Designer에서 저장할 때 `AutoScaleDimensions`가 현재 모니터 DPI를 기준으로 기록될 수 있다. 예를 들어 120 DPI에서는 M 버튼이 150 × 55 px로 저장되며, 이는 96 DPI의 120 × 44 px와 같은 크기다. 2026-09-22 원본 Designer 저장 후 카탈로그·템플릿 모두 96 DPI 기준이다. 카탈로그의 120→96 DPI 재기록은 컨트롤 핸들 생성 후 배치를 비교해 검증했다. 크기를 비교할 때 저장 DPI와 화면 배율을 함께 확인한다.

- Primary 샘플: `#D97706` 배경 / 검정 글자. 대화에서 검토한 주황을 유지하면서 작은 글자의 대비를 확보했다.
- Dark 샘플: `#404040` / 흰 글자.
- Danger 샘플: `#B22222` / 흰 글자.
- 기본 글꼴: 맑은 고딕. 수치 라벨과 그리드 수치 열: Consolas.
- 입력창 샘플 높이: S 32 / M 40 / L 48 px. Designer에서 높이가 유지되도록 `Multiline=true`인 여러 줄 TextBox 샘플이다. 단일행 입력창은 네이티브 컨트롤의 폰트 기준 높이를 따르므로 이 샘플과 구분한다.
- 상태 표시는 색상과 문구를 함께 보여준다. 샘플의 RUN은 실제 장비 상태가 아니다.
- 그리드는 헤더, 교차행, 선택색, 수치 정렬과 열만 구성되어 있다. 데이터 행은 연결하지 않는다.

ComboBox, 특수 그리기 컨트롤, Vision, Map 및 성능 공통 클래스는 이번 첫 단계에 포함하지 않는다. 특히 기본 ComboBox 높이는 폰트와 네이티브 컨트롤 제약을 확인한 뒤 별도로 정한다.

## 실제 수정 위치

| 변경하려는 것 | 수정 위치 |
|---|---|
| 컨트롤의 글자, 색상, 크기, 글꼴 | Designer에서 컨트롤 선택 → 속성 창 |
| 일반 공용 버튼의 기본 색상 | Designer의 Role 선택. 기본 RGB 정의는 Common의 UiStandardPalette 한 곳 |
| 공용 버튼의 개별 색상 예외 | BackColor/ForeColor 직접 지정. Reset하면 현재 Role 기본색으로 복귀 |
| 큰 영역의 비율과 고정 높이 | TableLayoutPanel의 행/열 편집 |
| 셀 안의 배치와 여백 | Dock / Anchor / Margin / Padding |
| 실제 화면의 표시·입력 이벤트 | 해당 화면의 일반 `.cs` |
| 업무 판단과 장비 동작 | 해당 화면과 분리된 기존 서비스 |

카탈로그의 배치·크기를 바꾸어도 기존 화면은 자동으로 갱신되지 않는다. 공용 기본 색상을 바꾸고 재빌드하면 Common 버튼 및 연결된 UiTheme 배경 3개에 반영되므로 이 사용처를 함께 확인한다. 개별 BackColor/ForeColor 지정은 기본색보다 우선한다. 라벨·입력창·상태·그리드는 아직 기존 Designer 기준 샘플이며 별도 공용 타입으로 전환하지 않았다.

정적 컨트롤과 레이아웃은 각 `.Designer.cs`의 `InitializeComponent()`에 인라인으로 작성되어 있다. 역할 기본색은 공용 버튼이 제공하며, Designer 안의 스타일 적용 함수·컨트롤 재귀 순회·동적 생성 루프는 사용하지 않는다. 일반 `.cs`에는 기본 생성자만 있다.

## 페이지 템플릿의 배치

`tableRoot`는 제목 48 px, 본문 Percent 100%, 명령 영역 64 px의 3행이다. 본문에는 `tableContent`의 두 열과 일반 Panel이 있고, 명령 영역은 `tableCommands`로 구성된다. TableLayoutPanel 중첩은 2단계다.

본문 35:65 비율은 템플릿의 예시일 뿐 기존 생산 화면에 적용할 배치 지시가 아니다. 기존 화면에 적용할 때는 해당 화면의 컨트롤 이름, 위치, 이벤트와 상태 갱신을 먼저 확인한다. 템플릿 자체를 생산 화면의 기반 클래스로 강제하지 않는다.

## 확인 방법

Designer에서 각 탭과 페이지 템플릿이 열리고 속성 변경이 즉시 보이는지 확인한다. 저장 후 닫았다 다시 열어도 같은 구성이어야 한다. 실제 화면 적용은 화면별로 진행하며 프로젝트 빌드는 `AGENTS.md`의 별도 출력 폴더 절차를 따른다.

운영 화면에는 아직 공용 버튼을 적용하지 않았다. 기존 UiClickAuditor의 임시 클릭 색상 대입은 공용 버튼에서 개별색으로 기록될 수 있고, SettingsPageLayoutStyler는 실행 중 일부 버튼의 색·크기·글꼴을 덮어쓴다. 두 동작은 이번 정리에서 변경하지 않으며, 해당 화면 적용 전에 역할 변경·클릭 복원·Designer와 실행 화면의 일치 여부를 별도로 검증한다. 전체 UI 공용화 완료로 간주하지 않는다.
