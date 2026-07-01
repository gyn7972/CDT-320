using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Common.Motion.Ajin;

namespace QMC.Common.Motion
{
    public sealed class InterpolatedMotionMapResult
    {
        public int ResultCode { get; set; }
        public int Coordinate { get; set; }
        public int[] RequestedAxes { get; set; }
        public int[] MappedAxes { get; set; }
        public uint MappedSize { get; set; }
        public AXT_MOTION_ABSREL AbsRelMode { get; set; }
        public string Message { get; set; }
        public List<InterpolatedMotionAxisInfo> AxisInfos { get; private set; }

        public InterpolatedMotionMapResult()
        {
            RequestedAxes = new int[0];
            MappedAxes = new int[0];
            AxisInfos = new List<InterpolatedMotionAxisInfo>();
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

        public string MappedAxesText
        {
            get { return string.Join(",", MappedAxes ?? new int[0]); }
        }

        public string AxisInfoText
        {
            get { return string.Join(" | ", AxisInfos.Select(x => x.ToString())); }
        }

        public override string ToString()
        {
            return string.Format(
                "result={0}, coordinate={1}, requested=[{2}], mapped=[{3}], size={4}, mode={5}, message={6}, axisInfo={7}",
                ResultCode,
                Coordinate,
                RequestedAxesText,
                MappedAxesText,
                MappedSize,
                AbsRelMode,
                Message,
                AxisInfoText);
        }
    }
}
