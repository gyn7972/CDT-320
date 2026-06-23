using QMC.CDT320;

namespace QMC.CDT_320.Equipment.Vision
{
    /// <summary>
    /// Vision 뷰어(이미지 스트림) 포트의 단일 해석(SSOT). 명령 채널과 별개 포트.
    /// <para>설정(<see cref="AppSettingsStore"/>, "VISION 연결" 페이지)에서 읽는다. 값이 없으면 기본값으로 보정.
    /// Vision 측 GrabStreamServer가 listen, 핸들러는 VisionFrameClient/VisionViewerSource로 접속한다.</para>
    /// </summary>
    public static class VisionViewerPorts
    {
        public const int DefaultWafer            = 5200;
        public const int DefaultBottomInspection = 5201;
        public const int DefaultBin              = 5203;
        public const int DefaultTopSide          = 5205;
        public const int DefaultBottomSide       = 5206;

        public static int Wafer            => Resolve(Cfg.VisionWaferViewerPort,      DefaultWafer);
        public static int BottomInspection => Resolve(Cfg.VisionInspectionViewerPort, DefaultBottomInspection);
        public static int Bin              => Resolve(Cfg.VisionBinViewerPort,        DefaultBin);
        public static int TopSide          => Resolve(Cfg.VisionTopSideViewerPort,    DefaultTopSide);
        public static int BottomSide       => Resolve(Cfg.VisionBottomSideViewerPort, DefaultBottomSide);

        private static AppSettings Cfg => AppSettingsStore.Current;

        private static int Resolve(int value, int fallback)
            => value > 0 && value < 65536 ? value : fallback;

        /// <summary>명령 채널 모듈명(VisionHub 기준)으로 뷰어 포트를 해석한다. 없으면 0.</summary>
        public static int ResolveByModule(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                return 0;

            switch (moduleName.Trim())
            {
                case "WaferVision":      return Wafer;
                case "BottomInspection": return BottomInspection;
                case "BinVision":        return Bin;
                case "TopSideVision":    return TopSide;
                case "BottomSideVision": return BottomSide;
                default:                 return 0;
            }
        }
    }
}
