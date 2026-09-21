# 공통 언어 처리와 번역 문구 관리

CDT-320은 기존 `Lang` API를 유지하면서 `QMC.Common.Localization`의 공통 서비스로 문구를 조회한다. 이 모듈은 표시 언어만 바꾸며 숫자 파싱에 영향을 주는 스레드 문화권, 명령, 설정값, 권한, 장비 상태를 변경하지 않는다.

## 파일 역할

| 위치 | 역할 |
|---|---|
| `QMC.Common/Localization/LocalizationService.cs` | 언어 선택, 변경 알림, 소스 우선순위, 대체 언어 조회 |
| `QMC.Common/Localization/ResourceLocalizationSource.cs` | .resx에서 지정한 문화권의 문자열 조회 |
| `QMC.Common/Localization/WinFormsLocalizationBinder.cs` | 컨트롤에 명시적으로 연결한 키 및 i18n 태그 갱신 |
| `QMC.Common/Localization/Resources/CommonStrings*.resx` | 저장, 취소, 확인 등 프로젝트 공통 문구 |
| `QMC.CDT-320/Ui/Localization/Lang.cs` | 기존 호출 호환, 리소스 구성, CDT-320 전용 버튼 표시 |
| `QMC.CDT-320/Ui/Localization/Resources/Strings*.resx` | 장비 화면의 키 기반 문구 |
| `QMC.CDT-320/Ui/Localization/Resources/DiagramStrings*.resx` | 직접 그리는 차트·맵·상태 화면의 제목, 범례, 툴팁 |
| `QMC.CDT-320/Ui/Localization/Resources/DialogStrings*.resx` | 입력·선택·로그인·작업 모드 대화상자의 문구 |
| `QMC.CDT-320/Ui/Localization/Resources/MessageStrings*.resx` | 작업 화면의 확인·실패·안내 메시지 템플릿 |
| `QMC.CDT-320/Ui/Localization/Resources/CalibrationStrings*.resx` | 콜렛·니들·픽업 Z·플레이스 Z·자동 보정의 표시와 도움말 |
| `QMC.CDT-320/Ui/Localization/Resources/VisionStrings*.resx` | 비전 설정·카메라 보정·초점·뷰어·테스트 화면 |
| `QMC.CDT-320/Ui/Localization/Resources/AdditionalDialogStrings*.resx` | 런타임 보정·수동 동작·웨이퍼맵·진행 팝업 등 추가 대화상자 |
| `QMC.CDT-320/Ui/Localization/Resources/ControlStrings*.resx` | 로그 설정·카세트 슬롯·조그·입출력·유닛 설정 등 표시 컨트롤 |
| `QMC.CDT-320/Ui/Localization/Resources/RecipeStrings*.resx` | 레시피 페이지의 그룹 제목·설정 표시·맵 생성 모드 |
| `QMC.CDT-320/Ui/Localization/Resources/SettingsStrings*.resx` | 모션·장치·입출력 등 설정 페이지의 표시 이름 |
| `QMC.CDT-320/Ui/Localization/Resources/WorkInfoStrings*.resx` | 픽커 작업정보·다이 정보·상세 로직 화면 |
| `QMC.CDT-320/Ui/Localization/Resources/WorkMapStrings*.resx` | 입력·출력 스테이지 맵의 버튼·메뉴·표·맵 제목 |
| `QMC.CDT-320/Ui/Localization/Resources/WorkMessageStrings*.resx` | 입력·출력 스테이지 맵의 확인·오류 메시지 |
| `QMC.CDT-320/Ui/Localization/Resources/HistoryStrings*.resx` | 알람 이력 해제 버튼·파일 열기·메시지 카탈로그 편집 안내 |
| `QMC.CDT-320/Ui/Localization/Resources/DisplayStrings*.resx` | 기존 원문 기반 표시의 번역 문구 |
| `QMC.CDT-320/Ui/Localization/Resources/DisplayAliases.resx` | 기존 원문을 고정 키에 연결하는 호환 자료 |

