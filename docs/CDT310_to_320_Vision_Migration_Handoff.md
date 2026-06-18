# CDT-310 → CDT-320 Vision 이식 핸드오프

> 목적: CDT-310(실구동) 비전 기능을 CDT-320(신규) 구조로 이식. 이 문서는 "CDT-310 → CDT-320 구현 진행 중" 채팅에서 실제 구현에 사용하는 작업 기준서다.
> 시각 자료: `docs/CDT310_to_320_Vision_Migration.html` (기능별 매핑·모듈별 표·코드 예시 포함).

---

## 0. 대원칙 (모든 결정의 기준)

1. **Vision은 측정·계산만, 모든 구동(모션)은 핸들러.** Jog·Stage move·Compensator·AutoFocus(Z)·AutoScale(이동)은 Vision에서 제외. AutoFocus/AutoScale은 "핸들러가 이동 → Vision이 sharpness/스케일 값만 반환".
2. **레시피 = 모든 레시피 화면 데이터의 SSOT.** ROI·파라미터·학습패턴·검사조명이 활성 `Recipe`를 따라 read/write. 수동 편집 + 핸들러 수신값 양방향. (현재 에디터 `RecipeName="default"` 고정 버그는 `RECIPE_LOAD_POLICY_2026-06-17.md`대로 활성 명칭 주입으로 수정.)
3. **폴더는 기존 구조에 분해해 끼워넣기.** 새 폴더/프로젝트 만들지 않음. 310 상속 클래스를 통째 이식하지 말고 로직만 해당 Backend/Unit 파일로 분해.
4. **백엔드 3종 대칭(Cognex/OpenCv/Sim).** 새 알고리즘도 세 백엔드에 같은 인터페이스로(없으면 fallback).

---

## 1. CDT-320 비전 아키텍처 요약 (이식 대상 틀)

- **계층**: `Core`(인터페이스/공용) → `Backends`(알고리즘 구현) → `Unit`(모듈+노드+데이터) → `Ui/Pages/Recipe`(에디터).
- **알고리즘 = 인터페이스 구현체**: `IPatternFinder`(Train/Match), `IInspector`(Inspect), `IEdgeFinder`(Measure), `IHistogramAnalyzer`, `IColorMatcher`.
- **모듈 = `VisionModule<Setup,Config,Recipe>`**. 생성자에서 `AddFinder<...>("XxxFinder")` / `AddInspector<...>("XxxInspector")` 호출 → `Finders/Inspectors` 딕셔너리 + `Components`(cascade) 자동 등록.
- **노드 = `FinderAlgorithm`/`InspectorAlgorithm`**. `ApplyToRuntime()`(LoadRecipe 직후 POCO→런타임), `CollectFromRuntime()`(SaveRecipe 직전 런타임→POCO).
- **데이터(POCO)**: `FinderAlgo{Setup,Config,Recipe}`, `InspectorAlgo{Setup,Config,Recipe}` (`AlgorithmData.cs`). DataContract JSON.
- **저장**: 모듈 1회 `SaveRecipe(명칭)`/`LoadRecipe(명칭)` → 자식 노드 전체 cascade. 경로 `Recipes/{recipeName}/{StorageKey}.recipe.json` (예 `WaferVision.EjectPinFinder.recipe.json`). 설정은 `EquipmentData/Setup|Config/{StorageKey}.json`.
- **백엔드 선택**: `VisionFactory` (Cognex→실패시 OpenCv→Sim). `IVisionBackend`는 현재 `CreatePatternFinder`/`CreateInspector` 만 노출.

---

## 2. 폴더 구조 매핑 (310 코드 → 320 목적지)

