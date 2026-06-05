# CDT-320 InputStage 시퀀스 상세 분석

**문서 버전**: 1.0
**작성일**: 2026-04-29
**대상 코드**: `Equipment/InputStageUnit.cs` (Stage 58)

---

## 1. 개요

InputStage는 **카세트 ↔ 픽업 사이의 가공 스테이지**다. 피더가 들고 온 웨이퍼를 받아 테이프 텐션을 잡고, 비전으로 원점을 수립하고, 다이 단위로 픽커가 픽업할 수 있게 위치를 제공한다.

```
          ┌─── LoadAndPrepareWaferAsync ─── 웨이퍼 받음 + 텐션 + 바코드
          │
          ├─── VisionAlignAndSetupOriginAsync ── Theta + Pitch + Origin
          │
          ├─── WaitForUserConfirmAsync ── 사용자 컨펌 (옵션)
          │
          ├─── MultiScanAndPickupAsync ── 다이 단위 비전+픽업 (파이프라인)
          │
          └─── UnloadWaferAsync ── 텐션 해제 + 교체 요청
```

---

## 2. 하드웨어 구성

| 종류 | 컴포넌트 | 역할 |
|---|---|---|
| Axis | `StageY` | 다이 행(Row) 정렬 — 전후 이동 |
| Axis | `StageT` | 웨이퍼 각도 보정 — Theta 회전 |
| Axis | `ExpanderZ` | 테이프 텐션 — Down(고정) / Up(해제) |
| Axis | `CameraX` | 다이 열(Col) 정렬 — 카메라 X |
| Axis | `NeedleBlockX` | 니들 블럭 X — 픽업 X 정렬 |
| Axis | `NeedleZ` | 이젝트 니들 Z — 다이 분리 |
| Axis | `EjectPinZ` *(Stage 44 추가)* | Eject Pin Z — Sim axis 8 호환 |
| DO | `NeedleVacuum` | 테이프 흡착 |

**SoftLimit 확장** (Stage 28):
```
StageY.Setup.SoftLimitPlus       = 350.0
CameraX.Setup.SoftLimitPlus      = 350.0
StageT.Setup.SoftLimitPlus       = 360.0   /  SoftLimitMinus = -360.0
NeedleBlockX.Setup.SoftLimitPlus = 250.0
```

**외부 인터페이스 (생성자 주입)**:
- `IWaferLoader Loader` — 피더 안전 위치 게이트
- `IBarcodeReader Barcode` — Wafer ID 취득
- `IVisionTcpClient Vision` — 비전 PC 통신
- `IWaferMapHandler MapHandler` — 맵 파싱/UI 송신
- `ITransferPickerUnit Tpu` — 픽업 동기화

---

## 3. 시퀀스 단계별 상세

### 3.1 `LoadAndPrepareWaferAsync()` — 자재 로딩

```
Step 1. Loader.IsFeederAtSafePosition  ─── false ⇒ return false (ExpanderZ 하강 금지)
            │
Step 2. ExpanderZ.MoveAbsolute(ExpanderDownPosition=50mm)   // 테이프 텐션 확보
            │  IsAlarm ⇒ return false
            ▼
Step 3. waferId = Barcode.ReadAsync(BarcodeReadTimeoutMs=3000ms)
            │  null/empty ⇒ return false
            ▼
Step 4. mapData = MapHandler.ParseMapAsync(waferId)
            │  null ⇒ return false
            ▼
        CurrentWaferMap = mapData
        MapHandler.SendMapToUi(mapData)
            ▼
        return true
```

**핵심**: Step 1 의 인터락은 `WaferLoaderAdapter.IsFeederAtSafePosition` 을 통해 **FeederY 위치 (≤30 또는 ≥140)** 으로 판정. 위험 구간(30~140) 에서는 ExpanderZ 하강이 절대 시작되지 않는다 → 피더-스테이지 충돌 방지.

### 3.2 `VisionAlignAndSetupOriginAsync()` — 원점 수립

