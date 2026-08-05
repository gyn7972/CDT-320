using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStageMoveProcessStep
    {
        Idle,
        CheckUnit,
        CheckTargetSide,
        MoveOppositeStageZToAvoid,
        CheckOppositeStageZAvoid,
        MoveNgStageYToAvoid,
        CheckNgStageYAvoid,
        EnsureNgClampLiftUp,
        MoveTargetStageZToAvoidBeforeY,
        CheckTargetStageZAvoidBeforeY,
        MoveTargetStageYToProcess,
        CheckTargetStageYProcess,
        MoveTargetStageZToProcess,
        CheckTargetStageZProcess,
        MoveVisionXToProcess,
        CheckVisionXProcess,
        MoveVisionXToAvoid,
        CheckVisionXAvoid,
        Complete,
        Error
    }

    internal sealed class OutputStageMoveProcessSequence : OutputStageSequenceBase<OutputStageMoveProcessStep>
    {
        public OutputStageMoveProcessSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.MoveProcess, "OutputStageMoveProcessSequence")
        {
        }

        protected override OutputStageMoveProcessStep IdleStep { get { return OutputStageMoveProcessStep.Idle; } }
        protected override OutputStageMoveProcessStep InitialStep { get { return OutputStageMoveProcessStep.CheckUnit; } }
        protected override OutputStageMoveProcessStep CompleteStep { get { return OutputStageMoveProcessStep.Complete; } }
        protected override OutputStageMoveProcessStep ErrorStep { get { return OutputStageMoveProcessStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputStageMoveProcessStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStageMoveProcessStep.CheckTargetSide));

                    // 대상 사이드 확인
                    case OutputStageMoveProcessStep.CheckTargetSide:
                        return Task.FromResult(CheckTargetSide());

                    // 반대쪽 스테이지 Z로 어보이드 이동
                    case OutputStageMoveProcessStep.MoveOppositeStageZToAvoid:
                        return MoveOppositeStageZToAvoidAsync(ct);

                    // 반대쪽 스테이지 Z 어보이드 확인
                    case OutputStageMoveProcessStep.CheckOppositeStageZAvoid:
                        return Task.FromResult(CheckOppositeStageZAvoid());

                    // NG 스테이지 Y로 어보이드 이동
                    case OutputStageMoveProcessStep.MoveNgStageYToAvoid:
                        return MoveNgStageYToAvoidAsync(ct);

                    // NG 스테이지 Y 어보이드 확인
                    case OutputStageMoveProcessStep.CheckNgStageYAvoid:
                        return Task.FromResult(CheckNgStageYAvoid());

                    // NG 클램프 리프트 업 확보
                    case OutputStageMoveProcessStep.EnsureNgClampLiftUp:
                        return EnsureNgClampLiftUpAsync(ct);

                    // 대상 스테이지 Y 이동 전 대상 Z 어보이드 확보
                    case OutputStageMoveProcessStep.MoveTargetStageZToAvoidBeforeY:
                        return MoveTargetStageZToAvoidBeforeYAsync(ct);

                    // 대상 스테이지 Y 이동 전 대상 Z 어보이드 확인
                    case OutputStageMoveProcessStep.CheckTargetStageZAvoidBeforeY:
                        return Task.FromResult(CheckTargetStageZAvoidBeforeY());

                    // 대상 스테이지 Y로 프로세스 이동
                    case OutputStageMoveProcessStep.MoveTargetStageYToProcess:
                        return MoveTargetAxisAsync(ResolveYAxis(Options.Side), "Process", Options.Side + " Y process", OutputStageMoveProcessStep.CheckTargetStageYProcess, ct);

                    // 대상 스테이지 Y 프로세스 확인
                    case OutputStageMoveProcessStep.CheckTargetStageYProcess:
                        return Task.FromResult(CheckTargetAxis(ResolveYAxis(Options.Side), "Process", Options.Side + " Y process", OutputStageMoveProcessStep.MoveTargetStageZToProcess));

                    // 대상 스테이지 Z로 프로세스 이동
                    case OutputStageMoveProcessStep.MoveTargetStageZToProcess:
                        return MoveTargetStageZToProcessAsync(ct);

                    // 대상 스테이지 Z 프로세스 확인
                    case OutputStageMoveProcessStep.CheckTargetStageZProcess:
                        return Task.FromResult(CheckTargetStageZProcess());

                    // 비전 X로 프로세스 이동
                    case OutputStageMoveProcessStep.MoveVisionXToProcess:
                        return MoveTargetAxisAsync(BinStageAxis.VisionX, "Process", "VisionX process", OutputStageMoveProcessStep.CheckVisionXProcess, ct);

                    // 비전 X 프로세스 확인
                    case OutputStageMoveProcessStep.CheckVisionXProcess:
                        return Task.FromResult(CheckTargetAxis(BinStageAxis.VisionX, "Process", "VisionX process", OutputStageMoveProcessStep.Complete));

                    // Place 준비용 비전 X 어보이드 이동
                    case OutputStageMoveProcessStep.MoveVisionXToAvoid:
                        return MoveTargetAxisAsync(BinStageAxis.VisionX, "Avoid", "OutputVisionX Avoid", OutputStageMoveProcessStep.CheckVisionXAvoid, ct);

                    // Place 준비용 비전 X 어보이드 확인
                    case OutputStageMoveProcessStep.CheckVisionXAvoid:
                        return Task.FromResult(CheckTargetAxis(BinStageAxis.VisionX, "Avoid", "OutputVisionX Avoid", OutputStageMoveProcessStep.Complete));

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-PROCESS-EX", Name, "Move process step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTargetSide()
        {
            try
            {
                if (Options.Side != BinSide.Good && Options.Side != BinSide.Ng)
                    return Fail("OUT-STAGE-SIDE", Name, "Invalid output stage side: " + Options.Side);

                CurrentStep = OutputStageMoveProcessStep.MoveOppositeStageZToAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-SIDE-EX", Name, "Target side check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOppositeStageZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                if (SkipMissingSideZAxis(opposite, opposite + " Z avoid before process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveNgStageYToAvoid;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(opposite),
                    ResolveSideZTarget(opposite, "Avoid"),
                    opposite + " Z avoid before process",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageMoveProcessStep.CheckOppositeStageZAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-OPP-Z-AVOID-EX", Name, "Opposite stage Z avoid failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckOppositeStageZAvoid()
        {
            try
            {
                BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                if (SkipMissingSideZAxis(opposite, opposite + " Z avoid final check before process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveNgStageYToAvoid;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(opposite);
                double target = ResolveSideZTarget(opposite, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-OPP-Z-CHECK", Stage.Name,
                        opposite + " Z avoid final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStageMoveProcessStep.MoveNgStageYToAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-OPP-Z-CHECK-EX", Name, "Opposite stage Z avoid check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNgStageYToAvoidAsync(CancellationToken ct)
        {
            try
            {
                double target = ResolveTarget(BinStageAxis.NgBinY, "Avoid");
                BaseAxis ngStageY = Stage != null && Stage.NgStage != null ? Stage.NgStage.StageY : null;
                if (Options.Side != BinSide.Good)
                {
                    CurrentStep = OutputStageMoveProcessStep.EnsureNgClampLiftUp;
                    return 0;
                }

                if (ngStageY.IsAtTargetPosition(target, 0.0))
                {
                    CurrentStep = OutputStageMoveProcessStep.CheckNgStageYAvoid;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    BinStageAxis.NgBinY,
                    target,
                    "NG Y avoid before Good process",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageMoveProcessStep.CheckNgStageYAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-NG-Y-AVOID-EX", Name, "NG stage Y avoid before Good process failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckNgStageYAvoid()
        {
            try
            {
                if (Options.Side != BinSide.Good)
                {
                    CurrentStep = OutputStageMoveProcessStep.EnsureNgClampLiftUp;
                    return 0;
                }

                if (!Stage.IsNgStageInAvoidPosition())
                    return Fail("OUT-STAGE-NG-Y-AVOID-CHECK", Stage.Name,
                        "NG stage must be in Avoid position before Good process. " +
                        BuildAxisState(BinStageAxis.NgBinY, ResolveTarget(BinStageAxis.NgBinY, "Avoid")));

                CurrentStep = OutputStageMoveProcessStep.EnsureNgClampLiftUp;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-NG-Y-AVOID-CHECK-EX", Name, "NG stage Y avoid check before Good process failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureNgClampLiftUpAsync(CancellationToken ct)
        {
            try
            {
                if (Options.Side != BinSide.Good || Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveTargetStageZToAvoidBeforeY;
                    return 0;
                }

                int result = await Stage.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, ResolveTimeout(), ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name,
                        "NG Bin Clamp Lift Up failed before Good process. result=" + result + ". " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Good));

                if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                    return Fail("OUT-STAGE-NG-CLAMP-UP-CHECK", Stage.Name,
                        "NG Bin Clamp Lift Up final check failed before Good process. " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Good));

                CurrentStep = OutputStageMoveProcessStep.MoveTargetStageZToAvoidBeforeY;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-NG-CLAMP-UP-EX", Name,
                    "NG Bin Clamp Lift Up before Good process exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToAvoidBeforeYAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid before Y process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                    return 0;
                }

                // ============================================================
                // [GoodStageZ 왕복 제거 2026-08-06]  ★자동 운전 무영향 — 옵션 기본 false★
                //
                // 실측(2026-08-06 05:15 PlaceZ 캘, OUT-STAGE-MOVE-TRACE):
                //   GoodBinZ  32.953 -> 0.000   dist=32.953   (Y 이동 전 Z Avoid)
                //   GoodBinY 288.193 -> 287.944 dist=0.249    (Y 는 0.25mm)
                //   GoodBinZ   0.000 -> 32.953  dist=32.953   (다시 Process)
                //   → 픽커 4대 반복. Y 0.05~0.25mm 를 위해 Z 를 32.953mm 왕복했다.
                //
                // 기존 인터락 규칙(OutputStageInterlockRules.VerifyGoodStageYMechanicalClear:1441):
                //   Y 목표가 Avoid/Load/Unload/Home 이면 Z Avoid 필수,
                //   그 외 목표면 Z 가 Avoid "또는 Process" 여도 Y 이동이 허용된다.
                //   캘 타겟(287.944)은 그 외에 해당하므로 Z 를 내릴 필요가 없다.
                //
                // 이 시퀀스는 생산(OutputSequence / OutputFeederLoadToStage)과 공용이므로
                // ★옵션이 켜진 호출부(PlaceZ 캘)에서만★ 생략한다. 옵션이 꺼진 자동 운전은 기존 그대로다.
                // 옵션이 켜져 있어도 인터락이 Avoid 를 요구하면 생략하지 않는다(이중 안전).
                // ============================================================
                if (Options.SkipTargetStageZAvoidBeforeYWhenInterlockAllows &&
                    CanSkipTargetStageZAvoidBeforeY())
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Avoid"),
                    Options.Side + " Z avoid before Y process",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageMoveProcessStep.CheckTargetStageZAvoidBeforeY;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TARGET-Z-AVOID-EX", Name, "Target stage Z avoid before Y process failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// [GoodStageZ 왕복 제거 2026-08-06] "Y 이동 전 Z Avoid" 를 건너뛰어도 되는지 판정한다.
        ///
        /// 판정은 새로 만들지 않고 인터락 규칙 함수를 그대로 호출한다
        /// (OutputStageInterlockRules.VerifyGoodStageYMechanicalClear:1441 과 동일 기준).
        /// 시퀀스가 자체 판정을 중복 구현하면 인터락과 어긋날 수 있어 금지한다.
        ///
        /// Good side 전용이다. NG side 의 Z Avoid 요구는 별도 규칙(NGStageY 이동 전 GoodStageZ Avoid)
        /// 이므로 여기서 완화하지 않는다.
        /// </summary>
        private bool CanSkipTargetStageZAvoidBeforeY()
        {
            try
            {
                if (Stage == null || Options.Side != BinSide.Good)
                    return false;

                double targetY = ResolveSideTarget(Options.Side, "Process");

                bool requiresAvoid = QMC.CDT320.Interlocks.OutputStageInterlockRules
                    .IsGoodStageYTargetRequiringGoodZAvoid(Stage, targetY);
                bool zAllowed = Stage.IsGoodStageZInAvoidOrProcessPosition();
                bool canSkip = !requiresAvoid && zAllowed;

                QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Calibration", "OUT-STAGE-Z-AVOID-SKIP",
                    Name + " Y 이동 전 Z Avoid 판정. side=" + Options.Side +
                    ", targetY=" + targetY.ToString("F3") +
                    ", requiresAvoid=" + requiresAvoid +
                    ", zAvoidOrProcess=" + zAllowed +
                    ", canSkip=" + canSkip +
                    ", " + BuildAxisState(ResolveZAxis(Options.Side), ResolveSideZTarget(Options.Side, "Avoid")) +
                    (canSkip ? " - 생략" : " - 수행"));

                return canSkip;
            }
            catch
            {
                // 판정 실패 시에는 기존 동작(Z Avoid 수행)으로 폴백한다.
                return false;
            }
            finally
            {
            }
        }

        private int CheckTargetStageZAvoidBeforeY()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid final check before Y process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Avoid");

                // [GoodStageZ 왕복 제거 2026-08-06] 이동을 생략했으면 Avoid 도달 검증도 건너뛴다.
                // (검증만 남기면 생략한 순간 반드시 실패한다)
                if (Options.SkipTargetStageZAvoidBeforeYWhenInterlockAllows &&
                    CanSkipTargetStageZAvoidBeforeY())
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                    return 0;
                }

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-TARGET-Z-AVOID-CHECK", Stage.Name,
                        Options.Side + " Z avoid final check failed before Y process. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TARGET-Z-AVOID-CHECK-EX", Name, "Target stage Z avoid check before Y process failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetAxisAsync(
            BinStageAxis axis,
            string positionName,
            string description,
            OutputStageMoveProcessStep nextStep,
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
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PROCESS-MOVE-EX", Name, description + " move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToProcessAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z process"))
                {
                    CurrentStep = ResolveVisionXStepAfterStageProcess();
                    return 0;
                }

                return await MoveTargetAxisAsync(
                    ResolveZAxis(Options.Side),
                    "Process",
                    Options.Side + " Z process",
                    OutputStageMoveProcessStep.CheckTargetStageZProcess,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PROCESS-Z-EX", Name, "Target stage Z process move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZProcess()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z process final check"))
                {
                    CurrentStep = ResolveVisionXStepAfterStageProcess();
                    return 0;
                }

                return CheckTargetAxis(
                    ResolveZAxis(Options.Side),
                    "Process",
                    Options.Side + " Z process",
                    ResolveVisionXStepAfterStageProcess());
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PROCESS-Z-CHECK-EX", Name, "Target stage Z process check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetAxis(
            BinStageAxis axis,
            string positionName,
            string description,
            OutputStageMoveProcessStep nextStep)
        {
            try
            {
                double target = ResolveTarget(axis, positionName);
                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-PROCESS-CHECK", Stage.Name,
                        description + " final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PROCESS-CHECK-EX", Name, description + " check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private OutputStageMoveProcessStep ResolveVisionXStepAfterStageProcess()
        {
            return Options.KeepVisionXAvoidOnProcessMove
                ? OutputStageMoveProcessStep.MoveVisionXToAvoid
                : OutputStageMoveProcessStep.MoveVisionXToProcess;
        }
    }
}

