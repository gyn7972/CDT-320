using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing.Calibration
{
    /// <summary>
    /// 콜렛 클리닝 자동 트리거 평가/실행 서비스.
    ///
    /// [실행 창 변경 2026-08-28 팀장님 지시] 실행 시점은 "GOOD Stage 빈 로딩 완료 후" 창으로
    /// 고정한다(기존: 인풋 새 웨이퍼 로딩 후 첫 Pick 전). GOOD 빈 로딩 완료 시
    /// NotifyGoodBinLoaded()가 계수+창 pending을 세우고, OutputSequence가 리소스를 잡기 전
    /// 안전 지점에서 RunIfGoodBinLoadedWindowAsync()로 소비한다. 픽커가 다이를 보유 중이면
    /// (빈 교체를 기다리던 Place 물량) 창을 유지한 채 스킵하고 픽커가 비면 실행한다.
    ///
    /// 트리거 3종(GOOD 빈 로딩 n회 / 공정 n개 / Auto 시작)은 각각 독립적으로 사용 유무를 가진다.
    /// 카운터는 ColletCleaningTriggerStateStore로 영속화되어 재시작해도 주기가 유지된다.
    /// (WaferExchangeCount 필드는 직렬화 호환을 위해 이름을 유지하며 GOOD 빈 로딩 횟수를 담는다.)
    /// </summary>
    internal static class ColletCleaningTriggerService
    {
        // GOOD 빈 로딩 후 아직 소비되지 않은 실행 창이 있는지(휘발 — 재시작 시 다음 로딩에서 재형성).
        private static volatile bool _goodBinWindowPending;

        /// <summary>GOOD Stage 빈 로딩 1회를 계수하고 클리닝 실행 창을 연다(로딩 완료 시점 호출).</summary>
        public static void NotifyGoodBinLoaded()
        {
            try
            {
                ColletCleaningTriggerState state = ColletCleaningTriggerStateStore.Current;
                state.WaferExchangeCount = state.WaferExchangeCount + 1;
                ColletCleaningTriggerStateStore.Save();
                _goodBinWindowPending = true;
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningTrigger",
                    "GOOD Stage 빈 로딩 계수 — 콜렛 클리닝 실행 창을 엽니다. goodBinLoadCount=" +
                    state.WaferExchangeCount + " - Check");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-TRIGGER",
                    "콜렛 클리닝 GOOD 빈 로딩 계수 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>공정 수를 누적한다(die 또는 wafer 단위는 설정에서 해석).</summary>
        public static void NotifyProcessed(int count)
        {
            if (count <= 0)
                return;

            try
            {
                ColletCleaningTriggerState state = ColletCleaningTriggerStateStore.Current;
                state.ProcessCount = state.ProcessCount + count;
                ColletCleaningTriggerStateStore.Save();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-TRIGGER",
                    "콜렛 클리닝 공정 계수 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        // 공정 수 계수는 Place 완료마다 호출되므로 설정/상태 파일에 접근하지 않는다(택트 영향).
        // 메모리에만 누적하고, 트리거 판정 시점(RunIfTriggeredAsync)에서 설정 단위에 맞는 쪽만 상태에 반영한다.
        // 판정은 어차피 "웨이퍼 로딩 완료 후 첫 Pick 전" 창에서만 가능하므로 반영 시점이 늦어도 기능 손실이 없다.
        private static int _pendingDieCount;
        private static int _pendingWaferCount;

        /// <summary>Die 1개 Place 완료를 계수한다(공정 수 단위가 Die일 때 판정 시점에 반영된다).</summary>
        public static void NotifyDiePlaced()
        {
            Interlocked.Increment(ref _pendingDieCount);
        }

        /// <summary>Wafer 1장 공정 진입을 계수한다(공정 수 단위가 Wafer일 때 판정 시점에 반영된다).</summary>
        public static void NotifyWaferProcessed()
        {
            Interlocked.Increment(ref _pendingWaferCount);
        }

        /// <summary>
        /// 메모리에 누적된 공정 수를 설정 단위에 맞는 쪽만 상태에 반영한다(판정 직전 1회).
        /// 두 카운터는 반영 여부와 무관하게 비운다(사용하지 않는 단위의 누적치가 남아 있지 않도록).
        /// </summary>
        private static void ApplyPendingProcessCounts(ColletCleaningSettings settings, ColletCleaningTriggerState state)
        {
            // 설정/상태를 못 읽으면 누적치를 비우지 않는다(다음 기회에 반영되도록 보존).
            if (settings == null || state == null)
                return;

            int dieDelta = Interlocked.Exchange(ref _pendingDieCount, 0);
            int waferDelta = Interlocked.Exchange(ref _pendingWaferCount, 0);

            try
            {
                int delta = settings.ProcessCountUnit == ColletCleaningProcessCountUnit.Wafer
                    ? waferDelta
                    : dieDelta;
                if (delta <= 0)
                    return;

                state.ProcessCount = state.ProcessCount + delta;
                ColletCleaningTriggerStateStore.Save();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-TRIGGER",
                    "콜렛 클리닝 공정 계수 반영 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        /// <summary>Auto 운전 시작 시 호출한다. Auto 시작 트리거 처리 여부를 리셋한다.</summary>
        public static void NotifyAutoRunStarted()
        {
            try
            {
                ColletCleaningTriggerState state = ColletCleaningTriggerStateStore.Current;
                state.AutoStartHandledInCurrentRun = false;
                ColletCleaningTriggerStateStore.Save();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-TRIGGER",
                    "콜렛 클리닝 Auto 시작 계수 리셋 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// [실행 창 2026-08-28] GOOD 빈 로딩 창(pending)이 열려 있으면 트리거를 평가·실행한다.
        /// OutputSequence가 액션 리소스를 잡기 전 지점에서 호출해야 하며(클리닝이 자체 lease를
        /// 잡으므로 데드락 방지), 픽커가 다이를 보유 중이면 창을 유지한 채 스킵한다(픽커가 비는
        /// 다음 호출에서 실행). 실행하지 않았으면 0, 실행 실패면 시퀀스 결과 코드를 돌려준다.
        /// </summary>
        public static async Task<int> RunIfGoodBinLoadedWindowAsync(MachineSequenceContext context, CancellationToken ct)
        {
            if (!_goodBinWindowPending)
                return 0;

            if (AnyPickerHoldsDie())
            {
                // 창은 유지 — Place가 끝나 픽커가 비면 다음 호출에서 실행된다(로그는 상태 전이 시 소음 방지 위해 생략).
                return 0;
            }

            _goodBinWindowPending = false;
            return await RunIfTriggeredAsync(context, ct).ConfigureAwait(false);
        }

        private static bool AnyPickerHoldsDie()
        {
            try
            {
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    if (QMC.CDT320.Materials.MaterialStateService.GetDieAtPicker(
                            QMC.CDT320.Materials.MaterialLocationKind.PickerFront, pickerNo) != null)
                        return true;
                    if (QMC.CDT320.Materials.MaterialStateService.GetDieAtPicker(
                            QMC.CDT320.Materials.MaterialLocationKind.PickerRear, pickerNo) != null)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-TRIGGER",
                    "콜렛 클리닝 픽커 보유 확인 중 예외 — 안전하게 보유 중으로 간주합니다. error=" + ex.Message);
                return true;
            }
        }

        /// <summary>
        /// 트리거 조건을 평가하고 성립하면 콜렛 클리닝을 실행한다.
        /// 실행하지 않았으면 0, 실행 후 실패면 시퀀스 결과 코드를 그대로 돌려준다.
        /// </summary>
        public static async Task<int> RunIfTriggeredAsync(MachineSequenceContext context, CancellationToken ct)
        {
            try
            {
                if (context == null || context.Machine == null)
                    return 0;

                ColletCleaningSettings settings = ResolveSettings();
                ColletCleaningTriggerState state = ColletCleaningTriggerStateStore.Current;

                // Place 완료마다 메모리에 모아둔 공정 수를 여기서 한 번에 상태로 반영한다.
                // (선택 콜렛이 없어 아래에서 조기 반환하는 경우에도 누적치가 무한히 남지 않도록 먼저 처리한다.)
                ApplyPendingProcessCounts(settings, state);

                if (settings == null || !settings.HasAnySelection())
                    return 0;

                string reason;
                if (!TryResolveTriggerReason(settings, state, out reason))
                    return 0;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningTrigger",
                    "자동 콜렛 클리닝 조건이 성립해 실행합니다. reason=" + reason +
                    ", waferExchangeCount=" + state.WaferExchangeCount +
                    ", processCount=" + state.ProcessCount + " - Start");
                context.LogPublic("[COLLET-CLEAN] 자동 콜렛 클리닝을 실행합니다. 조건=" + reason);

                var sequence = new AutoColletCleaningSequence(context, settings, SequenceRunMode.Auto);
                int result = await sequence.RunAsync(ct).ConfigureAwait(false);

                if (result != 0)
                {
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningTrigger",
                        "자동 콜렛 클리닝이 실패했습니다. reason=" + reason + ", result=" + result + " - Failed");
                    return result;
                }

                if (sequence.SkippedNoBin)
                {
                    // NG Bin이 없어 실행하지 못한 경우에는 카운터를 소비하지 않는다(다음 조건에서 재시도).
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningTrigger",
                        "NG Bin이 없어 자동 콜렛 클리닝을 건너뛰었습니다. 카운터를 유지하고 다음 조건에서 재시도합니다. reason=" +
                        reason + " - Skip");
                    return 0;
                }

                ConsumeCounters(settings, state, reason);
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
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-TRIGGER",
                    "자동 콜렛 클리닝 트리거 처리 중 예외가 발생했습니다. error=" + ex.Message);
                return 0;
            }
            finally
            {
            }
        }

        private static ColletCleaningSettings ResolveSettings()
        {
            try
            {
                CalibrationData data = CalibrationDataStore.LoadOrCreate();
                if (data == null)
                    return null;

                data.EnsureObjects();
                return data.ColletCleaning;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-TRIGGER",
                    "콜렛 클리닝 설정 로드 중 예외가 발생했습니다. error=" + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        private static bool TryResolveTriggerReason(
            ColletCleaningSettings settings,
            ColletCleaningTriggerState state,
            out string reason)
        {
            reason = string.Empty;

            if (settings.UseTriggerOnAutoStart && !state.AutoStartHandledInCurrentRun)
            {
                reason = "AutoStart";
                return true;
            }

            if (settings.UseTriggerOnWaferExchange &&
                state.WaferExchangeCount >= settings.WaferExchangeInterval)
            {
                reason = "WaferExchange(" + state.WaferExchangeCount + "/" + settings.WaferExchangeInterval + ")";
                return true;
            }

            if (settings.UseTriggerOnProcessCount &&
                state.ProcessCount >= settings.ProcessCountInterval)
            {
                reason = "ProcessCount(" + state.ProcessCount + "/" + settings.ProcessCountInterval +
                         " " + settings.ProcessCountUnit + ")";
                return true;
            }

            return false;
        }

        private static void ConsumeCounters(
            ColletCleaningSettings settings,
            ColletCleaningTriggerState state,
            string reason)
        {
            try
            {
                if (reason.StartsWith("AutoStart", StringComparison.OrdinalIgnoreCase))
                    state.AutoStartHandledInCurrentRun = true;

                if (settings.UseTriggerOnWaferExchange &&
                    state.WaferExchangeCount >= settings.WaferExchangeInterval)
                    state.WaferExchangeCount = 0;

                if (settings.UseTriggerOnProcessCount &&
                    state.ProcessCount >= settings.ProcessCountInterval)
                    state.ProcessCount = 0;

                state.LastTriggeredAt = DateTime.Now;
                state.LastTriggerReason = reason;
                ColletCleaningTriggerStateStore.Save();

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningTrigger",
                    "자동 콜렛 클리닝을 완료하고 트리거 카운터를 초기화했습니다. reason=" + reason +
                    ", waferExchangeCount=" + state.WaferExchangeCount +
                    ", processCount=" + state.ProcessCount + " - Ok");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-TRIGGER",
                    "콜렛 클리닝 트리거 카운터 초기화 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }
    }
}
