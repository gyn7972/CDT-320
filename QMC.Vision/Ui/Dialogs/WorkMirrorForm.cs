using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Dialogs
{
    /// <summary>
    /// 작업(운영) 화면을 보조 모니터에 띄우기 위한 독립 팝업 창.
    /// 메인 창과 별개로 자유롭게 이동/리사이즈할 수 있다.
    /// 현재는 빈 팝업(자리표시)만 제공하며, 내부 작업 화면 내용은 추후 구현한다.
    /// </summary>
    public sealed partial class WorkMirrorForm : Form
    {
        // 단일 인스턴스 유지용. 이미 열려 있으면 새로 만들지 않고 앞으로 가져온다.
        private static WorkMirrorForm _instance;

        public WorkMirrorForm()
        {
            InitializeComponent();
            BuildPlaceholder();
        }

        /// <summary>작업 화면 복제 팝업을 연다. 이미 열려 있으면 앞으로 가져온다.</summary>
        public static void Open(IWin32Window owner)
        {
            try
            {
                if (_instance != null && !_instance.IsDisposed)
                {
                    if (_instance.WindowState == FormWindowState.Minimized)
                        _instance.WindowState = FormWindowState.Normal;

                    _instance.Activate();
                    _instance.BringToFront();
                    return;
                }

                _instance = new WorkMirrorForm();
                _instance.FormClosed += (s, e) => _instance = null;
                MoveToSecondaryScreen(_instance, owner as Form);
                _instance.Show(owner);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[WorkMirrorForm] Open 실패: " + ex.Message);
                MessageBox.Show(
                    owner,
                    "작업 화면 복제 팝업을 여는 중 오류가 발생했습니다.\r\n원인: " + ex.Message,
                    "작업 화면 복제",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // 추후 작업 화면 내용이 들어갈 자리. 현재는 안내 문구만 표시한다.
        private void BuildPlaceholder()
        {
            try
            {
                var lbl = new Label
                {
                    Dock      = DockStyle.Fill,
                    Text      = "작업 화면 (추후 구현 예정)",
                    ForeColor = System.Drawing.Color.FromArgb(0xB8, 0xB8, 0xBC),
                    Font      = new Font("맑은 고딕", 14F),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                Controls.Add(lbl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[WorkMirrorForm] BuildPlaceholder 실패: " + ex.Message);
            }
        }

        // 가능하면 메인 창이 있는 모니터가 아닌 보조 모니터에 띄운다. 하나뿐이면 메인 모니터 중앙.
        private static void MoveToSecondaryScreen(Form target, Form owner)
        {
            try
            {
                Screen ownerScreen = owner != null ? Screen.FromControl(owner) : Screen.PrimaryScreen;
                Screen targetScreen = ownerScreen;

                foreach (Screen screen in Screen.AllScreens)
                {
                    if (!screen.Equals(ownerScreen))
                    {
                        targetScreen = screen;
                        break;
                    }
                }

                Rectangle area = targetScreen.WorkingArea;
                target.Location = new Point(
                    area.Left + Math.Max(0, (area.Width - target.Width) / 2),
                    area.Top + Math.Max(0, (area.Height - target.Height) / 2));
            }
            catch (Exception ex)
            {
                target.StartPosition = FormStartPosition.CenterScreen;
                System.Diagnostics.Debug.WriteLine("[WorkMirrorForm] 보조 모니터 배치 실패: " + ex.Message);
            }
        }
    }
}
