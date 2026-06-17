# QMC.Vision 구조 정렬 설계서 (핸들러 기준)

> 목적: `QMC.Vision`을 `QMC.CDT-320` 핸들러와 **동일한 구조**로 맞춘다.
> 본 문서는 "전체 구조"를 먼저 확정하기 위한 설계서이며, 코드 착수 전 합의용이다.
> 범위는 **① 전체 연결 구성**, **② UI 구성 및 배치** 두 축으로 한다.

작성 기준일: 2026-06-17

---

## 0. 합의된 방향 (전제)

- Vision은 핸들러와 **무조건 동일한 구조** (데이터 모델·UI 모두).
- 단, 기계 도메인이 다른 핸들러 전용 탭(Work/WorkInfo) 자리는 **Vision 대응 탭(Operation/DataLog)** 으로 채우는 선까지를 "동일"로 본다.
- 레시피 동기화는 **이름 전송(B-1)** 방식. 핸들러가 레시피 데이터의 SSOT이며, 변경 시 **레시피번호 + 레시피명칭**만 전송한다. Vision은 같은 이름의 로컬 레시피를 로드한다.
- 전역 통신(레시피/알람 등 특정 카메라에 속하지 않는 신호)은 **Main(5104) 채널**에 모은다.

---

## 1. 전체 연결 구성

### 1.1 채널 토폴로지 (핸들러 ↔ Vision)

핸들러는 `VisionHub`(클라이언트 6개)로 접속하고, Vision은 모듈별 `VisionTcpServer`로 listen 한다.
프로토콜: 라인 단위 UTF-8, `MODULE|CMD|args` → `ACK|MODULE|CMD|result` / `ERR|...`. 비동기 푸시는 `EPD|`(노광완료), `ARM|`(알람).

| 채널 | 포트 | 모듈명(SSOT) | 핸들러 클라이언트 | Vision 서버 | 현재 상태 |
|---|---|---|---|---|---|
| Wafer | 5100 | `WaferVision` | `VisionHub.Wafer` | `_svrWafer` | ✅ 양쪽 존재 |
| Inspection(Bottom) | 5101 | `BottomInspection` | `VisionHub.Inspection` | `_svrBottom` | ✅ 양쪽 존재 |
| Bin | 5103 | `BinVision` | `VisionHub.Bin` | `_svrBin` | ✅ 양쪽 존재 |
| **Main** | **5104** | **`MainComm`** | `VisionHub.Main` | **(없음)** | ⚠️ **핸들러만 존재, Vision 서버 미구현** |
| TopSide | 5105 | `TopSideVision` | `VisionHub.TopSide` | `_svrTopSideVision` | ✅ 양쪽 존재 |
| BottomSide | 5106 | `BottomSideVision` | `VisionHub.BottomSide` | `_svrBottomSideVision` | ✅ 양쪽 존재 |

> **모듈명은 핸들러가 SSOT.** 측면검사는 `TopSideVision` / `BottomSideVision`으로 통일.

### 1.2 핵심 갭

- **MainComm(5104) 수신 서버가 Vision에 없다.** 핸들러가 5104로 보내도 받을 상대가 없으므로, **Vision에 MainComm 모듈 서버 신설**이 연결 구성의 최우선 과제다.
- 알람(`ARM`)이 현재 채널마다 제각각 수신된다. 전역 알람은 Main으로 모으는 방향을 검토(2단계).

### 1.3 레시피 동기화 흐름 (목표)

```
[핸들러] 사용자가 레시피 변경 (Form1.LoadMachineRecipe)
   → 로컬 레시피 로드/적용
   → VisionHub.BroadcastRecipeAsync(번호, 명칭)
       → Main 채널 송신:  "MainComm|RECIPE|<번호>|<명칭>"
[Vision]  MainComm 서버 수신
   → 전 모듈에 LoadRecipe(<명칭>) cascade  (Recipes/<명칭>/ 로드)
   → "ACK|MainComm|RECIPE|OK" 응답
[핸들러] ACK 확인 + 로그(VISION-RECIPE)
```

