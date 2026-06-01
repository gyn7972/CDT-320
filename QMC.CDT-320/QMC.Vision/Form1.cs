using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Vision.Comm;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Pages;

namespace QMC.Vision
{
    /// <summary>하단 탭 식별자 — Handler 와 동일 구성: 4 좌측 + 3 우측 (Settings 추가).</summary>
    public enum Tab { Operation, Configuration, Recipe, DataLog, Settings }

    public partial class Form1 : Form
    {
        internal IVisionBackend Backend { get; private set; }

        internal WaferVisionModule        WaferMod      { get; private set; }
        internal BinVisionModule          BinMod        { get; private set; }
        internal BottomInspectionModule   BottomMod     { get; private set; }
        internal FrontSideInspectionModule    FrontSideMod    { get; private set; }
        internal RearSideInspectionModule RearSideMod { get; private set; }

        private VisionTcpServer _svrWafer, _svrBin, _svrBottom;
        private VisionTcpServer _svrFrontSide, _svrRearSide;

        // 원격 뷰어 (그랩 영상 송출) — 모듈별 5채널
        private GrabStreamServer _viewWafer, _viewBin, _viewBottom, _viewFrontSide, _viewRearSide;

        private OperationPage     _pgOperation;
        private ConfigurationPage _pgConfig;
        private RecipePage        _pgRecipe;
        private DataLogPage       _pgDataLog;
        private SettingsPage      _pgSettings;

        public Form1() { InitializeComponent(); }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ── 설정 + 백엔드 ──
            var cfg = VisionConfigStore.Load();
            var map = AlgorithmCameraMapStore.Load();   // 알고리즘-카메라 매핑 (없으면 defaults)

            // Stage 69 — 조명 시스템 Setup 로드 + LightHub 초기화 (Sim 백엔드면 Sim 컨트롤러).
            var lightSetup = QMC.Common.Recipes.LightSystemSetupStore.Load();
            // 첫 기동 자동 마이그레이션: Setup 비어 있고 레거시 io_set 존재 시 1회 변환 + 백업.
            if (lightSetup.Controllers == null || lightSetup.Controllers.Count == 0)
            {
                string ioSet = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "io_set.lightSource.json");
                var migrated = QMC.Common.Recipes.LightSystemMigrator.MigrateFromLegacy(ioSet);
                if (migrated != null)
                {
                    QMC.Common.Recipes.LightSystemMigrator.BackupLegacy(ioSet, DateTime.Now.ToString("yyyyMMdd"));
                    migrated.EnsureWirings();
                    QMC.Common.Recipes.LightSystemSetupStore.SetCurrent(migrated);
                    QMC.Common.Recipes.LightSystemSetupStore.Save();
                    lightSetup = migrated;
                }
            }
            // Stage 70 — 구버전 Wiring.Page → 검사 Setting.Page 마이그레이션 (1회).
            if (map.MigrateWiringPageToSettings(lightSetup))
            {
                AlgorithmCameraMapStore.Save();              // 검사 Setting.Page 기록
                QMC.Common.Recipes.LightSystemSetupStore.Save(); // Wiring.LegacyPage(0) 제거 — 새 스키마로 재저장
            }

            bool lightUseSim = cfg.Provider == VisionProvider.Sim;
            QMC.Vision.Comm.LightHub.Initialize(lightSetup, lightUseSim);

            Backend = VisionFactory.Global;
            lblStatusL.Text = $"Backend: {Backend.Name}   |   {Backend.VersionInfo}";
            lblStatusR.Text = $"TCP: Wafer={cfg.WaferVisionPort}  Bin={cfg.BinVisionPort}  Bottom={cfg.InspectionVisionPort}";

