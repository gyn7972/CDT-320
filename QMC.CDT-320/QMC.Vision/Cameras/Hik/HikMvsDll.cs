using System;
using System.IO;
using System.Reflection;

namespace QMC.Vision.Cameras.Hik
{
    /// <summary>
    /// HIKVISION MVS SDK(<c>MvCameraControl.Net.dll</c>) 동적 로더.
    /// <para>
    /// 실 운용 시 SDK 설치 (MVS 4.0+) 후 <c>C:\Program Files (x86)\MVS\Development\Assemblies\</c>
    /// 에서 아래 DLL들을 exe 폴더로 배치 (또는 PATH 추가):
    ///   - MvCameraControl.Net.dll   (managed)
    ///   - MvCameraControl.dll       (native x86/x64)
    ///   - MvUsbDev.dll, MvGigEVisionSDK.dll ...
    /// </para>
    /// <para>
    /// 본 로더는 SDK 설치 여부를 검사하고, 설치된 경우 리플렉션 기반으로
    /// <c>MvCamCtrl.NET.MyCamera</c> 타입을 노출한다.
    /// </para>
    /// </summary>
    public static class HikMvsDll
    {
        public static bool     IsLoaded       { get; private set; }
        public static string   Version        { get; private set; } = "Not loaded";
        public static string   LoadError      { get; private set; }
        public static Assembly Assembly       { get; private set; }

        /// <summary>MyCamera 타입 (리플렉션용).</summary>
        public static Type MyCameraType  { get; private set; }

        /// <summary>디바이스 목록 구조체 (MV_CC_DEVICE_INFO_LIST).</summary>
        public static Type DeviceInfoListType { get; private set; }

        /// <summary>디바이스 개별 정보 (MV_CC_DEVICE_INFO).</summary>
        public static Type DeviceInfoType     { get; private set; }

        private static readonly string[] Candidates =
        {
            "MvCameraControl.Net.dll",
            "MvCameraControl.dll"
        };

        static HikMvsDll() { TryLoad(); }

        public static void TryLoad()
        {
            if (IsLoaded) return;
            foreach (var name in Candidates)
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
                if (!File.Exists(path)) continue;
                try
                {
                    Assembly = Assembly.LoadFrom(path);
                    MyCameraType        = Assembly.GetType("MvCamCtrl.NET.MyCamera") ?? Assembly.GetType("MVSDK_Net.MyCamera");
                    DeviceInfoListType  = Assembly.GetType("MvCamCtrl.NET.CameraParams+MV_CC_DEVICE_INFO_LIST") ?? Assembly.GetType("MvCamCtrl.NET.MV_CC_DEVICE_INFO_LIST");
                    DeviceInfoType      = Assembly.GetType("MvCamCtrl.NET.CameraParams+MV_CC_DEVICE_INFO") ?? Assembly.GetType("MvCamCtrl.NET.MV_CC_DEVICE_INFO");
                    Version  = "HIK MVS " + (Assembly.GetName().Version?.ToString() ?? "(unknown)");
                    IsLoaded = MyCameraType != null;
                    if (!IsLoaded) LoadError = "MyCamera type not found in assembly";
                    return;
                }
                catch (Exception ex)
                {
                    LoadError = ex.Message;
                }
            }
            LoadError = "MvCameraControl.Net.dll not found in exe folder";
        }

        /// <summary>SDK 로드 실패 시 호출자에게 안내 문자열 제공.</summary>
        public static string GetInstallHint()
            => "HIKVISION MVS SDK 가 필요합니다.\n" +
               "설치 후 MvCameraControl.Net.dll / MvCameraControl.dll 을 프로그램 폴더에 배치하세요.\n" +
               "현재: " + (IsLoaded ? Version : ("실패 — " + LoadError));
    }
}
