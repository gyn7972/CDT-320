# P2 영속화 계획 — ParameterStore + 라우팅 (Step 0 확정안)

> 작성 2026-06-09. **Step 0: 설계 2점 확정 — 보고 후 정지(컨펌).** 컨펌 후 Step 1~5 구현.
> 기준 `docs/PARAMETER_REWORK_DESIGN.md`(§2·§3), `docs/PARAMETER_LAYER_DECISIONS.md`. 직렬화=Handler 미러 확정.
> 브랜치 `param-rework-p2-store` (P1 위).

---

## 재사용 시그니처 확인 (실코드)
- **`QMC.Common.Persistence.UnitRecipeStore`** — `T Load<T>(string recipeName, string storageKey) where T : new()` (`UnitRecipeStore.cs:25`) / `bool Save<T>(T data, string recipeName, string storageKey)` (`:53`). 경로 `Recipes/<recipeName>/<storageKey>.recipe.json` (`:181-184`). Read=DataContractJson(`:38`), Write=JsonPrettySerializer(`:66`), 원자적 tmp→Replace(`:59-72`), miss 시 `new T()`.
- **`QMC.Common.Persistence.EquipmentDataStore`** — `T Load<T>(string storageKey, string category)` / `bool Save<T>(...)`, 경로 `EquipmentData/<category>/<storageKey>.json` (`EquipmentDataStore.cs:22,47,80`). 동일 직렬화·원자성.
- **`QMC.Common.Data.Store.JsonPrettySerializer.WriteObject(Stream, Type, object[, settings])`** (`JsonPrettySerializer.cs:34,39`) — DataContractJson + UTF-8 2-space 들여쓰기 + `EmitTypeInformation.Never`.
- **`VisionConfigStore`** (기존, `vision.json`) `Load()/Save()` (`VisionConfig.cs:139,156`). **`AlgorithmCameraMapStore`** (기존, `algorithm_camera.json`) `Load()/Save()` (`AlgorithmCameraMap.cs:24,51`).

→ 셋 다 `T:new()` + DataContractJson/JsonPrettySerializer 라 **스냅샷 POCO 를 T 로 그대로 재사용 가능**. Newtonsoft 불필요.

---

## §A 제네릭 값 스냅샷 형태 (확정안)

디스크립터 값은 `object`(5 타입). DataContractJson 의 object 다형성 취약 → **문자열 스냅샷**으로 회피.

### 스냅샷 POCO (신설 `Core\Parameters\ParameterSnapshot.cs`)
```csharp
[DataContract]
public class ParameterSnapshotEntry {
    [DataMember] public string Target { get; set; }   // Setup 통합파일 구분용(파일당 다중 target)
    [DataMember] public string Key    { get; set; }
    [DataMember] public string Type   { get; set; }   // ParameterType.ToString()
    [DataMember] public string Value  { get; set; }   // InvariantCulture 인코딩
}

[DataContract]
public class ParameterSnapshot {
    [DataMember] public List<ParameterSnapshotEntry> Entries { get; set; }
    [OnDeserializing] void OnDeserializing(StreamingContext _) { Entries = new List<ParameterSnapshotEntry>(); }
    public ParameterSnapshot() { Entries = new List<ParameterSnapshotEntry>(); }   // T:new()
}
```
- **인코딩(Getter→Value)**: `Convert.ToString(getter(), CultureInfo.InvariantCulture)` (Double "3.14" 보장).
- **디코딩(Value→Setter)**: Type 으로 invariant 파싱 후 **typed object** 를 `descriptor.Setter` 에 전달 →
  Double=`double.Parse(v,Invariant)`, Int=`int.Parse(v,Invariant)`, Bool=`bool.Parse`, Text=`v`, Enum=`v`(Setter 가 Enum.Parse).
  (Setter 가 `Convert.ToDouble(object)` 시 typed double 이면 culture 무관 — 안전.)
- **`[OnDeserializing]` 기본값 주입** → 누락/구파일도 Entries 빈 리스트(G9 함정 차단).
- 파일 = 한 `ParameterSnapshot`(Entries). Recipe=타깃별 파일(Entries=그 타깃의 Recipe 디스크립터), Setup=통합 1파일(Entries=전 타깃 Setup 디스크립터, Target 으로 구분).

