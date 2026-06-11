# Vision 파라미터(Setup/Config/Recipe) — BaseUnit 일원화 가이드 (사용법)

> 2026-06-09. 디스크립터/ParameterStore 폐기 후 **BaseUnit Composite** 로 일원화한 최종 구조의 사용 설명.
> 결론 먼저: **저장/불러오기/삭제는 모듈 1회 호출이면 알고리즘까지 다 처리된다. 파라미터 추가는 데이터 클래스에 한 줄.**

---

## 1. 한 장 멘탈 모델

```
VisionModule (Composite, 예: WaferVision)
   ├─ 자기 Setup/Config/Recipe        (카메라/시뮬모드/노출 등 모듈 레벨)
   └─ Components = 알고리즘 노드들
         ├─ FinderAlgorithm("...EjectPinFinder")   ← 자기 Setup/Config/Recipe + 실제 finder 래핑
         └─ InspectorAlgorithm("...SurfaceInspector") ← 자기 Setup/Config/Recipe + 실제 inspector 래핑
```

- **모듈 = 트리 루트, 알고리즘 = 자식.** 모듈에 `SaveRecipe()` 한 번 호출하면 **자기 것 + 모든 알고리즘** 이 연쇄(cascade) 저장된다.
- 각 노드는 데이터 POCO(`FinderAlgoRecipe` 등)를 들고, **실제 finder/inspector(런타임이 읽는 값)와 자동 동기화**된다(Load 시 POCO→런타임 주입 / Save 시 런타임→POCO 수집 = `Apply/Collect`).
- **계층(Setup/Config/Recipe) = 데이터가 어느 클래스에 선언됐느냐.** 그게 곧 저장 파일·수명을 결정.
  - Setup = 전원 OFF 후 유지(장비 캘리브) / Config = 고정 사양 / Recipe = 제품·공정별.

---

## 2. 핵심 파일 (몇 개 안 됨)

| 파일 | 역할 | 손댈 일 |
|---|---|---|
| `Modules/AlgorithmData.cs` | 알고리즘 Setup/Config/Recipe 데이터 타입(=계층) | **자주**(파라미터 추가/계층) |
| `Modules/VisionModuleData.cs` | 모듈 Setup/Config/Recipe(카메라/노출 등) + 5모듈 고유 | 가끔 |
| `Modules/VisionModule.cs` | 모듈 베이스 — 알고리즘 등록(`AddFinder/AddInspector`)·`GetAlgorithm` | 모듈/알고리즘 추가 시 |
| `Modules/AlgorithmNode.cs` | 노드 + **Apply/Collect**(POCO↔실 finder 동기화) | 새 필드가 런타임 거처 있으면 한 줄 |
| `Modules/IVisionModule.cs` | 소비자(Form1·UI)가 쓰는 표면 API | 거의 안 봄 |
| `QMC.Common/BaseUnit.cs`·`Data/Store/UnitDataStore.cs` | 토대(Composite 저장/로드/삭제) | **안 건드림** |

---

## 3. 데이터 구조 한눈에 (현재)

**모듈 레벨** (`VisionModuleData.cs`)
- Setup: `IsSimulationMode`
- Config: `CameraId, Gain, FrameRate, TriggerMode, PixelFormat, DelayBeforeGrabMs, RoiOffsetX/Y, RoiWidth/Height`
- Recipe: `Exposure` (+ 모듈 고유: Wafer `AlignTimeoutMs`, Bin `PlacementToleranceMm`, BottomInsp `SurfaceThreshold`, Front/RearSide `ChippingThreshold`)

**알고리즘 레벨** (`AlgorithmData.cs`)
- Finder — Setup: (없음) / Config: `MaxInstances, AngleEnabled` / Recipe: `AcceptThreshold, SearchRoi, TrainRoi, TrainModelPath`
- Inspector — Setup: `CalibModelPath` / Config: `Enable` / Recipe: `InspectionRoi, Threshold(int)`

---

## 4. 사용법 레시피 (★ 제일 자주 보는 곳)

### 저장
```csharp
module.SaveSettings();        // 모듈+모든 알고리즘의 Setup+Config 저장 (cascade)
module.SaveRecipe("ProdA");   // 모듈+모든 알고리즘의 Recipe 저장 (제품명 "ProdA")
```
알고리즘 하나만:
```csharp
var node = module.GetAlgorithm("EjectPinFinder");   // 없으면 null
node.SaveRecipe("ProdA");
node.SaveSettings();
```
> 저장 직전 `CollectFromRuntime()` 가 실제 finder/inspector 의 현재 값을 POCO 로 수집 → 그래서 **런타임에서 바꾼 값이 그대로 저장**된다.

