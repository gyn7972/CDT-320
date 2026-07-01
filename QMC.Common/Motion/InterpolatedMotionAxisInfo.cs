using System;

namespace QMC.Common.Motion
{
    public sealed class InterpolatedMotionAxisInfo
    {
        public int AxisNo { get; set; }
        public int NodeNum { get; set; }
        public int ModulePosition { get; set; }
        public uint ModuleId { get; set; }

        public string ModuleIdText
        {
            get { return "0x" + ModuleId.ToString("X8"); }
        }

        public override string ToString()
        {
            return string.Format(
                "axis={0}, node={1}, modulePos={2}, moduleId={3}",
                AxisNo,
                NodeNum,
                ModulePosition,
                ModuleIdText);
        }
    }
}
