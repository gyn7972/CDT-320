# Input / Output 시퀀스 지도

작성일: 2026-07-27

**이 문서의 목적은 "어느 파일을 열어야 하는지"를 5초 안에 정하는 것이다.**
안전 불변조건과 검증 절차는 [`SEQUENCE_SAFETY_ANALYSIS.md`](SEQUENCE_SAFETY_ANALYSIS.md)에 있다.

---

## 0. 파일 배치

| 관심사 | 파일 |
| --- | --- |
| Input 자동 사이클 제어 | `InputSequence.cs` |
| Input 스텝 [1]~[6] 로딩 | `InputSequence.Steps.Load.cs` |
| Input 스텝 [7]~[8] 정렬 | `InputSequence.Steps.Align.cs` |
| Input 스텝 [9]~[10] 사용자 확인 | `InputSequence.Steps.Review.cs` |
| Output 진입점 / 액션 실행 코어 | `OutputSequence.cs` |
| Output 다음 작업 판정 (모션 없음) | `OutputSequence.ActionPlanner.cs` |
| Output 하위 유닛 시퀀스 호출 | `OutputSequence.UnitCalls.cs` |
| Output 리소스·Picker Avoid·인터락 | `OutputSequence.Safety.cs` |
| 피더 후퇴 안전 규칙 (공통) | `Safety/FeederRetreatPolicy.cs` |
| Ready 진입 안전 확인 | `MachineReadySequence.cs` |

하위 유닛 시퀀스는 `InputCassette/`, `InputFeeder/`, `InputStage/`, `OutputCassette/`, `OutputFeeder/`, `OutputStage/` 폴더에 있다.

---

## 1. Input 자동 시퀀스

`InputSequenceAutoStep`. 디스패처는 `InputSequence.DispatchInputStepAsync`.
모든 스텝은 실행 직전 `CheckInputStepInterlocksBeforeExecute()`를 통과한다.

| # | 스텝 | 움직이는 것 | 사전조건 | 실패 알람 | 구현 |
| --- | --- | --- | --- | --- | --- |
| 1 | `Mapping` | InputLifterZ (스캔 속도) | 카세트 감지 | `SEQ-IN-STEP-MAP` | Steps.Load |
| 2 | `ResolveSlot` | 없음 (판정) | 매핑 완료 | `SEQ-IN-NO-READY-WAFER` | Steps.Load |
| 3 | `PrepareStageLoad` | InputStage 축 | Picker Avoid, `InputStageArea` 점유 | `SEQ-IN-STEP-STAGE-PREP` | Steps.Load |
| 4 | `LoadFeederFromCassette` | InputLifterZ, InputFeederY, Clamp/Lift | Picker Avoid, 슬롯 결정됨 | `SEQ-IN-STEP-FEEDER-CST` | Steps.Load |
| 5 | `LoadFeederToStage` | InputFeederY, Clamp/Lift, Stage | Picker Avoid, `InputStageArea` 점유 | `SEQ-IN-STEP-FEEDER-STAGE` | Steps.Load |
| 6 | `RecoverFeeder` | Feeder Clamp/Lift, InputFeederY | Picker Avoid, **피더 비어 있음** | `SEQ-IN-STEP-FEEDER-RECOVER` | Steps.Load |
| 7 | `AlignStage` | InputStage θ/XY, InputVisionX | Picker Avoid, `InputStageArea` 점유 | `SEQ-IN-STEP-STAGE-ALIGN` | Steps.Align |
| 8 | `DieMapping` | InputStage XY, InputVisionX | Align 결과 유효 | `SEQ-IN-STEP-STAGE-DIEMAP` | Steps.Align |
| 9 | `ReviewStage` | (작업자 수동 조작 허용) | Picker 안전, Manual 세션 진입 | `SEQ-IN-REVIEW-*` | Steps.Review |
| 10 | `Complete` | 없음 | — | — | Steps.Review |

**스텝 9 분기** — 작업자 선택에 따라 `_autoStep`이 바뀐다.

| 선택 | 다음 스텝 |
| --- | --- |
| ConfirmAndContinue | `Complete` (Camera X Avoid 복귀 후 승인 Commit) |
| RetryMapping | `DieMapping` 재실행 |
| RetryAlign (기본) | `AlignStage`부터 재실행 |
| Stop | `SequenceStopException` |

---

## 2. Output 자동 시퀀스

`OutputSequenceAutoAction`. 판정은 `OutputSequence.ActionPlanner.cs`의 `ResolveNextOutputAction()`.
Input과 달리 고정 스텝 순서가 아니라 **매 사이클 상태를 보고 액션을 고른다.**

