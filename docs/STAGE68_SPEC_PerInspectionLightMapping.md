# STAGE 68 — SPEC: 검사별 조명 매핑 (Controller·Page·Channel·Level)

- **작성일**: 2026-05-29
- **단계**: SPEC (구현 전, 사용자 컨펌 대기)
- **Stage 번호 근거**: docs/코드 최신 = Stage 67 (LFine 드라이버) → **NN = 68**
- **브랜치**: `stage-68-per-insp-light-mapping-spec` (base: master)
- **의존**: Stage 66/67(LFine 드라이버 `ILightController`), Stage 64(검사 트리 3-레벨 + `AlgorithmCameraMapping`/`InspectionCameraOverride`)

---

## 1. 목표 & 범위

### 목표
검사(Inspection) 단위로 **(Controller, Page, Channel, Level)** 4축 + (On, StrobeTimeUs) 매핑을 저장·편집·적용. 한 검사가 풀 내 **여러 (Channel)** 을 동시에 사용 가능. LFine 페이지 개념 활용(검사별 사전 적재 페이지 1회 전환).

### 범위 (In Scope) — SPEC + CHECKLIST 까지만
- 데이터 모델: Setup 레이어(컨트롤러 인벤토리 + 알고리즘 결선) + Recipe 레이어(검사별 Level/On/Strobe)
- UI 설계: `LightSystemSetupPage`(Setup) + `InspectionLightPanel`(Recipe)
- 마이그레이션 설계: io_set.lightSource.json → light_system.json
- 알람 설계 (신규 3 + LFine 6 재사용)

### 범위 외 (Out of Scope)
- **코드 구현** — 본 Stage 는 문서만 (구현은 Stage 69)
- LFine 드라이버 자체 (Stage 66/67 완료)
- 검사 트리 3-레벨 (Stage 64 완료)
- **런타임 자동 적용** (Grab 직전 조명 적용) — 다음 Stage (§6)

### 1.1 컨트롤러 구성 — 본 장비 = **2 컨트롤러** (Stage 66 #5 동기 확정)
- 본 장비에 LFine 컨트롤러 2대 동시 연결 (예: COM1, COM2). 처음부터 다중 지원.
- **`LightHub` 신설** — VisionHub 패턴(`Dictionary<PortName, ILightController>`). `Form1.Load` 가 Setup.Controllers 순회하여 포트별 `LightControllerFactory.Create` → Hub 등록.
- 라우팅: 호출자가 알고리즘 결선의 `ControllerPort` 로 `LightHub.Get(port)`.
- **결선 룰**: 1 검사 = 1 컨트롤러 (알고리즘 단위 결선이라 자동 보장).
- 채널 풀 (사용자 예시): BottomInspection → COM1 [3,4,5,6], FrontSide → COM1 [1], RearSide → COM1 [2]. Wafer/Bin/COM2 배정은 확인 필요 #1.

---

## 2. 현황 분석 (코드 직접 확인, 0% 구현)

### 2.1 UI 더미
- `IlluminatorPanel.cs:7` (`: Panel`) — 4 채널 TrackBar(0~255). `tb.ValueChanged += (s,e) => val.Text = tb.Value.ToString();` (`:29`) — **하드웨어 호출 0, 검사 컨텍스트 미수신** (M-68-1)
- `FinderPage.cs:55` / `InspectorPage.cs:53` — `new IlluminatorPanel { ... }` 만, 모듈/검사 미전달 (M-68-2)

### 2.2 검사별 조명 영속화 — **부재**
- grep `Brightness|LightLevel|InspectionLight|조명값` → **0 매치**. 매뉴얼은 검사별 "현재/저장 밝기 / Set / Save / On / Off" 명시하나 코드에 모델 없음.

### 2.3 Handler LightControllerPage — 부분 시도
- `LightControllerPage.cs:180-198` — SerialPort 9600 Open + `port.Write(cmd)` 1회. 검사별 매핑·응답 0, Vision 결선 0 (M-68-3)

