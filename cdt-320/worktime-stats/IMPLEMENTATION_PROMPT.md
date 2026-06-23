# 구현 지시 프롬프트 — 작업 MAIN "작업 시간/UPH" 통계 엔진

> 이 프롬프트를 Claude Code에 그대로 전달해 구현을 진행한다.
> 사전 분석/설계 산출물: `cdt-320/worktime-stats/02_code_analysis.md`, `03_gap_analysis.md`,
> `04_design.md`, `05_checklist.md`. 작업 전 **반드시 `AGENTS.md`를 먼저 읽고** 그 규칙을 따른다.

---

## 0. 작업 목표 (한 문장)
오토 사이클(`MachineController`) 진행 중 작업 MAIN(`WorkMainPage`)의 "작업 시간" 패널에
**사이클 타임·UPH·가동률·가동/정지 시간**이 정확히 계산되어 **UI 끊김 없이** 표시되게 한다.

## 1. 현재 결함 (반드시 이해하고 시작)
- `WorkMainPage.BuildDisplaySnapshot()`가 카운트를 `LotStorage.ActiveLot.ProcessedDies/GoodCount`에서 읽는데,
  이 필드는 오토 사이클 중 **증가되지 않는다**(실측은 `MachineController.CycleDone/GoodCount/NgCount`).
  → UPH·가동률·칩수가 항상 0.
- 사이클 타임은 UI 500ms 폴링 틱에서 추정 → 폴링 종속 + 무한 누적 평균 → 부정확, 사실상 0 ms.
- 가동률이 수율(Good/Processed)로 잘못 계산됨.

## 2. 설계 원칙 (절대 규칙)
> **계산은 시퀀스/상태 스레드의 단일 엔진 1곳에서. 표시는 UI가 lock-free 스냅샷 읽기만.**
> UI는 절대 계산하지 않고, 엔진은 절대 UI(Control/Form)를 참조하지 않는다.

무지연 보장 = 엔진이 갱신마다 **불변 스냅샷**을 새로 만들어 `volatile` 필드에 통째 교체(reference swap).
UI는 그 참조 하나만 읽음 → 락 경합 0.

## 3. 확정 사양 (사용자 확정, 2026-06-23)
- **CYCLE TIME** = 다이당 ms, **Rolling 평균(최근 20 다이)** 표시.
- **UPH** = 순간(`3600000 / 다이당ms`)과 실효(`양품×3600 / 가동초`) **둘 다 산출**, 화면 기본은 **순간**.
- **부하 시간** 시작 = **사이클 Start 시점**. 가동률 = 가동시간/부하시간 × 100.
- **MTBF/MTTR** = hh:mm:ss 유지. MTBF=가동초/이상정지횟수, MTTR=이상정지초/이상정지횟수.

---

## 4. 구현 항목 (순서대로, 각 항목은 `05_checklist.md` 대응)

### A. 신규 `Equipment/Stats/ProductionStatsSnapshot.cs`
- 불변(immutable) DTO. **숫자/원시값만** 보관(문자열 포맷 금지 — 포맷은 UI 책임).
- 필드: `ProcessedDies, GoodCount, NgCount`(int) /
  `CycleMsPerDieRolling, CycleMsPerCycleInstant`(double) /
  `UphInstant, UphEffective`(double) /
  `LoadSeconds, UpSeconds, ContUpSeconds, NormalDownSeconds, ErrorDownSeconds, RecoverySeconds`(double) /
  `ErrorCount`(int) / `MtbfSeconds, MttrSeconds, UptimeRatePercent`(double) / `ActiveLotId`(string).
- `public static readonly ProductionStatsSnapshot Empty`(전부 0/"").
- 생성 후 불변(읽기 전용 프로퍼티).

### B. 신규 `Equipment/Stats/ProductionStatsEngine.cs`
- 필드: `private readonly object _sync = new object();`,
  `private volatile ProductionStatsSnapshot _current = ProductionStatsSnapshot.Empty;`
- `public ProductionStatsSnapshot GetSnapshot() => _current;`  // **lock 없음**
- 내부 누적 상태(작업 변수): 카운트, Rolling 순환버퍼 `long[20]` + index/합계, 상태별 누적 TimeSpan,
  현재 상태와 그 진입 UTC 시각, 부하 시작 UTC, ErrorCount, 직전 상태가 Alarm이었는지 등.
- `public void BeginLot(string lotId, int totalDies)`: 작업변수 리셋, 부하 시작=now, 현재상태 진입시각=now.
- `public void EndLot()`: 현재 구간 마감 후 확정.
- `public void OnCycleCompleted(int diesInCycle, int good, int ng, long cycleMs)`:
  카운트 누적 → Rolling 버퍼에 `cycleMs/diesInCycle` 적립(O(1)) → 순간/실효 UPH 재계산 → `Publish()`.
- `public void OnStateChanged(EquipmentStatus old, EquipmentStatus now, DateTime utcNow)`:
  `(utcNow - 현재상태진입시각)`을 old 상태 버킷에 적립 → 진입시각=utcNow →
  Alarm 진입 시 ErrorCount++ → AutoRunning 재진입 & 직전 Alarm이면 Recovery 적립 →
  파생값(가동률/MTBF/MTTR) 재계산 → `Publish()`.
- `private void Publish()`: 현 작업변수로 **새** `ProductionStatsSnapshot`을 만들어 `_current`에 대입.
  (쓰기는 `lock(_sync)` 안에서 수행. `GetSnapshot`은 락 밖.)
- 모든 public 메서드 `try/catch` + 실패 시 기존 로깅(`Log`/`EventLogger`) 사용. `catch{}` 금지.
- **UI 타입 참조 절대 금지.**

