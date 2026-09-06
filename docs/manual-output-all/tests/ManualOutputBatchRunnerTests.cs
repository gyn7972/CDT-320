using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Sequencing;

// 장비 어셈블리를 참조하지 않기 위한 물리 Side 타입의 최소 정의입니다.
namespace QMC.CDT320
{
    public enum BinSide { Ng, Good }
}

internal static class ManualOutputBatchRunnerTests
{
    private static int _passed;

    private static int Main()
    {
        try
        {
            Run("empty stages load NG then GOOD", LoadBoth);
            Run("occupied stages unload NG then GOOD without resupply", UnloadBoth);
            Run("load skips either occupied stage", LoadPartial);
            Run("unload skips either empty stage", UnloadPartial);
            Run("no-op still validates final guard and performs no transfer", NoWork);
            Run("disabled NG loads GOOD only", DisabledNgLoad);
            Run("disabled NG residual blocks load", DisabledNgResidualLoad);
            Run("disabled NG residual is still unloaded", DisabledNgResidualUnload);
            Run("incomplete feeder load resumes before remaining stages", ResumeFeederLoad);
            Run("completed feeder unload precedes other-side stage", ResumeFeederUnload);
            Run("completed feeder unload cannot restore an occupied same-side stage", OccupiedFeederUnloadStageBlocked);
            Run("completed feeder cannot be loaded", CompletedFeederLoadBlocked);
            Run("incomplete feeder cannot be forcibly unloaded", IncompleteFeederUnloadBlocked);
            Run("feeder cannot load onto occupied stage", OccupiedFeederStageBlocked);
            Run("first failure retains result and never starts second", FirstFailure);
            Run("second failure preserves first checkpoint and partial result", SecondFailure);
            Run("successful action is saved before canceled continuation", CancellationAfterAction);
            Run("cycle stop preserves original exception and completed state", CycleStopAfterCheckpoint);
            Run("success without stage progress blocks subsequent action", NoStageProgress);
            Run("success with occupied feeder blocks subsequent action", NoFeederProgress);
            Run("NG setting change stops batch after saving actual action", ConfigurationChanged);
            Run("untouched stage changing during transfer is rejected", OtherStageChanged);
            Run("final posture failure retains material completion summary", FinalGuardFailure);
            Run("material changes during final guard prevent completion", FinalMaterialChanged);
            Run("checkpoint failure does not start subsequent side", CheckpointFailure);
            Run("async transfer is awaited and one cancellation token is retained", SequentialAwait);
            Run("cancellation during async state check prevents command", CancelDuringRead);
            Run("pre-canceled request does not read or move", AlreadyCanceled);
            Run("missing state is rejected before transfer", MissingState);
            Console.WriteLine("PASS: " + _passed + " independent ALL runner scenarios. No equipment assemblies or hardware were loaded.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL after " + _passed + " scenarios: " + ex);
            return 1;
        }
    }

    private static void Run(string name, Action test)
    {
        test();
        _passed++;
        Console.WriteLine("PASS " + _passed + ": " + name);
    }

    private static int Execute(ManualOutputBatchRunner runner, FakeActions actions, ManualOutputBatchOperation operation, CancellationToken ct = default(CancellationToken))
    {
        actions.ExpectedToken = ct;
        return runner.RunAsync(operation, actions, ct).GetAwaiter().GetResult();
    }

    private static void LoadBoth()
    {
        var actions = new FakeActions();
        var runner = new ManualOutputBatchRunner();
        Equal(0, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Commands(actions, "Load:NG", "Load:GOOD");
        Equal(2, actions.Checkpoints.Count);
        True(actions.State.GoodPresent && actions.State.NgPresent, "Both stages must be occupied.");
        Equal(1, actions.CompleteCalls);
        True(actions.CompletedWithWork, "Final posture must receive actual-work flag.");
        Contains(runner.LastMessage, "ALL 로딩 완료");
    }

    private static void UnloadBoth()
    {
        var actions = new FakeActions(true, true);
        Equal(0, Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Unload));
        Commands(actions, "Unload:NG", "Unload:GOOD");
        True(!actions.State.GoodPresent && !actions.State.NgPresent, "No refill is allowed.");
        Equal(2, actions.Checkpoints.Count);
    }

