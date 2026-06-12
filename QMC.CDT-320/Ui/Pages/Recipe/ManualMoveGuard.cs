using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// 매뉴얼 축 이동 차단 게이트.
    /// 인터락 정의가 끝나기 전까지 레시피 페이지(InputStage / Front·Rear Picker / OutputStage)의
    /// 매뉴얼 이동 동작을 공통으로 막는다. 인터락 적용이 완료되면 <see cref="InterlockReady"/> 값만
    /// true로 바꾸면 전 페이지에서 한 번에 해제된다. (← 여기 한 곳만 수정)
    /// </summary>
    internal static class ManualMoveGuard
    {
        // 인터락 정의 완료 시 true 로 변경 → 모든 페이지의 매뉴얼 이동 허용
        public static readonly bool InterlockReady = true;

        /// <summary>
        /// 차단 중이면 안내 메시지를 띄우고 true 를 반환한다. 호출부는 true 일 때 즉시 return 한다.
        /// </summary>
        public static bool BlockIfNotReady(IWin32Window owner, string title)
        {
            if (InterlockReady)
                return false;

            QMC.Common.MessageDialog.Show(owner, "인터락이 정의되지 않아 동작이 차단되었습니다.", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
    }
}
