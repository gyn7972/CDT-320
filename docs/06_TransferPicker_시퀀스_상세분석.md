# CDT-320 TransferPicker 시퀀스 상세 분석

**문서 버전**: 1.1 (Mismatch 보완 2026-04-30)
**작성일**: 2026-04-29 / 보완: 2026-04-30
**대상 코드**: `Equipment/TransferPickerUnit.cs` (Stage 58, 956줄)
**검증 코드**: `Equipment/TransferPickerUnit.cs`, `Equipment/SimulatorBridge.cs`, `Equipment/CDT320Machine.cs`, `Equipment/Vision/VisionAdapters.cs`, `CDT320Simulator/MotionMap.cs`

---

## 1. 개요

TransferPicker(이하 TPU)는 InputStage와 OutputStage 사이에 위치한 **2-Arm × 4-Picker × 2-Vision** 구조의 핵심 이송기다. 한 사이클당 다이 1~4개를 동시에 처리하며, Bottom Vision + Side Vision 4면 검사를 모두 수행한다.

```
InputStage ── ⟨ FRONT Arm (Left) ⟩  ── OutputStage
              │ ArmX, ArmY, 4 Pickers
              │ Bottom Vision + Side 4면
              ⟨ REAR Arm (Right) ⟩
              │ 동일 구성, 좌우 대칭
```

---

## 2. 계층 구조

```
TransferPickerUnit
   ├── LeftArm  : TpuArmUnit  (FRONT)
   │     ├── ArmX, ArmY, SideVisionY (axis 9, 10, 19)
   │     └── Pickers[0..3]: PickerComponent
   │           └── PickerZ, PickerT, BlowDO, VacuumDO
   │
   └── RightArm : TpuArmUnit  (REAR)
         ├── ArmX, ArmY, SideVisionY (axis 21, 22, 20)
         └── Pickers[0..3]: PickerComponent
```

### 2.1 PickerComponent 메서드 (코드 기준 — TransferPickerUnit.cs:249~400)

| 메서드 | 라인 | 동작 |
|---|---|---|
| `PickupAsync()` | 317~332 | PickerZ → PickupPosition + VacuumOn + Task.Delay(VacuumSettleMs) |
| `MoveToFocusAsync()` | 339~350 | PickerZ → FocusPosition (Bottom/Side Vision 촬상 전) |
| `MoveToWaitAsync()` | 356~367 | PickerZ → WaitPosition (대기 상승 — 단독 호출용) |
| `PlaceAsync()` | 374~399 | PickerZ → PlacePosition + VacuumOff + BlowOn + Task.Delay(BlowPulseMs) + BlowOff + PickerZ → WaitPosition |
| `VacuumOn() / VacuumOff()` | 302/305 | VacuumOut DO 토글 |
| `BlowOn() / BlowOff()` | 308/311 | BlowOut DO 토글 |

> 주의: 메서드 이름은 코드상 `PickupAsync` (이전 문서 표기 `PickAsync`는 부정확). 또한 `PickupAsync` 본체는 VacuumOn 후 **WaitPosition 자동 상승을 수행하지 않는다** — Pickup 자세 그대로 유지된다. 상위 호출자가 별도로 `MoveToWaitAsync()` 또는 후속 시퀀스(InspectBottomVisionAsync 의 ArmX 이동 + MoveToFocusAsync) 를 통해 다음 자세로 진입한다.

### 2.2 TpuArmUnit 핵심 메서드 (코드 기준 — TransferPickerUnit.cs:418~899)

| 메서드 | 라인 | 동작 |
|---|---|---|
| `InspectBottomVisionAsync()` | 523~595 | 4 피커 순차 Bottom Vision (ArmX 오프셋 = BottomVisionX + i*PickerPitchX, ArmY = BottomVisionY) — 인자 없음, Pickers[0..3] 전체에 대해 내부 루프 |
| `InspectSideVisionAsync(BottomVisionOffset[] bottomOffsets)` | 622~842 | 4 피커 × 4면 Side Vision (Side1+2 → Task.WhenAll(PickerT 90도 + ArmX/Y 동시) → Side3+4) |
| `PlaceDiesAsync()` | 860~898 | ArmX/Y → (PlacePositionX, PlacePositionY) 동시 이동 후, 피커 1~4번 순차 PlaceAsync 호출 — 인자 없음 |