    private static void LoadPartial()
    {
        foreach (bool ngPresent in new[] { false, true })
        {
            var actions = new FakeActions(ngPresent, !ngPresent);
            Equal(0, Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Load));
            Commands(actions, ngPresent ? "Load:GOOD" : "Load:NG");
        }
    }

    private static void UnloadPartial()
    {
        foreach (bool ngPresent in new[] { false, true })
        {
            var actions = new FakeActions(ngPresent, !ngPresent);
            Equal(0, Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Unload));
            Commands(actions, ngPresent ? "Unload:NG" : "Unload:GOOD");
        }
    }

    private static void NoWork()
    {
        foreach (ManualOutputBatchOperation operation in new[] { ManualOutputBatchOperation.Load, ManualOutputBatchOperation.Unload })
        {
            bool load = operation == ManualOutputBatchOperation.Load;
            var actions = new FakeActions(load, load);
            var runner = new ManualOutputBatchRunner();
            Equal(0, Execute(runner, actions, operation));
            Commands(actions);
            Equal(0, actions.Checkpoints.Count);
            Equal(1, actions.CompleteCalls);
            True(!actions.CompletedWithWork, "No-op must not request final motion.");
            Equal("처리할 대상 없음", runner.LastMessage);
        }
    }

    private static void DisabledNgLoad()
    {
        var actions = new FakeActions { State = { NgEnabled = false } };
        Equal(0, Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Load));
        Commands(actions, "Load:GOOD");
    }

    private static void DisabledNgResidualLoad()
    {
        foreach (bool feederResidual in new[] { false, true })
        {
            var actions = new FakeActions { State = { NgEnabled = false, NgPresent = !feederResidual, FeederSide = feederResidual ? (BinSide?)BinSide.Ng : null } };
            var runner = new ManualOutputBatchRunner();
            Equal(-1, Execute(runner, actions, ManualOutputBatchOperation.Load));
            Commands(actions);
            Contains(runner.LastMessage, "NG 잔류 자재");
            Contains(runner.LastMessage, "OUTPUT UNLOAD");
        }
    }

    private static void DisabledNgResidualUnload()
    {
        var actions = new FakeActions(true, true) { State = { NgEnabled = false } };
        Equal(0, Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Unload));
        Commands(actions, "Unload:NG", "Unload:GOOD");
    }

    private static void ResumeFeederLoad()
    {
        var actions = new FakeActions { State = { FeederSide = BinSide.Good, FeederComplete = false } };
        Equal(0, Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Load));
        Commands(actions, "FeederLoad:GOOD", "Load:NG");
        Equal(2, actions.Checkpoints.Count);
    }

    private static void ResumeFeederUnload()
    {
        foreach (BinSide feederSide in new[] { BinSide.Ng, BinSide.Good })
        {
            bool ngFeeder = feederSide == BinSide.Ng;
            var actions = new FakeActions(!ngFeeder, ngFeeder) { State = { FeederSide = feederSide, FeederComplete = true } };
            Equal(0, Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Unload));
            Commands(actions, ngFeeder ? "FeederUnload:NG" : "FeederUnload:GOOD", ngFeeder ? "Unload:GOOD" : "Unload:NG");
            Equal(2, actions.Checkpoints.Count);
        }
    }

    private static void OccupiedFeederUnloadStageBlocked()
    {
        foreach (BinSide feederSide in new[] { BinSide.Ng, BinSide.Good })
        {
            var actions = new FakeActions(true, true) { State = { FeederSide = feederSide, FeederComplete = true } };
            var runner = new ManualOutputBatchRunner();
            Equal(-1, Execute(runner, actions, ManualOutputBatchOperation.Unload));
            Commands(actions);
            Equal(0, actions.Checkpoints.Count);
            Contains(runner.LastMessage, "같은 Side Stage");
        }
    }

    private static void CompletedFeederLoadBlocked()
    {
        var actions = new FakeActions { State = { FeederSide = BinSide.Good, FeederComplete = true } };
        var runner = new ManualOutputBatchRunner();
        Equal(-1, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Commands(actions);
        Contains(runner.LastMessage, "OUTPUT UNLOAD");
    }

    private static void IncompleteFeederUnloadBlocked()
    {
        var actions = new FakeActions { State = { FeederSide = BinSide.Ng, FeederComplete = false } };
        var runner = new ManualOutputBatchRunner();
        Equal(-1, Execute(runner, actions, ManualOutputBatchOperation.Unload));
        Commands(actions);
        Contains(runner.LastMessage, "강제 반환하지 않습니다");
    }

    private static void OccupiedFeederStageBlocked()
    {
        var actions = new FakeActions(false, true) { State = { FeederSide = BinSide.Good } };
        Equal(-1, Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Load));
        Commands(actions);
    }

    private static void FirstFailure()
    {
        var actions = new FakeActions { FailedAction = 1, FailureCode = -23 };
        var runner = new ManualOutputBatchRunner();
        Equal(-23, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Commands(actions, "Load:NG");
        Equal(0, actions.Checkpoints.Count);
        Equal(0, actions.CompleteCalls);
        Contains(runner.LastMessage, "NG 로딩 실패");
        Contains(runner.LastMessage, actions.FailureReason);
    }

    private static void SecondFailure()
    {
        var actions = new FakeActions { FailedAction = 2, FailureCode = -27 };
        var runner = new ManualOutputBatchRunner();
        Equal(-27, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Commands(actions, "Load:NG", "Load:GOOD");
        Equal(1, actions.Checkpoints.Count);
        True(actions.State.NgPresent && !actions.State.GoodPresent, "First completed state must survive.");
        Contains(runner.LastMessage, "NG 로딩 완료");
        Contains(runner.LastMessage, "GOOD 로딩 실패");
    }

    private static void CancellationAfterAction()
    {
        using (var cancellation = new CancellationTokenSource())
        {
            var actions = new FakeActions { AfterAction = a => cancellation.Cancel() };
            var runner = new ManualOutputBatchRunner();
            Throws<OperationCanceledException>(() => Execute(runner, actions, ManualOutputBatchOperation.Load, cancellation.Token));
            Commands(actions, "Load:NG");
            Equal(1, actions.Checkpoints.Count);
            True(actions.Events.IndexOf("save") > actions.Events.IndexOf("action-complete"), "Success must be saved despite immediate cancellation.");
            Contains(runner.LastMessage, "NG 로딩 완료");
            Contains(runner.LastMessage, "정지");
        }
    }

    private static void CycleStopAfterCheckpoint()
    {
        var stop = new SequenceStopException("CYCLE STOP 테스트");
        var actions = new FakeActions(true, true) { OnCheck = a => { if (a.Checkpoints.Count > 0) throw stop; } };
        var runner = new ManualOutputBatchRunner();
        Exception received = Throws<SequenceStopException>(() => Execute(runner, actions, ManualOutputBatchOperation.Unload));
        True(ReferenceEquals(stop, received), "Original cycle stop must reach caller.");
        Commands(actions, "Unload:NG");
        Equal(1, actions.Checkpoints.Count);
        Contains(runner.LastMessage, "NG 언로딩 완료");
        Contains(runner.LastMessage, "정지");
    }

    private static void NoStageProgress()
    {
        var actions = new FakeActions { SuppressStateChange = true };
        var runner = new ManualOutputBatchRunner();
        Equal(-1, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Commands(actions, "Load:NG");
        Contains(runner.LastMessage, "완료 상태 확인 실패");
        Equal(0, actions.CompleteCalls);
    }

    private static void NoFeederProgress()
    {
        var actions = new FakeActions { State = { FeederSide = BinSide.Good }, SuppressStateChange = true };
        var runner = new ManualOutputBatchRunner();
        Equal(-1, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Commands(actions, "FeederLoad:GOOD");
        Contains(runner.LastMessage, "Feeder에 자재가 남아");
    }

    private static void ConfigurationChanged()
    {
        var actions = new FakeActions { AfterAction = a => a.State.NgEnabled = false };
        var runner = new ManualOutputBatchRunner();
        Throws<InvalidOperationException>(() => Execute(runner, actions, ManualOutputBatchOperation.Load));
        Commands(actions, "Load:NG");
        Equal(1, actions.Checkpoints.Count);
        Contains(runner.LastMessage, "NG 사용 설정이 변경");
    }

    private static void OtherStageChanged()
    {
        var actions = new FakeActions { AfterAction = a => a.State.GoodPresent = true };
        var runner = new ManualOutputBatchRunner();
        Equal(-1, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Commands(actions, "Load:NG");
        Contains(runner.LastMessage, "작업하지 않은 GOOD");
    }

    private static void FinalGuardFailure()
    {
        var actions = new FakeActions { CompleteCode = -31 };
        var runner = new ManualOutputBatchRunner();
        Equal(-31, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Equal(2, actions.Checkpoints.Count);
        Contains(runner.LastMessage, "NG 로딩 완료");
        Contains(runner.LastMessage, "GOOD 로딩 완료");
        Contains(runner.LastMessage, "최종 상태 확인 실패");
        True(!runner.LastMessage.Contains("ALL 로딩 완료"), "Final guard failure must not claim ALL completion.");
    }

    private static void FinalMaterialChanged()
    {
        var actions = new FakeActions { OnComplete = a => a.State.NgPresent = false };
        var runner = new ManualOutputBatchRunner();
        Equal(-1, Execute(runner, actions, ManualOutputBatchOperation.Load));
        Contains(runner.LastMessage, "로딩 완료 상태가 유지되지 않았습니다");
    }

    private static void CheckpointFailure()
    {
        var saveError = new InvalidOperationException("테스트 저장 실패");
        var actions = new FakeActions { OnSave = a => { throw saveError; } };
        var runner = new ManualOutputBatchRunner();
        Exception received = Throws<InvalidOperationException>(() => Execute(runner, actions, ManualOutputBatchOperation.Load));
        True(ReferenceEquals(saveError, received), "Save exception must be preserved.");
        Commands(actions, "Load:NG");
        Contains(runner.LastMessage, "NG 로딩 완료");
        Contains(runner.LastMessage, "완료 상태 저장 실패");
    }

    private static void SequentialAwait()
    {
        using (var cancellation = new CancellationTokenSource())
        {
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var actions = new FakeActions
            {
                ExpectedToken = cancellation.Token,
                BeforeActionAsync = a =>
                {
                    if (a.Commands.Count == 1)
                    {
                        entered.SetResult(true);
                        return gate.Task;
                    }
                    return Task.CompletedTask;
                }
            };
            var runner = new ManualOutputBatchRunner();
            Task<int> run = runner.RunAsync(ManualOutputBatchOperation.Load, actions, cancellation.Token);
            True(entered.Task.Wait(TimeSpan.FromSeconds(3)), "NG transfer must start.");
            Commands(actions, "Load:NG");
            Equal(0, actions.Checkpoints.Count);
            True(!run.IsCompleted, "Batch must await NG completion.");
            gate.SetResult(true);
            True(run.Wait(TimeSpan.FromSeconds(3)), "Batch should finish after NG gate release.");
            Equal(0, run.GetAwaiter().GetResult());
            Commands(actions, "Load:NG", "Load:GOOD");
            Equal(1, actions.MaximumConcurrentActions);
        }
    }

    private static void CancelDuringRead()
    {
        using (var cancellation = new CancellationTokenSource())
        {
            var actions = new FakeActions { OnRead = a => cancellation.Cancel() };
            Throws<OperationCanceledException>(() => Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Load, cancellation.Token));
            Commands(actions);
        }
    }

    private static void AlreadyCanceled()
    {
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            var actions = new FakeActions();
            Throws<OperationCanceledException>(() => Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Load, cancellation.Token));
            Commands(actions);
            Equal(0, actions.ReadCalls);
        }
    }

    private static void MissingState()
    {
        var actions = new FakeActions { ReturnMissingState = true };
        Throws<InvalidOperationException>(() => Execute(new ManualOutputBatchRunner(), actions, ManualOutputBatchOperation.Load));
        Commands(actions);
    }

    private static void Commands(FakeActions actions, params string[] expected)
    {
        Equal(string.Join(",", expected), string.Join(",", actions.Commands));
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception("Expected <" + expected + ">, got <" + actual + ">.");
    }

    private static void True(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }

    private static void Contains(string value, string expected)
    {
        True(value != null && value.Contains(expected), "Expected message <" + expected + "> in <" + value + ">.");
    }

    private static T Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T ex) { return ex; }
        throw new Exception("Expected exception " + typeof(T).Name + ".");
    }

    private sealed class FakeActions : IManualOutputBatchActions
    {
        private int _activeActions;
        public ManualOutputBatchState State { get; set; }
        public List<string> Commands { get; } = new List<string>();
        public List<string> Checkpoints { get; } = new List<string>();
        public List<string> Events { get; } = new List<string>();
        public CancellationToken ExpectedToken { get; set; }
        public int FailedAction { get; set; }
        public int FailureCode { get; set; } = -19;
        public string FailureReason { get; set; } = "테스트 하위 동작 실패";
        public int CompleteCode { get; set; }
        public int CompleteCalls { get; private set; }
        public int ReadCalls { get; private set; }
        public int MaximumConcurrentActions { get; private set; }
        public bool CompletedWithWork { get; private set; }
        public bool SuppressStateChange { get; set; }
        public bool ReturnMissingState { get; set; }
        public Action<FakeActions> AfterAction { get; set; }
        public Action<FakeActions> OnSave { get; set; }
        public Action<FakeActions> OnCheck { get; set; }
        public Action<FakeActions> OnRead { get; set; }
        public Action<FakeActions> OnComplete { get; set; }
        public Func<FakeActions, Task> BeforeActionAsync { get; set; }

        public FakeActions(bool ngPresent = false, bool goodPresent = false)
        {
            State = new ManualOutputBatchState { NgEnabled = true, NgPresent = ngPresent, GoodPresent = goodPresent };
        }

        public Task<ManualOutputBatchState> ReadStateAsync(CancellationToken ct)
        {
            Equal(ExpectedToken, ct);
            ReadCalls++;
            OnRead?.Invoke(this);
            if (ReturnMissingState)
                return Task.FromResult<ManualOutputBatchState>(null);
            return Task.FromResult(new ManualOutputBatchState
            {
                NgEnabled = State.NgEnabled,
                GoodPresent = State.GoodPresent,
                NgPresent = State.NgPresent,
                FeederSide = State.FeederSide,
                FeederComplete = State.FeederComplete
            });
        }

        public void CheckCanContinue(CancellationToken ct)
        {
            Equal(ExpectedToken, ct);
            ct.ThrowIfCancellationRequested();
            OnCheck?.Invoke(this);
        }

        public Task<int> ExecuteFeederAsync(ManualOutputBatchOperation operation, BinSide side, CancellationToken ct)
        {
            return ExecuteActionAsync(operation, side, true, ct);
        }

        public Task<int> ExecuteSideAsync(ManualOutputBatchOperation operation, BinSide side, CancellationToken ct)
        {
            return ExecuteActionAsync(operation, side, false, ct);
        }

        public void SaveCheckpoint(string description)
        {
            Events.Add("save");
            Checkpoints.Add(description);
            OnSave?.Invoke(this);
        }

        public Task<int> CompleteAsync(ManualOutputBatchOperation operation, bool anyWork, CancellationToken ct)
        {
            Equal(ExpectedToken, ct);
            CompleteCalls++;
            CompletedWithWork = anyWork;
            OnComplete?.Invoke(this);
            return Task.FromResult(CompleteCode);
        }

        public string GetFailureReason() { return FailureReason; }
        public void Report(string message) { Events.Add("report:" + message); }

        private async Task<int> ExecuteActionAsync(ManualOutputBatchOperation operation, BinSide side, bool feeder, CancellationToken ct)
        {
            Equal(ExpectedToken, ct);
            _activeActions++;
            MaximumConcurrentActions = Math.Max(MaximumConcurrentActions, _activeActions);
            try
            {
                Commands.Add((feeder ? "Feeder" : string.Empty) + operation + ":" + (side == BinSide.Ng ? "NG" : "GOOD"));
                if (BeforeActionAsync != null)
                    await BeforeActionAsync(this).ConfigureAwait(false);
                if (FailedAction == Commands.Count)
                    return FailureCode;
                if (!SuppressStateChange)
                {
                    State.FeederSide = null;
                    State.FeederComplete = false;
                    if (!feeder || operation == ManualOutputBatchOperation.Load)
                    {
                        if (side == BinSide.Ng) State.NgPresent = operation == ManualOutputBatchOperation.Load;
                        else State.GoodPresent = operation == ManualOutputBatchOperation.Load;
                    }
                }
                Events.Add("action-complete");
                AfterAction?.Invoke(this);
                return 0;
            }
            finally
            {
                _activeActions--;
            }
        }
    }
}
