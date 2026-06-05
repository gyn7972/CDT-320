# STAGE 62 — SPEC: Vision 알고리즘별 카메라 파라미터 설정 (Recipe Layer)

- **작성일**: 2026-05-27
- **단계**: SPEC (구현 전, 사용자 컨펌 대기)
- **Stage 번호 근거**:
  - `D:\Work\CDT-320\docs\` 최신 산출물 = `STAGE60_SPEC_AlarmMasterManualCompat.md` / `STAGE60_RESULT_*`
  - 코드 내 최신 Stage 마커 = **Stage 61** (`QMC.CDT-320/QMC.CDT-320/Equipment/MachineController.cs:88, 97, 157, 732, 1114, 1334, 1470, 1649`; `RecipeStore.cs:252, 256`; `RecipeTab.cs:49`; `SimulatorBridge.cs:107, 139`)
  - 따라서 **NN = 62** 채택
- **git status**: ⚠ `D:\Work\CDT-320` / `D:\Work\CDT-320\QMC.CDT-320` 둘 다 `.git` 부재 — 본 작업은 git 없이 진행됨 (정지 조건의 "clean이 아닌 상태"는 git 부재와 동치로 보지 않음). 후속 단계 진입 전 git 초기화 또는 사용자 컨펌 필요.

---

## 1. 목표 & 범위

### 목표
QMC.Vision 의 **비전 알고리즘 5종 ↔ 카메라 + 카메라 파라미터** 매핑을 Recipe 단위로 편집/저장/적용할 수 있도록 한다.

### 범위 (In Scope)
- 5 알고리즘 (`Wafer / Bin / BottomInspection / TopSide / BottomSide`) 각각의:
  - 카메라 ID (1:1 매핑)
  - 카메라 파라미터 (Exposure / Gain / FPS / Trigger / Pixel / Delay [+ ROI — 추가 검토])
- Recipe 영속화 (JSON, `Config\algorithm_camera.json`) + 구버전 호환 (기본값 보충)
- 편집 UI (`SettingsPage` 안 TreeView 좌측 사이드바 + 우측 디테일)
- 런타임 적용 — Form1 기동 시 매핑 → 카메라 생성 + 파라미터 주입, 운영 중 hot-rebind
- 알람: 매핑 누락 / 카메라 파라미터 적용 실패 시 `AlarmManager.Raise` 호출

### 범위 외 (Out of Scope)
- 카메라 자동 캘리브레이션 (mm/pixel scale 자동 산출 — VisionScale 도구 기존 존재)
- 다중 카메라 동기 트리거 (다음 Stage 후보)
- Recipe Layer **Handler 측 RecipeProject 와의 통합** — 본 Stage 는 Vision 측 전역 설정으로 유지하고, Handler ↔ Vision 간 Project 동기는 별도 Stage 로 분리
- Cognex/OpenCv 백엔드 전환 시 ROI/Trigger 의미 차이 — 백엔드 무관 통일 인터페이스만 사용

---

## 2. 현황 분석 (Read-only 조사 결과)

### 2.1 비전 알고리즘 5종

`QMC.Vision/Modules/` 하위 — 모두 `VisionModule` 추상 클래스 상속.

| 클래스 | 파일:줄 | base 이름 | 등록 finder/inspector |
|---|---|---|---|
| `WaferVisionModule` | `QMC.Vision\Modules\WaferVisionModule.cs:9` | `"WaferVision"` | EjectPin / Reticle / AlignDie / FirstRef / SecondRef / Die / Scale (7) |
| `BinVisionModule` | `QMC.Vision\Modules\BinVisionModule.cs:8` | `"BinVision"` | Reticle / Die / PlacementInspector / Scale (4) |
| `BottomInspectionModule` | `QMC.Vision\Modules\BottomInspectionModule.cs:8` | `"BottomInspection"` | Reticle / Collet / Die / Surface / Focus / Scale / DistortionComp (7) |
| `TopSideInspectionModule` | `QMC.Vision\Modules\TopSideInspectionModule.cs:9` | `"TopSideInspection"` | DieEdge / TopSurface / TopChipping / Focus (4) |
| `BottomSideInspectionModule` | `QMC.Vision\Modules\BottomSideInspectionModule.cs:9` | `"BottomSideInspection"` | DieEdge / BottomSurface / BottomChipping / Focus (4) |

**등록 방식**: Enum/Factory 가 아니라 `Form1.Form1_Load` (`QMC.Vision\Form1.cs:54-58`) 에서 직접 `new`. 키 문자열은 `Config/AlgorithmCameraMap.cs:14-23` 의 `VisionAlgorithm` 상수 클래스(`Wafer/Bin/BottomInspection/TopSide/BottomSide`) — base 이름과 1:1 대응되지 않음에 유의 (예: `Wafer` ↔ `WaferVision`).

### 2.2 카메라 추상화

- 인터페이스: `QMC.Vision\Core\ICamera.cs:10`
  - 식별: `Info.Id` (string), `Info.IpAddress`, `Info.Transport` (`CameraTransport.Sim|GigE|Usb3|...`) — `QMC.Vision\Core\CameraInfo.cs:6, 24`
  - 파라미터 (set 가능):
    - `ExposureUs : double` (`ICamera.cs:43`)
    - `Gain : double` (`:44`)
    - `AcquisitionFrameRate : double` (`:45`)
    - `TriggerMode : CameraTriggerMode` (`:46`) — enum `Continuous|Software|Line0|Line1|Line2`
    - `PixelFormat : CameraPixelFormat` (`:47`) — enum `Mono8|Mono10|Mono12|BayerRG8|BayerGB8|BGR8|RGB8`
    - `Roi : Rectangle` (`:50`) — Width/Height/OffsetX/OffsetY
  - 읽기 전용: `Resolution : Size` (`:53`)
  - 디버그: `GetRawParameter(key) / SetRawParameter(key, value)` (`:57-58`)
- 베이스: `QMC.Vision\Core\CameraBase.cs:11` — 파라미터 캐시 + Hook 메서드 (`OnExposureChanged` 등)
- 팩토리: `QMC.Vision\Core\CameraFactory.cs:11`
  - `EnumerateAll()` — Hik GigE + Sim 탐색
  - `CreateById(string id)` — `"Sim/X"` 또는 IP. SDK 미설치 / 미발견 시 Sim fallback (`:35-51`)

### 2.3 Vision Recipe / Config 구조

#### 2.3.1 기존 전역 설정 (`VisionSettings`)
- 파일: `QMC.Vision\Config\VisionConfig.cs:12-57`
- 직렬화: `DataContractJsonSerializer` → `Config\vision.json`
- 카메라 ID 필드: **3개만** (`:30-32`)
  - `WaferCameraId`, `BinCameraId`, `BottomInspectionCameraId`
  - **TopSide / BottomSide 용 카메라 ID 필드 없음** ← 연결 부재 증거 #1
- 파라미터 필드: **알고리즘 무관 전역**만 존재 — `ScaleX / ScaleY / IsRotated / InvertedX / InvertedY / DelayBeforeGrabMs / SideLocation` (`:36-52`)
  - **알고리즘별 Exposure/Gain/Trigger 저장 위치 없음** ← 연결 부재 증거 #2

#### 2.3.2 신규 매핑 (`AlgorithmCameraMap` — 본 Stage 도입 부분, 이미 코드에 존재)
- 파일: `QMC.Vision\Config\AlgorithmCameraMap.cs`
- 직렬화: `DataContractJsonSerializer` → `Config\algorithm_camera.json` (`:78-81`)
- 모델:
  - `AlgorithmCameraMapping` (`:42-66`) — `Algorithm / CameraId / ExposureUs / Gain / FrameRate / TriggerMode / PixelFormat / DelayBeforeGrabMs`
  - `AlgorithmCameraMap.Items : List<...>` (`:71`)
  - `VisionAlgorithm` 상수 (`:14-38`) — 5 알고리즘 + 한글 라벨
- `EnsureDefaults` (`:125-152`) — 누락된 알고리즘 항목을 `VisionSettings` 의 기존 ID 또는 Sim 으로 채움 (구버전 호환)
- **ROI 미포함** ← 본 Stage 의 결정 항목 (확인 필요 #1)

#### 2.3.3 Binder
- 파일: `QMC.Vision\Config\AlgorithmCameraBinder.cs`
- `CreateAndApply(mapping)` — `CameraFactory.CreateById` → `Open` → 파라미터 적용
- `ApplyParameters(cam, mapping)` — Exposure/Gain/FrameRate/TriggerMode/PixelFormat 만 적용 (**ROI 미적용**)

### 2.4 알고리즘 ↔ 카메라 연결 (현 상태)

```
QMC.Vision\Form1.cs:48-58
    var camWafer      = CreateCameraForAlgorithm(map, VisionAlgorithm.Wafer,            "Sim/Wafer");
    var camBin        = CreateCameraForAlgorithm(map, VisionAlgorithm.Bin,              "Sim/Bin");
    var camBottom     = CreateCameraForAlgorithm(map, VisionAlgorithm.BottomInspection, "Sim/BottomInsp");
    var camTopSide    = CreateCameraForAlgorithm(map, VisionAlgorithm.TopSide,          "Sim/TopSide");
    var camBottomSide = CreateCameraForAlgorithm(map, VisionAlgorithm.BottomSide,       "Sim/BottomSide");

    WaferMod      = new WaferVisionModule       (camWafer,      Backend);
    ...
