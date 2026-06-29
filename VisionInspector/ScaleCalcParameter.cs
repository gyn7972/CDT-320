namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 스케일 계산 파라미터 클래스 (InspectionParameterBase 상속)
    /// Chip의 mm 단위 Width, Height 포함
    /// </summary>
    public class ScaleCalcParameter : InspectionParameterBase
    {
        /// <summary>
        /// 칩의 실제(mm) 단위 너비
        /// </summary>
        public double ChipWidthMm { get; set; }

        /// <summary>
        /// 칩의 실제(mm) 단위 높이
        /// </summary>
        public double ChipHeightMm { get; set; }
    }
}