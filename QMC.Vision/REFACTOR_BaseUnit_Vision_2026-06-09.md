# QMC.Vision 비전 모듈 BaseUnit 일원화 리팩터링 — 작업 내역 (코드 검토용)

> 대상: `QMC.Vision`(WinForms, .NET Framework 4.8) + `QMC.Common`
> 목표: 비전 모듈/알고리즘의 **Setup·Config·Recipe 저장·불러오기·수정·삭제**를 `BaseUnit` Composite 구조로 **일원화**. 기존 `ParameterStore`(IParameterProvider/Snapshot) 시스템은 사용하지 않음.

---

## 1. 배경 / 해결한 컴파일 에러

- 최초 증상: `'BaseUnit<...>.BaseUnit(string)'의 필수 매개 변수 'name'에 해당하는 인수가 없습니다.`
  - 원인: `VisionModule` 생성자가 `: base(name)` 을 호출하지 않음 + `BaseEquipmentNode.Name` 을 `new` 로 가리는 중복 `Name` 프로퍼티.
- 요구사항(확정):
  1. 비전 모듈은 **모듈별로 distinct 한** Setup/Config/Recipe 타입을 가진다(제네릭).
  2. 모듈 안의 **알고리즘(Finder/Inspector)별로** Setup/Config/Recipe 를 관리한다(저장/불러오기/수정/삭제).
  3. **BaseUnit 구조로만** 관리. `ParameterStore` 미사용.

---

## 2. 아키텍처

```
BaseEquipmentNode                                   (QMC.Common, 추상; Name/StorageKey/Setup/Config/Recipe + Save/Load/Delete virtual)
  └ BaseUnit<TSetup,TConfig,TRecipe>                (Composite: 자신 + Components[] 연쇄)
       ├ VisionModule<TSetup,TConfig,TRecipe>       (추상, IVisionModule 구현; Camera/Backend/Finders/Inspectors/Grab/...)
       │     └ Components = [ AlgorithmNode, ... ]  (각 Finder/Inspector = 자식 노드)
       │     └ WaferVisionModule : VisionModule<WaferVisionSetup, WaferVisionConfig, WaferVisionRecipe>   (외 4개 모듈)
       └ AlgorithmNode<TSetup,TConfig,TRecipe>      (추상, IAlgorithmNode 구현)
             ├ FinderAlgorithm<...>                 (IPatternFinder 래핑)
             └ InspectorAlgorithm<...>              (IInspector 래핑)
```

- **비제네릭 표면 인터페이스** `IVisionModule` 를 도입하여, 컬렉션/소비자(`Form1`, `VisionTcpServer`, UI Page 등)는 제네릭 타입 인자 없이 모듈을 참조한다.
- 각 알고리즘은 모듈의 `Components` 에 자식 노드로 등록 → 모듈 1회 호출로 Save/Load/Delete 가 전체 연쇄.

### 다형성 직렬화를 쓰지 않은 이유 (중요)
`JsonDataStore.CreateSettings()` 가 `EmitTypeInformation = EmitTypeInformation.Never` 이므로 `__type` 힌트가 기록되지 않는다. 따라서 base 타입 1개로 파생 타입을 저장/복원하는 다형성 방식은 파생 필드가 유실된다. 그래서 **모듈/알고리즘마다 구체 타입을 그대로 제네릭 인자로 사용**(typeof(구체타입) 직렬화)하는 방식을 택했다. 공유 필드는 base 클래스로 묶되, 직렬화는 항상 구체 타입으로만 일어나므로 안전하다.

---

## 3. 변경/추가 파일

### QMC.Common (기존 파일 편집 — 삭제 cascade API 추가)
| 파일 | 변경 |
|---|---|
| `Data/Store/JsonDataStore.cs` | `DeleteFile(path)` 추가 |
| `Data/Store/EquipmentDataStore.cs` | `Delete(storageKey, category)` 추가 |
| `Data/Store/RecipeDataStore.cs` | `DeleteNode(recipeName, storageKey)` 추가 (단일 노드 파일 삭제; 디렉토리 유지) |
| `Data/Store/UnitDataStore.cs` | `DeleteSetup/DeleteConfig/DeleteRecipe` 추가 |
| `BaseEquipmentNode.cs` | `virtual bool DeleteSettings()`, `virtual bool DeleteRecipe(string)` 추가 |
| `BaseUnit.cs` | 위 두 메서드 override — 자신 삭제 후 `Components` 연쇄 |

