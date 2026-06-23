# CDT-320 Design — 작업 시간/UPH 통계 엔진 + 무지연 표시

근거: `03_gap_analysis.md`. 목표: 사이클 타임·UPH를 **정확히 계산**하고 작업 MAIN에 **UI 무지연**으로 표시.

## 0. 설계 원칙 (한 줄)
> **계산은 시퀀스/상태 스레드에서 1곳(엔진)에, 표시는 UI 타이머에서 스냅샷 읽기만.**
> UI는 절대 계산하지 않는다. 엔진은 절대 UI를 만지지 않는다.

---

## 1. 아키텍처 개요

```
[오토 사이클 루프]            [상태 전이]                [UI 500ms 타이머]
MachineController            SetStatus(...)              WorkMainPage
   │ OnCycleCompleted(n,good)   │ OnStateChanged(old,new)    │ GetSnapshot()  ← 읽기 전용
   ▼                            ▼                            ▼
        ProductionStatsEngine (단일 인스턴스, thread-safe)
        - 사이클 타임(rolling) / UPH / 가동·정지 시간 / MTBF·MTTR 누적
        - 원자적 스냅샷 발행 (volatile reference swap)
```

- **신규: `Equipment/Stats/ProductionStatsEngine.cs`** — 모든 생산 통계의 단일 소유자.
- **신규: `Equipment/Stats/ProductionStatsSnapshot.cs`** — 불변(immutable) 표시용 스냅샷 DTO.
- `MachineController`는 사이클/상태 이벤트를 엔진에 **먹이기만** 한다.
- `WorkMainPage`는 `controller.Stats.GetSnapshot()` 한 번 호출로 끝 → diff apply(기존 패턴 유지).

이렇게 하면 R-003(소스 일치), R-001/R-002(정확 계산), R-007(무지연)이 한 번에 풀린다.

---

## 2. 무지연(non-blocking)의 핵심: lock-free 스냅샷

문제: UI 타이머가 엔진 내부를 읽을 때 사이클 스레드가 쓰고 있으면 락 경합 → UI 멈춤(끊김).

해법: **엔진은 내부 상태를 갱신할 때마다 불변 스냅샷 객체를 새로 만들어 `volatile` 필드에 통째로 교체(reference swap)한다.**
UI는 그 참조 하나만 읽는다. 읽기/쓰기 모두 락 없음 → UI 절대 안 막힘.

```csharp
// ProductionStatsEngine.cs (핵심만)
private volatile ProductionStatsSnapshot _current = ProductionStatsSnapshot.Empty;

public ProductionStatsSnapshot GetSnapshot() => _current;   // UI에서 호출. lock 없음.

private void Publish()        // 엔진 내부 갱신 끝에서 호출
{
    _current = BuildImmutableSnapshot();   // 새 객체로 원자 교체
}
```

- 쓰기 측(엔진 내부 누적)은 짧은 `lock(_sync)`로 보호하되, **UI는 그 락에 절대 안 들어감.**
- 스냅샷은 문자열이 아니라 **숫자(원시값)** 로 보관. 문자열 포맷팅은 UI의 `ApplyDisplaySnapshot`에서 수행
  (포맷 변경이 엔진에 영향 없게).

---

## 3. 사이클 타임 — 정확한 측정 (R-001)

측정 지점: `MachineController` 사이클 루프(6699행)의 **1 사이클 완료 직후**.

```csharp
// MachineController.cs — 사이클 루프
var swCycle = System.Diagnostics.Stopwatch.StartNew();   // 루프 진입 전 1회
for (int cyc = startCycle; cyc < totalCycles; cyc++)
{
    await DoOneDieAsync(cyc, totalCycles, _cycleCts.Token);
    CycleDone = Math.Min(totalDies, (cyc+1)*pickers);

    long cycleMs = swCycle.ElapsedMilliseconds;   // 이번 사이클 소요
    swCycle.Restart();
    int diesThisCycle = Math.Min(pickers, totalDies - cyc*pickers);
    Stats.OnCycleCompleted(diesThisCycle, goodThisCycle, ngThisCycle, cycleMs);  // ← 엔진에 먹임

    RaiseProgress();
    ...
}
```

엔진의 사이클 타임 산출(3가지 동시 보관, UI에서 택일 표시):
- **Instant**: 직전 1 사이클 소요 / 다이수 = 다이당 ms (현재 속도).
- **Rolling(권장 표시값)**: 최근 N개(예: 20 다이) 이동 평균 → 화면 깜빡임 적고 현재 속도 반영.
  순환 버퍼(`long[] _ring`)로 O(1) 갱신.
- **Cumulative**: 전체 평균 (로트 요약용).

> 화면 "CYCLE TIME (ms)"에는 **Rolling 평균(다이당 ms)** 표시를 권장. (Open Q1)

---

## 4. UPH — 사이클 타임과 일관 (R-002)

