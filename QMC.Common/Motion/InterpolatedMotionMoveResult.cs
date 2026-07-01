using System;

namespace QMC.Common.Motion
{
    public sealed class InterpolatedMotionMoveResult
    {
        public int ResultCode { get; set; }
        public int Coordinate { get; set; }
        public int[] RequestedAxes { get; set; }
        public double[] RequestedPositions { get; set; }
        public int[] MappedAxes { get; set; }
        public double[] MappedPositions { get; set; }
        public double Velocity { get; set; }
        public double Acceleration { get; set; }
        public double Deceleration { get; set; }
        public int TimeoutMs { get; set; }
        public long ElapsedMs { get; set; }
        public bool CommandIssued { get; set; }
        public string Message { get; set; }

        public InterpolatedMotionMoveResult()
        {
            RequestedAxes = new int[0];
            RequestedPositions = new double[0];
            MappedAxes = new int[0];
            MappedPositions = new double[0];
            Message = string.Empty;
        }

        public bool Success
        {
            get { return ResultCode == 0; }
        }

        public string RequestedAxesText
        {
            get { return string.Join(",", RequestedAxes ?? new int[0]); }
        }

        public string RequestedPositionsText
        {
            get { return string.Join(",", Array.ConvertAll(RequestedPositions ?? new double[0], x => x.ToString("F6"))); }
        }

        public string MappedAxesText
        {
            get { return string.Join(",", MappedAxes ?? new int[0]); }
        }

        public string MappedPositionsText
        {
            get { return string.Join(",", Array.ConvertAll(MappedPositions ?? new double[0], x => x.ToString("F6"))); }
        }

        public override string ToString()
        {
            return string.Format(
                "result={0}, coordinate={1}, requestedAxes=[{2}], requestedPos=[{3}], mappedAxes=[{4}], mappedPos=[{5}], vel={6:F3}, acc={7:F3}, dec={8:F3}, timeoutMs={9}, elapsedMs={10}, commandIssued={11}, message={12}",
                ResultCode,
                Coordinate,
                RequestedAxesText,
                RequestedPositionsText,
                MappedAxesText,
                MappedPositionsText,
                Velocity,
                Acceleration,
                Deceleration,
                TimeoutMs,
                ElapsedMs,
                CommandIssued,
                Message);
        }
    }
}
