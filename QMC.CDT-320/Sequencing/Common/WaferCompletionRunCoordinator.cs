using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Wafer 완료 시 신규 Pick을 차단하고 Picker 보유 제품, 후검사, 안전 복귀가 모두 끝난 뒤
    /// 자동 운전을 READY로 종료시키는 공용 상태 조정기입니다.
    /// </summary>
    internal sealed class WaferCompletionRunCoordinator
    {
        private const string CompletionSignal = "WaferCompleteDrainFinished";
        private const int InputCompleteReason = 1;
        private const int OutputGoodCompleteReason = 2;
        private const int OutputNgCompleteReason = 4;
        private const int MonitorIntervalMs = 50;
        private const int WaitLogIntervalMs = 1000;

        private readonly MachineSequenceContext _context;
        private int _enabled;
        private int _drainRequested;
        private int _runComplete;
        private int _completionReasons;
        private int _lastWaitLogTick;

        public WaferCompletionRunCoordinator(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public bool Enabled
        {
            get { return Volatile.Read(ref _enabled) != 0; }
        }

        public bool IsDrainRequested
        {
            get { return Volatile.Read(ref _drainRequested) != 0; }
        }

        public bool IsRunComplete
        {
            get { return Volatile.Read(ref _runComplete) != 0; }
        }

        public void Configure(bool enabled)
        {
            Interlocked.Exchange(ref _enabled, enabled ? 1 : 0);
            Interlocked.Exchange(ref _drainRequested, 0);
            Interlocked.Exchange(ref _runComplete, 0);
            Interlocked.Exchange(ref _completionReasons, 0);
            Interlocked.Exchange(ref _lastWaitLogTick, Environment.TickCount - WaitLogIntervalMs);
            _context.Bus.Reset(CompletionSignal);

            _context.LogPublic("[WAFER-COMPLETE] mode=" +
                               (enabled ? "STOP AFTER DRAIN" : "CONTINUE"));
        }

        /// <summary>현재 완료 신호를 읽어 Stop After Drain 요청을 갱신합니다.</summary>
        public void ObserveCompletionSignals()
        {
            if (!Enabled || IsRunComplete)
                return;

            try
            {
                int reasons = 0;
                bool inputPickExhausted = _context.Bus.IsSet("InputStageReady") &&
                                          !MaterialStateService.HasReadyInputStagePickTarget();
                bool inputExchangeReady = _context.Bus.IsSet("InputStageDieComplete") &&
                                          MaterialStateService.IsInputStagePickComplete();
                if (inputPickExhausted || inputExchangeReady)
                {
                    reasons |= InputCompleteReason;
                }

                if (_context.Bus.IsSet("OutputGoodStageReceiveComplete") &&
                    MaterialStateService.IsOutputStageReceiveComplete(BinSide.Good))
                {
                    reasons |= OutputGoodCompleteReason;
                }

                if (_context.Bus.IsSet("OutputNgStageReceiveComplete") &&
                    MaterialStateService.IsOutputStageReceiveComplete(BinSide.Ng))
                {
                    reasons |= OutputNgCompleteReason;
                }

                if (reasons != 0)
                    RequestDrain(reasons);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun",
                    "Wafer 완료 신호 확인 중 예외가 발생했습니다. error=" + ex.Message + " - Check");
            }
        }

        public async Task RunMonitorAsync(CancellationToken ct)
        {
            if (!Enabled)
                return;

            while (!ct.IsCancellationRequested)
            {
                if (_context.IsCycleStopRequested)
                {
                    _context.LogPublic("[WAFER-COMPLETE] Cycle Stop 요청을 감지하여 완료 감시를 종료합니다.");
                    QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun",
                        "Wafer completion monitor stopped by cycle stop request. - Stopped");
                    return;
                }

                ObserveCompletionSignals();
                if (TryCompleteDrain())
                    return;

                await Task.Delay(MonitorIntervalMs, ct).ConfigureAwait(false);
            }

            ct.ThrowIfCancellationRequested();
        }

        public async Task WaitForCompletionAsync(CancellationToken ct)
        {
            if (!Enabled || !IsDrainRequested)
                return;

            if (IsRunComplete)
                return;

            await _context.Bus.WaitAsync(CompletionSignal, ct).ConfigureAwait(false);
        }

        public string BuildCompletionMessage()
        {
            return "Wafer 작업이 완료되었습니다.\r\n" +
                   "STOP AFTER DRAIN 설정에 따라 Picker 보유 제품 배출과 Output 후검사를 완료하고 자동 운전을 종료했습니다.\r\n" +
                   "완료 원인: " + BuildCompletionReasonText() + "\r\n" +
                   "Picker 보유 제품: 없음\r\n" +
                   "Wafer 교체 후 다시 시작하십시오.";
        }

        private void RequestDrain(int reasons)
        {
            AddCompletionReasons(reasons);
            if (Interlocked.CompareExchange(ref _drainRequested, 1, 0) != 0)
                return;

            // Wafer 완료 후에는 공용 Pick 진입 게이트에서 신규 Pick을 막고, 이미 보유한 제품만 검사/Place까지 배출합니다.
            string message = "Wafer 완료가 감지되어 신규 Pick을 차단하고 Picker 보유 제품 배출을 시작합니다. " +
                             "reason=" + BuildCompletionReasonText();
            _context.LogPublic("[WAFER-COMPLETE] " + message);
            QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun", message + " - Start");
        }

        private bool TryCompleteDrain()
        {
            if (!Enabled || !IsDrainRequested)
                return false;
            if (IsRunComplete)
                return true;

            string heldPickerProducts = BuildHeldPickerProducts();
            string pickerReason;
            bool pickersSafe = AreAllEnabledPickersAvoidAndStopped(out pickerReason);
            bool outputInspectionIdle = _context.OutputPostPlaceInspections == null ||
                                        _context.OutputPostPlaceInspections.IsIdle;
            bool outputInspectionFailed = _context.OutputPostPlaceInspections != null &&
                                          _context.OutputPostPlaceInspections.HasFailure;
            bool loaderIdle = !_context.Bus.IsSet("InputLoaderActive") &&
                              !_context.Bus.IsSet("OutputLoaderActive");

            bool complete = string.IsNullOrWhiteSpace(heldPickerProducts) &&
                            pickersSafe &&
                            outputInspectionIdle &&
                            !outputInspectionFailed &&
                            loaderIdle;
            if (!complete)
            {
                WriteWaitingStateIfDue(
                    heldPickerProducts,
                    pickerReason,
                    outputInspectionIdle,
                    outputInspectionFailed,
                    loaderIdle);
                return false;
            }

            if (Interlocked.CompareExchange(ref _runComplete, 1, 0) != 0)
                return true;

            _context.Bus.Set(CompletionSignal);
            string message = "Stop After Drain 완료. reason=" + BuildCompletionReasonText() +
                             ", pickerProducts=none, pickersAvoid=True, outputInspectionIdle=True, loaderIdle=True";
            _context.LogPublic("[WAFER-COMPLETE] " + message);
            QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun", message + " - Ok");
            return true;
        }

        private void WriteWaitingStateIfDue(
            string heldPickerProducts,
            string pickerReason,
            bool outputInspectionIdle,
            bool outputInspectionFailed,
            bool loaderIdle)
        {
            int now = Environment.TickCount;
            int previous = Volatile.Read(ref _lastWaitLogTick);
            if (unchecked(now - previous) < WaitLogIntervalMs ||
                Interlocked.CompareExchange(ref _lastWaitLogTick, now, previous) != previous)
            {
                return;
            }

            QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun",
                "Stop After Drain 안전 완료 대기 중. reason=" + BuildCompletionReasonText() +
                ", pickerProducts=" + (string.IsNullOrWhiteSpace(heldPickerProducts) ? "none" : heldPickerProducts) +
                ", pickerSafe=" + string.IsNullOrWhiteSpace(pickerReason) +
                ", pickerReason=" + (string.IsNullOrWhiteSpace(pickerReason) ? "-" : pickerReason) +
                ", outputInspectionIdle=" + outputInspectionIdle +
                ", outputInspectionFailed=" + outputInspectionFailed +
                ", loaderIdle=" + loaderIdle + " - Wait");
        }

        private string BuildHeldPickerProducts()
        {
            var held = new List<string>();
            AppendHeldPickerProducts(held, MaterialLocationKind.PickerFront, "Front");
            AppendHeldPickerProducts(held, MaterialLocationKind.PickerRear, "Rear");
            return string.Join(", ", held.ToArray());
        }

        private static void AppendHeldPickerProducts(
            ICollection<string> held,
            MaterialLocationKind location,
            string side)
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                if (die == null)
                    continue;

                held.Add(side + " P" + pickerNo + "(" + (die.DieId ?? "-") + ")");
            }
        }

        private bool AreAllEnabledPickersAvoidAndStopped(out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = _context.Machine;
            if (machine == null)
            {
                reason = "Machine=null";
                return false;
            }

            PickerFrontUnit front = machine.PickerFrontUnit;
            if (front != null && front.Config != null && front.Config.UseUnit)
            {
                if (!front.IsFrontPickerInAvoidPosition())
                {
                    reason = "FrontPicker가 전체 Avoid 위치가 아닙니다.";
                    return false;
                }

                if (IsAnyAxisMoving(new[]
                    {
                        front.PickerX, front.PickerY,
                        front.PickerT0, front.PickerZ0,
                        front.PickerT1, front.PickerZ1,
                        front.PickerT2, front.PickerZ2,
                        front.PickerT3, front.PickerZ3
                    }))
                {
                    reason = "FrontPicker 축이 이동 중입니다.";
                    return false;
                }
            }

            PickerRearUnit rear = machine.PickerRearUnit;
            if (rear != null && rear.Config != null && rear.Config.UseUnit)
            {
                if (!rear.IsRearPickerInAvoidPosition())
                {
                    reason = "RearPicker가 전체 Avoid 위치가 아닙니다.";
                    return false;
                }

                if (IsAnyAxisMoving(new[]
                    {
                        rear.PickerX, rear.PickerY,
                        rear.PickerT0, rear.PickerZ0,
                        rear.PickerT1, rear.PickerZ1,
                        rear.PickerT2, rear.PickerZ2,
                        rear.PickerT3, rear.PickerZ3
                    }))
                {
                    reason = "RearPicker 축이 이동 중입니다.";
                    return false;
                }
            }

            return true;
        }

        private static bool IsAnyAxisMoving(IEnumerable<BaseAxis> axes)
        {
            foreach (BaseAxis axis in axes)
            {
                if (axis == null || axis.IsMoving)
                    return true;
            }

            return false;
        }

        private void AddCompletionReasons(int reasons)
        {
            int current;
            int updated;
            do
            {
                current = Volatile.Read(ref _completionReasons);
                updated = current | reasons;
            }
            while (Interlocked.CompareExchange(ref _completionReasons, updated, current) != current);
        }

        private string BuildCompletionReasonText()
        {
            int reasons = Volatile.Read(ref _completionReasons);
            var values = new List<string>();
            if ((reasons & InputCompleteReason) != 0)
                values.Add("INPUT WAFER 완료");
            if ((reasons & OutputGoodCompleteReason) != 0)
                values.Add("OUTPUT GOOD WAFER 완료");
            if ((reasons & OutputNgCompleteReason) != 0)
                values.Add("OUTPUT NG WAFER 완료");
            return values.Count > 0 ? string.Join(" / ", values.ToArray()) : "Wafer 완료";
        }
    }
}
