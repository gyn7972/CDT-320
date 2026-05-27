# CDT-320 피더 유닛 시퀀스 상세 분석

**문서 버전**: 1.0
**작성일**: 2026-04-29
**대상 코드 베이스**: `QMC.CDT-320` (Stage 58 기준)
**관련 모듈**:
- `Equipment/InputLoaderUnit.cs` — Input(Wafer) Feeder
- `Equipment/OutputUnloaderUnit.cs` — Output(Bin) Feeder
- `Equipment/Sim/WaferLoaderAdapter.cs` — 피더 ↔ InputStage 안전 게이트
- `Equipment/MachineController.cs` — 통합 시퀀스 호출자

---

## 1. 개요 — 피더란 무엇인가

CDT-320은 **2개의 피더(Feeder)** 를 가진다:

| 구분 | 클래스 | 역할 | 카세트 |
|---|---|---|---|
| **Input Feeder** | `InputLoaderUnit` | 카세트 → InputStage | 1개 (Wafer) |
| **Output Feeder** | `OutputUnloaderUnit` | OutputStage → 카세트 | 3개 (NG / Good1 / Good2) |

피더는 **카세트 ↔ 작업 스테이지** 사이의 **물리적 인계 메커니즘** 이다. 카세트에 보관된 웨이퍼/완료품을 슬롯 단위로 꺼내거나 적재하는 일을 담당한다. 시퀀스의 가장 첫 단계이자 마지막 단계이며, **잘못 동작하면 웨이퍼 파손**으로 직결되는 가장 위험한 영역이기도 하다.

```
 ┌───────────┐    Input Feeder     ┌───────────┐    TransferPicker    ┌───────────┐    Output Feeder    ┌──────────────┐
 │  Wafer    │ ─────────────────► │ Input     │ ────────────────────► │ Output    │ ──────────────────► │ NG / Good1 / │
 │  Cassette │   ScanCassette      │ Stage     │   PickAndPlace        │ Stage     │  StoreCompleted    │ Good2 카세트 │
 └───────────┘   LoadNext          └───────────┘                       └───────────┘                     └──────────────┘
                 RetractFeeder
```

---

## 2. Input Feeder (InputLoaderUnit) 시퀀스

### 2.1 하드웨어 구성

| 종류 | 컴포넌트 | 역할 |
|---|---|---|
| Axis | `ElevatorZ` | 카세트 상하 이동 (목표 슬롯을 피더 높이에 정렬) |
| Axis | `FeederY` | 웨이퍼를 카세트 → InputStage 로 전후진 이송 |
| DI | `CassetteExistSensor` | 로드포트에 카세트 안착 여부 |
| DI | `ProtrusionSensor` | **돌출 감지** — 웨이퍼가 카세트 밖으로 튀어나옴 (핵심 인터락) |
| DI | `WaferDetectSensor` | 매핑 시 슬롯에 웨이퍼 유무 |
| DI | `WaferClampedSensor` | 피더가 웨이퍼를 확실히 파지했는지 |
| Cyl | `FeederUpDownCyl` | 피더 상/하강 (웨이퍼 높이 정렬) |
| Cyl | `FeederClampCyl` | 피더 클램프 — 웨이퍼 파지/해제 |

### 2.2 4단계 메서드 매핑

```
ScanCassetteAsync ──► MoveToTargetSlotAsync ──► MoveToExchangePositionAsync ──► RetractFeederAsync
   (매핑 1회)            (다이마다)                (다이마다)                       (다이마다)
```

### 2.3 단계별 상세 시퀀스

#### 2.3.1 `ScanCassetteAsync(maxSlots, slotPitch)` — 카세트 매핑

```
┌─ Interlock ─────────────────────────────────┐
│  CassetteExistSensor.IsOn  →  필수 (false ⇒ 즉시 false return) │
└─────────────────────────────────────────────┘
                    │
                    ▼
        ElevatorZ.MoveAbsolute(FirstSlotPosition)   // 10mm
                    │
                    ▼
       ┌────[ for i in 0..maxSlots-1 ]────┐
       │                                  │
       │   if i > 0:                      │
       │     ElevatorZ.MoveRelative(pitch)│   // 6mm 씩 상승
       │                                  │
       │   await Delay(SettleTimeMs)      │   // 100ms 안정화
       │                                  │
       │   map[i] = WaferDetectSensor.IsOn│
       │                                  │
       └──────────────────────────────────┘
                    │
                    ▼
           WaferMap = map.AsReadOnly()
                    │
                    ▼
              return true
```

