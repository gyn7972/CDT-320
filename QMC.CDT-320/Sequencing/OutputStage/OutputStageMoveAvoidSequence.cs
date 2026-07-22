using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStageMoveAvoidStep
    {
        Idle,
        CheckUnit,
        MoveGoodStageZToAvoid,
        CheckGoodStageZAvoid,
        MoveGoodStageYToAvoid,
        CheckGoodStageYAvoid,
        MoveNgStageYToAvoid,
        CheckNgStageYAvoid,
        MoveVisionXToAvoid,
        CheckVisionXAvoid,
        VerifyAllAxesAvoid,
        Complete,
        Error
    }

    internal sealed class OutputStageMoveAvoidSequence : OutputStageSequenceBase<OutputStageMoveAvoidStep>
    {
        private enum AvoidAxisState
        {
            ReadyAtAvoid,
            ReadyOutsideAvoid,
            Unsafe
        }

        public OutputStageMoveAvoidSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.MoveAvoid, "OutputStageMoveAvoidSequence")
        {
        }

        protected override OutputStageMoveAvoidStep IdleStep { get { return OutputStageMoveAvoidStep.Idle; } }
        protected override OutputStageMoveAvoidStep InitialStep { get { return OutputStageMoveAvoidStep.CheckUnit; } }
        protected override OutputStageMoveAvoidStep CompleteStep { get { return OutputStageMoveAvoidStep.Complete; } }
        protected override OutputStageMoveAvoidStep ErrorStep { get { return OutputStageMoveAvoidStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputStageMoveAvoidStep.CheckUnit:
                        // 안전 순서: Vision X -> Good Z -> NG Y -> Good Y.
                        // Good Y는 NG Y exact Avoid 이후에만 이동할 수 있으므로 순서를 바꾸지 않는다.
                        return Task.FromResult(CheckUnit(OutputStageMoveAvoidStep.MoveVisionXToAvoid));

                    // 비전 X로 어보이드 이동
                    case OutputStageMoveAvoidStep.MoveVisionXToAvoid:
                        return MoveAxisAsync(BinStageAxis.VisionX, "Avoid", "VisionX avoid", OutputStageMoveAvoidStep.CheckVisionXAvoid, ct);

                    // 비전 X 어보이드 확인
                    case OutputStageMoveAvoidStep.CheckVisionXAvoid:
                        return Task.FromResult(CheckAxis(BinStageAxis.VisionX, "Avoid", "VisionX avoid", OutputStageMoveAvoidStep.MoveGoodStageZToAvoid));

                    // GOOD 스테이지 Z로 어보이드 이동
                    case OutputStageMoveAvoidStep.MoveGoodStageZToAvoid:
                        bool visionReady;
                        int visionGate = EnsurePrecedingAxisAtAvoid(
                            BinStageAxis.VisionX,
                            OutputStageMoveAvoidStep.MoveVisionXToAvoid,
                            "Good Z 이동 전 Vision X Avoid 재확인",
                            out visionReady);
                        if (!visionReady)
                            return Task.FromResult(visionGate);
                        return MoveAxisAsync(BinStageAxis.GoodBinZ, "Avoid", "Good Z avoid", OutputStageMoveAvoidStep.CheckGoodStageZAvoid, ct);

                    // GOOD 스테이지 Z 어보이드 확인
                    case OutputStageMoveAvoidStep.CheckGoodStageZAvoid:
                        return Task.FromResult(CheckAxis(BinStageAxis.GoodBinZ, "Avoid", "Good Z avoid", OutputStageMoveAvoidStep.MoveNgStageYToAvoid));

                    // GOOD 스테이지 Y로 어보이드 이동
                    case OutputStageMoveAvoidStep.MoveGoodStageYToAvoid:
                        bool goodYVisionReady;
                        int goodYVisionGate = EnsurePrecedingAxisAtAvoid(
                            BinStageAxis.VisionX,
                            OutputStageMoveAvoidStep.MoveVisionXToAvoid,
                            "Good Y 이동 전 Vision X Avoid 재확인",
                            out goodYVisionReady);
                        if (!goodYVisionReady)
                            return Task.FromResult(goodYVisionGate);

                        bool goodZReady;
                        int goodZGate = EnsurePrecedingAxisAtAvoid(
                            BinStageAxis.GoodBinZ,
                            OutputStageMoveAvoidStep.MoveGoodStageZToAvoid,
                            "Good Y 이동 전 Good Z Avoid 재확인",
                            out goodZReady);
                        if (!goodZReady)
                            return Task.FromResult(goodZGate);

                        bool ngYReady;
                        int ngYGate = EnsurePrecedingAxisAtAvoid(
                            BinStageAxis.NgBinY,
                            OutputStageMoveAvoidStep.MoveNgStageYToAvoid,
                            "Good Y 이동 전 NG Y Avoid 재확인",
                            out ngYReady);
                        if (!ngYReady)
                            return Task.FromResult(ngYGate);
                        return MoveAxisAsync(BinStageAxis.GoodBinY, "Avoid", "Good Y avoid", OutputStageMoveAvoidStep.CheckGoodStageYAvoid, ct);

                    // GOOD 스테이지 Y 어보이드 확인
                    case OutputStageMoveAvoidStep.CheckGoodStageYAvoid:
                        return Task.FromResult(CheckAxis(BinStageAxis.GoodBinY, "Avoid", "Good Y avoid", OutputStageMoveAvoidStep.VerifyAllAxesAvoid));

                    // NG 스테이지 Y로 어보이드 이동
                    case OutputStageMoveAvoidStep.MoveNgStageYToAvoid:
                        bool ngYVisionReady;
                        int ngYVisionGate = EnsurePrecedingAxisAtAvoid(
                            BinStageAxis.VisionX,
                            OutputStageMoveAvoidStep.MoveVisionXToAvoid,
                            "NG Y 이동 전 Vision X Avoid 재확인",
                            out ngYVisionReady);
                        if (!ngYVisionReady)
                            return Task.FromResult(ngYVisionGate);

                        bool ngYGoodZReady;
                        int ngYGoodZGate = EnsurePrecedingAxisAtAvoid(
                            BinStageAxis.GoodBinZ,
                            OutputStageMoveAvoidStep.MoveGoodStageZToAvoid,
                            "NG Y 이동 전 Good Z Avoid 재확인",
                            out ngYGoodZReady);
                        if (!ngYGoodZReady)
                            return Task.FromResult(ngYGoodZGate);
                        return MoveAxisAsync(BinStageAxis.NgBinY, "Avoid", "NG Y avoid", OutputStageMoveAvoidStep.CheckNgStageYAvoid, ct);

                    // NG 스테이지 Y 어보이드 확인
                    case OutputStageMoveAvoidStep.CheckNgStageYAvoid:
                        return Task.FromResult(CheckAxis(BinStageAxis.NgBinY, "Avoid", "NG Y avoid", OutputStageMoveAvoidStep.MoveGoodStageYToAvoid));

                    // 구버전 저장 Step으로 재개해도 네 축이 모두 Avoid인지 마지막에 다시 확인한다.
                    // 미완료 축이 있으면 반드시 Vision X -> Good Z -> NG Y -> Good Y 안전 순서로 재진입한다.
                    case OutputStageMoveAvoidStep.VerifyAllAxesAvoid:
                        return Task.FromResult(VerifyAllAxesAvoid());

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
                return Task.FromResult(Fail("OUT-STAGE-AVOID-EX", Name, "Move avoid step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisAsync(
            BinStageAxis axis,
            string positionName,
            string description,
            OutputStageMoveAvoidStep nextStep,
            CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(
                    axis,
                    ResolveTarget(axis, positionName),
                    description,
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = nextStep;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-AVOID-MOVE-EX", Name, description + " move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckAxis(
            BinStageAxis axis,
            string positionName,
            string description,
            OutputStageMoveAvoidStep nextStep)
        {
            try
            {
                double target = ResolveTarget(axis, positionName);
                string stateReason;
                if (EvaluateAxisAtAvoid(axis, out stateReason) != AvoidAxisState.ReadyAtAvoid)
                    return Fail("OUT-STAGE-AVOID-CHECK", Stage.Name,
                        description + " final check failed. target=" + target + ". " +
                        stateReason);

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-AVOID-CHECK-EX", Name, description + " check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyAllAxesAvoid()
        {
            try
            {
                bool visionReady;
                int visionResult = EnsurePrecedingAxisAtAvoid(
                    BinStageAxis.VisionX,
                    OutputStageMoveAvoidStep.MoveVisionXToAvoid,
                    "최종 확인에서 Vision X Avoid 미완료",
                    out visionReady);
                if (!visionReady)
                    return visionResult;

                bool goodZReady;
                int goodZResult = EnsurePrecedingAxisAtAvoid(
                    BinStageAxis.GoodBinZ,
                    OutputStageMoveAvoidStep.MoveGoodStageZToAvoid,
                    "최종 확인에서 Good Z Avoid 미완료",
                    out goodZReady);
                if (!goodZReady)
                    return goodZResult;

                bool ngYReady;
                int ngYResult = EnsurePrecedingAxisAtAvoid(
                    BinStageAxis.NgBinY,
                    OutputStageMoveAvoidStep.MoveNgStageYToAvoid,
                    "최종 확인에서 NG Y Avoid 미완료",
                    out ngYReady);
                if (!ngYReady)
                    return ngYResult;

                bool goodYReady;
                int goodYResult = EnsurePrecedingAxisAtAvoid(
                    BinStageAxis.GoodBinY,
                    OutputStageMoveAvoidStep.MoveGoodStageYToAvoid,
                    "최종 확인에서 Good Y Avoid 미완료",
                    out goodYReady);
                if (!goodYReady)
                    return goodYResult;

                CurrentStep = OutputStageMoveAvoidStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-AVOID-FINAL-CHECK-EX", Name,
                    "Move avoid final all-axis check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int EnsurePrecedingAxisAtAvoid(
            BinStageAxis axis,
            OutputStageMoveAvoidStep reentryStep,
            string reason,
            out bool ready)
        {
            string stateReason;
            AvoidAxisState state = EvaluateAxisAtAvoid(axis, out stateReason);
            if (state == AvoidAxisState.ReadyAtAvoid)
            {
                ready = true;
                return 0;
            }

            ready = false;
            if (state == AvoidAxisState.ReadyOutsideAvoid)
                return ReenterSafeOrder(reentryStep, reason + ". " + stateReason);

            return Fail("OUT-STAGE-AVOID-PREDECESSOR", Stage != null ? Stage.Name : "OutputStage",
                reason + " 중 선행축이 안전하게 정지된 상태가 아닙니다. 다음 축 이동을 차단합니다. " + stateReason);
        }

        private AvoidAxisState EvaluateAxisAtAvoid(BinStageAxis axis, out string stateReason)
        {
            if (Stage == null)
            {
                stateReason = "OutputStage=null";
                return AvoidAxisState.Unsafe;
            }

            QMC.Common.Motion.BaseAxis item = ResolveAxisForAvoidCheck(axis);
            if (item == null)
            {
                stateReason = axis + "=null";
                return AvoidAxisState.Unsafe;
            }

            double target = ResolveTarget(axis, "Avoid");
            bool inPosition = Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis));
            stateReason = BuildAxisState(axis, target);

            if (!item.IsServoOn || item.IsAlarm || item.IsMoving)
                return AvoidAxisState.Unsafe;

            return inPosition ? AvoidAxisState.ReadyAtAvoid : AvoidAxisState.ReadyOutsideAvoid;
        }

        private QMC.Common.Motion.BaseAxis ResolveAxisForAvoidCheck(BinStageAxis axis)
        {
            if (Stage == null)
                return null;

            switch (axis)
            {
                case BinStageAxis.VisionX:
                    return Stage.OutputCameraX;
                case BinStageAxis.GoodBinZ:
                    return Stage.GoodStage != null ? Stage.GoodStage.StageZ : null;
                case BinStageAxis.NgBinY:
                    return Stage.NgStage != null ? Stage.NgStage.StageY : null;
                case BinStageAxis.GoodBinY:
                    return Stage.GoodStage != null ? Stage.GoodStage.StageY : null;
                default:
                    return null;
            }
        }

        private int ReenterSafeOrder(OutputStageMoveAvoidStep nextStep, string reason)
        {
            WriteLog(Name,
                "MoveAvoid 저장 Step/최종 상태 재검증으로 안전 순서에 재진입합니다. reason=" +
                reason + ", nextStep=" + nextStep + " - Check");
            CurrentStep = nextStep;
            return 0;
        }
    }
}