```
QMC.Vision/Equipment/
├─ Core/        IPatternFinder, IInspector, IEdgeFinder, IVisionBackend,
│               Roi, MatchResult, VisionScale, *Factory
├─ Backends/{Cognex,OpenCv,Sim}/   PatternFinder · Inspector
│   └─ Cognex/ + Caliper · Histogram · ColorMatch (구현 있음, 팩토리 미배선)
├─ Unit/        VisionModule, AlgorithmNode, AlgorithmData,
│               {Wafer,Bin,Bottom,TopSide,BottomSide}VisionModule, VisionMachine
├─ Cameras/{Hik,Mil,Sim}, Optics/{LFine,Leesos,Sim}, Comm/, Config/, Tools/
Ui/Pages/Recipe/  RecipePage, VisionTargetPage(finder), InspectorTargetPage(inspector), Helper
```

| 이식할 310 코드 | 성격 | 320 목적지 |
|---|---|---|
| `PatternMatchingVisionPart.OnTrain/OnSearch` (CogPMAlign) | 알고리즘 | `Backends/{Cognex,OpenCv,Sim}/*PatternFinder.cs` |
| `CDTInspector`(CUDA), `SideChippingInspector` | 알고리즘 | `Backends/*/*Inspector.cs` (에지 기반은 `CognexCaliper`) |
| Finder/Inspector **등록** | 조립 | `Unit/{모듈}VisionModule.cs` (`AddFinder/AddInspector`) |
| `PatternMatchingParameters`(Score/Angle/Max…) 등 | 데이터 | `Unit/AlgorithmData.cs` (전용 필요시 파생 Recipe) |
| `VisionScale.ConvertPosition` (px→mm) | 공용 | `Core/VisionScale.cs` (이미 존재 — 재사용) |
| 2점정렬 θ/offset 계산 (`Aligner.TwoPointAlign`) | 측정/연산 | `Core` 정렬 헬퍼 **또는** 핸들러(원칙상 후자 권장) |
| 에디터 UI | UI | `Ui/Pages/Recipe/*TargetPage.cs` |
| 레시피 저장 | 영속화 | QMC.Common `Data/Store` (cascade 자동) |

---

## 3. 기능별 이식 매핑 (상태)

| 기능 | 310 구현 | 320 적용 | 상태 |
|---|---|---|---|
| 패턴 학습/검색 | `PatternMatchingVisionPart.OnTrain/OnSearch` | `IPatternFinder.Train/Match` → `CognexPatternFinder` | **이식 가능** (로직 거의 그대로) |
| 2점/4점 정렬+θ | `Aligner.TwoPointAlign/FourPointAlign`, `GetAngle` | `First/SecondReferenceFinder`=점 검출만 | **구현 필요**: θ/offset 계산 별도 이식 |
| 실시간 다이 추적 | `RealTimeScanner.Scan` | `DieFinder` | **구현 필요**: 스캔/방향우선 로직 별도 |
| 스케일 mm/px | `VisionCalibrator.OnWork` `(MoveDist*2)/pixelDist` | `ScaleFinder` + `VisionScale` | **구현 필요** + 이동=핸들러 |
| 스테이지 보정 | `VisionCompensator.SearchGridXy` | (Vision 제외, 측정 콜백만) | **제거/핸들러** |
| 바닥면 검사 | `CDTInspector` CUDA chipping/foreign | `SurfaceInspector`→`CognexInspector`(Blob) | **구현 필요**: 정밀 로직 이식 |
| 측면 칩핑 | `SideChippingInspector.InspectChipping` | Side 모듈 Inspector + `IEdgeFinder` | **구현 필요** + Edge 배선 |
| 이젝트 핀 | `NeedleBlock`(모션) | `EjectPinFinder`(위치 검출, 신규) | 모션=핸들러 / 검출=신규 |
| px→mm 변환 | `VisionScale.ConvertPosition` | 결과부 또는 핸들러 | **확인 필요** |

---

## 4. 우선 이식 대상 (Wafer → Bin)

