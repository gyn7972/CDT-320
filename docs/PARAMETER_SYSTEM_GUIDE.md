# 파라미터 시스템 — 유지보수 가이드 (쉬운 말)

> 목적: "이 코드 나중에 내가 못 고칠 것 같다"를 없애기 위한 실전 가이드. **결론 먼저: 파라미터를 추가·수정할 때 손대는 곳은 거의 항상 한 줄이다.** 나머지(저장·화면표시·상태점)는 자동.

---

## 1. 한 문장 멘탈 모델

- **디스크립터(ParameterDescriptor)** = 파라미터 한 칸의 "이름표". 이름·단위·**계층**, 그리고 *어디서 값을 읽고 쓸지*(실제 객체)를 담는다. **값 자체는 안 들고 있다** — 실 객체(finder/inspector/설정)를 가리킬 뿐.
- **스토어(ParameterStore)** = 이름표 모음 + 저장 담당. 화면은 스토어에 "이 검사 이름표 다 줘"라고 묻고, 저장은 스토어가 계층별 파일로 처리.
- **계층(Setup / Config / Recipe)** = 이름표에 붙은 꼬리표 한 단어. 이게 **(1) 어느 파일에 저장되고 (2) 어느 페이지에 보일지**를 결정.
  - Setup = 장비 캘리브(전원 꺼도 유지) / Config = 고정 사양 / Recipe = 제품별.

이게 전부다. 디스크립터가 "값을 안 들고 실 객체를 가리킨다"가 핵심 — 그래서 검사 알고리즘은 예전처럼 **자기 속성만 읽으면 되고 바뀐 게 없다.**

---

## 2. 내가 실제로 보는 파일은 이게 다 (8개, 대부분 안 건드림)

| 파일 | 역할 | 내가 건드릴 일 |
|---|---|---|
| `Core/Parameters/VisionParameterDescriptors.cs` | finder/inspector 공통 파라미터 목록 | **자주** (파라미터 추가·계층변경) |
| `Core/Inspectors/InspectionParameters.cs` | ② 검사 파라미터 5종(POCO) + 각자 목록 | **가끔** (검사 필드 추가) |
| `Config/VisionSettingsParameters.cs`·`CameraParameters.cs`·`LightingParameters.cs` | 전역/카메라/조명 파라미터 목록 | 가끔 |
| `Config/ParameterStoreBootstrap.cs` | 부팅 시 등록 + 마이그레이션 | 드물게 (새 provider) |
| `Core/Parameters/ParameterDescriptor.cs` | 이름표 양식 + 팩토리(Double/Int/Bool/Text/Enum) | 거의 안 봄 |
| `Core/Parameters/ParameterStore.cs` | 엔진(조회·저장·dirty) | **안 건드림** |
| `Core/Parameters/ParameterSnapshot.cs`·`SnapshotFileStore.cs` | 저장 형식·파일 | **안 건드림** |
| `Ui/Pages/VisionTargetPage.cs`(BuildParams)·`InspectorTargetPage`·`SettingsPage` | 그리드 표시 | 거의 안 건드림(자동) |

> 즉 **엔진·저장·UI는 건드릴 일이 거의 없다.** 일상 유지보수는 위 표 첫 2~3줄에서 끝난다.

---

## 3. 동작 흐름 (4단계)

1. **부팅** — `ParameterStoreBootstrap.Build()`가 모든 finder/inspector/설정을 스토어에 등록 → `LoadAll()`로 파일의 값을 실 객체에 주입.
2. **화면 표시** — 페이지가 `store.GetByTarget("검사id")` → 이름표들 → 그리드 (`VisionTargetPage.cs:124` BuildParams). 조명(Domain=Lighting)은 그리드 제외(전용 패널이 담당).
3. **편집** — 그리드에서 값 변경 → 이름표의 Setter(실 객체 갱신) + `store.SetValue`(dirty 표시 → 상태점 주황).
4. **저장** — 상단바 저장 → `store.SaveTarget("검사id")` → 계층별 파일로.

---

## 4. 레시피 — "이럴 때 → 여기"