- **번호**: 현재 `RecipeProject`에 전용 번호 필드가 없어, 우선 `RecipeStore.List()`의 1-based 인덱스를 번호로 사용한다. (추후 전용 `RecipeNo` 필드 도입 여지)
- **명칭**: `RecipeProject.FileName` (= 활성 레시피 이름).
- **전제조건(provisioning)**: 핸들러와 Vision 양쪽에 **동일 이름의 레시피 폴더**가 존재해야 한다.

### 1.4 이미 반영된 부분 (핸들러 측 1차 구현)

- `VisionTcpClient.SendRecipeAsync(int recipeNo, string recipeName)` — `MODULE|RECIPE|번호|명칭` 송신 헬퍼.
- `VisionHub.BroadcastRecipeAsync(int recipeNo, string recipeName)` — Main 채널로 송신, 미연결/실패 안전 처리 + `VISION-RECIPE` 로그.
- (보류) `Form1.LoadMachineRecipe` 훅 연결 — 전체 구조 합의 후 진행.

### 1.5 연결 구성 작업 항목

1. ✅ **[Vision] MainComm 서버 신설** — `Comm/MainCommServer.cs`. 5104 listen, `MainComm|RECIPE|번호|명칭` 수신 → `VisionMachine.LoadRecipe(명칭)` cascade(Machine→Unit→Component→Algorithm) + ACK. 포트는 `VisionSettings.MainCommPort`(5104). `Form1` 에서 생성·Start·Dispose.
2. ✅ **[Vision] 레시피 라우팅** — `VisionMachine`(Machine 루트)의 `LoadRecipe` 가 전 모듈에 Composite cascade.
3. ✅ **[핸들러] `Form1.LoadMachineRecipe` 훅** — 로드 성공 후 `VisionHub.BroadcastRecipeAsync(번호, 명칭)` 호출(비차단). 번호는 `ResolveVisionRecipeNo`(RecipeStore 목록 1-based).
4. ⏳ **[양쪽] 레시피 폴더 Provisioning 정책** — 현재 미존재 시 기본값 로드(리셋 허용). 이름 체계 통일 필요.
5. ⏳ **[2단계] 알람 집중화** — `ARM` 전역 알람을 Main으로 통합할지 결정.

> 레시피 동기화 end-to-end 구현 완료(개인PC+장비PC 양쪽). 흐름: 핸들러 레시피 변경 → `BroadcastRecipeAsync` → `MainComm\|RECIPE\|번호\|명칭`(5104) → Vision `MainCommServer` → `VisionMachine.LoadRecipe`.

---

## 2. UI 구성 및 배치

### 2.1 핸들러 UI 프레임 (정렬 목표 패턴)

핸들러는 다음 공통 골격을 쓴다. Vision도 **동일 골격**으로 맞춘다.

- **탭 프레임**: `Ui/Tabs/TabBase` 기반. 하단/측면 메뉴 버튼으로 탭 전환.
- **사이드바 네비게이션**: 탭 안에서 `RegisterSidebarButton(...)`으로 서브시스템(페이지) 전환.
- **페이지-per-서브시스템**: 서브시스템마다 전용 페이지(`*RecipePage`, `*SetupPage` 등).
- **데이터 바인딩 컨트롤**: `UnitConfigGrid` / `ParameterGridControl`로 Setup·Config·Recipe 객체를 그리드 바인딩.
- **데이터 3계층**: 모든 단위가 `BaseUnit<TSetup,TConfig,TRecipe>` → `EquipmentData/{Setup,Config}/`, `Recipes/<명칭>/`.

> Vision은 **데이터 3계층은 이미 동일**(모듈·알고리즘이 `BaseUnit` 상속). 정렬 작업의 본체는 **UI 골격**이다.

### 2.2 탭 매핑 (확정)

핸들러 하단 메뉴: **작업 · 작업정보 · 이력 · 레시피 · 설정 · 사용자 · 종료**
Vision 목표 하단 메뉴: **작업 · 레시피 · 이력 · 설정 · 사용자 · 종료**

