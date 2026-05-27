using System;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;
using QMC.CDT_320.Ui.Tabs;

namespace QMC.CDT_320
{
    /// <summary>하단 메인 탭 식별자.</summary>
    public enum MainTab
    {
        Work      = 0,
        WorkInfo  = 1,
        History   = 2,
        Recipe    = 3,
        Settings  = 4,
        User      = 5
    }

    public partial class Form1 : Form
    {
        internal CDT320_Machine    Machine        { get; private set; }
        internal SimulatorBridge   Bridge         { get; private set; }
        internal MachineController Controller     { get; private set; }

        /// <summary>Stage 61 — 상단 상태바 Project Name 갱신.
        /// ProjectPage 의 load/save 또는 SubsetPage 의 save 후 호출.</summary>
        public void RefreshProjectName(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName)) fileName = "-";
                if (lblProjectValue.InvokeRequired)
                    lblProjectValue.Invoke((Action)(() => lblProjectValue.Text = fileName));
                else
                    lblProjectValue.Text = fileName;
            }
            catch { }
        }
        /// <summary>Stage 26 — 시뮬 카세트 센서 드라이버 (sim 모드 전용).</summary>
        internal QMC.CDT320.Sim.SimCassetteDriver CassetteDriver { get; private set; }
        /// <summary>Stage 41 — SECS/HSMS Host (사이클 메시지 송신용).</summary>
        internal QMC.CDT320.Secs.SecsHost SecsHost { get; private set; }

        private WorkTab     _workTab;
        private WorkInfoTab _workInfoTab;
        private HistoryTab  _historyTab;
        private RecipeTab   _recipeTab;
        private SettingsTab _settingsTab;
        private UserTab     _userTab;

        private MainTab _currentTab = MainTab.Work;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 설정 로드 + 언어 복원
            var cfg = AppSettingsStore.Load();
            if (!string.IsNullOrEmpty(cfg.Language)) Lang.SetLanguage(cfg.Language);

            // AJINEXTEK AXL (실보드) 컨피그 로드 + 라이브러리 열기
            QMC.CDT320.Ajin.AjinConfigStore.Load();
            QMC.CDT320.Ajin.AjinFactory.UseRealBoard = cfg.UseAjin;
            if (cfg.UseAjin) QMC.CDT320.Ajin.AjinSystem.Open(cfg.AjinIrqNo);

            // QMC.Vision TCP 자동 연결 (비동기, 연결 실패해도 앱은 계속)
            // Stage 43 — 6 채널: Wafer/Inspection/Bin + Main/TopSide/BottomSide
            if (cfg.VisionAutoConnect)
            {
                _ = QMC.CDT320.VisionComm.VisionHub.ConnectAllAsync(
                    cfg.VisionHost,
                    cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                    cfg.VisionMainPort,  cfg.VisionTopSidePort,    cfg.VisionBottomSidePort);
            }
            QMC.CDT320.VisionComm.VisionHub.ConnectionChanged += OnVisionHubChanged;

            // 설비 + 브릿지 + 컨트롤러
            Machine    = new CDT320_Machine();
            Bridge     = new SimulatorBridge(Machine);
            Controller = new MachineController(Machine);

            // Stage 41 — SecsHost 인스턴스화 (옵션, 5000 포트)
            try
            {
                SecsHost = new QMC.CDT320.Secs.SecsHost(5000);
                Controller.SecsHost = SecsHost;
            }
            catch { /* SECS 미지원 환경에서 안전 무시 */ }

            // Stage 26 — 시뮬 모드일 때만 카세트 시뮬 드라이버 활성
            if (!cfg.UseAjin)
            {
                CassetteDriver = new QMC.CDT320.Sim.SimCassetteDriver(Machine.InputLoader, Machine.OutputUnloader);
            }
            Controller.StatusChanged += OnMachineStatusChanged;
            Controller.LogMessage    += s => QMC.CDT320.Logging.EventLogger.Write(QMC.CDT320.Logging.EventKind.Event, UserSession.Name, "CTRL", s);
            // Stage 33 — auto-cycle 모드에서는 Controller 로그도 Console 로 출력 (가시성)
            if (Program.AutoCycleCount > 0)
                Controller.LogMessage += s => Console.WriteLine("[CTRL] " + s);

            // 탭 UserControl 생성 — 파라미터 없는 생성자 + AttachHost 로 Host 주입
            _workTab     = new WorkTab     { Dock = DockStyle.Fill, Visible = false };
            _workInfoTab = new WorkInfoTab { Dock = DockStyle.Fill, Visible = false };
            _historyTab  = new HistoryTab  { Dock = DockStyle.Fill, Visible = false };
            _recipeTab   = new RecipeTab   { Dock = DockStyle.Fill, Visible = false };
            _settingsTab = new SettingsTab { Dock = DockStyle.Fill, Visible = false };
            _userTab     = new UserTab     { Dock = DockStyle.Fill, Visible = false };

            _workTab    .AttachHost(this);
            _workInfoTab.AttachHost(this);
            _historyTab .AttachHost(this);
            _recipeTab  .AttachHost(this);
            _settingsTab.AttachHost(this);
            _userTab    .AttachHost(this);

            pnlContent.Controls.Add(_workTab);
            pnlContent.Controls.Add(_workInfoTab);
            pnlContent.Controls.Add(_historyTab);
            pnlContent.Controls.Add(_recipeTab);
            pnlContent.Controls.Add(_settingsTab);
            pnlContent.Controls.Add(_userTab);

            // 하단 네비 버튼에 i18n 태그 부여
            btnTabWork    .Tag = "i18n:tab.work";
            btnTabWorkInfo.Tag = "i18n:tab.workInfo";
            btnTabHistory .Tag = "i18n:tab.history";
            btnTabRecipe  .Tag = "i18n:tab.recipe";
            btnTabSettings.Tag = "i18n:tab.settings";
            btnTabUser    .Tag = "i18n:tab.user";
            btnTabExit    .Tag = "i18n:tab.exit";

            // 상태바 텍스트에 i18n 태그 부여
            lblMapMode        .Tag = "i18n:status.mapEmpty";
            lblProjectCaption .Tag = "i18n:status.project";
            lblBarcodeCaption .Tag = "i18n:status.barcode";
            lblBinCaption     .Tag = "i18n:status.bin";
            lblVision         .Tag = "i18n:status.vision";
            lblPick           .Tag = "i18n:status.pick";
            lblReference      .Tag = "i18n:status.reference";

            // 헤더
            lblTitle          .Tag = "i18n:app.title";
            lblUserCaption    .Tag = "i18n:header.user";
            lblTimeCaption    .Tag = "i18n:header.time";

            // 언어/사용자 변경 이벤트 구독
            Lang.LanguageChanged    += OnLocalizationChanged;
            UserSession.UserChanged += OnUserChanged;

            // 초기 적용
            OnLocalizationChanged();

            // Stage 60 — DEBUG 빌드 시 admin 자동 로그인 (사용자 보고: "오른쪽 버튼 클릭 안됨" → 권한 부족 원인 해결)
            // RELEASE 빌드는 LoginDialog 통한 정상 로그인 유지.
