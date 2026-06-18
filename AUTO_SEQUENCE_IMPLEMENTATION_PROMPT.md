# Auto Sequence 구현 작업 프롬프트 (Codex / 코딩 에이전트용)

> 이 문서는 `QMC.CDT-320` 핸들러의 **Input / Output / Front·Rear Picker** 4개 유닛 시퀀스의
> Auto(자동) 로직을 코드베이스 규칙에 맞게 구현·정리하기 위한 작업 지시 프롬프트다.
> 코딩 에이전트(Codex 등)에 이 문서 전체를 그대로 붙여 넣어 사용한다.

---

## 0. 작업 전 필수 확인

1. **코드 수정 전 `AGENTS.md`를 먼저 읽고 그 규칙을 100% 따른다.** (`CLAUDE.md`가 진입점)
2. 관련 규칙 문서: `MATERIAL_ARCHITECTURE_PLAN.md`, `SEQUENCE_RECOVERY_RULES.md`, `OUTPUT_SEQUENCE_DESIGN.md`.
3. 기존 시퀀스 아키텍처를 **재설계하지 말고 그대로 확장**한다. 아래 클래스/패턴이 이미 존재한다.

### 고정 규칙 요약 (AGENTS.md)
- 모션/시퀀스 메소드는 **비동기 `Task<int>` 반환**. 성공 `0`, 실패 `-1`/장비코드.
- **UI Thread에서 `Thread.Sleep`·무한 `while`로 시퀀스 제어 금지.** 대기는 `await Task.Delay(ms, ct)` 또는 `Bus.WaitAsync`.
- 함수는 `try / catch / finally` 기준. `catch { }`로 예외 무시 금지.
- 실패 시: 로그 + `AlarmManager.Raise(...)` (Sequence) / 메시지박스 (UI). 본 시퀀스는 **Alarm 경로**.
- 명명: 메소드는 동사 시작, bool은 `Is`/`Can`/`Check`. 컨트롤 prefix 규칙 준수.
- 모든 파일 **UTF-8 저장**. 깨진 한글 발견 시 작업 범위 내 복원, 불확실하면 사용자 확인.
- 모든 `await`에 `.ConfigureAwait(false)` 유지 (기존 코드 컨벤션).
- `OperationCanceledException`, `SequenceStopException`은 잡아서 삼키지 말고 **rethrow**.

---

## 1. 대상 파일 (이미 존재 — 여기에 작업)

| 유닛 | 진입 파일 | Auto 진입점 |
|---|---|---|
| Input | `QMC.CDT-320/Sequencing/InputSequence.cs` | `ExecuteAutoAsync` → `ExecuteInputAutoCycleAsync` |
| Output | `QMC.CDT-320/Sequencing/OutputSequence.cs` | `ExecuteAutoAsync` → `ExecuteNextOutputStepAsync` |
| Front Picker | `QMC.CDT-320/Sequencing/FrontPickerSequence.cs` | `ExecuteAutoAsync` → `PickerProcessSequence` |
| Rear Picker | `QMC.CDT-320/Sequencing/RearPickerSequence.cs` | `ExecuteAutoAsync` → `PickerProcessSequence` |
| Picker 공정 | `QMC.CDT-320/Sequencing/Picker/PickerProcessSequence.cs` | Pickup→Bottom→Side→Place 상태머신 |

### 공통 인프라 (그대로 사용)
- `Sequencing/Common/UnitSequenceBase.cs` — 모든 유닛 시퀀스 베이스. `ExecuteAutoAsync`(Auto), `ExecuteStepAsync`(Manual/Step). Step 게이트(`StepUnit`) 내장.
- `Sequencing/Common/AutoSequenceCoordinator.cs` — 활성 유닛을 **병렬 Task로 실행**, 한 유닛 실패 시 전체 취소.
- `Sequencing/Common/SequenceSignalBus.cs` — 유닛 간 핸드오프 신호 (`Set`/`Reset`/`WaitAsync`). `Context.Bus`로 접근.
- `Sequencing/Common/SequenceResourceManager.cs` + `SequenceResourceLease.cs` — 영역 점유 락. `Context.Resources.AcquireAsync(...)`, `using` 스코프로 해제.
- `Sequencing/Common/SequenceResourceKind.cs` — `InputStageArea, InputFeederArea, InspectionArea, OutputPlaceArea, OutputGoodStageArea, OutputNgStageArea, OutputFeederArea, FrontPicker, RearPicker`.
- `Equipment/Materials/MaterialStateService.cs` — 자재 위치/상태 SSOT. 웨이퍼·다이 조회의 단일 출처.
- `Equipment/Interlocks/PickerZoneInterlockRules.cs`, `PickerFront/RearInterlockRules.cs` — 픽커 기구 간섭 영역 락.