```
Pre-cond: CurrentWaferMap != null

Phase A. Theta 보정 (반복)
   centerRow, centerCol = map 중앙
   MoveToDieAsync(centerRow, centerCol, useEstimate=true)
       │
       └─[ for iter in 0..MaxAlignIterations(=3) ]
           │
           Vision.TriggerAlignAsync("Center") → VisionAlignResult
              │  null ⇒ return false
              ▼
           StageT.MoveRelativeAsync(dTheta, AlignVelocity=30mm/s)
              │
              └─ |dTheta| < AlignConvergenceThresholdDeg(=0.005°) ⇒ break

Phase B. Ref1 좌표 측정
   MoveToDieAsync(map.Ref1Row, map.Ref1Col, useEstimate=true)
   ref1Result = Vision.TriggerAlignAsync("Ref1")
   ref1X = CameraX.ActualPosition + ref1Result.DeltaX
   ref1Y = StageY.ActualPosition  + ref1Result.DeltaY

Phase C. Ref2 좌표 측정 (동일 방식)

Phase D. 피치 계산
   PitchX = (ref2X - ref1X) / colSpan      ← ref X 좌표 같으면 Recipe.DefaultPitchX(0.15) fallback
   PitchY = (ref2Y - ref1Y) / rowSpan

Phase E. Origin 확정
   OriginX = ref1X - (Ref1Col × PitchX)
   OriginY = ref1Y - (Ref1Row × PitchY)
```

이후 모든 다이 좌표는 `(OriginX + col×PitchX, OriginY + row×PitchY)` 로 계산된다.

### 3.3 `WaitForUserConfirmAsync()` — 사용자 컨펌

`TaskCompletionSource<UserConfirmResult>` 메커니즘으로 UI 스레드 컨펌 대기:
```
_confirmTcs = new TCS()         // 시퀀스 스레드: 대기 시작
                                // (UI 스레드: ConfirmFromUi(result) 호출)
result = await _confirmTcs.Task // 자동 unblock
   │
   ├── result.AngleOffset != 0 ⇒ StageT.MoveRelative(AngleOffset)
   ├── result.StartOffsetX/Y != 0 ⇒ Origin 보정
   └── return result
```

**용도**: 자동 얼라인 후 사용자가 수동으로 미세조정. Recipe `AlignConfirmEnable=true` 일 때만 호출.

### 3.4 `MultiScanAndPickupAsync(startDieIndex)` — 핵심 픽업 파이프라인

이 메서드는 **CDT-320의 가장 중요한 처리량 최적화** 가 들어가 있다. 비전 분석 시간을 모션 이동 시간에 감추는 **2-Phase 파이프라인**.

```
totalDies = map.RowCount × map.ColumnCount
batchSize = Tpu.PickerCount (기본 1, Multi-Picker 모드 시 2~4)

while dieIndex < totalDies:
   batchStart = dieIndex
   batchEnd   = min(dieIndex + batchSize, totalDies)

   ┌─ Phase A: 배치 단위 비전 SCAN ─────────────────────┐
   │ for i in batchStart..batchEnd-1:                    │
   │   if not map.DieMap[row,col]: skip (NG die)         │
   │                                                     │
   │   StageY.MoveAbsolute(targetY) ║ 동시               │
   │   CameraX.MoveAbsolute(targetX) ║                   │
   │   ↓                                                 │
   │   Vision.TriggerExposeAsync(i)                      │
   │     ↳ Expose 완료만 받고 결과 안 기다림              │
   │     ↳ 즉시 다음 die 위치로 이동 (백그라운드 분석)   │
   └─────────────────────────────────────────────────────┘

   ┌─ Phase B: 배치 단위 PICKUP ────────────────────────┐
   │ for i in batchStart..batchEnd-1:                    │
   │   inspOk = Vision.GetResultAsync(i, 5000ms)         │
   │     ↳ Phase A 에서 백그라운드 처리된 결과 회수       │
   │   if not inspOk: skip                               │
   │                                                     │
   │   if not Tpu.IsPickerReady: skip                    │
   │                                                     │
   │   pickX = OriginX + col×PitchX + PickerOffsetX(0)   │
   │   pickY = OriginY + row×PitchY + PickerOffsetY(3)   │
   │                                                     │
   │   StageY.MoveAbsolute(pickY) ║ 동시                 │
   │   NeedleBlockX.MoveAbsolute(pickX) ║                │
   │   ↓                                                 │
   │   ExecutePickupAsync(i)                             │
   │     1. NeedleVacuum.On                              │
   │     2. Tpu.NotifyPickReady(dieIndex)                │
   │     3. NeedleZ.MoveAbsolute(EjectPosition=5mm)      │
   │     4. Tpu.WaitPickerUpAsync(PickerUpTimeoutMs)     │
   │     5. NeedleZ.MoveAbsolute(DownPosition=0)         │
   │     6. NeedleVacuum.Off                             │
   └─────────────────────────────────────────────────────┘

   dieIndex = batchEnd
```

