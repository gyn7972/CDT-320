# CDT-320 OutputUnloader 적재 시퀀스 상세 분석

**문서 버전**: 1.0
**작성일**: 2026-04-29
**대상 코드**: `Equipment/OutputUnloaderUnit.cs`, `MachineController.StoreCompletedWaferAsync` (Stage 58)

---

## 1. 개요

OutputUnloader는 OutputStage에서 분류된 웨이퍼(완성품)를 **3개 카세트** (NG / Good1 / Good2)에 적재하는 마지막 단계다. Input 피더와 같은 4단계 메커니즘을 가지지만, **카세트 만석 자동 처리**와 **Good1→Good2 전환** 로직이 추가된다.

```
OutputStage     ┌───────────── OutputUnloader ──────────────┐
   │            │                                            │
   └─ 다이 적재 │  StoreFullWaferAsync(target, slot)         │
      WafersPerOutputBatch=8 다이마다 호출                    │
                │     │                                      │
                │     ▼                                      │
                │  ┌──────────────────────────────────┐     │
                │  │ ┌────┬────────┬────────┐         │     │
                │  │ │ Ng │ Good1  │ Good2  │         │     │
                │  │ │25 슬롯×3 = 총 75 슬롯            │     │
                │  │ └────┴────────┴────────┘         │     │
                │  │ ElevatorZ + FeederY ↔ 적재        │     │
                │  └──────────────────────────────────┘     │
                └────────────────────────────────────────────┘
```

---

## 2. 호출 트리거

`MachineController.PickAndPlaceDieAsync` 의 마지막 부분:
```csharp
// Stage 27 — 매 WafersPerOutputBatch 다이마다 Output 카세트 적재
if (WafersPerOutputBatch > 0 &&
    ((index + 1) % WafersPerOutputBatch == 0))
{
    Log("[FEEDER] {WafersPerOutputBatch} 다이 완료 — Output 적재");
    await StoreCompletedWaferAsync(inspPass);
}
```

기본 `WafersPerOutputBatch = 8` — **8 die 처리될 때마다 한 번** Output 적재 발생. 사용자가 `Recipe.Output.WafersPerOutputBatch` 로 변경 가능.

---

## 3. `StoreCompletedWaferAsync(bool isGood)` 흐름

### 3.1 카세트 + 슬롯 결정 (자동)

```
if isGood:
   if OutputSlotGood1 < 25:
      target = Good1
      slot   = OutputSlotGood1++
   elif OutputSlotGood2 < 25:
      target = Good2                  ← Good1 만석 자동 전환
      slot   = OutputSlotGood2++
   else:
      AlarmManager.Raise("OUT-FULL-GOOD", Error)
      _cycleCts.Cancel()              ← 사이클 자동 정지
      return false
else:  # NG
   if OutputSlotNg < 25:
      target = Ng
      slot   = OutputSlotNg++
   else:
      AlarmManager.Raise("OUT-FULL-NG", Error)
      _cycleCts.Cancel()              ← 사이클 자동 정지
      return false
```

**3대 안전장치**:
1. **만석 자동 전환** (Good1 → Good2)
2. **만석 시 자동 정지** (모든 카세트 채워진 후 다이 손실 방지)
3. **Cancel Token** (사이클 진행 중에도 즉시 중단)

### 3.2 적재 호출

```
unloader.StoreFullWaferAsync(target, slot)
   │  실패 ⇒ AlarmManager.Raise("OUT-STORE", Error) + return false
   │  예외 ⇒ AlarmManager.Raise("OUT-STORE-EX", Error) + return false
   ▼
SimCassetteDriver.SetOutputSlotFilled(target, slot, true)   // UI LED 동기화
   ▼
return true
```

---

## 4. `OutputUnloader.StoreFullWaferAsync(target, slot)` 내부

피더 메커니즘은 InputLoader와 같은 4-Step 패턴:

