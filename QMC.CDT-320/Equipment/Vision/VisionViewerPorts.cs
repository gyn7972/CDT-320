namespace QMC.CDT_320.Equipment.Vision
{
    /// <summary>
    /// Vision 뷰어(이미지 스트림) 포트의 단일 정의(SSOT). 명령 채널과 별개 포트.
    /// Vision 측 GrabStreamServer가 listen, 핸들러는 VisionFrameClient로 접속한다.
    /// </summary>
    public static class VisionViewerPorts
    {
        public const int Wafer            = 5200;
        public const int BottomInspection = 5201;
        public const int Bin              = 5203;
        public const int TopSide          = 5205;
        public const int BottomSide       = 5206;

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
