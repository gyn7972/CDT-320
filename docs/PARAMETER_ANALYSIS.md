# Vision 파라미터 클래스·구조 전수 분석 (읽기 전용)

> 작성 2026-06-09. **읽기 전용 분석** — 코드 수정·커밋 없음. 모든 단정은 `파일:줄` 인용.
> 대상: `D:\Work\source\QMC.Vision\` (중첩 `QMC.CDT-320\QMC.Vision\` stale 제외).
> 기준: Project Instructions §3-3 Recipe 3-Layer (Setup / Config / Recipe).

---

## 요약 (Executive Summary)

Vision 파라미터는 **서로 단절된 3계통 + 영속화 4경로**로 분산되어 있다:

| 계통 | 모델 | UI | 저장 경로 | 상태 |
|---|---|---|---|---|
| ① 타깃 파인더/인스펙터 inline 속성 | IPatternFinder/IInspector 구현체 필드 | ParameterGridControl (RecipePage 타깃) | `Config/VisionRecipe/<alg>/<id>.json` | **저장=텍스트 스텁·로드=no-op (실데이터 미영속)** |
| ② InspectionParameters | `InspectionParametersBase` 5종 (DataContract) | ParameterEditorBase 5종 (SettingsPage) | `Recipes/<tool>.json` (DataContractJson) | 실제 직렬화 동작, **인스펙터가 소비 안 함(고아)** |
| ③ 검사 조명 | InspectionLightSetting/Override | InspectionLightPanel | AlgorithmCameraMap store | 동작 |
| (전역) | VisionSettings | SettingsPage 일부 | `Config/vision.json` (DataContractJson) | 동작 (scope 개념 없음) |

**최대 문제 3건**: (1) ①의 SaveParameters/LoadParameters가 전 백엔드 스텁/no-op → **ROI·임계값이 재시작 후 사라짐**, (2) ②의 풍부한 알고리즘 파라미터가 **어느 인스펙터도 참조 안 함**(식별자도 불일치), (3) **3-Layer scope가 ① ParameterGridItem 에만 존재**하고 그나마 호출부 하드코딩·임의 배치.

---

## §A 파라미터 데이터 모델

### A-1. UI 바인딩 모델 — ParameterGridItem (계통 ①)
`Ui\Controls\ParameterGridItem.cs`
- `ParameterGridValueType` enum: Double/Int/Bool/Text/Selection — `ParameterGridItem.cs:9`
- `ParameterGridScope` enum: **Recipe/Setup/Config** — `ParameterGridItem.cs:18` (← 유일한 3-Layer 표현)
- `ParameterGridItem` (sealed) — `ParameterGridItem.cs:25`
  - 필드: `Key/DisplayName/Unit/ValueType/Scope/DisplayScale` + `Func<object> Getter`/`Action<object> Setter`/`Func<object,bool> Validator`/`List<ParameterGridOption> Options` — `:27-36`
  - 팩토리: `Micron`(×1000 스케일) `:45`, `Double` `:60`, `Int` `:75`, `Bool` `:90`, `Selection`(IEnumerable) `:105`, `Selection<TEnum>` `:121`
- `ParameterGridOption` (Text/object Value) — `ParameterGridItem.cs:137`
- `ParameterGridChangedEventArgs` (Item + Scope) — `ParameterGridChangedEventArgs.cs:6`

> 특징: **값은 `object`**, 실제 데이터는 Getter/Setter 클로저로 외부 객체에 바인딩(모델 자체는 값을 보유하지 않음). 타입 변환은 런타임 `Convert.ToXxx`.

### A-2. 알고리즘 파라미터 컨테이너 — InspectionParameters (계통 ②)
`Core\Inspectors\InspectionParameters.cs`
- enum: `ChipType{White,Black}` `:9`, `DistortionTargetSearch{CrossLine,Circle}` `:12`, `SideSurface{FrontWidth,BackWidth,FrontHeight,BackHeight}` `:15`
- `[DataContract] abstract InspectionParametersBase` — `:21` : `Name` `:23` + `SaveJson(path)` `:25` / `LoadJsonInternal<T>` `:40` (DataContractJsonSerializer)
- `BottomInspectionParameters` — `:59` : **27개 필드** (ChipType/Threshold=128/WidthMin·MaxColorGap/HeightMin·Max/Chipping1·2/TopHatRadius=7/MinForeignAreaFilterSize/LinkDistance + double ChippingDepth=0.05/ChippingLength=0.20/ForeignObjectSize/FirstPeek/Peek/Stdev/PortentialDefactMinSize + float ChipSpecLimit W/H ×4 + bool UseContaminationInspection + string FileSavePath) — `:61-87`
- `SideInspectionParameters` — `:97` : ChipType/Surface/Threshold + ChippingDepth/Length/ForeignObjectSize/BladeWidth/ChipThickness + SpecLimit ×4 — `:99-112`
- `DieGapInspectionParameters` — `:122` : Threshold/UpperLimit/LowerLimit — `:124-126`
- `DistortionParameters` — `:136` : ChipType/TargetSearch/Threshold/PitchX/PitchY — `:138-142`
- `VisionScaleParameters` — `:152` : ChipType/Threshold/ChipWidth/ChipHeight — `:154-157`

> 메타데이터(이름·기본값·타입)는 **C# 프로퍼티 이니셜라이저 + [DataMember]** 로만 정의. **단위·범위·scope·검증 메타 없음**.

### A-3. ROI 모델 (공용)
`Core\Roi.cs:8` `[DataContract] Roi` : Name/Shape/CenterX/CenterY/Width=100/Height=100/AngleDeg/Radius=50/InnerRadius `:10-18`, `RoiShape{Rectangle,Circle,Annulus,Polygon}` `:30`.

### A-4. 백엔드 inline 파라미터 (계통 ①의 실값 보유처)
| 구현 | 파라미터 (기본값) | 위치 |
|---|---|---|
| SimPatternFinder | AcceptThreshold(0.5), MaxInstances(1) | `Backends\Sim\SimPatternFinder.cs:15-16` |
| OpenCvPatternFinder | AcceptThreshold(0.7), MaxInstances(1) | `Backends\OpenCv\OpenCvPatternFinder.cs:19-20` |
| CognexPatternFinder | AcceptThreshold(0.7), MaxInstances(1), _trainSucceeded | `Backends\Cognex\CognexPatternFinder.cs:20-28` |
| SimInspector / OpenCvInspector | (파라미터 없음, Roi만) | `Sim\SimInspector.cs` / `OpenCv\OpenCvInspector.cs` |
| CognexInspector | Threshold(128), MinDefectArea(25), MaxTotalDefectArea(500) | `Backends\Cognex\CognexInspector.cs:21-25` |
| CognexCaliper | EdgeThreshold(30) | `Backends\Cognex\CognexCaliper.cs:16` |
| CognexColorMatch | TargetColor(Red), Tolerance(30) | `Backends\Cognex\CognexColorMatch.cs:13-14` |
| CognexHistogram | (없음) | `Backends\Cognex\CognexHistogram.cs` |

인터페이스 계약: `Core\IPatternFinder.cs:9` (Id/SearchRoi/TrainRoi/TrainImage/AcceptThreshold/MaxInstances + Train/Match + Load/SaveParameters), `Core\IInspector.cs:9` (Id/InspectionRoi/Inspect/Load/SaveParameters).

### A-5. 전역 설정 — VisionSettings
`Config\VisionConfig.cs:12` `[DataContract] VisionSettings` : Provider/Language/LightUseSim + 포트(5100~5106, 뷰어 5200~5206) + **ScaleX/ScaleY(mm/px)** `:107-109` + IsRotated/InvertedX/InvertedY/DelayBeforeGrabMs/SideLocation/ReturnMmCoordinates + 카메라ID + ImageLog/DataLog. `VisionConfigStore` `:130` (`Config/vision.json`).

### A-6. 모델 관계 트리
```
파라미터 표현
├─ ① ParameterGridItem (object 값, Getter/Setter 클로저) ── ParameterGridControl
│     └─ 바인딩 대상: 백엔드 inline 속성 (Roi.CenterX 등)  ← 계통 ①의 실값
├─ ② InspectionParametersBase (DataContract, 실값 보유)
│     ├─ BottomInspectionParameters / SideInspectionParameters
│     ├─ DieGapInspectionParameters / DistortionParameters / VisionScaleParameters
│     └─ 편집: ParameterEditorBase 5종 (NumericUpDown 직접)   ← ①과 별개 UI
├─ ③ InspectionLightSetting/Override (조명) ── InspectionLightPanel
└─ (전역) VisionSettings ── vision.json
```

---

## §B Recipe 3-Layer 매핑 (핵심)

### B-1. scope 결정 방식
- 3-Layer(Setup/Config/Recipe)는 **계통 ①의 `ParameterGridScope` enum에만 존재**(`ParameterGridItem.cs:18`).
- 값별 scope는 **호출부에서 하드코딩** — 팩토리 인자로 지정. 예: `Ui\Pages\VisionTargetPage.cs` BuildParams
  - Search ROI X/Y/W/H = **`ParameterGridScope.Setup`** (`VisionTargetPage.cs:124-127`)
  - Train ROI X/Y/W/H = **`ParameterGridScope.Recipe`** (`:128-131`)
  - `InspectorTargetPage.cs` InspectionRoi X/Y/W/H = **Setup** (`InspectorTargetPage.cs:121-124`)
- ② InspectionParameters / ③ 조명 / 전역 VisionSettings 에는 **scope 개념 자체가 없음**.

### B-2. §3-3 의도 vs 실제 — 일치/불일치

| §3-3 레이어 | 의도(수명·내용) | 실제 코드 | 일치? |
|---|---|---|---|
| **Setup** (전원OFF 유지 기구설정: 위치/오프셋) | 별도 영속 저장 | Search/Inspection ROI 가 scope=Setup 으로 표시되나, **저장이 스텁이라 실제 미영속**(§C). 전용 Setup 저장소 없음. ScaleX/Y(캘리브)는 vision.json(전역)에 있음 | ❌ 불일치 |
| **Config** (고정 사양: 타임아웃/임계값) | 고정 영속 | AcceptThreshold/MaxInstances/CognexInspector.Threshold 등은 scope 미부여 + UI 미노출 + 미영속. DelayBeforeGrabMs 등 일부는 vision.json | ⚠️ 부분 |
| **Recipe** (제품별: 속도/검사임계) | `Config/VisionRecipe/...` | Train ROI 만 scope=Recipe. 검사 임계(Bottom/Side 등)는 계통 ②에서 `Recipes/<tool>.json` 으로 분리 저장 | ⚠️ 부분/이원화 |

- **핵심 불일치**: scope 라벨(Setup/Recipe)이 UI 표시용일 뿐 **저장소·수명에 연결되지 않음**. Setup 으로 표시된 ROI 가 재시작 시 사라질 수 있음(저장 스텁).
- ② InspectionParameters 는 전부 사실상 Recipe 성(제품별 검사 임계)인데 scope 미표기 + 별도 경로.

---

## §C 영속화 (직렬화·파일)

### C-1. 저장 경로 4종
| # | 경로 | 모델 | 직렬화 | 동작 |
|---|---|---|---|---|
| 1 | `Config/vision.json` | VisionSettings | DataContractJsonSerializer (`VisionConfig.cs:146,162`) | ✅ 동작 + 버전 마이그레이션(`OnDeserialized` `:53`) |
| 2 | `Recipes/<toolName>.json` | InspectionParameters | DataContractJsonSerializer (`InspectionParameters.cs:33,47`; 경로 `ParameterEditorBase.cs:23`) | ✅ 직렬화 동작 (단, 소비처 없음) |
| 3 | `Config/VisionRecipe/<alg>/<id>.json` | finder/inspector | **백엔드별 스텁/no-op** (아래) | ❌ 실데이터 미영속 |
| 4 | AlgorithmCameraMap store | InspectionLightSetting | (AlgorithmCameraMapStore.Save) | ✅ 동작 |

### C-2. IPatternFinder/IInspector SaveParameters/LoadParameters 실태 (계통 ①)
**전 백엔드가 실제 파라미터를 직렬화하지 않음**:
- SimPatternFinder: Load=no-op, Save=`File.WriteAllText(path,"SimPatternFinder: "+Id)` 텍스트 — `SimPatternFinder.cs:61-62`
- OpenCvPatternFinder: Load=빈 구현, Save=텍스트 식별자 — `OpenCvPatternFinder.cs:166-167`
- CognexPatternFinder: Load=**TODO**(.vpp 추후), Save=키=값 텍스트(ROI BoundingBox 문자열만, 실제 CogPMAlignTool 모델 미저장) — `CognexPatternFinder.cs:161-180`
- SimInspector/OpenCvInspector: Save·Load **둘 다 빈 구현** — `SimInspector.cs:31-32`, `OpenCvInspector.cs:38-39`
- CognexInspector: Save·Load **TODO**("Tier 3") — `CognexInspector.cs:136-137`

호출처: `VisionTargetPage.cs:167`(Save)/`:184`(Load), `InspectorTargetPage.cs:163/180`, `RecipePage.cs:247-248`.
→ **결과: ROI·임계값을 편집/저장해도 텍스트 스텁만 기록되고 LoadParameters 가 no-op 이라 재시작 시 복원 불가(데이터 손실).**

### C-3. DataContract 기본값 함정 (계통 ②)
`InspectionParametersBase` 5종은 프로퍼티 이니셜라이저(예: `Threshold=128`)만 사용. **DataContractJsonSerializer 는 이니셜라이저/생성자를 실행하지 않으므로** 구버전/부분 JSON 로드 시 누락 키가 **0/false 로 채워짐**. VisionSettings 는 이를 `[OnDeserializing]`(`VisionConfig.cs:37`)으로 방어하지만 **InspectionParameters 5종에는 [OnDeserializing] 방어가 없음** → 마이그레이션 취약(메모리 `feedback_datacontract_defaults` 동일 패턴).

---

## §D 알고리즘별 파라미터 셋

모듈(`Modules\*.cs`)이 `AddFinder/AddInspector`(`VisionModule.cs:101-113`)로 백엔드 인스턴스를 Id별 생성:

| 모듈 (AlgorithmKey) | Finders | Inspectors |
|---|---|---|
| Wafer (`WaferVisionModule.cs:11`) | EjectPin/Reticle/AlignDie/FirstReference/SecondReference/Die/Scale (7) `:24-30` | 없음 |
| Bin (`BinVisionModule.cs:10`) | Reticle/Die/Scale (3) `:20-23` | PlacementInspector (1) `:22` |
| BottomInspection (`BottomInspectionModule.cs:10`) | Reticle/Collet/Die/Focus/Scale/DistortionComp (6) `:23-29` | SurfaceInspector (1) `:26` |
| FrontSide (`FrontSideInspectionModule.cs:12`) | DieEdge/Focus (2) `:22,25` | TopSurfaceInspector/TopChippingInspector (2) `:23-24` |
| RearSide (`RearSideInspectionModule.cs:12`) | DieEdge/Focus (2) `:22,25` | BottomSurfaceInspector/BottomChippingInspector (2) `:23-24` |

각 finder 의 편집 가능 파라미터(타깃 페이지 ParameterGridControl): **SearchRoi·TrainRoi(8 항목)만** (`VisionTargetPage.cs:122-132`). 각 inspector: **InspectionRoi(4 항목)만** (`InspectorTargetPage.cs:118-125`).

### 중복·산재
- **Threshold=128** 이 최소 5곳 독립 정의: CognexInspector(`:21`), Bottom/Side/DieGap/Distortion/VisionScaleParameters(`InspectionParameters.cs:62,101,124,140,155`). 공유 상수/타입 없음.
- AcceptThreshold/MaxInstances 가 Sim/OpenCv/Cognex PatternFinder 에 각각 중복 선언(0.5 vs 0.7 기본값 불일치) — `SimPatternFinder.cs:15`, `OpenCvPatternFinder.cs:19`, `CognexPatternFinder.cs:20`.
- ② InspectionParameters 의 식별자("BottomInspection"/"SideInspection"/"DieGapInspection"/"Distortion"/"VisionScale" — `SettingsPage.cs:165-169`, `ParameterEditorHost.cs:39-43`)가 **모듈 inspector id("SurfaceInspector"/"PlacementInspector"/"TopChippingInspector" 등)와 전혀 불일치** → 두 체계가 매핑되지 않음.

---

## §E 런타임 동작

### E-1. 계통 ① (타깃 페이지 → 백엔드)
- ParameterGridItem 의 Getter/Setter 클로저가 백엔드 인스턴스 속성(예: `finder.SearchRoi.CenterX`)에 직접 바인딩(`VisionTargetPage.cs:124`).
- 편집: ParameterGridControl 셀 → `CommitValue`(`ParameterGridControl.cs:310`)가 `item.Setter` 호출 → 즉시 백엔드 객체 갱신 + `ParameterValueChanged` 발생(`:354`).
- dirty: 타깃 페이지가 `ParameterValueChanged`/조명 `LightChanged` 구독 → `MarkDirty` → 상태점(R2e). 저장은 상단바 → `SaveTarget`(스텁, §C).
- 알고리즘 수행: 백엔드 `Match`/`Inspect` 가 **자신의 inline 속성을 직접 읽음**(파일→모델 로드 경로 없음, no-op Load 때문).

### E-2. 계통 ② (Editor → Recipes json)
- ParameterEditorBase ctor 가 `Recipes/<tool>.json` 로드(`:31` OnLoad→LoadFromParameters), 저장 버튼 → SaveToParameters→`InspectionParametersBase.SaveJson`. **인스펙터 런타임이 이 파일을 읽지 않음** → 편집해도 실제 검사에 반영 안 됨(고아).

### E-3. scope별 처리 차이
- 런타임에서 Setup/Config/Recipe 를 **다르게 다루지 않음** — scope 는 UI 행 배경색(`ParameterGridControl.cs:192` ApplyScopeStyles)·라벨에만 영향. 저장/로드/적용에 scope 분기 없음.

---

## §F UI 바인딩 결합도

- **ParameterGridControl ↔ 모델**: `SetItems(IEnumerable<ParameterGridItem>)`(`ParameterGridControl.cs:43`). 모델은 UI를 모름(클로저 기반, 단방향 의존) → **결합도 낮음/유연**. 단 값이 `object`+`Convert`(`:266 ParseValue`)라 **타입 안전성 약함**. 편집은 NumericKeypadDialog(`:392`)·콤보·bool 토글.
- **ParameterEditorBase ↔ 모델**: NumericUpDown/ComboBox 를 BuildEditor 에서 직접 생성(`ParameterEditorBase.cs:34,68`)하고 Load/SaveFromParameters 에서 수동 매핑 → **그리드와 완전히 다른 UI 패턴**(통일성 없음). InspectionParameters 와 직접 결합.
- 두 UI 체계가 공존(타깃=그리드, SettingsPage=에디터) → 일관성·재사용 결여.

---

## §G 문제점·재작업 후보 (우선순위)

| # | 문제 | 근거 | 영향 |
|---|---|---|---|
| **G1** | **finder/inspector 파라미터 미영속** — Save=텍스트스텁/Load=no-op·TODO | `SimPatternFinder.cs:61-62`, `OpenCvPatternFinder.cs:166-167`, `Cognex*.cs:161-180/136-137`, Sim/OpenCvInspector no-op | **치명**: ROI·임계값 재시작 시 손실. RecipePage "저장" 무의미 |
| **G2** | **InspectionParameters(②) 고아** — 5종 풍부한 파라미터를 어떤 IInspector 도 참조 안 함 | `BottomInspectionParameters` 등 참조처 = 에디터뿐(grep §검증) | 검사 임계 편집이 실검사에 미반영 |
| **G3** | **식별자 불일치** — ② 키("BottomInspection")≠모듈 inspector id("SurfaceInspector") | `SettingsPage.cs:165-169` vs `BottomInspectionModule.cs:26` | 두 체계 연결 불가, 매핑 부재 |
| **G4** | **3-Layer 미구현** — scope 가 ①에만, 저장소·수명에 비연결·하드코딩 | `ParameterGridItem.cs:18`, `VisionTargetPage.cs:124-131` | §3-3 의도 미충족(Setup/Config/Recipe 동등 취급) |
| **G5** | **UI 미노출 파라미터** — AcceptThreshold/MaxInstances/CognexInspector.Threshold/MinDefectArea/MaxTotalDefectArea 가 그리드에 없음(ROI만 바인딩) | `VisionTargetPage.cs:122-132`, `InspectorTargetPage.cs:118-125` | 임계값을 사용자가 못 바꿈 |
| **G6** | **영속화 4분할·SSOT 없음** — vision.json / Recipes / VisionRecipe / 조명store | §C-1 | 일관성·백업·이관 어려움 |
| **G7** | **타입 안전성 약함** — object 값 + 문자열 키 + 런타임 Convert | `ParameterGridItem.cs:33`, `ParameterGridControl.cs:266` | 런타임 캐스트 오류 위험 |
| **G8** | **중복 정의·기본값 불일치** — Threshold=128 ×5, AcceptThreshold 0.5/0.7 혼재 | §D 중복 | 유지보수·일관성 |
| **G9** | **DataContract 기본값 함정 미방어**(②) — [OnDeserializing] 없음 | `InspectionParameters.cs:59-160` | 구 JSON 로드 시 임계값 0 으로 붕괴 |
| **G10** | **UI 2체계** — ParameterGridControl vs ParameterEditorBase | §F | 통일성·재사용 결여 |

### 재작업 방향(제안만 — 구현/설계 확정은 보고 후 사용자와)
- **SSOT 파라미터 모델**: 알고리즘별 강타입 파라미터 클래스 1계통으로 통합(② 흡수), scope/단위/범위/기본값 메타를 attribute 또는 선언적 정의로 보유.
- **실 영속화 구현**: finder/inspector SaveParameters/LoadParameters 를 강타입 JSON(Newtonsoft 또는 DataContract + OnDeserializing 방어)으로 실제 직렬화, `Config/VisionRecipe/<alg>/<id>.json` 단일 경로.
- **scope→저장소 연결**: Setup/Config/Recipe 를 실제 분리 저장·수명에 매핑(§3-3).
- **UI 단일화**: ParameterGridControl 로 통합(에디터 NumericUpDown 폐기), 모델→`ParameterGridItem` 자동 생성기.
- **식별자 정합**: 모듈 inspector id 와 파라미터 클래스 매핑 테이블 도입.

---

## 부록 — 콘솔 요약
```
## Vision 파라미터 분석
### §A 모델: ParameterGridItem(① object/클로저) / InspectionParametersBase 5종(② DataContract) / Roi / VisionSettings / 백엔드 inline 속성
### §B 3-Layer: 불일치 — scope 는 ① enum 에만, 저장소·수명 비연결·하드코딩(Search=Setup/Train=Recipe). ②③전역엔 scope 없음
### §C 영속화: 4경로(vision.json✅ / Recipes/<tool>.json✅고아 / VisionRecipe❌스텁·no-op / 조명store✅). DataContract 기본값 함정(②방어없음)
### §D 알고리즘셋: Wafer7F / Bin3F1I / Bottom6F1I / Front2F2I / Rear2F2I. 편집노출=ROI만. Threshold=128 ×5 중복, 식별자 불일치
### §E 런타임: ①Getter/Setter 클로저→백엔드 직접, Load no-op 라 파일→모델 경로 없음. ②에디터 Recipes json↔인스펙터 미연결(고아)
### §F UI결합도: 그리드=낮음(object/Convert 약타입) / 에디터=NumericUpDown 직접. 2체계 공존
### §G 문제 Top: G1 미영속(치명) / G2 ② 고아 / G3 식별자불일치 / G4 3-Layer 미구현 / G5 임계값 UI미노출 / G6 4분할 SSOT부재
```
