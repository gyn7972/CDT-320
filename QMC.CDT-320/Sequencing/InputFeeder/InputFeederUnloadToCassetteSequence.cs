using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederUnloadToCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckFeederWaferData,
        CheckCassetteSlotEmpty,
        VerifyFeederClamp,
        VerifyFeederLiftDown,
        VerifyWaferDetected,
        MoveCassetteToUnloadOffsetPosition,
        MoveFeederUnloadPosition,
        PrepareFeederUnclamp,
        VerifyWaferCleared,
        MoveMaterialDataToCassette,
        UpdateCassetteData,
        MoveFeederPostUnloadPosition,
        MoveCassetteToSlotPosition,
        VerifyTransferData,
        Complete,
        Error
    }

    internal sealed class InputFeederUnloadToCassetteSequence : InputFeederSequenceBase<InputFeederUnloadToCassetteStep>
    {
        public InputFeederUnloadToCassetteSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.UnloadToCassette, "InputFeederUnloadToCassetteSequence")
        {
        }

        protected override InputFeederUnloadToCassetteStep IdleStep { get { return InputFeederUnloadToCassetteStep.Idle; } }
        protected override InputFeederUnloadToCassetteStep InitialStep { get { return InputFeederUnloadToCassetteStep.CheckUnit; } }
        protected override InputFeederUnloadToCassetteStep CompleteStep { get { return InputFeederUnloadToCassetteStep.Complete; } }
        protected override InputFeederUnloadToCassetteStep ErrorStep { get { return InputFeederUnloadToCassetteStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputFeederUnloadToCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederUnloadToCassetteStep.CheckTransferReady));
                    // 이송 준비 확인
                    case InputFeederUnloadToCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    // 피더 웨이퍼 데이터 확인
                    case InputFeederUnloadToCassetteStep.CheckFeederWaferData:
                        return Task.FromResult(CheckFeederWaferData());
                    // 카세트 슬롯 비어있음 확인
                    case InputFeederUnloadToCassetteStep.CheckCassetteSlotEmpty:
                        return Task.FromResult(CheckCassetteSlotEmpty());
                    // 카세트로 언로드 오프셋 위치 이동
                    case InputFeederUnloadToCassetteStep.MoveCassetteToUnloadOffsetPosition:
                        return MoveCassetteToUnloadOffsetPositionAsync(ct);
                    // 피더 클램프 검증
                    case InputFeederUnloadToCassetteStep.VerifyFeederClamp:
                        return Task.FromResult(VerifyFeederClamp());
                    // 피더 리프트 다운 검증
                    case InputFeederUnloadToCassetteStep.VerifyFeederLiftDown:
                        return Task.FromResult(VerifyFeederLiftDown());
                    // 웨이퍼 감지 검증
                    case InputFeederUnloadToCassetteStep.VerifyWaferDetected:
                        return VerifyWaferDetectedAsync(ct);
                    // 피더 언로드 위치 이동
                    case InputFeederUnloadToCassetteStep.MoveFeederUnloadPosition:
                        return MoveFeederUnloadPositionAsync(ct);
                    // 피더 언클램프 준비
                    case InputFeederUnloadToCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    // 웨이퍼 클리어 검증
                    case InputFeederUnloadToCassetteStep.VerifyWaferCleared:
                        return VerifyWaferClearedAsync(ct);
                    // 자재 데이터를 카세트로 이동
                    case InputFeederUnloadToCassetteStep.MoveMaterialDataToCassette:
                        return Task.FromResult(MoveMaterialDataToCassette());
                    // 카세트 데이터 갱신
                    case InputFeederUnloadToCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());
                    // 피더 언로드 후 위치 이동
                    case InputFeederUnloadToCassetteStep.MoveFeederPostUnloadPosition:
                        return MoveFeederPostUnloadPositionAsync(ct);
                    // 카세트로 슬롯 위치 이동
                    case InputFeederUnloadToCassetteStep.MoveCassetteToSlotPosition:
                        return MoveCassetteToSlotPositionAsync(ct);
                    // 이송 데이터 검증
                    case InputFeederUnloadToCassetteStep.VerifyTransferData:
                        return Task.FromResult(VerifyTransferData());
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-CST-UNLOAD-STEP-EX", "InputFeederUnloadToCassetteSequence", "Unload to cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            string feederReason;
            if (!Feeder.CheckWaferFeederMoveReady(out feederReason))
                return Fail("IN-FEEDER-CST-UNLOAD-READY", Feeder.Name, "Input feeder is not move ready. " + feederReason);

            if (!Feeder.HasWaferOnFeeder())
                return Fail("IN-FEEDER-WAFER-MISSING", Feeder.Name, "InputFeeder must have wafer before cassette unload.");

            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            string cassetteReason;
            if (!IsHardwareBypass() && !cassette.CheckWaferCassetteTransferReady(TransferMode.Unload, out cassetteReason))
                return Fail("IN-FEEDER-CST-SENSOR", cassette.Name, "Input cassette is not detected or not ready for unload. " + cassetteReason);

            if (!cassette.CheckWaferCassetteMoveReady(out cassetteReason))
                return Fail("IN-FEEDER-CST-MOVE-READY", cassette.Name, "Input cassette lifter is not move ready. " + cassetteReason);

            CurrentStep = InputFeederUnloadToCassetteStep.CheckFeederWaferData;
            return 0;
        }

        private int CheckFeederWaferData()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-WAFER-DATA", "Material", "InputFeeder wafer data was not found before cassette unload.");

            CurrentStep = InputFeederUnloadToCassetteStep.CheckCassetteSlotEmpty;
            return 0;
        }

        private int CheckCassetteSlotEmpty()
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            int unloadSlot = ResolveUnloadSlotIndex();
            if (!IsUnloadSlotEmpty(cassette, unloadSlot))
                return Fail("IN-FEEDER-CST-SLOT-OCCUPIED", cassette.Name, "Unload cassette slot must be empty. slot=" + unloadSlot);

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyFeederClamp;
            return 0;
        }

        private async Task<int> MoveCassetteToUnloadOffsetPositionAsync(CancellationToken ct)
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            int unloadSlot = ResolveUnloadSlotIndex();
            double target = cassette.CalculateWaferCassetteSlotTargetPosition(unloadSlot) + ResolveCassetteUnloadOffset(cassette);
            int result = await MoveCassetteZAndVerifyAsync(cassette, target, "cassette unload offset", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederUnloadToCassetteStep.MoveFeederUnloadPosition;
            return 0;
        }

        private int VerifyFeederClamp()
        {
            if (!Feeder.IsWaferFeederClamp())
                return Fail("IN-FEEDER-CLAMP-CHECK", Feeder.Name,
                    "WaferFeeder must already be clamped before cassette unload. " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyFeederLiftDown;
            return 0;
        }

        private int VerifyFeederLiftDown()
        {
            if (!Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "WaferFeeder must already be down before cassette unload. " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyWaferDetected;
            return 0;
        }

        private async Task<int> VerifyWaferDetectedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-WAFER-DATA-MISSING", "Material", "InputFeeder wafer data disappeared before cassette unload.");

            if (!IsHardwareBypass())
            {
                bool detected = await Feeder.WaitWaferFeederRingState(true, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("IN-FEEDER-CST-UNLOAD-WAFER-SENSOR", Feeder.Name, "Wafer sensor timeout or data/sensor mismatch before cassette unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = InputFeederUnloadToCassetteStep.MoveCassetteToUnloadOffsetPosition;
            return 0;
        }

        private async Task<int> MoveFeederUnloadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederCassetteUnloadPosition(ResolveUnloadSlotIndex(), Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-CST-UNLOAD-POS", Feeder.Name,
                    "WaferFeeder cassette unload position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            int unloadSlot = ResolveUnloadSlotIndex();
            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInCassetteUnloadPosition(unloadSlot),
                "WaferFeeder cassette unload position slot=" + unloadSlot,
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederUnloadToCassetteStep.PrepareFeederUnclamp;
            return 0;
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

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyWaferCleared;
            return 0;
        }

        private async Task<int> VerifyWaferClearedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsHardwareBypass())
            {
                bool cleared = await Feeder.WaitWaferFeederRingState(false, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("IN-FEEDER-CST-UNLOAD-RING", Feeder.Name, "WaferFeeder ring remained after cassette unload.");
            }

            CurrentStep = InputFeederUnloadToCassetteStep.MoveMaterialDataToCassette;
            return 0;
        }

        private int MoveMaterialDataToCassette()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-MATERIAL-CST", "Material", "InputFeeder wafer data was not found for cassette material move.");

            InputCassetteUnit cassette = ResolveCassette();
            int unloadSlot = ResolveUnloadSlotIndex(wafer);
            double slotPosition = cassette != null ? cassette.CalculateWaferCassetteSlotTargetPosition(unloadSlot) : wafer.SourceCassetteSlotPosition;

            MaterialStateService.PutWaferInCassette(
                wafer.WaferId,
                Options.CassetteRole,
                unloadSlot,
                wafer.CassetteLotId,
                slotPosition,
                WaferMaterialState.Finish);

            Feeder.ClearCurrentWaferMaterial();
            Context.Bus.Set("InputFeederEmpty");
            CurrentStep = InputFeederUnloadToCassetteStep.UpdateCassetteData;
            return 0;
        }

        private int UpdateCassetteData()
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette != null)
            {
                cassette.UpdateWaferCassetteSlotState(ResolveUnloadSlotIndex(), SlotPresence.Exist, ProcessState.Done);
                if (cassette.IsInputCassetteProcessComplete())
                {
                    cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
                    Context.RequestOperatorMessage(
                        "입력 카세트 교체",
                        "입력 카세트의 모든 웨이퍼 작업이 완료되었습니다.\r\n카세트를 교체한 뒤 필요한 작업을 진행하세요.");
                }
            }

            CurrentStep = InputFeederUnloadToCassetteStep.MoveFeederPostUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederPostUnloadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            bool exchange = Options.PostUnloadMove == InputFeederPostUnloadMove.Exchange;
            int result = await AwaitStepWithCancellationAsync(
                exchange
                    ? Feeder.MoveToWaferFeederExchangePosition(Options.FineMove)
                    : Feeder.MoveToWaferFeederAvoidPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-POST-UNLOAD-MOVE", Feeder.Name,
                    "WaferFeeder post unload position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => exchange ? Feeder.IsWaferFeederInExchangePosition() : Feeder.IsWaferFeederInAvoidPosition(),
                "WaferFeeder post unload position target=" + Options.PostUnloadMove,
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederUnloadToCassetteStep.MoveCassetteToSlotPosition;
            return 0;
        }

        private async Task<int> MoveCassetteToSlotPositionAsync(CancellationToken ct)
        {
            if (!Options.ReturnCassetteToUnloadSlotAfterUnload)
            {
                CurrentStep = InputFeederUnloadToCassetteStep.VerifyTransferData;
                await Task.CompletedTask.ConfigureAwait(false);
                return 0;
            }

            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            double target = cassette.CalculateWaferCassetteSlotTargetPosition(ResolveUnloadSlotIndex());
            int result = await MoveCassetteZAndVerifyAsync(cassette, target, "cassette final slot", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyTransferData;
            return 0;
        }

        private int VerifyTransferData()
        {
            if (Feeder.CurrentWaferMaterial != null || MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder) != null)
                return Fail("IN-FEEDER-DATA-CLEAR", "Material", "InputFeeder wafer data remained after cassette unload.");

            int unloadSlot = ResolveUnloadSlotIndex();
            WaferMaterial cassetteWafer = MaterialStateService.GetWaferInCassette(Options.CassetteRole, unloadSlot);
            if (cassetteWafer == null)
                return Fail("IN-FEEDER-CST-DATA", "Material", "Cassette wafer data was not found after feeder unload. slot=" + unloadSlot);

            Context.Bus.Set("InputCassetteSlotUpdated");
            CurrentStep = InputFeederUnloadToCassetteStep.Complete;
            return 0;
        }

        private async Task<int> MoveCassetteZAndVerifyAsync(InputCassetteUnit cassette, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await AwaitStepWithCancellationAsync(cassette.MoveWaferLifterZ(target, Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-FEEDER-CST-Z-MOVE", cassette.Name,
                        description + " 이동 명령 실패. target=" + target + ", result=" + result + ". " + BuildCassetteZState(cassette, target));

                AxisMoveWaitResult waitResult = await cassette.WaitWaferLifterZMoveDoneInPosition(target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("IN-FEEDER-CST-Z", waitResult), cassette.Name,
                        description + " 이동 완료/위치 확인 실패. " +
                        FormatAxisMoveWaitResult(waitResult, BuildCassetteZState(cassette, target)));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-CST-Z-EX", cassette != null ? cassette.Name : "InputCassette",
                    description + " 이동 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
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

        private bool IsUnloadSlotEmpty(InputCassetteUnit cassette, int slotIndex)
        {
            if (cassette == null || slotIndex < 0)
                return false;

            WaferMaterial feederWafer = ResolveFeederWafer();
            WaferMaterial cassetteWafer = MaterialStateService.GetWaferInCassette(Options.CassetteRole, slotIndex);
            if (cassetteWafer != null && feederWafer != null && string.Equals(cassetteWafer.WaferId, feederWafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return true;

            WaferCassetteMaterial material = cassette.GetWaferMaterialCassette();
            if (material == null || material.Slots == null || slotIndex >= material.Slots.Count)
                return false;

            WaferSlotState state = material.Slots[slotIndex];
            if (state == null)
                return false;

            return state.Presence == SlotPresence.Empty ||
                   (state.Presence == SlotPresence.Exist && state.Process == ProcessState.Processing && feederWafer != null) ||
                   cassetteWafer == null;
        }

        private double ResolveCassetteUnloadOffset(InputCassetteUnit cassette)
        {
            return cassette != null && cassette.Config != null ? cassette.Config.UnloadingPositionOffset : 0.0;
        }

        private InputCassetteUnit ResolveCassette()
        {
            return Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
        }

        private WaferMaterial ResolveFeederWafer()
        {
            return Feeder.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
        }

        private int ResolveUnloadSlotIndex()
        {
            return ResolveUnloadSlotIndex(ResolveFeederWafer());
        }

        private int ResolveUnloadSlotIndex(WaferMaterial wafer)
        {
            if (wafer != null &&
                wafer.SourceCassetteRole == Options.CassetteRole &&
                wafer.SourceSlotNumber >= 0)
            {
                return wafer.SourceSlotNumber;
            }

            return Options.SlotIndex;
        }

        private bool IsHardwareBypass()
        {
            AppSettings settings = AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (Context.Controller != null && Context.Controller.GlobalDryRun) ||
                   (Feeder.Setup != null && Feeder.Setup.IsSimulationMode) ||
                   (Feeder.Config != null && Feeder.Config.bDryRun);
        }
    }
}

