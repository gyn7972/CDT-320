using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.Common.IO;
using QMC.CDT320.Ajin;

namespace QMC.CDT320
{
    /// <summary>
    /// Stage 48 — Post PNP Transfer Tool (CDT-310 매뉴얼 사양).<br/>
    /// Pick & Place 후처리 — Bin 적재 전 최종 정렬 + 진공 검증 + 안착 검사 보조.
    /// (현 시뮬레이션은 OutputStage.InspectBinPositionAsync 가 유사 역할 수행)
    /// </summary>
    public class PostPnpTransferUnit
    {
        public BaseAxis          PnpZ      { get; private set; }
        public BaseDigitalOutput PnpVacuum { get; private set; }
        public BaseDigitalInput  PnpPickOk { get; private set; }

        public PostPnpTransferUnit()
        {
            PnpZ      = AjinFactory.CreateAxis("PostPnp_Z");
            PnpVacuum = AjinFactory.CreateDigitalOutput("PostPnp_Vacuum");
            PnpPickOk = AjinFactory.CreateDigitalInput("PostPnp_PickOk");
        }

        /// <summary>최종 픽업 — Z 하강 + 진공 + 안착 확인.</summary>
        public async Task<bool> FinalPickupAsync()
        {
            await PnpZ.MoveAbsoluteAsync(50.0, 100.0);
            if (PnpZ.IsAlarm) return false;
            PnpVacuum.On();
            await Task.Delay(100);
            return PnpPickOk.IsOn;
        }

        /// <summary>최종 배치 — Z 하강 + 진공 OFF + Z 상승.</summary>
        public async Task<bool> FinalPlaceAsync()
        {
            await PnpZ.MoveAbsoluteAsync(50.0, 100.0);
            if (PnpZ.IsAlarm) return false;
            PnpVacuum.Off();
            await Task.Delay(100);
            await PnpZ.MoveAbsoluteAsync(0.0, 100.0);
            return !PnpZ.IsAlarm;
        }
    }
}
