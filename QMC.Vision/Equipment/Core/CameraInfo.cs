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

        /// <summary>저장·표시용 안정 식별자 — 고유이름(UserDefinedName) 우선, 없으면 Id(IP/시리얼).
        /// IP 는 DHCP/링크로컬에서 계속 바뀌므로 매핑 저장값으로는 UserDefinedName 을 쓴다.</summary>
        public string StableId
            => string.IsNullOrWhiteSpace(UserDefinedName) ? Id : UserDefinedName;

        /// <summary>주어진 식별자와 일치하는지 — UserDefinedName / IpAddress / SerialNumber / Id 중
        /// 하나라도 대소문자 무시 일치. IP 가 바뀌어도 고유이름으로 매칭되게 하는 핵심.</summary>
        public bool Matches(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            return Eq(UserDefinedName, id) || Eq(IpAddress, id) || Eq(SerialNumber, id) || Eq(Id, id);
        }

        private static bool Eq(string a, string b)
            => !string.IsNullOrWhiteSpace(a) &&
               string.Equals(a.Trim(), b.Trim(), System.StringComparison.OrdinalIgnoreCase);

        /// <summary>문자열이 IPv4(a.b.c.d) 형태인지 — 저장값이 이름인지 IP인지 판별용.</summary>
        public static bool LooksLikeIp(string s)
            => !string.IsNullOrWhiteSpace(s) && s.IndexOf('.') > 0
               && System.Net.IPAddress.TryParse(s.Trim(), out _);

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
