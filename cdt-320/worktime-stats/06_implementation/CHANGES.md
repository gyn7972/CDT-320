# 06 구현 변경 내역 — 작업 시간/UPH 통계 엔진

근거: `04_design.md`, `05_checklist.md`, 구현 지시 프롬프트(2026-06-23 확정사양).
원칙: **계산은 엔진 1곳(시퀀스/상태 스레드), 표시는 UI가 lock-free 스냅샷 읽기만.**

## 파일별 변경

### 신규 `QMC.CDT-320/Equipment/Stats/ProductionStatsSnapshot.cs`  (체크리스트 A)
- 불변(immutable) 표시용 DTO. 숫자/원시값만 보관(문자열 포맷 없음).
- 필드: `ProcessedDies/GoodCount/NgCount`(int), `CycleMsPerDieRolling/CycleMsPerCycleInstant`(double),
  `UphInstant/UphEffective`(double), `LoadSeconds/UpSeconds/ContUpSeconds/NormalDownSeconds/ErrorDownSeconds/RecoverySeconds`(double),
  `ErrorCount`(int), `MtbfSeconds/MttrSeconds/UptimeRatePercent`(double), `ActiveLotId`(string).
- `public static readonly ProductionStatsSnapshot Empty` (전부 0/"").
- 생성자 주입 후 읽기 전용 프로퍼티 → 생성 후 불변.

### 신규 `QMC.CDT-320/Equipment/Stats/ProductionStatsEngine.cs`  (체크리스트 B)
- `private readonly object _sync`, `private volatile ProductionStatsSnapshot _current = Empty`.
- `public ProductionStatsSnapshot GetSnapshot() => _current;` — **lock 없음**(UI 호출).
- 쓰기는 모두 `lock(_sync)` 안에서 수행하고 끝에 `PublishLocked(utcNow)`로 새 스냅샷 원자 교체(reference swap).
- 내부 작업변수: 카운트, Rolling 순환버퍼 `long[20]`+index+합계(O(1)), 상태별 누적 `TimeSpan` 버킷,
  현재 상태/진입 UTC, 부하 시작·종료 UTC, 연속가동·복귀 시작 UTC, `_afterAlarm`, ErrorCount, ActiveLotId.
- `BeginLot(lotId,totalDies)`: 작업변수 리셋 + 부하시작=now(사이클 Start 기준) + 진입시각=now.
- `EndLot()`: 진행 중 구간 적립 + 부하종료=now 확정.
- `OnCycleCompleted(diesInCycle,good,ng,cycleMs)`: 카운트 누적 → Rolling에 `cycleMs/diesInCycle` 적립 →
  Instant 갱신 → Publish.
- `OnStateChanged(old,now,utcNow)`: old 상태 구간을 버킷(Up/NormalDown/ErrorDown)에 적립 →
  Alarm 진입 시 ErrorCount++ → Alarm 이탈 시 복귀시계 시작 → AutoRunning 재진입 & 직전 Alarm이면 Recovery 적립 →
  진입시각=utcNow → Publish.
- `PublishLocked`: 확정 버킷 + **진행 중 구간**을 합쳐 라이브 값 산출(0 division 가드) →
  UphInstant=`3600000/다이당ms`, UphEffective=`good×3600/UpSec`, UptimeRate=`Up/Load×100`,
  Mtbf=`Up/ErrorCount`, Mttr=`ErrorDown/ErrorCount`.
- 모든 public 메서드 `try/catch` + 실패 시 `QMC.Common.Log.Write("WorkStats","ProductionStatsEngine",...)`. `catch{}` 없음.
- **UI 타입(Control/Form) 참조 없음.**

