# 알람 코드 감사 (ALARM AUDIT)

**작성일**: 2026-05-04
**대상**: AlarmMaster.cs 등록 ↔ 시퀀스 문서 04~08 정의 ↔ 코드 실사용 3원 비교
**목적**: 등록 누락 / 문서 누락 / Console.WriteLine 만 있는 채로 방치된 알람 코드 식별

---

## 1. 요약

| 분류 | 개수 |
|---|---|
| AlarmMaster.cs 등록 (`CreateDefaults()`) | 33 |
| 시퀀스 문서 04~08 에 명시 | 15 (UNIQUE: 14 — IS-EXPZ 중복) |
| 코드에서 실제 `AlarmManager.Raise()` 호출 | 25 (UNIQUE: 24, 동적 `AX-*`/`HOME-*` 제외) |
| `Console.WriteLine("[ALARM] ...")` 만 (Raise 미동반) | **다수** — 특히 `InputLoaderUnit`, `OutputUnloaderUnit`, `TransferPickerUnit`, `BaseCylinder` |

핵심 발견:
- **InputLoaderUnit / OutputUnloaderUnit / TransferPickerUnit 의 모든 실패 분기는 여전히 `Console.WriteLine` 만**. AlarmManager.Raise 0건. (단, MachineController 가 호출 결과를 받아 일부 LOT-*/OUT-* 알람으로 wrap 함.)
- **HOME-FAIL, MOVE-TIMEOUT, SERVO-OFF, LIMIT-HIT, VISION-CONN, VisionMatchFail, EXPOSE-TIMEOUT, PickFail, BottomInspFail, PlacementFail, SECS-DISCONN, SIM-DISCONN, EMG-PRESSED, VAC-LOW, CDA-LOW** 는 등록만 되어 있고 어디서도 Raise 호출 없음.
- **IS-LOAD, IS-EXCEPTION, OS-RECEIVE, OS-EXCEPTION, OS-BININSP, OS-BININSP-EX, TPU-PLACE, LOT-RET, AXL-OPEN, AXL-DLL, ALIGN-EX, E-STOP, AX-{n}, HOME-{Name}** 는 Raise 호출되지만 AlarmMaster 미등록 → UI 에 Title/Cause/Action 표시 안 됨.

---

## 2. 통합 비교 표

> 정렬: 등록 순서 → 미등록(코드 실사용) → 미등록(문서만)
> "AlarmMaster" 컬럼 ✓: `CreateDefaults()` 등록 / "문서" 컬럼 ✓: 04~08 시퀀스 문서 명시
> Console.WriteLine 위치는 동일 메서드 내 인접 라인의 `[ALARM]` 메시지를 가리킴 (Raise 가 추가되며 함께 살아 있는 경우 포함).

### 2.1 AlarmMaster 등록 코드 (33개)