| 액션 | 하는 일 | 움직이는 것 | 구현 |
| --- | --- | --- | --- |
| `StoreNgStageToCassette` | NG 스테이지 완료품 → 카세트 | NG Stage, Feeder, LifterZ | UnitCalls + 본체 |
| `StoreGoodStageToCassette` | GOOD 스테이지 완료품 → 카세트 | Good Stage, Feeder, LifterZ | UnitCalls + 본체 |
| `ResumeOccupiedFeeder` | 피더에 남은 Bin 처리 (재개) | Feeder, Stage 또는 카세트 | 본체 |
| `SupplyGoodCassetteToStage` | 카세트 → GOOD 스테이지 빈 Bin 공급 | LifterZ, Feeder, Good Stage | 본체 |
| `SupplyNgCassetteToStage` | 카세트 → NG 스테이지 빈 Bin 공급 | LifterZ, Feeder, NG Stage | 본체 |
| `WaitOutputStageReceiveComplete` | Picker 적재 완료 대기 | 없음 | 본체 |
| `StopNoOutputBinWork` | 작업 가능한 Bin 없음 → 정지 | 없음 | 본체 |

모든 작업 액션은 `CheckOutputWorkInterlocksBeforeExecute()`와
`ExecuteWithOutputPickerAvoidGateAsync()`를 통과한다 (둘 다 `OutputSequence.Safety.cs`).

---

## 3. 안전 전제 — 모션 전에 반드시 성립해야 하는 것

### 3-1. 피더 후퇴 (Input / Output 공통)

```
Unclamp  →  [보유 확인]  →  Lift Up  →  Y Avoid  →  Lift Down
```

- **Lift Down 상태로 Y를 움직이면 안 된다.** 스테이지에 놓인 자재를 누르고 긁는다 (2026-07-27 현장 확인).
- **자재를 문 채로 Lift Up 하면 피더가 파손된다.** Unclamp 센서 + 피더 공백을 확인하고 아니면 fail-closed.
- 단일 구현: `Safety/FeederRetreatPolicy.cs`. 호출부는 아래 표.

| 호출부 | 상황 |
| --- | --- |
| `MachineReadySequence.EnsureInputFeederReadySafetyAsync` | START 시 Input 피더 자동 복구 |
| `MachineReadySequence.EnsureOutputFeederReadySafetyAsync` | START 시 Output 피더 자동 복구 |
| `InputFeederRecoverSequence` | Input 피더 수동/시퀀스 복구 |
| `OutputFeederRecoverSequence` | Output 피더 수동/시퀀스 복구 (카세트 교체 준비 포함) |

생산 Load/Unload 시퀀스의 Lift Down은 **티칭 위치에서 수행하는 정상 동작**이라 이 규칙 대상이 아니다
(`OutputFeederLoadToStageSequence.PrepareFeederLiftDownAfterAvoid` 등).

### 3-2. Picker 이동

- PickerX 단독 이동은 **PickerY가 Avoid**여야 한다. 순서는 항상 **X/T 먼저, 그 다음 Y**.
- 셀 작업 종료 시 **Z Avoid → Y Avoid** 순서.
- PickerX는 **Input/Output 카세트 리프터가 모두 Avoid**여야 한다.
- 구현: `Picker/PickerSequenceBase.MovePickerXTThenYAndVerifyAsync`.

### 3-3. NG Stage

- NG StageY 이동 전: **NG Clamp Lift UP**, Good Guide Down, GoodStageZ Avoid
  (`EnsureNgStageYMoveClearAsync`).
- 로딩/언로딩 위치 도착 후: **Guide UP + Clamp Lift DOWN**.

### 3-4. 리소스 점유

| 리소스 | 보호 대상 |
| --- | --- |
| `InputStageArea` / `OutputGoodStageArea` / `OutputNgStageArea` | 스테이지 동시 접근 |
| `OutputPlaceArea` | Picker Place 구간 |
| `InputFeederArea` / `OutputFeederArea` | 피더 축 |
| `OutputLoaderActive` / `InputLoaderActive` | Picker 신규 공정 진입 차단 |

---

## 4. 속도 규칙

| 동작 | 속도 출처 |
| --- | --- |
| 카세트 매핑 스캔 | 레시피 Input/Output Cassette **SCAN/JOG VELOCITY** |
| 그 외 리프터 이동 (슬롯/Avoid/교체) | 축 **DefaultVelocity** |
| Manual 시퀀스 | `MotionSpeedScale.ManualSequencePercent` (재시작 후에도 유지) |
| Ready 시퀀스 | `MotionSpeedScale.ReadySequencePercent` |
| 캘리브레이션 안전 이동 | `CalibrationData.SafeMovePercent` |

---

## 5. 코드 수정 전 체크리스트

1. 이 문서에서 **어느 파일**인지 찾는다.
2. 모션을 추가/변경하면 **3장 안전 전제**에 해당하는 게 있는지 본다.
3. 안전 규칙이 이미 정책 클래스로 있으면 **복사하지 말고 호출**한다.
4. 알람 코드를 새로 만들면 이 문서 표에 추가한다.
5. 빌드는 반드시 별도 `OutDir`로. **Clean/Rebuild 금지** (`AGENTS.md`).
