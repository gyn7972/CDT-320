# ①② per-algorithm 파라미터 확장 인프라 (Step 0 설계, 보고 후 정지)

> 작성 2026-06-11. 브랜치 `refactor-baseunit-peralgo-param`(master=C3b-3 머지본 `9af9efb`/버그수정 `b66ff98` 위).
> **Step 0: 설계 — 보고 후 정지.** 확인 = ①훅 형태 ②확장점 형태. 컨펌 후 Step 1~4.
> 목표: 검사별 전용 파라미터를 쉽게 추가하는 재사용 메커니즘 2개. **프로덕션에 가짜 필드 0**(인프라만), 테스트로 end-to-end 증명 + 추가법 문서화.

---

## §A 현재 구조 (확인)
- `AlgorithmNode<TSetup,TConfig,TRecipe>`(`AlgorithmNode.cs:36-43`): `ApplyToRuntime`/`CollectFromRuntime` virtual + Load/Save override 에서 호출.
- `FinderAlgorithm.ApplyToRuntime`(`:64`): `Recipe is FinderAlgoRecipe r` 캐스트 → `_finder`(IPatternFinder) sync(SearchRoi/TrainRoi/AcceptThreshold + Config.MaxInstances). `CollectFromRuntime` 대칭. `_finder` 는 FinderAlgorithm private.
- `InspectorAlgorithm.ApplyToRuntime`(`:109`): `Recipe is InspectorAlgoRecipe r` → `_inspector.InspectionRoi` + `if (_inspector is CognexInspector cog) cog.Threshold=r.Threshold`(백엔드한정 선례).
- 그리드(`VisionTargetPage.BuildParams`/`InspectorTargetPage.BuildParams`): `ParameterGridItem.Double/Int(name,unit,scope,getter,setter)` 로 items 구성 → 런타임 `_finder`/`_inspector` 속성 바인딩 → `_params.SetItems(items)`. InspectorTargetPage 는 `if (_inspector is CognexInspector cog) items.Add(...)` per-backend 분기 보유.
- **핵심 한계**: 공통 Apply/Collect 가 부모타입(FinderAlgoRecipe 등) 캐스트 → 파생 전용필드 못 봄. 그리드도 런타임 백엔드 바인딩이라 POCO-only 전용필드 칸 없음.

## §B 설계

### ② 그리드 POCO 바인딩 확장점 — [확인②]
- `BuildParams` 말미(`_params.SetItems` 직전)에 **`AppendNodeParams(items)`** 호출. 이 메서드가 `_node` 의 **구체 Recipe/Config** 캐스트로 전용필드 칸 추가:
  ```
  private void AppendNodeParams(List<ParameterGridItem> items)
  {
      // (인프라: 프로덕션 케이스 0 — 현 동작 불변. 전용필드 추가 시 아래 패턴 1줄)
      // if (_node?.Recipe is EjectPinFinderRecipe r)
      //     items.Add(ParameterGridItem.Double("Pin Gap","px",ParameterGridScope.Recipe,
      //               () => r.PinGap, v => { r.PinGap = v; MarkDirty(); }));
  }
  ```
- 저장은 **POCO**(노드 Recipe/Config) 라 ① 없이도 편집·저장·재로드 복원(노드 SaveRecipe/LoadRecipe). MarkDirty 로 상태점 전파.
- 형태 [확인②]: (가) **각 타깃 페이지에 private `AppendNodeParams(items)` 타입스위치** [권장 — InspectorTargetPage 의 CognexInspector 분기와 동형, 페이지가 그 검사군 소유] vs (나) 노드/베이스에 가상 메서드(페이지-노드 결합↑).

### ① IAlgoParamSync 훅 — [확인①]
- 전용필드가 **런타임 백엔드에 실제 반영**돼야 할 때만(공용 Apply 가 파생필드 못 봄). 백엔드(finder/inspector)가 **선택 구현**:
  ```
  // QMC.Vision/Core/IAlgoParamSync.cs
  public interface IAlgoParamSync
  {
      void ApplyParams(IRecipeData recipe, IConfigData config, ISetupData setup);   // POCO → 백엔드
      void CollectParams(IRecipeData recipe, IConfigData config, ISetupData setup); // 백엔드 → POCO (대칭)
  }
  ```