| 코드 | AlarmMaster 등록 | 문서 정의 | AlarmManager.Raise 위치 | Console.WriteLine 위치 |
|---|---|---|---|---|
| HOME-FAIL | ✓ Motion/Error | — | (없음 — 동적 `HOME-{Name}` 으로만 발급, MachineController.cs:671/678) | — |
| MOVE-TIMEOUT | ✓ Motion/Error | — | (없음) | — |
| SERVO-OFF | ✓ Motion/Warning | — | (없음) | — |
| LIMIT-HIT | ✓ Motion/Error | — | (없음) | — |
| INTERLOCK | ✓ Safety/Warning | — | MachineController.cs:578 | — |
| VISION-CONN | ✓ Vision/Warning | — | (없음) | — |
| VisionMatchFail | ✓ Vision/Warning | — | (없음) | — |
| EXPOSE-TIMEOUT | ✓ Vision/Error | — | (없음) | TransferPickerUnit.cs:567~571, 681~683 (간접) |
| PickFail | ✓ Material/Warning | — | (없음) | InputStageUnit.cs:921, 949 (NeedleZ 실패 — 다른 코드로 래핑) |
| BottomInspFail | ✓ Material/Warning | — | (없음) | TransferPickerUnit.cs:588 (간접) |
| PlacementFail | ✓ Material/Warning | — | (없음) | — |
| SECS-DISCONN | ✓ Comm/Warning | — | (없음) | — |
| SIM-DISCONN | ✓ Comm/Warning | — | (없음) | — |
| EMG-PRESSED | ✓ Safety/Critical | — | (없음 — 대신 `E-STOP` 코드 사용, MachineController.cs:510) | — |
| VAC-LOW | ✓ IO/Warning | — | (없음) | — |
| CDA-LOW | ✓ IO/Error | — | (없음) | — |
| IONIZER | ✓ IO/Warning | — | IonizerSensor.cs:49 | — |
| CYCLE-EX | ✓ System/Error | — | MachineController.cs:826 | — |
| IS-FEEDER | ✓ Material/Warning | ✓ 05 (LoadAndPrepare Step 1) | InputStageUnit.cs:330 | InputStageUnit.cs (인접 동일 메서드 — 326~328) |
| IS-EXPZ | ✓ Motion/Error | ✓ 05 (Load + Unload 양쪽) | InputStageUnit.cs:345 (Load), 828 (Unload) | InputStageUnit.cs:344, 827 |
| IS-BARCODE | ✓ Material/Warning | ✓ 05 (LoadAndPrepare Step 3) | InputStageUnit.cs:360 | InputStageUnit.cs:359 |
| IS-MAP | ✓ Material/Warning | ✓ 05 (LoadAndPrepare Step 4) | InputStageUnit.cs:377 | (LoadAndPrepare Step 4 분기) |
| IS-ALIGN | ✓ Vision/Warning | ✓ 05 (VisionAlign 4분기) | InputStageUnit.cs:445, 465, 490, 509 | InputStageUnit.cs:431, 464, 489, 508 |
| IS-MOVE | ✓ Motion/Error | ✓ 05 (MultiScan A/B + ExecutePickup) | InputStageUnit.cs:701, 765, 922, 950 | InputStageUnit.cs:921, 949 |
| OS-AVOID | ✓ Safety/Error | ✓ 07 (EnsureOppositeStageAvoided) | OutputStageUnit.cs:542 | (Raise 도입으로 제거됨) |
| OS-WORKZ | ✓ Motion/Error | ✓ 07 (ReceiveDie Step 1-2) | OutputStageUnit.cs:344 | 〃 |
| OS-MOVEY | ✓ Motion/Error | ✓ 07 (ReceiveDie Step 1-3) | OutputStageUnit.cs:369 | 〃 |
| OS-PLACEDONE | ✓ Material/Warning | ✓ 07 (InspectBin Step 2-1) | OutputStageUnit.cs:653 | 〃 |
| OS-BINCAM | ✓ Motion/Error | ✓ 07 (InspectBin Step 2-2) | OutputStageUnit.cs:672 (진입), 698 (후퇴) | 〃 |
| OUT-FULL-GOOD | ✓ Material/Error | ✓ 04 / 08 | MachineController.cs:365 | — |
| OUT-FULL-NG | ✓ Material/Error | ✓ 04 / 08 | MachineController.cs:378 | — |
| OUT-STORE | ✓ Material/Error | ✓ 08 | MachineController.cs:391 | — |
| OUT-STORE-EX | ✓ System/Error | ✓ 08 | MachineController.cs:420 | — |

### 2.2 미등록인데 코드에서 Raise 호출 중 (UI Title/Cause/Action 누락)

| 코드 | AlarmMaster 등록 | 문서 정의 | AlarmManager.Raise 위치 | Console.WriteLine 위치 |
|---|---|---|---|---|
| **AXL-OPEN** | ✗ | — | AjinSystem.cs:29, 54 | — |
| **AXL-DLL** | ✗ | — | AjinSystem.cs:47 | — |
| **AX-{AxisNo}** (동적) | ✗ | — | AjinAxis.cs:174 (서보 알람 비트) | — |
| **HOME-{ax.Name}** (동적) | ✗ | — | MachineController.cs:671, 678 | — |
| **E-STOP** | ✗ (`EMG-PRESSED` 으로 등록됨 — 코드 불일치) | — | MachineController.cs:510 | — |
| **ALIGN-EX** | ✗ | — | MachineController.cs:641 | — |
| **LOT-NOCASS** | ✗ | ✓ 04 (LoadNextWafer 진입) | MachineController.cs:129, 451 | InputLoaderUnit.cs:190 |
| **LOT-SCAN** | ✗ | ✓ 04 (ScanCassette 실패) | MachineController.cs:142 | InputLoaderUnit.cs:203, 217 |
| **LOT-MOVE** | ✗ | ✓ 04 (MoveToTargetSlot 예외) | MachineController.cs:171 | (InputLoaderUnit 내 4건 분기) |
| **LOT-EX** | ✗ | ✓ 04 (MoveToExchange 실패) | MachineController.cs:180 | InputLoaderUnit.cs:331, 341, 365 |
| **LOT-RET** | ✗ | — (RetractFeeder 분기) | MachineController.cs:330 | InputLoaderUnit.cs:392, 412, 422 |
| **IS-LOAD** | ✗ | — | MachineController.cs:230 | — |
| **IS-EXCEPTION** | ✗ | — | MachineController.cs:261 | — |
| **OS-RECEIVE** | ✗ | — (07 doc 미정의 — 신규) | MachineController.cs:1168 | — |
| **OS-EXCEPTION** | ✗ | — | MachineController.cs:1177 | — |
| **OS-BININSP** | ✗ | — | MachineController.cs:1200 | — |
| **OS-BININSP-EX** | ✗ | — | MachineController.cs:1209 | — |
| **TPU-PLACE** | ✗ | — (06 doc 코드 부재) | MachineController.cs:1226 | TransferPickerUnit.cs:871 (PlaceDies 실패) |

