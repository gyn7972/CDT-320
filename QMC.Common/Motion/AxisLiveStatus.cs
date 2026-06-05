using QMC.Common.Motion.Ajin;

namespace QMC.Common.Motion
{
    /// <summary>
    /// ???? ??? ???? ?? ?? ?.<br/>
    /// ???? ??/??? ? ?? ???(<see cref="AxisSetup"/>/<see cref="AxisConfig"/>)?? ????.
    /// UI(CONFIG ??)?? ?? ?????? ????.
    /// </summary>
    public class AxisLiveStatus
    {
        // ?? CONFIG ??
        public AXM.MotorOutputMethod OutputMethod { get; set; }
        public AXM.EncoderInputMethod EncoderMethod { get; set; }
        public ActiveLevel ZPhaseLevel { get; set; }
        public ActiveLevel ServoOnLevel { get; set; }
        public double MaxVelocity { get; set; }
        public double MoveUnit { get; set; }
        public int PulsePerUnit { get; set; }

        // ?? INPOSITION ??
        public bool InPositionEnabled { get; set; }
        public ActiveLevel InPositionLevel { get; set; }
        public bool InPositionValue { get; set; }

        // ?? LIMIT ??
        public MotorEventAction PositiveLimitAction { get; set; }
        public ActiveLevel PositiveLimitLevel { get; set; }
        public bool PositiveLimitValue { get; set; }
        public MotorEventAction NegativeLimitAction { get; set; }
        public ActiveLevel NegativeLimitLevel { get; set; }
        public bool NegativeLimitValue { get; set; }
        public double SoftLimitPositive { get; set; }
        public double SoftLimitNegative { get; set; }

        // ?? EMERGENCY ??
        public ActiveLevel AmpFaultLevel { get; set; }
        public bool AmpFaultValue { get; set; }

        // ?? HOME ??
        public ActiveLevel HomeSensorLevel { get; set; }
        public bool HomeSensorValue { get; set; }

        // ?? ALARM ??
        public ActiveLevel AmpResetLevel { get; set; }
        public bool IsAlarm { get; set; }
        public uint AlarmCode { get; set; }

        // ?? POSITION ??
        public double ActualPosition { get; set; }
        public double CommandPosition { get; set; }
        public double PositionError { get; set; }
    }
}
