using QMC.Common.Motion;
using QMC.Common.Motion.Ajin;

namespace QMC.Common.IO
{
    /// <summary>
    /// AJINEXTEK AXL 보드 기반 디지털 출력 구현체.<br/>
    /// <see cref="BaseDigitalOutput.Write"/> 로 명령한 논리 상태를 실제 출력 비트에 기록하고,
    /// 10ms 폴링 루프에서 출력 피드백을 읽어 상태를 동기화한다.<br/>
    /// <list type="bullet">
    ///   <item><description><see cref="AjinMotionSystem.IsOpen"/> = false 이면 시뮬레이션 모드로 폴백한다.</description></item>
    ///   <item><description><see cref="IoSetup.IsNormallyClosed"/> 설정에 따라 물리 출력 신호를 반전한다.</description></item>
    /// </list>
    /// </summary>
    public class AjinDigitalOutput : BaseDigitalOutput
    {
        /// <summary>AXL DIO 모듈 번호.</summary>
        public int ModuleNo { get; }

        /// <summary>모듈 내 출력 비트 번호.</summary>
        public int BitNo { get; }

        /// <summary>
        /// 지정한 DIO 출력 비트를 초기화한다.
        /// </summary>
        public AjinDigitalOutput(string name, int moduleNo, int bitNo, bool normallyClosed = false)
            : base(name)
        {
            ModuleNo = moduleNo;
            BitNo    = bitNo;

            Setup.ModuleNo         = moduleNo;
            Setup.BitNo            = bitNo;
            Setup.IsNormallyClosed = normallyClosed;
            Config.IsSimulationMode = !IsReal();
        }

        /// <summary>실보드 사용 가능 여부.</summary>
        private bool IsReal()
        {
            return AjinMotionSystem.IsOpen
                && ModuleNo >= 0
                && BitNo >= 0;
        }

        /// <summary>논리 출력 상태를 실제 AXD 출력 비트에 기록한다.</summary>
        public override void Write(bool state)
        {
            base.Write(state);
            if (Config.IsSimulationMode) return;
            if (!IsReal()) return;

            try
            {
                bool physical = Setup.IsNormallyClosed ? !state : state;
                AXD.Write(ModuleNo, BitNo, physical);
            }
            catch
            {
                // 단발 출력 명령 예외는 상태 루프를 깨지 않도록 삼킨다.
            }
        }

        /// <summary>출력 피드백 비트를 읽어 <see cref="IsOn"/> 상태와 변경 이벤트를 갱신한다.</summary>
        public override void UpdateStatus()
        {
            if (Config.IsSimulationMode) return;
            if (!IsReal()) return;

            try
            {
                bool raw = false;
                if (AXD.ReadOutput(ModuleNo, BitNo, ref raw) != 0) return;

                bool logical = Setup.IsNormallyClosed ? !raw : raw;
                if (IsOn == logical) return;

                IsOn = logical;
                RaiseStateChanged(logical);
            }
            catch
            {
                // 폴링 중 예외는 무시하고 다음 주기에 재시도한다.
            }
        }
    }
}
