# [수정 지시] Feeder HOME의 VisionX 물리 퇴피 제거 + Output 인터락 Dog 게이트 추가

작업 대상: `D:\Source\CDT-320_New`
지시자: 팀장님 (2026-08-11 승인)

---

## 0. 이 작업의 규칙 (반드시 준수)

1. **시킨 것만 한다.** 아래 "변경 범위"에 없는 것은 손대지 말 것. 걸림돌이 생기면
   임의로 해결하지 말고 **"문제 있다, 어떻게 할까요?"로 팀장님께 물어볼 것**.
2. **수정 전에 변경할 파일·함수·라인 목록을 팀장님께 전부 고지**하고 승인 후 편집.
3. **빌드 주의**: 이 솔루션의 기본 빌드 출력이 실장비 실행 폴더(`D:\CDT-320`)로 직결된다.
   장비가 실행 중일 수 있으므로 **기본 빌드 금지**. 검증 빌드는 반드시
   `/p:OutDir=<임시경로>` 로 우회할 것.
4. **모션 속도 스케일 규칙**: 새로 추가/수정하는 모든 자동 시퀀스 일반 이동은
   `MotionSpeedScale`(DefaultVelocity 퍼센트 스케일)이 적용되는 경로를 사용해야 한다.
   스케일 미적용 이동 코드를 작성하지 말 것. 기존 코드에서 스케일 미적용을 발견하면
   고치지 말고 **팀장님께 보고**할 것. (이번 작업은 이동을 제거하는 쪽이라
   신규 이동 코드는 없어야 정상이다.)
5. **계측 필수**: 판정 분기마다 사유와 수치를 로그로 남겨서, 실장비 1회 실행으로
   원인을 확정할 수 있게 할 것. (아래 §4에 필수 로그 명시)
6. `ActualPosition`이 지령값을 반환하는 것은 **의도된 설계**다. 교정하지 말 것.
7. 시뮬레이션 모드에서 먼저 통과 확인 후 실장비 시험.

---

## 1. 배경 (왜 빼는가)

전체 초기화 Step 180(Input Feeder) / Step 260(Output Feeder)은 FeederY HOME 전에
**VisionX(카메라X)를 하드리밋까지 물리적으로 밀어내는 "퇴피" 동작**을 수행한다.
Input은 MEL(−) 방향, Output은 PEL(+) 방향이다.

이 퇴피 때문에 다음 문제가 실장비에서 반복되고 있다 (2026-08-11 실측):

```
19:52:34 / 20:07:11 / 20:27:42 / 20:29:36  INIT-PREP> EjectPinZ HOME 시작 차단
20:09:43 ~ 20:12:13 (5회), 20:35:46        INIT-PREP> Vision X 퇴피는 이번 전체 초기화에서
                                            공통 Z와 PickerY HOME을 먼저 완료해야 합니다.
                                            axis=FrontPickerZ0
20:52:40                                   INIT-INTERLOCK> step=340 ... InputFeederY HomeDone 아님
```

`axis=FrontPickerZ0`은 검사 리스트의 첫 번째 원소일 뿐이고 그 축 자체 문제가 아니다.
원인은 퇴피의 선행조건 검사(`VerifyFeederVisionRetreatRunPrerequisites`)가
`_runState.IsAxisHomed()` 기반이라 **"이번 초기화 실행 안에서" 14개 축 HOME을 요구**하는데,
개별축 HOME은 `BeginRun/EndRun`이 매번 감기며 실행 목록이 초기화되어 절대 충족되지 않는 것이다.

**팀장님 결정**: 카메라 퇴피 동작을 Input/Output **양쪽 다 제거**한다.
FeederY HOME의 안전은 **Feeder Avoid Dog 센서**로 확인한다.
Dog가 OFF면 **그냥 실패 처리**(작업자가 수동으로 피더를 Avoid로 뺀 뒤 재시도) — 옵션 (a).

---

## 2. 변경 범위 (이것만 한다)

### 2-1. `QMC.CDT-320\Equipment\Initialization\AxisInitializeRuntime.cs`

**(A) Step 180/260 특수 분기 제거** — `ExecuteSerialHomeAsync` (약 `:2498~2514`)

