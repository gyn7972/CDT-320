# CDT-320 OutputStage 시퀀스 상세 분석

**문서 버전**: 1.0
**작성일**: 2026-04-29
**대상 코드**: `Equipment/OutputStageUnit.cs` (Stage 58)

---

## 1. 개요

OutputStage는 TPU가 가져온 다이를 **Good / NG 등급별로 분류**하여 정확한 Bin 위치에 안착시키는 단계다. 두 개의 독립 스테이지(GoodStage / NgStage)가 좌우 충돌 회피 인터락을 공유하며, BinCamera로 안착 검사까지 수행한다.

```
   TPU Place            ┌──────────────────────────┐
   (Front/Rear Arm)     │ OutputStage              │
       │                │  ├── GoodStage (StageY/Z)│
       ▼                │  │     └── (Good 다이)   │
   ReceiveDie ────────► │  │                       │
   (Grade 분류)         │  └── NgStage  (StageY/Z) │
                        │        └── (NG 다이)     │
                        │                          │
                        │ BinCameraX ──── 안착 검사 │
                        └──────────────────────────┘
                              │
                              ▼
                       OutputUnloader 적재
```

---

## 2. 하드웨어 구성 (3 모듈)

### 2.1 GoodStage / NgStage (StageModule × 2)

| 종류 | 컴포넌트 | 역할 |
|---|---|---|
| Axis | `StageY` | 다이 안착 좌표 정렬 (Bottom Vision Y 오프셋 보정) |
| Axis | `StageZ` | Work(상승) ↔ Avoid(하강 — 충돌 회피) |

**메서드**:
- `MoveToWorkPositionAsync()` — StageZ 상승 (수신 가능)
- `MoveToAvoidPositionAsync()` — StageZ 하강 (반대편 작업 시)
- `MoveYAsync(targetY)` — StageY 절대 이동

### 2.2 BinCameraX (단일 Axis)

다이가 Place 된 후 **양쪽 스테이지 모두를 검사** 할 수 있는 공용 카메라. WorkPositionX(검사) ↔ RetractPositionX(후퇴) 두 위치만 사용.

### 2.3 외부 인터페이스

- `ITpuUnit Tpu` — Place 완료/픽커 후퇴 신호 수신 (`WaitPlaceDoneAsync`, `NotifyReadyForNext`)

---

## 3. 사이클 1 다이 처리 — 시퀀스

### 3.1 `ReceiveDieAsync(ReceiveDieRequest request)` — Step 1

```
target = (request.Grade == Good) ? GoodStage : NgStage

Step 1-1. [충돌 회피 인터락]
   EnsureOppositeStageAvoidedAsync(target)
       │  반대 스테이지 StageZ가 AvoidPositionZ에 없으면 강제 하강
       │  실패 ⇒ return false
       ▼
Step 1-2. target.MoveToWorkPositionAsync()
       │  StageZ 상승 (받을 준비)
       │  IsAlarm ⇒ return false
       ▼
Step 1-3. finalY = StageBasePositionY + TpuOffsetY + VisionOffsetY
   target.MoveYAsync(finalY)
       │  IsAlarm ⇒ return false
       ▼
Step 1-4. Tpu.NotifyPlaceReady()
       │  TPU에 "Place 시작 가능" 신호 전송
       │  TPU는 PickerZ 하강 → Vacuum Off → Blow 펄스 → PickerZ 상승
       ▼
   return true
```

**핵심 안전성**: Step 1-1 의 `EnsureOppositeStageAvoidedAsync` — 반대편 스테이지가 작업 위치(상승)에 있으면 BinCamera나 TPU 픽커와 충돌. **반드시 하강시켜야** Step 1-2 의 타겟 상승이 안전.

### 3.2 `InspectBinPositionAsync()` — Step 2

```
Step 2-1. Tpu.WaitPlaceDoneAsync(TpuPlaceDoneTimeoutMs)
       │  Place + 픽커 후퇴까지 대기
       │  timeout ⇒ return false
       ▼
Step 2-2. BinCameraX.MoveAbsolute(BinCameraWorkPositionX, BinCameraVelocity)
       │  IsAlarm ⇒ return false
       ▼
Step 2-3. (실 모드) Vision.TriggerExpose + GetResult
          (Sim 모드) Task.Delay(20ms) + 항상 OK
       │  검사 NG ⇒ 알람
       ▼
Step 2-4. BinCameraX.MoveAbsolute(BinCameraRetractPositionX)
       │  TPU 다음 다이 받기 위해 즉시 후퇴
       ▼
Step 2-5. Tpu.NotifyReadyForNext()
       │  TPU에 "다음 다이 받을 준비 완료" 신호
       ▼
   return true
```

