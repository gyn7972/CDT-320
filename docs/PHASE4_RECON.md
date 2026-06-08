# Phase 4 정찰 — Pages 12 (읽기 전용 측정, 2026-06-08)

> 코드 수정·커밋 없음. 대상 top-level `D:\Work\source\QMC.Vision\Ui\Pages\`. 전부 코드 직접 확인.
> 전 파일 **base=UserControl**, **InitializeComponent 없음(NO-IC) → 정적검사 위반 0** (모든 UI를 생성자/BuildLayout 에서 명령형 생성). 즉 Phase 1·2 와 동일하게 "생성자 UI → Designer InitializeComponent 추출" 작업.

| 파일 | 줄 | IC | 람다 | 루프 | Ctrl.Add | 정적/동적 | closure | 외부 시그니처 | 위험 | 분리규모 |
|---|---|---|---|---|---|---|---|---|---|---|
| OperationPage | 33 | NO-IC | 0 | 0 | 2 | 정적(placeholder) | 없음 | 파라미터리스 | 낮음 | 소(거의 자명) |
| DataLogPage | 30 | NO-IC | 0 | 0 | 2 | 정적(placeholder) | 없음 | 파라미터리스 | 낮음 | 소 |
| RecipePage | 157 | NO-IC | 4 | 2 | 12 | 정적+일부동적 | 낮음 | 파라미터리스 | 낮음~중 | 중 |
| ConfigurationPage | 230 | NO-IC | 6 | 1 | 14 | 정적 위주 | 낮음 | 파라미터리스 | 중 | 중 |
| SettingsPage | 244 | NO-IC | 1 | 4 | 11 | 정적(서브페이지 호스트) | 낮음 | 파라미터리스, Form1 호출 | 중 | 중 |
| SpcChartPage | 221 | NO-IC | 13 | 4 (~1 의심) | 13 | 차트 동적 | **검토** | 파라미터리스 | 중 | 중 |
| FinderPage | 337 | NO-IC | 11 | 4 | 10 | 정적배치+동적그랩 | 검토(RoiEdited 등) | **ctor(VisionModule, IPatternFinder)** + 파라미터리스, Stage87 Dispose/StartLive | 중~높음 | 중~대 |
| InspectorPage | 322 | NO-IC | 9 | 4 | 9 | 정적배치+동적 | 검토 | **ctor(VisionModule, IInspector)** + 파라미터리스 | 중~높음 | 중~대 |
| InspectionLightPanel | 307 | NO-IC | 16 | 11 (~2 의심) | 6 | **DataGridView 동적행(BindFields)** 위주 | **검토** | **ctor(algorithm, inspectionId)** + 파라미터리스, public SelectInspection | 높음 | 중(동적→Code, shell만 Designer) |
| InspectionOverridePanel | 319 | NO-IC | 19 | 3 | 33 | 정적 다수 | 낮음(~0) | 파라미터리스 | 높음(람다多) | 대 |
| CameraMappingPanel | 679 | NO-IC | 29 | 5 (~2 의심) | 39 | 정적 다수+동적 | **검토** | 파라미터리스 | 높음 | 대 |
| LightSystemSetupPage | 740 | NO-IC | 36 | 24 (~2 의심) | 17 | **그리드+TreeView 동적 결선** 위주 | **검토(높음)** | 파라미터리스, 결선/저장 다수 | 높음(최대) | 매우 대 |

### 정적검사 BEFORE: **위반 0** (12파일 전부 NO-IC — IC 부재. 분리 작업은 "추출"이지 "위반 수정"이 아님)
### Reflection 동적생성: 12파일 모두 0건 (Phase2 Editor 자식 같은 reflection 폼 아님 — 단 InspectionLightPanel/LightSystemSetupPage 는 DataGridView/TreeView 런타임 바인딩이라 그 부분은 Code.cs 유지 대상)

## 분할 권고
- **4a (저·중 위험, 분리량 적음 — 6파일)**: OperationPage, DataLogPage, RecipePage, ConfigurationPage, SettingsPage, SpcChartPage
  - 근거: 줄수 ≤244, 람다 적음/closure 거의 없음, 외부 생성자 주입 없음(전부 파라미터리스), 정적 위주라 shell 추출이 명확. SpcChart 만 차트 동적+람다13이라 4a 내 최난도.
- **4b (고위험·대형·결선/주입 多 — 6파일)**: FinderPage, InspectorPage, InspectionLightPanel, InspectionOverridePanel, CameraMappingPanel, LightSystemSetupPage
  - 근거: 줄수 307~740, 람다 9~36, 동적 바인딩(DataGridView/TreeView)·외부 생성자 주입(Finder/Inspector/InspectionLight)·closure 의심 다수. LightSystemSetupPage(740L/36람다/24루프)가 단연 최대 — 단독 취급 권장.

## 특이/주의 사항
- **외부 생성자 주입 3파일**(FinderPage=VisionModule+IPatternFinder, InspectorPage=VisionModule+IInspector, InspectionLightPanel=algorithm+inspectionId): 디자이너 인스턴스화용 **파라미터리스 ctor 이미 존재**(grep 확인) → ZoomDialog 처럼 추가 불필요. 단 주입 ctor + public(SelectInspection 등) 시그니처 **불변 유지** 필수.
- **closure 검토 필수**: SpcChartPage, FinderPage, InspectorPage, InspectionLightPanel, CameraMappingPanel, LightSystemSetupPage — 람다가 루프 근처에 존재. 변환 시 각 람다의 지역변수 캡처 여부 개별 확인 → 캡처면 IlluminatorPanel(Phase1)식 unroll/named 또는 정지·보고.
- **동적 바인딩 유지 대상**(Designer 금지, Code.cs): InspectionLightPanel 의 `BindFields`(DataGridView 행), LightSystemSetupPage 의 결선 그리드/TreeView 런타임 빌드, SpcChart 의 시리즈. 이들은 Phase2 Editor 자식처럼 "shell 만 Designer, 동적은 Code".
- **이미 Designer 분리 영향받은 파일**: InspectionLightPanel(Stage86/87 호스팅 제거), FinderPage/InspectorPage(Stage87 라이브 grab+Dispose 추가), LightSystemSetupPage(Stage83 BindSetsGrid 수정) — 현 상태 기준으로 작업.
- OperationPage/DataLogPage 는 placeholder(헤더+본문 라벨 2개) — 거의 자명한 변환.

## 권고 진행 순서
4a 먼저(쉬움·빠른 신뢰 확보) → 4b. 4b 내에서는 InspectionOverridePanel/CameraMappingPanel → Finder/Inspector → InspectionLightPanel → **LightSystemSetupPage 최후(최대·최고위험)**.