> 주의: 문서 v1.0 의 `MoveArmToInputAsync()` / `MoveArmToOutputAsync()` 는 코드에 존재하지 않는다. 이동은 InspectBottomVisionAsync (BottomVisionX), InspectSideVisionAsync (SideVision1/2X), PlaceDiesAsync (PlacePositionX) 의 내부 ArmX/ArmY MoveAbsoluteAsync 로 직접 수행된다.

---

## 3. 사이클 1 다이 처리 — End-to-End 시퀀스

`MachineController.PickAndPlaceDieAsync(int index)` 호출 흐름 (front=Left arm 기준):

```
Step 1. PICK 위치 이동
   front.ArmX.MoveAbsolute(300mm, 800mm/s)   // PICKUP_MM
        │  Interlock blocked ⇒ NgCount++; PickFailCount++; return
        ▼
   Stage 58 ─ Collet1UseCount++; NeedleUseCount++

Step 2. Eject Pin Z 작동 (다이 분리)
   InputStage.EjectPinZ.ServoOn()
   EjectPinZ.MoveAbsolute(NeedleEjectPosition=5mm)
   Delay 50ms
   EjectPinZ.MoveAbsolute(NeedleDownPosition=0)

Step 3. PICK 동작 (4 picker 동시 또는 단독)  [코드: PickerComponent.PickupAsync 라인 317~332]
   if PickersPerCycle == 1:
      front.Pickers[0].PickupAsync()
         │ PickerZ.MoveAbsolute(Setup.PickupPosition, Recipe.ZVelocity)
         │ if PickerZ.IsAlarm => return false
         │ VacuumOn()                                  # VacuumOut DO ON
         │ Task.Delay(Recipe.VacuumSettleMs)            # 진공 안정화 대기
   else:
      Task.WhenAll(front.Pickers[0..n-1].PickupAsync())   // 멀티 픽커 동시 픽업

Step 4. PICK 후 ArmZ 상승은 PickupAsync 내부에서 수행되지 않음 — Pickup 자세 그대로 유지.
        다음 단계(InspectBottomVisionAsync 의 ArmX/Y 이동 + MoveToFocusAsync) 가
        FocusPosition 으로 직접 진입하므로 별도 WaitPosition 복귀가 불필요하다.
        (단독 상승이 필요한 경로는 MoveToWaitAsync 를 별도 호출)

Step 5. Bottom Vision 촬상 (옵션 — VisionConnected && Bottom != null)
   front.InspectBottomVisionAsync()                  // 인자 없음 — 내부에서 Pickers[0..3] 전체 루프
        │  [코드: TransferPickerUnit.cs:523~595]
        │  for i in 0..3: ArmX/Y 동시 이동 → MoveToFocusAsync → TriggerBottomExposeAsync
        │  4 피커 종료 후 GetBottomResultsAsync 일괄 수신
        │  Returns BottomVisionOffset[] — 픽커별 OffsetX/Y/IsOk
        │  IsOk == false ⇒ inspPass = false

Step 6. Side Vision 4면 검사 (옵션)
   front.InspectSideVisionAsync(bottomResults)        // BottomVisionOffset[] 인자
        │  [코드: TransferPickerUnit.cs:622~842]
        │  for i in 0..3: Side1+2 → Task.WhenAll(PickerT.+90° + ArmX/Y Side3 이동) → Side3+4 → GetSideResult
        │  Returns SideVisionResult[] — Side1~4 각 OK
        │  IsAllOk == false ⇒ inspPass = false

Step 7. PLACE 위치 이동
   front.ArmX.MoveAbsolute(1200mm, 800mm/s)   // PLACE_MM
        │  Interlock blocked ⇒ NgCount++; PlaceFailCount++; return

Step 8. OutputStage.ReceiveDieAsync 호출 (충돌 회피 + StageZ 상승 + StageY 이동)
   ReceiveDieRequest:
      Grade = inspPass ? Good : Ng
      TpuOffsetX/Y = 0
      VisionOffsetX/Y = bottomOffsets[0].OffsetX/Y

Step 9. Bin 비전 (옵션 — inspPass 일 때만)
   VisionHub.Bin.InspectAsync("PlacementInspector", index)
       │  IsPass == false ⇒ inspPass = false

Step 10. OutputStage.InspectBinPositionAsync()
   Tpu.WaitPlaceDoneAsync(TpuPlaceDoneTimeoutMs)
   BinCameraX → 검사 위치 → 검사 → 후퇴

Step 11. Place 실행  [코드: PickerComponent.PlaceAsync 라인 374~399]
   ※ 4 픽커 일괄 Place 가 필요한 경로는 TpuArmUnit.PlaceDiesAsync (860~898) 가
     ArmX/Y 동시 이동 + 1~4번 순차 PlaceAsync 호출을 캡슐화.
   if PickersPerCycle == 1:
      front.Pickers[0].PlaceAsync()
         │  PickerZ.MoveAbsolute(Setup.PlacePosition, Recipe.ZVelocity)
         │  if PickerZ.IsAlarm => return false
         │  VacuumOff()                                # VacuumOut DO OFF
         │  BlowOn() ; Task.Delay(Recipe.BlowPulseMs) ; BlowOff()
         │  PickerZ.MoveAbsolute(Setup.WaitPosition, Recipe.ZVelocity)   ← 자동 상승 복귀
         │  if PickerZ.IsAlarm => return false
         │  실패 ⇒ PlaceFailCount++
   else:
      front.PlaceDiesAsync()                          // ArmX/Y 동시 이동 + 1~4 순차 PlaceAsync
      okCount < n ⇒ PlaceFailCount += (n - okCount)
      Collet2UseCount += (n-1)        // 추가 픽커는 #2 collet 카운트

Step 12. 안전 VacuumOff (Place 내부에서 이미 호출됨, redundant safety)
   front.Pickers[0].VacuumOff()

Step 13. 결과 분류 + Lot 기록
   if inspPass:
      die.Result = Good
      die.BinCode = BinCodeMap.ConvertToBinCode(die)
      JobQueue.MarkDone(pickJob, "Good")
      GoodCount++
      PlateRegistry.RecordGoodDie(die.BinCode)
   else:
      die.AddNG("BottomInspFail")
      die.BinCode = BinCodeMap.ConvertToBinCode(die)
      JobQueue.MarkFailed(pickJob, ...)
      NgCount++
      PlateRegistry.RecordNgDie(die.BinCode)

Step 14. 주기적 처리
   if (index+1) % WafersPerOutputBatch == 0:   StoreCompletedWaferAsync()
   if (index+1) % DiesPerColletClean == 0:     OutputStage.PerformColletCleaningAsync()

Step 15. Lot 통계 + Reject 분리
   LotStorage.ActiveLot.RecordDie(die.BinCode, inspPass)
   if SubPortMaterialRejector.ShouldReject(die):
      Log reject 좌표
```

