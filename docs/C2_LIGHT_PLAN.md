# C2 — 조명 설정 BaseUnit 흡수 (Step 0 설계, 보고 후 정지)

> 작성 2026-06-11. 브랜치 `refactor-baseunit-c2-light`(master=C1 머지본 `613b15c` 위).
> **Step 0: 설계 — 보고 후 정지.** 사용자 ★ 사전확정 반영, 남은 확인 = ①base 형태 ②매핑 불일치. 컨펌 후 Step 1~3.
> 목표: 검사별 조명을 **알고리즘 노드 BaseUnit** 으로 흡수 — 결선=노드 Setup / 레벨=노드 Recipe. 컨트롤러 정의·LightHub 유지. 구 데이터 보존.

---

## ★ 사용자 사전확정 (2026-06-11 — 재질문 금지)
- **매핑 = 키(ControllerPort+Channel)**. 이미 코드가 그렇게(`InspectionLightSetting.ControllerPort+Channel` ↔ `ControllerChannels.Channels` 풀).
- **타입 = 기존 QMC.Common.Recipes 재사용**(신규 타입 안 만듦, Common 무수정):
  - 노드 Setup += `List<ControllerChannels> LightWirings`
  - 노드 Recipe += `List<InspectionLightSetting> LightSettings`
  - 식별자 래퍼(`AlgorithmLightWiring.Algorithm`/`InspectionLightOverride.InspectionId`)는 떼고 **내부 리스트만** 흡수(노드=알고리즘 1개). 검증된 Clone/직렬화/마이그레이터 재사용.
- **배치 = 공통 base 도입**. Finder/Inspector 공통 base 에 조명 리스트 1회 정의 → 4개 구체가 상속. 기존 필드 유지.
- **런타임 점등 = 패널 2곳 전제**(`InspectionLightPanel.cs:280`·`LightLiveTuningPanel.cs:163`. 생산 시퀀스 자동점등 미구현 — `StabilizeDelayMs` 미연결, 향후 소비자도 노드에서 읽음).

---

## §A 현재 조명 구동 (코드, 확인됨)
- **채널 풀(결선)** = `LightSystemSetup.AlgorithmWirings`(알고리즘별 `AlgorithmLightWiring.ControllerSets[ControllerChannels{ControllerPort, Channels[]}]`). 알고리즘 레벨, `LightSystemSetupPage` 편집. 컨트롤러 능력(MaxPower/PageCount/ChannelLabels)도 여기. **유지.**
- **검사별 레벨** = `AlgorithmCameraMapping.InspectionLights`(`algorithm_camera.json`) → `InspectionLightOverride{InspectionId, Settings[]}` → `InspectionLightSetting{ControllerPort, Channel, Level, On, StrobeTimeUs, StabilizeDelayMs, Page}`(`InspectionLightSubset.cs:11`). **C2 흡수 대상.**
- **UI/소비**: `InspectionLightPanel`(`BindFieldsCore:106` 풀=LightSystemSetup·레벨=`GetLightOverride:137`; `Save:209`→`AlgorithmCameraMapStore.Save`; `Apply:222`→`LightHub.Get(port).SetChannelBatchAsync:280`). 호스트 5페이지가 `_finder/_inspector.Id` 로 공급(`FinderPage:46,126`·`InspectorPage:121`·`VisionTargetPage:116`·`InspectorTargetPage:117`). `LightLiveTuningPanel:163`(프리뷰, 행 출처=호스트 `CollectRowsForLiveTuning`).
- **런타임 점등 전수**: `SetChannelBatchAsync` 실 호출 = 패널 Apply/Test + 라이브튜닝 **2곳뿐**(VisionTcpServer·Grab 경로 매치 0). LightHub 의 나머지 호출은 Initialize/Connect/Disconnect 생애주기.

---

## §B 설계 (Step 0 — ①②만 컨펌)