- `FinderAlgorithm.ApplyToRuntime` 말미(공통 sync 후): `if (_finder is IAlgoParamSync s) s.ApplyParams(Recipe, Config, Setup);`. `CollectFromRuntime` 말미: `if (_finder is IAlgoParamSync s) s.CollectParams(Recipe, Config, Setup);`. InspectorAlgorithm 동형(`_inspector`).
- 백엔드가 자기 구체 Recipe 캐스트해 전용필드 읽기/쓰기. **미구현 백엔드 = no-op**(back-compat, 현 동작 불변).
- 시그니처 [확인①]: (가) **typed `IRecipeData/IConfigData/ISetupData`** [권장 — 노드가 그대로 전달, 발견성↑] vs (나) `object`(결합↓). Collect 대칭: (가) **ApplyParams+CollectParams 둘 다** [권장 — 런타임 변경 백엔드 대비] vs (나) ApplyParams만.

### per-algorithm 전용필드 추가 4단계 (문서화 대상)
1. `class XxxRecipe : AlgoRecipeBase { [DataMember] public double Field; }` (또는 FinderAlgoRecipe 상속).
2. 모듈 등록 `AddFinder<FinderAlgoSetup, FinderAlgoConfig, XxxRecipe>("XxxId")`.
3. ② 그리드 칸: 해당 타깃 페이지 `AppendNodeParams` 에 `if (_node.Recipe is XxxRecipe r) items.Add(...)` 1줄.
4. (백엔드 소비 시만) ① 훅: 그 백엔드가 `IAlgoParamSync` 구현해 `ApplyParams` 에서 `(XxxRecipe)recipe` 읽음.

## §C 증명 (프로덕션 오염 0)
`cdt-320/redesign-r2d-test/PerAlgoParamSmoke`:
- 테스트 전용 `TestFinderRecipe : AlgoRecipeBase { TestField }` + `TestSyncFinder : IPatternFinder, IAlgoParamSync`(ApplyParams 에서 (TestFinderRecipe)recipe.TestField 수신).
- ② : `ParameterGridItem.Double` 로 TestField 바인딩 → setter 호출(편집) → node.SaveRecipe → 재로드 → 복원 확인.
- ① : `FinderAlgorithm<…,TestFinderRecipe>(key, TestSyncFinder)` → LoadRecipe → ApplyToRuntime → 훅 호출 → TestSyncFinder 가 TestField 수신 확인. (미구현 백엔드 no-op 도 확인.)

## 게이트 (Step 4)
전체 빌드0(Common·Handler·Vision) / 정적 ic-helper-call0 / verify 코어 FAIL0 / PerAlgoParamSmoke PASS / 기존 스모크(CameraSsot·C2Light·PanelLight·AssignPanel) ALL PASS / R2 2/2 / **현 동작 불변(프로덕션 필드·UI 변화 0)** / Handler 무수정 / push 안 함.

## ★ 확인 필요 (2건)
1. **① IAlgoParamSync 형태**: (가) typed(IRecipeData/IConfigData/ISetupData) [권장] vs object. + ApplyParams+CollectParams 대칭 [권장] vs Apply만. 위치 QMC.Vision/Core.
2. **② 확장점 형태**: (가) 각 타깃 페이지 private `AppendNodeParams(items)` 타입스위치(SetItems 직전) [권장, CognexInspector 분기 동형] vs (나) 노드 가상 메서드.

## 정지 조건
확장점/훅이 현 Apply·BuildParams 깸 / 프로덕션 동작·UI 변화(인프라 초과) / 빌드·verify 회귀 / R2 깨짐 → 즉시 정지·보고. **Step 0 보고 전 구현 금지.**
