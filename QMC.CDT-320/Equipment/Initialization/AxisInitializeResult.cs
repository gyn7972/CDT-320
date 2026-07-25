using System;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// 축 초기화 실행 결과를 MachineController로 전달합니다.
    /// </summary>
    internal sealed class AxisInitializeResult
    {
        private AxisInitializeResult(
            int resultCode,
            int failedStepNo,
            string failedGroup,
            string failedLane,
            string errorMessage)
        {
            ResultCode = resultCode;
            FailedStepNo = failedStepNo;
            FailedGroup = failedGroup ?? string.Empty;
            FailedLane = failedLane ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool Succeeded
        {
            get { return ResultCode == 0; }
        }

        public int ResultCode { get; private set; }

        public int FailedStepNo { get; private set; }

        public string FailedGroup { get; private set; }

        public string FailedLane { get; private set; }

        public string ErrorMessage { get; private set; }

        public static AxisInitializeResult Success()
        {
            return new AxisInitializeResult(0, 0, string.Empty, string.Empty, string.Empty);
        }

        public static AxisInitializeResult Failure(
            int resultCode,
            AxisInitializeStep step,
            string failedLane,
            string errorMessage)
        {
            return new AxisInitializeResult(
                resultCode != 0 ? resultCode : -1,
                step != null ? step.StepNo : 0,
                step != null ? step.GroupName : string.Empty,
                failedLane,
                errorMessage);
        }
    }
}