### 2.4 LFine Page 개념 — 코드 확인
- `LFinePSDigitalIlluminator.cs:14-19` — `LastPowerInfo { int Page; int[] OnTimes = new int[16]; }` (페이지당 16채널)
- `:127` `SetStrobeOnTime(int page, int[] times)` — **페이지 단위 일괄 적용** → Page 축 근거.
- ⚠ **Stage 67 `LFineLightConfig` 에는 PageCount/Page 필드 없음** → 본 SPEC 이 `LightControllerEntry.PageCount` 로 보강. 컨트롤러 모델별 실제 PageCount 는 확인 필요 #3.

### 2.5 io_set.lightSource.json — 실 매핑 (8채널 / COM1·2·3)
| Idx | Name | Port | Lv | Idx | Name | Port | Lv |
|---|---|---|---|---|---|---|---|
| 1 | INPUT STAGE RING | COM1 | 128 | 5 | BIN VISION | COM3 | 140 |
| 2 | BOTTOM VISION | COM2 | 180 | 6 | TOP SIDE VISION | COM3 | 200 |
| 3 | SIDE VISION 1 | COM2 | 200 | 7 | BOTTOM SIDE VISION | COM3 | 200 |
| 4 | SIDE VISION 2 | COM2 | 200 | 8 | ALIGN MARK ILLUM | COM1 | 100 |

