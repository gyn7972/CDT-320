using QMC.Common.IO;
using QMC.CDT320.Ajin;

namespace QMC.CDT320
{
    /// <summary>
    /// Stage 47 — Ionizer Unit (CDT-310 매뉴얼 사양).<br/>
    /// 정전기 제거기 — 동작 상태 감시 + 알람 (이전 IonizerSensor 단일 클래스 확장).
    /// Machine이 하나의 인스턴스를 공유하며, 출력 생성과 제어를 이 클래스에서 관리한다.
    /// </summary>
    public class IonizerUnit
    {
        public BaseDigitalInput  IonizerOk { get; private set; }
        public BaseDigitalOutput IonizerOn { get; private set; }

        public IonizerUnit() : this(deferInitialization: false)
        {
        }

        internal IonizerUnit(bool deferInitialization)
        {
            if (!deferInitialization)
                Initialize();
        }

        internal BaseDigitalOutput InitializeOutput()
        {
            IonizerOn = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.IonizerOn);
            return IonizerOn;
        }

        internal void Initialize()
        {
            IonizerOk = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.InputLifterIonizerAlarm);
            InitializeOutput();
        }

        /// <summary>센서 확인이나 대기 없이 기존 이오나이저 논리 출력을 설정한다.</summary>
        public void SetEnabled(bool on) { IonizerOn.Write(on); }

        public void TurnOn()  { SetEnabled(true);  }
        public void TurnOff() { SetEnabled(false); }

        /// <summary>이오나이저 논리 출력의 현재 상태.</summary>
        public bool IsOn => IonizerOn != null && IonizerOn.IsOn;

        /// <summary>이오나이저 정상 동작 여부 (Ok 센서 ON).</summary>
        public bool IsHealthy => IonizerOk.IsOn;
    }
}
