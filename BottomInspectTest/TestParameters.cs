using System.ComponentModel;

namespace QMC.BottomInspectTest
{
    /// <summary>
    /// Bottom 표면 검사 테스트 파라미터 — 우측 PropertyGrid 에 바인딩되어 사용자가 편집한다.
    /// BottomInspectionParameter / VisionConfig(BottomVision 픽셀사이즈) 항목을 1:1 로 노출.
    /// </summary>
    public class TestParameters
    {
        // ── 검사 기본 ──
        [Category("1. 검사"), DisplayName("Threshold (0~255)"), Description("칩/배경 이진화 임계값")]
        public int Threshold { get; set; } = 128;

        [Category("1. 검사"), DisplayName("Dark Chip"), Description("칩이 배경보다 어두우면 true")]
        public bool DarkChip { get; set; } = false;

        [Category("1. 검사"), DisplayName("Chipping Depth (mm)")]
        public double ChippingDepth { get; set; } = 0.020;

        [Category("1. 검사"), DisplayName("Chipping Length (mm)")]
        public double ChippingLength { get; set; } = 0.050;

        [Category("1. 검사"), DisplayName("Foreign Object Size (mm)")]
        public double ForeignObjectSize { get; set; } = 0.5;

        // ── 칩 스펙 ──
        [Category("2. 칩 스펙"), DisplayName("Width Lower (mm)")]
        public double ChipWidthLower { get; set; } = 0;

        [Category("2. 칩 스펙"), DisplayName("Width Upper (mm)")]
        public double ChipWidthUpper { get; set; } = 0;

        [Category("2. 칩 스펙"), DisplayName("Height Lower (mm)")]
        public double ChipHeightLower { get; set; } = 0;

        [Category("2. 칩 스펙"), DisplayName("Height Upper (mm)")]
        public double ChipHeightUpper { get; set; } = 0;

        // ── 외곽/피크 검출 ──
        [Category("3. 외곽 검출"), DisplayName("First Peek Value Threshold")]
        public double FirstPeekValueThreshold { get; set; } = 230;

        [Category("3. 외곽 검출"), DisplayName("Peek Value Threshold")]
        public double PeekValueThreshold { get; set; } = 40;

        [Category("3. 외곽 검출"), DisplayName("Stdev")]
        public double Stdev { get; set; } = 0.01;

        // ── 이물(Black-Hat) ──
        [Category("4. 이물"), DisplayName("TopHat Radius (px)")]
        public int TopHatRadius { get; set; } = 21;

        [Category("4. 이물"), DisplayName("TopHat Threshold")]
        public int TopHatThreshold { get; set; } = 30;

        [Category("4. 이물"), DisplayName("Min Foreign Area (px)")]
        public int MinForeignAreaFilterSize { get; set; } = 36;

        [Category("4. 이물"), DisplayName("Link Distance (px)")]
        public int LinkDistance { get; set; } = 20;

        [Category("4. 이물"), DisplayName("Potential Defect Min Size (px)")]
        public double PortentiolDefactMinSize { get; set; } = 20;

        [Category("4. 이물"), DisplayName("오염(Contamination) 검사 사용")]
        public bool UseContaminationInspection { get; set; } = true;

        // ── 스케일 ──
        [Category("5. 스케일"), DisplayName("PixelSize W (mm/px, 1배 기준)"), Description("BottomVision 픽셀사이즈 — 검사부는 내부에서 ÷2(2배 확장 이미지 기준)로 사용")]
        public double PixelSizeWidthMm { get; set; } = 0.001399356618;

        [Category("5. 스케일"), DisplayName("PixelSize H (mm/px, 1배 기준)")]
        public double PixelSizeHeightMm { get; set; } = 0.001399356618;

        // ── 실행 옵션 ──
        [Category("6. 실행"), DisplayName("입력 2배 확장 후 검사"), Description("파일이 1배 원본이면 true — 소프트웨어 2배 확장 후 검사(검사부의 2배 확장 이미지 규약).\r\n장비가 저장한 검사 이미지는 이미 2배 공간이므로 false.")]
        public bool ExpandInput2x { get; set; } = false;

        [Category("6. 실행"), DisplayName("검사부 이미지 저장 사용"), Description("BottomInspect 내부의 결과/디펙 이미지 저장을 켤지(느려짐)")]
        public bool SaveInspectImages { get; set; } = false;
    }
}
