# LIGHT_TO_MODULE_PLAN — 조명 컨트롤러/페이지 지정을 검사(노드) → 모듈로 이전

브랜치 `refactor-light-assign-to-module` (master 분기). Vision 전용 / Handler·QMC.Common 무수정.
물리: 카메라 1 = 모듈 1 = 스트로브 조명 1(트리거 직결). ⇒ **지정(컨트롤러/페이지)=모듈 하드웨어**, **레벨값=검사별(노드 Recipe 유지)**.
게이트 R2: **CameraMappingPanel 은 의도적 2열 재배치라 R2 재베이스라인 / 타 페이지 R2 보존.**

---

## 확인된 현재 구조 (file:line)
- 지정(이전): `AlgoSetupBase.LightPages` — [AlgorithmData.cs:29](../QMC.Vision/Modules/AlgorithmData.cs). 영속 `EquipmentData/Setup/<module>.<algo>.json`.
- 레벨(유지): `AlgoRecipeBase.LightSettings` — [AlgorithmData.cs:41]. 영속 `Recipes/<recipe>/<module>.<algo>.json`.
- `LightPageRef` = **QMC.Common** [InspectionLightSubset.cs:40](../QMC.Common/Recipes/InspectionLightSubset.cs) — **무수정**(소속 프로퍼티만 Vision 측에서 이동).
- 모듈 Setup base `VisionModuleSetupBase`(현 `IsSimulationMode` 만) — [VisionModuleData.cs:15]. 영속 `Setup/<module>.json`.
- 모듈 해석 `Form1.ResolveModule(alg):IVisionModule`(switch). 노드 해석 `mod.GetAlgorithm(id)`.
- CameraMappingPanel: `Module()` 해석, `SaveAll()`→`mod.ImportCameraMapping(_buffer); mod.SaveSettings(); mod.SaveRecipe("default")` — [CameraMappingPanel.cs:230-248]. **모듈 SaveSettings 이미 호출** ⇒ 모듈 Setup.LightPages 자동 영속.
- 검사별 지정 UI `InspectionLightAssignPanel`: 그리드 2 ComboBox 컬럼(`_colCtrl`=PortName, `_colPage`=PageCount, **string items**=표시버그 회피), 그리드 내장 행추가(NewRow)/삭제(RowsRemoved), `PersistAssign`→`setup.LightPages=list; _node.SaveSettings()`. 페이지 콤보 per-cell items=SetPageCellItems.
- 레벨/점등 `InspectionLightPanel.ActivePages()` [InspectionLightPanel.cs:113-124] = **`_node.Setup.LightPages`**. 점등 `Apply()`→`ApplyControllerAsync()`→`ctrl.SetChannelBatchAsync(page, int[ChannelCount])`(레벨=Recipe).
- 트리 `SettingsPage` [:48-55] `cam:<alg>`/`cam:<alg>:<insp>`, [:68-101] AfterSelect→ShowCameraMapping/ShowInspectionLight, `_lightAssignPanel` [:17,36,118].

---

## Step 0 결정 (보고 후 정지) — 1·2·3·4 확인

### (1) 데이터 이전 — 확정안
`AlgoSetupBase` 에서 `LightPages` 제거 → `VisionModuleSetupBase` 에 `[DataMember] List<LightPageRef> LightPages` + `[OnDeserializing]` 빈 리스트(비파괴) 추가. 구 노드 json 의 `LightPages` 키는 로드 시 무시(비파괴, 구 파일 보존).

### (2) 마이그레이션 — 확정안(합집합 dedupe, raw 직독)
프로퍼티 제거로 DataContract 로는 구 노드 LightPages 를 못 읽으므로, 부팅 시 **각 노드의 구 Setup json(`Setup/<module>.<algo>.json`)에서 `LightPages` 배열을 경량 DTO 로 1회 raw 직독**(기존 JSON 직렬기 재사용) → 모듈 Setup.LightPages 로 **합집합 dedupe(키=ControllerPort.ToUpper()+"/"+Page)**, **빈 모듈만**, 구 파일 보존. 물리 동일이라 보통 1건 수렴, 다르면 전부 보존.
- 보강: 노드 Recipe.LightSettings 의 (Port,Page) 도 합집합에 포함(지정-only 가 아닌 레벨-only 데이터도 흡수). 둘 다 비면 스킵.

### (3) 채널 열거·점등 출처 변경 — 확정안
`InspectionLightPanel.ActivePages()` 가 `_node.Setup` 대신 **소속 모듈 Setup.LightPages**: `(FindForm() as Form1)?.ResolveModule(_algorithm)` → `(mod.Setup as VisionModuleSetupBase)?.LightPages`. 레벨(LightSettings)·점등(SetChannelBatchAsync)·컨트롤러 정의 불변. 마이그 전 표시용 폴백(Recipe 도출) 유지. 모듈 미해결 시 빈 목록.

