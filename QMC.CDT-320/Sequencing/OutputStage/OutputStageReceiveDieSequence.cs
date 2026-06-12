using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStageReceiveDieStep
    {
        Idle,
        CheckUnit,
        ResolveTargetSide,
        ReserveReceiveTarget,
        MoveOppositeStageZToAvoid,
        CheckOppositeStageZAvoid,
        MoveTargetStageZToLoad,
        CheckTargetStageZLoad,
        MoveTargetStageYToReceive,
        CheckTargetStageYReceive,
        NotifyTpuPlaceReady,
        Complete,
        Error
    }

    internal sealed class OutputStageReceiveDieSequence : OutputStageSequenceBase<OutputStageReceiveDieStep>
    {
        private BinSide _targetSide;
        private OutputStageReceiveTarget _receiveTarget;
        private double _targetY;

        public OutputStageReceiveDieSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.ReceiveDie, "OutputStageReceiveDieSequence")
        {
        }

        protected override OutputStageReceiveDieStep IdleStep { get { return OutputStageReceiveDieStep.Idle; } }
        protected override OutputStageReceiveDieStep InitialStep { get { return OutputStageReceiveDieStep.CheckUnit; } }
        protected override OutputStageReceiveDieStep CompleteStep { get { return OutputStageReceiveDieStep.Complete; } }
        protected override OutputStageReceiveDieStep ErrorStep { get { return OutputStageReceiveDieStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    case OutputStageReceiveDieStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStageReceiveDieStep.ResolveTargetSide));

                    case OutputStageReceiveDieStep.ResolveTargetSide:
                        return Task.FromResult(ResolveTargetSide());

                    case OutputStageReceiveDieStep.ReserveReceiveTarget:
                        return Task.FromResult(ReserveReceiveTarget());

                    case OutputStageReceiveDieStep.MoveOppositeStageZToAvoid:
                        return MoveOppositeStageZToAvoidAsync(ct);

                    case OutputStageReceiveDieStep.CheckOppositeStageZAvoid:
                        return Task.FromResult(CheckOppositeStageZAvoid());

                    case OutputStageReceiveDieStep.MoveTargetStageZToLoad:
                        return MoveTargetStageZToLoadAsync(ct);

                    case OutputStageReceiveDieStep.CheckTargetStageZLoad:
                        return Task.FromResult(CheckTargetStageZLoad());

                    case OutputStageReceiveDieStep.MoveTargetStageYToReceive:
                        return MoveTargetStageYToReceiveAsync(ct);

                    case OutputStageReceiveDieStep.CheckTargetStageYReceive:
                        return Task.FromResult(CheckTargetStageYReceive());

                    case OutputStageReceiveDieStep.NotifyTpuPlaceReady:
                        return Task.FromResult(NotifyTpuPlaceReady());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-RECEIVE-EX", Name, "Receive die step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int ResolveTargetSide()
        {
            try
            {
                _targetSide = ResolveSideFromGrade();
                Options.Side = _targetSide;

                CurrentStep = OutputStageReceiveDieStep.ReserveReceiveTarget;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-RECEIVE-SIDE-EX", Name, "Receive target side resolve failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int ReserveReceiveTarget()
        {
            try
            {
                _receiveTarget = MaterialStateService.ReserveNextOutputStageReceiveTarget(_targetSide);
                if (_receiveTarget == null)
                    return Fail("OUT-STAGE-RECEIVE-TARGET", "Material", "Output stage receive target was not found. side=" + _targetSide);

                double baseY = ResolveSideTarget(_targetSide, "Load");
                _targetY = baseY + _receiveTarget.OffsetY + Options.TpuOffsetY + Options.VisionOffsetY;

                CurrentStep = OutputStageReceiveDieStep.MoveOppositeStageZToAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-RECEIVE-TARGET-EX", Name, "Receive target reserve failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOppositeStageZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                BinSide opposite = _targetSide == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                if (SkipMissingSideZAxis(opposite, opposite + " Z avoid before receive"))
                {
                    CurrentStep = OutputStageReceiveDieStep.MoveTargetStageZToLoad;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(opposite),
                    ResolveSideZTarget(opposite, "Avoid"),
                    opposite + " Z avoid before receive",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageReceiveDieStep.CheckOppositeStageZAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-RECEIVE-OPP-Z-EX", Name, "Receive opposite stage avoid failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckOppositeStageZAvoid()
        {
            try
            {
                BinSide opposite = _targetSide == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                if (SkipMissingSideZAxis(opposite, opposite + " Z avoid final check before receive"))
                {
                    CurrentStep = OutputStageReceiveDieStep.MoveTargetStageZToLoad;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(opposite);
                double target = ResolveSideZTarget(opposite, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-RECEIVE-OPP-Z-CHECK", Stage.Name, opposite + " Z avoid final check failed. target=" + target);

                CurrentStep = OutputStageReceiveDieStep.MoveTargetStageZToLoad;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-RECEIVE-OPP-Z-CHECK-EX", Name, "Receive opposite stage avoid check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToLoadAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(_targetSide, _targetSide + " Z receive/load"))
                {
                    CurrentStep = OutputStageReceiveDieStep.MoveTargetStageYToReceive;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(_targetSide),
                    ResolveSideZTarget(_targetSide, "Load"),
                    _targetSide + " Z receive/load",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageReceiveDieStep.CheckTargetStageZLoad;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-RECEIVE-Z-EX", Name, "Receive target stage Z move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZLoad()
        {
            try
            {
                if (SkipMissingSideZAxis(_targetSide, _targetSide + " Z receive/load final check"))
                {
                    CurrentStep = OutputStageReceiveDieStep.MoveTargetStageYToReceive;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(_targetSide);
                double target = ResolveSideZTarget(_targetSide, "Load");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-RECEIVE-Z-CHECK", Stage.Name, _targetSide + " Z receive/load final check failed. target=" + target);

                CurrentStep = OutputStageReceiveDieStep.MoveTargetStageYToReceive;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-RECEIVE-Z-CHECK-EX", Name, "Receive target Z check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageYToReceiveAsync(CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(
                    ResolveYAxis(_targetSide),
                    _targetY,
                    _targetSide + " Y receive with offset",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageReceiveDieStep.CheckTargetStageYReceive;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-RECEIVE-Y-EX", Name, "Receive target stage Y move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageYReceive()
        {
            try
            {
                BinStageAxis axis = ResolveYAxis(_targetSide);
                if (!Stage.IsStageAxisInPosition(axis, _targetY, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-RECEIVE-Y-CHECK", Stage.Name, _targetSide + " Y receive final check failed. target=" + _targetY);

                CurrentStep = OutputStageReceiveDieStep.NotifyTpuPlaceReady;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-RECEIVE-Y-CHECK-EX", Name, "Receive target Y check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int NotifyTpuPlaceReady()
        {
            try
            {
                if (Stage.Tpu == null)
                    return Fail("OUT-STAGE-TPU-MISSING", Stage.Name, "TPU interface is not available.");

                Stage.Tpu.NotifyPlaceReady();
                CurrentStep = OutputStageReceiveDieStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TPU-NOTIFY-EX", Name, "TPU place ready notify failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
