# 작업: 비전 회피 ∥ 피커 진입 동시 기동 — FollowMoveAsync 팔로잉 진입 (Input/Output 비전 공통)

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일:
  - `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` (`FollowMoveAsync` 타임아웃 인자화)
  - `QMC.CDT-320\Equipment\Motion\SharedRailX\SharedRailXConfig.cs` / `SharedRailXConfigStore.cs`
    (타임아웃 설정 신설)
  - `QMC.CDT-320\Ui\Dialogs\SharedRailXSetupDialog.cs`(+Designer) (설정 UI 노출)
  - `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs` (웨이퍼 비전 회피∥피커 진입)
  - `QMC.CDT-320\Sequencing\Picker\PickerPlaceSequence.cs` (빈 비전 회피∥피커 진입)

## 선행 의존 (작업 시작 전 존재 여부 확인)
1. `AjinAxis.FollowMoveAsync(...)` — 2축 팔로잉 함수 (사양: `cdt-320\codex-prompts\axis-follow-move.md`).
   존재를 확인하고 시그니처를 파악할 것. 없으면 이 작업을 중단하고 보고.
2. 비전 최소 회피 작업 (`cdt-320\codex-prompts\vision-minimal-retreat.md`) —
   `InputVisionRetreatExtraClearance` / `OutputVisionRetreatExtraClearance`(기본 40)와
   최소 회피 목표 계산. **이미 적용돼 있으면 재사용**하고, 아직이면 이 작업에서 해당 사양대로
   설정 2종과 최소 회피 목표 계산을 함께 구현한다 (두 작업은 수치적으로 맞물려 설계됨).

## 목적

현재는 "비전 회피 이동 완료 → 피커 X 진입 시작" 순차 구조다. 이를:
```
비전 회피 이동을 비동기 시작(선행축, 명령은 시퀀스가 발행)
  └→ 즉시 피커X.FollowMoveAsync(선행=비전축, ...) 호출
      피커가 안전거리를 유지하며 비전을 실시간으로 따라 진입 (포지션 오버라이드 추종)
```
으로 바꿔 비전 회피와 피커 진입을 **동시 기동**한다. Input(웨이퍼)·Output(빈) 비전 모두 적용.

## 파라미터 매핑 (확정 — 이 값이 그대로 나와야 한다)

FollowMoveAsync의 간격 공식과 SharedRailX 페어 수식은 동일 구조다:
`-방향: 거리 = (후행 + HomeGap) − 선행` = `피커 + HomeClearance − 비전` (Input 페어식과 일치)
`+방향: 거리 = (선행 + HomeGap) − 후행` = `비전 + HomeClearance − 피커` (Output 페어식과 일치)

| FollowMoveAsync 인자 | 웨이퍼(Input) | 빈(Output) | 출처 |
|---|---|---|---|
| this(후행축) | 진입하는 피커X (Front/Rear) | 동일 | 시퀀스 컨텍스트 |
| leadingAxis(선행축) | InputVisionX | OutputVisionX | — |
| 선행 목표 | 최소 회피 목표 (minimal-retreat 계산값) | 동일 | 회피 좌표 계산부 |
| 후행 목표 | 해당 피커의 진입 X 목표 | 동일 | 배치 계산값 |
| direction | **−1** (둘 다 −방향) | **+1** (둘 다 +방향) | 기하 확정 |
| homeGap | **페어 HomeClearance (현장값 88.5)** | **페어 HomeClearance (현장값 510)** | `shared_rail_x.json` CollisionPairs에서 런타임 조회 — **하드코딩 금지** |
| safetyGap | **페어 SafetyDistance(10) + InputExtra(40) = 50** | **SafetyDistance(10) + OutputExtra(40) = 50** | 설정에서 런타임 계산 — 하드코딩 금지 |
| Vel/Acc/Dec | 각 축 공정값 (follow 내부에서 Min 선택) | 동일 | 기존 관례 |
| timeoutMs | **신설 설정 `VisionFollowEntryTimeoutMs`** | 동일 | R2 |