> **모니터링은 별도 스레드를 만들지 말 것.** Auto 루프 자체가 모니터링이다.
> 각 유닛은 자기 `ExecuteAutoAsync` 안에서 `MaterialStateService` 상태를 읽고
> `Bus` 신호로 다른 유닛과 동기화하며 무한 루프(`while (!ct.IsCancellationRequested)`)를 돈다.
> `ct` 취소가 유일한 정상 종료 경로다.

---

## 2. Manual / Step 모드 공유 원칙 (전 유닛 공통)

- Auto에서 검사하는 **선행 조건(맵핑 완료, 스테이지 점유, 상대 픽커 위치 등)을 Manual/Step에서도 동일하게 확인**한 뒤 자기 시퀀스를 실행해야 한다.
- 즉, 조건 확인 로직을 Auto 전용 분기에 묻지 말고 **공통 헬퍼**로 빼서 `ExecuteAutoAsync`와 `ExecuteStepAsync`가 같이 호출하게 한다.
- `UnitSequenceBase.ExecuteStepAsync`는 1스텝만 진행하고 게이트로 복귀한다. 이 패턴을 유지한다.

---

## 3. InputSequence 요구사항

**목표 사이클(Auto, 무한 반복):**

1. **맵핑 확인** — 카세트에 웨이퍼가 있고 맵핑 완료 상태인지 확인.
   - 미완료면 `InputSequenceAutoStep.Mapping`부터 시작. (`ExecuteMappingAsync`)
2. **스테이지 점유 확인** — 스테이지 위에 **몇 번 슬롯 웨이퍼가 올라가 있는지** 확인.
   - `ResolveStageWaferFromRuntimeState()` / `MaterialStateService.GetWaferAtLocation(InputStage)` 사용.
3. **스테이지가 비어 있으면** → 현재 작업해야 할 웨이퍼를 로딩.
   - 슬롯 결정: `ResolveCurrentOrNextInputSlot()` (진행 중 슬롯 우선, 없으면 다음 슬롯).
   - PrepareLoad → Feeder LoadFromCassette → Feeder LoadToStage → Recover 순. (기존 step enum 유지)
4. **스테이지에 웨이퍼가 있으면** → 얼라인·다이맵핑 완료 여부 확인.
   - 미완료면 그 지점부터 시작: `ResolveStageWaferResumeStep()`이 `AlignStage` / `DieMapping` / `Complete`를 판정.
5. **맵핑까지 완료** → 피커에게 "다이 가져가도 된다" 신호.
   - `Context.Bus.Set("InputStageReady")` + 다이맵 완료 신호(`InputStageDieMapped` / `InputStageFinishComplete`).
   - 이후 피커가 스테이지를 제어해 다이를 가져감 (Input 시퀀스는 관여하지 않음).
6. **모니터링(대기 중)** — 현재 웨이퍼에서 **몇 번 다이까지 작업 완료**됐는지 계속 확인.
   - `MaterialStateService.HasReadyInputStagePickTarget()`로 남은 픽 대상 존재 여부 판단.
   - 다이가 전부 빠지면 피커가 `Bus.Set("InputStageDieComplete")`를 줌 → `WaitPickerToCompleteInputStageDiesAsync`가 이를 대기.