### A) finder/inspector에 파라미터 1개 추가
`VisionParameterDescriptors.cs`의 `Finder()`(또는 `InspectorRoi()`)에 **한 줄 추가**:
```csharp
yield return ParameterDescriptor.Int(t, "MyParam", "내 파라미터", "px",
    ParameterLayer.Recipe, () => f.MyParam, v => f.MyParam = v, min: 0);
```
끝. 그리드 표시·SCOPE 라벨·저장·복원·상태점 전부 자동. (`f.MyParam`은 finder/inspector에 있는 실제 속성)

### B) 파라미터 계층 바꾸기 (예: Setup → Recipe)
해당 줄의 **`ParameterLayer.Setup` → `ParameterLayer.Recipe` 한 단어만** 변경.
- ⚠ 저장 파일이 바뀌므로(Setup파일 ↔ Recipe파일) **기존에 저장된 값은 한 번 재저장** 필요(편집→저장 1회). 단일 값이면 무시 수준.

### C) ② 검사 파라미터 필드 추가
`InspectionParameters.cs`의 해당 POCO(예 `BottomInspectionParameters`)에:
```csharp
[DataMember] public int MyField { get; set; } = 10;   // 기본값 필수
```
+ 같은 클래스 `DescribeParameters()`에 한 줄(레시피 A와 동일 형식).
+ `[OnDeserializing]` 기본값 가드가 있는지 확인(없으면 JSON에 그 필드 없을 때 0으로 덮임 — Handler 패턴).

### D) 새 검사(타깃) 추가
모듈에서 `AddFinder("...")` / `AddInspector("...")` (지금 하던 그대로). finder/inspector가 `IParameterProvider`면 `ParameterStoreBootstrap.Build()`가 모듈을 순회하며 **자동 등록**한다. 보통 별도 등록 불필요.

### E) 파라미터가 그리드에 "안 보임"
순서대로 확인: (1) `DescribeParameters()`에 그 줄이 있나 → (2) `Target`이 페이지의 검사 id와 같나(`f.Id`) → (3) `Domain == Lighting`이면 그리드에서 일부러 제외됨(전용 조명패널에 있음) → (4) **계층이 그 페이지와 맞나** (RecipePage=Recipe만, SettingsPage=Setup·Config만).

### F) 저장/복원이 "안 됨"
(1) 상단바 저장이 `store.SaveTarget(타깃)`을 부르나 → (2) 그 파라미터 계층의 저장 경로(아래 4. 위치) → (3) Sim 백엔드는 일부 no-op일 수 있음(실 백엔드 필요한 케이스).

### G) 저장 위치 (어느 파일?)
| 계층 | 파일 |
|---|---|
| Recipe | `Recipes/<default>/<검사id>.recipe.json` |
| Setup | `Config/Setup/vision_setup.json` (통합) |
| Config | `vision.json` (기존 VisionSettings) |
| Camera/Light | `algorithm_camera.json` (기존) |

---

## 5. 외워둘 규칙 3가지

1. **파라미터 추가·수정 = `DescribeParameters()` 한 줄.** 그 외는 자동.
2. **값은 디스크립터가 안 들고 실 객체에 Getter/Setter로 연결** → 알고리즘은 예전처럼 자기 속성만 읽음(무변경).
3. **계층 한 단어(Setup/Config/Recipe)가 "저장 파일 + 표시 페이지"를 결정.**

---

## 6. 헷갈리면

- "이 그리드 값이 어디서 오나?" → 그 페이지의 검사 id로 `VisionParameterDescriptors`/`InspectionParameters`/어댑터에서 `Target`이 같은 줄을 찾으면 된다.
- "이거 왜 여기 뜨나?"(조명처럼) → `Target`이 그 검사 id와 같아서 `GetByTarget`에 끌려온 것. (조명은 `Domain=Lighting`으로 그리드에서 제외 처리함.)
- 엔진(`ParameterStore`)·저장(`SnapshotFileStore`)은 한 번 동작 확인하면 다시 볼 일 거의 없다.