### 3.3 `PerformColletCleaningAsync()` — 주기적 클리닝

`MachineController.DiesPerColletClean(=24)` 다이마다 호출. PickerZ를 클리닝 위치로 이동해 콜렛 청소를 수행한다.
```
StageY → CleaningPositionY
PickerZ → 클리닝 헤드 접촉 위치
DO 클리닝 활성 (블로우/진공 펄스)
PickerZ 후퇴
```

---

## 4. 충돌 회피 인터락 상세

`EnsureOppositeStageAvoidedAsync(StageModule target)`:
```
opposite = (target == GoodStage) ? NgStage : GoodStage

if opposite.StageZ.ActualPosition <= AvoidPositionZ + tolerance:
   return true   // 이미 안전
else:
   await opposite.MoveToAvoidPositionAsync()
   if opposite.StageZ.IsAlarm:
      return false
   return true
```

**왜 필요한가**: TPU가 Front Arm으로 Good 분류 시 GoodStage가 상승해야 하는데, NgStage가 상승해 있으면 BinCamera 진입 경로 또는 TPU PickerZ 하강 경로와 충돌. 따라서 한 번에 한 쪽만 상승.

---

## 5. ReceiveDieRequest DTO

```csharp
public class ReceiveDieRequest
{
    public DieGrade Grade        { get; set; }    // Good | Ng
    public double   TpuOffsetX   { get; set; }    // TPU 기구 오프셋 (보통 0)
    public double   TpuOffsetY   { get; set; }
    public double   VisionOffsetX{ get; set; }    // Bottom Vision 보정값
    public double   VisionOffsetY{ get; set; }
}
```

`MachineController.PickAndPlaceDieAsync` 에서 다음과 같이 채움:
```csharp
new ReceiveDieRequest
{
    Grade         = inspPass ? DieGrade.Good : DieGrade.Ng,
    TpuOffsetX    = 0,
    TpuOffsetY    = 0,
    VisionOffsetX = bottomVisionOffsets[0].OffsetX,   // 픽커1 기준
    VisionOffsetY = bottomVisionOffsets[0].OffsetY
}
```

---

## 6. Sim 매핑

| 핸들러 측 | Sim Axis No | Sim Name |
|---|---|---|
| `NgStage.StageY` | 31 | NG BIN_Y (500mm) |
| `NgStage.StageZ` | 32 | NG BIN_Z (100mm) |
| `GoodStage.StageY` | 33 | GOOD BIN_Y (500mm) |
| `BinCameraX` | 34 | INSPECTION VISION_X (300mm) |

> 참고: Stage 58 시점 Sim에는 `GoodStage.StageZ` 가 별도 등록되지 않음. NG/Good StageZ 충돌 회피는 핸들러 측 모션만 검증되고 Sim 시각화에는 반영되지 않음. `OutputStage` 4축 표기 = StageY×2 + StageZ + BinCameraX.

---

### 6.1 Config 파라미터 (Mismatch 보완 2026-04-30)

`OutputStageConfig` / `OutputStageRecipe` 의 파라미터는 §3 시퀀스 곳곳에서 타임아웃·속도 기준으로 사용되지만 본문 표에 누락되어 있었다. 코드 대조 결과는 다음과 같다.

| 파라미터 | 타입 | 기본값 | 설명 | 라인 |
|---|---|---|---|---|
| TpuPlaceDoneTimeoutMs | int | 3000 | TPU Place 완료 대기 (ms). InspectBin Step 2-1 에서 사용 | OutputStageUnit.cs:216 |
| WaferChangeTimeoutMs | int | 0 (무한) | 웨이퍼 교체 완료 대기 (ms). RequestWaferChangeAsync Step 3-4 에서 사용 | OutputStageUnit.cs:219 |
| ColletCleaningTimeoutMs | int | 10000 | 콜렛 크리닝 완료 대기 (ms). PerformColletCleaningAsync Step 4-4 에서 사용 | OutputStageUnit.cs:222 |
| BinCameraVelocity | double | 200.0 | BinCameraX 이동 속도 (mm/s). InspectBin Step 2-2 / 2-4 에서 사용 (Recipe) | OutputStageUnit.cs:229 |

### 6.2 SoftLimit 확장 (Stage 30, Mismatch 보완 2026-04-30)

기본 SoftLimitPlus(=200) 가 OutputStage 작업 좌표(Base 50 + Vision Offset)에 비해 너무 좁아 Stage 30 에서 확장됨. 생성자에서 직접 덮어쓰는 방식으로, Setup/Recipe 에는 노출되지 않는다.

