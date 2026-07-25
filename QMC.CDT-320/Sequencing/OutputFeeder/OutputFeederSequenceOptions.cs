using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public sealed class OutputFeederSequenceOptions
    {
        public int SlotIndex { get; set; }
        public int NextSlotIndex { get; set; }
        public BinSide Side { get; set; }
        public CassetteMaterialRole CassetteRole { get; set; }
        public string ExpectedWaferId { get; set; }
        public int MoveTimeoutMs { get; set; }
        public bool FineMove { get; set; }
        public bool UseBarcode { get; set; }
        public bool UseVacuum { get; set; }

        /// <summary>
        /// true이면 이송 종료 시 OutputCassette 리프터를 Avoid로 되돌리지 않고 현재 슬롯 위치에 둔다.
        /// 같은 Loader 작업 승인(lease) 안에서 곧바로 다음 슬롯 접근이 이어질 때만 사용한다.
        /// (Picker X 이동은 리프터 Avoid를 요구하므로, lease를 놓기 전에는 반드시 Avoid로 복귀해야 한다.)
        /// OutputFeeder Avoid 복귀는 이 옵션과 무관하게 항상 수행한다.
        /// </summary>
        public bool KeepCassetteAtSlotForNextAccess { get; set; }

        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static OutputFeederSequenceOptions Default()
        {
            return new OutputFeederSequenceOptions
            {
                SlotIndex = 0,
                NextSlotIndex = 0,
                Side = BinSide.Good,
                CassetteRole = CassetteMaterialRole.Good1,
                ExpectedWaferId = "",
                MoveTimeoutMs = 300000,
                FineMove = false,
                UseBarcode = false,
                UseVacuum = true,
                KeepCassetteAtSlotForNextAccess = false,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }
}
