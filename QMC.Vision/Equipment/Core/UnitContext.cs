using System.Collections.Generic;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 전역 측정 단위(mm/px) 컨텍스트 — GENERAL 설정(VisionSettings.DisplayMm)을 단일 진입점으로 노출하고,
    /// 검사기 픽셀스케일을 카메라 ScaleX/Y 로 주입한다.
    /// 원칙: 검사기는 항상 카메라 ScaleX/Y(mm/px)로 mm 계산·판정한다(레시피 한계도 mm 기준).
    /// 표시 단위가 px 이면 화면 표시 시에만 mm→px 로 환산(판정/한계는 mm 그대로) → 단위 토글에도 한계 재입력 불필요.
    /// </summary>
    public static class UnitContext
    {
        /// <summary>전역 표시 단위. true=mm, false=px. VisionSettings.DisplayMm 미연결 시 mm 기본.</summary>
        public static bool DisplayMm
        {
            get { try { return QMC.Vision.Config.VisionConfigStore.Current?.DisplayMm ?? true; } catch { return true; } }
        }

        public static string UnitLabel => DisplayMm ? "mm" : "px";

        // 모드(Bottom/Side/Bin)별 카메라 스케일(mm/px) — 검사 실행 시 기록, 표시 환산(mm→px)에 사용.
        private static readonly Dictionary<string, double[]> _modeScale =
            new Dictionary<string, double[]>(System.StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        public static void SetModeScale(string mode, double sx, double sy)
        {
            if (string.IsNullOrEmpty(mode)) return;
            lock (_lock) { _modeScale[mode] = new[] { sx > 0 ? sx : 1.0, sy > 0 ? sy : 1.0 }; }
        }

        public static void GetModeScale(string mode, out double sx, out double sy)
        {
            sx = 1.0; sy = 1.0;
            lock (_lock) { if (mode != null && _modeScale.TryGetValue(mode, out var s)) { sx = s[0]; sy = s[1]; } }
        }

        /// <summary>검사기에 카메라 스케일(mm/px)을 주입 — 검사기는 항상 mm로 계산(판정 mm 일관).
        /// scale<=0(미보정)이면 1.0 → mm=px 가 되어 px 수치로 동작(안전 폴백).</summary>
        public static void ApplyScale(IInspector ins, double scaleX, double scaleY)
        {
            double sx = scaleX > 0 ? scaleX : 1.0, sy = scaleY > 0 ? scaleY : 1.0;
            if (ins is BottomInspector b) { b.PixelSizeWidthMm = sx; b.PixelSizeHeightMm = sy; }
            else if (ins is PlacementGapInspector p) { p.PixelSizeXmm = sx; p.PixelSizeYmm = sy; }
            else if (ins is SideAppearanceInspector s) { s.PixelSizeWidthMm = sx; s.PixelSizeHeightMm = sy; }
        }

        /// <summary>mm 값 → 표시 단위 값. mm 모드면 그대로, px 모드면 sx(또는 sy)로 나눠 px 로 환산.</summary>
        public static double ToDisplay(double mmValue, double scale)
            => (DisplayMm || scale <= 0) ? mmValue : mmValue / scale;
    }
}
