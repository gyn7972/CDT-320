using System.Threading.Tasks;

namespace QMC.CDT320.Sim
{
    /// <summary>
    /// Stage 27 — 실 <see cref="OutputUnloaderUnit"/> 를
    /// <see cref="IOutputUnloaderUnit"/> 인터페이스로 어댑팅.<br/>
    /// OutputStageUnit 이 Bin 가득 시 RequestWaferChangeAsync 호출하면
    /// 실제 Unloader 의 ExchangeWaferSequenceAsync 를 호출한다.
    /// (이전엔 NullOutputUnloaderUnit 으로 무효화되어 있었음)
    /// </summary>
    public class OutputUnloaderAdapter : IOutputUnloaderUnit
    {
        private readonly OutputUnloaderUnit _unloader;
        /// <summary>다음 적재 대상 슬롯 (간이) — 실 운영에서는 LotStorage 와 연동.</summary>
        private int _nextNgSlot    = 0;
        private int _nextGood1Slot = 0;
        private int _nextGood2Slot = 0;

        public OutputUnloaderAdapter(OutputUnloaderUnit unloader)
        {
            _unloader = unloader;
        }

        /// <summary>
        /// OutputStage 의 Bin 가득 신호를 받아 Unloader 가 카세트 교체 시퀀스를 수행.<br/>
        /// 시뮬에서는 ExchangeWaferSequenceAsync 의 단순화 — StoreFullWafer + (옵션) SupplyEmptyWafer.
        /// </summary>
        public async Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0)
        {
            TargetCassette target;
            int slot;
            switch (grade)
            {
                case DieGrade.Good:
                    if (_nextGood1Slot < 25) { target = TargetCassette.Good1; slot = _nextGood1Slot++; }
                    else                      { target = TargetCassette.Good2; slot = _nextGood2Slot++; }
                    break;
                case DieGrade.Ng:
                default:
                    target = TargetCassette.Ng;
                    slot = _nextNgSlot++;
                    break;
            }
            if (slot >= 25) return false;
            return await _unloader.StoreFullWaferAsync(target, slot);
        }
    }
}