```csharp
if (step != null && step.StepNo == 180)
    return await ExecuteFeederHomeWithVisionRetreatAsync(true,  axes.FirstOrDefault(), ct);
if (step != null && step.StepNo == 260)
    return await ExecuteFeederHomeWithVisionRetreatAsync(false, axes.FirstOrDefault(), ct);
```
→ 위 두 블록을 삭제해서 Step 180/260이 아래의 **일반 Serial HOME 루프**(`:2516~2524`)를
타도록 한다. 즉 FeederY를 평범하게 `ExecuteSingleAxisHomeAsync`로 HOME 한다.

**(B) 함수 전체 삭제**
- `ExecuteFeederHomeWithVisionRetreatAsync` (`:2232~2493`)
- `VerifyFeederVisionRetreatRunPrerequisites` (`:1475~1533`)
- `VerifyPickerAxesStoppedForVisionRetreat` (`:1585~`)
- `TryRefreshFeederAvoidDog` (`:1536~1583`) — 위 함수에서만 호출되므로 미사용이 된다.
  **단, §2-2에서 인터락이 Dog 실입력 읽기를 하도록 만들 때 이 로직을 참고할 것**
  (`AjinIoScanService.TryReadHardwareInput` + Simulation/IgnoreWaits 거부 패턴).

**(C) 상수 삭제** (`:25`, `:26`, `:32`)
- `FeederVisionLimitBackoffDistanceMm`
- `FeederVisionServoSettleMs`
- `FeederVisionLimitSearchTimeoutMs`

### 2-2. `QMC.CDT-320\Equipment\Interlocks\OutputFeederInterlockRules.cs`

`VerifyOutputFeederYAbsoluteGuard` (`:75~109`) 의 카메라 검사를 **Input과 동일한 형태**로
Dog 게이트 안에 넣는다.

현재 (Dog 조건 없이 항상 검사):
```csharp
bool initializeVisionRetreatVerified =
    request.MoveKind == MotionGuardMoveKind.AxisHome &&
    MotionGuardRuntime.IsFeederHomeVisionRetreatActive(feeder.FeederY, cameraX, false);

if (!initializeVisionRetreatVerified &&
    !IsOutputVisionXInAvoidPosition(stage) &&
    cameraX.ActualPosition < 1000.0)
    return MotionGuardRuleHelpers.Block(
        "OutputFeederY",
        "OutputFeederY 이동 불가: OutputCameraX가 정확한 Avoid 또는 1000 이상 위치여야 합니다. ...",
        out reason);
```

변경 후:
- `initializeVisionRetreatVerified` 및 `IsFeederHomeVisionRetreatActive` 호출 **삭제**
  (퇴피 스코프가 없어지므로)
- `bool feederAvoidDogOn = feeder.IsBinFeederAvoidPositionCheck();` 를 구해서
  카메라 검사를 `if (!feederAvoidDogOn) { ... }` 로 감싼다
- 즉 **Dog ON이면 카메라 위치를 보지 않고 통과**, **Dog OFF면 카메라가 Avoid이거나
  1000 이상이어야 통과, 아니면 Block** (옵션 (a))

**Dog 값은 실입력을 새로 읽어서 판정할 것.** 이유: 퇴피가 없어지면서 Dog가
**유일한 안전 게이트**가 된다. 같은 파일의 `BinFeederDownSensor` 검사(`:118~124`)가
이미 `AjinIoScanService.TryReadHardwareInput`로 실입력을 갱신하는 패턴을 쓰고 있으니
그것과 동일하게 처리한다. 읽기 실패 시에는 **Block**(안전측)으로 간다.
Simulation / DryRun 경로는 기존 `IsOutputFeederSimulationOrDryRun()` 처리 관례를 따른다.

### 2-3. `QMC.CDT-320\Equipment\Interlocks\InputFeederInterlockRules.cs`

`VerifyInputFeederYAbsoluteGuard` (`:103~141`):
- `initializeVisionRetreatVerified` 및 `IsFeederHomeVisionRetreatActive` 호출 **삭제**
- **`if (!feederAvoidDogOn)` 게이트 구조는 이미 있으므로 그대로 유지** (변경 없음)
- Dog 값을 실입력 갱신 후 판정하도록 §2-2와 동일하게 맞춘다

결과적으로 Input/Output이 완전히 대칭이 된다.

### 2-4. `QMC.CDT-320\Equipment\Interlocks\Common\MotionGuardRuntime.cs`

