using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStagePrepareUnloadStep
    {
        Idle,
        CheckUnit,
        CheckTargetSide,
        EnsureOutputFeederSafeBeforeStageMove,
        MoveOppositeStageToAvoid,
        CheckOppositeStageAvoid,
        MoveTargetStageZToAvoid,
        CheckTargetStageZAvoid,
        MoveTargetStageYToUnload,
        CheckTargetStageYUnload,
        MoveTargetStageZToUnload,
        CheckTargetStageZUnload,
        // 재개 호환을 위해 기존 Step 이름을 유지한다. 실린더 준비는 실제 인출 시퀀스에서 수행한다.
        EnsureTargetStageUnloadReadyState,
        Complete,
        Error
    }

    internal sealed class OutputStagePrepareUnloadSequence : OutputStageSequenceBase<OutputStagePrepareUnloadStep>
    {
        public OutputStagePrepareUnloadSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.PrepareUnload, "OutputStagePrepareUnloadSequence")
        {
        }

        protected override OutputStagePrepareUnloadStep IdleStep { get { return OutputStagePrepareUnloadStep.Idle; } }
        protected override OutputStagePrepareUnloadStep InitialStep { get { return OutputStagePrepareUnloadStep.CheckUnit; } }
        protected override OutputStagePrepareUnloadStep CompleteStep { get { return OutputStagePrepareUnloadStep.Complete; } }
        protected override OutputStagePrepareUnloadStep ErrorStep { get { return OutputStagePrepareUnloadStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputStagePrepareUnloadStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStagePrepareUnloadStep.CheckTargetSide));

                    // 대상 사이드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetSide:
                        return CheckTargetSideAsync(ct);

                    // 메뉴얼 동작에서는 피더 Y를 움직이지 않고 어보이드 위치만 재확인
                    case OutputStagePrepareUnloadStep.EnsureOutputFeederSafeBeforeStageMove:
                        return Task.FromResult(EnsureOutputFeederSafeBeforeStageMove());

                    // 대상 스테이지 이동 전 반대쪽 스테이지를 어보이드로 이동
                    case OutputStagePrepareUnloadStep.MoveOppositeStageToAvoid:
                        return MoveOppositeStageToAvoidAsync(ct);

                    // 반대쪽 스테이지 어보이드 확인
                    case OutputStagePrepareUnloadStep.CheckOppositeStageAvoid:
                        return Task.FromResult(CheckOppositeStageAvoid());

                    // 대상 스테이지 Z로 어보이드 이동
                    case OutputStagePrepareUnloadStep.MoveTargetStageZToAvoid:
                        return MoveTargetStageZToAvoidAsync(ct);

                    // 대상 스테이지 Z 어보이드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetStageZAvoid:
                        return Task.FromResult(CheckTargetStageZAvoid());

                    // 대상 스테이지 Y로 언로드 이동
                    case OutputStagePrepareUnloadStep.MoveTargetStageYToUnload:
                        return MoveTargetStageYToUnloadAsync(ct);

                    // 대상 스테이지 Y 언로드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetStageYUnload:
                        return Task.FromResult(CheckTargetStageYUnload());

                    // 대상 스테이지 Z로 언로드 이동
                    case OutputStagePrepareUnloadStep.MoveTargetStageZToUnload:
                        return MoveTargetStageZToUnloadAsync(ct);

                    // 대상 스테이지 Z 언로드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetStageZUnload:
                        return Task.FromResult(CheckTargetStageZUnload());

                    // 재개 호환용 Step: 축 위치 준비 완료 후 종료
                    case OutputStagePrepareUnloadStep.EnsureTargetStageUnloadReadyState:
                        return Task.FromResult(CompleteTargetStageUnloadPreparation());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-PREP-UNLOAD-EX", Name, "Prepare unload step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> CheckTargetSideAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options.Side != BinSide.Good && Options.Side != BinSide.Ng)
                    return Fail("OUT-STAGE-SIDE", Name, "Invalid output stage side: " + Options.Side);

                int pickerReady = await WaitPickersClearForOutputTransportAsync("OutputStage Unload 준비", ct).ConfigureAwait(false);
                if (pickerReady != 0)
                    return pickerReady;

                CurrentStep = OutputStagePrepareUnloadStep.EnsureOutputFeederSafeBeforeStageMove;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-SIDE-EX", Name, "Target side check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int EnsureOutputFeederSafeBeforeStageMove()
        {
            try
            {
                if (Options.AllowOutputFeederActuation)
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveOppositeStageToAvoid;
                    return 0;
                }

                if (OutputFeeder == null)
                    return Fail("OUT-STAGE-FEEDER-NO-UNIT", "OutputFeederUnit",
                        "OutputStage Unload 준비 중 OutputFeederUnit을 찾을 수 없습니다. side=" + Options.Side);

                if (!OutputFeeder.IsBinFeederYInAvoidPosition())
                    return Fail("OUT-STAGE-FEEDER-Y-MANUAL-POS", OutputFeeder.Name,
                        "메뉴얼 OutputStage Unload는 OutputFeederY를 이동하지 않습니다. 시작 전 FeederY를 Avoid 위치로 이동하십시오. side=" +
                        Options.Side + ", " + OutputFeeder.DescribeBinFeederYMoveDoneState());

                CurrentStep = OutputStagePrepareUnloadStep.MoveOppositeStageToAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-FEEDER-SAFE-EX", "OutputFeederUnit",
                    "OutputStage Unload 전 OutputFeeder 안전 위치 확인 중 예외가 발생했습니다. side=" +
                    Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOppositeStageToAvoidAsync(CancellationToken ct)
        {
            try
            {
                BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                BinStageAxis axis = opposite == BinSide.Ng
                    ? ResolveYAxis(BinSide.Ng)
                    : ResolveZAxis(BinSide.Good);
                double target = opposite == BinSide.Ng
                    ? ResolveSideTarget(BinSide.Ng, "Avoid")
                    : ResolveSideZTarget(BinSide.Good, "Avoid");

                int result = await MoveAxisAndVerifyAsync(
                    axis,
                    target,
                    opposite + " stage avoid before " + Options.Side + " unload",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckOppositeStageAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-UNLOAD-OPP-AVOID-EX", Name,
                    "Opposite stage avoid before unload failed. side=" + Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckOppositeStageAvoid()
        {
            try
            {
                BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                BinStageAxis axis = opposite == BinSide.Ng
                    ? ResolveYAxis(BinSide.Ng)
                    : ResolveZAxis(BinSide.Good);
                double target = opposite == BinSide.Ng
                    ? ResolveSideTarget(BinSide.Ng, "Avoid")
                    : ResolveSideZTarget(BinSide.Good, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-UNLOAD-OPP-AVOID-CHECK", Stage.Name,
                        "Opposite stage avoid final check failed before unload. side=" + Options.Side + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageZToAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-UNLOAD-OPP-AVOID-CHECK-EX", Name,
                    "Opposite stage avoid check before unload failed. side=" + Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid before unload"))
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Avoid"),
                    Options.Side + " Z avoid before unload",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetStageZAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-AVOID-EX", Name, "Target stage Z avoid move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZAvoid()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid final check before unload"))
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Z-AVOID-CHECK", Stage.Name,
                        Options.Side + " Z avoid final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-AVOID-CHECK-EX", Name, "Target stage Z avoid check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageYToUnloadAsync(CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(
                    ResolveYAxis(Options.Side),
                    ResolveSideTarget(Options.Side, "Unload"),
                    Options.Side + " Y unload",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetStageYUnload;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Y-UNLOAD-EX", Name, "Target stage Y unload move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageYUnload()
        {
            try
            {
                BinStageAxis axis = ResolveYAxis(Options.Side);
                double target = ResolveSideTarget(Options.Side, "Unload");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Y-UNLOAD-CHECK", Stage.Name,
                        Options.Side + " Y unload final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageZToUnload;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Y-UNLOAD-CHECK-EX", Name, "Target stage Y unload check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToUnloadAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z unload"))
                {
                    CurrentStep = OutputStagePrepareUnloadStep.EnsureTargetStageUnloadReadyState;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Unload"),
                    Options.Side + " Z unload",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetStageZUnload;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-UNLOAD-EX", Name,
                    "Target stage Z unload move failed. side=" + Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZUnload()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z unload final check"))
                {
                    CurrentStep = OutputStagePrepareUnloadStep.EnsureTargetStageUnloadReadyState;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Unload");
                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Z-UNLOAD-CHECK", Stage.Name,
                        Options.Side + " Z unload final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.EnsureTargetStageUnloadReadyState;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-UNLOAD-CHECK-EX", Name,
                    "Target stage Z unload check failed. side=" + Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CompleteTargetStageUnloadPreparation()
        {
            if (Stage == null)
                return Fail("OUT-STAGE-PREP-UNLOAD-FINAL-MISSING", "OutputStage",
                    "OutputStage Unload 최종 상태를 확인할 수 없습니다. side=" + Options.Side);

            if (OutputFeeder == null || OutputFeeder.FeederY == null)
                return Fail("OUT-STAGE-PREP-UNLOAD-FINAL-FEEDER", "OutputFeeder",
                    "OutputStage Unload 최종 확인 중 OutputFeederY 정보를 찾을 수 없습니다. side=" + Options.Side);

            OutputFeeder.FeederY.UpdateStatus();
            if (!OutputFeeder.FeederY.IsServoOn ||
                OutputFeeder.FeederY.IsAlarm ||
                OutputFeeder.FeederY.IsMoving ||
                !OutputFeeder.IsBinFeederYInAvoidPosition() ||
                !OutputFeeder.IsBinFeederAvoidPositionCheck())
            {
                return Fail("OUT-STAGE-PREP-UNLOAD-FINAL-FEEDER-AVOID", OutputFeeder.Name,
                    "OutputStage Unload 완료 처리 전 OutputFeederY가 안전하게 정지된 정확한 Avoid 위치가 아닙니다. side=" +
                    Options.Side + ", " + OutputFeeder.DescribeBinFeederYMoveDoneState());
            }

            BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
            BinStageAxis oppositeAxis = opposite == BinSide.Ng
                ? ResolveYAxis(BinSide.Ng)
                : ResolveZAxis(BinSide.Good);
            double oppositeTarget = opposite == BinSide.Ng
                ? ResolveSideTarget(BinSide.Ng, "Avoid")
                : ResolveSideZTarget(BinSide.Good, "Avoid");

            bool oppositeAtAvoid;
            int validation = ValidateFinalAxisState(
                oppositeAxis,
                oppositeTarget,
                "반대 Stage Avoid",
                out oppositeAtAvoid);
            if (validation != 0)
                return validation;

            BinStageAxis targetYAxis = ResolveYAxis(Options.Side);
            double targetY = ResolveSideTarget(Options.Side, "Unload");
            bool targetYAtUnload;
            validation = ValidateFinalAxisState(
                targetYAxis,
                targetY,
                "대상 Stage Y Unload",
                out targetYAtUnload);
            if (validation != 0)
                return validation;

            bool targetZAtUnload = true;
            if (HasSideZAxis(Options.Side))
            {
                BinStageAxis targetZAxis = ResolveZAxis(Options.Side);
                double targetZ = ResolveSideZTarget(Options.Side, "Unload");
                validation = ValidateFinalAxisState(
                    targetZAxis,
                    targetZ,
                    "대상 Stage Z Unload",
                    out targetZAtUnload);
                if (validation != 0)
                    return validation;
            }

            if (!oppositeAtAvoid ||
                !targetYAtUnload ||
                !targetZAtUnload ||
                !Stage.IsStageInUnloadPosition(Options.Side))
            {
                WriteLog(Name,
                    "구버전 PrepareUnload 최종 Step 재개 시 축 위치가 완료 조건과 달라 안전 시작 Step으로 되돌립니다. " +
                    "side=" + Options.Side + ", oppositeAtAvoid=" + oppositeAtAvoid +
                    ", targetYAtUnload=" + targetYAtUnload +
                    ", targetZAtUnload=" + targetZAtUnload + " - Check");
                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetSide;
                return 0;
            }

            WriteLog(Name,
                "OutputStage Unload 축 위치 준비 완료. 실제 배출 실린더 자세는 " +
                "OutputFeederUnloadFromStageSequence에서 확보합니다. side=" + Options.Side + " - Ok");
            CurrentStep = OutputStagePrepareUnloadStep.Complete;
            return 0;
        }

        private int ValidateFinalAxisState(
            BinStageAxis axis,
            double target,
            string description,
            out bool inPosition)
        {
            inPosition = false;
            QMC.Common.Motion.BaseAxis item = ResolveFinalCheckAxis(axis);
            if (item == null)
                return Fail("OUT-STAGE-PREP-UNLOAD-FINAL-AXIS-MISSING", Stage != null ? Stage.Name : "OutputStage",
                    description + " 축 정보를 찾을 수 없습니다. axis=" + axis + ", side=" + Options.Side);

            item.UpdateStatus();
            if (!item.IsServoOn || item.IsAlarm || item.IsMoving)
            {
                return Fail("OUT-STAGE-PREP-UNLOAD-FINAL-AXIS-STATE", Stage.Name,
                    description + " 축이 안전하게 정지된 상태가 아닙니다. " + BuildAxisState(axis, target));
            }

            inPosition = Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis));
            return 0;
        }

        private QMC.Common.Motion.BaseAxis ResolveFinalCheckAxis(BinStageAxis axis)
        {
            if (Stage == null)
                return null;

            switch (axis)
            {
                case BinStageAxis.GoodBinY:
                    return Stage.GoodStage != null ? Stage.GoodStage.StageY : null;
                case BinStageAxis.GoodBinZ:
                    return Stage.GoodStage != null ? Stage.GoodStage.StageZ : null;
                case BinStageAxis.NgBinY:
                    return Stage.NgStage != null ? Stage.NgStage.StageY : null;
                case BinStageAxis.NgBinZ:
                    return Stage.NgStage != null ? Stage.NgStage.StageZ : null;
                default:
                    return null;
            }
        }
    }
}
