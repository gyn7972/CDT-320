using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

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

        protected OutputCassetteUnit Cassette
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputCassetteUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            Options = options ?? OutputFeederSequenceOptions.Default();
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.OutputSeq, Name, () => CurrentStep.ToString(), Name, Options.RunMode.ToString()))
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options.RunMode == SequenceRunMode.Auto,
                GetType().Name + ":" + Name + ":" + Options.RunMode))
            using (SequenceResourceLease feederLease = await Context.Resources.AcquireAsync(
                SequenceResourceKind.OutputFeederArea,
                Name + ":" + Kind,
                ResolveTimeout(),
                ct).ConfigureAwait(false))
            try
            {
                SequenceTrace.RunStart(Name, Options.RunMode.ToString(), "kind=" + Kind);
                if (feederLease == null)
                {
                    SequenceTrace.RunEnd(Name, "Failed", -1, "kind=" + Kind, "reason=ResourceAcquireFailed");
                    return Fail("OUT-FEEDER-RESOURCE", Name, "OutputFeeder 영역을 점유할 수 없어 시퀀스를 시작할 수 없습니다. kind=" + Kind);
                }

                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[OUTPUT-FEEDER] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

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

                Context.LogPublic("[OUTPUT-FEEDER] " + Options.RunMode + " " + Kind + " complete");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Output feeder " + Kind + " sequence completed. - Ok");
                SequenceTrace.RunEnd(Name, "Completed", 0, "kind=" + Kind);
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Output feeder " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                SequenceTrace.RunEnd(Name, "Canceled", -1, "kind=" + Kind, "step=" + CurrentStep);
                throw;
            }
            catch (Exception ex)
            {
                int failResult = Fail("OUT-FEEDER-EX", Name, "Output feeder " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
                SequenceTrace.RunEnd(Name, "Failed", failResult, "kind=" + Kind, "step=" + CurrentStep, "error=" + ex.Message);
                return failResult;
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
            if (state != WaferMaterialState.Ready)
                return Fail("OUT-FEEDER-CST-STATE", "Material", "Output cassette source slot is not ready. waferId=" + wafer.WaferId + ", state=" + state);

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
            {
                return Fail("OUT-FEEDER-CST-WAFER-MISMATCH", "Material",
                    "선택 계획과 현재 cassette slot의 Bin ID가 다릅니다. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex +
                    ", displaySlot=" + (Options.SlotIndex + 1).ToString("00") +
                    ", expectedWafer=" + Options.ExpectedWaferId +
                    ", actualWafer=" + wafer.WaferId);
            }

            if (wafer.SourceCassetteRole != ResolveOutputCassetteRole() || wafer.SourceSlotNumber != Options.SlotIndex)
            {
                return Fail("OUT-FEEDER-CST-SOURCE-MISMATCH", "Material",
                    "Output Bin의 원본 cassette/slot 정보가 선택 slot과 다릅니다. wafer=" + wafer.WaferId +
                    ", sourceRole=" + wafer.SourceCassetteRole +
                    ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00") +
                    ", targetRole=" + ResolveOutputCassetteRole() +
                    ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));
            }

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

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, feederWafer.WaferId, StringComparison.OrdinalIgnoreCase))
            {
                return Fail("OUT-FEEDER-UNLOAD-WAFER-MISMATCH", "Material",
                    "언로딩 계획과 현재 OutputFeeder Bin ID가 다릅니다. expectedWafer=" + Options.ExpectedWaferId +
                    ", actualWafer=" + feederWafer.WaferId +
                    ", targetRole=" + ResolveOutputCassetteRole() +
                    ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));
            }

            if (feederWafer.SourceCassetteRole != ResolveOutputCassetteRole() || feederWafer.SourceSlotNumber != Options.SlotIndex)
            {
                return Fail("OUT-FEEDER-UNLOAD-SOURCE-MISMATCH", "Material",
                    "OutputFeeder Bin은 원본 cassette/slot으로만 복귀할 수 있습니다. wafer=" + feederWafer.WaferId +
                    ", sourceRole=" + feederWafer.SourceCassetteRole +
                    ", sourceSlot=" + (feederWafer.SourceSlotNumber + 1).ToString("00") +
                    ", targetRole=" + ResolveOutputCassetteRole() +
                    ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));
            }

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
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await AwaitStepWithCancellationAsync(moveTask, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-FEEDER-Y-MOVE", Feeder != null ? Feeder.Name : "OutputFeeder",
                        LocalizeFeederMoveDescription(description) + " 이동 명령이 실패했습니다. result=" + result + ", " +
                        (Feeder != null ? Feeder.DescribeBinFeederYMoveDoneState() + Feeder.DescribeBinFeederYLastMotionFailure() : "Feeder=null"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-Y-MOVE-EX", Feeder != null ? Feeder.Name : "OutputFeeder",
                    LocalizeFeederMoveDescription(description) + " 이동 명령 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        protected Task<int> WaitFeederYDoneAsync(Func<bool> inPosition, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool finalInPosition = inPosition == null || inPosition();
                bool overload = Feeder != null && Feeder.IsFeederOverload();

                if (!finalInPosition || overload)
                {
                    string state = Feeder != null ? Feeder.DescribeBinFeederYMoveDoneState() : "Feeder=null";
                    string alarmCode = overload ? "OUT-FEEDER-Y-OVERLOAD" : "OUT-FEEDER-Y-POSITION";
                    return Task.FromResult(Fail(alarmCode, Feeder != null ? Feeder.Name : "OutputFeeder",
                        LocalizeFeederMoveDescription(description) + " 최종 인계 조건 확인 실패. " +
                        "최종위치확인=" + finalInPosition +
                        ", overload=" + overload +
                        ", " + state));
                }

                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                return Task.FromCanceled<int>(ct);
            }
            catch (Exception ex)
            {
                string state = Feeder != null ? Feeder.DescribeBinFeederYMoveDoneState() : "Feeder=null";
                return Task.FromResult(Fail("OUT-FEEDER-Y-HANDOFF-EX", Feeder != null ? Feeder.Name : "OutputFeeder",
                    LocalizeFeederMoveDescription(description) + " 최종 인계 조건 확인 중 예외가 발생했습니다. error=" + ex.Message + ", " + state));
            }
            finally
            {
            }
        }

        protected static string ResolveAxisMoveWaitAlarmCode(string prefix, AxisMoveWaitResult waitResult)
        {
            return AxisMoveWaiter.ResolveAlarmCode(prefix, waitResult);
        }

        protected static string FormatAxisMoveWaitResult(AxisMoveWaitResult waitResult, string fallbackState)
        {
            return AxisMoveWaiter.FormatResult(waitResult, fallbackState);
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

        protected TargetCassette ResolveOutputTargetCassette()
        {
            CassetteMaterialRole role = ResolveOutputCassetteRole();
            if (role == CassetteMaterialRole.Ng1)
                return TargetCassette.Ng;
            if (role == CassetteMaterialRole.Good2)
                return TargetCassette.Good2;
            return TargetCassette.Good1;
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

        [Obsolete("새 코드에서는 CheckPickersClearForOutputTransport 또는 WaitPickersClearForOutputTransportAsync를 사용하세요.", true)]
        protected int CheckPickersNotInOutputZone(string description)
        {
            if (IsAnyFrontPickerInOutputZone())
                return Fail("OUT-FEEDER-FRONT-PICKER-OUTPUT-ZONE", FrontPicker != null ? FrontPicker.Name : "FrontPicker",
                    "FrontPicker가 Output zone에 있습니다. " + LocalizeFeederMoveDescription(description) + " 전에는 OutputFeeder가 Picker를 자동 이동하지 않습니다.");

            if (IsAnyRearPickerInOutputZone())
                return Fail("OUT-FEEDER-REAR-PICKER-OUTPUT-ZONE", RearPicker != null ? RearPicker.Name : "RearPicker",
                    "RearPicker가 Output zone에 있습니다. " + LocalizeFeederMoveDescription(description) + " 전에는 OutputFeeder가 Picker를 자동 이동하지 않습니다.");

            return 0;
        }

        [Obsolete("새 코드에서는 Output zone 기준 WaitPickersClearForOutputTransportAsync를 사용하세요.", true)]
        protected int CheckPickersInAvoidPosition(string description)
        {
            try
            {
                if (FrontPicker == null)
                    return Fail("OUT-FEEDER-PICKER-MISSING", "FrontPicker", description + " 전 FrontPicker 유닛을 확인할 수 없습니다.");

                if (RearPicker == null)
                    return Fail("OUT-FEEDER-PICKER-MISSING", "RearPicker", description + " 전 RearPicker 유닛을 확인할 수 없습니다.");

                if (!FrontPicker.IsFrontPickerInAvoidPosition())
                    return Fail("OUT-FEEDER-FRONT-PICKER-NOT-AVOID", FrontPicker.Name,
                        description + " 불가: FrontPicker가 Avoid 위치가 아닙니다. " + BuildPickerAvoidState());

                if (!RearPicker.IsRearPickerInAvoidPosition())
                    return Fail("OUT-FEEDER-REAR-PICKER-NOT-AVOID", RearPicker.Name,
                        description + " 불가: RearPicker가 Avoid 위치가 아닙니다. " + BuildPickerAvoidState());

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-PICKER-AVOID-CHECK-EX", Name,
                    description + " 전 Picker Avoid 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static string LocalizeFeederMoveDescription(string description)
        {
            switch (description)
            {
                case "cassette load":
                    return "카세트 로드 위치";
                case "cassette load avoid":
                    return "카세트 로드 회피 위치";
                case "cassette unload":
                    return "카세트 언로드 위치";
                case "cassette unload avoid":
                    return "카세트 언로드 회피 위치";
                case "stage load":
                    return "스테이지 로드 위치";
                case "stage load avoid":
                    return "스테이지 로드 회피 위치";
                case "stage load feeder avoid":
                    return "스테이지 로드 후 Feeder 회피 위치";
                case "stage unload":
                    return "스테이지 언로드 위치";
                case "stage unload avoid":
                    return "스테이지 언로드 회피 위치";
                case "exchange":
                    return "교환 위치";
                case "avoid":
                    return "회피 위치";
                case "before cassette to feeder load":
                    return "카세트에서 Feeder로 로드";
                case "before feeder to stage load":
                    return "Feeder에서 스테이지로 로드";
                case "before stage unload":
                    return "스테이지 언로드";
                default:
                    return string.IsNullOrWhiteSpace(description) ? "OutputFeederY" : description;
            }
        }

        private bool IsAnyFrontPickerInOutputZone()
        {
            if (FrontPicker == null)
                return false;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (FrontPicker.IsFrontPickerInDiePlacePosition(pickerNo))
                    return true;
            }

            return false;
        }

        private bool IsAnyRearPickerInOutputZone()
        {
            if (RearPicker == null)
                return false;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (RearPicker.IsRearPickerInDiePlacePosition(pickerNo))
                    return true;
            }

            return false;
        }

        private string BuildPickerAvoidState()
        {
            return "frontAvoid=" + (FrontPicker != null && FrontPicker.IsFrontPickerInAvoidPosition()) +
                   ", rearAvoid=" + (RearPicker != null && RearPicker.IsRearPickerInAvoidPosition());
        }

        protected int CheckPickersClearForOutputTransport(string description)
        {
            string frontDetail;
            if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                Context != null ? Context.Machine : null,
                true,
                PickerWorkZone.Output,
                out frontDetail))
            {
                return Fail("OUT-FEEDER-FRONT-PICKER-OUTPUT-ZONE", FrontPicker != null ? FrontPicker.Name : "FrontPicker",
                    "FrontPicker가 Output zone을 점유하거나 진입 중입니다. " +
                    LocalizeOutputTransportDescription(description) + " 전 OutputFeeder 이동을 시작할 수 없습니다. " +
                    frontDetail);
            }

            string rearDetail;
            if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                Context != null ? Context.Machine : null,
                false,
                PickerWorkZone.Output,
                out rearDetail))
            {
                return Fail("OUT-FEEDER-REAR-PICKER-OUTPUT-ZONE", RearPicker != null ? RearPicker.Name : "RearPicker",
                    "RearPicker가 Output zone을 점유하거나 진입 중입니다. " +
                    LocalizeOutputTransportDescription(description) + " 전 OutputFeeder 이동을 시작할 수 없습니다. " +
                    rearDetail);
            }

            return 0;
        }

        private static string LocalizeOutputTransportDescription(string description)
        {
            switch (description)
            {
                case "before cassette to feeder load":
                    return "카세트에서 OutputFeeder로 로드";
                case "before feeder to stage load":
                    return "OutputFeeder에서 OutputStage로 로드";
                case "before stage unload":
                    return "OutputStage에서 OutputFeeder로 언로드";
                case "OutputStage Load 준비":
                case "OutputStage Unload 준비":
                    return description;
                default:
                    return string.IsNullOrWhiteSpace(description) ? "OutputFeederY" : description;
            }
        }

        protected async Task<int> WaitPickersClearForOutputTransportAsync(string description, CancellationToken ct)
        {
            try
            {
                if (FrontPicker == null)
                    return Fail("OUT-FEEDER-PICKER-MISSING", "FrontPicker", description + " 전 FrontPicker 유닛을 확인할 수 없습니다.");

                if (RearPicker == null)
                    return Fail("OUT-FEEDER-PICKER-MISSING", "RearPicker", description + " 전 RearPicker 유닛을 확인할 수 없습니다.");

                int timeoutMs = ResolveTimeout();
                DateTime startTime = DateTime.UtcNow;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    string frontDetail;
                    string rearDetail;
                    bool frontBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        true,
                        PickerWorkZone.Output,
                        out frontDetail);
                    bool rearBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        false,
                        PickerWorkZone.Output,
                        out rearDetail);

                    if (!frontBlocking && !rearBlocking)
                        return 0;

                    string alarmState = BuildPickerTransportAlarmState();
                    if (!string.IsNullOrEmpty(alarmState))
                    {
                        return Fail("OUT-FEEDER-PICKER-ALARM", Name,
                            description + " 대기 불가: Picker 축 알람 상태입니다. " +
                            "front=" + frontDetail + ", rear=" + rearDetail + ", " + alarmState);
                    }

                    double elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("OUT-FEEDER-PICKER-OUTPUT-ZONE-TIMEOUT", Name,
                            description + " 대기 시간 초과: Picker가 Output zone에서 벗어나지 않았습니다. " +
                            "timeoutMs=" + timeoutMs + ", front=" + frontDetail +
                            ", rear=" + rearDetail + ", " + BuildPickerTransportMotionState());
                    }

                    if (!waitLogged)
                    {
                        WriteLog(Name,
                            description + " 전 Picker Output zone 해제 대기. " +
                            "front=" + frontDetail + ", rear=" + rearDetail +
                            ", timeoutMs=" + timeoutMs + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(50, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-PICKER-OUTPUT-ZONE-WAIT-EX", Name,
                    description + " 전 Picker Output zone 해제 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildPickerTransportAlarmState()
        {
            string reason = string.Empty;
            AppendPickerAxisAlarm(ref reason, "FrontPickerX", FrontPicker != null ? FrontPicker.PickerX : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerY", FrontPicker != null ? FrontPicker.PickerY : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerX", RearPicker != null ? RearPicker.PickerX : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerY", RearPicker != null ? RearPicker.PickerY : null);
            return reason;
        }

        private string BuildPickerTransportMotionState()
        {
            string state = string.Empty;
            AppendPickerAxisMotion(ref state, "FrontPickerX", FrontPicker != null ? FrontPicker.PickerX : null);
            AppendPickerAxisMotion(ref state, "FrontPickerY", FrontPicker != null ? FrontPicker.PickerY : null);
            AppendPickerAxisMotion(ref state, "RearPickerX", RearPicker != null ? RearPicker.PickerX : null);
            AppendPickerAxisMotion(ref state, "RearPickerY", RearPicker != null ? RearPicker.PickerY : null);
            return state;
        }

        private static void AppendPickerAxisAlarm(ref string reason, string label, QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null || !axis.IsAlarm)
                return;

            if (reason.Length > 0)
                reason += " ";

            reason += label +
                "(servo=" + axis.IsServoOn +
                ", alarm=" + axis.IsAlarm +
                ", moving=" + axis.IsMoving +
                ", actual=" + axis.ActualPosition +
                ");";
        }

        private static void AppendPickerAxisMotion(ref string state, string label, QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null)
                return;

            if (state.Length > 0)
                state += " ";

            state += label +
                "(servo=" + axis.IsServoOn +
                ", alarm=" + axis.IsAlarm +
                ", moving=" + axis.IsMoving +
                ", actual=" + axis.ActualPosition +
                ");";
        }

        protected int FailUnsupportedStep()
        {
            return Fail("OUT-FEEDER-STEP", Name, "Unsupported output feeder step: " + CurrentStep);
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    WriteLog(source, message + " - Stopped");
                    Context.LogPublic("[OUTPUT-FEEDER] STOP " + message);
                    throw new SequenceStopException(message);
                }

                TStep failedStep = CurrentStep;
                CurrentStep = ErrorStep;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                SequenceFailureStore.Record(SequenceStateName, Kind.ToString(), failedStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                Context.LogPublic("[OUTPUT-FEEDER] FAIL " + alarmCode + " - " + message);
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
            catch (Exception ex)
            {
                WriteLog(Name, "OutputFeeder 시작 스텝 복원 중 예외가 발생했습니다. state=" +
                    SequenceStateName + ", error=" + ex.Message + " - Failed");
                return initialStep;
            }
            finally
            {
            }
        }

        protected int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 300000;
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + Kind; }
        }

        protected static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
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

        protected static async Task<bool> AwaitStepWithCancellationAsync(Task<bool> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitBoolAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        protected static async Task<AxisMoveWaitResult> AwaitStepWithCancellationAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAxisWaitAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
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
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OutputFeeder sequence log failed. source=" + source + ", error=" + ex.Message);
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.OutputSeq, source, message);
        }


        protected async Task<int> MoveOutputCassetteAvoidPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                OutputCassetteUnit cassette = Cassette;
                if (cassette == null ||
                    cassette.OutputLifterZ == null ||
                    cassette.Recipe == null)
                {
                    return Fail(
                        "OUT-FEEDER-CST-MISSING",
                        "OutputCassette",
                        "Output cassette unit, axis, or recipe is not available.");
                }

                // 기구 간섭 방지:
                // OutputCassette 이동 전 OutputFeeder가 정지된 실제 AVOID 위치여야 한다.
                if (Feeder == null ||
                    Feeder.FeederY == null ||
                    Feeder.FeederY.IsMoving ||
                    !Feeder.IsBinFeederAvoidPositionCheck())
                {
                    return Fail(
                        "OUT-FEEDER-CST-AVOID-INTERLOCK",
                        cassette.Name,
                        "OutputCassette AVOID 이동 불가: " +
                        "OutputFeeder가 정지된 AVOID 위치가 아닙니다. " +
                        (Feeder != null
                            ? Feeder.DescribeFeederCylinderState()
                            : "OutputFeeder=null"));
                }

                double target = cassette.Recipe.AvoidPosition;

                int result = await AwaitStepWithCancellationAsync(
                    cassette.MoveBinLifterZ(
                        target,
                        Options != null && Options.FineMove,
                        ct),
                    ct).ConfigureAwait(false);

                if (result != 0)
                {
                    return Fail(
                        "OUT-FEEDER-CST-AVOID-MOVE",
                        cassette.Name,
                        "OutputCassette AVOID 이동 실패. result=" + result +
                        ", target=" + target +
                        ", actual=" + cassette.OutputLifterZ.ActualPosition +
                        ", detail=" + cassette.LastBinLifterMoveFailureMessage);
                }

                if (cassette.OutputLifterZ.IsMoving ||
                    cassette.OutputLifterZ.IsAlarm ||
                    !cassette.IsBinLifterZInAvoidPosition())
                {
                    return Fail(
                        "OUT-FEEDER-CST-AVOID-CHECK",
                        cassette.Name,
                        "OutputCassette AVOID 도착 확인 실패. " +
                        "moving=" + cassette.OutputLifterZ.IsMoving +
                        ", alarm=" + cassette.OutputLifterZ.IsAlarm +
                        ", actual=" + cassette.OutputLifterZ.ActualPosition +
                        ", target=" + target);
                }

                CurrentStep = CompleteStep;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "OUT-FEEDER-CST-AVOID-EX",
                    Name,
                    "OutputCassette 최종 AVOID 이동 중 예외 발생. error=" +
                    ex.Message);
            }
            finally
            {
            }
        }

    }
}