7. **웨이퍼 전체 소진** → 피커에 "작업 금지(웨이퍼 교체 예정)" 신호 후 **웨이퍼 언로딩**.
   - `Bus.Reset("InputStageReady")`로 픽업 금지 → `UnloadInputStageWaferIfPresentAsync` → 카세트로 반환.
8. 언로딩 후 카세트에 **다음 작업 웨이퍼가 있으면 로딩** → 2번으로 반복.
9. **마지막 웨이퍼가 처리되어 카세트가 비면** → 웨이퍼 교체 알림.
   - `cassette.IsInputCassetteProcessComplete()` → `cassette.RaiseInputCassetteCompleteAlarm(...)`.
   - 더 진행할 슬롯이 없으면 `StopAutoSequence(...)`로 `SequenceStopException`을 던져 Coordinator가 정지.

**구현 메모**
- 위 흐름의 대부분이 이미 `InputSequence.cs`에 골격으로 존재한다. **빠진 분기(다이 진행 카운트 모니터링, 마지막 웨이퍼 알림 타이밍)를 채우고, Manual/Step과 조건 공유를 정리**하는 것이 핵심.
- 슬롯 상태 갱신은 반드시 `UpdateInputSlotState(slot, presence, ProcessState.*)`로.

---

## 4. OutputSequence 요구사항

**전제:** GOOD / NG **스테이지 2개**가 있다. (`MaterialLocationKind.OutputStageGood`, `OutputStageNg`)

**목표 사이클(Auto, 무한 반복):** `ResolveNextOutputAction()`이 우선순위로 다음 액션을 결정한다.

1. **NG 스테이지 수령 완료** → 카세트로 배출 (`StoreNgStageToCassette`).
2. **GOOD 스테이지 수령 완료** → 카세트로 배출 (`StoreGoodStageToCassette`).
3. **피더에 보유품 존재** → 이어서 처리 (`ResumeOccupiedFeeder`).
4. **스테이지 비어 있고 카세트에 공급 가능** → 스테이지로 공급.
   - **로딩 우선순위는 NG Stage 우선** (`canSupplyNg && (!canSupplyGood || 둘 다 비었음)` → NG 먼저).
   - 그 다음 GOOD, 마지막으로 NG.
5. **공급/배출할 게 없으면** → 스테이지 Ready 신호 세팅 후 **피커의 수령 완료 신호를 대기** (`WaitOutputStageReceiveComplete`).
   - `SetOutputStageReadySignals()` → `OutputGoodStageReady`/`OutputNgStageReady`/`OutputStageReady`.
   - `WaitAnyOutputReceiveCompleteAsync`가 GOOD/NG 중 먼저 끝나는 쪽을 대기.

**Output 쪽 스테이지 로직(공급 시):**
- 스테이지에 웨이퍼 없으면 로딩(우선순위 NG) → 있으면 얼라인·맵핑 완료 확인 후 미완료 지점부터.
- 맵핑 완료 시 피커에 "다이 내려놔도 된다" 신호(`OutputStageReady`).
- **다이 결과값(GOOD/NG)으로 피커가 스테이지를 선택**해 플레이스함. Output 시퀀스는 어느 스테이지가 채워지는지 모니터링만.
- 현재 웨이퍼에 다이가 전부 채워지면 피커가 작업 금지 신호 → 해당 스테이지 웨이퍼 언로딩 → 카세트 반환.
- 카세트 마지막 웨이퍼까지 채워지면 **웨이퍼 교체 알림** + `StopAutoSequence`.

**구현 메모**
- 상당 부분이 `OutputSequence.cs` + `OutputSlotPlanner`에 구현돼 있다. **우선순위(NG 우선) 검증, 마지막 웨이퍼 알림, 카세트 동일-소스-슬롯(Same source slot) 제약**(`TryResolveNextStoreSlot`)을 재확인하고 누락 분기를 채운다.
- 배출 후 해당 측 `Receive*Complete` 신호를 반드시 `Reset` 한다 (재진입 방지).

---

## 5. FrontPicker / RearPicker 병렬 인터락 요구사항 ★핵심★