### B-1. 공통 base 도입 형태 — [확인①]
직렬화 점검 결과(**확정 근거**):
- 노드 영속은 `EquipmentDataStore.Save<T>/Load<T>` · `UnitRecipeStore.Save<T>/Load<T>` 가 **`typeof(T)` = 제네릭 구체 타입**(`FinderAlgoSetup` 등)으로 `DataContractJsonSerializer` 생성(`EquipmentDataStore.cs:32,60`). 즉 **구체 타입 직렬화**(다형성 없음, EmitTypeInformation.Never).
- → **추상 base 클래스 안전**: `[DataContract]` 추상 base 의 `[DataMember]` 는 구체 타입 직렬화에 **자연 포함**, `[KnownType]` 불요. 인터페이스 우회 불필요.
- **기존 파일 호환**: 구 JSON 에 `LightWirings`/`LightSettings` 키 없음 → 로드 시 null → base `[OnDeserializing]` 로 빈 리스트 초기화(NRE 방지). 비파괴.

설계(권장):
```
[DataContract] public abstract class AlgoSetupBase : ISetupData {
    [DataMember] public List<ControllerChannels>     LightWirings { get; set; }
    [OnDeserializing] void OnD(StreamingContext c){ LightWirings ??= new(); }   // + ctor 초기화
}
[DataContract] public abstract class AlgoRecipeBase : IRecipeData {
    [DataMember] public List<InspectionLightSetting> LightSettings { get; set; }
    [OnDeserializing] void OnD(StreamingContext c){ LightSettings ??= new(); }
}
class FinderAlgoSetup   : AlgoSetupBase  { }                       // 기존 빈 클래스
class InspectorAlgoSetup: AlgoSetupBase  { CalibModelPath ... }    // 기존 필드 유지
class FinderAlgoRecipe  : AlgoRecipeBase { AcceptThreshold/Roi ... }
class InspectorAlgoRecipe:AlgoRecipeBase { InspectionRoi/Threshold ... }
```
- 파생 클래스의 기존 `[OnDeserializing]`(InspectorAlgoSetup/Finder·InspectorRecipe)와 base `[OnDeserializing]` 은 **둘 다 실행**(DataContract 가 계층별 호출) — 충돌 없음.
- [확인①] (가) **추상 base 클래스** [권장, 위] vs (나) 인터페이스 `IHasLightWirings`/`IHasLightSettings` + 공유 헬퍼(직렬화는 각 구체에 멤버 직접 — base 안 쓰면 4중복, 권장 안 함). → 직렬화 안전 확인됐으므로 (가) 권장.

### B-2. 노드 매핑 — [확인②] ★ 불일치 발견
- `module.GetAlgorithm(id)` 는 **짧은 등록 id**("EjectPinFinder")로 키잉(`VisionModule.cs:RegisterAlgorithm`, `_algoById[id]`).
- 그러나 `finder.Id`/`inspector.Id` = `Backend.Create*(Name+"/"+id)` → **전체명**("WaferVision/EjectPinFinder", `VisionModule.cs:133`·`SimPatternFinder.cs`·`CognexPatternFinder.cs:14`). 호스트가 이 전체명을 InspectionId 로 사용(`FinderPage:46` `new InspectionLightPanel(AlgorithmKey, _finder.Id)`) → `algorithm_camera.json` `InspectionId` 도 전체명.
- **결론**: `module.GetAlgorithm(_finder.Id)` 는 **null**(전체명≠짧은키). 프롬프트 Step0-2 의 "검사id↔노드 id 일치" 는 **그대로면 전부 불일치**.
- **해소(권장)**: 노드 해석을 **참조 기반**으로 — `module.Algorithms.FirstOrDefault(a => a.Finder == _finder || a.Inspector == _inspector)`(VisionTargetPage 가 이미 쓰는 패턴, `VisionTargetPage.cs`). 호스트 5페이지가 `_finder`/`_inspector` 객체 보유하므로 안전. id 문자열 매칭 폐기.
- 마이그/저장 식별: 노드↔구 override 는 `finder.Id`(전체명) == 저장된 `InspectionId`(전체명)로 1:1 — 양쪽 전체명이라 일치.
- [확인②] 해소책 (가) **참조 기반 해석** [권장] vs (나) GetAlgorithm 에 전체명/짧은id 양쪽 키 등록(표면 변경 큼) vs (다) 호스트가 전체명→짧은id 파싱(취약).