### 2.3 문서에는 있는데 등록도 없고 코드 호출도 없는 코드

(없음. 문서 04~08 의 모든 코드는 등록 또는 실호출 둘 중 하나에는 잡힘.)

### 2.4 [ALARM] Console.WriteLine 만 있고 어떤 알람 코드와도 매핑 안 된 위치 (UI 에 보이지 않음)

| 파일 | 라인 | 메시지 | 비고 |
|---|---|---|---|
| BaseCylinder.cs | 203, 244 | 실린더 Fwd/Bwd 타임아웃 | 공통 베이스 — 코드 부여 권장 (`CYL-TIMEOUT` 등) |
| InputLoaderUnit.cs | 190, 203, 217, 331, 341, 365, 392, 412, 422 | Cassette/ElevatorZ/FeederY/Clamp 실패 9건 | MachineController 가 LOT-* 로 래핑하나, Unit 자체엔 Raise 없음 |
| OutputUnloaderUnit.cs | 480, 489, 498, 521, 557, 566, 575, 597, 606 | Pickup/Place 6-Step 실패 9건 | MachineController 가 OUT-STORE/OUT-STORE-EX 로 래핑 |
| TransferPickerUnit.cs | 323, 345, 362, 380, 394, 543~547, 567~571, 588, 659~663, 681~683, 749~755, 827~829, 871, 888~890 | Pickup/Place/MoveToFocus/Wait + InspectBottom/Side + PlaceDies 실패 14+건 | TPU 전용 코드(`TPU-PICK`, `TPU-PLACE-Z`, `TPU-INSPB`, `TPU-INSPS`) 부여 권장 |

---

## 3. 권장 조치

| 우선 | 조치 | 영향 코드 |
|---|---|---|
| **P1** | 등록 코드명 통일 — `EMG-PRESSED` ↔ `E-STOP` 중 하나로 정리 | EMG-PRESSED / E-STOP |
| **P1** | 실호출 14개 코드를 `AlarmMaster.CreateDefaults()` 에 추가 (UI Title/Cause/Action) | AXL-OPEN, AXL-DLL, ALIGN-EX, LOT-NOCASS, LOT-SCAN, LOT-MOVE, LOT-EX, LOT-RET, IS-LOAD, IS-EXCEPTION, OS-RECEIVE, OS-EXCEPTION, OS-BININSP, OS-BININSP-EX, TPU-PLACE |
| **P2** | 동적 prefix 코드(`AX-`, `HOME-`)는 마스터에 일반 정의 + UI 에서 prefix 매칭 fallback 처리 | AX-*, HOME-* |
| **P2** | TPU/InputLoader/OutputUnloader 의 `Console.WriteLine` Raise 호출로 승격 (현재 MachineController 만 알람 발급 — Unit 단위 추적 어려움) | 약 30+ 위치 |
| **P3** | 등록만 되고 Raise 미사용 13개 코드는 (a) 사용 예정이면 호출 추가, (b) 미사용이면 마스터에서 삭제 | HOME-FAIL, MOVE-TIMEOUT, SERVO-OFF, LIMIT-HIT, VISION-CONN, VisionMatchFail, EXPOSE-TIMEOUT, PickFail, BottomInspFail, PlacementFail, SECS-DISCONN, SIM-DISCONN, VAC-LOW, CDA-LOW |
| **P3** | 문서 06 (TransferPicker) 에 알람 코드 컬럼 신설 (현재 `[ALARM]` 텍스트 매칭만 가능) | (문서 작업) |
| **P3** | 문서 07 — `OS-AVOIDZ` (MoveToAvoidPositionAsync 실패, OutputStageUnit.cs:322 [ALARM]) 정식 추가 검토 | OS-AVOIDZ (신규) |

---

## 부록 A — 검증 방법

1. AlarmMaster: `D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs` `CreateDefaults()` 직접 카운트.
2. 문서: `docs/04~08_*.md` 에 정규식 `\b(IS|OS|OUT|LOT|TPU|AXL|AX)-[A-Z][A-Z0-9-]+\b` grep.
3. Raise 호출: `D:\Work\CDT-320` 전체에서 `AlarmManager\.Raise\(` grep, 6개 파일.
4. Console.WriteLine: `Console\.WriteLine.*\[ALARM\]` grep, 5개 파일에서 30+ 위치.

> 본 감사는 코드 read 시점(2026-05-04) 기준이며, MachineController.cs 의 LOT/OS 분기가 동적으로 추가된 코드(`HOME-{Name}` 등)는 prefix 만 카운트.
