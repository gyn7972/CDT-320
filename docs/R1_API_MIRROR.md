# R1 — Handler ↔ Vision API 미러 대조표

> 생성: 2026-06-08. R1: Vision 에 ParameterGrid 계열 + NumericKeypadDialog 재구현(결정 c).
> **public API 는 Handler 와 1:1 미러** — 향후 (a) QMC.Common 공용화 시 드롭인 교체 가능.
> Handler(`QMC.CDT_320.Ui.Controls`) **무수정**(읽기만). Vision 신규 namespace = `QMC.Vision.Ui.Controls`.

## 의존성 치환 (내부 구현만; public API 영향 없음)
| Handler 의존 | Vision 대체 | 사유 |
|---|---|---|
| `QMC.Common.Logging.EventLogger.Write(...)` | `System.Diagnostics.Debug.WriteLine(...)` | 컨트롤 self-contained 유지(WinForms만) → 공용화 용이 |
| `QMC.Common.MessageDialog.Show(owner,text,cap,btn,icon)` | `System.Windows.Forms.MessageBox.Show(owner,text,cap,btn,icon)` | 동일 시그니처/반환(DialogResult); Vision 기존 관행 |

## ParameterGridItem (모델) — 1:1
- enum `ParameterGridValueType { Double, Int, Bool, Text, Selection }`
- enum `ParameterGridScope { Recipe, Setup, Config }`
- class `ParameterGridItem` props: `Key, DisplayName, Unit, ValueType, Scope, DisplayScale, Getter:Func<object>, Setter:Action<object>, Validator:Func<object,bool>, Options:List<ParameterGridOption>`
- 정적 팩토리: `Micron(name,scope,Func<double>,Action<double>)`, `Double(name,unit,scope,...)`, `Int(...)`, `Bool(name,scope,Func<bool>,Action<bool>)`, `Selection(name,unit,scope,Func<object>,Action<object>,IEnumerable<ParameterGridOption>)`, `Selection<TEnum>(name,unit,scope,Func<TEnum>,Action<TEnum>)`
- class `ParameterGridOption { string Text; object Value; }` + ctor(text,value)

## ParameterGridChangedEventArgs — 1:1
- `: EventArgs`, props `Item:ParameterGridItem`, `Scope:ParameterGridScope`, ctor(`ParameterGridItem`).

## ParameterGridControl : UserControl — 1:1 public 표면
- ctor `ParameterGridControl()`
- `event EventHandler<ParameterGridChangedEventArgs> ParameterValueChanged`
- `event EventHandler<ParameterGridChangedEventArgs> ParameterRowDoubleClicked`
- `ParameterGridItem SelectedItem { get; }`
- `void SetItems(IEnumerable<ParameterGridItem> items)`
- `void RefreshValues()`
- 동작: 4열(PARAMETER|VALUE|UNIT|SCOPE) DataGridView, Bool 토글(확인 다이얼로그), Int/Double 더블클릭→NumericKeypadDialog, Selection 콤보셀 드롭다운, scope 색/value 색 동일.
- 동적 행 채움(SetItems/RebuildRows)은 **런타임**(Code.cs). Designer IC 엔 정적 grid+4컬럼만(헬퍼호출/객체초기화자/산술 0).

## NumericKeypadDialog : Form — 1:1 public 표면
- ctor `NumericKeypadDialog(string title, string valueText, string unit)`
- `string ValueText { get; }`
- 키패드(0-9, 000, ., +/-, BS, CLR, ±1/10/100/1000, OK, Cancel). ShowDialog→DialogResult.OK 시 ValueText 유효 숫자.

## EnumPickerDialog — **불요**
ParameterGridControl 의 Selection 편집은 **그리드 내 DataGridViewComboBoxCell 드롭다운**으로 처리(ShowSelectionEditor→grid.BeginEdit). Handler 도 EnumPickerDialog 미사용(별도 용도). → R1 신규 대상 아님.

## 신규 파일 (전부 QMC.Vision/Ui/Controls/)
- ParameterGridItem.cs (모델: enums + Item + Option)
- ParameterGridChangedEventArgs.cs
- ParameterGridControl.cs + .Designer.cs
- NumericKeypadDialog.cs + .Designer.cs

---

# R1 재검토 추가 (2026-06-08) — VisionRecipePage 미러 기준 + ui/tab + Step 2b

