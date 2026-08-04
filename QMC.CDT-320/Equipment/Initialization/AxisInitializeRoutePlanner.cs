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

        public AxisInitializeRouteResult Build(
            IList<AxisInitializeStep> steps,
            bool useDefaultSequenceFlow)
        {
            var orderedSteps = (steps ?? new AxisInitializeStep[0])
                .Where(x => x != null)
                .OrderBy(x => x.StepNo)
                .ThenBy(x => x.GroupName)
                .ToList();

            AxisInitializeSafetySnapshot snapshot =
                _runtime.CaptureSafetySnapshot(orderedSteps);

            var routeSteps = new List<AxisInitializeRouteStep>();
            foreach (AxisInitializeStep step in orderedSteps)
            {
                routeSteps.Add(BuildStep(step, useDefaultSequenceFlow));
            }

            return new AxisInitializeRouteResult(snapshot, routeSteps);
        }

        private AxisInitializeRouteStep BuildStep(
            AxisInitializeStep step,
            bool useDefaultSequenceFlow)
        {
            var routeStep = new AxisInitializeRouteStep
            {
                StepNo = step.StepNo,
                GroupName = step.GroupName ?? string.Empty,
                Phase = useDefaultSequenceFlow
                    ? AxisInitializeSequence.ResolveDefaultRoutePhase(step.StepNo)
                    : AxisInitializeRoutePhase.SerialFallback,
                Lane = useDefaultSequenceFlow
                    ? step.ParallelLane ?? string.Empty
                    : string.Empty
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

    }
}