수치 정합성 (검증 기준으로 사용):
- Input: 비전이 650→638.5로 회피 중, 피커 목표 600 → 피커 진입 한계 = 비전위치 − 38.5.
  비전 638.5 도착 순간 한계 = 600 = 피커 목표 → **비전 도착과 동시에 피커도 목표 도달 가능** ✓
- Output: 비전이 →80 회피 중, 피커 목표 540 → 한계 = 비전위치 + 460. 비전 80 도착 시 540 ✓
- 페어 HomeClearance/SafetyDistance를 설정에서 읽으므로 현장값이 바뀌어도 식이 유지된다.

## 요구사항

### R1. FollowMoveAsync 타임아웃 인자화 (`AjinAxis`)
- 현재 5초 고정(`FollowMoveTimeoutMs = 5000` 상수)을 **선택적 인자**로 전환:
  `int timeoutMs = 0` (0 이하 → 기존 5000 기본값). 기존 호출부는 무변경으로 동작.
- 함수 전체(팔로잉 루프 + 최종 모션돈 대기)에 동일 타임아웃 적용 — 기존 타임아웃 의미 유지.

### R2. 타임아웃 설정 신설 + UI
- `SharedRailXConfig`(+Document, 스토어)에 `VisionFollowEntryTimeoutMs` 추가.
  **기본값 15000ms**, `Normalize()`에서 1000 미만이면 기본값 보정. 기존 json 하위호환.
- **`SharedRailXSetupDialog`에 설정 항목 노출** (필수):
  - `VisionFollowEntryTimeoutMs` (ms 단위 숫자 입력)
  - 같은 다이얼로그에 `InputVisionRetreatExtraClearance` / `OutputVisionRetreatExtraClearance`도
    함께 노출한다 (이미 노출돼 있으면 생략). 저장/로드는 기존 다이얼로그의 config 저장 경로 재사용.
  - UI 배치는 기존 다이얼로그 스타일(레이블+NumericUpDown/TextBox)을 따르고, 단위를 레이블에
    명시 (ms / mm).

### R3. 웨이퍼 비전 — 픽업 진입 결합 (`PickerPickUpSequence`)
- 게이트: `Options.RunMode == Auto` **그리고** `TransferMotionMode`가 Conti 계열
  (`ContiSegmentedPickUp`/`FastContiSegmentedPickUp`). 미충족 시 기존 순차 경로 그대로.
- 게이트 충족 시:
  1. `MoveInputVisionToAvoidForPickerMove`에서 InputVisionX 최소 회피 이동을
     **비동기 시작**(Task 보관, await하지 않음). 이동 명령은 기존 경로(SharedRailX 중재 경유)와
     동일한 헬퍼로 발행한다.
  2. 첫 피커의 X 진입(`MovePickerXStageYPickerT`의 PickerX 이동 부분)을
     `pickerX.FollowMoveAsync(InputVisionX, ..., direction=-1, safetyGap, homeGap, timeout)` 으로
     대체한다. **X축만 follow** — StageY/NeedleX/PickerT/PickerY 등 나머지 축의 기존
     병렬/순차 구조는 변경하지 않는다.
  3. **비전이 이미 정지 상태**(이미 회피 완료, `IsMoving == false`)면 follow를 쓰지 않고 기존
     일반 이동 — 배치 2번째 피커부터는 자연히 일반 이동이 된다.
  4. 비전 회피 Task는 첫 피커 처리 완료 전에 반드시 join(await)해 결과 0 확인 + observe.
- 스텝 전이 구조는 유지하되, 비전 회피 Task를 스텝 간에 전달할 상태 필드를 추가한다
  (실패/취소 시 drain 포함 — 기존 moveTask 관례).

### R4. 빈 비전 — 플레이스 진입 결합 (`PickerPlaceSequence`)
- 게이트: `Options.RunMode == Auto` **그리고** `IsCoordinatedPlaceMotionMode(...)` Conti 계열.
- R3과 동일 구조: `MoveOutputStageAvoidPositionAsync`의 OutputVisionX 최소 회피를 비동기
  시작 → 피커 X의 플레이스 진입 이동을 `FollowMoveAsync(OutputVisionX, ..., direction=+1, ...)`로
  대체 → 비전 정지 상태면 일반 이동 → Task join/observe.
