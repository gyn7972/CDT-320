# Vision 파라미터 재작업 — 설계·계획 (SSOT 3계층 스토어)

> 작성 2026-06-09. **설계·계획 단계 — 본 구현 금지(문서 내 스케치만).** `docs/PARAMETER_ANALYSIS.md` 후속.
> 확정 방향: SSOT 디스크립터 + 3계층 ParameterStore. 페이지: SettingsPage=Setup+Config / RecipePage=Recipe. 제품 선택기=나중(`<default>`).
> 모든 단정 `파일:줄`. 도메인 판단 필요 항목은 **[확인 필요]** 표시(추측 금지).

---

## §0 현재 코드 정착점 (설계 전제)

- finder/inspector 는 모듈이 dict 로 보유: `Finders[bareId]`/`Inspectors[bareId]`, 인스턴스 `Id = Name+"/"+bareId` (`Modules\VisionModule.cs:101-113`). 예 `WaferVision/ReticleFinder`.
- 타깃 페이지가 파라미터를 ParameterGridItem 으로 바인딩: ROI 만 (`Ui\Pages\VisionTargetPage.cs:122-132`, `InspectorTargetPage.cs:118-125`).
- ② InspectionParameters 편집기는 SettingsPage 트리에서 문자열 tool 키로 분기 (`Ui\Pages\SettingsPage.cs:160-174`), 저장 `Recipes/<tool>.json` (`Ui\Editors\ParameterEditorBase.cs:23`).
- 직렬화 현황: 전부 `DataContractJsonSerializer`. **Newtonsoft.Json 미참조** (`QMC.Vision.csproj:36-55` Reference 목록에 없음).
- 영속화 4경로: vision.json(`Config\VisionConfig.cs:133`) / Recipes/<tool>.json / Config/VisionRecipe/<alg>/<id>.json(스텁) / 조명 AlgorithmCameraMapStore.

---

## §1 디스크립터 모델 설계

### 1-1. 판정 — attribute vs registry → **디스크립터 레지스트리 (DescribeParameters)**
근거(실코드):
- ① `ParameterGridItem` 은 이미 Key/DisplayName/Unit/ValueType/Scope/Getter/Setter/Validator 를 보유(`ParameterGridItem.cs:27-36`) — 사실상 디스크립터의 90%. **단 Min/Max/Default/Target(타깃 id) 부재, Scope 가 호출부 하드코딩**.
- ② POCO 는 `[DataMember]` 이니셜라이저로만 메타 보유, 계층·단위·범위 없음(`InspectionParameters.cs:61-87`).
- attribute 방식은 ② POCO 에는 달 수 있으나 ① 클로저 바인딩(런타임 객체 속성)에는 부적합 → 두 계통을 한 메커니즘으로 못 묶음.
→ **결론**: WinForms 비의존 POCO **`ParameterDescriptor`** 를 SSOT 로 신설하고, 각 도메인 객체가 `IParameterProvider.DescribeParameters()` 로 자신의 디스크립터를 공급. `ParameterGridItem` 은 디스크립터에서 생성되는 **UI 어댑터**로 강등(모델→UI 단방향).

### 1-2. ParameterDescriptor (스케치 — 구현 아님)
```csharp
// Core\Parameters\ParameterDescriptor.cs (신설 예정, WinForms 비의존)
public enum ParameterLayer { Setup, Config, Recipe }      // 기존 ParameterGridScope 대체/정렬
public enum ParameterType  { Double, Int, Bool, Text, Enum }

public sealed class ParameterDescriptor {
    public string Target { get; set; }       // 정규 id, 예 "BottomInspection/SurfaceInspector"
    public string Key { get; set; }          // 타깃 내 고유, 예 "Threshold"
    public string DisplayName { get; set; }
    public string Unit { get; set; }
    public ParameterType Type { get; set; }
    public ParameterLayer Layer { get; set; }    // ← 선언 1회 (하드코딩 제거)
    public object Default { get; set; }
    public double? Min { get; set; }  public double? Max { get; set; }
    public double DisplayScale { get; set; } = 1.0;
    public IReadOnlyList<ParameterGridOption> Options { get; set; }   // Enum용
    public Func<object> Getter { get; set; }     // 실 도메인 객체에 바인딩(② POCO 필드/① 속성)
    public Action<object> Setter { get; set; }
    public Func<object,bool> Validator { get; set; }   // Min/Max 자동 합성 가능
}

public interface IParameterProvider {            // finder/inspector/camera/light/scale 가 구현
    string ParameterTarget { get; }              // 정규 id
    IEnumerable<ParameterDescriptor> DescribeParameters();
}
```

