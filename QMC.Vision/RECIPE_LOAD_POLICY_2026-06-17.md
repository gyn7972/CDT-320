# Vision 레시피 로드 정책 설계 (2026-06-17)

핸들러(서버)–Vision 간 레시피 식별/로드 정책. 목표: **핸들러와 동일한 레시피 식별 단위**로 통일하고, **시작/변경 시 올바른 레시피를 자동 로드**한다.

## 1. 목표 동작 (요구사항)

| # | 상황 | 동작 |
|---|------|------|
| A | Vision 부팅 + 핸들러 꺼짐(미접속) | 로컬에 기억한 **마지막 레시피** 로드 |
| B | Vision 부팅 + 핸들러 켜짐(접속됨) | 핸들러가 **현재 레시피 1회 push** → 그걸로 로드/셋팅 |
| C | 운영 중 핸들러가 **레시피 변경** | 핸들러 broadcast → Vision 따라서 로드 |

→ B·C에서 로드할 때마다 **마지막 레시피(번호+명칭)를 로컬에 저장** → 다음 부팅 시 A에 사용.

## 2. 현재 상태 (코드 확인 결과)

### 동일한 부분 (핸들러와 이미 일치)
- 베이스: `QMC.Common`의 `BaseEquipmentNode → Machine/BaseUnit/BaseComponent`, 마커 `ISetupData/IConfigData/IRecipeData`.
- 저장소/직렬화: `RecipeDataStore/UnitDataStore/JsonDataStore` + `DataContractJsonSerializer`.
- 파일명: `{StorageKey}.recipe.json`, 경로 `Recipes/{recipeName}/{StorageKey}.recipe.json`.
- 런타임 로드: `MainCommServer.DoRecipe` → `VisionMachine.LoadRecipe(명칭)` cascade. **핸들러 규약과 동일.**

### 끊어진 부분 (이번에 정렬)
- **현재 레시피 명칭 SSOT** = `VisionMachine.Recipe.RecipeName` / `RecipeNo` (`MainCommServer.DoRecipe` 159–161에서 세팅).
- 그러나 **레시피 에디터**(`VisionTargetPage.cs:27`, `InspectorTargetPage.cs` 동일)는 `RecipeName = "default"` **상수 고정**.
  - `TargetPath()`, `SaveTarget()`(`_node.SaveRecipe("default")`), `LoadTarget()`(`_node.LoadRecipe("default")`) 전부 "default" 사용.
  - 결과: 사람이 편집/저장하는 곳은 `Recipes/default/...`, 장비가 로드하는 곳은 `Recipes/{명칭}/...` → **서로 안 만남.** (= 레시피 못 불러옴의 한 원인)
- **로컬 "마지막 레시피" 영속화 없음** (핸들러엔 `Recipes/.last_project` 마커 존재, CDT-300엔 `Config\Desktop.Config`).
- **핸들러 접속 시점 1회 전송(send-on-connect) 없음** — 현재는 레시피 *변경* 시에만 `VisionHub.BroadcastRecipeAsync` push (핸들러 `Form1.LoadMachineRecipe`).

## 3. 설계

### 3.1 현재 레시피 명칭 = 단일 SSOT
- `VisionMachine.Recipe.RecipeName` 을 활성 레시피 명칭으로 사용. 미설정 시 `"default"`.
- 편의 접근자 추가: `VisionMachine.CurrentRecipeName => string.IsNullOrWhiteSpace(Recipe?.RecipeName) ? "default" : Recipe.RecipeName`.

### 3.2 에디터 "default" → 활성 명칭 연동
- `VisionTargetPage` / `InspectorTargetPage` 의 고정 `"default"` 제거.
- 명칭을 **생성 시 주입**받는다(생성자 인자 `recipeName` 또는 `Func<string>` 공급자). 이유: 타깃 페이지는 `RecipePage.ShowSetting`에서 **폼에 추가되기 전 생성자에서 `LoadTarget()`** 하므로 `FindForm()` 이 아직 null → 그 시점 명칭 확보 불가. 따라서 RecipePage가 호스트(`FindForm() as Form1`)의 `Machine.CurrentRecipeName`을 읽어 주입.
- `RecipePage`는 호스트 `Form1.Machine` 참조 가능(이미 `BuildSidebar`에서 `FindForm() as Form1` 사용).

