using QMC.Common.IO;
using QMC.Common.Motion.Ajin;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// AXL AxdiReadInportBit 기반 실 DI.
    /// Setup.ModuleNo / BitNo / IsNormallyClosed 를 사용.
    /// </summary>
    public class AjinDigitalInput : BaseDigitalInput
    {
        protected override bool UseInternalStatusUpdate
        {
            get { return false; }
        }

        public AjinDigitalInput(string name, int moduleNo, int bitNo, bool normallyClosed = false)
            : base(name)
        {
            Setup.ModuleNo         = moduleNo;
            Setup.BitNo            = bitNo;
            Setup.IsNormallyClosed = normallyClosed;
            Config.IsSimulationMode = false;
        }

        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode) return;
            if (!AjinSystem.IsOpen)      return;
            AjinIoScanService service = AjinIoScanService.Current;
            if (service != null && service.TryApplyLatest(this)) return;

            bool raw = false;
            int ret;
            lock (AjinIoScanService.AxdSyncRoot)
                ret = AXD.Read(Setup.ModuleNo, Setup.BitNo, ref raw);
            if (ret != 0) return;

            bool signal = raw;
            bool logical = Setup.IsNormallyClosed ? !signal : signal;
            ApplyScannedState(logical);
        }

        // SimulateInput 은 실보드 모드에서 무시되므로 base 그대로.
    }
}
