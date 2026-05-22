using System;
using System.Windows.Forms;
using QMC.Vision.Comm;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Pages;

namespace QMC.Vision
{
    public enum Tab { Operation, Configuration, Maintenance, Recipe, DataLog }

    public partial class Form1 : Form
    {
        internal IVisionBackend Backend { get; private set; }

        internal WaferVisionModule        WaferMod  { get; private set; }
        internal BinVisionModule          BinMod    { get; private set; }
        internal BottomInspectionModule   BottomMod { get; private set; }
        // Stage 52 — 매뉴얼 호환 추가 모듈 (port 5105/5106)
        internal TopSideInspectionModule    TopSideMod    { get; private set; }
        internal BottomSideInspectionModule BottomSideMod { get; private set; }

        private VisionTcpServer _svrWafer, _svrBin, _svrBottom;
        private VisionTcpServer _svrTopSide, _svrBottomSide;

        private OperationPage     _pgOperation;
        private ConfigurationPage _pgConfig;
        private MaintenancePage   _pgMaint;
        private RecipePage        _pgRecipe;
        private DataLogPage       _pgDataLog;

        public Form1() { InitializeComponent(); }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 설정 + 백엔드 초기화
            var cfg = VisionConfigStore.Load();
            Backend = VisionFactory.Global;
            lblStatusL.Text = $"Backend: {Backend.Name}   |   {Backend.VersionInfo}";
            lblStatusR.Text = $"TCP: Wafer={cfg.WaferVisionPort}  Bin={cfg.BinVisionPort}  Bottom={cfg.InspectionVisionPort}";

            // 3 모듈 — 카메라 + 백엔드 주입
            var camWafer  = CameraFactory.CreateById(cfg.WaferCameraId);
            var camBin    = CameraFactory.CreateById(cfg.BinCameraId);
            var camBottom = CameraFactory.CreateById(cfg.BottomInspectionCameraId);
            try { camWafer .Open(); } catch { }
            try { camBin   .Open(); } catch { }
            try { camBottom.Open(); } catch { }

            WaferMod  = new WaferVisionModule     (camWafer,  Backend);
            BinMod    = new BinVisionModule       (camBin,    Backend);
            BottomMod = new BottomInspectionModule(camBottom, Backend);
            // Stage 52 — TopSide / BottomSide 모듈 (Bottom 카메라 공유 — 실 카메라 분리 시 변경)
            TopSideMod    = new TopSideInspectionModule   (camBottom, Backend);
            BottomSideMod = new BottomSideInspectionModule(camBottom, Backend);

            // TCP 서버 시작
            _svrWafer  = new VisionTcpServer(WaferMod,  cfg.WaferVisionPort);
            _svrBin    = new VisionTcpServer(BinMod,    cfg.BinVisionPort);
            _svrBottom = new VisionTcpServer(BottomMod, cfg.InspectionVisionPort);
            // Stage 52 — Side Vision TCP 서버
            _svrTopSide    = new VisionTcpServer(TopSideMod,    cfg.TopSideInspectionPort);
            _svrBottomSide = new VisionTcpServer(BottomSideMod, cfg.BottomSideInspectionPort);
            try { _svrWafer    .Start(); } catch { }
            try { _svrBin      .Start(); } catch { }
            try { _svrBottom   .Start(); } catch { }
            try { _svrTopSide   .Start(); } catch { }
            try { _svrBottomSide.Start(); } catch { }

            // 5 탭 UserControl — 미리 생성하고 show/hide 전환
            _pgOperation = new OperationPage     { Dock = DockStyle.Fill, Visible = false };
            _pgConfig    = new ConfigurationPage { Dock = DockStyle.Fill, Visible = false };
            _pgMaint     = new MaintenancePage   { Dock = DockStyle.Fill, Visible = false };
            _pgRecipe    = new RecipePage        { Dock = DockStyle.Fill, Visible = false };
            _pgDataLog   = new DataLogPage       { Dock = DockStyle.Fill, Visible = false };
            pnlContent.Controls.Add(_pgOperation);
            pnlContent.Controls.Add(_pgConfig);
            pnlContent.Controls.Add(_pgMaint);
            pnlContent.Controls.Add(_pgRecipe);
            pnlContent.Controls.Add(_pgDataLog);

            timerClock.Start();
            UpdateClock();
            ShowTab(Tab.Operation);
        }

        private void ShowTab(Tab t)
        {
            _pgOperation.Visible = t == Tab.Operation;
            _pgConfig   .Visible = t == Tab.Configuration;
            _pgMaint    .Visible = t == Tab.Maintenance;
            _pgRecipe   .Visible = t == Tab.Recipe;
            _pgDataLog  .Visible = t == Tab.DataLog;

            btnOperation    .BackColor = t == Tab.Operation     ? UiTheme.Accent : UiTheme.HeaderBg;
            btnConfiguration.BackColor = t == Tab.Configuration ? UiTheme.Accent : UiTheme.HeaderBg;
            btnMaintenance  .BackColor = t == Tab.Maintenance   ? UiTheme.Accent : UiTheme.HeaderBg;
            btnRecipe       .BackColor = t == Tab.Recipe        ? UiTheme.Accent : UiTheme.HeaderBg;
            btnDataLog      .BackColor = t == Tab.DataLog       ? UiTheme.Accent : UiTheme.HeaderBg;
        }

        private void TimerClock_Tick(object sender, EventArgs e) => UpdateClock();
        private void UpdateClock() => lblClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _svrWafer?.Dispose(); } catch { }
            try { _svrBin?.Dispose(); }   catch { }
            try { _svrBottom?.Dispose(); }catch { }
            try { Backend?.Dispose(); }   catch { }
            base.OnFormClosing(e);
        }
    }
}