UPH는 두 가지 정의가 있고, 사이클 타임과 **같은 분모**를 쓰면 어긋나지 않는다.

- **정의 A (순간/이론 UPH)** = `3600 / (rolling 다이당 초)` → 현재 사이클 타임과 직접 일치. 속도계 느낌.
- **정의 B (실효 UPH)** = `양품수 × 3600 / 가동시간(초)` → 정지·복귀 포함 실제 생산성.

권장: 화면 UPH = **정의 A(순간)**, 그리고 정의 B는 가동률/로트요약과 함께 별도. (Open Q2)
두 값 모두 스냅샷에 담아 두고 UI가 선택.

---

## 5. 가동/정지 시간 & 가동률 & MTBF/MTTR (R-004, R-005, R-006)

상태 전이(`SetStatus`)에서 엔진에 시간 구간을 적립한다. 엔진이 `EquipmentStatus` 별 누적 시간을 관리.

```csharp
// MachineController.SetStatus(newStatus) 안에서
Stats.OnStateChanged(_status /*old*/, newStatus, DateTime.UtcNow);
```

엔진 내부 시간 버킷:
- **부하 시간(Load)** = 사이클 Start ~ 현재(또는 종료). 분모.
- **가동 시간(Up)** = AutoRunning 상태 누적.
- **연속 가동(Cont Up)** = 마지막 정지 이후 AutoRunning 연속 구간.
- **통상 정지(Normal Down)** = Stopped/CycleStopped(운전자 정지) 누적.
- **이상 정지(Error Down)** = Alarm 상태 누적.
- **이상 복귀(Recovery)** = Alarm 해제 후 재가동까지.
- **이상 정지 횟수** = Alarm 진입 횟수.

산출식:
- **가동률(%)** = `가동시간 / 부하시간 × 100`  ← 현재 수율 오류를 이걸로 교체 (R-006).
- **MTBF** = `가동시간 / 이상정지횟수` (고장 간 평균 가동).
- **MTTR** = `이상정지시간 / 이상정지횟수` (평균 수리시간).

> 시간 적립은 "이벤트 시점에 직전 구간을 더하는" 방식(상태 enter 시각만 기억) → 타이머 폴링 불필요, 정확.
> 화면의 흐르는 초까지 보이려면 UI 타이머가 `now - 현재상태진입시각`만 더해 표시(엔진은 확정 구간만 보관).

---

## 6. 신규/수정 파일

**신규**
- `Equipment/Stats/ProductionStatsEngine.cs` — 통계 엔진(thread-safe, lock-free 스냅샷 발행).
- `Equipment/Stats/ProductionStatsSnapshot.cs` — 불변 스냅샷 DTO(숫자 필드).

**수정**
- `Equipment/MachineController.cs`
  - `public ProductionStatsEngine Stats { get; }` 추가, 생성자에서 init.
  - 사이클 루프: 사이클별 `Stopwatch` + `Stats.OnCycleCompleted(...)`.
  - `SetStatus(...)`: `Stats.OnStateChanged(...)`.
  - 사이클 Start/Reset 시 `Stats.BeginLot(lotId)` / 종료 시 `Stats.EndLot()`.
- `Ui/Pages/Work/WorkMainPage.cs`
  - `BuildDisplaySnapshot()`에서 `lot.GoodCount/ProcessedDies` 대신 `controller.Stats.GetSnapshot()` 사용.
  - 사이클 타임/UPH/가동률 계산 코드(214~238행) 제거 → 엔진 스냅샷 값 포맷만.
  - 기존 타이머/diff/가시성 로직은 유지.
- (선택) `Equipment/Lots/Lot.cs` — 로트 종료 시 엔진 최종값을 Lot에 동기화(JSON 저장/이력용).

---

## 7. 스레드 모델 요약 (R-007 보장)
- 엔진 쓰기: 사이클 스레드 / 상태 전이 스레드 (짧은 lock).
- 엔진 읽기: UI 타이머 — `volatile` 참조 1회 읽기, **lock 없음**.
- UI는 컨트롤러/Lot 내부를 직접 순회하지 않음 → 락 경합 0 → 끊김 0.
- 표시 갱신은 화면 가시 시에만(기존 `ShouldRefreshVisible` 유지).

---

## 8. 확정 사항 (2026-06-23 사용자 확정)
- **Q1. CYCLE TIME** = **다이당 ms (Rolling 평균)** 표시. ✅
- **Q2. UPH** = **순간 + 실효 둘 다 산출**, 화면은 선택 표시(기본 순간). ✅
- **Q3. 부하 시간 시작** = **사이클 Start 시점**. ✅
- **Q4. Rolling 윈도우** = 최근 20 다이 (기본값, 추후 설정화 가능).
- **Q5. MTBF/MTTR** = 화면처럼 hh:mm:ss 유지.

→ Stage 5(체크리스트) 작성 후 사용자 확인. 그 다음 Stage 6(구현).
