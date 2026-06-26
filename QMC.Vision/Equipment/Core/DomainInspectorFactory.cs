using System;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 검사기 id(모듈명/슬롯명)로 도메인 검사기(310 포팅)를 생성 — 모듈+도구마다 알고리즘을 명확히 구분.
    /// id 는 모듈명을 포함한다(예: "TopSideVision/TopSurfaceInspector"). 그 전제로 분기:
    ///  • Placement/Bin/DieGap         → PlacementGapInspector (Bin 위치·DieGap)
    ///  • SideVision(Top/Bottom) 칩핑   → SideAppearanceInspector (측면 칩핑, id "Chipping")
    ///  • SideVision(Top/Bottom) 표면   → SideAppearanceInspector (측면 이물, id "Surface")
    ///  • BottomInspection 표면         → BottomInspector (사이즈·칩핑·이물 3검사)
    /// 매칭 없으면 false → 백엔드 기본 검사기 사용.
    /// </summary>
    public static class DomainInspectorFactory
    {
        public static bool TryCreate(string id, out IInspector inspector)
        {
            inspector = null;
            if (string.IsNullOrEmpty(id)) return false;

            bool Has(string k) => id.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0;

            // 1) Bin/배치 위치
            if (Has("Placement") || Has("DieGap") || Has("BinVision"))
            { inspector = new PlacementGapInspector(id); return true; }

            // 2) 측면(앞=TopSideVision / 뒤=BottomSideVision) — 모듈명에 "SideVision". 역할은 id 의 Chipping/Surface 로.
            if (Has("SideVision") || Has("Chipping"))
            { inspector = new SideAppearanceInspector(id); return true; }

            // 3) 바텀(BottomInspection) 표면 — 사이즈/칩핑/이물 3검사.
            if (Has("BottomInspection") || Has("Surface"))
            { inspector = new BottomInspector(id); return true; }

            return false;
        }
    }
}