기본 `.resx`는 영어이며 `.ko.resx`, `.zh-CN.resx`, `.ja.resx`가 각 언어 파일이다. 언어별 파일이 없거나 키가 없으면 다음 대체 언어를 조회한다. CDT-320의 대체 순서는 영어, 한국어이며 끝까지 없으면 키를 그대로 표시한다. 한국어·영어만 제공하는 리소스의 중국어·일본어 표시는 아래의 대체 언어 규칙을 따른다.

## 기존 문구 수정

1. Visual Studio의 리소스 편집기에서 해당 언어 `.resx`를 연다.
2. 키(Name)는 유지하고 표시 문구(Value)만 수정한다.
3. 빌드 후 해당 화면과 언어 전환을 확인한다.

같은 영어라도 의미가 다르면 별도 키를 쓴다. `common.ok`는 확인 버튼, `inspection.good`는 검사 결과의 양품이다. 원래 명령이 `OK`라면 명령값은 계속 `OK`로 유지한다.

`DisplayAliases.resx`는 번역용 파일이 아니다. Name에는 고정 `display.*` 키, Value에는 코드가 사용하던 원문이 들어 있다. 원문의 대소문자·공백·줄바꿈은 변경하지 않는다. 컴파일러가 대소문자만 다른 리소스 이름을 중복으로 판단하므로 원문은 이름이 아닌 값으로 보관한다. `Lang`이 이를 대소문자를 구분하는 사전으로 읽는다. 영어 번역을 수정할 때는 `DisplayStrings.resx`의 Value를 바꾸고 호환용 원문은 그대로 둔다.

## CDT-320에 새 표시 문구 연결

새 문구는 의미가 고정된 키를 만들고 `Strings.resx`와 필요한 언어 파일에 등록한다. 공통 문구는 기존 `CommonStrings` 키를 먼저 확인한다. 기존 키는 문구를 바꿔도 이름을 바꾸지 않는다.

```csharp
// InitializeComponent 이후, 표시 컨트롤에 한 번 연결한다.
Lang.BindKey(btnStart, "work.start");
Lang.BindKey(btnConfirm, "common.ok");
Lang.BindKey(btnGood, "inspection.good");

// 동적 표시 영역에서는 키로 문구를 조회한다.
string caption = Lang.T("work.start");
```

`BindKey`는 컨트롤의 `Tag`를 사용하지 않으므로 권한 정보나 모델 객체를 보존한다. 기존 `Tag = "i18n:..."`도 지원하며, 태그와 바인딩이 함께 있으면 기존 동작대로 태그가 우선한다. 편집 중인 TextBox, ComboBox의 실제 값이나 선택 항목을 번역 대상으로 등록하지 않는다.

기존 `Lang.Display(원문)`과 `Lang.Bind(컨트롤, 원문)`는 호환 경로로 유지한다. 새 화면에서는 `T`와 `BindKey`를 사용한다. 기존 원문 기반 호출은 화면별 기능 검증을 거쳐 단계적으로 전환한다.

언어 변경 이벤트는 기존 Form1의 화면 갱신 경로를 그대로 사용한다. 공통 바인더는 이벤트에 자동 구독하지 않는다. 특수 그리드 등은 `ILocalizedView.ApplyLanguage()`에서 표시만 갱신하고 모델 값, 저장 Setter, 명령 실행을 호출하지 않는다.

## 다른 WinForms 프로젝트에서 재사용

.NET Framework 4.7.2와 호환되는 프로젝트에서 `QMC.Common`을 참조하고 자체 리소스를 구성한다. CDT-320 프로젝트나 `Lang.cs`를 참조할 필요는 없다. 아래 `MyProject.Resources.Strings`는 사용하는 프로젝트의 리소스 기본 이름으로 바꾼다.

