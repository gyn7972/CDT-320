# STAGE 78 — SPEC: 조명 컨트롤러 batch + Mode + Leesos 매뉴얼 정정

- **작성일**: 2026-06-02
- **단계**: SPEC (구현 전, 사용자 컨펌 대기) — **SPEC + CHECKLIST 까지만, 코드 변경 없음**
- **Stage 번호 근거**: git/docs 최신 = Stage 77 (Leesos 1차 구현) → **NN = 78**
- **브랜치**: `stage-78-light-batch-leesos-spec`
- **매뉴얼**: `LightControl\LEESOS_디지털 컨트롤러 사양서(12BIT).pdf` §5 통신 프로토콜 — **직접 확인함**

---

## 1. 목표 & 범위
1. **LFine tact-time 누락 해소**: `InspectionLightPanel.Apply` 의 채널별 SC foreach → **SP 1프레임 batch**.
2. **캐시 skip 안전화**: `LightControllerMode`(Continuous/StrobeExternal/StrobeOnCommand) 도입 — StrobeOnCommand 면 **무조건 송신**(매 호출이 발사 트리거).
3. **Leesos 매뉴얼 정정**: Stage 77 구현은 구 레퍼런스 코드(8-bit) 기준이라 **매뉴얼(12-bit)과 불일치** → 정정.
4. 인터페이스 1 프로퍼티(`Mode`) + 1 메서드(`SetChannelBatchAsync`) 추가.

**범위 외**: 런타임 자동 적용(Form1 그랩 사이클 결선), 채널별 다른 값 batch on Leesos(매뉴얼 미지원). 별도 Stage.

---

## 2. 현황 (코드 직접 확인 — Stage 77 반영분 포함)

### 2.1 이미 구현됨 (Stage 76/77)
- `LightControllerEntry.Vendor`("LFine"|"Leesos") + `[OnDeserializing]` 마이그레이션 — **존재** (`LightSystemSetup.cs:79,88-89`). ※ 사전조사의 "Vendor 필드 없음" 은 Stage 77 이후 **무효**.
- `LightControllerFactory.Create(LightControllerEntry,bool)` 벤더 switch + `ToLFineConfig`/`ToLeesosConfig` — **존재**. ※ "LFine 만 분기" 무효.
- `LightHub.Initialize` entry 기반 Create — **존재**.
- `LeesosLightController`/`LeesosProtocol`/`LeesosLightConfig` — **존재**(Stage 77). ※ "신규" 가 아니라 **정정 대상**.
- `LightSystemSetupPage` Vendor 콤보 컬럼 — **존재**(index6 + DisplayIndex1).
- `LFineProtocol.PageOnTimeFrame(int page, int[] times)` — **존재**(`LFineProtocol.cs:74`). batch 송신에 그대로 사용 가능.
- `ILightController.ReceiveResponseAsync` — **존재**(Stage 75).

### 2.2 부족분 (이번 Stage 추가)
- `LightControllerMode` enum — **없음**.
- `LightControllerEntry.Mode` 필드 + 마이그레이션 — **없음**.
- `ILightController.Mode` 프로퍼티 / `SetChannelBatchAsync` — **없음** (`ILightController.cs` 현재 9 메서드 + 3 속성).
- LFine/Sim/Leesos 의 `Mode`/`SetChannelBatchAsync` 구현 — **없음**.
- `LightSystemSetupPage` Mode 컬럼 — **없음**.
- `InspectionLightPanel.Apply`(`:215` async) 의 채널별 foreach(`:237-246`) → batch 미적용.

### 2.3 현재 working-tree WIP (미커밋 — 이 SPEC 이 정식화)
- `LFineProtocol.BuildChannelOnTimeCommand`: SC 채널 필드를 **0-based("00"~"15")** 로 (`channel-1`) 변경 중. (실장비 채널 인덱싱 정정으로 추정 — 구현 Stage 에서 확정.)
- `LeesosProtocol.BuildVolumeCommand`: **`{X3}` + `& 0xFFF`(12-bit)** 로 변경 중.

---

## 3. Leesos 매뉴얼 §5.3 확정 프로토콜 (PDF 직접 인용)

UART: **9600 / 8bit / 1 stop / no parity**, 모든 포맷 ASCII + `CR LF`(0x0D 0x0A).

