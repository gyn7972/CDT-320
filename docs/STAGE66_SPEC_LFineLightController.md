# STAGE 66 — SPEC: LFine 디지털 조명 컨트롤러 관리 클래스

- **작성일**: 2026-05-29
- **단계**: SPEC (구현 전, 사용자 컨펌 대기)
- **Stage 번호 근거**: docs/ 최신 = `STAGE64_*`, 코드 마커 최신 = Stage 65 (Maintenance→Recipe) → **NN = 66**
- **브랜치**: `stage-66-lfine-light-controller-spec` (base: master 통합본)
- **git**: 작업 시작 시 working tree clean 확인

---

## 1. 목표 & 범위

### 목표
LFine 디지털 조명 컨트롤러를 **시리얼(RS-232C)** 로 제어하는 **Vision 측 관리 클래스** 신설. Sim/실장비 동시 지원(BaseAxis 패턴), 비동기, 알람 통합.

### 범위 (In Scope) — 구현 Stage(67) 기준, **클래스 골격만** (#7 확정)
- 클래스 골격: `ILightController` + `LFineLightController`(실장비) + `SimLightController` + `LightControllerFactory`
- 시리얼 프로토콜 인코딩 (`LFineProtocol`) — 송신 전용(무응답, #4)
- Config 모델 (`LFineLightConfig` / `LFineChannel`, JSON 영속화) — **2 컨트롤러 List** (#5)
- 알람 통합 (신규 LIGHT-* 코드) — **Vision 로컬 raise** (#6)

### 범위 외 (Out of Scope)
- **UI 결선** (`IlluminatorPanel` 라이브 적용) — **클래스 골격만 결정(#7)** 이라 별도 Stage 로 분리
- **PS 변종** (`LFinePSDigitalIlluminator`) — 무시 확정 (#1, M-66-1)
- **응답 파싱** — 무응답 유지(#4)
- **Handler 알람 배너 전파** — Vision 로컬만(#6); TCP 전파는 별도 Stage
- 검사별 조명 override (Stage 62/64 형태 확장) — 별도 Stage
- 하드웨어 실결선/실장비 검증 — 구현 Stage (모니터 2번 검증 규칙 적용)
- 위치: `QMC.Vision\Optics\` (Vision 전용, 사용자 컨펌됨)
- 본 Stage(66) 는 **SPEC + CHECKLIST 까지만** — 코드 변경 없음

---

## 2. 현황 분석 (Read-only, 줄번호 재확인)

### 2.1 이전 프로젝트 LFine 코드 (참조용 — 직접 포팅 불가)
| 파일 | 줄 수 | 내용 |
|---|---|---|
| `LightControl\Optics\Illuminator.cs` | 192 | 베이스 추상 (`Part` 상속) — CheckPowerOn/SetVolume/TurnOnOff/SetStrobeOnTime/SetValue |
| `LightControl\Optics\LFine\LFineDigitalIlluminator.cs` | 303 | Strobe 타입 + 인라인 Communicator (`:203-302`) |
| `LightControl\Optics\LFine\LFineDigitalIlluminatorConfig.cs` | 251 | Serial 설정 (COM1/9600/8/One/None, MaxPower=240, MaxOnTime=999) |
| `LightControl\Optics\LFine\LFinePSDigitalIlluminator.cs` | 336 | PS 변종 — `LFinePSDigitalIlluminatorCommunicator` 참조하나 **파일 부재** (M-66-1) |

**⚠ 직접 포팅 불가**: 베이스 `SerialComm` / `Part` / `IlluminatorConfig` / `BrightSettingParameter` / `IlluminationChannel` / `ListParam` 는 이전 QMC.Core 의존, LightControl 폴더에 없음. **현 CDT-320 패턴으로 신규 작성**.

### 2.2 프로토콜 — 코드에서 직접 확인 (`LFineDigitalIlluminator.cs`)
`LFineDigitalIlluminatorCommunicator : SerialComm` (`:203`):
- `Stx = 0x02` (`:205`), `Etx = 0x03` (`:206`) — **확인 필요 #2 해소**
- `PowerText = "P"` (`:211`), `StrobeTimeText = "T"` (`:212`), `RemoteText = "R"` (`:213`)
- `Send(text)` → `SendFrame(text, Stx, Etx)` (`:235`) — **fire-and-forget** (응답 미파싱)
- `GetTimeCommand` (`:277`): `"{channel}{T}{time:000}{R}"` → 예 `1T050R`
- `GetPowerCommand` (`:282`): `"{channel}{P}{power:000}{R}"` → 예 `1P200R`
- `SetTime`(`:286`) / `SetPower`(`:295`) — Send 만, Receive 호출 안 함
- `Receive(ref response)` (`:246`) 존재하나 SetTime/SetPower 가 호출 안 함 → **응답 포맷 실사용 코드 없음 (확인 필요 #4 유지)**
- **On/Off 전용 명령 없음** — Power=0 으로 OFF 추정 (확인 필요 #3 유지)

### 2.3 현 CDT-320 자산 (재사용/연동 대상)
- `QMC.CDT-320\QMC.CDT-320\Ui\Pages\Settings\LightControllerPage.cs` (248줄, Stage 59) — UI 페이지. **하드웨어 비결선, JSON 그리드** (8채널: INDEX/NAME/PORT/LEVEL/MODE/Active/Color)
- `QMC.CDT-320\QMC.Vision\Ui\Controls\IlluminatorPanel.cs` (35줄) — **4채널 TrackBar 더미, 비결선**
- `QMC.CDT-320\bin\Debug\Config\io_set.lightSource.json` — 8채널 실 매핑 (COM1/2/3 3포트):
  ```
  1 INPUT STAGE RING   COM1 128    5 BIN VISION         COM3 140
  2 BOTTOM VISION      COM2 180    6 TOP SIDE VISION    COM3 200
  3 SIDE VISION 1      COM2 200    7 BOTTOM SIDE VISION COM3 200
  4 SIDE VISION 2      COM2 200    8 ALIGN MARK ILLUM   COM1 100
  ```

### 2.4 CDT-310 매뉴얼 (`manual_310_vision.txt`)
- Serial Port: **Illuminator communicator 1 + IlluminatorCommunicator2** = **컨트롤러 2개** 명시
- 검사 항목별 Illuminator control UI (사용 가능 조명 리스트 / 현재·저장 밝기 / Set·Save / On·Off)
- 평문 매뉴얼엔 **LFine 시리얼 프로토콜 상세표 없음** → §4 의 On/Off·응답은 "확인 필요" 유지

---

## 3. 클래스 설계 (의사 C#, `QMC.Vision.Optics`)

```csharp
public interface ILightController : IDisposable
{
    bool   IsConnected  { get; }
    string PortName     { get; }
    int    ChannelCount { get; }

    Task<bool> ConnectAsync();
    Task       DisconnectAsync();

    Task<bool> SetPowerAsync     (int channel, int power);      // 0 ~ MaxPower
    Task<bool> SetStrobeTimeAsync(int channel, int onTimeUs);   // 0 ~ MaxOnTimeUs
    Task<bool> SetOnOffAsync     (int channel, bool on);        // 내부: on=false → Power 0
    Task<int>  GetPowerAsync     (int channel);                 // 캐시 우선
    Task<bool> CheckPowerOnAsync (int channel);                 // 컨트롤러 헬스
}

public class LFineLightController : ILightController   // 실장비
{
    public LFineLightController(LFineLightConfig cfg);
    // SerialPort + 명령 큐(Task,CTS) + 응답 매처 + 알람 raise
    // Open 실패 → IsConnected=false + AlarmManager.Raise(LIGHT-OPEN-FAIL)
}

public class SimLightController : ILightController     // Sim
{
    public SimLightController(int channels = 8);
    // 모든 명령 캐시만 갱신, 0ms, IsConnected=true
}

public static class LightControllerFactory             // BaseAxis 패턴
{
    public static ILightController Create(LFineLightConfig cfg, bool useSim);
    // useSim=true → Sim, false → LFine (Sim 자동 fallback 은 명시 옵션으로만)
}
```

---

## 4. 시리얼 프로토콜 (코드 확인분 + 확인 필요)
| 항목 | 값 | 출처 |
|---|---|---|
| Stx / Etx | **0x02 / 0x03** | 코드 확인 (`:205-206`) |
| 프레임 | `<Stx>{payload}<Etx>` | 코드 확인 |
| Power | `{ch}{P}{power:000}{R}` 예 `1P200R` | 코드 확인 (`:282`) |
| StrobeTime | `{ch}{T}{time:000}{R}` 예 `1T050R` | 코드 확인 (`:277`) |
| On/Off | **미정** (Power=0 OFF 추정) | 확인 필요 #3 |
| 응답 | **미정** (실사용 코드 없음; Leesos 의 `R{echo}`/`RERR` 참고) | 확인 필요 #4 |
| Timeout | 1000 ms | Config 기본 |

에러 처리: `RERR` 수신 → `LIGHT-NAK`, 타임아웃 → `LIGHT-TIMEOUT`.

---

## 5. Config 모델 (DataContract — `Config\lfine_light.json`)
```csharp
[DataContract]
public class LFineLightConfig
{
    [DataMember] public string PortName     { get; set; } = "COM1";
    [DataMember] public int    BaudRate     { get; set; } = 9600;
    [DataMember] public int    DataBits     { get; set; } = 8;
    [DataMember] public string StopBits     { get; set; } = "One";
    [DataMember] public string Parity       { get; set; } = "None";
    [DataMember] public string Handshake    { get; set; } = "None";
    [DataMember] public int    TimeoutMs    { get; set; } = 1000;
    [DataMember] public int    MaxPower     { get; set; } = 240;
    [DataMember] public int    MaxOnTimeUs  { get; set; } = 999;
    [DataMember] public int    ChannelCount { get; set; } = 8;
    [DataMember] public List<LFineChannel> Channels { get; set; } = new List<LFineChannel>();
}

[DataContract]
public class LFineChannel
{
    [DataMember] public int    Index        { get; set; }   // 1~ChannelCount
    [DataMember] public string Name         { get; set; }   // "INPUT STAGE RING"
    [DataMember] public string Color        { get; set; }   // "White"
    [DataMember] public string Mode         { get; set; }   // "Continuous"/"Strobe"
    [DataMember] public int    DefaultLevel { get; set; } = 128;
    [DataMember] public bool   Active       { get; set; } = true;
}
```
- **다중 컨트롤러**: `List<LFineLightConfig>` 보관 + 인스턴스 N개 — **확인 필요 #5** (단일 vs 2 vs 3 포트).

---

## 6. 채널 매핑 (확인 필요 #5 의 다중 컨트롤러 결정에 따름)
- 기본 8채널 (io_set.json 기준): INPUT STAGE RING / BOTTOM VISION / SIDE VISION 1 / SIDE VISION 2 / BIN VISION / TOP SIDE VISION / BOTTOM SIDE VISION / ALIGN MARK ILLUM
- ⚠ 채널 6/7 라벨 "TOP/BOTTOM SIDE VISION" — Stage 63 리네임(FrontSide/RearSide) 과 라벨 정합 검토 필요 (M-66-5)
- 알고리즘/검사 ↔ 조명 채널 매핑은 별도 Stage

---

## 7. UI 결선 (확인 필요 #7)
- `IlluminatorPanel` (4채널 더미) → **채널 수 가변 + `ILightController` 라이브 호출**: 슬라이더 ValueChanged → 디바운스 → `SetPowerAsync`; On/Off 버튼 → `SetOnOffAsync`; 현재값/저장값 라벨 + Set/Save; 상태 라벨(Connected/Sim/Open실패)
- `LightControllerPage` (Handler 측) → Save 후 Reconnect 트리거 — **Handler 측이라 본 Stage 범위는 보고만** (M-66-3 연계)

---

## 8. Sim 모드
- `SimLightController`: 모든 명령 캐시만 갱신, 0ms, 로그만, `IsConnected=true`
- `Form1.Load` 또는 `Recipe.IsSimulationMode == true` 시 Factory 가 Sim 선택
- **⚠ 가짜 통과 방지**: 실장비 모드인데 Sim fallback 시 status + 알람으로 명확 표시 (카메라 테스트 정책 동일)

---

## 9. 알람 (신규 — 확인 필요 #6: 등록 경로)
| Code | Severity | Title | Cause | Action |
|---|---|---|---|---|
| `LIGHT-OPEN-FAIL` | Error | 조명 컨트롤러 시리얼 Open 실패 | COM 포트 미발견/충돌 | 케이블/PORT 확인 |
| `LIGHT-TIMEOUT` | Warning | 응답 타임아웃 | 컨트롤러 무응답 | 전원/케이블/포트 확인 |
| `LIGHT-NAK` | Warning | RERR 응답 | 잘못된 명령 형식 | 명령 포맷/펌웨어 확인 |
| `LIGHT-INVALID-RESP` | Warning | 응답 포맷 오류 | 프로토콜 불일치 | 펌웨어 버전 확인 |
| `LIGHT-TX-FAIL` | Warning | 송신 실패 | 시리얼 쓰기 예외 | 포트 상태 확인 |
| `LIGHT-PWR-RANGE` | Warning | Power 범위 초과 | MaxPower 초과 입력 | 입력값 점검 |

- 등록 위치: `QMC.Common\Alarms\AlarmMaster.cs` (Stage 62 에서 Common 으로 이동 완료 — Vision 도 `QMC.Common.Alarms.AlarmManager.Raise` 직접 호출 가능 → **확인 필요 #6 사실상 해소**, Vision 이 이미 VISION-* 코드 raise 중)

---

## 10. 영향 범위
### 신규 파일 (QMC.Vision)
- `Optics\ILightController.cs`
- `Optics\LFine\LFineLightController.cs`
- `Optics\LFine\LFineProtocol.cs` (정적 — 프레임/명령 인코딩·디코딩)
- `Optics\LFine\LFineLightConfig.cs`
- `Optics\Sim\SimLightController.cs`
- `Optics\LightControllerFactory.cs`

### 수정 파일
- `QMC.Vision\Ui\Controls\IlluminatorPanel.cs` — 채널 가변 + 라이브 적용 (확인 필요 #7)
- `QMC.Common\Alarms\AlarmMaster.cs` — 6 LIGHT-* 코드 추가
- `QMC.CDT-320\Ui\Pages\Settings\LightControllerPage.cs` — Handler 측, 본 Stage 보고만

---

## 11. 시퀀스 문서 04~08 영향
- **직접 영향 없음** — Vision 측 조명 드라이버 신설. 시퀀스 호출 방식 불변.

---

## 12. Mismatch 발견
| # | 위치 | 내용 |
|---|---|---|
| M-66-1 | `LFinePSDigitalIlluminator.cs` | `LFinePSDigitalIlluminatorCommunicator` 파일 부재 (LFine 폴더에 없음; LS/Leesos Communicator 만 존재) → PS 변종 본 Stage 범위 외 |
| M-66-2 | 매뉴얼(2 컨트롤러) vs io_set.json(COM1/2/3, 3포트) vs LFineConfig(단일 COM1) | 컨트롤러 개수 불일치 — 확인 필요 #5 |
| M-66-3 | `IlluminatorPanel.cs`(4채널 더미) vs io_set.json(8채널) | 채널 수 가변화 필요 |
| M-66-4 | LightControl 코드 = `Part/IlluminatorConfig/ListParam/SerialComm` 레거시 의존 | 신규 작성 — 직접 포팅 불가 |
| M-66-5 | io_set.json 채널 6/7 = "TOP/BOTTOM SIDE VISION" | Stage 63 FrontSide/RearSide 리네임과 라벨 정합 검토 |

---

## 13. 확인 필요 항목 — **전부 확정됨 (2026-05-29 사용자 컨펌)**
1. **PS 변종** → **무시** (범위 외 확정). LFineDigital(Strobe)만 대상.
2. **Stx/Etx** → **0x02 / 0x03 확정** (코드 확인).
3. **On/Off 명령** → **OFF = Power 0 전송**. 별도 On/Off 명령 없음. `SetOnOffAsync(ch,false)` = `SetPowerAsync(ch,0)`; `true` = 직전 저장 power 복원.
4. **응답 포맷** → **무응답 유지** (fire-and-forget). `TryParseResponse` 미구현 (`Receive` 미사용).
5. **컨트롤러 개수** → **2개**. Config = `List<LFineLightConfig>` 2개 (매뉴얼 Illuminator communicator 1·2). ⚠ io_set.json 의 COM1/2/3 8채널을 2 컨트롤러로 재매핑 필요 (구현 Stage, M-66-2).
6. **AlarmManager 직접 호출** → **가능 확정** + **Vision 로컬 알람만** (Handler 배너 전파 안 함; 전파는 별도 Stage). 별개 프로세스라 Vision 자체 AlarmManager 에만 raise.
7. **UI 결선 범위** → **클래스 골격만**. IlluminatorPanel 라이브 결선은 본 구현 Stage 범위 **외** (별도 Stage).

---

## 14. 다음 단계
SPEC + CHECKLIST 컨펌 → 구현 Stage(67) 분리. 구현 프롬프트에 **모니터 2번(보조 모니터) 검증 규칙** 반영.
예상 작업 시간: 6~9시간 (인터페이스/Sim 1.5h + Protocol+단위시험 2h + LFineController 2.5h + 알람/UI 결선 2h + 검증 1h).