```
Step 1. 카세트 안착 확인
   IsCassettePresent(target)
       │  ExistSensor_NG / Good1 / Good2 중 해당 센서 IsOn
       │  false ⇒ return false
       ▼
Step 2. ElevatorZ → 슬롯 위치 (돌출 인터락 병렬 감시)
   targetZ = baseZ[target] + slot × Setup.SlotPitchZ
   MoveElevatorWithProtrusionGuardAsync(targetZ, velocity)
       │
       │  Task.WhenAny:
       │    - moveTask: ElevatorZ.MoveAbsolute
       │    - watchTask: ProtrusionSensor 10ms 폴링
       │  돌출 감지 ⇒ EStop + InvalidOperationException
       ▼
Step 3. FeederY → 카세트 등급별 교환 위치
   exchangeY = (target == Ng) ? NgStageExchangePositionY
                              : GoodStageExchangePositionY
   FeederY.MoveAbsolute(exchangeY)
       │  IsAlarm ⇒ return false
       ▼
Step 4. FeederUpDownCyl + FeederClampCyl + Y 후진 (적재 실행)
   FeederUpDownCyl.MoveFwd()   // 피더 하강
   FeederClampCyl.MoveBwd()    // 클램프 해제 (적재)
   WaferDetectSensor 확인       // 슬롯에 웨이퍼 안착 확인
   FeederY.MoveAbsolute(0)      // 원점 복귀
   FeederUpDownCyl.MoveBwd()    // 피더 상승
       ▼
   _slotMap[target][slot] = true  // 슬롯 점유 마킹
   return true
```

### 4.1 MoveElevatorWithProtrusionGuardAsync (Mismatch 보완 2026-04-30)

- 코드 위치: `OutputUnloaderUnit.cs:382-451` (시그니처 `private async Task MoveElevatorWithProtrusionGuardAsync(double targetPositionZ, double velocity)`)
- 본 메서드는 ElevatorZ 의 모든 절대 이동에 대해 **유일한 진입점**으로 사용된다
  (`ScanAllCassettesAsync`, `StoreFullWaferAsync`, `SupplyEmptyWaferAsync` 모두 이 헬퍼를 통해서만 ElevatorZ 를 이동시킨다).
- 동작 단계:
  1. **선제 검사**: `ProtrusionSensor.IsOn` → `ElevatorZ.EStop()` + `InvalidOperationException` 즉시 throw (`:386-393`).
  2. **병렬 모션 + 감시**: `Task.WhenAny`로 본 모션 (`ElevatorZ.MoveAbsoluteAsync(targetPositionZ, velocity)`) 와 돌출 감시 폴링 Task 동시 실행 (`:398-417`).
  3. **돌출 감시 폴링**: `Config.ProtrusionPollIntervalMs` (기본 10 ms) 주기로 `ProtrusionSensor.IsOn` 체크 (`:402-414`).
  4. **돌출 감지 시**: 감시 Task 가 먼저 완료되면 `ElevatorZ.EStop()` → 모션 Task 정리 → `InvalidOperationException` throw (`:425-436`).
  5. **이동 완료 후 축 알람 검사**: `ElevatorZ.IsAlarm` 시 동일하게 예외 (`:440-446`).
- InputLoaderUnit 측 동일 패턴: `InputLoaderUnit.cs:249` `MoveToTargetSlotAsync(double targetPosition)`
  (구조 동일, 폴링 주기는 코드 내 하드코딩 `Task.Delay(10)`, OutputUnloader 는 `Config.ProtrusionPollIntervalMs` 로 외부 노출).

---

## 5. SoftLimit 확장 (Stage 27)

기본값으로는 부족한 stroke:
```csharp
FeederY  .Setup.SoftLimitPlus = 350.0;   // CassetteInsertPositionY=250mm 까지
ElevatorZ.Setup.SoftLimitPlus = 400.0;   // Good2 마지막 슬롯 = 160 + 24×6 = 304mm 까지
```
이전 값 200mm 로는 Good2 카세트의 상단 슬롯 도달 불가.

---

## 6. 카세트 슬롯 좌표 계산

```
GetSlotPositionZ(cassette, slotIndex):
   switch cassette:
      case Ng:    baseZ = Setup.NgFirstSlotPositionZ
      case Good1: baseZ = Setup.Good1FirstSlotPositionZ
      case Good2: baseZ = Setup.Good2FirstSlotPositionZ
   return baseZ + slotIndex × Setup.SlotPitchZ
```

