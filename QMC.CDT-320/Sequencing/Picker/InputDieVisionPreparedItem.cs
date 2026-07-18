using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;

namespace QMC.CDT320.Sequencing
{
    internal sealed class InputDieVisionPreparedItem
    {
        public int PickerIndex { get; set; }
        public int PickerNo { get; set; }
        public string DieId { get; set; }
        public InputStagePickTarget PickTarget { get; set; }
        public int VisionRequestIndex { get; set; }
        public VisionRequestHandle VisionRequest { get; set; }
        public bool ExposureCompleted { get; set; }
        public VisionAlignResult VisionOffset { get; set; }
        public bool DiePicked { get; set; }
    }
}
