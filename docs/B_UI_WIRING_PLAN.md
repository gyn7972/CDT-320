# B Part 2 — UI 배선 계획 (그리드/버튼 → BaseUnit API) — Step 0 (보고 후 정지)

> 작성 2026-06-09. **Step 0: 현 UI 실태 + 배선안 — 보고 후 정지(컨펌).** 컨펌 후 Step 1~3.
> 브랜치 `refactor-baseunit-vision`(A 연결 위). Part 1(Threshold int) 적용 완료.

---

## §A 현 UI 실태 (코드)
- **VisionTargetPage** `(IVisionModule _module, IPatternFinder _finder)` 보유(`VisionTargetPage.cs:25,44`).
  - 그리드: `BuildParams()` = `store.GetByTarget(_finder.Id)`(P4) → **ParameterStoreHost.Current=null → 빈 그리드**.
  - 저장: `SaveTarget()` = `_finder.SaveParameters(path)`(`:164`) → **스토어 위임 no-op**.
  - 로드: ctor `LoadTarget()` = `_finder.LoadParameters(path)` → no-op.
  - dirty/상태점: `MarkDirty`/`DirtyChanged`(R2e) → RecipePage 사이드바 점(`RecipePage.cs:162`).
- **InspectorTargetPage**: 동형(`_inspector`, InspectionRoi).
- **RecipePage**: `ShowSetting`이 `new VisionTargetPage(s.Module, s.Finder)`(`:161`)/`InspectorTargetPage`(`:167`). 상단바 SAVE → `tp.SaveTarget()`(`:237`). **레시피명 선택기 없음**(단일 product).
- **SettingsPage**: `ShowInspectionEditor` = `InspectionParamRegistry`+`store.GetByTarget`(P4, 구 ParameterStore) → 현재 빈 결과. ② 24필드 등 풍부한 파라미터는 **새 구조 모델(FinderAlgo*/InspectorAlgo*)에 없음**(리워크가 단순화).

## §B 배선안 (확정 제안)

### B-1. 그리드 채움 = **node POCO 가 아니라 실 finder/inspector 직접 바인딩** (P4 store 방식 → pre-P4 직접 방식으로)
근거: A(Apply/Collect)가 node POCO↔런타임을 동기화하므로, **그리드는 런타임 finder 속성을 직접 편집**하고 저장 시 node 가 Collect 한다(가장 단순, 추상화 재도입 없음 — 프롬프트 권장).
- VisionTargetPage.BuildParams (직접 `ParameterGridItem`):
  - Search X/Y/W/H → `f.SearchRoi.*` (Recipe)
  - Train X/Y/W/H → `f.TrainRoi.*` (Recipe)
  - Accept Threshold → `f.AcceptThreshold` (Recipe)
  - Max Instances → `f.MaxInstances` (Config)
  - 편집 setter 에서 `RefreshOverlay()`(ROI)·`MarkDirty()`.
- InspectorTargetPage.BuildParams:
  - Inspect X/Y/W/H → `i.InspectionRoi.*` (Setup? → **Recipe**, A 매핑 따라 InspectionRoi=Recipe)
  - Threshold → `(i is CognexInspector cog) cog.Threshold` (Recipe, Cognex 한정; 미지원 백엔드는 행 생략)
- → P4 `FromDescriptor`/`store.GetByTarget` 호출 제거. (이전 R2e 의 직접 BuildParams 로 복귀 + MaxInstances/Threshold 추가.)

### B-2. 버튼 = BaseUnit node API
- 노드 해석: `var node = _module.Algorithms.FirstOrDefault(a => a.Finder == _finder)` (inspector 는 `a.Inspector == _inspector`). (bareId 불필요.)
- **SaveTarget()**: `node.SaveSettings(); node.SaveRecipe(RECIPE_NAME);` → Collect(런타임→POCO) + 파일. dirty clear + DirtyChanged.
- **LoadTarget()**(ctor): `node.LoadSettings(); node.LoadRecipe(RECIPE_NAME);` → Apply(POCO→런타임) → `_params.RefreshValues()` + `RefreshOverlay()`.
- 상단바 SAVE(RecipePage.OnSaveRecipeClick) → 그대로 `tp.SaveTarget()` 경유.
- **삭제**(옵션): `node.DeleteRecipe(RECIPE_NAME)`/`DeleteSettings()` — UI 노출은 후속(이번 범위 밖, 메서드만 가용).

### B-3. 레시피명
선택기 없음 → **상수 `RECIPE_NAME = "default"`**(단일 product). 제품 선택기는 후속. 파일 = `Recipes/default/<모듈.알고>.recipe.json`.

### B-4. 페이지↔계층
- RecipePage 타깃 페이지 = Recipe(+ Config 의 MaxInstances 도 같은 그리드에 SCOPE 라벨로). 저장=node.SaveRecipe+SaveSettings.
- SettingsPage = Setup·Config — **[확인 필요] 아래 §C**.

## §C ★ 확인 필요
1. **SettingsPage ② 에디터**: 새 구조엔 ② 24필드 모델이 없음(FinderAlgo*/InspectorAlgo* 로 단순화). 현 ShowInspectionEditor(구 ParameterStore)는 빈 결과.
   - (가) **이번엔 손대지 않고 다음 단계(구 ParameterStore 삭제)에서 함께 정리**(권장) — 지금은 빈 화면이라 무해, 모듈/알고 Setup·Config UI 는 별도 설계.
   - (나) 지금 SettingsPage 에 모듈 Config(Camera 등)+알고 Config(MaxInstances/Enable) 그리드 신설.
2. **레시피명**: 상수 "default" OK? (제품 선택기 후속)
3. **그리드 편집→런타임 직접**(B-1) vs node POCO 직접 편집 중 — **런타임 직접(권장, A가 동기화)** 확정?

## 게이트(Step 3)
빌드0(Common→Vision)/정적(ic-helper-call)0/VS디자이너(R2)/실동작 왕복(그리드 편집→저장→재시작/재로드→복원+Match/Inspect 반영)/verify코어FAIL0/R2 레이아웃 보존.
