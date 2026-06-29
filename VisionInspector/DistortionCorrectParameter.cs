namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 왜곡 보정 검사 파라미터 클래스
    /// </summary>
    public class DistortionCorrectParameter : InspectionParameterBase
    {
        /// <summary>
        /// 검색 타입 (크로스 라인 또는 원)
        /// </summary>
        public enum SearchType
        {
            CrossLine,
            Circle
        }

        /// <summary>
        /// 검색할 타입 지정
        /// </summary>
        public SearchType TargetSearch { get; set; }

        /// <summary>
        /// 가로 점의 피치 (mm 단위)
        /// </summary>  
        public double PitchX { get; set; }
        /// <summary>
        /// 세로 점의 피치 (mm 단위)
        /// </summary>  
        
        public double PitchY { get; set; }

        public DistortionCorrectParameter()
        {
            // 기본값 설정
            TargetSearch = SearchType.Circle;
            PitchX = 1.0; // 기본 피치 X (mm)
            PitchY = 1.0; // 기본 피치 Y (mm)
        }
    }
}