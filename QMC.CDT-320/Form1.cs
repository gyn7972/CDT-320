using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Tabs;

namespace QMC.CDT_320
{
    /// <summary>?섎떒 硫붿씤 ???앸퀎??</summary>
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
        internal MotionMonitorService MotionMonitor { get; private set; }
        internal AjinIoScanService IoScan { get; private set; }
        internal string CurrentRecipeName { get; private set; }

        /// <summary>Stage 61 ???곷떒 ?곹깭諛?Project Name 媛깆떊.
        /// ProjectPage ??load/save ?먮뒗 SubsetPage ??save ???몄텧.</summary>
        public void RefreshProjectName(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName)) fileName = "-";
                CurrentRecipeName = NormalizeRecipeName(fileName);
                if (lblProjectValue.InvokeRequired)
                    lblProjectValue.Invoke((Action)(() => lblProjectValue.Text = fileName));
                else
                    lblProjectValue.Text = fileName;
            }
            catch { }
        }

        internal void LoadMachineSettings()
        {
            try
            {
                Machine?.LoadSettings();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    "NONE",
                    "DATA-LOAD",
                    "Machine settings load failed: " + ex.Message);
            }
            finally
            {
            }
        }

        internal void SaveMachineSettings()
        {
            try
            {
                if (Machine != null && !Machine.SaveSettings())
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "DATA-SAVE",
                        "Machine settings save returned false.");
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-SAVE",
                    "Machine settings save failed: " + ex.Message);
            }
            finally
            {
            }
        }

        internal void LoadMachineRecipe(string recipeName)
        {
            try
            {
                if (Machine == null || string.IsNullOrWhiteSpace(recipeName))
                    return;

                CurrentRecipeName = NormalizeRecipeName(recipeName);
                Machine.LoadRecipe(recipeName);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-LOAD",
                    "Machine recipe load failed: " + recipeName + " / " + ex.Message);
            }
            finally
            {
            }
        }

        internal void SaveMachineRecipe(string recipeName)
        {
            try
            {
                if (Machine == null || string.IsNullOrWhiteSpace(recipeName))
                    return;

                CurrentRecipeName = NormalizeRecipeName(recipeName);
                if (!Machine.SaveRecipe(recipeName))
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Alarm,
                        UserSession.Name,
                        "DATA-SAVE",
                        "Machine recipe save returned false: " + recipeName);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "DATA-SAVE",
                    "Machine recipe save failed: " + recipeName + " / " + ex.Message);
            }
            finally
            {
            }
        }

        private static string NormalizeRecipeName(string recipeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeName))
                    return string.Empty;

                recipeName = recipeName.Trim();
                if (recipeName.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                    recipeName = recipeName.Substring(0, recipeName.Length - ".Project".Length);

                return recipeName;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
            }
        }

        /// <summary>Stage 26 ???쒕? 移댁꽭???쇱꽌 ?쒕씪?대쾭 (sim 紐⑤뱶 ?꾩슜).</summary>
        internal QMC.CDT320.Sim.SimCassetteDriver CassetteDriver { get; private set; }
        /// <summary>Stage 41 ??SECS/HSMS Host (?ъ씠??硫붿떆吏 ?≪떊??.</summary>
        internal QMC.CDT320.Secs.SecsHost SecsHost { get; private set; }

        private WorkTab     _workTab;
        private WorkInfoTab _workInfoTab;
        private HistoryTab  _historyTab;
        private RecipeTab   _recipeTab;
        private SettingsTab _settingsTab;
        private UserTab     _userTab;
        private AxisJogPopup _jogPopup;
        private AxisPositionPopup _axisPositionPopup;

        private MainTab _currentTab = MainTab.Work;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ?ㅼ젙 濡쒕뱶 + ?몄뼱 蹂듭썝
            var cfg = AppSettingsStore.Load();
            if (!string.IsNullOrEmpty(cfg.Language)) Lang.SetLanguage(cfg.Language);
            QMC.Common.Alarms.AlarmManager.LanguageProvider = () => Lang.Current ?? "ko";

            // AJINEXTEK AXL (?ㅻ낫?? 而⑦뵾洹?濡쒕뱶 + ?쇱씠釉뚮윭由??닿린
            QMC.CDT320.Ajin.AjinConfigStore.Load();
            QMC.CDT320.Ajin.AjinFactory.UseRealBoard = cfg.UseAjin;
            if (cfg.UseAjin) 
                QMC.CDT320.Ajin.AjinSystem.Open(cfg.AjinIrqNo);

            QMC.CDT320.Ajin.AjinFactory.RegisterConfiguredAxes();

            // QMC.Vision TCP ?먮룞 ?곌껐 (鍮꾨룞湲? ?곌껐 ?ㅽ뙣?대룄 ?깆? 怨꾩냽)
            // Stage 43 ??6 梨꾨꼸: Wafer/Inspection/Bin + Main/TopSide/BottomSide
            if (cfg.VisionAutoConnect)
            {
                _ = QMC.CDT320.VisionComm.VisionHub.ConnectAllAsync(
                    cfg.VisionHost,
                    cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                    cfg.VisionMainPort,  cfg.VisionTopSidePort,    cfg.VisionBottomSidePort);
            }
            QMC.CDT320.VisionComm.VisionHub.ConnectionChanged += OnVisionHubChanged;

            // ?ㅻ퉬 + 釉뚮┸吏 + 而⑦듃濡ㅻ윭
            Machine    = new CDT320_Machine();
            LoadMachineSettings();
            Bridge     = new SimulatorBridge(Machine);
            Controller = new MachineController(Machine);
            MotionMonitor = new MotionMonitorService();
            MotionMonitor.Start(AjinAxisRegistry.GetOrderedAxes(), cfg.UseAjin ? 50 : 100);
            IoScan = new AjinIoScanService();
            IoScan.Start(EnumerateAjinInputs(Machine), EnumerateAjinOutputs(Machine), cfg.UseAjin ? 10 : 100, () => AjinSystem.IsOpen);

            // Stage 41 ??SecsHost ?몄뒪?댁뒪??(?듭뀡, 5000 ?ы듃)
            try
            {
                SecsHost = new QMC.CDT320.Secs.SecsHost(5000);
                Controller.SecsHost = SecsHost;
            }
            catch { /* SECS 誘몄????섍꼍?먯꽌 ?덉쟾 臾댁떆 */ }

            // Stage 26 ???쒕? 紐⑤뱶???뚮쭔 移댁꽭???쒕? ?쒕씪?대쾭 ?쒖꽦
            if (!cfg.UseAjin)
            {
                CassetteDriver = new QMC.CDT320.Sim.SimCassetteDriver(Machine.InputLoader, Machine.OutputUnloader);
            }
            Controller.StatusChanged += OnMachineStatusChanged;
            Controller.LogMessage    += s => QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, UserSession.Name, "CTRL", s);
            // Stage 33 ??auto-cycle 紐⑤뱶?먯꽌??Controller 濡쒓렇??Console 濡?異쒕젰 (媛?쒖꽦)
            if (Program.AutoCycleCount > 0)
                Controller.LogMessage += s => Console.WriteLine("[CTRL] " + s);

            // ??UserControl ?앹꽦 ???뚮씪誘명꽣 ?녿뒗 ?앹꽦??+ AttachHost 濡?Host 二쇱엯
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

            // Bottom navigation i18n tags.
            btnTabWork    .Tag = "i18n:tab.work";
            btnTabWorkInfo.Tag = "i18n:tab.workInfo";
            btnTabHistory .Tag = "i18n:tab.history";
            btnTabRecipe  .Tag = "i18n:tab.recipe";
            btnTabSettings.Tag = "i18n:tab.settings";
            btnTabUser    .Tag = "i18n:tab.user";
            btnTabExit    .Tag = "i18n:tab.exit";

            // Status bar i18n tags.
            lblMapMode        .Tag = "i18n:status.mapEmpty";
            lblProjectCaption .Tag = "i18n:status.project";
            lblBarcodeCaption .Tag = "i18n:status.barcode";
            lblBinCaption     .Tag = "i18n:status.bin";
            lblVision         .Tag = "i18n:status.vision";
            lblPick           .Tag = "i18n:status.pick";
            lblReference      .Tag = "i18n:status.reference";

            // ?ㅻ뜑
            lblTitle          .Tag = "i18n:app.title";
            lblUserCaption    .Tag = "i18n:header.user";
            lblTimeCaption    .Tag = "i18n:header.time";

            // ?몄뼱/?ъ슜??蹂寃??대깽??援щ룆
            Lang.LanguageChanged    += OnLocalizationChanged;
            UserSession.UserChanged += OnUserChanged;

            // 珥덇린 ?곸슜
            OnLocalizationChanged();

            // Stage 60 ??DEBUG 鍮뚮뱶 ??admin ?먮룞 濡쒓렇??(?ъ슜??蹂닿퀬: "?ㅻⅨ履?踰꾪듉 ?대┃ ?덈맖" ??沅뚰븳 遺議??먯씤 ?닿껐)
            // RELEASE 鍮뚮뱶??LoginDialog ?듯븳 ?뺤긽 濡쒓렇???좎?.
