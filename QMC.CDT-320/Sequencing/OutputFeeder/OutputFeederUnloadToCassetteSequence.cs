using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederUnloadToCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckFeederBinData,
        CheckCassetteTargetSlot,
        VerifyFeederClamped,
        VerifyFeederLiftDown,
        VerifyBinDetected,
        MoveCassetteToBinSlot,
        MoveFeederCassetteUnloadPosition,
        PrepareFeederUnclamp,
        MoveFeederAvoidPosition,
        VerifyBinReleasedToCassette,
        MoveMaterialDataToCassette,
        UpdateCassetteData,
        Complete,
        Error
    }

    internal sealed class OutputFeederUnloadToCassetteSequence : OutputFeederSequenceBase<OutputFeederUnloadToCassetteStep>
    {
        public OutputFeederUnloadToCassetteSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.UnloadToCassette, "OutputFeederUnloadToCassetteSequence")
        {
        }

        protected override OutputFeederUnloadToCassetteStep IdleStep { get { return OutputFeederUnloadToCassetteStep.Idle; } }
        protected override OutputFeederUnloadToCassetteStep InitialStep { get { return OutputFeederUnloadToCassetteStep.CheckUnit; } }
        protected override OutputFeederUnloadToCassetteStep CompleteStep { get { return OutputFeederUnloadToCassetteStep.Complete; } }
        protected override OutputFeederUnloadToCassetteStep ErrorStep { get { return OutputFeederUnloadToCassetteStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputFeederUnloadToCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederUnloadToCassetteStep.CheckTransferReady));

                    // 이송 준비 확인
                    case OutputFeederUnloadToCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    // 피더 BIN 데이터 확인
                    case OutputFeederUnloadToCassetteStep.CheckFeederBinData:
                        return Task.FromResult(CheckFeederBinData());

                    // 카세트 대상 슬롯 확인
                    case OutputFeederUnloadToCassetteStep.CheckCassetteTargetSlot:
                        return Task.FromResult(CheckCassetteTargetSlot());

                    // 피더 클램프 검증
                    case OutputFeederUnloadToCassetteStep.VerifyFeederClamped:
                        return Task.FromResult(VerifyFeederClamped());

                    // 피더 리프트 다운 검증
                    case OutputFeederUnloadToCassetteStep.VerifyFeederLiftDown:
                        return Task.FromResult(VerifyFeederLiftDown());

                    // BIN 감지 검증
                    case OutputFeederUnloadToCassetteStep.VerifyBinDetected:
                        return VerifyBinDetectedAsync(ct);

                    // 카세트 BIN 슬롯 이동
                    case OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlot:
                        return MoveCassetteToBinSlotAsync(ct);

                    // 피더 카세트 언로드 위치 이동
                    case OutputFeederUnloadToCassetteStep.MoveFeederCassetteUnloadPosition:
                        return MoveFeederCassetteUnloadPositionAsync(ct);

                    // 피더 언클램프 준비
                    case OutputFeederUnloadToCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    // 피더 어보이드 위치 이동
                    case OutputFeederUnloadToCassetteStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);

                    // BIN 해제로 카세트 검증
                    case OutputFeederUnloadToCassetteStep.VerifyBinReleasedToCassette:
                        return VerifyBinReleasedToCassetteAsync(ct);

                    // 자재 데이터를 카세트로 이동
                    case OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette:
                        return Task.FromResult(MoveMaterialDataToCassette());

                    // 카세트 데이터 갱신
                    case OutputFeederUnloadToCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-CST-UNLOAD-EX", Name, "Unload to cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            string readyReason;
            if (!Feeder.CheckFeederCassetteReady(Options.Side, Options.SlotIndex, TransferMode.Unload, out readyReason))
                return Fail("OUT-FEEDER-CST-UNLOAD-READY", Feeder.Name, "Output feeder cassette unload is not ready. " + readyReason);

            CurrentStep = OutputFeederUnloadToCassetteStep.CheckFeederBinData;
            return 0;
        }

        private int CheckFeederBinData()
        {
            return CheckCassetteSlotReadyForUnload(OutputFeederUnloadToCassetteStep.CheckCassetteTargetSlot);
        }

        private int CheckCassetteTargetSlot()
        {
            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (cassetteWafer != null)
                return Fail("OUT-FEEDER-CST-SLOT-OCCUPIED", "Material",
                    "Output cassette target slot became occupied before unload. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", waferId=" + cassetteWafer.WaferId +
                    ", state=" + cassetteWafer.State + ", loc=" + cassetteWafer.CurrentLocation);

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyFeederClamped;
            return 0;
        }

        private int VerifyFeederClamped()
        {
            if (Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-CLAMP-CHECK", Feeder.Name,
                    "Output feeder must already be clamped before cassette unload move. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyFeederLiftDown;
            return 0;
        }

        private int VerifyFeederLiftDown()
        {
            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "Output feeder must already be down before cassette unload. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyBinDetected;
            return 0;
        }

        private async Task<int> VerifyBinDetectedAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data disappeared before cassette unload.");

            if (!IsHardwareBypass())
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("OUT-FEEDER-CST-UNLOAD-RING-DETECT", Feeder.Name, "Output feeder ring was not detected before cassette unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlot;
            return 0;
        }

        private async Task<int> MoveCassetteToBinSlotAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Cassette == null)
                return Fail("OUT-FEEDER-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");

            TargetCassette targetCassette = ResolveOutputTargetCassette();
            double targetPosition = Cassette.CalculateBinCassetteSlotTargetPosition(targetCassette, Options.SlotIndex);

            string readyReason;
            if (!Cassette.CheckBinLifterZMoveReady(out readyReason))
                return Fail("OUT-FEEDER-CST-MOVE-READY", Cassette.Name,
                    "Output cassette is not ready to move before feeder unload. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", target=" + targetCassette +
                    ", targetPosition=" + targetPosition +
                    ". " + readyReason);

            try
            {
                int result = await AwaitStepWithCancellationAsync(
                    Cassette.PrepareBinCassetteForFeederLoad(targetCassette, Options.SlotIndex, ResolveTimeout(), Options.FineMove),
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-FEEDER-CST-SLOT-MOVE", Cassette.Name,
                        "Output cassette slot move failed before feeder unload. role=" + ResolveOutputCassetteRole() +
                        ", slot=" + Options.SlotIndex + ", target=" + targetCassette +
                        ", targetPosition=" + targetPosition + ", result=" + result +
                        ". " + Cassette.DescribeOutputLifterZState(targetPosition));
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-CST-SLOT-MOVE", Cassette.Name,
                    "Output cassette slot move exception before feeder unload. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", target=" + targetCassette +
                    ", targetPosition=" + targetPosition +
                    ", message=" + ex.Message + ". " + Cassette.DescribeOutputLifterZState(targetPosition));
            }

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveFeederCassetteUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederCassetteUnloadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederCassetteUnloadPosition(Options.Side, Options.SlotIndex, Options.FineMove), "cassette unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInCassetteUnloadPosition(Options.Side), "cassette unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadToCassetteStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name,
                    "Output feeder unclamp command failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name,
                    "Output feeder unclamp failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "cassette unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "cassette unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyBinReleasedToCassette;
            return 0;
        }

        private async Task<int> VerifyBinReleasedToCassetteAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data disappeared before cassette material move.");

            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("OUT-FEEDER-CST-RING", Feeder.Name, "Output feeder ring remained after cassette unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette;
            return 0;
        }

        private int MoveMaterialDataToCassette()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output feeder wafer data was not found for cassette material move.");

            MaterialStateService.PutWaferInCassette(
                wafer.WaferId,
                ResolveOutputCassetteRole(),
                Options.SlotIndex,
                wafer.CassetteLotId,
                wafer.SourceCassetteSlotPosition,
                WaferMaterialState.Finish);
            Feeder.ClearFeederMaterialState();
            CurrentStep = OutputFeederUnloadToCassetteStep.UpdateCassetteData;
            return 0;
        }

        private int UpdateCassetteData()
        {
            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (cassetteWafer == null)
                return Fail("OUT-FEEDER-CST-DATA-MISSING", "Material", "Output cassette data was not created after feeder unload. role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-CLEAR", "Material", "Output feeder data was not cleared after cassette unload. waferId=" + cassetteWafer.WaferId);

            Context.Bus.Set("OutputFeederEmpty");
            Context.Bus.Set("OutputCassetteSlotUpdated");
            CurrentStep = OutputFeederUnloadToCassetteStep.Complete;
            return 0;
        }
    }
}

