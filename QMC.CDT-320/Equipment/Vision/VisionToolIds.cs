namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Handler-side SSOT for Vision PC tool ids.
    /// These strings must match QMC.Vision module registrations exactly.
    /// Finder ids are used with MATCHASYNC/MATCHRESULT.
    /// Inspector ids are used with INSPECTASYNC/INSPECTRESULT.
    /// DryRun with a connected Vision channel sends GRAB only, then uses simulated/bypass results.
    ///
    /// Wire rule:
    ///   MODULE|COMMAND|tool|...
    ///   MODULE|INSPECTASYNC|inspector|fb|collet|die_index|channel|gridx;gridy
    ///   MODULE|MATCHASYNC|finder|fb|collet|die_index|channel|gridx;gridy
    /// Main separator is '|'. Composite grid separator is ';'. The result key is die_index.
    /// </summary>
    public static class VisionToolIds
    {
        public static class Wafer
        {
            public const string EjectPinFinder = "EjectPinFinder";
            public const string ReticleFinder = "ReticleFinder";
            public const string AlignDieFinder = "AlignDieFinder";
            public const string FirstReferenceFinder = "FirstReferenceFinder";
            public const string SecondReferenceFinder = "SecondReferenceFinder";
            public const string DieFinder = "DieFinder";
            public const string ScaleFinder = "ScaleFinder";
        }

        public static class Bin
        {
            public const string ReticleFinder = "ReticleFinder";
            public const string DieFinder = "DieFinder";
            public const string ScaleFinder = "ScaleFinder";
            public const string PlacementInspector = "PlacementInspector";
        }

        public static class BottomInspection
        {
            public const string ReticleFinder = "ReticleFinder";
            public const string ColletFinder = "ColletFinder";
            public const string DieFinder = "DieFinder";
            public const string SurfaceInspector = "SurfaceInspector";
            public const string FocusFinder = "FocusFinder";
            public const string ScaleFinder = "ScaleFinder";
            public const string COCInspector = "COCInspector";
            public const string DistortionCompensation = "DistortionCompensation";
        }

        public static class FrontSide
        {
            public const string DieEdgeFinder = "DieEdgeFinder";
            public const string FocusFinder = "FocusFinder";
            public const string SurfaceInspector = "FrontSurfaceInspector";
            public const string ChippingInspector = "FrontChippingInspector";
        }

        public static class RearSide
        {
            public const string DieEdgeFinder = "DieEdgeFinder";
            public const string FocusFinder = "FocusFinder";
            public const string SurfaceInspector = "RearSurfaceInspector";
            public const string ChippingInspector = "RearChippingInspector";
        }
    }
}
