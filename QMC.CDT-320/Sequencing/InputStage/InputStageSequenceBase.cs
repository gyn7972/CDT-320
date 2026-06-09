using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    internal abstract class InputStageSequenceBase<TStep> where TStep : struct
    {
        private const string SequenceNamePrefix = "InputStageSequence";

        protected InputStageSequenceBase(
            MachineSequenceContext context,
            InputStageSequenceKind kind,
            string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected InputStageSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected InputStageSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected InputStageUnit Stage
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            try
            {
                Options = options ?? InputStageSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[INPUT-STAGE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    TStep executingStep = CurrentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                }

                Context.LogPublic("[INPUT-STAGE] " + Options.RunMode + " " + Kind + " complete");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Input stage " + Kind + " sequence completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Input stage " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-EX", Name, "Input stage " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        protected int CheckUnit(TStep nextStep)
        {
            if (Stage == null)
                return Fail("IN-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            if (Stage.StageY == null || Stage.StageT == null || Stage.ExpanderZ == null || Stage.CameraX == null)
                return Fail("IN-STAGE-AXIS", Stage.Name, "Input stage axis is not available.");

            if (Stage.StageY.IsAlarm || Stage.StageT.IsAlarm || Stage.ExpanderZ.IsAlarm || Stage.CameraX.IsAlarm)
                return Fail("IN-STAGE-ALARM", Stage.Name, "Input stage axis alarm exists.");

            CurrentStep = nextStep;
            return 0;
        }

        protected async Task<int> MoveLoadPositionAsync()
        {
            if (Options.EnableMotion)
            {
                int result = await Stage.LoadAndPrepareWaferAsync(Options.WaferId, Options.RequireMapData, Options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-LOAD-PREP", Stage.Name, "Input stage load prepare failed. result=" + result);
            }

            Context.Bus.Set("InputStageLoadPrepared");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> MoveAlignPositionAsync(TStep nextStep)
        {
            if (Options.EnableMotion)
            {
                int result = await Stage.VisionAlignAndSetupOriginAsync(Options.RequireVisionAlign, Options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-ALIGN", Stage.Name, "Input stage vision align/setup failed. result=" + result);
            }

            CurrentStep = nextStep;
            return 0;
        }

        protected int BuildDiePositions()
        {
            Context.Bus.Set("InputStageAligned");
            Context.Bus.Set("InputStageReady");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> MoveUnloadPositionAsync()
        {
            if (Options.EnableMotion)
            {
                int result = await Stage.PrepareUnloadWaferAsync(Options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-UNLOAD-PREP", Stage.Name, "Input stage unload prepare failed. result=" + result);
            }

            Context.Bus.Set("InputStageUnloadPrepared");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> MoveAvoidPositionAsync()
        {
            if (Options.EnableMotion)
            {
                int result = await MoveAxisCommandAsync(QMC.CDT320.WaferStageAxis.WaferY, Stage.Recipe.WaferY.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;

                result = await WaitAxisInPositionResultAsync(QMC.CDT320.WaferStageAxis.WaferY, Stage.Recipe.WaferY.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveAxisCommandAsync(QMC.CDT320.WaferStageAxis.VisionX, Stage.Recipe.VisionX.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;

                result = await WaitAxisInPositionResultAsync(QMC.CDT320.WaferStageAxis.VisionX, Stage.Recipe.VisionX.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;
            }

            Context.Bus.Set("InputStageAvoidReady");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected int FailUnsupportedStep()
        {
            return Fail("IN-STAGE-STEP", Name, "Unsupported input stage step: " + CurrentStep);
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                TStep failedStep = CurrentStep;
                CurrentStep = ErrorStep;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                Context.LogPublic("[INPUT-STAGE] FAIL " + alarmCode + " - " + message);
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

        private async Task<int> MoveAxisCommandAsync(QMC.CDT320.WaferStageAxis axis, double target)
        {
            int result = await Stage.MoveInputStageAxis(axis, target, Options.FineMove).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-STAGE-MOVE", Stage.Name, "Input stage axis move failed. axis=" + axis + ", target=" + target + ", result=" + result);

            return 0;
        }

        private async Task<int> WaitAxisInPositionResultAsync(QMC.CDT320.WaferStageAxis axis, double target)
        {
            bool arrived = await WaitAxisInPositionAsync(axis, target, ResolveTimeout()).ConfigureAwait(false);
            if (!arrived)
                return Fail("IN-STAGE-MOVE-TIMEOUT", Stage.Name, "Input stage axis move done timeout. axis=" + axis + ", target=" + target);

            return 0;
        }

        private async Task<bool> WaitAxisInPositionAsync(QMC.CDT320.WaferStageAxis axis, double target, int timeoutMs)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(axis);
            if (item == null)
                return false;

            DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs > 0 ? timeoutMs : 10000);
            while (DateTime.Now <= deadline)
            {
                if (!item.IsMoving && IsAxisInPosition(item, target))
                    return true;

                await Task.Delay(20).ConfigureAwait(false);
            }

            return !item.IsMoving && IsAxisInPosition(item, target);
        }

        private QMC.Common.Motion.BaseAxis ResolveStageAxis(QMC.CDT320.WaferStageAxis axis)
        {
            switch (axis)
            {
                case QMC.CDT320.WaferStageAxis.WaferY: return Stage.StageY;
                case QMC.CDT320.WaferStageAxis.WaferT: return Stage.StageT;
                case QMC.CDT320.WaferStageAxis.WaferExpandingZ: return Stage.ExpanderZ;
                case QMC.CDT320.WaferStageAxis.VisionX: return Stage.CameraX;
                case QMC.CDT320.WaferStageAxis.NeedleX: return Stage.NeedleBlockX;
                case QMC.CDT320.WaferStageAxis.NeedleZ: return Stage.NeedleZ;
                case QMC.CDT320.WaferStageAxis.EjectPinZ: return Stage.EjectPinZ;
                default: return null;
            }
        }

        private static bool IsAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
        }

        private TStep ResolveStartStep(TStep initialStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    WriteLog("ResolveStartStep", "Input stage " + Kind + " sequence forced restart from step=" + initialStep + ". - Ok");
                    return initialStep;
                }

                string saved = SequenceResumeStore.ResolveStartStep(SequenceStateName, initialStep.ToString());
                TStep step;
                if (Enum.TryParse(saved, out step) &&
                    !IsStep(step, IdleStep) &&
                    !IsStep(step, CompleteStep) &&
                    !IsStep(step, ErrorStep))
                {
                    WriteLog("ResolveStartStep", "Input stage " + Kind + " sequence resume step=" + step + ". - Ok");
                    return step;
                }

                return initialStep;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStartStep", "Input stage " + Kind + " sequence resume step resolve failed: " + ex.Message + " - Failed");
                return initialStep;
            }
            finally
            {
            }
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + Kind; }
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

        private static bool IsStep(TStep left, TStep right)
        {
            return object.Equals(left, right);
        }

        protected static void WriteLog(string source, string message)
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
