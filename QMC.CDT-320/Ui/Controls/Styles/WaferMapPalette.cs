using System.Drawing;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 웨이퍼맵 공통 팔레트 (UI 표준 QMC-CDT320-UI-STD-001 §5.7).
    /// 렌더러(DieMapView/LiveLotMapView)와 페이지 레전드가 공유하는 "의미색"만 정의한다.
    /// 작업 모니터는 이 팔레트로 상태를 표시하고, BIN 번호는 원본 데이터로 별도 보존한다.
    /// 색 상수뿐인 정적 클래스로, 시퀀스/인터락과 무관하다.
    /// </summary>
    public static class WaferMapPalette
    {
        public static readonly Color ViewBackground = Color.FromArgb(0xDD, 0xDD, 0xDD);

        // ── 다이 진행 상태 (여정: 대기 → 비전 → 픽업/안착) ──
        public static readonly Color Wait      = Color.FromArgb(0xCC, 0xDD, 0xEE); // 검사 대기 (기존 다수파 값 유지)
        public static readonly Color Vision    = Color.FromArgb(0xF5, 0xA6, 0x23); // 검사 완료 — PrimaryBright (구 F2C14E·F5BE34 흡수)
        public static readonly Color PickerHeld = Color.FromArgb(0x8E, 0x63, 0xCE); // 픽커 보유 — 보라색
        public static readonly Color Placed = Color.FromArgb(0x44, 0x88, 0xCC);     // 안착 완료 / 검사 전 — 파란색
        public static readonly Color PickPlace = Placed; // 기존 화면 호환용. 작업 모니터는 두 상태를 구분한다.

        // 작업 모니터의 결과색은 BIN 설정과 분리한다. 저장된 BIN/Result 값은 바꾸지 않는다.
        public static readonly Color Good = Color.FromArgb(0x33, 0xB2, 0x6B);
        public static readonly Color Ng = Color.Firebrick;
        public static readonly Color NgFallback = Ng;

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
