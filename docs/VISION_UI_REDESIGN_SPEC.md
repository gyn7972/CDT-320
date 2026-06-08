# Vision UI 재설계 분석/설계 — Recipe·Settings (Handler 레시피 UI 기준)

> 생성: 2026-06-08. **읽기 전용 정찰 산출물 — 코드 미수정.** 구현 전 설계 근거.
> 범위: Vision RecipePage·SettingsPage 를 Handler 레시피 UI 형식(레이아웃+컴포넌트)으로 재설계. Handler 구조·UX·테마 채택 + Handler `Ui/Controls` 최대 재사용 + **기능 동작 보존** + **디자이너 로드 가능 규칙 준수**.
> 근거 파일: 코드 직접 확인(추측 없음). 어셈블리 관계는 csproj 확인.

---

## ★ 핵심 결론 (먼저)
1. **참조 불가 확정**: Vision.csproj 는 **QMC.Common 만** ProjectReference. Handler(QMC.CDT-320)는 **WinExe** 이고 Handler 컨트롤은 전부 `namespace QMC.CDT_320.Ui.Controls`(exe 내부). QMC.Common 에는 UI 컨트롤/테마가 **없음**. → **Vision 은 Handler 컨트롤을 그대로 참조 못 함.** 재사용은 (a)Common 이동 or (c)디자인 차용만.
2. **테마는 이미 정렬됨**: Vision `Ui/UiTheme.cs` 색/폰트가 Handler `Ui/UiTheme.cs` 와 **동일 값**(StatusBarBg #D97706, Accent #E85D1A, HeaderBg #2D2D30, Sidebar #595959, Malgun Gothic/Consolas). Vision 테마는 Handler 의 부분집합 → 디자인 차용 마찰 매우 낮음. (Handler 가 더 풍부: OptionHeaderBg/DotVision/TitleFont 등 추가)
3. **crown jewel = ParameterGridControl + ParameterGridItem**: 타입형(bool/int/double/selection/text) 파라미터 그리드 + scope 색. **self-contained**(deps: WinForms + QMC.Common.Logging 만, Equipment/Sequencing 참조 0). 단 Handler exe 소속이라 공유하려면 Common 이동 필요.
4. **디자이너 준수 주의**: Handler `RecipeLayoutHelpers`(OrangeBar/LabelCell/ValueCell)는 **헬퍼 메서드** — IC 에서 호출하면 직전에 고친 `ic-helper-call` 위반 재발. 차용 시 **런타임 BuildXxx(Code.cs)** 또는 **IC 인라인**으로만.

---

## §A Handler 레시피 UI 디자인 언어
근거: `QMC.CDT-320/Ui/Pages/Recipe/Helper/RecipeLayoutHelpers.cs`, `SubsetPageBase.cs`, `InputCassetteRecipePage.*`, `StageRecipePage.*`, `Ui/Tabs/RecipeTab.cs`, `Ui/Tabs/SettingsTab.cs`, `Ui/UiTheme.cs`.

### 레이아웃 구조
- **루트**: `TableLayoutPanel` Dock=Fill, 1열×2행 (Row0 헤더 30px abs, Row1 콘텐츠 %). **Stage 61 — Dock=Top 적층 대신 TableLayoutPanel** 로 z-order 충돌 회피(중요 규약).
- **SubsetPageBase 헤더(66px)**: Row0(30) 주황 섹션바 + Row1(36) TopBar(프로젝트 라벨 Dock=Fill | Reload Dock=Right 120 | SAVE Dock=Right 180, SAVE=Accent 주황).
- **3열 콘텐츠**: 좌(고정 296~420 — ACTION/WAIT/OPTIONS/IO GroupBox 스택) | 중(가변 % — 파라미터 그리드) | 우(고정 — JOG/SPEED/VISION GroupBox).
- **섹션 헤더(OrangeBar)**: H=26, BackColor=StatusBarBg(#D97706), Font=SectionFont(Malgun 11 Bold), Padding(10,0,0,0).
- **2-톤 key|value 행(LabelCell/ValueCell)**: 행높이 28~30 abs. LabelCell: bg #D0D0D0, Malgun 9, MiddleLeft, FixedSingle. ValueCell: bg White, Consolas 10, MiddleRight, FixedSingle.

### UX 흐름
- **네비**: Tab(RecipeTab) 의 사이드바 버튼(RegisterSidebarButton)→페이지 선택. 페이지는 `PageBase`/`SubsetPageBase` 상속.
- **생명주기**: OnLoad→LoadCurrentProject(RecipeStore.GetLastProjectName)→없는 subset 자동생성→BuildEditor(_editorPanel, AutoScroll)→DoSave(SafeSaveToRecipe + RecipeStore.Save + host.SaveMachineRecipe).
- **액션 배치**: 저장/Reload 는 상단 TopBar 우측. 이동 액션은 좌측 ACTION GroupBox 에 세로 스택(ActionButton 36H).

### 테마 (Handler UiTheme.cs 실제 값 — Vision 과 동일/상위)
- 색: HeaderBg #2D2D30, StatusBarBg/섹션 #D97706, Accent/액션 #E85D1A, MainBg #BFBFBF, OptionPanelBg #F0F0F0, Sidebar #595959, ValueBox White/Black, Dot{Vision #00BCD4/Pick #F5C718/Ref #BDBDBD/Off #555}.
- 폰트: SectionFont Malgun 11 Bold, ButtonFont Malgun 11, ValueFont Consolas 10, StatusBarFont Malgun 10 Bold, TitleFont Segoe UI Light 28.
- 레이아웃 상수(1920×1080): SidebarWidth 210, OptionWidth 340, HeaderHeight 70, StatusBarHeight 30, BottomBarHeight 80.

### 컨트롤-스타일 반복 수치
- 파라미터 행 30H / 라벨셀·값셀 28~30H FixedSingle / Jog 버튼 80×80 Malgun 18 / 액션버튼 36H Malgun 9 Bold / 섹션헤더 26H / NumericUpDown 160×28 Consolas / Combo·Text 200×28 / GroupBox Malgun 10 Bold + 간격 Margin(0,0,0,6).

### 레시피 페이지가 쓰는 커스텀 컨트롤
ParameterGridControl(파라미터 편집 주력), JogAxisMoveControl, JogPositionListControl, JogSpeedControl, IoCylinderPanelControl, IndicatorDot, ActionButton.

### 컨트롤 트리 스케치 (InputCassetteRecipePage)
```
TableLayoutPanel root (Fill, 1×2)
├ Label 헤더 (30, #2D2D30)
└ TableLayoutPanel content (Fill, 3×1: 296abs | * | 680abs)
  ├ 좌 TLP(1×2: 610abs|*) → GroupBox ACTION(ActionButton ×N 36H) / GroupBox IO(IoCylinderPanelControl)
  ├ 중 TLP(1×2: 610abs|*) → GroupBox OPTIONS(ParameterGridControl 16행) / GroupBox WAIT(ParameterGridControl 2행)
  └ 우 TLP(2×1: *|120abs) → GroupBox JOG(JogPositionListControl + JogAxisMoveControl) / GroupBox SPEED(JogSpeedControl)
```

---

## §B Handler Ui/Controls 재사용 인벤토리 + 참조 가능성
근거: `QMC.CDT-320/Ui/Controls/*.cs` (namespace 전부 `QMC.CDT_320.Ui.Controls` = Handler exe). **Equipment/Sequencing 참조 0** (전 컨트롤). 판정: (a)=Common 이동시 공유, (b)=포팅, (c)=디자인만 차용.

| 컨트롤 | 역할 | 의존 | 이식성 | Recipe/Settings 유용도 | 판정 |
|---|---|---|---|---|---|
| **ParameterGridControl** | 타입형 파라미터 DataGridView(bool/int/double/sel/text)+scope색 | WinForms+QMC.Common.Logging | self-contained | ★★★ 최우선(설정 편집 주력) | **(a)** Common 이동 권장 |
| **ParameterGridItem** | 행 데이터모델(Micron/Double/Int/Bool/Selection 팩토리) | primitives | self-contained | ★★★ (위 지원) | **(a)** |
| ParameterGridChangedEventArgs | 변경 이벤트 args | primitives | self-contained | ★★★ | **(a)** |
| **NumericKeypadDialog** | 숫자 키패드 모달 입력 | WinForms+Common.Logging/MessageDialog | self-contained | ★★ HW식 입력 UX | (a) |
| **ActionButton** | 페인팅 액션 버튼(hover/down) | WinForms/Drawing2D | self-contained | ★★ 액션행 | (a) |
| EnumPickerDialog | 옵션 선택 모달 | WinForms | self-contained | ★★ | (a) |
| IndicatorDot | LED 상태점 | WinForms/Drawing2D | self-contained | ★ 상태표시 | (a) |
| VerticalLabel | 세로 텍스트 | WinForms/Drawing2D | self-contained | ★ 섹션라벨 | (a) |
| MaterialDetailView | 읽기전용 상세 그리드 | WinForms | self-contained | ★ 요약표시 | (a) |
| ActionCommandItem | 액션 커맨드 모델 | Tasks | self-contained | ★ | (a) |
| ParamGrid | 읽기전용 key-value 표(경량) | +Common.Alarms | self-contained(경) | ★ | (a) |
| **SidebarButton** | 토글 사이드바 버튼 | +UiTheme | light-coupled | ★★ 다중섹션 네비 | (a)+테마처리 / (c) |
| UnitConfigGrid | 리플렉션 config 편집기 | +UiTheme+Localization+Common.Store | light-coupled | ★ config 다이얼로그 | (b)/(c) |
| ActionCommandPanelControl | async 액션 그리드 | +UserSession(보안) | security-coupled | ★ | (c) |

- **9/14 self-contained** → QMC.Common(신규 `QMC.Common.Ui` 폴더)으로 이동 시 Handler·Vision 양쪽 공유 가능.
- **light-coupled 3** = UiTheme/Localization 의존 → 테마는 양쪽 동일값이라 Common 에 공용 테마 두면 해소.
- **참조 가능성 종합**: 현재로선 **전부 직접 참조 불가**(Handler exe 소속). 공유하려면 **선행 PR: 자족 컨트롤 + 공용 테마를 QMC.Common 으로 이동**, Handler 는 새 위치 참조하도록 retarget.

---

## §C Vision 현재 상태 (RecipePage·SettingsPage)
근거: Vision `Ui/Pages/RecipePage.*`, `SettingsPage.*`, `Ui/UiTheme.cs`, `Form1.cs`.

### RecipePage (직전 디자이너 스윕으로 Designer/Code 분리됨)
- **레이아웃**: `_root` TableLayoutPanel 1열×3행 — Row0(30) `_hdr` 주황 헤더 / Row1(40) `_bar`(버튼 _btnSpc 180×32, _btnParams 220×32) / Row2(%) `_table` 2열(280abs `_tree` | % `_content`).
- **기능/동적**: OnLoad→PopulateTree(Form1 의 5모듈 WaferMod/BinMod/BottomMod/FrontSideMod/RearSideMod → Finder/Inspector 노드). 트리선택→`_content` 에 FinderPage/InspectorPage swap. 버튼→ShowSpc(SpcChartPage)/ShowParameterEditors(ParameterEditorHost). 모두 `_content.Controls.Clear()`+Dock.Fill.
- **보존 필수**: `public RecipePage()` 무인자, 외부주입 없음(Load 시 FindForm()=Form1 발견). `_content` 동적호스트 계약. ShowSpc()/ShowParameterEditors() + SpcChartPage/ParameterEditorHost(5 에디터). Form1.cs: `new RecipePage{Dock=Fill,Visible=false}`.
- **Handler 대비 격차**: Handler 식 OrangeBar 섹션/2-톤 셀/SAVE·Reload TopBar/GroupBox 구획 없음. 단순 헤더+버튼바+트리. → 차용 여지 큼.

### SettingsPage
- **레이아웃**: `_hdr`(Top 30 주황) + `_sidebar`(Left 230, `_sideHdr`+`_tree`) + `_detailHost`(Fill). SetChildIndex 로 detailHost 앞.
- **기능/동적**: ctor=IC+가드+BuildDetailPanels+LoadAlgorithms. 트리 3루트(■카메라매핑[alg→insp]/■검사알고리즘[5툴]/■시스템설정[조명]). `SwapEditor(Control)`: 영구패널(_camPanel/_inspTabs/_lightSetupPage) 가시토글 + 임시 검사에디터 생성·dispose. _inspTabs=TabControl(카메라:InspectionOverridePanel / 조명:InspectionLightPanel). OverrideChanged→트리 ● 마커 갱신.
- **보존 필수**: `public SettingsPage()` 무인자. `_detailHost`+`SwapEditor` 호스트 계약. **서브패널 계약**: UserControl+무인자ctor / CameraMappingPanel.SelectAlgorithm(string) / InspectionOverridePanel·InspectionLightPanel.SelectInspection(alg,insp) / InspectionOverridePanel.OverrideChanged 이벤트. Form1.cs: `new SettingsPage{Dock=Fill,Visible=false}`.
- **Handler 대비 격차**: 사이드바는 이미 Handler 식(어두운 #595959). 단 SidebarButton 스타일 미적용(기본 TreeView), 서브패널 내부는 직전 스윕의 평면 라벨/Num/Combo(2-톤 셀·ParameterGridControl 아님). → 셀 스타일·그리드 차용 여지.

### Vision UiTheme (Handler 와 동일값 — 비교)
StatusBarBg #D97706 / Accent #E85D1A / HeaderBg #2D2D30 / MainBg #BFBFBF / OptionPanelBg #F0F0F0 / Sidebar #595959 / SidebarBtnSel White↔#222 / SectionFont Malgun11Bold / ButtonFont Malgun11 / ValueFont Consolas10. **Handler 에만 있는 것**(차용 시 Vision 에 추가 후보): OptionHeaderBg/Fg, ValueBoxBg/Fg, Dot*, OptionWidth, RecipeLayout 셀 색 #D0D0D0.

---

## §D 매핑: Handler → Vision (영역별) + 기능보존 + 디자이너 준수
| 영역 | Handler 패턴 | → Vision 적용 | 재사용 | 디자이너 위험 |
|---|---|---|---|---|
| 페이지 골격 | TableLayoutPanel 루트(Stage61 z-order) | RecipePage/SettingsPage 이미 TLP — 유지·정돈 | 디자인 | 낮음(정적 TLP=IC 직렬화 가능) |
| 섹션 헤더 | OrangeBar(헬퍼) #D97706 26H | 정적 Label(주황) IC 인라인 | (c) | **헬퍼호출 금지** → IC 인라인 or Code |
| key\|value 행 | LabelCell/ValueCell 2-톤(헬퍼) | 서브패널 입력행 2-톤 재스타일 | (c) | 헬퍼 인라인 필수 |
| 파라미터 편집 | ParameterGridControl(타입형) | 검사 에디터 5종 + InspectionOverride 를 그리드화 | **(a) Common 이동** or (c) 재구현 | 동적 SetItems=Code(IC 아님)→안전 |
| 저장/Reload | TopBar SAVE/Reload | RecipePage `_bar` 에 SAVE/Reload 추가(기능 보존) | 디자인 | 낮음 |
| 사이드바 네비 | SidebarButton 토글 | SettingsPage 사이드바 버튼 스타일 | (a)/(c) | 페인팅 컨트롤=필드화 OK |
| 액션 버튼 | ActionButton 페인팅 | 필요시 액션행 | (a)/(c) | OK |

### 기능 보존 체크 (영역별)
- RecipePage: `_content` 호스트·트리·ShowSpc/ShowParameterEditors·5모듈 발견 **유지**. 재설계는 헤더/버튼바/셀 **시각만**.
- SettingsPage: `_detailHost`·SwapEditor·서브패널 계약(SelectAlgorithm/SelectInspection/OverrideChanged)·임시에디터 dispose **유지**.
- 서브패널(CameraMapping/InspectionOverride/InspectionLight/LightSetup): 무인자 ctor·public API **불변**. 내부 레이아웃만 재스타일.

### 디자이너 로드 가능 준수 (위험 지점)
- 신규 정적 shell 은 IC 직렬화 가능 부분집합만: **헬퍼호출/객체초기화자/지역변수산술 금지**(`DesignerLoadabilityCheck.ps1` ic-helper-call 포함 0 유지).
- RecipeLayout 식 셀/헤더 생성은 **반복 행** → 동적이면 Code.cs `BuildXxx()`(런타임), 정적이면 IC 인라인. **헬퍼 메서드를 IC 에서 부르지 말 것**(직전 Stage 100 사고 재발 방지).
- ParameterGridControl 은 `SetItems()` 런타임 채움 → Code.cs(OnLoad/Build), IC 에는 컨트롤 필드+빈 그리드만.

### 신규 커스텀 컨트롤 필요 여부
- 원칙적 신규 불필요(Handler 자산 차용/이동으로 충족). 단 **ParameterGridControl 공용화(Common 이동)** 결정 시 그 자체가 "신규 위치" 작업. 디자인 차용(c) 택하면 Vision 측 동등 컨트롤 1개 재구현 = 사실상 신규(보고·승인 대상).

---

## §E 단계 계획 권고
### 열린 결정 (사용자 컨펌 필요 — 최우선)
1. **재사용 방식**: (a) **ParameterGridControl+ParameterGridItem(+NumericKeypadDialog/ActionButton/IndicatorDot/공용 UiTheme)를 QMC.Common 으로 이동**해 Handler·Vision 공유 — 최선이나 **선행 PR**(Common 신규 `Ui` + Handler retarget) 필요, Handler 빌드 영향. vs (c) **디자인만 차용**(Vision 측 동등 컨트롤 재구현) — Common/Handler 무영향이나 코드 중복.
2. **테마 통합 범위**: Vision UiTheme 에 Handler 추가 상수(OptionHeaderBg/ValueBox/Dot/셀색)만 보강 vs 공용 테마 Common 이동.
3. **재설계 깊이**: 시각 재스타일(헤더/셀/저장바)만 vs 파라미터 편집을 ParameterGridControl 기반으로 전면 교체.

### 진행 순서 (위험도·의존도)
1. **(선행, 결정1=a 인 경우) 컨트롤 공용화 PR**: 자족 컨트롤 9 + 공용 테마 → `QMC.Common/Ui/`. Handler 네임스페이스 retarget + 빌드. Vision csproj 는 이미 Common 참조 → 자동 가용. 게이트: Handler·Vision 양쪽 빌드0.
2. **RecipePage 재설계**(저위험 — 기존 에디터 호스팅 유지): 헤더 OrangeBar + SAVE/Reload TopBar + 트리/콘텐츠 2-톤·GroupBox 정돈. `_content` 계약 불변.
3. **SettingsPage 재설계**(중위험 — 4 서브패널): 사이드바 SidebarButton 스타일 + detailHost 유지. SwapEditor·서브패널 계약 불변.
4. **서브패널 내부 재스타일**(CameraMapping/InspectionOverride/InspectionLight/LightSetup): 2-톤 셀 / (결정1=a 면) ParameterGridControl 로 입력행 교체. 각 무인자 ctor·API 불변.

### 각 단계 검증 게이트
빌드0 + 정적검사(**ic-helper-call 포함**)0 + **VS 디자이너 실제 열기**(Stage100 교훈) + 인스턴스화·DrawToBitmap 스모크 + **기능 동일성**(트리/스왑/저장/SelectXxx/OverrideChanged) + verify_all 코어 FAIL0.

### 권고 요약
- 우선 **결정1**(공용화 vs 차용) 확정 → 공용화 택하면 선행 PR, 차용 택하면 바로 RecipePage 부터.
- 순서 **RecipePage → SettingsPage → 서브패널**(저→고 위험). 1파일/1영역 = 1커밋, master 로컬 머지, push 금지(기존 규칙).