퇴피 예외 스코프를 삭제한다 (호출부가 모두 없어짐):
- `CurrentFeederHomeVisionRetreatScope` 필드 (`:19~20`)
- `BeginFeederHomeVisionRetreat` (`:480~493`)
- `IsFeederHomeVisionRetreatActive` (`:499~`)
- `FeederHomeVisionRetreatScope` 클래스 (`:1124~`)
- `FeederHomeVisionRetreatScopeToken` 클래스 (`:1141~1156`)

---

## 3. 절대 건드리지 말 것 (공용 코드 — 다른 기능이 쓰고 있음)

퇴피에서 쓰던 아래 `AjinAxis` 헬퍼들은 **PickerY 페어 HOME과 실시간 충돌 감시가 함께
사용**한다. 삭제하거나 시그니처를 바꾸면 다른 초기화 스텝과 안전 감시가 깨진다.

| 멤버 | 다른 사용처 |
|---|---|
| `SearchHardwareLimitForInitializeAsync` | `AxisInitializeRuntime.cs:2919, :2924` (PickerY 페어 MEL/PEL 동시 탐색) |
| `ReleaseInitializeHardwareLimitSearch` | `:3085, :3087` (PickerY 페어) |
| `StopInitializeHardwareLimitSearch` | `:3134, :3138` (PickerY 페어) |
| `IsInitializeHardwareLimitSearchActive` | `RealtimeCollisionSupervisor.cs:278~279` (PickerY 페어 리밋탐색 예외 판정) |
| `_hardwareLimitSearchDirection` | 위 전부의 공용 상태 |
| `TryReadInitializeHardwareFeedback` | `AxisInitializeRuntime.cs:1758` (`TryReadHomePreparationActual`) |

- `BackOffHardwareLimitForInitializeAsync`는 퇴피에서만 호출되지만, **범용 모션 헬퍼이므로
  삭제하지 말고 그대로 둔다**(호출부만 사라짐).
- `AjinAxis.IsFeederVisionRetreatAxis()`(`:553`)와 그것이 쓰이는 소프트리밋 억제
  (`:92`, `:2727~2729`)도 **그대로 둔다**. 퇴피가 없어지면 VisionX의
  `_hardwareLimitSearchDirection`이 항상 0이므로 이 분기는 자동으로 비활성이 된다.
  소프트리밋 판정 로직을 건드리는 위험을 만들지 않는다.
- `AxisInitializeRunState` 및 `IsAxisHomed` / `MarkAxisHomed`는
  `ExecuteSingleAxisHomeAsync`(`:1918`)의 중복 HOME 스킵에 계속 쓰이므로 **유지**.

---

## 4. 필수 로그 계측

1. **Output 인터락 Dog 판정** — 판정할 때마다 다음을 한 줄로:
   `dogOn`, Dog 실입력 읽기 성공/실패(+error), `cameraX.ActualPosition`,
   `IsOutputVisionXInAvoidPosition` 결과, 최종 통과/Block 사유.
   Input 인터락도 동일 항목으로 맞춘다.
2. **Step 180/260 진입 로그** — "VisionX 퇴피 없이 FeederY HOME을 수행합니다"를
   `stepNo`, 축 이름과 함께 남긴다. 기존 `VisionXFeederHomeRetreat` 태그로 나가던
   완료 로그가 사라지므로 대체 흔적이 필요하다.
3. Block 메시지에는 **작업자 조치**를 포함한다:
   "Feeder를 Avoid 위치로 이동시켜 Avoid Dog를 ON으로 만든 후 다시 실행하십시오."

---

## 5. 검증 (실장비 전에 반드시)

1. **시뮬레이션 모드 전체 초기화 완주** — Step 180 / 260이 카메라를 움직이지 않고
   FeederY HOME만 수행하고 통과하는지.
2. `INIT-PREP> Vision X 퇴피는 ...` 알람이 **더 이상 나오지 않는지** 확인.
3. **Dog OFF 시나리오**: Dog를 OFF로 만든 상태에서 Step 180 / 260 실행 →
   기대 동작은 **Block + 조치 안내 메시지**. 카메라가 움직이지 않아야 한다.
4. **Dog ON 시나리오**: 카메라가 Avoid가 아닌 임의 위치(예: 중앙)에 있어도
   FeederY HOME이 통과하는지.
