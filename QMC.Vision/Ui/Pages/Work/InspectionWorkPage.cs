using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 작업화면 호스트(와꾸) — 상단 공용 헤더(작업—모니터링 + 레시피/Lot/Wafer/Judge + RUN/READY, 모든 모드 유지)
    /// + 우측 세로 버튼(작업·Side·Die, 하단 정렬)으로 메인 영역을 전환한다.
    ///  • "작업"    = 기존 모니터 UI(OperationPage, 자체 헤더는 숨김) — 기본
    ///  • "Side"    = InspectionViewerControl(Side)
    ///  • "Die gap" = InspectionViewerControl(Bin)
    /// Bottom 은 버튼 없음(위 모니터 전용 창 BottomInspectionWindow 담당).
    ///
    /// ※ 현재 단계는 레이아웃 골격 + 버튼 전환 + RUN/READY 상태표시. 검사 데이터 바인딩은 후속.
    /// 컨트롤 선언/배치는 .Designer.cs, 동작 로직은 본 파일(AGENTS 디자이너 규칙).
    /// </summary>
    public partial class InspectionWorkPage : PageBase
    {
        private OperationPage _opPage;            // 작업(기존 모니터 UI)
        private InspectionViewerControl _viewer;  // Side/Bin 검사 뷰어
        private System.Windows.Forms.Timer _stateTimer;

        public InspectionWorkPage()
        {
            InitializeComponent();
            if (IsDesignerMode()) return;

            _opPage = new OperationPage { Dock = DockStyle.Fill, Visible = true };
            _opPage.HideOwnHeader();   // 공용 상단 헤더와 중복 방지
            _viewer = new InspectionViewerControl { Dock = DockStyle.Fill, Visible = false };

            _contentHost.Controls.Add(_viewer);
            _contentHost.Controls.Add(_opPage);

            _btnRun.Click   += OnRunToggleClick;
            _btnReady.Click += OnReadyToggleClick;

            ShowWork();
            StartStateTimer();
        }

        // ── 메인 영역 전환 ──
        private void btnWork_Click(object sender, EventArgs e) { ShowWork(); }
        private void btnSide_Click(object sender, EventArgs e) { ShowInspection(InspectionMode.Side, _btnSide); }
        private void btnDie_Click(object sender, EventArgs e)  { ShowInspection(InspectionMode.Bin,  _btnDie); }

        private void ShowWork()
        {
            try
            {
                if (_viewer != null) _viewer.Visible = false;
                if (_opPage != null) { _opPage.Visible = true; _opPage.BringToFront(); }
                Highlight(_btnWork);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[InspectionWorkPage] ShowWork 실패: " + ex.Message); }
        }

        private void ShowInspection(InspectionMode mode, Button active)
        {
            try
            {
                if (_opPage != null) _opPage.Visible = false;
                if (_viewer != null)
                {
                    _viewer.SetMode(mode);
                    _viewer.Visible = true;
                    _viewer.BringToFront();
                }
                Highlight(active);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[InspectionWorkPage] ShowInspection 실패: " + ex.Message); }
        }

        private void Highlight(Button active)
        {
            foreach (Button b in new[] { _btnWork, _btnSide, _btnDie })
            {
                if (b == null) continue;
                b.BackColor = (b == active) ? BtnNavActive : BtnNav;
            }
        }

        // ── RUN/READY (모든 모드에서 유지) — Form1 상태에 연동 ──
        private void StartStateTimer()
        {
            _stateTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _stateTimer.Tick += (s, e) =>
            {
                if (IsDisposed || Disposing) { _stateTimer.Stop(); return; }
                UpdateRunButton();
                UpdateReadyButton();
            };
            _stateTimer.Start();
            UpdateRunButton();
            UpdateReadyButton();
        }

        private void OnRunToggleClick(object sender, EventArgs e)
        {
            try
            {
                var host = FindForm() as Form1;
                if (host == null) return;
                if (host.IsRunActive) host.SetRun(false);
                else if (host.CanRun) host.SetRun(true);
                UpdateRunButton();
                UpdateReadyButton();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[InspectionWorkPage] RUN 토글 실패: " + ex.Message); }
        }

        private void OnReadyToggleClick(object sender, EventArgs e)
        {
            try
            {
                var host = FindForm() as Form1;
                if (host == null) return;
                if (host.IsReady) host.SetReady(false);
                else if (host.CanReady) host.SetReady(true);
                UpdateReadyButton();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[InspectionWorkPage] READY 토글 실패: " + ex.Message); }
        }

        private void UpdateRunButton()
        {
            if (_btnRun == null) return;
            var host = FindForm() as Form1;
            if (host == null) { _btnRun.Enabled = false; return; }

            bool running = host.IsRunActive;
            bool canRun  = host.CanRun;
            _btnRun.Enabled   = running || canRun;
            _btnRun.Text      = running ? "STOP" : "RUN";
            _btnRun.BackColor = running
                ? Color.FromArgb(0x5a, 0x22, 0x22)
                : (canRun ? Color.FromArgb(0x1f, 0x9d, 0x4d) : Color.FromArgb(0x55, 0x55, 0x55));
        }

        private void UpdateReadyButton()
        {
            if (_btnReady == null) return;
            var host = FindForm() as Form1;
            if (host == null) { _btnReady.Enabled = false; return; }

            bool ready  = host.IsReady;
            bool canRdy = host.CanReady;
            _btnReady.Enabled   = ready || canRdy;
            _btnReady.Text      = ready ? "READY ●" : "READY";
            _btnReady.BackColor = ready
                ? Color.FromArgb(0x1f, 0x9d, 0x4d)
                : (canRdy ? Color.FromArgb(0x35, 0x6b, 0x46) : Color.FromArgb(0x55, 0x55, 0x55));
        }
    }
}
