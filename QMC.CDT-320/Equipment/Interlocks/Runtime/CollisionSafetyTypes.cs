using System;

namespace QMC.CDT320.Interlocks
{
    public enum CollisionGateDecision
    {
        Allow,
        Wait,
        Block
    }

    public enum PickerSafetySide
    {
        Front,
        Rear
    }

    public enum PickerSafetyPhase
    {
        Idle,
        PickUp,
        BottomInspect,
        SideInspect,
        Place,
        Retracting
    }

    public enum PickerSafetyYState
    {
        Unknown,
        Retracted,
        Forward,
        Moving
    }

    public sealed class CollisionGateResult
    {
        public CollisionGateDecision Decision { get; private set; }
        public string Reason { get; private set; }

        public bool Allowed
        {
            get { return Decision == CollisionGateDecision.Allow; }
        }

        public static CollisionGateResult Allow()
        {
            return new CollisionGateResult
            {
                Decision = CollisionGateDecision.Allow,
                Reason = string.Empty
            };
        }

        public static CollisionGateResult Wait(string reason)
        {
            return new CollisionGateResult
            {
                Decision = CollisionGateDecision.Wait,
                Reason = reason ?? string.Empty
            };
        }

        public static CollisionGateResult Block(string reason)
        {
            return new CollisionGateResult
            {
                Decision = CollisionGateDecision.Block,
                Reason = reason ?? string.Empty
            };
        }
    }

    public sealed class PickerSafetySnapshot
    {
        public PickerSafetySide Side { get; set; }
        public PickerSafetyPhase Phase { get; set; }
        public PickerSafetyYState YState { get; set; }
        public bool Carrying { get; set; }
        public bool ForwardYPermitted { get; set; }
        public double XActual { get; set; }
        public double XCommand { get; set; }
        public bool XMoving { get; set; }
        public double YActual { get; set; }
        public double YCommand { get; set; }
        public bool YMoving { get; set; }

        public string Describe()
        {
            return Side +
                   " phase=" + Phase +
                   ", yState=" + YState +
                   ", carrying=" + Carrying +
                   ", forwardYPermitted=" + ForwardYPermitted +
                   ", xActual=" + XActual.ToString("0.###") +
                   ", xCommand=" + XCommand.ToString("0.###") +
                   ", xMoving=" + XMoving +
                   ", yActual=" + YActual.ToString("0.###") +
                   ", yCommand=" + YCommand.ToString("0.###") +
                   ", yMoving=" + YMoving;
        }
    }

    public sealed class AxisPairSafetySnapshot
    {
        public string PairName { get; set; }
        public bool Configured { get; set; }
        public double AxisAActual { get; set; }
        public double AxisBActual { get; set; }
        public double AxisACommand { get; set; }
        public double AxisBCommand { get; set; }
        public bool AxisAMoving { get; set; }
        public bool AxisBMoving { get; set; }
        public double HomeClearance { get; set; }
        public int AxisATowardSign { get; set; }
        public int AxisBTowardSign { get; set; }
        public double RequiredClearance { get; set; }
        public double ActualClearance { get; set; }
        public double TargetClearance { get; set; }
        public string RuleKind { get; set; }
        public string Reason { get; set; }

        public string Describe()
        {
            if (!Configured)
                return "pair=" + PairName + ", configured=False, reason=" + (Reason ?? string.Empty);

            if (string.Equals(RuleKind, "PickerYFacingDistance", StringComparison.OrdinalIgnoreCase))
            {
                return "pair=" + PairName +
                       ", configured=True" +
                       ", xDistance=" + ActualClearance.ToString("0.###") +
                       ", targetXDistance=" + TargetClearance.ToString("0.###") +
                       ", required=" + RequiredClearance.ToString("0.###") +
                       ", frontXActual=" + AxisAActual.ToString("0.###") +
                       ", rearXActual=" + AxisBActual.ToString("0.###") +
                       ", frontXCommand=" + AxisACommand.ToString("0.###") +
                       ", rearXCommand=" + AxisBCommand.ToString("0.###") +
                       ", frontXMoving=" + AxisAMoving +
                       ", rearXMoving=" + AxisBMoving +
                       ", ruleKind=" + (RuleKind ?? string.Empty);
            }

            return "pair=" + PairName +
                   ", configured=True" +
                   ", clearance=" + ActualClearance.ToString("0.###") +
                   ", targetClearance=" + TargetClearance.ToString("0.###") +
                   ", required=" + RequiredClearance.ToString("0.###") +
                   ", axisAActual=" + AxisAActual.ToString("0.###") +
                   ", axisBActual=" + AxisBActual.ToString("0.###") +
                   ", axisACommand=" + AxisACommand.ToString("0.###") +
                   ", axisBCommand=" + AxisBCommand.ToString("0.###") +
                   ", axisAMoving=" + AxisAMoving +
                   ", axisBMoving=" + AxisBMoving +
                   ", homeClearance=" + HomeClearance.ToString("0.###") +
                   ", signs=A:" + AxisATowardSign + ",B:" + AxisBTowardSign +
                   ", ruleKind=" + (RuleKind ?? string.Empty);
        }
    }

    public sealed class MotionSafetyState
    {
        public PickerSafetySnapshot Front { get; set; }
        public PickerSafetySnapshot Rear { get; set; }
        public AxisPairSafetySnapshot FrontRearPickerPair { get; set; }
        public DateTime CapturedUtc { get; set; }
    }
}
