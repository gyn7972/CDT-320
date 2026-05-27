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
    /// <summary>하단 탭 식별자 — Handler 와 동일 구성: 4 좌측 + 3 우측 (Settings 추가).</summary>
    public enum Tab { Operation, Configuration, Maintenance, Recipe, DataLog, Settings }

    public partial class Form1 : Form
    {
        internal IVisionBackend Backend { get; private set; }

        internal WaferVisionModule        WaferMod      { get; private set; }
        internal BinVisionModule          BinMod        { get; private set; }
        internal BottomInspectionModule   BottomMod     { get; private set; }
        internal TopSideInspectionModule    TopSideMod    { get; private set; }
        internal BottomSideInspectionModule BottomSideMod { get; private set; }

        private VisionTcpServer _svrWafer, _svrBin, _svrBottom;
        private VisionTcpServer _svrTopSide, _svrBottomSide;

        private OperationPage     _pgOperation;
        private ConfigurationPage _pgConfig;
        private MaintenancePage   _pgMaint;
        private RecipePage        _pgRecipe;
        private DataLogPage       _pgDataLog;
        private SettingsPage      _pgSettings;

        public Form1() { InitializeComponent(); }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ── 설정 + 백엔드 ──
            var cfg = VisionConfigStore.Load();
            var map = AlgorithmCameraMapStore.Load();   // 알고리즘-카메라 매핑 (없으면 defaults)

            Backend = VisionFactory.Global;
            lblStatusL.Text = $"Backend: {Backend.Name}   |   {Backend.VersionInfo}";
            lblStatusR.Text = $"TCP: Wafer={cfg.WaferVisionPort}  Bin={cfg.BinVisionPort}  Bottom={cfg.InspectionVisionPort}";

            // ── 모듈별 카메라: 매핑 기반 ──
            var camWafer      = CreateCameraForAlgorithm(map, VisionAlgorithm.Wafer,            "Sim/Wafer");
            var camBin        = CreateCameraForAlgorithm(map, VisionAlgorithm.Bin,              "Sim/Bin");
            var camBottom     = CreateCameraForAlgorithm(map, VisionAlgorithm.BottomInspection, "Sim/BottomInsp");
            var camTopSide    = CreateCameraForAlgorithm(map, VisionAlgorithm.TopSide,          "Sim/TopSide");
            var camBottomSide = CreateCameraForAlgorithm(map, VisionAlgorithm.BottomSide,       "Sim/BottomSide");

            WaferMod      = new WaferVisionModule       (camWafer,      Backend);
            BinMod        = new BinVisionModule         (camBin,        Backend);
            BottomMod     = new BottomInspectionModule  (camBottom,     Backend);
            TopSideMod    = new TopSideInspectionModule   (camTopSide,    Backend);
            BottomSideMod = new BottomSideInspectionModule(camBottomSide, Backend);

            ApplyDelayFromMap(WaferMod,      map, VisionAlgorithm.Wafer);
            ApplyDelayFromMap(BinMod,        map, VisionAlgorithm.Bin);
            ApplyDelayFromMap(BottomMod,     map, VisionAlgorithm.BottomInspection);
            ApplyDelayFromMap(TopSideMod,    map, VisionAlgorithm.TopSide);
            ApplyDelayFromMap(BottomSideMod, map, VisionAlgorithm.BottomSide);

            // ── TCP 서버 ──
            _svrWafer      = new VisionTcpServer(WaferMod,      cfg.WaferVisionPort);
            _svrBin        = new VisionTcpServer(BinMod,        cfg.BinVisionPort);
            _svrBottom     = new VisionTcpServer(BottomMod,     cfg.InspectionVisionPort);
            _svrTopSide    = new VisionTcpServer(TopSideMod,    cfg.TopSideInspectionPort);
            _svrBottomSide = new VisionTcpServer(BottomSideMod, cfg.BottomSideInspectionPort);
            try { _svrWafer     .Start(); } catch { }
            try { _svrBin       .Start(); } catch { }
            try { _svrBottom    .Start(); } catch { }
            try { _svrTopSide   .Start(); } catch { }
            try { _svrBottomSide.Start(); } catch { }

            // ── 6 탭 UserControl ──
            _pgOperation = new OperationPage     { Dock = DockStyle.Fill, Visible = false };
            _pgConfig    = new ConfigurationPage { Dock = DockStyle.Fill, Visible = false };
            _pgMaint     = new MaintenancePage   { Dock = DockStyle.Fill, Visible = false };
            _pgRecipe    = new RecipePage        { Dock = DockStyle.Fill, Visible = false };
            _pgDataLog   = new DataLogPage       { Dock = DockStyle.Fill, Visible = false };
            _pgSettings  = new SettingsPage      { Dock = DockStyle.Fill, Visible = false };
            pnlContent.Controls.Add(_pgOperation);
            pnlContent.Controls.Add(_pgConfig);
            pnlContent.Controls.Add(_pgMaint);
            pnlContent.Controls.Add(_pgRecipe);
            pnlContent.Controls.Add(_pgDataLog);
            pnlContent.Controls.Add(_pgSettings);

            timerClock.Start();
            UpdateClock();
            ShowTab(Tab.Operation);
        }

        private static ICamera CreateCameraForAlgorithm(AlgorithmCameraMap map, string algorithm, string fallbackId)
        {
            var m = map?.Get(algorithm);
            if (m == null)
            {
                m = new AlgorithmCameraMapping { Algorithm = algorithm, CameraId = fallbackId };
                AlgorithmCameraMapStore.Current.Items.Add(m);
            }
            if (string.IsNullOrEmpty(m.CameraId)) m.CameraId = fallbackId;
            return AlgorithmCameraBinder.CreateAndApply(m);
        }

        private static void ApplyDelayFromMap(VisionModule mod, AlgorithmCameraMap map, string algorithm)
        {
            var m = map?.Get(algorithm);
            if (mod != null && m != null) mod.DelayBeforeGrabMs = m.DelayBeforeGrabMs;
        }

        /// <summary>SettingsPage 의 "실행 모듈에 적용" 에서 호출 — 카메라 교체 또는 파라미터 갱신.</summary>
        public void RebindAlgorithmCamera(string algorithm, AlgorithmCameraMapping mapping)
        {
            if (mapping == null) return;
            VisionModule mod = ResolveModule(algorithm);
            if (mod == null) return;

            mod.DelayBeforeGrabMs = mapping.DelayBeforeGrabMs;

            // 카메라 ID 가 같으면 파라미터만 갱신, 다르면 카메라 교체
            if (string.Equals(mod.Camera?.Info?.Id, mapping.CameraId, StringComparison.OrdinalIgnoreCase))
            {
                AlgorithmCameraBinder.ApplyParameters(mod.Camera, mapping);
                return;
            }

            // 카메라 교체: 기존 닫고 새로 Open. TCP 서버는 모듈을 가리키므로 영향 없음.
            var oldCam = mod.Camera;
            var newCam = AlgorithmCameraBinder.CreateAndApply(mapping);
            mod.SetCamera(newCam);
            try { oldCam?.Dispose(); } catch { }
        }

        private VisionModule ResolveModule(string algorithm)
        {
            switch (algorithm)
            {
                case VisionAlgorithm.Wafer:            return WaferMod;
                case VisionAlgorithm.Bin:              return BinMod;
                case VisionAlgorithm.BottomInspection: return BottomMod;
                case VisionAlgorithm.TopSide:          return TopSideMod;
                case VisionAlgorithm.BottomSide:       return BottomSideMod;
                default:                               return null;
            }
        }

        private void ShowTab(Tab t)
        {
            _pgOperation.Visible = t == Tab.Operation;
            _pgConfig   .Visible = t == Tab.Configuration;
            _pgMaint    .Visible = t == Tab.Maintenance;
            _pgRecipe   .Visible = t == Tab.Recipe;
            _pgDataLog  .Visible = t == Tab.DataLog;
            _pgSettings .Visible = t == Tab.Settings;

            btnOperation    .Selected = t == Tab.Operation;
            btnConfiguration.Selected = t == Tab.Configuration;
            btnMaintenance  .Selected = t == Tab.Maintenance;
            btnRecipe       .Selected = t == Tab.Recipe;
            btnDataLog      .Selected = t == Tab.DataLog;
            btnSettings     .Selected = t == Tab.Settings;
        }

        private void TimerClock_Tick(object sender, EventArgs e) => UpdateClock();
        private void UpdateClock() => lblClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _svrWafer?.Dispose(); }      catch { }
            try { _svrBin?.Dispose(); }        catch { }
            try { _svrBottom?.Dispose(); }     catch { }
            try { _svrTopSide?.Dispose(); }    catch { }
            try { _svrBottomSide?.Dispose(); } catch { }
            try { Backend?.Dispose(); }        catch { }
            base.OnFormClosing(e);
        }
    }
}
