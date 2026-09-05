using System.Drawing;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 웨이퍼맵 공통 팔레트 (UI 표준 QMC-CDT320-UI-STD-001 §5.7).
    /// 렌더러(DieMapView/LiveLotMapView)와 페이지 레전드가 공유하는 "의미색"만 정의한다.
    /// BIN 레벨 색(Good/NG 실물 다이)은 BinCodeMap(레시피 데이터)이 원본 — 여기서 정의하지 않는다.
    /// 색 상수뿐인 정적 클래스로, 시퀀스/인터락과 무관하다.
    /// </summary>
    public static class WaferMapPalette
    {
        // ── 다이 진행 상태 (여정: 대기 → 비전 → 픽업/안착) ──
        public static readonly Color Wait      = Color.FromArgb(0xCC, 0xDD, 0xEE); // 검사 대기 (기존 다수파 값 유지)
        public static readonly Color Vision    = Color.FromArgb(0xF5, 0xA6, 0x23); // 검사 완료 — PrimaryBright (구 F2C14E·F5BE34 흡수)
        public static readonly Color PickPlace = Color.FromArgb(0x44, 0x88, 0xCC); // 픽업/안착 완료 — 청색 (구 24B86A: Good 초록과 분리)

        // ── 결과 표기 — 레전드/폴백 전용 (실물 다이는 BinCodeMap 색) ──
        public static readonly Color NgFallback = Color.Firebrick;                 // AlarmRed (구 IndianRed·D65B5B 흡수)

        // ── 마커/보조 상태 ──
        public static readonly Color StartMarker = Color.FromArgb(0xF5, 0xA6, 0x23); // 시작 다이 마커 (RunReview "S")
        public static readonly Color Skip        = Color.FromArgb(0x55, 0x55, 0x55); // 제외/스킵 — G600 (구 3C3C3C·5A5A5A·666666 흡수)
        public static readonly Color Unknown     = Color.FromArgb(80, 80, 100);      // 미기록 (기존 값 유지)

        // ── 선택/호버 강조 (테두리 펜) ──
        public static readonly Color Selection = Color.FromArgb(0xD9, 0x77, 0x06); // Primary (구 DeepSkyBlue·LimeGreen 통일)
        public static readonly Color Hover     = Color.FromArgb(0x00, 0xBC, 0xD4); // InfoCyan (구 Yellow — Vision 노랑과 충돌 해소)

        // ── 웨이퍼 외곽 ──
        public static readonly Color WaferOutline = Color.FromArgb(0x8F, 0x9C, 0xAD); // 구 4682DC·B4C4D8·4488CC 통일

        /// <summary>배경 밝기에 따른 축(크로스헤어) 색 — 구 Gold(FFD700) 대체 중립색.</summary>
        public static Color AxisFor(Color background)
        {
            int brightness = background.R + background.G + background.B;
            return brightness > 420
                ? Color.FromArgb(190, 0x80, 0x80, 0x80)   // 밝은 배경: G500 반투명
                : Color.FromArgb(190, 0x8F, 0x9C, 0xAD);  // 어두운 배경: 회청 반투명
        }
    }
}