**예시 (Good2 사양)**:
```
baseZ        = 160 mm
SlotPitchZ   = 6 mm
slot=0:    160 mm
slot=12:   232 mm   (가운데)
slot=24:   304 mm   (최상단)
```

### 6.1 Stage 교환 위치 (Mismatch 보완 2026-04-30)

`StoreFullWaferAsync` Step 3 의 `exchangeY` 결정 분기에 사용되는 `OutputLoaderSetup` 의 두 파라미터.
`GetStageExchangePositionY(cassette)` (`OutputUnloaderUnit.cs:354`) 가 카세트 등급에 따라 둘 중 하나를 반환한다.

| 파라미터 | 기본값 | 설명 | 라인 |
|---|---|---|---|
| `GoodStageExchangePositionY` | 150.0 mm | Good 등급 다이를 GoodStage 앞으로 픽업/배출하는 FeederY 위치 | `OutputUnloaderUnit.cs:80`, 사용처 `:359` |
| `NgStageExchangePositionY` | 200.0 mm | NG 등급 다이를 NgStage 앞으로 픽업/배출하는 FeederY 위치 | `OutputUnloaderUnit.cs:86`, 사용처 `:358` |

```csharp
// OutputUnloaderUnit.cs:354-360
private double GetStageExchangePositionY(TargetCassette cassette)
{
    return cassette == TargetCassette.Ng
        ? Setup.NgStageExchangePositionY
        : Setup.GoodStageExchangePositionY;
}
```

---

## 7. UI 표시

- **WorkInfoTab > OutputCassettePage**: 3개 카세트 25 슬롯 LED + 현재 적재 슬롯 강조
- **Simulator > MODULE AXIS POSITIONS > OutputUnloader (2)**: #35 BIN FEEDER_Y, #36 BIN LIFTER_Z (BRK)

---

## 8. 인터락 정리

| 인터락 | 검사 시점 | 위반 시 동작 |
|---|---|---|
| `IsCassettePresent(target)` | StoreFullWafer Step 1 | return false |
| `ProtrusionSensor.IsOn` | MoveElevatorWithProtrusionGuard | E-Stop + 예외 |
| `ElevatorZ.IsAlarm` | 이동 후 | 예외 |
| `FeederY.IsAlarm` | Step 3 후 | return false |
| 카세트 만석 | StoreCompletedWafer | `_cycleCts.Cancel()` (사이클 자동 정지) |
| `WaferDetectSensor` 미검출 | Step 4 후 | (옵션 — 적재 실패로 추정) |

---

## 9. 알람 코드

| 코드 | 발생 위치 | 의미 | 복구 |
|---|---|---|---|
| `OUT-FULL-GOOD` | StoreCompletedWafer | Good1+Good2 모두 만석 | 카세트 교체 |
| `OUT-FULL-NG` | StoreCompletedWafer | NG 카세트 만석 | NG 카세트 교체 |
| `OUT-STORE` | StoreFullWaferAsync false | 피더 시퀀스 실패 | 알람 클리어 후 재시도 |
| `OUT-STORE-EX` | StoreFullWaferAsync 예외 | Protrusion 감지 또는 ElevatorZ 알람 | **육안 확인** — 돌출 웨이퍼 제거 |

### 9.1 알람 코드 발급 위치 검증 (Mismatch 보완 2026-04-30)

`MachineController.cs` grep 결과 OUT-* 4 종 알람은 모두 `MachineController.StoreCompletedWaferAsync` 내부에서 직접 `AlarmManager.Raise` 로 발급된다 (ARCHITECTURE_EXPORT v2 §7.2 와 일치).

| 코드 | 발급 위치 (코드 라인) | 현재 처리 | TODO |
|---|---|---|---|
| `OUT-FULL-GOOD` | `MachineController.cs:365` | 구현됨 (`AlarmManager.Raise(AlarmSeverity.Error, ...)`, `_cycleCts.Cancel()`) | — |
| `OUT-FULL-NG`   | `MachineController.cs:378` | 구현됨 (동일) | — |
| `OUT-STORE`     | `MachineController.cs:391` | 구현됨 (`StoreFullWaferAsync` false 반환 분기) | — |
| `OUT-STORE-EX`  | `MachineController.cs:420` | 구현됨 (예외 catch 분기) | — |

