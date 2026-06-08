using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    public enum InputStageSequenceKind
    {
        PrepareLoad,
        Align,
        PrepareUnload,
        MoveAvoid
    }

    public enum InputStageSequenceStep
    {
        Idle,
        CheckUnit,
        MoveLoadPosition,
        MoveAlignPosition,
        BuildDiePositions,
        MoveUnloadPosition,
        MoveAvoidPosition,
        Complete,
        Error
    }

    public sealed class InputStageSequenceOptions
    {
        public bool FineMove { get; set; }
        public bool EnableMotion { get; set; }
        public bool RequireMapData { get; set; }
        public bool RequireVisionAlign { get; set; }
        public string WaferId { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static InputStageSequenceOptions Default()
        {
            return new InputStageSequenceOptions
            {
                FineMove = false,
                EnableMotion = true,
                RequireMapData = false,
                RequireVisionAlign = false,
                WaferId = "",
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }

    public sealed class InputStageSequence
    {
        private const string SequenceNamePrefix = "InputStageSequence";
        private readonly MachineSequenceContext _context;
        private InputStageSequenceKind _kind;
        private InputStageSequenceOptions _options;
        private InputStageSequenceStep _currentStep;

        public InputStageSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException("context");
            _currentStep = InputStageSequenceStep.Idle;
        }

        public Task<int> RunPrepareLoadAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            return RunAsync(ct, InputStageSequenceKind.PrepareLoad, InputStageSequenceStep.CheckUnit, options);
        }

        public Task<int> RunAlignAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            return RunAsync(ct, InputStageSequenceKind.Align, InputStageSequenceStep.CheckUnit, options);
        }

        public Task<int> RunPrepareUnloadAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            return RunAsync(ct, InputStageSequenceKind.PrepareUnload, InputStageSequenceStep.CheckUnit, options);
        }

        public Task<int> RunMoveAvoidAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            return RunAsync(ct, InputStageSequenceKind.MoveAvoid, InputStageSequenceStep.CheckUnit, options);
        }

        private async Task<int> RunAsync(
            CancellationToken ct,
            InputStageSequenceKind kind,
            InputStageSequenceStep initialStep,
            InputStageSequenceOptions options)
        {
            try
            {
                _kind = kind;
                _options = options ?? InputStageSequenceOptions.Default();
                _currentStep = ResolveStartStep(initialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, _currentStep.ToString());

                while (_currentStep != InputStageSequenceStep.Complete &&
                       _currentStep != InputStageSequenceStep.Error)
                {
                    ct.ThrowIfCancellationRequested();
                    _context.LogPublic("[INPUT-STAGE] " + _options.RunMode + " " + _kind + " step=" + _currentStep);

                    InputStageSequenceStep executingStep = _currentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (_currentStep != InputStageSequenceStep.Error)
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), _currentStep.ToString());
                }

                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Input stage " + _kind + " sequence completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Input stage " + _kind + " sequence canceled at step=" + _currentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-EX", "InputStageSequence", "Input stage " + _kind + " exception at step=" + _currentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (_currentStep)
            {
                case InputStageSequenceStep.CheckUnit:
                    return CheckUnit(ResolveFirstActionStep());

                case InputStageSequenceStep.MoveLoadPosition:
                    return await MoveLoadPositionAsync().ConfigureAwait(false);

                case InputStageSequenceStep.MoveAlignPosition:
                    return await MoveAlignPositionAsync().ConfigureAwait(false);

                case InputStageSequenceStep.BuildDiePositions:
                    return BuildDiePositions();

                case InputStageSequenceStep.MoveUnloadPosition:
                    return await MoveUnloadPositionAsync().ConfigureAwait(false);

                case InputStageSequenceStep.MoveAvoidPosition:
                    return await MoveAvoidPositionAsync().ConfigureAwait(false);

                default:
                    return Fail("IN-STAGE-STEP", "InputStageSequence", "Unsupported input stage step: " + _currentStep);
            }
        }

        private static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
        {
            if (stepTask == null)
                return -1;

            if (stepTask.IsCompleted)
                return await stepTask.ConfigureAwait(false);

            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
            if (!ReferenceEquals(completed, stepTask))
                ct.ThrowIfCancellationRequested();

            return await stepTask.ConfigureAwait(false);
        }

        private InputStageSequenceStep ResolveFirstActionStep()
        {
            switch (_kind)
            {
                case InputStageSequenceKind.PrepareLoad:
                    return InputStageSequenceStep.MoveLoadPosition;
                case InputStageSequenceKind.Align:
                    return InputStageSequenceStep.MoveAlignPosition;
                case InputStageSequenceKind.PrepareUnload:
                    return InputStageSequenceStep.MoveUnloadPosition;
                case InputStageSequenceKind.MoveAvoid:
                    return InputStageSequenceStep.MoveAvoidPosition;
                default:
                    return InputStageSequenceStep.Error;
            }
        }

        private int CheckUnit(InputStageSequenceStep nextStep)
        {
            if (Stage == null)
                return Fail("IN-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            if (Stage.StageY == null || Stage.StageT == null || Stage.ExpanderZ == null || Stage.CameraX == null)
                return Fail("IN-STAGE-AXIS", Stage.Name, "Input stage axis is not available.");

            if (Stage.StageY.IsAlarm || Stage.StageT.IsAlarm || Stage.ExpanderZ.IsAlarm || Stage.CameraX.IsAlarm)
                return Fail("IN-STAGE-ALARM", Stage.Name, "Input stage axis alarm exists.");

            _currentStep = nextStep;
            return 0;
        }

        private async Task<int> MoveLoadPositionAsync()
        {
            if (_options.EnableMotion)
            {
                int result = await Stage.LoadAndPrepareWaferAsync(_options.WaferId, _options.RequireMapData, _options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-LOAD-PREP", Stage.Name, "Input stage load prepare failed. result=" + result);
            }

            _context.Bus.Set("InputStageLoadPrepared");
            _currentStep = InputStageSequenceStep.Complete;
            return 0;
        }

        private async Task<int> MoveAlignPositionAsync()
        {
            if (_options.EnableMotion)
            {
                int result = await Stage.VisionAlignAndSetupOriginAsync(_options.RequireVisionAlign, _options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-ALIGN", Stage.Name, "Input stage vision align/setup failed. result=" + result);
            }

            _currentStep = InputStageSequenceStep.BuildDiePositions;
            return 0;
        }

        private int BuildDiePositions()
        {
            _context.Bus.Set("InputStageAligned");
            _context.Bus.Set("InputStageReady");
            _currentStep = InputStageSequenceStep.Complete;
            return 0;
        }

        private async Task<int> MoveUnloadPositionAsync()
        {
            if (_options.EnableMotion)
            {
                int result = await Stage.PrepareUnloadWaferAsync(_options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-UNLOAD-PREP", Stage.Name, "Input stage unload prepare failed. result=" + result);
            }

            _context.Bus.Set("InputStageUnloadPrepared");
            _currentStep = InputStageSequenceStep.Complete;
            return 0;
        }

        private async Task<int> MoveAvoidPositionAsync()
        {
            if (_options.EnableMotion)
            {
                int result = await MoveAxisAsync(QMC.CDT320.WaferStageAxis.WaferY, Stage.Recipe.WaferY.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveAxisAsync(QMC.CDT320.WaferStageAxis.VisionX, Stage.Recipe.VisionX.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;
            }

            _context.Bus.Set("InputStageAvoidReady");
            _currentStep = InputStageSequenceStep.Complete;
            return 0;
        }

        private async Task<int> MoveAxisAsync(QMC.CDT320.WaferStageAxis axis, double target)
        {
            int result = await Stage.MoveInputStageAxis(axis, target, _options.FineMove).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-STAGE-MOVE", Stage.Name, "Input stage axis move failed. axis=" + axis + ", target=" + target + ", result=" + result);

            return 0;
        }

        private InputStageSequenceStep ResolveStartStep(InputStageSequenceStep initialStep)
        {
            if (_options.StartMode != SequenceStartMode.Resume)
                return initialStep;

            string saved = SequenceResumeStore.ResolveStartStep(SequenceStateName, initialStep.ToString());
            InputStageSequenceStep step;
            return Enum.TryParse(saved, out step) ? step : initialStep;
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + _kind; }
        }

        private InputStageUnit Stage
        {
            get { return _context != null && _context.Machine != null ? _context.Machine.InputStageUnit : null; }
        }

        private int Fail(string alarmCode, string source, string message)
        {
            try
            {
                InputStageSequenceStep failedStep = _currentStep;
                _currentStep = InputStageSequenceStep.Error;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                _context.LogPublic("[INPUT-STAGE] FAIL " + alarmCode + " - " + message);
            }
            catch (Exception ex)
            {
                WriteLog(source, "Failure handling failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