---

## 4. Side Vision 4면 검사 — 회전 동시 구동 최적화

`InspectSideVisionAsync()` 의 핵심 최적화:

```
for picker_no in 1..4:
   ── [회전 전] Side 1, 2 ─────────────────────
   ArmX.Move(Side1X) ║ ArmY.Move(Side1Y)        // 동시
   PickerZ.MoveToFocus()
   Vision.TriggerSideExposeAsync(picker, side=1)  // Expose End 대기만
   ArmX.Move(Side2X) ║ ArmY.Move(Side2Y)        // 즉시 다음 위치 (분석은 백그라운드)
   PickerZ.MoveToFocus()
   Vision.TriggerSideExposeAsync(picker, side=2)

   ── [회전] PickerT 90° ⇕ Side3 X/Y 이동 동시 ─
   Task.WhenAll(
      PickerT.MoveAbsolute(90°),
      ArmX.MoveAbsolute(Side3X),
      ArmY.MoveAbsolute(Side3Y)
   )                                              // 회전 시간 = 이동 시간에 감춤

   ── [회전 후] Side 3, 4 ─────────────────────
   PickerZ.MoveToFocus()
   Vision.TriggerSideExposeAsync(picker, side=3)
   ArmX.Move(Side4X) ║ ArmY.Move(Side4Y)
   PickerZ.MoveToFocus()
   Vision.TriggerSideExposeAsync(picker, side=4)

   ── 결과 일괄 수집 ───────────────────────────
   results[picker-1] = Vision.GetSideResultsAsync(picker)   // 4면 한번에
```