| 현재 Vision 탭 | → 목표 | 처리 | 비고 |
|---|---|---|---|
| 운영 (Operation) | **작업 (Work)** | 이름 변경 | 실시간 노광/검사 모니터 |
| 환경설정 (Configuration) | **(삭제 → 설정에 흡수)** | 탭 제거 | 설정 탭의 GENERAL 페이지로 이동 |
| 레시피 (Recipe) | **레시피 (Recipe)** | 동일 유지 | 추후 핸들러서 수신, 일단 동일 구성 |
| 데이터로그 (DataLog) | **이력 (History)** | 이름 변경 | Material 추적/검사결과/이벤트 로그 |
| 설정 (Settings) | **설정 (Settings)** | 환경설정 흡수 | 사이드바: GENERAL / 카메라 셋업 / 조명 셋업 |
| (없음) | **사용자 (User)** | 신규 | 핸들러와 동일 구성 |
| 종료 (Exit) | **종료 (Exit)** | 동일 | |

> Vision은 핸들러의 **작업정보(WorkInfo)** 탭을 별도로 두지 않는다(목표 메뉴에서 제외). 핸들러 전용 운전 정보 성격이므로 Vision은 작업/이력으로 충분.

### 2.2.1 설정 탭 사이드바 (핸들러 정렬)

병합된 **설정** 탭은 핸들러 설정 탭과 동일하게 사이드바로 페이지를 전환한다.

| 사이드바 항목 | 내용 | 현재 출처 |
|---|---|---|
| **GENERAL** | Backend/Provider, 포트, 이미지·데이터 로그 경로 (앱 전역 환경설정) | 기존 `ConfigurationPage` |
| **카메라 셋업** | 모듈별 카메라 매핑/파라미터 (Setup·Config) | 기존 `SettingsPage` 의 `CameraMappingPanel` |
| **조명 셋업** | 조명 시스템 정의(컨트롤러/포트/채널) | 기존 `LightSystemSetupPage` |

→ 즉 **환경설정 탭은 없어지고**, 그 내용은 설정 탭의 **GENERAL** 페이지가 된다.

### 2.3 Recipe 탭 배치 (핸들러 정렬)

- **좌측 사이드바**: 모듈(WaferVision / BinVision / BottomInspection / TopSideVision / BottomSideVision).
- **모듈 선택 시**: 하위 알고리즘(Finder/Inspector) 트리 노출.
- **알고리즘 선택 시**: 우측에 Target 페이지(카메라 라이브, ROI, Match/Inspect, Light, 파라미터 그리드).
- **상단**: **레시피 선택기**(load/save/delete) — `"default"` 하드코딩 제거, `Recipes/<명칭>/` 활성화. (핸들러 `ProjectPage` 대응)
- 데이터: 알고리즘 **Recipe** 계층(ROI·임계값·LightSettings).

### 2.4 Settings 탭 배치 (핸들러 정렬)

- **좌측 트리/사이드바**: 모듈별 + System 노드.
- **모듈 선택 시**: 카메라/광학 **Setup·Config** 그리드(`UnitConfigGrid` 한 그리드, 핸들러와 동일하게 Setup+Config 혼합 표시).
- **System 노드**: 앱 전역 설정(Provider, 포트, 로그 경로) — 기존 `ConfigurationPage`를 Settings 하위로 흡수.
- 데이터: 모듈 **Setup/Config** 계층(CameraId·Gain·Exposure·ROI 등) + 알고리즘 Config(MaxInstances 등) 노출.

### 2.5 UI 작업 항목

1. **탭 프레임 정렬** — Vision Form1의 5탭을 핸들러 `TabBase`+사이드바 패턴으로 재구성.
2. **Recipe 탭** — 모듈→알고리즘 트리 + 레시피 선택기 추가.
3. **Settings 탭** — Setup/Config 그리드 통합 + ConfigurationPage를 System 노드로 흡수.
4. **공통 컨트롤 정렬** — `UnitConfigGrid`/`ParameterGridControl` 바인딩 패턴 일치.
5. (AGENTS.md 준수) Designer 인라인, prefix 명명, 한글 UTF-8.

---

## 2.8 구조 계층 검증 결과 (Machine → Unit → Component → Algorithm)

코드 확인 결과(증거 포함):

