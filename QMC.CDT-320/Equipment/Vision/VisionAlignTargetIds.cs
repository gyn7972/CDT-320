namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Handler-side SSOT for wafer alignment target keys.
    /// These are handler policy keys, not Vision PC tool ids.
    /// The mapping below resolves each target key to the actual QMC.Vision finder id.
    /// </summary>
    public static class VisionAlignTargetIds
    {
        public const string Center = "Center";
        public const string CenterVerify = "CenterVerify";
        public const string Ref1 = "Ref1";
        public const string Ref2 = "Ref2";
        public const string Ref1Ref2 = "Ref1Ref2";
        public const string InputPickDie = "InputPickDie";

        public static string ResolveWaferFinder(string alignTargetId)
        {
            switch (alignTargetId)
            {
                case Center:
                    return VisionToolIds.Wafer.AlignDieFinder;
                case Ref1:
                    return VisionToolIds.Wafer.FirstReferenceFinder;
                case Ref2:
                    return VisionToolIds.Wafer.SecondReferenceFinder;
                case InputPickDie:
                    return VisionToolIds.Wafer.DieFinder;
                default:
                    return alignTargetId;
            }
        }
    }
}