**이득**:
- 4면 직렬 처리 시: `4 × (이동 + 노출 + 분석)`
- 본 파이프라인: `4 × (이동 + 노출)` + 회전을 이동에 감춤 → ~30% 단축

### 4.1 코드 본체 검증 (Mismatch 보완 2026-04-30)

`TpuArmUnit.InspectSideVisionAsync(BottomVisionOffset[] bottomOffsets)` 본체 (TransferPickerUnit.cs:622~842):

| 단계 | 라인 | 동작 (코드) |
|---|---|---|
| BottomOffset 적용 | 635~642 | `offsetX/Y = bottomOffsets[i].OffsetX/Y` (null 안전 처리) |
| Side1 ArmX/Y 이동 | 653~655 | `Task.WhenAll(ArmX.MoveAbsolute(side1X), ArmY.MoveAbsolute(side1Y))` |
| Side1 PickerZ 포커스 | 666 | `picker.MoveToFocusAsync()` |
| Side1 Trigger | 675~676 | `Vision.TriggerSideExposeAsync(pickerNo, 1, ExposeTimeoutMs)` |
| Side2 ArmX/Y 이동 | 695~697 | `Task.WhenAll(ArmX.MoveAbsolute(side2X), ArmY.MoveAbsolute(side2Y))` |
| Side2 Trigger | 712~713 | `Vision.TriggerSideExposeAsync(pickerNo, 2, ...)` |
| **회전+이동 동시** | **739~747** | **`Task rotateTask = picker.PickerT.MoveRelativeAsync(90, ThetaVelocity)` + `Task focusMoveTask = Task.WhenAll(ArmX..., ArmY...)` → `await Task.WhenAll(rotateTask, focusMoveTask)`** |
| Side3 Trigger | 770~771 | `Vision.TriggerSideExposeAsync(pickerNo, 3, ...)` |
| Side4 ArmX/Y 이동 | 789~791 | `Task.WhenAll(ArmX.MoveAbsolute(side4X), ArmY.MoveAbsolute(side4Y))` |
| Side4 Trigger | 806~807 | `Vision.TriggerSideExposeAsync(pickerNo, 4, ...)` |
| 결과 일괄 수신 | 822~823 | `Vision.GetSideResultAsync(pickerNo, ResultTimeoutMs)` |

> 핵심: PickerT 회전은 `MoveRelative(90.0)` (절대각이 아니라 **상대각 +90°**) 이며, ArmX/ArmY Side3 이동과 `Task.WhenAll` 로 동시 실행됨. (코드 일치 — TransferPickerUnit.cs:622~842, Task.WhenAll 회전+이동 라인 747, 2026-04-30)

`TpuArmUnit.InspectBottomVisionAsync()` 본체 (TransferPickerUnit.cs:523~595): 인자 **없음**. 4 피커 순차 루프(527) 내부에서 ArmX/Y 동시 이동(537~539) → MoveToFocusAsync(550) → TriggerBottomExposeAsync(562) → 4 피커 종료 후 GetBottomResultsAsync(584) 일괄 수신. (코드 일치 — 2026-04-30)

`TpuArmUnit.PlaceDiesAsync()` 본체 (TransferPickerUnit.cs:860~898): 인자 **없음**. ArmX/Y → PlacePositionX/Y 동시 이동(865~867) 후 피커 1~4번 순차 `picker.PlaceAsync()` 호출(885). 어느 한 픽커라도 실패 시 즉시 `return false`. (코드 일치 — 2026-04-30)

---

## 5. PickerComponent.PickupAsync 내부 (코드 기준 — TransferPickerUnit.cs:317~332)