| 계층 | 핸들러 (CDT-320) | Vision 현재 | 일치 여부 |
|---|---|---|---|
| **Machine 루트** | `CDT320Machine : BaseUnit<...>` 가 전 유닛을 `Components`로 소유 | **없음** — `Form1`이 5개 모듈을 직접 생성·소유 | ❌ 불일치 |
| **Unit** | InputStage/Vision/Picker… (`BaseUnit<Setup,Config,Recipe>`) | 5개 모듈(`VisionModule<Setup,Config,Recipe> : BaseUnit`) | ✅ 동일 패턴 |
| **Component (Camera/Axis/Cylinder)** | Camera·Axis 등을 자식 `Component` 노드로 모델링(각자 Setup/Config/Recipe) | **카메라가 별도 노드가 아님** — 카메라 파라미터가 모듈 Config(`CameraId`,Gain…)에 포함(의도적 SSOT 설계, 코드 주석 "C1") | ⚠️ 의도적 분기 |
| **Algorithm (Finder/Inspector)** | (Vision 한정) 알고리즘 자식 노드 | `AlgorithmNode`(Finder/Inspector)가 모듈 `Components`로 등록, Save/Load cascade | ✅ 동일 |

### 2.8.1 핸들러 베이스 클래스 (정렬 기준)

핸들러 `QMC.Common` 의 3계층 베이스를 그대로 사용한다:

| 계층 | 베이스 | 자식 목록 | 데이터 스타일 |
|---|---|---|---|
| Machine(루트) | `Machine<TSetup,TConfig,TRecipe>` | `Units` | POCO (예: `CDT320MachineSetup`) |
| Unit(중간) | `BaseUnit<TSetup,TConfig,TRecipe>` | `Components` | `[DataContract]` |
| Component(Leaf) | `BaseComponent<TSetup,TConfig,TRecipe>` | (없음) | `[DataContract]` |

### 2.8.2 적용 완료 (핸들러 동일)

1. **VisionMachine** = `Machine<VisionMachineSetup, VisionMachineConfig, VisionMachineRecipe>` 신설 — 핸들러 `CDT320Machine` 과 동일하게 5개 모듈을 프로퍼티로 노출 + `Units.Add`. 머신 데이터는 핸들러처럼 POCO. (`Modules/VisionMachine.cs`, `Modules/VisionMachineData.cs`)
2. **VisionCamera** = `BaseComponent<CameraSetup, CameraConfig, CameraRecipe>` 신설 — 핸들러 `BaseComponent`(Axis/Cylinder) 계층 정렬. 모듈 `Components` 에 등록, StorageKey=`<모듈키>.Camera`. (`Modules/VisionCamera.cs`, `Modules/CameraData.cs`)
3. **카메라 SSOT 이전** — 카메라 Config(CameraId/Gain/FrameRate/Trigger/PixelFormat/ROI/Delay) + Recipe(Exposure)를 `VisionModuleConfigBase/RecipeBase` 에서 `CameraConfig/CameraRecipe` 로 이전. 모듈 카메라 API(`CameraId/ExportCameraMapping/ApplyCameraSettings/CollectCameraSettings/ImportCameraMapping`)는 `CameraNode` 로 위임. (`Modules/VisionModule.cs`, `Modules/VisionModuleData.cs`)

→ 최종 계층: **Machine(VisionMachine) → Unit(5 Module) → Component(VisionCamera + Algorithm 노드)**. 핸들러와 동일.

> 기존 저장 데이터: 카메라 설정 키가 `<모듈>.json` → `<모듈>.Camera.json` 으로 이동(리셋 허용 — 사용자 합의). VS 빌드 후 재설정.

## 2.9 장비(알람) 코드 정렬

핸들러는 모든 장비 알람 코드를 공용 `QMC.Common/Alarms/AlarmMaster.cs` 의 `CreateDefaults()` 에 `AlarmDefinition`(Code/Category/DefaultSeverity/Title·Cause·Action ko·en)으로 등록한다. Vision 이 raise 하던 코드는 이 카탈로그에 **미등록**이었다 → 핸들러와 동일 포맷으로 등록 완료:

| 코드 | Category | Severity |
|---|---|---|
| VISION-CAMOPEN | Vision | Error |
| VISION-PARAMFAIL | Vision | Warning |
| VISION-RECIPE | Communication | Warning |
| LIGHT-OPEN-FAIL | Communication | Error |
| LIGHT-TIMEOUT / TX-FAIL / NAK / INVALID-RESP | Communication | Warning |
| LIGHT-MAP-INVALID / PAGE-MISS / PWR-RANGE | IO | Warning |

