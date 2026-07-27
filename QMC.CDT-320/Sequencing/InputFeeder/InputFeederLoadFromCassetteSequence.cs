using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederLoadFromCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckCassetteWaferData,
        MoveCassetteToWaferSlot,
        MoveStageToAvoidPosition,
        MoveStageToLoadPosition,
        CheckPickerAvoidPosition,
        PrepareFeederUnclamp,
        PrepareFeederLiftDown,
        MoveFeederLoadPosition,
        VerifyWaferDetected,
        ClampFeederWafer,
        MoveMaterialDataToFeeder,
        UpdateCassetteData,
        Complete,
        Error
    }

    internal sealed class InputFeederLoadFromCassetteSequence : InputFeederSequenceBase<InputFeederLoadFromCassetteStep>
    {
        public InputFeederLoadFromCassetteSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.LoadFromCassette, "InputFeederLoadFromCassetteSequence")
        {
        }

        protected override InputFeederLoadFromCassetteStep IdleStep { get { return InputFeederLoadFromCassetteStep.Idle; } }
        protected override InputFeederLoadFromCassetteStep InitialStep { get { return InputFeederLoadFromCassetteStep.CheckUnit; } }
        protected override InputFeederLoadFromCassetteStep CompleteStep { get { return InputFeederLoadFromCassetteStep.Complete; } }
        protected override InputFeederLoadFromCassetteStep ErrorStep { get { return InputFeederLoadFromCassetteStep.Error; } }

        protected override InputFeederLoadFromCassetteStep ResolveStartStep(InputFeederLoadFromCassetteStep initialStep)
        {
            InputFeederLoadFromCassetteStep resolvedStep = base.ResolveStartStep(initialStep);
            switch (resolvedStep)
            {
                case InputFeederLoadFromCassetteStep.CheckTransferReady:
                case InputFeederLoadFromCassetteStep.CheckCassetteWaferData:
                case InputFeederLoadFromCassetteStep.MoveCassetteToWaferSlot:
                case InputFeederLoadFromCassetteStep.MoveStageToAvoidPosition:
                case InputFeederLoadFromCassetteStep.MoveStageToLoadPosition:
                case InputFeederLoadFromCassetteStep.CheckPickerAvoidPosition:
                case InputFeederLoadFromCassetteStep.PrepareFeederUnclamp:
                case InputFeederLoadFromCassetteStep.PrepareFeederLiftDown:
                    WriteLog(
                        "ResolveStartStep",
                        "Input cassette load 재개 전 앞단 안전조건을 다시 확인합니다. savedStep=" +
                        resolvedStep + ", restartStep=" +
                        InputFeederLoadFromCassetteStep.CheckUnit + " - Check");
                    return InputFeederLoadFromCassetteStep.CheckUnit;

                case InputFeederLoadFromCassetteStep.MoveFeederLoadPosition:
                    bool alreadyAtLoadPosition =
                        IsFeederAtCassetteLoadPositionComplete();
                    InputFeederLoadFromCassetteStep restartStep =
                        alreadyAtLoadPosition
                            ? InputFeederLoadFromCassetteStep.VerifyWaferDetected
                            : InputFeederLoadFromCassetteStep.CheckPickerAvoidPosition;
                    WriteLog(
                        "ResolveStartStep",
                        "InputFeeder cassette load 이동 재개 위치를 판정했습니다. savedStep=" +
                        resolvedStep + ", alreadyAtLoadPosition=" +
                        alreadyAtLoadPosition + ", restartStep=" + restartStep + " - Check");
                    return restartStep;

                case InputFeederLoadFromCassetteStep.VerifyWaferDetected:
                case InputFeederLoadFromCassetteStep.ClampFeederWafer:
                case InputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder:
                    return ResolveLateCassetteLoadResumeStep(resolvedStep);

                default:
                    return resolvedStep;
            }
        }

        private InputFeederLoadFromCassetteStep ResolveLateCassetteLoadResumeStep(
            InputFeederLoadFromCassetteStep savedStep)
        {
            if (IsFeederAtCassetteLoadPositionComplete())
            {
                WriteLog(
                    "ResolveStartStep",
                    "Input cassette load 후반 Step을 CassetteLoad 위치에서 재개합니다. savedStep=" +
                    savedStep + " - Check");
                return savedStep;
            }

            if (IsFeederSafeForFullCassetteLoadRestart())
            {
                WriteLog(
                    "ResolveStartStep",
                    "InputFeeder가 안전한 Avoid/Down/Unclamp/Empty/Ring OFF 상태이므로 " +
                    "저장된 후반 Step 대신 앞단 안전조건부터 다시 확인합니다. savedStep=" +
                    savedStep + ", restartStep=" +
                    InputFeederLoadFromCassetteStep.CheckUnit + " - Check");
                return InputFeederLoadFromCassetteStep.CheckUnit;
            }

            WriteLog(
                "ResolveStartStep",
                "Input cassette load 후반 Step의 물리상태가 재시작 조건과 일치하지 않습니다. " +
                "기존 Step의 강한 사전조건으로 차단합니다. savedStep=" +
                savedStep + ". " +
                (Feeder != null
                    ? Feeder.GetWaferFeederTransferState()
                    : "InputFeeder=null") + " - Check");
            return savedStep;
        }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputFeederLoadFromCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederLoadFromCassetteStep.CheckTransferReady));
                    // 이송 준비 확인
                    case InputFeederLoadFromCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    // 카세트 웨이퍼 데이터 확인
                    case InputFeederLoadFromCassetteStep.CheckCassetteWaferData:
                        return Task.FromResult(CheckCassetteWaferData());
                    // 카세트 웨이퍼 슬롯 이동
                    case InputFeederLoadFromCassetteStep.MoveCassetteToWaferSlot:
                        return MoveCassetteToWaferSlotAsync(ct);
                    // 스테이지 어보이드 위치 이동
                    case InputFeederLoadFromCassetteStep.MoveStageToAvoidPosition:
                        return MoveStageToAvoidPositionAsync(ct);
                    // 스테이지 로드 위치 이동
                    case InputFeederLoadFromCassetteStep.MoveStageToLoadPosition:
                        return MoveStageToLoadPositionAsync(ct);
                    // 피커 Input zone 해제 확인
                    case InputFeederLoadFromCassetteStep.CheckPickerAvoidPosition:
                        return WaitPickersClearForInputTransportAsync(ct);
                    // 피더 언클램프 준비
                    case InputFeederLoadFromCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    // 피더 리프트 다운 준비
                    case InputFeederLoadFromCassetteStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);
                    // 피더 로드 위치 이동
                    case InputFeederLoadFromCassetteStep.MoveFeederLoadPosition:
                        return MoveFeederLoadPositionAsync(ct);
                    // 웨이퍼 감지 검증
                    case InputFeederLoadFromCassetteStep.VerifyWaferDetected:
                        return VerifyWaferDetectedAsync(ct);
                    // 피더 웨이퍼 클램프
                    case InputFeederLoadFromCassetteStep.ClampFeederWafer:
                        return ClampFeederWaferAsync(ct);
                    // 자재 데이터를 피더로 이동
                    case InputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder:
                        return MoveMaterialDataToFeederAsync(ct);
                    // 카세트 데이터 갱신
                    case InputFeederLoadFromCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-CST-LOAD-STEP-EX", "InputFeederLoadFromCassetteSequence", "Load from cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            // To do: 피더 점유(자재 데이터/물리 링) 상태에서 카세트→피더 로드를 시작하면 이중 적재 위험이 있다.
            //        Output(VerifyFeederEmpty)에는 있던 방어가 Input에는 없어 추가한다.
            WaferMaterial feederWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
            if (feederWafer != null)
                return Fail("IN-FEEDER-DATA-OCCUPIED", "Material",
                    "Input feeder material data must be empty before cassette to feeder load. wafer=" + feederWafer.WaferId +
                    ", sourceRole=" + feederWafer.SourceCassetteRole +
                    ", sourceSlot=" + (feederWafer.SourceSlotNumber + 1).ToString("00"));

            if (!IsFeederRingStateConfirmed(false))
                return Fail("IN-FEEDER-WAFER-OCCUPIED", Feeder.Name,
                    "Input feeder already holds a wafer(ring detected) before cassette to feeder load. " +
                    Feeder.GetWaferFeederTransferState());

            string feederReason;
            if (!Feeder.CheckWaferCassetteReady(Options.SlotIndex, TransferMode.Load, out feederReason))
                return Fail("IN-FEEDER-CST-READY", Feeder.Name, "Input feeder cassette load condition is not ready. " + feederReason);

            InputCassetteUnit cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            string cassetteReason;
            if (!CheckCassetteTransferReadyForEntry(cassette, out cassetteReason))
                return Fail("IN-FEEDER-CST-SENSOR", cassette.Name, "Input cassette is not detected or not ready for transfer. " + cassetteReason);

            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            if (!IsInputStageEmpty(stage))
                return Fail("IN-FEEDER-STAGE-OCCUPIED", "InputStage", "Input stage must be empty before cassette to feeder load.");

            CurrentStep = InputFeederLoadFromCassetteStep.CheckCassetteWaferData;
            return 0;
        }

        private int CheckCassetteWaferData()
        {
            InputCassetteUnit cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            if (cassette.IsInputCassetteProcessComplete())
            {
                cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
                NotifyInputCassetteReplacementRequired();
                return Fail("IN-FEEDER-CST-COMPLETE", cassette.Name,
                    "Input cassette processing is complete. Replace input cassette.");
            }

            int nextSlot = cassette.FindNextProcessWaferSlot();
            if (nextSlot < 0 && cassette.IsInputCassetteProcessComplete())
            {
                cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
                NotifyInputCassetteReplacementRequired();
                return Fail("IN-FEEDER-CST-COMPLETE", cassette.Name,
                    "Input cassette processing is complete. Replace input cassette.");
            }

            if (Options.RunMode == SequenceRunMode.Auto &&
                nextSlot >= 0 &&
                Options.SlotIndex != nextSlot)
            {
                return Fail("IN-FEEDER-CST-SLOT-MISMATCH", cassette.Name,
                    "Selected slot is not current process slot. selected=" + Options.SlotIndex + ", current=" + nextSlot);
            }

            if (!IsSelectedSlotProcessReady(cassette, Options.SlotIndex))
            {
                return Fail("IN-FEEDER-CST-SLOT-NOT-READY", cassette.Name,
                    "Selected cassette slot is not ready for wafer loading. slot=" + Options.SlotIndex);
            }

            WaferMaterial wafer;
            string validationCode;
            string validationReason;
            if (!TryValidateSelectedCassetteWafer(
                out wafer,
                out validationCode,
                out validationReason))
            {
                return Fail(validationCode, "Material", validationReason);
            }

            if (WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.WorkReady)
            {
                WriteLog(
                    "CheckCassetteWaferData",
                    "WorkReady wafer를 정확한 Input cassette role/slot 검증 후 로딩 대상으로 허용합니다. " +
                    "wafer=" + wafer.WaferId +
                    ", role=" + Options.CassetteRole +
                    ", slot=" + (Options.SlotIndex + 1).ToString("00") + " - Check");
            }

            int entrySafety = CheckCassetteLoadEntrySafety();
            if (entrySafety != 0)
                return entrySafety;

            CurrentStep = InputFeederLoadFromCassetteStep.MoveCassetteToWaferSlot;
            return 0;
        }

        private async Task<int> MoveCassetteToWaferSlotAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            InputCassetteUnit cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            // To do: 1단/2단 로딩 지원. Input2(2단)이면 level=2로 전달해 해당 레벨 로딩 위치로 이동한다.
            int cassetteLevel = Options.CassetteRole == CassetteMaterialRole.Input2 ? 2 : 1;
            int result = await AwaitStepWithCancellationAsync(
                cassette.PrepareWaferCassetteForFeederLoad(Options.SlotIndex, ResolveTimeout(), Options.FineMove, cassetteLevel),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-CST-SLOT-MOVE", cassette.Name, "Input cassette slot move failed. slot=" + Options.SlotIndex + ", result=" + result);

            CurrentStep = InputFeederLoadFromCassetteStep.MoveStageToAvoidPosition;
            return 0;
        }

        private void NotifyInputCassetteReplacementRequired()
        {
            Context.RequestOperatorMessage(
                "입력 카세트 교체",
                "입력 카세트의 모든 웨이퍼 작업이 완료되었습니다.\r\n카세트를 교체한 뒤 필요한 작업을 진행하세요.");
        }

        private async Task<int> MoveStageToAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");
            if (stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-RECIPE", stage.Name, "Input stage recipe is not available.");

            if (IsInputStageFullyPreparedForCassetteLoad(stage))
            {
                CurrentStep = InputFeederLoadFromCassetteStep.CheckPickerAvoidPosition;
                return 0;
            }

            Task<int> needleZMove = MoveStageAxisCommandAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid", ct);
            Task<int> ejectPinZMove = MoveStageAxisCommandAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid", ct);
            int[] zResults = await Task.WhenAll(needleZMove, ejectPinZMove).ConfigureAwait(false);
            if (zResults[0] != 0)
                return zResults[0];
            if (zResults[1] != 0)
                return zResults[1];

            Task<int> needleZWait = WaitStageAxisInPositionResultAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid", ct);
            Task<int> ejectPinZWait = WaitStageAxisInPositionResultAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid", ct);
            int[] zWaitResults = await Task.WhenAll(needleZWait, ejectPinZWait).ConfigureAwait(false);
            if (zWaitResults[0] != 0)
                return zWaitResults[0];
            if (zWaitResults[1] != 0)
                return zWaitResults[1];

            int result = CheckStageAxisInPosition(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid");
            if (result != 0)
                return result;

            result = CheckStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid");
            if (result != 0)
                return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.AvoidPosition, "StageZ avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.VisionX, stage.Recipe.VisionX.AvoidPosition, "VisionX avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.NeedleX, stage.Recipe.NeedleX.AvoidPosition, "NeedleX avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederLoadFromCassetteStep.MoveStageToLoadPosition;
            return 0;
        }

        private async Task<int> MoveStageToLoadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.LoadPosition, "StageY load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.LoadPosition, "StageT load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.LoadPosition, "StageZ load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederLoadFromCassetteStep.CheckPickerAvoidPosition;
            return 0;
        }

        private async Task<int> WaitPickersClearForInputTransportAsync(CancellationToken ct)
        {
            try
            {
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
                        PickerWorkZone.Input,
                        out frontDetail);
                    bool rearBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        false,
                        PickerWorkZone.Input,
                        out rearDetail);

                    if (!frontBlocking && !rearBlocking)
                    {
                        CurrentStep = InputFeederLoadFromCassetteStep.PrepareFeederUnclamp;
                        return 0;
                    }

                    string alarmState = BuildPickerAlarmState();
                    if (!string.IsNullOrEmpty(alarmState))
                    {
                        return Fail("IN-FEEDER-PICKER-ALARM", Name,
                            "InputFeeder 로드 준비 대기 불가: Picker 축 알람 상태입니다. " +
                            "front=" + frontDetail + ", rear=" + rearDetail + ", " + alarmState);
                    }

                    double elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("IN-FEEDER-PICKER-INPUT-ZONE-TIMEOUT", Name,
                            "InputFeeder 로드 준비 대기 시간 초과: Picker가 Input zone에서 벗어나지 않았습니다. " +
                            "timeoutMs=" + timeoutMs + ", front=" + frontDetail +
                            ", rear=" + rearDetail + ", " + BuildPickerMotionState());
                    }

                    if (!waitLogged)
                    {
                        WriteLog(Name,
                            "InputFeeder 로드 준비 전 Picker Input zone 해제 대기. " +
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
                return Fail("IN-FEEDER-PICKER-INPUT-ZONE-WAIT-EX", Name,
                    "InputFeeder 로드 준비 전 Picker Input zone 해제 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildPickerAlarmState()
        {
            PickerFrontUnit front = Context != null && Context.Machine != null ? Context.Machine.PickerFrontUnit : null;
            PickerRearUnit rear = Context != null && Context.Machine != null ? Context.Machine.PickerRearUnit : null;

            string reason = string.Empty;
            AppendPickerAxisAlarm(ref reason, "FrontPickerX", front != null ? front.PickerX : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerY", front != null ? front.PickerY : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerX", rear != null ? rear.PickerX : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerY", rear != null ? rear.PickerY : null);
            return reason;
        }

        private string BuildPickerMotionState()
        {
            PickerFrontUnit front = Context != null && Context.Machine != null ? Context.Machine.PickerFrontUnit : null;
            PickerRearUnit rear = Context != null && Context.Machine != null ? Context.Machine.PickerRearUnit : null;

            string state = string.Empty;
            AppendPickerAxisMotion(ref state, "FrontPickerX", front != null ? front.PickerX : null);
            AppendPickerAxisMotion(ref state, "FrontPickerY", front != null ? front.PickerY : null);
            AppendPickerAxisMotion(ref state, "RearPickerX", rear != null ? rear.PickerX : null);
            AppendPickerAxisMotion(ref state, "RearPickerY", rear != null ? rear.PickerY : null);
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

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederUnclamp())
                return Fail("IN-FEEDER-UNCLAMP", Feeder.Name,
                    "WaferFeeder unclamp command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederLoadFromCassetteStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Feeder.IsWaferFeederDown())
            {
                CurrentStep = InputFeederLoadFromCassetteStep.MoveFeederLoadPosition;
                return 0;
            }

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederUpDownAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN", Feeder.Name,
                    "WaferFeeder lift down command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederLoadFromCassetteStep.MoveFeederLoadPosition;
            return 0;
        }

        private async Task<int> MoveFeederLoadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int precondition = CheckFeederLoadMoveSafety();
            if (precondition != 0)
                return precondition;

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederCassetteLoadPosition(Options.SlotIndex, Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-LOAD-POS", Feeder.Name,
                    "WaferFeeder cassette load position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInCassetteLoadPosition(Options.SlotIndex),
                "WaferFeeder cassette load position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederLoadFromCassetteStep.VerifyWaferDetected;
            return 0;
        }

        private async Task<int> VerifyWaferDetectedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer;
            int precondition = CheckFeederAtCassetteLoadSafety(
                false,
                "VerifyWaferDetected",
                out wafer);
            if (precondition != 0)
                return precondition;

            bool detected = await WaitFeederRingStateConfirmedAsync(
                true,
                ResolveTimeout(),
                ct).ConfigureAwait(false);
            if (!detected)
                return Fail("IN-FEEDER-WAFER-SENSOR", Feeder.Name, "Wafer sensor timeout or data/sensor mismatch before clamp. waferId=" + wafer.WaferId);

            CurrentStep = InputFeederLoadFromCassetteStep.ClampFeederWafer;
            return 0;
        }

        private async Task<int> ClampFeederWaferAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer;
            bool clampAlreadyComplete =
                Feeder != null &&
                Feeder.IsWaferFeederClamp() &&
                !Feeder.IsWaferFeederUnclamp() &&
                IsFeederRingStateConfirmed(true);
            if (clampAlreadyComplete)
            {
                int completedPrecondition = CheckFeederAtCassetteLoadSafety(
                    true,
                    "ClampFeederWaferResume",
                    out wafer);
                if (completedPrecondition != 0)
                    return completedPrecondition;

                CurrentStep = InputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder;
                return 0;
            }

            int precondition = CheckFeederAtCassetteLoadSafety(
                false,
                "ClampFeederWafer",
                out wafer);
            if (precondition != 0)
                return precondition;

            if (!IsFeederRingStateConfirmed(true))
                return Fail(
                    "IN-FEEDER-CLAMP-WAFER-SENSOR",
                    Feeder.Name,
                    "Wafer sensor is not detected before feeder clamp. wafer=" + wafer.WaferId);

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(true, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederClamp())
                return Fail("IN-FEEDER-CLAMP", Feeder.Name,
                    "WaferFeeder clamp command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            if (!IsFeederRingStateConfirmed(true))
                return Fail("IN-FEEDER-CLAMP-WAFER-SENSOR", Feeder.Name, "Wafer sensor is not detected after feeder clamp.");

            CurrentStep = InputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private async Task<int> MoveMaterialDataToFeederAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer;
            int precondition = CheckFeederAtCassetteLoadSafety(
                true,
                "MoveMaterialDataToFeeder",
                out wafer);
            if (precondition != 0)
                return precondition;

            bool stableRingDetected = await WaitFeederRingStateConfirmedAsync(
                true,
                ResolveTimeout(),
                ct).ConfigureAwait(false);
            if (!stableRingDetected)
            {
                return Fail(
                    "IN-FEEDER-MATERIAL-RING-STABLE",
                    Feeder.Name,
                    "Material 이동 전 InputFeeder Ring ON 안정 확인에 실패했습니다. wafer=" +
                    wafer.WaferId + ". " +
                    Feeder.GetWaferFeederTransferState());
            }

            precondition = CheckFeederAtCassetteLoadSafety(
                true,
                "MoveMaterialDataToFeederAfterRingStable",
                out wafer);
            if (precondition != 0)
                return precondition;

            Feeder.SetCurrentWaferMaterial(wafer);
            MaterialStateService.MoveWaferToInputFeeder(wafer);

            CurrentStep = InputFeederLoadFromCassetteStep.UpdateCassetteData;
            return 0;
        }

        private int UpdateCassetteData()
        {
            InputCassetteUnit cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette != null)
                cassette.UpdateWaferCassetteSlotState(
                    InputCassetteUnit.ResolveCassetteLevel(Options.CassetteRole),
                    Options.SlotIndex,
                    SlotPresence.Exist,
                    ProcessState.Processing);

            Context.Bus.Set("InputFeederOccupied");
            CurrentStep = InputFeederLoadFromCassetteStep.Complete;
            return 0;
        }

        private async Task<int> MoveStageAxisCommandAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisMove(item, target, out interlockReason))
                    return Fail("IN-FEEDER-STAGE-INTERLOCK", stage.Name,
                        description + " 이동 인터락 차단. " + interlockReason + ". " +
                        BuildStageAxisState(stage, axis, target));

                int result = await AwaitStepWithCancellationAsync(stage.MoveInputStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-FEEDER-STAGE-MOVE", stage.Name, description + " 이동 명령 실패. result=" + result + ", " + BuildStageAxisState(stage, axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStage",
                    description + " 이동 명령 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveStageAxisAndVerifyAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                int result = await MoveStageAxisCommandAsync(stage, axis, target, description, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-MOVE-VERIFY-EX", stage != null ? stage.Name : "InputStage",
                    description + " 이동 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitStageAxisInPositionResultAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int waitCode = await stage.WaitInputStageAxisInPositionResult(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitCode != 0)
                    return Fail("IN-FEEDER-STAGE-MOVE", stage.Name,
                        description + " 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildStageAxisState(stage, axis, target));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-WAIT-EX", stage != null ? stage.Name : "InputStage",
                    description + " 이동 완료 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return Fail("IN-FEEDER-STAGE-AXIS", stage.Name, description + " axis is not available. " + BuildStageAxisState(stage, axis, target));

            if (item.IsMoving || item.IsAlarm || !IsStageAxisInPosition(item, target))
                return Fail("IN-FEEDER-STAGE-POSITION", stage.Name, description + " position check failed. " + BuildStageAxisState(stage, axis, target));

            return 0;
        }

        private async Task<bool> WaitStageAxisInPositionAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            int timeoutMs,
            CancellationToken ct)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return false;

            DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs > 0 ? timeoutMs : 10000);
            while (DateTime.Now <= deadline)
            {
                ct.ThrowIfCancellationRequested();
                if (!item.IsMoving && IsStageAxisInPosition(item, target))
                    return true;

                await Task.Delay(20, ct).ConfigureAwait(false);
            }

            return !item.IsMoving && IsStageAxisInPosition(item, target);
        }

        private static bool IsStageAxisInPosition(QMC.Common.Motion.BaseAxis item, double target)
        {
            if (item == null)
                return false;

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(item.ActualPosition - target) <= tolerance;
        }

        private bool IsInputStageFullyPreparedForCassetteLoad(InputStageUnit stage)
        {
            if (stage == null ||
                stage.Recipe == null ||
                !IsInputStageEmpty(stage))
            {
                return false;
            }

            return IsStageAxisStronglyComplete(
                       stage.StageY,
                       stage.Recipe.WaferY.LoadPosition) &&
                   IsStageAxisStronglyComplete(
                       stage.StageT,
                       stage.Recipe.WaferT.LoadPosition) &&
                   IsStageAxisStronglyComplete(
                       stage.ExpanderZ,
                       stage.Recipe.WaferZ.LoadPosition) &&
                   IsStageAxisStronglyComplete(
                       stage.CameraX,
                       stage.Recipe.VisionX.AvoidPosition) &&
                   IsStageAxisStronglyComplete(
                       stage.NeedleBlockX,
                       stage.Recipe.NeedleX.AvoidPosition) &&
                   IsStageAxisStronglyComplete(
                       stage.NeedleZ,
                       stage.Recipe.NeedleZ.AvoidPosition) &&
                   IsStageAxisStronglyComplete(
                       stage.EjectPinZ,
                       stage.Recipe.EjectPinZ.AvoidPosition);
        }

        private static bool IsStageAxisStronglyComplete(
            QMC.Common.Motion.BaseAxis item,
            double target)
        {
            if (item == null)
                return false;

            double tolerance =
                item.Config != null &&
                item.Config.InPositionTolerance > 0.0
                    ? item.Config.InPositionTolerance
                    : 0.05;
            bool actualPositionKeyMatches =
                Math.Round(
                    item.ActualPosition,
                    3,
                    MidpointRounding.AwayFromZero) ==
                Math.Round(
                    target,
                    3,
                    MidpointRounding.AwayFromZero);
            bool commandPositionKeyMatches =
                Math.Round(
                    item.CommandPosition,
                    3,
                    MidpointRounding.AwayFromZero) ==
                Math.Round(
                    target,
                    3,
                    MidpointRounding.AwayFromZero);

            return item.IsServoOn &&
                   !item.IsAlarm &&
                   !item.IsMoving &&
                   item.IsInPosition &&
                   actualPositionKeyMatches &&
                   commandPositionKeyMatches &&
                   Math.Abs(item.ActualPosition - target) <= tolerance &&
                   Math.Abs(item.CommandPosition - target) <= tolerance;
        }

        private string BuildPickerXAxisState(QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null)
                return "axis=null";

            return "name=" + axis.Name +
                   ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (axis.IsMoving ? "Y" : "N") +
                   ", actual=" + axis.ActualPosition +
                   ", command=" + axis.CommandPosition;
        }

        private string BuildStageAxisState(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;

            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance +
                   FormatAxisLastMotionFailure(item) +
                   FormatStageLastMoveFailure(stage);
        }

        private static string FormatAxisLastMotionFailure(QMC.Common.Motion.BaseAxis item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.LastMotionFailureMessage))
                return string.Empty;

            return ", lastMotionFailure=" + item.LastMotionFailureMessage;
        }

        private static string FormatStageLastMoveFailure(InputStageUnit stage)
        {
            if (stage == null || string.IsNullOrWhiteSpace(stage.LastStageMoveFailureMessage))
                return string.Empty;

            return ", lastStageMoveFailure=" + stage.LastStageMoveFailureMessage;
        }

        private QMC.Common.Motion.BaseAxis ResolveStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                // 웨이퍼 Y축 반환
                case WaferStageAxis.WaferY: return stage.StageY;
                // 웨이퍼 T축 반환
                case WaferStageAxis.WaferT: return stage.StageT;
                // 웨이퍼 확장 Z축 반환
                case WaferStageAxis.WaferExpandingZ: return stage.ExpanderZ;
                // 비전 X축 반환
                case WaferStageAxis.VisionX: return stage.CameraX;
                // 니들 X축 반환
                case WaferStageAxis.NeedleX: return stage.NeedleBlockX;
                // 니들 Z축 반환
                case WaferStageAxis.NeedleZ: return stage.NeedleZ;
                // 이젝트 핀 Z축 반환
                case WaferStageAxis.EjectPinZ: return stage.EjectPinZ;
                default: return null;
            }
        }

        private bool TryValidateSelectedCassetteWafer(
            out WaferMaterial wafer,
            out string alarmCode,
            out string reason)
        {
            wafer = ResolveCassetteWafer();
            alarmCode = "IN-FEEDER-CST-WAFER-DATA";
            reason = string.Empty;

            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
            {
                reason =
                    "Mapped cassette wafer data was not found. role=" +
                    Options.CassetteRole + ", slot=" + Options.SlotIndex;
                return false;
            }

            WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
            if (state != WaferMaterialState.Ready &&
                state != WaferMaterialState.WorkReady)
            {
                alarmCode = "IN-FEEDER-CST-WAFER-STATE";
                reason =
                    "선택한 Input cassette wafer가 Ready/WorkReady 상태가 아닙니다. wafer=" +
                    wafer.WaferId + ", state=" + wafer.State;
                return false;
            }

            MaterialLocation location = wafer.CurrentLocation;
            if (location == null ||
                location.Kind != MaterialLocationKind.InputCassette ||
                location.CassetteRole != Options.CassetteRole ||
                location.SlotNumber != Options.SlotIndex)
            {
                alarmCode = "IN-FEEDER-CST-WAFER-LOCATION";
                reason =
                    "선택한 Input wafer의 현재 위치가 실제 cassette role/slot과 다릅니다. wafer=" +
                    wafer.WaferId + ", currentLocation=" +
                    (location != null ? location.ToString() : "null") +
                    ", targetRole=" + Options.CassetteRole +
                    ", targetSlot=" + (Options.SlotIndex + 1).ToString("00");
                return false;
            }

            CassetteMaterial cassetteMaterial =
                MaterialStateService.State != null &&
                MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(
                        item => item != null && item.Role == Options.CassetteRole)
                    : null;
            CassetteSlotMaterial slot =
                cassetteMaterial != null &&
                cassetteMaterial.Slots != null &&
                Options.SlotIndex >= 0 &&
                Options.SlotIndex < cassetteMaterial.Slots.Count
                    ? cassetteMaterial.Slots[Options.SlotIndex]
                    : null;
            if (slot == null ||
                !slot.HasWafer ||
                string.IsNullOrWhiteSpace(slot.WaferId) ||
                !string.Equals(slot.WaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
            {
                alarmCode = "IN-FEEDER-CST-WAFER-SLOT-DATA";
                reason =
                    "선택한 Input cassette slot의 Wafer ID와 Material이 일치하지 않습니다. wafer=" +
                    wafer.WaferId + ", slotWafer=" +
                    (slot != null ? slot.WaferId : "null") +
                    ", role=" + Options.CassetteRole +
                    ", slot=" + (Options.SlotIndex + 1).ToString("00");
                return false;
            }

            if (wafer.SourceCassetteRole != Options.CassetteRole ||
                wafer.SourceSlotNumber != Options.SlotIndex)
            {
                alarmCode = "IN-FEEDER-CST-WAFER-SOURCE";
                reason =
                    "선택한 Input wafer의 원본 cassette/slot 정보가 현재 slot과 다릅니다. wafer=" +
                    wafer.WaferId +
                    ", sourceRole=" + wafer.SourceCassetteRole +
                    ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00") +
                    ", targetRole=" + Options.CassetteRole +
                    ", targetSlot=" + (Options.SlotIndex + 1).ToString("00");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(
                    Options.ExpectedWaferId,
                    wafer.WaferId,
                    StringComparison.OrdinalIgnoreCase))
            {
                alarmCode = "IN-FEEDER-CST-WAFER-MISMATCH";
                reason =
                    "선택 계획과 현재 Input cassette slot의 Wafer ID가 다릅니다. expected=" +
                    Options.ExpectedWaferId + ", actual=" + wafer.WaferId;
                return false;
            }

            return true;
        }

        private int CheckCassetteLoadEntrySafety()
        {
            if (Feeder == null || !Feeder.IsWaferFeederTransferDataEmpty())
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-ENTRY-DATA",
                    "InputFeeder",
                    "WorkReady/Ready wafer 로딩 전 InputFeeder 데이터가 Empty가 아닙니다. " +
                    (Feeder != null ? Feeder.GetWaferFeederTransferState() : "Feeder=null"));
            }

            if (!IsFeederRingStateConfirmed(false))
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-ENTRY-RING",
                    Feeder.Name,
                    "WorkReady/Ready wafer 로딩 전 InputFeeder Ring이 OFF가 아닙니다. " +
                    Feeder.GetWaferFeederTransferState());
            }

            InputStageUnit stage =
                Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null)
                return Fail(
                    "IN-FEEDER-STAGE-MISSING",
                    "InputStage",
                    "Input stage unit is not available.");

            if (!IsInputStageEmpty(stage))
                return Fail(
                    "IN-FEEDER-STAGE-OCCUPIED",
                    "InputStage",
                    "Input stage must be empty before cassette to feeder load.");

            return 0;
        }

        private int CheckCassetteLoadMotionContext(
            string phase,
            bool requireCassetteMoveReady,
            out WaferMaterial wafer)
        {
            wafer = null;
            if (Feeder == null)
            {
                return Fail(
                    "IN-FEEDER-MISSING",
                    "InputFeeder",
                    "Input feeder unit is not available. phase=" + phase);
            }

            if (!HasValidCassetteLoadTeaching())
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-TEACHING",
                    Feeder.Name,
                    "InputFeeder CassetteLoad/Avoid teaching이 유효하지 않습니다. phase=" +
                    phase + ", cassetteLoad=" +
                    (Feeder.Recipe != null
                        ? Feeder.Recipe.CassetteLoadPosition.ToString()
                        : "null") +
                    ", avoid=" +
                    (Feeder.Recipe != null
                        ? Feeder.Recipe.AvoidPosition.ToString()
                        : "null"));
            }

            string validationCode;
            string validationReason;
            if (!TryValidateSelectedCassetteWafer(
                out wafer,
                out validationCode,
                out validationReason))
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-CONTEXT",
                    "Material",
                    "phase=" + phase + ", validationCode=" +
                    validationCode + ". " + validationReason);
            }

            if (!Feeder.IsWaferFeederTransferDataEmpty())
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-CONTEXT-DATA",
                    Feeder.Name,
                    "Cassette pickup 완료 전 InputFeeder 데이터가 Empty가 아닙니다. phase=" +
                    phase + ". " + Feeder.GetWaferFeederTransferState());
            }

            InputCassetteUnit cassette =
                Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette == null || cassette.InputLifterZ == null)
            {
                return Fail(
                    "IN-FEEDER-CST-MISSING",
                    "InputCassette",
                    "Input cassette unit or LifterZ is not available. phase=" + phase);
            }

            if (requireCassetteMoveReady)
            {
                string cassetteReason;
                if (!CheckCassetteTransferReadyForEntry(
                    cassette,
                    out cassetteReason))
                {
                    return Fail(
                        "IN-FEEDER-CST-SENSOR",
                        cassette.Name,
                        "Input cassette transfer condition is not ready. phase=" +
                        phase + ". " + cassetteReason);
                }
            }
            else if (!IsCassettePresenceConfirmed(cassette))
            {
                return Fail(
                    "IN-FEEDER-CST-NOT-DETECTED",
                    cassette.Name,
                    "Input cassette가 Feeder 진입 후 감지되지 않습니다. phase=" +
                    phase + ", waferSize=" + Options.WaferSize);
            }

            int cassetteLevel =
                InputCassetteUnit.ResolveCassetteLevel(Options.CassetteRole);
            double cassetteTarget =
                cassette.CalculateWaferCassetteSlotTargetPosition(
                    Options.SlotIndex,
                    cassetteLevel);
            double cassetteTolerance =
                cassette.ResolveWaferLifterZInPositionTolerance();
            bool cassetteActualOk =
                cassette.IsWaferLifterZInPosition(
                    cassetteTarget,
                    cassetteTolerance);
            bool cassetteCommandOk =
                Math.Abs(cassette.InputLifterZ.CommandPosition - cassetteTarget) <=
                cassetteTolerance;
            if (!cassette.InputLifterZ.IsServoOn ||
                cassette.InputLifterZ.IsAlarm ||
                cassette.InputLifterZ.IsMoving ||
                !cassette.InputLifterZ.IsInPosition ||
                !cassetteActualOk ||
                !cassetteCommandOk)
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-Z-PRECONDITION",
                    cassette.Name,
                    "InputCassette LifterZ가 선택 Slot 위치에 있지 않습니다. phase=" +
                    phase +
                    ", servo=" + cassette.InputLifterZ.IsServoOn +
                    ", alarm=" + cassette.InputLifterZ.IsAlarm +
                    ", moving=" + cassette.InputLifterZ.IsMoving +
                    ", inPosition=" + cassette.InputLifterZ.IsInPosition +
                    ", actual=" + cassette.InputLifterZ.ActualPosition +
                    ", command=" + cassette.InputLifterZ.CommandPosition +
                    ", target=" + cassetteTarget +
                    ", tolerance=" + cassetteTolerance);
            }

            InputStageUnit stage =
                Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null)
                return Fail(
                    "IN-FEEDER-STAGE-MISSING",
                    "InputStage",
                    "Input stage unit is not available. phase=" + phase);

            if (!IsInputStageEmpty(stage))
                return Fail(
                    "IN-FEEDER-STAGE-OCCUPIED",
                    "InputStage",
                    "Input stage must be empty before cassette pickup. phase=" + phase);

            return 0;
        }

        private int CheckFeederLoadMoveSafety()
        {
            WaferMaterial wafer;
            int contextResult =
                CheckCassetteLoadMotionContext(
                    "MoveFeederLoadPosition",
                    true,
                    out wafer);
            if (contextResult != 0)
                return contextResult;

            string feederReason;
            if (!Feeder.CheckWaferFeederMoveReady(out feederReason))
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-MOVE-READY",
                    Feeder.Name,
                    "InputFeeder cassette load 이동 준비가 되지 않았습니다. " +
                    feederReason);
            }

            if (Feeder.FeederY == null ||
                !Feeder.FeederY.IsServoOn ||
                Feeder.FeederY.IsAlarm ||
                Feeder.FeederY.IsMoving)
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-MOVE-AXIS",
                    Feeder.Name,
                    "InputFeederY가 정지된 Servo ON/Alarm OFF 상태가 아닙니다. " +
                    Feeder.GetWaferFeederTransferState());
            }

            if (!Feeder.IsWaferFeederDown() ||
                Feeder.IsWaferFeederUp() ||
                !Feeder.IsWaferFeederUnclamp() ||
                Feeder.IsWaferFeederClamp())
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-MOVE-POSTURE",
                    Feeder.Name,
                    "InputFeeder cassette load 이동 전 Down/Unclamp 자세가 아닙니다. wafer=" +
                    wafer.WaferId + ". " + Feeder.GetWaferFeederTransferState());
            }

            if (!IsFeederRingStateConfirmed(false))
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-MOVE-RING",
                    Feeder.Name,
                    "InputFeeder cassette load 이동 전 Ring이 OFF가 아닙니다. " +
                    Feeder.GetWaferFeederTransferState());
            }

            return 0;
        }

        private int CheckFeederAtCassetteLoadSafety(
            bool requireClampedWafer,
            string phase,
            out WaferMaterial wafer)
        {
            int contextResult =
                CheckCassetteLoadMotionContext(
                    phase,
                    false,
                    out wafer);
            if (contextResult != 0)
                return contextResult;

            if (!IsFeederAtCassetteLoadPositionComplete() ||
                !Feeder.IsWaferFeederDown() ||
                Feeder.IsWaferFeederUp())
            {
                double target =
                    Feeder.CalculateWaferFeederCassetteLoadPosition(Options.SlotIndex);
                double tolerance = ResolveFeederYInPositionTolerance();
                return Fail(
                    "IN-FEEDER-CST-LOAD-PICK-PRECONDITION",
                    Feeder.Name,
                    "InputFeeder가 정지된 CassetteLoad/Down 상태가 아닙니다. target=" +
                    target + ", tolerance=" + tolerance + ". " +
                    Feeder.GetWaferFeederTransferState());
            }

            if (requireClampedWafer)
            {
                if (!Feeder.IsWaferFeederClamp() ||
                    Feeder.IsWaferFeederUnclamp() ||
                    !IsFeederRingStateConfirmed(true))
                {
                    return Fail(
                        "IN-FEEDER-MATERIAL-PRECONDITION",
                        Feeder.Name,
                        "Material 이동 전 Clamp/Ring ON 상태가 아닙니다. wafer=" +
                        wafer.WaferId + ". " +
                        Feeder.GetWaferFeederTransferState());
                }
            }
            else if (!Feeder.IsWaferFeederUnclamp() ||
                     Feeder.IsWaferFeederClamp())
            {
                return Fail(
                    "IN-FEEDER-CST-LOAD-PICK-POSTURE",
                    Feeder.Name,
                    "Wafer 검출/Clamp 전 InputFeeder가 Unclamp 상태가 아닙니다. wafer=" +
                    wafer.WaferId + ". " +
                    Feeder.GetWaferFeederTransferState());
            }

            return 0;
        }

        private bool IsFeederAtCassetteLoadPositionComplete()
        {
            if (Feeder == null ||
                Feeder.FeederY == null ||
                !HasValidCassetteLoadTeaching())
                return false;

            double target =
                Feeder.CalculateWaferFeederCassetteLoadPosition(Options.SlotIndex);
            return IsFeederAtPositionComplete(target);
        }

        private bool HasValidCassetteLoadTeaching()
        {
            return Feeder != null &&
                   Feeder.Recipe != null &&
                   Feeder.Recipe.CassetteLoadPosition !=
                   Feeder.Recipe.AvoidPosition;
        }

        private bool IsFeederAtAvoidPositionComplete()
        {
            return Feeder != null &&
                   Feeder.Recipe != null &&
                   IsFeederAtPositionComplete(Feeder.Recipe.AvoidPosition) &&
                   Feeder.IsWaferFeederAvoidPositionCheck();
        }

        private bool IsFeederAtPositionComplete(double target)
        {
            if (Feeder == null || Feeder.FeederY == null)
                return false;

            double tolerance = ResolveFeederYInPositionTolerance();
            return Feeder.FeederY.IsServoOn &&
                   !Feeder.FeederY.IsAlarm &&
                   !Feeder.FeederY.IsMoving &&
                   Feeder.FeederY.IsInPosition &&
                   Math.Abs(Feeder.FeederY.ActualPosition - target) <= tolerance &&
                   Math.Abs(Feeder.FeederY.CommandPosition - target) <= tolerance;
        }

        private bool IsFeederSafeForFullCassetteLoadRestart()
        {
            if (!IsFeederAtAvoidPositionComplete() ||
                !Feeder.IsWaferFeederDown() ||
                Feeder.IsWaferFeederUp() ||
                !Feeder.IsWaferFeederUnclamp() ||
                Feeder.IsWaferFeederClamp() ||
                !Feeder.IsWaferFeederTransferDataEmpty())
            {
                return false;
            }

            return IsFeederRingStateConfirmed(false);
        }

        private double ResolveFeederYInPositionTolerance()
        {
            return Feeder != null &&
                   Feeder.FeederY != null &&
                   Feeder.FeederY.Config != null &&
                   Feeder.FeederY.Config.InPositionTolerance >= 0.0
                ? Feeder.FeederY.Config.InPositionTolerance
                : 0.05;
        }

        private bool CheckCassetteTransferReadyForEntry(
            InputCassetteUnit cassette,
            out string reason)
        {
            reason = string.Empty;
            if (cassette == null)
            {
                reason = "InputCassette=null";
                return false;
            }

            string unitReason;
            if (!cassette.CheckWaferCassetteTransferReady(
                TransferMode.Load,
                out unitReason))
            {
                reason = unitReason;
                return false;
            }

            string protrusionReason;
            if (!IsDigitalInputStateConfirmed(
                cassette.ProtrusionSensor,
                false,
                out protrusionReason))
            {
                reason =
                    "Input cassette protrusion sensor가 안전 OFF 상태가 아닙니다. " +
                    protrusionReason;
                return false;
            }

            if (!IsCassettePresenceConfirmed(cassette))
            {
                reason =
                    "Input cassette가 실제 센서에서 감지되지 않습니다. waferSize=" +
                    Options.WaferSize;
                return false;
            }

            return true;
        }

        private bool IsCassettePresenceConfirmed(InputCassetteUnit cassette)
        {
            if (cassette == null)
                return false;

            BaseDigitalInput first;
            BaseDigitalInput second;
            if (Options.WaferSize <= 8)
            {
                first = cassette.Wafer8CassetteCheck0;
                second = cassette.Wafer8CassetteCheck1;
            }
            else
            {
                first = cassette.Wafer12CassetteCheck0;
                second = cassette.Wafer12CassetteCheck1;
            }

            bool hasRealInput =
                IsRealDigitalInput(first) ||
                IsRealDigitalInput(second);
            if (!hasRealInput)
            {
                return IsSimulationDigitalInput(first) ||
                       IsSimulationDigitalInput(second);
            }

            return IsRealDigitalInputOn(first) ||
                   IsRealDigitalInputOn(second);
        }

        private bool IsFeederRingStateConfirmed(bool expected)
        {
            string reason;
            return IsDigitalInputStateConfirmed(
                Feeder != null ? Feeder.WaferFeederRingCheckSensor : null,
                expected,
                out reason);
        }

        private async Task<bool> WaitFeederRingStateConfirmedAsync(
            bool expected,
            int timeoutMs,
            CancellationToken ct)
        {
            BaseDigitalInput sensor =
                Feeder != null ? Feeder.WaferFeederRingCheckSensor : null;
            if (sensor == null)
                return false;

            if (IsSimulationDigitalInput(sensor))
                return true;

            int effectiveTimeoutMs = timeoutMs > 0 ? timeoutMs : 3000;
            const int stableTimeMs = 200;
            Stopwatch stopwatch = Stopwatch.StartNew();
            long stableStartMs = -1;

            while (stopwatch.ElapsedMilliseconds < effectiveTimeoutMs)
            {
                ct.ThrowIfCancellationRequested();

                int errorCode;
                bool read =
                    AjinIoScanService.TryReadHardwareInput(sensor, out errorCode);
                if (read && sensor.IsOn == expected)
                {
                    if (stableStartMs < 0)
                        stableStartMs = stopwatch.ElapsedMilliseconds;

                    if (stopwatch.ElapsedMilliseconds - stableStartMs >= stableTimeMs)
                        return true;
                }
                else
                {
                    stableStartMs = -1;
                }

                await Task.Delay(10, ct).ConfigureAwait(false);
            }

            int finalErrorCode;
            return AjinIoScanService.TryReadHardwareInput(
                       sensor,
                       out finalErrorCode) &&
                   sensor.IsOn == expected &&
                   stableStartMs >= 0 &&
                   stopwatch.ElapsedMilliseconds - stableStartMs >= stableTimeMs;
        }

        private static bool IsDigitalInputStateConfirmed(
            BaseDigitalInput input,
            bool expected,
            out string reason)
        {
            reason = string.Empty;
            if (input == null)
            {
                reason = "sensor=null";
                return false;
            }

            if (IsSimulationDigitalInput(input))
            {
                reason = "sensor=" + input.Name + ", mode=Simulation";
                return true;
            }

            int errorCode;
            if (!AjinIoScanService.TryReadHardwareInput(input, out errorCode))
            {
                reason =
                    "sensor=" + input.Name +
                    ", hardwareRead=False, errorCode=" + errorCode;
                return false;
            }

            bool actual = input.IsOn;
            reason =
                "sensor=" + input.Name +
                ", expected=" + expected +
                ", actual=" + actual;
            return actual == expected;
        }

        private static bool IsRealDigitalInputOn(BaseDigitalInput input)
        {
            if (!IsRealDigitalInput(input))
                return false;

            int errorCode;
            return AjinIoScanService.TryReadHardwareInput(input, out errorCode) &&
                   input.IsOn;
        }

        private static bool IsRealDigitalInput(BaseDigitalInput input)
        {
            return input != null && !IsSimulationDigitalInput(input);
        }

        private static bool IsSimulationDigitalInput(BaseDigitalInput input)
        {
            return input != null &&
                   input.Config != null &&
                   input.Config.IsSimulationMode;
        }

        private WaferMaterial ResolveCassetteWafer()
        {
            return MaterialStateService.GetWaferInCassette(Options.CassetteRole, Options.SlotIndex);
        }

        private bool IsInputStageEmpty(InputStageUnit stage)
        {
            if (stage == null)
                return false;

            return stage.CurrentWaferMaterial == null &&
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage) == null;
        }

        private bool IsSelectedSlotProcessReady(InputCassetteUnit cassette, int slotIndex)
        {
            if (cassette == null || slotIndex < 0)
                return false;

            WaferCassetteMaterial material = cassette.GetWaferMaterialCassette(
                InputCassetteUnit.ResolveCassetteLevel(Options.CassetteRole));
            if (material == null || material.Slots == null || slotIndex >= material.Slots.Count)
                return false;

            WaferSlotState state = material.Slots[slotIndex];
            if (state == null)
                return false;

            return state.Presence == SlotPresence.Exist &&
                   (state.Process == ProcessState.Ready || state.Process == ProcessState.Unknown);
        }

    }
}