### QMC.Vision (신규)
| 파일 | 내용 |
|---|---|
| `Modules/IVisionModule.cs` | 비제네릭 표면 인터페이스 `IVisionModule`, 알고리즘 표면 `IAlgorithmNode` |
| `Modules/AlgorithmNode.cs` | `AlgorithmNode<…>` + `FinderAlgorithm<…>` + `InspectorAlgorithm<…>` |
| `Modules/AlgorithmData.cs` | `FinderAlgo{Setup,Config,Recipe}`, `InspectorAlgo{Setup,Config,Recipe}` |
| `Modules/VisionModuleData.cs` | 모듈 공통 base + 5개 모듈 distinct Setup/Config/Recipe |

### QMC.Vision (재작성/수정)
| 파일 | 변경 |
|---|---|
| `Modules/VisionModule.cs` | `VisionModule<TSetup,TConfig,TRecipe> : BaseUnit<…>, IVisionModule` 로 제네릭화. `: base(name)` 수정, 중복 `Name` 제거, 알고리즘 등록(`AddFinder<…>`/`AddInspector<…>`/`GetAlgorithm`) 추가 |
| `Modules/{Wafer,Bin,BottomInspection,FrontSide,RearSide}*Module.cs` | 새 제네릭 베이스 상속 + 알고리즘을 타입 지정해 등록 |
| `Form1.cs` | 모듈 참조 `VisionModule`→`IVisionModule`, `ParameterStoreBootstrap.Build(...)` 호출 제거 → 기동 시 `module.LoadSettings()` 호출로 대체 |
| `Comm/VisionTcpServer.cs`, `Ui/Pages/FinderPage.cs`, `InspectorPage.cs`, `InspectorTargetPage.cs`, `VisionTargetPage.cs`, `RecipePage.cs` | 모듈 참조 타입 `VisionModule`→`IVisionModule` (단어경계 치환; `WaferVisionModule` 등 구체 타입명은 보존) |
| `Config/ParameterStoreBootstrap.cs` | 시그니처만 `IVisionModule` 로 맞춰 컴파일 유지(런타임 미호출, dormant) |
| `QMC.Vision.csproj` | 신규 4개 `<Compile Include>` 등록 |

> 참고: `Backends/Cognex/CognexPatternFinder.cs` 는 본 리팩터링과 무관하나 작업트리에 손상(잘림/NUL)이 있어 HEAD 기준으로 정리(내용 동일).

---

## 4. 영속화 스킴

각 알고리즘 `StorageKey = "{모듈키}.{알고리즘Id}"` (예: `WaferVision.EjectPinFinder`) → 파일이 모듈/알고리즘별로 분리된다.

```
EquipmentData/Setup/WaferVision.json                      (모듈 Setup)
EquipmentData/Config/WaferVision.json                     (모듈 Config)
EquipmentData/Setup/WaferVision.EjectPinFinder.json       (알고리즘 Setup)
EquipmentData/Config/WaferVision.EjectPinFinder.json      (알고리즘 Config)
Recipes/{recipeName}/WaferVision.recipe.json              (모듈 Recipe)
Recipes/{recipeName}/WaferVision.EjectPinFinder.recipe.json (알고리즘 Recipe)
```

### API (모듈 1회 호출 → 알고리즘 cascade)
| 동작 | 호출 |
|---|---|
| 저장(Setup+Config) | `module.SaveSettings()` |
| 불러오기(Setup+Config) | `module.LoadSettings()` |
| 저장(Recipe) | `module.SaveRecipe(recipeName)` |
| 불러오기(Recipe) | `module.LoadRecipe(recipeName)` |
| 삭제(Setup+Config) | `module.DeleteSettings()` |
| 삭제(Recipe) | `module.DeleteRecipe(recipeName)` |
| 알고리즘 단독 제어 | `var node = module.GetAlgorithm("EjectPinFinder"); node.SaveRecipe(name); node.DeleteSettings(); ...` |