```csharp
var text = new LocalizationService(
    new ILocalizationSource[]
    {
        new ResourceLocalizationSource(new ResourceManager(
            "MyProject.Resources.Strings", typeof(MainForm).Assembly), "en"),
        new ResourceLocalizationSource(new ResourceManager(
            "QMC.Common.Localization.Resources.CommonStrings",
            typeof(LocalizationService).Assembly), "en")
    },
    new[] { "ko", "en", "zh-CN", "ja" },
    "ko",
    new[] { "en", "ko" });

var binder = new WinFormsLocalizationBinder(text);
binder.Bind(btnSave, "common.save");

// UI 스레드에서 호출하며, 실제 폼에서는 구독과 해제를 함께 관리한다.
Action applyLanguage = () => binder.Apply(this);
text.LanguageChanged += applyLanguage;
Disposed += (sender, args) => text.LanguageChanged -= applyLanguage;
text.SetLanguage("en");
```

필요한 using은 `System`, `System.Resources`, `QMC.Common.Localization`이다. 서비스 인스턴스별로 언어를 관리하므로 다른 프로그램의 전역 언어 상태와 결합되지 않는다. 같은 문화권에서는 등록한 소스 순서대로 검색하므로 프로젝트 문구가 공통 문구보다 우선한다. 지정 문화권과 부모 문화권, 대체 언어와 부모 문화권, 중립 리소스 순으로 조회한다. `neutralLanguage: "en"` 설정이 중립 `.resx`를 영어 조회에 연결한다.

일반 컨트롤은 `Text`를 갱신한다. 전용 컨트롤의 다른 속성을 갱신해야 하면 바인더 생성자의 `setText`에 프로젝트 쪽 처리 함수를 전달한다. 공통 모듈에 프로젝트 전용 컨트롤 참조를 추가하지 않는다. `SetLanguage`의 이벤트는 호출한 스레드에서 발생하므로 백그라운드에서 호출할 경우 화면 소유자가 UI 스레드로 전달해야 한다.

## 빌드 및 배포

번역 문구는 빌드할 때 리소스로 컴파일된다. 문구를 수정하면 빌드·배포해야 하며 실행 중 파일 편집을 즉시 반영하는 방식은 아니다.

언어별 리소스는 빌드 출력의 `ko`, `zh-CN`, `ja` 폴더에 생성된다. 배포할 때 EXE와 Common DLL뿐 아니라 해당 폴더의 `QMC.CDT-320.resources.dll`, `QMC.Common.resources.dll`도 함께 복사한다. Visual Studio/MSBuild의 정상 빌드는 이 폴더들을 출력 위치에 생성한다. 언어 폴더를 누락하면 해당 언어 대신 대체 언어가 표시될 수 있다.

장비 명령·인터락·시퀀스 검증과 표시 언어 검증은 구분한다. 언어 구조 변경의 기본 검증은 기존 문구 전수 비교, 언어 전환 시 편집 상태와 명령값 보존, 격리 출력 빌드다. 이 검증이 실제 장비 구동을 검증한 것을 의미하지 않는다.

## 다이어그램과 메시지를 추가할 때

직접 그리는 글자는 Paint에서 `Lang.T` 또는 `Lang.Format`으로 조회한다. `ILocalizedView.ApplyLanguage()`에서는 `Invalidate()`와 표시 갱신만 호출한다. 그래프 데이터, 시간, 좌표, 상태 enum, 선택 행, 색상 판정에 번역된 문구를 넣지 않는다. 목록과 맵은 언어 변경만으로 데이터를 다시 읽거나 선택을 초기화하지 않는다.

고정 문장과 바뀌는 값은 분리한다. 예를 들어 리소스에 `이동할 수 없습니다.\r\n{0}` / `Cannot move.\r\n{0}`를 넣고 다음처럼 호출한다.

```csharp
// 실제 reason과 오류 코드는 번역 함수에 넣지 않고 템플릿의 값으로 전달한다.
string message = Lang.Format("message.stage.moveFailed", reason);
```

위 키는 새 문구를 작성하는 형식 예시이며, 사용할 키를 먼저 양쪽 언어 파일에 등록한다. `{0}`, `{1}` 같은 자리표시자는 각 언어에서 같은 인수를 참조해야 한다. 숫자 포맷은 현재 숫자 문화권을 유지하므로 언어 선택이 값 파싱 규칙을 바꾸지 않는다. 단어를 이어 붙이는 방식보다 문장 전체를 하나의 키로 관리한다.