### 1-3. ①②③ → 디스크립터 통합 매핑 (계층 배정 초안)

| 출처 | 기존 필드 | 신 Descriptor.Key | 계층 | 비고 |
|---|---|---|---|---|
| ① finder | SearchRoi X/Y/W/H (`VisionTargetPage.cs:124`) | SearchRoi.* | **Setup** | 기구 정렬 위치 |
| ① finder | TrainRoi X/Y/W/H (`:128`) | TrainRoi.* | **Recipe** | 제품 패턴 |
| ① finder | AcceptThreshold/MaxInstances (`SimPatternFinder.cs:15`) | AcceptThreshold/MaxInstances | **Recipe** [확인 필요] | 현재 UI 미노출(G5) |
| ① inspector | InspectionRoi X/Y/W/H (`InspectorTargetPage.cs:121`) | InspectionRoi.* | **Setup** | |
| ① inspector | CognexInspector Threshold/MinDefectArea/MaxTotalDefectArea (`CognexInspector.cs:21-25`) | 동일 | **Recipe** [확인 필요] | G5 |
| ② Bottom | 27필드 (`InspectionParameters.cs:61-87`) | 동일 | **Recipe**(검사임계 다수) + SpecLimit=**Config**? [확인 필요] | ChipSpecLimit/FileSavePath 계층 모호 |
| ② Side/DieGap/Distortion/VisionScale | 각 필드 | 동일 | **Recipe** 기본, Pitch/ChipWidth/Height 등 캘리브성=**Setup/Config**? [확인 필요] | |
| ③ 전역 | VisionSettings ScaleX/Y/Inverted/IsRotated (`VisionConfig.cs:107-115`) | 동일 | **Setup**(캘리브) [확인 필요] | |
| ③ 전역 | Ports/Viewer/DelayBeforeGrabMs/Language/Provider (`VisionConfig.cs:23-127`) | 동일 | **Config** | 고정 사양 |
| ③ 조명 | InspectionLightSetting (Level/Page 등) | 별도 | **Recipe** | 현 AlgorithmCameraMapStore 유지(어댑터로 디스크립터 노출만) |

> 계층 배정 다수가 **[확인 필요]** — §6 말미 목록에 집계. §3-3(Setup=기구/위치, Config=고정사양, Recipe=제품별)을 1차 기준으로 했으나 SpecLimit·캘리브·임계의 경계는 도메인 확정 필요.

---

## §2 ParameterStore (3계층) 설계

```csharp
// Core\Parameters\ParameterStore.cs (스케치)
public sealed class ParameterStore {
    // 등록: 도메인 객체가 부팅 시 자신을 등록 → 디스크립터 수집
    public void Register(IParameterProvider provider);
    // 조회
    public ParameterDescriptor Get(string target, string key);
    public IEnumerable<ParameterDescriptor> GetByLayer(ParameterLayer layer);
    public IEnumerable<ParameterDescriptor> GetByTarget(string target);
    public IEnumerable<ParameterDescriptor> GetByLayerAndTarget(ParameterLayer layer, string target); // UI 필터
    // 값 (Getter/Setter 위임 = 실 도메인 객체가 진실의 원천)
    public object GetValue(string target, string key);
    public void   SetValue(string target, string key, object value);  // → dirty(layer,target)
    // dirty (계층별·타깃별)
    public bool IsDirty(ParameterLayer layer);
    public bool IsDirty(string target);
    public event EventHandler<ParamDirtyEventArgs> DirtyChanged;
    // 영속화 (§3)
    public void LoadAll();  public void SaveLayer(ParameterLayer layer);  public void SaveTarget(string target);
}
```
- **값 소유 정책**: 스토어는 **레지스트리+라우터+dirty+영속화 오케스트레이터**. 실값은 도메인 객체(finder/inspector/VisionSettings/② POCO)에 그대로 둠 → 이중 진실원 방지(런타임 알고리즘이 이미 자기 속성을 읽으므로 `Match`/`Inspect` 무변경).
- 라우팅: `descriptor.Layer` → 해당 계층 컨테이너로 자동 분류(태그 기반). 타깃별 그룹은 `descriptor.Target`.
- dirty: SetValue 시 (layer,target) 양쪽 플래그 set → DirtyChanged 발화(상태점·계층별 저장 버튼 연동).

