# P4 UI 연결 계획 — 그리드 ↔ 스토어 (Step 0, 보고 후 정지)

> 작성 2026-06-09. **Step 0: 현 바인딩 실태 + FromDescriptor 설계 — 보고 후 정지(컨펌).** 컨펌 후 Step 1~4.
> 기준 `docs/PARAMETER_REWORK_DESIGN.md`(§5). R2 재설계(2f8bcb4) 위. 브랜치 `param-rework-p4-ui`.

---

## §A 현 바인딩 실태 (코드)
- **VisionTargetPage.BuildParams** (`VisionTargetPage.cs:119-135`): ROI 8개를 `ParameterGridItem.Double(...,ParameterGridScope.Setup/Recipe,...)` **직접 생성**(scope 하드코딩), finder.SearchRoi/TrainRoi 직접 바인딩. `_params.ParameterValueChanged += MarkDirty`.
- **InspectorTargetPage.BuildParams** (`InspectorTargetPage.cs:120-133`): InspectionRoi 4개 직접 생성(Setup), `RefreshOverlay` 콜백 포함.
- **미노출(G5)**: AcceptThreshold/MaxInstances(finder Recipe), Cognex Threshold/MinDefectArea/MaxTotalDefectArea, ② 24필드 — 디스크립터엔 있으나 그리드 미표시.
- **저장**: 상단바 SAVE → `RecipePage.OnSaveRecipeClick` → `ITargetPage.SaveTarget` → finder/inspector.SaveParameters → P2 store.SaveTarget 위임. (이미 스토어 경유)
- **상태점**: R2e `MarkDirty`/`DirtyChanged` → 사이드바 상태점(`RecipePage.UpdateSettingDot`).
- **SettingsPage**: ② 편집 = P3 `InspectionEditorFactory`(NumericUpDown 에디터 5종), 카메라/조명 별도 패널.
- **ParameterGridItem**(`Ui\Controls\ParameterGridItem.cs`): Key/DisplayName/Unit/ValueType/Scope/Getter/Setter/Validator/Options + 팩토리. `ParameterGridScope{Recipe,Setup,Config}`.

## §B FromDescriptor 어댑터 설계 (Step 1)
`ParameterGridItem.cs` 에 정적 팩토리 추가(Ui→Core 참조 OK):
```csharp
public static ParameterGridItem FromDescriptor(ParameterDescriptor d, ParameterStore store)
{
    var it = new ParameterGridItem {
        Key = d.Target + "::" + d.Key,
        DisplayName = d.DisplayName, Unit = d.Unit,
        DisplayScale = d.DisplayScale,
        Scope = MapScope(d.Layer),          // Setup/Config/Recipe 1:1
        ValueType = MapType(d.Type),        // Enum→Selection, 그 외 동일
        Getter = d.Getter,
        Setter = v => store.SetValue(d.Target, d.Key, v),   // 편집→스토어(dirty 발생)
        Validator = d.Validator
    };
    if (d.Options != null) foreach (o in d.Options) it.Options.Add(new ParameterGridOption(o.Text, o.Value));
    return it;
}
```
- SCOPE = `descriptor.Layer`(하드코딩 제거). 편집은 `store.SetValue` 경유 → (layer,target) dirty → 상태점.
- 단위/스케일/Min·Max(Validator)/Enum 옵션 디스크립터에서 그대로.

## §C 페이지별 질의·노출 규칙 (Step 2·3)
| 페이지 | 그리드 입력 | SCOPE | 저장 | dirty |
|---|---|---|---|---|
| **VisionTargetPage**(finder) | `store.GetByTarget(finder.Id)` = Search ROI(Setup)+Train ROI·AcceptThreshold·MaxInstances(Recipe) | 계층 라벨 | 상단바→`store.SaveTarget` | SetValue→DirtyChanged→MarkDirty |
| **InspectorTargetPage**(inspector) | `store.GetByTarget(inspector.Id)` = InspectionRoi(Setup) + Cognex MinDefectArea/MaxTotalDefectArea(Recipe) + **②(주입 시 Bottom 24필드 incl Threshold)** | 계층 라벨 | 상단바→`store.SaveTarget` | 동일 |
| **SettingsPage** Vision/Global | `store.GetByTarget("Vision/Global")` = Scale(Setup)+포트·뷰어·로그(Config) | 계층 | `store.SaveLayer(Setup)`+`SaveLayer(Config)`(=vision.json 위임) | — |
| **SettingsPage** Camera/&lt;alg&gt; | `store.GetByTarget("Camera/<alg>")` = ExposureUs(Recipe)+HW(Config) | 계층 | `store.SaveTarget`(→algorithm_camera.json 위임) | — |
| **SettingsPage** 조명 | R2e InspectionLightPanel 유지(권장) 또는 그리드 흡수 | — | 기존 | — |

- **ROI(Setup) 는 타깃 페이지에 유지**(카메라 드래그 편집 UX 보존), SCOPE 라벨로 Recipe 와 구분(§5/Step2). `ParameterValueChanged → RefreshOverlay + MarkDirty` 유지(오버레이 갱신 보존).
- **G5 자동 노출**: GetByTarget 이므로 AcceptThreshold/Cognex 임계/② 가 계층 맞는 페이지에 자동 표시.

## §D 에디터 5종 흡수 (Step 3)
- ② 디스크립터가 그리드로 노출되면 `ParameterEditorBase` 5종(NumericUpDown) 불필요 → SettingsPage/ParameterEditorHost 에서 그리드로 대체.
- **흡수 위치 [확인 필요]**: ② Bottom 은 target=`BottomInspection/SurfaceInspector`(=inspector 타깃) → **RecipePage InspectorTargetPage 에 자동 노출**. 그러면 SettingsPage 의 ② 편집기는 제거. vs ② 를 SettingsPage 그리드(GetByTarget(②target))로 유지. → 도메인/UX 결정.
- 흡수 후 `InspectionEditorFactory`·`ParameterEditorBase` 5종 폐기(P3 정합).

## ★ 확정 요청 (컨펌)
1. **타깃 페이지 그리드 = `GetByTarget`(Setup ROI + Recipe + ② 전부, SCOPE 라벨)** 로 통일 OK? (G5/② 자동 노출, ROI 는 SCOPE 로 구분)
2. **② 편집 위치**: (가) inspector 타깃 페이지(RecipePage)에 자동 노출 + SettingsPage 편집기 제거 vs (나) SettingsPage 그리드 유지.
3. **조명/라이브튜닝**: R2e InspectionLightPanel 전용 유지(권장) vs 그리드 흡수.
4. **에디터 5종 폐기 시점**: 흡수 완료 후 즉시 제거 vs 1라운드 병존.
