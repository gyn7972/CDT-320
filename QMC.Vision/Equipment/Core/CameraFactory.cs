using System.Collections.Generic;
using QMC.Vision.Cameras.Hik;
using QMC.Vision.Cameras.Mil;
using QMC.Vision.Cameras.Sim;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 카메라 검색 + 생성. 벤더별 구현을 한 곳에서 관리.
    /// 현재 지원: HIKVISION GigE + Matrox MIL(CL/CXP) + Sim. USB3 / Basler 등 추후 추가.
    /// </summary>
    public static class CameraFactory
    {
        /// <summary>연결된 카메라 전체 검색 (Hik + MIL + Sim).</summary>
        public static List<CameraInfo> EnumerateAll()
        {
            var list = new List<CameraInfo>();
            list.AddRange(HikGigECamera.Enumerate());
            list.AddRange(MilCamera.Enumerate());
            list.AddRange(SimCamera.Enumerate());
            return list;
        }

        /// <summary>Info 기반 카메라 인스턴스 생성 (미개장).</summary>
        public static ICamera Create(CameraInfo info)
        {
            if (info == null) return new SimCamera("Sim/0");
            // MIL 미가용(미설치/보드 없음)이면 Id 를 유지한 SimCamera 로 대체 — 개인 PC 에서도
            // 실기와 동일한 "Mil/n" 설정/레시피를 그대로 쓰게 한다(Sim=Real). 보드가 있으면 기존 경로.
            if (!string.IsNullOrEmpty(info.Id) && info.Id.StartsWith("Mil/"))
                return MilCamera.IsMilAvailable ? (ICamera)new MilCamera(info) : new SimCamera(info.Id);
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
            // Matrox MIL(CL/CXP): "Mil/n". MIL 미가용(미설치/보드 없음)이면 Id 를 유지한 SimCamera 로 대체
            //   (개인 PC Sim 환경 — 실기와 동일 설정/DCF UI 유지). 보드는 있는데 카메라 미연결이면
            //   기존대로 MilCamera 생성 → Open 실패 → 알람(실기 진단 경로 유지).
            if (id.StartsWith("Mil/"))
                return MilCamera.IsMilAvailable
                    ? (ICamera)new MilCamera(new CameraInfo { Id = id, Vendor = "Matrox", Transport = CameraTransport.CoaXPress })
                    : new SimCamera(id);

            // HIK: 사용자가 IP(xxx.xxx.xxx.xxx) 로 지정했다고 가정.
            // SDK 로드 여부 + enum 결과로 매칭 시도.
            foreach (var i in HikGigECamera.Enumerate())
                if (i.Matches(id)) return new HikGigECamera(i);   // 고유이름/IP/시리얼 매칭

            // 찾지 못했고 저장값이 IP 형태면 그 IP 로 직접 시도(SDK 로드 시). 저장값이 고유이름인데
            // 열거에 없으면 = 네트워크에 없음 → 아래 Sim fallback(온라인 되면 재열거 시 Matches 로 잡힘).
            if (HikMvsDll.IsLoaded && CameraInfo.LooksLikeIp(id))
                return new HikGigECamera(new CameraInfo { Id = id, IpAddress = id, Transport = CameraTransport.GigE, Vendor = "HIKVISION" });

            // Fallback
            return new SimCamera(id);
        }
    }
}