**처리량 이득**:
- Phase A: N 다이 × 이동시간 + Expose(빠름)
- Phase B: N 다이 × 픽업 시간 (그동안 Phase A 의 비전 결과 분석 완료)
- **Vision 분석 시간 = 0** (모션 이동 시간에 감춤)

**ExecutePickupAsync 6-Step 코드 검증** (코드 일치 확인 — `InputStageUnit.cs:850~894`, 2026-04-30):

| Step | 동작 | 코드 라인 |
|---|---|---|
| 1 | `NeedleVacuum.On()` + `NeedleVacuumSettleMs` 안정화 대기 | InputStageUnit.cs:853~854 |
| 2 | `Tpu.NotifyPickReady(dieIndex)` (TPU 픽업 가능 신호) | InputStageUnit.cs:857 |
| 3 | `NeedleZ.MoveAbsoluteAsync(NeedleEjectPosition, NeedleVelocity)` | InputStageUnit.cs:860 |
| 4 | `Tpu.WaitPickerUpAsync(PickerUpTimeoutMs)` (픽커 상승 완료 대기) | InputStageUnit.cs:870 |
| 5 | `NeedleZ.MoveAbsoluteAsync(NeedleDownPosition, NeedleVelocity)` | InputStageUnit.cs:883 |
| 6 | `NeedleVacuum.Off()` | InputStageUnit.cs:892 |

부가 처리: Step 3 후 `NeedleZ.IsAlarm` 체크 (라인 862), Step 4 타임아웃 시 안전을 위해 NeedleZ 하강 + 진공 해제 (라인 877~878), Step 5 후에도 `NeedleZ.IsAlarm` 체크 (라인 885).
**메서드명 보정 (Mismatch 보완 2026-04-30)**: 기존 문서의 `Tpu.SignalPickReady()` / `Tpu.WaitPickerUp()` 표기는 실제 코드에서 `Tpu.NotifyPickReady(dieIndex)` / `Tpu.WaitPickerUpAsync(timeoutMs)` 로 정정됨.

### 3.5 `UnloadWaferAsync()` — 자재 언로드

```
Step 1. StageY.MoveAbsolute(UnloadPositionY=0mm)
            │  IsAlarm ⇒ return false
            ▼
Step 2. ExpanderZ.MoveAbsolute(ExpanderUpPosition=0mm)   // 텐션 해제
            │  IsAlarm ⇒ return false
            ▼
Step 3. OnWaferChangeRequested()                          // 이벤트 발화
        CurrentWaferMap = null
            ▼
        return true
```

`WaferChangeRequested` 이벤트를 상위 `MachineController` 또는 `OutputStage` 가 구독해 다음 카세트 슬롯 진입을 트리거할 수 있다 (현재는 단일 카세트 사이클이라 미사용).

---

## 4. 인터락 정리

| 인터락 | 검사 시점 | 위반 시 동작 |
|---|---|---|
| `Loader.IsFeederAtSafePosition` | LoadAndPrepare Step 1 | return false (ExpanderZ 하강 금지) |
| `ExpanderZ.IsAlarm` | LoadAndPrepare Step 2 / Unload Step 2 | return false |
| Barcode 읽기 실패 | LoadAndPrepare Step 3 | return false |
| `MapHandler.ParseMapAsync` 실패 | LoadAndPrepare Step 4 | return false |
| `CurrentWaferMap == null` | VisionAlign / MultiScan 진입 | return false |
| `StageY/CameraX.IsAlarm` | VisionAlign / MultiScan 매 이동 | return false |
| `Vision.TriggerAlignAsync` null | VisionAlign 매 반복 | return false |
| `Tpu.IsPickerReady == false` | MultiScan Phase B | 다이 1개 skip (전체 중단 X) |
| `Vision.GetResultAsync` false | MultiScan Phase B | 다이 1개 skip |

---

## 5. Sim 매핑

| 핸들러 측 | Sim Axis No | Sim Name | Stroke |
|---|---|---|---|
| `StageY` | 2 | WAFER STAGE_Y | 400 |
| `StageT` | 3 | WAFER STAGE_T | 360 |
| `ExpanderZ` | 4 | WAFER EXPANDING_Z | 100 |
| `CameraX` | 5 | ALIGN VISION_X | 300 |
| `NeedleBlockX` | 6 | NEEDLE_X | 200 |
| `NeedleZ` | 7 | NEEDLE_Z | 100 (Brake) |
| `EjectPinZ` *(Stage 44)* | 8 | EJECT PIN_Z | 50 |

