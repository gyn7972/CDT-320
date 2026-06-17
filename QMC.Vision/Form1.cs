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
    /// <summary>하단 탭 식별자 — 핸들러 정렬: 작업 · 레시피 · 이력 · 설정 · 사용자.
    /// (환경설정은 별도 탭이 아니라 설정 탭의 GENERAL 페이지로 흡수)</summary>
    public enum Tab { Work, Recipe, History, Settings, User }

    public partial class Form1 : Form
    {
        internal IVisionBackend Backend { get; private set; }

        // 머신 루트 — 5개 모듈을 Components 로 소유(핸들러 CDT320Machine 정렬). Save/Recipe cascade 단일 진입점.
        internal VisionMachine Machine { get; private set; }

        internal WaferVisionModule        WaferMod      { get; private set; }
        internal BinVisionModule          BinMod        { get; private set; }
        internal BottomInspectionModule   BottomMod     { get; private set; }
        internal TopSideVisionModule    TopSideVisionMod    { get; private set; }
        internal BottomSideVisionModule BottomSideVisionMod { get; private set; }

        private VisionTcpServer _svrWafer, _svrBin, _svrBottom;
        private VisionTcpServer _svrTopSideVision, _svrBottomSideVision;
        private MainCommServer  _svrMain;   // 전역 통신(5104) — 레시피/전역 명령 수신

        // 원격 뷰어 (그랩 영상 송출) — 모듈별 5채널
        private GrabStreamServer _viewWafer, _viewBin, _viewBottom, _viewFrontSide, _viewRearSide;

        // 페이지 클래스명(OperationPage/DataLogPage)은 구현 그대로 유지, 탭 의미만 핸들러 정렬.
        // 핸들러 정렬: Form1 → Tab(UserControl) → Page 3단. 각 탭이 자체 사이드바 + 콘텐츠 호스트.
        private QMC.Vision.Ui.Tabs.WorkTab     _tabWork;
        private QMC.Vision.Ui.Tabs.RecipeTab   _tabRecipe;
        private QMC.Vision.Ui.Tabs.HistoryTab  _tabHistory;
        private QMC.Vision.Ui.Tabs.SettingsTab _tabSettings;
        private QMC.Vision.Ui.Tabs.UserTab     _tabUser;

        public Form1() { InitializeComponent(); }

        // ── 수명주기: Load 는 조립만, 세부는 Initialize* 헬퍼로 분리(핸들러 정렬) ──
        private void Form1_Load(object sender, EventArgs e)
        {
            var cfg = VisionConfigStore.Load();
            InitializeLighting(cfg);
            InitializeBackend(cfg);
            InitializeModulesAndMachine();
            InitializeServers(cfg);
            InitializeTabs();
            UpdateCameraStatusDot();

            timerClock.Start();
            UpdateClock();
            ShowTab(Tab.Work);
        }

        /// <summary>조명 시스템 Setup 로드 + 1회 마이그레이션 + LightHub 초기화 + 시작 시 시리얼 Open(비차단).</summary>
        private void InitializeLighting(VisionSettings cfg)
        {
            // Stage 69 — 조명 시스템 Setup 로드. 첫 기동 시 레거시 io_set 존재하면 1회 변환 + 백업.
            var lightSetup = QMC.Common.Recipes.LightSystemSetupStore.Load();
            if (lightSetup.Controllers == null || lightSetup.Controllers.Count == 0)
            {
                string ioSet = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "io_set.lightSource.json");
                var migrated = QMC.Common.Recipes.LightSystemMigrator.MigrateFromLegacy(ioSet);
                if (migrated != null)
                {
                    QMC.Common.Recipes.LightSystemMigrator.BackupLegacy(ioSet, DateTime.Now.ToString("yyyyMMdd"));
                    QMC.Common.Recipes.LightSystemSetupStore.SetCurrent(migrated);
                    QMC.Common.Recipes.LightSystemSetupStore.Save();
                    lightSetup = migrated;
                }
            }

            // Stage 73 — 조명 Sim 여부는 비전 Provider 와 독립(기본 true=Sim). 실점등은 [설정>조명]의 '조명 연결' 버튼.
            QMC.Vision.Comm.LightHub.Initialize(lightSetup, cfg.LightUseSim);

            // Stage 85 — 시작 시 자동 시리얼 Open(비차단). 실패 포트는 LIGHT-OPEN-FAIL 알람.
            _ = ConnectLightsOnStartupAsync();
        }

        /// <summary>비전 백엔드 선택 + 상태바 텍스트 / VISION 연결 동그라미.</summary>
        private void InitializeBackend(VisionSettings cfg)
        {
            Backend = VisionFactory.Global;
            lblStatusL.Text = $"Backend: {Backend.Name}   |   TCP: Wafer={cfg.WaferVisionPort}  Bin={cfg.BinVisionPort}  Bottom={cfg.InspectionVisionPort}";
            dotVision.IsOn  = Backend != null;
        }

        /// <summary>5개 모듈 생성(카메라 SSOT=모듈 Config) → Config/Recipe 로드+카메라 생성/적용 → 머신 루트 조립.</summary>
        private void InitializeModulesAndMachine()
        {
            WaferMod            = new WaferVisionModule      (null, Backend);
            BinMod              = new BinVisionModule        (null, Backend);
            BottomMod           = new BottomInspectionModule (null, Backend);
            TopSideVisionMod    = new TopSideVisionModule    (null, Backend);
            BottomSideVisionMod = new BottomSideVisionModule (null, Backend);

            InitModuleCamera(WaferMod,            VisionAlgorithm.Wafer,            "Sim/Wafer");
            InitModuleCamera(BinMod,              VisionAlgorithm.Bin,              "Sim/Bin");
            InitModuleCamera(BottomMod,           VisionAlgorithm.BottomInspection, "Sim/BottomInsp");
            InitModuleCamera(TopSideVisionMod,    VisionAlgorithm.FrontSide,        "Sim/FrontSide");
            InitModuleCamera(BottomSideVisionMod, VisionAlgorithm.RearSide,         "Sim/RearSide");

            // 머신 루트 — 5개 모듈을 Units 로 소유(핸들러 CDT320Machine 정렬). Save/Recipe cascade 단일 진입점.
            Machine = new VisionMachine(WaferMod, BinMod, BottomMod, TopSideVisionMod, BottomSideVisionMod);
        }

        /// <summary>모듈별 TCP 서버 + 전역 MainComm(5104) 서버 + 원격 뷰어 생성·시작.</summary>
        private void InitializeServers(VisionSettings cfg)
        {
            _svrWafer            = new VisionTcpServer(WaferMod,            cfg.WaferVisionPort);
            _svrBin              = new VisionTcpServer(BinMod,              cfg.BinVisionPort);
            _svrBottom           = new VisionTcpServer(BottomMod,          cfg.InspectionVisionPort);
            _svrTopSideVision    = new VisionTcpServer(TopSideVisionMod,    cfg.TopSideVisionPort);
            _svrBottomSideVision = new VisionTcpServer(BottomSideVisionMod, cfg.BottomSideVisionPort);
            try { _svrWafer            .Start(); } catch { }
            try { _svrBin              .Start(); } catch { }
            try { _svrBottom           .Start(); } catch { }
            try { _svrTopSideVision    .Start(); } catch { }
            try { _svrBottomSideVision .Start(); } catch { }

            // 전역 통신(MainComm 5104) — 핸들러 레시피 변경 수신 → 전 모듈 LoadRecipe cascade.
            try { _svrMain = new MainCommServer(Machine, cfg.MainCommPort); _svrMain.Start(); } catch { }

            // 원격 뷰어(모듈별 그랩 영상 송출).
            if (cfg.RemoteViewerEnable)
            {
                _viewWafer     = MakeViewer("Wafer",     cfg.WaferViewerPort,      WaferMod,            cfg);
                _viewBottom    = MakeViewer("Bottom",    cfg.InspectionViewerPort, BottomMod,           cfg);
                _viewBin       = MakeViewer("Bin",       cfg.BinViewerPort,        BinMod,              cfg);
                _viewFrontSide = MakeViewer("FrontSide", cfg.FrontSideViewerPort,  TopSideVisionMod,    cfg);
                _viewRearSide  = MakeViewer("RearSide",  cfg.RearSideViewerPort,   BottomSideVisionMod, cfg);
            }
        }

        /// <summary>탭 UserControl 5개 생성 + 콘텐츠 호스트 등록 + Host(Form1) 연결.</summary>
        private void InitializeTabs()
        {
            _tabWork     = new QMC.Vision.Ui.Tabs.WorkTab     { Dock = DockStyle.Fill, Visible = false };
            _tabRecipe   = new QMC.Vision.Ui.Tabs.RecipeTab   { Dock = DockStyle.Fill, Visible = false };
            _tabHistory  = new QMC.Vision.Ui.Tabs.HistoryTab  { Dock = DockStyle.Fill, Visible = false };
            _tabSettings = new QMC.Vision.Ui.Tabs.SettingsTab { Dock = DockStyle.Fill, Visible = false };
            _tabUser     = new QMC.Vision.Ui.Tabs.UserTab     { Dock = DockStyle.Fill, Visible = false };
            pnlContent.Controls.Add(_tabWork);
            pnlContent.Controls.Add(_tabRecipe);
            pnlContent.Controls.Add(_tabHistory);
            pnlContent.Controls.Add(_tabSettings);
            pnlContent.Controls.Add(_tabUser);
            _tabWork.AttachHost(this);
            _tabRecipe.AttachHost(this);
            _tabHistory.AttachHost(this);
            _tabSettings.AttachHost(this);
            _tabUser.AttachHost(this);
        }

        /// <summary>카메라 자동 Open 결과 집계 → CAMERA 연결 동그라미.</summary>
        private void UpdateCameraStatusDot()
        {
            int camOk = 0, camTotal = 5;
            void Tally(IVisionModule m) { if (m?.Camera != null && m.Camera.IsOpen) camOk++; }
            Tally(WaferMod); Tally(BinMod); Tally(BottomMod); Tally(TopSideVisionMod); Tally(BottomSideVisionMod);
            dotCamera.IsOn = (camTotal > 0 && camOk == camTotal);
        }

        /// <summary>Stage 85 — Form1.Load 시점에 LightHub 의 모든 컨트롤러 시리얼 Open 시도(비차단).
        /// 결과는 상태바에 표시. 실패 포트는 LIGHT-OPEN-FAIL 알람으로 이미 raise됨.
        /// 재연결은 [설정>조명 시스템]의 '조명 연결' 버튼.</summary>
        private async System.Threading.Tasks.Task ConnectLightsOnStartupAsync()
        {
            try
            {
                var res = await QMC.Vision.Comm.LightHub.ConnectAllAsync().ConfigureAwait(false);
                int ok = 0, total = res.Count;
                var fails = new System.Collections.Generic.List<string>();
                foreach (var kv in res)
                {
                    if (kv.Value) ok++;
                    else          fails.Add(kv.Key);
                }
                if (IsHandleCreated)
                    BeginInvoke((MethodInvoker)(() => UpdateLightStartupStatus(ok, total, fails)));
            }
            catch (Exception ex)
            {
                try { Console.WriteLine($"[LightHub] startup connect 예외: {ex.Message}"); } catch { }
            }
        }

        private void UpdateLightStartupStatus(int ok, int total, System.Collections.Generic.List<string> fails)
        {
            // LIGHT 연결 동그라미 — 인벤토리가 있고 전부 연결 성공이면 ON.
            if (dotLight != null) dotLight.IsOn = (total > 0 && fails.Count == 0);
        }

        /// <summary>C1/C3a — 카메라 SSOT=모듈 BaseUnit Config: LoadSettings/LoadRecipe → CameraId 로 생성·SetCamera·적용.
        /// 구 algorithm_camera.json 마이그 제거(C3a). 모듈 Config 없으면 fallbackId 카메라(저장은 사용자 편집 시).</summary>
        private static void InitModuleCamera(IVisionModule mod, string algorithm, string fallbackId)
        {
            if (mod == null) return;
            try { mod.LoadSettings(); mod.LoadRecipe("default"); } catch { }   // Config/Recipe + 알고리즘 cascade
            try { mod.MigrateLightPages(); } catch { }   // C3b-3 — 기존 조명 레벨 → 노드 LightPages 지정 도출

            // 카메라 생성(CameraId=생성 트리거) → SetCamera → Open → Config/Recipe 적용
            string camId = !string.IsNullOrEmpty(mod.CameraId) ? mod.CameraId : fallbackId;
            var cam = CameraFactory.CreateById(camId);
            mod.SetCamera(cam);
            try { cam.Open(); }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "VISION-CAMOPEN",
                    "Vision/" + algorithm, $"Camera.Open 실패 [{camId}]: {ex.Message}");
            }
            mod.ApplyCameraSettings();
        }

        /// <summary>모듈별 원격 뷰어 서버 생성+Start. 소스(GrabImage/ScreenRegion)에 따라 프레임 provider 선택.</summary>
        private GrabStreamServer MakeViewer(string name, int port, IVisionModule mod, VisionSettings cfg)
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
            IVisionModule mod = ResolveModule(algorithm);
            if (mod == null) { error = "unknown algorithm: " + algorithm; return false; }

            mod.DelayBeforeGrabMs = mapping.DelayBeforeGrabMs;

            // 카메라 ID 가 같으면 파라미터만 갱신, 다르면 카메라 교체
            if (string.Equals(mod.Camera?.Info?.Id, mapping.CameraId, StringComparison.OrdinalIgnoreCase))
            {
                // 같은 카메라라도 닫혀 있으면 먼저 연다(앱 시작 시 startup Open 실패로 닫힌 상태 복구).
                if (mod.Camera != null && !mod.Camera.IsOpen)
                {
                    try { mod.Camera.Open(); }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        AlarmManager.Raise(AlarmSeverity.Error, "VISION-CAMOPEN",
                            "Vision/" + algorithm, $"카메라 재오픈 실패 [{mapping.CameraId}]: {ex.Message}");
                        return false;
                    }
                }
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

        internal IVisionModule ResolveModule(string algorithm)
        {
            switch (algorithm)
            {
                case VisionAlgorithm.Wafer:            return WaferMod;
                case VisionAlgorithm.Bin:              return BinMod;
                case VisionAlgorithm.BottomInspection: return BottomMod;
                case VisionAlgorithm.FrontSide:          return TopSideVisionMod;
                case VisionAlgorithm.RearSide:       return BottomSideVisionMod;
                default:                               return null;
            }
        }

        private void ShowTab(Tab t)
        {
            _tabWork    .Visible = t == Tab.Work;
            _tabRecipe  .Visible = t == Tab.Recipe;
            _tabHistory .Visible = t == Tab.History;
            _tabSettings.Visible = t == Tab.Settings;
            _tabUser    .Visible = t == Tab.User;

            btnWork    .Selected = t == Tab.Work;
            btnRecipe  .Selected = t == Tab.Recipe;
            btnHistory .Selected = t == Tab.History;
            btnSettings.Selected = t == Tab.Settings;
            btnUser    .Selected = t == Tab.User;
        }

        // ── 하단 메뉴 네비게이션 (Designer 명명 핸들러) ──
        private void btnWork_Click(object sender, EventArgs e)     => ShowTab(Tab.Work);
        private void btnRecipe_Click(object sender, EventArgs e)   => ShowTab(Tab.Recipe);
        private void btnHistory_Click(object sender, EventArgs e)  => ShowTab(Tab.History);
        private void btnSettings_Click(object sender, EventArgs e) => ShowTab(Tab.Settings);
        private void btnUser_Click(object sender, EventArgs e)     => ShowTab(Tab.User);
        private void btnExit_Click(object sender, EventArgs e)     => this.Close();

        // ── 하단바 우측 버튼 런타임 배치 ──
        private void Form1_Shown(object sender, EventArgs e)              => LayoutBottomBar();
        private void pnlBottomBar_SizeChanged(object sender, EventArgs e) => LayoutBottomBar();

        /// <summary>하단바 우측 버튼(설정/사용자/종료)을 패널 실제 너비 기준으로 배치.
        /// Anchor=Top|Right 가 Maximized 전 폭을 기록해 밀려나는 문제 회피 — SizeChanged/Shown 에서 재배치.</summary>
        private void LayoutBottomBar()
        {
            if (pnlBottomBar == null || btnExit == null || btnSettings == null || btnUser == null) return;
            int w      = pnlBottomBar.ClientSize.Width;
            int btnW   = btnExit.Width > 0 ? btnExit.Width : 110;
            int gap    = 10;
            int margin = 20;
            int y      = (UiTheme.BottomBarHeight - (btnExit.Height > 0 ? btnExit.Height : 70)) / 2;

            // 우측 끝부터 역순: 종료 → 사용자 → 설정
            int x = w - margin - btnW;
            btnExit    .Location = new Point(x, y); x -= (btnW + gap);
            btnUser    .Location = new Point(x, y); x -= (btnW + gap);
            btnSettings.Location = new Point(x, y);
        }

        private void TimerClock_Tick(object sender, EventArgs e) => UpdateClock();
        private void UpdateClock() => lblTimeValue.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _viewWafer?.Dispose(); }     catch { }
            try { _viewBin?.Dispose(); }       catch { }
            try { _viewBottom?.Dispose(); }    catch { }
            try { _viewFrontSide?.Dispose(); } catch { }
            try { _viewRearSide?.Dispose(); }  catch { }
            try { _svrMain?.Dispose(); }       catch { }
            try { _svrWafer?.Dispose(); }      catch { }
            try { _svrBin?.Dispose(); }        catch { }
            try { _svrBottom?.Dispose(); }     catch { }
            try { _svrTopSideVision?.Dispose(); }    catch { }
            try { _svrBottomSideVision?.Dispose(); } catch { }

            // Stage 88 — 카메라 안전 정리 (TCP/뷰어 끊은 뒤, 조명/Backend 앞): 라이브 정지 → IVisionModule.Dispose(내부 Camera.Dispose).
            //   미정리 시 카메라 핸들이 남아 다음 실행에서 port 점유 가능.
            try { WaferMod    ?.Camera?.StopLive(); } catch { }
            try { BinMod      ?.Camera?.StopLive(); } catch { }
            try { BottomMod   ?.Camera?.StopLive(); } catch { }
            try { TopSideVisionMod?.Camera?.StopLive(); } catch { }
            try { BottomSideVisionMod ?.Camera?.StopLive(); } catch { }
            try { WaferMod    ?.Dispose(); } catch { }
            try { BinMod      ?.Dispose(); } catch { }
            try { BottomMod   ?.Dispose(); } catch { }
            try { TopSideVisionMod?.Dispose(); } catch { }
            try { BottomSideVisionMod ?.Dispose(); } catch { }

            try { QMC.Vision.Comm.LightHub.DisposeAll(); } catch { }
            try { Backend?.Dispose(); }        catch { }
            // 카메라(digitizer) 정리 후 MIL System/App 완전 해제 — 그래버 점유 해제(다음 실행/Intellicam 즉시 사용 가능).
            try { QMC.Vision.Cameras.Mil.MilSystem.Shutdown(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
