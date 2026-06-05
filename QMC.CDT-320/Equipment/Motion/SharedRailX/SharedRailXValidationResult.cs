namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXValidationResult
    {
        public bool Allowed { get; set; }
        public string Reason { get; set; }

        public static SharedRailXValidationResult Allow()
        {
            return new SharedRailXValidationResult { Allowed = true, Reason = string.Empty };
        }

        public static SharedRailXValidationResult Block(string reason)
        {
            return new SharedRailXValidationResult { Allowed = false, Reason = reason ?? string.Empty };
        }
    }
}
