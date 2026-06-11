# C3a — 구 시스템 은퇴 (Vision 국소) (Step 0 감사, 보고 후 정지)

> 작성 2026-06-11. 브랜치 `refactor-baseunit-c3a-retire`(master=C2 머지본 `74d6ba5` 위).
> **Step 0: 감사 — 보고 후 정지.** 확인 = ②fallback 처리 ③쓰기 제거 범위. 컨펌 후 Step 1~4.
> 목표: C1(카메라=모듈 SSOT)·C2(조명=노드 SSOT) 후 Vision 안에서만 구 경로 제거 — 검사별 카메라 override 드롭 / algorithm_camera.json fallback·쓰기 중단(읽기=레거시 임포트 유지). **QMC.Common·Handler 무수정.**

---

## §A 감사 결과 (코드 전수)

### A-1. 검사별 카메라 override 소비 — 생산 그랩 적용 **없음**(확정)
`ApplyOver`/`GetOrCreateOverride`/`.Inspections` 전수:
- `InspectionOverridePanel.cs`(65·69·70·71·146·164·179·201·213) — **패널 자체**. 실 `ApplyOver` = `:213` TestGrab 1곳(테스트 그랩 미리보기, 생산 아님).
- `AlgorithmCameraMap.cs:72-77` — Save 시 빈 Inspections 정리(Store 내부).
- `SettingsPage.cs:86-87` — 트리 "override 있음 ●" 뱃지.
→ **생산 그랩/TCP 적용 경로 0**. 검사별 카메라 override 는 안전하게 드롭 가능(정지조건 해당 없음).

### A-2. algorithm_camera.json 쓰기(Save) 호출처 — 5곳
| 위치 | 맥락 | C3a 처리 |
|---|---|---|
| `Form1.cs:70` | 부팅 Page/ControllerPort 마이그 후 Save | **제거**(인메모리 fixup 은 유지=노드 마이그 입력, 구파일 미저장). `:71` light_system Save 는 유지(컨트롤러 정의=보존 파일). |
| `CameraMappingPanel.cs:246` | 모듈 미해결 fallback Save | **제거**(Step 2, 명시 상태로) |
| `InspectionLightPanel.cs:258` | 노드 미해결 fallback Save | **제거**(Step 2, 명시 상태로) |
| `InspectionOverridePanel.cs:166·180` | override 패널 저장 | **제거**(Step 1, 패널 삭제) |

### A-3. Load(읽기) 호출처 — 레거시 임포트만 유지
- `Form1.cs:46` `map = AlgorithmCameraMapStore.Load()` — **유지**(읽기 전용 레거시 임포트). 소비자: 카메라 마이그(C1 InitModuleCamera)·조명 마이그(C2 MigrateModuleLights)·인메모리 Page/Port fixup.
- `SettingsPage.cs:58` `AlgorithmCameraMapStore.Load()` — 뱃지(A-1) 외 소비 없음 → 뱃지 제거 후 **불요**. [확인③ 범위]
- `CameraMappingPanel.cs:260` `if (Module()==null) Load()` / `InspectionOverridePanel.cs:188` — fallback/패널과 함께 제거.

### A-4. fallback 발동 조건 — 실사용 시 **도달 불가**(dead)
- `CameraMappingPanel`: 호스트 SettingsPage→`SelectAlgorithm` 시 `Module()`=`(FindForm() as Form1).ResolveModule(algorithm)`. Form1 안에서 항상 해결 → `Module()==null` fallback(105·246·260) 미도달.
- `InspectionLightPanel`: SettingsPage 가 `ResolveModule(alg).GetAlgorithm(짧은id)`, 타깃페이지가 참조 해석 → `_node` 항상 해결 → `_node==null` fallback(102·253·258) 미도달.
→ fallback 은 사실상 죽은 경로. 제거 시 **정상 경로 불변**. 미해결(방어적)이면 구파일 대신 **명시 상태 메시지**. [확인②]

### A-5. InspectionOverridePanel 삭제 영향
- 호스트 = **SettingsPage 단독**(`_inspPanel` 필드 `:17`, 생성 `:38`, `BuildDetailPanels` 카메라 탭, `OnOverrideChanged`, `ShowInspectionOverride`, `InspNodeText` 뱃지, `RefreshInspectionNode`).
- csproj 3엔트리: Compile `.cs`(237)·`.Designer.cs`(240)·EmbeddedResource `.resx`(292).
- 삭제 후 SettingsPage 검사 노드 = [카메라][조명] 2탭 → **조명만** 남음. `_inspTabs` 단일탭화 또는 `_lightPanel` 직접 표시로 단순화.

