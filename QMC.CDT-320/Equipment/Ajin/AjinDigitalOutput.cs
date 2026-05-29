using QMC.Common.IO;

namespace QMC.CDT320.Ajin
{
    public class AjinDigitalOutput : BaseDigitalOutput
    {
        protected override bool UseInternalStatusUpdate
        {
            get { return false; }
        }

        public AjinDigitalOutput(string name, int moduleNo, int bitNo, bool normallyClosed = false)
            : base(name)
        {
            Setup.ModuleNo = moduleNo;
            Setup.BitNo = bitNo;
            Setup.IsNormallyClosed = normallyClosed;
            Config.IsSimulationMode = false;
        }

        public override void Write(bool state)
        {
            base.Write(state);
            if (!AjinSystem.IsOpen) return;
            AjinIoScanService.WriteOutput(this, state);
        }

        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode) return;
            if (!AjinSystem.IsOpen) return;

            AjinIoScanService service = AjinIoScanService.Current;
            if (service != null && service.TryApplyLatest(this)) return;

            bool raw = false;
            int ret;
            lock (AjinIoScanService.AxdSyncRoot)
                ret = QMC.Common.Motion.Ajin.AXD.ReadOutput(Setup.ModuleNo, Setup.BitNo, ref raw);
            if (ret != 0) return;

            bool logical = Setup.IsNormallyClosed ? !raw : raw;
            ApplyScannedState(logical);
        }

    }
}