**파라미터** (실 호출): `maxSlots = 16`, `slotPitch = 6.0 mm`
**소요시간**: 16 × (이동 + 100ms 안정화) ≈ 4–10 초
**알람 케이스**:
- `LOT-NOCASS` — 카세트 미감지
- `LOT-SCAN` — `ElevatorZ.IsAlarm` (이동 실패)

#### 2.3.2 `MoveToTargetSlotAsync(targetPosition)` — 목표 슬롯 정렬

이 메서드는 **다이 1개 처리마다** 호출되어 다음 웨이퍼가 있는 슬롯 높이로 엘리베이터를 이동시킨다.

```
┌─ 이동 전 인터락 ──────────────────────────┐
│ if ProtrusionSensor.IsOn:                  │
│   ElevatorZ.EStop()                        │
│   throw InvalidOperationException          │
└────────────────────────────────────────────┘
                    │
                    ▼
       ┌─────── Task.WhenAny ───────┐
       │                            │
   [moveTask]                  [watchTask]
   ElevatorZ.MoveAbsolute    10ms 폴링 루프:
   (targetPosition)           if Protrusion.IsOn → return true
       │                            │
       └────────┬───────────────────┘
                │
       ┌────────▼────────┐
       │ 어느 쪽이 먼저  │
       │ 끝났는가?       │
       └─┬────────┬──────┘
         │        │
   moveTask 우선  watchTask 우선
         │        │
         ▼        ▼
   cts.Cancel    ElevatorZ.EStop()
   감시 정리      cts.Cancel
                 throw InvalidOperationException
                    │
                    ▼
       if ElevatorZ.IsAlarm: throw
                    │
                    ▼
                정상 완료
```

**핵심 안전 패턴**: `Task.WhenAny(moveTask, watchTask)` 로 **이동 + 감시 병렬화**.
돌출 감지가 먼저 끝나면 → **즉시 E-Stop + 예외**.
이동이 먼저 끝나면 → 감시 태스크 취소.

#### 2.3.3 `MoveToExchangePositionAsync()` — 웨이퍼 InputStage 입구로 이송

```
Step 1. FeederUpDownCyl.MoveFwd()       // 피더 하강 — 웨이퍼 높이 정렬
            │
            └─ 타임아웃 ⇒ 알람 + return false
            │
            ▼
Step 2. FeederClampCyl.MoveFwd()        // 클램프 전진 — 파지
            │
            └─ 타임아웃 ⇒ 알람 + return false
            │
            ▼
Step 3. WaferClampedSensor.WaitUntilState(true, 1000ms)
            │
            ├── true 이면 다음으로
            │
            └── false 이면 (파지 실패):
                  ↳ FeederClampCyl.MoveBwd()    (안전 해제)
                  ↳ FeederUpDownCyl.MoveBwd()   (피더 상승)
                  ↳ return false
            ▼
Step 4. FeederY.MoveAbsolute(ExchangePositionY=150mm)
            │
            └─ FeederY.IsAlarm ⇒ return false
            │
            ▼
       return true  (웨이퍼가 InputStage 입구에 대기)
```

**중요**: Step 3 의 클램프 확인 실패 시 **자동으로 안전 해제** 후 종료. 웨이퍼가 떨어지지 않은 상태로 안전 위치로 복귀.

#### 2.3.4 `RetractFeederAsync()` — 피더 원점 복귀

InputStage 가 웨이퍼를 받은 직후, 피더는 웨이퍼를 놓고 후퇴한다.

```
Step 1. FeederClampCyl.MoveBwd()         // 클램프 해제
            │
Step 2. WaferClampedSensor.WaitUntilState(false, 1000ms)
            │  센서 OFF 확인 — 클램프가 진짜로 해제됐는지
            ▼
Step 3. FeederY.MoveAbsolute(0)          // 원점 복귀 (피더 ≤30mm)
            │
            ▼
Step 4. FeederUpDownCyl.MoveBwd()        // 피더 상승
            │
            ▼
       return true
```