---

## §B 계획 (컨펌 후)

### Step 1. 검사별 카메라 override 드롭
- 삭제: `InspectionOverridePanel.cs`+`.Designer.cs`+`.resx` + csproj 3엔트리.
- SettingsPage: `_inspPanel` 필드/생성/카메라탭/`OnOverrideChanged`/`InspNodeText` 뱃지(`bm.Inspections`)/`RefreshInspectionNode`/`ShowInspectionOverride` override 부분 제거. 검사 노드 선택 = 조명 패널 표시로 단순화.
- **`InspectionCameraOverride` 타입(Common) 은 잔존**(C3b 삭제) — Vision 참조만 0.

### Step 2. fallback 경로 제거
- `CameraMappingPanel`: `LoadBuffer` 구store 분기·`SaveAll` fallback·`CancelChanges` fallback Load 제거. `Module()==null` 이면 명시 상태("운영 모듈 미해결 — 저장 불가"). 정상 경로(모듈 해결) 불변.
- `InspectionLightPanel`: `SavedSettings` 구store 분기·`Save` fallback 제거. `_node==null` 이면 명시 상태("검사 노드 미해결 — 저장 불가"). 정상 경로 불변.

### Step 3. 구 파일 쓰기 중단
- `Form1.cs:70` `AlgorithmCameraMapStore.Save()` 제거(인메모리 fixup·`:71` light_system Save 유지).
- `SettingsPage.cs:58` Load 제거(뱃지 소거로 불요). [확인③]
- `Load`(Form1:46)=레거시 임포트로 유지. algorithm_camera.json 은 디스크 잔존하되 Vision 미수정.

### Step 4. 게이트 + 보고·정지
빌드0(Common→Vision) / 정적 ic-helper-call0 / verify 코어 FAIL0 / 실동작(카메라·조명 편집→저장→재로드 복원 노드·모듈 경유 + **algorithm_camera.json 수정시각 불변=쓰기0**) / SettingsPage override 패널 소거 정상 / R2 보존 / Handler·QMC.Common 무수정.

---

## ★ 사용자 확정 (2026-06-11) — 범위 확대
2. **fallback 처리 = 명시 메시지 + 편집 비활성**. 모듈/노드 미해결 시 패널이 "설정 불러올 수 없음 — 모듈/노드 미해결" 표시 + 입력 컨트롤 `Enabled=false` + 경고 로그. 조용한 구경로·크래시 금지.
3. **구 파일 = 완전 은퇴(쓰기 + 읽기 둘 다)**. Step 3 확대:
   - **선결 검증(먼저)**: 모듈 Config·노드 Recipe JSON 에 카메라·조명 값이 실제 채워졌는지 확인. 비어 있으면(마이그 미완) **정지·보고**. + `algorithm_camera.json.bak` 백업 1부.
   - Save() 5곳 전부 제거 + **`Form1:46` Load 제거** + 입력(구파일) 없어져 죽는 **마이그 다리 호출 제거**(InitModuleCamera 의 ImportCameraMapping 마이그분기 / MigrateModuleLights→MigrateLegacyLights / Form1 의 MigrateWiringPageToSettings·FillRecipeControllerPorts).
   - 결과: **Vision 이 구 파일을 읽지도 쓰지도 않음**. 게이트=algorithm_camera.json 수정시각 불변(읽기·쓰기 0).
   - 잔존(C3b/패널용): `ImportCameraMapping`/`ExportCameraMapping`(CameraMappingPanel 카메라 편집 경로)·`AlgorithmCameraMapping` DTO 는 유지. `MigrateLegacyLights` 는 호출 0 → 메서드 제거.

### ★ 선결 검증 결과 (2026-06-11, bin/Debug 실데이터) — 통과
- 카메라: 5모듈 Config 전부 이전. 실 IP 포함(Bin `169.254.140.108`, FrontSide `192.168.2.4`).
- 조명: 구 파일 유일 조명(Bin/ReticleFinder COM4 ch2 Level1500) → 노드 Recipe.LightSettings + Setup.LightWirings([COM4 ch2]) 정확 일치.
- → 구 파일 내용이 노드/모듈에 빠짐없이 반영. **완전 은퇴 진행 가능**(데이터 손실 0).

## 정지 조건
검사별 override 생산 소비 발견 / fallback 제거가 정상 경로 깸 / Common 수정 필요 / 빌드·verify 회귀 / R2 깨짐 → 즉시 정지·보고. **Step 0 보고 전 구현 금지.**
