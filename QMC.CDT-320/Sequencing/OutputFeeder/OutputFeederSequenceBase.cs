using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    internal abstract class OutputFeederSequenceBase<TStep> where TStep : struct
    {
        private const string SequenceNamePrefix = "OutputFeederSequence";

        protected OutputFeederSequenceBase(MachineSequenceContext context, OutputFeederSequenceKind kind, string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected OutputFeederSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected OutputFeederSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected OutputFeederUnit Feeder
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputFeederUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            try
            {
                Options = options ?? OutputFeederSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[OUTPUT-FEEDER] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    TStep executingStep = CurrentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                }

                Context.LogPublic("[OUTPUT-FEEDER] " + Options.RunMode + " " + Kind + " complete");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Output feeder " + Kind + " sequence completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Output feeder " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-EX", Name, "Output feeder " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        protected int CheckUnit(TStep nextStep)
        {
            if (Feeder == null)
                return Fail("OUT-FEEDER-MISSING", "OutputFeeder", "Output feeder unit is not available.");

            if (!Feeder.CheckFeederOverloadClear())
                return Fail("OUT-FEEDER-OVERLOAD", Feeder.Name, "Output feeder overload is on.");

            CurrentStep = nextStep;
            return 0;
        }

        protected async Task<int> MoveFeederYCommandAsync(Task<int> moveTask, string description, CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(moveTask, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-Y-MOVE", Feeder.Name, description + " move command failed. result=" + result);

            return 0;
        }

        protected async Task<int> WaitFeederYDoneAsync(Func<bool> inPosition, string description, CancellationToken ct)
        {
            bool done = await AwaitStepWithCancellationAsync(Feeder.WaitBinFeederYMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            if (!done || (inPosition != null && !inPosition()))
                return Fail("OUT-FEEDER-Y-TIMEOUT", Feeder.Name, description + " move done timeout.");

            return 0;
        }

        protected bool IsHardwareBypass()
        {
            AppSettings settings = AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (Context.Controller != null && Context.Controller.GlobalDryRun) ||
                   (Feeder.Setup != null && Feeder.Setup.IsSimulationMode) ||
                   (Feeder.Config != null && Feeder.Config.bDryRun);
        }

        protected CassetteMaterialRole ResolveOutputCassetteRole()
        {
            if (Options.CassetteRole == CassetteMaterialRole.Good1 ||
                Options.CassetteRole == CassetteMaterialRole.Good2 ||
                Options.CassetteRole == CassetteMaterialRole.Ng1)
                return Options.CassetteRole;

            return Options.Side == BinSide.Ng ? CassetteMaterialRole.Ng1 : CassetteMaterialRole.Good1;
        }

        protected MaterialLocationKind ResolveOutputStageLocation()
        {
            return Options.Side == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood;
        }

        protected int FailUnsupportedStep()
        {
            return Fail("OUT-FEEDER-STEP", Name, "Unsupported output feeder step: " + CurrentStep);
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
                Context.LogPublic("[OUTPUT-FEEDER] FAIL " + alarmCode + " - " + message);
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

        private TStep ResolveStartStep(TStep initialStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    return initialStep;
                }

                string saved = SequenceResumeStore.ResolveStartStep(SequenceStateName, initialStep.ToString());
                TStep step;
                if (Enum.TryParse(saved, out step) &&
                    !IsStep(step, IdleStep) &&
                    !IsStep(step, CompleteStep) &&
                    !IsStep(step, ErrorStep))
                    return step;

                return initialStep;
            }
            catch
            {
                return initialStep;
            }
            finally
            {
            }
        }

        protected int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + Kind; }
        }

        protected static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
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

        protected static async Task<bool> AwaitStepWithCancellationAsync(Task<bool> stepTask, CancellationToken ct)
        {
            if (stepTask == null)
                return false;
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
            try { Log.Write("Main", "SYSTEM", source, message); } catch { }
        }
    }
}