### C. `Equipment/MachineController.cs` 연동
- `public ProductionStatsEngine Stats { get; } = new ProductionStatsEngine();` 추가(또는 생성자 init).
- `CycleRunAsync` 시작부(로트 결정 직후, ~6668~6690행 부근)에서 `Stats.BeginLot(lotId, totalDies);`.
- 사이클 루프(~6699행)에 `var sw = Stopwatch.StartNew();`를 루프 진입 전 1회. 각 사이클:
  ```csharp
  await DoOneDieAsync(cyc, totalCycles, _cycleCts.Token);
  CycleDone = Math.Min(totalDies, (cyc+1)*pickers);
  long ms = sw.ElapsedMilliseconds; sw.Restart();
  int diesThisCycle = Math.Min(pickers, totalDies - cyc*pickers);
  // good/ng delta는 기존 GoodCount/NgCount 누적분의 증가량으로 산출
  Stats.OnCycleCompleted(diesThisCycle, goodDelta, ngDelta, ms);
  RaiseProgress();
  ```
  good/ng delta는 사이클 전후 `GoodCount`/`NgCount` 값을 기억해 차이로 계산(기존 동작 변경 금지).
- `SetStatus(...)` 내부에서 상태 실제 변경 시 `Stats.OnStateChanged(old, newStatus, DateTime.UtcNow);`.
- 사이클 정상 종료/중단/STOP 경로에서 `Stats.EndLot();`.
- **기존 CycleDone/GoodCount/NgCount/CycleProgress 동작은 그대로 유지(회귀 금지).**

### D. `Ui/Pages/Work/WorkMainPage.cs` 표시 전환
- `BuildDisplaySnapshot()`에서 `host?.Controller?.Stats.GetSnapshot()` 1회 호출.
- **제거**: `_lastDone/_cycleAccMs/_cycleSamples/_lastCycleStartTicks` 및 214~238행 폴링/수율 계산 로직.
- 매핑:
  - CYCLE TIME ← `CycleMsPerDieRolling` → `((int)Math.Round(v)) + " ms"`
  - UPH ← `UphInstant.ToString("F2")` (실효는 보조 라벨/툴팁로 `UphEffective`, 선택)
  - 가동률 ← `UptimeRatePercent.ToString("F2") + " %"`
  - Total chip/Good/Bin ← 스냅샷 카운트 사용
  - 부하/가동/연속가동/통상정지/이상정지/복귀 ← 각 `*Seconds`를 `FormatTs(TimeSpan.FromSeconds(v))`
  - 이상정지횟수 ← `ErrorCount + " ea"`, MTBF/MTTR ← `*Seconds` → FormatTs
  - 작업중 LOT ID ← `ActiveLotId`
- (선택) 흐르는 초: 표시 시 현재 상태 경과분을 UI에서 가산. 복잡하면 1차 구현에선 생략 가능.
- **유지**: `Timer _refresh`, `SetText` diff, `ShouldRefreshVisible`, `ActiveLotChanged` + `BeginInvoke`.
- UI 스레드에서 컨트롤러/Lot 내부를 직접 순회하지 말 것(스냅샷만 읽기).

### E. (선택) `Equipment/Lots/Lot.cs` 이력 동기화
- `EndLot` 시 엔진 확정값을 Lot 통계에 반영 후 `LotStorage.SaveJson`(JSON pretty 유지).

---

## 5. 코딩 규칙 (AGENTS.md 요약 — 위반 금지)
- Designer 규칙: 컨트롤 선언/배치는 `.Designer.cs`, 로직은 `.cs`. Form/UserControl은 `partial`, 클래스명=파일명.
- 예외: 함수는 try/catch/finally 기준. `catch{}` 무시 금지. 실패 시 로그(+UI면 메시지박스/시퀀스면 Alarm).
- 모션/시퀀스: 비동기 `Task<int>`(성공 0/실패 -1). UI 스레드에서 `Thread.Sleep`·무한 while 금지.
- 명명: 컨트롤 prefix(btn/lbl/txt 등), 이벤트 함수 `컨트롤명_이벤트명`, 메소드 동사 시작, bool은 Is/Can/Check.
- 인코딩: 모든 파일 **UTF-8** 저장. 깨진 한글 발견 시 작업 범위 내 복원(불확실하면 사용자 확인).
- JSON: pretty UTF-8(`JsonPrettySerializer.WriteObject`).

## 6. 빌드 & 검증
```powershell
$MSB = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
& $MSB "QMC.Common\QMC.Common.csproj"   /t:Build /p:Configuration=Debug
& $MSB "QMC.CDT-320\QMC.CDT-320.csproj" /t:Build /p:Configuration=Debug
perl tools/verify_handler_features.pl
QMC.CDT-320\bin\Debug\QMC.CDT-320.exe --auto-cycle 10
```
검증 합격 기준:
- 빌드 경고 0.
- `--auto-cycle 10` 후 CYCLE TIME / UPH / 가동률이 **0이 아닌 값**으로 표시.
- 오토 사이클 중 UI 끊김 없음(체감).
- 기존 CycleProgress/카운트 회귀 없음.

## 7. 산출물
- 실제 코드 수정 + `cdt-320/worktime-stats/06_implementation/CHANGES.md`(파일별 변경/대응 체크리스트 항목).
- 구현 후 `05_checklist.md` 각 항목을 `07_verification.md`에 ✅/❌ + 근거(파일:라인)로 검증.
- ❌ 항목은 사용자에게 보고 후 재작업 여부 확인.