이 시퀀스가 끝나야 InputStage 의 **ExpanderZ Down + 다이 픽업** 이 안전하게 시작될 수 있다.

---

## 3. Output Feeder (OutputUnloaderUnit) 시퀀스

### 3.1 핵심 차이

Input Feeder 와 거의 같은 구조이지만 **3개 카세트** 를 다룬다:

| 카세트 | 용도 | 슬롯 위치 (Z) | 교환 Y |
|---|---|---|---|
| `Ng` | 불량품 적재 | `NgFirstSlotPositionZ` | `NgStageExchangePositionY` |
| `Good1` | 양품 적재 #1 | `Good1FirstSlotPositionZ` | `GoodStageExchangePositionY` |
| `Good2` | 양품 적재 #2 (Good1 만석 시) | `Good2FirstSlotPositionZ` | (Good 과 동일) |

### 3.2 추가된 안전 인터락

`MoveElevatorWithProtrusionGuardAsync()` — Input 의 `MoveToTargetSlotAsync` 와 동일한 패턴이지만 **velocity 파라미터** 까지 받는다. `Task.WhenAny` 경쟁 + 돌출 감지 즉시 E-Stop 구조 동일.

### 3.3 슬롯 점유 관리

`OutputUnloaderUnit` 는 카세트별 슬롯 점유 맵을 자체 보관:
```csharp
private readonly Dictionary<TargetCassette, bool[]> _slotMap;
// Ng    → bool[N]
// Good1 → bool[N]
// Good2 → bool[N]
```
적재 시 빈 슬롯을 검색하여 ElevatorZ 위치 계산:
```
GetSlotPositionZ(cassette, slotIndex) = baseZ[cassette] + slotIndex × Setup.SlotPitchZ
```

### 3.4 Soft Limit 확장 (Stage 27)

기본값 `SoftLimitPlus = 200mm` 이지만 OutputUnloader 는 더 멀리 가야 한다:
```csharp
FeederY  .Setup.SoftLimitPlus = 350.0;   // CassetteInsertPositionY = 250mm 까지
ElevatorZ.Setup.SoftLimitPlus = 400.0;   // Good2 마지막 슬롯 = 160 + 24×6 = 304mm 까지
```

### 3.5 내부 헬퍼 메서드: Pickup / Place (Mismatch 보완 2026-04-30)

`StoreFullWaferAsync` (`OutputUnloaderUnit.cs:737`) 와 `SupplyEmptyWaferAsync` (`OutputUnloaderUnit.cs:819`) 는 동일한 두 헬퍼로 분해되어 있다:

#### 3.5.1 `PickupWaferAtPositionAsync(double targetPositionY)` — `OutputUnloaderUnit.cs:470`

`private async Task<bool>` — 지정 FeederY 위치로 전진하여 웨이퍼를 파지한다.

| Step | 동작 | 코드 라인 | 실패 처리 |
|---|---|---|---|
| 1 | `FeederY.MoveAbsoluteAsync(targetPositionY, Recipe.FeederVelocity)` | 477 | `FeederY.IsAlarm` ⇒ false |
| 2 | `FeederUpDownCyl.MoveFwdAsync()` (피더 하강) | 486 | downOk=false ⇒ false |
| 3 | `FeederClampCyl.MoveFwdAsync()` (클램프 체결) | 495 | clampOk=false ⇒ `FeederUpDownCyl.MoveBwd()` (안전 복귀) ⇒ false |
| 4 | `WaferClampedSensor.WaitUntilStateAsync(true, Recipe.ClampSensorTimeoutMs)` | 504 | clamped=false ⇒ `Clamp.MoveBwd` + `UpDown.MoveBwd` ⇒ false |
| 5 | `FeederUpDownCyl.MoveBwdAsync()` (피더 상승) | 518 | upOk=false ⇒ false |

#### 3.5.2 `PlaceWaferAtPositionAsync(double targetPositionY)` — `OutputUnloaderUnit.cs:547`

