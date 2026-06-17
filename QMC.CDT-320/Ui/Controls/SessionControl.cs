using System;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>로그인 카드 컨트롤. 사용자 탭에서 비로그인 상태일 때 표시한다.</summary>
    public partial class SessionControl : UserControl
    {
        public SessionControl()
        {
            InitializeComponent();
            btnLogin.Click += (s, e) => TryLogin();
            txtPw.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TryLogin(); };
            Resize += (s, e) => CenterCard();
            Load += (s, e) => CenterCard();
        }

        private void CenterCard()
        {
            pnlCard.Left = Math.Max(0, (Width - pnlCard.Width) / 2);
            pnlCard.Top  = Math.Max(0, (Height - pnlCard.Height) / 2);
        }

        /// <summary>표시될 때 입력 초기화 + 포커스.</summary>
        public void Reset()
        {
            lblMsg.Text = "";
#if DEBUG
            // DEBUG 빌드: 자동 로그인(ForceSet)과 동일 기준으로 admin/admin 을 미리 채워 둔다.
            txtId.Text = "admin";
            txtPw.Text = "admin";
#else
            txtPw.Clear();
#endif
            try { txtId.Focus(); } catch { }
        }

        private void TryLogin()
        {
            lblMsg.Text = "";
            if (UserSession.Login((txtId.Text ?? "").Trim(), txtPw.Text))
            {
                // 성공 → UserSession.UserChanged 가 발생해 호스트(UserAccountPage)가 화면을 전환한다.
                txtPw.Clear();
                return;
            }
            lblMsg.Text = "ID 또는 비밀번호가 올바르지 않습니다.";
            txtPw.Clear();
            try { txtPw.Focus(); } catch { }
        }
    }
}
