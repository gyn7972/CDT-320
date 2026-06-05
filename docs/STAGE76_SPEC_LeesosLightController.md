# STAGE 76 — SPEC: LeesOS 디지털 조명 컨트롤러 (2차 벤더) 관리 클래스

- **작성일**: 2026-06-02
- **단계**: SPEC (구현 전, 사용자 컨펌 대기) — **본 Stage 는 SPEC + CHECKLIST 까지만, 코드 변경 없음**
- **Stage 번호 근거**: docs/ 최신 = `STAGE70_*`, git 코드 마커 최신 = Stage 75 (Stage 76 LFine 프레임드롭은 되돌림 → 번호 가용) → **NN = 76**
- **브랜치**: `stage-76-leesos-light-controller-spec` (base: master 통합본, working tree clean 확인)
- **목표**: 기존 LFine 구조(ILightController/LightHub/Factory) 위에 **두 번째 벤더 LeesOS** 컨트롤러 추가

---

## 1. 목표 & 범위

### 목표
LeesOS 디지털 조명 컨트롤러를 **RS-232C** 로 제어하는 **Vision 측 관리 클래스** 신설. 기존 `ILightController` 인터페이스 그대로 구현 → `LightHub` 가 PortName 라우팅으로 자연 통합. Sim/실장비 fallback(BaseAxis 패턴), 비동기, **알람 재사용**.

### 범위 (In Scope) — 구현 Stage 기준
- 신규 클래스: `LeesosLightController`(실장비) + `LeesosLightConfig`/`LeesosChannel` + `LeesosProtocol`(정적 명령 빌더/응답 파서)
- `LightControllerEntry.Vendor` 필드 + 마이그레이션
- `LightControllerFactory` 벤더 분기 + entry→Config 어댑터
- `LightHub.Initialize` 가 entry 기반 호출(벤더별 인스턴스)
- `LightSystemSetupPage` 인벤토리 그리드에 `Vendor` 컬럼

### 범위 외 (Out of Scope)
- **새 인터페이스 / 새 알람 / 새 화면** — 기존 구조 위 벤더 추가만.
- LeesOS 레거시 베이스(`SerialComm`/`IlluminatorConfig`/`ListParam`/`Param`/`BytesConverter`) 직접 포팅 — **프로토콜 의미만 차용** (M-76-1).
- Page 축, Strobe — LeesOS 미지원(no-op).

---

## 2. 현황 분석 (코드 직접 재확인 — 줄번호/시그니처)