---

## 6. UI 표시

- **WorkInfoTab > InputStagePage**: 6 axes 위치 + ExpanderZ 상태 + 컨펌 버튼
- **Simulator > MODULE AXIS POSITIONS > InputStage (7)**: #02~#08 실시간 / 100ms 갱신

---

## 7. 알람 코드

| 코드 | 발생 위치 | 의미 |
|---|---|---|
| `IS-FEEDER` | LoadAndPrepare | 피더 안전 위치 위반 |
| `IS-EXPZ` | LoadAndPrepare/Unload | ExpanderZ 알람 |
| `IS-BARCODE` | LoadAndPrepare | 바코드 읽기 실패 |
| `IS-MAP` | LoadAndPrepare | 맵 파싱 실패 |
| `IS-ALIGN` | VisionAlign | 비전 통신 실패 또는 StageT 알람 |
| `IS-MOVE` | MultiScan | 스캔/픽업 위치 이동 실패 |

(*알람 코드 — 현재 코드에는 일부 누락. AlarmManager.Raise 호출 추가 권장 — §10 mismatch 참조*)

---

## 8. 통합 흐름에서의 위치

```
MachineController.CycleRunAsync(N)
   │
   ├── LoadNextWaferAsync()
   │     ├── InputStage.LoadAndPrepareWaferAsync()  ← 본 문서
   │     ├── InputLoader.RetractFeederAsync()
   │     └── InputStage.VisionAlignAndSetupOriginAsync()  ← 본 문서
   │
   ├── for die in 0..N-1:
   │     └── PickAndPlaceDieAsync(die)
   │           ├── MoveInputStageToDieAsync(row, col)   ← Origin/Pitch 사용
   │           ├── ExecutePickupAsync (NeedleZ Eject + TPU)  ← 본 문서 §3.4
   │           ├── TPU 비전 + Place ...
   │           └── ...
   │
   └── UnloadInputStageWaferAsync()
         └── InputStage.UnloadWaferAsync()  ← 본 문서
```

---

## 9. 사이클 검증

`LOT-20260429-162418` (8 die):
| 시각 | 라인 | InputStage 메서드 |
|---|---|---|
| 00:11 | `[LOTPORT] InputStage handoff (LoadAndPrepare) 시작` | LoadAndPrepareWaferAsync |
| 00:13 | `[INPUTSTAGE] VisionAlign 시작` | VisionAlignAndSetupOriginAsync |
| 00:13 | `[INPUTSTAGE] VisionAlign OK` | (Origin/Pitch 확정) |
| 00:14~01:07 | 8회 반복 `[VISION] BottomInspection PASS` | MoveInputStageToDieAsync + Eject |
| 01:07 | `[INPUTSTAGE] UnloadWafer 시작` | UnloadWaferAsync |
| 01:08 | `[INPUTSTAGE] UnloadWafer OK` | (사이클 종료) |

---

## 10. 코드-문서 Mismatch (수정 필요)

### 10.1 알람 코드 코드 발생 누락 (Mismatch 보완 2026-04-30)

현재 `InputStageUnit.cs` 의 모든 알람 분기는 `Console.WriteLine($"[ALARM] ...")` 만 출력하며, `AlarmManager.Raise(code, severity, message)` 호출이 누락되어 있다. UI 알람 패널/HMI 로그 연동 및 알람 이력 보존을 위해 아래 6개 코드의 발생 지점에 `AlarmManager.Raise` 호출 추가가 필요하다.