공통 메시지창은 `MessageDialog.Localization`에 호스트의 `LocalizationService`를 연결한다. CDT-320의 `Lang`은 이를 초기화한다. 확인/취소/예/아니오/재시도/건너뛰기와 표준 제목은 `CommonStrings`의 `common.message.*`에서 관리한다. 버튼의 `DialogResult`, 기본/취소 버튼, 자동 닫기 시간은 표시 언어와 독립적이다. 사용자 지정 버튼 문구는 알려진 표준 버튼명이 아니면 그대로 유지한다.

메시지 본문은 창을 열 때 선택된 언어로 만든다. 이미 열린 공통 모달창의 본문을 다른 언어로 다시 만드는 기능은 이번 적용 범위에 포함하지 않는다. 기존 호스트의 `Lang.Apply`로 표준 버튼과 제목을 갱신할 수 있다.

현재 직접 그리기 적용 범위는 사이클 간트, 택타임 차트, 픽업 경로, 다이 맵, 실시간 웨이퍼 맵 제목/범례, 상태 화면이다. 주요 대화상자는 숫자·자재값·Enum·BIN·계정/로그인·로그설정·런타임필터·픽업순서·모드 선택을 포함한다. 작업 화면 9개 파일의 메시지 호출 169곳을 리소스에 연결했다.

후속 적용에는 기구 보정창 9개, 비전 설정·보정·뷰어, 런타임 보정·수동 동작·웨이퍼맵·진행 팝업과 로그 설정 내부, 카세트 슬롯, 맵 추적 설명을 포함한다. 설정명이나 선택 항목이 저장 키로도 쓰이는 경우에는 셀 값과 Items를 유지하고 그리는 문구만 번역한다. 외부 SDK 예외, 장비·시퀀스에서 전달한 실패 사유, 미등록 사용자 문구는 원문을 보존한다. 로그·알람의 운영자 편집 카탈로그는 기존 경로를 유지하며 GUI 리소스로 덮어쓰지 않는다. 리소스 키 수나 통과한 검증 수는 전체 GUI의 이관 완료율을 의미하지 않는다.

새 Diagram/Dialog/Message 리소스는 한국어와 영어를 제공한다. 중국어·일본어에서 아직 번역되지 않은 새 문구는 영어로 표시한다. 추가 언어는 같은 키의 해당 언어 `.resx` 파일을 등록하고 빌드하면 된다.

후속 Calibration/Vision/AdditionalDialog/Control 및 Recipe/Settings/WorkInfo/WorkMap/WorkMessage/History 리소스도 같은 한국어·영어 및 대체 언어 규칙을 사용한다. 레시피·설정 페이지의 제목과 표시 이름, 입력·출력 맵 메뉴와 확인창, 픽커 작업정보와 일부 이력 화면까지 연결되어 있다. 이미 등록된 문구의 표현을 수정할 때는 해당 `.ko.resx`의 Value만 수정한다. Name과 `{0}` 같은 자리표시자를 유지하며, C# 코드의 동작 조건을 바꾸지 않는다.

## 공용 바인더로 표·메뉴·선택 항목 연결

다른 프로젝트는 `QMC.Common.Localization`과 자체 리소스로 위의 서비스와 바인더를 구성한다. CDT-320의 `Lang`, 보정창, 장비 코드는 참조하지 않는다. `QMC.Common`의 언어 모듈은 CDT-320에 대한 프로젝트 참조가 없으며 호스트별 서비스 인스턴스를 사용할 수 있다.

```csharp
// 모두 InitializeComponent() 이후에 연결한다.
binder.Bind(btnSave, "common.save");
binder.Bind(grid.Columns["Value"], "settings.value");
binder.Bind(listView.Columns[0], "settings.name");
binder.Bind(menuSave, "common.save");
binder.Bind(toolTip, btnSave, "settings.saveHelp");

// 인수 배열을 복사해 보관한다. 문자열·숫자처럼 표시용 값을 전달한다.
binder.BindFormat(lblStatus, "status.completed", completedCount, totalCount);
binder.BindFormat(menuSelected, "menu.selected", selectedCount);

// 원본 Items와 선택값, ComboBox.Text를 유지하고 그릴 때만 번역한다.
// 기존 사용자 그리기가 없는 콤보박스에 연결한다.
binder.BindChoices(comboMode, raw => raw == "AUTO"
    ? text.GetString("mode.auto") : raw);

// 기본 대상은 읽기 전용 문자열 셀이다. 편집 셀과 ComboBox 셀은 제외된다.
binder.BindReadOnlyCells(grid, raw => raw == "READY"
    ? text.GetString("state.ready") : raw);

// 기존 언어 변경 이벤트에서 루트 컨트롤에 적용한다.
binder.Apply(this);
```

