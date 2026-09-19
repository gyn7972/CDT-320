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
| `QMC.CDT-320/Ui/Localization/Resources/DisplayStrings*.resx` | 기존 원문 기반 표시의 번역 문구 |
| `QMC.CDT-320/Ui/Localization/Resources/DisplayAliases.resx` | 기존 원문을 고정 키에 연결하는 호환 자료 |

기본 `.resx`는 영어이며 `.ko.resx`, `.zh-CN.resx`, `.ja.resx`가 각 언어 파일이다. 언어별 파일이 없거나 키가 없으면 다음 대체 언어를 조회한다. CDT-320의 대체 순서는 영어, 한국어이며 끝까지 없으면 키를 그대로 표시한다. 기존에 번역이 없던 항목은 이번 구조 분리에서 임의로 번역하지 않았다.

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