#if DEBUG
            QMC.CDT_320.Ui.Security.UserSession.ForceSet(
                "admin", QMC.CDT_320.Ui.Security.UserLevel.Admin);
            // Stage 60 — DEBUG 빌드 시 시뮬레이터 자동 연결 시도 (포트 7001, fire-and-forget)
            // 시뮬레이터 미실행이면 연결 실패해도 핸들러는 정상 동작 (Sim 모드 자동 분기로 사이클 OK).
            BeginInvoke(new Action(async () =>
            {
                try
                {
                    await Bridge.ConnectAsync("127.0.0.1", 7001);
                }
                catch { /* 시뮬레이터 미실행 — 무시 */ }
            }));
#endif
            OnUserChanged();

            // 마지막 레시피 자동 로드 — 재시작 시 직전 사용한 프로젝트 그대로 사용
            try
            {
                var last = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                if (last != null)
                {
                    Controller.ApplyRecipeMode(last);
                    // Stage 61 — 상단 상태바 Project Name 갱신
                    RefreshProjectName(last.FileName);
                    QMC.CDT320.Logging.EventLogger.Write(
                        QMC.CDT320.Logging.EventKind.Event,
                        "NONE",
                        "RECIPE-LOAD",
                        "Project loaded: " + last.FileName + ".Project (auto on startup)");
                }
            }
            catch { /* 레시피 파일 없거나 손상 — 무시 */ }

            // 시계 시작
            timerClock.Start();
            UpdateClock();

            // 기본 탭
            ShowTab(MainTab.Work);

            // Stage 59 — --start-page 옵션 처리. Stage 60 — 모든 탭에서 키 검색.
            if (!string.IsNullOrEmpty(Program.StartPage))
            {
                BeginInvoke(new Action(() =>
                {
                    var pairs = new (MainTab tab, TabBase tb)[]
                    {
                        (MainTab.Work,     _workTab),
                        (MainTab.WorkInfo, _workInfoTab),
                        (MainTab.History,  _historyTab),
                        (MainTab.Recipe,   _recipeTab),
                        (MainTab.Settings, _settingsTab),
                        (MainTab.User,     _userTab),
                    };
                    foreach (var p in pairs)
                    {
                        if (p.tb.TryShowPage(Program.StartPage))
                        {
                            ShowTab(p.tab);
                            return;
                        }
                    }
                    // fallback: 키 매칭 안 되면 설정 탭만 띄움
                    ShowTab(MainTab.Settings);
                }));
            }

            // Stage 60 — --audit-all 옵션: 모든 탭의 모든 페이지를 한번씩 로드하여
            // UiClickAuditor 통계를 EventLog 에 남긴다. 사용자 보고 "클릭 안되는 버튼들" 발굴용.
            // Stage 60 R12 — --click-test-all 추가: audit-all 후 모든 페이지의 모든 버튼 PerformClick 호출.
            if (Program.AuditAll)
            {
                BeginInvoke(new Action(() =>
                {
                    var pairs = new (MainTab tab, TabBase tb)[]
                    {
                        (MainTab.Work,     _workTab),
                        (MainTab.WorkInfo, _workInfoTab),
                        (MainTab.History,  _historyTab),
                        (MainTab.Recipe,   _recipeTab),
                        (MainTab.Settings, _settingsTab),
                        (MainTab.User,     _userTab),
                    };
                    foreach (var p in pairs)
                    {
                        try { ShowTab(p.tab); } catch { }
                        Application.DoEvents();
                        try { p.tb.ShowAllPagesOnce(); } catch { }
                        Application.DoEvents();
                    }

                    if (Program.ClickTestAll)
                    {
                        // 모든 페이지가 캐시된 후, 각 페이지의 모든 button PerformClick.
                        int tt = 0, ss = 0, ff = 0;
                        foreach (var p in pairs)
                        {
                            try
                            {
                                var (t, s, f) = p.tb.PerformClickAllPages();
                                tt += t; ss += s; ff += f;
                            }
                            catch { }
                            Application.DoEvents();
                        }
                        try
                        {
                            QMC.CDT320.Logging.EventLogger.Write(
                                QMC.CDT320.Logging.EventKind.Event,
                                QMC.CDT_320.Ui.Security.UserSession.Name,
                                "UI-CLICK-TEST-SUMMARY",
                                "tried=" + tt + " success=" + ss + " failed=" + ff);
                        }
                        catch { }
                    }

                    try { ShowTab(MainTab.Work); } catch { }
                }));
            }

            // Stage 24 — auto-cycle 모드 (명령행 --auto-cycle N) / Stage 58 — --auto-init / --keep-open
            if (Program.AutoCycleCount > 0 || Program.AutoInitOnly)
            {
                int n = Program.AutoCycleCount;
                bool initOnly = Program.AutoInitOnly;
                bool keepOpen = Program.AutoCycleKeepOpen || initOnly;
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(8000);
                    try { await Controller.InitAsync(); } catch { }
                    if (initOnly) return; // GUI 유지 — 사용자가 CYCLE RUN 직접 클릭
                    await System.Threading.Tasks.Task.Delay(2000);
                    try { await Controller.CycleRunAsync(n); } catch { }
                    await System.Threading.Tasks.Task.Delay(Program.AutoCycleEndDelayMs);
                    if (!keepOpen)
                    {
                        try { BeginInvoke(new Action(() => Close())); } catch { }
                    }
                });
            }
        }

        /// <summary>
        /// 탭 전환. 하단 버튼에서 호출.
        /// </summary>
        public void ShowTab(MainTab tab)
        {
            _currentTab = tab;

            _workTab    .Visible = tab == MainTab.Work;
            _workInfoTab.Visible = tab == MainTab.WorkInfo;
            _historyTab .Visible = tab == MainTab.History;
            _recipeTab  .Visible = tab == MainTab.Recipe;
            _settingsTab.Visible = tab == MainTab.Settings;
            _userTab    .Visible = tab == MainTab.User;

            btnTabWork    .Selected = tab == MainTab.Work;
            btnTabWorkInfo.Selected = tab == MainTab.WorkInfo;
            btnTabHistory .Selected = tab == MainTab.History;
            btnTabRecipe  .Selected = tab == MainTab.Recipe;
            btnTabSettings.Selected = tab == MainTab.Settings;
            btnTabUser    .Selected = tab == MainTab.User;

            // 선택된 탭에 포커스를 주고, 권한/번역 재적용
            Control active = null;
            switch (tab)
            {
                case MainTab.Work:     active = _workTab;     break;
                case MainTab.WorkInfo: active = _workInfoTab; break;
                case MainTab.History:  active = _historyTab;  break;
                case MainTab.Recipe:   active = _recipeTab;   break;
                case MainTab.Settings: active = _settingsTab; break;
                case MainTab.User:     active = _userTab;     break;
            }
            if (active != null)
            {
                Lang.Apply(active);
                AccessControl.Apply(active);
            }
        }

        private void OnLocalizationChanged()
        {
            Lang.Apply(this);
            // 상태 라벨 오른쪽 "NONE" 은 세션 변경 시에도 갱신되므로 OnUserChanged 에 있음
        }

        private void OnUserChanged()
        {
            lblUserValue.Text = UserSession.Name + " (" + UserSession.Level + ")";
            RefreshStateBig();

            // 권한 재적용
            AccessControl.Apply(this);
        }

        private void OnMachineStatusChanged(QMC.CDT320.MachineStatus status)
        {
            if (InvokeRequired) { BeginInvoke(new Action<QMC.CDT320.MachineStatus>(OnMachineStatusChanged), status); return; }
            RefreshStateBig();
        }

        private void OnVisionHubChanged()
        {
            if (InvokeRequired) { BeginInvoke(new Action(OnVisionHubChanged)); return; }
            // 상태바 중앙 영역에 Vision 상태 표시 (프로젝트 이름 우측)
            var h = QMC.CDT320.VisionComm.VisionHub.AllConnected ? "●" : "○";
            lblBarcodeValue.Text = "VIS " + h;
            lblBarcodeValue.ForeColor = QMC.CDT320.VisionComm.VisionHub.AllConnected
                                            ? System.Drawing.Color.LightGreen
                                            : System.Drawing.Color.White;
        }

        private void RefreshStateBig()
        {
            var ms = Controller?.Status ?? QMC.CDT320.MachineStatus.Idle;
            lblStateBig.Text = ms.ToString().ToUpper();
            System.Drawing.Color c;
            switch (ms)
            {
                case QMC.CDT320.MachineStatus.Ready:       c = System.Drawing.Color.LightGreen;  break;
                case QMC.CDT320.MachineStatus.Running:     c = System.Drawing.Color.DeepSkyBlue; break;
                case QMC.CDT320.MachineStatus.Cycling:     c = UiTheme.LogoOrange;              break;
                case QMC.CDT320.MachineStatus.Initializing:c = System.Drawing.Color.Gold;        break;
                case QMC.CDT320.MachineStatus.Alarm:       c = System.Drawing.Color.IndianRed;   break;
                case QMC.CDT320.MachineStatus.Stopped:     c = System.Drawing.Color.Silver;      break;
                default:                                   c = System.Drawing.Color.White;       break;
            }
            lblStateBig.ForeColor = c;
        }

        private void TimerClock_Tick(object sender, EventArgs e) => UpdateClock();
        private void UpdateClock()
        {
            lblTimeValue.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                AppSettingsStore.Current.Language = Lang.Current;
                AppSettingsStore.Save();
            }
            catch { }
            try { Bridge?.Dispose(); } catch { }
            try { QMC.CDT320.VisionComm.VisionHub.DisconnectAll(); } catch { }
            try { QMC.CDT320.Ajin.AjinSystem.Close(); } catch { }
            Lang.LanguageChanged    -= OnLocalizationChanged;
            UserSession.UserChanged -= OnUserChanged;
            base.OnFormClosing(e);
        }
    }
}
