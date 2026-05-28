using QMC.Common.IO;
using QMC.Common.Motion.Ajin;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// AXL AxdoWriteOutportBit / AxdoReadOutportBit 기반 실 DO.
    /// </summary>
    public class AjinDigitalOutput : BaseDigitalOutput
    {
        public AjinDigitalOutput(string name, int moduleNo, int bitNo, bool normallyClosed = false)
            : base(name)
        {
            Setup.ModuleNo         = moduleNo;
            Setup.BitNo            = bitNo;
            Setup.IsNormallyClosed = normallyClosed;
            Config.IsSimulationMode = false;
        }

        public override void Write(bool state)
        {
            // base.Write 가 IsOn 반영 + StateChanged 이벤트 발행을 담당.
            // IsSimulationMode=false 이므로 base 는 여기서 리턴하고, 우리가 실제 보드에 씀.
            base.Write(state);
            if (!AjinSystem.IsOpen) return;

            bool physical = Setup.IsNormallyClosed ? !state : state;
            AXD.Write(Setup.ModuleNo, Setup.BitNo, physical);
        }

        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode) return;
            if (!AjinSystem.IsOpen)      return;

            bool raw = false;
            if (AXD.ReadOutput(Setup.ModuleNo, Setup.BitNo, ref raw) != 0) return;

            bool logical = Setup.IsNormallyClosed ? !raw : raw;
            // 외부에서 직접 보드 토글했거나 하드웨어 피드백이 다를 수 있어 동기화
            if (IsOn != logical) IsOn = logical;
        }
    }
}