### 2.1 재사용 추상화 (그대로)
| 파일 | 확인 결과 |
|---|---|
| `QMC.Vision\Optics\ILightController.cs:12-44` | **속성 3개**(IsConnected/PortName/ChannelCount) + **메서드 9개**(ConnectAsync/DisconnectAsync/SetPowerAsync/SetStrobeTimeAsync/SetOnOffAsync/GetPowerAsync/CheckPowerOnAsync/SwitchPageAsync/**ReceiveResponseAsync**) + IDisposable. ※ 사전조사의 "11 메서드" 는 부정확 → **정정: 9 메서드 + 3 속성**. |
| `QMC.Vision\Optics\Sim\SimLightController.cs` | 벤더 무관 공통. 그대로 재사용. |
| `QMC.Vision\Comm\LightHub.cs:22` | `Initialize(LightSystemSetup setup, bool useSim)` — 각 entry 로 `LFineLightConfig` 빌드 후 `LightControllerFactory.Create(cfg, useSim)`. **벤더 분기 없음** → 갱신 대상. |
| `QMC.Vision\Optics\LightControllerFactory.cs:14` | `Create(LFineLightConfig cfg, bool useSim)` — **LFine 만 분기** → 확장 대상. |
| `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs:71-76` | 인벤토리 그리드 컬럼 = PortName/Name/BaudRate/ChannelCount/PageCount/MaxPower. **Vendor 컬럼 없음** → 추가 대상. |

> **핵심 호재**: Stage 75 에서 `ReceiveResponseAsync(int timeoutMs=0)` 가 인터페이스에 이미 추가됨. LeesOS 는 응답형이므로 이 메서드가 **응답 검증의 핵심 경로**로 그대로 쓰인다 (사전조사 시점엔 없던 메서드).

### 2.2 Setup 모델 한 곳 부족 — **`Vendor` 필드 없음**
`QMC.Common\Recipes\LightSystemSetup.cs:74` `LightControllerEntry` 필드:
`PortName / Name / BaudRate(9600) / ChannelCount(8) / PageCount(1) / MaxPower(240) / MaxOnTimeUs(999) / ChannelLabels`.
- **`Vendor` 없음** → 추가 필요.
- **주의(프로젝트 기존 버그)**: 이 클래스에 `[OnDeserializing]`/`[OnDeserialized]` 콜백이 **없다**. `DataContractJsonSerializer` 는 프로퍼티 이니셜라이저를 역직렬화 때 실행하지 않으므로, `public string Vendor {get;set;} = "LFine";` 만으로는 **구버전 JSON 로드 시 Vendor=null** 이 된다. → **반드시 `[OnDeserializing]` 콜백에서 `Vendor="LFine"` 기본값을 심어야 한다** (VisionSettings 의 선례와 동일). 사전조사의 "OnDeserialized" 표현을 **`[OnDeserializing]` 으로 정정**.

### 2.3 LeesOS 참조 코드 (이전 프로젝트 — 직접 포팅 불가, 프로토콜만 차용)
경로 `D:\Work\CDT-320\LightControl\Optics\Leesos\` (namespace `QMC.Common.Vision.Optics.Leesos`):
- `DigitalIlluminator.cs` (255줄) — 고수준(Illuminator 추상 상속)
- `DigitalIlluminatorCommunicator.cs` (242줄) — **시리얼 프로토콜 실 구현**
- `DigitalIlluminatorConfig.cs` (210줄) — Serial 설정

레거시 베이스 의존(`SerialComm`, `IlluminatorConfig`, `ListParam`, `Param`, `BytesConverter`) — 본 프로젝트에 없음 → 직접 포팅 불가. **프로토콜 의미만 재현**.

---

## 3. ⚠ 핵심 불일치 — 명령 포맷 (사전조사 정정)

사전조사(프롬프트 §3·§7·§8)의 명령 포맷 `H1ON`/`C255`/`SPWR1` 은 참조 코드의 **주석(`DigitalIlluminatorCommunicator.cs:56-57`)** 에서 가져온 값이다. **실제 코드(`GetCommandText` + 송신부)는 다른 prefix(`LH`/`LC`/`LS`) 를 보낸다.**

| 동작 | 사전조사(주석 기반, **오류**) | **실제 코드(정답 근거)** | 코드 위치 |
|---|---|---|---|
| ON | `H1ON` | **`LH1ON`** = `"LH"`+ch+`"ON"` | `GetCommandText:129`(`LH`) + `OnTurnOnOff:205` |
| OFF | `H1OF` | **`LH1OF`** = `"LH"`+ch+`"OF"` | 동상 (StatusOff=`"OF"`:32) |
| Volume | `C255`/`C{X2}` | **`LC1FF`** = `"LC"`+ch+`vol.ToString("X2")` | `GetCommandText:132`(`LC`) + `OnSetVolume:234` |
| Status(CheckPowerOn) | `SPWR1` | **`LS101`** = `"LS"`+ch+`"01"`, 응답 `Substring(2,2)=="ON"` | `GetCommandText:125`(`LS`) + `OnCheckPowerOn:162,168` |

- **채널**: `channel.ToString()` (10진, prefix 직후 concat). 예 ch=1 → `LH1ON`.
- **Volume**: `volume.ToString("X2")` (대문자 hex 2자리) → 사실상 **0~255(00~FF)**.
- **프레임 종료(Etx)**: `Etx1=0x0D`, `Etx2=0x0A`. 송신 `SendFrame(text, this.Etx)` 에 **2바이트 배열 `{0x0D,0x0A}`** 전달, 수신 `ReceiveFrame(out data, Etx2)` 는 **`0x0A` 까지** 누적. (사전조사의 "`\r` 만 보내고" 는 `SerialComm.SendFrame` 내부 동작에 의존 — 본 프로젝트에 그 클래스가 없어 **확인 필요**.)
- **응답 prefix**: `StartText="R"`, 에러 `ErrorCode="ERR"` → 에러 응답 `"RERR"`. 정상 echo 예: `LH1ON`→`R1ON`, `LC1FF`→`R...`.
- **중요**: 참조 코드의 echo 검증 로직은 **주석 처리(108-115)** 되어 있어 실제론 응답을 읽기만 하고 검증하지 않는다. **즉 "echo 검증 필수" 는 본 프로젝트의 설계 추가지, 레퍼런스 동작이 아니다.**

> **결론**: SPEC/구현은 **실제 코드 포맷(`LH`/`LC`/`LS`)** 을 1차 기준으로 한다. 단, 펌웨어 매뉴얼 부재 + 주석과 코드 불일치 때문에 **구현 Stage 진입 전 실 장비 hex dump 1회로 최종 확정 필수** (확인 필요 #2).

---

## 4. 클래스 설계 (`QMC.Vision\Optics\Leesos\` 신규 — 의사 C#)

```csharp
public class LeesosLightController : ILightController
{
    private readonly LeesosLightConfig _cfg;
    private SerialPort _port;
    private readonly SemaphoreSlim _ioGate = new SemaphoreSlim(1, 1);   // 송수신 1쌍 직렬화
    private readonly int[] _power;        // 1-기반 캐시
    private readonly int[] _lastOnPower;  // On/Off 복원

    public bool   IsConnected { get; private set; }
    public string PortName    => _cfg.PortName;
    public int    ChannelCount => _cfg.ChannelCount;

    public Task<bool> ConnectAsync();      // _port.Open, 실패 시 LIGHT-OPEN-FAIL + false
    public Task       DisconnectAsync();

    // 밝기 = Volume. 0~255 클램프(MaxPower). "LC{ch}{power:X2}" 송신 → echo "R..." 검증
    public Task<bool> SetPowerAsync(int channel, int power);

    // LeesOS Strobe 미지원(참조 명령 없음) → no-op + true (확인 필요 #4)
    public Task<bool> SetStrobeTimeAsync(int channel, int onTimeUs);

    // "LH{ch}{ON|OF}" 송신 → echo 검증. OFF=LHxOF, ON=LHxON
    public Task<bool> SetOnOffAsync(int channel, bool on);

    public Task<int>  GetPowerAsync(int channel);   // 캐시 우선

    // "LS{ch}01" 송신 → 응답 Substring(2,2)=="ON" 판정 (확인 필요 #5)
    public Task<bool> CheckPowerOnAsync(int channel);

    public Task<bool> SwitchPageAsync(int page);    // Page 미지원 → 항상 true(no-op)

    // ReadTo(0x0A) → StartText 'R' 검증. "RERR" 이면 LIGHT-NAK + null. (Stage 75 인터페이스 멤버 그대로 구현)
    public Task<string> ReceiveResponseAsync(int timeoutMs = 0);

    public void Dispose();
}
```

### 송수신 직렬화
LeesOS 는 명령마다 1요청-1응답이므로 `_ioGate`(SemaphoreSlim) 로 **(송신→수신) 쌍을 원자화**. LFine 의 단순 `lock(_txLock)` 송신 전용과 다른 점.

---

## 5. Config 모델 (`QMC.Vision\Optics\Leesos\LeesosLightConfig.cs`)

참조 `DigitalIlluminatorConfig` 기본값 기준 (COM1/9600/8/One/None/None/TimeOut 1000):
```csharp
[DataContract]
public class LeesosLightConfig
{
    [DataMember] public string PortName     { get; set; } = "COM2";   // LFine 와 충돌 회피용 placeholder
    [DataMember] public int    BaudRate     { get; set; } = 9600;     // 참조 config 일치 (확인 필요 #1)
    [DataMember] public int    DataBits     { get; set; } = 8;
    [DataMember] public string StopBits     { get; set; } = "One";
    [DataMember] public string Parity       { get; set; } = "None";
    [DataMember] public string Handshake    { get; set; } = "None";
    [DataMember] public int    TimeoutMs    { get; set; } = 1000;     // 응답형 → 적용됨
    [DataMember] public int    MaxPower     { get; set; } = 255;      // hex 2자리이므로 0~255
    [DataMember] public int    MaxOnTimeUs  { get; set; } = 0;        // Strobe 미지원
    [DataMember] public int    ChannelCount { get; set; } = 8;
    [DataMember] public List<LeesosChannel> Channels { get; set; } = new List<LeesosChannel>();
}
```
> 단, 운영 모델의 단일 진실원은 `LightControllerEntry`(Vendor 포함). `LeesosLightConfig` 는 어댑터(`ToLeesosConfig`)로 생성된다.

---

## 6. `LightControllerEntry` 에 `Vendor` 필드 추가 (필수)

대상 `QMC.Common\Recipes\LightSystemSetup.cs:74`.
```csharp
[DataMember(EmitDefaultValue = false)] public string Vendor { get; set; } = "LFine";   // "LFine" | "Leesos"
```
**마이그레이션 (정정: OnDeserializing)** — 구버전 JSON 에 Vendor 키 없으면 `null` 이 되므로:
```csharp
[OnDeserializing] internal void OnDeserializing(StreamingContext c) { Vendor = "LFine"; }
[OnDeserialized]  internal void OnDeserialized (StreamingContext c) { if (string.IsNullOrEmpty(Vendor)) Vendor = "LFine"; }
```
→ "키 있으면 덮어쓰고, 없으면 LFine" 보장. 다음 Save 부터 Vendor 키 기록.

---

## 7. `LightControllerFactory` 벤더 분기 + 어댑터

대상 `QMC.Vision\Optics\LightControllerFactory.cs`.
```csharp
// 신규 entry 기반 시그니처 (LightHub 가 이걸 호출)
public static ILightController Create(LightControllerEntry entry, bool useSim)
{
    if (useSim || entry == null) return new SimLightController(entry?.ChannelCount ?? 8);
    switch ((entry.Vendor ?? "LFine"))
    {
        case "LFine":  return new LFineLightController (entry.ToLFineConfig());
        case "Leesos": return new LeesosLightController(entry.ToLeesosConfig());
        default:
            AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-MAP-INVALID", "LightControllerFactory",
                               $"Unknown vendor '{entry.Vendor}' — Sim fallback");
            return new SimLightController(entry.ChannelCount);
    }
}
```
- 기존 `Create(LFineLightConfig, bool)` 시그니처는 **유지**(호환) 또는 신규가 내부 호출.
- 어댑터 `ToLFineConfig`/`ToLeesosConfig`: `LightControllerEntry` → 벤더 Config (PortName/BaudRate/ChannelCount/MaxPower/MaxOnTimeUs 매핑). 정적 헬퍼 또는 확장 메서드.
- **`LightHub.Initialize`** 를 entry 기반 `Create(entry, useSim)` 호출로 갱신 (현재는 LFineLightConfig 직접 빌드).

---

## 8. 시퀀스 (Send/Receive — LeesOS, 실제 코드 포맷 기준)

```
SetPowerAsync(ch=1, power=255):
  await _ioGate.WaitAsync()
  power = clamp(power, 0, 255)                     // MaxPower
  cmd = $"LC{ch}{power:X2}"                         // "LC1FF"
  _port.Write(ASCII(cmd) + {0x0D,0x0A})            // 송신
  resp = ReceiveResponseAsync(_cfg.TimeoutMs)       // ReadTo(0x0A) → "R..."
  if resp == null:            Raise(LIGHT-TIMEOUT)      + return false
  if resp.EndsWith("ERR"):    Raise(LIGHT-NAK)          + return false   // "RERR"
  if !resp.StartsWith("R"):   Raise(LIGHT-INVALID-RESP) + return false
  cache[ch] = power; if(power>0) lastOn[ch]=power
  return true
  finally: _ioGate.Release()

SetOnOffAsync(ch=1, on=true):
  cmd = $"LH{ch}{(on ? "ON" : "OF")}"               // "LH1ON" / "LH1OF"
  ... (동일 송수신/검증)

CheckPowerOnAsync(ch=1):
  cmd = $"LS{ch}01"                                  // "LS101"
  resp = ...; return resp.Length>=4 && resp.Substring(2,2)=="ON"
```
> echo 일치 비교(`resp == "R" + 기대값`)는 펌웨어 응답 포맷 확정(확인 필요 #2/#5) 후 강화. 그 전엔 `R` prefix + non-`ERR` 까지만 판정.

---

## 9. UI 변경 (`LightSystemSetupPage`)

대상 `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs` 인벤토리 그리드(`_gridCtrl`, 컬럼 추가는 `BuildLayout` 의 `_gridCtrl.Columns.Add` 블록).
- 컬럼 **추가**: `Vendor` (`DataGridViewComboBoxColumn`, Items=["LFine","Leesos"]). 위치: PortName 옆.
- 신규 컨트롤러 추가(`AddController`) 시 기본 Vendor="LFine".
- Vendor 변경 시 ChannelCount/PageCount/MaxPower 기본값을 그 벤더 기본값으로 **제안**(현재 값이 0/빈 값일 때만 자동 채움).
- (옵션) Vendor=Leesos 시 PageCount 셀 readonly 1 강제 — 확인 필요 #7.
- ※ `CollectFromUi`/`BindAllCore`/`_labelCache`(Stage 71) 도 Vendor 왕복 반영 필요.

---

## 10. 알람 (재사용 — 신규 0)
LFine 등록 알람 **재사용**: `LIGHT-OPEN-FAIL` / `LIGHT-TIMEOUT` / `LIGHT-PWR-RANGE` / `LIGHT-TX-FAIL` / `LIGHT-MAP-INVALID`. 응답형 추가 의미:
- `LIGHT-NAK` — `"RERR"` 수신
- `LIGHT-INVALID-RESP` — `R` prefix 아님/형식 불일치
> **확인 필요**: `LIGHT-NAK`/`LIGHT-INVALID-RESP` 가 현재 `AlarmMaster` 에 **등록돼 있는지 구현 Stage 에서 grep 확인** (사전조사는 등록 가정이나 verify_all 의 LIGHT-* 체크는 OPEN-FAIL/TIMEOUT/PWR-RANGE 3개만 단언). 미등록이면 등록 추가 + verify 개수 갱신.

---

## 11. 영향 범위
- **신규 파일**: `LeesosLightController.cs`, `LeesosLightConfig.cs`(+`LeesosChannel`), `LeesosProtocol.cs`(정적)
- **수정 파일**:
  - `QMC.Common\Recipes\LightSystemSetup.cs` — Vendor 필드 + OnDeserializing/OnDeserialized
  - `QMC.Vision\Optics\LightControllerFactory.cs` — 벤더 스위치 + 어댑터
  - `QMC.Vision\Comm\LightHub.cs` — Initialize 가 entry 기반 Create 호출
  - `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs` — Vendor 컬럼 + 왕복
  - (조건부) `QMC.Common\Alarms\AlarmMaster.cs` — LIGHT-NAK/INVALID-RESP 미등록 시 추가
- **시퀀스 문서 04~08**: 직접 영향 없음 (Vision 측 드라이버 벤더 추가).

---

## 12. Mismatch / 결정 충돌
| # | 위치 | 내용 |
|---|---|---|
| M-76-1 | `LightControl/Optics/Leesos/*` | 레거시 베이스(SerialComm/IlluminatorConfig/ListParam/Param/BytesConverter) 의존 → 직접 포팅 불가, **프로토콜만 차용** |
| M-76-2 | `DigitalIlluminatorCommunicator.cs:56-57`(주석) vs `125-234`(코드) | 명령 prefix **주석=H/C/SPWR, 코드=LH/LC/LS 불일치** → 코드(LH/LC/LS) 채택 + 실 장비 hex dump 로 최종 확정 (확인 필요 #2) |
| M-76-3 | `ILightController` (응답 가정) | 인터페이스는 LFine 무응답 가정으로 설계되나 Stage 75 `ReceiveResponseAsync` 존재 → Leesos 응답 검증을 그 메서드로 수행, **시그니처 변경 없음** |
| M-76-4 | 사전조사 "11 메서드"/"OnDeserialized" | 실제 = 9 메서드+3속성 / 마이그레이션은 `[OnDeserializing]` 필요 → 정정 |
| M-76-5 | `LightControllerEntry` Vendor 없음 + 콜백 없음 | DataContract 이니셜라이저 미실행 → OnDeserializing 기본값 필수 |

---

## 13. 확인 필요 항목 (사용자 컨펌 요청 — 구현 진입 전)
1. **시리얼 기본값** — 참조 config = Baud **9600 / 8N1 / Handshake.None**. 실 장비 명판/매뉴얼과 일치?
2. **명령 포맷 정확성** — 실제 코드 = **`LH{ch}ON|OF` / `LC{ch}{X2}` / `LS{ch}01`** (주석의 H/C/SPWR 아님). 실 장비 **hex dump 1회**로 최종 확정 필요(매뉴얼 부재).
3. **응답 timeout** — 1000 ms 기본 OK? (LeesOS 는 응답 대기형).
4. **Strobe 지원 여부** — 참조에 strobe 명령 없음 → `SetStrobeTimeAsync` no-op + true 로 확정해도 되나?
5. **CheckPowerOn 응답 포맷** — `LS{ch}01` 응답의 `Substring(2,2)=="ON"` 판정 규칙(StatusBright="00"/OnOff="01"/Error="02") 정확한가?
6. **1포트=1컨트롤러** — 데이지체인 아닌 1포트 1대 가정 OK?
7. **Page 미지원 UI** — Vendor=Leesos 시 PageCount 컬럼 readonly 1 강제할지?

---

## 14. 다음 단계
SPEC + CHECKLIST 컨펌 → 구현 Stage. 구현 프롬프트엔:
- **실 장비 hex dump 단계를 별도 검증 절로 명시** (명령 포맷 확정).
- **실행 검증은 보조 모니터(2번)에서**.
- Setup UI 의 Vendor 콤보가 콤보로 보이는지 스크린샷 검증.
- `LightHub`/Factory 는 분기만 추가, `LeesosLightController`+`LeesosProtocol` 신설.