- 플레이스 쪽 피커 X 진입 지점(`MovePickerXYAndTToPlaceAsync` /
  `MoveOutputStageYAndPickerXYTToPlaceAsync` 등 — X 이동을 수행하는 실제 경로를 읽고 결정)에서
  X 부분만 follow로 대체하고 Y/T/StageY 병렬 구조는 유지한다.

### R5. 인터락 — 우회 절대 금지 + 경로 통과 검증 (필수 요구사항)
- **`BeginMotionGuardBypass` 등 인터락 우회 API 사용 절대 금지.**
- FollowMoveAsync 내부의 이동/오버라이드 경로(`MoveAbsoluteAsync`, `TryOverridePosition`)가
  공유레일 축에서 `MotionGuardRuntime`(SharedRailX 충돌 검증 포함)을 **통과하는지 코드로
  확인하고, 통과 근거(호출 체인)를 작업 보고에 명시**한다.
  - 만약 follow 내부 경로가 SharedRailX 중앙 중재를 거치지 않는 직접 이동이라면, 중재를
    통과하도록 통합(중재 경유 이동/오버라이드 사용)하되 인터락 판정 자체는 절대 변경하지 않는다.
- 논리적 안전 근거(주석으로 명시): 팔로잉 유지 간격(50) > 인터락 요구 간격(10)이므로
  정상 추종 중 인터락 거부는 발생하지 않는다. 그럼에도 거부(-11)가 발생하면 R6 폴백.
- 인터락/충돌 판정 코드 diff 0건을 수용 기준으로 한다.

### R6. 실패 처리 / 폴백
- follow가 실패(타임아웃 -21, 인터락 -11, 선행축 알람 -22 등)하면:
  1. 피커 X 정지 확인(함수가 정지 처리) 후,
  2. 비전 회피 Task 완료를 await (결과 무관 observe),
  3. **기존 일반 이동(순차 경로)으로 1회 재시도**. 재시도도 실패하면 기존 Fail 관례로 시퀀스 에러.
- 취소(ct)는 기존 관례대로 전파, 양 Task drain.

## 제약 사항
- 비Conti/수동/캘리브레이션 경로 무변경. 기존 FollowMoveAsync 호출부 무변경(기본 인자).
- homeGap/safetyGap/timeout 하드코딩 금지 — 전부 SharedRailX 설정에서 런타임 조회.
  페어 조회는 (비전축, 해당 피커축) 페어를 정확히 매칭 (Front/Rear 구분).
- 시뮬레이션 모드 동작 필수 (follow의 시뮬 분기 경유 — 별도 분기 금지).
- 코드 스타일: 기존 관례 (한국어 로그 Start/Ok/Failed, F6 포맷, Fail/FailMotion).

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과. UI 다이얼로그가 열리고 신설 값 저장/로드 동작.
2. **수치 정합성** (시뮬 하네스, 실측 설정값 88.5/510/10/40 사용, 결과 보고):
   - Input: 비전 650→638.5 회피 + 피커 700→600 진입 동시 시작 →
     전 구간에서 간격(피커+88.5−비전) ≥ 50 유지(로그로 매 폴링 기록), 최종 피커=600 도달,
     비전 도착 시점과 피커 도달 시점이 근접(오버랩 성립).
   - Output: 비전→80 + 피커→540, 간격(비전+510−피커) ≥ 50 유지, 최종 540 도달.
   - 전 구간 간격이 인터락 요구(10)를 침범한 순간이 0회.
3. **동시성 검증**: 로그 타임스탬프로 "비전 이동 시작 ≈ 피커 이동 시작"(순차 대비 오버랩)을 확인.
4. **인터락 검증(필수)**: R5의 호출 체인 보고 + 우회 API 미사용 grep 증명 +
   인터락 코드 diff 0건 + 시뮬 사이클에서 인터락 위반/알람 0건.
5. 폴백 검증: follow 강제 실패(타임아웃 축소 등) 시 일반 이동 재시도로 사이클이 정상 완료.
6. 비전 정지 상태(2번째 피커)에서 follow를 타지 않고 일반 이동함을 로그로 확인.
7. 비Conti 모드 회귀 없음 (기존 순차 경로/좌표 동일).
