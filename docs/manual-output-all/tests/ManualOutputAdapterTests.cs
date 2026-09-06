using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.Common.Alarms;

internal static class ManualOutputAdapterTests
{
    private static int _passed;

    private static int Main()
    {
        try
        {
            Run("actual adapter calls Manual LOAD in NG then GOOD order", ManualLoad);
            Run("actual adapter calls Manual UNLOAD and never refills", ManualUnload);
            Run("feeder sensor mismatch blocks before lower transfer", FeederSensorMismatch);
            Run("stage sensor mismatch blocks before lower transfer", StageSensorMismatch);
            Run("same-side occupied stage blocks completed feeder unload", SameSideFeederBlocked);
            Run("other-side stage allows completed feeder resume first", OtherSideFeederResume);
            Run("stage source cassette mismatch blocks before transfer", SourceCassetteMismatch);
            Run("material grade mismatch blocks before transfer", GradeMismatch);
            Run("duplicate active stage material blocks before transfer", DuplicateMaterial);
            Run("material identity change during strict sensor checks blocks", MaterialChangedDuringRead);
            Run("material identity change after progress before move blocks", MaterialChangedBeforeMove);
            Run("ready remains suppressed throughout lower transfers", ReadyAfterWholeBatchOnly);
            Run("post-ready final sensor failure clears all ready signals", FailureAfterReadyClearsSignals);
            Run("final axis position mismatch prevents successful completion", FinalPoseMismatch);
            Run("final picker avoid failure prevents ready", FinalPickerMismatch);
            Run("occupied stage insecure cylinder state cannot be skipped", OccupiedStageUnsafe);
            Run("occupied stage unconfirmed required barcode cannot be skipped", OccupiedStageBarcode);
            Run("checkpoint cancellation propagates and clears ready", CancellationAtCheckpoint);
            Run("non-manual mode is rejected before lower transfer", AutoModeBlocked);
            Console.WriteLine("PASS: " + _passed + " actual ALL adapter integration scenarios against memory-only boundary stubs. No hardware assemblies were loaded.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL after " + _passed + " scenarios: " + ex);
            return 1;
        }
    }

    private static OutputSequence Create()
    {
        AlarmManager.HasActive = false;
        SequenceFailureStore.Clear();
        MaterialStateService.State = new MaterialSnapshot();
        AppSettingsStore.Current = new AppSettings();
        return new OutputSequence();
    }

    private static WaferMaterial Add(OutputSequence sequence, BinSide side, MaterialLocationKind location, bool complete = false)
    {
        var bin = OutputSequence.CreateBin(side, location);
        bin.Complete = complete;
        MaterialStateService.State.Wafers.Add(bin);
        sequence.SyncTestSensors();
        return bin;
    }

    private static int Execute(OutputSequence sequence, bool load, IProgress<string> progress = null)
    {
        return sequence.ExecuteManualOutputBatchAsync(load, CancellationToken.None, description => { }, progress).GetAwaiter().GetResult();
    }

    private static void ManualLoad()
    {
        var sequence = Create();
        Equal(0, Execute(sequence, true));
        Commands(sequence, "ManualLoad:Ng", "ManualLoad:Good");
        True(sequence.ActiveDuringTransfer.All(value => value), "Batch active flag must span all transfers.");
        True(sequence.Context.Bus.IsSet("OutputStageReady"), "Ready is expected after final validation.");
    }

