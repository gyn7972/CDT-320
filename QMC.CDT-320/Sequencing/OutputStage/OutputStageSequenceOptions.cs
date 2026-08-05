namespace QMC.CDT320.Sequencing
{
    public sealed class OutputStageSequenceOptions
    {
        public BinSide Side { get; set; }
        public DieGrade Grade { get; set; }
        public double TpuOffsetX { get; set; }
        public double TpuOffsetY { get; set; }
        public double VisionOffsetX { get; set; }
        public double VisionOffsetY { get; set; }
        public bool FineMove { get; set; }
        public int MoveTimeoutMs { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }
        public bool KeepVisionXAvoidOnProcessMove { get; set; }
        public bool AllowOutputFeederActuation { get; set; }

        /// <summary>
        /// [GoodStageZ 왕복 제거 2026-08-06] Process 이동 시 "Y 이동 전 Z Avoid" 단계를 건너뛸지 여부.
        ///
        /// 기본 false — ★자동 운전(생산) 동작은 그대로 유지된다.★
        /// PlaceZ 캘처럼 같은 Y 목표로 픽커만 바꿔가며 반복 실행하는 경우에만 true 로 켠다.
        ///
        /// true 라도 무조건 건너뛰지 않는다. OutputStageInterlockRules 규칙상
        /// Z Avoid 가 필요한 목표(Avoid/Load/Unload/Home)이거나 Z 가 허용 위치가 아니면
        /// 기존대로 Z 를 Avoid 로 내린다(MoveTargetStageZToAvoidBeforeYAsync 참조).
        ///
        /// 배경: PlaceZ 캘 실측(2026-08-06 05:15) — Y 를 0.05~0.25mm 옮기려고
        ///       GoodStageZ 를 32.953mm 내렸다 올리는 왕복이 픽커마다 반복됐다.
        /// </summary>
        public bool SkipTargetStageZAvoidBeforeYWhenInterlockAllows { get; set; }

        public static OutputStageSequenceOptions Default()
        {
            return new OutputStageSequenceOptions
            {
                Side = BinSide.Good,
                Grade = DieGrade.Good,
                TpuOffsetX = 0.0,
                TpuOffsetY = 0.0,
                VisionOffsetX = 0.0,
                VisionOffsetY = 0.0,
                FineMove = false,
                MoveTimeoutMs = 300000,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume,
                KeepVisionXAvoidOnProcessMove = false,
                AllowOutputFeederActuation = true,
                SkipTargetStageZAvoidBeforeYWhenInterlockAllows = false
            };
        }
    }
}