            // ── 모듈별 카메라: 매핑 기반 ──
            var camWafer      = CreateCameraForAlgorithm(map, VisionAlgorithm.Wafer,            "Sim/Wafer");
            var camBin        = CreateCameraForAlgorithm(map, VisionAlgorithm.Bin,              "Sim/Bin");
            var camBottom     = CreateCameraForAlgorithm(map, VisionAlgorithm.BottomInspection, "Sim/BottomInsp");
            var camFrontSide    = CreateCameraForAlgorithm(map, VisionAlgorithm.FrontSide,          "Sim/FrontSide");
            var camRearSide = CreateCameraForAlgorithm(map, VisionAlgorithm.RearSide,       "Sim/RearSide");

            WaferMod      = new WaferVisionModule       (camWafer,      Backend);
            BinMod        = new BinVisionModule         (camBin,        Backend);
            BottomMod     = new BottomInspectionModule  (camBottom,     Backend);
            FrontSideMod    = new FrontSideInspectionModule   (camFrontSide,    Backend);
            RearSideMod = new RearSideInspectionModule(camRearSide, Backend);

            ApplyDelayFromMap(WaferMod,      map, VisionAlgorithm.Wafer);
            ApplyDelayFromMap(BinMod,        map, VisionAlgorithm.Bin);
            ApplyDelayFromMap(BottomMod,     map, VisionAlgorithm.BottomInspection);
            ApplyDelayFromMap(FrontSideMod,    map, VisionAlgorithm.FrontSide);
            ApplyDelayFromMap(RearSideMod, map, VisionAlgorithm.RearSide);

            // ── TCP 서버 ──
            _svrWafer      = new VisionTcpServer(WaferMod,      cfg.WaferVisionPort);
            _svrBin        = new VisionTcpServer(BinMod,        cfg.BinVisionPort);
            _svrBottom     = new VisionTcpServer(BottomMod,     cfg.InspectionVisionPort);
            _svrFrontSide    = new VisionTcpServer(FrontSideMod,    cfg.FrontSideInspectionPort);
            _svrRearSide = new VisionTcpServer(RearSideMod, cfg.RearSideInspectionPort);
            try { _svrWafer     .Start(); } catch { }
            try { _svrBin       .Start(); } catch { }
            try { _svrBottom    .Start(); } catch { }
            try { _svrFrontSide   .Start(); } catch { }
            try { _svrRearSide.Start(); } catch { }

            // ── 원격 뷰어 서버 (모듈별 그랩 영상 송출) ──
            if (cfg.RemoteViewerEnable)
            {
                _viewWafer     = MakeViewer("Wafer",     cfg.WaferViewerPort,      WaferMod,     cfg);
                _viewBottom    = MakeViewer("Bottom",    cfg.InspectionViewerPort, BottomMod,    cfg);
                _viewBin       = MakeViewer("Bin",       cfg.BinViewerPort,        BinMod,       cfg);
                _viewFrontSide = MakeViewer("FrontSide", cfg.FrontSideViewerPort,  FrontSideMod, cfg);
                _viewRearSide  = MakeViewer("RearSide",  cfg.RearSideViewerPort,   RearSideMod,  cfg);
            }

            // ── 5 탭 UserControl (Stage 65: Maintenance 통합 → Recipe) ──
            _pgOperation = new OperationPage     { Dock = DockStyle.Fill, Visible = false };
            _pgConfig    = new ConfigurationPage { Dock = DockStyle.Fill, Visible = false };
            _pgRecipe    = new RecipePage        { Dock = DockStyle.Fill, Visible = false };
            _pgDataLog   = new DataLogPage       { Dock = DockStyle.Fill, Visible = false };
            _pgSettings  = new SettingsPage      { Dock = DockStyle.Fill, Visible = false };
            pnlContent.Controls.Add(_pgOperation);
            pnlContent.Controls.Add(_pgConfig);
            pnlContent.Controls.Add(_pgRecipe);
            pnlContent.Controls.Add(_pgDataLog);
            pnlContent.Controls.Add(_pgSettings);

