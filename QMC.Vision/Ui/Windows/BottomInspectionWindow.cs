using System;
using System.Windows.Forms;
using Microsoft.Win32;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Windows
{
    /// <summary>
    /// 위(보조) 모니터 전용 Bottom 검사 풀스크린 고정 창.
    /// 프로그램 시작 시 자동 표시되어 보조 모니터를 꽉 채우고, 이동/리사이즈가 잠긴다.
    /// 내용은 <see cref="InspectionViewerControl"/>(Bottom 고정) 하나를 호스팅.
    ///
    /// ※ 현재 단계는 창 골격 + 모니터 배치만. 결과 데이터 바인딩은 후속 작업.
    /// 컨트롤 선언/배치는 .Designer.cs, 동작 로직은 본 파일(AGENTS 디자이너 규칙).
    /// </summary>
    public partial class BottomInspectionWindow : Form
    {
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MOVE       = 0xF010;
        private const int SC_SIZE       = 0xF000;

        private static BottomInspectionWindow _instance;

        private readonly InspectionViewerControl _viewer;

        public BottomInspectionWindow()
        {
            InitializeComponent();

            _viewer = new InspectionViewerControl { Dock = DockStyle.Fill };
            _viewer.SetMode(InspectionMode.Bottom);
            Controls.Add(_viewer);

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        /// <summary>위 창을 보조 모니터에 띄운다(단일 인스턴스). 호스트(Form1)에서 시작 시 호출.</summary>
        public static void ShowFor(Form owner)
        {
            try
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new BottomInspectionWindow();

                _instance.PlaceOnTargetScreen();
                if (!_instance.Visible) _instance.Show(owner);
                _instance.BringToFront();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[BottomInspectionWindow] ShowFor 실패: " + ex.Message);
            }
        }

        /// <summary>현재 인스턴스가 있으면 닫는다(호스트 종료 시).</summary>
        public static void CloseInstance()
        {
            try { if (_instance != null && !_instance.IsDisposed) _instance.Close(); }
            catch { }
            finally { _instance = null; }
        }

        /// <summary>대상(위/보조) 모니터를 골라 그 화면을 꽉 채운다. 보조 모니터가 없으면 주 모니터 폴백.</summary>
        private void PlaceOnTargetScreen()
        {
            try
            {
                Screen primary = Screen.PrimaryScreen;
                Screen target = primary;

                // 비주 모니터 중 '위쪽'(Bounds.Y 가 가장 작은) 화면 우선 선택.
                foreach (Screen s in Screen.AllScreens)
                {
                    if (s.Primary) continue;
                    if (target == primary || s.Bounds.Y < target.Bounds.Y)
                        target = s;
                }

                // 단일 모니터면 주 화면을 덮지 않도록 TopMost 해제(개발/단일 PC 폴백).
                TopMost = (target != primary);

                Bounds = target.Bounds;   // 작업표시줄까지 덮는 전체 화면
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[BottomInspectionWindow] PlaceOnTargetScreen 실패: " + ex.Message);
            }
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((Action)PlaceOnTargetScreen); } catch { } return; }
            PlaceOnTargetScreen();
        }

        // 이동/리사이즈 시스템 명령 차단(테두리 없음으로 기본 차단되지만 키보드 메뉴 경로까지 봉쇄).
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND)
            {
                int cmd = m.WParam.ToInt32() & 0xFFF0;
                if (cmd == SC_MOVE || cmd == SC_SIZE) return;
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; } catch { }
            base.OnFormClosed(e);
        }
    }
}