> 카탈로그는 공용(`QMC.Common`)이라 핸들러·Vision 양쪽 `Config/alarm_master.json` 이 동일 코드 체계를 공유한다. (기존 alarm_master.json 존재 시 신규 코드 반영은 파일 재생성 필요)

## 3. 권장 진행 순서 (핸들러 우선)

1. **연결 구성 확정** — Main(5104) 채널 규약(RECIPE/ALARM) 문서 fix → Vision MainComm 서버 신설 → 핸들러 `Form1` 훅.
2. **레시피 provisioning 정책** 확정.
3. **UI 프레임 정렬** — 탭/사이드바 골격 → Recipe 탭(트리+선택기) → Settings 탭(Setup/Config 통합).
4. 각 단계마다 VS 빌드 + perl 검증(`verify_all.pl`, `verify_vision_features.pl`).

> 빌드/실행 검증은 Windows(VS 2022 + .NET Framework 4.7.2)에서 수행. 본 작업 환경(리눅스)에서는 정적 리뷰까지만 가능.

---

## 3.1 폴더 구조 정렬 (추가 목표 — 후속 단계)

핸들러 솔루션 폴더: **Equipment / Sequencing / Ui** (+ Properties, App.config, Form1, Program).
Vision 현재 폴더: Backends / Cameras / Comm / Config / Core / Modules / Optics / Tools / Ui.

핸들러 동일 구성을 위한 제안 매핑(검토용):

| 핸들러 폴더 | Vision 통합 대상 |
|---|---|
| **Equipment/** | Modules, Cameras, Optics, Core, Backends, Config, Comm, Tools (장비/하드웨어/데이터/통신 계층) |
| **Sequencing/** | (Vision은 핸들러 명령에 반응 — 시퀀스 최소. 필요 시 신설) |
| **Ui/** | Ui (이미 동일) |

**판단(2026-06-17): 자동 일괄 이동 보류.** 사유:
- Vision 비-UI 코드가 ~100개 파일 규모 → 전부 `Equipment/` 하위로 이동 시 csproj `<Compile Include>` 경로 100여 곳 재작성 + 물리 이동. (네임스페이스는 폴더 비종속이라 그대로 둘 수 있어 코드 변경은 없음.)
- 이 환경(리눅스)에서는 **빌드 검증 불가** → 대량 이동 후 컴파일 깨짐을 잡을 수 없음.
- **장비PC(V:)를 다른 AI가 동시 수정 중** → 대량 파일 이동은 충돌·유실 위험 큼.
- 기능적 가치가 없는 순수 구조 정렬이라, 위 리스크를 감수할 단계가 아님.

→ **빌드 가능한 Windows 환경에서, 다른 AI 작업과 조율한 뒤, 단독 세션으로 진행** 권장. 매핑안(Equipment 통합)은 위 표대로 확정.

**실행 도구: `tools/reorg_vision_folders.ps1`** (작성 완료)
- 네임스페이스 불변(핸들러처럼 폴더≠네임스페이스) → `.cs` 변경 0, csproj 경로만 갱신.
- `git mv`(이력 보존) + csproj `Include` 경로 일괄 치환(UTF-8 BOM 보존), `-DryRun` 지원, `git checkout`/`git clean` 으로 되돌리기 가능.
- 대상 8폴더(Backends/Cameras/Comm/Config/Core/Modules/Optics/Tools) → `Equipment/` 하위. Ui 는 유지.
- 검증: csproj Include 8폴더 항목이 디스크와 일치, Ui(58) 불변, DependentUpon(파일명) 무영향 확인.
- 사용: `pwsh .\tools\reorg_vision_folders.ps1 -DryRun` 미리보기 → 실행 → VS 빌드 검증. 양쪽 PC 각각 실행.

## 4. 미해결 결정 사항 (합의 필요)

1. **레시피 번호 체계** — 리스트 인덱스로 갈지, `RecipeNo` 전용 필드를 도입할지.
2. **Provisioning** — Vision에 해당 레시피 폴더가 없을 때: 송신 거부 / 빈 폴더 생성 / 핸들러가 데이터 푸시(B-2 폴백).
3. **알람 집중화** — 전역 `ARM`을 Main으로 통합할지, 채널별 유지할지.
4. **History/User 탭** — Vision에도 둘지(핸들러 완전 동일) 여부.
