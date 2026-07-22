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
        // 조기 허가 경로: InputPickVision 자재 기록/미촬영 다이 전파까지 완료된 항목인지.
        // prepare(CollectVisionResultsAsync)의 Apply가 세팅하며, 미적용이면 픽업 CalculatePickTargets가 수행한다.
        public bool VisionOffsetApplied { get; set; }
        public bool DiePicked { get; set; }
    }
}
