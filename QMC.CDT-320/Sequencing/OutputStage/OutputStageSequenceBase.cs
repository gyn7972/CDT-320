using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    internal abstract class OutputStageSequenceBase<TStep> where TStep : struct
    {
        private const string SequenceNamePrefix = "OutputStageSequence";

        protected OutputStageSequenceBase(MachineSequenceContext context, OutputStageSequenceKind kind, string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected OutputStageSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected OutputStageSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected OutputStageUnit Stage
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, OutputStageSequenceOptions options)
        {
            try
            {
                Options = options ?? OutputStageSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[OUTPUT-STAGE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);
                    TStep executingStep = CurrentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                }

                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Output stage " + Kind + " sequence completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Output stage " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-EX", Name, "Output stage " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        protected int CheckUnit(TStep nextStep)
        {
            try
            {
                var stage = Stage;
                if (stage == null)
                    return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available.");
                if (stage.GoodStage == null || stage.NgStage == null || stage.OutputCameraX == null)
                    return Fail("OUT-STAGE-AXIS-MISSING", stage.Name, "Output stage axis is not available.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-CHECK-EX", Name, "Output stage check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveSideAxesAsync(string positionName, TStep nextStep, CancellationToken ct)
        {
            try
            {
                var stage = Stage;
                if (stage == null)
                    return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available.");

                if (Options.Side == BinSide.Ng)
                {
                    int result = await MoveAxisAndVerifyAsync(BinStageAxis.NgBinY, ResolveTarget(BinStageAxis.NgBinY, positionName), positionName + " NG Y", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = nextStep;
                    return 0;
                }

                int move = await MoveAxisAndVerifyAsync(BinStageAxis.GoodBinY, ResolveTarget(BinStageAxis.GoodBinY, positionName), positionName + " Good Y", ct).ConfigureAwait(false);
                if (move != 0) return move;
                move = await MoveAxisAndVerifyAsync(BinStageAxis.GoodBinZ, ResolveTarget(BinStageAxis.GoodBinZ, positionName), positionName + " Good Z", ct).ConfigureAwait(false);
                if (move != 0) return move;

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-MOVE-EX", Name, positionName + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveAllAvoidAsync(TStep nextStep, CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(BinStageAxis.GoodBinZ, ResolveTarget(BinStageAxis.GoodBinZ, "Avoid"), "Good Z avoid", ct).ConfigureAwait(false);
                if (result != 0) return result;
                result = await MoveAxisAndVerifyAsync(BinStageAxis.GoodBinY, ResolveTarget(BinStageAxis.GoodBinY, "Avoid"), "Good Y avoid", ct).ConfigureAwait(false);
                if (result != 0) return result;
                result = await MoveAxisAndVerifyAsync(BinStageAxis.NgBinY, ResolveTarget(BinStageAxis.NgBinY, "Avoid"), "NG Y avoid", ct).ConfigureAwait(false);
                if (result != 0) return result;
                result = await MoveAxisAndVerifyAsync(BinStageAxis.VisionX, ResolveTarget(BinStageAxis.VisionX, "Avoid"), "VisionX avoid", ct).ConfigureAwait(false);
                if (result != 0) return result;

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-AVOID-EX", Name, "Move avoid exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveVisionProcessAsync(TStep nextStep, CancellationToken ct)
        {
            int result = await MoveAxisAndVerifyAsync(BinStageAxis.VisionX, ResolveTarget(BinStageAxis.VisionX, "Process"), "VisionX process", ct).ConfigureAwait(false);
            if (result != 0) return result;
            CurrentStep = nextStep;
            return 0;
        }

        protected int Complete()
        {
            CurrentStep = CompleteStep;
            return 0;
        }

        protected int FailUnsupportedStep()
        {
            return Fail("OUT-STAGE-STEP", Name, "Unsupported output stage step: " + CurrentStep);
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                TStep failedStep = CurrentStep;
                CurrentStep = ErrorStep;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                SequenceFailureStore.Record(SequenceStateName, Kind.ToString(), failedStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                Context.LogPublic("[OUTPUT-STAGE] FAIL " + alarmCode + " - " + message);
            }
            catch
            {
            }
            finally
            {
            }

            return -1;
        }

        protected async Task<int> MoveAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (Stage != null && !Stage.HasStageAxis(axis))
            {
                WriteLog(Name, description + " skipped because axis does not exist. axis=" + axis + " - Ok");
                return 0;
            }

            int result = await AwaitStepWithCancellationAsync(Stage.MoveStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-MOVE", Stage.Name, description + " move command failed. axis=" + axis + ", target=" + target + ", result=" + result);

            bool done = await AwaitStepWithCancellationAsync(Stage.WaitStageAxisMoveDone(axis, ResolveTimeout()), ct).ConfigureAwait(false);
            if (!done)
                return Fail("OUT-STAGE-MOVE-WAIT", Stage.Name, description + " move done timeout. axis=" + axis + ", target=" + target);

            if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                return Fail("OUT-STAGE-MOVE-CHECK", Stage.Name, description + " final position check failed. axis=" + axis + ", target=" + target);

            ct.ThrowIfCancellationRequested();
            return 0;
        }

        protected double ResolveTarget(BinStageAxis axis, string positionName)
        {
            if (axis == BinStageAxis.NgBinZ)
            {
                return 0.0;
            }

            return Stage.GetStageTeachingPosition(axis, positionName);
        }

        protected double ResolveTolerance(BinStageAxis axis)
        {
            switch (axis)
            {
                case BinStageAxis.NgBinY:
                    return Stage.NgStage.StageY.Config != null ? Stage.NgStage.StageY.Config.InPositionTolerance : 0.01;
                case BinStageAxis.NgBinZ:
                    return 0.01;
                case BinStageAxis.GoodBinY:
                    return Stage.GoodStage.StageY.Config != null ? Stage.GoodStage.StageY.Config.InPositionTolerance : 0.01;
                case BinStageAxis.GoodBinZ:
                    return Stage.GoodStage.StageZ.Config != null ? Stage.GoodStage.StageZ.Config.InPositionTolerance : 0.01;
                case BinStageAxis.VisionX:
                    return Stage.OutputCameraX.Config != null ? Stage.OutputCameraX.Config.InPositionTolerance : 0.01;
                default:
                    return 0.01;
            }
        }

        protected int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
        }

        private TStep ResolveStartStep(TStep defaultStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    return defaultStep;
                }

                string saved = SequenceResumeStore.ResolveStartStep(SequenceStateName, defaultStep.ToString());
                TStep parsed;
                if (Enum.TryParse(saved, out parsed) &&
                    !IsStep(parsed, IdleStep) &&
                    !IsStep(parsed, CompleteStep) &&
                    !IsStep(parsed, ErrorStep))
                    return parsed;

                return defaultStep;
            }
            catch
            {
                return defaultStep;
            }
            finally
            {
            }
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + Kind; }
        }

        protected static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
        {
            if (stepTask == null) return -1;
            if (stepTask.IsCompleted) return await stepTask.ConfigureAwait(false);
            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
            if (!ReferenceEquals(completed, stepTask)) ct.ThrowIfCancellationRequested();
            return await stepTask.ConfigureAwait(false);
        }

        protected static async Task<bool> AwaitStepWithCancellationAsync(Task<bool> stepTask, CancellationToken ct)
        {
            if (stepTask == null) return false;
            if (stepTask.IsCompleted) return await stepTask.ConfigureAwait(false);
            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
            if (!ReferenceEquals(completed, stepTask)) ct.ThrowIfCancellationRequested();
            return await stepTask.ConfigureAwait(false);
        }

        private static bool IsStep(TStep left, TStep right)
        {
            return object.Equals(left, right);
        }

        protected BinSide ResolveSideFromGrade()
        {
            if (Options != null && Options.Grade == DieGrade.Ng)
                return BinSide.Ng;

            return BinSide.Good;
        }

        protected BinStageAxis ResolveYAxis(BinSide side)
        {
            if (side == BinSide.Ng)
                return BinStageAxis.NgBinY;

            return BinStageAxis.GoodBinY;
        }

        protected BinStageAxis ResolveZAxis(BinSide side)
        {
            if (side == BinSide.Ng)
                return BinStageAxis.NgBinZ;

            return BinStageAxis.GoodBinZ;
        }

        protected bool HasSideZAxis(BinSide side)
        {
            if (Stage == null)
                return false;

            return Stage.HasStageAxis(ResolveZAxis(side));
        }

        protected bool SkipMissingSideZAxis(BinSide side, string description)
        {
            if (HasSideZAxis(side))
                return false;

            WriteLog(Name, description + " skipped because " + side + " stage has no Z axis. - Ok");
            return true;
        }

        protected double ResolveSideTarget(BinSide side, string positionName)
        {
            return ResolveTarget(ResolveYAxis(side), positionName);
        }

        protected double ResolveSideZTarget(BinSide side, string positionName)
        {
            return ResolveTarget(ResolveZAxis(side), positionName);
        }

        protected static void WriteLog(string source, string message)
        {
            try { Log.Write("Main", "SYSTEM", source, message); } catch { }
        }
    }
}
