# CDT-320 Implementation Checklist — 작업 시간/UPH 통계 엔진

근거: `04_design.md`(설계 + 확정사항). 각 항목은 Stage 7에서 ✅/❌로 검증 가능한 형태로 작성.
확정값: CYCLE TIME=다이당 ms Rolling(20), UPH=순간+실효 둘 다, 부하시간=사이클 Start 기준, MTBF/MTTR=hh:mm:ss.

## A. ProductionStatsSnapshot (불변 DTO) — R-001~006
- [ ] `Equipment/Stats/ProductionStatsSnapshot.cs` 신규 생성
- [ ] 모든 필드 숫자/원시값 (문자열 포맷 아님): ProcessedDies, GoodCount, NgCount(int)
- [ ] CycleMsPerDieRolling(double), CycleMsPerCycleInstant(double) 포함
- [ ] UphInstant(double), UphEffective(double) 둘 다 포함
- [ ] LoadSeconds, UpSeconds, ContUpSeconds, NormalDownSeconds, ErrorDownSeconds, RecoverySeconds(double)
- [ ] ErrorCount(int), MtbfSeconds, MttrSeconds(double), UptimeRatePercent(double), ActiveLotId(string)
- [ ] 정적 `Empty` 인스턴스 제공 (전부 0)
- [ ] 클래스 불변(읽기 전용 프로퍼티 또는 init 후 변경 없음)

## B. ProductionStatsEngine (단일 엔진) — R-001~007
- [ ] `Equipment/Stats/ProductionStatsEngine.cs` 신규 생성
- [ ] `private volatile ProductionStatsSnapshot _current = ProductionStatsSnapshot.Empty;`
- [ ] `public ProductionStatsSnapshot GetSnapshot() => _current;` — **lock 없음** (UI 호출용)
- [ ] 내부 쓰기는 `lock(_sync)`로 보호, 끝에 `Publish()`로 새 스냅샷 원자 교체
- [ ] `BeginLot(lotId, totalDies)` — 카운터/타이머 리셋, 부하시간 시작시각 기록 (사이클 Start 기준)
- [ ] `EndLot()` — 확정값 마감
- [ ] `OnCycleCompleted(int diesInCycle, int good, int ng, long cycleMs)`:
  - [ ] ProcessedDies/GoodCount/NgCount 누적
  - [ ] Rolling 순환 버퍼(최근 20 다이)에 다이당 ms 적립 → CycleMsPerDieRolling 갱신 (O(1))
  - [ ] CycleMsPerCycleInstant = cycleMs 갱신
  - [ ] UphInstant = 3600000 / CycleMsPerDieRolling (0 division 가드)
  - [ ] Publish() 호출
- [ ] `OnStateChanged(EquipmentStatus old, EquipmentStatus now, DateTime utcNow)`:
  - [ ] old 상태 구간 시간을 해당 버킷(Up/NormalDown/ErrorDown/...)에 적립
  - [ ] Alarm 진입 시 ErrorCount++
  - [ ] AutoRunning 재진입 시 연속가동(ContUp) 시작 갱신, 직전 Alarm이면 Recovery 적립
  - [ ] Publish() 호출
- [ ] UphEffective = GoodCount × 3600 / UpSeconds (0 가드)
- [ ] UptimeRatePercent = UpSeconds / LoadSeconds × 100 (0 가드)
- [ ] MtbfSeconds = UpSeconds / ErrorCount, MttrSeconds = ErrorDownSeconds / ErrorCount (0 가드)
- [ ] 모든 public 메서드 try/catch + 실패 시 Log (AGENTS.md 규칙 준수)
- [ ] UI 타입(Control/Form) 참조 전혀 없음 (엔진은 UI 비의존)

## C. MachineController 연동 — R-001,003,004
- [ ] `public ProductionStatsEngine Stats { get; }` 프로퍼티 추가 + 생성자 초기화
- [ ] 사이클 Start(`CycleRunAsync` 시작부)에서 `Stats.BeginLot(lotId, totalDies)` 호출
- [ ] 사이클 루프에 `Stopwatch` 추가, 1 사이클 완료 직후 `Stats.OnCycleCompleted(...)` 호출
- [ ] good/ng 사이클 단위 집계값을 OnCycleCompleted에 전달 (기존 GoodCount/NgCount delta 활용)
- [ ] `SetStatus(...)` 안에서 `Stats.OnStateChanged(old, new, DateTime.UtcNow)` 호출
- [ ] 사이클 정상/중단 종료 시 `Stats.EndLot()` 호출
- [ ] 기존 CycleDone/GoodCount/NgCount 동작 변경 없음 (회귀 방지)

## D. WorkMainPage 표시 전환 — R-002,003,006,007,008
- [ ] `BuildDisplaySnapshot()`에서 `controller.Stats.GetSnapshot()` 1회 호출로 값 취득
- [ ] 사이클 타임 폴링 추정 코드(_cycleAccMs/_cycleSamples/_lastCycleStartTicks 등) 제거
- [ ] CYCLE TIME 라벨 = CycleMsPerDieRolling 포맷("0 ms")
- [ ] UPH 라벨 = UphInstant 표시(F2), 실효값은 보조(툴팁/별도 라벨 가능)
- [ ] 가동률 라벨 = UptimeRatePercent (수율 계산 제거)
- [ ] Total chip/Good/Bin = 스냅샷 카운트 사용 (lot.ProcessedDies 의존 제거)
- [ ] 부하/가동/연속가동/통상정지/이상정지/복귀 = 스냅샷 *Seconds → FormatTs
- [ ] 흐르는 초 표시: 현재 상태 진입 후 경과(now - enter)를 UI에서 가산 (선택)
- [ ] 이상정지횟수/MTBF/MTTR = 스냅샷 값
- [ ] 작업중 LOT ID = 스냅샷 ActiveLotId
- [ ] 기존 타이머/diff(SetText)/가시성(ShouldRefreshVisible) 로직 유지
- [ ] UI 스레드에서 컨트롤러/Lot 내부를 직접 순회하지 않음 (락 경합 0 확인)

## E. 빌드/회귀
- [ ] QMC.CDT-320 빌드 경고 없이 통과
- [ ] `perl tools/verify_handler_features.pl` 회귀 통과(가능 시)
- [ ] `QMC.CDT-320.exe --auto-cycle 10` 실행 시 CYCLE TIME/UPH/가동률 0이 아닌 값 표시 확인
- [ ] 오토 사이클 중 UI 끊김 없음(체감) 확인

## F. (선택) Lot 이력 동기화
- [ ] EndLot 시 엔진 확정값을 Lot에 반영 후 `LotStorage.SaveJson` (이력/JSON pretty 유지)
