# 07 검증 — 작업 시간/UPH 통계 엔진

`05_checklist.md` 각 항목을 코드 근거(파일:라인) + 빌드/시뮬레이션 결과로 검증한다.
파일 경로는 repo 루트 기준.

## A. ProductionStatsSnapshot (불변 DTO)
| 항목 | 결과 | 근거 |
|---|---|---|
| 신규 파일 생성 | ✅ | `QMC.CDT-320/Equipment/Stats/ProductionStatsSnapshot.cs` |
| 모든 필드 숫자/원시값 | ✅ | ProcessedDies/GoodCount/NgCount(int) — Snapshot.cs:66-71 |
| CycleMsPerDieRolling/Instant(double) | ✅ | Snapshot.cs:73-76 |
| UphInstant/UphEffective 둘 다 | ✅ | Snapshot.cs:78-81 |
| Load/Up/ContUp/NormalDown/ErrorDown/Recovery Seconds | ✅ | Snapshot.cs:83-94 |
| ErrorCount/Mtbf/Mttr/UptimeRate/ActiveLotId | ✅ | Snapshot.cs:96-105 |
| 정적 `Empty` 제공 | ✅ | Snapshot.cs:15-22 |
| 클래스 불변(읽기 전용) | ✅ | 전 프로퍼티 `{ get; }` 생성자 주입, Snapshot.cs:24-64 |

## B. ProductionStatsEngine (단일 엔진)
| 항목 | 결과 | 근거 |
|---|---|---|
| 신규 파일 생성 | ✅ | `QMC.CDT-320/Equipment/Stats/ProductionStatsEngine.cs` |
| `volatile _current = Empty` | ✅ | Engine.cs:30 |
| `GetSnapshot() => _current` (lock 없음) | ✅ | Engine.cs:62-65 |
| 쓰기 `lock(_sync)` + 끝에 Publish | ✅ | BeginLot/EndLot/OnCycleCompleted/OnStateChanged 전부 `lock(_sync)` 후 `PublishLocked` |
| BeginLot(lotId,totalDies) 리셋+부하시작 | ✅ | Engine.cs:68-117 (부하시작=now, Engine.cs:97) |
| EndLot 확정 마감 | ✅ | Engine.cs:120-143 (`_loadEndUtc=now`, Engine.cs:131) |
| OnCycleCompleted: 카운트 누적 | ✅ | Engine.cs:152-159 |
| Rolling 순환버퍼(20) 다이당ms O(1) | ✅ | Engine.cs:161-172 (`perDie=cycleMs/diesInCycle`, ring sum 교체) |
| CycleMsPerCycleInstant=cycleMs | ✅ | Engine.cs:174 |
| UphInstant=3600000/다이당ms (0가드) | ✅ | Engine.cs:300 |
| OnStateChanged: old 구간 버킷 적립 | ✅ | Engine.cs:193 → `AccumulateCurrentSegmentLocked` Engine.cs:240-256 |
| Alarm 진입 ErrorCount++ | ✅ | Engine.cs:206-207 |
| AutoRunning 재진입 ContUp 시작 / 직전 Alarm이면 Recovery 적립 | ✅ | Engine.cs:210-218 |
| Publish 호출 | ✅ | 각 메서드 말미 `PublishLocked(...)` |
| UphEffective=good×3600/UpSec (0가드) | ✅ | Engine.cs:301 |
| UptimeRate=Up/Load×100 (0가드) | ✅ | Engine.cs:302 |
| Mtbf=Up/ErrCnt, Mttr=ErrDown/ErrCnt (0가드) | ✅ | Engine.cs:303-304 |
| 모든 public try/catch + Log | ✅ | BeginLot/EndLot/OnCycleCompleted/OnStateChanged 각각 `catch(Exception ex){ Log.Write(...) }` |
| UI 타입 참조 없음 | ✅ | using = `System`, `QMC.Common`만 (Engine.cs:1-2). Control/Form 없음 |