### 불러오기
```csharp
module.LoadSettings();        // Setup+Config 로드 (부팅 시 Form1 이 이걸 호출)
module.LoadRecipe("ProdA");   // Recipe 로드
```
> 로드 직후 `ApplyToRuntime()` 가 POCO→실 finder/inspector 주입 → **로드 즉시 검사(Match/Inspect)에 반영**.

### 수정(Edit)
두 가지 경로 — 결과는 같다(저장 시 Collect 가 수집):
1. **런타임 객체 직접**(UI 그리드가 이 방식): `finder.AcceptThreshold = 0.8; finder.SearchRoi.CenterX = 320;` 후 `node.SaveRecipe("ProdA")`.
2. POCO 직접: `((FinderAlgoRecipe)((IAlgorithmNode)node).Recipe).AcceptThreshold = 0.8;` 후 저장. (BaseUnit 의 `Recipe` setter 가 private 이라 **객체 통째 교체는 불가, 필드만 변경**.)

### 삭제
```csharp
module.DeleteSettings();       // Setup+Config 파일 삭제 (모듈+알고리즘 cascade)
module.DeleteRecipe("ProdA");  // 해당 제품 Recipe 파일 삭제
var node = module.GetAlgorithm("EjectPinFinder");
node.DeleteRecipe("ProdA");    // 알고리즘 하나만
```

### 알고리즘 찾기 / 순회
```csharp
IAlgorithmNode n = module.GetAlgorithm("SurfaceInspector");   // id로
foreach (var a in module.Algorithms) { var f = a.Finder; var i = a.Inspector; }
```

---

## 5. 저장 파일 위치

`StorageKey` = 모듈 `"WaferVision"`, 알고리즘 `"WaferVision.EjectPinFinder"`.

```
EquipmentData/Setup/WaferVision.json                         (모듈 Setup)
EquipmentData/Config/WaferVision.json                        (모듈 Config)
EquipmentData/Setup/WaferVision.EjectPinFinder.json          (알고리즘 Setup)
EquipmentData/Config/WaferVision.EjectPinFinder.json         (알고리즘 Config)
Recipes/ProdA/WaferVision.recipe.json                        (모듈 Recipe)
Recipes/ProdA/WaferVision.EjectPinFinder.recipe.json         (알고리즘 Recipe)
```
- 직렬화: `DataContractJsonSerializer`(read) + `JsonPrettySerializer`(write), 원자적 저장. 모든 데이터 POCO 는 `[OnDeserializing]` 으로 기본값 보강(필드 누락 시 0/null 방지).

---

## 6. 자주 하는 변경

### 파라미터 1개 추가 (알고리즘)
1. `AlgorithmData.cs` 의 해당 계층 클래스에 필드 + 기본값:
   ```csharp
   [DataMember] public double MyParam { get; set; }
   // 그 클래스 SetDefaults() 에 MyParam = 기본값;
   ```
   (어느 클래스에 넣느냐 = 계층. FinderAlgoRecipe=Recipe, FinderAlgoConfig=Config …)
2. **런타임 finder/inspector 에 대응 속성이 있으면** `AlgorithmNode.cs` 의 `FinderAlgorithm`/`InspectorAlgorithm` 에 한 줄씩:
   ```csharp
   // ApplyToRuntime():   _finder.MyParam = r.MyParam;
   // CollectFromRuntime(): r.MyParam = _finder.MyParam;
   ```
   대응 속성이 없으면(저장만 하고 알고리즘이 아직 안 씀) Apply/Collect 생략 — POCO-only.
3. UI 그리드에 노출하려면 그 페이지 `BuildParams()` 에 `ParameterGridItem.Double(...)` 한 줄.

### 계층 바꾸기
그 필드 줄을 다른 계층 클래스로 **이동**(예 `FinderAlgoSetup`→`FinderAlgoRecipe`). 저장 파일 위치 자동 변경. (기존 값 있으면 1회 재저장.)

### 새 모듈 / 새 알고리즘
- 모듈: `VisionModuleData.cs` 에 `XxxSetup/Config/Recipe` 추가(대개 base 상속) → `XxxModule : VisionModule<XxxSetup,XxxConfig,XxxRecipe>`.
- 알고리즘: 모듈 생성자에서 `AddFinder<FinderAlgoSetup,FinderAlgoConfig,FinderAlgoRecipe>("XxxFinder")`. **자동으로 노드 등록 + Save/Load/Delete cascade 대상**이 됨. 별도 등록 불필요.

---

## 7. 부팅 / UI 연결 방식 (참고)