**`OutputUnloaderUnit` 자체에서 직접 발급하는 OUT-* 알람 코드는 없음** — 모든 진단성 메시지는 `Console.WriteLine("[ALARM] ...")` 로만 출력되며, 알람 코드 발급은 상위 `MachineController` 가 일괄 담당한다. 따라서 별도 TODO 없음.

---

## 10. 사이클 검증

`LOT-20260429-162418` (8 die / WafersPerOutputBatch=8) → 1회 호출:
| 시각 | 라인 | OutputUnloader |
|---|---|---|
| 16:25:30 | `[FEEDER] 8 다이 완료 — Output 적재` | StoreCompletedWaferAsync 진입 |
| 16:25:30 | `[FEEDER] StoreFullWafer → Good1 Slot[0]` | 카세트/슬롯 결정 |
| 16:25:32 | `[FEEDER] OK — Good1 Slot[0] 적재 완료` | StoreFullWafer 성공 |

---

## 11. 상위 흐름과의 통합

```
MachineController.PickAndPlaceDieAsync(index):
   ...
   ── 결과 분류 (Good/NG) ──
   if (index+1) % WafersPerOutputBatch == 0:
      StoreCompletedWaferAsync(inspPass)        ← 본 문서
         │
         ├── isGood이면 Good1/Good2 자동 선택
         ├── unloader.StoreFullWaferAsync(target, slot)
         └── SimCassetteDriver.SetOutputSlotFilled (UI 동기화)
   ...
```

---

## 12. 추가 메서드 — 코드에만 있는 시퀀스 (Mismatch 보완 2026-04-30)

§3·§4 까지의 본문은 사이클 중 가장 빈번하게 호출되는 `StoreFullWaferAsync` 한 가지에 집중되어 있으나, `OutputUnloaderUnit` 은 이 외에도 세 가지 공개(public) 시퀀스를 제공한다. 본 절은 코드 직접 검증으로 보완한 누락분이다.

### 12.1 ScanAllCassettesAsync()

- 시그니처: `public async Task<bool> ScanAllCassettesAsync()` (`OutputUnloaderUnit.cs:630-716`)
- 호출 시점: `MachineController.ScanOutputCassettesAsync()` (`MachineController.cs:427`, 호출 라인 `:432` `await unloader.ScanAllCassettesAsync()`).
  - 사이클 시작 시 `MachineController.cs:726` `bool oOk = await ScanOutputCassettesAsync();` 로 자동 호출.
  - 수동 트리거: `Ui/Pages/WorkInfo/OutputPages.cs:114` 의 `MAP` 버튼 → `host.Controller.ScanOutputCassettesAsync()`, 그리고 `:274` 사이클 시작 시 자동 호출.
- 동작 단계:
  1. 카세트 순회 배열 `{ Ng, Good1, Good2 }` (`:634-639`).
  2. 각 카세트마다 `IsCassettePresent` 검사 → 미안착이면 `_slotMap[c] = new bool[0]` 후 `continue` (`:647-654`).
  3. `MaxSlotsPerCassette` (기본 25) 만큼 `bool[]` 할당 (`:656-657`).
  4. `MoveElevatorWithProtrusionGuardAsync(GetSlotPositionZ(c, 0), Recipe.ScanVelocity)` 로 첫 슬롯 이동 (`:660-670`).
  5. `for i = 0..maxSlots-1`: `i > 0` 일 때 `MoveElevatorWithProtrusionGuardAsync(GetSlotPositionZ(c, i), Recipe.ScanVelocity)` 로 다음 슬롯 이동 (`:673-690`).
  6. 슬롯 도달마다 `Task.Delay(Config.ScanSettleTimeMs)` (기본 100 ms) 안정화 후 `WaferDetectSensor.IsOn` 을 `map[i]` 에 기록 (`:692-702`).
  7. `_slotMap[cassette] = map`; 채워진 슬롯 수 카운트 후 `[INFO]` 로그 (`:704-712`).
