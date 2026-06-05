using QMC.Common.IO;
using QMC.CDT320.Ajin;

namespace QMC.CDT320
{
    /// <summary>
    /// Stage 47 — Ionizer Unit (CDT-310 매뉴얼 사양).<br/>
    /// 정전기 제거기 — 동작 상태 감시 + 알람 (이전 IonizerSensor 단일 클래스 확장).
    /// </summary>
    public class IonizerUnit
    {
        public BaseDigitalInput  IonizerOk { get; private set; }
        public BaseDigitalOutput IonizerOn { get; private set; }

        public IonizerUnit()
        {
            IonizerOk = AjinFactory.CreateDigitalInput("IonizerOk");
            IonizerOn = AjinFactory.CreateDigitalOutput("IonizerOn");
        }

        public void TurnOn()  { IonizerOn.On();  }
        public void TurnOff() { IonizerOn.Off(); }

        /// <summary>이오나이저 정상 동작 여부 (Ok 센서 ON).</summary>
        public bool IsHealthy => IonizerOk.IsOn;
    }
}