```csharp
public async Task<bool> PickupAsync()
{
    await PickerZ.MoveAbsoluteAsync(Setup.PickupPosition, Recipe.ZVelocity);
    if (PickerZ.IsAlarm) { ALARM 로그; return false; }

    VacuumOn();                                     // VacuumOut DO ON
    await Task.Delay(Recipe.VacuumSettleMs).ContinueWith(_ => { });
    return true;
    // ※ WaitPosition 자동 상승은 수행하지 않음 — Pickup 자세 유지.
}
```

### 5.1 PlaceAsync 내부 (코드 기준 — TransferPickerUnit.cs:374~399)

```csharp
public async Task<bool> PlaceAsync()
{
    // 하강
    await PickerZ.MoveAbsoluteAsync(Setup.PlacePosition, Recipe.ZVelocity);
    if (PickerZ.IsAlarm) return false;

    // 진공 OFF + Blow 파기 펄스
    VacuumOff();
    BlowOn();
    await Task.Delay(Recipe.BlowPulseMs).ContinueWith(_ => { });
    BlowOff();

    // 상승 복귀
    await PickerZ.MoveAbsoluteAsync(Setup.WaitPosition, Recipe.ZVelocity);
    if (PickerZ.IsAlarm) return false;
    return true;
}
```

### 5.2 MoveToFocusAsync / MoveToWaitAsync (코드 기준 — 339~350 / 356~367)

```csharp
public async Task<bool> MoveToFocusAsync()                  // 339~350
{
    await PickerZ.MoveAbsoluteAsync(Setup.FocusPosition, Recipe.ZVelocity);
    return !PickerZ.IsAlarm;
}

public async Task<bool> MoveToWaitAsync()                   // 356~367
{
    await PickerZ.MoveAbsoluteAsync(Setup.WaitPosition, Recipe.ZVelocity);
    return !PickerZ.IsAlarm;
}
```

### 5.3 코드 본체 검증 (Mismatch 보완 2026-04-30)

| 메서드 | 라인 | 시그니처 일치 | 본문 단계 일치 |
|---|---|---|---|
| `PickerComponent.PickupAsync()` | 317~332 | 일치 (`Task<bool>`, 인자 없음) | **부분 일치** — 문서 v1.0 §3 Step 4 의 "WaitPosition 복귀" 부분이 실제 코드에는 없음. PickupAsync 는 Pickup 자세 유지로 끝남 |
| `PickerComponent.PlaceAsync()` | 374~399 | 일치 (`Task<bool>`, 인자 없음) | 일치 (PlacePosition → VacuumOff → BlowOn/Delay/BlowOff → WaitPosition) |
| `PickerComponent.MoveToFocusAsync()` | 339~350 | 일치 | 일치 (FocusPosition 단일 이동) |
| `PickerComponent.MoveToWaitAsync()` | 356~367 | 일치 | 일치 (WaitPosition 단일 이동) |

> 주요 보완: 메서드명은 `PickAsync` 가 아닌 **`PickupAsync`** 이며, 본체에 WaitPosition 자동 상승이 **없다**. `Setup.PickupPosition` 으로 하강한 그대로 종료한다 (사이클 흐름상 다음 단계에서 ArmX 이동 + MoveToFocusAsync 로 자연스럽게 상승).

### 5.4 IVisionTpuClient 구현체 (Mismatch 보완 2026-04-30)

`interface IVisionTpuClient` 정의: TransferPickerUnit.cs:70~112 (메서드 4개 — TriggerBottomExposeAsync, GetBottomResultsAsync, TriggerSideExposeAsync, GetSideResultAsync).

| 구현체 | 파일 | 라인 | 역할 |
|---|---|---|---|
| `NullVisionTpuClient` | CDT320Machine.cs | 91~117 | 빌드/시뮬용 Null Object — Trigger 모두 true 반환, GetBottomResults 는 4개 IsOk=true OffsetX/Y=0 배열, GetSideResult 는 4면 모두 Ok=true |
| `TpuVisionAdapter` | Equipment/Vision/VisionAdapters.cs | 69~134 | 실 Vision PC TCP 어댑터 — `VisionHub.Inspection` 통해 `ExposeAsync` / `MatchAsync("DieFinder")` / `InspectAsync("SurfaceInspector")` 호출. VisionHub 미연결 시 fallback (Trigger=false, GetBottomResults=null, GetSideResult=null) |