`private async Task<bool>` — 지정 FeederY 위치로 전진하여 웨이퍼를 내려놓는다.

| Step | 동작 | 코드 라인 | 실패 처리 |
|---|---|---|---|
| 1 | `FeederY.MoveAbsoluteAsync(targetPositionY, Recipe.FeederVelocity)` | 554 | `FeederY.IsAlarm` ⇒ false |
| 2 | `FeederUpDownCyl.MoveFwdAsync()` (피더 하강) | 563 | downOk=false ⇒ false |
| 3 | `FeederClampCyl.MoveBwdAsync()` (언클램프) | 572 | unclampOk=false ⇒ `FeederUpDownCyl.MoveBwd()` (안전 복귀) ⇒ false |
| 4 | `WaferClampedSensor.WaitUntilStateAsync(false, Recipe.ClampSensorTimeoutMs)` | 581 | released=false ⇒ `UpDown.MoveBwd` ⇒ false |
| 5 | `FeederUpDownCyl.MoveBwdAsync()` (피더 상승) | 594 | upOk=false ⇒ false |
| 6 | `FeederY.MoveAbsoluteAsync(Setup.FeederHomePositionY, Recipe.FeederVelocity)` | 603 | `FeederY.IsAlarm` ⇒ false |

> Pickup 과 Place 의 핵심 차이는 Step 3 (`MoveFwd` vs `MoveBwd`) / Step 4 (센서 ON 대기 vs OFF 대기) / Place 의 추가 Step 6 (홈 후진).

### 3.6 공개 시퀀스 메서드 (Mismatch 보완 2026-04-30)

#### 3.6.1 `ScanAllCassettesAsync()` — `OutputUnloaderUnit.cs:630`

`public async Task<bool>` — 3개 카세트(Ng, Good1, Good2) 를 순차적으로 스캔하여 `_slotMap` 갱신.
- 카세트 미안착 시 해당 카세트만 스킵 (`_slotMap[cassette] = new bool[0]`)
- `MoveElevatorWithProtrusionGuardAsync(slotZ, Recipe.ScanVelocity)` 로 슬롯별 이동
- 슬롯당 `Config.ScanSettleTimeMs` 대기 후 `WaferDetectSensor.IsOn` 기록
- **호출 시점**: `MachineController.ScanOutputCassettesAsync` (`MachineController.cs:432`) → `MachineController.cs:726` 의 초기화 자동 매핑 1회

#### 3.6.2 `StoreFullWaferAsync(TargetCassette target, int slotIndex)` — `OutputUnloaderUnit.cs:737`

`public async Task<bool>` — OutputStage → 카세트 슬롯으로 완성 웨이퍼 회수.
- Step 1: `IsCassettePresent(target)` 확인
- Step 2-1: `PickupWaferAtPositionAsync(GetStageExchangePositionY(target))` (라인 756)
- Step 2-2: `MoveElevatorWithProtrusionGuardAsync(slotZ, Recipe.ElevatorVelocity)` (라인 772)
- Step 2-3: `PlaceWaferAtPositionAsync(Setup.CassetteInsertPositionY)` (라인 782)
- 성공 시 `_slotMap[target][slotIndex] = true`
- **호출 시점**: `MachineController.StoreCompletedWaferAsync` (`MachineController.cs:388`)

#### 3.6.3 `SupplyEmptyWaferAsync(TargetCassette source, int slotIndex)` — `OutputUnloaderUnit.cs:819`

`public async Task<bool>` — 카세트 슬롯 → OutputStage 로 빈 웨이퍼 공급 (Store 의 역순).
- Step 1: `IsCassettePresent(source)` 확인
- Step 3-1: `MoveElevatorWithProtrusionGuardAsync(slotZ, Recipe.ElevatorVelocity)` (라인 843)
- Step 3-2: `PickupWaferAtPositionAsync(Setup.CassetteInsertPositionY)` (라인 853)
- Step 3-3: `PlaceWaferAtPositionAsync(GetStageExchangePositionY(source))` (라인 868)
- 성공 시 `_slotMap[source][slotIndex] = false`
- **호출 시점**: 현재 `MachineController` 에서는 호출처 없음. UI 수동 버튼 (`Ui/Pages/WorkInfo/OutputPages.cs:115` — `btnPick.Click`) 에서만 직접 호출. (`OutputUnloaderUnit.ExchangeWaferSequenceAsync` 내부 Step 2 에서도 호출되지만, 해당 시퀀스 자체가 외부에서 호출되지 않음 — 아래 3.6.4 참고)

