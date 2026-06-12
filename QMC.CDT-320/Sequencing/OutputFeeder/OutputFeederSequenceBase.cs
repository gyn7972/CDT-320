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

        protected OutputStageUnit Stage
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null; }
        }

        protected PickerFrontUnit FrontPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerFrontUnit : null; }
        }

        protected PickerRearUnit RearPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerRearUnit : null; }
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

            string readyReason;
            if (!Feeder.CheckBinFeederYMoveReady(out readyReason))
                return Fail("OUT-FEEDER-MOVE-READY", Feeder.Name, "Output feeder is not ready. " + readyReason);

            CurrentStep = nextStep;
            return 0;
        }

        protected int CheckCassetteSlotReadyForLoad(TStep nextStep)
        {
            WaferMaterial wafer = ResolveCassetteWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-CST-DATA", "Material",
                    "Output cassette source slot data was not found. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", side=" + Options.Side);

            WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
            if (state != WaferMaterialState.Ready && state != WaferMaterialState.WorkReady)
                return Fail("OUT-FEEDER-CST-STATE", "Material", "Output cassette source slot is not ready. waferId=" + wafer.WaferId + ", state=" + state);

            WaferMaterial feederWafer = ResolveFeederWafer();
            if (feederWafer != null)
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material",
                    "Output feeder data must be empty before cassette load. waferId=" + feederWafer.WaferId +
                    ", state=" + feederWafer.State + ", loc=" + feederWafer.CurrentLocation);

            if (!IsHardwareBypass() && !Feeder.IsFeederEmpty())
                return Fail("OUT-FEEDER-SENSOR-OCCUPIED", Feeder.Name,
                    "Output feeder sensor must be empty before cassette load. sensorOccupied=" + Feeder.IsFeederOccupied() +
                    ", sensorEmpty=" + Feeder.IsFeederEmpty());

            CurrentStep = nextStep;
            return 0;
        }

        protected int CheckFeederReadyForStageLoad(TStep nextStep)
        {
            WaferMaterial feederWafer = ResolveFeederWafer();
            if (feederWafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material",
                    "Output feeder data was not found before stage load. side=" + Options.Side +
                    ", location=OutputFeeder empty.");

            WaferMaterial stageWafer = ResolveStageWafer();
            if (stageWafer != null)
                return Fail("OUT-STAGE-DATA-OCCUPIED", "Material",
                    "Output " + Options.Side + " stage must be empty before feeder to stage load. side=" + Options.Side +
                    ", waferId=" + stageWafer.WaferId + ", state=" + stageWafer.State +
                    ", loc=" + stageWafer.CurrentLocation);

            if (!IsHardwareBypass() && !Feeder.IsFeederOccupied())
                return Fail("OUT-FEEDER-SENSOR-EMPTY", Feeder.Name,
                    "Output feeder sensor/data mismatch before stage load. waferId=" + feederWafer.WaferId +
                    ", sensorOccupied=" + Feeder.IsFeederOccupied() + ", sensorEmpty=" + Feeder.IsFeederEmpty());

            CurrentStep = nextStep;
            return 0;
        }

        protected int CheckStageReadyForFeederUnload(TStep nextStep)
        {
            WaferMaterial stageWafer = ResolveStageWafer();
            if (stageWafer == null)
                return Fail("OUT-STAGE-DATA-MISSING", "Material",
                    "Output " + Options.Side + " stage data was not found before stage unload. location=" +
                    ResolveOutputStageLocation());

            WaferMaterial feederWafer = ResolveFeederWafer();
            if (feederWafer != null)
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material",
                    "Output feeder data must be empty before stage unload. waferId=" + feederWafer.WaferId +
                    ", state=" + feederWafer.State + ", loc=" + feederWafer.CurrentLocation);

            if (!IsHardwareBypass() && !Feeder.IsFeederEmpty())
                return Fail("OUT-FEEDER-SENSOR-OCCUPIED", Feeder.Name,
                    "Output feeder sensor must be empty before stage unload. sensorOccupied=" + Feeder.IsFeederOccupied() +
                    ", sensorEmpty=" + Feeder.IsFeederEmpty());

            CurrentStep = nextStep;
            return 0;
        }

        protected int CheckCassetteSlotReadyForUnload(TStep nextStep)
        {
            WaferMaterial feederWafer = ResolveFeederWafer();
            if (feederWafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material",
                    "Output feeder data was not found before cassette unload. location=OutputFeeder empty.");

            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (cassetteWafer != null && WaferMaterialStateText.Normalize(cassetteWafer.State) != WaferMaterialState.Empty)
                return Fail("OUT-FEEDER-CST-SLOT-OCCUPIED", "Material",
                    "Output cassette target slot must be empty before unload. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", waferId=" + cassetteWafer.WaferId +
                    ", state=" + cassetteWafer.State + ", loc=" + cassetteWafer.CurrentLocation);

            if (!IsHardwareBypass() && !Feeder.IsFeederOccupied())
                return Fail("OUT-FEEDER-SENSOR-EMPTY", Feeder.Name,
                    "Output feeder sensor/data mismatch before cassette unload. waferId=" + feederWafer.WaferId +
                    ", sensorOccupied=" + Feeder.IsFeederOccupied() + ", sensorEmpty=" + Feeder.IsFeederEmpty());

            CurrentStep = nextStep;
            return 0;
        }

        protected async Task<int> MoveFeederYCommandAsync(Task<int> moveTask, string description, CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(moveTask, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-Y-MOVE", Feeder.Name,
                    description + " move command failed. result=" + result + ", " +
                    (Feeder != null ? Feeder.DescribeBinFeederYMoveDoneState() : "Feeder=null"));

            return 0;
        }

        protected async Task<int> WaitFeederYDoneAsync(Func<bool> inPosition, string description, CancellationToken ct)
        {
            bool done = await AwaitStepWithCancellationAsync(Feeder.WaitBinFeederYMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            bool finalInPosition = inPosition == null || inPosition();

            if (!done || !finalInPosition)
            {
                string state = Feeder != null ? Feeder.DescribeBinFeederYMoveDoneState() : "Feeder=null";
                return Fail("OUT-FEEDER-Y-TIMEOUT", Feeder.Name,
                    description + " move done timeout. done=" + done +
                    ", finalInPosition=" + finalInPosition +
                    ", " + state);
            }

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

        protected WaferMaterial ResolveCassetteWafer()
        {
            return MaterialStateService.GetWaferInCassette(ResolveOutputCassetteRole(), Options.SlotIndex);
        }

        protected WaferMaterial ResolveFeederWafer()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
        }

        protected WaferMaterial ResolveStageWafer()
        {
            return MaterialStateService.GetWaferAtLocation(ResolveOutputStageLocation());
        }

        protected int VerifyFeederRingSensor(bool expected, string alarmCode, string description)
        {
            if (IsHardwareBypass())
                return 0;

            if (Feeder.IsFeederRingDetected(expected))
                return 0;

            return Fail(alarmCode, Feeder.Name, description + " sensor=" + (Feeder.IsFeederRingDetected(true) ? "ON" : "OFF") + ", expected=" + (expected ? "ON" : "OFF"));
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
                SequenceFailureStore.Record(SequenceStateName, Kind.ToString(), failedStep.ToString(), alarmCode, source, message);
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

        protected static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs, CancellationToken ct)
        {
            DateTime until = DateTime.UtcNow.AddMilliseconds(timeoutMs > 0 ? timeoutMs : 1);
            while (DateTime.UtcNow < until)
            {
                ct.ThrowIfCancellationRequested();
                if (condition != null && condition())
                    return true;

                await Task.Delay(20, ct).ConfigureAwait(false);
            }

            return condition != null && condition();
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