---

## §B 계층별 영속화 라우팅 — "신 스토어" vs "기존 위임"

| 계층 | 대상 파라미터 | 저장 방식 | 경로 | 비고 |
|---|---|---|---|---|
| **Recipe** | finder ROI(Train)·AcceptThreshold·MaxInstances / inspector ROI 임계 / ② / Camera ExposureUs·조명 Level 등 | **신 스토어**(스냅샷) `UnitRecipeStore<ParameterSnapshot>` 재사용 | `Recipes/<default>/<target>.recipe.json` | target=정규 id. **단 Camera/Lighting 의 Recipe 항목은 §아래 위임 예외** |
| **Setup** | finder SearchRoi / inspector InspectionRoi / VisionSettings Scale·방향 / ② FileSavePath / 조명 결선(Channel·Port) | **신 스토어**(스냅샷) 통합 1파일 | `Config/Setup/vision_setup.json` | Vision 박형 store(EquipmentDataStore 패턴, 경로만 Config/ 하위) |
| **Config** | VisionSettings 포트·뷰어·로그·Provider·Language·DelayBeforeGrab / Camera HW(Gain·CameraId·FrameRate…) | **기존 위임** | `vision.json` / `algorithm_camera.json` | 포맷·경로 무변경(파괴 금지) |
| **Camera/Lighting (전체)** | algorithm_camera.json 의 모든 항목(ExposureUs·조명 Level 이 Recipe 계층이어도) | **기존 위임**(P2 분리 보류) | `algorithm_camera.json` | 계층은 메타로만 표기, 물리 저장은 기존 파일. 분리 이전은 P3+ |

### 라우팅 규칙 (충돌 회피)
- **물리 저장 우선순위**: provider 의 ParameterTarget 접두로 판정.
  - `Vision/Global` → VisionConfigStore 위임(Config 항목) + 신 Setup 스토어(Scale 등 Setup 항목). ⚠ VisionSettings 는 Config·Setup 둘 다 가짐 → **Config 항목=vision.json, Setup 항목=vision_setup.json** 분리 저장(둘 다 같은 VisionSettings 객체 바인딩이라 로드 시 양쪽 주입).
  - `Camera/*`, 조명(InspectionId) → AlgorithmCameraMapStore 위임(계층 무관 통째).
  - 그 외(finder/inspector/②) → 신 스냅샷 스토어(Recipe=UnitRecipeStore, Setup=통합파일).
- 즉 **P2 신규 물리 저장 = finder/inspector/② 의 Recipe + Setup 뿐.** vision.json·algorithm_camera.json 은 기존 그대로.

> ★[확인 필요] VisionSettings 가 Config(vision.json)·Setup(vision_setup.json) 으로 쪼개지는 것 OK? 대안: VisionSettings 는 통째로 vision.json 위임(Scale 도 vision.json 에 이미 있음 — 실제 `ScaleX/Y` 는 vision.json 필드). → **권장: VisionSettings 는 통째로 vision.json 위임**(Scale 이 이미 거기 있으니 분리 불필요), Setup 통합파일엔 finder/inspector ROI 만. 이게 더 단순·비파괴. 컨펌 요청.

---

## §C 마이그레이션·호환 읽기
- 최초 `LoadAll`: 신 Recipe 경로(`Recipes/<default>/<target>.recipe.json`) 없으면 구 `Recipes/<tool>.json`(②, `ParameterEditorBase.cs:23`) 읽어 디스크립터 Setter 주입 → 신 경로 재저장. **구 파일 보존**(롤백).
- `vision.json`·`algorithm_camera.json` 그대로 사용(위임, 변환 없음).
- Side/DieGap = describe·저장은 되되 인스펙터 미연결(orphan, P3).

---

## 확정 요청 (2점)
1. **스냅샷 형태** = §A(`{Target,Key,Type,Value}` + `[OnDeserializing]`, UnitRecipeStore<ParameterSnapshot> 재사용) — OK?
2. **VisionSettings 저장**: (가) Config=vision.json·Setup=vision_setup.json **분리** vs **(나) 통째로 vision.json 위임(권장 — ScaleX/Y 이미 vision.json 필드라 단순·비파괴)**. → Setup 통합파일은 finder/inspector ROI 전용.