#### 3.6.4 `ExchangeWaferSequenceAsync(storeTarget, storeSlotIndex, supplySource, supplySlotIndex)` — `OutputUnloaderUnit.cs:907`

`public async Task<bool>` — Bin 가득 시 통합 교체 시퀀스 (Store → Supply).
- Step 4-1: `StoreFullWaferAsync(storeTarget, storeSlotIndex)` (라인 920) — 실패 시 즉시 중지 (Supply 진행 금지: 다이 손실 방지)
- Step 4-2: `SupplyEmptyWaferAsync(supplySource, supplySlotIndex)` (라인 939)
- **호출 시점**: 현재 코드 베이스 내 직접 호출처 없음. `OutputUnloaderAdapter` (`Equipment/Sim/OutputUnloaderAdapter.cs`) 의 주석에는 호출 의도가 명시되어 있으나, 실제 `RequestWaferChangeAsync` 구현은 `StoreFullWaferAsync` 만 호출하도록 단순화되어 있음 (`OutputUnloaderAdapter.cs:46`). 즉 `ExchangeWaferSequenceAsync` 는 **현재 dead-code 에 가까움**.

### 3.7 Good1 → Good2 자동 전환 (책임 분리) (Mismatch 보완 2026-04-30)

`OutputUnloaderUnit` 자체에는 자동 전환 로직이 **없다**. `StoreFullWaferAsync(target, slotIndex)` 는 호출자가 지정한 카세트/슬롯에 그대로 적재할 뿐이다.

**자동 전환 결정은 `MachineController.StoreCompletedWaferAsync(bool isGood)` 에서 수행** — `MachineController.cs:352` ~ `MachineController.cs:424`:

```csharp
if (isGood)
{
    if (OutputSlotGood1 < 25) { target = TargetCassette.Good1; slot = OutputSlotGood1++; }
    else if (OutputSlotGood2 < 25) { target = TargetCassette.Good2; slot = OutputSlotGood2++; }
    else
    {
        // Stage 27 fix — 카세트 가득 = 사이클 자동 정지
        AlarmManager.Raise(AlarmSeverity.Error, "OUT-FULL-GOOD",
            unloader.Name, "Good 카세트 모두 가득 — 사이클 자동 정지.");
        Log("[FEEDER] Good cassette full → CycleStop");
        _cycleCts?.Cancel();
        return false;
    }
}
else
{
    if (OutputSlotNg < 25) { target = TargetCassette.Ng; slot = OutputSlotNg++; }
    else
    {
        AlarmManager.Raise(AlarmSeverity.Error, "OUT-FULL-NG",
            unloader.Name, "NG 카세트 가득 — 사이클 자동 정지.");
        Log("[FEEDER] NG cassette full → CycleStop");
        _cycleCts?.Cancel();
        return false;
    }
}
```

**규칙**:
- `OutputSlotGood1 < 25` → Good1 적재, `OutputSlotGood1++`
- `OutputSlotGood1 >= 25 && OutputSlotGood2 < 25` → Good2 적재, `OutputSlotGood2++`
- 둘 다 가득 (≥ 25) → `OUT-FULL-GOOD` 알람 + `_cycleCts?.Cancel()` (사이클 자동 정지)
- NG 도 동일 패턴: `OUT-FULL-NG` + `_cycleCts?.Cancel()`

**호출 시점**: `MachineController.cs:1272` — `await StoreCompletedWaferAsync(inspPass);` (다이 검사 완료 시점)

> **책임 분리 요지**: `OutputUnloaderUnit` 은 "어디에 적재할지"를 모르고, 단순한 픽-앤-플레이스 시퀀스만 수행. 카세트 선택·슬롯 카운터·만석 정책은 모두 상위 컨트롤러(`MachineController`) 가 보유. 이로 인해 카세트 만석 정책 변경(예: Good3 추가, NG 만석 시 Stop 대신 알람만) 은 `MachineController` 만 수정하면 된다.

