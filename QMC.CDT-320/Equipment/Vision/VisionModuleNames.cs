using System;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Handler와 VisionPC가 공통으로 사용하는 Vision 명령 채널 모듈명 해석 기준.
    /// 모듈명 문자열은 여기 상수를 기준으로 사용한다.
    /// </summary>
    public static class VisionModuleNames
    {
        public const string Wafer = "WaferVision";
        public const string BottomInspection = "BottomInspection";
        public const string Bin = "BinVision";
        public const string Main = "MainComm";
        public const string FrontSide = "FrontSideVision";
        public const string RearSide = "RearSideVision";

        public static string ResolveByChannel(AutoVisionChannel channel)
        {
            switch (channel)
            {
                case AutoVisionChannel.Wafer:
                    return Wafer;
                case AutoVisionChannel.BottomInspection:
                    return BottomInspection;
                case AutoVisionChannel.Bin:
                    return Bin;
                case AutoVisionChannel.Main:
                    return Main;
                case AutoVisionChannel.FrontSide:
                    return FrontSide;
                case AutoVisionChannel.RearSide:
                    return RearSide;
                default:
                    return string.Empty;
            }
        }

        public static AutoVisionChannel ResolveChannelByModule(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                return AutoVisionChannel.Wafer;

            switch (moduleName.Trim())
            {
                case Wafer:
                case "Wafer":
                    return AutoVisionChannel.Wafer;
                case BottomInspection:
                case "Inspection":
                case "Bottom":
                    return AutoVisionChannel.BottomInspection;
                case Bin:
                case "Bin":
                    return AutoVisionChannel.Bin;
                case Main:
                case "Main":
                    return AutoVisionChannel.Main;
                case FrontSide:
                case "FrontSide":
                    return AutoVisionChannel.FrontSide;
                case RearSide:
                case "RearSide":
                    return AutoVisionChannel.RearSide;
                default:
                    return AutoVisionChannel.Wafer;
            }
        }

        public static AutoVisionChannel ResolveByModule(string moduleName)
        {
            return ResolveChannelByModule(moduleName);
        }

        public static bool TryResolveChannelByModule(string moduleName, out AutoVisionChannel channel)
        {
            channel = AutoVisionChannel.Wafer;
            if (string.IsNullOrWhiteSpace(moduleName))
                return false;

            string trimmed = moduleName.Trim();
            switch (trimmed)
            {
                case Wafer:
                case "Wafer":
                    channel = AutoVisionChannel.Wafer;
                    return true;
                case BottomInspection:
                case "Inspection":
                case "Bottom":
                    channel = AutoVisionChannel.BottomInspection;
                    return true;
                case Bin:
                case "Bin":
                    channel = AutoVisionChannel.Bin;
                    return true;
                case Main:
                case "Main":
                    channel = AutoVisionChannel.Main;
                    return true;
                case FrontSide:
                case "FrontSide":
                    channel = AutoVisionChannel.FrontSide;
                    return true;
                case RearSide:
                case "RearSide":
                    channel = AutoVisionChannel.RearSide;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryResolveByModule(string moduleName, out AutoVisionChannel channel)
        {
            return TryResolveChannelByModule(moduleName, out channel);
        }
    }
}