**각 픽커의 단일 사이클:** `Pickup → Bottom 검사 → Side 검사 → Place` (= `PickerProcessSequence`의 상태머신, 이미 존재).

**Front, Rear가 병렬로 동시에 도는 것이 핵심이며, 인터락·작업 순서가 절대 꼬이면 안 된다.**

### 5.1 진입 조건
- 각 픽커는 `WaitForPickerWorkAsync`로 할 일이 생길 때까지 `Task.Delay`로 대기(폴링) — 별도 모니터 스레드 금지.
- **Pickup 직전:** Input 측 신호 확인(`InputStageReady` 세팅 + `MaterialStateService.HasReadyInputStagePickTarget()`로 가져갈 다이 존재).
- **Place 직전:** Output 측 신호 확인(`OutputStageReady` / 해당 grade 스테이지 수령 가능 + 내려놓을 위치 존재).
- 가져갈 다이 / 내려놓을 위치가 없으면 **무엇을 해야 하는지 모니터링하며 대기**한다(에러 아님).

### 5.2 우선순위
- **Front Picker 우선.** 자원/영역 경합 시 Front가 먼저 점유한다.
- 우선순위는 `SequenceResourceManager` 락 획득 순서 + 신호 게이트로 강제한다. (Front가 락을 먼저 잡도록 보장)

### 5.3 기구 간섭 인터락 (반드시 구현)
기구 간섭 때문에 다음 두 동작은 **상대 픽커의 특정 공정 단계에서만** 허용된다:

| 내가 하려는 동작 | 허용 조건 (상대 픽커 상태) |
|---|---|
| **Pickup** | 상대 픽커가 **Side 검사를 시작**했을 때 |
| **Place** | 상대 픽커가 **Bottom 검사 중**일 때 |

- 즉, 한 픽커가 Side 검사 단계에 진입하면 상대 픽커가 Pickup 가능, 한 픽커가 Bottom 검사 단계에 있으면 상대 픽커가 Place 가능.
- 이를 위해 **각 픽커가 자기 공정 단계 진입/이탈을 신호로 게시**하고, 상대는 그 신호를 `Bus.WaitAsync`로 기다린다. (제안 신호명은 §6 표 참조)
- 추가로 기존 `PickerZoneInterlockRules.BeginPickerWorkAreaUse(...)` 영역 락과 `MoveOppositePickerToAvoidAndVerifyAsync(...)`를 그대로 활용해 물리적 회피를 보장한다.
- **데드락 금지:** 두 픽커가 서로의 단계 신호를 동시에 기다려 멈추지 않도록, 신호는 "상대가 해당 단계에 있다"는 **상태 게시(Set on enter / Reset on exit)** 방식으로 설계하고, 대기에는 항상 `ct` 연동 타임아웃/취소를 건다.

### 5.4 Manual/Step 공유
- Manual/Step 모드에서도 위 5.1~5.3 조건(상대 픽커 단계, Input/Output 신호, 영역 락)을 동일하게 확인한 뒤 자기 단계를 1스텝 진행한다.
- `PickerProcessSequence.IsStepRunMode()` 분기를 유지하되, 조건 확인은 Auto/Step 공통 헬퍼로.

---

## 6. 신호(SignalBus) 규약

`Context.Bus` 사용. **기존 신호명은 그대로 재사용**하고, 병렬 픽커 인터락용 신호만 신규 추가한다.

### 기존 신호 (검증된 실제 사용 중)
- Input: `InputCassetteMapped`, `InputStageLoadPrepared`, `InputStageAligned`, `InputStageDieMapped`, `InputStageFinishComplete`, `InputStageReady`, `InputStageOccupied`, `InputStageEmpty`, `InputWaferLoaded`, `InputWaferUnloaded`, `InputStageDieComplete`, `InputFeederOccupied`/`Empty`/`Recovered`/`Exchanged`, `InputCassetteSlotUpdated`, `InputStageAvoidReady`.
- Output: `OutputStageReady`, `OutputGoodStageReady`, `OutputNgStageReady`, `OutputGoodStageReceiveComplete`, `OutputNgStageReceiveComplete`, `OutputStageOccupied`, `OutputStageEmpty`, `OutputFeederOccupied`/`Empty`, `OutputCassetteSlotUpdated`.
- 핸드오프 예: 피커 Place 완료 시 `Bus.Set("InputStageDieComplete")` (이미 `PickerPlaceSequence.cs:527`).