> CDT320Machine.cs 생성자 라인 225 에서 `TransferPicker = new TransferPickerUnit(new VisionComm.TpuVisionAdapter())` 로 실 어댑터를 주입한다 — `NullVisionTpuClient` 는 현재 미사용 상태이나 향후 단독 단위 테스트 / VisionHub 미설치 빌드 fallback 용으로 유지.

---

## 6. 인터락 정리

| 인터락 | 검사 시점 | 위반 시 동작 |
|---|---|---|
| `MoveAxisAsync` interlock blocked | PICK/PLACE/Side 위치 이동 | `JobQueue.MarkFailed` + 카운터 증가 + return |
| `PickerZ.IsAlarm` | Pick/Place/MoveToFocus | return false → 상위 NgCount++ |
| `ArmX/ArmY.IsAlarm` | Side Vision 위치 이동 | return null (전체 Side 검사 중단) |
| `Vision.TriggerSideExposeAsync` 실패 | 매 Side trigger | return null |
| `Tpu.IsPickerReady == false` | InputStage MultiScan 호출 | 다이 1개 skip |
| `Tpu.WaitPlaceDoneAsync` timeout | OutputStage InspectBin | return false |
| Side Vision `IsAllOk` false | Step 6 후 | inspPass = false (NG bin 분류) |

---

## 7. Sim 매핑

| 핸들러 측 | Sim Axis No | Sim Name |
|---|---|---|
| `LeftArm.ArmX` | 9 | FRONT PICKER_X (1500mm) |
| `LeftArm.ArmY` | 10 | FRONT PICKER_Y (750mm) |
| `LeftArm.Pickers[0].PickerT` | 11 | FRONT PICKER_T0 |
| `LeftArm.Pickers[0].PickerZ` | 12 | FRONT PICKER_Z0 (50mm) |
| `LeftArm.Pickers[1].T/Z` | 13/14 | T1/Z1 |
| `LeftArm.Pickers[2].T/Z` | 15/16 | T2/Z2 |
| `LeftArm.Pickers[3].T/Z` | 17/18 | T3/Z3 |
| `LeftArm.SideVisionY` *(Stage 44)* | 19 | FRONT SIDE VISION_Y0 |
| `RightArm.SideVisionY` *(Stage 44)* | 20 | REAR SIDE VISION_Y0 |
| `RightArm.ArmX` | 21 | REAR PICKER_X |
| `RightArm.ArmY` | 22 | REAR PICKER_Y |
| `RightArm.Pickers[0..3].T/Z` | 23~30 | REAR_T0~Z3 |

### 7.1 코드 검증 (Mismatch 보완 2026-04-30)

`SimulatorBridge.BuildMaps()` (SimulatorBridge.cs:88~156) 매핑 상세:

| 코드 라인 | 매핑 |
|---|---|
| 106 | `_axisMap[left.ArmX] = 9` (FRONT PICKER_X) |
| 107 | `_axisMap[left.ArmY] = 10` (FRONT PICKER_Y) |
| 108~112 | LeftArm Pickers[0..3]: T = 11+i*2 (11/13/15/17), Z = 12+i*2 (12/14/16/18) |
| 116 | `_axisMap[right.ArmX] = 21` (REAR PICKER_X) |
| 117 | `_axisMap[right.ArmY] = 22` (REAR PICKER_Y) |
| 118~122 | RightArm Pickers[0..3]: T = 23+i*2 (23/25/27/29), Z = 24+i*2 (24/26/28/30) |
| 125 | `_axisMap[left.SideVisionY] = 19` (FRONT SIDE VISION_Y0) |
| 126 | `_axisMap[right.SideVisionY] = 20` (REAR SIDE VISION_Y0) |

