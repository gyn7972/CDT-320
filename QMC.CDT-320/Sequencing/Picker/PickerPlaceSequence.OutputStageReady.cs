using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Recipes;
using QMC.Common.Motion;
using QMC.Common;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal sealed partial class PickerPlaceSequence
    {
        private int ResolveOutputSide()
        {
            OutputStageResultRoutingMode routingMode = ResolveOutputStageResultRoutingMode();
            if (routingMode == OutputStageResultRoutingMode.ForceGoodStage)
            {
                _currentOutputSide = BinSide.Good;
                if (!IsInspectionFlowComplete(_currentDie))
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " GOOD 강제 배출 모드이므로 현재 Picker Bottom FINAL 확인 후 Good Stage 접근을 시작합니다. " +
                        "Side FINAL은 PickerZ 하강 게이트까지 병렬 수집합니다. " +
                        "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", pickerNo=" + _currentPickerNo + " - Check");
                }
                else if (_currentDie != null && _currentDie.Result == DieResult.NG)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Die 결과가 NG이지만 설정에 따라 Good Stage 순번으로 Place합니다. " +
                        "die=" + _currentDie.DieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", routingMode=" + routingMode +
                        ", forcedOutputSide=" + _currentOutputSide + " - Check");
                }

                CurrentStep = PickerPlaceStep.VerifyOutputStageReady;
                return 0;
            }

            if (routingMode != OutputStageResultRoutingMode.RouteByInspectionResult)
            {
                return Fail("PICKER-PLACE-ROUTING-MODE", "OutputStage",
                    "지원하지 않는 Die 결과 배출 모드입니다. mode=" + routingMode +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo + ".");
            }

            if (!IsInspectionFlowComplete(_currentDie))
            {
                return Fail("PICKER-PLACE-INSPECTION-INCOMPLETE", "Material",
                    "검사 결과별 출력 Stage 분기 전에 Bottom/Side 검사 흐름이 완료되지 않았습니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", bottomDone=" + HasInspectionResult(_currentDie, "Bottom") +
                    ", side0Done=" + HasInspectionResult(_currentDie, "Side0") +
                    ", side90Done=" + HasInspectionResult(_currentDie, "Side90"));
            }

            if (_currentDie.Result == DieResult.Good)
                _currentOutputSide = BinSide.Good;
            else if (_currentDie.Result == DieResult.NG)
                _currentOutputSide = BinSide.Ng;
            else
                return Fail("PICKER-PLACE-DIE-RESULT-UNKNOWN", "Material",
                    "검사 결과별 출력 Stage 분기 전에 Die result가 확정되지 않았습니다. die=" +
                    _currentDie.DieId + ", pickerNo=" + _currentPickerNo + ".");

            // [Good 선배출·NG 유예 2026-08-25 팀장님 지시] Good 패스에서 NG 판정 다이는 스테이지/픽커
            // 이동 없이 유예 목록에 넣고 다음 픽커로 진행한다(택트 무손실 — 이 다이의 결과 대기는
            // 위 게이트에서 이미 끝났고, 검증된 Bottom FINAL 보정값은 BottomShot에 캐시되어 NG 패스
            // 재방문 시 재사용된다). 사이드 전환은 Good 전량 배출 후 전환 스텝에서 1회만 수행한다.
            // [적대적 검증 반영 2026-08-25] Auto 전용 — 수동/스텝 실행은 기존 다이별 순서를 그대로
            // 유지한다(스텝 절차·순서 기대 불변, 검증 확정 minor).
            if (Options != null && Options.RunMode == SequenceRunMode.Auto &&
                _placeRoutingPass == BinSide.Good && _currentOutputSide == BinSide.Ng)
            {
                if (!_deferredNgPickerIndexes.Contains(_currentPickerIndex))
                    _deferredNgPickerIndexes.Add(_currentPickerIndex);
                WriteLog("PickerPlaceSequence",
                    Name + " NG 판정 다이를 Good 선배출 패스에서 유예합니다(스테이지/픽커 이동 없음). " +
                    "die=" + _currentDie.DieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", deferredNgCount=" + _deferredNgPickerIndexes.Count + " - Check");
                CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                return 0;
            }
            // 방어: NG 패스의 다이는 유예 시점에 NG로 확정된 결과다 — Good으로 바뀌어 있으면
            // Material 상태가 흔들린 것이므로 진행하지 않는다.
            if (_placeRoutingPass == BinSide.Ng && _currentOutputSide != BinSide.Ng)
            {
                return Fail("PICKER-PLACE-NG-PASS-RESULT-CHANGED", "Material",
                    "NG 유예 패스의 Die 결과가 유예 시점과 다릅니다. die=" + _currentDie.DieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", dieResult=" + _currentDie.Result + ".");
            }

            WriteLog("PickerPlaceSequence",
                Name + " 검사 결과에 따라 출력 Stage를 결정했습니다. " +
                "die=" + _currentDie.DieId +
                ", pickerNo=" + _currentPickerNo +
                ", dieResult=" + _currentDie.Result +
                ", outputSide=" + _currentOutputSide +
                ", routingPass=" + _placeRoutingPass + " - Ok");
            CurrentStep = PickerPlaceStep.VerifyOutputStageReady;
            return 0;
        }

        private async Task<int> VerifyOutputStageReadyAsync(CancellationToken ct)
        {
            // 이번 대기의 로그 상태만 보관한다. 실제 준비 조건과 폴링 결과는 캐시하지 않는다.
            var waitLog = new OutputStageReadyWaitLogState();
            try
            {
                bool safeWaitPositionPrepared = false;
                bool fullAvoidPrepared = false;
                // [공급 교착 해소 2026-08-25 팀장님 지시] 스테이지 미준비(빈 없음/공급 필요) 대기가
                // 임계시간을 넘으면 보유 lease를 양보한다 — Place가 OutputPlaceArea/StageArea를 쥔 채
                // ready를 기다리는 동안 공급 시퀀스(OutputSupply/Store, OutputSequence.cs:1817/1947/2042/2177)는
                // 같은 lease를 Auto 무한 재시도로 기다려 순환 대기 교착이 된다(08-23 바코드 게이트 교정
                // 주석에 기록된 교착 클래스 — 게이트만 교정되고 이 대기는 남아 있었다).
                bool notReadyLeaseYielded = false;
                DateTime notReadyWaitStartedUtc = DateTime.MinValue;
                const int notReadyLeaseYieldThresholdMs = 5000;
                if (ForceSafeYBeforeFirstPlaceMove)
                {
                    int waitSafeResult = await MovePickerToSafeYBeforeOutputStageReadyWaitAsync(ct).ConfigureAwait(false);
                    if (waitSafeResult != 0)
                        return waitSafeResult;

                    safeWaitPositionPrepared = true;
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 재시작 안전 진입: OutputStage 준비 확인 전 PickerY를 Avoid로 정리했습니다. " +
                        "side=" + Side +
                        ", outputSide=" + _currentOutputSide +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", pickerNo=" + _currentPickerNo + " - Ok");
                }

                while (!ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();

                    string reason;
                    bool materialReady = MaterialStateService.IsOutputStageReceiveAvailable(_currentOutputSide, out reason);
                    bool signalReady = IsOutputStageSideReadySignalSet(_currentOutputSide);
                    bool autoMode = Options != null && Options.RunMode == SequenceRunMode.Auto;
                    bool stageReceiveComplete = MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide);

                    if (materialReady && (!autoMode || signalReady))
                    {
                        FlushOutputStageReadyWaitLog(waitLog);
                        BeginOutputPostPlaceInspectionBatch();
                        WriteLog("PickerPlaceSequence",
                            Name + " OutputStage 수령 준비 확인 완료. side=" + _currentOutputSide +
                            ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                            ", pickerNo=" + _currentPickerNo +
                            ", materialReady=" + materialReady +
                            ", signalReady=" + signalReady +
                            ", reason=" + reason + " - Ok");

                        CurrentStep = PickerPlaceStep.ReserveOutputStageTarget;
                        return 0;
                    }

                    string detail = GetOutputStageReadyWaitDetail(waitLog,
                        _currentOutputSide, _currentDie != null ? _currentDie.DieId : "-",
                        _currentPickerNo, materialReady, signalReady, reason);

                    if (!autoMode)
                        return Fail("PICKER-PLACE-OUTPUT-STAGE-NOT-READY", "Material", detail);

                    if (stageReceiveComplete && !fullAvoidPrepared)
                    {
                        AppSettings settings = AppSettingsStore.Current;
                        if (settings != null && settings.UseOutputGoodPickupCap)
                        {
                            int pending;
                            int held;
                            int reserved;
                            int allowance = MaterialStateService.GetOutputGoodNewPickAllowance(
                                out pending,
                                out held,
                                out reserved);
                            QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "PickerPlaceSequence",
                                Name + " 픽업 캡 적용 상태에서 보유 대기 경로에 도달했습니다. " +
                                "수동 슬롯 완료 등 예외 경로를 확인하십시오. side=" + Side +
                                ", outputSide=" + _currentOutputSide +
                                ", pickerNo=" + _currentPickerNo +
                                ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                                ", goodPending=" + pending +
                                ", held=" + held +
                                ", reserved=" + reserved +
                                ", allowance=" + allowance + " - Warning");
                        }

                        const string fullWaitDescription = "OutputStage 수령 완료 대기 중 보유 Die Picker 전체 Avoid";
                        int fullAvoidResult = await MovePickerToAvoidAfterPlaceFastAsync(
                            fullWaitDescription,
                            ct).ConfigureAwait(false);
                        if (fullAvoidResult != 0)
                            return fullAvoidResult;

                        _currentPlaceZSafeReturnCompleted = true;
                        ClearPendingContiRetreat();
                        ForceSafeYBeforeFirstPlaceMove = true;
                        KeepPickerYForwardDuringPlaceReadyWait = false;
                        ReleaseOutputPlaceArea();
                        ReleaseOutputStageArea();
                        ReleaseOutputFeederArea();
                        EndOutputPostPlaceInspectionBatch();

                        int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                            "OutputStage Full 교체 대기");
                        if (workZoneReleaseResult != 0)
                            return workZoneReleaseResult;

                        int publishResult = await PublishOutputStageExchangeReadyAfterSafeCompletionAsync(ct).ConfigureAwait(false);
                        if (publishResult != 0)
                            return publishResult;

                        safeWaitPositionPrepared = true;
                        fullAvoidPrepared = true;
                        WriteLog("PickerPlaceSequence",
                            Name + " OutputStage가 완료되어 다음 Place 대상이 열릴 때까지 보유 Die 상태로 Picker 전체 Avoid 복귀를 완료했습니다. " +
                            "side=" + Side + ", outputSide=" + _currentOutputSide +
                            ", pickerNo=" + _currentPickerNo + " - Ok");

                        Func<BinSide, int, string, CancellationToken, Task<int>> handoff =
                            WaitForOutputStageExchangeWithProcessHandoffAsync;
                        if (handoff == null)
                        {
                            return Fail("PICKER-PLACE-OUTPUT-HANDOFF-MISSING", Name,
                                "OutputStage Full 안전 대기 후 부모 Picker Process 리소스 양도 경로가 없습니다. " +
                                "side=" + Side + ", outputSide=" + _currentOutputSide +
                                ", pickerNo=" + _currentPickerNo);
                        }

                        int handoffResult = await handoff(
                            _currentOutputSide,
                            _currentPickerNo,
                            fullWaitDescription,
                            ct).ConfigureAwait(false);
                        if (handoffResult != 0)
                            return handoffResult;

                        // 부모가 Place phase와 Output work-zone을 다시 획득했으므로,
                        // 최종 Place 후 Avoid에서 후검사 전에 다시 정상 해제할 수 있도록 알림 상태를 복원한다.
                        _parentOutputWorkZoneReleaseNotified = false;
                        // 같은 Stage를 기다리던 다른 Picker가 먼저 새 Bin을 다시 Full로 만들 수 있다.
                        // handoff 1회가 끝날 때마다 새 교체 세대로 보고 다음 Full을 다시 양도할 수 있게 한다.
                        fullAvoidPrepared = false;
                        WriteLog("PickerPlaceSequence",
                            Name + " OutputStage 교체 후 부모 Picker Process/Place 작업영역 재점유가 완료되어 " +
                            "기존 보유 Die Place 준비 확인을 다시 시작합니다. side=" + Side +
                            ", outputSide=" + _currentOutputSide +
                            ", pickerNo=" + _currentPickerNo + " - Ok");
                    }
                    else if (!safeWaitPositionPrepared)
                    {
                        if (KeepPickerYForwardDuringPlaceReadyWait && !ForceSafeYBeforeFirstPlaceMove)
                        {
                            WriteLog("PickerPlaceSequence",
                                Name + " 정상 연속 Place 대기: Side 검사 종료 위치에서 PickerY Avoid 복귀를 생략합니다. " +
                                "OutputStage 준비가 열리면 현재 PickerY 위치에서 Place 목표 Y로 직접 이동합니다. " +
                                "side=" + Side +
                                ", outputSide=" + _currentOutputSide +
                                ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                                ", pickerNo=" + _currentPickerNo + " - Check");
                        }
                        else
                        {
                            int waitSafeResult = await MovePickerToSafeYBeforeOutputStageReadyWaitAsync(ct).ConfigureAwait(false);
                            if (waitSafeResult != 0)
                                return waitSafeResult;
                        }

                        safeWaitPositionPrepared = true;
                    }
                    // [공급 교착 해소 2026-08-25 팀장님 지시] 미준비(수령 완료가 아닌 not-ready = 빈 없음/
                    // 공급 필요/Ready 신호 다운) 대기가 임계시간을 넘고 lease를 보유 중이면 1회 양보한다.
                    // 양보 세트는 검증된 full-wait 분기와 동일 조합(handoff/부모 work zone 제외 — 공급
                    // 경로는 OutputPlaceArea+사이드 StageArea만 필요하므로 사이클 구성요소만 최소 반환):
                    // 픽커 전체 Avoid(다이 보유 상태) → Conti 상태 정리 → lease 3종 반환 → 후검사 배치
                    // 종료(대기 중 후검사 진행 허용). 이후 lease 없이 폴링을 계속하고, ready가 열리면
                    // 성공 분기의 BeginOutputPostPlaceInspectionBatch와 다음 스텝(MoveOutputStageAvoidPosition)의
                    // null 가드가 배치·lease를 기존 경로로 재개·재획득한다(ForceSafeYBeforeFirstPlaceMove로
                    // 안전 재진입). 임계 미만의 일시 not-ready는 기존과 동일하게 lease 보유 대기(택트 보존).
                    // 체인 순서 유의: 안전 Y 확보(safeWaitPositionPrepared) 분기 뒤에 두어 기존 첫 반복의
                    // 안전 Y 정리가 그대로 선행된다.
                    else if (!stageReceiveComplete &&
                             !notReadyLeaseYielded &&
                             (_outputPlaceLease != null || _outputStageLease != null || _outputFeederLease != null))
                    {
                        if (notReadyWaitStartedUtc == DateTime.MinValue)
                            notReadyWaitStartedUtc = DateTime.UtcNow;

                        if ((DateTime.UtcNow - notReadyWaitStartedUtc).TotalMilliseconds >= notReadyLeaseYieldThresholdMs)
                        {
                            const string yieldDescription = "OutputStage 준비(공급) 대기 lease 양보 — 보유 Die Picker 전체 Avoid";
                            bool hadPlaceLease = _outputPlaceLease != null;
                            bool hadStageLease = _outputStageLease != null;
                            bool hadFeederLease = _outputFeederLease != null;

                            int yieldAvoidResult = await MovePickerToAvoidAfterPlaceFastAsync(
                                yieldDescription,
                                ct).ConfigureAwait(false);
                            if (yieldAvoidResult != 0)
                                return yieldAvoidResult;

                            _currentPlaceZSafeReturnCompleted = true;
                            ClearPendingContiRetreat();
                            ForceSafeYBeforeFirstPlaceMove = true;
                            KeepPickerYForwardDuringPlaceReadyWait = false;
                            ReleaseOutputPlaceArea();
                            ReleaseOutputStageArea();
                            ReleaseOutputFeederArea();
                            EndOutputPostPlaceInspectionBatch();

                            safeWaitPositionPrepared = true;
                            notReadyLeaseYielded = true;
                            // 최소 로그 정책에서도 남도록 레벨 지정 — 무로그 정지 방지(08-23 교훈).
                            QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "PickerPlaceSequence",
                                Name + " OutputStage 미준비 대기가 " + notReadyLeaseYieldThresholdMs +
                                "ms를 넘어 보유 lease를 양보했습니다(공급/복구 시퀀스 진행 허용, 이후 lease 없이 대기). " +
                                "side=" + Side +
                                ", outputSide=" + _currentOutputSide +
                                ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                                ", pickerNo=" + _currentPickerNo +
                                ", hadPlaceLease=" + hadPlaceLease +
                                ", hadStageLease=" + hadStageLease +
                                ", hadFeederLease=" + hadFeederLease +
                                ", reason=" + reason + " - Check");
                        }
                    }

                    WriteOutputStageReadyWaitLog(waitLog, detail, stageReceiveComplete);
                    Context.StopIfCycleStopRequested(
                        "PickerPlaceSequence.WaitOutputStageReady",
                        ShouldDeferCycleStopForPickerDrain(),
                        "Picker Place drain");
                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                ct.ThrowIfCancellationRequested();
                return Fail("PICKER-PLACE-OUTPUT-STAGE-READY-CANCELED", "Material",
                    "OutputStage 수령 준비 대기가 취소되었습니다. side=" + _currentOutputSide +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-OUTPUT-STAGE-READY-EX", "Material",
                    "OutputStage 수령 준비 확인 중 예외가 발생했습니다. side=" + _currentOutputSide +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                // 성공·실패·취소·Cycle Stop 모두 남은 로그 횟수만 정리한다. 장비 상태는 변경하지 않는다.
                FlushOutputStageReadyWaitLog(waitLog);
            }
        }

        private sealed class OutputStageReadyWaitLogState
        {
            public BinSide OutputSide;
            public string DieId;
            public int PickerNo;
            public bool MaterialReady;
            public bool SignalReady;
            public string Reason;
            public string CachedDetail;
            public string LastLoggedDetail;
            public bool LastStageReceiveComplete;
            public string WaitId;
            public long EntryNumber;
            public long SuppressedCount;
            public int LastEmitTick;
        }

        private static string GetOutputStageReadyWaitDetail(OutputStageReadyWaitLogState state,
            BinSide outputSide, string dieId, int pickerNo, bool materialReady, bool signalReady, string reason)
        {
            if (state.CachedDetail == null || state.OutputSide != outputSide ||
                !string.Equals(state.DieId, dieId, StringComparison.Ordinal) || state.PickerNo != pickerNo ||
                state.MaterialReady != materialReady || state.SignalReady != signalReady ||
                !string.Equals(state.Reason, reason, StringComparison.Ordinal))
            {
                state.OutputSide = outputSide;
                state.DieId = dieId;
                state.PickerNo = pickerNo;
                state.MaterialReady = materialReady;
                state.SignalReady = signalReady;
                state.Reason = reason;
                state.CachedDetail =
                    "OutputStage가 Die를 받을 준비가 되지 않았습니다. side=" + outputSide +
                    ", die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", materialReady=" + materialReady +
                    ", signalReady=" + signalReady +
                    ", reason=" + reason;
            }
            return state.CachedDetail;
        }

        private void WriteOutputStageReadyWaitLog(OutputStageReadyWaitLogState state,
            string detail, bool stageReceiveComplete)
        {
            const int waitLogIntervalMs = 1000;
            if (!string.Equals(state.LastLoggedDetail, detail, StringComparison.Ordinal) ||
                state.LastStageReceiveComplete != stageReceiveComplete)
            {
                FlushOutputStageReadyWaitLog(state);
                state.LastLoggedDetail = detail;
                state.LastStageReceiveComplete = stageReceiveComplete;
                WriteOutputStageReadyWaitLogEntry(state, 0, 0);
                state.LastEmitTick = Environment.TickCount;
                return;
            }

            state.SuppressedCount++;
            int elapsedMs = unchecked(Environment.TickCount - state.LastEmitTick);
            if (elapsedMs < 0 || elapsedMs >= waitLogIntervalMs)
                FlushOutputStageReadyWaitLog(state);
        }

        private void FlushOutputStageReadyWaitLog(OutputStageReadyWaitLogState state)
        {
            try
            {
                if (state.SuppressedCount == 0)
                    return;

                int now = Environment.TickCount;
                int elapsedMs = Math.Max(0, unchecked(now - state.LastEmitTick));
                WriteOutputStageReadyWaitLogEntry(state, state.SuppressedCount, elapsedMs);
                state.SuppressedCount = 0;
                state.LastEmitTick = now;
            }
            catch (Exception ex)
            {
                // 종료 요약 실패로 원래 시퀀스 반환값이나 취소/정지 예외를 바꾸지 않는다.
                System.Diagnostics.Debug.WriteLine("OutputStage 대기 로그 요약 실패: " + ex.Message);
            }
        }

        private void WriteOutputStageReadyWaitLogEntry(OutputStageReadyWaitLogState state,
            long suppressedCount, int elapsedMs)
        {
            if (state.WaitId == null)
                state.WaitId = SequenceTrace.NextTraceId();
            state.EntryNumber++;

            // Source와 기존 로그 경로는 유지한다. 호출 ID/기록 순번으로 하위 필터의 재축약을 방지한다.
            WriteLog("PickerPlaceSequence",
                Name + (suppressedCount == 0 ? " Place 대기: " : " Place 대기 반복 요약: ") +
                state.LastLoggedDetail +
                ", stageReceiveComplete=" + state.LastStageReceiveComplete +
                ", waitId=" + state.WaitId +
                ", waitEntry=" + state.EntryNumber +
                ", repeatSuppressed=" + suppressedCount +
                ", intervalMs=" + elapsedMs + " - Wait");
        }

        private async Task<int> MovePickerToSafeYBeforeOutputStageReadyWaitAsync(CancellationToken ct)
        {
            try
            {
                int zResult = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "Place 준비 대기 전 PickerZ 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (zResult != 0)
                    return zResult;

                double yAvoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (CanSkipPickerMoveCommand(PickerAxis.PickerY, yAvoid))
                    return 0;

                int yResult = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    yAvoid,
                    "Place 준비 대기 전 PickerY SafeY",
                    ct,
                    "AvoidPosition;PickerPhase=PlaceReadyWaitSafeY").ConfigureAwait(false);
                if (yResult != 0)
                    return yResult;

                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 준비 대기 전 PickerY를 SafeY/Avoid로 이동했습니다. " +
                    "side=" + Side +
                    ", outputSide=" + _currentOutputSide +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-WAIT-SAFE-Y-EX", Name,
                    "OutputStage 준비 대기 전 PickerY SafeY 이동 중 예외가 발생했습니다. side=" +
                    Side + ", outputSide=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsOutputStageSideReadySignalSet(BinSide side)
        {
            try
            {
                if (Context == null || Context.Bus == null)
                    return false;

                if (side == BinSide.Good)
                    return Context.Bus.IsSet("OutputGoodStageReady");

                if (side == BinSide.Ng)
                    return Context.Bus.IsSet("OutputNgStageReady");

                return false;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 준비 신호 확인 실패. side=" + side +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private int ReserveOutputStageTarget()
        {
            _receiveTarget = MaterialStateService.ReserveNextOutputStageReceiveTarget(_currentOutputSide);
            if (_receiveTarget == null)
            {
                return Fail("PICKER-PLACE-RECEIVE-TARGET-MISSING", "Material",
                    "Output stage receive target is missing. die=" + _currentDie.DieId +
                    ", side=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo);
            }

            _placeTargetPrepared = false;

            CurrentStep = PickerPlaceStep.MoveOutputStageAvoidPosition;
            return 0;
        }

        private async Task<int> MoveOutputStageAvoidPositionAsync(CancellationToken ct)
        {
            if (_outputPlaceLease == null)
            {
                int inspectWaitResult = await WaitOutputPostPlaceInspectionIdleAsync(ct).ConfigureAwait(false);
                if (inspectWaitResult != 0)
                    return inspectWaitResult;

                _outputPlaceLease = await AcquireResourceAsync(SequenceResourceKind.OutputPlaceArea, Name + ":Place", ct).ConfigureAwait(false);
                if (_outputPlaceLease == null)
                    return -1;
            }

            if (_outputStageLease == null)
            {
                SequenceResourceKind resource = _currentOutputSide == BinSide.Ng
                    ? SequenceResourceKind.OutputNgStageArea
                    : SequenceResourceKind.OutputGoodStageArea;

                _outputStageLease = await AcquireResourceAsync(resource, Name + ":Place:" + _currentOutputSide, ct).ConfigureAwait(false);
                if (_outputStageLease == null)
                    return -1;
            }

            int result = await EnsureOutputStagePlaceEntryReadyAsync(ct).ConfigureAwait(false);

            if (result != 0)
                return result;

            // [사용자 지시 2026-08-25] 픽커 Z 하강 잔류 중 Good/NG 스테이지 이동 금지 — 스테이지
            // 전환/상대 이동이 실제로 필요한 경우에만, 이동 발행 전에 이전 픽커 Z 지연 복귀를 먼저
            // 완료한다. 같은 사이드 연속 배치(상대 이미 Avoid·GoodZ 이미 자리)는 스테이지 이동이
            // 없으므로 복귀를 당기지 않는다(택트 보존 — 같은 스테이지 Y+Z 동기 도착 유지, 사용자 확인).
            if (IsOutputStageTransitionMoveExpected())
            {
                int transitionRetreatResult = await CompletePendingContiRetreatIfNeededAsync(
                    "스테이지 전환/상대 이동 전 이전 픽커 Z 안전 복귀",
                    ct).ConfigureAwait(false);
                if (transitionRetreatResult != 0)
                    return transitionRetreatResult;
            }

            result = await MoveOppositeOutputStageToAvoidForPlaceAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            // [NG 라우팅 Z 전환 순서 교정 2026-08-25, 사용자 승인] NG 다이가 GoodStageZ를 Avoid로
            // 내려놓은 뒤 Good 다이가 오면, 기존 순서(픽커 접근·Z 사전 하강 → Z Process 상승)가
            // 역순이라 "PickerZ 하강 상태에서 Stage Z 상승 금지" 인터락에 차단됐다(02:29 Critical 실측
            // — 스테이지가 하강한 픽커 밑에서 올라오는 실충돌 위험을 인터락이 옳게 막은 것).
            // GoodZ가 Process가 아닌 Good 다이에서만: ① 이전 픽커 Z 지연 복귀를 먼저 완료하고
            // (하강 잔류 픽커 Z가 상승 인터락에 걸리지 않게), ② 픽커 접근 시작 전에 GoodZ를
            // Process로 선행 상승한다. GoodZ가 이미 Process면(연속 Good, ForceGoodStage 로트 전체)
            // 이 블록은 실행되지 않아 기존 동작·택트 무변경. Good→NG 전환(GoodZ 하강)은 인터락이
            // 하강을 검사하지 않으므로(안전 방향 통과 규칙) 별도 조치가 필요 없다.
            if (_currentOutputSide == BinSide.Good && !IsGoodStageZAtProcessPosition())
            {
                int retreatResult = await CompletePendingContiRetreatIfNeededAsync(
                    "Good Stage Z Process 선행 상승 전 이전 픽커 Z 안전 복귀",
                    ct).ConfigureAwait(false);
                if (retreatResult != 0)
                    return retreatResult;

                result = await EnsureOutputStageZReadyForPlaceAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPlaceSequence",
                    Name + " NG→Good 전환: 픽커 접근 전에 Good Stage Z를 Process로 선행 상승했습니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
            }

            // [Good 선배출·NG 유예 2026-08-25 팀장님 지시] NG 다이 진입 전 NgY Process 기준 선행 정렬.
            // VerifyOutputStageReady(NG 수령 준비) 통과 후 시점이라 공급 시퀀스와의 lease 순환 대기가
            // 없고, MoveOpposite(GoodZ↓·NgY 선행 회피 가드·GoodY Avoid)가 직전에 완료된 상태다.
            // 여기서 NgY를 Process 기준까지 도착 검증으로 정렬해 두면 이후 Conti StageY 이동은
            // 슬롯 오프셋 수준만 남아, NgY 대행정(≈618mm)과 픽커 Z 사전하강(ContiXYMidRatio 트리거)이
            // 겹치던 2026-08-25 니어미스 창이 소멸한다. 이미 Process 부근이면(연속 NG) 무동작.
            // NG Clamp Lift Up은 MoveStageAxis NgY 공통 관문이 자동 확보한다. 정지 재시작으로 NG부터
            // 시작하는 배치도 이 블록이 같은 방식으로 방어한다.
            if (_currentOutputSide == BinSide.Ng && !IsNgStageYAtProcessBasePosition())
            {
                int ngRetreatResult = await CompletePendingContiRetreatIfNeededAsync(
                    "NG Stage Y Process 선행 정렬 전 이전 픽커 Z 안전 복귀",
                    ct).ConfigureAwait(false);
                if (ngRetreatResult != 0)
                    return ngRetreatResult;

                int ngPreAlignResult = await MoveOutputStageAxisAndVerifyAsync(
                    BinStageAxis.NgBinY,
                    OutputStage.Recipe.NGStageY.ProcessPosition,
                    "NG 진입 전 NG Stage Y Process 선행 정렬",
                    ct,
                    BuildOutputStagePlaceMoveTargetName("NgYProcessPreAlign")).ConfigureAwait(false);
                if (ngPreAlignResult != 0)
                    return ngPreAlignResult;

                WriteLog("PickerPlaceSequence",
                    Name + " NG 진입 전 NG Stage Y를 Process 기준 위치로 선행 정렬했습니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", ngY=" + DescribeStageAxisActual(BinStageAxis.NgBinY) + " - Ok");
            }

            if (IsPickerMotionOnlyTestMode())
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Picker Motion Only Test 모드: OutputVisionX 피커 진입용 Avoid 이동을 생략합니다. side=" +
                    _currentOutputSide + ", die=" + (_currentDie != null ? _currentDie.DieId : "-") + " - Check");
                CurrentStep = PickerPlaceStep.MoveOutputStageReceivePosition;
                return 0;
            }

            bool outputFeederAlreadySafe = OutputFeeder != null &&
                                           OutputFeeder.IsFeederUnclamped() &&
                                           CanSkipOutputFeederMoveCommand(OutputFeeder, OutputFeeder.Recipe.AvoidPosition);
            if (!outputFeederAlreadySafe)
            {
                result = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveVisionXToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                    ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail("PICKER-PLACE-VISION-X-FEEDER-SAFE", "OutputStage",
                        "OutputFeeder 안전 위치 확보 전 OutputVisionX 전체 Avoid 이동 실패. " +
                        "side=" + _currentOutputSide + ", result=" + result + ", " +
                        OutputStage.DescribeStageLoadMoveState(_currentOutputSide));
                }

                result = await EnsureOutputFeederSafeBeforePlaceStageMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPlaceSequence",
                    Name + " OutputFeeder가 안전 위치가 아니어서 OutputVisionX 전체 Avoid 후 Feeder를 먼저 정리했습니다. " +
                    "side=" + _currentOutputSide + ", die=" + (_currentDie != null ? _currentDie.DieId : "-") + " - Ok");
            }

            result = PreparePlaceTargetValues();
            if (result != 0)
            {
                if (HasPendingContiRetreat())
                {
                    int retreatResult = await CompletePendingContiRetreatIfNeededAsync(
                        "Place 목표 계산 실패 후 이전 PickerZ 안전 복귀",
                        ct).ConfigureAwait(false);
                    if (retreatResult != 0)
                    {
                        WriteLog("PickerPlaceSequence",
                            Name + " Place 목표 계산 실패와 함께 이전 PickerZ 안전 복귀도 실패했습니다. " +
                            "targetResult=" + result +
                            ", retreatResult=" + retreatResult + " - Failed");
                        return retreatResult;
                    }
                }
                return result;
            }

            double fullAvoid = OutputStage.Recipe.VisionX.AvoidPosition;
            double visionTarget = fullAvoid;
            string retreatDetail = "전체 Avoid 사용";
            // Conti 게이트: Auto + ContiSegmentedPlace면 부호 인지 최소 회피(Extra 포함).
            // 미충족이면 기존 경로(-0.1/1.0 — OutputVisionX는 사실상 전체 Avoid 폴백) 그대로 (동작 무변경).
            // planned는 배치 전체 목표 보관 필드가 없어 현재 아이템 _targetPickerX만 전달 —
            // 서비스가 피커 축 Actual/Command를 자동 포함하며, 다음 아이템 차례에 정확 좌표로 재계산된다.
            PickerPlaceMotionConfig retreatPlaceConfig = ResolvePlaceMotionConfig();
            bool useMinimalRetreat =
                Options != null && Options.RunMode == SequenceRunMode.Auto &&
                retreatPlaceConfig != null &&
                IsCoordinatedPlaceMotionMode(retreatPlaceConfig.MotionMode);
            string retreatMode = useMinimalRetreat ? "minimal" : "legacy";
            List<double> plannedEntryPickerTargets = null;
            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                Context != null ? Context.Machine : null);
            if (service != null)
            {
                var planned = new Dictionary<SharedRailXAxis, IList<double>>();
                SharedRailXAxis pickerRailAxis = Side == PickerSequenceSide.Front
                    ? SharedRailXAxis.FrontPickerX
                    : SharedRailXAxis.RearPickerX;
                // 수정(2026-07-24): 현재 아이템 정확값 + 나머지 배치 아이템의 근사값으로 보강한다.
                // 근사 = 현재 PickerX − 현재 픽커 오프셋 + 해당 픽커 오프셋 (같은 빈 작업점 기준 피치 차).
                // 근사 실패 아이템은 생략 — 서비스가 피커 Actual/Command를 자동 포함해 안전.
                var plannedPickerTargets = new List<double> { _targetPickerX };
                double currentOffsetX;
                double currentOffsetY;
                string currentOffsetReason;
                // [Good 선배출·NG 유예 2026-08-25] 잔여 배치 근사는 현재 패스의 활성 목록 기준이다 —
                // NG 패스에서 _pickedPickerIndexes를 그대로 돌면 이미 배출된 픽커의 유령 목표가 섞인다.
                IList<int> plannedBatchList = _placeRoutingPass == BinSide.Ng
                    ? _deferredNgPickerIndexes
                    : (IList<int>)_pickedPickerIndexes;
                if (plannedBatchList != null &&
                    TryResolveOutputVisionToPickerOffsets(_currentPickerIndex, out currentOffsetX, out currentOffsetY, out currentOffsetReason))
                {
                    for (int i = _pickerCursor + 1; i < plannedBatchList.Count; i++)
                    {
                        double itemOffsetX;
                        double itemOffsetY;
                        string itemOffsetReason;
                        if (TryResolveOutputVisionToPickerOffsets(plannedBatchList[i], out itemOffsetX, out itemOffsetY, out itemOffsetReason))
                            plannedPickerTargets.Add(_targetPickerX - currentOffsetX + itemOffsetX);
                    }
                }
                planned[pickerRailAxis] = plannedPickerTargets;
                plannedEntryPickerTargets = plannedPickerTargets;

                double dynamicTarget;
                string dynamicDetail;
                // 회피 목표 마진(사용자 승인 2026-07-26, 4번): Extra에 +1mm — 최심 Place 목표와의
                // 최종 간격이 팔로잉 safetyGap과 정확히 같아지는 경계치 해소(팔로잉 gap은 무변경).
                bool resolved = useMinimalRetreat
                    ? service.TryResolveMinimalVisionRetreatTarget(
                        OutputStage.OutputCameraX,
                        fullAvoid,
                        planned,
                        (service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0) +
                        VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm,
                        out dynamicTarget,
                        out dynamicDetail)
                    : service.TryResolveNearestVisionRetreatTarget(
                        OutputStage.OutputCameraX,
                        fullAvoid,
                        -0.1,
                        planned,
                        1.0,
                        out dynamicTarget,
                        out dynamicDetail);
                if (resolved)
                {
                    visionTarget = dynamicTarget;
                    retreatDetail = dynamicDetail;
                }
                else
                {
                    retreatDetail = dynamicDetail + " 전체 Avoid로 대체합니다.";
                }
            }

            // 회피 no-op 방지(사용자 승인 2026-07-26): "Place 진입에 실제로 필요한 비전 위치"를
            // 게시해 후검사 큐가 배치 EPD 시점에 미리 물러나게 한다(사이드 촬영과 병렬, 시점만 선행).
            // [결함 수정 2026-07-26] 기존: 서비스 최소 회피 결과(visionTarget)를 그대로 게시 —
            //   비전이 fullAvoid에 주차된 채 계산되면 hold(현재 위치 유지) 규칙이 fullAvoid를
            //   돌려주고, 그 값이 게시→선회피 확장→다음 배치도 fullAvoid…로 래치됐다
            //   (실장비 07:11~07:13 5배치 전부 target=1000.681 실측 — 매 배치 +500mm 과회피).
            // 현재 기준: 게시값을 비전 현재 위치와 무관하게 "배치 planned 피커 목표의 페어식
            //   하한(Safety+Extra+1mm)"으로 직접 계산한다. fullAvoid 이상이면 게시하지 않는다
            //   (무게시 = 무확장 = 기존 동작이라 안전한 폴백).
            if (useMinimalRetreat)
            {
                double placeEntryBound;
                if (TryComputeOutputPlaceEntryVisionBound(
                        plannedEntryPickerTargets, fullAvoid, out placeEntryBound))
                {
                    VisionIndependentRetreatCoordinator.RegisterOutputPlaceEntryTarget(
                        placeEntryBound, Name + ":PlaceEntry");
                }
            }

            WriteLog("PickerPlaceSequence",
                Name + " OutputVisionX 피커 진입 회피 좌표를 확정했습니다. " +
                "mode=" + retreatMode +
                ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", pickerX=" + _targetPickerX.ToString("F6") +
                ", target=" + visionTarget.ToString("F6") +
                ", fullAvoid=" + fullAvoid.ToString("F6") +
                ", detail=" + retreatDetail + " - Check");

            // 기존 조건(사용자 지시 2026-07-25, O-2): 이동 명령을 발행하지 않고 이연 플래그만 세워
            //   피커 X 진입 직전에 기동했다 — 인풋 미러 실측에서 홀드 손해로 확인된 설계다.
            // 현재 기준(사용자 지시 2026-07-26, 이연 폐지·인풋 미러): 후검사 큐의 독립 회피가 진행
            //   중이면 (a) 목표가 배치 planned 목표와 페어 간격을 만족하면 그 Task/목표를 인수하고,
            //   (b) 부족하면 "외부 join → 정확 좌표 연장 회피" 합성 Task를 지금 기동한다.
            //   (c) 외부 회피가 없으면 자체 회피를 지금 즉시 비동기 기동한다.
            //   이동 명령 경로(SharedRailX 중재 경유)와 join 지점(피커 X 진입 완료 전)은 기존과 동일하다.
            if (useMinimalRetreat)
            {
                await JoinOutputVisionRetreatMoveTaskAsync("새 회피 기동 전 이전 Task 정리", ct).ConfigureAwait(false);
                _outputVisionRetreatTarget = visionTarget;

                Task<int> externalRetreatTask;
                double externalRetreatTarget;
                bool externalTaskAlreadyCompleted;
                if (VisionIndependentRetreatCoordinator.TryAdoptOutput(
                    out externalRetreatTask, out externalRetreatTarget, out externalTaskAlreadyCompleted))
                {
                    if (IsOutputVisionTargetClearOfPlannedPickerTargets(externalRetreatTarget, plannedEntryPickerTargets))
                    {
                        // 현재 기준(사용자 승인 2026-07-26, 3번): 인수 Task를 그대로 쓰지 않고
                        // "외부 Task join → 실위치 도착 대기" 합성으로 감싼다 — 이동 Task가
                        // 실제 도착 전에 완료 상태가 되어도 join이 곧 도착을 의미하게 된다.
                        _outputVisionRetreatTarget = externalRetreatTarget;
                        _outputVisionRetreatMoveTask = AwaitExternalOutputVisionArrivalAsync(
                            externalRetreatTask, externalRetreatTarget, ct);
                        WriteLog("PickerPlaceSequence",
                            Name + " 후검사 독립 회피를 인수합니다(목표 충분 — 추가 이동 없음). " +
                            "externalTarget=" + externalRetreatTarget.ToString("F6") +
                            ", exactTarget=" + visionTarget.ToString("F6") +
                            ", externalTaskCompleted=" + externalTaskAlreadyCompleted + " - Ok");
                    }
                    else
                    {
                        _outputVisionRetreatMoveTask = ExtendExternalOutputVisionRetreatAsync(
                            externalRetreatTask, externalRetreatTarget, visionTarget, ct);
                        WriteLog("PickerPlaceSequence",
                            Name + " 후검사 독립 회피를 인수하고 정확 좌표 연장 회피를 합성 기동했습니다(목표 부족). " +
                            "externalTarget=" + externalRetreatTarget.ToString("F6") +
                            ", exactTarget=" + visionTarget.ToString("F6") +
                            ", externalTaskCompleted=" + externalTaskAlreadyCompleted + " - Start");
                    }
                }
                else
                {
                    // 보강(사용자 승인 2026-07-26, 3번): 자체 회피도 "이동+실위치 도착 대기" 합성.
                    _outputVisionRetreatMoveTask = RunOwnOutputVisionRetreatAsync(visionTarget, ct);
                    WriteLog("PickerPlaceSequence",
                        Name + " OutputVisionX 최소 회피 이동을 즉시 비동기 시작했습니다(외부 독립 회피 없음). " +
                        "target=" + visionTarget.ToString("F6") + " - Start");
                }
            }
            else
            {
                result = await MoveOutputStageAxisAndVerifyAsync(
                    BinStageAxis.VisionX,
                    visionTarget,
                    "OutputVisionX 최소 회피 위치 이동",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("PICKER-PLACE-VISION-X-AVOID", "OutputStage",
                        "OutputVisionX 피커 진입용 최소 회피 이동 실패. side=" + _currentOutputSide +
                        ", target=" + visionTarget.ToString("F6") +
                        ", result=" + result + ", " + OutputStage.DescribeStageLoadMoveState(_currentOutputSide));
            }

            CurrentStep = PickerPlaceStep.MoveOutputStageReceivePosition;
            return 0;
        }

        // 게시용 "Place 진입 요구 비전 좌표" 직접 계산 — 배치 planned 피커 목표 전부에 대해
        // 페어식 하한(팔로잉 safetyGap=Safety+Extra + 1mm 경계 여유)을 만족하는 가장 얕은 좌표.
        // 비전 현재 위치를 전혀 참조하지 않으므로 hold/래치가 원천적으로 불가능하다.
        private bool TryComputeOutputPlaceEntryVisionBound(
            IList<double> plannedPickerTargets,
            double fullAvoid,
            out double bound)
        {
            bound = 0.0;
            try
            {
                if (plannedPickerTargets == null || plannedPickerTargets.Count == 0 ||
                    OutputStage == null || OutputStage.OutputCameraX == null)
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                if (service == null || pickerX == null)
                    return false;

                int direction;
                double homeGap;
                double safetyGap;
                string gapDetail;
                if (!service.TryGetFollowGapParameters(
                    OutputStage.OutputCameraX,
                    pickerX,
                    service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0,
                    out direction,
                    out homeGap,
                    out safetyGap,
                    out gapDetail))
                {
                    return false;
                }

                double required = safetyGap + VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm;
                bool first = true;
                for (int i = 0; i < plannedPickerTargets.Count; i++)
                {
                    double picker = plannedPickerTargets[i];
                    double candidate = direction > 0
                        ? picker + homeGap - required
                        : picker - homeGap + required;
                    if (first)
                    {
                        bound = candidate;
                        first = false;
                    }
                    else
                    {
                        // 가장 제약이 큰(회피 쪽으로 가장 깊은) 하한을 취한다.
                        bound = direction > 0 ? Math.Min(bound, candidate) : Math.Max(bound, candidate);
                    }
                }

                // fullAvoid보다 얕을 때만 유효 — 같거나 넘으면 게시 무의미(확장 없음이 안전).
                bool shallowerThanFullAvoid = direction > 0
                    ? bound > fullAvoid + 0.000001
                    : bound < fullAvoid - 0.000001;
                return !first && shallowerThanFullAvoid;
            }
            catch
            {
                return false;
            }
        }

        private async Task<int> EnsureOutputStagePlaceEntryReadyAsync(CancellationToken ct)
        {
            int timeout = ResolveTimeout();

            int result = await OutputStage.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, timeout, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!OutputStage.IsBinGuideClampLiftUp(BinSide.Ng))
                return Fail("PICKER-PLACE-NG-CLAMP-UP", "OutputStage",
                    "Place 진입 전 NG Bin Clamp Lift가 Up 상태가 아닙니다. " +
                    OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            result = await OutputStage.EnsureBinGuideDownAsync(BinSide.Good, timeout, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!OutputStage.IsBinGuideDown(BinSide.Good))
                return Fail("PICKER-PLACE-GOOD-GUIDE-DOWN", "OutputStage",
                    "Place 진입 전 Good Bin Guide가 Down 상태가 아닙니다. " +
                    OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            return 0;
        }

        private async Task<int> MoveOppositeOutputStageToAvoidForPlaceAsync(CancellationToken ct)
        {
            if (_currentOutputSide == BinSide.Good)
            {
                int ngResult = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                    ct).ConfigureAwait(false);
                if (ngResult != 0)
                    return Fail("PICKER-PLACE-OPP-STAGE-AVOID", "OutputStage",
                        "Place 전 상대 NG Stage Avoid 이동 실패. result=" + ngResult +
                        ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

                return 0;
            }

            int goodZResult = await AwaitStepWithCancellationAsync(
                OutputStage.MoveGoodStageZToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                ct).ConfigureAwait(false);
            if (goodZResult != 0)
                return Fail("PICKER-PLACE-OPP-STAGE-Z-AVOID", "OutputStage",
                    "Place 전 상대 Good Stage Z Avoid 이동 실패. result=" + goodZResult +
                    ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            // [NG 라우팅 순서 교정 2026-08-25] GoodStageY 이동의 절대 인터락 조건은 "NG Stage가
            // Avoid 위치"다. 직전 혼합 배치/후검사가 NG Y를 작업 위치에 둔 채 끝난 경우, GoodY
            // Avoid 이동을 발행하기 전에 NG Y를 먼저 Avoid로 이동시킨다(11:00 Critical 실측 —
            // 혼합 배치 다음의 첫 NG 다이에서 차단). GoodY가 이미 Avoid면 GoodY 이동 자체가
            // 스킵되므로 NG Y 선행 회피도 하지 않는다(NG 연속 배치 택트 보존). NG Y는 이후
            // 수령 위치 이동 단계에서 다시 진입한다.
            if (!IsGoodStageYAtAvoidPosition() && !OutputStage.IsNgStageInAvoidPosition())
            {
                int ngPreAvoidResult = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                    ct).ConfigureAwait(false);
                if (ngPreAvoidResult != 0)
                    return Fail("PICKER-PLACE-OPP-STAGE-NG-PRE-AVOID", "OutputStage",
                        "GoodStageY Avoid 이동 전 NG Stage 선행 Avoid 이동 실패. result=" + ngPreAvoidResult +
                        ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));
            }

            double goodYAvoid = OutputStage.Recipe.GoodStageY.AvoidPosition;
            int goodYResult = await MoveOutputStageAxisAndVerifyAsync(
                BinStageAxis.GoodBinY,
                goodYAvoid,
                "opposite Good stage Y avoid before place",
                ct).ConfigureAwait(false);
            if (goodYResult != 0)
                return goodYResult;

            return 0;
        }

        // [사용자 지시 2026-08-25] 이번 다이의 Place 준비에서 Good/NG 스테이지의 전환/상대 이동이
        // 실제로 발행될지 판정한다. 판단 불가면 true — 이전 픽커 Z 복귀를 우선한다(안전 방향).
        private bool IsOutputStageTransitionMoveExpected()
        {
            try
            {
                if (OutputStage == null)
                    return true;

                if (_currentOutputSide == BinSide.Good)
                    return !OutputStage.IsNgStageInAvoidPosition() || !IsGoodStageZAtProcessPosition();

                return !OutputStage.IsGoodStageZAtAvoid() || !IsGoodStageYAtAvoidPosition();
            }
            catch
            {
                return true;
            }
        }

        // [사용자 지시 2026-08-25] GoodStageY가 Avoid 위치에 있는지 판정한다(전환 이동 예상 판정용).
        private bool IsGoodStageYAtAvoidPosition()
        {
            try
            {
                if (OutputStage == null || OutputStage.Recipe == null ||
                    OutputStage.GoodStage == null || OutputStage.GoodStage.StageY == null)
                    return false;

                OutputStage.Recipe.EnsurePositionObjects();
                if (OutputStage.Recipe.GoodStageY == null)
                    return false;

                BaseAxis goodY = OutputStage.GoodStage.StageY;
                double tolerance = goodY.Config != null && goodY.Config.InPositionTolerance > 0.0
                    ? goodY.Config.InPositionTolerance
                    : 0.05;
                return OutputStage.IsStageAxisInPosition(
                    BinStageAxis.GoodBinY,
                    OutputStage.Recipe.GoodStageY.AvoidPosition,
                    tolerance);
            }
            catch
            {
                return false;
            }
        }

        // [Good 선배출·NG 유예 2026-08-25] NgStageY가 Process 기준 위치에 있는지 판정한다
        // (NG 진입 전 선행 정렬 필요 여부 판정용). 판정 불가면 false — 선행 정렬 블록이 실행되며,
        // 이미 위치면 MoveStageAxis가 이동 없이 통과하므로 무해하다.
        private bool IsNgStageYAtProcessBasePosition()
        {
            try
            {
                if (OutputStage == null || OutputStage.Recipe == null ||
                    OutputStage.NgStage == null || OutputStage.NgStage.StageY == null)
                    return false;

                OutputStage.Recipe.EnsurePositionObjects();
                if (OutputStage.Recipe.NGStageY == null)
                    return false;

                BaseAxis ngY = OutputStage.NgStage.StageY;
                double tolerance = ngY.Config != null && ngY.Config.InPositionTolerance > 0.0
                    ? ngY.Config.InPositionTolerance
                    : 0.05;
                return OutputStage.IsStageAxisInPosition(
                    BinStageAxis.NgBinY,
                    OutputStage.Recipe.NGStageY.ProcessPosition,
                    tolerance);
            }
            catch
            {
                return false;
            }
        }

        // [NG 라우팅 Z 전환 순서 교정 2026-08-25] GoodStageZ가 Process 위치에 있는지 판정한다.
        // 판정 불가(유닛/레시피 없음)면 false — 상위에서 선행 상승 블록이 실행되며, 그 경로의
        // EnsureOutputStageZReadyForPlaceAsync가 이미 위치면 이동 없이 검증만 하므로 무해하다.
        private bool IsGoodStageZAtProcessPosition()
        {
            try
            {
                if (OutputStage == null || OutputStage.Recipe == null ||
                    OutputStage.GoodStage == null || OutputStage.GoodStage.StageZ == null)
                    return false;

                OutputStage.Recipe.EnsurePositionObjects();
                if (OutputStage.Recipe.GoodStageZ == null)
                    return false;

                BaseAxis goodZ = OutputStage.GoodStage.StageZ;
                double tolerance = goodZ.Config != null && goodZ.Config.InPositionTolerance > 0.0
                    ? goodZ.Config.InPositionTolerance
                    : 0.05;
                return OutputStage.IsStageAxisInPosition(
                    BinStageAxis.GoodBinZ,
                    OutputStage.Recipe.GoodStageZ.ProcessPosition,
                    tolerance);
            }
            catch
            {
                return false;
            }
        }

        // [Good 선배출·NG 유예 2026-08-25 팀장님 지시] Good 전량 배출 완료 후 NG 패스 진입 전 1회 실행.
        // 근거(2026-08-25 실장비 니어미스): 전환 다이의 Conti가 NgY 대행정(Avoid 884.81→Process 267.077,
        // 약 618mm)과 픽커 Z 사전하강(ContiXYMidRatio 트리거)을 겹쳐 수행해, 하강 중인 픽커 밑을 NG 빈
        // 구조물이 횡단하는 창이 있었다(자동 StageY 인터락은 Conti 오버랩 보존을 위해 픽커 Z를 의도적으로
        // 면제 — 시퀀스 게이트가 유일한 방어).
        // [적대적 검증 반영 2026-08-25] 이 스텝은 스테이지를 움직이지 않고 lease도 새로 잡지 않는다.
        //  - 여기서 스테이지 이동/lease 획득을 하면 NG 스테이지 준비 확인(VerifyOutputStageReady) 전에
        //    OutputPlaceArea/OutputNgStageArea를 쥔 채 대기하게 되어 NG 빈 공급 시퀀스와 순환 대기
        //    교착이 생기고(검증 확정 critical), GoodY Avoid 직접 발행은 MoveOpposite의 NgY 선행 회피
        //    가드(11:00 Critical 교정)를 우회해 혼합→전부NG 배치 연쇄에서 인터락 차단된다(검증 확정 major).
        //  - 따라서 여기서는 ① 전 픽커 Z Avoid 완료·검증 ② Good 사이드 stage lease 반환(NG lease
        //    획득은 첫 NG 다이의 기존 경로가 준비 확인 후 수행) ③ VisionX 재계산 후퇴 기동(place lease
        //    보유 시에만 — 전부 NG 배치는 다이별 회피 경로가 담당)만 수행하고, Good 스테이지 정리
        //    (GoodZ↓·NgY 선행 회피·GoodY Avoid)와 NgY Process 선행 정렬은 첫 NG 다이의
        //    MoveOutputStageAvoidPosition(검증된 다이별 경로)이 준비 확인 후 수행한다. VisionX 이동은
        //    그 스테이지 전환 이동들과 자연 병행되며 join은 기존 다이별 회피 기동 전 정리 지점이 담당한다.
        private async Task<int> TransitionOutputStageForNgPassAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (OutputStage == null || OutputStage.Recipe == null)
                return Fail("PICKER-PLACE-NGPASS-UNIT", "OutputStage",
                    "NG 패스 전환에 필요한 OutputStage/Recipe를 찾을 수 없습니다.");
            OutputStage.Recipe.EnsurePositionObjects();

            WriteLog("PickerPlaceSequence",
                Name + " Good→NG 패스 전환을 시작합니다(스테이지 정렬은 첫 NG 다이 경로가 준비 확인 후 수행). " +
                "deferredNgCount=" + _deferredNgPickerIndexes.Count +
                ", goodPlaced=" + _goodPassPlacedCount +
                ", goodZ=" + DescribeStageAxisActual(BinStageAxis.GoodBinZ) +
                ", goodY=" + DescribeStageAxisActual(BinStageAxis.GoodBinY) +
                ", ngY=" + DescribeStageAxisActual(BinStageAxis.NgBinY) +
                ", visionX=" + DescribeStageAxisActual(BinStageAxis.VisionX) + " - Start");

            // ① 전 픽커 Z Avoid 완료 + 검증 (지연 상승 태스크 join 포함) — 이후 스테이지 전환 이동의 전제.
            int zResult = await JoinAllPickerZAtAvoidWithRecoveryAsync(
                "NG 패스 전환 전 전 픽커 Z Avoid 확보", ct).ConfigureAwait(false);
            if (zResult != 0)
                return zResult;
            ClearPendingContiRetreat();

            // ② Good 사이드 stage lease 반환 — 첫 NG 다이의 MoveOutputStageAvoidPosition이
            //    준비 확인 후 OutputNgStageArea를 새로 획득한다(사이드 불일치 해소, 교착 방지).
            ReleaseOutputStageArea();

            // ③ VisionX 재계산 후퇴(B안, 팀장님 확정): 유예 NG 픽커들의 진입 X 근사 목표로 필요 후퇴를
            //    재계산하고, 현 위치가 이미 그 이상 후퇴면 무이동(간섭 없음), 부족하면 즉시 기동해
            //    첫 NG 다이의 스테이지 전환 이동과 병행시킨다. 계산 불가 시 전체 Avoid 폴백(fail-safe).
            //    place lease 미보유(전부 NG 배치)면 이전 배치 후검사의 VisionX 사용과 경합할 수 있어
            //    기동하지 않는다 — 첫 NG 다이의 기존 회피 경로(유휴 대기+lease 후)가 정확 좌표로 수행.
            if (_outputPlaceLease != null)
            {
                int priorRetreatJoin = await JoinOutputVisionRetreatMoveTaskAsync(
                    "NG 패스 전환 회피 기동 전 이전 Task 정리", ct).ConfigureAwait(false);
                if (priorRetreatJoin != 0)
                    return priorRetreatJoin;

                double requiredVisionX;
                string visionDetail;
                ResolveNgPassTransitionVisionRetreatTarget(out requiredVisionX, out visionDetail);

                BaseAxis visionAxis = OutputStage.OutputCameraX;
                if (visionAxis != null)
                {
                    visionAxis.UpdateStatus();
                    if (visionAxis.IsMoving)
                    {
                        // 소유자 없는 잔여 이동이면 정지 대기 후 판정(이동 중 재명령 금지 — AXM 거부 방지).
                        int stopResult = await WaitOutputVisionXStoppedQuietAsync(
                            "NG 패스 전환 회피 판정 전", ct).ConfigureAwait(false);
                        if (stopResult != 0)
                            return stopResult;
                        visionAxis.UpdateStatus();
                    }
                }

                double visionActual = visionAxis != null ? visionAxis.ActualPosition : double.NaN;
                double visionTolerance = visionAxis != null && visionAxis.Config != null && visionAxis.Config.InPositionTolerance > 0.0
                    ? visionAxis.Config.InPositionTolerance
                    : 0.05;
                // 후퇴 방향은 +X(전체 Avoid가 Process보다 큼). 현 위치 확인 불가면 이동(보수적).
                bool visionMoveNeeded = double.IsNaN(visionActual) ||
                                        visionActual < requiredVisionX - visionTolerance;
                if (visionMoveNeeded)
                {
                    // 기동 이후 이 함수에는 await가 없다 — 실패/취소로 Task가 미조인 잔존하는 창 없음.
                    _outputVisionRetreatTarget = requiredVisionX;
                    _outputVisionRetreatMoveTask = RunOwnOutputVisionRetreatAsync(requiredVisionX, ct);
                    WriteLog("PickerPlaceSequence",
                        Name + " NG 패스 전환: VisionX 재계산 후퇴를 기동했습니다(첫 NG 다이 스테이지 전환과 병행). " +
                        "required=" + requiredVisionX.ToString("F6") +
                        ", actual=" + (double.IsNaN(visionActual) ? "-" : visionActual.ToString("F6")) +
                        ", detail=" + visionDetail + " - Start");
                }
                else
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " NG 패스 전환: VisionX 현 위치가 요구 후퇴 이상이라 이동하지 않습니다(간섭 없음). " +
                        "required=" + requiredVisionX.ToString("F6") +
                        ", actual=" + visionActual.ToString("F6") +
                        ", detail=" + visionDetail + " - Check");
                }
            }
            else
            {
                WriteLog("PickerPlaceSequence",
                    Name + " NG 패스 전환: place lease 미보유(전부 NG 배치) — VisionX 전환 후퇴 기동을 생략하고 " +
                    "첫 NG 다이의 기존 회피 경로에 맡깁니다. - Check");
            }

            WriteLog("PickerPlaceSequence",
                Name + " Good→NG 패스 전환 완료 — 유예 NG 배출을 시작합니다. " +
                "deferredNgCount=" + _deferredNgPickerIndexes.Count + " - Ok");

            CurrentStep = PickerPlaceStep.SelectNextPicker;
            return 0;
        }

        // [Good 선배출·NG 유예 2026-08-25] 전환 시 VisionX 필요 후퇴 목표(B안) — 유예 NG 픽커들의
        // 진입 X 근사(마지막 Good 목표 기준 픽커 오프셋 피치 차, 다이별 배치 근사식과 동일)로
        // 최소 후퇴를 재계산한다. 근사/계산 불가 시 전체 Avoid 폴백(항상 충분 — fail-safe).
        private void ResolveNgPassTransitionVisionRetreatTarget(out double target, out string detail)
        {
            double fullAvoid = OutputStage.Recipe.VisionX.AvoidPosition;
            target = fullAvoid;
            detail = "전체 Avoid 폴백";
            try
            {
                PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
                bool useMinimalRetreat =
                    Options != null && Options.RunMode == SequenceRunMode.Auto &&
                    placeConfig != null &&
                    IsCoordinatedPlaceMotionMode(placeConfig.MotionMode);
                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                if (!useMinimalRetreat || service == null)
                {
                    detail = "최소 회피 조건 미충족(mode/service) — 전체 Avoid 폴백";
                    return;
                }

                // [적대적 검증 반영 2026-08-25] 근사 base는 _targetPickerX를 만든 "마지막 Good 배출
                // 픽커"의 오프셋이어야 한다. _currentPickerIndex는 마지막 반복 픽커(보통 유예 NG 픽커)라
                // 배치가 Good으로 끝나지 않으면 픽커 피치 수 배만큼 어긋난다(검증 확정 major).
                var plannedPickerTargets = new List<double>();
                double baseOffsetX;
                double baseOffsetY;
                string baseOffsetReason;
                if (_goodPassPlacedCount > 0 &&
                    _lastGoodPlacedPickerIndex >= 0 &&
                    TryResolveOutputVisionToPickerOffsets(_lastGoodPlacedPickerIndex, out baseOffsetX, out baseOffsetY, out baseOffsetReason))
                {
                    for (int i = 0; i < _deferredNgPickerIndexes.Count; i++)
                    {
                        double itemOffsetX;
                        double itemOffsetY;
                        string itemOffsetReason;
                        if (TryResolveOutputVisionToPickerOffsets(_deferredNgPickerIndexes[i], out itemOffsetX, out itemOffsetY, out itemOffsetReason))
                            plannedPickerTargets.Add(_targetPickerX - baseOffsetX + itemOffsetX);
                    }
                }

                if (plannedPickerTargets.Count == 0)
                {
                    detail = "유예 픽커 진입 X 근사 불가(goodPlaced=" + _goodPassPlacedCount + ") — 전체 Avoid 폴백";
                    return;
                }

                var planned = new Dictionary<SharedRailXAxis, IList<double>>();
                planned[Side == PickerSequenceSide.Front
                    ? SharedRailXAxis.FrontPickerX
                    : SharedRailXAxis.RearPickerX] = plannedPickerTargets;

                double dynamicTarget;
                string dynamicDetail;
                if (service.TryResolveMinimalVisionRetreatTarget(
                        OutputStage.OutputCameraX,
                        fullAvoid,
                        planned,
                        (service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0) +
                        VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm,
                        out dynamicTarget,
                        out dynamicDetail))
                {
                    target = dynamicTarget;
                    detail = dynamicDetail;
                }
                else
                {
                    detail = dynamicDetail + " 전체 Avoid로 대체합니다.";
                }
            }
            catch (Exception ex)
            {
                target = fullAvoid;
                detail = "재계산 예외(" + ex.Message + ") — 전체 Avoid 폴백";
            }
        }

        // [Good 선배출·NG 유예 2026-08-25] 전환 계측용 — 스테이지 축 실측 위치 문자열(축 없으면 "-").
        private string DescribeStageAxisActual(BinStageAxis axis)
        {
            try
            {
                if (OutputStage == null || !OutputStage.HasStageAxis(axis))
                    return "-";

                BaseAxis item = null;
                switch (axis)
                {
                    case BinStageAxis.GoodBinY:
                        item = OutputStage.GoodStage != null ? OutputStage.GoodStage.StageY : null;
                        break;
                    case BinStageAxis.GoodBinZ:
                        item = OutputStage.GoodStage != null ? OutputStage.GoodStage.StageZ : null;
                        break;
                    case BinStageAxis.NgBinY:
                        item = OutputStage.NgStage != null ? OutputStage.NgStage.StageY : null;
                        break;
                    case BinStageAxis.VisionX:
                        item = OutputStage.OutputCameraX;
                        break;
                }

                if (item == null)
                    return "-";
                item.UpdateStatus();
                return item.ActualPosition.ToString("F3");
            }
            catch
            {
                return "-";
            }
        }

        // 현재 기준(사용자 승인 2026-07-25, M8): 확인과 예약을 원자화한다.
        //   기존 조건: 상대 점유 확인(대기)과 예약이 분리되어 있어 양쪽이 동시에 게이트를 통과하면
        //             둘 다 예약되고 이후 X 이동이 인터락 -11 → Critical로 라인을 세웠다.
        //   현재 기준: TryReservePickerWorkAreaExclusive가 성공할 때까지 대기한다. 성공 = 그 순간
        //             상대 미점유가 보장된 상태이므로 이후 X 진입이 점유 경합으로 차단되지 않는다.
        // 동시 Place 금지 정책은 유지된다(늦은 쪽은 예약 실패 → 대기 → 순서 진행).
        // 계층 관계: 1차 직렬화는 OutputPlaceArea 자원 lease와 PickerPhaseCoordinator의 Place/Place
        //   차단이며, 이 예약은 그 뒤의 방어 계층이다. 정상 흐름에서는 첫 시도에 성공해
        //   Wait 로그가 찍히지 않는 것이 정상이다(찍히면 상위 직렬화 구멍 신호 → 보고 대상).
        // 비Auto는 기존과 동일하게 즉시 실패(인터락 차단과 같은 결론, 대기 없음).
        private async Task<int> ReserveOutputWorkAreaExclusiveWithWaitAsync(CancellationToken ct)
        {
            try
            {
                string occupiedOwner;
                if (TryReservePickerWorkAreaExclusive(PickerWorkZone.Output, "Place", out occupiedOwner))
                    return 0;

                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    return Fail("PICKER-OPPOSITE-OUTPUT-ZONE", Name,
                        "Place 진입 불가: 상대 Picker가 Output 작업영역을 점유 중입니다. owner=" + occupiedOwner);
                }

                bool waitLogged = false;
                DateTime lastWaitLog = DateTime.MinValue;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(
                            Name + ".WaitOppositePickerOutputClearBeforePlace",
                            ShouldDeferCycleStopForPickerDrain(),
                            "Picker Place drain");

                    if (TryReservePickerWorkAreaExclusive(PickerWorkZone.Output, "Place", out occupiedOwner))
                    {
                        if (waitLogged)
                        {
                            WriteLog("PickerPlaceSequence",
                                Name + " 상대 Picker Output 작업영역 대기 완료. Output 작업영역을 예약하고 Place 진입을 진행합니다. side=" + Side + " - Ok");
                        }

                        return 0;
                    }

                    if ((DateTime.UtcNow - lastWaitLog).TotalMilliseconds >= 1000.0)
                    {
                        lastWaitLog = DateTime.UtcNow;
                        waitLogged = true;
                        WriteLog("PickerPlaceSequence",
                            Name + " Place 진입 전 상대 Picker Output 작업영역 해제 대기. 동시 Place는 허용되지 않습니다. " +
                            "side=" + Side +
                            ", owner=" + occupiedOwner + " - Wait");
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-OPPOSITE-OUTPUT-ZONE-EX", Name,
                    "상대 Picker Output 작업영역 원자 예약 대기 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputStageReceivePositionAsync(CancellationToken ct)
        {
            int feederReady = await EnsureOutputFeederSafeBeforePlaceStageMoveAsync(ct).ConfigureAwait(false);
            if (feederReady != 0)
            {
                await JoinOutputVisionRetreatMoveTaskAsync("Feeder 안전 확보 실패 정리", ct).ConfigureAwait(false);
                return feederReady;
            }

            // 현재 기준(사용자 승인 2026-07-25, M8): 동시 Place 금지는 M4의 "대기 게이트 → 예약"
            // 2단계가 아니라 원자 예약 1단계로 처리한다 — 확인과 등록 사이 창(TOCTOU)을 제거한다.
            // 일반/팔로잉/Conti 모든 X 진입 모드가 아래
            // MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync 하나로 수렴하므로
            // 이 한 곳에서 전 경로를 커버한다. 인터락 자체(최후 방어선)는 무변경.
            // OutputFeeder/OutputStage 로딩이 1순위다.
            // Picker는 OutputPlace/Stage/Feeder 리소스와 Feeder 안전 위치가 확보된 뒤에만
            // Output work area를 점유해야 Feeder 로딩 중 Picker owner로 인한 인터락 오판이 생기지 않는다.
            int oppositeOutputClear = await ReserveOutputWorkAreaExclusiveWithWaitAsync(ct).ConfigureAwait(false);
            if (oppositeOutputClear != 0)
            {
                await JoinOutputVisionRetreatMoveTaskAsync("Output 작업영역 원자 예약 실패 정리", ct).ConfigureAwait(false);
                return oppositeOutputClear;
            }

            BinStageAxis yAxis = _currentOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;

            int prepareResult = PreparePlaceTargetValues();
            if (prepareResult != 0)
            {
                await JoinOutputVisionRetreatMoveTaskAsync("Place 목표 계산 실패 정리", ct).ConfigureAwait(false);
                return prepareResult;
            }
            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 계산 완료. side=" + Side + ", step=" + CurrentStep);

            int result = await MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync(yAxis, ct).ConfigureAwait(false);

            // R4(follow-entry): 비동기 비전 회피 Task는 피커 X 진입 처리 완료 전에 반드시 join.
            // 진입 실패 시에도 drain(observe)하고, 진입 성공 후 회피 실패면 시퀀스 Fail.
            int retreatJoinResult = await JoinOutputVisionRetreatMoveTaskAsync("피커 X 진입 완료", ct).ConfigureAwait(false);
            if (result != 0)
                return result;
            if (retreatJoinResult != 0)
            {
                return Fail("PICKER-PLACE-VISION-X-AVOID", "OutputStage",
                    "OutputVisionX 비동기 최소 회피 이동이 실패했습니다. side=" + _currentOutputSide +
                    ", target=" + _outputVisionRetreatTarget.ToString("F6") +
                    ", result=" + retreatJoinResult + ", " + OutputStage.DescribeStageLoadMoveState(_currentOutputSide));
            }

            if (ForceSafeYBeforeFirstPlaceMove)
                ForceSafeYBeforeFirstPlaceMove = false;

            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 이동 완료. side=" + Side + ", step=" + CurrentStep);

            if (!_pickerZPlacedByContiSegmentedPlace)
            {
                result = await EnsureOutputStageZReadyForPlaceAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 이동 후 Z Ready 확인 완료. side=" + Side + ", step=" + CurrentStep);
            CurrentStep = PickerPlaceStep.VerifyPlaceTarget;
            return 0;
        }

        private async Task<int> EnsureOutputFeederSafeBeforePlaceStageMoveAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                OutputFeederUnit feeder = OutputFeeder;
                if (feeder == null)
                    return Fail("PICKER-PLACE-FEEDER-NO-UNIT", "BinFeederUnit",
                        "Place 전 OutputFeederUnit을 찾을 수 없습니다. side=" + _currentOutputSide +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-"));

                if (_outputFeederLease == null)
                {
                    _outputFeederLease = await AcquireResourceAsync(
                        SequenceResourceKind.OutputFeederArea,
                        Name + ":Place:OutputFeederAvoid:" + _currentOutputSide,
                        ct).ConfigureAwait(false);
                    if (_outputFeederLease == null)
                        return -1;
                }

                if (!feeder.IsFeederUnclamped())
                {
                    int unclampResult = await feeder.SetFeederClampAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
                    if (unclampResult != 0)
                        return Fail("PICKER-PLACE-FEEDER-UNCLAMP", feeder.Name,
                            "Place 전 OutputFeeder Unclamp 명령 실패. result=" + unclampResult +
                            ", side=" + _currentOutputSide + ", " + feeder.DescribeFeederCylinderState());
                }

                if (!feeder.IsFeederUnclamped())
                    return Fail("PICKER-PLACE-FEEDER-UNCLAMP-CHECK", feeder.Name,
                        "Place 전 OutputFeeder Unclamp 최종 확인 실패. side=" + _currentOutputSide +
                        ", " + feeder.DescribeFeederCylinderState());

                if (!CanSkipOutputFeederMoveCommand(feeder, feeder.Recipe.AvoidPosition))
                {
                    int moveResult = await feeder.MoveToFeederAvoidPosition(Options.FineMove).ConfigureAwait(false);
                    if (moveResult != 0)
                        return Fail("PICKER-PLACE-FEEDER-Y-AVOID", feeder.Name,
                            "Place 전 OutputFeederY Avoid 이동 명령 실패. result=" + moveResult +
                            ", side=" + _currentOutputSide + ", " + feeder.DescribeBinFeederYMoveDoneState() +
                            feeder.DescribeBinFeederYLastMotionFailure());

                    // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
                }

                if (!feeder.IsBinFeederYInAvoidPosition())
                    return Fail("PICKER-PLACE-FEEDER-Y-AVOID-CHECK", feeder.Name,
                        "Place 전 OutputFeederY Avoid 최종 확인 실패. side=" + _currentOutputSide +
                        ", " + feeder.DescribeBinFeederYMoveDoneState());

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-FEEDER-SAFE-EX", "BinFeederUnit",
                    "Place 전 OutputFeeder 안전 위치 확보 중 예외가 발생했습니다. side=" +
                    _currentOutputSide + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

    }
}
