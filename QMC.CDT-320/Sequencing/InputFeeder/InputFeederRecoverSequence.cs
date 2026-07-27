using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Sequencing.Safety;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederRecoverStep
    {
        Idle,
        CheckUnit,
        RetreatToAvoid,
        Complete,
        Error
    }

    /// <summary>
    /// 빈 InputFeeder를 안전 대기 자세(Avoid + Lift Down)로 되돌린다.
    ///
    /// 후퇴 순서와 파손 방지 확인은 FeederRetreatPolicy가 단일 구현으로 보장한다.
    /// 여기서 Unclamp/Lift/Avoid 순서를 다시 구현하지 말 것.
    /// (2026-07-27: 순서가 4곳에 복사되어 있었고 그중 하나가 Lift Down 상태로 주행해
    ///  스테이지 제품을 긁었다. 그래서 정책 클래스로 합쳤다.)
    /// </summary>
    internal sealed class InputFeederRecoverSequence : InputFeederSequenceBase<InputFeederRecoverStep>
    {
        public InputFeederRecoverSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.Recover, "InputFeederRecoverSequence")
        {
        }

        protected override InputFeederRecoverStep IdleStep { get { return InputFeederRecoverStep.Idle; } }
        protected override InputFeederRecoverStep InitialStep { get { return InputFeederRecoverStep.CheckUnit; } }
        protected override InputFeederRecoverStep CompleteStep { get { return InputFeederRecoverStep.Complete; } }
        protected override InputFeederRecoverStep ErrorStep { get { return InputFeederRecoverStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputFeederRecoverStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederRecoverStep.RetreatToAvoid));

                    // 안전 후퇴: Unclamp -> 보유확인 -> Lift Up -> Y Avoid -> Lift Down
                    case InputFeederRecoverStep.RetreatToAvoid:
                        return RetreatToAvoidAsync(ct);

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-RECOVER-STEP-EX", "InputFeederRecoverSequence", "Recover step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> RetreatToAvoidAsync(CancellationToken ct)
        {
            if (IsFeederAlreadyRecoveredStrongly())
            {
                Context.Bus.Set("InputFeederRecovered");
                CurrentStep = InputFeederRecoverStep.Complete;
                return 0;
            }

            int timeoutMs = ResolveTimeout();

            // Avoid 이동은 기존처럼 시퀀스 베이스의 이동/인계 확인 경로를 그대로 사용한다.
            var target = new InputFeederRetreatTarget(
                Feeder,
                async (moveTimeoutMs, token) =>
                {
                    int moveResult = await AwaitStepWithCancellationAsync(
                        Feeder.MoveToWaferFeederAvoidPosition(Options.FineMove), token).ConfigureAwait(false);
                    if (moveResult != 0)
                        return moveResult;

                    return await WaitFeederYDoneAsync(
                        () => Feeder.IsWaferFeederInAvoidPosition(),
                        "WaferFeeder recover avoid position",
                        token).ConfigureAwait(false);
                });

            FeederRetreatResult retreat = await FeederRetreatPolicy.RetreatToAvoidAsync(
                target, timeoutMs, timeoutMs, ct).ConfigureAwait(false);
            if (!retreat.Success)
            {
                // Avoid 이동 단계는 하위 헬퍼가 이미 알람을 올렸으므로 중복 알람 없이 결과만 전파한다.
                if (retreat.FailedPhase == FeederRetreatPhase.MoveAvoid && retreat.CommandResult != 0)
                    return retreat.CommandResult;

                return Fail(ResolveRetreatAlarmCode(retreat.FailedPhase), Feeder.Name,
                    "WaferFeeder recover 실패. " + retreat.Message);
            }

            Context.Bus.Set("InputFeederRecovered");
            CurrentStep = InputFeederRecoverStep.Complete;
            return 0;
        }

        private bool IsFeederAlreadyRecoveredStrongly()
        {
            if (Feeder == null ||
                Feeder.FeederY == null ||
                Feeder.Recipe == null)
            {
                return false;
            }

            double target = Feeder.Recipe.AvoidPosition;
            double tolerance =
                Feeder.FeederY.Config != null &&
                Feeder.FeederY.Config.InPositionTolerance >= 0.0
                    ? Feeder.FeederY.Config.InPositionTolerance
                    : 0.05;
            bool actualPositionKeyMatches =
                Math.Round(
                    Feeder.FeederY.ActualPosition,
                    3,
                    MidpointRounding.AwayFromZero) ==
                Math.Round(
                    target,
                    3,
                    MidpointRounding.AwayFromZero);
            bool commandPositionKeyMatches =
                Math.Round(
                    Feeder.FeederY.CommandPosition,
                    3,
                    MidpointRounding.AwayFromZero) ==
                Math.Round(
                    target,
                    3,
                    MidpointRounding.AwayFromZero);

            return Feeder.FeederY.IsServoOn &&
                   !Feeder.FeederY.IsAlarm &&
                   !Feeder.FeederY.IsMoving &&
                   Feeder.FeederY.IsInPosition &&
                   actualPositionKeyMatches &&
                   commandPositionKeyMatches &&
                   Math.Abs(Feeder.FeederY.ActualPosition - target) <= tolerance &&
                   Math.Abs(Feeder.FeederY.CommandPosition - target) <= tolerance &&
                   Feeder.IsWaferFeederInAvoidPosition() &&
                   Feeder.IsWaferFeederAvoidPositionCheck() &&
                   Feeder.IsWaferFeederDown() &&
                   !Feeder.IsWaferFeederUp() &&
                   Feeder.IsWaferFeederUnclamp() &&
                   !Feeder.IsWaferFeederClamp() &&
                   Feeder.IsWaferFeederTransferDataEmpty() &&
                   Feeder.IsWaferFeederEmpty() &&
                   !Feeder.IsWaferFeederOverload();
        }

        // 기존 알람 코드 체계를 유지한다(현장 알람 대응 문서와 일치시키기 위함).
        private static string ResolveRetreatAlarmCode(FeederRetreatPhase phase)
        {
            switch (phase)
            {
                case FeederRetreatPhase.Unclamp: return "IN-FEEDER-RECOVER-UNCLAMP";
                case FeederRetreatPhase.LiftUpBlocked: return "IN-FEEDER-RECOVER-LIFT-BLOCK";
                case FeederRetreatPhase.LiftUp: return "IN-FEEDER-RECOVER-UP";
                case FeederRetreatPhase.MoveAvoid: return "IN-FEEDER-RECOVER-AVOID";
                case FeederRetreatPhase.LiftDown: return "IN-FEEDER-RECOVER-DOWN";
                default: return "IN-FEEDER-RECOVER-CHECK";
            }
        }
    }
}