### (4) CameraMappingPanel 2열 재정렬 + 조명 섹션 — 확정안 (좌표)
**좌측 컬럼(X=20..670, 위치 불변)**: 카메라 필드(Y 28..305) + DCF(`_chkMilDcf` 320 / `_lblMil`·`_txtMilDcf` 352, 조건부) **그대로** → 그 아래 **조명 섹션 신설**:
- `_lblLightAssign`(20, 392, 320×22) "조명 컨트롤러/페이지 지정"
- `_gridLightAssign`(20, 418, 620×150) — ComboBox 2컬럼(ControllerPort/Page, string items), AllowUserToAddRows/Delete
- `_lblLightStatus`(20, 572, 620×22)

**우측 컬럼(X=700..1120, ~420w)**: 프리뷰·버튼·상태줄 이동:
- 버튼 재적층: `_btnSave`(700,40,100)·`_btnCancel`(810,40,90)·`_btnReset`(910,40,110) / `_btnApply`(700,80,150)·`_btnTestGrab`(860,80,120) / `_btnConnect`(700,120,100)·`_btnLiveStart`(810,120,100)·`_btnLiveStop`(920,120,100)
- `_lblStatus`(700,160,420×22)
- **`_picPreview`(700,190, 420×236)** — 16:9 유지하며 **640×360 → 420×236 축소**(우측 컬럼 폭 제약). ⇒ **★ 확인 필요(아래)**

좌측 끝 Y≈594, 우측 끝 Y≈426 — 1140×595 _body 내 스크롤 없이 동시 표시. Designer 인라인 선언 유지, 재배치 후 디자이너 로드 가능 확인. 핸들러/값 바인딩 불변(위치만).

**★ 확인**: 우측 컬럼 폭(~420)상 프리뷰 640px 유지 불가 → **(A) 420×236 축소**(권고, 스크롤 없음) vs **(B) 프리뷰만 하단 전폭(Y≥440, 640 유지)·버튼만 우측**.

### (5)(6) UI 이식 / 트리 정리 — 확정안
이식: AssignPanel 그리드 로직(컨트롤러=PortName, 페이지=PageCount string items, per-cell items, NewRow/RowsRemoved, 표시버그 회피)을 CameraMappingPanel 새 섹션으로. `SaveAll()` 에 LightPages 수집 추가(모듈 Setup.LightPages=list, 이미 mod.SaveSettings 호출).
트리: `cam:<alg>:<insp>` 생성([:54-55]) + `ShowInspectionLight`([:95-101]) + `_lightAssignPanel`([:17,36,118]) 제거. `InspectionLightAssignPanel`(.cs/.Designer.cs)+csproj 삭제. 모듈 노드(`cam:<alg>`)=카메라+조명.

---

## Step 1 데이터 이전 + 마이그 (백엔드)
AlgorithmData: LightPages 제거. VisionModuleData: VisionModuleSetupBase 에 추가(+OnDeserializing). VisionModule: 모듈 마이그(raw 직독 union-dedupe, 빈 모듈만, 구 파일 보존) — 기존 `MigrateLightPages` 대체/재배선. Form1 부팅 호출부 점검. 빌드0 + 왕복 스모크.

## Step 2 출처=모듈 (백엔드) — **체크포인트 커밋**
InspectionLightPanel.ActivePages()/점등 모듈 LightPages 읽도록. 레벨/점등/컨트롤러 불변. 스모크(레벨 채널이 모듈 지정 기준).

## Step 3 CameraMappingPanel 2열 + 조명 섹션 (UI)
Step0-4 좌표로 Designer 재작성(프리뷰·버튼·상태 우측, 좌하단 조명 그리드). 코드: 조명 그리드 바인딩/Persist 이식, SaveAll 에 LightPages 수집. 정적0·빌드0·디자이너 로드.

## Step 4 검사 하위노드·AssignPanel 제거 (UI) — **체크포인트 커밋**
SettingsPage 트리/Show/필드 제거, AssignPanel 3파일+csproj 삭제. RecipePage 레벨 편집 유지. 빌드0.

## Step 5 게이트 + 보고
전체 빌드0(Common·Handler·Vision)/정적0/verify0/실동작(모듈 지정→검사 레벨→저장→재시작 복원+Sim 점등; 하위노드 소멸·모듈 2열; MIL DCF 무충돌; 우측 프리뷰·버튼 동작)/CameraMappingPanel R2 재베이스라인·타페이지 R2 보존/디자이너 로드/Handler·Common 무수정/push 안 함.

## 정지 조건
이전이 마이그·영속 손상 / 모듈 해석 불가 / 조명 섹션 DCF·기존 컨트롤 충돌 / 빌드·verify 회귀 / 점등 안 됨 / 디자이너 로드 실패 / 타페이지 R2 깨짐 → 즉시 정지·보고.