### 3.1 채널 개별 밝기 (§5.3.1)
- 송신 `LC{n1}{n2}{n3}{n4}` + CRLF
  - `L`=12BIT 모델 prefix, `C`=command
  - `n1` = 채널: **`1`~`9`, `A`~`G`(10~16), `T`=전체**
  - `n2~n4` = DATA **`000`~`FFF`** (12-bit, 0~4095)
  - 예) `LC1FFF` (ch1), `LCTFFF` (전체)
- 응답 `R{n1}{n2}{n3}{n4}` + CRLF — 예) `R1FFF`, `RTFFF`

### 3.2 채널 ON/OFF (§5.3.2)
- 송신 `LH{n1}{ON|OF}` + CRLF — 예) `LH1ON`, `LH1OF`, 전체 `LHTON`/`LHTOF`
- 응답 `R{n1}{OK|ER}` + CRLF — 예) **`R1OK`**(성공), **`R1ER`**(에러)

### 3.3 상태 확인 (§5.3.3)
- 송신 `LS{n1}{00|01|02}` + CRLF (`00`=밝기, `01`=On/Off, `02`=에러)
- 응답 `R{n1}{...}` + CRLF
  - 밝기: `R1000`~`R1FFF`
  - On/Off: `R1ON` / `R1OF`
  - 에러: `R1OK` / `R1ER`

> **Stage 77 대비 핵심 정정**: ① Volume 8-bit(X2)→**12-bit(X3, 0~4095)**, ② 에러 응답 `"ERR"`→**`"ER"`**(2자), ③ 채널 인코딩 raw int→**1~9/A~G/T**, ④ MaxPower 255→**4095**, ChannelCount 8→**4**(LPD-6524-4CH 기본).

---

## 4. 데이터 모델

### 4.1 `LightControllerMode` enum (신규, `QMC.Common\Recipes\LightControllerMode.cs`)
```csharp
public enum LightControllerMode
{
    Continuous       = 0,   // PWM dimming — 명령 후 값 유지. 캐시 skip 안전.
    StrobeExternal   = 1,   // 시간/밝기 설정 + 외부 HW 트리거 발사. 캐시 skip 안전.
    StrobeOnCommand  = 2,   // 명령 송신 자체가 발사. 캐시 skip 절대 금지.
}
```

### 4.2 `LightControllerEntry.Mode` (추가)
```csharp
[DataMember(EmitDefaultValue = false)]
public LightControllerMode Mode { get; set; } = LightControllerMode.StrobeOnCommand;
```
- **마이그레이션**: 기존 `[OnDeserializing]`(Vendor="LFine") 에 `Mode = StrobeOnCommand` 추가 — 구버전 JSON 에 키 없으면 가장 안전한 값(skip 안 함 = 가짜 통과 방지). Clone 에 Mode 복제.

---

## 5. 인터페이스 변경 — `ILightController`
```csharp
/// <summary>Stage 78 — Mode-aware 캐시 정책 노출.</summary>
LightControllerMode Mode { get; }

/// <summary>한 페이지/배치의 채널 값 일괄 적용.
///   Continuous/StrobeExternal: 직전과 동일하면 skip(no-op+true).
///   StrobeOnCommand: 무조건 송신(매 호출이 트리거).
///   LFine=SP 1프레임 / Leesos=전체동일값 LCT 1프레임, 그 외 LC loop(변경분만).</summary>
Task<bool> SetChannelBatchAsync(int page, int[] valuesPerChannel);   // 길이 == ChannelCount
```
- 구현체 3종(LFine/Sim/Leesos) 모두 추가.

---

## 6. LFineLightController
- `Mode => _cfg.Mode` (Config 에 Mode 추가 — `LFineLightConfig` 또는 어댑터 `ToLFineConfig` 가 entry.Mode 전달).
- `SetChannelBatchAsync(page, times)`:
  - 길이 검증(== ChannelCount).
  - Mode != StrobeOnCommand 이면 `_lastPageTimes[page]` 와 비교, 같으면 skip(true).
  - `SendFrame(LFineProtocol.PageOnTimeFrame(page, times))` — **1프레임**. 성공 시 `_lastPageTimes[page]=clone`, `_power[]`/`_lastOnPower[]` 갱신.
- 현재 `SetPowerAsync`/`SetStrobeTimeAsync`(`:67,82`)는 **이미 캐시 비교 없이 매번 송신** → StrobeOnCommand 안전. (skip 로직 없음 — 유지.)
- ※ working-tree WIP(SC 채널 0-based)와 정합: PageOnTimeFrame 채널 순서/인덱싱은 구현 Stage 에서 실장비 기준 재확인(확인 필요 추가).

