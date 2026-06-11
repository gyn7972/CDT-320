# C3b-2 — InspectionLightOverride 제거 (노드 직접화) (Step 0 확인, 보고 후 정지)

> 작성 2026-06-11. 브랜치 `refactor-baseunit-c3b2-lightoverride`(master=C3b-1 머지본 `780b1e5` 위).
> **Step 0: 확인 — 보고 후 정지.** 확인 = ①참조0 ③조명멤버 제거 안전성. 컨펌 후 Step 1~4.
> 목표: `InspectionLightPanel.Collect()` 를 노드 Recipe(`List<InspectionLightSetting>`) 직접 생성으로 재배선 → 죽은 `InspectionLightOverride` 타입 + `AlgorithmCameraMapping` 잔존 조명 멤버 제거. 결과: `AlgorithmCameraMapping` = **순수 카메라 DTO**.

---

## §A 감사 결과 (솔루션 전체 grep)

### A-1. `InspectionLightOverride` 참조 (정의 외) — 2곳뿐
- `QMC.Vision/Ui/Pages/InspectionLightPanel.cs:217` `Collect()` 가 UI→`InspectionLightOverride` 생성(중간산출).
- `QMC.Common/Recipes/AlgorithmCameraSubset.cs` 의 `AlgorithmCameraMapping.InspectionLights`(멤버)·`GetOrCreateLightOverride`·`GetLightOverride`·Clone 블록.
- **마이그/테스트/기타 참조 0**(C2LightSmoke 는 주석 1줄). → 정지조건 해당 없음.

### A-2. Collect() 소비 경로
- `Collect()` 호출 = `Save()`(`:245`)·`Apply()`(`:271`) **2곳**. 둘 다 `ov.Settings`(=`List<InspectionLightSetting>`)만 사용:
  - Save: `recipe.LightSettings = ov.Settings`(노드 Recipe, C3a). 노드 해석은 참조 기반(`_node`).
  - Apply: `ov.Settings` 를 컨트롤러별 group → LightHub 점등.
- → Collect 반환 타입을 `List<InspectionLightSetting>` 로 직접화, 소비처는 `.Settings` 제거하고 리스트 직접 사용. **InspectionId 래퍼 불필요**(노드가 검사 단위).

### A-3. 조명 멤버 제거 안전성 (AlgorithmCameraSubset.cs)
- `GetLightOverride`/`GetOrCreateLightOverride`/`AlgorithmCameraMapping.InspectionLights` 외부 참조 **0**(노드 SSOT 로 대체됨, C2).
- **연쇄 죽은 메서드**: `AlgorithmCameraSubset.MigrateWiringPageToSettings`(`:256`)·`FillRecipeControllerPorts`(`:279`) 가 `alg.InspectionLights` 순회 → InspectionLights 제거 시 컴파일 깨짐. 둘 다 **외부 참조 0**(C3a 가 Form1 호출 제거) → 함께 제거.
- `InspectionLightSetting`(유지 타입): Handler 참조 **0**, `LightSystemSetup.cs:129` 는 **주석만**(코드 의존 0) → 오세국 공유파일 무영향.

---

## §B 계획 (Step 1~3)

### Step 1. InspectionLightPanel 노드 직접화 (Vision)
- `Collect()` 반환 `InspectionLightOverride` → `List<InspectionLightSetting>` 로 변경(그리드 → 설정 리스트 직접 생성, InspectionId 래퍼 제거).
- `Save()`: `var settings = Collect(); settings.RemoveAll(...); recipe.LightSettings = settings;`(노드 경유, C2 동작 불변).
- `Apply()`: `var settings = Collect(); settings.GroupBy(...)`(LightHub 점등 로직 불변).

### Step 2. AlgorithmCameraMapping 조명 멤버 제거 (Common)
`QMC.Common/Recipes/AlgorithmCameraSubset.cs` 에서 제거:
- `AlgorithmCameraMapping.InspectionLights` 멤버 + `GetOrCreateLightOverride` + `GetLightOverride` + Clone 의 InspectionLights 블록.
- `AlgorithmCameraSubset.MigrateWiringPageToSettings` + `FillRecipeControllerPorts`(죽은 메서드, InspectionLights 의존).
- 결과: `AlgorithmCameraMapping` = 순수 카메라 DTO. **카메라 필드·VisionAlgorithm·InspectionLabel·AlgorithmCameraSubset(Items/Get/MigrateLegacyAlgorithmNames/EnsureDefaults/Clone) 보존.**

### Step 3. InspectionLightOverride 타입 삭제 (Common)
`QMC.Common/Recipes/InspectionLightSubset.cs` 에서 `InspectionLightOverride` 클래스 제거. **`InspectionLightSetting`(노드 Recipe 원소) 보존 — 파일 유지.**

### Step 4. 게이트
전체 빌드0(Common→Handler→Vision) / 정적0 / verify 코어 FAIL0 / 실동작(조명 편집→저장→재로드 복원 노드 경유 + Apply→LightHub Sim 점등) / 스모크 C2Light·PanelLight / R2 보존 / Handler 무수정·빌드0 / push 안 함.

---

## ★ 확인 필요 (2건)
1. **참조 0**: `InspectionLightOverride` = InspectionLightPanel.Collect + AlgorithmCameraMapping 조명멤버 외 참조 **0**(마이그/테스트 0). 이 전제로 제거 진행 OK?
3. **AlgorithmCameraMapping 조명멤버 제거**: `InspectionLights`/`GetLightOverride`/`GetOrCreateLightOverride` + 연쇄 죽은 `MigrateWiringPageToSettings`/`FillRecipeControllerPorts` 제거 → 순수 카메라 DTO. 외부 참조 0·Handler 0 확인됨. OK?

## 정지 조건
InspectionLightOverride 가 패널 외(마이그 등)서도 쓰임 발견 / Collect 재배선이 C2 동작 깸 / 조명멤버 참조 잔존 / 빌드·verify 회귀 / 점등 안 됨 / R2 깨짐 → 즉시 정지·보고. **Step 0 보고 전 구현 금지.**
