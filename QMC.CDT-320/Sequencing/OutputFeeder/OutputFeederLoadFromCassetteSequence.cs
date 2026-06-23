using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederLoadFromCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckOutputStageEmpty,
        CheckCassetteBinData,
        MoveCassetteToBinSlot,
        PrepareFeederUnclamp,
        PrepareFeederLiftDown,
        MoveFeederCassetteLoadPosition,
        VerifyFeederEmpty,
        VerifyBinDetected,
        ClampFeederBin,
        MoveFeederAvoidPosition,
        MoveMaterialDataToFeeder,
        UpdateCassetteData,
        Complete,
        Error
    }

    internal sealed class OutputFeederLoadFromCassetteSequence : OutputFeederSequenceBase<OutputFeederLoadFromCassetteStep>
    {
        public OutputFeederLoadFromCassetteSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.LoadFromCassette, "OutputFeederLoadFromCassetteSequence")
        {
        }

        protected override OutputFeederLoadFromCassetteStep IdleStep { get { return OutputFeederLoadFromCassetteStep.Idle; } }
        protected override OutputFeederLoadFromCassetteStep InitialStep { get { return OutputFeederLoadFromCassetteStep.CheckUnit; } }
        protected override OutputFeederLoadFromCassetteStep CompleteStep { get { return OutputFeederLoadFromCassetteStep.Complete; } }
        protected override OutputFeederLoadFromCassetteStep ErrorStep { get { return OutputFeederLoadFromCassetteStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputFeederLoadFromCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederLoadFromCassetteStep.CheckTransferReady));

                    // 이송 준비 확인
                    case OutputFeederLoadFromCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    // 아웃풋 스테이지 비어있음 확인
                    case OutputFeederLoadFromCassetteStep.CheckOutputStageEmpty:
                        return Task.FromResult(CheckTargetOutputStageEmpty());

                    // 카세트 BIN 데이터 확인
                    case OutputFeederLoadFromCassetteStep.CheckCassetteBinData:
                        return Task.FromResult(CheckCassetteBinData());

                    // 카세트 BIN 슬롯 이동
                    case OutputFeederLoadFromCassetteStep.MoveCassetteToBinSlot:
                        return MoveCassetteToBinSlotAsync(ct);

                    // 피더 언클램프 준비
                    case OutputFeederLoadFromCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    // 피더 리프트 다운 준비
                    case OutputFeederLoadFromCassetteStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);

                    // 피더 카세트 로드 위치 이동
                    case OutputFeederLoadFromCassetteStep.MoveFeederCassetteLoadPosition:
                        return MoveFeederCassetteLoadPositionAsync(ct);

                    // 피더 비어있음 검증
                    case OutputFeederLoadFromCassetteStep.VerifyFeederEmpty:
                        return Task.FromResult(VerifyFeederEmpty());

                    // BIN 감지 검증
                    case OutputFeederLoadFromCassetteStep.VerifyBinDetected:
                        return VerifyBinDetectedAsync(ct);

                    // 피더 BIN 클램프
                    case OutputFeederLoadFromCassetteStep.ClampFeederBin:
                        return ClampFeederBinAsync(ct);

                    // 피더 어보이드 위치 이동
                    case OutputFeederLoadFromCassetteStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);

                    // 자재 데이터를 피더로 이동
                    case OutputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder:
                        return Task.FromResult(MoveMaterialDataToFeeder());

                    // 카세트 데이터 갱신
                    case OutputFeederLoadFromCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-CST-LOAD-EX", Name, "Load from cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            if (Options.SlotIndex < 0)
                return Fail("OUT-FEEDER-CST-SLOT", Feeder.Name, "Output cassette source slot is invalid. slot=" + Options.SlotIndex);

            string teachingReason;
            if (!Feeder.ValidateBinFeederYTeachingComplete(Options.Side, out teachingReason))
                return Fail("OUT-FEEDER-TEACHING", Feeder.Name, "OutputFeederY teaching is not complete. " + teachingReason);

            string moveReason;
            if (!Feeder.CheckBinFeederYMoveReady(out moveReason))
                return Fail("OUT-FEEDER-MOVE-READY", Feeder.Name, "OutputFeederY is not ready to move. " + moveReason);

            CurrentStep = OutputFeederLoadFromCassetteStep.CheckOutputStageEmpty;
            return 0;
        }

        private int CheckTargetOutputStageEmpty()
        {
            MaterialLocationKind targetLocation = Options.Side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;

            WaferMaterial targetStageWafer = MaterialStateService.GetWaferAtLocation(targetLocation);
            if (targetStageWafer != null)
            {
                string stageName = Options.Side == BinSide.Ng ? "Output NGStage" : "Output GoodStage";
                return Fail(
                    "OUT-STAGE-DATA-OCCUPIED",
                    "Material",
                    stageName + " must be empty before cassette to feeder load. side=" + Options.Side +
                    ", waferId=" + targetStageWafer.WaferId +
                    ", state=" + targetStageWafer.State +
                    ", loc=" + targetStageWafer.CurrentLocation);
            }

            CurrentStep = OutputFeederLoadFromCassetteStep.CheckCassetteBinData;
            return 0;
        }

        private int CheckCassetteBinData()
        {
            return CheckCassetteSlotReadyForLoad(OutputFeederLoadFromCassetteStep.MoveCassetteToBinSlot);
        }

        private async Task<int> MoveCassetteToBinSlotAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Cassette == null)
                return Fail("OUT-FEEDER-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");

            int result = await AwaitStepWithCancellationAsync(
                Cassette.PrepareBinCassetteForFeederLoad(ResolveOutputTargetCassette(), Options.SlotIndex, ResolveTimeout(), Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-CST-SLOT-MOVE", Cassette.Name,
                    "Output cassette slot move failed before feeder load. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", target=" + ResolveOutputTargetCassette() +
                    ", result=" + result);

            CurrentStep = OutputFeederLoadFromCassetteStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!Feeder.IsFeederUnclamped())
            {
                int unclamp = await Feeder.SetFeederClampAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
                if (unclamp != 0)
                    return Fail("OUT-FEEDER-PREP-UNCLAMP", Feeder.Name, "Output feeder unclamp preparation command failed. result=" + unclamp + ", " + Feeder.DescribeFeederCylinderState());

                if (!Feeder.IsFeederUnclamped())
                    return Fail("OUT-FEEDER-PREP-UNCLAMP", Feeder.Name, "Output feeder unclamp preparation failed. result=" + unclamp + ", " + Feeder.DescribeFeederCylinderState());
            }

            CurrentStep = OutputFeederLoadFromCassetteStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!Feeder.IsFeederDown())
            {
                int down = await Feeder.SetFeederUpDownAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
                if (down != 0)
                    return Fail("OUT-FEEDER-PREP-DOWN", Feeder.Name, "Output feeder lift down preparation command failed. result=" + down + ", " + Feeder.DescribeFeederCylinderState());

                if (!Feeder.IsFeederDown())
                    return Fail("OUT-FEEDER-PREP-DOWN", Feeder.Name, "Output feeder lift down preparation failed. result=" + down + ", " + Feeder.DescribeFeederCylinderState());
            }

            CurrentStep = OutputFeederLoadFromCassetteStep.MoveFeederCassetteLoadPosition;
            return 0;
        }

        private int VerifyFeederEmpty()
        {
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material", "Output feeder data must still be empty before cassette bin detect.");

            CurrentStep = OutputFeederLoadFromCassetteStep.VerifyBinDetected;
            return 0;
        }

        private async Task<int> MoveFeederCassetteLoadPositionAsync(CancellationToken ct)
        {
            int visionAvoid = await EnsureOutputVisionXAvoidForFeederMoveAsync(ct).ConfigureAwait(false);
            if (visionAvoid != 0)
                return visionAvoid;

            int pickerClear = CheckPickersClearForOutputTransport("before cassette to feeder load");
            if (pickerClear != 0)
                return pickerClear;

            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederCassetteLoadPosition(Options.Side, Options.SlotIndex, Options.FineMove), "cassette load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInCassetteLoadPosition(Options.Side), "cassette load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadFromCassetteStep.VerifyFeederEmpty;
            return 0;
        }

        private async Task<int> EnsureOutputVisionXAvoidForFeederMoveAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Stage == null)
                    return Fail("OUT-FEEDER-STAGE-MISSING", "OutputStage", "OutputFeederY 이동 전 OutputStageUnit을 확인할 수 없습니다.");

                if (Stage.IsVisionXInAvoidPosition())
                    return 0;

                int result = await Stage.MoveVisionXToAvoidAndVerifyAsync(
                    ResolveTimeout(),
                    Options.FineMove,
                    ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail(
                        "OUT-FEEDER-VISION-X-AVOID",
                        "OutputStage",
                        "OutputFeederY 이동 전 OutputVisionX Avoid 이동 실패. result=" + result +
                        ", " + Stage.BuildStageAxisState(BinStageAxis.VisionX, Stage.Recipe.VisionX.AvoidPosition));
                }

                if (!Stage.IsVisionXInAvoidPosition())
                {
                    return Fail(
                        "OUT-FEEDER-VISION-X-AVOID-CHECK",
                        "OutputStage",
                        "OutputFeederY 이동 전 OutputVisionX Avoid 최종 확인 실패. " +
                        Stage.BuildStageAxisState(BinStageAxis.VisionX, Stage.Recipe.VisionX.AvoidPosition));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "OUT-FEEDER-VISION-X-AVOID-EX",
                    "OutputStage",
                    "OutputFeederY 이동 전 OutputVisionX Avoid 확인 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ClampFeederBinAsync(CancellationToken ct)
        {
            int result = await Feeder.SetFeederClampAsync(true, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp final check failed after cassette load. side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederLoadFromCassetteStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "cassette load avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "cassette load avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private async Task<int> VerifyBinDetectedAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveCassetteWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-CST-DATA-MISSING", "Material", "Output cassette wafer data disappeared before feeder material move. role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            if (!IsHardwareBypass())
            {
                bool detected = await Feeder.WaitFeederRingState(true, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("OUT-FEEDER-RING", Feeder.Name, "Output feeder ring was not detected after cassette load. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederLoadFromCassetteStep.ClampFeederBin;
            return 0;
        }

        private int MoveMaterialDataToFeeder()
        {
            WaferMaterial wafer = ResolveCassetteWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output cassette wafer data was not found for feeder material move. role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = MaterialLocationKind.OutputFeeder }, WaferMaterialState.WorkReady);
            Feeder.UpdateFeederMaterialState(MaterialState.Occupied);
            Context.Bus.Set("OutputFeederOccupied");
            CurrentStep = OutputFeederLoadFromCassetteStep.UpdateCassetteData;
            return 0;
        }

        private int UpdateCassetteData()
        {
            WaferMaterial feederWafer = ResolveFeederWafer();
            if (feederWafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data was not found after cassette load material move.");

            WaferMaterial sourceWafer = ResolveCassetteWafer();
            if (sourceWafer != null &&
                string.Equals(sourceWafer.WaferId, feederWafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("OUT-FEEDER-CST-UPDATE", "Material", "Output cassette slot data was not cleared after feeder load. waferId=" + feederWafer.WaferId);

            Context.Bus.Set("OutputCassetteSlotUpdated");
            CurrentStep = OutputFeederLoadFromCassetteStep.Complete;
            return 0;
        }
    }
}

