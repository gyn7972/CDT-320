using System;
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

        static HikMvsDll() { TryLoad(); }

        public static void TryLoad()
        {
            if (IsLoaded) return;
            EnsureGenTLSearchPath();
            // 강타입 reference (MvCameraControl.Net.dll 은 출력 폴더에 항상 동반 배포).
            // LoadFrom 컨텍스트 불일치로 인한 캐스팅 실패를 막기 위해 typeof 로 타입을 얻는다.
            // native MvCameraControl.dll 미설치 시에도 어셈블리 로드는 성공하고,
            // 실제 호출(Enum/Open) 시점에만 SDK 오류가 반환된다(→ Sim fallback).
            try
            {
                MyCameraType = typeof(MvCamCtrl.NET.MyCamera);
                Assembly     = MyCameraType.Assembly;
                DeviceInfoListType  = Assembly.GetType("MvCamCtrl.NET.MyCamera+MV_CC_DEVICE_INFO_LIST")
                                   ?? Assembly.GetType("MvCamCtrl.NET.CameraParams+MV_CC_DEVICE_INFO_LIST")
                                   ?? Assembly.GetType("MvCamCtrl.NET.MV_CC_DEVICE_INFO_LIST");
                DeviceInfoType      = Assembly.GetType("MvCamCtrl.NET.MyCamera+MV_CC_DEVICE_INFO")
                                   ?? Assembly.GetType("MvCamCtrl.NET.CameraParams+MV_CC_DEVICE_INFO")
                                   ?? Assembly.GetType("MvCamCtrl.NET.MV_CC_DEVICE_INFO");
                Version  = "HIK MVS " + (Assembly.GetName().Version?.ToString() ?? "(unknown)");
                IsLoaded = MyCameraType != null;
                if (!IsLoaded) LoadError = "MyCamera type not found in assembly";
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                LoadError = ex.Message;
            }
        }

        /// <summary>
        /// GenTL producer(.cti) 검색 경로(GENICAM_GENTL64/32_PATH) 맨 앞에 exe 폴더를 추가.
        /// <para>프레임그래버/GenTL("OtherDevice") 카메라는 이 producer 로 발견되는데, x64 전환 후
        /// producer 와 그 의존 DLL 이 exe 폴더에 co-locate 되어야 로드된다(post-build 가 *.dll/*.cti 복사).
        /// 미설정 시 GenTL enum 이 0x8000000C(MV_E_LOAD_LIBRARY) → 카메라 미검출.</para>
        /// </summary>
        private static void EnsureGenTLSearchPath()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory?.TrimEnd('\\');
                if (string.IsNullOrEmpty(baseDir)) return;
                string varName = Environment.Is64BitProcess ? "GENICAM_GENTL64_PATH" : "GENICAM_GENTL32_PATH";
                string cur = Environment.GetEnvironmentVariable(varName) ?? "";
                if (cur.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase)) return; // 이미 맨 앞
                Environment.SetEnvironmentVariable(varName, baseDir + ";" + cur);
            }
            catch { }
        }

        /// <summary>SDK 로드 실패 시 호출자에게 안내 문자열 제공.</summary>
        public static string GetInstallHint()
            => "HIKVISION MVS SDK 가 필요합니다.\n" +
               "설치 후 MvCameraControl.Net.dll / MvCameraControl.dll 을 프로그램 폴더에 배치하세요.\n" +
               "현재: " + (IsLoaded ? Version : ("실패 — " + LoadError));
    }
}