| 코드 | 등급 | 발생 위치 | 코드 라인 | 현재 처리 | TODO |
|---|---|---|---|---|---|
| `IS-FEEDER` | Warning | `LoadAndPrepareWaferAsync` Step 1 — `Loader.IsFeederAtSafePosition == false` | InputStageUnit.cs:324~329 | Console.WriteLine 만 (라인 326~328) | AlarmManager.Raise 적용 필요 (현재 Console.WriteLine 만) |
| `IS-EXPZ` | Error | `LoadAndPrepareWaferAsync` Step 2 — `ExpanderZ.IsAlarm` (Down 이동 후) | InputStageUnit.cs:336~340 | Console.WriteLine 만 (라인 338) | AlarmManager.Raise 적용 필요 (현재 Console.WriteLine 만) |
| `IS-EXPZ` | Error | `UnloadWaferAsync` Step 2 — `ExpanderZ.IsAlarm` (Up 이동 후) | InputStageUnit.cs:774~778 | Console.WriteLine 만 (라인 776) | 위와 동일. UnloadWafer 분기에도 추가 필요 |
| `IS-BARCODE` | Warning | `LoadAndPrepareWaferAsync` Step 3 — Barcode `null/empty` | InputStageUnit.cs:346~350 | Console.WriteLine 만 (라인 348) | AlarmManager.Raise 적용 필요 (현재 Console.WriteLine 만) |
| `IS-MAP` | Warning | `LoadAndPrepareWaferAsync` Step 4 — `MapHandler.ParseMapAsync` null 반환 | InputStageUnit.cs:357~362 | Console.WriteLine 만 (라인 359~361) | AlarmManager.Raise 적용 필요 (현재 Console.WriteLine 만) |
| `IS-ALIGN` | Warning | `VisionAlignAndSetupOriginAsync` — Center 비전 통신 실패 (`Vision.TriggerAlignAsync` null) | InputStageUnit.cs:420~425 | Console.WriteLine 만 (라인 422~423) | AlarmManager.Raise 적용 필요 (현재 Console.WriteLine 만) |
| `IS-ALIGN` | Warning | `VisionAlignAndSetupOriginAsync` — StageT 알람 (수렴 이동 실패) | InputStageUnit.cs:436~440 | Console.WriteLine 만 (라인 438) | 위와 동일 |
| `IS-ALIGN` | Warning | `VisionAlignAndSetupOriginAsync` — Ref1 촬상 실패 | InputStageUnit.cs:456~460 | Console.WriteLine 만 (라인 458) | 위와 동일 |
| `IS-ALIGN` | Warning | `VisionAlignAndSetupOriginAsync` — Ref2 촬상 실패 | InputStageUnit.cs:470~474 | Console.WriteLine 만 (라인 472) | 위와 동일 |
| `IS-MOVE` | Error | `MultiScanAndPickupAsync` Phase A — 스캔 이동 후 `StageY/CameraX.IsAlarm` | InputStageUnit.cs:656~661 | Console.WriteLine 만 (라인 658~659) | AlarmManager.Raise 적용 필요 (현재 Console.WriteLine 만) |
| `IS-MOVE` | Error | `MultiScanAndPickupAsync` Phase B — 픽업 이동 후 `StageY/NeedleBlockX.IsAlarm` | InputStageUnit.cs:715~720 | Console.WriteLine 만 (라인 717~718) | 위와 동일 |
| `IS-MOVE` | Error | `ExecutePickupAsync` — `NeedleZ.IsAlarm` (상승/하강 실패) | InputStageUnit.cs:862~866, 885~889 | Console.WriteLine 만 (라인 865, 888) | 위와 동일 |

### 10.2 메서드명/네이밍 Mismatch (보완 2026-04-30)

| 기존 문서 표기 | 실제 코드 | 비고 |
|---|---|---|
| `Tpu.SignalPickReady()` | `Tpu.NotifyPickReady(dieIndex)` | §3.4 6-Step Step 2 — `dieIndex` 인자 포함 |
| `Tpu.WaitPickerUp(PickerUpTimeoutMs)` | `Tpu.WaitPickerUpAsync(PickerUpTimeoutMs)` | §3.4 6-Step Step 4 — Async 접미사 |
| `OriginX = ref1X - (Ref1Col × PitchX)` (대문자 R) | `OriginX = ref1X - (map.Ref1Col * PitchX)` | §3.2 Phase E — 코드는 `map.Ref1Col` 필드 사용 |

### 10.3 검증 완료 항목 (Mismatch 보완 2026-04-30)

- §3.4 ExecutePickupAsync 6-Step: 코드 일치 확인 (`InputStageUnit.cs:850~894`, 2026-04-30) — §3.4 본문 표 참조.
- §3.1 LoadAndPrepareWaferAsync 4-Step: 코드 일치 확인 (`InputStageUnit.cs:321~371`, 2026-04-30).
- §3.5 UnloadWaferAsync 3-Step: 코드 일치 확인 (`InputStageUnit.cs:756~789`, 2026-04-30).