DO 매핑 (SimulatorBridge.cs:140~146):
- LeftArm Pickers[i].VacuumOut → `Y048~Y051`
- LeftArm Pickers[i].BlowOut → `Y056~Y059`
- RightArm Pickers[i].VacuumOut → `Y064~Y067`
- RightArm Pickers[i].BlowOut → `Y072~Y075`

(코드 일치 확인 — SimulatorBridge.cs:88~156, 2026-04-30)

또한 시뮬레이터 측 `CDT320Simulator/MotionMap.cs:27~70` 의 정적 생성자에서 동일한 #00~#36 체계로 등록됨 — #08 `EJECT PIN_Z`, #19 `FRONT SIDE VISION_Y0`, #20 `REAR SIDE VISION_Y0`, #11/13/15/17 = FRONT PICKER_T0~T3, #12/14/16/18 = FRONT PICKER_Z0~Z3, #23~#30 = REAR PICKER_T0~Z3, #31~#36 = NG/GOOD BIN/INSPECTION/FEEDER/LIFTER. 핸들러측(SimulatorBridge) 과 시뮬레이터측(MotionMap) 의 축 번호 체계가 완전히 일치한다. (코드 일치 — MotionMap.cs:27~70, 2026-04-30)

---

## 8. UI 표시

- **WorkInfoTab > HeadPage**: ArmX/Y + 4 Picker 상태
- **Simulator > MODULE AXIS POSITIONS > Front Picker (11)**: #09~#19
- **Simulator > MODULE AXIS POSITIONS > Rear Picker (11)**: #21~#30, #20
- **Simulator > 3D Viewport**: PNP HEAD #1 (orange) / #2 (purple) 실시간 이동

---

## 9. 알람 코드 (코드 기준 — `[ALARM] '...' -> ...` Console 메시지)

| 메서드 | 코드 라인 | 알람 메시지 패턴 |
|---|---|---|
| `PickerComponent.PickupAsync` | 322~324 | `Pickup: PickerZ 하강 실패` |
| `PickerComponent.PlaceAsync` (하강) | 380 | `Place: PickerZ 하강 실패` |
| `PickerComponent.PlaceAsync` (상승) | 394 | `Place: PickerZ 상승 복귀 실패` |
| `PickerComponent.MoveToFocusAsync` | 345 | `MoveToFocus: PickerZ 이동 실패` |
| `PickerComponent.MoveToWaitAsync` | 362 | `MoveToWait: PickerZ 이동 실패` |
| `TpuArmUnit.InspectBottomVisionAsync` | 543~547 | `InspectBottom: Arm 이동 실패 (PickerN)` |
| `TpuArmUnit.InspectBottomVisionAsync` | 567~571 | `InspectBottom: Expose End 수신 실패 (PickerN)` |
| `TpuArmUnit.InspectBottomVisionAsync` | 588 | `InspectBottom: 결과 수신 실패` |
| `TpuArmUnit.InspectSideVisionAsync` | 659~663 | `InspectSide: SideN 위치 이동 실패 (PickerN)` |
| `TpuArmUnit.InspectSideVisionAsync` | 749~755 | `InspectSide: 90도 회전 또는 Side3 위치 이동 실패` |
| `TpuArmUnit.InspectSideVisionAsync` | 681~683 등 | `InspectSide: SideN Expose End 실패 (PickerN)` |
| `TpuArmUnit.InspectSideVisionAsync` | 827~829 | `InspectSide: Side 결과 수신 실패 (PickerN)` |
| `TpuArmUnit.PlaceDiesAsync` | 871 | `PlaceDies: Place 위치 이동 실패` |
| `TpuArmUnit.PlaceDiesAsync` | 888~890 | `PlaceDies: 배출 실패 (PickerN)` |

---

## 10. 사이클 검증

`LOT-20260429-162418` 발췌:
| 시각 | 라인 | TPU 메서드 |
|---|---|---|
| 16:25:03.862 | `[VISION] Side Picker1 ok=True` | InspectSideVisionAsync 결과 |
| 16:25:05.983 | `[BinVision:5103] RX: PASS` | Step 9 Bin Vision |
| 16:25:08 | `[DIE 3/8] GOOD bin=1` | Step 13 결과 분류 |