```

- 매핑 기반 카메라 생성은 **이미 구현됨** (이전 사용자 요청들의 결과). 본 Stage 는 이를 정식화 + 알람 wiring + 검증.
- `Form1.RebindAlgorithmCamera` (`:116-136`) — 운영 중 hot-rebind 지원 (camera ID 같으면 파라미터만 갱신, 다르면 카메라 교체).

### 2.5 참고: Handler 측 Recipe 3-Layer

- `QMC.CDT-320\QMC.CDT-320\Equipment\Recipes\RecipeStore.cs:14` — `RecipeStore` static class
- 직렬화: `DataContractJsonSerializer` → `Recipes\<FileName>.Project`
- `IRecipeData` 인터페이스 (Equipment 측 units 가 구현) — `InputLoaderRecipe / InputStageRecipe / OutputStageRecipe / PickerRecipe / TpuArmRecipe / TpuRecipe / OutputLoaderRecipe / StageModuleRecipe / UnitRecipe / CDT320MachineRecipe`
- `RecipeProject` (`:210-253`) — Project 1개 = `FileName / MachineNumber / CassetteFlow / Die / Frame / LoadFrame / UnloadFrame / Module / BottomInsp / TopSideInsp / BottomSideInsp / Output / Pickup`
  - 이미 `InspectionSubset` (BottomInsp/TopSideInsp/BottomSideInsp) 가 Recipe Layer 에 있음 — 그러나 **Vision 측에서 이를 직접 읽지 않음** (TCP 명령으로 파라미터를 동적 수신할 가능성 — 별도 조사 필요)
- 컨벤션: `[DataContract] [DataMember]`, 정적 `Load / Save / List / Delete`, `LastProjectMarker` 파일 (`.last_project`)

### 2.6 기존 UI 패턴 (QMC.Vision)

- 메인 폼: `QMC.Vision\Form1.cs` + `Form1.Designer.cs`
- 6 탭: Operation / Configuration / Maintenance / Recipe / DataLog / **Settings** (`Tab` enum `Form1.cs:13`)
- 설정 페이지 진입점: 하단 메뉴 `btnSettings` (Handler 와 동일 스타일 `BottomMenuButton`)
- `SettingsPage` (`QMC.Vision\Ui\Pages\SettingsPage.cs:17`) — 좌측 TreeView 사이드바 (2 그룹: 카메라 매핑 / 검사 알고리즘) + 우측 디테일 (`CameraMappingPanel` 또는 `ParameterEditor` 5종)
- 카메라 매핑 패널: `QMC.Vision\Ui\Pages\CameraMappingPanel.cs` — CameraId 콤보 / NumericUpDown / Save/Apply/TestGrab
- 검사 파라미터 편집기: `QMC.Vision\Ui\Editors\` 의 5 종 (`BottomInspection / SideInspection / DieGapInspection / Distortion / VisionScale`) — `ParameterEditorBase` 상속, `Recipes\<tool>.json` 영속화

---

## 3. 기능 요구사항

| ID | 요구 |
|---|---|
| FR-1 | 5 알고리즘 각각에 카메라 1대 매핑 가능 (1:1) |
| FR-2 | 알고리즘별 카메라 파라미터 셋: Exposure / Gain / FrameRate / TriggerMode / PixelFormat / DelayBeforeGrabMs **+ ROI (선택 — 확인 필요 #1)** |
| FR-3 | 편집 UI: 좌측 알고리즘 리스트, 우측 카메라 콤보 + 파라미터 입력 (기존 `CameraMappingPanel` 활용) |
| FR-4 | 저장 (Save) — 즉시 JSON 파일 영속화 (`algorithm_camera.json`) |
| FR-5 | 로드 (Load) — Form1 기동 시 + Settings 페이지 진입 시 |
| FR-6 | 적용 (Apply to running) — 운영 중 카메라 hot-rebind (이미 구현, 검증만 필요) |
| FR-7 | 기본값 복원 (Reset) — 알고리즘 1개 또는 전체를 `EnsureDefaults` 값으로 되돌림 (현재 UI 버튼 없음 — 추가 필요) |
| FR-8 | 취소 (Cancel) — 미저장 변경 폐기 + 디스크 값으로 다시 바인딩 (현재 미구현 — 추가 필요) |
| FR-9 | Validation — 빈 CameraId 거부, 범위 밖 값 거부 (NumericUpDown 의 Min/Max 외 추가 검사 필요) |
| FR-10 | 매핑 누락 알람 — Form1.Load 시 알고리즘에 해당하는 mapping 이 없으면 `AlarmManager.Raise(VISION-MAPMISS, ...)` |
| FR-11 | 파라미터 적용 실패 알람 — `ApplyParameters` 또는 `Camera.Open` 실패 시 `AlarmManager.Raise(VISION-PARAMFAIL, ...)` |
| FR-12 | 구버전 호환 — `algorithm_camera.json` 누락 또는 일부 알고리즘 누락 시 `EnsureDefaults` 가 채움 (이미 구현 — 명시) |
| FR-13 | Sim 모드 — 카메라가 SimCamera 인 경우 파라미터 적용은 no-op (코드 검증 필요 — 확인 필요 #4) |

---

## 4. 데이터 모델 (현행 + 보강 제안)

### 4.1 현행 (이미 코드에 존재)

```csharp
// QMC.Vision\Config\AlgorithmCameraMap.cs
[DataContract]
public class AlgorithmCameraMapping
{
    [DataMember] public string Algorithm           { get; set; } = "";
    [DataMember] public string CameraId            { get; set; } = "";
    [DataMember] public double ExposureUs          { get; set; } = 5000;
    [DataMember] public double Gain                { get; set; } = 1.0;
    [DataMember] public double FrameRate           { get; set; } = 30;
    [DataMember] public string TriggerMode         { get; set; } = "Software";
    [DataMember] public string PixelFormat         { get; set; } = "Mono8";
    [DataMember] public int    DelayBeforeGrabMs   { get; set; } = 0;
}

