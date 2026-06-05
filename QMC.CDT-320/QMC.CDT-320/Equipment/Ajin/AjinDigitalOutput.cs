using QMC.Common.IO;

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

            int physical = Setup.IsNormallyClosed ? (state ? 0 : 1) : (state ? 1 : 0);
            Axl.AxdoWriteOutportBit(Setup.ModuleNo, Setup.BitNo, physical);
        }

        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode) return;
            if (!AjinSystem.IsOpen)      return;

            int raw = 0;
            if (!AxtReturn.IsSuccess(Axl.AxdoReadOutportBit(Setup.ModuleNo, Setup.BitNo, ref raw))) return;

            bool logical = Setup.IsNormallyClosed ? raw == 0 : raw != 0;
            // 외부에서 직접 보드 토글했거나 하드웨어 피드백이 다를 수 있어 동기화
            if (IsOn != logical) IsOn = logical;
        }
    }
}
