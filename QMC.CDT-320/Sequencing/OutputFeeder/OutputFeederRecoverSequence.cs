using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Sequencing.Safety;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederRecoverStep
    {
        Idle,
        CheckUnit,
        CheckFeederEmpty,
        RetreatToAvoid,
        Complete,
        Error
    }

    /// <summary>
    /// 빈 OutputFeeder를 안전 대기 자세(Avoid + Lift Down)로 되돌린다.
    ///
    /// 후퇴 순서와 파손 방지 확인은 FeederRetreatPolicy가 단일 구현으로 보장한다.
    /// 여기서 Unclamp/Lift/Avoid 순서를 다시 구현하지 말 것.
    /// (2026-07-27: 순서가 4곳에 복사되어 있었고 그중 하나가 Lift Down 상태로 주행해
    ///  스테이지 제품을 긁었다. 그래서 정책 클래스로 합쳤다.)
    /// </summary>
    internal sealed class OutputFeederRecoverSequence : OutputFeederSequenceBase<OutputFeederRecoverStep>
    {
        public OutputFeederRecoverSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.Recover, "OutputFeederRecoverSequence")
        {
        }

        protected override OutputFeederRecoverStep IdleStep { get { return OutputFeederRecoverStep.Idle; } }
        protected override OutputFeederRecoverStep InitialStep { get { return OutputFeederRecoverStep.CheckUnit; } }
        protected override OutputFeederRecoverStep CompleteStep { get { return OutputFeederRecoverStep.Complete; } }
        protected override OutputFeederRecoverStep ErrorStep { get { return OutputFeederRecoverStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputFeederRecoverStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederRecoverStep.CheckFeederEmpty));

                    // 피더 비어있음 확인
                    case OutputFeederRecoverStep.CheckFeederEmpty:
                        return Task.FromResult(CheckFeederEmpty());

                    // 안전 후퇴: Unclamp -> 보유확인 -> Lift Up -> Y Avoid -> Lift Down
                    case OutputFeederRecoverStep.RetreatToAvoid:
                        return RetreatToAvoidAsync(ct);

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-RECOVER-EX", Name, "Recover step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckFeederEmpty()
        {
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-RECOVER-OCCUPIED", "Material",
                    "Output feeder recover is not allowed while bin data exists. Use load-to-stage or unload-to-cassette sequence. " + Feeder.DescribeFeederCylinderState());

            if (!IsHardwareBypass() && !Feeder.IsFeederEmpty())
                return Fail("OUT-FEEDER-RECOVER-SENSOR", Feeder.Name,
                    "Output feeder recover is not allowed while feeder sensor is occupied. " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederRecoverStep.RetreatToAvoid;
            return 0;
        }

        private async Task<int> RetreatToAvoidAsync(CancellationToken ct)
        {
            int timeoutMs = ResolveTimeout();

            // Avoid 이동은 기존처럼 시퀀스 베이스의 이동/인계 확인 경로를 그대로 사용한다.
            // (이동 명령 실패 알람 OUT-FEEDER-Y-MOVE, 최종 인계 확인 OUT-FEEDER-Y-POSITION/OVERLOAD 유지)
            var target = new OutputFeederRetreatTarget(
                Feeder,
                async (moveTimeoutMs, token) =>
                {
                    int moveResult = await MoveFeederYCommandAsync(
                        Feeder.MoveToFeederAvoidPosition(Options.FineMove), "avoid", token).ConfigureAwait(false);
                    if (moveResult != 0)
                        return moveResult;

                    return await WaitFeederYDoneAsync(
                        () => Feeder.IsBinFeederInAvoidPosition(), "avoid", token).ConfigureAwait(false);
                },
                IsHardwareBypass());

            FeederRetreatResult retreat = await FeederRetreatPolicy.RetreatToAvoidAsync(
                target, timeoutMs, timeoutMs, ct).ConfigureAwait(false);
            if (!retreat.Success)
            {
                // Avoid 이동 단계는 하위 헬퍼가 이미 알람을 올렸으므로 중복 알람 없이 결과만 전파한다.
                if (retreat.FailedPhase == FeederRetreatPhase.MoveAvoid && retreat.CommandResult != 0)
                    return retreat.CommandResult;

                return Fail(ResolveRetreatAlarmCode(retreat.FailedPhase), Feeder.Name,
                    "Output feeder recover 실패. " + retreat.Message);
            }

            CurrentStep = OutputFeederRecoverStep.Complete;
            return 0;
        }

        // 기존 알람 코드 체계를 유지한다(현장 알람 대응 문서와 일치시키기 위함).
        private static string ResolveRetreatAlarmCode(FeederRetreatPhase phase)
        {
            switch (phase)
            {
                case FeederRetreatPhase.Unclamp: return "OUT-FEEDER-RECOVER-UNCLAMP";
                case FeederRetreatPhase.LiftUpBlocked: return "OUT-FEEDER-RECOVER-LIFT-BLOCK";
                case FeederRetreatPhase.LiftUp: return "OUT-FEEDER-RECOVER-UP";
                case FeederRetreatPhase.MoveAvoid: return "OUT-FEEDER-RECOVER-AVOID";
                case FeederRetreatPhase.LiftDown: return "OUT-FEEDER-RECOVER-DOWN";
                default: return "OUT-FEEDER-RECOVER-CHECK";
            }
        }
    }
}
