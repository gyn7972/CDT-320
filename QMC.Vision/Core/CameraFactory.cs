using System.Collections.Generic;
using QMC.Vision.Cameras.Hik;
using QMC.Vision.Cameras.Sim;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 카메라 검색 + 생성. 벤더별 구현을 한 곳에서 관리.
    /// 현재 지원: HIKVISION GigE + Sim. USB3 / Basler / Cognex 등 추후 추가.
    /// </summary>
    public static class CameraFactory
    {
        /// <summary>연결된 카메라 전체 검색 (Hik + Sim).</summary>
        public static List<CameraInfo> EnumerateAll()
        {
            var list = new List<CameraInfo>();
            list.AddRange(HikGigECamera.Enumerate());
            list.AddRange(SimCamera.Enumerate());
            return list;
        }

        /// <summary>Info 기반 카메라 인스턴스 생성 (미개장).</summary>
        public static ICamera Create(CameraInfo info)
        {
            if (info == null) return new SimCamera("Sim/0");
            switch (info.Transport)
            {
                case CameraTransport.GigE: return new HikGigECamera(info);
                case CameraTransport.Sim:  return new SimCamera(info.Id ?? "Sim/0");
                default:                   return new SimCamera("Sim/0"); // Usb3 등 미지원
            }
        }

        /// <summary>ID(IP 또는 "Sim/x") 문자열로 카메라 생성. SDK 미설치/장치 미발견 시 Sim fallback.</summary>
        public static ICamera CreateById(string id)
        {
            if (string.IsNullOrEmpty(id)) return new SimCamera("Sim/0");
            if (id.StartsWith("Sim/")) return new SimCamera(id);

            // HIK: 사용자가 IP(xxx.xxx.xxx.xxx) 로 지정했다고 가정.
            // SDK 로드 여부 + enum 결과로 매칭 시도.
            foreach (var i in HikGigECamera.Enumerate())
                if (i.Id == id) return new HikGigECamera(i);

            // 찾지 못하면 Info 최소 정보만으로 시도 (SDK 로드 시 실패할 가능성).
            if (HikMvsDll.IsLoaded)
                return new HikGigECamera(new CameraInfo { Id = id, IpAddress = id, Transport = CameraTransport.GigE, Vendor = "HIKVISION" });

            // Fallback
            return new SimCamera(id);
        }
    }
}