### WaferVision (Finder 7, Inspector 0)
`EjectPin · Reticle · AlignDie · FirstReference · SecondReference · Die · Scale` — 전부 `IPatternFinder`.
- Reticle/AlignDie/Die: 310 `PatternMatchingVisionPart` 매칭 → `CognexPatternFinder.Train/Match`로 이식.
- First/SecondReference: 310 `TwoPointAligner` 재구조화 — **점 검출은 Finder, θ/offset 계산은 핸들러(또는 Core 헬퍼)로 반드시 분리 이식**.
- Scale: 310 `VisionCalibrator` 계산식 이식, 이동은 핸들러.
- EjectPin: 310엔 비전 Finder 없음(NeedleBlock=모션) → 320 신규 핀 위치 검출.

### BinVision (Finder: Reticle·Die·Scale, Inspector: Placement)
- Reticle/Die/Scale: Wafer와 동일 패턴매칭 이식.
- Placement: 배치 정확도 검사 → `IInspector.Inspect` 구현(310 배치 확인 로직 이식).

---

## 5. 선결정/주의 (이식 전에 정해둘 것)

- [ ] **θ/offset 계산 위치**: Core 헬퍼 vs 핸들러. (원칙상 핸들러 권장 — Vision은 점만 반환)
- [ ] **px→mm 변환 위치**: Vision 결과부 vs 핸들러. (`VisionScale.ConvertPosition` 재사용)
- [ ] **Edge/Histogram/Color 사용 여부**: 측면칩핑을 EdgeFinder로 갈 거면 → `IVisionBackend`에 `CreateEdgeFinder` 추가 → OpenCv/Sim 구현(Cognex `CognexCaliper`는 있음) → `VisionModule`에 `AddEdgeFinder` 헬퍼.
- [ ] **직렬화 포맷**: 310 바이너리 → 320 JSON. 310 레시피 그대로 못 읽음 → **재티칭/1회 마이그레이션**.
- [ ] **레시피명 연동**: 에디터 `"default"` 고정 제거(`RECIPE_LOAD_POLICY` 4단계).

---

## 6. 신규 Finder/Inspector 추가 6단계 (320)

1. **데이터 타입**: 공용 `FinderAlgo{Setup,Config,Recipe}` 재사용. 전용 필요시 파생 Recipe.
2. **모듈 등록**: 생성자에서 `AddFinder<...>("XxxFinder")` / `AddInspector<...>("Xxx")`.
3. **백엔드 구현**: `Backends/*/`의 PatternFinder/Inspector에 Train/Match/Inspect 로직. 전용 파라미터는 `IAlgoParamSync`로 POCO↔런타임 동기화.
4. **UI 자동 반영**: `RecipePage`가 `module.Finders/Inspectors` 열거 → 세팅선택기 노출. 라벨은 `InspectionLabel`에 추가.
5. **파라미터 그리드**: `*TargetPage.AppendNodeParams()`에 전용 필드만 추가(공용 자동).
6. **검증**: `LoadRecipe/SaveRecipe` cascade 확인 + `tools/verify_vision_features.pl` 회귀.

---

## 7. 핵심 참조

- 분석 HTML: `docs/CDT310_to_320_Vision_Migration.html`
- 레시피 로드/저장 정책: `QMC.Vision/RECIPE_LOAD_POLICY_2026-06-17.md`
- 320 코어: `QMC.Vision/Equipment/Core/{IPatternFinder,IInspector,IEdgeFinder,IVisionBackend,MatchResult,VisionScale}.cs`
- 320 노드/모듈: `QMC.Vision/Equipment/Unit/{VisionModule,AlgorithmNode,AlgorithmData,WaferVisionModule,BinVisionModule}.cs`
- 320 UI: `QMC.Vision/Ui/Pages/Recipe/{RecipePage,VisionTargetPage,InspectorTargetPage,ITargetPage}.cs`
- 310 비전: `QMC.Common/VisionPart/*`, `CDT-310_1/{CDTInspector,SideChippingInspector}.cs`, `QMC.Common/Parts/NeedleBlock.cs`

> 작업 규칙은 프로젝트 `AGENTS.md` 준수(Designer 인라인, 예외 try/catch/finally, 모션 비동기 Task<int>, UTF-8, JSON pretty). C:/V: 동시 반영.