## C. MachineController 연동
| 항목 | 결과 | 근거 |
|---|---|---|
| `Stats` 프로퍼티 + 초기화 | ✅ | MachineController.cs:270-271 (`= new ...()` 자동 초기화) |
| 사이클 Start에서 BeginLot | ✅ | CycleRunAsync, `Stats.BeginLot(lotId, totalDies)` (SetStatus(AutoRunning) 직전) |
| 사이클 루프 Stopwatch + OnCycleCompleted | ✅ | `swCycle = Stopwatch.StartNew()` 루프 진입 전 1회, 각 사이클 `Stats.OnCycleCompleted(...)` |
| good/ng delta 전달(기존 카운트 활용) | ✅ | `goodBefore/ngBefore` → `goodDelta/ngDelta = Max(0, after-before)` |
| SetStatus에서 OnStateChanged | ✅ | MachineController.cs:7847 부근 `Stats.OnStateChanged(old, s, DateTime.UtcNow)` |
| 종료 시 EndLot | ✅ | 3개 CloseLot 직후 `Stats.EndLot()` (정상/abort/예외) |
| 기존 CycleDone/GoodCount/NgCount 동작 무변경 | ✅ | 기존 라인 그대로, Stats 호출만 `try{}catch{}`로 추가. 빌드/회귀에서 기존 카운트 로직 변경 없음 |

## D. WorkMainPage 표시 전환
| 항목 | 결과 | 근거 |
|---|---|---|
| GetSnapshot() 1회 호출로 취득 | ✅ | WorkMainPage.cs BuildDisplaySnapshot `ctrl?.Stats?.GetSnapshot() ?? Empty` |
| 폴링 추정 코드 제거 | ✅ | `_lastDone/_cycleAccMs/_cycleSamples/_lastCycleStartTicks` 필드+로직 삭제 (grep 결과 0건) |
| CYCLE TIME = CycleMsPerDieRolling("0 ms") | ✅ | `((int)Math.Round(stats.CycleMsPerDieRolling)) + " ms"` |
| UPH = UphInstant(F2), 실효 보조 | ✅ | `stats.UphInstant.ToString("F2")` (UphEffective 스냅샷 보관) |
| 가동률 = UptimeRatePercent (수율 제거) | ✅ | `stats.UptimeRatePercent.ToString("F2")+" %"` — 기존 Good/Processed 수율 계산 삭제 |
| Total/Good/Bin = 스냅샷 카운트 | ✅ | TotalChip=`stats.ProcessedDies`, BinQty=`stats.GoodCount` (lot.ProcessedDies 의존 제거) |
| 시간 6종 = *Seconds → FormatTs | ✅ | `FormatTs(TimeSpan.FromSeconds(stats.*Seconds))` |
| 흐르는 초 (선택) | ⏭️ | 1차 구현 생략(설계상 선택). 엔진은 사이클/상태 이벤트마다 in-progress 구간 포함 발행 → 오토 중 갱신됨 |
| 이상정지횟수/MTBF/MTTR = 스냅샷 | ✅ | ErrCnt=`stats.ErrorCount`, Mtbf/Mttr=`FormatTs(FromSeconds(...))` |
| LOT ID = ActiveLotId | ✅ | `stats.ActiveLotId` (빈 값 fallback "(no lot)") |
| 기존 타이머/diff/가시성 유지 | ✅ | `_refresh`, `SetText` diff, `ShouldRefreshVisible`, `OnActiveLotChanged`+`BeginInvoke` 그대로 |
| UI 스레드 직접 순회 없음(락 경합 0) | ✅ | UI는 `GetSnapshot()`(volatile 1회 읽기)만. 컨트롤러/Lot 내부 순회 없음 |