---

## 4. 피더 ↔ InputStage 안전 게이트 (`WaferLoaderAdapter`)

### 4.1 왜 필요한가

피더가 이동하는 도중에 InputStage의 **ExpanderZ가 하강** 하면 피더와 충돌한다.
반대로 InputStage가 다이를 픽업하는 동안 피더가 끼어들면 정렬이 깨진다.

### 4.2 안전 위치 정의

`InputStage.LoadAndPrepareWaferAsync()` 의 첫 단계에서 호출:
```csharp
if (!Loader.IsFeederAtSafePosition) return false;
```

`WaferLoaderAdapter.IsFeederAtSafePosition` 의 판정 로직:
```
       FeederY.ActualPosition
          ▲
   400mm  │
          │       ┌──── ✅ 안전 (≥ DangerRangeMax = 140mm)
   150mm  │ ──────┘     "교환 위치 — 인계 직전/직후"
          │
          │       ╳ 위험 (30mm < pos < 140mm)
          │       "이동 중, 위치 부정확"
          │
    30mm  │ ──────┐
          │       └──── ✅ 안전 (≤ DangerRangeMin = 30mm)
     0mm  │              "홈 근처 — InputStage 단독 작업 OK"
          ▼
```

**3가지 안전 상태**:
1. **홈 근처** (≤30mm) — 피더 후퇴 완료, InputStage 가 단독으로 ExpanderZ 하강 / 다이 픽업 가능
2. **교환 위치 또는 그 이상** (≥140mm) — 웨이퍼 인계 직전/직후, InputStage 가 받을 준비 OK
3. 그 외 (30 < pos < 140) — **이동 중 또는 부정확 위치** ⇒ ExpanderZ 하강 금지

### 4.3 NullWaferLoader 와의 차이

`Equipment/CDT320Machine.cs` 에 `NullWaferLoader.IsFeederAtSafePosition => true` 가 빌드용으로 존재.
이전 버전은 이 Null Object 가 항상 `true` 를 반환해서 인터락이 무력화되어 있었음. **Stage 28** 에서 `WaferLoaderAdapter` 로 교체하여 실 `FeederY.ActualPosition` 을 보고 판정하게 됨.

---

## 5. 통합 흐름: `MachineController.LoadNextWaferAsync()`

피더 시퀀스를 사용하는 **유일한 진입점** 은 `MachineController.LoadNextWaferAsync` 이다. `MachineController.CycleRunAsync(N)` 는 **N개 다이마다 한 번 호출** 하지 않고, **사이클 시작 시 1회** 만 호출한다 (1 카세트 = 1 회 매핑 + 1 회 인계).

### 5.1 전체 흐름도

```
                  CycleRunAsync(N) 진입
                          │
                          ▼
                ┌─────────────────────┐
                │ LoadNextWaferAsync()│  ← 사이클 시작 시 1회
                └─────────┬───────────┘
                          │
                          ▼
        ┌─── CassetteExistSensor.IsOn ? ─── False ──► AlarmManager.Raise(LOT-NOCASS)
        │                                              return false
        ▼ True
        │
        ├─── WaferMap.Count == 0 ? ─── True ──► loader.ScanCassetteAsync(16, 6.0)
        │                                        실패 ⇒ AlarmManager.Raise(LOT-SCAN)
        ▼ False
        │
        ▼
  next = (CurrentInputSlot+1 부터 WaferMap[s]==true 인 첫 슬롯)
        │
        │ 발견 못함 ⇒ "No more wafer" return false
        ▼
  targetZ = FirstSlotPosition + next × 6.0
        │
        ▼
  loader.MoveToTargetSlotAsync(targetZ)             ← Protrusion 인터락 + 병렬 감시
        │
        │ 예외 ⇒ AlarmManager.Raise(LOT-MOVE)
        ▼
  loader.MoveToExchangePositionAsync()              ← 4-step 시퀀스
        │
        │ false ⇒ AlarmManager.Raise(LOT-EX)
        ▼
  CurrentInputSlot = next
  InputWaferAtExchange = true
  RaiseLotPortChanged()                              ← UI 알림
        │
        ▼
  (Sim 모드) SimCassetteDriver.SetInputSlotWafer(next, false)   ← 슬롯 LED 갱신
        │
        ▼
  ─── InputStage Handoff ──────────────────────
        │
        ▼
  InputStage.LoadAndPrepareWaferAsync()
        │  → IsFeederAtSafePosition 확인 (≥140mm)
        │  → ExpanderZ Down (테이프 텐션)
        │  → Barcode 리딩
        │
        ├── 성공 ⇒ loader.RetractFeederAsync()      ← 피더 후퇴 (≤30mm)
        │           InputWaferAtExchange = false
        │           InputStage.VisionAlignAndSetupOriginAsync()
        │
        └── 실패 ⇒ Log + 종료
        ▼
  return true
```