→ io_set = **3 포트**, 본 장비 결정 = **2 컨트롤러** → 마이그레이션 시 재매핑 필요 (M-68-4, 확인 필요 #1).

### 2.6 ⚠ 드라이버 인터페이스 갭 (구현 Stage 사전 작업)
- Stage 67 `ILightController` 메서드: Connect/Disconnect/SetPower/SetStrobeTime/SetOnOff/GetPower/CheckPowerOn. **`SwitchPageAsync` 없음.**
- 본 SPEC §5 Apply 가 페이지 전환을 쓰려면 구현 Stage 에서 `ILightController` 에 `Task<bool> SwitchPageAsync(int page)` 추가 필요. PageCount==1 이면 no-op. (확인 필요 #3 와 연동)

---

## 3. 데이터 모델 (의사 C#)

### 3.1 Setup 레이어 (`QMC.Common\Recipes\LightSystemSetup.cs` 신규) — 기구적, 제품 무관
```csharp
[DataContract]
public class LightSystemSetup
{
    [DataMember] public List<LightControllerEntry> Controllers      { get; set; } = new List<LightControllerEntry>();
    [DataMember] public List<AlgorithmLightWiring> AlgorithmWirings { get; set; } = new List<AlgorithmLightWiring>();
}

[DataContract]
public class LightControllerEntry
{
    [DataMember] public string PortName     { get; set; }   // 자연키(FK) — Setup 내 유일. 중복 금지
    [DataMember] public string Name         { get; set; }   // "Main Illuminator" — 사람용 라벨(FK 아님)
    [DataMember] public int    BaudRate     { get; set; } = 9600;
    [DataMember] public int    ChannelCount { get; set; } = 8;   // 페이지당 채널
    [DataMember] public int    PageCount    { get; set; } = 1;   // 1 = 페이지 미사용
    [DataMember] public int    MaxPower     { get; set; } = 240;
    [DataMember] public int    MaxOnTimeUs  { get; set; } = 999;
    [DataMember] public List<LightChannelLabel> ChannelLabels { get; set; } = new List<LightChannelLabel>();
}

[DataContract]
public class LightChannelLabel
{
    [DataMember] public int    Channel { get; set; }   // 1~ChannelCount
    [DataMember] public string Name    { get; set; }   // "INPUT STAGE RING"
    [DataMember] public string Color   { get; set; } = "White";
}

/// <summary>알고리즘 1 ↔ 컨트롤러 1 + 사용 채널 풀. 기구적 결선(거의 불변).</summary>
[DataContract]
public class AlgorithmLightWiring
{
    [DataMember] public string    Algorithm      { get; set; }   // VisionAlgorithm 상수
    [DataMember] public string    ControllerPort { get; set; }   // LightControllerEntry.PortName FK
    [DataMember] public List<int> Channels       { get; set; } = new List<int>();  // 다중. 예 Bottom=[3,4,5,6]
    [DataMember] public int       Page           { get; set; } = 0; // PageCount==1 면 0 고정
}
```
- **FK = PortName** (확인 필요 §12 결정됨). 리스크 완화: Setup 진입 sanity check + "포트 일괄 변경" 도구(확인 필요 #8).

### 3.2 Recipe 레이어 — 검사별 값 (제품 파라미터만)
**Controller/Page 는 Setup 의 AlgorithmLightWiring 에서 추론 → Recipe 에 중복 저장 안 함.**
```csharp
[DataContract]
public class InspectionLightSetting
{
    [DataMember] public int  Channel      { get; set; }   // 소속 알고리즘 Wiring.Channels 풀 내 값
    [DataMember] public int  Level        { get; set; }   // 0~MaxPower
    [DataMember] public bool On           { get; set; } = true;
    [DataMember] public int  StrobeTimeUs { get; set; } = 0;
}

[DataContract]
public class InspectionLightOverride
{
    [DataMember] public string InspectionId { get; set; } = "";   // "EjectPinFinder"
    [DataMember] public List<InspectionLightSetting> Settings { get; set; } = new List<InspectionLightSetting>();
}
```
- **제약**: 각 `Setting.Channel` 은 소속 알고리즘 `AlgorithmLightWiring.Channels` 풀 안에 있어야 함. 밖이면 `LIGHT-CHANNEL-OUT-OF-POOL` + 해당 Setting 스킵.

### 3.3 AlgorithmCameraSubset 통합 — **옵션 A (확인 필요 #2, 추천)**
```csharp
// AlgorithmCameraMapping 에 추가
[DataMember(EmitDefaultValue = false)]
public List<InspectionLightOverride> InspectionLights { get; set; }   // null = 조명 미사용 (구버전 호환)
```
- 카메라 오버라이드(Stage 64 `InspectionCameraOverride`) 와 같은 `algorithm_camera.json` 에서 검사별로 함께 조회. 옵션 B = 별도 `algorithm_light.json` 분리.

---

## 4. UI

### 4.1 Setup 페이지 (`LightSystemSetupPage` 신규) — 저장 `Config\light_system.json`
- 진입: SettingsPage 좌측 트리 "■ 시스템 설정 / 조명 시스템" (신규 prefix `sys:light`)
- **섹션 1 — 컨트롤러 인벤토리**: 컨트롤러 목록(2개) + (Port/Baud/ChannelCount/PageCount/MaxPower) + 채널 라벨 표(Channel/Name/Color). Validation: PortName 중복 거부, ChannelCount>0.
- **섹션 2 — 알고리즘 결선 표** (5 알고리즘 고정):

  | 알고리즘 | 컨트롤러(콤보) | 사용 채널(다중 체크) | 페이지 |
  |---|---|---|---|
  | Wafer | COM? ▼ | ☐1..☐8 | 0 |
  | Bin | COM? ▼ | ☐1..☐8 | 0 |
  | BottomInspection | COM1 ▼ | ☑3 ☑4 ☑5 ☑6 | 0 |
  | FrontSide | COM1 ▼ | ☑1 | 0 |
  | RearSide | COM1 ▼ | ☑2 | 0 |

  - 콤보 표시 `Name (PortName)`, 저장 PortName. 채널은 선택 컨트롤러 1~ChannelCount CheckedListBox.
  - Validation: 같은 (Controller,Channel) 두 알고리즘 겹침 → 경고(차단 안 함). 빈 풀 = 조명 미사용 OK. 신규 알고리즘은 EnsureDefaults 빈 항목.
- **포트 일괄 변경 버튼** (확인 필요 #8) — 옛 PortName→새 PortName 으로 LightControllerEntry + 모든 AlgorithmWiring.ControllerPort 원자적 동시 갱신.

### 4.2 검사별 조명 패널 (`InspectionLightPanel`) — Recipe
- 진입: Stage 64 검사 노드(`cam:<alg>:<inspId>`) 우측 디테일. 카메라 오버라이드 패널과 **배치 = 확인 필요 #6** (탭/좌우분할/아래 섹션).
- **결선 헤더(읽기 전용)**: `검사 / 소속 알고리즘 / 결선: COM1 Page0 풀:[3,4,5,6]`. 결선 미설정 → 경고 + Setup 이동 링크. 풀 비면 패널 비활성.
- **값 편집 표(풀 내 채널만)**:

  | Channel(콤보=풀만) | Level(슬라이더+숫자) | On | Strobe(us) |
  |---|---|---|---|
  | 3 (BOTTOM VISION) ▼ | 180 | ✓ | 0 |

  - Controller/Page 컬럼 없음(결선 추론). 행 추가는 풀 잔여 채널 있을 때만.
  - 버튼: **Apply**(LightHub 라이브) / **Save**(영속화) / **Reset**(행 제거) / **Cancel**(리로드) / **On/Off All**.
  - 슬라이더 ValueChanged → 디바운스(확인 필요 #4, 기본 50ms) → 라이브.

---

## 5. 시퀀스 (Save / Load / Apply)
```
Save:  표 편집 → InspectionLightPanel.GetOverride()
       → AlgorithmCameraMapping.InspectionLights upsert → algorithm_camera.json 영속화

Load:  Form1.Load → AlgorithmCameraMapStore.Load()
       → 검사 노드 진입 시 해당 InspectionLightOverride 로 표 채움

Apply (수동, LightHub):
  algorithm = algorithmOf(inspectionId)
  wiring    = setup.AlgorithmWirings.First(w => w.Algorithm == algorithm)
  if wiring == null || wiring.ControllerPort == null:  Raise(LIGHT-WIRING-MISS) + abort
  ctrl = LightHub.Get(wiring.ControllerPort)
  if ctrl == null:                                     Raise(LIGHT-MAP-INVALID) + abort
  if ctrl.PageCount > 1:  ctrl.SwitchPageAsync(wiring.Page)   // ← 구현 Stage 가 ILightController 에 추가
  foreach s in override.Settings:
      if !wiring.Channels.Contains(s.Channel): Raise(LIGHT-CHANNEL-OUT-OF-POOL) + continue
      ctrl.SetPowerAsync(s.Channel, s.Level)
      ctrl.SetOnOffAsync(s.Channel, s.On)
      if s.StrobeTimeUs > 0: ctrl.SetStrobeTimeAsync(s.Channel, s.StrobeTimeUs)
```

---

## 6. 런타임 자동 적용 (범위 밖 — 다음 Stage)
- `VisionModule.Grab(inspectionId)` 직전: 효과적 LightOverride → 페이지 전환 → 채널 적용 → 안정화 wait → Grab → (옵션) 끄기.
- 같은 컨트롤러를 다른 검사가 다른 페이지로 → 컨트롤러별 `SemaphoreSlim` 직렬화 (카메라와 동일 패턴).

---

## 7. 마이그레이션 (io_set.lightSource.json → light_system.json)
- PORT 별 그룹핑 → LightControllerEntry 생성 + ChannelLabels/DefaultLevel 채움.
- 채널 이름 휴리스틱 → AlgorithmWirings 자동 채움: WAFER→Wafer, BIN→Bin, "BOTTOM VISION"/"BOTTOM INSP"→BottomInspection, "TOP SIDE"/"FRONT SIDE"→FrontSide, "BOTTOM SIDE"/"REAR SIDE"→RearSide. "RING"/"ALIGN MARK" 모호 → 자동 결선 안 함, 사용자 검토 안내 (확인 필요 #5).
- ⚠ io_set 3 포트 → 본 장비 2 컨트롤러 재매핑 필요 (M-68-4).
- 원본 백업 `io_set.lightSource.json.bak.YYYYMMDD` (확인 필요 #7).

---

## 8. 알람 (LFine 6 재사용 + 신규 3)
- 재사용 (Stage 67 등록 완료): `LIGHT-OPEN-FAIL` / `LIGHT-TIMEOUT` / `LIGHT-NAK` / `LIGHT-INVALID-RESP` / `LIGHT-TX-FAIL` / `LIGHT-PWR-RANGE`
- **신규 3** (구현 Stage 에서 `QMC.Common\Alarms\AlarmMaster.cs` 등록):
  - `LIGHT-WIRING-MISS` (Warning) — 알고리즘 AlgorithmLightWiring 없음 / ControllerPort 빈값
  - `LIGHT-MAP-INVALID` (Warning) — Wiring.ControllerPort 가 LightHub 미등록 포트
  - `LIGHT-CHANNEL-OUT-OF-POOL` (Warning) — Setting.Channel 이 알고리즘 풀 밖
- Vision 로컬 raise (Stage 67 정책 동일).

---

## 9. 영향 범위
### 신규 파일
- `QMC.Common\Recipes\LightSystemSetup.cs` — LightControllerEntry / LightChannelLabel / AlgorithmLightWiring / Store
- `QMC.Common\Recipes\InspectionLightSubset.cs` — InspectionLightSetting / InspectionLightOverride (또는 AlgorithmCameraSubset 확장)
- `QMC.Vision\Comm\LightHub.cs` — Dictionary<PortName, ILightController>
- `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs`
- `QMC.Vision\Ui\Pages\InspectionLightPanel.cs`
- `QMC.Vision\Config\LightSystemMigrator.cs`
### 수정 파일
- `QMC.Common\Recipes\AlgorithmCameraSubset.cs` — `AlgorithmCameraMapping.InspectionLights` (옵션 A)
- `QMC.Vision\Optics\ILightController.cs` — `SwitchPageAsync(int)` 추가 (§2.6, PageCount>1 시)
- `QMC.Vision\Ui\Pages\SettingsPage.cs` — 트리 "조명 시스템" 노드 + 검사 노드에 InspectionLightPanel 결합
- `QMC.Vision\Form1.cs` — LightHub 초기화
- `QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs` (실제 위치 `QMC.Common\Alarms\AlarmMaster.cs`) — 신규 3 알람

---

## 10. 시퀀스 문서 04~08 영향
- 본 Stage(UI/모델) 직접 영향 없음. **다음 Stage(런타임 자동 적용)** 에서 06_TransferPicker / 07_OutputStage 비전 호출 절에 "조명 적용 → 그랩" 단계 추가 필요할 수 있음 (메모).

---

## 11. Mismatch
| # | 위치 | 내용 |
|---|---|---|
| M-68-1 | IlluminatorPanel.cs:29 | 4채널 더미 — ValueChanged 가 라벨 텍스트만, 하드웨어 명령 0 |
| M-68-2 | FinderPage:55 / InspectorPage:53 | IlluminatorPanel 에 검사 컨텍스트 미전달 |
| M-68-3 | LightControllerPage:180-198 | 시리얼 Write 1회 시도만, 검사별 매핑/응답 없음, Vision 비결선 |
| M-68-4 | 매뉴얼(2 컨트롤러) / io_set(3 포트) / 본 결정(2 컨트롤러) | io_set 8채널 3포트를 2 컨트롤러로 재매핑 필요 (Stage 66 #5 동기) |
| M-68-5 | Stage 67 ILightController | SwitchPageAsync 부재 — Page 축 사용 시 구현 Stage 에서 추가 |

---

## 12. 확인 필요 항목 (사용자 컨펌 요청)
> **결정됨**: 컨트롤러 FK=`PortName`. `Id` 필드 없음. Setup 내 PortName 유일성 강제. 컨트롤러 개수 = **2** (Stage 66 #5).

1. **채널 풀 배정** — Wafer/Bin 은 어느 컨트롤러·채널? COM2 사용 알고리즘은? (BottomInspection=COM1[3,4,5,6] / FrontSide=COM1[1] / RearSide=COM1[2] 는 예시 확정)
2. **저장 통합** — 옵션 A(algorithm_camera.json 합침, 추천) vs B(algorithm_light.json 분리)?
3. **PageCount** — LFine 실제 모델이 페이지 기능 있는지? 없으면 PageCount=1 고정 + SwitchPageAsync no-op.
4. **Live preview 디바운스** — 50ms 기본 OK?
5. **마이그레이션 자동 매핑 정책** — 휴리스틱 추정 + 모호 항목(RING/ALIGN) 미할당 후 사용자 검토 — OK?
6. **InspectionLightPanel 배치** — (a)좌우분할 (b)탭 (c)카메라 패널 아래 섹션?
7. **백업 파일명** — `.bak.YYYYMMDD` OK?
8. **포트 일괄 변경 도구** — Setup 페이지에 포함? (추천: 포함)

---

## 13. 다음 단계
SPEC + CHECKLIST 컨펌 → 구현 Stage(69). 구현 프롬프트: **모니터 2번 검증 규칙** 자동 포함. LFine 드라이버(Stage 67) 완료 상태라 라이브 적용 시연 가능 (단 ILightController.SwitchPageAsync 선 추가 필요).
예상 작업 시간: 8~12시간 (모델 2h + LightHub 1h + Setup UI 3h + InspectionLightPanel 2.5h + 마이그레이션 1.5h + 검증 1.5h).
