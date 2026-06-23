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
    public enum Tab { Work, Sequencer, Recipe, History, Settings, User }

    public partial class Form1 : Form
    {
        internal IVisionBackend Backend { get; private set; }

        // 머신 루트 — 5개 모듈을 Components 로 소유(핸들러 CDT320Machine 정렬). Save/Recipe cascade 단일 진입점.
        internal VisionMachine Machine { get; private set; }
        private QMC.Vision.Sequencing.VisionAutoSequenceHost _autoSeqHost;   // Sim 자동 시퀀스 호스트

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
        private QMC.Vision.Ui.Tabs.WorkTab      _tabWork;
        private QMC.Vision.Ui.Tabs.SequencerTab _tabSequencer;
        private QMC.Vision.Ui.Tabs.RecipeTab   _tabRecipe;
        private QMC.Vision.Ui.Tabs.HistoryTab  _tabHistory;
        private QMC.Vision.Ui.Tabs.SettingsTab _tabSettings;
        private QMC.Vision.Ui.Tabs.UserTab     _tabUser;

        public Form1() { InitializeComponent(); }

        // ── 수명주기: Load 는 조립만, 세부는 Initialize* 헬퍼로 분리(핸들러 정렬) ──
        private void Form1_Load(object sender, EventArgs e)
        {
            var cfg = VisionConfigStore.Load();
            // 데이터 저장 루트 적용 — 레시피/설비데이터 Store 사용 전에 반드시 먼저 설정.
            QMC.Common.Data.Store.DataPaths.Root = cfg.EffectiveDataRoot;
            QMC.Common.Data.Store.DataPaths.EnsureRoot();
            InitializeLighting(cfg);
            InitializeBackend(cfg);
            InitializeModulesAndMachine();
            EnsureDataStores(cfg);             // 기본 데이터 폴더 + default 레시피 보장(없으면 생성)
            RestoreLastRecipe(cfg);            // 마지막 적용 레시피 복원(+상단 Recipe 표시)
            InitializeServers(cfg);
            InitializeTabs();
            InitializeLocalization(cfg);
            UpdateCameraStatusDot();

            timerClock.Start();
            UpdateClock();
            StartResourceSampler();   // 리소스 샘플링/CSV 는 백그라운드(UI 버벅임 방지)
            ShowTab(Tab.Work);

            // 부팅 시 자동 실행하지 않음 — STOP 이 기준. Sim 자체 실행은 작업 화면 RUN 버튼으로 사용자가 시작.
        }

        /// <summary>저장 언어 적용 + 헤더/하단 탭 i18n 태그 + 언어 변경 시 전체 재번역(핸들러 Form1 정렬).
        /// 언어는 [설정 > GENERAL > 언어 설정] 콤보에서 변경하며, 변경 시 LanguageChanged → Lang.Apply(this).</summary>
        private void InitializeLocalization(VisionSettings cfg)
        {
            if (!string.IsNullOrEmpty(cfg.Language))
                QMC.Vision.Ui.Localization.Lang.SetLanguage(cfg.Language);

            lblTitle      .Tag = "i18n:app.title";
            lblUserCaption.Tag = "i18n:header.user";
            lblTimeCaption.Tag = "i18n:header.time";
            btnWork    .Tag = "i18n:tab.work";
            btnRecipe  .Tag = "i18n:tab.recipe";
            btnHistory .Tag = "i18n:tab.history";
            btnSettings.Tag = "i18n:tab.settings";
            btnUser    .Tag = "i18n:tab.user";
            btnExit    .Tag = "i18n:tab.exit";

            QMC.Vision.Ui.Localization.Lang.LanguageChanged += OnLocalizationChanged;
            OnLocalizationChanged();
        }

        /// <summary>언어 변경 후 메인 폼 전체 표시 문구를 다시 적용.</summary>
        private void OnLocalizationChanged() => QMC.Vision.Ui.Localization.Lang.Apply(this);

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
            dotVision.IsOn  = Backend != null;
            RefreshStatusBar();   // 초기 상태바(Recipe: -)
        }

        /// <summary>기본 데이터 폴더(Recipes/EquipmentData/Config/Log)와 'default' 레시피를 보장한다.
        /// 새 PC·새 배포 폴더(D:\CDT-320 등)에서 데이터 폴더가 없으면 저장이 일어나기 전까지 폴더가
        /// 만들어지지 않아 핸들러↔비전 레시피 연동이 비는 문제를 막는다(없을 때만 1회 생성).</summary>
        private void EnsureDataStores(VisionSettings cfg)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // 1) 기본 데이터 폴더 생성(이미 있으면 무해).
                System.IO.Directory.CreateDirectory(QMC.Common.Data.Store.RecipeDataStore.Root);
                System.IO.Directory.CreateDirectory(QMC.Common.Data.Store.EquipmentDataStore.Root);
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(baseDir, "Config"));
                EnsureRelativeDir(baseDir, cfg?.ImageLogPath ?? @".\Log\Image");
                EnsureRelativeDir(baseDir, cfg?.DataLogPath  ?? @".\Log\Data");

                if (Machine == null) return;

                // 2) 설비 고정 데이터(Setup/Config)가 없으면 현재값으로 최초 저장.
                if (!System.IO.Directory.Exists(QMC.Common.Data.Store.EquipmentDataStore.Root)
                    || System.IO.Directory.GetFiles(QMC.Common.Data.Store.EquipmentDataStore.Root, "*.json",
                           System.IO.SearchOption.AllDirectories).Length == 0)
                {
                    try { Machine.SaveSettings(); } catch { }
                }

                // 3) 'default' 레시피 폴더가 없으면 현재(기본) 런타임으로 최초 저장 → 연동 기준 레시피 확보.
                string defaultDir = QMC.Common.Data.Store.RecipeDataStore.DirOf("default");
                if (!System.IO.Directory.Exists(defaultDir))
                {
                    Machine.SetRecipe("default");
                    Machine.SaveRecipe("default");
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning, "SYSTEM", "DATA-INIT",
                    "EnsureDataStores 실패: " + ex.Message);
            }
        }

        /// <summary>상대경로(".\Log\Image" 등)를 baseDir 기준 절대경로로 만들어 폴더 생성.</summary>
        private static void EnsureRelativeDir(string baseDir, string relativeOrAbsolute)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return;
                string full = System.IO.Path.IsPathRooted(relativeOrAbsolute)
                    ? relativeOrAbsolute
                    : System.IO.Path.Combine(baseDir, relativeOrAbsolute.TrimStart('.', '\\', '/'));
                System.IO.Directory.CreateDirectory(full);
            }
            catch { }
        }

        /// <summary>마지막 적용 레시피(VisionSettings.LastRecipeName) 복원 — 전 모듈 로드 + 상단 표시.
        /// 미설정 시 'default' 로 복원(EnsureDataStores 가 default 를 보장).</summary>
        private void RestoreLastRecipe(VisionSettings cfg)
        {
            try
            {
                if (Machine == null) return;
                string last = cfg?.LastRecipeName;
                if (string.IsNullOrWhiteSpace(last)) last = "default";
                Machine.SetRecipe(last);
                SetRecipeStatus(last);
            }
            catch { }
        }

        // ── 상단 상태바 레시피 연동 ──
        private string _statusRecipe = "-";

        // ── 리소스(CPU/메모리) 모니터 — 사양 산정용. 1초마다 샘플링(TimerClock). ──
        private readonly ResourceMonitor _resMon = new ResourceMonitor();
        private System.Threading.Timer _resTimer;   // 리소스 샘플링/CSV 백그라운드 타이머(UI 스레드 부하 제거)
        private readonly ToolTip _resTip = new ToolTip();   // 상태바 hover 시 상세(스레드/핸들/GC/가동)

        /// <summary>레시피 적용/생성 시 상단 상태바 'Recipe:' 갱신 (RecipePage 가 호출).</summary>
        internal void SetRecipeStatus(string name)
        {
            _statusRecipe = string.IsNullOrWhiteSpace(name) ? "-" : name;
            RefreshStatusBar();
        }

        private void RefreshStatusBar()
        {
            try
            {
                var cfg = QMC.Vision.Config.VisionConfigStore.Current ?? QMC.Vision.Config.VisionConfigStore.Load();
                lblStatusL.Text = $"● READY   |   Recipe: {_statusRecipe}   |   Backend: {Backend?.Name}   |   TCP: W={cfg.WaferVisionPort} B={cfg.BinVisionPort} Bot={cfg.InspectionVisionPort}   |   {_resMon.ShortText()}";
            }
            catch { }
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

            // 정적 로그 저장부가 레시피별 토글(LogEnable)/이미지 저장 모드(ImageSaveMode)를 읽도록 활성 레시피 provider 등록.
            QMC.Vision.Core.ActiveRecipeContext.SetProvider(() => Machine?.Recipe);

            // Sim 자동 시퀀스 호스트(핸들러 TCP 없이 자체 순차 실행). 시작/정지는 RUN 버튼·GENERAL 설정·종료 훅에서.
            // 로그 싱크 연결 — 시퀀스 시작/정지/단계/실패가 이력(EventLogger "SEQ")에 남는다.
            _autoSeqHost = new QMC.Vision.Sequencing.VisionAutoSequenceHost(Machine, SeqLog);
        }

        /// <summary>시퀀서 호스트 — 시퀀서 테스트 페이지가 모듈별 시작/정지/스텝 및 로그 구독에 사용.</summary>
        internal QMC.Vision.Sequencing.VisionAutoSequenceHost AutoSeq => _autoSeqHost;

        /// <summary>시퀀서 로그 싱크 — 이력(EventLogger "SEQ") + 디버그 출력.</summary>
        private void SeqLog(string message)
        {
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "SEQ", "SEQ", message); }
            catch { }
            System.Diagnostics.Debug.WriteLine(message);
        }

        /// <summary>GENERAL 의 Sim 자체 실행 토글 변경 시 호출 — OFF 면 진행 중 시퀀스 정지(자동 시작은 안 함, RUN 버튼으로 시작).</summary>
        internal void ApplySimAutoSequence()
        {
            try { _autoSeqHost?.ApplyFromConfig(); }
            catch { }
        }

        private bool _runArmed;   // 실제(비Sim) 모드 RUN 상태 — RUN arming(핸들러 접속 시).
        private bool _ready;      // READY 상태 — RUN 활성 후 작업자가 누름. 핸들러 VISION 사용 게이트.
        private bool _prevHandlerConnected;   // 핸들러 접속 상승엣지 감지용(자동 RUN).
        private string _handlerRecipeName;    // 핸들러가 RECIPE 명령으로 지시한 레시피명(SSOT). READY 아닐 때 실시간 동기 체크 기준.
        private DateTime _lastRecipeReqUtc;   // 레시피 요청(RECIPEREQ) throttle — 미동기 시 과다 요청 방지.
        private int _recipeReqCount;          // 무응답 시 요청 횟수 상한(접속 시 0으로 리셋).

        /// <summary>MainComm 서버의 핸들러 접속/해제 이벤트 핸들러(수신 스레드) — UI 스레드로 마샬 후 자동 RUN 처리.
        /// 핸들러 측 VisionHub.ConnectionChanged 와 동일한 이벤트 기반 패턴.</summary>
        private void OnHandlerConnectionChanged(bool connected)
        {
            try
            {
                if (connected) _recipeReqCount = 0;   // 접속 시 레시피 요청 상한 리셋(재시도 허용).
                if (IsHandleCreated) BeginInvoke((Action)(() => AutoArmRunOnConnect()));
                else                 AutoArmRunOnConnect();
            }
            catch { }
        }

        /// <summary>핸들러 접속(상승엣지) 시 자동 RUN(arming) — 실제 모드 한정.
        /// RUN 만 자동 진행하고 READY 는 작업자가 눌러야 핸들러 VISION 사용 가능(안전). 접속 이벤트에서 호출.</summary>
        private void AutoArmRunOnConnect()
        {
            if (IsSelfRunMode) return;             // Sim 자체 실행은 자동 RUN 대상 아님
            bool connected = IsHandlerConnected;
            if (connected && !_prevHandlerConnected && !IsRunActive)
            {
                SetRun(true);                      // 접속 → 자동 RUN. READY 는 수동.
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "Run",
                          "핸들러 접속 → 자동 RUN(arming). READY 대기."); } catch { }
            }
            _prevHandlerConnected = connected;
        }

        /// <summary>Sim 자체 실행 모드 여부 — GENERAL 의 'Sim 자동 실행'(핸들러 없이 자체 순차 실행) 설정.</summary>
        internal bool IsSelfRunMode =>
            QMC.Vision.Config.VisionConfigStore.Current.SimAutoSequence;

        /// <summary>핸들러(MainComm 5104) 가 접속돼 있는가.</summary>
        internal bool IsHandlerConnected => _svrMain != null && _svrMain.HasClient;

        /// <summary>통신 페이지 상태 표시용 — 채널별 listen/접속 상태 스냅샷.</summary>
        internal struct CommChannelStatus
        {
            public string   Name;
            public int      Port;
            public bool     Listening;
            public bool     Connected;
            public DateTime LastRxUtc;   // default = 미수신
        }

        /// <summary>6 채널(Wafer/Inspection/Bin/Main/TopSide/BottomSide) 의 현재 listen·접속 상태를 반환한다.</summary>
        internal System.Collections.Generic.List<CommChannelStatus> GetVisionCommStatus()
        {
            var list = new System.Collections.Generic.List<CommChannelStatus>();
            void AddSvr(string name, VisionTcpServer s)
            {
                list.Add(s == null
                    ? new CommChannelStatus { Name = name, Port = 0, Listening = false, Connected = false, LastRxUtc = default(DateTime) }
                    : new CommChannelStatus { Name = name, Port = s.Port, Listening = s.IsRunning, Connected = s.HasClient, LastRxUtc = s.LastRxUtc });
            }
            AddSvr("WaferVision",      _svrWafer);
            AddSvr("BottomInspection", _svrBottom);
            AddSvr("BinVision",        _svrBin);
            list.Add(_svrMain == null
                ? new CommChannelStatus { Name = "MainComm", Port = 0, Listening = false, Connected = false, LastRxUtc = default(DateTime) }
                : new CommChannelStatus { Name = "MainComm", Port = _svrMain.Port, Listening = _svrMain.IsRunning, Connected = _svrMain.HasClient, LastRxUtc = _svrMain.LastRxUtc });
            AddSvr("TopSideVision",    _svrTopSideVision);
            AddSvr("BottomSideVision", _svrBottomSideVision);
            return list;
        }

        /// <summary>뷰어(영상 스트림, 5200대) 채널 상태 표시용.</summary>
        internal struct ViewerChannelStatus
        {
            public string Name;
            public int    Port;
            public bool   Listening;   // GrabStreamServer.IsRunning
            public int    Clients;     // 접속 클라이언트(핸들러 뷰어) 수
        }

        /// <summary>뷰어 서버 상태 — 명령 채널 행 순서와 정렬(Wafer/BottomInsp/Bin/[Main 없음]/TopSide/BottomSide).
        /// Main 은 영상 없음이라 목록에서 제외(5개). RemoteViewer 비활성 시 서버 null → 중지로 표시.</summary>
        internal System.Collections.Generic.List<ViewerChannelStatus> GetVisionViewerStatus()
        {
            var list = new System.Collections.Generic.List<ViewerChannelStatus>();
            void AddView(string name, QMC.Vision.Comm.GrabStreamServer v)
            {
                list.Add(v == null
                    ? new ViewerChannelStatus { Name = name, Port = 0, Listening = false, Clients = 0 }
                    : new ViewerChannelStatus { Name = name, Port = v.Port, Listening = v.IsRunning, Clients = v.ConnectedClients });
            }
            AddView("Wafer",      _viewWafer);
            AddView("Bottom",     _viewBottom);
            AddView("Bin",        _viewBin);
            AddView("FrontSide",  _viewFrontSide);
            AddView("RearSide",   _viewRearSide);
            return list;
        }

        /// <summary>RUN 전환 가능 여부 — Sim 자체 실행이면 항상, 아니면 핸들러 접속 시.</summary>
        internal bool CanRun => IsSelfRunMode || IsHandlerConnected;

        /// <summary>현재 RUN 중인가 — Sim 자체 실행은 시퀀서 가동, 실제 모드는 RUN arming 상태.</summary>
        internal bool IsRunActive => IsSelfRunMode ? (_autoSeqHost?.IsRunning ?? false) : _runArmed;

        /// <summary>READY 전환 가능 여부 — RUN 활성 상태에서만 READY 를 누를 수 있다.</summary>
        internal bool CanReady => IsRunActive;

        /// <summary>핸들러가 VISION 을 사용할 수 있는 상태인가 — READY(작업자 승인) + RUN 활성.
        /// 핸들러 명령(GRAB/MATCH 등) 게이트 조건. RUN 이 풀리면 자동 해제(<see cref="IsRunActive"/> 종속).</summary>
        internal bool IsReady => _ready && IsRunActive;

        /// <summary>작업 탭 READY 토글 — RUN 활성 상태에서만 켜진다. 켜지면 핸들러 VISION 사용 허용.</summary>
        internal void SetReady(bool on)
        {
            _ready = on && IsRunActive;
        }

        /// <summary>작업 탭 RUN/STOP 토글 — Sim 자체 실행은 시퀀서 시작/정지, 실제 모드는 RUN 상태 set(핸들러 접속 시).</summary>
        internal void SetRun(bool on)
        {
            try
            {
                if (!on) _ready = false;        // STOP 시 READY 자동 해제 — 핸들러 VISION 사용 차단
                if (on) _resMon.ResetPeaks();   // RUN 시작 시 피크 초기화 — 이번 가동 기준 최대 부하 측정
                if (IsSelfRunMode)
                {
                    if (on) _autoSeqHost?.Start(QMC.Vision.Config.VisionConfigStore.Current.SimSequenceIntervalMs);
                    else    _autoSeqHost?.Stop();
                }
                else
                {
                    // 실제 모드: 핸들러 접속 시에만 RUN. Phase 2 에서 _runArmed 를 VisionTcpServer 명령 게이트에 연결.
                    _runArmed = on && IsHandlerConnected;
                }
            }
            catch { }
        }

        /// <summary>핸들러에 "현재 레시피 알려줘" 요청(RECIPEREQ) 푸시 — 3초 throttle. 핸들러가 RECIPE 로 응답하면 _handlerRecipeName 기억.</summary>
        private void RequestHandlerRecipeThrottled()
        {
            try
            {
                if (_svrMain == null) return;
                if (_recipeReqCount >= 10) return;   // 핸들러 무응답 시 과다 요청 방지(재접속 시 리셋).
                if ((DateTime.UtcNow - _lastRecipeReqUtc).TotalSeconds < 3.0) return;
                _lastRecipeReqUtc = DateTime.UtcNow;
                _recipeReqCount++;
                _svrMain.RequestRecipe();
                if (_recipeReqCount >= 10)
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "VISION", "RecipeSync",
                        "핸들러가 레시피 요청에 응답하지 않음 — 요청 중단(재접속 시 재시도). 핸들러 빌드/활성 레시피 확인 필요.");
            }
            catch { }
        }

        /// <summary>핸들러 RECIPE 명령 수신(MainComm 수신 스레드) → UI 스레드로 마샬 후 지시 레시피명 기억.</summary>
        private void OnHandlerRecipeCommanded(int no, string name)
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired) { try { BeginInvoke((Action)(() => OnHandlerRecipeCommanded(no, name))); } catch { } return; }
                _handlerRecipeName = name;   // 지시 레시피 기억(DoRecipe 가 이미 적용함). 실시간 체크의 기준값.
                SetRecipeStatus(name);       // 상단 상태바 'Recipe:' 를 핸들러 적용 레시피로 갱신(통신 스레드 DoRecipe 는 UI 미접근).
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "RecipeSync",
                    "핸들러 레시피 수신/적용 → 상태바 갱신: no=" + no + " name=" + name);
            }
            catch { }
        }

        /// <summary>실시간 레시피 동기 체크 — READY 아닐 때만, 핸들러 접속 중 활성 레시피가 핸들러 지시와 다르면 핸들러 레시피로 재적용.
        /// READY(생산 사용 중)면 변경 금지(스킵). 같으면 아무것도 안 함(diff only). TimerClock_Tick(UI 스레드)에서 주기 호출.</summary>
        private void CheckHandlerRecipeSync()
        {
            try
            {
                if (Machine == null) return;
                if (!IsHandlerConnected) return;                            // 핸들러 비접속 시 강제 안 함(오프라인 편집 허용)
                if (string.IsNullOrWhiteSpace(_handlerRecipeName))
                {
                    RequestHandlerRecipeThrottled();                        // 지시 이력 없음 → 핸들러에 현재 레시피 능동 요청(throttle)
                    return;
                }
                if (IsReady) return;                                        // READY(생산 사용 중) — 변경 금지

                string active = Machine.CurrentRecipeName;
                if (string.Equals(active, _handlerRecipeName, StringComparison.OrdinalIgnoreCase)) return;  // 동일 → 변경 불필요

                // 다름 → 핸들러 지시 레시피로 재셋팅(없으면 SetRecipe 내부에서 생성/기본값 로드).
                Machine.SetRecipe(_handlerRecipeName);
                try
                {
                    var cfg = QMC.Vision.Config.VisionConfigStore.Current;
                    if (cfg != null) { cfg.LastRecipeName = _handlerRecipeName; QMC.Vision.Config.VisionConfigStore.Save(); }
                }
                catch { }
                SetRecipeStatus(_handlerRecipeName);
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "RecipeSync",
                    "READY 아님 — 활성('" + active + "') ≠ 핸들러('" + _handlerRecipeName + "') → 핸들러 레시피로 재적용");
            }
            catch (Exception ex)
            {
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "VISION", "RecipeSync", "동기 체크 실패: " + ex.Message); } catch { }
            }
        }

        /// <summary>모듈별 TCP 서버 + 전역 MainComm(5104) 서버 + 원격 뷰어 생성·시작.</summary>
        private void InitializeServers(VisionSettings cfg)
        {
            _svrWafer            = new VisionTcpServer(WaferMod,            cfg.WaferVisionPort);
            _svrBin              = new VisionTcpServer(BinMod,              cfg.BinVisionPort);
            _svrBottom           = new VisionTcpServer(BottomMod,          cfg.InspectionVisionPort);
            _svrTopSideVision    = new VisionTcpServer(TopSideVisionMod,    cfg.TopSideVisionPort);
            _svrBottomSideVision = new VisionTcpServer(BottomSideVisionMod, cfg.BottomSideVisionPort);
            // READY 게이트 — READY(작업자 승인 + RUN 활성) 상태에서만 핸들러 명령 수락(PING 제외). + 통신 로그 수집.
            foreach (var s in new[] { _svrWafer, _svrBin, _svrBottom, _svrTopSideVision, _svrBottomSideVision })
            {
                s.IsCommandAllowed = () => IsReady;
                s.Log += QMC.Vision.Comm.VisionCommLog.Add;
            }
            try { _svrWafer            .Start(); } catch { }
            try { _svrBin              .Start(); } catch { }
            try { _svrBottom           .Start(); } catch { }
            try { _svrTopSideVision    .Start(); } catch { }
            try { _svrBottomSideVision .Start(); } catch { }

            // 전역 통신(MainComm 5104) — 핸들러 레시피 변경 수신 → 전 모듈 LoadRecipe cascade.
            try
            {
                _svrMain = new MainCommServer(Machine, cfg.MainCommPort);
                _svrMain.Log += QMC.Vision.Comm.VisionCommLog.Add;
                _svrMain.ConnectionChanged += OnHandlerConnectionChanged;   // 핸들러 접속 이벤트 → 자동 RUN(핸들러 패턴 정렬)
                _svrMain.RecipeCommanded += OnHandlerRecipeCommanded;       // 핸들러 지시 레시피 기억(실시간 동기 체크 기준)
                _svrMain.Start();
            }
            catch { }

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
            _tabWork      = new QMC.Vision.Ui.Tabs.WorkTab      { Dock = DockStyle.Fill, Visible = false };
            _tabSequencer = new QMC.Vision.Ui.Tabs.SequencerTab { Dock = DockStyle.Fill, Visible = false };
            _tabRecipe    = new QMC.Vision.Ui.Tabs.RecipeTab    { Dock = DockStyle.Fill, Visible = false };
            _tabHistory   = new QMC.Vision.Ui.Tabs.HistoryTab   { Dock = DockStyle.Fill, Visible = false };
            _tabSettings  = new QMC.Vision.Ui.Tabs.SettingsTab  { Dock = DockStyle.Fill, Visible = false };
            _tabUser      = new QMC.Vision.Ui.Tabs.UserTab      { Dock = DockStyle.Fill, Visible = false };
            pnlContent.Controls.Add(_tabWork);
            pnlContent.Controls.Add(_tabSequencer);
            pnlContent.Controls.Add(_tabRecipe);
            pnlContent.Controls.Add(_tabHistory);
            pnlContent.Controls.Add(_tabSettings);
            pnlContent.Controls.Add(_tabUser);
            _tabWork.AttachHost(this);
            _tabSequencer.AttachHost(this);
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
                // GrabImage: 새 그랩이 들어왔거나(seq) 결과/마크가 갱신됐을 때(rev) 프레임 제공.
                // rev 도 보는 이유: MATCH 가 그랩(seq++) 후에 마크를 기록하므로, seq 만 보면 마크가 다음 그랩까지 한 박자 늦게 전송됨.
                long lastSeq = -1; long lastRev = -1;
                provider = () =>
                {
                    long s  = mod.ViewerFrameSeq;
                    long rv = QMC.Vision.Core.ModuleResultStore.Revision(mod.Name);
                    if (s == lastSeq && rv == lastRev) return null;
                    lastSeq = s; lastRev = rv;
                    return mod.AcquireViewerFrame();
                };
            }
            // 프레임 동봉 메타(핸들러 측정·결과 오버레이용): 스케일(mm/px) + 최신 판정/결과.
            Func<QMC.Common.Ui.Controls.VisionFrameMeta> metaProvider = () =>
            {
                var meta = new QMC.Common.Ui.Controls.VisionFrameMeta { Module = mod.Name };
                try { var cm = mod.ExportCameraMapping(); meta.ScaleX = cm.ScaleX; meta.ScaleY = cm.ScaleY; } catch { }
                try
                {
                    bool pass; string[] lines;
                    if (QMC.Vision.Core.ModuleResultStore.TryGet(mod.Name, out pass, out lines))
                    {
                        meta.Verdict = pass ? "OK" : "NG";
                        meta.VerdictPass = pass;
                        meta.ResultLines = lines;
                    }
                }
                catch { }
                // 최근 MATCH 오버레이(찾은 위치/각/박스 + 검색 ROI) → 핸들러 뷰어가 영상 위에 표시.
                try
                {
                    if (QMC.Vision.Core.MatchOverlayStore.TryGet(mod.Name, out var ov))
                    {
                        if (ov.Marks != null && ov.Marks.Length > 0)
                        {
                            var fm = new QMC.Common.Ui.Controls.FrameMark[ov.Marks.Length];
                            for (int i = 0; i < ov.Marks.Length; i++)
                            {
                                var k = ov.Marks[i];
                                fm[i] = new QMC.Common.Ui.Controls.FrameMark
                                { X = k.X, Y = k.Y, Score = k.Score, Angle = k.Angle, BoxW = k.BoxW, BoxH = k.BoxH };
                            }
                            meta.Marks = fm;
                        }
                        meta.RoiX = ov.RoiX; meta.RoiY = ov.RoiY; meta.RoiW = ov.RoiW; meta.RoiH = ov.RoiH;
                    }
                }
                catch { }
                return meta;
            };

            var srv = new GrabStreamServer(name, port, provider, cfg.RemoteViewerFps, cfg.RemoteViewerQuality, metaProvider, cfg.RemoteViewerMaxSize);
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
                    AlarmManager.Raise(AlarmSeverity.Error, "VISION-PARAMFAIL",
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
                AlarmManager.Raise(AlarmSeverity.Error, "VISION-PARAMFAIL",
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
            _tabWork     .Visible = t == Tab.Work;
            _tabSequencer.Visible = t == Tab.Sequencer;
            _tabRecipe   .Visible = t == Tab.Recipe;
            _tabHistory  .Visible = t == Tab.History;
            _tabSettings .Visible = t == Tab.Settings;
            _tabUser     .Visible = t == Tab.User;

            btnWork     .Selected = t == Tab.Work;
            btnSequencer.Selected = t == Tab.Sequencer;
            btnRecipe   .Selected = t == Tab.Recipe;
            btnHistory  .Selected = t == Tab.History;
            btnSettings .Selected = t == Tab.Settings;
            btnUser     .Selected = t == Tab.User;
        }

        // ── 하단 메뉴 네비게이션 (Designer 명명 핸들러) ──
        private void btnWork_Click(object sender, EventArgs e)     => ShowTab(Tab.Work);
        private void btnSequencer_Click(object sender, EventArgs e) => ShowTab(Tab.Sequencer);
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

        private void TimerClock_Tick(object sender, EventArgs e)
        {
            // UI 스레드 틱은 시계만(가벼움). 무거운 리소스 샘플링/CSV/상태바는 백그라운드(StartResourceSampler).
            UpdateClock();
            CheckHandlerRecipeSync();   // 실시간 레시피 동기 — READY 아닐 때 활성≠핸들러면 핸들러 레시피로 재적용(diff only)
        }

        /// <summary>리소스 샘플링 + CSV(디스크 IO) 를 백그라운드 스레드에서 1초 주기로 수행한다.
        /// Process/GPU 카운터 조회와 파일 쓰기를 UI 스레드에서 빼 버벅임을 방지. UI 갱신만 마샬.
        /// 콜백 재진입 방지를 위해 주기 타이머 대신 매 콜백 끝에서 재무장한다.</summary>
        private void StartResourceSampler()
        {
            if (_resTimer != null) return;
            _resTimer = new System.Threading.Timer(ResSampleTick, null, 1000, System.Threading.Timeout.Infinite);
        }

        private void ResSampleTick(object state)
        {
            try
            {
                _resMon.Sample();
                if (QMC.Vision.Config.VisionConfigStore.Current?.ResourceLogEnable == true)
                    AppendResourceCsv();   // 디스크 IO — 백그라운드에서
                // 가벼운 UI 갱신(상태바 텍스트/툴팁)만 UI 스레드로 마샬.
                if (IsHandleCreated)
                    BeginInvoke((Action)(() =>
                    {
                        RefreshStatusBar();
                        try { _resTip.SetToolTip(lblStatusL, _resMon.DetailText()); } catch { }
                    }));
            }
            catch { }
            finally { try { _resTimer?.Change(1000, System.Threading.Timeout.Infinite); } catch { } }
        }
        private void UpdateClock() => lblTimeValue.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // ── 리소스 CSV 로깅 (DataRoot\Log\Resource\resource_yyyyMMdd.csv) ──
        private bool _resCsvHeaderWritten;
        private void AppendResourceCsv()
        {
            try
            {
                string dir = System.IO.Path.Combine(QMC.Common.Data.Store.DataPaths.Root, "Log", "Resource");
                System.IO.Directory.CreateDirectory(dir);
                string path = System.IO.Path.Combine(dir, "resource_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                if (!_resCsvHeaderWritten && !System.IO.File.Exists(path))
                    System.IO.File.AppendAllText(path, ResourceMonitor.CsvHeader() + Environment.NewLine);
                _resCsvHeaderWritten = true;
                System.IO.File.AppendAllText(path, _resMon.CsvLine() + Environment.NewLine);
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _resTimer?.Dispose(); _resTimer = null; } catch { }
            try { _autoSeqHost?.Stop(); }      catch { }
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