### 5.2 사이클 종료 시 — `UnloadInputStageWaferAsync()`

사이클 끝나면 InputStage 에서 웨이퍼를 카세트로 되돌릴지 처리하는 단계가 있지만, 현재 구현은 **단순 unload** 만 수행 (피더로 카세트 복귀는 미구현, 다음 wafer 로 직접 진행).

---

## 6. 시뮬레이터 ↔ 실 축 매핑

### 6.1 Axis 번호 매핑 (`SimulatorBridge.cs`)

| 핸들러 측 (실 축) | Sim Axis No | MotionMap Name | Stroke (mm) | Brake |
|---|---|---|---|---|
| `InputLoader.ElevatorZ` | **0** | `WAFER LIFTER_Z` | 200 | ✅ |
| `InputLoader.FeederY` | **1** | `WAFER FEEDER_Y` | 300 | – |
| `OutputUnloader.FeederY` | **35** | `BIN FEEDER_Y` | 300 | – |
| `OutputUnloader.ElevatorZ` | **36** | `BIN LIFTER_Z` | 200 | ✅ |

### 6.2 Simulator 표시

`Simulator > 우측 패널 > MODULE AXIS POSITIONS`:
- ▼ **InputLoader (2)** → `#00 WAFER LIFTER_Z` (BRK), `#01 WAFER FEEDER_Y` (IDL/MOV)
- ▼ **OutputUnloader (2)** → `#35 BIN FEEDER_Y` (IDL/MOV), `#36 BIN LIFTER_Z` (BRK)

DispatcherTimer 100ms 간격으로 `MotionMap.GetAxis(no).CurrentPos` 를 읽어 실시간 갱신.
- 🟢 IDL: 정지
- 🟠 MOV: 이동 중 — `MoveAxis()` 의 보간 동안
- 🔴 BRK: 브레이크 잠김 (`IsBrakeAxis(0|7|36)`)

### 6.3 IO 연동 (`IoMap.cs` 일부)

| 핸들러 컴포넌트 | DO 심볼 (Y) | DI 심볼 (X) |
|---|---|---|
| `FeederUpDownCyl` (FWD) | Y016 WAFER FEEDER UP | X028 |
| `FeederUpDownCyl` (BWD) | Y017 WAFER FEEDER DOWN | X029 |
| `CassetteExistSensor` | – | (시뮬은 `SimCassetteDriver` 가 직접 set) |
| `ProtrusionSensor` | – | (시뮬은 정상 시 OFF) |
| `WaferDetectSensor` | – | (시뮬은 슬롯별 매핑) |

---

## 7. UI 표시 위치

### 7.1 핸들러 — 작업정보 탭 > InputFeederPage

`Ui/Pages/WorkInfo/InputFeederPage.cs`:
- ElevatorZ 현재 위치
- FeederY 현재 위치
- 카세트 16 슬롯 LED (`SimCassetteDriver.InputSlots`)
- 센서 상태 (Cassette/Protrusion/Detect/Clamped)
- 실린더 상태 (UpDown/Clamp)
- 액션: ScanCassette / LoadNext / RetractFeeder / Home

### 7.2 핸들러 — 작업정보 탭 > InputCassettePage

카세트 LED 16개 (slot 0~15), 각 LED 색상:
- 비어있음: LightGray
- 웨이퍼 있음: LimeGreen
- 현재 처리 슬롯: 주황 강조