---

## 7. LeesosLightController 정정 + batch

### 7.1 LeesosProtocol (정정/확장)
```csharp
public static string EncodeChannel(int ch)   // 1~9 → "1".."9", 10~16 → "A".."G"
public const string ChannelAll = "T";
public static string BuildVolumeCommand(int ch, int v)   => $"LC{EncodeChannel(ch)}{v:X3}";   // 0~4095
public static string BuildVolumeAllCommand(int v)        => $"LC{ChannelAll}{v:X3}";           // "LCTFFF"
public static string BuildOnOffCommand(int ch, bool on)  => $"LH{EncodeChannel(ch)}{(on?"ON":"OF")}";
public static string BuildStatusCommand(int ch, LeesosStatusType t) => $"LS{EncodeChannel(ch)}{(int)t:D2}";
public static bool ValidateEcho(string r, int ch, int v) => r == $"R{EncodeChannel(ch)}{v:X3}";
public static bool ValidateAllEcho(string r, int v)      => r == $"RT{v:X3}";
public static bool IsErrorResponse(string r)             => r != null && r.EndsWith("ER");   // R{n1}ER  ※ "ERR" 아님
public enum LeesosStatusType { Brightness=0, OnOff=1, Error=2 }
```
- WrapFrame/UnwrapFrame(CRLF) 유지. **기존 `Classify`("ERR")/`ParseStatusOn`(Substring) 은 매뉴얼 기준으로 정정.**

### 7.2 LeesosLightController
- `Mode => LightControllerMode.Continuous` (강제 — PWM only).
- `SetPowerAsync(ch,power)`: 0~4095 클램프, `_power[ch]==power` 면 skip(Continuous 안전), `LC` 송신 + `ValidateEcho` 검증.
- `SetOnOffAsync(ch,on)`: `LH` 송신 + `IsErrorResponse`(R{ch}ER)면 LIGHT-NAK.
- `SetStrobeTimeAsync`/`SwitchPageAsync`: no-op + true.
- `SetChannelBatchAsync(page, values)`:
  - 전체 동일값 → `LCT` 1프레임 + `ValidateAllEcho`.
  - 그 외 → 변경된 채널만 `LC` loop(캐시 skip).
- `CheckPowerOnAsync(ch)`: `LS{ch}01` → 응답 `R{ch}ON`/`R{ch}OF` 판정.
- `_ioGate` 송수신 직렬화 유지.

### 7.3 LeesosLightConfig 정정
- `MaxPower` 255 → **4095**, `ChannelCount` 8 → **4**(LPD-6524-4CH 기본; 2CH 모델은 사용자 변경).

---

## 8. SimLightController
- `Mode { get; }` 추가 — entry 가 준 모드를 그대로 흉내(테스트 시 Strobe 시뮬). 생성자에 mode 전달(Factory 가 entry.Mode 주입).
- `SetChannelBatchAsync` 추가 — 캐시 갱신 + (Sim 카운터로 송신/skip 검증 가능).

---

## 9. Factory + Hub
- 이미 entry 기반 분기 존재(Stage 77). **추가**: `SimLightController` 생성 시 `entry.Mode` 전달, 어댑터가 `entry.Mode` 를 벤더 Config 로 전달(LFine). Leesos 는 Mode 강제 Continuous.
- Unknown Vendor → `LIGHT-MAP-INVALID` + Sim (기존 유지).

## 10. SetupUI — `LightSystemSetupPage`
- Vendor 컬럼 — **이미 있음**(Stage 77).
- **Mode 컬럼 추가**(DataGridViewComboBox, Items=["Continuous","StrobeExternal","StrobeOnCommand"], 끝에 추가 + DisplayIndex 로 Vendor 옆).
- Vendor=Leesos 선택 시: Mode 셀 = `Continuous` + readonly, PageCount=1(이미 Stage 77 에서 PageCount=1 보정).
- 신규 추가 시 기본 Vendor="LFine", Mode="StrobeOnCommand".
- BindAllCore/CollectFromUi 가 Mode 왕복(인덱스 보존 위해 컬렉션 끝에 추가).
- Mode 변경 시 안내 라벨: "Strobe On Command 는 캐시 skip 안 함 — 매 호출이 발사".