- 합계 75 슬롯 (= 3 × 25) 스캔. 어떤 카세트에서도 돌출 또는 축 알람 발생 시 `false` 반환.

### 12.2 SupplyEmptyWaferAsync(TargetCassette source, int slotIndex)

- 시그니처: `public async Task<bool> SupplyEmptyWaferAsync(TargetCassette source, int slotIndex)` (`OutputUnloaderUnit.cs:819-884`)
- 호출 시점:
  - 수동 트리거: `Ui/Pages/WorkInfo/OutputPages.cs:115` `PICK` 버튼 → `host.Machine.OutputUnloader.SupplyEmptyWaferAsync(TargetCassette.Good1, 0)` (디버그용 단독 실행).
  - 정규 호출: `ExchangeWaferSequenceAsync` Step 4-2 (`:939`) — 자동 사이클에서는 `ExchangeWaferSequenceAsync` 경유로만 호출됨.
  - `MachineController` 본 사이클 (Stage 27) 에서는 호출되지 않음 (`StoreFullWaferAsync` 만 사용).
- 동작 단계 (`StoreFullWaferAsync` 의 역방향, Cassette → Stage):
  1. `IsCassettePresent(source)` 확인 → false 시 `[ALARM]` 로그 후 `return false` (`:826-832`).
  2. `slotZ = GetSlotPositionZ(source, slotIndex)` (`:835`).
  3. `MoveElevatorWithProtrusionGuardAsync(slotZ, Recipe.ElevatorVelocity)` — 돌출/축 알람 발생 시 catch → `return false` (`:841-850`).
  4. `PickupWaferAtPositionAsync(Setup.CassetteInsertPositionY)` 로 카세트 슬롯에서 빈 웨이퍼 파지 (`:853-859`).
  5. `stageY = GetStageExchangePositionY(source)` 후 `PlaceWaferAtPositionAsync(stageY)` 로 OutputStage 에 배출 (`:866-874`).
  6. `_slotMap[source][slotIndex] = false` 마킹 (`:877-878`).

### 12.3 ExchangeWaferSequenceAsync(storeTarget, storeSlotIndex, supplySource, supplySlotIndex)

- 시그니처:
  ```csharp
  public async Task<bool> ExchangeWaferSequenceAsync(
      TargetCassette storeTarget,  int storeSlotIndex,
      TargetCassette supplySource, int supplySlotIndex)
  ```
  (`OutputUnloaderUnit.cs:907-956`)
- 호출 시점: `Equipment/Sim/OutputUnloaderAdapter.cs:9, 27` 주석 — "실제 Unloader 의 ExchangeWaferSequenceAsync 를 호출". 즉 어댑터 계층 (Sim/통합 인터페이스) 진입점이며, 현재 메인 사이클 (`MachineController.PickAndPlaceDieAsync` Stage 27) 에서는 사용하지 않고 `StoreFullWaferAsync` 만 직접 호출한다.
- 동작 단계 — 두 시퀀스를 직렬 실행 (Step 사이 추가 모션 없음):
  1. **Step 1** `StoreFullWaferAsync(storeTarget, storeSlotIndex)` (`:920`) — 실패 시 즉시 `return false`, **Step 2 진행 금지** (주석: 중간 상태에서 빈 웨이퍼 잘못 공급 → 다이 손실 방지, `:923-930`).
  2. **Step 2** `SupplyEmptyWaferAsync(supplySource, supplySlotIndex)` (`:939`) — 실패 시 `return false` (Stage 빈 상태이므로 TPU 일시 정지 필요, 주석 `:942-947`).
  3. 양쪽 모두 성공 시 `[INFO] 교체 시퀀스 성공적으로 완료.` 로그 (`:953-955`).
- 인터락: 두 단계 각각 내부에서 `MoveElevatorWithProtrusionGuardAsync` 와 `IsCassettePresent` 를 자체 수행하므로 Exchange 레벨에서는 추가 검사 없음.
