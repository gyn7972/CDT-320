using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT320.Sequencing
{
    internal abstract class InputCassetteSequenceBase<TStep> where TStep : struct
    {
        private const string SequenceNamePrefix = "InputCassetteSequence";

        protected InputCassetteSequenceBase(
            MachineSequenceContext context,
            InputCassetteSequenceKind kind,
            string name)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected InputCassetteSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected InputCassetteSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected InputCassetteUnit Cassette
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null; }
        }

        protected InputFeederUnit Feeder
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputFeederUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            Options = options ?? InputCassetteSequenceOptions.Default();
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.InputSeq, Name, () => CurrentStep.ToString(), Name, Options.RunMode.ToString()))
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options.RunMode == SequenceRunMode.Auto,
                GetType().Name + ":" + Name + ":" + Options.RunMode))
            try
            {
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceTrace.RunStart(Name, Options.RunMode.ToString(), "kind=" + Kind);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[INPUT-CASSETTE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    TStep executingStep = CurrentStep;
                    SequenceTrace.StepStart(Name, executingStep.ToString(), "kind=" + Kind);
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                    {
                        SequenceTrace.StepFail(Name, executingStep.ToString(), result, "kind=" + Kind, "next=" + CurrentStep);
                        SequenceTrace.RunEnd(Name, "Failed", result, "kind=" + Kind, "step=" + executingStep);
                        return result;
                    }

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                    SequenceTrace.StepEnd(Name, executingStep.ToString(), result, "kind=" + Kind, "next=" + CurrentStep);
                }

                Context.LogPublic("[INPUT-CASSETTE] " + Options.RunMode + " " + Kind + " complete");
                WriteLog("RunAsync", "Input cassette " + Kind + " sequence completed. - Ok");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                SequenceTrace.RunEnd(Name, "Completed", 0, "kind=" + Kind);
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Input cassette " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                SequenceTrace.RunEnd(Name, "Canceled", -1, "kind=" + Kind, "step=" + CurrentStep);
                throw;
            }
            catch (Exception ex)
            {
                int failResult = Fail("IN-CST-EXCEPTION", Name, "Input cassette " + Kind + " sequence exception at step=" + CurrentStep + ": " + ex.Message);
                SequenceTrace.RunEnd(Name, "Failed", failResult, "kind=" + Kind, "step=" + CurrentStep, "error=" + ex.Message);
                return failResult;
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        protected int CheckLot(TStep nextStep)
        {
            try
            {
                if (Options.RequireActiveLot && LotStorage.ActiveLot == null)
                    return Fail("IN-CST-NO-LOT", Name, "Active lot is required for cassette mapping.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-LOT-EX", Name, "Lot check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteDetected(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                bool detected = cassette.IsWaferCassetteExist(ResolveCassetteSize(cassette));
                if (!IsHardwareBypassed() && !detected)
                    return Fail("IN-CST-MISSING", cassette.Name, "Input cassette is not detected.");
                if (IsHardwareBypassed() && !detected)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: cassette detect sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-DETECT-EX", Name, "Cassette detect check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteSize(TStep nextStep, bool allowMismatch)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                bool matched = IsCassetteSizeMatched(cassette);
                if (!IsHardwareBypassed() && !matched)
                {
                    if (!allowMismatch)
                        return Fail("IN-CST-SIZE", cassette.Name, "Input cassette size does not match recipe/config.");

                    Context.LogPublic("[INPUT-CASSETTE] Unloading continues although cassette size does not match recipe/config.");
                }

                if (IsHardwareBypassed() && !matched)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: cassette size sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-SIZE-EX", Name, "Cassette size check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteMaterial(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                var material = cassette.GetWaferMaterialCassette();
                if (material == null)
                    return Fail("IN-CST-MATERIAL", cassette.Name, "Input cassette material information is missing.");
                int slotCount = cassette.Config != null ? cassette.Config.SlotCount : 0;
                if (slotCount <= 0 || material.Slots == null || material.Slots.Count != slotCount)
                    return Fail("IN-CST-MATERIAL-SLOT", cassette.Name, "Input cassette material slot information does not match cassette config.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-MATERIAL-EX", Name, "Cassette material check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckMappingStartCondition(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                var feeder = Feeder;
                string feederOccupiedReason;
                if (IsFeederOccupiedBeforeMapping(feeder, out feederOccupiedReason))
                    return Fail("IN-CST-MAP-FEEDER-OCCUPIED",
                        feeder != null ? feeder.Name : "InputFeeder",
                        "InputFeeder에 제품이 있어 카세트 매핑을 시작할 수 없습니다. 제품 배출/정리 후 매핑을 다시 실행하세요. " + feederOccupiedReason);

                WaferMaterial activeWafer = MaterialStateService.State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    (w.SourceCassetteRole == CassetteMaterialRole.Input1 || w.SourceCassetteRole == CassetteMaterialRole.Input2) &&
                    WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                    (w.CurrentLocation == null ||
                     w.CurrentLocation.Kind != MaterialLocationKind.InputCassette ||
                     w.CurrentLocation.CassetteRole != w.SourceCassetteRole ||
                     w.CurrentLocation.SlotNumber != w.SourceSlotNumber));
                if (activeWafer != null)
                {
                    return Fail("IN-CST-MAP-MATERIAL-ACTIVE", "Material",
                        "공정 중 Input wafer가 cassette 밖에 있어 재매핑을 시작할 수 없습니다. wafer=" + activeWafer.WaferId +
                        ", sourceRole=" + activeWafer.SourceCassetteRole +
                        ", sourceSlot=" + (activeWafer.SourceSlotNumber + 1).ToString("00") +
                        ", state=" + activeWafer.State +
                        ", location=" + activeWafer.CurrentLocation);
                }

                string readyReason;
                bool ready = cassette.CheckWaferCassetteMappingReady(out readyReason);
                if (!IsHardwareBypassed() && !ready)
                    return Fail("IN-CST-MAP-READY", cassette.Name, "Input cassette is not ready for mapping. " + readyReason);
                if (IsHardwareBypassed() && !ready)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: mapping ready sensor check skipped. " + readyReason);
                if (!HasProcessWaferIfMapped(cassette))
                    Context.LogPublic("[INPUT-CASSETTE] No unprocessed wafer is currently registered. Mapping will refresh wafer information.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-MAP-READY-EX", Name, "Mapping start condition check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckFeederPosition(TStep nextStep)
        {
            try
            {
                var feeder = Feeder;
                bool ready = IsFeederAllowedForCassetteMove(feeder);
                if (!IsHardwareBypassed() && !ready)
                    return Fail("IN-CST-FEEDER-POS", feeder != null ? feeder.Name : "InputFeeder", "Feeder must be in Avoid or Exchange position.");
                if (IsHardwareBypassed() && !ready)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: feeder position sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-FEEDER-EX", Name, "Feeder position check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveLoadingPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                string readyReason;
                if (!cassette.CheckWaferCassetteMoveReady(out readyReason))
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move. " + readyReason);

                double target = cassette.Recipe.LoaingPosition;
                int result = await cassette.MoveWaferLifterZ(target, Options.FineMove, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-CST-LOAD-POS", cassette.Name,
                        "Move loading position failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                CurrentStep = CompleteStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-LOAD-EX", Name, "Move loading position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveUnloadingPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                string readyReason;
                if (!cassette.CheckWaferCassetteMoveReady(out readyReason))
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move. " + readyReason);

                double target = cassette.Recipe.UnloadingPosition;
                int result = await cassette.MoveWaferLifterZ(target, Options.FineMove, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-CST-UNLOAD-POS", cassette.Name,
                        "Move unloading position failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                CurrentStep = CompleteStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-UNLOAD-EX", Name, "Move unloading position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveMappingStartPositionAsync(TStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                string readyReason;
                if (!cassette.CheckWaferCassetteMoveReady(out readyReason))
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move. " + readyReason);

                // To do: 스캔 시작은 MappingStart(밑 슬롯 검출 앵커)보다 반 피치 아래에서 출발한다(시작 시 센서 ON 방지).
                double target = cassette.ResolveMappingScanStartPosition();
                int result = await cassette.MoveWaferLifterZ(target, Options.FineMove, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-CST-MAP-START", cassette.Name,
                        "Move mapping start position failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-MAP-START-EX", Name, "Move mapping start position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveMappingEndPositionAsync(TStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                double target = cassette.Recipe.MappingEndPosition;
                int result = await cassette.MoveWaferLifterZ(target, Options.FineMove, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-CST-MAP-END", cassette.Name,
                        "Move mapping end position failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-MAP-END-EX", Name, "Move mapping end position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> ScanSlotsAsync(TStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                if (IsHardwareBypassed())
                {
                    cassette.BuildSimulatedWaferMap();
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: simulated wafer map generated.");
                }
                else
                {
                    int result = await cassette.WaferScanFromCurrentStart(ResolveMoveTimeout(cassette), Options.FineMove, ct).ConfigureAwait(false);
                    if (result != 0) return Fail("IN-CST-SCAN", cassette.Name, "Wafer scan failed. result=" + result);
                }

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-SCAN-EX", Name, "Wafer scan exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int BuildWaferInfo(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                int result = RegisterMappingResult(cassette);
                if (result != 0)
                    return Fail("IN-CST-BUILD-WAFER", cassette != null ? cassette.Name : "InputCassette", "Input cassette material mapping result registration failed. result=" + result);

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-BUILD-WAFER-EX", Name, "Build wafer information exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveFirstWaferSlotAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                int slotCount = cassette.Config != null ? cassette.Config.SlotCount : 0;
                if (slotCount > 0)
                {
                    // To do: [맵핑 재설계] 첫 제품 슬롯 = 전체 맨 위(flat 0 = 최상위 레벨 01번). 제품은 맨 위에서 아래로 진행.
                    double target = cassette.CalculateWaferCassetteSlotTargetPosition(0, cassette.ResolveCassetteLevelCount());
                    int result = await cassette.MoveWaferLifterZ(target, Options.FineMove, ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("IN-CST-FIRST-SLOT", cassette.Name,
                            "Move slot 1 failed. result=" + result + ". " + BuildCassetteZState(cassette, target));
                }

                //EventLogger.Write(EventKind.InputSeq, "UI", "INPUT-STAGE", title + " 시퀀스 중단: " + message);
                CurrentStep = CompleteStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-FIRST-SLOT-EX", Name, "Move slot 1 exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int FailUnsupportedStep()
        {
            return Fail("IN-CST-STEP", Name, "Unsupported cassette sequence step: " + CurrentStep);
        }

        private string BuildCassetteZState(InputCassetteUnit cassette, double target)
        {
            if (cassette == null || cassette.InputLifterZ == null)
                return "CassetteZ=null, target=" + target;

            double tolerance = cassette.ResolveWaferLifterZInPositionTolerance();
            return "CassetteZ name=" + cassette.InputLifterZ.Name +
                   ", servo=" + cassette.InputLifterZ.IsServoOn +
                   ", alarm=" + cassette.InputLifterZ.IsAlarm +
                   ", alarmCode=" + cassette.InputLifterZ.AlarmCode +
                   ", moving=" + cassette.InputLifterZ.IsMoving +
                   ", actual=" + cassette.InputLifterZ.ActualPosition +
                   ", command=" + cassette.InputLifterZ.CommandPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance +
                   ", inPosition=" + cassette.IsWaferLifterZInPosition(target, tolerance);
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    WriteLog(source, message + " - Stopped");
                    Context.LogPublic("[INPUT-CASSETTE] STOP " + message);
                    throw new SequenceStopException(message);
                }

                TStep failedStep = CurrentStep;
                CurrentStep = ErrorStep;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                SequenceFailureStore.Record(SequenceStateName, Kind.ToString(), failedStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                Context.LogPublic("[INPUT-CASSETTE] FAIL " + alarmCode + " - " + message);
            }
            catch (SequenceStopException)
            {
                throw;
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

        private TStep ResolveStartStep(TStep defaultStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    WriteLog("ResolveStartStep", "Input cassette " + Kind + " sequence forced restart from step=" + defaultStep + ". - Ok");
                    return defaultStep;
                }

                string stepText = SequenceResumeStore.ResolveStartStep(SequenceStateName, defaultStep.ToString());
                TStep parsed;
                if (Enum.TryParse(stepText, out parsed) &&
                    !IsStep(parsed, IdleStep) &&
                    !IsStep(parsed, CompleteStep) &&
                    !IsStep(parsed, ErrorStep))
                {
                    WriteLog("ResolveStartStep", "Input cassette " + Kind + " sequence resume step=" + parsed + ". - Ok");
                    return parsed;
                }

                return defaultStep;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStartStep", "Input cassette " + Kind + " sequence resume step resolve failed: " + ex.Message + " - Failed");
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

        private bool IsFeederAllowedForCassetteMove(InputFeederUnit feeder)
        {
            if (feeder == null) return false;
            return feeder.IsWaferFeederInAvoidPosition() || feeder.IsWaferFeederInExchangePosition();
        }

        private bool IsFeederOccupiedBeforeMapping(InputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (feeder == null)
                {
                    reason = "InputFeeder=null";
                    return false;
                }

                bool dataOccupied = feeder.IsWaferFeederTransferDataOccupied();
                bool waferOccupied = feeder.HasWaferOnFeeder();
                bool rawSensorDetected = false; //실제 웨이퍼를 집기전에 이미 스테이지에 감지되어있다. 추후다시사용 //IsAnyWaferFeederRawRingSensorOn(feeder);
                bool occupied = dataOccupied || waferOccupied || rawSensorDetected;

                reason = "DataOccupied=" + dataOccupied +
                         ", WaferOccupied=" + waferOccupied +
                         ", RawSensorDetected=" + rawSensorDetected +
                         ". " + feeder.GetWaferFeederTransferState();

                return occupied;
            }
            catch (Exception ex)
            {
                reason = "InputFeeder 제품 보유 상태 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return true;
            }
            finally
            {
            }
        }

        private bool IsAnyWaferFeederRawRingSensorOn(InputFeederUnit feeder)
        {
            try
            {
                if (feeder == null)
                    return false;

                return IsRawInputOn(feeder.WaferFeederRingCheckSensor) ||
                       IsRawInputOn(feeder.WaferFeeder8RingCheckSensor) ||
                       IsRawInputOn(feeder.WaferFeeder12RingCheckSensor);
            }
            catch (Exception ex)
            {
                WriteLog("InputCassetteFeederCheck",
                    "InputFeeder raw ring sensor 확인 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool IsRawInputOn(BaseDigitalInput input)
        {
            try
            {
                return input != null && input.IsOn;
            }
            catch (Exception ex)
            {
                WriteLog("InputCassetteFeederCheck",
                    "InputFeeder raw input 확인 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private int ResolveCassetteSize(InputCassetteUnit cassette)
        {
            if (Options.RequiredCassetteSize == 8 || Options.RequiredCassetteSize == 12)
                return Options.RequiredCassetteSize;
            return MaterialStateService.ResolveWaferSizeInch(cassette.Config.InchSelect);
        }

        private bool IsCassetteSizeMatched(InputCassetteUnit cassette)
        {
            int size = ResolveCassetteSize(cassette);
            return cassette.IsWaferCassetteExist(size);
        }

        private bool IsHardwareBypassed()
        {
            var settings = AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (Context.Controller != null && Context.Controller.GlobalDryRun) ||
                   (Cassette != null && Cassette.Setup != null && Cassette.Setup.IsSimulationMode) ||
                   (Cassette != null && Cassette.Config != null && Cassette.Config.bDryRun);
        }

        private int ResolveMoveTimeout(InputCassetteUnit cassette)
        {
            if (Options.MoveTimeoutMs > 0)
                return Options.MoveTimeoutMs;

            int configured = cassette != null ? cassette.ResolveWaferLifterZMoveTimeoutMs() : 0;
            return configured > 0 ? configured : 3000;
        }

        private bool HasProcessWaferIfMapped(InputCassetteUnit cassette)
        {
            return cassette.WaferMap == null || cassette.WaferMap.Count == 0 || cassette.HasMoreProcessWafer();
        }

        private int RegisterMappingResult(InputCassetteUnit cassette)
        {
            try
            {
                if (cassette == null)
                {
                    WriteLog("RegisterMappingResult", "Input cassette unit is not available. - Failed");
                    return -1;
                }

                if (cassette.WaferMap == null)
                {
                    WriteLog("RegisterMappingResult", "Input cassette wafer map is not available. - Failed");
                    cassette.RollbackWaferMapping();
                    return -1;
                }

                int slotCount = cassette.Config != null ? cassette.Config.SlotCount : 0;
                int levelCount = ResolveInputCassetteLevelCount(cassette);
                int expectedMapCount = slotCount * levelCount;
                if (slotCount <= 0 || cassette.WaferMap.Count != expectedMapCount)
                {
                    WriteLog("RegisterMappingResult",
                        "Input cassette mapping result length does not match SlotCount/level count. slotCount=" + slotCount +
                        ", levelCount=" + levelCount +
                        ", expected=" + expectedMapCount +
                        ", actual=" + cassette.WaferMap.Count + " - Failed");
                    cassette.RollbackWaferMapping();
                    return -1;
                }

                var arr = new bool[cassette.WaferMap.Count];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = cassette.WaferMap[i];

                IReadOnlyList<bool> level1Map = BuildCassetteLevelMap(cassette.WaferMap, slotCount, 1);
                IReadOnlyList<bool> level2Map = BuildCassetteLevelMap(cassette.WaferMap, slotCount, 2);

                SlotMapperRegistry.Update("InputCassette", arr);
                int inchSelect = cassette.Config != null ? cassette.Config.InchSelect : 0;
                MaterialStateService.UpdateInputCassetteMapping(
                    levelCount,
                    slotCount,
                    level1Map,
                    level2Map,
                    BuildCassetteLevelSlotPositions(cassette, 1),
                    BuildCassetteLevelSlotPositions(cassette, 2),
                    LotStorage.ActiveLot != null ? LotStorage.ActiveLot.LotID : "",
                    MaterialStateService.ResolveInputTapeFrameSpecName(inchSelect));
                cassette.ApplyRegisteredWaferMappingState();
                cassette.CommitWaferMapping();
                Context.Controller.ApplyInputCassetteMappingCompleted();
                WriteLog("RegisterMappingResult", "Input cassette material mapping result registered. - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                if (cassette != null)
                    cassette.RollbackWaferMapping();
                WriteLog("RegisterMappingResult", "Input cassette material mapping result registration exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        // To do: [맵핑 재설계] WaferMap flat 배치 = 앞쪽(0~N-1)이 2단(위 카세트), 뒤쪽(N~2N-1)이 1단.
        //        2단 미사용(map 길이 == slotCount)이면 전체가 1단이다. local 0 = 각 레벨 맨 위 슬롯.
        private static IReadOnlyList<bool> BuildCassetteLevelMap(IReadOnlyList<bool> map, int slotCount, int level)
        {
            if (slotCount < 0)
                slotCount = 0;

            var levelMap = new bool[slotCount];
            if (map == null || slotCount == 0)
                return levelMap;

            bool hasLevel2 = map.Count > slotCount;
            int offset;
            if (!hasLevel2)
                offset = level == 1 ? 0 : -1;              // 1단 전용: level2 요청이면 빈 맵
            else
                offset = level >= 2 ? 0 : slotCount;       // 2단=앞쪽, 1단=뒤쪽

            if (offset < 0)
                return levelMap;

            for (int i = 0; i < slotCount; i++)
            {
                int sourceIndex = offset + i;
                levelMap[i] = sourceIndex >= 0 && sourceIndex < map.Count && map[sourceIndex];
            }

            return levelMap;
        }

        private static double[] BuildCassetteLevelSlotPositions(InputCassetteUnit cassette, int level)
        {
            try
            {
                int count = cassette != null && cassette.Config != null ? cassette.Config.SlotCount : 0;
                if (count < 0)
                    count = 0;

                var positions = new double[count];
                for (int i = 0; i < positions.Length; i++)
                    // To do: 웨이퍼 카세트 포지션은 "실측 검출 위치 + 로딩 오프셋"으로 저장한다.
                    //        CalculateWaferCassetteSlotTargetPosition은 실측(Recipe.SlotPosition[flat])을 우선 사용하고
                    //        미맵핑 슬롯만 명목값으로 대체하므로, 실제 축 이동 목표와 항상 일치한다.
                    positions[i] = cassette.CalculateWaferCassetteSlotTargetPosition(i, level);

                return positions;
            }
            catch (Exception ex)
            {
                WriteLog("BuildCassetteLevelSlotPositions", "Cassette level slot positions build failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static int ResolveInputCassetteLevelCount(InputCassetteUnit cassette)
        {
            int configured = cassette != null && cassette.Config != null ? cassette.Config.SelectedCassetteLevel : 1;
            return configured >= 2 ? 2 : 1;
        }

        private static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitIntAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private static async Task<AxisMoveWaitResult> AwaitStepWithCancellationAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
        {
            try
            {
                AxisMoveWaitResult defaultValue =
                    new AxisMoveWaitResult(AxisMoveWaitFailure.AxisMissing, "Step task is null.", string.Empty);
                return await SequenceAwaiter.AwaitAsync(stepTask, defaultValue, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
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

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.InputSeq, source, message);
        }
    }
}
