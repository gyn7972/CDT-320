using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// 현재 상태를 읽어 초기화 Step의 실행 Phase와 대기 사유를 정리합니다.
    /// 실제 Step 순서를 바꾸거나 축을 움직이는 기능은 이 클래스에 두지 않습니다.
    /// </summary>
    internal sealed class AxisInitializeRoutePlanner
    {
        private readonly AxisInitializeRuntime _runtime;

        public AxisInitializeRoutePlanner(AxisInitializeRuntime runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public AxisInitializeRouteResult Build(IList<AxisInitializeStep> steps)
        {
            var orderedSteps = (steps ?? new AxisInitializeStep[0])
                .Where(x => x != null)
                .OrderBy(x => x.StepNo)
                .ThenBy(x => x.GroupName)
                .ToList();

            AxisInitializeSafetySnapshot snapshot =
                _runtime.CaptureSafetySnapshot(orderedSteps);

            int firstLaneStepNo;
            int lastLaneStepNo;
            ResolveLaneBarrier(orderedSteps, out firstLaneStepNo, out lastLaneStepNo);

            var routeSteps = new List<AxisInitializeRouteStep>();
            foreach (AxisInitializeStep step in orderedSteps)
            {
                routeSteps.Add(BuildStep(
                    step,
                    firstLaneStepNo,
                    lastLaneStepNo));
            }

            return new AxisInitializeRouteResult(snapshot, routeSteps);
        }

        private AxisInitializeRouteStep BuildStep(
            AxisInitializeStep step,
            int firstLaneStepNo,
            int lastLaneStepNo)
        {
            var routeStep = new AxisInitializeRouteStep
            {
                StepNo = step.StepNo,
                GroupName = step.GroupName ?? string.Empty,
                Phase = ResolvePhase(step, firstLaneStepNo, lastLaneStepNo),
                Lane = step.ParallelLane ?? string.Empty
            };

            if (!step.Enabled)
            {
                routeStep.State = AxisInitializeRouteState.Disabled;
                routeStep.Reason = "Plan에서 비활성화된 Step입니다.";
                return routeStep;
            }

            string reason;
            bool readyNow = _runtime.InspectStep(step, out reason);
            routeStep.State = readyNow
                ? AxisInitializeRouteState.ReadyNow
                : AxisInitializeRouteState.RequiresRecheck;
            routeStep.Reason = readyNow
                ? "실제 실행 직전에 인터락을 다시 확인합니다."
                : reason;
            return routeStep;
        }

        /// <summary>
        /// 수정자가 전체 경로 구분을 한곳에서 확인할 수 있도록 Phase 판단을 모았습니다.
        /// </summary>
        private static string ResolvePhase(
            AxisInitializeStep step,
            int firstLaneStepNo,
            int lastLaneStepNo)
        {
            if (AxisInitializeParallelLane.Is(
                step.ParallelLane,
                AxisInitializeParallelLane.Input))
                return AxisInitializeRoutePhase.InputLane;

            if (AxisInitializeParallelLane.Is(
                step.ParallelLane,
                AxisInitializeParallelLane.Output))
                return AxisInitializeRoutePhase.OutputLane;

            if (firstLaneStepNo == int.MaxValue || lastLaneStepNo == int.MinValue)
                return AxisInitializeRoutePhase.SerialFallback;

            if (step.StepNo < firstLaneStepNo)
                return AxisInitializeRoutePhase.CommonPreLane;
            if (step.StepNo > lastLaneStepNo)
                return AxisInitializeRoutePhase.SharedRailPostLane;

            // 현재 Plan에서는 발생하지 않아야 하며, 실행부도 Lane 누락으로 차단하는 구간입니다.
            return AxisInitializeRoutePhase.SerialFallback;
        }

        private static void ResolveLaneBarrier(
            IList<AxisInitializeStep> steps,
            out int firstLaneStepNo,
            out int lastLaneStepNo)
        {
            var laneSteps = (steps ?? new AxisInitializeStep[0])
                .Where(x => x != null && x.Enabled &&
                    (AxisInitializeParallelLane.Is(
                         x.ParallelLane,
                         AxisInitializeParallelLane.Input) ||
                     AxisInitializeParallelLane.Is(
                         x.ParallelLane,
                         AxisInitializeParallelLane.Output)))
                .ToList();

            firstLaneStepNo = laneSteps.Count > 0
                ? laneSteps.Min(x => x.StepNo)
                : int.MaxValue;
            lastLaneStepNo = laneSteps.Count > 0
                ? laneSteps.Max(x => x.StepNo)
                : int.MinValue;
        }
    }
}
