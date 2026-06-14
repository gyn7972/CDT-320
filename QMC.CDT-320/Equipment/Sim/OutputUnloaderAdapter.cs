using System.Threading.Tasks;

namespace QMC.CDT320.Sim
{
    /// <summary>
    /// Bridges OutputStage wafer-change requests to the split OutputCassette/OutputFeeder units.
    /// </summary>
    public class OutputUnloaderAdapter : IOutputUnloaderUnit
    {
        private readonly OutputCassetteUnit _cassette;
        private readonly OutputFeederUnit _feeder;

        private int _nextNgSlot;
        private int _nextGood1Slot;
        private int _nextGood2Slot;

        public OutputUnloaderAdapter(OutputCassetteUnit cassette, OutputFeederUnit feeder)
        {
            _cassette = cassette;
            _feeder = feeder;
        }

        public async Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0)
        {
            TargetCassette target;
            int slot;

            switch (grade)
            {
                // Good bin은 Good1 슬롯을 먼저 사용하고 가득 차면 Good2로 전환
                case DieGrade.Good:
                    if (_nextGood1Slot < _cassette.Config.SlotCount)
                    {
                        target = TargetCassette.Good1;
                        slot = _nextGood1Slot++;
                    }
                    else
                    {
                        target = TargetCassette.Good2;
                        slot = _nextGood2Slot++;
                    }
                    break;

                // NG 또는 알 수 없는 등급은 NG 카세트 슬롯 사용
                case DieGrade.Ng:
                default:
                    target = TargetCassette.Ng;
                    slot = _nextNgSlot++;
                    break;
            }

            if (slot >= _cassette.Config.SlotCount)
                return false;

            return await _cassette.StoreFullWaferAsync(_feeder, target, slot);
        }
    }
}
