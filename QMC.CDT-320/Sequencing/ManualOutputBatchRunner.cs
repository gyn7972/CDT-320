using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum ManualOutputBatchOperation
    {
        Load,
        Unload
    }

    internal sealed class ManualOutputBatchState
    {
        public bool NgEnabled { get; set; }
        public bool GoodPresent { get; set; }
        public bool NgPresent { get; set; }
        public BinSide? FeederSide { get; set; }
        public bool FeederComplete { get; set; }
    }

    internal interface IManualOutputBatchActions
    {
        Task<ManualOutputBatchState> ReadStateAsync(CancellationToken ct);
        void CheckCanContinue(CancellationToken ct);
        Task<int> ExecuteFeederAsync(ManualOutputBatchOperation operation, BinSide side, CancellationToken ct);
        Task<int> ExecuteSideAsync(ManualOutputBatchOperation operation, BinSide side, CancellationToken ct);
        void SaveCheckpoint(string description);
        Task<int> CompleteAsync(ManualOutputBatchOperation operation, bool anyWork, CancellationToken ct);
        string GetFailureReason();
        void Report(string message);
    }

    /// <summary>
    /// 실제 센서/Material 검증과 이송은 기존 시퀀스에 위임하고, ALL의 순서와 작업 경계를 관리합니다.
    /// 공유 Feeder를 사용하는 작업은 최대 세 번(잔류 Feeder, NG, GOOD)만 순차 실행합니다.
    /// </summary>
    internal sealed class ManualOutputBatchRunner
    {
        public string LastMessage { get; private set; }

        public async Task<int> RunAsync(ManualOutputBatchOperation operation, IManualOutputBatchActions actions, CancellationToken ct)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));

            var completed = new List<string>();
            string operationName = operation == ManualOutputBatchOperation.Load ? "로딩" : "언로딩";
            string active = "ALL " + operationName + " 준비";
            LastMessage = string.Empty;
            try
            {
                if (operation != ManualOutputBatchOperation.Load && operation != ManualOutputBatchOperation.Unload)
                    return Fail(actions, completed, -1, "지원하지 않는 ALL 작업입니다.");

                ManualOutputBatchState state = await ReadCheckedStateAsync(actions, null, ct).ConfigureAwait(false);
                bool ngEnabled = state.NgEnabled;
                string invalidReason = GetLoadDisabledNgReason(operation, state);
                if (invalidReason != null)
                    return Fail(actions, completed, -1, invalidReason);

                if (state.FeederSide.HasValue)
                {
                    BinSide feederSide = state.FeederSide.Value;
                    active = FormatSide(feederSide) + " Feeder " + operationName;
                    if (operation == ManualOutputBatchOperation.Load && state.FeederComplete)
                        return Fail(actions, completed, -1, active + " 불가: 완료 Bin이 남아 있습니다. OUTPUT UNLOAD로 먼저 배출하세요.");
                    if (operation == ManualOutputBatchOperation.Unload && !state.FeederComplete)
                        return Fail(actions, completed, -1, active + " 불가: 미완료 Bin은 강제 반환하지 않습니다. OUTPUT LOAD 재개 조건을 확인하세요.");
                    if (IsPresent(state, feederSide))
                        return Fail(actions, completed, -1, active + " 불가: 같은 Side Stage에 자재가 있어 Feeder 이송을 재개할 수 없습니다. Stage가 빈 복구 조건을 확인하세요.");

                    Report(actions, completed, active + " 중");
                    CheckCanContinue(actions, ct);
                    int result = await actions.ExecuteFeederAsync(operation, feederSide, ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail(actions, completed, result, active + " 실패: " + GetFailureReason(actions));

                    // 하위 이송이 성공한 실제 상태는 정지/취소 검사보다 먼저 저장합니다.
                    // 다음 Side가 실패하거나 정지해도 이미 끝난 이송 상태를 보존해야 합니다.
                    completed.Add(active + " 완료");
                    active += " 완료 상태 저장";
                    actions.SaveCheckpoint(completed[completed.Count - 1]);
                    active = FormatSide(feederSide) + " Feeder " + operationName + " 완료 상태 확인";
                    ManualOutputBatchState after = await ReadCheckedStateAsync(actions, ngEnabled, ct).ConfigureAwait(false);
                    invalidReason = GetPostconditionReason(operation, feederSide, true, state, after);
                    if (invalidReason != null)
                        return Fail(actions, completed, -1, active + " 실패: " + invalidReason);
                }

                // 빈 Stage 공급/잔류 Stage 배출 순서는 Auto와 같은 NG → GOOD입니다.
                // 각 Side 직전에 다시 읽어 Feeder 재개로 이미 끝난 작업을 중복 실행하지 않습니다.
                foreach (BinSide side in new[] { BinSide.Ng, BinSide.Good })
                {
                    active = FormatSide(side) + " " + operationName + " 준비";
                    state = await ReadCheckedStateAsync(actions, ngEnabled, ct).ConfigureAwait(false);
                    if (state.FeederSide.HasValue)
                        return Fail(actions, completed, -1, active + " 실패: Feeder 잔류 자재를 확인하세요. 후속 이송을 시작하지 않습니다.");
                    invalidReason = GetLoadDisabledNgReason(operation, state);
                    if (invalidReason != null)
                        return Fail(actions, completed, -1, invalidReason);
                    if (operation == ManualOutputBatchOperation.Load && side == BinSide.Ng && !ngEnabled)
                        continue;
                    bool needsWork = operation == ManualOutputBatchOperation.Load ? !IsPresent(state, side) : IsPresent(state, side);
                    if (!needsWork)
                        continue;

                    active = FormatSide(side) + " " + operationName;
                    Report(actions, completed, active + " 중");
                    CheckCanContinue(actions, ct);
                    int result = await actions.ExecuteSideAsync(operation, side, ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail(actions, completed, result, active + " 실패: " + GetFailureReason(actions));

                    completed.Add(active + " 완료");
                    active += " 완료 상태 저장";
                    actions.SaveCheckpoint(completed[completed.Count - 1]);
                    active = FormatSide(side) + " " + operationName + " 완료 상태 확인";
                    ManualOutputBatchState after = await ReadCheckedStateAsync(actions, ngEnabled, ct).ConfigureAwait(false);
                    invalidReason = GetPostconditionReason(operation, side, false, state, after);
                    if (invalidReason != null)
                        return Fail(actions, completed, -1, active + " 실패: " + invalidReason);
                }

                active = "ALL " + operationName + " 최종 상태 확인";
                state = await ReadCheckedStateAsync(actions, ngEnabled, ct).ConfigureAwait(false);
                invalidReason = GetFinalStateReason(operation, state);
                if (invalidReason != null)
                    return Fail(actions, completed, -1, active + " 실패: " + invalidReason);

                CheckCanContinue(actions, ct);
                int completionResult = await actions.CompleteAsync(operation, completed.Count > 0, ct).ConfigureAwait(false);
                if (completionResult != 0)
                    return Fail(actions, completed, completionResult, active + " 실패: " + GetFailureReason(actions));

                // 마지막 안전 자세 완료 뒤에도 정지와 자재 변화를 확인하고 성공을 확정합니다.
                state = await ReadCheckedStateAsync(actions, ngEnabled, ct).ConfigureAwait(false);
                invalidReason = GetFinalStateReason(operation, state);
                if (invalidReason != null)
                    return Fail(actions, completed, -1, active + " 실패: " + invalidReason);
                Report(actions, completed, completed.Count == 0 ? "처리할 대상 없음" : "ALL " + operationName + " 완료");
                return 0;
            }
            catch (Exception ex)
            {
                bool isStop = ex is OperationCanceledException || SequenceStopException.IsSequenceStop(ex);
                LastMessage = JoinSummary(completed, active + (isStop ? " 중 정지: " : " 실패: ") + ex.Message);
                // 정상 정지 예외를 고장으로 변환하지 않고 Controller까지 원래 예외로 전달합니다.
                throw;
            }
        }

        private static async Task<ManualOutputBatchState> ReadCheckedStateAsync(IManualOutputBatchActions actions, bool? ngEnabled, CancellationToken ct)
        {
            CheckCanContinue(actions, ct);
            ManualOutputBatchState state = await actions.ReadStateAsync(ct).ConfigureAwait(false);
            CheckCanContinue(actions, ct);
            if (state == null)
                throw new InvalidOperationException("ALL 작업의 센서/Material 상태를 확인할 수 없습니다.");
            if (ngEnabled.HasValue && state.NgEnabled != ngEnabled.Value)
                throw new InvalidOperationException("ALL 실행 중 NG 사용 설정이 변경되었습니다. 현재 자재를 확인한 뒤 다시 실행하세요.");
            if (state.FeederSide.HasValue && state.FeederSide.Value != BinSide.Ng && state.FeederSide.Value != BinSide.Good)
                throw new InvalidOperationException("Feeder 자재의 NG/GOOD 구분을 확인할 수 없습니다.");
            if (!state.FeederSide.HasValue && state.FeederComplete)
                throw new InvalidOperationException("Feeder 자재와 완료 상태가 일치하지 않습니다.");
            return state;
        }

        private static void CheckCanContinue(IManualOutputBatchActions actions, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            actions.CheckCanContinue(ct);
        }

        private static string GetLoadDisabledNgReason(ManualOutputBatchOperation operation, ManualOutputBatchState state)
        {
            if (operation == ManualOutputBatchOperation.Load && !state.NgEnabled &&
                (state.NgPresent || state.FeederSide == BinSide.Ng))
                return "NG 미사용 설정에 NG 잔류 자재가 있습니다. OUTPUT UNLOAD 가능 여부와 복구 상태를 확인하세요.";
            return null;
        }

        private static string GetPostconditionReason(ManualOutputBatchOperation operation, BinSide side, bool feederAction, ManualOutputBatchState before, ManualOutputBatchState after)
        {
            if (after.FeederSide.HasValue)
                return "이송 성공 후에도 Feeder에 자재가 남아 있습니다.";
            bool expectedPresent = operation == ManualOutputBatchOperation.Load || (feederAction && IsPresent(before, side));
            if (IsPresent(after, side) != expectedPresent)
                return FormatSide(side) + (expectedPresent ? " Stage의 로딩 완료 자재를 확인할 수 없습니다." : " Stage에 배출 대상 자재가 남아 있습니다.");
            BinSide otherSide = side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
            if (IsPresent(after, otherSide) != IsPresent(before, otherSide))
                return "작업하지 않은 " + FormatSide(otherSide) + " Stage의 자재 상태가 변경되었습니다.";
            return GetLoadDisabledNgReason(operation, after);
        }

        private static string GetFinalStateReason(ManualOutputBatchOperation operation, ManualOutputBatchState state)
        {
            if (state.FeederSide.HasValue)
                return "Feeder에 자재가 남아 있습니다.";
            string disabledReason = GetLoadDisabledNgReason(operation, state);
            if (disabledReason != null)
                return disabledReason;
            if (operation == ManualOutputBatchOperation.Load)
            {
                if (!state.GoodPresent || (state.NgEnabled && !state.NgPresent))
                    return "필요한 NG/GOOD Stage의 로딩 완료 상태가 유지되지 않았습니다.";
            }
            else if (state.GoodPresent || state.NgPresent)
            {
                return "NG/GOOD Stage에 배출 대상 자재가 남아 있습니다.";
            }
            return null;
        }

        private int Fail(IManualOutputBatchActions actions, List<string> completed, int result, string reason)
        {
            Report(actions, completed, reason);
            return result;
        }

        private void Report(IManualOutputBatchActions actions, List<string> completed, string detail)
        {
            LastMessage = JoinSummary(completed, detail);
            actions.Report(LastMessage);
        }

        private static string GetFailureReason(IManualOutputBatchActions actions)
        {
            string reason = actions.GetFailureReason();
            return string.IsNullOrWhiteSpace(reason) ? "하위 시퀀스가 실패했습니다. 장비 알람과 자재 상태를 확인하세요." : reason;
        }

        private static string JoinSummary(List<string> completed, string detail)
        {
            return completed.Count == 0 ? detail : string.Join(" / ", completed) + " / " + detail;
        }

        private static bool IsPresent(ManualOutputBatchState state, BinSide side)
        {
            return side == BinSide.Ng ? state.NgPresent : state.GoodPresent;
        }

        private static string FormatSide(BinSide side)
        {
            return side == BinSide.Ng ? "NG" : "GOOD";
        }
    }
}