[DataContract]
public class AlgorithmCameraMap
{
    [DataMember] public List<AlgorithmCameraMapping> Items { get; set; }
}
```

### 4.2 보강 제안 (본 Stage)

```csharp
// AlgorithmCameraMapping 에 추가
[DataMember] public int RoiOffsetX { get; set; } = 0;
[DataMember] public int RoiOffsetY { get; set; } = 0;
[DataMember] public int RoiWidth   { get; set; } = 0;   // 0 = full sensor
[DataMember] public int RoiHeight  { get; set; } = 0;
// 헬퍼 (직렬화 제외):
public bool      IsRoiFull => RoiWidth <= 0 || RoiHeight <= 0;
public Rectangle ToRectangle() => new Rectangle(RoiOffsetX, RoiOffsetY, RoiWidth, RoiHeight);
```

### 4.3 JSON 스키마 예시 (직렬화 결과)

```json
{
  "Items": [
    {
      "Algorithm": "Wafer",
      "CameraId": "192.168.1.10",
      "ExposureUs": 8000,
      "Gain": 2.5,
      "FrameRate": 30,
      "TriggerMode": "Software",
      "PixelFormat": "Mono8",
      "DelayBeforeGrabMs": 5,
      "RoiOffsetX": 0,
      "RoiOffsetY": 0,
      "RoiWidth": 0,
      "RoiHeight": 0
    },
    { "Algorithm": "Bin", ... },
    { "Algorithm": "BottomInspection", ... },
    { "Algorithm": "TopSide", ... },
    { "Algorithm": "BottomSide", ... }
  ]
}
```

---

## 5. UI 와이어프레임 (현행 + 보강)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 설정 — 비전 알고리즘별 카메라 + 검사 파라미터  (오렌지 헤더)                │
├──────────────────────┬──────────────────────────────────────────────────────┤
│ ■ 카메라 매핑        │ 카메라 매핑 — 웨이퍼 비전  (Wafer)                  │
│   ▸ 웨이퍼 비전 [*]  ├──────────────────────────────────────────────────────┤
│   ▸ 빈 비전          │ 카메라 ID    [ 192.168.1.10        ▼ ] [카메라 검색] │
│   ▸ 바텀 검사        │ Exposure(μs) [ 8000               ]                 │
│   ▸ 상면 검사        │ Gain (dB)    [ 2.5                ]                 │
│   ▸ 하면 검사        │ FrameRate    [ 30                 ]                 │
│ ■ 검사 알고리즘      │ Trigger      [ Software           ▼ ]               │
│   ▸ 바텀 검사 파라.. │ PixelFormat  [ Mono8              ▼ ]               │
│   ▸ 측면 검사 파라.. │ Delay (ms)   [ 5                  ]                 │
│   ▸ 다이 갭 검사     │ ── ROI (보강 시 표시) ──                            │
│   ▸ 왜곡 보정        │ OffsetX [0] OffsetY [0] W [0] H [0] (0=full sensor)  │
│   ▸ 비전 스케일      │                                                     │
│                      │ [저장] [기본값 복원] [취소] [실행 적용] [테스트 그랩]│
│                      │ Status: ...                                         │
│                      │ [Preview PictureBox]                                │
└──────────────────────┴──────────────────────────────────────────────────────┘
```