### B-3. 마이그레이션 매핑 규칙
- 트리거: 부팅 시 노드 Setup/Recipe 에 조명 비어있음 + 구 데이터 존재.
- 매핑: 각 모듈의 각 노드에 대해 `node.Finder?.Id ?? node.Inspector?.Id`(전체명) → `AlgorithmCameraMapStore.Current.Get(module.AlgorithmKey).GetLightOverride(전체명)` 조회.
  - 있으면: `InspectionLightOverride.Settings` → 노드 `Recipe.LightSettings`(전 필드 복사, 검증된 `InspectionLightSetting.Clone()` 재사용). 결선 = 그 Settings 의 (port,ch) → `LightSystemSetup.GetWiring(AlgorithmKey).ControllerSets` 중 해당 채널 포함 set 들 → 노드 `Setup.LightWirings`(`ControllerChannels.Clone()`).
- 알고리즘명↔노드 StorageKey: `module.AlgorithmKey`(=VisionAlgorithm.*) 로 구 store 조회, 노드는 `module.Algorithms` 순회. 노드 StorageKey=`<ModuleName>.<짧은id>` 는 파일 분리용(매핑엔 finder.Id 전체명 사용).
- **구 파일(algorithm_camera.json·light_system.json) 보존**(C3 까지).

### B-4. UI 재배선 범위 (Step 2)
- `InspectionLightPanel`: 노드 컨텍스트 주입(`SelectInspection` 에 노드 또는 module+finder/inspector). BindFieldsCore 레벨 출처 = `_node.Recipe.LightSettings`(키 (port,ch) 조회), 풀/라벨/능력 = `LightSystemSetupStore` 그대로. Save = `_node.Recipe.LightSettings`+`Setup.LightWirings` 갱신 → `_node.SaveSettings()`/`SaveRecipe("default")`. Apply = `LightSettings` → 기존 LightHub 점등(**SetChannelBatchAsync 로직 불변**).
- 호스트 5페이지: 패널에 노드 전달(참조 해석 §B-2). `CollectRowsForLiveTuning` 출처 = `GetLightOverride` → `_node.Recipe.LightSettings`.
- `LightLiveTuningPanel`: 행은 호스트가 노드에서 공급, 점등 불변.
- `LightSystemSetupPage`(컨트롤러/풀) = **유지**. R2·디자이너 보존, 정적 ic-helper-call 0.

---

## ★ 확인 필요 (Step 0 정지 — 2건만)
1. **공통 base 형태**: (가) **추상 base 클래스**(AlgoSetupBase/AlgoRecipeBase) [권장 — 직렬화 구체타입 기반이라 안전 확인] vs (나) 인터페이스+헬퍼(4중복).
2. **노드 매핑 불일치 해소**: finder.Id=전체명 ≠ GetAlgorithm 짧은키. (가) **참조 기반 해석**(a.Finder==_finder) [권장] vs (나) GetAlgorithm 양쪽키 등록 vs (다) 전체명 파싱.

## 게이트 (Step 3)
빌드0(Common→Vision, **Common 무수정**) / 정적 ic-helper-call0 / verify 코어 FAIL0 / **실동작**(레벨·채널 편집→저장→재로드 복원 + Apply→LightHub Sim 점등, `Recipes/default/<모듈.알고>.recipe.json`+Setup 에 조명 기록) / R2 보존 / Handler 무수정.

## 정지 조건
base 직렬화 깨짐 / 매핑 불일치 미해소 / 런타임 지점 누락 / 빌드·verify 회귀 / 점등 안 됨 / R2 깨짐 → 즉시 정지·보고. **Step 0 보고 전 구현 금지.**
