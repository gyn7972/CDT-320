using System.Collections.Generic;
using System.Drawing;

namespace QMC.Vision.Inspector
{
    public class InspectionParameterBase
    {
        // 칩 타입 (화이트/블랙)
        public enum ChipType
        {
            White,
            Black
        }

        // 칩 타입 선택 필드 및 프로퍼티
        private ChipType _selectedChipType;
        // 스레숄드
        private int _threshold;
        private Rectangle _chipRoi;

        public Rectangle ChipRoi
        {
            get => _chipRoi;
            set => _chipRoi = value;
        }
        // 이미지 리스트
        public List<byte[]> Images { get; set; } = new List<byte[]>();
        public int ImageWidth { get; set; } = 0;
        public int ImageHeight { get; set; } = 0;
        public bool IsSaveGoodImage { get; set; } = true;
        public ChipType SelectedChipType
        {
            get => _selectedChipType;
            set => _selectedChipType = value;
        }
        public int Threshold
        {
            get => _threshold;
            set => _threshold = value;
        }

        public int IndexX { get; set; } = 0; // X 인덱스 (기본값 0)
        public int IndexY { get; set; } = 0; // Y 인덱스 (기본값 0)

        public string WaferID { get; set; } = "Empty";
        public int ColletID { get; set; } = 0; // 콜렛 ID (기본값 0)
    }
}