### 수정(Edit) 패턴
`BaseUnit.Setup/Config/Recipe` 의 setter 가 `private set` 이므로 **객체 통째 교체 불가**. 내부 필드만 변경 후 저장:
```csharp
var node = module.GetAlgorithm("EjectPinFinder");
((FinderAlgoRecipe)((IAlgorithmNode)node).Recipe).AcceptThreshold = 0.8;
node.SaveRecipe("ProdA");
```

---

## 5. 데이터 타입 요약

- 모듈 공통 base: `VisionModuleSetupBase`(IsSimulationMode), `VisionModuleConfigBase`(CameraId/Gain/FrameRate/Trigger/Pixel/Delay/ROI), `VisionModuleRecipeBase`(Exposure). `[OnDeserializing]` 로 기본값 보강(DataContractJsonSerializer 는 이니셜라이저 미실행).
- 모듈별 distinct: `Wafer/Bin/BottomInspection/FrontSide/RearSide` × {Setup,Config,Recipe} (base 상속, 일부 고유 필드).
- 알고리즘 재사용: `FinderAlgo{Setup(TrainModelPath,TrainRoi), Config(MaxInstances,AngleEnabled), Recipe(AcceptThreshold,SearchRoi)}`, `InspectorAlgo{Setup(CalibModelPath), Config(Enable), Recipe(InspectionRoi,Threshold)}`. `Roi` 는 기존 `[DataContract]` 타입 재사용.

---

## 6. 코드 검토 포인트 (리뷰어 확인 권장)

1. **인터페이스 명시적 구현**: `IVisionModule.Finders/Inspectors` 는 `IReadOnlyDictionary` 로 명시적 구현, 클래스에는 `Dictionary` public 유지 — 소비자가 `IVisionModule` 로 받을 때/구체 타입으로 받을 때 모두 의도대로인지.
2. **`AlgorithmNode` 의 `ISetupData/IConfigData/IRecipeData` 명시적 구현**: `=> Setup` 이 `BaseUnit.Setup`(TSetup, `private set`) 의 get 으로 해석되는지(.NET 4.8 / C# 7.3 에 공변 반환 없음 → 명시적 구현으로 처리함).
3. **생성자 순서**: base(BaseUnit) 생성자가 `Components` 초기화 → 파생 모듈 생성자 본문에서 `AddFinder<…>` 가 `Components.Add` 호출. 순서상 안전한지.
4. **`Components.Add((BaseEquipmentNode)node)`** 캐스팅: 인터페이스→클래스 명시적 캐스팅 유효성.
5. **Composite 삭제 의미론**: `DeleteSettings/DeleteRecipe` 가 모듈+모든 알고리즘 파일을 지우는 것이 의도와 맞는지(전체 vs 단일).
6. **ParameterStore 제거 범위**: 런타임 호출만 제거(dormant). UI 일부 페이지가 여전히 `ParameterStore` 를 참조(컴파일은 됨) — 8번 참조.
7. **모듈별 distinct 스키마**의 적정성: 현재 5개 모듈이 대부분 base 필드를 공유하고 일부만 고유. 실제로 필요한 모듈별 고유 필드 추가 여지.
8. **빌드 무관 이슈**: `System.IO.Ports` 참조 미해결 경고는 HEAD 부터 존재(프레임워크 타게팅 팩 문제), 본 작업과 무관.

---

## 7. 알려진 후속 작업 (미완)

- **UI 핸들러 연결**: `FinderPage`/`InspectorPage`/`RecipePage` 등의 저장/불러오기 버튼은 과거 `ParameterStore` 로 읽고썼다. 타입만 `IVisionModule` 로 맞춰 컴파일은 되지만, 버튼 본문을 새 `GetAlgorithm(...)` + `node.SaveRecipe/SaveSettings/Delete...` 로 연결해야 실제로 새 구조에 반영된다. (페이지별 진행 권장)
- **모듈 단위 vs 알고리즘 단위 삭제 정책** UI 노출 방식 결정.
- `AddFinderRaw/AddInspectorRaw`(미사용 protected) 정리 여부.

---

## 8. 빌드 메모

- 빌드 순서: `QMC.Common` → `QMC.Vision`.
- 신규 4개 파일 csproj 등록 완료.
- 본 워크스페이스 저장 과정에서 일부 파일이 잘림/NUL 손상되었던 이력이 있어 전부 git HEAD 대조로 복구함(내용 손실 없음 확인). 검토 시 `git diff` 로 의도 변경만 있는지 교차 확인 권장.