추가 버튼: **기본값 복원 (Reset)**, **취소 (Cancel)** — 현재 SettingsPage/CameraMappingPanel 에 없음.

---

## 6. 시퀀스 / 적용 흐름

### 6.1 Form1 기동 시
```
Form1_Load
  └─ VisionConfigStore.Load()                       (vision.json — 전역 cfg)
  └─ AlgorithmCameraMapStore.Load()                 (algorithm_camera.json — 알고리즘 매핑)
       └─ EnsureDefaults()  ← 누락된 알고리즘 채움
  └─ for each alg in {Wafer, Bin, Bottom, TopSide, BottomSide}:
       mapping = map.Get(alg)
       if (mapping == null || mapping.CameraId == "")  → ALARM(VISION-MAPMISS)
       cam = CameraFactory.CreateById(mapping.CameraId)
       cam.Open()       ← 실패 시 Sim fallback (CameraFactory 내부)
       ApplyParameters(cam, mapping)   ← 실패 시 ALARM(VISION-PARAMFAIL)
       module = new XxxVisionModule(cam, Backend)
       module.DelayBeforeGrabMs = mapping.DelayBeforeGrabMs
```

### 6.2 사용자 편집 → 저장 → 적용
```
사용자가 SettingsPage TreeView 에서 알고리즘 선택
  → CameraMappingPanel.SelectAlgorithm(alg) 가 매핑 객체 바인딩
  → 사용자가 필드 수정 (NumericUpDown / ComboBox / TextBox)
  → OnFieldChanged() 가 메모리상 매핑 객체에 즉시 반영 (디스크 미저장)
[저장] 클릭
  → AlgorithmCameraMapStore.Save()  (디스크에 algorithm_camera.json 갱신)
[실행 적용] 클릭
  → Form1.RebindAlgorithmCamera(alg, mapping)
       ├ CameraId 동일 → ApplyParameters 만
       └ CameraId 변경 → 신규 cam 생성 + Open + ApplyParameters + module.SetCamera + old cam Dispose
[취소] 클릭 (신규)
  → AlgorithmCameraMapStore.Load()  + BindFields() 재호출
[기본값 복원] 클릭 (신규)
  → 현재 알고리즘 항목을 EnsureDefaults 로직으로 reset
[테스트 그랩] 클릭
  → 임시 cam 생성 + Open + Grab → PictureBox 표시
```