### 3.3 레시피 변경 시 에디터 갱신
- 레시피 변경(TCP) → `RecipePage`의 캐시된 타깃 페이지(`_cache`) **무효화 후 재바인딩**(현재 명칭으로 재로드). 또는 표시 중 페이지만 reload.
- 트리거: `VisionMachine`(또는 Form1)에 `RecipeChanged` 이벤트 추가 → MainComm 수신·boot 로드 시 발생 → RecipePage 구독.

### 3.4 마지막 레시피 영속화 (`.last_recipe`)
- 위치: `Recipes/.last_recipe` (핸들러 `.last_project` 와 동격, plain text: `번호|명칭` 또는 JSON).
- 쓰기: Vision이 레시피를 로드할 때마다(=`VisionMachine.LoadRecipe` 성공 직후, 또는 MainComm 수신부) 기록.
- 읽기: 부팅 시(Form1_Load) 로드.

### 3.5 부팅 정책 (Form1_Load)
1. `.last_recipe` 있으면 그 명칭으로 `Machine.LoadRecipe(명칭)` (시나리오 A 충족 — 핸들러 없어도 동작).
2. 핸들러가 접속/전송하면 그 RECIPE로 덮어씀(B·C). → 자연스럽게 override.

### 3.6 핸들러측 보강
- **send-on-connect**: 핸들러가 Vision(MainComm 5104)에 연결될 때 **현재 레시피를 1회 전송**.
  - 위치: 핸들러 `VisionHub`/`VisionTcpClient` 연결 성공 콜백에서 `BroadcastRecipeAsync(현재no, 현재명칭)` 호출.
- **change 시 broadcast**: 기존 `Form1.LoadMachineRecipe → BroadcastRecipeAsync` 유지(확인).

## 4. 변경 파일 (C:/V: 동시)

| 단계 | 파일 | 변경 |
|------|------|------|
| 1 | `QMC.Vision/Equipment/Modules/VisionMachine.cs` | `CurrentRecipeName` 접근자, `RecipeChanged` 이벤트 |
| 1 | `QMC.Vision/Ui/Pages/Recipe/VisionTargetPage.cs`, `InspectorTargetPage.cs` | `"default"` 제거 → 주입된 명칭 사용 |
| 1 | `QMC.Vision/Ui/Pages/Recipe/RecipePage.cs` | 타깃 페이지 생성 시 `Machine.CurrentRecipeName` 주입, `RecipeChanged` 구독→캐시 무효화 |
| 2 | `QMC.Vision/Equipment/.../RecipeMarker`(신규 또는 기존 store) | `.last_recipe` read/write |
| 2 | `QMC.Vision/Form1.cs` | 부팅 시 `.last_recipe` 로드, MainComm 수신 시 마커 갱신 |
| 3 | `QMC.CDT-320/.../VisionHub.cs` / `VisionTcpClient.cs` / `Form1.cs` | 접속 시 현재 레시피 1회 전송 |
| 4 | `QMC.Vision/Equipment/Comm/MainCommServer.cs` | 수신 로드 후 마커 갱신 + `RecipeChanged` 발생 일원화 |

## 5. 리스크 / 주의
- **타이밍**: 타깃 페이지 생성자 `LoadTarget()`가 폼 부착 전 실행 → 명칭은 반드시 **주입**(FindForm 의존 금지).
- **캐시**: 레시피 변경 시 `RecipePage._cache`/`_settings` 페이지들이 옛 명칭 데이터를 들고 있으므로 무효화 필요.
- **기존 데이터**: 과거 `Recipes/default/...` 파일은 새 명칭 폴더로 자동 이전 안 됨 → 재저장 또는 1회 마이그레이션(선택).
- **catch{} 침묵**: `LoadTarget`/`LoadRecipe`가 예외를 삼킴 → 로드 실패가 조용히 묻힘. 진단 로그 1줄 추가 권장.

## 6. 진행 순서
1단계(식별 단위 통일) → 2단계(마지막 레시피 + 부팅 로드) → 3단계(핸들러 send-on-connect) → 4단계(수신부 일원화). 각 단계 후 빌드/동작 확인, C:/V: 동시 반영.