### 7.3 시뮬레이터 — MODULE AXIS POSITIONS

위 §6.2 참고. 사이클 진행 중 `#00`, `#01` 의 CurrentPos / MOV/IDL 상태가 실시간 변동.

---

## 8. 시퀀스 검증 — 실제 사이클 로그 매핑

`LOT-20260429-162418` 사이클 기준 (8 die / good=8):

| 시각 (mm:ss.ms) | Event | 해당 메서드 |
|---|---|---|
| 00:00.000 | `[LOTPORT] WaferMap empty → scan cassette` | `LoadNextWaferAsync` 진입 |
| ~00:09 | `[INIT] WaferMap OK (16장 감지)` | `ScanCassetteAsync` 완료 |
| ~00:09.5 | `[LOTPORT] Move to slot 0 (Z=10.00mm)` | `MoveToTargetSlotAsync` |
| ~00:11 | `[LOTPORT] LoadNextWafer OK — slot=0` | `MoveToExchangePositionAsync` 완료 |
| ~00:11 | `[LOTPORT] InputStage handoff (LoadAndPrepare) 시작` | InputStage.LoadAndPrepare |
| ~00:13 | `[LOTPORT] 피더 후퇴 (InputStage 단독 작업으로 전환)` | `RetractFeederAsync` |
| ~01:07 | `[INPUTSTAGE] UnloadWafer 시작` | 8 die 처리 완료 |
| ~01:08 | `[INPUTSTAGE] UnloadWafer OK` | `UnloadInputStageWaferAsync` |
| ~01:08 | `[CYCLE] 완료 (good=8, ng=0)` | CycleRunAsync 종료 |

---

## 9. 알람 코드 정리

| 코드 | 발생 위치 | 의미 | 복구 |
|---|---|---|---|
| `LOT-NOCASS` | `LoadNextWaferAsync` 진입 | 카세트 미감지 | 카세트 안착 후 재시도 |
| `LOT-SCAN` | `ScanCassetteAsync` 실패 | ElevatorZ 알람 | 알람 클리어 + 홈 복귀 |
| `LOT-MOVE` | `MoveToTargetSlotAsync` 예외 | Protrusion 감지 또는 ElevatorZ 알람 | **육안 확인 필요** — 돌출 웨이퍼 제거 |
| `LOT-EX` | `MoveToExchangePositionAsync` 실패 | 클램프 미파지 또는 FeederY 알람 | 피더 홈 → 재시도 |

---

## 10. 핵심 설계 원칙 (Take-aways)

1. **두 단계 인터락**: `Sensor.IsOn` 검사 + `Task.WhenAny` 이동 중 병렬 감시 — 정적 + 동적 안전망 동시 사용.
2. **자동 안전 해제**: `MoveToExchangePositionAsync` 의 클램프 확인 실패 시 자동으로 `MoveBwd()` 실행 — 웨이퍼 안전 위치로 복귀 후 알람.
3. **위치 기반 안전 게이트**: `WaferLoaderAdapter` 가 `FeederY.ActualPosition` 만 보고 판정 — 단일 변수로 전체 인터락 결정 가능, 예외 케이스 최소화.
4. **3-구간 안전 모델**: 0~30 (홈), 30~140 (위험 — 이동 중), 140+ (교환) — 단순하지만 hand-off 시점에 모두 안전.
5. **시뮬레이터 일관성**: 실 축 ↔ Sim Axis 1:1 매핑 (`InputLoader.ElevatorZ` ↔ Axis 0). 시뮬도 같은 stroke/brake 정의 — 실보드 전환 시 코드 변경 없음.

---

**이 문서는 InputLoader/OutputUnloader 의 시퀀스만 다룬다. 다음 단계 문서**:
- `05_InputStage_시퀀스_상세분석.md` (예정) — Expander/Vision/Origin/MultiScan
- `06_TransferPicker_시퀀스_상세분석.md` (예정) — Pick/Inspect/Place + 멀티 픽커
- `07_OutputStage_시퀀스_상세분석.md` (예정) — ReceiveDie/Bin 분류
