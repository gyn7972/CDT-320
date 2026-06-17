namespace QMC.Common.Logging
{
    /// <summary>
    /// 메시지 카탈로그 1건. 코드(Code)와 설명(Ko/En)은 항상 분리해서 보관한다.
    /// UI 에서는 Code 와 Ko 를 한 칸에 합쳐 보여줄 수 있으나, 저장은 분리된 필드로 한다.
    /// </summary>
    public class MessageDefinition
    {
        public string    Code { get; set; } = "";
        public EventKind Kind { get; set; } = EventKind.Event;
        public string    Ko   { get; set; } = "";
        public string    En   { get; set; } = "";

        /// <summary>언어에 맞는 설명. en 이 비어 있으면 한국어로 폴백.</summary>
        public string Text(string lang)
        {
            return (lang == "en" && !string.IsNullOrEmpty(En)) ? En : Ko;
        }
    }
}
