using System;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 검사기 id(모듈명/슬롯명)로 도메인 검사기(310 포팅)를 생성. 백엔드(Cognex/OpenCv/Sim) 공통.
    ///  • Placement → PlacementGapInspector(Bin DieGap/위치)
    ///  • Chipping / Side+Surface → SideAppearanceInspector(측면 칩핑)
    ///  • Surface(비-Side) → BottomInspector(Bottom 사이즈·칩핑·이물)
    /// 매칭 없으면 false → 백엔드 기본 검사기 사용.
    /// </summary>
    public static class DomainInspectorFactory
    {
        public static bool TryCreate(string id, out IInspector inspector)
        {
            inspector = null;
            if (string.IsNullOrEmpty(id)) return false;

            bool Has(string k) => id.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0;

            if (Has("Placement")) { inspector = new PlacementGapInspector(id); return true; }
            if (Has("Chipping"))  { inspector = new SideAppearanceInspector(id); return true; }
            if (Has("Side") && Has("Surface")) { inspector = new SideAppearanceInspector(id); return true; }
            if (Has("Surface"))   { inspector = new BottomInspector(id); return true; }
            return false;
        }
    }
}