### 신규 제안 신호 (병렬 픽커 인터락 — 이름 확정 전 코드 리뷰 필요)
- `FrontPickerInBottomInspection`, `FrontPickerInSideInspection`
- `RearPickerInBottomInspection`, `RearPickerInSideInspection`
- 규칙: 해당 단계 **진입 시 `Set`**, **이탈 시 `Reset`**. 상대 픽커는 §5.3 표에 따라 대기.

> 신규 신호명은 기존 컨벤션(PascalCase, 유닛+상태)에 맞추고, 추가 전 `Sequencing` 전체에서 충돌 여부를 grep으로 확인할 것.

### 리소스 락
`Context.Resources.AcquireAsync(SequenceResourceKind.XXX, holder, timeoutMs, ct)` → 반드시 `using (lease)` 스코프. `lease == null`이면 `Fail(...)`.

---

## 7. 실패/정지 처리 패턴 (기존 그대로)

```csharp
private int Fail(string alarmCode, string source, string message)
{
    // SequenceFailureStore.Record(...) + Log.Write(...) + AlarmManager.Raise(Warning, ...) + Context.LogPublic(...)
    return -1;
}

private int StopAutoSequence(string reason)
{
    // 정상적 작업 종료(더 진행할 자재 없음 등) → throw new SequenceStopException(reason);
}
```
- 장비/모션 실패 → `Fail(...)` 후 `-1` 전파 → 호출부에서 `throw new InvalidOperationException(...)` → Coordinator가 전체 취소.
- 작업 소진(정상) → `StopAutoSequence(...)`.

---

## 8. 완료 기준 (Definition of Done)

1. **빌드 통과** (`CLAUDE.md`의 MSBuild 명령):
   ```powershell
   & $MSB "QMC.Common\QMC.Common.csproj"   /t:Build /p:Configuration=Debug
   & $MSB "QMC.CDT-320\QMC.CDT-320.csproj" /t:Build /p:Configuration=Debug
   ```
2. **검증 스크립트 통과:**
   ```powershell
   perl tools/verify_all.pl
   perl tools/verify_handler_features.pl
   $env:RUN_GUI_CYCLE=1; perl tools/runtime_cycle_test.pl
   ```
3. `QMC.CDT-320.exe --auto-cycle 10` 자동 사이클이 Init→CycleRun(10)→Lot JSON 저장까지 정지 없이 완주.
4. Front/Rear **병렬 사이클에서 인터락 위반(동시 Pickup/Place 충돌)·데드락이 발생하지 않음** — Sim 모드로 반복 사이클 확인.
5. Auto에서 검증한 선행 조건이 Manual/Step에서도 동일하게 적용됨.
6. 신규/수정 코드가 AGENTS.md 규칙(예외·로그·알람·명명·인코딩·`Task<int>`) 준수.

---

## 9. 작업 순서 제안

1. (분석) 4개 시퀀스 + `PickerProcessSequence` + SignalBus 신호 흐름을 그려 현재 구현/누락을 표로 정리.
2. (Input) §3 누락 분기 채우기 — 다이 진행 모니터링, 마지막 웨이퍼 알림, Manual 조건 공유.
3. (Output) §4 우선순위(NG 우선)·마지막 웨이퍼 알림·슬롯 제약 검증 및 보완.
4. (Picker) §5 병렬 인터락 신호 추가 + Pickup/Place 게이트 구현 (★가장 신중하게★, 데드락 테스트).
5. (검증) §8 빌드·perl·auto-cycle·Sim 반복 사이클.

> **한 번에 4개를 다 고치지 말고 유닛 단위로 작업 → 빌드 → 검증을 반복**한다.
> 인터락 변경은 작은 단위 커밋으로 나눠 회귀를 좁힌다.