5. Auto 운전 / Manual 이동 경로의 FeederY 인터락 동작이 **변하지 않았는지**
   (퇴피 예외는 `MoveKind == AxisHome`에만 걸려 있었으므로 Auto/Manual은 무영향이어야 정상).
6. PickerY 페어 HOME 스텝이 여전히 정상 동작하는지(§3 공용 헬퍼 회귀 확인).

---

## 6. 팀장님께 보고/확인이 필요한 잔여 사항 (임의 처리 금지)

1. **Step 180 / 260의 플랜 레벨 HomeDone 인터락은 그대로 남는다.**
   `AxisInitializeSequence.cs`:
   - `:252~255` Step 180 → `FrontPickerY, RearPickerY, FrontPickerZ0~Z3, RearPickerZ0~Z3` HomeDone 요구
   - `:350~353` Step 260 → 위 + `OutputGoodStageZ` HomeDone 요구

   이것은 카메라와 무관한 별개의 안전 요구(수직축이 홈이어야 피더가 움직여도 안전)이고
   팀장님이 빼라고 지시한 대상이 아니므로 **이번 작업에서 건드리지 않는다.**
   → 따라서 **PickerZ 8축 + PickerY 2축 HOME이 먼저 성공해야 Step 180/260이 실행된다.**
   이 인터락도 빼야 하는지는 팀장님 판단 사항이니 물어볼 것.

2. **Output Feeder Avoid Dog 센서의 물리 상태 미확인.**
   설정은 실입력이다 (`Setup\BinFeederAvoidPositionCheck.json` = ModuleNo 2 / BitNo 27,
   `IsNormallyClosed: false`; `Config\...json` = `IsSimulationMode: false`, `IgnoreWaits: false`).
   그러나 배선/부착 정상 여부는 로그로 확인되지 않았다.
   **Dog가 물리적으로 ON이 안 되면 이 변경 후 Output FeederY HOME은 영구 차단된다.**
   이것이 이번 변경의 유일한 실패 모드이므로, 실장비 시험 전에 팀장님께
   센서 실동작 확인을 요청할 것. (참고: 같은 Output 계열에
   `CYL-SENSOR-BYPASS ... cylinder=NGBinGuideClamp` 바이패스가 걸려 있어 Output 센서
   신뢰도를 한 번 볼 필요가 있다.)

3. 이번 변경으로 **해소되지 않는 별개 장애**(보고만, 손대지 말 것):
   - `EjectPinZ HOME 시작 차단` — 초기화 플랜에 EjectPinZ Servo ON 경로가 없어
     사전에 수동 Servo ON이 필요하다. 게다가 `AjinAxis.ServoOnCore`가
     `AlarmManager.HasActive`면 조용히 무시하므로 **알람 리셋 → Servo ON → INIT** 순서 필수.
   - **27개 축 동시 `Servo alarm 0x0001`** (앱 시작 시). 이 코드는 드라이브 고유값이 아니라
     "보드가 amp fault를 보고했다"는 소프트웨어 기본값(`AjinAxis.cs:2775`)이다.
     동시 발생이므로 EMG/안전회로/드라이브 전원 계열 공통 원인을 봐야 한다.
     `fault`가 참인 동안은 `_homeDoneLatched`가 계속 풀려 **어떤 축도 홈 상태를 유지할 수 없다.**
   - `FrontPickerZ0`(AxisNo 12) 소프트리밋 불일치: 런타임 `SoftLimitPlus`가 1인데
     `Setup\FrontPickerZ0.json`은 20(Stroke 25)이고 파일 수정일이 6/20이다.
     Setup 파일은 앱 종료 시 덮어써지므로 **앱 종료 상태에서** 확인해야 한다.

---

## 7. 작업 체크리스트

### 7-1. 코드 편집

- [ ] `AxisInitializeRuntime.cs` `ExecuteSerialHomeAsync` — Step 180 분기 삭제
- [ ] `AxisInitializeRuntime.cs` `ExecuteSerialHomeAsync` — Step 260 분기 삭제
- [ ] `AxisInitializeRuntime.cs` — Step 180/260 진입 로그 추가 (§4-2)
- [ ] `AxisInitializeRuntime.cs` — `ExecuteFeederHomeWithVisionRetreatAsync` 삭제
- [ ] `AxisInitializeRuntime.cs` — `VerifyFeederVisionRetreatRunPrerequisites` 삭제
- [ ] `AxisInitializeRuntime.cs` — `VerifyPickerAxesStoppedForVisionRetreat` 삭제
- [ ] `AxisInitializeRuntime.cs` — `TryRefreshFeederAvoidDog` 삭제
- [ ] `AxisInitializeRuntime.cs` — 상수 3개 삭제
      (`FeederVisionLimitBackoffDistanceMm`, `FeederVisionServoSettleMs`,
      `FeederVisionLimitSearchTimeoutMs`)
