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

        /// <summary>검사기에 주입된 카메라 스케일(mm/px)을 돌려준다(ApplyScale 로 주입된 값) — 결과 표시 환산용. 미주입/0 이면 1.</summary>
        public static void GetInspectorScale(IInspector ins, out double sx, out double sy)
        {
            sx = 1.0; sy = 1.0;
            if (ins is BottomInspector b)              { if (b.PixelSizeWidthMm > 0) sx = b.PixelSizeWidthMm; if (b.PixelSizeHeightMm > 0) sy = b.PixelSizeHeightMm; }
            else if (ins is PlacementGapInspector p)   { if (p.PixelSizeXmm     > 0) sx = p.PixelSizeXmm;     if (p.PixelSizeYmm      > 0) sy = p.PixelSizeYmm; }
            else if (ins is SideAppearanceInspector s) { if (s.PixelSizeWidthMm > 0) sx = s.PixelSizeWidthMm; if (s.PixelSizeHeightMm > 0) sy = s.PixelSizeHeightMm; }
        }

        // 검사 결과 항목명 → 길이(mm) 항목 축 맵 — 표시 단위(px) 환산 전용. 판정/저장/통신 값(mm)은 불변.
        // X축(가로 스케일): 좌/우 갭·Width·Offset X 등. Y축(세로 스케일): 상/하 갭·Height·Offset Y·칩핑 깊이 등.
        private static readonly HashSet<string> _lenX = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Right max", "Right min", "Left max", "Left min", "Right Gap Avg", "Left Gap Avg",
            "Offset X", "Width", "Chipping Left", "Chipping Right", "Chipping ch1", "Chipping ch2", "Foreign Max"
        };
        private static readonly HashSet<string> _lenY = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Top gap min", "Top gap max", "Bottom gap", "Bottom min", "Top Gap Avg", "Bottom Gap Avg",
            "Offset Y", "Height", "Chipping Top", "Chipping Bottom", "Max Chipping Depth"
        };

        /// <summary>결과 항목명이 길이(mm) 항목이면 true + 세로축 여부. Angle/Count/시간 등 비길이 항목은 false.</summary>
        public static bool TryGetLengthAxis(string name, out bool yAxis)
        {
            yAxis = false;
            if (string.IsNullOrEmpty(name)) return false;
            if (_lenY.Contains(name)) { yAxis = true; return true; }
            return _lenX.Contains(name);
        }

        /// <summary>검사 결과 항목 값(mm 문자열)을 전역 표시 단위 문자열로 — px 모드면 해당 축 스케일(mm/px)로 나눔.
        /// 비길이 항목·파싱 실패·스케일 미보정(≤0/1)이면 원문 그대로. (저장·판정·통신은 mm 불변 — 표시 전용)</summary>
        public static string ItemValueToDisplay(string name, string value, double sx, double sy)
        {
            try
            {
                if (DisplayMm) return value;
                if (!TryGetLengthAxis(name, out bool y)) return value;
                double s = y ? sy : sx;
                if (s <= 0 || s == 1.0) return value;
                if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                                     System.Globalization.CultureInfo.InvariantCulture, out double mm)) return value;
                return (mm / s).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch { return value; }
        }

        /// <summary>ItemValueToDisplay — 검사기에서 주입 스케일을 직접 읽는 편의 오버로드.</summary>
        public static string ItemValueToDisplay(string name, string value, IInspector ins)
        {
            GetInspectorScale(ins, out double sx, out double sy);
            return ItemValueToDisplay(name, value, sx, sy);
        }
    }
}