### 6.3 비전 처리 시 (TCP 명령 수신)
```
VisionTcpServer.HandleRequest
  → module.Grab(timeoutMs)  ← module.Camera 사용. 카메라 파라미터는 이미 적용된 상태
```
- 본 Stage 에서는 TCP 명령 단위로 파라미터를 동적 변경하지 않음.
- 단, `SetCameraParam` 같은 명령이 매뉴얼에 존재할 가능성 → 별도 조사 필요 (확인 필요 #5).

---

## 7. 알람 / 에러 처리 정책

### 7.1 신규 알람 코드 제안

기존 prefix 컨벤션 (`AlarmMaster.cs:149-160`): `VISION-CONN`, `VisionMatchFail`, `EXPOSE-TIMEOUT`, `ALIGN-EX`, `IS-ALIGN`, `OS-BININSP`.
일관성을 위해 본 Stage 는 **`VISION-` prefix** 사용 (대시 형식).

| Code | Category | Severity | Title | Cause | Action |
|---|---|---|---|---|---|
| `VISION-MAPMISS` | Vision | Warning | 알고리즘 카메라 매핑 누락 | algorithm_camera.json 에 해당 알고리즘 항목 없음 | 설정 페이지에서 카메라 ID 지정 후 저장 |
| `VISION-PARAMFAIL` | Vision | Warning | 카메라 파라미터 적용 실패 | Exposure/Gain/Trigger 등 설정 중 예외 | EventLog 확인, 카메라 SDK/드라이버 점검 |
| `VISION-CAMOPEN` | Vision | Error | 카메라 Open 실패 | GigE 미연결 / SDK 미설치 | IP/케이블/SDK 확인, Sim fallback 동작 시 무시 가능 |

### 7.2 AlarmMaster.cs 등록 위치

`QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs` 의 `// ── Vision (비전) ──` 블록 (`:148~160`) 끝에 3 행 추가.

### 7.3 AlarmManager.Raise 호출 지점

⚠ **중요 — 아키텍처 결정 (확인 필요 #2)**:
- `AlarmManager` 클래스는 `QMC.CDT320` 네임스페이스 (Handler) 에 있음. `QMC.Vision` 프로젝트가 직접 참조하지 않음 → 알람을 어떻게 raise 할 것인가?
- 옵션 A: QMC.Vision 측에 별도 `VisionAlarmLogger` (파일 로그) 추가, Handler 가 TCP 로 수신
- 옵션 B: QMC.Common 또는 새 공용 어셈블리에 `IAlarmReporter` 인터페이스 추가, 양쪽 구현
- 옵션 C: Vision → Handler TCP 알람 메시지 (기존 `VISION-CONN` 처리 방식 차용 — Handler 의 `VisionHub.ConnectionChanged` 가 알람 발생)
- **추천**: 옵션 C (가장 적은 변경) — 호출 지점은 Handler 의 `OnVisionHubChanged` (`QMC.CDT-320\Form1.cs` 근처)
- 또는 **단순화**: Vision 측 알람은 SettingsPage 의 status label / EventLog 로만 남기고, AlarmMaster 등록은 옵션 (Stage 63 으로 분리)

---

## 8. 영향 범위

### 8.1 신규 파일
(없음 — 기존 파일 보강)

### 8.2 수정 파일

| 파일 | 변경 내용 |
|---|---|
| `QMC.Vision\Config\AlgorithmCameraMap.cs:42-66` | `AlgorithmCameraMapping` 에 ROI 4 필드 추가 (FR-2) |
| `QMC.Vision\Config\AlgorithmCameraBinder.cs:18-27` | `ApplyParameters` 에 ROI 반영 (`cam.Roi = mapping.ToRectangle()`) |
| `QMC.Vision\Ui\Pages\CameraMappingPanel.cs` | ROI 4 NumericUpDown + Reset/Cancel 버튼 + Validation |
| `QMC.Vision\Form1.cs:48-58` | 매핑 누락 시 EventLog (옵션 C 채택 시) 또는 알람 raise |
| `QMC.Vision\Form1.cs:97-107` | `CreateCameraForAlgorithm` 의 실패 분기에 로그/알람 추가 |
| `QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs:160` | 3 신규 알람 정의 추가 (옵션 C/B 채택 시) |

### 8.3 시퀀스 문서 04~08 영향
- 04_Feeder / 05_InputStage / 06_TransferPicker / 07_OutputStage / 08_OutputUnloader: **직접 영향 없음** — 본 Stage 는 Vision 카메라 파라미터의 사용자 설정 경로 정식화. 비전 호출 자체의 시퀀스는 불변.
- 단, `MachineController.cs:1114, 1334` 의 파이프라인 wafer 비전 헬퍼 호출은 이미 매핑 기반 파라미터 적용 후 동작 → 별도 갱신 불필요.

---

## 9. Mismatch 발견 항목

| # | 문서 위치 | 코드 위치 | 차이 |
|---|---|---|---|
| M-62-1 | 작업 지시 문서 (`MISMATCH_LOG.md` 언급) | 실제 파일 `D:\Work\CDT-320\docs\MISMATCH_RESOLUTION_LOG.md` | 파일명 불일치 — 추가 항목은 `MISMATCH_RESOLUTION_LOG.md` 에 기록 권장 |
| M-62-2 | `VisionConfig.cs:30-32` 의 3 카메라 ID 필드 | 알고리즘 5 개 (`VisionAlgorithm.All`) | TopSide / BottomSide 카메라 ID 가 `VisionSettings` 에 없음 (신규 `AlgorithmCameraMap` 이 이를 보완) |
| M-62-3 | `AlgorithmCameraMapping` (`:42-66`) | `ICamera.Roi` (`ICamera.cs:50`) | ROI 인터페이스는 있으나 매핑에 미포함 — 본 Stage 보강 항목 |
| M-62-4 | `RecipeProject` 의 `InspectionSubset` 3 개 (`RecipeStore.cs:246-248`) | Vision 측 사용 흔적 | Handler Recipe 의 BottomInsp/TopSideInsp/BottomSideInsp 가 Vision 측에서 직접 사용되는지 미확인 — TCP 명령으로 수신할 가능성 |
| M-62-5 | `AlarmMaster.cs` Vision prefix | 코드 안에 혼재 (`VISION-CONN`, `VisionMatchFail`, `EXPOSE-TIMEOUT`, `ALIGN-EX`) | 일관성 없음 — 본 Stage 는 `VISION-` 채택 |

### `MISMATCH_RESOLUTION_LOG.md` 추가 안

```markdown
## STAGE 62 — Vision 알고리즘별 카메라 파라미터 (2026-05-27)

| ID | 위치 | 내용 | 처리 |
|---|---|---|---|
| M-62-2 | VisionConfig.cs:30-32 | TopSide/BottomSide 카메라 ID 필드 없음 | AlgorithmCameraMap 신설로 보완. 구버전 VisionSettings 의 3 필드는 EnsureDefaults fallback 으로만 사용 |
| M-62-3 | AlgorithmCameraMap.cs:42-66 | ROI 필드 누락 | Stage 62 에서 RoiOffsetX/Y/Width/Height 4 필드 추가 |
| M-62-5 | AlarmMaster.cs Vision 카테고리 | prefix 혼재 (VISION-/Vision/EXPOSE-/ALIGN-) | 신규 코드는 VISION-* 로 통일 (기존은 손대지 않음) |
```

---

## 10. 비범위 (Out of Scope)

- 카메라 자동 캘리브레이션 (mm/pixel) — 별도 `VisionScale` 도구 존재
- 다중 카메라 동기 트리거 (HW trigger sync)
- 백엔드 (Sim/OpenCv/Cognex) 전환 시 파라미터 의미 차이 처리
- Handler `RecipeProject` ↔ Vision `AlgorithmCameraMap` 동기 (별도 Stage 후보)
- TCP 명령 `SetCameraParam` 등 동적 파라미터 변경 (실시간 명령 — 미존재 시 추가 검토)

---

## 11. 확인 필요 항목 (사용자 컨펌 요청)

1. **ROI 포함 여부** — `AlgorithmCameraMapping` 에 RoiOffsetX/Y/Width/Height 4 필드를 본 Stage 에서 추가할지? (UI 도 그에 맞춰 확장)
2. **알람 발생 경로** — Vision 측에서 `AlarmManager.Raise` 를 직접 호출할 수 없음 (참조 없음). 옵션 A/B/C 중 어느 것?
   - 옵션 A: Vision 자체 file logger only (AlarmMaster 등록 안 함)
   - 옵션 B: QMC.Common 에 `IAlarmReporter` 추상화 신설
   - 옵션 C: Vision → Handler TCP 알람 channel 추가 (`VisionHub` 흐름 차용)
3. **Recipe Layer 통합 범위** — `algorithm_camera.json` 을 Project-independent 전역으로 유지 vs Project (RecipeProject) 별로 분리? 후자라면 `RecipeProject` 에 `AlgorithmCameraSubset` 추가 필요.
4. **Sim 모드 no-op 검증 방법** — SimCamera 의 `OnExposureChanged` 등이 진짜 no-op 인지 코드 확인이 추가 필요한가? (`QMC.Vision\Cameras\Sim\SimCamera.cs` 추가 조사 필요)
5. **TCP `SetCameraParam` 동적 명령 존재 여부** — CDT-310 매뉴얼 (`D:\Work\CDT-320\2025.08.19_CDT310 Vision.docx`) 에 동적 카메라 파라미터 변경 명령이 정의돼 있는가? 있다면 본 Stage 범위에 포함할지.
6. **`MISMATCH_LOG.md` vs `MISMATCH_RESOLUTION_LOG.md`** — 작업 지시문의 파일명과 실제 파일명 불일치. 어느 쪽에 추가?
7. **git 초기화** — 본 프로젝트는 git 저장소가 아닌 상태. Stage 62 구현 진입 전 git init + 첫 커밋이 필요한가?

---

## 12. 다음 단계

본 SPEC + CHECKLIST 사용자 컨펌 후:
1. 확인 필요 항목 1~7 해소
2. (옵션) git init + 본 SPEC/CHECKLIST 첫 커밋
3. (옵션) 브랜치 `stage-62-vision-algo-camera-config` 생성
4. CHECKLIST 의 각 항목 순차 구현 → 빌드 → verify_all
5. `STAGE62_RESULT_VisionAlgorithmCameraConfig.md` 산출