#if DEBUG
            QMC.CDT_320.Ui.Security.UserSession.ForceSet(
                "admin", QMC.CDT_320.Ui.Security.UserLevel.Admin);
            // Stage 60 ??DEBUG 鍮뚮뱶 ???쒕??덉씠???먮룞 ?곌껐 ?쒕룄 (?ы듃 7001, fire-and-forget)
            // ?쒕??덉씠??誘몄떎?됱씠硫??곌껐 ?ㅽ뙣?대룄 ?몃뱾?щ뒗 ?뺤긽 ?숈옉 (Sim 紐⑤뱶 ?먮룞 遺꾧린濡??ъ씠??OK).
            BeginInvoke(new Action(async () =>
            {
                try
                {
                    await Bridge.ConnectAsync("127.0.0.1", 7001);
                }
                catch { /* ?쒕??덉씠??誘몄떎????臾댁떆 */ }
            }));
#endif
            OnUserChanged();

            // 留덉?留??덉떆???먮룞 濡쒕뱶 ???ъ떆????吏곸쟾 ?ъ슜???꾨줈?앺듃 洹몃?濡??ъ슜
            try
            {
                var last = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                if (last != null)
                {
                    LoadMachineRecipe(last.FileName);
                    Controller.ApplyRecipeMode(last);
                    // Stage 61 ???곷떒 ?곹깭諛?Project Name 媛깆떊
                    RefreshProjectName(last.FileName);
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "NONE",
                        "RECIPE-LOAD",
                        "Project loaded: " + last.FileName + ".Project (auto on startup)");
                }
            }
            catch { /* ?덉떆???뚯씪 ?녾굅???먯긽 ??臾댁떆 */ }

            // ?쒓퀎 ?쒖옉
            timerClock.Start();
            UpdateClock();

            // Default page.
            ShowTab(MainTab.Work);

            // Stage 59 ??--start-page ?듭뀡 泥섎━. Stage 60 ??紐⑤뱺 ??뿉????寃??
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
                    // fallback: ??留ㅼ묶 ???섎㈃ ?ㅼ젙 ??쭔 ?꾩?
                    ShowTab(MainTab.Settings);
                }));
            }

            // Stage 60 ??--audit-all ?듭뀡: 紐⑤뱺 ??쓽 紐⑤뱺 ?섏씠吏瑜??쒕쾲??濡쒕뱶?섏뿬
            // UiClickAuditor ?듦퀎瑜?EventLog ???④릿?? ?ъ슜??蹂닿퀬 "?대┃ ?덈릺??踰꾪듉?? 諛쒓뎬??
            // Stage 60 R12 ??--click-test-all 異붽?: audit-all ??紐⑤뱺 ?섏씠吏??紐⑤뱺 踰꾪듉 PerformClick ?몄텧.
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
                        // 紐⑤뱺 ?섏씠吏媛 罹먯떆???? 媛??섏씠吏??紐⑤뱺 button PerformClick.
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
                            QMC.Common.Logging.EventLogger.Write(
                                QMC.Common.Logging.EventKind.Event,
                                QMC.CDT_320.Ui.Security.UserSession.Name,
                                "UI-CLICK-TEST-SUMMARY",
                                "tried=" + tt + " success=" + ss + " failed=" + ff);
                        }
                        catch { }
                    }

                    try { ShowTab(MainTab.Work); } catch { }
                }));
            }

            // Stage 24 ??auto-cycle 紐⑤뱶 (紐낅졊??--auto-cycle N) / Stage 58 ??--auto-init / --keep-open
            if (Program.AutoCycleCount > 0 || Program.AutoInitOnly)
            {
                int n = Program.AutoCycleCount;
                bool initOnly = Program.AutoInitOnly;
                bool keepOpen = Program.AutoCycleKeepOpen || initOnly;
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(8000);
                    try { await Controller.InitAsync(); } catch { }
                    if (initOnly) return; // GUI ?좎? ???ъ슜?먭? CYCLE RUN 吏곸젒 ?대┃
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
        /// ???꾪솚. ?섎떒 踰꾪듉?먯꽌 ?몄텧.
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

            // Apply focus, permission, and localization to the selected tab.
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

        private void RequestApplicationExit()
        {
            try
            {
                using (var dialog = new MessageBoxYesNo())
                {
                    DialogResult result = dialog.ShowDialog(
                        "종료",
                        "프로그램을 종료하시겠습니까?",
                        this,
                        new[] { "예", "아니오" });

                    if (result == DialogResult.Yes)
                    {
                        Log.Write("Main", UserSession.Name, "RequestApplicationExit", "Application exit requested by user. - Ok");
                        QMC.Common.Logging.EventLogger.Write(
                            QMC.Common.Logging.EventKind.Event,
                            UserSession.Name,
                            "APP-EXIT",
                            "Application exit requested.");
                        Close();
                    }
                    else
                    {
                        Log.Write("Main", UserSession.Name, "RequestApplicationExit", "Application exit requested by user. - canceled");
                        QMC.Common.Logging.EventLogger.Write(
                            QMC.Common.Logging.EventKind.Event,
                            UserSession.Name,
                            "APP-EXIT",
                            "Application exit canceled.");
                    }
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    UserSession.Name,
                    "APP-EXIT",
                    "Exit confirmation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnLocalizationChanged()
        {
            Lang.Apply(this);
            // ?곹깭 ?쇰꺼 ?ㅻⅨ履?"NONE" ? ?몄뀡 蹂寃??쒖뿉??媛깆떊?섎?濡?OnUserChanged ???덉쓬
        }

        private void OnUserChanged()
        {
            lblUserValue.Text = UserSession.Name + " (" + UserSession.Level + ")";
            RefreshStateBig();

            // Apply permission state.
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
            // ?곹깭諛?以묒븰 ?곸뿭??Vision ?곹깭 ?쒖떆 (?꾨줈?앺듃 ?대쫫 ?곗륫)
            var h = QMC.CDT320.VisionComm.VisionHub.AllConnected ? "O" : "X";
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

        private void ShowOrRestoreJogPopup(IWin32Window owner)
        {
            if (_jogPopup == null || _jogPopup.IsDisposed)
            {
                _jogPopup = new AxisJogPopup(CurrentAxes())
                {
                    StartPosition = FormStartPosition.CenterScreen,
                    ShowInTaskbar = true,
                    Owner = null
                };
                _jogPopup.Load += (s, e) => TaskbarHelper.SetAppId(_jogPopup.Handle, "CDT320.JogPanel");
                _jogPopup.FormClosed += (s, e) => { _jogPopup = null; };
                _jogPopup.FormClosing += (s, ev) =>
                {
                    if (ev.CloseReason == CloseReason.UserClosing)
                    {
                        ev.Cancel = true;
                        _jogPopup.Hide();
                    }
                };
            }

            ShowPopup(_jogPopup);
        }

        private void ShowOrRestoreAxisPositionPopup(IWin32Window owner)
        {
            if (_axisPositionPopup == null || _axisPositionPopup.IsDisposed)
            {
                _axisPositionPopup = new AxisPositionPopup(CurrentAxes(), MotionMonitor)
                {
                    StartPosition = FormStartPosition.CenterScreen,
                    ShowInTaskbar = true,
                    Owner = null
                };
                _axisPositionPopup.Load += (s, e) => TaskbarHelper.SetAppId(_axisPositionPopup.Handle, "CDT320.AxisPosition");
                _axisPositionPopup.FormClosed += (s, e) => { _axisPositionPopup = null; };
                _axisPositionPopup.FormClosing += (s, ev) =>
                {
                    if (ev.CloseReason == CloseReason.UserClosing)
                    {
                        ev.Cancel = true;
                        _axisPositionPopup.Hide();
                    }
                };
            }

            ShowPopup(_axisPositionPopup);
        }

        private static void ShowPopup(Form popup)
        {
            if (popup == null) return;
            if (!popup.Visible) popup.Show();
            if (popup.WindowState == FormWindowState.Minimized)
                popup.WindowState = FormWindowState.Normal;
            popup.BringToFront();
            popup.TopMost = true;
            popup.Activate();
        }

        private List<BaseAxis> CurrentAxes()
        {
            return AjinAxisRegistry.GetOrderedAxes();
        }

        private static IEnumerable<BaseAxis> EnumerateAxes(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var axis in EnumerateAxes(unit))
                    yield return axis;
        }

        private static IEnumerable<AjinDigitalInput> EnumerateAjinInputs(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var input in EnumerateAjinInputs(unit))
                    yield return input;
        }

        private static IEnumerable<AjinDigitalOutput> EnumerateAjinOutputs(CDT320_Machine machine)
        {
            if (machine == null) yield break;
            foreach (var unit in machine.Units)
                foreach (var output in EnumerateAjinOutputs(unit))
                    yield return output;
        }

        private static IEnumerable<BaseAxis> EnumerateAxes(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var axis = node as BaseAxis;
            if (axis != null)
            {
                yield return axis;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childAxis in EnumerateAxes(child))
                    yield return childAxis;
        }

        private static IEnumerable<AjinDigitalInput> EnumerateAjinInputs(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var input = node as AjinDigitalInput;
            if (input != null)
            {
                yield return input;
                yield break;
            }

            var cylinder = node as BaseCylinder;
            if (cylinder != null)
            {
                var inFwd = cylinder.InFwd as AjinDigitalInput;
                var inBwd = cylinder.InBwd as AjinDigitalInput;
                if (inFwd != null) yield return inFwd;
                if (inBwd != null) yield return inBwd;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childInput in EnumerateAjinInputs(child))
                    yield return childInput;
        }

        private static IEnumerable<AjinDigitalOutput> EnumerateAjinOutputs(BaseEquipmentNode node)
        {
            if (node == null) yield break;

            var output = node as AjinDigitalOutput;
            if (output != null)
            {
                yield return output;
                yield break;
            }

            var cylinder = node as BaseCylinder;
            if (cylinder != null)
            {
                var outFwd = cylinder.OutFwd as AjinDigitalOutput;
                var outBwd = cylinder.OutBwd as AjinDigitalOutput;
                if (outFwd != null) yield return outFwd;
                if (outBwd != null) yield return outBwd;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop == null) yield break;

            var components = prop.GetValue(node) as System.Collections.IEnumerable;
            if (components == null) yield break;

            foreach (BaseEquipmentNode child in components)
                foreach (var childOutput in EnumerateAjinOutputs(child))
                    yield return childOutput;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                AppSettingsStore.Current.Language = Lang.Current;
                AppSettingsStore.Save();
            }
            catch { }
            SaveMachineSettings();
            try { MotionMonitor?.Dispose(); } catch { }
            try { IoScan?.Dispose(); } catch { }
            try { if (_jogPopup != null && !_jogPopup.IsDisposed) _jogPopup.Dispose(); } catch { }
            try { if (_axisPositionPopup != null && !_axisPositionPopup.IsDisposed) _axisPositionPopup.Dispose(); } catch { }
            try { Bridge?.Dispose(); } catch { }
            try { QMC.CDT320.VisionComm.VisionHub.DisconnectAll(); } catch { }
            try { QMC.CDT320.Ajin.AjinSystem.Close(); } catch { }
            Lang.LanguageChanged    -= OnLocalizationChanged;
            UserSession.UserChanged -= OnUserChanged;
            base.OnFormClosing(e);
        }
    }
}