            timerClock.Start();
            UpdateClock();
            ShowTab(Tab.Operation);
        }

        private static ICamera CreateCameraForAlgorithm(AlgorithmCameraSubset map, string algorithm, string fallbackId)
        {
            var m = map?.Get(algorithm);
            bool wasMissing = false;
            if (m == null)
            {
                wasMissing = true;
                m = new AlgorithmCameraMapping { Algorithm = algorithm, CameraId = fallbackId };
                AlgorithmCameraMapStore.Current.Items.Add(m);
            }
            if (string.IsNullOrEmpty(m.CameraId))
            {
                wasMissing = true;
                m.CameraId = fallbackId;
            }

            if (wasMissing)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "VISION-MAPMISS",
                    "Vision/" + algorithm,
                    $"매핑 누락 — fallback '{fallbackId}' 사용");
            }

            var cam = AlgorithmCameraBinder.CreateAndApply(m, out var openErr, out var applyErr);
            if (!string.IsNullOrEmpty(openErr))
            {
                AlarmManager.Raise(AlarmSeverity.Error, "VISION-CAMOPEN",
                    "Vision/" + algorithm,
                    $"Camera.Open 실패 [{m.CameraId}]: {openErr}");
            }
            if (!string.IsNullOrEmpty(applyErr))
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "VISION-PARAMFAIL",
                    "Vision/" + algorithm,
                    $"파라미터 적용 실패: {applyErr}");
            }
            return cam;
        }

        private static void ApplyDelayFromMap(VisionModule mod, AlgorithmCameraSubset map, string algorithm)
        {
            var m = map?.Get(algorithm);
            if (mod != null && m != null) mod.DelayBeforeGrabMs = m.DelayBeforeGrabMs;
        }

        /// <summary>모듈별 원격 뷰어 서버 생성+Start. 소스(GrabImage/ScreenRegion)에 따라 프레임 provider 선택.</summary>
        private GrabStreamServer MakeViewer(string name, int port, VisionModule mod, VisionSettings cfg)
        {
            Func<Bitmap> provider;
            if (string.Equals(cfg.RemoteViewerSource, "ScreenRegion", StringComparison.OrdinalIgnoreCase))
            {
                var rect = new Rectangle(cfg.RemoteViewerScreenX, cfg.RemoteViewerScreenY,
                                         cfg.RemoteViewerScreenW, cfg.RemoteViewerScreenH);
                provider = () => GrabStreamServer.CaptureScreenRegion(rect);
            }
            else
            {
                // GrabImage: 새 그랩(테스트 그랩/실제 운행/라이브)이 들어왔을 때만 프레임 제공. 변화 없으면 null → 송출 안 함.
                long lastSeq = -1;
                provider = () =>
                {
                    long s = mod.ViewerFrameSeq;
                    if (s == lastSeq) return null;
                    lastSeq = s;
                    return mod.AcquireViewerFrame();
                };
            }
            var srv = new GrabStreamServer(name, port, provider, cfg.RemoteViewerFps, cfg.RemoteViewerQuality);
            try { srv.Start(); } catch { }
            return srv;
        }

        /// <summary>SettingsPage 의 "실행 모듈에 적용" 에서 호출 — 카메라 교체 또는 파라미터 갱신.
        /// 오류 메시지를 out 으로 반환 (SettingsPage 가 status label 에 표시).</summary>
        public bool RebindAlgorithmCamera(string algorithm, AlgorithmCameraMapping mapping, out string error)
        {
            error = null;
            if (mapping == null) { error = "mapping is null"; return false; }
            VisionModule mod = ResolveModule(algorithm);
            if (mod == null) { error = "unknown algorithm: " + algorithm; return false; }

            mod.DelayBeforeGrabMs = mapping.DelayBeforeGrabMs;

            // 카메라 ID 가 같으면 파라미터만 갱신, 다르면 카메라 교체
            if (string.Equals(mod.Camera?.Info?.Id, mapping.CameraId, StringComparison.OrdinalIgnoreCase))
            {
                if (!AlgorithmCameraBinder.TryApplyParameters(mod.Camera, mapping, out var applyErr))
                {
                    error = applyErr;
                    AlarmManager.Raise(AlarmSeverity.Warning, "VISION-PARAMFAIL",
                        "Vision/" + algorithm, "파라미터 적용 실패: " + applyErr);
                    return false;
                }
                return true;
            }

            // 카메라 교체: 새 카메라 생성 + Open + Apply. 실패 시 old 유지 (롤백).
            var newCam = AlgorithmCameraBinder.CreateAndApply(mapping, out var openErr, out var newApplyErr);
            if (!string.IsNullOrEmpty(openErr))
            {
                error = openErr;
                AlarmManager.Raise(AlarmSeverity.Error, "VISION-CAMOPEN",
                    "Vision/" + algorithm, $"신규 카메라 Open 실패 [{mapping.CameraId}]: {openErr}");
                try { newCam?.Dispose(); } catch { }
                return false;
            }
            // 신규 카메라 OK — 교체
            var oldCam = mod.Camera;
            mod.SetCamera(newCam);
            try { oldCam?.Dispose(); } catch { }
            if (!string.IsNullOrEmpty(newApplyErr))
            {
                // open 은 성공했지만 일부 파라미터 적용 실패 — warning
                error = newApplyErr;
                AlarmManager.Raise(AlarmSeverity.Warning, "VISION-PARAMFAIL",
                    "Vision/" + algorithm, "신규 카메라 파라미터 적용 실패: " + newApplyErr);
                return false;
            }
            return true;
        }

        /// <summary>레거시 시그니처 — error 무시.</summary>
        public void RebindAlgorithmCamera(string algorithm, AlgorithmCameraMapping mapping)
            => RebindAlgorithmCamera(algorithm, mapping, out _);

        internal VisionModule ResolveModule(string algorithm)
        {
            switch (algorithm)
            {
                case VisionAlgorithm.Wafer:            return WaferMod;
                case VisionAlgorithm.Bin:              return BinMod;
                case VisionAlgorithm.BottomInspection: return BottomMod;
                case VisionAlgorithm.FrontSide:          return FrontSideMod;
                case VisionAlgorithm.RearSide:       return RearSideMod;
                default:                               return null;
            }
        }

        private void ShowTab(Tab t)
        {
            _pgOperation.Visible = t == Tab.Operation;
            _pgConfig   .Visible = t == Tab.Configuration;
            _pgRecipe   .Visible = t == Tab.Recipe;
            _pgDataLog  .Visible = t == Tab.DataLog;
            _pgSettings .Visible = t == Tab.Settings;

            btnOperation    .Selected = t == Tab.Operation;
            btnConfiguration.Selected = t == Tab.Configuration;
            btnRecipe       .Selected = t == Tab.Recipe;
            btnDataLog      .Selected = t == Tab.DataLog;
            btnSettings     .Selected = t == Tab.Settings;
        }

        private void TimerClock_Tick(object sender, EventArgs e) => UpdateClock();
        private void UpdateClock() => lblClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _viewWafer?.Dispose(); }     catch { }
            try { _viewBin?.Dispose(); }       catch { }
            try { _viewBottom?.Dispose(); }    catch { }
            try { _viewFrontSide?.Dispose(); } catch { }
            try { _viewRearSide?.Dispose(); }  catch { }
            try { _svrWafer?.Dispose(); }      catch { }
            try { _svrBin?.Dispose(); }        catch { }
            try { _svrBottom?.Dispose(); }     catch { }
            try { _svrFrontSide?.Dispose(); }    catch { }
            try { _svrRearSide?.Dispose(); } catch { }
            try { QMC.Vision.Comm.LightHub.DisposeAll(); } catch { }
            try { Backend?.Dispose(); }        catch { }
            base.OnFormClosing(e);
        }
    }
}