## E. 빌드/회귀
| 항목 | 결과 | 근거 |
|---|---|---|
| QMC.CDT-320 빌드 통과 | ✅ | `MSBuild` 별도 OutDir 성공 → `QMC.CDT-320.exe` 생성 |
| 신규 코드 경고 0 | ✅ | 빌드 경고는 전부 기존 무관 파일(InputCassettePage/AlgorithmNode 등) 및 MachineController.cs:3744(기존 unreachable). 신규 2파일/수정부 신규 경고 없음 |
| `verify_handler_features.pl` | ⚠️ N/A | 해당 스크립트가 repo의 `tools/`에 존재하지 않음(CLAUDE.md 명세와 실제 스크립트셋 상이) |
| 통합 회귀 `verify_all.pl` | ✅(무관 실패) | 실패 15건 전부 **QMC.Vision** 파일(SpcChartPage/AlgorithmNode/RecipePage 등) 및 Vision exe 미실행 케이스 → 본 변경(QMC.CDT-320)과 무관, 기존 환경 드리프트 |
| auto-cycle 후 CYCLE TIME/UPH/가동률 ≠ 0 | ✅ | 빌드 산출물 엔진 시뮬레이션(아래) — 모두 정상 비0 산출 |
| 오토 중 UI 끊김 없음(체감) | ✅(설계 보장) | UI는 lock-free volatile 읽기만(쓰기 lock에 미진입). 실 GUI 체감은 본 환경 미확인 — 후속 현장 확인 권장 |

## 엔진 정확성 시뮬레이션 (빌드 산출물 reflection 호출)

### 시나리오 1 — 정상 오토(10다이=4+4+2, cycle 480ms, 실시간)
```
ActiveLotId       = LOT-TEST
ProcessedDies     = 10        GoodCount = 10
CycleMsPerDieRoll = 160       (480/4, 480/4, 480/2 평균 = 160) ✓
CycleMsInstant    = 480       (직전 사이클 ms) ✓
UphInstant        = 22500     (= 3600000/160) ✓
UphEffective      = 54522.9   (= 10×3600/0.66초, 짧은 실행이라 큼) ✓
LoadSeconds       = 0.662     UpSeconds = 0.660
UptimeRatePercent = 99.70 %   (가동/부하, 수율 아님) ✓
ErrorCount        = 0
```
→ **이전 결함(항상 0) 해소 확인**: 카운트·CYCLE TIME·UPH·가동률 모두 정상 비0.

### 시나리오 2 — Alarm/복귀 포함(주입 타임스탬프로 구간 검증)
```
UpSeconds        = 30   (20+10 AutoRunning) ✓
NormalDownSeconds= 5    (Stopped) ✓
ErrorDownSeconds = 8    (Alarm) ✓
RecoverySeconds  = 5    (Alarm해제→재가동) ✓
ContUpSeconds    = 10   (마지막 연속 가동) ✓
ErrorCount       = 1    MtbfSeconds = 30 (Up/1) ✓  MttrSeconds = 8 (ErrDown/1) ✓
```
→ 상태별 버킷/ErrorCount/MTBF/MTTR/Recovery 모두 정확.
주: 본 시나리오의 Load/UptimeRate 비정상값은 **테스트 한정 아티팩트**(OnStateChanged에 가짜 시각 주입 +
BeginLot/EndLot은 실 wall-clock 사용). 실 시스템은 `SetStatus`가 항상 `DateTime.UtcNow`를 전달해
Load와 동일 시계 → 시나리오 1처럼 가동률 ≤ 100%로 정상.

## 미구현 / 후속
- **F(선택) Lot 이력 동기화**: 미구현(선택 항목). 후속 과제.
- **흐르는 초(D 선택)**: 미구현. 엔진이 이벤트마다 in-progress 포함 발행하므로 오토 중 갱신은 됨.
- **실 GUI 체감 무끊김**: 설계상 보장(lock-free)되나 현장 GUI 실행 체감은 본 환경에서 미확인.
- `_stats_verify/` 임시 빌드 폴더는 AGENTS 규칙(임시 검증 폴더 임의 삭제 금지)에 따라 남겨둠.