## 11. InspectionLightPanel.Apply (`:234-246` 정정)
```csharp
foreach (var grp in ov.Settings.GroupBy(s => s.Page).OrderBy(g => g.Key))
{
    await ctrl.SwitchPageAsync(grp.Key);
    int[] times = new int[ctrl.ChannelCount];          // 0 = OFF
    foreach (var s in grp) {
        if (!w.Channels.Contains(s.Channel)) { Raise(LIGHT-CHANNEL-OUT-OF-POOL); continue; }
        if (s.Channel >= 1 && s.Channel <= ctrl.ChannelCount) times[s.Channel-1] = s.Level;
    }
    await ctrl.SetChannelBatchAsync(grp.Key, times);   // ← SC×N foreach → batch 1회
    applied += grp.Count(s => s.Level > 0);
}
```
On/Off 는 Level=0 → OFF(times 0)로 일원화하거나, 별도 SetOnOff 유지(확인 필요 #5).

## 12. 알람 (재사용 — 신규 0)
`LIGHT-OPEN-FAIL`/`LIGHT-TIMEOUT`/`LIGHT-NAK`/`LIGHT-INVALID-RESP`/`LIGHT-TX-FAIL`/`LIGHT-PWR-RANGE`/`LIGHT-MAP-INVALID` 모두 AlarmMaster 등록됨(개수 60 불변).

## 13. 영향 범위
- 신규: `LightControllerMode.cs`
- 수정: `LightSystemSetup.cs`(Mode), `ILightController.cs`(Mode+batch), `LFineLightController.cs`(+Config/adapter), `SimLightController.cs`, `LeesosProtocol.cs`(12-bit/A~G/T/ER), `LeesosLightController.cs`(batch/정정), `LeesosLightConfig.cs`(4095/4), `LightControllerFactory.cs`(Mode 전달), `LightHub.cs`(필요 시), `LightSystemSetupPage.cs`(Mode 컬럼), `InspectionLightPanel.cs`(batch)

## 14. 시퀀스 문서 04~08 영향
- 직접 영향 없음 (Vision 측 드라이버/UI).

## 15. Mismatch
| # | 위치 | 내용 |
|---|---|---|
| M-78-1 | LFine SetPowerAsync 캐시 | LFine 은 현재 매번 송신(skip 없음) → StrobeOnCommand 이미 안전. batch 에만 Mode-gated skip 도입 |
| M-78-2 | Stage 77 Leesos(8-bit X2, "ERR", raw ch) | 매뉴얼 = **12-bit X3, "ER", 1~9/A~G/T** → 정정 (M-77 후속) |
| M-78-3 | LeesosLightConfig 255/8CH | 매뉴얼 = 4095 / 4CH(LPD-6524-4CH) |
| M-78-4 | InspectionLightPanel.Apply SC foreach(:237-246) | SP/배치 1프레임으로 일원화(tact-time) |
| M-78-5 | 사전조사(프롬프트) "Vendor 없음/Factory LFine만/Leesos 신규/11메서드" | Stage 77 이후 무효 — 본 SPEC 에서 현행 기준 정정 |
| M-78-6 | working-tree WIP (LFine SC 0-based, Leesos X3) | 미커밋 — 본 SPEC 이 정식화, 구현 Stage 에서 실장비로 채널 인덱싱 확정 |

## 16. 확인 필요 항목 (구현 전 컨펌)
1. **Leesos 응답 timeout 1000ms** — 매뉴얼 명시 없음. 1000ms OK?
2. **LFine SC 채널 0-based("00"~"15")** — working-tree WIP. 실장비 채널 인덱싱이 0-base 맞나? (PageOnTimeFrame 채널 순서도 동일 기준?)
3. **Leesos ChannelCount 기본 4** (LPD-6524-4CH) OK? 2CH 모델은 SetupUI 조정.
4. **Mode 기본값 = StrobeOnCommand** (안전치) 확정?
5. **Apply On/Off 처리** — Level=0=OFF 로 batch 일원화 vs SetOnOff 병행? `applied` 카운트 정책?

## 17. 다음 단계
SPEC + CHECKLIST 컨펌 → 구현 Stage. 순서: 인터페이스(Mode+batch) → 데이터모델(Mode+마이그레이션) → LFine batch → Leesos 매뉴얼 정정 → Sim → UI Mode 컬럼 → Apply batch → verify. 이후 실장비 hex dump + 런타임 자동 적용 결선.