---

## §3 영속화 라우팅 설계

### 3-1. 계층별 경로 (Handler 규약 미러)
| 계층 | 경로 | 단위 |
|---|---|---|
| Setup | `Config/Setup/<target>.json` (또는 통합) — Handler `EquipmentDataStore`/`JsonDataStore<T>` 패턴 | 타깃별/통합 [확인 필요] |
| Config | `Config/vision.json` (기존 VisionSettings 정리·유지) | 전역 |
| Recipe | **`Recipes/<product>/<target>.recipe.json`** — Handler `UnitRecipeStore` 규약 그대로(now `product=<default>`) | 제품×타깃 |
| (조명) | AlgorithmCameraMapStore 유지, 어댑터로 Recipe 계층에 편입 | |

> 변경점: Recipe 경로를 기존 `Config/VisionRecipe/...` 대신 **Handler 와 동일한 `Recipes/<product>/<storageKey>.recipe.json`** 로 통일(아래 §3-2). 구 `Config/VisionRecipe` 는 마이그레이션 소스로만.

### 3-2. 직렬화 = **Handler 방식 그대로 미러** (사용자 결정 2026-06-09 — Newtonsoft 추가 금지)
Handler 실태 조사 결과(전부 `QMC.Common` 공용, Vision 도 이미 QMC.Common 참조):
- **Read** = `DataContractJsonSerializer.ReadObject` (`QMC.CDT-320\Equipment\Recipes\RecipeStore.cs:134`, `QMC.Common\Persistence\UnitRecipeStore.cs:38`).
- **Write** = `QMC.Common.Data.Store.JsonPrettySerializer.WriteObject` (`RecipeStore.cs:150`; 구현 `QMC.Common\Data\Store\JsonPrettySerializer.cs:39-54` = DataContractJson + UTF-8 2-space 들여쓰기 + `EmitTypeInformation.Never`).
- **기본값 처리 패턴(② 함정 G9 차단)** = **`[OnDeserializing]` 로 기본값 주입 + `[OnDeserialized]` fallback**. 근거 주석: "DataContract 이니셜라이저는 역직렬화 때 실행되지 않으므로 OnDeserializing 으로 기본값 주입" (`QMC.Common\Recipes\LightSystemSetup.cs:82,96-97`). VisionSettings 도 동일(`VisionConfig.cs:37`).
- **원자적 저장**(손상 방지) = tmp 파일 → `File.Replace`/`Move` (`UnitRecipeStore.cs:59-72`).
- **재사용 베이스**: `QMC.Common.Persistence.UnitRecipeStore`(Recipe 계층 그대로 사용 — `Load<T>` miss 시 `new T()` 기본값 안전 `:25-47`, `Save<T>` 원자적 `:53-82`, Copy/Rename/Delete/List 제품 관리 포함), `EquipmentDataStore`/`JsonDataStore<T>`(Setup/Config 전역).

→ **Vision 직렬화 결정(확정)**: Newtonsoft 미도입. **Read=DataContractJsonSerializer / Write=JsonPrettySerializer**. 전 파라미터·레시피 POCO 에 **`[OnDeserializing]` 기본값 주입 의무화**(② 5종 + 신 디스크립터 POCO). Recipe 계층은 **`UnitRecipeStore<T>` 직접 재사용**(product=`<default>`, storageKey=정규 target id). → G1(스텁)·G9(함정) 동시 해소 + Handler 와 포맷·디렉터리 일관.

