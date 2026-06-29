using QMC.CDT320;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Equipment.Vision
{
    /// <summary>
    /// Vision 뷰어(이미지 스트림) 포트의 단일 해석(SSOT). 명령 채널과 별개 포트.
    /// <para>설정(<see cref="AppSettingsStore"/>, "VISION 연결" 페이지)에서 읽는다. 값이 없으면 기본값으로 보정한다.</para>
    /// </summary>
    public static class VisionViewerPorts
    {
        /// <summary>기본값. 설정값이 없거나 잘못되면 이 포트로 보정한다.</summary>
        public const int DefaultWafer = 5200;
        public const int DefaultBottomInspection = 5201;
        public const int DefaultBin = 5203;
        public const int DefaultTopSide = 5205;
        public const int DefaultBottomSide = 5206;

        public static int Wafer { get { return Resolve(Cfg.VisionWaferViewerPort, DefaultWafer); } }
        public static int BottomInspection { get { return Resolve(Cfg.VisionInspectionViewerPort, DefaultBottomInspection); } }
        public static int Bin { get { return Resolve(Cfg.VisionBinViewerPort, DefaultBin); } }
        public static int TopSide { get { return Resolve(Cfg.VisionTopSideViewerPort, DefaultTopSide); } }
        public static int BottomSide { get { return Resolve(Cfg.VisionBottomSideViewerPort, DefaultBottomSide); } }

        private static AppSettings Cfg { get { return AppSettingsStore.Current; } }

        public static int ResolveByChannel(AutoVisionChannel channel)
        {
            switch (channel)
            {
                case AutoVisionChannel.Wafer:
                    return Wafer;
                case AutoVisionChannel.BottomInspection:
                    return BottomInspection;
                case AutoVisionChannel.Bin:
                    return Bin;
                case AutoVisionChannel.FrontSide:
                    return TopSide;
                case AutoVisionChannel.RearSide:
                    return BottomSide;
                default:
                    return 0;
            }
        }

        private static int Resolve(int value, int fallback)
        {
            return value > 0 && value < 65536 ? value : fallback;
        }
    }
}