- **부팅**(`Form1.cs`): 모듈들 생성 후 `foreach m: m.LoadSettings();` — Setup/Config 로드(Recipe 는 제품 선택 시 `LoadRecipe`). 현재 제품명은 상수 `"default"`(제품 선택기는 후속).
- **UI 그리드**(`VisionTargetPage`/`InspectorTargetPage`): `ParameterGridItem.Double("Search X", … () => _finder.SearchRoi.CenterX, v => _finder.SearchRoi.CenterX = v)` 처럼 **실제 finder 속성에 직접 바인딩**(추상화 없음). 편집 → 런타임 즉시 반영(미저장은 상태점 표시).
- **저장/불러오기 버튼**: `node = module.Algorithms.FirstOrDefault(a => a.Finder == _finder)` → `node.SaveSettings()+SaveRecipe("default")` / `LoadSettings()+LoadRecipe("default")`.

---

## 8. 주의점

- **수정 후 저장 안 하면** 런타임 값은 바뀌어 있지만(WYSIWYG) 파일엔 미반영 — 상태점(주황)으로 표시됨. `LoadRecipe` 하면 저장본으로 되돌아감.
- **Recipe setter 는 private** — 객체 통째 교체 불가, 필드만 변경.
- **인스펙터 Threshold 는 int**(Cognex 0~255 그레이). 소수 임계가 필요한 백엔드가 생기면 그때 타입 재검토.
- **310-era ② 필드(ChippingDepth/ColorGap/SpecLimit 등)는 현재 모델에 없음** — 과거 미사용(orphan)이라 드롭. 실제 검사 알고리즘이 그 필드를 쓰게 되면 `InspectorAlgo*` 에 추가 + Apply/Collect 연결.
- `QMC.Common` 은 본 리팩터링에서 Delete cascade API 가 추가됨(공용 모듈 — push/머지 시 팀 조율).

## 9. 검사별 전용 파라미터 추가법 (①② 인프라, C3 후속)

검사 하나에만 있는 전용 파라미터(예: EjectPin 의 PinGap)를 추가하는 재사용 메커니즘. **공용 필드(ROI/Threshold 등)는 기존대로** — 아래는 그 검사에만 있는 필드용.

### 4단계
1. **전용 Recipe/Config 타입** (검사 전용 필드 보유):
   ```csharp
   [DataContract]
   public class EjectPinFinderRecipe : FinderAlgoRecipe   // 또는 AlgoRecipeBase
   {
       [DataMember] public double PinGap { get; set; }
       // 기본값 필요 시 [OnDeserializing] 으로 주입(initializer 는 역직렬화 때 안 돎)
   }
   ```
2. **모듈 등록** — `AddFinder/AddInspector` 의 TRecipe 를 전용 타입으로:
   ```csharp
   EjectPin = AddFinder<FinderAlgoSetup, FinderAlgoConfig, EjectPinFinderRecipe>("EjectPinFinder");
   ```
3. **② 그리드 칸** — 해당 타깃 페이지(`VisionTargetPage`/`InspectorTargetPage`) 의 `AppendNodeParams` 에 1줄(노드 구체 Recipe 캐스트, 저장=POCO):
   ```csharp
   private void AppendNodeParams(List<ParameterGridItem> items)
   {
       if (_node?.Recipe is EjectPinFinderRecipe r)
           items.Add(ParameterGridItem.Double("Pin Gap", "px", ParameterGridScope.Recipe,
                     () => r.PinGap, v => { r.PinGap = v; MarkDirty(); }));
   }
   ```
   → 이것만으로 **편집·저장·재로드 복원**(노드 BaseUnit Save/Load). ① 불필요.
4. **① 백엔드 반영(전용필드가 런타임에 실제 영향 줄 때만)** — 그 백엔드(finder/inspector)가 `IAlgoParamSync` 구현:
   ```csharp
   class EjectPinFinder : IPatternFinder, IAlgoParamSync
   {
       public void ApplyParams(IRecipeData recipe, IConfigData config, ISetupData setup)
       { if (recipe is EjectPinFinderRecipe r) _gap = r.PinGap; }   // POCO → 백엔드
       public void CollectParams(IRecipeData recipe, IConfigData config, ISetupData setup)
       { if (recipe is EjectPinFinderRecipe r) r.PinGap = _gap; }   // 백엔드 → POCO (런타임 변경 시)
   }
   ```
   → `AlgorithmNode`(Finder/InspectorAlgorithm)의 ApplyToRuntime/CollectFromRuntime 이 공통 sync 후 훅 호출. **미구현 백엔드 = no-op**(back-compat).

### 메커니즘 위치
- ① `QMC.Vision/Core/IAlgoParamSync.cs` + `AlgorithmNode.cs`(Finder/InspectorAlgorithm 훅 호출).
- ② `VisionTargetPage.AppendNodeParams` / `InspectorTargetPage.AppendNodeParams`(SetItems 직전).
- 증명: `cdt-320/redesign-r2d-test/PerAlgoParamSmoke.cs`(테스트 전용 타입으로 ①② end-to-end). **프로덕션엔 전용필드 0**(인프라만 — 실제 필드는 위 4단계로 추가).