### 3-2a. Handler RecipeProject 가 이미 Vision 파라미터 보유 — [확인 필요]
`RecipeProject`(`RecipeStore.cs:210`)는 이미 Vision 관련 항목 포함: `InspectionSubset`×3(BottomInsp/TopSideInsp/BottomSideInsp — Enable/ExposureMs/LightIntensity/ChippingDepthMaxMm/ScratchAreaMaxMm2/MinDieCenterScore 등 `:288-299`), `DieSubset`(ChipSpecLimit·ChippingDepth/Length·ForeignSizeMax `:303-318`).
- **[확인 필요] (아키텍처)**: Vision 이 (가) Handler 의 `Recipes/<name>.Project` 를 **공유 읽기**(SSOT=Handler 레시피, 별도 프로세스 간 파일 공유)할지, vs (나) **패턴만 미러한 별도 Vision 레시피 스토어**(Vision 전용 항목만)로 둘지. 도메인·운영 결정 필요(두 프로세스 레시피 동기화 정책).

### 3-3. SaveParameters/LoadParameters 스텁 제거 (G1)
- `IPatternFinder`/`IInspector` 의 Save/Load 를 **스토어 위임 래퍼**로 전환:
  ```csharp
  void SaveParameters(string path) => ParameterStoreHost.Current.SaveTarget(this.ParameterTarget);
  void LoadParameters(string path) => ParameterStoreHost.Current.LoadTarget(this.ParameterTarget);
  ```
  (기존 호출처 `VisionTargetPage.cs:167/184`, `InspectorTargetPage.cs:163/180`, `RecipePage.cs:247-248` 무변경 유지 — 비파괴.) 또는 호출처를 store API 로 점진 교체. 텍스트 스텁(`SimPatternFinder.cs:62` 등) 폐기.

### 3-4. 마이그레이션·호환 읽기
- 최초 LoadAll 시: ① 신규 경로 없으면 → 구 `Recipes/<tool>.json`(②) + `vision.json`(기존 그대로) + 조명 store 읽어 디스크립터 Setter 로 주입 → 신 경로로 재저장. 구 파일은 보존(롤백 가능).
- vision.json 은 Config 계층 그대로 재사용(파괴 없음). Recipes/<tool>.json → Recipe(<default>) 로 이전.

---

## §4 단일 id 체계 (G2·G3)

### 4-1. 실 사례 (전수)
- 모듈 inspector instance id: `PlacementInspector`(`BinVisionModule.cs:22`), `SurfaceInspector`(`BottomInspectionModule.cs:26`), `TopSurfaceInspector`/`TopChippingInspector`(`FrontSideInspectionModule.cs:23-24`), `BottomSurfaceInspector`/`BottomChippingInspector`(`RearSideInspectionModule.cs:23-24`).
- ② tool 키: `BottomInspection`/`SideInspection`/`DieGapInspection`/`Distortion`/`VisionScale`(`SettingsPage.cs:165-169`). → **검사 종류(타입)**, 인스턴스 아님.

### 4-2. 정규 id 제안
- **정규 Target = `<AlgorithmKey>/<instanceId>`** (예 `BottomInspection/SurfaceInspector`). finder/inspector 의 `ParameterTarget` 로 노출.
- ② 파라미터 클래스 ↔ 실 인스펙터 연결 매핑(파라미터 종류→인스턴스):

| ② 클래스 | 연결 대상 인스턴스(정규 id) | 판정 |
|---|---|---|
| BottomInspectionParameters | BottomInspection/SurfaceInspector | [확인 필요] |
| SideInspectionParameters | FrontSide/TopSurfaceInspector·TopChippingInspector / RearSide/Bottom…(4 표면?) | [확인 필요] — Surface enum 4면과 인스턴스 매핑 |
| DieGapInspectionParameters | Bin/PlacementInspector? 또는 별도 | [확인 필요] |
| DistortionParameters | BottomInspection/DistortionCompensation (finder) | [확인 필요] |
| VisionScaleParameters | *Module Scale(=ScaleFinder)/VisionConfig.ScaleX·Y | [확인 필요] |

