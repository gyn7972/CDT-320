using System.Windows.Forms;

namespace QMC.Vision.Ui.Security
{
    /// <summary>사용자 권한 레벨 — 핸들러 정렬. 낮은 값일수록 제한적.</summary>
    public enum UserLevel
    {
        Operator = 0,
        Engineer = 1,
        Admin    = 2
    }

    /// <summary>
    /// 권한 기반 UI 접근 제어 — 핸들러 QMC.CDT_320.Ui.Security.AccessControl 정렬(경량 stub).
    /// 현재 Vision 은 로그인/권한 개념이 없어 전체 허용(Admin). 권한 도입 시 Apply 를 구현한다.
    /// </summary>
    public static class AccessControl
    {
        /// <summary>현재 사용자 레벨(기본 Admin = 전체 허용).</summary>
        public static UserLevel Current { get; set; } = UserLevel.Admin;

        /// <summary>페이지의 권한 태그에 따라 컨트롤 표시/활성화 조정. (stub: no-op)</summary>
        public static void Apply(Control root) { }
    }
}
