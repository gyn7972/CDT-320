using QMC.Common.IO;
using QMC.Common.Logging;

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

            if (IsBinOutput(Name))
            {
                EventLogger.Write(EventKind.Event, "QMC", "AJIN-DO-WRITE",
                    "DO write. name=" + Name
                    + ", module=" + Setup.ModuleNo
                    + ", bit=" + Setup.BitNo
                    + ", state=" + (state ? "ON" : "OFF")
                    + ", sim=" + Config.IsSimulationMode);
            }

            if (Config.IsSimulationMode) return;
            if (!AjinSystem.IsOpen) return;
            AjinIoScanService.WriteOutput(this, state);
        }

        private static bool IsBinOutput(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                return name.IndexOf("BinGuide", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("BinClamp", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("BinUnclamp", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
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