### 4-3. 연결 방법 (G2 해소)
- 인스펙터가 자신의 ② POCO 인스턴스를 **보유**하고 `DescribeParameters()` 가 그 POCO 필드에 Getter/Setter 바인딩 → 편집이 **실검사에 반영**(인스펙터 `Inspect` 가 그 POCO 를 읽도록 백엔드 연결). CognexInspector 의 inline Threshold(`CognexInspector.cs:21`) 는 해당 ② POCO 필드로 흡수/일원화.
- 매핑 테이블은 코드 상수(레지스트리)로 1곳 정의 → SettingsPage 문자열 분기(`SettingsPage.cs:163`) 폐기.

---

## §5 UI 연결 설계 (P4)

- `ParameterGridControl` 입력을 **스토어 질의 결과**로: `store.GetByLayerAndTarget(layer, target)` → 디스크립터 → `ParameterGridItem` 어댑터 자동 생성(팩토리 `ParameterGridItem.FromDescriptor(d)`).
- **SCOPE 컬럼 = `descriptor.Layer`**(하드코딩 제거; 현 `VisionTargetPage.cs:124` 의 scope 인자 삭제).
- 편집 → `item.Setter`(=descriptor.Setter) → 동시 `store.SetValue` → (layer,target) dirty → 상태점(R2e `MarkDirty` 경로 재사용).
- 페이지 노출 규칙:
  - **RecipePage**(타깃 페이지): `layer=Recipe` 디스크립터만. 저장=Recipe 계층/타깃.
  - **SettingsPage**: `layer∈{Setup,Config}` 디스크립터. 카메라/조명/검사 트리 노드별 타깃 필터.
- **G5 해소**: AcceptThreshold/MaxInstances/Threshold/MinDefectArea 등도 디스크립터로 선언되므로 자동 노출(계층에 따라 Recipe/Config 페이지에).
- ParameterEditorBase NumericUpDown 5종(`ParameterEditorBase.cs`)은 그리드로 흡수 → 점진 폐기(P4 후) [확인 필요: 즉시 폐기 vs 유지].

---

## §6 단계 계획 (P1~P4)

### P1 — 디스크립터 모델 + 계층 메타 (①②③ 통합 정의)
- 신설: `Core\Parameters\ParameterDescriptor.cs`, `IParameterProvider.cs`, `ParameterLayer` enum.
- 각 도메인에 `DescribeParameters()` 구현(값은 기존 객체 바인딩, 동작 무변경).
- 영향: 백엔드 finder/inspector, ② POCO 보유 인스펙터, VisionSettings 어댑터.
- 게이트: 빌드0 / 정적0 / verify코어 FAIL0 / 기능동일(런타임 경로 무변경). 비파괴: UI·저장 아직 미연결(디스크립터만 추가).
- 위험: 낮음(추가만). 롤백: 신파일 제거.

### P2 — ParameterStore + 영속화 라우팅 (스텁 대체, G1·G9)
- 신설: `ParameterStore`, `ParameterJson`(직렬화 단일), 계층 경로 라우팅, `[OnDeserializing]` 가드.
- Save/Load 스텁 → 스토어 위임 래퍼(§3-3). 마이그레이션·호환 읽기(§3-4).
- 게이트: 빌드0/정적0/verify코어0 + **영속 왕복 테스트**(ROI·임계값 저장→재시작→복원). 비파괴: vision.json 포맷 유지, 구 Recipes 읽기 호환.
- 위험: 중(저장 포맷). 롤백: 래퍼를 구 스텁으로 되돌림 + 구 파일 보존.

### P3 — 도메인 연결 + id 통일 (G2·G3)
- 정규 id(`ParameterTarget`) 도입, ②↔인스턴스 매핑 레지스트리, 인스펙터가 ② POCO 보유·Inspect 가 읽도록 연결. SettingsPage 문자열 분기 폐기.
- 게이트: 빌드0/정적0/verify0 + 검사 파라미터 편집→실검사 반영 확인. 비파괴: 기존 검사 결과 동치(기본값 동일 시).
- 위험: 중(검사 동작). 롤백: 매핑 비활성 시 기존 inline 사용.

### P4 — UI 연결 (그리드↔스토어)
- ParameterGridControl 입력을 스토어 질의로, SCOPE=계층, 편집→store.Set→dirty. RecipePage/SettingsPage 노출 규칙. G5 노출. 에디터 5종 흡수.
- 게이트: 빌드0/정적0/VS디자이너/스모크/verify0/기능동일. 비파괴: SettingsPage 기존 편집 동작 유지(전환 완료까지 병존 가능).
- 위험: 중(UI). 롤백: 페이지별 단계 전환.

