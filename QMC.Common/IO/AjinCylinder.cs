using System.Reflection;
using QMC.Common.Motion;

namespace QMC.Common.IO
{
    /// <summary>
    /// AJINEXTEK AXL DIO 기반 실린더 구현체.<br/>
    /// 전진/후진 솔레노이드 출력 2점과 전진/후진 확인 센서 입력 2점을
    /// <see cref="AjinDigitalOutput"/> / <see cref="AjinDigitalInput"/> 으로 구성한다.<br/>
    /// <list type="bullet">
    ///   <item><description>복동 실린더는 OutFwd/OutBwd 를 상호 배타적으로 제어한다.</description></item>
    ///   <item><description>단동 실린더는 OutFwd 하나만 사용하며 ON=전진, OFF=후진으로 동작한다.</description></item>
    ///   <item><description><see cref="AjinMotionSystem.IsOpen"/> = false 이면 내부 DI/DO 가 자동으로 시뮬레이션 모드로 폴백한다.</description></item>
    /// </list>
    /// </summary>
    public class AjinCylinder : BaseCylinder
    {
        private readonly int _outFwdModule;
        private readonly int _outFwdBit;
        private readonly int _outBwdModule;
        private readonly int _outBwdBit;
        private readonly int _inFwdModule;
        private readonly int _inFwdBit;
        private readonly int _inBwdModule;
        private readonly int _inBwdBit;

        /// <summary>
        /// 튜플 매핑으로 실린더 DIO 4점을 초기화한다.
        /// </summary>
        public AjinCylinder(string name,
            (int mod, int bit) outFwd, (int mod, int bit) outBwd,
            (int mod, int bit) inFwd,  (int mod, int bit) inBwd,
            bool singleSolenoid = false)
            : this(name,
                  outFwd.mod, outFwd.bit,
                  outBwd.mod, outBwd.bit,
                  inFwd.mod,  inFwd.bit,
                  inBwd.mod,  inBwd.bit,
                  singleSolenoid)
        {
        }

        /// <summary>
        /// 모듈/비트 번호로 실린더 DIO 4점을 초기화한다.
        /// </summary>
        public AjinCylinder(string name,
            int outFwdModule, int outFwdBit,
            int outBwdModule, int outBwdBit,
            int inFwdModule,  int inFwdBit,
            int inBwdModule,  int inBwdBit,
            bool singleSolenoid = false)
            : base(name)
        {
            _outFwdModule = outFwdModule;
            _outFwdBit    = outFwdBit;
            _outBwdModule = outBwdModule;
            _outBwdBit    = outBwdBit;
            _inFwdModule  = inFwdModule;
            _inFwdBit     = inFwdBit;
            _inBwdModule  = inBwdModule;
            _inBwdBit     = inBwdBit;

            Setup.IsSingleSolenoid = singleSolenoid;
            Config.IsSimulationMode = !AjinMotionSystem.IsOpen;

            ReplaceInternalIo(name);
        }

        /// <summary>
        /// <see cref="BaseCylinder"/> 생성 중 임시 출력 객체를 만든다.
        /// </summary>
        protected override BaseDigitalOutput CreateDigitalOutput(string name)
        {
            return new PlaceholderDigitalOutput(name);
        }

        /// <summary>
        /// <see cref="BaseCylinder"/> 생성 중 임시 입력 객체를 만든다.
        /// </summary>
        protected override BaseDigitalInput CreateDigitalInput(string name)
        {
            return new PlaceholderDigitalInput(name);
        }

        /// <summary>생성 완료 후 실제 AJIN DI/DO 객체로 교체한다.</summary>
        private void ReplaceInternalIo(string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            System.Type type = typeof(BaseCylinder);

            BaseDigitalOutput oldOutFwd = OutFwd;
            BaseDigitalOutput oldOutBwd = OutBwd;
            BaseDigitalInput oldInFwd = InFwd;
            BaseDigitalInput oldInBwd = InBwd;

            type.GetProperty("OutFwd", flags).SetValue(this, new AjinDigitalOutput(name + "_OutFwd", _outFwdModule, _outFwdBit), null);
            type.GetProperty("OutBwd", flags).SetValue(this, new AjinDigitalOutput(name + "_OutBwd", _outBwdModule, _outBwdBit), null);
            type.GetProperty("InFwd",  flags).SetValue(this, new AjinDigitalInput (name + "_InFwd",  _inFwdModule,  _inFwdBit), null);
            type.GetProperty("InBwd",  flags).SetValue(this, new AjinDigitalInput (name + "_InBwd",  _inBwdModule,  _inBwdBit), null);

            oldOutFwd.Dispose();
            oldOutBwd.Dispose();
            oldInFwd.Dispose();
            oldInBwd.Dispose();
        }

        /// <summary>생성자 호출 순서 보정을 위한 임시 출력.</summary>
        private sealed class PlaceholderDigitalOutput : BaseDigitalOutput
        {
            public PlaceholderDigitalOutput(string name) : base(name)
            {
            }
        }

        /// <summary>생성자 호출 순서 보정을 위한 임시 입력.</summary>
        private sealed class PlaceholderDigitalInput : BaseDigitalInput
        {
            public PlaceholderDigitalInput(string name) : base(name)
            {
            }
        }
    }
}