    private static void ManualUnload()
    {
        var sequence = Create();
        Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood);
        Add(sequence, BinSide.Ng, MaterialLocationKind.OutputStageNg);
        Equal(0, Execute(sequence, false));
        Commands(sequence, "ManualUnload:Ng", "ManualUnload:Good");
        True(!sequence.Context.Bus.IsSet("OutputStageReady"), "Unload must not signal a loaded stage.");
    }

    private static void FeederSensorMismatch()
    {
        var sequence = Create();
        sequence.Context.Machine.OutputFeederUnit.RingPresent = true;
        FailsBeforeMove(sequence, true, "센서와 Material");
    }

    private static void StageSensorMismatch()
    {
        var sequence = Create();
        Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood);
        sequence.Context.Machine.OutputStageUnit.GoodBinRingSensor.Present = false;
        FailsBeforeMove(sequence, true, "센서와 Material");
    }

    private static void SameSideFeederBlocked()
    {
        var sequence = Create();
        Add(sequence, BinSide.Ng, MaterialLocationKind.OutputFeeder, true);
        Add(sequence, BinSide.Ng, MaterialLocationKind.OutputStageNg);
        FailsBeforeMove(sequence, false, "같은 Side Stage");
    }

    private static void OtherSideFeederResume()
    {
        var sequence = Create();
        Add(sequence, BinSide.Ng, MaterialLocationKind.OutputFeeder, true);
        Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood);
        Equal(0, Execute(sequence, false));
        Commands(sequence, "FeederResume", "ManualUnload:Good");
    }

    private static void SourceCassetteMismatch()
    {
        var sequence = Create();
        Add(sequence, BinSide.Ng, MaterialLocationKind.OutputStageGood);
        FailsBeforeMove(sequence, false, "원본 카세트가 일치하지 않습니다");
    }

    private static void GradeMismatch()
    {
        var sequence = Create();
        Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood).OutputGrade = BinSide.Ng;
        FailsBeforeMove(sequence, true, "원본 카세트가 일치하지 않습니다");
    }

    private static void DuplicateMaterial()
    {
        var sequence = Create();
        Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood);
        Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood);
        FailsBeforeMove(sequence, true, "중복 Material");
    }

    private static void MaterialChangedDuringRead()
    {
        var sequence = Create();
        WaferMaterial bin = Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood);
        sequence.Context.Machine.OutputFeederUnit.BeforeConfirmation = () => bin.SourceSlotNumber++;
        FailsBeforeMove(sequence, true, "센서 확인 중 자재");
    }

    private static void MaterialChangedBeforeMove()
    {
        var sequence = Create();
        var progress = new InlineProgress(message =>
        {
            if (message.Contains("NG 로딩 중"))
                Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood);
        });
        Equal(-1, Execute(sequence, true, progress));
        Commands(sequence);
        Contains(sequence.LastManualOutputBatchMessage, "이송 시작 직전 자재 상태");
    }

    private static void ReadyAfterWholeBatchOnly()
    {
        var sequence = Create();
        sequence.AfterTransfer = owner => True(!owner.Context.Bus.IsSet("OutputStageReady"), "Ready must not be published between sides.");
        Equal(0, Execute(sequence, true));
        True(sequence.Context.Bus.IsSet("OutputStageReady"), "Ready must be published after all material work.");
    }

    private static void FailureAfterReadyClearsSignals()
    {
        var sequence = Create();
        sequence.Context.Machine.OutputFeederUnit.BeforeConfirmation = () =>
        {
            if (sequence.Context.Bus.IsSet("OutputStageReady"))
                sequence.Context.Machine.OutputStageUnit.GoodBinRingSensor.Present = false;
        };
        Equal(-1, Execute(sequence, true));
        Equal(2, sequence.TestCommands.Count);
        NoReady(sequence);
        Contains(sequence.LastManualOutputBatchMessage, "GOOD/Feeder 센서");
    }

    private static void FinalPoseMismatch()
    {
        var sequence = Create();
        sequence.AfterTransfer = owner => owner.Context.Machine.OutputStageUnit.OutputCameraX.ActualPosition = 17;
        Equal(-1, Execute(sequence, true));
        Equal(2, sequence.TestCommands.Count);
        Contains(sequence.LastManualOutputBatchMessage, "완료 위치 불일치");
        NoReady(sequence);
    }

    private static void FinalPickerMismatch()
    {
        var sequence = Create();
        sequence.PickersSafe = false;
        Equal(-1, Execute(sequence, true));
        Contains(sequence.LastManualOutputBatchMessage, "Picker Avoid");
        NoReady(sequence);
    }

    private static void OccupiedStageUnsafe()
    {
        foreach (BinSide side in new[] { BinSide.Good, BinSide.Ng })
        {
            var sequence = Create();
            Add(sequence, side, side == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood);
            sequence.Context.Machine.OutputStageUnit.GoodGuideSecure = side != BinSide.Good;
            sequence.Context.Machine.OutputStageUnit.NgGuideSecure = side != BinSide.Ng;
            FailsBeforeMove(sequence, true, "Guide Down/Clamp Lift Up/Clamp");
        }
    }

    private static void OccupiedStageBarcode()
    {
        var sequence = Create();
        AppSettingsStore.Current.UseOutputBinBarcode = true;
        Add(sequence, BinSide.Good, MaterialLocationKind.OutputStageGood).BarcodeConfirmed = false;
        FailsBeforeMove(sequence, true, "바코드 확인이 완료되지 않았습니다");
    }

    private static void CancellationAtCheckpoint()
    {
        var sequence = Create();
        int saved = 0;
        using (var cancellation = new CancellationTokenSource())
        {
            bool thrown = false;
            try
            {
                sequence.ExecuteManualOutputBatchAsync(true, cancellation.Token, description =>
                {
                    saved++;
                    cancellation.Cancel();
                }).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { thrown = true; }
            True(thrown, "Cancellation must reach Controller.");
        }
        Equal(1, saved);
        Commands(sequence, "ManualLoad:Ng");
        NoReady(sequence);
        True(!AlarmManager.HasActive, "Normal cancellation must not create an alarm in adapter.");
    }

    private static void AutoModeBlocked()
    {
        var sequence = Create();
        sequence.Mode = SequenceRunMode.Auto;
        FailsBeforeMove(sequence, true, "Manual 모드");
    }

    private static void FailsBeforeMove(OutputSequence sequence, bool load, string reason)
    {
        Equal(-1, Execute(sequence, load));
        Commands(sequence);
        Contains(sequence.LastManualOutputBatchMessage, reason);
        NoReady(sequence);
    }

    private static void NoReady(OutputSequence sequence)
    {
        foreach (string name in new[] { "OutputGoodStageReady", "OutputNgStageReady", "OutputStageReady" })
            True(!sequence.Context.Bus.IsSet(name), "Failure must clear " + name + ".");
    }

    private static void Commands(OutputSequence sequence, params string[] expected)
    {
        Equal(string.Join(",", expected), string.Join(",", sequence.TestCommands));
    }

    private static void Run(string name, Action test) { test(); Console.WriteLine("PASS " + ++_passed + ": " + name); }
    private static void True(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Equal<T>(T expected, T actual)
    {
        True(EqualityComparer<T>.Default.Equals(expected, actual), "Expected <" + expected + ">, got <" + actual + ">.");
    }
    private static void Contains(string actual, string text) { True(actual != null && actual.Contains(text), "Missing <" + text + "> in <" + actual + ">."); }
    private sealed class InlineProgress : IProgress<string>
    {
        private readonly Action<string> _report;
        public InlineProgress(Action<string> report) { _report = report; }
        public void Report(string value) { _report(value); }
    }
}
