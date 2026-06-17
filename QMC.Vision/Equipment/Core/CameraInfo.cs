using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>카메라 장치 식별 정보.</summary>
    public class CameraInfo
    {
        /// <summary>고유 식별자 — 디바이스 IP(GigE) 또는 시리얼 번호(USB).</summary>
        public string Id            { get; set; }
        public string Model         { get; set; }
        public string Vendor        { get; set; }
        public string SerialNumber  { get; set; }
        /// <summary>GigE 카메라 IP. USB 이면 빈 값.</summary>
        public string IpAddress     { get; set; }
        public string MacAddress    { get; set; }
        /// <summary>GigE 카메라의 사용자 정의 이름(chUserDefinedName). 설정 안 된 경우 빈 문자열.</summary>
        public string UserDefinedName { get; set; }
        /// <summary>CameraTransport.GigE / USB3 / SIM</summary>
        public CameraTransport Transport { get; set; }
        public Size  MaxResolution  { get; set; }

        public override string ToString()
            => $"[{Transport}] {Vendor} {Model} ({Id})";
    }

    public enum CameraTransport { Sim, GigE, Usb3, CameraLink, CoaXPress }

    /// <summary>카메라 트리거 모드.</summary>
    public enum CameraTriggerMode
    {
        /// <summary>자유 실행 (연속 촬영).</summary>
        Continuous,
        /// <summary>소프트웨어 트리거 — <c>TriggerSoftware()</c> 호출 시 1장.</summary>
        Software,
        /// <summary>하드웨어 라인 0 (보통 Expose 입력 신호).</summary>
        Line0,
        Line1,
        Line2
    }

    public enum CameraPixelFormat { Mono8, Mono10, Mono12, BayerRG8, BayerGB8, BGR8, RGB8 }
}