### 계층 배정·도메인 확정 대기 목록 ([확인 필요])

**계층 배정 — §1-3 초안 수락(사용자 결정: Search ROI/캘리브·스케일=Setup, Train ROI/검사임계=Recipe, 포트·지연=Config). 세부만 확정 대기:**
1. finder AcceptThreshold/MaxInstances 계층 (Recipe? Config?)
2. CognexInspector Threshold/MinDefectArea/MaxTotalDefectArea 계층 (Recipe?)
3. ② ChipSpecLimit(상·하한)·FileSavePath 계층 (Config? Recipe?)
4. ② Pitch/ChipWidth/ChipHeight 등 캘리브성 계층 (Setup? Config?)
5. VisionSettings ScaleX/Y/Inverted/IsRotated 계층 (Setup 캘리브 가정 — 확정?)

**사용자 추가 요청 3건 (2026-06-09):**
6. **Search ROI 타깃별 검증** — Search ROI 를 Setup 으로 일괄 배정하는 게 모든 finder 타깃에 타당한지 타깃별 검증(일부는 Recipe 가 맞을 수 있음).
7. **누락 카테고리 계층 배정** — 노출(Exposure)·조명(Light/Illumination)·매칭 임계(Match/Accept threshold) 등 §1-3 초안에 누락·미분류된 파라미터 카테고리의 계층 배정.
8. **파라미터×타깃 계층 override 허용 여부** — 같은 Key 라도 타깃에 따라 계층을 다르게 지정(override)할 수 있게 할지, 아니면 Key 당 계층 1개로 고정할지(디스크립터 모델 설계에 영향).

**id·매핑·구조:**
9. ②↔인스턴스 매핑 5건(§4-2 표) 전부
10. Setup 저장 단위(타깃별 파일 vs 통합 파일)
11. Recipe 아키텍처(§3-2a): Handler `.Project` 공유 읽기(SSOT=Handler) vs 패턴만 미러한 별도 Vision 스토어
12. ParameterEditorBase 5종 즉시 폐기 vs 병존 기간

**해소됨:**
- ~~직렬화 방식~~ → **확정: Handler 미러(DataContractJson read + JsonPrettySerializer write + [OnDeserializing] 가드 + UnitRecipeStore 재사용), Newtonsoft 미도입** (§3-2).

---

## 콘솔 요약
```
## Vision 파라미터 재작업 설계
### §1 디스크립터: ParameterDescriptor(POCO, Layer 메타) + IParameterProvider.DescribeParameters — attribute 아닌 레지스트리(①클로저+②POCO 통합). ParameterGridItem=UI어댑터로 강등
### §2 Store: 3계층 레지스트리+라우터+dirty+영속오케스트레이터. 실값은 도메인객체 유지(이중진실 방지, Match/Inspect 무변경)
### §3 영속화: Recipe=Recipes/<default>/<target>.recipe.json(Handler UnitRecipeStore 재사용), Config=vision.json, Setup=Config/Setup. 직렬화=**Handler 미러**(DataContractJson read + JsonPrettySerializer write + [OnDeserializing] 가드, Newtonsoft 無). Save/Load 스텁→스토어 위임(G1·G9). 구경로 호환읽기 마이그레이션. [확인]Handler .Project 공유 vs 별도
### §4 id: 정규 <AlgorithmKey>/<instanceId>. ②키↔인스턴스 매핑 레지스트리(5건 [확인필요]). 인스펙터가 ②POCO 보유→편집 실검사 반영(G2)
### §5 UI: 그리드 입력=store 질의, SCOPE=계층(하드코딩 제거), 편집→store.Set→dirty. RecipePage=Recipe/SettingsPage=Setup·Config. G5 노출. 에디터5종 흡수
### §6 단계: P1 디스크립터 / P2 Store+영속(스텁대체) / P3 도메인연결+id통일 / P4 UI. 각 빌드0·정적0·verify0·비파괴. [확인필요] 9건(계층배정·매핑·직렬화)
```
