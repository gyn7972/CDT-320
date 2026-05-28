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

            bool raw = false;
            if (AXD.Read(Setup.ModuleNo, Setup.BitNo, ref raw) != 0) return;

            bool signal = raw;
            bool logical = Setup.IsNormallyClosed ? !signal : signal;
            if (IsOn != logical)
            {
                IsOn = logical;
                RaiseStateChanged(logical);
            }
        }

        // SimulateInput 은 실보드 모드에서 무시되므로 base 그대로.
    }
}
