using QMC.Common.IO;
using QMC.Common;
using QMC.CDT320.Ajin;

namespace QMC.CDT320
{
    /// <summary>
    /// Stage 45 — Operation Panel + Tower Lamp + Buzzer (CDT-310 매뉴얼 사양).<br/>
    /// 운전자 조작 버튼 (Start/Stop/Reset/EMG) + 표시 램프 + 신호탑 + 부저.
    /// </summary>
    public class OperationPanelUnit : BaseUnit<UnitSetup, UnitConfig, UnitRecipe>
    {
        // ─── DI (운전자 입력) ─────────────────────────────────────────
        public BaseDigitalInput StartButton { get; private set; }
        public BaseDigitalInput StopButton  { get; private set; }
        public BaseDigitalInput ResetButton { get; private set; }
        /// <summary>EMG 비상정지 버튼 (X003).</summary>
        public BaseDigitalInput EmgFront    { get; private set; }
        public BaseDigitalInput EmgLeft     { get; private set; }
        public BaseDigitalInput EmgRear     { get; private set; }
        public BaseDigitalInput OpEmgOn     { get; private set; }

        // ─── DO (운전자 표시) ─────────────────────────────────────────
        public BaseDigitalOutput StartLamp  { get; private set; }
        public BaseDigitalOutput StopLamp   { get; private set; }
        public BaseDigitalOutput ResetLamp  { get; private set; }

        // ─── Tower Lamp ───────────────────────────────────────────────
        public BaseDigitalOutput TlRed      { get; private set; }
        public BaseDigitalOutput TlYellow   { get; private set; }
        public BaseDigitalOutput TlGreen    { get; private set; }

        // ─── Buzzer ───────────────────────────────────────────────────
        public BaseDigitalOutput Buzzer     { get; private set; }

        public OperationPanelUnit() : base("OperationPanel")
        {
            // DI
            StartButton = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.StartButton);
            StopButton  = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.StopButton);
            ResetButton = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.ResetButton);
            EmgFront    = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.ElecEmgOn);
            EmgLeft     = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.RightEmgOn);
            EmgRear     = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.RearEmgOn);
            OpEmgOn     = AjinFactory.CreateDigitalInput(AjinIoCatalog.Inputs.OpEmgOn);

            // DO
            StartLamp = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.StartLamp);
            StopLamp  = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.StopLamp);
            ResetLamp = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.ResetLamp);

            TlRed     = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.TlRed);
            TlYellow  = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.TlYellow);
            TlGreen   = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.TlGreen);

            Buzzer    = AjinFactory.CreateDigitalOutput(AjinIoCatalog.Outputs.Buzzer);

            Components.Add(StartButton);
            Components.Add(StopButton);
            Components.Add(ResetButton);
            Components.Add(EmgFront);
            Components.Add(EmgLeft);
            Components.Add(EmgRear);
            Components.Add(OpEmgOn);
            Components.Add(StartLamp);
            Components.Add(StopLamp);
            Components.Add(ResetLamp);
            Components.Add(TlRed);
            Components.Add(TlYellow);
            Components.Add(TlGreen);
            Components.Add(Buzzer);
        }

        // ─── 헬퍼 메서드 ─────────────────────────────────────────────

        /// <summary>신호탑 — 운전 중(녹색). 노랑/빨강 OFF.</summary>
        public void TowerLampRunning()
        {
            TlGreen.On(); TlYellow.Off(); TlRed.Off();
        }

        /// <summary>신호탑 — 경고(노란색). 녹색/빨강 OFF.</summary>
        public void TowerLampWarning()
        {
            TlGreen.Off(); TlYellow.On(); TlRed.Off();
        }

        /// <summary>신호탑 — 알람(빨간색). 녹색/노랑 OFF + 부저 ON.</summary>
        public void TowerLampAlarm()
        {
            TlGreen.Off(); TlYellow.Off(); TlRed.On();
            Buzzer.On();
        }

        /// <summary>신호탑 OFF + 부저 OFF.</summary>
        public void TowerLampOff()
        {
            TlGreen.Off(); TlYellow.Off(); TlRed.Off();
            Buzzer.Off();
        }

        /// <summary>운전자 램프 — Start ON / Stop OFF.</summary>
        public void OpLampReady()
        {
            StartLamp.On(); StopLamp.Off(); ResetLamp.Off();
        }

        /// <summary>운전자 램프 — Stop ON / Start OFF.</summary>
        public void OpLampStopped()
        {
            StartLamp.Off(); StopLamp.On(); ResetLamp.Off();
        }
    }
}