### 수정 `QMC.CDT-320/Equipment/MachineController.cs`  (체크리스트 C)
- (line ~270) `public QMC.CDT320.Stats.ProductionStatsEngine Stats { get; } = new ...();` 추가.
- `SetStatus(s)` (line ~7847): `old` 보관 후 `Stats.OnStateChanged(old, s, DateTime.UtcNow)` 호출. 기존 StatusChanged 이벤트 유지.
- `CycleRunAsync` 로트 결정 직후(SetStatus(AutoRunning) 직전): `Stats.BeginLot(lotId, totalDies)`.
- 사이클 루프: 진입 전 `Stopwatch.StartNew()` 1회. 각 사이클에서 `goodBefore/ngBefore` 기억 → `DoOneDieAsync` →
  `CycleDone` 갱신 → `cycleMs=sw.ElapsedMilliseconds; sw.Restart()` → `goodDelta/ngDelta` 산출 →
  `Stats.OnCycleCompleted(diesInCycle, goodDelta, ngDelta, cycleMs)` → `RaiseProgress()`.
- 3개 종료 경로(정상 완료 / 취소-abort / 예외)에서 `LotStorage.CloseLot(...)` 직후 `Stats.EndLot()` 호출.
  (CycleStop/일시정지(resume pending) 경로는 EndLot 미호출 — 로트 계속 진행.)
- **기존 CycleDone/GoodCount/NgCount/CycleProgress 동작 변경 없음(회귀 방지).** Stats 호출은 모두 `try{}catch{}`로 격리.

### 수정 `QMC.CDT-320/Ui/Pages/Work/WorkMainPage.cs`  (체크리스트 D)
- `using QMC.CDT320.Stats;` 추가.
- 폴링/수율 계산용 필드 제거: `_runStart, _lastDone, _cycleAccMs, _cycleSamples, _lastCycleStartTicks`.
- `BuildDisplaySnapshot()`: `ProductionStatsSnapshot stats = ctrl?.Stats?.GetSnapshot() ?? Empty;` 1회 읽기.
  - TotalChip/StageInfo frame ← `stats.ProcessedDies`
  - BinQty ← `stats.GoodCount`
  - Load/Up/ContUp/NormDown/ErrDown/Recovery ← `FormatTs(TimeSpan.FromSeconds(stats.*Seconds))`
  - ErrCnt ← `stats.ErrorCount + " ea"`
  - UPH ← `stats.UphInstant.ToString("F2")` (실효는 스냅샷에 함께 보관)
  - Mtbf/Mttr ← `FormatTs(TimeSpan.FromSeconds(stats.Mtbf/MttrSeconds))`
  - CYCLE TIME ← `((int)Math.Round(stats.CycleMsPerDieRolling)) + " ms"`
  - 가동률 ← `stats.UptimeRatePercent.ToString("F2") + " %"` (수율 계산 제거)
  - LOT ID ← `stats.ActiveLotId` (빈 값이면 "(no lot)")
- 기존 `Timer _refresh` / `SetText` diff / `ShouldRefreshVisible` / `ActiveLotChanged`+`BeginInvoke` 유지.
- `lot`은 Bin 분포(BinNum)와 Project fallback에만 사용. 시간/카운트는 컨트롤러/Lot 직접 순회 없이 스냅샷만 읽음.

### 수정 `QMC.CDT-320/QMC.CDT-320.csproj`
- 신규 두 파일 `<Compile Include>` 등록(`Equipment\Stats\ProductionStatsSnapshot.cs`, `...Engine.cs`).

### 미구현(선택 항목 E — `Equipment/Lots/Lot.cs` 이력 동기화)
- 본 구현 범위에서 제외. 엔진 확정값을 Lot 통계/JSON에 반영하는 작업은 후속 과제로 남김.

## 빌드/검증 요약
- `MSBuild QMC.CDT-320.csproj`(별도 OutDir) **성공**. 신규/수정 코드发 신규 경고 0(나머지 경고는 기존 무관 파일).
- 엔진 단위 시뮬레이션(빌드 산출물 reflection 호출)으로 정확성 확인 — 상세 `07_verification.md` 참조.
