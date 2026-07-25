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
        MoveInputCassetteAvoidPosition,
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
                    // 인풋 카세트 AVOID 이동
                    case InputFeederUnloadToCassetteStep.MoveInputCassetteAvoidPosition:
                        return MoveInputCassetteAvoidPositionAsync(ct);
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

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("IN-FEEDER-WAFER-MISMATCH", "Material", "언로딩 계획과 현재 InputFeeder Wafer ID가 다릅니다. expected=" + Options.ExpectedWaferId + ", actual=" + wafer.WaferId);

            if (wafer.SourceCassetteRole != Options.CassetteRole || wafer.SourceSlotNumber != Options.SlotIndex)
                return Fail("IN-FEEDER-WAFER-SOURCE", "Material", "Input wafer는 원본 cassette/slot으로만 복귀할 수 있습니다. wafer=" + wafer.WaferId +
                    ", sourceRole=" + wafer.SourceCassetteRole + ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00") +
                    ", targetRole=" + Options.CassetteRole + ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));

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
            // To do: C4 - 언로드 복귀도 원본 레벨(Input1/Input2) 위치로 이동한다.
            int unloadLevel = InputCassetteUnit.ResolveCassetteLevel(Options.CassetteRole);
            double target = cassette.CalculateWaferCassetteSlotTargetPosition(unloadSlot, unloadLevel) + ResolveCassetteUnloadOffset(cassette);
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
            double slotPosition = cassette != null ? cassette.CalculateWaferCassetteSlotTargetPosition(unloadSlot, InputCassetteUnit.ResolveCassetteLevel(Options.CassetteRole)) : wafer.SourceCassetteSlotPosition;

            if (wafer.SourceCassetteRole != Options.CassetteRole || wafer.SourceSlotNumber != unloadSlot)
                return Fail("IN-FEEDER-MATERIAL-SOURCE", "Material", "물리 배출 후 Material 갱신 직전에 원본 cassette/slot 불일치가 확인되었습니다. wafer=" + wafer.WaferId +
                    ", sourceRole=" + wafer.SourceCassetteRole + ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00") +
                    ", targetRole=" + Options.CassetteRole + ", targetSlot=" + (unloadSlot + 1).ToString("00"));

            WaferMaterial targetWafer = MaterialStateService.GetWaferInCassette(Options.CassetteRole, unloadSlot);
            if (targetWafer != null && !string.Equals(targetWafer.WaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("IN-FEEDER-MATERIAL-TARGET", "Material", "물리 배출 후 대상 cassette slot에 다른 Material이 확인되어 데이터를 덮어쓰지 않습니다. movingWafer=" + wafer.WaferId +
                    ", targetWafer=" + targetWafer.WaferId + ", targetRole=" + Options.CassetteRole + ", targetSlot=" + (unloadSlot + 1).ToString("00"));

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
                cassette.UpdateWaferCassetteSlotState(
                    InputCassetteUnit.ResolveCassetteLevel(Options.CassetteRole),
                    ResolveUnloadSlotIndex(),
                    SlotPresence.Exist,
                    ProcessState.Done);
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

        private async Task<int> MoveInputCassetteAvoidPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputCassetteUnit cassette = ResolveCassette();
                if (cassette == null ||
                    cassette.InputLifterZ == null ||
                    cassette.Recipe == null)
                {
                    return Fail(
                        "IN-FEEDER-CST-MISSING",
                        "InputCassette",
                        "Input cassette unit, axis, or recipe is not available.");
                }

                // 카세트 이동 전 InputFeeder를 반드시 AVOID 위치로 이동한다.
                int result = await AwaitStepWithCancellationAsync(
                    Feeder.MoveToWaferFeederAvoidPosition(Options.FineMove),
                    ct).ConfigureAwait(false);

                if (result != 0)
                {
                    return Fail(
                        "IN-FEEDER-FINAL-AVOID-MOVE",
                        Feeder.Name,
                        "InputCassette 이동 전 InputFeeder Avoid 이동 실패. result=" +
                        result + ". " + Feeder.GetWaferFeederTransferState());
                }

                result = await WaitFeederYDoneAsync(
                    () => Feeder.IsWaferFeederInAvoidPosition(),
                    "InputFeeder final avoid before InputCassette avoid",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                if (Feeder.FeederY == null ||
                    Feeder.FeederY.IsMoving ||
                    !Feeder.IsWaferFeederAvoidPositionCheck())
                {
                    return Fail(
                        "IN-FEEDER-FINAL-AVOID-CHECK",
                        Feeder.Name,
                        "InputCassette 이동 전 InputFeeder Avoid Dog 확인 실패. " +
                        Feeder.GetWaferFeederTransferState());
                }

                double target = cassette.Recipe.AvoidPosition;

                // 연속 이송 최적화: 같은 Loader 작업 승인(lease) 안에서 곧바로 다음 슬롯 접근이 이어지는 경우
                // 리프터를 Avoid로 되돌리지 않고 현재 슬롯 높이에 둔다(슬롯 -> 슬롯 직행).
                // 안전 전제: 이 시점에 위에서 InputFeeder Avoid 복귀와 Avoid Dog를 이미 확인했고,
                //           Loader lease가 유지되는 동안에는 Picker 공정이 신규 진입할 수 없다.
                //           Picker X 이동은 리프터 Avoid를 요구하므로, lease를 놓기 전 마지막 이송에서는
                //           반드시 Avoid로 복귀해야 한다(호출자가 옵션으로 제어).
                if (Options != null && Options.KeepCassetteAtSlotForNextAccess)
                {
                    WriteLog("InputFeederUnloadToCassetteSequence",
                        "연속 이송을 위해 InputCassette 리프터를 Avoid로 되돌리지 않고 현재 슬롯 위치를 유지합니다. " +
                        BuildCassetteZState(cassette, target) + " - Skip");
                    CurrentStep = InputFeederUnloadToCassetteStep.Complete;
                    return 0;
                }

                result = await MoveCassetteZAndVerifyAsync(
                    cassette,
                    target,
                    "input cassette final avoid",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                if (cassette.InputLifterZ.IsMoving ||
                    cassette.InputLifterZ.IsAlarm ||
                    !cassette.IsWaferLifterZInAvoidPosition())
                {
                    return Fail(
                        "IN-FEEDER-CST-AVOID-CHECK",
                        cassette.Name,
                        "InputCassette 최종 Avoid 도착 확인 실패. " +
                        BuildCassetteZState(cassette, target));
                }

                CurrentStep = InputFeederUnloadToCassetteStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "IN-FEEDER-CST-AVOID-EX",
                    "InputFeederUnloadToCassetteSequence",
                    "InputCassette 최종 Avoid 이동 중 예외 발생. error=" +
                    ex.Message);
            }
            finally
            {
            }
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

            double target = cassette.CalculateWaferCassetteSlotTargetPosition(ResolveUnloadSlotIndex(), InputCassetteUnit.ResolveCassetteLevel(Options.CassetteRole));
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
            CurrentStep = InputFeederUnloadToCassetteStep.MoveInputCassetteAvoidPosition;
            return 0;
        }

        private async Task<int> MoveCassetteZAndVerifyAsync(InputCassetteUnit cassette, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await AwaitStepWithCancellationAsync(cassette.MoveWaferLifterZ(target, Options.FineMove, ct), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-FEEDER-CST-Z-MOVE", cassette.Name,
                        description + " 이동 명령 실패. target=" + target + ", result=" + result + ". " + BuildCassetteZState(cassette, target));

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
            if (cassetteWafer != null && WaferMaterialStateText.Normalize(cassetteWafer.State) != WaferMaterialState.Empty)
                return false;

            int cassetteLevel = InputCassetteUnit.ResolveCassetteLevel(Options.CassetteRole);
            WaferCassetteMaterial material = cassette.GetWaferMaterialCassette(cassetteLevel);
            if (material == null || material.Slots == null || slotIndex >= material.Slots.Count)
                return false;

            WaferSlotState state = material.Slots[slotIndex];
            if (state == null)
                return false;

            if (state.Presence == SlotPresence.Empty)
                return true;

            bool sameSourceWaferOnFeeder =
                feederWafer != null &&
                feederWafer.CurrentLocation != null &&
                feederWafer.CurrentLocation.Kind == MaterialLocationKind.InputFeeder &&
                feederWafer.SourceCassetteRole == Options.CassetteRole &&
                feederWafer.SourceSlotNumber == slotIndex;
            if (state.Presence == SlotPresence.Exist &&
                state.Process == ProcessState.Processing &&
                sameSourceWaferOnFeeder)
            {
                return true;
            }

            // 방어 조건: Unit의 slot projection은 휘발성이라 앱 재시작 직후 Unknown("정보 없음")이 된다.
            // Unknown을 점유로 오판하지 않도록, 영속 Material(단일 기준)이 아래를 모두 증명할 때만 허용한다.
            //  - 위에서 대상 cassette slot Material이 비어 있음을 이미 확인했다(cassetteWafer 없음/Empty).
            //  - 지금 피더가 든 wafer의 원본이 정확히 이 role/slot이다(sameSourceWaferOnFeeder).
            // 스캔으로 점유가 확인된 Exist는 위 조건 외에는 계속 차단된다.
            if (state.Presence == SlotPresence.Unknown && sameSourceWaferOnFeeder)
            {
                WriteLog("InputFeederUnloadToCassetteSequence",
                    "Unit slot projection이 Unknown이지만 영속 Material 기준으로 원본 슬롯이 비어 있어 언로드를 허용합니다. role=" +
                    Options.CassetteRole + ", slot=" + slotIndex +
                    ", feederWafer=" + (feederWafer != null ? feederWafer.WaferId : "") + " - Check");
                return true;
            }

            WriteLog("InputFeederUnloadToCassetteSequence",
                "Input cassette unload slot validation failed. role=" + Options.CassetteRole +
                ", level=" + cassetteLevel +
                ", slot=" + slotIndex +
                ", presence=" + state.Presence +
                ", process=" + state.Process +
                ", cassetteWafer=" + (cassetteWafer != null ? cassetteWafer.WaferId : "") +
                ", feederWafer=" + (feederWafer != null ? feederWafer.WaferId : "") +
                ", feederSourceRole=" + (feederWafer != null ? feederWafer.SourceCassetteRole.ToString() : "") +
                ", feederSourceSlot=" + (feederWafer != null ? feederWafer.SourceSlotNumber.ToString() : "") +
                ", sameSourceWaferOnFeeder=" + sameSourceWaferOnFeeder + " - Failed");
            return false;
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