메뉴의 하위 항목·컨텍스트 메뉴·ListView 열·DataGridView 열·툴팁도 같은 Apply 경로로 갱신된다. 데이터 셀, 실제 선택값, Name, DataPropertyName, Tag는 유지한다. 명시적 바인딩만 처리하므로 사용자 입력이나 임의 문자열을 찾아서 자동 치환하지 않는다. `BindChoices`와 `BindReadOnlyCells`는 같은 컨트롤에 다시 연결해도 이벤트를 중복 등록하지 않는다.

기존 화면이 원문 상태 문자열을 전달하는 경우 `BindDisplay(control, originalText, formatter)`로 원문 스냅샷과 순수 표시 변환기를 연결할 수 있다. 변환기는 알려진 문구만 리소스로 조회하고, 알 수 없는 문구나 외부 오류 원문은 그대로 반환한다. 일반적인 새 문장은 키와 인수를 분리하는 `BindFormat`을 먼저 사용한다.

`CalibrationDialogText`, `AdditionalDialogText` 같은 원문 매핑은 기존 CDT-320 화면을 이관하기 위한 프로젝트 전용 코드다. 다른 프로젝트에 복사할 공통 규칙은 아니다. 새 화면은 원문을 다시 파싱하지 않고 `BindFormat`에 파일명·좌표·수량을 별도 인수로 전달한다. 화면 문구를 로그에도 사용하던 코드는 기록용 원문을 별도로 보관하여 표시 언어가 로그 내용을 바꾸지 않도록 한다.

CDT-320의 `Lang`은 다이 맵 컨트롤에 바인딩하면 `Caption`을 갱신한다. 맵 객체나 선택 다이는 변경하지 않는다. 설정 표처럼 표시 이름이 저장 키로도 쓰이는 컨트롤은 별도의 이름 표시 변환기를 연결한다. 내부 키·이벤트 인수·값 Dictionary는 원문을 유지해야 한다.

DataGridView 머리글 변경은 WinForms의 `CellValueChanged`에 `RowIndex = -1`로 전달될 수 있다. 저장 처리 핸들러는 기존과 같이 `e.RowIndex < 0`을 제외해야 한다. 언어 변경 처리에서 설정 로드·저장, 행 재생성, 선택 초기화, 장비 상태 재조회나 모션 명령을 호출하지 않는다.

Visual Studio 디자이너의 정적 Text/HeaderText 대입은 유지한다. 런타임 번역 바인딩은 일반 `.cs`에서 InitializeComponent 이후 등록하고, 정적인 문구와 동적으로 변경되는 문구를 중복 바인딩하지 않는다.

## 공용 진행창에서 언어 변경

`QMC.Common.Ui.Dialogs.ProgressDialog`는 마지막 `ProgressInfo`를 보관한다. 호스트에서 상태별 제목과 `CompletedStepsFormat`을 현재 언어의 리소스로 지정한 뒤 UI 스레드에서 `RefreshDisplay()`를 호출하면 제목·단계·메시지만 다시 표시한다. 진행률·완료 단계 수·상태·스핀 타이머는 변경하지 않는다.

호스트의 원문 상태명을 표시로 바꿔야 하면 `TextFormatter`에 순수 변환 함수를 연결한다. 함수가 알지 못하는 경로·오류·장비 메시지는 원문을 반환해야 한다. 원본 `ProgressInfo`를 유지하고 화면 표시 시 변환해야 재전환할 때 이전 언어의 문장이 남지 않는다.
