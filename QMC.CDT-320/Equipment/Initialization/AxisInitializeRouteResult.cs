using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Initialization
{
    internal static class AxisInitializeRouteState
    {
        public const string ReadyNow = "ReadyNow";
        public const string RequiresRecheck = "RequiresRecheck";
        public const string Disabled = "Disabled";
    }

    internal static class AxisInitializeRoutePhase
    {
        public const string CommonPreLane = "CommonPreLane";
        public const string InputLane = "InputLane";
        public const string OutputLane = "OutputLane";
        public const string SharedRailPostLane = "SharedRailPostLane";
        public const string SerialFallback = "SerialFallback";
    }

    /// <summary>
    /// 한 초기화 Step의 현재 시점 판정입니다.
    /// RequiresRecheck는 실패 확정이 아니라 선행 Step 완료 후 다시 검사해야 한다는 뜻입니다.
    /// </summary>
    internal sealed class AxisInitializeRouteStep
    {
        public int StepNo { get; set; }
        public string GroupName { get; set; }
        public string Phase { get; set; }
        public string Lane { get; set; }
        public string State { get; set; }
        public string Reason { get; set; }

        public string BuildDisplayText()
        {
            string stateText;
            if (string.Equals(State, AxisInitializeRouteState.ReadyNow, StringComparison.OrdinalIgnoreCase))
                stateText = "현재 실행 가능";
            else if (string.Equals(State, AxisInitializeRouteState.Disabled, StringComparison.OrdinalIgnoreCase))
                stateText = "사용 안 함";
            else
                stateText = "선행 조건 대기";

            string text = "[사전판단: " + stateText + "][" + BuildPhaseText() + "]";
            if (!string.IsNullOrWhiteSpace(Reason))
                text += " " + Reason.Trim();

            return text;
        }

        public string BuildCheckText()
        {
            string stateText;
            if (string.Equals(State, AxisInitializeRouteState.ReadyNow, StringComparison.OrdinalIgnoreCase))
                stateText = "실행 가능";
            else if (string.Equals(State, AxisInitializeRouteState.Disabled, StringComparison.OrdinalIgnoreCase))
                stateText = "사용 안 함";
            else
                stateText = "조건 대기";

            return stateText + " / " + BuildPhaseText();
        }

        public string BuildLogText()
        {
            return "step=" + StepNo +
                   ", group=" + (GroupName ?? string.Empty) +
                   ", phase=" + (Phase ?? string.Empty) +
                   ", lane=" + (Lane ?? string.Empty) +
                   ", state=" + (State ?? string.Empty) +
                   (string.IsNullOrWhiteSpace(Reason) ? string.Empty : ", reason=" + Reason.Trim());
        }

        private string BuildPhaseText()
        {
            if (string.Equals(Phase, AxisInitializeRoutePhase.CommonPreLane, StringComparison.OrdinalIgnoreCase))
                return "공통 선행";
            if (string.Equals(Phase, AxisInitializeRoutePhase.InputLane, StringComparison.OrdinalIgnoreCase))
                return "입력 병렬";
            if (string.Equals(Phase, AxisInitializeRoutePhase.OutputLane, StringComparison.OrdinalIgnoreCase))
                return "출력 병렬";
            if (string.Equals(Phase, AxisInitializeRoutePhase.SharedRailPostLane, StringComparison.OrdinalIgnoreCase))
                return "공유레일 후행";
            return "직렬 실행";
        }
    }

    /// <summary>
    /// Monitor와 초기화 시작 로그가 함께 사용하는 경로 미리보기 결과입니다.
    /// </summary>
    internal sealed class AxisInitializeRouteResult
    {
        public AxisInitializeRouteResult(
            AxisInitializeSafetySnapshot snapshot,
            IList<AxisInitializeRouteStep> steps)
        {
            Snapshot = snapshot;
            Steps = steps ?? new List<AxisInitializeRouteStep>();
        }

        public AxisInitializeSafetySnapshot Snapshot { get; private set; }

        public IList<AxisInitializeRouteStep> Steps { get; private set; }

        public AxisInitializeRouteStep FindStep(int stepNo, string groupName)
        {
            return Steps.FirstOrDefault(x =>
                x != null &&
                x.StepNo == stepNo &&
                string.Equals(x.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
        }

        public string BuildSummary()
        {
            int readyCount = Steps.Count(x => x != null &&
                string.Equals(x.State, AxisInitializeRouteState.ReadyNow, StringComparison.OrdinalIgnoreCase));
            int recheckCount = Steps.Count(x => x != null &&
                string.Equals(x.State, AxisInitializeRouteState.RequiresRecheck, StringComparison.OrdinalIgnoreCase));
            int disabledCount = Steps.Count(x => x != null &&
                string.Equals(x.State, AxisInitializeRouteState.Disabled, StringComparison.OrdinalIgnoreCase));

            return "steps=" + Steps.Count +
                   ", readyNow=" + readyCount +
                   ", requiresRecheck=" + recheckCount +
                   ", disabled=" + disabledCount +
                   ", " + (Snapshot != null ? Snapshot.BuildSummary() : "snapshot=none");
        }
    }
}
