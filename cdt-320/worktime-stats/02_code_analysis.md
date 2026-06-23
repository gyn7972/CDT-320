# CDT-320 Code Analysis — 작업 시간/UPH 표시 경로

분석 대상: `QMC.CDT-320` (WinForms, .NET 4.7.2). 관련 요구는 `01_requirement.md` 참조.

## 관련 레이어 맵

### UI 레이어
- **`Ui/Pages/Work/WorkMainPage.cs`** (374줄) — 작업 MAIN 화면. "작업 시간" 패널의 표시 주체.
  - `Timer _refresh` (500ms) → `RefreshAll()` → `BuildDisplaySnapshot()` → `ApplyDisplaySnapshot()`.
  - **스냅샷 패턴은 이미 잘 잡혀 있음**: 값을 한 번에 읽어 `WorkMainDisplaySnapshot`을 만들고, `SetText()`로 값이 바뀐 라벨만 갱신(레이아웃 churn 최소화).
  - 화면이 안 보이면(`ShouldRefreshVisible`) 타이머를 멈춤 → 불필요한 갱신 차단(좋음).
  - `LotStorage.ActiveLotChanged` 구독 → 비-UI 스레드 변경은 `BeginInvoke`로 마샬링(좋음).
- `Ui/Pages/Work/WorkMainPage.Designer.cs` — 라벨 컨트롤(`lblUph`, `lblCycle`, `lblRate`, `lblLoad`, ...) 선언.
- `Ui/Localization/Lang.cs` — `work.workTime.*` 라벨 i18n 키.

### Business Logic / 시퀀스 레이어
- **`Equipment/MachineController.cs`** (7000+줄) — 오토 사이클 엔진.
  - 카운터 필드: `CycleTotal`, `CycleDone`, `GoodCount`(int, private set, 105~107행), `NgCount`.
  - 통계 필드: `PickFailCount`, `PlaceFailCount`, `Collet1UseCount`, `NeedleUseCount`, `ErrorCount`,
    `NormalDownTime`, `ErrorDownTime`, `RecoveryTime`, `Mtbf`, `Mttr` (248~268행, `internal set`).
  - 메인 사이클 루프 (`CycleRunAsync`, ~6699행):
    ```
    for (int cyc = startCycle; cyc < totalCycles; cyc++) {
        await DoOneDieAsync(cyc, totalCycles, token);   // 1 사이클 = pickers개 다이 처리
        CycleDone = min(totalDies, (cyc+1)*pickers);
        RaiseProgress();
        ...
    }
    ```
    → **여기가 진짜 사이클 경계.** 사이클 타임 측정의 정확한 지점.
  - `event Action<int,int> CycleProgress` (63행) 존재하나 **구독자 0** (`RaiseProgress`만 호출).
- `Equipment/EquipmentStatus.cs` — Idle/Initializing/Ready/ManualRunning/AutoRunning/Stopped/CycleStopped/Completed/Alarm.
- `Equipment/CycleMode.cs` — Auto/Manual.

### Data / Lot
- **`Equipment/Lots/Lot.cs`** — Lot 통계 모델. `ProcessedDies`, `GoodCount`, `NgCount`, `BinDistribution`,
  `Duration`(= FinishedAt-StartedAt), `RecordDie(bin, isGood)`.
- **`Equipment/Lots/LotStorage.cs`** — `static ActiveLot`, `OpenLot/CloseLot`, `event ActiveLotChanged`.

## Existing capabilities
- 작업 MAIN 화면 자체 갱신 루프(500ms, 가시성 연동)는 이미 구현됨.
- 컨트롤러가 사이클 진행(`CycleDone/Total`), 양·불량(`GoodCount/NgCount`), 픽/플레이스 실패, 콜릿·니들 사용수를 누적함.
- 컨트롤러에 down-time / MTBF / MTTR 필드는 **존재**하나 실제 누적 로직 확인 필요(현재 대부분 Zero로 보임).

## 핵심 결함 (Stage 3에서 갭으로 정리)

1. **표시 소스 불일치 — 가장 큰 문제.**
   `WorkMainPage.BuildDisplaySnapshot()`는 카운트를 **`LotStorage.ActiveLot.ProcessedDies` / `GoodCount`** 에서 읽음.
   그러나 런타임에 이 두 필드를 증가시키는 코드가 **존재하지 않음** (`Lot.RecordDie`는 시퀀스에서 호출 안 됨;
   실측 카운트는 `MachineController.CycleDone/GoodCount/NgCount`에만 있음).
   → 결과: **UPH = 0.00, 가동률 = 0.00%, Total chip = 0** 으로 항상 비어 보임 (스크린샷 그대로).

2. **사이클 타임 측정 위치/방식 오류.**
   현재 `WorkMainPage`가 500ms 폴링 틱 안에서 `ProcessedDies` 증가분을 보고 wall-clock 차이를 나눠 추정.
   - 폴링 주기(500ms)에 종속 → 실제 사이클 시간이 아니라 "폴링 간격 ÷ 그 사이 처리량"이 됨.
   - `_cycleAccMs/_cycleSamples`가 **무한 누적** → 현재 속도가 아닌 전체 평균. 가속/감속 반영 못 함.
   - 애초에 `ProcessedDies`가 안 늘어나므로(#1) 분기 자체가 동작 안 함 → CYCLE TIME = 0 ms.

3. **UPH 계산이 사이클 타임과 분리됨.**
   `uph = GoodCount * 3600 / upTime.TotalSeconds` 이고 `upTime = lot.Duration`(정지 포함). 사이클 타임과 독립 →
   두 값이 서로 어긋날 수 있음. 정의(순수 가동시간 vs 부하시간) 미확정.

4. **가동률(Rate) 정의 오류.**
   `rate = GoodCount / ProcessedDies * 100` → 이건 **수율(Yield)** 이지 설비 가동률이 아님.
   가동률은 통상 `가동시간 / 부하시간`.

5. **down-time / MTBF / MTTR 누적 로직 부재(의심).**
   필드는 있으나 상태 전이(AutoRunning↔Alarm↔Stopped) 기반 시간 누적 엔진이 안 보임 → 항상 00:00:00.

## Notable patterns / conventions (Stage 4 설계가 따라야 할 것)
- 시퀀스는 `async Task` 기반. 모션은 `MoveAxisCommandAndWaitAsync` 등 `Task<int>`(0 성공/-1 실패).
- UI 갱신은 **스냅샷 빌드 → diff apply** 패턴(이미 정착). 새 구조도 이 패턴 유지.
- 비-UI 스레드 → UI는 `BeginInvoke`. 표시 전용 타이머는 가시성에 연동.
- 통계 카운터는 컨트롤러가 `internal set`으로 보유. 외부(UI)는 읽기만.
- 예외는 try/catch + 로그(`EventLogger`/`Log`). `catch{}` 무시 금지(단 표시 갱신의 transient 실패는 예외적으로 무시 허용).