| 축 | SoftLimitPlus | 코드 라인 |
|---|---|---|
| StageY (GoodStage / NgStage 공통, `StageModule` 생성자) | 350.0 | OutputStageUnit.cs:283 |
| StageZ (GoodStage / NgStage 공통, `StageModule` 생성자) | 250.0 | OutputStageUnit.cs:284 |
| BinCameraX (`OutputStageUnit` 생성자) | 350.0 | OutputStageUnit.cs:466 |

> 주: GoodStage/NgStage 양측 StageY/StageZ 는 동일 `StageModule` 생성자를 공유하므로 한 줄에서 양쪽이 동시에 적용된다 (`StageY.Setup.SoftLimitPlus = 350.0;` 등).

---

## 7. UI 표시

- **WorkInfoTab > OutputStagePage**: GoodStage Y/Z, NgStage Y/Z, BinCameraX, 다이 카운트
- **Simulator > MODULE AXIS POSITIONS > OutputStage (4)**: #31~#34

---

## 8. 인터락 정리

| 인터락 | 검사 시점 | 위반 시 동작 |
|---|---|---|
| `EnsureOppositeStageAvoidedAsync` | ReceiveDie Step 1-1 | return false (모든 후속 차단) |
| `target.StageZ.IsAlarm` (상승 후) | ReceiveDie Step 1-2 | return false |
| `target.StageY.IsAlarm` | ReceiveDie Step 1-3 | return false |
| `Tpu.WaitPlaceDoneAsync` timeout | InspectBin Step 2-1 | return false |
| `BinCameraX.IsAlarm` | InspectBin Step 2-2 | return false |
| Bin Vision 검사 NG | InspectBin Step 2-3 | (현재 sim에선 무시 — 실 모드에서 알람 권장) |

---

## 9. 알람 코드

| 코드 | 메서드 | 의미 |
|---|---|---|
| `OS-AVOID` | EnsureOppositeStageAvoided | 반대편 스테이지 회피 실패 |
| `OS-WORKZ` | ReceiveDie Step 1-2 | StageZ 상승 알람 |
| `OS-MOVEY` | ReceiveDie Step 1-3 | StageY 이동 알람 |
| `OS-PLACEDONE` | InspectBin Step 2-1 | TPU Place 대기 타임아웃 |
| `OS-BINCAM` | InspectBin Step 2-2 | BinCameraX 알람 |

(*현재 코드에서는 일부 알람 코드 미발급 — §11 mismatch 참조*)

### 9.1 코드 미발급 알람 (Mismatch 보완 2026-04-30)

위 표의 알람 코드는 문서상의 식별자일 뿐, 실제 코드는 모두 `Console.WriteLine("[ALARM] ...")` 만 출력하고 `AlarmManager.Raise(...)` 같은 알람 등록 호출이 없다. 운영 모드 전환 시 반드시 보완해야 한다.

| 코드 | 발생 위치 | 현재 처리 (코드 라인) | TODO |
|---|---|---|---|
| OS-AVOID | EnsureOppositeStageAvoidedAsync 실패 (반대편 회피 하강 실패) | Console.WriteLine — OutputStageUnit.cs:527 | AlarmManager.Raise 적용 필요 |
| OS-WORKZ | StageZ WorkPosition 이동 IsAlarm (`StageModule.MoveToWorkPositionAsync`) | Console.WriteLine — OutputStageUnit.cs:341 | 〃 |
| OS-MOVEY | StageY 이동 IsAlarm (`StageModule.MoveYAsync`) | Console.WriteLine — OutputStageUnit.cs:360 | 〃 |
| OS-PLACEDONE | WaitPlaceDoneAsync 타임아웃 (InspectBin Step 2-1) | Console.WriteLine — OutputStageUnit.cs:633 | 〃 |
| OS-BINCAM | BinCameraX 이동 IsAlarm — 진입(Step 2-2) / 후퇴(Step 2-4) 양측 | Console.WriteLine — OutputStageUnit.cs:646, 666 | 〃 |

> 부가: `MoveToAvoidPositionAsync` 실패 시의 `[ALARM]` (OutputStageUnit.cs:322) 도 동일 패턴으로 OS-AVOIDZ 등 별도 코드 부여 검토 필요.

---

## 10. 사이클 검증

`LOT-20260429-162418`:
| 시각 | 라인 | OutputStage |
|---|---|---|
| 16:25:03 | `[OUTSTAGE] ReceiveDie OK` | ReceiveDieAsync |
| 16:25:05 | `[BinVision:5103] PASS Width=200.86` | InspectBinPositionAsync |
| 16:25:06 | `[OUTSTAGE] BinInspect OK` | InspectBin 완료 |
| 16:26:09 | `[COLLET] Cleaning OK` | PerformColletCleaningAsync (24 die마다) |
