# Stage 93 Phase 4a — Vision UI 디자이너 로드 가능화 (Pages 저·중 6)

> Phase 4a 만 수행하고 정지. Phase 4b 별도 지시 대기. 대상 top-level `D:\Work\source\QMC.Vision\Ui\Pages\`.
> 성격: 12 Pages 전부 InitializeComponent 부재(생성자 UI) → 정적 shell 을 신규 InitializeComponent 로 **추출**.

## 변환: 6파일 (각 1커밋)
| 파일 | 처리 요약 | 커밋 |
|------|----------|------|
| OperationPage | placeholder(헤더+본문) → Designer | 3a8147f |
| DataLogPage | 헤더+TabControl(Log/Alarm/Utility) → Designer | e58dacd |
| RecipePage | 헤더/툴바/TLP×2/트리/콘텐트 정적 → Designer; 트리채움·패널전환은 Code. 람다4→named | 0e11cd8 |
| ConfigurationPage | 헤더+3 GroupBox 정적 → Designer; 런타임 데이터/Cognex진단은 Code(LoadConfig). 람다6→named(_cgxLabel 필드승격) | a797a97 |
| SettingsPage | chrome(헤더/사이드바/트리/디테일호스트) → Designer; 서브패널 인스턴스화·트리구성·전환은 Code(BuildDetailPanels/LoadAlgorithms) | 98b40e1 |
| SpcChartPage | 필터바/Chart+ChartArea+Legend 정적 → Designer; CSV로드·시리즈생성은 Code. 람다5→named | 1d4d534 |

(Step0: PHASE4_RECON, PHASE4A_BEFORE 동봉 in 3a8147f)

## 추출 원칙 (정적 shell→Designer / 동적→Code)
- **Designer**: 컨트롤 필드 + InitializeComponent(속성별·리터럴좌표·named 이벤트연결·SuspendLayout/Resume·SetChildIndex z-order·NumericUpDown/Chart BeginInit/EndInit).
- **Code**: 생성자(InitializeComponent + Designtime 가드 + 데이터로드), 런타임 채움(콤보 Items/트리 노드/차트 시리즈/Cognex진단/CSV), 핸들러 본문, 서브패널 인스턴스화(SettingsPage).

## 제거/처리
- 객체초기화자 → 속성별 할당 (전 파일)
- 람다 이벤트 → named EventHandler: Recipe 4, Config 6, Settings 1(+_tree.AfterSelect 기존 named), SpcChart 5. **closure 캡처 0** (ConfigurationPage 의 cgxLabel 지역 캡처 2건은 `_cgxLabel` 필드 승격으로 해소 — 규칙3).
- 초기화 부작용 방지: Config/SpcChart 의 콤보/날짜/Provider 초기 세팅이 핸들러를 오발하지 않도록 `_initializing` 가드(원본은 "핸들러 wired 전에 값 설정" 순서였음 → 동작 동일 보존).
- LINQ 람다(Plot/Reload 내 Select/Where)는 IC 밖 Code 메서드 → 변환 대상 아님.

## 검증 (게이트 = 빌드0 + 정적0 + 스모크; verify_all 은퇴·미실행)
- MSBuild QMC.Vision: **오류 0 / 신규 경고 0** (선재 System.IO.Ports 1건만).
- 정적검사 AFTER: 6 Designer.cs 전부 **위반 0**.
- 스모크(헤드리스 인스턴스화+DrawToBitmap, `cdt-320\designer-phase4a-test\`): **6/6 ALL PASS** 무예외. SettingsPage 렌더에 헤더+사이드바 트리+우측 CameraMappingPanel 정상 표시(서브패널 호스팅·트리구성 보존 확인).
- public 시그니처 불변(전부 파라미터리스 ctor). 동작 변경 0.

### 디자이너 로드 검증
- UserControl 6개 → 디자인 표면 有. 정적검사 0 + 인스턴스화·렌더 무예외로 검증(VS 실제 열기는 선택). 동적 채움 미실행(Designtime 가드) 상태에서도 무예외.

## closure 등 보류: 없음.

## 커밋
6 변환 커밋 → 로컬 master `--no-ff` 머지 + 브랜치 삭제. **remote push 안 함.**

→ **Phase 4b 진입 대기 (컨펌 필요).** 4b = FinderPage, InspectorPage, InspectionLightPanel, InspectionOverridePanel, CameraMappingPanel, LightSystemSetupPage (고위험·대형·동적/주입多).