- [ ] `OutputFeederInterlockRules.cs` — `initializeVisionRetreatVerified` 삭제
- [ ] `OutputFeederInterlockRules.cs` — `feederAvoidDogOn` 실입력 갱신 후 취득
- [ ] `OutputFeederInterlockRules.cs` — 카메라 검사를 `if (!feederAvoidDogOn)`로 감싸기
- [ ] `OutputFeederInterlockRules.cs` — Dog 읽기 실패 시 Block(안전측)
- [ ] `OutputFeederInterlockRules.cs` — 판정 로그 추가 (§4-1)
- [ ] `InputFeederInterlockRules.cs` — `initializeVisionRetreatVerified` 삭제
- [ ] `InputFeederInterlockRules.cs` — Dog 실입력 갱신 적용 (게이트 구조는 유지)
- [ ] `InputFeederInterlockRules.cs` — 판정 로그 추가 (§4-1)
- [ ] `MotionGuardRuntime.cs` — 퇴피 스코프 5개 멤버 삭제
- [ ] Block 메시지에 작업자 조치 문구 포함 (§4-3)

### 7-2. 건드리지 않았음을 확인 (§3)

- [ ] `SearchHardwareLimitForInitializeAsync` 무변경
- [ ] `BackOffHardwareLimitForInitializeAsync` 무변경 (호출부만 사라짐)
- [ ] `ReleaseInitializeHardwareLimitSearch` / `StopInitializeHardwareLimitSearch` 무변경
- [ ] `IsInitializeHardwareLimitSearchActive` 무변경
- [ ] `TryReadInitializeHardwareFeedback` 무변경
- [ ] `AjinAxis.IsFeederVisionRetreatAxis()` 및 소프트리밋 억제 분기 무변경
- [ ] `AxisInitializeRunState` / `IsAxisHomed` / `MarkAxisHomed` 무변경
- [ ] `AxisInitializeSequence.cs` 플랜 정의 무변경 (§6-1)

### 7-3. 빌드/정적 확인

- [ ] `/p:OutDir=<임시경로>` 로만 빌드 (실장비 폴더 직결 방지)
- [ ] 컴파일 경고 0건 — 미사용 변수/using 잔재 없음
- [ ] 삭제한 심볼의 잔여 참조 0건 (`IsFeederHomeVisionRetreatActive`,
      `BeginFeederHomeVisionRetreat`, `VerifyFeederVisionRetreatRunPrerequisites`,
      `VerifyPickerAxesStoppedForVisionRetreat`, `TryRefreshFeederAvoidDog`,
      `FeederVision*` 상수) 전체 검색으로 확인
- [ ] 신규 이동 코드 없음 = `MotionSpeedScale` 위반 없음 (§0-4)

### 7-4. 시뮬레이션 검증 (실장비 전 필수)

- [ ] 전체 초기화 완주 — Step 180 통과, 카메라 무이동
- [ ] 전체 초기화 완주 — Step 260 통과, 카메라 무이동
- [ ] `INIT-PREP> Vision X 퇴피는 ...` 알람 미발생
- [ ] Dog OFF 시나리오 → Block + 조치 안내, 카메라 무이동
- [ ] Dog ON + 카메라 임의 위치 → FeederY HOME 통과
- [ ] Auto 이동(`AxisTeachingMove`) FeederY 인터락 동작 불변
- [ ] Manual 이동(`AxisMove`) FeederY 인터락 동작 불변
- [ ] PickerY 페어 HOME 스텝 정상 (공용 헬퍼 회귀 없음)

### 7-5. 실장비 시험 전 팀장님 확인 사항

- [ ] Output Feeder Avoid Dog(M2/B27) 실동작 확인 요청 (§6-2)
- [ ] Step 180/260 플랜 HomeDone 인터락 유지 여부 확인 (§6-1)
- [ ] 잔여 별개 장애 3건 보고 (§6-3)