## §VisionRecipePage (Handler) — R2 미러 기준 구조
근거: `QMC.CDT-320/Ui/Pages/Recipe/VisionRecipePage.cs(159)` + `.Designer.cs`. **좌측 트리 없음 / 탭컨트롤 없음** — 단일 비전 레시피 페이지(타깃 1개). `PageBase` 상속, ctor `VisionRecipePage()` / `VisionRecipePage(string titleI18n)`.
- **레이아웃(전부 TableLayoutPanel)**: `rootLayout`(1열×2행) = `lblHeader` + `contentLayout`(3열: 좌|중|우).
  - **좌** `leftLayout`: `grpCamera`(cameraPanel + lblCameraInfo) / `grpMatchResult`(gridMatch DataGridView).
  - **중** `centerLayout`: `grpRoi`(thumbPanel/lblThumb + `roiOptionLayout`: 라디오 **rdoMain/rdoSub/rdoChip/rdoCross** + **rdoIndex1/2/4/8**) / `grpAction`(**actionCommandPanel = ActionCommandPanelControl**, 3×3 GRAB/MATCH/FAST SHUTTER/SMALL ROI/MATCH MOVE/IMAGE SAVE/THETA MATCH MOVE).
  - **우** `rightLayout`: `grpScale`(key|value Label 쌍 ScaleX/ScaleY/Pitch/Scale/Gray) / `grpJog`(ActionButton 조그패드 X/Y/T/Stop) / `grpSpeed`(TrackBar trkSpeed + lblSpeedValue).
- **검사/타깃 선택 방식**: 페이지 내부는 **라디오버튼**(ROI Main/Sub/Chip/Cross, Index 1/2/4/8)로 대상 전환. 5개 비전 타깃(Input/Output/Lower/Bottom/Side Vision)은 **각각 별도 VisionRecipePage 인스턴스**로 Handler 쉘 RecipeTab 사이드바가 선택(페이지 내부 아님).
- **데이터 흐름**: BindActionCommands→actionCommandPanel.SetItems(GRAB/MATCH/…), 액션 ExecuteAsync→VisionHub.Wafer.ExposeAsync/MatchAsync(TCP). 즉 **이미 Vision TCP(VisionHub) 호출** = Handler→Vision 통신. (Vision 자체 RecipePage 는 로컬 모듈 직접 호출.)
- 사용 커스텀 컨트롤: **ActionCommandPanelControl**(액션 그리드, async), **ActionButton**(조그). (R1 ParameterGridControl 은 VisionRecipePage 엔 직접 없음 — Subset/Stage 등 다른 레시피 페이지에서 사용.)

## §ui/tab (Handler `Ui/Tabs/`) — TabBase / RecipeTab
근거: `TabBase.cs(217)`, `RecipeTab.cs(46)`. **판정: 재사용 컨트롤 아님 → 쉘-결합 내비게이션 패턴.**
- TabBase: 하단 6개 탭 공통 껍데기(우측 사이드바 + 중앙 콘텐츠 호스트). API `AddSidebarButton`/`RegisterSidebarButton(SidebarButton, i18nKey, UserLevel?, Func<UserControl>)`/`SetSidebarHeader`/`ShowPage(key)`/`TryShowPage`. 사이드바버튼→lazy 페이지팩토리→`PnlContent` 콘텐츠 스왑 + PageCache.
- RecipeTab: TabBase 상속, ctor 에서 `RegisterSidebarButton` 24개(Project/InputCassette/…/InputVision/OutputVision/LowerVision/BottomVision/SideVision/…) 등록. 비전 타깃은 `() => new VisionRecipePage("recipe.xxxVision")`.
- **의존성(쉘 결합)**: `Form1 Host`, `QMC.CDT_320.Ui.Security`(UserLevel/AccessControl/UserSession), `Localization.Lang`(T/Apply), `UiTheme`, `UiClickAuditor`. → Vision 으로 그대로 미러 불가(보안/로컬라이제이션/Host 결합). 원자 재사용 조각은 `SidebarButton`(토글버튼, UiTheme 경결합)뿐.

## §Step 2b 판정 — 검사 선택 탭 컨트롤 = **패턴 (R1 미생성, R2 인라인 적용)**
- Handler 의 "검사/페이지 선택"은 **TabBase 사이드바버튼 + 콘텐츠 스왑 패턴**(쉘 결합)이지 독립 드롭인 컨트롤이 아님. 따라서 프롬프트 규칙("단순 레이아웃 패턴이면 R1 보고만, R2 적용")대로 **R1 에서 탭 컨트롤 신규 생성 안 함**.
- **R2 권고**: Vision RecipePage 의 좌측 트리 제거 후, **인라인 사이드바-버튼 스트립 패턴**(TLP 베이스, 버튼별 모듈/검사 선택→`_content` 스왑)로 재구성. 필요 시 `SidebarButton`(Handler) 을 Vision 에 경량 미러(UiTheme 의존만; Vision UiTheme 동일값 보유) — **R2 시점에 결정**. 또는 VisionRecipePage 식 단일페이지+라디오 방식.
- ⚠ 컨벤션(2026-06-08): 레이아웃 베이스 **TableLayoutPanel**(GroupBox 단독 아님), RowStyles/ColumnStyles 는 IC 내 선언적(헬퍼·반복문 금지), 동적행 런타임 BuildXxx, **마우스 우선**, 긴 영역 스크롤.

## R1 최종 산출 (재검토 반영)
- 신규 컨트롤 3종(ParameterGridItem/Control + NumericKeypadDialog) — 완료(Stage 101-103).
- **검사 선택 탭 컨트롤: 신규 생성 안 함**(패턴 판정 → R2 인라인). EnumPickerDialog: 불요.
- R2 미러 기준 = 본 문서 §VisionRecipePage + §ui/tab.
